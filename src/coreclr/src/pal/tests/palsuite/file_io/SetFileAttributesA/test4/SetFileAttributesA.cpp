// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*=====================================================================
**
** Source:  SetFileAttributesA.c
**
**
** Purpose: Tests the PAL implementation of the SetFileAttributesA function
** Check that using two flags (READONLY and NORMAL) only sets the file
** as READONLY, as MSDN notes that everything else overrides NORMAL.
**
** Depends:
**        CreateFileA
**        CloseHandle
**
**
**===================================================================*/


#include <palsuite.h>



int __cdecl main(int argc, char **argv)
{
    DWORD TheResult;
    HANDLE TheFile;
    char* FileName = {"test_file"};
    
    if (0 != PAL_Initialize(argc,argv))
    {
        return FAIL;
    }
    
    // Create the test file
    FILE *testFile = fopen(FileName, "w");
    if (testFile == NULL)
    {
        Fail("Unexpected error: Unable to open file %S with fopen. \n", FileName);
    }
    if (fputs("testing", testFile) == EOF)
    {
        Fail("Unexpected error: Unable to write to file %S with fputs. \n", FileName);
    }
    if (fclose(testFile) != 0)
    {
        Fail("Unexpected error: Unable to close file %S with fclose. \n", FileName);
    }
    testFile = NULL;

    /* Try to set the file to Read-only|Normal ... It should
     end up as Readonly, since this overrides Normal*/

    TheResult = SetFileAttributesA(FileName,
                                  FILE_ATTRIBUTE_NORMAL|
                                  FILE_ATTRIBUTE_READONLY);
    
    if(TheResult == 0)
    {
        Fail("ERROR: SetFileAttributesA returned 0, failure, when trying "
               "to set the FILE_ATTRIBUTE_READONLY|FILE_ATTRIBUTE_NORMAL "
               "attribute.");
    }

    /* Attempt to open this READONLY file with WRITE access,
       The open should fail and the HANDLE should be invalid.
    */

    TheFile = CreateFileA(
        FileName,                         // file name
        GENERIC_READ|GENERIC_WRITE,       // access mode
        0,                                // share mode
        NULL,                             // SD
        OPEN_ALWAYS,                      // how to create
        FILE_ATTRIBUTE_NORMAL,            // file attributes
        NULL                              // handle to template file
        );

    if(TheFile != INVALID_HANDLE_VALUE) 
    {
        TheResult = CloseHandle(TheFile);
        if(TheResult == 0) 
        {
            Trace("ERROR: CloseHandle failed.  This tests relies upon it "
                "working properly.");
        }
        Fail("ERROR: Tried to open a file that was created as "
               "READONLY with the GENERIC_WRITE access mode.  This should"
               " cause CreateFileA to return an INVALID_HANDLE_VALUE.");
    }

    /* Try to open the file with READ access, this should be ok.
       The HANDLE will be valid.
    */

    TheFile = CreateFileA(
        FileName,                         // file name
        GENERIC_READ,                     // access mode
        0,                                // share mode
        NULL,                             // SD
        OPEN_ALWAYS,                      // how to create
        FILE_ATTRIBUTE_NORMAL,            // file attributes
        NULL                              // handle to template file
        );

    if(TheFile == INVALID_HANDLE_VALUE) 
    {
        Fail("ERROR: Tried to open a file that was created as "
               "READONLY with the GENERIC_READ access mode.  This should"
               " cause CreateFileA to return an valid handle, but "
               "INVALID_HANDLE_VALUE was returned!.");
    }
    
    /* Close that HANDLE */

    TheResult = CloseHandle(TheFile);

    if(TheResult == 0) 
    {
        Fail("ERROR: CloseHandle failed.  This tests relies upon it "
               "working properly.");
    }
    
    PAL_Terminate();
    return PASS;
}
