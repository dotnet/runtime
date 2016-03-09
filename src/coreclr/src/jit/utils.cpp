// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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

#define DECLARE_DATA

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


const BYTE          varTypeClassification[] =
{
    #define DEF_TP(tn,nm,jitType,verType,sz,sze,asze,st,al,tf,howUsed) tf,
    #include "typelist.h"
    #undef  DEF_TP
};

/*****************************************************************************/
/*****************************************************************************/
#ifdef DEBUG
extern
const char * const  opcodeNames[] =
{
    #define OPDEF(name,string,pop,push,oprType,opcType,l,s1,s2,ctrl) string,
    #include "opcode.def"
    #undef  OPDEF
};

extern
const BYTE          opcodeArgKinds[] =
{
    #define OPDEF(name,string,pop,push,oprType,opcType,l,s1,s2,ctrl) (BYTE) oprType,
    #include "opcode.def"
    #undef  OPDEF
};
#endif

#if defined(DEBUG) || defined(FEATURE_JIT_METHOD_PERF)
/*****************************************************************************/

const   char *      varTypeName(var_types vt)
{
    static
    const char * const  varTypeNames[] =
    {
        #define DEF_TP(tn,nm,jitType,verType,sz,sze,asze,st,al,tf,howUsed) nm,
        #include "typelist.h"
        #undef  DEF_TP
    };

    assert((unsigned)vt < sizeof(varTypeNames)/sizeof(varTypeNames[0]));

    return  varTypeNames[vt];
}
#endif // DEBUG || FEATURE_JIT_METHOD_PERF

#if defined(DEBUG) || defined(LATE_DISASM)
/*****************************************************************************
 *
 *  Return the name of the given register.
 */

const   char *      getRegName(regNumber reg, bool isFloat)
{
    // Special-case REG_NA; it's not in the regNames array, but we might want to print it.
    if (reg == REG_NA)
    {
        return "NA";
    }
#if defined(_TARGET_X86_) && defined(LEGACY_BACKEND)
    static
    const char * const  regNames[] =
    {
        #define REGDEF(name, rnum, mask, sname) sname,
        #include "register.h"
    };

    static
    const char * const  floatRegNames[] =
    {
        #define REGDEF(name, rnum, mask, sname) sname,
        #include "registerxmm.h"
    };
    if (isFloat) 
    {
        assert(reg < ArrLen(floatRegNames));
        return floatRegNames[reg];
    }
    else
    {
        assert(reg < ArrLen(regNames));
        return regNames[reg];
    }
#elif defined(_TARGET_ARM64_)
    static
    const char * const  regNames[] =
    {
        #define REGDEF(name, rnum, mask, xname, wname) xname,
        #include "register.h"
    };
    assert(reg < ArrLen(regNames));
    return regNames[reg];
#else
    static
    const char * const  regNames[] =
    {
        #define REGDEF(name, rnum, mask, sname) sname,
        #include "register.h"
    };
    assert(reg < ArrLen(regNames));
    return regNames[reg];
#endif

}

const char *    getRegName(unsigned reg, bool isFloat) // this is for gcencode.cpp and disasm.cpp that dont use the regNumber type
{
    return getRegName((regNumber)reg, isFloat);
}
#endif // defined(DEBUG) || defined(LATE_DISASM)

#if defined(DEBUG)

