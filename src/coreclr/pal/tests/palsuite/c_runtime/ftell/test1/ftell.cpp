// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*=====================================================================
**
** Source:  ftell.c (test 1)
**
** Purpose: Tests the PAL implementation of the ftell function.
**
**
**===================================================================*/

#include <palsuite.h>

FILE* pFile;
struct TESTS 
{
    long lDist;
    int nFrom;
    long lPosition;
};


/*************************************************
**
** Validate 
**
** Purpose:
**      Tests whether the move was successful. If
**      it passes, it returns TRUE. If it fails
**      it outputs some error messages and returns
**      FALSE.
**
*************************************************/
BOOL Validate(long lExpected)
{
    long lPos = -2;

    if (((lPos = ftell(pFile)) == -1) || (lPos != lExpected))
    {
        Trace("ftell: ERROR -> ftell returned %ld when expecting %ld.\n", 
            lPos,
            lExpected);
        if (fclose(pFile) != 0)
        {
            Trace("ftell: ERROR -> fclose failed to close the file.\n");
        }
        return FALSE;
    }
    return TRUE;
}


/*************************************************
**
** MovePointer
**
** Purpose:
**      Accepts the distance to move and the
**      distance and calls fseek to move the file
**      pointer. If the fseek fails, error messages
**      are displayed and FALSE is returned. TRUE
**      is returned on a successful fseek.
**
*************************************************/
BOOL MovePointer(long lDist, int nFrom)
{
    /* move the file pointer*/
    if (fseek(pFile, lDist, nFrom) != 0)
    {
        Trace("ftell: ERROR -> fseek failed to move the file pointer "
            "%l characters.\n",
            lDist);
        if (fclose(pFile) != 0)
        {
            Trace("ftell: ERROR -> fclose failed to close the file.\n");
        }
        return FALSE;
    }
    return TRUE;
}



PALTEST(c_runtime_ftell_test1_paltest_ftell_test1, "c_runtime/ftell/test1/paltest_ftell_test1")
{
    const char szFileName[] = {"testfile.txt"};
    long lPos = -1;
    int i;
    char szTempBuffer[256];
    struct TESTS testCase[] = 
    {
        {0, SEEK_SET, 0},
        {10, SEEK_CUR, 10},
        {-5, SEEK_CUR, 5},
        {-2, SEEK_END, 50}
    };
       


    if (0 != PAL_Initialize(argc,argv))
    {
        return FAIL;
    }

    memset(szTempBuffer, 0, 256);


    /* open the test file */
    pFile = fopen(szFileName, "r");
    if (pFile == NULL)
    {
        Fail("ftell: ERROR -> fopen failed to open the file \"%s\".\n");
    }

    /* loop through the test cases */
    for (i = 0; i < (sizeof(testCase)/sizeof(struct TESTS)); i++)
    {
        if (MovePointer(testCase[i].lDist, testCase[i].nFrom) != TRUE)
        {
            Fail("");
        }
        else if (Validate(testCase[i].lPosition) != TRUE)
        {
            Fail("");
        }
    }

    if (fclose(pFile) != 0)
    {
        Fail("ftell: ERROR -> fclose failed to close the file.\n");
    }

    /* lets just see if we can find out where we are in a closed stream... */
    if ((lPos = ftell(pFile)) != -1)
    {
        Fail("ftell: ERROR -> ftell returned a valid position (%ld) on a "
            "closed file handle\n", 
            lPos);
    }
    
    PAL_Terminate();
    return PASS;
}
