// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*=============================================================
**
** Source: pal_get_stdin.c
**
** Purpose: Positive test the PAL_get_stdout API.
**          Call PAL_get_stdin to retrieve the PAL standard input
**          stream pointer.
**          This test case should be run manually.
**          

**
**============================================================*/
#include <palsuite.h>

PALTEST(pal_specific_PAL_get_stdin_test1_paltest_pal_get_stdin_test1, "pal_specific/PAL_get_stdin/test1/paltest_pal_get_stdin_test1")
{
    int err;
    FILE *pPAL_stdin = NULL;  
    char Buffer[256];
    
    /*Initialize the PAL environment*/
    err = PAL_Initialize(argc, argv);
    if(0 != err)
    {
        return FAIL;
    }

    /*retrieve the PAL standard input stream pointer*/
    pPAL_stdin = PAL_get_stdin();  
    if(NULL == pPAL_stdin)
    {
        Fail("\nFailed to call PAL_get_stdin API to retrieve the "
                "PAL standard input stream pointer, "
                "error code = %u\n", GetLastError());
    }    

    /*zero the buffer*/
    memset(Buffer, 0, 256);

    printf("\nPlease input some words: (less than 255 characters)\n");     

    /*further test the input stream*/
    /*read message from the PAL standard input stream*/
    if(NULL == fgets(Buffer, 255, pPAL_stdin))
    {
        Fail( "Failed to call fgets to get a string from PAL standard "
            "input stream, error code=%u\n", GetLastError());
    }    
    else
    {
        if(1 == strlen(Buffer) && Buffer[0] == '\n')
        {
            printf("\nEmpty input!\n");
        }
        else
        {
            printf("\nYour input words are:\n%s\n", Buffer);
        }
    }

    
    PAL_Terminate();
    return PASS;
}
