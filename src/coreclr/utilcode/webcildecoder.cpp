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

#include "cordecoderhelpers.h"

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
      m_sections(NULL),
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
    if (m_pHeader != NULL && m_pHeader->VersionMajor >= 1)
    {
        // For version 1 and above, the section headers start after the larger header
        if (size < sizeof(WebcilHeader_1))
        {
            m_pHeader = NULL; // Not enough data for even the header
        }
    }
    if (!m_pHeader)
    {
        m_sections = NULL;
    }
    else
    {
        m_sections = (const WebcilSectionHeader *)(((uint8_t*)flatBase) + (m_pHeader->VersionMajor >= 1 ? sizeof(WebcilHeader_1) : sizeof(WebcilHeader)));
    }
    m_pCorHeader = NULL;
    m_relocated = FALSE;
}

void WebcilDecoder::Reset()
{
    LIMITED_METHOD_CONTRACT;
    m_base = 0;
    m_size = 0;
    m_hasContents = FALSE;
    m_pHeader = NULL;
    m_sections = NULL;
    m_pCorHeader = NULL;
    m_relocated = FALSE;
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

    if ((pHeader->VersionMajor != WEBCIL_VERSION_MAJOR_0 && pHeader->VersionMajor != WEBCIL_VERSION_MAJOR_1) ||
        pHeader->VersionMinor != WEBCIL_VERSION_MINOR)
    {
        RETURN FALSE;
    }

    if (pHeader->CoffSections == 0 || pHeader->CoffSections > WEBCIL_MAX_SECTIONS)
        RETURN FALSE;

    COUNT_T headerSize;
    if (pHeader->VersionMajor == WEBCIL_VERSION_MAJOR_0)
    {
        headerSize = sizeof(WebcilHeader);
    }
    else
    {
        headerSize = sizeof(WebcilHeader_1);
    }

    if (m_size < headerSize)
        RETURN FALSE;

    COUNT_T headerEnd = headerSize + (COUNT_T)pHeader->CoffSections * sizeof(WebcilSectionHeader);
    if (m_size < headerEnd)
        RETURN FALSE;

    RETURN TRUE;
}

BOOL WebcilDecoder::HasBaseRelocations() const
{
    LIMITED_METHOD_CONTRACT;
    if (!HasWebcilHeaders())
        return FALSE;

    const WebcilHeader *pHeader = (const WebcilHeader *)m_base;
    uint32_t relocSectionIndex = pHeader->Reserved0;
    uint16_t numSections = pHeader->CoffSections;

    if (relocSectionIndex == 0 || relocSectionIndex > numSections)
        return FALSE;

    return TRUE;
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
    const WebcilSectionHeader *sections = m_sections;
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

    S_UINT32 offset32 = S_UINT32(corHeaderRva)
                      - S_UINT32(section->VirtualAddress)
                      + S_UINT32(section->PointerToRawData);
    if (offset32.IsOverflow())
        return;

    COUNT_T offset = static_cast<COUNT_T>(offset32.Value());

    if (m_size < sizeof(IMAGE_COR20_HEADER))
        return;

    if (offset > m_size - sizeof(IMAGE_COR20_HEADER))
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

    if (m_pCorHeader != NULL)
        return TRUE;

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

    if (m_pCorHeader != NULL)
        RETURN m_pCorHeader;

    FindCorHeader();
    RETURN m_pCorHeader;
}

// ------------------------------------------------------------
// Metadata
// ------------------------------------------------------------

PTR_CVOID WebcilDecoder::GetMetadata(COUNT_T *pSize) const
{
    WRAPPER_NO_CONTRACT;
    return CorDecoderHelpers::GetMetadata(*this, pSize);
}

// ------------------------------------------------------------
// Strong name
// ------------------------------------------------------------

BOOL WebcilDecoder::IsStrongNameSigned() const
{
    WRAPPER_NO_CONTRACT;

    if (!HasCorHeader())
        return FALSE;

    return CorDecoderHelpers::IsStrongNameSigned(*this);
}

BOOL WebcilDecoder::HasStrongNameSignature() const
{
    WRAPPER_NO_CONTRACT;

    if (!HasCorHeader())
        return FALSE;

    return CorDecoderHelpers::HasStrongNameSignature(*this);
}

// ------------------------------------------------------------
// Entry point
// ------------------------------------------------------------

BOOL WebcilDecoder::HasManagedEntryPoint() const
{
    WRAPPER_NO_CONTRACT;

    if (!HasCorHeader())
        return FALSE;

    return CorDecoderHelpers::HasManagedEntryPoint(*this);
}

