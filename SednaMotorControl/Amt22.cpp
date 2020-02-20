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

#include <stdexcept>
#include <iomanip>
#include <string>
#include <wiringPi.h>
#include "Amt22.h"

using namespace std;
using namespace Sedna;


Amt22::Amt22(unsigned char ChipSelectPin) :
    Spi(ChipSelectPin, 1000000, SpiMode::Mode0, 3, 3, 40, 3),
    StartupTime(200000)
{

}


void Amt22::ValidateChecksum(unsigned char* Buffer)
{
    bool k1 = (Buffer[0] >> 7 & 1);
    bool k0 = (Buffer[0] >> 6 & 1);

    bool h5 = (Buffer[0] >> 5 & 1);
    bool h4 = (Buffer[0] >> 4 & 1);
    bool h3 = (Buffer[0] >> 3 & 1);
    bool h2 = (Buffer[0] >> 2 & 1);
    bool h1 = (Buffer[0] >> 1 & 1);
    bool h0 = (Buffer[0] >> 0 & 1);

    bool l7 = (Buffer[1] >> 7 & 1);
    bool l6 = (Buffer[1] >> 6 & 1);
    bool l5 = (Buffer[1] >> 5 & 1);
    bool l4 = (Buffer[1] >> 4 & 1);
    bool l3 = (Buffer[1] >> 3 & 1);
    bool l2 = (Buffer[1] >> 2 & 1);
    bool l1 = (Buffer[1] >> 1 & 1);
    bool l0 = (Buffer[1] >> 0 & 1);

    bool oddCheck = !(h5 ^ h3 ^ h1 ^ l7 ^ l5 ^ l3 ^ l1);
    bool evenCheck = !(h4 ^ h2 ^ h0 ^ l6 ^ l4 ^ l2 ^ l0);

    if (k1 != oddCheck)
    {
        stringstream error;
        error << "Odd checksum bit failed (" << hex << (int)Buffer[0] << ", " << hex << (int)Buffer[1] << ")";
        throw runtime_error(error.str());
    }
    if (k0 != evenCheck)
    {
        stringstream error;
        error << "Even checksum bit failed (" << hex << (int)Buffer[0] << ", " << hex << (int)Buffer[1] << ")";
        throw runtime_error(error.str());
    }
}


unsigned short Amt22::GetPosition()
{
    // Read from the device and validate that it came back OK
    unsigned char buffer[] = { 0x00, 0x00 };
    Spi.TransferData(buffer, 2);
    ValidateChecksum(buffer);
    
    // Make a short from the data
    unsigned short data = 0;
    data += (buffer[0] << 8);
    data += buffer[1];

    // Get rid of the 2 checksum bits at the top
    data &= 0x3FFF;

    return data;
}


void Amt22::Reset()
{
    unsigned char buffer[] = { 0x00, 0x60 };
    Spi.TransferData(buffer, 2);
    delayMicroseconds(StartupTime);
}


void Amt22::SetZeroPosition()
{
    unsigned char buffer[] = { 0x00, 0x70 };
    Spi.TransferData(buffer, 2);
    delayMicroseconds(StartupTime);
}