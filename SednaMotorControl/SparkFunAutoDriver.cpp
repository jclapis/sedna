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


// This file is derived from SparkFun's L6470 Autodriver library. Please see
// the original source for more information:
// https://github.com/sparkfun/L6470-AutoDriver/tree/master/Libraries/Arduino/src


#include <wiringPi.h>
#include <math.h>
#include "SparkFunAutoDriver.h"

using namespace Sedna;

int AutoDriver::_numBoards = 0;

// Constructors
AutoDriver::AutoDriver(SpiDevice* SpiDevice, int position, int CSPin, int resetPin, int busyPin)
{
    _CSPin = CSPin;
    _position = position;
    _resetPin = resetPin;
    _busyPin = busyPin;
    _numBoards++;


    _SPI = SpiDevice;
}

AutoDriver::AutoDriver(SpiDevice* SpiDevice, int position, int CSPin, int resetPin)
    : AutoDriver(SpiDevice, position, CSPin, resetPin, -1)
{

}

int AutoDriver::busyCheck(void)
{
    if (_busyPin == -1)
    {
        if (getParam(STATUS) & 0x0002) return 0;
        else                           return 1;
    }
    else
    {
        if (digitalRead(_busyPin) == HIGH) return 0;
        else                               return 1;
    }
}

// Realize the "set parameter" function, to write to the various registers in
//  the dSPIN chip.
void AutoDriver::setParam(unsigned char param, unsigned long value)
{
    param |= SET_PARAM;
    SPIXfer((unsigned char)param);
    paramHandler(param, value);
}

// Realize the "get parameter" function, to read from the various registers in
//  the dSPIN chip.
long AutoDriver::getParam(unsigned char param)
{
    SPIXfer(param | GET_PARAM);
    return paramHandler(param, 0);
}

// Returns the content of the ABS_POS register, which is a signed 22-bit number
//  indicating the number of steps the motor has traveled from the HOME
//  position. HOME is defined by zeroing this register, and it is zero on
//  startup.
long AutoDriver::getPos()
{
    long temp = getParam(ABS_POS);

    // Since ABS_POS is a 22-bit 2's comp value, we need to check bit 21 and, if
    //  it's set, set all the bits ABOVE 21 in order for the value to maintain
    //  its appropriate sign.
    if (temp & 0x00200000) temp |= 0xffc00000;
    return temp;
}

// Just like getPos(), but for MARK.
long AutoDriver::getMark()
{
    long temp = getParam(MARK);

    // Since ABS_POS is a 22-bit 2's comp value, we need to check bit 21 and, if
    //  it's set, set all the bits ABOVE 21 in order for the value to maintain
    //  its appropriate sign.
    if (temp & 0x00200000) temp |= 0xffC00000;
    return temp;
}

// RUN sets the motor spinning in a direction (defined by the constants
//  FWD and REV). Maximum speed and minimum speed are defined
//  by the MAX_SPEED and MIN_SPEED registers; exceeding the FS_SPD value
//  will switch the device into full-step mode.
// The spdCalc() function is provided to convert steps/s values into
//  appropriate integer values for this function.
void AutoDriver::run(unsigned char dir, float stepsPerSec)
{
    SPIXfer(RUN | dir);
    unsigned long integerSpeed = spdCalc(stepsPerSec);
    if (integerSpeed > 0xFFFFF) integerSpeed = 0xFFFFF;

    // Now we need to push this value out to the dSPIN. The 32-bit value is
    //  stored in memory in little-endian format, but the dSPIN expects a
    //  big-endian output, so we need to reverse the byte-order of the
    //  data as we're sending it out. Note that only 3 of the 4 bytes are
    //  valid here.

    // We begin by pointing bytePointer at the first byte in integerSpeed.
    unsigned char* bytePointer = (unsigned char*)&integerSpeed;
    // Next, we'll iterate through a for loop, indexing across the bytes in
    //  integerSpeed starting with byte 2 and ending with byte 0.
    for (signed char i = 2; i >= 0; i--)
    {
        SPIXfer(bytePointer[i]);
    }
}

// STEP_CLOCK puts the device in external step clocking mode. When active,
//  pin 25, STCK, becomes the step clock for the device, and steps it in
//  the direction (set by the FWD and REV constants) imposed by the call
//  of this function. Motion commands (RUN, MOVE, etc) will cause the device
//  to exit step clocking mode.
void AutoDriver::stepClock(unsigned char dir)
{
    SPIXfer(STEP_CLOCK | dir);
}

