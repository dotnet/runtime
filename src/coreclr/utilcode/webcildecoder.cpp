// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// --------------------------------------------------------------------------------
// WebcilDecoder.cpp
//
// Implementation of the WebcilDecoder class.
// See webcildecoder.h for API documentation.
// --------------------------------------------------------------------------------

#include "stdafx.h"
#include "webcildecoder.h"

#ifdef FEATURE_WEBCIL

#include "corhlpr.h"

typedef DPTR(COR_ILMETHOD_TINY) PTR_COR_ILMETHOD_TINY;
typedef DPTR(COR_ILMETHOD_FAT) PTR_COR_ILMETHOD_FAT;
typedef DPTR(COR_ILMETHOD_SECT_SMALL) PTR_COR_ILMETHOD_SECT_SMALL;
typedef DPTR(COR_ILMETHOD_SECT_FAT) PTR_COR_ILMETHOD_SECT_FAT;

// ------------------------------------------------------------
// Format detection (static)
// ------------------------------------------------------------

bool WebcilDecoder::DetectWebcilFormat(const void* data, COUNT_T size)
{
    LIMITED_METHOD_CONTRACT;
    if (size < sizeof(WebcilHeader))
        return false;
    const uint8_t* bytes = static_cast<const uint8_t*>(data);
    return bytes[0] == 'W' && bytes[1] == 'b' && bytes[2] == 'I' && bytes[3] == 'L';
}

// ------------------------------------------------------------
// Construction / Initialization
// ------------------------------------------------------------

WebcilDecoder::WebcilDecoder()
    : m_base(0),
      m_size(0),
      m_hasContents(FALSE),
      m_pHeader(NULL),
      m_pCorHeader(NULL)
{
    LIMITED_METHOD_CONTRACT;
}

void WebcilDecoder::Init(void *flatBase, COUNT_T size)
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        PRECONDITION((size == 0) || CheckPointer(flatBase));
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    m_base = (TADDR)flatBase;
    m_size = size;
    m_hasContents = (size > 0);
    m_pHeader = (m_hasContents && size >= sizeof(WebcilHeader)) ? (const WebcilHeader *)flatBase : NULL;
    m_pCorHeader = NULL;
}

void WebcilDecoder::Reset()
{
    LIMITED_METHOD_CONTRACT;
    m_base = 0;
    m_size = 0;
    m_hasContents = FALSE;
    m_pHeader = NULL;
    m_pCorHeader = NULL;
}

// ------------------------------------------------------------
// Basic properties
// ------------------------------------------------------------

PTR_VOID WebcilDecoder::GetBase() const
{
    LIMITED_METHOD_CONTRACT;
    return PTR_VOID(m_base);
}

COUNT_T WebcilDecoder::GetSize() const
{
    LIMITED_METHOD_CONTRACT;
    return m_size;
}

BOOL WebcilDecoder::HasContents() const
{
    LIMITED_METHOD_CONTRACT;
    return m_hasContents;
}

// ------------------------------------------------------------
// Header checks
// ------------------------------------------------------------

BOOL WebcilDecoder::HasWebcilHeaders() const
{
    CONTRACT(BOOL)
    {
        INSTANCE_CHECK;
        NOTHROW;
        GC_NOTRIGGER;
        PRECONDITION(HasContents());
    }
    CONTRACT_END;

    if (m_size < sizeof(WebcilHeader))
        RETURN FALSE;

    const WebcilHeader *pHeader = (const WebcilHeader *)(m_base);
    if (pHeader->Id[0] != WEBCIL_MAGIC_W ||
        pHeader->Id[1] != WEBCIL_MAGIC_B ||
        pHeader->Id[2] != WEBCIL_MAGIC_I ||
        pHeader->Id[3] != WEBCIL_MAGIC_L)
    {
        RETURN FALSE;
    }

    if (pHeader->VersionMajor != WEBCIL_VERSION_MAJOR ||
        pHeader->VersionMinor != WEBCIL_VERSION_MINOR)
    {
        RETURN FALSE;
    }

    if (pHeader->CoffSections == 0 || pHeader->CoffSections > WEBCIL_MAX_SECTIONS)
        RETURN FALSE;

    COUNT_T headerEnd = sizeof(WebcilHeader) + (COUNT_T)pHeader->CoffSections * sizeof(WebcilSectionHeader);
    if (m_size < headerEnd)
        RETURN FALSE;

    RETURN TRUE;
}

