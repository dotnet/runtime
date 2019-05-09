// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XX                                                                           XX
XX                          Compiler                                         XX
XX                                                                           XX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
*/
#include "jitpch.h"
#ifdef _MSC_VER
#pragma hdrstop
#endif // _MSC_VER
#include "hostallocator.h"
#include "emit.h"
#include "ssabuilder.h"
#include "valuenum.h"
#include "rangecheck.h"
#include "lower.h"
#include "stacklevelsetter.h"
#include "jittelemetry.h"

#if defined(DEBUG)
// Column settings for COMPlus_JitDumpIR.  We could(should) make these programmable.
#define COLUMN_OPCODE 30
#define COLUMN_OPERANDS (COLUMN_OPCODE + 25)
#define COLUMN_KINDS 110
#define COLUMN_FLAGS (COLUMN_KINDS + 32)
#endif

#if defined(DEBUG)
unsigned Compiler::jitTotalMethodCompiled = 0;
#endif // defined(DEBUG)

#if defined(DEBUG)
LONG Compiler::jitNestingLevel = 0;
#endif // defined(DEBUG)

#ifdef ALT_JIT
// static
bool                Compiler::s_pAltJitExcludeAssembliesListInitialized = false;
AssemblyNamesList2* Compiler::s_pAltJitExcludeAssembliesList            = nullptr;
#endif // ALT_JIT

#ifdef DEBUG
// static
bool                Compiler::s_pJitDisasmIncludeAssembliesListInitialized = false;
AssemblyNamesList2* Compiler::s_pJitDisasmIncludeAssembliesList            = nullptr;

// static
bool       Compiler::s_pJitFunctionFileInitialized = false;
MethodSet* Compiler::s_pJitMethodSet               = nullptr;
#endif // DEBUG

/*****************************************************************************
 *
 *  Little helpers to grab the current cycle counter value; this is done
 *  differently based on target architecture, host toolchain, etc. The
 *  main thing is to keep the overhead absolutely minimal; in fact, on
 *  x86/x64 we use RDTSC even though it's not thread-safe; GetThreadCycles
 *  (which is monotonous) is just too expensive.
 */
#ifdef FEATURE_JIT_METHOD_PERF

#if defined(_HOST_X86_) || defined(_HOST_AMD64_)

#if defined(_MSC_VER)

#include <intrin.h>
inline bool _our_GetThreadCycles(unsigned __int64* cycleOut)
{
    *cycleOut = __rdtsc();
    return true;
}

#elif defined(__GNUC__)

inline bool _our_GetThreadCycles(unsigned __int64* cycleOut)
{
    uint32_t hi, lo;
    __asm__ __volatile__("rdtsc" : "=a"(lo), "=d"(hi));
    *cycleOut = (static_cast<unsigned __int64>(hi) << 32) | static_cast<unsigned __int64>(lo);
    return true;
}

#else // neither _MSC_VER nor __GNUC__

// The following *might* work - might as well try.
#define _our_GetThreadCycles(cp) GetThreadCycles(cp)

#endif

#elif defined(_HOST_ARM_) || defined(_HOST_ARM64_)

// If this doesn't work please see ../gc/gc.cpp for additional ARM
// info (and possible solutions).
#define _our_GetThreadCycles(cp) GetThreadCycles(cp)

#else // not x86/x64 and not ARM

// Don't know what this target is, but let's give it a try; if
// someone really wants to make this work, please add the right
// code here.
#define _our_GetThreadCycles(cp) GetThreadCycles(cp)

#endif // which host OS

#endif // FEATURE_JIT_METHOD_PERF
/*****************************************************************************/
inline unsigned getCurTime()
{
    SYSTEMTIME tim;

    GetSystemTime(&tim);

    return (((tim.wHour * 60) + tim.wMinute) * 60 + tim.wSecond) * 1000 + tim.wMilliseconds;
}

/*****************************************************************************/
#ifdef DEBUG
/*****************************************************************************/

static FILE* jitSrcFilePtr;

static unsigned jitCurSrcLine;

void Compiler::JitLogEE(unsigned level, const char* fmt, ...)
{
    va_list args;

    if (verbose)
    {
        va_start(args, fmt);
        vflogf(jitstdout, fmt, args);
        va_end(args);
    }

    va_start(args, fmt);
    vlogf(level, fmt, args);
    va_end(args);
}

void Compiler::compDspSrcLinesByLineNum(unsigned line, bool seek)
{
    if (!jitSrcFilePtr)
    {
        return;
    }

    if (jitCurSrcLine == line)
    {
        return;
    }

    if (jitCurSrcLine > line)
    {
        if (!seek)
        {
            return;
        }

        if (fseek(jitSrcFilePtr, 0, SEEK_SET) != 0)
        {
            printf("Compiler::compDspSrcLinesByLineNum:  fseek returned an error.\n");
        }
        jitCurSrcLine = 0;
    }

    if (!seek)
    {
        printf(";\n");
    }

    do
    {
        char   temp[128];
        size_t llen;

        if (!fgets(temp, sizeof(temp), jitSrcFilePtr))
        {
            return;
        }

        if (seek)
        {
            continue;
        }

        llen = strlen(temp);
        if (llen && temp[llen - 1] == '\n')
        {
            temp[llen - 1] = 0;
        }

        printf(";   %s\n", temp);
    } while (++jitCurSrcLine < line);

    if (!seek)
    {
        printf(";\n");
    }
}

/*****************************************************************************/

void Compiler::compDspSrcLinesByNativeIP(UNATIVE_OFFSET curIP)
{
    static IPmappingDsc* nextMappingDsc;
    static unsigned      lastLine;

    if (!opts.dspLines)
    {
        return;
    }

    if (curIP == 0)
    {
        if (genIPmappingList)
        {
            nextMappingDsc = genIPmappingList;
            lastLine       = jitGetILoffs(nextMappingDsc->ipmdILoffsx);

            unsigned firstLine = jitGetILoffs(nextMappingDsc->ipmdILoffsx);

            unsigned earlierLine = (firstLine < 5) ? 0 : firstLine - 5;

            compDspSrcLinesByLineNum(earlierLine, true); // display previous 5 lines
            compDspSrcLinesByLineNum(firstLine, false);
        }
        else
        {
            nextMappingDsc = nullptr;
        }

        return;
    }

    if (nextMappingDsc)
    {
        UNATIVE_OFFSET offset = nextMappingDsc->ipmdNativeLoc.CodeOffset(genEmitter);

        if (offset <= curIP)
        {
            IL_OFFSET nextOffs = jitGetILoffs(nextMappingDsc->ipmdILoffsx);

            if (lastLine < nextOffs)
            {
                compDspSrcLinesByLineNum(nextOffs);
            }
            else
            {
                // This offset corresponds to a previous line. Rewind to that line

                compDspSrcLinesByLineNum(nextOffs - 2, true);
                compDspSrcLinesByLineNum(nextOffs);
            }

            lastLine       = nextOffs;
            nextMappingDsc = nextMappingDsc->ipmdNext;
        }
    }
}

/*****************************************************************************/
#endif // DEBUG

/*****************************************************************************/
#if defined(DEBUG) || MEASURE_NODE_SIZE || MEASURE_BLOCK_SIZE || DISPLAY_SIZES || CALL_ARG_STATS

static unsigned genMethodCnt;  // total number of methods JIT'ted
unsigned        genMethodICnt; // number of interruptible methods
unsigned        genMethodNCnt; // number of non-interruptible methods
static unsigned genSmallMethodsNeedingExtraMemoryCnt = 0;

#endif

/*****************************************************************************/
#if MEASURE_NODE_SIZE
NodeSizeStats genNodeSizeStats;
NodeSizeStats genNodeSizeStatsPerFunc;

unsigned  genTreeNcntHistBuckets[] = {10, 20, 30, 40, 50, 100, 200, 300, 400, 500, 1000, 5000, 10000, 0};
Histogram genTreeNcntHist(genTreeNcntHistBuckets);

unsigned  genTreeNsizHistBuckets[] = {1000, 5000, 10000, 50000, 100000, 500000, 1000000, 0};
Histogram genTreeNsizHist(genTreeNsizHistBuckets);
#endif // MEASURE_NODE_SIZE

/*****************************************************************************/
#if MEASURE_MEM_ALLOC

unsigned  memAllocHistBuckets[] = {64, 128, 192, 256, 512, 1024, 4096, 8192, 0};
Histogram memAllocHist(memAllocHistBuckets);
unsigned  memUsedHistBuckets[] = {16, 32, 64, 128, 192, 256, 512, 1024, 4096, 8192, 0};
Histogram memUsedHist(memUsedHistBuckets);

#endif // MEASURE_MEM_ALLOC

/*****************************************************************************
 *
 *  Variables to keep track of total code amounts.
 */

#if DISPLAY_SIZES

size_t grossVMsize; // Total IL code size
size_t grossNCsize; // Native code + data size
size_t totalNCsize; // Native code + data + GC info size (TODO-Cleanup: GC info size only accurate for JIT32_GCENCODER)
size_t gcHeaderISize; // GC header      size: interruptible methods
size_t gcPtrMapISize; // GC pointer map size: interruptible methods
size_t gcHeaderNSize; // GC header      size: non-interruptible methods
size_t gcPtrMapNSize; // GC pointer map size: non-interruptible methods

#endif // DISPLAY_SIZES

/*****************************************************************************
 *
 *  Variables to keep track of argument counts.
 */

#if CALL_ARG_STATS

unsigned argTotalCalls;
unsigned argHelperCalls;
unsigned argStaticCalls;
unsigned argNonVirtualCalls;
unsigned argVirtualCalls;

unsigned argTotalArgs; // total number of args for all calls (including objectPtr)
unsigned argTotalDWordArgs;
unsigned argTotalLongArgs;
unsigned argTotalFloatArgs;
unsigned argTotalDoubleArgs;

unsigned argTotalRegArgs;
unsigned argTotalTemps;
unsigned argTotalLclVar;
unsigned argTotalDeferred;
unsigned argTotalConst;

unsigned argTotalObjPtr;
unsigned argTotalGTF_ASGinArgs;

unsigned argMaxTempsPerMethod;

unsigned  argCntBuckets[] = {0, 1, 2, 3, 4, 5, 6, 10, 0};
Histogram argCntTable(argCntBuckets);

unsigned  argDWordCntBuckets[] = {0, 1, 2, 3, 4, 5, 6, 10, 0};
Histogram argDWordCntTable(argDWordCntBuckets);

unsigned  argDWordLngCntBuckets[] = {0, 1, 2, 3, 4, 5, 6, 10, 0};
Histogram argDWordLngCntTable(argDWordLngCntBuckets);

unsigned  argTempsCntBuckets[] = {0, 1, 2, 3, 4, 5, 6, 10, 0};
Histogram argTempsCntTable(argTempsCntBuckets);

#endif // CALL_ARG_STATS

/*****************************************************************************
 *
 *  Variables to keep track of basic block counts.
 */

#if COUNT_BASIC_BLOCKS

//          --------------------------------------------------
//          Basic block count frequency table:
//          --------------------------------------------------
//              <=         1 ===>  26872 count ( 56% of total)
//               2 ..      2 ===>    669 count ( 58% of total)
//               3 ..      3 ===>   4687 count ( 68% of total)
//               4 ..      5 ===>   5101 count ( 78% of total)
//               6 ..     10 ===>   5575 count ( 90% of total)
//              11 ..     20 ===>   3028 count ( 97% of total)
//              21 ..     50 ===>   1108 count ( 99% of total)
//              51 ..    100 ===>    182 count ( 99% of total)
//             101 ..   1000 ===>     34 count (100% of total)
//            1001 ..  10000 ===>      0 count (100% of total)
//          --------------------------------------------------

unsigned  bbCntBuckets[] = {1, 2, 3, 5, 10, 20, 50, 100, 1000, 10000, 0};
Histogram bbCntTable(bbCntBuckets);

/* Histogram for the IL opcode size of methods with a single basic block */

unsigned  bbSizeBuckets[] = {1, 4, 8, 16, 32, 64, 128, 256, 512, 1024, 2048, 0};
Histogram bbOneBBSizeTable(bbSizeBuckets);

#endif // COUNT_BASIC_BLOCKS

/*****************************************************************************
 *
 *  Used by optFindNaturalLoops to gather statistical information such as
 *   - total number of natural loops
 *   - number of loops with 1, 2, ... exit conditions
 *   - number of loops that have an iterator (for like)
 *   - number of loops that have a constant iterator
 */

#if COUNT_LOOPS

unsigned totalLoopMethods;        // counts the total number of methods that have natural loops
unsigned maxLoopsPerMethod;       // counts the maximum number of loops a method has
unsigned totalLoopOverflows;      // # of methods that identified more loops than we can represent
unsigned totalLoopCount;          // counts the total number of natural loops
unsigned totalUnnatLoopCount;     // counts the total number of (not-necessarily natural) loops
unsigned totalUnnatLoopOverflows; // # of methods that identified more unnatural loops than we can represent
unsigned iterLoopCount;           // counts the # of loops with an iterator (for like)
unsigned simpleTestLoopCount;     // counts the # of loops with an iterator and a simple loop condition (iter < const)
unsigned constIterLoopCount;      // counts the # of loops with a constant iterator (for like)
bool     hasMethodLoops;          // flag to keep track if we already counted a method as having loops
unsigned loopsThisMethod;         // counts the number of loops in the current method
bool     loopOverflowThisMethod;  // True if we exceeded the max # of loops in the method.

/* Histogram for number of loops in a method */

unsigned  loopCountBuckets[] = {0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 0};
Histogram loopCountTable(loopCountBuckets);

/* Histogram for number of loop exits */

unsigned  loopExitCountBuckets[] = {0, 1, 2, 3, 4, 5, 6, 0};
Histogram loopExitCountTable(loopExitCountBuckets);

#endif // COUNT_LOOPS

//------------------------------------------------------------------------
// getJitGCType: Given the VM's CorInfoGCType convert it to the JIT's var_types
//
// Arguments:
//    gcType    - an enum value that originally came from an element
//                of the BYTE[] returned from getClassGClayout()
//
// Return Value:
//    The corresponsing enum value from the JIT's var_types
//
// Notes:
//   The gcLayout of each field of a struct is returned from getClassGClayout()
//   as a BYTE[] but each BYTE element is actually a CorInfoGCType value
//   Note when we 'know' that there is only one element in theis array
//   the JIT will often pass the address of a single BYTE, instead of a BYTE[]
//

var_types Compiler::getJitGCType(BYTE gcType)
{
    var_types     result      = TYP_UNKNOWN;
    CorInfoGCType corInfoType = (CorInfoGCType)gcType;

    if (corInfoType == TYPE_GC_NONE)
    {
        result = TYP_I_IMPL;
    }
    else if (corInfoType == TYPE_GC_REF)
    {
        result = TYP_REF;
    }
    else if (corInfoType == TYPE_GC_BYREF)
    {
        result = TYP_BYREF;
    }
    else
    {
        noway_assert(!"Bad value of 'gcType'");
    }
    return result;
}

#if FEATURE_MULTIREG_ARGS
//---------------------------------------------------------------------------
// getStructGcPtrsFromOp: Given a GenTree node of TYP_STRUCT that represents
//                        a pass by value argument, return the gcPtr layout
//                        for the pointers sized fields
// Arguments:
//    op         - the operand of TYP_STRUCT that is passed by value
//    gcPtrsOut  - an array of BYTES that are written by this method
//                 they will contain the VM's CorInfoGCType values
//                 for each pointer sized field
// Return Value:
//     Two [or more] values are written into the gcPtrs array
//
// Note that for ARM64 there will always be exactly two pointer sized fields

void Compiler::getStructGcPtrsFromOp(GenTree* op, BYTE* gcPtrsOut)
{
    assert(op->TypeGet() == TYP_STRUCT);

#ifdef _TARGET_ARM64_
    if (op->OperGet() == GT_OBJ)
    {
        CORINFO_CLASS_HANDLE objClass = op->gtObj.gtClass;

        int structSize = info.compCompHnd->getClassSize(objClass);
        assert(structSize <= 2 * TARGET_POINTER_SIZE);

        BYTE gcPtrsTmp[2] = {TYPE_GC_NONE, TYPE_GC_NONE};

        info.compCompHnd->getClassGClayout(objClass, &gcPtrsTmp[0]);

        gcPtrsOut[0] = gcPtrsTmp[0];
        gcPtrsOut[1] = gcPtrsTmp[1];
    }
    else if (op->OperGet() == GT_LCL_VAR)
    {
        GenTreeLclVarCommon* varNode = op->AsLclVarCommon();
        unsigned             varNum  = varNode->gtLclNum;
        assert(varNum < lvaCount);
        LclVarDsc* varDsc = &lvaTable[varNum];

        // At this point any TYP_STRUCT LclVar must be a 16-byte pass by value argument
        assert(varDsc->lvSize() == 2 * TARGET_POINTER_SIZE);

        gcPtrsOut[0] = varDsc->lvGcLayout[0];
        gcPtrsOut[1] = varDsc->lvGcLayout[1];
    }
    else
#endif
    {
        noway_assert(!"Unsupported Oper for getStructGcPtrsFromOp");
    }
}
#endif // FEATURE_MULTIREG_ARGS

#ifdef ARM_SOFTFP
//---------------------------------------------------------------------------
// IsSingleFloat32Struct:
//    Check if the given struct type contains only one float32 value type
//
// Arguments:
//    clsHnd     - the handle for the struct type
//
// Return Value:
//    true if the given struct type contains only one float32 value type,
//    false otherwise.
//

bool Compiler::isSingleFloat32Struct(CORINFO_CLASS_HANDLE clsHnd)
{
    for (;;)
    {
        // all of class chain must be of value type and must have only one field
        if (!info.compCompHnd->isValueClass(clsHnd) || info.compCompHnd->getClassNumInstanceFields(clsHnd) != 1)
        {
            return false;
        }

        CORINFO_CLASS_HANDLE* pClsHnd   = &clsHnd;
        CORINFO_FIELD_HANDLE  fldHnd    = info.compCompHnd->getFieldInClass(clsHnd, 0);
        CorInfoType           fieldType = info.compCompHnd->getFieldType(fldHnd, pClsHnd);

        switch (fieldType)
        {
            case CORINFO_TYPE_VALUECLASS:
                clsHnd = *pClsHnd;
                break;

            case CORINFO_TYPE_FLOAT:
                return true;

            default:
                return false;
        }
    }
}
#endif // ARM_SOFTFP

//-----------------------------------------------------------------------------
// getPrimitiveTypeForStruct:
//     Get the "primitive" type that is is used for a struct
//     of size 'structSize'.
//     We examine 'clsHnd' to check the GC layout of the struct and
//     return TYP_REF for structs that simply wrap an object.
//     If the struct is a one element HFA/HVA, we will return the
//     proper floating point or vector type.
//
// Arguments:
//    structSize - the size of the struct type, cannot be zero
//    clsHnd     - the handle for the struct type, used when may have
//                 an HFA or if we need the GC layout for an object ref.
//
// Return Value:
//    The primitive type (i.e. byte, short, int, long, ref, float, double)
//    used to pass or return structs of this size.
//    If we shouldn't use a "primitive" type then TYP_UNKNOWN is returned.
// Notes:
//    For 32-bit targets (X86/ARM32) the 64-bit TYP_LONG type is not
//    considered a primitive type by this method.
//    So a struct that wraps a 'long' is passed and returned in the
//    same way as any other 8-byte struct
//    For ARM32 if we have an HFA struct that wraps a 64-bit double
//    we will return TYP_DOUBLE.
//    For vector calling conventions, a vector is considered a "primitive"
//    type, as it is passed in a single register.
//
var_types Compiler::getPrimitiveTypeForStruct(unsigned structSize, CORINFO_CLASS_HANDLE clsHnd, bool isVarArg)
{
    assert(structSize != 0);

    var_types useType = TYP_UNKNOWN;

// Start by determining if we have an HFA/HVA with a single element.
#ifdef FEATURE_HFA
#if defined(_TARGET_WINDOWS_) && defined(_TARGET_ARM64_)
    // Arm64 Windows VarArg methods arguments will not classify HFA types, they will need to be treated
    // as if they are not HFA types.
    if (!isVarArg)
#endif // defined(_TARGET_WINDOWS_) && defined(_TARGET_ARM64_)
    {
        switch (structSize)
        {
            case 4:
            case 8:
#ifdef _TARGET_ARM64_
            case 16:
#endif // _TARGET_ARM64_
            {
                var_types hfaType;
#ifdef ARM_SOFTFP
                // For ARM_SOFTFP, HFA is unsupported so we need to check in another way.
                // This matters only for size-4 struct because bigger structs would be processed with RetBuf.
                if (isSingleFloat32Struct(clsHnd))
                {
                    hfaType = TYP_FLOAT;
                }
#else  // !ARM_SOFTFP
                hfaType = GetHfaType(clsHnd);
#endif // ARM_SOFTFP
                // We're only interested in the case where the struct size is equal to the size of the hfaType.
                if (varTypeIsValidHfaType(hfaType))
                {
                    if (genTypeSize(hfaType) == structSize)
                    {
                        useType = hfaType;
                    }
                    else
                    {
                        return TYP_UNKNOWN;
                    }
                }
            }
        }
        if (useType != TYP_UNKNOWN)
        {
            return useType;
        }
    }
#endif // FEATURE_HFA

    // Now deal with non-HFA/HVA structs.
    switch (structSize)
    {
        case 1:
            useType = TYP_BYTE;
            break;

        case 2:
            useType = TYP_SHORT;
            break;

#if !defined(_TARGET_XARCH_) || defined(UNIX_AMD64_ABI)
        case 3:
            useType = TYP_INT;
            break;

#endif // !_TARGET_XARCH_ || UNIX_AMD64_ABI

#ifdef _TARGET_64BIT_
        case 4:
            // We dealt with the one-float HFA above. All other 4-byte structs are handled as INT.
            useType = TYP_INT;
            break;

#if !defined(_TARGET_XARCH_) || defined(UNIX_AMD64_ABI)
        case 5:
        case 6:
        case 7:
            useType = TYP_I_IMPL;
            break;

#endif // !_TARGET_XARCH_ || UNIX_AMD64_ABI
#endif // _TARGET_64BIT_

        case TARGET_POINTER_SIZE:
        {
            BYTE gcPtr = 0;
            // Check if this pointer-sized struct is wrapping a GC object
            info.compCompHnd->getClassGClayout(clsHnd, &gcPtr);
            useType = getJitGCType(gcPtr);
        }
        break;

        default:
            useType = TYP_UNKNOWN;
            break;
    }

    return useType;
}

//-----------------------------------------------------------------------------
// getArgTypeForStruct:
//     Get the type that is used to pass values of the given struct type.
//     If you have already retrieved the struct size then it should be
//     passed as the optional fourth argument, as this allows us to avoid
//     an extra call to getClassSize(clsHnd)
//
// Arguments:
//    clsHnd       - the handle for the struct type
//    wbPassStruct - An "out" argument with information about how
//                   the struct is to be passed
//    isVarArg     - is vararg, used to ignore HFA types for Arm64 windows varargs
//    structSize   - the size of the struct type,
//                   or zero if we should call getClassSize(clsHnd)
//
// Return Value:
//    For wbPassStruct you can pass a 'nullptr' and nothing will be written
//     or returned for that out parameter.
//    When *wbPassStruct is SPK_PrimitiveType this method's return value
//       is the primitive type used to pass the struct.
//    When *wbPassStruct is SPK_ByReference this method's return value
//       is always TYP_UNKNOWN and the struct type is passed by reference to a copy
//    When *wbPassStruct is SPK_ByValue or SPK_ByValueAsHfa this method's return value
//       is always TYP_STRUCT and the struct type is passed by value either
//       using multiple registers or on the stack.
//
// Assumptions:
//    The size must be the size of the given type.
//    The given class handle must be for a value type (struct).
//
// Notes:
//    About HFA types:
//        When the clsHnd is a one element HFA type we return the appropriate
//         floating point primitive type and *wbPassStruct is SPK_PrimitiveType
//        If there are two or more elements in the HFA type then the this method's
//         return value is TYP_STRUCT and *wbPassStruct is SPK_ByValueAsHfa
//
var_types Compiler::getArgTypeForStruct(CORINFO_CLASS_HANDLE clsHnd,
                                        structPassingKind*   wbPassStruct,
                                        bool                 isVarArg,
                                        unsigned             structSize)
{
    var_types         useType         = TYP_UNKNOWN;
    structPassingKind howToPassStruct = SPK_Unknown; // We must change this before we return

    assert(structSize != 0);

// Determine if we can pass the struct as a primitive type.
// Note that on x86 we never pass structs as primitive types (unless the VM unwraps them for us).
#ifndef _TARGET_X86_
#ifdef UNIX_AMD64_ABI

    // An 8-byte struct may need to be passed in a floating point register
    // So we always consult the struct "Classifier" routine
    //
    SYSTEMV_AMD64_CORINFO_STRUCT_REG_PASSING_DESCRIPTOR structDesc;
    eeGetSystemVAmd64PassStructInRegisterDescriptor(clsHnd, &structDesc);

    if (structDesc.passedInRegisters && (structDesc.eightByteCount != 1))
    {
        // We can't pass this as a primitive type.
    }
    else if (structDesc.eightByteClassifications[0] == SystemVClassificationTypeSSE)
    {
        // If this is passed as a floating type, use that.
        // Otherwise, we'll use the general case - we don't want to use the "EightByteType"
        // directly, because it returns `TYP_INT` for any integral type <= 4 bytes, and
        // we need to preserve small types.
        useType = GetEightByteType(structDesc, 0);
    }
    else
#endif // UNIX_AMD64_ABI

        // The largest arg passed in a single register is MAX_PASS_SINGLEREG_BYTES,
        // so we can skip calling getPrimitiveTypeForStruct when we
        // have a struct that is larger than that.
        //
        if (structSize <= MAX_PASS_SINGLEREG_BYTES)
    {
        // We set the "primitive" useType based upon the structSize
        // and also examine the clsHnd to see if it is an HFA of count one
        useType = getPrimitiveTypeForStruct(structSize, clsHnd, isVarArg);
    }

#endif // !_TARGET_X86_

    // Did we change this struct type into a simple "primitive" type?
    //
    if (useType != TYP_UNKNOWN)
    {
        // Yes, we should use the "primitive" type in 'useType'
        howToPassStruct = SPK_PrimitiveType;
    }
    else // We can't replace the struct with a "primitive" type
    {
        // See if we can pass this struct by value, possibly in multiple registers
        // or if we should pass it by reference to a copy
        //
        if (structSize <= MAX_PASS_MULTIREG_BYTES)
        {
            // Structs that are HFA/HVA's are passed by value in multiple registers.
            // Arm64 Windows VarArg methods arguments will not classify HFA/HVA types, they will need to be treated
            // as if they are not HFA/HVA types.
            var_types hfaType;
#if defined(_TARGET_WINDOWS_) && defined(_TARGET_ARM64_)
            if (isVarArg)
            {
                hfaType = TYP_UNDEF;
            }
            else
#endif // defined(_TARGET_WINDOWS_) && defined(_TARGET_ARM64_)
            {
                hfaType = GetHfaType(clsHnd);
            }
            if (varTypeIsValidHfaType(hfaType))
            {
                // HFA's of count one should have been handled by getPrimitiveTypeForStruct
                assert(GetHfaCount(clsHnd) >= 2);

                // setup wbPassType and useType indicate that this is passed by value as an HFA
                //  using multiple registers
                //  (when all of the parameters registers are used, then the stack will be used)
                howToPassStruct = SPK_ByValueAsHfa;
                useType         = TYP_STRUCT;
            }
            else // Not an HFA struct type
            {

#ifdef UNIX_AMD64_ABI
                // The case of (structDesc.eightByteCount == 1) should have already been handled
                if ((structDesc.eightByteCount > 1) || !structDesc.passedInRegisters)
                {
                    // setup wbPassType and useType indicate that this is passed by value in multiple registers
                    //  (when all of the parameters registers are used, then the stack will be used)
                    howToPassStruct = SPK_ByValue;
                    useType         = TYP_STRUCT;
                }
                else
                {
                    assert(structDesc.eightByteCount == 0);
                    // Otherwise we pass this struct by reference to a copy
                    // setup wbPassType and useType indicate that this is passed using one register
                    //  (by reference to a copy)
                    howToPassStruct = SPK_ByReference;
                    useType         = TYP_UNKNOWN;
                }

#elif defined(_TARGET_ARM64_)

                // Structs that are pointer sized or smaller should have been handled by getPrimitiveTypeForStruct
                assert(structSize > TARGET_POINTER_SIZE);

                // On ARM64 structs that are 9-16 bytes are passed by value in multiple registers
                //
                if (structSize <= (TARGET_POINTER_SIZE * 2))
                {
                    // setup wbPassType and useType indicate that this is passed by value in multiple registers
                    //  (when all of the parameters registers are used, then the stack will be used)
                    howToPassStruct = SPK_ByValue;
                    useType         = TYP_STRUCT;
                }
                else // a structSize that is 17-32 bytes in size
                {
                    // Otherwise we pass this struct by reference to a copy
                    // setup wbPassType and useType indicate that this is passed using one register
                    //  (by reference to a copy)
                    howToPassStruct = SPK_ByReference;
                    useType         = TYP_UNKNOWN;
                }

#elif defined(_TARGET_X86_) || defined(_TARGET_ARM_)

                // Otherwise we pass this struct by value on the stack
                // setup wbPassType and useType indicate that this is passed by value according to the X86/ARM32 ABI
                howToPassStruct = SPK_ByValue;
                useType         = TYP_STRUCT;

#else //  _TARGET_XXX_

                noway_assert(!"Unhandled TARGET in getArgTypeForStruct (with FEATURE_MULTIREG_ARGS=1)");

#endif //  _TARGET_XXX_
            }
        }
        else // (structSize > MAX_PASS_MULTIREG_BYTES)
        {
            // We have a (large) struct that can't be replaced with a "primitive" type
            // and can't be passed in multiple registers
            CLANG_FORMAT_COMMENT_ANCHOR;

#if defined(_TARGET_X86_) || defined(_TARGET_ARM_) || defined(UNIX_AMD64_ABI)

            // Otherwise we pass this struct by value on the stack
            // setup wbPassType and useType indicate that this is passed by value according to the X86/ARM32 ABI
            howToPassStruct = SPK_ByValue;
            useType         = TYP_STRUCT;

#elif defined(_TARGET_AMD64_) || defined(_TARGET_ARM64_)

            // Otherwise we pass this struct by reference to a copy
            // setup wbPassType and useType indicate that this is passed using one register (by reference to a copy)
            howToPassStruct = SPK_ByReference;
            useType         = TYP_UNKNOWN;

#else //  _TARGET_XXX_

            noway_assert(!"Unhandled TARGET in getArgTypeForStruct");

#endif //  _TARGET_XXX_
        }
    }

    // 'howToPassStruct' must be set to one of the valid values before we return
    assert(howToPassStruct != SPK_Unknown);
    if (wbPassStruct != nullptr)
    {
        *wbPassStruct = howToPassStruct;
    }

    return useType;
}

//-----------------------------------------------------------------------------
// getReturnTypeForStruct:
//     Get the type that is used to return values of the given struct type.
//     If you have already retrieved the struct size then it should be
//     passed as the optional third argument, as this allows us to avoid
//     an extra call to getClassSize(clsHnd)
//
// Arguments:
//    clsHnd         - the handle for the struct type
//    wbReturnStruct - An "out" argument with information about how
//                     the struct is to be returned
//    structSize     - the size of the struct type,
//                     or zero if we should call getClassSize(clsHnd)
//
// Return Value:
//    For wbReturnStruct you can pass a 'nullptr' and nothing will be written
//     or returned for that out parameter.
//    When *wbReturnStruct is SPK_PrimitiveType this method's return value
//       is the primitive type used to return the struct.
//    When *wbReturnStruct is SPK_ByReference this method's return value
//       is always TYP_UNKNOWN and the struct type is returned using a return buffer
//    When *wbReturnStruct is SPK_ByValue or SPK_ByValueAsHfa this method's return value
//       is always TYP_STRUCT and the struct type is returned using multiple registers.
//
// Assumptions:
//    The size must be the size of the given type.
//    The given class handle must be for a value type (struct).
//
// Notes:
//    About HFA types:
//        When the clsHnd is a one element HFA type then this method's return
//          value is the appropriate floating point primitive type and
//          *wbReturnStruct is SPK_PrimitiveType.
//        If there are two or more elements in the HFA type and the target supports
//          multireg return types then the return value is TYP_STRUCT and
//          *wbReturnStruct is SPK_ByValueAsHfa.
//        Additionally if there are two or more elements in the HFA type and
//          the target doesn't support multreg return types then it is treated
//          as if it wasn't an HFA type.
//    About returning TYP_STRUCT:
//        Whenever this method's return value is TYP_STRUCT it always means
//         that multiple registers are used to return this struct.
//
var_types Compiler::getReturnTypeForStruct(CORINFO_CLASS_HANDLE clsHnd,
                                           structPassingKind*   wbReturnStruct /* = nullptr */,
                                           unsigned             structSize /* = 0 */)
{
    var_types         useType             = TYP_UNKNOWN;
    structPassingKind howToReturnStruct   = SPK_Unknown; // We must change this before we return
    bool              canReturnInRegister = true;

    assert(clsHnd != NO_CLASS_HANDLE);

    if (structSize == 0)
    {
        structSize = info.compCompHnd->getClassSize(clsHnd);
    }
    assert(structSize > 0);

#ifdef UNIX_AMD64_ABI
    // An 8-byte struct may need to be returned in a floating point register
    // So we always consult the struct "Classifier" routine
    //
    SYSTEMV_AMD64_CORINFO_STRUCT_REG_PASSING_DESCRIPTOR structDesc;
    eeGetSystemVAmd64PassStructInRegisterDescriptor(clsHnd, &structDesc);

    if (structDesc.eightByteCount == 1)
    {
        assert(structSize <= sizeof(double));
        assert(structDesc.passedInRegisters);

        if (structDesc.eightByteClassifications[0] == SystemVClassificationTypeSSE)
        {
            // If this is returned as a floating type, use that.
            // Otherwise, leave as TYP_UNKONWN and we'll sort things out below.
            useType           = GetEightByteType(structDesc, 0);
            howToReturnStruct = SPK_PrimitiveType;
        }
    }
    else
    {
        // Return classification is not always size based...
        canReturnInRegister = structDesc.passedInRegisters;
    }

#endif // UNIX_AMD64_ABI

    // Check for cases where a small struct is returned in a register
    // via a primitive type.
    //
    // The largest "primitive type" is MAX_PASS_SINGLEREG_BYTES
    // so we can skip calling getPrimitiveTypeForStruct when we
    // have a struct that is larger than that.
    if (canReturnInRegister && (useType == TYP_UNKNOWN) && (structSize <= MAX_PASS_SINGLEREG_BYTES))
    {
        // We set the "primitive" useType based upon the structSize
        // and also examine the clsHnd to see if it is an HFA of count one
        //
        // The ABI for struct returns in varArg methods, is same as the normal case,
        // so pass false for isVararg
        useType = getPrimitiveTypeForStruct(structSize, clsHnd, /*isVararg=*/false);

        if (useType != TYP_UNKNOWN)
        {
            if (structSize == genTypeSize(useType))
            {
                // Currently: 1, 2, 4, or 8 byte structs
                howToReturnStruct = SPK_PrimitiveType;
            }
            else
            {
                // Currently: 3, 5, 6, or 7 byte structs
                assert(structSize < genTypeSize(useType));
                howToReturnStruct = SPK_EnclosingType;
            }
        }
    }

#ifdef _TARGET_64BIT_
    // Note this handles an odd case when FEATURE_MULTIREG_RET is disabled and HFAs are enabled
    //
    // getPrimitiveTypeForStruct will return TYP_UNKNOWN for a struct that is an HFA of two floats
    // because when HFA are enabled, normally we would use two FP registers to pass or return it
    //
    // But if we don't have support for multiple register return types, we have to change this.
    // Since what we have is an 8-byte struct (float + float)  we change useType to TYP_I_IMPL
    // so that the struct is returned instead using an 8-byte integer register.
    //
    if ((FEATURE_MULTIREG_RET == 0) && (useType == TYP_UNKNOWN) && (structSize == (2 * sizeof(float))) && IsHfa(clsHnd))
    {
        useType           = TYP_I_IMPL;
        howToReturnStruct = SPK_PrimitiveType;
    }
#endif

    // Did we change this struct type into a simple "primitive" type?
    if (useType != TYP_UNKNOWN)
    {
        // If so, we should have already set howToReturnStruct, too.
        assert(howToReturnStruct != SPK_Unknown);
    }
    else // We can't replace the struct with a "primitive" type
    {
        // See if we can return this struct by value, possibly in multiple registers
        // or if we should return it using a return buffer register
        //
        if ((FEATURE_MULTIREG_RET == 1) && (structSize <= MAX_RET_MULTIREG_BYTES))
        {
            // Structs that are HFA's are returned in multiple registers
            if (IsHfa(clsHnd))
            {
                // HFA's of count one should have been handled by getPrimitiveTypeForStruct
                assert(GetHfaCount(clsHnd) >= 2);

                // setup wbPassType and useType indicate that this is returned by value as an HFA
                //  using multiple registers
                howToReturnStruct = SPK_ByValueAsHfa;
                useType           = TYP_STRUCT;
            }
            else // Not an HFA struct type
            {

#ifdef UNIX_AMD64_ABI

                // The case of (structDesc.eightByteCount == 1) should have already been handled
                if (structDesc.eightByteCount > 1)
                {
                    // setup wbPassType and useType indicate that this is returned by value in multiple registers
                    howToReturnStruct = SPK_ByValue;
                    useType           = TYP_STRUCT;
                    assert(structDesc.passedInRegisters == true);
                }
                else
                {
                    assert(structDesc.eightByteCount == 0);
                    // Otherwise we return this struct using a return buffer
                    // setup wbPassType and useType indicate that this is return using a return buffer register
                    //  (reference to a return buffer)
                    howToReturnStruct = SPK_ByReference;
                    useType           = TYP_UNKNOWN;
                    assert(structDesc.passedInRegisters == false);
                }

#elif defined(_TARGET_ARM64_)

                // Structs that are pointer sized or smaller should have been handled by getPrimitiveTypeForStruct
                assert(structSize > TARGET_POINTER_SIZE);

                // On ARM64 structs that are 9-16 bytes are returned by value in multiple registers
                //
                if (structSize <= (TARGET_POINTER_SIZE * 2))
                {
                    // setup wbPassType and useType indicate that this is return by value in multiple registers
                    howToReturnStruct = SPK_ByValue;
                    useType           = TYP_STRUCT;
                }
                else // a structSize that is 17-32 bytes in size
                {
                    // Otherwise we return this struct using a return buffer
                    // setup wbPassType and useType indicate that this is returned using a return buffer register
                    //  (reference to a return buffer)
                    howToReturnStruct = SPK_ByReference;
                    useType           = TYP_UNKNOWN;
                }

#elif defined(_TARGET_ARM_) || defined(_TARGET_X86_)

                // Otherwise we return this struct using a return buffer
                // setup wbPassType and useType indicate that this is returned using a return buffer register
                //  (reference to a return buffer)
                howToReturnStruct = SPK_ByReference;
                useType           = TYP_UNKNOWN;

#else //  _TARGET_XXX_

                noway_assert(!"Unhandled TARGET in getReturnTypeForStruct (with FEATURE_MULTIREG_ARGS=1)");

#endif //  _TARGET_XXX_
            }
        }
        else // (structSize > MAX_RET_MULTIREG_BYTES) || (FEATURE_MULTIREG_RET == 0)
        {
            // We have a (large) struct that can't be replaced with a "primitive" type
            // and can't be returned in multiple registers

            // We return this struct using a return buffer register
            // setup wbPassType and useType indicate that this is returned using a return buffer register
            //  (reference to a return buffer)
            howToReturnStruct = SPK_ByReference;
            useType           = TYP_UNKNOWN;
        }
    }

    // 'howToReturnStruct' must be set to one of the valid values before we return
    assert(howToReturnStruct != SPK_Unknown);
    if (wbReturnStruct != nullptr)
    {
        *wbReturnStruct = howToReturnStruct;
    }

    return useType;
}

///////////////////////////////////////////////////////////////////////////////
//
// MEASURE_NOWAY: code to measure and rank dynamic occurences of noway_assert.
// (Just the appearances of noway_assert, whether the assert is true or false.)
// This might help characterize the cost of noway_assert in non-DEBUG builds,
// or determine which noway_assert should be simple DEBUG-only asserts.
//
///////////////////////////////////////////////////////////////////////////////

#if MEASURE_NOWAY

struct FileLine
{
    char*    m_file;
    unsigned m_line;
    char*    m_condStr;

    FileLine() : m_file(nullptr), m_line(0), m_condStr(nullptr)
    {
    }

    FileLine(const char* file, unsigned line, const char* condStr) : m_line(line)
    {
        size_t newSize = (strlen(file) + 1) * sizeof(char);
        m_file         = HostAllocator::getHostAllocator().allocate<char>(newSize);
        strcpy_s(m_file, newSize, file);

        newSize   = (strlen(condStr) + 1) * sizeof(char);
        m_condStr = HostAllocator::getHostAllocator().allocate<char>(newSize);
        strcpy_s(m_condStr, newSize, condStr);
    }

    FileLine(const FileLine& other)
    {
        m_file    = other.m_file;
        m_line    = other.m_line;
        m_condStr = other.m_condStr;
    }

    // GetHashCode() and Equals() are needed by JitHashTable

    static unsigned GetHashCode(FileLine fl)
    {
        assert(fl.m_file != nullptr);
        unsigned code = fl.m_line;
        for (const char* p = fl.m_file; *p != '\0'; p++)
        {
            code += *p;
        }
        // Could also add condStr.
        return code;
    }

    static bool Equals(FileLine fl1, FileLine fl2)
    {
        return (fl1.m_line == fl2.m_line) && (0 == strcmp(fl1.m_file, fl2.m_file));
    }
};

typedef JitHashTable<FileLine, FileLine, size_t, HostAllocator> FileLineToCountMap;
FileLineToCountMap* NowayAssertMap;

void Compiler::RecordNowayAssert(const char* filename, unsigned line, const char* condStr)
{
    if (NowayAssertMap == nullptr)
    {
        NowayAssertMap = new (HostAllocator::getHostAllocator()) FileLineToCountMap(HostAllocator::getHostAllocator());
    }
    FileLine fl(filename, line, condStr);
    size_t*  pCount = NowayAssertMap->LookupPointer(fl);
    if (pCount == nullptr)
    {
        NowayAssertMap->Set(fl, 1);
    }
    else
    {
        ++(*pCount);
    }
}

void RecordNowayAssertGlobal(const char* filename, unsigned line, const char* condStr)
{
    if ((JitConfig.JitMeasureNowayAssert() == 1) && (JitTls::GetCompiler() != nullptr))
    {
        JitTls::GetCompiler()->RecordNowayAssert(filename, line, condStr);
    }
}

struct NowayAssertCountMap
{
    size_t   count;
    FileLine fl;

    NowayAssertCountMap() : count(0)
    {
    }

    static int __cdecl compare(const void* elem1, const void* elem2)
    {
        NowayAssertCountMap* e1 = (NowayAssertCountMap*)elem1;
        NowayAssertCountMap* e2 = (NowayAssertCountMap*)elem2;
        return (int)((ssize_t)e2->count - (ssize_t)e1->count); // sort in descending order
    }
};

void DisplayNowayAssertMap()
{
    if (NowayAssertMap != nullptr)
    {
        FILE* fout;

        LPCWSTR strJitMeasureNowayAssertFile = JitConfig.JitMeasureNowayAssertFile();
        if (strJitMeasureNowayAssertFile != nullptr)
        {
            fout = _wfopen(strJitMeasureNowayAssertFile, W("a"));
            if (fout == nullptr)
            {
                fprintf(jitstdout, "Failed to open JitMeasureNowayAssertFile \"%ws\"\n", strJitMeasureNowayAssertFile);
                return;
            }
        }
        else
        {
            fout = jitstdout;
        }

        // Iterate noway assert map, create sorted table by occurrence, dump it.
        unsigned             count = NowayAssertMap->GetCount();
        NowayAssertCountMap* nacp  = new NowayAssertCountMap[count];
        unsigned             i     = 0;

        for (FileLineToCountMap::KeyIterator iter = NowayAssertMap->Begin(), end = NowayAssertMap->End();
             !iter.Equal(end); ++iter)
        {
            nacp[i].count = iter.GetValue();
            nacp[i].fl    = iter.Get();
            ++i;
        }

        qsort(nacp, count, sizeof(nacp[0]), NowayAssertCountMap::compare);

        if (fout == jitstdout)
        {
            // Don't output the header if writing to a file, since we'll be appending to existing dumps in that case.
            fprintf(fout, "\nnoway_assert counts:\n");
            fprintf(fout, "count, file, line, text\n");
        }

        for (i = 0; i < count; i++)
        {
            fprintf(fout, "%u, %s, %u, \"%s\"\n", nacp[i].count, nacp[i].fl.m_file, nacp[i].fl.m_line,
                    nacp[i].fl.m_condStr);
        }

        if (fout != jitstdout)
        {
            fclose(fout);
            fout = nullptr;
        }
    }
}

#endif // MEASURE_NOWAY

/*****************************************************************************
 * variables to keep track of how many iterations we go in a dataflow pass
 */

#if DATAFLOW_ITER

unsigned CSEiterCount; // counts the # of iteration for the CSE dataflow
unsigned CFiterCount;  // counts the # of iteration for the Const Folding dataflow

#endif // DATAFLOW_ITER

#if MEASURE_BLOCK_SIZE
size_t genFlowNodeSize;
size_t genFlowNodeCnt;
#endif // MEASURE_BLOCK_SIZE

/*****************************************************************************/
// We keep track of methods we've already compiled.

/*****************************************************************************
 *  Declare the statics
 */

#ifdef DEBUG
/* static */
unsigned Compiler::s_compMethodsCount = 0; // to produce unique label names
#endif

#if MEASURE_MEM_ALLOC
/* static */
bool Compiler::s_dspMemStats = false;
#endif

#ifndef PROFILING_SUPPORTED
const bool Compiler::Options::compNoPInvokeInlineCB = false;
#endif

/*****************************************************************************
 *
 *  One time initialization code
 */

/* static */
void Compiler::compStartup()
{
#if DISPLAY_SIZES
    grossVMsize = grossNCsize = totalNCsize = 0;
#endif // DISPLAY_SIZES

    /* Initialize the table of tree node sizes */

    GenTree::InitNodeSize();

#ifdef JIT32_GCENCODER
    // Initialize the GC encoder lookup table

    GCInfo::gcInitEncoderLookupTable();
#endif

    /* Initialize the emitter */

    emitter::emitInit();

    // Static vars of ValueNumStore
    ValueNumStore::InitValueNumStoreStatics();

    compDisplayStaticSizes(jitstdout);
}

/*****************************************************************************
 *
 *  One time finalization code
 */

/* static */
void Compiler::compShutdown()
{
#ifdef ALT_JIT
    if (s_pAltJitExcludeAssembliesList != nullptr)
    {
        s_pAltJitExcludeAssembliesList->~AssemblyNamesList2(); // call the destructor
        s_pAltJitExcludeAssembliesList = nullptr;
    }
#endif // ALT_JIT

#ifdef DEBUG
    if (s_pJitDisasmIncludeAssembliesList != nullptr)
    {
        s_pJitDisasmIncludeAssembliesList->~AssemblyNamesList2(); // call the destructor
        s_pJitDisasmIncludeAssembliesList = nullptr;
    }
#endif // DEBUG

#if MEASURE_NOWAY
    DisplayNowayAssertMap();
#endif // MEASURE_NOWAY

    /* Shut down the emitter */

    emitter::emitDone();

#if defined(DEBUG) || defined(INLINE_DATA)
    // Finish reading and/or writing inline xml
    InlineStrategy::FinalizeXml();
#endif // defined(DEBUG) || defined(INLINE_DATA)

#if defined(DEBUG) || MEASURE_NODE_SIZE || MEASURE_BLOCK_SIZE || DISPLAY_SIZES || CALL_ARG_STATS
    if (genMethodCnt == 0)
    {
        return;
    }
#endif

#if NODEBASH_STATS
    GenTree::ReportOperBashing(jitstdout);
#endif

    // Where should we write our statistics output?
    FILE* fout = jitstdout;

#ifdef FEATURE_JIT_METHOD_PERF
    if (compJitTimeLogFilename != nullptr)
    {
        FILE* jitTimeLogFile = _wfopen(compJitTimeLogFilename, W("a"));
        if (jitTimeLogFile != nullptr)
        {
            CompTimeSummaryInfo::s_compTimeSummary.Print(jitTimeLogFile);
            fclose(jitTimeLogFile);
        }
    }
#endif // FEATURE_JIT_METHOD_PERF

#if COUNT_AST_OPERS

    // Add up all the counts so that we can show percentages of total
    unsigned gtc = 0;
    for (unsigned op = 0; op < GT_COUNT; op++)
        gtc += GenTree::s_gtNodeCounts[op];

    if (gtc > 0)
    {
        unsigned rem_total = gtc;
        unsigned rem_large = 0;
        unsigned rem_small = 0;

        unsigned tot_large = 0;
        unsigned tot_small = 0;

        fprintf(fout, "\nGenTree operator counts (approximate):\n\n");

        for (unsigned op = 0; op < GT_COUNT; op++)
        {
            unsigned siz = GenTree::s_gtTrueSizes[op];
            unsigned cnt = GenTree::s_gtNodeCounts[op];
            double   pct = 100.0 * cnt / gtc;

            if (siz > TREE_NODE_SZ_SMALL)
                tot_large += cnt;
            else
                tot_small += cnt;

            // Let's not show anything below a threshold
            if (pct >= 0.5)
            {
                fprintf(fout, "    GT_%-17s   %7u (%4.1lf%%) %3u bytes each\n", GenTree::OpName((genTreeOps)op), cnt,
                        pct, siz);
                rem_total -= cnt;
            }
            else
            {
                if (siz > TREE_NODE_SZ_SMALL)
                    rem_large += cnt;
                else
                    rem_small += cnt;
            }
        }
        if (rem_total > 0)
        {
            fprintf(fout, "    All other GT_xxx ...   %7u (%4.1lf%%) ... %4.1lf%% small + %4.1lf%% large\n", rem_total,
                    100.0 * rem_total / gtc, 100.0 * rem_small / gtc, 100.0 * rem_large / gtc);
        }
        fprintf(fout, "    -----------------------------------------------------\n");
        fprintf(fout, "    Total    .......   %11u --ALL-- ... %4.1lf%% small + %4.1lf%% large\n", gtc,
                100.0 * tot_small / gtc, 100.0 * tot_large / gtc);
        fprintf(fout, "\n");
    }

#endif // COUNT_AST_OPERS

#if DISPLAY_SIZES

    if (grossVMsize && grossNCsize)
    {
        fprintf(fout, "\n");
        fprintf(fout, "--------------------------------------\n");
        fprintf(fout, "Function and GC info size stats\n");
        fprintf(fout, "--------------------------------------\n");

        fprintf(fout, "[%7u VM, %8u %6s %4u%%] %s\n", grossVMsize, grossNCsize, Target::g_tgtCPUName,
                100 * grossNCsize / grossVMsize, "Total (excluding GC info)");

        fprintf(fout, "[%7u VM, %8u %6s %4u%%] %s\n", grossVMsize, totalNCsize, Target::g_tgtCPUName,
                100 * totalNCsize / grossVMsize, "Total (including GC info)");

        if (gcHeaderISize || gcHeaderNSize)
        {
            fprintf(fout, "\n");

            fprintf(fout, "GC tables   : [%7uI,%7uN] %7u byt  (%u%% of IL, %u%% of %s).\n",
                    gcHeaderISize + gcPtrMapISize, gcHeaderNSize + gcPtrMapNSize, totalNCsize - grossNCsize,
                    100 * (totalNCsize - grossNCsize) / grossVMsize, 100 * (totalNCsize - grossNCsize) / grossNCsize,
                    Target::g_tgtCPUName);

            fprintf(fout, "GC headers  : [%7uI,%7uN] %7u byt, [%4.1fI,%4.1fN] %4.1f byt/meth\n", gcHeaderISize,
                    gcHeaderNSize, gcHeaderISize + gcHeaderNSize, (float)gcHeaderISize / (genMethodICnt + 0.001),
                    (float)gcHeaderNSize / (genMethodNCnt + 0.001),
                    (float)(gcHeaderISize + gcHeaderNSize) / genMethodCnt);

            fprintf(fout, "GC ptr maps : [%7uI,%7uN] %7u byt, [%4.1fI,%4.1fN] %4.1f byt/meth\n", gcPtrMapISize,
                    gcPtrMapNSize, gcPtrMapISize + gcPtrMapNSize, (float)gcPtrMapISize / (genMethodICnt + 0.001),
                    (float)gcPtrMapNSize / (genMethodNCnt + 0.001),
                    (float)(gcPtrMapISize + gcPtrMapNSize) / genMethodCnt);
        }
        else
        {
            fprintf(fout, "\n");

            fprintf(fout, "GC tables   take up %u bytes (%u%% of instr, %u%% of %6s code).\n",
                    totalNCsize - grossNCsize, 100 * (totalNCsize - grossNCsize) / grossVMsize,
                    100 * (totalNCsize - grossNCsize) / grossNCsize, Target::g_tgtCPUName);
        }

#ifdef DEBUG
#if DOUBLE_ALIGN
        fprintf(fout, "%u out of %u methods generated with double-aligned stack\n",
                Compiler::s_lvaDoubleAlignedProcsCount, genMethodCnt);
#endif
#endif
    }

#endif // DISPLAY_SIZES

#if CALL_ARG_STATS
    compDispCallArgStats(fout);
#endif

#if COUNT_BASIC_BLOCKS
    fprintf(fout, "--------------------------------------------------\n");
    fprintf(fout, "Basic block count frequency table:\n");
    fprintf(fout, "--------------------------------------------------\n");
    bbCntTable.dump(fout);
    fprintf(fout, "--------------------------------------------------\n");

    fprintf(fout, "\n");

    fprintf(fout, "--------------------------------------------------\n");
    fprintf(fout, "IL method size frequency table for methods with a single basic block:\n");
    fprintf(fout, "--------------------------------------------------\n");
    bbOneBBSizeTable.dump(fout);
    fprintf(fout, "--------------------------------------------------\n");
#endif // COUNT_BASIC_BLOCKS

#if COUNT_LOOPS

    fprintf(fout, "\n");
    fprintf(fout, "---------------------------------------------------\n");
    fprintf(fout, "Loop stats\n");
    fprintf(fout, "---------------------------------------------------\n");
    fprintf(fout, "Total number of methods with loops is %5u\n", totalLoopMethods);
    fprintf(fout, "Total number of              loops is %5u\n", totalLoopCount);
    fprintf(fout, "Maximum number of loops per method is %5u\n", maxLoopsPerMethod);
    fprintf(fout, "# of methods overflowing nat loop table is %5u\n", totalLoopOverflows);
    fprintf(fout, "Total number of 'unnatural' loops is %5u\n", totalUnnatLoopCount);
    fprintf(fout, "# of methods overflowing unnat loop limit is %5u\n", totalUnnatLoopOverflows);
    fprintf(fout, "Total number of loops with an         iterator is %5u\n", iterLoopCount);
    fprintf(fout, "Total number of loops with a simple   iterator is %5u\n", simpleTestLoopCount);
    fprintf(fout, "Total number of loops with a constant iterator is %5u\n", constIterLoopCount);

    fprintf(fout, "--------------------------------------------------\n");
    fprintf(fout, "Loop count frequency table:\n");
    fprintf(fout, "--------------------------------------------------\n");
    loopCountTable.dump(fout);
    fprintf(fout, "--------------------------------------------------\n");
    fprintf(fout, "Loop exit count frequency table:\n");
    fprintf(fout, "--------------------------------------------------\n");
    loopExitCountTable.dump(fout);
    fprintf(fout, "--------------------------------------------------\n");

#endif // COUNT_LOOPS

#if DATAFLOW_ITER

    fprintf(fout, "---------------------------------------------------\n");
    fprintf(fout, "Total number of iterations in the CSE dataflow loop is %5u\n", CSEiterCount);
    fprintf(fout, "Total number of iterations in the  CF dataflow loop is %5u\n", CFiterCount);

#endif // DATAFLOW_ITER

#if MEASURE_NODE_SIZE

    fprintf(fout, "\n");
    fprintf(fout, "---------------------------------------------------\n");
    fprintf(fout, "GenTree node allocation stats\n");
    fprintf(fout, "---------------------------------------------------\n");

    fprintf(fout, "Allocated %6I64u tree nodes (%7I64u bytes total, avg %4I64u bytes per method)\n",
            genNodeSizeStats.genTreeNodeCnt, genNodeSizeStats.genTreeNodeSize,
            genNodeSizeStats.genTreeNodeSize / genMethodCnt);

    fprintf(fout, "Allocated %7I64u bytes of unused tree node space (%3.2f%%)\n",
            genNodeSizeStats.genTreeNodeSize - genNodeSizeStats.genTreeNodeActualSize,
            (float)(100 * (genNodeSizeStats.genTreeNodeSize - genNodeSizeStats.genTreeNodeActualSize)) /
                genNodeSizeStats.genTreeNodeSize);

    fprintf(fout, "\n");
    fprintf(fout, "---------------------------------------------------\n");
    fprintf(fout, "Distribution of per-method GenTree node counts:\n");
    genTreeNcntHist.dump(fout);

    fprintf(fout, "\n");
    fprintf(fout, "---------------------------------------------------\n");
    fprintf(fout, "Distribution of per-method GenTree node  allocations (in bytes):\n");
    genTreeNsizHist.dump(fout);

#endif // MEASURE_NODE_SIZE

#if MEASURE_BLOCK_SIZE

    fprintf(fout, "\n");
    fprintf(fout, "---------------------------------------------------\n");
    fprintf(fout, "BasicBlock and flowList/BasicBlockList allocation stats\n");
    fprintf(fout, "---------------------------------------------------\n");

    fprintf(fout, "Allocated %6u basic blocks (%7u bytes total, avg %4u bytes per method)\n", BasicBlock::s_Count,
            BasicBlock::s_Size, BasicBlock::s_Size / genMethodCnt);
    fprintf(fout, "Allocated %6u flow nodes (%7u bytes total, avg %4u bytes per method)\n", genFlowNodeCnt,
            genFlowNodeSize, genFlowNodeSize / genMethodCnt);

#endif // MEASURE_BLOCK_SIZE

#if MEASURE_MEM_ALLOC

    if (s_dspMemStats)
    {
        fprintf(fout, "\nAll allocations:\n");
        ArenaAllocator::dumpAggregateMemStats(jitstdout);

        fprintf(fout, "\nLargest method:\n");
        ArenaAllocator::dumpMaxMemStats(jitstdout);

        fprintf(fout, "\n");
        fprintf(fout, "---------------------------------------------------\n");
        fprintf(fout, "Distribution of total memory allocated per method (in KB):\n");
        memAllocHist.dump(fout);

        fprintf(fout, "\n");
        fprintf(fout, "---------------------------------------------------\n");
        fprintf(fout, "Distribution of total memory used      per method (in KB):\n");
        memUsedHist.dump(fout);
    }

#endif // MEASURE_MEM_ALLOC

#if LOOP_HOIST_STATS
#ifdef DEBUG // Always display loop stats in retail
    if (JitConfig.DisplayLoopHoistStats() != 0)
#endif // DEBUG
    {
        PrintAggregateLoopHoistStats(jitstdout);
    }
#endif // LOOP_HOIST_STATS

#if MEASURE_PTRTAB_SIZE

    fprintf(fout, "\n");
    fprintf(fout, "---------------------------------------------------\n");
    fprintf(fout, "GC pointer table stats\n");
    fprintf(fout, "---------------------------------------------------\n");

    fprintf(fout, "Reg pointer descriptor size (internal): %8u (avg %4u per method)\n", GCInfo::s_gcRegPtrDscSize,
            GCInfo::s_gcRegPtrDscSize / genMethodCnt);

    fprintf(fout, "Total pointer table size: %8u (avg %4u per method)\n", GCInfo::s_gcTotalPtrTabSize,
            GCInfo::s_gcTotalPtrTabSize / genMethodCnt);

#endif // MEASURE_PTRTAB_SIZE

#if MEASURE_NODE_SIZE || MEASURE_BLOCK_SIZE || MEASURE_PTRTAB_SIZE || DISPLAY_SIZES

    if (genMethodCnt != 0)
    {
        fprintf(fout, "\n");
        fprintf(fout, "A total of %6u methods compiled", genMethodCnt);
#if DISPLAY_SIZES
        if (genMethodICnt || genMethodNCnt)
        {
            fprintf(fout, " (%u interruptible, %u non-interruptible)", genMethodICnt, genMethodNCnt);
        }
#endif // DISPLAY_SIZES
        fprintf(fout, ".\n");
    }

#endif // MEASURE_NODE_SIZE || MEASURE_BLOCK_SIZE || MEASURE_PTRTAB_SIZE || DISPLAY_SIZES

#if EMITTER_STATS
    emitterStats(fout);
#endif

#if MEASURE_FATAL
    fprintf(fout, "\n");
    fprintf(fout, "---------------------------------------------------\n");
    fprintf(fout, "Fatal errors stats\n");
    fprintf(fout, "---------------------------------------------------\n");
    fprintf(fout, "   badCode:             %u\n", fatal_badCode);
    fprintf(fout, "   noWay:               %u\n", fatal_noWay);
    fprintf(fout, "   NOMEM:               %u\n", fatal_NOMEM);
    fprintf(fout, "   noWayAssertBody:     %u\n", fatal_noWayAssertBody);
#ifdef DEBUG
    fprintf(fout, "   noWayAssertBodyArgs: %u\n", fatal_noWayAssertBodyArgs);
#endif // DEBUG
    fprintf(fout, "   NYI:                 %u\n", fatal_NYI);
#endif // MEASURE_FATAL
}

/*****************************************************************************
 *  Display static data structure sizes.
 */

/* static */
void Compiler::compDisplayStaticSizes(FILE* fout)
{
#if MEASURE_NODE_SIZE
    GenTree::DumpNodeSizes(fout);
#endif

    BasicBlock::DisplayStaticSizes(fout);

#if EMITTER_STATS
    emitterStaticStats(fout);
#endif
}

/*****************************************************************************
 *
 *  Constructor
 */

void Compiler::compInit(ArenaAllocator* pAlloc, InlineInfo* inlineInfo)
{
    assert(pAlloc);
    compArenaAllocator = pAlloc;

    // Inlinee Compile object will only be allocated when needed for the 1st time.
    InlineeCompiler = nullptr;

    // Set the inline info.
    impInlineInfo = inlineInfo;

    eeInfoInitialized = false;

    compDoAggressiveInlining = false;

    if (compIsForInlining())
    {
        m_inlineStrategy = nullptr;
        compInlineResult = inlineInfo->inlineResult;
    }
    else
    {
        m_inlineStrategy = new (this, CMK_Inlining) InlineStrategy(this);
        compInlineResult = nullptr;
    }

#ifdef FEATURE_TRACELOGGING
    // Make sure JIT telemetry is initialized as soon as allocations can be made
    // but no later than a point where noway_asserts can be thrown.
    //    1. JIT telemetry could allocate some objects internally.
    //    2. NowayAsserts are tracked through telemetry.
    //    Note: JIT telemetry could gather data when compiler is not fully initialized.
    //          So you have to initialize the compiler variables you use for telemetry.
    assert((unsigned)PHASE_PRE_IMPORT == 0);
    previousCompletedPhase = PHASE_PRE_IMPORT;
    info.compILCodeSize    = 0;
    info.compMethodHnd     = nullptr;
    compJitTelemetry.Initialize(this);
#endif

#ifdef DEBUG
    bRangeAllowStress = false;
#endif

    fgInit();
    lvaInit();

    if (!compIsForInlining())
    {
        codeGen = getCodeGenerator(this);
        optInit();
        hashBv::Init(this);

        compVarScopeMap = nullptr;

        // If this method were a real constructor for Compiler, these would
        // become method initializations.
        impPendingBlockMembers    = JitExpandArray<BYTE>(getAllocator());
        impSpillCliquePredMembers = JitExpandArray<BYTE>(getAllocator());
        impSpillCliqueSuccMembers = JitExpandArray<BYTE>(getAllocator());

        lvMemoryPerSsaData = SsaDefArray<SsaMemDef>();

        //
        // Initialize all the per-method statistics gathering data structures.
        //

        optLoopsCloned = 0;

#if LOOP_HOIST_STATS
        m_loopsConsidered             = 0;
        m_curLoopHasHoistedExpression = false;
        m_loopsWithHoistedExpressions = 0;
        m_totalHoistedExpressions     = 0;
#endif // LOOP_HOIST_STATS
#if MEASURE_NODE_SIZE
        genNodeSizeStatsPerFunc.Init();
#endif // MEASURE_NODE_SIZE
    }
    else
    {
        codeGen = nullptr;
    }

    compJmpOpUsed         = false;
    compLongUsed          = false;
    compTailCallUsed      = false;
    compLocallocUsed      = false;
    compLocallocOptimized = false;
    compQmarkRationalized = false;
    compQmarkUsed         = false;
    compFloatingPointUsed = false;
    compUnsafeCastUsed    = false;

    compNeedsGSSecurityCookie = false;
    compGSReorderStackLayout  = false;

    compGeneratingProlog = false;
    compGeneratingEpilog = false;

    compLSRADone       = false;
    compRationalIRForm = false;

#ifdef DEBUG
    compCodeGenDone        = false;
    compRegSetCheckLevel   = 0;
    opts.compMinOptsIsUsed = false;
#endif
    opts.compMinOptsIsSet = false;

    // Used by fgFindJumpTargets for inlining heuristics.
    opts.instrCount = 0;

    // Used to track when we should consider running EarlyProp
    optMethodFlags = 0;

#ifdef DEBUG
    m_nodeTestData      = nullptr;
    m_loopHoistCSEClass = FIRST_LOOP_HOIST_CSE_CLASS;
#endif
    m_switchDescMap      = nullptr;
    m_blockToEHPreds     = nullptr;
    m_fieldSeqStore      = nullptr;
    m_zeroOffsetFieldMap = nullptr;
    m_arrayInfoMap       = nullptr;
    m_refAnyClass        = nullptr;
    for (MemoryKind memoryKind : allMemoryKinds())
    {
        m_memorySsaMap[memoryKind] = nullptr;
    }

#ifdef DEBUG
    if (!compIsForInlining())
    {
        compDoComponentUnitTestsOnce();
    }
#endif // DEBUG

    vnStore               = nullptr;
    m_opAsgnVarDefSsaNums = nullptr;
    fgSsaPassesCompleted  = 0;
    fgVNPassesCompleted   = 0;

    // check that HelperCallProperties are initialized

    assert(s_helperCallProperties.IsPure(CORINFO_HELP_GETSHARED_GCSTATIC_BASE));
    assert(!s_helperCallProperties.IsPure(CORINFO_HELP_GETFIELDOBJ)); // quick sanity check

    // We start with the flow graph in tree-order
    fgOrder = FGOrderTree;

#ifdef FEATURE_SIMD
    m_simdHandleCache = nullptr;
#endif // FEATURE_SIMD

    compUsesThrowHelper = false;
}

/*****************************************************************************
 *
 *  Destructor
 */

void Compiler::compDone()
{
}

void* Compiler::compGetHelperFtn(CorInfoHelpFunc ftnNum,        /* IN  */
                                 void**          ppIndirection) /* OUT */
{
    void* addr;

    if (info.compMatchedVM)
    {
        addr = info.compCompHnd->getHelperFtn(ftnNum, ppIndirection);
    }
    else
    {
        // If we don't have a matched VM, we won't get valid results when asking for a helper function.
        addr = UlongToPtr(0xCA11CA11); // "callcall"
    }

    return addr;
}

unsigned Compiler::compGetTypeSize(CorInfoType cit, CORINFO_CLASS_HANDLE clsHnd)
{
    var_types sigType = genActualType(JITtype2varType(cit));
    unsigned  sigSize;
    sigSize = genTypeSize(sigType);
    if (cit == CORINFO_TYPE_VALUECLASS)
    {
        sigSize = info.compCompHnd->getClassSize(clsHnd);
    }
    else if (cit == CORINFO_TYPE_REFANY)
    {
        sigSize = 2 * TARGET_POINTER_SIZE;
    }
    return sigSize;
}

#ifdef DEBUG
static bool DidComponentUnitTests = false;

void Compiler::compDoComponentUnitTestsOnce()
{
    if (!JitConfig.RunComponentUnitTests())
    {
        return;
    }

    if (!DidComponentUnitTests)
    {
        DidComponentUnitTests = true;
        ValueNumStore::RunTests(this);
        BitSetSupport::TestSuite(getAllocatorDebugOnly());
    }
}

//------------------------------------------------------------------------
// compGetJitDefaultFill:
//
// Return Value:
//    An unsigned char value used to initizalize memory allocated by the JIT.
//    The default value is taken from COMPLUS_JitDefaultFill,  if is not set
//    the value will be 0xdd.  When JitStress is active a random value based
//    on the method hash is used.
//
// Notes:
//    Note that we can't use small values like zero, because we have some
//    asserts that can fire for such values.
//
// static
unsigned char Compiler::compGetJitDefaultFill(Compiler* comp)
{
    unsigned char defaultFill = (unsigned char)JitConfig.JitDefaultFill();

    if (comp != nullptr && comp->compStressCompile(STRESS_GENERIC_VARN, 50))
    {
        unsigned temp;
        temp = comp->info.compMethodHash();
        temp = (temp >> 16) ^ temp;
        temp = (temp >> 8) ^ temp;
        temp = temp & 0xff;
        // asserts like this: assert(!IsUninitialized(stkLvl));
        // mean that small values for defaultFill are problematic
        // so we make the value larger in that case.
        if (temp < 0x20)
        {
            temp |= 0x80;
        }

        // Make a misaligned pointer value to reduce probability of getting a valid value and firing
        // assert(!IsUninitialized(pointer)).
        temp |= 0x1;

        defaultFill = (unsigned char)temp;
    }

    return defaultFill;
}

#endif // DEBUG

/*****************************************************************************/
#ifdef DEBUG
/*****************************************************************************/

VarName Compiler::compVarName(regNumber reg, bool isFloatReg)
{
    if (isFloatReg)
    {
        assert(genIsValidFloatReg(reg));
    }
    else
    {
        assert(genIsValidReg(reg));
    }

    if ((info.compVarScopesCount > 0) && compCurBB && opts.varNames)
    {
        unsigned   lclNum;
        LclVarDsc* varDsc;

        /* Look for the matching register */
        for (lclNum = 0, varDsc = lvaTable; lclNum < lvaCount; lclNum++, varDsc++)
        {
            /* If the variable is not in a register, or not in the register we're looking for, quit. */
            /* Also, if it is a compiler generated variable (i.e. slot# > info.compVarScopesCount), don't bother. */
            if ((varDsc->lvRegister != 0) && (varDsc->lvRegNum == reg) && (varDsc->IsFloatRegType() || !isFloatReg) &&
                (varDsc->lvSlotNum < info.compVarScopesCount))
            {
                /* check if variable in that register is live */
                if (VarSetOps::IsMember(this, compCurLife, varDsc->lvVarIndex))
                {
                    /* variable is live - find the corresponding slot */
                    VarScopeDsc* varScope =
                        compFindLocalVar(varDsc->lvSlotNum, compCurBB->bbCodeOffs, compCurBB->bbCodeOffsEnd);
                    if (varScope)
                    {
                        return varScope->vsdName;
                    }
                }
            }
        }
    }

    return nullptr;
}

const char* Compiler::compRegVarName(regNumber reg, bool displayVar, bool isFloatReg)
{

#ifdef _TARGET_ARM_
    isFloatReg = genIsValidFloatReg(reg);
#endif

    if (displayVar && (reg != REG_NA))
    {
        VarName varName = compVarName(reg, isFloatReg);

        if (varName)
        {
            const int   NAME_VAR_REG_BUFFER_LEN = 4 + 256 + 1;
            static char nameVarReg[2][NAME_VAR_REG_BUFFER_LEN]; // to avoid overwriting the buffer when have 2
                                                                // consecutive calls before printing
            static int index = 0;                               // for circular index into the name array

            index = (index + 1) % 2; // circular reuse of index
            sprintf_s(nameVarReg[index], NAME_VAR_REG_BUFFER_LEN, "%s'%s'", getRegName(reg, isFloatReg),
                      VarNameToStr(varName));

            return nameVarReg[index];
        }
    }

    /* no debug info required or no variable in that register
       -> return standard name */

    return getRegName(reg, isFloatReg);
}

const char* Compiler::compRegNameForSize(regNumber reg, size_t size)
{
    if (size == 0 || size >= 4)
    {
        return compRegVarName(reg, true);
    }

    // clang-format off
    static
    const char  *   sizeNames[][2] =
    {
        { "al", "ax" },
        { "cl", "cx" },
        { "dl", "dx" },
        { "bl", "bx" },
#ifdef _TARGET_AMD64_
        {  "spl",   "sp" }, // ESP
        {  "bpl",   "bp" }, // EBP
        {  "sil",   "si" }, // ESI
        {  "dil",   "di" }, // EDI
        {  "r8b",  "r8w" },
        {  "r9b",  "r9w" },
        { "r10b", "r10w" },
        { "r11b", "r11w" },
        { "r12b", "r12w" },
        { "r13b", "r13w" },
        { "r14b", "r14w" },
        { "r15b", "r15w" },
#endif // _TARGET_AMD64_
    };
    // clang-format on

    assert(isByteReg(reg));
    assert(genRegMask(reg) & RBM_BYTE_REGS);
    assert(size == 1 || size == 2);

    return sizeNames[reg][size - 1];
}

const char* Compiler::compFPregVarName(unsigned fpReg, bool displayVar)
{
    const int   NAME_VAR_REG_BUFFER_LEN = 4 + 256 + 1;
    static char nameVarReg[2][NAME_VAR_REG_BUFFER_LEN]; // to avoid overwriting the buffer when have 2 consecutive calls
                                                        // before printing
    static int index = 0;                               // for circular index into the name array

    index = (index + 1) % 2; // circular reuse of index

    /* no debug info required or no variable in that register
       -> return standard name */

    sprintf_s(nameVarReg[index], NAME_VAR_REG_BUFFER_LEN, "ST(%d)", fpReg);
    return nameVarReg[index];
}

const char* Compiler::compLocalVarName(unsigned varNum, unsigned offs)
{
    unsigned     i;
    VarScopeDsc* t;

    for (i = 0, t = info.compVarScopes; i < info.compVarScopesCount; i++, t++)
    {
        if (t->vsdVarNum != varNum)
        {
            continue;
        }

        if (offs >= t->vsdLifeBeg && offs < t->vsdLifeEnd)
        {
            return VarNameToStr(t->vsdName);
        }
    }

    return nullptr;
}

/*****************************************************************************/
#endif // DEBUG
/*****************************************************************************/

void Compiler::compSetProcessor()
{
    //
    // NOTE: This function needs to be kept in sync with EEJitManager::SetCpuInfo() in vm\codemap.cpp
    //

    const JitFlags& jitFlags = *opts.jitFlags;

#if defined(_TARGET_ARM_)
    info.genCPU = CPU_ARM;
#elif defined(_TARGET_ARM64_)
    info.genCPU       = CPU_ARM64;
#elif defined(_TARGET_AMD64_)
    info.genCPU                   = CPU_X64;
#elif defined(_TARGET_X86_)
    if (jitFlags.IsSet(JitFlags::JIT_FLAG_TARGET_P4))
        info.genCPU = CPU_X86_PENTIUM_4;
    else
        info.genCPU = CPU_X86;
#endif

    //
    // Processor specific optimizations
    //
    CLANG_FORMAT_COMMENT_ANCHOR;

#ifdef _TARGET_AMD64_
    opts.compUseFCOMI = false;
    opts.compUseCMOV  = true;
#elif defined(_TARGET_X86_)
    opts.compUseFCOMI = jitFlags.IsSet(JitFlags::JIT_FLAG_USE_FCOMI);
    opts.compUseCMOV  = jitFlags.IsSet(JitFlags::JIT_FLAG_USE_CMOV);

#ifdef DEBUG
    if (opts.compUseFCOMI)
        opts.compUseFCOMI = !compStressCompile(STRESS_USE_FCOMI, 50);
    if (opts.compUseCMOV)
        opts.compUseCMOV = !compStressCompile(STRESS_USE_CMOV, 50);
#endif // DEBUG

#endif // _TARGET_X86_

// Instruction set flags for Intel hardware intrinsics
#ifdef _TARGET_XARCH_
    opts.compSupportsISA = 0;

#ifdef FEATURE_CORECLR
    if (JitConfig.EnableHWIntrinsic())
    {
        // Dummy ISAs for simplifying the JIT code
        opts.setSupportedISA(InstructionSet_Vector128);
        opts.setSupportedISA(InstructionSet_Vector256);

        if (JitConfig.EnableSSE())
        {
            opts.setSupportedISA(InstructionSet_SSE);
#ifdef _TARGET_AMD64_
            opts.setSupportedISA(InstructionSet_SSE_X64);
#endif // _TARGET_AMD64_

            if (JitConfig.EnableSSE2())
            {
                opts.setSupportedISA(InstructionSet_SSE2);
#ifdef _TARGET_AMD64_
                opts.setSupportedISA(InstructionSet_SSE2_X64);
#endif // _TARGET_AMD64_

                if (jitFlags.IsSet(JitFlags::JIT_FLAG_USE_AES) && JitConfig.EnableAES())
                {
                    opts.setSupportedISA(InstructionSet_AES);
                }

                if (jitFlags.IsSet(JitFlags::JIT_FLAG_USE_PCLMULQDQ) && JitConfig.EnablePCLMULQDQ())
                {
                    opts.setSupportedISA(InstructionSet_PCLMULQDQ);
                }

                // We need to additionaly check that COMPlus_EnableSSE3_4 is set, as that
                // is a prexisting config flag that controls the SSE3+ ISAs
                if (jitFlags.IsSet(JitFlags::JIT_FLAG_USE_SSE3) && JitConfig.EnableSSE3() && JitConfig.EnableSSE3_4())
                {
                    opts.setSupportedISA(InstructionSet_SSE3);

                    if (jitFlags.IsSet(JitFlags::JIT_FLAG_USE_SSSE3) && JitConfig.EnableSSSE3())
                    {
                        opts.setSupportedISA(InstructionSet_SSSE3);

                        if (jitFlags.IsSet(JitFlags::JIT_FLAG_USE_SSE41) && JitConfig.EnableSSE41())
                        {
                            opts.setSupportedISA(InstructionSet_SSE41);
#ifdef _TARGET_AMD64_
                            opts.setSupportedISA(InstructionSet_SSE41_X64);
#endif // _TARGET_AMD64_

                            if (jitFlags.IsSet(JitFlags::JIT_FLAG_USE_SSE42) && JitConfig.EnableSSE42())
                            {
                                opts.setSupportedISA(InstructionSet_SSE42);
#ifdef _TARGET_AMD64_
                                opts.setSupportedISA(InstructionSet_SSE42_X64);
#endif // _TARGET_AMD64_

                                if (jitFlags.IsSet(JitFlags::JIT_FLAG_USE_POPCNT) && JitConfig.EnablePOPCNT())
                                {
                                    opts.setSupportedISA(InstructionSet_POPCNT);
#ifdef _TARGET_AMD64_
                                    opts.setSupportedISA(InstructionSet_POPCNT_X64);
#endif // _TARGET_AMD64_
                                }

                                if (jitFlags.IsSet(JitFlags::JIT_FLAG_USE_AVX) && JitConfig.EnableAVX())
                                {
                                    opts.setSupportedISA(InstructionSet_AVX);

                                    if (jitFlags.IsSet(JitFlags::JIT_FLAG_USE_FMA) && JitConfig.EnableFMA())
                                    {
                                        opts.setSupportedISA(InstructionSet_FMA);
                                    }

                                    if (jitFlags.IsSet(JitFlags::JIT_FLAG_USE_AVX2) && JitConfig.EnableAVX2())
                                    {
                                        opts.setSupportedISA(InstructionSet_AVX2);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        if (jitFlags.IsSet(JitFlags::JIT_FLAG_USE_LZCNT) && JitConfig.EnableLZCNT())
        {
            opts.setSupportedISA(InstructionSet_LZCNT);
#ifdef _TARGET_AMD64_
            opts.setSupportedISA(InstructionSet_LZCNT_X64);
#endif // _TARGET_AMD64_
        }

        // We currently need to also check that AVX is supported as that controls the support for the VEX encoding
        // in the emitter.
        if (jitFlags.IsSet(JitFlags::JIT_FLAG_USE_BMI1) && JitConfig.EnableBMI1() && compSupports(InstructionSet_AVX))
        {
            opts.setSupportedISA(InstructionSet_BMI1);
#ifdef _TARGET_AMD64_
            opts.setSupportedISA(InstructionSet_BMI1_X64);
#endif // _TARGET_AMD64_
        }

        // We currently need to also check that AVX is supported as that controls the support for the VEX encoding
        // in the emitter.
        if (jitFlags.IsSet(JitFlags::JIT_FLAG_USE_BMI2) && JitConfig.EnableBMI2() && compSupports(InstructionSet_AVX))
        {
            opts.setSupportedISA(InstructionSet_BMI2);
#ifdef _TARGET_AMD64_
            opts.setSupportedISA(InstructionSet_BMI2_X64);
#endif // _TARGET_AMD64_
        }
    }
#else  // !FEATURE_CORECLR
    if (!jitFlags.IsSet(JitFlags::JIT_FLAG_PREJIT))
    {
        // If this is not FEATURE_CORECLR, the only flags supported by the VM are AVX and AVX2.
        // Furthermore, the only two configurations supported by the desktop JIT are SSE2 and AVX2,
        // so if the latter is set, we also check all the in-between options.
        // Note that the EnableSSE2 and EnableSSE flags are only checked by HW Intrinsic code,
        // so the System.Numerics.Vector support doesn't depend on those flags.
        // However, if any of these are disabled, we will not enable AVX2.
        //
        if (jitFlags.IsSet(JitFlags::JIT_FLAG_USE_AVX) && jitFlags.IsSet(JitFlags::JIT_FLAG_USE_AVX2) &&
            (JitConfig.EnableAVX2() != 0) && (JitConfig.EnableAVX() != 0) && (JitConfig.EnableSSE42() != 0) &&
            (JitConfig.EnableSSE41() != 0) && (JitConfig.EnableSSSE3() != 0) && (JitConfig.EnableSSE3() != 0) &&
            (JitConfig.EnableSSE2() != 0) && (JitConfig.EnableSSE() != 0) && (JitConfig.EnableSSE3_4() != 0))
        {
            opts.setSupportedISA(InstructionSet_SSE);
            opts.setSupportedISA(InstructionSet_SSE2);
            opts.setSupportedISA(InstructionSet_SSE3);
            opts.setSupportedISA(InstructionSet_SSSE3);
            opts.setSupportedISA(InstructionSet_SSE41);
            opts.setSupportedISA(InstructionSet_SSE42);
            opts.setSupportedISA(InstructionSet_AVX);
            opts.setSupportedISA(InstructionSet_AVX2);
        }
    }
#endif // !FEATURE_CORECLR

    if (!compIsForInlining())
    {
        if (canUseVexEncoding())
        {
            codeGen->getEmitter()->SetUseVEXEncoding(true);
            // Assume each JITted method does not contain AVX instruction at first
            codeGen->getEmitter()->SetContainsAVX(false);
            codeGen->getEmitter()->SetContains256bitAVX(false);
        }
    }
#endif // _TARGET_XARCH_

#if defined(_TARGET_ARM64_)
    // There is no JitFlag for Base instructions handle manually
    opts.setSupportedISA(InstructionSet_Base);
    // Dummy ISAs for simplifying the JIT code
    opts.setSupportedISA(InstructionSet_Vector64);
    opts.setSupportedISA(InstructionSet_Vector128);
#define HARDWARE_INTRINSIC_CLASS(flag, jit_config, isa)                                                                \
    if (jitFlags.IsSet(JitFlags::flag) && JitConfig.jit_config())                                                      \
        opts.setSupportedISA(InstructionSet_##isa);
#include "hwintrinsiclistArm64.h"

#endif
}

#ifdef PROFILING_SUPPORTED
// A Dummy routine to receive Enter/Leave/Tailcall profiler callbacks.
// These are used when complus_JitEltHookEnabled=1
#ifdef _TARGET_AMD64_
void DummyProfilerELTStub(UINT_PTR ProfilerHandle, UINT_PTR callerSP)
{
    return;
}
#else  //! _TARGET_AMD64_
void DummyProfilerELTStub(UINT_PTR ProfilerHandle)
{
    return;
}
#endif //!_TARGET_AMD64_

#endif // PROFILING_SUPPORTED

bool Compiler::compIsFullTrust()
{
    return (info.compCompHnd->canSkipMethodVerification(info.compMethodHnd) == CORINFO_VERIFICATION_CAN_SKIP);
}

bool Compiler::compShouldThrowOnNoway(
#ifdef FEATURE_TRACELOGGING
    const char* filename, unsigned line
#endif
    )
{
#ifdef FEATURE_TRACELOGGING
    compJitTelemetry.NotifyNowayAssert(filename, line);
#endif

    // In min opts, we don't want the noway assert to go through the exception
    // path. Instead we want it to just silently go through codegen for
    // compat reasons.
    // If we are not in full trust, we should always fire for security.
    return !opts.MinOpts() || !compIsFullTrust();
}

// ConfigInteger does not offer an option for decimal flags.  Any numbers are interpreted as hex.
// I could add the decimal option to ConfigInteger or I could write a function to reinterpret this
// value as the user intended.
unsigned ReinterpretHexAsDecimal(unsigned in)
{
    // ex: in: 0x100 returns: 100
    unsigned result = 0;
    unsigned index  = 1;

    // default value
    if (in == INT_MAX)
    {
        return in;
    }

    while (in)
    {
        unsigned digit = in % 16;
        in >>= 4;
        assert(digit < 10);
        result += digit * index;
        index *= 10;
    }
    return result;
}

void Compiler::compInitOptions(JitFlags* jitFlags)
{
#ifdef UNIX_AMD64_ABI
    opts.compNeedToAlignFrame = false;
#endif // UNIX_AMD64_ABI
    memset(&opts, 0, sizeof(opts));

    if (compIsForInlining())
    {
        // The following flags are lost when inlining. (They are removed in
        // Compiler::fgInvokeInlineeCompiler().)
        assert(!jitFlags->IsSet(JitFlags::JIT_FLAG_BBOPT));
        assert(!jitFlags->IsSet(JitFlags::JIT_FLAG_BBINSTR));
        assert(!jitFlags->IsSet(JitFlags::JIT_FLAG_PROF_ENTERLEAVE));
        assert(!jitFlags->IsSet(JitFlags::JIT_FLAG_DEBUG_EnC));
        assert(!jitFlags->IsSet(JitFlags::JIT_FLAG_DEBUG_INFO));

        assert(jitFlags->IsSet(JitFlags::JIT_FLAG_SKIP_VERIFICATION));
    }

    opts.jitFlags  = jitFlags;
    opts.compFlags = CLFLG_MAXOPT; // Default value is for full optimization

    if (jitFlags->IsSet(JitFlags::JIT_FLAG_DEBUG_CODE) || jitFlags->IsSet(JitFlags::JIT_FLAG_MIN_OPT) ||
        jitFlags->IsSet(JitFlags::JIT_FLAG_TIER0))
    {
        opts.compFlags = CLFLG_MINOPT;
    }
    // Don't optimize .cctors (except prejit) or if we're an inlinee
    else if (!jitFlags->IsSet(JitFlags::JIT_FLAG_PREJIT) && ((info.compFlags & FLG_CCTOR) == FLG_CCTOR) &&
             !compIsForInlining())
    {
        opts.compFlags = CLFLG_MINOPT;
    }

    // Default value is to generate a blend of size and speed optimizations
    //
    opts.compCodeOpt = BLENDED_CODE;

    // If the EE sets SIZE_OPT or if we are compiling a Class constructor
    // we will optimize for code size at the expense of speed
    //
    if (jitFlags->IsSet(JitFlags::JIT_FLAG_SIZE_OPT) || ((info.compFlags & FLG_CCTOR) == FLG_CCTOR))
    {
        opts.compCodeOpt = SMALL_CODE;
    }
    //
    // If the EE sets SPEED_OPT we will optimize for speed at the expense of code size
    //
    else if (jitFlags->IsSet(JitFlags::JIT_FLAG_SPEED_OPT) ||
             (jitFlags->IsSet(JitFlags::JIT_FLAG_TIER1) && !jitFlags->IsSet(JitFlags::JIT_FLAG_MIN_OPT)))
    {
        opts.compCodeOpt = FAST_CODE;
        assert(!jitFlags->IsSet(JitFlags::JIT_FLAG_SIZE_OPT));
    }

    //-------------------------------------------------------------------------

    opts.compDbgCode = jitFlags->IsSet(JitFlags::JIT_FLAG_DEBUG_CODE);
    opts.compDbgInfo = jitFlags->IsSet(JitFlags::JIT_FLAG_DEBUG_INFO);
    opts.compDbgEnC  = jitFlags->IsSet(JitFlags::JIT_FLAG_DEBUG_EnC);

#if REGEN_SHORTCUTS || REGEN_CALLPAT
    // We never want to have debugging enabled when regenerating GC encoding patterns
    opts.compDbgCode = false;
    opts.compDbgInfo = false;
    opts.compDbgEnC  = false;
#endif

    compSetProcessor();

#ifdef DEBUG
    opts.dspOrder = false;
    if (compIsForInlining())
    {
        verbose = impInlineInfo->InlinerCompiler->verbose;
    }
    else
    {
        verbose = false;
        codeGen->setVerbose(false);
    }
    verboseTrees     = verbose && shouldUseVerboseTrees();
    verboseSsa       = verbose && shouldUseVerboseSsa();
    asciiTrees       = shouldDumpASCIITrees();
    opts.dspDiffable = compIsForInlining() ? impInlineInfo->InlinerCompiler->opts.dspDiffable : false;
#endif

    opts.compNeedSecurityCheck = false;
    opts.altJit                = false;

#if defined(LATE_DISASM) && !defined(DEBUG)
    // For non-debug builds with the late disassembler built in, we currently always do late disassembly
    // (we have no way to determine when not to, since we don't have class/method names).
    // In the DEBUG case, this is initialized to false, below.
    opts.doLateDisasm = true;
#endif

#ifdef DEBUG

    const JitConfigValues::MethodSet* pfAltJit;
    if (jitFlags->IsSet(JitFlags::JIT_FLAG_PREJIT))
    {
        pfAltJit = &JitConfig.AltJitNgen();
    }
    else
    {
        pfAltJit = &JitConfig.AltJit();
    }

#ifdef ALT_JIT
    if (pfAltJit->contains(info.compMethodName, info.compClassName, &info.compMethodInfo->args))
    {
        opts.altJit = true;
    }

    unsigned altJitLimit = ReinterpretHexAsDecimal(JitConfig.AltJitLimit());
    if (altJitLimit > 0 && Compiler::jitTotalMethodCompiled >= altJitLimit)
    {
        opts.altJit = false;
    }
#endif // ALT_JIT

#else // !DEBUG

    const char* altJitVal;
    if (jitFlags->IsSet(JitFlags::JIT_FLAG_PREJIT))
    {
        altJitVal = JitConfig.AltJitNgen().list();
    }
    else
    {
        altJitVal = JitConfig.AltJit().list();
    }

#ifdef ALT_JIT
    // In release mode, you either get all methods or no methods. You must use "*" as the parameter, or we ignore it.
    // You don't get to give a regular expression of methods to match.
    // (Partially, this is because we haven't computed and stored the method and class name except in debug, and it
    // might be expensive to do so.)
    if ((altJitVal != nullptr) && (strcmp(altJitVal, "*") == 0))
    {
        opts.altJit = true;
    }
#endif // ALT_JIT

#endif // !DEBUG

#ifdef ALT_JIT
    // Take care of COMPlus_AltJitExcludeAssemblies.
    if (opts.altJit)
    {
        // First, initialize the AltJitExcludeAssemblies list, but only do it once.
        if (!s_pAltJitExcludeAssembliesListInitialized)
        {
            const wchar_t* wszAltJitExcludeAssemblyList = JitConfig.AltJitExcludeAssemblies();
            if (wszAltJitExcludeAssemblyList != nullptr)
            {
                // NOTE: The Assembly name list is allocated in the process heap, not in the no-release heap, which is
                // reclaimed
                // for every compilation. This is ok because we only allocate once, due to the static.
                s_pAltJitExcludeAssembliesList = new (HostAllocator::getHostAllocator())
                    AssemblyNamesList2(wszAltJitExcludeAssemblyList, HostAllocator::getHostAllocator());
            }
            s_pAltJitExcludeAssembliesListInitialized = true;
        }

        if (s_pAltJitExcludeAssembliesList != nullptr)
        {
            // We have an exclusion list. See if this method is in an assembly that is on the list.
            // Note that we check this for every method, since we might inline across modules, and
            // if the inlinee module is on the list, we don't want to use the altjit for it.
            const char* methodAssemblyName = info.compCompHnd->getAssemblyName(
                info.compCompHnd->getModuleAssembly(info.compCompHnd->getClassModule(info.compClassHnd)));
            if (s_pAltJitExcludeAssembliesList->IsInList(methodAssemblyName))
            {
                opts.altJit = false;
            }
        }
    }
#endif // ALT_JIT

#ifdef DEBUG

    bool altJitConfig = !pfAltJit->isEmpty();

    //  If we have a non-empty AltJit config then we change all of these other
    //  config values to refer only to the AltJit. Otherwise, a lot of COMPlus_* variables
    //  would apply to both the altjit and the normal JIT, but we only care about
    //  debugging the altjit if the COMPlus_AltJit configuration is set.
    //
    if (compIsForImportOnly() && (!altJitConfig || opts.altJit))
    {
        if (JitConfig.JitImportBreak().contains(info.compMethodName, info.compClassName, &info.compMethodInfo->args))
        {
            assert(!"JitImportBreak reached");
        }
    }

    bool    verboseDump        = false;
    bool    dumpIR             = false;
    bool    dumpIRTypes        = false;
    bool    dumpIRLocals       = false;
    bool    dumpIRRegs         = false;
    bool    dumpIRSsa          = false;
    bool    dumpIRValnums      = false;
    bool    dumpIRCosts        = false;
    bool    dumpIRFlags        = false;
    bool    dumpIRKinds        = false;
    bool    dumpIRNodes        = false;
    bool    dumpIRNoLists      = false;
    bool    dumpIRNoLeafs      = false;
    bool    dumpIRNoStmts      = false;
    bool    dumpIRTrees        = false;
    bool    dumpIRLinear       = false;
    bool    dumpIRDataflow     = false;
    bool    dumpIRBlockHeaders = false;
    bool    dumpIRExit         = false;
    LPCWSTR dumpIRPhase        = nullptr;

    if (!altJitConfig || opts.altJit)
    {
        LPCWSTR dumpIRFormat = nullptr;

        // We should only enable 'verboseDump' when we are actually compiling a matching method
        // and not enable it when we are just considering inlining a matching method.
        //
        if (!compIsForInlining())
        {
            if (jitFlags->IsSet(JitFlags::JIT_FLAG_PREJIT))
            {
                if (JitConfig.NgenDump().contains(info.compMethodName, info.compClassName, &info.compMethodInfo->args))
                {
                    verboseDump = true;
                }
                unsigned ngenHashDumpVal = (unsigned)JitConfig.NgenHashDump();
                if ((ngenHashDumpVal != (DWORD)-1) && (ngenHashDumpVal == info.compMethodHash()))
                {
                    verboseDump = true;
                }
                if (JitConfig.NgenDumpIR().contains(info.compMethodName, info.compClassName,
                                                    &info.compMethodInfo->args))
                {
                    dumpIR = true;
                }
                unsigned ngenHashDumpIRVal = (unsigned)JitConfig.NgenHashDumpIR();
                if ((ngenHashDumpIRVal != (DWORD)-1) && (ngenHashDumpIRVal == info.compMethodHash()))
                {
                    dumpIR = true;
                }
                dumpIRFormat = JitConfig.NgenDumpIRFormat();
                dumpIRPhase  = JitConfig.NgenDumpIRPhase();
            }
            else
            {
                if (JitConfig.JitDump().contains(info.compMethodName, info.compClassName, &info.compMethodInfo->args))
                {
                    verboseDump = true;
                }
                unsigned jitHashDumpVal = (unsigned)JitConfig.JitHashDump();
                if ((jitHashDumpVal != (DWORD)-1) && (jitHashDumpVal == info.compMethodHash()))
                {
                    verboseDump = true;
                }
                if (JitConfig.JitDumpIR().contains(info.compMethodName, info.compClassName, &info.compMethodInfo->args))
                {
                    dumpIR = true;
                }
                unsigned jitHashDumpIRVal = (unsigned)JitConfig.JitHashDumpIR();
                if ((jitHashDumpIRVal != (DWORD)-1) && (jitHashDumpIRVal == info.compMethodHash()))
                {
                    dumpIR = true;
                }
                dumpIRFormat = JitConfig.JitDumpIRFormat();
                dumpIRPhase  = JitConfig.JitDumpIRPhase();
            }
        }

        if (dumpIRPhase == nullptr)
        {
            dumpIRPhase = W("*");
        }

        this->dumpIRPhase = dumpIRPhase;

        if (dumpIRFormat != nullptr)
        {
            this->dumpIRFormat = dumpIRFormat;
        }

        dumpIRTrees  = false;
        dumpIRLinear = true;
        if (dumpIRFormat != nullptr)
        {
            for (LPCWSTR p = dumpIRFormat; (*p != 0);)
            {
                for (; (*p != 0); p++)
                {
                    if (*p != L' ')
                    {
                        break;
                    }
                }

                if (*p == 0)
                {
                    break;
                }

                static bool dumpedHelp = false;

                if ((*p == L'?') && (!dumpedHelp))
                {
                    printf("*******************************************************************************\n");
                    printf("\n");
                    dFormatIR();
                    printf("\n");
                    printf("\n");
                    printf("Available specifiers (comma separated):\n");
                    printf("\n");
                    printf("?          dump out value of COMPlus_JitDumpIRFormat and this list of values\n");
                    printf("\n");
                    printf("linear     linear IR dump (default)\n");
                    printf("tree       tree IR dump (traditional)\n");
                    printf("mixed      intermingle tree dump with linear IR dump\n");
                    printf("\n");
                    printf("dataflow   use data flow form of linear IR dump\n");
                    printf("structural use structural form of linear IR dump\n");
                    printf("all        implies structural, include everything\n");
                    printf("\n");
                    printf("kinds      include tree node kinds in dump, example: \"kinds=[LEAF][LOCAL]\"\n");
                    printf("flags      include tree node flags in dump, example: \"flags=[CALL][GLOB_REF]\" \n");
                    printf("types      includes tree node types in dump, example: \".int\"\n");
                    printf("locals     include local numbers and tracking numbers in dump, example: \"(V3,T1)\"\n");
                    printf("regs       include register assignments in dump, example: \"(rdx)\"\n");
                    printf("ssa        include SSA numbers in dump, example: \"<d:3>\" or \"<u:3>\"\n");
                    printf("valnums    include Value numbers in dump, example: \"<v:$c4>\" or \"<v:$c4,$c5>\"\n");
                    printf("\n");
                    printf("nolist     exclude GT_LIST nodes from dump\n");
                    printf("noleafs    exclude LEAF nodes from dump (fold into operations)\n");
                    printf("nostmts    exclude GT_STMTS from dump (unless required by dependencies)\n");
                    printf("\n");
                    printf("blkhdrs    include block headers\n");
                    printf("exit       exit program after last phase dump (used with single method)\n");
                    printf("\n");
                    printf("*******************************************************************************\n");
                    dumpedHelp = true;
                }

                if (wcsncmp(p, W("types"), 5) == 0)
                {
                    dumpIRTypes = true;
                }

                if (wcsncmp(p, W("locals"), 6) == 0)
                {
                    dumpIRLocals = true;
                }

                if (wcsncmp(p, W("regs"), 4) == 0)
                {
                    dumpIRRegs = true;
                }

                if (wcsncmp(p, W("ssa"), 3) == 0)
                {
                    dumpIRSsa = true;
                }

                if (wcsncmp(p, W("valnums"), 7) == 0)
                {
                    dumpIRValnums = true;
                }

                if (wcsncmp(p, W("costs"), 5) == 0)
                {
                    dumpIRCosts = true;
                }

                if (wcsncmp(p, W("flags"), 5) == 0)
                {
                    dumpIRFlags = true;
                }

                if (wcsncmp(p, W("kinds"), 5) == 0)
                {
                    dumpIRKinds = true;
                }

                if (wcsncmp(p, W("nodes"), 5) == 0)
                {
                    dumpIRNodes = true;
                }

                if (wcsncmp(p, W("exit"), 4) == 0)
                {
                    dumpIRExit = true;
                }

                if (wcsncmp(p, W("nolists"), 7) == 0)
                {
                    dumpIRNoLists = true;
                }

                if (wcsncmp(p, W("noleafs"), 7) == 0)
                {
                    dumpIRNoLeafs = true;
                }

                if (wcsncmp(p, W("nostmts"), 7) == 0)
                {
                    dumpIRNoStmts = true;
                }

                if (wcsncmp(p, W("trees"), 5) == 0)
                {
                    dumpIRTrees  = true;
                    dumpIRLinear = false;
                }

                if (wcsncmp(p, W("structural"), 10) == 0)
                {
                    dumpIRLinear  = true;
                    dumpIRNoStmts = false;
                    dumpIRNoLeafs = false;
                    dumpIRNoLists = false;
                }

                if (wcsncmp(p, W("all"), 3) == 0)
                {
                    dumpIRLinear  = true;
                    dumpIRKinds   = true;
                    dumpIRFlags   = true;
                    dumpIRTypes   = true;
                    dumpIRLocals  = true;
                    dumpIRRegs    = true;
                    dumpIRSsa     = true;
                    dumpIRValnums = true;
                    dumpIRCosts   = true;
                    dumpIRNoStmts = false;
                    dumpIRNoLeafs = false;
                    dumpIRNoLists = false;
                }

                if (wcsncmp(p, W("linear"), 6) == 0)
                {
                    dumpIRTrees  = false;
                    dumpIRLinear = true;
                }

                if (wcsncmp(p, W("mixed"), 5) == 0)
                {
                    dumpIRTrees  = true;
                    dumpIRLinear = true;
                }

                if (wcsncmp(p, W("dataflow"), 8) == 0)
                {
                    dumpIRDataflow = true;
                    dumpIRNoLeafs  = true;
                    dumpIRNoLists  = true;
                    dumpIRNoStmts  = true;
                }

                if (wcsncmp(p, W("blkhdrs"), 7) == 0)
                {
                    dumpIRBlockHeaders = true;
                }

                for (; (*p != 0); p++)
                {
                    if (*p == L',')
                    {
                        p++;
                        break;
                    }
                }
            }
        }
    }

    if (verboseDump)
    {
        verbose = true;
    }

    if (dumpIR)
    {
        this->dumpIR = true;
    }

    if (dumpIRTypes)
    {
        this->dumpIRTypes = true;
    }

    if (dumpIRLocals)
    {
        this->dumpIRLocals = true;
    }

    if (dumpIRRegs)
    {
        this->dumpIRRegs = true;
    }

    if (dumpIRSsa)
    {
        this->dumpIRSsa = true;
    }

    if (dumpIRValnums)
    {
        this->dumpIRValnums = true;
    }

    if (dumpIRCosts)
    {
        this->dumpIRCosts = true;
    }

    if (dumpIRFlags)
    {
        this->dumpIRFlags = true;
    }

    if (dumpIRKinds)
    {
        this->dumpIRKinds = true;
    }

    if (dumpIRNodes)
    {
        this->dumpIRNodes = true;
    }

    if (dumpIRNoLists)
    {
        this->dumpIRNoLists = true;
    }

    if (dumpIRNoLeafs)
    {
        this->dumpIRNoLeafs = true;
    }

    if (dumpIRNoLeafs && dumpIRDataflow)
    {
        this->dumpIRDataflow = true;
    }

    if (dumpIRNoStmts)
    {
        this->dumpIRNoStmts = true;
    }

    if (dumpIRTrees)
    {
        this->dumpIRTrees = true;
    }

    if (dumpIRLinear)
    {
        this->dumpIRLinear = true;
    }

    if (dumpIRBlockHeaders)
    {
        this->dumpIRBlockHeaders = true;
    }

    if (dumpIRExit)
    {
        this->dumpIRExit = true;
    }

#endif // DEBUG

#ifdef FEATURE_SIMD
    // Minimum bar for availing SIMD benefits is SSE2 on AMD64/x86.
    featureSIMD = jitFlags->IsSet(JitFlags::JIT_FLAG_FEATURE_SIMD);
    setUsesSIMDTypes(false);
#endif // FEATURE_SIMD

    if (compIsForImportOnly())
    {
        return;
    }

#if FEATURE_TAILCALL_OPT
    // By default opportunistic tail call optimization is enabled.
    // Recognition is done in the importer so this must be set for
    // inlinees as well.
    opts.compTailCallOpt = true;
#endif // FEATURE_TAILCALL_OPT

    if (compIsForInlining())
    {
        return;
    }

    // The rest of the opts fields that we initialize here
    // should only be used when we generate code for the method
    // They should not be used when importing or inlining
    CLANG_FORMAT_COMMENT_ANCHOR;

#if FEATURE_TAILCALL_OPT
    opts.compTailCallLoopOpt = true;
#endif // FEATURE_TAILCALL_OPT

    opts.genFPorder = true;
    opts.genFPopt   = true;

    opts.instrCount = 0;
    opts.lvRefCount = 0;

#ifdef PROFILING_SUPPORTED
    opts.compJitELTHookEnabled = false;
#endif // PROFILING_SUPPORTED

#if defined(_TARGET_ARM64_)
    // 0 is default: use the appropriate frame type based on the function.
    opts.compJitSaveFpLrWithCalleeSavedRegisters = 0;
#endif // defined(_TARGET_ARM64_)

#ifdef DEBUG
    opts.dspInstrs       = false;
    opts.dspEmit         = false;
    opts.dspLines        = false;
    opts.varNames        = false;
    opts.dmpHex          = false;
    opts.disAsm          = false;
    opts.disAsmSpilled   = false;
    opts.disDiffable     = false;
    opts.dspCode         = false;
    opts.dspEHTable      = false;
    opts.dspDebugInfo    = false;
    opts.dspGCtbls       = false;
    opts.disAsm2         = false;
    opts.dspUnwind       = false;
    opts.compLongAddress = false;
    opts.optRepeat       = false;

#ifdef LATE_DISASM
    opts.doLateDisasm = false;
#endif // LATE_DISASM

    compDebugBreak = false;

    //  If we have a non-empty AltJit config then we change all of these other
    //  config values to refer only to the AltJit.
    //
    if (!altJitConfig || opts.altJit)
    {
        if (jitFlags->IsSet(JitFlags::JIT_FLAG_PREJIT))
        {
            if ((JitConfig.NgenOrder() & 1) == 1)
            {
                opts.dspOrder = true;
            }

            if (JitConfig.NgenGCDump().contains(info.compMethodName, info.compClassName, &info.compMethodInfo->args))
            {
                opts.dspGCtbls = true;
            }

            if (JitConfig.NgenDisasm().contains(info.compMethodName, info.compClassName, &info.compMethodInfo->args))
            {
                opts.disAsm = true;
            }
            if (JitConfig.NgenDisasm().contains("SPILLED", nullptr, nullptr))
            {
                opts.disAsmSpilled = true;
            }

            if (JitConfig.NgenUnwindDump().contains(info.compMethodName, info.compClassName,
                                                    &info.compMethodInfo->args))
            {
                opts.dspUnwind = true;
            }

            if (JitConfig.NgenEHDump().contains(info.compMethodName, info.compClassName, &info.compMethodInfo->args))
            {
                opts.dspEHTable = true;
            }

            if (JitConfig.NgenDebugDump().contains(info.compMethodName, info.compClassName, &info.compMethodInfo->args))
            {
                opts.dspDebugInfo = true;
            }
        }
        else
        {
            bool disEnabled = true;

            // Setup assembly name list for disassembly, if not already set up.
            if (!s_pJitDisasmIncludeAssembliesListInitialized)
            {
                const wchar_t* assemblyNameList = JitConfig.JitDisasmAssemblies();
                if (assemblyNameList != nullptr)
                {
                    s_pJitDisasmIncludeAssembliesList = new (HostAllocator::getHostAllocator())
                        AssemblyNamesList2(assemblyNameList, HostAllocator::getHostAllocator());
                }
                s_pJitDisasmIncludeAssembliesListInitialized = true;
            }

            // If we have an assembly name list for disassembly, also check this method's assembly.
            if (s_pJitDisasmIncludeAssembliesList != nullptr)
            {
                const char* assemblyName = info.compCompHnd->getAssemblyName(
                    info.compCompHnd->getModuleAssembly(info.compCompHnd->getClassModule(info.compClassHnd)));

                if (!s_pJitDisasmIncludeAssembliesList->IsInList(assemblyName))
                {
                    disEnabled = false;
                }
            }

            if (disEnabled)
            {
                if ((JitConfig.JitOrder() & 1) == 1)
                {
                    opts.dspOrder = true;
                }

                if (JitConfig.JitGCDump().contains(info.compMethodName, info.compClassName, &info.compMethodInfo->args))
                {
                    opts.dspGCtbls = true;
                }

                if (JitConfig.JitDisasm().contains(info.compMethodName, info.compClassName, &info.compMethodInfo->args))
                {
                    opts.disAsm = true;
                }

                if (JitConfig.JitDisasm().contains("SPILLED", nullptr, nullptr))
                {
                    opts.disAsmSpilled = true;
                }

                if (JitConfig.JitUnwindDump().contains(info.compMethodName, info.compClassName,
                                                       &info.compMethodInfo->args))
                {
                    opts.dspUnwind = true;
                }

                if (JitConfig.JitEHDump().contains(info.compMethodName, info.compClassName, &info.compMethodInfo->args))
                {
                    opts.dspEHTable = true;
                }

                if (JitConfig.JitDebugDump().contains(info.compMethodName, info.compClassName,
                                                      &info.compMethodInfo->args))
                {
                    opts.dspDebugInfo = true;
                }
            }
        }

#ifdef LATE_DISASM
        if (JitConfig.JitLateDisasm().contains(info.compMethodName, info.compClassName, &info.compMethodInfo->args))
            opts.doLateDisasm = true;
#endif // LATE_DISASM

        // This one applies to both Ngen/Jit Disasm output: COMPlus_JitDiffableDasm=1
        if (JitConfig.DiffableDasm() != 0)
        {
            opts.disDiffable = true;
            opts.dspDiffable = true;
        }

        if (JitConfig.JitLongAddress() != 0)
        {
            opts.compLongAddress = true;
        }

        if (JitConfig.JitOptRepeat().contains(info.compMethodName, info.compClassName, &info.compMethodInfo->args))
        {
            opts.optRepeat = true;
        }
    }

    if (verboseDump)
    {
        opts.dspCode    = true;
        opts.dspEHTable = true;
        opts.dspGCtbls  = true;
        opts.disAsm2    = true;
        opts.dspUnwind  = true;
        verbose         = true;
        verboseTrees    = shouldUseVerboseTrees();
        verboseSsa      = shouldUseVerboseSsa();
        codeGen->setVerbose(true);
    }

    treesBeforeAfterMorph = (JitConfig.TreesBeforeAfterMorph() == 1);
    morphNum              = 0; // Initialize the morphed-trees counting.

    expensiveDebugCheckLevel = JitConfig.JitExpensiveDebugCheckLevel();
    if (expensiveDebugCheckLevel == 0)
    {
        // If we're in a stress mode that modifies the flowgraph, make 1 the default.
        if (fgStressBBProf() || compStressCompile(STRESS_DO_WHILE_LOOPS, 30))
        {
            expensiveDebugCheckLevel = 1;
        }
    }

    if (verbose)
    {
        printf("****** START compiling %s (MethodHash=%08x)\n", info.compFullName, info.compMethodHash());
        printf("Generating code for %s %s\n", Target::g_tgtPlatformName, Target::g_tgtCPUName);
        printf(""); // in our logic this causes a flush
    }

    if (JitConfig.JitBreak().contains(info.compMethodName, info.compClassName, &info.compMethodInfo->args))
    {
        assert(!"JitBreak reached");
    }

    unsigned jitHashBreakVal = (unsigned)JitConfig.JitHashBreak();
    if ((jitHashBreakVal != (DWORD)-1) && (jitHashBreakVal == info.compMethodHash()))
    {
        assert(!"JitHashBreak reached");
    }

    if (verbose ||
        JitConfig.JitDebugBreak().contains(info.compMethodName, info.compClassName, &info.compMethodInfo->args) ||
        JitConfig.JitBreak().contains(info.compMethodName, info.compClassName, &info.compMethodInfo->args))
    {
        compDebugBreak = true;
    }

    memset(compActiveStressModes, 0, sizeof(compActiveStressModes));

    // Read function list, if not already read, and there exists such a list.
    if (!s_pJitFunctionFileInitialized)
    {
        const wchar_t* functionFileName = JitConfig.JitFunctionFile();
        if (functionFileName != nullptr)
        {
            s_pJitMethodSet =
                new (HostAllocator::getHostAllocator()) MethodSet(functionFileName, HostAllocator::getHostAllocator());
        }
        s_pJitFunctionFileInitialized = true;
    }

#endif // DEBUG

//-------------------------------------------------------------------------

#ifdef DEBUG
    assert(!codeGen->isGCTypeFixed());
    opts.compGcChecks = (JitConfig.JitGCChecks() != 0) || compStressCompile(STRESS_GENERIC_VARN, 5);
#endif

#if defined(DEBUG) && defined(_TARGET_XARCH_)
    enum
    {
        STACK_CHECK_ON_RETURN = 0x1,
        STACK_CHECK_ON_CALL   = 0x2,
        STACK_CHECK_ALL       = 0x3
    };

    DWORD dwJitStackChecks = JitConfig.JitStackChecks();
    if (compStressCompile(STRESS_GENERIC_VARN, 5))
    {
        dwJitStackChecks = STACK_CHECK_ALL;
    }
    opts.compStackCheckOnRet = (dwJitStackChecks & DWORD(STACK_CHECK_ON_RETURN)) != 0;
#if defined(_TARGET_X86_)
    opts.compStackCheckOnCall = (dwJitStackChecks & DWORD(STACK_CHECK_ON_CALL)) != 0;
#endif // defined(_TARGET_X86_)
#endif // defined(DEBUG) && defined(_TARGET_XARCH_)

#if MEASURE_MEM_ALLOC
    s_dspMemStats = (JitConfig.DisplayMemStats() != 0);
#endif

#ifdef PROFILING_SUPPORTED
    opts.compNoPInvokeInlineCB = jitFlags->IsSet(JitFlags::JIT_FLAG_PROF_NO_PINVOKE_INLINE);

    // Cache the profiler handle
    if (jitFlags->IsSet(JitFlags::JIT_FLAG_PROF_ENTERLEAVE))
    {
        BOOL hookNeeded;
        BOOL indirected;
        info.compCompHnd->GetProfilingHandle(&hookNeeded, &compProfilerMethHnd, &indirected);
        compProfilerHookNeeded        = !!hookNeeded;
        compProfilerMethHndIndirected = !!indirected;
    }
    else
    {
        compProfilerHookNeeded        = false;
        compProfilerMethHnd           = nullptr;
        compProfilerMethHndIndirected = false;
    }

    // Honour COMPlus_JitELTHookEnabled only if VM has not asked us to generate profiler
    // hooks in the first place. That is, override VM only if it hasn't asked for a
    // profiler callback for this method.
    if (!compProfilerHookNeeded && (JitConfig.JitELTHookEnabled() != 0))
    {
        opts.compJitELTHookEnabled = true;
    }

    // TBD: Exclude PInvoke stubs
    if (opts.compJitELTHookEnabled)
    {
        compProfilerMethHnd           = (void*)DummyProfilerELTStub;
        compProfilerMethHndIndirected = false;
    }

#endif // PROFILING_SUPPORTED

#if FEATURE_TAILCALL_OPT
    const wchar_t* strTailCallOpt = JitConfig.TailCallOpt();
    if (strTailCallOpt != nullptr)
    {
        opts.compTailCallOpt = (UINT)_wtoi(strTailCallOpt) != 0;
    }

    if (JitConfig.TailCallLoopOpt() == 0)
    {
        opts.compTailCallLoopOpt = false;
    }
#endif

    opts.compScopeInfo = opts.compDbgInfo;

#ifdef LATE_DISASM
    codeGen->getDisAssembler().disOpenForLateDisAsm(info.compMethodName, info.compClassName,
                                                    info.compMethodInfo->args.pSig);
#endif

    //-------------------------------------------------------------------------

    opts.compReloc = jitFlags->IsSet(JitFlags::JIT_FLAG_RELOC);

#ifdef DEBUG
#if defined(_TARGET_XARCH_)
    // Whether encoding of absolute addr as PC-rel offset is enabled
    opts.compEnablePCRelAddr = (JitConfig.EnablePCRelAddr() != 0);
#endif
#endif // DEBUG

    opts.compProcedureSplitting = jitFlags->IsSet(JitFlags::JIT_FLAG_PROCSPLIT);

#ifdef _TARGET_ARM64_
    // TODO-ARM64-NYI: enable hot/cold splitting
    opts.compProcedureSplitting = false;
#endif // _TARGET_ARM64_

#ifdef DEBUG
    opts.compProcedureSplittingEH = opts.compProcedureSplitting;
#endif // DEBUG

    if (opts.compProcedureSplitting)
    {
        // Note that opts.compdbgCode is true under ngen for checked assemblies!
        opts.compProcedureSplitting = !opts.compDbgCode;

#ifdef DEBUG
        // JitForceProcedureSplitting is used to force procedure splitting on checked assemblies.
        // This is useful for debugging on a checked build.  Note that we still only do procedure
        // splitting in the zapper.
        if (JitConfig.JitForceProcedureSplitting().contains(info.compMethodName, info.compClassName,
                                                            &info.compMethodInfo->args))
        {
            opts.compProcedureSplitting = true;
        }

        // JitNoProcedureSplitting will always disable procedure splitting.
        if (JitConfig.JitNoProcedureSplitting().contains(info.compMethodName, info.compClassName,
                                                         &info.compMethodInfo->args))
        {
            opts.compProcedureSplitting = false;
        }
        //
        // JitNoProcedureSplittingEH will disable procedure splitting in functions with EH.
        if (JitConfig.JitNoProcedureSplittingEH().contains(info.compMethodName, info.compClassName,
                                                           &info.compMethodInfo->args))
        {
            opts.compProcedureSplittingEH = false;
        }
#endif
    }

    fgBlockCounts                = nullptr;
    fgProfileData_ILSizeMismatch = false;
    fgNumProfileRuns             = 0;
    if (jitFlags->IsSet(JitFlags::JIT_FLAG_BBOPT))
    {
        assert(!compIsForInlining());
        HRESULT hr;
        hr = info.compCompHnd->getMethodBlockCounts(info.compMethodHnd, &fgBlockCountsCount, &fgBlockCounts,
                                                    &fgNumProfileRuns);

        // a failed result that also has a non-NULL fgBlockCounts
        // indicates that the ILSize for the method no longer matches
        // the ILSize for the method when profile data was collected.
        //
        // We will discard the IBC data in this case
        //
        if (FAILED(hr) && (fgBlockCounts != nullptr))
        {
            fgProfileData_ILSizeMismatch = true;
            fgBlockCounts                = nullptr;
        }
#ifdef DEBUG
        // A successful result implies a non-NULL fgBlockCounts
        //
        if (SUCCEEDED(hr))
        {
            assert(fgBlockCounts != nullptr);
        }

        // A failed result implies a NULL fgBlockCounts
        //   see implementation of Compiler::fgHaveProfileData()
        //
        if (FAILED(hr))
        {
            assert(fgBlockCounts == nullptr);
        }
#endif
    }

#ifdef DEBUG
    // Now, set compMaxUncheckedOffsetForNullObject for STRESS_NULL_OBJECT_CHECK
    if (compStressCompile(STRESS_NULL_OBJECT_CHECK, 30))
    {
        compMaxUncheckedOffsetForNullObject = (size_t)JitConfig.JitMaxUncheckedOffset();
        if (verbose)
        {
            printf("STRESS_NULL_OBJECT_CHECK: compMaxUncheckedOffsetForNullObject=0x%X\n",
                   compMaxUncheckedOffsetForNullObject);
        }
    }

    if (verbose)
    {
        // If we are compiling for a specific tier, make that very obvious in the output.
        // Note that we don't expect multiple TIER flags to be set at one time, but there
        // is nothing preventing that.
        if (jitFlags->IsSet(JitFlags::JIT_FLAG_TIER0))
        {
            printf("OPTIONS: Tier-0 compilation (set COMPlus_TieredCompilation=0 to disable)\n");
        }
        if (jitFlags->IsSet(JitFlags::JIT_FLAG_TIER1))
        {
            printf("OPTIONS: Tier-1 compilation\n");
        }

        printf("OPTIONS: compCodeOpt = %s\n",
               (opts.compCodeOpt == BLENDED_CODE)
                   ? "BLENDED_CODE"
                   : (opts.compCodeOpt == SMALL_CODE) ? "SMALL_CODE"
                                                      : (opts.compCodeOpt == FAST_CODE) ? "FAST_CODE" : "UNKNOWN_CODE");

        printf("OPTIONS: compDbgCode = %s\n", dspBool(opts.compDbgCode));
        printf("OPTIONS: compDbgInfo = %s\n", dspBool(opts.compDbgInfo));
        printf("OPTIONS: compDbgEnC  = %s\n", dspBool(opts.compDbgEnC));
        printf("OPTIONS: compProcedureSplitting   = %s\n", dspBool(opts.compProcedureSplitting));
        printf("OPTIONS: compProcedureSplittingEH = %s\n", dspBool(opts.compProcedureSplittingEH));

        if (jitFlags->IsSet(JitFlags::JIT_FLAG_BBOPT) && fgHaveProfileData())
        {
            printf("OPTIONS: using real profile data\n");
        }

        if (fgProfileData_ILSizeMismatch)
        {
            printf("OPTIONS: discarded IBC profile data due to mismatch in ILSize\n");
        }

        if (jitFlags->IsSet(JitFlags::JIT_FLAG_PREJIT))
        {
            printf("OPTIONS: Jit invoked for ngen\n");
        }
    }
#endif

    opts.compGCPollType = GCPOLL_NONE;
    if (jitFlags->IsSet(JitFlags::JIT_FLAG_GCPOLL_CALLS))
    {
        opts.compGCPollType = GCPOLL_CALL;
    }
    else if (jitFlags->IsSet(JitFlags::JIT_FLAG_GCPOLL_INLINE))
    {
        // make sure that the EE didn't set both flags.
        assert(opts.compGCPollType == GCPOLL_NONE);
        opts.compGCPollType = GCPOLL_INLINE;
    }

#ifdef PROFILING_SUPPORTED
#ifdef UNIX_AMD64_ABI
    if (compIsProfilerHookNeeded())
    {
        opts.compNeedToAlignFrame = true;
    }
#endif // UNIX_AMD64_ABI
#endif

#if defined(DEBUG) && defined(_TARGET_ARM64_)
    if ((s_pJitMethodSet == nullptr) || s_pJitMethodSet->IsActiveMethod(info.compFullName, info.compMethodHash()))
    {
        opts.compJitSaveFpLrWithCalleeSavedRegisters = JitConfig.JitSaveFpLrWithCalleeSavedRegisters();
    }
#endif // defined(DEBUG) && defined(_TARGET_ARM64_)
}

#ifdef DEBUG

bool Compiler::compJitHaltMethod()
{
    /* This method returns true when we use an INS_BREAKPOINT to allow us to step into the generated native code */
    /* Note that this these two "Jit" environment variables also work for ngen images */

    if (JitConfig.JitHalt().contains(info.compMethodName, info.compClassName, &info.compMethodInfo->args))
    {
        return true;
    }

    /* Use this Hash variant when there are a lot of method with the same name and different signatures */

    unsigned fJitHashHaltVal = (unsigned)JitConfig.JitHashHalt();
    if ((fJitHashHaltVal != (unsigned)-1) && (fJitHashHaltVal == info.compMethodHash()))
    {
        return true;
    }

    return false;
}

/*****************************************************************************
 * Should we use a "stress-mode" for the given stressArea. We have different
 *   areas to allow the areas to be mixed in different combinations in
 *   different methods.
 * 'weight' indicates how often (as a percentage) the area should be stressed.
 *    It should reflect the usefulness:overhead ratio.
 */

const LPCWSTR Compiler::s_compStressModeNames[STRESS_COUNT + 1] = {
#define STRESS_MODE(mode) W("STRESS_") W(#mode),

    STRESS_MODES
#undef STRESS_MODE
};

bool Compiler::compStressCompile(compStressArea stressArea, unsigned weight)
{
    unsigned hash;
    DWORD    stressLevel;

    if (!bRangeAllowStress)
    {
        return false;
    }

    if (!JitConfig.JitStressOnly().isEmpty() &&
        !JitConfig.JitStressOnly().contains(info.compMethodName, info.compClassName, &info.compMethodInfo->args))
    {
        return false;
    }

    bool           doStress = false;
    const wchar_t* strStressModeNames;

    // Does user explicitly prevent using this STRESS_MODE through the command line?
    const wchar_t* strStressModeNamesNot = JitConfig.JitStressModeNamesNot();
    if ((strStressModeNamesNot != nullptr) &&
        (wcsstr(strStressModeNamesNot, s_compStressModeNames[stressArea]) != nullptr))
    {
        doStress = false;
        goto _done;
    }

    // Does user explicitly set this STRESS_MODE through the command line?
    strStressModeNames = JitConfig.JitStressModeNames();
    if (strStressModeNames != nullptr)
    {
        if (wcsstr(strStressModeNames, s_compStressModeNames[stressArea]) != nullptr)
        {
            doStress = true;
            goto _done;
        }

        // This stress mode name did not match anything in the stress
        // mode whitelist. If user has requested only enable mode,
        // don't allow this stress mode to turn on.
        const bool onlyEnableMode = JitConfig.JitStressModeNamesOnly() != 0;

        if (onlyEnableMode)
        {
            doStress = false;
            goto _done;
        }
    }

    // 0:   No stress (Except when explicitly set in complus_JitStressModeNames)
    // !=2: Vary stress. Performance will be slightly/moderately degraded
    // 2:   Check-all stress. Performance will be REALLY horrible
    stressLevel = getJitStressLevel();

    assert(weight <= MAX_STRESS_WEIGHT);

    /* Check for boundary conditions */

    if (stressLevel == 0 || weight == 0)
    {
        return false;
    }

    // Should we allow unlimited stress ?
    if (stressArea > STRESS_COUNT_VARN && stressLevel == 2)
    {
        return true;
    }

    if (weight == MAX_STRESS_WEIGHT)
    {
        doStress = true;
        goto _done;
    }

    // Get a hash which can be compared with 'weight'

    assert(stressArea != 0);
    hash = (info.compMethodHash() ^ stressArea ^ stressLevel) % MAX_STRESS_WEIGHT;

    assert(hash < MAX_STRESS_WEIGHT && weight <= MAX_STRESS_WEIGHT);
    doStress = (hash < weight);

_done:

    if (doStress && !compActiveStressModes[stressArea])
    {
        if (verbose)
        {
            printf("\n\n*** JitStress: %ws ***\n\n", s_compStressModeNames[stressArea]);
        }
        compActiveStressModes[stressArea] = 1;
    }

    return doStress;
}

#endif // DEBUG

void Compiler::compInitDebuggingInfo()
{
    assert(!compIsForInlining());

#ifdef DEBUG
    if (verbose)
    {
        printf("*************** In compInitDebuggingInfo() for %s\n", info.compFullName);
    }
#endif

    /*-------------------------------------------------------------------------
     *
     * Get hold of the local variable records, if there are any
     */

    info.compVarScopesCount = 0;

    if (opts.compScopeInfo)
    {
        eeGetVars();
    }

    compInitVarScopeMap();

    if (opts.compScopeInfo || opts.compDbgCode)
    {
        compInitScopeLists();
    }

    if (opts.compDbgCode && (info.compVarScopesCount > 0))
    {
        /* Create a new empty basic block. fgExtendDbgLifetimes() may add
           initialization of variables which are in scope right from the
           start of the (real) first BB (and therefore artificially marked
           as alive) into this block.
         */

        fgEnsureFirstBBisScratch();

        fgInsertStmtAtEnd(fgFirstBB, gtNewNothingNode());

        JITDUMP("Debuggable code - Add new %s to perform initialization of variables\n", fgFirstBB->dspToString());
    }

    /*-------------------------------------------------------------------------
     *
     * Read the stmt-offsets table and the line-number table
     */

    info.compStmtOffsetsImplicit = ICorDebugInfo::NO_BOUNDARIES;

    // We can only report debug info for EnC at places where the stack is empty.
    // Actually, at places where there are not live temps. Else, we won't be able
    // to map between the old and the new versions correctly as we won't have
    // any info for the live temps.

    assert(!opts.compDbgEnC || !opts.compDbgInfo ||
           0 == (info.compStmtOffsetsImplicit & ~ICorDebugInfo::STACK_EMPTY_BOUNDARIES));

    info.compStmtOffsetsCount = 0;

    if (opts.compDbgInfo)
    {
        /* Get hold of the line# records, if there are any */

        eeGetStmtOffsets();

#ifdef DEBUG
        if (verbose)
        {
            printf("info.compStmtOffsetsCount    = %d\n", info.compStmtOffsetsCount);
            printf("info.compStmtOffsetsImplicit = %04Xh", info.compStmtOffsetsImplicit);

            if (info.compStmtOffsetsImplicit)
            {
                printf(" ( ");
                if (info.compStmtOffsetsImplicit & ICorDebugInfo::STACK_EMPTY_BOUNDARIES)
                {
                    printf("STACK_EMPTY ");
                }
                if (info.compStmtOffsetsImplicit & ICorDebugInfo::NOP_BOUNDARIES)
                {
                    printf("NOP ");
                }
                if (info.compStmtOffsetsImplicit & ICorDebugInfo::CALL_SITE_BOUNDARIES)
                {
                    printf("CALL_SITE ");
                }
                printf(")");
            }
            printf("\n");
            IL_OFFSET* pOffs = info.compStmtOffsets;
            for (unsigned i = 0; i < info.compStmtOffsetsCount; i++, pOffs++)
            {
                printf("%02d) IL_%04Xh\n", i, *pOffs);
            }
        }
#endif
    }
}

void Compiler::compSetOptimizationLevel()
{
    bool theMinOptsValue;
#pragma warning(suppress : 4101)
    unsigned jitMinOpts;

    if (compIsForInlining())
    {
        theMinOptsValue = impInlineInfo->InlinerCompiler->opts.MinOpts();
        goto _SetMinOpts;
    }

    theMinOptsValue = false;

    if (opts.compFlags == CLFLG_MINOPT)
    {
        JITLOG((LL_INFO100, "CLFLG_MINOPT set for method %s\n", info.compFullName));
        theMinOptsValue = true;
    }

#ifdef DEBUG
    jitMinOpts = JitConfig.JitMinOpts();

    if (!theMinOptsValue && (jitMinOpts > 0))
    {
        // jitTotalMethodCompiled does not include the method that is being compiled now, so make +1.
        unsigned methodCount     = Compiler::jitTotalMethodCompiled + 1;
        unsigned methodCountMask = methodCount & 0xFFF;
        unsigned kind            = (jitMinOpts & 0xF000000) >> 24;
        switch (kind)
        {
            default:
                if (jitMinOpts <= methodCount)
                {
                    if (verbose)
                    {
                        printf(" Optimizations disabled by JitMinOpts and methodCount\n");
                    }
                    theMinOptsValue = true;
                }
                break;
            case 0xD:
            {
                unsigned firstMinopts  = (jitMinOpts >> 12) & 0xFFF;
                unsigned secondMinopts = (jitMinOpts >> 0) & 0xFFF;

                if ((firstMinopts == methodCountMask) || (secondMinopts == methodCountMask))
                {
                    if (verbose)
                    {
                        printf("0xD: Optimizations disabled by JitMinOpts and methodCountMask\n");
                    }
                    theMinOptsValue = true;
                }
            }
            break;
            case 0xE:
            {
                unsigned startMinopts = (jitMinOpts >> 12) & 0xFFF;
                unsigned endMinopts   = (jitMinOpts >> 0) & 0xFFF;

                if ((startMinopts <= methodCountMask) && (endMinopts >= methodCountMask))
                {
                    if (verbose)
                    {
                        printf("0xE: Optimizations disabled by JitMinOpts and methodCountMask\n");
                    }
                    theMinOptsValue = true;
                }
            }
            break;
            case 0xF:
            {
                unsigned bitsZero = (jitMinOpts >> 12) & 0xFFF;
                unsigned bitsOne  = (jitMinOpts >> 0) & 0xFFF;

                if (((methodCountMask & bitsOne) == bitsOne) && ((~methodCountMask & bitsZero) == bitsZero))
                {
                    if (verbose)
                    {
                        printf("0xF: Optimizations disabled by JitMinOpts and methodCountMask\n");
                    }
                    theMinOptsValue = true;
                }
            }
            break;
        }
    }

    if (!theMinOptsValue)
    {
        if (JitConfig.JitMinOptsName().contains(info.compMethodName, info.compClassName, &info.compMethodInfo->args))
        {
            theMinOptsValue = true;
        }
    }

#if 0
    // The code in this #if can be used to debug optimization issues according to method hash.
	// To use, uncomment, rebuild and set environment variables minoptshashlo and minoptshashhi.
#ifdef DEBUG
    unsigned methHash = info.compMethodHash();
    char* lostr = getenv("minoptshashlo");
    unsigned methHashLo = 0;
	if (lostr != nullptr)
	{
		sscanf_s(lostr, "%x", &methHashLo);
		char* histr = getenv("minoptshashhi");
		unsigned methHashHi = UINT32_MAX;
		if (histr != nullptr)
		{
			sscanf_s(histr, "%x", &methHashHi);
			if (methHash >= methHashLo && methHash <= methHashHi)
			{
				printf("MinOpts for method %s, hash = 0x%x.\n",
					info.compFullName, info.compMethodHash());
				printf("");         // in our logic this causes a flush
				theMinOptsValue = true;
			}
		}
	}
#endif
#endif

    if (compStressCompile(STRESS_MIN_OPTS, 5))
    {
        theMinOptsValue = true;
    }
    // For PREJIT we never drop down to MinOpts
    // unless unless CLFLG_MINOPT is set
    else if (!opts.jitFlags->IsSet(JitFlags::JIT_FLAG_PREJIT))
    {
        if ((unsigned)JitConfig.JitMinOptsCodeSize() < info.compILCodeSize)
        {
            JITLOG((LL_INFO10, "IL Code Size exceeded, using MinOpts for method %s\n", info.compFullName));
            theMinOptsValue = true;
        }
        else if ((unsigned)JitConfig.JitMinOptsInstrCount() < opts.instrCount)
        {
            JITLOG((LL_INFO10, "IL instruction count exceeded, using MinOpts for method %s\n", info.compFullName));
            theMinOptsValue = true;
        }
        else if ((unsigned)JitConfig.JitMinOptsBbCount() < fgBBcount)
        {
            JITLOG((LL_INFO10, "Basic Block count exceeded, using MinOpts for method %s\n", info.compFullName));
            theMinOptsValue = true;
        }
        else if ((unsigned)JitConfig.JitMinOptsLvNumCount() < lvaCount)
        {
            JITLOG((LL_INFO10, "Local Variable Num count exceeded, using MinOpts for method %s\n", info.compFullName));
            theMinOptsValue = true;
        }
        else if ((unsigned)JitConfig.JitMinOptsLvRefCount() < opts.lvRefCount)
        {
            JITLOG((LL_INFO10, "Local Variable Ref count exceeded, using MinOpts for method %s\n", info.compFullName));
            theMinOptsValue = true;
        }
        if (theMinOptsValue == true)
        {
            JITLOG((LL_INFO10000, "IL Code Size,Instr %4d,%4d, Basic Block count %3d, Local Variable Num,Ref count "
                                  "%3d,%3d for method %s\n",
                    info.compILCodeSize, opts.instrCount, fgBBcount, lvaCount, opts.lvRefCount, info.compFullName));
            if (JitConfig.JitBreakOnMinOpts() != 0)
            {
                assert(!"MinOpts enabled");
            }
        }
    }
#else  // !DEBUG
    // Retail check if we should force Minopts due to the complexity of the method
    // For PREJIT we never drop down to MinOpts
    // unless unless CLFLG_MINOPT is set
    if (!theMinOptsValue && !opts.jitFlags->IsSet(JitFlags::JIT_FLAG_PREJIT) &&
        ((DEFAULT_MIN_OPTS_CODE_SIZE < info.compILCodeSize) || (DEFAULT_MIN_OPTS_INSTR_COUNT < opts.instrCount) ||
         (DEFAULT_MIN_OPTS_BB_COUNT < fgBBcount) || (DEFAULT_MIN_OPTS_LV_NUM_COUNT < lvaCount) ||
         (DEFAULT_MIN_OPTS_LV_REF_COUNT < opts.lvRefCount)))
    {
        theMinOptsValue = true;
    }
#endif // DEBUG

    JITLOG((LL_INFO10000,
            "IL Code Size,Instr %4d,%4d, Basic Block count %3d, Local Variable Num,Ref count %3d,%3d for method %s\n",
            info.compILCodeSize, opts.instrCount, fgBBcount, lvaCount, opts.lvRefCount, info.compFullName));

#if 0
    // The code in this #if has been useful in debugging loop cloning issues, by
    // enabling selective enablement of the loop cloning optimization according to
    // method hash.
#ifdef DEBUG
    if (!theMinOptsValue)
    {
    unsigned methHash = info.compMethodHash();
    char* lostr = getenv("opthashlo");
    unsigned methHashLo = 0;
    if (lostr != NULL)
    {
        sscanf_s(lostr, "%x", &methHashLo);
        // methHashLo = (unsigned(atoi(lostr)) << 2);  // So we don't have to use negative numbers.
    }
    char* histr = getenv("opthashhi");
    unsigned methHashHi = UINT32_MAX;
    if (histr != NULL)
    {
        sscanf_s(histr, "%x", &methHashHi);
        // methHashHi = (unsigned(atoi(histr)) << 2);  // So we don't have to use negative numbers.
    }
    if (methHash < methHashLo || methHash > methHashHi)
    {
        theMinOptsValue = true;
    }
    else
    {
        printf("Doing optimization in  in %s (0x%x).\n", info.compFullName, methHash);
    }
    }
#endif
#endif

_SetMinOpts:

    // Set the MinOpts value
    opts.SetMinOpts(theMinOptsValue);

#ifdef DEBUG
    if (verbose && !compIsForInlining())
    {
        printf("OPTIONS: opts.MinOpts() == %s\n", opts.MinOpts() ? "true" : "false");
    }
#endif

    /* Control the optimizations */

    if (opts.OptimizationDisabled())
    {
        opts.compFlags &= ~CLFLG_MAXOPT;
        opts.compFlags |= CLFLG_MINOPT;
    }

    if (!compIsForInlining())
    {
        codeGen->setFramePointerRequired(false);
        codeGen->setFrameRequired(false);

        if (opts.OptimizationDisabled())
        {
            codeGen->setFrameRequired(true);
        }

#if !defined(_TARGET_AMD64_)
        // The VM sets JitFlags::JIT_FLAG_FRAMED for two reasons: (1) the COMPlus_JitFramed variable is set, or
        // (2) the function is marked "noinline". The reason for #2 is that people mark functions
        // noinline to ensure the show up on in a stack walk. But for AMD64, we don't need a frame
        // pointer for the frame to show up in stack walk.
        if (opts.jitFlags->IsSet(JitFlags::JIT_FLAG_FRAMED))
            codeGen->setFrameRequired(true);
#endif

        if (opts.jitFlags->IsSet(JitFlags::JIT_FLAG_RELOC))
        {
            codeGen->genAlignLoops = false; // loop alignment not supported for prejitted code

            // The zapper doesn't set JitFlags::JIT_FLAG_ALIGN_LOOPS, and there is
            // no reason for it to set it as the JIT doesn't currently support loop alignment
            // for prejitted images. (The JIT doesn't know the final address of the code, hence
            // it can't align code based on unknown addresses.)
            assert(!opts.jitFlags->IsSet(JitFlags::JIT_FLAG_ALIGN_LOOPS));
        }
        else
        {
            codeGen->genAlignLoops = opts.jitFlags->IsSet(JitFlags::JIT_FLAG_ALIGN_LOOPS);
        }
    }

    info.compUnwrapContextful = opts.OptimizationEnabled();

    fgCanRelocateEHRegions = true;
}

#ifdef _TARGET_ARMARCH_
// Function compRsvdRegCheck:
//  given a curState to use for calculating the total frame size
//  it will return true if the REG_OPT_RSVD should be reserved so
//  that it can be use to form large offsets when accessing stack
//  based LclVar including both incoming and out going argument areas.
//
//  The method advances the frame layout state to curState by calling
//  lvaFrameSize(curState).
//
bool Compiler::compRsvdRegCheck(FrameLayoutState curState)
{
    // Always do the layout even if returning early. Callers might
    // depend on us to do the layout.
    unsigned frameSize = lvaFrameSize(curState);
    JITDUMP("\n"
            "compRsvdRegCheck\n"
            "  frame size  = %6d\n"
            "  compArgSize = %6d\n",
            frameSize, compArgSize);

    if (opts.MinOpts())
    {
        // Have a recovery path in case we fail to reserve REG_OPT_RSVD and go
        // over the limit of SP and FP offset ranges due to large
        // temps.
        JITDUMP(" Returning true (MinOpts)\n\n");
        return true;
    }

    unsigned calleeSavedRegMaxSz = CALLEE_SAVED_REG_MAXSZ;
    if (compFloatingPointUsed)
    {
        calleeSavedRegMaxSz += CALLEE_SAVED_FLOAT_MAXSZ;
    }
    calleeSavedRegMaxSz += REGSIZE_BYTES; // we always push LR.  See genPushCalleeSavedRegisters

    noway_assert(frameSize >= calleeSavedRegMaxSz);

#if defined(_TARGET_ARM64_)

    // TODO-ARM64-CQ: update this!
    JITDUMP(" Returning true (ARM64)\n\n");
    return true; // just always assume we'll need it, for now

#else  // _TARGET_ARM_

    // frame layout:
    //
    //         ... high addresses ...
    //                         frame contents       size
    //                         -------------------  ------------------------
    //                         inArgs               compArgSize (includes prespill)
    //  caller SP --->
    //                         prespill
    //                         LR                   REGSIZE_BYTES
    //  R11    --->            R11                  REGSIZE_BYTES
    //                         callee saved regs    CALLEE_SAVED_REG_MAXSZ   (32 bytes)
    //                     optional saved fp regs   CALLEE_SAVED_FLOAT_MAXSZ (64 bytes)
    //                         lclSize
    //                             incl. TEMPS      MAX_SPILL_TEMP_SIZE
    //                             incl. outArgs
    //  SP     --->
    //          ... low addresses ...
    //
    // When codeGen->isFramePointerRequired is true, R11 will be established as a frame pointer.
    // We can then use R11 to access incoming args with positive offsets, and LclVars with
    // negative offsets.
    //
    // In functions with EH, in the non-funclet (or main) region, even though we will have a
    // frame pointer, we can use SP with positive offsets to access any or all locals or arguments
    // that we can reach with SP-relative encodings. The funclet region might require the reserved
    // register, since it must use offsets from R11 to access the parent frame.

    unsigned maxR11PositiveEncodingOffset = compFloatingPointUsed ? 0x03FC : 0x0FFF;
    JITDUMP("  maxR11PositiveEncodingOffset     = %6d\n", maxR11PositiveEncodingOffset);

    // Floating point load/store instructions (VLDR/VSTR) can address up to -0x3FC from R11, but we
    // don't know if there are either no integer locals, or if we don't need large negative offsets
    // for the integer locals, so we must use the integer max negative offset, which is a
    // smaller (absolute value) number.
    unsigned maxR11NegativeEncodingOffset = 0x00FF; // This is a negative offset from R11.
    JITDUMP("  maxR11NegativeEncodingOffset     = %6d\n", maxR11NegativeEncodingOffset);

    // -1 because otherwise we are computing the address just beyond the last argument, which we don't need to do.
    unsigned maxR11PositiveOffset = compArgSize + (2 * REGSIZE_BYTES) - 1;
    JITDUMP("  maxR11PositiveOffset             = %6d\n", maxR11PositiveOffset);

    // The value is positive, but represents a negative offset from R11.
    // frameSize includes callee-saved space for R11 and LR, which are at non-negative offsets from R11
    // (+0 and +4, respectively), so don't include those in the max possible negative offset.
    assert(frameSize >= (2 * REGSIZE_BYTES));
    unsigned maxR11NegativeOffset = frameSize - (2 * REGSIZE_BYTES);
    JITDUMP("  maxR11NegativeOffset             = %6d\n", maxR11NegativeOffset);

    if (codeGen->isFramePointerRequired())
    {
        if (maxR11NegativeOffset > maxR11NegativeEncodingOffset)
        {
            JITDUMP(" Returning true (frame required and maxR11NegativeOffset)\n\n");
            return true;
        }
        if (maxR11PositiveOffset > maxR11PositiveEncodingOffset)
        {
            JITDUMP(" Returning true (frame required and maxR11PositiveOffset)\n\n");
            return true;
        }
    }

    // Now consider the SP based frame case. Note that we will use SP based offsets to access the stack in R11 based
    // frames in the non-funclet main code area.

    unsigned maxSPPositiveEncodingOffset = compFloatingPointUsed ? 0x03FC : 0x0FFF;
    JITDUMP("  maxSPPositiveEncodingOffset      = %6d\n", maxSPPositiveEncodingOffset);

    // -1 because otherwise we are computing the address just beyond the last argument, which we don't need to do.
    assert(compArgSize + frameSize > 0);
    unsigned maxSPPositiveOffset = compArgSize + frameSize - 1;

    if (codeGen->isFramePointerUsed())
    {
        // We have a frame pointer, so we can use it to access part of the stack, even if SP can't reach those parts.
        // We will still generate SP-relative offsets if SP can reach.

        // First, check that the stack between R11 and SP can be fully reached, either via negative offset from FP
        // or positive offset from SP. Don't count stored R11 or LR, which are reached from positive offsets from FP.

        unsigned maxSPLocalsCombinedOffset = frameSize - (2 * REGSIZE_BYTES) - 1;
        JITDUMP("  maxSPLocalsCombinedOffset        = %6d\n", maxSPLocalsCombinedOffset);

        if (maxSPLocalsCombinedOffset > maxSPPositiveEncodingOffset)
        {
            // Can R11 help?
            unsigned maxRemainingLocalsCombinedOffset = maxSPLocalsCombinedOffset - maxSPPositiveEncodingOffset;
            JITDUMP("  maxRemainingLocalsCombinedOffset = %6d\n", maxRemainingLocalsCombinedOffset);

            if (maxRemainingLocalsCombinedOffset > maxR11NegativeEncodingOffset)
            {
                JITDUMP(" Returning true (frame pointer exists; R11 and SP can't reach entire stack between them)\n\n");
                return true;
            }

            // Otherwise, yes, we can address the remaining parts of the locals frame with negative offsets from R11.
        }

        // Check whether either R11 or SP can access the arguments.
        if ((maxR11PositiveOffset > maxR11PositiveEncodingOffset) &&
            (maxSPPositiveOffset > maxSPPositiveEncodingOffset))
        {
            JITDUMP(" Returning true (frame pointer exists; R11 and SP can't reach all arguments)\n\n");
            return true;
        }
    }
    else
    {
        if (maxSPPositiveOffset > maxSPPositiveEncodingOffset)
        {
            JITDUMP(" Returning true (no frame pointer exists; SP can't reach all of frame)\n\n");
            return true;
        }
    }

    // We won't need to reserve REG_OPT_RSVD.
    //
    JITDUMP(" Returning false\n\n");
    return false;
#endif // _TARGET_ARM_
}
#endif // _TARGET_ARMARCH_

void Compiler::compFunctionTraceStart()
{
#ifdef DEBUG
    if (compIsForInlining())
    {
        return;
    }

    if ((JitConfig.JitFunctionTrace() != 0) && !opts.disDiffable)
    {
        LONG newJitNestingLevel = InterlockedIncrement(&Compiler::jitNestingLevel);
        if (newJitNestingLevel <= 0)
        {
            printf("{ Illegal nesting level %d }\n", newJitNestingLevel);
        }

        for (LONG i = 0; i < newJitNestingLevel - 1; i++)
        {
            printf("  ");
        }
        printf("{ Start Jitting %s (MethodHash=%08x)\n", info.compFullName,
               info.compMethodHash()); /* } editor brace matching workaround for this printf */
    }
#endif // DEBUG
}

void Compiler::compFunctionTraceEnd(void* methodCodePtr, ULONG methodCodeSize, bool isNYI)
{
#ifdef DEBUG
    assert(!compIsForInlining());

    if ((JitConfig.JitFunctionTrace() != 0) && !opts.disDiffable)
    {
        LONG newJitNestingLevel = InterlockedDecrement(&Compiler::jitNestingLevel);
        if (newJitNestingLevel < 0)
        {
            printf("{ Illegal nesting level %d }\n", newJitNestingLevel);
        }

        for (LONG i = 0; i < newJitNestingLevel; i++)
        {
            printf("  ");
        }
        /* { editor brace-matching workaround for following printf */
        printf("} Jitted Entry %03x at" FMT_ADDR "method %s size %08x%s%s\n", Compiler::jitTotalMethodCompiled,
               DBG_ADDR(methodCodePtr), info.compFullName, methodCodeSize,
               isNYI ? " NYI" : (compIsForImportOnly() ? " import only" : ""), opts.altJit ? " altjit" : "");
    }
#endif // DEBUG
}

//*********************************************************************************************
// #Phases
//
// This is the most interesting 'toplevel' function in the JIT.  It goes through the operations of
// importing, morphing, optimizations and code generation.  This is called from the EE through the
// code:CILJit::compileMethod function.
//
// For an overview of the structure of the JIT, see:
//   https://github.com/dotnet/coreclr/blob/master/Documentation/botr/ryujit-overview.md
//
void Compiler::compCompile(void** methodCodePtr, ULONG* methodCodeSize, JitFlags* compileFlags)
{
    if (compIsForInlining())
    {
        // Notify root instance that an inline attempt is about to import IL
        impInlineRoot()->m_inlineStrategy->NoteImport();
    }

    hashBv::Init(this);

    VarSetOps::AssignAllowUninitRhs(this, compCurLife, VarSetOps::UninitVal());

    /* The temp holding the secret stub argument is used by fgImport() when importing the intrinsic. */

    if (info.compPublishStubParam)
    {
        assert(lvaStubArgumentVar == BAD_VAR_NUM);
        lvaStubArgumentVar                  = lvaGrabTempWithImplicitUse(false DEBUGARG("stub argument"));
        lvaTable[lvaStubArgumentVar].lvType = TYP_I_IMPL;
    }

    EndPhase(PHASE_PRE_IMPORT);

    compFunctionTraceStart();

    /* Convert the instrs in each basic block to a tree based intermediate representation */

    fgImport();

    assert(!fgComputePredsDone);
    if (fgCheapPredsValid)
    {
        // Remove cheap predecessors before inlining and fat call transformation;
        // allowing the cheap predecessor lists to be inserted causes problems
        // with splitting existing blocks.
        fgRemovePreds();
    }

    // Transform indirect calls that require control flow expansion.
    fgTransformIndirectCalls();

    EndPhase(PHASE_IMPORTATION);

    if (compIsForInlining())
    {
        /* Quit inlining if fgImport() failed for any reason. */

        if (!compDonotInline())
        {
            /* Filter out unimported BBs */

            fgRemoveEmptyBlocks();

            // Update type of return spill temp if we have gathered
            // better info when importing the inlinee, and the return
            // spill temp is single def.
            if (fgNeedReturnSpillTemp())
            {
                CORINFO_CLASS_HANDLE retExprClassHnd = impInlineInfo->retExprClassHnd;
                if (retExprClassHnd != nullptr)
                {
                    LclVarDsc* returnSpillVarDsc = lvaGetDesc(lvaInlineeReturnSpillTemp);

                    if (returnSpillVarDsc->lvSingleDef)
                    {
                        lvaUpdateClass(lvaInlineeReturnSpillTemp, retExprClassHnd,
                                       impInlineInfo->retExprClassHndIsExact);
                    }
                }
            }
        }

        EndPhase(PHASE_POST_IMPORT);

#ifdef FEATURE_JIT_METHOD_PERF
        if (pCompJitTimer != nullptr)
        {
#if MEASURE_CLRAPI_CALLS
            EndPhase(PHASE_CLR_API);
#endif
            pCompJitTimer->Terminate(this, CompTimeSummaryInfo::s_compTimeSummary, false);
        }
#endif

        return;
    }

    assert(!compDonotInline());

    // Maybe the caller was not interested in generating code
    if (compIsForImportOnly())
    {
        compFunctionTraceEnd(nullptr, 0, false);
        return;
    }

#if !FEATURE_EH
    // If we aren't yet supporting EH in a compiler bring-up, remove as many EH handlers as possible, so
    // we can pass tests that contain try/catch EH, but don't actually throw any exceptions.
    fgRemoveEH();
#endif // !FEATURE_EH

    if (compileFlags->IsSet(JitFlags::JIT_FLAG_BBINSTR))
    {
        fgInstrumentMethod();
    }

    // We could allow ESP frames. Just need to reserve space for
    // pushing EBP if the method becomes an EBP-frame after an edit.
    // Note that requiring a EBP Frame disallows double alignment.  Thus if we change this
    // we either have to disallow double alignment for E&C some other way or handle it in EETwain.

    if (opts.compDbgEnC)
    {
        codeGen->setFramePointerRequired(true);

        // Since we need a slots for security near ebp, its not possible
        // to do this after an Edit without shifting all the locals.
        // So we just always reserve space for these slots in case an Edit adds them
        opts.compNeedSecurityCheck = true;

        // We don't care about localloc right now. If we do support it,
        // EECodeManager::FixContextForEnC() needs to handle it smartly
        // in case the localloc was actually executed.
        //
        // compLocallocUsed            = true;
    }

    EndPhase(PHASE_POST_IMPORT);

    /* Initialize the BlockSet epoch */

    NewBasicBlockEpoch();

    /* Massage the trees so that we can generate code out of them */

    fgMorph();
    EndPhase(PHASE_MORPH_END);

    /* GS security checks for unsafe buffers */
    if (getNeedsGSSecurityCookie())
    {
#ifdef DEBUG
        if (verbose)
        {
            printf("\n*************** -GS checks for unsafe buffers \n");
        }
#endif

        gsGSChecksInitCookie();

        if (compGSReorderStackLayout)
        {
            gsCopyShadowParams();
        }

#ifdef DEBUG
        if (verbose)
        {
            fgDispBasicBlocks(true);
            printf("\n");
        }
#endif
    }
    EndPhase(PHASE_GS_COOKIE);

    /* Compute bbNum, bbRefs and bbPreds */

    JITDUMP("\nRenumbering the basic blocks for fgComputePred\n");
    fgRenumberBlocks();

    noway_assert(!fgComputePredsDone); // This is the first time full (not cheap) preds will be computed.
    fgComputePreds();
    EndPhase(PHASE_COMPUTE_PREDS);

    /* If we need to emit GC Poll calls, mark the blocks that need them now.  This is conservative and can
     * be optimized later. */
    fgMarkGCPollBlocks();
    EndPhase(PHASE_MARK_GC_POLL_BLOCKS);

    /* From this point on the flowgraph information such as bbNum,
     * bbRefs or bbPreds has to be kept updated */

    // Compute the block and edge weights
    fgComputeBlockAndEdgeWeights();
    EndPhase(PHASE_COMPUTE_EDGE_WEIGHTS);

#if FEATURE_EH_FUNCLETS

    /* Create funclets from the EH handlers. */

    fgCreateFunclets();
    EndPhase(PHASE_CREATE_FUNCLETS);

#endif // FEATURE_EH_FUNCLETS

    if (opts.OptimizationEnabled())
    {
        optOptimizeLayout();
        EndPhase(PHASE_OPTIMIZE_LAYOUT);

        // Compute reachability sets and dominators.
        fgComputeReachability();
        EndPhase(PHASE_COMPUTE_REACHABILITY);
    }

    if (opts.OptimizationEnabled())
    {
        /*  Perform loop inversion (i.e. transform "while" loops into
            "repeat" loops) and discover and classify natural loops
            (e.g. mark iterative loops as such). Also marks loop blocks
            and sets bbWeight to the loop nesting levels
        */

        optOptimizeLoops();
        EndPhase(PHASE_OPTIMIZE_LOOPS);

        // Clone loops with optimization opportunities, and
        // choose the one based on dynamic condition evaluation.
        optCloneLoops();
        EndPhase(PHASE_CLONE_LOOPS);

        /* Unroll loops */
        optUnrollLoops();
        EndPhase(PHASE_UNROLL_LOOPS);
    }

#ifdef DEBUG
    fgDebugCheckLinks();
#endif

    /* Create the variable table (and compute variable ref counts) */

    lvaMarkLocalVars();
    EndPhase(PHASE_MARK_LOCAL_VARS);

    // IMPORTANT, after this point, every place where trees are modified or cloned
    // the local variable reference counts must be updated
    // You can test the value of the following variable to see if
    // the local variable ref counts must be updated
    //
    assert(lvaLocalVarRefCounted());

    if (opts.OptimizationEnabled())
    {
        /* Optimize boolean conditions */

        optOptimizeBools();
        EndPhase(PHASE_OPTIMIZE_BOOLS);

        // optOptimizeBools() might have changed the number of blocks; the dominators/reachability might be bad.
    }

    /* Figure out the order in which operators are to be evaluated */
    fgFindOperOrder();
    EndPhase(PHASE_FIND_OPER_ORDER);

    // Weave the tree lists. Anyone who modifies the tree shapes after
    // this point is responsible for calling fgSetStmtSeq() to keep the
    // nodes properly linked.
    // This can create GC poll calls, and create new BasicBlocks (without updating dominators/reachability).
    fgSetBlockOrder();
    EndPhase(PHASE_SET_BLOCK_ORDER);

    // IMPORTANT, after this point, every place where tree topology changes must redo evaluation
    // order (gtSetStmtInfo) and relink nodes (fgSetStmtSeq) if required.
    CLANG_FORMAT_COMMENT_ANCHOR;

#ifdef DEBUG
    // Now  we have determined the order of evaluation and the gtCosts for every node.
    // If verbose, dump the full set of trees here before the optimization phases mutate them
    //
    if (verbose)
    {
        fgDispBasicBlocks(true); // 'true' will call fgDumpTrees() after dumping the BasicBlocks
        printf("\n");
    }
#endif

    // At this point we know if we are fully interruptible or not
    if (opts.OptimizationEnabled())
    {
        bool doSsa           = true;
        bool doEarlyProp     = true;
        bool doValueNum      = true;
        bool doLoopHoisting  = true;
        bool doCopyProp      = true;
        bool doAssertionProp = true;
        bool doRangeAnalysis = true;
        int  iterations      = 1;

#if defined(OPT_CONFIG)
        doSsa           = (JitConfig.JitDoSsa() != 0);
        doEarlyProp     = doSsa && (JitConfig.JitDoEarlyProp() != 0);
        doValueNum      = doSsa && (JitConfig.JitDoValueNumber() != 0);
        doLoopHoisting  = doValueNum && (JitConfig.JitDoLoopHoisting() != 0);
        doCopyProp      = doValueNum && (JitConfig.JitDoCopyProp() != 0);
        doAssertionProp = doValueNum && (JitConfig.JitDoAssertionProp() != 0);
        doRangeAnalysis = doAssertionProp && (JitConfig.JitDoRangeAnalysis() != 0);

        if (opts.optRepeat)
        {
            iterations = JitConfig.JitOptRepeatCount();
        }
#endif // defined(OPT_CONFIG)

        while (iterations > 0)
        {
            if (doSsa)
            {
                fgSsaBuild();
                EndPhase(PHASE_BUILD_SSA);
            }

            if (doEarlyProp)
            {
                /* Propagate array length and rewrite getType() method call */
                optEarlyProp();
                EndPhase(PHASE_EARLY_PROP);
            }

            if (doValueNum)
            {
                fgValueNumber();
                EndPhase(PHASE_VALUE_NUMBER);
            }

            if (doLoopHoisting)
            {
                /* Hoist invariant code out of loops */
                optHoistLoopCode();
                EndPhase(PHASE_HOIST_LOOP_CODE);
            }

            if (doCopyProp)
            {
                /* Perform VN based copy propagation */
                optVnCopyProp();
                EndPhase(PHASE_VN_COPY_PROP);
            }

#if FEATURE_ANYCSE
            /* Remove common sub-expressions */
            optOptimizeCSEs();
#endif // FEATURE_ANYCSE

#if ASSERTION_PROP
            if (doAssertionProp)
            {
                /* Assertion propagation */
                optAssertionPropMain();
                EndPhase(PHASE_ASSERTION_PROP_MAIN);
            }

            if (doRangeAnalysis)
            {
                /* Optimize array index range checks */
                RangeCheck rc(this);
                rc.OptimizeRangeChecks();
                EndPhase(PHASE_OPTIMIZE_INDEX_CHECKS);
            }
#endif // ASSERTION_PROP

            /* update the flowgraph if we modified it during the optimization phase*/
            if (fgModified)
            {
                fgUpdateFlowGraph();
                EndPhase(PHASE_UPDATE_FLOW_GRAPH);

                // Recompute the edge weight if we have modified the flow graph
                fgComputeEdgeWeights();
                EndPhase(PHASE_COMPUTE_EDGE_WEIGHTS2);
            }

            // Iterate if requested, resetting annotations first.
            if (--iterations == 0)
            {
                break;
            }
            ResetOptAnnotations();
            RecomputeLoopInfo();
        }
    }

#ifdef _TARGET_AMD64_
    //  Check if we need to add the Quirk for the PPP backward compat issue
    compQuirkForPPPflag = compQuirkForPPP();
#endif

    fgDetermineFirstColdBlock();
    EndPhase(PHASE_DETERMINE_FIRST_COLD_BLOCK);

#ifdef DEBUG
    fgDebugCheckLinks(compStressCompile(STRESS_REMORPH_TREES, 50));

    // Stash the current estimate of the function's size if necessary.
    if (verbose)
    {
        compSizeEstimate  = 0;
        compCycleEstimate = 0;
        for (BasicBlock* block = fgFirstBB; block != nullptr; block = block->bbNext)
        {
            for (GenTreeStmt* stmt = block->firstStmt(); stmt != nullptr; stmt = stmt->getNextStmt())
            {
                compSizeEstimate += stmt->GetCostSz();
                compCycleEstimate += stmt->GetCostEx();
            }
        }
    }
#endif

    // rationalize trees
    Rationalizer rat(this); // PHASE_RATIONALIZE
    rat.Run();

    // Here we do "simple lowering".  When the RyuJIT backend works for all
    // platforms, this will be part of the more general lowering phase.  For now, though, we do a separate
    // pass of "final lowering."  We must do this before (final) liveness analysis, because this creates
    // range check throw blocks, in which the liveness must be correct.
    fgSimpleLowering();
    EndPhase(PHASE_SIMPLE_LOWERING);

#ifdef DEBUG
    fgDebugCheckBBlist();
    fgDebugCheckLinks();
#endif

    /* Enable this to gather statistical data such as
     * call and register argument info, flowgraph and loop info, etc. */

    compJitStats();

#ifdef _TARGET_ARM_
    if (compLocallocUsed)
    {
        // We reserve REG_SAVED_LOCALLOC_SP to store SP on entry for stack unwinding
        codeGen->regSet.rsMaskResvd |= RBM_SAVED_LOCALLOC_SP;
    }
#endif // _TARGET_ARM_

    /* Assign registers to variables, etc. */

    ///////////////////////////////////////////////////////////////////////////////
    // Dominator and reachability sets are no longer valid. They haven't been
    // maintained up to here, and shouldn't be used (unless recomputed).
    ///////////////////////////////////////////////////////////////////////////////
    fgDomsComputed = false;

    /* Create LSRA before Lowering, this way Lowering can initialize the TreeNode Map */
    m_pLinearScan = getLinearScanAllocator(this);

    /* Lower */
    m_pLowering = new (this, CMK_LSRA) Lowering(this, m_pLinearScan); // PHASE_LOWERING
    m_pLowering->Run();

    StackLevelSetter stackLevelSetter(this); // PHASE_STACK_LEVEL_SETTER
    stackLevelSetter.Run();

    lvaTrackedFixed = true; // We can not add any new tracked variables after this point.

    /* Now that lowering is completed we can proceed to perform register allocation */
    m_pLinearScan->doLinearScan();
    EndPhase(PHASE_LINEAR_SCAN);

    // Copied from rpPredictRegUse()
    genFullPtrRegMap = (codeGen->genInterruptible || !codeGen->isFramePointerUsed());

#ifdef DEBUG
    fgDebugCheckLinks();
#endif

    /* Generate code */

    codeGen->genGenerateCode(methodCodePtr, methodCodeSize);

#ifdef FEATURE_JIT_METHOD_PERF
    if (pCompJitTimer)
    {
#if MEASURE_CLRAPI_CALLS
        EndPhase(PHASE_CLR_API);
#endif
        pCompJitTimer->Terminate(this, CompTimeSummaryInfo::s_compTimeSummary, true);
    }
#endif

    RecordStateAtEndOfCompilation();

#ifdef FEATURE_TRACELOGGING
    compJitTelemetry.NotifyEndOfCompilation();
#endif

#if defined(DEBUG)
    ++Compiler::jitTotalMethodCompiled;
#endif // defined(DEBUG)

    compFunctionTraceEnd(*methodCodePtr, *methodCodeSize, false);
    JITDUMP("Method code size: %d\n", (unsigned)(*methodCodeSize));

#if FUNC_INFO_LOGGING
    if (compJitFuncInfoFile != nullptr)
    {
        assert(!compIsForInlining());
#ifdef DEBUG // We only have access to info.compFullName in DEBUG builds.
        fprintf(compJitFuncInfoFile, "%s\n", info.compFullName);
#elif FEATURE_SIMD
        fprintf(compJitFuncInfoFile, " %s\n", eeGetMethodFullName(info.compMethodHnd));
#endif
        fprintf(compJitFuncInfoFile, ""); // in our logic this causes a flush
    }
#endif // FUNC_INFO_LOGGING
}

//------------------------------------------------------------------------
// ResetOptAnnotations: Clear annotations produced during global optimizations.
//
// Notes:
//    The intent of this method is to clear any information typically assumed
//    to be set only once; it is used between iterations when JitOptRepeat is
//    in effect.

void Compiler::ResetOptAnnotations()
{
    assert(opts.optRepeat);
    assert(JitConfig.JitOptRepeatCount() > 0);
    fgResetForSsa();
    vnStore               = nullptr;
    m_opAsgnVarDefSsaNums = nullptr;
    m_blockToEHPreds      = nullptr;
    fgSsaPassesCompleted  = 0;
    fgVNPassesCompleted   = 0;

    for (BasicBlock* block = fgFirstBB; block != nullptr; block = block->bbNext)
    {
        for (GenTreeStmt* stmt = block->firstStmt(); stmt != nullptr; stmt = stmt->getNextStmt())
        {
            stmt->gtFlags &= ~GTF_STMT_HAS_CSE;

            for (GenTree* tree = stmt->gtStmtList; tree != nullptr; tree = tree->gtNext)
            {
                tree->ClearVN();
                tree->ClearAssertion();
                tree->gtCSEnum = NO_CSE;
            }
        }
    }
}

//------------------------------------------------------------------------
// RecomputeLoopInfo: Recompute loop annotations between opt-repeat iterations.
//
// Notes:
//    The intent of this method is to update loop structure annotations, and those
//    they depend on; these annotations may have become stale during optimization,
//    and need to be up-to-date before running another iteration of optimizations.

void Compiler::RecomputeLoopInfo()
{
    assert(opts.optRepeat);
    assert(JitConfig.JitOptRepeatCount() > 0);
    // Recompute reachability sets, dominators, and loops.
    optLoopCount   = 0;
    fgDomsComputed = false;
    for (BasicBlock* block = fgFirstBB; block != nullptr; block = block->bbNext)
    {
        block->bbFlags &= ~BBF_LOOP_FLAGS;
    }
    fgComputeReachability();
    // Rebuild the loop tree annotations themselves.  Since this is performed as
    // part of 'optOptimizeLoops', this will also re-perform loop rotation, but
    // not other optimizations, as the others are not part of 'optOptimizeLoops'.
    optOptimizeLoops();
}

/*****************************************************************************/
void Compiler::ProcessShutdownWork(ICorStaticInfo* statInfo)
{
}

#ifdef _TARGET_AMD64_
//  Check if we need to add the Quirk for the PPP backward compat issue.
//  This Quirk addresses a compatibility issue between the new RyuJit and the previous JIT64.
//  A backward compatibity issue called 'PPP' exists where a PInvoke call passes a 32-byte struct
//  into a native API which basically writes 48 bytes of data into the struct.
//  With the stack frame layout used by the RyuJIT the extra 16 bytes written corrupts a
//  caller saved register and this leads to an A/V in the calling method.
//  The older JIT64 jit compiler just happened to have a different stack layout and/or
//  caller saved register set so that it didn't hit the A/V in the caller.
//  By increasing the amount of stack allocted for the struct by 32 bytes we can fix this.
//
//  Return true if we actually perform the Quirk, otherwise return false
//
bool Compiler::compQuirkForPPP()
{
    if (lvaCount != 2)
    { // We require that there are exactly two locals
        return false;
    }

    if (compTailCallUsed)
    { // Don't try this quirk if a tail call was used
        return false;
    }

    bool       hasOutArgs          = false;
    LclVarDsc* varDscExposedStruct = nullptr;

    unsigned   lclNum;
    LclVarDsc* varDsc;

    /* Look for struct locals that are address taken */
    for (lclNum = 0, varDsc = lvaTable; lclNum < lvaCount; lclNum++, varDsc++)
    {
        if (varDsc->lvIsParam) // It can't be a parameter
        {
            continue;
        }

        // We require that the OutgoingArg space lclVar exists
        if (lclNum == lvaOutgoingArgSpaceVar)
        {
            hasOutArgs = true; // Record that we saw it
            continue;
        }

        // Look for a 32-byte address exposed Struct and record its varDsc
        if ((varDsc->TypeGet() == TYP_STRUCT) && varDsc->lvAddrExposed && (varDsc->lvExactSize == 32))
        {
            varDscExposedStruct = varDsc;
        }
    }

    // We only perform the Quirk when there are two locals
    // one of them is a address exposed struct of size 32
    // and the other is the outgoing arg space local
    //
    if (hasOutArgs && (varDscExposedStruct != nullptr))
    {
#ifdef DEBUG
        if (verbose)
        {
            printf("\nAdding a backwards compatibility quirk for the 'PPP' issue\n");
        }
#endif // DEBUG

        // Increase the exact size of this struct by 32 bytes
        // This fixes the PPP backward compat issue
        varDscExposedStruct->lvExactSize += 32;

        // Update the GC info to indicate that the padding area does
        // not contain any GC pointers.
        //
        // The struct is now 64 bytes.
        //
        // We're on x64 so this should be 8 pointer slots.
        assert((varDscExposedStruct->lvExactSize / TARGET_POINTER_SIZE) == 8);

        BYTE* oldGCPtrs = varDscExposedStruct->lvGcLayout;
        BYTE* newGCPtrs = getAllocator(CMK_LvaTable).allocate<BYTE>(8);

        for (int i = 0; i < 4; i++)
        {
            newGCPtrs[i] = oldGCPtrs[i];
        }

        for (int i = 4; i < 8; i++)
        {
            newGCPtrs[i] = TYPE_GC_NONE;
        }

        varDscExposedStruct->lvGcLayout = newGCPtrs;

        return true;
    }
    return false;
}
#endif // _TARGET_AMD64_

/*****************************************************************************/

#ifdef DEBUG
void* forceFrameJIT; // used to force to frame &useful for fastchecked debugging

bool Compiler::skipMethod()
{
    static ConfigMethodRange fJitRange;
    fJitRange.EnsureInit(JitConfig.JitRange());
    assert(!fJitRange.Error());

    // Normally JitConfig.JitRange() is null, we don't want to skip
    // jitting any methods.
    //
    // So, the logic below relies on the fact that a null range string
    // passed to ConfigMethodRange represents the set of all methods.

    if (!fJitRange.Contains(info.compCompHnd, info.compMethodHnd))
    {
        return true;
    }

    if (JitConfig.JitExclude().contains(info.compMethodName, info.compClassName, &info.compMethodInfo->args))
    {
        return true;
    }

    if (!JitConfig.JitInclude().isEmpty() &&
        !JitConfig.JitInclude().contains(info.compMethodName, info.compClassName, &info.compMethodInfo->args))
    {
        return true;
    }

    return false;
}

#endif

/*****************************************************************************/

int Compiler::compCompile(CORINFO_METHOD_HANDLE methodHnd,
                          CORINFO_MODULE_HANDLE classPtr,
                          COMP_HANDLE           compHnd,
                          CORINFO_METHOD_INFO*  methodInfo,
                          void**                methodCodePtr,
                          ULONG*                methodCodeSize,
                          JitFlags*             compileFlags)
{
#ifdef FEATURE_JIT_METHOD_PERF
    static bool checkedForJitTimeLog = false;

    pCompJitTimer = nullptr;

    if (!checkedForJitTimeLog)
    {
        // Call into VM to get the config strings. FEATURE_JIT_METHOD_PERF is enabled for
        // retail builds. Do not call the regular Config helper here as it would pull
        // in a copy of the config parser into the clrjit.dll.
        InterlockedCompareExchangeT(&Compiler::compJitTimeLogFilename, compHnd->getJitTimeLogFilename(), NULL);

        // At a process or module boundary clear the file and start afresh.
        JitTimer::PrintCsvHeader();

        checkedForJitTimeLog = true;
    }
    if ((Compiler::compJitTimeLogFilename != nullptr) || (JitTimeLogCsv() != nullptr))
    {
        pCompJitTimer = JitTimer::Create(this, methodInfo->ILCodeSize);
    }
#endif // FEATURE_JIT_METHOD_PERF

#ifdef DEBUG
    Compiler* me  = this;
    forceFrameJIT = (void*)&me; // let us see the this pointer in fastchecked build
    // set this early so we can use it without relying on random memory values
    verbose = compIsForInlining() ? impInlineInfo->InlinerCompiler->verbose : false;

    this->dumpIR             = compIsForInlining() ? impInlineInfo->InlinerCompiler->dumpIR : false;
    this->dumpIRPhase        = compIsForInlining() ? impInlineInfo->InlinerCompiler->dumpIRPhase : nullptr;
    this->dumpIRFormat       = compIsForInlining() ? impInlineInfo->InlinerCompiler->dumpIRFormat : nullptr;
    this->dumpIRTypes        = compIsForInlining() ? impInlineInfo->InlinerCompiler->dumpIRTypes : false;
    this->dumpIRLocals       = compIsForInlining() ? impInlineInfo->InlinerCompiler->dumpIRLocals : false;
    this->dumpIRRegs         = compIsForInlining() ? impInlineInfo->InlinerCompiler->dumpIRRegs : false;
    this->dumpIRSsa          = compIsForInlining() ? impInlineInfo->InlinerCompiler->dumpIRSsa : false;
    this->dumpIRValnums      = compIsForInlining() ? impInlineInfo->InlinerCompiler->dumpIRValnums : false;
    this->dumpIRCosts        = compIsForInlining() ? impInlineInfo->InlinerCompiler->dumpIRCosts : false;
    this->dumpIRFlags        = compIsForInlining() ? impInlineInfo->InlinerCompiler->dumpIRFlags : false;
    this->dumpIRKinds        = compIsForInlining() ? impInlineInfo->InlinerCompiler->dumpIRKinds : false;
    this->dumpIRNodes        = compIsForInlining() ? impInlineInfo->InlinerCompiler->dumpIRNodes : false;
    this->dumpIRNoLists      = compIsForInlining() ? impInlineInfo->InlinerCompiler->dumpIRNoLists : false;
    this->dumpIRNoLeafs      = compIsForInlining() ? impInlineInfo->InlinerCompiler->dumpIRNoLeafs : false;
    this->dumpIRNoStmts      = compIsForInlining() ? impInlineInfo->InlinerCompiler->dumpIRNoStmts : false;
    this->dumpIRTrees        = compIsForInlining() ? impInlineInfo->InlinerCompiler->dumpIRTrees : false;
    this->dumpIRLinear       = compIsForInlining() ? impInlineInfo->InlinerCompiler->dumpIRLinear : false;
    this->dumpIRDataflow     = compIsForInlining() ? impInlineInfo->InlinerCompiler->dumpIRDataflow : false;
    this->dumpIRBlockHeaders = compIsForInlining() ? impInlineInfo->InlinerCompiler->dumpIRBlockHeaders : NULL;
    this->dumpIRExit         = compIsForInlining() ? impInlineInfo->InlinerCompiler->dumpIRExit : NULL;

#endif

#if defined(DEBUG) || defined(INLINE_DATA)
    info.compMethodHashPrivate = 0;
#endif // defined(DEBUG) || defined(INLINE_DATA)

#if FUNC_INFO_LOGGING
    LPCWSTR tmpJitFuncInfoFilename = JitConfig.JitFuncInfoFile();

    if (tmpJitFuncInfoFilename != nullptr)
    {
        LPCWSTR oldFuncInfoFileName =
            InterlockedCompareExchangeT(&compJitFuncInfoFilename, tmpJitFuncInfoFilename, NULL);
        if (oldFuncInfoFileName == nullptr)
        {
            assert(compJitFuncInfoFile == nullptr);
            compJitFuncInfoFile = _wfopen(compJitFuncInfoFilename, W("a"));
            if (compJitFuncInfoFile == nullptr)
            {
#if defined(DEBUG) && !defined(FEATURE_PAL) // no 'perror' in the PAL
                perror("Failed to open JitFuncInfoLogFile");
#endif // defined(DEBUG) && !defined(FEATURE_PAL)
            }
        }
    }
#endif // FUNC_INFO_LOGGING

    // if (s_compMethodsCount==0) setvbuf(jitstdout, NULL, _IONBF, 0);

    info.compCompHnd    = compHnd;
    info.compMethodHnd  = methodHnd;
    info.compMethodInfo = methodInfo;

    virtualStubParamInfo = new (this, CMK_Unknown) VirtualStubParamInfo(IsTargetAbi(CORINFO_CORERT_ABI));

    // Do we have a matched VM? Or are we "abusing" the VM to help us do JIT work (such as using an x86 native VM
    // with an ARM-targeting "altjit").
    info.compMatchedVM = IMAGE_FILE_MACHINE_TARGET == info.compCompHnd->getExpectedTargetArchitecture();

#if (defined(_TARGET_UNIX_) && !defined(_HOST_UNIX_)) || (!defined(_TARGET_UNIX_) && defined(_HOST_UNIX_))
    // The host and target platforms don't match. This info isn't handled by the existing
    // getExpectedTargetArchitecture() JIT-EE interface method.
    info.compMatchedVM = false;
#endif

    // If we are not compiling for a matched VM, then we are getting JIT flags that don't match our target
    // architecture. The two main examples here are an ARM targeting altjit hosted on x86 and an ARM64
    // targeting altjit hosted on x64. (Though with cross-bitness work, the host doesn't necessarily need
    // to be of the same bitness.) In these cases, we need to fix up the JIT flags to be appropriate for
    // the target, as the VM's expected target may overlap bit flags with different meaning to our target.
    // Note that it might be better to do this immediately when setting the JIT flags in CILJit::compileMethod()
    // (when JitFlags::SetFromFlags() is called), but this is close enough. (To move this logic to
    // CILJit::compileMethod() would require moving the info.compMatchedVM computation there as well.)

    if (!info.compMatchedVM)
    {
#if defined(_TARGET_ARM_)

// Currently nothing needs to be done. There are no ARM flags that conflict with other flags.

#endif // defined(_TARGET_ARM_)

#if defined(_TARGET_ARM64_)

        // The x86/x64 architecture capabilities flags overlap with the ARM64 ones. Set a reasonable architecture
        // target default. Currently this is disabling all ARM64 architecture features except FP and SIMD, but this
        // should be altered to possibly enable all of them, when they are known to all work.

        compileFlags->Clear(JitFlags::JIT_FLAG_HAS_ARM64_AES);
        compileFlags->Clear(JitFlags::JIT_FLAG_HAS_ARM64_ATOMICS);
        compileFlags->Clear(JitFlags::JIT_FLAG_HAS_ARM64_CRC32);
        compileFlags->Clear(JitFlags::JIT_FLAG_HAS_ARM64_DCPOP);
        compileFlags->Clear(JitFlags::JIT_FLAG_HAS_ARM64_DP);
        compileFlags->Clear(JitFlags::JIT_FLAG_HAS_ARM64_FCMA);
        compileFlags->Set(JitFlags::JIT_FLAG_HAS_ARM64_FP);
        compileFlags->Clear(JitFlags::JIT_FLAG_HAS_ARM64_FP16);
        compileFlags->Clear(JitFlags::JIT_FLAG_HAS_ARM64_JSCVT);
        compileFlags->Clear(JitFlags::JIT_FLAG_HAS_ARM64_LRCPC);
        compileFlags->Clear(JitFlags::JIT_FLAG_HAS_ARM64_PMULL);
        compileFlags->Clear(JitFlags::JIT_FLAG_HAS_ARM64_SHA1);
        compileFlags->Clear(JitFlags::JIT_FLAG_HAS_ARM64_SHA256);
        compileFlags->Clear(JitFlags::JIT_FLAG_HAS_ARM64_SHA512);
        compileFlags->Clear(JitFlags::JIT_FLAG_HAS_ARM64_SHA3);
        compileFlags->Set(JitFlags::JIT_FLAG_HAS_ARM64_SIMD);
        compileFlags->Clear(JitFlags::JIT_FLAG_HAS_ARM64_SIMD_V81);
        compileFlags->Clear(JitFlags::JIT_FLAG_HAS_ARM64_SIMD_FP16);
        compileFlags->Clear(JitFlags::JIT_FLAG_HAS_ARM64_SM3);
        compileFlags->Clear(JitFlags::JIT_FLAG_HAS_ARM64_SM4);
        compileFlags->Clear(JitFlags::JIT_FLAG_HAS_ARM64_SVE);

#endif // defined(_TARGET_ARM64_)
    }

    compMaxUncheckedOffsetForNullObject = eeGetEEInfo()->maxUncheckedOffsetForNullObject;

    // Set the context for token lookup.
    if (compIsForInlining())
    {
        impTokenLookupContextHandle = impInlineInfo->tokenLookupContextHandle;

        assert(impInlineInfo->inlineCandidateInfo->clsHandle == compHnd->getMethodClass(methodHnd));
        info.compClassHnd = impInlineInfo->inlineCandidateInfo->clsHandle;

        assert(impInlineInfo->inlineCandidateInfo->clsAttr == info.compCompHnd->getClassAttribs(info.compClassHnd));
        // printf("%x != %x\n", impInlineInfo->inlineCandidateInfo->clsAttr,
        // info.compCompHnd->getClassAttribs(info.compClassHnd));
        info.compClassAttr = impInlineInfo->inlineCandidateInfo->clsAttr;
    }
    else
    {
        impTokenLookupContextHandle = MAKE_METHODCONTEXT(info.compMethodHnd);

        info.compClassHnd  = compHnd->getMethodClass(methodHnd);
        info.compClassAttr = info.compCompHnd->getClassAttribs(info.compClassHnd);
    }

    info.compProfilerCallback = false; // Assume false until we are told to hook this method.

#if defined(DEBUG) || defined(LATE_DISASM)
    const char* classNamePtr;

    info.compMethodName = eeGetMethodName(methodHnd, &classNamePtr);
    unsigned len        = (unsigned)roundUp(strlen(classNamePtr) + 1);
    info.compClassName  = getAllocator(CMK_DebugOnly).allocate<char>(len);
    strcpy_s((char*)info.compClassName, len, classNamePtr);

    info.compFullName = eeGetMethodFullName(methodHnd);
#endif // defined(DEBUG) || defined(LATE_DISASM)

#ifdef DEBUG
    if (!compIsForInlining())
    {
        JitTls::GetLogEnv()->setCompiler(this);
    }

    // Have we been told to be more selective in our Jitting?
    if (skipMethod())
    {
        if (compIsForInlining())
        {
            compInlineResult->NoteFatal(InlineObservation::CALLEE_MARKED_AS_SKIPPED);
        }
        return CORJIT_SKIPPED;
    }

    // Opt-in to jit stress based on method hash ranges.
    //
    // Note the default (with JitStressRange not set) is that all
    // methods will be subject to stress.
    static ConfigMethodRange fJitStressRange;
    fJitStressRange.EnsureInit(JitConfig.JitStressRange());
    assert(!fJitStressRange.Error());
    bRangeAllowStress = fJitStressRange.Contains(info.compCompHnd, info.compMethodHnd);

#endif // DEBUG

    // Set this before the first 'BADCODE'
    // Skip verification where possible
    tiVerificationNeeded = !compileFlags->IsSet(JitFlags::JIT_FLAG_SKIP_VERIFICATION);

    assert(!compIsForInlining() || !tiVerificationNeeded); // Inlinees must have been verified.

    // assume the code is verifiable unless proven otherwise
    tiIsVerifiableCode = TRUE;

    tiRuntimeCalloutNeeded = false;

    CorInfoInstantiationVerification instVerInfo = INSTVER_GENERIC_PASSED_VERIFICATION;

    if (!compIsForInlining() && tiVerificationNeeded)
    {
        instVerInfo = compHnd->isInstantiationOfVerifiedGeneric(methodHnd);

        if (tiVerificationNeeded && (instVerInfo == INSTVER_GENERIC_FAILED_VERIFICATION))
        {
            CorInfoCanSkipVerificationResult canSkipVerificationResult =
                info.compCompHnd->canSkipMethodVerification(info.compMethodHnd);

            switch (canSkipVerificationResult)
            {
                case CORINFO_VERIFICATION_CANNOT_SKIP:
                    // We cannot verify concrete instantiation.
                    // We can only verify the typical/open instantiation
                    // The VM should throw a VerificationException instead of allowing this.
                    NO_WAY("Verification of closed instantiations is not supported");
                    break;

                case CORINFO_VERIFICATION_CAN_SKIP:
                    // The VM should first verify the open instantiation. If unverifiable code
                    // is detected, it should pass in JitFlags::JIT_FLAG_SKIP_VERIFICATION.
                    assert(!"The VM should have used JitFlags::JIT_FLAG_SKIP_VERIFICATION");
                    tiVerificationNeeded = false;
                    break;

                case CORINFO_VERIFICATION_RUNTIME_CHECK:
                    // This is a concrete generic instantiation with unverifiable code, that also
                    // needs a runtime callout.
                    tiVerificationNeeded   = false;
                    tiRuntimeCalloutNeeded = true;
                    break;

                case CORINFO_VERIFICATION_DONT_JIT:
                    // We cannot verify concrete instantiation.
                    // We can only verify the typical/open instantiation
                    // The VM should throw a VerificationException instead of allowing this.
                    BADCODE("NGEN of unverifiable transparent code is not supported");
                    break;
            }
        }

        // load any constraints for verification, noting any cycles to be rejected by the verifying importer
        if (tiVerificationNeeded)
        {
            compHnd->initConstraintsForVerification(methodHnd, &info.hasCircularClassConstraints,
                                                    &info.hasCircularMethodConstraints);
        }
    }

    /* Setup an error trap */

    struct Param
    {
        Compiler* pThis;

        CORINFO_MODULE_HANDLE classPtr;
        COMP_HANDLE           compHnd;
        CORINFO_METHOD_INFO*  methodInfo;
        void**                methodCodePtr;
        ULONG*                methodCodeSize;
        JitFlags*             compileFlags;

        CorInfoInstantiationVerification instVerInfo;
        int                              result;
    } param;
    param.pThis          = this;
    param.classPtr       = classPtr;
    param.compHnd        = compHnd;
    param.methodInfo     = methodInfo;
    param.methodCodePtr  = methodCodePtr;
    param.methodCodeSize = methodCodeSize;
    param.compileFlags   = compileFlags;
    param.instVerInfo    = instVerInfo;
    param.result         = CORJIT_INTERNALERROR;

    setErrorTrap(compHnd, Param*, pParam, &param) // ERROR TRAP: Start normal block
    {
        pParam->result = pParam->pThis->compCompileHelper(pParam->classPtr, pParam->compHnd, pParam->methodInfo,
                                                          pParam->methodCodePtr, pParam->methodCodeSize,
                                                          pParam->compileFlags, pParam->instVerInfo);
    }
    finallyErrorTrap() // ERROR TRAP: The following block handles errors
    {
        /* Cleanup  */

        if (compIsForInlining())
        {
            goto DoneCleanUp;
        }

        /* Tell the emitter that we're done with this function */

        genEmitter->emitEndCG();

    DoneCleanUp:
        compDone();
    }
    endErrorTrap() // ERROR TRAP: End

        return param.result;
}

#if defined(DEBUG) || defined(INLINE_DATA)
unsigned Compiler::Info::compMethodHash() const
{
    if (compMethodHashPrivate == 0)
    {
        compMethodHashPrivate = compCompHnd->getMethodHash(compMethodHnd);
    }
    return compMethodHashPrivate;
}
#endif // defined(DEBUG) || defined(INLINE_DATA)

void Compiler::compCompileFinish()
{
#if defined(DEBUG) || MEASURE_NODE_SIZE || MEASURE_BLOCK_SIZE || DISPLAY_SIZES || CALL_ARG_STATS
    genMethodCnt++;
#endif

#if MEASURE_MEM_ALLOC
    {
        compArenaAllocator->finishMemStats();
        memAllocHist.record((unsigned)((compArenaAllocator->getTotalBytesAllocated() + 1023) / 1024));
        memUsedHist.record((unsigned)((compArenaAllocator->getTotalBytesUsed() + 1023) / 1024));
    }

#ifdef DEBUG
    if (s_dspMemStats || verbose)
    {
        printf("\nAllocations for %s (MethodHash=%08x)\n", info.compFullName, info.compMethodHash());
        compArenaAllocator->dumpMemStats(jitstdout);
    }
#endif // DEBUG
#endif // MEASURE_MEM_ALLOC

#if LOOP_HOIST_STATS
    AddLoopHoistStats();
#endif // LOOP_HOIST_STATS

#if MEASURE_NODE_SIZE
    genTreeNcntHist.record(static_cast<unsigned>(genNodeSizeStatsPerFunc.genTreeNodeCnt));
    genTreeNsizHist.record(static_cast<unsigned>(genNodeSizeStatsPerFunc.genTreeNodeSize));
#endif

#if defined(DEBUG)
    // Small methods should fit in ArenaAllocator::getDefaultPageSize(), or else
    // we should bump up ArenaAllocator::getDefaultPageSize()

    if ((info.compILCodeSize <= 32) &&     // Is it a reasonably small method?
        (info.compNativeCodeSize < 512) && // Some trivial methods generate huge native code. eg. pushing a single huge
                                           // struct
        (impInlinedCodeSize <= 128) &&     // Is the the inlining reasonably bounded?
                                           // Small methods cannot meaningfully have a big number of locals
                                           // or arguments. We always track arguments at the start of
                                           // the prolog which requires memory
        (info.compLocalsCount <= 32) && (!opts.MinOpts()) && // We may have too many local variables, etc
        (getJitStressLevel() == 0) &&                        // We need extra memory for stress
        !opts.optRepeat &&                                   // We need extra memory to repeat opts
        !compArenaAllocator->bypassHostAllocator() && // ArenaAllocator::getDefaultPageSize() is artificially low for
                                                      // DirectAlloc
        // Factor of 2x is because data-structures are bigger under DEBUG
        (compArenaAllocator->getTotalBytesAllocated() > (2 * ArenaAllocator::getDefaultPageSize())) &&
        // RyuJIT backend needs memory tuning! TODO-Cleanup: remove this case when memory tuning is complete.
        (compArenaAllocator->getTotalBytesAllocated() > (10 * ArenaAllocator::getDefaultPageSize())) &&
        !verbose) // We allocate lots of memory to convert sets to strings for JitDump
    {
        genSmallMethodsNeedingExtraMemoryCnt++;

        // Less than 1% of all methods should run into this.
        // We cannot be more strict as there are always degenerate cases where we
        // would need extra memory (like huge structs as locals - see lvaSetStruct()).
        assert((genMethodCnt < 500) || (genSmallMethodsNeedingExtraMemoryCnt < (genMethodCnt / 100)));
    }
#endif // DEBUG

#if defined(DEBUG) || defined(INLINE_DATA)

    m_inlineStrategy->DumpData();
    m_inlineStrategy->DumpXml();

#endif

#ifdef DEBUG
    if (opts.dspOrder)
    {
        // mdMethodDef __stdcall CEEInfo::getMethodDefFromMethod(CORINFO_METHOD_HANDLE hMethod)
        mdMethodDef currentMethodToken = info.compCompHnd->getMethodDefFromMethod(info.compMethodHnd);

        unsigned profCallCount = 0;
        if (opts.jitFlags->IsSet(JitFlags::JIT_FLAG_BBOPT) && fgHaveProfileData())
        {
            assert(fgBlockCounts[0].ILOffset == 0);
            profCallCount = fgBlockCounts[0].ExecutionCount;
        }

        static bool headerPrinted = false;
        if (!headerPrinted)
        {
            // clang-format off
            headerPrinted = true;
            printf("         |  Profiled  | Exec-    |   Method has    |   calls   | Num |LclV |AProp| CSE |   Reg   |bytes | %3s code size | \n", Target::g_tgtCPUName);
            printf(" mdToken |     |  RGN |    Count | EH | FRM | LOOP | NRM | IND | BBs | Cnt | Cnt | Cnt |  Alloc  |  IL  |   HOT |  COLD | method name \n");
            printf("---------+-----+------+----------+----+-----+------+-----+-----+-----+-----+-----+-----+---------+------+-------+-------+-----------\n");
            //      06001234 | PRF |  HOT |      219 | EH | ebp | LOOP |  15 |   6 |  12 |  17 |  12 |   8 |   28 p2 |  145 |   211 |   123 | System.Example(int)
            // clang-format on
        }

        printf("%08X | ", currentMethodToken);

        CorInfoRegionKind regionKind = info.compMethodInfo->regionKind;

        if (opts.altJit)
        {
            printf("ALT | ");
        }
        else if (fgHaveProfileData())
        {
            printf("PRF | ");
        }
        else
        {
            printf("    | ");
        }

        if (regionKind == CORINFO_REGION_NONE)
        {
            printf("     | ");
        }
        else if (regionKind == CORINFO_REGION_HOT)
        {
            printf(" HOT | ");
        }
        else if (regionKind == CORINFO_REGION_COLD)
        {
            printf("COLD | ");
        }
        else if (regionKind == CORINFO_REGION_JIT)
        {
            printf(" JIT | ");
        }
        else
        {
            printf("UNKN | ");
        }

        printf("%8d | ", profCallCount);

        if (compHndBBtabCount > 0)
        {
            printf("EH | ");
        }
        else
        {
            printf("   | ");
        }

        if (rpFrameType == FT_EBP_FRAME)
        {
            printf("%3s | ", STR_FPBASE);
        }
        else if (rpFrameType == FT_ESP_FRAME)
        {
            printf("%3s | ", STR_SPBASE);
        }
#if DOUBLE_ALIGN
        else if (rpFrameType == FT_DOUBLE_ALIGN_FRAME)
        {
            printf("dbl | ");
        }
#endif
        else // (rpFrameType == FT_NOT_SET)
        {
            printf("??? | ");
        }

        if (fgHasLoops)
        {
            printf("LOOP |");
        }
        else
        {
            printf("     |");
        }

        printf(" %3d |", optCallCount);
        printf(" %3d |", optIndirectCallCount);
        printf(" %3d |", fgBBcountAtCodegen);
        printf(" %3d |", lvaCount);

        if (opts.MinOpts())
        {
            printf("  MinOpts  |");
        }
        else
        {
            printf(" %3d |", optAssertionCount);
#if FEATURE_ANYCSE
            printf(" %3d |", optCSEcount);
#else
            printf(" %3d |", 0);
#endif // FEATURE_ANYCSE
        }

        printf(" LSRA    |"); // TODO-Cleanup: dump some interesting LSRA stat into the order file?
        printf(" %4d |", info.compMethodInfo->ILCodeSize);
        printf(" %5d |", info.compTotalHotCodeSize);
        printf(" %5d |", info.compTotalColdCodeSize);

        printf(" %s\n", eeGetMethodFullName(info.compMethodHnd));
        printf(""); // in our logic this causes a flush
    }

    if (verbose)
    {
        printf("****** DONE compiling %s\n", info.compFullName);
        printf(""); // in our logic this causes a flush
    }

    // Only call _DbgBreakCheck when we are jitting, not when we are ngen-ing
    // For ngen the int3 or breakpoint instruction will be right at the
    // start of the ngen method and we will stop when we execute it.
    //
    if (!opts.jitFlags->IsSet(JitFlags::JIT_FLAG_PREJIT))
    {
        if (compJitHaltMethod())
        {
#if !defined(_HOST_UNIX_)
            // TODO-UNIX: re-enable this when we have an OS that supports a pop-up dialog

            // Don't do an assert, but just put up the dialog box so we get just-in-time debugger
            // launching.  When you hit 'retry' it will continue and naturally stop at the INT 3
            // that the JIT put in the code
            _DbgBreakCheck(__FILE__, __LINE__, "JitHalt");
#endif
        }
    }
#endif // DEBUG
}

#ifdef PSEUDORANDOM_NOP_INSERTION
// this is zlib adler32 checksum.  source came from windows base

#define BASE 65521L // largest prime smaller than 65536
#define NMAX 5552
// NMAX is the largest n such that 255n(n+1)/2 + (n+1)(BASE-1) <= 2^32-1

#define DO1(buf, i)                                                                                                    \
    {                                                                                                                  \
        s1 += buf[i];                                                                                                  \
        s2 += s1;                                                                                                      \
    }
#define DO2(buf, i)                                                                                                    \
    DO1(buf, i);                                                                                                       \
    DO1(buf, i + 1);
#define DO4(buf, i)                                                                                                    \
    DO2(buf, i);                                                                                                       \
    DO2(buf, i + 2);
#define DO8(buf, i)                                                                                                    \
    DO4(buf, i);                                                                                                       \
    DO4(buf, i + 4);
#define DO16(buf)                                                                                                      \
    DO8(buf, 0);                                                                                                       \
    DO8(buf, 8);

unsigned adler32(unsigned adler, char* buf, unsigned int len)
{
    unsigned int s1 = adler & 0xffff;
    unsigned int s2 = (adler >> 16) & 0xffff;
    int          k;

    if (buf == NULL)
        return 1L;

    while (len > 0)
    {
        k = len < NMAX ? len : NMAX;
        len -= k;
        while (k >= 16)
        {
            DO16(buf);
            buf += 16;
            k -= 16;
        }
        if (k != 0)
            do
            {
                s1 += *buf++;
                s2 += s1;
            } while (--k);
        s1 %= BASE;
        s2 %= BASE;
    }
    return (s2 << 16) | s1;
}
#endif

unsigned getMethodBodyChecksum(__in_z char* code, int size)
{
#ifdef PSEUDORANDOM_NOP_INSERTION
    return adler32(0, code, size);
#else
    return 0;
#endif
}

int Compiler::compCompileHelper(CORINFO_MODULE_HANDLE            classPtr,
                                COMP_HANDLE                      compHnd,
                                CORINFO_METHOD_INFO*             methodInfo,
                                void**                           methodCodePtr,
                                ULONG*                           methodCodeSize,
                                JitFlags*                        compileFlags,
                                CorInfoInstantiationVerification instVerInfo)
{
    CORINFO_METHOD_HANDLE methodHnd = info.compMethodHnd;

    info.compCode         = methodInfo->ILCode;
    info.compILCodeSize   = methodInfo->ILCodeSize;
    info.compILImportSize = 0;

    if (info.compILCodeSize == 0)
    {
        BADCODE("code size is zero");
    }

    if (compIsForInlining())
    {
#ifdef DEBUG
        unsigned methAttr_Old  = impInlineInfo->inlineCandidateInfo->methAttr;
        unsigned methAttr_New  = info.compCompHnd->getMethodAttribs(info.compMethodHnd);
        unsigned flagsToIgnore = CORINFO_FLG_DONT_INLINE | CORINFO_FLG_FORCEINLINE;
        assert((methAttr_Old & (~flagsToIgnore)) == (methAttr_New & (~flagsToIgnore)));
#endif

        info.compFlags = impInlineInfo->inlineCandidateInfo->methAttr;
    }
    else
    {
        info.compFlags = info.compCompHnd->getMethodAttribs(info.compMethodHnd);
#ifdef PSEUDORANDOM_NOP_INSERTION
        info.compChecksum = getMethodBodyChecksum((char*)methodInfo->ILCode, methodInfo->ILCodeSize);
#endif
    }

    // compInitOptions will set the correct verbose flag.

    compInitOptions(compileFlags);

#ifdef ALT_JIT
    if (!compIsForInlining() && !opts.altJit)
    {
        // We're an altjit, but the COMPlus_AltJit configuration did not say to compile this method,
        // so skip it.
        return CORJIT_SKIPPED;
    }
#endif // ALT_JIT

#ifdef DEBUG

    if (verbose)
    {
        printf("IL to import:\n");
        dumpILRange(info.compCode, info.compILCodeSize);
    }

#endif

    // Check for COMPlus_AggressiveInlining
    if (JitConfig.JitAggressiveInlining())
    {
        compDoAggressiveInlining = true;
    }

    if (compDoAggressiveInlining)
    {
        info.compFlags |= CORINFO_FLG_FORCEINLINE;
    }

#ifdef DEBUG

    // Check for ForceInline stress.
    if (compStressCompile(STRESS_FORCE_INLINE, 0))
    {
        info.compFlags |= CORINFO_FLG_FORCEINLINE;
    }

    if (compIsForInlining())
    {
        JITLOG((LL_INFO100000, "\nINLINER impTokenLookupContextHandle for %s is 0x%p.\n",
                eeGetMethodFullName(info.compMethodHnd), dspPtr(impTokenLookupContextHandle)));
    }

    // Force verification if asked to do so
    if (JitConfig.JitForceVer())
    {
        tiVerificationNeeded = (instVerInfo == INSTVER_NOT_INSTANTIATION);
    }

    if (tiVerificationNeeded)
    {
        JITLOG((LL_INFO10000, "tiVerificationNeeded initially set to true for %s\n", info.compFullName));
    }
#endif // DEBUG

    /* Since tiVerificationNeeded can be turned off in the middle of
       compiling a method, and it might have caused blocks to be queued up
       for reimporting, impCanReimport can be used to check for reimporting. */

    impCanReimport = (tiVerificationNeeded || compStressCompile(STRESS_CHK_REIMPORT, 15));

    // Need security prolog/epilog callouts when there is a declarative security in the method.
    tiSecurityCalloutNeeded = ((info.compFlags & CORINFO_FLG_NOSECURITYWRAP) == 0);

    if (tiSecurityCalloutNeeded || (info.compFlags & CORINFO_FLG_SECURITYCHECK))
    {
        // We need to allocate the security object on the stack
        // when the method being compiled has a declarative security
        // (i.e. when CORINFO_FLG_NOSECURITYWRAP is reset for the current method).
        // This is also the case when we inject a prolog and epilog in the method.
        opts.compNeedSecurityCheck = true;
    }

    /* Initialize set a bunch of global values */

    info.compScopeHnd      = classPtr;
    info.compXcptnsCount   = methodInfo->EHcount;
    info.compMaxStack      = methodInfo->maxStack;
    compHndBBtab           = nullptr;
    compHndBBtabCount      = 0;
    compHndBBtabAllocCount = 0;

    info.compNativeCodeSize    = 0;
    info.compTotalHotCodeSize  = 0;
    info.compTotalColdCodeSize = 0;

    fgHasBackwardJump = false;

#ifdef DEBUG
    compCurBB = nullptr;
    lvaTable  = nullptr;

    // Reset node and block ID counter
    compGenTreeID    = 0;
    compBasicBlockID = 0;
#endif

    /* Initialize emitter */

    if (!compIsForInlining())
    {
        codeGen->getEmitter()->emitBegCG(this, compHnd);
    }

    info.compIsStatic = (info.compFlags & CORINFO_FLG_STATIC) != 0;

    info.compIsContextful = (info.compClassAttr & CORINFO_FLG_CONTEXTFUL) != 0;

    info.compPublishStubParam = opts.jitFlags->IsSet(JitFlags::JIT_FLAG_PUBLISH_SECRET_PARAM);

    switch (methodInfo->args.getCallConv())
    {
        case CORINFO_CALLCONV_VARARG:
        case CORINFO_CALLCONV_NATIVEVARARG:
            info.compIsVarArgs = true;
            break;
        case CORINFO_CALLCONV_DEFAULT:
            info.compIsVarArgs = false;
            break;
        default:
            BADCODE("bad calling convention");
    }
    info.compRetNativeType = info.compRetType = JITtype2varType(methodInfo->args.retType);

    info.compCallUnmanaged   = 0;
    info.compLvFrameListRoot = BAD_VAR_NUM;

    info.compInitMem = ((methodInfo->options & CORINFO_OPT_INIT_LOCALS) != 0);

    /* Allocate the local variable table */

    lvaInitTypeRef();

    if (!compIsForInlining())
    {
        compInitDebuggingInfo();
    }

#ifdef DEBUG
    if (compIsForInlining())
    {
        compBasicBlockID = impInlineInfo->InlinerCompiler->compBasicBlockID;
    }
#endif

    const bool forceInline = !!(info.compFlags & CORINFO_FLG_FORCEINLINE);

    if (!compIsForInlining() && opts.jitFlags->IsSet(JitFlags::JIT_FLAG_PREJIT))
    {
        // We're prejitting the root method. We also will analyze it as
        // a potential inline candidate.
        InlineResult prejitResult(this, methodHnd, "prejit");

        // Do the initial inline screen.
        impCanInlineIL(methodHnd, methodInfo, forceInline, &prejitResult);

        // Temporarily install the prejitResult as the
        // compInlineResult so it's available to fgFindJumpTargets
        // and can accumulate more observations as the IL is
        // scanned.
        //
        // We don't pass prejitResult in as a parameter to avoid
        // potential aliasing confusion -- the other call to
        // fgFindBasicBlocks may have set up compInlineResult and
        // the code in fgFindJumpTargets references that data
        // member extensively.
        assert(compInlineResult == nullptr);
        assert(impInlineInfo == nullptr);
        compInlineResult = &prejitResult;

        // Find the basic blocks. We must do this regardless of
        // inlineability, since we are prejitting this method.
        //
        // This will also update the status of this method as
        // an inline candidate.
        fgFindBasicBlocks();

        // Undo the temporary setup.
        assert(compInlineResult == &prejitResult);
        compInlineResult = nullptr;

        // If still a viable, discretionary inline, assess
        // profitability.
        if (prejitResult.IsDiscretionaryCandidate())
        {
            prejitResult.DetermineProfitability(methodInfo);
        }

        m_inlineStrategy->NotePrejitDecision(prejitResult);

        // Handle the results of the inline analysis.
        if (prejitResult.IsFailure())
        {
            // This method is a bad inlinee according to our
            // analysis.  We will let the InlineResult destructor
            // mark it as noinline in the prejit image to save the
            // jit some work.
            //
            // This decision better not be context-dependent.
            assert(prejitResult.IsNever());
        }
        else
        {
            // This looks like a viable inline candidate.  Since
            // we're not actually inlining, don't report anything.
            prejitResult.SetReported();
        }
    }
    else
    {
        // We are jitting the root method, or inlining.
        fgFindBasicBlocks();
    }

    // If we're inlining and the candidate is bad, bail out.
    if (compDonotInline())
    {
        goto _Next;
    }

#ifdef FEATURE_CORECLR
    if (fgHasBackwardJump && (info.compFlags & CORINFO_FLG_DISABLE_TIER0_FOR_LOOPS) != 0 && fgCanSwitchToTier1())
#else // !FEATURE_CORECLR
    // We may want to use JitConfig value here to support DISABLE_TIER0_FOR_LOOPS
    if (fgHasBackwardJump && fgCanSwitchToTier1())
#endif
    {
        // Method likely has a loop, switch to the OptimizedTier to avoid spending too much time running slower code
        fgSwitchToTier1();
    }

    compSetOptimizationLevel();

#if COUNT_BASIC_BLOCKS
    bbCntTable.record(fgBBcount);

    if (fgBBcount == 1)
    {
        bbOneBBSizeTable.record(methodInfo->ILCodeSize);
    }
#endif // COUNT_BASIC_BLOCKS

#ifdef DEBUG
    if (verbose)
    {
        printf("Basic block list for '%s'\n", info.compFullName);
        fgDispBasicBlocks();
    }
#endif

#ifdef DEBUG
    /* Give the function a unique number */

    if (opts.disAsm || opts.dspEmit || verbose)
    {
        s_compMethodsCount = ~info.compMethodHash() & 0xffff;
    }
    else
    {
        s_compMethodsCount++;
    }
#endif

    if (compIsForInlining())
    {
        compInlineResult->NoteInt(InlineObservation::CALLEE_NUMBER_OF_BASIC_BLOCKS, fgBBcount);

        if (compInlineResult->IsFailure())
        {
            goto _Next;
        }
    }

#ifdef DEBUG
    if (JitConfig.DumpJittedMethods() == 1 && !compIsForInlining())
    {
        printf("Compiling %4d %s::%s, IL size = %u, hsh=0x%x\n", Compiler::jitTotalMethodCompiled, info.compClassName,
               info.compMethodName, info.compILCodeSize, info.compMethodHash());
    }
    if (compIsForInlining())
    {
        compGenTreeID = impInlineInfo->InlinerCompiler->compGenTreeID;
    }
#endif

    compCompile(methodCodePtr, methodCodeSize, compileFlags);

#ifdef DEBUG
    if (compIsForInlining())
    {
        impInlineInfo->InlinerCompiler->compGenTreeID    = compGenTreeID;
        impInlineInfo->InlinerCompiler->compBasicBlockID = compBasicBlockID;
    }
#endif

_Next:

    if (compDonotInline())
    {
        // Verify we have only one inline result in play.
        assert(impInlineInfo->inlineResult == compInlineResult);
    }

    if (!compIsForInlining())
    {
        compCompileFinish();

        // Did we just compile for a target architecture that the VM isn't expecting? If so, the VM
        // can't used the generated code (and we better be an AltJit!).

        if (!info.compMatchedVM)
        {
            return CORJIT_SKIPPED;
        }

#ifdef ALT_JIT
#ifdef DEBUG
        if (JitConfig.RunAltJitCode() == 0)
        {
            return CORJIT_SKIPPED;
        }
#endif // DEBUG
#endif // ALT_JIT
    }

    /* Success! */
    return CORJIT_OK;
}

//------------------------------------------------------------------------
// compFindLocalVarLinear: Linear search for variable's scope containing offset.
//
// Arguments:
//     varNum    The variable number to search for in the array of scopes.
//     offs      The offset value which should occur within the life of the variable.
//
// Return Value:
//     VarScopeDsc* of a matching variable that contains the offset within its life
//     begin and life end or nullptr when there is no match found.
//
//  Description:
//     Linear search for matching variables with their life begin and end containing
//     the offset.
//     or NULL if one couldn't be found.
//
//  Note:
//     Usually called for scope count = 4. Could be called for values upto 8.
//
VarScopeDsc* Compiler::compFindLocalVarLinear(unsigned varNum, unsigned offs)
{
    for (unsigned i = 0; i < info.compVarScopesCount; i++)
    {
        VarScopeDsc* dsc = &info.compVarScopes[i];
        if ((dsc->vsdVarNum == varNum) && (dsc->vsdLifeBeg <= offs) && (dsc->vsdLifeEnd > offs))
        {
            return dsc;
        }
    }
    return nullptr;
}

//------------------------------------------------------------------------
// compFindLocalVar: Search for variable's scope containing offset.
//
// Arguments:
//    varNum    The variable number to search for in the array of scopes.
//    offs      The offset value which should occur within the life of the variable.
//
// Return Value:
//    VarScopeDsc* of a matching variable that contains the offset within its life
//    begin and life end.
//    or NULL if one couldn't be found.
//
//  Description:
//     Linear search for matching variables with their life begin and end containing
//     the offset only when the scope count is < MAX_LINEAR_FIND_LCL_SCOPELIST,
//     else use the hashtable lookup.
//
VarScopeDsc* Compiler::compFindLocalVar(unsigned varNum, unsigned offs)
{
    if (info.compVarScopesCount < MAX_LINEAR_FIND_LCL_SCOPELIST)
    {
        return compFindLocalVarLinear(varNum, offs);
    }
    else
    {
        VarScopeDsc* ret = compFindLocalVar(varNum, offs, offs);
        assert(ret == compFindLocalVarLinear(varNum, offs));
        return ret;
    }
}

//------------------------------------------------------------------------
// compFindLocalVar: Search for variable's scope containing offset.
//
// Arguments:
//    varNum    The variable number to search for in the array of scopes.
//    lifeBeg   The life begin of the variable's scope
//    lifeEnd   The life end of the variable's scope
//
// Return Value:
//    VarScopeDsc* of a matching variable that contains the offset within its life
//    begin and life end, or NULL if one couldn't be found.
//
//  Description:
//     Following are the steps used:
//     1. Index into the hashtable using varNum.
//     2. Iterate through the linked list at index varNum to find a matching
//        var scope.
//
VarScopeDsc* Compiler::compFindLocalVar(unsigned varNum, unsigned lifeBeg, unsigned lifeEnd)
{
    assert(compVarScopeMap != nullptr);

    VarScopeMapInfo* info;
    if (compVarScopeMap->Lookup(varNum, &info))
    {
        VarScopeListNode* list = info->head;
        while (list != nullptr)
        {
            if ((list->data->vsdLifeBeg <= lifeBeg) && (list->data->vsdLifeEnd > lifeEnd))
            {
                return list->data;
            }
            list = list->next;
        }
    }
    return nullptr;
}

//-------------------------------------------------------------------------
// compInitVarScopeMap: Create a scope map so it can be looked up by varNum
//
//  Description:
//     Map.K => Map.V :: varNum => List(ScopeDsc)
//
//     Create a scope map that can be indexed by varNum and can be iterated
//     on it's values to look for matching scope when given an offs or
//     lifeBeg and lifeEnd.
//
//  Notes:
//     1. Build the map only when we think linear search is slow, i.e.,
//     MAX_LINEAR_FIND_LCL_SCOPELIST is large.
//     2. Linked list preserves original array order.
//
void Compiler::compInitVarScopeMap()
{
    if (info.compVarScopesCount < MAX_LINEAR_FIND_LCL_SCOPELIST)
    {
        return;
    }

    assert(compVarScopeMap == nullptr);

    compVarScopeMap = new (getAllocator()) VarNumToScopeDscMap(getAllocator());

    // 599 prime to limit huge allocations; for ex: duplicated scopes on single var.
    compVarScopeMap->Reallocate(min(info.compVarScopesCount, 599));

    for (unsigned i = 0; i < info.compVarScopesCount; ++i)
    {
        unsigned varNum = info.compVarScopes[i].vsdVarNum;

        VarScopeListNode* node = VarScopeListNode::Create(&info.compVarScopes[i], getAllocator());

        // Index by varNum and if the list exists append "node" to the "list".
        VarScopeMapInfo* info;
        if (compVarScopeMap->Lookup(varNum, &info))
        {
            info->tail->next = node;
            info->tail       = node;
        }
        // Create a new list.
        else
        {
            info = VarScopeMapInfo::Create(node, getAllocator());
            compVarScopeMap->Set(varNum, info);
        }
    }
}

int __cdecl genCmpLocalVarLifeBeg(const void* elem1, const void* elem2)
{
    return (*((VarScopeDsc**)elem1))->vsdLifeBeg - (*((VarScopeDsc**)elem2))->vsdLifeBeg;
}

int __cdecl genCmpLocalVarLifeEnd(const void* elem1, const void* elem2)
{
    return (*((VarScopeDsc**)elem1))->vsdLifeEnd - (*((VarScopeDsc**)elem2))->vsdLifeEnd;
}

inline void Compiler::compInitScopeLists()
{
    if (info.compVarScopesCount == 0)
    {
        compEnterScopeList = compExitScopeList = nullptr;
        return;
    }

    // Populate the 'compEnterScopeList' and 'compExitScopeList' lists

    compEnterScopeList = new (this, CMK_DebugInfo) VarScopeDsc*[info.compVarScopesCount];
    compExitScopeList  = new (this, CMK_DebugInfo) VarScopeDsc*[info.compVarScopesCount];

    for (unsigned i = 0; i < info.compVarScopesCount; i++)
    {
        compEnterScopeList[i] = compExitScopeList[i] = &info.compVarScopes[i];
    }

    qsort(compEnterScopeList, info.compVarScopesCount, sizeof(*compEnterScopeList), genCmpLocalVarLifeBeg);
    qsort(compExitScopeList, info.compVarScopesCount, sizeof(*compExitScopeList), genCmpLocalVarLifeEnd);
}

void Compiler::compResetScopeLists()
{
    if (info.compVarScopesCount == 0)
    {
        return;
    }

    assert(compEnterScopeList && compExitScopeList);

    compNextEnterScope = compNextExitScope = 0;
}

VarScopeDsc* Compiler::compGetNextEnterScope(unsigned offs, bool scan)
{
    assert(info.compVarScopesCount);
    assert(compEnterScopeList && compExitScopeList);

    if (compNextEnterScope < info.compVarScopesCount)
    {
        assert(compEnterScopeList[compNextEnterScope]);
        unsigned nextEnterOff = compEnterScopeList[compNextEnterScope]->vsdLifeBeg;
        assert(scan || (offs <= nextEnterOff));

        if (!scan)
        {
            if (offs == nextEnterOff)
            {
                return compEnterScopeList[compNextEnterScope++];
            }
        }
        else
        {
            if (nextEnterOff <= offs)
            {
                return compEnterScopeList[compNextEnterScope++];
            }
        }
    }

    return nullptr;
}

VarScopeDsc* Compiler::compGetNextExitScope(unsigned offs, bool scan)
{
    assert(info.compVarScopesCount);
    assert(compEnterScopeList && compExitScopeList);

    if (compNextExitScope < info.compVarScopesCount)
    {
        assert(compExitScopeList[compNextExitScope]);
        unsigned nextExitOffs = compExitScopeList[compNextExitScope]->vsdLifeEnd;
        assert(scan || (offs <= nextExitOffs));

        if (!scan)
        {
            if (offs == nextExitOffs)
            {
                return compExitScopeList[compNextExitScope++];
            }
        }
        else
        {
            if (nextExitOffs <= offs)
            {
                return compExitScopeList[compNextExitScope++];
            }
        }
    }

    return nullptr;
}

// The function will call the callback functions for scopes with boundaries
// at instrs from the current status of the scope lists to 'offset',
// ordered by instrs.

void Compiler::compProcessScopesUntil(unsigned   offset,
                                      VARSET_TP* inScope,
                                      void (Compiler::*enterScopeFn)(VARSET_TP* inScope, VarScopeDsc*),
                                      void (Compiler::*exitScopeFn)(VARSET_TP* inScope, VarScopeDsc*))
{
    assert(offset != BAD_IL_OFFSET);
    assert(inScope != nullptr);

    bool         foundExit = false, foundEnter = true;
    VarScopeDsc* scope;
    VarScopeDsc* nextExitScope  = nullptr;
    VarScopeDsc* nextEnterScope = nullptr;
    unsigned     offs = offset, curEnterOffs = 0;

    goto START_FINDING_SCOPES;

    // We need to determine the scopes which are open for the current block.
    // This loop walks over the missing blocks between the current and the
    // previous block, keeping the enter and exit offsets in lockstep.

    do
    {
        foundExit = foundEnter = false;

        if (nextExitScope)
        {
            (this->*exitScopeFn)(inScope, nextExitScope);
            nextExitScope = nullptr;
            foundExit     = true;
        }

        offs = nextEnterScope ? nextEnterScope->vsdLifeBeg : offset;

        while ((scope = compGetNextExitScope(offs, true)) != nullptr)
        {
            foundExit = true;

            if (!nextEnterScope || scope->vsdLifeEnd > nextEnterScope->vsdLifeBeg)
            {
                // We overshot the last found Enter scope. Save the scope for later
                // and find an entering scope

                nextExitScope = scope;
                break;
            }

            (this->*exitScopeFn)(inScope, scope);
        }

        if (nextEnterScope)
        {
            (this->*enterScopeFn)(inScope, nextEnterScope);
            curEnterOffs   = nextEnterScope->vsdLifeBeg;
            nextEnterScope = nullptr;
            foundEnter     = true;
        }

        offs = nextExitScope ? nextExitScope->vsdLifeEnd : offset;

    START_FINDING_SCOPES:

        while ((scope = compGetNextEnterScope(offs, true)) != nullptr)
        {
            foundEnter = true;

            if ((nextExitScope && scope->vsdLifeBeg >= nextExitScope->vsdLifeEnd) || (scope->vsdLifeBeg > curEnterOffs))
            {
                // We overshot the last found exit scope. Save the scope for later
                // and find an exiting scope

                nextEnterScope = scope;
                break;
            }

            (this->*enterScopeFn)(inScope, scope);

            if (!nextExitScope)
            {
                curEnterOffs = scope->vsdLifeBeg;
            }
        }
    } while (foundExit || foundEnter);
}

#if defined(DEBUG)

void Compiler::compDispScopeLists()
{
    unsigned i;

    printf("Local variable scopes = %d\n", info.compVarScopesCount);

    if (info.compVarScopesCount)
    {
        printf("    \tVarNum \tLVNum \t      Name \tBeg \tEnd\n");
    }

    printf("Sorted by enter scope:\n");
    for (i = 0; i < info.compVarScopesCount; i++)
    {
        VarScopeDsc* varScope = compEnterScopeList[i];
        assert(varScope);
        printf("%2d: \t%02Xh \t%02Xh \t%10s \t%03Xh   \t%03Xh", i, varScope->vsdVarNum, varScope->vsdLVnum,
               VarNameToStr(varScope->vsdName) == nullptr ? "UNKNOWN" : VarNameToStr(varScope->vsdName),
               varScope->vsdLifeBeg, varScope->vsdLifeEnd);

        if (compNextEnterScope == i)
        {
            printf(" <-- next enter scope");
        }

        printf("\n");
    }

    printf("Sorted by exit scope:\n");
    for (i = 0; i < info.compVarScopesCount; i++)
    {
        VarScopeDsc* varScope = compExitScopeList[i];
        assert(varScope);
        printf("%2d: \t%02Xh \t%02Xh \t%10s \t%03Xh   \t%03Xh", i, varScope->vsdVarNum, varScope->vsdLVnum,
               VarNameToStr(varScope->vsdName) == nullptr ? "UNKNOWN" : VarNameToStr(varScope->vsdName),
               varScope->vsdLifeBeg, varScope->vsdLifeEnd);

        if (compNextExitScope == i)
        {
            printf(" <-- next exit scope");
        }

        printf("\n");
    }
}

void Compiler::compDispLocalVars()
{
    printf("info.compVarScopesCount = %d\n", info.compVarScopesCount);

    if (info.compVarScopesCount > 0)
    {
        printf("    \tVarNum \tLVNum \t      Name \tBeg \tEnd\n");
    }

    for (unsigned i = 0; i < info.compVarScopesCount; i++)
    {
        VarScopeDsc* varScope = &info.compVarScopes[i];
        printf("%2d: \t%02Xh \t%02Xh \t%10s \t%03Xh   \t%03Xh\n", i, varScope->vsdVarNum, varScope->vsdLVnum,
               VarNameToStr(varScope->vsdName) == nullptr ? "UNKNOWN" : VarNameToStr(varScope->vsdName),
               varScope->vsdLifeBeg, varScope->vsdLifeEnd);
    }
}

#endif // DEBUG

/*****************************************************************************/

#if MEASURE_CLRAPI_CALLS

struct WrapICorJitInfo : public ICorJitInfo
{
    //------------------------------------------------------------------------
    // WrapICorJitInfo::makeOne: allocate an instance of WrapICorJitInfo
    //
    // Arguments:
    //    alloc      - the allocator to get memory from for the instance
    //    compile    - the compiler instance
    //    compHndRef - the ICorJitInfo handle from the EE; the caller's
    //                 copy may be replaced with a "wrapper" instance
    //
    // Return Value:
    //    If the config flags indicate that ICorJitInfo should be wrapped,
    //    we return the "wrapper" instance; otherwise we return "nullptr".

    static WrapICorJitInfo* makeOne(ArenaAllocator* alloc, Compiler* compiler, COMP_HANDLE& compHndRef /* INOUT */)
    {
        WrapICorJitInfo* wrap = nullptr;

        if (JitConfig.JitEECallTimingInfo() != 0)
        {
            // It's too early to use the default allocator, so we do this
            // in two steps to be safe (the constructor doesn't need to do
            // anything except fill in the vtable pointer, so we let the
            // compiler do it).
            void* inst = alloc->allocateMemory(roundUp(sizeof(WrapICorJitInfo)));
            if (inst != nullptr)
            {
                // If you get a build error here due to 'WrapICorJitInfo' being
                // an abstract class, it's very likely that the wrapper bodies
                // in ICorJitInfo_API_wrapper.hpp are no longer in sync with
                // the EE interface; please be kind and update the header file.
                wrap = new (inst, jitstd::placement_t()) WrapICorJitInfo();

                wrap->wrapComp = compiler;

                // Save the real handle and replace it with our wrapped version.
                wrap->wrapHnd = compHndRef;
                compHndRef    = wrap;
            }
        }

        return wrap;
    }

private:
    Compiler*   wrapComp;
    COMP_HANDLE wrapHnd; // the "real thing"

public:
#include "ICorJitInfo_API_wrapper.hpp"
};

#endif // MEASURE_CLRAPI_CALLS

/*****************************************************************************/

// Compile a single method

int jitNativeCode(CORINFO_METHOD_HANDLE methodHnd,
                  CORINFO_MODULE_HANDLE classPtr,
                  COMP_HANDLE           compHnd,
                  CORINFO_METHOD_INFO*  methodInfo,
                  void**                methodCodePtr,
                  ULONG*                methodCodeSize,
                  JitFlags*             compileFlags,
                  void*                 inlineInfoPtr)
{
    //
    // A non-NULL inlineInfo means we are compiling the inlinee method.
    //
    InlineInfo* inlineInfo = (InlineInfo*)inlineInfoPtr;

    bool jitFallbackCompile = false;
START:
    int result = CORJIT_INTERNALERROR;

    ArenaAllocator* pAlloc = nullptr;
    ArenaAllocator  alloc;

#if MEASURE_CLRAPI_CALLS
    WrapICorJitInfo* wrapCLR = nullptr;
#endif

    if (inlineInfo)
    {
        // Use inliner's memory allocator when compiling the inlinee.
        pAlloc = inlineInfo->InlinerCompiler->compGetArenaAllocator();
    }
    else
    {
        pAlloc = &alloc;
    }

    Compiler* pComp;
    pComp = nullptr;

    struct Param
    {
        Compiler*       pComp;
        ArenaAllocator* pAlloc;
        bool            jitFallbackCompile;

        CORINFO_METHOD_HANDLE methodHnd;
        CORINFO_MODULE_HANDLE classPtr;
        COMP_HANDLE           compHnd;
        CORINFO_METHOD_INFO*  methodInfo;
        void**                methodCodePtr;
        ULONG*                methodCodeSize;
        JitFlags*             compileFlags;
        InlineInfo*           inlineInfo;
#if MEASURE_CLRAPI_CALLS
        WrapICorJitInfo* wrapCLR;
#endif

        int result;
    } param;
    param.pComp              = nullptr;
    param.pAlloc             = pAlloc;
    param.jitFallbackCompile = jitFallbackCompile;
    param.methodHnd          = methodHnd;
    param.classPtr           = classPtr;
    param.compHnd            = compHnd;
    param.methodInfo         = methodInfo;
    param.methodCodePtr      = methodCodePtr;
    param.methodCodeSize     = methodCodeSize;
    param.compileFlags       = compileFlags;
    param.inlineInfo         = inlineInfo;
#if MEASURE_CLRAPI_CALLS
    param.wrapCLR = nullptr;
#endif
    param.result = result;

    setErrorTrap(compHnd, Param*, pParamOuter, &param)
    {
        setErrorTrap(nullptr, Param*, pParam, pParamOuter)
        {
            if (pParam->inlineInfo)
            {
                // Lazily create the inlinee compiler object
                if (pParam->inlineInfo->InlinerCompiler->InlineeCompiler == nullptr)
                {
                    pParam->inlineInfo->InlinerCompiler->InlineeCompiler =
                        (Compiler*)pParam->pAlloc->allocateMemory(roundUp(sizeof(*pParam->pComp)));
                }

                // Use the inlinee compiler object
                pParam->pComp = pParam->inlineInfo->InlinerCompiler->InlineeCompiler;
#ifdef DEBUG
// memset(pParam->pComp, 0xEE, sizeof(Compiler));
#endif
            }
            else
            {
                // Allocate create the inliner compiler object
                pParam->pComp = (Compiler*)pParam->pAlloc->allocateMemory(roundUp(sizeof(*pParam->pComp)));
            }

#if MEASURE_CLRAPI_CALLS
            pParam->wrapCLR = WrapICorJitInfo::makeOne(pParam->pAlloc, pParam->pComp, pParam->compHnd);
#endif

            // push this compiler on the stack (TLS)
            pParam->pComp->prevCompiler = JitTls::GetCompiler();
            JitTls::SetCompiler(pParam->pComp);

// PREFIX_ASSUME gets turned into ASSERT_CHECK and we cannot have it here
#if defined(_PREFAST_) || defined(_PREFIX_)
            PREFIX_ASSUME(pParam->pComp != NULL);
#else
            assert(pParam->pComp != nullptr);
#endif

            pParam->pComp->compInit(pParam->pAlloc, pParam->inlineInfo);

#ifdef DEBUG
            pParam->pComp->jitFallbackCompile = pParam->jitFallbackCompile;
#endif

            // Now generate the code
            pParam->result =
                pParam->pComp->compCompile(pParam->methodHnd, pParam->classPtr, pParam->compHnd, pParam->methodInfo,
                                           pParam->methodCodePtr, pParam->methodCodeSize, pParam->compileFlags);
        }
        finallyErrorTrap()
        {
            Compiler* pCompiler = pParamOuter->pComp;

            // If OOM is thrown when allocating memory for a pComp, we will end up here.
            // For this case, pComp and also pCompiler will be a nullptr
            //
            if (pCompiler != nullptr)
            {
                pCompiler->info.compCode = nullptr;

                // pop the compiler off the TLS stack only if it was linked above
                assert(JitTls::GetCompiler() == pCompiler);
                JitTls::SetCompiler(pCompiler->prevCompiler);
            }

            if (pParamOuter->inlineInfo == nullptr)
            {
                // Free up the allocator we were using
                pParamOuter->pAlloc->destroy();
            }
        }
        endErrorTrap()
    }
    impJitErrorTrap()
    {
        // If we were looking at an inlinee....
        if (inlineInfo != nullptr)
        {
            // Note that we failed to compile the inlinee, and that
            // there's no point trying to inline it again anywhere else.
            inlineInfo->inlineResult->NoteFatal(InlineObservation::CALLEE_COMPILATION_ERROR);
        }
        param.result = __errc;
    }
    endErrorTrap()

        result = param.result;

    if (!inlineInfo && (result == CORJIT_INTERNALERROR || result == CORJIT_RECOVERABLEERROR) && !jitFallbackCompile)
    {
        // If we failed the JIT, reattempt with debuggable code.
        jitFallbackCompile = true;

        // Update the flags for 'safer' code generation.
        compileFlags->Set(JitFlags::JIT_FLAG_MIN_OPT);
        compileFlags->Clear(JitFlags::JIT_FLAG_SIZE_OPT);
        compileFlags->Clear(JitFlags::JIT_FLAG_SPEED_OPT);

        goto START;
    }

    return result;
}

#if defined(UNIX_AMD64_ABI)

// GetTypeFromClassificationAndSizes:
//   Returns the type of the eightbyte accounting for the classification and size of the eightbyte.
//
// args:
//   classType: classification type
//   size: size of the eightbyte.
//
// static
var_types Compiler::GetTypeFromClassificationAndSizes(SystemVClassificationType classType, int size)
{
    var_types type = TYP_UNKNOWN;
    switch (classType)
    {
        case SystemVClassificationTypeInteger:
            if (size == 1)
            {
                type = TYP_BYTE;
            }
            else if (size <= 2)
            {
                type = TYP_SHORT;
            }
            else if (size <= 4)
            {
                type = TYP_INT;
            }
            else if (size <= 8)
            {
                type = TYP_LONG;
            }
            else
            {
                assert(false && "GetTypeFromClassificationAndSizes Invalid Integer classification type.");
            }
            break;
        case SystemVClassificationTypeIntegerReference:
            type = TYP_REF;
            break;
        case SystemVClassificationTypeIntegerByRef:
            type = TYP_BYREF;
            break;
        case SystemVClassificationTypeSSE:
            if (size <= 4)
            {
                type = TYP_FLOAT;
            }
            else if (size <= 8)
            {
                type = TYP_DOUBLE;
            }
            else
            {
                assert(false && "GetTypeFromClassificationAndSizes Invalid SSE classification type.");
            }
            break;

        default:
            assert(false && "GetTypeFromClassificationAndSizes Invalid classification type.");
            break;
    }

    return type;
}

//-------------------------------------------------------------------
// GetEightByteType: Returns the type of eightbyte slot of a struct
//
// Arguments:
//   structDesc  -  struct classification description.
//   slotNum     -  eightbyte slot number for the struct.
//
// Return Value:
//    type of the eightbyte slot of the struct
//
// static
var_types Compiler::GetEightByteType(const SYSTEMV_AMD64_CORINFO_STRUCT_REG_PASSING_DESCRIPTOR& structDesc,
                                     unsigned                                                   slotNum)
{
    var_types eightByteType = TYP_UNDEF;
    unsigned  len           = structDesc.eightByteSizes[slotNum];

    switch (structDesc.eightByteClassifications[slotNum])
    {
        case SystemVClassificationTypeInteger:
            // See typelist.h for jit type definition.
            // All the types of size < 4 bytes are of jit type TYP_INT.
            if (structDesc.eightByteSizes[slotNum] <= 4)
            {
                eightByteType = TYP_INT;
            }
            else if (structDesc.eightByteSizes[slotNum] <= 8)
            {
                eightByteType = TYP_LONG;
            }
            else
            {
                assert(false && "GetEightByteType Invalid Integer classification type.");
            }
            break;
        case SystemVClassificationTypeIntegerReference:
            assert(len == REGSIZE_BYTES);
            eightByteType = TYP_REF;
            break;
        case SystemVClassificationTypeIntegerByRef:
            assert(len == REGSIZE_BYTES);
            eightByteType = TYP_BYREF;
            break;
        case SystemVClassificationTypeSSE:
            if (structDesc.eightByteSizes[slotNum] <= 4)
            {
                eightByteType = TYP_FLOAT;
            }
            else if (structDesc.eightByteSizes[slotNum] <= 8)
            {
                eightByteType = TYP_DOUBLE;
            }
            else
            {
                assert(false && "GetEightByteType Invalid SSE classification type.");
            }
            break;
        default:
            assert(false && "GetEightByteType Invalid classification type.");
            break;
    }

    return eightByteType;
}

//------------------------------------------------------------------------------------------------------
// GetStructTypeOffset: Gets the type, size and offset of the eightbytes of a struct for System V systems.
//
// Arguments:
//    'structDesc' -  struct description
//    'type0'      -  out param; returns the type of the first eightbyte.
//    'type1'      -  out param; returns the type of the second eightbyte.
//    'offset0'    -  out param; returns the offset of the first eightbyte.
//    'offset1'    -  out param; returns the offset of the second eightbyte.
//
// static
void Compiler::GetStructTypeOffset(const SYSTEMV_AMD64_CORINFO_STRUCT_REG_PASSING_DESCRIPTOR& structDesc,
                                   var_types*                                                 type0,
                                   var_types*                                                 type1,
                                   unsigned __int8*                                           offset0,
                                   unsigned __int8*                                           offset1)
{
    *offset0 = structDesc.eightByteOffsets[0];
    *offset1 = structDesc.eightByteOffsets[1];

    *type0 = TYP_UNKNOWN;
    *type1 = TYP_UNKNOWN;

    // Set the first eightbyte data
    if (structDesc.eightByteCount >= 1)
    {
        *type0 = GetEightByteType(structDesc, 0);
    }

    // Set the second eight byte data
    if (structDesc.eightByteCount == 2)
    {
        *type1 = GetEightByteType(structDesc, 1);
    }
}

//------------------------------------------------------------------------------------------------------
// GetStructTypeOffset: Gets the type, size and offset of the eightbytes of a struct for System V systems.
//
// Arguments:
//    'typeHnd'    -  type handle
//    'type0'      -  out param; returns the type of the first eightbyte.
//    'type1'      -  out param; returns the type of the second eightbyte.
//    'offset0'    -  out param; returns the offset of the first eightbyte.
//    'offset1'    -  out param; returns the offset of the second eightbyte.
//
void Compiler::GetStructTypeOffset(CORINFO_CLASS_HANDLE typeHnd,
                                   var_types*           type0,
                                   var_types*           type1,
                                   unsigned __int8*     offset0,
                                   unsigned __int8*     offset1)
{
    SYSTEMV_AMD64_CORINFO_STRUCT_REG_PASSING_DESCRIPTOR structDesc;
    eeGetSystemVAmd64PassStructInRegisterDescriptor(typeHnd, &structDesc);
    assert(structDesc.passedInRegisters);
    GetStructTypeOffset(structDesc, type0, type1, offset0, offset1);
}

#endif // defined(UNIX_AMD64_ABI)

/*****************************************************************************/
/*****************************************************************************/

#ifdef DEBUG
Compiler::NodeToIntMap* Compiler::FindReachableNodesInNodeTestData()
{
    NodeToIntMap* reachable = new (getAllocatorDebugOnly()) NodeToIntMap(getAllocatorDebugOnly());

    if (m_nodeTestData == nullptr)
    {
        return reachable;
    }

    // Otherwise, iterate.

    for (BasicBlock* block = fgFirstBB; block != nullptr; block = block->bbNext)
    {
        for (GenTreeStmt* stmt = block->FirstNonPhiDef(); stmt != nullptr; stmt = stmt->getNextStmt())
        {
            for (GenTree* tree = stmt->gtStmtList; tree != nullptr; tree = tree->gtNext)
            {
                TestLabelAndNum tlAndN;

                // For call nodes, translate late args to what they stand for.
                if (tree->OperGet() == GT_CALL)
                {
                    GenTreeCall*    call = tree->AsCall();
                    GenTreeArgList* args = call->gtCallArgs;
                    unsigned        i    = 0;
                    while (args != nullptr)
                    {
                        GenTree* arg = args->Current();
                        if (arg->gtFlags & GTF_LATE_ARG)
                        {
                            // Find the corresponding late arg.
                            GenTree* lateArg = call->fgArgInfo->GetArgNode(i);
                            if (GetNodeTestData()->Lookup(lateArg, &tlAndN))
                            {
                                reachable->Set(lateArg, 0);
                            }
                        }
                        i++;
                        args = args->Rest();
                    }
                }

                if (GetNodeTestData()->Lookup(tree, &tlAndN))
                {
                    reachable->Set(tree, 0);
                }
            }
        }
    }
    return reachable;
}

void Compiler::TransferTestDataToNode(GenTree* from, GenTree* to)
{
    TestLabelAndNum tlAndN;
    // We can't currently associate multiple annotations with a single node.
    // If we need to, we can fix this...

    // If the table is null, don't create it just to do the lookup, which would fail...
    if (m_nodeTestData != nullptr && GetNodeTestData()->Lookup(from, &tlAndN))
    {
        assert(!GetNodeTestData()->Lookup(to, &tlAndN));
        // We can't currently associate multiple annotations with a single node.
        // If we need to, we can fix this...
        TestLabelAndNum tlAndNTo;
        assert(!GetNodeTestData()->Lookup(to, &tlAndNTo));

        GetNodeTestData()->Remove(from);
        GetNodeTestData()->Set(to, tlAndN);
    }
}

void Compiler::CopyTestDataToCloneTree(GenTree* from, GenTree* to)
{
    if (m_nodeTestData == nullptr)
    {
        return;
    }
    if (from == nullptr)
    {
        assert(to == nullptr);
        return;
    }
    // Otherwise...
    TestLabelAndNum tlAndN;
    if (GetNodeTestData()->Lookup(from, &tlAndN))
    {
        // We can't currently associate multiple annotations with a single node.
        // If we need to, we can fix this...
        TestLabelAndNum tlAndNTo;
        assert(!GetNodeTestData()->Lookup(to, &tlAndNTo));
        GetNodeTestData()->Set(to, tlAndN);
    }
    // Now recurse, in parallel on both trees.

    genTreeOps oper = from->OperGet();
    unsigned   kind = from->OperKind();
    assert(oper == to->OperGet());

    // Cconstant or leaf nodes have no children.
    if (kind & (GTK_CONST | GTK_LEAF))
    {
        return;
    }

    // Otherwise, is it a 'simple' unary/binary operator?

    if (kind & GTK_SMPOP)
    {
        if (from->gtOp.gtOp1 != nullptr)
        {
            assert(to->gtOp.gtOp1 != nullptr);
            CopyTestDataToCloneTree(from->gtOp.gtOp1, to->gtOp.gtOp1);
        }
        else
        {
            assert(to->gtOp.gtOp1 == nullptr);
        }

        if (from->gtGetOp2IfPresent() != nullptr)
        {
            assert(to->gtGetOp2IfPresent() != nullptr);
            CopyTestDataToCloneTree(from->gtGetOp2(), to->gtGetOp2());
        }
        else
        {
            assert(to->gtGetOp2IfPresent() == nullptr);
        }

        return;
    }

    // Otherwise, see what kind of a special operator we have here.

    switch (oper)
    {
        case GT_STMT:
            CopyTestDataToCloneTree(from->gtStmt.gtStmtExpr, to->gtStmt.gtStmtExpr);
            return;

        case GT_CALL:
            CopyTestDataToCloneTree(from->gtCall.gtCallObjp, to->gtCall.gtCallObjp);
            CopyTestDataToCloneTree(from->gtCall.gtCallArgs, to->gtCall.gtCallArgs);
            CopyTestDataToCloneTree(from->gtCall.gtCallLateArgs, to->gtCall.gtCallLateArgs);

            if (from->gtCall.gtCallType == CT_INDIRECT)
            {
                CopyTestDataToCloneTree(from->gtCall.gtCallCookie, to->gtCall.gtCallCookie);
                CopyTestDataToCloneTree(from->gtCall.gtCallAddr, to->gtCall.gtCallAddr);
            }
            // The other call types do not have additional GenTree arguments.

            return;

        case GT_FIELD:
            CopyTestDataToCloneTree(from->gtField.gtFldObj, to->gtField.gtFldObj);
            return;

        case GT_ARR_ELEM:
            assert(from->gtArrElem.gtArrRank == to->gtArrElem.gtArrRank);
            for (unsigned dim = 0; dim < from->gtArrElem.gtArrRank; dim++)
            {
                CopyTestDataToCloneTree(from->gtArrElem.gtArrInds[dim], to->gtArrElem.gtArrInds[dim]);
            }
            CopyTestDataToCloneTree(from->gtArrElem.gtArrObj, to->gtArrElem.gtArrObj);
            return;

        case GT_CMPXCHG:
            CopyTestDataToCloneTree(from->gtCmpXchg.gtOpLocation, to->gtCmpXchg.gtOpLocation);
            CopyTestDataToCloneTree(from->gtCmpXchg.gtOpValue, to->gtCmpXchg.gtOpValue);
            CopyTestDataToCloneTree(from->gtCmpXchg.gtOpComparand, to->gtCmpXchg.gtOpComparand);
            return;

        case GT_ARR_BOUNDS_CHECK:
#ifdef FEATURE_SIMD
        case GT_SIMD_CHK:
#endif // FEATURE_SIMD
#ifdef FEATURE_HW_INTRINSICS
        case GT_HW_INTRINSIC_CHK:
#endif // FEATURE_HW_INTRINSICS
            CopyTestDataToCloneTree(from->gtBoundsChk.gtIndex, to->gtBoundsChk.gtIndex);
            CopyTestDataToCloneTree(from->gtBoundsChk.gtArrLen, to->gtBoundsChk.gtArrLen);
            return;

        default:
            unreached();
    }
}

#endif // DEBUG

/*
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XX                                                                           XX
XX                          jvc                                              XX
XX                                                                           XX
XX  Functions for the stand-alone version of the JIT .                       XX
XX                                                                           XX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
*/

/*****************************************************************************/
void codeGeneratorCodeSizeBeg()
{
}

/*****************************************************************************
 *
 *  Used for counting pointer assignments.
 */

/*****************************************************************************/
void codeGeneratorCodeSizeEnd()
{
}
/*****************************************************************************
 *
 *  Gather statistics - mainly used for the standalone
 *  Enable various #ifdef's to get the information you need
 */

void Compiler::compJitStats()
{
#if CALL_ARG_STATS

    /* Method types and argument statistics */
    compCallArgStats();
#endif // CALL_ARG_STATS
}

#if CALL_ARG_STATS

/*****************************************************************************
 *
 *  Gather statistics about method calls and arguments
 */

void Compiler::compCallArgStats()
{
    GenTree* args;
    GenTree* argx;

    unsigned argNum;

    unsigned argDWordNum;
    unsigned argLngNum;
    unsigned argFltNum;
    unsigned argDblNum;

    unsigned regArgNum;
    unsigned regArgDeferred;
    unsigned regArgTemp;

    unsigned regArgLclVar;
    unsigned regArgConst;

    unsigned argTempsThisMethod = 0;

    assert(fgStmtListThreaded);

    for (BasicBlock* block = fgFirstBB; block != nullptr; block = block->bbNext)
    {
        for (GenTreeStmt* stmt = block->firstStmt(); stmt != nullptr; stmt = stmt->getNextStmt())
        {
            for (GenTree* call = stmt->gtStmtList; call != nullptr; call = call->gtNext)
            {
                if (call->gtOper != GT_CALL)
                    continue;

                argNum =

                    regArgNum = regArgDeferred = regArgTemp =

                        regArgConst = regArgLclVar =

                            argDWordNum = argLngNum = argFltNum = argDblNum = 0;

                argTotalCalls++;

                if (!call->gtCall.gtCallObjp)
                {
                    if (call->gtCall.gtCallType == CT_HELPER)
                    {
                        argHelperCalls++;
                    }
                    else
                    {
                        argStaticCalls++;
                    }
                }
                else
                {
                    /* We have a 'this' pointer */

                    argDWordNum++;
                    argNum++;
                    regArgNum++;
                    regArgDeferred++;
                    argTotalObjPtr++;

                    if (call->IsVirtual())
                    {
                        /* virtual function */
                        argVirtualCalls++;
                    }
                    else
                    {
                        argNonVirtualCalls++;
                    }
                }
            }
        }
    }

    argTempsCntTable.record(argTempsThisMethod);

    if (argMaxTempsPerMethod < argTempsThisMethod)
    {
        argMaxTempsPerMethod = argTempsThisMethod;
    }
}

/* static */
void Compiler::compDispCallArgStats(FILE* fout)
{
    if (argTotalCalls == 0)
        return;

    fprintf(fout, "\n");
    fprintf(fout, "--------------------------------------------------\n");
    fprintf(fout, "Call stats\n");
    fprintf(fout, "--------------------------------------------------\n");
    fprintf(fout, "Total # of calls = %d, calls / method = %.3f\n\n", argTotalCalls,
            (float)argTotalCalls / genMethodCnt);

    fprintf(fout, "Percentage of      helper calls = %4.2f %%\n", (float)(100 * argHelperCalls) / argTotalCalls);
    fprintf(fout, "Percentage of      static calls = %4.2f %%\n", (float)(100 * argStaticCalls) / argTotalCalls);
    fprintf(fout, "Percentage of     virtual calls = %4.2f %%\n", (float)(100 * argVirtualCalls) / argTotalCalls);
    fprintf(fout, "Percentage of non-virtual calls = %4.2f %%\n\n", (float)(100 * argNonVirtualCalls) / argTotalCalls);

    fprintf(fout, "Average # of arguments per call = %.2f%%\n\n", (float)argTotalArgs / argTotalCalls);

    fprintf(fout, "Percentage of DWORD  arguments   = %.2f %%\n", (float)(100 * argTotalDWordArgs) / argTotalArgs);
    fprintf(fout, "Percentage of LONG   arguments   = %.2f %%\n", (float)(100 * argTotalLongArgs) / argTotalArgs);
    fprintf(fout, "Percentage of FLOAT  arguments   = %.2f %%\n", (float)(100 * argTotalFloatArgs) / argTotalArgs);
    fprintf(fout, "Percentage of DOUBLE arguments   = %.2f %%\n\n", (float)(100 * argTotalDoubleArgs) / argTotalArgs);

    if (argTotalRegArgs == 0)
        return;

    /*
        fprintf(fout, "Total deferred arguments     = %d \n", argTotalDeferred);

        fprintf(fout, "Total temp arguments         = %d \n\n", argTotalTemps);

        fprintf(fout, "Total 'this' arguments       = %d \n", argTotalObjPtr);
        fprintf(fout, "Total local var arguments    = %d \n", argTotalLclVar);
        fprintf(fout, "Total constant arguments     = %d \n\n", argTotalConst);
    */

    fprintf(fout, "\nRegister Arguments:\n\n");

    fprintf(fout, "Percentage of deferred arguments = %.2f %%\n", (float)(100 * argTotalDeferred) / argTotalRegArgs);
    fprintf(fout, "Percentage of temp arguments     = %.2f %%\n\n", (float)(100 * argTotalTemps) / argTotalRegArgs);

    fprintf(fout, "Maximum # of temps per method    = %d\n\n", argMaxTempsPerMethod);

    fprintf(fout, "Percentage of ObjPtr arguments   = %.2f %%\n", (float)(100 * argTotalObjPtr) / argTotalRegArgs);
    // fprintf(fout, "Percentage of global arguments   = %.2f %%\n", (float)(100 * argTotalDWordGlobEf) /
    // argTotalRegArgs);
    fprintf(fout, "Percentage of constant arguments = %.2f %%\n", (float)(100 * argTotalConst) / argTotalRegArgs);
    fprintf(fout, "Percentage of lcl var arguments  = %.2f %%\n\n", (float)(100 * argTotalLclVar) / argTotalRegArgs);

    fprintf(fout, "--------------------------------------------------\n");
    fprintf(fout, "Argument count frequency table (includes ObjPtr):\n");
    fprintf(fout, "--------------------------------------------------\n");
    argCntTable.dump(fout);
    fprintf(fout, "--------------------------------------------------\n");

    fprintf(fout, "--------------------------------------------------\n");
    fprintf(fout, "DWORD argument count frequency table (w/o LONG):\n");
    fprintf(fout, "--------------------------------------------------\n");
    argDWordCntTable.dump(fout);
    fprintf(fout, "--------------------------------------------------\n");

    fprintf(fout, "--------------------------------------------------\n");
    fprintf(fout, "Temps count frequency table (per method):\n");
    fprintf(fout, "--------------------------------------------------\n");
    argTempsCntTable.dump(fout);
    fprintf(fout, "--------------------------------------------------\n");

    /*
        fprintf(fout, "--------------------------------------------------\n");
        fprintf(fout, "DWORD argument count frequency table (w/ LONG):\n");
        fprintf(fout, "--------------------------------------------------\n");
        argDWordLngCntTable.dump(fout);
        fprintf(fout, "--------------------------------------------------\n");
    */
}

#endif // CALL_ARG_STATS

// JIT time end to end, and by phases.

#ifdef FEATURE_JIT_METHOD_PERF
// Static variables
CritSecObject       CompTimeSummaryInfo::s_compTimeSummaryLock;
CompTimeSummaryInfo CompTimeSummaryInfo::s_compTimeSummary;
#if MEASURE_CLRAPI_CALLS
double JitTimer::s_cyclesPerSec = CycleTimer::CyclesPerSecond();
#endif
#endif // FEATURE_JIT_METHOD_PERF

#if defined(FEATURE_JIT_METHOD_PERF) || DUMP_FLOWGRAPHS || defined(FEATURE_TRACELOGGING)
const char* PhaseNames[] = {
#define CompPhaseNameMacro(enum_nm, string_nm, short_nm, hasChildren, parent, measureIR) string_nm,
#include "compphases.h"
};

const char* PhaseEnums[] = {
#define CompPhaseNameMacro(enum_nm, string_nm, short_nm, hasChildren, parent, measureIR) #enum_nm,
#include "compphases.h"
};

const LPCWSTR PhaseShortNames[] = {
#define CompPhaseNameMacro(enum_nm, string_nm, short_nm, hasChildren, parent, measureIR) W(short_nm),
#include "compphases.h"
};
#endif // defined(FEATURE_JIT_METHOD_PERF) || DUMP_FLOWGRAPHS

#ifdef FEATURE_JIT_METHOD_PERF
bool PhaseHasChildren[] = {
#define CompPhaseNameMacro(enum_nm, string_nm, short_nm, hasChildren, parent, measureIR) hasChildren,
#include "compphases.h"
};

int PhaseParent[] = {
#define CompPhaseNameMacro(enum_nm, string_nm, short_nm, hasChildren, parent, measureIR) parent,
#include "compphases.h"
};

bool PhaseReportsIRSize[] = {
#define CompPhaseNameMacro(enum_nm, string_nm, short_nm, hasChildren, parent, measureIR) measureIR,
#include "compphases.h"
};

CompTimeInfo::CompTimeInfo(unsigned byteCodeBytes)
    : m_byteCodeBytes(byteCodeBytes)
    , m_totalCycles(0)
    , m_parentPhaseEndSlop(0)
    , m_timerFailure(false)
#if MEASURE_CLRAPI_CALLS
    , m_allClrAPIcalls(0)
    , m_allClrAPIcycles(0)
#endif
{
    for (int i = 0; i < PHASE_NUMBER_OF; i++)
    {
        m_invokesByPhase[i] = 0;
        m_cyclesByPhase[i]  = 0;
#if MEASURE_CLRAPI_CALLS
        m_CLRinvokesByPhase[i] = 0;
        m_CLRcyclesByPhase[i]  = 0;
#endif
    }

#if MEASURE_CLRAPI_CALLS
    assert(ARRAYSIZE(m_perClrAPIcalls) == API_ICorJitInfo_Names::API_COUNT);
    assert(ARRAYSIZE(m_perClrAPIcycles) == API_ICorJitInfo_Names::API_COUNT);
    assert(ARRAYSIZE(m_maxClrAPIcycles) == API_ICorJitInfo_Names::API_COUNT);
    for (int i = 0; i < API_ICorJitInfo_Names::API_COUNT; i++)
    {
        m_perClrAPIcalls[i]  = 0;
        m_perClrAPIcycles[i] = 0;
        m_maxClrAPIcycles[i] = 0;
    }
#endif
}

bool CompTimeSummaryInfo::IncludedInFilteredData(CompTimeInfo& info)
{
    return false; // info.m_byteCodeBytes < 10;
}

//------------------------------------------------------------------------
// CompTimeSummaryInfo::AddInfo: Record timing info from one compile.
//
// Arguments:
//    info          - The timing information to record.
//    includePhases - If "true", the per-phase info in "info" is valid,
//                    which means that a "normal" compile has ended; if
//                    the value is "false" we are recording the results
//                    of a partial compile (typically an import-only run
//                    on behalf of the inliner) in which case the phase
//                    info is not valid and so we only record EE call
//                    overhead.
void CompTimeSummaryInfo::AddInfo(CompTimeInfo& info, bool includePhases)
{
    if (info.m_timerFailure)
    {
        return; // Don't update if there was a failure.
    }

    CritSecHolder timeLock(s_compTimeSummaryLock);

    if (includePhases)
    {
        bool includeInFiltered = IncludedInFilteredData(info);

        m_numMethods++;

        // Update the totals and maxima.
        m_total.m_byteCodeBytes += info.m_byteCodeBytes;
        m_maximum.m_byteCodeBytes = max(m_maximum.m_byteCodeBytes, info.m_byteCodeBytes);
        m_total.m_totalCycles += info.m_totalCycles;
        m_maximum.m_totalCycles = max(m_maximum.m_totalCycles, info.m_totalCycles);

#if MEASURE_CLRAPI_CALLS
        // Update the CLR-API values.
        m_total.m_allClrAPIcalls += info.m_allClrAPIcalls;
        m_maximum.m_allClrAPIcalls = max(m_maximum.m_allClrAPIcalls, info.m_allClrAPIcalls);
        m_total.m_allClrAPIcycles += info.m_allClrAPIcycles;
        m_maximum.m_allClrAPIcycles = max(m_maximum.m_allClrAPIcycles, info.m_allClrAPIcycles);
#endif

        if (includeInFiltered)
        {
            m_numFilteredMethods++;
            m_filtered.m_byteCodeBytes += info.m_byteCodeBytes;
            m_filtered.m_totalCycles += info.m_totalCycles;
            m_filtered.m_parentPhaseEndSlop += info.m_parentPhaseEndSlop;
        }

        for (int i = 0; i < PHASE_NUMBER_OF; i++)
        {
            m_total.m_invokesByPhase[i] += info.m_invokesByPhase[i];
            m_total.m_cyclesByPhase[i] += info.m_cyclesByPhase[i];

#if MEASURE_CLRAPI_CALLS
            m_total.m_CLRinvokesByPhase[i] += info.m_CLRinvokesByPhase[i];
            m_total.m_CLRcyclesByPhase[i] += info.m_CLRcyclesByPhase[i];
#endif

            if (includeInFiltered)
            {
                m_filtered.m_invokesByPhase[i] += info.m_invokesByPhase[i];
                m_filtered.m_cyclesByPhase[i] += info.m_cyclesByPhase[i];
#if MEASURE_CLRAPI_CALLS
                m_filtered.m_CLRinvokesByPhase[i] += info.m_CLRinvokesByPhase[i];
                m_filtered.m_CLRcyclesByPhase[i] += info.m_CLRcyclesByPhase[i];
#endif
            }
            m_maximum.m_cyclesByPhase[i] = max(m_maximum.m_cyclesByPhase[i], info.m_cyclesByPhase[i]);

#if MEASURE_CLRAPI_CALLS
            m_maximum.m_CLRcyclesByPhase[i] = max(m_maximum.m_CLRcyclesByPhase[i], info.m_CLRcyclesByPhase[i]);
#endif
        }
        m_total.m_parentPhaseEndSlop += info.m_parentPhaseEndSlop;
        m_maximum.m_parentPhaseEndSlop = max(m_maximum.m_parentPhaseEndSlop, info.m_parentPhaseEndSlop);
    }
#if MEASURE_CLRAPI_CALLS
    else
    {
        m_totMethods++;

        // Update the "global" CLR-API values.
        m_total.m_allClrAPIcalls += info.m_allClrAPIcalls;
        m_maximum.m_allClrAPIcalls = max(m_maximum.m_allClrAPIcalls, info.m_allClrAPIcalls);
        m_total.m_allClrAPIcycles += info.m_allClrAPIcycles;
        m_maximum.m_allClrAPIcycles = max(m_maximum.m_allClrAPIcycles, info.m_allClrAPIcycles);

        // Update the per-phase CLR-API values.
        m_total.m_invokesByPhase[PHASE_CLR_API] += info.m_allClrAPIcalls;
        m_maximum.m_invokesByPhase[PHASE_CLR_API] =
            max(m_maximum.m_perClrAPIcalls[PHASE_CLR_API], info.m_allClrAPIcalls);
        m_total.m_cyclesByPhase[PHASE_CLR_API] += info.m_allClrAPIcycles;
        m_maximum.m_cyclesByPhase[PHASE_CLR_API] =
            max(m_maximum.m_cyclesByPhase[PHASE_CLR_API], info.m_allClrAPIcycles);
    }

    for (int i = 0; i < API_ICorJitInfo_Names::API_COUNT; i++)
    {
        m_total.m_perClrAPIcalls[i] += info.m_perClrAPIcalls[i];
        m_maximum.m_perClrAPIcalls[i] = max(m_maximum.m_perClrAPIcalls[i], info.m_perClrAPIcalls[i]);

        m_total.m_perClrAPIcycles[i] += info.m_perClrAPIcycles[i];
        m_maximum.m_perClrAPIcycles[i] = max(m_maximum.m_perClrAPIcycles[i], info.m_perClrAPIcycles[i]);

        m_maximum.m_maxClrAPIcycles[i] = max(m_maximum.m_maxClrAPIcycles[i], info.m_maxClrAPIcycles[i]);
    }
#endif
}

// Static
LPCWSTR Compiler::compJitTimeLogFilename = nullptr;

void CompTimeSummaryInfo::Print(FILE* f)
{
    if (f == nullptr)
    {
        return;
    }
    // Otherwise...
    double countsPerSec = CycleTimer::CyclesPerSecond();
    if (countsPerSec == 0.0)
    {
        fprintf(f, "Processor does not have a high-frequency timer.\n");
        return;
    }

    double totTime_ms = 0.0;

    fprintf(f, "JIT Compilation time report:\n");
    fprintf(f, "  Compiled %d methods.\n", m_numMethods);
    if (m_numMethods != 0)
    {
        fprintf(f, "  Compiled %d bytecodes total (%d max, %8.2f avg).\n", m_total.m_byteCodeBytes,
                m_maximum.m_byteCodeBytes, (double)m_total.m_byteCodeBytes / (double)m_numMethods);
        totTime_ms = ((double)m_total.m_totalCycles / countsPerSec) * 1000.0;
        fprintf(f, "  Time: total: %10.3f Mcycles/%10.3f ms\n", ((double)m_total.m_totalCycles / 1000000.0),
                totTime_ms);
        fprintf(f, "          max: %10.3f Mcycles/%10.3f ms\n", ((double)m_maximum.m_totalCycles) / 1000000.0,
                ((double)m_maximum.m_totalCycles / countsPerSec) * 1000.0);
        fprintf(f, "          avg: %10.3f Mcycles/%10.3f ms\n",
                ((double)m_total.m_totalCycles) / 1000000.0 / (double)m_numMethods, totTime_ms / (double)m_numMethods);

        const char* extraHdr1 = "";
        const char* extraHdr2 = "";
#if MEASURE_CLRAPI_CALLS
        bool extraInfo = (JitConfig.JitEECallTimingInfo() != 0);
        if (extraInfo)
        {
            extraHdr1 = "    CLRs/meth   % in CLR";
            extraHdr2 = "-----------------------";
        }
#endif

        fprintf(f, "\n  Total time by phases:\n");
        fprintf(f, "     PHASE                          inv/meth   Mcycles    time (ms)  %% of total    max (ms)%s\n",
                extraHdr1);
        fprintf(f, "     ---------------------------------------------------------------------------------------%s\n",
                extraHdr2);

        // Ensure that at least the names array and the Phases enum have the same number of entries:
        assert(_countof(PhaseNames) == PHASE_NUMBER_OF);
        for (int i = 0; i < PHASE_NUMBER_OF; i++)
        {
            double phase_tot_ms = (((double)m_total.m_cyclesByPhase[i]) / countsPerSec) * 1000.0;
            double phase_max_ms = (((double)m_maximum.m_cyclesByPhase[i]) / countsPerSec) * 1000.0;

#if MEASURE_CLRAPI_CALLS
            // Skip showing CLR API call info if we didn't collect any
            if (i == PHASE_CLR_API && !extraInfo)
                continue;
#endif

            // Indent nested phases, according to depth.
            int ancPhase = PhaseParent[i];
            while (ancPhase != -1)
            {
                fprintf(f, "  ");
                ancPhase = PhaseParent[ancPhase];
            }
            fprintf(f, "     %-30s %6.2f  %10.2f   %9.3f   %8.2f%%    %8.3f", PhaseNames[i],
                    ((double)m_total.m_invokesByPhase[i]) / ((double)m_numMethods),
                    ((double)m_total.m_cyclesByPhase[i]) / 1000000.0, phase_tot_ms, (phase_tot_ms * 100.0 / totTime_ms),
                    phase_max_ms);

#if MEASURE_CLRAPI_CALLS
            if (extraInfo && i != PHASE_CLR_API)
            {
                double nest_tot_ms  = (((double)m_total.m_CLRcyclesByPhase[i]) / countsPerSec) * 1000.0;
                double nest_percent = nest_tot_ms * 100.0 / totTime_ms;
                double calls_per_fn = ((double)m_total.m_CLRinvokesByPhase[i]) / ((double)m_numMethods);

                if (nest_percent > 0.1 || calls_per_fn > 10)
                    fprintf(f, "       %5.1f   %8.2f%%", calls_per_fn, nest_percent);
            }
#endif
            fprintf(f, "\n");
        }

        // Show slop if it's over a certain percentage of the total
        double pslop_pct = 100.0 * m_total.m_parentPhaseEndSlop * 1000.0 / countsPerSec / totTime_ms;
        if (pslop_pct >= 1.0)
        {
            fprintf(f, "\n  'End phase slop' should be very small (if not, there's unattributed time): %9.3f Mcycles = "
                       "%3.1f%% of total.\n\n",
                    m_total.m_parentPhaseEndSlop / 1000000.0, pslop_pct);
        }
    }
    if (m_numFilteredMethods > 0)
    {
        fprintf(f, "  Compiled %d methods that meet the filter requirement.\n", m_numFilteredMethods);
        fprintf(f, "  Compiled %d bytecodes total (%8.2f avg).\n", m_filtered.m_byteCodeBytes,
                (double)m_filtered.m_byteCodeBytes / (double)m_numFilteredMethods);
        double totTime_ms = ((double)m_filtered.m_totalCycles / countsPerSec) * 1000.0;
        fprintf(f, "  Time: total: %10.3f Mcycles/%10.3f ms\n", ((double)m_filtered.m_totalCycles / 1000000.0),
                totTime_ms);
        fprintf(f, "          avg: %10.3f Mcycles/%10.3f ms\n",
                ((double)m_filtered.m_totalCycles) / 1000000.0 / (double)m_numFilteredMethods,
                totTime_ms / (double)m_numFilteredMethods);

        fprintf(f, "  Total time by phases:\n");
        fprintf(f, "     PHASE                            inv/meth Mcycles    time (ms)  %% of total\n");
        fprintf(f, "     --------------------------------------------------------------------------------------\n");
        // Ensure that at least the names array and the Phases enum have the same number of entries:
        assert(_countof(PhaseNames) == PHASE_NUMBER_OF);
        for (int i = 0; i < PHASE_NUMBER_OF; i++)
        {
            double phase_tot_ms = (((double)m_filtered.m_cyclesByPhase[i]) / countsPerSec) * 1000.0;
            // Indent nested phases, according to depth.
            int ancPhase = PhaseParent[i];
            while (ancPhase != -1)
            {
                fprintf(f, "  ");
                ancPhase = PhaseParent[ancPhase];
            }
            fprintf(f, "     %-30s  %5.2f  %10.2f   %9.3f   %8.2f%%\n", PhaseNames[i],
                    ((double)m_filtered.m_invokesByPhase[i]) / ((double)m_numFilteredMethods),
                    ((double)m_filtered.m_cyclesByPhase[i]) / 1000000.0, phase_tot_ms,
                    (phase_tot_ms * 100.0 / totTime_ms));
        }

        double fslop_ms = m_filtered.m_parentPhaseEndSlop * 1000.0 / countsPerSec;
        if (fslop_ms > 1.0)
        {
            fprintf(f, "\n  'End phase slop' should be very small (if not, there's unattributed time): %9.3f Mcycles = "
                       "%3.1f%% of total.\n\n",
                    m_filtered.m_parentPhaseEndSlop / 1000000.0, fslop_ms);
        }
    }

#if MEASURE_CLRAPI_CALLS
    if (m_total.m_allClrAPIcalls > 0 && m_total.m_allClrAPIcycles > 0)
    {
        fprintf(f, "\n");
        if (m_totMethods > 0)
            fprintf(f, "  Imported %u methods.\n\n", m_numMethods + m_totMethods);

        fprintf(f, "     CLR API                                   # calls   total time    max time     avg time   %% "
                   "of total\n");
        fprintf(f, "     -------------------------------------------------------------------------------");
        fprintf(f, "---------------------\n");

        static const char* APInames[] = {
#define DEF_CLR_API(name) #name,
#include "ICorJitInfo_API_names.h"
        };

        unsigned shownCalls  = 0;
        double   shownMillis = 0.0;
#ifdef DEBUG
        unsigned checkedCalls  = 0;
        double   checkedMillis = 0.0;
#endif

        for (unsigned pass = 0; pass < 2; pass++)
        {
            for (unsigned i = 0; i < API_ICorJitInfo_Names::API_COUNT; i++)
            {
                unsigned calls = m_total.m_perClrAPIcalls[i];
                if (calls == 0)
                    continue;

                unsigned __int64 cycles = m_total.m_perClrAPIcycles[i];
                double           millis = 1000.0 * cycles / countsPerSec;

                // Don't show the small fry to keep the results manageable
                if (millis < 0.5)
                {
                    // We always show the following API because it is always called
                    // exactly once for each method and its body is the simplest one
                    // possible (it just returns an integer constant), and therefore
                    // it can be used to measure the overhead of adding the CLR API
                    // timing code. Roughly speaking, on a 3GHz x64 box the overhead
                    // per call should be around 40 ns when using RDTSC, compared to
                    // about 140 ns when using GetThreadCycles() under Windows.
                    if (i != API_ICorJitInfo_Names::API_getExpectedTargetArchitecture)
                        continue;
                }

                // In the first pass we just compute the totals.
                if (pass == 0)
                {
                    shownCalls += m_total.m_perClrAPIcalls[i];
                    shownMillis += millis;
                    continue;
                }

                unsigned __int32 maxcyc = m_maximum.m_maxClrAPIcycles[i];
                double           max_ms = 1000.0 * maxcyc / countsPerSec;

                fprintf(f, "     %-40s", APInames[i]);                                 // API name
                fprintf(f, " %8u %9.1f ms", calls, millis);                            // #calls, total time
                fprintf(f, " %8.1f ms  %8.1f ns", max_ms, 1000000.0 * millis / calls); // max, avg time
                fprintf(f, "     %5.1f%%\n", 100.0 * millis / shownMillis);            // % of total

#ifdef DEBUG
                checkedCalls += m_total.m_perClrAPIcalls[i];
                checkedMillis += millis;
#endif
            }
        }

#ifdef DEBUG
        assert(checkedCalls == shownCalls);
        assert(checkedMillis == shownMillis);
#endif

        if (shownCalls > 0 || shownMillis > 0)
        {
            fprintf(f, "     -------------------------");
            fprintf(f, "---------------------------------------------------------------------------\n");
            fprintf(f, "     Total for calls shown above              %8u %10.1f ms", shownCalls, shownMillis);
            if (totTime_ms > 0.0)
                fprintf(f, " (%4.1lf%% of overall JIT time)", shownMillis * 100.0 / totTime_ms);
            fprintf(f, "\n");
        }
        fprintf(f, "\n");
    }
#endif

    fprintf(f, "\n");
}

JitTimer::JitTimer(unsigned byteCodeSize) : m_info(byteCodeSize)
{
#if MEASURE_CLRAPI_CALLS
    m_CLRcallInvokes = 0;
    m_CLRcallCycles  = 0;
#endif

#ifdef DEBUG
    m_lastPhase = (Phases)-1;
#if MEASURE_CLRAPI_CALLS
    m_CLRcallAPInum = -1;
#endif
#endif

    unsigned __int64 threadCurCycles;
    if (_our_GetThreadCycles(&threadCurCycles))
    {
        m_start         = threadCurCycles;
        m_curPhaseStart = threadCurCycles;
    }
}

void JitTimer::EndPhase(Compiler* compiler, Phases phase)
{
    // Otherwise...
    // We re-run some phases currently, so this following assert doesn't work.
    // assert((int)phase > (int)m_lastPhase);  // We should end phases in increasing order.

    unsigned __int64 threadCurCycles;
    if (_our_GetThreadCycles(&threadCurCycles))
    {
        unsigned __int64 phaseCycles = (threadCurCycles - m_curPhaseStart);

        // If this is not a leaf phase, the assumption is that the last subphase must have just recently ended.
        // Credit the duration to "slop", the total of which should be very small.
        if (PhaseHasChildren[phase])
        {
            m_info.m_parentPhaseEndSlop += phaseCycles;
        }
        else
        {
            // It is a leaf phase.  Credit duration to it.
            m_info.m_invokesByPhase[phase]++;
            m_info.m_cyclesByPhase[phase] += phaseCycles;

#if MEASURE_CLRAPI_CALLS
            // Record the CLR API timing info as well.
            m_info.m_CLRinvokesByPhase[phase] += m_CLRcallInvokes;
            m_info.m_CLRcyclesByPhase[phase] += m_CLRcallCycles;
#endif

            // Credit the phase's ancestors, if any.
            int ancPhase = PhaseParent[phase];
            while (ancPhase != -1)
            {
                m_info.m_cyclesByPhase[ancPhase] += phaseCycles;
                ancPhase = PhaseParent[ancPhase];
            }

#if MEASURE_CLRAPI_CALLS
            const Phases lastPhase = PHASE_CLR_API;
#else
            const Phases lastPhase = PHASE_NUMBER_OF;
#endif
            if (phase + 1 == lastPhase)
            {
                m_info.m_totalCycles = (threadCurCycles - m_start);
            }
            else
            {
                m_curPhaseStart = threadCurCycles;
            }
        }

        if ((JitConfig.JitMeasureIR() != 0) && PhaseReportsIRSize[phase])
        {
            m_info.m_nodeCountAfterPhase[phase] = compiler->fgMeasureIR();
        }
        else
        {
            m_info.m_nodeCountAfterPhase[phase] = 0;
        }
    }

#ifdef DEBUG
    m_lastPhase = phase;
#endif
#if MEASURE_CLRAPI_CALLS
    m_CLRcallInvokes = 0;
    m_CLRcallCycles  = 0;
#endif
}

#if MEASURE_CLRAPI_CALLS

//------------------------------------------------------------------------
// JitTimer::CLRApiCallEnter: Start the stopwatch for an EE call.
//
// Arguments:
//    apix - The API index - an "enum API_ICorJitInfo_Names" value.
//

void JitTimer::CLRApiCallEnter(unsigned apix)
{
    assert(m_CLRcallAPInum == -1); // Nested calls not allowed
    m_CLRcallAPInum = apix;

    // If we can't get the cycles, we'll just ignore this call
    if (!_our_GetThreadCycles(&m_CLRcallStart))
        m_CLRcallStart = 0;
}

//------------------------------------------------------------------------
// JitTimer::CLRApiCallLeave: compute / record time spent in an EE call.
//
// Arguments:
//    apix - The API's "enum API_ICorJitInfo_Names" value; this value
//           should match the value passed to the most recent call to
//           "CLRApiCallEnter" (i.e. these must come as matched pairs),
//           and they also may not nest.
//

void JitTimer::CLRApiCallLeave(unsigned apix)
{
    // Make sure we're actually inside a measured CLR call.
    assert(m_CLRcallAPInum != -1);
    m_CLRcallAPInum = -1;

    // Ignore this one if we don't have a valid starting counter.
    if (m_CLRcallStart != 0)
    {
        if (JitConfig.JitEECallTimingInfo() != 0)
        {
            unsigned __int64 threadCurCycles;
            if (_our_GetThreadCycles(&threadCurCycles))
            {
                // Compute the cycles spent in the call.
                threadCurCycles -= m_CLRcallStart;

                // Add the cycles to the 'phase' and bump its use count.
                m_info.m_cyclesByPhase[PHASE_CLR_API] += threadCurCycles;
                m_info.m_invokesByPhase[PHASE_CLR_API] += 1;

                // Add the values to the "per API" info.
                m_info.m_allClrAPIcycles += threadCurCycles;
                m_info.m_allClrAPIcalls += 1;

                m_info.m_perClrAPIcalls[apix] += 1;
                m_info.m_perClrAPIcycles[apix] += threadCurCycles;
                m_info.m_maxClrAPIcycles[apix] = max(m_info.m_maxClrAPIcycles[apix], (unsigned __int32)threadCurCycles);

                // Subtract the cycles from the enclosing phase by bumping its start time
                m_curPhaseStart += threadCurCycles;

                // Update the running totals.
                m_CLRcallInvokes += 1;
                m_CLRcallCycles += threadCurCycles;
            }
        }

        m_CLRcallStart = 0;
    }

    assert(m_CLRcallAPInum != -1); // No longer in this API call.
    m_CLRcallAPInum = -1;
}

#endif // MEASURE_CLRAPI_CALLS

CritSecObject JitTimer::s_csvLock;

LPCWSTR Compiler::JitTimeLogCsv()
{
    LPCWSTR jitTimeLogCsv = JitConfig.JitTimeLogCsv();
    return jitTimeLogCsv;
}

void JitTimer::PrintCsvHeader()
{
    LPCWSTR jitTimeLogCsv = Compiler::JitTimeLogCsv();
    if (jitTimeLogCsv == nullptr)
    {
        return;
    }

    CritSecHolder csvLock(s_csvLock);

    FILE* fp = _wfopen(jitTimeLogCsv, W("a"));
    if (fp != nullptr)
    {
        // Seek to the end of the file s.t. `ftell` doesn't lie to us on Windows
        fseek(fp, 0, SEEK_END);

        // Write the header if the file is empty
        if (ftell(fp) == 0)
        {
            fprintf(fp, "\"Method Name\",");
            fprintf(fp, "\"Assembly or SPMI Index\",");
            fprintf(fp, "\"IL Bytes\",");
            fprintf(fp, "\"Basic Blocks\",");
            fprintf(fp, "\"Min Opts\",");
            fprintf(fp, "\"Loops Cloned\",");

            for (int i = 0; i < PHASE_NUMBER_OF; i++)
            {
                fprintf(fp, "\"%s\",", PhaseNames[i]);
                if ((JitConfig.JitMeasureIR() != 0) && PhaseReportsIRSize[i])
                {
                    fprintf(fp, "\"Node Count After %s\",", PhaseNames[i]);
                }
            }

            InlineStrategy::DumpCsvHeader(fp);

            fprintf(fp, "\"Executable Code Bytes\",");
            fprintf(fp, "\"GC Info Bytes\",");
            fprintf(fp, "\"Total Bytes Allocated\",");
            fprintf(fp, "\"Total Cycles\",");
            fprintf(fp, "\"CPS\"\n");
        }
        fclose(fp);
    }
}

extern ICorJitHost* g_jitHost;

void JitTimer::PrintCsvMethodStats(Compiler* comp)
{
    LPCWSTR jitTimeLogCsv = Compiler::JitTimeLogCsv();
    if (jitTimeLogCsv == nullptr)
    {
        return;
    }

    // eeGetMethodFullName uses locks, so don't enter crit sec before this call.
    const char* methName = comp->eeGetMethodFullName(comp->info.compMethodHnd);

    // Try and access the SPMI index to report in the data set.
    //
    // If the jit is not hosted under SPMI this will return the
    // default value of zero.
    //
    // Query the jit host directly here instead of going via the
    // config cache, since value will change for each method.
    int index = g_jitHost->getIntConfigValue(W("SuperPMIMethodContextNumber"), 0);

    CritSecHolder csvLock(s_csvLock);

    FILE* fp = _wfopen(jitTimeLogCsv, W("a"));
    fprintf(fp, "\"%s\",", methName);
    if (index != 0)
    {
        fprintf(fp, "%d,", index);
    }
    else
    {
        const char* methodAssemblyName = comp->info.compCompHnd->getAssemblyName(
            comp->info.compCompHnd->getModuleAssembly(comp->info.compCompHnd->getClassModule(comp->info.compClassHnd)));
        fprintf(fp, "\"%s\",", methodAssemblyName);
    }
    fprintf(fp, "%u,", comp->info.compILCodeSize);
    fprintf(fp, "%u,", comp->fgBBcount);
    fprintf(fp, "%u,", comp->opts.MinOpts());
    fprintf(fp, "%u,", comp->optLoopsCloned);
    unsigned __int64 totCycles = 0;
    for (int i = 0; i < PHASE_NUMBER_OF; i++)
    {
        if (!PhaseHasChildren[i])
        {
            totCycles += m_info.m_cyclesByPhase[i];
        }
        fprintf(fp, "%I64u,", m_info.m_cyclesByPhase[i]);

        if ((JitConfig.JitMeasureIR() != 0) && PhaseReportsIRSize[i])
        {
            fprintf(fp, "%u,", m_info.m_nodeCountAfterPhase[i]);
        }
    }

    comp->m_inlineStrategy->DumpCsvData(fp);

    fprintf(fp, "%u,", comp->info.compNativeCodeSize);
    fprintf(fp, "%Iu,", comp->compInfoBlkSize);
    fprintf(fp, "%Iu,", comp->compGetArenaAllocator()->getTotalBytesAllocated());
    fprintf(fp, "%I64u,", m_info.m_totalCycles);
    fprintf(fp, "%f\n", CycleTimer::CyclesPerSecond());
    fclose(fp);
}

// Completes the timing of the current method, and adds it to "sum".
void JitTimer::Terminate(Compiler* comp, CompTimeSummaryInfo& sum, bool includePhases)
{
    if (includePhases)
    {
        PrintCsvMethodStats(comp);
    }

    sum.AddInfo(m_info, includePhases);
}
#endif // FEATURE_JIT_METHOD_PERF

#if LOOP_HOIST_STATS
// Static fields.
CritSecObject Compiler::s_loopHoistStatsLock; // Default constructor.
unsigned      Compiler::s_loopsConsidered             = 0;
unsigned      Compiler::s_loopsWithHoistedExpressions = 0;
unsigned      Compiler::s_totalHoistedExpressions     = 0;

// static
void Compiler::PrintAggregateLoopHoistStats(FILE* f)
{
    fprintf(f, "\n");
    fprintf(f, "---------------------------------------------------\n");
    fprintf(f, "Loop hoisting stats\n");
    fprintf(f, "---------------------------------------------------\n");

    double pctWithHoisted = 0.0;
    if (s_loopsConsidered > 0)
    {
        pctWithHoisted = 100.0 * (double(s_loopsWithHoistedExpressions) / double(s_loopsConsidered));
    }
    double exprsPerLoopWithExpr = 0.0;
    if (s_loopsWithHoistedExpressions > 0)
    {
        exprsPerLoopWithExpr = double(s_totalHoistedExpressions) / double(s_loopsWithHoistedExpressions);
    }
    fprintf(f, "Considered %d loops.  Of these, we hoisted expressions out of %d (%6.2f%%).\n", s_loopsConsidered,
            s_loopsWithHoistedExpressions, pctWithHoisted);
    fprintf(f, "  A total of %d expressions were hoisted, an average of %5.2f per loop-with-hoisted-expr.\n",
            s_totalHoistedExpressions, exprsPerLoopWithExpr);
}

void Compiler::AddLoopHoistStats()
{
    CritSecHolder statsLock(s_loopHoistStatsLock);

    s_loopsConsidered += m_loopsConsidered;
    s_loopsWithHoistedExpressions += m_loopsWithHoistedExpressions;
    s_totalHoistedExpressions += m_totalHoistedExpressions;
}

void Compiler::PrintPerMethodLoopHoistStats()
{
    double pctWithHoisted = 0.0;
    if (m_loopsConsidered > 0)
    {
        pctWithHoisted = 100.0 * (double(m_loopsWithHoistedExpressions) / double(m_loopsConsidered));
    }
    double exprsPerLoopWithExpr = 0.0;
    if (m_loopsWithHoistedExpressions > 0)
    {
        exprsPerLoopWithExpr = double(m_totalHoistedExpressions) / double(m_loopsWithHoistedExpressions);
    }
    printf("Considered %d loops.  Of these, we hoisted expressions out of %d (%5.2f%%).\n", m_loopsConsidered,
           m_loopsWithHoistedExpressions, pctWithHoisted);
    printf("  A total of %d expressions were hoisted, an average of %5.2f per loop-with-hoisted-expr.\n",
           m_totalHoistedExpressions, exprsPerLoopWithExpr);
}
#endif // LOOP_HOIST_STATS

//------------------------------------------------------------------------
// RecordStateAtEndOfInlining: capture timing data (if enabled) after
// inlining as completed.
//
// Note:
// Records data needed for SQM and inlining data dumps.  Should be
// called after inlining is complete.  (We do this after inlining
// because this marks the last point at which the JIT is likely to
// cause type-loading and class initialization).

void Compiler::RecordStateAtEndOfInlining()
{
#if defined(DEBUG) || defined(INLINE_DATA) || defined(FEATURE_CLRSQM)

    m_compCyclesAtEndOfInlining    = 0;
    m_compTickCountAtEndOfInlining = 0;
    bool b                         = CycleTimer::GetThreadCyclesS(&m_compCyclesAtEndOfInlining);
    if (!b)
    {
        return; // We don't have a thread cycle counter.
    }
    m_compTickCountAtEndOfInlining = GetTickCount();

#endif // defined(DEBUG) || defined(INLINE_DATA) || defined(FEATURE_CLRSQM)
}

//------------------------------------------------------------------------
// RecordStateAtEndOfCompilation: capture timing data (if enabled) after
// compilation is completed.

void Compiler::RecordStateAtEndOfCompilation()
{
#if defined(DEBUG) || defined(INLINE_DATA) || defined(FEATURE_CLRSQM)

    // Common portion
    m_compCycles = 0;
    unsigned __int64 compCyclesAtEnd;
    bool             b = CycleTimer::GetThreadCyclesS(&compCyclesAtEnd);
    if (!b)
    {
        return; // We don't have a thread cycle counter.
    }
    assert(compCyclesAtEnd >= m_compCyclesAtEndOfInlining);

    m_compCycles = compCyclesAtEnd - m_compCyclesAtEndOfInlining;

#endif // defined(DEBUG) || defined(INLINE_DATA) || defined(FEATURE_CLRSQM)

#ifdef FEATURE_CLRSQM

    // SQM only portion
    unsigned __int64 mcycles64 = m_compCycles / ((unsigned __int64)1000000);
    unsigned         mcycles;
    if (mcycles64 > UINT32_MAX)
    {
        mcycles = UINT32_MAX;
    }
    else
    {
        mcycles = (unsigned)mcycles64;
    }

    DWORD ticksAtEnd = GetTickCount();
    assert(ticksAtEnd >= m_compTickCountAtEndOfInlining);
    DWORD compTicks = ticksAtEnd - m_compTickCountAtEndOfInlining;

    if (mcycles >= 1000)
    {
        info.compCompHnd->logSQMLongJitEvent(mcycles, compTicks, info.compILCodeSize, fgBBcount, opts.MinOpts(),
                                             info.compMethodHnd);
    }

#endif // FEATURE_CLRSQM
}

#if FUNC_INFO_LOGGING
// static
LPCWSTR Compiler::compJitFuncInfoFilename = nullptr;

// static
FILE* Compiler::compJitFuncInfoFile = nullptr;
#endif // FUNC_INFO_LOGGING

#ifdef DEBUG

// dumpConvertedVarSet() dumps the varset bits that are tracked
// variable indices, and we convert them to variable numbers, sort the variable numbers, and
// print them as variable numbers. To do this, we use a temporary set indexed by
// variable number. We can't use the "all varset" type because it is still size-limited, and might
// not be big enough to handle all possible variable numbers.
void dumpConvertedVarSet(Compiler* comp, VARSET_VALARG_TP vars)
{
    BYTE* pVarNumSet; // trivial set: one byte per varNum, 0 means not in set, 1 means in set.

    size_t varNumSetBytes = comp->lvaCount * sizeof(BYTE);
    pVarNumSet            = (BYTE*)_alloca(varNumSetBytes);
    memset(pVarNumSet, 0, varNumSetBytes); // empty the set

    VarSetOps::Iter iter(comp, vars);
    unsigned        varIndex = 0;
    while (iter.NextElem(&varIndex))
    {
        unsigned varNum = comp->lvaTrackedToVarNum[varIndex];
        assert(varNum < comp->lvaCount);
        pVarNumSet[varNum] = 1; // This varNum is in the set
    }

    bool first = true;
    printf("{");
    for (size_t varNum = 0; varNum < comp->lvaCount; varNum++)
    {
        if (pVarNumSet[varNum] == 1)
        {
            if (!first)
            {
                printf(" ");
            }
            printf("V%02u", varNum);
            first = false;
        }
    }
    printf("}");
}

/*XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XX                                                                           XX
XX                          Debugging helpers                                XX
XX                                                                           XX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
*/

/*****************************************************************************/
/* The following functions are intended to be called from the debugger, to dump
 * various data structures.
 *
 * The versions that start with 'c' take a Compiler* as the first argument.
 * The versions that start with 'd' use the tlsCompiler, so don't require a Compiler*.
 *
 * Summary:
 *      cBlock,      dBlock         : Display a basic block (call fgTableDispBasicBlock()).
 *      cBlocks,     dBlocks        : Display all the basic blocks of a function (call fgDispBasicBlocks()).
 *      cBlocksV,    dBlocksV       : Display all the basic blocks of a function (call fgDispBasicBlocks(true)).
 *                                    "V" means "verbose", and will dump all the trees.
 *      cTree,       dTree          : Display a tree (call gtDispTree()).
 *      cTreeLIR,    dTreeLIR       : Display a tree in LIR form (call gtDispLIRNode()).
 *      cTrees,      dTrees         : Display all the trees in a function (call fgDumpTrees()).
 *      cEH,         dEH            : Display the EH handler table (call fgDispHandlerTab()).
 *      cVar,        dVar           : Display a local variable given its number (call lvaDumpEntry()).
 *      cVarDsc,     dVarDsc        : Display a local variable given a LclVarDsc* (call lvaDumpEntry()).
 *      cVars,       dVars          : Display the local variable table (call lvaTableDump()).
 *      cVarsFinal,  dVarsFinal     : Display the local variable table (call lvaTableDump(FINAL_FRAME_LAYOUT)).
 *      cBlockCheapPreds, dBlockCheapPreds : Display a block's cheap predecessors (call block->dspCheapPreds()).
 *      cBlockPreds, dBlockPreds    : Display a block's predecessors (call block->dspPreds()).
 *      cBlockSuccs, dBlockSuccs    : Display a block's successors (call block->dspSuccs(compiler)).
 *      cReach,      dReach         : Display all block reachability (call fgDispReach()).
 *      cDoms,       dDoms          : Display all block dominators (call fgDispDoms()).
 *      cLiveness,   dLiveness      : Display per-block variable liveness (call fgDispBBLiveness()).
 *      cCVarSet,    dCVarSet       : Display a "converted" VARSET_TP: the varset is assumed to be tracked variable
 *                                    indices. These are converted to variable numbers and sorted. (Calls
 *                                    dumpConvertedVarSet()).
 *
 *      cFuncIR,     dFuncIR        : Display all the basic blocks of a function in linear IR form.
 *      cLoopIR,     dLoopIR        : Display a loop in linear IR form.
 *                   dLoopNumIR     : Display a loop (given number) in linear IR form.
 *      cBlockIR,    dBlockIR       : Display a basic block in linear IR form.
 *      cTreeIR,     dTreeIR        : Display a tree in linear IR form.
 *                   dTabStopIR     : Display spaces to the next tab stop column
 *      cTreeTypeIR  dTreeTypeIR    : Display tree type
 *      cTreeKindsIR dTreeKindsIR   : Display tree kinds
 *      cTreeFlagsIR dTreeFlagsIR   : Display tree flags
 *      cOperandIR   dOperandIR     : Display tree operand
 *      cLeafIR      dLeafIR        : Display tree leaf
 *      cIndirIR     dIndirIR       : Display indir tree as [t#] or [leaf]
 *      cListIR      dListIR        : Display tree list
 *      cSsaNumIR    dSsaNumIR      : Display SSA number as <u|d:#>
 *      cValNumIR    dValNumIR      : Display Value number as <v{l|c}:#{,R}>
 *      cDependsIR                  : Display dependencies of a tree DEP(t# ...) node
 *                                    based on child comma tree nodes
 *                   dFormatIR      : Display dump format specified on command line
 *
 *
 * The following don't require a Compiler* to work:
 *      dRegMask                    : Display a regMaskTP (call dspRegMask(mask)).
 */

void cBlock(Compiler* comp, BasicBlock* block)
{
    static unsigned sequenceNumber = 0; // separate calls with a number to indicate this function has been called
    printf("===================================================================== *Block %u\n", sequenceNumber++);
    comp->fgTableDispBasicBlock(block);
}

void cBlocks(Compiler* comp)
{
    static unsigned sequenceNumber = 0; // separate calls with a number to indicate this function has been called
    printf("===================================================================== *Blocks %u\n", sequenceNumber++);
    comp->fgDispBasicBlocks();
}

void cBlocksV(Compiler* comp)
{
    static unsigned sequenceNumber = 0; // separate calls with a number to indicate this function has been called
    printf("===================================================================== *BlocksV %u\n", sequenceNumber++);
    comp->fgDispBasicBlocks(true);
}

void cTree(Compiler* comp, GenTree* tree)
{
    static unsigned sequenceNumber = 0; // separate calls with a number to indicate this function has been called
    printf("===================================================================== *Tree %u\n", sequenceNumber++);
    comp->gtDispTree(tree, nullptr, ">>>");
}

void cTreeLIR(Compiler* comp, GenTree* tree)
{
    static unsigned sequenceNumber = 0; // separate calls with a number to indicate this function has been called
    printf("===================================================================== *TreeLIR %u\n", sequenceNumber++);
    comp->gtDispLIRNode(tree);
}

void cTrees(Compiler* comp)
{
    static unsigned sequenceNumber = 0; // separate calls with a number to indicate this function has been called
    printf("===================================================================== *Trees %u\n", sequenceNumber++);
    comp->fgDumpTrees(comp->fgFirstBB, nullptr);
}

void cEH(Compiler* comp)
{
    static unsigned sequenceNumber = 0; // separate calls with a number to indicate this function has been called
    printf("===================================================================== *EH %u\n", sequenceNumber++);
    comp->fgDispHandlerTab();
}

void cVar(Compiler* comp, unsigned lclNum)
{
    static unsigned sequenceNumber = 0; // separate calls with a number to indicate this function has been called
    printf("===================================================================== *Var %u\n", sequenceNumber++);
    comp->lvaDumpEntry(lclNum, Compiler::FINAL_FRAME_LAYOUT);
}

void cVarDsc(Compiler* comp, LclVarDsc* varDsc)
{
    static unsigned sequenceNumber = 0; // separate calls with a number to indicate this function has been called
    printf("===================================================================== *VarDsc %u\n", sequenceNumber++);
    unsigned lclNum = (unsigned)(varDsc - comp->lvaTable);
    comp->lvaDumpEntry(lclNum, Compiler::FINAL_FRAME_LAYOUT);
}

void cVars(Compiler* comp)
{
    static unsigned sequenceNumber = 0; // separate calls with a number to indicate this function has been called
    printf("===================================================================== *Vars %u\n", sequenceNumber++);
    comp->lvaTableDump();
}

void cVarsFinal(Compiler* comp)
{
    static unsigned sequenceNumber = 0; // separate calls with a number to indicate this function has been called
    printf("===================================================================== *Vars %u\n", sequenceNumber++);
    comp->lvaTableDump(Compiler::FINAL_FRAME_LAYOUT);
}

void cBlockCheapPreds(Compiler* comp, BasicBlock* block)
{
    static unsigned sequenceNumber = 0; // separate calls with a number to indicate this function has been called
    printf("===================================================================== *BlockCheapPreds %u\n",
           sequenceNumber++);
    block->dspCheapPreds();
}

void cBlockPreds(Compiler* comp, BasicBlock* block)
{
    static unsigned sequenceNumber = 0; // separate calls with a number to indicate this function has been called
    printf("===================================================================== *BlockPreds %u\n", sequenceNumber++);
    block->dspPreds();
}

void cBlockSuccs(Compiler* comp, BasicBlock* block)
{
    static unsigned sequenceNumber = 0; // separate calls with a number to indicate this function has been called
    printf("===================================================================== *BlockSuccs %u\n", sequenceNumber++);
    block->dspSuccs(comp);
}

void cReach(Compiler* comp)
{
    static unsigned sequenceNumber = 0; // separate calls with a number to indicate this function has been called
    printf("===================================================================== *Reach %u\n", sequenceNumber++);
    comp->fgDispReach();
}

void cDoms(Compiler* comp)
{
    static unsigned sequenceNumber = 0; // separate calls with a number to indicate this function has been called
    printf("===================================================================== *Doms %u\n", sequenceNumber++);
    comp->fgDispDoms();
}

void cLiveness(Compiler* comp)
{
    static unsigned sequenceNumber = 0; // separate calls with a number to indicate this function has been called
    printf("===================================================================== *Liveness %u\n", sequenceNumber++);
    comp->fgDispBBLiveness();
}

void cCVarSet(Compiler* comp, VARSET_VALARG_TP vars)
{
    static unsigned sequenceNumber = 0; // separate calls with a number to indicate this function has been called
    printf("===================================================================== dCVarSet %u\n", sequenceNumber++);
    dumpConvertedVarSet(comp, vars);
    printf("\n"); // dumpConvertedVarSet() doesn't emit a trailing newline
}

void dBlock(BasicBlock* block)
{
    cBlock(JitTls::GetCompiler(), block);
}

void dBlocks()
{
    cBlocks(JitTls::GetCompiler());
}

void dBlocksV()
{
    cBlocksV(JitTls::GetCompiler());
}

void dTree(GenTree* tree)
{
    cTree(JitTls::GetCompiler(), tree);
}

void dTreeLIR(GenTree* tree)
{
    cTreeLIR(JitTls::GetCompiler(), tree);
}

void dTrees()
{
    cTrees(JitTls::GetCompiler());
}

void dEH()
{
    cEH(JitTls::GetCompiler());
}

void dVar(unsigned lclNum)
{
    cVar(JitTls::GetCompiler(), lclNum);
}

void dVarDsc(LclVarDsc* varDsc)
{
    cVarDsc(JitTls::GetCompiler(), varDsc);
}

void dVars()
{
    cVars(JitTls::GetCompiler());
}

void dVarsFinal()
{
    cVarsFinal(JitTls::GetCompiler());
}

void dBlockPreds(BasicBlock* block)
{
    cBlockPreds(JitTls::GetCompiler(), block);
}

void dBlockCheapPreds(BasicBlock* block)
{
    cBlockCheapPreds(JitTls::GetCompiler(), block);
}

void dBlockSuccs(BasicBlock* block)
{
    cBlockSuccs(JitTls::GetCompiler(), block);
}

void dReach()
{
    cReach(JitTls::GetCompiler());
}

void dDoms()
{
    cDoms(JitTls::GetCompiler());
}

void dLiveness()
{
    cLiveness(JitTls::GetCompiler());
}

void dCVarSet(VARSET_VALARG_TP vars)
{
    cCVarSet(JitTls::GetCompiler(), vars);
}

void dRegMask(regMaskTP mask)
{
    static unsigned sequenceNumber = 0; // separate calls with a number to indicate this function has been called
    printf("===================================================================== dRegMask %u\n", sequenceNumber++);
    dspRegMask(mask);
    printf("\n"); // dspRegMask() doesn't emit a trailing newline
}

void dBlockList(BasicBlockList* list)
{
    printf("WorkList: ");
    while (list != nullptr)
    {
        printf(FMT_BB " ", list->block->bbNum);
        list = list->next;
    }
    printf("\n");
}

// Global variables available in debug mode.  That are set by debug APIs for finding
// Trees, Stmts, and/or Blocks using id or bbNum.
// That can be used in watch window or as a way to get address of fields for data break points.

GenTree*     dbTree;
GenTreeStmt* dbStmt;
BasicBlock*  dbTreeBlock;
BasicBlock*  dbBlock;

// Debug APIs for finding Trees, Stmts, and/or Blocks.
// As a side effect, they set the debug variables above.

GenTree* dFindTree(GenTree* tree, unsigned id)
{
    GenTree* child;

    if (tree == nullptr)
    {
        return nullptr;
    }

    if (tree->gtTreeID == id)
    {
        dbTree = tree;
        return tree;
    }

    unsigned childCount = tree->NumChildren();
    for (unsigned childIndex = 0; childIndex < childCount; childIndex++)
    {
        child = tree->GetChild(childIndex);
        child = dFindTree(child, id);
        if (child != nullptr)
        {
            return child;
        }
    }

    return nullptr;
}

GenTree* dFindTree(unsigned id)
{
    Compiler*   comp = JitTls::GetCompiler();
    BasicBlock* block;
    GenTree*    tree;

    dbTreeBlock = nullptr;
    dbTree      = nullptr;

    for (block = comp->fgFirstBB; block != nullptr; block = block->bbNext)
    {
        for (GenTreeStmt* stmt = block->firstStmt(); stmt; stmt = stmt->gtNextStmt)
        {
            tree = dFindTree(stmt, id);
            if (tree != nullptr)
            {
                dbTreeBlock = block;
                return tree;
            }
        }
    }

    return nullptr;
}

GenTreeStmt* dFindStmt(unsigned id)
{
    Compiler*   comp = JitTls::GetCompiler();
    BasicBlock* block;

    dbStmt = nullptr;

    unsigned stmtId = 0;
    for (block = comp->fgFirstBB; block != nullptr; block = block->bbNext)
    {
        for (GenTreeStmt* stmt = block->firstStmt(); stmt; stmt = stmt->gtNextStmt)
        {
            stmtId++;
            if (stmtId == id)
            {
                dbStmt = stmt;
                return stmt;
            }
        }
    }

    return nullptr;
}

BasicBlock* dFindBlock(unsigned bbNum)
{
    Compiler*   comp  = JitTls::GetCompiler();
    BasicBlock* block = nullptr;

    dbBlock = nullptr;
    for (block = comp->fgFirstBB; block != nullptr; block = block->bbNext)
    {
        if (block->bbNum == bbNum)
        {
            dbBlock = block;
            break;
        }
    }

    return block;
}

/*****************************************************************************
 *
 *  COMPlus_JitDumpIR support - dump out function in linear IR form
 */

void cFuncIR(Compiler* comp)
{
    BasicBlock* block;

    printf("Method %s::%s, hsh=0x%x\n", comp->info.compClassName, comp->info.compMethodName,
           comp->info.compMethodHash());

    printf("\n");

    for (block = comp->fgFirstBB; block != nullptr; block = block->bbNext)
    {
        cBlockIR(comp, block);
    }
}

/*****************************************************************************
 *
 *  COMPlus_JitDumpIR support - dump out the format specifiers from COMPlus_JitDumpIRFormat
 */

void dFormatIR()
{
    Compiler* comp = JitTls::GetCompiler();

    if (comp->dumpIRFormat != nullptr)
    {
        printf("COMPlus_JitDumpIRFormat=%ls", comp->dumpIRFormat);
    }
}

/*****************************************************************************
 *
 *  COMPlus_JitDumpIR support - dump out function in linear IR form
 */

void dFuncIR()
{
    cFuncIR(JitTls::GetCompiler());
}

/*****************************************************************************
 *
 *  COMPlus_JitDumpIR support - dump out loop in linear IR form
 */

void cLoopIR(Compiler* comp, Compiler::LoopDsc* loop)
{
    BasicBlock* blockHead   = loop->lpHead;
    BasicBlock* blockFirst  = loop->lpFirst;
    BasicBlock* blockTop    = loop->lpTop;
    BasicBlock* blockEntry  = loop->lpEntry;
    BasicBlock* blockBottom = loop->lpBottom;
    BasicBlock* blockExit   = loop->lpExit;
    BasicBlock* blockLast   = blockBottom->bbNext;
    BasicBlock* block;

    printf("LOOP\n");
    printf("\n");
    printf("HEAD   " FMT_BB "\n", blockHead->bbNum);
    printf("FIRST  " FMT_BB "\n", blockFirst->bbNum);
    printf("TOP    " FMT_BB "\n", blockTop->bbNum);
    printf("ENTRY  " FMT_BB "\n", blockEntry->bbNum);
    if (loop->lpExitCnt == 1)
    {
        printf("EXIT   " FMT_BB "\n", blockExit->bbNum);
    }
    else
    {
        printf("EXITS  %u", loop->lpExitCnt);
    }
    printf("BOTTOM " FMT_BB "\n", blockBottom->bbNum);
    printf("\n");

    cBlockIR(comp, blockHead);
    for (block = blockFirst; ((block != nullptr) && (block != blockLast)); block = block->bbNext)
    {
        cBlockIR(comp, block);
    }
}

/*****************************************************************************
 *
 *  COMPlus_JitDumpIR support - dump out loop in linear IR form
 */

void dLoopIR(Compiler::LoopDsc* loop)
{
    cLoopIR(JitTls::GetCompiler(), loop);
}

/*****************************************************************************
 *
 *  COMPlus_JitDumpIR support - dump out loop (given loop number) in linear IR form
 */

void dLoopNumIR(unsigned loopNum)
{
    Compiler* comp = JitTls::GetCompiler();

    if (loopNum >= comp->optLoopCount)
    {
        printf("loopNum %u out of range\n");
        return;
    }

    Compiler::LoopDsc* loop = &comp->optLoopTable[loopNum];
    cLoopIR(JitTls::GetCompiler(), loop);
}

/*****************************************************************************
 *
 *  COMPlus_JitDumpIR support - dump spaces to specified tab stop
 */

int dTabStopIR(int curr, int tabstop)
{
    int chars = 0;

    if (tabstop <= curr)
    {
        chars += printf(" ");
    }

    for (int i = curr; i < tabstop; i++)
    {
        chars += printf(" ");
    }

    return chars;
}

void cNodeIR(Compiler* comp, GenTree* tree);

/*****************************************************************************
 *
 *  COMPlus_JitDumpIR support - dump out block in linear IR form
 */

void cBlockIR(Compiler* comp, BasicBlock* block)
{
    bool noStmts = comp->dumpIRNoStmts;
    bool trees   = comp->dumpIRTrees;

    if (comp->dumpIRBlockHeaders)
    {
        block->dspBlockHeader(comp);
    }
    else
    {
        printf(FMT_BB ":\n", block->bbNum);
    }

    printf("\n");

    if (!block->IsLIR())
    {
        for (GenTreeStmt* stmt = block->firstStmt(); stmt; stmt = stmt->gtNextStmt)
        {
            // Print current stmt.

            if (trees)
            {
                cTree(comp, stmt);
                printf("\n");
                printf("=====================================================================\n");
            }

            if (comp->compRationalIRForm)
            {
                for (GenTree* tree = stmt->gtStmtList; tree != nullptr; tree = tree->gtNext)
                {
                    cNodeIR(comp, tree);
                }
            }
            else
            {
                cTreeIR(comp, stmt);
            }

            if (!noStmts && !trees)
            {
                printf("\n");
            }
        }
    }
    else
    {
        for (GenTree* node = block->bbTreeList; node != nullptr; node = node->gtNext)
        {
            cNodeIR(comp, node);
        }
    }

    int chars = 0;

    chars += dTabStopIR(chars, COLUMN_OPCODE);

    chars += printf("   ");
    switch (block->bbJumpKind)
    {
        case BBJ_EHFINALLYRET:
            chars += printf("BRANCH(EHFINALLYRET)");
            break;

        case BBJ_EHFILTERRET:
            chars += printf("BRANCH(EHFILTERRET)");
            break;

        case BBJ_EHCATCHRET:
            chars += printf("BRANCH(EHCATCHRETURN)");
            chars += dTabStopIR(chars, COLUMN_OPERANDS);
            chars += printf(" " FMT_BB, block->bbJumpDest->bbNum);
            break;

        case BBJ_THROW:
            chars += printf("BRANCH(THROW)");
            break;

        case BBJ_RETURN:
            chars += printf("BRANCH(RETURN)");
            break;

        case BBJ_NONE:
            // For fall-through blocks
            chars += printf("BRANCH(NONE)");
            break;

        case BBJ_ALWAYS:
            chars += printf("BRANCH(ALWAYS)");
            chars += dTabStopIR(chars, COLUMN_OPERANDS);
            chars += printf(" " FMT_BB, block->bbJumpDest->bbNum);
            if (block->bbFlags & BBF_KEEP_BBJ_ALWAYS)
            {
                chars += dTabStopIR(chars, COLUMN_KINDS);
                chars += printf("; [KEEP_BBJ_ALWAYS]");
            }
            break;

        case BBJ_LEAVE:
            chars += printf("BRANCH(LEAVE)");
            chars += dTabStopIR(chars, COLUMN_OPERANDS);
            chars += printf(" " FMT_BB, block->bbJumpDest->bbNum);
            break;

        case BBJ_CALLFINALLY:
            chars += printf("BRANCH(CALLFINALLY)");
            chars += dTabStopIR(chars, COLUMN_OPERANDS);
            chars += printf(" " FMT_BB, block->bbJumpDest->bbNum);
            break;

        case BBJ_COND:
            chars += printf("BRANCH(COND)");
            chars += dTabStopIR(chars, COLUMN_OPERANDS);
            chars += printf(" " FMT_BB, block->bbJumpDest->bbNum);
            break;

        case BBJ_SWITCH:
            chars += printf("BRANCH(SWITCH)");
            chars += dTabStopIR(chars, COLUMN_OPERANDS);

            unsigned jumpCnt;
            jumpCnt = block->bbJumpSwt->bbsCount;
            BasicBlock** jumpTab;
            jumpTab = block->bbJumpSwt->bbsDstTab;
            do
            {
                chars += printf("%c " FMT_BB, (jumpTab == block->bbJumpSwt->bbsDstTab) ? ' ' : ',', (*jumpTab)->bbNum);
            } while (++jumpTab, --jumpCnt);
            break;

        default:
            unreached();
            break;
    }

    printf("\n");
    if (block->bbNext != nullptr)
    {
        printf("\n");
    }
}

/*****************************************************************************
 *
 *  COMPlus_JitDumpIR support - dump out block in linear IR form
 */

void dBlockIR(BasicBlock* block)
{
    cBlockIR(JitTls::GetCompiler(), block);
}

/*****************************************************************************
 *
 *  COMPlus_JitDumpIR support - dump out tree node type for linear IR form
 */

int cTreeTypeIR(Compiler* comp, GenTree* tree)
{
    int chars = 0;

    var_types type = tree->TypeGet();

    const char* typeName = varTypeName(type);
    chars += printf(".%s", typeName);

    return chars;
}

/*****************************************************************************
 *
 *  COMPlus_JitDumpIR support - dump out tree node type for linear IR form
 */

int dTreeTypeIR(GenTree* tree)
{
    int chars = cTreeTypeIR(JitTls::GetCompiler(), tree);

    return chars;
}

/*****************************************************************************
 *
 *  COMPlus_JitDumpIR support - dump out tree node kind for linear IR form
 */

int cTreeKindsIR(Compiler* comp, GenTree* tree)
{
    int chars = 0;

    unsigned kind = tree->OperKind();

    chars += printf("kinds=");
    if (kind == GTK_SPECIAL)
    {
        chars += printf("[SPECIAL]");
    }
    if (kind & GTK_CONST)
    {
        chars += printf("[CONST]");
    }
    if (kind & GTK_LEAF)
    {
        chars += printf("[LEAF]");
    }
    if (kind & GTK_UNOP)
    {
        chars += printf("[UNOP]");
    }
    if (kind & GTK_BINOP)
    {
        chars += printf("[BINOP]");
    }
    if (kind & GTK_LOGOP)
    {
        chars += printf("[LOGOP]");
    }
    if (kind & GTK_COMMUTE)
    {
        chars += printf("[COMMUTE]");
    }
    if (kind & GTK_EXOP)
    {
        chars += printf("[EXOP]");
    }
    if (kind & GTK_LOCAL)
    {
        chars += printf("[LOCAL]");
    }
    if (kind & GTK_SMPOP)
    {
        chars += printf("[SMPOP]");
    }

    return chars;
}

/*****************************************************************************
 *
 *  COMPlus_JitDumpIR support - dump out tree node kind for linear IR form
 */

int dTreeKindsIR(GenTree* tree)
{
    int chars = cTreeKindsIR(JitTls::GetCompiler(), tree);

    return chars;
}

/*****************************************************************************
 *
 *  COMPlus_JitDumpIR support - dump out tree node flags for linear IR form
 */

int cTreeFlagsIR(Compiler* comp, GenTree* tree)
{
    int chars = 0;

    if (tree->gtFlags != 0)
    {
        chars += printf("flags=");

        // Node flags
        CLANG_FORMAT_COMMENT_ANCHOR;

#if defined(DEBUG)
        if (comp->dumpIRNodes)
        {
            if (tree->gtDebugFlags & GTF_DEBUG_NODE_LARGE)
            {
                chars += printf("[NODE_LARGE]");
            }
            if (tree->gtDebugFlags & GTF_DEBUG_NODE_SMALL)
            {
                chars += printf("[NODE_SMALL]");
            }
        }
        if (tree->gtDebugFlags & GTF_DEBUG_NODE_MORPHED)
        {
            chars += printf("[MORPHED]");
        }
#endif // defined(DEBUG)

        if (tree->gtFlags & GTF_COLON_COND)
        {
            chars += printf("[COLON_COND]");
        }

        // Operator flags

        genTreeOps op = tree->OperGet();
        switch (op)
        {

            case GT_LCL_VAR:
            case GT_LCL_VAR_ADDR:
            case GT_LCL_FLD:
            case GT_LCL_FLD_ADDR:
            case GT_STORE_LCL_FLD:
            case GT_STORE_LCL_VAR:
                if (tree->gtFlags & GTF_VAR_DEF)
                {
                    chars += printf("[VAR_DEF]");
                }
                if (tree->gtFlags & GTF_VAR_USEASG)
                {
                    chars += printf("[VAR_USEASG]");
                }
                if (tree->gtFlags & GTF_VAR_CAST)
                {
                    chars += printf("[VAR_CAST]");
                }
                if (tree->gtFlags & GTF_VAR_ITERATOR)
                {
                    chars += printf("[VAR_ITERATOR]");
                }
                if (tree->gtFlags & GTF_VAR_CLONED)
                {
                    chars += printf("[VAR_CLONED]");
                }
                if (tree->gtFlags & GTF_VAR_DEATH)
                {
                    chars += printf("[VAR_DEATH]");
                }
                if (tree->gtFlags & GTF_VAR_ARR_INDEX)
                {
                    chars += printf("[VAR_ARR_INDEX]");
                }
#if defined(DEBUG)
                if (tree->gtDebugFlags & GTF_DEBUG_VAR_CSE_REF)
                {
                    chars += printf("[VAR_CSE_REF]");
                }
#endif
                break;

            case GT_NOP:

                if (tree->gtFlags & GTF_NOP_DEATH)
                {
                    chars += printf("[NOP_DEATH]");
                }
                break;

            case GT_NO_OP:
                break;

            case GT_FIELD:
                if (tree->gtFlags & GTF_FLD_VOLATILE)
                {
                    chars += printf("[FLD_VOLATILE]");
                }
                break;

            case GT_INDEX:

                if (tree->gtFlags & GTF_INX_REFARR_LAYOUT)
                {
                    chars += printf("[INX_REFARR_LAYOUT]");
                }
                if (tree->gtFlags & GTF_INX_STRING_LAYOUT)
                {
                    chars += printf("[INX_STRING_LAYOUT]");
                }
                __fallthrough;
            case GT_INDEX_ADDR:
                if (tree->gtFlags & GTF_INX_RNGCHK)
                {
                    chars += printf("[INX_RNGCHK]");
                }
                break;

            case GT_IND:
            case GT_STOREIND:

                if (tree->gtFlags & GTF_IND_VOLATILE)
                {
                    chars += printf("[IND_VOLATILE]");
                }
                if (tree->gtFlags & GTF_IND_TGTANYWHERE)
                {
                    chars += printf("[IND_TGTANYWHERE]");
                }
                if (tree->gtFlags & GTF_IND_TGT_NOT_HEAP)
                {
                    chars += printf("[IND_TGT_NOT_HEAP]");
                }
                if (tree->gtFlags & GTF_IND_TLS_REF)
                {
                    chars += printf("[IND_TLS_REF]");
                }
                if (tree->gtFlags & GTF_IND_ASG_LHS)
                {
                    chars += printf("[IND_ASG_LHS]");
                }
                if (tree->gtFlags & GTF_IND_UNALIGNED)
                {
                    chars += printf("[IND_UNALIGNED]");
                }
                if (tree->gtFlags & GTF_IND_INVARIANT)
                {
                    chars += printf("[IND_INVARIANT]");
                }
                break;

            case GT_CLS_VAR:

                if (tree->gtFlags & GTF_CLS_VAR_ASG_LHS)
                {
                    chars += printf("[CLS_VAR_ASG_LHS]");
                }
                break;

            case GT_MUL:
#if !defined(_TARGET_64BIT_)
            case GT_MUL_LONG:
#endif

                if (tree->gtFlags & GTF_MUL_64RSLT)
                {
                    chars += printf("[64RSLT]");
                }
                if (tree->gtFlags & GTF_ADDRMODE_NO_CSE)
                {
                    chars += printf("[ADDRMODE_NO_CSE]");
                }
                break;

            case GT_ADD:

                if (tree->gtFlags & GTF_ADDRMODE_NO_CSE)
                {
                    chars += printf("[ADDRMODE_NO_CSE]");
                }
                break;

            case GT_LSH:

                if (tree->gtFlags & GTF_ADDRMODE_NO_CSE)
                {
                    chars += printf("[ADDRMODE_NO_CSE]");
                }
                break;

            case GT_MOD:
            case GT_UMOD:
                break;

            case GT_EQ:
            case GT_NE:
            case GT_LT:
            case GT_LE:
            case GT_GT:
            case GT_GE:

                if (tree->gtFlags & GTF_RELOP_NAN_UN)
                {
                    chars += printf("[RELOP_NAN_UN]");
                }
                if (tree->gtFlags & GTF_RELOP_JMP_USED)
                {
                    chars += printf("[RELOP_JMP_USED]");
                }
                if (tree->gtFlags & GTF_RELOP_QMARK)
                {
                    chars += printf("[RELOP_QMARK]");
                }
                break;

            case GT_QMARK:

                if (tree->gtFlags & GTF_QMARK_CAST_INSTOF)
                {
                    chars += printf("[QMARK_CAST_INSTOF]");
                }
                break;

            case GT_BOX:

                if (tree->gtFlags & GTF_BOX_VALUE)
                {
                    chars += printf("[BOX_VALUE]");
                }
                break;

            case GT_CNS_INT:

            {
                unsigned handleKind = (tree->gtFlags & GTF_ICON_HDL_MASK);

                switch (handleKind)
                {

                    case GTF_ICON_SCOPE_HDL:

                        chars += printf("[ICON_SCOPE_HDL]");
                        break;

                    case GTF_ICON_CLASS_HDL:

                        chars += printf("[ICON_CLASS_HDL]");
                        break;

                    case GTF_ICON_METHOD_HDL:

                        chars += printf("[ICON_METHOD_HDL]");
                        break;

                    case GTF_ICON_FIELD_HDL:

                        chars += printf("[ICON_FIELD_HDL]");
                        break;

                    case GTF_ICON_STATIC_HDL:

                        chars += printf("[ICON_STATIC_HDL]");
                        break;

                    case GTF_ICON_STR_HDL:

                        chars += printf("[ICON_STR_HDL]");
                        break;

                    case GTF_ICON_PSTR_HDL:

                        chars += printf("[ICON_PSTR_HDL]");
                        break;

                    case GTF_ICON_PTR_HDL:

                        chars += printf("[ICON_PTR_HDL]");
                        break;

                    case GTF_ICON_VARG_HDL:

                        chars += printf("[ICON_VARG_HDL]");
                        break;

                    case GTF_ICON_PINVKI_HDL:

                        chars += printf("[ICON_PINVKI_HDL]");
                        break;

                    case GTF_ICON_TOKEN_HDL:

                        chars += printf("[ICON_TOKEN_HDL]");
                        break;

                    case GTF_ICON_TLS_HDL:

                        chars += printf("[ICON_TLD_HDL]");
                        break;

                    case GTF_ICON_FTN_ADDR:

                        chars += printf("[ICON_FTN_ADDR]");
                        break;

                    case GTF_ICON_CIDMID_HDL:

                        chars += printf("[ICON_CIDMID_HDL]");
                        break;

                    case GTF_ICON_BBC_PTR:

                        chars += printf("[ICON_BBC_PTR]");
                        break;

                    case GTF_ICON_FIELD_OFF:

                        chars += printf("[ICON_FIELD_OFF]");
                        break;
                }
            }
            break;

            case GT_OBJ:
            case GT_STORE_OBJ:
                if (tree->AsObj()->HasGCPtr())
                {
                    chars += printf("[BLK_HASGCPTR]");
                }
                __fallthrough;

            case GT_BLK:
            case GT_DYN_BLK:
            case GT_STORE_BLK:
            case GT_STORE_DYN_BLK:

                if (tree->gtFlags & GTF_BLK_VOLATILE)
                {
                    chars += printf("[BLK_VOLATILE]");
                }
                if (tree->AsBlk()->IsUnaligned())
                {
                    chars += printf("[BLK_UNALIGNED]");
                }
                break;

            case GT_CALL:

                if (tree->gtFlags & GTF_CALL_UNMANAGED)
                {
                    chars += printf("[CALL_UNMANAGED]");
                }
                if (tree->gtFlags & GTF_CALL_INLINE_CANDIDATE)
                {
                    chars += printf("[CALL_INLINE_CANDIDATE]");
                }
                if (!tree->AsCall()->IsVirtual())
                {
                    chars += printf("[CALL_NONVIRT]");
                }
                if (tree->AsCall()->IsVirtualVtable())
                {
                    chars += printf("[CALL_VIRT_VTABLE]");
                }
                if (tree->AsCall()->IsVirtualStub())
                {
                    chars += printf("[CALL_VIRT_STUB]");
                }
                if (tree->gtFlags & GTF_CALL_NULLCHECK)
                {
                    chars += printf("[CALL_NULLCHECK]");
                }
                if (tree->gtFlags & GTF_CALL_POP_ARGS)
                {
                    chars += printf("[CALL_POP_ARGS]");
                }
                if (tree->gtFlags & GTF_CALL_HOISTABLE)
                {
                    chars += printf("[CALL_HOISTABLE]");
                }

                // More flags associated with calls.

                {
                    GenTreeCall* call = tree->AsCall();

                    if (call->gtCallMoreFlags & GTF_CALL_M_EXPLICIT_TAILCALL)
                    {
                        chars += printf("[CALL_M_EXPLICIT_TAILCALL]");
                    }
                    if (call->gtCallMoreFlags & GTF_CALL_M_TAILCALL)
                    {
                        chars += printf("[CALL_M_TAILCALL]");
                    }
                    if (call->gtCallMoreFlags & GTF_CALL_M_VARARGS)
                    {
                        chars += printf("[CALL_M_VARARGS]");
                    }
                    if (call->gtCallMoreFlags & GTF_CALL_M_RETBUFFARG)
                    {
                        chars += printf("[CALL_M_RETBUFFARG]");
                    }
                    if (call->gtCallMoreFlags & GTF_CALL_M_DELEGATE_INV)
                    {
                        chars += printf("[CALL_M_DELEGATE_INV]");
                    }
                    if (call->gtCallMoreFlags & GTF_CALL_M_NOGCCHECK)
                    {
                        chars += printf("[CALL_M_NOGCCHECK]");
                    }
                    if (call->gtCallMoreFlags & GTF_CALL_M_SPECIAL_INTRINSIC)
                    {
                        chars += printf("[CALL_M_SPECIAL_INTRINSIC]");
                    }

                    if (call->IsUnmanaged())
                    {
                        if (call->gtCallMoreFlags & GTF_CALL_M_UNMGD_THISCALL)
                        {
                            chars += printf("[CALL_M_UNMGD_THISCALL]");
                        }
                    }
                    else if (call->IsVirtualStub())
                    {
                        if (call->gtCallMoreFlags & GTF_CALL_M_VIRTSTUB_REL_INDIRECT)
                        {
                            chars += printf("[CALL_M_VIRTSTUB_REL_INDIRECT]");
                        }
                    }
                    else if (!call->IsVirtual())
                    {
                        if (call->gtCallMoreFlags & GTF_CALL_M_NONVIRT_SAME_THIS)
                        {
                            chars += printf("[CALL_M_NONVIRT_SAME_THIS]");
                        }
                    }

                    if (call->gtCallMoreFlags & GTF_CALL_M_FRAME_VAR_DEATH)
                    {
                        chars += printf("[CALL_M_FRAME_VAR_DEATH]");
                    }
                    if (call->gtCallMoreFlags & GTF_CALL_M_TAILCALL_VIA_HELPER)
                    {
                        chars += printf("[CALL_M_TAILCALL_VIA_HELPER]");
                    }
#if FEATURE_TAILCALL_OPT
                    if (call->gtCallMoreFlags & GTF_CALL_M_IMPLICIT_TAILCALL)
                    {
                        chars += printf("[CALL_M_IMPLICIT_TAILCALL]");
                    }
#endif
                    if (call->gtCallMoreFlags & GTF_CALL_M_PINVOKE)
                    {
                        chars += printf("[CALL_M_PINVOKE]");
                    }
                }
                break;

            case GT_STMT:

                if (tree->gtFlags & GTF_STMT_CMPADD)
                {
                    chars += printf("[STMT_CMPADD]");
                }
                if (tree->gtFlags & GTF_STMT_HAS_CSE)
                {
                    chars += printf("[STMT_HAS_CSE]");
                }
                break;

            default:

            {
                unsigned flags = (tree->gtFlags & (~(unsigned)(GTF_COMMON_MASK | GTF_OVERFLOW)));
                if (flags != 0)
                {
                    chars += printf("[%08X]", flags);
                }
            }
            break;
        }

        // Common flags.

        if (tree->gtFlags & GTF_ASG)
        {
            chars += printf("[ASG]");
        }
        if (tree->gtFlags & GTF_CALL)
        {
            chars += printf("[CALL]");
        }
        switch (op)
        {
            case GT_MUL:
            case GT_CAST:
            case GT_ADD:
            case GT_SUB:
                if (tree->gtFlags & GTF_OVERFLOW)
                {
                    chars += printf("[OVERFLOW]");
                }
                break;
            default:
                break;
        }
        if (tree->gtFlags & GTF_EXCEPT)
        {
            chars += printf("[EXCEPT]");
        }
        if (tree->gtFlags & GTF_GLOB_REF)
        {
            chars += printf("[GLOB_REF]");
        }
        if (tree->gtFlags & GTF_ORDER_SIDEEFF)
        {
            chars += printf("[ORDER_SIDEEFF]");
        }
        if (tree->gtFlags & GTF_REVERSE_OPS)
        {
            if (op != GT_LCL_VAR)
            {
                chars += printf("[REVERSE_OPS]");
            }
        }
        if (tree->gtFlags & GTF_SPILLED)
        {
            chars += printf("[SPILLED_OPER]");
        }
#if FEATURE_SET_FLAGS
        if (tree->gtFlags & GTF_SET_FLAGS)
        {
            if ((op != GT_IND) && (op != GT_STOREIND))
            {
                chars += printf("[ZSF_SET_FLAGS]");
            }
        }
#endif
        if (tree->gtFlags & GTF_IND_NONFAULTING)
        {
            if (tree->OperIsIndirOrArrLength())
            {
                chars += printf("[IND_NONFAULTING]");
            }
        }
        if (tree->gtFlags & GTF_MAKE_CSE)
        {
            chars += printf("[MAKE_CSE]");
        }
        if (tree->gtFlags & GTF_DONT_CSE)
        {
            chars += printf("[DONT_CSE]");
        }
        if (tree->gtFlags & GTF_BOOLEAN)
        {
            chars += printf("[BOOLEAN]");
        }
        if (tree->gtFlags & GTF_UNSIGNED)
        {
            chars += printf("[SMALL_UNSIGNED]");
        }
        if (tree->gtFlags & GTF_LATE_ARG)
        {
            chars += printf("[SMALL_LATE_ARG]");
        }
        if (tree->gtFlags & GTF_SPILL)
        {
            chars += printf("[SPILL]");
        }
        if (tree->gtFlags & GTF_REUSE_REG_VAL)
        {
            if (op == GT_CNS_INT)
            {
                chars += printf("[REUSE_REG_VAL]");
            }
        }
    }

    return chars;
}

/*****************************************************************************
 *
 *  COMPlus_JitDumpIR support - dump out tree node flags for linear IR form
 */

int dTreeFlagsIR(GenTree* tree)
{
    int chars = cTreeFlagsIR(JitTls::GetCompiler(), tree);

    return chars;
}

/*****************************************************************************
 *
 *  COMPlus_JitDumpIR support - dump out SSA number on tree node for linear IR form
 */

int cSsaNumIR(Compiler* comp, GenTree* tree)
{
    int chars = 0;

    if (tree->gtLclVarCommon.HasSsaName())
    {
        if (tree->gtFlags & GTF_VAR_USEASG)
        {
            assert(tree->gtFlags & GTF_VAR_DEF);
            chars += printf("<u:%d><d:%d>", tree->gtLclVarCommon.gtSsaNum, comp->GetSsaNumForLocalVarDef(tree));
        }
        else
        {
            chars += printf("<%s:%d>", (tree->gtFlags & GTF_VAR_DEF) ? "d" : "u", tree->gtLclVarCommon.gtSsaNum);
        }
    }

    return chars;
}

/*****************************************************************************
 *
 *  COMPlus_JitDumpIR support - dump out SSA number on tree node for linear IR form
 */

int dSsaNumIR(GenTree* tree)
{
    int chars = cSsaNumIR(JitTls::GetCompiler(), tree);

    return chars;
}

/*****************************************************************************
 *
 *  COMPlus_JitDumpIR support - dump out Value Number on tree node for linear IR form
 */

int cValNumIR(Compiler* comp, GenTree* tree)
{
    int chars = 0;

    if (tree->gtVNPair.GetLiberal() != ValueNumStore::NoVN)
    {
        assert(tree->gtVNPair.GetConservative() != ValueNumStore::NoVN);
        ValueNumPair vnp = tree->gtVNPair;
        ValueNum     vn;
        if (vnp.BothEqual())
        {
            chars += printf("<v:");
            vn = vnp.GetLiberal();
            chars += printf(FMT_VN, vn);
            if (ValueNumStore::isReservedVN(vn))
            {
                chars += printf("R");
            }
            chars += printf(">");
        }
        else
        {
            vn = vnp.GetLiberal();
            chars += printf("<v:");
            chars += printf(FMT_VN, vn);
            if (ValueNumStore::isReservedVN(vn))
            {
                chars += printf("R");
            }
            chars += printf(",");
            vn = vnp.GetConservative();
            chars += printf(FMT_VN, vn);
            if (ValueNumStore::isReservedVN(vn))
            {
                chars += printf("R");
            }
            chars += printf(">");
        }
    }

    return chars;
}

/*****************************************************************************
 *
 *  COMPlus_JitDumpIR support - dump out Value Number on tree node for linear IR form
 */

int dValNumIR(GenTree* tree)
{
    int chars = cValNumIR(JitTls::GetCompiler(), tree);

    return chars;
}

/*****************************************************************************
 *
 *  COMPlus_JitDumpIR support - dump out tree leaf node for linear IR form
 */

int cLeafIR(Compiler* comp, GenTree* tree)
{
    int         chars  = 0;
    genTreeOps  op     = tree->OperGet();
    const char* ilKind = nullptr;
    const char* ilName = nullptr;
    unsigned    ilNum  = 0;
    unsigned    lclNum = 0;
    bool        hasSsa = false;

    switch (op)
    {

        case GT_PHI_ARG:
        case GT_LCL_VAR:
        case GT_LCL_VAR_ADDR:
        case GT_STORE_LCL_VAR:
            lclNum = tree->gtLclVarCommon.gtLclNum;
            comp->gtGetLclVarNameInfo(lclNum, &ilKind, &ilName, &ilNum);
            if (ilName != nullptr)
            {
                chars += printf("%s", ilName);
            }
            else
            {
                LclVarDsc* varDsc = comp->lvaTable + lclNum;
                chars += printf("%s%d", ilKind, ilNum);
                if (comp->dumpIRLocals)
                {
                    chars += printf("(V%02u", lclNum);
                    if (varDsc->lvTracked)
                    {
                        chars += printf(":T%02u", varDsc->lvVarIndex);
                    }
                    if (comp->dumpIRRegs)
                    {
                        if (varDsc->lvRegister)
                        {
                            chars += printf(":%s", getRegName(varDsc->lvRegNum));
                        }
                        else
                        {
                            switch (tree->GetRegTag())
                            {
                                case GenTree::GT_REGTAG_REG:
                                    chars += printf(":%s", comp->compRegVarName(tree->gtRegNum));
                                    break;
                                default:
                                    break;
                            }
                        }
                    }
                    chars += printf(")");
                }
                else if (comp->dumpIRRegs)
                {
                    if (varDsc->lvRegister)
                    {
                        chars += printf("(%s)", getRegName(varDsc->lvRegNum));
                    }
                    else
                    {
                        switch (tree->GetRegTag())
                        {
                            case GenTree::GT_REGTAG_REG:
                                chars += printf("(%s)", comp->compRegVarName(tree->gtRegNum));
                                break;
                            default:
                                break;
                        }
                    }
                }
            }

            hasSsa = true;
            break;

        case GT_LCL_FLD:
        case GT_LCL_FLD_ADDR:
        case GT_STORE_LCL_FLD:

            lclNum = tree->gtLclVarCommon.gtLclNum;
            comp->gtGetLclVarNameInfo(lclNum, &ilKind, &ilName, &ilNum);
            if (ilName != nullptr)
            {
                chars += printf("%s+%u", ilName, tree->gtLclFld.gtLclOffs);
            }
            else
            {
                chars += printf("%s%d+%u", ilKind, ilNum, tree->gtLclFld.gtLclOffs);
                LclVarDsc* varDsc = comp->lvaTable + lclNum;
                if (comp->dumpIRLocals)
                {
                    chars += printf("(V%02u", lclNum);
                    if (varDsc->lvTracked)
                    {
                        chars += printf(":T%02u", varDsc->lvVarIndex);
                    }
                    if (comp->dumpIRRegs)
                    {
                        if (varDsc->lvRegister)
                        {
                            chars += printf(":%s", getRegName(varDsc->lvRegNum));
                        }
                        else
                        {
                            switch (tree->GetRegTag())
                            {
                                case GenTree::GT_REGTAG_REG:
                                    chars += printf(":%s", comp->compRegVarName(tree->gtRegNum));
                                    break;
                                default:
                                    break;
                            }
                        }
                    }
                    chars += printf(")");
                }
                else if (comp->dumpIRRegs)
                {
                    if (varDsc->lvRegister)
                    {
                        chars += printf("(%s)", getRegName(varDsc->lvRegNum));
                    }
                    else
                    {
                        switch (tree->GetRegTag())
                        {
                            case GenTree::GT_REGTAG_REG:
                                chars += printf("(%s)", comp->compRegVarName(tree->gtRegNum));
                                break;
                            default:
                                break;
                        }
                    }
                }
            }

            // TODO: We probably want to expand field sequence.
            // gtDispFieldSeq(tree->gtLclFld.gtFieldSeq);

            hasSsa = true;
            break;

        case GT_CNS_INT:

            if (tree->IsIconHandle())
            {
#if 0
            // TODO: Commented out because sometimes the CLR throws
            // and exception when asking the names of some handles.
            // Need to investigate.

            const char* className;
            const char* fieldName;
            const char* methodName;
            const wchar_t* str;

            switch (tree->GetIconHandleFlag())
            {

            case GTF_ICON_SCOPE_HDL:

                chars += printf("SCOPE(?)");
                break;

            case GTF_ICON_CLASS_HDL:

                className = comp->eeGetClassName((CORINFO_CLASS_HANDLE)tree->gtIntCon.gtIconVal);
                chars += printf("CLASS(%s)", className);
                break;

            case GTF_ICON_METHOD_HDL:

                methodName = comp->eeGetMethodName((CORINFO_METHOD_HANDLE)tree->gtIntCon.gtIconVal,
                    &className);
                chars += printf("METHOD(%s.%s)", className, methodName);
                break;

            case GTF_ICON_FIELD_HDL:

                fieldName = comp->eeGetFieldName((CORINFO_FIELD_HANDLE)tree->gtIntCon.gtIconVal,
                    &className);
                chars += printf("FIELD(%s.%s) ", className, fieldName);
                break;

            case GTF_ICON_STATIC_HDL:

                fieldName = comp->eeGetFieldName((CORINFO_FIELD_HANDLE)tree->gtIntCon.gtIconVal,
                    &className);
                chars += printf("STATIC_FIELD(%s.%s)", className, fieldName);
                break;

            case GTF_ICON_STR_HDL:

                str = comp->eeGetCPString(tree->gtIntCon.gtIconVal);
                chars += printf("\"%S\"", str);
                break;

            case GTF_ICON_PSTR_HDL:

                chars += printf("PSTR(?)");
                break;

            case GTF_ICON_PTR_HDL:

                chars += printf("PTR(?)");
                break;

            case GTF_ICON_VARG_HDL:

                chars += printf("VARARG(?)");
                break;

            case GTF_ICON_PINVKI_HDL:

                chars += printf("PINVOKE(?)");
                break;

            case GTF_ICON_TOKEN_HDL:

                chars += printf("TOKEN(%08X)", tree->gtIntCon.gtIconVal);
                break;

            case GTF_ICON_TLS_HDL:

                chars += printf("TLS(?)");
                break;

            case GTF_ICON_FTN_ADDR:

                chars += printf("FTN(?)");
                break;

            case GTF_ICON_CIDMID_HDL:

                chars += printf("CIDMID(?)");
                break;

            case GTF_ICON_BBC_PTR:

                chars += printf("BBC(?)");
                break;

            default:

                chars += printf("HANDLE(?)");
                break;
            }
#else
#ifdef _TARGET_64BIT_
                if ((tree->gtIntCon.gtIconVal & 0xFFFFFFFF00000000LL) != 0)
                {
                    chars += printf("HANDLE(0x%llx)", dspPtr(tree->gtIntCon.gtIconVal));
                }
                else
#endif
                {
                    chars += printf("HANDLE(0x%0x)", dspPtr(tree->gtIntCon.gtIconVal));
                }
#endif
            }
            else
            {
                if (tree->TypeGet() == TYP_REF)
                {
                    assert(tree->gtIntCon.gtIconVal == 0);
                    chars += printf("null");
                }
#ifdef _TARGET_64BIT_
                else if ((tree->gtIntCon.gtIconVal & 0xFFFFFFFF00000000LL) != 0)
                {
                    chars += printf("0x%llx", tree->gtIntCon.gtIconVal);
                }
                else
#endif
                {
                    chars += printf("%ld(0x%x)", tree->gtIntCon.gtIconVal, tree->gtIntCon.gtIconVal);
                }
            }
            break;

        case GT_CNS_LNG:

            chars += printf("CONST(LONG)");
            break;

        case GT_CNS_DBL:

            chars += printf("CONST(DOUBLE)");
            break;

        case GT_CNS_STR:

            chars += printf("CONST(STR)");
            break;

        case GT_JMP:

        {
            const char* methodName;
            const char* className;

            methodName = comp->eeGetMethodName((CORINFO_METHOD_HANDLE)tree->gtVal.gtVal1, &className);
            chars += printf(" %s.%s", className, methodName);
        }
        break;

        case GT_NO_OP:
        case GT_START_NONGC:
        case GT_START_PREEMPTGC:
        case GT_PROF_HOOK:
        case GT_CATCH_ARG:
        case GT_MEMORYBARRIER:
        case GT_ARGPLACE:
        case GT_PINVOKE_PROLOG:
        case GT_JMPTABLE:
            // Do nothing.
            break;

        case GT_RET_EXPR:

            chars += printf("t%d", tree->gtRetExpr.gtInlineCandidate->gtTreeID);
            break;

        case GT_PHYSREG:

            chars += printf("%s", getRegName(tree->gtPhysReg.gtSrcReg, varTypeIsFloating(tree)));
            break;

        case GT_LABEL:
            break;

        case GT_IL_OFFSET:

            if (tree->gtStmt.gtStmtILoffsx == BAD_IL_OFFSET)
            {
                chars += printf("?");
            }
            else
            {
                chars += printf("0x%x", jitGetILoffs(tree->gtStmt.gtStmtILoffsx));
            }
            break;

        case GT_CLS_VAR:
        case GT_CLS_VAR_ADDR:
        default:

            if (tree->OperIsLeaf())
            {
                chars += printf("<leaf nyi: %s>", tree->OpName(tree->OperGet()));
            }

            chars += printf("t%d", tree->gtTreeID);
            break;
    }

    if (comp->dumpIRTypes)
    {
        chars += cTreeTypeIR(comp, tree);
    }
    if (comp->dumpIRValnums)
    {
        chars += cValNumIR(comp, tree);
    }
    if (hasSsa && comp->dumpIRSsa)
    {
        chars += cSsaNumIR(comp, tree);
    }

    return chars;
}

/*****************************************************************************
 *
 *  COMPlus_JitDumpIR support - dump out tree leaf node for linear IR form
 */

int dLeafIR(GenTree* tree)
{
    int chars = cLeafIR(JitTls::GetCompiler(), tree);

    return chars;
}

/*****************************************************************************
 *
 *  COMPlus_JitDumpIR support - dump out tree indir node for linear IR form
 */

int cIndirIR(Compiler* comp, GenTree* tree)
{
    assert(tree->gtOper == GT_IND);

    int      chars = 0;
    GenTree* child;

    chars += printf("[");
    child = tree->GetChild(0);
    chars += cLeafIR(comp, child);
    chars += printf("]");

    return chars;
}

/*****************************************************************************
 *
 *  COMPlus_JitDumpIR support - dump out tree indir node for linear IR form
 */

int dIndirIR(GenTree* tree)
{
    int chars = cIndirIR(JitTls::GetCompiler(), tree);

    return chars;
}

/*****************************************************************************
 *
 *  COMPlus_JitDumpIR support - dump out tree operand node for linear IR form
 */

int cOperandIR(Compiler* comp, GenTree* operand)
{
    int chars = 0;

    if (operand == nullptr)
    {
        chars += printf("t?");
        return chars;
    }

    bool dumpTypes    = comp->dumpIRTypes;
    bool dumpValnums  = comp->dumpIRValnums;
    bool foldIndirs   = comp->dumpIRDataflow;
    bool foldLeafs    = comp->dumpIRNoLeafs;
    bool foldCommas   = comp->dumpIRDataflow;
    bool dumpDataflow = comp->dumpIRDataflow;
    bool foldLists    = comp->dumpIRNoLists;
    bool dumpRegs     = comp->dumpIRRegs;

    genTreeOps op = operand->OperGet();

    if (foldLeafs && operand->OperIsLeaf())
    {
        if ((op == GT_ARGPLACE) && foldLists)
        {
            return chars;
        }
        chars += cLeafIR(comp, operand);
    }
    else if (dumpDataflow && (operand->OperIs(GT_ASG) || (op == GT_STORE_LCL_VAR) || (op == GT_STORE_LCL_FLD)))
    {
        operand = operand->GetChild(0);
        chars += cOperandIR(comp, operand);
    }
    else if ((op == GT_INDEX) && foldIndirs)
    {
        chars += printf("[t%d]", operand->gtTreeID);
        if (dumpTypes)
        {
            chars += cTreeTypeIR(comp, operand);
        }
        if (dumpValnums)
        {
            chars += cValNumIR(comp, operand);
        }
    }
    else if ((op == GT_IND) && foldIndirs)
    {
        chars += cIndirIR(comp, operand);
        if (dumpTypes)
        {
            chars += cTreeTypeIR(comp, operand);
        }
        if (dumpValnums)
        {
            chars += cValNumIR(comp, operand);
        }
    }
    else if ((op == GT_COMMA) && foldCommas)
    {
        operand = operand->GetChild(1);
        chars += cOperandIR(comp, operand);
    }
    else if ((op == GT_LIST) && foldLists)
    {
        GenTree* list       = operand;
        unsigned childCount = list->NumChildren();

        operand          = list->GetChild(0);
        int operandChars = cOperandIR(comp, operand);
        chars += operandChars;
        if (childCount > 1)
        {
            if (operandChars > 0)
            {
                chars += printf(", ");
            }
            operand = list->GetChild(1);
            if (operand->gtOper == GT_LIST)
            {
                chars += cListIR(comp, operand);
            }
            else
            {
                chars += cOperandIR(comp, operand);
            }
        }
    }
    else
    {
        chars += printf("t%d", operand->gtTreeID);
        if (dumpRegs)
        {
            regNumber regNum = operand->GetReg();
            if (regNum != REG_NA)
            {
                chars += printf("(%s)", getRegName(regNum));
            }
        }
        if (dumpTypes)
        {
            chars += cTreeTypeIR(comp, operand);
        }
        if (dumpValnums)
        {
            chars += cValNumIR(comp, operand);
        }
    }

    return chars;
}

/*****************************************************************************
 *
 *  COMPlus_JitDumpIR support - dump out tree operand node for linear IR form
 */

int dOperandIR(GenTree* operand)
{
    int chars = cOperandIR(JitTls::GetCompiler(), operand);

    return chars;
}

/*****************************************************************************
 *
 *  COMPlus_JitDumpIR support - dump out tree list of nodes for linear IR form
 */

int cListIR(Compiler* comp, GenTree* list)
{
    int chars = 0;
    int operandChars;

    assert(list->gtOper == GT_LIST);

    GenTree* child;
    unsigned childCount;

    childCount = list->NumChildren();
    assert(childCount == 1 || childCount == 2);

    operandChars = 0;
    for (unsigned childIndex = 0; childIndex < childCount; childIndex++)
    {
        if ((childIndex > 0) && (operandChars > 0))
        {
            chars += printf(", ");
        }

        child        = list->GetChild(childIndex);
        operandChars = cOperandIR(comp, child);
        chars += operandChars;
    }

    return chars;
}

/*****************************************************************************
 *
 *  COMPlus_JitDumpIR support - dump out tree list of nodes for linear IR form
 */

int dListIR(GenTree* list)
{
    int chars = cListIR(JitTls::GetCompiler(), list);

    return chars;
}

/*****************************************************************************
 *
 *  COMPlus_JitDumpIR support - dump out tree dependencies based on comma nodes for linear IR form
 */

int cDependsIR(Compiler* comp, GenTree* comma, bool* first)
{
    int chars = 0;

    assert(comma->gtOper == GT_COMMA);

    GenTree* child;

    child = comma->GetChild(0);
    if (child->gtOper == GT_COMMA)
    {
        chars += cDependsIR(comp, child, first);
    }
    else
    {
        if (!(*first))
        {
            chars += printf(", ");
        }
        chars += printf("t%d", child->gtTreeID);
        *first = false;
    }

    child = comma->GetChild(1);
    if (child->gtOper == GT_COMMA)
    {
        chars += cDependsIR(comp, child, first);
    }

    return chars;
}

/*****************************************************************************
 *
 *  COMPlus_JitDumpIR support - dump out tree dependencies based on comma nodes for linear IR form
 */

int dDependsIR(GenTree* comma)
{
    int  chars = 0;
    bool first = TRUE;

    chars = cDependsIR(JitTls::GetCompiler(), comma, &first);

    return chars;
}

/*****************************************************************************
 *
 *  COMPlus_JitDumpIR support - dump out tree node in linear IR form
 */

void cNodeIR(Compiler* comp, GenTree* tree)
{
    bool       foldLeafs    = comp->dumpIRNoLeafs;
    bool       foldIndirs   = comp->dumpIRDataflow;
    bool       foldLists    = comp->dumpIRNoLists;
    bool       dataflowView = comp->dumpIRDataflow;
    bool       dumpTypes    = comp->dumpIRTypes;
    bool       dumpValnums  = comp->dumpIRValnums;
    bool       noStmts      = comp->dumpIRNoStmts;
    genTreeOps op           = tree->OperGet();
    unsigned   childCount   = tree->NumChildren();
    GenTree*   child;

    // What are we skipping?

    if (tree->OperIsLeaf())
    {
        if (foldLeafs)
        {
            return;
        }
    }
    else if (op == GT_IND)
    {
        if (foldIndirs)
        {
            return;
        }
    }
    else if (op == GT_LIST)
    {
        if (foldLists)
        {
            return;
        }
    }
    else if (op == GT_STMT)
    {
        if (noStmts)
        {
            if (dataflowView)
            {
                child = tree->GetChild(0);
                if (child->gtOper != GT_COMMA)
                {
                    return;
                }
            }
            else
            {
                return;
            }
        }
    }
    else if (op == GT_COMMA)
    {
        if (dataflowView)
        {
            return;
        }
    }

    bool nodeIsValue = tree->IsValue();

    // Dump tree id or dataflow destination.

    int chars = 0;

    // if (comp->compRationalIRForm)
    // {
    //   chars += printf("R");
    // }

    chars += printf("    ");
    if (dataflowView && tree->OperIs(GT_ASG))
    {
        child = tree->GetChild(0);
        chars += cOperandIR(comp, child);
    }
    else if (dataflowView && ((op == GT_STORE_LCL_VAR) || (op == GT_STORE_LCL_FLD)))
    {
        chars += cLeafIR(comp, tree);
    }
    else if (dataflowView && (op == GT_STOREIND))
    {
        child = tree->GetChild(0);
        chars += printf("[");
        chars += cOperandIR(comp, child);
        chars += printf("]");
        if (dumpTypes)
        {
            chars += cTreeTypeIR(comp, tree);
        }
        if (dumpValnums)
        {
            chars += cValNumIR(comp, tree);
        }
    }
    else if (nodeIsValue)
    {
        chars += printf("t%d", tree->gtTreeID);
        if (comp->dumpIRRegs)
        {
            regNumber regNum = tree->GetReg();
            if (regNum != REG_NA)
            {
                chars += printf("(%s)", getRegName(regNum));
            }
        }
        if (dumpTypes)
        {
            chars += cTreeTypeIR(comp, tree);
        }
        if (dumpValnums)
        {
            chars += cValNumIR(comp, tree);
        }
    }

    // Dump opcode and tree ID if need in dataflow view.

    chars += dTabStopIR(chars, COLUMN_OPCODE);
    const char* opName = tree->OpName(op);
    chars += printf(" %c %s", nodeIsValue ? '=' : ' ', opName);

    if (dataflowView)
    {
        if (tree->OperIs(GT_ASG) || (op == GT_STORE_LCL_VAR) || (op == GT_STORE_LCL_FLD) || (op == GT_STOREIND))
        {
            chars += printf("(t%d)", tree->gtTreeID);
        }
    }

    // Dump modifiers for opcodes to help with readability

    if (op == GT_CALL)
    {
        GenTreeCall* call = tree->AsCall();

        if (call->gtCallType == CT_USER_FUNC)
        {
            if (call->IsVirtualStub())
            {
                chars += printf(":VS");
            }
            else if (call->IsVirtualVtable())
            {
                chars += printf(":VT");
            }
            else if (call->IsVirtual())
            {
                chars += printf(":V");
            }
        }
        else if (call->gtCallType == CT_HELPER)
        {
            chars += printf(":H");
        }
        else if (call->gtCallType == CT_INDIRECT)
        {
            chars += printf(":I");
        }
        else if (call->IsUnmanaged())
        {
            chars += printf(":U");
        }
        else
        {
            if (call->IsVirtualStub())
            {
                chars += printf(":XVS");
            }
            else if (call->IsVirtualVtable())
            {
                chars += printf(":XVT");
            }
            else
            {
                chars += printf(":?");
            }
        }

        if (call->IsUnmanaged())
        {
            if (call->gtCallMoreFlags & GTF_CALL_M_UNMGD_THISCALL)
            {
                chars += printf(":T");
            }
        }

        if (tree->gtFlags & GTF_CALL_NULLCHECK)
        {
            chars += printf(":N");
        }
    }
    else if (op == GT_INTRINSIC)
    {
        CorInfoIntrinsics intrin = tree->gtIntrinsic.gtIntrinsicId;

        chars += printf(":");
        switch (intrin)
        {
            case CORINFO_INTRINSIC_Sin:
                chars += printf("Sin");
                break;
            case CORINFO_INTRINSIC_Cos:
                chars += printf("Cos");
                break;
            case CORINFO_INTRINSIC_Cbrt:
                chars += printf("Cbrt");
                break;
            case CORINFO_INTRINSIC_Sqrt:
                chars += printf("Sqrt");
                break;
            case CORINFO_INTRINSIC_Cosh:
                chars += printf("Cosh");
                break;
            case CORINFO_INTRINSIC_Sinh:
                chars += printf("Sinh");
                break;
            case CORINFO_INTRINSIC_Tan:
                chars += printf("Tan");
                break;
            case CORINFO_INTRINSIC_Tanh:
                chars += printf("Tanh");
                break;
            case CORINFO_INTRINSIC_Asin:
                chars += printf("Asin");
                break;
            case CORINFO_INTRINSIC_Asinh:
                chars += printf("Asinh");
                break;
            case CORINFO_INTRINSIC_Acos:
                chars += printf("Acos");
                break;
            case CORINFO_INTRINSIC_Acosh:
                chars += printf("Acosh");
                break;
            case CORINFO_INTRINSIC_Atan:
                chars += printf("Atan");
                break;
            case CORINFO_INTRINSIC_Atan2:
                chars += printf("Atan2");
                break;
            case CORINFO_INTRINSIC_Atanh:
                chars += printf("Atanh");
                break;
            case CORINFO_INTRINSIC_Log10:
                chars += printf("Log10");
                break;
            case CORINFO_INTRINSIC_Pow:
                chars += printf("Pow");
                break;
            case CORINFO_INTRINSIC_Exp:
                chars += printf("Exp");
                break;
            case CORINFO_INTRINSIC_Ceiling:
                chars += printf("Ceiling");
                break;
            case CORINFO_INTRINSIC_Floor:
                chars += printf("Floor");
                break;
            default:
                chars += printf("unknown(%d)", intrin);
                break;
        }
    }

    // Dump operands.

    chars += dTabStopIR(chars, COLUMN_OPERANDS);

    // Dump operator specific fields as operands

    switch (op)
    {
        default:
            break;
        case GT_FIELD:

        {
            const char* className = nullptr;
            const char* fieldName = comp->eeGetFieldName(tree->gtField.gtFldHnd, &className);

            chars += printf(" %s.%s", className, fieldName);
        }
        break;

        case GT_CALL:

            if (tree->gtCall.gtCallType != CT_INDIRECT)
            {
                const char* methodName;
                const char* className;

                methodName = comp->eeGetMethodName(tree->gtCall.gtCallMethHnd, &className);

                chars += printf(" %s.%s", className, methodName);
            }
            break;

        case GT_STORE_LCL_VAR:
        case GT_STORE_LCL_FLD:

            if (!dataflowView)
            {
                chars += printf(" ");
                chars += cLeafIR(comp, tree);
            }
            break;

        case GT_LEA:

            GenTreeAddrMode* lea    = tree->AsAddrMode();
            GenTree*         base   = lea->Base();
            GenTree*         index  = lea->Index();
            unsigned         scale  = lea->gtScale;
            int              offset = lea->Offset();

            chars += printf(" [");
            if (base != nullptr)
            {
                chars += cOperandIR(comp, base);
            }
            if (index != nullptr)
            {
                if (base != nullptr)
                {
                    chars += printf("+");
                }
                chars += cOperandIR(comp, index);
                if (scale > 1)
                {
                    chars += printf("*%u", scale);
                }
            }
            if ((offset != 0) || ((base == nullptr) && (index == nullptr)))
            {
                if ((base != nullptr) || (index != nullptr))
                {
                    chars += printf("+");
                }
                chars += printf("%d", offset);
            }
            chars += printf("]");
            break;
    }

    // Dump operands.

    if (tree->OperIsLeaf())
    {
        chars += printf(" ");
        chars += cLeafIR(comp, tree);
    }
    else if (op == GT_LEA)
    {
        // Already dumped it above.
    }
    else if (op == GT_PHI)
    {
        if (tree->gtOp.gtOp1 != nullptr)
        {
            bool first = true;
            for (GenTreeArgList* args = tree->gtOp.gtOp1->AsArgList(); args != nullptr; args = args->Rest())
            {
                child = args->Current();
                if (!first)
                {
                    chars += printf(",");
                }
                first = false;
                chars += printf(" ");
                chars += cOperandIR(comp, child);
            }
        }
    }
    else
    {
        bool hasComma     = false;
        bool first        = true;
        int  operandChars = 0;
        for (unsigned childIndex = 0; childIndex < childCount; childIndex++)
        {
            child = tree->GetChild(childIndex);
            if (child == nullptr)
            {
                continue;
            }

            if (child->gtOper == GT_COMMA)
            {
                hasComma = true;
            }

            if (dataflowView && (childIndex == 0))
            {
                if ((op == GT_ASG) || (op == GT_STOREIND))
                {
                    continue;
                }
            }

            if (!first)
            {
                chars += printf(",");
            }

            bool isList = (child->gtOper == GT_LIST);
            if (!isList || !foldLists)
            {
                if (foldLeafs && (child->gtOper == GT_ARGPLACE))
                {
                    continue;
                }
                chars += printf(" ");
                operandChars = cOperandIR(comp, child);
                chars += operandChars;
                if (operandChars > 0)
                {
                    first = false;
                }
            }
            else
            {
                assert(isList);
                chars += printf(" ");
                operandChars = cOperandIR(comp, child);
                chars += operandChars;
                if (operandChars > 0)
                {
                    first = false;
                }
            }
        }

        if (dataflowView && hasComma)
        {
            chars += printf(", DEPS(");
            first = true;
            for (unsigned childIndex = 0; childIndex < childCount; childIndex++)
            {
                child = tree->GetChild(childIndex);
                if (child->gtOper == GT_COMMA)
                {
                    chars += cDependsIR(comp, child, &first);
                }
            }
            chars += printf(")");
        }
    }

    // Dump kinds, flags, costs

    if (comp->dumpIRKinds || comp->dumpIRFlags || comp->dumpIRCosts)
    {
        chars += dTabStopIR(chars, COLUMN_KINDS);
        chars += printf(";");
        if (comp->dumpIRKinds)
        {
            chars += printf(" ");
            chars += cTreeKindsIR(comp, tree);
        }
        if (comp->dumpIRFlags && (tree->gtFlags != 0))
        {
            if (comp->dumpIRKinds)
            {
                chars += dTabStopIR(chars, COLUMN_FLAGS);
            }
            else
            {
                chars += printf(" ");
            }
            chars += cTreeFlagsIR(comp, tree);
        }
        if (comp->dumpIRCosts && (tree->gtCostsInitialized))
        {
            chars += printf(" CostEx=%d, CostSz=%d", tree->GetCostEx(), tree->GetCostSz());
        }
    }

    printf("\n");
}

/*****************************************************************************
 *
 *  COMPlus_JitDumpIR support - dump out tree in linear IR form
 */

void cTreeIR(Compiler* comp, GenTree* tree)
{
    genTreeOps op         = tree->OperGet();
    unsigned   childCount = tree->NumChildren();
    GenTree*   child;

    // Recurse and dump trees that this node depends on.

    if (tree->OperIsLeaf())
    {
    }
    else if (tree->OperIsBinary() && tree->IsReverseOp())
    {
        child = tree->GetChild(1);
        cTreeIR(comp, child);
        child = tree->GetChild(0);
        cTreeIR(comp, child);
    }
    else if (op == GT_PHI)
    {
        // Don't recurse.
    }
    else
    {
        assert(!tree->IsReverseOp());
        for (unsigned childIndex = 0; childIndex < childCount; childIndex++)
        {
            child = tree->GetChild(childIndex);
            if (child != nullptr)
            {
                cTreeIR(comp, child);
            }
        }
    }

    cNodeIR(comp, tree);
}

/*****************************************************************************
 *
 *  COMPlus_JitDumpIR support - dump out tree in linear IR form
 */

void dTreeIR(GenTree* tree)
{
    cTreeIR(JitTls::GetCompiler(), tree);
}

#endif // DEBUG

#if VARSET_COUNTOPS
// static
BitSetSupport::BitSetOpCounter Compiler::m_varsetOpCounter("VarSetOpCounts.log");
#endif
#if ALLVARSET_COUNTOPS
// static
BitSetSupport::BitSetOpCounter Compiler::m_allvarsetOpCounter("AllVarSetOpCounts.log");
#endif

// static
HelperCallProperties Compiler::s_helperCallProperties;

/*****************************************************************************/
/*****************************************************************************/

//------------------------------------------------------------------------
// killGCRefs:
// Given some tree node return does it need all GC refs to be spilled from
// callee save registers.
//
// Arguments:
//    tree       - the tree for which we ask about gc refs.
//
// Return Value:
//    true       - tree kills GC refs on callee save registers
//    false      - tree doesn't affect GC refs on callee save registers
bool Compiler::killGCRefs(GenTree* tree)
{
    if (tree->IsCall())
    {
        GenTreeCall* call = tree->AsCall();
        if (call->IsUnmanaged())
        {
            return true;
        }

        if (call->gtCallMethHnd == eeFindHelper(CORINFO_HELP_JIT_PINVOKE_BEGIN))
        {
            assert(opts.ShouldUsePInvokeHelpers());
            return true;
        }
    }
    else if (tree->OperIs(GT_START_PREEMPTGC))
    {
        return true;
    }

    return false;
}
