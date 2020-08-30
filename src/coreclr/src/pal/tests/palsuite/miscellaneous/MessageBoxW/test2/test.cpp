// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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

int __cdecl main(int argc, char *argv[]) {
  
    /*
     * Initialize the PAL and return FAILURE if this fails
     */

    if(0 != (PAL_Initialize(argc, argv)))
    {
        return FAIL;
    }

    /* Check to make sure there are no problems accepting all the ICON 
       styles and FLAG styles.  These don't change anything, unless 
       they don't work at all. 
    */
  
    if(MessageBox(NULL, 
                  convert("Pal Testing"), 
                  convert("Pal Title"), 
                  MB_OK |MB_ICONEXCLAMATION|MB_TASKMODAL) != IDOK) 
    {
        Fail("ERROR: The MB_OK style should always return IDOK.");    
    }
  
    if(MessageBox(NULL, 
                  convert("Pal Testing"), 
                  convert("Pal Title"), 
                  MB_OK |MB_ICONINFORMATION|MB_SYSTEMMODAL) != IDOK) 
    {
        Fail("ERROR: The MB_OK style should always return IDOK.");
    }

    /* MB_SERVICE_NOTIFICATION doesn't seem to be available under windows?  
       It claims it exists and it should be supported under FreeBSD.
    */
  
#if UNIX    
    if(MessageBox(NULL, 
                  convert("Pal Testing"), 
                  convert("Pal Title"), 
                  MB_OK |MB_ICONSTOP|MB_SERVICE_NOTIFICATION) != IDOK) 
    {
        Fail("ERROR: The MB_OK style should always return IDOK.");
    }
#endif   
  
    if(MessageBox(NULL, 
                  convert("Pal Testing"), 
                  convert("Pal Title"), 
                  MB_OK |MB_ICONQUESTION) != IDOK) 
    {
        Fail("ERROR: The MB_OK style should always return IDOK.");
    }
    
    PAL_Terminate();
    return PASS;
}


