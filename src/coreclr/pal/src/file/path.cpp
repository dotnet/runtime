// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*++



Module Name:

    path.c

Abstract:

    Implementation of all functions related to path support

Revision History:



--*/

#include "pal/thread.hpp"
#include "pal/palinternal.h"
#include "pal/dbgmsg.h"
#include "pal/file.h"
#include "pal/malloc.hpp"
#include "pal/stackstring.hpp"

#include <errno.h>

#include <unistd.h>
#include <stdlib.h>

SET_DEFAULT_DEBUG_CHANNEL(FILE);


// In safemath.h, Template SafeInt uses macro _ASSERTE, which need to use variable
// defdbgchan defined by SET_DEFAULT_DEBUG_CHANNEL. Therefore, the include statement
// should be placed after the SET_DEFAULT_DEBUG_CHANNEL(FILE)
#include <safemath.h>

int MaxWCharToAcpLengthRatio = 3;
/*++
Function:
  GetFullPathNameA

See MSDN doc.
--*/
DWORD
PALAPI
GetFullPathNameA(
     IN LPCSTR lpFileName,
     IN DWORD nBufferLength,
     OUT LPSTR lpBuffer,
     OUT LPSTR *lpFilePart)
{
    DWORD  nReqPathLen, nRet = 0;
    PathCharString unixPath;
    LPSTR unixPathBuf;
    BOOL fullPath = FALSE;

    PERF_ENTRY(GetFullPathNameA);
    ENTRY("GetFullPathNameA(lpFileName=%p (%s), nBufferLength=%u, lpBuffer=%p, "
          "lpFilePart=%p)\n",
          lpFileName?lpFileName:"NULL",
          lpFileName?lpFileName:"NULL", nBufferLength, lpBuffer, lpFilePart);

    if(NULL == lpFileName)
    {
        WARN("lpFileName is NULL\n");
        SetLastError(ERROR_INVALID_PARAMETER);
        goto done;
    }

    /* find out if lpFileName is a partial or full path */
    if ('/' == *lpFileName)
    {
        fullPath = TRUE;
    }

    if(fullPath)
    {
        if( !unixPath.Set(lpFileName, strlen(lpFileName)))
        {
            ERROR("Set() failed;\n");
            SetLastError(ERROR_NOT_ENOUGH_MEMORY);
            goto done;
        }
    }
    else
    {

        /* build full path */
        if(!GetCurrentDirectoryA(unixPath))
        {
            /* no reason for this to fail now... */
            ASSERT("GetCurrentDirectoryA() failed! lasterror is %#xd\n",
                   GetLastError());
            SetLastError(ERROR_INTERNAL_ERROR);
            goto done;
        }

        if (!unixPath.Append("/", 1) ||
            !unixPath.Append(lpFileName,strlen(lpFileName))
           )
        {
            ERROR("Append failed!\n");
            SetLastError(ERROR_NOT_ENOUGH_MEMORY);
            goto done;
        }

    }

    unixPathBuf = unixPath.OpenStringBuffer(unixPath.GetCount());

    /* now we can canonicalize this */
    FILECanonicalizePath(unixPathBuf);

    /* at last, we can figure out how long this path is */
    nReqPathLen = strlen(unixPathBuf);

    unixPath.CloseBuffer(nReqPathLen);
    nReqPathLen++;
    if(nBufferLength < nReqPathLen)
    {
        TRACE("reporting insufficient buffer : minimum is %d, caller "
              "provided %d\n", nReqPathLen, nBufferLength);
        nRet = nReqPathLen;
        goto done;
    }

    nRet = nReqPathLen-1;
    strcpy_s(lpBuffer, nBufferLength, unixPath);

    /* locate the filename component if caller cares */
    if(lpFilePart)
    {
        *lpFilePart = strrchr(lpBuffer, '/');

        if (*lpFilePart == NULL)
        {
            ASSERT("Not able to find '/' in the full path.\n");
            SetLastError( ERROR_INTERNAL_ERROR );
            nRet = 0;
            goto done;
        }
        else
        {
            (*lpFilePart)++;
        }
    }

done:
    LOGEXIT("GetFullPathNameA returns DWORD %u\n", nRet);
    PERF_EXIT(GetFullPathNameA);
    return nRet;
}


