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

extern "C"
{
    /// <summary>
    /// Defines the type of a SPI device.
    /// </summary>
    enum SpiDeviceType
    {
        /// <summary>
        /// An L6470 autodriver 
        /// </summary>
        SpiDeviceType_L6470 = 0,

        /// <summary>
        /// An AMT22 rotary encoder
        /// </summary>
        SpiDeviceType_Amt22 = 1
    };


    /// <summary>
    /// Initializes the motor control system. This must be called before anything else.
    /// </summary>
    /// <returns>0 if successful, -1 if an error occurred.</returns>
    int Initialize();


    /// <summary>
    /// Returns a string with a human-readable message defining the last error that occurred.
    /// </summary>
    /// <returns>A string taht describes the last error.</returns>
    const char* GetLastError();


    /// <summary>
    /// Creates a handle to an AMT22 rotary encoder.
    /// </summary>
    /// <param name="ChipSelectPin">The number of the chip select pin for this device.
    /// This uses the WiringPi pin layout. To find what the pin number is for a given
    /// physical pin, run `gpio readall` on your Pi.</param>
    /// <param name="Device">[OUT] A handle to the new device.</param>
    /// <returns>0 if this was successful, -1 if an error occurred. Use GetLastError() to
    /// see what went wrong.</returns>
    int CreateAmt22(unsigned char ChipSelectPin, void** Device);


    /// <summary>
    /// Frees an AMT22 device.
    /// </summary>
    /// <param name="Device">The handle of the device to free.</param>
    void FreeAmt22(void* Device);

}