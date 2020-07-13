// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
