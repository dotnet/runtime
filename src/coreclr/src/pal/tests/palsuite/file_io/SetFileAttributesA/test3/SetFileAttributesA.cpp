// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*=====================================================================
**
** Source:  SetFileAttributesA.c
**
**
** Purpose: Tests the PAL implementation of the SetFileAttributesA function
** Test that the function fails if the file doesn't exist..
**
**
**===================================================================*/


#include <palsuite.h>


int __cdecl main(int argc, char **argv)
{
    DWORD TheResult;
    char* FileName = {"no_file"};
    
    if (0 != PAL_Initialize(argc,argv))
    {
        return FAIL;
    }
    
    
    /* Try to set the file to NORMAL on a file that doesn't
       exist.
    */

    TheResult = SetFileAttributesA(FileName, FILE_ATTRIBUTE_NORMAL);
    
    if(TheResult != 0)
    {
        Fail("ERROR: SetFileAttributesA returned non-zero0, success, when"
               " trying to set the FILE_ATTRIBUTE_NORMAL attribute on a non "
               "existant file.  This should fail.");
    }

   
    
    PAL_Terminate();
    return PASS;
}
