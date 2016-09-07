// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*=====================================================================
**
** Source:  SetFileTime.c
**
** Purpose: Tests the PAL implementation of the SetFileTime function.  
** This test checks to ensure that the function fails when passed an
** invalid file HANDLE
**
**
**===================================================================*/



#include <palsuite.h>




int __cdecl main(int argc, char **argv)
{

    FILETIME SetCreation, SetLastWrite, SetLastAccess;
    HANDLE TheFileHandle = NULL;
    BOOL result;
    
    if (0 != PAL_Initialize(argc,argv))
    {
        return FAIL;
    }

    /* Populate some FILETIME structures with values 
       These values are valid Creation, Access and Write times
       which I generated, and should work properly.
    */

    SetCreation.dwLowDateTime = 458108416;
    SetCreation.dwHighDateTime = 29436904;

    SetLastAccess.dwLowDateTime = 341368832;
    SetLastAccess.dwHighDateTime = 29436808;

    SetLastWrite.dwLowDateTime = -1995099136;
    SetLastWrite.dwHighDateTime = 29436915;

    
    /* Pass this function an invalid file HANDLE and it should
       fail.
    */
    
    result = SetFileTime(TheFileHandle,
                         &SetCreation,&SetLastAccess,&SetLastWrite);
    
    if(result != 0)
    {
        Fail("ERROR: Passed an invalid file HANDLE to SetFileTime, but it "
               "returned non-zero.  This should return zero for failure.");
    }
    

    PAL_Terminate();
    return PASS;
}
