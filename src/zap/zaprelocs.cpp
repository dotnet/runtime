// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//
// ZapRelocs.cpp
//

//
// Zapping of relocations
// 
// ======================================================================================

#include "common.h"

#include "zaprelocs.h"

#ifdef REDHAWK
void PDB_NoticeReloc(ZapRelocationType type, DWORD rvaReloc, ZapNode * pTarget, int targetOffset);
#endif

void ZapBaseRelocs::WriteReloc(PVOID pSrc, int offset, ZapNode * pTarget, int targetOffset, ZapRelocationType type)
{
    _ASSERTE(pTarget != NULL);

    PBYTE pLocation = (PBYTE)pSrc + offset;
    DWORD rva = m_pImage->GetCurrentRVA() + offset;
    TADDR pActualTarget = (TADDR)m_pImage->GetBaseAddress() + pTarget->GetRVA() + targetOffset;

#ifdef REDHAWK
    PDB_NoticeReloc(type, rva, pTarget, targetOffset);
#endif

    switch (type)
    {
    case IMAGE_REL_BASED_ABSOLUTE:
        *(UNALIGNED DWORD *)pLocation = pTarget->GetRVA() + targetOffset;
        // IMAGE_REL_BASED_ABSOLUTE does not need base reloc entry
        return;

    case IMAGE_REL_BASED_ABSOLUTE_TAGGED:
        _ASSERTE(targetOffset == 0);
        *(UNALIGNED DWORD *)pLocation = (DWORD)CORCOMPILE_TAG_TOKEN(pTarget->GetRVA());
        // IMAGE_REL_BASED_ABSOLUTE_TAGGED does not need base reloc entry
        return;

    case IMAGE_REL_BASED_PTR:
#ifdef _TARGET_ARM_
        // Misaligned relocs disable ASLR on ARM. We should never ever emit them.
        _ASSERTE(IS_ALIGNED(rva, sizeof(TADDR)));
#endif
        *(UNALIGNED TADDR *)pLocation = pActualTarget;
        break;

    case IMAGE_REL_BASED_RELPTR:
        {
            TADDR pSite = (TADDR)m_pImage->GetBaseAddress() + rva;
            *(UNALIGNED TADDR *)pLocation = (INT32)(pActualTarget - pSite);
        }
        // neither IMAGE_REL_BASED_RELPTR nor IMAGE_REL_BASED_MD_METHODENTRY need base reloc entry
        return;

    case IMAGE_REL_BASED_RELPTR32:
        {
            TADDR pSite = (TADDR)m_pImage->GetBaseAddress() + rva;
            *(UNALIGNED INT32 *)pLocation = (INT32)(pActualTarget - pSite);
        }
        // IMAGE_REL_BASED_RELPTR32 does not need base reloc entry
        return;

#if defined(_TARGET_X86_) || defined(_TARGET_AMD64_)
    case IMAGE_REL_BASED_REL32:
        {
            TADDR pSite = (TADDR)m_pImage->GetBaseAddress() + rva;
            *(UNALIGNED INT32 *)pLocation = (INT32)(pActualTarget - (pSite + sizeof(INT32)));
        }
        // IMAGE_REL_BASED_REL32 does not need base reloc entry
        return;
#endif // _TARGET_X86_ || _TARGET_AMD64_

#if defined(_TARGET_ARM_)
    case IMAGE_REL_BASED_THUMB_MOV32:
        {
            PutThumb2Mov32((UINT16 *)pLocation, (UINT32)pActualTarget);
            break;
        }

    case IMAGE_REL_BASED_THUMB_BRANCH24:
        {
            TADDR pSite = (TADDR)m_pImage->GetBaseAddress() + rva;

            // Kind of a workaround: make this reloc work both for calls (which have the thumb bit set),
            // and for relative jumps used for hot/cold splitting (which don't).
            pActualTarget &= ~THUMB_CODE;

            // Calculate the reloffset without the ThumbBit set so that it can be correctly encoded.
            _ASSERTE(!(pActualTarget & THUMB_CODE));// we expect pActualTarget not to have the thumb bit set
            _ASSERTE(!(pSite & THUMB_CODE));        // we expect pSite not to have the thumb bit set
            INT32 relOffset = (INT32)(pActualTarget - (pSite + sizeof(INT32)));
            if (!FitsInThumb2BlRel24(relOffset))
            {
                // Retry the compilation with IMAGE_REL_BASED_THUMB_BRANCH24 relocations disabled
                // (See code:ZapInfo::getRelocTypeHint)
                ThrowHR(COR_E_OVERFLOW);
            }
            PutThumb2BlRel24((UINT16 *)pLocation, relOffset);
        }
        // IMAGE_REL_BASED_THUMB_BRANCH24 does not need base reloc entry
        return;
#endif
#if defined(_TARGET_ARM64_)
    case IMAGE_REL_ARM64_BRANCH26:
        {
            TADDR pSite = (TADDR)m_pImage->GetBaseAddress() + rva;

            INT32 relOffset = (INT32)(pActualTarget - pSite);
            if (!FitsInRel28(relOffset))
            {
                ThrowHR(COR_E_OVERFLOW);
            }
            PutArm64Rel28((UINT32 *)pLocation,relOffset);
        }
        return;

    case IMAGE_REL_ARM64_PAGEBASE_REL21:
        {
            TADDR pSitePage = ((TADDR)m_pImage->GetBaseAddress() + rva) & 0xFFFFFFFFFFFFF000LL;
            TADDR pActualTargetPage = pActualTarget & 0xFFFFFFFFFFFFF000LL;

            INT64 relPage = (INT64)(pActualTargetPage - pSitePage);
            INT32 imm21 = (INT32)(relPage >> 12) & 0x1FFFFF;
            PutArm64Rel21((UINT32 *)pLocation, imm21);
        }
        return;

    case IMAGE_REL_ARM64_PAGEOFFSET_12A:
        {
            INT32 imm12 = (INT32)(pActualTarget & 0xFFFLL);
            PutArm64Rel12((UINT32 *)pLocation, imm12);
        }
        return;
#endif

    default:
        _ASSERTE(!"Unknown relocation type");
        break;
    }

    DWORD page = AlignDown(rva, RELOCATION_PAGE_SIZE);

    if (page != m_page)
    {
        FlushWriter();

        m_page = page;
        m_pageIndex = m_SerializedRelocs.GetCount();

        // Reserve space for IMAGE_BASE_RELOCATION
        for (size_t iSpace = 0; iSpace < sizeof(IMAGE_BASE_RELOCATION) / sizeof(USHORT); iSpace++)
            m_SerializedRelocs.Append(0);
    }

    m_SerializedRelocs.Append((USHORT)(AlignmentTrim(rva, RELOCATION_PAGE_SIZE) | (type << 12)));
}

