// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*=====================================================================

**

** Source:  test2.c

**

** Purpose:   Test the return value of GetLastError() after calling 

**            some file_io functions with an invalid path. 

**            

**            Functions covered by this test are: 

**            FindFirstFileA, FindFirstFileW,

**            GetFileAttributesA, GetFileAttributesW,

**            SetFileAttributesA, SetFileAttributesW.

**
**

**



**

**===================================================================*/



#include <palsuite.h>



int __cdecl main(int argc, char *argv[])

{



    BOOL              testPass       = TRUE;

    BOOL              bRc            = TRUE;

    HANDLE            hFile; 

    WIN32_FIND_DATA   findFileData;

    WIN32_FIND_DATAW  wFindFileData;

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



    /*............. Test FindFirstFileA..................................*/



    /* test with an invalid file name */

    hFile = FindFirstFileA(sBadFileName,&findFileData );                    



    if (hFile == INVALID_HANDLE_VALUE) 

    { 

        if(GetLastError() != ERROR_FILE_NOT_FOUND)

        {

            Trace("FindFirstFileA: calling GetLastError() returned [%u] "

                "while it should return [%u] for a bad File Name\n",

                GetLastError(),ERROR_FILE_NOT_FOUND);   

            testPass = FALSE;

        }   



    } 

    else

    {

        Trace("FindFirstFileA: managed to find a file with an incorrect "

            "filename\n");   

        testPass = FALSE;



        if(!FindClose(hFile))

        {

            Trace("FindFirstFileA: Call to FindClose failed with ErrorCode"

                " [%u]\n", GetLastError());



        }



    }



    /* test with an invalid path */

    hFile = FindFirstFileA(sBadFilePath,&findFileData); 



    if (hFile == INVALID_HANDLE_VALUE) 

    { 

        if(GetLastError() != ERROR_PATH_NOT_FOUND)

        {

            Trace("FindFirstFileA: calling GetLastError() returned [%u] "

                "while it should return [%u] for a bad file path name\n",

                GetLastError(), ERROR_PATH_NOT_FOUND);   

            testPass = FALSE;

        }   



    } 

    else

    {

        Trace("FindFirstFileA: managed to find a file with an incorrect"

            " filename\n");   

        testPass = FALSE;

        /*this should not happen*/

        if(!FindClose(hFile))

        {

            Trace("FindFirstFileA: Call to FindClose Failed with ErrorCode"

                " [%u]\n", GetLastError());



        }



    }







    /*............. Test FindFirstFileW..................................*/



    /* test with an invalid file name */

    hFile = FindFirstFileW(wBadFileName,&wFindFileData );                    



    if (hFile == INVALID_HANDLE_VALUE) 

    { 

        if(GetLastError() != ERROR_FILE_NOT_FOUND)

        {

            Trace("FindFirstFileW: calling GetLastError() returned [%u] "

                "while it should return [%u] for a bad File Name\n",

                GetLastError(),ERROR_FILE_NOT_FOUND);   

            testPass = FALSE;

        }   



    } 

    else

    {

        Trace("FindFirstFileW: managed to find a file with an incorrect "

            "filename\n");   

        testPass = FALSE;



        if(!FindClose(hFile))

        {

            Trace("FindFirstFileW: Call to FindClose failed with ErrorCode"

                " [%u]\n", GetLastError());



        }



    }



    /* test with an invalid path */

    hFile = FindFirstFileW(wBadFilePath,&wFindFileData); 



    if (hFile == INVALID_HANDLE_VALUE) 

    { 

        if(GetLastError() != ERROR_PATH_NOT_FOUND)

        {

            Trace("FindFirstFileW: calling GetLastError() returned [%u] "

                "while it should return [%u] for a bad file path name\n",

                GetLastError(), ERROR_PATH_NOT_FOUND);   

            testPass = FALSE;

        }   



    } 

    else

    {

        Trace("FindFirstFileW: managed to find a file with an incorrect "

            "filename\n");   

        testPass = FALSE;

        /*this should not happen*/

        if(!FindClose(hFile))

        {

            Trace("FindFirstFileW: Call to FindClose Failed with ErrorCode "

                "[%u]\n", GetLastError());



        }



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









    /*...................Test SetFileAttributesW.............................*/



    /* test with an invalid path */

    bRc = SetFileAttributesW(wBadFilePath,FILE_ATTRIBUTE_NORMAL);

    if(!bRc)

    {

        if(GetLastError()!= ERROR_PATH_NOT_FOUND)

        {

            Trace("SetFileAttributesW: calling GetLastError() after setting"

                " the attributes of a file with wrong path returned [%u] "

                "while it should return [%u]\n",

                GetLastError(), ERROR_PATH_NOT_FOUND);

            testPass = FALSE;

        }

    }

    else

    {

        Trace("SetFileAttributesW: managed to get the attrib of a file"

            " with wrong path\n");     

        testPass = FALSE;

    }



    /* test with invalid file name */

    bRc = SetFileAttributesW(wBadFileName,FILE_ATTRIBUTE_NORMAL);

    if(!bRc)

    { 

        if(GetLastError()!= ERROR_FILE_NOT_FOUND)

        {

            Trace("SetFileAttributesW: calling GetLastError() after setting"

                " the attributes of a file with wrong name returned [%u]"

                " while it should return [%u]\n",

                GetLastError(), ERROR_FILE_NOT_FOUND);

            testPass = FALSE;

        }



    }

    else

    {

        Trace("SetFileAttributesW: managed to get the attrib of a file with"

            " wrong name\n");     

        testPass = FALSE;

    }





    /*...................Test SetFileAttributesA.............................*/



    /* test with an invalid path */

    bRc = SetFileAttributesA(sBadFilePath,FILE_ATTRIBUTE_NORMAL);

    if(!bRc)

    {

        if(GetLastError()!= ERROR_PATH_NOT_FOUND)

        {

            Trace("SetFileAttributesA: calling GetLastError() after setting "

                "the attributes of a file with wrong path returned [%u] "

                "while it should return [%u]\n"

                ,GetLastError(), ERROR_PATH_NOT_FOUND);

            testPass = FALSE;

        }

    }

    else

    {

        Trace("SetFileAttributesA: managed to get the attrib of a file "

            "with wrong path\n");     

        testPass = FALSE;

    }



    /* test with invalid file name */

    bRc = SetFileAttributesA(sBadFileName,FILE_ATTRIBUTE_NORMAL);

    if(!bRc)

    { 

        if(GetLastError()!= ERROR_FILE_NOT_FOUND)

        {

            Trace("SetFileAttributesA: calling GetLastError() after setting"

                " the attributes of a file with wrong name returned [%u]"

                " while it should return [%u]\n",

                GetLastError(), ERROR_FILE_NOT_FOUND);

            testPass = FALSE;

        }



    }

    else

    {

        Trace("SetFileAttributesA: managed to get the attrib of a file with "

            "wrong name\n");     

        testPass = FALSE;

    }





    if(! testPass)

    {

        Fail("");

    }

    PAL_Terminate();

    return PASS;

}



