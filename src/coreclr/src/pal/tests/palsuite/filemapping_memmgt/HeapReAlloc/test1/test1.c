//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
//

/*=============================================================
**
** Source: test1.c
**
** Purpose: Allocate some memory. Then reallocate that memory.  Ensure the
** return values are correct, and also that data placed in the allocated
** memory carries over to the reallocated block.
**
**
**============================================================*/

#include <palsuite.h>

int __cdecl main(int argc, char *argv[])
{
    
    HANDLE TheHeap;
    char* TheMemory;
    char* ReAllocMemory;
    int i;

    if(PAL_Initialize(argc, argv) != 0)
    {
        return FAIL;
    }
    
    TheHeap = GetProcessHeap();
    
    if(TheHeap == NULL)
    {
        Fail("ERROR: GetProcessHeap() returned NULL when it was called. "
             "GetLastError() returned %d.",GetLastError());
    }
    
    /* Allocate 100 bytes on the heap */
    if((TheMemory = HeapAlloc(TheHeap, 0, 100)) == NULL)
    {
        Fail("ERROR: HeapAlloc returned NULL when it was called.  "
             "GetLastError() returned %d.",GetLastError());
    }
    
    /* Set each byte of that memory block to 'x' */
    memset(TheMemory, 'X', 100);
    
    /* Reallocate the memory */
    ReAllocMemory = HeapReAlloc(TheHeap, 0, TheMemory, 100);

    if(ReAllocMemory == NULL)
    {
        Fail("ERROR: HeapReAlloc failed to reallocate the 100 bytes of "
             "heap memory. GetLastError returns %d.",GetLastError());
    }

    /* Check that each byte of the memory Reallocated is 'x' */
    
    for(i=0; i<100; ++i)
    {
        if(ReAllocMemory[i] != 'X')
        {
            Fail("ERROR: Byte number %d of the reallocated memory block "
                 "is not set to 'X' as it should be.",i);
        }
    }
    
    
    PAL_Terminate();
    return PASS;
}
