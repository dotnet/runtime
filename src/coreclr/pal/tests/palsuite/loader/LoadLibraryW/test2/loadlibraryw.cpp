// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*=============================================================
**
** Source: loadlibraryw.c
**
** Purpose: Negative test the LoadLibraryW API.
**          Call LoadLibraryW with a not exist module Name 
**
**
**============================================================*/
#define UNICODE
#include <palsuite.h>

PALTEST(loader_LoadLibraryW_test2_paltest_loadlibraryw_test2, "loader/LoadLibraryW/test2/paltest_loadlibraryw_test2")
{
    HMODULE ModuleHandle;
    int err;
    WCHAR *pwModuleName;
    const char *pModuleName = "Not-exist-module-name";

    /* Initialize the PAL environment */
    err = PAL_Initialize(argc, argv);
    if(0 != err)
    {
        return FAIL;
    }
    
    /* convert a normal string to a wide one */
    pwModuleName = convert((char *)pModuleName);


    /*try to load a not exist module */
    ModuleHandle = LoadLibraryW(pwModuleName);
    
    /* free the memory */
    free(pwModuleName);
    
    if(NULL != ModuleHandle)
    {
        Trace("Failed to call LoadLibraryW with a not exist mudule name, "
            "a NULL module handle is expected, but no NULL module handle "
            "is returned, error code=%u\n", GetLastError());

        /* decrement the reference count of the loaded module */
        err = FreeLibrary(ModuleHandle);
        if(0 == err)
        {
            Trace("\nFailed to all FreeLibrary API to decrement "
                    "the reference count of the loaded module, "
                    "error code = %u\n", GetLastError());

        }

        Fail("");
    }

    PAL_Terminate();
    return PASS;
}
