// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//
// CEELOAD reads in the PE file format using LoadLibrary
// ===========================================================================
#include "ildasmpch.h"

#include "ceeload.h"
#include <corhdr.h>
#include <corimage.h>
#include "util.hpp"
#include "pedecoder.h"

/*************************************************************************************/
// Constructor and destructor!
/*************************************************************************************/
PELoader::PELoader()
{
    m_File = nullptr;
    m_hMod = NULL;
    m_pNT64 = NULL;
    m_bIsPE32 = FALSE;
    m_FileSize = m_FileSizeAligned = 0;
}

PELoader::~PELoader()
{

    m_hMod = NULL;
    m_pNT64 = NULL;
}

/*************************************************************************************/
/*************************************************************************************/
void PELoader::close()
{
    delete m_File;
    m_File = nullptr;
}

BOOL PELoader::open(const WCHAR* moduleName)
{
    HMODULE newhMod = NULL;

    _ASSERTE(moduleName);
    if (!moduleName)
        return FALSE;

    m_File = CreateMappedFile(moduleName);
    if (m_File == nullptr)
        return FALSE;

    m_FileSize = m_File->Size();

    newhMod = (HMODULE)m_File->Address();
   return open(newhMod);
}


/*************************************************************************************/
BOOL PELoader::open(HMODULE hMod)
{
    IMAGE_DOS_HEADER * pdosHeader;

    // get the dos header...
    m_hMod = hMod;
    pdosHeader = (IMAGE_DOS_HEADER*) hMod;
    // If this is not a PE32+ image
    if (pdosHeader->e_magic == VAL16(IMAGE_DOS_SIGNATURE) &&
        0 < VAL32(pdosHeader->e_lfanew) && VAL32(pdosHeader->e_lfanew) < 0xFF0)   // has to start on first page
    {
        size_t fileAlignment;

        m_pNT32 = (IMAGE_NT_HEADERS32*) ((BYTE *)m_hMod + VAL32(pdosHeader->e_lfanew));

        m_bIsPE32 = (m_pNT32->OptionalHeader.Magic == VAL16(IMAGE_NT_OPTIONAL_HDR32_MAGIC));

        if (m_bIsPE32)
        {
            if ((m_pNT32->Signature != VAL32(IMAGE_NT_SIGNATURE)) ||
                (m_pNT32->FileHeader.SizeOfOptionalHeader != VAL16(sizeof(IMAGE_OPTIONAL_HEADER32))))
            {
                // Make this appear uninitialized because for some reason this file is toasted.
                m_pNT32 = NULL;
                m_hMod = NULL;
                return FALSE;
            }
            fileAlignment = VAL32(m_pNT32->OptionalHeader.FileAlignment)-1;
        }
        else
        {
            if ((m_pNT64->Signature != VAL32(IMAGE_NT_SIGNATURE)) ||
                (m_pNT64->FileHeader.SizeOfOptionalHeader != VAL16(sizeof(IMAGE_OPTIONAL_HEADER64))))
            {
                // Make this appear uninitialized because for some reason this file is toasted.
                m_pNT64 = NULL;
                m_hMod = NULL;
                return FALSE;
            }
            fileAlignment = VAL32(m_pNT64->OptionalHeader.FileAlignment)-1;
        }
        m_FileSizeAligned = (m_FileSize + fileAlignment)&(~fileAlignment);
    }
    else
    {
        // Make this appear uninitialized because for some reason this file is toasted.
        m_hMod = NULL;
        return FALSE;
    }
    return TRUE;
}

/*************************************************************************************/
void PELoader::dump()
{
}

/*************************************************************************************/
BOOL PELoader::getCOMHeader(IMAGE_COR20_HEADER **ppCorHeader)
{
    PIMAGE_SECTION_HEADER   pSectionHeader;

    if (m_bIsPE32)
    {
        PIMAGE_NT_HEADERS32     pImageHeader;
        // Get the image header from the image, then get the directory location
        // of the CLR header which may or may not be filled out.
        pImageHeader = (PIMAGE_NT_HEADERS32)Cor_RtlImageNtHeader(m_hMod, (ULONG) m_FileSize);
        _ASSERTE(pImageHeader != NULL);
        pSectionHeader = (PIMAGE_SECTION_HEADER) Cor_RtlImageRvaToVa32(pImageHeader, (PBYTE)m_hMod,
            VAL32(pImageHeader->OptionalHeader.DataDirectory[IMAGE_DIRECTORY_ENTRY_COMHEADER].VirtualAddress),
            (DWORD)m_FileSizeAligned /* FileLength */);
    }
    else
    {
        PIMAGE_NT_HEADERS64     pImageHeader;

        // Get the image header from the image, then get the directory location
        // of the CLR header which may or may not be filled out.
        pImageHeader = (PIMAGE_NT_HEADERS64)Cor_RtlImageNtHeader(m_hMod, (ULONG) m_FileSize);
        _ASSERTE(pImageHeader != NULL);
        pSectionHeader = (PIMAGE_SECTION_HEADER) Cor_RtlImageRvaToVa64(pImageHeader, (PBYTE)m_hMod,
            VAL32(pImageHeader->OptionalHeader.DataDirectory[IMAGE_DIRECTORY_ENTRY_COMHEADER].VirtualAddress),
            (DWORD)m_FileSizeAligned /* FileLength */);
    }

    // If the section header exists, then return ok and the address.
    if (pSectionHeader)
    {
        *ppCorHeader = (IMAGE_COR20_HEADER *) pSectionHeader;
        return TRUE;
    }
    // If there is no CLR Data in this image, return false.
    else
        return FALSE;
}

/*************************************************************************************/
BOOL PELoader::getVAforRVA(DWORD rva,void **ppva)
{
    PIMAGE_SECTION_HEADER   pSectionHeader;

    if (m_bIsPE32)
    {
        // Get the image header from the image, then get the directory location
        // of the CLR header which may or may not be filled out.
        PIMAGE_NT_HEADERS32     pImageHeader;
        pImageHeader = (PIMAGE_NT_HEADERS32) Cor_RtlImageNtHeader(m_hMod, (ULONG) m_FileSize);
        _ASSERTE(pImageHeader != NULL);
        pSectionHeader = (PIMAGE_SECTION_HEADER) Cor_RtlImageRvaToVa32(pImageHeader, (PBYTE)m_hMod,
            rva, (DWORD)m_FileSizeAligned /* FileLength */);
    }
    else
    {
        PIMAGE_NT_HEADERS64     pImageHeader;
        pImageHeader = (PIMAGE_NT_HEADERS64) Cor_RtlImageNtHeader(m_hMod, (ULONG) m_FileSize);
        _ASSERTE(pImageHeader != NULL);
        pSectionHeader = (PIMAGE_SECTION_HEADER) Cor_RtlImageRvaToVa64(pImageHeader, (PBYTE)m_hMod,
            rva, (DWORD)m_FileSizeAligned /* FileLength */);
    }

    // If the section header exists, then return ok and the address.
    if (pSectionHeader)
    {
        *ppva = pSectionHeader;
        return TRUE;
    }
    // If there is no CLR Data in this image, return false.
    else
        return FALSE;
}

void SectionInfo::Init(PELoader *pPELoader, IMAGE_DATA_DIRECTORY *dir)
{
    _ASSERTE(dir);
    m_dwSectionOffset = VAL32(dir->VirtualAddress);
    if (m_dwSectionOffset != 0)
        m_pSection = pPELoader->base() + m_dwSectionOffset;
    else
        m_pSection = 0;
    m_dwSectionSize = VAL32(dir->Size);
}

