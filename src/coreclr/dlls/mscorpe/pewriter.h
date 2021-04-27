// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#ifndef PEWriter_H
#define PEWriter_H

#include <crtwrap.h>

#include <windows.h>

#include "ceegen.h"

#include "pesectionman.h"

class PEWriter;
class PEWriterSection;
class PEDecoder;
struct entry;
struct _IMAGE_SECTION_HEADER;

#define SUBSECTION_ALIGN 16

/***************************************************************/
// PEWriter is derived from PESectionManager. While the base class just
// manages the sections, PEWriter can actually write them out.

class PEWriter : public PESectionMan
{
public:

    // See ICeeFileGen.h for definition of createFlags
    HRESULT Init(PESectionMan *pFrom, DWORD createFlags, LPCWSTR seedFileName = NULL);
    HRESULT Cleanup();

    // Finds section with given name.  returns 0 if not found
    virtual PESection* getSection(const char* name);

    // Create a new section
    virtual HRESULT newSection(const char* name, PESection **section,
                        unsigned flags=sdNone, unsigned estSize=0x10000,
                        unsigned estRelocs=0x100);

    HRESULT link();
    HRESULT fixup(CeeGenTokenMapper *pMapper);
    HRESULT write(__in LPCWSTR fileName);
    HRESULT write(void **ppImage);

    // calling these functions is optional
    DWORD      getSectionAlignment();
    void       setSectionAlignment(DWORD);
    DWORD      getFileAlignment();
    void       setFileAlignment(DWORD);
    DWORD      getImageBase32();
    void       setImageBase32(DWORD);
    UINT64     getImageBase64();
    void       setImageBase64(UINT64);
    void       stripRelocations(bool val);        // default = false

    void getHeaderInfo(PIMAGE_NT_HEADERS *ppNtHeaders, PIMAGE_SECTION_HEADER *ppSections, ULONG *pNumSections);

    // these affect the charactertics in the NT file header
    void setCharacteristics(unsigned mask);
    void clearCharacteristics(unsigned mask);

    // these affect the charactertics in the NT optional header
    void setDllCharacteristics(unsigned mask);
    void clearDllCharacteristics(unsigned mask);

    // sets the SubSystem field in the optional header
    void setSubsystem(unsigned subsystem, unsigned major, unsigned minor);

    // specify the entry point as an offset into the text section. The
    // method will convert into an RVA from the base
    void setEntryPointTextOffset(unsigned entryPoint);

    HRESULT setDirectoryEntry(PEWriterSection *section, ULONG entry, ULONG size, ULONG offset=0);


    // get the RVA for the IL section
    ULONG getIlRva();

    // set the RVA for the IL section by supplying offset to the IL section
    void setIlRva(DWORD offset);

    unsigned getSubsystem();

    size_t getImageBase();

    HRESULT getFileTimeStamp(DWORD *pTimeStamp);

    IMAGE_NT_HEADERS32* ntHeaders32()    { return (IMAGE_NT_HEADERS32*) m_ntHeaders; }
    IMAGE_NT_HEADERS64* ntHeaders64()    { return (IMAGE_NT_HEADERS64*) m_ntHeaders; }

    bool isPE32()  // true  -> created a PE  32-bit PE file
                   // false -> created a PE+ 64-bit PE file
    { return (m_ntHeaders->OptionalHeader.Magic == VAL16(IMAGE_NT_OPTIONAL_HDR32_MAGIC)); }

    bool isI386()  // true  -> target machine is i386
    { return (m_ntHeaders->FileHeader.Machine == VAL16(IMAGE_FILE_MACHINE_I386)); }

    bool isIA64()  // true  -> target machine is ia64
    { return (m_ntHeaders->FileHeader.Machine == VAL16(IMAGE_FILE_MACHINE_IA64)); }

    bool isAMD64()  // true  -> target machine is ia64
    { return (m_ntHeaders->FileHeader.Machine == VAL16(IMAGE_FILE_MACHINE_AMD64)); }

    bool isARM()  // true  -> target machine is ARM
    { return (m_ntHeaders->FileHeader.Machine == VAL16(IMAGE_FILE_MACHINE_ARMNT)); }

    bool createCorMainStub() // do we need the CorExeMain/CorDllMain stubs?
    { return m_createCorMainStub; }

private:
    DWORD  m_ilRVA;
    ULONG  m_dataRvaBase;
    ULONG  m_rdataRvaBase;
    ULONG  m_codeRvaBase;
    DWORD  m_peFileTimeStamp;

