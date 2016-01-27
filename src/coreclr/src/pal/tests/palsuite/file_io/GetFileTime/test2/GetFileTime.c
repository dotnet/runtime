// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*=====================================================================
**
** Source:  GetFileTime.c
**
** Purpose: Tests the PAL implementation of the GetFileTime function
** Test to see that access date either stays the same or increases
** when a read is performed.  Write
** and creation time should stay unchanged.  Note: Under FreeBSD
** the Creation time should not change with just a read.
**
** Depends:
**         FileTimeToDosDateTime
**         CreateFile
**         ReadFile
**         CloseHandle
**
**
**===================================================================*/


#include <palsuite.h>


int __cdecl main(int argc, char **argv)
{

    FILETIME Creation,LastAccess,LastWrite;
    HANDLE TheFileHandle;
    ULONG64 FirstWrite, SecondWrite, 
        FirstCreationTime, SecondCreationTime;
    DWORD temp;
    char ReadBuffer[10];
    BOOL result;
    WORD DosDateOne, DosDateTwo, DosTime;
    
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
    
 
    /* Get the Last Write, Creation and Access File time of that File */
    if(GetFileTime(TheFileHandle,&Creation,&LastAccess,&LastWrite)==0)
    {
        Fail("ERROR: GetFileTime returned 0, indicating failure.");
    }
  
    /* Call FileTimeToDosDateTime so we can aquire just the date
       portion of the Last Access FILETIME.  
    */
    if(FileTimeToDosDateTime(&LastAccess, &DosDateOne, &DosTime) == 0)
    {
        Fail("ERROR: FiletimeToDosDateTime failed, returning 0.  "
             "GetLastError returned %d.\n",GetLastError());
    }

    /* Convert the structure to an ULONG64 */

    FirstCreationTime = ( (((ULONG64)Creation.dwHighDateTime)<<32) | 
                          ((ULONG64)Creation.dwLowDateTime));

    FirstWrite =  ( (((ULONG64)LastWrite.dwHighDateTime)<<32) | 
                    ((ULONG64)LastWrite.dwLowDateTime));

    /* Sleep for 3 seconds, this will ensure the time changes */
    Sleep(3000);

    /* Read from the file -- this should change
       last access, but we'll only check the date portion, because some file
       systems have a resolution of a day.
    */
  
    result = ReadFile(TheFileHandle,      // handle to file
                      &ReadBuffer,        // data buffer
                      2,                  // number of bytes to read
                      &temp,              // number of bytes read
                      NULL);
    
    if(result == 0) 
    {
        Fail("ERROR: Failed to read from the file.");
    }
    

    /* Close the File, so the changes are recorded */
    result = CloseHandle(TheFileHandle);
  
    if(result == 0) 
    {
        Fail("ERROR: Failed to close the file handle.");
    }

 
    /* Reopen the file */
    TheFileHandle = 
        CreateFile("the_file",                  /* file name */ 
                   GENERIC_READ,                /* access mode */
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
    if(GetFileTime(TheFileHandle,&Creation,&LastAccess,&LastWrite) == 0)
    {
        Fail("ERROR: GetFileTime returned 0, indicating failure.");
    }

    /* Get the Date of the LastAccessTime here again. */
    if(FileTimeToDosDateTime(&LastAccess, &DosDateTwo, &DosTime) == 0)
    {
        Fail("ERROR: FileTimeToDosDateTime failed, returning 0.  "
             "GetLastError returned %d.\n",GetLastError());
    }
    

    /* Store the results in a ULONG64 */

    SecondCreationTime = ( (((ULONG64)Creation.dwHighDateTime)<<32) | 
                           ((ULONG64)Creation.dwLowDateTime));
  
    SecondWrite = ( (((ULONG64)LastWrite.dwHighDateTime)<<32) | 
                    ((ULONG64)LastWrite.dwLowDateTime));
  
    /* Now -- to test.  We'll ensure that the Second
       LastWrite time is the same as the first.  This shouldn't 
       have changed.
    */

    if(FirstWrite != SecondWrite) 
    {
        Fail("ERROR: The last-write-file-time after reading  "
               "increased from the original.  The second value should be "
               "equal.");
    }


    /* 
       For LastAccessTime, just check that the date is greater or equal
       for the second over the first.  The time is not conisered on some
       file systems.  (such as fat32)
    */
    
    if(DosDateOne > DosDateTwo) 
    {
        Fail("ERROR: The last-access-time after reading should have "
             "stayed the same or increased, but it did not.\n");
    }
    
   
    /* Check to ensure CreationTime hasn't changed.  This should not
       have changed in either environment. 
    */

    if(FirstCreationTime != SecondCreationTime) 
    {
        Fail("ERROR: The creation time after reading should not "
               "not change from the original.  The second value should be "
               "equal.");
    }
  

    PAL_Terminate();
    return PASS;
}
