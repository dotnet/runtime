// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// ZapWriter.cpp
//

//
// Infrastructure for writing PE files. (Not NGEN specific)
//
// ======================================================================================

#include "common.h"

//---------------------------------------------------------------------------------------
// ZapNode

void * operator new(size_t size, ZapHeap * pHeap)
{
    return ((LoaderHeap*)pHeap)->AllocMem(S_SIZE_T(size));
}

void * operator new[](size_t size, ZapHeap * pHeap)
{
    return ((LoaderHeap*)pHeap)->AllocMem(S_SIZE_T(size));
}

//---------------------------------------------------------------------------------------
// ZapWriter

ZapWriter::ZapWriter()
{
}

ZapWriter::~ZapWriter()
{
    for (COUNT_T iPhysicalSection = 0; iPhysicalSection < m_Sections.GetCount(); iPhysicalSection++)
    {
        ZapPhysicalSection * pPhysicalSection = m_Sections[iPhysicalSection];
        pPhysicalSection->~ZapPhysicalSection();
    }
    delete (LoaderHeap*)m_pHeap;
}

void ZapWriter::Initialize()
{
    const DWORD dwReserveSize = 0x1000000;
    const DWORD dwCommitSize  = 0x10000;

    m_pHeap = reinterpret_cast<ZapHeap*>(new LoaderHeap(dwReserveSize, dwCommitSize));

    m_isDll = true;

    // Default file alignment
    m_FileAlignment = 0x200;
}

#if defined(TARGET_UNIX) && defined(TARGET_64BIT)
#define SECTION_ALIGNMENT   m_FileAlignment
#define PAL_MAX_PAGE_SIZE   0x10000
#else
#define SECTION_ALIGNMENT   0x1000
#define PAL_MAX_PAGE_SIZE   0
#endif

void ZapWriter::Save(IStream * pStream)
{
    INDEBUG(m_fSaving = TRUE;)

    InitializeWriter(pStream);

    _ASSERTE(m_Sections.GetCount() > 0);

    ZapPhysicalSection * pLastPhysicalSection = m_Sections[m_Sections.GetCount() - 1];

    ULARGE_INTEGER estimatedFileSize;
    estimatedFileSize.QuadPart = pLastPhysicalSection->m_dwFilePos + pLastPhysicalSection->m_dwSizeOfRawData;

    if (CLRConfig::GetConfigValue(CLRConfig::INTERNAL_NGenSimulateDiskFull) != 0)
    {
        ThrowHR(HRESULT_FROM_WIN32(ERROR_DISK_FULL));
    }

    // Set the file size upfront to reduce disk fragmentation
    IfFailThrow(pStream->SetSize(estimatedFileSize));

    LARGE_INTEGER zero;
    zero.QuadPart = 0;

    // Write the content of all sections
    IfFailThrow(pStream->Seek(zero, STREAM_SEEK_SET, NULL));
    SaveContent();
    FlushWriter();

    // Finally write the NT headers
    IfFailThrow(pStream->Seek(zero, STREAM_SEEK_SET, NULL));
    SaveHeaders();
    FlushWriter();
}

DWORD ZapNode::ComputeRVA(ZapWriter * pZapWriter, DWORD dwPos)
{
    dwPos = AlignUp(dwPos, GetAlignment());

    SetRVA(dwPos);

    dwPos += GetSize();

    return dwPos;
}