    HANDLE   m_file;

    // "Seed" file information. The new file data will be "appended" to the seed file
    // These are valid only if m_hSeedFile is valid

    HANDLE              m_hSeedFile;
    HANDLE              m_hSeedFileMap;
    PEDecoder *         m_pSeedFileDecoder;
    IMAGE_NT_HEADERS *  m_pSeedFileNTHeaders;
    unsigned            m_iSeedSections;
    IMAGE_SECTION_HEADER*m_pSeedSections;
    IMAGE_SECTION_HEADER*m_pSeedSectionToAdd; // used only by newSection()

    PEWriterSection **getSectStart() {
        return (PEWriterSection**)sectStart;
    }

    PEWriterSection **getSectCur() {
        return (PEWriterSection**)sectCur;
    }

    COUNT_T getSectCount() {
        return COUNT_T(sectCur - sectStart);
    }


    IMAGE_DOS_HEADER    m_dosHeader;
    IMAGE_NT_HEADERS *  m_ntHeaders;
    DWORD               m_ntHeadersSize; // Size of IMAGE_NT_HEADERS (not including section headers)

    unsigned            virtualPos;
    unsigned            filePos;

    PEWriterSection *   reloc;
    PEWriterSection *   strtab;

    IMAGE_SECTION_HEADER *headers, *headersEnd;

    struct directoryEntry {
        PEWriterSection *section;
        ULONG offset;
        ULONG size;
    };

    // Directory entries in the file header
    directoryEntry  *   pEntries;
    USHORT              cEntries;

    bool   m_createCorMainStub;

    // Helpers for link()
    HRESULT linkSortSections(entry * entries,
                             unsigned * piEntries, // OUT
                             unsigned * piUniqueSections); // OUT
    HRESULT linkSortHeaders(entry * entries, unsigned iEntries, unsigned iUniqueSections);
    HRESULT linkPlaceSections(entry * entries, unsigned iEntries);
    void setSectionIndex(IMAGE_SECTION_HEADER * h, unsigned sectionIndex);

    HRESULT Open(__in LPCWSTR fileName);
    HRESULT Write(const void *data, int size);
    HRESULT Seek(int offset);
    HRESULT Pad(int align);
    HRESULT Close();
};

// This class encapsulates emitting the base relocs into the
// .reloc section of the PE file, for the case where the image
// does not get loaded at its preferred base address.

class PERelocSection
{
 private:
    PEWriterSection *   section;
    unsigned            relocPage;
    unsigned            relocSize;
    DWORD *             relocSizeAddr;
    unsigned            pages;

#ifdef _DEBUG
    unsigned            lastRVA;
#endif

 public:
    PERelocSection(PEWriterSection *pBaseReloc);
    void AddBaseReloc(unsigned rva, int type, unsigned short highAdj=0);
    void Finish(bool isPE32);
};

class PEWriterSection : public PESection {

public:

    PEWriterSection(const char* name, unsigned flags,
                    unsigned estSize, unsigned estRelocs)
        : PESection(name, flags, estSize, estRelocs)  {}

    virtual HRESULT  applyRelocs(IMAGE_NT_HEADERS *  pNtHeaders,
                         PERelocSection *    relocSection,
                         CeeGenTokenMapper * pTokenMapper,
                         DWORD               rdataRvaBase,
                         DWORD               dataRvaBase,
                         DWORD               textRvaBase);

    virtual HRESULT  write      (HANDLE file);
    virtual unsigned writeMem   (void ** pMem);
    virtual bool     isSeedSection() { return false; }

};

// This is for sections from the seed file. Their order needs to be maintained and
// they need to be written to the output file.

class PESeedSection : public PEWriterSection {

public:

    PESeedSection(PEDecoder * peDecoder, IMAGE_SECTION_HEADER * seedSection);

    // PESection methods

