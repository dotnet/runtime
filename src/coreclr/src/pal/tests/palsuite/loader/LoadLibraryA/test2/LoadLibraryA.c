//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
//

/*=============================================================
**
** Source: loadlibrarya.c
**
** Purpose: Negative test the LoadLibraryA API.
**          Call LoadLibraryA with a not exist module Name 
**
**
**============================================================*/
#include <palsuite.h>

int __cdecl main(int argc, char *argv[])
{
    HMODULE ModuleHandle;
    int err;
    const char *pModuleName = "Not-exist-module-name";

    /* Initialize the PAL environment */
    err = PAL_Initialize(argc, argv);
    if(0 != err)
    {
        return FAIL;
    }
    
    /*try to load a not exist module */
    ModuleHandle = LoadLibraryA(pModuleName);
    if(NULL != ModuleHandle)
    {
        Trace("Failed to call LoadLibraryA with a not exist mudule name, "
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
