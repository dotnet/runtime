// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*============================================================
**
** Source: test.c
**
** Purpose: Test for CharNextA, ensures it returns the proper char for an 
**          entire string
**
**
**=========================================================*/

#include <palsuite.h>

int __cdecl main(int argc,char *argv[]) 
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

    /* Use CharNext to move through an entire string.  Ensure the pointer that 
       is returned points to the correct character, by comparing it with 
       'StringPointer' which isn't touched by CharNext. 
    */
  
    while(*AnExampleString != '\0') 
    {
    
        /* Fail if any characters are different.  This is comparing the 
         *  addresses of both characters, not the characters themselves. 
         */
    
        if(AnExampleString != &StringPointer[count]) 
        {
            Fail("ERROR: %#x and %#x are different.  These should be the same "
                 " address.\n",AnExampleString,&StringPointer[count]);
            
            
        }
    
        AnExampleString = CharNextA(AnExampleString);
        ++count;
    }	
    
    PAL_Terminate();
    return PASS;
}



