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

#include "Spi.h"


namespace Sedna
{
    /// <summary>
    /// This represents an AMT22 rotary encoder. Please see the datasheet for details:
    /// https://www.cuidevices.com/product/resource/amt22.pdf
    /// </summary>
    class Amt22
    {
    public:
        /// <summary>
        /// Creates a new Amt22 instance.
        /// </summary>
        /// <param name="ChipSelectPin">The number of the chip select pin for this device.</param>
        Amt22(unsigned char ChipSelectPin);


        /// <summary>
        /// Gets the current position of the encoder's shaft. For 12-bit devices, this ranges from
        /// 0 to 4095. For 14-bit devices, this ranges from 0 to 16383.
        /// </summary>
        /// <returns>The position of the shaft the encoder is coupled to.</returns>
        unsigned short GetPosition();


        /// <summary>
        /// Resets the encoder. The encoder must be stationary in order to power back on.
        /// </summary>
        void Reset();


        /// <summary>
        /// Sets the current position as the new zero position, then resets the device.
        /// The encoder must be stationary for this command.
        /// </summary>
        void SetZeroPosition();


    private:
        /// <summary>
        /// The underlying SPI device used to communicate with the AMT22
        /// </summary>
        SpiDevice Spi;


        /// <summary>
        /// The device's startup and reset time, in microseconds
        /// </summary>
        int StartupTime;


        /// <summary>
        /// Validates that the data returned by the device passes its checksum test.
        /// </summary>
        /// <param name="Buffer">The data return by the device (2 bytes)</param>
        void ValidateChecksum(unsigned char* Buffer);
    };
}