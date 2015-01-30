//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
//

/*=====================================================================
**
** Source:  test3.c
**
** Purpose: Tests how QueueUserAPC handles an invalid thread.
**
**
**===================================================================*/

#include <palsuite.h>

int __cdecl main (int argc, char **argv) 
{    
    int ret;

    if (0 != (PAL_Initialize(argc, argv)))
    {
        return FAIL;
    }

    ret = QueueUserAPC(NULL, NULL, 0);
    if (ret != 0)
    {
        Fail("QueueUserAPC passed with an invalid thread!\n");
    }

    PAL_Terminate();
    return PASS;
}
