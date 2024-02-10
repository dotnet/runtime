// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// ===================================================================================================
// Portions of the code implemented below are based on the 'Berkeley SoftFloat Release 3e' algorithms.
// ===================================================================================================

/*XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XX                                                                           XX
XX                                  Utils.cpp                                XX
XX                                                                           XX
XX   Has miscellaneous utility functions                                     XX
XX                                                                           XX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
*/

#include "jitpch.h"
#ifdef _MSC_VER
#pragma hdrstop
#endif

#include "opcode.h"
#include "jitstd/algorithm.h"

#include <dn-u16.h> // for u16_strtod

/*****************************************************************************/

#define DECLARE_DATA

// clang-format off
extern
const signed char       opcodeSizes[] =
{
    #define InlineNone_size           0
    #define ShortInlineVar_size       1
    #define InlineVar_size            2
    #define ShortInlineI_size         1
    #define InlineI_size              4
    #define InlineI8_size             8
    #define ShortInlineR_size         4
    #define InlineR_size              8
    #define ShortInlineBrTarget_size  1
    #define InlineBrTarget_size       4
    #define InlineMethod_size         4
    #define InlineField_size          4
    #define InlineType_size           4
    #define InlineString_size         4
    #define InlineSig_size            4
    #define InlineRVA_size            4
    #define InlineTok_size            4
    #define InlineSwitch_size         0       // for now
    #define InlinePhi_size            0       // for now
    #define InlineVarTok_size         0       // remove

    #define OPDEF(name,string,pop,push,oprType,opcType,l,s1,s2,ctrl) oprType ## _size ,
    #include "opcode.def"
    #undef OPDEF

    #undef InlineNone_size
    #undef ShortInlineVar_size
    #undef InlineVar_size
    #undef ShortInlineI_size
    #undef InlineI_size
    #undef InlineI8_size
    #undef ShortInlineR_size
    #undef InlineR_size
    #undef ShortInlineBrTarget_size
    #undef InlineBrTarget_size
    #undef InlineMethod_size
    #undef InlineField_size
    #undef InlineType_size
    #undef InlineString_size
    #undef InlineSig_size
    #undef InlineRVA_size
    #undef InlineTok_size
    #undef InlineSwitch_size
    #undef InlinePhi_size
};
// clang-format on

const BYTE varTypeClassification[] = {
#define DEF_TP(tn, nm, jitType, sz, sze, asze, st, al, regTyp, regFld, csr, ctr, tf) tf,
#include "typelist.h"
#undef DEF_TP
};

const BYTE varTypeRegister[] = {
#define DEF_TP(tn, nm, jitType, sz, sze, asze, st, al, regTyp, regFld, csr, ctr, tf) regTyp,
#include "typelist.h"
#undef DEF_TP
};

/*****************************************************************************/
/*****************************************************************************/
#ifdef DEBUG
extern const char* const opcodeNames[] = {
#define OPDEF(name, string, pop, push, oprType, opcType, l, s1, s2, ctrl) string,
#include "opcode.def"
#undef OPDEF
};

extern const BYTE opcodeArgKinds[] = {
#define OPDEF(name, string, pop, push, oprType, opcType, l, s1, s2, ctrl) (BYTE) oprType,
#include "opcode.def"
#undef OPDEF
};
#endif

/*****************************************************************************/

const char* varTypeName(var_types vt)
{
    static const char* const varTypeNames[] = {
#define DEF_TP(tn, nm, jitType, sz, sze, asze, st, al, regTyp, regFld, csr, ctr, tf) nm,
#include "typelist.h"
#undef DEF_TP
    };

    assert((unsigned)vt < ArrLen(varTypeNames));

    return varTypeNames[vt];
}

/*****************************************************************************
 *
 *  Return the name of the given register.
 */

const char* getRegName(regNumber reg)
{
    // Special-case REG_NA; it's not in the regNames array, but we might want to print it.
    if (reg == REG_NA)
    {
        return "NA";
    }

    static const char* const regNames[] = {
#if defined(TARGET_ARM64)
#define REGDEF(name, rnum, mask, xname, wname) xname,
#else
#define REGDEF(name, rnum, mask, sname) sname,
#endif
#include "register.h"
    };
    assert(reg < ArrLen(regNames));
    return regNames[reg];
}

const char* getRegName(unsigned reg) // this is for gcencode.cpp and disasm.cpp that dont use the regNumber type
{
    return getRegName((regNumber)reg);
}

#if defined(DEBUG)

const char* getRegNameFloat(regNumber reg, var_types type)
{
#ifdef TARGET_ARM
    assert(genIsValidFloatReg(reg));
    if (type == TYP_FLOAT)
        return getRegName(reg);
    else
    {
        const char* regName;

        switch (reg)
        {
            default:
                assert(!"Bad double register");
                regName = "d??";
                break;
            case REG_F0:
                regName = "d0";
                break;
            case REG_F2:
                regName = "d2";
                break;
            case REG_F4:
                regName = "d4";
                break;
            case REG_F6:
                regName = "d6";
                break;
            case REG_F8:
                regName = "d8";
                break;
            case REG_F10:
                regName = "d10";
                break;
            case REG_F12:
                regName = "d12";
                break;
            case REG_F14:
                regName = "d14";
                break;
            case REG_F16:
                regName = "d16";
                break;
            case REG_F18:
                regName = "d18";
                break;
            case REG_F20:
                regName = "d20";
                break;
            case REG_F22:
                regName = "d22";
                break;
            case REG_F24:
                regName = "d24";
                break;
            case REG_F26:
                regName = "d26";
                break;
            case REG_F28:
                regName = "d28";
                break;
            case REG_F30:
                regName = "d30";
                break;
        }
        return regName;
    }

#elif defined(TARGET_ARM64)

    static const char* regNamesFloat[] = {
#define REGDEF(name, rnum, mask, xname, wname) xname,
#include "register.h"
    };
    assert((unsigned)reg < ArrLen(regNamesFloat));

    return regNamesFloat[reg];

#elif defined(TARGET_LOONGARCH64)

    static const char* regNamesFloat[] = {
#define REGDEF(name, rnum, mask, sname) sname,
#include "register.h"
    };

    assert((unsigned)reg < ArrLen(regNamesFloat));

    return regNamesFloat[reg];

#else
    static const char* regNamesFloat[] = {
#define REGDEF(name, rnum, mask, sname) "x" sname,
#include "register.h"
    };
#ifdef FEATURE_SIMD
    static const char* regNamesYMM[] = {
#define REGDEF(name, rnum, mask, sname) "y" sname,
#include "register.h"
    };
    static const char* regNamesZMM[] = {
#define REGDEF(name, rnum, mask, sname) "z" sname,
#include "register.h"
    };
#endif // FEATURE_SIMD
    assert((unsigned)reg < ArrLen(regNamesFloat));

#if defined(FEATURE_SIMD) && defined(TARGET_XARCH)
    if (type == TYP_SIMD64)
    {
        return regNamesZMM[reg];
    }
    else if (type == TYP_SIMD32)
    {
        return regNamesYMM[reg];
    }
#endif // FEATURE_SIMD && TARGET_XARCH

    return regNamesFloat[reg];
#endif
}

/*****************************************************************************
 *
 *  Displays a range of registers
 *   -- This is a helper used by dspRegMask
 */
const char* dspRegRange(regMaskMixed regMask, size_t& minSiz, const char* sep, regNumber regFirst, regNumber regLast)
{
#ifdef TARGET_XARCH
    assert(((regFirst == REG_INT_FIRST) && (regLast == REG_INT_LAST)) ||
           ((regFirst == REG_FP_FIRST) && (regLast == REG_FP_LAST)) ||
           ((regFirst == REG_MASK_FIRST) && (regLast == REG_MASK_LAST)));
#else
    assert(((regFirst == REG_INT_FIRST) && (regLast == REG_INT_LAST)) ||
           ((regFirst == REG_FP_FIRST) && (regLast == REG_FP_LAST)));
#endif

    if (strlen(sep) > 0)
    {
        // We've already printed something.
        sep = " ";
    }

    bool      inRegRange = false;
    regNumber regPrev    = REG_NA;
    regNumber regHead    = REG_NA; // When we start a range, remember the first register of the range, so we don't use
                                   // range notation if the range contains just a single register.

    for (regNumber regNum = regFirst; regNum <= regLast; regNum = REG_NEXT(regNum))
    {
        singleRegMask regBit = genRegMask(regNum);

        if ((regMask & regBit) != 0)
        {
            // We have a register to display. It gets displayed now if:
            // 1. This is the first register to display of a new range of registers (possibly because
            //    no register has ever been displayed).
            // 2. This is the last register of an acceptable range (either the last register of a type,
            //    or the last of a range that is displayed with range notation).
            if (!inRegRange)
            {
                // It's the first register of a potential range.
                const char* nam = getRegName(regNum);
                printf("%s%s", sep, nam);
                minSiz -= strlen(sep) + strlen(nam);

                // What kind of separator should we use for this range (if it is indeed going to be a range)?
                CLANG_FORMAT_COMMENT_ANCHOR;

                if (genIsValidIntReg(regNum))
                {
                    // By default, we're not starting a potential register range.
                    sep = " ";

#if defined(TARGET_AMD64)
                    // For AMD64, create ranges for int registers R8 through R15, but not the "old" registers.
                    if (regNum >= REG_R8)
                    {
                        regHead    = regNum;
                        inRegRange = true;
                        sep        = "-";
                    }
#elif defined(TARGET_ARM64)
                    // R17 and R28 can't be the start of a range, since the range would include TEB or FP
                    if ((regNum < REG_R17) || ((REG_R19 <= regNum) && (regNum < REG_R28)))
                    {
                        regHead    = regNum;
                        inRegRange = true;
                        sep        = "-";
                    }
#elif defined(TARGET_ARM)
                    if (regNum < REG_R12)
                    {
                        regHead    = regNum;
                        inRegRange = true;
                        sep        = "-";
                    }
#elif defined(TARGET_X86)
                    // No register ranges
                    CLANG_FORMAT_COMMENT_ANCHOR;
#elif defined(TARGET_LOONGARCH64)
                    if (REG_A0 <= regNum && regNum <= REG_T8)
                    {
                        regHead    = regNum;
                        inRegRange = true;
                        sep        = "-";
                    }
#elif defined(TARGET_RISCV64)
                    if ((REG_A0 <= regNum && REG_A7 >= regNum) || REG_T0 == regNum || REG_T1 == regNum ||
                        (REG_T2 <= regNum && REG_T6 >= regNum))
                    {
                        regHead    = regNum;
                        inRegRange = true;
                        sep        = "-";
                    }
#else // TARGET*
#error Unsupported or unset target architecture
#endif // TARGET*
                }
                else
                {
                    regHead    = regNum;
                    inRegRange = true;
                    sep        = "-";
                }
            }
#if defined(TARGET_ARM64)
            else if ((regNum == regLast) || (regNum == REG_R17) // last register before TEB
                     || (regNum == REG_R28))                    // last register before FP
#elif defined(TARGET_LOONGARCH64)
            else if ((regNum == regLast) || (regNum == REG_A7) || (regNum == REG_T8))
#else
            else if (regNum == regLast)
#endif
            {
                // We've already printed a register and hit the end of a range

                const char* nam = getRegName(regNum);
                printf("%s%s", sep, nam);
                minSiz -= strlen(sep) + strlen(nam);

                regHead    = REG_NA;
                inRegRange = false; // No longer in the middle of a register range
                sep        = " ";
            }
        }
        else if (inRegRange)
        {
            assert(regHead != REG_NA);

            if (regPrev != regHead)
            {
                // Close out the previous range, if it included more than one register.
                const char* nam = getRegName(regPrev);
                printf("%s%s", sep, nam);
                minSiz -= (strlen(sep) + strlen(nam));
            }

            regHead    = REG_NA;
            inRegRange = false;
            sep        = " ";
        }

        if (regBit > regMask)
        {
            break;
        }

        regPrev = regNum;
    }

    return sep;
}

/*****************************************************************************
 *
 *  Displays a register set.
 *  TODO-ARM64-Cleanup: don't allow ip0, ip1 as part of a range.
 */
void dspRegMask(regMaskMixed regMask, size_t minSiz)
{
    const char* sep = "";

    printf("[");

    sep = dspRegRange(regMask, minSiz, sep, REG_INT_FIRST, REG_INT_LAST);
    sep = dspRegRange(regMask, minSiz, sep, REG_FP_FIRST, REG_FP_LAST);

#ifdef TARGET_XARCH
    sep = dspRegRange(regMask, minSiz, sep, REG_MASK_FIRST, REG_MASK_LAST);
#endif // TARGET_XARCH

    printf("]");

    while ((int)minSiz > 0)
    {
        printf(" ");
        minSiz--;
    }
}

//------------------------------------------------------------------------
// dumpILBytes: Helper for dumpSingleInstr() to dump hex bytes of an IL stream,
// aligning up to a minimum alignment width.
//
// Arguments:
//    codeAddr  - Pointer to IL byte stream to display.
//    codeSize  - Number of bytes of IL byte stream to display.
//    alignSize - Pad out to this many characters, if fewer than this were written.
//
void dumpILBytes(const BYTE* const codeAddr,
                 unsigned          codeSize,
                 unsigned          alignSize) // number of characters to write, for alignment
{
    for (IL_OFFSET offs = 0; offs < codeSize; ++offs)
    {
        printf(" %02x", *(codeAddr + offs));
    }

    unsigned charsWritten = 3 * codeSize;
    for (unsigned i = charsWritten; i < alignSize; i++)
    {
        printf(" ");
    }
}

