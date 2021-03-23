// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#include "stdafx.h"

// Enable building with older SDKs that don't have IMAGE_FILE_MACHINE_ARM64 defined.
#ifndef IMAGE_FILE_MACHINE_ARM64
#define IMAGE_FILE_MACHINE_ARM64             0xAA64  // ARM64 Little-Endian
#endif

#include "blobfetcher.h"
#include "pedecoder.h"

#ifdef _DEBUG
#define LOGGING
#endif

#ifdef LOGGING
#include "log.h"

static const char* const RelocName[] = {
    "Absolute", "Unk1",    "Unk2",    "HighLow", "Unk4", "MapToken",
    "Relative", "FilePos", "CodeRel", "Movl64",  "Dir64", "PcRel25", "PcRel64",
    "AbsTag" };
static const char RelocSpaces[] = "        ";

static INT64 s_minPcRel25;
static INT64 s_maxPcRel25;
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

inline static bool isExeOrDll(IMAGE_NT_HEADERS* ntHeaders) {
    return ((ntHeaders->FileHeader.Characteristics & VAL16(IMAGE_FILE_EXECUTABLE_IMAGE)) != 0);
}

#ifndef IMAGE_DLLCHARACTERISTICS_NO_SEH
#define IMAGE_DLLCHARACTERISTICS_NO_SEH 0x400
#endif

#ifndef IMAGE_DLLCHARACTERISTICS_DYNAMIC_BASE
#define IMAGE_DLLCHARACTERISTICS_DYNAMIC_BASE   0x0040
#endif

#ifndef IMAGE_DLLCHARACTERISTICS_NX_COMPAT
#define IMAGE_DLLCHARACTERISTICS_NX_COMPAT      0x0100
#endif

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
        *offset = VAL16((rva & 0xFFF) | (type << 12));
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
/* subtract two unsigned pointers yeilding a signed pointer sized int */
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
    static_assert_no_msg(NumItems(RelocName) == srRelocSentinel);
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
        bool   isRelocPtr   = ((curType & srRelocPtr) != 0);
        bool   noBaseBaseReloc = ((curType & srNoBaseReloc) != 0);
        UINT64 targetOffset = 0;
        int    slotNum      = 0;
        INT64  oldStarPos;

        // If cur->section is NULL then this is a pointer outside the module.
        bool externalAddress = (cur->section == NULL);

        curType &= ~(srRelocPtr | srNoBaseReloc);

        /* If we see any srRelocHighLow's in a PE64 file we convert them into DIR64 relocs */
        if (!isPE32 && (curType == srRelocHighLow))
            curType = srRelocDir64;

        /* If we have an IA64 instruction fixup then extract the slot number and adjust curOffset */
        if ((curType == srRelocIA64PcRel25) || (curType == srRelocIA64Imm64) || (curType == srRelocIA64PcRel64))
        {
            _ASSERTE((curOffset & 0x3) == 0);
            slotNum = (curOffset & 0xf) >> 2;
            curOffset &= ~0xf;
        }

        DWORD curRVA = m_baseRVA;    // RVA in the PE image of the reloc site
        IfFailRet(AddOvf_RVA(curRVA, curOffset));
        DWORD UNALIGNED * pos = (DWORD *) m_blobFetcher.ComputePointer(curOffset);

        PREFIX_ASSUME(pos != NULL);

#ifdef LOGGING
        LOG((LF_ZAP, LL_INFO1000000,
             "   Reloc %s%s%s at %-7s+%04x (RVA=%08x) at" FMT_ADDR,
             RelocName[curType], (isRelocPtr) ? "Ptr" : "   ",
             &RelocSpaces[strlen(RelocName[curType])],
             m_name, curOffset, curRVA, DBG_ADDR(pos)));
#endif
        //
        // 'pos' is the site of the reloc
        // Compute 'targetOffset' from pointer if necessary
        //

        if (isRelocPtr)
        {
            // Calculate the value of ptr to pass to computeOffset
            char * ptr = (char *) pos;

            if (curType == srRelocRelative) {
                //
                // Here we add sizeof(int) because we need to calculate
                // ptr as the true call target address (x86 pc-rel)
                // We need to true call target address since pass it
                // to computeOffset and this function would fall if
                // the address we pass is before the start of a section
                //
                oldStarPos   = (SSIZE_T) ptr;
                IfFailRet(AddOvf_S_S32(oldStarPos, GET_UNALIGNED_INT32(pos)));
                IfFailRet(AddOvf_S_U32(oldStarPos, sizeof(int)));
                ptr          = (char *) oldStarPos;
                targetOffset = externalAddress ? (size_t) ptr
                                               : cur->section->computeOffset(ptr);
                // We subtract off the four bytes that we added previous
                // since the code below depends upon this
                IfFailRet(SubOvf_U_U32(targetOffset, sizeof(int)));
                IfFailRet(UnsignedFitsIn32Bits(targetOffset));  // Check for overflow
                SET_UNALIGNED_VAL32(pos, targetOffset);
            }
            else if (curType == srRelocIA64Imm64) {
                _ASSERTE(slotNum == 1);
                ptr = (char *) ((intptr_t) GetIA64Imm64((UINT64 *) ptr));
                oldStarPos   = (SSIZE_T) ptr;
                targetOffset = externalAddress ? (size_t) ptr
                                               : cur->section->computeOffset(ptr);
                _ASSERTE(!isPE32);
                PutIA64Imm64((UINT64 *)pos, targetOffset);
            }
            else if (curType == srRelocIA64PcRel64) {
                _ASSERTE(slotNum == 1);
                ptr = (char *) ((intptr_t) GetIA64Rel64((UINT64 *) ptr));
                oldStarPos   = (SSIZE_T) ptr;
                targetOffset = externalAddress ? (size_t) ptr
                                               : cur->section->computeOffset(ptr);
                _ASSERTE(!isPE32);
                PutIA64Rel64((UINT64 *)pos, targetOffset);
            }
            else {
                _ASSERTE(curType != srRelocIA64PcRel25);
                ptr = (char *) GET_UNALIGNED_VALPTR(ptr);
                oldStarPos   = (SSIZE_T) ptr;
                targetOffset = externalAddress ? (size_t) ptr
                                               : cur->section->computeOffset(ptr);
                IfFailRet(UnsignedFitsIn32Bits(targetOffset));  // Check for overflow
                SET_UNALIGNED_VAL32(pos, targetOffset);
                /* Zero the upper 32-bits for a machine with 64-bit pointers */
                if (!isPE32)
                    SET_UNALIGNED_VAL32(pos+1, 0);
            }
        }
