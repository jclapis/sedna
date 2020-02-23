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
 
using System;
using System.Collections.Generic;
using System.Text;

namespace Sedna
{
    /// <summary>
    /// This class represents an assembly of an L6470, a stepper motor, and an AMT22.
    /// </summary>
    public class MotorAssembly : IDisposable
    {
        /// <summary>
        /// The rotary encoder coupled to the assembly
        /// </summary>
        private readonly Amt22 Encoder;


        /// <summary>
        /// The motor driver
        /// </summary>
        private readonly L6470 Driver;


        /// <summary>
        /// Creates a new <see cref="MotorAssembly"/> instance.
        /// </summary>
        /// <param name="MotorSelectPin">The number of the SPI chip select pin used for the L6470.
        /// This uses the WiringPi numbering scheme.</param>
        /// <param name="EncoderSelectPin">The number of the SPI chip select pin used for the AMT22.
        /// This uses the WiringPi numbering scheme.</param>
        /// <param name="StepAngle">The angle that a single motor step moves the motor shaft,
        /// in degrees</param>
        /// <param name="MaxCurrent">The max current the motor can sustain, per coil, in amps.</param>
        public MotorAssembly(
            byte MotorSelectPin, 
            byte EncoderSelectPin,
            double StepAngle,
            double MaxCurrent)
        {
            Encoder = new Amt22(EncoderSelectPin);
            Driver = new L6470(MotorSelectPin, StepAngle, MaxCurrent);
        }


        /// <summary>
        /// The current position of the motor assembly, from 0 to 1. 
        /// </summary>
        public ushort Position
        {
            get
            {
                return Encoder.GetPosition();
            }
        }


        /// <summary>
        /// The current, in amps, that the motor is currently pulling.
        /// </summary>
        public double Current
        {
            get
            {
                return -1; // NYI
            }
        }


        #region IDisposable Support
        private bool DisposedValue = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!DisposedValue)
            {
                if (disposing)
                {
                    Encoder.Dispose();
                    Driver.Dispose();
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
