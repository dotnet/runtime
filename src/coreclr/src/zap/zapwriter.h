// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//
// ZapWriter.h
//

//
// Infrastructure for writing PE files. (Not NGEN specific)
//
// ======================================================================================



#ifndef __ZAPWRITER_H__
#define __ZAPWRITER_H__

#include "zapnodetype.h"

class ZapWriter;
class ZapHeap;

// This is maximum size of anything in the image written by ZapWriter. Used for overflow checking.
#define ZAPWRITER_MAX_SIZE 0x3FFFFFFF

// All ZapNodes should be allocated from ZapHeap returned by ZapWriter::GetHeap()
void *operator new(size_t size, ZapHeap * pZapHeap);
void *operator new[](size_t size, ZapHeap * pZapHeap);

//
// ZapHeap does not support deallocation. Empty operators delete avoids deallocating memory
// if the constructor fails
//
inline void operator delete(void *, ZapHeap * pZapHeap)
{
    // Memory allocated by ZapHeap is never freed
}
inline void operator delete[](void *, ZapHeap * pZapHeap)
{
    // Memory allocated by ZapHeap is never freed
}


//------------------------------------------------------------------------------------------------------
// ZapNode is the basic building block of the native image. Every ZapNode must know how to persist itself.
//
// The basic contract for a ZapNode is that it understands its allocations requirements (size and alignment),
// and knows how to save itself (given a ZapWriter). At some point a ZapNode is given a location in the
// executable (an RVA), which it is responsible remembering.
//
// See file:../../doc/BookOfTheRuntime/NGEN/NGENDesign.doc for an overview.
//
class ZapNode
{
    friend class ZapWriter;

    DWORD m_RVA;

public:
    void SetRVA(DWORD dwRVA)
    {
        _ASSERTE(m_RVA == 0 || m_RVA == (DWORD)-1);
        m_RVA = dwRVA;
    }

    ZapNode()
    {
        // All ZapNodes are expected to be allocate from ZapWriter::GetHeap() that returns zero filled memory
#ifdef __GNUC__
#pragma GCC diagnostic push
#pragma GCC diagnostic ignored "-Wuninitialized"
#endif
        _ASSERTE(m_RVA == 0);
#ifdef __GNUC__
#pragma GCC diagnostic pop
#endif
    }

    // This constructor should be used to allocate temporary ZapNodes on the stack only
    ZapNode(DWORD rva)
        : m_RVA(rva)
    {
    }

    virtual ~ZapNode()
    {
    }

    // Returns the size of the node in the image. All nodes that are written into the image should override this method.
    virtual DWORD GetSize()
    {
#if defined(_MSC_VER) //UNREACHABLE doesn't work in GCC, when the method has a non-void return
        UNREACHABLE();
#else
        _ASSERTE(!"Unreachable");
        return 0;
#endif
    }

    // Alignment for this node.
    virtual UINT GetAlignment()
    {
        return 1;
    }

    // Returns the type of the ZapNode. All nodes should override this method.
    virtual ZapNodeType GetType()
    {
        return ZapNodeType_Unknown;
    }

    // Assign RVA to this node. dwPos is current RVA, returns updated current RVA.
    virtual DWORD ComputeRVA(ZapWriter * pZapWriter, DWORD dwPos);

    // All nodes that are written into the image should override this method. The implementation should write exactly GetSize() bytes
    // using ZapWriter::Write method
    virtual void Save(ZapWriter * pZapWriter)
    {
        UNREACHABLE();
    }

    // Returns the RVA of the node. Valid only after ComputeRVA phase
    DWORD GetRVA()
    {
        _ASSERTE(m_RVA != 0 && m_RVA != (DWORD)-1);
        return m_RVA;
    }

    // Returns whether the node was placed into a virtual section
    BOOL IsPlaced()
    {
        return m_RVA != 0;
    }
};

//---------------------------------------------------------------------------------------
// Virtual section of PE image.
class ZapVirtualSection : public ZapNode
{
    friend class ZapWriter;

    DWORD m_dwAlignment;

    SArray<ZapNode *> m_Nodes;

    // State initialized once the section is placed
    DWORD m_dwSize;

    DWORD m_dwSectionType;

    BYTE m_defaultFill;

    ZapVirtualSection(DWORD dwAlignment)
        : m_dwAlignment(dwAlignment)
    {
    }

public:
    virtual DWORD GetSize()
    {
        return m_dwSize;
    }