// MOVE will send the motor numStep full steps in the
//  direction imposed by dir (FWD or REV constants may be used). The motor
//  will accelerate according the acceleration and deceleration curves, and
//  will run at MAX_SPEED. Stepping mode will adhere to FS_SPD value, as well.
void AutoDriver::move(unsigned char dir, unsigned long numSteps)
{
    SPIXfer(MOVE | dir);
    if (numSteps > 0x3FFFFF) numSteps = 0x3FFFFF;
    // See run() for an explanation of what's going on here.
    unsigned char* bytePointer = (unsigned char*)&numSteps;
    for (signed char i = 2; i >= 0; i--)
    {
        SPIXfer(bytePointer[i]);
    }
}

// GOTO operates much like MOVE, except it produces absolute motion instead
//  of relative motion. The motor will be moved to the indicated position
//  in the shortest possible fashion.
void AutoDriver::goTo(long pos)
{
    SPIXfer(GOTO);
    if (pos > 0x3FFFFF) pos = 0x3FFFFF;
    // See run() for an explanation of what's going on here.
    unsigned char* bytePointer = (unsigned char*)&pos;
    for (signed char i = 2; i >= 0; i--)
    {
        SPIXfer(bytePointer[i]);
    }
}

// Same as GOTO, but with user constrained rotational direction.
void AutoDriver::goToDir(unsigned char dir, long pos)
{
    SPIXfer(GOTO_DIR | dir);
    if (pos > 0x3FFFFF) pos = 0x3FFFFF;
    // See run() for an explanation of what's going on here.
    unsigned char* bytePointer = (unsigned char*)&pos;
    for (signed char i = 2; i >= 0; i--)
    {
        SPIXfer(bytePointer[i]);
    }
}

// GoUntil will set the motor running with direction dir (REV or
//  FWD) until a falling edge is detected on the SW pin. Depending
//  on bit SW_MODE in CONFIG, either a hard stop or a soft stop is
//  performed at the falling edge, and depending on the value of
//  act (either RESET or COPY) the value in the ABS_POS register is
//  either RESET to 0 or COPY-ed into the MARK register.
void AutoDriver::goUntil(unsigned char action, unsigned char dir, float stepsPerSec)
{
    SPIXfer(GO_UNTIL | action | dir);
    unsigned long integerSpeed = spdCalc(stepsPerSec);
    if (integerSpeed > 0x3FFFFF) integerSpeed = 0x3FFFFF;
    // See run() for an explanation of what's going on here.
    unsigned char* bytePointer = (unsigned char*)&integerSpeed;
    for (signed char i = 2; i >= 0; i--)
    {
        SPIXfer(bytePointer[i]);
    }
}

// Similar in nature to GoUntil, ReleaseSW produces motion at the
//  higher of two speeds: the value in MIN_SPEED or 5 steps/s.
//  The motor continues to run at this speed until a rising edge
//  is detected on the switch input, then a hard stop is performed
//  and the ABS_POS register is either COPY-ed into MARK or RESET to
//  0, depending on whether RESET or COPY was passed to the function
//  for act.
void AutoDriver::releaseSw(unsigned char action, unsigned char dir)
{
    SPIXfer(RELEASE_SW | action | dir);
}

// GoHome is equivalent to GoTo(0), but requires less time to send.
//  Note that no direction is provided; motion occurs through shortest
//  path. If a direction is required, use GoTo_DIR().
void AutoDriver::goHome()
{
    SPIXfer(GO_HOME);
}

// GoMark is equivalent to GoTo(MARK), but requires less time to send.
//  Note that no direction is provided; motion occurs through shortest
//  path. If a direction is required, use GoTo_DIR().
void AutoDriver::goMark()
{
    SPIXfer(GO_MARK);
}

// setMark() and setHome() allow the user to define new MARK or
//  ABS_POS values.
void AutoDriver::setMark(long newMark)
{
    setParam(MARK, newMark);
}

void AutoDriver::setPos(long newPos)
{
    setParam(ABS_POS, newPos);
}

