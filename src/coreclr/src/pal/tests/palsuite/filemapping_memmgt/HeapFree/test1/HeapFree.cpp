// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*=============================================================
**
** Source:  heapfree.c
**
** Purpose: Positive test the HeapFree API.
**          Call HeapFree to free a memory block 
**          and try to free an invalid memory block
**
**
**============================================================*/
#include <palsuite.h>

#define HEAPSIZE 64

int __cdecl main(int argc, char *argv[])
{
    int err;
    HANDLE ProcessHeapHandle;
    LPVOID lpHeap;


    /* Initialize the PAL environment */
    if(0 != PAL_Initialize(argc, argv))
    {
        return FAIL;
    }

    /* Retrieve the calling process heap handle */
    ProcessHeapHandle = GetProcessHeap();
     
    if(!ProcessHeapHandle)
    {
        Fail("\nFailed to call GetProcessHeap API!\n");
    }
    
    
    lpHeap = HeapAlloc(ProcessHeapHandle,/* HeapHandle */
                        HEAP_ZERO_MEMORY,/* control flag */
                        HEAPSIZE);       /* specify the heap size */
    if(NULL == lpHeap)
    {
        Fail("Failed to call HeapAlloc API!\n");
    }
    

    /* free a allocate heap memory */
    err = HeapFree(ProcessHeapHandle,
                    0,
                    lpHeap);

    if(0 == err)
    {
        Fail("Failed to call HeapFree API!\n");
    }

    PAL_Terminate();
    return PASS;
}