//------------------------------------------------------------------------
// dumpSingleInstr: Display a single IL instruction.
//
// Arguments:
//    codeAddr  - Base pointer to a stream of IL instructions.
//    offs      - Offset from codeAddr of the IL instruction to display.
//    prefix    - Optional string to prefix the IL instruction with (if nullptr, no prefix is output).
//
// Return Value:
//    Size of the displayed IL instruction in the instruction stream, in bytes. (Add this to 'offs' to
//    get to the next instruction.)
//
unsigned dumpSingleInstr(const BYTE* const codeAddr, IL_OFFSET offs, const char* prefix)
{
    const BYTE*    opcodePtr      = codeAddr + offs;
    const BYTE*    startOpcodePtr = opcodePtr;
    const unsigned ALIGN_WIDTH    = 3 * 6; // assume 3 characters * (1 byte opcode + 4 bytes data + 1 prefix byte) for
                                           // most things

    if (prefix != nullptr)
    {
        printf("%s", prefix);
    }

    OPCODE opcode = (OPCODE)getU1LittleEndian(opcodePtr);
    opcodePtr += sizeof(__int8);

DECODE_OPCODE:

    if (opcode >= CEE_COUNT)
    {
        printf("\nIllegal opcode: %02X\n", (int)opcode);
        return (IL_OFFSET)(opcodePtr - startOpcodePtr);
    }

    /* Get the size of additional parameters */

    size_t   sz      = opcodeSizes[opcode];
    unsigned argKind = opcodeArgKinds[opcode];

    /* See what kind of an opcode we have, then */

    switch (opcode)
    {
        case CEE_PREFIX1:
            opcode = OPCODE(getU1LittleEndian(opcodePtr) + 256);
            opcodePtr += sizeof(__int8);
            goto DECODE_OPCODE;

        default:
        {
            __int64 iOp;
            double  dOp;
            int     jOp;
            DWORD   jOp2;

            switch (argKind)
            {
                case InlineNone:
                    dumpILBytes(startOpcodePtr, (unsigned)(opcodePtr - startOpcodePtr), ALIGN_WIDTH);
                    printf(" %-12s", opcodeNames[opcode]);
                    break;

                case ShortInlineVar:
                    iOp = getU1LittleEndian(opcodePtr);
                    goto INT_OP;
                case ShortInlineI:
                    iOp = getI1LittleEndian(opcodePtr);
                    goto INT_OP;
                case InlineVar:
                    iOp = getU2LittleEndian(opcodePtr);
                    goto INT_OP;
                case InlineTok:
                case InlineMethod:
                case InlineField:
                case InlineType:
                case InlineString:
                case InlineSig:
                case InlineI:
                    iOp = getI4LittleEndian(opcodePtr);
                    goto INT_OP;
                case InlineI8:
                    iOp = getU4LittleEndian(opcodePtr);
                    iOp |= (__int64)getU4LittleEndian(opcodePtr + 4) << 32;
                    goto INT_OP;

                INT_OP:
                    dumpILBytes(startOpcodePtr, (unsigned)((opcodePtr - startOpcodePtr) + sz), ALIGN_WIDTH);
                    printf(" %-12s 0x%X", opcodeNames[opcode], iOp);
                    break;

                case ShortInlineR:
                    dOp = FloatingPointUtils::convertToDouble(getR4LittleEndian(opcodePtr));
                    goto FLT_OP;
                case InlineR:
                    dOp = getR8LittleEndian(opcodePtr);
                    goto FLT_OP;

                FLT_OP:
                    dumpILBytes(startOpcodePtr, (unsigned)((opcodePtr - startOpcodePtr) + sz), ALIGN_WIDTH);
                    printf(" %-12s %f", opcodeNames[opcode], dOp);
                    break;

                case ShortInlineBrTarget:
                    jOp = getI1LittleEndian(opcodePtr);
                    goto JMP_OP;
                case InlineBrTarget:
                    jOp = getI4LittleEndian(opcodePtr);
                    goto JMP_OP;

                JMP_OP:
                    dumpILBytes(startOpcodePtr, (unsigned)((opcodePtr - startOpcodePtr) + sz), ALIGN_WIDTH);
                    printf(" %-12s %d (IL_%04x)", opcodeNames[opcode], jOp, (int)(opcodePtr + sz - codeAddr) + jOp);
                    break;

                case InlineSwitch:
                    jOp2 = getU4LittleEndian(opcodePtr);
                    opcodePtr += 4;
                    opcodePtr += jOp2 * 4; // Jump over the table
                    dumpILBytes(startOpcodePtr, (unsigned)(opcodePtr - startOpcodePtr), ALIGN_WIDTH);
                    printf(" %-12s", opcodeNames[opcode]);
                    break;

                case InlinePhi:
                    jOp2 = getU1LittleEndian(opcodePtr);
                    opcodePtr += 1;
                    opcodePtr += jOp2 * 2; // Jump over the table
                    dumpILBytes(startOpcodePtr, (unsigned)(opcodePtr - startOpcodePtr), ALIGN_WIDTH);
                    printf(" %-12s", opcodeNames[opcode]);
                    break;

                default:
                    assert(!"Bad argKind");
            }

            opcodePtr += sz;
            break;
        }
    }

    printf("\n");
    return (IL_OFFSET)(opcodePtr - startOpcodePtr);
}

//------------------------------------------------------------------------
// dumpILRange: Display a range of IL instructions from an IL instruction stream.
//
// Arguments:
//    codeAddr  - Pointer to IL byte stream to display.
//    codeSize  - Number of bytes of IL byte stream to display.
//
void dumpILRange(const BYTE* const codeAddr, unsigned codeSize) // in bytes
{
    for (IL_OFFSET offs = 0; offs < codeSize;)
    {
        char prefix[100];
        sprintf_s(prefix, ArrLen(prefix), "IL_%04x ", offs);
        unsigned codeBytesDumped = dumpSingleInstr(codeAddr, offs, prefix);
        offs += codeBytesDumped;
    }
}

/*****************************************************************************
 *
 *  Display a variable set.
 */
const char* genES2str(BitVecTraits* traits, EXPSET_TP set)
{
    const int    bufSize = 65; // Supports a BitVec of up to 256 bits
    static char  num1[bufSize];
    static char  num2[bufSize];
    static char* nump = num1;

    assert(bufSize > roundUp(BitVecTraits::GetSize(traits), (unsigned)sizeof(char)) / 8);

    char* temp = nump;
    nump       = (nump == num1) ? num2 : num1;
    sprintf_s(temp, bufSize, "%s", BitVecOps::ToString(traits, set));

    return temp;
}

//------------------------------------------------------------------------
// refCntWtd2str: Return a string representation of a weighted ref count
//
// Arguments:
//    refCntWtd - weight to format
//    padForDecimalPlaces - (default: false) If true, pad any integral or non-numeric
//                          output on the right with three spaces, representing space
//                          for ".00". This makes "1" line up with "2.34" at the "2" column.
//                          This is used for formatting the BasicBlock list.
//
const char* refCntWtd2str(weight_t refCntWtd, bool padForDecimalPlaces)
{
    const int    bufSize = 17;
    static char  num1[bufSize];
    static char  num2[bufSize];
    static char* nump = num1;

    char* temp = nump;

    const char* strDecimalPaddingString = "";
    if (padForDecimalPlaces)
    {
        strDecimalPaddingString = "   ";
    }

    nump = (nump == num1) ? num2 : num1;

    if (refCntWtd >= BB_MAX_WEIGHT)
    {
        sprintf_s(temp, bufSize, "MAX%s", strDecimalPaddingString);
    }
    else
    {
        weight_t scaledWeight = refCntWtd / BB_UNITY_WEIGHT;
        weight_t intPart      = (weight_t)floor(scaledWeight);
        bool     isLarge      = intPart > 1e9;
        bool     isSmall      = (intPart < 1e-2) && (intPart != 0);

        // Use g format for high dynamic range counts.
        //
        if (isLarge || isSmall)
        {
            sprintf_s(temp, bufSize, "%.2g", scaledWeight);
        }
        else
        {
            if (intPart == scaledWeight)
            {
                sprintf_s(temp, bufSize, "%lld%s", (long long)intPart, strDecimalPaddingString);
            }
            else
            {
                sprintf_s(temp, bufSize, "%.2f", scaledWeight);
            }
        }
    }
    return temp;
}

#endif // DEBUG

#if defined(DEBUG)

//------------------------------------------------------------------------
// Contains: check if the range includes a particular hash
//
// Arguments:
//    hash -- hash value to check

bool ConfigMethodRange::Contains(unsigned hash)
{
    _ASSERT(m_inited == 1);

    // No ranges specified means all methods included.
    if (m_lastRange == 0)
    {
        return true;
    }

    for (unsigned i = 0; i < m_lastRange; i++)
    {
        if ((m_ranges[i].m_low <= hash) && (hash <= m_ranges[i].m_high))
        {
            return true;
        }
    }

    return false;
}

//------------------------------------------------------------------------
// InitRanges: parse the range string and set up the range info
//
// Arguments:
//    rangeStr -- string to parse (may be nullptr)
//    capacity -- number ranges to allocate in the range array
//
// Notes:
//    Does some internal error checking; clients can use Error()
//    to determine if the range string couldn't be fully parsed
//    because of bad characters or too many entries, or had values
//    that were too large to represent.

void ConfigMethodRange::InitRanges(const WCHAR* rangeStr, unsigned capacity)
{
    // Make sure that the memory was zero initialized
    assert(m_inited == 0 || m_inited == 1);
    assert(m_entries == 0);
    assert(m_ranges == nullptr);
    assert(m_lastRange == 0);

    // Flag any strange-looking requests
    assert(capacity < 100000);

    if (rangeStr == nullptr)
    {
        m_inited = 1;
        return;
    }

    // Allocate some persistent memory
    ICorJitHost* jitHost = g_jitHost;
    m_ranges             = (Range*)jitHost->allocateMemory(capacity * sizeof(Range));
    m_entries            = capacity;

    const WCHAR* p           = rangeStr;
    unsigned     lastRange   = 0;
    bool         setHighPart = false;

    while ((*p != 0) && (lastRange < m_entries))
    {
        while ((*p == L' ') || (*p == L','))
        {
            p++;
        }

        int i = 0;

        while (((L'0' <= *p) && (*p <= L'9')) || ((L'A' <= *p) && (*p <= L'F')) || ((L'a' <= *p) && (*p <= L'f')))
        {
            int n = 0;

            if ((L'0' <= *p) && (*p <= L'9'))
            {
                n = (*p++) - L'0';
            }
            else if ((L'A' <= *p) && (*p <= L'F'))
            {
                n = (*p++) - L'A' + 10;
            }
            else if ((L'a' <= *p) && (*p <= L'f'))
            {
                n = (*p++) - L'a' + 10;
            }

            int j = 16 * i + n;

            // Check for overflow
            if ((m_badChar != 0) && (j <= i))
            {
                m_badChar = (p - rangeStr) + 1;
            }

            i = j;
        }

        // Was this the high part of a low-high pair?
        if (setHighPart)
        {
            // Yep, set it and move to the next range
            m_ranges[lastRange].m_high = i;

            // Sanity check that range is proper
            if ((m_badChar != 0) && (m_ranges[lastRange].m_high < m_ranges[lastRange].m_low))
            {
                m_badChar = (p - rangeStr) + 1;
            }

            lastRange++;
            setHighPart = false;
            continue;
        }

        // Must have been looking for the low part of a range
        m_ranges[lastRange].m_low = i;

        while (*p == L' ')
        {
            p++;
        }

        // Was that the low part of a low-high pair?
        if (*p == L'-')
        {
            // Yep, skip the dash and set high part next time around.
            p++;
            setHighPart = true;
            continue;
        }

        // Else we have a point range, so set high = low
        m_ranges[lastRange].m_high = i;
        lastRange++;
    }

    // If we didn't parse the full range string, note index of the the
    // first bad char.
    if ((m_badChar != 0) && (*p != 0))
    {
        m_badChar = (p - rangeStr) + 1;
    }

    // Finish off any remaining open range
    if (setHighPart)
    {
        m_ranges[lastRange].m_high = UINT_MAX;
        lastRange++;
    }

    assert(lastRange <= m_entries);
    m_lastRange = lastRange;
    m_inited    = 1;
}

//------------------------------------------------------------------------
// Dump: dump hash ranges to stdout
//
void ConfigMethodRange::Dump()
{
    if (m_inited != 1)
    {
        printf("<uninitialized method range>\n");
        return;
    }

    if (m_lastRange == 0)
    {
        printf("<empty method range>\n");
        return;
    }

    printf("<method range with %d entries>\n", m_lastRange);
    for (unsigned i = 0; i < m_lastRange; i++)
    {
        if (m_ranges[i].m_low == m_ranges[i].m_high)
        {
            printf("%i [0x%08x]\n", i, m_ranges[i].m_low);
        }
        else
        {
            printf("%i [0x%08x-0x%08x]\n", i, m_ranges[i].m_low, m_ranges[i].m_high);
        }
    }
}

//------------------------------------------------------------------------
// Init: parse a string to set up a ConfigIntArray
//
// Arguments:
//    str -- string to parse (may be nullptr)
//
// Notes:
//    Values are separated decimal with no whitespace.
//    Separators are any digit not '-' or '0-9'
//
void ConfigIntArray::Init(const WCHAR* str)
{
    // Count the number of values
    //
    const WCHAR* p         = str;
    unsigned     numValues = 0;
    while (*p != 0)
    {
        if ((*p == L'-') || ((L'0' <= *p) && (*p <= L'9')))
        {
            if (*p == L'-')
            {
                p++;
            }

            while ((L'0' <= *p) && (*p <= L'9'))
            {
                p++;
            }

            numValues++;
        }
        else
        {
            p++;
        }
    }

    m_length = numValues;
    m_values = (int*)g_jitHost->allocateMemory(numValues * sizeof(int));

    numValues         = 0;
    p                 = str;
    int  currentValue = 0;
    bool isNegative   = false;
    while (*p != 0)
    {
        if ((*p == L'-') || ((L'0' <= *p) && (*p <= L'9')))
        {
            if (*p == L'-')
            {
                isNegative = true;
                p++;
            }

            while ((L'0' <= *p) && (*p <= L'9'))
            {
                currentValue = currentValue * 10 + (*p++) - L'0';
            }

            if (isNegative)
            {
                currentValue = -currentValue;
            }

            m_values[numValues++] = currentValue;
            currentValue          = 0;
        }
        else
        {
            p++;
        }
    }
}

//------------------------------------------------------------------------
// Dump: dump config array to stdout
//
void ConfigIntArray::Dump()
{
    if (m_values == nullptr)
    {
        printf("<uninitialized config int array>\n");
        return;
    }

    if (m_length == 0)
    {
        printf("<empty config int array>\n");
        return;
    }

    for (unsigned i = 0; i < m_length; i++)
    {
        printf("%s%i", i == 0 ? "" : ", ", m_values[i]);
    }
}

//------------------------------------------------------------------------
// Init: parse a string to set up a ConfigDoubleArray
//
// Arguments:
//    str -- string to parse (may be nullptr)
//
// Notes:
//    Values are comma, tab or space separated.
//    Consecutive separators are ignored
//
void ConfigDoubleArray::Init(const WCHAR* str)
{
    // Count the number of values
    //
    const WCHAR* p         = str;
    unsigned     numValues = 0;
    while (*p != 0)
    {
        if (*p == L',')
        {
            p++;
            continue;
        }
        WCHAR* pNext = nullptr;
        u16_strtod(p, &pNext);
        if (errno == 0)
        {
            numValues++;
        }
        p = pNext;
    }

    m_length  = numValues;
    m_values  = (double*)g_jitHost->allocateMemory(numValues * sizeof(double));
    p         = str;
    numValues = 0;
    while (*p != 0)
    {
        if (*p == L',')
        {
            p++;
            continue;
        }

        WCHAR* pNext = nullptr;
        double val   = u16_strtod(p, &pNext);
        if (errno == 0)
        {
            m_values[numValues++] = val;
        }
        p = pNext;
    }
}

//------------------------------------------------------------------------
// Dump: dump config array to stdout
//
void ConfigDoubleArray::Dump()
{
    if (m_values == nullptr)
    {
        printf("<uninitialized config double array>\n");
        return;
    }

    if (m_length == 0)
    {
        printf("<empty config double array>\n");
        return;
    }

    for (unsigned i = 0; i < m_length; i++)
    {
        printf("%s%f ", i == 0 ? "" : ",", m_values[i]);
    }
}

#endif // defined(DEBUG)

#if CALL_ARG_STATS || COUNT_BASIC_BLOCKS || COUNT_LOOPS || EMITTER_STATS || MEASURE_NODE_SIZE || MEASURE_MEM_ALLOC

/*****************************************************************************
 *  Histogram class.
 */

Histogram::Histogram(const unsigned* const sizeTable) : m_sizeTable(sizeTable)
{
    unsigned sizeCount = 0;
    do
    {
        sizeCount++;
    } while ((sizeTable[sizeCount] != 0) && (sizeCount < 1000));

    assert(sizeCount < HISTOGRAM_MAX_SIZE_COUNT - 1);

    m_sizeCount = sizeCount;

    memset(m_counts, 0, (m_sizeCount + 1) * sizeof(*m_counts));
}

void Histogram::dump(FILE* output)
{
    unsigned t = 0;
    for (unsigned i = 0; i < m_sizeCount; i++)
    {
        t += m_counts[i];
    }

    for (unsigned c = 0, i = 0; i <= m_sizeCount; i++)
    {
        if (i == m_sizeCount)
        {
            if (m_counts[i] == 0)
            {
                break;
            }

            fprintf(output, "      >    %7u", m_sizeTable[i - 1]);
        }
        else
        {
            if (i == 0)
            {
                fprintf(output, "     <=    ");
            }
            else
            {
                fprintf(output, "%7u .. ", m_sizeTable[i - 1] + 1);
            }

            fprintf(output, "%7u", m_sizeTable[i]);
        }

        c += static_cast<unsigned>(m_counts[i]);

        fprintf(output, " ===> %7u count (%3u%% of total)\n", static_cast<unsigned>(m_counts[i]), (int)(100.0 * c / t));
    }
}

