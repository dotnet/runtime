// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*=============================================================
**
** Source: pal_getpaldirectoryw.c
**
** Purpose: Positive test the PAL_GetPALDirectoryW API.
**          Call this API to retrieve a fully-qualified 
**          directory name where the PAL DLL is loaded from.
**
**
**============================================================*/
#define UNICODE
#include <palsuite.h>

PALTEST(pal_specific_PAL_GetPALDirectoryW_test1_paltest_pal_getpaldirectoryw_test1, "pal_specific/PAL_GetPALDirectoryW/test1/paltest_pal_getpaldirectoryw_test1")
{
    int err;
    BOOL bValue;
    DWORD dwFileAttribute;
    WCHAR *wpDirectoryName = NULL;
    char *pDirectoryName = NULL;
  
    /*Initialize the PAL environment*/
    err = PAL_Initialize(argc, argv);
    if(0 != err)
    {
        return FAIL;
    }

    /*allocate momory to store the directory name*/
    wpDirectoryName = (WCHAR*)malloc(MAX_PATH*sizeof(WCHAR));
    if(NULL == wpDirectoryName)
    {
        Fail("\nFailed to allocate memory for storing directory name!\n");
    } 

    UINT size = MAX_PATH;
    /*retrieve the machine configuration directory*/
    bValue = PAL_GetPALDirectoryW(wpDirectoryName, &size);
    if(FALSE == bValue) 
    {
        free(wpDirectoryName);
        Fail("Failed to call PAL_GetPALDirectoryW API, "
                "error code =%u\n", GetLastError());
    }
    

    /*convert wide char string to a standard one*/
    pDirectoryName = convertC(wpDirectoryName);
    if(0 == strlen(pDirectoryName))
    {
        free(wpDirectoryName);
        free(pDirectoryName);
        Fail("The retrieved directory name string is empty!\n");
    }

    /*free the memory*/
    free(pDirectoryName);

    /*retrieve the attribute of a file or directory*/
    dwFileAttribute = GetFileAttributesW(wpDirectoryName);

    /*free the memory*/
    free(wpDirectoryName);

    /*check if the attribute indicates a directory*/
    if(FILE_ATTRIBUTE_DIRECTORY != 
            (dwFileAttribute & FILE_ATTRIBUTE_DIRECTORY))
    {
        Fail("The retrieved directory name is not a valid directory!\n");
    }

    PAL_Terminate();
    return PASS;
}
