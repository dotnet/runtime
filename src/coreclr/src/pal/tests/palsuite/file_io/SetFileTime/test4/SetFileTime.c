// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*=====================================================================
**
** Source:  SetFileTime.c
**
** Purpose: Tests the PAL implementation of the SetFileTime function
** This passes a variety of NULL values as parameters to the function.
** It should still succeed.
**
** Depends:
**         CreateFile
**         

**
**===================================================================*/

#include <palsuite.h>



int __cdecl main(int argc, char **argv)
{
#if WIN32
    FILETIME Creation;
#endif
    FILETIME LastWrite,LastAccess;
    HANDLE TheFileHandle;

    
    if (0 != PAL_Initialize(argc,argv))
    {
        return FAIL;
    }
    
    /* Populate some FILETIME structures with values 
       These values are valid Creation, Access and Write times
       which I generated, and should work properly.

       These values aren't being used for comparison, so they should
       work ok, even though they weren't generated specifically for 
       FreeBSD or WIN32 ...
    */
#if WIN32
    Creation.dwLowDateTime = 458108416;
    Creation.dwHighDateTime = 29436904;
#endif
    LastAccess.dwLowDateTime = 341368832;
    LastAccess.dwHighDateTime = 29436808;

    LastWrite.dwLowDateTime = -1995099136;
    LastWrite.dwHighDateTime = 29436915;
    
    /* Open the file to get a HANDLE */
    TheFileHandle = 
        CreateFile(
            "the_file",                 
            GENERIC_READ|GENERIC_WRITE,  
            0,                           
            NULL,                        
            OPEN_ALWAYS,                 
            FILE_ATTRIBUTE_NORMAL,       
            NULL);                       

        
    if(TheFileHandle == INVALID_HANDLE_VALUE) 
    {
        Fail("ERROR: Failed to open the file.  The error number "
               "returned was %d.",GetLastError());
    }
    
    /* Pass all NULLs, this is useless but should still work. */
    if(SetFileTime(TheFileHandle,NULL,NULL,NULL)==0)
    {
        Fail("ERROR: SetFileTime returned 0, indicating failure. "
               "Three of the params were NULL in this case, did they "
               "cause the problem?");
    }
    
#if WIN32
    /* Set the Creation time of the File */
    if(SetFileTime(TheFileHandle,&Creation,NULL,NULL)==0)
    {
        Fail("ERROR: SetFileTime returned 0, indicating failure. "
               "Two of the params were NULL in this case, did they "
               "cause the problem?");
    }
#endif

#if WIN32
    /* Set the Creation, LastWrite time of the File */
    if(SetFileTime(TheFileHandle,&Creation,&LastWrite,NULL)==0)
#else
    /* Set the LastWrite time of the File */
    if(SetFileTime(TheFileHandle,NULL,&LastWrite,NULL)==0)
#endif
    {
        Fail("ERROR: SetFileTime returned 0, indicating failure. "
               "One of the params were NULL in this case, did it "
               "cause the problem?");
    }
  

    PAL_Terminate();
    return PASS;
}
