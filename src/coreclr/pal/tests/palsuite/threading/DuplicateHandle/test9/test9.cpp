// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*=====================================================================
**
** Source:  test9.c (DuplicateHandle)
**
** Purpose: Tests the PAL implementation of the DuplicateHandle function,
**          with a handle from GetCurrentProcess. The test will create a
**          process, duplicate it, then using ReadProcessMemory will
**          read from the memory location of the CreateProcess process
**          memory and the DuplicateHandle process memory. If the
**          duplication is correct the memory will be the same for both.
**
**
**===================================================================*/

#include <palsuite.h>

PALTEST(threading_DuplicateHandle_test9_paltest_duplicatehandle_test9, "threading/DuplicateHandle/test9/paltest_duplicatehandle_test9")
{
    HANDLE  hProcess;
    HANDLE  hDupProcess;
    char    lpBuffer[64];
    char    lpDupBuffer[64];
    SIZE_T  lpNumberOfBytesRead;
    SIZE_T  lpDupNumberOfBytesRead;
    char lpTestBuffer[] = "abcdefghijklmnopqrstuvwxyz";

    /* Initialize the PAL.
    */
    if(0 != (PAL_Initialize(argc, argv)))
    {
        return FAIL;
    }

    /* Initialize the buffers.
    */
    ZeroMemory( &lpBuffer, sizeof(lpBuffer) );
    ZeroMemory( &lpDupBuffer, sizeof(lpDupBuffer) );

    /* Get current process, this will be duplicated.
    */
    hProcess = GetCurrentProcess();
    if(hProcess == NULL)
    {
        Fail("ERROR: Unable to get the current process\n");
    }

    /* Duplicate the current process handle.
    */
    if (!(DuplicateHandle(GetCurrentProcess(),       /* source handle process*/
                          hProcess,                  /* handle to duplicate*/
                          GetCurrentProcess(),       /* target process handle*/
                          &hDupProcess,              /* duplicate handle*/
                          (DWORD)0,                  /* requested access*/
                          FALSE,                     /* handle inheritance*/
                          DUPLICATE_SAME_ACCESS)))   /* optional actions*/
    {
        Trace("ERROR:%u: Failed to create the duplicate handle"
             " to hProcess=0x%lx",
             GetLastError(),
             hProcess);
        CloseHandle(hProcess);
        Fail("");
    }

    /* Get memory read of the current process.
    */
    if ((ReadProcessMemory(hDupProcess, &lpTestBuffer,
         lpDupBuffer, sizeof(lpDupBuffer), &lpDupNumberOfBytesRead)) == 0)
    {
        Trace("ERROR:%u: Unable to read the process memory of "
             "hDupProcess=0x%lx.\n",
             GetLastError(),
             hDupProcess);
        CloseHandle(hProcess);
        CloseHandle(hDupProcess);
        Fail("");
    }

    /* Get read memory of the created process.
    */
    if ((ReadProcessMemory(hProcess, &lpTestBuffer,
         lpBuffer, sizeof(lpBuffer), &lpNumberOfBytesRead)) == 0)
    {
        Trace("ERROR:%u: Unable to read the process memory of "
             "hProcess=0x%lx.\n",
             GetLastError(),
             hProcess);
        CloseHandle(hProcess);
        CloseHandle(hDupProcess);
        Fail("");
    }

    /* Compare the number of bytes that were read by each
     * ReadProcessMemory.*/
    if (lpDupNumberOfBytesRead != lpNumberOfBytesRead)
    {
        Trace("ERROR: ReadProcessMemory read different numbers of bytes "
            "from duplicate process handles.\n");
        CloseHandle(hProcess);
        CloseHandle(hDupProcess);
        Fail("");
    }

    /* Compare the two buffers to make sure they are equal.
    */
    if ((strcmp(lpBuffer, lpDupBuffer)) != 0)
    {
        Trace("ERROR: ReadProcessMemory read different numbers of bytes "
            "from duplicate process handles. hProcess read \"%s\" and "
            "hDupProcess read \"%s\"\n",
            lpBuffer,
            lpDupBuffer);
        CloseHandle(hProcess);
        CloseHandle(hDupProcess);
        Fail("");
    }

    /* Clean-up thread and Terminate the PAL.*/
    CloseHandle(hProcess);
    CloseHandle(hDupProcess);
    PAL_Terminate();
    return PASS;
}
