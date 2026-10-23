// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <windows.h>
#include <assert.h>
#include <new>
#include <string.h>
#include "minipal.h"

typedef void* (WINAPI* VirtualAlloc2Fn)(
    HANDLE process,
    void* address,
    SIZE_T size,
    ULONG allocationType,
    ULONG protection,
    MEM_EXTENDED_PARAMETER* parameters,
    ULONG parameterCount);

typedef void* (WINAPI* MapViewOfFile3Fn)(
    HANDLE mapping,
    HANDLE process,
    void* address,
    ULONG64 offset,
    SIZE_T size,
    ULONG allocationType,
    ULONG protection,
    MEM_EXTENDED_PARAMETER* parameters,
    ULONG parameterCount);

static INIT_ONCE s_placeholderInitialization = INIT_ONCE_STATIC_INIT;
static VirtualAlloc2Fn s_virtualAlloc2 = nullptr;
static MapViewOfFile3Fn s_mapViewOfFile3 = nullptr;

static BOOL CALLBACK InitializePlaceholderMapping(PINIT_ONCE, void*, void**)
{
    // Use the API-set contract because kernel32.dll does not directly export these APIs on all supported systems.
    HMODULE module = LoadLibraryExW(L"api-ms-win-core-memory-l1-1-6.dll", nullptr, LOAD_LIBRARY_SEARCH_SYSTEM32);
    if (module != nullptr)
    {
        s_virtualAlloc2 = reinterpret_cast<VirtualAlloc2Fn>(GetProcAddress(module, "VirtualAlloc2"));
        s_mapViewOfFile3 = reinterpret_cast<MapViewOfFile3Fn>(GetProcAddress(module, "MapViewOfFile3"));
    }

    // Cache unavailable APIs as well, rather than retrying initialization on each call.
    return TRUE;
}

bool VMToOSInterface::SupportsPlaceholderMapping()
{
    return InitOnceExecuteOnce(&s_placeholderInitialization, InitializePlaceholderMapping, nullptr, nullptr) &&
        s_virtualAlloc2 != nullptr && s_mapViewOfFile3 != nullptr;
}

static DWORD GetNativeProtection(VMToOSInterface::MemoryProtection protection)
{
    switch (protection)
    {
        case VMToOSInterface::MemoryProtection::NoAccess:
            return PAGE_NOACCESS;
        case VMToOSInterface::MemoryProtection::ReadOnly:
            return PAGE_READONLY;
        case VMToOSInterface::MemoryProtection::ReadWrite:
            return PAGE_READWRITE;
        case VMToOSInterface::MemoryProtection::WriteCopy:
            return PAGE_WRITECOPY;
        case VMToOSInterface::MemoryProtection::Execute:
            return PAGE_EXECUTE;
        case VMToOSInterface::MemoryProtection::ExecuteRead:
            return PAGE_EXECUTE_READ;
        case VMToOSInterface::MemoryProtection::ExecuteReadWrite:
            return PAGE_EXECUTE_READWRITE;
        default:
            assert(!"Unknown memory protection");
            return PAGE_NOACCESS;
    }
}

#ifndef NDEBUG
static bool IsValidMemoryRange(void* address, size_t size)
{
    return size != 0 && size <= UINTPTR_MAX - reinterpret_cast<uintptr_t>(address);
}
#endif // !NDEBUG

VMToOSInterface::MemoryMapHandle VMToOSInterface::CreateMemoryMapping(MemoryMapHandle fileHandle, uint64_t size, MemoryProtection protection)
{
    DWORD nativeProtection = GetNativeProtection(protection);

    return CreateFileMappingW(fileHandle, nullptr, nativeProtection,
        static_cast<DWORD>(size >> 32), static_cast<DWORD>(size), nullptr);
}

bool VMToOSInterface::CloseMemoryMapping(MemoryMapHandle mapping)
{
    return CloseHandle(mapping) != FALSE;
}

void* VMToOSInterface::MapAnonymousMemory(size_t size, MemoryProtection protection, bool executableBacking)
{
    assert(size != 0);

    MemoryMapHandle mapping = CreateMemoryMapping(INVALID_HANDLE_VALUE, size,
        executableBacking ? MemoryProtection::ExecuteReadWrite : protection);
    if (mapping == InvalidMemoryMapHandle)
    {
        return nullptr;
    }

    void* view = MapMemory(mapping, 0, nullptr, size, protection, true);
    if (view == nullptr)
    {
        DWORD error = GetLastError();
        CloseMemoryMapping(mapping);
        SetLastError(error);
        return nullptr;
    }

    if (!CloseMemoryMapping(mapping))
    {
        DWORD error = GetLastError();
        UnmapMemory(view, size);
        SetLastError(error);
        return nullptr;
    }

    return view;
}

