// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*=============================================================
**
** Source:
**
** Purpose: Positive test the FreeLibrary API.
**
**
**============================================================*/
#include <palsuite.h>

/*char    LibraryName[] = "dlltest";*/
/* SHLEXT is defined only for Unix variants */

#if defined(SHLEXT)
#define LibraryName    "dlltest"SHLEXT
#else
#define LibraryName   "dlltest"
#endif




BOOL  PALAPI TestDll(HMODULE, int);

PALTEST(filemapping_memmgt_FreeLibrary_test1_paltest_freelibrary_test1, "filemapping_memmgt/FreeLibrary/test1/paltest_freelibrary_test1")
{
    HANDLE hLib;

    /* Initialize the PAL. */
    if ((PAL_Initialize(argc, argv)) != 0)
    {
        return (FAIL);
    }
    
    /*Load library (DLL). */
    hLib = LoadLibrary(LibraryName);

    if(hLib == NULL)
    {
        Fail("ERROR:%u:Unable to load library %s\n", 
             GetLastError(), 
             LibraryName);
    }

    /* Test access to DLL. */
    if(!TestDll(hLib, PASS))
        {
        Trace("ERROR: TestDll function returned FALSE "
             "expected TRUE\n.");
        FreeLibrary(hLib);
        Fail("");
    }

    /* Call the FreeLibrary API. */ 
    if (!FreeLibrary(hLib))
    {
        Fail("ERROR:%u: Unable to free library \"%s\"\n", 
             GetLastError(),
             LibraryName);
    }

    /* Test access to the free'd DLL. */
    if(!TestDll(hLib, FAIL))
    {
        Fail("ERROR: TestDll function returned FALSE "
            "expected TRUE\n.");
    }

    PAL_Terminate();
    return PASS;

}


BOOL PALAPI TestDll(HMODULE hLib, int testResult)
{
    int     RetVal;
#if WIN32
    char    FunctName[] = "_DllTest@0";
#else
    char    FunctName[] = "DllTest";
#endif
    FARPROC DllAddr;    

    /* Attempt to grab the proc address of the dll function.
     * This one should succeed.*/
    if(testResult == PASS)
    {
        DllAddr = GetProcAddress(hLib, FunctName);
        if(DllAddr == NULL)
        {
            Trace("ERROR: Unable to load function \"%s\" library \"%s\"\n", 
                    FunctName,
                    LibraryName);
            return (FALSE);
        }
        /* Run the function in the DLL, 
         * to ensure that the DLL was loaded properly.*/
        RetVal = DllAddr();
        if (RetVal != 1)
        {
            Trace("ERROR: Unable to receive correct information from DLL! "
                ":expected \"1\", returned \"%d\"\n",
                RetVal);
            return (FALSE);
        }
    }

    /* Attempt to grab the proc address of the dll function.
     * This one should fail.*/
    if(testResult == FAIL)
    {
        DllAddr = GetProcAddress(hLib, FunctName);
        if(DllAddr != NULL)
        {
            Trace("ERROR: Able to load function \"%s\" from free'd"
                " library \"%s\"\n", 
                FunctName, 
                LibraryName);
            return (FALSE);
        }
    }
    return (TRUE);
}
