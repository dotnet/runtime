// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*============================================================
**
** Source:    test3.c
**
** Purpose:   Testing lstrcatw with two NULL strings passed on
**
**
**=========================================================*/

#define UNICODE

#include <palsuite.h>

int __cdecl main(int argc, char *argv[])
{
  


    /*
     * Initialize the PAL and return FAILURE if this fails
     */

    if(0 != (PAL_Initialize(argc, argv)))
    {
        return FAIL;
    }


    /* testing the behaviour of lstrcatW with two NULL strings */
    if( lstrcat(NULL,NULL) != NULL)
    {
        
        Fail("lstrcat:ERROR: the function should returned NULL\n");

    }

    PAL_Terminate();
    return PASS;
}



