//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
//

/*=====================================================================
**
** Source:  GetTempPathW.c (test 1)
**
** Purpose: Tests the PAL implementation of the GetTempPathW function.
**
**
**===================================================================*/

#include <palsuite.h>

static void SetEnvVar(WCHAR tmpValue[], WCHAR tempPath[])
{
    BOOL checkValue=FALSE;
    
    /* see if the environment variable is created correctly */
    checkValue = SetEnvironmentVariableW(tmpValue, tempPath);
    if(!checkValue)
    {
        if (tempPath == NULL)
        {
            if (SetEnvironmentVariableW(tmpValue, convert("")) &&
                SetEnvironmentVariableW(tmpValue, NULL))
            {
                return;
            }
        }
        
        Fail("GetTempPathA: ERROR -> Failed to set the environment "
            "variable correctly.\n");
    }  
}

int __cdecl main(int argc, char *argv[])
{
    DWORD dwBuffLength = _MAX_DIR;
    WCHAR wPath[_MAX_DIR];
    WCHAR tmpValue[] = {'T','M','P','\0'};
    WCHAR tempValue[] = {'T','E','M','P','\0'};
#if WIN32
    WCHAR tempPath[] = {'C',':','\\','t','e','m','p','\\','\0'};
    WCHAR tmpPath[] = {'C',':','\\','t','m','p','\\','\0'}; 
#else
    WCHAR tempPath[] = {'.','\\','t','e','m','p','\\','\0'};
    WCHAR tmpPath[] = {'.','\\','t','m','p','\\','\0'};
#endif


    if (0 != PAL_Initialize(argc,argv))
    {
        return FAIL;
    }

    if (GetTempPathW(dwBuffLength, wPath) == 0)
    {
        Fail("GetTempPathW: ERROR -> Failed to return a temporary path. "
            "error code: %ld\n", GetLastError());
    }
    else
    {
        // CreateDirectory should fail on an existing directory
        if (CreateDirectoryW(wPath, NULL) != 0)
        {
            Fail("GetTempPathW: ERROR -> The path returned is apparently "
                "invalid since CreateDirectory succeeded whereas it should "
                "have failed.\n");
        }
    }

    /* set both tmp and temp to null and check that gettemppathw returns
    a non zero value*/
    SetEnvVar(tmpValue, NULL);

    /* create the environment variable */
    SetEnvVar(tempValue, tempPath);  
    
    /* set the environment variable to NULL */
    SetEnvVar(tempValue, NULL);  
    
    if(GetTempPathW(dwBuffLength, wPath) == 0)
    {
        Fail("GetTempPathW: ERROR -> Failed to return a temporary path. "
            "error code: %ld\n", GetLastError());
    }

    /* set temp, and gettemppathw should return value of temp */
    SetEnvVar(tempValue, tempPath);
    
    if(GetTempPathW(dwBuffLength, wPath) == 0)
    {
        Fail("GetTempPathW: ERROR -> Failed to return a temporary path. "
            "error code: %ld\n", GetLastError());
    }
    
    if(wcscmp(wPath, tempPath) != 0)
    {
        Fail("GetTempPathW: ERROR -> Failed to return correct temporary path. "
            "Expected path %s but got %s.\n", tempPath, wPath);
    }

    /* set temp to null, and set temp to a proper value,
    gettemppathw should return value stored in tmp */
    SetEnvVar(tempValue, NULL);
    SetEnvVar(tmpValue, tmpPath);
    
    if(GetTempPathW(dwBuffLength, wPath) == 0)
    {
        Fail("GetTempPathW: ERROR -> Failed to return a temporary path. "
            "error code: %ld\n", GetLastError());
    }
    
    if(wcscmp(wPath, tmpPath) != 0)
    {
        Fail("GetTempPathW: ERROR -> Failed to return correct temporary path. "
            "Expected path %s but got %s.\n", tmpPath, wPath);
    }

    /* set temp and gettemppathw should return value stored in tmp */
    SetEnvVar(tempValue, tempPath);
    
    if(GetTempPathW(dwBuffLength, wPath) == 0)
    {
        Fail("GetTempPathW: ERROR -> Failed to return a temporary path. "
            "error code: %ld\n", GetLastError());
    }
    
    if(wcscmp(wPath, tmpPath) != 0)
    {
        Fail("GetTempPathW: ERROR -> Failed to return correct temporary path. "
            "Expected path %s but got %s.\n", tmpPath, wPath);
    }

    PAL_Terminate();
    return PASS;
}

