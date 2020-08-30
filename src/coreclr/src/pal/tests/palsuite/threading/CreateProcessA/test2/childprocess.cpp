// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*============================================================
**
** Source: createprocessa/test2/childprocess.c
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

#include <palsuite.h>
#include "test2.h"



int __cdecl main( int argc, char **argv ) 
{
    int iRetCode = EXIT_OK_CODE; /* preset exit code to OK */
    char szBuf[BUF_LEN];


    if(0 != (PAL_Initialize(argc, argv)))
    {
        return FAIL;
    }

    if (argc != 4)
    {
        return EXIT_ERR_CODE3;
    }

    if (strcmp(argv[1], szArg1) != 0
        || strcmp(argv[2], szArg2) != 0
        || strcmp(argv[3], szArg3) != 0)
    {
        return EXIT_ERR_CODE4;
    }


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
