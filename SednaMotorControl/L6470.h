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

#include <vector>

namespace Sedna
{
    /// <summary>
    /// This represents the options for the overcurrent threshold setting. It corresponds
    /// to the OCD_TH register.
    /// </summary>
    enum class OvercurrentThreshold : unsigned char
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
    enum class MicrostepMode : unsigned char
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
    enum class PowerBridgeSlewRate : unsigned char
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
    enum class PwmFrequency : unsigned short
    {
        _2_790 = 0b110000,
        _3_255 = 0b101000,
        _3_348 = 0b110001,
        _3_906 = 0b100000,
        _4_464 = 0b110011,
        _4_557 = 0b101010,
        _4_688 = 0b100001,
        _4_883 = 0b011000,
        _5_208 = 0b101011,
        _5_469 = 0b100010,
        _5_580 = 0b110100,
        _5_859 = 0b011001,
        _6_250 = 0b100011,
        _6_510 = 0b010000,
        _6_696 = 0b110101,
        _6_836 = 0b011010,
        _7_813 = 0b010001,
        _8_929 = 0b110111,
        _9_115 = 0b010010,
        _9_375 = 0b100101,
        _9_766 = 0b001000,
        _10_417 = 0b010011,
        _10_938 = 0b100110,
        _11_719 = 0b001001,
        _12_500 = 0b100111,
        _13_021 = 0b010100,
        _13_672 = 0b001010,
        _15_625 = 0b001011,
        _18_229 = 0b010110,
        _19_531 = 0b000000,
        _20_833 = 0b010111,
        _23_438 = 0b000001,
        _27_344 = 0b000010,
        _31_250 = 0b000011,
        _39_063 = 0b000100,
        _46_875 = 0b000101,
        _54_688 = 0b000110,
        _62_500 = 0b000111
    };



    /// <summary>
    /// 
    /// </summary>
    class L6470
    {
    public:

    private:
    };
}