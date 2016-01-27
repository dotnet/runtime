// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*=====================================================================
**
** Source:  CompareFileTime.c
**
** Purpose: Tests the PAL implementation of the CompareFileTime function.
** Defines a large and small file time, and compares them in all fashions
** to ensure proper return values.
**
**
**===================================================================*/

#include <palsuite.h>



int __cdecl main(int argc, char **argv)
{

    FILETIME SmallTime, BigTime;

    if (0 != PAL_Initialize(argc,argv))
    {
        return FAIL;
    }

    /* Set a Big and Small time.  These were generated using
       GetFileTime on a file, with a recent creation time and an old
       modify time, to get a bigger and smaller value.
    */
    
    BigTime.dwLowDateTime = -755748832;
    BigTime.dwHighDateTime = 29436941;

    SmallTime.dwLowDateTime = -459754240;
    SmallTime.dwHighDateTime = 29436314;

    /* Check to see that SmallTime is less than Big Time */

    if(CompareFileTime(&SmallTime,&BigTime) != -1)
    {
        Fail("ERROR: The first time is less than the second time, so "
               "-1 should have been returned to indicate this.");
    }

    /* Check that BigTime is greater than SmallTime */

    if(CompareFileTime(&BigTime,&SmallTime) != 1)
    {
        Fail("ERROR: The first time is greater than the second time, so "
               "1 should have been returned to indicate this.");
    }

    /* Check that BigTime is equal to BigTime */

    if(CompareFileTime(&BigTime,&BigTime) != 0)
    {
        Fail("ERROR: The first time is equal to the second time, so "
               "0 should have been returned to indicate this.");
    }
    
    PAL_Terminate();
    return PASS;
}

