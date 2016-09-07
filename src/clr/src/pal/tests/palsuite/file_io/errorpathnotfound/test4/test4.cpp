// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*=====================================================================

**

** Source:  test4.c

**

** Purpose:   Test the return value of GetLastError() after calling 

**            some file_io functions with an invalid path. 

**            

**            Functions covered by this test are: 

**            GetDiskFreeSpaceW, GetTempFileNameA

**            and GetTempFileNameW

**
**

**



**

**===================================================================*/



#include <palsuite.h>



int __cdecl main(int argc, char *argv[])

{



    BOOL  testPass       = TRUE;

    BOOL  bRc            = TRUE;

    DWORD lastErr=-50;

    DWORD dwSectorsPerCluster_02;  /* sectors per cluster */

    DWORD dwBytesPerSector_02;     /* bytes per sector */

    DWORD dwNumberOfFreeClusters;  /* free clusters */

    DWORD dwTotalNumberOfClusters; /* total clusters */



    UINT        uiError = 0;

    char        szReturnedName[256];

    const       UINT uUnique = 0;

    const char* sDot = {"tmpr"};

    const char* sPrefix = {"cfr"};



    WCHAR        wzReturnedName[256];

    const WCHAR wDot[] = {'t','m','p','r','\0'};

    const WCHAR wPrefix[] = {'c','f','r','\0'};





    const WCHAR wBadFilePath[] = 

    {'w','b','a','d','/','b','a',

    'd','.','t','m','p','\0'};

    const WCHAR wBadFileName[] = 

    {'w','B','a','d','.','t','m','p','\0'};







    if (0 != PAL_Initialize(argc,argv))

    {

        return FAIL;

    }



    /* test .................. GetDiskFreeSpaceW .................. */



    /* test with invalid file name */

    bRc = GetDiskFreeSpaceW(wBadFileName,

        &dwSectorsPerCluster_02,   

        &dwBytesPerSector_02,      

        &dwNumberOfFreeClusters,

        &dwTotalNumberOfClusters);

    if (bRc != TRUE)



    {

        lastErr=GetLastError();



        if(lastErr != ERROR_FILE_NOT_FOUND)

        {

            Trace("GetDiskFreeSpaceW: calling GetLastError() returned [%u] "

                "while it should return [%u] for a bad File Name\n",

                lastErr,ERROR_FILE_NOT_FOUND);   

            testPass = FALSE;

        }   

    }

    else

    {

        Trace("GetDiskFreeSpaceW: GetDiskFreeSpaceW succeeded when given "

            "a bad fileName\n");     

        testPass = FALSE;



    }





    /* test with invalid path name */

    bRc = GetDiskFreeSpaceW(wBadFilePath,

        &dwSectorsPerCluster_02,   

        &dwBytesPerSector_02,      

        &dwNumberOfFreeClusters,

        &dwTotalNumberOfClusters);

    if (bRc != TRUE)



    {

        lastErr=GetLastError();

        if(lastErr != ERROR_PATH_NOT_FOUND)

        {

            Trace("GetDiskFreeSpaceW: calling GetLastError() returned [%u] "

                "while it should return [%u] for a bad File Name\n",

                lastErr,ERROR_PATH_NOT_FOUND);   

            testPass = FALSE;

        }   

    }

    else

    {

        Trace("GetDiskFreeSpaceW: GetDiskFreeSpaceW succeeded when given "

            "a bad fileName\n");     

        testPass = FALSE;





    }





    /* test .................. GetTempFileNameA .................. */



    /* test with invalid path name */

    uiError = GetTempFileNameA(sDot, sPrefix, uUnique, szReturnedName);

    if (uiError == 0)

    {

        lastErr=GetLastError();

        if(lastErr != ERROR_DIRECTORY)

        {



            Trace("GetTempFileNameA: calling GetLastError() returned [%u] "

                "while it should return [%u] for invalid path name\n",

                lastErr,ERROR_DIRECTORY);   

            testPass = FALSE;

        }

    }

    else

    {

        Trace("GetTempFileNameA: GetTempFileNameA succeeded when given "

            "invalid path name\n");     

        testPass = FALSE;

    }







    /* test .................. GetTempFileNameW .................. */    



    /* test with invalid path name */

    uiError = GetTempFileNameW(wDot, wPrefix, uUnique, wzReturnedName);

    if (uiError == 0)

    {

        lastErr=GetLastError();

        if(lastErr != ERROR_DIRECTORY)

        {



            Trace("GetTempFileNameW: calling GetLastError() returned [%u] "

                "while it should return [%u] for an invalid path name\n",

                lastErr,ERROR_DIRECTORY);   

            testPass = FALSE;

        }

    }

    else

    {

        Trace("GetTempFileNameW: GetTempFileNameW succeeded when given"

            " an invalid path name\n");     

        testPass = FALSE;

    }



    if(! testPass)

    {

        Fail("");

    }













    PAL_Terminate();

    return PASS;

}



