/* ========================================================================
 * Copyright (C) 2020 Joe Clapis.
 * 
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 * 
 *     http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 * ======================================================================== */

/* This code was largely inspired by SparkFun's L6470 AutoDriver dSPIN library for
 * the Arduino, written by Mike Hord, and extensive perusing of the L6470 datasheets
 * and application notes.
 * 
 * You can find the SparkFun library here:
 * https://github.com/sparkfun/L6470-AutoDriver/tree/master/Libraries/Arduino/src */


using System;
using System.Threading;

namespace Sedna
{
    #region Enums

    /// <summary>
    /// This represents the options for the overcurrent threshold setting. It corresponds
    /// to the OCD_TH register.
    /// </summary>
    internal enum OvercurrentThreshold : byte
    {
        _375mA,
        _750mA,
        _1125mA,
        _1500mA,
        _1875mA,
        _2250mA,
        _2625mA,
        _3000mA,
        _3375mA,
        _3750mA,
        _4125mA,
        _4500mA,
        _4875mA,
        _5250mA,
        _5625mA,
        _6000mA
    };


    /// <summary>
    /// This defines the allowed microstepping modes (how finely the driver will divide
    /// a step into microsteps). It corresponds to the lowest 3 bits of the STEP_MODE
    /// register.
    /// </summary>
    internal enum MicrostepMode : byte
    {
        _1PerStep,
        _2PerStep,
        _4PerStep,
        _8PerStep,
        _16PerStep,
        _32PerStep,
        _64PerStep,
        _128PerStep
    };


    /// <summary>
    /// This defines the options for the PWM frequency applied to the power bridges. It
    /// corresponds to the F_PWM_INT and F_PWM_DEC bits of the CONFIG register. The values
    /// are given in kHz, and assume a clock frequency of 16 MHz. If you're using a different
    /// clock frequency, multiply these values by the clock (in MHz) and divide by 16 to get
    /// the actual frequency they'll end up producing.
    /// </summary>
    /// <remarks>
    /// Consult pages 32 and 49 of the datasheet for more info on the PWM stuff.
    /// </remarks>
    internal enum PwmFrequency : byte
    {
        /// <summary>
        /// 2.790 kHz
        /// </summary>
        _2_790 = 0b110000,

        /// <summary>
        /// 3.255 kHz
        /// </summary>
        _3_255 = 0b101000,

        /// <summary>
        /// 3.348 kHz
        /// </summary>
        _3_348 = 0b110001,

        /// <summary>
        /// 3.906 kHz
        /// </summary>
        _3_906 = 0b100000,

        /// <summary>
        /// 4.464 kHz
        /// </summary>
        _4_464 = 0b110011,

        /// <summary>
        /// 4.557 kHz
        /// </summary>
        _4_557 = 0b101010,

        /// <summary>
        /// 4.688 kHz
        /// </summary>
        _4_688 = 0b100001,

        /// <summary>
        /// 4.883 kHz
        /// </summary>
        _4_883 = 0b011000,

        /// <summary>
        /// 5.208 kHz
        /// </summary>
        _5_208 = 0b101011,

        /// <summary>
        /// 5.469 kHz
        /// </summary>
        _5_469 = 0b100010,

        /// <summary>
        /// 5.580 kHz
        /// </summary>
        _5_580 = 0b110100,

        /// <summary>
        /// 5.859 kHz
        /// </summary>
        _5_859 = 0b011001,

        /// <summary>
        /// 6.250 kHz
        /// </summary>
        _6_250 = 0b100011,

        /// <summary>
        /// 6.510 kHz
        /// </summary>
        _6_510 = 0b010000,

        /// <summary>
        /// 6.696 kHz
        /// </summary>
        _6_696 = 0b110101,

        /// <summary>
        /// 6.836 kHz
        /// </summary>
        _6_836 = 0b011010,

        /// <summary>
        /// 7.813 kHz
        /// </summary>
        _7_813 = 0b010001,

        /// <summary>
        /// 8.929 kHz
        /// </summary>
        _8_929 = 0b110111,

