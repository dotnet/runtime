//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
//

/*============================================================================
**
** Source:  test9.c
**
** Purpose: Tests sscanf with characters
**
**
**==========================================================================*/



#include <palsuite.h>
#include "../sscanf.h"


int __cdecl main(int argc, char *argv[])
{
    if (PAL_Initialize(argc, argv))
    {
        return FAIL;
    }

    DoCharTest("1234d", "%c", "1", 1);
    DoCharTest("1234d", "%c", "1", 1);
    DoCharTest("abc", "%2c", "ab", 2);
    DoCharTest(" ab", "%c", " ", 1);
    DoCharTest("ab", "%hc", "a", 1);
    DoWCharTest("ab", "%lc", convert("a"), 1);
    DoCharTest("ab", "%Lc", "a", 1);
    DoCharTest("ab", "%I64c", "a", 1);

    PAL_Terminate();
    return PASS;
}
