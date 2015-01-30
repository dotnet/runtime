//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
//

/*============================================================
**
** Source:  
**
** Source : test1.c
**
** Purpose: Test for GetTickCount() function
**
**
**=========================================================*/

#include <palsuite.h>

int __cdecl main(int argc, char *argv[]) {

    DWORD FirstCount = 0;
    DWORD SecondCount = 0;

    /*
     * Initialize the PAL and return FAILURE if this fails
     */

    if(0 != (PAL_Initialize(argc, argv)))
    {
        return FAIL;
    }

    /* Grab a FirstCount, then loop for a bit to make the clock increase */
    FirstCount = GetTickCount();
  
    /* Make sure some time passes */
	Sleep(60); //Since the get tick count is accurate within 55 milliseconds.

    /* Get a second count */
    SecondCount = GetTickCount();

    /* Make sure the second one is bigger than the first. 
       This isn't the best test, but it at least shows that it's returning a
       DWORD which is increasing.
    */
  
    if(FirstCount >= SecondCount) 
    {
        Fail("ERROR: The first time (%d) was greater/equal than the second time "
             " (%d).  The tick count should have increased.\n",
             FirstCount,SecondCount);
    }
    
    PAL_Terminate();
    return PASS;
}



