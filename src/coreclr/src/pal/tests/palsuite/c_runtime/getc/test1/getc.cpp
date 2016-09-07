// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*=====================================================================
**
** Source:  getc.c (test 1)
**
** Purpose: Tests the PAL implementation of the getc function.
**
**
**===================================================================*/

#include <palsuite.h>


int __cdecl main(int argc, char *argv[])
{
    const char szFileName[] = {"testfile.tmp"};
    const char szTestString[] = 
                    {"The quick brown fox jumped over the lazy dog's back."};
    FILE* pFile = NULL;
    int nCount = 0;
    int nChar = 0;
    char szBuiltString[256];


    if (0 != PAL_Initialize(argc,argv))
    {
        return FAIL;
    }

    memset(szBuiltString, 0, 256);


    /* create/open a file for read and write */
    pFile = fopen(szFileName, "w+");
    if (pFile == NULL)
    {
        Fail("getc: ERROR -> fopen failed to create the file %s with the "
            "error code %ld\n",
            szFileName,
            GetLastError());
    }

    /* try reading from an empty file */
    if ((nChar = getc(pFile)) != EOF)
    {
        Trace("getc: ERROR -> getc returned \"%c\" when run on "
            "an empty file.\n", nChar);
        if (fclose(pFile) != 0)
        {
            Trace("getc: ERROR -> fclose failed to close the file. "
                "GetLastError returned %ld\n",
                GetLastError());
        }
        Fail("");
    }

    // Move the file pointer back to the beginning of the file. Some
    // platforms require an fseek() between a getc() that returns EOF
    // and any subsequent output to the file.
    if (fseek(pFile, 0, SEEK_SET) != 0)
    {
        Trace("getc: ERROR -> fseek failed to move the file pointer to the "
            "beginning of the file. GetLastError returned %ld\n",
            GetLastError());
        if (fclose(pFile) != 0)
        {
            Trace("getc: ERROR -> fclose failed to close the file. "
                "GetLastError returned %ld\n",
                GetLastError());
        }
        Fail("");
    }

    /* populate the file with a known string */
    nCount = fprintf(pFile, szTestString);
    if (nCount != strlen(szTestString))
    {
        Fail("getc: ERROR -> fprintf failed to write %s. The string is %d "
            "characters long but fprintf apparently only wrote %d characters."
            " GetLastError returned %ld\n",
            szTestString,
            strlen(szTestString),
            nCount,
            GetLastError());
    }

    /* move the file pointer back to the beginning of the file */
    if (fseek(pFile, 0, SEEK_SET) != 0)
    {
        Trace("getc: ERROR -> fseek failed to move the file pointer to the "
            "beginning of the file. GetLastError returned %ld\n",
            GetLastError());
        if (fclose(pFile) != 0)
        {
            Trace("getc: ERROR -> fclose failed to close the file. "
                "GetLastError returned %ld\n",
                GetLastError());
        }
        Fail("");
    }

    /* now get the characters one at a time */
    nCount = 0;
    while ((nChar = getc(pFile)) != EOF)
    {
        szBuiltString[nCount++] = nChar;
    }
    
    /* now, let's see if it worked */
    if (strcmp(szBuiltString, szTestString) != 0)
    {
        Trace("getc: ERROR -> Reading one char at a time, getc built \"%s\" "
            "however it should have built \"%s\".\n",
            szBuiltString,
            szTestString);
        if (fclose(pFile) != 0)
        {
            Trace("getc: ERROR -> fclose failed to close the file. "
                "GetLastError returned %ld\n",
                GetLastError());
        }
        Fail("");
    }

    /* with the file pointer at EOF, try reading past EOF*/
    if ((nChar = getc(pFile)) != EOF)
    {
        Trace("getc: ERROR -> getc returned \"%c\" when reading past "
            "the end of the file.\n", nChar);
        if (fclose(pFile) != 0)
        {
            Trace("getc: ERROR -> fclose failed to close the file. "
                "GetLastError returned %ld\n",
                GetLastError());
        }
        Fail("");
    }

    if (fclose(pFile) != 0)
    {
        Fail("getc: ERROR -> fclose failed to close the file. "
            "GetLastError returned %ld\n",
            GetLastError());
    }

    
    PAL_Terminate();
    return PASS;
}
