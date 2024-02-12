// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*++



Module Name:

    directory.c

Abstract:

    Implementation of the file WIN API for the PAL

Revision History:



--*/

#include "pal/palinternal.h"
#include "pal/dbgmsg.h"
#include "pal/file.h"
#include "pal/stackstring.hpp"

#include <stdlib.h>
#include <sys/param.h>
#include <sys/types.h>
#include <sys/stat.h>
#include <errno.h>

SET_DEFAULT_DEBUG_CHANNEL(FILE);



/*++
Function:
  CreateDirectoryW

Note:
  lpSecurityAttributes always NULL.

See MSDN doc.
--*/
BOOL
PALAPI
CreateDirectoryW(
         IN LPCWSTR lpPathName,
         IN LPSECURITY_ATTRIBUTES lpSecurityAttributes)
{
    BOOL  bRet = FALSE;
    DWORD dwLastError = 0;
    int   mb_size;
    char  *mb_dir = NULL;

    PERF_ENTRY(CreateDirectoryW);
    ENTRY("CreateDirectoryW(lpPathName=%p (%S), lpSecurityAttr=%p)\n",
          lpPathName?lpPathName:W16_NULLSTRING,
          lpPathName?lpPathName:W16_NULLSTRING, lpSecurityAttributes);

    if ( lpSecurityAttributes )
    {
        ASSERT("lpSecurityAttributes is not NULL as it should be\n");
        dwLastError = ERROR_INVALID_PARAMETER;
        goto done;
    }

    /* translate the wide char lpPathName string to multibyte string */
    if(0 == (mb_size = WideCharToMultiByte( CP_ACP, 0, lpPathName, -1, NULL, 0,
                                            NULL, NULL )))
    {
        ASSERT("WideCharToMultiByte failure! error is %d\n", GetLastError());
        dwLastError = ERROR_INTERNAL_ERROR;
        goto done;
    }

    if (((mb_dir = (char *)PAL_malloc(mb_size)) == NULL) ||
        (WideCharToMultiByte( CP_ACP, 0, lpPathName, -1, mb_dir, mb_size, NULL,
                              NULL) != mb_size))
    {
        ASSERT("WideCharToMultiByte or PAL_malloc failure! LastError:%d errno:%d\n",
              GetLastError(), errno);
        dwLastError = ERROR_INTERNAL_ERROR;
        goto done;
    }

    bRet = CreateDirectoryA(mb_dir,NULL);
done:
    if( dwLastError )
    {
        SetLastError( dwLastError );
    }
    if (mb_dir != NULL)
    {
        free(mb_dir);
    }
    LOGEXIT("CreateDirectoryW returns BOOL %d\n", bRet);
    PERF_EXIT(CreateDirectoryW);
    return bRet;
}

/*++
Routine Name:

    RemoveDirectoryHelper

Routine Description:

    Core function which removes a directory. Called by RemoveDirectory[AW]

Parameters:

    LPSTR lpPathName - [in/out]
        The directory name to remove. It is converted in place to a unix path.

Return Value:

    BOOL -
        TRUE <=> successful

--*/

static
BOOL
RemoveDirectoryHelper (
    PathCharString& lpPathName,
    LPDWORD dwLastError
)
{
    BOOL  bRet = FALSE;
    *dwLastError = 0;

    if ( rmdir(lpPathName) != 0 )
    {
        TRACE("Removal of directory [%s] was unsuccessful, errno = %d.\n",
              lpPathName.GetString(), errno);

        switch( errno )
        {
        case ENOTDIR:
            /* FALL THROUGH */
        case ENOENT:
        {
            struct stat stat_data;

            if ( stat( lpPathName, &stat_data) == 0 &&
                 (stat_data.st_mode & S_IFMT) == S_IFREG )
            {
                /* Not a directory, it is a file. */
                *dwLastError = ERROR_DIRECTORY;
            }
            else
            {
                FILEGetProperNotFoundError( lpPathName, dwLastError );
            }
            break;
        }
        case ENOTEMPTY:
            *dwLastError = ERROR_DIR_NOT_EMPTY;
            break;
        default:
            *dwLastError = ERROR_ACCESS_DENIED;
        }
    }
    else {
        TRACE("Removal of directory [%s] was successful.\n", lpPathName.GetString());
        bRet = TRUE;
    }

    return bRet;
}

