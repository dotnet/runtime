// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*=============================================================================
**
** Source: test1.c (filemapping_memmgt\getprocaddress\test1)
**
** Purpose: Positive test the GetProcAddress API.
**          The first test calls GetProcAddress to retrieve the 
**          address of SimpleFunction inside testlib by its name, 
**          then calls the function and checks that it worked. 
**
**
**===========================================================================*/
#include <palsuite.h>

typedef int (PALAPI *SIMPLEFUNCTION)(int);

/* SHLEXT is defined only for Unix variants */
#if defined(SHLEXT)
#define lpModuleName    "testlib"SHLEXT
#else
#define lpModuleName    "testlib.dll"
#endif

int __cdecl main(int argc, char *argv[])
{
    int err;
    HMODULE hModule;
    SIMPLEFUNCTION procAddressByName;

#if WIN32
    const char *FunctionName = "_SimpleFunction@4";
#else
    const char *FunctionName = "SimpleFunction";
#endif

    /* Initialize the PAL environment. */
    if(0 != PAL_Initialize(argc, argv))
    {
        return FAIL;
    }


    /* load a module */
    hModule = LoadLibrary(lpModuleName);
    if(!hModule)
    {
        Fail("Unexpected error: "
             "LoadLibrary(%s) failed.\n",
             lpModuleName);
    }

    /*
     * Test 1
     *
     * Get the address of a function 
     */
    procAddressByName = (SIMPLEFUNCTION) GetProcAddress(hModule,FunctionName);
    if(!procAddressByName)
	{
        Trace("ERROR: Unable to get address of SimpleFunction by its name. "
              "GetProcAddress returned NULL with error %d\n",
              GetLastError());

         /* Cleanup */
        err = FreeLibrary(hModule);
        if(0 == err)
	    {
            Fail("Unexpected error: Failed to FreeLibrary %s\n", 
                 lpModuleName);
	    }
        Fail("");
	}

    /* Call the function to see that it really worked */
    /* Simple function adds 1 to the argument passed */
    if( 2 != ((procAddressByName)(1)))
    { 
        Trace("ERROR: Problem calling the function by its address.\n");
         
        /* Cleanup */
        err = FreeLibrary(hModule);
        if(0 == err)
	    {
            Fail("Unexpected error: Failed to FreeLibrary %s\n", 
                 lpModuleName);
	    }
        Fail("");
    }

    /* Cleanup */
    err = FreeLibrary(hModule);
    if(0 == err)
	{
        Fail("Unexpected error: Failed to FreeLibrary %s\n", 
             lpModuleName);
	}

    PAL_Terminate();
    return PASS;
}