        /// <summary>
        /// 9.115 kHz
        /// </summary>
        _9_115 = 0b010010,

        /// <summary>
        /// 9.375 kHz
        /// </summary>
        _9_375 = 0b100101,

        /// <summary>
        /// 9.766 kHz
        /// </summary>
        _9_766 = 0b001000,

        /// <summary>
        /// 10.417 kHz
        /// </summary>
        _10_417 = 0b010011,

        /// <summary>
        /// 10.938 kHz
        /// </summary>
        _10_938 = 0b100110,

        /// <summary>
        /// 11.719 kHz
        /// </summary>
        _11_719 = 0b001001,

        /// <summary>
        /// 12.500 kHz
        /// </summary>
        _12_500 = 0b100111,

        /// <summary>
        /// 13.021 kHz
        /// </summary>
        _13_021 = 0b010100,

        /// <summary>
        /// 13.672 kHz
        /// </summary>
        _13_672 = 0b001010,

        /// <summary>
        /// 15.625 kHz
        /// </summary>
        _15_625 = 0b001011,

        /// <summary>
        /// 18.229 kHz
        /// </summary>
        _18_229 = 0b010110,

        /// <summary>
        /// 19.531 kHz
        /// </summary>
        _19_531 = 0b000000,

        /// <summary>
        /// 20.833 kHz
        /// </summary>
        _20_833 = 0b010111,

        /// <summary>
        /// 23.438 kHz
        /// </summary>
        _23_438 = 0b000001,

        /// <summary>
        /// 27.344 kHz
        /// </summary>
        _27_344 = 0b000010,

        /// <summary>
        /// 31.250 kHz
        /// </summary>
        _31_250 = 0b000011,

        /// <summary>
        /// 39.063 kHz
        /// </summary>
        _39_063 = 0b000100,

        /// <summary>
        /// 46.875 kHz
        /// </summary>
        _46_875 = 0b000101,

        /// <summary>
        /// 54.688 kHz
        /// </summary>
        _54_688 = 0b000110,

        /// <summary>
        /// 62.500 kHz
        /// </summary>
        _62_500 = 0b000111
    };


    /// <summary>
    /// Indicates which direction the motor is moving.
    /// </summary>
    internal enum MotorDirection : byte
    {
        Reverse,
        Forward
    };


    /// <summary>
    /// Indicates what the motor is currently doing.
    /// </summary>
    internal enum MotorActivity : byte
    {
        Stopped,
        Accelerating,
        Decelerating,
        ConstantSpeed
    };


    /// <summary>
    /// These are the IDs of the various registers on the L6470
    /// </summary>
    internal enum Register : byte
    {
        AbsolutePosition = 0x01,
        ElectricalPosition,
        MarkPosition,
        CurrentSpeed,
        Acceleration,
        Deceleration,
        MaximumSpeed,
        MinimumSpeed,
        HoldingKVal,
        ConstantSpeedKVal,
        AccelerationStartingKVal,
        DecelerationStartingKVal,
        IntersectSpeed,
        StartSlope,
        AccelerationFinalSlope,
        DecelerationFinalSlope,
        ThermalCompensationFactor,
        AdcOutput,
        OvercurrentThreshold,
        StallThreshold,
        FullStepSpeed,
        StepMode,
        AlarmEnables,
        Configuration,
        Status
    };

    #endregion


    /// <summary>
    /// This represents the current status of an L6470.
    /// </summary>
    internal class L6470Status
    {
        /// <summary>
        /// True if step clock mode is active, false if it's in a normal running mode.
        /// </summary>
        /// <remarks>This corresponds to the SCK_MOD bit.</remarks>
        public bool StepClockModeActive { get; }


        /// <summary>
        /// True if motor coil A is stalled.
        /// </summary>
        /// <remarks>This corresponds to the STEP_LOSS_A bit.</remarks>
        public bool BridgeAStalled { get; }


        /// <summary>
        /// True if motor coil B is stalled.
        /// </summary>
        /// <remarks>This corresponds to the STEP_LOSS_B bit.</remarks>
        public bool BridgeBStalled { get; }


