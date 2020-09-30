// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*=============================================================
**
** Source: pal_get_stdout.c
**
** Purpose: Positive test the PAL_get_stdout API.
**          Call PAL_get_stdout to retrieve the PAL standard output
**          stream pointer.
**          This test case should be run manually and automatically.
**          

**
**============================================================*/
#include <palsuite.h>

PALTEST(pal_specific_PAL_get_stdout_test1_paltest_pal_get_stdout_test1, "pal_specific/PAL_get_stdout/test1/paltest_pal_get_stdout_test1")
{
    int err;
    FILE *pPAL_stdout = NULL;  
    const char *pMsg = "\nThis is a PAL_get_stdout test output message, "
                    "not an error message!\n";

    /*Initialize the PAL environment*/
    err = PAL_Initialize(argc, argv);
    if(0 != err)
    {
        return FAIL;
    }

    /*retrieve the PAL output stream pointer*/
    pPAL_stdout = PAL_get_stdout();    
    if(NULL == pPAL_stdout)
    {
        Fail("\nFailed to call PAL_get_stdout API to retrieve the "
            "standard PAL output stream pointer, error code=%u\n",
            GetLastError());
    }

    /*output a test message through PAL standard output stream*/    
    err = fputs(pMsg, pPAL_stdout);
    if(EOF == err)
    {
        Fail("\nFailed to call fputs to output message to PAL stdandard "
                "output stream, error code=%u\n", GetLastError());
    }

    PAL_Terminate();
    return PASS;
}
