// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*=============================================================
**
** Source:  getmodulefilenamew.c
**
** Purpose: Positive test the GetModuleFileName API.
**          Call GetModuleFileName to retrieve current process 
**          full path and file name by passing a NULL module handle
**          in UNICODE
**
**
**============================================================*/
#define UNICODE
#include <palsuite.h>

#define MODULENAMEBUFFERSIZE 1024


PALTEST(filemapping_memmgt_GetModuleFileNameW_test2_paltest_getmodulefilenamew_test2, "filemapping_memmgt/GetModuleFileNameW/test2/paltest_getmodulefilenamew_test2")
{

    DWORD ModuleNameLength;
    WCHAR *ModuleFileNameBuf;
    int err;


    //Initialize the PAL environment
    err = PAL_Initialize(argc, argv);
    if(0 != err)
    {
        ExitProcess(FAIL);
    }

    ModuleFileNameBuf = (WCHAR*)malloc(MODULENAMEBUFFERSIZE*sizeof(WCHAR));

    //retrieve the current process full path and file name
    //by passing a NULL module handle
    ModuleNameLength = GetModuleFileName(
                NULL,             //a NULL handle
                ModuleFileNameBuf,//buffer for module file name
                MODULENAMEBUFFERSIZE);

    //free the memory
    free(ModuleFileNameBuf);

    if(0 == ModuleNameLength)
    {
        Fail("\nFailed to all GetModuleFileName API!\n");
    }


    PAL_Terminate();
    return PASS;
}
