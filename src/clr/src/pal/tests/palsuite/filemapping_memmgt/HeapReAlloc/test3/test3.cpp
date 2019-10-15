// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*=============================================================
**
** Source: test3.c
**
** Purpose: Allocate some memory. Then reallocate that memory into a
** bigger space on the heap.  Check that the first portion of the data is 
** unchanged.  Then set the new portion to a value, to ensure that it is
** properly writable memory.
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
    if((TheMemory = (char*)HeapAlloc(TheHeap, 0, 100)) == NULL)
    {
        Fail("ERROR: HeapAlloc returned NULL when it was called.  "
             "GetLastError() returned %d.",GetLastError());
    }
    
    /* Set the first 100 bytes to 'X' */
    memset(TheMemory, 'X', 100);
    
    /* Reallocate the memory to 200 bytes  */
    ReAllocMemory = (char*)HeapReAlloc(TheHeap, 0, TheMemory, 200);
    
    if(ReAllocMemory == NULL)
    {
        Fail("ERROR: HeapReAlloc failed to reallocate the 100 bytes of "
             "heap memory. GetLastError returns %d.",GetLastError());
    }
    
    /* Check that each of the first 100 bytes hasn't lost any data. */
    for(i=0; i<100; ++i)
    {
        
        if(ReAllocMemory[i] != 'X')
        {
            Fail("ERROR: Byte number %d of the reallocated memory block "
                 "is not set to 'X' as it should be.",i);
        }
    }    

    /* Beyond the first 100 bytes is valid free memory.  We'll set all this
       memory to a value -- though, even if HeapReAlloc didn't work, it might 
       still be possible to memset this memory without raising an exception.
    */
    memset(ReAllocMemory+100, 'Z', 100);
    
    PAL_Terminate();
    return PASS;
}