    virtual ZapNodeType GetType()
    {
        return ZapNodeType_VirtualSection;
    }

    DWORD GetSectionType()
    {
        return m_dwSectionType;
    }

    void SetSectionType(DWORD dwSectionType)
    {
        _ASSERTE((dwSectionType & IBCTypeReservedFlag) != 0 || !"IBCType flag is not specified");
        _ASSERTE((dwSectionType & RangeTypeReservedFlag) != 0 || !"RangeType flag is not specified");
        _ASSERTE((dwSectionType & VirtualSectionTypeReservedFlag) != 0 || !"VirtualSectionType flag is not specified");
        _ASSERTE((dwSectionType & VirtualSectionTypeReservedFlag) < CORCOMPILE_SECTION_TYPE_COUNT || !"Invalid VirtualSectionType flag");
        m_dwSectionType = dwSectionType;
    }

    void SetDefaultFill(BYTE fill)
    {
        m_defaultFill = fill;
    }

    void Place(ZapNode * pNode)
    {
        _ASSERTE(!pNode->IsPlaced());
        m_Nodes.Append(pNode);
        pNode->SetRVA((DWORD)-1);
    }

    COUNT_T GetNodeCount()
    {
        return m_Nodes.GetCount();
    }

    ZapNode * GetNode(COUNT_T iNode)
    {
        return m_Nodes[iNode];
    }
};

//---------------------------------------------------------------------------------------
// The named physical section of the PE Image. It contains one or more virtual sections.
class ZapPhysicalSection : public ZapNode
{
    friend class ZapWriter;

    SArray<ZapVirtualSection *> m_Sections;

    LPCSTR m_pszName;
    DWORD m_dwCharacteristics;

    // Number of zero filled sections (zero filled sections are always last in m_Sections array)
    COUNT_T m_nBssSections;

    // State initialized once the section is placed
    DWORD m_dwSize;
    DWORD m_dwFilePos;
    DWORD m_dwSizeOfRawData;

    ZapPhysicalSection(LPCSTR pszName, DWORD dwCharacteristics)
        : m_pszName(pszName),
          m_dwCharacteristics(dwCharacteristics)
    {
    }

public:
    ~ZapPhysicalSection()
    {
        for (COUNT_T iVirtualSection = 0; iVirtualSection < m_Sections.GetCount(); iVirtualSection++)
        {
            ZapVirtualSection * pVirtualSection = m_Sections[iVirtualSection];
            pVirtualSection->~ZapVirtualSection();
        }
    }

    virtual DWORD GetSize()
    {
        return m_dwSize;
    }

    virtual ZapNodeType GetType()
    {
        return ZapNodeType_PhysicalSection;
    }

    DWORD GetFilePos()
    {
        _ASSERTE(m_dwFilePos != 0);
        return m_dwFilePos;
    }

    COUNT_T GetVirtualSectionCount()
    {
        return m_Sections.GetCount();
    }

    ZapVirtualSection * GetVirtualSection(COUNT_T iSection)
    {
        return m_Sections[iSection];
    }

};

//---------------------------------------------------------------------------------------
//
// The ZapWriter
//
// Notice that ZapWriter implements IStream that can be passed to APIs that write to stream
//
// The main API in a ZapWriter is (not suprisingly) the code:ZapWriter.Write method.
//
// Relocations are handled by a higher level object, code:ZapImage, which knows about all the sections of a
// ngen image and how to do relections.  Every ZapWriter has an associated ZapImage which you get to by
// calling code:ZapImage.GetImage.
//
class ZapWriter : public IStream
{
    ZapHeap * m_pHeap;

    SArray<ZapPhysicalSection *> m_Sections;

    ZapNode * m_DirectoryEntries[IMAGE_NUMBEROF_DIRECTORY_ENTRIES];
    DWORD     m_dwTimeDateStamp;
    ULONGLONG m_BaseAddress;
    ULONGLONG m_SizeOfStackReserve;
    ULONGLONG m_SizeOfStackCommit;
    USHORT    m_Subsystem;
    USHORT    m_DllCharacteristics;
    BOOL      m_isDll;
    DWORD     m_FileAlignment;

    // Current state of the writer for debug checks
    INDEBUG(BOOL m_fSaving;)

    DWORD m_dwCurrentRVA;
    BOOL m_fWritingRelocs; // Set to true once we start reloc sections at the end of the file

