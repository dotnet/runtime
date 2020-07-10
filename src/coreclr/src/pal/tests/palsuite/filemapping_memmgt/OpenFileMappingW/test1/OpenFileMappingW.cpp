// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*=============================================================
**
** Source:  openfilemappingw.c (test 1)
**
** Purpose: Positive test the OpenFileMapping API.
**          Call OpenFileMapping to open a named file-mapping
**          object with FILE_MAP_ALL_ACCESS access
**
**
**============================================================*/

#define UNICODE
#include <palsuite.h>

int __cdecl main(int argc, char *argv[])
{

    HANDLE  lpMapViewAddress;
    char    buf[] = "this is a test";
    char    ch[1024];

    HANDLE  FileMappingHandle;
    HANDLE  OpenFileMappingHandle;
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
                                NULL,                /* Not inherited*/
                                PAGE_READWRITE,      /* Read and write*/
                                0,                   /* High-order size*/
                                LOWORDERSIZE,        /* Low-order size*/
                                wpMappingFileObject);/* Named object*/


    if(NULL == FileMappingHandle) 
    {
        Fail("\nFailed to call CreateFileMapping to create a "
             "mapping object!\n");
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
                                    FILE_MAP_ALL_ACCESS,
                                    FALSE,
                                    wpMappingFileObject);

    if(NULL == OpenFileMappingHandle)
    {
        Trace("\nFailed to Call OpenFileMapping API!\n");
        RetVal = FAIL;
        goto CleanUpOne;
    }

    /* Test the opened map view.
     */
    lpMapViewAddress = MapViewOfFile(
                            OpenFileMappingHandle,
                            FILE_MAP_ALL_ACCESS, /* access code */
                            0,                   /* high order offset */
                            0,                   /* low order offset */
                            LOWORDERSIZE);       /* number of bytes for map */

    if(NULL == lpMapViewAddress)
    {
        Trace("ERROR:%u: Failed to call MapViewOfFile "
             "API to map a view of file!\n", 
             GetLastError());
        RetVal = FAIL;
        goto CleanUpTwo;
    }

    /* Write to the Map View.
     */
    memcpy(lpMapViewAddress, buf, strlen(buf));
    
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

