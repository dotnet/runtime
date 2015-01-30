//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
//

/*============================================================================
**
** Source:  test1.c
**
** Purpose: Calls qsort to sort a buffer, and verifies that it has done
**          the job correctly.
**
**
**==========================================================================*/

#include <palsuite.h>

int __cdecl charcmp(const void *pa, const void *pb)
{
    return memcmp(pa, pb, 1);
}

int __cdecl main(int argc, char **argv)
{
    char before[] = "cgaiehdbjf";
    const char after[] = "abcdefghij";

    if (PAL_Initialize(argc, argv))
    {
        return FAIL;
    }


    qsort(before, sizeof(before) - 1, sizeof(char), charcmp);
  
    if (memcmp(before, after, sizeof(before)) != 0)
    {
        Fail("qsort did not correctly sort an array of characters.\n");
    }

    PAL_Terminate();
    return PASS;

}





