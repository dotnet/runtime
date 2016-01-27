// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*=====================================================================
**
** Source:  test1.c
**
** Purpose: Tests the PAL implementation of the _makepath function.
**          Create a path, and ensure that it builds how it is
**          supposed to.
** 
**
**
**===================================================================*/

#if WIN32
#define PATHNAME "C:\\test\\test.txt"
#else
#define PATHNAME "/test/test.txt"
#endif

#include <palsuite.h>

int __cdecl main(int argc, char **argv)
{
    char FullPath[128];

    /*
     *  Initialize the PAL and return FAIL if this fails
     */
    if (0 != (PAL_Initialize(argc,argv)))
    {
        return FAIL;
    }

#if WIN32
    _makepath(FullPath,"C","\\test","test","txt");
#else
    _makepath(FullPath,NULL,"/test","test","txt");
#endif

    if(strcmp(FullPath,PATHNAME) != 0)
    {
        Fail("ERROR: The pathname which was created turned out to be %s "
               "when it was supposed to be %s.\n",FullPath,PATHNAME);
    }


    PAL_Terminate();
    return PASS;
}













