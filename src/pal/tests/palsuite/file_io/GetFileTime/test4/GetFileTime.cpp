// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*=====================================================================
**
** Source:  GetFileTime.c
**
** Purpose: Tests the PAL implementation of the GetFileTime function
** Test to see that passing NULL values to GetFileTime works and that
** calling the function on a bad HANDLE causes the correct failure.
**
** Depends:
**         CreateFile
**         CloseHandle
**
**
**===================================================================*/


#include <palsuite.h>


int __cdecl main(int argc, char **argv)
{

    FILETIME Creation,LastWrite,LastAccess;
    HANDLE TheFileHandle;
    BOOL result;
    
    if (0 != PAL_Initialize(argc,argv))
    {
        return FAIL;
    }
    
    /* Open the file to get a HANDLE */
    TheFileHandle = 
        CreateFile(
            "the_file",                 
            GENERIC_READ,  
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
    if(GetFileTime(TheFileHandle,NULL,NULL,NULL)==0)
    {
        Fail("ERROR: GetFileTime returned 0, indicating failure. "
               "Three of the params were NULL in this case, did they "
               "cause the problem?");
    }
    

    /* Get the Creation time of the File */
    if(GetFileTime(TheFileHandle,&Creation,NULL,NULL)==0)
    {
        Fail("ERROR: GetFileTime returned 0, indicating failure. "
               "Two of the params were NULL in this case, did they "
               "cause the probleM?");
    }

    /* Get the Creation, LastWrite time of the File */
    if(GetFileTime(TheFileHandle,&Creation,&LastWrite,NULL)==0)
    {
        Fail("ERROR: GetFileTime returned 0, indicating failure. "
               "One of the params were NULL in this case, did it "
               "cause the problem?");
    }
  

    /* Close the File, so the changes are recorded */
    result = CloseHandle(TheFileHandle);
  
    if(result == 0) 
    {
        Fail("ERROR: Failed to close the file handle.");
    }

    /* Call GetFileTime again  */
    if(GetFileTime(TheFileHandle,&Creation,&LastWrite,&LastAccess) != 0)
    {
        Fail("ERROR: GetFileTime returned non zero, indicating success. "
               "It was passed an invalid file HANDLE and should have "
               "failed.");
    }

    PAL_Terminate();
    return PASS;
}