CHECK WebcilDecoder::CheckWebcilHeaders() const
{
    CONTRACT_CHECK
    {
        INSTANCE_CHECK;
        NOTHROW;
        GC_NOTRIGGER;
        PRECONDITION(HasContents());
    }
    CONTRACT_CHECK_END;

    CHECK(HasWebcilHeaders());

    const WebcilHeader *pHeader = (const WebcilHeader *)m_base;
    const WebcilSectionHeader *sections = (const WebcilSectionHeader *)(m_base + sizeof(WebcilHeader));
    uint16_t numSections = pHeader->CoffSections;

    for (uint16_t i = 0; i < numSections; i++)
    {
        // PointerToRawData + SizeOfRawData must fit within file
        CHECK(CheckOverflow(sections[i].PointerToRawData, sections[i].SizeOfRawData));
        CHECK(sections[i].PointerToRawData + sections[i].SizeOfRawData <= m_size);

        // VirtualAddress ordering — sections should not overlap in virtual space
        for (uint16_t j = 0; j < i; j++)
        {
            // Check no overlap: either i is entirely before j, or entirely after
            uint32_t iStart = sections[i].VirtualAddress;
            CHECK(CheckOverflow(sections[i].VirtualAddress, sections[i].VirtualSize));
            uint32_t iEnd = sections[i].VirtualAddress + sections[i].VirtualSize;
            uint32_t jStart = sections[j].VirtualAddress;
            CHECK(CheckOverflow(sections[j].VirtualAddress, sections[j].VirtualSize));
            uint32_t jEnd = sections[j].VirtualAddress + sections[j].VirtualSize;
            CHECK(iEnd <= jStart || jEnd <= iStart);
        }
    }

    CHECK_OK;
}

CHECK WebcilDecoder::CheckNTHeaders() const
{
    CHECK_FAIL("Not a PE image");
    CHECK_OK;
}

// ------------------------------------------------------------
// Format checks
// ------------------------------------------------------------

CHECK WebcilDecoder::CheckFormat() const
{
    CONTRACT_CHECK
    {
        INSTANCE_CHECK;
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACT_CHECK_END;

    CHECK(HasContents());
    CHECK(HasWebcilHeaders());
    CHECK(CheckWebcilHeaders());

    CHECK_OK;
}

CHECK WebcilDecoder::CheckILFormat() const
{
    CONTRACT_CHECK
    {
        INSTANCE_CHECK;
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACT_CHECK_END;

    // TODO: implement — validate COR header and metadata
    CHECK(CheckFormat());
    CHECK(HasCorHeader());
    CHECK(CheckCorHeader());

    CHECK_OK;
}

CHECK WebcilDecoder::CheckILOnlyFormat() const
{
    CONTRACT_CHECK
    {
        INSTANCE_CHECK;
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACT_CHECK_END;

    // Webcil is always IL-only
    CHECK(CheckILFormat());

    CHECK_OK;
}

// ------------------------------------------------------------
// COR header
// ------------------------------------------------------------

void WebcilDecoder::FindCorHeader() const
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        NOTHROW;
        GC_NOTRIGGER;
        PRECONDITION(HasWebcilHeaders());
    }
    CONTRACTL_END;

    if (m_pCorHeader != NULL)
        return;

    const WebcilHeader *pHeader = (const WebcilHeader *)m_base;
    RVA corHeaderRva = pHeader->PeCliHeaderRva;
    COUNT_T corHeaderSize = pHeader->PeCliHeaderSize;

    if (corHeaderRva == 0 || corHeaderSize < sizeof(IMAGE_COR20_HEADER))
        return;

    const WebcilSectionHeader *section = RvaToSection(corHeaderRva);
    if (section == NULL)
        return;

    COUNT_T offset = corHeaderRva - section->VirtualAddress + section->PointerToRawData;
    if (offset + sizeof(IMAGE_COR20_HEADER) > m_size)
        return;

    m_pCorHeader = (IMAGE_COR20_HEADER *)(m_base + offset);
}

BOOL WebcilDecoder::HasCorHeader() const
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    if (!HasWebcilHeaders())
        return FALSE;

    FindCorHeader();
    return m_pCorHeader != NULL;
}