void Histogram::record(unsigned size)
{
    unsigned i;
    for (i = 0; i < m_sizeCount; i++)
    {
        if (m_sizeTable[i] >= size)
        {
            break;
        }
    }

    InterlockedAdd(&m_counts[i], 1);
}

void NodeCounts::dump(FILE* output)
{
    struct Entry
    {
        genTreeOps oper;
        unsigned   count;
    };

    Entry sorted[GT_COUNT];
    for (int i = 0; i < GT_COUNT; i++)
    {
        sorted[i].oper  = static_cast<genTreeOps>(i);
        sorted[i].count = static_cast<unsigned>(m_counts[i]);
    }

    jitstd::sort(sorted, sorted + ArrLen(sorted), [](const Entry& lhs, const Entry& rhs) {
        if (lhs.count > rhs.count)
        {
            return true;
        }

        if (lhs.count < rhs.count)
        {
            return false;
        }

        return static_cast<unsigned>(lhs.oper) < static_cast<unsigned>(rhs.oper);
    });

    for (const Entry& entry : sorted)
    {
        if (entry.count == 0)
        {
            break;
        }

        fprintf(output, "%-20s : %7u\n", GenTree::OpName(entry.oper), entry.count);
    }
}

void NodeCounts::record(genTreeOps oper)
{
    assert(oper < GT_COUNT);
    InterlockedAdd(&m_counts[oper], 1);
}

struct DumpOnShutdownEntry
{
    const char* Name;
    Dumpable*   Dumpable;
};

static DumpOnShutdownEntry s_dumpOnShutdown[16];

DumpOnShutdown::DumpOnShutdown(const char* name, Dumpable* dumpable)
{
    for (DumpOnShutdownEntry& entry : s_dumpOnShutdown)
    {
        if ((entry.Name == nullptr) && (entry.Dumpable == nullptr))
        {
            entry.Name     = name;
            entry.Dumpable = dumpable;
            return;
        }
    }

    assert(!"No space left in table");
}

void DumpOnShutdown::DumpAll()
{
    for (const DumpOnShutdownEntry& entry : s_dumpOnShutdown)
    {
        if (entry.Name != nullptr)
        {
            jitprintf("%s\n", entry.Name);
        }

        if (entry.Dumpable != nullptr)
        {
            entry.Dumpable->dump(jitstdout());
            jitprintf("\n");
        }
    }
}

#endif // CALL_ARG_STATS || COUNT_BASIC_BLOCKS || COUNT_LOOPS || EMITTER_STATS || MEASURE_NODE_SIZE

/*****************************************************************************
 * Fixed bit vector class
 */

// bitChunkSize() - Returns number of bits in a bitVect chunk
inline UINT FixedBitVect::bitChunkSize()
{
    return sizeof(UINT) * 8;
}

// bitNumToBit() - Returns a bit mask of the given bit number
inline UINT FixedBitVect::bitNumToBit(UINT bitNum)
{
    assert(bitNum < bitChunkSize());
    assert(bitChunkSize() <= sizeof(int) * 8);

    return 1 << bitNum;
}

// bitVectInit() - Initializes a bit vector of a given size
FixedBitVect* FixedBitVect::bitVectInit(UINT size, Compiler* comp)
{
    UINT          bitVectMemSize, numberOfChunks;
    FixedBitVect* bv;

    assert(size != 0);

    numberOfChunks = (size - 1) / bitChunkSize() + 1;
    bitVectMemSize = numberOfChunks * (bitChunkSize() / 8); // size in bytes

    assert(bitVectMemSize * bitChunkSize() >= size);

    bv = (FixedBitVect*)comp->getAllocator(CMK_FixedBitVect).allocate<char>(sizeof(FixedBitVect) + bitVectMemSize);
    memset(bv->bitVect, 0, bitVectMemSize);

    bv->bitVectSize = size;

    return bv;
}

// bitVectSet() - Sets the given bit
void FixedBitVect::bitVectSet(UINT bitNum)
{
    UINT index;

    assert(bitNum <= bitVectSize);

    index = bitNum / bitChunkSize();
    bitNum -= index * bitChunkSize();

    bitVect[index] |= bitNumToBit(bitNum);
}

// bitVectClear() - Clears the given bit
void FixedBitVect::bitVectClear(UINT bitNum)
{
    UINT index;

    assert(bitNum <= bitVectSize);

    index = bitNum / bitChunkSize();
    bitNum -= index * bitChunkSize();

    bitVect[index] &= ~bitNumToBit(bitNum);
}

// bitVectTest() - Tests the given bit
bool FixedBitVect::bitVectTest(UINT bitNum)
{
    UINT index;

    assert(bitNum <= bitVectSize);

    index = bitNum / bitChunkSize();
    bitNum -= index * bitChunkSize();

    return (bitVect[index] & bitNumToBit(bitNum)) != 0;
}

// bitVectOr() - Or in the given bit vector
void FixedBitVect::bitVectOr(FixedBitVect* bv)
{
    UINT bitChunkCnt = (bitVectSize - 1) / bitChunkSize() + 1;

    assert(bitVectSize == bv->bitVectSize);

    // Or each chunks
    for (UINT i = 0; i < bitChunkCnt; i++)
    {
        bitVect[i] |= bv->bitVect[i];
    }
}

// bitVectAnd() - And with passed in bit vector
void FixedBitVect::bitVectAnd(FixedBitVect& bv)
{
    UINT bitChunkCnt = (bitVectSize - 1) / bitChunkSize() + 1;

    assert(bitVectSize == bv.bitVectSize);

    // And each chunks
    for (UINT i = 0; i < bitChunkCnt; i++)
    {
        bitVect[i] &= bv.bitVect[i];
    }
}

// bitVectGetFirst() - Find the first bit on and return bit num,
//                    Return -1 if no bits found.
UINT FixedBitVect::bitVectGetFirst()
{
    return bitVectGetNext((UINT)-1);
}

// bitVectGetNext() - Find the next bit on given previous position and return bit num.
//                    Return -1 if no bits found.
UINT FixedBitVect::bitVectGetNext(UINT bitNumPrev)
{
    UINT bitNum = (UINT)-1;
    UINT index;
    UINT bitMask;
    UINT bitChunkCnt = (bitVectSize - 1) / bitChunkSize() + 1;
    UINT i;

    if (bitNumPrev == (UINT)-1)
    {
        index   = 0;
        bitMask = (UINT)-1;
    }
    else
    {
        UINT bit;

        index = bitNumPrev / bitChunkSize();
        bitNumPrev -= index * bitChunkSize();
        bit     = bitNumToBit(bitNumPrev);
        bitMask = ~(bit | (bit - 1));
    }

    // Find first bit
    for (i = index; i < bitChunkCnt; i++)
    {
        UINT bitChunk = bitVect[i] & bitMask;

        if (bitChunk != 0)
        {
            BitScanForward((ULONG*)&bitNum, bitChunk);
            break;
        }

        bitMask = 0xFFFFFFFF;
    }

    // Empty bit vector?
    if (bitNum == (UINT)-1)
    {
        return (UINT)-1;
    }

    bitNum += i * bitChunkSize();

    assert(bitNum <= bitVectSize);

    return bitNum;
}

// bitVectGetNextAndClear() - Find the first bit on, clear it and return it.
//                            Return -1 if no bits found.
UINT FixedBitVect::bitVectGetNextAndClear()
{
    UINT bitNum      = (UINT)-1;
    UINT bitChunkCnt = (bitVectSize - 1) / bitChunkSize() + 1;
    UINT i;

    // Find first bit
    for (i = 0; i < bitChunkCnt; i++)
    {
        if (bitVect[i] != 0)
        {
            BitScanForward((ULONG*)&bitNum, bitVect[i]);
            break;
        }
    }

    // Empty bit vector?
    if (bitNum == (UINT)-1)
    {
        return (UINT)-1;
    }

    // Clear the bit in the right chunk
    bitVect[i] &= ~bitNumToBit(bitNum);

    bitNum += i * bitChunkSize();

    assert(bitNum <= bitVectSize);

    return bitNum;
}

int SimpleSprintf_s(_In_reads_(cbBufSize - (pWriteStart - pBufStart)) char* pWriteStart,
                    _In_reads_(cbBufSize) char*                             pBufStart,
                    size_t                                                  cbBufSize,
                    _In_z_ const char*                                      fmt,
                    ...)
{
    assert(fmt);
    assert(pBufStart);
    assert(pWriteStart);
    assert((size_t)pBufStart <= (size_t)pWriteStart);
    int ret;

    // compute the space left in the buffer.
    if ((pBufStart + cbBufSize) < pWriteStart)
    {
        NO_WAY("pWriteStart is past end of buffer");
    }
    size_t  cbSpaceLeft = (size_t)((pBufStart + cbBufSize) - pWriteStart);
    va_list args;
    va_start(args, fmt);
    ret = vsprintf_s(pWriteStart, cbSpaceLeft, const_cast<char*>(fmt), args);
    va_end(args);
    if (ret < 0)
    {
        NO_WAY("vsprintf_s failed.");
    }
    return ret;
}

#ifdef DEBUG

void hexDump(FILE* dmpf, BYTE* addr, size_t size)
{
    if (size == 0)
    {
        return;
    }

    assert(addr != nullptr);

    for (size_t i = 0; i < size; i++)
    {
        fprintf(dmpf, "%02X", *addr++);
    }
}

#endif // DEBUG

