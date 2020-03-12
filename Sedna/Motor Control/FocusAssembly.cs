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
using System.Threading;
using System.Threading.Tasks;

namespace Sedna
{
    public class FocusMoveResult : EventArgs
    {
        public bool Succeeded { get; }

        public string ErrorMessage { get; }

        public FocusMoveResult(bool Succeeded, string ErrorMessage)
        {
            this.Succeeded = Succeeded;
            this.ErrorMessage = ErrorMessage;
        }
    }


    public class FocusAssembly : IDisposable
    {
        private enum MotorAction
        {
            Forward,
            Reverse,
            Stop
        }

        /// <summary>
        /// The logger for capturing debug and info messages
        /// </summary>
        internal Logger Logger { get; }


        /// <summary>
        /// The rotary encoder coupled to the assembly
        /// </summary>
        internal Amt22 Encoder { get; }


        /// <summary>
        /// The motor driver
        /// </summary>
        internal L6470 Driver { get; }


        /// <summary>
        /// The lower bound of the focuser (the lowest value the rotary encoder
        /// is allowed to reach before shutting the motors off)
        /// </summary>
        public int LowerEncoderBound { get; }


        /// <summary>
        /// The upper bound of the focuser (the highest value the rotary encoder
        /// is allowed to reach before shutting the motors off)
        /// </summary>
        public int UpperEncoderBound { get; }


        private int DesiredPosition;


        private bool StopMove;


        private readonly object MoveLock;


        private Task MoveTask;


        public event EventHandler<FocusMoveResult> MoveFinished;


        public MicrostepMode MicrostepMode
        {
            get
            {
                return Driver.MicrostepMode;
            }
            set
            {
                Driver.MicrostepMode = value;
            }
        }


        public double Position
        {
            get
            {
                int currentPosition = Encoder.GetPosition();
                if(currentPosition < 0)
                {
                    // If the position error'd out, return the error value.
                    return currentPosition;
                }

                // Normalize the position from the lower bound to the upper bound.
                double normalizedPosition = (currentPosition - LowerEncoderBound) / (UpperEncoderBound - LowerEncoderBound);
                return normalizedPosition;
            }
        }


        public int BoundSafetyThreshold { get; set; }


        private int MoveLoopDelay;


        public int EncoderUpdateRate
        {
            get
            {
                return (int)Math.Round(1000.0 / MoveLoopDelay);
            }
            set
            {
                MoveLoopDelay = (int)Math.Round(1000.0 / value);
            }
        }


        public FocusAssembly(
            Logger Logger,
            byte MotorSelectPin,
            byte MotorResetPin,
            byte EncoderSelectPin,
            double StepAngle,
            double MaxCurrent,
            int LowerEncoderBound,
            int UpperEncoderBound,
            int BoundSafetyThreshold = 500,
            int EncoderUpdateRate = 50)
        {
            this.Logger = Logger;
            Encoder = new Amt22(EncoderSelectPin, Logger);
            Driver = new L6470(MotorSelectPin, MotorResetPin, StepAngle, MaxCurrent);
            this.LowerEncoderBound = LowerEncoderBound;
            this.UpperEncoderBound = UpperEncoderBound;
            this.BoundSafetyThreshold = BoundSafetyThreshold;
            this.EncoderUpdateRate = EncoderUpdateRate;

            StopMove = false;
            MoveLock = new object();
        }


        public void MoveToPosition(double Position, double MaxRPM = 60)
        {
            // Clamp the input position to something between 0 and 1
            Position = Math.Min(1.0, Position);
            Position = Math.Max(0.0, Position);

            // De-normalize the position
            double encoderPosition = Position * (UpperEncoderBound - LowerEncoderBound);
            encoderPosition += LowerEncoderBound;

            lock (MoveLock)
            {
                Logger.Debug($"Moving to focus position {Position} (encoder value {encoderPosition})");
                Driver.DesiredSpeed = MaxRPM;
                DesiredPosition = (int)Math.Round(encoderPosition);
                if(MoveTask == null)
                {
                    MoveTask = Task.Run(MovementLoop);
                }
            }
        }


        private void MovementLoop()
        {
            FocusMoveResult result;
            while (!StopMove)
            {
                // Check the current status and report any failures
                L6470Status status = Driver.GetStatus();
                try
                {
                    CheckL6470State(status);
                }
                catch(Exception ex)
                {
                    Driver.SoftHiZ();
                    MoveTask = null;
                    result = new FocusMoveResult(false, ex.Message);
                    MoveFinished?.Invoke(this, result);
                    return;
                }

                // Get the current position
                int currentPosition = Encoder.GetPosition();

                // See which direction we're supposed to go
                MotorAction action = MotorAction.Stop;
                if(DesiredPosition > currentPosition)
                {
                    action = MotorAction.Forward;
                }
                else if(DesiredPosition < currentPosition)
                {
                    action = MotorAction.Reverse;
                }

                // See if the move would take us out of bounds, and if so, don't do it!
                if(action == MotorAction.Forward && currentPosition >= (UpperEncoderBound - BoundSafetyThreshold))
                {
                    action = MotorAction.Stop;
                }
                else if(action == MotorAction.Reverse && currentPosition <= (LowerEncoderBound + BoundSafetyThreshold))
                {
                    action = MotorAction.Stop;
                }

                // Perform the requested action
                switch(action)
                {
                    // Shut it down and consider this move a success
                    case MotorAction.Stop:
                        Driver.SoftHiZ();
                        MoveTask = null;
                        result = new FocusMoveResult(true, null);
                        MoveFinished?.Invoke(this, result);
                        return;

                    // Go forward if it isn't already going forward
                    case MotorAction.Forward:
                        if(Driver.Direction == MotorDirection.Reverse || status.MotorState == MotorActivity.Stopped)
                        {
                            Driver.Direction = MotorDirection.Forward;
                            Driver.Run();
                        }
                        break;

                    // Go backward if it isn't already going backward
                    case MotorAction.Reverse:
                        if(Driver.Direction == MotorDirection.Forward || status.MotorState == MotorActivity.Stopped)
                        {
                            Driver.Direction = MotorDirection.Reverse;
                            Driver.Run();
                        }
                        break;
                }

                Thread.Sleep(MoveLoopDelay);
            }

            // If we get here, the system requested a stop.
            Driver.SoftHiZ();
            MoveTask = null;
            result = new FocusMoveResult(true, null);
            MoveFinished?.Invoke(this, result);
            return;
        }


        private void CheckL6470State(L6470Status Status)
        {
            if(Status.BridgeAStalled)
            {
                throw new Exception($"Focus motor bridge A is stalled.");
            }
            if (Status.BridgeBStalled)
            {
                throw new Exception($"Focus motor bridge B is stalled.");
            }
            if(Status.OvercurrentDetected)
            {
                throw new Exception($"Focus motor exceeded its current limit.");
            }
            if(Status.ThermalShutdownTriggered)
            {
                throw new Exception($"Focus motor driver overheated.");
            }
            if(Status.UndervoltageDetected)
            {
                throw new Exception($"Focus motor fell below its motor voltage threshold.");
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
                    // Wait for any active movements to stop
                    StopMove = true;
                    if(MoveTask != null)
                    {
                        if(!MoveTask.Wait(500))
                        {
                            // Kill the motor if the task didn't respond in time
                            Driver.SoftHiZ();
                        }
                    }

                    // Dispose of the encoder and driver components
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