CHECK WebcilDecoder::CheckCorHeader() const
{
    CONTRACT_CHECK
    {
        INSTANCE_CHECK;
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACT_CHECK_END;

    CHECK(HasCorHeader());

    const WebcilHeader *pWcHeader = (const WebcilHeader *)m_base;
    CHECK(pWcHeader->PeCliHeaderSize >= sizeof(IMAGE_COR20_HEADER));
    CHECK(CheckRva(pWcHeader->PeCliHeaderRva, sizeof(IMAGE_COR20_HEADER)));

    IMAGE_COR20_HEADER *pCor = GetCorHeader();

    CHECK(VAL16(pCor->MajorRuntimeVersion) > 1 && VAL16(pCor->MajorRuntimeVersion) <= COR_VERSION_MAJOR);
    CHECK(VAL32(pCor->cb) >= offsetof(IMAGE_COR20_HEADER, ManagedNativeHeader) + sizeof(IMAGE_DATA_DIRECTORY));

    // Validate metadata directory
    CHECK(pCor->MetaData.VirtualAddress != 0);
    CHECK(pCor->MetaData.Size != 0);
    CHECK(CheckRva(VAL32(pCor->MetaData.VirtualAddress), VAL32(pCor->MetaData.Size)));

    // Validate resources directory if present
    if (pCor->Resources.VirtualAddress != 0)
        CHECK(CheckRva(VAL32(pCor->Resources.VirtualAddress), VAL32(pCor->Resources.Size)));

    // Validate strong name signature if present
    if (pCor->StrongNameSignature.VirtualAddress != 0)
        CHECK(CheckRva(VAL32(pCor->StrongNameSignature.VirtualAddress), VAL32(pCor->StrongNameSignature.Size)));

    // Webcil is always IL-only — no VTable fixups or EAT jumps
    CHECK(pCor->VTableFixups.Size == VAL32(0));
    CHECK(pCor->ExportAddressTableJumps.Size == VAL32(0));
    CHECK(!(pCor->Flags & VAL32(COMIMAGE_FLAGS_NATIVE_ENTRYPOINT)));

    // Strong name signed images should have a signature
    if (IsStrongNameSigned())
        CHECK(HasStrongNameSignature());

    CHECK_OK;
}

IMAGE_COR20_HEADER *WebcilDecoder::GetCorHeader() const
{
    CONTRACT(IMAGE_COR20_HEADER *)
    {
        INSTANCE_CHECK;
        NOTHROW;
        GC_NOTRIGGER;
        PRECONDITION(HasCorHeader());
        POSTCONDITION(CheckPointer(RETVAL));
    }
    CONTRACT_END;

    FindCorHeader();
    RETURN m_pCorHeader;
}

// ------------------------------------------------------------
// Metadata
// ------------------------------------------------------------

PTR_CVOID WebcilDecoder::GetMetadata(COUNT_T *pSize) const
{
    CONTRACT(PTR_CVOID)
    {
        INSTANCE_CHECK;
        NOTHROW;
        GC_NOTRIGGER;
        PRECONDITION(HasCorHeader());
    }
    CONTRACT_END;

    IMAGE_DATA_DIRECTORY *pDir = &GetCorHeader()->MetaData;

    if (pSize != NULL)
        *pSize = VAL32(pDir->Size);

    RVA rva = VAL32(pDir->VirtualAddress);
    if (rva == 0)
        RETURN NULL;

    RETURN dac_cast<PTR_VOID>(GetRvaData(rva));
}

// ------------------------------------------------------------
// Strong name
// ------------------------------------------------------------

BOOL WebcilDecoder::IsStrongNameSigned() const
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    if (!HasCorHeader())
        return FALSE;

    return ((GetCorHeader()->Flags & VAL32(COMIMAGE_FLAGS_STRONGNAMESIGNED)) != 0);
}

