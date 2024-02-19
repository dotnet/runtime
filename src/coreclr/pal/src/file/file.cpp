// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*++

Module Name:

    file.cpp

Abstract:

    Implementation of the file WIN API for the PAL

--*/

#include "pal/dbgmsg.h"
SET_DEFAULT_DEBUG_CHANNEL(FILE); // some headers have code with asserts, so do this first

#include "pal/thread.hpp"
#include "pal/file.hpp"
#include "pal/malloc.hpp"
#include "pal/stackstring.hpp"

#include "pal/palinternal.h"
#include "pal/file.h"
#include "pal/filetime.h"
#include "pal/utils.h"

#include <time.h>
#include <stdio.h>
#include <sys/file.h>
#include <sys/types.h>
#include <sys/stat.h>
#include <sys/param.h>
#include <sys/mount.h>
#include <errno.h>
#include <limits.h>

using namespace CorUnix;

int MaxWCharToAcpLengthFactor = 3;

PAL_ERROR
InternalSetFilePointerForUnixFd(
    int iUnixFd,
    LONG lDistanceToMove,
    PLONG lpDistanceToMoveHigh,
    DWORD dwMoveMethod,
    PLONG lpNewFilePointerLow
    );

void
CFileProcessLocalDataCleanupRoutine(
    CPalThread *pThread,
    IPalObject *pObjectToCleanup
    );

void
FileCleanupRoutine(
    CPalThread *pThread,
    IPalObject *pObjectToCleanup,
    bool fShutdown,
    bool fCleanupSharedState
    );

CObjectType CorUnix::otFile(
                otiFile,
                FileCleanupRoutine,
                NULL,   // No initialization routine
                0,      // No immutable data
                NULL,   // No immutable data copy routine
                NULL,   // No immutable data cleanup routine
                sizeof(CFileProcessLocalData),
                CFileProcessLocalDataCleanupRoutine,
                0,      // No shared data
                GENERIC_READ|GENERIC_WRITE,  // Ignored -- no Win32 object security support
                CObjectType::SecuritySupported,
                CObjectType::OSPersistedSecurityInfo,
                CObjectType::UnnamedObject,
                CObjectType::LocalDuplicationOnly,
                CObjectType::UnwaitableObject,
                CObjectType::SignalingNotApplicable,
                CObjectType::ThreadReleaseNotApplicable,
                CObjectType::OwnershipNotApplicable
                );

CAllowedObjectTypes CorUnix::aotFile(otiFile);

void
CFileProcessLocalDataCleanupRoutine(
    CPalThread *pThread,
    IPalObject *pObjectToCleanup
    )
{
    PAL_ERROR palError;
    CFileProcessLocalData *pLocalData = NULL;
    IDataLock *pLocalDataLock = NULL;

    palError = pObjectToCleanup->GetProcessLocalData(
        pThread,
        ReadLock,
        &pLocalDataLock,
        reinterpret_cast<void**>(&pLocalData)
        );

    if (NO_ERROR != palError)
    {
        ASSERT("Unable to obtain data to cleanup file object");
        return;
    }

    free(pLocalData->unix_filename);

    pLocalDataLock->ReleaseLock(pThread, FALSE);
}

void
FileCleanupRoutine(
    CPalThread *pThread,
    IPalObject *pObjectToCleanup,
    bool fShutdown,
    bool fCleanupSharedState
    )
{
    PAL_ERROR palError;
    CFileProcessLocalData *pLocalData = NULL;
    IDataLock *pLocalDataLock = NULL;

    palError = pObjectToCleanup->GetProcessLocalData(
        pThread,
        ReadLock,
        &pLocalDataLock,
        reinterpret_cast<void**>(&pLocalData)
        );

    if (NO_ERROR != palError)
    {
        ASSERT("Unable to obtain data to cleanup file object");
        return;
    }

    if (!fShutdown && -1 != pLocalData->unix_fd)
    {
        close(pLocalData->unix_fd);
    }

    pLocalDataLock->ReleaseLock(pThread, FALSE);
}

typedef enum
{
  PIID_STDIN_HANDLE,
  PIID_STDOUT_HANDLE,
  PIID_STDERR_HANDLE
} PROCINFO_ID;

#define PAL_LEGAL_FLAGS_ATTRIBS (FILE_ATTRIBUTE_NORMAL| \
                                 FILE_FLAG_SEQUENTIAL_SCAN| \
                                 FILE_FLAG_WRITE_THROUGH| \
                                 FILE_FLAG_NO_BUFFERING| \
                                 FILE_FLAG_RANDOM_ACCESS| \
                                 FILE_FLAG_BACKUP_SEMANTICS)

/* Static global. The init function must be called
before any other functions and if it is not successful,
no other functions should be done. */
static HANDLE pStdIn = INVALID_HANDLE_VALUE;
static HANDLE pStdOut = INVALID_HANDLE_VALUE;
static HANDLE pStdErr = INVALID_HANDLE_VALUE;

/*++
Function :
    FILEGetProperNotFoundError

Returns the proper error code, based on the
Windows behavior.

    IN LPSTR lpPath - The path to check.
    LPDWORD lpErrorCode - The error to set.
*/
void FILEGetProperNotFoundError( LPCSTR lpPath, LPDWORD lpErrorCode )
{
    struct stat stat_data;
    LPSTR lpDupedPath = NULL;
    LPSTR lpLastPathSeparator = NULL;

    TRACE( "FILEGetProperNotFoundError( %s )\n", lpPath?lpPath:"(null)" );

    if ( !lpErrorCode )
    {
        ASSERT( "lpErrorCode has to be valid\n" );
        return;
    }

    if ( NULL == ( lpDupedPath = strdup(lpPath) ) )
    {
        ERROR( "strdup() failed!\n" );
        *lpErrorCode = ERROR_NOT_ENOUGH_MEMORY;
        return;
    }

    /* Determine whether it's a file not found or path not found. */
    lpLastPathSeparator = strrchr( lpDupedPath, '/');
    if ( lpLastPathSeparator != NULL )
    {
        *lpLastPathSeparator = '\0';

        /* If the last path component is a directory,
           we return file not found. If it's a file or
           doesn't exist, we return path not found. */
        if ( '\0' == *lpDupedPath ||
             ( stat( lpDupedPath, &stat_data ) == 0 &&
             ( stat_data.st_mode & S_IFMT ) == S_IFDIR ) )
        {
            TRACE( "ERROR_FILE_NOT_FOUND\n" );
            *lpErrorCode = ERROR_FILE_NOT_FOUND;
        }
        else
        {
            TRACE( "ERROR_PATH_NOT_FOUND\n" );
            *lpErrorCode = ERROR_PATH_NOT_FOUND;
        }
    }
    else
    {
        TRACE( "ERROR_FILE_NOT_FOUND\n" );
        *lpErrorCode = ERROR_FILE_NOT_FOUND;
    }

    free(lpDupedPath);
    lpDupedPath = NULL;
    TRACE( "FILEGetProperNotFoundError returning TRUE\n" );
    return;
}

/*++
Function :
    FILEGetLastErrorFromErrnoAndFilename

Returns the proper error code for errno, or, if errno is ENOENT,
based on the Windows behavior for nonexistent filenames.

    IN LPSTR lpPath - The path to check.
*/
PAL_ERROR FILEGetLastErrorFromErrnoAndFilename(LPCSTR lpPath)
{
    PAL_ERROR palError;
    if (ENOENT == errno)
    {
        FILEGetProperNotFoundError(lpPath, &palError);
    }
    else
    {
        palError = FILEGetLastErrorFromErrno();
    }
    return palError;
}

BOOL
CorUnix::RealPathHelper(LPCSTR lpUnixPath, PathCharString& lpBuffer)
{
    StringHolder lpRealPath;
    lpRealPath = realpath(lpUnixPath, NULL);
    if (lpRealPath.IsNull())
    {
        return FALSE;
    }

    lpBuffer.Set(lpRealPath, strlen(lpRealPath));
    return TRUE;
}
/*++
InternalCanonicalizeRealPath
    Wraps realpath() to hide platform differences. See the man page for
    realpath(3) for details of how realpath() works.

    On systems on which realpath() allows the last path component to not
    exist, this is a straight thunk through to realpath(). On other
    systems, we remove the last path component, then call realpath().

--*/
PAL_ERROR
CorUnix::InternalCanonicalizeRealPath(LPCSTR lpUnixPath, PathCharString& lpBuffer)
{
    PAL_ERROR palError = NO_ERROR;

#if !REALPATH_SUPPORTS_NONEXISTENT_FILES
    StringHolder lpExistingPath;
    LPSTR pchSeparator = NULL;
    LPSTR lpFilename = NULL;
#endif // !REALPATH_SUPPORTS_NONEXISTENT_FILES

    if (lpUnixPath == NULL)
    {
        ERROR ("Invalid argument to InternalCanonicalizeRealPath\n");
        palError = ERROR_INVALID_PARAMETER;
        goto LExit;
    }

#if REALPATH_SUPPORTS_NONEXISTENT_FILES
    RealPathHelper(lpUnixPath, lpBuffer);
#else   // !REALPATH_SUPPORTS_NONEXISTENT_FILES

    lpExistingPath = strdup(lpUnixPath);
    if (lpExistingPath.IsNull())
    {
        ERROR ("strdup failed with error %d\n", errno);
        palError = ERROR_NOT_ENOUGH_MEMORY;
        goto LExit;
    }

    pchSeparator = strrchr(lpExistingPath, '/');
    if (pchSeparator == NULL)
    {
        PathCharString pszCwdBuffer;

        if (GetCurrentDirectoryA(pszCwdBuffer)== 0)
        {
            WARN("getcwd(NULL) failed with error %d\n", errno);
            palError = DIRGetLastErrorFromErrno();
            goto LExit;
        }

        if (!RealPathHelper(pszCwdBuffer, lpBuffer))
        {
            WARN("realpath() failed with error %d\n", errno);
            palError = FILEGetLastErrorFromErrno();
            goto LExit;
        }
        lpFilename = lpExistingPath;
    }
    else
    {
#if defined(HOST_AMD64)
        bool fSetFilename = true;
        // Since realpath implementation cannot handle inexistent filenames,
        // check if we are going to truncate the "/" corresponding to the
        // root folder (e.g. case of "/Volumes"). If so:
        //
        // 1) Set the separator to point to the NULL terminator of the specified
        //    file/folder name.
        //
        // 2) Null terminate lpBuffer
        //
        // 3) Since there is no explicit filename component in lpExistingPath (as
        //    we only have "/" corresponding to the root), set lpFilename to NULL,
        //    alongwith a flag indicating that it has already been set.
        if (pchSeparator == lpExistingPath)
        {
            pchSeparator = lpExistingPath+strlen(lpExistingPath);

            // Set the lpBuffer to NULL
            lpBuffer.Clear();
            lpFilename = NULL;
            fSetFilename = false;
        }
        else
#endif // defined(HOST_AMD64)
            *pchSeparator = '\0';

        if (!RealPathHelper(lpExistingPath, lpBuffer))
        {
            WARN("realpath() failed with error %d\n", errno);
            palError = FILEGetLastErrorFromErrno();
            goto LExit;
        }

#if defined(HOST_AMD64)
        if (fSetFilename == true)
#endif // defined(HOST_AMD64)
            lpFilename = pchSeparator + 1;
    }

#if defined(HOST_AMD64)
    if (lpFilename == NULL)
        goto LExit;
#endif // HOST_AMD64

    if (!lpBuffer.Append("/",1) || !lpBuffer.Append(lpFilename, strlen(lpFilename)))
    {
        ERROR ("Append failed!\n");
        palError = ERROR_INSUFFICIENT_BUFFER;

        // Doing a goto here since we want to exit now. This will work
        // incase someone else adds another if clause below us.
        goto LExit;
    }

#endif // REALPATH_SUPPORTS_NONEXISTENT_FILES
LExit:

    if ((palError == NO_ERROR) && lpBuffer.IsEmpty())
    {
        // convert all these into ERROR_PATH_NOT_FOUND
        palError = ERROR_PATH_NOT_FOUND;
    }

    return palError;
}