#ifdef LOGGING
        else
        {
            if (curType == srRelocIA64PcRel25)
            {
                oldStarPos = GetIA64Rel25((UINT64 *) pos, slotNum);
            }
            else
            {
                if (curType == srRelocIA64PcRel64)
                {
                    _ASSERTE(slotNum == 1);
                    oldStarPos = GetIA64Rel64((UINT64 *) pos);
                }
                else if (curType == srRelocIA64Imm64)
                {
                    oldStarPos = GetIA64Imm64((UINT64 *)pos);
                }
                else
                {
                    oldStarPos = GET_UNALIGNED_VAL32(pos);
                }
            }
        }
#endif

        //
        // 'targetOffset' has now been computed. Write out the appropriate value.
        // Record base relocs as necessary.
        //

        bool  fBaseReloc = false;
        bool  fNeedBrl   = false;
        INT64 newStarPos = 0; // oldStarPos gets updated to newStarPos

        if (curType == srRelocAbsolute || curType == srRelocAbsoluteTagged) {
            _ASSERTE(!externalAddress);

            newStarPos = GET_UNALIGNED_INT32(pos);

            if (curType == srRelocAbsoluteTagged)
                newStarPos = (newStarPos & ~0x80000001) >> 1;

            if (rdataRvaBase > 0 && ! strcmp((const char *)(cur->section->m_name), ".rdata"))
                IfFailRet(AddOvf_S_U32(newStarPos, rdataRvaBase));
            else if (dataRvaBase > 0 && ! strcmp((const char *)(cur->section->m_name), ".data"))
                IfFailRet(AddOvf_S_U32(newStarPos, dataRvaBase));
            else
                IfFailRet(AddOvf_S_U32(newStarPos, cur->section->m_baseRVA));

            if (curType == srRelocAbsoluteTagged)
                newStarPos = (newStarPos << 1) | 0x80000001;

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
            _ASSERTE(!externalAddress);
            newStarPos = GET_UNALIGNED_VAL32(pos);
            IfFailRet(AddOvf_S_U32(newStarPos, cur->section->m_filePos));
            SET_UNALIGNED_VAL32(pos, newStarPos);
        }
        else if (curType == srRelocRelative)
        {
            if (externalAddress) {
#if defined(HOST_AMD64)
                newStarPos = GET_UNALIGNED_INT32(pos);
#else  // x86
                UINT64 targetAddr = GET_UNALIGNED_VAL32(pos);
                IfFailRet(SubOvf_U_U(newStarPos, targetAddr, imageBase));
#endif
            }
            else {
                newStarPos = GET_UNALIGNED_INT32(pos);
                IfFailRet(AddOvf_S_U32(newStarPos, cur->section->m_baseRVA));
            }
            IfFailRet(SubOvf_S_U32(newStarPos, curRVA));
            IfFailRet(SignedFitsIn31Bits(newStarPos));  // Check for overflow
            SET_UNALIGNED_VAL32(pos, newStarPos);
        }
        else if (curType == srRelocCodeRelative)
        {
            newStarPos = GET_UNALIGNED_INT32(pos);
            IfFailRet(SubOvf_S_U32(newStarPos, codeRvaBase));
            if (externalAddress)
                IfFailRet(SubOvf_S_U(newStarPos, imageBase));
            else
                IfFailRet(AddOvf_S_U32(newStarPos, cur->section->m_baseRVA));
            IfFailRet(SignedFitsIn31Bits(newStarPos));  // Check for overflow
            SET_UNALIGNED_VAL32(pos, newStarPos);

        }
        else if (curType == srRelocIA64PcRel25)
        {
            _ASSERTE((m_baseRVA & 15) == 0);
            _ASSERTE((cur->section->m_baseRVA & 15) == 0);

            newStarPos = GetIA64Rel25((UINT64 *) pos, slotNum);
            IfFailRet(SubOvf_S_U32(newStarPos, curRVA));
            if (externalAddress)
                IfFailRet(SubOvf_S_U(newStarPos, imageBase));
            else
                IfFailRet(AddOvf_S_U32(newStarPos, cur->section->m_baseRVA));

            INT64 hiBits = newStarPos >> 24;

            _ASSERTE((hiBits==0) || (hiBits==-1));

            IfFailRet(AddOvf_S_U32(newStarPos, GetIA64Rel25((UINT64 *) pos, slotNum)));

            hiBits = newStarPos >> 24;

            _ASSERTE((hiBits==0) || (hiBits==-1));

            INT32 delta32 = (INT32) newStarPos;

            PutIA64Rel25((UINT64 *) pos, slotNum, delta32);

            _ASSERTE(GetIA64Rel25((UINT64 *) pos, slotNum) == delta32);

#ifdef LOGGING
            if (newStarPos < s_minPcRel25)
                s_minPcRel25 = newStarPos;
            if (newStarPos > s_maxPcRel25)
                s_maxPcRel25 = newStarPos;
#endif
        }
        else if (curType == srRelocIA64PcRel64)
        {
            _ASSERTE((m_baseRVA & 15) == 0);
            _ASSERTE(slotNum == 1);

            newStarPos = GetIA64Rel64((UINT64 *) pos);
            IfFailRet(SubOvf_S_U32(newStarPos, m_baseRVA));

            if (externalAddress)
                IfFailRet(SubOvf_S_U(newStarPos, imageBase));
            else
            {
                _ASSERTE((cur->section->m_baseRVA & 15) == 0);
                IfFailRet(AddOvf_S_U32(newStarPos, cur->section->m_baseRVA));
            }

            INT64 hiBits = newStarPos >> 24;

            fNeedBrl = (hiBits != 0) && (hiBits != -1);

            /* Can we convert the brl.call into a br.call? */
            if (!fNeedBrl)
            {
                INT32 delta32 = (INT32) newStarPos;

                UINT64  temp0 = ((UINT64 *) pos)[0];
                UINT64  temp1 = ((UINT64 *) pos)[1];
#ifdef _DEBUG
                //
                // make certain we're decoding a brl opcode, with template 4 or 5
                //
                UINT64  templa = (temp0 >>  0) & 0x1f;
                UINT64  opcode = (temp1 >> 60) & 0xf;

                _ASSERTE(((opcode == 0xC) || (opcode == 0xD)) &&
                         ((templa == 0x4) || (templa == 0x5)));
#endif
                const UINT64 mask0 = UI64(0x00003FFFFFFFFFE1);
                const UINT64 mask1 = UI64(0x7700000FFF800000);

                /* Clear all bits used as part of the slot1 and slot2 */
                temp0 &= mask0;   // opcode becomes 4 or 5
                temp1 &= mask1;

                temp0 |= 0x10;    // template becomes 0x10 or 0x11
                temp1 |= 0x200;   // slot 1 becomes nop.i

                ((UINT64 *) pos)[0] = temp0;
                ((UINT64 *) pos)[1] = temp1;

                PutIA64Rel25((UINT64 *) pos, 2, delta32);
                _ASSERTE(GetIA64Rel25((UINT64 *) pos, 2) == delta32);
            }
            else
            {
                PutIA64Rel64((UINT64 *) pos, newStarPos);
                _ASSERTE(GetIA64Rel64((UINT64 *) pos) == newStarPos);
            }
        }
        else if (curType == srRelocHighLow)
        {
            _ASSERTE(isPE32);

            // we have a 32-bit value at pos
            UINT64 value = GET_UNALIGNED_VAL32(pos);

            if (!externalAddress)
            {
                IfFailRet(AddOvf_U_U32(value, cur->section->m_baseRVA));
                IfFailRet(AddOvf_U_U(value, imageBase));
            }

            IfFailRet(UnsignedFitsIn32Bits(value));  // Check for overflow
            SET_UNALIGNED_VAL32(pos, value);

            newStarPos = value;

            fBaseReloc = true;
        }
        else if (curType == srRelocDir64)
        {
            _ASSERTE(!isPE32);

            // we have a 64-bit value at pos
            UINT64 UNALIGNED * p_value = (UINT64 *) pos;
            targetOffset = *p_value;

            if (!externalAddress)
            {
                // The upper bits of targetOffset must be zero
                IfFailRet(UnsignedFitsIn32Bits(targetOffset));

                IfFailRet(AddOvf_U_U32(targetOffset, cur->section->m_baseRVA));
                IfFailRet(AddOvf_U_U(targetOffset, imageBase));
            }

            *p_value   = targetOffset;
            newStarPos = targetOffset;
            fBaseReloc = true;
        }
        else if (curType == srRelocIA64Imm64)
        {
            _ASSERTE(!isPE32);
            _ASSERTE((curRVA & 15) == 0);       // This reloc should be 16-byte aligned

            // we have a 64-bit value encoded in the instruction at pos
            targetOffset = GetIA64Imm64((UINT64 *)pos);

            if (!externalAddress)
            {
                // The upper bits of targetOffset must be zero
                IfFailRet(UnsignedFitsIn32Bits(targetOffset));

                IfFailRet(AddOvf_U_U32(targetOffset, cur->section->m_baseRVA));
                IfFailRet(AddOvf_U_U(targetOffset, imageBase));
            }

            PutIA64Imm64((UINT64 *)pos, targetOffset);
            newStarPos = targetOffset;
            fBaseReloc = true;
        }
        else
        {
            _ASSERTE(!"Unknown Relocation type");
        }

        if (fBaseReloc && !noBaseBaseReloc)
        {
            pBaseRelocSection->AddBaseReloc(curRVA, curType);
        }