/*++
Function:
  RemoveDirectoryW

See MSDN doc.
--*/
BOOL
PALAPI
RemoveDirectoryW(
         IN LPCWSTR lpPathName)
{
    PathCharString mb_dirPathString;
    int   mb_size;
    DWORD dwLastError = 0;
    BOOL  bRet = FALSE;
    size_t length;
    char * mb_dir = NULL;

    PERF_ENTRY(RemoveDirectoryW);
    ENTRY("RemoveDirectoryW(lpPathName=%p (%S))\n",
          lpPathName?lpPathName:W16_NULLSTRING,
          lpPathName?lpPathName:W16_NULLSTRING);

    if (lpPathName == NULL)
    {
        dwLastError = ERROR_PATH_NOT_FOUND;
        goto done;
    }

    length = (PAL_wcslen(lpPathName)+1) * 3;
    mb_dir = mb_dirPathString.OpenStringBuffer(length);
    if (NULL == mb_dir)
    {
        dwLastError = ERROR_NOT_ENOUGH_MEMORY;
        goto done;
    }

    mb_size = WideCharToMultiByte( CP_ACP, 0, lpPathName, -1, mb_dir, length,
                                   NULL, NULL );

    if( mb_size == 0 )
    {
        mb_dirPathString.CloseBuffer(0);
        ASSERT("WideCharToMultiByte failure! error is %d\n", dwLastError);
        dwLastError = ERROR_INTERNAL_ERROR;
        goto done;
    }

    mb_dirPathString.CloseBuffer(mb_size - 1);

    if ((bRet = RemoveDirectoryHelper (mb_dirPathString, &dwLastError)))
    {
        TRACE("Removal of directory [%s] was successful.\n", mb_dir);
    }

done:
    if( dwLastError )
    {
        SetLastError( dwLastError );
    }

    LOGEXIT("RemoveDirectoryW returns BOOL %d\n", bRet);
    PERF_EXIT(RemoveDirectoryW);
    return bRet;
}


/*++
Function:
  GetCurrentDirectoryA

--*/
DWORD
GetCurrentDirectoryA(PathCharString& lpBuffer)
{
    DWORD dwDirLen = 0;
    DWORD dwLastError = 0;

    char  *current_dir;

    PERF_ENTRY(GetCurrentDirectoryA);
    ENTRY("GetCurrentDirectoryA(lpBuffer=%p)\n", lpBuffer.GetString());

    current_dir = lpBuffer.OpenStringBuffer(MAX_PATH);
    /* NULL first arg means getcwd will allocate the string */
    current_dir = getcwd( current_dir, MAX_PATH);

    if (current_dir != NULL )
    {
        dwDirLen = strlen( current_dir );
        lpBuffer.CloseBuffer(dwDirLen);
        goto done;
    }
    else if ( errno == ERANGE )
    {
        lpBuffer.CloseBuffer(0);
        current_dir = getcwd( NULL, 0);
    }

    if ( !current_dir )
    {
        WARN("Getcwd failed with errno=%d [%s]\n", errno, strerror(errno));
        dwLastError = DIRGetLastErrorFromErrno();
        dwDirLen = 0;
        goto done;
    }

    dwDirLen = strlen( current_dir );
    lpBuffer.Set(current_dir, dwDirLen);
    free(current_dir);
done:

    if ( dwLastError )
    {
        SetLastError(dwLastError);
    }

    LOGEXIT("GetCurrentDirectoryA returns DWORD %u\n", dwDirLen);
    PERF_EXIT(GetCurrentDirectoryA);
    return dwDirLen;
}

