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

// Import the spidev header as C, otherwise the mode macros (e.g. SPI_IOC_RD_MODE)
// won't always work. Shoutout to Phil Mills on StackOverflow for figuring this out:
// https://stackoverflow.com/questions/31572738/spi-ioc-messagen-macro-giving-me-fits
extern "C"
{
    #include <linux/spi/spidev.h>
}

#include <stdio.h>
#include <fcntl.h>
#include <string.h>
#include <errno.h>
#include <sys/ioctl.h>
#include <asm/ioctl.h>
#include <wiringPi.h>
#include "PiSpi.h"


// This will hold the error message of the last error
static char LastErrorMessage[1024];


// Sets the last error message based on what happened during the error
static void SetLastError(const char* Message)
{
    int lastErrorCode = errno;
    snprintf(LastErrorMessage, 1024, "%s: %s (%d)", Message, strerror(lastErrorCode), lastErrorCode);
}


// The FD for SPI hardware device 0
static int SpiDevDescriptor;


// The underlying device struct
struct SpiDevice
{
    uint8_t ChipSelectPin;
    uint32_t BitRate;
    int Mode;
    uint8_t TimeBeforeRead;
    uint8_t TimeBetweenBytes;
    uint8_t TimeAfterRead;
    uint8_t TimeBetweenReads;
};


PiSpi_Result PiSpi_CreateDevice(
    uint8_t ChipSelectPin,
    uint32_t BitRate,
    PiSpi_SpiMode Mode,
    uint8_t TimeBeforeRead,
    uint8_t TimeBetweenBytes,
    uint8_t TimeAfterRead,
    uint8_t TimeBetweenReads,
    void** Device)
{
    // Initialize the SPI bus when the first device is created
    if (SpiDevDescriptor == 0)
    {
        // Initialize WiringPi
        int result = wiringPiSetupSys();
        if (result == -1)
        {
            SetLastError("WiringPi failed to setup properly");
            return PiSpi_WiringPiInitFailed;
        }

        // Open the first SPI device for read-write, and tell it that we'll take care of the chip select
        // manually. This is necessary when you have more than 2 SPI devices, since the Pi only supports
        // 2 in hardware. We're going to do what's called a "software CS" in this library:
        // https://raspberrypi.stackexchange.com/questions/71448/how-to-connect-multiple-spi-devices-adcs-to-raspberry-pi
        SpiDevDescriptor = open("/dev/spidev0.0", O_RDWR | SPI_NO_CS);
        if (SpiDevDescriptor == -1)
        {
            SetLastError("Failed to open SPI Device 0 during bus initialization");
            return PiSpi_OpeningSpiDev0Failed;
        }

        // Set the bits per word to 8, which most SPI devices use. This is hardcoded for now because I
        // don't really plan to ever need to modify this; if I do, I'll have to come back here and fix it.
        int bitsPerWord = 8;
        result = ioctl(SpiDevDescriptor, SPI_IOC_WR_BITS_PER_WORD, &bitsPerWord);
        if (result == -1)
        {
            SetLastError("Failed to set the bits-per-word for the SPI bus during initialization");
            return PiSpi_SettingBitsPerWordFailed;
        }
    }

    // Define the chip select pin as an output and make sure it's not currently selected
    pinMode(ChipSelectPin, OUTPUT);
    digitalWrite(ChipSelectPin, HIGH);

    // Create the SPI device struct
    SpiDevice* device = new SpiDevice;
    device->ChipSelectPin = ChipSelectPin;
    device->BitRate = BitRate;
    device->Mode = Mode;
    device->TimeBeforeRead = TimeBeforeRead;
    device->TimeBetweenBytes = TimeBetweenBytes;
    device->TimeAfterRead = TimeAfterRead;
    device->TimeBetweenReads = TimeBetweenReads;
    *Device = device;

    return PiSpi_OK;
}


void PiSpi_FreeDevice(void* Device)
{
    SpiDevice* device = static_cast<SpiDevice*>(Device);
    if (device != nullptr)
    {
        delete device;
    }
}


PiSpi_Result RunTransferWithoutDelaysBetweenBytes(SpiDevice* Device, uint8_t* Buffer, uint32_t BufferSize)
{
    // Set up the SPI transfer structure
    __u64 bufferAsLong = reinterpret_cast<__u64>(Buffer);
    spi_ioc_transfer spiTransfer;
    memset(&spiTransfer, 0, sizeof(spiTransfer));

    spiTransfer.speed_hz = Device->BitRate;
    spiTransfer.len = static_cast<__u32>(BufferSize);
    spiTransfer.rx_buf = bufferAsLong;
    spiTransfer.tx_buf = bufferAsLong;

    // Run the transfer
    int result = ioctl(SpiDevDescriptor, SPI_IOC_MESSAGE(1), &spiTransfer);
    if (result == -1)
    {
        SetLastError("Error transferring data to/from the SPI device");
        return PiSpi_SpiTransferFailed;
    }

    return PiSpi_OK;
}