PAL_ERROR
CorUnix::InternalCreateFile(
    CPalThread *pThread,
    LPCSTR lpFileName,
    DWORD dwDesiredAccess,
    DWORD dwShareMode,
    LPSECURITY_ATTRIBUTES lpSecurityAttributes,
    DWORD dwCreationDisposition,
    DWORD dwFlagsAndAttributes,
    HANDLE hTemplateFile,
    HANDLE *phFile
    )
{
    PAL_ERROR palError = 0;
    IPalObject *pFileObject = NULL;
    IPalObject *pRegisteredFile = NULL;
    IDataLock *pDataLock = NULL;
    CFileProcessLocalData *pLocalData = NULL;
    CObjectAttributes oaFile(NULL, lpSecurityAttributes);
    BOOL fFileExists = FALSE;

    BOOL inheritable = FALSE;
    PathCharString lpUnixPath;
    int   filed = -1;
    int   create_flags = (S_IRUSR | S_IWUSR | S_IRGRP | S_IROTH);
    int   open_flags = 0;

    // track whether we've created the file with the intended name,
    // so that it can be removed on failure exit
    BOOL bFileCreated = FALSE;

    const char* szNonfilePrefix = "\\\\.\\";
    PathCharString lpFullUnixPath;

    /* for dwShareMode only three flags are accepted */
    if ( dwShareMode & ~(FILE_SHARE_READ | FILE_SHARE_WRITE | FILE_SHARE_DELETE) )
    {
        ASSERT( "dwShareMode is invalid\n" );
        palError = ERROR_INVALID_PARAMETER;
        goto done;
    }

    if ( lpFileName == NULL )
    {
        ERROR("InternalCreateFile called with NULL filename\n");
        palError = ERROR_PATH_NOT_FOUND;
        goto done;
    }

    if ( strncmp(lpFileName, szNonfilePrefix, strlen(szNonfilePrefix)) == 0 )
    {
        ERROR("InternalCreateFile does not support paths beginning with %s\n", szNonfilePrefix);
        palError = ERROR_INVALID_PARAMETER;
        goto done;
    }

    if( !lpUnixPath.Set(lpFileName,strlen(lpFileName)))
    {
        ERROR("strdup() failed\n");
        palError = ERROR_NOT_ENOUGH_MEMORY;
        goto done;
    }

    // Compute the absolute pathname to the file.  This pathname is used
    // to determine if two file names represent the same file.
    palError = InternalCanonicalizeRealPath(lpUnixPath, lpFullUnixPath);
    if (palError != NO_ERROR)
    {
        goto done;
    }

    lpUnixPath.Set(lpFullUnixPath);

    switch( dwDesiredAccess )
    {
    case 0:
        /* Device Query Access was requested. let's use open() with
           no flags, it's basically the equivalent of O_RDONLY, since
           O_RDONLY is defined as 0x0000 */
        break;
    case( GENERIC_READ ):
        open_flags |= O_RDONLY;
        break;
    case( GENERIC_WRITE ):
        open_flags |= O_WRONLY;
        break;
    case( GENERIC_READ | GENERIC_WRITE ):
        open_flags |= O_RDWR;
        break;
    default:
        ERROR("dwDesiredAccess value of %d is invalid\n", dwDesiredAccess);
        palError = ERROR_INVALID_PARAMETER;
        goto done;
    }

    TRACE("open flags are 0x%lx\n", open_flags);

    if ( lpSecurityAttributes )
    {
        if ( lpSecurityAttributes->nLength != sizeof( SECURITY_ATTRIBUTES ) ||
             lpSecurityAttributes->lpSecurityDescriptor != NULL ||
             !lpSecurityAttributes->bInheritHandle )
        {
            ASSERT("lpSecurityAttributes points to invalid values.\n");
            palError = ERROR_INVALID_PARAMETER;
            goto done;
        }
        inheritable = TRUE;
    }

    if ( (dwFlagsAndAttributes & PAL_LEGAL_FLAGS_ATTRIBS) !=
          dwFlagsAndAttributes)
    {
        ASSERT("Bad dwFlagsAndAttributes\n");
        palError = ERROR_INVALID_PARAMETER;
        goto done;
    }
    else if (dwFlagsAndAttributes & FILE_FLAG_BACKUP_SEMANTICS)
    {
        /* Override the open flags, and always open as readonly.  This
           flag is used when opening a directory, to change its
           creation/modification/access times.  On Windows, the directory
           must be open for write, but on Unix, it needs to be readonly. */
        open_flags = O_RDONLY;
    } else {
        struct stat st;

        if (stat(lpUnixPath, &st) == 0 && (st.st_mode & S_IFDIR))
        {
            /* The file exists and it is a directory.  Without
                   FILE_FLAG_BACKUP_SEMANTICS, Win32 CreateFile always fails
                   to open directories. */
                palError = ERROR_ACCESS_DENIED;
                goto done;
        }
    }

    if ( hTemplateFile )
    {
        ASSERT("hTemplateFile is not NULL, as it should be.\n");
        palError = ERROR_INVALID_PARAMETER;
        goto done;
    }

    /* NB: According to MSDN docs, When CREATE_ALWAYS or OPEN_ALWAYS is
       set, CreateFile should SetLastError to ERROR_ALREADY_EXISTS,
       even though/if CreateFile will be successful.
    */
    switch( dwCreationDisposition )
    {
    case( CREATE_ALWAYS ):
        // check whether the file exists
        if ( access( lpUnixPath, F_OK ) == 0 )
        {
            fFileExists = TRUE;
        }
        open_flags |= O_CREAT | O_TRUNC;
        break;
    case( CREATE_NEW ):
        open_flags |= O_CREAT | O_EXCL;
        break;
    case( OPEN_EXISTING ):
        /* don't need to do anything here */
        break;
    case( OPEN_ALWAYS ):
        if ( access( lpUnixPath, F_OK ) == 0 )
        {
            fFileExists = TRUE;
        }
        open_flags |= O_CREAT;
        break;
    case( TRUNCATE_EXISTING ):
        open_flags |= O_TRUNC;
        break;
    default:
        ASSERT("dwCreationDisposition value of %d is not valid\n",
              dwCreationDisposition);
        palError = ERROR_INVALID_PARAMETER;
        goto done;
    }

    if ( dwFlagsAndAttributes & FILE_FLAG_NO_BUFFERING )
    {
        TRACE("I/O will be unbuffered\n");
#ifdef O_DIRECT
        open_flags |= O_DIRECT;
#endif
    }
    else
    {
        TRACE("I/O will be buffered\n");
    }

    filed = InternalOpen(lpUnixPath, open_flags, create_flags);
    TRACE("Allocated file descriptor [%d]\n", filed);

    if ( filed < 0 )
    {
        TRACE("open() failed; error is %s (%d)\n", strerror(errno), errno);
        palError = FILEGetLastErrorFromErrnoAndFilename(lpUnixPath);
        goto done;
    }

    // Deduce whether we created a file in the previous operation (there's a
    // small timing window between the access() used to determine fFileExists
    // and the open() operation, but there's not much we can do about that.
    bFileCreated = (dwCreationDisposition == CREATE_ALWAYS ||
                    dwCreationDisposition == CREATE_NEW ||
                    dwCreationDisposition == OPEN_ALWAYS) &&
        !fFileExists;

#ifndef O_DIRECT
    if ( dwFlagsAndAttributes & FILE_FLAG_NO_BUFFERING )
    {
#ifdef F_NOCACHE
        if (-1 == fcntl(filed, F_NOCACHE, 1))
        {
            ASSERT("Can't set F_NOCACHE; fcntl() failed. errno is %d (%s)\n",
               errno, strerror(errno));
            palError = ERROR_INTERNAL_ERROR;
            goto done;
        }
#elif HAVE_DIRECTIO
        if (-1 == directio(filed, DIRECTIO_ON))
        {
            ASSERT("Can't set DIRECTIO_ON; directio() failed. errno is %d (%s)\n",
               errno, strerror(errno));
            palError = ERROR_INTERNAL_ERROR;
            goto done;
        }
#else
#error Insufficient support for uncached I/O on this platform
#endif
    }
#endif

    /* make file descriptor close-on-exec; inheritable handles will get
      "uncloseonexeced" in CreateProcess if they are actually being inherited*/
    if(-1 == fcntl(filed,F_SETFD, FD_CLOEXEC))
    {
        ASSERT("can't set close-on-exec flag; fcntl() failed. errno is %d "
             "(%s)\n", errno, strerror(errno));
        palError = ERROR_INTERNAL_ERROR;
        goto done;
    }

    palError = g_pObjectManager->AllocateObject(
        pThread,
        &otFile,
        &oaFile,
        &pFileObject
        );

    if (NO_ERROR != palError)
    {
        goto done;
    }

    palError = pFileObject->GetProcessLocalData(
        pThread,
        WriteLock,
        &pDataLock,
        reinterpret_cast<void**>(&pLocalData)
        );

    if (NO_ERROR != palError)
    {
        goto done;
    }

    _ASSERTE(pLocalData->unix_filename == NULL);
    pLocalData->unix_filename = strdup(lpUnixPath);
    if (pLocalData->unix_filename == NULL)
    {
        ASSERT("Unable to copy string\n");
        palError = ERROR_INTERNAL_ERROR;
        goto done;
    }

    pLocalData->inheritable = inheritable;
    pLocalData->unix_fd = filed;
    pLocalData->open_flags = open_flags;
    pLocalData->open_flags_deviceaccessonly = (dwDesiredAccess == 0);

    //
    // We've finished initializing our local data, so release that lock
    //

    pDataLock->ReleaseLock(pThread, TRUE);
    pDataLock = NULL;

    palError = g_pObjectManager->RegisterObject(
        pThread,
        pFileObject,
        &aotFile,
        phFile,
        &pRegisteredFile
        );

    //
    // pFileObject is invalidated by the call to RegisterObject, so NULL it
    // out here to ensure that we don't try to release a reference on
    // it down the line.
    //

    pFileObject = NULL;

done:

    // At this point, if we've been successful, palError will be NO_ERROR.
    // CreateFile can return ERROR_ALREADY_EXISTS in some success cases;
    // those cases are flagged by fFileExists and are handled below.
    if (NO_ERROR != palError)
    {
        if (filed >= 0)
        {
            close(filed);
        }
        if (bFileCreated)
        {
            if (-1 == unlink(lpUnixPath))
            {
                WARN("can't delete file; unlink() failed with errno %d (%s)\n",
                     errno, strerror(errno));
            }
        }
    }

    if (NULL != pDataLock)
    {
        pDataLock->ReleaseLock(pThread, TRUE);
    }

    if (NULL != pFileObject)
    {
        pFileObject->ReleaseReference(pThread);
    }

    if (NULL != pRegisteredFile)
    {
        pRegisteredFile->ReleaseReference(pThread);
    }

    if (NO_ERROR == palError && fFileExists)
    {
        palError = ERROR_ALREADY_EXISTS;
    }

    return palError;
}

/*++
Function:
  CreateFileA

Note:
  Only bInherit flag is used from the LPSECURITY_ATTRIBUTES struct.
  Desired access is READ, WRITE or 0
  Share mode is READ, WRITE or DELETE

See MSDN doc.
--*/
HANDLE
PALAPI
CreateFileA(
        IN LPCSTR lpFileName,
        IN DWORD dwDesiredAccess,
        IN DWORD dwShareMode,
        IN LPSECURITY_ATTRIBUTES lpSecurityAttributes,
        IN DWORD dwCreationDisposition,
        IN DWORD dwFlagsAndAttributes,
        IN HANDLE hTemplateFile
        )
{
    CPalThread *pThread;
    PAL_ERROR palError = NO_ERROR;
    HANDLE  hRet = INVALID_HANDLE_VALUE;

    PERF_ENTRY(CreateFileA);
    ENTRY("CreateFileA(lpFileName=%p (%s), dwAccess=%#x, dwShareMode=%#x, "
          "lpSecurityAttr=%p, dwDisposition=%#x, dwFlags=%#x, "
          "hTemplateFile=%p )\n",lpFileName?lpFileName:"NULL",lpFileName?lpFileName:"NULL", dwDesiredAccess,
          dwShareMode, lpSecurityAttributes, dwCreationDisposition,
          dwFlagsAndAttributes, hTemplateFile);

    pThread = InternalGetCurrentThread();

    palError = InternalCreateFile(
        pThread,
        lpFileName,
        dwDesiredAccess,
        dwShareMode,
        lpSecurityAttributes,
        dwCreationDisposition,
        dwFlagsAndAttributes,
        hTemplateFile,
        &hRet
        );

    //
    // We always need to set last error, even on success:
    // we need to protect ourselves from the situation
    // where last error is set to ERROR_ALREADY_EXISTS on
    // entry to the function
    //

    pThread->SetLastError(palError);

    LOGEXIT("CreateFileA returns HANDLE %p\n", hRet);
    PERF_EXIT(CreateFileA);
    return hRet;
}


