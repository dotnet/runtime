//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
//

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

int __cdecl main(int argc, char *argv[])
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
