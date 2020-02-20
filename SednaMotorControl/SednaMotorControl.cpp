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
#include "SednaMotorControl.h"
#include "Spi.h"

using namespace std;
using namespace Sedna;

// This will hold the error message of the last error
static char LastErrorMessage[1024];


// Sets the last error message based on what happened during the error
static int SetLastError(const char* Message)
{
    int lastErrorCode = errno;
    snprintf(LastErrorMessage, 1024, "%s: %s (%d)", Message, strerror(lastErrorCode), lastErrorCode);
    return -1;
}


const char* GetLastError()
{
    return LastErrorMessage;
}


int Initialize()
{
    // Initialize WiringPi
    int result = wiringPiSetup();
    if (result == -1)
    {
        return SetLastError("WiringPi failed to setup properly");
    }

    return 0;
}


int CreateDevice(SpiDeviceType DeviceType, unsigned char ChipSelectPin, void** Driver)
{
    try
    {
        SpiDevice* device;
        switch (DeviceType)
        {
        case SpiDeviceType::SpiDeviceType_L6470:
            // The L6470 has a speed cap of 5 MHz and all of its timings are in the nanosecond range
            // so I just set all of the delays to 1 us. The AutoDriver library uses 4 MHz as the SPI
            // datarate for some reason (probably related to the Arduino's clock itself), but I'm not
            // in a huge rush for data so 4 MHz is fine.
            // Datasheet: https://cdn.sparkfun.com/datasheets/Robotics/dSPIN.pdf
            device = new SpiDevice(ChipSelectPin, 4000000, SpiMode::Mode3, 1, 0, 1, 1);
            break;

        case SpiDeviceType::SpiDeviceType_Amt22:
            // The AMT22 series has a speed cap of 2 MHz, but I'll leave it at 1 to be safe. The
            // timings are in the microsecond range so they actually matter here. The datasheet is
            // pretty detailed: https://www.mouser.com/datasheet/2/670/amt22-1517358.pdf
            device = new SpiDevice(ChipSelectPin, 1000000, SpiMode::Mode0, 3, 3, 40, 3);
            break;
        }

        *Driver = device;
        return 0;
    }
    catch (runtime_error& ex)
    {
        return SetLastError(ex.what());
    }
}


int FreeDevice(void* Device)
{
    SpiDevice* spiDevice = static_cast<SpiDevice*>(Device);
    if (spiDevice != nullptr)
    {
        delete spiDevice;
    }

    return 0;
}