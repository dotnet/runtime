// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*============================================================
**
** Source: test.c
**
** Purpose: Test for MessageBoxW() function
**
**
**=========================================================*/

#define UNICODE

#include <palsuite.h>

int __cdecl main(int argc, char *argv[]) 
{
    /* Declare Variables to use with convert()*/
    WCHAR * PalTitle = NULL;
    WCHAR * OkTesting = NULL;
    WCHAR * AbortTesting = NULL;
    WCHAR * YesTesting = NULL;
    /*
     * Initialize the PAL and return FAILURE if this fails
     */

    if(0 != (PAL_Initialize(argc, argv)))
    {
        return FAIL;
    }
    
    PalTitle = convert("Pal Testing");
    OkTesting = convert("Click OK Please!");

    /* Handle, text, title, style */
    if(MessageBox(NULL, OkTesting, 
                  PalTitle, 
                  MB_OK) != IDOK) 
    {
        free(OkTesting);
        free(PalTitle);
        Fail("ERROR: The MB_OK style should return IDOK.");
    }

    free(OkTesting);
    AbortTesting = convert("Click Abort Please!");
    if(MessageBox(NULL, 
                  AbortTesting, 
                  PalTitle, 
                  MB_ABORTRETRYIGNORE) != IDABORT) 
    {
        free(AbortTesting);
        free(PalTitle);
        Fail("ERROR: The MB_ABORTRETRYIGNORE style should "
             "return IDABORT.");   
    }

    free(AbortTesting);
    YesTesting = convert("Click No Please!");

    if(MessageBox(NULL, 
                  YesTesting, 
                  PalTitle, 
                  MB_YESNO) != IDNO) 
    {
        free(PalTitle);
        free(YesTesting);
        Fail("ERROR: The MB_YESNO style should return IDNO.");
    }    
    
    free(YesTesting);
    free(PalTitle);
    
    PAL_Terminate();
    return PASS;
}