void ZapBaseRelocs::FlushWriter()
{
    if (m_page != 0)
    {
        // The blocks has to be 4-byte aligned
        if (m_SerializedRelocs.GetCount() & 1)
            m_SerializedRelocs.Append(0);

        IMAGE_BASE_RELOCATION * pBaseRelocation = (IMAGE_BASE_RELOCATION *)&(m_SerializedRelocs[m_pageIndex]);
        pBaseRelocation->VirtualAddress = m_page;
        pBaseRelocation->SizeOfBlock = (m_SerializedRelocs.GetCount() - m_pageIndex) * sizeof(USHORT);

        m_page = 0;
    }
}

void ZapBaseRelocs::Save(ZapWriter * pZapWriter)
{
    FlushWriter();

    pZapWriter->SetWritingRelocs();

    // Write the relocs as blob
    pZapWriter->Write(&m_SerializedRelocs[0], m_SerializedRelocs.GetCount() * sizeof(USHORT));
}

//////////////////////////////////////////////////////////////////////////////
//
// ZapBlobWithRelocs
//

int _cdecl CmpZapRelocs(const void *p1, const void *p2)
{
    LIMITED_METHOD_CONTRACT;

    const ZapReloc *relocTemp1 = (ZapReloc *)p1;
    const ZapReloc *relocTemp2 = (ZapReloc *)p2;
    if (relocTemp1->m_offset < relocTemp2->m_offset)
        return -1;
    else if (relocTemp1->m_offset > relocTemp2->m_offset)
        return 1;
    else
        return 0;
}