#ifdef LOGGING
        const char* sectionName;

        if (externalAddress)
        {
            sectionName = "external";
        }
        else
        {
            sectionName = cur->section->m_name;
        }

        LOG((LF_ZAP, LL_INFO1000000,
             "to %-7s+%04x, old =" FMT_ADDR "new =" FMT_ADDR "%s%s\n",
             sectionName, targetOffset,
             DBG_ADDR(oldStarPos), DBG_ADDR(newStarPos),
             fBaseReloc ? "(BASE RELOC)" : "",
             fNeedBrl   ? "(BRL)"        : ""  ));
#endif

    }
    return S_OK;
}

/******************************************************************/

PESeedSection::PESeedSection(PEDecoder * peDecoder,
                             IMAGE_SECTION_HEADER * seedSection)
  : PEWriterSection((const char *)seedSection->Name,
              VAL32(seedSection->Characteristics),
              VAL32(seedSection->SizeOfRawData),
              0),
    m_pSeedFileDecoder(peDecoder),
    m_pSeedSectionHeader(seedSection)
{
    m_baseRVA = VAL32(seedSection->VirtualAddress);
}

HRESULT  PESeedSection::write(HANDLE file) {
    ULONG sizeOfSection = VAL32(m_pSeedSectionHeader->SizeOfRawData);
    LPCVOID sectionData = PBYTE(m_pSeedFileDecoder->GetBase()) + m_pSeedSectionHeader->PointerToRawData;

    DWORD dwWritten = 0;
    if (!WriteFile(file, sectionData, sizeOfSection, &dwWritten, NULL)) {
        return HRESULT_FROM_GetLastError();
    }
    _ASSERTE(dwWritten == sizeOfSection);
    return S_OK;
}