void ZapWriter::ComputeRVAs()
{
    DWORD dwHeaderSize = GetSizeOfNTHeaders();

    DWORD dwPos = dwHeaderSize;
    DWORD dwFilePos = dwHeaderSize;

    for (COUNT_T iPhysicalSection = 0; iPhysicalSection < m_Sections.GetCount(); iPhysicalSection++)
    {
        ZapPhysicalSection * pPhysicalSection = m_Sections[iPhysicalSection];

        DWORD dwAlignedFilePos = AlignUp(dwFilePos, m_FileAlignment);
        dwFilePos = dwAlignedFilePos;

        pPhysicalSection->m_dwFilePos = dwFilePos;

        dwPos = AlignUp(dwPos, SECTION_ALIGNMENT) + PAL_MAX_PAGE_SIZE;
        pPhysicalSection->SetRVA(dwPos);

        DWORD dwEndOfRawData = dwPos;

#ifdef REDHAWK
        printf("Physical Section \"%s\" {\n", pPhysicalSection->m_pszName);
#endif // REDHAWK

        for (COUNT_T iVirtualSection = 0; iVirtualSection < pPhysicalSection->m_Sections.GetCount(); iVirtualSection++)
        {
            ZapVirtualSection * pVirtualSection = pPhysicalSection->m_Sections[iVirtualSection];

            // Do not bother with empty virtual sections
            if (pVirtualSection->m_Nodes.GetCount() == 0)
                continue;

            dwPos = AlignUp(dwPos, pVirtualSection->m_dwAlignment);
            pVirtualSection->SetRVA(dwPos);

            for (COUNT_T iNode = 0; iNode < pVirtualSection->m_Nodes.GetCount(); iNode++)
            {
                ZapNode * pNode = pVirtualSection->m_Nodes[iNode];

                DWORD dwNextPos = pNode->ComputeRVA(this, dwPos);
                _ASSERTE(dwNextPos >= dwPos);

                if (dwNextPos < dwPos || dwNextPos > ZAPWRITER_MAX_SIZE)
                    ThrowHR(COR_E_OVERFLOW);

                dwPos = dwNextPos;
            }

            pVirtualSection->m_dwSize = dwPos - pVirtualSection->GetRVA();

            if (iVirtualSection < pPhysicalSection->m_Sections.GetCount() - pPhysicalSection->m_nBssSections)
                dwEndOfRawData = dwPos;
#ifdef REDHAWK
            if (pVirtualSection->m_dwSize > 0)
            {
                printf("    %08x (%6u bytes): %s\n", pVirtualSection->GetRVA(), pVirtualSection->m_dwSize, pVirtualSection->m_pszTag);
            }
#endif // REDHAWK
        }

        pPhysicalSection->m_dwSize = dwPos - pPhysicalSection->GetRVA();

        pPhysicalSection->m_dwSizeOfRawData = dwEndOfRawData - pPhysicalSection->GetRVA();

        dwFilePos += pPhysicalSection->m_dwSizeOfRawData;

#ifdef REDHAWK
        printf("    %08x: end\n", dwPos);
        printf("}\n");
#endif // REDHAWK
    }
}

void ZapWriter::SaveContent()
{
    DWORD dwHeaderSize = GetSizeOfNTHeaders();

    WritePad(dwHeaderSize);

    DWORD dwPos = dwHeaderSize;
    DWORD dwFilePos = dwHeaderSize;

    for (COUNT_T iPhysicalSection = 0; iPhysicalSection < m_Sections.GetCount(); iPhysicalSection++)
    {
        ZapPhysicalSection * pPhysicalSection = m_Sections[iPhysicalSection];
        DWORD dwAlignedFilePos = AlignUp(dwFilePos, m_FileAlignment);
        WritePad(dwAlignedFilePos - dwFilePos);
        dwFilePos = dwAlignedFilePos;

        dwPos = AlignUp(dwPos, SECTION_ALIGNMENT) + PAL_MAX_PAGE_SIZE;

        if (m_fWritingRelocs)
        {
            pPhysicalSection->m_RVA = dwPos;
            pPhysicalSection->m_dwFilePos = dwFilePos;
        }
        _ASSERTE(pPhysicalSection->GetRVA() == dwPos);
        _ASSERTE(pPhysicalSection->m_dwFilePos == dwFilePos);
        _ASSERTE(m_dwWriterFilePos == dwFilePos);

        for (COUNT_T iVirtualSection = 0; iVirtualSection < pPhysicalSection->m_Sections.GetCount() - pPhysicalSection->m_nBssSections; iVirtualSection++)
        {
            ZapVirtualSection * pVirtualSection = pPhysicalSection->m_Sections[iVirtualSection];

            // Do not bother with empty virtual sections
            if (pVirtualSection->m_Nodes.GetCount() == 0)
                continue;

            if (m_fWritingRelocs)
            {
                pVirtualSection->m_RVA = dwPos;

                _ASSERTE(pVirtualSection->m_Nodes.GetCount() == 1);
                pVirtualSection->m_Nodes[0]->m_RVA = dwPos;
            }

            DWORD dwVirtualSectionPos = pVirtualSection->GetRVA();
            if (dwVirtualSectionPos != dwPos)
                WritePad(dwVirtualSectionPos - dwPos);
            dwPos = dwVirtualSectionPos;

            for (COUNT_T iNode = 0; iNode < pVirtualSection->m_Nodes.GetCount(); iNode++)
            {
                ZapNode * pNode = pVirtualSection->m_Nodes[iNode];

                DWORD dwNodePos = pNode->GetRVA();
                if (dwNodePos != dwPos)
                    WritePad(dwNodePos - dwPos, pVirtualSection->m_defaultFill);
                dwPos = dwNodePos;

                m_dwCurrentRVA = dwPos;
                pNode->Save(this);

#ifdef _DEBUG
                if (dwPos + pNode->GetSize() != m_dwCurrentRVA)
                {
                    _ASSERTE(!"Mismatch between ZapNode::GetSize() and ZapNode::Save() implementations");
                    pNode->GetSize();
                    pNode->Save(this);
                }
#endif

                dwPos = m_dwCurrentRVA;
            }

            DWORD dwVirtualSectionSize = dwPos - pVirtualSection->GetRVA();
            if (m_fWritingRelocs)
            {
                pVirtualSection->m_dwSize = dwVirtualSectionSize;
            }
            _ASSERTE(pVirtualSection->m_dwSize == dwVirtualSectionSize);
        }

        DWORD dwPhysicalSectionSize = dwPos - pPhysicalSection->GetRVA();
        if (m_fWritingRelocs)
        {
            pPhysicalSection->m_dwSize = dwPhysicalSectionSize;
            pPhysicalSection->m_dwSizeOfRawData = dwPhysicalSectionSize;
        }
        _ASSERTE(pPhysicalSection->m_dwSizeOfRawData == dwPhysicalSectionSize);

        dwPos = pPhysicalSection->GetRVA() + pPhysicalSection->m_dwSize;

        dwFilePos += pPhysicalSection->m_dwSizeOfRawData;
    }

    WritePad(AlignmentPad(dwFilePos, m_FileAlignment));
}

