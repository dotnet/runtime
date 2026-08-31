// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*=============================================================
**
** Source: UnMapViewOfFile.c
**
** Purpose: Positive test the MapViewOfFile API.
**          Call MapViewOfFile with access FILE_MAP_ALL_ACCESS.
**
** Depends: CreateFile,
**          GetFileSize,
**          memset,
**          memcpy,
**          memcmp,
**          ReadFile,
**          UnMapViewOfFile,
**          CreateFileMapping,
**          CloseHandle.

**
**============================================================*/
#include <palsuite.h>

PALTEST(filemapping_memmgt_UnmapViewOfFile_test1_paltest_unmapviewoffile_test1, "filemapping_memmgt/UnmapViewOfFile/test1/paltest_unmapviewoffile_test1")
{
    const   int MappingSize = 2048;
    HANDLE  hFile;
    HANDLE  hFileMapping;
    LPVOID lpMapViewAddress;
    char    buf[] = "this is a test string";
    char    ch[2048];
    char    readString[2048];
    char    lpFileName[] = "test.tmp";
    DWORD   dwBytesRead;
    BOOL    bRetVal;

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
                        OPEN_ALWAYS,
        FILE_ATTRIBUTE_NORMAL,
        NULL);
    if (hFile == INVALID_HANDLE_VALUE)
    {
        Fail("ERROR: %u :unable to create file \"%s\".\n",
            GetLastError(),
            lpFileName);
    }

    /* Initialize the buffers.
     */
    memset(ch,  0, MappingSize);
    memset(readString,  0, MappingSize);

    /* Create a unnamed file-mapping object with file handle FileHandle
     * and with PAGE_READWRITE protection.
     */
    hFileMapping = CreateFileMapping(
                            hFile,
        NULL,               /*not inherited*/
        PAGE_READWRITE,     /*read and wite*/
        0,                  /*high-order of object size*/
                            MappingSize,        /*low-orger of object size*/
        NULL);              /*unnamed object*/

    if(NULL == hFileMapping)
        {
        Trace("ERROR:%u: Failed to create File Mapping.\n", 
             GetLastError());
        CloseHandle(hFile);
        Fail("");
        }

    /* maps a view of a file into the address space of the calling process.
     */
    lpMapViewAddress = MapViewOfFile(
                            hFileMapping,
                            FILE_MAP_ALL_ACCESS, /* access code */
                            0,                   /* high order offset */
                            0,                   /* low order offset */
                            MappingSize);        /* number of bytes for map */

    if(NULL == lpMapViewAddress)
        {
        Trace("ERROR:%u: Failed to call MapViewOfFile API to map a view"
             " of file!\n",
             GetLastError());
        CloseHandle(hFile);
        CloseHandle(hFileMapping);
        Fail("");
    }

    /* Write to the MapView and copy the MapViewOfFile
     * to buffer, so we can compare with value read from 
     * file directly.
     */

    memcpy(lpMapViewAddress, buf, strlen(buf));
    memcpy(ch, (LPCSTR)lpMapViewAddress, MappingSize);
    
    /* Read from the File handle.
     */
    bRetVal = ReadFile(hFile,
                       readString,
                       strlen(buf),
                       &dwBytesRead,
                       NULL);

    if (bRetVal == FALSE)
        {
        Trace("ERROR: %u :unable to read from file handle "
                "hFile=0x%lx\n",
                GetLastError(),
                hFile);
        CloseHandle(hFile);
        CloseHandle(hFileMapping);
        Fail("");
    }

    if (memcmp(ch, readString, strlen(readString)) != 0)
        {
        Trace("ERROR: Read string from file \"%s\", is "
              "not equal to string written through MapView "
              "\"%s\".\n",
              readString,
              ch);
        CloseHandle(hFile);
        CloseHandle(hFileMapping);
        Fail("");
        }

    /* Unmap the view of file.
     */
    if(UnmapViewOfFile(lpMapViewAddress) == FALSE)
        {
        Trace("ERROR: Failed to call UnmapViewOfFile API to"
             " unmap the view of a file, error code=%u\n",
             GetLastError());
        CloseHandle(hFile);
        CloseHandle(hFileMapping);
        Fail("");
        }

    /* Re-initialize the buffer.
     */
    memset(ch,  0, MappingSize);

    /* Close handle to created file mapping.
     */
    if(CloseHandle(hFileMapping) == FALSE)
    {
        Trace("ERROR:%u:Failed to call CloseHandle API "
             "to close a file mapping handle.",
             GetLastError());
        CloseHandle(hFile);
        Fail("");
    }

    /* Close handle to create file.
     */
    if(CloseHandle(hFile) == FALSE)
    {
        Fail("ERROR:%u:Failed to call CloseHandle API "
             "to close a file handle.",
             GetLastError());
    }

    PAL_Terminate();
    return PASS;
}

  