// Sets the ABS_POS register to 0, effectively declaring the current
//  position to be "HOME".
void AutoDriver::resetPos()
{
    SPIXfer(RESET_POS);
}

// Reset device to power up conditions. Equivalent to toggling the STBY
//  pin or cycling power.
void AutoDriver::resetDev()
{
    SPIXfer(RESET_DEVICE);
}

// Bring the motor to a halt using the deceleration curve.
void AutoDriver::softStop()
{
    SPIXfer(SOFT_STOP);
}

// Stop the motor with infinite deceleration.
void AutoDriver::hardStop()
{
    SPIXfer(HARD_STOP);
}

// Decelerate the motor and put the bridges in Hi-Z state.
void AutoDriver::softHiZ()
{
    SPIXfer(SOFT_HIZ);
}

// Put the bridges in Hi-Z state immediately with no deceleration.
void AutoDriver::hardHiZ()
{
    SPIXfer(HARD_HIZ);
}

// Fetch and return the 16-bit value in the STATUS register. Resets
//  any warning flags and exits any error states. Using GetParam()
//  to read STATUS does not clear these values.
int AutoDriver::getStatus()
{
    int temp = 0;
    unsigned char* bytePointer = (unsigned char*)&temp;
    SPIXfer(GET_STATUS);
    bytePointer[1] = SPIXfer(0);
    bytePointer[0] = SPIXfer(0);
    return temp;
}

// Setup the SYNC/BUSY pin to be either SYNC or BUSY, and to a desired
//  ticks per step level.
void AutoDriver::configSyncPin(unsigned char pinFunc, unsigned char syncSteps)
{
    // Only some of the bits in this register are of interest to us; we need to
    //  clear those bits. It happens that they are the upper four.
    unsigned char syncPinConfig = (unsigned char)getParam(STEP_MODE);
    syncPinConfig &= 0x0F;

    // Now, let's OR in the arguments. We're going to mask the incoming
    //  data to avoid touching any bits that aren't appropriate. See datasheet
    //  for more info about which bits we're touching.
    syncPinConfig |= ((pinFunc & 0x80) | (syncSteps & 0x70));

    // Now we should be able to send that byte right back to the dSPIN- it
    //  won't corrupt the other bits, and the changes are made.
    setParam(STEP_MODE, (unsigned long)syncPinConfig);
}

// The dSPIN chip supports microstepping for a smoother ride. This function
//  provides an easy front end for changing the microstepping mode.
void AutoDriver::configStepMode(unsigned char stepMode)
{

    // Only some of these bits are useful (the lower three). We'll extract the
    //  current contents, clear those three bits, then set them accordingly.
    unsigned char stepModeConfig = (unsigned char)getParam(STEP_MODE);
    stepModeConfig &= 0xF8;

    // Now we can OR in the new bit settings. Mask the argument so we don't
    //  accidentally the other bits, if the user sends us a non-legit value.
    stepModeConfig |= (stepMode & 0x07);

    // Now push the change to the chip.
    setParam(STEP_MODE, (unsigned long)stepModeConfig);
}

unsigned char AutoDriver::getStepMode() {
    return (unsigned char)(getParam(STEP_MODE) & 0x07);
}

// This is the maximum speed the dSPIN will attempt to produce.
void AutoDriver::setMaxSpeed(float stepsPerSecond)
{
    // We need to convert the floating point stepsPerSecond into a value that
    //  the dSPIN can understand. Fortunately, we have a function to do that.
    unsigned long integerSpeed = maxSpdCalc(stepsPerSecond);

    // Now, we can set that paramter.
    setParam(MAX_SPEED, integerSpeed);
}


float AutoDriver::getMaxSpeed()
{
    return maxSpdParse(getParam(MAX_SPEED));
}

// Set the minimum speed allowable in the system. This is the speed a motion
//  starts with; it will then ramp up to the designated speed or the max
//  speed, using the acceleration profile.
void AutoDriver::setMinSpeed(float stepsPerSecond)
{
    // We need to convert the floating point stepsPerSecond into a value that
    //  the dSPIN can understand. Fortunately, we have a function to do that.
    unsigned long integerSpeed = minSpdCalc(stepsPerSecond);

    // MIN_SPEED also contains the LSPD_OPT flag, so we need to protect that.
    unsigned long temp = getParam(MIN_SPEED) & 0x00001000;

    // Now, we can set that paramter.
    setParam(MIN_SPEED, integerSpeed | temp);
}

