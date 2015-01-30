//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
//

/*============================================================================
**
** Source:  test2.c
**
** Purpose: Calls qsort to sort a buffer, and verifies that it has done
**          the job correctly.
**
**
**==========================================================================*/

#include <palsuite.h>

int __cdecl twocharcmp(const void *pa, const void *pb)
{
    return memcmp(pa, pb, 2);
}

int __cdecl main(int argc, char **argv)
{
    char before[] = "ccggaaiieehhddbbjjff";
    const char after[] = "aabbccddeeffgghhiijj";

    if (PAL_Initialize(argc, argv))
    {
        return FAIL;
    }


    qsort(before, (sizeof(before) - 1) / 2, 2 * sizeof(char), twocharcmp);
  
    if (memcmp(before, after, sizeof(before)) != 0)
    {
        Fail("qsort did not correctly sort an array of 2-character "
             "buffers.\n");
    }

    PAL_Terminate();
    return PASS;

}





