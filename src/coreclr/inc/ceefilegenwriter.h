// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// ===========================================================================
// File: CeeFileGenWriter.h
//
// ===========================================================================

#ifndef _CEEFILEGENWRITER_H_
#define _CEEFILEGENWRITER_H_
//

// CeeFileGenWriter contains all the code necessary to actually write an exe
// while CCeeGen contains everything else. This lets CeeGen.exe and the VM
// share more code without forcing the VM to carry the extra code to write an
// exe.
#include <windef.h>
#include "ceegen.h"
#include "iceefilegen.h"

class PEWriter;
class CeeFileGenWriter;

// default setting for PE file
const UINT32 CEE_IMAGE_BASE_32 =              0x00400000;
const UINT64 CEE_IMAGE_BASE_64 = UI64(0x0000000140000000);
const int CEE_IMAGE_SUBSYSTEM_MAJOR_VERSION = 4;
const int CEE_IMAGE_SUBSYSTEM_MINOR_VERSION = 0;

class CeeFileGenWriter : public CCeeGen
{
    mdToken m_entryPoint;       // token for entry point
    DWORD   m_comImageFlags;

    LPWSTR m_outputFileName;
    LPWSTR m_resourceFileName;
    bool   m_dllSwitch;

    ULONG m_iatOffset;

    DWORD m_dwManifestRVA;
    DWORD m_dwManifestSize;

    DWORD m_dwStrongNameRVA;
    DWORD m_dwStrongNameSize;

    DWORD m_dwVTableRVA;
    DWORD m_dwVTableSize;

    bool m_linked;
    bool m_fixed;

    HRESULT checkForErrors();

    struct IDataDllInfo {
        const char *m_name;
        int m_numMethods;
        const char **m_methodName;
        int m_iltOffset;
        int m_ibnOffset;
        int m_iatOffset;
        int m_nameOffset;
    } *m_iDataDlls;
    int m_dllCount;

    CeeSection *m_iDataSectionIAT;
    int m_iDataOffsetIAT;
    char *m_iDataIAT;

    HRESULT allocateIAT();
public:
    // Create with one of these two methods, not operator new
    static HRESULT CreateNewInstance(CCeeGen *pCeeFileGenFrom, CeeFileGenWriter* & pGenWriter,
                                        DWORD createFlags = ICEE_CREATE_FILE_PURE_IL);
    // See ICeeFileGen.h for the definition of the bits used in createFlags
    static HRESULT CreateNewInstanceEx(CCeeGen *pCeeFileGenFrom, CeeFileGenWriter* & pGenWriter,
                                        DWORD createFlags, LPCWSTR seedFileName = NULL);

    virtual HRESULT Cleanup();

    PEWriter &getPEWriter();

    HRESULT link();     // Layout the sections and assign their starting addresses
    HRESULT fixup();    // Apply relocations to any pointer data. Also generate PE base relocs
    HRESULT generateImage(void **ppImage);

    HRESULT setImageBase(size_t imageBase);
    HRESULT setImageBase64(ULONGLONG imageBase);
    HRESULT setFileAlignment(ULONG fileAlignment);
    HRESULT setSubsystem(DWORD subsystem, DWORD major, DWORD minor);

    HRESULT getMethodRVA(ULONG codeOffset, ULONG *codeRVA);

    HRESULT setEntryPoint(mdMethodDef method);
    mdMethodDef getEntryPoint();

    HRESULT setComImageFlags(DWORD mask);
    HRESULT clearComImageFlags(DWORD mask);
    DWORD getComImageFlags();

    HRESULT setOutputFileName(_In_ LPWSTR outputFileName);
    LPWSTR getOutputFileName();

    HRESULT setResourceFileName(_In_ LPWSTR resourceFileName);
    LPWSTR getResourceFileName();

    HRESULT setDirectoryEntry(CeeSection &section, ULONG entry, ULONG size, ULONG offset=0);
    HRESULT computeSectionOffset(CeeSection &section, _In_ char *ptr,
                                 unsigned *offset);
    HRESULT computeOffset(_In_ char *ptr, CeeSection **pSection,
                          unsigned *offset);
    HRESULT getCorHeader(IMAGE_COR20_HEADER **ppHeader);

    HRESULT getFileTimeStamp(DWORD *pTimeStamp);

    HRESULT setLibraryGuid(_In_ LPWSTR libraryGuid);

    HRESULT setDllSwitch(bool dllSwitch);
    bool getDllSwitch();
    HRESULT setManifestEntry(ULONG size, ULONG offset);
    HRESULT setStrongNameEntry(ULONG size, ULONG offset);
    HRESULT setVTableEntry(ULONG size, ULONG offset);
    HRESULT setVTableEntry64(ULONG size, void* ptr);

protected:
    CeeFileGenWriter(); // ctor is protected

    HRESULT emitResourceSection();
    HRESULT emitExeMain();

    HRESULT setAddrReloc(UCHAR *instrAddr, DWORD value);
    HRESULT addAddrReloc(CeeSection &thisSection, UCHAR *instrAddr, DWORD offset, CeeSection *targetSection);

    HRESULT MapTokens(CeeGenTokenMapper *pMapper, IMetaDataImport *pImport);
    HRESULT MapTokensForMethod(CeeGenTokenMapper *pMapper,BYTE *pCode, LPCWSTR szMethodName);
};


inline PEWriter &CeeFileGenWriter::getPEWriter()
{
    return (PEWriter &) *m_peSectionMan;
}

inline LPWSTR CeeFileGenWriter::getOutputFileName() {
    return m_outputFileName;
}

inline LPWSTR CeeFileGenWriter::getResourceFileName() {
    return m_resourceFileName;
}

inline HRESULT CeeFileGenWriter::setDllSwitch(bool dllSwitch) {
    m_dllSwitch = dllSwitch;
    return S_OK;
}

inline bool CeeFileGenWriter::getDllSwitch() {
    return m_dllSwitch;
}

inline mdMethodDef CeeFileGenWriter::getEntryPoint() {
    return m_entryPoint;
}

inline HRESULT CeeFileGenWriter::setEntryPoint(mdMethodDef method) {
    m_entryPoint = method;
    return S_OK;
}

inline HRESULT CeeFileGenWriter::setComImageFlags(DWORD mask) {
    m_comImageFlags |= mask; return S_OK;
}

inline HRESULT CeeFileGenWriter::clearComImageFlags(DWORD mask) {
    m_comImageFlags &= ~mask; return S_OK;
}

inline DWORD CeeFileGenWriter::getComImageFlags() {
    return m_comImageFlags;
}


//
#if defined(_IMAGE_FILE_4K_SECTION_ALIGNMENT_)
#define IMAGE_NT_OPTIONAL_HDR_SECTION_ALIGNMENT 0x1000
#else
#define IMAGE_NT_OPTIONAL_HDR_SECTION_ALIGNMENT 0x2000
#endif

// The stub is always x86 so we always mark the image as x86
#define IMAGE_FILE_MACHINE IMAGE_FILE_MACHINE_I386


#endif	// _CEEFILEGENWRITER_H_