        /// <summary>
        /// True if the device shutdown because the motor's maximum current was exceeded.
        /// </summary>
        /// <remarks>This corresponds to the OCD bit.</remarks>
        public bool OvercurrentDetected { get; }


        /// <summary>
        /// True if the device shutdown because it passed its temperature cutoff.
        /// </summary>
        /// <remarks>This corresponds to the TH_SD bit.</remarks>
        public bool ThermalShutdownTriggered { get; }


        /// <summary>
        /// True if the device is getting too hot, and is in danger of shutting down.
        /// </summary>
        /// <remarks>This corresponds to the TH_WRN bit.</remarks>
        public bool ThermalWarningTriggered { get; }


        /// <summary>
        /// True if the device is disabled because the motor coils don't have enough
        /// voltage.
        /// </summary>
        /// <remarks>This corresponds to the UVLO bit.</remarks>
        public bool UndervoltageDetected { get; }


        /// <summary>
        /// True if the last command sent to the device wasn't understood (e.g. it wasn't
        /// one of the commands defined in the datasheet).
        /// </summary>
        /// <remarks>This corresponds to the WRONG_CMD bit.</remarks>
        public bool ReceivedUnknownCommand { get; }


        /// <summary>
        /// True if the last command sent failed or wasn't performed properly.
        /// </summary>
        /// <remarks>This corresponds to the NOTPERF_CMD bit.</remarks>
        public bool LastCommandFailed { get; }


        /// <summary>
        /// Defines what the motor is currently doing.
        /// </summary>
        /// <remarks>This corresponds to the MOT_STATUS bits.</remarks>
        public MotorActivity MotorState { get; }


        /// <summary>
        /// Defines what direction the motor is running in.
        /// </summary>
        /// <remarks>This corresponds to the DIR bit.</remarks>
        public MotorDirection Direction { get; }


        /// <summary>
        /// True if the motors stopped because the kill switch was pressed.
        /// </summary>
        /// <remarks>This corresponds to the SW_EVN bit.</remarks>
        public bool KillSwitchTriggered { get; }


        /// <summary>
        /// True if the motor kill switch is currently turned on / pressed.
        /// </summary>
        /// <remarks>This corresponds to the SW_F bit.</remarks>
        public bool KillSwitchActive { get; }


        /// <summary>
        /// True if the device is currently busy performing an operation.
        /// </summary>
        /// <remarks>This corresponds to the BUSY bit.</remarks>
        public bool IsBusy { get; }


        /// <summary>
        /// True if the motor coils are currently powered up.
        /// </summary>
        /// <remarks>This corresponds to the opposite of the HiZ bit.</remarks>
        public bool BridgesActive { get; }


