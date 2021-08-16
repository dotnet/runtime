// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*=============================================================
**
** Source:  loadlibraryw.c
**
** Purpose: Positive test the LoadLibrary API.
**          Call LoadLibrary to map a module into the calling
**          process address space(DLL file)
**
**
**============================================================*/
#define UNICODE
#include <palsuite.h>
/* SHLEXT is defined only for Unix variants */

#if defined(SHLEXT)
#define ModuleName    "librotor_pal"SHLEXT
#else
#define ModuleName    "rotor_pal.dll"
#endif

PALTEST(loader_LoadLibraryW_test1_paltest_loadlibraryw_test1, "loader/LoadLibraryW/test1/paltest_loadlibraryw_test1")
{
    HMODULE ModuleHandle;
    int err;
    WCHAR *lpModuleName;

    /* Initialize the PAL environment */
    err = PAL_Initialize(argc, argv);
    if(0 != err)
    {
        ExitProcess(FAIL);
    }

    /* convert a normal string to a wide one */
    lpModuleName = convert(ModuleName);

    /* load a module */
    ModuleHandle = LoadLibrary(lpModuleName);

    /* free the memory */
    free(lpModuleName);

    if(!ModuleHandle)
    {
        Fail("Failed to call LoadLibrary API!\n");
    }


    /* decrement the reference count of the loaded dll */
    err = FreeLibrary(ModuleHandle);
    if(0 == err)
    {
        Fail("\nFailed to all FreeLibrary API!\n");
    }

    PAL_Terminate();
    return PASS;
}
