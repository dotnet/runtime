// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*=============================================================
**
** Source:  createfilemappingw.c
**
** Purpose: Positive test the CreateFileMapping API.
**          Call CreateFileMapping to create a unnamed
**          file-mapping object with PAGE_READONLY
**          protection and SEC_IMAGE attribute in UNICODE
**
**
**============================================================*/
#define UNICODE
#include <palsuite.h>

PALTEST(filemapping_memmgt_CreateFileMappingW_test2_paltest_createfilemappingw_test2, "filemapping_memmgt/CreateFileMappingW/test2/paltest_createfilemappingw_test2")
{

    HANDLE FileHandle;
    HANDLE FileMappingHandle;
    int err;
    WCHAR *wpFileName = NULL;
    char executableFileName[256]="";


    //Initialize the PAL environment
    err = PAL_Initialize(argc, argv);
    if(0 != err)
    {
        ExitProcess(FAIL);
    }

#if WIN32
    sprintf_s(executableFileName, ARRAY_SIZE(executableFileName),"%s","executable.exe");
#else
    sprintf_s(executableFileName, ARRAY_SIZE(executableFileName),"%s","executable");
#endif

    //conver string to a unicode one
    wpFileName = convert(executableFileName);


    //create a file and return the file handle
    FileHandle = CreateFile(wpFileName,
        GENERIC_READ,
        FILE_SHARE_READ,
        NULL,
        OPEN_EXISTING,
        FILE_ATTRIBUTE_ARCHIVE,
        NULL);

    //free this memory
    free(wpFileName);

    if(INVALID_HANDLE_VALUE == FileHandle)
    {
        Fail("Failed to call CreateFile to create a file\n");
    }

    //create a unnamed file-mapping object with file handle FileHandle
    //and with PAGE_READONLY protection
    FileMappingHandle = CreateFileMapping(
        FileHandle,         //File Handle
        NULL,               //not inherited
        PAGE_READONLY|SEC_IMAGE,      //access protection and section attribute
        0,                  //high-order of object size
        0,                  //low-orger of object size
        NULL);              //unnamed object


    if(NULL == FileMappingHandle)
    {
        Trace("\nFailed to call CreateFileMapping to create a mapping object!\n");
        err = CloseHandle(FileHandle);
        if(0 == err)
        {
            Fail("\nFailed to call CloseHandle API\n");
        }
        Fail("");
    }
    if(GetLastError() == ERROR_ALREADY_EXISTS)
    {
        Trace("\nFile mapping object already exists!\n");
        err = CloseHandle(FileHandle);
        if(0 == err)
        {
            Trace("\nFailed to call CloseHandle API to close a file handle\n");
            err = CloseHandle(FileMappingHandle);
            if(0 == err)
            {
                Fail("\nFailed to call CloseHandle API to close a mapping object handle\n");
            }
            Fail("");
        }
        err = CloseHandle(FileMappingHandle);
        if(0 == err)
        {
            Fail("\nFailed to call CloseHandle API to close a mapping object handle\n");
        }
        Fail("");
    }
    err = CloseHandle(FileMappingHandle);
    if(0 == err)
    {
        Trace("\nFailed to call CloseHandle API to close a mapping object handle\n");
        err = CloseHandle(FileHandle);
        if(0 == err)
        {
            Fail("\nFailed to call CloseHandle API to close a file handle\n");
        }
        Fail("");
    }
    err = CloseHandle(FileHandle);
    if(0 == err)
    {
        Fail("\nFailed to call CloseHandle API to close a file handle\n");
    }

    PAL_Terminate();
    return PASS;
}
