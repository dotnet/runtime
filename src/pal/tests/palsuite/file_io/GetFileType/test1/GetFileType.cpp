// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*=====================================================================
**
** Source:  GetFileType.c (test 1)
**
** Purpose: Tests the PAL implementation of the GetFileType function.
**
**
**===================================================================*/

#include <palsuite.h>

const char* szTextFile = "text.txt";

int __cdecl main(int argc, char *argv[])
{
    HANDLE hFile = NULL;
    DWORD dwRc = 0;


    if (0 != PAL_Initialize(argc,argv))
    {
        return FAIL;
    }


    /* test FILE_TYPE_UNKNOWN */
    dwRc = GetFileType(hFile);
    if (dwRc != FILE_TYPE_UNKNOWN)
    {
        Fail("GetFileType: ERROR -> Was expecting a return type of "
            "FILE_TYPE_UNKNOWN but the function returned %ld.\n",
            dwRc);
    }


    /* create a test file */
    hFile = CreateFile(szTextFile, 
        GENERIC_READ | GENERIC_WRITE,
        FILE_SHARE_READ | FILE_SHARE_WRITE,
        NULL,
        CREATE_ALWAYS,
        FILE_ATTRIBUTE_NORMAL,
        NULL);

    if(hFile == INVALID_HANDLE_VALUE)
    {
        Fail("GetFileType: ERROR -> Unable to create file \"%s\".\n", 
            szTextFile);
    }

    dwRc = GetFileType(hFile);
    if (CloseHandle(hFile) != TRUE)
    {
        Fail("GetFileType: ERROR -> Unable to close file \"%s\".\n", 
            szTextFile);
    }
    if (!DeleteFileA(szTextFile))
    {
        Fail("GetFileType: ERROR -> Unable to delete file \"%s\".\n", 
            szTextFile);
    }

    if (dwRc != FILE_TYPE_DISK)
    {
        Fail("GetFileType: ERROR -> Was expecting a return type of "
            "FILE_TYPE_DISK but the function returned %ld.\n",
            dwRc);
    }

    PAL_Terminate();
    return PASS;
}