void ZapBlobWithRelocs::Save(ZapWriter * pZapWriter)
{
    if (m_pRelocs != NULL)
    {

        // pre-pass to figure out if we need to sort
        // if the offsets are not in ascending order AND the offsets within this
        // array ending up describing locations in different pages, the relocation
        // writer generates bad relocation info (e.g. multiple entries for the same page)
        // that is no longer accepted by the OS loader
        // Also, having relocs in ascending order allows a more compact representation.

        ZapReloc *pReloc = m_pRelocs;

        // we need to check only for more than one reloc entry 
        if (pReloc->m_type != IMAGE_REL_INVALID && pReloc[1].m_type != IMAGE_REL_INVALID)
        {
            bool isSorted = true;
            DWORD lastOffset = pReloc->m_offset;
            DWORD cReloc = 1;

            // we start with the second entry (the first entry is already consumed)
            while (pReloc[cReloc].m_type != IMAGE_REL_INVALID)
            {
                // we cannot abort the loop here because we need to count the entries
                // to properly sort the relocs!!!
                if (pReloc[cReloc].m_offset < lastOffset)
                    isSorted = false;
                lastOffset = pReloc[cReloc].m_offset;
                cReloc++;
            }
            if (!isSorted)
            {
                qsort(pReloc, cReloc, sizeof(ZapReloc), CmpZapRelocs);
            }
        }

        ZapImage * pImage = ZapImage::GetImage(pZapWriter);
        PBYTE pData = GetData();

        for (pReloc = m_pRelocs; pReloc->m_type != IMAGE_REL_INVALID; pReloc++)
        {
            PBYTE pLocation = pData + pReloc->m_offset;
            int targetOffset = 0;

            // Decode the offset
            switch (pReloc->m_type)
            {
            case IMAGE_REL_BASED_ABSOLUTE:
                targetOffset = *(UNALIGNED DWORD *)pLocation;
                break;

            case IMAGE_REL_BASED_ABSOLUTE_TAGGED:
                targetOffset = 0;
                break;

            case IMAGE_REL_BASED_PTR:
                targetOffset = (int)*(UNALIGNED TADDR *)pLocation;
                break;
            case IMAGE_REL_BASED_RELPTR:
                targetOffset = (int)*(UNALIGNED TADDR *)pLocation;
                break;

            case IMAGE_REL_BASED_RELPTR32:
                targetOffset = (int)*(UNALIGNED INT32 *)pLocation;
                break;

#if defined(_TARGET_X86_) || defined(_TARGET_AMD64_)
            case IMAGE_REL_BASED_REL32:
                targetOffset = *(UNALIGNED INT32 *)pLocation;
                break;
#endif // _TARGET_X86_ || _TARGET_AMD64_

#if defined(_TARGET_ARM_)
            case IMAGE_REL_BASED_THUMB_MOV32:
                targetOffset = (int)GetThumb2Mov32((UINT16 *)pLocation);
                break;

            case IMAGE_REL_BASED_THUMB_BRANCH24:
                targetOffset = GetThumb2BlRel24((UINT16 *)pLocation);
                break;
#endif // defined(_TARGET_ARM_)

#if defined(_TARGET_ARM64_)
            case IMAGE_REL_ARM64_BRANCH26:
                targetOffset = (int)GetArm64Rel28((UINT32*)pLocation);
                break;

            case IMAGE_REL_ARM64_PAGEBASE_REL21:
                targetOffset = (int)GetArm64Rel21((UINT32*)pLocation);
                break;

            case IMAGE_REL_ARM64_PAGEOFFSET_12A:
                targetOffset = (int)GetArm64Rel12((UINT32*)pLocation);
                break;

#endif // defined(_TARGET_ARM64_)

            default:
                _ASSERTE(!"Unknown reloc type");
                break;
            }

            pImage->WriteReloc(pData, pReloc->m_offset,
                pReloc->m_pTargetNode, targetOffset, pReloc->m_type);
        }
    }

    ZapBlob::Save(pZapWriter);
}

