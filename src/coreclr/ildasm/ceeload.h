// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// ===========================================================================
// File: CEELOAD.H
//

// CEELOAD.H defines the class use to represent the PE file
// ===========================================================================
#ifndef CEELoad_H
#define CEELoad_H

class PELoader;

//
// Used to cache information about sections we're interested in (descr, callsig, il)
//
class SectionInfo
{
public:
    BYTE *  m_pSection;         // pointer to the beginning of the section
    DWORD   m_dwSectionOffset;  // RVA
    DWORD   m_dwSectionSize;

    // init this class's member variables from the provided directory
    void Init(PELoader *pPELoader, IMAGE_DATA_DIRECTORY *dir);

    // returns whether this RVA is inside the section
    BOOL InSection(DWORD dwRVA)
    {
        return (dwRVA >= m_dwSectionOffset) && (dwRVA < m_dwSectionOffset + m_dwSectionSize);
    }
};

class PELoader {
  protected:

    HMODULE m_hMod;
    HANDLE m_hFile;
    HANDLE m_hMapFile;
    BOOL   m_bIsPE32;
    size_t m_FileSize;
    size_t m_FileSizeAligned;

    union
    {
        PIMAGE_NT_HEADERS64	m_pNT64;
        PIMAGE_NT_HEADERS32 m_pNT32;
    };

  public:
    SectionInfo m_DescrSection;
    SectionInfo m_CallSigSection;
    SectionInfo m_ILSection;

    PELoader();
    ~PELoader();
    BOOL open(const char* moduleNameIn);
    BOOL open(const WCHAR* moduleNameIn);
    BOOL open(HMODULE hMod);
    BOOL getCOMHeader(IMAGE_COR20_HEADER **ppCorHeader);
    BOOL getVAforRVA(DWORD rva,void **ppCorHeader);
    void close();
    void dump();
    inline BOOL IsPE32() { return m_bIsPE32; };
    inline PIMAGE_NT_HEADERS32 ntHeaders32() { return m_pNT32; };
    inline PIMAGE_NT_HEADERS64 ntHeaders64() { return m_pNT64; };
    inline PIMAGE_DOS_HEADER dosHeader() { return (PIMAGE_DOS_HEADER)m_hMod; };
    inline PIMAGE_FILE_HEADER coffHeader() { return &(m_pNT32->FileHeader); };
    inline DWORD Signature() { return m_pNT32->Signature; };
    inline BYTE*  base() { return (BYTE*) m_hMod; };
    inline HMODULE getHModule() { return  m_hMod; };
    inline HANDLE getHFile()	{ return  m_hFile; } ;
};

#endif // CEELoad_H
