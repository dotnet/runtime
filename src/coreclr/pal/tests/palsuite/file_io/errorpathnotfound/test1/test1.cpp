// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*=====================================================================

**

** Source:  test1.c

**

** Purpose:   Test the return value of GetLastError() after calling 

**            some file_io functions with an invalid path. 

**            

**            Functions covered by this test are: 

**            CopyFileA, CopyFileW, CreateFileA,CreateFileW,

**            DeleteFileA and DeleteFileW.

**
**



**

**===================================================================*/



#include <palsuite.h>



PALTEST(file_io_errorpathnotfound_test1_paltest_errorpathnotfound_test1, "file_io/errorpathnotfound/test1/paltest_errorpathnotfound_test1")

{



    BOOL testPass = TRUE;

    BOOL bRc = TRUE;

    HANDLE hFile; 



    const char* sBadFilePath = "bad/badPath.tmp";

    const char* sBadFileName = "badName.tmp";

    const char* sDest = "dest.tmp";

    const WCHAR wBadFilePath[] = 

        {'w','b','a','d','/','b','a',

        'd','.','t','m','p','\0'};

    const WCHAR wBadFileName[] = 

        {'w','B','a','d','.','t','m','p','\0'};

    const WCHAR wDest[] = 

        {'w','d','e','s','t','.','t','m','p','\0'};





    if (0 != PAL_Initialize(argc,argv))

    {

        return FAIL;

    }



    /*...................Test CopyFileW.............................*/



    /* test with an invalid path */

    bRc = CopyFileW(wBadFilePath,wDest,TRUE);

    if(!bRc)

    {

        if(GetLastError()!= ERROR_PATH_NOT_FOUND)

        {

            Trace("CopyFileW: calling GetLastError() after copying a file"

                " with wrong path returned [%u] while it should return [%u]\n"

                ,GetLastError(), ERROR_PATH_NOT_FOUND);

            testPass = FALSE;

        }

    }

    else

    {

        testPass = FALSE; 

    }



    /* test with invalid file name */

    bRc = CopyFileW(wBadFileName,wDest,TRUE);

    if(!bRc)

    { 

        if(GetLastError()!= ERROR_FILE_NOT_FOUND)

        {

            Trace("CopyFileW: calling GetLastError() after copying a file"

                " with wrong name returned [%u] while it should return [%u]\n"

                ,GetLastError(), ERROR_FILE_NOT_FOUND);

            testPass = FALSE;

        }



    }

    else

    {

        Trace("CopyFileW: managed to copy a file with wrong name\n");     

        testPass = FALSE;

    }



  



    /*..................CopyFileA...................................*/



    /* test with an invalid path */

    bRc = CopyFileA(sBadFilePath,sDest,TRUE);

    if(! bRc)

    {

        if(GetLastError()!= ERROR_PATH_NOT_FOUND)

        {

            Trace("CopyFileA: calling GetLastError() after copying a file"

                " with wrong path returned [%u] while it should return [%u]\n"

                ,GetLastError(), ERROR_PATH_NOT_FOUND);

            testPass = FALSE;

        }

    }

    else

    {

        Trace("CopyFileA: managed to copy a file with wrong path\n");     

        testPass = FALSE;

    }



    /* test with an invalid file name */

    bRc = CopyFileA(sBadFileName,sDest,TRUE);

    if(! bRc)

    { 

        if(GetLastError()!= ERROR_FILE_NOT_FOUND)

        {

            Trace("CopyFileA: calling GetLastError() after copying a file"

                " with wrong name returned [%u] while it should return [%u]\n"

                ,GetLastError(), ERROR_FILE_NOT_FOUND);

            testPass = FALSE;

        }

    }

    else

    {

        Trace("CopyFileA: managed to copy a file with wrong name\n"); 

        testPass = FALSE;

    }



    



    /*............. Test CreateFileA..................................*/



    /* test with an invalid file name */

    hFile = CreateFileA(sBadFileName,          

        GENERIC_READ,              /* open for reading */

        FILE_SHARE_READ,           /* share for reading */

        NULL,                      /* no security */

        OPEN_EXISTING,             /* existing file only */

        FILE_ATTRIBUTE_NORMAL,     /* normal file */ 

        NULL);                     /* no attr. template */



    if (hFile == INVALID_HANDLE_VALUE) 

    { 

        if(GetLastError() != ERROR_FILE_NOT_FOUND)

        {

            Trace("CreateFileA: calling GetLastError() returned [%u] "

                "while it should return [%u] for a bad File Name\n",

                GetLastError(),ERROR_FILE_NOT_FOUND);   

            testPass = FALSE;

        }   



    } 

    else

    {

        Trace("CreateFileA: managed to create a file with an incorrect "

              "filename\n");   

        testPass = FALSE;



        if(!CloseHandle(hFile))

        {

            Trace("CreateFileA: Call to CloseHandle failed with ErrorCode "

                "[%u]\n", GetLastError());



        }

        if(!remove(sBadFileName))

        {

            Trace("CreateFileA: Call to remove failed with ErrorCode "

                "[%u]\n", errno);

        }

    }



    /* test with an invalid path */

    hFile = CreateFileA(sBadFilePath,          

        GENERIC_READ,              /* open for reading */

        FILE_SHARE_READ,           /* share for reading */

        NULL,                      /* no security */

        OPEN_EXISTING,             /* existing file only */

        FILE_ATTRIBUTE_NORMAL,     /* normal file */ 

        NULL);                     /* no attr. template */



    if (hFile == INVALID_HANDLE_VALUE) 

    { 

        if(GetLastError() != ERROR_PATH_NOT_FOUND)

        {

            Trace("CreateFileA: calling GetLastError() returned [%u] "

                "while it should return [%u] for a bad file path name\n",

                GetLastError(), ERROR_PATH_NOT_FOUND);   

            testPass = FALSE;

        }   



    } 

    else

    {

        Trace("CreateFileA: managed to create a file with an incorrect "

              "filename\n");   

        testPass = FALSE;

        /*this should not happen*/

        if(!CloseHandle(hFile))

        {

            Trace("CreateFileA: Call to CloseHandle Failed with ErrorCode "

                "[%u]\n", GetLastError());



        }

        if(!remove(sBadFilePath))

        {

            Trace("CreateFileA: Call to remove Failed with ErrorCode "

                "[%u]\n", errno);

        }

    }





    



    /*............. Test CreateFileW..................................*/



    /* test with an invalid file name */

    hFile = CreateFileW(wBadFileName,          

        GENERIC_READ,              /* open for reading */

        FILE_SHARE_READ,           /* share for reading */

        NULL,                      /* no security */

        OPEN_EXISTING,             /* existing file only */

        FILE_ATTRIBUTE_NORMAL,     /* normal file */ 

        NULL);                     /* no attr. template */



    if (hFile == INVALID_HANDLE_VALUE) 

    { 

        if(GetLastError() != ERROR_FILE_NOT_FOUND)

        {

            Trace("CreateFileW: calling GetLastError() returned [%u] "

                "while it should return [%u] for a bad filename\n",

                GetLastError(), ERROR_FILE_NOT_FOUND);   

            testPass = FALSE;

        }   



    } 

    else

    {

        Trace("CreateFileW: managed to create a file with an incorrect "

              "filename\n");   

        testPass = FALSE;



        if(!CloseHandle(hFile))

        {

            Trace("CreateFileW: Call to CloseHandle Failed with ErrorCode "

                "[%u]\n", GetLastError());



        }



        if(!DeleteFileW(wBadFileName))

        {

            Trace("CreateFileW: Call to DeleteFile Failed with ErrorCode "

                "[%u]\n", GetLastError());

        }

    }







    /* test with an invalid path */

    hFile = CreateFileW(wBadFilePath,          

        GENERIC_READ,              /* open for reading */

        FILE_SHARE_READ,           /* share for reading */

        NULL,                      /* no security */

        OPEN_EXISTING,             /* existing file only */

        FILE_ATTRIBUTE_NORMAL,     /* normal file */ 

        NULL);                     /* no attr. template */



    if (hFile == INVALID_HANDLE_VALUE) 

    { 



        if(GetLastError() != ERROR_PATH_NOT_FOUND)

        {

            Trace("CreateFileW: calling GetLastError() returned [%u] "

                "while it should return [%u] for a bad file path \n",

                GetLastError(), ERROR_FILE_NOT_FOUND);   

            testPass = FALSE;

        }   



    } 

    else

    {

        Trace("CreateFileW: managed to create a file with an incorrect "

              "filename\n");   

        testPass = FALSE;



        if(!CloseHandle(hFile))

        {

            Trace("CreateFileW: Call to CloseHandle Failed with ErrorCode "

                "[%u]\n", GetLastError());



        }

        if(!DeleteFileW(wBadFilePath))

        {

            Trace("CreateFileW: Call to DeleteFile Failed with ErrorCode "

                "[%u]\n", GetLastError());

        }

    }





    

    /* .............  DeleteFileW..................................*/



    /* test with an invalid path */

    if(DeleteFileW(wBadFilePath))

    {

        Trace("DeleteFileW: Call to DeleteFileW to delete a file"

            " that does not exist succeeded\n");

        testPass = FALSE;



    }

    else

    {

        if(GetLastError() != ERROR_PATH_NOT_FOUND)

        {

            Trace("DeleteFileW: Call GetLastError()returned "

                "[%u] while it should return ERROR_PATH_NOT_FOUND [%u]\n",

                GetLastError(),ERROR_FILE_NOT_FOUND);

            testPass = FALSE;



        }



    }



    /* test with an invalid file name */

    if(DeleteFileW(wBadFileName))

    {

        Trace("DeleteFileW: Call to DeleteFileW to delete a file"

            " that does not exist succeeded\n");

        testPass = FALSE;



    }

    else

    {

        if(GetLastError() != ERROR_FILE_NOT_FOUND)

        {

            Trace("DeleteFileW: Call GetLastError()returned [%u]"

                " while it should return ERROR_FILE_NOT_FOUND [%u]\n",

                GetLastError(),ERROR_FILE_NOT_FOUND);

            testPass = FALSE;



        }



    }





    /* .............  DeleteFileA..................................*/



    /* test with an invalid path */

    if(DeleteFileA(sBadFilePath))

    {

        Trace("DeleteFileA: Call to DeleteFileA to delete a file"

            " that does not exist succeeded\n");

        testPass = FALSE;



    }

    else

    {

        if(GetLastError() != ERROR_PATH_NOT_FOUND)

        {

            Trace("DeleteFileA: Call GetLastError() returned [%u]"

                " while it should return ERROR_PATH_NOT_FOUND [%u]\n",

                GetLastError(),ERROR_FILE_NOT_FOUND);

            testPass = FALSE;



        }



    }



    /* test with an invalid file name */

    if(DeleteFileA(sBadFileName))

    {

        Trace("DeleteFileA: Call to DeleteFileA to delete a file"

            " that does not exist succeeded\n");

        testPass = FALSE;



    }

    else

    {

        if(GetLastError() != ERROR_FILE_NOT_FOUND)

        {

            Trace("DeleteFileA: Call GetLastError() returned [%u]"

                " while it should return ERROR_FILE_NOT_FOUND [%u]\n",

                GetLastError(),ERROR_FILE_NOT_FOUND);

            testPass = FALSE;



        }



    }



  



    if(! testPass)

    {

        Fail("");

    }

    PAL_Terminate();

    return PASS;

}



