// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*=============================================================
**
** Source:  openfilemappingw.c (test 3)
**
** Purpose: Positive test the OpenFileMappingW API.
**          Call OpenFileMappingW to open a named file-mapping
**          object with FILE_MAP_READ access
**
**
**============================================================*/
#define UNICODE
#include <palsuite.h>

int __cdecl main(int argc, char *argv[])
{
    char    buf[] = "this is a test";
    char    ch[1024];
    HANDLE  FileMappingHandle;
    HANDLE  OpenFileMappingHandle;
    HANDLE  OpenFileMappingHandle2;
    HANDLE  lpMapViewAddress;
    HANDLE  lpMapViewAddress2;
    const   int LOWORDERSIZE = 1024;
    int     RetVal = PASS;
    WCHAR   wpMappingFileObject[] = {'m','y','O','b','j','e','c','t','\0'};


    /* Initialize the PAL environment.
     */
    if(0 != PAL_Initialize(argc, argv))
    {
        return FAIL;
    }

    /* Create a unnamed file-mapping object with file handle FileHandle.
     */
    FileMappingHandle = CreateFileMapping(
                                INVALID_HANDLE_VALUE,
                                NULL,                /* not inherited */
                                PAGE_READWRITE,      /* read and wite */
                                0,                   /* high-order size */
                                LOWORDERSIZE,        /* must be non-zero */
                                wpMappingFileObject);/* named object */

    if(NULL == FileMappingHandle) 
    {
        Fail("\nFailed to call CreateFileMapping to create mapping object!\n");
    }
    if(GetLastError() == ERROR_ALREADY_EXISTS)
    {
        Trace("\nFile mapping object already exists!\n");
        RetVal = FAIL;
        goto CleanUpOne;
    }

    /* Open a named file-mapping object with FILE_MAP_ALL_ACCESS access.
     */
    OpenFileMappingHandle =  OpenFileMapping(
                                    FILE_MAP_READ,
                                    FALSE,
                                    wpMappingFileObject);

    if(NULL == OpenFileMappingHandle)
    {
        Trace("\nFailed to Call OpenFileMapping API!\n");
        RetVal = FAIL;
        goto CleanUpOne;
    }

    /* Open a file mapping with FILE_MAP_ALL_ACCESS access,
     * to verify the FILE_MAP_READ.
     */
    OpenFileMappingHandle2 =  OpenFileMapping(
                                    FILE_MAP_ALL_ACCESS,
                                    FALSE,
                                    wpMappingFileObject);

    if(NULL == OpenFileMappingHandle2)
    {
        Trace("\nFailed to Call OpenFileMapping API!\n");
        RetVal = FAIL;
        goto CleanUpTwo;
    }

    /* Test the opened map view.
     */
    lpMapViewAddress = MapViewOfFile(
                            OpenFileMappingHandle,
                            FILE_MAP_READ,       /* access code */
                            0,                   /* high order offset */
                            0,                   /* low order offset */
                            LOWORDERSIZE);       /* number of bytes for map */

    if(NULL == lpMapViewAddress)
    {
        Trace("ERROR:%u: Failed to call MapViewOfFile "
             "API to map a view of file!\n", 
             GetLastError());
        RetVal = FAIL;
        goto CleanUpThree;
    }

    /* Open a map view with FILE_MAP_ALL_ACCESS to verify,
     * the FILE_MAP_READ view.
     */
    lpMapViewAddress2 = MapViewOfFile(
                            OpenFileMappingHandle2,
                            FILE_MAP_ALL_ACCESS, /* access code */
                            0,                   /* high order offset */
                            0,                   /* low order offset */
                            LOWORDERSIZE);       /* number of bytes for map */

    if(NULL == lpMapViewAddress2)
    {
        Trace("2ERROR:%u: Failed to call MapViewOfFile "
             "API to map a view of file!\n", 
             GetLastError());
        RetVal = FAIL;
        goto CleanUpFour;
    }

    /* Write to the Map View.
     */
    memcpy(lpMapViewAddress2, buf, strlen(buf));
    /* Read from the Map View.
    */
    memcpy(ch, (LPCSTR)lpMapViewAddress, LOWORDERSIZE); 
    
    /* Compare what was written to the Map View,
     * to what was read.
     */
    if (memcmp(ch, buf, strlen(buf))!= 0)
    {
        Trace("ERROR: MapViewOfFile not equal to file contents "
             "retrieved \"%s\", expected \"%s\".\n",
             ch,
             buf);
        RetVal = FAIL;
        goto CleanUpFive;
    }

CleanUpFive:
        
        /* Unmap the view of file.
         */
        if ( UnmapViewOfFile(lpMapViewAddress2) == FALSE )
        {
            Trace("ERROR:%u: Failed to UnmapViewOfFile of \"%0x%lx\".\n",
                  GetLastError(),
                  lpMapViewAddress2);
            RetVal = FAIL;
        }

CleanUpFour:
        
        /* Unmap the view of file.
         */
        if ( UnmapViewOfFile(lpMapViewAddress) == FALSE )
        {
            Trace("ERROR:%u: Failed to UnmapViewOfFile of \"%0x%lx\".\n",
                  GetLastError(),
                  lpMapViewAddress);
            RetVal = FAIL;
        }

CleanUpThree:

        /* Close Handle to opened file mapping.
         */
        if ( CloseHandle(OpenFileMappingHandle2) == 0 )
        {
            Trace("ERROR:%u: Failed to CloseHandle \"0x%lx\".\n",
                  GetLastError(),
                  OpenFileMappingHandle2);
            RetVal = FAIL;
        }

CleanUpTwo:

        /* Close Handle to opened file mapping.
         */
        if ( CloseHandle(OpenFileMappingHandle) == 0 )
        {
            Trace("ERROR:%u: Failed to CloseHandle \"0x%lx\".\n",
                  GetLastError(),
                  OpenFileMappingHandle);
            RetVal = FAIL;
        }

CleanUpOne:
        
        /* Close Handle to create file mapping.
         */
        if ( CloseHandle(FileMappingHandle) == 0 )
        {
            Trace("ERROR:%u: Failed to CloseHandle \"0x%lx\".\n",
                  GetLastError(),
                  FileMappingHandle);
            RetVal = FAIL;
        }

    /* Terminate the PAL.
     */
    PAL_TerminateEx(RetVal);
    return RetVal;
}