/*++
Function:
  GetFullPathNameW

See MSDN doc.
--*/
DWORD
PALAPI
GetFullPathNameW(
     IN LPCWSTR lpFileName,
     IN DWORD nBufferLength,
     OUT LPWSTR lpBuffer,
     OUT LPWSTR *lpFilePart)
{
    LPSTR fileNameA;
    CHAR * bufferA;
    size_t bufferASize = 0;
    PathCharString bufferAPS;
    LPSTR lpFilePartA;
    int   fileNameLength;
    int   srcSize;
    DWORD length;
    DWORD nRet = 0;

    PERF_ENTRY(GetFullPathNameW);
    ENTRY("GetFullPathNameW(lpFileName=%p (%S), nBufferLength=%u, lpBuffer=%p"
          ", lpFilePart=%p)\n",
          lpFileName?lpFileName:W16_NULLSTRING,
          lpFileName?lpFileName:W16_NULLSTRING, nBufferLength,
          lpBuffer, lpFilePart);


    fileNameLength = WideCharToMultiByte(CP_ACP, 0, lpFileName,
                                         -1, NULL, 0, NULL, NULL);
    if (fileNameLength == 0)
    {
        /* Couldn't convert to ANSI. That's odd. */
        SetLastError(ERROR_INVALID_PARAMETER);
        goto done;
    }
    else
    {
        fileNameA = static_cast<LPSTR>(alloca(fileNameLength));
    }

    /* Now convert lpFileName to ANSI. */
    srcSize = WideCharToMultiByte (CP_ACP, 0, lpFileName,
                                   -1, fileNameA, fileNameLength,
                                   NULL, NULL );
    if( srcSize == 0 )
    {
        DWORD dwLastError = GetLastError();
        ASSERT("WideCharToMultiByte failure! error is %d\n", dwLastError);
        SetLastError(ERROR_INVALID_PARAMETER);
        goto done;
    }

    bufferASize = nBufferLength * MaxWCharToAcpLengthRatio;
    bufferA = bufferAPS.OpenStringBuffer(bufferASize);
    if (NULL == bufferA)
    {
        SetLastError(ERROR_NOT_ENOUGH_MEMORY);
        goto done;
    }
    length = GetFullPathNameA(fileNameA, bufferASize, bufferA, &lpFilePartA);
    bufferAPS.CloseBuffer(length);

    if (length == 0 || length > bufferASize)
    {
        /* Last error is set by GetFullPathNameA */
        nRet = length;
        goto done;
    }

    /* Convert back to Unicode the result */
    nRet = MultiByteToWideChar( CP_ACP, 0, bufferA, -1,
                                lpBuffer, nBufferLength );

    if (nRet == 0)
    {
        if ( GetLastError() == ERROR_INSUFFICIENT_BUFFER )
        {
            /* get the required length */
            nRet = MultiByteToWideChar( CP_ACP, 0, bufferA, -1,
                                        NULL, 0 );
            SetLastError(ERROR_BUFFER_OVERFLOW);
        }

        goto done;
    }

    /* MultiByteToWideChar counts the trailing NULL, but
       GetFullPathName does not. */
    nRet--;

    /* now set lpFilePart */
    if (lpFilePart != NULL)
    {
        *lpFilePart = lpBuffer;
        *lpFilePart += MultiByteToWideChar( CP_ACP, 0, bufferA,
                                            lpFilePartA - bufferA, NULL, 0);
    }

done:
    LOGEXIT("GetFullPathNameW returns DWORD %u\n", nRet);
    PERF_EXIT(GetFullPathNameW);
    return nRet;
}


