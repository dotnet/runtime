// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//
// Implementation of the portions of the Platform Abstraction Layer (PAL) library that are common among
// multiple PAL variants.
//

#include <windows.h>
#include <stdio.h>
#include <errno.h>
#include <evntprov.h>
#include "CommonTypes.h"
#include "daccess.h"
#include "PalLimitedContext.h"
#include "Pal.h"
#include <winternl.h>
#include "CommonMacros.h"
#include "rhassert.h"

// Given the OS handle of a loaded module, compute the upper and lower virtual address bounds (inclusive).
void PalGetModuleBounds(HANDLE hOsHandle, _Out_ uint8_t ** ppLowerBound, _Out_ uint8_t ** ppUpperBound)
{
    BYTE *pbModule = (BYTE*)hOsHandle;
    DWORD cbModule;

    IMAGE_NT_HEADERS *pNtHeaders = (IMAGE_NT_HEADERS*)(pbModule + ((IMAGE_DOS_HEADER*)hOsHandle)->e_lfanew);
    if (pNtHeaders->OptionalHeader.Magic == IMAGE_NT_OPTIONAL_HDR32_MAGIC)
        cbModule = ((IMAGE_OPTIONAL_HEADER32*)&pNtHeaders->OptionalHeader)->SizeOfImage;
    else
        cbModule = ((IMAGE_OPTIONAL_HEADER64*)&pNtHeaders->OptionalHeader)->SizeOfImage;

    *ppLowerBound = pbModule;
    *ppUpperBound = pbModule + cbModule - 1;
}

// Reads through the PE header of the specified module, and returns
// the module's matching PDB's signature GUID, age, and build path by
// fishing them out of the last IMAGE_DEBUG_DIRECTORY of type
// IMAGE_DEBUG_TYPE_CODEVIEW.  Used when sending the ModuleLoad event
// to help profilers find matching PDBs for loaded modules.
//
// Arguments:
//
// [in] hOsHandle - OS Handle for module from which to get PDB info
// [out] pGuidSignature - PDB's signature GUID to be placed here
// [out] pdwAge - PDB's age to be placed here
// [out] wszPath - PDB's build path to be placed here
// [in] cchPath - Number of wide characters allocated in wszPath, including NULL terminator
//
// This is a simplification of similar code in CLR's GetCodeViewInfo
// in eventtrace.cpp.
void PalGetPDBInfo(HANDLE hOsHandle, GUID * pGuidSignature, _Out_ uint32_t * pdwAge, _Out_writes_z_(cchPath) WCHAR * wszPath, int32_t cchPath)
{
    // Zero-init [out]-params
    ZeroMemory(pGuidSignature, sizeof(*pGuidSignature));
    *pdwAge = 0;
    if (cchPath <= 0)
        return;
    wszPath[0] = L'\0';

    BYTE *pbModule = (BYTE*)hOsHandle;

    IMAGE_NT_HEADERS const * pNtHeaders = (IMAGE_NT_HEADERS*)(pbModule + ((IMAGE_DOS_HEADER*)hOsHandle)->e_lfanew);
    IMAGE_DATA_DIRECTORY const * rgDataDirectory = NULL;
    if (pNtHeaders->OptionalHeader.Magic == IMAGE_NT_OPTIONAL_HDR32_MAGIC)
        rgDataDirectory = ((IMAGE_OPTIONAL_HEADER32 const *)&pNtHeaders->OptionalHeader)->DataDirectory;
    else
        rgDataDirectory = ((IMAGE_OPTIONAL_HEADER64 const *)&pNtHeaders->OptionalHeader)->DataDirectory;

    IMAGE_DATA_DIRECTORY const * pDebugDataDirectory = &rgDataDirectory[IMAGE_DIRECTORY_ENTRY_DEBUG];

    // We don't have to worry about dealing with FLAT files (with padding missing), so header addresses
    // can be used as is
    IMAGE_DEBUG_DIRECTORY const *rgDebugEntries = (IMAGE_DEBUG_DIRECTORY const *) (pbModule + pDebugDataDirectory->VirtualAddress);
    DWORD cbDebugEntries = pDebugDataDirectory->Size;
    if (cbDebugEntries < sizeof(IMAGE_DEBUG_DIRECTORY))
        return;

    // Since rgDebugEntries is an array of IMAGE_DEBUG_DIRECTORYs, cbDebugEntries
    // should be a multiple of sizeof(IMAGE_DEBUG_DIRECTORY).
    if (cbDebugEntries % sizeof(IMAGE_DEBUG_DIRECTORY) != 0)
        return;

    // CodeView RSDS debug information -> PDB 7.00
    struct CV_INFO_PDB70 
    {
        DWORD          magic; 
        GUID           signature;       // unique identifier 
        DWORD          age;             // an always-incrementing value 
        char           path[1];         // zero terminated string with the name of the PDB file 
    };

    // Temporary storage for a CV_INFO_PDB70 and its size (which could be less than
    // sizeof(CV_INFO_PDB70); see below).
    struct PdbInfo
    {
        CV_INFO_PDB70 *     m_pPdb70;
        ULONG               m_cbPdb70;
    };

    // Iterate through all debug directory entries. The convention is that debuggers &
    // profilers typically just use the very last IMAGE_DEBUG_TYPE_CODEVIEW entry.
    PdbInfo pdbInfoLast = {0};
    int cEntries = cbDebugEntries / sizeof(IMAGE_DEBUG_DIRECTORY);
    for (int i = 0; i < cEntries; i++)
    {
        if (rgDebugEntries[i].Type != IMAGE_DEBUG_TYPE_CODEVIEW)
            continue;

        // Get raw data pointed to by this IMAGE_DEBUG_DIRECTORY

        if (rgDebugEntries[i].AddressOfRawData == NULL)
        {
            // No data. Skip.
            continue;
        }

        DWORD rvaOfRawData = rgDebugEntries[i].AddressOfRawData;
        ULONG cbDebugData = rgDebugEntries[i].SizeOfData;
        if (cbDebugData < offsetof(CV_INFO_PDB70, magic) + sizeof(((CV_INFO_PDB70*)0)->magic))
        {
            // raw data too small to contain magic number at expected spot, so its format
            // is not recognizable. Skip
            continue;
        }

        // Verify the magic number is as expected
        const DWORD CV_SIGNATURE_RSDS = 0x53445352;
        CV_INFO_PDB70 * pPdb70 = (CV_INFO_PDB70 *) (pbModule + rvaOfRawData);
        if (pPdb70->magic != CV_SIGNATURE_RSDS)
        {
            // Unrecognized magic number.  Skip
            continue;
        }

        // How much space is available for the path?
        size_t cchPathMaxIncludingNullTerminator = (cbDebugData - offsetof(CV_INFO_PDB70, path)) / sizeof(char);
        ASSERT(cchPathMaxIncludingNullTerminator >= 1);   // Guaranteed above

        // Verify path string fits inside the declared size
        size_t cchPathActualExcludingNullTerminator = strnlen_s(pPdb70->path, cchPathMaxIncludingNullTerminator);
        if (cchPathActualExcludingNullTerminator == cchPathMaxIncludingNullTerminator)
        {
            // This is how strnlen indicates failure--it couldn't find the null
            // terminator within the buffer size specified
            return;
        }

        // Looks valid.  Remember it.
        pdbInfoLast.m_pPdb70 = pPdb70;
        pdbInfoLast.m_cbPdb70 = cbDebugData;
    }

    // Take the last IMAGE_DEBUG_TYPE_CODEVIEW entry we saw, and return it to the caller
    if (pdbInfoLast.m_pPdb70 != NULL)
    {
        memcpy(pGuidSignature, &pdbInfoLast.m_pPdb70->signature, sizeof(GUID));
        *pdwAge = pdbInfoLast.m_pPdb70->age;

        // Convert build path from ANSI (UTF-8) to UNICODE (UTF-16)
        int result = MultiByteToWideChar(
            CP_UTF8,                // Code page for UTF-8
            0,                      // No special flags
            pdbInfoLast.m_pPdb70->path, // Source UTF-8 string
            -1,                     // Source string length (-1 for null-terminated)
            wszPath,                // Destination buffer for UTF-16 string
            cchPath                 // Size of destination buffer
        );

        if (result == 0)
        {
            // Conversion failed. PDB path isn't essential. An empty string will do.
            ASSERT(cchPath > 0);        // Guaranteed at top of function
            wszPath[0] = L'\0';
        }
    }
}

