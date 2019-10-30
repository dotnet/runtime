// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*=====================================================================

**

** Source:  test3.c

**

** Purpose:   Test the return value of GetLastError() after calling 

**            some file_io functions with an invalid path. 

**            

**            Functions covered by this test are: 

**            CreateDirectoryA, CreateDirectoryW, RemoveDirectoryW,

**
**



**

**===================================================================*/



#include <palsuite.h>



int __cdecl main(int argc, char *argv[])

{



    BOOL testPass = TRUE;

    BOOL bRc = TRUE;

    DWORD errCode;



    const char* sBadSubDirectory = "bad/badDir";



    /* Needed for RemoveDirectoryW */

    const WCHAR tempName[] = {'b','a','d','/',

        'b','a','d','D','i','r'};



    const WCHAR wBadSubDirectory[] = 

        {'w','b','a','d','/','b','a',

        'd','D','i','r','\0'};

 

    if (0 != PAL_Initialize(argc,argv))

    {

        return FAIL;

    }



    /* ---------------------CreateDirectoryA------------------------- */





    /* Testing of CreateDirectoryA */

    bRc = CreateDirectoryA(sBadSubDirectory,NULL);

    if(!bRc)

    {

        errCode = GetLastError();

        if(errCode != ERROR_PATH_NOT_FOUND)

        {

            Trace("CreateDirectoryA: calling GetLastError() after creating a "

                "directory with invalid path returned [%u] while it should "

                "return [%u]\n",errCode, ERROR_PATH_NOT_FOUND);

            testPass = FALSE;

        }

    }

    else

    {

        Trace("CreateDirectoryA: Created a directory with an invalid path.\n");

        bRc = RemoveDirectoryW(tempName);

        if(!bRc)

        {

            Trace("Failed to remove %s with error %u.\n",

                sBadSubDirectory,GetLastError());

        }

        testPass = FALSE;

    }



    /* ---------------------CreateDirectoryW------------------------- */



    /* Testing of CreateDirectoryW */

    bRc = CreateDirectoryW(wBadSubDirectory,NULL);

    if(!bRc)

    {

        errCode = GetLastError();

        if(errCode != ERROR_PATH_NOT_FOUND)

        {

            Trace("CreateDirectoryW: calling GetLastError() after creating a "

                "directory with invalid path returned [%u] while it should "

                "return [%u]\n",errCode, ERROR_PATH_NOT_FOUND);

            testPass = FALSE;

        }

    }

    else

    {

        Trace("CreateDirectoryW: Created a directory with an invalid path.\n");

        bRc = RemoveDirectoryW(wBadSubDirectory);

        if(!bRc)

        {

            Trace("Failed to remove %S with error %u.\n",

                wBadSubDirectory,GetLastError());

        }

        testPass = FALSE;

    }



    /* ---------------------RemoveDirectoryW------------------------ */



    /* Testing of RemoveDirectoryW */

    bRc = RemoveDirectoryW(wBadSubDirectory);

    if(!bRc)

    {

        errCode = GetLastError();

        if(errCode != ERROR_PATH_NOT_FOUND)

        {

            Trace("RemoveDirectoryW: calling GetLastError() after removing a "

                "sub directory with invalid path returned [%u] while it should "

                "return [%u]\n",errCode, ERROR_PATH_NOT_FOUND);

            testPass = FALSE;

        }

    }

    else

    {

        Trace("RemoveDirectoryW: Removed a directory that did not exist.\n");

    }

    int exitCode = testPass ? PASS : FAIL;
    PAL_TerminateEx(exitCode);
    return exitCode;

 








}