unsigned PESeedSection::writeMem(void ** pMem) {
    ULONG sizeOfSection = VAL32(m_pSeedSectionHeader->SizeOfRawData);
    LPCVOID sectionData = PBYTE(m_pSeedFileDecoder->GetBase()) + m_pSeedSectionHeader->PointerToRawData;

    COPY_AND_ADVANCE(*pMem, sectionData, sizeOfSection);
    return sizeOfSection;
}

/******************************************************************/
HRESULT PEWriter::Init(PESectionMan *pFrom, DWORD createFlags, LPCWSTR seedFileName)
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
    else if ((createFlags & ICEE_CREATE_MACHINE_MASK) == ICEE_CREATE_MACHINE_IA64)
    {
        m_ntHeaders->FileHeader.Machine = VAL16(IMAGE_FILE_MACHINE_IA64);
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
    m_encMode = FALSE;

    virtualPos = 0;
    filePos = 0;
    reloc = NULL;
    strtab = NULL;
    headers = NULL;
    headersEnd = NULL;

    m_file = INVALID_HANDLE_VALUE;

    //
    // Seed file
    //

    m_hSeedFile = INVALID_HANDLE_VALUE;
    m_hSeedFileMap = INVALID_HANDLE_VALUE;
    m_pSeedFileDecoder = NULL;
    m_iSeedSections = 0;
    m_pSeedSectionToAdd = NULL;

    if (seedFileName)
    {
        HandleHolder hFile (WszCreateFile(seedFileName,
                                     GENERIC_READ,
                                     FILE_SHARE_READ,
                                     NULL,
                                     OPEN_EXISTING,
                                     FILE_ATTRIBUTE_NORMAL | FILE_FLAG_SEQUENTIAL_SCAN,
                                     NULL));

        if (hFile == INVALID_HANDLE_VALUE)
            return HRESULT_FROM_GetLastError();

        MapViewHolder hMapFile (WszCreateFileMapping(hFile, NULL, PAGE_READONLY, 0, 0, NULL));
        DWORD dwFileLen = SafeGetFileSize(hFile, 0);
        if (dwFileLen == 0xffffffff)
            return HRESULT_FROM_GetLastError();

        if (hMapFile == NULL)
            return HRESULT_FROM_GetLastError();

        BYTE * baseFileView = (BYTE*) MapViewOfFile(hMapFile, FILE_MAP_READ, 0, 0, 0);

        PEDecoder * pPEDecoder = new (nothrow) PEDecoder(baseFileView, (COUNT_T)dwFileLen);
        if (pPEDecoder == NULL) return E_OUTOFMEMORY;

        if (pPEDecoder->Has32BitNTHeaders())
        {
            if ((createFlags & ICEE_CREATE_FILE_PE32) == 0)
                return E_FAIL;

            setImageBase32(DWORD(size_t(pPEDecoder->GetPreferredBase())));
        }
        else
        {
            if ((createFlags & ICEE_CREATE_FILE_PE64) == 0)
                return E_FAIL;

            setImageBase64(UINT64((intptr_t) pPEDecoder->GetPreferredBase()));
        }

        setFileAlignment   (VAL32(pPEDecoder->GetFileAlignment()));
        setSectionAlignment(VAL32(pPEDecoder->GetSectionAlignment()));

        hFile.SuppressRelease();
        hMapFile.SuppressRelease();

        m_hSeedFile = hFile;
        m_hSeedFileMap = hMapFile;
        m_pSeedFileDecoder = pPEDecoder;

#ifdef HOST_64BIT
        m_pSeedFileNTHeaders = pPEDecoder->GetNTHeaders64();
#else
        m_pSeedFileNTHeaders = pPEDecoder->GetNTHeaders32();
#endif

        // Add the seed sections

        m_pSeedSections = m_pSeedFileDecoder->FindFirstSection();

        m_pSeedSectionToAdd = m_pSeedSections;
        m_iSeedSections = m_pSeedFileDecoder->GetNumberOfSections();

        for (unsigned i = 0; i < m_iSeedSections; m_pSeedSectionToAdd++, i++) {
            PESection * dummy;
            getSectionCreate((const char *)(m_pSeedSectionToAdd->Name),
                VAL32(m_pSeedSectionToAdd->Characteristics),
                &dummy);
        }

        m_pSeedSectionToAdd = NULL;
    }

    return S_OK;
}

