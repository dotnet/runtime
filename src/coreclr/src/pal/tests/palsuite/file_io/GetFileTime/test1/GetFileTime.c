// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*=====================================================================
**
** Source:  GetFileTime.c
**
** Purpose: Tests the PAL implementation of the GetFileTime function.
** This test checks the time of a file, writes to it, then checks the
** time again to ensure that write time has increased.  It
** also checks that creation time is the same under WIN32 and has
** increased under FreeBSD.
**
** Depends:
**        CreateFile
**        WriteFile
**        CloseHandle
**
**
**===================================================================*/


#include <palsuite.h>


int __cdecl main(int argc, char **argv)
{

    FILETIME Creation,LastAccess,LastWrite;
    HANDLE TheFileHandle;
    ULONG64 FirstWrite, SecondWrite, FirstCreationTime, SecondCreationTime;
    DWORD temp;
    BOOL result;

    if (0 != PAL_Initialize(argc,argv))
    {
        return FAIL;
    }
  
    /* Open the file to get a HANDLE */
    TheFileHandle = 
        CreateFile(
            "the_file",                  // File Name
            GENERIC_READ|GENERIC_WRITE,  // Access Mode
            0,                           // Share Mode
            NULL,                        // SD
            OPEN_ALWAYS,                 // Howto Create
            FILE_ATTRIBUTE_NORMAL,       // File Attributes
            NULL                         // Template file
            );                       
    
    if(TheFileHandle == INVALID_HANDLE_VALUE) 
    {
        Fail("ERROR: Failed to open the file.  The error number "
               "returned was %d.",GetLastError());
    }

  
    /* Get the Last Write, Creation and Access File time of that File */
    if(!GetFileTime(TheFileHandle,&Creation,&LastAccess,&LastWrite))
    {
        Fail("ERROR: GetFileTime returned 0, indicating failure.");
    }

    /* Convert the structure to an ULONG64 */

    FirstCreationTime = ((((ULONG64)Creation.dwHighDateTime)<<32) | 
                         ((ULONG64)Creation.dwLowDateTime));
    
    FirstWrite =        ((((ULONG64)LastWrite.dwHighDateTime)<<32) | 
                         ((ULONG64)LastWrite.dwLowDateTime));

    /* Sleep for 3 seconds, this will ensure the time changes */
    Sleep(3000);

    /* Write to the file -- this should change write access and
       last access 
    */

    result = WriteFile(TheFileHandle,   // File handle  
                       "something",     // String to write
                       9,               // Bytes to write
                       &temp,           // Bytes written
                       NULL);
    
    if(result == 0) 
    {
        Fail("ERROR: Failed to write to file. The file must be "
               "written to in order to test that the write time is "
               "updated.");
    }
  
    /* Close the File, so the changes are recorded */
    result = CloseHandle(TheFileHandle);
  
    if(result == 0) 
    {
        Fail("ERROR: Failed to close the file handle.");
    }


    /* Reopen the file */
    TheFileHandle = 
        CreateFile(
            "the_file",                  /* file name */
            GENERIC_READ|GENERIC_WRITE,  /* access mode */
            0,                           /* share mode */
            NULL,                        /* SD */
            OPEN_ALWAYS,                 /* how to create */
            FILE_ATTRIBUTE_NORMAL,       /* file attributes */
            NULL                         /* handle to template file */
            );
  

    if(TheFileHandle == INVALID_HANDLE_VALUE) 
    {
        Fail("ERROR: Failed to re-open the file.  The error number "
               "returned was %d.",GetLastError());
    }
    
    

    /* Call GetFileTime again */
    if(!GetFileTime(TheFileHandle,&Creation,&LastAccess,&LastWrite))
    {
        Fail("ERROR: GetFileTime returned 0, indicating failure.");
    }  
    
    /* Store the results in a ULONG64 */
    
    SecondCreationTime = ( (((ULONG64)Creation.dwHighDateTime)<<32) | 
                           ((ULONG64)Creation.dwLowDateTime));
  
    SecondWrite = ( (((ULONG64)LastWrite.dwHighDateTime)<<32) | 
                    ((ULONG64)LastWrite.dwLowDateTime));
  
  
    /* Now -- to test.  We'll ensure that the Second
       LastWrite time is larger than the first.  It tells us that
       time is passing, which is good! 
    */

    if(FirstWrite >= SecondWrite) 
    {
        Fail("ERROR: The last-write-file-time after writing did not "
               "increase from the original.  The second value should be "
               "larger.");
    }

#if WIN32
    /* Then we can check to make sure that the creation time
       hasn't changed.  This should always stay the same.
    */
    
    if(FirstCreationTime != SecondCreationTime) 
    {
        Fail("ERROR: The creation time after writing should not "
               "not change from the original.  The second value should be "
               "equal.");
    }
#else
    /* Then we can check to make sure that the creation time
       has changed.  Under FreeBSD it changes whenever the file is
       access or written.
    */
    
    if(FirstCreationTime >= SecondCreationTime) 
    {
        Fail("ERROR: The creation time after writing should be "
               "greater than the original.  The second value should be "
               "larger.");
    }
    
#endif
    
    PAL_Terminate();
    return PASS;
}

