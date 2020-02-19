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
#include "SednaMotorControl.h"


// =======================
// === Error Messaging ===
// =======================


// This will hold the error message of the last error
static char LastErrorMessage[1024];


// The FD for the active SPI device (which is device 0)
static int SpiDevDescriptor;


// Sets the last error message based on what happened during the error
static int SetLastError(const char* Message)
{
    int lastErrorCode = errno;
    snprintf(LastErrorMessage, 1024, "%s: %s (%d)", Message, strerror(lastErrorCode), lastErrorCode);
    return -1;
}


// Public getter for the last error message
const char* GetLastError()
{
    return LastErrorMessage;
}


// =======================
// === Initialization ===
// =======================


// Initializes WiringPi and the SPI bus
int Initialize()
{
    // If this is already initialized, we don't need to do anything.
    if (SpiDevDescriptor != 0)
    {
        return 0;
    }

    // Initialize WiringPi
    int result = wiringPiSetup();
    if (result == -1)
    {
        return SetLastError("WiringPi failed to setup properly");
    }

    // Open the first SPI device for read-write, and tell it that we'll take care of the chip select
    // manually. This is necessary when you have more than 2 SPI devices, since the Pi only supports
    // 2 in hardware. We're going to do what's called a "software CS" in this library:
    // https://raspberrypi.stackexchange.com/questions/71448/how-to-connect-multiple-spi-devices-adcs-to-raspberry-pi
    SpiDevDescriptor = open("/dev/spidev0.0", O_RDWR | SPI_NO_CS);
    if (SpiDevDescriptor == -1)
    {
        return SetLastError("Failed to open SPI Device 0");
    }

    // Set the bits per word to 8, which most SPI devices use. This is hardcoded for now because I
    // don't really plan to ever need to modify this; if I do, I'll have to come back here and fix it.
    int bitsPerWord = 8;
    result = ioctl(SpiDevDescriptor, SPI_IOC_WR_BITS_PER_WORD, &bitsPerWord);
    if (result == -1)
    {
        return SetLastError("Failed to set the SPI bits-per-word");
    }

    return 0;
}


// Sets up the chip select pin of a device
void InitializeSpiDevice(SpiDevice* Device)
{
    pinMode(Device->ChipSelectPin, OUTPUT);
    digitalWrite(Device->ChipSelectPin, HIGH);
}


// Executes a device read/write transfer.
int TransferData(SpiDevice* Device, char* Buffer, __u32 BufferSize)
{
    // Set up the SPI transfer structure
    __u64 bufferAsLong = reinterpret_cast<__u64>(Buffer);
    spi_ioc_transfer spiTransfer;
    memset(&spiTransfer, 0, sizeof(spiTransfer));

    spiTransfer.speed_hz = Device->BitRate;
    spiTransfer.word_delay_usecs = Device->TimeBetweenBytes;
    spiTransfer.len = BufferSize;
    spiTransfer.rx_buf = bufferAsLong;
    spiTransfer.tx_buf = bufferAsLong;


    // Set the SPI mode
    int result = ioctl(SpiDevDescriptor, SPI_IOC_WR_MODE, &Device->SpiMode);
    if (result == -1)
    {
        return SetLastError("Failed to set the SPI mode");
    }

    // Pull the CS pin down and wait for the device to be ready
    digitalWrite(Device->ChipSelectPin, LOW);
    delayMicroseconds(Device->TimeBeforeRead);

    // Run the transfer
    result = ioctl(SpiDevDescriptor, SPI_IOC_MESSAGE(1), &spiTransfer);
    if (result == -1)
    {
        return SetLastError("Error transferring data to/from the SPI device");
    }

    // Wait for the cooldown before setting the CS pin back to high
    delayMicroseconds(Device->TimeAfterRead);
    digitalWrite(Device->ChipSelectPin, HIGH);

    // Wait for the device to be ready to use again
    delayMicroseconds(Device->TimeBetweenReads);

    return 0;
}