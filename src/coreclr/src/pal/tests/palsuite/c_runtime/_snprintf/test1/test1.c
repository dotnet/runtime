//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
//

/*============================================================================
**
** Source:  test1.c
**
** Purpose: General test to see if _snprintf works correctly
**
**
**==========================================================================*/



#include <palsuite.h>
#include "../_snprintf.h"

/*
 * Notes: memcmp is used, as is strlen.
 */

int __cdecl main(int argc, char *argv[])
{
    char checkstr[] = "hello world";
    char buf[256] = { 0 };
    int ret;

    
    if (PAL_Initialize(argc, argv) != 0)
    {
        return FAIL;
    }


    _snprintf(buf, 256, "hello world");
    if (memcmp(checkstr, buf, strlen(checkstr)+1) != 0)
    {
        Fail("ERROR: expected \"%s\" (up to %d chars), got \"%s\"\n",
             checkstr, 256, buf);
    }

    _snprintf(buf, 256, "xxxxxxxxxxxxxxxxx");
    ret = _snprintf(buf, 8, "hello world");

    if (ret >= 0)
    {
        Fail("ERROR: expected negative return value, got %d", ret);
    }
    if (memcmp(checkstr, buf, 8) != 0 || buf[8] != 'x')
    {
        Fail("ERROR: expected %s (up to %d chars), got %s\n",
              checkstr, 8, buf);
    }
    
    PAL_Terminate();
    return PASS;
}
