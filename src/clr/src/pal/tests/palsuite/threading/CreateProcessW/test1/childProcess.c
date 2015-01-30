//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
//

/*============================================================
**
** Source: CreateProcessW/test1/childprocess.c
**
** Purpose: Test to ensure CreateProcessW starts a new process.  This test 
** launches a child process, and examines a file written by the child.
** This code is the child code.
**
** Dependencies: GetCurrentDirectory
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

const WCHAR szPathDelimW[] = {'\\','\0'};

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
mkAbsoluteFilenameW ( 
    LPWSTR dirName,  
    DWORD dwDirLength, 
    LPCWSTR fileName, 
    DWORD dwFileLength,
    LPWSTR absPathName )
{
    extern const WCHAR szPathDelimW[];

    DWORD sizeDN, sizeFN, sizeAPN;

    sizeDN = wcslen( dirName );
    sizeFN = wcslen( fileName );
    sizeAPN = (sizeDN + 1 + sizeFN + 1);

    /* insure ((dirName + DELIM + fileName + \0) =< _MAX_PATH ) */
    if ( sizeAPN > _MAX_PATH )
    {
	return ( 0 );
    }
    
    wcsncpy(absPathName, dirName, dwDirLength +1);
    wcsncpy(absPathName, szPathDelimW, 2);
    wcsncpy(absPathName, fileName, dwFileLength +1);

    return (sizeAPN);

} 

int __cdecl main( int argc, char **argv ) 
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

    dwDirLength = GetCurrentDirectory( _MAX_PATH, szDirNameW );

    if (0 == dwDirLength) 
    {
	Fail ("GetCurrentDirectory call failed.  Could not get "
		"current working directory\n.  Exiting.\n");
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
    szAbsPathNameA = malloc (dwSize +1);    

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