//---------------------------------------------------------------------------------------
//
// ZapVirtualSection
//
#ifdef REDHAWK
UINT32 ZapVirtualSection::FillInNodeOffsetMap(MapSHash<ZapNode *, UINT32> * pMap)
{
    UINT32 dataSize = 0;
    for (int i = 0; i < m_Nodes.GetCount(); i++)
    {
        ZapNode* pNode = m_Nodes[i];
        pMap->Add(pNode, dataSize);
        dataSize += pNode->GetSize();
    }

    return dataSize;
}
#endif // REDHAWK

//---------------------------------------------------------------------------------------
// Simple buffered writer

#define WRITE_BUFFER_SIZE   0x10000

void ZapWriter::InitializeWriter(IStream * pStream)
{
    m_pBuffer = new (GetHeap()) BYTE[WRITE_BUFFER_SIZE];
    m_nBufferPos = 0;

    m_pStream = pStream;

    INDEBUG(m_dwWriterFilePos = 0;)
}

void ZapWriter::FlushWriter()
{
    if (m_nBufferPos > 0)
    {
        ULONG cbWritten;
        IfFailThrow(m_pStream->Write(m_pBuffer, m_nBufferPos, &cbWritten));
        _ASSERTE(cbWritten == m_nBufferPos);

        m_nBufferPos = 0;
    }
}

void ZapWriter::Write(PVOID p, DWORD dwSize)
{
    m_dwCurrentRVA += dwSize;
    INDEBUG(m_dwWriterFilePos += dwSize;)

    if (m_dwCurrentRVA >= ZAPWRITER_MAX_SIZE)
        ThrowHR(COR_E_OVERFLOW);

    DWORD cbAvailable = min(dwSize, WRITE_BUFFER_SIZE - m_nBufferPos);

    memcpy(m_pBuffer + m_nBufferPos, p, cbAvailable);
    p = (PBYTE)p + cbAvailable;
    dwSize -= cbAvailable;

    m_nBufferPos += cbAvailable;

    if (m_nBufferPos < WRITE_BUFFER_SIZE)
        return;

    FlushWriter();

    if (dwSize == 0)
        return;

    cbAvailable = AlignDown(dwSize, WRITE_BUFFER_SIZE);

    if (cbAvailable > 0)
    {
        ULONG cbWritten;
        IfFailThrow(m_pStream->Write(p, cbAvailable, &cbWritten));
        _ASSERTE(cbWritten == cbAvailable);

        p = (PBYTE)p + cbAvailable;
        dwSize -= cbAvailable;
    }

    _ASSERTE(m_nBufferPos == 0);
    memcpy(m_pBuffer, p, dwSize);
    m_nBufferPos = dwSize;
}

