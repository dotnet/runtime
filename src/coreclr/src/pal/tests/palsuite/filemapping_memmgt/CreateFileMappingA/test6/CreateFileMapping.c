//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
//

/*=============================================================
**
** Source:  createfilemapping.c (test 6)
**
** Purpose: Positive test the CreateFileMapping API.
**          Test CreateFileMapping to a "swap" handle with 
**          access PAGE_READWRITE.
**
**
**============================================================*/
#include <palsuite.h>

const   int MAPPINGSIZE = 2048;
HANDLE  SWAP_HANDLE     = ((VOID *)(-1));

int __cdecl main(int argc, char *argv[])
{
    char    testString[] = "this is a test string";
    char    lpObjectName[] = "myMappingObject";
    char    results[2048];
    int     RetVal = PASS;
    
    HANDLE hFileMapRW;
    LPVOID lpMapViewRW;
    LPVOID lpMapViewRW2;

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

    /* Create a map view of the READWRITE file mapping.
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


    /* maps a view of a file into the address space of the calling process.
     */
    lpMapViewRW2 = MapViewOfFile(
                            hFileMapRW,
                            FILE_MAP_ALL_ACCESS,  /* access code */
                            0,                    /* high order offset*/
                            0,                    /* low order offset*/
                            MAPPINGSIZE);         /* number of bytes for map */

    if(NULL == lpMapViewRW2)
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
    memcpy(results, (LPCSTR)lpMapViewRW2, MAPPINGSIZE);

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
    if ( UnmapViewOfFile(lpMapViewRW2) == FALSE )
    {
        Trace("ERROR:%u: Failed to UnmapViewOfFile of \"%0x%lx\".\n",
                GetLastError(),
                lpMapViewRW2);
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

