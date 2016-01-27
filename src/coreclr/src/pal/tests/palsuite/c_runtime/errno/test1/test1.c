// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*============================================================================
**
** Source:  test1.c
**
** Purpose: Test that errno begins as 0, and sets to ERANGE when that 
** error is forced with wcstoul.  
**
**
**==========================================================================*/

#include <palsuite.h>

int __cdecl main(int argc, char *argv[])
{
    WCHAR overstr[] = {'4','2','9','4','9','6','7','2','9','6',0};
    WCHAR *end;
    
    if (PAL_Initialize(argc, argv))
    {
        return FAIL;
    }
    
    /* 
       From rotor.doc:  The only value that must be supported is 
       ERANGE, in the event that wcstoul() fails due to overflow. 
    */ 
    
    wcstoul(overstr, &end, 10);
    
    if (errno != ERANGE)
    {
        Fail("ERROR: wcstoul did not set errno to ERANGE.  Instead "
             "the value of errno is %d\n", errno);
    }

        
    PAL_Terminate();
    return PASS;
}