uint32_t g_RhNumberOfProcessors;

int32_t PalGetProcessCpuCount()
{
    ASSERT(g_RhNumberOfProcessors > 0);
    return g_RhNumberOfProcessors;
}

// Retrieves the entire range of memory dedicated to the calling thread's stack.  This does
// not get the current dynamic bounds of the stack, which can be significantly smaller than
// the maximum bounds.
bool PalGetMaximumStackBounds(_Out_ void** ppStackLowOut, _Out_ void** ppStackHighOut)
{
    // VirtualQuery on the address of a local variable to get the allocation
    // base of the stack.  Then use the StackBase field in the TEB to give
    // the highest address of the stack region.
    MEMORY_BASIC_INFORMATION mbi = { 0 };
    SIZE_T cb = VirtualQuery(&mbi, &mbi, sizeof(mbi));
    if (cb != sizeof(mbi))
        return false;

    NT_TIB* pTib = (NT_TIB*)NtCurrentTeb();
    *ppStackHighOut = pTib->StackBase;      // stack base is the highest address
    *ppStackLowOut = mbi.AllocationBase;    // allocation base is the lowest address
    return true;
}

// retrieves the full path to the specified module, if moduleBase is NULL retrieves the full path to the
// executable module of the current process.
//
// Return value:  number of characters in name string
//
//NOTE:  This implementation exists because calling GetModuleFileName is not wack compliant.  if we later decide
//       that the framework package containing mrt100_app no longer needs to be wack compliant, this should be
//       removed and the windows implementation of GetModuleFileName should be substitued on windows.
int32_t PalGetModuleFileName(_Out_ const TCHAR** pModuleNameOut, HANDLE moduleBase)
{
    TEB* pTEB = NtCurrentTeb();
    LIST_ENTRY* pStartLink = &(pTEB->ProcessEnvironmentBlock->Ldr->InMemoryOrderModuleList);
    LIST_ENTRY* pCurLink = pStartLink->Flink;

    do
    {
        LDR_DATA_TABLE_ENTRY* pEntry = CONTAINING_RECORD(pCurLink, LDR_DATA_TABLE_ENTRY, InMemoryOrderLinks);

        //null moduleBase will result in the first module being returned
        //since the module list is ordered this is the executable module of the current process
        if ((pEntry->DllBase == moduleBase) || (moduleBase == NULL))
        {
            *pModuleNameOut = pEntry->FullDllName.Buffer;
            return pEntry->FullDllName.Length / 2;
        }
        pCurLink = pCurLink->Flink;
    }
    while (pCurLink != pStartLink);

    *pModuleNameOut = NULL;
    return 0;
}
