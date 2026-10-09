// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Various common helpers for PE resource reading used by multiple debug components.

#include <dbgutil.h>
#include "corerror.h"
#include <assert.h>
#include <stdio.h>
#include <memory>
#include <dn-u16.h>
#include "corhlpr.h"

#ifdef HOST_WINDOWS

namespace
{
    HRESULT GetMachineAndDirectoryAddress(ICorDebugDataTarget* dataTarget,
        ULONG64 moduleBaseAddress,
        uint8_t imageDirectory,
        WORD* imageFileMachine,
        DWORD* directoryRVA)
    {
        // Fun code ahead... below is a hand written PE decoder with some of the file offsets hardcoded.
        // It supports no more than what we absolutely have to get to the PE directory we need. Any of the
        // magic numbers used below can be determined by using the public documentation on the web.
        //
        // Yes utilcode has a PE decoder, no it does not support reading its data through a datatarget
        // It was easier to inspect the small portion that I needed than to shove an abstraction layer under
        // our utilcode and then make sure everything still worked.

        // SECURITY WARNING: all data provided by the data target should be considered untrusted.
        // Do not allow malicious data to cause large reads, memory allocations, buffer overflow,
        // or any other undesirable behavior.

        HRESULT hr = S_OK;

        // at offset 3c in the image is a 4 byte file pointer that indicates where the PE signature is
        IMAGE_DOS_HEADER dosHeader;
        hr = ReadFromDataTarget(dataTarget, moduleBaseAddress, (BYTE*)&dosHeader, sizeof(dosHeader));

        // verify there is a 4 byte PE signature there
        DWORD peSigFilePointer = 0;
        if (SUCCEEDED(hr))
        {
            peSigFilePointer = dosHeader.e_lfanew;
            DWORD peSig = 0;
            hr = ReadFromDataTarget(dataTarget, moduleBaseAddress + peSigFilePointer, (BYTE*)&peSig, 4);
            if (SUCCEEDED(hr) && peSig != IMAGE_NT_SIGNATURE)
            {
                hr = E_FAIL; // PE signature not present
            }
        }

        // after the signature is a 20 byte image file header
        // we need to parse this to figure out the target architecture
        IMAGE_FILE_HEADER imageFileHeader = {};
        if (SUCCEEDED(hr))
        {
            hr = ReadFromDataTarget(dataTarget, moduleBaseAddress + peSigFilePointer + 4, (BYTE*)&imageFileHeader, IMAGE_SIZEOF_FILE_HEADER);
        }

        WORD optHeaderMagic = 0;
        DWORD peOptImageHeaderFilePointer = 0;
        if (SUCCEEDED(hr))
        {
            if(imageFileMachine != NULL)
            {
                *imageFileMachine = imageFileHeader.Machine;
            }

            // 4 bytes after the signature is the 20 byte image file header
            // 24 bytes after the signature is the image-only header
            // at the beginning of the image-only header is a 2 byte magic number indicating its format
            peOptImageHeaderFilePointer = peSigFilePointer + IMAGE_SIZEOF_FILE_HEADER + sizeof(DWORD);
            hr = ReadFromDataTarget(dataTarget, moduleBaseAddress + peOptImageHeaderFilePointer, (BYTE*)&optHeaderMagic, 2);
        }

        // Either 112 or 128 bytes after the beginning of the image-only header is an 8 byte resource table
        // depending on whether the image is PE32 or PE32+
        DWORD sectionRVA = 0;
        if (SUCCEEDED(hr))
        {
            if (optHeaderMagic == IMAGE_NT_OPTIONAL_HDR32_MAGIC) // PE32
            {
                IMAGE_OPTIONAL_HEADER32 header32;
                hr = ReadFromDataTarget(dataTarget, moduleBaseAddress + peOptImageHeaderFilePointer,
                    (BYTE*)&header32, sizeof(header32));
                if (SUCCEEDED(hr))
                {
                    sectionRVA = header32.DataDirectory[imageDirectory].VirtualAddress;
                }
            }
            else if (optHeaderMagic == IMAGE_NT_OPTIONAL_HDR64_MAGIC) //PE32+
            {
                IMAGE_OPTIONAL_HEADER64 header64;
                hr = ReadFromDataTarget(dataTarget, moduleBaseAddress + peOptImageHeaderFilePointer,
                    (BYTE*)&header64, sizeof(header64));
                if (SUCCEEDED(hr))
                {
                    sectionRVA = header64.DataDirectory[imageDirectory].VirtualAddress;
                }
            }
            else
            {
                hr = E_FAIL; // Invalid PE
            }
        }

        *directoryRVA = sectionRVA;
        return hr;
    }
}

// Returns the RVA of the resource section for the module specified by the given data target and module base.
// Returns failure if the module doesn't have a resource section.
//
// Arguments
//   pDataTarget - dataTarget for the process we are inspecting
//   moduleBaseAddress - base address of a module we should inspect
//   pwImageFileMachine - updated with the Machine from the IMAGE_FILE_HEADER
//   pdwResourceSectionRVA - updated with the resultant RVA on success
HRESULT GetMachineAndResourceSectionRVA(ICorDebugDataTarget* pDataTarget,
    ULONG64 moduleBaseAddress,
    WORD* pwImageFileMachine,
    DWORD* pdwResourceSectionRVA)
{
    return GetMachineAndDirectoryAddress(pDataTarget, moduleBaseAddress, IMAGE_DIRECTORY_ENTRY_RESOURCE, pwImageFileMachine, pdwResourceSectionRVA);
}