        /// <summary>
        /// Creates a new <see cref="L6470Status"/> instance.
        /// </summary>
        /// <param name="StatusValue">The value provided by the SPI bus after a
        /// GetStatus() command.</param>
        public L6470Status(ushort StatusValue)
        {
            // SCK_MOD is bit 15, active high
            byte stepClockModeActive = (byte)(StatusValue >> 15 & 1);
            StepClockModeActive = (stepClockModeActive == 1);

            // STEP_LOSS_B is bit 14, active low
            byte bridgeBStalled = (byte)(StatusValue >> 14 & 1);
            BridgeBStalled = (bridgeBStalled == 0);

            // STEP_LOSS_A is bit 13, active low
            byte bridgeAStalled = (byte)(StatusValue >> 13 & 1);
            BridgeAStalled = (bridgeAStalled == 0);

            // OCD is bit 12, active low
            byte overcurrentDetected = (byte)(StatusValue >> 12 & 1);
            OvercurrentDetected = (overcurrentDetected == 0);

            // TH_SD is bit 11, active low
            byte thermalShutdown = (byte)(StatusValue >> 11 & 1);
            ThermalShutdownTriggered = (thermalShutdown == 0);

            // TH_WRN is bit 10, active low
            byte thermalWarning = (byte)(StatusValue >> 10 & 1);
            ThermalWarningTriggered = (thermalWarning == 0);

            // UVLO is bit 9, active low
            byte undervoltage = (byte)(StatusValue >> 9 & 1);
            UndervoltageDetected = (undervoltage == 0);

            // WRONG_CMD is bit 8, active high
            byte wrongCommand = (byte)(StatusValue >> 8 & 1);
            ReceivedUnknownCommand = (wrongCommand == 1);

            // NOTPERF_CMD is bit 7, active high
            byte commandFailed = (byte)(StatusValue >> 7 & 1);
            LastCommandFailed = (commandFailed == 1);

            // MOT_STATUS is bits 6 and 5
            byte motorStatus = (byte)(StatusValue >> 5 & 0b11);
            MotorState = (MotorActivity)(motorStatus);

            // DIR is bit 4
            byte direction = (byte)(StatusValue >> 4 & 1);
            Direction = (MotorDirection)direction;

            // SW_EVN is bit 3, active high
            byte killSwitchTriggered = (byte)(StatusValue >> 3 & 1);
            KillSwitchTriggered = (killSwitchTriggered == 1);

            // SW_F is bit 2, low if the switch is open, high if the switch is closed
            byte killSwitchActive = (byte)(StatusValue >> 2 & 1);
            KillSwitchActive = (killSwitchActive == 1);

            // BUSY is bit 1, active low
            byte isBusy = (byte)(StatusValue >> 1 & 1);
            IsBusy = (isBusy == 0);

            // HiZ is bit 0, active high but we want the opposite since HiZ means
            // "bridges disabled"
            byte hiZ = (byte)(StatusValue & 1);
            BridgesActive = (hiZ == 0);
        }
    }


    /// <summary>
    /// This class represents an ST L6470 Autodriver that's connected to a stepper motor.
    /// </summary>
    /// <remarks>
    /// The datasheet for this chip is very long and elaborate. You can find it here:
    /// https://www.st.com/resource/en/datasheet/l6470.pdf
    /// This application note (AN3980) is also very helpful:
    /// https://www.st.com/resource/en/application_note/dm00037891.pdf
    /// </remarks>
    internal class L6470 : IDisposable
    {
        /// <summary>
        /// The underlying SPI device used to communicate with the L6470
        /// </summary>
        private readonly SpiDevice Spi;


        /// <summary>
        /// The angle that a single motor step moves the motor shaft, in degrees
        /// </summary>
        private readonly double StepAngle;


        /// <summary>
        /// The motor's speed, in RPM
        /// </summary>
        public uint Speed { get; set; }


        /// <summary>
        /// The motor's direction
        /// </summary>
        public MotorDirection Direction { get; set; }


        /// <summary>
        /// Creates a new <see cref="L6470"/> instance.
        /// </summary>
        /// <param name="ChipSelectPin">The number of the chip select pin for this device,
        /// using the WiringPi numbering scheme.</param>
        /// <param name="StepAngle">The angle that a single motor step moves the motor shaft,
        /// in degrees</param>
        /// <param name="MaxCurrent">The max current the motor can sustain, per coil, in amps.</param>
        public L6470(byte ChipSelectPin, double StepAngle, double MaxCurrent)
        {
            Spi = new SpiDevice(ChipSelectPin, 4000000, SpiMode.Mode3, 1, 0, 1, 1);
            this.StepAngle = StepAngle;

            // Set the overcurrent threshold
            byte overcurrentTheshold = GetClosestOvercurrentThreshold(MaxCurrent);
            SetParam_8Bit(Register.OvercurrentThreshold, overcurrentTheshold);

            // Default to 16 microsteps: SYNC_EN = 0, SYNC_SEL = 0, STEP_SEL = 4
            byte stepMode = (byte)(MicrostepMode._16PerStep);
            SetParam_8Bit(Register.StepMode, stepMode);

            // Default to a max speed of as high as it goes, I guess...
            ushort maxSpeed = 0x03FF;
            SetParam_16Bit(Register.MaximumSpeed, maxSpeed);

            // Set the full-step speed (the threshold at which it disables microstepping)
            // to a default of 300 RPM. That seems like a lot so it's probably a good
            // starting point.
            ushort fullStepSpeed = GetFormattedFullStepSpeed(300.0);
            SetParam_16Bit(Register.FullStepSpeed, fullStepSpeed);

            // Set the acceleration and deceleration to 500 RPM/sec.
            ushort acceleration = GetFormattedAcceleration(500.0);
            SetParam_16Bit(Register.Acceleration, acceleration);
            SetParam_16Bit(Register.Deceleration, acceleration);

            // Set the config register to the following defaults:
            // OSC_SEL and EXT_CLK = 0000; 16 MHz internal clock, no output
            // SW_MODE = 0; hard stop when the kill switch is thrown
            // EN_VSCOMP = 1; enabled
            // OC_SD = 1; enabled
            // POW_SR = 11; 530 V/us for maximum torque
            // F_PWM_INT and F_PWM_DEC = 000111; 62.5 kHz PWM output
            ushort config = 0;
            config |= (1 << 5); // Set EN_VSCOMP to 1
            config |= (1 << 7); // Set OC_SD to 1
            config |= (0b11 << 8); // Set POW_SR to 530 V/us
            config |= ((byte)PwmFrequency._62_500 << 10);
            SetParam_16Bit(Register.Configuration, config);
        }


