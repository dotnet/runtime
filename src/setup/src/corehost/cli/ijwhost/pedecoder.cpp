// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#include "pedecoder.h"
#include "corhdr.h"

bool PEDecoder::HasManagedEntryPoint() const
{
    ULONG flags = GetCorHeader()->Flags;
    return (!(flags & COMIMAGE_FLAGS_NATIVE_ENTRYPOINT) &&
            (!IsNilToken(GetCorHeader()->EntryPointToken)));
}

IMAGE_COR_VTABLEFIXUP *PEDecoder::GetVTableFixups(std::size_t *pCount) const
{
    IMAGE_DATA_DIRECTORY *pDir = &GetCorHeader()->VTableFixups;

    if (pCount != NULL)
        *pCount = pDir->Size / sizeof(IMAGE_COR_VTABLEFIXUP);

    return (IMAGE_COR_VTABLEFIXUP*)(GetDirectoryData(pDir));
}

bool PEDecoder::HasNativeEntryPoint() const
{
    DWORD flags = GetCorHeader()->Flags;
    return ((flags & COMIMAGE_FLAGS_NATIVE_ENTRYPOINT) &&
            (GetCorHeader()->EntryPointToken != 0));
}

void *PEDecoder::GetNativeEntryPoint() const
{
    return ((void *) GetRvaData(GetCorHeader()->EntryPointToken));
}
