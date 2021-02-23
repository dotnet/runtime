// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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

int __cdecl charcmp_qsort_test1(const void *pa, const void *pb)
{
    return memcmp(pa, pb, 1);
}

PALTEST(c_runtime_qsort_test1_paltest_qsort_test1, "c_runtime/qsort/test1/paltest_qsort_test1")
{
    char before[] = "cgaiehdbjf";
    const char after[] = "abcdefghij";

    if (PAL_Initialize(argc, argv))
    {
        return FAIL;
    }


    qsort(before, sizeof(before) - 1, sizeof(char), charcmp_qsort_test1);
  
    if (memcmp(before, after, sizeof(before)) != 0)
    {
        Fail("qsort did not correctly sort an array of characters.\n");
    }

    PAL_Terminate();
    return PASS;

}