        /// <summary>
        /// Determines the index of the closest overcurrent threshold setting to the provided
        /// max current.
        /// </summary>
        /// <param name="MaxMotorCurrent">The max current the motor can sustain, per coil, in amps.</param>
        /// <returns>The index of the overcurrent threshold setting that best matches this current</returns>
        private static byte GetClosestOvercurrentThreshold(double MaxMotorCurrent)
        {
            // If this motor has a current lower than 375mA, the L6470 can't really guard
            // against overcurrents so this isn't really a viable usecase but we'll set it
            // to the lowest legal value anyway.
            if (MaxMotorCurrent < 0.375)
            {
                return 0;
            }

            // The L6470 has 16 overcurrent settings, starting with 375mA and going up
            // to 6000mA in 375mA increments. This finds the highest one that doesn't
            // exceed the motor current.
            for (int i = 0; i < 16; i++)
            {
                double overcurrentSetting = 0.375 * (i + 1);
                if (overcurrentSetting > MaxMotorCurrent)
                {
                    return (byte)(i - 1);
                }
            }

            // If the motor takes over 6 amps, the L6470 can't even drive it to its full
            // potential so this is going to screw it up but return the max setting anyway.
            return 15;
        }

        #region Commands

        /// <summary>
        /// Gets the status of the device.
        /// </summary>
        /// <returns>The device status</returns>
        public L6470Status GetStatus()
        {
            byte[] buffer = { 0b11010000, 0, 0 };
            Spi.TransferData(buffer);

            ushort status = 0;
            status |= (ushort)(buffer[1] << 8);
            status |= buffer[2];

            return new L6470Status(status);
        }


        /// <summary>
        /// Sets the microstepping mode. This can only be done when the motor isn't running
        /// and the coils are disabled, so this will perform a soft HiZ first.
        /// </summary>
        /// <param name="Mode">The new microstepping mode</param>
        public void SetMicrostepMode(MicrostepMode Mode)
        {
            SoftHiZ();

            // Wait for the HiZ finish
            L6470Status status = GetStatus();
            while(status.IsBusy || status.BridgesActive)
            {
                Thread.Sleep(10);
                status = GetStatus();
            }

            // Set the new microstepping mode, leaving SYNC_EN and SYNC_SEL to 0
            SetParam_8Bit(Register.StepMode, (byte)Mode);
        }


        /// <summary>
        /// Runs the motor using the <see cref="Speed"/> and <see cref="Direction"/>
        /// properties.
        /// </summary>
        public void Run()
        {
            uint formattedSpeed = GetFormattedSpeed(Speed);
            byte command = 0b01010000;
            if(Direction == MotorDirection.Forward)
            {
                // The direction is encoded in the last bit of the command byte,
                // reverse = 0, forward = 1
                command |= 1;
            }

            byte[] buffer = { command, 0, 0, 0 };
            buffer[1] = (byte)(formattedSpeed >> 16);
            buffer[2] = (byte)(formattedSpeed >> 8);
            buffer[3] = (byte)formattedSpeed;

            Spi.TransferData(buffer);
        }


