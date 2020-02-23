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
#include "Amt22.h"
#include "L6470.h"

namespace Sedna
{
    /// <summary>
    /// This class defines a single motor assembly, which consists of a stepper motor
    /// driven by an L6470 and attached to an AMT22x-B 14-bit rotary encoder for absolute
    /// position information.
    /// </summary>
    class MotorAssembly
    {
    public:
        /// <summary>
        /// Creates a new MotorAssembly instance.
        /// </summary>
        /// <param name="MotorSelectPin">The number of the SPI chip select pin for the L6470,
        /// using the WiringPI pin layout.</param>
        /// <param name="EncoderSelectPin">The number of the SPI chip select pin for the AMT22,
        /// using the WiringPI pin layout.</param>
        /// <param name="StepAngle">The angle for a single step of the motor. This will come
        /// from the motor's spec sheet.</param>
        /// <param name="MaxCurrent">The maximum amount of current, in Amps, that the motor can
        /// tolerate before declaring an overcurrent event.</param>
        MotorAssembly(
            uint8_t MotorSelectPin,
            uint8_t EncoderSelectPin,
            float StepAngle,
            float MaxCurrent);

    private:
        Amt22 Encoder;
        L6470 MotorDriver;
    };
}