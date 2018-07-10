/**
 * \file
 * System.Native PAL internal calls (Android)
 * Adapter code between the Mono runtime and the CoreFX Platform Abstraction Layer (PAL)
 * Copyright 2018 Microsoft
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
*/

#include <unistd.h>
#include <assert.h>

#include "pal_compiler.h"
#include "pal_config.h"
#include "pal_errno.h"
#include "pal_utilities.h"

#include "pal-android.h"

int32_t SystemNative_Read(intptr_t fd, void* buffer, int32_t bufferSize)
{
    assert(buffer != NULL || bufferSize == 0);
    assert(bufferSize >= 0);

    if (bufferSize < 0)
    {
        errno = EINVAL;
        return -1;
    }

    ssize_t count;
    count = read(ToFileDescriptor(fd), buffer, (uint32_t)bufferSize);

    assert(count >= -1 && count <= bufferSize);
    return (int32_t)count;
}
