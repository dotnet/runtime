// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//
// ZapRelocs.h
//

//
// Zapping of relocations
// 
// ======================================================================================

#ifndef __ZAPRELOCS_H__
#define __ZAPRELOCS_H__

typedef BYTE ZapRelocationType; // IMAGE_REL_XXX enum

#ifdef BINDER
// Special binder specific relocation (on ARM):
// bit0 in NativeCodeEntry in MethodDesc is used to signify "no fixup list" (not THUMB2 code)
// otherwise should be treated exactly like IMAGE_REL_BASED_PTR
#define IMAGE_REL_BASED_MD_METHODENTRY    0x7F
#endif // BINDER

// Special NGEN-specific relocation type for fixups (absolute RVA in the middle 30 bits)
#define IMAGE_REL_BASED_ABSOLUTE_TAGGED   0x7E

// Special NGEN-specific relocation type for relative pointer (used to make NGen relocation section smaller)
#define IMAGE_REL_BASED_RELPTR            0x7D
#define IMAGE_REL_BASED_RELPTR32          0x7C

// Invalid reloc marker (used to mark end of the reloc array)
#define IMAGE_REL_INVALID           0xFF

// IMAGE_REL_BASED_PTR is architecture specific reloc of virtual address
#ifdef _WIN64
#define IMAGE_REL_BASED_PTR IMAGE_REL_BASED_DIR64
#else
#define IMAGE_REL_BASED_PTR IMAGE_REL_BASED_HIGHLOW
#endif

// Size of base relocs relocation page
#define RELOCATION_PAGE_SIZE    0x1000

//
// The ZapNode for regular PE base relocs
//

class ZapBaseRelocs : public ZapNode
{
    ZapImage * m_pImage;

    // The page currently being written
    DWORD m_page;
    COUNT_T m_pageIndex;

    // Reloc writer output
    SArray<USHORT> m_SerializedRelocs;

    void FlushWriter();

public:
    ZapBaseRelocs(ZapImage * pImage)
        : m_pImage(pImage)
    {
        // Everything is zero initialized by the allocator
    }

    void WriteReloc(PVOID pSrc, int offset, ZapNode * pTarget, int targetOffset, ZapRelocationType type);

    virtual DWORD GetSize()
    {
        return m_SerializedRelocs.GetCount() * sizeof(USHORT);
    }

    virtual ZapNodeType GetType()
    {
        return ZapNodeType_Relocs;
    }

    virtual void Save(ZapWriter * pZapWriter);
};

//
//
// Blob with associated relocations. Used for compiled code.
//

struct ZapReloc
{
    ZapRelocationType   m_type;
    DWORD               m_offset;
    ZapNode *           m_pTargetNode;
};

class ZapBlobWithRelocs : public ZapBlob
{
    ZapReloc * m_pRelocs;

protected:
    ZapBlobWithRelocs(SIZE_T cbSize)
        : ZapBlob(cbSize)
    {
    }

public:
    void SetRelocs(ZapReloc * pRelocs)
    {
        _ASSERTE(m_pRelocs == NULL);
        _ASSERTE(pRelocs != NULL);
        m_pRelocs = pRelocs;
    }

    ZapReloc * GetRelocs()
    {
        return m_pRelocs;
    }

    virtual PBYTE GetData()
    {
        return (PBYTE)(this + 1);
    }

    virtual void Save(ZapWriter * pZapWriter);

    // Returns number of straddler relocs, assuming RVA of the node is dwPos
    COUNT_T GetCountOfStraddlerRelocations(DWORD dwPos);

    // Create new zap blob node. The node *does* own copy of the memory.
    static ZapBlobWithRelocs * NewBlob(ZapWriter * pWriter, PVOID pData, SIZE_T cbSize);

    // Create new aligned zap blob node.
    static ZapBlobWithRelocs * NewAlignedBlob(ZapWriter * pWriter, PVOID pData, SIZE_T cbSize, SIZE_T cbAlignment);

#ifdef BINDER
    // Compress a reloc blob that was pessimistically sized (removes entries with a NULL target node).
    void SqueezeRelocs(DWORD entryCount);

    // Helper to set a reloc target to a specific offset.
    void SetPointerToOffset(size_t offset, size_t setOffs)
    {
        assert(offset < GetSize() && offset + sizeof(SIZE_T) <= GetSize());
        *(SIZE_T *)(GetData() + offset) = setOffs;
    }

    // Helper to zero a reloc target.
    void ZeroPointer(size_t offset)
    {
        SetPointerToOffset(offset, 0);
    }

#ifdef CLR_STANDALONE_BINDER // REDHAWK doesn't use the low-bit trick (yet?)
    // Helper to set reloc target to 1, which indicates a double indirection in the CLR.
    void SetPointerToIndirect(size_t offset)
    {
        SetPointerToOffset(offset, 1);
    }
#endif
#endif
};

#if defined(TARGET_THUMB2) && defined(BINDER)
class ZapThumb2CodeBlob : public ZapBlobWithRelocs
{
protected:
    ZapThumb2CodeBlob(SIZE_T cbSize)
        : ZapBlobWithRelocs(cbSize)
    {
    }

public:
    virtual UINT GetAlignment()
    {
        return 4;
    }

    static ZapThumb2CodeBlob * NewThumb2CodeBlob(ZapWriter * pWriter, PVOID pData, SIZE_T cbSize)
    {
        S_SIZE_T cbAllocSize = S_SIZE_T(sizeof(ZapThumb2CodeBlob)) + S_SIZE_T(cbSize);
        if(cbAllocSize.IsOverflow())
            ThrowHR(COR_E_OVERFLOW);
        
        void * pMemory = new (pWriter->GetHeap()) BYTE[cbAllocSize.Value()];

        ZapThumb2CodeBlob * pZapBlob = new (pMemory) ZapThumb2CodeBlob(cbSize);

        if (pData != NULL)
            memcpy(pZapBlob + 1, pData, cbSize);

        return pZapBlob;
    }

    virtual BOOL IsThumb2Code()
    {
        return TRUE;
    }
};
#endif // TARGET_THUMB2 && BINDER


#endif // __ZAPRELOCS_H__
