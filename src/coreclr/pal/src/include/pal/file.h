// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*++



Module Name:

    include/pal/file.h

Abstract:
    Header file for file utility functions.

Revision History:



--*/

#ifndef _PAL_FILE_H_
#define _PAL_FILE_H_

#include "pal/shmemory.h"
#include "pal/stackstring.hpp"
#include <sys/types.h>
#include <dirent.h>
#include <glob.h>

#ifdef __cplusplus
extern "C"
{
#endif // __cplusplus

typedef struct _find_handle
{
    struct _find_handle *self_addr; /* for pointer verification */

    char   dir[_MAX_DIR];
    char   fname[MAX_PATH_FNAME]; /* includes extension */
    glob_t gGlob;
    char   **next;
} find_obj;

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
Function:
  DIRGetLastErrorFromErrno

Convert errno into the appropriate win32 error and return it.
--*/
DWORD DIRGetLastErrorFromErrno( void );

/*++
FILEInitStdHandles

Create handle objects for stdin, stdout and stderr

(no parameters)

Return value:
    TRUE on success, FALSE on failure
--*/
BOOL FILEInitStdHandles(void);

/*++

Function :
    FILEGetProperNotFoundError

Returns the proper error code, based on the
Windows behavoir.

    IN LPSTR lpPath - The path to check.
    LPDWORD lpErrorCode - The error to set.
*/
void FILEGetProperNotFoundError( LPCSTR lpPath, LPDWORD lpErrorCode );

#ifdef __cplusplus
}
#endif // __cplusplus

#endif /* _PAL_FILE_H_ */

