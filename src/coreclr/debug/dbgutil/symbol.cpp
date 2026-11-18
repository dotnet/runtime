// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <dbgutil.h>
#if !defined(HOST_WASM) && (defined(HOST_WINDOWS) || defined(HOST_APPLE) || defined(__linux__))
#include "elfreader.h"
#include "machoreader.h"

enum class TargetOperatingSystem
{
    Unknown,
    Windows,
    Linux,
    OSX
};

static TargetOperatingSystem
GetTargetOperatingSystem(ICorDebugDataTarget* dataTarget, uint64_t baseAddress)
{
    CorDebugPlatform platform;
    if (FAILED(dataTarget->GetPlatform(&platform)))
    {
        return TargetOperatingSystem::Unknown;
    }
    switch (platform)
    {
        case CORDB_PLATFORM_WINDOWS_X86:
        case CORDB_PLATFORM_WINDOWS_AMD64:
        case CORDB_PLATFORM_WINDOWS_ARM64:
            return TargetOperatingSystem::Windows;
        default:
            break;
    }

    BYTE magic[4];
    ULONG32 read = 0;
    if (FAILED(dataTarget->ReadVirtual(baseAddress, magic, sizeof(magic), &read)) || read != sizeof(magic))
    {
        return TargetOperatingSystem::Unknown;
    }
    if (memcmp(magic, "\177ELF", sizeof(magic)) == 0)
    {
        return TargetOperatingSystem::Linux;
    }
    if (memcmp(magic, "\xcf\xfa\xed\xfe", sizeof(magic)) == 0)
    {
        return TargetOperatingSystem::OSX;
    }
    return TargetOperatingSystem::Unknown;
}
#endif

extern "C" bool
TryGetSymbol(ICorDebugDataTarget* dataTarget, uint64_t baseAddress, const char* symbolName, uint64_t* symbolAddress)
{
    if (dataTarget == nullptr || symbolName == nullptr || symbolAddress == nullptr)
    {
        return false;
    }
    *symbolAddress = 0;

#if !defined(HOST_WASM) && (defined(HOST_WINDOWS) || defined(HOST_APPLE) || defined(__linux__))
    switch (GetTargetOperatingSystem(dataTarget, baseAddress))
    {
        case TargetOperatingSystem::Windows:
            return PEReader::TryLookupSymbol(dataTarget, baseAddress, symbolName, symbolAddress);
        case TargetOperatingSystem::Linux:
            return TryGetElfSymbol(dataTarget, baseAddress, symbolName, symbolAddress);
        case TargetOperatingSystem::OSX:
            return TryGetMachOSymbol(dataTarget, baseAddress, symbolName, symbolAddress);
        default:
            return false;
    }
#else
    return false;
#endif
}
