//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
//

/*=============================================================================
**
** Source: test2.c
**
** Purpose: Positive test for ExitProcess.
** 
** Dependencies: none
** 

**
**===========================================================================*/
#include <palsuite.h>



int __cdecl main( int argc, char **argv ) 

{
    /* call ExitProcess() -- should work without PAL_Initialize() */
    ExitProcess(PASS);

    
    /* return failure if we reach here -- note no attempt at       */
    /* meaningful output because we never called PAL_Initialize(). */
    return FAIL; 
}
