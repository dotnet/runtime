// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#ifndef _PAL_SHARED_MEMORY_INL_
#define _PAL_SHARED_MEMORY_INL_

#include "sharedmemory.h"

#include "dbgmsg.h"

template<SIZE_T DestinationByteCount, SIZE_T SourceByteCount>
SIZE_T SharedMemoryHelpers::CopyString(
    char(&destination)[DestinationByteCount],
    SIZE_T destinationStartOffset,
    const char(&source)[SourceByteCount])
{
    return CopyString(destination, destinationStartOffset, source, SourceByteCount - 1);
}

template<SIZE_T DestinationByteCount>
SIZE_T SharedMemoryHelpers::CopyString(
    char(&destination)[DestinationByteCount],
    SIZE_T destinationStartOffset,
    LPCSTR source,
    SIZE_T sourceCharCount)
{
    _ASSERTE(destinationStartOffset <= DestinationByteCount);
    _ASSERTE(sourceCharCount < DestinationByteCount - destinationStartOffset);

    memcpy_s(&destination[destinationStartOffset], DestinationByteCount - destinationStartOffset, source, sourceCharCount + 1);
    return destinationStartOffset + sourceCharCount;
}

#endif // !_PAL_SHARED_MEMORY_INL_