/******************************************************************/
HRESULT PEWriter::Cleanup() {

    if (m_hSeedFile != INVALID_HANDLE_VALUE)
    {
        CloseHandle(m_hSeedFile);
        CloseHandle(m_hSeedFileMap);
        delete m_pSeedFileDecoder;
    }

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

PESection* PEWriter::getSection(const char* name)
{
    int     len = (int)strlen(name);

    // the section name can be at most 8 characters including the null.
    if (len < 8)
        len++;
    else
        len = 8;

    // dbPrintf(("looking for section %s\n", name));
    // Skip over the seed sections

    for(PESection** cur = sectStart+m_iSeedSections; cur < sectCur; cur++) {
        // dbPrintf(("searching section %s\n", (*cur)->m_ame));
        if (strncmp((*cur)->m_name, name, len) == 0) {
            // dbPrintf(("found section %s\n", (*cur)->m_name));
            return(*cur);
        }
    }
    return(0);
}

HRESULT PEWriter::newSection(const char* name, PESection **section,
                            unsigned flags, unsigned estSize,
                            unsigned estRelocs)
{
    if (m_pSeedSectionToAdd) {
        _ASSERTE(strcmp((const char *)(m_pSeedSectionToAdd->Name), name) == 0 &&
            VAL32(m_pSeedSectionToAdd->Characteristics) == flags);

        PESeedSection * ret = new (nothrow) PESeedSection(m_pSeedFileDecoder, m_pSeedSectionToAdd);
        *section = ret;
        TESTANDRETURNMEMORY(ret);
        return S_OK;
    }

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

void PEWriter::setEnCRvaBase(ULONG dataBase, ULONG rdataBase)
{
    m_dataRvaBase = dataBase;
    m_rdataRvaBase = rdataBase;
    m_encMode = TRUE;
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
    unsigned            m_seedCount;

  public:
    SectionNameSorter(entry *entries, PEWriterSection ** sections, int count, unsigned seedSections)
      : CQuickSort<entry>(entries, count),
        m_entries(entries),
        m_sections(sections),
        m_count(unsigned(count)),
        m_seedCount(seedSections)
    {}

    // Sorts the entries according to alphabetical + numerical order

    int Compare(entry *first, entry *second)
    {
        PEWriterSection * firstSection = m_sections[first->arrayIndex];
        PEWriterSection * secondSection = m_sections[second->arrayIndex];

        // Seed sections are always at the start, in the order they were
        // added to the PEWriter

        if (firstSection->isSeedSection() || secondSection->isSeedSection()) {
            if (firstSection->isSeedSection() && secondSection->isSeedSection())
                return first->arrayIndex - second->arrayIndex;

            return firstSection->isSeedSection() ? -1 : 1;
        }

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

            // Seed sections should stay at the front
            _ASSERTE(i >= m_seedCount || i == e->arrayIndex);

            if (!m_sections[ePrev->arrayIndex]->isSeedSection() &&
                (ePrev->nameLength == e->nameLength) &&
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

    bool ExeOrDll = isExeOrDll(m_ntHeaders);

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

        //
        // Special case: omit "text0" from obj's
        //

        if (!ExeOrDll && strcmp((*cur)->m_name, ".text0") == 0)
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
        e->index = index;
        e->arrayIndex = (unsigned short)(cur - getSectStart());
        e++;
    }

    //
    // Sort the entries according to alphabetical + numerical order
    //

    SectionNameSorter sorter(entries, getSectStart(), int(e - entries), m_iSeedSections);
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
            && !getSectStart()[ePrev->arrayIndex]->isSeedSection()
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

        if (s->isSeedSection()) {
            virtualPos = s->getBaseRVA();
        }

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

    if (getSectStart()[sectionIndex]->isSeedSection()) {
        h->SectionIndex = VAL32(sectionIndex);
        return;
    }

    //
    // Reserve some dummy "array index" values for special sections
    // at the start of the image (after the seed sections)
    //

    static const char * const SpecialNames[] = { ".text", ".cormeta", NULL };
    enum { SPECIAL_NAMES_COUNT = NumItems(SpecialNames) };

    for (const char * const * s = SpecialNames; /**/; s++)
    {
        if (*s == 0)
        {
            h->SectionIndex = VAL32(sectionIndex + SPECIAL_NAMES_COUNT);
            break;
        }
        else if (strcmp((char *) h->Name, *s) == 0)
        {
            h->SectionIndex = VAL32(m_iSeedSections + DWORD(s - SpecialNames));
            break;
        }
    }

}


HRESULT PEWriter::link() {

    //
    // NOTE:
    // link() can be called more than once!  This is because at least one compiler
    // (the prejitter) needs to know the base addresses of some segments before it
    // builds others. It's up to the caller to insure the layout remains the same
    // after changes are made, though.
    //

    //
    // Assign base addresses to all sections, and layout & merge PE sections
    //

    bool ExeOrDll = isExeOrDll(m_ntHeaders);

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

        unsigned RoundUpVal;

        if (ExeOrDll)
        {
            RoundUpVal = 0x0200;
        }
        else
        {
            // Don't bother padding for objs
            RoundUpVal = 4;
        }

        m_ntHeaders->OptionalHeader.FileAlignment = VAL32(RoundUpVal);
    }

    //
    // Now, assign a section header & location to each section
    //

    if (ExeOrDll)
    {
        iUniqueSections++; // One more for .reloc
        filePos = sizeof(IMAGE_DOS_HEADER)+sizeof(x86StubPgm) + m_ntHeadersSize;
    }
    else
    {
        filePos = sizeof(IMAGE_FILE_HEADER);
    }

    m_ntHeaders->FileHeader.NumberOfSections = VAL16(iUniqueSections);

    filePos += iUniqueSections * sizeof(IMAGE_SECTION_HEADER);
    filePos  = roundUp(filePos, VAL32(m_ntHeaders->OptionalHeader.FileAlignment));

    m_ntHeaders->OptionalHeader.SizeOfHeaders = VAL32(filePos);

    virtualPos = roundUp(filePos, VAL32(m_ntHeaders->OptionalHeader.SectionAlignment));

    if (m_hSeedFile != INVALID_HANDLE_VALUE) {
        // We do not support relocating/sliding down the seed sections
        if (filePos > VAL32(m_pSeedSections->VirtualAddress) ||
            virtualPos > VAL32(m_pSeedSections->VirtualAddress))
           return E_FAIL;

        if (virtualPos < VAL32(m_pSeedSections->VirtualAddress)) {
            virtualPos = VAL32(m_pSeedSections->VirtualAddress);
        }
    }

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

#ifdef _PREFAST_
#pragma warning(push)
#pragma warning(disable:21000) // Suppress PREFast warning about overly large function
#endif
HRESULT PEWriter::fixup(CeeGenTokenMapper *pMapper)
{
    HRESULT hr;

    bool ExeOrDll = isExeOrDll(m_ntHeaders);
    const unsigned RoundUpVal = VAL32(m_ntHeaders->OptionalHeader.FileAlignment);

    if(ExeOrDll)
    {
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

#ifdef _DEBUG
        if (m_encMode)
            printf("Applying relocs for .rdata section with RVA %x\n", m_rdataRvaBase);
#endif

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
    } // end if(ExeOrDll)
    else //i.e., if OBJ
    {
        //
        // Clean up note:
        // I've cleaned up the executable linking path, but the .obj linking
        // is still a bit strange, what with a "extra" reloc & strtab sections
        // which are created after the linking step and get treated specially.
        //
        reloc = new (nothrow) PEWriterSection(".reloc",
                                    sdReadOnly | IMAGE_SCN_MEM_DISCARDABLE, 0x4000, 0);
        if(reloc == NULL) return E_OUTOFMEMORY;
        strtab = new (nothrow)  PEWriterSection(".strtab",
                                     sdReadOnly | IMAGE_SCN_MEM_DISCARDABLE, 0x4000, 0); //string table (if any)
        if(strtab == NULL) return E_OUTOFMEMORY;

        DWORD* TokInSymbolTable = new (nothrow) DWORD[16386];
        if (TokInSymbolTable == NULL) return E_OUTOFMEMORY;

        m_ntHeaders->FileHeader.SizeOfOptionalHeader = 0;
        //For each section set VirtualAddress to 0
        PEWriterSection **cur;
        for(cur = getSectStart(); cur < getSectCur(); cur++)
        {
            IMAGE_SECTION_HEADER* header = (*cur)->m_header;
            header->VirtualAddress = VAL32(0);
        }
        // Go over section relocations and build the Symbol Table, use .reloc section as buffer:
        DWORD tk=0, rva=0, NumberOfSymbols=0;
        BOOL  ToRelocTable = FALSE;
        IMAGE_SYMBOL is;
        IMAGE_RELOCATION ir;
        ULONG StrTableLen = 4; //size itself only
        char* szSymbolName = NULL;
        char* pch;

        PESection *textSection;
        getSectionCreate(".text", 0, &textSection);

        for(PESectionReloc* rcur = textSection->m_relocStart; rcur < textSection->m_relocCur; rcur++)
        {
            switch((int)rcur->type)
            {
                case 0x7FFA: // Ptr to symbol name
#ifdef HOST_64BIT
                    _ASSERTE(!"this is probably broken!!");
#endif // HOST_64BIT
                    szSymbolName = (char*)(UINT_PTR)(rcur->offset);
                    break;

                case 0x7FFC: // Ptr to file name
                    TokInSymbolTable[NumberOfSymbols++] = 0;
                    memset(&is,0,sizeof(IMAGE_SYMBOL));
                    memcpy(is.N.ShortName,".file\0\0\0",8);
                    is.Value = 0;
                    is.SectionNumber = VAL16(IMAGE_SYM_DEBUG);
                    is.Type = VAL16(IMAGE_SYM_DTYPE_NULL);
                    is.StorageClass = IMAGE_SYM_CLASS_FILE;
                    is.NumberOfAuxSymbols = 1;
                    if((pch = reloc->getBlock(sizeof(IMAGE_SYMBOL))))
                        memcpy(pch,&is,sizeof(IMAGE_SYMBOL));
                    else return E_OUTOFMEMORY;
                    TokInSymbolTable[NumberOfSymbols++] = 0;
                    memset(&is,0,sizeof(IMAGE_SYMBOL));
#ifdef HOST_64BIT
                    _ASSERTE(!"this is probably broken!!");
#endif // HOST_64BIT
                    strcpy_s((char*)&is,sizeof(is),(char*)(UINT_PTR)(rcur->offset));
                    if((pch = reloc->getBlock(sizeof(IMAGE_SYMBOL))))
                        memcpy(pch,&is,sizeof(IMAGE_SYMBOL));
                    else return E_OUTOFMEMORY;
#ifdef HOST_64BIT
                    _ASSERTE(!"this is probably broken!!");
#endif // HOST_64BIT
                    delete (char*)(UINT_PTR)(rcur->offset);
                    ToRelocTable = FALSE;
                    tk = 0;
                    szSymbolName = NULL;
                    break;

                case 0x7FFB: // compid value
                    TokInSymbolTable[NumberOfSymbols++] = 0;
                    memset(&is,0,sizeof(IMAGE_SYMBOL));
                    memcpy(is.N.ShortName,"@comp.id",8);
                    is.Value = VAL32(rcur->offset);
                    is.SectionNumber = VAL16(IMAGE_SYM_ABSOLUTE);
                    is.Type = VAL16(IMAGE_SYM_DTYPE_NULL);
                    is.StorageClass = IMAGE_SYM_CLASS_STATIC;
                    is.NumberOfAuxSymbols = 0;
                    if((pch = reloc->getBlock(sizeof(IMAGE_SYMBOL))))
                        memcpy(pch,&is,sizeof(IMAGE_SYMBOL));
                    else return E_OUTOFMEMORY;
                    ToRelocTable = FALSE;
                    tk = 0;
                    szSymbolName = NULL;
                    break;

                case 0x7FFF: // Token value, def
                    tk = rcur->offset;
                    ToRelocTable = FALSE;
                    break;

                case 0x7FFE: //Token value, ref
                    tk = rcur->offset;
                    ToRelocTable = TRUE;
                    break;

                case 0x7FFD: //RVA value
                    rva = rcur->offset;
                    if(tk)
                    {
                        // Add to SymbolTable
                        DWORD i;
                        for(i = 0; (i < NumberOfSymbols)&&(tk != TokInSymbolTable[i]); i++);
                        if(i == NumberOfSymbols)
                        {
                            if(szSymbolName && *szSymbolName) // Add "extern" symbol and string table entry
                            {
                                TokInSymbolTable[NumberOfSymbols++] = 0;
                                memset(&is,0,sizeof(IMAGE_SYMBOL));
                                i++; // so reloc record (if generated) points to COM token symbol
                                is.N.Name.Long = VAL32(StrTableLen);
                                is.SectionNumber = VAL16(1); //textSection is the first one
                                is.StorageClass = IMAGE_SYM_CLASS_EXTERNAL;
                                is.NumberOfAuxSymbols = 0;
                                is.Value = VAL32(rva);
                                if(TypeFromToken(tk) == mdtMethodDef)
                                {
                                    is.Type = VAL16(0x20); //IMAGE_SYM_DTYPE_FUNCTION;
                                }
                                if((pch = reloc->getBlock(sizeof(IMAGE_SYMBOL))))
                                    memcpy(pch,&is,sizeof(IMAGE_SYMBOL));
                                else return E_OUTOFMEMORY;
                                DWORD l = (DWORD)(strlen(szSymbolName)+1); // don't forget zero terminator!
                                if((pch = reloc->getBlock(1)))
                                    memcpy(pch,szSymbolName,1);
                                else return E_OUTOFMEMORY;
                                delete szSymbolName;
                                StrTableLen += l;
                            }
                            TokInSymbolTable[NumberOfSymbols++] = tk;
                            memset(&is,0,sizeof(IMAGE_SYMBOL));
                            sprintf_s((char*)is.N.ShortName,sizeof(is.N.ShortName),"%08X",tk);
                            is.SectionNumber = VAL16(1); //textSection is the first one
                            is.StorageClass = 0x6B; //IMAGE_SYM_CLASS_COM_TOKEN;
                            is.Value = VAL32(rva);
                            if(TypeFromToken(tk) == mdtMethodDef)
                            {
                                is.Type = VAL16(0x20); //IMAGE_SYM_DTYPE_FUNCTION;
                                //is.NumberOfAuxSymbols = 1;
                            }
                            if((pch = reloc->getBlock(sizeof(IMAGE_SYMBOL))))
                                memcpy(pch,&is,sizeof(IMAGE_SYMBOL));
                            else return E_OUTOFMEMORY;
                            if(is.NumberOfAuxSymbols == 1)
                            {
                                BYTE dummy[sizeof(IMAGE_SYMBOL)];
                                memset(dummy,0,sizeof(IMAGE_SYMBOL));
                                dummy[0] = dummy[2] = 1;
                                if((pch = reloc->getBlock(sizeof(IMAGE_SYMBOL))))
                                    memcpy(pch,dummy,sizeof(IMAGE_SYMBOL));
                                else return E_OUTOFMEMORY;
                                TokInSymbolTable[NumberOfSymbols++] = 0;
                            }
                        }
                        if(ToRelocTable)
                        {
                            IMAGE_SECTION_HEADER* phdr = textSection->m_header;
                            // Add to reloc table
                            ir.VirtualAddress = VAL32(rva);
                            ir.SymbolTableIndex = VAL32(i);
                            ir.Type = VAL16(IMAGE_REL_I386_SECREL);
                            if(phdr->PointerToRelocations == 0)
                                phdr->PointerToRelocations = VAL32(VAL32(phdr->PointerToRawData) + VAL32(phdr->SizeOfRawData));
                            phdr->NumberOfRelocations = VAL32(VAL32(phdr->NumberOfRelocations) + 1);
                            if((pch = reloc->getBlock(sizeof(IMAGE_RELOCATION))))
                                memcpy(pch,&is,sizeof(IMAGE_RELOCATION));
                            else return E_OUTOFMEMORY;
                        }
                    }
                    ToRelocTable = FALSE;
                    tk = 0;
                    szSymbolName = NULL;
                    break;

                default:
                    break;
            } //end switch(cur->type)
        } // end for all relocs
        // Add string table counter:
        if((pch = reloc->getBlock(sizeof(ULONG))))
            memcpy(pch,&StrTableLen,sizeof(ULONG));
        else return E_OUTOFMEMORY;
        reloc->m_header->Misc.VirtualSize = VAL32(reloc->dataLen());
        if(NumberOfSymbols)
        {
            // recompute the actual sizes and positions of all the sections
            filePos = roundUp(VAL16(m_ntHeaders->FileHeader.NumberOfSections) * sizeof(IMAGE_SECTION_HEADER)+
                              sizeof(IMAGE_FILE_HEADER), RoundUpVal);
            for(cur = getSectStart(); cur < getSectCur(); cur++)
            {
                IMAGE_SECTION_HEADER* header = (*cur)->m_header;
                header->Misc.VirtualSize = VAL32((*cur)->dataLen());
                header->VirtualAddress = VAL32(0);
                header->SizeOfRawData = VAL32(roundUp(VAL32(header->Misc.VirtualSize), RoundUpVal));
                header->PointerToRawData = VAL32(filePos);

                filePos += VAL32(header->SizeOfRawData);
            }
            m_ntHeaders->FileHeader.Machine = VAL16(0xC0EE); //COM+ EE
            m_ntHeaders->FileHeader.PointerToSymbolTable = VAL32(filePos);
            m_ntHeaders->FileHeader.NumberOfSymbols = VAL32(NumberOfSymbols);
            filePos += roundUp(VAL32(reloc->m_header->Misc.VirtualSize)+strtab->dataLen(),RoundUpVal);
        }
        delete[] TokInSymbolTable;
    } //end if OBJ

    const unsigned headerOffset = (unsigned) (ExeOrDll ? sizeof(IMAGE_DOS_HEADER) + sizeof(x86StubPgm) : 0);

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
#ifdef _PREFAST_
#pragma warning(pop)
#endif

HRESULT PEWriter::Open(__in LPCWSTR fileName)
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
HRESULT PEWriter::write(__in LPCWSTR fileName) {

    HRESULT hr;

    bool ExeOrDll;
    unsigned RoundUpVal;
    ExeOrDll = isExeOrDll(m_ntHeaders);
    RoundUpVal = VAL32(m_ntHeaders->OptionalHeader.FileAlignment);

    IfFailGo(Open(fileName));

    if(ExeOrDll)
    {
        // write the PE headers
        IfFailGo(Write(&m_dosHeader, sizeof(IMAGE_DOS_HEADER)));
        IfFailGo(Write(x86StubPgm, sizeof(x86StubPgm)));
        IfFailGo(Write(m_ntHeaders, m_ntHeadersSize));
    }
    else
    {
        // write the object file header
        IfFailGo(Write(&(m_ntHeaders->FileHeader),sizeof(IMAGE_FILE_HEADER)));
    }

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

    // writes for an object file
    if (!ExeOrDll)
    {
        // write the relocs section (Does nothing if relocs section is empty)
        IfFailGo(reloc->write(m_file));
        //write string table (obj only, empty for exe or dll)
        IfFailGo(strtab->write(m_file));
        int lena = padLen(VAL32(reloc->m_header->Misc.VirtualSize)+strtab->dataLen(), RoundUpVal);
        if (lena > 0)
            IfFailGo(Write(NULL, lena));
    }

    return Close();

 ErrExit:
    Close();

    return hr;
}

HRESULT PEWriter::write(void ** ppImage)
{
    bool ExeOrDll = isExeOrDll(m_ntHeaders);
    const unsigned RoundUpVal = VAL32(m_ntHeaders->OptionalHeader.FileAlignment);
    char *pad = (char *) _alloca(RoundUpVal);
    memset(pad, 0, RoundUpVal);

    size_t lSize = filePos;
    if (!ExeOrDll)
    {
        lSize += reloc->dataLen();
        lSize += strtab->dataLen();
        lSize += padLen(VAL32(reloc->m_header->Misc.VirtualSize)+strtab->dataLen(),
                        RoundUpVal);
    }

    // allocate the block we are handing back to the caller
    void * pImage = (void *) ::CoTaskMemAlloc(lSize);
    if (NULL == pImage)
    {
        return E_OUTOFMEMORY;
    }

    // zero the memory
    ::memset(pImage, 0, lSize);

    char *pCur = (char *)pImage;

    if(ExeOrDll)
    {
        // PE Header
        COPY_AND_ADVANCE(pCur, &m_dosHeader, sizeof(IMAGE_DOS_HEADER));
        COPY_AND_ADVANCE(pCur, x86StubPgm, sizeof(x86StubPgm));
        COPY_AND_ADVANCE(pCur, m_ntHeaders, m_ntHeadersSize);
    }
    else
    {
        COPY_AND_ADVANCE(pCur, &(m_ntHeaders->FileHeader), sizeof(IMAGE_FILE_HEADER));
    }

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

    // !!! Need to jump to the right place...

    if (!ExeOrDll)
    {
        // now the relocs (exe, dll) or symbol table (obj) (if any)
        // write the relocs section (Does nothing if relocs section is empty)
        reloc->writeMem((void **)&pCur);

        //write string table (obj only, empty for exe or dll)
        strtab->writeMem((void **)&pCur);

        // final pad
        size_t len = padLen(VAL32(reloc->m_header->Misc.VirtualSize)+strtab->dataLen(), RoundUpVal);
        if (len > 0)
        {
            // WARNING: macro - must enclose in curly braces
            COPY_AND_ADVANCE(pCur, pad, len);
        }
    }

    // make sure we wrote the exact numbmer of bytes expected
    _ASSERTE(lSize == (size_t) (pCur - (char *)pImage));

    // give pointer to memory image back to caller (who must free with ::CoTaskMemFree())
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
    _ASSERTE(m_hSeedFile == INVALID_HANDLE_VALUE);

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