BOOL WebcilDecoder::HasStrongNameSignature() const
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    if (!HasCorHeader())
        return FALSE;

    return (GetCorHeader()->StrongNameSignature.VirtualAddress != 0);
}

PTR_CVOID WebcilDecoder::GetStrongNameSignature(COUNT_T *pSize) const
{
    CONTRACT(PTR_CVOID)
    {
        INSTANCE_CHECK;
        NOTHROW;
        GC_NOTRIGGER;
        PRECONDITION(HasCorHeader());
        PRECONDITION(HasStrongNameSignature());
    }
    CONTRACT_END;

    IMAGE_DATA_DIRECTORY *pDir = &GetCorHeader()->StrongNameSignature;

    if (pSize != NULL)
        *pSize = VAL32(pDir->Size);

    RETURN dac_cast<PTR_CVOID>(GetRvaData(VAL32(pDir->VirtualAddress)));
}

// ------------------------------------------------------------
// Entry point
// ------------------------------------------------------------

BOOL WebcilDecoder::HasManagedEntryPoint() const
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    if (!HasCorHeader())
        return FALSE;

    ULONG flags = GetCorHeader()->Flags;
    return (!(flags & VAL32(COMIMAGE_FLAGS_NATIVE_ENTRYPOINT)) &&
            (!IsNilToken(GetEntryPointToken())));
}

ULONG WebcilDecoder::GetEntryPointToken() const
{
    CONTRACT(ULONG)
    {
        INSTANCE_CHECK;
        NOTHROW;
        GC_NOTRIGGER;
        PRECONDITION(HasCorHeader());
    }
    CONTRACT_END;

    RETURN VAL32(IMAGE_COR20_HEADER_FIELD(*GetCorHeader(), EntryPointToken));
}

// ------------------------------------------------------------
// R2R / native manifest metadata
// ------------------------------------------------------------

PTR_CVOID WebcilDecoder::GetNativeManifestMetadata(COUNT_T *pSize) const
{
    LIMITED_METHOD_CONTRACT;
    if (pSize != NULL)
        *pSize = 0;
    return NULL;
}

// ------------------------------------------------------------
// RVA operations
// ------------------------------------------------------------

const WebcilSectionHeader *WebcilDecoder::RvaToSection(RVA rva) const
{
    LIMITED_METHOD_CONTRACT;

    if (!HasWebcilHeaders())
        return NULL;

    const WebcilHeader *pHeader = (const WebcilHeader *)m_base;
    const WebcilSectionHeader *section = (const WebcilSectionHeader *)(m_base + sizeof(WebcilHeader));
    const WebcilSectionHeader *sectionEnd = section + pHeader->CoffSections;

    while (section < sectionEnd)
    {
        // Webcil is always flat — check both virtual range and raw data range
        if (rva >= section->VirtualAddress &&
            rva < section->VirtualAddress + section->VirtualSize &&
            rva < section->VirtualAddress + section->SizeOfRawData)
        {
            return section;
        }

        section++;
    }

    return NULL;
}

const WebcilSectionHeader *WebcilDecoder::OffsetToSection(COUNT_T fileOffset) const
{
    LIMITED_METHOD_CONTRACT;

    if (!HasWebcilHeaders())
        return NULL;

    const WebcilHeader *pHeader = (const WebcilHeader *)m_base;
    const WebcilSectionHeader *section = (const WebcilSectionHeader *)(m_base + sizeof(WebcilHeader));
    const WebcilSectionHeader *sectionEnd = section + pHeader->CoffSections;

    while (section < sectionEnd)
    {
        if (fileOffset >= section->PointerToRawData &&
            fileOffset < section->PointerToRawData + section->SizeOfRawData)
        {
            return section;
        }

        section++;
    }

    return NULL;
}

