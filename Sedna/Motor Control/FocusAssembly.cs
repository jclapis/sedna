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
    public class FocusAssembly
    {
        /// <summary>
        /// The logger for capturing debug and info messages
        /// </summary>
        private readonly Logger Logger;


        /// <summary>
        /// The rotary encoder coupled to the assembly
        /// </summary>
        private readonly Amt22 Encoder;


        /// <summary>
        /// The motor driver
        /// </summary>
        private readonly L6470 Driver;


        /// <summary>
        /// The lower bound of the focuser (the lowest value the rotary encoder
        /// is allowed to reach before shutting the motors off)
        /// </summary>
        private readonly int LowerEncoderBound;


        /// <summary>
        /// The upper bound of the focuser (the highest value the rotary encoder
        /// is allowed to reach before shutting the motors off)
        /// </summary>
        private readonly int UpperEncoderBound;


        public MicrostepMode MicrostepMode
        {
            get
            {
                Driver
            }
            set
            {

            }
        }


        public double Position
        {
            get
            {

            }
        }


        public int EncoderUpdateRate { get; set; }

        public FocusAssembly(
            Logger Logger,
            byte MotorSelectPin,
            byte EncoderSelectPin,
            double StepAngle,
            double MaxCurrent,
            int LowerEncoderBound,
            int UpperEncoderBound,
            int EncoderUpdateRate = 50)
        {
            Encoder = new Amt22(EncoderSelectPin, Logger);
            Driver = new L6470(MotorSelectPin, StepAngle, MaxCurrent);
            this.LowerEncoderBound = LowerEncoderBound;
            this.UpperEncoderBound = UpperEncoderBound;
        }


        public void MoveToPosition(double Position, double MaxRPM = 3)
        {

        }
    }
}
