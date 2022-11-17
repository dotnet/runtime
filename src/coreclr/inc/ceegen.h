// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// ===========================================================================
// File: CEEGEN.H
//
// ===========================================================================

#ifndef _CEEGEN_H_
#define _CEEGEN_H_

#include "cor.h"
#include "iceefilegen.h"
#include "ceegentokenmapper.h"

class CeeSection;
class CeeSectionString;
class CCeeGen;
class PESectionMan;
class PESection;

typedef DWORD StringRef;

#if 0

 This is a description of the current implementation of these types for generating
 CLR modules.

  ICeeGenInternal - interface to generate in-memory CLR module.

  CCeeGen - implementation of ICeeGen. Currently it uses both CeeSections
            as well as PESections (inside PESectionMan), and maintains a
            1:1 relation between them. This is ugly.

  CeeFileGenWriter - Provides functionality to write in-memory module to
                     PE format file. Uses PEWriter (m_pSectionMan) for
                     file-writing functionality

  PEWriter - It can generate a PE format file. It also knows to apply
             pointer relocs when it lays out the PESections.


  ICeeFileGen - Interface used by compilers, ngen, etc, to generate
                a CLR file.
                Has a bunch of methods to emit signatures, tokens, methods,
                etc which are not implemented. These are left over from before

                                                     +----------------------------+
                                                     | ICeeGenInternal            |
                                                     |                            |
                                                     |  COM-style version of      |
                                                     |  ICeeFileGen. HCEEFILE is  |
                                                     |  replaced with "this"      |
        +-------------------------+                  |                            |
        |     CeeSectionImpl      |                  +----------------------------+
        +-------------------------+                                |
           |                  |                                    |
           |                  |                                    v
           |                  v                      +---------------------------+
           |             +------------+              |      CCeeGen              |
           |             |            |              +---------------------------+
           |             | CeeSection |  contains    |                           |
           |             |            |<-------------| CeeSection* m_sections    |
           |             +------------+              |                           |
           |                                        /| PESectionMan m_pSectionMan|
           |                                       / |                           |
           |             +-----------------+      /  +---------------------------+
           v             |   PESectionMan  |<----+                 |
 +-----------+           |                 |  contains             |
 | PESection |           +-----------------+                       |
 |           |  contains | PESection *     |                       v
 |           |<----------|      sectStart, |        +------------------------------+
 +-----------+           |      sectCur,   |        |       CeeFileGenWriter       |
                         |      sectEnd    |        +------------------------------+
                         +-----------------+        | Does meta-data specific      |
                                  |                 | stuff and then dispatches to |
                                  |                 | m_pSectionMan.PEWriter::***()|
                                  |                 |                              |
                                  v                 +------------------------------+
                       +------------------------+                  ^
                       |       PEWriter         |                  |wraps
                       +------------------------+                  |
                       | Low -level file writer |    +----------------------------+
                       | Knows how to do        |    |        ICeeFileGen         |
                       | pointer relocs         |    |                            |
                       |                        |    | C-style interface. Deals    |
                       +------------------------+    | with HCEEFILE, HCEESECTION |
                                                     | etc. It is mostly just a   |
                                                     | thin wrapper for a         |
                                                     | CeeFileGenWriter           |
                                                     +----------------------------+

#endif // 0

// ***** CeeSection classes

class CeeSectionImpl {
  public:
    virtual unsigned dataLen() = 0;
    virtual char * getBlock(
        unsigned len,
        unsigned align = 1) = 0;
    virtual HRESULT addSectReloc(
        unsigned               offset,
        CeeSection &           relativeTo,
        CeeSectionRelocType    reloc = srRelocAbsolute,
        CeeSectionRelocExtra * extra = NULL) = 0;
    virtual HRESULT addBaseReloc(
        unsigned               offset,
        CeeSectionRelocType    reloc = srRelocHighLow,
        CeeSectionRelocExtra * extra = NULL) = 0;
    virtual HRESULT directoryEntry(unsigned num) = 0;
    virtual unsigned char * name() = 0;
    virtual char * computePointer(unsigned offset) const = 0;
    virtual BOOL containsPointer(_In_ char * ptr) const = 0;
    virtual unsigned computeOffset(_In_ char * ptr) const = 0;
    virtual unsigned getBaseRVA() = 0;
    virtual void SetInitialGrowth(unsigned growth) = 0;
};

class CeeSection {
    // m_ceeFile allows inter-section communication
    CCeeGen &m_ceeFile;

    // abstract away implementation to allow inheritance from CeeSection
    CeeSectionImpl &m_impl;

  public:
    enum RelocFlags {
        RELOC_NONE = 0,

        // address should be fixed up to be a RVA not a normal address
        RELOC_RVA = 1
    };

    CeeSection(CCeeGen &ceeFile, CeeSectionImpl &impl)
        : m_ceeFile(ceeFile), m_impl(impl) { LIMITED_METHOD_CONTRACT; }

