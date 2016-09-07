// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*=====================================================================
**
** Source:  ungetc.c (test 2)
**
** Purpose: Tests the PAL implementation of the ungetc function
**
** Dependencies:
**          fopen
**          fread
**          fclose
**          fseek
**          getc
**
**
**===================================================================*/

#include <palsuite.h>


int __cdecl main(int argc, char *argv[])
{
    const char szFileName[] = {"test2.txt"};
    const char szNewString[] = {"bar bar"};
    char szBuffer[MAX_PATH];
    FILE* pFile = NULL;
    int nChar = 32; /* space */
    int i = 0;
    int nRc = 0;
    size_t nCount = 0;


    if (0 != PAL_Initialize(argc,argv))
    {
        return FAIL;
    }


    memset(szBuffer, 0, MAX_PATH);

    /*
    ** open the file in write only mode, populate it and
    ** attempt to push an unread character back on the stream
    */


    /* open the file for read */
    pFile = fopen(szFileName, "r");
    if (pFile == NULL)
    {
        Fail("ungetc: ERROR -> fopen failed to open the file \"%s\""
            " as read-only.n",
            szFileName);
    }


    /* 
    ** Call getc to get the first char and ungetc to put 
    ** it back. getc should read it again.
    */

    /* read a character */
    if ((nChar = getc(pFile)) == EOF)
    {
        Trace("ungetc: ERROR -> getc encountered an error reading.\n");
        if (fclose(pFile) != 0)
        {
            Trace("ungetc: ERROR -> fclose failed to close the file.\n");
        }
        Fail("");
    }

    /* put it back */
    if ((nRc = ungetc(nChar, pFile)) == EOF)
    {
        Trace("ungetc: ERROR -> ungetc failed to push '%c' back onto the"
            " stream.\n");
        if (fclose(pFile) != 0)
        {
            Trace("ungetc: ERROR -> fclose failed to close the file.\n");
        }
        Fail("");
    }

    /* read it again... hopefully */
    if (((nChar = getc(pFile)) == EOF) || (nChar != nRc))
    {
        Trace("ungetc: ERROR -> getc encountered an error reading.\n");
        if (fclose(pFile) != 0)
        {
            Trace("ungetc: ERROR -> fclose failed to close the file.\n");
        }
        Fail("");
    }

    /* 
    ** test multiple ungetcs by replacing "foo" in the stream with "bar"
    */

    /* move the file pointer back to the beginning of the file */
    if (fseek(pFile, 0, SEEK_SET) != 0)
    {
        Trace("ungetc: ERROR -> fseek failed to move the file pointer to the "
            "beginning of the file. GetLastError returned %ld\n",
            GetLastError());
        if (fclose(pFile) != 0)
        {
            Trace("ungetc: ERROR -> fclose failed to close the file.\n");
        }
        Fail("");
    }

    /* read a few characters */
    for (i = 0; i < 3; i++)
    {
        if (getc(pFile) == EOF)
        {
            Trace("ungetc: ERROR -> getc encountered an error reading. "
                "GetLastError returned %ld\n",
                GetLastError());
            if (fclose(pFile) != 0)
            {
                Trace("ungetc: ERROR -> fclose failed to close the file.\n");
            }
            Fail("");
        }
    }

    /* we just read "foo" so push "bar" back on the stream */
    for (i = 2; i >= 0; i--)
    {
        if ((nRc = ungetc(szNewString[i], pFile)) == EOF)
        {
            Trace("ungetc: ERROR -> ungetc failed to push '%c' back onto the"
                " stream.\n");
            if (fclose(pFile) != 0)
            {
                Trace("ungetc: ERROR -> fclose failed to close the file.\n");
            }
            Fail("");
        }
    }
    

    /* read the new and improved stream - I use szNewString because it 
       is correct length */
    nCount = fread(szBuffer, sizeof(char), strlen(szNewString), pFile);

    /* did we get the right number of characters?*/
    if (nCount != strlen(szNewString))
    {
        Trace("ungetc: ERROR -> fread read %d characters from the stream but"
            " %d characters were expected\n",
            nRc,
            strlen(szNewString));
        if (fclose(pFile) != 0)
        {
            Trace("ungetc: ERROR -> fclose failed to close the file.\n");
        }
        Fail("");
    }

    /* did we get the right string? */
    if (strcmp(szBuffer, szNewString) != 0)
    {
        Trace("ungetc: ERROR -> fread returned \"%s\" but \"%s\" was "
            "expected\n",
            szBuffer,
            szNewString);
        if (fclose(pFile) != 0)
        {
            Trace("ungetc: ERROR -> fclose failed to close the file.\n");
        }
        Fail("");
    }

    if (fclose(pFile) != 0)
    {
        Fail("ungetc: ERROR -> fclose failed to close the file.\n");
    }

    
    PAL_Terminate();
    return PASS;
}
