//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
//

/*=============================================================
**
** Source: getmodulefilenamew.c
**
** Purpose: Test the GetModuleFileNameW to retrieve the specified module
**          full path and file name in UNICODE.
**
**
**============================================================*/
#define UNICODE
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
    WCHAR *lpModuleName;
    DWORD ModuleNameLength;
    WCHAR *ModuleFileNameBuf;
    char* TempBuf = NULL;
    char* LastBuf = NULL;
    char NewModuleFileNameBuf[MODULENAMEBUFFERSIZE+200] = "";


    //Initialize the PAL environment
    err = PAL_Initialize(argc, argv);
    if(0 != err)
    {
        ExitProcess(FAIL);
    }

    ModuleFileNameBuf = malloc(MODULENAMEBUFFERSIZE*sizeof(WCHAR));

    //convert a normal string to a wide one
    lpModuleName = convert(ModuleName);

    //load a module
        ModuleHandle = LoadLibrary(lpModuleName);

    //free the memory
    free(lpModuleName);

    if(!ModuleHandle)
    {
        Fail("Failed to call LoadLibrary API!\n");
    }


    //retrieve the specified module full path and file name
    ModuleNameLength = GetModuleFileName(
                ModuleHandle,//specified module handle
                ModuleFileNameBuf,//buffer for module file name
                MODULENAMEBUFFERSIZE);



    //convert a wide full path name to a normal one
    strcpy(NewModuleFileNameBuf,convertC(ModuleFileNameBuf));

    //strip out all full path
    TempBuf = strtok(NewModuleFileNameBuf,Delimiter);
    LastBuf = TempBuf;
    while(NULL != TempBuf)
    {
        LastBuf = TempBuf;
        TempBuf = strtok(NULL,Delimiter);
    }


    //free the memory
    free(ModuleFileNameBuf);

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