/*++
Function:
  GetTempPathA

See MSDN.

Notes:
    On Windows, the temp path is determined by the following steps:
    1. The value of the "TMP" environment variable, or if it doesn't exist,
    2. The value of the "TEMP" environment variable, or if it doesn't exist,
    3. The Windows directory.

    On Unix, we follow in spirit:
    1. The value of the "TMPDIR" environment variable, or if it doesn't exist,
    2. The /tmp directory.
    This is the same approach employed by mktemp.

--*/
DWORD
PALAPI
GetTempPathA(
	     IN DWORD nBufferLength,
	     OUT LPSTR lpBuffer)
{
    DWORD dwPathLen = 0;

    PERF_ENTRY(GetTempPathA);
    ENTRY("GetTempPathA(nBufferLength=%u, lpBuffer=%p)\n",
          nBufferLength, lpBuffer);

    if ( !lpBuffer )
    {
        ERROR( "lpBuffer was not a valid pointer.\n" )
        SetLastError( ERROR_INVALID_PARAMETER );
        LOGEXIT("GetTempPathA returns DWORD %u\n", dwPathLen);
        PERF_EXIT(GetTempPathA);
        return 0;
    }

    /* Try the TMPDIR environment variable. This is the same env var checked by mktemp. */
    dwPathLen = GetEnvironmentVariableA("TMPDIR", lpBuffer, nBufferLength);
    if (dwPathLen > 0)
    {
        /* The env var existed. dwPathLen will be the length without null termination
         * if the entire value was successfully retrieved, or it'll be the length
         * required to store the value with null termination.
         */
        if (dwPathLen < nBufferLength)
        {
            /* The environment variable fit in the buffer. Make sure it ends with '/'. */
            if (lpBuffer[dwPathLen - 1] != '/')
            {
                /* If adding the slash would still fit in our provided buffer, do it.  Otherwise,
                 * let the caller know how much space would be needed.
                 */
                if (dwPathLen + 2 <= nBufferLength)
                {
                    lpBuffer[dwPathLen++] = '/';
                    lpBuffer[dwPathLen] = '\0';
                }
                else
                {
                    dwPathLen += 2;
                }
            }
        }
        else /* dwPathLen >= nBufferLength */
        {
            /* The value is too long for the supplied buffer.  dwPathLen will now be the
             * length required to hold the value, but we don't know whether that value
             * is going to be '/' terminated.  Since we'll need enough space for the '/', and since
             * a caller would assume that the dwPathLen we return will be sufficient,
             * we make sure to account for it in dwPathLen even if that means we end up saying
             * one more byte of space is needed than actually is.
             */
            dwPathLen++;
        }
    }
    else /* env var not found or was empty */
    {
        /* no luck, use /tmp/ or /data/local/tmp on Android */
        const char *defaultDir = TEMP_DIRECTORY_PATH;
        size_t defaultDirLen = strlen(defaultDir);
        if (defaultDirLen < nBufferLength)
        {
            dwPathLen = defaultDirLen;
            strcpy_s(lpBuffer, nBufferLength, defaultDir);
        }
        else
        {
            /* get the required length */
            dwPathLen = defaultDirLen + 1;
        }
    }

    if ( dwPathLen >= nBufferLength )
    {
        ERROR("Buffer is too small, need space for %d characters including null termination\n", dwPathLen);
        SetLastError( ERROR_INSUFFICIENT_BUFFER );
    }

    LOGEXIT("GetTempPathA returns DWORD %u\n", dwPathLen);
    PERF_EXIT(GetTempPathA);
    return dwPathLen;
}

