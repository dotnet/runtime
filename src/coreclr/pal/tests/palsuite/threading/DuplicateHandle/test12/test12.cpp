// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*=====================================================================
**
** Source:    test12.c (DuplicateHandle)
**
** Purpose:   Tests the PAL implementation of the DuplicateHandle function.
**            This test will create handle to file (to write) and close it,
**            then call duplicate handle to read what was written.
**
**
**===================================================================*/
#include <palsuite.h>

PALTEST(threading_DuplicateHandle_test12_paltest_duplicatehandle_test12, "threading/DuplicateHandle/test12/paltest_duplicatehandle_test12")
{
    HANDLE  hFile;
    HANDLE  hDupFile;
    char    buf[256];
    char    teststr[]    = "A uNiQuE tEsT sTrInG";
    char    lpFileName[] = "testfile.txt";
    DWORD   dwBytesWritten;
    DWORD   dwBytesRead;
    BOOL    bRetVal;

    /*Initialize the PAL*/
    if ((PAL_Initialize(argc,argv)) != 0)
    {
        return (FAIL);
    }

    /*Create a file handle with CreateFile*/
    hFile = CreateFile(lpFileName,
                GENERIC_WRITE|GENERIC_READ,
                FILE_SHARE_WRITE|FILE_SHARE_READ,
                NULL,
                OPEN_ALWAYS,
                FILE_ATTRIBUTE_NORMAL,
                NULL);
    if (hFile == INVALID_HANDLE_VALUE)
    {
        Fail("ERROR: %u :unable to create file \"%s\".\n",
            GetLastError(),
            lpFileName);
    }

    /*Write test string to the file.*/
    bRetVal = WriteFile(hFile,      // handle to file
                teststr,            // data buffer
                strlen(teststr),    // number of bytes to write
                &dwBytesWritten,    // number of bytes written
                NULL);              // overlapped buffer

    if (bRetVal == FALSE)
    {
        Trace("ERROR: %u : unable to write to file handle "
                "hFile=0x%lx\n",
                GetLastError(),
                hFile);
        CloseHandle(hFile);
        Fail("");
    }

    /*Create a duplicate handle with DuplicateHandle.*/
    if (!(DuplicateHandle(
            GetCurrentProcess(),
            hFile,
            GetCurrentProcess(),
            &hDupFile,
            GENERIC_READ|GENERIC_WRITE,
            FALSE,
            DUPLICATE_SAME_ACCESS)))
    {
        Trace("ERROR: %u : Fail to create the duplicate handle"
             " to hFile=0x%lx\n",
             GetLastError(),
             hFile);
        CloseHandle(hFile);
        Fail("");
    }

    if( !CloseHandle(hFile) )
    {
        Fail("Duplicate Handle:Unable to close original file: Error[%u]\n", GetLastError());
    }

    memset(buf, 0, 256);

    /*Read from the Duplicated handle.*/
    bRetVal = ReadFile(hDupFile,
                       buf,
                       256,
                       &dwBytesRead,
                       NULL);
    if (bRetVal == FALSE)
    {
        Trace("ERROR: %u :unable to read from file handle "
                "hFile=0x%lx\n",
                GetLastError(),
                hDupFile);
         CloseHandle(hDupFile);
        Fail("");
    }

    /*Compare what was written to what was read.*/
    if (memcmp(teststr, buf, dwBytesRead) != 0)
    {
        Trace("ERROR: expected %s, got %s\n", teststr, buf);
        CloseHandle(hDupFile);
        Fail("");
    }

    /*Close the handles*/
    CloseHandle(hDupFile);

    bRetVal  = DeleteFileA(lpFileName);
    if (bRetVal != TRUE)
    {
        Trace("Error:%u: DuplicateHandle, DeleteFileA: Couldn't delete DeleteFileA's"
            " %s\n", GetLastError(), lpFileName);
         Fail("");
    }


    PAL_Terminate();
    return (PASS);
}
