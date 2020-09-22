// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*=============================================================================
**
** Source:  GetFileAttributesA.c
**
** Purpose: Tests the PAL implementation of the GetFileAttributesA function by
**          checking the attributes of:
**          - a normal directory and file
**          - a read only directory and file
**          - a read write directory and file
**          - a hidden directory and file
**          - a read only hidden directory and file
**          - a directory and a file with no attributes
**          - an invalid file name
**
**
**===========================================================================*/
#include <palsuite.h>

const int TYPE_DIR = 0;
const int TYPE_FILE = 1;
/* Structure defining a test case */
typedef struct
{
    char *name;     /* name of the file/directory */
    DWORD expectedAttribs;  /* expected attributes */
    HANDLE hFile;  /* Handle to the file */
    int isFile;    /* is file (1) or dir (0) */
}TestCaseFile;

typedef struct
{
    char *name;     /* name of the file/directory */
    DWORD expectedAttribs;  /* expected attributes */
    HANDLE hFile;  /* Handle to the file */
    int isFile;    /* is file (1) or dir (0) */
}TestCaseDir;

DWORD desiredAccessFile_GetFileAttributesA_test1 = GENERIC_READ | GENERIC_WRITE;
DWORD shareModeFile_GetFileAttributesA_test1  =  FILE_SHARE_READ|FILE_SHARE_WRITE|FILE_SHARE_DELETE;
LPSECURITY_ATTRIBUTES lpAttrFile_GetFileAttributesA_test1 = NULL;
DWORD dwCreationDispFile_GetFileAttributesA_test1  = CREATE_NEW;
DWORD dwFlagsAttribFile_GetFileAttributesA_test1 = FILE_ATTRIBUTE_NORMAL;
HANDLE hTemplateFile_GetFileAttributesA_test1  = NULL;

int numFileTests_A = 6;
TestCaseFile gfaTestsFile_A[6]; /* GetFileAttributes tests list */

int numDirTests_A = 6;
TestCaseDir gfaTestsDir_A[6]; /* GetFileAttributes tests list */

BOOL CleanUpFiles_GetFileAttributesA_test1()
{
    DWORD dwAtt;
    int i;
    BOOL result = TRUE;
    for (i = 0; i < numFileTests_A -1 ; i++ )
    {
        dwAtt = GetFileAttributesA(gfaTestsFile_A[i].name);

        if( dwAtt != INVALID_FILE_ATTRIBUTES )
        {
            //Trace("Files iteration %d\n", i);
            if(!SetFileAttributesA (gfaTestsFile_A[i].name, FILE_ATTRIBUTE_NORMAL))
            {
                result = FALSE;
                Trace("ERROR:%d: Error setting attributes [%s][%d]\n", GetLastError(), gfaTestsFile_A[i].name, FILE_ATTRIBUTE_NORMAL);
            }

            if(!DeleteFileA (gfaTestsFile_A[i].name))
            {
                result = FALSE;
                Trace("ERROR:%d: Error deleting file [%s][%d]\n", GetLastError(), gfaTestsFile_A[i].name, dwAtt);
            }

        }
    }
//    Trace("Value of result is %d\n", result);
    return result;
}
BOOL SetUpFiles_GetFileAttributesA_test1()
{
    int i = 0;
    BOOL result = TRUE;
    for (i = 0; i < numFileTests_A -1; i++ )
    {
        gfaTestsFile_A[i].hFile = CreateFile(gfaTestsFile_A[i].name,
                        desiredAccessFile_GetFileAttributesA_test1,
                        shareModeFile_GetFileAttributesA_test1,
                        lpAttrFile_GetFileAttributesA_test1,
                        dwCreationDispFile_GetFileAttributesA_test1,
                        dwFlagsAttribFile_GetFileAttributesA_test1,
                        hTemplateFile_GetFileAttributesA_test1);

        if( gfaTestsFile_A[i].hFile == NULL )
        {
            Fail("Error while creating files for iteration %d\n", i);
        }

        if(!SetFileAttributesA (gfaTestsFile_A[i].name, gfaTestsFile_A[i].expectedAttribs))
        {
            result = FALSE;
            Trace("ERROR:%d: Error setting attributes [%s][%d]\n", GetLastError(), gfaTestsFile_A[i].name, gfaTestsFile_A[i].expectedAttribs);
        }
    }

    return result;
}

