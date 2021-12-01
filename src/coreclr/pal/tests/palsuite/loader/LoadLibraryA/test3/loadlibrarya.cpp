// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*=============================================================
**
** Source: loadlibrarya.c
**
** Purpose: Negative test the LoadLibrary API with NULL module
**          name.
**
**
**============================================================*/
#include <palsuite.h>

PALTEST(loader_LoadLibraryA_test3_paltest_loadlibrarya_test3, "loader/LoadLibraryA/test3/paltest_loadlibrarya_test3")
{
    HMODULE ModuleHandle;
    int err;

    /*Initialize the PAL environment*/
    err = PAL_Initialize(argc, argv);
    if(0 != err)
    {
        return FAIL;
    }

    /*load a module by passing a NULL module name*/
    ModuleHandle = LoadLibraryA(NULL);
    if(NULL != ModuleHandle)
    {
        Fail("\nFailed to call loadlibrarya API for a negative test, "
            "call loadibrarya with NULL moudle name, a NULL module "
            "handle is expected, but no NULL module handle is returned, "
            "error code =%u\n", GetLastError());   
    }
  
    PAL_Terminate();
    return PASS;
}
