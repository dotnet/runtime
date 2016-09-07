// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*=====================================================================
**
** Source:  GetFileTime.c
**
** Purpose: Test the PAL implementation of GetFileTime. This test
**          creates a file and compares create and write times after
**          the buffers are flushed, but before the close, and verifies 
**          the results are as expected
**
** Depends:
**          CreateFile
**          WriteFile
**          FlushFileBuffers
**          CloseHandle
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
    ULONG64 FirstAccess;
    ULONG64 SecondAccess;
    ULONG64 FirstCreationTime;
    ULONG64 SecondCreationTime;
    DWORD temp;
    const char* someText = "1234567890123456789012345678901234567890";

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
    if(!WriteFile(hFile, someText, strlen(someText), &temp, NULL)) 
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

    /* Flush the buffers */
    if(!FlushFileBuffers(hFile)) 
    {
        Trace("ERROR: The FlushFileBuffers function failed. "
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

    /* Convert the structures to an ULONG64 */
    FirstCreationTime = ((((ULONG64)Creation.dwHighDateTime)<<32) | 
                         ((ULONG64)Creation.dwLowDateTime));
    
    FirstWrite =        ((((ULONG64)LastWrite.dwHighDateTime)<<32) | 
                         ((ULONG64)LastWrite.dwLowDateTime));

    FirstAccess =        ((((ULONG64)LastAccess.dwHighDateTime)<<32) | 
                         ((ULONG64)LastAccess.dwLowDateTime));

    /* Sleep for 3 seconds, this will ensure the time changes */
    Sleep(3000);

    /* Write to the file again so we have something to flush */
    if(!WriteFile(hFile, someText, strlen(someText), &temp, NULL)) 
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
  
    /* Flush the buffers forcing the access/mod time to change */
    if(!FlushFileBuffers(hFile)) 
    {
        Trace("ERROR: The FlushFileBuffers function failed. "
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
  
    SecondAccess = ((((ULONG64)LastAccess.dwHighDateTime)<<32) | 
                    ((ULONG64)LastAccess.dwLowDateTime));

  
    /* Now -- to test.  We'll ensure that the Second
       LastWrite and access times are larger than the first.
       It tells us that time is passing, which is good! 
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
        Fail("ERROR: The write-file-time (%I64d) after the first flush "
            "should be less than the write-file-time (%I64d) after the second "
               "flush.\n",
               FirstWrite, 
               LastWrite);

    }

    
    if(SecondAccess < FirstAccess) 
    {
        /* Close the File */
        if(!CloseHandle(hFile)) 
        {
            Trace("ERROR: Failed to close the file handle. "
                "GetLastError returned %u.\n", 
                GetLastError());
        }
        Fail("ERROR: The access-file-time (%I64d) after the first flush "
            "should be less than or equal to the access-file-time (%I64d) "
               "after the second flush.\n",
               FirstAccess, 
               LastAccess);
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

