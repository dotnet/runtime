// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef _SRC_INC_INTERNAL_DNMD_PEIMAGE_HPP_
#define _SRC_INC_INTERNAL_DNMD_PEIMAGE_HPP_

#include "dnmd_platform.hpp"

#include <cstddef>
#include <cstdint>
#include <cstring>

namespace dnmd
{
    struct PEMetadataInfo
    {
        size_t metadataOffset;
        uint32_t metadataSize;
        DWORD peKind;
        DWORD machine;
    };

    namespace detail
    {
        constexpr uint16_t PE32Magic = 0x10b;
        constexpr uint16_t PE32PlusMagic = 0x20b;
        constexpr uint32_t PESignature = 0x00004550;
        constexpr uint32_t ReadyToRunSignature = 0x00525452;

        struct ReadyToRunHeaderPrefix
        {
            uint32_t signature;
            uint16_t majorVersion;
            uint16_t minorVersion;
            uint32_t flags;
            uint32_t sectionCount;
        };
        static_assert(sizeof(ReadyToRunHeaderPrefix) == 16, "ReadyToRun header prefix must be 16 bytes");

        template<typename T>
        inline bool ReadValue(uint8_t const* image, size_t size, size_t offset, T& value)
        {
            if (offset > size || sizeof(value) > size - offset)
                return false;
            std::memcpy(&value, image + offset, sizeof(value));
            return true;
        }

        inline bool MapRva(uint8_t const* image, size_t size, size_t sectionsOffset, uint16_t sectionCount,
                           uint32_t sizeOfHeaders, uint32_t rva, size_t length, size_t& offset)
        {
            if (rva < sizeOfHeaders && length <= sizeOfHeaders - rva &&
                rva <= size && length <= size - rva)
            {
                offset = rva;
                return true;
            }

            for (uint16_t i = 0; i < sectionCount; ++i)
            {
                IMAGE_SECTION_HEADER section;
                if (!ReadValue(image, size, sectionsOffset + size_t(i) * sizeof(section), section))
                    return false;
                if (rva < section.VirtualAddress)
                    continue;
                uint32_t sectionOffset = rva - section.VirtualAddress;
                if (sectionOffset > section.SizeOfRawData || length > section.SizeOfRawData - sectionOffset ||
                    section.PointerToRawData > size || sectionOffset > size - section.PointerToRawData ||
                    length > size - section.PointerToRawData - sectionOffset)
                    continue;
                offset = size_t(section.PointerToRawData) + sectionOffset;
                return true;
            }
            return false;
        }
    }

