//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
//

/*=====================================================================
**
** Source:  GetFileSize.c (test 1)
**
** Purpose: Tests the PAL implementation of the GetFileSize function.
**
**
**===================================================================*/

#include <palsuite.h>

const char* szTextFile = "text.txt";

void CleanUp(HANDLE hFile)
{
    if (CloseHandle(hFile) != TRUE)
    {
        Fail("GetFileSize: ERROR -> Unable to close file \"%s\".\n", 
            szTextFile);
    }
    if (!DeleteFileA(szTextFile))
    {
        Fail("GetFileSize: ERROR -> Unable to delete file \"%s\".\n", 
            szTextFile);
    }
}


int __cdecl main(int argc, char *argv[])
{
    HANDLE hFile = NULL;
    DWORD dwRc = 0;
    DWORD dwRc2 = 0;
    DWORD dwError = 0;
    DWORD dwHighOrder = 0;
    DWORD dwOffset = 0;
    DWORD dwReturnedHighOrder = 0;
    DWORD dwReturnedOffset = 0;
    DWORD lpNumberOfBytesWritten;
    char * data = "1234567890";

    if (0 != PAL_Initialize(argc,argv))
    {
        return FAIL;
    }


    /* test on a null file handle */
    dwRc = GetFileSize(hFile, NULL);
    if (dwRc != INVALID_FILE_SIZE)
    {
        Fail("GetFileSize: ERROR -> A file size was returned for "
            "an invalid handle.\n");
    }
    /* test on a null file handle using the high order option */
    dwRc = GetFileSize(hFile, &dwHighOrder);
    if (dwRc != INVALID_FILE_SIZE)
    {
        Fail("GetFileSize: ERROR -> A file size was returned for "
            "an invalid handle.\n");
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
        Fail("GetFileSize: ERROR -> Unable to create file \"%s\".\n", 
            szTextFile);
    }

    /* give the file a size */
    dwOffset = 256;
    dwRc = SetFilePointer(hFile, dwOffset, NULL, FILE_BEGIN);
    if (dwRc == INVALID_SET_FILE_POINTER)
    {
        Trace("GetFileSize: ERROR -> Call to SetFilePointer failed with %ld.\n", 
            GetLastError());
        CleanUp(hFile);
        Fail("");
    }
    else
    {
        if (!SetEndOfFile(hFile))
        {
            Trace("GetFileSize: ERROR -> Call to SetEndOfFile failed with %ld.\n", 
                GetLastError());
            CleanUp(hFile);
            Fail("");
        }
        dwReturnedOffset = GetFileSize(hFile, &dwReturnedHighOrder);
        if ((dwReturnedOffset != dwOffset) || 
            (dwReturnedHighOrder != dwHighOrder))
        {
            CleanUp(hFile);
            Fail("GetFileSize: ERROR -> File sizes do not match up.\n");
        }
    }

    /* make the file large using the high order option */
    dwOffset = 256;
    dwHighOrder = 1;
    dwRc = SetFilePointer(hFile, dwOffset, (PLONG)&dwHighOrder, FILE_BEGIN);
    if (dwRc == INVALID_SET_FILE_POINTER)
    {
        Trace("GetFileSize: ERROR -> Call to SetFilePointer failed with %ld.\n", 
            GetLastError());
        CleanUp(hFile);
        Fail("");
    }
    else
    {
        if (!SetEndOfFile(hFile))
        {
            dwError = GetLastError();
            CleanUp(hFile);
            if (dwError == 112)
            {
                Fail("GetFileSize: ERROR -> SetEndOfFile failed due to lack of "
                    "disk space\n");
            }
            else
            {
                Fail("GetFileSize: ERROR -> SetEndOfFile call failed "
                    "with error %ld\n", dwError);
            }
        }
        else
        {
            dwReturnedOffset = GetFileSize(hFile, &dwReturnedHighOrder);
            if ((dwReturnedOffset != dwOffset) || 
                (dwReturnedHighOrder != dwHighOrder))
            {
                CleanUp(hFile);
                Fail("GetFileSize: ERROR -> File sizes do not match up.\n");
            }
        }
    }


    /* set the file size to zero */
    dwOffset = 0;
    dwHighOrder = 0;
    dwRc = SetFilePointer(hFile, dwOffset, NULL, FILE_BEGIN);
    if (dwRc == INVALID_SET_FILE_POINTER)
    {
        Trace("GetFileSize: ERROR -> SetEndOfFile call failed with error %ld\n",
            GetLastError());
        CleanUp(hFile);
        Fail("");
    }
    else
    {
        if (!SetEndOfFile(hFile))
        {
            Trace("GetFileSize: ERROR -> Call to SetEndOfFile failed "
                "with %ld.\n", GetLastError());
            CleanUp(hFile);
            Fail("");
        }
        dwReturnedOffset = GetFileSize(hFile, &dwReturnedHighOrder);
        if ((dwReturnedOffset != dwOffset) || 
            (dwReturnedHighOrder != dwHighOrder))
        {
            CleanUp(hFile);
            Fail("GetFileSize: ERROR -> File sizes do not match up.\n");
        }
    }

    /*  test if file size changes by writing to it. */
    /* get file size */
    dwRc = GetFileSize(hFile, NULL);

    /* test writing to the file */
    if(WriteFile(hFile, data, strlen(data), &lpNumberOfBytesWritten, NULL)==0)
    {
        Trace("GetFileSize: ERROR -> Call to WriteFile failed with %ld.\n", 
             GetLastError());
        CleanUp(hFile);
        Fail("");
    }
    
    /* make sure the buffer flushed.*/
    if(FlushFileBuffers(hFile)==0)
    {
        Trace("GetFileSize: ERROR -> Call to FlushFileBuffers failed with %ld.\n", 
             GetLastError());
        CleanUp(hFile);
        Fail("");
    }

    /* get file size after writing some chars */
    dwRc2 = GetFileSize(hFile, NULL);
    if((dwRc2-dwRc) !=strlen(data))
    {
        CleanUp(hFile);
        Fail("GetFileSize: ERROR -> File size did not increase properly after.\n"
             "writing %d chars\n", strlen(data));        
    }

    CleanUp(hFile);
    PAL_Terminate();
    return PASS;
}
