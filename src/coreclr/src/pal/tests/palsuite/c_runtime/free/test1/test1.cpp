// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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

int __cdecl main(int argc, char **argv)
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

