/*++
Function:
  CreateFileW

Note:
  Only bInherit flag is used from the LPSECURITY_ATTRIBUTES struct.
  Desired access is READ, WRITE or 0
  Share mode is READ, WRITE or DELETE

See MSDN doc.
--*/
HANDLE
PALAPI
CreateFileW(
        IN LPCWSTR lpFileName,
        IN DWORD dwDesiredAccess,
        IN DWORD dwShareMode,
        IN LPSECURITY_ATTRIBUTES lpSecurityAttributes,
        IN DWORD dwCreationDisposition,
        IN DWORD dwFlagsAndAttributes,
        IN HANDLE hTemplateFile)
{
    CPalThread *pThread;
    PAL_ERROR palError = NO_ERROR;
    PathCharString namePathString;
    char * name;
    int size;
    int length = 0;
    HANDLE  hRet = INVALID_HANDLE_VALUE;

    PERF_ENTRY(CreateFileW);
    ENTRY("CreateFileW(lpFileName=%p (%S), dwAccess=%#x, dwShareMode=%#x, "
          "lpSecurityAttr=%p, dwDisposition=%#x, dwFlags=%#x, hTemplateFile=%p )\n",
          lpFileName?lpFileName:W16_NULLSTRING,
          lpFileName?lpFileName:W16_NULLSTRING, dwDesiredAccess, dwShareMode,
          lpSecurityAttributes, dwCreationDisposition, dwFlagsAndAttributes,
          hTemplateFile);

    pThread = InternalGetCurrentThread();

    if (lpFileName != NULL)
    {
        length = (PAL_wcslen(lpFileName)+1) * MaxWCharToAcpLengthFactor;
    }

    name = namePathString.OpenStringBuffer(length);
    if (NULL == name)
    {
        palError = ERROR_NOT_ENOUGH_MEMORY;
        goto done;
    }

    size = WideCharToMultiByte( CP_ACP, 0, lpFileName, -1, name, length,
                                NULL, NULL );

    if( size == 0 )
    {
        namePathString.CloseBuffer(0);
        DWORD dwLastError = GetLastError();
        ASSERT("WideCharToMultiByte failure! error is %d\n", dwLastError);
        palError = ERROR_INTERNAL_ERROR;
        goto done;
    }

    namePathString.CloseBuffer(size - 1);

    palError = InternalCreateFile(
        pThread,
        name,
        dwDesiredAccess,
        dwShareMode,
        lpSecurityAttributes,
        dwCreationDisposition,
        dwFlagsAndAttributes,
        hTemplateFile,
        &hRet
        );

    //
    // We always need to set last error, even on success:
    // we need to protect ourselves from the situation
    // where last error is set to ERROR_ALREADY_EXISTS on
    // entry to the function
    //

done:
	pThread->SetLastError(palError);
    LOGEXIT( "CreateFileW returns HANDLE %p\n", hRet );
    PERF_EXIT(CreateFileW);
    return hRet;
}


/*++
Function:
  CopyFileW

See MSDN doc.

Notes:
  There are several (most) error paths here that do not call SetLastError().
This is because we know that CreateFile, ReadFile, and WriteFile will do so,
and will have a much better idea of the specific error.
--*/
BOOL
PALAPI
CopyFileW(
      IN LPCWSTR lpExistingFileName,
      IN LPCWSTR lpNewFileName,
      IN BOOL bFailIfExists)
{
    CPalThread *pThread;
    PathCharString sourcePathString;
    PathCharString destPathString;
    char * source;
    char * dest;
    int src_size, dest_size, length = 0;
    BOOL bRet = FALSE;

    PERF_ENTRY(CopyFileW);
    ENTRY("CopyFileW(lpExistingFileName=%p (%S), lpNewFileName=%p (%S), bFailIfExists=%d)\n",
          lpExistingFileName?lpExistingFileName:W16_NULLSTRING,
          lpExistingFileName?lpExistingFileName:W16_NULLSTRING,
          lpNewFileName?lpNewFileName:W16_NULLSTRING,
          lpNewFileName?lpNewFileName:W16_NULLSTRING, bFailIfExists);

    pThread = InternalGetCurrentThread();
    if (lpExistingFileName != NULL)
    {
        length = (PAL_wcslen(lpExistingFileName)+1) * MaxWCharToAcpLengthFactor;
    }

    source = sourcePathString.OpenStringBuffer(length);
    if (NULL == source)
    {
        pThread->SetLastError(ERROR_NOT_ENOUGH_MEMORY);
        goto done;
    }

    src_size = WideCharToMultiByte( CP_ACP, 0, lpExistingFileName, -1, source, length,
                                NULL, NULL );

    if( src_size == 0 )
    {
        sourcePathString.CloseBuffer(0);
        DWORD dwLastError = GetLastError();
        ASSERT("WideCharToMultiByte failure! error is %d\n", dwLastError);
        pThread->SetLastError(ERROR_INTERNAL_ERROR);
        goto done;
    }

    sourcePathString.CloseBuffer(src_size - 1);
    length = 0;

    if (lpNewFileName != NULL)
    {
        length = (PAL_wcslen(lpNewFileName)+1) * MaxWCharToAcpLengthFactor;
    }

    dest = destPathString.OpenStringBuffer(length);
    if (NULL == dest)
    {
        pThread->SetLastError(ERROR_NOT_ENOUGH_MEMORY);
        goto done;
    }
    dest_size = WideCharToMultiByte( CP_ACP, 0, lpNewFileName, -1, dest, length,
                                NULL, NULL );

    if( dest_size == 0 )
    {
        destPathString.CloseBuffer(0);
        DWORD dwLastError = GetLastError();
        ASSERT("WideCharToMultiByte failure! error is %d\n", dwLastError);
        pThread->SetLastError(ERROR_INTERNAL_ERROR);
        goto done;
    }

    destPathString.CloseBuffer(dest_size - 1);
    bRet = CopyFileA(source,dest,bFailIfExists);

done:
    LOGEXIT("CopyFileW returns BOOL %d\n", bRet);
    PERF_EXIT(CopyFileW);
    return bRet;
}


/*++
Function:
  DeleteFileA

See MSDN doc.
--*/
BOOL
PALAPI
DeleteFileA(
        IN LPCSTR lpFileName)
{
    PAL_ERROR palError = NO_ERROR;
    CPalThread *pThread;
    int     result;
    BOOL    bRet = FALSE;
    DWORD   dwLastError = 0;
    PathCharString lpunixFileName;
    PathCharString lpFullunixFileName;

    PERF_ENTRY(DeleteFileA);
    ENTRY("DeleteFileA(lpFileName=%p (%s))\n", lpFileName?lpFileName:"NULL", lpFileName?lpFileName:"NULL");

    pThread = InternalGetCurrentThread();

    if( !lpunixFileName.Set(lpFileName, strlen(lpFileName)))
    {
        palError = ERROR_NOT_ENOUGH_MEMORY;
        goto done;
    }

    // Compute the absolute pathname to the file.  This pathname is used
    // to determine if two file names represent the same file.
    palError = InternalCanonicalizeRealPath(lpunixFileName, lpFullunixFileName);
    if (palError != NO_ERROR)
    {
        if (!lpFullunixFileName.Set(lpunixFileName, strlen(lpunixFileName)))
        {
            palError = ERROR_NOT_ENOUGH_MEMORY;
            goto done;
        }
    }

    result = unlink( lpFullunixFileName );

    if (result < 0)
    {
        TRACE("unlink returns %d\n", result);
        dwLastError = FILEGetLastErrorFromErrnoAndFilename(lpFullunixFileName);
    }
    else
    {
        bRet = TRUE;
    }

done:
    if(dwLastError)
    {
        pThread->SetLastError( dwLastError );
    }

    LOGEXIT("DeleteFileA returns BOOL %d\n", bRet);
    PERF_EXIT(DeleteFileA);
    return bRet;
}


/*++
Function:
  GetFileAttributesA

Note:
  Checking for directory and read-only file.

Caveats:
  There are some important things to note about this implementation, which
are due to the differences between the FAT filesystem and Unix filesystems:

- fifo's, sockets, and symlinks will return -1, and GetLastError() will
  return ERROR_ACCESS_DENIED

- if a file is write-only, or has no permissions at all, it is treated
  the same as if it had mode 'rw'. This is consistent with behaviour on
  NTFS files with the same permissions.

- the following flags will never be returned:

FILE_ATTRIBUTE_SYSTEM
FILE_ATTRIBUTE_ARCHIVE
FILE_ATTRIBUTE_HIDDEN

--*/
DWORD
PALAPI
GetFileAttributesA(
           IN LPCSTR lpFileName)
{
    CPalThread *pThread;
    struct stat stat_data;
    DWORD dwAttr = 0;
    DWORD dwLastError = 0;

    PERF_ENTRY(GetFileAttributesA);
    ENTRY("GetFileAttributesA(lpFileName=%p (%s))\n", lpFileName?lpFileName:"NULL", lpFileName?lpFileName:"NULL");

    pThread = InternalGetCurrentThread();
    if (lpFileName == NULL)
    {
        dwLastError = ERROR_PATH_NOT_FOUND;
        goto done;
    }

    if ( stat(lpFileName, &stat_data) != 0 )
    {
        dwLastError = FILEGetLastErrorFromErrnoAndFilename(lpFileName);
        goto done;
    }

    if ( (stat_data.st_mode & S_IFMT) == S_IFDIR )
    {
        dwAttr |= FILE_ATTRIBUTE_DIRECTORY;
    }
    else if ( (stat_data.st_mode & S_IFMT) != S_IFREG )
    {
        ERROR("Not a regular file or directory, S_IFMT is %#x\n",
              stat_data.st_mode & S_IFMT);
        dwLastError = ERROR_ACCESS_DENIED;
        goto done;
    }

    if ( UTIL_IsReadOnlyBitsSet( &stat_data ) )
    {
        dwAttr |= FILE_ATTRIBUTE_READONLY;
    }

    /* finally, if nothing is set... */
    if ( dwAttr == 0 )
    {
        dwAttr = FILE_ATTRIBUTE_NORMAL;
    }

done:
    if (dwLastError)
    {
        pThread->SetLastError(dwLastError);
        dwAttr = INVALID_FILE_ATTRIBUTES;
    }

    LOGEXIT("GetFileAttributesA returns DWORD %#x\n", dwAttr);
    PERF_EXIT(GetFileAttributesA);
    return dwAttr;
}




/*++
Function:
  GetFileAttributesW

Note:
  Checking for directory and read-only file

See MSDN doc.
--*/
DWORD
PALAPI
GetFileAttributesW(
           IN LPCWSTR lpFileName)
{
    CPalThread *pThread;
    int   size;
    PathCharString filenamePS;
    int length = 0;
    char * filename;
    DWORD dwRet = (DWORD) -1;

    PERF_ENTRY(GetFileAttributesW);
    ENTRY("GetFileAttributesW(lpFileName=%p (%S))\n",
          lpFileName?lpFileName:W16_NULLSTRING,
          lpFileName?lpFileName:W16_NULLSTRING);

    pThread = InternalGetCurrentThread();
    if (lpFileName == NULL)
    {
        pThread->SetLastError(ERROR_PATH_NOT_FOUND);
        goto done;
    }

    length = (PAL_wcslen(lpFileName)+1) * MaxWCharToAcpLengthFactor;
    filename = filenamePS.OpenStringBuffer(length);
    if (NULL == filename)
    {
        pThread->SetLastError(ERROR_NOT_ENOUGH_MEMORY);
        goto done;
    }
    size = WideCharToMultiByte( CP_ACP, 0, lpFileName, -1, filename, length,
                                NULL, NULL );

    if( size == 0 )
    {
        filenamePS.CloseBuffer(0);
        DWORD dwLastError = GetLastError();
        ASSERT("WideCharToMultiByte failure! error is %d\n", dwLastError);
        pThread->SetLastError(ERROR_INTERNAL_ERROR);
    }
    else
    {
        filenamePS.CloseBuffer(size - 1);
        dwRet = GetFileAttributesA( filename );
    }

done:
    LOGEXIT("GetFileAttributesW returns DWORD %#x\n", dwRet);
    PERF_EXIT(GetFileAttributesW);
    return dwRet;
}


/*++
Function:
  GetFileAttributesExW

See MSDN doc, and notes for GetFileAttributesW.
--*/
BOOL
PALAPI
GetFileAttributesExW(
             IN LPCWSTR lpFileName,
             IN GET_FILEEX_INFO_LEVELS fInfoLevelId,
             OUT LPVOID lpFileInformation)
{
    CPalThread *pThread;
    BOOL bRet = FALSE;
    DWORD dwLastError = 0;
    LPWIN32_FILE_ATTRIBUTE_DATA attr_data;

    struct stat stat_data;

    char * name;
    PathCharString namePS;
    int length = 0;
    int  size;

    PERF_ENTRY(GetFileAttributesExW);
    ENTRY("GetFileAttributesExW(lpFileName=%p (%S), fInfoLevelId=%d, "
          "lpFileInformation=%p)\n", lpFileName?lpFileName:W16_NULLSTRING, lpFileName?lpFileName:W16_NULLSTRING,
          fInfoLevelId, lpFileInformation);

    pThread = InternalGetCurrentThread();
    if ( fInfoLevelId != GetFileExInfoStandard )
    {
        ASSERT("Unrecognized value for fInfoLevelId=%d\n", fInfoLevelId);
        dwLastError = ERROR_INVALID_PARAMETER;
        goto done;
    }

    if ( !lpFileInformation )
    {
        ASSERT("lpFileInformation is NULL\n");
        dwLastError = ERROR_INVALID_PARAMETER;
        goto done;
    }

    if (lpFileName == NULL)
    {
        dwLastError = ERROR_PATH_NOT_FOUND;
        goto done;
    }

    length = (PAL_wcslen(lpFileName)+1) * MaxWCharToAcpLengthFactor;
    name = namePS.OpenStringBuffer(length);
    if (NULL == name)
    {
        dwLastError = ERROR_NOT_ENOUGH_MEMORY;
        goto done;
    }
    size = WideCharToMultiByte( CP_ACP, 0, lpFileName, -1, name, length,
                                NULL, NULL );

    if( size == 0 )
    {
        namePS.CloseBuffer(0);
        dwLastError = GetLastError();
        ASSERT("WideCharToMultiByte failure! error is %d\n", dwLastError);
        dwLastError = ERROR_INTERNAL_ERROR;
        goto done;
    }

    namePS.CloseBuffer(size - 1);
    attr_data = (LPWIN32_FILE_ATTRIBUTE_DATA)lpFileInformation;

    attr_data->dwFileAttributes = GetFileAttributesW(lpFileName);
    /* assume that GetFileAttributesW will call SetLastError appropriately */
    if ( attr_data->dwFileAttributes == (DWORD)-1 )
    {
        goto done;
    }

    /* do the stat */
    if ( stat(name, &stat_data) != 0 )
    {
        ERROR("stat failed on %S\n", lpFileName);
        dwLastError = FILEGetLastErrorFromErrnoAndFilename(name);
        goto done;
    }

    /* get the file times */
    attr_data->ftCreationTime =
        FILEUnixTimeToFileTime( stat_data.st_ctime,
                                ST_CTIME_NSEC(&stat_data) );
    attr_data->ftLastAccessTime =
        FILEUnixTimeToFileTime( stat_data.st_atime,
                                ST_ATIME_NSEC(&stat_data) );
    attr_data->ftLastWriteTime =
        FILEUnixTimeToFileTime( stat_data.st_mtime,
                                ST_MTIME_NSEC(&stat_data) );

    /* if Unix mtime is greater than atime, return mtime
       as the last access time */
    if (CompareFileTime(&attr_data->ftLastAccessTime,
                        &attr_data->ftLastWriteTime) < 0)
    {
         attr_data->ftLastAccessTime = attr_data->ftLastWriteTime;
    }

    /* if Unix ctime is greater than mtime, return mtime
       as the create time */
    if (CompareFileTime(&attr_data->ftLastWriteTime,
                        &attr_data->ftCreationTime) < 0)
    {
         attr_data->ftCreationTime = attr_data->ftLastWriteTime;
    }

    /* Get the file size. GetFileSize is not used because it gets the
       size of an already-open file */
    attr_data->nFileSizeLow = (DWORD) stat_data.st_size;
#if SIZEOF_OFF_T > 4
    attr_data->nFileSizeHigh = (DWORD)(stat_data.st_size >> 32);
#else
    attr_data->nFileSizeHigh = 0;
#endif

    bRet = TRUE;

done:
    if (dwLastError) pThread->SetLastError(dwLastError);

    LOGEXIT("GetFileAttributesExW returns BOOL %d\n", bRet);
    PERF_EXIT(GetFileAttributesExW);
    return bRet;
}