void ZapWriter::WritePad(DWORD dwSize, BYTE fill)
{
    m_dwCurrentRVA += dwSize;
    INDEBUG(m_dwWriterFilePos += dwSize;)

    if (m_dwCurrentRVA >= ZAPWRITER_MAX_SIZE)
        ThrowHR(COR_E_OVERFLOW);

    DWORD cbAvailable = min(dwSize, WRITE_BUFFER_SIZE - m_nBufferPos);

    memset(m_pBuffer + m_nBufferPos, fill, cbAvailable);
    dwSize -= cbAvailable;

    m_nBufferPos += cbAvailable;

    if (m_nBufferPos < WRITE_BUFFER_SIZE)
        return;

    FlushWriter();

    if (dwSize == 0)
        return;

    memset(m_pBuffer, fill, min(WRITE_BUFFER_SIZE, dwSize));

    while (dwSize >= WRITE_BUFFER_SIZE)
    {
        ULONG cbWritten;
        cbAvailable = min(WRITE_BUFFER_SIZE, dwSize);
        IfFailThrow(m_pStream->Write(m_pBuffer, cbAvailable, &cbWritten));
        _ASSERTE(cbWritten == cbAvailable);

        dwSize -= cbAvailable;
    }

    m_nBufferPos = dwSize;
}

STDMETHODIMP ZapWriter::Write(void const *pv, ULONG cb, ULONG *pcbWritten)
{
    HRESULT hr = S_OK;

    EX_TRY
    {
        Write((PVOID)pv, cb);

        if (pcbWritten != 0)
            *pcbWritten = cb;
    }
    EX_CATCH_HRESULT(hr)

    return hr;
}

//---------------------------------------------------------------------------------------
// NT Headers

void ZapWriter::SaveHeaders()
{
    SaveDosHeader();
    SaveSignature();
    SaveFileHeader();
    SaveOptionalHeader();
    SaveSections();
}

void ZapWriter::SaveDosHeader()
{
    IMAGE_DOS_HEADER header;

    ZeroMemory(&header, sizeof(header));

    header.e_magic = VAL16(IMAGE_DOS_SIGNATURE);
    header.e_lfanew = VAL32(sizeof(IMAGE_DOS_HEADER));

    // Legacy tools depend on e_lfarlc to be 0x40
    header.e_lfarlc = VAL16(0x40);

    // We put the PE Signature at 0x80 so that we are the same offset for IL Images

    header.e_lfanew = VAL32(sizeof(IMAGE_DOS_HEADER) + 0x40);
    Write(&header, sizeof(header));

    // Write out padding to get to offset 0x80
    WritePad(0x40);
}

void ZapWriter::SaveSignature()
{
    ULONG Signature = VAL32(IMAGE_NT_SIGNATURE);
    Write(&Signature, sizeof(Signature));
}

void ZapWriter::SaveFileHeader()
{
    IMAGE_FILE_HEADER fileHeader;
    ZeroMemory(&fileHeader, sizeof(fileHeader));

    fileHeader.Machine = VAL16(GetMachine());
    fileHeader.TimeDateStamp = VAL32(m_dwTimeDateStamp);
    fileHeader.SizeOfOptionalHeader = Is64Bit() ? VAL16(sizeof(IMAGE_OPTIONAL_HEADER64)) : VAL16(sizeof(IMAGE_OPTIONAL_HEADER32));

    // Count the number of non-empty physical sections
    int nSections = 0;
    for (COUNT_T iPhysicalSection = 0; iPhysicalSection < m_Sections.GetCount(); iPhysicalSection++)
    {
        if (m_Sections[iPhysicalSection]->m_dwSize != 0)
            nSections++;
    }
    fileHeader.NumberOfSections = VAL16(nSections);

    fileHeader.Characteristics = VAL16(IMAGE_FILE_EXECUTABLE_IMAGE |
        (Is64Bit() ? 0 : IMAGE_FILE_32BIT_MACHINE) |
        (m_isDll ? IMAGE_FILE_DLL : 0) |
        (Is64Bit() ? IMAGE_FILE_LARGE_ADDRESS_AWARE : 0) );

    Write(&fileHeader, sizeof(fileHeader));
}

