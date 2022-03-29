// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// PESectionMan implementation
//


#include "stdafx.h"

/*****************************************************************/
HRESULT PESectionMan::Init()
{
    const int initNumSections = 16;
    sectStart = new (nothrow) PESection*[initNumSections];
    if (!sectStart)
        return E_OUTOFMEMORY;
    sectCur = sectStart;
    sectEnd = &sectStart[initNumSections];

    return S_OK;
}

/*****************************************************************/
HRESULT PESectionMan::Cleanup()
{
    for (PESection** ptr = sectStart; ptr < sectCur; ptr++)
        delete *ptr;
    delete [] sectStart;

    return S_OK;
}

/*****************************************************************/
// <REVISIT_TODO>this class is located in it's own DLL (MsCorXvt.dll)
// Since DLL allocates, The DLL must delete; we can't simply delete from
// the client (This is a bug in VC, see knowledge base Q122675)</REVISIT_TODO>
void PESectionMan::sectionDestroy(PESection **section)
{
    // check if this section is referenced in other sections' relocs
    for(PESection** ptr = sectStart; ptr < sectCur; ptr++)
    {
        if(ptr != section)
        {
            for(PESectionReloc* cur = (*ptr)->m_relocStart; cur < (*ptr)->m_relocCur; cur++)
            {
                if(cur->section == *section) // here it is! Delete the reference
                {
                    for(PESectionReloc* tmp = cur; tmp < (*ptr)->m_relocCur; tmp++)
                    {
                        memcpy(tmp,(tmp+1),sizeof(PESectionReloc));
                    }
                    (*ptr)->m_relocCur--;
                    cur--; // no position shift this time
                }
            }
        }
    }
    delete *section;
    *section = NULL;
}
/*****************************************************************/

/******************************************************************/
// Apply the relocs for all the sections
// Called by: ClassConverter after loading up during an in-memory conversion,

HRESULT PESectionMan::applyRelocs(CeeGenTokenMapper *pTokenMapper)
{
    HRESULT hr;

    // Cycle through each of the sections
    for(PESection ** ppCurSection = sectStart; ppCurSection < sectCur; ppCurSection++) {
        IfFailRet((*ppCurSection)->applyRelocs(pTokenMapper));
    } // End sections
    return S_OK;
}


/*****************************************************************/
PESection* PESectionMan::getSection(const char* name)
{
    int     len = (int)strlen(name);

    // the section name can be at most 8 characters including the null.
    if (len < 8)
        len++;
    else
        len = 8;

    // dbPrintf(("looking for section %s\n", name));
    for(PESection** cur = sectStart; cur < sectCur; cur++) {
        // dbPrintf(("searching section %s\n", (*cur)->m_ame));
        if (strncmp((*cur)->m_name, name, len) == 0) {
            // dbPrintf(("found section %s\n", (*cur)->m_name));
            return(*cur);
        }
    }
    return(0);
}

/******************************************************************/
HRESULT PESectionMan::getSectionCreate(const char* name, unsigned flags,
                                       PESection **section)
{
    PESection* ret = getSection(name);

    // If there is an existing section with the given name, return that
    if (ret != NULL) {
        *section = ret;
        return(S_OK);
    }

    // Check if there is space for a new section
    if (sectCur >= sectEnd) {
        unsigned curLen = (unsigned)(sectCur-sectStart);
        unsigned newLen = (curLen * 2) + 1;
        PESection** sectNew = new (nothrow) PESection*[newLen];
        if (sectNew == NULL)
        {
            return E_OUTOFMEMORY;
        }
        memcpy(sectNew, sectStart, sizeof(PESection*)*curLen);
        delete [] sectStart;
        sectStart = sectNew;
        sectCur = &sectStart[curLen];
        sectEnd = &sectStart[newLen];
    }

    HRESULT hr;
    IfFailRet(newSection(name, &ret, flags));

    // dbPrintf(("MAKING NEW %s SECTION data starts at 0x%x\n", name, ret->dataStart));
    *sectCur++ = ret;
    _ASSERTE(sectCur <= sectEnd);
    *section = ret;
    return(S_OK);
}

/******************************************************************/
HRESULT PESectionMan::newSection(const char* name, PESection **section,
                                 unsigned flags, unsigned estSize, unsigned estRelocs)
{
    PESection * ret = new (nothrow) PESection(name, flags, estSize, estRelocs);
    if (ret == NULL)
    {
        return E_OUTOFMEMORY;
    }
    *section = ret;
    return S_OK;
}

