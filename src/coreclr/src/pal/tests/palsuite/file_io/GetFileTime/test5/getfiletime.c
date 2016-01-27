// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*=====================================================================
**
** Source:  GetFileTime.c
**
** Purpose: Test the PAL implementation of GetFileTime. This test
**          creates a file and compares create and write times between
**          writes, but before the close, and verifies the results are
**          as expected
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
    FILETIME Creation;
    FILETIME LastAccess;
    FILETIME LastWrite;
    HANDLE hFile;
    ULONG64 FirstWrite;
    ULONG64 SecondWrite;
    ULONG64 FirstCreationTime;
    ULONG64 SecondCreationTime;
    DWORD temp;

    if (0 != PAL_Initialize(argc,argv))
    {
        return FAIL;
    }
  
    /* Open the file to get a HANDLE */
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

     /* Write to the file -- this should change write access and
       last access 
    */
    if(!WriteFile(hFile, "something", 9, &temp, NULL)) 
    {
        Trace("ERROR: Failed to write to file. The file must be "
               "written to in order to test that the write time is "
               "updated. GetLastError returned %u.\n", 
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

    FlushFileBuffers(hFile);
 
    /* Get the Last Write, Creation and Access File time of that File */
    if(!GetFileTime(hFile, &Creation, &LastAccess, &LastWrite))
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

    /* Convert the structure to an ULONG64 */

    FirstCreationTime = ((((ULONG64)Creation.dwHighDateTime)<<32) | 
                         ((ULONG64)Creation.dwLowDateTime));
    
    FirstWrite =        ((((ULONG64)LastWrite.dwHighDateTime)<<32) | 
                         ((ULONG64)LastWrite.dwLowDateTime));

    /* Sleep for 3 seconds, this will ensure the time changes */
    Sleep(3000);

    /* Write to the file again -- this should change write access and
       last access 
    */
    if(!WriteFile(hFile, "something", 9, &temp, NULL)) 
    {
        Trace("ERROR: Failed to write to file. The file must be "
               "written to in order to test that the write time is "
               "updated. GetLastError returned %u.\n", 
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
  

    FlushFileBuffers(hFile);

    /* Call GetFileTime again */
    if(!GetFileTime(hFile,&Creation,&LastAccess,&LastWrite))
    {
        Trace("ERROR: GetFileTime returned 0, indicating failure."
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
        /* Close the File */
        if(!CloseHandle(hFile)) 
        {
            Trace("ERROR: Failed to close the file handle. "
                "GetLastError returned %u.\n", 
                GetLastError());
        }
        Fail("ERROR: The last-write-file-time after writing did not "
               "increase from the original.  The second value should be "
               "larger.\n");
    }

#if WIN32
    /* Then we can check to make sure that the creation time
       hasn't changed.  This should always stay the same.
    */
    
    if(FirstCreationTime != SecondCreationTime) 
    {
        /* Close the File */
        if(!CloseHandle(hFile)) 
        {
            Trace("ERROR: Failed to close the file handle. "
                "GetLastError returned %u.\n", 
                GetLastError());
        }
        Fail("ERROR: The creation time after writing should not "
               "not change from the original.  The second value should be "
               "equal.\n");
    }
#else
    /* Then we can check to make sure that the creation time
       has changed.  Under FreeBSD it changes whenever the file is
       access or written.
    */
    
    if(FirstCreationTime >= SecondCreationTime) 
    {
        /* Close the File */
        if(!CloseHandle(hFile)) 
        {
            Trace("ERROR: Failed to close the file handle. "
                "GetLastError returned %u.\n", 
                GetLastError());
        }
        Fail("ERROR: The creation time after writing should be "
               "greater than the original.  The second value should be "
               "larger.\n");
    }
    
#endif
    
    /* Close the File */
    if(!CloseHandle(hFile)) 
    {
        Fail("ERROR: Failed to close the file handle. "
            "GetLastError returned %u.\n", 
            GetLastError());
    }

    PAL_Terminate();
    return PASS;
}

