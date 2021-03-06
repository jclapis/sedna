﻿/* ========================================================================
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
using System.Runtime.InteropServices;

namespace Sedna
{
    /// <summary>
    /// This defines what SPI mode a device uses.
    /// </summary>
    internal enum SpiMode
    {
        /// <summary>
        /// Mode 0 (clock polarity 0, clock phase 0, clock edge 1)
        /// </summary>
        Mode0,

        /// <summary>
        /// Mode 1 (clock polarity 0, clock phase 1, clock edge 0)
        /// </summary>
        Mode1,

        /// <summary>
        /// Mode 2 (clock polarity 1, clock phase 0, clock edge 1)
        /// </summary>
        Mode2,

        /// <summary>
        /// Mode 3 (clock polarity 1, clock phase 1, clock edge 0)
        /// </summary>
        Mode3
    }


    /// <summary>
    /// This defines the various return codes for PiSpi functions.
    /// </summary>
    internal enum PiSpi_Result
    {
        /// <summary>
        /// The function succeeded.
        /// </summary>
        PiSpi_OK = 0,

        /// <summary>
        /// Initializing WiringPi failed.
        /// </summary>
        PiSpi_WiringPiInitFailed = -1,

        /// <summary>
        /// Opening SPI device 0 (/dev/spidev0.0) failed.
        /// </summary>
        PiSpi_OpeningSpiDev0Failed = -2,

        /// <summary>
        /// Setting the bits-per-word for the SPI bus failed.
        /// </summary>
        PiSpi_SettingBitsPerWordFailed = -3,

        /// <summary>
        /// Setting the SPI mode for a transfer failed.
        /// </summary>
        PiSpi_SettingSpiModeFailed = -4,

        /// <summary>
        /// Transferring data over the SPI bus failed.
        /// </summary>
        PiSpi_SpiTransferFailed = -5
    }


    /// <summary>
    /// This class wraps PiSpi, providing SPI bus functionality.
    /// </summary>
    internal class SpiDevice : IDisposable
    {
        #region Static Stuff

        /// <summary>
        /// The name of the PiSpi library
        /// </summary>
        private const string PiSpiLib = "PiSpi";


        /// <summary>
        /// A synchronization lock so multiple threads don't try to run SPI
        /// transfers simultaneously
        /// </summary>
        private static object TransferLock;


        /// <summary>
        /// Initializes the transfer lock
        /// </summary>
        static SpiDevice()
        {
            TransferLock = new object();
        }

        #endregion

        #region Interop Functions

        /// <summary>
        /// Creates a new SPI device. This will use the standard SPI device 0 pins for SCLK, MISO, and MOSI.
        /// It will also initialize the chip select pin to output mode and set it HIGH.
        /// </summary>
        /// <param name="ChipSelectPin">The number of the chip select pin for this device. This uses the
        /// BCM pin layout. To find what the pin number is for a given physical pin, run `gpio readall`
        /// on your Pi.</param>
        /// <param name="BitRate">The speed of the SPI communications for this device, in Hz.</param>
        /// <param name="Mode">The SPI mode that this device uses</param>
        /// <param name="TimeBeforeRead">The delay (in microseconds) between pulling the chip select pin low
        /// and starting to read/write data from/to the device.</param>
        /// <param name="TimeBetweenBytes">The delay (in microseconds) to wait between reading and writing
        /// individual bytes. Some devices have specific timings for this; consult your device's datasheet.
        /// </param>
        /// <param name="TimeAfterRead">The delay (in microseconds) to wait after finishing a data read/write
        /// before setting the chip select pin back to high (unselecting the device).</param>
        /// <param name="TimeBetweenReads">The delay (in microseconds) to wait after unselecting a device before
        /// it can be selected again for a subsequent read/write.</param>
        /// <param name="Device">[OUT] A handle to the new device.</param>
        /// <returns>PiSpi_OK on a success, or one of the error codes (all negative numbers) on a failure.
        /// Call PiSpi_GetLastError() for more information.</returns>
        [DllImport(PiSpiLib, CharSet = CharSet.Ansi, ExactSpelling = true)]
        private static extern PiSpi_Result PiSpi_CreateDevice(
            byte ChipSelectPin,
            uint BitRate,
            SpiMode Mode,
            byte TimeBeforeRead,
            byte TimeBetweenBytes,
            byte TimeAfterRead,
            byte TimeBetweenReads,
            out IntPtr Device);


        /// <summary>
        /// Deletes / frees a SPI device.
        /// </summary>
        /// <param name="Device">The device to free. This must have been created with PiSpi_CreateDevice().</param>
        [DllImport(PiSpiLib, CharSet = CharSet.Ansi, ExactSpelling = true)]
        private static extern void PiSpi_FreeDevice(IntPtr Device);


        /// <summary>
        /// Transfers data to and from the SPI device in duplex mode.
        /// </summary>
        /// <param name="Device">A handle to a SPI device created by PiSpi_CreateDevice().</param>
        /// <param name="Buffer">The buffer containing the data to write. After the transfer, this will contain
        /// the data that was read from the device.</param>
        /// <param name="BufferSize">The size of the buffer, in bytes.</param>
        /// <returns>PiSpi_OK on a success, or one of the error codes (all negative numbers) on a failure.
        /// Call PiSpi_GetLastError() for more information.</returns>
        [DllImport(PiSpiLib, CharSet = CharSet.Ansi, ExactSpelling = true)]
        private static extern PiSpi_Result PiSpi_TransferData(IntPtr Device, IntPtr Buffer, uint BufferSize);


        /// <summary>
        /// Returns an error message describing the last error that occurred.
        /// </summary>
        /// <returns>A human-readable description of the last error.</returns>
        [DllImport(PiSpiLib, CharSet = CharSet.Ansi, ExactSpelling = true)]
        private static extern IntPtr PiSpi_GetLastError();

        #endregion


        /// <summary>
        /// The handle to the underlying SPI device
        /// </summary>
        private readonly IntPtr Handle;


        /// <summary>
        /// Creates a new SPI device. This will use the standard SPI device 0 pins for SCLK, MISO, and MOSI.
        /// It will also initialize the chip select pin to output mode and set it HIGH.
        /// </summary>
        /// <param name="ChipSelectPin">The number of the chip select pin for this device. This uses the
        /// BCM pin layout. To find what the pin number is for a given physical pin, run `gpio readall`
        /// on your Pi.</param>
        /// <param name="BitRate">The speed of the SPI communications for this device, in Hz.</param>
        /// <param name="Mode">The SPI mode that this device uses</param>
        /// <param name="TimeBeforeRead">The delay (in microseconds) between pulling the chip select pin low
        /// and starting to read/write data from/to the device.</param>
        /// <param name="TimeBetweenBytes">The delay (in microseconds) to wait between reading and writing
        /// individual bytes. Some devices have specific timings for this; consult your device's datasheet.
        /// </param>
        /// <param name="TimeAfterRead">The delay (in microseconds) to wait after finishing a data read/write
        /// before setting the chip select pin back to high (unselecting the device).</param>
        /// <param name="TimeBetweenReads">The delay (in microseconds) to wait after unselecting a device before
        /// it can be selected again for a subsequent read/write.</param>
        public SpiDevice(
            byte ChipSelectPin,
            uint BitRate,
            SpiMode Mode,
            byte TimeBeforeRead,
            byte TimeBetweenBytes,
            byte TimeAfterRead,
            byte TimeBetweenReads)
        {
            PiSpi_Result result = PiSpi_CreateDevice(ChipSelectPin, BitRate, Mode,
                TimeBeforeRead, TimeBetweenBytes, TimeAfterRead, TimeBetweenReads, out IntPtr handle);
            if (result != PiSpi_Result.PiSpi_OK)
            {
                string error = Marshal.PtrToStringAnsi(PiSpi_GetLastError());
                throw new Exception($"Error creating SPI device: {error}");
            }

            Handle = handle;
        }


        /// <summary>
        /// Transfers data to and from the SPI device in duplex mode.
        /// </summary>
        /// <param name="Buffer">The buffer containing the data to write. After the transfer, this will contain
        /// the data that was read from the device.</param>
        public void TransferData(byte[] Buffer)
        {
            lock(TransferLock)
            {
                GCHandle bufferHandle = GCHandle.Alloc(Buffer, GCHandleType.Pinned);
                PiSpi_Result result = PiSpi_TransferData(Handle, bufferHandle.AddrOfPinnedObject(), (uint)Buffer.Length);
                bufferHandle.Free();

                if(result != PiSpi_Result.PiSpi_OK)
                {
                    string error = Marshal.PtrToStringAnsi(PiSpi_GetLastError());
                    throw new Exception($"Error during a SPI transfer: {error}");
                }
            }
        }


        #region IDisposable Support
        private bool DisposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!DisposedValue)
            {
                if (disposing)
                {
                }

                if(Handle != IntPtr.Zero)
                {
                    PiSpi_FreeDevice(Handle);
                }

                DisposedValue = true;
            }
        }

        ~SpiDevice()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion


    }
}
