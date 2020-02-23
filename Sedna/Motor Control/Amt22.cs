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

namespace Sedna
{

    /// <summary>
    /// This represents an AMT22 rotary encoder.
    /// </summary>
    /// <remarks>
    /// Please see the datasheet for details:
    /// https://www.cuidevices.com/product/resource/amt22.pdf
    /// </remarks>
    public class Amt22 : IDisposable
    {
        /// <summary>
        /// The underlying SPI device used to communicate with the AMT22
        /// </summary>
        private readonly SpiDevice Spi;


        /// <summary>
        /// Creates a new Amt22 instance.
        /// </summary>
        /// <param name="ChipSelectPin">The number of the chip select pin for this device,
        /// using the WiringPi numbering scheme.</param>
        public Amt22(byte ChipSelectPin)
        {
            Spi = new SpiDevice(ChipSelectPin, 1000000, SpiMode.Mode0, 3, 3, 40, 3);
        }


        /// <summary>
        /// Gets the current position of the encoder's shaft. For 12-bit devices, this ranges from
        /// 0 to 4095. For 14-bit devices, this ranges from 0 to 16383.
        /// </summary>
        /// <returns>The position of the shaft the encoder is coupled to.</returns>
        public ushort GetPosition()
        {
            // Read from the device and validate that it came back OK
            byte[] buffer = { 0x00, 0x00 };
            Spi.TransferData(buffer);
            ValidateChecksum(buffer);

            // Make a short from the data
            ushort data = 0;
            data |= (ushort)(buffer[0] << 8);
            data |= buffer[1];

            // Get rid of the 2 checksum bits at the top
            data &= 0x3FFF;

            return data;
        }


        /// <summary>
        /// Resets the encoder. The encoder must be stationary in order to power back on.
        /// </summary>
        public void Reset()
        {
            byte[] buffer = { 0x00, 0x60 };
            Spi.TransferData(buffer);
            // The device takes 200 microseconds to reset, but .NET doesn't give us that
            // resolution so just sleep for 1ms.
            Thread.Sleep(1);
        }


        /// <summary>
        /// Sets the current position as the new zero position, then resets the device.
        /// The encoder must be stationary for this command.
        /// </summary>
        public void SetZeroPosition()
        {
            byte[] buffer = { 0x00, 0x70 };
            Spi.TransferData(buffer);
            // The device takes 200 microseconds to reset, but .NET doesn't give us that
            // resolution so just sleep for 1ms.
            Thread.Sleep(1);
        }


        /// <summary>
        /// Validates that the data returned by the device passes its checksum test.
        /// </summary>
        /// <param name="Buffer">The data returned by the device (2 bytes)</param>
        private void ValidateChecksum(byte[] Buffer)
        {
            bool k1 = (Buffer[0] >> 7 & 1) == 1;
            bool k0 = (Buffer[0] >> 6 & 1) == 1;

            bool h5 = (Buffer[0] >> 5 & 1) == 1;
            bool h4 = (Buffer[0] >> 4 & 1) == 1;
            bool h3 = (Buffer[0] >> 3 & 1) == 1;
            bool h2 = (Buffer[0] >> 2 & 1) == 1;
            bool h1 = (Buffer[0] >> 1 & 1) == 1;
            bool h0 = (Buffer[0] >> 0 & 1) == 1;

            bool l7 = (Buffer[1] >> 7 & 1) == 1;
            bool l6 = (Buffer[1] >> 6 & 1) == 1;
            bool l5 = (Buffer[1] >> 5 & 1) == 1;
            bool l4 = (Buffer[1] >> 4 & 1) == 1;
            bool l3 = (Buffer[1] >> 3 & 1) == 1;
            bool l2 = (Buffer[1] >> 2 & 1) == 1;
            bool l1 = (Buffer[1] >> 1 & 1) == 1;
            bool l0 = (Buffer[1] >> 0 & 1) == 1;

            bool oddCheck = !(h5 ^ h3 ^ h1 ^ l7 ^ l5 ^ l3 ^ l1);
            bool evenCheck = !(h4 ^ h2 ^ h0 ^ l6 ^ l4 ^ l2 ^ l0);

            if (k1 != oddCheck)
            {
                throw new Exception($"Odd checksum bit failed (0x{Buffer[0].ToString("X2")}, 0x{Buffer[1].ToString("X2")})");
            }
            if (k0 != evenCheck)
            {
                throw new Exception($"Even checksum bit failed (0x{Buffer[0].ToString("X2")}, 0x{Buffer[1].ToString("X2")})");
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
                    Spi.Dispose();
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
