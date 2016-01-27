// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*=============================================================
**
** Source: test4.c
**
** Purpose: 
** - Attempt to call LockFile on a file without GENERIC_READ or
** GENERIC_WRITE  (this should fail)
** - Attempt to overlap two locks, this should fail.
**
**
**============================================================*/

#include <palsuite.h>

char fileName[] = "testfile.tmp";

void OverlapTest() 
{
    HANDLE TheFile = NULL;
    DWORD FileStart = 0;
    const char lpBuffer[] = "This is a test file.";
    DWORD bytesWritten;
    BOOL bRc = TRUE;
    
    TheFile = CreateFile(fileName,     
                         GENERIC_READ|GENERIC_WRITE, 
                         FILE_SHARE_READ|FILE_SHARE_WRITE,
                         NULL,                          
                         CREATE_ALWAYS,                 
                         FILE_ATTRIBUTE_NORMAL, 
                         NULL);
    
    if (TheFile == INVALID_HANDLE_VALUE) 
    { 
        Fail("ERROR: Could not open file '%s' with CreateFile. "
             "GetLastError() returned %d.",fileName,GetLastError()); 
    } 

    bRc = WriteFile(TheFile,
                    lpBuffer,
                    (DWORD)sizeof(lpBuffer),
                    &bytesWritten,
                    NULL);

    if(!bRc)
    {      
        Trace("ERROR: Could not write to file '%s' with WriteFile.",fileName);

        if(CloseHandle(TheFile) == 0)
        {
            Fail("ERROR: CloseHandle failed to close the file.");
        }
        Fail("");

    }
    else if(bytesWritten != (DWORD)sizeof(lpBuffer))
    {
        Trace("ERROR: Could not write the correct number of bytes to the "
        "file '%s' with WriteFile.",fileName);

        if(CloseHandle(TheFile) == 0)
        {
            Fail("ERROR: CloseHandle failed to close the file.");
        }
        Fail("");
    }

    /* Lock the First 5 bytes of the File */
    
    if(LockFile(TheFile, FileStart, 0, 5, 0) == 0)
    {
        Trace("ERROR: LockFile failed in Overlap test.  "
             "GetLastError returns %d.",
             GetLastError());

        if(CloseHandle(TheFile) == 0)
        {
            Fail("ERROR: CloseHandle failed to close the file.");
        }
        Fail("");
    }

    /* Lock from Byte 2 until 7 -- this overlaps and should return failure. */
    if(LockFile(TheFile,FileStart+2, 0, 5, 0) != 0)
    {
        Trace("ERROR: LockFile returned success when it was overlapped on "
             "an already locked region of the file.");

        if(CloseHandle(TheFile) == 0)
        {
            Fail("ERROR: CloseHandle failed to close the file.");
    }
        Fail("");
    }

    /* Unlock the file */
    if(UnlockFile(TheFile, FileStart, 0, 5, 0) == 0)
    {
        Trace("ERROR: UnlockFile failed in Overlap test.  GetLastError "
             "returns %d.",GetLastError());

        if(CloseHandle(TheFile) == 0)
        {
            Fail("ERROR: CloseHandle failed to close the file.");
        }
        Fail("");
    }

    /* Close the File */
    if(CloseHandle(TheFile) == 0)
    {
        Fail("ERROR: CloseHandle failed to close the file in the Overlap "
             "test.  GetLastError() returned %d.",GetLastError());
    }
}

void FlagsTest(DWORD TheFlags, int ExpectedResult) 
{    
    HANDLE TheFile = NULL;
    DWORD FileStart = 0;
    int result;

    TheFile = CreateFile(fileName,     
                         TheFlags, 
                         FILE_SHARE_READ|FILE_SHARE_WRITE,
                         NULL,                          
                         OPEN_EXISTING,                 
                         FILE_ATTRIBUTE_NORMAL, 
                         NULL);
    
    if (TheFile == INVALID_HANDLE_VALUE) 
    { 
        Fail("ERROR: Could not open file '%s' with CreateFile. "
             "GetLastError() returned %d.",fileName,GetLastError()); 
    } 

    /* Lock the First 5 bytes of the File.  The result of this depends 
       upon which flags were set with the CreateFile.
    */
    
    result = LockFile(TheFile, FileStart, 0, 5, 0);
    
    /* If the expected result is 1, check to ensure the result is non-zero, 
       as non-zero is returned on success 
    */
    if(ExpectedResult == 1)
    {
        if(result == 0)
        {
            Trace("ERROR: LockFile returned zero when the expected result "
                 "was non-zero.  It was passed the flag value %d.",
                 TheFlags);   

            if(CloseHandle(TheFile) == 0)
            {
                Fail("ERROR: CloseHandle failed to close the file.");
            }
            Fail("");
        }
    }
    /* If the expected result is 0, check to ensure the result is 0 */
    else 
    {
        if(result != 0)
        {
            Trace("ERROR: LockFile returned %d when the expected result "
                 "was zero.  It was passed the flag value %d.",
                 result, TheFlags);

            if(CloseHandle(TheFile) == 0)
            {
                Fail("ERROR: CloseHandle failed to close the file.");
        }
            Fail("");
    }
    }
    
    /* Only unlock the file if we expect it to be successfully locked */
    if(ExpectedResult)
    {
        if(UnlockFile(TheFile,FileStart,0, 5, 0) == 0)
        {
            Fail("ERROR: UnlockFile failed in the Flags Test.  GetLastError() "
                 "returned %d.",GetLastError());

            if(CloseHandle(TheFile) == 0)
            {
                Fail("ERROR: CloseHandle failed to close the file.");
            }
            Fail("");
        }
    }
    
    /* Close the File */
    if(CloseHandle(TheFile) == 0)
    {
        Fail("ERROR: CloseHandle failed to close the file in the Flags "
             "test. GetLastError() returned %d.",GetLastError());
    }
}

int __cdecl main(int argc, char *argv[])
{
 
    if(0 != (PAL_Initialize(argc, argv)))
    {
        return FAIL;
    }

    /* This test opens a file, then calls lock twice, overlapping the 
       regions and checking to ensure that this causes an error.
    */
    OverlapTest();
    
    /* Test that LockFile fails if no flags are set */
    FlagsTest(0,0);
    
    /* Test that LockFile passes if only GENERIC_READ is set */
    FlagsTest(GENERIC_READ,1);

    /* Test that LockFile passes if only GENERIC_WRITE is set */
    FlagsTest(GENERIC_WRITE,1);
    
    PAL_Terminate();
    return PASS;
}

