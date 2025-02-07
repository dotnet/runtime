// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#include "stdafx.h"

#include "blobfetcher.h"
#include "pedecoder.h"

#ifdef _DEBUG
#define LOGGING
#endif

#ifdef LOGGING
#include "log.h"

static const char* const RelocName[] = { "Absolute", "HighLow", "MapToken", "FilePos" };
static const char RelocSpaces[] = "        ";

#endif

    /* This is the stub program that says it can't be run in DOS mode */
    /* it is x86 specific, but so is dos so I suppose that is OK */
static const unsigned char x86StubPgm[] = {
    0x0e, 0x1f, 0xba, 0x0e, 0x00, 0xb4, 0x09, 0xcd, 0x21, 0xb8, 0x01, 0x4c, 0xcd, 0x21, 0x54, 0x68,
    0x69, 0x73, 0x20, 0x70, 0x72, 0x6f, 0x67, 0x72, 0x61, 0x6d, 0x20, 0x63, 0x61, 0x6e, 0x6e, 0x6f,
    0x74, 0x20, 0x62, 0x65, 0x20, 0x72, 0x75, 0x6e, 0x20, 0x69, 0x6e, 0x20, 0x44, 0x4f, 0x53, 0x20,
    0x6d, 0x6f, 0x64, 0x65, 0x2e, 0x0d, 0x0d, 0x0a, 0x24, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };

    /* number of pad bytes to make 'len' bytes align to 'align' */
inline static unsigned roundUp(unsigned len, unsigned align) {
    return((len + align-1) & ~(align-1));
}

inline static unsigned padLen(unsigned len, unsigned align) {
    return(roundUp(len, align) - len);
}

#define COPY_AND_ADVANCE(target, src, size) { \
                            ::memcpy((void *) (target), (const void *) (src), (size)); \
                            (char *&) (target) += (size); }

/******************************************************************/
int __cdecl relocCmp(const void* a_, const void* b_) {

    const PESectionReloc* a = (const PESectionReloc*) a_;
    const PESectionReloc* b = (const PESectionReloc*) b_;
    return (a->offset > b->offset ? 1 : (a->offset == b->offset ? 0 : -1));
}

PERelocSection::PERelocSection(PEWriterSection *pBaseReloc)
{
   section = pBaseReloc;
   relocPage = (unsigned) -1;
   relocSize = 0;
   relocSizeAddr = NULL;
   pages = 0;

#ifdef _DEBUG
   lastRVA = 0;
#endif
}

void PERelocSection::AddBaseReloc(unsigned rva, int type, unsigned short highAdj)
{
#ifdef _DEBUG
    // Guarantee that we're adding relocs in strict increasing order.
    _ASSERTE(rva > lastRVA);
    lastRVA = rva;
#endif

    if (relocPage != (rva & ~0xFFF)) {
        if (relocSizeAddr) {
            if ((relocSize & 1) == 1) {     // pad to an even number
                short *ps = (short*) section->getBlock(2);
                if(ps) {
                    *ps = 0;
                    relocSize++;
                }
            }
            *relocSizeAddr = VAL32(relocSize*2 + sizeof(IMAGE_BASE_RELOCATION));
        }
        IMAGE_BASE_RELOCATION* base = (IMAGE_BASE_RELOCATION*) section->getBlock(sizeof(IMAGE_BASE_RELOCATION));
        if(base) {
            relocPage = (rva & ~0xFFF);
            relocSize = 0;
            base->VirtualAddress = VAL32(relocPage);
            // Size needs to be fixed up when we know it - save address here
            relocSizeAddr = &base->SizeOfBlock;
            pages++;
        }
    }

    relocSize++;
    unsigned short* offset = (unsigned short*) section->getBlock(2);
    if(offset) {
        *offset = VAL16((unsigned short)(rva & 0xFFF) | (unsigned short)(type << 12));
    }
}

void PERelocSection::Finish(bool isPE32)
{
    // fixup the last reloc block (if there was one)
    if (relocSizeAddr) {
        if ((relocSize & 1) == 1) {     // pad to an even number
            short* psh = (short*) section->getBlock(2);
            if(psh)
            {
                *psh = 0;
                relocSize++;
            }
        }
        *relocSizeAddr = VAL32(relocSize*2 + sizeof(IMAGE_BASE_RELOCATION));
    }
}

#define GET_UNALIGNED_INT32(_ptr)     ((INT32) GET_UNALIGNED_VAL32(_ptr))

static inline HRESULT SignedFitsIn31Bits(INT64 immediate)
{
    INT64 hiBits = immediate >> 31;
    if ((hiBits == 0) || (hiBits == -1))
    {
        return S_OK;
    }
    else
    {
        return E_FAIL;
    }
}

static inline HRESULT UnsignedFitsIn32Bits(UINT64 immediate)
{
    UINT64 hiBits = immediate >> 32;
    if (hiBits == 0)
    {
        return S_OK;
    }
    else
    {
        return E_FAIL;
    }
}

static inline HRESULT AddOvf_RVA(DWORD& a, DWORD b)
{
    DWORD r = a + b;
    if (r < a)  // Check for overflow
        return E_FAIL;
    a = r;
    return S_OK;
}

static inline HRESULT AddOvf_S_U32(INT64 & a, unsigned int b)
{
    INT64 r = a + b;
    if (r < a)  // Check for overflow
        return E_FAIL;
    a = r;
    return S_OK;
}

static inline HRESULT AddOvf_S_S32(INT64 & a, int b)
{
    INT64 r = a + b;
    if ( ((r >= a) && (b >= 0)) ||
         ((r <  a) && (b <  0))    )
    {
        a = r;
        return S_OK;
    }
    return E_FAIL;
}

static inline HRESULT AddOvf_U_U32(UINT64 & a, unsigned int b)
{
    UINT64 r = a + b;
    if (r < a)  // Check for overflow
        return E_FAIL;
    a = r;
    return S_OK;
}

static inline HRESULT AddOvf_U_U(UINT64 & a, UINT64 b)
{
    UINT64 r = a + b;
    if (r < a)  // Check for overflow
        return E_FAIL;
    a = r;
    return S_OK;
}

static inline HRESULT SubOvf_S_U32(INT64 & a, unsigned int b)
{
    INT64 r = a - b;
    if (r > a)  // Check for overflow
        return E_FAIL;
    a = r;
    return S_OK;
}

static inline HRESULT SubOvf_S_U(INT64 & a, UINT64 b)
{
    INT64 r = a - b;
    if (r > a)  // Check for overflow
        return E_FAIL;
    a = r;
    return S_OK;
}

static inline HRESULT SubOvf_U_U32(UINT64 & a, unsigned int b)
{
    UINT64 r = a - b;
    if (r > a)  // Check for overflow
        return E_FAIL;
    a = r;
    return S_OK;
}

