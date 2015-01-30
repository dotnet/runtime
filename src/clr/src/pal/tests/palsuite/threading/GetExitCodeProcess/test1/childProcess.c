//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
//

/*============================================================
**
** Source: childprocess.c
**
** Purpose: Test to ensure GetExitCodeProcess returns the right 
** value. All this program does is return a predefined value.
**
** Dependencies: none
** 

**
**=========================================================*/

#include <pal.h>
#include "myexitcode.h"

int __cdecl main( int argc, char **argv ) 
{
    int i;
    
    // simulate some activity 
    for( i=0; i<10000; i++ )
        ;
        
    // return the predefined exit code
    return TEST_EXIT_CODE;
}