float AutoDriver::getMinSpeed()
{
    return minSpdParse(getParam(MIN_SPEED));
}

// Above this threshold, the dSPIN will cease microstepping and go to full-step
//  mode. 
void AutoDriver::setFullSpeed(float stepsPerSecond)
{
    unsigned long integerSpeed = FSCalc(stepsPerSecond);
    setParam(FS_SPD, integerSpeed);
}

float AutoDriver::getFullSpeed()
{
    return FSParse(getParam(FS_SPD));
}

// Set the acceleration rate, in steps per second per second. This value is
//  converted to a dSPIN friendly value. Any value larger than 29802 will
//  disable acceleration, putting the chip in "infinite" acceleration mode.
void AutoDriver::setAcc(float stepsPerSecondPerSecond)
{
    unsigned long integerAcc = accCalc(stepsPerSecondPerSecond);
    setParam(ACC, integerAcc);
}

float AutoDriver::getAcc()
{
    return accParse(getParam(ACC));
}

// Same rules as setAcc().
void AutoDriver::setDec(float stepsPerSecondPerSecond)
{
    unsigned long integerDec = decCalc(stepsPerSecondPerSecond);
    setParam(DECEL, integerDec);
}

float AutoDriver::getDec()
{
    return accParse(getParam(DECEL));
}

void AutoDriver::setOCThreshold(unsigned char threshold)
{
    setParam(OCD_TH, 0x0F & threshold);
}

unsigned char AutoDriver::getOCThreshold()
{
    return (unsigned char)(getParam(OCD_TH) & 0xF);
}

// The next few functions are all breakouts for individual options within the
//  single register CONFIG. We'll read CONFIG, blank some bits, then OR in the
//  new value.

// This is a multiplier/divider setup for the PWM frequency when microstepping.
//  Divisors of 1-7 are available; multipliers of .625-2 are available. See
//  datasheet for more details; it's not clear what the frequency being
//  multiplied/divided here is, but it is clearly *not* the actual clock freq.
void AutoDriver::setPWMFreq(int divisor, int multiplier)
{
    unsigned long configVal = getParam(CONFIG);

    // The divisor is set by config 15:13, so mask 0xE000 to clear them.
    configVal &= ~(0xE000);
    // The multiplier is set by config 12:10; mask is 0x1C00
    configVal &= ~(0x1C00);
    // Now we can OR in the masked-out versions of the values passed in.
    configVal |= ((0xE000 & divisor) | (0x1C00 & multiplier));
    setParam(CONFIG, configVal);
}

int AutoDriver::getPWMFreqDivisor()
{
    return (int)(getParam(CONFIG) & 0xE000);
}

int AutoDriver::getPWMFreqMultiplier()
{
    return (int)(getParam(CONFIG) & 0x1C00);
}

// Slew rate of the output in V/us. Can be 180, 290, or 530.
void AutoDriver::setSlewRate(int slewRate)
{
    unsigned long configVal = getParam(CONFIG);

    // These bits live in CONFIG 9:8, so the mask is 0x0300.
    configVal &= ~(0x0300);
    //Now, OR in the masked incoming value.
    configVal |= (0x0300 & slewRate);
    setParam(CONFIG, configVal);
}

int AutoDriver::getSlewRate()
{
    return (int)(getParam(CONFIG) & 0x0300);
}

// Single bit- do we shutdown the drivers on overcurrent or not?
void AutoDriver::setOCShutdown(int OCShutdown)
{
    unsigned long configVal = getParam(CONFIG);
    // This bit is CONFIG 7, mask is 0x0080
    configVal &= ~(0x0080);
    //Now, OR in the masked incoming value.
    configVal |= (0x0080 & OCShutdown);
    setParam(CONFIG, configVal);
}

int AutoDriver::getOCShutdown()
{
    return (int)(getParam(CONFIG) & 0x0080);
}

// Enable motor voltage compensation? Not at all straightforward- check out
//  p34 of the datasheet.
void AutoDriver::setVoltageComp(int vsCompMode)
{
    unsigned long configVal = getParam(CONFIG);
    // This bit is CONFIG 5, mask is 0x0020
    configVal &= ~(0x0020);
    //Now, OR in the masked incoming value.
    configVal |= (0x0020 & vsCompMode);
    setParam(CONFIG, configVal);
}