#ifndef HOST_AMD64
/* subtract two unsigned pointers yielding a signed pointer sized int */
static inline HRESULT SubOvf_U_U(INT64 & r, UINT64 a, UINT64 b)
{
    r = a - b;
    if ( ((a >= b) && (r >= 0))  ||
         ((a <  b) && (r <  0)))
    {
        return S_OK;
    }
    return E_FAIL;
}
#endif


/******************************************************************/
/* apply the relocs for this section.
*/

HRESULT PEWriterSection::applyRelocs(IMAGE_NT_HEADERS  *  pNtHeaders,
                                     PERelocSection    *  pBaseRelocSection,
                                     CeeGenTokenMapper *  pTokenMapper,
                                     DWORD                dataRvaBase,
                                     DWORD                rdataRvaBase,
                                     DWORD                codeRvaBase)
{
    HRESULT hr;

    _ASSERTE(pBaseRelocSection); // need section to write relocs

#ifdef LOGGING
    // Ensure that if someone adds a value to CeeSectionRelocType in cor.h,
    // that they also add an entry to RelocName.
    static_assert_no_msg(ARRAY_SIZE(RelocName) == srRelocSentinel);
#ifdef _DEBUG
    for (unsigned int i = 0; i < srRelocSentinel; i++)
    {
        _ASSERTE(strlen(RelocName[i]) <= strlen(RelocSpaces));
    }
#endif // _DEBUG
#endif // LOGGING

    if (m_relocCur == m_relocStart)
        return S_OK;

    bool isPE32 = (pNtHeaders->OptionalHeader.Magic == VAL16(IMAGE_NT_OPTIONAL_HDR32_MAGIC));

#ifdef LOGGING
    LOG((LF_ZAP, LL_INFO100000,
         "APPLYING section relocs for section %s start RVA = 0x%x\n",
         m_name, m_baseRVA));
#endif

    UINT64 imageBase = isPE32 ? VAL32(((IMAGE_NT_HEADERS32 *) pNtHeaders)->OptionalHeader.ImageBase)
                              : VAL64(((IMAGE_NT_HEADERS64 *) pNtHeaders)->OptionalHeader.ImageBase);

    // sort them to make the baseRelocs pretty
    qsort(m_relocStart, (m_relocCur - m_relocStart), sizeof(PESectionReloc), relocCmp);

    for (PESectionReloc * cur = m_relocStart; cur < m_relocCur; cur++)
    {
        _ASSERTE((cur->offset + 4) <= m_blobFetcher.GetDataLen());

        int    curType      = cur->type;
        DWORD  curOffset    = cur->offset;
        UINT64 targetOffset = 0;
        int    slotNum      = 0;
#ifdef LOGGING
        INT64  oldStarPos;
#endif

        DWORD curRVA = m_baseRVA;    // RVA in the PE image of the reloc site
        IfFailRet(AddOvf_RVA(curRVA, curOffset));
        DWORD UNALIGNED * pos = (DWORD *) m_blobFetcher.ComputePointer(curOffset);

        PREFIX_ASSUME(pos != NULL);

#ifdef LOGGING
        LOG((LF_ZAP, LL_INFO1000000,
             "   Reloc %s%s at %-7s+%04x (RVA=%08x) at" FMT_ADDR,
             &RelocSpaces[strlen(RelocName[curType])],
             m_name, curOffset, curRVA, DBG_ADDR(pos)));

        oldStarPos = GET_UNALIGNED_VAL32(pos);
#endif

        //
        // 'targetOffset' has now been computed. Write out the appropriate value.
        // Record base relocs as necessary.
        //

        int baseReloc = 0;
        INT64 newStarPos = 0; // oldStarPos gets updated to newStarPos

        if (curType == srRelocAbsolute) {

            newStarPos = GET_UNALIGNED_INT32(pos);

            if (rdataRvaBase > 0 && ! strcmp((const char *)(cur->section->m_name), ".rdata"))
                IfFailRet(AddOvf_S_U32(newStarPos, rdataRvaBase));
            else if (dataRvaBase > 0 && ! strcmp((const char *)(cur->section->m_name), ".data"))
                IfFailRet(AddOvf_S_U32(newStarPos, dataRvaBase));
            else
                IfFailRet(AddOvf_S_U32(newStarPos, cur->section->m_baseRVA));

            SET_UNALIGNED_VAL32(pos, newStarPos);
        }
        else if (curType == srRelocMapToken)
        {
            mdToken newToken;
            if (pTokenMapper != NULL && pTokenMapper->HasTokenMoved((mdToken)GET_UNALIGNED_VAL32(pos), newToken)) {
                // we have a mapped token
                SET_UNALIGNED_VAL32(pos, newToken);
            }
            newStarPos = GET_UNALIGNED_VAL32(pos);
        }
        else if (curType == srRelocFilePos)
        {
            newStarPos = GET_UNALIGNED_VAL32(pos);
            IfFailRet(AddOvf_S_U32(newStarPos, cur->section->m_filePos));
            SET_UNALIGNED_VAL32(pos, newStarPos);
        }
        else if (curType == srRelocHighLow)
        {
            if (isPE32)
            {
                // we have a 32-bit value at pos
                UINT64 value = GET_UNALIGNED_VAL32(pos);

                IfFailRet(AddOvf_U_U32(value, cur->section->m_baseRVA));
                IfFailRet(AddOvf_U_U(value, imageBase));

                IfFailRet(UnsignedFitsIn32Bits(value));  // Check for overflow
                SET_UNALIGNED_VAL32(pos, value);

                newStarPos = value;

                baseReloc = IMAGE_REL_BASED_HIGHLOW;
            }
            else
            {
                // we have a 64-bit value at pos
                UINT64 UNALIGNED * p_value = (UINT64 *) pos;
                targetOffset = *p_value;

                // The upper bits of targetOffset must be zero
                IfFailRet(UnsignedFitsIn32Bits(targetOffset));

                IfFailRet(AddOvf_U_U32(targetOffset, cur->section->m_baseRVA));
                IfFailRet(AddOvf_U_U(targetOffset, imageBase));

                *p_value   = targetOffset;
                newStarPos = targetOffset;

                baseReloc =  IMAGE_REL_BASED_DIR64;
            }
        }
        else
        {
            _ASSERTE(!"Unknown Relocation type");
        }

        if (baseReloc != 0)
        {
            pBaseRelocSection->AddBaseReloc(curRVA, baseReloc);
        }

#ifdef LOGGING
        LOG((LF_ZAP, LL_INFO1000000,
             "to %-7s+%04x, old =" FMT_ADDR "new =" FMT_ADDR "%s\n",
             cur->section->m_name, targetOffset,
             DBG_ADDR(oldStarPos), DBG_ADDR(newStarPos),
             baseReloc ? "(BASE RELOC)" : ""));
#endif

    }
    return S_OK;
}

