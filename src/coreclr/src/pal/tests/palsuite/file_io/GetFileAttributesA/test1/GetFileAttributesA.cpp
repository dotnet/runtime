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

DWORD desiredAccessFile = GENERIC_READ | GENERIC_WRITE;
DWORD shareModeFile  =  FILE_SHARE_READ|FILE_SHARE_WRITE|FILE_SHARE_DELETE;
LPSECURITY_ATTRIBUTES lpAttrFile = NULL;
DWORD dwCreationDispFile  = CREATE_NEW;
DWORD dwFlagsAttribFile = FILE_ATTRIBUTE_NORMAL;
HANDLE hTemplateFile  = NULL;

int numFileTests = 6;
TestCaseFile gfaTestsFile[6]; /* GetFileAttributes tests list */

int numDirTests = 6;
TestCaseDir gfaTestsDir[6]; /* GetFileAttributes tests list */

BOOL CleanUpFiles()
{
    DWORD dwAtt;
    int i;
    BOOL result = TRUE;
    for (i = 0; i < numFileTests -1 ; i++ )
    {
        dwAtt = GetFileAttributesA(gfaTestsFile[i].name);
 
        if( dwAtt != INVALID_FILE_ATTRIBUTES )
        {
            //Trace("Files iteration %d\n", i);
            if(!SetFileAttributesA (gfaTestsFile[i].name, FILE_ATTRIBUTE_NORMAL))
            {
                result = FALSE;
                Trace("ERROR:%d: Error setting attributes [%s][%d]\n", GetLastError(), gfaTestsFile[i].name, FILE_ATTRIBUTE_NORMAL); 
            } 

            if(!DeleteFileA (gfaTestsFile[i].name))
            {
                result = FALSE;
                Trace("ERROR:%d: Error deleting file [%s][%d]\n", GetLastError(), gfaTestsFile[i].name, dwAtt);   
            }
            
        }
    }
//    Trace("Value of result is %d\n", result);
    return result;
}
BOOL SetUpFiles()
{
    int i = 0;
    BOOL result = TRUE;
    for (i = 0; i < numFileTests -1; i++ )
    {
        gfaTestsFile[i].hFile = CreateFile(gfaTestsFile[i].name,
                        desiredAccessFile,
                        shareModeFile,
                        lpAttrFile,
                        dwCreationDispFile,
                        dwFlagsAttribFile,
                        hTemplateFile);

        if( gfaTestsFile[i].hFile == NULL )
        {
            Fail("Error while creating files for iteration %d\n", i);
        }

        if(!SetFileAttributesA (gfaTestsFile[i].name, gfaTestsFile[i].expectedAttribs))
        {
            result = FALSE;
            Trace("ERROR:%d: Error setting attributes [%s][%d]\n", GetLastError(), gfaTestsFile[i].name, gfaTestsFile[i].expectedAttribs); 
        } 
    }

    return result;
}

BOOL CleanUpDirs()
{
    DWORD dwAtt;
    int i;
    BOOL result = TRUE;
    for (i = 0; i < numDirTests -1 ; i++ )
    {
        dwAtt = GetFileAttributesA(gfaTestsDir[i].name);
 
        if( dwAtt != INVALID_FILE_ATTRIBUTES )
        {
            
            if(!SetFileAttributesA (gfaTestsDir[i].name, FILE_ATTRIBUTE_DIRECTORY))
            {
                result = FALSE;
                Trace("ERROR:%d: Error setting attributes [%s][%d]\n", GetLastError(), gfaTestsDir[i].name, (FILE_ATTRIBUTE_NORMAL | FILE_ATTRIBUTE_DIRECTORY)); 
            } 

            if(!RemoveDirectoryA (gfaTestsDir[i].name))
            {
                result = FALSE;
                Trace("ERROR:%d: Error deleting file [%s][%d]\n", GetLastError(), gfaTestsDir[i].name, dwAtt);   
            }
            
        }
    }

    return result;
}