/*++
Function:
  SetFileAttributesA

Notes:
  Used for setting read-only attribute on file only.

--*/
BOOL
PALAPI
SetFileAttributesA(
           IN LPCSTR lpFileName,
           IN DWORD dwFileAttributes)
{
    CPalThread *pThread;
    struct stat stat_data;
    mode_t new_mode;

    DWORD dwLastError = 0;
    BOOL  bRet = FALSE;

    PERF_ENTRY(SetFileAttributesA);
    ENTRY("SetFileAttributesA(lpFileName=%p (%s), dwFileAttributes=%#x)\n",
        lpFileName?lpFileName:"NULL",
        lpFileName?lpFileName:"NULL", dwFileAttributes);

    pThread = InternalGetCurrentThread();

    /* Windows behavior for SetFileAttributes is that any valid attributes
    are set on a file and any invalid attributes are ignored. SetFileAttributes
    returns success and does not set an error even if some or all of the
    attributes are invalid. If all the attributes are invalid, SetFileAttributes
    sets a file's attribute to NORMAL. */

    /* If dwFileAttributes does not contain READONLY or NORMAL, set it to NORMAL
    and print a warning message. */
    if ( !(dwFileAttributes & (FILE_ATTRIBUTE_READONLY |FILE_ATTRIBUTE_NORMAL)) )
    {
        dwFileAttributes = FILE_ATTRIBUTE_NORMAL;
        WARN("dwFileAttributes(%#x) contains attributes that are either not supported "
            "or cannot be set via SetFileAttributes.\n");
    }

    if ( (dwFileAttributes & FILE_ATTRIBUTE_NORMAL) &&
         (dwFileAttributes != FILE_ATTRIBUTE_NORMAL) )
    {
        WARN("Ignoring FILE_ATTRIBUTE_NORMAL -- it must be used alone\n");
    }

    if (lpFileName == NULL)
    {
        dwLastError = ERROR_FILE_NOT_FOUND;
        goto done;
    }

    if ( stat(lpFileName, &stat_data) != 0 )
    {
        TRACE("stat failed on %s; errno is %d (%s)\n",
             lpFileName, errno, strerror(errno));
        dwLastError = FILEGetLastErrorFromErrnoAndFilename(lpFileName);
        goto done;
    }

    new_mode = stat_data.st_mode;
    TRACE("st_mode is %#x\n", new_mode);

    /* if we can't do GetFileAttributesA on it, don't do SetFileAttributesA */
    if ( !(new_mode & S_IFREG) && !(new_mode & S_IFDIR) )
    {
        ERROR("Not a regular file or directory, S_IFMT is %#x\n",
              new_mode & S_IFMT);
        dwLastError = ERROR_ACCESS_DENIED;
        goto done;
    }

    /* set or unset the "read-only" attribute */
    if (dwFileAttributes & FILE_ATTRIBUTE_READONLY)
    {
        /* remove the write bit from everybody */
        new_mode &= ~(S_IWUSR | S_IWGRP | S_IWOTH);
    }
    else
    {
        /* give write permission to the owner if the owner
         * already has read permission */
        if ( new_mode & S_IRUSR )
        {
            new_mode |= S_IWUSR;
        }
    }
    TRACE("new mode is %#x\n", new_mode);

    bRet = TRUE;
    if ( new_mode != stat_data.st_mode )
    {
        if ( chmod(lpFileName, new_mode) != 0 )
        {
            ERROR("chmod(%s, %#x) failed\n", lpFileName, new_mode);
            dwLastError = FILEGetLastErrorFromErrnoAndFilename(lpFileName);
            bRet = FALSE;
        }
    }

done:
    if (dwLastError)
    {
        pThread->SetLastError(dwLastError);
    }

    LOGEXIT("SetFileAttributesA returns BOOL %d\n", bRet);
    PERF_EXIT(SetFileAttributesA);
    return bRet;
}

/*++
Function:
  SetFileAttributesW

Notes:
  Used for setting read-only attribute on file only.

--*/
BOOL
PALAPI
SetFileAttributesW(
           IN LPCWSTR lpFileName,
           IN DWORD dwFileAttributes)
{
    CPalThread *pThread;
    char * name;
    PathCharString namePS;
    int length = 0;
    int  size;

    DWORD dwLastError = 0;
    BOOL  bRet = FALSE;

    PERF_ENTRY(SetFileAttributesW);
    ENTRY("SetFileAttributesW(lpFileName=%p (%S), dwFileAttributes=%#x)\n",
        lpFileName?lpFileName:W16_NULLSTRING,
        lpFileName?lpFileName:W16_NULLSTRING, dwFileAttributes);

    pThread = InternalGetCurrentThread();
    if (lpFileName == NULL)
    {
        dwLastError = ERROR_PATH_NOT_FOUND;
        goto done;
    }

    length = (PAL_wcslen(lpFileName)+1) * MaxWCharToAcpLengthFactor;
    name = namePS.OpenStringBuffer(length);
    if (NULL == name)
    {
        dwLastError = ERROR_NOT_ENOUGH_MEMORY;
        goto done;
    }
    size = WideCharToMultiByte( CP_ACP, 0, lpFileName, -1, name, length,
                                NULL, NULL );

    if( size == 0 )
    {
        namePS.CloseBuffer(0);
        dwLastError = GetLastError();
        ASSERT("WideCharToMultiByte failure! error is %d\n", dwLastError);
        dwLastError = ERROR_INVALID_PARAMETER;
        goto done;
    }
    namePS.CloseBuffer(size - 1);
    bRet = SetFileAttributesA(name,dwFileAttributes);

done:
    if (dwLastError) pThread->SetLastError(dwLastError);

    LOGEXIT("SetFileAttributes returns BOOL %d\n", bRet);
    PERF_EXIT(SetFileAttributesW);
    return bRet;
}

/*++
InternalOpen

Wrapper for open.

Input parameters:

szPath = pointer to a pathname of a file to be opened
nFlags = arguments that control how the file should be accessed
mode = file permission settings that are used only when a file is created

Return value:
    File descriptor on success, -1 on failure
--*/
int
CorUnix::InternalOpen(
    const char *szPath,
    int nFlags,
    ...
    )
{
    int nRet = -1;
    int mode = 0;
    va_list ap;

    // If nFlags does not contain O_CREAT, the mode parameter will be ignored.
    if (nFlags & O_CREAT)
    {
        va_start(ap, nFlags);
        mode = va_arg(ap, int);
        va_end(ap);
    }

    do
    {
#if OPEN64_IS_USED_INSTEAD_OF_OPEN
        nRet = open64(szPath, nFlags, mode);
#else
        nRet = open(szPath, nFlags, mode);
#endif
    }
    while ((nRet == -1) && (errno == EINTR));

    return nRet;
}

PAL_ERROR
CorUnix::InternalWriteFile(
    CPalThread *pThread,
    HANDLE hFile,
    LPCVOID lpBuffer,
    DWORD nNumberOfBytesToWrite,
    LPDWORD lpNumberOfBytesWritten,
    LPOVERLAPPED lpOverlapped
    )
{
    PAL_ERROR palError = 0;
    IPalObject *pFileObject = NULL;
    CFileProcessLocalData *pLocalData = NULL;
    IDataLock *pLocalDataLock = NULL;
    int ifd;
    int res;

    if (NULL != lpNumberOfBytesWritten)
    {
        //
        // This must be set to 0 before any other error checking takes
        // place, per MSDN
        //

        *lpNumberOfBytesWritten = 0;
    }
    else
    {
        ASSERT( "lpNumberOfBytesWritten is NULL\n" );
        palError = ERROR_INVALID_PARAMETER;
        goto done;
    }

    // Win32 WriteFile disallows writing to STD_INPUT_HANDLE
    if (hFile == INVALID_HANDLE_VALUE || hFile == pStdIn)
    {
        palError = ERROR_INVALID_HANDLE;
        goto done;
    }
    else if ( lpOverlapped )
    {
        ASSERT( "lpOverlapped is not NULL, as it should be.\n" );
        palError = ERROR_INVALID_PARAMETER;
        goto done;
    }

    palError = g_pObjectManager->ReferenceObjectByHandle(
        pThread,
        hFile,
        &aotFile,
        &pFileObject
        );

    if (NO_ERROR != palError)
    {
        goto done;
    }

    palError = pFileObject->GetProcessLocalData(
        pThread,
        ReadLock,
        &pLocalDataLock,
        reinterpret_cast<void**>(&pLocalData)
        );

    if (NO_ERROR != palError)
    {
        goto done;
    }

    if (pLocalData->open_flags_deviceaccessonly == TRUE)
    {
        ERROR("File open for device access only\n");
        palError = ERROR_ACCESS_DENIED;
        goto done;
    }

    ifd = pLocalData->unix_fd;

    //
    // Release the data lock before performing the (possibly blocking)
    // write call
    //

    pLocalDataLock->ReleaseLock(pThread, FALSE);
    pLocalDataLock = NULL;
    pLocalData = NULL;

#if WRITE_0_BYTES_HANGS_TTY
    if( nNumberOfBytesToWrite == 0 && isatty(ifd) )
    {
        res = 0;
        *lpNumberOfBytesWritten = 0;
        goto done;
    }
#endif

    res = write( ifd, lpBuffer, nNumberOfBytesToWrite );
    TRACE("write() returns %d\n", res);

    if ( res >= 0 )
    {
        *lpNumberOfBytesWritten = res;
    }
    else
    {
        palError = FILEGetLastErrorFromErrno();
    }

done:

    if (NULL != pLocalDataLock)
    {
        pLocalDataLock->ReleaseLock(pThread, FALSE);
    }

    if (NULL != pFileObject)
    {
        pFileObject->ReleaseReference(pThread);
    }

    return palError;
}


/*++
Function:
  WriteFileW

Note:
  lpOverlapped always NULL.

See MSDN doc.
--*/
BOOL
PALAPI
WriteFile(
      IN HANDLE hFile,
      IN LPCVOID lpBuffer,
      IN DWORD nNumberOfBytesToWrite,
      OUT LPDWORD lpNumberOfBytesWritten,
      IN LPOVERLAPPED lpOverlapped)
{
    PAL_ERROR palError;
    CPalThread *pThread;

    PERF_ENTRY(WriteFile);
    ENTRY("WriteFile(hFile=%p, lpBuffer=%p, nToWrite=%u, lpWritten=%p, "
          "lpOverlapped=%p)\n", hFile, lpBuffer, nNumberOfBytesToWrite,
          lpNumberOfBytesWritten, lpOverlapped);

    pThread = InternalGetCurrentThread();

    palError = InternalWriteFile(
        pThread,
        hFile,
        lpBuffer,
        nNumberOfBytesToWrite,
        lpNumberOfBytesWritten,
        lpOverlapped
        );

    if (NO_ERROR != palError)
    {
        pThread->SetLastError(palError);
    }

    LOGEXIT("WriteFile returns BOOL %d\n", NO_ERROR == palError);
    PERF_EXIT(WriteFile);
    return NO_ERROR == palError;
}