void HelperCallProperties::init()
{
    for (CorInfoHelpFunc helper = CORINFO_HELP_UNDEF; // initialize helper
         (helper < CORINFO_HELP_COUNT);               // test helper for loop exit
         helper = CorInfoHelpFunc(int(helper) + 1))   // update helper to next
    {
        // Generally you want initialize these to their most typical/safest result
        //
        bool isPure        = false; // true if the result only depends upon input args and not any global state
        bool noThrow       = false; // true if the helper will never throw
        bool alwaysThrow   = false; // true if the helper will always throw
        bool nonNullReturn = false; // true if the result will never be null or zero
        bool isAllocator   = false; // true if the result is usually a newly created heap item, or may throw OutOfMemory
        bool mutatesHeap   = false; // true if any previous heap objects [are|can be] modified
        bool mayRunCctor   = false; // true if the helper call may cause a static constructor to be run.

        switch (helper)
        {
            // Arithmetic helpers that cannot throw
            case CORINFO_HELP_LLSH:
            case CORINFO_HELP_LRSH:
            case CORINFO_HELP_LRSZ:
            case CORINFO_HELP_LMUL:
            case CORINFO_HELP_LNG2DBL:
            case CORINFO_HELP_ULNG2DBL:
            case CORINFO_HELP_DBL2INT:
            case CORINFO_HELP_DBL2LNG:
            case CORINFO_HELP_DBL2UINT:
            case CORINFO_HELP_DBL2ULNG:
            case CORINFO_HELP_FLTREM:
            case CORINFO_HELP_DBLREM:
            case CORINFO_HELP_FLTROUND:
            case CORINFO_HELP_DBLROUND:

                isPure  = true;
                noThrow = true;
                break;

            // Arithmetic helpers that *can* throw.

            // This (or these) are not pure, in that they have "VM side effects"...but they don't mutate the heap.
            case CORINFO_HELP_ENDCATCH:

                noThrow = true;
                break;

            // Arithmetic helpers that may throw
            case CORINFO_HELP_LMOD: // Mods throw div-by zero, and signed mods have problems with the smallest integer
                                    // mod -1,
            case CORINFO_HELP_MOD:  // which is not representable as a positive integer.
            case CORINFO_HELP_UMOD:
            case CORINFO_HELP_ULMOD:

            case CORINFO_HELP_UDIV: // Divs throw divide-by-zero.
            case CORINFO_HELP_DIV:
            case CORINFO_HELP_LDIV:
            case CORINFO_HELP_ULDIV:

            case CORINFO_HELP_LMUL_OVF:
            case CORINFO_HELP_ULMUL_OVF:
            case CORINFO_HELP_DBL2INT_OVF:
            case CORINFO_HELP_DBL2LNG_OVF:
            case CORINFO_HELP_DBL2UINT_OVF:
            case CORINFO_HELP_DBL2ULNG_OVF:

                isPure = true;
                break;

            // Heap Allocation helpers, these all never return null
            case CORINFO_HELP_NEWSFAST:
            case CORINFO_HELP_NEWSFAST_ALIGN8:
            case CORINFO_HELP_NEWSFAST_ALIGN8_VC:
            case CORINFO_HELP_NEWFAST:
            case CORINFO_HELP_NEWFAST_MAYBEFROZEN:
            case CORINFO_HELP_NEWSFAST_FINALIZE:
            case CORINFO_HELP_NEWSFAST_ALIGN8_FINALIZE:
            case CORINFO_HELP_READYTORUN_NEW:
            case CORINFO_HELP_BOX:

                isAllocator   = true;
                nonNullReturn = true;
                noThrow       = true; // only can throw OutOfMemory
                break;

            // These allocation helpers do some checks on the size (and lower bound) inputs,
            // and can throw exceptions other than OOM.
            case CORINFO_HELP_NEWARR_1_VC:
            case CORINFO_HELP_NEWARR_1_ALIGN8:
            case CORINFO_HELP_NEW_MDARR:
            case CORINFO_HELP_NEW_MDARR_RARE:
            case CORINFO_HELP_NEWARR_1_DIRECT:
            case CORINFO_HELP_NEWARR_1_MAYBEFROZEN:
            case CORINFO_HELP_NEWARR_1_OBJ:
            case CORINFO_HELP_READYTORUN_NEWARR_1:

                isAllocator   = true;
                nonNullReturn = true;
                break;

            // Heap Allocation helpers that are also pure
            case CORINFO_HELP_STRCNS:

                isPure        = true;
                isAllocator   = true;
                nonNullReturn = true;
                noThrow       = true; // only can throw OutOfMemory
                break;

            case CORINFO_HELP_BOX_NULLABLE:
                // Box Nullable is not a 'pure' function
                // It has a Byref argument that it reads the contents of.
                //
                // So two calls to Box Nullable that pass the same address (with the same Value Number)
                // will produce different results when the contents of the memory pointed to by the Byref changes
                //
                isAllocator = true;
                noThrow     = true; // only can throw OutOfMemory
                break;

            case CORINFO_HELP_RUNTIMEHANDLE_METHOD:
            case CORINFO_HELP_RUNTIMEHANDLE_CLASS:
            case CORINFO_HELP_RUNTIMEHANDLE_METHOD_LOG:
            case CORINFO_HELP_RUNTIMEHANDLE_CLASS_LOG:
            case CORINFO_HELP_READYTORUN_GENERIC_HANDLE:
                // logging helpers are not technically pure but can be optimized away
                isPure        = true;
                noThrow       = true;
                nonNullReturn = true;
                break;

            // type casting helpers
            case CORINFO_HELP_ISINSTANCEOFINTERFACE:
            case CORINFO_HELP_ISINSTANCEOFARRAY:
            case CORINFO_HELP_ISINSTANCEOFCLASS:
            case CORINFO_HELP_ISINSTANCEOFANY:
            case CORINFO_HELP_READYTORUN_ISINSTANCEOF:
            case CORINFO_HELP_TYPEHANDLE_TO_RUNTIMETYPE:
            case CORINFO_HELP_TYPEHANDLE_TO_RUNTIMETYPEHANDLE:

                isPure  = true;
                noThrow = true; // These return null for a failing cast
                break;

            case CORINFO_HELP_GETCURRENTMANAGEDTHREADID:
                isPure  = true;
                noThrow = true;
                break;

            // type casting helpers that throw
            case CORINFO_HELP_CHKCASTINTERFACE:
            case CORINFO_HELP_CHKCASTARRAY:
            case CORINFO_HELP_CHKCASTCLASS:
            case CORINFO_HELP_CHKCASTANY:
            case CORINFO_HELP_CHKCASTCLASS_SPECIAL:
            case CORINFO_HELP_READYTORUN_CHKCAST:

                // These throw for a failing cast
                // But if given a null input arg will return null
                isPure = true;
                break;

            // helpers returning addresses, these can also throw
            case CORINFO_HELP_UNBOX:
            case CORINFO_HELP_LDELEMA_REF:

                isPure = true;
                break;

            // GETREFANY is pure up to the value of the struct argument. We
            // only support that when it is not an implicit byref.
            case CORINFO_HELP_GETREFANY:
#ifndef WINDOWS_AMD64_ABI
                isPure = true;
#endif
                break;

            // helpers that return internal handle
            case CORINFO_HELP_GETCLASSFROMMETHODPARAM:
            case CORINFO_HELP_GETSYNCFROMCLASSHANDLE:

                isPure  = true;
                noThrow = true;
                break;

            // Helpers that load the base address for static variables.
            // We divide these between those that may and may not invoke
            // static class constructors.
            case CORINFO_HELP_GETSHARED_GCSTATIC_BASE:
            case CORINFO_HELP_GETSHARED_NONGCSTATIC_BASE:
            case CORINFO_HELP_GETSHARED_GCSTATIC_BASE_DYNAMICCLASS:
            case CORINFO_HELP_GETSHARED_NONGCSTATIC_BASE_DYNAMICCLASS:
            case CORINFO_HELP_GETGENERICS_GCTHREADSTATIC_BASE:
            case CORINFO_HELP_GETGENERICS_NONGCTHREADSTATIC_BASE:
            case CORINFO_HELP_GETSHARED_GCTHREADSTATIC_BASE:
            case CORINFO_HELP_GETSHARED_NONGCTHREADSTATIC_BASE:
            case CORINFO_HELP_CLASSINIT_SHARED_DYNAMICCLASS:
            case CORINFO_HELP_GETSHARED_GCTHREADSTATIC_BASE_DYNAMICCLASS:
            case CORINFO_HELP_GETSHARED_NONGCTHREADSTATIC_BASE_DYNAMICCLASS:
            case CORINFO_HELP_GETSTATICFIELDADDR_TLS:
            case CORINFO_HELP_GETGENERICS_GCSTATIC_BASE:
            case CORINFO_HELP_GETGENERICS_NONGCSTATIC_BASE:
            case CORINFO_HELP_READYTORUN_GCSTATIC_BASE:
            case CORINFO_HELP_READYTORUN_NONGCSTATIC_BASE:
            case CORINFO_HELP_READYTORUN_THREADSTATIC_BASE:
            case CORINFO_HELP_READYTORUN_NONGCTHREADSTATIC_BASE:
            case CORINFO_HELP_READYTORUN_GENERIC_STATIC_BASE:

                // These may invoke static class constructors
                // These can throw InvalidProgram exception if the class can not be constructed
                //
                isPure        = true;
                nonNullReturn = true;
                mayRunCctor   = true;
                break;

            case CORINFO_HELP_GETSHARED_GCSTATIC_BASE_NOCTOR:
            case CORINFO_HELP_GETSHARED_NONGCSTATIC_BASE_NOCTOR:
            case CORINFO_HELP_GETSHARED_GCTHREADSTATIC_BASE_NOCTOR:
            case CORINFO_HELP_GETSHARED_GCTHREADSTATIC_BASE_NOCTOR_OPTIMIZED:
            case CORINFO_HELP_GETSHARED_NONGCTHREADSTATIC_BASE_NOCTOR:
            case CORINFO_HELP_GETSHARED_NONGCTHREADSTATIC_BASE_NOCTOR_OPTIMIZED:
            case CORINFO_HELP_READYTORUN_THREADSTATIC_BASE_NOCTOR:

                // These do not invoke static class constructors
                //
                isPure        = true;
                noThrow       = true;
                nonNullReturn = true;
                break;

            // GC Write barrier support
            // TODO-ARM64-Bug?: Can these throw or not?
            case CORINFO_HELP_ASSIGN_REF:
            case CORINFO_HELP_CHECKED_ASSIGN_REF:
            case CORINFO_HELP_ASSIGN_REF_ENSURE_NONHEAP:
            case CORINFO_HELP_ASSIGN_BYREF:
            case CORINFO_HELP_ASSIGN_STRUCT:

                mutatesHeap = true;
                break;

            // Accessing fields (write)
            case CORINFO_HELP_SETFIELD32:
            case CORINFO_HELP_SETFIELD64:
            case CORINFO_HELP_SETFIELDOBJ:
            case CORINFO_HELP_SETFIELDSTRUCT:
            case CORINFO_HELP_SETFIELDFLOAT:
            case CORINFO_HELP_SETFIELDDOUBLE:
            case CORINFO_HELP_ARRADDR_ST:

                mutatesHeap = true;
                break;

            // These helper calls always throw an exception
            case CORINFO_HELP_OVERFLOW:
            case CORINFO_HELP_VERIFICATION:
            case CORINFO_HELP_RNGCHKFAIL:
            case CORINFO_HELP_THROWDIVZERO:
            case CORINFO_HELP_THROWNULLREF:
            case CORINFO_HELP_THROW:
            case CORINFO_HELP_RETHROW:
            case CORINFO_HELP_THROW_ARGUMENTEXCEPTION:
            case CORINFO_HELP_THROW_ARGUMENTOUTOFRANGEEXCEPTION:
            case CORINFO_HELP_THROW_NOT_IMPLEMENTED:
            case CORINFO_HELP_THROW_PLATFORM_NOT_SUPPORTED:
            case CORINFO_HELP_THROW_TYPE_NOT_SUPPORTED:
            case CORINFO_HELP_FAIL_FAST:
            case CORINFO_HELP_METHOD_ACCESS_EXCEPTION:
            case CORINFO_HELP_FIELD_ACCESS_EXCEPTION:
            case CORINFO_HELP_CLASS_ACCESS_EXCEPTION:

                alwaysThrow = true;
                break;

            // These helper calls may throw an exception
            case CORINFO_HELP_MON_EXIT_STATIC:

                break;

            // This is a debugging aid; it simply returns a constant address.
            case CORINFO_HELP_LOOP_CLONE_CHOICE_ADDR:
                isPure  = true;
                noThrow = true;
                break;

            case CORINFO_HELP_DBG_IS_JUST_MY_CODE:
            case CORINFO_HELP_BBT_FCN_ENTER:
            case CORINFO_HELP_POLL_GC:
            case CORINFO_HELP_MON_ENTER:
            case CORINFO_HELP_MON_EXIT:
            case CORINFO_HELP_MON_ENTER_STATIC:
            case CORINFO_HELP_JIT_REVERSE_PINVOKE_ENTER:
            case CORINFO_HELP_JIT_REVERSE_PINVOKE_EXIT:
            case CORINFO_HELP_GETFIELDADDR:
            case CORINFO_HELP_INIT_PINVOKE_FRAME:
            case CORINFO_HELP_JIT_PINVOKE_BEGIN:
            case CORINFO_HELP_JIT_PINVOKE_END:

                noThrow = true;
                break;

            // Not sure how to handle optimization involving the rest of these  helpers
            default:

                // The most pessimistic results are returned for these helpers
                mutatesHeap = true;
                break;
        }

        m_isPure[helper]        = isPure;
        m_noThrow[helper]       = noThrow;
        m_alwaysThrow[helper]   = alwaysThrow;
        m_nonNullReturn[helper] = nonNullReturn;
        m_isAllocator[helper]   = isAllocator;
        m_mutatesHeap[helper]   = mutatesHeap;
        m_mayRunCctor[helper]   = mayRunCctor;
    }
}

//=============================================================================
// AssemblyNamesList2
//=============================================================================
// The string should be of the form
// MyAssembly
// MyAssembly;mscorlib;System
//
// You must use ';' as a separator; whitespace no longer works

AssemblyNamesList2::AssemblyNamesList2(const WCHAR* list, HostAllocator alloc) : m_alloc(alloc)
{
    WCHAR          prevChar   = '?';     // dummy
    LPWSTR         nameStart  = nullptr; // start of the name currently being processed. nullptr if no current name
    AssemblyName** ppPrevLink = &m_pNames;

    for (LPWSTR listWalk = const_cast<LPWSTR>(list); prevChar != '\0'; prevChar = *listWalk, listWalk++)
    {
        WCHAR curChar = *listWalk;

        if (curChar == W(';') || curChar == W('\0'))
        {
            // Found separator or end of string
            if (nameStart)
            {
                // Found the end of the current name; add a new assembly name to the list.

                AssemblyName* newName = new (m_alloc) AssemblyName();

                // Null out the current character so we can do zero-terminated string work; we'll restore it later.
                *listWalk = W('\0');

                // How much space do we need?
                int convertedNameLenBytes =
                    WszWideCharToMultiByte(CP_UTF8, 0, nameStart, -1, nullptr, 0, nullptr, nullptr);
                newName->m_assemblyName = new (m_alloc) char[convertedNameLenBytes]; // convertedNameLenBytes includes
                                                                                     // the trailing null character
                if (WszWideCharToMultiByte(CP_UTF8, 0, nameStart, -1, newName->m_assemblyName, convertedNameLenBytes,
                                           nullptr, nullptr) != 0)
                {
                    *ppPrevLink = newName;
                    ppPrevLink  = &newName->m_next;
                }
                else
                {
                    // Failed to convert the string. Ignore this string (and leak the memory).
                }

                nameStart = nullptr;

                // Restore the current character.
                *listWalk = curChar;
            }
        }
        else if (!nameStart)
        {
            //
            // Found the start of a new name
            //

            nameStart = listWalk;
        }
    }

    assert(nameStart == nullptr); // cannot be in the middle of a name
    *ppPrevLink = nullptr;        // Terminate the last element of the list.
}

AssemblyNamesList2::~AssemblyNamesList2()
{
    for (AssemblyName* pName = m_pNames; pName != nullptr; /**/)
    {
        AssemblyName* cur = pName;
        pName             = pName->m_next;

        m_alloc.deallocate(cur->m_assemblyName);
        m_alloc.deallocate(cur);
    }
}

bool AssemblyNamesList2::IsInList(const char* assemblyName)
{
    for (AssemblyName* pName = m_pNames; pName != nullptr; pName = pName->m_next)
    {
        if (_stricmp(pName->m_assemblyName, assemblyName) == 0)
        {
            return true;
        }
    }

    return false;
}

//=============================================================================
// MethodSet
//=============================================================================

MethodSet::MethodSet(const WCHAR* filename, HostAllocator alloc) : m_pInfos(nullptr), m_alloc(alloc)
{
    FILE* methodSetFile = _wfopen(filename, W("r"));
    if (methodSetFile == nullptr)
    {
        return;
    }

    MethodInfo* lastInfo = m_pInfos;
    char        buffer[1024];

    while (true)
    {
        // Get next line
        if (fgets(buffer, sizeof(buffer), methodSetFile) == nullptr)
        {
            break;
        }

        // Ignore lines starting with leading ";" "#" "//".
        if ((0 == strncmp(buffer, ";", 1)) || (0 == strncmp(buffer, "#", 1)) || (0 == strncmp(buffer, "//", 2)))
        {
            continue;
        }

        // Remove trailing newline, if any.
        char* p = strpbrk(buffer, "\r\n");
        if (p != nullptr)
        {
            *p = '\0';
        }

        char*    methodName;
        unsigned methodHash = 0;

        // Parse the line. Very simple. One of:
        //
        //    <method-name>
        //    <method-name><whitespace>(MethodHash=<hash>)

        const char methodHashPattern[] = " (MethodHash=";
        p                              = strstr(buffer, methodHashPattern);
        if (p == nullptr)
        {
            // Just use it without the hash.
            methodName = _strdup(buffer);
        }
        else
        {
            // There's a method hash; use that.

            // First, get the method name.
            char* p2 = p;
            *p       = '\0';

            // Null terminate method at first whitespace. (Don't have any leading whitespace!)
            p = strpbrk(buffer, " \t");
            if (p != nullptr)
            {
                *p = '\0';
            }
            methodName = _strdup(buffer);

            // Now get the method hash.
            p2 += strlen(methodHashPattern);
            char* p3 = strchr(p2, ')');
            if (p3 == nullptr)
            {
                // Malformed line: no trailing slash.
                JITDUMP("Couldn't parse: %s\n", p2);
                // We can still just use the method name.
            }
            else
            {
                // Convert the slash to null.
                *p3 = '\0';

                // Now parse it as hex.
                int count = sscanf_s(p2, "%x", &methodHash);
                if (count != 1)
                {
                    JITDUMP("Couldn't parse: %s\n", p2);
                    // Still, use the method name.
                }
            }
        }

        MethodInfo* newInfo = new (m_alloc) MethodInfo(methodName, methodHash);
        if (m_pInfos == nullptr)
        {
            m_pInfos = lastInfo = newInfo;
        }
        else
        {
            lastInfo->m_next = newInfo;
            lastInfo         = newInfo;
        }
    }

    if (fclose(methodSetFile))
    {
        JITDUMP("Unable to close %ws\n", filename);
    }

    if (m_pInfos == nullptr)
    {
        JITDUMP("No methods read from %ws\n", filename);
    }
    else
    {
        JITDUMP("Methods read from %ws:\n", filename);

        int methodCount = 0;
        for (MethodInfo* pInfo = m_pInfos; pInfo != nullptr; pInfo = pInfo->m_next)
        {
            JITDUMP("  %s (MethodHash: %x)\n", pInfo->m_MethodName, pInfo->m_MethodHash);
            ++methodCount;
        }

        if (methodCount > 100)
        {
            JITDUMP("Warning: high method count (%d) for MethodSet with linear search lookups might be slow\n",
                    methodCount);
        }
    }
}

MethodSet::~MethodSet()
{
    for (MethodInfo* pInfo = m_pInfos; pInfo != nullptr; /**/)
    {
        MethodInfo* cur = pInfo;
        pInfo           = pInfo->m_next;

        m_alloc.deallocate(cur->m_MethodName);
        m_alloc.deallocate(cur);
    }
}

// TODO: make this more like JitConfigValues::MethodSet::contains()?
bool MethodSet::IsInSet(const char* methodName)
{
    for (MethodInfo* pInfo = m_pInfos; pInfo != nullptr; pInfo = pInfo->m_next)
    {
        if (_stricmp(pInfo->m_MethodName, methodName) == 0)
        {
            return true;
        }
    }

    return false;
}

bool MethodSet::IsInSet(int methodHash)
{
    for (MethodInfo* pInfo = m_pInfos; pInfo != nullptr; pInfo = pInfo->m_next)
    {
        if (pInfo->m_MethodHash == methodHash)
        {
            return true;
        }
    }

    return false;
}

bool MethodSet::IsActiveMethod(const char* methodName, int methodHash)
{
    if (methodHash != 0)
    {
        // Use the method hash.
        if (IsInSet(methodHash))
        {
            JITDUMP("Method active in MethodSet (hash match): %s Hash: %x\n", methodName, methodHash);
            return true;
        }
    }

    // Else, fall back and use the method name.
    assert(methodName != nullptr);
    if (IsInSet(methodName))
    {
        JITDUMP("Method active in MethodSet (name match): %s Hash: %x\n", methodName, methodHash);
        return true;
    }

    return false;
}

