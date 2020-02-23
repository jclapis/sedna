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

#pragma once

#include <stdint.h>
#include "Spi.h"

namespace Sedna
{
    /// <summary>
    /// This represents the options for the overcurrent threshold setting. It corresponds
    /// to the OCD_TH register.
    /// </summary>
    enum class OvercurrentThreshold : uint8_t
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
    enum class MicrostepMode : uint8_t
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
    /// This defines the options for the slew rate for the power bridge output, in volts per
    /// microsecond. It corresponds to the POW_SR bits of the CONFIG register.
    /// </summary>
    enum class PowerBridgeSlewRate : uint8_t
    {
        /// <summary>
        /// 180 V/us
        /// </summary>
        _180 = 0x00,

        /// <summary>
        /// 290 V/us
        /// </summary>
        _290 = 0x02,

        /// <summary>
        /// 530 V/us
        /// </summary>
        _530 = 0x03
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
    enum class PwmFrequency : uint8_t
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

    enum class MotorDirection : uint8_t
    {
        Reverse,
        Forward
    };


    enum class MotorStatus : uint8_t
    {
        Stopped,
        Accelerating,
        Decelerating,
        ConstantSpeed
    };


    struct Status
    {
        bool BridgesEnabled;

        bool IsBusy;

        bool KillSwitchActive;

        MotorDirection Direction;

        MotorStatus MotorMode;

        bool LowVoltageAlarm;

        bool ThermalWarningAlarm;

        bool ThermalShutdownAlarm;

        bool OvercurrentAlarm;

        bool BridgeAStalled;

        bool BridgeBStalled;
    };


    enum class Register : uint8_t
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


    /// <summary>
    /// This class represents an ST L6470 Autodriver that's connected to a stepper motor.
    /// </summary>
    /// <remarks>
    /// The datasheet for this chip is very long and elaborate. You can find it here:
    /// https://www.st.com/resource/en/datasheet/l6470.pdf
    /// This application note (AN3980) is also very helpful:
    /// https://www.st.com/resource/en/application_note/dm00037891.pdf
    /// </remarks>
    class L6470
    {
    public:
        L6470(uint8_t ChipSelectPin, float StepAngle, float MaxCurrent);

        Status GetStatus();

        void SetPwmFrequency(PwmFrequency Frequency);
        
        void SetOvercurrentThreshold(OvercurrentThreshold Threshold);
        
        void SetMicrostepMode(MicrostepMode Mode);
        
        void SetFullStepSpeedThreshold(float RPM);
        
        void SetSpeed(uint32_t Speed);
        
        void SetDirection(MotorDirection Direction);
        
        void Run();
        
        void SoftStop();
        
        void HardStop();
        
        void SoftHiZ();
        
        void HardHiZ();

    private:
        float StepAngle;

        uint32_t Speed;

        MotorDirection Direction;

        SpiDevice Spi;

        uint8_t GetParam_8Bit(Register Register);
        
        uint16_t GetParam_16Bit(Register Register);
        
        uint32_t GetParam_24Bit(Register Register);

        void SetParam_8Bit(Register Register, uint8_t Value);

        void SetParam_16Bit(Register Register, uint16_t Value);
        
        void SetParam_24Bit(Register Register, uint32_t Value);

        uint8_t GetClosestOvercurrentThreshold(float MaxMotorCurrent);

        uint16_t GetFormattedFullStepSpeed(float RPM);

        uint16_t GetFormattedAcceleration(float RPMPerSecond);
    };
}