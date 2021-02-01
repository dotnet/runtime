// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef _PAL_SHARED_MEMORY_INL_
#define _PAL_SHARED_MEMORY_INL_

#include "sharedmemory.h"

#include "dbgmsg.h"

#include <string.h>

template<SIZE_T SuffixByteCount>
void SharedMemoryHelpers::BuildSharedFilesPath(
    PathCharString& destination,
    const char (&suffix)[SuffixByteCount])
{
    BuildSharedFilesPath(destination, suffix, SuffixByteCount - 1);
}

#endif // !_PAL_SHARED_MEMORY_INL_