//------------------------------------------------------------------------
// CachedCyclesPerSecond - Return the cached value of CycleTimer::CyclesPerSecond().
//
// Calling CycleTimer::CyclesPerSecond() can be expensive: it runs a loop of non-empty
// code to compute its value. So call it once and cache the result.
//
double CachedCyclesPerSecond()
{
    static volatile LONG s_CachedCyclesPerSecondInitialized = 0;
    static double        s_CachedCyclesPerSecond            = 0.0;
    static CritSecObject s_CachedCyclesPerSecondLock;

    if (s_CachedCyclesPerSecondInitialized == 1)
    {
        return s_CachedCyclesPerSecond;
    }

    // It wasn't initialized yet, so initialize it. There might be a race,
    // so lock the update.

    CritSecHolder cachedCyclesPerSecondLock(s_CachedCyclesPerSecondLock);

    if (s_CachedCyclesPerSecondInitialized == 1)
    {
        // Someone else initialized it first.
        return s_CachedCyclesPerSecond;
    }

    s_CachedCyclesPerSecond = CycleTimer::CyclesPerSecond();

    LONG originalInitializedValue = InterlockedCompareExchange(&s_CachedCyclesPerSecondInitialized, 1, 0);
    if (originalInitializedValue == 1)
    {
        // This is unexpected; the critical section should have protected us.
    }

    return s_CachedCyclesPerSecond;
}

#ifdef FEATURE_JIT_METHOD_PERF
CycleCount::CycleCount() : cps(CachedCyclesPerSecond())
{
}

bool CycleCount::GetCycles(unsigned __int64* time)
{
    return CycleTimer::GetThreadCyclesS(time);
}

bool CycleCount::Start()
{
    return GetCycles(&beginCycles);
}

double CycleCount::ElapsedTime()
{
    unsigned __int64 nowCycles;
    (void)GetCycles(&nowCycles);
    return ((double)(nowCycles - beginCycles) / cps) * 1000.0;
}

bool PerfCounter::Start()
{
    bool result = QueryPerformanceFrequency(&beg) != 0;
    if (!result)
    {
        return result;
    }
    freq = (double)beg.QuadPart / 1000.0;
    (void)QueryPerformanceCounter(&beg);
    return result;
}

// Return elapsed time from Start() in millis.
double PerfCounter::ElapsedTime()
{
    LARGE_INTEGER li;
    (void)QueryPerformanceCounter(&li);
    return (double)(li.QuadPart - beg.QuadPart) / freq;
}

#endif

#ifdef DEBUG

/*****************************************************************************
 * Return the number of digits in a number of the given base (default base 10).
 * Used when outputting strings.
 */
unsigned CountDigits(unsigned num, unsigned base /* = 10 */)
{
    assert(2 <= base && base <= 16); // sanity check
    unsigned count = 1;
    while (num >= base)
    {
        num /= base;
        ++count;
    }
    return count;
}

unsigned CountDigits(double num, unsigned base /* = 10 */)
{
    assert(2 <= base && base <= 16); // sanity check
    unsigned count = 1;
    while (num >= base)
    {
        num /= base;
        ++count;
    }
    return count;
}

#endif // DEBUG

double FloatingPointUtils::convertUInt64ToDouble(unsigned __int64 uIntVal)
{
    __int64 s64 = uIntVal;
    double  d;
    if (s64 < 0)
    {
#if defined(TARGET_XARCH)
        // RyuJIT codegen and clang (or gcc) may produce different results for casting uint64 to
        // double, and the clang result is more accurate. For example,
        //    1) (double)0x84595161401484A0UL --> 43e08b2a2c280290  (RyuJIT codegen or VC++)
        //    2) (double)0x84595161401484A0UL --> 43e08b2a2c280291  (clang or gcc)
        // If the folding optimization below is implemented by simple casting of (double)uint64_val
        // and it is compiled by clang, casting result can be inconsistent, depending on whether
        // the folding optimization is triggered or the codegen generates instructions for casting. //
        // The current solution is to force the same math as the codegen does, so that casting
        // result is always consistent.

        // d = (double)(int64_t)uint64 + 0x1p64
        uint64_t adjHex = 0x43F0000000000000UL;
        d               = (double)s64 + *(double*)&adjHex;
#else
        d = (double)uIntVal;
#endif
    }
    else
    {
        d = (double)uIntVal;
    }
    return d;
}

float FloatingPointUtils::convertUInt64ToFloat(unsigned __int64 u64)
{
    double d = convertUInt64ToDouble(u64);
    return (float)d;
}

unsigned __int64 FloatingPointUtils::convertDoubleToUInt64(double d)
{
    unsigned __int64 u64;
    if (d >= 0.0)
    {
        // Work around a C++ issue where it doesn't properly convert large positive doubles
        const double two63 = 2147483648.0 * 4294967296.0;
        if (d < two63)
        {
            u64 = UINT64(d);
        }
        else
        {
            // subtract 0x8000000000000000, do the convert then add it back again
            u64 = INT64(d - two63) + I64(0x8000000000000000);
        }
        return u64;
    }

#ifdef TARGET_XARCH

    // While the Ecma spec does not specifically call this out,
    // the case of conversion from negative double to unsigned integer is
    // effectively an overflow and therefore the result is unspecified.
    // With MSVC for x86/x64, such a conversion results in the bit-equivalent
    // unsigned value of the conversion to integer. Other compilers convert
    // negative doubles to zero when the target is unsigned.
    // To make the behavior consistent across OS's on TARGET_XARCH,
    // this double cast is needed to conform MSVC behavior.

    u64 = UINT64(INT64(d));
#else
    u64   = UINT64(d);
#endif // TARGET_XARCH

    return u64;
}

//------------------------------------------------------------------------
// convertToDouble: Convert a single to a double with platform independent
// preservation of payload bits.
//
// Arguments:
//   f - the single
//
// Return Value:
//   A double.
//
// Remarks:
//   All our host platforms except for RISCV-64 will preserve payload bits of
//   NaNs. This function implements the conversion in software for RISCV-64 to
//   mimic other platforms.
//
double FloatingPointUtils::convertToDouble(float f)
{
#ifdef HOST_RISCV64
    if (f == f)
    {
        return f;
    }

    uint32_t bits    = BitOperations::SingleToUInt32Bits(f);
    uint32_t payload = bits & ((1u << 23) - 1);
    uint64_t newBits = ((uint64_t)(bits >> 31) << 63) | 0x7FF8000000000000ul | ((uint64_t)payload << 29);
    return BitOperations::UInt64BitsToDouble(newBits);
#else
    return f;
#endif
}

//------------------------------------------------------------------------
// convertToSingle: Convert a double to a single with platform independent
// preservation of payload bits.
//
// Arguments:
//   d - the double
//
// Return Value:
//   A float.
//
// Remarks:
//   All our host platforms except for RISCV-64 will preserve payload bits of
//   NaNs. This function implements the conversion in software for RISCV-64 to
//   mimic other platforms.
//
float FloatingPointUtils::convertToSingle(double d)
{
#ifdef HOST_RISCV64
    if (d == d)
    {
        return (float)d;
    }

    uint64_t bits       = BitOperations::DoubleToUInt64Bits(d);
    uint32_t newPayload = (uint32_t)((bits >> 29) & ((1u << 23) - 1));
    uint32_t newBits    = ((uint32_t)(bits >> 63) << 31) | 0x7F800000u | newPayload;
    return BitOperations::UInt32BitsToSingle(newBits);
#else
    return (float)d;
#endif
}

// Rounds a double-precision floating-point value to the nearest integer,
// and rounds midpoint values to the nearest even number.
double FloatingPointUtils::round(double x)
{
    // ************************************************************************************
    // IMPORTANT: Do not change this implementation without also updating Math.Round(double),
    //            MathF.Round(float), and FloatingPointUtils::round(float)
    // ************************************************************************************

    // This is based on the 'Berkeley SoftFloat Release 3e' algorithm

    uint64_t bits     = *reinterpret_cast<uint64_t*>(&x);
    int32_t  exponent = (int32_t)(bits >> 52) & 0x07FF;

    if (exponent <= 0x03FE)
    {
        if ((bits << 1) == 0)
        {
            // Exactly +/- zero should return the original value
            return x;
        }

        // Any value less than or equal to 0.5 will always round to exactly zero
        // and any value greater than 0.5 will always round to exactly one. However,
        // we need to preserve the original sign for IEEE compliance.

        double result = ((exponent == 0x03FE) && ((bits & UI64(0x000FFFFFFFFFFFFF)) != 0)) ? 1.0 : 0.0;
        return _copysign(result, x);
    }

    if (exponent >= 0x0433)
    {
        // Any value greater than or equal to 2^52 cannot have a fractional part,
        // So it will always round to exactly itself.

        return x;
    }

    // The absolute value should be greater than or equal to 1.0 and less than 2^52
    assert((0x03FF <= exponent) && (exponent <= 0x0432));

    // Determine the last bit that represents the integral portion of the value
    // and the bits representing the fractional portion

    uint64_t lastBitMask   = UI64(1) << (0x0433 - exponent);
    uint64_t roundBitsMask = lastBitMask - 1;

    // Increment the first fractional bit, which represents the midpoint between
    // two integral values in the current window.

    bits += lastBitMask >> 1;

    if ((bits & roundBitsMask) == 0)
    {
        // If that overflowed and the rest of the fractional bits are zero
        // then we were exactly x.5 and we want to round to the even result

        bits &= ~lastBitMask;
    }
    else
    {
        // Otherwise, we just want to strip the fractional bits off, truncating
        // to the current integer value.

        bits &= ~roundBitsMask;
    }

    return *reinterpret_cast<double*>(&bits);
}

// Windows x86 and Windows ARM/ARM64 may not define _copysignf() but they do define _copysign().
// We will redirect the macro to this other functions if the macro is not defined for the platform.
// This has the side effect of a possible implicit upcasting for arguments passed in and an explicit
// downcasting for the _copysign() call.
#if (defined(TARGET_X86) || defined(TARGET_ARM) || defined(TARGET_ARM64)) && !defined(TARGET_UNIX)

#if !defined(_copysignf)
#define _copysignf (float)_copysign
#endif

#endif

// Rounds a single-precision floating-point value to the nearest integer,
// and rounds midpoint values to the nearest even number.
float FloatingPointUtils::round(float x)
{
    // ************************************************************************************
    // IMPORTANT: Do not change this implementation without also updating MathF.Round(float),
    //            Math.Round(double), and FloatingPointUtils::round(double)
    // ************************************************************************************

    // This is based on the 'Berkeley SoftFloat Release 3e' algorithm

    uint32_t bits     = *reinterpret_cast<uint32_t*>(&x);
    int32_t  exponent = (int32_t)(bits >> 23) & 0xFF;

    if (exponent <= 0x7E)
    {
        if ((bits << 1) == 0)
        {
            // Exactly +/- zero should return the original value
            return x;
        }

        // Any value less than or equal to 0.5 will always round to exactly zero
        // and any value greater than 0.5 will always round to exactly one. However,
        // we need to preserve the original sign for IEEE compliance.

        float result = ((exponent == 0x7E) && ((bits & 0x007FFFFF) != 0)) ? 1.0f : 0.0f;
        return _copysignf(result, x);
    }

    if (exponent >= 0x96)
    {
        // Any value greater than or equal to 2^52 cannot have a fractional part,
        // So it will always round to exactly itself.

        return x;
    }

    // The absolute value should be greater than or equal to 1.0 and less than 2^52
    assert((0x7F <= exponent) && (exponent <= 0x95));

    // Determine the last bit that represents the integral portion of the value
    // and the bits representing the fractional portion

    uint32_t lastBitMask   = 1U << (0x96 - exponent);
    uint32_t roundBitsMask = lastBitMask - 1;

    // Increment the first fractional bit, which represents the midpoint between
    // two integral values in the current window.

    bits += lastBitMask >> 1;

    if ((bits & roundBitsMask) == 0)
    {
        // If that overflowed and the rest of the fractional bits are zero
        // then we were exactly x.5 and we want to round to the even result

        bits &= ~lastBitMask;
    }
    else
    {
        // Otherwise, we just want to strip the fractional bits off, truncating
        // to the current integer value.

        bits &= ~roundBitsMask;
    }

    return *reinterpret_cast<float*>(&bits);
}

bool FloatingPointUtils::isNormal(double x)
{
    int64_t bits = reinterpret_cast<int64_t&>(x);
    bits &= 0x7FFFFFFFFFFFFFFF;
    return (bits < 0x7FF0000000000000) && (bits != 0) && ((bits & 0x7FF0000000000000) != 0);
}

bool FloatingPointUtils::isNormal(float x)
{
    int32_t bits = reinterpret_cast<int32_t&>(x);
    bits &= 0x7FFFFFFF;
    return (bits < 0x7F800000) && (bits != 0) && ((bits & 0x7F800000) != 0);
}

//------------------------------------------------------------------------
// infinite_double: return an infinite double value
//
// Returns:
//    Infinite double value.
//
// Notes:
//    This is the predefined constant HUGE_VAL on many platforms.
//
double FloatingPointUtils::infinite_double()
{
    int64_t bits = 0x7FF0000000000000;
    return *reinterpret_cast<double*>(&bits);
}

//------------------------------------------------------------------------
// infinite_float: return an infinite float value
//
// Returns:
//    Infinite float value.
//
// Notes:
//    This is the predefined constant HUGE_VALF on many platforms.
//
float FloatingPointUtils::infinite_float()
{
    int32_t bits = 0x7F800000;
    return *reinterpret_cast<float*>(&bits);
}

//------------------------------------------------------------------------
// hasPreciseReciprocal: check double for precise reciprocal. E.g. 2.0 <--> 0.5
//
// Arguments:
//    x - value to check for precise reciprocal
//
// Return Value:
//    True if 'x' is a power of two value and is not denormal (denormals may not be well-defined
//    on some platforms such as if the user modified the floating-point environment via a P/Invoke)
//

bool FloatingPointUtils::hasPreciseReciprocal(double x)
{
    if (!isNormal(x))
    {
        return false;
    }

    uint64_t i        = reinterpret_cast<uint64_t&>(x);
    uint64_t exponent = (i >> 52) & 0x7FFul;   // 0x7FF mask drops the sign bit
    uint64_t mantissa = i & 0xFFFFFFFFFFFFFul; // 0xFFFFFFFFFFFFF mask drops the sign + exponent bits
    return mantissa == 0 && exponent != 0 && exponent != 1023;
}

//------------------------------------------------------------------------
// hasPreciseReciprocal: check float for precise reciprocal. E.g. 2.0f <--> 0.5f
//
// Arguments:
//    x - value to check for precise reciprocal
//
// Return Value:
//    True if 'x' is a power of two value and is not denormal (denormals may not be well-defined
//    on some platforms such as if the user modified the floating-point environment via a P/Invoke)
//

bool FloatingPointUtils::hasPreciseReciprocal(float x)
{
    if (!isNormal(x))
    {
        return false;
    }

    uint32_t i        = reinterpret_cast<uint32_t&>(x);
    uint32_t exponent = (i >> 23) & 0xFFu; // 0xFF mask drops the sign bit
    uint32_t mantissa = i & 0x7FFFFFu;     // 0x7FFFFF mask drops the sign + exponent bits
    return mantissa == 0 && exponent != 0 && exponent != 127;
}

//------------------------------------------------------------------------
// isAllBitsSet: Determines whether the specified value is AllBitsSet
//
// Arguments:
//    val - value to check for AllBitsSet
//
// Return Value:
//    True if val is AllBitsSet
//

bool FloatingPointUtils::isAllBitsSet(float val)
{
    UINT32 bits = *reinterpret_cast<UINT32*>(&val);
    return bits == 0xFFFFFFFFU;
}

