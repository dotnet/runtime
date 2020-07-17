// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*============================================================
**
** Source: CreateProcessA/test1/childprocess.c
**
** Purpose: Test to ensure CreateProcessA starts a new process.  This test 
** launches a child process, and examines a file written by the child.
** This code is the child code.
**
** Dependencies: GetCurrentDirectory
**               strlen
**               fopen
**               fclose
**               fprintf
** 

**
**=========================================================*/

#include <palsuite.h>

const char *szCommonFileA = "childdata.tmp";

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

    static FILE * fp;

    DWORD dwFileLength;
    DWORD dwDirLength;
    DWORD dwSize;
    
    char szDirNameA[_MAX_DIR];
    char szAbsPathNameA[_MAX_PATH];

    if(0 != (PAL_Initialize(argc, argv)))
    {
        return ( FAIL );
    }

    dwDirLength = GetCurrentDirectory( _MAX_PATH, szDirNameA );

    if (0 == dwDirLength) 
    {
	Fail ("GetCurrentDirectory call failed.  Could not get "
		"current working directory\n.  Exiting.\n");
    }

    dwFileLength = strlen( szCommonFileA );

    dwSize = mkAbsoluteFilenameA( szDirNameA, dwDirLength, szCommonFileA, 
				  dwFileLength, szAbsPathNameA );

    if (0 == dwSize)
    {
	Fail ("Palsuite Code: mkAbsoluteFilename() call failed.  Could "
		"not build absolute path name to file\n.  Exiting.\n");
    }
    
    if ( NULL == ( fp = fopen ( szAbsPathNameA , "w+" ) ) ) 
    {
       /* 
	 * A return value of NULL indicates an error condition or an
	 * EOF condition 
	 */
	Fail ("%s unable to open %s for writing.  Exiting.\n", argv[0]
	      , szAbsPathNameA );
    }

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