    inline bool TryGetPEMetadata(uint8_t const* image, size_t size, PEMetadataInfo& result)
    {
        if (image == nullptr)
            return false;

        IMAGE_DOS_HEADER dos;
        if (!detail::ReadValue(image, size, 0, dos) || dos.e_magic != IMAGE_DOS_SIGNATURE || dos.e_lfanew < 0)
            return false;

        size_t ntOffset = static_cast<size_t>(dos.e_lfanew);
        uint32_t signature;
        IMAGE_FILE_HEADER fileHeader;
        if (!detail::ReadValue(image, size, ntOffset, signature) || signature != detail::PESignature ||
            !detail::ReadValue(image, size, ntOffset + sizeof(signature), fileHeader))
            return false;

        size_t optionalOffset = ntOffset + sizeof(signature) + sizeof(fileHeader);
        if (optionalOffset > size || fileHeader.SizeOfOptionalHeader > size - optionalOffset)
            return false;
        uint16_t magic;
        if (!detail::ReadValue(image, size, optionalOffset, magic))
            return false;

        IMAGE_DATA_DIRECTORY comDirectory;
        uint32_t sizeOfHeaders, directoryCount;
        size_t directoryOffset;
        if (magic == detail::PE32Magic)
        {
            directoryOffset = offsetof(IMAGE_OPTIONAL_HEADER32, DataDirectory);
            if (!detail::ReadValue(image, size, optionalOffset + offsetof(IMAGE_OPTIONAL_HEADER32, SizeOfHeaders), sizeOfHeaders) ||
                !detail::ReadValue(image, size, optionalOffset + offsetof(IMAGE_OPTIONAL_HEADER32, NumberOfRvaAndSizes), directoryCount))
                return false;
        }
        else if (magic == detail::PE32PlusMagic)
        {
            directoryOffset = offsetof(IMAGE_OPTIONAL_HEADER64, DataDirectory);
            if (!detail::ReadValue(image, size, optionalOffset + offsetof(IMAGE_OPTIONAL_HEADER64, SizeOfHeaders), sizeOfHeaders) ||
                !detail::ReadValue(image, size, optionalOffset + offsetof(IMAGE_OPTIONAL_HEADER64, NumberOfRvaAndSizes), directoryCount))
                return false;
        }
        else
        {
            return false;
        }

        size_t requiredOptionalSize = directoryOffset +
            (IMAGE_DIRECTORY_ENTRY_COM_DESCRIPTOR + 1) * sizeof(IMAGE_DATA_DIRECTORY);
        if (directoryCount <= IMAGE_DIRECTORY_ENTRY_COM_DESCRIPTOR ||
            fileHeader.SizeOfOptionalHeader < requiredOptionalSize ||
            !detail::ReadValue(image, size, optionalOffset + directoryOffset +
                IMAGE_DIRECTORY_ENTRY_COM_DESCRIPTOR * sizeof(IMAGE_DATA_DIRECTORY), comDirectory) ||
            comDirectory.VirtualAddress == 0 || comDirectory.Size < sizeof(IMAGE_COR20_HEADER))
            return false;

        size_t sectionsOffset = optionalOffset + fileHeader.SizeOfOptionalHeader;
        if (sectionsOffset > size || fileHeader.NumberOfSections > (size - sectionsOffset) / sizeof(IMAGE_SECTION_HEADER))
            return false;
        size_t corOffset;
        if (!detail::MapRva(image, size, sectionsOffset, fileHeader.NumberOfSections, sizeOfHeaders,
            comDirectory.VirtualAddress, sizeof(IMAGE_COR20_HEADER), corOffset))
            return false;
        IMAGE_COR20_HEADER corHeader;
        if (!detail::ReadValue(image, size, corOffset, corHeader) ||
            corHeader.cb < sizeof(corHeader) || corHeader.MetaData.Size == 0)
            return false;

        DWORD machine = fileHeader.Machine;
        DWORD peKind = magic == detail::PE32PlusMagic ? pe32Plus : peNot;
        if ((corHeader.Flags & COMIMAGE_FLAGS_ILONLY) != 0)
        {
            peKind |= peILonly;
            if (magic == detail::PE32PlusMagic && machine == IMAGE_FILE_MACHINE_I386)
                peKind &= ~static_cast<DWORD>(pe32Plus);
        }
        if (COR_IS_32BIT_REQUIRED(corHeader.Flags))
            peKind |= pe32BitRequired;
        else if (COR_IS_32BIT_PREFERRED(corHeader.Flags))
            peKind |= pe32BitPreferred;
        if (peKind == peNot)
            peKind = pe32BitRequired;

        if (corHeader.ManagedNativeHeader.Size >= sizeof(detail::ReadyToRunHeaderPrefix))
        {
            size_t nativeOffset;
            detail::ReadyToRunHeaderPrefix nativeHeader;
            if (detail::MapRva(image, size, sectionsOffset, fileHeader.NumberOfSections, sizeOfHeaders,
                    corHeader.ManagedNativeHeader.VirtualAddress,
                    corHeader.ManagedNativeHeader.Size, nativeOffset) &&
                detail::ReadValue(image, size, nativeOffset, nativeHeader) &&
                nativeHeader.signature == detail::ReadyToRunSignature)
            {
                DWORD nativeMachine = 0;
#if defined(TARGET_X86)
                nativeMachine = IMAGE_FILE_MACHINE_I386;
#elif defined(TARGET_AMD64)
                nativeMachine = IMAGE_FILE_MACHINE_AMD64;
#elif defined(TARGET_ARM64)
                nativeMachine = IMAGE_FILE_MACHINE_ARM64;
#endif
                DWORD osMask = 0;
#if defined(TARGET_LINUX)
                osMask = IMAGE_FILE_MACHINE_OS_MASK_LINUX;
#elif defined(TARGET_OSX) || defined(TARGET_IOS) || defined(TARGET_TVOS) || defined(TARGET_MACCATALYST)
                osMask = IMAGE_FILE_MACHINE_OS_MASK_APPLE;
#elif defined(TARGET_FREEBSD)
                osMask = IMAGE_FILE_MACHINE_OS_MASK_FREEBSD;
#elif defined(TARGET_NETBSD)
                osMask = IMAGE_FILE_MACHINE_OS_MASK_NETBSD;
#elif defined(TARGET_SUNOS)
                osMask = IMAGE_FILE_MACHINE_OS_MASK_SUN;
#endif
                if (nativeMachine != 0 && machine == (nativeMachine ^ osMask))
                    machine = nativeMachine;
                if ((nativeHeader.flags & 1) != 0)
                {
                    peKind = peILonly;
                    machine = IMAGE_FILE_MACHINE_I386;
                }
            }
        }

        size_t metadataOffset;
        if (!detail::MapRva(image, size, sectionsOffset, fileHeader.NumberOfSections, sizeOfHeaders,
                corHeader.MetaData.VirtualAddress, corHeader.MetaData.Size, metadataOffset))
            return false;

        result.metadataOffset = metadataOffset;
        result.metadataSize = corHeader.MetaData.Size;
        result.peKind = peKind;
        result.machine = machine;
        return true;
    }
}

#endif // _SRC_INC_INTERNAL_DNMD_PEIMAGE_HPP_
