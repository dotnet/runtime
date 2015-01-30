//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
//

/*============================================================
**
** Source: test.c
**
** Purpose: Test for CharNextExA function
**
**
**=========================================================*/

/* 
   This test is FINISHED.  This function is the same as CharNextA it seems,
   since the only fields that differ are always 0.
*/

#include <palsuite.h>

int __cdecl main(int argc, char *argv[]) 
{
	
    char * AnExampleString = "this is the string";
    char * StringPointer = AnExampleString;
    int count = 0;

    /*
     * Initialize the PAL and return FAILURE if this fails
     */

    if(0 != (PAL_Initialize(argc, argv)))
    {
        return FAIL;
    }
  
    /* Use CharNext to move through an entire string.  Ensure the pointer 
     *  that is returned points to the correct character, by comparing it with 
     *  'stringPointer' which isn't touched by CharNext. 
     */
  
    while(*AnExampleString != 0) 
    { 
        /* Fail if any characters are different.  This is comparing the
         *  addresses of both characters, not the characters themselves.
         */
    
        if(AnExampleString != &StringPointer[count]) 
        {
            Fail("ERROR: %#x and %#x are different.  These should be the "
                 "same address.\n",AnExampleString,&StringPointer[count]); 
        }
    
        AnExampleString = CharNextExA(0,AnExampleString,0);
        ++count;
    }	
    
    PAL_Terminate();
    return PASS;
}