CHECK WebcilDecoder::CheckRva(RVA rva, IsNullOK ok) const
{
    CONTRACT_CHECK
    {
        INSTANCE_CHECK;
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACT_CHECK_END;

    if (rva == 0)
        CHECK_MSG(ok == NULL_OK, "Zero RVA illegal");
    else
        CHECK(RvaToSection(rva) != NULL);

    CHECK_OK;
}

CHECK WebcilDecoder::CheckRva(RVA rva, COUNT_T size, int forbiddenFlags, IsNullOK ok) const
{
    CONTRACT_CHECK
    {
        INSTANCE_CHECK;
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACT_CHECK_END;

    (void)forbiddenFlags; // Webcil sections have no characteristics flags

    if (rva == 0)
    {
        CHECK_MSG(ok == NULL_OK, "Zero RVA illegal");
        CHECK(size == 0);
    }
    else
    {
        const WebcilSectionHeader *section = RvaToSection(rva);
        CHECK(section != NULL);

        CHECK(CheckOverflow(section->VirtualAddress, section->SizeOfRawData));
        CHECK(CheckOverflow(rva, size));
        CHECK(rva >= section->VirtualAddress);
        CHECK(rva + size <= section->VirtualAddress + section->SizeOfRawData);
    }

    CHECK_OK;
}

TADDR WebcilDecoder::GetRvaData(RVA rva, IsNullOK ok) const
{
    CONTRACT(TADDR)
    {
        INSTANCE_CHECK;
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACT_END;

    if ((rva == 0) && (ok == NULL_NOT_OK))
        RETURN (TADDR)NULL;

    // Webcil is always flat — translate RVA via section table to file offset
    COUNT_T offset = RvaToOffset(rva);
    RETURN (m_base + offset);
}

RVA WebcilDecoder::GetDataRva(const TADDR data) const
{
    CONTRACT(RVA)
    {
        INSTANCE_CHECK;
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACT_END;

    if (data == (TADDR)NULL)
        RETURN 0;

    // Webcil is always flat — convert file offset to RVA
    COUNT_T offset = (COUNT_T)(data - m_base);
    RETURN OffsetToRva(offset);
}

BOOL WebcilDecoder::PointerInPE(PTR_CVOID data) const
{
    LIMITED_METHOD_CONTRACT;

    TADDR taddrData = dac_cast<TADDR>(data);
    TADDR taddrBase = m_base;
    return (taddrData >= taddrBase) && (taddrData < (taddrBase + m_size));
}

// ------------------------------------------------------------
// Offset operations
// ------------------------------------------------------------

CHECK WebcilDecoder::CheckOffset(COUNT_T fileOffset, IsNullOK ok) const
{
    CONTRACT_CHECK
    {
        INSTANCE_CHECK;
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACT_CHECK_END;

    if (fileOffset == 0)
        CHECK_MSG(ok == NULL_OK, "Null pointer illegal");
    else
        CHECK(OffsetToSection(fileOffset) != NULL);

    CHECK_OK;
}

CHECK WebcilDecoder::CheckOffset(COUNT_T fileOffset, COUNT_T size, IsNullOK ok) const
{
    CONTRACT_CHECK
    {
        INSTANCE_CHECK;
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACT_CHECK_END;

    if (fileOffset == 0)
    {
        CHECK_MSG(ok == NULL_OK, "Zero fileOffset illegal");
        CHECK(size == 0);
    }
    else
    {
        const WebcilSectionHeader *section = OffsetToSection(fileOffset);
        CHECK(section != NULL);

        CHECK(CheckOverflow(section->PointerToRawData, section->SizeOfRawData));
        CHECK(CheckOverflow(fileOffset, size));
        CHECK(fileOffset >= section->PointerToRawData);
        CHECK(fileOffset + size <= section->PointerToRawData + section->SizeOfRawData);
    }

    CHECK_OK;
}

TADDR WebcilDecoder::GetOffsetData(COUNT_T fileOffset, IsNullOK ok) const
{
    CONTRACT(TADDR)
    {
        INSTANCE_CHECK;
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACT_END;

    if ((fileOffset == 0) && (ok == NULL_NOT_OK))
        RETURN (TADDR)NULL;

    RETURN GetRvaData(OffsetToRva(fileOffset));
}

COUNT_T WebcilDecoder::RvaToOffset(RVA rva) const
{
    CONTRACT(COUNT_T)
    {
        INSTANCE_CHECK;
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACT_END;

    if (rva == 0)
        RETURN 0;

    const WebcilSectionHeader *section = RvaToSection(rva);
    _ASSERTE(section != NULL);

    RETURN rva - section->VirtualAddress + section->PointerToRawData;
}

RVA WebcilDecoder::OffsetToRva(COUNT_T fileOffset) const
{
    CONTRACT(RVA)
    {
        INSTANCE_CHECK;
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACT_END;

    if (fileOffset == 0)
        RETURN 0;

    const WebcilSectionHeader *section = OffsetToSection(fileOffset);
    _ASSERTE(section != NULL);

    RETURN fileOffset - section->PointerToRawData + section->VirtualAddress;
}

// ------------------------------------------------------------
// Section access
// ------------------------------------------------------------

COUNT_T WebcilDecoder::GetNumberOfSections() const
{
    LIMITED_METHOD_CONTRACT;
    if (!HasWebcilHeaders())
        return 0;
    return ((const WebcilHeader *)m_base)->CoffSections;
}

COUNT_T WebcilDecoder::GetVirtualSize() const
{
    LIMITED_METHOD_CONTRACT;

    if (!HasWebcilHeaders())
        return m_size;

    const WebcilHeader *pHeader = (const WebcilHeader *)m_base;
    const WebcilSectionHeader *section = (const WebcilSectionHeader *)(m_base + sizeof(WebcilHeader));
    COUNT_T maxVA = 0;

    for (uint16_t i = 0; i < pHeader->CoffSections; i++)
    {
        COUNT_T sectionEnd = section[i].VirtualAddress + section[i].VirtualSize;
        if (sectionEnd > maxVA)
            maxVA = sectionEnd;
    }

    return maxVA > 0 ? maxVA : m_size;
}

// ------------------------------------------------------------
// PE kind
// ------------------------------------------------------------

void WebcilDecoder::GetPEKindAndMachine(DWORD *pdwPEKind, DWORD *pdwMachine)
{
    LIMITED_METHOD_CONTRACT;

    if (pdwPEKind != NULL)
        *pdwPEKind = peILonly;
    if (pdwMachine != NULL)
        *pdwMachine = IMAGE_FILE_MACHINE_UNKNOWN;
}

// ------------------------------------------------------------
// Directory entries
// ------------------------------------------------------------

BOOL WebcilDecoder::HasDirectoryEntry(int entry) const
{
    LIMITED_METHOD_CONTRACT;

    // Webcil has no PE IMAGE_DATA_DIRECTORY array.
    // Only the debug directory is stored in the WebcilHeader.
    if (entry == IMAGE_DIRECTORY_ENTRY_DEBUG && HasWebcilHeaders())
    {
        const WebcilHeader *pHeader = (const WebcilHeader *)m_base;
        return pHeader->PeDebugRva != 0 && pHeader->PeDebugSize != 0;
    }

    return FALSE;
}

TADDR WebcilDecoder::GetDirectoryEntryData(int entry, COUNT_T *pSize) const
{
    LIMITED_METHOD_CONTRACT;

    // Webcil stores debug directory info in the header
    if (entry == IMAGE_DIRECTORY_ENTRY_DEBUG && HasWebcilHeaders())
    {
        const WebcilHeader *pHeader = (const WebcilHeader *)m_base;
        RVA debugRva = pHeader->PeDebugRva;
        COUNT_T debugSize = pHeader->PeDebugSize;

        if (debugRva == 0 || debugSize == 0)
        {
            if (pSize != NULL)
                *pSize = 0;
            return (TADDR)0;
        }

        if (pSize != NULL)
            *pSize = debugSize;

        return GetRvaData(debugRva);
    }

    if (pSize != NULL)
        *pSize = 0;
    return (TADDR)0;
}

PTR_IMAGE_DEBUG_DIRECTORY WebcilDecoder::GetDebugDirectoryEntry(UINT index) const
{
    CONTRACT(PTR_IMAGE_DEBUG_DIRECTORY)
    {
        INSTANCE_CHECK;
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACT_END;

    if (!HasWebcilHeaders())
        RETURN NULL;

    const WebcilHeader *pHeader = (const WebcilHeader *)m_base;
    if (pHeader->PeDebugRva == 0 || pHeader->PeDebugSize == 0)
        RETURN NULL;

    COUNT_T cbDebugDir;
    TADDR taDebugDir = GetDirectoryEntryData(IMAGE_DIRECTORY_ENTRY_DEBUG, &cbDebugDir);

    if (taDebugDir == (TADDR)0)
        RETURN NULL;

    UINT cNumEntries = cbDebugDir / sizeof(IMAGE_DEBUG_DIRECTORY);
    if (index >= cNumEntries)
        RETURN NULL;

    PTR_IMAGE_DEBUG_DIRECTORY pDebugEntry = dac_cast<PTR_IMAGE_DEBUG_DIRECTORY>(taDebugDir);
    pDebugEntry += index;
    RETURN pDebugEntry;
}

// ------------------------------------------------------------
// Resources
// ------------------------------------------------------------

const void *WebcilDecoder::GetResources(COUNT_T *pSize) const
{
    CONTRACT(const void *)
    {
        INSTANCE_CHECK;
        NOTHROW;
        GC_NOTRIGGER;
        PRECONDITION(HasCorHeader());
    }
    CONTRACT_END;

    IMAGE_DATA_DIRECTORY *pDir = &GetCorHeader()->Resources;

    if (pSize != NULL)
        *pSize = VAL32(pDir->Size);

    RVA rva = VAL32(pDir->VirtualAddress);
    if (rva == 0)
        RETURN NULL;

    RETURN (void *)GetRvaData(rva);
}

CHECK WebcilDecoder::CheckResource(COUNT_T offset) const
{
    CONTRACT_CHECK
    {
        INSTANCE_CHECK;
        NOTHROW;
        GC_NOTRIGGER;
        PRECONDITION(HasCorHeader());
    }
    CONTRACT_CHECK_END;

    IMAGE_DATA_DIRECTORY *pDir = &GetCorHeader()->Resources;

    CHECK(CheckOverflow(VAL32(pDir->VirtualAddress), offset));

    RVA rva = VAL32(pDir->VirtualAddress) + offset;

    // Need at least a length DWORD for the resource size
    CHECK(CheckRva(rva, sizeof(DWORD)));

    // Read the resource size
    DWORD resourceSize = GET_UNALIGNED_VAL32((LPVOID)GetRvaData(rva));

    // Compute start and end of the resource using overflow-checked arithmetic
    S_UINT32 dataStartRva = S_UINT32(rva) + sizeof(DWORD);
    CHECK(!dataStartRva.IsOverflow());

    S_UINT32 resourceEndRva = dataStartRva + resourceSize;
    CHECK(!resourceEndRva.IsOverflow());

    // Compute end of the resources directory using overflow-checked arithmetic
    S_UINT32 resourcesEnd = S_UINT32(VAL32(pDir->VirtualAddress)) + VAL32(pDir->Size);
    CHECK(!resourcesEnd.IsOverflow());

    // Check resource is within resource section
    CHECK((UINT32)resourceEndRva <= (UINT32)resourcesEnd);

    CHECK_OK;
}

const void *WebcilDecoder::GetResource(COUNT_T offset, COUNT_T *pSize) const
{
    CONTRACT(const void *)
    {
        INSTANCE_CHECK;
        NOTHROW;
        GC_NOTRIGGER;
        PRECONDITION(HasCorHeader());
    }
    CONTRACT_END;

    IMAGE_DATA_DIRECTORY *pDir = &GetCorHeader()->Resources;

    if (CheckResource(offset) == FALSE)
        RETURN NULL;

    void *resourceBlob = (void *)GetRvaData(VAL32(pDir->VirtualAddress) + offset);
    _ASSERTE(resourceBlob != NULL);

    if (pSize != NULL)
        *pSize = GET_UNALIGNED_VAL32(resourceBlob);

    RETURN (const void *)((BYTE *)resourceBlob + sizeof(DWORD));
}

// ------------------------------------------------------------
// IL method validation
// ------------------------------------------------------------

CHECK WebcilDecoder::CheckILMethod(RVA rva)
{
    CONTRACT_CHECK
    {
        INSTANCE_CHECK;
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACT_CHECK_END;

    // Validate that the IL method body is within image bounds
    CHECK(CheckRva(rva, sizeof(IMAGE_COR_ILMETHOD_TINY)));

    TADDR pIL = GetRvaData(rva);

    PTR_COR_ILMETHOD_TINY pMethodTiny = PTR_COR_ILMETHOD_TINY(pIL);

    if (pMethodTiny->IsTiny())
    {
        CHECK(CheckRva(rva, sizeof(IMAGE_COR_ILMETHOD_TINY) + pMethodTiny->GetCodeSize()));
        CHECK_OK;
    }

    CHECK(CheckRva(rva, sizeof(IMAGE_COR_ILMETHOD_FAT)));

    PTR_COR_ILMETHOD_FAT pMethodFat = PTR_COR_ILMETHOD_FAT(pIL);
    CHECK(pMethodFat->IsFat());

    S_UINT32 codeEnd = S_UINT32(4) * S_UINT32(pMethodFat->GetSize()) + S_UINT32(pMethodFat->GetCodeSize());
    CHECK(!codeEnd.IsOverflow());
    CHECK(pMethodFat->GetSize() >= (sizeof(COR_ILMETHOD_FAT) / 4));
    CHECK(CheckRva(rva, codeEnd.Value()));

    if (!pMethodFat->More())
        CHECK_OK;

    TADDR pSect = AlignUp(pIL + codeEnd.Value(), 4);

    while (true)
    {
        CHECK(CheckRva(rva, UINT32(pSect - pIL) + sizeof(IMAGE_COR_ILMETHOD_SECT_SMALL)));

        PTR_COR_ILMETHOD_SECT_SMALL pSectSmall = PTR_COR_ILMETHOD_SECT_SMALL(pSect);

        UINT32 sectSize;

        if (pSectSmall->IsSmall())
        {
            sectSize = pSectSmall->DataSize;
            if ((pSectSmall->Kind & CorILMethod_Sect_KindMask) == CorILMethod_Sect_EHTable)
                sectSize = COR_ILMETHOD_SECT_EH_SMALL::Size(sectSize / sizeof(IMAGE_COR_ILMETHOD_SECT_EH_CLAUSE_SMALL));
        }
        else
        {
            CHECK(CheckRva(rva, UINT32(pSect - pIL) + sizeof(IMAGE_COR_ILMETHOD_SECT_FAT)));

            PTR_COR_ILMETHOD_SECT_FAT pSectFat = PTR_COR_ILMETHOD_SECT_FAT(pSect);
            sectSize = pSectFat->GetDataSize();
            if ((pSectSmall->Kind & CorILMethod_Sect_KindMask) == CorILMethod_Sect_EHTable)
                sectSize = COR_ILMETHOD_SECT_EH_FAT::Size(sectSize / sizeof(IMAGE_COR_ILMETHOD_SECT_EH_CLAUSE_FAT));
        }

        CHECK(sectSize > 0);

        S_UINT32 sectEnd = S_UINT32(UINT32(pSect - pIL)) + S_UINT32(sectSize);
        CHECK(!sectEnd.IsOverflow());
        CHECK(CheckRva(rva, sectEnd.Value()));

        if (!pSectSmall->More())
            CHECK_OK;

        pSect = AlignUp(pIL + sectEnd.Value(), 4);
    }
}

// ------------------------------------------------------------
// DAC support
// ------------------------------------------------------------

#ifdef DACCESS_COMPILE
void WebcilDecoder::EnumMemoryRegions(CLRDataEnumMemoryFlags flags, bool enumThis)
{
    SUPPORTS_DAC;

    if (enumThis)
    {
        DacEnumMemoryRegion(m_base, sizeof(WebcilHeader));
    }

    if (HasWebcilHeaders())
    {
        // Enumerate section headers
        const WebcilHeader *pHeader = (const WebcilHeader *)m_base;
        DacEnumMemoryRegion(m_base + sizeof(WebcilHeader),
                           sizeof(WebcilSectionHeader) * pHeader->CoffSections);

        // Enumerate COR header if present
        if (m_pCorHeader != NULL)
        {
            DacEnumMemoryRegion(dac_cast<TADDR>(m_pCorHeader), sizeof(IMAGE_COR20_HEADER));
        }
    }
}
#endif // DACCESS_COMPILE

#endif // FEATURE_WEBCIL