const char* getRegNameFloat(regNumber reg, var_types type)
{
#ifdef _TARGET_ARM_
    assert(genIsValidFloatReg(reg));
    if (type == TYP_FLOAT)
        return getRegName(reg);
    else 
    {
        const char* regName;

        switch (reg) {
        default:
            assert(!"Bad double register");
            regName="d??";
            break;
        case REG_F0:
            regName = "d0"; break;
        case REG_F2:
            regName = "d2"; break;
        case REG_F4:
            regName = "d4"; break;
        case REG_F6:
            regName = "d6"; break;
        case REG_F8:
            regName = "d8"; break;
        case REG_F10:
            regName = "d10"; break;
        case REG_F12:
            regName = "d12"; break;
        case REG_F14:
            regName = "d14"; break;
        case REG_F16:
            regName = "d16"; break;
        case REG_F18:
            regName = "d18"; break;
        case REG_F20:
            regName = "d20"; break;
        case REG_F22:
            regName = "d22"; break;
        case REG_F24:
            regName = "d24"; break;
        case REG_F26:
            regName = "d26"; break;
        case REG_F28:
            regName = "d28"; break;
        case REG_F30:
            regName = "d30"; break;
        }
        return regName;
    }

#elif defined(_TARGET_X86_) && defined(LEGACY_BACKEND)

    static const char* regNamesFloat[] =
    {
        #define REGDEF(name, rnum, mask, sname) sname,
        #include "registerxmm.h"
    };
    assert((unsigned)reg < ArrLen(regNamesFloat));

    return regNamesFloat[reg];

#elif defined(_TARGET_ARM64_)

    static const char* regNamesFloat[] =
    {
        #define REGDEF(name, rnum, mask, xname, wname) xname,
        #include "register.h"
    };
    assert((unsigned)reg < ArrLen(regNamesFloat));

    return regNamesFloat[reg];

#else
    static const char* regNamesFloat[] =
    {
        #define REGDEF(name, rnum, mask, sname) "x" sname,
        #include "register.h"
    };
#ifdef FEATURE_AVX_SUPPORT
    static const char* regNamesYMM[] =
    {
        #define REGDEF(name, rnum, mask, sname) "y" sname,
        #include "register.h"
    };
#endif // FEATURE_AVX_SUPPORT
    assert((unsigned)reg < ArrLen(regNamesFloat));

#ifdef FEATURE_AVX_SUPPORT
    if (type == TYP_SIMD32)
    {
        return regNamesYMM[reg];
    }
#endif // FEATURE_AVX_SUPPORT

    return regNamesFloat[reg];
#endif
}

/*****************************************************************************
 *
 *  Displays a register set.
 *  TODO-ARM64-Cleanup: don't allow ip0, ip1 as part of a range.
 */

