// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*=====================================================================
**
** Source:  GetFileTime.c
**
** Purpose: Tests the PAL implementation of the GetFileTime function
** Test to see that creation time is changed when two different files
** are created. 
**
** Depends:
**         CreateFile
**         ReadFile
**         CloseHandle
**
**
**===================================================================*/


#include <palsuite.h>


int __cdecl main(int argc, char **argv)
{

    FILETIME Creation;
    HANDLE TheFileHandle, SecondFileHandle;
    ULONG64 FirstCreationTime, SecondCreationTime;
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
    

    /* Get the Creation time of the File */
    if(GetFileTime(TheFileHandle,&Creation,NULL,NULL)==0)
    {
        Fail("ERROR: GetFileTime returned 0, indicating failure. "
               "Two of the params were NULL in this case, did they "
               "cause the probleM?");
    }
  
    /* Convert the structure to an ULONG64 */

    FirstCreationTime = ( (((ULONG64)Creation.dwHighDateTime)<<32) | 
                          ((ULONG64)Creation.dwLowDateTime));


    /* Close the File, so the changes are recorded */
    result = CloseHandle(TheFileHandle);
  
    if(result == 0) 
    {
        Fail("ERROR: Failed to close the file handle.");
    }


    /* Sleep for 3 seconds, this will ensure the time changes */
    Sleep(3000);


 
    /* Open another file */
    SecondFileHandle = 
        CreateFile("the_other_file",                  /* file name */ 
                   GENERIC_READ,                /* access mode */
                   0,                           /* share mode */
                   NULL,                        /* SD */ 
                   CREATE_ALWAYS,                 /* how to create */
                   FILE_ATTRIBUTE_NORMAL,       /* file attributes */
                   NULL                         /* handle to template file */
            );
    
    if(SecondFileHandle == INVALID_HANDLE_VALUE) 
    {
        Fail("ERROR: Failed to open the second file.  The error number "
               "returned was %d.",GetLastError());
    }

    
    /* Call GetFileTime again  */
    if(GetFileTime(SecondFileHandle,&Creation,NULL,NULL) == 0)
    {
        Fail("ERROR: GetFileTime returned 0, indicating failure. "
               "Perhaps the NULLs in the function broke it?");
    }

    /* Close the File*/
    result = CloseHandle(SecondFileHandle);
    
    if(result == 0) 
    {
        Fail("ERROR: Failed to close the file handle.");
    }


    /* Store the results in a ULONG64 */

    SecondCreationTime = ( (((ULONG64)Creation.dwHighDateTime)<<32) | 
                           ((ULONG64)Creation.dwLowDateTime));
  
  
  
    /* Now -- to test. We ensure that the FirstCreationTime is
       less than the SecondCreationTime
    */

   
    if(FirstCreationTime >= SecondCreationTime) 
    {
        Fail("ERROR: The creation time of the two files should be "
               "different.  The first file should have a creation "
               "time less than the second.");
    }

  

    PAL_Terminate();
    return PASS;
}
