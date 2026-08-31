// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*=====================================================================
**
** Source:  WriteFile.c (test 5)
**
** Purpose: Tests the PAL implementation of the WriteFile function.
**          Performs writing a huge file.
**
** dependency:
**          CreateFile.
**          GetFileSize.
**          FlushFileBuffers
**          CloseHandle
**          DeleteFile
**
**
**===================================================================*/


#include <palsuite.h>

BOOL CleanUp_WriteFile_test5(HANDLE hFile, const char * fileName)
{
    BOOL bRc = TRUE;
    if (CloseHandle(hFile) != TRUE)
    {
        bRc = FALSE;
        Trace("WriteFile: ERROR -> Unable to close file \"%s\","
            " error: %ld.\n", fileName, GetLastError());
    }
    if (!DeleteFileA(fileName))
    {
        bRc = FALSE;
        Trace("WriteFile: ERROR -> Unable to delete file \"%s\","
            " error: %ld.\n", fileName, GetLastError());
    }
    return bRc;
}


PALTEST(file_io_WriteFile_test5_paltest_writefile_test5, "file_io/WriteFile/test5/paltest_writefile_test5")
{

    HANDLE hFile = NULL;
    DWORD dwBytesWritten;
    const char* hugeStringTest =
        "1234567890123456789012345678901234567890";
    const char* szWritableFile = "writeable.txt";
    int i =0;
    if (0 != PAL_Initialize(argc,argv))
    {
        return FAIL;
    }

    /* create the test file         */
    hFile = CreateFile(szWritableFile, 
        GENERIC_WRITE,
        FILE_SHARE_WRITE,
        NULL,
        CREATE_ALWAYS,
        FILE_ATTRIBUTE_NORMAL,
        NULL);

    if(hFile == INVALID_HANDLE_VALUE)
    {
        Fail("WriteFile: ERROR -> Unable to create file \"%s\".\n", 
            szWritableFile);
    }   

    /* write 4000 000 chars to the file.*/
    for (i=0; i<100000;i++)
    {
        if( WriteFile(hFile,        /* HANDLE handle to file    */
            hugeStringTest,         /* data buffer              */
            strlen(hugeStringTest), /* number of bytes to write */
            &dwBytesWritten,        /* number of bytes written  */
            NULL)                   /* overlapped buffer        */
            ==0)
        {
            Trace("WriteFile: ERROR -> Unable to write to file error: %ld \n",
                GetLastError());
            CleanUp_WriteFile_test5(hFile,szWritableFile);
            Fail("");

        }
    }

    if(!FlushFileBuffers(hFile))
    {
        Trace("WriteFile: ERROR -> Call to FlushFileBuffers failed"
              "error %ld \n",GetLastError());
        CleanUp_WriteFile_test5(hFile,szWritableFile);        
        Fail("");
    }

    /* test if the size changed properly. */
    if(GetFileSize(hFile,NULL) != 4000000)
    {
        Trace("WriteFile: ERROR -> file size did not change properly"
            " after writing 4000 000 chars to it ( size= %u )\n",                   
            GetFileSize(hFile,NULL));
        CleanUp_WriteFile_test5(hFile,szWritableFile); 
        Fail("");

    }

    if (!CleanUp_WriteFile_test5(hFile,szWritableFile))
    {
        Fail("");
    }

    PAL_Terminate();
    return PASS;
}