/******************************************************************/
HRESULT PEWriter::Init(PESectionMan *pFrom, DWORD createFlags)
{
    if (pFrom)
        *(PESectionMan*)this = *pFrom;
    else {
        HRESULT hr = PESectionMan::Init();
        if (FAILED(hr))
            return hr;
    }
    time_t now;
    time(&now);

#ifdef LOGGING
    InitializeLogging();
#endif

    // Save the timestamp so that we can give it out if someone needs
    // it.
    m_peFileTimeStamp = (DWORD) now;

    // We must be creating either a PE32 or a PE64 file
    if (createFlags & ICEE_CREATE_FILE_PE64)
    {
        m_ntHeaders     = (IMAGE_NT_HEADERS *) new (nothrow) IMAGE_NT_HEADERS64;
        m_ntHeadersSize = sizeof(IMAGE_NT_HEADERS64);

        if (!m_ntHeaders) return E_OUTOFMEMORY;
        memset(m_ntHeaders, 0, m_ntHeadersSize);

        m_ntHeaders->OptionalHeader.Magic = VAL16(IMAGE_NT_OPTIONAL_HDR64_MAGIC);
        m_ntHeaders->FileHeader.SizeOfOptionalHeader = VAL16(sizeof(IMAGE_OPTIONAL_HEADER64));
    }
    else
    {
        _ASSERTE(createFlags & ICEE_CREATE_FILE_PE32);
        m_ntHeaders     = (IMAGE_NT_HEADERS *) new (nothrow) IMAGE_NT_HEADERS32;
        m_ntHeadersSize = sizeof(IMAGE_NT_HEADERS32);

        if (!m_ntHeaders) return E_OUTOFMEMORY;
        memset(m_ntHeaders, 0, m_ntHeadersSize);

        m_ntHeaders->OptionalHeader.Magic = VAL16(IMAGE_NT_OPTIONAL_HDR32_MAGIC);
        m_ntHeaders->FileHeader.SizeOfOptionalHeader = VAL16(sizeof(IMAGE_OPTIONAL_HEADER32));
    }

    // Record whether we should create the CorExeMain and CorDllMain stubs
    m_createCorMainStub = ((createFlags & ICEE_CREATE_FILE_CORMAIN_STUB) != 0);

    // We must have a valid target machine selected
    if ((createFlags & ICEE_CREATE_MACHINE_MASK) == ICEE_CREATE_MACHINE_I386)
    {
        m_ntHeaders->FileHeader.Machine = VAL16(IMAGE_FILE_MACHINE_I386);
    }
    else if ((createFlags & ICEE_CREATE_MACHINE_MASK) == ICEE_CREATE_MACHINE_AMD64)
    {
        m_ntHeaders->FileHeader.Machine = VAL16(IMAGE_FILE_MACHINE_AMD64);
    }
    else if ((createFlags & ICEE_CREATE_MACHINE_MASK) == ICEE_CREATE_MACHINE_ARM)
    {
        m_ntHeaders->FileHeader.Machine = VAL16(IMAGE_FILE_MACHINE_ARMNT);

        // The OS loader already knows how to initialize pure managed assemblies and we have no legacy OS
        // support to worry about on ARM so don't ever create the stub for ARM binaries.
        m_createCorMainStub = false;
    }
    else if ((createFlags & ICEE_CREATE_MACHINE_MASK) == ICEE_CREATE_MACHINE_ARM64)
    {
        m_ntHeaders->FileHeader.Machine = VAL16(IMAGE_FILE_MACHINE_ARM64);

        // The OS loader already knows how to initialize pure managed assemblies and we have no legacy OS
        // support to worry about on ARM64 so don't ever create the stub for ARM64 binaries.
        m_createCorMainStub = false;
    }
    else
    {
        _ASSERTE(!"Invalid target machine");
    }

    cEntries = IMAGE_DIRECTORY_ENTRY_COM_DESCRIPTOR + 1;
    pEntries = new (nothrow) directoryEntry[cEntries];
    if (pEntries == NULL) return E_OUTOFMEMORY;
    memset(pEntries, 0, sizeof(*pEntries) * cEntries);

    m_ntHeaders->Signature                       = VAL32(IMAGE_NT_SIGNATURE);
    m_ntHeaders->FileHeader.TimeDateStamp        = VAL32((ULONG) now);
    m_ntHeaders->FileHeader.Characteristics      = VAL16(0);

    if (createFlags & ICEE_CREATE_FILE_STRIP_RELOCS)
    {
        m_ntHeaders->FileHeader.Characteristics |= VAL16(IMAGE_FILE_RELOCS_STRIPPED);
    }

    // Linker version should be consistent with current VC level
    m_ntHeaders->OptionalHeader.MajorLinkerVersion  = 11;
    m_ntHeaders->OptionalHeader.MinorLinkerVersion  = 0;

    m_ntHeaders->OptionalHeader.SectionAlignment    = VAL32(IMAGE_NT_OPTIONAL_HDR_SECTION_ALIGNMENT);
    m_ntHeaders->OptionalHeader.FileAlignment       = VAL32(0);
    m_ntHeaders->OptionalHeader.AddressOfEntryPoint = VAL32(0);

    m_ntHeaders->OptionalHeader.MajorOperatingSystemVersion = VAL16(4);
    m_ntHeaders->OptionalHeader.MinorOperatingSystemVersion = VAL16(0);

    m_ntHeaders->OptionalHeader.MajorImageVersion     = VAL16(0);
    m_ntHeaders->OptionalHeader.MinorImageVersion     = VAL16(0);
    m_ntHeaders->OptionalHeader.MajorSubsystemVersion = VAL16(4);
    m_ntHeaders->OptionalHeader.MinorSubsystemVersion = VAL16(0);
    m_ntHeaders->OptionalHeader.Win32VersionValue     = VAL32(0);
    m_ntHeaders->OptionalHeader.Subsystem             = VAL16(0);
    m_ntHeaders->OptionalHeader.DllCharacteristics    = VAL16(0);
    m_ntHeaders->OptionalHeader.CheckSum              = VAL32(0);
    setDllCharacteristics(IMAGE_DLLCHARACTERISTICS_NO_SEH |
                          IMAGE_DLLCHARACTERISTICS_NX_COMPAT |
                          IMAGE_DLLCHARACTERISTICS_DYNAMIC_BASE |
                          IMAGE_DLLCHARACTERISTICS_TERMINAL_SERVER_AWARE);

    if (isPE32())
    {
        IMAGE_NT_HEADERS32*  p_ntHeaders32 = ntHeaders32();
        p_ntHeaders32->OptionalHeader.ImageBase             = VAL32(CEE_IMAGE_BASE_32);
        p_ntHeaders32->OptionalHeader.SizeOfStackReserve    = VAL32(0x100000);
        p_ntHeaders32->OptionalHeader.SizeOfStackCommit     = VAL32(0x1000);
        p_ntHeaders32->OptionalHeader.SizeOfHeapReserve     = VAL32(0x100000);
        p_ntHeaders32->OptionalHeader.SizeOfHeapCommit      = VAL32(0x1000);
        p_ntHeaders32->OptionalHeader.LoaderFlags           = VAL32(0);
        p_ntHeaders32->OptionalHeader.NumberOfRvaAndSizes   = VAL32(16);
    }
    else
    {
        IMAGE_NT_HEADERS64*  p_ntHeaders64 = ntHeaders64();
        // FIX what are the correct values for PE+ (64-bit) ?
        p_ntHeaders64->OptionalHeader.ImageBase             = VAL64(CEE_IMAGE_BASE_64);
        p_ntHeaders64->OptionalHeader.SizeOfStackReserve    = VAL64(0x400000);
        p_ntHeaders64->OptionalHeader.SizeOfStackCommit     = VAL64(0x4000);
        p_ntHeaders64->OptionalHeader.SizeOfHeapReserve     = VAL64(0x100000);
        p_ntHeaders64->OptionalHeader.SizeOfHeapCommit      = VAL64(0x2000);
        p_ntHeaders64->OptionalHeader.LoaderFlags           = VAL32(0);
        p_ntHeaders64->OptionalHeader.NumberOfRvaAndSizes   = VAL32(16);
    }

    m_ilRVA = (DWORD) -1;
    m_dataRvaBase = 0;
    m_rdataRvaBase = 0;
    m_codeRvaBase = 0;

    virtualPos = 0;
    filePos = 0;
    reloc = NULL;
    strtab = NULL;
    headers = NULL;
    headersEnd = NULL;

    m_file = INVALID_HANDLE_VALUE;

    return S_OK;
}

