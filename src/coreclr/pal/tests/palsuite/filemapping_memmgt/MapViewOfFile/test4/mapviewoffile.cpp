// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*=============================================================
**
** Source: MapViewOfFile.c
**
** Purpose: Negative test the MapViewOfFile API.
**          Call MapViewOfFile with all access modes, except 
**          read-only, on a read only map.
**
** Depends: CreateFile,
**          CreateFileMapping,
**          CloseHandle,
**          UnMapViewOfFile.
**          

**
**============================================================*/
#include <palsuite.h>

PALTEST(filemapping_memmgt_MapViewOfFile_test4_paltest_mapviewoffile_test4, "filemapping_memmgt/MapViewOfFile/test4/paltest_mapviewoffile_test4")
{

    HANDLE hFile;
    BOOL   err;
    HANDLE hFileMapping;
    LPVOID lpMapViewAddress;
    DWORD  dwBytesWritten;
    const  int MAPPINGSIZE = 2048;
    char   buf[] = "this is a test string";
    char   lpFileName[] = "test.tmp";

    /* Initialize the PAL environment.
     */
    if(0 != PAL_Initialize(argc, argv))
    {
        return FAIL;
    }

    /* Create a file handle with CreateFile.
     */
    hFile = CreateFile( lpFileName,
                        GENERIC_WRITE|GENERIC_READ,
                        FILE_SHARE_READ|FILE_SHARE_WRITE,
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

    /* Write to the File handle.
     */ 
    err = WriteFile(hFile,
                        buf,
                        strlen(buf),
                        &dwBytesWritten,
                        NULL);

    if ( !FlushFileBuffers( hFile ) )
    {
        CloseHandle(hFile);
        Fail("ERROR: Unable to flush the buffers\n");
    }
    
    if (err == FALSE)
    {
        Trace("ERROR: %u :unable to write to file handle "
                "hFile=0x%lx\n",
                GetLastError(),
                hFile);
        CloseHandle(hFile);
        Fail("");
    }

    /* Create a unnamed file-mapping object with file handle FileHandle
     * and with PAGE_READWRITE protection.
     */
    hFileMapping = CreateFileMapping(
                            hFile,
                            NULL,           /*not inherited*/
                            PAGE_READONLY,  /*read and wite*/
                            0,              /*high-order of object size*/
                            0,              /*low-orger of object size*/
                            NULL);          /*unnamed object*/

    if(NULL == hFileMapping)
    {
        Trace("ERROR:%u: Failed to create File Mapping.\n", 
              GetLastError());
        CloseHandle(hFile);
        Fail("");
    }

    /* map a writeable view of a file to a read-only file map.
     */
    lpMapViewAddress = MapViewOfFile(
                            hFileMapping,
                            FILE_MAP_WRITE, /* access code */
                            0,              /* high order offset */
                            0,              /* low order offset */
                            MAPPINGSIZE);   /* number of bytes for map */

    if(NULL != lpMapViewAddress)
    {
        Trace("ERROR:%u: Able to create a writeable MapViewOfFile"
             " to a read-only file.\n", 
             GetLastError());
        CloseHandle(hFile);
        CloseHandle(hFileMapping);
        UnmapViewOfFile(lpMapViewAddress);
        Fail("");
    }

    /* map an all access view of a file to a read-only file map.
     */
    lpMapViewAddress = MapViewOfFile(
                            hFileMapping,
                            FILE_MAP_ALL_ACCESS, /* access code */
                            0,                   /* high order offset */
                            0,                   /* low order offset */
                            MAPPINGSIZE);        /* number of bytes for map */

    if(NULL != lpMapViewAddress)
    {
        Trace("ERROR:%u: Able to create an all access MapViewOfFile"
              " to a read-only file.\n", 
              GetLastError());
        CloseHandle(hFile);
        CloseHandle(hFileMapping);
        UnmapViewOfFile(lpMapViewAddress);
        Fail("");
    }

    /* map an copy view of a file to a read-only file map.
     */
    lpMapViewAddress = MapViewOfFile(
                            hFileMapping,
                            FILE_MAP_COPY, /* access code */
                            0,             /* high order offset */
                            0,             /* low order offset */
                            MAPPINGSIZE);  /* number of bytes for map */

    if(NULL != lpMapViewAddress)
    {
        Trace("ERROR:%u: Able to create a copy access MapViewOfFile "
              "to a read-only file.\n", 
              GetLastError());
        CloseHandle(hFile);
        CloseHandle(hFileMapping);
        Fail("");
    }
    
    /* Clean-up and Teminate. */
    CloseHandle(hFile);
    CloseHandle(hFileMapping);
    PAL_Terminate();
    return PASS;
}

