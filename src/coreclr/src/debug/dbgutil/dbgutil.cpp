//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
//*****************************************************************************
// dbgutil.cpp
// 

//
//*****************************************************************************

//
// Various common helpers for PE resource reading used by multiple debug components.
//

#include <dbgutil.h>
#include "corerror.h"
#include <assert.h>
#include <stdio.h>

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
    // Fun code ahead... below is a hand written PE decoder with some of the file offsets hardcoded.
    // It supports no more than what we absolutely have to to get to the resources we need. Any of the
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
    hr = ReadFromDataTarget(pDataTarget, moduleBaseAddress, (BYTE*)&dosHeader, sizeof(dosHeader));

    // verify there is a 4 byte PE signature there
    DWORD peSigFilePointer = 0;
    if (SUCCEEDED(hr))
    {
        peSigFilePointer = dosHeader.e_lfanew;
        DWORD peSig = 0;
        hr = ReadFromDataTarget(pDataTarget, moduleBaseAddress + peSigFilePointer, (BYTE*)&peSig, 4);
        if (SUCCEEDED(hr) && peSig != IMAGE_NT_SIGNATURE)
        {
            hr = E_FAIL; // PE signature not present
        }
    }

    // after the signature is a 20 byte image file header
    // we need to parse this to figure out the target architecture
    IMAGE_FILE_HEADER imageFileHeader;
    if (SUCCEEDED(hr))
    {
        hr = ReadFromDataTarget(pDataTarget, moduleBaseAddress + peSigFilePointer + 4, (BYTE*)&imageFileHeader, IMAGE_SIZEOF_FILE_HEADER);
    }



    WORD optHeaderMagic = 0;
    DWORD peOptImageHeaderFilePointer = 0;
    if (SUCCEEDED(hr))
    {
        if(pwImageFileMachine != NULL)
        {
            *pwImageFileMachine = imageFileHeader.Machine;
        }

        // 4 bytes after the signature is the 20 byte image file header
        // 24 bytes after the signature is the image-only header
        // at the beginning of the image-only header is a 2 byte magic number indicating its format
        peOptImageHeaderFilePointer = peSigFilePointer + IMAGE_SIZEOF_FILE_HEADER + sizeof(DWORD);
        hr = ReadFromDataTarget(pDataTarget, moduleBaseAddress + peOptImageHeaderFilePointer, (BYTE*)&optHeaderMagic, 2);
    }

    // Either 112 or 128 bytes after the beginning of the image-only header is an 8 byte resource table
    // depending on whether the image is PE32 or PE32+
    DWORD resourceSectionRVA = 0;
    if (SUCCEEDED(hr))
    {
        if (optHeaderMagic == IMAGE_NT_OPTIONAL_HDR32_MAGIC) // PE32
        {
            IMAGE_OPTIONAL_HEADER32 header32;
            hr = ReadFromDataTarget(pDataTarget, moduleBaseAddress + peOptImageHeaderFilePointer,
                (BYTE*)&header32, sizeof(header32));
            if (SUCCEEDED(hr))
            {
                resourceSectionRVA = header32.DataDirectory[IMAGE_DIRECTORY_ENTRY_RESOURCE].VirtualAddress;
            }
        }
        else if (optHeaderMagic == IMAGE_NT_OPTIONAL_HDR64_MAGIC) //PE32+
        {
            IMAGE_OPTIONAL_HEADER64 header64;
            hr = ReadFromDataTarget(pDataTarget, moduleBaseAddress + peOptImageHeaderFilePointer,
                (BYTE*)&header64, sizeof(header64));
            if (SUCCEEDED(hr))
            {
                resourceSectionRVA = header64.DataDirectory[IMAGE_DIRECTORY_ENTRY_RESOURCE].VirtualAddress;
            }
        }
        else
        {
            hr = E_FAIL; // Invalid PE
        }
    }

    *pdwResourceSectionRVA = resourceSectionRVA;
    return S_OK;
}