//Clone each of our sections.  This will cause a deep copy of the sections
HRESULT PESectionMan::cloneInstance(PESectionMan *destination) {
    _ASSERTE(destination);
    PESection       *pSection;
    PESection       **destPtr;
    HRESULT         hr = NOERROR;

    //Copy each of the sections
    for (PESection** ptr = sectStart; ptr < sectCur; ptr++) {
        destPtr = destination->sectStart;
        pSection = NULL;

        // try to find the matching section by name
        for (; destPtr < destination->sectCur; destPtr++)
        {
            if (strcmp((*destPtr)->m_name, (*ptr)->m_name) == 0)
            {
                pSection = *destPtr;
                break;
            }
        }
        if (destPtr >= destination->sectCur)
        {
            // cannot find a section in the destination with matching name
            // so create one!
            IfFailRet( destination->getSectionCreate((*ptr)->m_name,
                                                     (*ptr)->flags(),
                                                     &pSection) );
        }
        if (pSection)
            IfFailRet( (*ptr)->cloneInstance(pSection) );
    }

    //destination->sectEnd=destination->sectStart + (sectEnd-sectStart);
    return S_OK;
}


//*****************************************************************************
// Implementation for PESection
//*****************************************************************************
PESection::PESection(const char *name, unsigned flags,
                     unsigned estSize, unsigned estRelocs)
{
    dirEntry = -1;

    // No init needed for CBlobFectcher m_pIndex

    m_relocStart = new (nothrow) PESectionReloc[estRelocs];
    if (m_relocStart == NULL)
    {
        // Can't report an error out of here - just initialize
        // as if estRelocs was 0 (all three m_reloc pointers will be NULL).
        // We'll lazily grow as needed.
        estRelocs = 0;
    }
    m_relocCur =  m_relocStart;
    m_relocEnd = &m_relocStart[estRelocs];
    m_header = NULL;
    m_baseRVA = 0;
    m_filePos = 0;
    m_filePad = 0;
    m_flags = flags;

    _ASSERTE(strlen(name)<sizeof(m_name));
    strncpy_s(m_name, sizeof(m_name), name, strlen(name));
}


/******************************************************************/
PESection::~PESection() {
    delete [] m_relocStart;
}


/******************************************************************/
void PESection::writeSectReloc(unsigned val, CeeSection& relativeTo, CeeSectionRelocType reloc, CeeSectionRelocExtra *extra)
{
    addSectReloc(dataLen(), relativeTo, reloc, extra);
    unsigned* ptr = (unsigned*) getBlock(4);
    *ptr = val;
}

/******************************************************************/
HRESULT PESection::addSectReloc(unsigned offset, CeeSection& relativeToIn,
                                CeeSectionRelocType reloc, CeeSectionRelocExtra *extra)
{
    return addSectReloc(offset,
                        (PESection *)&relativeToIn.getImpl(), reloc, extra);
}

/******************************************************************/
HRESULT PESection::addSectReloc(unsigned offset, PESection *relativeTo,
                                CeeSectionRelocType reloc, CeeSectionRelocExtra *extra)
{
    /* dbPrintf(("******** GOT a section reloc for section %s offset 0x%x to section %x offset 0x%x\n",
       header->m_name, offset, relativeTo->m_name, *((unsigned*) dataStart + offset))); */
    _ASSERTE(offset < dataLen());

    if (m_relocCur >= m_relocEnd)  {
        unsigned curLen = (unsigned)(m_relocCur-m_relocStart);
        unsigned newLen = curLen * 2 + 1;
        PESectionReloc* relocNew = new (nothrow) PESectionReloc[newLen];
        if (relocNew == NULL)
        {
            return E_OUTOFMEMORY;
        }

        memcpy(relocNew, m_relocStart, sizeof(PESectionReloc)*curLen);
        delete m_relocStart;
        m_relocStart = relocNew;
        m_relocCur = &m_relocStart[curLen];
        m_relocEnd = &m_relocStart[newLen];
    }

    m_relocCur->type = reloc;
    m_relocCur->offset = offset;
    m_relocCur->section = relativeTo;
    if (extra)
        m_relocCur->extra = *extra;
    m_relocCur++;
    assert(m_relocCur <= m_relocEnd);
    return S_OK;
}

/******************************************************************/
// Compute a pointer (wrap blobfetcher)
char * PESection::computePointer(unsigned offset) const // virtual
{
    return m_blobFetcher.ComputePointer(offset);
}

/******************************************************************/
BOOL PESection::containsPointer(_In_ char *ptr) const // virtual
{
    return m_blobFetcher.ContainsPointer(ptr);
}

/******************************************************************/
// Compute an offset (wrap blobfetcher)
unsigned PESection::computeOffset(_In_ char *ptr) const // virtual
{
    return m_blobFetcher.ComputeOffset(ptr);
}


