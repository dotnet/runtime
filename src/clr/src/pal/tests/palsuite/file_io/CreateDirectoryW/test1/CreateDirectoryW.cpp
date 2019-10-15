// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*=====================================================================
**
** Source:  CreateDirectoryW.c
**
** Purpose: Tests the PAL implementation of the CreateDirectoryW function.
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
    pTemp = convert("test_directory");
    bRc = CreateDirectoryW(pTemp, NULL);
    free(pTemp);
    if (bRc == FALSE)
    {
        Fail("CreateDirectoryW: Failed to create \"test_directory\"\n");
    }

    /*  directory exists */
    pTemp = convert("test_directory");
    bRc = CreateDirectoryW(pTemp, NULL);
    if (bRc == TRUE)
    {
        bRc = RemoveDirectoryW(pTemp);
        if(!bRc)
        {
            free(pTemp);
            Fail("CreateDirectoryW: RemoveDirectoryW failed to "
                "delete the directory with error %u.\n",
                GetLastError());
        }
        free(pTemp);
        Fail("CreateDirectoryW: Succeeded creating the directory"
            " \"test_directory\" when it exists already.\n");
    }

    bRc = RemoveDirectoryW(pTemp);
    if(!bRc)
    {
        free(pTemp);
        Fail("CreateDirectoryW: RemoveDirectoryW failed to "
            "delete the directory with error %u.\n",
            GetLastError());
    }
    free(pTemp);

    /* long directory names (CREATE_MAX_PATH_SIZE - 1, CREATE_MAX_PATH_SIZE  
     and CREATE_MAX_PATH_SIZE + 1 characters 
     including terminating null char) */

    curDirLen = GetCurrentDirectoryA(0, NULL);

    memset(szDirName, 0, buf_size);
    memset(szDirName, 'a', CREATE_MAX_PATH_SIZE - 2 - curDirLen);
    pTemp = convert((LPSTR)szDirName);
    bRc = CreateDirectoryW(pTemp, NULL);
    if (bRc == FALSE)
    {
        free(pTemp);
        Fail("CreateDirectoryW: Failed to create a directory"
            " name (%d) chars long with the error code %ld\n", 
            CREATE_MAX_PATH_SIZE - 1,
            GetLastError());
    }
    else
    {

        /* Check to see if it's possible to navigate to directory */
        GetCurrentDirectoryA(curDirectory, buffer);
        bSuccess = SetCurrentDirectoryA(szDirName);
        if(!bSuccess)
        {
            Trace("CreateDirectoryW: SetCurrentDirectoryA failed to "
                "navigate to the newly created directory with error "
                "code %u.\n", GetLastError());  
            bRc = RemoveDirectoryW(pTemp);
            if(!bRc)
            {
                free(pTemp);
                Fail("CreateDirectoryW: RemoveDirectoryW failed to "
                    "delete the directory with error %u.\n",
                    GetLastError());
            }
            free(pTemp);
            Fail("");
  
        }

        /* Set directory back to initial directory */
        bRc = SetCurrentDirectoryA(buffer);
        if(!bRc)
        {
            free(pTemp);
            Fail("CreateDirectoryW: SetCurrentDirectoryA failed to "
                "change the directory with error %u.\n",
                GetLastError());
        }

        bRc = RemoveDirectoryW(pTemp);
        if(!bRc)
        {
            free(pTemp);
            Fail("CreateDirectoryW: RemoveDirectoryW failed to "
                "delete the directory with error %u.\n",
                GetLastError());
        }
        free(pTemp);
    }


    memset(szDirName, 0, buf_size);
    memset(szDirName, 'a', CREATE_MAX_PATH_SIZE - 1 - curDirLen);
    pTemp = convert(szDirName);
    bRc = CreateDirectoryW(pTemp, NULL);
    if (bRc == FALSE)
    {
        free(pTemp);
        Fail("CreateDirectoryW: Failed to create a directory"
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
            Trace("CreateDirectoryW: SetCurrentDirectoryA failed to "
                "navigate to the newly created directory with error "
                "code %u.\n", GetLastError());
        
            bRc = RemoveDirectoryW(pTemp);
            if(!bRc)
            {
                free(pTemp);
                Fail("CreateDirectoryW: RemoveDirectoryW failed to "
                    "delete the directory with error %u.\n",
                    GetLastError());
            }
            free(pTemp);
            Fail("");
        }

        /* Set directory back to initial directory */
        bRc = SetCurrentDirectoryA(buffer);
        if(!bRc)
        {
            free(pTemp);
            Fail("CreateDirectoryW: SetCurrentDirectoryA failed to "
                "change the directory with error %u.\n",
                GetLastError());
        }

        
        bRc = RemoveDirectoryW(pTemp);
        if(!bRc)
        {
            free(pTemp);
            Fail("CreateDirectoryW: RemoveDirectoryW failed to "
                "delete the directory with error %u.\n",
                GetLastError());
        }
        free(pTemp);
    }

    memset(szDirName, 0, buf_size);
    memset(szDirName, 'a', CREATE_MAX_PATH_SIZE - curDirLen);
    pTemp = convert(szDirName);
    bRc = CreateDirectoryW(pTemp, NULL);

    if (bRc != FALSE)
    {
        RemoveDirectoryW(pTemp);    
        if(!bRc)
        {
            free(pTemp);
            Fail("CreateDirectoryW: RemoveDirectoryW failed to "
                "delete the directory with error %u.\n",
                GetLastError());
        }
		if (strlen(szDirName) > CREATE_MAX_PATH_SIZE)
		{
        	free(pTemp);
        	Fail("CreateDirectoryW: Failed because it created a directory"
            	" name 1 character longer (%d chars) than the max dir size"
            	" allowed\n", 
            	strlen(szDirName));
		}
    }
      
    free(pTemp);
    
    /* long directory name CREATE_MAX_PATH_SIZE + 3 chars including "..\" 
    (real path length <= CREATE_MAX_PATH_SIZE) */
    memset(szDirName, 0, buf_size);
    memset(szDirName, 'a', CREATE_MAX_PATH_SIZE + 3 - 1 - curDirLen);
    szDirName[0] = '.';
    szDirName[1] = '.';
    szDirName[2] = '\\';
    pTemp = convert(szDirName);
    bRc = CreateDirectoryW(pTemp, NULL);
    if (bRc == FALSE)
    {
        free(pTemp);
        Fail("CreateDirectoryW: Failed to create a directory name more "
            "than %d chars long and its real path name is less "
            "than %d chars\n",
            CREATE_MAX_PATH_SIZE,
            CREATE_MAX_PATH_SIZE);
    }
    else
    {
        /* Check to see if it's possible to navigate to directory */
        GetCurrentDirectoryA(curDirectory, buffer);
        bSuccess = SetCurrentDirectoryA(szDirName);
        if(!bSuccess)
        {
            Trace("CreateDirectoryW: SetCurrentDirectoryA failed to "
                "navigate to the newly created directory with error "
                "code %u.\n", GetLastError());
            bRc = RemoveDirectoryW(pTemp);
            if(!bRc)
            {
                free(pTemp);    
                Fail("CreateDirectoryW: RemoveDirectoryW failed to "
                    "delete the directory with error %u.\n",
                    GetLastError());
            }
            free(pTemp);
            Fail("");
        }

        /* Set directory back to initial directory */
        bRc = SetCurrentDirectoryA(buffer);
        if(!bRc)
        {
            free(pTemp);
            Fail("CreateDirectoryW: SetCurrentDirectoryA failed to "
                "change the directory with error %u.\n",
                GetLastError());
        }
        
        bRc = RemoveDirectoryW(pTemp);
        if(!bRc)
        {
            free(pTemp);
            Fail("CreateDirectoryW: RemoveDirectoryW failed to "
                "delete the directory with error %u.\n",
                GetLastError());
        }
        free(pTemp);
    }

    /* directories with dots */
    memset(szDirName, 0, 252);
    sprintf_s(szDirName, _countof(szDirName), ".dotDirectory");
    pTemp = convert(szDirName);
    bRc = CreateDirectoryW(pTemp, NULL);
    if (bRc == FALSE)
    {
        free(pTemp);
        Fail("CreateDirectoryW: Failed to create a dot directory\n");
    }
    else
    {
        /* Check to see if it's possible to navigate to directory */
        GetCurrentDirectoryA(curDirectory, buffer);
        bSuccess = SetCurrentDirectoryA(szDirName);
        if(!bSuccess)
        {
            Trace("CreateDirectoryW: SetCurrentDirectoryA failed to "
                "navigate to the newly created directory with error "
                "code %u.\n", GetLastError());
    
            bRc = RemoveDirectoryW(pTemp);
            if(!bRc)
            {
                free(pTemp);            
                Fail("CreateDirectoryW: RemoveDirectoryW failed to "
                    "delete the directory with error %u.\n",
                    GetLastError());
            }
            free(pTemp);
            Fail("");
        }

        /* Set directory back to initial directory */
        bRc = SetCurrentDirectoryA(buffer);
        if(!bRc)
        {
            free(pTemp);
            Fail("CreateDirectoryW: SetCurrentDirectoryA failed to "
                "change the directory with error %u.\n",
                GetLastError());
        }

        bRc = RemoveDirectoryW(pTemp);
        if(!bRc)
        {
            free(pTemp);
            Fail("CreateDirectoryW: RemoveDirectoryW failed to "
                "delete the directory with error %u.\n",
                GetLastError());
        }
        free(pTemp);
    }
    
    PAL_Terminate();  
    return PASS;
}