/*++
Function:
  GetTempPathW

See MSDN.
See also the comment for GetTempPathA.
--*/
DWORD
PALAPI
GetTempPathW(
	     IN DWORD nBufferLength,
	     OUT LPWSTR lpBuffer)
{
    PERF_ENTRY(GetTempPathW);
    ENTRY("GetTempPathW(nBufferLength=%u, lpBuffer=%p)\n",
          nBufferLength, lpBuffer);

    if (!lpBuffer)
    {
        ERROR("lpBuffer was not a valid pointer.\n")
        SetLastError(ERROR_INVALID_PARAMETER);
        LOGEXIT("GetTempPathW returns DWORD 0\n");
        PERF_EXIT(GetTempPathW);
        return 0;
    }

    char* tempBuffer = (char*)alloca(nBufferLength > 0 ? nBufferLength : 1);
    DWORD dwRetVal = GetTempPathA( nBufferLength, tempBuffer );

    if ( dwRetVal >= nBufferLength )
    {
        ERROR( "lpBuffer was not large enough.\n" )
        SetLastError( ERROR_INSUFFICIENT_BUFFER );
        *lpBuffer = '\0';
    }
    else if ( dwRetVal != 0 )
    {
        /* Convert to wide. */
        if ( 0 == MultiByteToWideChar( CP_ACP, 0, tempBuffer, -1,
                                       lpBuffer, dwRetVal + 1 ) )
        {
            ASSERT( "An error occurred while converting the string to wide.\n" );
            SetLastError( ERROR_INTERNAL_ERROR );
            dwRetVal = 0;
        }
    }
    else
    {
        ERROR( "The function failed.\n" );
        *lpBuffer = '\0';
    }

    LOGEXIT("GetTempPathW returns DWORD %u\n", dwRetVal );
    PERF_EXIT(GetTempPathW);
    return dwRetVal;
}


/*++
Function:
  FILEGetDirectoryFromFullPathA

Parse the given path. If it contains a directory part and a file part,
put the directory part into the supplied buffer, and return the number of
characters written to the buffer. If the buffer is not large enough,
return the required size of the buffer including the NULL character. If
there is no directory part in the path, return 0.
--*/
DWORD FILEGetDirectoryFromFullPathA( LPCSTR lpFullPath,
                     DWORD  nBufferLength,
                     LPSTR  lpBuffer )
{
    size_t full_len, dir_len, i;
    LPCSTR lpDirEnd;
    DWORD  dwRetLength;

    full_len = strlen( lpFullPath );

    /* look for the first path separator backwards */
    lpDirEnd = lpFullPath + full_len - 1;
    while( lpDirEnd >= lpFullPath && *lpDirEnd != '/')
    --lpDirEnd;

    dir_len = lpDirEnd - lpFullPath + 1; /* +1 for fencepost */

    if ( dir_len <= 0 )
    {
        dwRetLength = 0;
    }
    else if (dir_len >= nBufferLength)
    {
        dwRetLength = dir_len + 1; /* +1 for NULL char */
    }
    else
    {
    /* put the directory into the buffer, including 1 or more
       trailing path separators */
    for( i = 0; i < dir_len; ++i )
        *(lpBuffer + i) = *(lpFullPath + i);

    *(lpBuffer + i) = '\0';

    dwRetLength = dir_len;
    }

    return( dwRetLength );
}

