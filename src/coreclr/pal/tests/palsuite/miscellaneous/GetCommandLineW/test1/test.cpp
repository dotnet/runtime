// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*============================================================
**
** Source : test.c
**
** Purpose: Test for GetCommandLineW() function
**
**
**=========================================================*/

#define UNICODE
#include <palsuite.h>

PALTEST(miscellaneous_GetCommandLineW_test1_paltest_getcommandlinew_test1, "miscellaneous/GetCommandLineW/test1/paltest_getcommandlinew_test1")
{

    LPWSTR TheResult = NULL;	
    WCHAR *CommandLine;
    int i;
    WCHAR * p;

    /*
     * Initialize the PAL and return FAILURE if this fails
     */

    if(0 != (PAL_Initialize(argc, argv)))
    {
        return FAIL;
    }

    CommandLine = (WCHAR*)malloc(1024);
    wcscpy(CommandLine,convert(argv[0]));

    for(i=1;i<argc;++i) 
    {
        wcscat(CommandLine,convert(" "));
        wcscat(CommandLine,convert(argv[i]));
    }
  
    TheResult = GetCommandLine();
  
    /* If it is NULL, it failed. */
    if(TheResult == NULL) 
    {
        Fail("ERROR: The command line returned was NULL -- but should be "
	     "a LPWSTR.");      
    }

    // It's ok that if there is trailing white spaces in "TheResult"
    // Let's trim them.
    p = TheResult + wcslen(TheResult) - 1;
    while (* p == L' ' || * p == L'\t') { printf("%c\n", *p); * p-- = 0 ; }
  
    if(memcmp(TheResult,CommandLine,wcslen(TheResult)*2+2) != 0) 
    {
        Fail("ERROR: The command line returned was %s instead of %s "
	     "which was the command.\n",
	     convertC(TheResult), convertC(CommandLine));
    }
  
    free(CommandLine);
    
    PAL_Terminate();
    return PASS;
  
}