    void SaveContent();

    DWORD GetSizeOfNTHeaders();
    void SaveHeaders();

    // Simple buffered writer
    void InitializeWriter(IStream * pStream);

    IStream * m_pStream;
    PBYTE m_pBuffer;
    ULONG m_nBufferPos;
    INDEBUG(DWORD m_dwWriterFilePos;)

    //
    // NT Headers
    //

    BOOL Is64Bit()
    {
#ifdef _TARGET_64BIT_
        return TRUE;
#else // !_TARGET_64BIT_
        return FALSE;
#endif // !_TARGET_64BIT_
    }

    USHORT GetMachine()
    {
        return IMAGE_FILE_MACHINE_NATIVE_NI;
    }

    void SaveDosHeader();
    void SaveSignature();
    void SaveFileHeader();
    void SaveOptionalHeader();
    void SaveSections();

    // IStream support - the only actually implemented method is IStream::Write

    // IUnknown methods
    STDMETHODIMP_(ULONG) AddRef()
    {
        return 1;
    }

    STDMETHODIMP_(ULONG) Release()
    {
        return 1;
    }

    STDMETHODIMP QueryInterface(REFIID riid, LPVOID *ppv)
    {
        HRESULT hr = S_OK;
        if (IsEqualIID(riid, IID_IUnknown) || IsEqualIID(riid, IID_IStream)) {
            *ppv = static_cast<IStream *>(this);
        }
        else {
            *ppv = NULL;
            hr = E_NOINTERFACE;
        }
        return hr;
    }

    // ISequentialStream methods:
    STDMETHODIMP Read(void *pv, ULONG cb, ULONG *pcbRead)
    {
        _ASSERTE(false);
        return E_NOTIMPL;
    }

    STDMETHODIMP Write(void const *pv, ULONG cb, ULONG *pcbWritten);

    // IStream methods:
    STDMETHODIMP Seek(LARGE_INTEGER dlibMove, DWORD dwOrigin, ULARGE_INTEGER *plibNewPosition)
    {
        // IMetaDataEmit::SaveToStream calls Seek(0) but ignores the returned error
        //_ASSERTE(false);
        return E_NOTIMPL;
    }

    STDMETHODIMP SetSize(ULARGE_INTEGER libNewSize)
    {
        _ASSERTE(false);
        return E_NOTIMPL;
    }

    STDMETHODIMP CopyTo(IStream *pstm, ULARGE_INTEGER cb, ULARGE_INTEGER *pcbRead, ULARGE_INTEGER *pcbWritten)
    {
        _ASSERTE(false);
        return E_NOTIMPL;
    }

    STDMETHODIMP Commit(DWORD grfCommitFlags)
    {
        _ASSERTE(false);
        return E_NOTIMPL;
    }

    STDMETHODIMP Revert()
    {
        _ASSERTE(false);
        return E_NOTIMPL;
    }

    STDMETHODIMP LockRegion(ULARGE_INTEGER libOffset, ULARGE_INTEGER cb, DWORD dwLockType)
    {
        _ASSERTE(false);
        return E_NOTIMPL;
    }

    STDMETHODIMP UnlockRegion(ULARGE_INTEGER libOffset, ULARGE_INTEGER cb, DWORD dwLockType)
    {
        _ASSERTE(false);
        return E_NOTIMPL;
    }

    STDMETHODIMP Stat(STATSTG *pstatstg, DWORD grfStatFlag)
    {
        _ASSERTE(false);
        return E_NOTIMPL;
    }

    STDMETHODIMP Clone(IStream **ppIStream)
    {
        _ASSERTE(false);
        return E_NOTIMPL;
    }

public:
    ZapWriter();
    ~ZapWriter();

    void Initialize();

    // Create new section in the PE file. The sections will be saved in the order they are created.
    ZapPhysicalSection * NewPhysicalSection(LPCSTR pszName, DWORD dwCharacteristics)
    {
        _ASSERTE(!IsSaving());
        ZapPhysicalSection * pSection = new (GetHeap()) ZapPhysicalSection(pszName, dwCharacteristics);
        m_Sections.Append(pSection);
        return pSection;
    }

