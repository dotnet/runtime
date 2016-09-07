// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*=============================================================
**
** Source:  createfilemappingw.c (test 3)
**
** Purpose: Positive test the CreateFileMappingW API.
**          Call CreateFileMapping with access PAGE_READWRITE.
**
**
**============================================================*/
#define UNICODE
#include <palsuite.h>

const   int MAPPINGSIZE = 2048;

int __cdecl main(int argc, char *argv[])
{

    HANDLE  hFile;
    char    buf[] = "this is a test string";
    char    ch[2048];
    WCHAR   lpFileName[] = {'t','e','s','t','.','t','m','p','\0'};
    HANDLE  hFileMapping;
    LPVOID  lpMapViewAddress;
    int     RetVal = PASS;

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

    /* Initialize the buffers.
     */
    memset(ch,  0, MAPPINGSIZE);

    /* Create a unnamed file-mapping object with file handle FileHandle
     * and with PAGE_READWRITE protection.
     */
    hFileMapping = CreateFileMapping(
                            hFile,
                            NULL,               /*not inherited*/
                            PAGE_READWRITE,     /*read and wite*/
                            0,                  /*high-order size*/
                            MAPPINGSIZE,        /*low-order size*/
                            NULL);              /*unnamed object*/

    if(NULL == hFileMapping)
    {
        Trace("ERROR:%u: Failed to create File Mapping.\n", 
              GetLastError());
        RetVal = FAIL;
        goto CleanUpOne;
    }

    /* maps a view of a file into the address space of the calling process.
     */
    lpMapViewAddress = MapViewOfFile(
                            hFileMapping,
                            FILE_MAP_ALL_ACCESS, /* access code */
                            0,                   /*high order offset*/
                            0,                   /*low order offset*/
                            MAPPINGSIZE);        /* number of bytes for map */

    if(NULL == lpMapViewAddress)
    {
        Trace("ERROR:%u: Failed to call MapViewOfFile "
              "API to map a view of file!\n", 
              GetLastError());
        RetVal = FAIL;
        goto CleanUpTwo;
    }
    
    /* Write to the Map view.
     */ 
    memcpy(lpMapViewAddress, buf, strlen(buf));

    /* Read from the Map view.
     */ 
    memcpy(ch, (LPCSTR)lpMapViewAddress, MAPPINGSIZE);
    
    /* Copy the MapViewOfFile to buffer, so we can
     * compare with value read from file directly.
     */
    if (memcmp(ch, buf, strlen(buf))!= 0)
    {
        Trace("ERROR: MapViewOfFile not equal to file contents "
              "retrieved \"%s\", expected \"%s\".\n",
              ch, buf);
        RetVal = FAIL;
        goto CleanUpThree;
    }

CleanUpThree:
        
    /* Unmap the view of file.
        */
    if ( UnmapViewOfFile(lpMapViewAddress) == FALSE )
    {
        Trace("ERROR:%u: Failed to UnmapViewOfFile of \"%0x%lx\".\n",
                GetLastError(),
                lpMapViewAddress);
        RetVal = FAIL;
    }

CleanUpTwo:

    /* Close Handle to opend file mapping.
        */
    if ( CloseHandle(hFileMapping) == FALSE )
    {
        Trace("ERROR:%u: Failed to CloseHandle \"0x%lx\".\n",
                GetLastError(),
                hFileMapping);
        RetVal = FAIL;
    }

CleanUpOne:
        
    /* Close Handle to create file mapping.
        */
    if ( CloseHandle(hFile) == FALSE )
    {
        Trace("ERROR:%u: Failed to CloseHandle \"0x%lx\".\n",
                GetLastError(),
                hFile);
        RetVal = FAIL;
    }

    /* Terminate the PAL.
     */ 
    PAL_TerminateEx(RetVal);
    return RetVal;
}