/******************************************************************/
HRESULT PESection::addBaseReloc(unsigned offset, CeeSectionRelocType reloc,
                                CeeSectionRelocExtra *extra)
{
    HRESULT     hr = E_FAIL;

    // Use for fixing up pointers pointing outside of the module.
    //
    // We only record base relocs for cross module pc-rel pointers
    //

    switch (reloc)
    {
#ifdef HOST_64BIT
    case srRelocDir64Ptr:
#endif
    case srRelocAbsolutePtr:
    case srRelocHighLowPtr:
        // For non pc-rel pointers we don't need to record a section reloc
        hr = S_OK;
        break;

#if defined (TARGET_X86) || defined (TARGET_AMD64)
    case srRelocRelativePtr:
    case srRelocRelative:
        hr = addSectReloc(offset, NULL, reloc, extra);
        break;
#endif

    default:
        _ASSERTE(!"unhandled reloc in PESection::addBaseReloc");
        break;
    }
    return hr;
}

/******************************************************************/
// Dynamic mem allocation, but we can't move old blocks (since others
// have pointers to them), so we need a fancy way to grow
char* PESection::getBlock(unsigned len, unsigned align)
{
    return m_blobFetcher.MakeNewBlock(len, align);
}

unsigned PESection::dataLen()
{
    return m_blobFetcher.GetDataLen();
}

// Apply all the relocs for in memory conversion

// <REVISIT_TODO>@FUTURE: Currently, our VM is rather inefficient in dealing with in-memory RVA.
// @FUTURE: VM is given an index to memory pool and a helper will return the memory pointer given the index.
// @FUTURE: We will consider having the coverter resolve RVAs into addresses.</REVISIT_TODO>

HRESULT PESection::applyRelocs(CeeGenTokenMapper *pTokenMapper)
{
    // For each section, go through each of its relocs
    for(PESectionReloc* pCurReloc = m_relocStart; pCurReloc < m_relocCur; pCurReloc++) {
        if (pCurReloc->type == srRelocMapToken) {
            unsigned * pos = (unsigned*)
                m_blobFetcher.ComputePointer(pCurReloc->offset);
            mdToken newToken;
            PREFIX_ASSUME(pos != NULL);
            if (pTokenMapper->HasTokenMoved(*pos, newToken)) {
                // we have a mapped token
                *pos = newToken;
            }
        }

#if 0
        _ASSERTE(pCurReloc->offset + 4 <= CurSection.m_blobFetcher.GetDataLen());
        unsigned * pAddr = (unsigned *)
            CurSection.m_blobFetcher.ComputePointer(pCurReloc->offset);
        _ASSERTE(pCurReloc->type == srRelocAbsolute);

        // Current contents contain an offset into pCurReloc->section
        // computePointer() is like pCurReloc-section + *pAddr, but for non-linear section
        // This will resolve *pAddr to be a complete address
        *pAddr = (unsigned) pCurReloc->section->computePointer(*pAddr);
#endif

    } // End relocs
    return S_OK;
}

HRESULT PESection::cloneInstance(PESection *destination) {
    PESectionReloc *cur;
    INT32 newSize;
    HRESULT hr = NOERROR;

    _ASSERTE(destination);

    destination->dirEntry = dirEntry;

    //Merge the information currently in the BlobFetcher into
    //out current blob fetcher
    m_blobFetcher.Merge(&(destination->m_blobFetcher));

    //Copy the name.
    strncpy_s(destination->m_name, sizeof(destination->m_name), m_name, sizeof(m_name) - 1);

    //Clone the relocs
    //If the arrays aren't the same size, reallocate as necessary.
    //<REVISIT_TODO>@FUTURE:  Make this a ref-counted structure and don't copy it.</REVISIT_TODO>

    newSize = (INT32)(m_relocCur-m_relocStart);

    if (newSize>(destination->m_relocEnd - destination->m_relocStart)) {
        delete destination->m_relocStart;

        destination->m_relocStart = new (nothrow) PESectionReloc[newSize];
        if (destination->m_relocStart == NULL)
            IfFailGo( E_OUTOFMEMORY );
        destination->m_relocEnd = destination->m_relocStart+(newSize);
    }

    //copy the correct data over into our new array.
    memcpy(destination->m_relocStart, m_relocStart, sizeof(PESectionReloc)*(newSize));
    destination->m_relocCur = destination->m_relocStart + (newSize);
    for (cur=destination->m_relocStart; cur<destination->m_relocCur; cur++) {
        cur->section=destination;
    }
ErrExit:
    return hr;
}

void PESection::SetInitialGrowth(unsigned growth)
{
    m_blobFetcher.SetInitialGrowth(growth);
}