/******************************************************************/
HRESULT PEWriter::Cleanup() {

    if (isPE32())
    {
        delete ntHeaders32();
    }
    else
    {
        delete ntHeaders64();
    }

    if (headers != NULL)
        delete [] headers;

    if (pEntries != NULL)
        delete [] pEntries;

    return PESectionMan::Cleanup();
}

HRESULT PEWriter::newSection(const char* name, PESection **section,
                            unsigned flags, unsigned estSize,
                            unsigned estRelocs)
{
    PEWriterSection * ret = new (nothrow) PEWriterSection(name, flags, estSize, estRelocs);
    *section = ret;
    TESTANDRETURNMEMORY(ret);
    return S_OK;
}

ULONG PEWriter::getIlRva()
{
    // assume that pe optional header is less than size of section alignment. So this
    // gives out the rva for the .text section, which is merged after the .text0 section
    // This is verified in debug build when actually write out the file
    _ASSERTE(m_ilRVA > 0);
    return m_ilRVA;
}

void PEWriter::setIlRva(ULONG offset)
{
    // assume that pe optional header is less than size of section alignment. So this
    // gives out the rva for the .text section, which is merged after the .text0 section
    // This is verified in debug build when actually write out the file
    m_ilRVA = roundUp(VAL32(m_ntHeaders->OptionalHeader.SectionAlignment) + offset, SUBSECTION_ALIGN);
}

HRESULT PEWriter::setDirectoryEntry(PEWriterSection *section, ULONG entry, ULONG size, ULONG offset)
{
    if (entry >= cEntries)
    {
        USHORT cNewEntries = (USHORT)max((ULONG)cEntries * 2, entry + 1);

        if (cNewEntries <= cEntries) return E_OUTOFMEMORY;  // Integer overflow
        if (cNewEntries <= entry) return E_OUTOFMEMORY;  // Integer overflow

        directoryEntry *pNewEntries = new (nothrow) directoryEntry [ cNewEntries ];
        if (pNewEntries == NULL) return E_OUTOFMEMORY;

        CopyMemory(pNewEntries, pEntries, cEntries * sizeof(*pNewEntries));
        ZeroMemory(pNewEntries + cEntries, (cNewEntries - cEntries) * sizeof(*pNewEntries));

        delete [] pEntries;
        pEntries = pNewEntries;
        cEntries = cNewEntries;
    }

    pEntries[entry].section = section;
    pEntries[entry].offset = offset;
    pEntries[entry].size = size;
    return S_OK;
}

//-----------------------------------------------------------------------------
// These 2 write functions must be implemented here so that they're in the same
// .obj file as whoever creates the FILE struct. We can't pass a FILE struct
// across a dll boundary and use it.
//-----------------------------------------------------------------------------

HRESULT PEWriterSection::write(HANDLE file)
{
    return m_blobFetcher.Write(file);
}

//-----------------------------------------------------------------------------
// Write out the section to the stream
//-----------------------------------------------------------------------------
HRESULT CBlobFetcher::Write(HANDLE file)
{
// Must write out each pillar (including idx = m_nIndexUsed), one after the other
    unsigned idx;
    for(idx = 0; idx <= m_nIndexUsed; idx ++) {
        if (m_pIndex[idx].GetDataLen() > 0)
        {
            ULONG length = m_pIndex[idx].GetDataLen();
            DWORD dwWritten = 0;
            if (!WriteFile(file, m_pIndex[idx].GetRawDataStart(), length, &dwWritten, NULL))
            {
                return HRESULT_FROM_GetLastError();
            }
            _ASSERTE(dwWritten == length);
        }
    }

    return S_OK;
}


//-----------------------------------------------------------------------------
// These 2 write functions must be implemented here so that they're in the same
// .obj file as whoever creates the FILE struct. We can't pass a FILE struct
// across a dll boundary  and use it.
//-----------------------------------------------------------------------------

unsigned PEWriterSection::writeMem(void **ppMem)
{
    HRESULT hr;
    hr = m_blobFetcher.WriteMem(ppMem);
    _ASSERTE(SUCCEEDED(hr));

    return m_blobFetcher.GetDataLen();
}

//-----------------------------------------------------------------------------
// Write out the section to memory
//-----------------------------------------------------------------------------
HRESULT CBlobFetcher::WriteMem(void **ppMem)
{
    char **ppDest = (char **)ppMem;
    // Must write out each pillar (including idx = m_nIndexUsed), one after the other
    unsigned idx;
    for(idx = 0; idx <= m_nIndexUsed; idx ++) {
        if (m_pIndex[idx].GetDataLen() > 0)
        {
            // WARNING: macro - must enclose in curly braces
            COPY_AND_ADVANCE(*ppDest, m_pIndex[idx].GetRawDataStart(), m_pIndex[idx].GetDataLen());
        }
    }

    return S_OK;
}

/******************************************************************/

//
// Intermediate table to sort to help determine section order
//
struct entry {
    const char *    name;       // full name of the section
    unsigned char   nameLength; // length of the text part of the name
    signed char     index;      // numeral value at the end of the name; -1 if none
    unsigned short  arrayIndex; // index of section within sectStart[]
};

class SectionNameSorter : protected CQuickSort<entry>
{
    entry *             m_entries;
    PEWriterSection **  m_sections;
    unsigned            m_count;

  public:
    SectionNameSorter(entry *entries, PEWriterSection ** sections, int count)
      : CQuickSort<entry>(entries, count),
        m_entries(entries),
        m_sections(sections),
        m_count(unsigned(count))
    {}

    // Sorts the entries according to alphabetical + numerical order