//------------------------------------------------------------------------
// isAllBitsSet: Determines whether the specified value is AllBitsSet
//
// Arguments:
//    val - value to check for AllBitsSet
//
// Return Value:
//    True if val is AllBitsSet
//

bool FloatingPointUtils::isAllBitsSet(double val)
{
    UINT64 bits = *reinterpret_cast<UINT64*>(&val);
    return bits == 0xFFFFFFFFFFFFFFFFULL;
}

//------------------------------------------------------------------------
// isNegative: Determines whether the specified value is negative
//
// Arguments:
//    val - value to check
//
// Return Value:
//    True if val is negative
//

bool FloatingPointUtils::isNegative(float val)
{
    return *reinterpret_cast<INT32*>(&val) < 0;
}

//------------------------------------------------------------------------
// isNegative: Determines whether the specified value is negative
//
// Arguments:
//    val - value to check
//
// Return Value:
//    True if val is negative
//

bool FloatingPointUtils::isNegative(double val)
{
    return *reinterpret_cast<INT64*>(&val) < 0;
}

//------------------------------------------------------------------------
// isNaN: Determines whether the specified value is NaN
//
// Arguments:
//    val - value to check for NaN
//
// Return Value:
//    True if val is NaN
//

bool FloatingPointUtils::isNaN(float val)
{
    UINT32 bits = *reinterpret_cast<UINT32*>(&val);
    return (bits & 0x7FFFFFFFU) > 0x7F800000U;
}

//------------------------------------------------------------------------
// isNaN: Determines whether the specified value is NaN
//
// Arguments:
//    val - value to check for NaN
//
// Return Value:
//    True if val is NaN
//

bool FloatingPointUtils::isNaN(double val)
{
    UINT64 bits = *reinterpret_cast<UINT64*>(&val);
    return (bits & 0x7FFFFFFFFFFFFFFFULL) > 0x7FF0000000000000ULL;
}

//------------------------------------------------------------------------
// isNegativeZero: Determines whether the specified value is negative zero (-0.0)
//
// Arguments:
//    val - value to check for (-0.0)
//
// Return Value:
//    True if val is (-0.0)
//

bool FloatingPointUtils::isNegativeZero(double val)
{
    UINT64 bits = *reinterpret_cast<UINT64*>(&val);
    return bits == 0x8000000000000000ULL;
}

//------------------------------------------------------------------------
// isPositiveZero: Determines whether the specified value is positive zero (+0.0)
//
// Arguments:
//    val - value to check for (+0.0)
//
// Return Value:
//    True if val is (+0.0)
//

bool FloatingPointUtils::isPositiveZero(double val)
{
    UINT64 bits = *reinterpret_cast<UINT64*>(&val);
    return bits == 0x0000000000000000ULL;
}

//------------------------------------------------------------------------
// maximum: This matches the IEEE 754:2019 `maximum` function
//
// It propagates NaN inputs back to the caller and
// otherwise returns the greater of the inputs. It
// treats +0 as greater than -0 as per the specification.
//
// Arguments:
//    val1 - left operand
//    val2 - right operand
//
// Return Value:
//    Either val1 or val2
//
double FloatingPointUtils::maximum(double val1, double val2)
{
    if (val1 != val2)
    {
        if (!isNaN(val1))
        {
            return val2 < val1 ? val1 : val2;
        }

        return val1;
    }

    return isNegative(val2) ? val1 : val2;
}

//------------------------------------------------------------------------
// maximumMagnitude: This matches the IEEE 754:2019 `maximumMagnitude` function
//
// It propagates NaN inputs back to the caller and
// otherwise returns the input with a greater magnitude.
// It treats +0 as greater than -0 as per the specification.
//
// Arguments:
//    x - left operand
//    y - right operand
//
// Return Value:
//    Either x or y
//
double FloatingPointUtils::maximumMagnitude(double x, double y)
{
    double ax = fabs(x);
    double ay = fabs(y);

    if ((ax > ay) || isNaN(ax))
    {
        return x;
    }

    if (ax == ay)
    {
        return isNegative(x) ? y : x;
    }

    return y;
}

//------------------------------------------------------------------------
// maximumMagnitudeNumber: // This matches the IEEE 754:2019 `maximumMagnitudeNumber` function
//
// It does not propagate NaN inputs back to the caller and
// otherwise returns the input with a larger magnitude.
// It treats +0 as larger than -0 as per the specification.
//
// Arguments:
//    x - left operand
//    y - right operand
//
// Return Value:
//    Either x or y
//
double FloatingPointUtils::maximumMagnitudeNumber(double x, double y)
{
    double ax = fabs(x);
    double ay = fabs(y);

    if ((ax > ay) || isNaN(ay))
    {
        return x;
    }

    if (ax == ay)
    {
        return isNegative(x) ? y : x;
    }

    return y;
}

//------------------------------------------------------------------------
// maximumNumber: This matches the IEEE 754:2019 `maximumNumber` function
//
// It does not propagate NaN inputs back to the caller and
// otherwise returns the larger of the inputs. It
// treats +0 as larger than -0 as per the specification.
//
// Arguments:
//    x - left operand
//    y - right operand
//
// Return Value:
//    Either x or y
//
double FloatingPointUtils::maximumNumber(double x, double y)
{
    if (x != y)
    {
        if (!isNaN(y))
        {
            return y < x ? x : y;
        }

        return x;
    }

    return isNegative(y) ? x : y;
}

//------------------------------------------------------------------------
// maximum: This matches the IEEE 754:2019 `maximum` function
//
// It propagates NaN inputs back to the caller and
// otherwise returns the greater of the inputs. It
// treats +0 as greater than -0 as per the specification.
//
// Arguments:
//    val1 - left operand
//    val2 - right operand
//
// Return Value:
//    Either val1 or val2
//
float FloatingPointUtils::maximum(float val1, float val2)
{
    if (val1 != val2)
    {
        if (!isNaN(val1))
        {
            return val2 < val1 ? val1 : val2;
        }

        return val1;
    }

    return isNegative(val2) ? val1 : val2;
}

//------------------------------------------------------------------------
// maximumMagnitude: This matches the IEEE 754:2019 `maximumMagnitude` function
//
// It propagates NaN inputs back to the caller and
// otherwise returns the input with a greater magnitude.
// It treats +0 as greater than -0 as per the specification.
//
// Arguments:
//    x - left operand
//    y - right operand
//
// Return Value:
//    Either x or y
//
float FloatingPointUtils::maximumMagnitude(float x, float y)
{
    float ax = fabsf(x);
    float ay = fabsf(y);

    if ((ax > ay) || isNaN(ax))
    {
        return x;
    }

    if (ax == ay)
    {
        return isNegative(x) ? y : x;
    }

    return y;
}

//------------------------------------------------------------------------
// maximumMagnitudeNumber: This matches the IEEE 754:2019 `maximumMagnitudeNumber` function
//
// It does not propagate NaN inputs back to the caller and
// otherwise returns the input with a larger magnitude.
// It treats +0 as larger than -0 as per the specification.
//
// Arguments:
//    x - left operand
//    y - right operand
//
// Return Value:
//    Either x or y
//
float FloatingPointUtils::maximumMagnitudeNumber(float x, float y)
{
    float ax = fabsf(x);
    float ay = fabsf(y);

    if ((ax > ay) || isNaN(ay))
    {
        return x;
    }

    if (ax == ay)
    {
        return isNegative(x) ? y : x;
    }

    return y;
}

//------------------------------------------------------------------------
// maximumNumber: This matches the IEEE 754:2019 `maximumNumber` function
//
// It does not propagate NaN inputs back to the caller and
// otherwise returns the larger of the inputs. It
// treats +0 as larger than -0 as per the specification.
//
// Arguments:
//    x - left operand
//    y - right operand
//
// Return Value:
//    Either x or y
//
float FloatingPointUtils::maximumNumber(float x, float y)
{
    if (x != y)
    {
        if (!isNaN(y))
        {
            return y < x ? x : y;
        }

        return x;
    }

    return isNegative(y) ? x : y;
}

//------------------------------------------------------------------------
// minimum: This matches the IEEE 754:2019 `minimum` function
//
// It propagates NaN inputs back to the caller and
// otherwise returns the lesser of the inputs. It
// treats +0 as greater than -0 as per the specification.
//
// Arguments:
//    val1 - left operand
//    val2 - right operand
//
// Return Value:
//    Either val1 or val2
//
double FloatingPointUtils::minimum(double val1, double val2)
{
    if (val1 != val2)
    {
        if (!isNaN(val1))
        {
            return val1 < val2 ? val1 : val2;
        }

        return val1;
    }

    return isNegative(val1) ? val1 : val2;
}

//------------------------------------------------------------------------
// minimumMagnitude: This matches the IEEE 754:2019 `minimumMagnitude` function
//
// It propagates NaN inputs back to the caller and
// otherwise returns the input with a lesser magnitude.
// It treats +0 as greater than -0 as per the specification.
//
// Arguments:
//    x - left operand
//    y - right operand
//
// Return Value:
//    Either x or y
//
double FloatingPointUtils::minimumMagnitude(double x, double y)
{
    double ax = fabs(x);
    double ay = fabs(y);

    if ((ax < ay) || isNaN(ax))
    {
        return x;
    }

    if (ax == ay)
    {
        return isNegative(x) ? x : y;
    }

    return y;
}

//------------------------------------------------------------------------
// minimumMagnitudeNumber: This matches the IEEE 754:2019 `minimumMagnitudeNumber` function
//
// It does not propagate NaN inputs back to the caller and
// otherwise returns the input with a larger magnitude.
// It treats +0 as larger than -0 as per the specification.
//
// Arguments:
//    x - left operand
//    y - right operand
//
// Return Value:
//    Either x or y
//
double FloatingPointUtils::minimumMagnitudeNumber(double x, double y)
{
    double ax = fabs(x);
    double ay = fabs(y);

    if ((ax < ay) || isNaN(ay))
    {
        return x;
    }

    if (ax == ay)
    {
        return isNegative(x) ? x : y;
    }

    return y;
}

//------------------------------------------------------------------------
// minimumNumber: This matches the IEEE 754:2019 `minimumNumber` function
//
// It does not propagate NaN inputs back to the caller and
// otherwise returns the larger of the inputs. It
// treats +0 as larger than -0 as per the specification.
//
// Arguments:
//    x - left operand
//    y - right operand
//
// Return Value:
//    Either x or y
//
double FloatingPointUtils::minimumNumber(double x, double y)
{
    if (x != y)
    {
        if (!isNaN(y))
        {
            return x < y ? x : y;
        }

        return x;
    }

    return isNegative(x) ? x : y;
}

//------------------------------------------------------------------------
// minimum: This matches the IEEE 754:2019 `minimum` function
//
// It propagates NaN inputs back to the caller and
// otherwise returns the lesser of the inputs. It
// treats +0 as greater than -0 as per the specification.
//
// Arguments:
//    val1 - left operand
//    val2 - right operand
//
// Return Value:
//    Either val1 or val2
//
float FloatingPointUtils::minimum(float val1, float val2)
{
    if (val1 != val2)
    {
        if (!isNaN(val1))
        {
            return val1 < val2 ? val1 : val2;
        }

        return val1;
    }

    return isNegative(val1) ? val1 : val2;
}

//------------------------------------------------------------------------
// minimumMagnitude: This matches the IEEE 754:2019 `minimumMagnitude` function
//
// It propagates NaN inputs back to the caller and
// otherwise returns the input with a lesser magnitude.
// It treats +0 as greater than -0 as per the specification.
//
// Arguments:
//    x - left operand
//    y - right operand
//
// Return Value:
//    Either x or y
//
float FloatingPointUtils::minimumMagnitude(float x, float y)
{
    float ax = fabsf(x);
    float ay = fabsf(y);

    if ((ax < ay) || isNaN(ax))
    {
        return x;
    }

    if (ax == ay)
    {
        return isNegative(x) ? x : y;
    }

    return y;
}

//------------------------------------------------------------------------
// minimumMagnitudeNumber: This matches the IEEE 754:2019 `minimumMagnitudeNumber` function
//
// It does not propagate NaN inputs back to the caller and
// otherwise returns the input with a larger magnitude.
// It treats +0 as larger than -0 as per the specification.
//
// Arguments:
//    x - left operand
//    y - right operand
//
// Return Value:
//    Either x or y
//
float FloatingPointUtils::minimumMagnitudeNumber(float x, float y)
{
    float ax = fabsf(x);
    float ay = fabsf(y);

    if ((ax < ay) || isNaN(ay))
    {
        return x;
    }

    if (ax == ay)
    {
        return isNegative(x) ? x : y;
    }

    return y;
}

//------------------------------------------------------------------------
// minimumNumber: This matches the IEEE 754:2019 `minimumNumber` function
//
// It does not propagate NaN inputs back to the caller and
// otherwise returns the larger of the inputs. It
// treats +0 as larger than -0 as per the specification.
//
// Arguments:
//    x - left operand
//    y - right operand
//
// Return Value:
//    Either x or y
//
float FloatingPointUtils::minimumNumber(float x, float y)
{
    if (x != y)
    {
        if (!isNaN(y))
        {
            return x < y ? x : y;
        }

        return x;
    }

    return isNegative(x) ? x : y;
}

//------------------------------------------------------------------------
// normalize: Normalize a floating point value.
//
// Arguments:
//    value - the value
//
// Return Value:
//    Normalized value.
//
// Remarks:
//   This is a no-op on all host platforms but x86. On x86 floats are returned on
//   the x87 stack. Since `fld` will automatically quiet signalling NaNs this
//   means that it is very easy for a float to nondeterministically change bit
//   representation if it is a snan, depending on whether a function that
//   returns the value is inlined or not by the C++ compiler. To get around the
//   nondeterminism we quiet the NaNs ahead of time as a best-effort fix.
//
double FloatingPointUtils::normalize(double value)
{
#ifdef HOST_X86
    if (!isNaN(value))
    {
        return value;
    }

    uint64_t bits;
    static_assert_no_msg(sizeof(bits) == sizeof(value));
    memcpy(&bits, &value, sizeof(value));
    bits |= 1ull << 51;
    memcpy(&value, &bits, sizeof(bits));
    return value;
#else
    return value;
#endif
}

//------------------------------------------------------------------------
// BitOperations::BitScanReverse: Search the mask data from most significant bit (MSB) to least significant bit
// (LSB) for a set bit (1).
//
// Arguments:
//    value - the value
//
// Return Value:
//    0 if the mask is zero; nonzero otherwise.
//
uint32_t BitOperations::BitScanReverse(uint32_t value)
{
    assert(value != 0);

#if defined(_MSC_VER)
    unsigned long result;
    ::_BitScanReverse(&result, value);
    return static_cast<uint32_t>(result);
#else
    // LZCNT returns index starting from MSB, whereas BSR gives the index from LSB.
    // 31 ^ BSR here is equivalent to 31 - BSR since the BSR result is always between 0 and 31.
    // This saves an instruction, as subtraction from constant requires either MOV/SUB or NEG/ADD.

    int32_t result = __builtin_clz(value);
    return static_cast<uint32_t>(31 ^ result);
#endif
}

//------------------------------------------------------------------------
// BitOperations::BitScanReverse: Search the mask data from most significant bit (MSB) to least significant bit
// (LSB) for a set bit (1).
//
// Arguments:
//    value - the value
//
// Return Value:
//    0 if the mask is zero; nonzero otherwise.
//
uint32_t BitOperations::BitScanReverse(uint64_t value)
{
    assert(value != 0);

#if defined(_MSC_VER)
#if defined(HOST_64BIT)
    unsigned long result;
    ::_BitScanReverse64(&result, value);
    return static_cast<uint32_t>(result);
#else
    uint32_t upper = static_cast<uint32_t>(value >> 32);

    if (upper == 0)
    {
        uint32_t lower = static_cast<uint32_t>(value);
        return BitScanReverse(lower);
    }

    return 32 + BitScanReverse(upper);
#endif // HOST_64BIT
#else
    // LZCNT returns index starting from MSB, whereas BSR gives the index from LSB.
    // 63 ^ BSR here is equivalent to 63 - BSR since the BSR result is always between 0 and 63.
    // This saves an instruction, as subtraction from constant requires either MOV/SUB or NEG/ADD.

    int32_t result = __builtin_clzll(value);
    return static_cast<uint32_t>(63 ^ result);
#endif
}

