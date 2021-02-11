// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*=============================================================
**
** Source:  createfilemappingw.c (test 5)
**
** Purpose: Positive test the CreateFileMappingW API.
**          Test CreateFileMappingW to a "swap" handle with 
**          access PAGE_READONLY.
**
**
**============================================================*/
#define UNICODE
#include <palsuite.h>

const   int MAPPINGSIZE = 2048;

PALTEST(filemapping_memmgt_CreateFileMappingW_test5_paltest_createfilemappingw_test5, "filemapping_memmgt/CreateFileMappingW/test5/paltest_createfilemappingw_test5")
{
    HANDLE  SWAP_HANDLE     = ((VOID *)(-1));
    char    testString[] = "this is a test string";
    WCHAR   lpObjectName[] = {'m','y','O','b','j','e','c','t','\0'};
    int     RetVal = FAIL;
    char    results[2048];
    
    HANDLE hFileMapRO;
    HANDLE hFileMapRW;
    LPVOID lpMapViewRO;
    LPVOID lpMapViewRW;

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
                            PAGE_READWRITE,     /*read only*/
                            0,                  /*high-order size*/
                            MAPPINGSIZE,        /*low-order size*/
                            lpObjectName);      /*named object*/

    if(NULL == hFileMapRW)
    {
        Fail("ERROR:%u: Failed to create File Mapping.\n", 
              GetLastError());
    }

    /* maps a view of a file into the address space of the calling process.
     */
    lpMapViewRW = MapViewOfFile(
                            hFileMapRW,
                            FILE_MAP_ALL_ACCESS, /* access code */
                            0,                   /* high order offset*/
                            0,                   /* low order offset*/
                            MAPPINGSIZE);        /* number of bytes for map */

    if(NULL == lpMapViewRW)
    {
        Trace("ERROR:%u: Failed to call MapViewOfFile "
              "API to map a view of file!\n", 
              GetLastError());
        RetVal = FAIL;
        goto CleanUpOne;
    }


    hFileMapRO = CreateFileMapping(
                            SWAP_HANDLE,
                            NULL,               /*not inherited*/
                            PAGE_READONLY,      /*read and write*/
                            0,                  /*high-order size*/
                            MAPPINGSIZE,        /*low-order size*/
                            lpObjectName);      /*named object*/

    if(NULL == hFileMapRO)
    {
        Trace("ERROR:%u: Failed to create File Mapping.\n", 
              GetLastError());
        RetVal = FAIL;
        goto CleanUpTwo;
    }
    
    /* maps a view of a file into the address space of the calling process.
     */
    lpMapViewRO = MapViewOfFile(
                            hFileMapRO,
                            FILE_MAP_READ,  /* access code */
                            0,                    /* high order offset*/
                            0,                    /* low order offset*/
                            MAPPINGSIZE);         /* number of bytes for map */

    if(NULL == lpMapViewRO)
    {
        Trace("ERROR:%u: Failed to call MapViewOfFile "
              "API to map a view of file!\n", 
              GetLastError());
        RetVal = FAIL;
        goto CleanUpThree;
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
        goto CleanUpFour;
    }

    /* Test was successful.
     */
    RetVal = PASS;

CleanUpFour:
        
    /* Unmap the view of file.
        */
    if ( UnmapViewOfFile(lpMapViewRO) == FALSE )
    {
        Trace("ERROR:%u: Failed to UnmapViewOfFile of \"%0x%lx\".\n",
                GetLastError(),
                lpMapViewRO);
        RetVal = FAIL;
    }

CleanUpThree:
        
    /* Close Handle to opend file mapping.
        */
    if ( CloseHandle(hFileMapRO) == FALSE )
    {
        Trace("ERROR:%u: Failed to CloseHandle \"0x%lx\".\n",
                GetLastError(),
                hFileMapRO);
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
        
    /* Close Handle to opend file mapping.
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

