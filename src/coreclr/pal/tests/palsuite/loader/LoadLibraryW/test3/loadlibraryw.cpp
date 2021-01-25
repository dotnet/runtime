// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*=============================================================
**
** Source: loadlibraryw.c
**
** Purpose: Negative test the loadlibraryw API.
**          Call loadlibraryw with NULL module name
**
**
**============================================================*/
#define UNICODE
#include <palsuite.h>

PALTEST(loader_LoadLibraryW_test3_paltest_loadlibraryw_test3, "loader/LoadLibraryW/test3/paltest_loadlibraryw_test3")
{
    HMODULE ModuleHandle;
    int err;

    /* Initialize the PAL environment */
    err = PAL_Initialize(argc, argv);
    if(0 != err)
    {
        return FAIL;
    }

    /* load a module with a NULL module name */
    ModuleHandle = LoadLibraryW(NULL);
    if(NULL != ModuleHandle)
    {
        Fail("\nFailed to call loadlibraryw API for a negative test, "
            "call loadibraryw with NULL moudle name, a NULL module "
            "handle is expected, but no NULL module handle is returned, "
            "error code =%u\n", GetLastError());   
    }
  
    PAL_Terminate();
    return PASS;
}
