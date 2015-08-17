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

#ifdef  __cplusplus
} // extern "C"
#endif

#endif // __PAL_COREFX_H__