#endif // HOST_WINDOWS

// A small wrapper that reads from the data target and throws on error
HRESULT ReadFromDataTarget(ICorDebugDataTarget* pDataTarget,
    ULONG64 addr,
    BYTE* pBuffer,
    ULONG32 bytesToRead)
{
    //PRECONDITION(CheckPointer(pDataTarget));
    //PRECONDITION(CheckPointer(pBuffer));

    HRESULT hr = S_OK;
    ULONG32 bytesReadTotal = 0;
    ULONG32 bytesRead = 0;
    do
    {
        if (FAILED(pDataTarget->ReadVirtual((CORDB_ADDRESS)(addr + bytesReadTotal),
            pBuffer,
            bytesToRead - bytesReadTotal,
            &bytesRead)))
        {
            hr = CORDBG_E_READVIRTUAL_FAILURE;
            break;
        }
        bytesReadTotal += bytesRead;
    } while (bytesRead != 0 && (bytesReadTotal < bytesToRead));

    // If we can't read all the expected memory, then fail
    if (SUCCEEDED(hr) && (bytesReadTotal != bytesToRead))
    {
        hr = HRESULT_FROM_WIN32(ERROR_PARTIAL_COPY);
    }

    return hr;
}

#if TARGET_WINDOWS

extern "C" bool
TryGetSymbol(ICorDebugDataTarget* dataTarget, uint64_t baseAddress, const char* symbolName, uint64_t* symbolAddress)
{
    if (symbolAddress == nullptr)
    {
        return false;
    }

    *symbolAddress = 0;

    DWORD exportTableRva;
    if (FAILED(GetMachineAndDirectoryAddress(dataTarget, baseAddress, IMAGE_DIRECTORY_ENTRY_EXPORT, nullptr, &exportTableRva)))
    {
        return false;
    }

    // A module with no export directory reports a zero RVA here; there is nothing to search and
    // reading at baseAddress would parse the PE headers as an IMAGE_EXPORT_DIRECTORY.
    if (exportTableRva == 0)
    {
        return false;
    }

    // Manually read the export directory from the target to find the requested symbol.
    IMAGE_EXPORT_DIRECTORY exportDir{};
    if (FAILED(ReadFromDataTarget(dataTarget, baseAddress + exportTableRva, (BYTE*)&exportDir, sizeof(exportDir))))
    {
        return false;
    }

    uint32_t namePointerCount = VAL32(exportDir.NumberOfNames);
    uint32_t exportAddressCount = VAL32(exportDir.NumberOfFunctions);
    uint32_t addressTableRVA = VAL32(exportDir.AddressOfFunctions);
    uint32_t ordinalTableRVA = VAL32(exportDir.AddressOfNameOrdinals);
    uint32_t nameTableRVA = VAL32(exportDir.AddressOfNames);

    for (uint32_t nameIndex = 0; nameIndex < namePointerCount; nameIndex++)
    {
        uint32_t namePointerRVA = 0;
        if (FAILED(ReadFromDataTarget(dataTarget, baseAddress + nameTableRVA + sizeof(uint32_t) * nameIndex, (BYTE*)&namePointerRVA, sizeof(namePointerRVA))))
        {
            return false;
        }
        if (namePointerRVA != 0)
        {
            size_t symbolNameLength = strlen(symbolName);
            if (symbolNameLength > UINT32_MAX)
            {
                // Symbol name is too large, we won't discover it.
                return false;
            }
            // Allocate a buffer for the memory that we'll read out of the target image.
            // We can allocate as much space as the target symbol name as we gracefully handle reading
            // inaccessible memory.
            std::unique_ptr<char[]> namePointer(new char[symbolNameLength + 1]);
            // If we fail to read the memory or the name doesn't match, then we don't have the right export.
            if (SUCCEEDED(ReadFromDataTarget(dataTarget, baseAddress + namePointerRVA, (BYTE*)namePointer.get(), (UINT32)symbolNameLength + 1))
                && strncmp(namePointer.get(), symbolName, symbolNameLength + 1) == 0)
            {
                // If the name matches, we should be able to get the ordinal
                uint16_t ordinalForNamedExport = 0;
                if (FAILED(ReadFromDataTarget(dataTarget, baseAddress + ordinalTableRVA + sizeof(uint16_t) * nameIndex, (BYTE*)&ordinalForNamedExport, sizeof(ordinalForNamedExport))))
                {
                    return false;
                }
                // The ordinal indexes the export address table; reject an out-of-range value from
                // untrusted image data before using it to compute a read offset.
                if (ordinalForNamedExport >= exportAddressCount)
                {
                    return false;
                }
                // If the name matches, we should be able to get the export
                uint32_t exportRVA = 0;
                if (FAILED(ReadFromDataTarget(dataTarget, baseAddress + addressTableRVA + sizeof(uint32_t) * ordinalForNamedExport, (BYTE*)&exportRVA, sizeof(exportRVA))))
                {
                    return false;
                }
                *symbolAddress = baseAddress + exportRVA;
                return true;
            }
        }
    }

    return false;
}
#endif
