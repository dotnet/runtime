// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*=====================================================================
**
** Source:  CreateDirectoryA.c
**
** Purpose: Tests the PAL implementation of the CreateDirectoryA function.
**
** Depends on:
**          RemoveDirectoryW (since RemoveDirectoryA is unavailable)
**          GetCurrentDirectoryA
**
**
**===================================================================*/

#include <palsuite.h>


/* apparently, under WIN32 the max path size is 248 but under 
   BSD it is _MAX_PATH */
#if WIN32
#define CREATE_MAX_PATH_SIZE    248
#else
#define CREATE_MAX_PATH_SIZE    _MAX_PATH
#endif


int __cdecl main(int argc, char *argv[])
{
    const char* szTestDir = {"test_directory"};
    const char* szDotDir = {".dotDirectory"};
    BOOL bRc = FALSE;
    BOOL bSuccess = FALSE;
    const int buf_size = CREATE_MAX_PATH_SIZE + 10;
    char szDirName[CREATE_MAX_PATH_SIZE + 10];
    char buffer[CREATE_MAX_PATH_SIZE + 10];
    WCHAR* pTemp = NULL;
    DWORD curDirLen;
    DWORD curDirectory = 1024;
    
    if (0 != PAL_Initialize(argc,argv))
    {
        return FAIL;
    }

    
    /* directory does not exist */ 
    bRc = CreateDirectoryA(szTestDir, NULL);
    if (bRc == FALSE)
    {
        Fail("CreateDirectoryA: Failed to create \"%s\" with error code %ld\n",
            szTestDir,
            GetLastError());
    }

    
    /* directory exists should fail */
    bRc = CreateDirectoryA(szTestDir, NULL);
    if (bRc == TRUE)
    {
        pTemp = convert((LPSTR)szTestDir);
        bRc = RemoveDirectoryW(pTemp);
        free(pTemp);
        if (!bRc)
        {
            Trace("CreateDirectoryA: RemoveDirectoryW failed to remove "
                "\"%s\" with the error code %ld.\n",
                szTestDir,
                GetLastError());
        }
        Fail("CreateDirectoryA: Succeeded creating the directory"
            "\"%s\" when it exists already.\n",
            szTestDir);
    }
    else 
    {
        pTemp = convert((LPSTR)szTestDir);
        bRc = RemoveDirectoryW(pTemp);
        free(pTemp);
        if (!bRc)
        {
            Fail("CreateDirectoryA: RemoveDirectoryW failed to remove "
                "\"%s\" with the error code %ld.\n",
                szTestDir,
                GetLastError());
        }
    }

   
    /* long directory names (CREATE_MAX_PATH_SIZE - 1, CREATE_MAX_PATH_SIZE  
       and CREATE_MAX_PATH_SIZE + 1 characters 
      including terminating null char) */

    curDirLen = GetCurrentDirectoryA(0, NULL);

    memset(szDirName, 0, buf_size);
    memset(szDirName, 'a', CREATE_MAX_PATH_SIZE - 2 - curDirLen);
    bRc = CreateDirectoryA(szDirName, NULL);
    if (bRc == FALSE)
    {
        Fail("CreateDirectoryA: Failed to create a directory"
            " name %d chars long with the error code %ld\n", 
            strlen(szDirName),
            GetLastError());
    }
    else
    {
        /* Check to see if it's possible to navigate to directory */
        GetCurrentDirectoryA(curDirectory, buffer);
        bSuccess = SetCurrentDirectoryA(szDirName);
        if(!bSuccess)
        {
            Fail("CreateDirectoryA: SetCurrentDirectoryA failed to "
                "navigate to the newly created directory with error "
                "code %u.\n", GetLastError());
        }

        /* Set directory back to initial directory */
        SetCurrentDirectoryA(buffer);

        pTemp = convert((LPSTR)szDirName);
        bRc = RemoveDirectoryW(pTemp);
        free(pTemp);
        if (!bRc)
        {
            Fail("CreateDirectoryA: RemoveDirectoryW failed to remove "
                "\"%s\" with the error code %ld.\n",
                szDirName,
                GetLastError());
        }
    }


    memset(szDirName, 0, buf_size);
    memset(szDirName, 'a', CREATE_MAX_PATH_SIZE - 1 - curDirLen);
    bRc = CreateDirectoryA(szDirName, NULL);
    if (bRc == FALSE)
    {
        Fail("CreateDirectoryA: Failed to create a directory"
            " name %d chars long with error code %ld\n", 
            strlen(szDirName),
            GetLastError());
    }
    else
    {
        /* Check to see if it's possible to navigate to directory */
        GetCurrentDirectoryA(curDirectory, buffer);
        bSuccess = SetCurrentDirectoryA(szDirName);
        if(!bSuccess)
        {
            Fail("CreateDirectoryA: SetCurrentDirectoryA failed to "
                "navigate to the newly created directory with error "
                "code %u.\n", GetLastError());
        }

        /* Set Directroy back to initial directory */
        SetCurrentDirectoryA(buffer);
        
        pTemp = convert(szDirName);
        bRc = RemoveDirectoryW(pTemp);
        free(pTemp);
        if (!bRc)
        {
            Fail("CreateDirectoryA: RemoveDirectoryW failed to remove "
                "\"%s\" with the error code %ld.\n",
                szDirName,
                GetLastError());
        }
    }

    memset(szDirName, 0, buf_size);
    memset(szDirName, 'a', CREATE_MAX_PATH_SIZE - curDirLen);
    bRc = CreateDirectoryA(szDirName, NULL);
    if (bRc != FALSE)
    {
        /* Check to see if it's possible to navigate to directory */
        GetCurrentDirectoryA(curDirectory, buffer);
        bSuccess = SetCurrentDirectoryA(szDirName);
        if(!bSuccess)
        {
            Fail("CreateDirectoryA: SetCurrentDirectoryA failed to "
                "navigate to the newly created directory with error "
                "code %u.\n", GetLastError());
        }

        /* set directory back to initial directory */
        SetCurrentDirectoryA(buffer);

        pTemp = convert(szDirName);
        bRc = RemoveDirectoryW(pTemp);
        free(pTemp);
        if (!bRc)
        {
            Trace("CreateDirectoryA: RemoveDirectoryW failed to remove "
                "\"%s\" with the error code %ld.\n",
                szDirName,
                GetLastError());
        }
		if (strlen(szDirName) > CREATE_MAX_PATH_SIZE)
		{
        	Fail("CreateDirectoryA: Failed because it created a directory"
            	" name 1 character longer (%d chars) than the max dir size "
            	"allowed\n", 
            	strlen(szDirName));
		}
    }


    /* long directory name CREATE_MAX_PATH_SIZE + 3 chars including "..\" 
       (real path length <= CREATE_MAX_PATH_SIZE) */
    memset(szDirName, 0, buf_size);
    memset(szDirName, 'a', CREATE_MAX_PATH_SIZE + 3 - 1 - curDirLen);
    szDirName[0] = '.';
    szDirName[1] = '.';
    szDirName[2] = '\\';
    bRc = CreateDirectoryA(szDirName, NULL);
    if (bRc == FALSE)
    {
        Fail("CreateDirectoryA: Failed to create a directory name more "
            "than %d chars long and its real path name is less "
            "than %d chars, error %u\n",
            CREATE_MAX_PATH_SIZE,
            CREATE_MAX_PATH_SIZE, GetLastError());
    }
    else
    {
        /* Check to see if it's possible to navigate to directory */
        GetCurrentDirectoryA(curDirectory, buffer);
        bSuccess = SetCurrentDirectoryA(szDirName);
        if(!bSuccess)
        {
            Fail("CreateDirectoryA: SetCurrentDirectoryA failed to "
                "navigate to the newly created directory with error "
                "code %u.\n", GetLastError());
        }

        /* set directory back to initial directory */
        SetCurrentDirectoryA(buffer);

        pTemp = convert(szDirName);
        bRc = RemoveDirectoryW(pTemp);
        free(pTemp);
        if (!bRc)
        {
            Fail("CreateDirectoryA: RemoveDirectoryW failed to remove "
                " \"%s\" with the error code %ld.\n",
                szDirName,
                GetLastError());
        }
    }


    /* directories with dots  */
    memset(szDirName, 0, buf_size);
    sprintf_s(szDirName, _countof(szDirName), szDotDir);
    bRc = CreateDirectoryA(szDirName, NULL);
    if (bRc == FALSE)
    {
        Fail("CreateDirectoryA: Failed to create \"%s\" with error code %ld\n",
            szDotDir,
            GetLastError());
    }
    else
    {
        
        /* Check to see if it's possible to navigate to directory */
        GetCurrentDirectoryA(curDirectory, buffer);
        bSuccess = SetCurrentDirectoryA(szDirName);
        if(!bSuccess)
        {
            Fail("CreateDirectoryA: SetCurrentDirectoryA failed to "
                "navigate to the newly created directory with error "
                "code %u.\n", GetLastError());
        }

        /* set directory back to initial directory */
        SetCurrentDirectoryA(buffer);
        
        pTemp = convert((LPSTR)szDotDir);
        bRc = RemoveDirectoryW(pTemp);
        free(pTemp);
        if (!bRc)
        {
            Fail("CreateDirectoryA: RemoveDirectoryW failed to remove "
                " \"%s\" with the error code %ld.\n",
                szDotDir,
                GetLastError());
        }
    }


    PAL_Terminate();  
    return PASS;
}
