// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*=====================================================================

**

** Source:  test2.c

**

** Purpose:   Test the return value of GetLastError() after calling 

**            some file_io functions with an invalid path. 

**            

**            Functions covered by this test are: 

**            GetFileAttributesA, GetFileAttributesW,

**
**

**



**

**===================================================================*/



#include <palsuite.h>



PALTEST(file_io_errorpathnotfound_test2_paltest_errorpathnotfound_test2, "file_io/errorpathnotfound/test2/paltest_errorpathnotfound_test2")

{



    BOOL              testPass       = TRUE;

    BOOL              bRc            = TRUE;

    HANDLE            hFile; 

    DWORD              fileAttrib;



    const char* sBadFilePath = "bad/badPath.tmp";

    const char* sBadFileName = "badName.tmp";



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


    
    /*...................Test GetFileAttributesW.............................*/



    /* test with an invalid path */

    fileAttrib = GetFileAttributesW(wBadFilePath);

    if(fileAttrib == -1)

    {

        if(GetLastError()!= ERROR_PATH_NOT_FOUND)

        {

            Trace("GetFileAttributesW: calling GetLastError() after getting"

                " the attributes of a file with wrong path returned [%u]"

                " while it should return [%u]\n",

                GetLastError(), ERROR_PATH_NOT_FOUND);

            testPass = FALSE;

        }

    }

    else

    {

        Trace("GetFileAttributesW: managed to get the attrib of a file"

            " with wrong path\n");     

        testPass = FALSE;

    }



    /* test with invalid file name */

    fileAttrib = GetFileAttributesW(wBadFileName);

    if(fileAttrib == -1)

    { 

        if(GetLastError()!= ERROR_FILE_NOT_FOUND)

        {

            Trace("GetFileAttributesW: calling GetLastError() after getting"

                " the attributes of a file with wrong name returned [%u] "

                "while it should return [%u]\n"

                ,GetLastError(), ERROR_FILE_NOT_FOUND);

            testPass = FALSE;

        }

    }

    else

    {

        Trace("GetFileAttributesW: managed to get the attrib of a file"

            " with wrong name\n");     

        testPass = FALSE;

    }



    /*...................Test GetFileAttributesA.............................*/



    /* test with an invalid path */

    fileAttrib = GetFileAttributesA(sBadFilePath);

    if(fileAttrib == -1)

    {

        if(GetLastError()!= ERROR_PATH_NOT_FOUND)

        {

            Trace("GetFileAttributesA: calling GetLastError() after getting"

                " the attributes of a file with wrong path returned [%u] while"

                " it should return [%u]\n",

                GetLastError(), ERROR_PATH_NOT_FOUND);

            testPass = FALSE;

        }

    }

    else

    {

        Trace("GetFileAttributesA: managed to get the attrib of a file"

            " with wrong path\n");     

        testPass = FALSE;

    }



    /* test with invalid file name */

    fileAttrib = GetFileAttributesA(sBadFileName);

    if(fileAttrib == -1)

    { 

        if(GetLastError()!= ERROR_FILE_NOT_FOUND)

        {

            Trace("GetFileAttributesA: calling GetLastError() after getting "

                "the attributes of a file with wrong name returned [%u] "

                "while it should return [%u]\n"

                ,GetLastError(), ERROR_FILE_NOT_FOUND);

            testPass = FALSE;

        }



    }

    else

    {

        Trace("GetFileAttributesA: managed to get the attrib of a file with"

              " wrong name\n"); 

        testPass = FALSE;

    }


    if(! testPass)

    {

        Fail("");

    }

    PAL_Terminate();

    return PASS;

}



