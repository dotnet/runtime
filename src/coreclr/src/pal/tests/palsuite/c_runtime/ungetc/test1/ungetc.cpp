// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*=====================================================================
**
** Source:  ungetc.c (test 1)
**
** Purpose: Tests the PAL implementation of the ungetc function by calling
**          the function on a write-only file.
**
** Dependencies:
**          fopen
**          fclose
**          fseek
**
**
**===================================================================*/

#include <palsuite.h>


int __cdecl main(int argc, char *argv[])
{
    char szFileName[] = {"test1.tmp"};
    const char text[] = 
        {"The quick brown fox jumped over the lazy dog's back."};
    FILE* pFile = NULL;
    int nChar = 65; /* 'A' */
    int nRc = 0;
    int itemsExpected;
    int itemsWritten;

    if (0 != PAL_Initialize(argc,argv))
    {
        return FAIL;
    }

    /* create the file */
    pFile = fopen(szFileName, "w");
    if (pFile == NULL)
    {
        Fail("ungetc: ERROR -> fopen failed to create the file \"%s\""
            " as write-only.\n",
            szFileName);
    }

    /* write to the file */
    itemsExpected = sizeof(text);
    itemsWritten = fwrite(text, sizeof(text[0]), sizeof(text), pFile);  
    if (itemsWritten == 0)
    {
        Trace("ungetc: ERROR -> fwrite failed to write to the file \"%s\"\n",
            szFileName);

        if (fclose(pFile) != 0)
        {
            Fail("ungetc: ERROR -> fclose failed to close the file.\n");
        }
        Fail("");

    }
    else if (itemsWritten != itemsExpected) 
    {
        Trace("ungetc: ERROR -> fwrite failed to write the correct number "
            "of characters to the file \"%s\"\n",
            szFileName);

        if (fclose(pFile) != 0)
        {
            Fail("ungetc: ERROR -> fclose failed to close the file.\n");
        }
        Fail("");
    }

    /* Close the file */
    if (fclose(pFile) != 0)
    {
        Fail("ungetc: ERROR -> fclose failed to close the file.\n");
    }

    /*
    ** open the file in write only mode and
    ** attempt to push an unread character back on the stream
    */


    /* open the file write-only */
    pFile = fopen(szFileName, "a");
    if (pFile == NULL)
    {
        Fail("ungetc: ERROR -> fopen failed to open the file \"%s\""
            " as write-only.\n",
            szFileName);
    }

    /* move the file pointer back to the beginning of the file */
    if (fseek(pFile, 1, SEEK_SET) != 0)
    {

        Trace("ungetc: ERROR -> fseek failed to move the file pointer to the "
            "beginning of the file.\n");
        if (fclose(pFile) != 0)
        {
            Trace("ungetc: ERROR -> fclose failed to close the file.\n");
        }
        Fail("");
    }

    /* call ungetc on a write-only file which should fail */
    if ((nRc = ungetc(nChar, pFile)) != EOF)
    {
        Trace("ungetc: ERROR -> ungetc returned \"%c\" when run on "
            "an write-only file.\n", nChar);
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