    virtual ~CeeSection() {LIMITED_METHOD_CONTRACT;  }

    // bytes in this section at present
    unsigned dataLen();

    // section base, after linking
    unsigned getBaseRVA();

    // get a block to write on (use instead of write to avoid copy)
    char* getBlock(unsigned len, unsigned align=1);

    // Indicates that the DWORD at 'offset' in the current section should
    // have the base of section 'relativeTo added to it
    HRESULT addSectReloc(unsigned offset, CeeSection& relativeTo,
                         CeeSectionRelocType = srRelocAbsolute, CeeSectionRelocExtra *extra = 0);
    // Add a base reloc for the given offset in the current section
    virtual HRESULT addBaseReloc(unsigned offset, CeeSectionRelocType reloc = srRelocHighLow, CeeSectionRelocExtra *extra = 0);


    // this section will be directory entry 'num'
    HRESULT directoryEntry(unsigned num);

    // return section name
    unsigned char *name();

    // simulate the base + offset with a more complex data storage
    char * computePointer(unsigned offset) const;
    BOOL containsPointer(_In_ char *ptr) const;
    unsigned computeOffset(_In_ char *ptr) const;

    CeeSectionImpl &getImpl();
    CCeeGen &ceeFile();
    void SetInitialGrowth(unsigned growth);
};

// ***** CCeeGen class
// Only handles in memory stuff
// Base class for CeeFileGenWriter (which actually generates PEFiles)

class CCeeGen : public ICeeGenInternal {
    LONG m_cRefs;
  protected:
    short m_textIdx;            // m_sections[] index for the .text section
    short m_metaIdx;            // m_sections[] index for metadata (.text, or .cormeta for obj files)
    short m_corHdrIdx;          // m_sections[] index for the COM+ header (.text0)
    short m_stringIdx;          // m_sections[] index for strings (.text, or .rdata for EnC)
    short m_ilIdx;              // m_sections[] index for IL (.text)

    CeeGenTokenMapper *m_pTokenMap;
    BOOLEAN m_fTokenMapSupported;   // temporary to support both models
    IMapToken *m_pRemapHandler;

    CeeSection **m_sections;
    short m_numSections;
    short m_allocSections;

    PESectionMan * m_peSectionMan;

    IMAGE_COR20_HEADER *m_corHeader;
    DWORD m_corHeaderOffset;

    HRESULT allocateCorHeader();

    HRESULT addSection(CeeSection *section, short *sectionIdx);

// Init process: Call static CreateNewInstance() , not operator new
  protected:
    HRESULT Init();
    CCeeGen();

  public:

    virtual ~CCeeGen() {}

    static HRESULT CreateNewInstance(CCeeGen* & pCeeFileGen); // call this to instantiate

    virtual HRESULT Cleanup();

    // ICeeGenInternal interfaces

    ULONG STDMETHODCALLTYPE AddRef();
    ULONG STDMETHODCALLTYPE Release();
    STDMETHODIMP QueryInterface(
        REFIID riid,
        void **ppInterface);

    STDMETHODIMP EmitString (
        _In_ LPWSTR lpString,               // [IN] String to emit
        ULONG *RVA);

    STDMETHODIMP GetString (
        ULONG RVA,
        __inout LPWSTR *lpString);

    STDMETHODIMP AllocateMethodBuffer (
        ULONG cchBuffer,                    // [IN] Length of string to emit
        UCHAR **lpBuffer,                   // [OUT] Returned buffer
        ULONG *RVA);

    STDMETHODIMP GetMethodBuffer (
        ULONG RVA,
        UCHAR **lpBuffer);

    STDMETHODIMP GetIMapTokenIface (
        IUnknown **pIMapToken);

    STDMETHODIMP GenerateCeeFile ();

    STDMETHODIMP GetIlSection (
        HCEESECTION *section);

    STDMETHODIMP GetStringSection (
        HCEESECTION *section);

    STDMETHODIMP AddSectionReloc (
        HCEESECTION section,
        ULONG offset,
        HCEESECTION relativeTo,
        CeeSectionRelocType relocType);

    STDMETHODIMP GetSectionCreate (
        const char *name,
        DWORD flags,
        HCEESECTION *section);

    STDMETHODIMP GetSectionDataLen (
        HCEESECTION section,
        ULONG *dataLen);

    STDMETHODIMP GetSectionBlock (
        HCEESECTION section,
        ULONG len,
        ULONG align=1,
        void **ppBytes=0);

   STDMETHODIMP ComputePointer (
        HCEESECTION section,
        ULONG RVA,                          // [IN] RVA for method to return
        UCHAR **lpBuffer);                  // [OUT] Returned buffer


    STDMETHODIMP AddNotificationHandler(IUnknown *pHandler);

