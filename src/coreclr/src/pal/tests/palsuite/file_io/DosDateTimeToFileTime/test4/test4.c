//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
//

/*=====================================================================
**
** Source: test4.c
**
** Purpose: Create a DOS time near 1980 and one near the current date.
** Convert these two a FILETIME and then compare the values to ensure they're
** correct -- this should test days, years, leap years etc.
**
**
**===================================================================*/

#include <palsuite.h>

int __cdecl main(int argc, char **argv)
{
    WORD EarlyDosDate = 0x21;         /* Jan, 1st, 1980 */
    WORD RecentDosDate = 0x14CF;      /* Dec 15th, 2000 */
    WORD DosTime = 0x55AF;            /* 10:45:30       */
    WORD SecondDosTime = 0xBF7D;      /* 23:59:58 */
    
    FILETIME FirstResultTime;
    FILETIME SecondResultTime;
    
    ULONG64 FullFirstTime;
    ULONG64 FullSecondTime;
    
    if (0 != PAL_Initialize(argc,argv))
    {
        return FAIL;
    }
    
    /* Convert two DosDateTimes to FileTimes, ensure that each call returns
       non-zero
    */
    if(DosDateTimeToFileTime(EarlyDosDate, DosTime, &FirstResultTime) == 0)
    {
        Fail("ERROR: DosTimeToFileTime should have returned non-0 to indicate "
             "success.  GetLastError() returned %d.",GetLastError());
    }
    
    if(DosDateTimeToFileTime(RecentDosDate, 
                             SecondDosTime, &SecondResultTime) == 0)
    {
        Fail("ERROR: DosTimeToFileTime should have returned non-0 to indicate "
             "success.  GetLastError() returned %d.",GetLastError());
    }

    /* Move the FILETIME structures into a ULONG value which we can 
       work with
    */
    FullFirstTime = ((((ULONG64)FirstResultTime.dwHighDateTime)<<32) | 
                    ((ULONG64)FirstResultTime.dwLowDateTime));
    
    FullSecondTime = ((((ULONG64)SecondResultTime.dwHighDateTime)<<32) | 
                      ((ULONG64)SecondResultTime.dwLowDateTime));
    
    /* 
       The magic number below was calculated under win32 and is assumed
       to be correct.  That's the value between the two dates above.  
       Check to ensure this is always true.
    */
    
    if((FullSecondTime-FullFirstTime) != UI64(3299228680000000))
    {
        Fail("ERROR: The two dates should have been "
             "3299228680000000 nanseconds apart, but the result "
             "returned was %I64d.\n",FullSecondTime-FullFirstTime);
    }

    PAL_Terminate();
    return PASS;
}

