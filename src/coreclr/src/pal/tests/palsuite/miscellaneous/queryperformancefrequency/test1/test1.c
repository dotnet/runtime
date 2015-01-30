//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
//

/*============================================================
**
** Source: test1.c
**
** Purpose: Test for QueryPerformanceFrequency function
**
**
**=========================================================*/

#include <palsuite.h>

int __cdecl main(int argc, char *argv[])
{

    LARGE_INTEGER Freq;

    /* Initialize the PAL.
     */

    if(0 != (PAL_Initialize(argc, argv)))
    {
        return FAIL;
    }

    /* Check the return value of the performance 
     * frequency, a value of zero indicates that 
     * either the call has failed or the 
     * high-resolution performance counter is not
     * installed.
     */
    if (!QueryPerformanceFrequency(&Freq))
    {
        
        Fail("ERROR:%u:Unable to retrieve the frequency of the "
             "high-resolution performance counter.\n", 
             GetLastError());
    }
    
    
    /* Check the return value the frequency the
     * value should be non-zero.
     */
    if (Freq.QuadPart == 0)
    {

        Fail("ERROR: The frequency has been determined to be 0 "
             "the frequency should be non-zero.\n");

    }
    
    /* Terminate the PAL.
     */  
    PAL_Terminate();
    return PASS;
}