/*++
Function:
  SetCurrentDirectoryW

See MSDN doc.
--*/
BOOL
PALAPI
SetCurrentDirectoryW(
            IN LPCWSTR lpPathName)
{
    BOOL bRet;
    DWORD dwLastError = 0;
    PathCharString dirPathString;
    int  size;
    size_t length;
    char * dir = NULL;

    PERF_ENTRY(SetCurrentDirectoryW);
    ENTRY("SetCurrentDirectoryW(lpPathName=%p (%S))\n",
          lpPathName?lpPathName:W16_NULLSTRING,
          lpPathName?lpPathName:W16_NULLSTRING);

   /*check if the given path is null. If so
     return FALSE*/
    if (lpPathName == NULL )
    {
        ERROR("Invalid path/directory name\n");
        dwLastError = ERROR_INVALID_NAME;
        bRet = FALSE;
        goto done;
    }

    length = (PAL_wcslen(lpPathName)+1) * 3;
    dir = dirPathString.OpenStringBuffer(length);
    if (NULL == dir)
    {
        dwLastError = ERROR_NOT_ENOUGH_MEMORY;
        bRet = FALSE;
        goto done;
    }

    size = WideCharToMultiByte( CP_ACP, 0, lpPathName, -1, dir, length,
                                NULL, NULL );

    if( size == 0 )
    {
        dirPathString.CloseBuffer(0);
        dwLastError = GetLastError();
        ASSERT("WideCharToMultiByte failure! error is %d\n", dwLastError);
        dwLastError = ERROR_INTERNAL_ERROR;
        bRet = FALSE;
        goto done;
    }

    dirPathString.CloseBuffer(size - 1);
    bRet = SetCurrentDirectoryA(dir);
done:
    if( dwLastError )
    {
        SetLastError(dwLastError);
    }

    LOGEXIT("SetCurrentDirectoryW returns BOOL %d\n", bRet);
    PERF_EXIT(SetCurrentDirectoryW);
    return bRet;
}

