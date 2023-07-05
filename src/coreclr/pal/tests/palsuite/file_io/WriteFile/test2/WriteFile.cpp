// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*=====================================================================
**
** Source:  WriteFile.c (test 2)
**
** Purpose: Tests the PAL implementation of the WriteFile function.
**          Creates a number of files and writes different amounts of
**          data and verifies the writes.
**
**
**===================================================================*/


#include <palsuite.h>


char* writeBuffer_WriteFile_test2;
#define szWritableFile "Writeable.txt"
#define szResultsFile "Results.txt"
const int PAGESIZE = 4096;

BOOL writeTest_WriteFile_test2(DWORD dwByteCount, DWORD dwBytesWrittenResult, BOOL bResult)
{
    HANDLE hFile = NULL;
    DWORD dwBytesWritten;
    BOOL bRc = FALSE;

    /* create the test file */
    remove(szWritableFile);
    hFile = CreateFile(szWritableFile, GENERIC_WRITE, FILE_SHARE_WRITE,    
                       NULL, CREATE_ALWAYS, FILE_ATTRIBUTE_NORMAL, NULL);

    if(hFile == INVALID_HANDLE_VALUE)
    {
        Trace("WriteFile: ERROR -> Unable to create file \"%s\".\n", 
            szWritableFile);
        return FALSE;
    }
    
    bRc = WriteFile(hFile, writeBuffer_WriteFile_test2, dwByteCount, &dwBytesWritten, NULL);
    CloseHandle(hFile);

    if ((bRc != bResult) || (dwBytesWrittenResult != dwBytesWritten))
    {
        Trace("WriteFile returned BOOL:%d and dwWritten:%d what we do expect is"
              " BOOL:%d and dwWritten:%d\n", bRc, dwBytesWritten, bResult, 
              dwBytesWrittenResult);
        return FALSE;
    }

    return TRUE;
}

PALTEST(file_io_WriteFile_test2_paltest_writefile_test2, "file_io/WriteFile/test2/paltest_writefile_test2")
{
    const char * testString = "The quick fox jumped over the lazy dog's back.";
    const int testStringLen = strlen(testString);
    
    DWORD dwByteCount[4] =   {-1,    10,   testStringLen, 0};
    DWORD dwByteWritten[4] = {0,     10,   testStringLen, 0};
    BOOL bResults[] =        {FALSE, TRUE, TRUE,          TRUE};
    
    const int BUFFER_SIZE = 2 * PAGESIZE;
    int j;
    BOOL bRc = FALSE;
    DWORD oldProt;

    if (0 != PAL_Initialize(argc,argv))
    {
        return FAIL;
    }

    /* allocate read-write memery for writeBuffer_WriteFile_test2 */
    if (!(writeBuffer_WriteFile_test2 = (char*) VirtualAlloc(NULL, BUFFER_SIZE, MEM_COMMIT, 
                                             PAGE_READWRITE)))
	{
		Fail("VirtualAlloc failed: GetLastError returns %d\n", GetLastError());
		return FAIL;
	}
	
    memset((void*) writeBuffer_WriteFile_test2, '.', BUFFER_SIZE);
    strcpy(writeBuffer_WriteFile_test2, testString);
    
    /* write protect the second page of writeBuffer_WriteFile_test2 */
	if (!VirtualProtect(&writeBuffer_WriteFile_test2[PAGESIZE], PAGESIZE, PAGE_NOACCESS, &oldProt))
	{
		Fail("VirtualProtect failed: GetLastError returns %d\n", GetLastError());
		return FAIL;
	}
    
    for (j = 0; j< 4; j++)
    {
        bRc = writeTest_WriteFile_test2(dwByteCount[j], dwByteWritten[j], bResults[j]);
        if (bRc != TRUE)
        {
            Fail("WriteFile: ERROR -> Failed on test[%d]\n", j);
        }
    }

    PAL_Terminate();
    return PASS;
}
