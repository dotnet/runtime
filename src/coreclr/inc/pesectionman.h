// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// Section Manager for portable executables
// Common to both Memory Only and Static (EXE making) code


#ifndef PESectionMan_H
#define PESectionMan_H

#include "windef.h"

#include "ceegen.h"
#include "blobfetcher.h"

class PESection;
struct PESectionReloc;

struct _IMAGE_SECTION_HEADER;

class PESectionMan
{
public:

    virtual ~PESectionMan() {}

    HRESULT Init();
    HRESULT Cleanup();

    // Finds section with given name, or creates a new one
    HRESULT getSectionCreate(
        const char *name,
        unsigned flags, // IMAGE_SCN_* flags. eg. IMAGE_SCN_CNT_INITIALIZED_DATA
        PESection **section);

    // Since we allocate, we must delete (Bug in VC, see knowledge base Q122675)
    void sectionDestroy(PESection **section);

    // Apply all the relocs for in memory conversion
    HRESULT applyRelocs(CeeGenTokenMapper *pTokenMapper);

    HRESULT cloneInstance(PESectionMan *destination);

protected:

    // Finds section with given name.  returns 0 if not found
    virtual PESection *getSection(const char *name);

    // Create a new section
    virtual HRESULT newSection(
        const char *name,
        PESection **section,
        unsigned    flags     = sdNone,
        unsigned    estSize   = 0x10000,
        unsigned    estRelocs = 1);

    // Keep proctected & no accessors, so that derived class PEWriter
    // is the ONLY one with access

    PESection **sectStart;
    PESection **sectCur;
    PESection **sectEnd;
};  // class PESectionMan

/***************************************************************
 * This represents a section of a ICeeFileGen. Multiple sections
 * can be created with pointers to one another. These will
 * automatically get fixed up when the ICeeFileGen is "baked".
 *
 * It is implemented using CBlobFetcher as a list of blobs.
 * Thus it can grow arbitrarily. At the same time, it can appear
 * as a flat consecutive piece of memory which can be indexed into
 * using offsets.
 */

class PESection : public CeeSectionImpl {
  public:
    // bytes in this section at present
    unsigned dataLen();

    // Apply all the relocs for in memory conversion
    HRESULT applyRelocs(CeeGenTokenMapper *pTokenMapper);

    // get a block to write on (use instead of write to avoid copy)
    char* getBlock(unsigned len, unsigned align=1);

    // writes 'val' (which is offset into section 'relativeTo')
    // and adds a relocation fixup for that section
    void writeSectReloc(unsigned val, CeeSection& relativeTo,
                CeeSectionRelocType reloc = srRelocHighLow,
                CeeSectionRelocExtra *extra=0);

    // Indicates that the DWORD at 'offset' in the current section should
    // have the base of section 'relativeTo' added to it
    HRESULT addSectReloc(unsigned offset, CeeSection& relativeTo,
                            CeeSectionRelocType reloc = srRelocHighLow,
                            CeeSectionRelocExtra *extra=0);

    // If relativeTo is NULL, it is treated as a base reloc.
    // ie. the value only needs to be fixed at load time if the module gets rebased.
    HRESULT addSectReloc(unsigned offset, PESection *relativeTo,
                            CeeSectionRelocType reloc = srRelocHighLow,
                            CeeSectionRelocExtra *extra=0);

    // Add a base reloc for the given offset in the current section
    HRESULT addBaseReloc(unsigned offset, CeeSectionRelocType reloc = srRelocHighLow,
                            CeeSectionRelocExtra *extra = 0);

    // section name
    unsigned char *name() {
        LIMITED_METHOD_CONTRACT;
        return (unsigned char *) m_name;
    }

    // section flags
    unsigned flags() {
        LIMITED_METHOD_CONTRACT;
        return m_flags;
    }

    // virtual base
    unsigned getBaseRVA() {
        LIMITED_METHOD_CONTRACT;
        return m_baseRVA;
    }

    // return the dir entry for this section
    int getDirEntry() {
        LIMITED_METHOD_CONTRACT;
        return dirEntry;
    }
    // this section will be directory entry 'num'
    HRESULT directoryEntry(unsigned num);

    // Indexes offset as if this were an array
    // Returns a pointer into the correct blob
    virtual char * computePointer(unsigned offset) const;

    // Checks to see if pointer is in section
    virtual BOOL containsPointer(_In_ char *ptr) const;

    // Given a pointer pointing into this section,
    // computes an offset as if this were an array
    virtual unsigned computeOffset(_In_ char *ptr) const;

    // Make 'destination' a copy of the current PESection
    HRESULT cloneInstance(PESection *destination);

    // Cause the section to allocate memory in smaller chunks
    void SetInitialGrowth(unsigned growth);

    virtual ~PESection();
private:

    // purposely not defined,
    PESection();

    // purposely not defined,
    PESection(const PESection&);

    // purposely not defined,
    PESection& operator=(const PESection& x);

    // this dir entry points to this section
    int dirEntry;

protected:
    friend class PEWriter;
    friend class PEWriterSection;
    friend class PESectionMan;

    PESection(const char* name, unsigned flags,
              unsigned estSize, unsigned estRelocs);

    // Blob fetcher handles getBlock() and fetching binary chunks.
    CBlobFetcher m_blobFetcher;

    PESectionReloc* m_relocStart;
    PESectionReloc* m_relocCur;
    PESectionReloc* m_relocEnd;

    // These will be set while baking (finalizing) the file
    unsigned    m_baseRVA;      // RVA into the file of this section.
    unsigned    m_filePos;      // Start offset into the file (treated as a data image)
    unsigned    m_filePad;      // Padding added to the end of the section for alignment

    char        m_name[8+6];    // extra room for digits
    unsigned    m_flags;

    struct _IMAGE_SECTION_HEADER* m_header; // Corresponding header. Assigned after link()
};

/***************************************************************/
/* implementation section */

inline HRESULT PESection::directoryEntry(unsigned num) {
    WRAPPER_NO_CONTRACT;
    TESTANDRETURN(num < 16, E_INVALIDARG);
    dirEntry = num;
    return S_OK;
}

// This remembers the location where a reloc needs to be applied.
// It is relative to the contents of a PESection

struct PESectionReloc {
    CeeSectionRelocType     type;       // type of reloc
    unsigned                offset;     // offset within the current PESection where the reloc is to be applied
    CeeSectionRelocExtra    extra;
    PESection*              section;    // target PESection. NULL implies that the target is a fixed address outside the module
};

#endif // #define PESectionMan_H