    int Compare(entry *first, entry *second)
    {
        PEWriterSection * firstSection = m_sections[first->arrayIndex];
        PEWriterSection * secondSection = m_sections[second->arrayIndex];

        // Sort the names

        int lenDiff = first->nameLength - second->nameLength;
        int smallerLen;
        if (lenDiff < 0)
            smallerLen = first->nameLength;
        else
            smallerLen = second->nameLength;

        int result = strncmp(first->name, second->name, smallerLen);

        if (result != 0)
            return result;
        else
        {
            if (lenDiff != 0)
                return lenDiff;
            else
                return (int)(first->index - second->index);
        }
    }

    int SortSections()
    {
        Sort();

        entry * ePrev = m_entries;
        entry * e = ePrev + 1;
        int iSections = 1; // First section is obviously unique

        for (unsigned i = 1; i < m_count; i++, ePrev = e, e++) {

            if ((ePrev->nameLength == e->nameLength) &&
                strncmp(ePrev->name, e->name, e->nameLength) == 0)
            {
                continue;
            }

            iSections++;
        }

        return iSections;
    }
};

#define SectionIndex    IMAGE_SECTION_HEADER::VirtualAddress
#define FirstEntryIndex IMAGE_SECTION_HEADER::SizeOfRawData

HRESULT PEWriter::linkSortSections(entry * entries,
                                   unsigned * piEntries,
                                   unsigned * piUniqueSections)
{
    //
    // Preserve current section order as much as possible, but apply the following
    // rules:
    //  - sections named "xxx#" are collated into a single PE section "xxx".
    //      The contents of the CeeGen sections are sorted according to numerical
    //      order & made contiguous in the PE section
    //  - "text" always comes first in the file
    //  - empty sections receive no PE section
    //

    entry *e = entries;
    for (PEWriterSection **cur = getSectStart(); cur < getSectCur(); cur++) {

        //
        // Throw away any old headers we've used.
        //

        (*cur)->m_header = NULL;

        //
        // Don't allocate PE data for 0 length sections
        //

        if ((*cur)->dataLen() == 0)
            continue;

        e->name = (*cur)->m_name;

        //
        // Now find the end of the text part of the section name, and
        // calculate the numeral (if any) at the end
        //

        _ASSERTE(strlen(e->name) < UCHAR_MAX);
        const char *p = e->name + strlen(e->name);
        int index = 0; // numeral at the end of the section name
        int placeValue = 1;
        if (isdigit(p[-1]))
        {
            while (--p > e->name)
            {
                if (!isdigit(*p))
                    break;
                index += ((*p - '0') * placeValue);
                placeValue *= 10;
            }
            p++;

            //
            // Special case: put "xxx" after "xxx0" and before "xxx1"
            //

            if (index == 0)
                index = -1;
        }

        _ASSERTE(index == -1 || index == atoi(p));

        e->nameLength = (unsigned char)(p - e->name);
        e->index = (char)index;
        e->arrayIndex = (unsigned short)(cur - getSectStart());
        e++;
    }

    //
    // Sort the entries according to alphabetical + numerical order
    //

    SectionNameSorter sorter(entries, getSectStart(), int(e - entries));
    *piUniqueSections = sorter.SortSections();

    *piEntries = unsigned(e - entries);

    return S_OK;
}

class HeaderSorter : public CQuickSort<IMAGE_SECTION_HEADER>
{
  public:
    HeaderSorter(IMAGE_SECTION_HEADER *headers, int count)
      : CQuickSort<IMAGE_SECTION_HEADER>(headers, count) {}

    int Compare(IMAGE_SECTION_HEADER *first, IMAGE_SECTION_HEADER *second)
    {
        // IMAGE_SECTION_HEADER::VirtualAddress/SectionIndex contains the
        // index of the section
        return VAL32(first->SectionIndex) - VAL32(second->SectionIndex);
    }
};

HRESULT PEWriter::linkSortHeaders(entry * entries, unsigned iEntries, unsigned iUniqueSections)
{
    if (headers != NULL)
        delete [] headers;

    // 1 extra for .reloc
    S_UINT32 cUniqueSectionsAllocated = S_UINT32(iUniqueSections) + S_UINT32(1);
    if (cUniqueSectionsAllocated.IsOverflow())
    {
        return COR_E_OVERFLOW;
    }
    headers = new (nothrow) IMAGE_SECTION_HEADER[cUniqueSectionsAllocated.Value()];
    TESTANDRETURNMEMORY(headers);

    memset(headers, 0, sizeof(*headers) * cUniqueSectionsAllocated.Value());

    entry *ePrev = NULL;
    IMAGE_SECTION_HEADER *h = headers - 1;

    //
    // Store the sorting index
    //

    entry * entriesEnd = entries + iEntries;

    for (entry * e = entries ; e < entriesEnd; e++)
    {
        if (ePrev != NULL
            && e->nameLength == ePrev->nameLength
            && strncmp(e->name, ePrev->name, e->nameLength) == 0)
        {
            //
            // This section has the same name as the previous section, and
            // will be collapsed with the previous section.
            // Just update the (common) header information
            //

            if (e->arrayIndex < ePrev->arrayIndex)
            {
                //
                // Use the smaller of the indices of e and ePrev
                //
                h->SectionIndex = VAL32(VAL32(h->SectionIndex) - (e->arrayIndex - ePrev->arrayIndex));
            }

            // Store an approximation of the size of the section temporarily
            h->Misc.VirtualSize =  VAL32(VAL32(h->Misc.VirtualSize) + getSectStart()[e->arrayIndex]->dataLen());
        }
        else
        {
            // Grab a new header

            h++;

            strncpy_s((char *) h->Name, sizeof(h->Name), e->name, e->nameLength);

            setSectionIndex(h, e->arrayIndex);

            // Store the entry index in this field temporarily
            h->FirstEntryIndex = VAL32((DWORD)(e - entries));

            // Store an approximation of the size of the section temporarily
            h->Misc.VirtualSize = VAL32(getSectStart()[e->arrayIndex]->dataLen());
        }
        ePrev = e;
    }

    headersEnd = ++h;

    _ASSERTE(headers + iUniqueSections == headersEnd);

    //
    // Sort the entries according to alphabetical + numerical order
    //

    HeaderSorter headerSorter(headers, int(iUniqueSections));

    headerSorter.Sort();

    return S_OK;
} // PEWriter::linkSortHeaders

