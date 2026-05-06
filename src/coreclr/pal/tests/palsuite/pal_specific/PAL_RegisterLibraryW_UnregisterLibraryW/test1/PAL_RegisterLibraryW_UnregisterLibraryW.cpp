// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*=============================================================
**
** Source: pal_registerlibrary_unregisterlibrary
**
** Purpose: Positive test the PAL_RegisterLibrary API and
**          PAL_UnRegisterLibrary.
**          Call PAL_RegisterLibrary to map a module into the calling
**          process address space and call PAL_UnRegisterLibrary
**          to unmap this module.
**
**
**============================================================*/
#define UNICODE
#include <palsuite.h>

PALTEST(pal_specific_PAL_RegisterLibraryW_UnregisterLibraryW_test1_paltest_pal_registerlibraryw_unregisterlibraryw_test1, "pal_specific/PAL_RegisterLibraryW_UnregisterLibraryW/test1/paltest_pal_registerlibraryw_unregisterlibraryw_test1")
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

    /*zero the buffer*/
    memset(ModuleName,0,64);
    sprintf_s(ModuleName, ARRAY_SIZE(ModuleName), "%s", "rotor_pal");

    /*convert a normal string to a wide one*/
    wpModuleName = convert(ModuleName);

    /*load a module*/
    ModuleHandle = PAL_RegisterLibrary(wpModuleName);

    /*free the memory*/
    free(wpModuleName);

    if(!ModuleHandle)
    {
        Fail("Failed to call PAL_RegisterLibrary API to map a module "
            "into calling process, error code=%u!\n", GetLastError());
    }

    /*decrement the reference count of the loaded DLL*/
    err = PAL_UnregisterLibrary(ModuleHandle);
    if(0 == err)
    {
        Fail("\nFailed to call PAL_UnregisterLibrary API to "
                "decrement the count of the loaded DLL module, "
                "error code=%u!\n", GetLastError());
    }

    PAL_Terminate();
    return PASS;
}
