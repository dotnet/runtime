// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*============================================================================
**
** Source:  test1.c
**
** Purpose: Repeatedly allocates and frees a chunk of memory, to verify
**          that free is really returning memory to the heap
**
**
**==========================================================================*/

#include <palsuite.h>

PALTEST(c_runtime_free_test1_paltest_free_test1, "c_runtime/free/test1/paltest_free_test1")
{

    char *testA;

    long i;
    if (PAL_Initialize(argc, argv))
    {
        return FAIL;
    }


    /* check that free really returns memory to the heap. */
    for(i=1; i<1000000; i++)
    {
        testA = (char *)malloc(1000*sizeof(char));
        if (testA==NULL)
        {
            Fail("Either free is failing to return memory to the heap, or"
                 " the system is running out of memory for some other "
                 "reason.\n");
        }
        free(testA);
    }

    free(NULL); /*should do nothing*/
    PAL_Terminate();
    return PASS;
}

















