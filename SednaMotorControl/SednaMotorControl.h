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

#ifdef __cplusplus
extern "C" {
#endif

    /// <summary>
    /// This represents the settings for a specific device on the SPI bus.
    /// </summary>
    struct SpiDevice
    {
        /// <summary>
        /// The number of the chip select pin for this device. This uses the
        /// WiringPi pin layout. To find what the pin number is for a given
        /// physical pin, run `gpio readall` on your Pi.
        /// </summary>
        unsigned char ChipSelectPin;


        /// <summary>
        /// The speed of the SPI communications for this device, in Hz.
        /// </summary>
        unsigned int BitRate;


        /// <summary>
        /// The SPI mode that this device uses
        /// </summary>
        unsigned char SpiMode;


        /// <summary>
        /// The delay (in microseconds) between pulling the chip select pin low and
        /// starting to read/write data from/to the device.
        /// </summary>
        unsigned char TimeBeforeRead;


        /// <summary>
        /// The delay (in microseconds) to wait between reading and writing individual bytes.
        /// Some devices have specific timings for this; consult your device's datasheet.
        /// </summary>
        unsigned char TimeBetweenBytes;


        /// <summary>
        /// The delay (in microseconds) to wait after finishing a data read/write before setting
        /// the chip select pin back to high (unselecting the device).
        /// </summary>
        unsigned char TimeAfterRead;


        /// <summary>
        /// The delay (in microseconds) to wait after unselecting a device before it can be 
        /// selected again for a subsequent read/write.
        /// </summary>
        unsigned char TimeBetweenReads;
    };


    /// <summary>
    /// Returns a string with a human-readable message defining the last error that occurred.
    /// </summary>
    /// <returns>A string taht describes the last error.</returns>
    const char* GetLastError();


    /// <summary>
    /// Initializes the motor control system.
    /// </summary>
    /// <returns>0 if successful, -1 if an error occurred.</returns>
    int Initialize();


    /// <summary>
    /// Initializes a SPI device so it can be used.
    /// </summary>
    void InitializeSpiDevice(SpiDevice* Device);



#ifdef __cplusplus
}
#endif