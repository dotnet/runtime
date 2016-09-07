// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*=====================================================================
**
** Source:  SetFileAttributesW.c
**
** Purpose: Tests the PAL implementation of the SetFileAttributesW function
** Test that we can set the defined attributes aside from READONLY on a
** file, and that it doesn't return failure.  Note, these attributes won't
** do anything to the file, however.
**
**
**===================================================================*/

#define UNICODE

#include <palsuite.h>

/* this cleanup method tries to revert the file back to its initial attributes */
void do_cleanup(WCHAR* filename, DWORD attributes)
{
    DWORD result;
    result = SetFileAttributes(filename, attributes);
    if (result == 0)
    {
        Fail("ERROR:SetFileAttributesW returned 0,failure in the do_cleanup "
             "method when trying to revert the file back to its initial attributes (%u)", GetLastError());
    }
}

int __cdecl main(int argc, char **argv)
{
    DWORD TheResult;
	DWORD initialAttr;
    CHAR *FileName_Multibyte = "test_file";
    WCHAR FileName[MAX_PATH];
    
    if (0 != PAL_Initialize(argc,argv))
    {
        return FAIL;
    }
    
    // Create the test file
    FILE *testFile = fopen(FileName_Multibyte, "w");
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

    /* Make a wide character string for the file name */
    
    MultiByteToWideChar(CP_ACP,
                        0,
                        FileName_Multibyte,
                        -1,
                        FileName,
                        MAX_PATH);

	/* Get the initial attributes of the file */
    initialAttr = GetFileAttributesW(FileName);

	/* Try to set the file to HIDDEN */

    TheResult = SetFileAttributes(FileName,FILE_ATTRIBUTE_HIDDEN);
    
    if(TheResult == 0)
    {
        do_cleanup(FileName,initialAttr);
		Fail("ERROR: SetFileAttributes returned 0, failure, when trying "
               "to set the FILE_ATTRIBUTE_HIDDEN attribute.  This should "
               "not do anything in FreeBSD, but it shouldn't fail.");
    }

    /* Try to set the file to ARCHIVE */

    TheResult = SetFileAttributes(FileName,FILE_ATTRIBUTE_ARCHIVE);
    
    if(TheResult == 0)
    {
        do_cleanup(FileName,initialAttr);
		Fail("ERROR: SetFileAttributes returned 0, failure, when trying "
               "to set the FILE_ATTRIBUTE_ARCHIVE attribute.");
    }

    /* Try to set the file to SYSTEM */

    TheResult = SetFileAttributes(FileName,FILE_ATTRIBUTE_SYSTEM);
    
    if(TheResult == 0)
    {
        do_cleanup(FileName,initialAttr);
		Fail("ERROR: SetFileAttributes returned 0, failure, when trying "
               "to set the FILE_ATTRIBUTE_SYSTEM attribute.");
    }

    /* Try to set the file to DIRECTORY */

    TheResult = SetFileAttributes(FileName,FILE_ATTRIBUTE_DIRECTORY);
    
    if(TheResult == 0)
    {
        do_cleanup(FileName,initialAttr);
		Fail("ERROR: SetFileAttributes returned 0, failure, when trying "
               "to set the FILE_ATTRIBUTE_DIRECTORY attribute.");
    }
    
	do_cleanup(FileName,initialAttr);
    PAL_Terminate();
    return PASS;
}
