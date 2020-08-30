// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*============================================================
**
** Source: CreateProcessA/test1/parentprocess.c
**
** Purpose: Test to ensure CreateProcessA starts a new process.  This test 
** launches a child process, and examines a file written by the child.
** This process (the parent process) reads the file created by the child and 
** compares the value the child wrote to the file.  (a const char *)
**
** Dependencies: GetCurrentDirectory
**               strlen
**               WaitForSingleObject
**               fopen
**               fclose
**               Fail
** 

**
**=========================================================*/

#include <palsuite.h>

const char *szCommonFileA = "childdata.tmp";

const char *szChildFileA = "paltest_createprocessa_test1_child";

const char *szPathDelimA = "\\";

const char *szCommonStringA = "058d2d057111a313aa82401c2e856002\0";

/*
 * Take two wide strings representing file and directory names
 * (dirName, fileName), join the strings with the appropriate path
 * delimiter and populate a wide character buffer (absPathName) with
 * the resulting string.
 *
 * Returns: The number of wide characters in the resulting string.
 * 0 is returned on Error.
 */
int 
mkAbsoluteFilenameA ( 
    LPSTR dirName,  
    DWORD dwDirLength, 
    LPCSTR fileName, 
    DWORD dwFileLength,
    LPSTR absPathName )
{
    extern const char *szPathDelimA;

    DWORD sizeDN, sizeFN, sizeAPN;
    
    sizeDN = strlen( dirName );
    sizeFN = strlen( fileName );
    sizeAPN = (sizeDN + 1 + sizeFN + 1);
    
    /* insure ((dirName + DELIM + fileName + \0) =< _MAX_PATH ) */
    if ( sizeAPN > _MAX_PATH )
    {
	return ( 0 );
    }
    
    strncpy(absPathName, dirName, dwDirLength +1);
    strncpy(absPathName, szPathDelimA, 2);
    strncpy(absPathName, fileName, dwFileLength +1);
    
    return (sizeAPN);
  
} 

int __cdecl main( int argc, char **argv ) 

{

    STARTUPINFO si;
    PROCESS_INFORMATION pi;

    static FILE * fp;

    DWORD dwFileLength;
    DWORD dwDirLength;
    DWORD dwSize;
    
    size_t cslen;
    
    char szReadStringA[256];

    char szDirNameA[_MAX_DIR];  
    char absPathBuf[_MAX_PATH];
    char *szAbsPathNameA;


    if(0 != (PAL_Initialize(argc, argv)))
    {
	return ( FAIL );
    }
    
    ZeroMemory ( &si, sizeof(si) );
    si.cb = sizeof(si);
    ZeroMemory ( &pi, sizeof(pi) );
    
    szAbsPathNameA=&absPathBuf[0];
    dwFileLength = strlen( szChildFileA );

    dwDirLength = GetCurrentDirectory(_MAX_PATH, szDirNameA);

    if (0 == dwDirLength) 
    {
	Fail ("GetCurrentDirectory call failed.  Could not get "
		"current working directory\n.  Exiting.\n");
    }

    dwSize = mkAbsoluteFilenameA( szDirNameA, dwDirLength, szChildFileA, 
				  dwFileLength, szAbsPathNameA );

    if (0 == dwSize)
    {
	Fail ("Palsuite Code: mkAbsoluteFilename() call failed.  Could "
		"not build absolute path name to file\n.  Exiting.\n");
    }

    if ( !CreateProcessA ( NULL,  
			   szAbsPathNameA,
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
	
    szAbsPathNameA=&absPathBuf[0];

    dwFileLength = strlen( szCommonFileA );

    dwSize = mkAbsoluteFilenameA( szDirNameA, dwDirLength, szCommonFileA, 
				  dwFileLength, szAbsPathNameA );
    
    /* set the string length for the open call*/

    if (0 == dwSize)
    {
	Fail ("Palsuite Code: mkAbsoluteFilename() call failed.  Could "
		"not build absolute path name to file\n.  Exiting.\n");
    }

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
