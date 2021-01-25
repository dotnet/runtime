// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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

int __cdecl twocharcmp_qsort_test2(const void *pa, const void *pb)
{
    return memcmp(pa, pb, 2);
}

PALTEST(c_runtime_qsort_test2_paltest_qsort_test2, "c_runtime/qsort/test2/paltest_qsort_test2")
{
    char before[] = "ccggaaiieehhddbbjjff";
    const char after[] = "aabbccddeeffgghhiijj";

    if (PAL_Initialize(argc, argv))
    {
        return FAIL;
    }


    qsort(before, (sizeof(before) - 1) / 2, 2 * sizeof(char), twocharcmp_qsort_test2);
  
    if (memcmp(before, after, sizeof(before)) != 0)
    {
        Fail("qsort did not correctly sort an array of 2-character "
             "buffers.\n");
    }

    PAL_Terminate();
    return PASS;

}





