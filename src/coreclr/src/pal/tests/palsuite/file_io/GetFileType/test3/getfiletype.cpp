// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*=====================================================================
**
** Source:  getfiletype.c
**
** Purpose: Test the PAL implementation of the GetFileType on a handle 
**          to a console.
**
**
**===================================================================*/

#include <palsuite.h>


int __cdecl main(int argc, char *argv[])
{
    HANDLE hFile;
#if WIN32
    char *lpFileName = "CONIN$";
#else
    char *lpFileName = "/dev/null";
#endif
    DWORD dwFileType;


    if (0 != PAL_Initialize(argc,argv))
    {
        return FAIL;
    }

    /* get a handle to the console */
    hFile = CreateFile(lpFileName,
        GENERIC_READ,
        FILE_SHARE_READ,
        NULL,
        OPEN_ALWAYS,
        FILE_ATTRIBUTE_NORMAL,
        NULL);
    if (hFile == INVALID_HANDLE_VALUE)
    {
        Fail("GetFileType: ERROR: CreateFile failed to open %s with "
            "error %u.\n",
            lpFileName,
            GetLastError());
    }

    /* Get the file type */
    if ((dwFileType = GetFileType(hFile)) != FILE_TYPE_CHAR)
    {
        if (!CloseHandle(hFile))
        {
            Trace("GetFileType: ERROR: %u : Unable to close the handle "
                "hFile=0x%lx\n", GetLastError(), hFile);
        }
        Fail("GetFileType: ERROR: GetFileType returned %u for a device "
            "instead of the expected FILE_TYPE_CHAR (%u).\n",
            dwFileType,
            FILE_TYPE_CHAR);
    }

    if (!CloseHandle(hFile))
    {
        Fail("GetFileType: ERROR: %u : Unable to close the handle "
            "hFile=0x%lx\n", GetLastError(), hFile);
    }

    PAL_Terminate();  
    return PASS;
}
