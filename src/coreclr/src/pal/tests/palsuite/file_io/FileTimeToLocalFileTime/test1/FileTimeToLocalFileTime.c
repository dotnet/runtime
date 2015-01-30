//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
//

/*=====================================================================
**
** Source:  FileTimeToLocalFileTime.c
**
** Purpose: Tests the PAL implementation of the FileTimeToLocalFileTime
** This test converts a FILETIME to LOCALTIME, and after finding the correct
** delta based on the timezone, it ensures that the difference between 
** FILETIME and LOCALTIME is correct.
**
** Depends
**       GetTimeZoneInformation
**
**
**===================================================================*/

#include <palsuite.h>



int __cdecl main(int argc, char **argv)
{

    FILETIME UTCTime, LocalTime;
    ULONG64 FullFileTime, FullLocalTime, CorrectTime;
    TIME_ZONE_INFORMATION ZoneInfo;
    int DeltaBetweenLocalAndUTC;
    int result;

    if (0 != PAL_Initialize(argc,argv))
    {
        return FAIL;
    }

    /* This is a valid UTC file time generated with GetFileTime */
    UTCTime.dwLowDateTime = 1867954880;
    UTCTime.dwHighDateTime = 29437095;
  
    /* Call the function */
    result = FileTimeToLocalFileTime(&UTCTime,&LocalTime);
  
    if(result == 0) 
    {
        Fail("ERROR: FileTimeToLocalTime has returned zero, which "
               "indicates that it failed.");
    }

    /* We need to get the time zone that the user is running in. */
    result = GetTimeZoneInformation(&ZoneInfo);

    /* Use the Time Zone Information to construct the delta between UTC
       and local time -- in hours.
    */
    
    if(result == TIME_ZONE_ID_STANDARD)
    {
        DeltaBetweenLocalAndUTC = 
            (ZoneInfo.Bias + ZoneInfo.StandardBias);  
    }
    else if (result == TIME_ZONE_ID_DAYLIGHT) 
    {
        DeltaBetweenLocalAndUTC = 
            (ZoneInfo.Bias + ZoneInfo.DaylightBias); 
    }
    else 
    {
        DeltaBetweenLocalAndUTC = (ZoneInfo.Bias);
    }
 
    /* Change the UTC and Local FILETIME structures into ULONG64 
       types 
    */
  
    FullFileTime = ((((ULONG64)UTCTime.dwHighDateTime)<<32) | 
                    ((ULONG64)UTCTime.dwLowDateTime));
  
    FullLocalTime = ((((ULONG64)LocalTime.dwHighDateTime)<<32) | 
                     ((ULONG64)LocalTime.dwLowDateTime));

    /* This magic number is 10000000 * 60 * 60 -- which is the
       number of 100s of Nanseconds in a second, multiplied by the number
       of seconds in a minute, multiplied by the number of minutes in an
       hour.
     
       The correct time is the delta in hundreds of nanoseconds between
       Local and UTC times.
    */
  
    CorrectTime = 600000000 * ((ULONG64)DeltaBetweenLocalAndUTC);
  
    /* Now check to ensure that the difference between the Local and UTC
       times that was calculated with the function equals what it should be.
    */
    if((FullFileTime - FullLocalTime) != CorrectTime)
    {
        Fail("ERROR: The LocalFileTime that was returned is not equal to "
               "what the LocalFileTime should have been.");
    } 
  
    PAL_Terminate();
    return PASS;
}

