// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*=====================================================================
**
** Source:  WriteFile.c (test 4)
**
** Purpose: Tests the PAL implementation of the WriteFile function.
**          Performs multiple writes to a file at different locations
**          then verifies the results with GetFileSize.
**
** dependency:
**          CreateFile.
**          GetFileSize.
**          FlushFileBuffers
**          SetFilePointer.
**          CloseHandle.
**          DeleteFile.
**          
**
**
**===================================================================*/


#include <palsuite.h>

BOOL CleanUp_WriteFile_test4(HANDLE hFile, const char * fileName)
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

PALTEST(file_io_WriteFile_test4_paltest_writefile_test4, "file_io/WriteFile/test4/paltest_writefile_test4")
{
    const char* szStringTest = "1234567890";
    const char* szWritableFile = "writeable.txt";
    HANDLE hFile = NULL;
    DWORD dwBytesWritten;

    if (0 != PAL_Initialize(argc,argv))
    {   
        return FAIL;
    }

    /* create the test file */ 
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


    /* test wtriting to the file */
    if( WriteFile(hFile,        /* HANDLE handle to file    */
        szStringTest,           /* data buffer              */
        strlen(szStringTest),   /* number of bytes to write */
        &dwBytesWritten,        /* number of bytes written  */
        NULL)                   /* overlapped buffer        */
        ==0)
    {
        Trace("WriteFile: ERROR -> Unable to write to file error: %ld \n",
            GetLastError());
        CleanUp_WriteFile_test4(hFile,szWritableFile);
        Fail("");
    }

    if(!FlushFileBuffers(hFile))
    {   Trace("WriteFile: ERROR -> Call to FlushFile Buffers failed "
              "error %ld \n",GetLastError());
        CleanUp_WriteFile_test4(hFile,szWritableFile);
        Fail("");        
    }

    /* check the file size */
    if(GetFileSize(hFile, NULL)!=strlen(szStringTest))
    {
        Trace("WriteFile: ERROR -> writing %u chars to empty file "
            "caused its size to become %u\n",strlen(szStringTest),
            GetFileSize(hFile, NULL));
        CleanUp_WriteFile_test4(hFile,szWritableFile);        
        Fail("");
    }

    /* test writing to the file at position 5. */
    SetFilePointer(
        hFile,              /* handle to file           */
        0x5,                /* bytes to move pointer    */
        NULL,               /* bytes to move pointer    */
        FILE_BEGIN          /* starting point           */
        );


    if( WriteFile(hFile,        /* HANDLE handle to file    */
        szStringTest,           /* data buffer              */
        strlen(szStringTest),   /* number of bytes to write */
        &dwBytesWritten,        /* number of bytes written  */
        NULL)                   /* overlapped buffer        */
        ==0)
    {
        Trace("WriteFile: ERROR -> Unable to write to file after "
              " moiving the file poiner to 5 error: %ld \n",
              GetLastError());       
        CleanUp_WriteFile_test4(hFile,szWritableFile);        
        Fail("");
    }


    if(!FlushFileBuffers(hFile))
    {
        Trace("WriteFile: ERROR -> Call to FlushFile Buffers failed "
              "error %ld \n",GetLastError());
        CleanUp_WriteFile_test4(hFile,szWritableFile);
        Fail("");
    }

    /* Check the file size */
    if(GetFileSize(hFile, NULL)!=(strlen(szStringTest)+5))
    {
        Trace("WriteFile: ERROR -> writing %u chars to the file after "
              "sitting the file pointer to 5 resulted in wrong file size; "
              "Expected %u resulted %u.",strlen(szStringTest),
              (strlen(szStringTest)+5),GetFileSize(hFile, NULL));
        CleanUp_WriteFile_test4(hFile,szWritableFile);
        Fail("");
    }

    if (!CleanUp_WriteFile_test4(hFile,szWritableFile))
    {
        Fail("");
    }

    PAL_Terminate();
    return PASS;
}