    unsigned dataLen() { return m_pSeedSectionHeader->SizeOfRawData; }
    HRESULT applyRelocs(CeeGenTokenMapper *pTokenMapper) { return S_OK; }
    char* getBlock(unsigned len, unsigned align) { _ASSERTE(!"PESeedSection"); return NULL; }
    HRESULT truncate(unsigned newLen) { _ASSERTE(!"PESeedSection"); return E_FAIL; }
    void writeSectReloc(unsigned val, CeeSection& relativeTo,
                CeeSectionRelocType reloc,
                CeeSectionRelocExtra *extra) { _ASSERTE(!"PESeedSection"); return; }
    HRESULT addSectReloc(unsigned offset, CeeSection& relativeTo,
                            CeeSectionRelocType reloc,
                            CeeSectionRelocExtra *extra) { _ASSERTE(!"PESeedSection"); return E_FAIL; }
    HRESULT addSectReloc(unsigned offset, PESection *relativeTo,
                            CeeSectionRelocType reloc,
                            CeeSectionRelocExtra *extra) { _ASSERTE(!"PESeedSection"); return E_FAIL; }
    HRESULT addBaseReloc(unsigned offset, CeeSectionRelocType reloc,
                            CeeSectionRelocExtra *extra) { _ASSERTE(!"PESeedSection"); return E_FAIL; }
//  unsigned char *name();
//  unsigned flags();
//  unsigned getBaseRVA();
    int getDirEntry() { _ASSERTE(!"PESeedSection"); return 0; }
    HRESULT directoryEntry(unsigned num) { _ASSERTE(!"PESeedSection"); return E_FAIL; }
    char * computePointer(unsigned offset) const { _ASSERTE(!"PESeedSection"); return NULL; }
    BOOL containsPointer(__in char *ptr) const { _ASSERTE(!"PESeedSection"); return FALSE; }
    unsigned computeOffset(__in char *ptr) const { _ASSERTE(!"PESeedSection"); return 0; }
    HRESULT cloneInstance(PESection *destination) { _ASSERTE(!"PESeedSection"); return E_FAIL; }

    // PEWriterSection

    HRESULT applyRelocs(IMAGE_NT_HEADERS *  pNtHeaders,
                        PERelocSection *    relocSection,
                        CeeGenTokenMapper * pTokenMapper,
                        DWORD               rdataRvaBase,
                        DWORD               dataRvaBase,
                        DWORD               textRvaBase) { return S_OK; }

    HRESULT  write(HANDLE file);
    unsigned writeMem(void ** pMem);
    bool isSeedSection() { return true; }

protected:

    PEDecoder *             m_pSeedFileDecoder;
    IMAGE_SECTION_HEADER *  m_pSeedSectionHeader;

};

inline DWORD PEWriter::getSectionAlignment() {
    return VAL32(m_ntHeaders->OptionalHeader.FileAlignment);
}

inline void PEWriter::setSectionAlignment(DWORD SectionAlignment) {

    _ASSERTE(m_hSeedFile == INVALID_HANDLE_VALUE);
    m_ntHeaders->OptionalHeader.SectionAlignment = VAL32(SectionAlignment);
}

inline DWORD PEWriter::getFileAlignment() {
    return m_ntHeaders->OptionalHeader.FileAlignment;
}

inline void PEWriter::setFileAlignment(DWORD fileAlignment) {
    _ASSERTE(m_hSeedFile == INVALID_HANDLE_VALUE);
    m_ntHeaders->OptionalHeader.FileAlignment = VAL32(fileAlignment);
}

inline unsigned PEWriter::getSubsystem() {
    return VAL16(m_ntHeaders->OptionalHeader.Subsystem);
}

inline void PEWriter::setSubsystem(unsigned subsystem, unsigned major, unsigned minor) {
    m_ntHeaders->OptionalHeader.Subsystem = VAL16(subsystem);
    m_ntHeaders->OptionalHeader.MajorSubsystemVersion = VAL16(major);
    m_ntHeaders->OptionalHeader.MinorSubsystemVersion = VAL16(minor);
}

inline void PEWriter::setCharacteristics(unsigned mask) {
    m_ntHeaders->FileHeader.Characteristics |= VAL16(mask);
}

inline void PEWriter::clearCharacteristics(unsigned mask) {
    m_ntHeaders->FileHeader.Characteristics &= VAL16(~mask);
}

inline void PEWriter::setDllCharacteristics(unsigned mask) {
    m_ntHeaders->OptionalHeader.DllCharacteristics |= VAL16(mask);
}

inline void PEWriter::clearDllCharacteristics(unsigned mask) {
    m_ntHeaders->OptionalHeader.DllCharacteristics &= VAL16(~mask);
}

inline void PEWriter::setEntryPointTextOffset(unsigned offset) {
    m_ntHeaders->OptionalHeader.AddressOfEntryPoint = VAL32(offset);
}

#endif
