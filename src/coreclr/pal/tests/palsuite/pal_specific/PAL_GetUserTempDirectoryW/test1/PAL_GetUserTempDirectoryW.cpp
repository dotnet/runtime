// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*=============================================================
**
** Source: pal_getusertempdirectoryw.c
**
** Purpose: Positive test the PAL_GetUserTempDirectoryW API.
**          Call PAL_GetUserTempDirectoryW to retrieve the user
**          temp directory.
**
**
**============================================================*/
#define UNICODE
#include <palsuite.h>

#define DIRECTORYLENGTH 1024

PALTEST(pal_specific_PAL_GetUserTempDirectoryW_test1_paltest_pal_getusertempdirectoryw_test1, "pal_specific/PAL_GetUserTempDirectoryW/test1/paltest_pal_getusertempdirectoryw_test1")
{
    int err;
    DWORD dwFileAttribute;
    DWORD cch = DIRECTORYLENGTH;
    WCHAR wDirectoryName[DIRECTORYLENGTH];

    //Initialize the PAL environment
    err = PAL_Initialize(argc, argv);
    if(0 != err)
    {
        ExitProcess(FAIL);
    }

    //retrieve the user temp directory
    err = PAL_GetUserTempDirectory(ddtInstallationDependentDirectory, wDirectoryName, &cch);

    if(0 == err || 0 == strlen(convertC(wDirectoryName)))
    {
        Fail("Failed to call PAL_GetUserTempDirectoryW API!\n");
    }


    //retrieve the attributes of a file or directory
    dwFileAttribute = GetFileAttributesW(wDirectoryName);


    //check if the retrieved attribute indicates a directory
    if( FILE_ATTRIBUTE_DIRECTORY != (FILE_ATTRIBUTE_DIRECTORY & dwFileAttribute))
    {
        Fail("PAL_GetUserTempDirectoryW API returned a non-directory name!\n");
    }

    printf ("PAL_GetUserTempDirectoryW returns %S\n", wDirectoryName);

    PAL_Terminate();
    return PASS;

}
