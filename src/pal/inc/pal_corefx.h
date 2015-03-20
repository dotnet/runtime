//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

/*++

Module Name:

    pal_corefx.h

Abstract:

    Header file for functions meant to be consumed by the CoreFX libraries.

--*/

#include "pal.h"

#ifndef __PAL_COREFX_H__
#define __PAL_COREFX_H__

#ifdef  __cplusplus
extern "C" {
#endif

PALIMPORT
int
PALAPI
EnsureOpenSslInitialized();

PALIMPORT
int
PALAPI
ForkAndExecProcess(
           const char* filename,
           char* const argv[],
           char* const envp[],
           const char* cwd,
           int redirectStdin,
           int redirectStdout,
           int redirectStderr,
           int* childPid,
           int* stdinFd,
           int* stdoutFd,
           int* stderrFd);

struct fileinfo {
    int32_t  flags;   /* flags for testing if some members are present */
    int32_t  mode;    /* protection */
    int32_t  uid;     /* user ID of owner */
    int32_t  gid;     /* group ID of owner */
    int64_t  size;    /* total size, in bytes */
    int64_t  atime;   /* time of last access */
    int64_t  mtime;   /* time of last modification */
    int64_t  ctime;   /* time of last status change */
    int64_t  btime;   /* time the file was created (birthtime) */
};

#define FILEINFO_FLAGS_NONE 0x0
#define FILEINFO_FLAGS_HAS_BTIME 0x1

PALIMPORT
int
PALAPI
GetFileInformationFromPath(
    const char* path,
    struct fileinfo* buf);

PALIMPORT
int
PALAPI
GetFileInformationFromFd(
    int fd,
    struct fileinfo* buf);

#ifdef  __cplusplus
} // extern "C"
#endif

#endif // __PAL_COREFX_H__
