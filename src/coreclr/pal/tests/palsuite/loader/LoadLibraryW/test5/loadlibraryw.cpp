// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*=============================================================
**
** Source: loadlibraryw.c
**
** Purpose: Negative test the LoadLibraryW API.
**          Call LoadLibraryW by passing a module name
**          without extension but with a trailing dot.
**
**
**============================================================*/
#define UNICODE
#include <palsuite.h>

PALTEST(loader_LoadLibraryW_test5_paltest_loadlibraryw_test5, "loader/LoadLibraryW/test5/paltest_loadlibraryw_test5")
{
    HMODULE ModuleHandle;
    int err;
    WCHAR *lpModuleName;
    char ModuleName[_MAX_FNAME];

    /* Initialize the PAL environment */
    err = PAL_Initialize(argc, argv);
    if(0 != err)
    {
        return FAIL;
    }

    memset(ModuleName, 0, _MAX_FNAME);

    /*Module name without extension but with a trailing dot*/
#if WIN32
    sprintf_s(ModuleName, ARRAY_SIZE(ModuleName),"%s","rotor_pal.");
#else
    sprintf_s(ModuleName, ARRAY_SIZE(ModuleName),"%s","librotor_pal.");
#endif

    /* convert a normal string to a wide one */
    lpModuleName = convert(ModuleName);

    /* load a module */
    ModuleHandle = LoadLibraryW(lpModuleName);

    /* free the memory */
    free(lpModuleName);

    if(NULL != ModuleHandle)
    {
        Trace("Failed to call LoadLibraryW API for a negative test "
            "call LoadLibraryW with module name which does not have "
            "extension except a trailing dot, a NULL module handle is"
            "expected, but no NULL module handle is returned, "
            "error code = %u\n", GetLastError());

        /* decrement the reference count of the loaded dll */
        err = FreeLibrary(ModuleHandle);
        if(0 == err)
        {
            Trace("\nFailed to call FreeLibrary API, "
                "error code = %u\n", GetLastError());
        }

        Fail("");
    }

    PAL_Terminate();
    return PASS;
}