/*++
FILECanonicalizePath
    Removes all instances of '/./', '/../' and '//' from an absolute path.

Parameters:
    LPSTR lpUnixPath : absolute path to modify, in Unix format

(no return value)

Notes :
-behavior is undefined if path is not absolute
-the order of steps *is* important: /one/./../two would give /one/two
 instead of /two if step 3 was done before step 2
-reason for this function is that GetFullPathName can't use realpath(), since
 realpath() requires the given path to be valid and GetFullPathName does not.
--*/
void FILECanonicalizePath(LPSTR lpUnixPath)
{
    LPSTR slashslashptr;
    LPSTR dotdotptr;
    LPSTR slashdotptr;
    LPSTR slashptr;

    /* step 1 : replace '//' sequences by a single '/' */

    slashslashptr = lpUnixPath;
    while(1)
    {
        slashslashptr = strstr(slashslashptr,"//");
        if(NULL == slashslashptr)
        {
            break;
        }
        /* remove extra '/' */
        TRACE("stripping '//' from %s\n", lpUnixPath);
        memmove(slashslashptr,slashslashptr+1,strlen(slashslashptr+1)+1);
    }

    /* step 2 : replace '/./' sequences by a single '/' */

    slashdotptr = lpUnixPath;
    while(1)
    {
        slashdotptr = strstr(slashdotptr,"/./");
        if(NULL == slashdotptr)
        {
            break;
        }
        /* strip the extra '/.' */
        TRACE("removing '/./' sequence from %s\n", lpUnixPath);
        memmove(slashdotptr,slashdotptr+2,strlen(slashdotptr+2)+1);
    }

    /* step 3 : replace '/<name>/../' sequences by a single '/' */

    while(1)
    {
        dotdotptr = strstr(lpUnixPath,"/../");
        if(NULL == dotdotptr)
        {
            break;
        }
        if(dotdotptr == lpUnixPath)
        {
            /* special case : '/../' at the beginning of the path are replaced
               by a single '/' */
            TRACE("stripping leading '/../' from %s\n", lpUnixPath);
            memmove(lpUnixPath, lpUnixPath+3,strlen(lpUnixPath+3)+1);
            continue;
        }

        /* null-terminate the string before the '/../', so that strrchr will
           start looking right before it */
        *dotdotptr = '\0';
        slashptr = strrchr(lpUnixPath,'/');
        if(NULL == slashptr)
        {
            /* this happens if this function was called with a relative path.
               don't do that.  */
            ASSERT("can't find leading '/' before '/../ sequence\n");
            break;
        }
        TRACE("removing '/<dir>/../' sequence from %s\n", lpUnixPath);
        memmove(slashptr,dotdotptr+3,strlen(dotdotptr+3)+1);
    }

    /* step 4 : remove a trailing '/..' */

    dotdotptr = strstr(lpUnixPath,"/..");
    if(dotdotptr == lpUnixPath)
    {
        /* if the full path is simply '/..', replace it by '/' */
        lpUnixPath[1] = '\0';
    }
    else if(NULL != dotdotptr && '\0' == dotdotptr[3])
    {
        *dotdotptr = '\0';
        slashptr = strrchr(lpUnixPath,'/');
        if(NULL != slashptr)
        {
            /* make sure the last slash isn't the root */
            if (slashptr == lpUnixPath)
            {
                lpUnixPath[1] = '\0';
            }
            else
            {
                *slashptr = '\0';
            }
        }
    }

    /* step 5 : remove a trailing '/.' */

    slashdotptr = strstr(lpUnixPath,"/.");
    if (slashdotptr != NULL && slashdotptr[2] == '\0')
    {
        if(slashdotptr == lpUnixPath)
        {
            // if the full path is simply '/.', replace it by '/' */
            lpUnixPath[1] = '\0';
        }
        else
        {
            *slashdotptr = '\0';
        }
    }
}