PAL_ERROR
CorUnix::InternalReadFile(
    CPalThread *pThread,
    HANDLE hFile,
    LPVOID lpBuffer,
    DWORD nNumberOfBytesToRead,
    LPDWORD lpNumberOfBytesRead,
    LPOVERLAPPED lpOverlapped
    )
{
    PAL_ERROR palError = 0;
    IPalObject *pFileObject = NULL;
    CFileProcessLocalData *pLocalData = NULL;
    IDataLock *pLocalDataLock = NULL;
    int ifd;
    int res;

    if (NULL != lpNumberOfBytesRead)
    {
        //
        // This must be set to 0 before any other error checking takes
        // place, per MSDN
        //

        *lpNumberOfBytesRead = 0;
    }
    else
    {
        ERROR( "lpNumberOfBytesRead is NULL\n" );
        palError = ERROR_INVALID_PARAMETER;
        goto done;
    }

    if (INVALID_HANDLE_VALUE == hFile)
    {
        ERROR( "Invalid file handle\n" );
        palError = ERROR_INVALID_HANDLE;
        goto done;
    }
    else if (NULL != lpOverlapped)
    {
        ASSERT( "lpOverlapped is not NULL, as it should be.\n" );
        palError = ERROR_INVALID_PARAMETER;
        goto done;
    }
    else if (NULL == lpBuffer)
    {
        ERROR( "Invalid parameter. (lpBuffer:%p)\n", lpBuffer);
        palError = ERROR_NOACCESS;
        goto done;
    }

    palError = g_pObjectManager->ReferenceObjectByHandle(
        pThread,
        hFile,
        &aotFile,
        &pFileObject
        );

    if (NO_ERROR != palError)
    {
        goto done;
    }

    palError = pFileObject->GetProcessLocalData(
        pThread,
        ReadLock,
        &pLocalDataLock,
        reinterpret_cast<void**>(&pLocalData)
        );

    if (NO_ERROR != palError)
    {
        goto done;
    }

    if (pLocalData->open_flags_deviceaccessonly == TRUE)
    {
        ERROR("File open for device access only\n");
        palError = ERROR_ACCESS_DENIED;
        goto done;
    }

    ifd = pLocalData->unix_fd;

    //
    // Release the data lock before performing the (possibly blocking)
    // read call
    //

    pLocalDataLock->ReleaseLock(pThread, FALSE);
    pLocalDataLock = NULL;
    pLocalData = NULL;

Read:
    TRACE("Reading from file descriptor %d\n", ifd);
    res = read(ifd, lpBuffer, nNumberOfBytesToRead);
    TRACE("read() returns %d\n", res);

    if (res >= 0)
    {
        *lpNumberOfBytesRead = res;
    }
    else if (errno == EINTR)
    {
        // Try to read again.
        goto Read;
    }
    else
    {
        palError = FILEGetLastErrorFromErrno();
    }

done:

    if (NULL != pLocalDataLock)
    {
        pLocalDataLock->ReleaseLock(pThread, FALSE);
    }

    if (NULL != pFileObject)
    {
        pFileObject->ReleaseReference(pThread);
    }

    return palError;
}

/*++
Function:
  ReadFile

Note:
  lpOverlapped always NULL.

See MSDN doc.
--*/
BOOL
PALAPI
ReadFile(
     IN HANDLE hFile,
     OUT LPVOID lpBuffer,
     IN DWORD nNumberOfBytesToRead,
     OUT LPDWORD lpNumberOfBytesRead,
     IN LPOVERLAPPED lpOverlapped)
{
    PAL_ERROR palError;
    CPalThread *pThread;

    PERF_ENTRY(ReadFile);
    ENTRY("ReadFile(hFile=%p, lpBuffer=%p, nToRead=%u, "
          "lpRead=%p, lpOverlapped=%p)\n",
          hFile, lpBuffer, nNumberOfBytesToRead,
          lpNumberOfBytesRead, lpOverlapped);

    pThread = InternalGetCurrentThread();

    palError = InternalReadFile(
        pThread,
        hFile,
        lpBuffer,
        nNumberOfBytesToRead,
        lpNumberOfBytesRead,
        lpOverlapped
        );

    if (NO_ERROR != palError)
    {
        pThread->SetLastError(palError);
    }

    LOGEXIT("ReadFile returns BOOL %d\n", NO_ERROR == palError);
    PERF_EXIT(ReadFile);
    return NO_ERROR == palError;
}


/*++
Function:
  GetStdHandle

See MSDN doc.
--*/
HANDLE
PALAPI
GetStdHandle(
         IN DWORD nStdHandle)
{
    CPalThread *pThread;
    HANDLE hRet = INVALID_HANDLE_VALUE;

    PERF_ENTRY(GetStdHandle);
    ENTRY("GetStdHandle(nStdHandle=%#x)\n", nStdHandle);

    pThread = InternalGetCurrentThread();
    switch( nStdHandle )
    {
    case STD_INPUT_HANDLE:
        hRet = pStdIn;
        break;
    case STD_OUTPUT_HANDLE:
        hRet = pStdOut;
        break;
    case STD_ERROR_HANDLE:
        hRet = pStdErr;
        break;
    default:
        ERROR("nStdHandle is invalid\n");
        pThread->SetLastError(ERROR_INVALID_PARAMETER);
        break;
    }

    LOGEXIT("GetStdHandle returns HANDLE %p\n", hRet);
    PERF_EXIT(GetStdHandle);
    return hRet;
}

//
// We need to break out the actual mechanics of setting the file pointer
// on the unix FD for InternalReadFile and InternalWriteFile, as they
// need to call this routine in order to determine the value of the
// current file pointer when computing the scope of their transaction
// lock. If we didn't break out this logic we'd end up referencing the file
// handle multiple times, and, in the process, would attempt to recursively
// obtain the local process data lock for the underlying file object.
//

PAL_ERROR
InternalSetFilePointerForUnixFd(
    int iUnixFd,
    LONG lDistanceToMove,
    PLONG lpDistanceToMoveHigh,
    DWORD dwMoveMethod,
    PLONG lpNewFilePointerLow
    )
{
    PAL_ERROR palError = NO_ERROR;
    int     seek_whence = 0;
    __int64 seek_offset = 0LL;
    __int64 seek_res = 0LL;
    off_t old_offset;

    switch( dwMoveMethod )
    {
    case FILE_BEGIN:
        seek_whence = SEEK_SET;
        break;
    case FILE_CURRENT:
        seek_whence = SEEK_CUR;
        break;
    case FILE_END:
        seek_whence = SEEK_END;
        break;
    default:
        ERROR("dwMoveMethod = %d is invalid\n", dwMoveMethod);
        palError = ERROR_INVALID_PARAMETER;
        goto done;
    }

    //
    // According to MSDN, if lpDistanceToMoveHigh is not null,
    // lDistanceToMove is treated as unsigned;
    // it is treated as signed otherwise
    //

    if ( lpDistanceToMoveHigh )
    {
        /* set the high 32 bits of the offset */
        seek_offset = ((__int64)*lpDistanceToMoveHigh << 32);

        /* set the low 32 bits */
        /* cast to unsigned long to avoid sign extension */
        seek_offset |= (ULONG) lDistanceToMove;
    }
    else
    {
        seek_offset |= lDistanceToMove;
    }

    /* store the current position, in case the lseek moves the pointer
       before the beginning of the file */
    old_offset = lseek(iUnixFd, 0, SEEK_CUR);
    if (old_offset == -1)
    {
        ERROR("lseek(fd,0,SEEK_CUR) failed errno:%d (%s)\n",
              errno, strerror(errno));
        palError = ERROR_ACCESS_DENIED;
        goto done;
    }

    // Check to see if we're going to seek to a negative offset.
    // If we're seeking from the beginning or the current mark,
    // this is simple.
    if ((seek_whence == SEEK_SET && seek_offset < 0) ||
        (seek_whence == SEEK_CUR && seek_offset + old_offset < 0))
    {
        palError = ERROR_NEGATIVE_SEEK;
        goto done;
    }
    else if (seek_whence == SEEK_END && seek_offset < 0)
    {
        // We need to determine if we're seeking past the
        // beginning of the file, but we don't want to adjust
        // the mark in the process. stat is the only way to
        // do that.
        struct stat fileData;
        int result;

        result = fstat(iUnixFd, &fileData);
        if (result == -1)
        {
            // It's a bad fd. This shouldn't happen because
            // we've already called lseek on it, but you
            // never know. This is the best we can do.
            palError = ERROR_ACCESS_DENIED;
            goto done;
        }
        if (fileData.st_size < -seek_offset)
        {
            // Seeking past the beginning.
            palError = ERROR_NEGATIVE_SEEK;
            goto done;
        }
    }

    seek_res = (__int64)lseek( iUnixFd,
                               seek_offset,
                               seek_whence );
    if ( seek_res < 0 )
    {
        /* lseek() returns -1 on error, but also can seek to negative
           file offsets, so -1 can also indicate a successful seek to offset
           -1.  Win32 doesn't allow negative file offsets, so either case
           is an error. */
        ERROR("lseek failed errno:%d (%s)\n", errno, strerror(errno));
        lseek(iUnixFd, old_offset, SEEK_SET);
        palError = ERROR_ACCESS_DENIED;
    }
    else
    {
        /* store high-order DWORD */
        if ( lpDistanceToMoveHigh )
            *lpDistanceToMoveHigh = (DWORD)(seek_res >> 32);

        /* return low-order DWORD of seek result */
        *lpNewFilePointerLow = (DWORD)seek_res;
    }

done:

    return palError;
}

PAL_ERROR
CorUnix::InternalSetFilePointer(
    CPalThread *pThread,
    HANDLE hFile,
    LONG lDistanceToMove,
    PLONG lpDistanceToMoveHigh,
    DWORD dwMoveMethod,
    PLONG lpNewFilePointerLow
    )
{
    PAL_ERROR palError = NO_ERROR;
    IPalObject *pFileObject = NULL;
    CFileProcessLocalData *pLocalData = NULL;
    IDataLock *pLocalDataLock = NULL;

    if (INVALID_HANDLE_VALUE == hFile)
    {
        ERROR( "Invalid file handle\n" );
        palError = ERROR_INVALID_HANDLE;
        goto InternalSetFilePointerExit;
    }

    palError = g_pObjectManager->ReferenceObjectByHandle(
        pThread,
        hFile,
        &aotFile,
        &pFileObject
        );

    if (NO_ERROR != palError)
    {
        goto InternalSetFilePointerExit;
    }

    palError = pFileObject->GetProcessLocalData(
        pThread,
        ReadLock,
        &pLocalDataLock,
        reinterpret_cast<void**>(&pLocalData)
        );

    if (NO_ERROR != palError)
    {
        goto InternalSetFilePointerExit;
    }

    palError = InternalSetFilePointerForUnixFd(
        pLocalData->unix_fd,
        lDistanceToMove,
        lpDistanceToMoveHigh,
        dwMoveMethod,
        lpNewFilePointerLow
        );

InternalSetFilePointerExit:

    if (NULL != pLocalDataLock)
    {
        pLocalDataLock->ReleaseLock(pThread, FALSE);
    }

    if (NULL != pFileObject)
    {
        pFileObject->ReleaseReference(pThread);
    }

    return palError;
}

/*++
Function:
  SetFilePointer

See MSDN doc.
--*/
DWORD
PALAPI
SetFilePointer(
           IN HANDLE hFile,
           IN LONG lDistanceToMove,
           IN PLONG lpDistanceToMoveHigh,
           IN DWORD dwMoveMethod)
{
    PAL_ERROR palError = NO_ERROR;
    CPalThread *pThread;
    LONG lNewFilePointerLow = 0;

    PERF_ENTRY(SetFilePointer);
    ENTRY("SetFilePointer(hFile=%p, lDistance=%d, lpDistanceHigh=%p, "
          "dwMoveMethod=%#x)\n", hFile, lDistanceToMove,
          lpDistanceToMoveHigh, dwMoveMethod);

    pThread = InternalGetCurrentThread();

    palError = InternalSetFilePointer(
        pThread,
        hFile,
        lDistanceToMove,
        lpDistanceToMoveHigh,
        dwMoveMethod,
        &lNewFilePointerLow
        );

    if (NO_ERROR != palError)
    {
        lNewFilePointerLow = INVALID_SET_FILE_POINTER;
    }

    /* This function must always call SetLastError - even if successful.
       If we seek to a value greater than 2^32 - 1, we will effectively be
       returning a negative value from this function. Now, let's say that
       returned value is -1. Furthermore, assume that win32error has been
       set before even entering this function. Then, when this function
       returns to SetFilePointer in win32native.cs, it will have returned
       -1 and win32error will have been set, which will cause an error to be
       returned. Since -1 may not be an error in this case and since we
       can't assume that the win32error is related to SetFilePointer,
       we need to always call SetLastError here. That way, if this function
       succeeds, SetFilePointer in win32native won't mistakenly determine
       that it failed. */
    pThread->SetLastError(palError);

    LOGEXIT("SetFilePointer returns DWORD %#x\n", lNewFilePointerLow);
    PERF_EXIT(SetFilePointer);
    return lNewFilePointerLow;
}

