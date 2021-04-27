// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*============================================================================
**
** Source:  test1.c
**
** Purpose: Uses realloc to allocate and realloate memory, checking
**          that memory contents are copied when the memory is reallocated.
**
**
**==========================================================================*/

#include <palsuite.h>

PALTEST(c_runtime_realloc_test1_paltest_realloc_test1, "c_runtime/realloc/test1/paltest_realloc_test1")
{
    char *testA;
    const int len1 = 10;
    const char str1[] = "aaaaaaaaaa";

    const int len2 = 20;
    const char str2[] = "bbbbbbbbbbbbbbbbbbbb";

    if (PAL_Initialize(argc, argv))
    {
        return FAIL;
    }

    /* this should work like malloc */
    testA = (char *)realloc(NULL, len1*sizeof(char));  
    memcpy(testA, str1, len1);
    if (testA == NULL)
    {
        Fail("We ran out of memory (unlikely), or realloc is broken.\n");
    }

    if (memcmp(testA, str1, len1) != 0)
    { 
        Fail("realloc doesn't properly allocate new memory.\n");
    }
  
    testA = (char *)realloc(testA, len2*sizeof(char));  
    if (memcmp(testA, str1, len1) != 0)
    { 
        Fail("realloc doesn't move the contents of the original memory "
             "block to the newly allocated block.\n");
    }

    memcpy(testA, str2, len2);
    if (memcmp(testA, str2, len2) != 0)
    {
        Fail("Couldn't write to memory allocated by realloc.\n");
    }

    /* free the buffer */
    testA = (char*)realloc(testA, 0);
    if (testA != NULL)
    {
        Fail("Realloc didn't return NULL when called with a length "
             "of zero.\n");
    }
    PAL_Terminate();
    return PASS;
}
