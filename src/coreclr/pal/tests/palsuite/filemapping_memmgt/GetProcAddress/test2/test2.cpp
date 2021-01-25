// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*=============================================================================
**
** Source: test2.c (filemapping_memmgt\getprocaddress\test2)
**
** Purpose: This test tries to call GetProcAddress with
**          a NULL handle, with a NULL function name, with an empty 
**          function name, with an invalid name and with an 
**          invalid ordinal value.
**
**
**===========================================================================*/
#include <palsuite.h>


/* SHLEXT is defined only for Unix variants */
#if defined(SHLEXT)
#define lpModuleName    "testlib"SHLEXT
#else
#define lpModuleName    "testlib.dll"
#endif


/**
 * main
 */
PALTEST(filemapping_memmgt_GetProcAddress_test2_paltest_getprocaddress_test2, "filemapping_memmgt/GetProcAddress/test2/paltest_getprocaddress_test2")
{
    int err;
    HMODULE hModule;
    FARPROC procAddress;

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
     * Call GetProcAddress with a NULL handle
     */
    procAddress = GetProcAddress(NULL,"SimpleFunction");
    if(procAddress != NULL)
	{
        Trace("ERROR: GetProcAddress with a NULL handle "
              "returned a non-NULL value when it should have "
              "returned a NULL value with an error\n");

         /* Cleanup */
        err = FreeLibrary(hModule);
        if(0 == err)
	    {
            Fail("Unexpected error: Failed to FreeLibrary %s\n", 
                 lpModuleName);
	    }
        Fail("");
	}

    /**
     * Test 2
     *
     * Call GetProcAddress with a NULL function name
     */

    procAddress = GetProcAddress(hModule,NULL);
    if(procAddress != NULL)
	{
        Trace("ERROR: GetProcAddress with a NULL function name "
              "returned a non-NULL value when it should have "
              "returned a NULL value with an error\n");

         /* Cleanup */
        err = FreeLibrary(hModule);
        if(0 == err)
	    {
            Fail("Unexpected error: Failed to FreeLibrary %s\n", 
                 lpModuleName);
	    }
        Fail("");
	}

    /**
     * Test 3
     *
     * Call GetProcAddress with an empty function name string
     */

    procAddress = GetProcAddress(hModule,"");
    if(procAddress != NULL)
	{
        Trace("ERROR: GetProcAddress with an empty function name "
              "returned a non-NULL value when it should have "
              "returned a NULL value with an error\n");

         /* Cleanup */
        err = FreeLibrary(hModule);
        if(0 == err)
	    {
            Fail("Unexpected error: Failed to FreeLibrary %s\n", 
                 lpModuleName);
	    }
        Fail("");
	}

    /**
     * Test 4
     *
     * Call GetProcAddress with an invalid name
     */

    procAddress = GetProcAddress(hModule,"Simple Function");
    if(procAddress != NULL)
	{
        Trace("ERROR: GetProcAddress with an invalid function name "
              "returned a non-NULL value when it should have "
              "returned a NULL value with an error\n");

         /* Cleanup */
        err = FreeLibrary(hModule);
        if(0 == err)
	    {
            Fail("Unexpected error: Failed to FreeLibrary %s\n", 
                 lpModuleName);
	    }
        Fail("");
	}

    /* cleanup */
    err = FreeLibrary(hModule);
    if(0 == err)
	{
        Fail("Unexpected error: Failed to FreeLibrary %s\n", 
              lpModuleName);
	}

    PAL_Terminate();
    return PASS;
}