HRESULT GetResourceRvaFromResourceSectionRva(ICorDebugDataTarget* pDataTarget,
    ULONG64 moduleBaseAddress,
    DWORD resourceSectionRva,
    DWORD type,
    DWORD name,
    DWORD language,
    DWORD* pResourceRva,
    DWORD* pResourceSize)
{
    HRESULT hr = S_OK;
    DWORD nameTableRva = 0;
    DWORD langTableRva = 0;
    DWORD resourceDataEntryRva = 0;
    *pResourceRva = 0;
    *pResourceSize = 0;

    // The resource section begins with a resource directory that indexes all the resources by type.
    // Each entry it points to is another resource directory that indexes all the same type
    // resources by name. And each entry in that table points to another resource directory that indexes
    // all the same type/name resources by language. Entries in the final table give the RVA of the actual
    // resource. 
    // Note all RVAs in this section are relative to the beginning of the resource section,
    // not the beginning of the image.

    hr = GetNextLevelResourceEntryRVA(pDataTarget, type, moduleBaseAddress, resourceSectionRva, &nameTableRva);


    if (SUCCEEDED(hr))
    {
        nameTableRva += resourceSectionRva;
        hr = GetNextLevelResourceEntryRVA(pDataTarget, name, moduleBaseAddress, nameTableRva, &langTableRva);

    }
    if (SUCCEEDED(hr))
    {
        langTableRva += resourceSectionRva;
        hr = GetNextLevelResourceEntryRVA(pDataTarget, language, moduleBaseAddress, langTableRva, &resourceDataEntryRva);
    }

    // The resource data entry has the first 4 bytes indicating the RVA of the resource
    // The next 4 bytes indicate the size of the resource
    if (SUCCEEDED(hr))
    {
        resourceDataEntryRva += resourceSectionRva;
        IMAGE_RESOURCE_DATA_ENTRY dataEntry;
        hr = ReadFromDataTarget(pDataTarget, moduleBaseAddress + resourceDataEntryRva,
            (BYTE*)&dataEntry, sizeof(dataEntry));
        *pResourceRva = dataEntry.OffsetToData;
        *pResourceSize = dataEntry.Size;
    }

    return hr;
}

HRESULT GetResourceRvaFromResourceSectionRvaByName(ICorDebugDataTarget* pDataTarget,
    ULONG64 moduleBaseAddress,
    DWORD resourceSectionRva,
    DWORD type,
    LPCWSTR pwszName,
    DWORD language,
    DWORD* pResourceRva,
    DWORD* pResourceSize)
{
    HRESULT hr = S_OK;
    DWORD nameTableRva = 0;
    DWORD langTableRva = 0;
    DWORD resourceDataEntryRva = 0;
    *pResourceRva = 0;
    *pResourceSize = 0;

    // The resource section begins with a resource directory that indexes all the resources by type.
    // Each entry it points to is another resource directory that indexes all the same type
    // resources by name. And each entry in that table points to another resource directory that indexes
    // all the same type/name resources by language. Entries in the final table give the RVA of the actual
    // resource. 
    // Note all RVAs in this section are relative to the beginning of the resource section,
    // not the beginning of the image.
    hr = GetNextLevelResourceEntryRVA(pDataTarget, type, moduleBaseAddress, resourceSectionRva, &nameTableRva);


    if (SUCCEEDED(hr))
    {
        nameTableRva += resourceSectionRva;
        hr = GetNextLevelResourceEntryRVAByName(pDataTarget, pwszName, moduleBaseAddress, nameTableRva, resourceSectionRva, &langTableRva);
    }
    if (SUCCEEDED(hr))
    {
        langTableRva += resourceSectionRva;
        hr = GetNextLevelResourceEntryRVA(pDataTarget, language, moduleBaseAddress, langTableRva, &resourceDataEntryRva);
    }

    // The resource data entry has the first 4 bytes indicating the RVA of the resource
    // The next 4 bytes indicate the size of the resource
    if (SUCCEEDED(hr))
    {
        resourceDataEntryRva += resourceSectionRva;
        IMAGE_RESOURCE_DATA_ENTRY dataEntry;
        hr = ReadFromDataTarget(pDataTarget, moduleBaseAddress + resourceDataEntryRva,
            (BYTE*)&dataEntry, sizeof(dataEntry));
        *pResourceRva = dataEntry.OffsetToData;
        *pResourceSize = dataEntry.Size;
    }

    return hr;
}

// Traverses down one level in the PE resource tree structure
// 
// Arguments:
//   pDataTarget - the data target for inspecting this process
//   id - the id of the next node in the resource tree you want
//   moduleBaseAddress - the base address of the module being inspected
//   resourceDirectoryRVA - the base address of the beginning of the resource directory for this
//                          level of the tree
//   pNextLevelRVA - out - The RVA for the next level tree directory or the RVA of the resource entry
//
// Returns:
//   S_OK if succesful or an appropriate failing HRESULT
HRESULT GetNextLevelResourceEntryRVA(ICorDebugDataTarget* pDataTarget,
    DWORD id,
    ULONG64 moduleBaseAddress,
    DWORD resourceDirectoryRVA,
    DWORD* pNextLevelRVA)
{
    *pNextLevelRVA = 0;
    HRESULT hr = S_OK;

    // A resource directory which consists of
    // a header followed by a number of entries. In the header at offset 12 is
    // the number entries identified by name, followed by the number of entries
    // identified by ID at offset 14. Both are 2 bytes.
    // This method only supports locating entries by ID, not by name
    IMAGE_RESOURCE_DIRECTORY resourceDirectory;
    hr = ReadFromDataTarget(pDataTarget, moduleBaseAddress + resourceDirectoryRVA, (BYTE*)&resourceDirectory, sizeof(resourceDirectory));



    // The ith resource directory entry is at offset 16 + 8i from the beginning of the resource
    // directory table
    WORD numNameEntries;
    WORD numIDEntries;
    if (SUCCEEDED(hr))
    {
        numNameEntries = resourceDirectory.NumberOfNamedEntries;
        numIDEntries = resourceDirectory.NumberOfIdEntries;

        for (WORD i = numNameEntries; i < numNameEntries + numIDEntries; i++)
        {
            IMAGE_RESOURCE_DIRECTORY_ENTRY entry;
            hr = ReadFromDataTarget(pDataTarget, moduleBaseAddress + resourceDirectoryRVA + sizeof(resourceDirectory) + sizeof(entry)*i,
                (BYTE*)&entry, sizeof(entry));
            if (FAILED(hr))
            {
                break;
            }
            if (entry.Id == id)
            {
                *pNextLevelRVA = entry.OffsetToDirectory;
                break;
            }
        }
    }

    // If we didn't find the entry
    if (SUCCEEDED(hr) && *pNextLevelRVA == 0)
    {
        hr = E_FAIL;
    }

    return hr; // resource not found
}

