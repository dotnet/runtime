// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*============================================================
**
** Source: CreateProcessW/test1/childprocess.c
**
** Purpose: Test to ensure CreateProcessW starts a new process.  This test 
** launches a child process, and examines a file written by the child.
** This code is the child code.
**
** Dependencies: GetTempPath
**               MultiByteToWideChar
**               wcslen
**               strlen
**               WideCharToMultiByte
**               fopen
**               fclose
**               fprintf
** 

**
**=========================================================*/

#define UNICODE
#include <palsuite.h>

const WCHAR szCommonFileW[] = 
            {'c','h','i','l','d','d','a','t','a','.','t','m','p','\0'};


#define szCommonStringA "058d2d057111a313aa82401c2e856002\0"

PALTEST(threading_CreateProcessW_test1_paltest_createprocessw_test1_child, "threading/CreateProcessW/test1/paltest_createprocessw_test1_child")
{

    static FILE * fp;

    DWORD dwFileLength;
    DWORD dwDirLength;
    DWORD dwSize;
    
    char *szAbsPathNameA;
    WCHAR szDirNameW[_MAX_DIR];
    WCHAR szAbsPathNameW[_MAX_PATH];

    if(0 != (PAL_Initialize(argc, argv)))
    {
        return ( FAIL );
    }

    dwDirLength = GetTempPath(_MAX_PATH, szDirNameW);

    if (0 == dwDirLength) 
    {
	Fail ("GetTempPath call failed.  Could not get "
		"temp directory\n.  Exiting.\n");
    }

    dwFileLength = wcslen( szCommonFileW );

    dwSize = mkAbsoluteFilenameW( szDirNameW, dwDirLength, szCommonFileW, 
				  dwFileLength, szAbsPathNameW );

    if (0 == dwSize)
    {
	Fail ("Palsuite Code: mkAbsoluteFilename() call failed.  Could "
		"not build absolute path name to file\n.  Exiting.\n");
    }
    
    /* set the string length for the open call */
    szAbsPathNameA = (char*)malloc(dwSize +1);    

    if (NULL == szAbsPathNameA)
    {
	Fail ("Unable to malloc (%d) bytes.  Exiting\n", (dwSize +1) );
    }

    WideCharToMultiByte (CP_ACP, 0, szAbsPathNameW, -1, szAbsPathNameA, 
			 (dwSize + 1), NULL, NULL); 

    if ( NULL == ( fp = fopen ( szAbsPathNameA , "w+" ) ) ) 
    {
       /* 
	 * A return value of NULL indicates an error condition or an
	 * EOF condition 
	 */
	Fail ("%s unable to open %s for writing.  Exiting.\n", argv[0]
	      , szAbsPathNameA );
    }

    free (szAbsPathNameA);

    if ( 0 >= ( fprintf ( fp, "%s", szCommonStringA )))
    {
	Fail("%s unable to write to %s. Exiting.\n", argv[0]
	     , szAbsPathNameA );
    }
    
    if (0 != (fclose ( fp ))) 
    {
	Fail ("%s unable to close file %s.  Pid may not be "
	      "written to file. Exiting.\n", argv[0], szAbsPathNameA );
    }

    PAL_Terminate();
    return ( PASS );    
    
}
