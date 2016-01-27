// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// --------------------------------------------------------------------------------
// PEDecoder.cpp
//

// --------------------------------------------------------------------------------

#include "stdafx.h"

#include "ex.h"
#include "pedecoder.h"
#include "mdcommon.h"
#include "nibblemapmacros.h"

CHECK PEDecoder::CheckFormat() const
{
    CONTRACT_CHECK
    {
        INSTANCE_CHECK;
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACT_CHECK_END;

    CHECK(HasContents());

    if (HasNTHeaders())
    {
        CHECK(CheckNTHeaders());

        if (HasCorHeader())
        {
            CHECK(CheckCorHeader());

#if !defined(FEATURE_MIXEDMODE) && !defined(FEATURE_PREJIT)
            CHECK(IsILOnly());
#endif

            if (IsILOnly() && !HasReadyToRunHeader())
                CHECK(CheckILOnly());

            if (HasNativeHeader())
                CHECK(CheckNativeHeader());

            CHECK(CheckWillCreateGuardPage());
        }
    }

    CHECK_OK;
}

CHECK PEDecoder::CheckNTFormat() const
{
    CONTRACT_CHECK
    {
        INSTANCE_CHECK;
        NOTHROW;
        GC_NOTRIGGER;
        PRECONDITION(HasContents());
    }
    CONTRACT_CHECK_END;

    CHECK(CheckFormat());
    CHECK(HasNTHeaders());

    CHECK_OK;
}

CHECK PEDecoder::CheckCORFormat() const
{
    CONTRACT_CHECK
    {
        INSTANCE_CHECK;
        NOTHROW;
        GC_NOTRIGGER;
        PRECONDITION(HasContents());
    }
    CONTRACT_CHECK_END;

    CHECK(CheckFormat());
    CHECK(HasNTHeaders());
    CHECK(HasCorHeader());

    CHECK_OK;
}


CHECK PEDecoder::CheckILFormat() const
{
    CONTRACT_CHECK
    {
        INSTANCE_CHECK;
        NOTHROW;
        GC_NOTRIGGER;
        PRECONDITION(HasContents());
    }
    CONTRACT_CHECK_END;

    CHECK(CheckFormat());
    CHECK(HasNTHeaders());
    CHECK(HasCorHeader());
    CHECK(!HasNativeHeader());

    CHECK_OK;
}


CHECK PEDecoder::CheckILOnlyFormat() const
{
    CONTRACT_CHECK
    {
        INSTANCE_CHECK;
        NOTHROW;
        GC_NOTRIGGER;
        PRECONDITION(HasContents());
    }
    CONTRACT_CHECK_END;

    CHECK(CheckFormat());
    CHECK(HasNTHeaders());
    CHECK(HasCorHeader());
    CHECK(IsILOnly());
    CHECK(!HasNativeHeader());

    CHECK_OK;
}

CHECK PEDecoder::CheckNativeFormat() const
{
    CONTRACT_CHECK
    {
        INSTANCE_CHECK;
        NOTHROW;
        GC_NOTRIGGER;
        PRECONDITION(HasContents());
    }
    CONTRACT_CHECK_END;

#ifdef FEATURE_PREJIT
    CHECK(CheckFormat());
    CHECK(HasNTHeaders());
    CHECK(HasCorHeader());
    CHECK(!IsILOnly());
    CHECK(HasNativeHeader());
#else // FEATURE_PREJIT
    CHECK(false);
#endif // FEATURE_PREJIT

    CHECK_OK;
}

BOOL PEDecoder::HasNTHeaders() const
{
    CONTRACT(BOOL)
    {
        INSTANCE_CHECK;
        NOTHROW;
        GC_NOTRIGGER;
        SUPPORTS_DAC;
        PRECONDITION(HasContents());
        SO_TOLERANT;
    }
    CONTRACT_END;

    // Check for a valid DOS header

    if (m_size < sizeof(IMAGE_DOS_HEADER))
        RETURN FALSE;

    IMAGE_DOS_HEADER* pDOS = PTR_IMAGE_DOS_HEADER(m_base);

    {
        if (pDOS->e_magic != VAL16(IMAGE_DOS_SIGNATURE)
            || (DWORD) pDOS->e_lfanew == VAL32(0))
        {
            RETURN FALSE;
        }

        // Check for integer overflow
        S_SIZE_T cbNTHeaderEnd(S_SIZE_T(static_cast<SIZE_T>(VAL32(pDOS->e_lfanew))) +
                               S_SIZE_T(sizeof(IMAGE_NT_HEADERS)));
        if (cbNTHeaderEnd.IsOverflow())
        {
            RETURN FALSE;
        }

        // Now check for a valid NT header
        if (m_size < cbNTHeaderEnd.Value())
        {
            RETURN FALSE;
        }
    }

    IMAGE_NT_HEADERS *pNT = PTR_IMAGE_NT_HEADERS(m_base + VAL32(pDOS->e_lfanew));

    if (pNT->Signature != VAL32(IMAGE_NT_SIGNATURE))
        RETURN FALSE;

    if (pNT->OptionalHeader.Magic == VAL16(IMAGE_NT_OPTIONAL_HDR32_MAGIC))
    {
        if (pNT->FileHeader.SizeOfOptionalHeader != VAL16(sizeof(IMAGE_OPTIONAL_HEADER32)))
            RETURN FALSE;
    }
    else if (pNT->OptionalHeader.Magic == VAL16(IMAGE_NT_OPTIONAL_HDR64_MAGIC))
    {
        // on 64 bit we can promote this
        if (pNT->FileHeader.SizeOfOptionalHeader != VAL16(sizeof(IMAGE_OPTIONAL_HEADER64)))
            RETURN FALSE;

        // Check for integer overflow
        S_SIZE_T cbNTHeaderEnd(S_SIZE_T(static_cast<SIZE_T>(VAL32(pDOS->e_lfanew))) +
                               S_SIZE_T(sizeof(IMAGE_NT_HEADERS64)));

        if (cbNTHeaderEnd.IsOverflow())
        {
            RETURN FALSE;
    }

        // Now check for a valid NT header
        if (m_size < cbNTHeaderEnd.Value())
        {
            RETURN FALSE;
        }

    }
    else
        RETURN FALSE;

    // Go ahead and cache NT header since we already found it.
    const_cast<PEDecoder *>(this)->m_pNTHeaders = dac_cast<PTR_IMAGE_NT_HEADERS>(pNT);

    RETURN TRUE;
}

CHECK PEDecoder::CheckNTHeaders() const
{
    CONTRACT_CHECK
    {
        INSTANCE_CHECK;
        NOTHROW;
        GC_NOTRIGGER;
        SUPPORTS_DAC;
        PRECONDITION(HasContents());
        SO_TOLERANT;
    }
    CONTRACT_CHECK_END;

    // Only check once per file
    if (m_flags & FLAG_NT_CHECKED)
        CHECK_OK;

    CHECK(HasNTHeaders());

    IMAGE_NT_HEADERS *pNT = FindNTHeaders();

    CHECK((pNT->FileHeader.Characteristics & VAL16(IMAGE_FILE_SYSTEM)) == 0);

    CHECK(CheckAlignment(VAL32(pNT->OptionalHeader.FileAlignment)));
    CHECK(CheckAlignment(VAL32(pNT->OptionalHeader.SectionAlignment)));

    CHECK(CheckAligned((UINT)VAL32(pNT->OptionalHeader.FileAlignment), 512));
    CHECK(CheckAligned((UINT)VAL32(pNT->OptionalHeader.SectionAlignment), VAL32(pNT->OptionalHeader.FileAlignment)));

    // INVESTIGATE: this doesn't seem to be necessary on Win64 - why??
    //CHECK(CheckAligned((UINT)VAL32(pNT->OptionalHeader.SectionAlignment), OS_PAGE_SIZE));
    CHECK(CheckAligned((UINT)VAL32(pNT->OptionalHeader.SectionAlignment), 0x1000)); // for base relocs logic
    CHECK(CheckAligned((UINT)VAL32(pNT->OptionalHeader.SizeOfImage), VAL32(pNT->OptionalHeader.SectionAlignment)));
    CHECK(CheckAligned((UINT)VAL32(pNT->OptionalHeader.SizeOfHeaders), VAL32(pNT->OptionalHeader.FileAlignment)));

    // Data directories will be validated later on.
    PTR_IMAGE_DATA_DIRECTORY pDataDirectories = NULL;

    if (Has32BitNTHeaders())
    {
        IMAGE_NT_HEADERS32* pNT32=GetNTHeaders32();
        CHECK(CheckAligned(VAL32(pNT32->OptionalHeader.ImageBase), 0x10000));
        CHECK((VAL32(pNT32->OptionalHeader.SizeOfStackCommit) <= VAL32(pNT32->OptionalHeader.SizeOfStackReserve)));
        CHECK((VAL32(pNT32->OptionalHeader.SizeOfHeapCommit) <= VAL32(pNT32->OptionalHeader.SizeOfHeapReserve)));
        pDataDirectories = dac_cast<PTR_IMAGE_DATA_DIRECTORY>(
            dac_cast<TADDR>(pNT32) + offsetof(IMAGE_NT_HEADERS32, OptionalHeader.DataDirectory));
    }
    else
    {
        IMAGE_NT_HEADERS64* pNT64=GetNTHeaders64();
        CHECK(CheckAligned(VAL64(pNT64->OptionalHeader.ImageBase), 0x10000));
        CHECK((VAL64(pNT64->OptionalHeader.SizeOfStackCommit) <= VAL64(pNT64->OptionalHeader.SizeOfStackReserve)));
        CHECK((VAL64(pNT64->OptionalHeader.SizeOfHeapCommit) <= VAL64(pNT64->OptionalHeader.SizeOfHeapReserve)));
        pDataDirectories = dac_cast<PTR_IMAGE_DATA_DIRECTORY>(
            dac_cast<TADDR>(pNT64) + offsetof(IMAGE_NT_HEADERS64, OptionalHeader.DataDirectory));
    }

    // @todo: this is a bit awkward here, it would be better to make this assertion on
    // PEDecoder instantiation.  However, we don't necessarily have the NT headers there (in fact
    // they might not exist.)

    if (IsMapped())
    {
        // Ideally we would require the layout address to honor the section alignment constraints.
        // However, we do have 8K aligned IL only images which we load on 32 bit platforms. In this
        // case, we can only guarantee OS page alignment (which after all, is good enough.)
        CHECK(CheckAligned(m_base, OS_PAGE_SIZE));
    }

    // @todo: check NumberOfSections for overflow of SizeOfHeaders

    UINT32 currentAddress  = 0;
    UINT32 currentOffset = 0;

    CHECK(CheckSection(currentAddress, 0, VAL32(pNT->OptionalHeader.SizeOfHeaders),
                       currentOffset, 0, VAL32(pNT->OptionalHeader.SizeOfHeaders)));

    currentAddress=currentOffset=VAL32(pNT->OptionalHeader.SizeOfHeaders);

    PTR_IMAGE_SECTION_HEADER section = FindFirstSection(pNT);
    PTR_IMAGE_SECTION_HEADER sectionEnd = section + VAL16(pNT->FileHeader.NumberOfSections);

    CHECK(sectionEnd >= section);


    while (section < sectionEnd)
    {

        //
        // NOTE: the if condition is becuase of a design issue in the CLR and OS loader's remapping
        // of PE32 headers to PE32+. Because IMAGE_NT_HEADERS64 is bigger than IMAGE_NT_HEADERS32,
        // the remapping will expand this part of the header and push out the following
        // IMAGE_SECTION_HEADER entries. When IMAGE_DOS_HEADER::e_lfanew is large enough (size is
        // proportional to the number of tools used to produce the inputs to the C++ linker, and
        // has become larger when producing some WinMD files) this can push the last section header
        // beyond the boundary set by IMAGE_NT_HEADERS::OptionalHeader.SizeOfHeaders (e.g., this
        // was recently seen where the unaligned size of the headers was 0x1f8 and SizeOfHeaders was
        // 0x200, and the header remapping resulted in new headers size of 0x208). To compensate
        // for this issue (it would be quite difficult to fix in the remapping code; see Dev11 430008)
        // we assume that when the image is mapped that the needed validation has already been done.
        // 

        if (!IsMapped())
        {
            CHECK(CheckBounds(dac_cast<PTR_CVOID>(pNT),VAL32(pNT->OptionalHeader.SizeOfHeaders),
                              section,sizeof(IMAGE_SECTION_HEADER)));
        }

        // Check flags
        // Only allow a small list of characteristics
        CHECK(!(section->Characteristics &
            ~(VAL32((IMAGE_SCN_CNT_CODE           |
                  IMAGE_SCN_CNT_INITIALIZED_DATA  |
                  IMAGE_SCN_CNT_UNINITIALIZED_DATA|
                  IMAGE_SCN_MEM_DISCARDABLE       |
                  IMAGE_SCN_MEM_NOT_CACHED        |
                  IMAGE_SCN_MEM_NOT_PAGED         |
                  IMAGE_SCN_MEM_EXECUTE           |
                  IMAGE_SCN_MEM_READ              |
                  IMAGE_SCN_MEM_WRITE             |
                  // allow shared sections for all images for now.
                  // we'll constrain this in CheckILOnly
                  IMAGE_SCN_MEM_SHARED)))));

        // we should not allow writable code sections, check if both flags are set
        CHECK((section->Characteristics & VAL32((IMAGE_SCN_CNT_CODE|IMAGE_SCN_MEM_WRITE))) !=
            VAL32((IMAGE_SCN_CNT_CODE|IMAGE_SCN_MEM_WRITE)));

        CHECK(CheckSection(currentAddress, VAL32(section->VirtualAddress), VAL32(section->Misc.VirtualSize),
                           currentOffset, VAL32(section->PointerToRawData), VAL32(section->SizeOfRawData)));

        currentAddress = VAL32(section->VirtualAddress)
          + AlignUp((UINT)VAL32(section->Misc.VirtualSize), (UINT)VAL32(pNT->OptionalHeader.SectionAlignment));
        currentOffset = VAL32(section->PointerToRawData) + VAL32(section->SizeOfRawData);

        section++;
    }

    // Now check that the COR data directory is either NULL, or exists entirely in one section.
    {
        PTR_IMAGE_DATA_DIRECTORY pCORDataDir = pDataDirectories + IMAGE_DIRECTORY_ENTRY_COMHEADER;
        CHECK(CheckRva(VAL32(pCORDataDir->VirtualAddress), VAL32(pCORDataDir->Size), 0, NULL_OK));
    }

    // @todo: verify directory entries

    const_cast<PEDecoder *>(this)->m_flags |= FLAG_NT_CHECKED;

    CHECK_OK;
}

CHECK PEDecoder::CheckSection(COUNT_T previousAddressEnd, COUNT_T addressStart, COUNT_T addressSize,
                              COUNT_T previousOffsetEnd, COUNT_T offsetStart, COUNT_T offsetSize) const
{
    CONTRACT_CHECK
    {
        INSTANCE_CHECK;
        PRECONDITION(HasNTHeaders());
        NOTHROW;
        GC_NOTRIGGER;
        SUPPORTS_DAC;
        SO_TOLERANT;
    }
    CONTRACT_CHECK_END;

    // Fetch the NT header
    IMAGE_NT_HEADERS *pNT = FindNTHeaders();

    // OS will zero pad a mapped file up to file alignment size - some images rely on this
    // COUNT_T alignedSize = AlignUp(m_size, VAL32(pNT->OptionalHeader.FileAlignment));
    COUNT_T alignedSize = IsMapped() ? AlignUp(m_size, VAL32(pNT->OptionalHeader.FileAlignment)) : m_size;

    // Check to make sure that our memory is big enough to cover the stated range.
    // Note that this check is only required if we have a non-flat image.
    if (IsMapped())
        CHECK(alignedSize >= VAL32(pNT->OptionalHeader.SizeOfImage));

    // Check expected alignments
    CHECK(CheckAligned(addressStart, VAL32(pNT->OptionalHeader.SectionAlignment)));
    CHECK(CheckAligned(offsetStart, VAL32(pNT->OptionalHeader.FileAlignment)));
    CHECK(CheckAligned(offsetSize, VAL32(pNT->OptionalHeader.FileAlignment)));

    // addressSize is typically not aligned, so we align it for purposes of checks.
    COUNT_T alignedAddressSize = AlignUp(addressSize, VAL32(pNT->OptionalHeader.SectionAlignment));
    CHECK(addressSize <= alignedAddressSize);

    // Check overflow
    CHECK(CheckOverflow(addressStart, alignedAddressSize));
    CHECK(CheckOverflow(offsetStart, offsetSize));

    // Make sure we don't overlap the previous section
    CHECK(addressStart >= previousAddressEnd
          && (offsetSize == 0
              || offsetStart >= previousOffsetEnd));

    // Make sure we don't overrun the end of the mapped image
    CHECK(addressStart + alignedAddressSize <= VAL32(pNT->OptionalHeader.SizeOfImage));

    // Make sure we don't overrun the end of the file (only relevant if we're not mapped, otherwise
    // we don't know the file size, as it's not declared in the headers.)
    if (!IsMapped())
        CHECK(offsetStart + offsetSize <= alignedSize);

    // Make sure the data doesn't overrun the virtual address space
    CHECK(offsetSize <= alignedAddressSize);

    CHECK_OK;
}

CHECK PEDecoder::CheckDirectoryEntry(int entry, int forbiddenFlags, IsNullOK ok) const
{
    CONTRACT_CHECK
    {
        INSTANCE_CHECK;
        PRECONDITION(CheckNTHeaders());
        PRECONDITION(entry < IMAGE_NUMBEROF_DIRECTORY_ENTRIES);
        PRECONDITION(HasDirectoryEntry(entry));
        NOTHROW;
        GC_NOTRIGGER;
        SO_TOLERANT;
    }
    CONTRACT_CHECK_END;

    CHECK(CheckDirectory(GetDirectoryEntry(entry), forbiddenFlags, ok));

    CHECK_OK;
}

CHECK PEDecoder::CheckDirectory(IMAGE_DATA_DIRECTORY *pDir, int forbiddenFlags, IsNullOK ok) const
{
    CONTRACT_CHECK
    {
        INSTANCE_CHECK;
        PRECONDITION(CheckNTHeaders());
        PRECONDITION(CheckPointer(pDir));
        NOTHROW;
        GC_NOTRIGGER;
        SUPPORTS_DAC;
        SO_TOLERANT;
    }
    CONTRACT_CHECK_END;

    CHECK(CheckRva(VAL32(pDir->VirtualAddress), VAL32(pDir->Size), forbiddenFlags, ok));

    CHECK_OK;
}

CHECK PEDecoder::CheckRva(RVA rva, COUNT_T size, int forbiddenFlags, IsNullOK ok) const
{
    CONTRACT_CHECK
    {
        INSTANCE_CHECK;
        NOTHROW;
        GC_NOTRIGGER;
        SUPPORTS_DAC;
        SO_TOLERANT;
    }
    CONTRACT_CHECK_END;

    if (rva == 0)
    {
        CHECK_MSG(ok == NULL_OK, "Zero RVA illegal");
        CHECK(size == 0);
    }
    else
    {
        IMAGE_SECTION_HEADER *section = RvaToSection(rva);

        CHECK(section != NULL);

        CHECK(CheckBounds(VAL32(section->VirtualAddress),
                          // AlignUp((UINT)VAL32(section->Misc.VirtualSize), (UINT)VAL32(FindNTHeaders()->OptionalHeader.SectionAlignment)),
                          (UINT)VAL32(section->Misc.VirtualSize),
                          rva, size));
        if(!IsMapped())
        {
            CHECK(CheckBounds(VAL32(section->VirtualAddress), VAL32(section->SizeOfRawData), rva, size));
        }

        if (forbiddenFlags!=0)
            CHECK((section->Characteristics & VAL32(forbiddenFlags))==0);
    }

    CHECK_OK;
}

CHECK PEDecoder::CheckRva(RVA rva, IsNullOK ok) const
{
    CONTRACT_CHECK
    {
        INSTANCE_CHECK;
        NOTHROW;
        GC_NOTRIGGER;
        SUPPORTS_DAC;
        SO_TOLERANT;
    }
    CONTRACT_CHECK_END;

    if (rva == 0)
        CHECK_MSG(ok == NULL_OK, "Zero RVA illegal");
    else
        CHECK(RvaToSection(rva) != NULL);

    CHECK_OK;
}

CHECK PEDecoder::CheckOffset(COUNT_T fileOffset, COUNT_T size, IsNullOK ok) const
{
    CONTRACT_CHECK
    {
        INSTANCE_CHECK;
        PRECONDITION(CheckNTHeaders());
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACT_CHECK_END;

    if (fileOffset == 0)
    {
        CHECK_MSG(ok == NULL_OK, "zero fileOffset illegal");
        CHECK(size == 0);
    }
    else
    {
        IMAGE_SECTION_HEADER *section = OffsetToSection(fileOffset);

        CHECK(section != NULL);

        CHECK(CheckBounds(section->PointerToRawData, section->SizeOfRawData,
                          fileOffset, size));
    }

    CHECK_OK;
}

CHECK PEDecoder::CheckOffset(COUNT_T fileOffset, IsNullOK ok) const
{
    CONTRACT_CHECK
    {
        INSTANCE_CHECK;
        PRECONDITION(CheckNTHeaders());
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACT_CHECK_END;

    if (fileOffset == NULL)
        CHECK_MSG(ok == NULL_OK, "Null pointer illegal");
    else
    {
        CHECK(OffsetToSection(fileOffset) != NULL);
    }

    CHECK_OK;
}

CHECK PEDecoder::CheckData(const void *data, COUNT_T size, IsNullOK ok) const
{
    CONTRACT_CHECK
    {
        INSTANCE_CHECK;
        PRECONDITION(CheckNTHeaders());
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACT_CHECK_END;

    if (data == NULL)
    {
        CHECK_MSG(ok == NULL_OK, "NULL pointer illegal");
        CHECK(size == 0);
    }
    else
    {
        CHECK(CheckUnderflow(data, m_base));
        CHECK((UINT_PTR) (((BYTE *) data) - ((BYTE *) m_base)) <= COUNT_T_MAX);

        if (IsMapped())
            CHECK(CheckRva((COUNT_T) ((BYTE *) data - (BYTE *) m_base), size));
        else
            CHECK(CheckOffset((COUNT_T) ((BYTE *) data - (BYTE *) m_base), size));
    }

    CHECK_OK;
}

CHECK PEDecoder::CheckData(const void *data, IsNullOK ok) const
{
    CONTRACT_CHECK
    {
        INSTANCE_CHECK;
        PRECONDITION(CheckNTHeaders());
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACT_CHECK_END;

    if (data == NULL)
        CHECK_MSG(ok == NULL_OK, "Null pointer illegal");
    else
    {
        CHECK(CheckUnderflow(data, m_base));
        CHECK((UINT_PTR) (((BYTE *) data) - ((BYTE *) m_base)) <= COUNT_T_MAX);

        if (IsMapped())
            CHECK(CheckRva((COUNT_T) ((BYTE *) data - (BYTE *) m_base)));
        else
            CHECK(CheckOffset((COUNT_T) ((BYTE *) data - (BYTE *) m_base)));
    }

    CHECK_OK;
}

CHECK PEDecoder::CheckInternalAddress(SIZE_T address, IsNullOK ok) const
{
    CONTRACT_CHECK
    {
        INSTANCE_CHECK;
        PRECONDITION(CheckNTHeaders());
        NOTHROW;
        GC_NOTRIGGER;
        SO_TOLERANT;
    }
    CONTRACT_CHECK_END;

    if (address == 0)
        CHECK_MSG(ok == NULL_OK, "Zero RVA illegal");
    else
        CHECK(RvaToSection(InternalAddressToRva(address)) != NULL);

    CHECK_OK;
}

CHECK PEDecoder::CheckInternalAddress(SIZE_T address, COUNT_T size, IsNullOK ok) const
{
    CONTRACT_CHECK
    {
        INSTANCE_CHECK;
        PRECONDITION(CheckNTHeaders());
        NOTHROW;
        GC_NOTRIGGER;
        SO_TOLERANT;
    }
    CONTRACT_CHECK_END;

    if (address == 0)
    {
        CHECK_MSG(ok == NULL_OK, "Zero RVA illegal");
        CHECK(size == 0);
    }
    else
    {
        CHECK(CheckRva(InternalAddressToRva(address), size));
    }

    CHECK_OK;
}

RVA PEDecoder::InternalAddressToRva(SIZE_T address) const
{
    CONTRACT(RVA)
    {
        INSTANCE_CHECK;
        PRECONDITION(CheckNTHeaders());
        NOTHROW;
        GC_NOTRIGGER;
        POSTCONDITION(CheckRva(RETVAL));
        SO_TOLERANT;
    }
    CONTRACT_END;

    if (m_flags & FLAG_RELOCATED)
    {
        // Address has been fixed up
        RETURN (RVA) ((BYTE *) address - (BYTE *) m_base);
    }
    else
    {
        // Address has not been fixed up
        RETURN (RVA) (address - (SIZE_T) GetPreferredBase());
    }
}

// Returns a pointer to the named section or NULL if not found.
// The name should include the starting "." as well.
IMAGE_SECTION_HEADER *PEDecoder::FindSection(LPCSTR sectionName) const
{
    CONTRACT(IMAGE_SECTION_HEADER *)
    {
        INSTANCE_CHECK;
        PRECONDITION(CheckNTHeaders());
        PRECONDITION(sectionName != NULL);
        NOTHROW;
        GC_NOTRIGGER;
        CANNOT_TAKE_LOCK;
        SO_TOLERANT;
        POSTCONDITION(CheckPointer(RETVAL, NULL_OK));
    }
    CONTRACT_END;

    // Ensure that the section name length is valid
    SIZE_T iSectionNameLength = strlen(sectionName);
    if ((iSectionNameLength < 1) || (iSectionNameLength > IMAGE_SIZEOF_SHORT_NAME))
    {
        _ASSERTE(!"Invalid section name!");
        RETURN NULL;
    }

    // Get the start and ends of the sections
    PTR_IMAGE_SECTION_HEADER pSection = FindFirstSection(FindNTHeaders());
    _ASSERTE(pSection != NULL);
    PTR_IMAGE_SECTION_HEADER pSectionEnd = pSection + VAL16(FindNTHeaders()->FileHeader.NumberOfSections);
    _ASSERTE(pSectionEnd != NULL);

    BOOL fFoundSection = FALSE;

    // Loop thru the sections and see if we got the section we are interested in
    while (pSection < pSectionEnd)
    {
        // Is this the section we are looking for?
        if (strncmp(sectionName, (char*)pSection->Name, iSectionNameLength) == 0)
        {
            // We found our section - break out of the loop
            fFoundSection = TRUE;
            break;
        }

        // Move to the next section
        pSection++;
     }

    if (TRUE == fFoundSection)
        RETURN pSection;
    else
        RETURN NULL;
}

IMAGE_SECTION_HEADER *PEDecoder::RvaToSection(RVA rva) const
{
    CONTRACT(IMAGE_SECTION_HEADER *)
    {
        INSTANCE_CHECK;
        NOTHROW;
        GC_NOTRIGGER;
        CANNOT_TAKE_LOCK;
        POSTCONDITION(CheckPointer(RETVAL, NULL_OK));
        SUPPORTS_DAC;
        SO_TOLERANT;
    }
    CONTRACT_END;

    PTR_IMAGE_SECTION_HEADER section = dac_cast<PTR_IMAGE_SECTION_HEADER>(FindFirstSection(FindNTHeaders()));
    PTR_IMAGE_SECTION_HEADER sectionEnd = section + VAL16(FindNTHeaders()->FileHeader.NumberOfSections);

    while (section < sectionEnd)
    {
        if (rva < (VAL32(section->VirtualAddress)
                   + AlignUp((UINT)VAL32(section->Misc.VirtualSize), (UINT)VAL32(FindNTHeaders()->OptionalHeader.SectionAlignment))))
        {
            if (rva < VAL32(section->VirtualAddress))
                RETURN NULL;
            else
            {
                RETURN section;
            }
        }

        section++;
    }

    RETURN NULL;
}

IMAGE_SECTION_HEADER *PEDecoder::OffsetToSection(COUNT_T fileOffset) const
{
    CONTRACT(IMAGE_SECTION_HEADER *)
    {
        INSTANCE_CHECK;
        PRECONDITION(CheckNTHeaders());
        NOTHROW;
        GC_NOTRIGGER;
        POSTCONDITION(CheckPointer(RETVAL, NULL_OK));
        SUPPORTS_DAC;
        SO_TOLERANT;
    }
    CONTRACT_END;

    PTR_IMAGE_SECTION_HEADER section = dac_cast<PTR_IMAGE_SECTION_HEADER>(FindFirstSection(FindNTHeaders()));
    PTR_IMAGE_SECTION_HEADER sectionEnd = section + VAL16(FindNTHeaders()->FileHeader.NumberOfSections);

    while (section < sectionEnd)
    {
        if (fileOffset < section->PointerToRawData + section->SizeOfRawData)
        {
            if (fileOffset < section->PointerToRawData)
                RETURN NULL;
            else
                RETURN section;
        }

        section++;
    }

    RETURN NULL;
}

TADDR PEDecoder::GetRvaData(RVA rva, IsNullOK ok /*= NULL_NOT_OK*/) const
{
    CONTRACT(TADDR)
    {
        INSTANCE_CHECK;
        PRECONDITION(CheckNTHeaders());
        PRECONDITION(CheckRva(rva, NULL_OK));
        NOTHROW;
        GC_NOTRIGGER;
        SO_TOLERANT;
        CANNOT_TAKE_LOCK;
        SUPPORTS_DAC;
    }
    CONTRACT_END;

    if ((rva == 0)&&(ok == NULL_NOT_OK))
        RETURN NULL;

    RVA offset;
    if (IsMapped())
        offset = rva;
    else
    {
        // !!! check for case where rva is in padded portion of segment
        offset = RvaToOffset(rva);
    }

    RETURN( m_base + offset );
}

RVA PEDecoder::GetDataRva(const TADDR data) const
{
    CONTRACT(RVA)
    {
        INSTANCE_CHECK;
        PRECONDITION(CheckNTHeaders());
        PRECONDITION(CheckData((void *)data, NULL_OK));
        NOTHROW;
        GC_NOTRIGGER;
        SUPPORTS_DAC;
    }
    CONTRACT_END;

    if (data == NULL)
        RETURN 0;

    COUNT_T offset = (COUNT_T) (data - m_base);
    if (IsMapped())
        RETURN offset;
    else
        RETURN OffsetToRva(offset);
}

BOOL PEDecoder::PointerInPE(PTR_CVOID data) const
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        NOTHROW;
        GC_NOTRIGGER;
        FORBID_FAULT;
        SO_TOLERANT;
        SUPPORTS_DAC;
    }
    CONTRACTL_END;

    TADDR taddrData = dac_cast<TADDR>(data);
    TADDR taddrBase = dac_cast<TADDR>(m_base);

    if (this->IsMapped())
    {
        return taddrBase <= taddrData  && taddrData  < taddrBase + GetVirtualSize();
    }
    else
    {
        return taddrBase <= taddrData  && taddrData < taddrBase + GetSize();
    }
}

TADDR PEDecoder::GetOffsetData(COUNT_T fileOffset, IsNullOK ok /*= NULL_NOT_OK*/) const
{
    CONTRACT(TADDR)
    {
        INSTANCE_CHECK;
        PRECONDITION(CheckNTHeaders());
        PRECONDITION(CheckOffset(fileOffset, NULL_OK));
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACT_END;

    if ((fileOffset == 0)&&(ok == NULL_NOT_OK))
        RETURN NULL;

    RETURN GetRvaData(OffsetToRva(fileOffset));
}

//-------------------------------------------------------------------------------
// Lifted from "..\md\inc\mdfileformat.h"
// (cannot #include it here because it references lot of other stuff)
#define STORAGE_MAGIC_SIG   0x424A5342  // BSJB
struct STORAGESIGNATURE
{
    ULONG       lSignature;             // "Magic" signature.
    USHORT      iMajorVer;              // Major file version.
    USHORT      iMinorVer;              // Minor file version.
    ULONG       iExtraData;             // Offset to next structure of information
    ULONG       iVersionString;         // Length of version string
};
typedef STORAGESIGNATURE UNALIGNED * PSTORAGESIGNATURE;
typedef DPTR(STORAGESIGNATURE UNALIGNED) PTR_STORAGESIGNATURE;


struct STORAGEHEADER
{
    BYTE        fFlags;                 // STGHDR_xxx flags.
    BYTE        pad;
    USHORT      iStreams;               // How many streams are there.
};
typedef STORAGEHEADER UNALIGNED * PSTORAGEHEADER;
typedef DPTR(STORAGEHEADER UNALIGNED) PTR_STORAGEHEADER;


struct STORAGESTREAM
{
    ULONG       iOffset;                // Offset in file for this stream.
    ULONG       iSize;                  // Size of the file.
    char        rcName[32];  // Start of name, null terminated.
};
typedef STORAGESTREAM UNALIGNED * PSTORAGESTREAM;
typedef DPTR(STORAGESTREAM UNALIGNED) PTR_STORAGESTREAM;


// if the stream's name is shorter than 32 bytes (incl.zero terminator),
// the size of storage stream header is less than sizeof(STORAGESTREAM)
// and is padded to 4-byte alignment
inline PTR_STORAGESTREAM NextStorageStream(PTR_STORAGESTREAM pSS)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        SO_TOLERANT;
        CANNOT_TAKE_LOCK;
    }
    CONTRACTL_END;

    SUPPORTS_DAC;
    TADDR pc = dac_cast<TADDR>(pSS);
    pc += (sizeof(STORAGESTREAM) - 32 /*sizeof(STORAGESTREAM::rcName)*/ + strlen(pSS->rcName)+1+3)&~3;
    return PTR_STORAGESTREAM(pc);
}
//-------------------------------------------------------------------------------


CHECK PEDecoder::CheckCorHeader() const
{
    CONTRACT_CHECK
    {
        INSTANCE_CHECK;
        NOTHROW;
        GC_NOTRIGGER;
        SUPPORTS_DAC;
        SO_TOLERANT;
    }
    CONTRACT_CHECK_END;

    if (m_flags & FLAG_COR_CHECKED)
        CHECK_OK;

    CHECK(CheckNTHeaders());

    CHECK(HasCorHeader());

    IMAGE_DATA_DIRECTORY *pDir = GetDirectoryEntry(IMAGE_DIRECTORY_ENTRY_COMHEADER);

    CHECK(CheckDirectory(pDir, IMAGE_SCN_MEM_WRITE, NULL_NOT_OK));

    CHECK(VAL32(pDir->Size) >= sizeof(IMAGE_COR20_HEADER));

    IMAGE_SECTION_HEADER *section = RvaToSection(VAL32(pDir->VirtualAddress));
    CHECK(section != NULL);
    CHECK((section->Characteristics & VAL32(IMAGE_SCN_MEM_READ))!=0);

    CHECK(CheckRva(VAL32(pDir->VirtualAddress), sizeof(IMAGE_COR20_HEADER)));

    IMAGE_COR20_HEADER *pCor = GetCorHeader();

    //CHECK(((ULONGLONG)pCor & 0x3)==0);

    // If the file is COM+ 1.0, which by definition has nothing the runtime can
    // use, or if the file requires a newer version of this engine than us,
    // it cannot be run by this engine.
    CHECK(VAL16(pCor->MajorRuntimeVersion) > 1 && VAL16(pCor->MajorRuntimeVersion) <= COR_VERSION_MAJOR);

    CHECK(CheckDirectory(&pCor->MetaData, IMAGE_SCN_MEM_WRITE, HasNativeHeader() ? NULL_OK : NULL_NOT_OK));
    CHECK(CheckDirectory(&pCor->Resources, IMAGE_SCN_MEM_WRITE, NULL_OK));
    CHECK(CheckDirectory(&pCor->StrongNameSignature, IMAGE_SCN_MEM_WRITE, NULL_OK));
    CHECK(CheckDirectory(&pCor->CodeManagerTable, IMAGE_SCN_MEM_WRITE, NULL_OK));
    CHECK(CheckDirectory(&pCor->VTableFixups, 0, NULL_OK));
    CHECK(CheckDirectory(&pCor->ExportAddressTableJumps, 0, NULL_OK));
    CHECK(CheckDirectory(&pCor->ManagedNativeHeader, 0, NULL_OK));

    CHECK(VAL32(pCor->cb) >= offsetof(IMAGE_COR20_HEADER, ManagedNativeHeader) + sizeof(IMAGE_DATA_DIRECTORY));

    DWORD validBits = COMIMAGE_FLAGS_ILONLY
      | COMIMAGE_FLAGS_32BITREQUIRED
      | COMIMAGE_FLAGS_TRACKDEBUGDATA
      | COMIMAGE_FLAGS_STRONGNAMESIGNED
      | COMIMAGE_FLAGS_NATIVE_ENTRYPOINT
      | COMIMAGE_FLAGS_IL_LIBRARY
      | COMIMAGE_FLAGS_32BITPREFERRED;

    CHECK((pCor->Flags&VAL32(~validBits)) == 0);

    // Pure IL images should not have VTable fixups or EAT jumps
    if (IsILOnly())
    {
        CHECK(pCor->VTableFixups.Size == VAL32(0));
        CHECK(pCor->ExportAddressTableJumps.Size == VAL32(0));
        CHECK(!(pCor->Flags & VAL32(COMIMAGE_FLAGS_NATIVE_ENTRYPOINT)));
        //@TODO: If not an exe, check that EntryPointToken is mdNil
    }
    else
    {
        if (pCor->Flags & VAL32(COMIMAGE_FLAGS_NATIVE_ENTRYPOINT))
        {
            CHECK(CheckRva(VAL32(IMAGE_COR20_HEADER_FIELD(*pCor,EntryPointToken))));
        }
    }

    // Strong name signed images should have a signature
    if (IsStrongNameSigned())
        CHECK(HasStrongNameSignature());

    // IL library files (really a misnomer - these are native images) only
    // may have a native image header
    if ((pCor->Flags&VAL32(COMIMAGE_FLAGS_IL_LIBRARY)) == 0)
    {
        CHECK(VAL32(pCor->ManagedNativeHeader.Size) == 0);
    }

    // Metadata header checks
    IMAGE_DATA_DIRECTORY *pDirMD = &pCor->MetaData;
    COUNT_T ctMD = (COUNT_T)VAL32(pDirMD->Size);
    TADDR   pcMD = (TADDR)GetDirectoryData(pDirMD);

    if(pcMD != NULL)
    {
        // Storage signature checks
        CHECK(ctMD >= sizeof(STORAGESIGNATURE));
        PTR_STORAGESIGNATURE pStorageSig = PTR_STORAGESIGNATURE((TADDR)pcMD);
        COUNT_T ctMDStreamSize = ctMD;          // Store MetaData stream size for later usage


        CHECK(VAL32(pStorageSig->lSignature) == STORAGE_MAGIC_SIG);
        COUNT_T ctSSig;
        CHECK(ClrSafeInt<COUNT_T>::addition(sizeof(STORAGESIGNATURE), (COUNT_T)VAL32(pStorageSig->iVersionString), ctSSig));
        CHECK(ctMD > ctSSig);

        // Storage header checks
        pcMD += ctSSig;

        PTR_STORAGEHEADER pSHdr = PTR_STORAGEHEADER((TADDR)pcMD);


        ctMD -= ctSSig;
        CHECK(ctMD >= sizeof(STORAGEHEADER));
        pcMD = dac_cast<TADDR>(pSHdr) + sizeof(STORAGEHEADER);
        ctMD -= sizeof(STORAGEHEADER);
        WORD nStreams = VAL16(pSHdr->iStreams);

        // Storage streams checks (pcMD is a target pointer, so watch out)
        PTR_STORAGESTREAM pStr = PTR_STORAGESTREAM((TADDR)pcMD);
        PTR_STORAGESTREAM pSSOutOfRange =
            PTR_STORAGESTREAM((TADDR)(pcMD + ctMD));
        size_t namelen;
        WORD iStr;
        PTR_STORAGESTREAM pSS;
        for(iStr = 1, pSS = pStr; iStr <= nStreams; iStr++)
        {
            CHECK(pSS < pSSOutOfRange);
            CHECK(pSS + 1 <= pSSOutOfRange);

            for(namelen=0; (namelen<32)&&(pSS->rcName[namelen]!=0); namelen++);
            CHECK((0 < namelen)&&(namelen < 32));

            // Is it ngen image?
            if (!HasNativeHeader())
            {
                // Forbid HOT_MODEL_STREAM for non-ngen images
                CHECK(strcmp(pSS->rcName, HOT_MODEL_STREAM_A) != 0);
            }

            pcMD = dac_cast<TADDR>(NextStorageStream(pSS));
            ctMD -= (COUNT_T)(pcMD - dac_cast<TADDR>(pSS));

            pSS = PTR_STORAGESTREAM((TADDR)pcMD);
        }

        // At this moment, pcMD is pointing past the last stream header
        // and ctMD contains total size left for streams per se
        // Now, check the offsets and sizes of streams
        COUNT_T ctStreamsBegin = (COUNT_T)(pcMD - dac_cast<TADDR>(pStorageSig));  // min.possible offset
        COUNT_T  ctSS, ctSSbegin, ctSSend = 0;
        for(iStr = 1, pSS = pStr; iStr <= nStreams; iStr++,pSS = NextStorageStream(pSS))
        {
            ctSSbegin = (COUNT_T)VAL32(pSS->iOffset);
            CHECK(ctStreamsBegin <= ctSSbegin);
            CHECK(ctSSbegin < ctMDStreamSize);

            ctSS = (COUNT_T)VAL32(pSS->iSize);
            CHECK(ctMD >= ctSS);
            CHECK(ClrSafeInt<COUNT_T>::addition(ctSSbegin, ctSS, ctSSend));
            CHECK(ctSSend <= ctMDStreamSize);
            ctMD -= ctSS;

            // Check stream overlap
            PTR_STORAGESTREAM pSSprior;
            for(pSSprior=pStr; pSSprior < pSS; pSSprior = NextStorageStream(pSSprior))
            {
                COUNT_T ctSSprior_end = 0;
                CHECK(ClrSafeInt<COUNT_T>::addition((COUNT_T)VAL32(pSSprior->iOffset), (COUNT_T)VAL32(pSSprior->iSize), ctSSprior_end));
                CHECK((ctSSbegin >= ctSSprior_end)||(ctSSend <= (COUNT_T)VAL32(pSSprior->iOffset)));
            }
        }
    }  //end if(pcMD != NULL)

    const_cast<PEDecoder *>(this)->m_flags |= FLAG_COR_CHECKED;

    CHECK_OK;
}

#if !defined(FEATURE_CORECLR)
#define WHIDBEY_SP2_VERSION_MAJOR 2
#define WHIDBEY_SP2_VERSION_MINOR 0
#define WHIDBEY_SP2_VERSION_BUILD 50727
#define WHIDBEY_SP2_VERSION_PRIVATE_BUILD 3053
#endif // !defined(FEATURE_CORECLR)


// This function exists to provide compatibility between two different native image
// (NGEN) formats. In particular, the manifest metadata blob and the full metadata
// blob swapped locations from 3.5RTM to 3.5SP1. The logic here is to look at the
// runtime version embedded in the native image, to determine which format it is.
IMAGE_DATA_DIRECTORY *PEDecoder::GetMetaDataHelper(METADATA_SECTION_TYPE type) const
{
    CONTRACT(IMAGE_DATA_DIRECTORY *)
    {
        INSTANCE_CHECK;
        PRECONDITION(CheckCorHeader());
#ifdef FEATURE_PREJIT
        PRECONDITION(type == METADATA_SECTION_FULL || type == METADATA_SECTION_MANIFEST);
        PRECONDITION(type != METADATA_SECTION_MANIFEST || HasNativeHeader());
#else // FEATURE_PREJIT
        PRECONDITION(type == METADATA_SECTION_FULL);
#endif // FEATURE_PREJIT
        NOTHROW;
        GC_NOTRIGGER;
        SUPPORTS_DAC;
    }
    CONTRACT_END;

    IMAGE_DATA_DIRECTORY *pDirRet = &GetCorHeader()->MetaData;

#ifdef FEATURE_PREJIT
    // For backward compatibility reasons, we must be able to locate the metadata in all v2 native images as
    // well as current version of native images. This is needed by mdbg.exe for SxS debugging scenarios.
    // Specifically, mdbg.exe can be used to debug v2 managed applications, and need to be able to find
    // metadata in v2 native images. Therefore, the location of the data we need to locate the metadata must
    // never be moved. Here are some asserts to ensure that.
    // IMAGE_COR20_HEADER should be stable since it is defined in ECMA. Verify a coupld of fields we use:
    _ASSERTE(offsetof(IMAGE_COR20_HEADER, MetaData) == 8);
    _ASSERTE(offsetof(IMAGE_COR20_HEADER, ManagedNativeHeader) == 64);
    // We use a couple of fields in CORCOMPILE_HEADER.
    _ASSERTE(offsetof(CORCOMPILE_HEADER, VersionInfo) == 40);
    _ASSERTE(offsetof(CORCOMPILE_HEADER, ManifestMetaData) == 88);
    // And we use four version fields in CORCOMPILE_VERSION_INFO.
    _ASSERTE(offsetof(CORCOMPILE_VERSION_INFO, wVersionMajor) == 4);
    _ASSERTE(offsetof(CORCOMPILE_VERSION_INFO, wVersionMinor) == 6);
    _ASSERTE(offsetof(CORCOMPILE_VERSION_INFO, wVersionBuildNumber) == 8);
    _ASSERTE(offsetof(CORCOMPILE_VERSION_INFO, wVersionPrivateBuildNumber) == 10);

    // Visual Studio took dependency on crossgen /CreatePDB returning COR_E_NI_AND_RUNTIME_VERSION_MISMATCH
    // when crossgen and the native image come from different runtimes. In order to reach error path that returns
    // COR_E_NI_AND_RUNTIME_VERSION_MISMATCH in this case, size of CORCOMPILE_HEADER has to remain constant,
    // and the offset of PEKind and Machine fields inside CORCOMPILE_HEADER also have to remain constant, to pass earlier
    // checks that lead to different error codes. See Windows Phone Blue Bug #45406 for details.
    _ASSERTE(sizeof(CORCOMPILE_HEADER) == 160 + sizeof(TADDR));
    _ASSERTE(offsetof(CORCOMPILE_HEADER, PEKind) == 108 + sizeof(TADDR));
    _ASSERTE(offsetof(CORCOMPILE_HEADER, Machine) == 116 + sizeof(TADDR));

    // Handle NGEN format; otherwise, there is only one MetaData section in the
    // COR_HEADER and so the value of pDirRet is correct
    if (HasNativeHeader())
    {
#ifdef FEATURE_CORECLR

        if (type == METADATA_SECTION_MANIFEST)
            pDirRet = &GetNativeHeader()->ManifestMetaData;

#else // FEATURE_CORECLR

        IMAGE_DATA_DIRECTORY *pDirNativeHeader = &GetNativeHeader()->ManifestMetaData;

        // This code leverages the fact that pre-Whidbey SP2 private build numbers can never
        // be greater than Whidbey SP2 private build number, because otherwise major setup
        // issues would arise. To prevent this, it is standard to bump the private build
        // number up a significant amount for SPs, as was the case between Whidbey SP1 and
        // Whidbey SP2.
        // 
        // Since we could be reading an older version of native image, we tell
        // GetNativeVersionInfoMaybeNull to skip checking the native header.
        CORCOMPILE_VERSION_INFO *pVerInfo = GetNativeVersionInfoMaybeNull(true);
        bool fIsPreWhidbeySP2 = false;

        // If pVerInfo is NULL, we assume that we're in an NGEN compilation domain and that
        // the information has not yet been written. Since an NGEN compilation domain running
        // in the v4.0 runtime can only complie v4.0 native images, we'll assume the default
        // fIsPreWhidbeySP2 value (false) is correct.
        if (pVerInfo != NULL && pVerInfo->wVersionMajor <= WHIDBEY_SP2_VERSION_MAJOR)
        {
            if (pVerInfo->wVersionMajor < WHIDBEY_SP2_VERSION_MAJOR)
                fIsPreWhidbeySP2 = true;
            else if (pVerInfo->wVersionMajor == WHIDBEY_SP2_VERSION_MAJOR)
            {
                // If the sp2 minor version isn't 0, we need this logic:
                //     if (pVerInfo->wVersionMinor < WHIDBEY_SP2_VERSION_MINOR)
                //         fIsPreWhidbeySP2 = true;
                //     else
                // However, if it is zero, with that logic we get a warning about the comparison
                // of an unsigned variable to zero always being false.
                _ASSERTE(WHIDBEY_SP2_VERSION_MINOR == 0);

                if (pVerInfo->wVersionMinor == WHIDBEY_SP2_VERSION_MINOR)
                {
                    if (pVerInfo->wVersionBuildNumber < WHIDBEY_SP2_VERSION_BUILD)
                        fIsPreWhidbeySP2 = true;
                    else if (pVerInfo->wVersionBuildNumber == WHIDBEY_SP2_VERSION_BUILD)
                    {
                        if (pVerInfo->wVersionPrivateBuildNumber < WHIDBEY_SP2_VERSION_PRIVATE_BUILD)
                            fIsPreWhidbeySP2 = true;
                    }
                }
            }
        }

        // In pre-Whidbey SP2, pDirRet points to manifest and pDirNativeHeader points to full.
        if (fIsPreWhidbeySP2)
        {
            if (type == METADATA_SECTION_FULL)
                pDirRet = pDirNativeHeader;
        }
        // In Whidbey SP2 and later, pDirRet points to full and pDirNativeHeader points to manifest.
        else
        {
            if (type == METADATA_SECTION_MANIFEST)
                pDirRet = pDirNativeHeader;
        }

#endif // FEATURE_CORECLR

    }

#endif // FEATURE_PREJIT

    RETURN pDirRet;
}

PTR_CVOID PEDecoder::GetMetadata(COUNT_T *pSize) const
{
    CONTRACT(PTR_CVOID)
    {
        INSTANCE_CHECK;
        PRECONDITION(CheckCorHeader());
        PRECONDITION(CheckPointer(pSize, NULL_OK));
        NOTHROW;
        GC_NOTRIGGER;
        SUPPORTS_DAC;
    }
    CONTRACT_END;

    IMAGE_DATA_DIRECTORY *pDir = GetMetaDataHelper(METADATA_SECTION_FULL);

    if (pSize != NULL)
        *pSize = VAL32(pDir->Size);

    RETURN dac_cast<PTR_VOID>(GetDirectoryData(pDir));
}

const void *PEDecoder::GetResources(COUNT_T *pSize) const
{
    CONTRACT(const void *)
    {
        INSTANCE_CHECK;
        PRECONDITION(CheckCorHeader());
        PRECONDITION(CheckPointer(pSize, NULL_OK));
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACT_END;

    IMAGE_DATA_DIRECTORY *pDir = &GetCorHeader()->Resources;

    if (pSize != NULL)
        *pSize = VAL32(pDir->Size);

    RETURN (void *)GetDirectoryData(pDir);
}

CHECK PEDecoder::CheckResource(COUNT_T offset) const
{
    CONTRACT_CHECK
    {
        INSTANCE_CHECK;
        NOTHROW;
        GC_NOTRIGGER;
        PRECONDITION(CheckCorHeader());
    }
    CONTRACT_CHECK_END;

    IMAGE_DATA_DIRECTORY *pDir = &GetCorHeader()->Resources;

    CHECK(CheckOverflow(VAL32(pDir->VirtualAddress), offset));

    RVA rva = VAL32(pDir->VirtualAddress) + offset;

    // Make sure we have at least enough data for a length
    CHECK(CheckRva(rva, sizeof(DWORD)));

    // Make sure resource is within resource section
    CHECK(CheckBounds(VAL32(pDir->VirtualAddress), VAL32(pDir->Size),
                      rva + sizeof(DWORD), GET_UNALIGNED_VAL32((LPVOID)GetRvaData(rva))));

    CHECK_OK;
}

const void *PEDecoder::GetResource(COUNT_T offset, COUNT_T *pSize) const
{
    CONTRACT(const void *)
    {
        INSTANCE_CHECK;
        PRECONDITION(CheckCorHeader());
        PRECONDITION(CheckPointer(pSize, NULL_OK));
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACT_END;

    IMAGE_DATA_DIRECTORY *pDir = &GetCorHeader()->Resources;

    // 403571: Prefix complained correctly about need to always perform rva check
    if (CheckResource(offset) == FALSE)
        return NULL;

    void * resourceBlob = (void *)GetRvaData(VAL32(pDir->VirtualAddress) + offset);
    // Holds if CheckResource(offset) == TRUE
    PREFIX_ASSUME(resourceBlob != NULL);

     if (pSize != NULL)
        *pSize = GET_UNALIGNED_VAL32(resourceBlob);

    RETURN (const void *) ((BYTE*)resourceBlob+sizeof(DWORD));
}

BOOL PEDecoder::HasManagedEntryPoint() const
{
    CONTRACTL {
        INSTANCE_CHECK;
        PRECONDITION(CheckCorHeader());
        NOTHROW;
        GC_NOTRIGGER;
    } CONTRACTL_END;

    ULONG flags = GetCorHeader()->Flags;
    return (!(flags & VAL32(COMIMAGE_FLAGS_NATIVE_ENTRYPOINT)) &&
            (!IsNilToken(GetEntryPointToken())));
}

ULONG PEDecoder::GetEntryPointToken() const
{
    CONTRACT(ULONG)
    {
        INSTANCE_CHECK;
        PRECONDITION(CheckCorHeader());
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACT_END;

    RETURN VAL32(IMAGE_COR20_HEADER_FIELD(*GetCorHeader(), EntryPointToken));
}

IMAGE_COR_VTABLEFIXUP *PEDecoder::GetVTableFixups(COUNT_T *pCount) const
{
    CONTRACT(IMAGE_COR_VTABLEFIXUP *)
    {
        INSTANCE_CHECK;
        PRECONDITION(CheckCorHeader());
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACT_END;
    IMAGE_DATA_DIRECTORY *pDir = &GetCorHeader()->VTableFixups;

    if (pCount != NULL)
        *pCount = VAL32(pDir->Size)/sizeof(IMAGE_COR_VTABLEFIXUP);

    RETURN PTR_IMAGE_COR_VTABLEFIXUP(GetDirectoryData(pDir));
}

CHECK PEDecoder::CheckILOnly() const
{
    CONTRACT_CHECK
    {
        INSTANCE_CHECK;
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACT_CHECK_END;

    if (m_flags & FLAG_IL_ONLY_CHECKED)
        CHECK_OK;

    CHECK(CheckCorHeader());


    // Allow only verifiable directories.

    static int s_allowedBitmap =
        ((1 << (IMAGE_DIRECTORY_ENTRY_IMPORT   )) |
         (1 << (IMAGE_DIRECTORY_ENTRY_RESOURCE )) |
         (1 << (IMAGE_DIRECTORY_ENTRY_SECURITY )) |
         (1 << (IMAGE_DIRECTORY_ENTRY_BASERELOC)) |
         (1 << (IMAGE_DIRECTORY_ENTRY_DEBUG    )) |
         (1 << (IMAGE_DIRECTORY_ENTRY_IAT      )) |
         (1 << (IMAGE_DIRECTORY_ENTRY_COMHEADER)));




    for (UINT32 entry=0; entry<GetNumberOfRvaAndSizes(); ++entry)
    {
        if (Has32BitNTHeaders())
            CheckBounds(dac_cast<PTR_CVOID>(&GetNTHeaders32()->OptionalHeader),
                        GetNTHeaders32()->FileHeader.SizeOfOptionalHeader,
                        dac_cast<PTR_CVOID>(GetNTHeaders32()->OptionalHeader.DataDirectory + entry),
                        sizeof(IMAGE_DATA_DIRECTORY));
        else
            CheckBounds(dac_cast<PTR_CVOID>(&GetNTHeaders64()->OptionalHeader),
                        GetNTHeaders32()->FileHeader.SizeOfOptionalHeader,
                        dac_cast<PTR_CVOID>(GetNTHeaders64()->OptionalHeader.DataDirectory + entry),
                        sizeof(IMAGE_DATA_DIRECTORY));

        if (HasDirectoryEntry(entry))
        {
            CHECK((s_allowedBitmap & (1 << entry)) != 0);
            if (entry!=IMAGE_DIRECTORY_ENTRY_SECURITY)  //ignored by OS loader
                CHECK(CheckDirectoryEntry(entry,IMAGE_SCN_MEM_SHARED,NULL_NOT_OK));
        }
    }
    if (HasDirectoryEntry(IMAGE_DIRECTORY_ENTRY_IMPORT) ||
        HasDirectoryEntry(IMAGE_DIRECTORY_ENTRY_BASERELOC) ||
        FindNTHeaders()->OptionalHeader.AddressOfEntryPoint != 0)
    {
        // When the image is LoadLibrary'd, we whack the import, IAT directories and the entrypoint. We have to relax
        // the verification for mapped images. Ideally, we would only do it for a post-LoadLibrary image.
        if (!IsMapped() || (HasDirectoryEntry(IMAGE_DIRECTORY_ENTRY_IMPORT) || HasDirectoryEntry(IMAGE_DIRECTORY_ENTRY_BASERELOC)))
        {
            CHECK(CheckILOnlyImportDlls());
            CHECK(CheckILOnlyBaseRelocations());
        }

#ifdef _TARGET_X86_
        if (!IsMapped())
        {
            CHECK(CheckILOnlyEntryPoint());
        }
#endif
    }

    // Check some section characteristics
    IMAGE_NT_HEADERS *pNT = FindNTHeaders();
    IMAGE_SECTION_HEADER *section = FindFirstSection(pNT);
    IMAGE_SECTION_HEADER *sectionEnd = section + VAL16(pNT->FileHeader.NumberOfSections);
    while (section < sectionEnd)
    {
        // Don't allow shared sections for IL-only images
        CHECK(!(section->Characteristics & IMAGE_SCN_MEM_SHARED));

        // Be sure that we have some access to the section.  Note that this test assumes that
        //  execute or write permissions will also let us read the section.  If that is found to be an
        //  incorrect assumption, this will need to be modified.
        CHECK((section->Characteristics & (IMAGE_SCN_MEM_EXECUTE | IMAGE_SCN_MEM_READ | IMAGE_SCN_MEM_WRITE)) != 0);
        section++;
    }

    // For EXE, check that OptionalHeader.Win32VersionValue is zero.  When this value is non-zero, GetVersionEx
    //  returns PE supplied values, rather than native OS values; the runtime relies on knowing the actual
    //  OS version.
    if (!IsDll())
    {
        CHECK(GetWin32VersionValue() == 0);
    }


    const_cast<PEDecoder *>(this)->m_flags |= FLAG_IL_ONLY_CHECKED;

    CHECK_OK;
}


CHECK PEDecoder::CheckILOnlyImportDlls() const
{
    CONTRACT_CHECK
    {
        INSTANCE_CHECK;
        PRECONDITION(CheckNTHeaders());
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACT_CHECK_END;

    // The only allowed DLL Imports are MscorEE.dll:_CorExeMain,_CorDllMain

#ifdef _WIN64
    // On win64, when the image is LoadLibrary'd, we whack the import and IAT directories. We have to relax
    // the verification for mapped images. Ideally, we would only do it for a post-LoadLibrary image.
    if (IsMapped() && !HasDirectoryEntry(IMAGE_DIRECTORY_ENTRY_IMPORT))
        CHECK_OK;
#endif

    CHECK(HasDirectoryEntry(IMAGE_DIRECTORY_ENTRY_IMPORT));
    CHECK(CheckDirectoryEntry(IMAGE_DIRECTORY_ENTRY_IMPORT, IMAGE_SCN_MEM_WRITE));

    // Get the import directory entry
    PIMAGE_DATA_DIRECTORY pDirEntryImport = GetDirectoryEntry(IMAGE_DIRECTORY_ENTRY_IMPORT);
    CHECK(pDirEntryImport != NULL);
    PREFIX_ASSUME(pDirEntryImport != NULL);

    // There should be space for 2 entries. (mscoree and NULL)
    CHECK(VAL32(pDirEntryImport->Size) >= (2 * sizeof(IMAGE_IMPORT_DESCRIPTOR)));

    // Get the import data
    PIMAGE_IMPORT_DESCRIPTOR pID = (PIMAGE_IMPORT_DESCRIPTOR) GetDirectoryData(pDirEntryImport);
    CHECK(pID != NULL);
    PREFIX_ASSUME(pID != NULL);

    // Entry 0: ILT, Name, IAT must be be non-null.  Forwarder, DateTime should be NULL.
    CHECK( IMAGE_IMPORT_DESC_FIELD(pID[0], Characteristics) != 0
        && pID[0].TimeDateStamp == 0
        && (pID[0].ForwarderChain == 0 || pID[0].ForwarderChain == static_cast<ULONG>(-1))
        && pID[0].Name != 0
        && pID[0].FirstThunk != 0);

    // Entry 1: must be all nulls.
    CHECK( IMAGE_IMPORT_DESC_FIELD(pID[1], Characteristics) == 0
        && pID[1].TimeDateStamp == 0
        && pID[1].ForwarderChain == 0
        && pID[1].Name == 0
        && pID[1].FirstThunk == 0);

    // Ensure the RVA of the name plus its length is valid for this image
    UINT nameRVA = VAL32(pID[0].Name);
    CHECK(CheckRva(nameRVA, (COUNT_T) sizeof("mscoree.dll")));

    // Make sure the name is equal to mscoree
    CHECK(SString::_stricmp( (char *)GetRvaData(nameRVA), "mscoree.dll") == 0);

    // Check the Hint/Name table.
    CHECK(CheckILOnlyImportByNameTable(VAL32(IMAGE_IMPORT_DESC_FIELD(pID[0], OriginalFirstThunk))));

    // The IAT needs to be checked only for size.
    CHECK(CheckRva(VAL32(pID[0].FirstThunk), 2*sizeof(UINT32)));

    CHECK_OK;
}

CHECK PEDecoder::CheckILOnlyImportByNameTable(RVA rva) const
{
    CONTRACT_CHECK
    {
        INSTANCE_CHECK;
        PRECONDITION(CheckNTHeaders());
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACT_CHECK_END;

    // Check if we have enough space to hold 2 DWORDS
    CHECK(CheckRva(rva, 2*sizeof(UINT32)));

    UINT32 UNALIGNED *pImportArray = (UINT32 UNALIGNED *) GetRvaData(rva);

    CHECK(GET_UNALIGNED_VAL32(&pImportArray[0]) != 0);
    CHECK(GET_UNALIGNED_VAL32(&pImportArray[1]) == 0);

    UINT32 importRVA = GET_UNALIGNED_VAL32(&pImportArray[0]);

    // First bit Set implies Ordinal lookup
    CHECK((importRVA & 0x80000000) == 0);

#define DLL_NAME "_CorDllMain"
#define EXE_NAME "_CorExeMain"

    static_assert_no_msg(sizeof(DLL_NAME) == sizeof(EXE_NAME));

    // Check if we have enough space to hold 2 bytes +
    // _CorExeMain or _CorDllMain and a NULL char
    CHECK(CheckRva(importRVA, offsetof(IMAGE_IMPORT_BY_NAME, Name) + sizeof(DLL_NAME)));

    IMAGE_IMPORT_BY_NAME *import = (IMAGE_IMPORT_BY_NAME*) GetRvaData(importRVA);

    CHECK(SString::_stricmp((char *) import->Name, DLL_NAME) == 0 || _stricmp((char *) import->Name, EXE_NAME) == 0);

    CHECK_OK;
}

#ifdef _TARGET_X86_
// jmp dword ptr ds:[XXXX]
#define JMP_DWORD_PTR_DS_OPCODE { 0xFF, 0x25 }
#define JMP_DWORD_PTR_DS_OPCODE_SIZE   2        // Size of opcode
#define JMP_SIZE   6                            // Size of opcode + operand
#endif

CHECK PEDecoder::CheckILOnlyBaseRelocations() const
{
    CONTRACT_CHECK
    {
        INSTANCE_CHECK;
        PRECONDITION(CheckNTHeaders());
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACT_CHECK_END;

    if (!HasDirectoryEntry(IMAGE_DIRECTORY_ENTRY_BASERELOC))
    {
        // We require base relocs for dlls.
        CHECK(!IsDll());

        CHECK((FindNTHeaders()->FileHeader.Characteristics & VAL16(IMAGE_FILE_RELOCS_STRIPPED)) != 0);
    }
    else
    {
        CHECK((FindNTHeaders()->FileHeader.Characteristics & VAL16(IMAGE_FILE_RELOCS_STRIPPED)) == 0);

        CHECK(CheckDirectoryEntry(IMAGE_DIRECTORY_ENTRY_BASERELOC, IMAGE_SCN_MEM_WRITE));

        IMAGE_DATA_DIRECTORY *pRelocDir = GetDirectoryEntry(IMAGE_DIRECTORY_ENTRY_BASERELOC);

        IMAGE_SECTION_HEADER *section = RvaToSection(VAL32(pRelocDir->VirtualAddress));
        CHECK(section != NULL);
        CHECK((section->Characteristics & VAL32(IMAGE_SCN_MEM_READ))!=0);

        IMAGE_BASE_RELOCATION *pReloc = (IMAGE_BASE_RELOCATION *)
          GetRvaData(VAL32(pRelocDir->VirtualAddress));

        // 403569: PREfix correctly complained about pReloc being possibly NULL
        CHECK(pReloc != NULL);
        CHECK(VAL32(pReloc->SizeOfBlock) == VAL32(pRelocDir->Size));

        UINT16 *pRelocEntry = (UINT16 *) (pReloc + 1);
        UINT16 *pRelocEntryEnd = (UINT16 *) ((BYTE *) pReloc + VAL32(pReloc->SizeOfBlock));
        if(FindNTHeaders()->FileHeader.Machine == VAL16(IMAGE_FILE_MACHINE_IA64))
        {
            // Exactly 2 Reloc records, both IMAGE_REL_BASED_DIR64
            CHECK(VAL32(pReloc->SizeOfBlock) >= (sizeof(IMAGE_BASE_RELOCATION)+2*sizeof(UINT16)));
            CHECK((VAL16(pRelocEntry[0]) & 0xF000) == (IMAGE_REL_BASED_DIR64 << 12));
            pRelocEntry++;
            CHECK((VAL16(pRelocEntry[0]) & 0xF000) == (IMAGE_REL_BASED_DIR64 << 12));
        }
        else
        {
            // Only one Reloc record is expected
            CHECK(VAL32(pReloc->SizeOfBlock) >= (sizeof(IMAGE_BASE_RELOCATION)+sizeof(UINT16)));
            if(FindNTHeaders()->FileHeader.Machine == VAL16(IMAGE_FILE_MACHINE_AMD64))
                CHECK((VAL16(pRelocEntry[0]) & 0xF000) == (IMAGE_REL_BASED_DIR64 << 12));
            else
                CHECK((VAL16(pRelocEntry[0]) & 0xF000) == (IMAGE_REL_BASED_HIGHLOW << 12));
        }

        while (++pRelocEntry < pRelocEntryEnd)
        {
            // NULL padding entries are allowed
            CHECK((VAL16(pRelocEntry[0]) & 0xF000) == IMAGE_REL_BASED_ABSOLUTE);
        }
    }

    CHECK_OK;
}

#ifdef _TARGET_X86_
CHECK PEDecoder::CheckILOnlyEntryPoint() const
{
    CONTRACT_CHECK
    {
        INSTANCE_CHECK;
        PRECONDITION(CheckNTHeaders());
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACT_CHECK_END;

    CHECK(FindNTHeaders()->OptionalHeader.AddressOfEntryPoint != 0);

    if(FindNTHeaders()->FileHeader.Machine == VAL16(IMAGE_FILE_MACHINE_I386))
    {
        // EntryPoint should be a jmp dword ptr ds:[XXXX] instruction.
        // XXXX should be RVA of the first and only entry in the IAT.

        CHECK(CheckRva(VAL32(FindNTHeaders()->OptionalHeader.AddressOfEntryPoint), JMP_SIZE));

        BYTE *stub = (BYTE *) GetRvaData(VAL32(FindNTHeaders()->OptionalHeader.AddressOfEntryPoint));

        static const BYTE s_DllOrExeMain[] = JMP_DWORD_PTR_DS_OPCODE;

        // 403570: prefix complained about stub being possibly NULL.
        // Unsure here. PREFIX_ASSUME might be also correct as indices are
        // verified in the above CHECK statement.
        CHECK(stub != NULL);
        CHECK(memcmp(stub, s_DllOrExeMain, JMP_DWORD_PTR_DS_OPCODE_SIZE) == 0);

        // Verify target of jump - it should be first entry in the IAT.

        PIMAGE_IMPORT_DESCRIPTOR pID =
            (PIMAGE_IMPORT_DESCRIPTOR) GetDirectoryEntryData(IMAGE_DIRECTORY_ENTRY_IMPORT);

        UINT32 va = * (UINT32 *) (stub + JMP_DWORD_PTR_DS_OPCODE_SIZE);

        CHECK(VAL32(pID[0].FirstThunk) == (va - (SIZE_T) GetPreferredBase()));
    }

    CHECK_OK;
}
#endif // _TARGET_X86_

#ifndef DACCESS_COMPILE

void PEDecoder::LayoutILOnly(void *base, BOOL allowFullPE) const
{
    CONTRACT_VOID
    {
        INSTANCE_CHECK;
        PRECONDITION(allowFullPE || CheckILOnlyFormat());
        PRECONDITION(CheckZeroedMemory(base, VAL32(FindNTHeaders()->OptionalHeader.SizeOfImage)));
        // Ideally we would require the layout address to honor the section alignment constraints.
        // However, we do have 8K aligned IL only images which we load on 32 bit platforms. In this
        // case, we can only guarantee OS page alignment (which after all, is good enough.)
        PRECONDITION(CheckAligned((SIZE_T)base, OS_PAGE_SIZE));
        THROWS;
        GC_NOTRIGGER;
    }
    CONTRACT_END;

    // We're going to copy everything first, and write protect what we need to later.

    // First, copy headers
    CopyMemory(base, (void *)m_base, VAL32(FindNTHeaders()->OptionalHeader.SizeOfHeaders));

    // Now, copy all sections to appropriate virtual address

    IMAGE_SECTION_HEADER *sectionStart = IMAGE_FIRST_SECTION(FindNTHeaders());
    IMAGE_SECTION_HEADER *sectionEnd = sectionStart + VAL16(FindNTHeaders()->FileHeader.NumberOfSections);

    IMAGE_SECTION_HEADER *section = sectionStart;
    while (section < sectionEnd)
    {
        // Raw data may be less than section size if tail is zero, but may be more since VirtualSize is
        // not padded.
        DWORD size = min(VAL32(section->SizeOfRawData), VAL32(section->Misc.VirtualSize));

        CopyMemory((BYTE *) base + VAL32(section->VirtualAddress), (BYTE *) m_base + VAL32(section->PointerToRawData), size);

        // Note that our memory is zeroed already, so no need to initialize any tail.

        section++;
    }

    // Apply write protection to copied headers
    DWORD oldProtection;
    if (!ClrVirtualProtect((void *) base, VAL32(FindNTHeaders()->OptionalHeader.SizeOfHeaders),
                           PAGE_READONLY, &oldProtection))
        ThrowLastError();

    // Finally, apply proper protection to copied sections
    section = sectionStart;
    while (section < sectionEnd)
    {
        // Add appropriate page protection.
        if ((section->Characteristics & VAL32(IMAGE_SCN_MEM_WRITE)) == 0)
        {
            if (!ClrVirtualProtect((void *) ((BYTE *)base + VAL32(section->VirtualAddress)),
                                   VAL32(section->Misc.VirtualSize),
                                   PAGE_READONLY, &oldProtection))
                ThrowLastError();
        }

        section++;
    }

    RETURN;
}

#endif // #ifndef DACCESS_COMPILE

#ifndef FEATURE_PAL
void * PEDecoder::GetWin32Resource(LPCWSTR lpName, LPCWSTR lpType, COUNT_T *pSize /*=NULL*/) const
{
    CONTRACTL {
        INSTANCE_CHECK;
        PRECONDITION(IsMapped());
        NOTHROW;
        GC_NOTRIGGER;
    } CONTRACTL_END;

    if (pSize != NULL)
        *pSize = 0;

    HMODULE hModule = (HMODULE) dac_cast<TADDR>(GetBase());

    // Use the Win32 functions to decode the resources

    HRSRC hResource = WszFindResourceEx(hModule, lpType, lpName, MAKELANGID(LANG_NEUTRAL, SUBLANG_NEUTRAL));
    if (!hResource)
        return NULL;

    HGLOBAL hLoadedResource = ::LoadResource(hModule, hResource);
    if (!hLoadedResource)
        return NULL;

    PVOID pResource = ::LockResource(hLoadedResource);
    if (!pResource)
        return NULL;

    if (pSize != NULL)
        *pSize = ::SizeofResource(hModule, hResource);

    return pResource;
}
#endif // FEATURE_PAL

BOOL PEDecoder::HasNativeHeader() const
{
    CONTRACT(BOOL)
    {
        INSTANCE_CHECK;
        NOTHROW;
        GC_NOTRIGGER;
        SUPPORTS_DAC;
        SO_TOLERANT;
    }
    CONTRACT_END;

#ifdef FEATURE_PREJIT
    // Pretend that ready-to-run images do not have native header
    RETURN (((GetCorHeader()->Flags & VAL32(COMIMAGE_FLAGS_IL_LIBRARY)) != 0) && !HasReadyToRunHeader());
#else
    RETURN FALSE;
#endif
}

CHECK PEDecoder::CheckNativeHeader() const
{
    CONTRACT_CHECK
    {
        NOTHROW;
        GC_NOTRIGGER;
        SUPPORTS_DAC;
        SO_TOLERANT;
    }
    CONTRACT_CHECK_END;

#ifdef FEATURE_PREJIT
    if (m_flags & FLAG_NATIVE_CHECKED)
        CHECK_OK;

    CHECK(CheckCorHeader());

    CHECK(HasNativeHeader());

    IMAGE_DATA_DIRECTORY *pDir = &GetCorHeader()->ManagedNativeHeader;

    CHECK(CheckDirectory(pDir));
    CHECK(VAL32(pDir->Size) == sizeof(CORCOMPILE_HEADER));

#if 0
    // We want to be sure to not trigger these checks when loading a native
    // image in a retail build

    // And we do not want to trigger these checks in debug builds either to avoid debug/retail behavior
    // differences.

    PTR_CORCOMPILE_HEADER pHeader = PTR_CORCOMPILE_HEADER((TADDR)GetDirectoryData(pDir));

    CHECK(CheckDirectory(&pHeader->EEInfoTable));
    CHECK(pHeader->EEInfoTable.Size == sizeof(CORCOMPILE_EE_INFO_TABLE));

    CHECK(CheckDirectory(&pHeader->HelperTable, 0, NULL_OK));
    // @todo: verify helper table size

    CHECK(CheckDirectory(&pHeader->ImportSections, 0, NULL_OK));
    // @todo verify import sections

    CHECK(CheckDirectory(&pHeader->ImportTable, 0, NULL_OK));
    // @todo verify import table

    CHECK(CheckDirectory(&pHeader->VersionInfo, 0, NULL_OK)); // no version header for precompiled netmodules
    CHECK(pHeader->VersionInfo.Size == 0
          || (pHeader->VersionInfo.Size == sizeof(CORCOMPILE_VERSION_INFO) &&
              // Sanity check that we are not just pointing to zeroed-out memory
              ((CORCOMPILE_VERSION_INFO*)PTR_READ(GetDirectoryData(&pHeader->VersionInfo), sizeof(CORCOMPILE_VERSION_INFO)))->wOSMajorVersion != 0));

    CHECK(CheckDirectory(&pHeader->Dependencies, 0, NULL_OK)); // no version header for precompiled netmodules
    CHECK(pHeader->Dependencies.Size % sizeof(CORCOMPILE_DEPENDENCY) == 0);

    CHECK(CheckDirectory(&pHeader->DebugMap, 0, NULL_OK));
    CHECK(pHeader->DebugMap.Size % sizeof(CORCOMPILE_DEBUG_RID_ENTRY) == 0);

    // GetPersistedModuleImage()
    CHECK(CheckDirectory(&pHeader->ModuleImage));
    CHECK(pHeader->ModuleImage.Size > 0); // sizeof(Module) if we knew it here

    CHECK(CheckDirectory(&pHeader->CodeManagerTable));
    CHECK(pHeader->CodeManagerTable.Size == sizeof(CORCOMPILE_CODE_MANAGER_ENTRY));

    PTR_CORCOMPILE_CODE_MANAGER_ENTRY pEntry = PTR_CORCOMPILE_CODE_MANAGER_ENTRY((TADDR)GetDirectoryData(&pHeader->CodeManagerTable));
    CHECK(CheckDirectory(&pEntry->HotCode, IMAGE_SCN_MEM_WRITE, NULL_OK));
    CHECK(CheckDirectory(&pEntry->Code, IMAGE_SCN_MEM_WRITE, NULL_OK));
    CHECK(CheckDirectory(&pEntry->ColdCode, IMAGE_SCN_MEM_WRITE, NULL_OK));

    CHECK(CheckDirectory(&pHeader->ProfileDataList, 0, NULL_OK));
    CHECK(pHeader->ProfileDataList.Size >= sizeof(CORCOMPILE_METHOD_PROFILE_LIST)
          || pHeader->ProfileDataList.Size == 0);

#endif

    const_cast<PEDecoder *>(this)->m_flags |= FLAG_NATIVE_CHECKED;

#else // FEATURE_PREJIT
    CHECK(false);
#endif // FEATURE_PREJIT

    CHECK_OK;
}

READYTORUN_HEADER * PEDecoder::FindReadyToRunHeader() const
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        SUPPORTS_DAC;
        SO_TOLERANT;
    }
    CONTRACTL_END;

    IMAGE_DATA_DIRECTORY *pDir = &GetCorHeader()->ManagedNativeHeader;

    if (VAL32(pDir->Size) >= sizeof(READYTORUN_HEADER) && CheckDirectory(pDir))
    {
        PTR_READYTORUN_HEADER pHeader = PTR_READYTORUN_HEADER((TADDR)GetDirectoryData(pDir));
        if (pHeader->Signature == READYTORUN_SIGNATURE)
        {
            const_cast<PEDecoder *>(this)->m_pReadyToRunHeader = pHeader;
            return pHeader;
        }
    }

    const_cast<PEDecoder *>(this)->m_flags |= FLAG_HAS_NO_READYTORUN_HEADER;
    return NULL;
}

//
// code:PEDecoder::CheckILMethod and code:PEDecoder::ComputeILMethodSize really belong to
// file:..\inc\corhlpr.cpp. Unfortunately, corhlpr.cpp is public header file that cannot be
// properly DACized and have other dependencies on the rest of the CLR.
//

typedef DPTR(COR_ILMETHOD_TINY) PTR_COR_ILMETHOD_TINY;
typedef DPTR(COR_ILMETHOD_FAT) PTR_COR_ILMETHOD_FAT;
typedef DPTR(COR_ILMETHOD_SECT_SMALL) PTR_COR_ILMETHOD_SECT_SMALL;
typedef DPTR(COR_ILMETHOD_SECT_FAT) PTR_COR_ILMETHOD_SECT_FAT;

CHECK PEDecoder::CheckILMethod(RVA rva)
{
    CONTRACT_CHECK
    {
        INSTANCE_CHECK;
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACT_CHECK_END;

    //
    // Incrementaly validate that the entire IL method body is within the bounds of the image
    //

    // We need to have at least the tiny header
    CHECK(CheckRva(rva, sizeof(IMAGE_COR_ILMETHOD_TINY)));

    TADDR pIL = GetRvaData(rva);

    PTR_COR_ILMETHOD_TINY pMethodTiny = PTR_COR_ILMETHOD_TINY(pIL);

    if (pMethodTiny->IsTiny())
    {
        // Tiny header has no optional sections - we are done.
        CHECK(CheckRva(rva, sizeof(IMAGE_COR_ILMETHOD_TINY) + pMethodTiny->GetCodeSize()));
        CHECK_OK;
    }

    //
    // Fat header
    //

    CHECK(CheckRva(rva, sizeof(IMAGE_COR_ILMETHOD_FAT)));

    PTR_COR_ILMETHOD_FAT pMethodFat = PTR_COR_ILMETHOD_FAT(pIL);

    CHECK(pMethodFat->IsFat());

    S_UINT32 codeEnd = S_UINT32(4) * S_UINT32(pMethodFat->GetSize()) + S_UINT32(pMethodFat->GetCodeSize());
    CHECK(!codeEnd.IsOverflow());

    // Check minimal size of the header
    CHECK(pMethodFat->GetSize() >= (sizeof(COR_ILMETHOD_FAT) / 4));

    CHECK(CheckRva(rva, codeEnd.Value()));

    if (!pMethodFat->More())
    {
        CHECK_OK;
    }

    // DACized copy of code:COR_ILMETHOD_FAT::GetSect
    TADDR pSect = AlignUp(pIL + codeEnd.Value(), 4);

    //
    // Optional sections following the code
    //

    for (;;)
    {
        CHECK(CheckRva(rva, UINT32(pSect - pIL) + sizeof(IMAGE_COR_ILMETHOD_SECT_SMALL)));

        PTR_COR_ILMETHOD_SECT_SMALL pSectSmall = PTR_COR_ILMETHOD_SECT_SMALL(pSect);

        UINT32 sectSize;

        if (pSectSmall->IsSmall())
        {
            sectSize = pSectSmall->DataSize;

            // Workaround for bug in shipped compilers - see comment in code:COR_ILMETHOD_SECT::DataSize
            if ((pSectSmall->Kind & CorILMethod_Sect_KindMask) == CorILMethod_Sect_EHTable)
                sectSize = COR_ILMETHOD_SECT_EH_SMALL::Size(sectSize / sizeof(IMAGE_COR_ILMETHOD_SECT_EH_CLAUSE_SMALL));
        }
        else
        {
            CHECK(CheckRva(rva, UINT32(pSect - pIL) + sizeof(IMAGE_COR_ILMETHOD_SECT_FAT)));

            PTR_COR_ILMETHOD_SECT_FAT pSectFat = PTR_COR_ILMETHOD_SECT_FAT(pSect);

            sectSize = pSectFat->GetDataSize();

            // Workaround for bug in shipped compilers - see comment in code:COR_ILMETHOD_SECT::DataSize
            if ((pSectSmall->Kind & CorILMethod_Sect_KindMask) == CorILMethod_Sect_EHTable)
                sectSize = COR_ILMETHOD_SECT_EH_FAT::Size(sectSize / sizeof(IMAGE_COR_ILMETHOD_SECT_EH_CLAUSE_FAT));
        }

        // Section has to be non-empty to avoid infinite loop below
        CHECK(sectSize > 0);

        S_UINT32 sectEnd = S_UINT32(UINT32(pSect - pIL)) + S_UINT32(sectSize);
        CHECK(!sectEnd.IsOverflow());

        CHECK(CheckRva(rva, sectEnd.Value()));

        if (!pSectSmall->More())
        {
            CHECK_OK;
        }

        // DACized copy of code:COR_ILMETHOD_FAT::Next
        pSect = AlignUp(pIL + sectEnd.Value(), 4);
    }
}

//
// Compute size of IL blob. Assumes that the IL is within the bounds of the image - make sure
// to call code:PEDecoder::CheckILMethod before calling this method.
//
// code:PEDecoder::ComputeILMethodSize is DACized duplicate of code:COR_ILMETHOD_DECODER::GetOnDiskSize.
// code:MethodDesc::GetILHeader contains debug-only check that ensures that both implementations
// are in sync.
//

SIZE_T PEDecoder::ComputeILMethodSize(TADDR pIL)
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
        SUPPORTS_DAC;
    } CONTRACTL_END;

    //
    // Mirror flow of code:PEDecoder::CheckILMethod, except for the range checks
    //

    PTR_COR_ILMETHOD_TINY pMethodTiny = PTR_COR_ILMETHOD_TINY(pIL);

    if (pMethodTiny->IsTiny())
    {
        return sizeof(IMAGE_COR_ILMETHOD_TINY) + pMethodTiny->GetCodeSize();
    }

    PTR_COR_ILMETHOD_FAT pMethodFat = PTR_COR_ILMETHOD_FAT(pIL);

    UINT32 codeEnd = 4 * pMethodFat->GetSize() + pMethodFat->GetCodeSize();

    if (!pMethodFat->More())
    {
        return codeEnd;
    }

    // DACized copy of code:COR_ILMETHOD_FAT::GetSect
    TADDR pSect = AlignUp(pIL + codeEnd, 4);

    for (;;)
    {
        PTR_COR_ILMETHOD_SECT_SMALL pSectSmall = PTR_COR_ILMETHOD_SECT_SMALL(pSect);

        UINT32 sectSize;

        if (pSectSmall->IsSmall())
        {
            sectSize = pSectSmall->DataSize;

            // Workaround for bug in shipped compilers - see comment in code:COR_ILMETHOD_SECT::DataSize
            if ((pSectSmall->Kind & CorILMethod_Sect_KindMask) == CorILMethod_Sect_EHTable)
                sectSize = COR_ILMETHOD_SECT_EH_SMALL::Size(sectSize / sizeof(IMAGE_COR_ILMETHOD_SECT_EH_CLAUSE_SMALL));
        }
        else
        {
            PTR_COR_ILMETHOD_SECT_FAT pSectFat = PTR_COR_ILMETHOD_SECT_FAT(pSect);

            sectSize = pSectFat->GetDataSize();

            // Workaround for bug in shipped compilers - see comment in code:COR_ILMETHOD_SECT::DataSize
            if ((pSectSmall->Kind & CorILMethod_Sect_KindMask) == CorILMethod_Sect_EHTable)
                sectSize = COR_ILMETHOD_SECT_EH_FAT::Size(sectSize / sizeof(IMAGE_COR_ILMETHOD_SECT_EH_CLAUSE_FAT));
        }

        UINT32 sectEnd = UINT32(pSect - pIL) + sectSize;

        if (!pSectSmall->More() || (sectSize == 0))
        {
            return sectEnd;
        }

        // DACized copy of code:COR_ILMETHOD_FAT::Next
        pSect = AlignUp(pIL + sectEnd, 4);
    }
}

//
// GetDebugDirectoryEntry - return the debug directory entry at the specified index
//
// Arguments:
//   index    The 0-based index of the entry to return.  Usually this is just 0,
//            but there can be multiple debug directory entries in a PE file.
//
// Return value:
//   A pointer to the IMAGE_DEBUG_DIRECTORY in the PE file for the specified index,
//   or NULL if it doesn't exist.
//
// Note that callers on untrusted input are required to validate the debug directory
// first by calling CheckDirectoryEntry(IMAGE_DIRECTORY_ENTRY_DEBUG) (possibly
// indirectly via one of the CheckILOnly* functions).
//
PTR_IMAGE_DEBUG_DIRECTORY PEDecoder::GetDebugDirectoryEntry(UINT index) const
{
    CONTRACT(PTR_IMAGE_DEBUG_DIRECTORY)
    {
        INSTANCE_CHECK;
        PRECONDITION(CheckNTHeaders());
        NOTHROW;
        GC_NOTRIGGER;
        SUPPORTS_DAC;
    }
    CONTRACT_END;

    if (!HasDirectoryEntry(IMAGE_DIRECTORY_ENTRY_DEBUG))
    {
        RETURN NULL;
    }

    // Get a pointer to the contents and size of the debug directory
    // Also validates (in CHK builds) that this is all within one section, which the
    // caller should have already validated if they don't trust the context of this PE file.
    COUNT_T cbDebugDir;
    TADDR taDebugDir = GetDirectoryEntryData(IMAGE_DIRECTORY_ENTRY_DEBUG, &cbDebugDir);

    // Check if the specified directory entry exists (based on the size of the directory)
    // Note that the directory size should be an even multiple of the entry size, but we
    // just round-down because we need to be resiliant (without asserting) to corrupted /
    // fuzzed PE files.
    UINT cNumEntries = cbDebugDir / sizeof(IMAGE_DEBUG_DIRECTORY);
    if (index >= cNumEntries)
    {
        RETURN NULL;    // index out of range
    }

    // Get the debug directory entry at the specified index.
    PTR_IMAGE_DEBUG_DIRECTORY pDebugEntry = dac_cast<PTR_IMAGE_DEBUG_DIRECTORY>(taDebugDir);
    pDebugEntry += index;   // offset from the first entry to the requested entry
    RETURN pDebugEntry;
}


#ifdef FEATURE_PREJIT

CORCOMPILE_EE_INFO_TABLE *PEDecoder::GetNativeEEInfoTable() const
{
    CONTRACT(CORCOMPILE_EE_INFO_TABLE *)
    {
        PRECONDITION(CheckNativeHeader());
        NOTHROW;
        GC_NOTRIGGER;
        POSTCONDITION(CheckPointer(RETVAL));
    }
    CONTRACT_END;

    IMAGE_DATA_DIRECTORY *pDir = &GetNativeHeader()->EEInfoTable;

    // 403523: PREFIX correctly complained here. Fixed GetDirectoryData.
    RETURN PTR_CORCOMPILE_EE_INFO_TABLE(GetDirectoryData(pDir));
}


void *PEDecoder::GetNativeHelperTable(COUNT_T *pSize) const
{
    CONTRACT(void *)
    {
        PRECONDITION(CheckNativeHeader());
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACT_END;

    IMAGE_DATA_DIRECTORY *pDir = &GetNativeHeader()->HelperTable;

    if (pSize != NULL)
        *pSize = VAL32(pDir->Size);

    RETURN (void *)GetDirectoryData(pDir);
}

CORCOMPILE_VERSION_INFO *PEDecoder::GetNativeVersionInfoMaybeNull(bool skipCheckNativeHeader) const
{
    CONTRACT(CORCOMPILE_VERSION_INFO *)
    {
        PRECONDITION(skipCheckNativeHeader || CheckNativeHeader());
        POSTCONDITION(CheckPointer(RETVAL, NULL_OK));
        NOTHROW;
        GC_NOTRIGGER;
        SUPPORTS_DAC;
    }
    CONTRACT_END;

    IMAGE_DATA_DIRECTORY *pDir = &GetNativeHeader()->VersionInfo;

    RETURN PTR_CORCOMPILE_VERSION_INFO(GetDirectoryData(pDir));
}

CORCOMPILE_VERSION_INFO *PEDecoder::GetNativeVersionInfo() const
{
    CONTRACT(CORCOMPILE_VERSION_INFO *)
    {
        POSTCONDITION(CheckPointer(RETVAL));
        NOTHROW;
        GC_NOTRIGGER;
        SUPPORTS_DAC;
    }
    CONTRACT_END;

    RETURN GetNativeVersionInfoMaybeNull();
}

BOOL PEDecoder::HasNativeDebugMap() const
{
    CONTRACT(BOOL)
    {
        PRECONDITION(CheckNativeHeader());
        INSTANCE_CHECK;
        NOTHROW;
        GC_NOTRIGGER;
        SUPPORTS_DAC;
    }
    CONTRACT_END;

    // 403522: Prefix complained correctly here.
    CORCOMPILE_HEADER *pNativeHeader = GetNativeHeader();
    if (!pNativeHeader)
        RETURN FALSE;
    else
        RETURN (VAL32(pNativeHeader->DebugMap.VirtualAddress) != 0);
}

TADDR PEDecoder::GetNativeDebugMap(COUNT_T *pSize) const
{
    CONTRACT(TADDR)
    {
        PRECONDITION(CheckNativeHeader());
        PRECONDITION(CheckPointer(pSize, NULL_OK));
        NOTHROW;
        GC_NOTRIGGER;
        SUPPORTS_DAC;
    }
    CONTRACT_END;

    IMAGE_DATA_DIRECTORY *pDir = &GetNativeHeader()->DebugMap;

    if (pSize != NULL)
        *pSize = VAL32(pDir->Size);

    RETURN (GetDirectoryData(pDir));
}

Module *PEDecoder::GetPersistedModuleImage(COUNT_T *pSize) const
{
    CONTRACT(Module *)
    {
        PRECONDITION(CheckNativeHeader());
        PRECONDITION(CheckPointer(pSize, NULL_OK));
        POSTCONDITION(CheckPointer(RETVAL));
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACT_END;

    IMAGE_DATA_DIRECTORY *pDir = &GetNativeHeader()->ModuleImage;

    if (pSize != NULL)
        *pSize = VAL32(pDir->Size);

    RETURN (Module *) GetDirectoryData(pDir);
}

CHECK PEDecoder::CheckNativeHeaderVersion() const
{
    CONTRACT_CHECK
    {
        PRECONDITION(CheckNativeHeader());
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACT_CHECK_END;

    IMAGE_DATA_DIRECTORY *pDir = &GetCorHeader()->ManagedNativeHeader;
    CHECK(VAL32(pDir->Size) == sizeof(CORCOMPILE_HEADER));

    CORCOMPILE_HEADER *pNativeHeader = GetNativeHeader();
    CHECK(pNativeHeader->Signature == CORCOMPILE_SIGNATURE);
    CHECK(pNativeHeader->MajorVersion == CORCOMPILE_MAJOR_VERSION);
    CHECK(pNativeHeader->MinorVersion == CORCOMPILE_MINOR_VERSION);

    CHECK_OK;
}

CORCOMPILE_CODE_MANAGER_ENTRY *PEDecoder::GetNativeCodeManagerTable() const
{
    CONTRACT(CORCOMPILE_CODE_MANAGER_ENTRY *)
    {
        PRECONDITION(CheckNativeHeader());
        POSTCONDITION(CheckPointer(RETVAL));
        SUPPORTS_DAC;
        NOTHROW;
        GC_NOTRIGGER;
        SO_TOLERANT;
    }
    CONTRACT_END;

    IMAGE_DATA_DIRECTORY *pDir = &GetNativeHeader()->CodeManagerTable;

    RETURN PTR_CORCOMPILE_CODE_MANAGER_ENTRY(GetDirectoryData(pDir));
}

PCODE PEDecoder::GetNativeHotCode(COUNT_T * pSize) const
{
    CONTRACT(PCODE)
    {
        PRECONDITION(CheckNativeHeader());
        PRECONDITION(CheckPointer(pSize, NULL_OK));
        SUPPORTS_DAC;
        NOTHROW;
        GC_NOTRIGGER;
        SO_TOLERANT;
    }
    CONTRACT_END;

    IMAGE_DATA_DIRECTORY *pDir = &GetNativeCodeManagerTable()->HotCode;

    if (pSize != NULL)
        *pSize = VAL32(pDir->Size);

    RETURN GetDirectoryData(pDir);
}

PCODE PEDecoder::GetNativeCode(COUNT_T * pSize) const
{
    CONTRACT(PCODE)
    {
        PRECONDITION(CheckNativeHeader());
        PRECONDITION(CheckPointer(pSize, NULL_OK));
        SUPPORTS_DAC;
        NOTHROW;
        GC_NOTRIGGER;
        SO_TOLERANT;
    }
    CONTRACT_END;

    IMAGE_DATA_DIRECTORY *pDir = &GetNativeCodeManagerTable()->Code;

    if (pSize != NULL)
        *pSize = VAL32(pDir->Size);

    RETURN GetDirectoryData(pDir);
}

PCODE PEDecoder::GetNativeColdCode(COUNT_T * pSize) const
{
    CONTRACT(PCODE)
    {
        PRECONDITION(CheckNativeHeader());
        PRECONDITION(CheckPointer(pSize, NULL_OK));
        NOTHROW;
        GC_NOTRIGGER;
        SUPPORTS_DAC;
    }
    CONTRACT_END;

    IMAGE_DATA_DIRECTORY *pDir = &GetNativeCodeManagerTable()->ColdCode;

    if (pSize != NULL)
        *pSize = VAL32(pDir->Size);

    RETURN GetDirectoryData(pDir);
}


CORCOMPILE_METHOD_PROFILE_LIST *PEDecoder::GetNativeProfileDataList(COUNT_T * pSize) const
{
    CONTRACT(CORCOMPILE_METHOD_PROFILE_LIST *)
    {
        PRECONDITION(CheckNativeHeader());
        PRECONDITION(CheckPointer(pSize, NULL_OK));
        POSTCONDITION(CheckPointer(RETVAL, NULL_OK));
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACT_END;

    IMAGE_DATA_DIRECTORY *pDir = &GetNativeHeader()->ProfileDataList;

    if (pSize != NULL)
        *pSize = VAL32(pDir->Size);

    RETURN PTR_CORCOMPILE_METHOD_PROFILE_LIST(GetDirectoryData(pDir));
}



PTR_CVOID PEDecoder::GetNativeManifestMetadata(COUNT_T *pSize) const
{
    CONTRACT(PTR_CVOID)
    {
        INSTANCE_CHECK;
        PRECONDITION(CheckNativeHeader());
        POSTCONDITION(CheckPointer(RETVAL, NULL_OK)); // TBD - may not store metadata for IJW
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACT_END;

    IMAGE_DATA_DIRECTORY *pDir = GetMetaDataHelper(METADATA_SECTION_MANIFEST);

    if (pSize != NULL)
        *pSize = VAL32(pDir->Size);

    RETURN dac_cast<PTR_VOID>(GetDirectoryData(pDir));
}

CHECK PEDecoder::CheckNativeImportFromIndex(COUNT_T index) const
{
    CONTRACT_CHECK
    {
        PRECONDITION(CheckNativeHeader());
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACT_CHECK_END;

    CHECK_MSG(index >= 0 && index < GetNativeImportTableCount(), "Bad Native Import Index");

    CHECK_OK;
}

COUNT_T PEDecoder::GetNativeImportTableCount() const
{
    CONTRACT(COUNT_T)
    {
        PRECONDITION(CheckNativeHeader());
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACT_END;

    IMAGE_DATA_DIRECTORY *pDir = &GetNativeHeader()->ImportTable;

    RETURN (VAL32(pDir->Size) / sizeof(CORCOMPILE_IMPORT_TABLE_ENTRY));
}

CORCOMPILE_IMPORT_TABLE_ENTRY *PEDecoder::GetNativeImportFromIndex(COUNT_T index) const
{
    CONTRACT(CORCOMPILE_IMPORT_TABLE_ENTRY *)
    {
        PRECONDITION(CheckNativeHeader());
        PRECONDITION(CheckNativeImportFromIndex(index));
        POSTCONDITION(CheckPointer(RETVAL));
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACT_END;

    IMAGE_DATA_DIRECTORY *pDir = &GetNativeHeader()->ImportTable;

    CORCOMPILE_IMPORT_TABLE_ENTRY *pEntry
      = (CORCOMPILE_IMPORT_TABLE_ENTRY *) GetDirectoryData(pDir);

    RETURN pEntry + index;
}

PTR_CORCOMPILE_IMPORT_SECTION PEDecoder::GetNativeImportSections(COUNT_T *pCount) const
{
    CONTRACT(PTR_CORCOMPILE_IMPORT_SECTION)
    {
        PRECONDITION(CheckNativeHeader());
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACT_END;

    IMAGE_DATA_DIRECTORY *pDir = &GetNativeHeader()->ImportSections;

    if (pCount != NULL)
        *pCount = VAL32(pDir->Size) / sizeof(CORCOMPILE_IMPORT_SECTION);

    RETURN dac_cast<PTR_CORCOMPILE_IMPORT_SECTION>(GetDirectoryData(pDir));
}

PTR_CORCOMPILE_IMPORT_SECTION PEDecoder::GetNativeImportSectionFromIndex(COUNT_T index) const
{
    CONTRACT(PTR_CORCOMPILE_IMPORT_SECTION)
    {
        PRECONDITION(CheckNativeHeader());
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACT_END;

    IMAGE_DATA_DIRECTORY *pDir = &GetNativeHeader()->ImportSections;

    _ASSERTE(VAL32(pDir->Size) % sizeof(CORCOMPILE_IMPORT_SECTION) == 0);
    _ASSERTE(index * sizeof(CORCOMPILE_IMPORT_SECTION) < VAL32(pDir->Size));

    RETURN dac_cast<PTR_CORCOMPILE_IMPORT_SECTION>(GetDirectoryData(pDir)) + index;
}

PTR_CORCOMPILE_IMPORT_SECTION PEDecoder::GetNativeImportSectionForRVA(RVA rva) const
{
    CONTRACT(PTR_CORCOMPILE_IMPORT_SECTION)
    {
        PRECONDITION(CheckNativeHeader());
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACT_END;

    IMAGE_DATA_DIRECTORY *pDir = &GetNativeHeader()->ImportSections;

    _ASSERTE(VAL32(pDir->Size) % sizeof(CORCOMPILE_IMPORT_SECTION) == 0);

    PTR_CORCOMPILE_IMPORT_SECTION pSections = dac_cast<PTR_CORCOMPILE_IMPORT_SECTION>(GetDirectoryData(pDir));
    PTR_CORCOMPILE_IMPORT_SECTION pEnd = dac_cast<PTR_CORCOMPILE_IMPORT_SECTION>(dac_cast<TADDR>(pSections) + VAL32(pDir->Size));

    for (PTR_CORCOMPILE_IMPORT_SECTION pSection = pSections; pSection < pEnd; pSection++)
    {
        if (rva >= VAL32(pSection->Section.VirtualAddress) && rva < VAL32(pSection->Section.VirtualAddress) + VAL32(pSection->Section.Size))
            RETURN pSection;
    }

    RETURN NULL;
}

TADDR PEDecoder::GetStubsTable(COUNT_T *pSize) const
{
    CONTRACTL {
        INSTANCE_CHECK;
        PRECONDITION(CheckNativeHeader());
        NOTHROW;
        GC_NOTRIGGER;
    } CONTRACTL_END;

    IMAGE_DATA_DIRECTORY *pDir = &GetNativeHeader()->StubsData;

    if (pSize != NULL)
        *pSize = VAL32(pDir->Size);

    return (GetDirectoryData(pDir));
}

TADDR PEDecoder::GetVirtualSectionsTable(COUNT_T *pSize) const
{
    CONTRACTL {
        INSTANCE_CHECK;
        PRECONDITION(CheckNativeHeader());
        NOTHROW;
        GC_NOTRIGGER;
    } CONTRACTL_END;

    IMAGE_DATA_DIRECTORY *pDir = &GetNativeHeader()->VirtualSectionsTable;

    if (pSize != NULL)
        *pSize = VAL32(pDir->Size);

    return (GetDirectoryData(pDir));
}

#endif // FEATURE_PREJIT

// Get the SizeOfStackReserve and SizeOfStackCommit from the PE file that was used to create
// the calling process (.exe file).
void PEDecoder::GetEXEStackSizes(SIZE_T *PE_SizeOfStackReserve, SIZE_T *PE_SizeOfStackCommit) const
{
    CONTRACTL {
        PRECONDITION(!IsDll()); // This routine should only be called for EXE files.
        NOTHROW;
        GC_NOTRIGGER;
    } CONTRACTL_END;

    * PE_SizeOfStackReserve = GetSizeOfStackReserve();
    * PE_SizeOfStackCommit  = GetSizeOfStackCommit();
}

CHECK PEDecoder::CheckWillCreateGuardPage() const
{
    CONTRACT_CHECK
    {
        PRECONDITION(CheckNTHeaders());
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACT_CHECK_END;

    if (!IsDll())
    {
        SIZE_T sizeReservedStack = 0;
        SIZE_T sizeCommitedStack = 0;

        GetEXEStackSizes(&sizeReservedStack, &sizeCommitedStack);

        CHECK(ThreadWillCreateGuardPage(sizeReservedStack, sizeCommitedStack));

    }

    CHECK_OK;
}

BOOL PEDecoder::HasNativeEntryPoint() const
{
    CONTRACTL {
        INSTANCE_CHECK;
        NOTHROW;
        GC_NOTRIGGER;
        PRECONDITION(CheckCorHeader());
    } CONTRACTL_END;

    ULONG flags = GetCorHeader()->Flags;
    return ((flags & VAL32(COMIMAGE_FLAGS_NATIVE_ENTRYPOINT)) &&
            (IMAGE_COR20_HEADER_FIELD(*GetCorHeader(), EntryPointToken) != VAL32(0)));
}

void *PEDecoder::GetNativeEntryPoint() const
{
    CONTRACT (void *) {
        INSTANCE_CHECK;
        NOTHROW;
        GC_NOTRIGGER;
        PRECONDITION(CheckCorHeader());
        PRECONDITION(HasNativeEntryPoint());
        POSTCONDITION(CheckPointer(RETVAL, NULL_OK));
    } CONTRACT_END;

    RETURN ((void *) GetRvaData((RVA)VAL32(IMAGE_COR20_HEADER_FIELD(*GetCorHeader(), EntryPointToken))));
}

#ifdef DACCESS_COMPILE

void
PEDecoder::EnumMemoryRegions(CLRDataEnumMemoryFlags flags,
                             bool enumThis)
{
    SUPPORTS_DAC;
    if (enumThis)
    {
        DAC_ENUM_DTHIS();
    }

    DacEnumMemoryRegion((TADDR)m_base, sizeof(IMAGE_DOS_HEADER));
    m_pNTHeaders.EnumMem();
    m_pCorHeader.EnumMem();
    m_pNativeHeader.EnumMem();
    m_pReadyToRunHeader.EnumMem();

    if (HasNTHeaders())
    {
        // resource file does not have NT Header.
        //
        // we also need to write out section header.
        DacEnumMemoryRegion(dac_cast<TADDR>(FindFirstSection()), sizeof(IMAGE_SECTION_HEADER) * GetNumberOfSections());
    }
}

#endif // #ifdef DACCESS_COMPILE

// --------------------------------------------------------------------------------

#ifdef _DEBUG

// This is a stress mode to force DLLs to be relocated.
// This is particularly useful for hardbinding of ngen images as we
// embed pointers into other hardbound ngen dependencies.

BOOL PEDecoder::GetForceRelocs()
{
    WRAPPER_NO_CONTRACT;

    static ConfigDWORD forceRelocs;
    return (forceRelocs.val(CLRConfig::INTERNAL_ForceRelocs) != 0);
}

BOOL PEDecoder::ForceRelocForDLL(LPCWSTR lpFileName)
{
    // Use static contracts to avoid recursion, as the dynamic contracts
    // do WszLoadLibrary(MSCOREE_SHIM_W).
#ifdef _DEBUG
		STATIC_CONTRACT_NOTHROW;                                        \
		ANNOTATION_DEBUG_ONLY;                                          \
		STATIC_CONTRACT_CANNOT_TAKE_LOCK;                               \
		ANNOTATION_FN_SO_NOT_MAINLINE;
#endif

#if defined(DACCESS_COMPILE) || defined(FEATURE_PAL)
    return TRUE;
#else

    CONTRACT_VIOLATION(SOToleranceViolation);

    // Contracts in ConfigDWORD do WszLoadLibrary(MSCOREE_SHIM_W).
    // This check prevents recursion.
    if (wcsstr(lpFileName, MSCOREE_SHIM_W) != 0)
        return TRUE;

    if (!GetForceRelocs())
        return TRUE;

    BOOL fSuccess = FALSE;
    PBYTE hndle = NULL;
    PEDecoder pe;
    void* pPreferredBase;
    COUNT_T nVirtualSize;

    HANDLE hFile = WszCreateFile(lpFileName,
                                 GENERIC_READ,
                                 FILE_SHARE_READ,
                                 NULL,
                                 OPEN_EXISTING,
                                 FILE_FLAG_SEQUENTIAL_SCAN,
                                 NULL);
    if (hFile == INVALID_HANDLE_VALUE)
        goto ErrExit;

    HANDLE hMap = WszCreateFileMapping(hFile,
                                       NULL,
                                       SEC_IMAGE | PAGE_READONLY,
                                       0,
                                       0,
                                       NULL);
    CloseHandle(hFile);

    if (hMap == NULL)
        goto ErrExit;

    hndle = (PBYTE)MapViewOfFile(hMap,
                                       FILE_MAP_READ,
                                       0,
                                       0,
                                       0);
    CloseHandle(hMap);

    if (!hndle)
        goto ErrExit;

    pe.Init(hndle);

    pPreferredBase = (void*)pe.GetPreferredBase();
    nVirtualSize = pe.GetVirtualSize();

    UnmapViewOfFile(hndle);
    hndle = NULL;

    // Reserve the space so nobody can use it. A potential bug is likely to
    // result in a plain AV this way. It is not a good idea to use the original
    // mapping for the reservation since since it would lock the file on the disk.
    if (!ClrVirtualAlloc(pPreferredBase, nVirtualSize, MEM_RESERVE, PAGE_NOACCESS))
        goto ErrExit;

    fSuccess = TRUE;

ErrExit:
    if (hndle != NULL)
        UnmapViewOfFile(hndle);

    return fSuccess;

#endif // DACCESS_COMPILE || FEATURE_PAL
}

#endif // _DEBUG

//
//  MethodSectionIterator class is used to iterate hot (or) cold method section in an ngen image.
//  Also used to iterate over jitted methods in the code heap
//
MethodSectionIterator::MethodSectionIterator(const void *code, SIZE_T codeSize,
                                             const void *codeTable, SIZE_T codeTableSize)
{
    //For DAC builds,we'll read the table one DWORD at a time.  Note that m_code IS
    //NOT a host pointer.
    m_codeTableStart = PTR_DWORD(TADDR(codeTable));
    m_codeTable = m_codeTableStart;
    _ASSERTE((codeTableSize % sizeof(DWORD)) == 0);
    m_codeTableEnd = m_codeTableStart + (codeTableSize / sizeof(DWORD));
    m_code = (BYTE *) code;
    m_current = NULL;


    if (m_codeTable < m_codeTableEnd)
    {
        m_dword = *m_codeTable++;
        m_index = 0;
    }
    else
    {
        m_index = NIBBLES_PER_DWORD;
    }
}

BOOL MethodSectionIterator::Next()
{
    while (m_codeTable < m_codeTableEnd || m_index < (int)NIBBLES_PER_DWORD)
    {
        while (m_index++ < (int)NIBBLES_PER_DWORD)
        {
            int nibble = (m_dword & HIGHEST_NIBBLE_MASK)>>HIGHEST_NIBBLE_BIT;
            m_dword <<= NIBBLE_SIZE;

            if (nibble != 0)
            {
                // We have found a method start
                m_current = m_code + ((nibble-1)*CODE_ALIGN);
                m_code += BYTES_PER_BUCKET;
                return TRUE;
            }

            m_code += BYTES_PER_BUCKET;
        }

        if (m_codeTable < m_codeTableEnd)
        {
            m_dword = *m_codeTable++;
            m_index = 0;
        }
    }
    return FALSE;
}
