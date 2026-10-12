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
    HMODULE module = LoadLibraryExW(L"kernelbase.dll", nullptr, LOAD_LIBRARY_SEARCH_SYSTEM32);
    if (module != nullptr)
    {
        s_virtualAlloc2 = reinterpret_cast<VirtualAlloc2Fn>(GetProcAddress(module, "VirtualAlloc2"));
        s_mapViewOfFile3 = reinterpret_cast<MapViewOfFile3Fn>(GetProcAddress(module, "MapViewOfFile3"));
        FreeLibrary(module);
    }

    // Cache unavailable APIs as well, rather than retrying initialization on each call.
    return TRUE;
}

bool VMToOSInterface::SupportsPlaceholderMapping()
{
    return InitOnceExecuteOnce(&s_placeholderInitialization, InitializePlaceholderMapping, nullptr, nullptr) &&
        s_virtualAlloc2 != nullptr && s_mapViewOfFile3 != nullptr;
}

#ifndef NDEBUG
static bool IsValidMemoryProtection(DWORD nativeProtection)
{
    constexpr DWORD allowedProtections = PAGE_NOACCESS | PAGE_READONLY | PAGE_READWRITE |
        PAGE_EXECUTE | PAGE_EXECUTE_READ | PAGE_EXECUTE_READWRITE;
    return nativeProtection != 0 && (nativeProtection & (nativeProtection - 1)) == 0 &&
        (nativeProtection & allowedProtections) != 0;
}
#endif // !NDEBUG

static bool IsValidMemoryRange(void* address, size_t size)
{
    return size != 0 && size <= UINTPTR_MAX - reinterpret_cast<uintptr_t>(address);
}

VMToOSInterface::MemoryMapHandle VMToOSInterface::CreateMemoryMapping(MemoryMapHandle fileHandle, uint64_t size, MemoryProtection protection)
{
    DWORD nativeProtection = static_cast<DWORD>(protection);
    assert(IsValidMemoryProtection(nativeProtection));

    return CreateFileMappingW(fileHandle, nullptr, nativeProtection,
        static_cast<DWORD>(size >> 32), static_cast<DWORD>(size), nullptr);
}

bool VMToOSInterface::CloseMemoryMapping(MemoryMapHandle mapping)
{
    return CloseHandle(mapping) != FALSE;
}

void* VMToOSInterface::MapAnonymousMemory(size_t size, MemoryProtection protection, bool executableBacking)
{
    if (size == 0)
    {
        SetLastError(ERROR_INVALID_PARAMETER);
        return nullptr;
    }

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

    DWORD nativeProtection = static_cast<DWORD>(protection);
    assert(IsValidMemoryProtection(nativeProtection));

    if (size > UINT64_MAX - offset || (address != nullptr && !IsValidMemoryRange(address, size)))
    {
        SetLastError(ERROR_INVALID_PARAMETER);
        return nullptr;
    }

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

void* VMToOSInterface::ReserveMemoryPlaceholder(void* address, size_t size)
{
    assert(SupportsPlaceholderMapping());

    if (!IsValidMemoryRange(address, size))
    {
        SetLastError(ERROR_INVALID_PARAMETER);
        return nullptr;
    }

    return s_virtualAlloc2(GetCurrentProcess(), address, size,
        MEM_RESERVE | MEM_RESERVE_PLACEHOLDER, PAGE_NOACCESS, nullptr, 0);
}

bool VMToOSInterface::SplitMemoryPlaceholder(void* address, size_t size)
{
    assert(SupportsPlaceholderMapping());

    if (address == nullptr || !IsValidMemoryRange(address, size))
    {
        SetLastError(ERROR_INVALID_PARAMETER);
        return false;
    }

    return VirtualFree(address, size, MEM_RELEASE | MEM_PRESERVE_PLACEHOLDER) != FALSE;
}

void* VMToOSInterface::CommitMemoryPlaceholder(void* address, size_t size, MemoryProtection protection)
{
    assert(SupportsPlaceholderMapping());

    DWORD nativeProtection = static_cast<DWORD>(protection);
    assert(IsValidMemoryProtection(nativeProtection));

    if (address == nullptr || !IsValidMemoryRange(address, size))
    {
        SetLastError(ERROR_INVALID_PARAMETER);
        return nullptr;
    }

    return s_virtualAlloc2(GetCurrentProcess(), address, size,
        MEM_COMMIT | MEM_RESERVE | MEM_REPLACE_PLACEHOLDER, nativeProtection, nullptr, 0);
}

bool VMToOSInterface::ReleaseMemoryReservation(void* address, size_t)
{
    if (address == nullptr)
    {
        SetLastError(ERROR_INVALID_PARAMETER);
        return false;
    }

    return VirtualFree(address, 0, MEM_RELEASE) != FALSE;
}

bool VMToOSInterface::ProtectMemory(void* address, size_t size, MemoryProtection protection)
{
    DWORD nativeProtection = static_cast<DWORD>(protection);
    assert(IsValidMemoryProtection(nativeProtection));

    if (address == nullptr || !IsValidMemoryRange(address, size))
    {
        SetLastError(ERROR_INVALID_PARAMETER);
        return false;
    }

    DWORD oldProtection;
    return VirtualProtect(address, size, nativeProtection, &oldProtection) != FALSE;
}

bool VMToOSInterface::PopulateMemoryPlaceholder(void* address, size_t allocationSize, const void* source, size_t copySize, MemoryProtection finalProtection)
{
    assert(IsValidMemoryProtection(static_cast<DWORD>(finalProtection)));

    if (address == nullptr || !IsValidMemoryRange(address, allocationSize) || copySize > allocationSize ||
        (copySize != 0 && (source == nullptr || copySize > UINTPTR_MAX - reinterpret_cast<uintptr_t>(source))))
    {
        SetLastError(ERROR_INVALID_PARAMETER);
        return false;
    }

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
