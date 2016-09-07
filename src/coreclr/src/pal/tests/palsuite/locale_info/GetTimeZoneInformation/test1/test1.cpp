// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*============================================================================
**
** Source: test1.c
**
** Purpose: Tests that GetTimeZoneInformation gives reasonable values.
**
**
**==========================================================================*/


#include <palsuite.h>

int __cdecl main(int argc, char *argv[])
{
    TIME_ZONE_INFORMATION tzi;
    DWORD ret;

    if (PAL_Initialize(argc, argv))
    {
        return FAIL;
    }

    ret = GetTimeZoneInformation(&tzi);
    if (ret == TIME_ZONE_ID_UNKNOWN) 
    {        
        /* Occurs in time zones that do not use daylight savings time. */
        if (tzi.StandardBias != 0)
        {
            Fail("GetTimeZoneInformation() gave invalid data!\n"
                "Returned TIME_ZONE_ID_UNKNOWN but StandardBias != 0!\n");
        }
        if (tzi.DaylightBias != 0)
        {
            Fail("GetTimeZoneInformation() gave invalid data!\n"
                "Returned TIME_ZONE_ID_UNKNOWN but DaylightBias != 0!\n");
        }
    }    
    else if (ret == TIME_ZONE_ID_STANDARD)
    {
        if (tzi.StandardBias != 0)
        {
            Fail("GetTimeZoneInformation() gave invalid data!\n"
                "StandardBias is %d, should be 0!\n", tzi.StandardBias);
        }
    }
    else if (ret == TIME_ZONE_ID_DAYLIGHT)
    {
        if (tzi.DaylightBias != -60 && tzi.DaylightBias != 0)
        {
            Fail("GetTimeZoneInformation() gave invalid data!\n"
                "DaylightBias is %d, should be 0 or -60!\n", tzi.DaylightBias);
        }
    }
    else 
    {
        Fail("GetTimeZoneInformation() returned an invalid value!\n");
    }

    if (tzi.Bias % 30 != 0)
    {
        Fail("GetTimeZoneInformation() gave an invalid bias of %d!\n", 
            tzi.Bias);
    }

    PAL_Terminate();

    return PASS;
}