COUNT_T ZapBlobWithRelocs::GetCountOfStraddlerRelocations(DWORD dwPos)
{
    if (m_pRelocs == NULL)
        return 0;

    // Straddlers can exist only if the node is crossing page boundary
    if (AlignDown(dwPos, RELOCATION_PAGE_SIZE) == AlignDown(dwPos + GetSize() - 1, RELOCATION_PAGE_SIZE))
        return 0;

    COUNT_T nStraddlers = 0;

    for (ZapReloc * pReloc = m_pRelocs; pReloc->m_type != IMAGE_REL_INVALID; pReloc++)
    {
        if (pReloc->m_type == IMAGE_REL_BASED_PTR)
        {
            if (AlignmentTrim(dwPos + pReloc->m_offset, RELOCATION_PAGE_SIZE) > RELOCATION_PAGE_SIZE - sizeof(TADDR))
                nStraddlers++;          
        }
    }

    return nStraddlers;
}

ZapBlobWithRelocs * ZapBlobWithRelocs::NewBlob(ZapWriter * pWriter, PVOID pData, SIZE_T cbSize)
{
    S_SIZE_T cbAllocSize = S_SIZE_T(sizeof(ZapBlobWithRelocs)) + S_SIZE_T(cbSize);
    if(cbAllocSize.IsOverflow())
        ThrowHR(COR_E_OVERFLOW);
    
    void * pMemory = new (pWriter->GetHeap()) BYTE[cbAllocSize.Value()];

    ZapBlobWithRelocs * pZapBlobWithRelocs = new (pMemory) ZapBlobWithRelocs(cbSize);
    
    if (pData != NULL)
        memcpy((void*)(pZapBlobWithRelocs + 1), pData, cbSize);

    return pZapBlobWithRelocs;
}

template <DWORD alignment>
class ZapAlignedBlobWithRelocsConst : public ZapBlobWithRelocs
{
protected:
    ZapAlignedBlobWithRelocsConst(SIZE_T cbSize)
        : ZapBlobWithRelocs(cbSize)
    {
    }

public:
    virtual UINT GetAlignment()
    {
        return alignment;
    }

    static ZapBlobWithRelocs * NewBlob(ZapWriter * pWriter, PVOID pData, SIZE_T cbSize)
    {
        S_SIZE_T cbAllocSize = S_SIZE_T(sizeof(ZapAlignedBlobWithRelocsConst<alignment>)) + S_SIZE_T(cbSize);
        if(cbAllocSize.IsOverflow())
            ThrowHR(COR_E_OVERFLOW);
        
        void * pMemory = new (pWriter->GetHeap()) BYTE[cbAllocSize.Value()];

        ZapAlignedBlobWithRelocsConst<alignment> * pZapBlob = new (pMemory) ZapAlignedBlobWithRelocsConst<alignment>(cbSize);

        if (pData != NULL)
            memcpy((void*)(pZapBlob + 1), pData, cbSize);

        return pZapBlob;
    }
};

ZapBlobWithRelocs * ZapBlobWithRelocs::NewAlignedBlob(ZapWriter * pWriter, PVOID pData, SIZE_T cbSize, SIZE_T cbAlignment)
{
    switch (cbAlignment)
    {
    case 1:
        return ZapBlobWithRelocs::NewBlob(pWriter, pData, cbSize);
    case 2:
        return ZapAlignedBlobWithRelocsConst<2>::NewBlob(pWriter, pData, cbSize);
    case 4:
        return ZapAlignedBlobWithRelocsConst<4>::NewBlob(pWriter, pData, cbSize);
    case 8:
        return ZapAlignedBlobWithRelocsConst<8>::NewBlob(pWriter, pData, cbSize);
    case 16:
        return ZapAlignedBlobWithRelocsConst<16>::NewBlob(pWriter, pData, cbSize);

    default:
        _ASSERTE(!"Requested alignment not supported");
        return NULL;
    }
}
