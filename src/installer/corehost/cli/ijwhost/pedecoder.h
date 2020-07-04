// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


#ifndef PEDECODER_H
#define PEDECODER_H

#include "pal.h"

struct IMAGE_COR_VTABLEFIXUP;

// A subsection of the PEDecoder from CoreCLR that has only the methods we need.
class PEDecoder
{
public:
    PEDecoder(void* mappedBase)
        :m_base((std::uintptr_t)mappedBase)
    {
    }

    bool HasCorHeader() const
    {
        return HasDirectoryEntry(IMAGE_DIRECTORY_ENTRY_COM_DESCRIPTOR);
    }

    bool HasManagedEntryPoint() const;
    bool HasNativeEntryPoint() const;
    void* GetNativeEntryPoint() const;
    IMAGE_COR_VTABLEFIXUP* GetVTableFixups(size_t* numFixupRecords) const;

    HINSTANCE GetBase() const
    {
        return (HINSTANCE)m_base;
    }
    
    std::uintptr_t GetRvaData(std::uint32_t rva) const
    {
        if (rva == 0)
        {
            return (std::uintptr_t)nullptr;
        }

        return m_base + rva;
    }

private:
    bool HasDirectoryEntry(int entry) const
    {
        return FindNTHeaders()->OptionalHeader.DataDirectory[entry].VirtualAddress != 0;
    }

    IMAGE_NT_HEADERS* FindNTHeaders() const
    {
        return reinterpret_cast<IMAGE_NT_HEADERS*>(m_base + (reinterpret_cast<IMAGE_DOS_HEADER*>(m_base)->e_lfanew));
    }

    IMAGE_COR20_HEADER *GetCorHeader() const
    {
        return FindCorHeader();
    }

    inline IMAGE_COR20_HEADER *FindCorHeader() const
    {
        return reinterpret_cast<IMAGE_COR20_HEADER*>(GetDirectoryEntryData(IMAGE_DIRECTORY_ENTRY_COM_DESCRIPTOR));
    }

    IMAGE_DATA_DIRECTORY *GetDirectoryEntry(int entry) const
    {
        return reinterpret_cast<IMAGE_DATA_DIRECTORY*>(
            reinterpret_cast<std::uintptr_t>(FindNTHeaders()) +
            offsetof(IMAGE_NT_HEADERS, OptionalHeader.DataDirectory) +
            entry * sizeof(IMAGE_DATA_DIRECTORY));
    }

    std::uintptr_t GetDirectoryEntryData(int entry, size_t* pSize = nullptr) const
    {
        IMAGE_DATA_DIRECTORY *pDir = GetDirectoryEntry(entry);

        if (pSize != nullptr)
            *pSize = (std::int32_t)(pDir->Size);

        return GetDirectoryData(pDir);
    }

    std::uintptr_t PEDecoder::GetDirectoryData(IMAGE_DATA_DIRECTORY *pDir) const
    {
        return GetRvaData(pDir->VirtualAddress);
    }

    std::uintptr_t m_base;
};

#endif
