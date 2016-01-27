// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*=============================================================
**
** Source:  openfilemappinga.c (test 1)
**
** Purpose: Positive test the OpenFileMapping API.
**          Call OpenFileMapping to open a named file-mapping
**          object with FILE_MAP_ALL_ACCESS access
**
**
**============================================================*/
#include <palsuite.h>

int __cdecl main(int argc, char *argv[])
{
    HANDLE  FileMappingHandle;
    HANDLE  OpenFileMappingHandle;
    HANDLE  lpMapViewAddress;
    const   int LOWORDERSIZE = 1024;
    char    buf[] = "this is a test";
    char    MapObject[] = "myMappingObject";
    char    ch[1024];
    int     err;
    int     RetVal = PASS;

    /* Initialize the PAL environment.
     */
    err = PAL_Initialize(argc, argv);
    if(0 != err)
    {
        return FAIL;
    }


    /* Create a named file-mapping object with
     * file handle FileHandle.
     */
    FileMappingHandle = CreateFileMapping(
                            INVALID_HANDLE_VALUE,
                            NULL,                   /* not inherited */
                            PAGE_READWRITE,         /* read and write */
                            0,                      /* high-order size */
                            LOWORDERSIZE,           /* low-order size */
                            MapObject);             /* named object */

    if(NULL == FileMappingHandle)
    {
        Fail("ERROR:%u:Failed to call CreateFileMapping to "
             "create a mapping object.\n",
             GetLastError());
    }
    if(GetLastError() == ERROR_ALREADY_EXISTS)
    {
        Trace("ERROR:File mapping object already exists\n");
        RetVal = FAIL;
        goto CleanUpOne;
    }

    /* Open a named file-mapping object with 
     * FILE_MAP_ALL_ACCESS access.
     */
    OpenFileMappingHandle =  OpenFileMapping(
                                FILE_MAP_ALL_ACCESS,
                                TRUE,
                                MapObject );

    if(NULL == OpenFileMappingHandle)
    {
        Trace("ERROR:%u:Failed to Call OpenFileMapping API!\n",
              GetLastError());
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


    /* Terminat the PAL.*/
    PAL_TerminateEx(RetVal);
    return RetVal;
}
