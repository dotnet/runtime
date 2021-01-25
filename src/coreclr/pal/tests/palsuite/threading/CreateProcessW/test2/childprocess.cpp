// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*============================================================
**
** Source: createprocessw/test2/childprocess.c
**
** Purpose: This child process reads a string from stdin
**          and writes it out to stdout & stderr
**
** Dependencies: memset
**               fgets
**               gputs
** 

**
**=========================================================*/

#define UNICODE
#include <palsuite.h>
#include "test2.h"


PALTEST(threading_CreateProcessW_test2_paltest_createprocessw_test2_child, "threading/CreateProcessW/test2/paltest_createprocessw_test2_child")
{
    int iRetCode = EXIT_OK_CODE; /* preset exit code to OK */
    char szBuf[BUF_LEN];

    WCHAR *swzParam1, *swzParam2, *swzParam3 = NULL;


    if(0 != (PAL_Initialize(argc, argv)))
    {
        return FAIL;
    }
    if (argc != 4)
    {
        return EXIT_ERR_CODE3;
    }

    swzParam1 = convert(argv[1]);
    swzParam2 = convert(argv[2]);
    swzParam3 = convert(argv[3]);

    if (wcscmp(swzParam1, szArg1) != 0
        || wcscmp(swzParam2, szArg2) != 0
        || wcscmp(swzParam3, szArg3) != 0)
    {
        return EXIT_ERR_CODE4;
    }

    free(swzParam1);
    free(swzParam2);
    free(swzParam3);

    memset(szBuf, 0, BUF_LEN);

    /* Read the string that was written by the parent */
    if (fgets(szBuf, BUF_LEN, stdin) == NULL)
    {
        return EXIT_ERR_CODE1;
    }


    /* Write the string out to the stdout & stderr pipes */
    if (fputs(szBuf, stdout) == EOF
        || fputs(szBuf, stderr) == EOF)
    {
        return EXIT_ERR_CODE2;
    }

    /* The exit code will indicate success or failure */
    PAL_TerminateEx(iRetCode);

    /* Return special exit code to indicate success or failure */
    return iRetCode;
}
