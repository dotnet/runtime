//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
//

/*=====================================================================
**
** Source:  WriteFile.c (test 1)
**
** Purpose: Tests the PAL implementation of the WriteFile function.
**          This test will attempt to write to a NULL handle and a
**          read-only file
**
**
**===================================================================*/

#include <palsuite.h>


const char* szStringTest = "The quick fox jumped over the lazy dog's back.";
const char* szReadOnlyFile = "ReadOnly.txt";
void do_cleanup()
{
	BOOL bRc = FALSE;
	bRc = DeleteFileA(szReadOnlyFile);
    if (bRc != TRUE)
    {
		Fail ("DeleteFileA: ERROR[%ld]: During Cleanup: Couldn't delete WriteFile's"
            " \"ReadOnly.txt\"\n", GetLastError());
    }

}

int __cdecl main(int argc, char *argv[])
{
    HANDLE hFile = NULL;
    DWORD dwBytesWritten;
    BOOL bRc = FALSE;
	DWORD last_error;

    if (0 != PAL_Initialize(argc,argv))
    {
        return FAIL;
    }

    //
    // Write to a NULL handle
    //

    bRc = WriteFile(hFile, szStringTest, 20, &dwBytesWritten, NULL);

    if (bRc == TRUE)
    {
		last_error = GetLastError();
        Fail("WriteFile: ERROR[%ld] -> Able to write to a NULL handle\n", last_error);
    }


    //
    // Write to a file with read-only permissions
    //

    // create a file without write permissions
    hFile = CreateFile(szReadOnlyFile, 
        GENERIC_READ,
        FILE_SHARE_READ,
        NULL,
        OPEN_ALWAYS,
        FILE_ATTRIBUTE_NORMAL,
        NULL);

    if(hFile == INVALID_HANDLE_VALUE)
    {
        last_error = GetLastError();
        Fail("WriteFile: ERROR[%ld] -> Unable to create file \"%s\".\n", 
            last_error, szReadOnlyFile);
    }
    
    if (!SetFileAttributes(szReadOnlyFile, FILE_ATTRIBUTE_READONLY))
    {
		last_error = GetLastError();
		Trace("WriteFile: ERROR[%ld] -> Unable to make the file read-only.\n", last_error);
		do_cleanup();
        Fail("WriteFile: ERROR[%ld] -> Unable to make the file read-only.\n", last_error);
    }

    bRc = WriteFile(hFile, szStringTest, 20, &dwBytesWritten, NULL);
    if (bRc == TRUE)
    {	last_error = GetLastError();
		Trace("WriteFile: ERROR[%ld] -> Able to write to a read-only file.\n", last_error);
		do_cleanup();
        Fail("WriteFile: ERROR[%ld] -> Able to write to a read-only file.\n", last_error);
    }


    bRc = CloseHandle(hFile);
    if (bRc != TRUE)
    {   last_error = GetLastError();
		Trace("WriteFile: ERROR[%ld] -> Unable to close file \"%s\".\n", last_error, szReadOnlyFile);
		do_cleanup();
        Fail("WriteFile: ERROR -> Unable to close file \"%s\".\n", 
            szReadOnlyFile);
    }

	//To delete file need to make it normal
	if(!SetFileAttributesA(szReadOnlyFile,FILE_ATTRIBUTE_NORMAL))
	{
		last_error = GetLastError();
	    Fail("WriteFile: ERROR[%ld] -> Unable to make the file attribute NORMAL.\n", last_error);

	}
	do_cleanup();
    PAL_Terminate();
    return PASS;
}
