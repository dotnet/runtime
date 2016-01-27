// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*=============================================================
**
** Source: pal_get_stderr.c
**
** Purpose: Positive test the PAL_get_stderr API.
**          Call PAL_get_stderr to retrieve the PAL standard error
**          output stream pointer.
**          This test case should be run manually and automatically.
**          

**
**============================================================*/
#include <palsuite.h>

int __cdecl main(int argc, char *argv[])
{
    int err;
    FILE *pPAL_stderr = NULL;  
    const char *pMsg = "\nThis is a PAL_get_stderr test message, "
                    "not an error message!\n";

    /*Initialize the PAL environment*/
    err = PAL_Initialize(argc, argv);
    if(0 != err)
    {
        return FAIL;
    }

    /*retrieve the PAL standard error output stream pointer*/
    pPAL_stderr = PAL_get_stderr();  

    if(NULL == pPAL_stderr)
    {
        Fail("\nFailed to call PAL_get_stderr API, error code = %u\n",
                GetLastError());
    }    
    
    /*output a test message through PAL standard error stream*/    
    err = fputs(pMsg, pPAL_stderr);
    if(EOF == err)
    {
        Fail("\nFailed to call fputs to output message to PAL stdandard "
                "error stream, error code=%u\n", GetLastError());
    }

    PAL_Terminate();
    return PASS;
}
