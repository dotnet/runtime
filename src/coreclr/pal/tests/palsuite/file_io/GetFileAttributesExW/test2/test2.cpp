// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*=====================================================================
**
** Source:  getfileattributesexw.c (getfileattributesexw\test2)
**
** Purpose: Tests the PAL implementation of GetFileAttributesExW.
**          First get a file's attributes, modify the file, 
**          re-get its attributes
**          and compare the two sets of attributes.
**
**
**===================================================================*/
#include <palsuite.h>

/**
 * This is a helper function which takes two FILETIME structures and 
 * checks that the second time isn't before the first.
 */
static int IsFileTimeOk(FILETIME FirstTime, FILETIME SecondTime)
{
    
    ULONG64 TimeOne, TimeTwo;

    TimeOne = ((((ULONG64)FirstTime.dwHighDateTime)<<32) | 
               ((ULONG64)FirstTime.dwLowDateTime));
    
    TimeTwo = ((((ULONG64)SecondTime.dwHighDateTime)<<32) | 
               ((ULONG64)SecondTime.dwLowDateTime));
    
    return(TimeOne <= TimeTwo);
}

PALTEST(file_io_GetFileAttributesExW_test2_paltest_getfileattributesexw_test2, "file_io/GetFileAttributesExW/test2/paltest_getfileattributesexw_test2")
{
    DWORD res;
    char fileName[MAX_PATH] = "test_file";
    WCHAR *wFileName;
    WIN32_FILE_ATTRIBUTE_DATA beforeAttribs;
    WIN32_FILE_ATTRIBUTE_DATA afterAttribs;
    FILE *testFile;
    ULONG64 beforeFileSize;
    ULONG64 afterFileSize;

    if (0 != PAL_Initialize(argc,argv))
    {
        return FAIL;
    }

    /* Create the file */
    testFile = fopen(fileName, "w+");
    if( NULL == testFile )
    {
        Fail("Unexpected error: Unable to open file %S "
             "with fopen. \n",
             fileName);
    }

    if( EOF == fputs( "testing", testFile ) )
    {
        Fail("Unexpected error: Unable to write to file %S "
             "with fputs. \n",
             fileName);
    }

    if( 0 != fclose(testFile) )
    {
        Fail("Unexpected error: Unable to close file %S "
             "with fclose. \n",
             fileName);
    }

    /* Test the Values returned by GetFileAttributesExW 
     * before and after manipulating a file shouldn't be the same.
     */

    wFileName = convert(fileName);

    res = GetFileAttributesExW(wFileName,
                               GetFileExInfoStandard,
                               &beforeAttribs);
    
    if(res == 0)
    {
        Fail("ERROR: unable to get initial file attributes with "
             "GetFileAttributesEx that returned 0 with error %d.\n",
             GetLastError());
    }
    
    /* Make sure the time are different */
    Sleep(500);

    testFile = fopen(fileName, "w+");
    if( NULL == testFile )
    {
        Fail("Unexpected error: Unable to open file %S "
             "with fopen. \n",
             fileName);
    }

    if( EOF == fputs( "testing GetFileAttributesExW", testFile ) )
    {
        Fail("Unexpected error: Unable to write to file %S "
             "with fputs. \n",
             fileName);
    }

    if( 0 != fclose(testFile) )
    {
        Fail("Unexpected error: Unable to close file %S "
             "with fclose. \n",
             fileName);
    }

    res = GetFileAttributesExW(wFileName,
                               GetFileExInfoStandard,
                               &afterAttribs);
    
    if(res == 0)
    {
        Fail("ERROR: unable to get file attributes after operations with "
             "GetFileAttributesEx that returned 0 with error %d.\n",
             GetLastError());
    }

    /* Check the creation time */
    if(!IsFileTimeOk(beforeAttribs.ftCreationTime, 
                        afterAttribs.ftCreationTime))
    {
        Fail("ERROR: Creation time after the fputs operation "
             "is earlier than the creation time before the fputs.\n");
    }

    /* Check the last access time */
    if(!IsFileTimeOk(beforeAttribs.ftLastAccessTime, 
                        afterAttribs.ftLastAccessTime))
    {
        Fail("ERROR: Last access time after the fputs operation "
             "is earlier than the last access time before the fputs.\n");
    }

    /* Check the last write time */
    if(!IsFileTimeOk(beforeAttribs.ftLastWriteTime,
                        afterAttribs.ftLastWriteTime))
    {
        Fail("ERROR: Last write time after the fputs operation "
             "is earlier than the last write time before the fputs.\n");
    }

    beforeFileSize = ((ULONG64)beforeAttribs.nFileSizeHigh)<< 32 | 
                     ((ULONG64)beforeAttribs.nFileSizeLow);

    afterFileSize  = ((ULONG64)afterAttribs.nFileSizeHigh)<< 32 | 
                     ((ULONG64)afterAttribs.nFileSizeLow);
    
    /* Check the file size */
    if( afterFileSize <= beforeFileSize )
    {
        Fail("ERROR: the file should have had a bigger size "
             "after(%d) the operations than before(%d)\n",
             afterAttribs.nFileSizeLow,
             beforeAttribs.nFileSizeLow);
    }


    PAL_Terminate();
    return PASS;
}
