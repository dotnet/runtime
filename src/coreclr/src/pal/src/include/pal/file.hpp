//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
//

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
        IFileLockController *pLockController;

        int  unix_fd;
        DWORD dwDesiredAccess; /* Unix assumes files are always opened for reading.
                                  In Windows we can open a file for writing only */                             
        int  open_flags;       /* stores Unix file creation flags */
        BOOL open_flags_deviceaccessonly;
        char unix_filename[MAXPATHLEN];
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
    InternalGetFileType(
        CPalThread *pThread,
        HANDLE hFile,
        DWORD *pdwFileType
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
    InternalLockFile(
        CPalThread *pThread,
        HANDLE hFile,
        DWORD dwFileOffsetLow,
        DWORD dwFileOffsetHigh,
        DWORD nNumberOfBytesToLockLow,
        DWORD nNumberOfBytesToLockHigh
        );

    PAL_ERROR
    InternalUnlockFile(
        CPalThread *pThread,
        HANDLE hFile,
        DWORD dwFileOffsetLow,
        DWORD dwFileOffsetHigh,
        DWORD nNumberOfBytesToUnlockLow,
        DWORD nNumberOfBytesToUnlockHigh
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

    PAL_ERROR
    InternalSetFileTime(
        CPalThread *pThread,
        IN HANDLE hFile,
        IN CONST FILETIME *lpCreationTime,
        IN CONST FILETIME *lpLastAccessTime,
        IN CONST FILETIME *lpLastWriteTime
        );

    PAL_ERROR
    InternalGetFileTime(
        CPalThread *pThread,
        IN HANDLE hFile,
        OUT LPFILETIME lpCreationTime,
        OUT LPFILETIME lpLastAccessTime,
        OUT LPFILETIME lpLastWriteTime
        );

    /*++
      InternalCanonicalizeRealPath
      Wraps realpath() to hide platform differences. See the man page for
      realpath(3) for details of how realpath() works.

      On systems on which realpath() allows the last path component to not
      exist, this is a straight thunk through to realpath(). On other
      systems, we remove the last path component, then call realpath().

      cch is the size of lpBuffer and has to be atleast PATH_MAX (since 
      realpath() requires the buffer to be atleast PATH_MAX).
      --*/
    PAL_ERROR
    InternalCanonicalizeRealPath(
        CPalThread *pThread,
        LPCSTR lpUnixPath,
        LPSTR lpBuffer,
        DWORD cch
        );  

    /*++
    InternalGetcwd     
    Wraps getcwd so the thread calling it can't be suspended holding an internal lock.
    --*/
    char *
    InternalGetcwd(
        CPalThread *pthrCurrent,
        char *szBuf,
        size_t nSize
        );

    /*++
    InternalFflush    
    Wraps fflush so the thread calling it can't be suspended holding an internal lock.
    --*/
    int
    InternalFflush(
        CPalThread *pthrCurrent,
        FILE * stream
        );

    /*++
    InternalMkstemp     
    Wraps mkstemp so the thread calling it can't be suspended holding an internal lock.
    --*/
    int 
    InternalMkstemp(
        CPalThread *pthrCurrent,
        char *szNameTemplate
        );

    /*++
    InternalUnlink
    Wraps unlink so the thread calling it can't be suspended holding an internal lock.
    --*/
    int
    InternalUnlink(
        CPalThread *pthrCurrent,
        const char *szPath
        );


    /*++
    InternalDeleteFile
    Wraps SYS_delete so the thread calling it can't be suspended holding an internal lock.
    --*/
    int 
    InternalDeleteFile(
        CPalThread *pthrCurrent,
        const char *szPath
        );

    /*++
    InternalRename
    Wraps rename so the thread calling it can't be suspended holding an internal lock.
    --*/
    int 
    InternalRename(
        CPalThread *pthrCurrent, 
        const char *szOldName, 
        const char *szNewName
        );

    /*++
    InternalFgets
    Wraps fgets so the thread calling it can't be suspended holding an internal lock.
    --*/
    char *
    InternalFgets(
        CPalThread *pthrCurrent,
        char *sz,
        int nSize,
        FILE *f,
        bool fTextMode
        );

    /*++
    InternalFwrite
    Wraps fwrite so the thread calling it can't be suspended holding an internal lock.
    --*/
    size_t
    InternalFwrite(
        CPalThread *pthrCurrent,
        const void *pvBuffer, 
        size_t nSize, 
        size_t nCount,     
        FILE *f,
        INT *pnErrorCode
        );

    /*++
    InternalOpen
    Wraps open so the thread calling it can't be suspended holding an internal lock.
    --*/
    int
    InternalOpen(
        CPalThread *pthrCurrent,
        const char *szFilename,
        int nFlags,
        ...
        );

    /*++
    InternalFseek
    Wraps fseek so the thread calling it can't be suspended holding an internal lock.
    --*/
    int
    InternalFseek(
        CPalThread *pthrCurrent,
        FILE *f,
        long lOffset,
        int nWhence
        );
}

extern "C"
{

//
// These routines should all be separated out into something along the lines
// of fileutils.* (instead of being commingled with the core file object
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
  FileDosToUnixPathW

Abstract:
  Change a DOS path to a Unix path. Replace '\' by '/'.

Parameter:
  IN/OUT lpPath: path to be modified
  --*/
void
FILEDosToUnixPathW(LPWSTR lpPath);

/*++
Function:
  FileUnixToDosPathA

Abstract:
  Change a Unix path to a DOS path. Replace '/' by '\'.

Parameter:
  IN/OUT lpPath: path to be modified
--*/
void 
FILEUnixToDosPathA(LPSTR lpPath);


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
  FILEGetFileNameFromFullPath

Given a full path, return a pointer to the first char of the filename part.
--*/
LPCSTR FILEGetFileNameFromFullPathA( LPCSTR lpFullPath );

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
FILEGetFileNameFromSymLink

Input paramters:

source  = path to the file on input, path to the file with all 
          symbolic links traversed on return

Note: Assumes the maximum size of the source is MAX_LONGPATH

Return value:
    TRUE on success, FALSE on failure
--*/
BOOL FILEGetFileNameFromSymLink(char *source);

/*++

Function : 
    FILEGetProperNotFoundError
    
Returns the proper error code, based on the 
Windows behavoir.

    IN LPSTR lpPath - The path to check.
    LPDWORD lpErrorCode - The error to set.
*/
void FILEGetProperNotFoundError( LPSTR lpPath, LPDWORD lpErrorCode );

}

#endif /* _PAL_FILE_HPP_ */

