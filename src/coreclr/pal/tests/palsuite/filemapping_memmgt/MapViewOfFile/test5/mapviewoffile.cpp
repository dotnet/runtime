// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*=============================================================
**
** Source:  MapViewOfFile.c
**
** Purpose: Negative test the MapViewOfFile API.
**          Passing invalid values for the hFileMappingObject.
**
** Depends: CreatePipe,
**          CreateFile,
**          CreateFileMapping,
**          CloseHandle.
**          

**
**============================================================*/
#include <palsuite.h>

PALTEST(filemapping_memmgt_MapViewOfFile_test5_paltest_mapviewoffile_test5, "filemapping_memmgt/MapViewOfFile/test5/paltest_mapviewoffile_test5")
{

    const   int MAPPINGSIZE = 2048;
    HANDLE  hFileMapping;
    LPVOID  lpMapViewAddress;
    HANDLE  hReadPipe   = NULL;
    HANDLE  hWritePipe  = NULL;
    BOOL    bRetVal;

    SECURITY_ATTRIBUTES lpPipeAttributes;

    /* Initialize the PAL environment.
     */
    if(0 != PAL_Initialize(argc, argv))
    {
        return FAIL;
    }


    /* Attempt to create a MapViewOfFile with a NULL handle.
     */
    hFileMapping = NULL;
    
    lpMapViewAddress = MapViewOfFile(
                            hFileMapping,
                            FILE_MAP_WRITE, /* access code */
                            0,              /* high order offset */
                            0,              /* low order offset */
                            MAPPINGSIZE);   /* number of bytes for map */

    if((NULL != lpMapViewAddress) && 
       (GetLastError() != ERROR_INVALID_HANDLE))
    {
        Trace("ERROR:%u: Able to create a MapViewOfFile with "
              "hFileMapping=0x%lx.\n",
              GetLastError());
        UnmapViewOfFile(lpMapViewAddress);
        Fail("");
    }

    /* Attempt to create a MapViewOfFile with an invalid handle.
     */
    hFileMapping = INVALID_HANDLE_VALUE;
    
    lpMapViewAddress = MapViewOfFile(
                            hFileMapping,
                            FILE_MAP_WRITE, /* access code */
                            0,              /* high order offset */
                            0,              /* low order offset */
                            MAPPINGSIZE);   /* number of bytes for map */

    if((NULL != lpMapViewAddress) && 
       (GetLastError() != ERROR_INVALID_HANDLE))
    {
        Trace("ERROR:%u: Able to create a MapViewOfFile with "
              "hFileMapping=0x%lx.\n",
              GetLastError());
        UnmapViewOfFile(lpMapViewAddress);
        Fail("");
    }

    /* Clean-up and Terminate the PAL.
    */
    PAL_Terminate();
    return PASS;
}

  
