/**
 * \file
 * System.Native PAL internal calls (Android)
 * Adapter code between the Mono runtime and the CoreFX Platform Abstraction Layer (PAL)
 * Copyright 2018 Microsoft
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
*/

#include "pal_compiler.h"

/**
 * Reads the number of bytes specified into the provided buffer from the specified, opened file descriptor.
 *
 * Returns the number of bytes read on success; otherwise, -1 is returned an errno is set.
 *
 * Note - on fail. the position of the stream may change depending on the platform; consult man 2 read for more info
 */
DLLEXPORT int32_t SystemNative_Read(intptr_t fd, void* buffer, int32_t bufferSize);