    // Create new virtual section within the physical section. The sections will be saved in the order they are created.
    // The default virtual section alignment is 16.
    ZapVirtualSection * NewVirtualSection(ZapPhysicalSection * pPhysicalSection, DWORD dwAlignment = 16, ZapVirtualSection * pInsertAfter = NULL)
    {
        _ASSERTE(!IsSaving());
        ZapVirtualSection * pSection = new (GetHeap()) ZapVirtualSection(dwAlignment);
        if (pInsertAfter != NULL)
        {
            // pInsertAfter is workaround to get decent layout with the current scheme of virtual sections. It should not be necessary
            // once we have better layout algorithms in place.
            for (COUNT_T iSection = 0; iSection < pPhysicalSection->m_Sections.GetCount(); iSection++)
            {
                if (pPhysicalSection->m_Sections[iSection] == pInsertAfter)
                {
                    pPhysicalSection->m_Sections.Insert(pPhysicalSection->m_Sections+(iSection+1));
                    pPhysicalSection->m_Sections[iSection+1] = pSection;
                    return pSection;
                }
            }
            _ASSERTE(false);
        }

        pPhysicalSection->m_Sections.Append(pSection);
        return pSection;
    }

    void MarkBssSection(ZapPhysicalSection * pPhysicalSection, ZapVirtualSection * pSection)
    {
        _ASSERTE(!IsSaving());
        _ASSERTE(pPhysicalSection->m_Sections[pPhysicalSection->m_Sections.GetCount() - 1] == pSection);
        pPhysicalSection->m_nBssSections++;
    }

    void Append(ZapVirtualSection * pVirtualSection, ZapNode * pNode)
    {
        _ASSERTE(!IsSaving());
        pVirtualSection->m_Nodes.Append(pNode);
    }

    // Set the directory entry in the image to match the given ZapNode
    void SetDirectoryEntry(DWORD entry, ZapNode * pNode)
    {
        _ASSERTE(!IsSaving());
        _ASSERTE(entry < IMAGE_NUMBEROF_DIRECTORY_ENTRIES);
        _ASSERTE(m_DirectoryEntries[entry] == NULL);
        m_DirectoryEntries[entry] = pNode;
    }

    // Set the timedate stamp of the image
    void SetTimeDateStamp(DWORD dwTimeDateStamp)
    {
        _ASSERTE(!IsSaving());
        m_dwTimeDateStamp = dwTimeDateStamp;
    }

    // Set the base address of the image
    void SetBaseAddress(ULONGLONG baseAddress)
    {
        _ASSERTE(!IsSaving());
        m_BaseAddress = baseAddress;
    }

    ULONGLONG GetBaseAddress()
    {
        _ASSERTE(m_BaseAddress != 0);
        return m_BaseAddress;
    }

    void SetSizeOfStackReserve(ULONGLONG sizeOfStackReserve)
    {
        _ASSERTE(!IsSaving());
        m_SizeOfStackReserve = sizeOfStackReserve;
    }

    void SetSizeOfStackCommit(ULONGLONG sizeOfStackCommit)
    {
        _ASSERTE(!IsSaving());
        m_SizeOfStackCommit = sizeOfStackCommit;
    }

    void SetSubsystem(USHORT subsystem)
    {
        _ASSERTE(!IsSaving());
        m_Subsystem = subsystem;
    }

    void SetDllCharacteristics(USHORT dllCharacteristics)
    {
        _ASSERTE(!IsSaving());
        m_DllCharacteristics = dllCharacteristics;
    }

    void SetIsDll(BOOL isDLL)
    {
        m_isDll = isDLL;
    }

    void SetFileAlignment(DWORD fileAlignment)
    {
        m_FileAlignment = fileAlignment;
    }

    // Compute RVAs for everything in the file
    void ComputeRVAs();

    // Save the content into stream
    void Save(IStream * pStream);

    // Get the heap. The lifetime of this heap is same as the lifetime of the ZapWriter. All ZapNodes should
    // be allocated from this heap.
    ZapHeap * GetHeap()
    {
        return m_pHeap;
    }

    COUNT_T GetPhysicalSectionCount()
    {
        return m_Sections.GetCount();
    }

    ZapPhysicalSection * GetPhysicalSection(COUNT_T iSection)
    {
        return m_Sections[iSection];
    }

#ifdef _DEBUG
    // Certain methods can be called only during the save phase
    BOOL IsSaving()
    {
        return m_fSaving;
    }
#endif

    DWORD GetCurrentRVA()
    {
        _ASSERTE(IsSaving());
        return m_dwCurrentRVA;
    }


