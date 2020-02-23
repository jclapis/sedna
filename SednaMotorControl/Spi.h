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

namespace Sedna
{
	/// <summary>
	/// This defines what SPI mode a device uses.
	/// </summary>
	enum class SpiMode : uint8_t
	{
		/// <summary>
		/// Mode 0 (clock polarity 0, clock phase 0, clock edge 1)
		/// </summary>
		Mode0 = 0,

		/// <summary>
		/// Mode 1 (clock polarity 0, clock phase 1, clock edge 0)
		/// </summary>
		Mode1 = 1,

		/// <summary>
		/// Mode 2 (clock polarity 1, clock phase 0, clock edge 1)
		/// </summary>
		Mode2 = 2,

		/// <summary>
		/// Mode 3 (clock polarity 1, clock phase 1, clock edge 0)
		/// </summary>
		Mode3 = 3
	};


	/// <summary>
	/// This represents a device that is connected to the SPI bus.
	/// </summary>
	class SpiDevice
	{
	public:
		/// <summary>
		/// Creates a new SPI device.
		/// </summary>
		/// <param name="ChipSelectPin">The number of the chip select pin for this device. This uses the
		/// WiringPi pin layout. To find what the pin number is for a given physical pin, run `gpio readall`
		/// on your Pi.</param>
		/// <param name="BitRate">The speed of the SPI communications for this device, in Hz.</param>
		/// <param name="SpiMode">The SPI mode that this device uses</param>
		/// <param name="TimeBeforeRead">The delay (in microseconds) between pulling the chip select pin low
		/// and starting to read/write data from/to the device.</param>
		/// <param name="TimeBetweenBytes">The delay (in microseconds) to wait between reading and writing
		/// individual bytes. Some devices have specific timings for this; consult your device's datasheet.
		/// </param>
		/// <param name="TimeAfterRead">The delay (in microseconds) to wait after finishing a data read/write
		/// before setting the chip select pin back to high (unselecting the device).</param>
		/// <param name="TimeBetweenReads">The delay (in microseconds) to wait after unselecting a device before
		/// it can be selected again for a subsequent read/write.</param>
		SpiDevice(
			uint8_t ChipSelectPin,
			uint32_t BitRate,
			SpiMode Mode,
			uint8_t TimeBeforeRead,
			uint8_t TimeBetweenBytes,
			uint8_t TimeAfterRead,
			uint8_t TimeBetweenReads
		);


		/// <summary>
		/// Transfers data to and from the SPI device in duplex mode.
		/// </summary>
		/// <param name="Buffer">The buffer containing the data to write. After the transfer, this will contain
		/// the data read from the device.</param>
		/// <param name="BufferSize">The size of the buffer, in bytes.</param>
		void TransferData(uint8_t* Buffer, uint32_t BufferSize);


	private:
		/// <summary>
		/// The number of the chip select pin for this device. This uses the
		/// WiringPi pin layout. To find what the pin number is for a given
		/// physical pin, run `gpio readall` on your Pi.
		/// </summary>
		uint8_t ChipSelectPin;


		/// <summary>
		/// The speed of the SPI communications for this device, in Hz.
		/// </summary>
		uint32_t BitRate;


		/// <summary>
		/// The SPI mode that this device uses
		/// </summary>
		SpiMode Mode;


		/// <summary>
		/// The delay (in microseconds) between pulling the chip select pin low and
		/// starting to read/write data from/to the device.
		/// </summary>
		uint8_t TimeBeforeRead;


		/// <summary>
		/// The delay (in microseconds) to wait between reading and writing individual bytes.
		/// Some devices have specific timings for this; consult your device's datasheet.
		/// </summary>
		uint8_t TimeBetweenBytes;


		/// <summary>
		/// The delay (in microseconds) to wait after finishing a data read/write before setting
		/// the chip select pin back to high (unselecting the device).
		/// </summary>
		uint8_t TimeAfterRead;


		/// <summary>
		/// The delay (in microseconds) to wait after unselecting a device before it can be 
		/// selected again for a subsequent read/write.
		/// </summary>
		uint8_t TimeBetweenReads;
	};

}