void ZapWriter::SaveOptionalHeader()
{
    // Write the correct flavor of the optional header
    union
    {
        IMAGE_OPTIONAL_HEADER32 header32;
        IMAGE_OPTIONAL_HEADER64 header64;
    }
    optionalHeader;

    ZeroMemory(&optionalHeader, sizeof(optionalHeader));

    PIMAGE_OPTIONAL_HEADER pHeader = (PIMAGE_OPTIONAL_HEADER)&optionalHeader;

    // Common fields between 32-bit and 64-bit

    // Linker version should be consistent with current VC level
    pHeader->MajorLinkerVersion = 11;

    pHeader->SectionAlignment = VAL32(SECTION_ALIGNMENT);
    pHeader->FileAlignment = VAL32(m_FileAlignment);

    // Win2k = 5.0 for 32-bit images, Win2003 = 5.2 for 64-bit images
    pHeader->MajorOperatingSystemVersion = VAL16(5);
    pHeader->MinorOperatingSystemVersion = Is64Bit() ? VAL16(2) : VAL16(0);

    pHeader->MajorSubsystemVersion = pHeader->MajorOperatingSystemVersion;
    pHeader->MinorSubsystemVersion = pHeader->MinorOperatingSystemVersion;

#ifdef REDHAWK
    pHeader->AddressOfEntryPoint = m_entryPointRVA;
#endif

    ZapPhysicalSection * pLastPhysicalSection = m_Sections[m_Sections.GetCount() - 1];
    pHeader->SizeOfImage = VAL32(AlignUp(pLastPhysicalSection->GetRVA() + pLastPhysicalSection->m_dwSize, SECTION_ALIGNMENT));

    pHeader->SizeOfHeaders = VAL32(AlignUp(GetSizeOfNTHeaders(), m_FileAlignment));

    pHeader->Subsystem = VAL16(m_Subsystem);
    pHeader->DllCharacteristics = VAL16(m_DllCharacteristics);


    // Different fields between 32-bit and 64-bit

    PIMAGE_DATA_DIRECTORY pDataDirectory;

    if (Is64Bit())
    {
        PIMAGE_OPTIONAL_HEADER64 pHeader64 = (PIMAGE_OPTIONAL_HEADER64)pHeader;

        pHeader64->Magic = VAL16(IMAGE_NT_OPTIONAL_HDR64_MAGIC);

        pHeader64->ImageBase             = VAL64(m_BaseAddress);
        pHeader64->NumberOfRvaAndSizes   = VAL32(IMAGE_NUMBEROF_DIRECTORY_ENTRIES);

        pHeader64->SizeOfStackReserve = VAL64(m_SizeOfStackReserve);
        pHeader64->SizeOfStackCommit = VAL64(m_SizeOfStackCommit);

        pDataDirectory = pHeader64->DataDirectory;
    }
    else
    {
        PIMAGE_OPTIONAL_HEADER32 pHeader32 = (PIMAGE_OPTIONAL_HEADER32)pHeader;

        pHeader32->Magic = VAL16(IMAGE_NT_OPTIONAL_HDR32_MAGIC);

        pHeader32->ImageBase             = VAL32((ULONG)m_BaseAddress);
        pHeader32->NumberOfRvaAndSizes   = VAL32(IMAGE_NUMBEROF_DIRECTORY_ENTRIES);

        pHeader32->SizeOfStackReserve = VAL32((ULONG)m_SizeOfStackReserve);
        pHeader32->SizeOfStackCommit = VAL32((ULONG)m_SizeOfStackCommit);

        pDataDirectory = pHeader32->DataDirectory;
    }

    for (int i = 0; i < IMAGE_NUMBEROF_DIRECTORY_ENTRIES; i++)
    {
        SetDirectoryData(&pDataDirectory[i], m_DirectoryEntries[i]);
    }

    Write(&optionalHeader, Is64Bit() ? sizeof(IMAGE_OPTIONAL_HEADER64) : sizeof(IMAGE_OPTIONAL_HEADER32));
}

