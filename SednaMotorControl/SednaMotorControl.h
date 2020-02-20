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
extern "C"
{
#endif

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


    int CreateDevice(SpiDeviceType DeviceType, unsigned char ChipSelectPin, void** Driver);

    int FreeDevice(void* Device);





#ifdef __cplusplus
}
#endif