PiSpi_Result RunTransferWithDelaysBetweenBytes(SpiDevice* Device, uint8_t* Buffer, uint32_t BufferSize)
{
    spi_ioc_transfer spiTransfer;
    memset(&spiTransfer, 0, sizeof(spiTransfer));
    spiTransfer.speed_hz = Device->BitRate;
    for (int i = 0; i < BufferSize; i++)
    {
        // Set up the transfer structure
        uint8_t* currentBufferBytePointer = Buffer + i;
        __u64 bufferAsLong = reinterpret_cast<__u64>(currentBufferBytePointer);
        spiTransfer.len = static_cast<__u32>(sizeof(uint8_t));
        spiTransfer.rx_buf = bufferAsLong;
        spiTransfer.tx_buf = bufferAsLong;

        // Run the transfer
        int result = ioctl(SpiDevDescriptor, SPI_IOC_MESSAGE(1), &spiTransfer);
        if (result == -1)
        {
            SetLastError("Error transferring data to/from the SPI device");
            return PiSpi_SpiTransferFailed;
        }

        // Wait for the specified time between bytes
        delayMicroseconds(Device->TimeBetweenBytes);
    }

    return PiSpi_OK;
}


PiSpi_Result PiSpi_TransferData(void* Device, uint8_t* Buffer, uint32_t BufferSize)
{
    SpiDevice* device = static_cast<SpiDevice*>(Device);

    // Set the SPI mode
    int result = ioctl(SpiDevDescriptor, SPI_IOC_WR_MODE, &device->Mode);
    if (result == -1)
    {
        SetLastError("Failed to set the SPI mode");
        return PiSpi_SettingSpiModeFailed;
    }

    // Before we do a transfer, we have to make sure the SPI clock is in the right
    // polarity (modes 0 and 1 need to be low, modes 2 and 3 need to be high) before
    // pulling down the chip select. If we did a transfer using a device with a different
    // polarity right before this one, then the clock will be left in the wrong state
    // when we set the chip select pin, and the SPI device will behave improperly.
    // Since we don't own the SPI clock pin, the easiest way to do this is to run a 
    // "dummy" SPI transfer with none of the chip select pins flagged now that we just set
    // the right mode. We aren't actually talking to anything in this transfer, but the
    // clock will be set to the correct state afterwards so the device behaves correctly.
    uint8_t dummyByte;
    spi_ioc_transfer dummyTransfer;
    memset(&dummyTransfer, 0, sizeof(dummyTransfer));
    __u64 dummyBytePtr = reinterpret_cast<__u64>(&dummyByte);
    dummyTransfer.len = static_cast<__u32>(sizeof(uint8_t));
    dummyTransfer.rx_buf = dummyBytePtr;
    dummyTransfer.tx_buf = dummyBytePtr;
    ioctl(SpiDevDescriptor, SPI_IOC_MESSAGE(1), &dummyTransfer);

    // Pull the CS pin down and wait for the device to be ready
    digitalWrite(device->ChipSelectPin, LOW);
    if (device->TimeBeforeRead > 0)
    {
        delayMicroseconds(device->TimeBeforeRead);
    }

    // Run the transfer
    PiSpi_Result transferResult;
    if (device->TimeBetweenBytes > 0)
    {
        transferResult = RunTransferWithDelaysBetweenBytes(device, Buffer, BufferSize);
    }
    else
    {
        transferResult = RunTransferWithoutDelaysBetweenBytes(device, Buffer, BufferSize);
    }
    if (transferResult != PiSpi_OK)
    {
        digitalWrite(device->ChipSelectPin, HIGH);
        return transferResult;
    }

    // Wait for the cooldown before setting the CS pin back to high
    if (device->TimeAfterRead > 0)
    {
        delayMicroseconds(device->TimeAfterRead);
    }
    digitalWrite(device->ChipSelectPin, HIGH);

    // Wait for the device to be ready to use again
    if (device->TimeBetweenReads > 0)
    {
        delayMicroseconds(device->TimeBetweenReads);
    }

    return PiSpi_OK;
}


const char* PiSpi_GetLastError()
{
    return LastErrorMessage;
}