ULONG WebcilDecoder::GetEntryPointToken() const
{
    WRAPPER_NO_CONTRACT;
    return CorDecoderHelpers::GetEntryPointToken(*this);
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
    const WebcilSectionHeader *section = m_sections;
    const WebcilSectionHeader *sectionEnd = section + pHeader->CoffSections;

    while (section < sectionEnd)
    {
        // Webcil is always flat — check both virtual range and raw data range
        // Use subtraction-based checks to avoid uint32 overflow
        if (rva >= section->VirtualAddress)
        {
            uint32_t offset = rva - section->VirtualAddress;
            if (offset < section->VirtualSize && offset < section->SizeOfRawData)
            {
                return section;
            }
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
    const WebcilSectionHeader *section = m_sections;
    const WebcilSectionHeader *sectionEnd = section + pHeader->CoffSections;

    while (section < sectionEnd)
    {
        if (fileOffset >= section->PointerToRawData)
        {
            COUNT_T offsetWithinSection = fileOffset - section->PointerToRawData;
            if (offsetWithinSection < section->SizeOfRawData)
            {
                return section;
            }
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
    const WebcilSectionHeader *section = m_sections;
    COUNT_T maxVA = 0;

    for (uint16_t i = 0; i < pHeader->CoffSections; i++)
    {
        S_UINT32 sectionEnd = S_UINT32(section[i].VirtualAddress) + S_UINT32(section[i].VirtualSize);
        if (!sectionEnd.IsOverflow() && sectionEnd.Value() > maxVA)
            maxVA = sectionEnd.Value();
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
        // IL-only arch-neutral images use IMAGE_FILE_MACHINE_I386 for historic reasons
        *pdwMachine = IMAGE_FILE_MACHINE_I386;
}

// ------------------------------------------------------------
// Directory entries
// ------------------------------------------------------------

BOOL WebcilDecoder::HasDirectoryEntry(int entry) const
{
    LIMITED_METHOD_CONTRACT;

    // Webcil has no PE IMAGE_DATA_DIRECTORY array.
    // Only the debug directory and base relocation directory are supported.
    if (entry == IMAGE_DIRECTORY_ENTRY_DEBUG && HasWebcilHeaders())
    {
        const WebcilHeader *pHeader = (const WebcilHeader *)m_base;
        return pHeader->PeDebugRva != 0 && pHeader->PeDebugSize != 0;
    }

    if (entry == IMAGE_DIRECTORY_ENTRY_BASERELOC && HasWebcilHeaders())
    {
        return HasBaseRelocations();
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

        // Validate the full debug directory range is within a section
        if (!CheckRva(debugRva, debugSize, 0, NULL_NOT_OK))
        {
            if (pSize != NULL)
                *pSize = 0;
            return (TADDR)0;
        }

        if (pSize != NULL)
            *pSize = debugSize;

        return GetRvaData(debugRva);
    }

    // Base relocations: Reserved0 is the 1-based index of the relocations section
    if (entry == IMAGE_DIRECTORY_ENTRY_BASERELOC && HasWebcilHeaders())
    {
        const WebcilHeader *pHeader = (const WebcilHeader *)m_base;
        uint16_t relocSectionIndex = pHeader->Reserved0;
        if (relocSectionIndex == 0)
        {
            if (pSize != NULL)
                *pSize = 0;
            return (TADDR)0;
        }

        // Convert from 1-based to 0-based index and validate
        uint16_t sectionIndex = relocSectionIndex - 1;
        if (sectionIndex >= pHeader->CoffSections)
        {
            if (pSize != NULL)
                *pSize = 0;
            return (TADDR)0;
        }

        const WebcilSectionHeader *sections = (const WebcilSectionHeader *)(m_base + sizeof(WebcilHeader));
        const WebcilSectionHeader *relocSection = &sections[sectionIndex];

        if (pSize != NULL)
            *pSize = relocSection->SizeOfRawData;

        return (TADDR)(m_base + relocSection->PointerToRawData);
    }

    if (pSize != NULL)
        *pSize = 0;
    return (TADDR)0;
}

PTR_IMAGE_DEBUG_DIRECTORY WebcilDecoder::GetDebugDirectoryEntry(UINT index) const
{
    WRAPPER_NO_CONTRACT;
    return CorDecoderHelpers::GetDebugDirectoryEntry(*this, index);
}

// ------------------------------------------------------------
// Resources
// ------------------------------------------------------------

const void *WebcilDecoder::GetResources(COUNT_T *pSize) const
{
    WRAPPER_NO_CONTRACT;
    return CorDecoderHelpers::GetResources(*this, pSize);
}

CHECK WebcilDecoder::CheckResource(COUNT_T offset) const
{
    WRAPPER_NO_CONTRACT;
    return CorDecoderHelpers::CheckResource(*this, offset);
}

const void *WebcilDecoder::GetResource(COUNT_T offset, COUNT_T *pSize) const
{
    WRAPPER_NO_CONTRACT;
    return CorDecoderHelpers::GetResource(*this, offset, pSize);
}

// ------------------------------------------------------------
// IL method validation
// ------------------------------------------------------------

CHECK WebcilDecoder::CheckILMethod(RVA rva)
{
    WRAPPER_NO_CONTRACT;
    return CorDecoderHelpers::CheckILMethod(*this, rva);
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
        if (m_sections != NULL)
        {
            DacEnumMemoryRegion(m_base, dac_cast<TADDR>(m_sections) - dac_cast<TADDR>(m_base));
        }
        else
        {
            DacEnumMemoryRegion(m_base, min(m_size, (COUNT_T)sizeof(WebcilHeader)));
        }
    }

    if (HasWebcilHeaders() && m_sections != NULL)
    {
        // Enumerate section headers
        const WebcilHeader *pHeader = (const WebcilHeader *)m_base;
        DacEnumMemoryRegion(dac_cast<TADDR>(m_sections), sizeof(WebcilSectionHeader) * pHeader->CoffSections);

        // Enumerate COR header if present
        if (m_pCorHeader != NULL)
        {
            DacEnumMemoryRegion(dac_cast<TADDR>(m_pCorHeader), sizeof(IMAGE_COR20_HEADER));
        }
    }
}
#endif // DACCESS_COMPILE

#endif // FEATURE_WEBCIL