void ZapWriter::SaveSections()
{
    for (COUNT_T iPhysicalSection = 0; iPhysicalSection < m_Sections.GetCount(); iPhysicalSection++)
    {
        ZapPhysicalSection * pPhysicalSection = m_Sections[iPhysicalSection];

        // Do not save empty sections
        if (pPhysicalSection->m_dwSize == 0)
            continue;

        IMAGE_SECTION_HEADER header;
        ZeroMemory(&header, sizeof(header));

        SIZE_T cbName = strlen(pPhysicalSection->m_pszName);
        _ASSERTE(cbName <= sizeof(header.Name));
        memcpy(header.Name, pPhysicalSection->m_pszName, min(sizeof(header.Name), cbName));

        header.Misc.VirtualSize = VAL32(pPhysicalSection->m_dwSize);
        header.VirtualAddress = VAL32(pPhysicalSection->GetRVA());

        header.SizeOfRawData = VAL32(AlignUp(pPhysicalSection->m_dwSizeOfRawData, m_FileAlignment));

        if (header.SizeOfRawData != 0)
            header.PointerToRawData = VAL32(pPhysicalSection->m_dwFilePos);

        header.Characteristics = VAL32(pPhysicalSection->m_dwCharacteristics);

        Write(&header, sizeof(header));
    }
}

DWORD ZapWriter::GetSizeOfNTHeaders()
{
    return  sizeof(IMAGE_DOS_HEADER) + 0x40 + /* Padding for DOS Header */
            sizeof(ULONG) +
            sizeof(IMAGE_FILE_HEADER) +
            (Is64Bit() ? sizeof(IMAGE_OPTIONAL_HEADER64) : sizeof(IMAGE_OPTIONAL_HEADER32)) +
            (m_Sections.GetCount() * sizeof(IMAGE_SECTION_HEADER));
}

void ZapWriter::SetDirectoryData(IMAGE_DATA_DIRECTORY * pDir, ZapNode * pZapNode)
{
    DWORD size = (pZapNode != NULL) ? pZapNode->GetSize() : 0;

    if (size != 0)
    {
        pDir->VirtualAddress = pZapNode->GetRVA();
        pDir->Size = size;
    }
    else
    {
        pDir->VirtualAddress = 0;
        pDir->Size = 0;
    }
}

//---------------------------------------------------------------------------------------
// ZapBlob

ZapBlob * ZapBlob::NewBlob(ZapWriter * pWriter, PVOID pData, SIZE_T cbSize)
{
    S_SIZE_T cbAllocSize = S_SIZE_T(sizeof(ZapBlob)) + S_SIZE_T(cbSize);
    if(cbAllocSize.IsOverflow())
        ThrowHR(COR_E_OVERFLOW);

    void * pMemory = new (pWriter->GetHeap()) BYTE[cbAllocSize.Value()];

    ZapBlob * pZapBlob = new (pMemory) ZapBlob(cbSize);

    if (pData != NULL)
        memcpy((void*)(pZapBlob + 1), pData, cbSize);

    return pZapBlob;
}

template <UINT alignment>
class ZapAlignedBlobConst : public ZapBlob
{
protected:
    ZapAlignedBlobConst(SIZE_T cbSize)
        : ZapBlob(cbSize)
    {
    }

public:
    virtual UINT GetAlignment()
    {
        return alignment;
    }

    static ZapBlob * NewBlob(ZapWriter * pWriter, PVOID pData, SIZE_T cbSize)
    {
        S_SIZE_T cbAllocSize = S_SIZE_T(sizeof(ZapAlignedBlobConst<alignment>)) + S_SIZE_T(cbSize);
        if(cbAllocSize.IsOverflow())
            ThrowHR(COR_E_OVERFLOW);

        void * pMemory = new (pWriter->GetHeap()) BYTE[cbAllocSize.Value()];

        ZapAlignedBlobConst<alignment> * pZapBlob = new (pMemory) ZapAlignedBlobConst<alignment>(cbSize);

        if (pData != NULL)
            memcpy((void *)(pZapBlob + 1), pData, cbSize);

        return pZapBlob;
    }
};

ZapBlob * ZapBlob::NewAlignedBlob(ZapWriter * pWriter, PVOID pData, SIZE_T cbSize, SIZE_T cbAlignment)
{
    switch (cbAlignment)
    {
    case 4:
        return ZapAlignedBlobConst<4>::NewBlob(pWriter, pData, cbSize);
    case 8:
        return ZapAlignedBlobConst<8>::NewBlob(pWriter, pData, cbSize);
    case 16:
        return ZapAlignedBlobConst<16>::NewBlob(pWriter, pData, cbSize);

    default:
        _ASSERTE(!"Requested alignment not supported");
        return NULL;
    }
}

void ZapBlob::Save(ZapWriter * pZapWriter)
{
    pZapWriter->Write(GetData(), GetSize());
}
