//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
//

/*=====================================================================
**
** Source:  test1.c (SetErrorMode)
**
** Purpose: Tests the PAL implementation of the SetErrorMode function.
**          This test will set the error mode and then read the error
**          set with GetLastError().
**
**
**===================================================================*/

#include <palsuite.h>

int __cdecl main(int argc, char **argv)
{   
    DWORD dErrorReturn;
    UINT  dErrorModes[] = {SEM_NOOPENFILEERRORBOX, SEM_FAILCRITICALERRORS, 0};
    int   i;
    
    /*
     *  Initialize the Pal
     */
    if ((PAL_Initialize(argc,argv)) != 0)
    {
        return (FAIL);
    }

    /*
     *  Loop through the supported Error Modes and verify
     *  that GetLastError() returns the correct Error Mode
     */
    for (i=0; i < (sizeof(dErrorModes) / sizeof(UINT)); i++)
    {
        SetLastError(dErrorModes[i]);
        if ((dErrorReturn = GetLastError()) != dErrorModes[i])
        {   
            Fail("ERROR: SetLastError was set to 0x%4.4x but,"
                    " GetLastError returned 0x%4.4x\n", 
                    dErrorModes[i],
                    dErrorReturn);
        }
    }
        
    PAL_Terminate();
    return (PASS);
}
