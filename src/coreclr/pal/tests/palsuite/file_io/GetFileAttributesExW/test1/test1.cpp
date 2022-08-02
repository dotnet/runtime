// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*=====================================================================
**
** Source:  test1.c
**
** Purpose: Tests the PAL implementation of the GetFileAttributesExW function.
** Call the function on a normal directory and file and a read-only directory
** and file and a hidden file and directory.
** Ensure that the returned attributes and file sizes are correct.
**
**
**===================================================================*/

#define UNICODE
#include <palsuite.h>

typedef enum Item
{
    IS_DIR,
    IS_FILE
}ItemType;

/* This function takes a structure and checks that the information
   within the structure is correct.  The 'Attribs' are the expected
   file attributes, 'TheType' is IS_DIR or IS_FILE and the 'Name' is the
   name of the file/directory in question.
*/
void VerifyInfo(WIN32_FILE_ATTRIBUTE_DATA InfoStruct,
                DWORD Attribs, ItemType TheType, WCHAR* Name)
{
    HANDLE hFile;
    FILETIME CorrectCreation, CorrectAccess, CorrectModify;
    WCHAR CopyName[64];

    wcscpy(CopyName,Name);
    free(Name);

    /* Check to see that the file attributes were recorded */
    if(InfoStruct.dwFileAttributes != Attribs)
    {
        Fail("ERROR: The file attributes on the file/directory were "
             "recorded as being %d instead of %d.\n",
             InfoStruct.dwFileAttributes,
             Attribs);
    }

    /* Note: We can't open a handle to a directory in windows.  This
       block of tests will only be run on files.
    */
    if(TheType == IS_FILE)
    {

        /* Get a handle to the file */
        hFile = CreateFile(CopyName,
                           0,
                           0,
                           NULL,
                           OPEN_EXISTING,
                           FILE_ATTRIBUTE_NORMAL,
                           NULL);

        if (hFile == INVALID_HANDLE_VALUE)
        {
            Fail("ERROR: Could not open a handle to the file "
                 "'%S'.  GetLastError() returned %d.",CopyName,
                 GetLastError());
        }


        if(InfoStruct.nFileSizeLow != GetFileSize(hFile,NULL))
        {
            Fail("ERROR: The file size reported by GetFileAttributesEx "
                 "did not match the file size given by GetFileSize.\n");
        }

        if(CloseHandle(hFile) == 0)
        {
            Fail("ERROR: Failed to properly close the handle to the "
                 "file we're testing.  GetLastError() returned %d.\n",
                 GetLastError());

        }

    }


}

/* Given a file/directory name, the expected attribs and whether or not it
   is a file or directory, call GetFileAttributesEx and verify the
   results are correct.
*/

void RunTest_GetFileAttributesExW_test1(char* Name, DWORD Attribs, ItemType TheType )
{
    WCHAR* TheName;
    WIN32_FILE_ATTRIBUTE_DATA InfoStruct;
    DWORD TheResult;

    TheName = convert(Name);

    TheResult = GetFileAttributesEx(TheName,
                                    GetFileExInfoStandard,
                                    &InfoStruct);
    if(TheResult == 0)
    {
        free(TheName);
        Fail("ERROR: GetFileAttributesEx returned 0, indicating failure.  "
             "GetLastError returned %d.\n",GetLastError());
    }

    VerifyInfo(InfoStruct, Attribs, TheType, TheName);

}

PALTEST(file_io_GetFileAttributesExW_test1_paltest_getfileattributesexw_test1, "file_io/GetFileAttributesExW/test1/paltest_getfileattributesexw_test1")
{
    DWORD TheResult;
    WCHAR* FileName;
    WIN32_FILE_ATTRIBUTE_DATA InfoStruct;

    if (0 != PAL_Initialize(argc,argv))
    {
        return FAIL;
    }

    /* Test a Directory */
    RunTest_GetFileAttributesExW_test1("normal_test_directory", FILE_ATTRIBUTE_DIRECTORY, IS_DIR);


    /* Test a Normal File */

    RunTest_GetFileAttributesExW_test1("normal_test_file", FILE_ATTRIBUTE_NORMAL, IS_FILE);

    /* Test a Read-Only Directory */

    RunTest_GetFileAttributesExW_test1("ro_test_directory",
            FILE_ATTRIBUTE_READONLY|FILE_ATTRIBUTE_DIRECTORY, IS_DIR);

    /* Test a Read-Only File */

    RunTest_GetFileAttributesExW_test1("ro_test_file", FILE_ATTRIBUTE_READONLY, IS_FILE);

    /* Test a Hidden File */

    RunTest_GetFileAttributesExW_test1(".hidden_file", FILE_ATTRIBUTE_HIDDEN, IS_FILE);

    /* Test a Hidden Directory */

    RunTest_GetFileAttributesExW_test1(".hidden_directory",
            FILE_ATTRIBUTE_HIDDEN|FILE_ATTRIBUTE_DIRECTORY, IS_DIR);

    /* Test a Non-Existent File */

    FileName = convert("nonexistent_test_file");

    TheResult = GetFileAttributesEx(FileName,
                                    GetFileExInfoStandard,
                                    &InfoStruct);

    if(TheResult != 0)
    {
        free(FileName);
        Fail("ERROR: GetFileAttributesEx returned non-zero, indicating "
             "success when it should have failed.  It was called on a "
             "non-existent file.");
    }

    free(FileName);

    PAL_Terminate();
    return PASS;
}
