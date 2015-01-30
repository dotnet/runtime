//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
//

/*=============================================================
**
** Source: isprint.c
**
** Purpose: Negative test for the isprint API. Call isprint 
**			to test if out of range characters are 
**			not printable.
**
**
**============================================================*/
#include <palsuite.h>

int __cdecl main(int argc, char *argv[])
{
    int err;

    /*Initialize the PAL environment*/
    err = PAL_Initialize(argc, argv);
    if(0 != err)
    {
        return FAIL;
    }

    /*check that function fails for values that are not printable*/
    err = isprint(0x15);
    if(err)
    {
        Fail("\nSucceeded when it should have failed because 0x15 "
        "is not in the range of printable characters\n");
    }

    err = isprint(0xAA);
    if(err)
    {
        Fail("\nSucceeded when it should have failed because 0xAA "
        "is not in the range of printable characters\n");
    }
    
    /* check carriage return */
    if(0 != isprint(0x0d))
    {
        Fail("\nSucceeded when it should have failed because 0x0d "
        "is not in the range of printable characters\n");
    }
    
    /* check line feed */
    if(0 != isprint(0x0a))
    {
        Fail("\nSucceeded when it should have failed because 0x0a "
        "is not in the range of printable characters\n");
    }
    
    PAL_Terminate();
    return PASS;
}