    // This is the main entrypoint used to write the image. Every implementation of ZapNode::Save will call this method.
    void Write(PVOID p, DWORD dwSize);

    // Writes padding
    void WritePad(DWORD size, BYTE fill = 0);

    // Flush any buffered data
    void FlushWriter();

    BOOL IsWritingRelocs()
    {
        return m_fWritingRelocs;
    }

    void SetWritingRelocs()
    {
        m_fWritingRelocs = TRUE;
    }

    // Convenience helper to initialize IMAGE_DATA_DIRECTORY
    static void SetDirectoryData(IMAGE_DATA_DIRECTORY * pDir, ZapNode * pZapNode);
};

//---------------------------------------------------------------------------------------
// ZapBlob
//
// Generic node for unstructured sequence of bytes.
// Includes SHash support (ZapBlob::SHashTraits)
//
class ZapBlob : public ZapNode
{
    DWORD m_cbSize;

protected:
    ZapBlob(SIZE_T cbSize)
        : m_cbSize((DWORD)cbSize)
    {
        if (cbSize > ZAPWRITER_MAX_SIZE)
            ThrowHR(COR_E_OVERFLOW);
    }

public:
    class SHashKey
    {
        PBYTE   m_pData;
        SIZE_T  m_cbSize;

    public:
        SHashKey(PVOID pData, SIZE_T cbSize)
            : m_pData((PBYTE)pData), m_cbSize(cbSize)
        {
        }

        PBYTE GetData() const
        {
            return m_pData;
        }

        SIZE_T GetBlobSize() const
        {
            return m_cbSize;
        }
    };

    class SHashTraits : public DefaultSHashTraits<ZapBlob *>
    {
    public:
        typedef const ZapBlob::SHashKey key_t;

        static key_t GetKey(element_t e)
        {
            LIMITED_METHOD_CONTRACT;
            return key_t(e->GetData(), e->GetBlobSize());
        }
        static BOOL Equals(key_t k1, key_t k2)
        {
            LIMITED_METHOD_CONTRACT;
            if (k1.GetBlobSize() != k2.GetBlobSize())
                return FALSE;
            return memcmp(k1.GetData(), k2.GetData(), k1.GetBlobSize()) == 0;
        }
        static count_t Hash(key_t k)
        {
            LIMITED_METHOD_CONTRACT;
            count_t hash = 5381 + (count_t)(k.GetBlobSize() << 7);

            PBYTE pbData = k.GetData();
            PBYTE pbDataEnd = pbData + k.GetBlobSize();

            for (/**/ ; pbData < pbDataEnd; pbData++)
            {
                hash = ((hash << 5) + hash) ^ *pbData;
            }
            return hash;
        }
    };

    virtual PBYTE GetData()
    {
        return (PBYTE)(this + 1);
    }

    // Used to shrink the size of the blob
    void AdjustBlobSize(SIZE_T cbSize)
    {
        _ASSERTE(cbSize <= m_cbSize);
        _ASSERTE(cbSize != 0);
        m_cbSize = (DWORD)cbSize;
    }

    // Raw size of the blob
    DWORD GetBlobSize()
    {
        return m_cbSize;
    }

    virtual DWORD GetSize()
    {
        return m_cbSize;
    }

    virtual ZapNodeType GetType()
    {
        return ZapNodeType_Blob;
    }

    virtual void Save(ZapWriter * pZapWriter);

    // Create new zap blob node. The node *does* own copy of the memory.
    static ZapBlob * NewBlob(ZapWriter * pWriter, PVOID pData, SIZE_T cbSize);

    // Create new aligned zap blob node.
    static ZapBlob * NewAlignedBlob(ZapWriter * pWriter, PVOID pData, SIZE_T cbSize, SIZE_T cbAlignment);
};

class ZapBlobPtr : public ZapBlob
{
    PBYTE m_pData;

public:
    ZapBlobPtr(PVOID pData, SIZE_T cbSize)
        : ZapBlob(cbSize), m_pData((PBYTE)pData)
    {
    }

    virtual PBYTE GetData()
    {
        return m_pData;
    }
};

class ZapDummyNode : public ZapNode
{
    DWORD m_cbSize;

public:
    ZapDummyNode(DWORD cbSize)
        : m_cbSize(cbSize)
    {
    }

    virtual DWORD GetSize()
    {
        return m_cbSize;
    }
};

#endif // __ZAPWRITER_H__
