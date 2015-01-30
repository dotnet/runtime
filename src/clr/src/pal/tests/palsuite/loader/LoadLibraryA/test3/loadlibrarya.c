//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
//

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

int __cdecl main(int argc, char *argv[])
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