// Traverses down one level in the PE resource tree structure
// 
// Arguments:
//   pDataTarget - the data target for inspecting this process
//   name - the name of the next node in the resource tree you want
//   moduleBaseAddress - the base address of the module being inspected
//   resourceDirectoryRVA - the base address of the beginning of the resource directory for this
//                          level of the tree
//   resourceSectionRVA - the rva of the beginning of the resource section of the PE file
//   pNextLevelRVA - out - The RVA for the next level tree directory or the RVA of the resource entry
//
// Returns:
//   S_OK if succesful or an appropriate failing HRESULT
HRESULT GetNextLevelResourceEntryRVAByName(ICorDebugDataTarget* pDataTarget,
    LPCWSTR pwzName,
    ULONG64 moduleBaseAddress,
    DWORD resourceDirectoryRva,
    DWORD resourceSectionRva,
    DWORD* pNextLevelRva)
{
    HRESULT hr = S_OK;
    DWORD nameLength = (DWORD)wcslen(pwzName);
    WCHAR entryName[50];
    assert(nameLength < 50);     // this implementation won't support matching a name longer
    // than 50 characters. We only look up the hard coded name
    // of the debug resource in clr.dll though, so it shouldn't be
    // an issue. Increase this count if we ever want to look up
    // larger names
    if (nameLength >= 50)
    {
        hr = E_FAIL; // invalid name length
    }

    // A resource directory which consists of
    // a header followed by a number of entries. In the header at offset 12 is
    // the number entries identified by name, followed by the number of entries
    // identified by ID at offset 14. Both are 2 bytes.
    // This method only supports locating entries by ID, not by name
    IMAGE_RESOURCE_DIRECTORY resourceDirectory = { 0 };
    if (SUCCEEDED(hr))
    {
        hr = ReadFromDataTarget(pDataTarget, moduleBaseAddress + resourceDirectoryRva, (BYTE*)&resourceDirectory, sizeof(resourceDirectory));
    }

    // The ith resource directory entry is at offset 16 + 8i from the beginning of the resource
    // directory table
    if (SUCCEEDED(hr))
    {
        WORD numNameEntries = resourceDirectory.NumberOfNamedEntries;
        for (WORD i = 0; i < numNameEntries; i++)
        {
            IMAGE_RESOURCE_DIRECTORY_ENTRY entry;
            hr = ReadFromDataTarget(pDataTarget, moduleBaseAddress + resourceDirectoryRva + sizeof(resourceDirectory) + sizeof(entry)*i,
                (BYTE*)&entry, sizeof(entry));
            if (FAILED(hr))
            {
                break;
            }

            // the NameRVAOrID field points to a UTF16 string with a 2 byte length in front of it
            // read the 2 byte length first. The doc of course doesn't mention this, but the RVA is
            // relative to the base of the resource section and needs the leading bit stripped.
            WORD entryNameLength = 0;
            hr = ReadFromDataTarget(pDataTarget, moduleBaseAddress + resourceSectionRva +
                entry.NameOffset, (BYTE*)&entryNameLength, sizeof(entryNameLength));
            if (FAILED(hr))
            {
                break;
            }
            if (entryNameLength != nameLength)
            {
                continue; // names aren't the same length, not a match
            }

            // read the rest of the string data and check for a match
            hr = ReadFromDataTarget(pDataTarget, moduleBaseAddress + resourceSectionRva +
                entry.NameOffset + 2, (BYTE*)entryName, entryNameLength*sizeof(WCHAR));
            if (FAILED(hr))
            {
                break;
            }
            if (memcmp(entryName, pwzName, entryNameLength*sizeof(WCHAR)) == 0)
            {
                *pNextLevelRva = entry.OffsetToDirectory;
                break;
            }
        }
    }

    if (SUCCEEDED(hr) && *pNextLevelRva == 0)
    {
        hr = E_FAIL; // resource not found
    }

    return hr;
}

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