HRESULT PEWriter::linkPlaceSections(entry * entries, unsigned iEntries)
{
    entry * entriesEnd = entries + iEntries;

    for (IMAGE_SECTION_HEADER * h = headers; h < headersEnd; h++)
    {
        // Get to the first entry corresponding to this section header

        entry * e = entries + VAL32(h->FirstEntryIndex);
        PEWriterSection *s = getSectStart()[e->arrayIndex];

        h->VirtualAddress = VAL32(virtualPos);
        h->PointerToRawData = VAL32(filePos);

        s->m_baseRVA = virtualPos;
        s->m_filePos = filePos;
        s->m_header = h;
        h->Characteristics = VAL32(s->m_flags);

#ifdef LOGGING
        LOG((LF_ZAP, LL_INFO10,
             "   Section %-7s RVA=%08x, Length=%08x, FilePos=%08x\n",
             s->m_name, s->m_baseRVA, s->dataLen(), s->m_filePos));
#endif

        unsigned dataSize = s->dataLen();

        // Find all the other entries corresponding to this section header

        PEWriterSection *sPrev = s;
        entry * ePrev = e;
        while (++e < entriesEnd)
        {
           if (e->nameLength != ePrev->nameLength
               || strncmp(e->name, ePrev->name, e->nameLength) != 0)
               break;

           s = getSectStart()[e->arrayIndex];
           _ASSERTE(s->m_flags == VAL32(h->Characteristics));

           sPrev->m_filePad = padLen(dataSize, SUBSECTION_ALIGN);
           dataSize = roundUp(dataSize, SUBSECTION_ALIGN);

           s->m_baseRVA = virtualPos + dataSize;
           s->m_filePos = filePos + dataSize;
           s->m_header = h;
           sPrev = s;

           dataSize += s->dataLen();

#ifdef LOGGING
           LOG((LF_ZAP, LL_INFO10,
                "   Section %-7s RVA=%08x, Length=%08x, FilePos=%08x\n",
                s->m_name, s->m_baseRVA, s->dataLen(), s->m_filePos));
#endif

           ePrev = e;
        }

        h->Misc.VirtualSize = VAL32(dataSize);

        sPrev->m_filePad = padLen(dataSize, VAL32(m_ntHeaders->OptionalHeader.FileAlignment));
        dataSize = roundUp(dataSize, VAL32(m_ntHeaders->OptionalHeader.FileAlignment));
        h->SizeOfRawData = VAL32(dataSize);
        filePos += dataSize;

        dataSize = roundUp(dataSize, VAL32(m_ntHeaders->OptionalHeader.SectionAlignment));
        virtualPos += dataSize;
    }

    return S_OK;
}

void PEWriter::setSectionIndex(IMAGE_SECTION_HEADER * h, unsigned sectionIndex) {

    //
    // Reserve some dummy "array index" values for special sections
    // at the start of the image (after the seed sections)
    //

    static const char * const SpecialNames[] = { ".text", ".cormeta", NULL };
    enum { SPECIAL_NAMES_COUNT = ARRAY_SIZE(SpecialNames) };

    for (const char * const * s = SpecialNames; /**/; s++)
    {
        if (*s == 0)
        {
            h->SectionIndex = VAL32(sectionIndex + SPECIAL_NAMES_COUNT);
            break;
        }
        else if (strcmp((char *) h->Name, *s) == 0)
        {
            h->SectionIndex = VAL32(DWORD(s - SpecialNames));
            break;
        }
    }

}


HRESULT PEWriter::link() {

    //
    // NOTE:
    // link() can be called more than once!  This is because at least one compiler
    // (the prejitter) needs to know the base addresses of some segments before it
    // builds others. It's up to the caller to ensure the layout remains the same
    // after changes are made, though.
    //

    //
    // Assign base addresses to all sections, and layout & merge PE sections
    //

    //
    // Collate by name & sort by index
    //

    // First collect all information into entries[]

    int sectCount = getSectCount();
    entry *entries = (entry *) _alloca(sizeof(entry) * sectCount);

    unsigned iUniqueSections, iEntries;
    HRESULT hr;
    IfFailRet(linkSortSections(entries, &iEntries, &iUniqueSections));

    //
    // Now, allocate a header for each unique section name.
    // Also record the minimum section index for each section
    // so we can preserve order as much as possible.
    //

    IfFailRet(linkSortHeaders(entries, iEntries, iUniqueSections));

    //
    // If file alignment is not zero, it must have been set through
    //  setFileAlignment(), in which case we leave it untouched
    //

    if( VAL32(0) == m_ntHeaders->OptionalHeader.FileAlignment )
    {
        //
        // Figure out what file alignment to use.
        //

        unsigned RoundUpVal = 0x0200;

        m_ntHeaders->OptionalHeader.FileAlignment = VAL32(RoundUpVal);
    }

    //
    // Now, assign a section header & location to each section
    //

    iUniqueSections++; // One more for .reloc
    filePos = sizeof(IMAGE_DOS_HEADER)+sizeof(x86StubPgm) + m_ntHeadersSize;

    m_ntHeaders->FileHeader.NumberOfSections = (WORD)VAL16(iUniqueSections);

    filePos += iUniqueSections * sizeof(IMAGE_SECTION_HEADER);
    filePos  = roundUp(filePos, VAL32(m_ntHeaders->OptionalHeader.FileAlignment));

    m_ntHeaders->OptionalHeader.SizeOfHeaders = VAL32(filePos);

    virtualPos = roundUp(filePos, VAL32(m_ntHeaders->OptionalHeader.SectionAlignment));

    // Now finally assign RVAs to the sections

    IfFailRet(linkPlaceSections(entries, iEntries));

    return S_OK;
}

#undef SectionIndex
#undef FirstEntryIndex


class SectionRVASorter : public CQuickSort<PEWriterSection*>
{
    public:
        SectionRVASorter(PEWriterSection **elts, SSIZE_T count)
          : CQuickSort<PEWriterSection*>(elts, count) {}

        int Compare(PEWriterSection **e1, PEWriterSection **e2)
        {
            return (*e1)->getBaseRVA() - (*e2)->getBaseRVA();
        }
};