/*++
Function:
  SearchPathW

See MSDN doc.

PAL-specific notes :
-lpPath must be non-NULL; path delimiters are platform-dependent (':' for Unix)
-lpFileName must be non-NULL, may be an absolute path
-lpExtension must be NULL
-lpFilePart (if non-NULL) doesn't need to be used (but we do)
--*/
DWORD
PALAPI
SearchPathW(
    IN LPCWSTR lpPath,
    IN LPCWSTR lpFileName,
    IN LPCWSTR lpExtension,
    IN DWORD nBufferLength,
    OUT LPWSTR lpBuffer,
    OUT LPWSTR *lpFilePart
    )
{
    DWORD nRet = 0;
    WCHAR * FullPath;
    size_t FullPathLength = 0;
    PathWCharString FullPathPS;
    LPCWSTR pPathStart;
    LPCWSTR pPathEnd;
    size_t PathLength;
    size_t FileNameLength;
    DWORD dw;
    DWORD length;
    char * AnsiPath;
    PathCharString AnsiPathPS;
    size_t CanonicalPathLength;
    int canonical_size;
    WCHAR * CanonicalPath;
    PathWCharString CanonicalPathPS;

    PERF_ENTRY(SearchPathW);
    ENTRY("SearchPathW(lpPath=%p (%S), lpFileName=%p (%S), lpExtension=%p, "
          "nBufferLength=%u, lpBuffer=%p, lpFilePart=%p)\n",
	  lpPath,
	  lpPath, lpFileName, lpFileName, lpExtension, nBufferLength, lpBuffer,
          lpFilePart);

    /* validate parameters */

    if(NULL == lpPath)
    {
        ASSERT("lpPath may not be NULL\n");
        SetLastError(ERROR_INVALID_PARAMETER);
        goto done;
    }
    if(NULL == lpFileName)
    {
        ASSERT("lpFileName may not be NULL\n");
        SetLastError(ERROR_INVALID_PARAMETER);
        goto done;
    }
    if(NULL != lpExtension)
    {
        ASSERT("lpExtension must be NULL, is %p instead\n", lpExtension);
        SetLastError(ERROR_INVALID_PARAMETER);
        goto done;
    }

    /* special case : if file name contains absolute path, don't search the
       provided path */
    if(L'/' == lpFileName[0])
    {
        /* Canonicalize the path to deal with back-to-back '/', etc. */
        length = MAX_LONGPATH; //Use it for first try
        CanonicalPath = CanonicalPathPS.OpenStringBuffer(length);
        if (NULL == CanonicalPath)
        {
            SetLastError(ERROR_NOT_ENOUGH_MEMORY);
            goto done;
        }
        dw = GetFullPathNameW(lpFileName, length+1, CanonicalPath, NULL);
        CanonicalPathPS.CloseBuffer(dw);
        if (length+1 < dw)
        {
            CanonicalPath = CanonicalPathPS.OpenStringBuffer(dw-1);
            if (NULL == CanonicalPath)
            {
                SetLastError(ERROR_NOT_ENOUGH_MEMORY);
                goto done;
            }
            dw = GetFullPathNameW(lpFileName, dw, CanonicalPath, NULL);
            CanonicalPathPS.CloseBuffer(dw);
        }

        if (dw == 0)
        {
            WARN("couldn't canonicalize path <%S>, error is %#x. failing.\n",
                 lpPath, GetLastError());
            SetLastError(ERROR_INVALID_PARAMETER);
            goto done;
        }

        /* see if the file exists */
        CanonicalPathLength = (PAL_wcslen(CanonicalPath)+1) * MaxWCharToAcpLengthRatio;
        AnsiPath = AnsiPathPS.OpenStringBuffer(CanonicalPathLength);
        if (NULL == AnsiPath)
        {
            SetLastError(ERROR_NOT_ENOUGH_MEMORY);
            goto done;
        }
	    canonical_size = WideCharToMultiByte(CP_ACP, 0, CanonicalPath, -1,
			    AnsiPath, CanonicalPathLength, NULL, NULL);
	    AnsiPathPS.CloseBuffer(canonical_size);

        if(0 == access(AnsiPath, F_OK))
        {
            /* found it */
            nRet = dw;
        }
    }
    else
    {
        LPCWSTR pNextPath;

        pNextPath = lpPath;

        FileNameLength = PAL_wcslen(lpFileName);

        while (*pNextPath)
        {
            pPathStart = pNextPath;

            /* get a pointer to the end of the first path in pPathStart */
            pPathEnd = PAL_wcschr(pPathStart, ':');
            if (!pPathEnd)
            {
                pPathEnd = pPathStart + PAL_wcslen(pPathStart);
                /* we want to break out of the loop after this pass, so let
                   *pNextPath be '\0' */
                pNextPath = pPathEnd;
            }
            else
            {
                /* point to the next component in the path string */
                pNextPath = pPathEnd+1;
            }

            PathLength = pPathEnd-pPathStart;

            if(0 == PathLength)
            {
                /* empty component : there were 2 consecutive ':' */
                continue;
            }

            /* Construct a pathname by concatenating one path from lpPath, '/'
               and lpFileName */
            FullPathLength = PathLength + FileNameLength;
            FullPath = FullPathPS.OpenStringBuffer(FullPathLength+1);
            if (NULL == FullPath)
            {
                SetLastError(ERROR_NOT_ENOUGH_MEMORY);
                goto done;
            }
            memcpy(FullPath, pPathStart, PathLength*sizeof(WCHAR));
            FullPath[PathLength] = '/';
            PAL_wcscpy(&FullPath[PathLength+1], lpFileName);

            FullPathPS.CloseBuffer(FullPathLength+1);

            /* Canonicalize the path to deal with back-to-back '/', etc. */
            length = MAX_LONGPATH; //Use it for first try
            CanonicalPath = CanonicalPathPS.OpenStringBuffer(length);
            if (NULL == CanonicalPath)
            {
                SetLastError(ERROR_NOT_ENOUGH_MEMORY);
                goto done;
            }
            dw = GetFullPathNameW(FullPath, length+1,
                                  CanonicalPath, NULL);
            CanonicalPathPS.CloseBuffer(dw);

            if (length+1 < dw)
            {
                CanonicalPath = CanonicalPathPS.OpenStringBuffer(dw-1);
                if (NULL == CanonicalPath)
                {
                    SetLastError(ERROR_NOT_ENOUGH_MEMORY);
                    goto done;
                }
                dw = GetFullPathNameW(FullPath, dw, CanonicalPath, NULL);
                CanonicalPathPS.CloseBuffer(dw);
            }

            if (dw == 0)
            {
                /* Call failed - possibly low memory.  Skip the path */
                WARN("couldn't canonicalize path <%S>, error is %#x. "
                     "skipping it\n", FullPath, GetLastError());
                continue;
            }

            /* see if the file exists */
            CanonicalPathLength = (PAL_wcslen(CanonicalPath)+1) * MaxWCharToAcpLengthRatio;
            AnsiPath = AnsiPathPS.OpenStringBuffer(CanonicalPathLength);
            if (NULL == AnsiPath)
            {
                SetLastError(ERROR_NOT_ENOUGH_MEMORY);
                goto done;
            }
            canonical_size = WideCharToMultiByte(CP_ACP, 0, CanonicalPath, -1,
                                AnsiPath, CanonicalPathLength, NULL, NULL);
            AnsiPathPS.CloseBuffer(canonical_size);

            if(0 == access(AnsiPath, F_OK))
            {
                /* found it */
                nRet = dw;
                break;
            }
        }
    }

    if (nRet == 0)
    {
       /* file not found anywhere; say so. in Windows, this always seems to say
          FILE_NOT_FOUND, even if path doesn't exist */
       SetLastError(ERROR_FILE_NOT_FOUND);
    }
    else
    {
        /* find out the required buffer size, copy path to buffer if it's
           large enough */
        nRet = PAL_wcslen(CanonicalPath)+1;
        if(nRet <= nBufferLength)
        {
            if(NULL == lpBuffer)
            {
                /* Windows merily crashes here, but let's not */
                ERROR("caller told us buffer size was %d, but buffer is NULL\n",
                      nBufferLength);
                SetLastError(ERROR_INVALID_PARAMETER);
                nRet = 0;
                goto done;
            }
            PAL_wcscpy(lpBuffer, CanonicalPath);

            /* don't include the null-terminator in the count if buffer was
               large enough */
            nRet--;

            if(NULL != lpFilePart)
            {
                *lpFilePart = PAL_wcsrchr(lpBuffer, '/');
                if(NULL == *lpFilePart)
                {
                    ASSERT("no '/' in full path!\n");
                }
                else
                {
                    /* point to character after last '/' */
                    (*lpFilePart)++;
                }
            }
        }
    }
done:
    LOGEXIT("SearchPathW returns DWORD %u\n", nRet);
    PERF_EXIT(SearchPathW);
    return nRet;
}
