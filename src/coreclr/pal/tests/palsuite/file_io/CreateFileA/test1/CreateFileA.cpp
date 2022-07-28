// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*=====================================================================
**
** Source:  CreateFileA.c
**
** Purpose: Test the PAL implementation of the CreateFileA function
**
**
**===================================================================*/

#include <palsuite.h>

BOOL Cleanup_CreateFileA_test1(void)
{
    char FileName[20];
    int i;
    BOOL bRet = TRUE; // assume success

    // loop through all accesses, modes, dispositions and flags
    for (i=0; i<4*8*4*5; ++i) {
        sprintf_s(FileName, ARRAY_SIZE(FileName), "test%03d.txt", i);
	if (DeleteFileA(FileName) == FALSE) {
	    if (GetLastError() != ERROR_FILE_NOT_FOUND) {
		bRet = FALSE;
	    }
	}
    }
    return bRet;
}


PALTEST(file_io_CreateFileA_test1_paltest_createfilea_test1, "file_io/CreateFileA/test1/paltest_createfilea_test1")
{
    BOOL bSuccess = TRUE;
    int nCounter = 0;
    HANDLE hFile;
    char lpFileName[20];
    FILE *outFile = NULL;
    char results[1024];
    int i, j, k, l;
    DWORD dwDesiredAccess[4] = {0,              // 0
                                GENERIC_READ,   // 1
                                GENERIC_WRITE,  // 2
                                GENERIC_READ | GENERIC_WRITE};  // 3
    DWORD dwShareMode[8] = {0,                  // 0
                            FILE_SHARE_READ,    // 1
                            FILE_SHARE_WRITE,   // 2
                            FILE_SHARE_DELETE,  // 3
                            FILE_SHARE_READ | FILE_SHARE_WRITE,   // 4
                            FILE_SHARE_READ | FILE_SHARE_DELETE,  // 5
                            FILE_SHARE_WRITE | FILE_SHARE_DELETE, // 6
                            FILE_SHARE_READ|FILE_SHARE_WRITE|FILE_SHARE_DELETE};  // 7
    LPSECURITY_ATTRIBUTES lpAttr = NULL;
    DWORD dwCreationDisp[4] = {CREATE_NEW,          // 0
                                CREATE_ALWAYS,      // 1
                                OPEN_EXISTING,      // 2
                                OPEN_ALWAYS};       // 3
    DWORD dwFlagsAttrib[5] = {FILE_ATTRIBUTE_NORMAL,      // 0
                                FILE_FLAG_SEQUENTIAL_SCAN,  // 1
                                FILE_FLAG_WRITE_THROUGH,    // 2
                                FILE_FLAG_NO_BUFFERING,     // 3
                                FILE_FLAG_RANDOM_ACCESS};   // 4
    HANDLE hTemplate = NULL;


    if (0 != PAL_Initialize(argc,argv))
    {
        return FAIL;
    }

    if (!Cleanup_CreateFileA_test1()) {
	Trace("Pre-test Cleanup() failed.  LastError=%d\n", GetLastError());
	return FAIL;
    }

    /* open the file to read the expected results */
    outFile = fopen("winoutput", "r");
    memset (results, 0, 1024);

    fgets(results, 1024, outFile);
    nCounter = (int)strlen(results);
    fclose(outFile);

    nCounter = 0;

    // desired access loop
    for (i = 0; i < 4; i++)
    {
        // share mode loop
        for (j = 0; j < 8; j++)
        {
            // security attributes loop
            for (k = 0; k < 4; k++)
            {
                // creation disp loop
                for (l = 0; l < 5; l++)
                {
                    sprintf_s(lpFileName, ARRAY_SIZE(lpFileName), "test%03d.txt", nCounter);
                    hFile = CreateFile(lpFileName,
                        dwDesiredAccess[i],
                        dwShareMode[j],
                        lpAttr,
                        dwCreationDisp[k],
                        dwFlagsAttrib[l],
                        hTemplate);
                    if (hFile == INVALID_HANDLE_VALUE)
                    {
                        if (results[nCounter] == '1')
                        {
                            Trace("CreateFile: ERROR: Failed when expected "
                                "to pass %s [%d][%d][%d][%d]\n",
                                lpFileName, i, j, k, l);
                            bSuccess = FALSE;
                        }
                    }
                    else
                    {
                        CloseHandle(hFile);
                        if (results[nCounter] == '0')
                        {
                            Trace("CreateFile: ERROR: Passed when expected "
                                "to fail %s [%d][%d][%d][%d]\n",
                                lpFileName, i, j, k, l);
                            bSuccess = FALSE;
                        }
                    }
                    nCounter ++;
                }
            }
        }
    }

    if (!Cleanup_CreateFileA_test1())
    {
        Trace("Post-test Cleanup() failed.  LastError=%d\n", GetLastError());
        return FAIL;
    }

    int exitCode = bSuccess ? PASS : FAIL;
    PAL_TerminateEx(exitCode);
    return exitCode;
}