//------------------------------------------------------------------------
// BitOperations::DoubleToUInt64Bits: Gets the underlying bits for a double-precision floating-point value.
//
// Arguments:
//    value - The number to convert
//
// Return Value:
//    The underlying bits for value.
//
uint64_t BitOperations::DoubleToUInt64Bits(double value)
{
    uint64_t result;
    memcpy(&result, &value, sizeof(double));
    return result;
}

//------------------------------------------------------------------------
// BitOperations::LeadingZeroCount: Count the number of leading zero bits in a mask.
//
// Arguments:
//    value - the value
//
// Return Value:
//    The number of leading bits in value
//
uint32_t BitOperations::LeadingZeroCount(uint32_t value)
{
    if (value == 0)
    {
        return 32;
    }

#if defined(_MSC_VER)
    // LZCNT returns index starting from MSB, whereas BSR gives the index from LSB.
    // 31 ^ BSR here is equivalent to 31 - BSR since the BSR result is always between 0 and 31.
    // This saves an instruction, as subtraction from constant requires either MOV/SUB or NEG/ADD.

    uint32_t result = BitOperations::BitScanReverse(value);
    return 31 ^ result;
#else
    int32_t result = __builtin_clz(value);
    return static_cast<uint32_t>(result);
#endif
}

//------------------------------------------------------------------------
// BitOperations::LeadingZeroCount: Count the number of leading zero bits in a mask.
//
// Arguments:
//    value - the value
//
// Return Value:
//    The number of leading bits in value
//
uint32_t BitOperations::LeadingZeroCount(uint64_t value)
{
    if (value == 0)
    {
        return 64;
    }

#if defined(_MSC_VER)
    // LZCNT returns index starting from MSB, whereas BSR gives the index from LSB.
    // 63 ^ BSR here is equivalent to 63 - BSR since the BSR result is always between 0 and 63.
    // This saves an instruction, as subtraction from constant requires either MOV/SUB or NEG/ADD.

    uint32_t result = BitOperations::BitScanReverse(value);
    return 63 ^ result;
#else
    int32_t result = __builtin_clzll(value);
    return static_cast<uint32_t>(result);
#endif
}

//------------------------------------------------------------------------
// BitOperations::Log2: Returns the integer (floor) log of the specified value, base 2.
//
// Arguments:
//    value - the value
//
// Return Value:
//    The integer (floor) log of value, base 2
//
uint32_t BitOperations::Log2(uint32_t value)
{
    // The 0->0 contract is fulfilled by setting the LSB to 1.
    // Log(1) is 0, and setting the LSB for values > 1 does not change the log2 result.
    return 31 ^ BitOperations::LeadingZeroCount(value | 1);
}

//------------------------------------------------------------------------
// BitOperations::Log2: Returns the integer (floor) log of the specified value, base 2.
//
// Arguments:
//    value - the value
//
// Return Value:
//    The integer (floor) log of value, base 2
//
uint32_t BitOperations::Log2(uint64_t value)
{
    // The 0->0 contract is fulfilled by setting the LSB to 1.
    // Log(1) is 0, and setting the LSB for values > 1 does not change the log2 result.
    return 63 ^ BitOperations::LeadingZeroCount(value | 1);
}

//------------------------------------------------------------------------
// BitOperations::PopCount: Returns the population count (number of bits set) of a mask.
//
// Arguments:
//    value - the value
//
// Return Value:
//    The population count (number of bits set) of value
//
uint32_t BitOperations::PopCount(uint32_t value)
{
#if defined(_MSC_VER)
    // Inspired by the Stanford Bit Twiddling Hacks by Sean Eron Anderson:
    // http://graphics.stanford.edu/~seander/bithacks.html

    const uint32_t c1 = 0x55555555u;
    const uint32_t c2 = 0x33333333u;
    const uint32_t c3 = 0x0F0F0F0Fu;
    const uint32_t c4 = 0x01010101u;

    value -= (value >> 1) & c1;
    value = (value & c2) + ((value >> 2) & c2);
    value = (((value + (value >> 4)) & c3) * c4) >> 24;

    return value;
#else
    int32_t result = __builtin_popcount(value);
    return static_cast<uint32_t>(result);
#endif
}

//------------------------------------------------------------------------
// BitOperations::PopCount: Returns the population count (number of bits set) of a mask.
//
// Arguments:
//    value - the value
//
// Return Value:
//    The population count (number of bits set) of value
//
uint32_t BitOperations::PopCount(uint64_t value)
{
#if defined(_MSC_VER)
    // Inspired by the Stanford Bit Twiddling Hacks by Sean Eron Anderson:
    // http://graphics.stanford.edu/~seander/bithacks.html

    const uint64_t c1 = 0x5555555555555555ull;
    const uint64_t c2 = 0x3333333333333333ull;
    const uint64_t c3 = 0x0F0F0F0F0F0F0F0Full;
    const uint64_t c4 = 0x0101010101010101ull;

    value -= (value >> 1) & c1;
    value = (value & c2) + ((value >> 2) & c2);
    value = (((value + (value >> 4)) & c3) * c4) >> 56;

    return static_cast<uint32_t>(value);
#else
    int32_t result = __builtin_popcountll(value);
    return static_cast<uint32_t>(result);
#endif
}

//------------------------------------------------------------------------
// BitOperations::ReverseBits: Reverses the bits in an integer value
//
// Arguments:
//    value - the value
//
// Return Value:
//    The reversed bits of value
//
uint32_t BitOperations::ReverseBits(uint32_t value)
{
    // Inspired by the Stanford Bit Twiddling Hacks by Sean Eron Anderson:
    // http://graphics.stanford.edu/~seander/bithacks.html

    uint32_t result = value;

    // swap odd and even bits
    result = ((result >> 1) & 0x55555555) | ((result & 0x55555555) << 1);

    // swap consecutive pairs
    result = ((result >> 2) & 0x33333333) | ((result & 0x33333333) << 2);

    // swap nibbles ...
    result = ((result >> 4) & 0x0F0F0F0F) | ((result & 0x0F0F0F0F) << 4);

    // swap bytes
    result = ((result >> 8) & 0x00FF00FF) | ((result & 0x00FF00FF) << 8);

    // swap 2-byte pairs
    result = (result >> 16) | (result << 16);

    return result;
}

//------------------------------------------------------------------------
// BitOperations::ReverseBits: Reverses the bits in an integer value
//
// Arguments:
//    value - the value
//
// Return Value:
//    The reversed bits of value
//
uint64_t BitOperations::ReverseBits(uint64_t value)
{
    // Inspired by the Stanford Bit Twiddling Hacks by Sean Eron Anderson:
    // http://graphics.stanford.edu/~seander/bithacks.html

    uint64_t result = value;

    // swap odd and even bits
    result = ((result >> 1) & 0x5555555555555555ull) | ((result & 0x5555555555555555ull) << 1);

    // swap consecutive pairs
    result = ((result >> 2) & 0x3333333333333333ull) | ((result & 0x3333333333333333ull) << 2);

    // swap nibbles ...
    result = ((result >> 4) & 0x0F0F0F0F0F0F0F0Full) | ((result & 0x0F0F0F0F0F0F0F0Full) << 4);

    // swap bytes
    result = ((result >> 8) & 0x00FF00FF00FF00FFull) | ((result & 0x00FF00FF00FF00FFull) << 8);

    // swap 2-byte pairs
    result = ((result >> 16) & 0x0000FFFF0000FFFFull) | ((result & 0x0000FFFF0000FFFFull) << 16);

    // swap 4-byte pairs
    result = (result >> 32) | (result << 32);

    return result;
}

//------------------------------------------------------------------------
// BitOperations::RotateLeft: Rotates the specified value left by the specified number of bits.
//
// Arguments:
//    value  - the value to rotate
//    offset - the number of bits to rotate by
//
// Return Value:
//    The rotated value
//
uint32_t BitOperations::RotateLeft(uint32_t value, uint32_t offset)
{
    // Mask the offset to ensure deterministic xplat behavior for overshifting
    return (value << (offset & 0x1F)) | (value >> ((32 - offset) & 0x1F));
}

//------------------------------------------------------------------------
// BitOperations::RotateLeft: Rotates the specified value left by the specified number of bits.
//
// Arguments:
//    value  - the value to rotate
//    offset - the number of bits to rotate by
//
// Return Value:
//    The rotated value
//
uint64_t BitOperations::RotateLeft(uint64_t value, uint32_t offset)
{
    // Mask the offset to ensure deterministic xplat behavior for overshifting
    return (value << (offset & 0x3F)) | (value >> ((64 - offset) & 0x3F));
}

//------------------------------------------------------------------------
// BitOperations::RotateRight: Rotates the specified value right by the specified number of bits.
//
// Arguments:
//    value  - the value to rotate
//    offset - the number of bits to rotate by
//
// Return Value:
//    The rotated value
//
uint32_t BitOperations::RotateRight(uint32_t value, uint32_t offset)
{
    // Mask the offset to ensure deterministic xplat behavior for overshifting
    return (value >> (offset & 0x1F)) | (value << ((32 - offset) & 0x1F));
}

//------------------------------------------------------------------------
// BitOperations::RotateRight: Rotates the specified value right by the specified number of bits.
//
// Arguments:
//    value  - the value to rotate
//    offset - the number of bits to rotate by
//
// Return Value:
//    The rotated value
//
uint64_t BitOperations::RotateRight(uint64_t value, uint32_t offset)
{
    // Mask the offset to ensure deterministic xplat behavior for overshifting
    return (value >> (offset & 0x3F)) | (value << ((64 - offset) & 0x3F));
}

//------------------------------------------------------------------------
// BitOperations::SingleToUInt32Bits: Gets the underlying bits for a single-precision floating-point value.
//
// Arguments:
//    value - The number to convert
//
// Return Value:
//    The underlying bits for value.
//
uint32_t BitOperations::SingleToUInt32Bits(float value)
{
    uint32_t result;
    memcpy(&result, &value, sizeof(float));
    return result;
}

//------------------------------------------------------------------------
// BitOperations::TrailingZeroCount: Count the number of trailing zero bits in an integer value.
//
// Arguments:
//    value - the value
//
// Return Value:
//    The number of trailing zero bits in value
//
uint32_t BitOperations::TrailingZeroCount(uint32_t value)
{
    if (value == 0)
    {
        return 32;
    }

#if defined(_MSC_VER)
    return BitOperations::BitScanForward(value);
#else
    int32_t result = __builtin_ctz(value);
    return static_cast<uint32_t>(result);
#endif
}

//------------------------------------------------------------------------
// BitOperations::TrailingZeroCount: Count the number of trailing zero bits in an integer value.
//
// Arguments:
//    value - the value
//
// Return Value:
//    The number of trailing zero bits in value
//
uint32_t BitOperations::TrailingZeroCount(uint64_t value)
{
    if (value == 0)
    {
        return 64;
    }

#if defined(_MSC_VER)
    return BitOperations::BitScanForward(value);
#else
    int32_t result = __builtin_ctzll(value);
    return static_cast<uint32_t>(result);
#endif
}

//------------------------------------------------------------------------
// BitOperations::UInt32BitsToSingle: Gets a single-precision floating-point from its underlying bit value.
//
// Arguments:
//    value - The underlying bit value.
//
// Return Value:
//    The single-precision floating-point from value.
//
float BitOperations::UInt32BitsToSingle(uint32_t value)
{
    float result;
    memcpy(&result, &value, sizeof(uint32_t));
    return result;
}

//------------------------------------------------------------------------
// BitOperations::UInt64BitsToDouble: Gets a double-precision floating-point from its underlying bit value.
//
// Arguments:
//    value - The underlying bit value.
//
// Return Value:
//    The double-precision floating-point from value.
//
double BitOperations::UInt64BitsToDouble(uint64_t value)
{
    double result;
    memcpy(&result, &value, sizeof(uint64_t));
    return result;
}

namespace MagicDivide
{
template <int TableBase = 0, int TableSize, typename Magic>
static const Magic* TryGetMagic(const Magic (&table)[TableSize], typename Magic::DivisorType index)
{
    if ((index < TableBase) || (TableBase + TableSize <= index))
    {
        return nullptr;
    }

    const Magic* p = &table[index - TableBase];

    if (p->magic == 0)
    {
        return nullptr;
    }

    return p;
};

template <typename T>
struct UnsignedMagic
{
    typedef T DivisorType;

