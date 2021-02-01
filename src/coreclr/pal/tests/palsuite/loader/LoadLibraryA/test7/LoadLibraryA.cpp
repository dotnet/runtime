// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*=============================================================
**
** Source:  loadlibrarya.c
**
** Purpose: Positive test the LoadLibrary API by calling it multiple times.
**          Call LoadLibrary to map a module into the calling 
**          process address space(DLL file)
**
**
**============================================================*/
#include <palsuite.h>

/* SHLEXT is defined only for Unix variants */

#if defined(SHLEXT)
#define ModuleName    "librotor_pal"SHLEXT
#else
#define ModuleName    "rotor_pal.dll"
#endif

PALTEST(loader_LoadLibraryA_test7_paltest_loadlibrarya_test7, "loader/LoadLibraryA/test7/paltest_loadlibrarya_test7")
{
    HMODULE ModuleHandle;
	HMODULE ReturnHandle;
    int err;

    /* Initialize the PAL environment */
    err = PAL_Initialize(argc, argv);
    if(0 != err)
    {
        ExitProcess(FAIL);
    }

    /* load a module */
    ModuleHandle = LoadLibrary(ModuleName);
    if(!ModuleHandle)
    {
		Fail("Error[%u]:Failed to call LoadLibrary API!\n", GetLastError());
    }

	/* Call LoadLibrary again, should return same handle as returned for first time */
	ReturnHandle = LoadLibrary(ModuleName);
	if(!ReturnHandle)
    {
		Fail("Error[%u]:Failed to call LoadLibrary API second time!\n", GetLastError());
    }
   
    if(ModuleHandle != ReturnHandle)
    {
		Fail("Error[%u]:Failed to return the same handle while calling LoadLibrary API twice!\n", GetLastError());
    }
 
    Trace("Value of handle ModuleHandle[%x], ReturnHandle[%x]\n", ModuleHandle, ReturnHandle);
	/* decrement the reference count of the loaded dll */
    err = FreeLibrary(ModuleHandle);
	
    if(0 == err)
    {
		Fail("Error[%u]:Failed to FreeLibrary API!\n", GetLastError());
    }
	
	/* Try Freeing a library again, should not fail */
	err = FreeLibrary(ReturnHandle);
	
    if(0 == err)
    {
		Fail("Error[%u][%d]: Was not successful in freeing a Library twice using FreeLibrary!\n", GetLastError(), err);
    }

	/* Try Freeing a library again, should fail */
	err = FreeLibrary(ReturnHandle);
	
    if(1 != err)
    {
		Fail("Error[%u][%d]: Was successful in freeing a Library thrice using FreeLibrary!\n", GetLastError(), err);
    }
    PAL_Terminate();
    return PASS;
}
