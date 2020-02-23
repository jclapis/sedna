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


#include <stdio.h>
#include <fcntl.h>
#include <string.h>
#include <errno.h>
#include <sys/ioctl.h>
#include <asm/ioctl.h>
#include <linux/spi/spidev.h>
#include <wiringPi.h>
#include <stdexcept>
#include "Spi.h"

using namespace std;

namespace Sedna
{
    // The FD for the active SPI device (which is device 0)
    static int SpiDevDescriptor;


    SpiDevice::SpiDevice(
        uint8_t ChipSelectPin,
        uint32_t BitRate,
        SpiMode Mode,
        uint8_t TimeBeforeRead,
        uint8_t TimeBetweenBytes,
        uint8_t TimeAfterRead,
        uint8_t TimeBetweenReads)
        :
        ChipSelectPin(ChipSelectPin),
        BitRate(BitRate),
        Mode(Mode),
        TimeBeforeRead(TimeBeforeRead),
        TimeBetweenBytes(TimeBetweenBytes),
        TimeAfterRead(TimeAfterRead),
        TimeBetweenReads(TimeBetweenReads)
    {

        // Initialize the SPI bus when the first device is created
        if (SpiDevDescriptor == 0)
        {
            // Open the first SPI device for read-write, and tell it that we'll take care of the chip select
            // manually. This is necessary when you have more than 2 SPI devices, since the Pi only supports
            // 2 in hardware. We're going to do what's called a "software CS" in this library:
            // https://raspberrypi.stackexchange.com/questions/71448/how-to-connect-multiple-spi-devices-adcs-to-raspberry-pi
            SpiDevDescriptor = open("/dev/spidev0.0", O_RDWR | SPI_NO_CS);
            if (SpiDevDescriptor == -1)
            {
                throw runtime_error("Failed to open SPI Device 0 during bus initialization");
            }

            // Set the bits per word to 8, which most SPI devices use. This is hardcoded for now because I
            // don't really plan to ever need to modify this; if I do, I'll have to come back here and fix it.
            int bitsPerWord = 8;
            int result = ioctl(SpiDevDescriptor, SPI_IOC_WR_BITS_PER_WORD, &bitsPerWord);
            if (result == -1)
            {
                throw runtime_error("Failed to set the bits-per-word for the SPI bus during initialization");
            }
        }

        // Define the chip select pin as an output and make sure it's not currently selected
        pinMode(ChipSelectPin, OUTPUT);
        digitalWrite(ChipSelectPin, HIGH);
    }


    void SpiDevice::TransferData(uint8_t* Buffer, uint32_t BufferSize)
    {
        // Set up the SPI transfer structure
        __u64 bufferAsLong = reinterpret_cast<__u64>(Buffer);
        spi_ioc_transfer spiTransfer;
        memset(&spiTransfer, 0, sizeof(spiTransfer));

        spiTransfer.speed_hz = BitRate;
        spiTransfer.word_delay_usecs = TimeBetweenBytes;
        spiTransfer.len = static_cast<__u32>(BufferSize);
        spiTransfer.rx_buf = bufferAsLong;
        spiTransfer.tx_buf = bufferAsLong;


        // Set the SPI mode
        int result = ioctl(SpiDevDescriptor, SPI_IOC_WR_MODE, Mode);
        if (result == -1)
        {
            throw runtime_error("Failed to set the SPI mode");
        }

        // Pull the CS pin down and wait for the device to be ready
        digitalWrite(ChipSelectPin, LOW);
        delayMicroseconds(TimeBeforeRead);

        // Run the transfer
        result = ioctl(SpiDevDescriptor, SPI_IOC_MESSAGE(1), &spiTransfer);
        if (result == -1)
        {
            throw runtime_error("Error transferring data to/from the SPI device");
        }

        // Wait for the cooldown before setting the CS pin back to high
        delayMicroseconds(TimeAfterRead);
        digitalWrite(ChipSelectPin, HIGH);

        // Wait for the device to be ready to use again
        delayMicroseconds(TimeBetweenReads);
    }
}