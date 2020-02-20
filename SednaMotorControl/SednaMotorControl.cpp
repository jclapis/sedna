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
#include "Amt22.h"

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


int CreateAmt22(unsigned char ChipSelectPin, void** Device)
{
    try
    {
        Amt22* device = new Amt22(ChipSelectPin);
        *Device = device;
        return 0;
    }
    catch (runtime_error& ex)
    {
        return SetLastError(ex.what());
    }
}


void FreeAmt22(void* Device)
{
    Amt22* device = static_cast<Amt22*>(Device);
    if (device != nullptr)
    {
        delete device;
    }
}

// TODO: L6470
// device = new SpiDevice(ChipSelectPin, 4000000, SpiMode::Mode3, 1, 0, 1, 1);