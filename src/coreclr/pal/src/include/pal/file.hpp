// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*++



Module Name:

    include/pal/file.hpp

Abstract:
    Header file for file utility functions.

Revision History:



--*/

#ifndef _PAL_FILE_HPP_
#define _PAL_FILE_HPP_

#include "corunix.hpp"
#include "pal/stackstring.hpp"
#include <sys/types.h>
#include <sys/param.h>
#include <dirent.h>


namespace CorUnix
{
    extern CObjectType otFile;
    extern CAllowedObjectTypes aotFile;

    class CFileProcessLocalData
    {
    public:
        int  unix_fd;
        int  open_flags;       /* stores Unix file creation flags */
        BOOL open_flags_deviceaccessonly;
        CHAR *unix_filename;
        BOOL inheritable;
    };

    PAL_ERROR
    InternalCreateFile(
        CPalThread *pThread,
        LPCSTR lpFileName,
        DWORD dwDesiredAccess,
        DWORD dwShareMode,
        LPSECURITY_ATTRIBUTES lpSecurityAttributes,
        DWORD dwCreationDisposition,
        DWORD dwFlagsAndAttributes,
        HANDLE hTemplateFile,
        HANDLE *pFileHandle
        );

    PAL_ERROR
    InternalWriteFile(
        CPalThread *pThread,
        HANDLE hFile,
        LPCVOID lpBuffer,
        DWORD nNumberOfBytesToWrite,
        LPDWORD lpNumberOfBytesWritten,
        LPOVERLAPPED lpOverlapped
        );

    PAL_ERROR
    InternalReadFile(
        CPalThread *pThread,
        HANDLE hFile,
        LPVOID lpBuffer,
        DWORD nNumberOfBytesToRead,
        LPDWORD lpNumberOfBytesRead,
        LPOVERLAPPED lpOverlapped
        );

    PAL_ERROR
    InternalSetEndOfFile(
        CPalThread *pThread,
        HANDLE hFile
        );

    PAL_ERROR
    InternalGetFileSize(
        CPalThread *pThread,
        HANDLE hFile,
        DWORD *pdwFileSizeLow,
        DWORD *pdwFileSizeHigh
        );

    PAL_ERROR
    InternalFlushFileBuffers(
        CPalThread *pThread,
        HANDLE hFile
        );

    PAL_ERROR
    InternalCreatePipe(
        CPalThread *pThread,
        HANDLE *phReadPipe,
        HANDLE *phWritePipe,
        LPSECURITY_ATTRIBUTES lpPipeAttributes,
        DWORD nSize
        );

    PAL_ERROR
    InternalSetFilePointer(
        CPalThread *pThread,
        HANDLE hFile,
        LONG lDistanceToMove,
        PLONG lpDistanceToMoveHigh,
        DWORD dwMoveMethod,
        PLONG lpNewFilePointerLow
        );

    BOOL
    RealPathHelper(LPCSTR lpUnixPath, PathCharString& lpBuffer);
    /*++
      InternalCanonicalizeRealPath
      Wraps realpath() to hide platform differences. See the man page for
      realpath(3) for details of how realpath() works.

      On systems on which realpath() allows the last path component to not
      exist, this is a straight thunk through to realpath(). On other
      systems, we remove the last path component, then call realpath().
      --*/
    PAL_ERROR
    InternalCanonicalizeRealPath(
        LPCSTR lpUnixPath,
        PathCharString& lpBuffer
        );

    /*++
    InternalFgets
    Wraps fgets
    --*/
    char *
    InternalFgets(
        char *sz,
        int nSize,
        FILE *f,
        bool fTextMode
        );

    /*++
    InternalFwrite
    Wraps fwrite
    --*/
    size_t
    InternalFwrite(
        const void *pvBuffer,
        size_t nSize,
        size_t nCount,
        FILE *f,
        INT *pnErrorCode
        );

    /*++
    InternalOpen
    Wraps open
    --*/
    int
    InternalOpen(
        const char *szFilename,
        int nFlags,
        ...
        );
}

extern "C"
{

//
// These routines should all be separated out into something along the lines
// of fileutils.* (instead of being comingled with the core file object
// code).
//

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
void FILECanonicalizePath(LPSTR lpUnixPath);

/*++
Function:
  FileDosToUnixPathA

Abstract:
  Change a DOS path to a Unix path. Replace '\' by '/'.

Parameter:
  IN/OUT lpPath: path to be modified
--*/
void
FILEDosToUnixPathA(LPSTR lpPath);

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
                     LPSTR  lpBuffer );

/*++
Function:
  FILEGetLastErrorFromErrno

Convert errno into the appropriate win32 error and return it.
--*/
DWORD FILEGetLastErrorFromErrno( void );

/*++
FILEInitStdHandles

Create handle objects for stdin, stdout and stderr

(no parameters)

Return value:
    TRUE on success, FALSE on failure
--*/
BOOL FILEInitStdHandles(void);

/*++
FILECleanupStdHandles

Close primary handles for stdin, stdout and stderr

(no parameters, no return value)
--*/
void FILECleanupStdHandles(void);

/*++

Function :
    FILEGetProperNotFoundError

Returns the proper error code, based on the
Windows behavoir.

    IN LPSTR lpPath - The path to check.
    LPDWORD lpErrorCode - The error to set.
*/
void FILEGetProperNotFoundError( LPCSTR lpPath, LPDWORD lpErrorCode );

}

#endif /* _PAL_FILE_HPP_ */

