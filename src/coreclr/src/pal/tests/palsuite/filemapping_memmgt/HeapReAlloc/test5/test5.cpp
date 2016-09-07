// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*=============================================================
**
** Source: test5.c
**
** Purpose: Allocate some memory. Then call HeapRealloc with 0 as the
** amount of memory to reallocate.  This should work, essentially freeing
** the memory (though we can't verfiy this)
**
**
**============================================================*/

#include <palsuite.h>

int __cdecl main(int argc, char *argv[])
{
    
    HANDLE TheHeap;
    char* TheMemory;
    char* ReAllocMemory;
    char* ReAllocMemory2;

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

    /* Set each byte of that memory block to 'x' */
    memset(TheMemory, 'X', 100);

    /* Reallocate the memory into 0 bytes */
    ReAllocMemory = (char*)HeapReAlloc(TheHeap, 0, TheMemory, 0);

    if(ReAllocMemory == NULL)
    {
        Fail("ERROR: HeapReAlloc failed to reallocate the 100 bytes of "
             "heap memory. GetLastError returns %d.",GetLastError());
    }

    /* Reallocate the memory we just put into 0 bytes, into 100 bytes. */
    ReAllocMemory2 = (char*)HeapReAlloc(TheHeap, 0, ReAllocMemory, 100);

    if(ReAllocMemory2 == NULL)
    {
        Fail("ERROR: HeapReAlloc failed to reallocate the 0 bytes of "
             "heap memory into 100. GetLastError returns %d.",GetLastError());
    }

    PAL_Terminate();
    return PASS;
}
