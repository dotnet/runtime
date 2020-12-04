// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*=============================================================
**
** Source:  createfilemappingw.c (test 8)
**
** Purpose: Positive test the CreateFileMappingW API.
**          Test the un-verifiable parameter combinations.
**
**
**============================================================*/
#define UNICODE
#include <palsuite.h>

const   int MAPPINGSIZE = 2048;

PALTEST(filemapping_memmgt_CreateFileMappingW_test8_paltest_createfilemappingw_test8, "filemapping_memmgt/CreateFileMappingW/test8/paltest_createfilemappingw_test8")
{
    HANDLE  SWAP_HANDLE     = ((VOID *)(-1));
    WCHAR   lpObjectName[] = {'m','y','O','b','j','e','c','t','\0'};
    
    HANDLE  hFileMap;

    /* Initialize the PAL environment.
     */
    if(0 != PAL_Initialize(argc, argv))
    {
        return FAIL;
    }

    /* Create a READONLY, "swap", un-named file mapping.
     * This test is unverifiable since there is no hook back to the file map
     * because it is un-named. As well, since it resides in "swap", and is
     * initialized to zero, there is nothing to read.
     */
    hFileMap = CreateFileMapping(
                            SWAP_HANDLE,
                            NULL,               /*not inherited*/
                            PAGE_READONLY,      /*read only*/
                            0,                  /*high-order size*/
                            MAPPINGSIZE,        /*low-order size*/
                            NULL);              /*un-named object*/

    if(NULL == hFileMap)
    {
        Fail("ERROR:%u: Failed to create File Mapping.\n", 
              GetLastError());
    }

    
    /* Create a COPYWRITE, "swap", un-named file mapping.
     * This test is unverifiable, here is a quote from MSDN:
     * 
     * Copy on write access. If you create the map with PAGE_WRITECOPY and 
     * the view with FILE_MAP_COPY, you will receive a view to file. If you 
     * write to it, the pages are automatically swappable and the modifications
     * you make will not go to the original data file. 
     *
     */
    hFileMap = CreateFileMapping(
                            SWAP_HANDLE,
                            NULL,               /*not inherited*/
                            PAGE_WRITECOPY,     /*write copy*/
                            0,                  /*high-order size*/
                            MAPPINGSIZE,        /*low-order size*/
                            NULL);              /*unnamed object*/

    if(NULL == hFileMap)
    {
        Fail("ERROR:%u: Failed to create File Mapping.\n", 
              GetLastError());
    }


    /* Terminate the PAL.
     */ 
    PAL_Terminate();
    return PASS;
}

