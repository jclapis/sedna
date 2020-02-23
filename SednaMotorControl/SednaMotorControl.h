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

extern "C"
{
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
    /// <param name="MotorAssembly">[OUT] A handle to the new motor assembly.</param>
    int CreateMotorAssembly(
        uint8_t MotorSelectPin,
        uint8_t EncoderSelectPin,
        float StepAngle,
        float MaxCurrent,
        void** MotorAssembly);


    /// <summary>
    /// Deletes a motor assembly, freeing its memory.
    /// </summary>
    /// <param name="Assembly">The motor assembly to free. This must have been created by
    /// the CreateMotorAssembly function.</param>
    void FreeMotorAssembly(void* Assembly);

}