/*++
Function:
  SetFilePointerEx

See MSDN doc.
--*/
BOOL
PALAPI
SetFilePointerEx(
           IN HANDLE hFile,
           IN LARGE_INTEGER liDistanceToMove,
           OUT PLARGE_INTEGER lpNewFilePointer,
           IN DWORD dwMoveMethod)
{
    PAL_ERROR palError = NO_ERROR;
    CPalThread *pThread;
    BOOL Ret = FALSE;

    PERF_ENTRY(SetFilePointerEx);
    ENTRY("SetFilePointerEx(hFile=%p, liDistanceToMove=0x%llx, "
           "lpNewFilePointer=%p (0x%llx), dwMoveMethod=0x%x)\n", hFile,
           liDistanceToMove.QuadPart, lpNewFilePointer,
           (lpNewFilePointer) ? (*lpNewFilePointer).QuadPart : 0, dwMoveMethod);

    LONG lDistanceToMove;
    lDistanceToMove = (LONG)liDistanceToMove.u.LowPart;
    LONG lDistanceToMoveHigh;
    lDistanceToMoveHigh = liDistanceToMove.u.HighPart;

    LONG lNewFilePointerLow = 0;

    pThread = InternalGetCurrentThread();

    palError = InternalSetFilePointer(
        pThread,
        hFile,
        lDistanceToMove,
        &lDistanceToMoveHigh,
        dwMoveMethod,
        &lNewFilePointerLow
        );

    if (NO_ERROR != palError)
    {
        pThread->SetLastError(palError);
    }
    else
    {
        if (lpNewFilePointer != NULL)
        {
            lpNewFilePointer->u.LowPart = (DWORD)lNewFilePointerLow;
            lpNewFilePointer->u.HighPart = (DWORD)lDistanceToMoveHigh;
        }
        Ret = TRUE;
    }

    LOGEXIT("SetFilePointerEx returns BOOL %d\n", Ret);
    PERF_EXIT(SetFilePointerEx);
    return Ret;
}

PAL_ERROR
CorUnix::InternalGetFileSize(
    CPalThread *pThread,
    HANDLE hFile,
    DWORD *pdwFileSizeLow,
    DWORD *pdwFileSizeHigh
    )
{
    PAL_ERROR palError = NO_ERROR;
    IPalObject *pFileObject = NULL;
    CFileProcessLocalData *pLocalData = NULL;
    IDataLock *pLocalDataLock = NULL;

    struct stat stat_data;

    if (INVALID_HANDLE_VALUE == hFile)
    {
        ERROR( "Invalid file handle\n" );
        palError = ERROR_INVALID_HANDLE;
        goto InternalGetFileSizeExit;
    }

    palError = g_pObjectManager->ReferenceObjectByHandle(
        pThread,
        hFile,
        &aotFile,
        &pFileObject
        );

    if (NO_ERROR != palError)
    {
        goto InternalGetFileSizeExit;
    }

    palError = pFileObject->GetProcessLocalData(
        pThread,
        ReadLock,
        &pLocalDataLock,
        reinterpret_cast<void**>(&pLocalData)
        );

    if (NO_ERROR != palError)
    {
        goto InternalGetFileSizeExit;
    }

    if (fstat(pLocalData->unix_fd, &stat_data) != 0)
    {
        ERROR("fstat failed of file descriptor %d\n", pLocalData->unix_fd);
        palError = FILEGetLastErrorFromErrno();
        goto InternalGetFileSizeExit;
    }

    *pdwFileSizeLow = (DWORD)stat_data.st_size;

    if (NULL != pdwFileSizeHigh)
    {
#if SIZEOF_OFF_T > 4
        *pdwFileSizeHigh = (DWORD)(stat_data.st_size >> 32);
#else
        *pdwFileSizeHigh = 0;
#endif
    }

InternalGetFileSizeExit:

    if (NULL != pLocalDataLock)
    {
        pLocalDataLock->ReleaseLock(pThread, FALSE);
    }

    if (NULL != pFileObject)
    {
        pFileObject->ReleaseReference(pThread);
    }

    return palError;
}

/*++
Function:
  GetFileSize

See MSDN doc.
--*/
DWORD
PALAPI
GetFileSize(
        IN HANDLE hFile,
        OUT LPDWORD lpFileSizeHigh)
{
    PAL_ERROR palError = NO_ERROR;
    CPalThread *pThread;
    DWORD dwFileSizeLow;

    PERF_ENTRY(GetFileSize);
    ENTRY("GetFileSize(hFile=%p, lpFileSizeHigh=%p)\n", hFile, lpFileSizeHigh);

    pThread = InternalGetCurrentThread();

    palError = InternalGetFileSize(
        pThread,
        hFile,
        &dwFileSizeLow,
        lpFileSizeHigh
        );

    if (NO_ERROR != palError)
    {
        pThread->SetLastError(palError);
        dwFileSizeLow = INVALID_FILE_SIZE;
    }

    LOGEXIT("GetFileSize returns DWORD %u\n", dwFileSizeLow);
    PERF_EXIT(GetFileSize);
    return dwFileSizeLow;
}

/*++
Function:
GetFileSizeEx

See MSDN doc.
--*/
BOOL
PALAPI GetFileSizeEx(
IN   HANDLE hFile,
OUT  PLARGE_INTEGER lpFileSize)
{
    PAL_ERROR palError = NO_ERROR;
    CPalThread *pThread;
    DWORD dwFileSizeHigh;
    DWORD dwFileSizeLow;

    PERF_ENTRY(GetFileSizeEx);
    ENTRY("GetFileSizeEx(hFile=%p, lpFileSize=%p)\n", hFile, lpFileSize);

    pThread = InternalGetCurrentThread();

    if (lpFileSize != NULL)
    {
        palError = InternalGetFileSize(
            pThread,
            hFile,
            &dwFileSizeLow,
            &dwFileSizeHigh
            );

        if (NO_ERROR == palError)
        {
            lpFileSize->u.LowPart = dwFileSizeLow;
            lpFileSize->u.HighPart = dwFileSizeHigh;
        }
    }
    else
    {
        palError = ERROR_INVALID_PARAMETER;
    }

    if (NO_ERROR != palError)
    {
        pThread->SetLastError(palError);
    }

    LOGEXIT("GetFileSizeEx returns BOOL %d\n", NO_ERROR == palError);
    PERF_EXIT(GetFileSizeEx);
    return NO_ERROR == palError;
}

PAL_ERROR
CorUnix::InternalFlushFileBuffers(
    CPalThread *pThread,
    HANDLE hFile
    )
{
    PAL_ERROR palError = NO_ERROR;
    IPalObject *pFileObject = NULL;
    CFileProcessLocalData *pLocalData = NULL;
    IDataLock *pLocalDataLock = NULL;

    if (INVALID_HANDLE_VALUE == hFile)
    {
        ERROR( "Invalid file handle\n" );
        palError = ERROR_INVALID_HANDLE;
        goto InternalFlushFileBuffersExit;
    }

    palError = g_pObjectManager->ReferenceObjectByHandle(
        pThread,
        hFile,
        &aotFile,
        &pFileObject
        );

    if (NO_ERROR != palError)
    {
        goto InternalFlushFileBuffersExit;
    }

    palError = pFileObject->GetProcessLocalData(
        pThread,
        ReadLock,
        &pLocalDataLock,
        reinterpret_cast<void**>(&pLocalData)
        );

    if (NO_ERROR != palError)
    {
        goto InternalFlushFileBuffersExit;
    }

    if (pLocalData->open_flags_deviceaccessonly == TRUE)
    {
        ERROR("File open for device access only\n");
        palError = ERROR_ACCESS_DENIED;
        goto InternalFlushFileBuffersExit;
    }

#if HAVE_FSYNC || defined(__APPLE__)
    do
    {

#if defined(__APPLE__)
        if (fcntl(pLocalData->unix_fd, F_FULLFSYNC) != -1)
            break;
#else // __APPLE__
        if (fsync(pLocalData->unix_fd) == 0)
            break;
#endif // __APPLE__

        switch (errno)
        {
        case EINTR:
            // Execution was interrupted by a signal, so restart.
            TRACE("fsync(%d) was interrupted. Restarting\n", pLocalData->unix_fd);
            break;

        default:
            palError = FILEGetLastErrorFromErrno();
            WARN("fsync(%d) failed with error %d\n", pLocalData->unix_fd, errno);
            break;
        }
    } while (NO_ERROR == palError);
#else // HAVE_FSYNC
    /* flush all buffers out to disk - there is no way to flush
       an individual file descriptor's buffers out. */
    sync();
#endif // HAVE_FSYNC else


InternalFlushFileBuffersExit:

    if (NULL != pLocalDataLock)
    {
        pLocalDataLock->ReleaseLock(pThread, FALSE);
    }

    if (NULL != pFileObject)
    {
        pFileObject->ReleaseReference(pThread);
    }

    return palError;
}


/*++
Function:
  FlushFileBuffers

See MSDN doc.
--*/
BOOL
PALAPI
FlushFileBuffers(
         IN HANDLE hFile)
{
    PAL_ERROR palError = NO_ERROR;
    CPalThread *pThread;

    PERF_ENTRY(FlushFileBuffers);
    ENTRY("FlushFileBuffers(hFile=%p)\n", hFile);

    pThread = InternalGetCurrentThread();

    palError = InternalFlushFileBuffers(
        pThread,
        hFile
        );

    if (NO_ERROR != palError)
    {
        pThread->SetLastError(palError);
    }

    LOGEXIT("FlushFileBuffers returns BOOL %d\n", NO_ERROR == palError);
    PERF_EXIT(FlushFileBuffers);
    return NO_ERROR == palError;
}

#define ENSURE_UNIQUE_NOT_ZERO \
    if ( uUniqueSeed == 0 ) \
    {\
        uUniqueSeed++;\
    }

/*++
 Function:
   GetTempFileNameA

uUnique is always 0.
 --*/
const int MAX_PREFIX        = 3;
const int MAX_SEEDSIZE      = 8; /* length of "unique portion of
                                   the string, plus extension(FFFF.TMP). */
static USHORT uUniqueSeed   = 0;
static BOOL IsInitialized   = FALSE;