HRESULT PEWriter::fixup(CeeGenTokenMapper *pMapper)
{
    HRESULT hr;

    const unsigned RoundUpVal = VAL32(m_ntHeaders->OptionalHeader.FileAlignment);

    //
    // Apply manual relocation for entry point field
    //

    PESection *textSection;
    IfFailRet(getSectionCreate(".text", 0, &textSection));

    if (m_ntHeaders->OptionalHeader.AddressOfEntryPoint != VAL32(0))
        m_ntHeaders->OptionalHeader.AddressOfEntryPoint = VAL32(VAL32(m_ntHeaders->OptionalHeader.AddressOfEntryPoint) + textSection->m_baseRVA);

    //
    // Apply normal relocs
    //

    IfFailRet(getSectionCreate(".reloc", sdReadOnly | IMAGE_SCN_MEM_DISCARDABLE,
                                (PESection **) &reloc));
    reloc->m_baseRVA = virtualPos;
    reloc->m_filePos = filePos;
    reloc->m_header = headersEnd++;
    strcpy_s((char *)reloc->m_header->Name, sizeof(reloc->m_header->Name),
                ".reloc");
    reloc->m_header->Characteristics = VAL32(reloc->m_flags);
    reloc->m_header->VirtualAddress = VAL32(virtualPos);
    reloc->m_header->PointerToRawData = VAL32(filePos);

    //
    // Sort the sections by RVA
    //

    CQuickArray<PEWriterSection *> sections;

    SIZE_T count = getSectCur() - getSectStart();
    IfFailRet(sections.ReSizeNoThrow(count));
    UINT i = 0;
    PEWriterSection **cur;
    for(cur = getSectStart(); cur < getSectCur(); cur++, i++)
        sections[i] = *cur;

    SectionRVASorter sorter(sections.Ptr(), sections.Size());

    sorter.Sort();

    PERelocSection relocSection(reloc);

    cur = sections.Ptr();
    PEWriterSection **curEnd = cur + sections.Size();
    while (cur < curEnd)
    {
        IfFailRet((*cur)->applyRelocs(m_ntHeaders,
                                        &relocSection,
                                        pMapper,
                                        m_dataRvaBase,
                                        m_rdataRvaBase,
                                        m_codeRvaBase));
        cur++;
    }

    relocSection.Finish(isPE32());
    reloc->m_header->Misc.VirtualSize = VAL32(reloc->dataLen());

    // Strip the reloc section if the flag is set
    if (m_ntHeaders->FileHeader.Characteristics & VAL16(IMAGE_FILE_RELOCS_STRIPPED))
    {
        reloc->m_header->Misc.VirtualSize = VAL32(0);
    }

    reloc->m_header->SizeOfRawData = VAL32(roundUp(VAL32(reloc->m_header->Misc.VirtualSize), RoundUpVal));
    reloc->m_filePad = padLen(VAL32(reloc->m_header->Misc.VirtualSize), RoundUpVal);
    filePos += VAL32(reloc->m_header->SizeOfRawData);
    virtualPos += roundUp(VAL32(reloc->m_header->Misc.VirtualSize),
                            VAL32(m_ntHeaders->OptionalHeader.SectionAlignment));

    if (reloc->m_header->Misc.VirtualSize == VAL32(0))
    {
        //
        // Omit reloc section from section list.  (It will
        // still be there but the loader won't see it - this
        // only works because we've allocated it as the last
        // section.)
        //
        m_ntHeaders->FileHeader.NumberOfSections = VAL16(VAL16(m_ntHeaders->FileHeader.NumberOfSections) - 1);
    }
    else
    {
        IMAGE_DATA_DIRECTORY * pRelocDataDirectory;
        //
        // Put reloc address in header
        //
        if (isPE32())
        {
            pRelocDataDirectory = &(ntHeaders32()->OptionalHeader.DataDirectory[IMAGE_DIRECTORY_ENTRY_BASERELOC]);
        }
        else
        {
            pRelocDataDirectory = &(ntHeaders64()->OptionalHeader.DataDirectory[IMAGE_DIRECTORY_ENTRY_BASERELOC]);
        }

        pRelocDataDirectory->VirtualAddress = reloc->m_header->VirtualAddress;
        pRelocDataDirectory->Size           = reloc->m_header->Misc.VirtualSize;
    }

    // compute ntHeader fields that depend on the sizes of other things
    for(IMAGE_SECTION_HEADER *h = headersEnd-1; h >= headers; h--) {    // go backwards, so first entry takes precedence
        if (h->Characteristics & VAL32(IMAGE_SCN_CNT_CODE)) {
            m_ntHeaders->OptionalHeader.BaseOfCode = h->VirtualAddress;
            m_ntHeaders->OptionalHeader.SizeOfCode =
                VAL32(VAL32(m_ntHeaders->OptionalHeader.SizeOfCode) + VAL32(h->SizeOfRawData));
        }
        if (h->Characteristics & VAL32(IMAGE_SCN_CNT_INITIALIZED_DATA)) {
            if (isPE32())
            {
                ntHeaders32()->OptionalHeader.BaseOfData = h->VirtualAddress;
            }
            m_ntHeaders->OptionalHeader.SizeOfInitializedData =
                VAL32(VAL32(m_ntHeaders->OptionalHeader.SizeOfInitializedData) + VAL32(h->SizeOfRawData));
        }
        if (h->Characteristics & VAL32(IMAGE_SCN_CNT_UNINITIALIZED_DATA)) {
            m_ntHeaders->OptionalHeader.SizeOfUninitializedData =
                VAL32(VAL32(m_ntHeaders->OptionalHeader.SizeOfUninitializedData) + VAL32(h->SizeOfRawData));
        }
    }

    int index;
    IMAGE_DATA_DIRECTORY * pCurDataDirectory;

    // go backwards, so first entry takes precedence
    for(cur = getSectCur()-1; getSectStart() <= cur; --cur)
    {
        index = (*cur)->getDirEntry();

        // Is this a valid directory entry
        if (index > 0)
        {
            if (isPE32())
            {
                _ASSERTE((unsigned)(index) < VAL32(ntHeaders32()->OptionalHeader.NumberOfRvaAndSizes));

                pCurDataDirectory = &(ntHeaders32()->OptionalHeader.DataDirectory[index]);
            }
            else
            {
                _ASSERTE((unsigned)(index) < VAL32(ntHeaders64()->OptionalHeader.NumberOfRvaAndSizes));

                pCurDataDirectory = &(ntHeaders64()->OptionalHeader.DataDirectory[index]);
            }

            pCurDataDirectory->VirtualAddress = VAL32((*cur)->m_baseRVA);
            pCurDataDirectory->Size           = VAL32((*cur)->dataLen());
        }
    }

    // handle the directory entries specified using the file.
    for (index=0; index < cEntries; index++)
    {
        if (pEntries[index].section)
        {
            PEWriterSection *section = pEntries[index].section;
            _ASSERTE(pEntries[index].offset < section->dataLen());

            if (isPE32())
                pCurDataDirectory = &(ntHeaders32()->OptionalHeader.DataDirectory[index]);
            else
                pCurDataDirectory = &(ntHeaders64()->OptionalHeader.DataDirectory[index]);

            pCurDataDirectory->VirtualAddress = VAL32(section->m_baseRVA + pEntries[index].offset);
            pCurDataDirectory->Size           = VAL32(pEntries[index].size);
        }
    }

    m_ntHeaders->OptionalHeader.SizeOfImage = VAL32(virtualPos);

    const unsigned headerOffset = (unsigned)sizeof(IMAGE_DOS_HEADER) + sizeof(x86StubPgm);

    memset(&m_dosHeader, 0, sizeof(IMAGE_DOS_HEADER));
    m_dosHeader.e_magic = VAL16(IMAGE_DOS_SIGNATURE);
    m_dosHeader.e_cblp =  VAL16(0x90);              // bytes in last page
    m_dosHeader.e_cp =  VAL16(3);                   // pages in file
    m_dosHeader.e_cparhdr =  VAL16(4);              // size of header in paragraphs
    m_dosHeader.e_maxalloc =   VAL16(0xFFFF);       // maximum extra mem needed
    m_dosHeader.e_sp =  VAL16(0xB8);                // initial SP value
    m_dosHeader.e_lfarlc =  VAL16(0x40);            // file offset of relocations
    m_dosHeader.e_lfanew =  VAL32(headerOffset);    // file offset of NT header!

    return(S_OK);   // SUCCESS
}

