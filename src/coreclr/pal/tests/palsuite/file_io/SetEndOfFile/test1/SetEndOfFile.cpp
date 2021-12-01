// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*=====================================================================
**
** Source:  SetEndOfFile.c (test 1)
**
** Purpose: Tests the PAL implementation of the SetEndOfFile function.
**          This test will attempt to operate on a NULL file handle and
**          also test truncating a file not opened with GENERIC_WRITE
**
** Assumes successful:
**          SetEndOfFile
**          CreateFile
**          CloseHandle
**          

**
**===================================================================*/

#include <palsuite.h>




PALTEST(file_io_SetEndOfFile_test1_paltest_setendoffile_test1, "file_io/SetEndOfFile/test1/paltest_setendoffile_test1")
{
    HANDLE hFile = NULL;
    BOOL bRc = FALSE;


    if (0 != PAL_Initialize(argc,argv))
    {
        return FAIL;
    }

    bRc = SetEndOfFile(NULL);
    if (bRc == TRUE)
    {
        Fail("SetEndOfFile: ERROR -> Operation succeeded on a NULL file "
            "handle\n");
    }

    /* create a test file */
    hFile = CreateFile(szTextFile,
        GENERIC_READ,
        FILE_SHARE_READ,
        NULL,
        OPEN_ALWAYS,
        FILE_ATTRIBUTE_NORMAL,
        NULL);

    if(hFile == INVALID_HANDLE_VALUE)
    {
        Fail("SetEndOfFile: ERROR -> Unable to create file \"%s\".\n",
            szTextFile);
    }

    bRc = SetEndOfFile(hFile);
    if (bRc == TRUE)
    {
        Trace("SetEndOfFile: ERROR -> Operation succeeded on read-only"
              " file.\n");
        bRc = CloseHandle(hFile);
        if (bRc != TRUE)
        {
            Trace("SetEndOfFile: ERROR -> Unable to close file \"%s\".\n",
                szTextFile);
        }
        PAL_TerminateEx(FAIL);
        return FAIL;
    }

    bRc = CloseHandle(hFile);
    if (bRc != TRUE)
    {
        Fail("SetEndOfFile: ERROR -> Unable to close file \"%s\".\n",
            szTextFile);
    }


    PAL_Terminate();
    return PASS;
}
