// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*=============================================================
**
** Source:  createfilemappingw.c (test 7)
**
** Purpose: Positive test the CreateFileMappingW API.
**          Test CreateFileMappingW to a "swap" handle with 
**          access PAGE_READWRITE.
**
**
**============================================================*/
#define UNICODE
#include <palsuite.h>

const   int MAPPINGSIZE = 2048;

PALTEST(filemapping_memmgt_CreateFileMappingW_test7_paltest_createfilemappingw_test7, "filemapping_memmgt/CreateFileMappingW/test7/paltest_createfilemappingw_test7")
{
    HANDLE  SWAP_HANDLE     = ((VOID *)(-1));
    char    testString[] = "this is a test string";
    WCHAR   lpObjectName[] = {'m','y','O','b','j','e','c','t','\0'};
    char    results[2048];
    int     RetVal = PASS;
    
    HANDLE hFileMapRW;
    LPVOID lpMapViewRW;
    LPVOID lpMapViewRO;

    /* Initialize the PAL environment.
     */
    if(0 != PAL_Initialize(argc, argv))
    {
        return FAIL;
    }

    /* Initialize the buffers.
     */
    memset(results,  0, MAPPINGSIZE);

    /* Create a named file-mapping object with file handle FileHandle
     * and with PAGE_READWRITE protection.
    */
    hFileMapRW = CreateFileMapping(
                            SWAP_HANDLE,
                            NULL,               /*not inherited*/
                            PAGE_READWRITE,     /*read write*/
                            0,                  /*high-order size*/
                            MAPPINGSIZE,        /*low-order size*/
                            lpObjectName);      /*unnamed object*/

    if(NULL == hFileMapRW)
    {
        Fail("ERROR:%u: Failed to create File Mapping.\n", 
             GetLastError());
    }

    /* Create a map view to the READWRITE file mapping.
     */
    lpMapViewRW = MapViewOfFile(
                            hFileMapRW,
                            FILE_MAP_ALL_ACCESS,/* access code */
                            0,                  /* high order offset*/
                            0,                  /* low order offset*/
                            MAPPINGSIZE);       /* number of bytes for map */

    if(NULL == lpMapViewRW)
    {
        Trace("ERROR:%u: Failed to call MapViewOfFile "
              "API to map a view of file!\n", 
              GetLastError());
        RetVal = FAIL;
        goto CleanUpOne;
    }


    /* Create a map view to the READWRITE file mapping.
     */
    lpMapViewRO = MapViewOfFile(
                            hFileMapRW,
                            FILE_MAP_READ,        /* access code */
                            0,                    /* high order offset*/
                            0,                    /* low order offset*/
                            MAPPINGSIZE);         /* number of bytes for map */

    if(NULL == lpMapViewRO)
    {
        Trace("ERROR:%u: Failed to call MapViewOfFile "
              "API to map a view of file!\n", 
              GetLastError());
        RetVal = FAIL;
        goto CleanUpTwo;
    }

    /* Write the test string to the Map view.
    */    
    memcpy(lpMapViewRW, testString, strlen(testString));

    /* Read from the second Map view.
    */
    memcpy(results, (LPCSTR)lpMapViewRO, MAPPINGSIZE);

    /* Verify the contents of the file mapping,
     * by comparing what was written to what was read.
     */
    if (memcmp(results, testString, strlen(testString))!= 0)
    {
        Trace("ERROR: MapViewOfFile not equal to file contents "
              "retrieved \"%s\", expected \"%s\".\n",
              results,
              testString);
        RetVal = FAIL;
        goto CleanUpThree;
    }

    /* Test successful.
     */
    RetVal = PASS;

CleanUpThree:
        
    /* Unmap the view of file.
     */
    if ( UnmapViewOfFile(lpMapViewRO) == FALSE )
    {
        Trace("ERROR:%u: Failed to UnmapViewOfFile of \"%0x%lx\".\n",
                GetLastError(),
                lpMapViewRO);
        RetVal = FAIL;
    }   

CleanUpTwo:

    /* Unmap the view of file.
     */
    if ( UnmapViewOfFile(lpMapViewRW) == FALSE )
    {
        Trace("ERROR:%u: Failed to UnmapViewOfFile of \"%0x%lx\".\n",
                GetLastError(),
                lpMapViewRW);
        RetVal = FAIL;
    }


CleanUpOne:
        
    /* Close Handle to create file mapping.
     */
    if ( CloseHandle(hFileMapRW) == FALSE )
    {
        Trace("ERROR:%u: Failed to CloseHandle \"0x%lx\".\n",
                GetLastError(),
                hFileMapRW);
        RetVal = FAIL;
    }


    /* Terminate the PAL.
     */ 
    PAL_TerminateEx(RetVal);
    return RetVal;
}

