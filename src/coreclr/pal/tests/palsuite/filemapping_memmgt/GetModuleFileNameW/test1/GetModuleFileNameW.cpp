// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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

PALTEST(filemapping_memmgt_GetModuleFileNameW_test1_paltest_getmodulefilenamew_test1, "filemapping_memmgt/GetModuleFileNameW/test1/paltest_getmodulefilenamew_test1")
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

    ModuleFileNameBuf = (WCHAR*)malloc(MODULENAMEBUFFERSIZE*sizeof(WCHAR));

    //convert a normal string to a wide one
    lpModuleName = convert(ModuleName);

    //load a module
        ModuleHandle = LoadLibraryExW(lpModuleName, NULL, 0);

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
    char* context;
    TempBuf = strtok_r(NewModuleFileNameBuf,Delimiter, &context);
    LastBuf = TempBuf;
    while(NULL != TempBuf)
    {
        LastBuf = TempBuf;
        TempBuf = strtok_r(NULL,Delimiter, &context);
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

