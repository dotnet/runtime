// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*=============================================================
**
** Source:  createfilemapping.c (test 9)
**
** Purpose: Negative test the CreateFileMappingW API.
**
**
**============================================================*/
#define UNICODE
#include <palsuite.h>

const int MAPPINGSIZE = 2048;

PALTEST(filemapping_memmgt_CreateFileMappingW_test9_paltest_createfilemappingw_test9, "filemapping_memmgt/CreateFileMappingW/test9/paltest_createfilemappingw_test9")
{

    HANDLE  hFile;
    WCHAR   lpFileName[] = {'t','e','s','t','.','t','m','p','\0'};

    HANDLE hFileMapping;

    /* Initialize the PAL environment.
     */
    if(0 != PAL_Initialize(argc, argv))
    {
        return FAIL;
    }

    /* Create a file handle with CreateFile, as READONLY
     */
    hFile = CreateFile( lpFileName,
                        GENERIC_READ,
                        FILE_SHARE_READ,
                        NULL,
                        CREATE_ALWAYS,
                        FILE_ATTRIBUTE_NORMAL,
                        NULL);

    if (hFile == INVALID_HANDLE_VALUE)
    {
        Fail("ERROR: %u :unable to create file \"%s\".\n",
             GetLastError(),
             lpFileName);
    }

    /* Attempt to create a unnamed file-mapping object to a READONLY file
     * as READWRITE access.
     */
    hFileMapping = CreateFileMapping(
                            hFile,
                            NULL,               /*not inherited*/
                            PAGE_READWRITE,     /*read and write*/
                            0,                  /*high-order size*/
                            MAPPINGSIZE,        /*low-order size*/
                            NULL);              /*unnamed object*/

    if(NULL != hFileMapping)
    {
        Trace("ERROR: Able to create READWRITE mapping to a "
              "READONLY file.\n" );
        if( 0 == CloseHandle(hFile) )
        {
            Trace("Unexpected Error: Unable to close file handle\n");
        }
        Fail("");
    }

    /* Attempt to create a unnamed file-mapping object to a zero length
     * file.
     */
    hFileMapping = CreateFileMapping(
                            hFile,
                            NULL,               /*not inherited*/
                            PAGE_READWRITE,     /*read and write*/
                            0,                  /*high-order size*/
                            0,                  /*low-order size*/
                            NULL);              /*unnamed object*/

    if( NULL != hFileMapping )
    {
        Trace("ERROR: Able to create READWRITE mapping to a "
              "READONLY file.\n" );
        if( 0 == CloseHandle(hFile) )
        {
            Trace("Unexpected Error: Unable to close file handle\n");
        }
        Fail("");
    }
    if(GetLastError() != ERROR_ACCESS_DENIED)
    {
        Trace("ERROR: Expected GetLastError() to return "
              "ERROR_FILE_INVALID (%d), it returned %u.\n",
              ERROR_FILE_INVALID,
              GetLastError());
        if( 0 == CloseHandle(hFile) )
        {
            Trace("Unexpected Error: Unable to close file handle\n");
        }
        Fail("");
    }

    /* Attempt to create a file mapping that is larger than
     * the file.
     */
    hFileMapping = CreateFileMapping(
                            hFile,
                            NULL,               /*not inherited*/
                            PAGE_READONLY,      /*read only*/
                            0,                  /*high-order size*/
                            MAPPINGSIZE,        /*low-order size*/
                            NULL);              /*unnamed object*/
    if(NULL != hFileMapping)
    {
        Trace("ERROR: Able to create file mapping of size %d to "
              "file of size 0.\n",
              MAPPINGSIZE);
        if( 0 == CloseHandle(hFile) )
        {
            Trace("Unexpected Error: Unable to close file handle\n");
        }
        Fail("");
    }

    if(GetLastError() != ERROR_NOT_ENOUGH_MEMORY )
    {
        Trace("ERROR: Expected GetLastError() to return "
              "ERROR_NOT_ENOUGH_MEMORY (%d), it returned %u.\n",
              ERROR_NOT_ENOUGH_MEMORY,
              GetLastError());
        if( 0 == CloseHandle(hFile) )
        {
            Trace("Unexpected Error: Unable to close file handle\n");
        }
        Fail("");
    }

    if( 0 == CloseHandle(hFile) )
    {
        Fail("Unexpected Error: Unable to close file handle\n");
    }

    /* Terminate the PAL.
     */
    PAL_Terminate();
    return PASS;
}

