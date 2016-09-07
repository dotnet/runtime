// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*============================================================
**
** Source: _fullpath/test1/test1.c
**
** Purpose: Test to see if the _fullpath function returns the
** proper values.  A check is done to ensure NULL is returned
** by _fullpath only for the condition where the length of the 
** created absolute path name (absPath) is greater than 
** maxLength.  
**
** Dependencies: strlen
**               strncmp
**               SetCurrentDirectory
**               GetCurrentDirectory
** 

**
**=========================================================*/

#include <palsuite.h>

struct testcase 
{
    char relPath[50]; /* relative path array */
    int maxLength;    /* pathlength to pass */
    BOOL bRet;        /* TRUE if testcase expects function to return NULL */
};

int __cdecl main( int argc, char **argv ) 
{

    DWORD dwOrigDirLength;
    DWORD dwNewDirLength;
    DWORD dwRetStrLength;
    BOOL bRet;
    char *retPath;
    char szAbsPath[_MAX_PATH + 1];
    char szDirNameOWD[_MAX_DIR];
    char szDirNameNWD[_MAX_DIR];
    int i;

    struct testcase testcases[]=
    {
        {"." , _MAX_PATH, FALSE},
        {".." , _MAX_PATH, FALSE},
        {"..\\..", _MAX_PATH, FALSE},
        {"..\\..\\..", _MAX_PATH, FALSE},
        {"..", 1, TRUE}
    };

    if(0 != (PAL_Initialize(argc, argv)))
    {
        return ( FAIL );
    }
     
    for (i = 0; i < sizeof(testcases)/sizeof(struct testcase) ; i++)
    {

        /* reset variables */
        memset(szAbsPath, 0, _MAX_PATH + 1);
        memset(szDirNameOWD, 0, _MAX_DIR);
        memset(szDirNameNWD, 0, _MAX_DIR);

        dwOrigDirLength = 0;
        dwNewDirLength = 0;
        dwRetStrLength = 0;
       
        /* Get the current directory name */
        dwOrigDirLength = GetCurrentDirectory(_MAX_PATH, szDirNameOWD);
        if (0 == dwOrigDirLength) 
        {
            Fail ("PALSUITE ERROR: _fullpath (char *, %s, %d) test failed."
                  "\nGetCurrentDirectory (%d, %s) call failed. GetLastError"
                  " returned '%d'\n", testcases[i].relPath, 
                  testcases[i].maxLength, _MAX_PATH, szDirNameOWD, 
                  GetLastError());
        }

        /* 
         * Set the current directory to relPath.
         */
        bRet = SetCurrentDirectory(testcases[i].relPath);
        if (0 == bRet) 
        {
            Fail ("PALSUITE ERROR: _fullpath (char *, %s, %d) test failed."
                  "\nSetCurrentDirectory (%s) call failed. GetLastError"
                  " returned '%d'\n", testcases[i].relPath, 
                  testcases[i].maxLength, testcases[i].relPath, 
                  GetLastError());
        }
            
        /* Get the new current directory name */
        dwNewDirLength = GetCurrentDirectory(_MAX_PATH, szDirNameNWD);
        if (0 == dwNewDirLength) 
        {
            Fail ("PALSUITE ERROR: _fullpath (char *, %s, %d) test failed."
                  "\nGetCurrentDirectory(%d, %s) call failed. GetLastError"
                   " returned '%d'\n", testcases[i].relPath, 
                  testcases[i].maxLength, _MAX_PATH, szDirNameNWD,
                  GetLastError());
        }
            
        /* Set the current directory back to the original one */
        bRet = SetCurrentDirectory(szDirNameOWD);
        if (0 == bRet) 
        {
            Fail ("PALSUITE ERROR: _fullpath (char *, %s, %d) test failed."
                  "\nSetCurrentDirectory(%s) call failed. GetLastError"
                  " returned '%d'\n", testcases[i].relPath, 
                  testcases[i].maxLength, szDirNameOWD, GetLastError());
        }

        retPath = _fullpath( szAbsPath, 
                             testcases[i].relPath, 
                             testcases[i].maxLength );
        
        if ( NULL == retPath )
        {
            /* The function returned NULL when a value was expected */
            if ( FALSE == testcases[i].bRet )
            {
                Fail("PALSUITE ERROR: test failed.\n"
                     "_fullpath (char *, %s, %d) returned NULL\n" 
                     "when '%s' was expected\n", testcases[i].relPath, 
                      testcases[i].maxLength, szDirNameNWD );
            }
        } 
        else
        {
            dwRetStrLength = strlen ( szAbsPath );

            /* Check that the path lengths are identical. */
            if ( dwRetStrLength != dwNewDirLength )
            {
                Fail ("PALSUITE ERROR: _fullpath (char *, %s, %d) test failed."
                      "\ndwRetStringLength '%d' is not equal to "
                      "dwNewDirLength '%d'.\nszAbsPath is '%s' retPath is '%s'\n"
                      "szDirNameNWD is '%s'\n" , testcases[i].relPath, 
                      testcases[i].maxLength, dwRetStrLength ,dwNewDirLength
                      ,szAbsPath ,retPath ,szDirNameNWD);
            }

            /* 
             * Perform a string comparison on the path provided by 
             * GetCurrentDirectory and the path provided by _fullpath 
             * to ensure they are identical.
             */
            if ( 0 != strncmp( szDirNameNWD, szAbsPath, dwNewDirLength ))
            {
                Fail ("PALSUITE ERROR: _fullpath (char *, %s, %d) test failed."
                      "strncmp ( %s, %s, %d ) call failed.\n", 
                      testcases[i].relPath, testcases[i].maxLength, 
                      szDirNameNWD, szAbsPath, dwNewDirLength );
            }

            /* 
             * Perform a string comparison on both paths provided by 
             * _fullpath to ensure they are identical.
             */
            if ( 0 != strncmp( retPath, szAbsPath, dwNewDirLength ))
            {
                Fail ("PALSUITE ERROR: _fullpath (char *, %s, %d) test failed."
                      "strncmp ( %s, %s, %d ) call failed.\n", 
                      testcases[i].relPath, testcases[i].maxLength, 
                      szDirNameNWD, szAbsPath, dwNewDirLength );
            }
        } 
    }

    PAL_Terminate();
    return ( PASS );
}