BOOL SetUpDirs()
{
    int i = 0;
    BOOL result = TRUE;
    DWORD ret = 0;
    for (i = 0; i < numDirTests - 1 ; i++ )
    {
        result = CreateDirectoryA(gfaTestsDir[i].name,
                         NULL);

        if(!result )
        {
            result = FALSE;
            Fail("Error while creating directory for iteration %d\n", i);
        }

        if(!SetFileAttributesA (gfaTestsDir[i].name, gfaTestsDir[i].expectedAttribs))
        {
            result = FALSE;
            Trace("ERROR:%d: Error setting attributes [%s][%d]\n", GetLastError(), gfaTestsDir[i].name, gfaTestsDir[i].expectedAttribs); 
        } 

        ret = GetFileAttributesA (gfaTestsDir[i].name);
        if(ret != gfaTestsDir[i].expectedAttribs)
        {
            result = FALSE;
            Trace("ERROR: Error setting attributes [%s][%d]\n", gfaTestsDir[i].name, gfaTestsDir[i].expectedAttribs); 
        } 
        //Trace("Setup Dir setting attr [%d], returned [%d]\n", gfaTestsDir[i].expectedAttribs, ret);

    }
    //Trace("Setup dirs returning %d\n", result);
    return result;
}
int __cdecl main(int argc, char **argv)
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
    gfaTestsDir[0].name    = NormalDirectoryName;
    gfaTestsDir[0].expectedAttribs = FILE_ATTRIBUTE_DIRECTORY;
    gfaTestsDir[0].isFile  = TYPE_DIR;
    
    gfaTestsDir[1].name    = ReadOnlyDirectoryName;
    gfaTestsDir[1].expectedAttribs = FILE_ATTRIBUTE_DIRECTORY | 
                          FILE_ATTRIBUTE_READONLY;
    gfaTestsDir[1].isFile  = TYPE_DIR;

    gfaTestsDir[2].name    = ReadWriteDirectoryName;
    gfaTestsDir[2].expectedAttribs = FILE_ATTRIBUTE_DIRECTORY;
    gfaTestsDir[2].isFile  = TYPE_DIR;

    gfaTestsDir[3].name    = HiddenDirectoryName;
    gfaTestsDir[3].expectedAttribs = FILE_ATTRIBUTE_DIRECTORY; //| 
                          //FILE_ATTRIBUTE_HIDDEN;
    gfaTestsDir[3].isFile  = TYPE_DIR;
    
    gfaTestsDir[4].name    = HiddenReadOnlyDirectoryName;
    gfaTestsDir[4].expectedAttribs = FILE_ATTRIBUTE_DIRECTORY | 
                          FILE_ATTRIBUTE_READONLY; //|
                          //FILE_ATTRIBUTE_HIDDEN;
    gfaTestsDir[4].isFile  = TYPE_DIR;

    gfaTestsDir[5].name    = NoDirectoryName;
    gfaTestsDir[5].expectedAttribs = INVALID_FILE_ATTRIBUTES;
    gfaTestsDir[5].isFile  = TYPE_DIR;

    /* Tests on file */
    gfaTestsFile[0].name    = NormalFileName;
    gfaTestsFile[0].expectedAttribs = FILE_ATTRIBUTE_NORMAL;
    gfaTestsFile[0].isFile  = TYPE_FILE;


    gfaTestsFile[1].name    = ReadOnlyFileName;
    gfaTestsFile[1].expectedAttribs = FILE_ATTRIBUTE_READONLY;
    gfaTestsFile[1].isFile  = TYPE_FILE;

    gfaTestsFile[2].name    = ReadWriteFileName;
    gfaTestsFile[2].expectedAttribs = FILE_ATTRIBUTE_NORMAL;
    gfaTestsFile[2].isFile  = TYPE_FILE;

    gfaTestsFile[3].name    = HiddenFileName;
    gfaTestsFile[3].expectedAttribs = FILE_ATTRIBUTE_NORMAL; //FILE_ATTRIBUTE_HIDDEN;
    gfaTestsFile[3].isFile  = TYPE_FILE;

    gfaTestsFile[4].name    = HiddenReadOnlyFileName;
    gfaTestsFile[4].expectedAttribs = FILE_ATTRIBUTE_READONLY; //|
                           //FILE_ATTRIBUTE_HIDDEN;
    gfaTestsFile[4].isFile  = TYPE_FILE;


    gfaTestsFile[5].name    = NotReallyAFileName;
    gfaTestsFile[5].expectedAttribs =  INVALID_FILE_ATTRIBUTES;
    gfaTestsFile[5].isFile  = TYPE_FILE;    

    /* Initialize PAL environment */
    if (0 != PAL_Initialize(argc,argv))
    {
        return FAIL;
    }

    if(!CleanUpFiles())
    {
        Fail("GetFileAttributesA: Pre-Clean Up Files Failed\n");
    }

    if(0 == SetUpFiles())
    {
        Fail("GetFileAttributesA: SetUp Files Failed\n");
    }

    if(!CleanUpDirs())
    {
        Fail("GetFileAttributesA: Pre-Clean Up Directories Failed\n");
    }

    if(!SetUpDirs())
    {
        Fail("GetFileAttributesA: SetUp Directories Failed\n");
    }

    /* 
     * Go through all the test cases above,
     * call GetFileAttributesA on the name and
     * make sure the return value is the one expected
     */
    for( i = 0; i < numFileTests; i++ )
    {
        result = GetFileAttributesA(gfaTestsFile[i].name);

        if( result != gfaTestsFile[i].expectedAttribs )
        {
            bFailed = TRUE;

            Trace("ERROR: GetFileAttributesA Test#%u on %s "
                  "returned %u instead of %u. \n",
                  i,
                  gfaTestsFile[i].name,
                  result,
                  gfaTestsFile[i].expectedAttribs);

        }
    }


    for( i = 0; i < numDirTests; i++ )
    {
        result = GetFileAttributesA(gfaTestsDir[i].name);

        if( result != gfaTestsDir[i].expectedAttribs )
        {
            bFailed = TRUE;

            Trace("ERROR: GetFileAttributesA on Directories Test#%u on %s "
                  "returned %u instead of %u. \n",
                  i,
                  gfaTestsDir[i].name,
                  result,
                  gfaTestsDir[i].expectedAttribs);

        }
    }

    if(!CleanUpFiles())
    {
        Fail("GetFileAttributesA: Post-Clean Up Files Failed\n");
    }

    if(!CleanUpDirs())
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
