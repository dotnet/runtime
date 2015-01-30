//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
//

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

int __cdecl main(int argc, char *argv[])
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
