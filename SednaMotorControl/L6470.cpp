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

#include "L6470.h"
#include <limits>

namespace Sedna
{
    L6470::L6470(uint8_t ChipSelectPin, float StepAngle, float MaxCurrent)
        : Spi(ChipSelectPin, 4000000, SpiMode::Mode3, 1, 0, 1, 1)
    {
        // Set the overcurrent threshold
        uint8_t overcurrentTheshold = GetClosestOvercurrentThreshold(MaxCurrent);
        SetParam_8Bit(Register::OvercurrentThreshold, overcurrentTheshold);

        // Default to 16 microsteps: SYNC_EN = 0, SYNC_SEL = 0, STEP_SEL = 4
        uint8_t stepMode = static_cast<uint8_t>(MicrostepMode::_16PerStep);
        SetParam_8Bit(Register::StepMode, stepMode);

        // Default to a max speed of as high as it goes, I guess...
        uint16_t maxSpeed = 0x03FF;
        SetParam_16Bit(Register::MaximumSpeed, maxSpeed);

        // Set the full-step speed (the threshold at which it disables microstepping)
        // to a default of 300 RPM. That seems like a lot so it's probably a good
        // starting point.
        uint16_t fullStepSpeed = GetFormattedFullStepSpeed(300.0);
        SetParam_16Bit(Register::FullStepSpeed, fullStepSpeed);

        // Set the acceleration and deceleration to 500 RPM/sec.
        uint16_t acceleration = GetFormattedAcceleration(500.0);
        SetParam_16Bit(Register::Acceleration, acceleration);
        SetParam_16Bit(Register::Deceleration, acceleration);

        // Set the config register to the following defaults:
        // OSC_SEL and EXT_CLK = 0000; 16 MHz internal clock, no output
        // SW_MODE = 0; hard stop when the kill switch is thrown
        // EN_VSCOMP = 1; enabled
        // OC_SD = 1; enabled
        // POW_SR = 11; 530 V/us for maximum torque
        // F_PWM_INT and F_PWM_DEC = 000111; 62.5 kHz PWM output
        uint16_t config = 0;
        config |= (1 << 5); // Set EN_VSCOMP to 1
        config |= (1 << 7); // Set OC_SD to 1
        config |= (0b11 << 8); // Set POW_SR to 530 V/us
        config |= (static_cast<uint16_t>(PwmFrequency::_62_500) << 10);
        SetParam_16Bit(Register::Configuration, config);
    }


    uint8_t L6470::GetClosestOvercurrentThreshold(float MaxMotorCurrent)
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
            float overcurrentSetting = 0.375 * (i + 1);
            if (overcurrentSetting > MaxMotorCurrent)
            {
                return i - 1;
            }
        }

        // If the motor takes over 6 amps, the L6470 can't even drive it to its full
        // potential so this is going to screw it up but return the max setting anyway.
        return 15;
    }


    // =====================================
    // === Parameter Getters and Setters ===
    // =====================================


    uint8_t L6470::GetParam_8Bit(Register Register)
    {
        // Create the GetParam command (001, then the 5 register bits)
        uint8_t command = 0b00100000 | static_cast<uint8_t>(Register);

        // Make the SPI buffer - the first byte is the command, the rest are for the response
        uint8_t buffer[] = { command, 0x00 };

        // Run the SPI transfer and return the response byte
        Spi.TransferData(buffer, 2);
        return buffer[1];
    }


    uint16_t L6470::GetParam_16Bit(Register Register)
    {
        // Create the GetParam command (001, then the 5 register bits)
        uint8_t command = 0b00100000 | static_cast<uint8_t>(Register);

        // Make the SPI buffer - the first byte is the command, the rest are for the response
        uint8_t buffer[] = { command, 0x00, 0x00 };

        // Run the SPI transfer
        Spi.TransferData(buffer, 3);

        // Build a short from the response and return it
        uint16_t response = 0;
        response |= (buffer[1] << 8);
        response |= buffer[2];
        return response;
    }


    uint32_t L6470::GetParam_24Bit(Register Register)
    {
        // Create the GetParam command (001, then the 5 register bits)
        uint8_t command = 0b00100000 | static_cast<uint8_t>(Register);

        // Make the SPI buffer - the first byte is the command, the rest are for the response
        uint8_t buffer[] = { command, 0x00, 0x00, 0x00 };

        // Run the SPI transfer
        Spi.TransferData(buffer, 4);

        // Build an int from the response and return it
        uint32_t response = 0;
        response |= (buffer[1] << 16);
        response |= (buffer[2] << 8);
        response |= buffer[3];
        return response;
    }


    void L6470::SetParam_8Bit(Register Register, uint8_t Value)
    {
        // Create the SetParam command (000, then the 5 register bits)
        uint8_t command = static_cast<uint8_t>(Register);

        // Make the SPI buffer and run the SPI transfer
        uint8_t buffer[] = { command, Value };
        Spi.TransferData(buffer, 2);
    }


    void L6470::SetParam_16Bit(Register Register, uint16_t Value)
    {
        // Create the SetParam command (000, then the 5 register bits)
        uint8_t command = static_cast<uint8_t>(Register);

        // Make the SPI buffer and run the SPI transfer
        uint8_t buffer[] = { command, 0x00, 0x00 };
        buffer[1] = static_cast<uint8_t>(Value >> 8);
        buffer[2] = static_cast<uint8_t>(Value);
        Spi.TransferData(buffer, 3);
    }


    void L6470::SetParam_24Bit(Register Register, uint32_t Value)
    {
        // Create the SetParam command (000, then the 5 register bits)
        uint8_t command = static_cast<uint8_t>(Register);

        // Make the SPI buffer and run the SPI transfer
        uint8_t buffer[] = { command, 0x00, 0x00, 0x00 };
        buffer[1] = static_cast<uint8_t>(Value >> 16);
        buffer[2] = static_cast<uint8_t>(Value >> 8);
        buffer[3] = static_cast<uint8_t>(Value);
        Spi.TransferData(buffer, 4);
    }


    // =====================================
    // === Parameter Getters and Setters ===
    // =====================================


    // The formula for this conversion comes from AN3980, page 13
    uint16_t L6470::GetFormattedFullStepSpeed(float RPM)
    {
        float stepsPerRev = 360.0 / StepAngle;
        float stepsPerSecond = RPM / 60.0 * stepsPerRev;
        float formattedSpeed = stepsPerSecond * 0.065536;

        // Clamp the value to the lowest 10-bits since the FS_SPD register takes
        // a 10-bit value
        if (formattedSpeed > 0x03FF)
        {
            return 0x03FF;
        }
        return static_cast<uint16_t>(formattedSpeed);
    }


    // The formula for this conversion comes from AN3980, page 13
    uint16_t L6470::GetFormattedAcceleration(float RPMPerSecond)
    {
        float stepsPerRev = 360.0 / StepAngle;
        float stepsPerSecond_2 = RPMPerSecond / 60.0 * stepsPerRev;
        float formattedAcceleration = stepsPerSecond_2 * 0.068719476736 + 0.5;

        // Clamp the value to the lowest 12-bits since the ACC / DEC registers
        // take 12-bit values
        if (formattedAcceleration > 0x0FFF)
        {
            return 0x0FFF;
        }
        return static_cast<uint16_t>(formattedAcceleration);
    }

}