int32_t VMToOSInterface::GetLastMemoryMappingError()
{
    return HRESULT_FROM_WIN32(GetLastError());
}

void* VMToOSInterface::MapMemory(MemoryMapHandle mapping, uint64_t offset, void* address, size_t size, MemoryProtection protection, bool shared)
{
#if defined(_DEBUG) && defined(TARGET_X86)
    if (address == nullptr)
    {
        char* allocationProbe = new (std::nothrow) char;
        if (allocationProbe == nullptr)
        {
            SetLastError(ERROR_OUTOFMEMORY);
            return nullptr;
        }
        delete allocationProbe;
    }
#endif // _DEBUG && TARGET_X86

    if (address != nullptr)
    {
        assert(SupportsPlaceholderMapping());
    }

    DWORD nativeProtection = GetNativeProtection(protection);

    assert(size <= UINT64_MAX - offset);
    assert(address == nullptr || IsValidMemoryRange(address, size));

    if (address != nullptr)
    {
        if (!shared)
        {
            if (protection == MemoryProtection::ReadWrite)
            {
                nativeProtection = PAGE_WRITECOPY;
            }
            else if (protection == MemoryProtection::ExecuteReadWrite)
            {
                nativeProtection = PAGE_EXECUTE_WRITECOPY;
            }
        }

        return s_mapViewOfFile3(mapping, GetCurrentProcess(), address, offset, size,
            MEM_REPLACE_PLACEHOLDER, nativeProtection, nullptr, 0);
    }

    DWORD access;
    switch (protection)
    {
        case MemoryProtection::ReadOnly:
            access = FILE_MAP_READ;
            break;
        case MemoryProtection::ReadWrite:
            access = shared ? FILE_MAP_WRITE : FILE_MAP_COPY;
            break;
        case MemoryProtection::Execute:
            access = FILE_MAP_EXECUTE;
            break;
        case MemoryProtection::ExecuteRead:
            access = FILE_MAP_EXECUTE | FILE_MAP_READ;
            break;
        case MemoryProtection::ExecuteReadWrite:
            access = FILE_MAP_EXECUTE | (shared ? FILE_MAP_WRITE : FILE_MAP_COPY);
            break;
        default:
            SetLastError(ERROR_INVALID_PARAMETER);
            return nullptr;
    }

    void* view = MapViewOfFile(mapping, access, static_cast<DWORD>(offset >> 32), static_cast<DWORD>(offset), size);
    if (view == nullptr && GetLastError() == ERROR_SUCCESS)
        SetLastError(ERROR_OUTOFMEMORY);
    return view;
}

int VMToOSInterface::UnmapMemory(void* address, size_t)
{
    return UnmapViewOfFile(address) ? 0 : -1;
}

void* VMToOSInterface::ReserveMemoryPlaceholder(void* address, size_t size, bool)
{
    assert(SupportsPlaceholderMapping());

    assert(IsValidMemoryRange(address, size));

    return s_virtualAlloc2(GetCurrentProcess(), address, size,
        MEM_RESERVE | MEM_RESERVE_PLACEHOLDER, PAGE_NOACCESS, nullptr, 0);
}

bool VMToOSInterface::SplitMemoryPlaceholder(void* address, size_t size)
{
    assert(SupportsPlaceholderMapping());

    assert(address != nullptr && IsValidMemoryRange(address, size));

    return VirtualFree(address, size, MEM_RELEASE | MEM_PRESERVE_PLACEHOLDER) != FALSE;
}

void* VMToOSInterface::CommitMemoryPlaceholder(void* address, size_t size, MemoryProtection protection)
{
    assert(SupportsPlaceholderMapping());

    DWORD nativeProtection = GetNativeProtection(protection);

    assert(address != nullptr && IsValidMemoryRange(address, size));

    return s_virtualAlloc2(GetCurrentProcess(), address, size,
        MEM_COMMIT | MEM_RESERVE | MEM_REPLACE_PLACEHOLDER, nativeProtection, nullptr, 0);
}

bool VMToOSInterface::ReleaseMemoryReservation(void* address, size_t)
{
    assert(address != nullptr);

    return VirtualFree(address, 0, MEM_RELEASE) != FALSE;
}