BOOL CleanUpDirs_GetFileAttributesA_test1()
{
    DWORD dwAtt;
    int i;
    BOOL result = TRUE;
    for (i = 0; i < numDirTests_A -1 ; i++ )
    {
        dwAtt = GetFileAttributesA(gfaTestsDir_A[i].name);

        if( dwAtt != INVALID_FILE_ATTRIBUTES )
        {

            if(!SetFileAttributesA (gfaTestsDir_A[i].name, FILE_ATTRIBUTE_DIRECTORY))
            {
                result = FALSE;
                Trace("ERROR:%d: Error setting attributes [%s][%d]\n", GetLastError(), gfaTestsDir_A[i].name, (FILE_ATTRIBUTE_NORMAL | FILE_ATTRIBUTE_DIRECTORY));
            }

            LPWSTR nameW = convert(gfaTestsDir_A[i].name);
            if(!RemoveDirectoryW (nameW))
            {
                result = FALSE;
                Trace("ERROR:%d: Error deleting file [%s][%d]\n", GetLastError(), gfaTestsDir_A[i].name, dwAtt);
            }

            free(nameW);
        }
    }

    return result;
}

BOOL SetUpDirs_GetFileAttributesA_test1()
{
    int i = 0;
    BOOL result = TRUE;
    DWORD ret = 0;
    for (i = 0; i < numDirTests_A - 1 ; i++ )
    {
        result = CreateDirectoryA(gfaTestsDir_A[i].name,
                         NULL);

        if(!result )
        {
            result = FALSE;
            Fail("Error while creating directory for iteration %d\n", i);
        }

        if(!SetFileAttributesA (gfaTestsDir_A[i].name, gfaTestsDir_A[i].expectedAttribs))
        {
            result = FALSE;
            Trace("ERROR:%d: Error setting attributes [%s][%d]\n", GetLastError(), gfaTestsDir_A[i].name, gfaTestsDir_A[i].expectedAttribs);
        }

        ret = GetFileAttributesA (gfaTestsDir_A[i].name);
        if(ret != gfaTestsDir_A[i].expectedAttribs)
        {
            result = FALSE;
            Trace("ERROR: Error setting attributes [%s][%d]\n", gfaTestsDir_A[i].name, gfaTestsDir_A[i].expectedAttribs);
        }
        //Trace("Setup Dir setting attr [%d], returned [%d]\n", gfaTestsDir_A[i].expectedAttribs, ret);

    }
    //Trace("Setup dirs returning %d\n", result);
    return result;
}
PALTEST(file_io_GetFileAttributesA_test1_paltest_getfileattributesa_test1, "file_io/GetFileAttributesA/test1/paltest_getfileattributesa_test1")
{
    int i;
    BOOL  bFailed = FALSE;
    DWORD result;

    char * NormalDirectoryName          = "normal_test_directory";
    char * ReadOnlyDirectoryName        = "ro_test_directory";
    char * ReadWriteDirectoryName       = "rw_directory";
    char * HiddenDirectoryName          = ".hidden_directory";
    char * HiddenReadOnlyDirectoryName  = ".hidden_ro_directory";
    char * NoDirectoryName              = "no_directory";

    char * NormalFileName               = "normal_test_file";
    char * ReadOnlyFileName             = "ro_test_file";
    char * ReadWriteFileName            = "rw_file";
    char * HiddenFileName               = ".hidden_file";
    char * HiddenReadOnlyFileName       = ".hidden_ro_file";
    char * NotReallyAFileName           = "not_really_a_file";

    /* Tests on directory */
    gfaTestsDir_A[0].name    = NormalDirectoryName;
    gfaTestsDir_A[0].expectedAttribs = FILE_ATTRIBUTE_DIRECTORY;
    gfaTestsDir_A[0].isFile  = TYPE_DIR;

    gfaTestsDir_A[1].name    = ReadOnlyDirectoryName;
    gfaTestsDir_A[1].expectedAttribs = FILE_ATTRIBUTE_DIRECTORY |
                          FILE_ATTRIBUTE_READONLY;
    gfaTestsDir_A[1].isFile  = TYPE_DIR;

    gfaTestsDir_A[2].name    = ReadWriteDirectoryName;
    gfaTestsDir_A[2].expectedAttribs = FILE_ATTRIBUTE_DIRECTORY;
    gfaTestsDir_A[2].isFile  = TYPE_DIR;

    gfaTestsDir_A[3].name    = HiddenDirectoryName;
    gfaTestsDir_A[3].expectedAttribs = FILE_ATTRIBUTE_DIRECTORY; //|
                          //FILE_ATTRIBUTE_HIDDEN;
    gfaTestsDir_A[3].isFile  = TYPE_DIR;

    gfaTestsDir_A[4].name    = HiddenReadOnlyDirectoryName;
    gfaTestsDir_A[4].expectedAttribs = FILE_ATTRIBUTE_DIRECTORY |
                          FILE_ATTRIBUTE_READONLY; //|
                          //FILE_ATTRIBUTE_HIDDEN;
    gfaTestsDir_A[4].isFile  = TYPE_DIR;

    gfaTestsDir_A[5].name    = NoDirectoryName;
    gfaTestsDir_A[5].expectedAttribs = INVALID_FILE_ATTRIBUTES;
    gfaTestsDir_A[5].isFile  = TYPE_DIR;

    /* Tests on file */
    gfaTestsFile_A[0].name    = NormalFileName;
    gfaTestsFile_A[0].expectedAttribs = FILE_ATTRIBUTE_NORMAL;
    gfaTestsFile_A[0].isFile  = TYPE_FILE;


    gfaTestsFile_A[1].name    = ReadOnlyFileName;
    gfaTestsFile_A[1].expectedAttribs = FILE_ATTRIBUTE_READONLY;
    gfaTestsFile_A[1].isFile  = TYPE_FILE;

    gfaTestsFile_A[2].name    = ReadWriteFileName;
    gfaTestsFile_A[2].expectedAttribs = FILE_ATTRIBUTE_NORMAL;
    gfaTestsFile_A[2].isFile  = TYPE_FILE;

    gfaTestsFile_A[3].name    = HiddenFileName;
    gfaTestsFile_A[3].expectedAttribs = FILE_ATTRIBUTE_NORMAL; //FILE_ATTRIBUTE_HIDDEN;
    gfaTestsFile_A[3].isFile  = TYPE_FILE;

    gfaTestsFile_A[4].name    = HiddenReadOnlyFileName;
    gfaTestsFile_A[4].expectedAttribs = FILE_ATTRIBUTE_READONLY; //|
                           //FILE_ATTRIBUTE_HIDDEN;
    gfaTestsFile_A[4].isFile  = TYPE_FILE;


    gfaTestsFile_A[5].name    = NotReallyAFileName;
    gfaTestsFile_A[5].expectedAttribs =  INVALID_FILE_ATTRIBUTES;
    gfaTestsFile_A[5].isFile  = TYPE_FILE;

    /* Initialize PAL environment */
    if (0 != PAL_Initialize(argc,argv))
    {
        return FAIL;
    }

    if(!CleanUpFiles_GetFileAttributesA_test1())
    {
        Fail("GetFileAttributesA: Pre-Clean Up Files Failed\n");
    }

    if(0 == SetUpFiles_GetFileAttributesA_test1())
    {
        Fail("GetFileAttributesA: SetUp Files Failed\n");
    }

    if(!CleanUpDirs_GetFileAttributesA_test1())
    {
        Fail("GetFileAttributesA: Pre-Clean Up Directories Failed\n");
    }

    if(!SetUpDirs_GetFileAttributesA_test1())
    {
        Fail("GetFileAttributesA: SetUp Directories Failed\n");
    }

    /*
     * Go through all the test cases above,
     * call GetFileAttributesA on the name and
     * make sure the return value is the one expected
     */
    for( i = 0; i < numFileTests_A; i++ )
    {
        result = GetFileAttributesA(gfaTestsFile_A[i].name);

        if( result != gfaTestsFile_A[i].expectedAttribs )
        {
            bFailed = TRUE;

            Trace("ERROR: GetFileAttributesA Test#%u on %s "
                  "returned %u instead of %u. \n",
                  i,
                  gfaTestsFile_A[i].name,
                  result,
                  gfaTestsFile_A[i].expectedAttribs);

        }
    }


    for( i = 0; i < numDirTests_A; i++ )
    {
        result = GetFileAttributesA(gfaTestsDir_A[i].name);

        if( result != gfaTestsDir_A[i].expectedAttribs )
        {
            bFailed = TRUE;

            Trace("ERROR: GetFileAttributesA on Directories Test#%u on %s "
                  "returned %u instead of %u. \n",
                  i,
                  gfaTestsDir_A[i].name,
                  result,
                  gfaTestsDir_A[i].expectedAttribs);

        }
    }

    if(!CleanUpFiles_GetFileAttributesA_test1())
    {
        Fail("GetFileAttributesA: Post-Clean Up Files Failed\n");
    }

    if(!CleanUpDirs_GetFileAttributesA_test1())
    {
        Fail("GetFileAttributesA: Post-Clean Up Directories Failed\n");
    }

    /* If any errors, just call Fail() */
    if( bFailed )
    {
        Fail("");
    }

    PAL_Terminate();
    return PASS;
}