    // Write the metadata in "emitter" to the default metadata section is "section" is 0
    // If 'section != 0, it will put the data in 'buffer'.  This
    // buffer is assumed to be in 'section' at 'offset' and of size 'buffLen'
    // (should use GetSaveSize to insure that buffer is big enough
    virtual HRESULT emitMetaData(IMetaDataEmit *emitter,
                        CeeSection* section=0, DWORD offset=0, BYTE* buffer=0, unsigned buffLen=0);
    virtual HRESULT getMethodRVA(ULONG codeOffset, ULONG *codeRVA);

    STDMETHODIMP SetInitialGrowth(DWORD growth);

    CeeSection &getTextSection();
    CeeSection &getMetaSection();
    CeeSection &getCorHeaderSection();
    CeeSectionString &getStringSection();
    CeeSection &getIlSection();

    virtual HRESULT getSectionCreate (const char *name, DWORD flags, CeeSection **section=NULL, short *sectionIdx = NULL);

    PESectionMan* getPESectionMan() {
        LIMITED_METHOD_CONTRACT;
        return m_peSectionMan;
    }

    virtual HRESULT getMapTokenIface(IUnknown **pIMapToken, IMetaDataEmit *emitter=0);

    CeeGenTokenMapper *getTokenMapper() {
        LIMITED_METHOD_CONTRACT;
        return m_pTokenMap;
    }

    virtual HRESULT addNotificationHandler(IUnknown *pHandler);

    //Clone is actually a misnomer here.  This method will copy all of the
    //instance variables and then do a deep copy (as necessary) of the sections.
    //Section data will be appended onto any information already in the section.
    //This is done to support the DynamicIL -> PersistedIL transform.
    virtual HRESULT cloneInstance(CCeeGen *destination);
};

// ***** CeeSection inline methods

inline unsigned CeeSection::dataLen() {
    WRAPPER_NO_CONTRACT;
    return m_impl.dataLen(); }

inline unsigned CeeSection::getBaseRVA() {
    WRAPPER_NO_CONTRACT;
    return m_impl.getBaseRVA(); }

inline char *CeeSection::getBlock(unsigned len, unsigned align) {
    WRAPPER_NO_CONTRACT;
    return m_impl.getBlock(len, align); }

inline HRESULT CeeSection::addSectReloc(
                unsigned offset, CeeSection& relativeTo, CeeSectionRelocType reloc, CeeSectionRelocExtra *extra) {
    WRAPPER_NO_CONTRACT;
    return(m_impl.addSectReloc(offset, relativeTo, reloc, extra));
}

inline HRESULT CeeSection::addBaseReloc(unsigned offset, CeeSectionRelocType reloc, CeeSectionRelocExtra *extra) {
    WRAPPER_NO_CONTRACT;
    return(m_impl.addBaseReloc(offset, reloc, extra));
}


inline HRESULT CeeSection::directoryEntry(unsigned num) {
    WRAPPER_NO_CONTRACT;
    TESTANDRETURN(num < IMAGE_NUMBEROF_DIRECTORY_ENTRIES, E_INVALIDARG);
    m_impl.directoryEntry(num);
    return S_OK;
}

inline CCeeGen &CeeSection::ceeFile() {
    LIMITED_METHOD_CONTRACT;
    return m_ceeFile; }

inline CeeSectionImpl &CeeSection::getImpl() {
    LIMITED_METHOD_CONTRACT;
    return m_impl; }

inline unsigned char *CeeSection::name() {
    WRAPPER_NO_CONTRACT;
    return m_impl.name();
}

inline char * CeeSection::computePointer(unsigned offset) const
{
    WRAPPER_NO_CONTRACT;
    return m_impl.computePointer(offset);
}

inline BOOL CeeSection::containsPointer(_In_ char *ptr) const
{
    WRAPPER_NO_CONTRACT;
    return m_impl.containsPointer(ptr);
}

inline unsigned CeeSection::computeOffset(_In_ char *ptr) const
{
    WRAPPER_NO_CONTRACT;
    return m_impl.computeOffset(ptr);
}

inline void CeeSection::SetInitialGrowth(unsigned growth)
{
    WRAPPER_NO_CONTRACT;
    m_impl.SetInitialGrowth(growth);
}

// ***** CCeeGen inline methods

inline CeeSection &CCeeGen::getTextSection() {
    LIMITED_METHOD_CONTRACT;

    return *m_sections[m_textIdx]; }

inline CeeSection &CCeeGen::getMetaSection() {
    LIMITED_METHOD_CONTRACT;

    return *m_sections[m_metaIdx]; }

inline CeeSection &CCeeGen::getCorHeaderSection() {
    LIMITED_METHOD_CONTRACT;
    _ASSERTE(m_corHdrIdx >= 0);
    return *m_sections[m_corHdrIdx]; }

inline CeeSectionString &CCeeGen::getStringSection() {
    LIMITED_METHOD_CONTRACT;

    return *(CeeSectionString*)m_sections[m_stringIdx]; }

inline CeeSection &CCeeGen::getIlSection() {
    LIMITED_METHOD_CONTRACT;

    return *m_sections[m_ilIdx]; }

#endif
