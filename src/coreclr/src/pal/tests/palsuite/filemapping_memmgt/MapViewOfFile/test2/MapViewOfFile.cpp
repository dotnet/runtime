// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*=============================================================
**
** Source: MapViewOfFile.c
**
** Purpose: Positive test the MapViewOfFile API.
**          Call MapViewOfFile with access FILE_MAP_WRITE.
**
** Depends: CreateFile,
**          GetFileSize,
**          memset,
**          CreateFileMapping,
**          CloseHandle,
**          memcpy,
**          ReadFile,
**          memcmp,
**          UnMapViewOfFile.
**
**
**============================================================*/
#include <palsuite.h>

PALTEST(filemapping_memmgt_MapViewOfFile_test2_paltest_mapviewoffile_test2, "filemapping_memmgt/MapViewOfFile/test2/paltest_mapviewoffile_test2")
{

    HANDLE  hFile;
    HANDLE  hFileMapping;
    LPVOID lpMapViewAddress;
    char    buf[] = "this is a test string";
    const   int MAPPINGSIZE = 2048;
    char    ch[2048];
    char    readString[2048];
    char    lpFileName[] = "test.tmp";
    DWORD   dwBytesRead;
    DWORD   dwInitialSize = 0;
    DWORD   dwFinalSize = 0;
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
                        CREATE_ALWAYS,
                        FILE_ATTRIBUTE_NORMAL, 
        NULL);
    if (hFile == INVALID_HANDLE_VALUE)
    {
        Fail("ERROR: %u :unable to create file \"%s\".\n",
            GetLastError(),
            lpFileName);
    }

    /* Get the initial size of file, for latter tests.
     */
    dwInitialSize = GetFileSize (hFile, NULL); 
    if ( dwInitialSize == INVALID_FILE_SIZE )
    {
        Fail("ERROR:%u: The created file \"%s\" has an invalid "
             "file size.\n",
             GetLastError(),
             lpFileName);
    }

    /* Initialize the buffers.
     */
    memset(ch,  0, MAPPINGSIZE);
    memset(readString,  0, MAPPINGSIZE);

    /* Create a unnamed file-mapping object with file handle FileHandle
     * and with PAGE_READWRITE protection.
     */
    hFileMapping = CreateFileMapping(
                            hFile,
                            NULL,           /*not inherited*/
                            PAGE_READWRITE, /*read and wite*/
                            0,              /*high-order of object size*/
                            MAPPINGSIZE,    /*low-orger of object size*/
                            NULL);          /*unnamed object*/

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
                            FILE_MAP_WRITE, /* access code */
                            0,              /*high order offset*/
                            0,              /*low order offset*/
                            MAPPINGSIZE);   /* number of bytes for map */

    if(NULL == lpMapViewAddress)
    {
        Trace("ERROR:%u: Failed to call MapViewOfFile API to map a view"
              " of file!\n",
              GetLastError());
        CloseHandle(hFile);
        CloseHandle(hFileMapping);
        Fail("");
    }

    /* Verify that the size of the file has increased to 
     * accomidate the MapView. 
     */
    dwFinalSize = GetFileSize (hFile, NULL); 
    if ( (dwFinalSize <= dwInitialSize) && (dwFinalSize != MAPPINGSIZE))
    {
        CloseHandle(hFile);
        CloseHandle(hFileMapping);
        Fail("ERROR: Size of the file was expected to "
             "increase from \"%d\", to \"%d\".\n ",
             dwInitialSize,
             MAPPINGSIZE);
    }

    /* Write to the MapView and copy the MapViewOfFile
     * to buffer, so we can compare with value read from 
     * file directly.
     */
    memcpy(lpMapViewAddress, buf, strlen(buf));
    
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

    if (memcmp(buf, readString, strlen(readString)) != 0)
    {
        CloseHandle(hFile);
        CloseHandle(hFileMapping);    
        Fail("ERROR: Read string from file \"%s\", is "
             "not equal to string written through MapView "
             "\"%s\".\n",
             readString,
             ch);
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

    /* Close handle to create file.
     */
    if(CloseHandle(hFile) == FALSE)
    {
        Trace("ERROR:%u:Failed to call CloseHandle API "
              "to close a file handle.",
              GetLastError());
        CloseHandle(hFileMapping);
        Fail("");
    }
    
    if(CloseHandle(hFileMapping) == FALSE)
    {
        Fail("ERROR:%u:Failed to call CloseHandle API "
             "to close a file mapping handle.",
             GetLastError());
    }

    PAL_Terminate();
    return PASS;
}

  
