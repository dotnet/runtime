// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*=====================================================================
**
** Source:  GetFileTime.c
**
** Purpose: Tests the PAL implementation of the GetFileTime function.
**          Perform two reads from a file without closing until the end
**          of the test and verify that only the access times change.
**          Note: Under Win32, modify time changes as well so we will
**                check that it doesn't go backwards
**
** Depends:
**         FileTimeToDosDateTime
**         CreateFile
**         ReadFile
**         WriteFile
**         CloseHandle
**
**
**===================================================================*/


#include <palsuite.h>


int __cdecl main(int argc, char **argv)
{

    FILETIME Creation;
    FILETIME LastAccess;
    FILETIME LastWrite;
    HANDLE hFile;
    ULONG64 FirstWrite = (ULONG64)0;
    ULONG64 SecondWrite = (ULONG64)0; 
    ULONG64 FirstCreationTime = (ULONG64)0;
    ULONG64 SecondCreationTime = (ULONG64)0;
    DWORD temp;
    char ReadBuffer[10];
    WORD DosDateOne;
    WORD DosDateTwo;
    WORD DosTime;
    
    if (0 != PAL_Initialize(argc,argv))
    {
        return FAIL;
    }
    memset(&Creation, 0, sizeof(FILETIME));
    memset(&LastAccess, 0, sizeof(FILETIME));
    memset(&LastWrite, 0, sizeof(FILETIME));
    
    /* Create the file to get a HANDLE */
    hFile = CreateFile("test.tmp",                 
        GENERIC_READ|GENERIC_WRITE,  
        0,                           
        NULL,                        
        CREATE_ALWAYS,                 
        FILE_ATTRIBUTE_NORMAL,       
        NULL);                       

    if(hFile == INVALID_HANDLE_VALUE) 
    {
        Fail("ERROR: Failed to create the file.  The error number "
               "returned was %u.\n",
               GetLastError());
    }

    /* give us something to read from the file */
    if(!WriteFile(hFile, "something", 9, &temp, NULL)) 
    {
        Trace("ERROR: Failed to write to file. "
               "GetLastError returned %u.\n", 
                GetLastError());
        /* Close the File */
        if(!CloseHandle(hFile)) 
        {
            Trace("ERROR: Failed to close the file handle. "
                "GetLastError returned %u.\n", 
                GetLastError());
        }
        Fail("");
    }

    /* let's do a read to set the file times for our test */
    if(!ReadFile(hFile, &ReadBuffer, 2, &temp, NULL)) 
    {
        Trace("ERROR: Failed to read from the file. "
            "GetLastError returned %u.\n", 
            GetLastError());
        /* Close the File */
        if(!CloseHandle(hFile)) 
        {
            Trace("ERROR: Failed to close the file handle. "
                "GetLastError returned %u.\n", 
                GetLastError());
        }
        Fail("");
    }
 
    /* Get the Last Write, Creation and Access File time of the file */
    if(GetFileTime(hFile, &Creation, &LastAccess, &LastWrite)==0)
    {
        Trace("ERROR: GetFileTime returned 0, indicating failure."
            " GetLastError returned %u\n",
            GetLastError());
        /* Close the File */
        if(!CloseHandle(hFile)) 
        {
            Trace("ERROR: Failed to close the file handle. "
                "GetLastError returned %u.\n", 
                GetLastError());
        }
        Fail("");
    }

    /* Call FileTimeToDosDateTime so we can aquire just the date
       portion of the Last Access FILETIME.  
    */
    if(FileTimeToDosDateTime(&LastAccess, &DosDateOne, &DosTime) == 0)
    {
        Trace("ERROR: FiletimeToDosDateTime failed, returning 0.  "
             "GetLastError returned %u.\n",
             GetLastError());
        /* Close the File */
        if(!CloseHandle(hFile)) 
        {
            Trace("ERROR: Failed to close the file handle. "
                "GetLastError returned %u.\n", 
                GetLastError());
        }
        Fail("");
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
    memset(&Creation, 0, sizeof(FILETIME));
    memset(&LastAccess, 0, sizeof(FILETIME));
    memset(&LastWrite, 0, sizeof(FILETIME));
  
    if(!ReadFile(hFile, &ReadBuffer, 2, &temp, NULL)) 
    {
        Trace("ERROR: Failed to read from the file. "
            "GetLastError returned %u.\n", 
            GetLastError());
        /* Close the File */
        if(!CloseHandle(hFile)) 
        {
            Trace("ERROR: Failed to close the file handle. "
                "GetLastError returned %u.\n", 
                GetLastError());
        }
        Fail("");
    }
    
    
    /* Call GetFileTime to get the updated time values*/
    if(GetFileTime(hFile, &Creation, &LastAccess, &LastWrite) == 0)
    {
        Trace("ERROR: GetFileTime returned 0, indicating failure. "
             "GetLastError returned %d.\n",
             GetLastError());
        /* Close the File */
        if(!CloseHandle(hFile)) 
        {
            Trace("ERROR: Failed to close the file handle. "
                "GetLastError returned %u.\n", 
                GetLastError());
        }
        Fail("");
    }

    /* Get the Date of the LastAccessTime here again. */
    if(FileTimeToDosDateTime(&LastAccess, &DosDateTwo, &DosTime) == 0)
    {
        Trace("ERROR: FileTimeToDosDateTime failed, returning 0.  "
             "GetLastError returned %d.\n",
             GetLastError());
        /* Close the File */
        if(!CloseHandle(hFile)) 
        {
            Trace("ERROR: Failed to close the file handle. "
                "GetLastError returned %u.\n", 
                GetLastError());
        }
        Fail("");
    }
    

    /* Store the results in a ULONG64 */
    SecondCreationTime = ( (((ULONG64)Creation.dwHighDateTime)<<32) | 
                           ((ULONG64)Creation.dwLowDateTime));
  
    SecondWrite = ( (((ULONG64)LastWrite.dwHighDateTime)<<32) | 
                    ((ULONG64)LastWrite.dwLowDateTime));
  
    /* Now -- to test.  We'll ensure that the SecondWrite
       time is not less than the FirstWrite time
    */

    if(SecondWrite < FirstWrite) 
    {
        Trace("ERROR: The write-file-time (%I64d) after the first read "
            "is less than the write-file-time (%I64d) after the second "
               "read.\n",
               FirstWrite, 
               LastWrite);
        /* Close the File */
        if(!CloseHandle(hFile)) 
        {
            Trace("ERROR: Failed to close the file handle. "
                "GetLastError returned %u.\n", 
                GetLastError());
        }
        Fail("");
    }

    /* 
       For LastAccessTime, just check that the date is greater or equal
       for the second over the first.  The time is not conisered on some
       file systems.  (such as fat32)
    */
    
    if(DosDateOne > DosDateTwo) 
    {
        Trace("ERROR: The last-access-time after reading should have "
             "stayed the same or increased, but it did not.\n");
        /* Close the File */
        if(!CloseHandle(hFile)) 
        {
            Trace("ERROR: Failed to close the file handle. "
                "GetLastError returned %u.\n", 
                GetLastError());
        }
        Fail("");
    }
    
   
    /* Check to ensure CreationTime hasn't changed.  This should not
       have changed in either environment. 
    */

    if(FirstCreationTime != SecondCreationTime) 
    {
        Trace("ERROR: The creation time after reading should not "
               "not change from the original.  The second value should be "
               "equal.\n");
        /* Close the File */
        if(!CloseHandle(hFile)) 
        {
            Trace("ERROR: Failed to close the file handle. "
                "GetLastError returned %u.\n", 
                GetLastError());
        }
        Fail("");
    }
  
    /* Close the File, so the changes are recorded */
    if(!CloseHandle(hFile)) 
    {
        Fail("ERROR: Failed to close the file handle. "
            "GetLastError returned %u.\n", 
            GetLastError());
    }

    PAL_Terminate();
    return PASS;
}