UINT
PALAPI
GetTempFileNameA(
                 IN LPCSTR lpPathName,
                 IN LPCSTR lpPrefixString,
                 IN UINT   uUnique,
                 OUT LPSTR lpTempFileName)
{
    CPalThread *pThread;
    CHAR * full_name;
    PathCharString full_namePS;
    int length;
    CHAR * file_template;
    PathCharString file_templatePS;
    CHAR    chLastPathNameChar;

    HANDLE  hTempFile;
    UINT    uRet = 0;
    DWORD   dwError;
    USHORT  uLoopCounter = 0;

    PERF_ENTRY(GetTempFileNameA);
    ENTRY("GetTempFileNameA(lpPathName=%p (%s), lpPrefixString=%p (%s), uUnique=%u, "
          "lpTempFileName=%p)\n",  lpPathName?lpPathName:"NULL",  lpPathName?lpPathName:"NULL",
        lpPrefixString?lpPrefixString:"NULL",
        lpPrefixString?lpPrefixString:"NULL", uUnique,
        lpTempFileName?lpTempFileName:"NULL");

    pThread = InternalGetCurrentThread();
    if ( !IsInitialized )
    {
        uUniqueSeed = (USHORT)( time( NULL ) );

        /* On the off chance 0 is returned.
        0 being the error return code.  */
        ENSURE_UNIQUE_NOT_ZERO
        IsInitialized = TRUE;
    }

    if ( !lpPathName || *lpPathName == '\0' )
    {
       pThread->SetLastError( ERROR_DIRECTORY );
       goto done;
    }

    if ( NULL == lpTempFileName )
    {
        ERROR( "lpTempFileName cannot be NULL\n" );
        pThread->SetLastError( ERROR_INVALID_PARAMETER );
        goto done;
    }

    if ( strlen( lpPathName ) + MAX_SEEDSIZE + MAX_PREFIX >= MAX_LONGPATH )
    {
        WARN( "File names larger than MAX_LONGPATH (%d)!\n", MAX_LONGPATH );
        pThread->SetLastError( ERROR_FILENAME_EXCED_RANGE );
        goto done;
    }

    length = strlen(lpPathName) + MAX_SEEDSIZE + MAX_PREFIX + 10;
    file_template = file_templatePS.OpenStringBuffer(length);
    if (NULL == file_template)
    {
        pThread->SetLastError(ERROR_NOT_ENOUGH_MEMORY);
        goto done;
    }
    *file_template = '\0';
    strcat_s( file_template, file_templatePS.GetSizeOf(), lpPathName );
    file_templatePS.CloseBuffer(length);

    chLastPathNameChar = file_template[strlen(file_template)-1];
    if (chLastPathNameChar != '/')
    {
        strcat_s( file_template, file_templatePS.GetSizeOf(), "/" );
    }

    if ( lpPrefixString )
    {
        strncat_s( file_template, file_templatePS.GetSizeOf(), lpPrefixString, MAX_PREFIX );
    }
    strncat_s( file_template, file_templatePS.GetSizeOf(), "%.4x.TMP", MAX_SEEDSIZE );

    /* Create the file. */
    dwError = GetLastError();
    pThread->SetLastError( NOERROR );

    length = strlen(file_template) + MAX_SEEDSIZE + MAX_PREFIX;
    full_name = full_namePS.OpenStringBuffer(length);
    if (NULL == full_name)
    {
        pThread->SetLastError(ERROR_NOT_ENOUGH_MEMORY);
        goto done;
    }
    sprintf_s( full_name, full_namePS.GetSizeOf(), file_template, (0 == uUnique) ? uUniqueSeed : uUnique);
    full_namePS.CloseBuffer(length);

    hTempFile = CreateFileA( full_name, GENERIC_WRITE,
                             FILE_SHARE_READ, NULL, CREATE_NEW, 0, NULL );

    if (uUnique == 0)
    {
        /* The USHORT will overflow back to 0 if we go past
        65536 files, so break the loop after 65536 iterations.
        If the CreateFile call was not successful within that
        number of iterations, then there are no temp file names
        left for that directory. */
        while ( ERROR_PATH_NOT_FOUND != GetLastError() &&
                INVALID_HANDLE_VALUE == hTempFile && uLoopCounter < 0xFFFF )
        {
            uUniqueSeed++;
            ENSURE_UNIQUE_NOT_ZERO;

            pThread->SetLastError( NOERROR );
            sprintf_s( full_name, full_namePS.GetSizeOf(), file_template, uUniqueSeed );
            hTempFile = CreateFileA( full_name, GENERIC_WRITE,
                                    FILE_SHARE_READ, NULL, CREATE_NEW, 0, NULL );
            uLoopCounter++;

        }
    }

    /* Reset the error code.*/
    if ( NOERROR == GetLastError() )
    {
        pThread->SetLastError( dwError );
    }

    /* Windows sets ERROR_FILE_EXISTS,if there
    are no available temp files. */
    if ( INVALID_HANDLE_VALUE != hTempFile )
    {
        if (0 == uUnique)
        {
            uRet = uUniqueSeed;
            uUniqueSeed++;
            ENSURE_UNIQUE_NOT_ZERO;
        }
        else
        {
            uRet = uUnique;
        }

        if ( CloseHandle( hTempFile ) )
        {
            if (strcpy_s( lpTempFileName, MAX_LONGPATH, full_name ) != SAFECRT_SUCCESS)
            {
                ERROR( "strcpy_s failed!\n");
                pThread->SetLastError( ERROR_FILENAME_EXCED_RANGE );
                *lpTempFileName = '\0';
                uRet = 0;
            }
        }
        else
        {
            ASSERT( "Unable to close the handle %p\n", hTempFile );
            pThread->SetLastError( ERROR_INTERNAL_ERROR );
            *lpTempFileName = '\0';
            uRet = 0;
        }
    }
    else if ( INVALID_HANDLE_VALUE == hTempFile && uLoopCounter < 0xFFFF )
    {
        ERROR( "Unable to create temp file. \n" );
        uRet = 0;

        if ( ERROR_PATH_NOT_FOUND == GetLastError() )
        {
            /* CreateFile failed because it could not
            find the path. */
            pThread->SetLastError( ERROR_DIRECTORY );
        } /* else use the lasterror value from CreateFileA */
    }
    else
    {
        TRACE( "65535 files already exist in the directory. "
               "No temp files available for creation.\n" );
        pThread->SetLastError( ERROR_FILE_EXISTS );
    }

done:
    LOGEXIT("GetTempFileNameA returns UINT %u\n", uRet);
    PERF_EXIT(GetTempFileNameA);
    return uRet;

}

/*++
Function:
  GetTempFileNameW

uUnique is always 0.
--*/
UINT
PALAPI
GetTempFileNameW(
         IN LPCWSTR lpPathName,
         IN LPCWSTR lpPrefixString,
         IN UINT uUnique,
         OUT LPWSTR lpTempFileName)
{
    CPalThread *pThread;
    INT path_size = 0;
    INT prefix_size = 0;
    CHAR * full_name;
    CHAR * prefix_string;
    CHAR * tempfile_name = NULL;
    PathCharString full_namePS, prefix_stringPS;
    INT length = 0;
    UINT   uRet;

    PERF_ENTRY(GetTempFileNameW);
    ENTRY("GetTempFileNameW(lpPathName=%p (%S), lpPrefixString=%p (%S), uUnique=%u, "
          "lpTempFileName=%p)\n", lpPathName?lpPathName:W16_NULLSTRING, lpPathName?lpPathName:W16_NULLSTRING,
          lpPrefixString?lpPrefixString:W16_NULLSTRING,
          lpPrefixString?lpPrefixString:W16_NULLSTRING,uUnique, lpTempFileName);

    pThread = InternalGetCurrentThread();
    /* Sanity checks. */
    if ( !lpPathName || *lpPathName == '\0' )
    {
        pThread->SetLastError( ERROR_DIRECTORY );
        uRet = 0;
        goto done;
    }

    length = (PAL_wcslen(lpPathName)+1) * MaxWCharToAcpLengthFactor;
    full_name = full_namePS.OpenStringBuffer(length);
    if (NULL == full_name)
    {
        pThread->SetLastError(ERROR_NOT_ENOUGH_MEMORY);
        uRet = 0;
        goto done;
    }
    path_size = WideCharToMultiByte( CP_ACP, 0, lpPathName, -1, full_name,
                                     length, NULL, NULL );

    if( path_size == 0 )
    {
        full_namePS.CloseBuffer(0);
        DWORD dwLastError = GetLastError();
        ASSERT("WideCharToMultiByte failure! error is %d\n", dwLastError);
        pThread->SetLastError(ERROR_INTERNAL_ERROR);
        uRet = 0;
        goto done;
    }

    full_namePS.CloseBuffer(path_size - 1);

    if (lpPrefixString != NULL)
    {
        length = (PAL_wcslen(lpPrefixString)+1) * MaxWCharToAcpLengthFactor;
        prefix_string = prefix_stringPS.OpenStringBuffer(length);
        if (NULL == prefix_string)
        {
            pThread->SetLastError(ERROR_NOT_ENOUGH_MEMORY);
            uRet = 0;
            goto done;
        }
        prefix_size = WideCharToMultiByte( CP_ACP, 0, lpPrefixString, -1,
                                           prefix_string,
                                           MAX_LONGPATH - path_size - MAX_SEEDSIZE,
                                           NULL, NULL );

        if( prefix_size == 0 )
        {
            prefix_stringPS.CloseBuffer(0);
            DWORD dwLastError = GetLastError();
            ASSERT("WideCharToMultiByte failure! error is %d\n", dwLastError);
            pThread->SetLastError(ERROR_INTERNAL_ERROR);
            uRet = 0;
            goto done;
        }
        prefix_stringPS.CloseBuffer(prefix_size - 1);
    }

    tempfile_name = (char*)InternalMalloc(MAX_LONGPATH);
    if (tempfile_name == NULL)
    {
        pThread->SetLastError(ERROR_NOT_ENOUGH_MEMORY);
        uRet = 0;
        goto done;
    }

    uRet = GetTempFileNameA(full_name,
                            (lpPrefixString == NULL) ? NULL : prefix_string,
                            0, tempfile_name);
    if (uRet)
    {
        path_size = MultiByteToWideChar( CP_ACP, 0, tempfile_name, -1,
                                           lpTempFileName, MAX_LONGPATH );

        if (!path_size)
        {
            DWORD dwLastError = GetLastError();
            if (dwLastError == ERROR_INSUFFICIENT_BUFFER)
            {
                WARN("File names larger than MAX_PATH_FNAME (%d)! \n", MAX_LONGPATH);
                dwLastError = ERROR_FILENAME_EXCED_RANGE;
            }
            else
            {
                ASSERT("MultiByteToWideChar failure! error is %d", dwLastError);
                dwLastError = ERROR_INTERNAL_ERROR;
            }
            pThread->SetLastError(dwLastError);
            uRet = 0;
        }
    }

done:
    free(tempfile_name);

    LOGEXIT("GetTempFileNameW returns UINT %u\n", uRet);
    PERF_EXIT(GetTempFileNameW);
    return uRet;
}

/*++
Function:
  FILEGetLastErrorFromErrno

Convert errno into the appropriate win32 error and return it.
--*/
DWORD FILEGetLastErrorFromErrno( void )
{
    DWORD dwRet;

    switch(errno)
    {
    case 0:
        dwRet = ERROR_SUCCESS;
        break;
    case ENAMETOOLONG:
        dwRet = ERROR_FILENAME_EXCED_RANGE;
        break;
    case ENOTDIR:
        dwRet = ERROR_PATH_NOT_FOUND;
        break;
    case ENOENT:
        dwRet = ERROR_FILE_NOT_FOUND;
        break;
    case EACCES:
    case EPERM:
    case EROFS:
    case EISDIR:
        dwRet = ERROR_ACCESS_DENIED;
        break;
    case EEXIST:
        dwRet = ERROR_ALREADY_EXISTS;
        break;
    case ENOTEMPTY:
        dwRet = ERROR_DIR_NOT_EMPTY;
        break;
    case EBADF:
        dwRet = ERROR_INVALID_HANDLE;
        break;
    case ENOMEM:
        dwRet = ERROR_NOT_ENOUGH_MEMORY;
        break;
    case EBUSY:
        dwRet = ERROR_BUSY;
        break;
    case ENOSPC:
    case EDQUOT:
        dwRet = ERROR_DISK_FULL;
        break;
    case ELOOP:
        dwRet = ERROR_BAD_PATHNAME;
        break;
    case EIO:
        dwRet = ERROR_WRITE_FAULT;
        break;
    case EMFILE:
        dwRet = ERROR_TOO_MANY_OPEN_FILES;
        break;
    case ERANGE:
        dwRet = ERROR_BAD_PATHNAME;
        break;
    default:
        ERROR("unexpected errno %d (%s); returning ERROR_GEN_FAILURE\n",
              errno, strerror(errno));
        dwRet = ERROR_GEN_FAILURE;
    }

    TRACE("errno = %d (%s), LastError = %d\n", errno, strerror(errno), dwRet);

    return dwRet;
}

/*++
Function:
  DIRGetLastErrorFromErrno

Convert errno into the appropriate win32 error and return it.
--*/
DWORD DIRGetLastErrorFromErrno( void )
{
    if (errno == ENOENT)
        return ERROR_PATH_NOT_FOUND;
    else
        return FILEGetLastErrorFromErrno();
}


/*++
Function:
  CopyFileA

See MSDN doc.

Notes:
  There are several (most) error paths here that do not call SetLastError().
This is because we know that CreateFile, ReadFile, and WriteFile will do so,
and will have a much better idea of the specific error.
--*/
BOOL
PALAPI
CopyFileA(
      IN LPCSTR lpExistingFileName,
      IN LPCSTR lpNewFileName,
      IN BOOL bFailIfExists)
{
    CPalThread *pThread;
    HANDLE       hSource = INVALID_HANDLE_VALUE;
    HANDLE       hDest = INVALID_HANDLE_VALUE;
    DWORD        dwDestCreationMode;
    BOOL         bGood = FALSE;
    DWORD        dwSrcFileAttributes;
    struct stat  SrcFileStats;

    const int    buffer_size = 16*1024;
    char        *buffer = (char*)alloca(buffer_size);
    DWORD        bytes_read;
    DWORD        bytes_written;
    int          permissions;


    PERF_ENTRY(CopyFileA);
    ENTRY("CopyFileA(lpExistingFileName=%p (%s), lpNewFileName=%p (%s), bFailIfExists=%d)\n",
          lpExistingFileName?lpExistingFileName:"NULL",
          lpExistingFileName?lpExistingFileName:"NULL",
          lpNewFileName?lpNewFileName:"NULL",
          lpNewFileName?lpNewFileName:"NULL", bFailIfExists);

    pThread = InternalGetCurrentThread();
    if ( bFailIfExists )
    {
        dwDestCreationMode = CREATE_NEW;
    }
    else
    {
        dwDestCreationMode = CREATE_ALWAYS;
    }

    hSource = CreateFileA( lpExistingFileName,
               GENERIC_READ,
               FILE_SHARE_READ,
               NULL,
               OPEN_EXISTING,
               0,
               NULL );

    if ( hSource == INVALID_HANDLE_VALUE )
    {
        ERROR("CreateFileA failed for %s\n", lpExistingFileName);
        goto done;
    }

    /* Need to preserve the file attributes */
    dwSrcFileAttributes = GetFileAttributesA(lpExistingFileName);
    if (dwSrcFileAttributes == 0xffffffff)
    {
        ERROR("GetFileAttributesA failed for %s\n", lpExistingFileName);
        goto done;
    }

    /* Need to preserve the owner/group and chmod() flags */
    if (stat (lpExistingFileName, &SrcFileStats) == -1)
    {
        ERROR("stat() failed for %s\n", lpExistingFileName);
        pThread->SetLastError(FILEGetLastErrorFromErrnoAndFilename(lpExistingFileName));
        goto done;
    }

    hDest = CreateFileA( lpNewFileName,
             GENERIC_WRITE,
             FILE_SHARE_READ,
             NULL,
             dwDestCreationMode,
             0,
             NULL );

    if ( hDest == INVALID_HANDLE_VALUE )
    {
        ERROR("CreateFileA failed for %s\n", lpNewFileName);
        goto done;
    }

    // We don't set file attributes in CreateFile. The only attribute
    // that is reflected on disk in Unix is read-only, and we set that
    // here.
    permissions = (S_IRWXU | S_IRWXG | S_IRWXO);
    if ((dwSrcFileAttributes & FILE_ATTRIBUTE_READONLY) != 0)
    {
        permissions &= ~(S_IWUSR | S_IWGRP | S_IWOTH);
    }

    /* Make sure the new file has the same chmod() flags. */
    if (chmod(lpNewFileName, SrcFileStats.st_mode & permissions) == -1)
    {
        WARN ("chmod() failed to set mode 0x%x on new file\n",
              SrcFileStats.st_mode & permissions);
        pThread->SetLastError(FILEGetLastErrorFromErrnoAndFilename(lpNewFileName));
        goto done;
    }

    while( (bGood = ReadFile( hSource, buffer, buffer_size, &bytes_read, NULL ))
           && bytes_read > 0 )
    {
        bGood = ( WriteFile( hDest, buffer, bytes_read, &bytes_written, NULL )
          && bytes_written == bytes_read);
        if (!bGood) break;
    }

    if (!bGood)
    {
        ERROR("Copy failed\n");

        if ( !CloseHandle(hDest) ||
             !DeleteFileA(lpNewFileName) )
        {
            ERROR("Unable to clean up partial copy\n");
        }
        hDest = INVALID_HANDLE_VALUE;

        goto done;
    }

done:

    if ( hSource != INVALID_HANDLE_VALUE )
    {
        CloseHandle( hSource );
    }
    if ( hDest != INVALID_HANDLE_VALUE )
    {
        CloseHandle( hDest );
    }

    LOGEXIT("CopyFileA returns BOOL %d\n", bGood);
    PERF_EXIT(CopyFileA);
    return bGood;
}