int AutoDriver::getVoltageComp()
{
    return (int)(getParam(CONFIG) & 0x0020);
}

// The switch input can either hard-stop the driver _or_ activate an interrupt.
//  This bit allows you to select what it does.
void AutoDriver::setSwitchMode(int switchMode)
{
    unsigned long configVal = getParam(CONFIG);
    // This bit is CONFIG 4, mask is 0x0010
    configVal &= ~(0x0010);
    //Now, OR in the masked incoming value.
    configVal |= (0x0010 & switchMode);
    setParam(CONFIG, configVal);
}

int AutoDriver::getSwitchMode()
{
    return (int)(getParam(CONFIG) & 0x0010);
}

// There are a number of clock options for this chip- it can be configured to
//  accept a clock, drive a crystal or resonator, and pass or not pass the
//  clock signal downstream. Theoretically, you can use pretty much any
//  frequency you want to drive it; practically, this library assumes it's
//  being driven at 16MHz. Also, the device will use these bits to set the
//  math used to figure out steps per second and stuff like that.
void AutoDriver::setOscMode(int oscillatorMode)
{
    unsigned long configVal = getParam(CONFIG);
    // These bits are CONFIG 3:0, mask is 0x000F
    configVal &= ~(0x000F);
    //Now, OR in the masked incoming value.
    configVal |= (0x000F & oscillatorMode);
    setParam(CONFIG, configVal);
}

int AutoDriver::getOscMode()
{
    return (int)(getParam(CONFIG) & 0x000F);
}

// The KVAL registers are...weird. I don't entirely understand how they differ
//  from the microstepping, but if you have trouble getting the motor to run,
//  tweaking KVAL has proven effective in the past. There's a separate register
//  for each case: running, static, accelerating, and decelerating.

void AutoDriver::setAccKVAL(unsigned char kvalInput)
{
    setParam(KVAL_ACC, kvalInput);
}

unsigned char AutoDriver::getAccKVAL()
{
    return (unsigned char)getParam(KVAL_ACC);
}

void AutoDriver::setDecKVAL(unsigned char kvalInput)
{
    setParam(KVAL_DEC, kvalInput);
}

unsigned char AutoDriver::getDecKVAL()
{
    return (unsigned char)getParam(KVAL_DEC);
}

void AutoDriver::setRunKVAL(unsigned char kvalInput)
{
    setParam(KVAL_RUN, kvalInput);
}

unsigned char AutoDriver::getRunKVAL()
{
    return (unsigned char)getParam(KVAL_RUN);
}

void AutoDriver::setHoldKVAL(unsigned char kvalInput)
{
    setParam(KVAL_HOLD, kvalInput);
}

unsigned char AutoDriver::getHoldKVAL()
{
    return (unsigned char)getParam(KVAL_HOLD);
}

// Enable or disable the low-speed optimization option. With LSPD_OPT enabled,
//  motion starts from 0 instead of MIN_SPEED and low-speed optimization keeps
//  the driving sine wave prettier than normal until MIN_SPEED is reached.
void AutoDriver::setLoSpdOpt(bool enable)
{
    unsigned long temp = getParam(MIN_SPEED);
    if (enable) temp |= 0x00001000; // Set the LSPD_OPT bit
    else        temp &= 0xffffefff; // Clear the LSPD_OPT bit
    setParam(MIN_SPEED, temp);
}

bool AutoDriver::getLoSpdOpt()
{
    return (bool)((getParam(MIN_SPEED) & 0x00001000) != 0);
}

// AutoDriverSupport.cpp - Contains utility functions for converting real-world 
//  units (eg, steps/s) to values usable by the dsPIN controller. These are all
//  private members of class AutoDriver.

// The value in the ACC register is [(steps/s/s)*(tick^2)]/(2^-40) where tick is 
//  250ns (datasheet value)- 0x08A on boot.
// Multiply desired steps/s/s by .137438 to get an appropriate value for this register.
// This is a 12-bit value, so we need to make sure the value is at or below 0xFFF.
unsigned long AutoDriver::accCalc(float stepsPerSecPerSec)
{
    float temp = stepsPerSecPerSec * 0.137438;
    if ((unsigned long) long(temp) > 0x00000FFF) return 0x00000FFF;
    else return (unsigned long) long(temp);
}