void* VMToOSInterface::ReserveMemory(size_t size, bool)
{
    assert(size != 0);

    return VirtualAlloc(nullptr, size, MEM_RESERVE, PAGE_NOACCESS);
}

void* VMToOSInterface::ReserveMemoryWithinRange(const void* rangeStart, const void* rangeEnd, size_t size, bool executable)
{
    assert(size != 0);

    if (rangeStart == nullptr && rangeEnd == nullptr)
    {
        return ReserveMemory(size, executable);
    }

    constexpr uintptr_t granularity = 64 * 1024;
    uintptr_t start = reinterpret_cast<uintptr_t>(rangeStart);
    uintptr_t end = reinterpret_cast<uintptr_t>(rangeEnd);
    assert(end > start);
    if (start > UINTPTR_MAX - (granularity - 1))
    {
        SetLastError(ERROR_NOT_ENOUGH_MEMORY);
        return nullptr;
    }

    uintptr_t tryAddress = (start + granularity - 1) & ~(granularity - 1);

    while (tryAddress <= end && size <= end - tryAddress)
    {
        MEMORY_BASIC_INFORMATION mbInfo;
        if (VirtualQuery(reinterpret_cast<LPCVOID>(tryAddress), &mbInfo, sizeof(mbInfo)) == 0)
        {
            break;
        }

        if (mbInfo.State == MEM_FREE && mbInfo.RegionSize >= size)
        {
            void* result = VirtualAlloc(reinterpret_cast<void*>(tryAddress), size, MEM_RESERVE, PAGE_NOACCESS);
            if (result != nullptr)
            {
                return result;
            }

            // Another thread may have raced us for this region; keep scanning forward.
            if (tryAddress > UINTPTR_MAX - granularity)
            {
                break;
            }
            tryAddress += granularity;
        }
        else
        {
            uintptr_t regionBase = reinterpret_cast<uintptr_t>(mbInfo.BaseAddress);
            uintptr_t regionEnd = mbInfo.RegionSize > UINTPTR_MAX - regionBase
                ? UINTPTR_MAX
                : regionBase + mbInfo.RegionSize;
            if (tryAddress > UINTPTR_MAX - granularity)
            {
                break;
            }
            uintptr_t nextAddress = tryAddress + granularity;
            tryAddress = nextAddress > regionEnd ? nextAddress : regionEnd;
        }
    }

    SetLastError(ERROR_NOT_ENOUGH_MEMORY);
    return nullptr;
}

bool VMToOSInterface::DecommitMemory(void* address, size_t size)
{
    assert(address != nullptr && IsValidMemoryRange(address, size));

    return VirtualFree(address, size, MEM_DECOMMIT) != FALSE;
}

void* VMToOSInterface::CommitReservedMemory(void* address, size_t size, MemoryProtection protection)
{
    DWORD nativeProtection = GetNativeProtection(protection);

    assert(address != nullptr && IsValidMemoryRange(address, size));

    return VirtualAlloc(address, size, MEM_COMMIT, nativeProtection);
}

bool VMToOSInterface::ProtectMemory(void* address, size_t size, MemoryProtection protection)
{
    DWORD nativeProtection = GetNativeProtection(protection);

    assert(address != nullptr && IsValidMemoryRange(address, size));

    DWORD oldProtection;
    return VirtualProtect(address, size, nativeProtection, &oldProtection) != FALSE;
}

bool VMToOSInterface::PopulateMemoryPlaceholder(void* address, size_t allocationSize, const void* source, size_t copySize, MemoryProtection finalProtection)
{
    assert(address != nullptr && IsValidMemoryRange(address, allocationSize));
    assert(copySize <= allocationSize);
    assert(copySize == 0 || (source != nullptr && copySize <= UINTPTR_MAX - reinterpret_cast<uintptr_t>(source)));

    if (CommitMemoryPlaceholder(address, allocationSize, MemoryProtection::ReadWrite) == nullptr)
    {
        return false;
    }

    if (copySize != 0)
    {
        memcpy(address, source, copySize);
    }

    if (!ProtectMemory(address, allocationSize, finalProtection))
    {
        return false;
    }

    if (copySize != 0 && (finalProtection == MemoryProtection::Execute ||
        finalProtection == MemoryProtection::ExecuteRead || finalProtection == MemoryProtection::ExecuteReadWrite))
    {
        return FlushInstructionCache(GetCurrentProcess(), address, copySize) != FALSE;
    }

    return true;
}

bool VMToOSInterface::UseSharedReadOnlyMapping(size_t)
{
    return false;
}