PAL_ERROR
CorUnix::InternalCreatePipe(
    CPalThread *pThread,
    HANDLE *phReadPipe,
    HANDLE *phWritePipe,
    LPSECURITY_ATTRIBUTES lpPipeAttributes,
    DWORD nSize
    )
{
    PAL_ERROR palError = NO_ERROR;
    IPalObject *pReadFileObject = NULL;
    IPalObject *pReadRegisteredFile = NULL;
    IPalObject *pWriteFileObject = NULL;
    IPalObject *pWriteRegisteredFile = NULL;
    IDataLock *pDataLock = NULL;
    CFileProcessLocalData *pLocalData = NULL;
    CObjectAttributes oaFile(NULL, lpPipeAttributes);

    int readWritePipeDes[2] = {-1, -1};

    if ((phReadPipe == NULL) || (phWritePipe == NULL))
    {
        ERROR("One of the two parameters hReadPipe(%p) and hWritePipe(%p) is Null\n",phReadPipe,phWritePipe);
        palError = ERROR_INVALID_PARAMETER;
        goto InternalCreatePipeExit;
    }

    if ((lpPipeAttributes == NULL) ||
        (lpPipeAttributes->bInheritHandle == FALSE) ||
        (lpPipeAttributes->lpSecurityDescriptor != NULL))
    {
        ASSERT("invalid security attributes!\n");
        palError = ERROR_INVALID_PARAMETER;
        goto InternalCreatePipeExit;
    }

    if (pipe(readWritePipeDes) == -1)
    {
        ERROR("pipe() call failed errno:%d (%s) \n", errno, strerror(errno));
        palError = ERROR_INTERNAL_ERROR;
        goto InternalCreatePipeExit;
    }

    /* enable close-on-exec for both pipes; if one gets passed to CreateProcess
       it will be "uncloseonexeced" in order to be inherited */
    if(-1 == fcntl(readWritePipeDes[0],F_SETFD,FD_CLOEXEC))
    {
        ASSERT("can't set close-on-exec flag; fcntl() failed. errno is %d "
             "(%s)\n", errno, strerror(errno));
        palError = ERROR_INTERNAL_ERROR;
        goto InternalCreatePipeExit;
    }
    if(-1 == fcntl(readWritePipeDes[1],F_SETFD,FD_CLOEXEC))
    {
        ASSERT("can't set close-on-exec flag; fcntl() failed. errno is %d "
             "(%s)\n", errno, strerror(errno));
        palError = ERROR_INTERNAL_ERROR;
        goto InternalCreatePipeExit;
    }

    //
    // Setup the object for the read end of the pipe
    //

    palError = g_pObjectManager->AllocateObject(
        pThread,
        &otFile,
        &oaFile,
        &pReadFileObject
        );

    if (NO_ERROR != palError)
    {
        goto InternalCreatePipeExit;
    }

    palError = pReadFileObject->GetProcessLocalData(
        pThread,
        WriteLock,
        &pDataLock,
        reinterpret_cast<void**>(&pLocalData)
        );

    if (NO_ERROR != palError)
    {
        goto InternalCreatePipeExit;
    }

    pLocalData->inheritable = TRUE;
    pLocalData->open_flags = O_RDONLY;

    //
    // After storing the file descriptor in the object's local data
    // we want to clear it from the array to prevent a possible double
    // close if an error occurs.
    //

    pLocalData->unix_fd = readWritePipeDes[0];
    readWritePipeDes[0] = -1;

    pDataLock->ReleaseLock(pThread, TRUE);
    pDataLock = NULL;

    //
    // Setup the object for the write end of the pipe
    //

    palError = g_pObjectManager->AllocateObject(
        pThread,
        &otFile,
        &oaFile,
        &pWriteFileObject
        );

    if (NO_ERROR != palError)
    {
        goto InternalCreatePipeExit;
    }

    palError = pWriteFileObject->GetProcessLocalData(
        pThread,
        WriteLock,
        &pDataLock,
        reinterpret_cast<void**>(&pLocalData)
        );

    if (NO_ERROR != palError)
    {
        goto InternalCreatePipeExit;
    }

    pLocalData->inheritable = TRUE;
    pLocalData->open_flags = O_WRONLY;

    //
    // After storing the file descriptor in the object's local data
    // we want to clear it from the array to prevent a possible double
    // close if an error occurs.
    //

    pLocalData->unix_fd = readWritePipeDes[1];
    readWritePipeDes[1] = -1;

    pDataLock->ReleaseLock(pThread, TRUE);
    pDataLock = NULL;

    //
    // Register the pipe objects
    //

    palError = g_pObjectManager->RegisterObject(
        pThread,
        pReadFileObject,
        &aotFile,
        phReadPipe,
        &pReadRegisteredFile
        );

    //
    // pReadFileObject is invalidated by the call to RegisterObject, so NULL it
    // out here to ensure that we don't try to release a reference on
    // it down the line.
    //

    pReadFileObject = NULL;

    if (NO_ERROR != palError)
    {
        goto InternalCreatePipeExit;
    }

    palError = g_pObjectManager->RegisterObject(
        pThread,
        pWriteFileObject,
        &aotFile,
        phWritePipe,
        &pWriteRegisteredFile
        );

    //
    // pWriteFileObject is invalidated by the call to RegisterObject, so NULL it
    // out here to ensure that we don't try to release a reference on
    // it down the line.
    //

    pWriteFileObject = NULL;

InternalCreatePipeExit:

    if (NO_ERROR != palError)
    {
        if (-1 != readWritePipeDes[0])
        {
            close(readWritePipeDes[0]);
        }

        if (-1 != readWritePipeDes[1])
        {
            close(readWritePipeDes[1]);
        }
    }

    if (NULL != pReadFileObject)
    {
        pReadFileObject->ReleaseReference(pThread);
    }

    if (NULL != pReadRegisteredFile)
    {
        pReadRegisteredFile->ReleaseReference(pThread);
    }

    if (NULL != pWriteFileObject)
    {
        pWriteFileObject->ReleaseReference(pThread);
    }

    if (NULL != pWriteRegisteredFile)
    {
        pWriteRegisteredFile->ReleaseReference(pThread);
    }

    return palError;
}

/*++
Function:
  CreatePipe

See MSDN doc.
--*/
PALIMPORT
BOOL
PALAPI
CreatePipe(
        OUT PHANDLE hReadPipe,
        OUT PHANDLE hWritePipe,
        IN LPSECURITY_ATTRIBUTES lpPipeAttributes,
        IN DWORD nSize)
{
    PAL_ERROR palError;
    CPalThread *pThread;

    PERF_ENTRY(CreatePipe);
    ENTRY("CreatePipe(hReadPipe:%p, hWritePipe:%p, lpPipeAttributes:%p, nSize:%d\n",
          hReadPipe, hWritePipe, lpPipeAttributes, nSize);

    pThread = InternalGetCurrentThread();

    palError = InternalCreatePipe(
        pThread,
        hReadPipe,
        hWritePipe,
        lpPipeAttributes,
        nSize
        );

    if (NO_ERROR != palError)
    {
        pThread->SetLastError(palError);
    }

    LOGEXIT("CreatePipe return %s\n", NO_ERROR == palError ? "TRUE":"FALSE");
    PERF_EXIT(CreatePipe);
    return NO_ERROR == palError;
}

/*++
init_std_handle [static]

utility function for FILEInitStdHandles. do the work that is common to all
three standard handles

Parameters:
    HANDLE pStd : Defines which standard handle to assign
    FILE *stream        : file stream to associate to handle

Return value:
    handle for specified stream, or INVALID_HANDLE_VALUE on failure
--*/
static HANDLE init_std_handle(HANDLE * pStd, FILE *stream)
{
    CPalThread *pThread = InternalGetCurrentThread();
    PAL_ERROR palError = NO_ERROR;
    IPalObject *pFileObject = NULL;
    IPalObject *pRegisteredFile = NULL;
    IDataLock *pDataLock = NULL;
    CFileProcessLocalData *pLocalData = NULL;
    CObjectAttributes oa;

    HANDLE hFile = INVALID_HANDLE_VALUE;
    int new_fd = -1;

    /* duplicate the FILE *, so that we can fclose() in FILECloseHandle without
       closing the original */
    new_fd = fcntl(fileno(stream), F_DUPFD_CLOEXEC, 0); // dup, but with CLOEXEC
    if(-1 == new_fd)
    {
        ERROR("dup() failed; errno is %d (%s)\n", errno, strerror(errno));
        goto done;
    }

    palError = g_pObjectManager->AllocateObject(
        pThread,
        &otFile,
        &oa,
        &pFileObject
        );

    if (NO_ERROR != palError)
    {
        goto done;
    }

    palError = pFileObject->GetProcessLocalData(
        pThread,
        WriteLock,
        &pDataLock,
        reinterpret_cast<void**>(&pLocalData)
        );

    if (NO_ERROR != palError)
    {
        goto done;
    }

    pLocalData->inheritable = TRUE;
    pLocalData->unix_fd = new_fd;
    pLocalData->open_flags = 0;
    pLocalData->open_flags_deviceaccessonly = FALSE;

    //
    // We've finished initializing our local data, so release that lock
    //

    pDataLock->ReleaseLock(pThread, TRUE);
    pDataLock = NULL;

    palError = g_pObjectManager->RegisterObject(
        pThread,
        pFileObject,
        &aotFile,
        &hFile,
        &pRegisteredFile
        );

    //
    // pFileObject is invalidated by the call to RegisterObject, so NULL it
    // out here to ensure that we don't try to release a reference on
    // it down the line.
    //

    pFileObject = NULL;

done:

    if (NULL != pDataLock)
    {
        pDataLock->ReleaseLock(pThread, TRUE);
    }

    if (NULL != pFileObject)
    {
        pFileObject->ReleaseReference(pThread);
    }

    if (NULL != pRegisteredFile)
    {
        pRegisteredFile->ReleaseReference(pThread);
    }

    if (NO_ERROR == palError)
    {
        *pStd = hFile;
    }
    else if (-1 != new_fd)
    {
        close(new_fd);
    }

    return hFile;
}


/*++
FILEInitStdHandles

Create handle objects for stdin, stdout and stderr

(no parameters)

Return value:
    TRUE on success, FALSE on failure
--*/
BOOL FILEInitStdHandles(void)
{
    HANDLE stdin_handle;
    HANDLE stdout_handle;
    HANDLE stderr_handle;

    TRACE("creating handle objects for stdin, stdout, stderr\n");

    stdin_handle = init_std_handle(&pStdIn, stdin);
    if(INVALID_HANDLE_VALUE == stdin_handle)
    {
        ERROR("failed to create stdin handle\n");
        goto fail;
    }

    stdout_handle = init_std_handle(&pStdOut, stdout);
    if(INVALID_HANDLE_VALUE == stdout_handle)
    {
        ERROR("failed to create stdout handle\n");
        CloseHandle(stdin_handle);
        goto fail;
    }

    stderr_handle = init_std_handle(&pStdErr, stderr);
    if(INVALID_HANDLE_VALUE == stderr_handle)
    {
        ERROR("failed to create stderr handle\n");
        CloseHandle(stdin_handle);
        CloseHandle(stdout_handle);
        goto fail;
    }
    return TRUE;

fail:
    pStdIn = INVALID_HANDLE_VALUE;
    pStdOut = INVALID_HANDLE_VALUE;
    pStdErr = INVALID_HANDLE_VALUE;
    return FALSE;
}