        /// <summary>
        /// Slowly stops the motor, using the gradual deceleration curve.
        /// </summary>
        public void SoftStop()
        {
            byte[] buffer = { 0b10110000 };
            Spi.TransferData(buffer);
        }


        /// <summary>
        /// Immediately stops the motor, ignoring the deceleration curve,
        /// but keeps the coils engaged (holding).
        /// </summary>
        public void HardStop()
        {
            byte[] buffer = { 0b10111000 };
            Spi.TransferData(buffer);
        }


        /// <summary>
        /// Gracefully stops the motor using the gradual deceleration curve,
        /// then disengages the coils.
        /// </summary>
        public void SoftHiZ()
        {
            byte[] buffer = { 0b10100000 };
            Spi.TransferData(buffer);
        }


        /// <summary>
        /// Immediately stops the motor, ignoring the deceleration curve,
        /// and disenages the coils. This is like an "EMERGENCY STOP!" function.
        /// </summary>
        public void HardHiZ()
        {
            byte[] buffer = { 0b10101000 };
            Spi.TransferData(buffer);
        }

        #endregion


        #region Parameter Getters and Setters

        /// <summary>
        /// Gets the value of a register that has a size of 8 or fewer bits.
        /// </summary>
        /// <param name="Register">The register to get the value of</param>
        /// <returns>The value of the register</returns>
        private byte GetParam_8Bit(Register Register)
        {
            // Create the GetParam command (001, then the 5 register bits)
            byte command = (byte)(0b00100000 | (byte)Register);

            // Make the SPI buffer - the first byte is the command, the rest are for the response
            byte[] buffer = { command, 0 };

            // Run the SPI transfer and return the response byte
            Spi.TransferData(buffer);
            return buffer[1];
        }


        /// <summary>
        /// Gets the value of a register that has a size of 16 or fewer bits.
        /// </summary>
        /// <param name="Register">The register to get the value of</param>
        /// <returns>The value of the register</returns>
        private ushort GetParam_16Bit(Register Register)
        {
            // Create the GetParam command (001, then the 5 register bits)
            byte command = (byte)(0b00100000 | (byte)Register);

            // Make the SPI buffer - the first byte is the command, the rest are for the response
            byte[] buffer = { command, 0, 0 };

            // Run the SPI transfer
            Spi.TransferData(buffer);

            // Build a short from the response and return it
            ushort response = 0;
            response |= (ushort)(buffer[1] << 8);
            response |= buffer[2];
            return response;
        }


        /// <summary>
        /// Gets the value of a register that has a size of 24 or fewer bits.
        /// </summary>
        /// <param name="Register">The register to get the value of</param>
        /// <returns>The value of the register</returns>
        private uint GetParam_24Bit(Register Register)
        {
            // Create the GetParam command (001, then the 5 register bits)
            byte command = (byte)(0b00100000 | (byte)Register);

            // Make the SPI buffer - the first byte is the command, the rest are for the response
            byte[] buffer = { command, 0, 0, 0 };

            // Run the SPI transfer
            Spi.TransferData(buffer);

            // Build an int from the response and return it
            uint response = 0;
            response |= (uint)(buffer[1] << 16);
            response |= (uint)(buffer[2] << 8);
            response |= buffer[3];
            return response;
        }


        /// <summary>
        /// Sets the value of a register that has a size of 8 or fewer bits.
        /// </summary>
        /// <param name="Register">The register to set the value of</param>
        /// <param name="Value">The new value of the register</param>
        private void SetParam_8Bit(Register Register, byte Value)
        {
            // Create the SetParam command (000, then the 5 register bits)
            byte command = (byte)Register;

            // Make the SPI buffer and run the SPI transfer
            byte[] buffer = { command, Value };
            Spi.TransferData(buffer);
        }


