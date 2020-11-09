// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*============================================================================
**
** Source: test3.c
**
** Purpose: Tests that WideCharToMultiByte correctly handles the following 
**          error conditions: insufficient buffer space, invalid code pages,
**          and invalid flags.
**
**
**==========================================================================*/


#include <palsuite.h>


PALTEST(locale_info_WideCharToMultiByte_test3_paltest_widechartomultibyte_test3, "locale_info/WideCharToMultiByte/test3/paltest_widechartomultibyte_test3")
{    
    char mbStr[128];
    WCHAR wideStr[128];
    int ret;
    int i;
    int k;
    BOOL bRet=TRUE;

    /* These codepages are currently supported by the PAL */
    int codePages[] ={
        CP_ACP,
        CP_UTF8
    };


    if (PAL_Initialize(argc, argv))
    {
        return FAIL;
    }

    /* Go through all of the code pages */
    for(i=0; i<(sizeof(codePages)/sizeof(int)); i++)
    {
    
        for (k=0; k<128; k++)
        {
            wideStr[k] = 'a';
            mbStr[k] = 0;
        }

        wideStr[127] = 0;

        /* try with insufficient buffer space */
        ret = WideCharToMultiByte(codePages[i], 0, wideStr, -1, 
                                  mbStr, 10, NULL, NULL);
        if (ret != 0)
        {
            Trace("WideCharToMultiByte did not return an error!\n"
                  "Expected return of 0, got %d for code page %d.\n", ret, 
                  codePages[i]);
            bRet = FALSE;
        }

        ret = GetLastError();
        if (ret != ERROR_INSUFFICIENT_BUFFER)
        {
            Fail("WideCharToMultiByte set the last error to %u instead of "
                 "ERROR_INSUFFICIENT_BUFFER for code page %d.\n",
                 GetLastError(),codePages[i]);
            bRet = FALSE;
        }
    }

    /* Return failure if any of the code pages returned the wrong results */
    if(!bRet)
    {
        return FAIL;
    }

    /* try with a wacky code page */
    ret = WideCharToMultiByte(-1, 0, wideStr, -1, mbStr, 128, NULL, NULL);
    if (ret != 0)
    {
        Fail("WideCharToMultiByte did not return an error!\n"
             "Expected return of 0, got %d for invalid code page.\n", ret);
    }

    ret = GetLastError();
    if (ret != ERROR_INVALID_PARAMETER)
    {
        Fail("WideCharToMultiByte set the last error to %u instead of "
             "ERROR_INVALID_PARAMETER for invalid code page -1.\n",
             GetLastError());
    }

    PAL_Terminate();

    return PASS;
}