    T    magic;
    bool increment;
    char postShift;
};

template <typename T>
const UnsignedMagic<T>* TryGetUnsignedMagic(T divisor)
{
    return nullptr;
}

template <>
const UnsignedMagic<uint32_t>* TryGetUnsignedMagic(uint32_t divisor)
{
    static const UnsignedMagic<uint32_t> table[]{
        {0xaaaaaaab, false, 1}, // 3
        {},
        {0xcccccccd, false, 2}, // 5
        {0xaaaaaaab, false, 2}, // 6
        {0x49249249, true, 1},  // 7
        {},
        {0x38e38e39, false, 1}, // 9
        {0xcccccccd, false, 3}, // 10
        {0xba2e8ba3, false, 3}, // 11
        {0xaaaaaaab, false, 3}, // 12
    };

    return TryGetMagic<3>(table, divisor);
}

template <>
const UnsignedMagic<uint64_t>* TryGetUnsignedMagic(uint64_t divisor)
{
    static const UnsignedMagic<uint64_t> table[]{
        {0xaaaaaaaaaaaaaaab, false, 1}, // 3
        {},
        {0xcccccccccccccccd, false, 2}, // 5
        {0xaaaaaaaaaaaaaaab, false, 2}, // 6
        {0x9249249249249249, true, 2},  // 7
        {},
        {0xe38e38e38e38e38f, false, 3}, // 9
        {0xcccccccccccccccd, false, 3}, // 10
        {0x2e8ba2e8ba2e8ba3, false, 1}, // 11
        {0xaaaaaaaaaaaaaaab, false, 3}, // 12
    };

    return TryGetMagic<3>(table, divisor);
}

//------------------------------------------------------------------------
// GetUnsignedMagic: Generates a magic number and shift amount for the magic
// number unsigned division optimization.
//
// Arguments:
//    d     - The divisor
//    increment   - Pointer to a flag indicating if incrementing the numerator is required
//    preShift - Pointer to the pre-shift value to be returned
//    postShift - Pointer to the post-shift value to be returned
//
// Returns:
//    The magic number.
//
// Notes:
//    Based on "Faster Unsigned Division by Constants" by ridiculous_fish.
//    https://ridiculousfish.com/files/faster_unsigned_division_by_constants.pdf
//    https://github.com/ridiculousfish/libdivide/blob/master/doc/divide_by_constants_codegen_reference.c

template <typename T>
T GetUnsignedMagic(T d, bool* increment /*out*/, int* preShift /*out*/, int* postShift /*out*/, unsigned num_bits)
{
    assert((d >= 3) && !isPow2(d));

    // The numerator must fit in a uint
    assert(num_bits > 0 && num_bits <= sizeof(T) * CHAR_BIT);

    // Bits in a uint
    const unsigned UINT_BITS = sizeof(T) * CHAR_BIT;

    if (num_bits == UINT_BITS)
    {
        const UnsignedMagic<T>* magic = TryGetUnsignedMagic(d);

        if (magic != nullptr)
        {
            *increment = magic->increment;
            *preShift  = 0;
            *postShift = magic->postShift;
            return magic->magic;
        }
    }

    // The extra shift implicit in the difference between UINT_BITS and num_bits
    const unsigned extra_shift = UINT_BITS - num_bits;

    // The initial power of 2 is one less than the first one that can possibly work
    const T initial_power_of_2 = (T)1 << (UINT_BITS - 1);

    // The remainder and quotient of our power of 2 divided by d
    T quotient = initial_power_of_2 / d, remainder = initial_power_of_2 % d;

    // The magic info for the variant "round down" algorithm
    T        down_multiplier = 0;
    unsigned down_exponent   = 0;
    int      has_magic_down  = 0;

    // Compute ceil(log_2 D)
    unsigned ceil_log_2_D = 0;
    for (T tmp = d; tmp > 0; tmp >>= 1)
        ceil_log_2_D += 1;

    // Begin a loop that increments the exponent, until we find a power of 2 that works.
    unsigned exponent;
    for (exponent = 0;; exponent++)
    {
        // Quotient and remainder is from previous exponent; compute it for this exponent.
        if (remainder >= d - remainder)
        {
            // Doubling remainder will wrap around D
            quotient  = quotient * 2 + 1;
            remainder = remainder * 2 - d;
        }
        else
        {
            // Remainder will not wrap
            quotient  = quotient * 2;
            remainder = remainder * 2;
        }

        // We're done if this exponent works for the round_up algorithm.
        // Note that exponent may be larger than the maximum shift supported,
        // so the check for >= ceil_log_2_D is critical.
        if ((exponent + extra_shift >= ceil_log_2_D) || (d - remainder) <= ((T)1 << (exponent + extra_shift)))
            break;

        // Set magic_down if we have not set it yet and this exponent works for the round_down algorithm
        if (!has_magic_down && remainder <= ((T)1 << (exponent + extra_shift)))
        {
            has_magic_down  = 1;
            down_multiplier = quotient;
            down_exponent   = exponent;
        }
    }

    if (exponent < ceil_log_2_D)
    {
        // magic_up is efficient
        *increment = false;
        *preShift  = 0;
        *postShift = (int)exponent;
        return quotient + 1;
    }
    else if (d & 1)
    {
        // Odd divisor, so use magic_down, which must have been set
        assert(has_magic_down);
        *increment = true;
        *preShift  = 0;
        *postShift = (int)down_exponent;
        return down_multiplier;
    }
    else
    {
        // Even divisor, so use a prefix-shifted dividend
        unsigned pre_shift = 0;
        T        shifted_D = d;
        while ((shifted_D & 1) == 0)
        {
            shifted_D >>= 1;
            pre_shift += 1;
        }
        T result = GetUnsignedMagic<T>(shifted_D, increment, preShift, postShift, num_bits - pre_shift);
        assert(*increment == 0 && *preShift == 0); // expect no increment or pre_shift in this path
        *preShift = (int)pre_shift;
        return result;
    }
}

uint32_t GetUnsigned32Magic(
    uint32_t d, bool* increment /*out*/, int* preShift /*out*/, int* postShift /*out*/, unsigned bits)
{
    assert(bits <= 32);
    return GetUnsignedMagic<uint32_t>(d, increment, preShift, postShift, bits);
}

#ifdef TARGET_64BIT
uint64_t GetUnsigned64Magic(
    uint64_t d, bool* increment /*out*/, int* preShift /*out*/, int* postShift /*out*/, unsigned bits)
{
    assert(bits <= 64);
    return GetUnsignedMagic<uint64_t>(d, increment, preShift, postShift, bits);
}
#endif

template <typename T>
struct SignedMagic
{
    typedef T DivisorType;

    T   magic;
    int shift;
};

template <typename T>
const SignedMagic<T>* TryGetSignedMagic(T divisor)
{
    return nullptr;
}

template <>
const SignedMagic<int32_t>* TryGetSignedMagic(int32_t divisor)
{
    static const SignedMagic<int32_t> table[]{
        {0x55555556, 0}, // 3
        {},
        {0x66666667, 1},          // 5
        {0x2aaaaaab, 0},          // 6
        {(int32_t)0x92492493, 2}, // 7
        {},
        {0x38e38e39, 1}, // 9
        {0x66666667, 2}, // 10
        {0x2e8ba2e9, 1}, // 11
        {0x2aaaaaab, 1}, // 12
    };

    return TryGetMagic<3>(table, divisor);
}

template <>
const SignedMagic<int64_t>* TryGetSignedMagic(int64_t divisor)
{
    static const SignedMagic<int64_t> table[]{
        {0x5555555555555556, 0}, // 3
        {},
        {0x6666666666666667, 1}, // 5
        {0x2aaaaaaaaaaaaaab, 0}, // 6
        {0x4924924924924925, 1}, // 7
        {},
        {0x1c71c71c71c71c72, 0}, // 9
        {0x6666666666666667, 2}, // 10
        {0x2e8ba2e8ba2e8ba3, 1}, // 11
        {0x2aaaaaaaaaaaaaab, 1}, // 12
    };

    return TryGetMagic<3>(table, divisor);
}

//------------------------------------------------------------------------
// GetSignedMagic: Generates a magic number and shift amount for
// the magic number division optimization.
//
// Arguments:
//    denom - The denominator
//    shift - Pointer to the shift value to be returned
//
// Returns:
//    The magic number.
//
// Notes:
//    This code is previously from UTC where it notes it was taken from
//   _The_PowerPC_Compiler_Writer's_Guide_, pages 57-58. The paper is based on
//   is "Division by invariant integers using multiplication" by Torbjorn Granlund
//   and Peter L. Montgomery in PLDI 94

template <typename T>
T GetSignedMagic(T denom, int* shift /*out*/)
{
    const SignedMagic<T>* magic = TryGetSignedMagic(denom);

    if (magic != nullptr)
    {
        *shift = magic->shift;
        return magic->magic;
    }

    const int bits         = sizeof(T) * 8;
    const int bits_minus_1 = bits - 1;

    typedef typename std::make_unsigned<T>::type UT;

    const UT two_nminus1 = UT(1) << bits_minus_1;

    int p;
    UT  absDenom;
    UT  absNc;
    UT  delta;
    UT  q1;
    UT  r1;
    UT  r2;
    UT  q2;
    UT  t;
    T   result_magic;

    absDenom = abs(denom);
    t        = two_nminus1 + (UT(denom) >> bits_minus_1);
    absNc    = t - 1 - (t % absDenom);        // absolute value of nc
    p        = bits_minus_1;                  // initialize p
    q1       = two_nminus1 / absNc;           // initialize q1 = 2^p / abs(nc)
    r1       = two_nminus1 - (q1 * absNc);    // initialize r1 = rem(2^p, abs(nc))
    q2       = two_nminus1 / absDenom;        // initialize q1 = 2^p / abs(denom)
    r2       = two_nminus1 - (q2 * absDenom); // initialize r1 = rem(2^p, abs(denom))

    do
    {
        p++;
        q1 *= 2; // update q1 = 2^p / abs(nc)
        r1 *= 2; // update r1 = rem(2^p / abs(nc))

        if (r1 >= absNc)
        { // must be unsigned comparison
            q1++;
            r1 -= absNc;
        }

        q2 *= 2; // update q2 = 2^p / abs(denom)
        r2 *= 2; // update r2 = rem(2^p / abs(denom))

        if (r2 >= absDenom)
        { // must be unsigned comparison
            q2++;
            r2 -= absDenom;
        }

        delta = absDenom - r2;
    } while (q1 < delta || (q1 == delta && r1 == 0));

    result_magic = q2 + 1; // resulting magic number
    if (denom < 0)
    {
        result_magic = -result_magic;
    }
    *shift = p - bits; // resulting shift

    return result_magic;
}

int32_t GetSigned32Magic(int32_t d, int* shift /*out*/)
{
    return GetSignedMagic<int32_t>(d, shift);
}

#ifdef TARGET_64BIT
int64_t GetSigned64Magic(int64_t d, int* shift /*out*/)
{
    return GetSignedMagic<int64_t>(d, shift);
}
#endif
}

namespace CheckedOps
{
bool CastFromIntOverflows(int32_t fromValue, var_types toType, bool fromUnsigned)
{
    switch (toType)
    {
        case TYP_BYTE:
        case TYP_UBYTE:
        case TYP_SHORT:
        case TYP_USHORT:
        case TYP_INT:
        case TYP_UINT:
        case TYP_LONG:
        case TYP_ULONG:
            return fromUnsigned ? !FitsIn(toType, static_cast<uint32_t>(fromValue)) : !FitsIn(toType, fromValue);

        case TYP_FLOAT:
        case TYP_DOUBLE:
            return false;

        default:
            unreached();
    }
}

bool CastFromLongOverflows(int64_t fromValue, var_types toType, bool fromUnsigned)
{
    switch (toType)
    {
        case TYP_BYTE:
        case TYP_UBYTE:
        case TYP_SHORT:
        case TYP_USHORT:
        case TYP_INT:
        case TYP_UINT:
        case TYP_LONG:
        case TYP_ULONG:
            return fromUnsigned ? !FitsIn(toType, static_cast<uint64_t>(fromValue)) : !FitsIn(toType, fromValue);

        case TYP_FLOAT:
        case TYP_DOUBLE:
            return false;

        default:
            unreached();
    }
}

//  ________________________________________________
// |                                                |
// |  Casting from floating point to integer types  |
// |________________________________________________|
//
// The code below uses the following pattern to determine if an overflow would
// occur when casting from a floating point type to an integer type:
//
//     return !(MIN <= fromValue && fromValue <= MAX);
//
// This section will provide some background on how MIN and MAX were derived
// and why they are in fact the values to use in that comparison.
//
// First - edge cases:
// 1) NaNs - they compare "false" to normal numbers, which MIN and MAX are, making
//    the condition return "false" as well, which is flipped to true via "!", indicating
//    overflow - exactly what we want.
// 2) Infinities - they are outside of range of any normal numbers, making one of the comparisons
//    always return "false", indicating overflow.
// 3) Subnormal numbers - have no special behavior with respect to comparisons.
// 4) Minus zero - compares equal to "+0", which is what we want as it can be safely cast to an integer "0".
//
// Binary normal floating point numbers are represented in the following format:
//
//     number = sign * (1 + mantissa) * 2^exponent
//
// Where "exponent" is a biased binary integer.
// And "mantissa" is a fixed-point binary fraction of the following form:
//
//     mantissa = bits[1] * 2^-1 + bits[2] * 2^-2 + ... + bits[N] * 2^-N
//
// Where "N" is the number of digits that depends on the width of floating point type
// in question. It is equal to "23" for "float"s and to "52" for "double"s.
//
// If we did our calculations with real numbers, the condition to check would simply be:
//
//     return !((INT_MIN - 1) < fromValue && fromValue < (INT_MAX + 1));
//
// This is because casting uses the "round to zero" semantic: "checked((int)((double)int.MaxValue + 0.9))"
// yields "int.MaxValue" - not an error. Likewise, "checked((int)((double)int.MinValue - 0.9))"
// results in "int.MinValue". However, "checked((int)((double)int.MaxValue + 1))" will not compile.
//
// The problem, of course, is that we are not dealing with real numbers, but rather floating point approximations.
// At the same time, some real numbers can be represented in the floating point world exactly.
// It so happens that both "INT_MIN - 1" and "INT_MAX + 1" can satisfy that requirement for most cases.
// For unsigned integers, where M is the width of the type in bits:
//
//     INT_MIN - 1 = 0 - 1 = -2^0 - exactly representable.
//     INT_MAX + 1 = (2^M - 1) + 1 = 2^M - exactly representable.
//
// For signed integers:
//
//     INT_MIN - 1 = -(2^(M - 1)) - 1 - not always exactly representable.
//     INT_MAX + 1 = (2^(M - 1) - 1) + 1 = 2^(M - 1) - exactly representable.
//
// So, we have simple values for MIN and MAX in all but the signed MIN case.
// To find out what value should be used then, the following equation needs to be solved:
//
//     -(2^(M - 1)) - 1 = -(2^(M - 1)) * (1 + m)
//     1 + 1 / 2^(M - 1) = 1 + m
//     m = 2^(1 - M)
//
// In this case "m" is the "mantissa". The result obtained means that we can find the exact
// value in cases when "|1 - M| <= N" <=> "M <= N + 1" - i. e. the precision is high enough for there to be a position
// in the fixed point mantissa that could represent the "-1". It is the case for the following combinations of types:
//
//     float -> int8 / int16
//     double -> int8 / int16 / int32
//
// For the remaining cases, we could use a value that is the first representable one for the respective type
// and is less than the infinitely precise MIN: -(1 + 2^-N) * 2^(M - 1).
// However, a simpler approach is to just use a different comparison.
// Instead of "MIN < fromValue", we'll do "MIN <= fromValue", where
// MIN is just "-(2^(M - 1))" - the smallest representable value that can be cast safely.
// The following table shows the final values and operations for MIN:
//
//     | Cast            | MIN                     | Comparison |
//     |-----------------|-------------------------|------------|
//     | float -> int8   | -129.0f                 | <          |
//     | float -> int16  | -32769.0f               | <          |
//     | float -> int32  | -2147483648.0f          | <=         |
//     | float -> int64  | -9223372036854775808.0f | <=         |
//     | double -> int8  | -129.0                  | <          |
//     | double -> int16 | -32769.0                | <          |
//     | double -> int32 | -2147483649.0           | <          |
//     | double -> int64 | -9223372036854775808.0  | <=         |
//
// Note: casts from floating point to floating point never overflow.

bool CastFromFloatOverflows(float fromValue, var_types toType)
{
    switch (toType)
    {
        case TYP_BYTE:
            return !(-129.0f < fromValue && fromValue < 128.0f);
        case TYP_UBYTE:
            return !(-1.0f < fromValue && fromValue < 256.0f);
        case TYP_SHORT:
            return !(-32769.0f < fromValue && fromValue < 32768.0f);
        case TYP_USHORT:
            return !(-1.0f < fromValue && fromValue < 65536.0f);
        case TYP_INT:
            return !(-2147483648.0f <= fromValue && fromValue < 2147483648.0f);
        case TYP_UINT:
            return !(-1.0 < fromValue && fromValue < 4294967296.0f);
        case TYP_LONG:
            return !(-9223372036854775808.0 <= fromValue && fromValue < 9223372036854775808.0f);
        case TYP_ULONG:
            return !(-1.0f < fromValue && fromValue < 18446744073709551616.0f);
        case TYP_FLOAT:
        case TYP_DOUBLE:
            return false;
        default:
            unreached();
    }
}

bool CastFromDoubleOverflows(double fromValue, var_types toType)
{
    switch (toType)
    {
        case TYP_BYTE:
            return !(-129.0 < fromValue && fromValue < 128.0);
        case TYP_UBYTE:
            return !(-1.0 < fromValue && fromValue < 256.0);
        case TYP_SHORT:
            return !(-32769.0 < fromValue && fromValue < 32768.0);
        case TYP_USHORT:
            return !(-1.0 < fromValue && fromValue < 65536.0);
        case TYP_INT:
            return !(-2147483649.0 < fromValue && fromValue < 2147483648.0);
        case TYP_UINT:
            return !(-1.0 < fromValue && fromValue < 4294967296.0);
        case TYP_LONG:
            return !(-9223372036854775808.0 <= fromValue && fromValue < 9223372036854775808.0);
        case TYP_ULONG:
            return !(-1.0 < fromValue && fromValue < 18446744073709551616.0);
        case TYP_FLOAT:
        case TYP_DOUBLE:
            return false;
        default:
            unreached();
    }
}
}
