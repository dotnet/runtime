// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*=============================================================
**
** Source: pal_registerlibraryw_unregisterlibraryw_neg.c
**
** Purpose: Negative test the PAL_RegisterLibrary API.
**          Call PAL_RegisterLibrary to map a non-existent module
**          into the calling process address space.
**
**
**============================================================*/
#define UNICODE
#include <palsuite.h>

PALTEST(pal_specific_PAL_RegisterLibraryW_UnregisterLibraryW_test2_neg_paltest_reg_unreg_libraryw_neg, "pal_specific/PAL_RegisterLibraryW_UnregisterLibraryW/test2_neg/paltest_reg_unreg_libraryw_neg")
{
    HMODULE ModuleHandle;
    char ModuleName[64];
    WCHAR *wpModuleName = NULL;
    int err;

    /*Initialize the PAL environment*/
    err = PAL_Initialize(argc, argv);
    if(0 != err)
    {
        return FAIL;
    }

    memset(ModuleName, 0, 64);
    sprintf_s(ModuleName, ARRAY_SIZE(ModuleName), "%s", "not_exist_module_name");

    /*convert a normal string to a wide one*/
    wpModuleName = convert(ModuleName);

    /*load a not exist module*/
    ModuleHandle = PAL_RegisterLibrary(wpModuleName);

    /*free the memory*/
    free(wpModuleName);

    if(NULL != ModuleHandle)
    {
        Trace("ERROR: PAL_RegisterLibrary successfully mapped "
              "a module that does not exist into the calling process\n");

        /*decrement the reference count of the loaded DLL*/
        err = PAL_UnregisterLibrary(ModuleHandle);
        if(0 == err)
        {
            Trace("\nFailed to call PAL_UnregisterLibrary API to decrement the "
                "count of the loaded DLL module!\n");
        }
        Fail("");

    }

    PAL_Terminate();
    return PASS;
}