        /// <summary>
        /// Sets the value of a register that has a size of 16 or fewer bits.
        /// </summary>
        /// <param name="Register">The register to set the value of</param>
        /// <param name="Value">The new value of the register</param>
        private void SetParam_16Bit(Register Register, ushort Value)
        {
            // Create the SetParam command (000, then the 5 register bits)
            byte command = (byte)Register;

            // Make the SPI buffer and run the SPI transfer
            byte[] buffer = { command, 0, 0 };
            buffer[1] = (byte)(Value >> 8);
            buffer[2] = (byte)(Value);
            Spi.TransferData(buffer);
        }


        /// <summary>
        /// Sets the value of a register that has a size of 24 or fewer bits.
        /// </summary>
        /// <param name="Register">The register to set the value of</param>
        /// <param name="Value">The new value of the register</param>
        private void SetParam_24Bit(Register Register, uint Value)
        {
            // Create the SetParam command (000, then the 5 register bits)
            byte command = (byte)Register;

            // Make the SPI buffer and run the SPI transfer
            byte[] buffer = { command, 0, 0, 0 };
            buffer[1] = (byte)(Value >> 16);
            buffer[2] = (byte)(Value >> 8);
            buffer[3] = (byte)(Value);
            Spi.TransferData(buffer);
        }

        #endregion

        #region Formatting and Conversion

        /// <summary>
        /// Converts an RPM value into a form that can be used for the FS_SPD register.
        /// </summary>
        /// <param name="RPM">The RPM being converted</param>
        /// <returns>The RPM in FS_SPD-compatible form</returns>
        /// <remarks>
        /// The formula for this conversion comes from AN3980, page 13
        /// </remarks>
        private ushort GetFormattedFullStepSpeed(double RPM)
        {
            double stepsPerRev = 360.0 / StepAngle;
            double stepsPerSecond = RPM / 60.0 * stepsPerRev;
            double formattedSpeed = Math.Round(stepsPerSecond * 0.065536);

            // Clamp the value to the lowest 10-bits since the FS_SPD register takes
            // a 10-bit value
            if (formattedSpeed > 0x03FF)
            {
                return 0x03FF;
            }
            return (ushort)formattedSpeed;
        }


        /// <summary>
        /// Converts an RPM/s value into a form that can be used for the ACC/DEC registers.
        /// </summary>
        /// <param name="RPMPerSecond">The RPM/s being converted</param>
        /// <returns>The RPM/s in ACC/DEC-compatible form</returns>
        /// <remarks>
        /// The formula for this conversion comes from AN3980, page 13
        /// </remarks>
        private ushort GetFormattedAcceleration(double RPMPerSecond)
        {
            double stepsPerRev = 360.0 / StepAngle;
            double stepsPerSecond_2 = RPMPerSecond / 60.0 * stepsPerRev;
            double formattedAcceleration = Math.Round(stepsPerSecond_2 * 0.068719476736 + 0.5);

            // Clamp the value to the lowest 12-bits since the ACC / DEC registers
            // take 12-bit values
            if (formattedAcceleration > 0x0FFF)
            {
                return 0x0FFF;
            }
            return (ushort)formattedAcceleration;
        }


        /// <summary>
        /// Converts an RPM value into a form that can be used as input to a RUN command.
        /// </summary>
        /// <param name="RPM">The RPM being converted</param>
        /// <returns>The RPM in RUN-compatible form</returns>
        /// <remarks>
        /// The formula for this conversion comes from AN3980, page 13
        /// </remarks>
        private uint GetFormattedSpeed(double RPM)
        {
            double stepsPerRev = 360.0 / StepAngle;
            double stepsPerSecond = RPM / 60.0 * stepsPerRev;
            double formattedSpeed = Math.Round(stepsPerSecond * 67.108864 + 0.5);

            // Clamp the value to the lowest 20-bits since the SPD setting
            // takes 20-bit values
            if (formattedSpeed > 0xFFFFF)
            {
                return 0xFFFFF;
            }
            return (uint)formattedSpeed;
        }

        #endregion

        #region IDisposable Support
        private bool DisposedValue = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!DisposedValue)
            {
                if (disposing)
                {
                    Spi.Dispose();
                }
                
                DisposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }
        #endregion


    }
}