void                dspRegMask(regMaskTP regMask, size_t minSiz)
{
    const char* sep = "";

    printf("[");

    bool      inRegRange = false;
    regNumber regPrev = REG_NA;
    regNumber regHead = REG_NA; // When we start a range, remember the first register of the range, so we don't use range notation if the range contains just a single register.
    for (regNumber regNum = REG_INT_FIRST; regNum <= REG_INT_LAST; regNum = REG_NEXT(regNum))
    {
        regMaskTP   regBit = genRegMask(regNum);
       
        if ((regMask & regBit) != 0)
        {
            // We have a register to display. It gets displayed now if:
            // 1. This is the first register to display of a new range of registers (possibly because
            //    no register has ever been displayed).
            // 2. This is the last register of an acceptable range (either the last integer register,
            //    or the last of a range that is displayed with range notation).
            if (!inRegRange)
            {
                // It's the first register of a potential range.
                const char* nam = getRegName(regNum);
                printf("%s%s", sep, nam);
                minSiz -= strlen(sep) + strlen(nam);

                // By default, we're not starting a potential register range.
                sep = " ";

                // What kind of separator should we use for this range (if it is indeed going to be a range)?
#if defined(_TARGET_AMD64_)
                // For AMD64, create ranges for int registers R8 through R15, but not the "old" registers.
                if (regNum >= REG_R8)
                {
                    regHead = regNum;
                    inRegRange = true;
                    sep = "-";
                }
#elif defined(_TARGET_ARM64_)
                // R17 and R28 can't be the start of a range, since the range would include TEB or FP
                if ((regNum < REG_R17) ||
                    ((REG_R19 <= regNum) && (regNum < REG_R28)))
                {
                    regHead = regNum;
                    inRegRange = true;
                    sep = "-";
                }
#elif defined(_TARGET_ARM_)
                if (regNum < REG_R12)
                {
                    regHead = regNum;
                    inRegRange = true;
                    sep = "-";
                }
#elif defined(_TARGET_X86_)
                // No register ranges
#else // _TARGET_*
#error Unsupported or unset target architecture
#endif // _TARGET_*
            }
            // We've already printed a register. Is this the end of a range?
#if defined(_TARGET_ARM64_)
            else if ((regNum == REG_INT_LAST)
                     || (regNum == REG_R17) // last register before TEB
                     || (regNum == REG_R28)) // last register before FP
#else // _TARGET_ARM64_
            else if (regNum == REG_INT_LAST)
#endif // _TARGET_ARM64_
            {
                const char* nam = getRegName(regNum);
                printf("%s%s", sep, nam);
                minSiz -= strlen(sep) + strlen(nam);
                inRegRange = false; // No longer in the middle of a register range
                regHead = REG_NA;
                sep = " ";
            }
        } 
        else // ((regMask & regBit) == 0)
        {
            if (inRegRange)
            {
                assert(regHead != REG_NA);
                if (regPrev != regHead)
                {
                    // Close out the previous range, if it included more than one register.
                    const char* nam = getRegName(regPrev);
                    printf("%s%s", sep, nam);
                    minSiz -= strlen(sep) + strlen(nam);
                }
                sep = " ";
                inRegRange = false;
                regHead = REG_NA;
            }
        }

        if (regBit > regMask)
            break;

        regPrev = regNum;
    }

#if CPU_HAS_BYTE_REGS
    if (regMask & RBM_BYTE_REG_FLAG)
    {
        const char *  nam = "BYTE";
        printf("%s%s", sep, nam);
        minSiz -= (strlen(sep) + strlen(nam));   
    }
#endif

#if !FEATURE_STACK_FP_X87
    if (strlen(sep) > 0)
    {
        // We've already printed something.
        sep = " ";
    }
    inRegRange = false;
    regPrev = REG_NA;
    regHead = REG_NA;
    for (regNumber regNum = REG_FP_FIRST; regNum <= REG_FP_LAST; regNum = REG_NEXT(regNum))
    {
        regMaskTP   regBit = genRegMask(regNum);

        if (regMask & regBit)
        {
            if (!inRegRange || (regNum == REG_FP_LAST))
            {
                const char* nam = getRegName(regNum);
                printf("%s%s", sep, nam);
                minSiz -= strlen(sep) + strlen(nam);
                sep = "-";
                regHead = regNum;
            }
            inRegRange = true;
        }
        else
        {
            if (inRegRange)
            {
                if (regPrev != regHead)
                {
                    const   char *  nam = getRegName(regPrev);
                    printf("%s%s", sep, nam);
                    minSiz -= (strlen(sep) + strlen(nam));
                }
                sep = " ";
            }
            inRegRange = false;
        }

        if (regBit > regMask)
            break;

        regPrev = regNum;
    }
#endif

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
void
dumpILBytes(const BYTE* const codeAddr,
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
unsigned
dumpSingleInstr(const BYTE* const codeAddr, IL_OFFSET offs, const char* prefix)
{
    const BYTE  *        opcodePtr = codeAddr + offs;
    const BYTE  *   startOpcodePtr = opcodePtr;
    const unsigned ALIGN_WIDTH = 3 * 6; // assume 3 characters * (1 byte opcode + 4 bytes data + 1 prefix byte) for most things

    if (prefix != NULL)
        printf("%s", prefix);

    OPCODE      opcode = (OPCODE) getU1LittleEndian(opcodePtr);
    opcodePtr += sizeof(__int8);

DECODE_OPCODE:

    if (opcode >= CEE_COUNT)
    {
        printf("\nIllegal opcode: %02X\n", (int) opcode);
        return (IL_OFFSET)(opcodePtr - startOpcodePtr);
    }

    /* Get the size of additional parameters */

    size_t      sz      = opcodeSizes   [opcode];
    unsigned    argKind = opcodeArgKinds[opcode];

    /* See what kind of an opcode we have, then */

    switch (opcode)
    {
        case CEE_PREFIX1:
            opcode = OPCODE(getU1LittleEndian(opcodePtr) + 256);
            opcodePtr += sizeof(__int8);
            goto DECODE_OPCODE;

        default:
        {
            __int64     iOp;
            double      dOp;
            int         jOp;
            DWORD       jOp2;

            switch (argKind)
            {
            case InlineNone      :
                                    dumpILBytes(startOpcodePtr, (unsigned)(opcodePtr - startOpcodePtr), ALIGN_WIDTH);
                                    printf(" %-12s", opcodeNames[opcode]);
                                    break;

            case ShortInlineVar  :   iOp  = getU1LittleEndian(opcodePtr);  goto INT_OP;
            case ShortInlineI    :   iOp  = getI1LittleEndian(opcodePtr);  goto INT_OP;
            case InlineVar       :   iOp  = getU2LittleEndian(opcodePtr);  goto INT_OP;
            case InlineTok       :
            case InlineMethod    :
            case InlineField     :
            case InlineType      :
            case InlineString    :
            case InlineSig       :
            case InlineI         :   iOp  = getI4LittleEndian(opcodePtr);  goto INT_OP;
            case InlineI8        :   iOp  = getU4LittleEndian(opcodePtr);
                                     iOp |= (__int64)getU4LittleEndian(opcodePtr + 4) << 32;
                                    goto INT_OP;

        INT_OP:
                                    dumpILBytes(startOpcodePtr, (unsigned)((opcodePtr - startOpcodePtr) + sz), ALIGN_WIDTH);
                                    printf(" %-12s 0x%X", opcodeNames[opcode], iOp);
                                    break;

            case ShortInlineR    :  dOp  = getR4LittleEndian(opcodePtr);  goto FLT_OP;
            case InlineR         :  dOp  = getR8LittleEndian(opcodePtr);  goto FLT_OP;

        FLT_OP:  
                                    dumpILBytes(startOpcodePtr, (unsigned)((opcodePtr - startOpcodePtr) + sz), ALIGN_WIDTH);
                                    printf(" %-12s %f", opcodeNames[opcode], dOp);
                                    break;

            case ShortInlineBrTarget:  jOp  = getI1LittleEndian(opcodePtr);  goto JMP_OP;
            case InlineBrTarget:       jOp  = getI4LittleEndian(opcodePtr);  goto JMP_OP;

        JMP_OP:  
                                    dumpILBytes(startOpcodePtr, (unsigned)((opcodePtr - startOpcodePtr) + sz), ALIGN_WIDTH);
                                    printf(" %-12s %d (IL_%04x)",
                                           opcodeNames[opcode],
                                           jOp,
                                           (int)(opcodePtr + sz - codeAddr) + jOp);
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

            default         : assert(!"Bad argKind");
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
void
dumpILRange(const BYTE* const codeAddr,
            unsigned          codeSize) // in bytes
{
    for (IL_OFFSET offs = 0; offs < codeSize; )
    {
        char prefix[100];
        sprintf(prefix, "IL_%04x ", offs);
        unsigned codeBytesDumped = dumpSingleInstr(codeAddr, offs, prefix);
        offs += codeBytesDumped;
    }
}

/*****************************************************************************
 *
 *  Display a variable set (which may be a 32-bit or 64-bit number); only
 *  one or two of these can be used at once.
 */


const   char *      genES2str(EXPSET_TP set)
{
    const int bufSize = 17;
    static
    char            num1[bufSize];

    static
    char            num2[bufSize];

    static
    char    *       nump = num1;

    char    *       temp = nump;

    nump = (nump == num1) ? num2
                          : num1;

#if EXPSET_SZ == 32
    sprintf_s(temp, bufSize, "%08X", set);
#else
    sprintf_s(temp, bufSize, "%08X%08X", (int)(set >> 32), (int)set);
#endif

    return  temp;
}


const   char *      refCntWtd2str(unsigned refCntWtd)
{
    const int bufSize = 17;
    static
    char            num1[bufSize];

    static
    char            num2[bufSize];

    static
    char    *       nump = num1;

    char    *       temp = nump;

    nump = (nump == num1) ? num2
                          : num1;

    unsigned valueInt  = refCntWtd / BB_UNITY_WEIGHT;
    unsigned valueFrac = refCntWtd % BB_UNITY_WEIGHT;

    if (valueFrac == 0)
    {
       sprintf_s(temp, bufSize, "%2u  ", valueInt);        
    }
    else     {
       sprintf_s(temp, bufSize, "%2u.%1u", valueInt, (valueFrac*10/BB_UNITY_WEIGHT));
    }

    return  temp;
}

#define MAX_RANGE 0xfffff

/**************************************************************************/
bool ConfigMethodRange::contains(ICorJitInfo* info, CORINFO_METHOD_HANDLE method) 
{
    _ASSERT(m_inited == 1);

    if (m_lastRange == 0)   // no range mean everything
        return true;

    unsigned hash = info->getMethodHash(method)%MAX_RANGE;
    assert(hash < MAX_RANGE);
    int i = 0;

    for (i=0 ; i<m_lastRange ; i+=2) 
    {
        if (m_ranges[i]<=hash && hash<=m_ranges[i+1])
        {
            return true;
        }        
    }

    return false;
}

/**************************************************************************/
void ConfigMethodRange::initRanges(const wchar_t* rangeStr)
{
    // make sure that the memory was zero initialized
    _ASSERTE(m_inited == 0 || m_inited == 1);

    if (rangeStr == nullptr)
    {
        m_inited = true;
        return;
    }

    LPCWSTR p = const_cast<LPCWSTR>(rangeStr);
    unsigned char lastRange = 0;
    while (*p) {
        while (*p == ' ')       //skip blanks
            p++;
        int i = 0;
        while ('0' <= *p && *p <= '9')
        {
            i = 10*i + ((*p++) - '0');
        }
        m_ranges[lastRange++] = i;

        while (*p == ' ')
            p++;

        // Have we read only the first part of a (possible) pair?
        if (lastRange & 1) 
        {
            // Is this entry of the form "beg-end" or simply "num"?
            if (*p == '-')
                p++; // Skip over the '-' to get to "end"
            else
                m_ranges[lastRange++] = i; // This is just "num".
        }
    }
    if (lastRange & 1) 
        m_ranges[lastRange++] = MAX_RANGE;
    assert(lastRange < 100);
    m_lastRange = lastRange;

    m_inited = true;
}

#endif // DEBUG


#if CALL_ARG_STATS || COUNT_BASIC_BLOCKS || COUNT_LOOPS || EMITTER_STATS || MEASURE_NODE_SIZE

/*****************************************************************************
 *  Histogram class.
 */

Histogram::Histogram(IAllocator* allocator, const unsigned* const sizeTable)
    : m_allocator(allocator)
    , m_sizeTable(sizeTable)
    , m_counts(nullptr)
{
    unsigned sizeCount = 0;
    do
    {
        sizeCount++;
    }
    while ((sizeTable[sizeCount] != 0) && (sizeCount < 1000));

    m_sizeCount = sizeCount;
}

Histogram::~Histogram()
{
    m_allocator->Free(m_counts);
}

// We need to lazy allocate the histogram data so static `Histogram` variables don't try to
// call the host memory allocator in the loader lock, which doesn't work.
void Histogram::ensureAllocated()
{
    if (m_counts == nullptr)
    {
        m_counts = new (m_allocator) unsigned[m_sizeCount + 1];
        memset(m_counts, 0, (m_sizeCount + 1) * sizeof(*m_counts));
    }
}

void Histogram::dump(FILE* output)
{
    ensureAllocated();

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

        c += m_counts[i];

        fprintf(output, " ===> %7u count (%3u%% of total)\n", m_counts[i], (int)(100.0 * c / t));
    }
}

void Histogram::record(unsigned size)
{
    ensureAllocated();

    unsigned i;
    for (i = 0; i < m_sizeCount; i++)
    {
        if (m_sizeTable[i] >= size)
        {
            break;
        }
    }

    m_counts[i]++;
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
FixedBitVect * FixedBitVect::bitVectInit(UINT size, Compiler *comp)
{
    UINT bitVectMemSize, numberOfChunks;
    FixedBitVect *bv;
    
    assert(size != 0);

    numberOfChunks = (size - 1) / bitChunkSize() + 1;
    bitVectMemSize = numberOfChunks * (bitChunkSize() / 8);  // size in bytes
    
    assert(bitVectMemSize * bitChunkSize() >= size);
    
    bv = (FixedBitVect *)comp->compGetMemA(sizeof(FixedBitVect) + bitVectMemSize, CMK_FixedBitVect);
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
void FixedBitVect::bitVectOr(FixedBitVect *bv)
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
void FixedBitVect::bitVectAnd(FixedBitVect &bv)
{
    UINT bitChunkCnt = (bitVectSize - 1) / bitChunkSize() + 1;
    
    assert(bitVectSize == bv.bitVectSize);

    // And each chunks
    for (UINT i = 0; i < bitChunkCnt ; i++)
    {
        bitVect[i] &= bv.bitVect[i];
    }
}

// bitVectGetFirst() - Find the first bit on and return bit num,
//                    Return -1 if no bits found.
UINT FixedBitVect::bitVectGetFirst()
{
    return bitVectGetNext((UINT) -1);
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
        index = 0;
        bitMask = (UINT)-1;
    }
    else
    {
        UINT bit;

        index = bitNumPrev / bitChunkSize();
        bitNumPrev -= index * bitChunkSize();
        bit = bitNumToBit(bitNumPrev);
        bitMask = ~(bit | (bit - 1));
    }


    // Find first bit
    for (i = index; i < bitChunkCnt ; i++)
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
        return (UINT)-1;

    bitNum += i * bitChunkSize();

    assert(bitNum <= bitVectSize);

    return bitNum;
}

// bitVectGetNextAndClear() - Find the first bit on, clear it and return it.
//                            Return -1 if no bits found.
UINT FixedBitVect::bitVectGetNextAndClear()
{
    UINT bitNum = (UINT)-1;
    UINT bitChunkCnt = (bitVectSize - 1) / bitChunkSize() + 1;
    UINT i;

    // Find first bit
    for (i = 0; i < bitChunkCnt ; i++)
    {
        if (bitVect[i] != 0)
        {
            BitScanForward((ULONG*)&bitNum, bitVect[i]);
            break;
        }
    }

    // Empty bit vector?
    if (bitNum == (UINT)-1)
        return (UINT)-1;

    // Clear the bit in the right chunk
    bitVect[i] &= ~bitNumToBit(bitNum);

    bitNum += i * bitChunkSize();

    assert(bitNum <= bitVectSize);

    return bitNum;
}

int SimpleSprintf_s(__in_ecount(cbBufSize - (pWriteStart- pBufStart)) char * pWriteStart,
                    __in_ecount(cbBufSize) char * pBufStart, size_t cbBufSize,
                    __in_z const char * fmt, ...)
{
    _ASSERTE(fmt);
    _ASSERTE(pBufStart);
    _ASSERTE(pWriteStart);
    _ASSERTE((size_t)pBufStart <= (size_t)pWriteStart);
    int ret;

    //compute the space left in the buffer.
    if ((pBufStart + cbBufSize) < pWriteStart)
        NO_WAY("pWriteStart is past end of buffer");
    size_t cbSpaceLeft = (size_t)((pBufStart + cbBufSize) - pWriteStart);
    va_list args;
    va_start(args, fmt);
    ret = vsprintf_s(pWriteStart, cbSpaceLeft, const_cast<char*>(fmt), args);
    va_end(args);
    if (ret < 0)
        NO_WAY("vsprintf_s failed.");
    return ret;
}

#ifdef  DEBUG

void                hexDump(FILE* dmpf, const char* name, BYTE* addr, size_t size)
{
    if  (!size)
        return;

    assert(addr);

    fprintf(dmpf, "Hex dump of %s:\n", name);

    for (unsigned i = 0; i < size; i++)
    {
        if  ((i % 16) == 0)
        {
            fprintf(dmpf, "\n    %04X: ", i);
        }

        fprintf(dmpf, "%02X ", *addr++);
    }

    fprintf(dmpf, "\n\n");
}

#endif // DEBUG

void HelperCallProperties::init()
{        
    for (CorInfoHelpFunc helper=CORINFO_HELP_UNDEF;    // initialize helper
         (helper < CORINFO_HELP_COUNT);                // test helper for loop exit
         helper = CorInfoHelpFunc( int(helper) + 1 ) ) // update helper to next            
    {
        // Generally you want initialize these to their most typical/safest result
        //
        bool isPure        = false;      // true if the result only depends upon input args and not any global state    
        bool noThrow       = false;      // true if the helper will never throw
        bool nonNullReturn = false;      // true if the result will never be null or zero
        bool isAllocator   = false;      // true if the result is usually a newly created heap item, or may throw OutOfMemory  
        bool mutatesHeap   = false;      // true if any previous heap objects [are|can be] modified
        bool mayRunCctor   = false;      // true if the helper call may cause a static constructor to be run.
        bool mayFinalize   = false;      // true if the helper call allocates an object that may need to run a finalizer
        
        switch (helper)
        {
            // Arithmetic helpers that cannot throw
        case CORINFO_HELP_LLSH:
        case CORINFO_HELP_LRSH:
        case CORINFO_HELP_LRSZ:
        case CORINFO_HELP_LMUL:
        case CORINFO_HELP_ULDIV:
        case CORINFO_HELP_ULMOD:
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
            
            isPure   = true;
            noThrow  = true;
            break;

            // Arithmetic helpers that *can* throw.


            // This (or these) are not pure, in that they have "VM side effects"...but they don't mutate the heap.
        case CORINFO_HELP_ENDCATCH:
            break;
            
            // Arithmetic helpers that may throw
        case CORINFO_HELP_LMOD:   // Mods throw div-by zero, and signed mods have problems with the smallest integer mod -1,
        case CORINFO_HELP_MOD:    // which is not representable as a positive integer.
        case CORINFO_HELP_UMOD:

        case CORINFO_HELP_UDIV:   // Divs throw divide-by-zero.
        case CORINFO_HELP_LDIV:

        case CORINFO_HELP_LMUL_OVF:
        case CORINFO_HELP_ULMUL_OVF:
        case CORINFO_HELP_DBL2INT_OVF:
        case CORINFO_HELP_DBL2LNG_OVF:
        case CORINFO_HELP_DBL2UINT_OVF:
        case CORINFO_HELP_DBL2ULNG_OVF:
            
            isPure  = true;
            break;
            
            // Heap Allocation helpers, these all never return null
        case CORINFO_HELP_NEWSFAST:
        case CORINFO_HELP_NEWSFAST_ALIGN8:

            isAllocator   = true;
            nonNullReturn = true;
            noThrow       = true;  // only can throw OutOfMemory
            break;

        case CORINFO_HELP_NEW_CROSSCONTEXT:
        case CORINFO_HELP_NEWFAST:
        case CORINFO_HELP_READYTORUN_NEW:

            mayFinalize   = true;  // These may run a finalizer
            isAllocator   = true;
            nonNullReturn = true;
            noThrow       = true;  // only can throw OutOfMemory
            break;
            
            // These allocation helpers do some checks on the size (and lower bound) inputs,
            // and can throw exceptions other than OOM.
        case CORINFO_HELP_NEWARR_1_VC:
        case CORINFO_HELP_NEWARR_1_ALIGN8:

            isAllocator   = true;
            nonNullReturn = true;
            break;

            // These allocation helpers do some checks on the size (and lower bound) inputs,
            // and can throw exceptions other than OOM.
        case CORINFO_HELP_NEW_MDARR:
        case CORINFO_HELP_NEWARR_1_DIRECT:
        case CORINFO_HELP_NEWARR_1_OBJ:
        case CORINFO_HELP_READYTORUN_NEWARR_1:

            mayFinalize   = true;  // These may run a finalizer
            isAllocator   = true;
            nonNullReturn = true;
            break;
            
            // Heap Allocation helpers that are also pure
        case CORINFO_HELP_STRCNS:
            
            isPure        = true;
            isAllocator   = true;
            nonNullReturn = true;
            noThrow       = true;  // only can throw OutOfMemory
            break;

        case CORINFO_HELP_BOX: 
            nonNullReturn = true;
            isAllocator   = true;
            noThrow       = true;  // only can throw OutOfMemory
            break;

        case CORINFO_HELP_BOX_NULLABLE: 
            // Box Nullable is not a 'pure' function
            // It has a Byref argument that it reads the contents of.
            //
            // So two calls to Box Nullable that pass the same address (with the same Value Number)
            // will produce different results when the contents of the memory pointed to by the Byref changes
            //
            isAllocator   = true;
            noThrow       = true;  // only can throw OutOfMemory
            break;
            
        case CORINFO_HELP_RUNTIMEHANDLE_METHOD:
        case CORINFO_HELP_RUNTIMEHANDLE_CLASS:
        case CORINFO_HELP_RUNTIMEHANDLE_METHOD_LOG:
        case CORINFO_HELP_RUNTIMEHANDLE_CLASS_LOG:
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

            isPure   = true;
            noThrow  = true;   // These return null for a failing cast
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
        case CORINFO_HELP_GETREFANY:
        case CORINFO_HELP_LDELEMA_REF:
            
            isPure = true;
            break;
            
            // helpers that return internal handle
            // TODO-ARM64-Bug?: Can these throw or not?
        case CORINFO_HELP_GETCLASSFROMMETHODPARAM:
        case CORINFO_HELP_GETSYNCFROMCLASSHANDLE:
            
            isPure = true;
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
        case CORINFO_HELP_GETSTATICFIELDADDR_CONTEXT:
        case CORINFO_HELP_GETSTATICFIELDADDR_TLS:
        case CORINFO_HELP_GETGENERICS_GCSTATIC_BASE:
        case CORINFO_HELP_GETGENERICS_NONGCSTATIC_BASE:
        case CORINFO_HELP_READYTORUN_STATIC_BASE:

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
        case CORINFO_HELP_GETSHARED_NONGCTHREADSTATIC_BASE_NOCTOR:

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
#if COR_JIT_EE_VERSION > 460
        case CORINFO_HELP_THROWNULLREF:
#endif // COR_JIT_EE_VERSION
        case CORINFO_HELP_THROW:
        case CORINFO_HELP_RETHROW:

            break;  

            // These helper calls may throw an exception
        case CORINFO_HELP_METHOD_ACCESS_CHECK:
        case CORINFO_HELP_FIELD_ACCESS_CHECK:
        case CORINFO_HELP_CLASS_ACCESS_CHECK:
        case CORINFO_HELP_DELEGATE_SECURITY_CHECK:
            
            break;  

            // This is a debugging aid; it simply returns a constant address.
        case CORINFO_HELP_LOOP_CLONE_CHOICE_ADDR:
            isPure = true;
            noThrow = true;
            break;

            // Not sure how to handle optimization involving the rest of these  helpers
        default:
            
            // The most pessimistic results are returned for these helpers
            mutatesHeap = true;
            break;
        }
        
        m_isPure       [helper] = isPure;
        m_noThrow      [helper] = noThrow;
        m_nonNullReturn[helper] = nonNullReturn;
        m_isAllocator  [helper] = isAllocator;
        m_mutatesHeap  [helper] = mutatesHeap;
        m_mayRunCctor  [helper] = mayRunCctor;
        m_mayFinalize  [helper] = mayFinalize;
    }
}


//=============================================================================
// AssemblyNamesList2
//=============================================================================
// The string should be of the form
// MyAssembly
// MyAssembly;mscorlib;System
// MyAssembly;mscorlib System

AssemblyNamesList2::AssemblyNamesList2(const wchar_t* list, IAllocator* alloc)
    : m_alloc(alloc)
{
    assert(m_alloc != nullptr);

    WCHAR prevChar = '?'; // dummy
    LPWSTR nameStart = nullptr; // start of the name currently being processed. nullptr if no current name
    AssemblyName** ppPrevLink = &m_pNames;
    
    for (LPWSTR listWalk = const_cast<LPWSTR>(list); prevChar != '\0'; prevChar = *listWalk, listWalk++)
    {
        WCHAR curChar = *listWalk;
        
        if (iswspace(curChar) || curChar == W(';') || curChar == W('\0') )
        {
            //
            // Found white-space
            //
            
            if (nameStart)
            {
                // Found the end of the current name; add a new assembly name to the list.
                
                AssemblyName* newName = new (m_alloc) AssemblyName();
                
                // Null out the current character so we can do zero-terminated string work; we'll restore it later.
                *listWalk = W('\0');

                // How much space do we need?
                int convertedNameLenBytes = WszWideCharToMultiByte(CP_UTF8, 0, nameStart, -1, NULL, 0, NULL, NULL);
                newName->m_assemblyName = new (m_alloc) char[convertedNameLenBytes]; // convertedNameLenBytes includes the trailing null character
                if (WszWideCharToMultiByte(CP_UTF8, 0, nameStart, -1, newName->m_assemblyName, convertedNameLenBytes, NULL, NULL) != 0)
                {
                    *ppPrevLink = newName;
                    ppPrevLink = &newName->m_next;
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
    *ppPrevLink = nullptr; // Terminate the last element of the list.
}

AssemblyNamesList2::~AssemblyNamesList2()
{
    for (AssemblyName* pName = m_pNames; pName != nullptr; /**/)
    {
        AssemblyName* cur = pName;
        pName = pName->m_next;

        m_alloc->Free(cur->m_assemblyName);
        m_alloc->Free(cur);
    }
}

bool AssemblyNamesList2::IsInList(LPCUTF8 assemblyName)
{
    for (AssemblyName* pName = m_pNames; pName != nullptr; pName = pName->m_next)
    {
        if (_stricmp(pName->m_assemblyName, assemblyName) == 0)
            return true;
    }

    return false;
}

#ifdef FEATURE_JIT_METHOD_PERF
CycleCount::CycleCount()
    : cps(CycleTimer::CyclesPerSecond())
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
    (void) GetCycles(&nowCycles);
    return ((double) (nowCycles - beginCycles) / cps) * 1000.0;
}

bool PerfCounter::Start()
{
    bool result = QueryPerformanceFrequency(&beg) != 0;
    if (!result)
    {
        return result;
    }
    freq = (double) beg.QuadPart / 1000.0;
    (void) QueryPerformanceCounter(&beg);
    return result;
}

// Return elapsed time from Start() in millis.
double PerfCounter::ElapsedTime()
{
    LARGE_INTEGER li;
    (void) QueryPerformanceCounter(&li);
    return (double) (li.QuadPart - beg.QuadPart) / freq;
}

#endif


#ifdef DEBUG

/*****************************************************************************
 * Return the number of digits in a number of the given base (default base 10).
 * Used when outputting strings.
 */
unsigned            CountDigits(unsigned num, unsigned base /* = 10 */)
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
