// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
        PAL_free(mb_dir);
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
    LPSTR lpPathName,
    LPDWORD dwLastError
)
{
    BOOL  bRet = FALSE;
    *dwLastError = 0; 

    FILEDosToUnixPathA( lpPathName );
    if ( !FILEGetFileNameFromSymLink(lpPathName))
    {
        FILEGetProperNotFoundError( lpPathName, dwLastError );
        goto done;
    }

    if ( rmdir(lpPathName) != 0 )
    {
        TRACE("Removal of directory [%s] was unsuccessful, errno = %d.\n",
              lpPathName, errno);

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
        TRACE("Removal of directory [%s] was successful.\n", lpPathName);
        bRet = TRUE;
    }

done:
    return bRet;
}

/*++
Function:
  RemoveDirectoryA

See MSDN doc.
--*/
BOOL
PALAPI
RemoveDirectoryA(
         IN LPCSTR lpPathName)
{
    DWORD dwLastError = 0;
    BOOL  bRet = FALSE;
    PathCharString mb_dirPathString;
    size_t length;
    char * mb_dir;
    
    PERF_ENTRY(RemoveDirectoryA);
    ENTRY("RemoveDirectoryA(lpPathName=%p (%s))\n",
          lpPathName,
          lpPathName);

    if (lpPathName == NULL) 
    {
        dwLastError = ERROR_PATH_NOT_FOUND;
        goto done;
    }

    length = strlen(lpPathName);
    mb_dir = mb_dirPathString.OpenStringBuffer(length);
    if (NULL == mb_dir)
    {
        dwLastError = ERROR_NOT_ENOUGH_MEMORY;
        goto done;
    }
    
    if (strncpy_s (mb_dir, sizeof(char) * (length+1), lpPathName, MAX_LONGPATH) != SAFECRT_SUCCESS)
    {
        mb_dirPathString.CloseBuffer(length);
        WARN("mb_dir is larger than MAX_LONGPATH (%d)!\n", MAX_LONGPATH);
        dwLastError = ERROR_FILENAME_EXCED_RANGE;
        goto done;
    }

    mb_dirPathString.CloseBuffer(length);
    bRet = RemoveDirectoryHelper (mb_dir, &dwLastError);

done:
    if( dwLastError )
    {
        SetLastError( dwLastError );
    }

    LOGEXIT("RemoveDirectoryA returns BOOL %d\n", bRet);
    PERF_EXIT(RemoveDirectoryA);
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
    char * mb_dir;

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
    mb_dirPathString.CloseBuffer(mb_size);

    if( mb_size == 0 )
    {
        dwLastError = GetLastError();
        if( dwLastError == ERROR_INSUFFICIENT_BUFFER )
        {
            WARN("lpPathName is larger than MAX_LONGPATH (%d)!\n", MAX_LONGPATH);
            dwLastError = ERROR_FILENAME_EXCED_RANGE;
        }
        else
        {
            ASSERT("WideCharToMultiByte failure! error is %d\n", dwLastError);
            dwLastError = ERROR_INTERNAL_ERROR;
        }
        goto done;
    }

    if ((bRet = RemoveDirectoryHelper (mb_dir, &dwLastError)))
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

See MSDN doc.
--*/
DWORD
PALAPI
GetCurrentDirectoryA(
             IN DWORD nBufferLength,
             OUT LPSTR lpBuffer)
{
    DWORD dwDirLen = 0;
    DWORD dwLastError = 0;

    char  *current_dir;

    PERF_ENTRY(GetCurrentDirectoryA);
    ENTRY("GetCurrentDirectoryA(nBufferLength=%u, lpBuffer=%p)\n", nBufferLength, lpBuffer);

    /* NULL first arg means getcwd will allocate the string */
    current_dir = PAL__getcwd( NULL, MAX_LONGPATH + 1 );

    if ( !current_dir )
    {
        WARN( "PAL__getcwd returned NULL\n" );
        dwLastError = DIRGetLastErrorFromErrno();
        goto done;
    }

    dwDirLen = strlen( current_dir );

    /* if the supplied buffer isn't long enough, return the required
       length, including room for the NULL terminator */
    if ( nBufferLength <= dwDirLen )
    {
        ++dwDirLen; /* include space for the NULL */
        goto done;
    }
    else
    {
        strcpy_s( lpBuffer, nBufferLength, current_dir );
    }

done:
    PAL_free( current_dir );

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
  GetCurrentDirectoryW

See MSDN doc.
--*/
DWORD
PALAPI
GetCurrentDirectoryW(
             IN DWORD nBufferLength,
             OUT LPWSTR lpBuffer)
{
    DWORD dwWideLen = 0;
    DWORD dwLastError = 0;

    char  *current_dir;
    int   dir_len;

    PERF_ENTRY(GetCurrentDirectoryW);
    ENTRY("GetCurrentDirectoryW(nBufferLength=%u, lpBuffer=%p)\n",
          nBufferLength, lpBuffer);

    current_dir = PAL__getcwd( NULL, MAX_LONGPATH + 1 );

    if ( !current_dir )
    {
        WARN( "PAL__getcwd returned NULL\n" );
        dwLastError = DIRGetLastErrorFromErrno();
        goto done;
    }

    dir_len = strlen( current_dir );
    dwWideLen = MultiByteToWideChar( CP_ACP, 0,
                 current_dir, dir_len,
                 NULL, 0 );

    /* if the supplied buffer isn't long enough, return the required
       length, including room for the NULL terminator */
    if ( nBufferLength > dwWideLen )
    {
        if(!MultiByteToWideChar( CP_ACP, 0, current_dir, dir_len + 1,
                     lpBuffer, nBufferLength ))
        {
            ASSERT("MultiByteToWideChar failure!\n");
            dwWideLen = 0;
            dwLastError = ERROR_INTERNAL_ERROR;
        }
    }
    else
    {
        ++dwWideLen; /* include space for the NULL */
    }

done:
    PAL_free( current_dir );

    if ( dwLastError )
    {
        SetLastError(dwLastError);
    }

    LOGEXIT("GetCurrentDirectoryW returns DWORD %u\n", dwWideLen);
    PERF_EXIT(GetCurrentDirectoryW);
    return dwWideLen;
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
    char * dir;
    
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
    dirPathString.CloseBuffer(size);
    
    if( size == 0 )
    {
        dwLastError = GetLastError();
        if( dwLastError == ERROR_INSUFFICIENT_BUFFER )
        {
            WARN("lpPathName is larger than MAX_LONGPATH (%d)!\n", MAX_LONGPATH);
            dwLastError = ERROR_FILENAME_EXCED_RANGE;
        }
        else
        {
            ASSERT("WideCharToMultiByte failure! error is %d\n", dwLastError);
            dwLastError = ERROR_INTERNAL_ERROR;
        }
        bRet = FALSE;
        goto done;
    }

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
    char *realPath;
    LPSTR UnixPathName = NULL;
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

    UnixPathName = PAL__strdup(lpPathName);
    if (UnixPathName == NULL )
    {
        ERROR("PAL__strdup() failed\n");
        dwLastError = ERROR_NOT_ENOUGH_MEMORY;
        goto done;
    }
    FILEDosToUnixPathA( UnixPathName );
    // Remove any trailing slashes at the end because mkdir might not
    // handle them appropriately on all platforms.
    pathLength = strlen(UnixPathName);
    i = pathLength;
    while(i > 1)
    {
        if(UnixPathName[i - 1] =='/')
        {
            UnixPathName[i - 1]='\0';
            i--;
        }
        else
        {
            break;
        }
    }

    // Check the constraint for the real path length (should be < MAX_LONGPATH).

    // Get an absolute path.
    if (UnixPathName[0] == '/')
    {
        realPath = UnixPathName;
    }
    else
    {
        const char *cwd = PAL__getcwd(NULL, MAX_LONGPATH);        
        if (NULL == cwd)
        {
            WARN("Getcwd failed with errno=%d [%s]\n", errno, strerror(errno));
            dwLastError = DIRGetLastErrorFromErrno();
            goto done;
        }

        // Copy cwd, '/', path
        int iLen = strlen(cwd) + 1 + pathLength + 1;
        realPath = static_cast<char *>(alloca(iLen));
        sprintf_s(realPath, iLen, "%s/%s", cwd, UnixPathName);

        PAL_free((char *)cwd);
    }
    
    // Canonicalize the path so we can determine its length.
    FILECanonicalizePath(realPath);

    if (strlen(realPath) >= MAX_LONGPATH)
    {
        WARN("UnixPathName is larger than MAX_LONGPATH (%d)!\n", MAX_LONGPATH);
        dwLastError = ERROR_FILENAME_EXCED_RANGE;
        goto done;
    }

    if ( mkdir(realPath, mode) != 0 )
    {
        TRACE("Creation of directory [%s] was unsuccessful, errno = %d.\n",
              UnixPathName, errno);

        switch( errno )
        {
        case ENOTDIR:
            /* FALL THROUGH */
        case ENOENT:
            FILEGetProperNotFoundError( realPath, &dwLastError );
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
        TRACE("Creation of directory [%s] was successful.\n", UnixPathName);
        bRet = TRUE;
    }

done:
    if( dwLastError )
    {
        SetLastError( dwLastError );
    }
    PAL_free( UnixPathName );
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
    LPSTR UnixPathName = NULL;

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
    if (strlen(lpPathName) >= MAX_LONGPATH)
    {
        WARN("Path/directory name longer than MAX_LONGPATH characters\n");
        dwLastError = ERROR_FILENAME_EXCED_RANGE;
        goto done;
    }

    UnixPathName = PAL__strdup(lpPathName);
    if (UnixPathName == NULL )
    {
        ERROR("PAL__strdup() failed\n");
        dwLastError = ERROR_NOT_ENOUGH_MEMORY;
        goto done;
    }
    FILEDosToUnixPathA( UnixPathName );

    TRACE("Attempting to open Unix dir [%s]\n", UnixPathName);
    result = chdir(UnixPathName);

    if ( result == 0 )
    {
        bRet = TRUE;
    }
    else
    {
        if ( errno == ENOTDIR || errno == ENOENT )
        {
            struct stat stat_data;

            if ( stat( UnixPathName, &stat_data) == 0 &&
                 (stat_data.st_mode & S_IFMT) == S_IFREG )
            {
                /* Not a directory, it is a file. */
                dwLastError = ERROR_DIRECTORY;
            }
            else
            {
                FILEGetProperNotFoundError( UnixPathName, &dwLastError );
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

    if(UnixPathName != NULL)
    {
        PAL_free( UnixPathName );
    }

    LOGEXIT("SetCurrentDirectoryA returns BOOL %d\n", bRet);
    PERF_EXIT(SetCurrentDirectoryA);
    return bRet;
}
