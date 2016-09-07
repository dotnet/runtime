// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*=====================================================================
**
** Source:  test1.c
**
** Purpose: Tests the PAL implementation of the _wmakepath function.
**          Create a path, and ensure that it builds how it is
**          supposed to.
**
**
**===================================================================*/

#define UNICODE

#include <palsuite.h>

int __cdecl main(int argc, char **argv)
{
    WCHAR FullPath[128];
    WCHAR File[] = {'t','e','s','t','\0'};
    WCHAR Ext[] = {'t','x','t','\0'};
    char * PrintResult=NULL;  /* Used for printing out errors */
    char * PrintCorrect=NULL;

#if WIN32
    WCHAR Drive[] = {'C','\0'};
    WCHAR Dir[] = {'\\','t','e','s','t','\0'};
    WCHAR PathName[] =
        {'C',':','\\','t','e','s','t','\\','t','e',
         's','t','.','t','x','t','\0'};
#else
    WCHAR *Drive = NULL;
    WCHAR Dir[] = {'/','t','e','s','t','\0'};
    WCHAR PathName[] =
 {'/','t','e','s','t','/','t','e','s','t','.','t','x','t','\0'};
#endif

    /*
     *  Initialize the PAL and return FAIL if this fails
     */
    if (0 != (PAL_Initialize(argc,argv)))
    {
        return FAIL;
    }

    _wmakepath(FullPath, Drive, Dir, File, Ext);

    if(wcscmp(FullPath,PathName) != 0)
    {
        PrintResult = convertC(FullPath);
        PrintCorrect = convertC(PathName);

        Fail("ERROR: The pathname which was created turned out to be %s "
               "when it was supposed to be %s.\n",PrintResult,PrintCorrect);
    }


    PAL_Terminate();
    return PASS;
}