float AutoDriver::accParse(unsigned long stepsPerSecPerSec)
{
    return (float)(stepsPerSecPerSec & 0x00000FFF) / 0.137438;
}

// The calculation for DEC is the same as for ACC. Value is 0x08A on boot.
// This is a 12-bit value, so we need to make sure the value is at or below 0xFFF.
unsigned long AutoDriver::decCalc(float stepsPerSecPerSec)
{
    float temp = stepsPerSecPerSec * 0.137438;
    if ((unsigned long) long(temp) > 0x00000FFF) return 0x00000FFF;
    else return (unsigned long) long(temp);
}

float AutoDriver::decParse(unsigned long stepsPerSecPerSec)
{
    return (float)(stepsPerSecPerSec & 0x00000FFF) / 0.137438;
}

// The value in the MAX_SPD register is [(steps/s)*(tick)]/(2^-18) where tick is 
//  250ns (datasheet value)- 0x041 on boot.
// Multiply desired steps/s by .065536 to get an appropriate value for this register
// This is a 10-bit value, so we need to make sure it remains at or below 0x3FF
unsigned long AutoDriver::maxSpdCalc(float stepsPerSec)
{
    unsigned long temp = ceil(stepsPerSec * .065536);
    if (temp > 0x000003FF) return 0x000003FF;
    else return temp;
}


float AutoDriver::maxSpdParse(unsigned long stepsPerSec)
{
    return (float)(stepsPerSec & 0x000003FF) / 0.065536;
}

// The value in the MIN_SPD register is [(steps/s)*(tick)]/(2^-24) where tick is 
//  250ns (datasheet value)- 0x000 on boot.
// Multiply desired steps/s by 4.1943 to get an appropriate value for this register
// This is a 12-bit value, so we need to make sure the value is at or below 0xFFF.
unsigned long AutoDriver::minSpdCalc(float stepsPerSec)
{
    float temp = stepsPerSec / 0.238;
    if ((unsigned long) long(temp) > 0x00000FFF) return 0x00000FFF;
    else return (unsigned long) long(temp);
}

float AutoDriver::minSpdParse(unsigned long stepsPerSec)
{
    return (float)((stepsPerSec & 0x00000FFF) * 0.238);
}

// The value in the FS_SPD register is ([(steps/s)*(tick)]/(2^-18))-0.5 where tick is 
//  250ns (datasheet value)- 0x027 on boot.
// Multiply desired steps/s by .065536 and subtract .5 to get an appropriate value for this register
// This is a 10-bit value, so we need to make sure the value is at or below 0x3FF.
unsigned long AutoDriver::FSCalc(float stepsPerSec)
{
    float temp = (stepsPerSec * .065536) - .5;
    if ((unsigned long) long(temp) > 0x000003FF) return 0x000003FF;
    else return (unsigned long) long(temp);
}

float AutoDriver::FSParse(unsigned long stepsPerSec)
{
    return (((float)(stepsPerSec & 0x000003FF)) + 0.5) / 0.065536;
}

// The value in the INT_SPD register is [(steps/s)*(tick)]/(2^-24) where tick is 
//  250ns (datasheet value)- 0x408 on boot.
// Multiply desired steps/s by 4.1943 to get an appropriate value for this register
// This is a 14-bit value, so we need to make sure the value is at or below 0x3FFF.
unsigned long AutoDriver::intSpdCalc(float stepsPerSec)
{
    float temp = stepsPerSec * 4.1943;
    if ((unsigned long) long(temp) > 0x00003FFF) return 0x00003FFF;
    else return (unsigned long) long(temp);
}

float AutoDriver::intSpdParse(unsigned long stepsPerSec)
{
    return (float)(stepsPerSec & 0x00003FFF) / 4.1943;
}

// When issuing RUN command, the 20-bit speed is [(steps/s)*(tick)]/(2^-28) where tick is 
//  250ns (datasheet value).
// Multiply desired steps/s by 67.106 to get an appropriate value for this register
// This is a 20-bit value, so we need to make sure the value is at or below 0xFFFFF.
unsigned long AutoDriver::spdCalc(float stepsPerSec)
{
    unsigned long temp = stepsPerSec * 67.106;
    if (temp > 0x000FFFFF) return 0x000FFFFF;
    else return temp;
}

