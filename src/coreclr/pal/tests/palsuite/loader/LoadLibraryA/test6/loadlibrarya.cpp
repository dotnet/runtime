// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*=============================================================
**
** Source:  loadlibrary.c (test 6)
**
** Purpose: Positive test the LoadLibrary API. Test will verify
**          that it is unable to load the library twice. Once by
**          using the full path name and secondly by using the
**          short name.
**

**
**============================================================*/
#include <palsuite.h>

/*Define platform specific information*/
#if defined(SHLEXT)
#define LibraryName    "dlltest"SHLEXT
#define GETATTACHCOUNTNAME "GetAttachCount"
#else
typedef int (*dllfunct)();
#define LibraryName    "dlltest.dll"
#define GETATTACHCOUNTNAME "_GetAttachCount@0"
#endif

/* Helper function to test the loaded library.
 */
BOOL PALAPI TestDll(HMODULE hLib)
{
    int     RetVal;
    char    FunctName[] = GETATTACHCOUNTNAME;
    FARPROC DllFunc;

    /* Access a function from the loaded library.
     */
    DllFunc = GetProcAddress(hLib, FunctName);
    if(DllFunc == NULL)
    {
        Trace("ERROR: Unable to load function \"%s\" from library \"%s\"\n",
              FunctName,
              LibraryName);
        return (FALSE);
    }

    /* Verify that the DLL_PROCESS_ATTACH is only
     * accessed once.*/
    RetVal = DllFunc();
    if (RetVal != 1)
    {
        Trace("ERROR: Unable to receive correct information from DLL! "
              ":expected \"1\", returned \"%d\"\n",
              RetVal);
        return (FALSE);
    }

    return (TRUE);
}

PALTEST(loader_LoadLibraryA_test6_paltest_loadlibrarya_test6, "loader/LoadLibraryA/test6/paltest_loadlibrarya_test6")
{
    HANDLE hFullLib;
    HANDLE hShortLib;
    int    iRetVal  = FAIL;
    char   fullPath[_MAX_DIR];
	char drive[_MAX_DRIVE];
    char dir[_MAX_DIR];
    char fname[_MAX_FNAME];
    char ext[_MAX_EXT];



    /* Initialize the PAL. */
    if ((PAL_Initialize(argc, argv)) != 0)
    {
        return (FAIL);
    }

    /* Initialize the buffer.
     */
    memset(fullPath, 0, _MAX_DIR);

    /* Get the full path to the library (DLL).
     */

	if (NULL != realpath(argv[0],fullpath)) {

	 _splitpath(fullPath,drive,dir,fname,ext);
	 _makepath(fullPath,drive,dir,LibraryName,"");


	} else {
		Fail("ERROR: conversion from relative path \" %s \" to absolute path failed. realpath returned NULL\n",argv[0]);
	}

    /* Call Load library with the short name of
     * the dll.
     */
    hShortLib = LoadLibrary(LibraryName);
    if(hShortLib == NULL)
    {
        Fail("ERROR:%u:Unable to load library %s\n",
             GetLastError(),
             LibraryName);
    }

    /* Test the loaded library.
     */
    if (!TestDll(hShortLib))
    {
        iRetVal = FAIL;
        goto cleanUpOne;
    }

    /* Call Load library with the full name of
     * the dll.
     */
    hFullLib = LoadLibrary(fullPath);
    if(hFullLib == NULL)
    {
        Trace("ERROR:%u:Unable to load library %s\n",
              GetLastError(),
              fullPath);
        iRetVal = FAIL;
        goto cleanUpTwo;
    }

    /* Test the loaded library.
     */
    if (!TestDll(hFullLib))
    {
        iRetVal = FAIL;
        goto cleanUpTwo;
    }

    /* Test Succeeded.
     */
    iRetVal = PASS;

cleanUpTwo:

    /* Call the FreeLibrary API.
     */
    if (!FreeLibrary(hFullLib))
    {
        Trace("ERROR:%u: Unable to free library \"%s\"\n",
              GetLastError(),
              fullPath);
        iRetVal = FAIL;
    }

cleanUpOne:

    /* Call the FreeLibrary API.
     */
    if (!FreeLibrary(hShortLib))
    {
        Trace("ERROR:%u: Unable to free library \"%s\"\n",
              GetLastError(),
              LibraryName);
        iRetVal = FAIL;
    }

    /* Terminate the PAL.
     */
    PAL_TerminateEx(iRetVal);
    return iRetVal;
}
