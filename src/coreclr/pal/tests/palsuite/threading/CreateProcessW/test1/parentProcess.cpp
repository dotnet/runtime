// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*============================================================
**
** Source: CreateProcessW/test1/parentprocess.c
**
** Purpose: Test to ensure CreateProcessW starts a new process.  This test 
** launches a child process, and examines a file written by the child.
** This process (the parent process) reads the file created by the child and 
** compares the value the child wrote to the file.  (a const char *)
**
** Dependencies: GetTempPath
**               MultiByteToWideChar
**               wcslen
**               strlen
**               WideCharToMultiByte
**               WaitForSingleObject
**               fopen
**               fclose
**               Fail
** 

**
**=========================================================*/

#define UNICODE
#include <palsuite.h>

const WCHAR szCommonFileW[] = 
            {'c','h','i','l','d','d','a','t','a','.','t','m','p','\0'};

const WCHAR szChildFileW[] = u"threading/CreateProcessW/test1/paltest_createprocessw_test1_child";

#define szCommonStringA "058d2d057111a313aa82401c2e856002\0"

PALTEST(threading_CreateProcessW_test1_paltest_createprocessw_test1, "threading/CreateProcessW/test1/paltest_createprocessw_test1")

{

    STARTUPINFOW si;
    PROCESS_INFORMATION pi;

    static FILE * fp;

    DWORD dwFileLength;
    DWORD dwDirLength;
    DWORD dwSize;
    
    size_t cslen;
    
    char szReadStringA[256];

    char szAbsPathNameA[_MAX_PATH];
    WCHAR szDirNameW[_MAX_DIR];  
    WCHAR absPathBuf[_MAX_PATH];
    WCHAR *szAbsPathNameW;


    if(0 != (PAL_Initialize(argc, argv)))
    {
	return ( FAIL );
    }
    
    ZeroMemory ( &si, sizeof(si) );
    si.cb = sizeof(si);
    ZeroMemory ( &pi, sizeof(pi) );
    
    szAbsPathNameW=&absPathBuf[0];

    dwDirLength = GetTempPath(_MAX_PATH, szDirNameW);

    if (0 == dwDirLength) 
    {
	Fail ("GetTempPath call failed.  Could not get "
		"temp directory\n.  Exiting.\n");
    }

    int mbwcResult = MultiByteToWideChar(CP_ACP, 0, argv[0], -1, szAbsPathNameW, sizeof(absPathBuf));

    if (0 == mbwcResult)
    {
        Fail ("Palsuite Code: MultiByteToWideChar() call failed. Exiting.\n");
    }

    wcscat(szAbsPathNameW, u" ");
    wcscat(szAbsPathNameW, szChildFileW);
    
    if ( !CreateProcessW ( NULL,  
			   szAbsPathNameW,
			   NULL,          
			   NULL,          
			   FALSE,         
			   CREATE_NEW_CONSOLE,
			   NULL,              
			   NULL,              
			   &si,               
			   &pi )              
	)
    {
	Fail ( "CreateProcess call failed.  GetLastError returned %d\n", 
		 GetLastError() );
    }
    
    WaitForSingleObject ( pi.hProcess, INFINITE );
	
    szAbsPathNameW=&absPathBuf[0];

    dwFileLength = wcslen( szCommonFileW );

    dwSize = mkAbsoluteFilenameW( szDirNameW, dwDirLength, szCommonFileW, 
				  dwFileLength, szAbsPathNameW );
    
    /* set the string length for the open call*/

    if (0 == dwSize)
    {
	Fail ("Palsuite Code: mkAbsoluteFilename() call failed.  Could "
		"not build absolute path name to file\n.  Exiting.\n");
    }
    
    WideCharToMultiByte (CP_ACP, 0, szAbsPathNameW, -1, szAbsPathNameA, 
			 (dwSize + 1), NULL, NULL);

    if ( NULL == ( fp = fopen ( szAbsPathNameA , "r" ) ) )
    {
	Fail ("%s\nunable to open %s\nfor reading.  Exiting.\n", argv[0], 
	      szAbsPathNameA );
    }

    cslen = strlen ( szCommonStringA );

    if ( NULL == fgets( szReadStringA, (cslen + 1), fp ))
    {
	/* 
	 * A return value of NULL indicates an error condition or an
	 * EOF condition 
	 */
	Fail ("%s\nunable to read file\n%s\nszReadStringA is %s\n"
	      "Exiting.\n", argv[0], szAbsPathNameA, 
	      szReadStringA );
    }

    if ( 0 != strncmp( szReadStringA, szCommonStringA, cslen ))
    {
	Fail ("string comparison failed.\n  szReadStringA is %s and\n"
	      "szCommonStringA is %s\n", szReadStringA,
	      szCommonStringA );
    }
    else
    {
	Trace ("string comparison passed.\n");
    }
    
    if (0 != (fclose ( fp ))) 
    {
	Trace ("%s unable to close file %s.  This may cause a file pointer "
	       "leak.  Continuing.\n", argv[0], szAbsPathNameA );
    }

    /* Close process and thread handle */
    CloseHandle ( pi.hProcess );
    CloseHandle ( pi.hThread );

    PAL_Terminate();
    return ( PASS );

}
