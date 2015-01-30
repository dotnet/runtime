//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
//

/*=====================================================================
**
** Source: test2.c
**
** Purpose: Ensure that DosDateTimeToFileTime fails when passed invalid
** dos dates and times. 
**
**
**===================================================================*/

/* Note: Passing a NULL FILETIME causes the function to fault. 
   Also -- you can't create an invalid 'year' in the date. 
*/

#include <palsuite.h>

int DoInvalidTest(WORD TheDate, WORD TheTime, char* DateOrTime, char* Wrong)
{
    FILETIME ResultTime;
    
    if(DosDateTimeToFileTime(TheDate, TheTime, &ResultTime) != 0)
    {
        Trace("ERROR: DosTimeToFileTime should have failed when passed a "
              "%s with invalid %s.\n", DateOrTime, Wrong);
        return 1;
    }

    if(GetLastError() != ERROR_INVALID_PARAMETER)
    {
        Trace("ERROR: GetLastError should have returned "
              "ERROR_INVALID_PARAMETER but it returned %d.\n",GetLastError());
        return 1;
    }
    
    return 0;

}

int __cdecl main(int argc, char **argv)
{
    
    WORD DosDate = 0x14CF;              /* Dec 15th, 2000 */
    WORD InvalidMonthDosDate = 0x29EF;  /* 15th Month, 15th Day, 2000 */
    WORD InvalidDayDosDate = 0x2820;    /* Jan 0th, 2000 */

    WORD DosTime = 0x55AF;              /* 10:45:30 */
    WORD InvalidSecondDosTime = 0x55BF; /* 10:45:62 */
    WORD InvalidMinuteDosTime = 0xF81;  /* 1:60:02 */
    WORD InvalidHourDosTime = 0xC021;   /* 24:01:02 */

    if (0 != PAL_Initialize(argc,argv))
    {
        return FAIL;
    }
    
    if(
        DoInvalidTest(DosDate, InvalidSecondDosTime, "time", "seconds") ||
        DoInvalidTest(DosDate, InvalidMinuteDosTime, "time", "minutes") ||
        DoInvalidTest(DosDate, InvalidHourDosTime, "time", "hours") ||
        DoInvalidTest(InvalidMonthDosDate, DosTime, "date", "month") ||
        DoInvalidTest(InvalidDayDosDate, DosTime, "date", "day"))
    {
        Fail("");
    }
    
    PAL_Terminate();
    return PASS;
}