float AutoDriver::spdParse(unsigned long stepsPerSec)
{
    return (float)(stepsPerSec & 0x000FFFFF) / 67.106;
}

// Much of the functionality between "get parameter" and "set parameter" is
//  very similar, so we deal with that by putting all of it in one function
//  here to save memory space and simplify the program.
long AutoDriver::paramHandler(unsigned char param, unsigned long value)
{
    long retVal = 0;   // This is a temp for the value to return.

    // This switch structure handles the appropriate action for each register.
    //  This is necessary since not all registers are of the same length, either
    //  bit-wise or byte-wise, so we want to make sure we mask out any spurious
    //  bits and do the right number of transfers. That is handled by the xferParam()
    //  function, in most cases, but for 1-byte or smaller transfers, we call
    //  SPIXfer() directly.
    switch (param)
    {
        // ABS_POS is the current absolute offset from home. It is a 22 bit number expressed
        //  in two's complement. At power up, this value is 0. It cannot be written when
        //  the motor is running, but at any other time, it can be updated to change the
        //  interpreted position of the motor.
    case ABS_POS:
        retVal = xferParam(value, 22);
        break;
        // EL_POS is the current electrical position in the step generation cycle. It can
        //  be set when the motor is not in motion. Value is 0 on power up.
    case EL_POS:
        retVal = xferParam(value, 9);
        break;
        // MARK is a second position other than 0 that the motor can be told to go to. As
        //  with ABS_POS, it is 22-bit two's complement. Value is 0 on power up.
    case MARK:
        retVal = xferParam(value, 22);
        break;
        // SPEED contains information about the current speed. It is read-only. It does 
        //  NOT provide direction information.
    case SPEED:
        retVal = xferParam(0, 20);
        break;
        // ACC and DEC set the acceleration and deceleration rates. Set ACC to 0xFFF 
        //  to get infinite acceleration/decelaeration- there is no way to get infinite
        //  deceleration w/o infinite acceleration (except the HARD STOP command).
        //  Cannot be written while motor is running. Both default to 0x08A on power up.
        // AccCalc() and DecCalc() functions exist to convert steps/s/s values into
        //  12-bit values for these two registers.
    case ACC:
        retVal = xferParam(value, 12);
        break;
    case DECEL:
        retVal = xferParam(value, 12);
        break;
        // MAX_SPEED is just what it says- any command which attempts to set the speed
        //  of the motor above this value will simply cause the motor to turn at this
        //  speed. Value is 0x041 on power up.
        // MaxSpdCalc() function exists to convert steps/s value into a 10-bit value
        //  for this register.
    case MAX_SPEED:
        retVal = xferParam(value, 10);
        break;
        // MIN_SPEED controls two things- the activation of the low-speed optimization
        //  feature and the lowest speed the motor will be allowed to operate at. LSPD_OPT
        //  is the 13th bit, and when it is set, the minimum allowed speed is automatically
        //  set to zero. This value is 0 on startup.
        // MinSpdCalc() function exists to convert steps/s value into a 12-bit value for this
        //  register. SetLSPDOpt() function exists to enable/disable the optimization feature.
    case MIN_SPEED:
        retVal = xferParam(value, 13);
        break;
        // FS_SPD register contains a threshold value above which microstepping is disabled
        //  and the dSPIN operates in full-step mode. Defaults to 0x027 on power up.
        // FSCalc() function exists to convert steps/s value into 10-bit integer for this
        //  register.
    case FS_SPD:
        retVal = xferParam(value, 10);
        break;
        // KVAL is the maximum voltage of the PWM outputs. These 8-bit values are ratiometric
        //  representations: 255 for full output voltage, 128 for half, etc. Default is 0x29.
        // The implications of different KVAL settings is too complex to dig into here, but
        //  it will usually work to max the value for RUN, ACC, and DEC. Maxing the value for
        //  HOLD may result in excessive power dissipation when the motor is not running.
    case KVAL_HOLD:
        retVal = xferParam(value, 8);
        break;
    case KVAL_RUN:
        retVal = xferParam(value, 8);
        break;
    case KVAL_ACC:
        retVal = xferParam(value, 8);
        break;
    case KVAL_DEC:
        retVal = xferParam(value, 8);
        break;
        // INT_SPD, ST_SLP, FN_SLP_ACC and FN_SLP_DEC are all related to the back EMF
        //  compensation functionality. Please see the datasheet for details of this
        //  function- it is too complex to discuss here. Default values seem to work
        //  well enough.
    case INT_SPD:
        retVal = xferParam(value, 14);
        break;
    case ST_SLP:
        retVal = xferParam(value, 8);
        break;
    case FN_SLP_ACC:
        retVal = xferParam(value, 8);
        break;
    case FN_SLP_DEC:
        retVal = xferParam(value, 8);
        break;
        // K_THERM is motor winding thermal drift compensation. Please see the datasheet
        //  for full details on operation- the default value should be okay for most users.
    case K_THERM:
        value &= 0x0F;
        retVal = xferParam(value, 8);
        break;
        // ADC_OUT is a read-only register containing the result of the ADC measurements.
        //  This is less useful than it sounds; see the datasheet for more information.
    case ADC_OUT:
        retVal = xferParam(value, 8);
        break;
        // Set the overcurrent threshold. Ranges from 375mA to 6A in steps of 375mA.
        //  A set of defined constants is provided for the user's convenience. Default
        //  value is 3.375A- 0x08. This is a 4-bit value.
    case OCD_TH:
        value &= 0x0F;
        retVal = xferParam(value, 8);
        break;
        // Stall current threshold. Defaults to 0x40, or 2.03A. Value is from 31.25mA to
        //  4A in 31.25mA steps. This is a 7-bit value.
    case STALL_TH:
        value &= 0x7F;
        retVal = xferParam(value, 8);
        break;
        // STEP_MODE controls the microstepping settings, as well as the generation of an
        //  output signal from the dSPIN. Bits 2:0 control the number of microsteps per
        //  step the part will generate. Bit 7 controls whether the BUSY/SYNC pin outputs
        //  a BUSY signal or a step synchronization signal. Bits 6:4 control the frequency
        //  of the output signal relative to the full-step frequency; see datasheet for
        //  that relationship as it is too complex to reproduce here.
        // Most likely, only the microsteps per step value will be needed; there is a set
        //  of constants provided for ease of use of these values.
    case STEP_MODE:
        retVal = xferParam(value, 8);
        break;
        // ALARM_EN controls which alarms will cause the FLAG pin to fall. A set of constants
        //  is provided to make this easy to interpret. By default, ALL alarms will trigger the
        //  FLAG pin.
    case ALARM_EN:
        retVal = xferParam(value, 8);
        break;
        // CONFIG contains some assorted configuration bits and fields. A fairly comprehensive
        //  set of reasonably self-explanatory constants is provided, but users should refer
        //  to the datasheet before modifying the contents of this register to be certain they
        //  understand the implications of their modifications. Value on boot is 0x2E88; this
        //  can be a useful way to verify proper start up and operation of the dSPIN chip.
    case CONFIG:
        retVal = xferParam(value, 16);
        break;
        // STATUS contains read-only information about the current condition of the chip. A
        //  comprehensive set of constants for masking and testing this register is provided, but
        //  users should refer to the datasheet to ensure that they fully understand each one of
        //  the bits in the register.
    case STATUS:  // STATUS is a read-only register
        retVal = xferParam(0, 16);;
        break;
    default:
        SPIXfer((unsigned char)value);
        break;
    }
    return retVal;
}

// Generalization of the subsections of the register read/write functionality.
//  We want the end user to just write the value without worrying about length,
//  so we pass a bit length parameter from the calling function.
long AutoDriver::xferParam(unsigned long value, unsigned char bitLen)
{
    unsigned char byteLen = bitLen / 8;      // How many BYTES do we have?
    if (bitLen % 8 > 0) byteLen++;  // Make sure not to lose any partial byte values.

    unsigned char temp;

    unsigned long retVal = 0;

    for (int i = 0; i < byteLen; i++)
    {
        retVal = retVal << 8;
        temp = SPIXfer((unsigned char)(value >> ((byteLen - i - 1) * 8)));
        retVal |= temp;
    }

    unsigned long mask = 0xffffffff >> (32 - bitLen);
    return retVal & mask;
}

unsigned char AutoDriver::SPIXfer(unsigned char data)
{
    _SPI->TransferData(&data, 1);
}