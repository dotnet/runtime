//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
//

/*=============================================================
**
** Source:  createfilemapping_neg.c
**
** Purpose: Negative test the CreateFileMapping API.
**          Call CreateFileMapping to create a unnamed
**          file-mapping object with PAGE_READONLY
**          protection and try to map a zero length file
**          in UNICODE
**
**
**============================================================*/
#define UNICODE
#include <palsuite.h>

int __cdecl main(int argc, char *argv[])
{

    HANDLE FileHandle;
    HANDLE FileMappingHandle;
    int err;
    WCHAR *lpFileName = NULL;

    //Initialize the PAL environment
    err = PAL_Initialize(argc, argv);
    if(0 != err)
    {
        ExitProcess(FAIL);
    }

    //conver string to a unicode one
    lpFileName = convert("temp.txt");


    //create a file and return the file handle
    FileHandle = CreateFile(lpFileName,
        GENERIC_READ,
        FILE_SHARE_READ,
        NULL,
        CREATE_ALWAYS,
        FILE_ATTRIBUTE_ARCHIVE,
        NULL);

    //free this memory
    free(lpFileName);
   
    if(INVALID_HANDLE_VALUE == FileHandle)
    {
        Fail("Failed to call CreateFile to create a file\n");
    }

    //create a unnamed file-mapping object with file handle FileHandle
    //and with PAGE_READONLY protection
    //try to map a file which is zero length.
    FileMappingHandle = CreateFileMapping(
        FileHandle,         //File Handle
        NULL,               //not inherited
        PAGE_READONLY,      //access protection 
        0,                  //high-order of object size
        0,                  //low-orger of object size
        NULL);              //unnamed object


    if(NULL != FileMappingHandle || ERROR_FILE_INVALID != GetLastError()) 
    {//no error occured 
        Trace("\nFailed to call CreateFileMapping API for a negative test!\n");
        err = CloseHandle(FileHandle);
        if(0 == err)
        {
            Fail("\nFailed to call CloseHandle API\n");
        }
        err = CloseHandle(FileMappingHandle);
        if(0 == err)
        {
            Fail("\nFailed to call CloseHandle API\n");
        }
        Fail("");
    }
    
    //close the file handle
    err = CloseHandle(FileHandle);
    if(0 == err)
    {
        Fail("\nFailed to call CloseHandle API\n");
    }

    PAL_Terminate();
    return PASS;
}