/*++
Function:
  CreateDirectoryA

Note:
  lpSecurityAttributes always NULL.

See MSDN doc.
--*/
BOOL
PALAPI
CreateDirectoryA(
         IN LPCSTR lpPathName,
         IN LPSECURITY_ATTRIBUTES lpSecurityAttributes)
{
    BOOL  bRet = FALSE;
    DWORD dwLastError = 0;
    PathCharString realPath;
    char* realPathBuf;
    LPSTR unixPathName = NULL;
    int pathLength;
    int i;
    const int mode = S_IRWXU | S_IRWXG | S_IRWXO;

    PERF_ENTRY(CreateDirectoryA);
    ENTRY("CreateDirectoryA(lpPathName=%p (%s), lpSecurityAttr=%p)\n",
          lpPathName?lpPathName:"NULL",
          lpPathName?lpPathName:"NULL", lpSecurityAttributes);

    if ( lpSecurityAttributes )
    {
        ASSERT("lpSecurityAttributes is not NULL as it should be\n");
        dwLastError = ERROR_INVALID_PARAMETER;
        goto done;
    }

    // Windows returns ERROR_PATH_NOT_FOUND when called with NULL.
    // If we don't have this check, strdup(NULL) segfaults.
    if (lpPathName == NULL)
    {
        ERROR("CreateDirectoryA called with NULL pathname!\n");
        dwLastError = ERROR_PATH_NOT_FOUND;
        goto done;
    }

    unixPathName = strdup(lpPathName);
    if (unixPathName == NULL )
    {
        ERROR("strdup() failed\n");
        dwLastError = ERROR_NOT_ENOUGH_MEMORY;
        goto done;
    }
    // Remove any trailing slashes at the end because mkdir might not
    // handle them appropriately on all platforms.
    pathLength = strlen(unixPathName);
    i = pathLength;
    while(i > 1)
    {
        if(unixPathName[i - 1] =='/')
        {
            unixPathName[i - 1]='\0';
            i--;
        }
        else
        {
            break;
        }
    }


    // Get an absolute path.
    if (unixPathName[0] == '/')
    {
        realPathBuf = unixPathName;
    }
    else
    {

        DWORD len = GetCurrentDirectoryA(realPath);
        if (len == 0 || !realPath.Reserve(realPath.GetCount() + pathLength + 1 ))
        {
            dwLastError = DIRGetLastErrorFromErrno();
            WARN("Getcwd failed with errno=%d \n", dwLastError);
            goto done;
        }

        realPath.Append("/", 1);
        realPath.Append(unixPathName, pathLength);
        realPathBuf = realPath.OpenStringBuffer(realPath.GetCount());
    }

    // Canonicalize the path so we can determine its length.
    FILECanonicalizePath(realPathBuf);

    if ( mkdir(realPathBuf, mode) != 0 )
    {
        TRACE("Creation of directory [%s] was unsuccessful, errno = %d.\n",
              unixPathName, errno);

        switch( errno )
        {
        case ENOTDIR:
            /* FALL THROUGH */
        case ENOENT:
            FILEGetProperNotFoundError( realPathBuf, &dwLastError );
            goto done;
        case EEXIST:
            dwLastError = ERROR_ALREADY_EXISTS;
            break;
        default:
            dwLastError = ERROR_ACCESS_DENIED;
        }
    }
    else
    {
        TRACE("Creation of directory [%s] was successful.\n", unixPathName);
        bRet = TRUE;
    }

    realPath.CloseBuffer(0); //The PathCharString usage is done
done:
    if( dwLastError )
    {
        SetLastError( dwLastError );
    }
    free( unixPathName );
    LOGEXIT("CreateDirectoryA returns BOOL %d\n", bRet);
    PERF_EXIT(CreateDirectoryA);
    return bRet;
}

/*++
Function:
  SetCurrentDirectoryA

See MSDN doc.
--*/
BOOL
PALAPI
SetCurrentDirectoryA(
            IN LPCSTR lpPathName)
{
    BOOL bRet = FALSE;
    DWORD dwLastError = 0;
    int result;

    PERF_ENTRY(SetCurrentDirectoryA);
    ENTRY("SetCurrentDirectoryA(lpPathName=%p (%s))\n",
          lpPathName?lpPathName:"NULL",
          lpPathName?lpPathName:"NULL");

   /*check if the given path is null. If so
     return FALSE*/
    if (lpPathName == NULL )
    {
        ERROR("Invalid path/directory name\n");
        dwLastError = ERROR_INVALID_NAME;
        goto done;
    }

    TRACE("Attempting to open Unix dir [%s]\n", lpPathName);
    result = chdir(lpPathName);

    if ( result == 0 )
    {
        bRet = TRUE;
    }
    else
    {
        if ( errno == ENOTDIR || errno == ENOENT )
        {
            struct stat stat_data;

            if ( stat( lpPathName, &stat_data) == 0 &&
                 (stat_data.st_mode & S_IFMT) == S_IFREG )
            {
                /* Not a directory, it is a file. */
                dwLastError = ERROR_DIRECTORY;
            }
            else
            {
                FILEGetProperNotFoundError( lpPathName, &dwLastError );
            }
            TRACE("chdir() failed, path was invalid.\n");
        }
        else
        {
            dwLastError = ERROR_ACCESS_DENIED;
            ERROR("chdir() failed; errno is %d (%s)\n", errno, strerror(errno));
        }
    }


done:
    if( dwLastError )
    {
        SetLastError(dwLastError);
    }

    LOGEXIT("SetCurrentDirectoryA returns BOOL %d\n", bRet);
    PERF_EXIT(SetCurrentDirectoryA);
    return bRet;
}