HRESULT PEWriter::Open(_In_ LPCWSTR fileName)
{
    _ASSERTE(m_file == INVALID_HANDLE_VALUE);
    HRESULT hr = NOERROR;

    m_file = WszCreateFile(fileName,
                           GENERIC_WRITE,
                           0, // No sharing.  Was: FILE_SHARE_READ | FILE_SHARE_WRITE,
                           NULL,
                           CREATE_ALWAYS,
                           FILE_ATTRIBUTE_NORMAL,
                           NULL );
    if (m_file == INVALID_HANDLE_VALUE)
        hr = HRESULT_FROM_GetLastErrorNA();

    return hr;
}

HRESULT PEWriter::Seek(int offset)
{
    _ASSERTE(m_file != INVALID_HANDLE_VALUE);
    if (SetFilePointer(m_file, offset, 0, FILE_BEGIN))
        return S_OK;
    else
        return HRESULT_FROM_GetLastError();
}

HRESULT PEWriter::Write(const void *data, int size)
{
    _ASSERTE(m_file != INVALID_HANDLE_VALUE);

    HRESULT hr = S_OK;
    DWORD dwWritten = 0;
    if (size)
    {
        CQuickBytes zero;
        if (data == NULL)
        {
            hr = zero.ReSizeNoThrow(size);
            if (SUCCEEDED(hr))
            {
                ZeroMemory(zero.Ptr(), size);
                data = zero.Ptr();
            }
        }

        if (WriteFile(m_file, data, size, &dwWritten, NULL))
        {
            _ASSERTE(dwWritten == (DWORD)size);
        }
        else
            hr = HRESULT_FROM_GetLastError();
    }

    return hr;
}

HRESULT PEWriter::Pad(int align)
{
    DWORD offset = SetFilePointer(m_file, 0, NULL, FILE_CURRENT);
    int pad = padLen(offset, align);
    if (pad > 0)
        return Write(NULL, pad);
    else
        return S_FALSE;
}

HRESULT PEWriter::Close()
{
    if (m_file == INVALID_HANDLE_VALUE)
        return S_OK;

    HRESULT hr;
    if (CloseHandle(m_file))
        hr = S_OK;
    else
        hr = HRESULT_FROM_GetLastError();

    m_file = INVALID_HANDLE_VALUE;

    return hr;
}

/******************************************************************/
HRESULT PEWriter::write(_In_ LPCWSTR fileName) {

    HRESULT hr;

    unsigned RoundUpVal;
    RoundUpVal = VAL32(m_ntHeaders->OptionalHeader.FileAlignment);

    IfFailGo(Open(fileName));

    // write the PE headers
    IfFailGo(Write(&m_dosHeader, sizeof(IMAGE_DOS_HEADER)));
    IfFailGo(Write(x86StubPgm, sizeof(x86StubPgm)));
    IfFailGo(Write(m_ntHeaders, m_ntHeadersSize));

    IfFailGo(Write(headers, (int)(sizeof(IMAGE_SECTION_HEADER)*(headersEnd-headers))));

    IfFailGo(Pad(RoundUpVal));

    // write the actual data
    for (PEWriterSection **cur = getSectStart(); cur < getSectCur(); cur++) {
        if ((*cur)->m_header != NULL) {
            IfFailGo(Seek((*cur)->m_filePos));
            IfFailGo((*cur)->write(m_file));
            IfFailGo(Write(NULL, (*cur)->m_filePad));
        }
    }

    return Close();

 ErrExit:
    Close();

    return hr;
}

HRESULT PEWriter::write(void ** ppImage)
{
    const unsigned RoundUpVal = VAL32(m_ntHeaders->OptionalHeader.FileAlignment);
    char *pad = (char *) _alloca(RoundUpVal);
    memset(pad, 0, RoundUpVal);

    size_t lSize = filePos;

    // allocate the block we are handing back to the caller
    void * pImage = (void *) CoTaskMemAlloc(lSize);
    if (NULL == pImage)
    {
        return E_OUTOFMEMORY;
    }

    // zero the memory
    ::memset(pImage, 0, lSize);

    char *pCur = (char *)pImage;

    // PE Header
    COPY_AND_ADVANCE(pCur, &m_dosHeader, sizeof(IMAGE_DOS_HEADER));
    COPY_AND_ADVANCE(pCur, x86StubPgm, sizeof(x86StubPgm));
    COPY_AND_ADVANCE(pCur, m_ntHeaders, m_ntHeadersSize);

    COPY_AND_ADVANCE(pCur, headers, sizeof(*headers)*(headersEnd - headers));

    // now the sections
    // write the actual data
    for (PEWriterSection **cur = getSectStart(); cur < getSectCur(); cur++) {
        if ((*cur)->m_header != NULL) {
            unsigned len;
            pCur = (char*)pImage + (*cur)->m_filePos;
            len = (*cur)->writeMem((void**)&pCur);
            _ASSERTE(len == (*cur)->dataLen());
            COPY_AND_ADVANCE(pCur, pad, (*cur)->m_filePad);
        }
    }

    // make sure we wrote the exact numbmer of bytes expected
    _ASSERTE(lSize == (size_t) (pCur - (char *)pImage));

    // give pointer to memory image back to caller (who must free with CoTaskMemFree())
    *ppImage = pImage;

    // all done
    return S_OK;
}

HRESULT PEWriter::getFileTimeStamp(DWORD *pTimeStamp)
{
    if (pTimeStamp)
        *pTimeStamp = m_peFileTimeStamp;

    return S_OK;
}

void PEWriter::setFileHeaderTimeStamp(DWORD timeStamp)
{
    m_ntHeaders->FileHeader.TimeDateStamp = timeStamp;
}

DWORD PEWriter::getImageBase32()
{
    _ASSERTE(isPE32());
    return VAL32(ntHeaders32()->OptionalHeader.ImageBase);
}

UINT64 PEWriter::getImageBase64()
{
    _ASSERTE(!isPE32());
    return VAL64(ntHeaders64()->OptionalHeader.ImageBase);
}

void PEWriter::setImageBase32(DWORD imageBase)
{
    _ASSERTE(isPE32());
    ntHeaders32()->OptionalHeader.ImageBase = VAL32(imageBase);
}

void PEWriter::setImageBase64(UINT64 imageBase)
{
    _ASSERTE(!isPE32());
    ntHeaders64()->OptionalHeader.ImageBase = VAL64(imageBase);
}

void PEWriter::getHeaderInfo(PIMAGE_NT_HEADERS *ppNtHeaders, PIMAGE_SECTION_HEADER *ppSections, ULONG *pNumSections)
{
    *ppNtHeaders = m_ntHeaders;
    *ppSections = headers;
    *pNumSections = (ULONG)(headersEnd - headers);
}
