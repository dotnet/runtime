// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*=============================================================
**
** Source: getmodulefilenamea.c
**
** Purpose: Positive test the GetModuleFileName API.
**          Call GetModuleFileName to retrieve the specified
**          module full path and file name
**
**
**============================================================*/

#include <palsuite.h>

#define MODULENAMEBUFFERSIZE 1024

/* SHLEXT is defined only for Unix variants */

#if defined(SHLEXT)
#define ModuleName   "librotor_pal"SHLEXT
#define Delimiter    "/"
#else
#define ModuleName   "rotor_pal.dll"
#define Delimiter    "\\"
#endif

int __cdecl main(int argc, char *argv[])
{
    HMODULE ModuleHandle;
    int err;
    DWORD ModuleNameLength;
    char ModuleFileNameBuf[MODULENAMEBUFFERSIZE]="";
    char* TempBuf = NULL;
    char* LastBuf = NULL;

    //Initialize the PAL environment
    err = PAL_Initialize(argc, argv);
    if(0 != err)
    {
        ExitProcess(FAIL);
    }


    //load a module
    ModuleHandle = LoadLibrary(ModuleName);
    if(!ModuleHandle)
    {
        Fail("Failed to call LoadLibrary API!\n");
    }


    //retrieve the specified module full path and file name
    ModuleNameLength = GetModuleFileName(
                ModuleHandle,//specified module handle
                ModuleFileNameBuf,//buffer for module file name
                MODULENAMEBUFFERSIZE);

    //strip out all full path
    TempBuf = strtok(ModuleFileNameBuf,Delimiter);
    LastBuf = TempBuf;
    while(NULL != TempBuf)
    {
        LastBuf = TempBuf;
        TempBuf = strtok(NULL,Delimiter);
    }


    if(0 == ModuleNameLength || strcmp(ModuleName,LastBuf))
    {
        Trace("\nFailed to all GetModuleFileName API!\n");
        err = FreeLibrary(ModuleHandle);
        if(0 == err)
        {
            Fail("\nFailed to all FreeLibrary API!\n");
        }
        Fail("");
    }

        //decrement the reference count of the loaded dll
    err = FreeLibrary(ModuleHandle);
    if(0 == err)
    {
        Fail("\nFailed to all FreeLibrary API!\n");
    }

    PAL_Terminate();
    return PASS;
}

