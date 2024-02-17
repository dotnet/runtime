// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
#include "patchpointinfo.h"
#include "jitstd/algorithm.h"

extern ICorJitHost* g_jitHost;

unsigned Compiler::jitTotalMethodCompiled = 0;

#if defined(DEBUG)
LONG Compiler::jitNestingLevel = 0;
#endif // defined(DEBUG)

// static
bool                Compiler::s_pAltJitExcludeAssembliesListInitialized = false;
AssemblyNamesList2* Compiler::s_pAltJitExcludeAssembliesList            = nullptr;

#ifdef DEBUG
// static
bool                Compiler::s_pJitDisasmIncludeAssembliesListInitialized = false;
AssemblyNamesList2* Compiler::s_pJitDisasmIncludeAssembliesList            = nullptr;

// static
bool       Compiler::s_pJitFunctionFileInitialized = false;
MethodSet* Compiler::s_pJitMethodSet               = nullptr;
#endif // DEBUG

#ifdef CONFIGURABLE_ARM_ABI
// static
bool GlobalJitOptions::compFeatureHfa          = false;
LONG GlobalJitOptions::compUseSoftFPConfigured = 0;
#endif // CONFIGURABLE_ARM_ABI

/*****************************************************************************
 *
 *  Little helpers to grab the current cycle counter value; this is done
 *  differently based on target architecture, host toolchain, etc. The
 *  main thing is to keep the overhead absolutely minimal; in fact, on
 *  x86/x64 we use RDTSC even though it's not thread-safe; GetThreadCycles
 *  (which is monotonous) is just too expensive.
 */
#ifdef FEATURE_JIT_METHOD_PERF

#if defined(HOST_X86) || defined(HOST_AMD64)

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

#elif defined(HOST_ARM) || defined(HOST_ARM64)

// If this doesn't work please see ../gc/gc.cpp for additional ARM
// info (and possible solutions).
#define _our_GetThreadCycles(cp) GetThreadCycles(cp)

#else // not x86/x64 and not ARM

// Don't know what this target is, but let's give it a try; if
// someone really wants to make this work, please add the right
// code here.
#define _our_GetThreadCycles(cp) GetThreadCycles(cp)

#endif // which host OS

const BYTE genTypeSizes[] = {
#define DEF_TP(tn, nm, jitType, sz, sze, asze, st, al, regTyp, regFld, csr, ctr, tf) sz,
#include "typelist.h"
#undef DEF_TP
};

const BYTE genTypeAlignments[] = {
#define DEF_TP(tn, nm, jitType, sz, sze, asze, st, al, regTyp, regFld, csr, ctr, tf) al,
#include "typelist.h"
#undef DEF_TP
};

const BYTE genTypeStSzs[] = {
#define DEF_TP(tn, nm, jitType, sz, sze, asze, st, al, regTyp, regFld, csr, ctr, tf) st,
#include "typelist.h"
#undef DEF_TP
};

const BYTE genActualTypes[] = {
#define DEF_TP(tn, nm, jitType, sz, sze, asze, st, al, regTyp, regFld, csr, ctr, tf) jitType,
#include "typelist.h"
#undef DEF_TP
};

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
        vflogf(jitstdout(), fmt, args);
        va_end(args);
    }

    va_start(args, fmt);
    vlogf(level, fmt, args);
    va_end(args);
}

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

unsigned  computeReachabilitySetsIterationBuckets[] = {1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 0};
Histogram computeReachabilitySetsIterationTable(computeReachabilitySetsIterationBuckets);

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
unsigned totalLoopCount;          // counts the total number of natural loops
unsigned totalUnnatLoopCount;     // counts the total number of (not-necessarily natural) loops
unsigned totalUnnatLoopOverflows; // # of methods that identified more unnatural loops than we can represent
unsigned iterLoopCount;           // counts the # of loops with an iterator (for like)
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
//    The corresponding enum value from the JIT's var_types
//
// Notes:
//   The gcLayout of each field of a struct is returned from getClassGClayout()
//   as a BYTE[] but each BYTE element is actually a CorInfoGCType value
//   Note when we 'know' that there is only one element in this array
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

#ifdef TARGET_X86
//---------------------------------------------------------------------------
// isTrivialPointerSizedStruct:
//    Check if the given struct type contains only one pointer-sized integer value type
//
// Arguments:
//    clsHnd - the handle for the struct type.
//
// Return Value:
//    true if the given struct type contains only one pointer-sized integer value type,
//    false otherwise.
//
bool Compiler::isTrivialPointerSizedStruct(CORINFO_CLASS_HANDLE clsHnd) const
{
    assert(info.compCompHnd->isValueClass(clsHnd));
    if (info.compCompHnd->getClassSize(clsHnd) != TARGET_POINTER_SIZE)
    {
        return false;
    }
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

        var_types vt = JITtype2varType(fieldType);

        if (fieldType == CORINFO_TYPE_VALUECLASS)
        {
            clsHnd = *pClsHnd;
        }
        else if (varTypeIsI(vt) && !varTypeIsGC(vt))
        {
            return true;
        }
        else
        {
            return false;
        }
    }
}
#endif // TARGET_X86

//---------------------------------------------------------------------------
// isNativePrimitiveStructType:
//    Check if the given struct type is an intrinsic type that should be treated as though
//    it is not a struct at the unmanaged ABI boundary.
//
// Arguments:
//    clsHnd - the handle for the struct type.
//
// Return Value:
//    true if the given struct type should be treated as a primitive for unmanaged calls,
//    false otherwise.
//
bool Compiler::isNativePrimitiveStructType(CORINFO_CLASS_HANDLE clsHnd)
{
    if (!isIntrinsicType(clsHnd))
    {
        return false;
    }
    const char* namespaceName = nullptr;
    const char* typeName      = getClassNameFromMetadata(clsHnd, &namespaceName);

    if (strcmp(namespaceName, "System.Runtime.InteropServices") != 0)
    {
        return false;
    }

    return strcmp(typeName, "CLong") == 0 || strcmp(typeName, "CULong") == 0 || strcmp(typeName, "NFloat") == 0;
}

//-----------------------------------------------------------------------------
// getPrimitiveTypeForStruct:
//     Get the "primitive" type that is used for a struct
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
    if (GlobalJitOptions::compFeatureHfa)
    {
        // Arm64 Windows VarArg methods arguments will not classify HFA types, they will need to be treated
        // as if they are not HFA types.
        if (!(TargetArchitecture::IsArm64 && TargetOS::IsWindows && isVarArg))
        {
            switch (structSize)
            {
                case 4:
                case 8:
#ifdef TARGET_ARM64
                case 16:
#endif // TARGET_ARM64
                {
                    var_types hfaType = GetHfaType(clsHnd);
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
    }

    // Now deal with non-HFA/HVA structs.
    switch (structSize)
    {
        case 1:
            useType = TYP_UBYTE;
            break;

        case 2:
            useType = TYP_USHORT;
            break;

#if !defined(TARGET_XARCH) || defined(UNIX_AMD64_ABI)
        case 3:
            useType = TYP_INT;
            break;

#endif // !TARGET_XARCH || UNIX_AMD64_ABI

#ifdef TARGET_64BIT
        case 4:
            // We dealt with the one-float HFA above. All other 4-byte structs are handled as INT.
            useType = TYP_INT;
            break;

#if !defined(TARGET_XARCH) || defined(UNIX_AMD64_ABI)
        case 5:
        case 6:
        case 7:
            useType = TYP_I_IMPL;
            break;

#endif // !TARGET_XARCH || UNIX_AMD64_ABI
#endif // TARGET_64BIT

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
// Note that on x86 we only pass specific pointer-sized structs that satisfy isTrivialPointerSizedStruct checks.
#ifndef TARGET_X86
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
#else
    if (isTrivialPointerSizedStruct(clsHnd))
    {
        useType = TYP_I_IMPL;
    }
#endif // !TARGET_X86

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
            if (TargetArchitecture::IsArm64 && TargetOS::IsWindows && isVarArg)
            {
                hfaType = TYP_UNDEF;
            }
            else
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

#elif defined(TARGET_ARM64)

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

#elif defined(TARGET_X86) || defined(TARGET_ARM) || defined(TARGET_LOONGARCH64) || defined(TARGET_RISCV64)

                // Otherwise we pass this struct by value on the stack
                // setup wbPassType and useType indicate that this is passed by value according to the X86/ARM32 ABI
                // On LOONGARCH64 struct that is 1-16 bytes is passed by value in one/two register(s)
                howToPassStruct = SPK_ByValue;
                useType         = TYP_STRUCT;

#else //  TARGET_XXX

                noway_assert(!"Unhandled TARGET in getArgTypeForStruct (with FEATURE_MULTIREG_ARGS=1)");

#endif //  TARGET_XXX
            }
        }
        else // (structSize > MAX_PASS_MULTIREG_BYTES)
        {
            // We have a (large) struct that can't be replaced with a "primitive" type
            // and can't be passed in multiple registers
            CLANG_FORMAT_COMMENT_ANCHOR;

#if defined(TARGET_X86) || defined(TARGET_ARM) || defined(UNIX_AMD64_ABI)

            // Otherwise we pass this struct by value on the stack
            // setup wbPassType and useType indicate that this is passed by value according to the X86/ARM32 ABI
            howToPassStruct = SPK_ByValue;
            useType         = TYP_STRUCT;

#elif defined(TARGET_AMD64) || defined(TARGET_ARM64) || defined(TARGET_LOONGARCH64) || defined(TARGET_RISCV64)

            // Otherwise we pass this struct by reference to a copy
            // setup wbPassType and useType indicate that this is passed using one register (by reference to a copy)
            howToPassStruct = SPK_ByReference;
            useType         = TYP_UNKNOWN;

#else //  TARGET_XXX

            noway_assert(!"Unhandled TARGET in getArgTypeForStruct");

#endif //  TARGET_XXX
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
//    callConv       - the calling convention of the function
//                     that returns this struct.
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
var_types Compiler::getReturnTypeForStruct(CORINFO_CLASS_HANDLE     clsHnd,
                                           CorInfoCallConvExtension callConv,
                                           structPassingKind*       wbReturnStruct /* = nullptr */,
                                           unsigned                 structSize /* = 0 */)
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
            // Otherwise, leave as TYP_UNKNOWN and we'll sort things out below.
            useType           = GetEightByteType(structDesc, 0);
            howToReturnStruct = SPK_PrimitiveType;
        }
    }
    else
    {
        // Return classification is not always size based...
        canReturnInRegister = structDesc.passedInRegisters;
        if (!canReturnInRegister)
        {
            assert(structDesc.eightByteCount == 0);
            howToReturnStruct = SPK_ByReference;
            useType           = TYP_UNKNOWN;
        }
    }
#elif UNIX_X86_ABI
    if (callConv != CorInfoCallConvExtension::Managed && !isNativePrimitiveStructType(clsHnd))
    {
        canReturnInRegister = false;
        howToReturnStruct   = SPK_ByReference;
        useType             = TYP_UNKNOWN;
    }
#elif defined(TARGET_LOONGARCH64)
    if (structSize <= (TARGET_POINTER_SIZE * 2))
    {
        uint32_t floatFieldFlags = info.compCompHnd->getLoongArch64PassStructInRegisterFlags(clsHnd);

        if ((floatFieldFlags & STRUCT_FLOAT_FIELD_ONLY_ONE) != 0)
        {
            howToReturnStruct = SPK_PrimitiveType;
            useType           = (structSize > 4) ? TYP_DOUBLE : TYP_FLOAT;
        }
        else if (floatFieldFlags & (STRUCT_HAS_FLOAT_FIELDS_MASK ^ STRUCT_FLOAT_FIELD_ONLY_ONE))
        {
            howToReturnStruct = SPK_ByValue;
            useType           = TYP_STRUCT;
        }
    }

#elif defined(TARGET_RISCV64)
    if (structSize <= (TARGET_POINTER_SIZE * 2))
    {
        uint32_t floatFieldFlags = info.compCompHnd->getRISCV64PassStructInRegisterFlags(clsHnd);

        if ((floatFieldFlags & STRUCT_FLOAT_FIELD_ONLY_ONE) != 0)
        {
            howToReturnStruct = SPK_PrimitiveType;
            useType           = (structSize > 4) ? TYP_DOUBLE : TYP_FLOAT;
        }
        else if (floatFieldFlags & (STRUCT_HAS_FLOAT_FIELDS_MASK ^ STRUCT_FLOAT_FIELD_ONLY_ONE))
        {
            howToReturnStruct = SPK_ByValue;
            useType           = TYP_STRUCT;
        }
    }

#endif
    if (TargetOS::IsWindows && !TargetArchitecture::IsArm32 && callConvIsInstanceMethodCallConv(callConv) &&
        !isNativePrimitiveStructType(clsHnd))
    {
        canReturnInRegister = false;
        howToReturnStruct   = SPK_ByReference;
        useType             = TYP_UNKNOWN;
    }

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

#ifdef TARGET_64BIT
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
    else if (canReturnInRegister) // We can't replace the struct with a "primitive" type
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

                // The cases of (structDesc.eightByteCount == 1) and (structDesc.eightByteCount == 0)
                // should have already been handled
                assert(structDesc.eightByteCount > 1);
                // setup wbPassType and useType indicate that this is returned by value in multiple registers
                howToReturnStruct = SPK_ByValue;
                useType           = TYP_STRUCT;
                assert(structDesc.passedInRegisters == true);

#elif defined(TARGET_ARM64)

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
#elif defined(TARGET_X86)

                // Only 8-byte structs are return in multiple registers.
                // We also only support multireg struct returns on x86 to match the native calling convention.
                // So return 8-byte structs only when the calling convention is a native calling convention.
                if (structSize == MAX_RET_MULTIREG_BYTES && callConv != CorInfoCallConvExtension::Managed)
                {
                    // setup wbPassType and useType indicate that this is return by value in multiple registers
                    howToReturnStruct = SPK_ByValue;
                    useType           = TYP_STRUCT;
                }
                else
                {
                    // Otherwise we return this struct using a return buffer
                    // setup wbPassType and useType indicate that this is returned using a return buffer register
                    //  (reference to a return buffer)
                    howToReturnStruct = SPK_ByReference;
                    useType           = TYP_UNKNOWN;
                }
#elif defined(TARGET_ARM)

                // Otherwise we return this struct using a return buffer
                // setup wbPassType and useType indicate that this is returned using a return buffer register
                //  (reference to a return buffer)
                howToReturnStruct = SPK_ByReference;
                useType           = TYP_UNKNOWN;

#elif defined(TARGET_LOONGARCH64) || defined(TARGET_RISCV64)

                // On LOONGARCH64/RISCV64 struct that is 1-16 bytes is returned by value in one/two register(s)
                howToReturnStruct = SPK_ByValue;
                useType           = TYP_STRUCT;

#else //  TARGET_XXX

                noway_assert(!"Unhandled TARGET in getReturnTypeForStruct (with FEATURE_MULTIREG_ARGS=1)");

#endif //  TARGET_XXX
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
// MEASURE_NOWAY: code to measure and rank dynamic occurrences of noway_assert.
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

    struct compare
    {
        bool operator()(const NowayAssertCountMap& elem1, const NowayAssertCountMap& elem2)
        {
            return (ssize_t)elem2.count < (ssize_t)elem1.count; // sort in descending order
        }
    };
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
                fprintf(jitstdout(), "Failed to open JitMeasureNowayAssertFile \"%ws\"\n",
                        strJitMeasureNowayAssertFile);
                return;
            }
        }
        else
        {
            fout = jitstdout();
        }

        // Iterate noway assert map, create sorted table by occurrence, dump it.
        unsigned             count = NowayAssertMap->GetCount();
        NowayAssertCountMap* nacp  = new NowayAssertCountMap[count];
        unsigned             i     = 0;

        for (FileLineToCountMap::Node* const iter : FileLineToCountMap::KeyValueIteration(NowayAssertMap))
        {
            nacp[i].count = iter->GetValue();
            nacp[i].fl    = iter->GetKey();
            ++i;
        }

        jitstd::sort(nacp, nacp + count, NowayAssertCountMap::compare());

        if (fout == jitstdout())
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

        if (fout != jitstdout())
        {
            fclose(fout);
            fout = nullptr;
        }
    }
}

#endif // MEASURE_NOWAY

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
LONG Compiler::s_compMethodsCount = 0; // to produce unique label names
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
    ValueNumStore::ValidateValueNumStoreStatics();

    compDisplayStaticSizes();
}

/*****************************************************************************
 *
 *  One time finalization code
 */

/* static */
void Compiler::compShutdown()
{
    if (s_pAltJitExcludeAssembliesList != nullptr)
    {
        s_pAltJitExcludeAssembliesList->~AssemblyNamesList2(); // call the destructor
        s_pAltJitExcludeAssembliesList = nullptr;
    }

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

#if defined(DEBUG)
    // Finish reading and/or writing inline xml
    if (JitConfig.JitInlineDumpXmlFile() != nullptr)
    {
        FILE* file = _wfopen(JitConfig.JitInlineDumpXmlFile(), W("a"));
        if (file != nullptr)
        {
            InlineStrategy::FinalizeXml(file);
            fclose(file);
        }
        else
        {
            InlineStrategy::FinalizeXml();
        }
    }
#endif // defined(DEBUG)

#if defined(DEBUG) || MEASURE_NODE_SIZE || MEASURE_BLOCK_SIZE || DISPLAY_SIZES || CALL_ARG_STATS
    if (genMethodCnt == 0)
    {
        return;
    }
#endif

#if NODEBASH_STATS
    GenTree::ReportOperBashing(jitstdout());
#endif

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

    JitTimer::Shutdown();
#endif // FEATURE_JIT_METHOD_PERF

#if COUNT_AST_OPERS

    // Add up all the counts so that we can show percentages of total
    unsigned totalCount = 0;
    for (unsigned op = 0; op < GT_COUNT; op++)
    {
        totalCount += GenTree::s_gtNodeCounts[op];
    }

    if (totalCount > 0)
    {
        struct OperInfo
        {
            unsigned   Count;
            unsigned   Size;
            genTreeOps Oper;
        };

        OperInfo opers[GT_COUNT];
        for (unsigned op = 0; op < GT_COUNT; op++)
        {
            opers[op] = {GenTree::s_gtNodeCounts[op], GenTree::s_gtTrueSizes[op], static_cast<genTreeOps>(op)};
        }

        jitstd::sort(opers, opers + ArrLen(opers), [](const OperInfo& l, const OperInfo& r) {
            // We'll be sorting in descending order.
            return l.Count >= r.Count;
        });

        unsigned remainingCount      = totalCount;
        unsigned remainingCountLarge = 0;
        unsigned remainingCountSmall = 0;

        unsigned countLarge = 0;
        unsigned countSmall = 0;

        jitprintf("\nGenTree operator counts (approximate):\n\n");

        for (OperInfo oper : opers)
        {
            unsigned size       = oper.Size;
            unsigned count      = oper.Count;
            double   percentage = 100.0 * count / totalCount;

            if (size > TREE_NODE_SZ_SMALL)
            {
                countLarge += count;
            }
            else
            {
                countSmall += count;
            }

            // Let's not show anything below a threshold
            if (percentage >= 0.5)
            {
                jitprintf("    GT_%-17s   %7u (%4.1lf%%) %3u bytes each\n", GenTree::OpName(oper.Oper), count,
                          percentage, size);
                remainingCount -= count;
            }
            else
            {
                if (size > TREE_NODE_SZ_SMALL)
                {
                    remainingCountLarge += count;
                }
                else
                {
                    remainingCountSmall += count;
                }
            }
        }

        if (remainingCount > 0)
        {
            jitprintf("    All other GT_xxx ...   %7u (%4.1lf%%) ... %4.1lf%% small + %4.1lf%% large\n", remainingCount,
                      100.0 * remainingCount / totalCount, 100.0 * remainingCountSmall / totalCount,
                      100.0 * remainingCountLarge / totalCount);
        }
        jitprintf("    -----------------------------------------------------\n");
        jitprintf("    Total    .......   %11u --ALL-- ... %4.1lf%% small + %4.1lf%% large\n", totalCount,
                  100.0 * countSmall / totalCount, 100.0 * countLarge / totalCount);
        jitprintf("\n");
    }

#endif // COUNT_AST_OPERS

#if DISPLAY_SIZES

    if (grossVMsize && grossNCsize)
    {
        jitprintf("\n");
        jitprintf("--------------------------------------\n");
        jitprintf("Function and GC info size stats\n");
        jitprintf("--------------------------------------\n");

        jitprintf("[%7u VM, %8u %6s %4u%%] %s\n", grossVMsize, grossNCsize, Target::g_tgtCPUName,
                  100 * grossNCsize / grossVMsize, "Total (excluding GC info)");

        jitprintf("[%7u VM, %8u %6s %4u%%] %s\n", grossVMsize, totalNCsize, Target::g_tgtCPUName,
                  100 * totalNCsize / grossVMsize, "Total (including GC info)");

        if (gcHeaderISize || gcHeaderNSize)
        {
            jitprintf("\n");

            jitprintf("GC tables   : [%7uI,%7uN] %7u byt  (%u%% of IL, %u%% of %s).\n", gcHeaderISize + gcPtrMapISize,
                      gcHeaderNSize + gcPtrMapNSize, totalNCsize - grossNCsize,
                      100 * (totalNCsize - grossNCsize) / grossVMsize, 100 * (totalNCsize - grossNCsize) / grossNCsize,
                      Target::g_tgtCPUName);

            jitprintf("GC headers  : [%7uI,%7uN] %7u byt, [%4.1fI,%4.1fN] %4.1f byt/meth\n", gcHeaderISize,
                      gcHeaderNSize, gcHeaderISize + gcHeaderNSize, (float)gcHeaderISize / (genMethodICnt + 0.001),
                      (float)gcHeaderNSize / (genMethodNCnt + 0.001),
                      (float)(gcHeaderISize + gcHeaderNSize) / genMethodCnt);

            jitprintf("GC ptr maps : [%7uI,%7uN] %7u byt, [%4.1fI,%4.1fN] %4.1f byt/meth\n", gcPtrMapISize,
                      gcPtrMapNSize, gcPtrMapISize + gcPtrMapNSize, (float)gcPtrMapISize / (genMethodICnt + 0.001),
                      (float)gcPtrMapNSize / (genMethodNCnt + 0.001),
                      (float)(gcPtrMapISize + gcPtrMapNSize) / genMethodCnt);
        }
        else
        {
            jitprintf("\n");

            jitprintf("GC tables   take up %u bytes (%u%% of instr, %u%% of %6s code).\n", totalNCsize - grossNCsize,
                      100 * (totalNCsize - grossNCsize) / grossVMsize, 100 * (totalNCsize - grossNCsize) / grossNCsize,
                      Target::g_tgtCPUName);
        }

#ifdef DEBUG
#if DOUBLE_ALIGN
        jitprintf("%u out of %u methods generated with double-aligned stack\n", Compiler::s_lvaDoubleAlignedProcsCount,
                  genMethodCnt);
#endif
#endif
    }

#endif // DISPLAY_SIZES

#if CALL_ARG_STATS
    compDispCallArgStats(jitstdout());
#endif

#if COUNT_BASIC_BLOCKS
    jitprintf("--------------------------------------------------\n");
    jitprintf("Basic block count frequency table:\n");
    jitprintf("--------------------------------------------------\n");
    bbCntTable.dump(jitstdout());
    jitprintf("--------------------------------------------------\n");

    jitprintf("\n");

    jitprintf("--------------------------------------------------\n");
    jitprintf("IL method size frequency table for methods with a single basic block:\n");
    jitprintf("--------------------------------------------------\n");
    bbOneBBSizeTable.dump(jitstdout());
    jitprintf("--------------------------------------------------\n");

    jitprintf("--------------------------------------------------\n");
    jitprintf("fgComputeReachabilitySets `while (change)` iterations:\n");
    jitprintf("--------------------------------------------------\n");
    computeReachabilitySetsIterationTable.dump(jitstdout());
    jitprintf("--------------------------------------------------\n");

#endif // COUNT_BASIC_BLOCKS

#if COUNT_LOOPS

    jitprintf("\n");
    jitprintf("---------------------------------------------------\n");
    jitprintf("Loop stats\n");
    jitprintf("---------------------------------------------------\n");
    jitprintf("Total number of methods with loops is %5u\n", totalLoopMethods);
    jitprintf("Total number of              loops is %5u\n", totalLoopCount);
    jitprintf("Maximum number of loops per method is %5u\n", maxLoopsPerMethod);
    jitprintf("Total number of 'unnatural' loops is %5u\n", totalUnnatLoopCount);
    jitprintf("# of methods overflowing unnat loop limit is %5u\n", totalUnnatLoopOverflows);
    jitprintf("Total number of loops with an         iterator is %5u\n", iterLoopCount);
    jitprintf("Total number of loops with a constant iterator is %5u\n", constIterLoopCount);

    jitprintf("--------------------------------------------------\n");
    jitprintf("Loop count frequency table:\n");
    jitprintf("--------------------------------------------------\n");
    loopCountTable.dump(jitstdout());
    jitprintf("--------------------------------------------------\n");
    jitprintf("Loop exit count frequency table:\n");
    jitprintf("--------------------------------------------------\n");
    loopExitCountTable.dump(jitstdout());
    jitprintf("--------------------------------------------------\n");

#endif // COUNT_LOOPS

#if MEASURE_NODE_SIZE

    jitprintf("\n");
    jitprintf("---------------------------------------------------\n");
    jitprintf("GenTree node allocation stats\n");
    jitprintf("---------------------------------------------------\n");

    jitprintf("Allocated %6I64u tree nodes (%7I64u bytes total, avg %4I64u bytes per method)\n",
              genNodeSizeStats.genTreeNodeCnt, genNodeSizeStats.genTreeNodeSize,
              genNodeSizeStats.genTreeNodeSize / genMethodCnt);

    jitprintf("Allocated %7I64u bytes of unused tree node space (%3.2f%%)\n",
              genNodeSizeStats.genTreeNodeSize - genNodeSizeStats.genTreeNodeActualSize,
              (float)(100 * (genNodeSizeStats.genTreeNodeSize - genNodeSizeStats.genTreeNodeActualSize)) /
                  genNodeSizeStats.genTreeNodeSize);

    jitprintf("\n");
    jitprintf("---------------------------------------------------\n");
    jitprintf("Distribution of per-method GenTree node counts:\n");
    genTreeNcntHist.dump(jitstdout());

    jitprintf("\n");
    jitprintf("---------------------------------------------------\n");
    jitprintf("Distribution of per-method GenTree node  allocations (in bytes):\n");
    genTreeNsizHist.dump(jitstdout());

#endif // MEASURE_NODE_SIZE

#if MEASURE_BLOCK_SIZE

    jitprintf("\n");
    jitprintf("---------------------------------------------------\n");
    jitprintf("BasicBlock and FlowEdge/BasicBlockList allocation stats\n");
    jitprintf("---------------------------------------------------\n");

    jitprintf("Allocated %6u basic blocks (%7u bytes total, avg %4u bytes per method)\n", BasicBlock::s_Count,
              BasicBlock::s_Size, BasicBlock::s_Size / genMethodCnt);
    jitprintf("Allocated %6u flow nodes (%7u bytes total, avg %4u bytes per method)\n", genFlowNodeCnt, genFlowNodeSize,
              genFlowNodeSize / genMethodCnt);

#endif // MEASURE_BLOCK_SIZE

#if MEASURE_MEM_ALLOC

    if (s_dspMemStats)
    {
        jitprintf("\nAll allocations:\n");
        ArenaAllocator::dumpAggregateMemStats(jitstdout());

        jitprintf("\nLargest method:\n");
        ArenaAllocator::dumpMaxMemStats(jitstdout());

        jitprintf("\n");
        jitprintf("---------------------------------------------------\n");
        jitprintf("Distribution of total memory allocated per method (in KB):\n");
        memAllocHist.dump(jitstdout());

        jitprintf("\n");
        jitprintf("---------------------------------------------------\n");
        jitprintf("Distribution of total memory used      per method (in KB):\n");
        memUsedHist.dump(jitstdout());
    }

#endif // MEASURE_MEM_ALLOC

#if LOOP_HOIST_STATS
#ifdef DEBUG // Always display loop stats in retail
    if (JitConfig.DisplayLoopHoistStats() != 0)
#endif // DEBUG
    {
        PrintAggregateLoopHoistStats(jitstdout());
    }
#endif // LOOP_HOIST_STATS

#if TRACK_ENREG_STATS
    if (JitConfig.JitEnregStats() != 0)
    {
        s_enregisterStats.Dump(jitstdout());
    }
#endif // TRACK_ENREG_STATS

#if MEASURE_PTRTAB_SIZE

    jitprintf("\n");
    jitprintf("---------------------------------------------------\n");
    jitprintf("GC pointer table stats\n");
    jitprintf("---------------------------------------------------\n");

    jitprintf("Reg pointer descriptor size (internal): %8u (avg %4u per method)\n", GCInfo::s_gcRegPtrDscSize,
              GCInfo::s_gcRegPtrDscSize / genMethodCnt);

    jitprintf("Total pointer table size: %8u (avg %4u per method)\n", GCInfo::s_gcTotalPtrTabSize,
              GCInfo::s_gcTotalPtrTabSize / genMethodCnt);

#endif // MEASURE_PTRTAB_SIZE

#if MEASURE_NODE_SIZE || MEASURE_BLOCK_SIZE || MEASURE_PTRTAB_SIZE || DISPLAY_SIZES

    if (genMethodCnt != 0)
    {
        jitprintf("\n");
        jitprintf("A total of %6u methods compiled", genMethodCnt);
#if DISPLAY_SIZES
        if (genMethodICnt || genMethodNCnt)
        {
            jitprintf(" (%u interruptible, %u non-interruptible)", genMethodICnt, genMethodNCnt);
        }
#endif // DISPLAY_SIZES
        jitprintf(".\n");
    }

#endif // MEASURE_NODE_SIZE || MEASURE_BLOCK_SIZE || MEASURE_PTRTAB_SIZE || DISPLAY_SIZES

#if EMITTER_STATS
    emitterStats(jitstdout());
#endif

#if MEASURE_FATAL
    jitprintf("\n");
    jitprintf("---------------------------------------------------\n");
    jitprintf("Fatal errors stats\n");
    jitprintf("---------------------------------------------------\n");
    jitprintf("   badCode:             %u\n", fatal_badCode);
    jitprintf("   noWay:               %u\n", fatal_noWay);
    jitprintf("   implLimitation:      %u\n", fatal_implLimitation);
    jitprintf("   NOMEM:               %u\n", fatal_NOMEM);
    jitprintf("   noWayAssertBody:     %u\n", fatal_noWayAssertBody);
#ifdef DEBUG
    jitprintf("   noWayAssertBodyArgs: %u\n", fatal_noWayAssertBodyArgs);
#endif // DEBUG
    jitprintf("   NYI:                 %u\n", fatal_NYI);
#endif // MEASURE_FATAL

#if CALL_ARG_STATS || COUNT_BASIC_BLOCKS || COUNT_LOOPS || EMITTER_STATS || MEASURE_NODE_SIZE || MEASURE_MEM_ALLOC
    DumpOnShutdown::DumpAll();
#endif
}

/*****************************************************************************
 *  Display static data structure sizes.
 */

/* static */
void Compiler::compDisplayStaticSizes()
{
#if MEASURE_NODE_SIZE
    GenTree::DumpNodeSizes();
#endif

#if EMITTER_STATS
    emitterStaticStats();
#endif
}

/*****************************************************************************
 *
 *  Constructor
 */
void Compiler::compInit(ArenaAllocator*       pAlloc,
                        CORINFO_METHOD_HANDLE methodHnd,
                        COMP_HANDLE           compHnd,
                        CORINFO_METHOD_INFO*  methodInfo,
                        InlineInfo*           inlineInfo)
{
    assert(pAlloc);
    compArenaAllocator = pAlloc;

    // Inlinee Compile object will only be allocated when needed for the 1st time.
    InlineeCompiler = nullptr;

    // Set the inline info.
    impInlineInfo       = inlineInfo;
    info.compCompHnd    = compHnd;
    info.compMethodHnd  = methodHnd;
    info.compMethodInfo = methodInfo;
    info.compClassHnd   = compHnd->getMethodClass(methodHnd);

#ifdef DEBUG
    compAllowStress = true;

    // set this early so we can use it without relying on random memory values
    verbose = compIsForInlining() ? impInlineInfo->InlinerCompiler->verbose : false;

    compNumStatementLinksTraversed = 0;
    compPoisoningAnyImplicitByrefs = false;
#endif

#if defined(DEBUG) || defined(LATE_DISASM) || DUMP_FLOWGRAPHS || DUMP_GC_TABLES
    // Initialize the method name and related info, as it is used early in determining whether to
    // apply stress modes, and which ones to apply.
    // Note that even allocating memory can invoke the stress mechanism, so ensure that both
    // 'compMethodName' and 'compFullName' are either null or valid before we allocate.
    // (The stress mode checks references these prior to checking bRangeAllowStress.)
    //
    info.compMethodName = nullptr;
    info.compClassName  = nullptr;
    info.compFullName   = nullptr;

    info.compMethodName = eeGetMethodName(methodHnd);
    info.compClassName  = eeGetClassName(info.compClassHnd);
    info.compFullName   = eeGetMethodFullName(methodHnd);
    info.compPerfScore  = 0.0;

    info.compMethodSuperPMIIndex = g_jitHost->getIntConfigValue(W("SuperPMIMethodContextNumber"), -1);
#endif // defined(DEBUG) || defined(LATE_DISASM) || DUMP_FLOWGRAPHS

#if defined(DEBUG)
    info.compMethodHashPrivate = 0;
#endif // defined(DEBUG)

#ifdef DEBUG
    // Opt-in to jit stress based on method hash ranges.
    //
    // Note the default (with JitStressRange not set) is that all
    // methods will be subject to stress.
    static ConfigMethodRange fJitStressRange;
    fJitStressRange.EnsureInit(JitConfig.JitStressRange());
    assert(!fJitStressRange.Error());
    compAllowStress =
        fJitStressRange.Contains(info.compMethodHash()) &&
        (JitConfig.JitStressOnly().isEmpty() ||
         JitConfig.JitStressOnly().contains(info.compMethodHnd, info.compClassHnd, &info.compMethodInfo->args));

#endif // DEBUG

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

    // Initialize this to the first phase to run.
    mostRecentlyActivePhase = PHASE_PRE_IMPORT;

    // Initially, no phase checks are active, and all dumps are enabled.
    activePhaseChecks = PhaseChecks::CHECK_NONE;
    activePhaseDumps  = PhaseDumps::DUMP_ALL;

    fgInit();
    lvaInit();
    optInit();

    if (!compIsForInlining())
    {
        codeGen = getCodeGenerator(this);
        hashBv::Init(this);
        compVarScopeMap = nullptr;

        // If this method were a real constructor for Compiler, these would
        // become method initializations.
        impPendingBlockMembers    = JitExpandArray<BYTE>(getAllocator());
        impSpillCliquePredMembers = JitExpandArray<BYTE>(getAllocator());
        impSpillCliqueSuccMembers = JitExpandArray<BYTE>(getAllocator());

        new (&genIPmappings, jitstd::placement_t()) jitstd::list<IPmappingDsc>(getAllocator(CMK_DebugInfo));
        new (&genRichIPmappings, jitstd::placement_t()) jitstd::list<RichIPMapping>(getAllocator(CMK_DebugOnly));

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
    compTailPrefixSeen    = false;
    compLocallocSeen      = false;
    compLocallocUsed      = false;
    compLocallocOptimized = false;
    compQmarkRationalized = false;
    compQmarkUsed         = false;
    compFloatingPointUsed = false;

    compSuppressedZeroInit = false;

    compNeedsGSSecurityCookie = false;
    compGSReorderStackLayout  = false;

    compGeneratingProlog       = false;
    compGeneratingEpilog       = false;
    compGeneratingUnwindProlog = false;
    compGeneratingUnwindEpilog = false;

    compPostImportationCleanupDone = false;
    compLSRADone                   = false;
    compRationalIRForm             = false;

#ifdef DEBUG
    compCodeGenDone        = false;
    opts.compMinOptsIsUsed = false;
#endif
    opts.compMinOptsIsSet = false;

    // Used by fgFindJumpTargets for inlining heuristics.
    opts.instrCount = 0;

    // Used to track when we should consider running EarlyProp
    optMethodFlags       = 0;
    optNoReturnCallCount = 0;

#ifdef DEBUG
    m_nodeTestData      = nullptr;
    m_loopHoistCSEClass = FIRST_LOOP_HOIST_CSE_CLASS;
#endif
    m_switchDescMap  = nullptr;
    m_blockToEHPreds = nullptr;
    m_dominancePreds = nullptr;
    m_fieldSeqStore  = nullptr;
    m_refAnyClass    = nullptr;
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

    vnStore                    = nullptr;
    m_outlinedCompositeSsaNums = nullptr;
    m_nodeToLoopMemoryBlockMap = nullptr;
    m_signatureToLookupInfoMap = nullptr;
    fgSsaPassesCompleted       = 0;
    fgSsaValid                 = false;
    fgVNPassesCompleted        = 0;

    // check that HelperCallProperties are initialized

    assert(s_helperCallProperties.IsPure(CORINFO_HELP_GETSHARED_GCSTATIC_BASE));
    assert(!s_helperCallProperties.IsPure(CORINFO_HELP_GETFIELDOBJ)); // quick sanity check

    // We start with the flow graph in tree-order
    fgOrder = FGOrderTree;

    m_classLayoutTable = nullptr;

#ifdef FEATURE_SIMD
    m_simdHandleCache = nullptr;
#endif // FEATURE_SIMD

    compUsesThrowHelper = false;

    impLclValues = nullptr;
    impLclMapSize = 0;

    m_preferredInitCctor = CORINFO_HELP_UNDEF;
}

/*****************************************************************************
 *
 *  Destructor
 */

void Compiler::compDone()
{
#ifdef LATE_DISASM
    if (codeGen != nullptr)
    {
        codeGen->getDisAssembler().disDone();
    }
#endif // LATE_DISASM
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
//    The default value is taken from DOTNET_JitDefaultFill,  if is not set
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
            if ((varDsc->lvRegister != 0) && (varDsc->GetRegNum() == reg) &&
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

#endif // DEBUG

const char* Compiler::compRegVarName(regNumber reg, bool displayVar, bool isFloatReg)
{
#ifdef TARGET_ARM
    isFloatReg = genIsValidFloatReg(reg);
#endif

#ifdef DEBUG
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
            sprintf_s(nameVarReg[index], NAME_VAR_REG_BUFFER_LEN, "%s'%s'", getRegName(reg), VarNameToStr(varName));

            return nameVarReg[index];
        }
    }
#endif

    /* no debug info required or no variable in that register
       -> return standard name */

    return getRegName(reg);
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
#ifdef TARGET_AMD64
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
#endif // TARGET_AMD64
    };
    // clang-format on

    assert(isByteReg(reg));
    assert(genRegMask(reg) & RBM_BYTE_REGS);
    assert(size == 1 || size == 2);

    return sizeNames[reg][size - 1];
}

#ifdef DEBUG
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
#endif

/*****************************************************************************/

void Compiler::compSetProcessor()
{
    //
    // NOTE: This function needs to be kept in sync with EEJitManager::SetCpuInfo() in vm\codeman.cpp
    //

    const JitFlags& jitFlags = *opts.jitFlags;

    //
    // Processor specific optimizations
    //
    CLANG_FORMAT_COMMENT_ANCHOR;

    CORINFO_InstructionSetFlags instructionSetFlags = jitFlags.GetInstructionSetFlags();
    opts.compSupportsISA.Reset();
    opts.compSupportsISAReported.Reset();
    opts.compSupportsISAExactly.Reset();

// The VM will set the ISA flags depending on actual hardware support
// and any specified config switches specified by the user. The exception
// here is for certain "artificial ISAs" such as Vector64/128/256 where they
// don't actually exist. The JIT is in charge of adding those and ensuring
// the total sum of flags is still valid.
#if defined(TARGET_XARCH)
    // Get the preferred vector bitwidth, rounding down to the nearest multiple of 128-bits
    uint32_t preferredVectorBitWidth   = (ReinterpretHexAsDecimal(JitConfig.PreferredVectorBitWidth()) / 128) * 128;
    uint32_t preferredVectorByteLength = preferredVectorBitWidth / 8;

    if (instructionSetFlags.HasInstructionSet(InstructionSet_SSE))
    {
        instructionSetFlags.AddInstructionSet(InstructionSet_Vector128);
    }

    if (instructionSetFlags.HasInstructionSet(InstructionSet_AVX))
    {
        instructionSetFlags.AddInstructionSet(InstructionSet_Vector256);
    }

    // x86-64-v4 feature level supports AVX512F, AVX512BW, AVX512CD, AVX512DQ, AVX512VL
    // These have been shipped together historically and at the time of this writing
    // there exists no hardware which doesn't support the entire feature set. To simplify
    // the overall JIT implementation, we currently require the entire set of ISAs to be
    // supported and disable AVX512 support otherwise.

    if (instructionSetFlags.HasInstructionSet(InstructionSet_AVX512F))
    {
        assert(instructionSetFlags.HasInstructionSet(InstructionSet_AVX512F));
        assert(instructionSetFlags.HasInstructionSet(InstructionSet_AVX512F_VL));
        assert(instructionSetFlags.HasInstructionSet(InstructionSet_AVX512BW));
        assert(instructionSetFlags.HasInstructionSet(InstructionSet_AVX512BW_VL));
        assert(instructionSetFlags.HasInstructionSet(InstructionSet_AVX512CD));
        assert(instructionSetFlags.HasInstructionSet(InstructionSet_AVX512CD_VL));
        assert(instructionSetFlags.HasInstructionSet(InstructionSet_AVX512DQ));
        assert(instructionSetFlags.HasInstructionSet(InstructionSet_AVX512DQ_VL));

        instructionSetFlags.AddInstructionSet(InstructionSet_Vector512);

        if ((preferredVectorByteLength == 0) && jitFlags.IsSet(JitFlags::JIT_FLAG_VECTOR512_THROTTLING))
        {
            // Some architectures can experience frequency throttling when
            // executing 512-bit width instructions. To account for this we set the
            // default preferred vector width to 256-bits in some scenarios. Power
            // users can override this with `DOTNET_PreferredVectorBitWidth=512` to
            // allow using such instructions where hardware support is available.
            //
            // Do not condition this based on stress mode as it makes the support
            // reported inconsistent across methods and breaks expectations/functionality

            preferredVectorByteLength = 256 / 8;
        }
    }

    opts.preferredVectorByteLength = preferredVectorByteLength;
#elif defined(TARGET_ARM64)
    if (instructionSetFlags.HasInstructionSet(InstructionSet_AdvSimd))
    {
        instructionSetFlags.AddInstructionSet(InstructionSet_Vector64);
        instructionSetFlags.AddInstructionSet(InstructionSet_Vector128);
    }
#endif // TARGET_ARM64

    assert(instructionSetFlags.Equals(EnsureInstructionSetFlagsAreValid(instructionSetFlags)));
    opts.setSupportedISAs(instructionSetFlags);

#ifdef TARGET_XARCH
    if (!compIsForInlining())
    {
        if (canUseVexEncoding())
        {
            codeGen->GetEmitter()->SetUseVEXEncoding(true);
            // Assume each JITted method does not contain AVX instruction at first
            codeGen->GetEmitter()->SetContainsAVX(false);
            codeGen->GetEmitter()->SetContains256bitOrMoreAVX(false);
            codeGen->GetEmitter()->SetContainsCallNeedingVzeroupper(false);
        }
        if (canUseEvexEncoding())
        {
            codeGen->GetEmitter()->SetUseEvexEncoding(true);
            // TODO-XArch-AVX512 : Revisit other flags to be set once avx512 instructions are added.
        }
    }
#endif // TARGET_XARCH
}

bool Compiler::notifyInstructionSetUsage(CORINFO_InstructionSet isa, bool supported) const
{
    const char* isaString = InstructionSetToString(isa);
    JITDUMP("Notify VM instruction set (%s) %s be supported.\n", isaString, supported ? "must" : "must not");
    return info.compCompHnd->notifyInstructionSetUsage(isa, supported);
}

#ifdef PROFILING_SUPPORTED
// A Dummy routine to receive Enter/Leave/Tailcall profiler callbacks.
// These are used when DOTNET_JitEltHookEnabled=1
#ifdef TARGET_AMD64
void DummyProfilerELTStub(UINT_PTR ProfilerHandle, UINT_PTR callerSP)
{
    return;
}
#else  //! TARGET_AMD64
void DummyProfilerELTStub(UINT_PTR ProfilerHandle)
{
    return;
}
#endif //! TARGET_AMD64

#endif // PROFILING_SUPPORTED

bool Compiler::compShouldThrowOnNoway()
{
    // In min opts, we don't want the noway assert to go through the exception
    // path. Instead we want it to just silently go through codegen for
    // compat reasons.
    return !opts.MinOpts();
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
    opts = {};

    if (compIsForInlining())
    {
        // The following flags are lost when inlining. (They are removed in
        // Compiler::fgInvokeInlineeCompiler().)
        assert(!jitFlags->IsSet(JitFlags::JIT_FLAG_BBINSTR));
        assert(!jitFlags->IsSet(JitFlags::JIT_FLAG_BBINSTR_IF_LOOPS));
        assert(!jitFlags->IsSet(JitFlags::JIT_FLAG_PROF_ENTERLEAVE));
        assert(!jitFlags->IsSet(JitFlags::JIT_FLAG_DEBUG_EnC));
        assert(!jitFlags->IsSet(JitFlags::JIT_FLAG_REVERSE_PINVOKE));
        assert(!jitFlags->IsSet(JitFlags::JIT_FLAG_TRACK_TRANSITIONS));
    }

    opts.jitFlags  = jitFlags;
    opts.compFlags = CLFLG_MAXOPT; // Default value is for full optimization

    if (jitFlags->IsSet(JitFlags::JIT_FLAG_DEBUG_CODE) || jitFlags->IsSet(JitFlags::JIT_FLAG_MIN_OPT) ||
        jitFlags->IsSet(JitFlags::JIT_FLAG_TIER0))
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

#ifdef DEBUG
    opts.compJitAlignLoopAdaptive       = JitConfig.JitAlignLoopAdaptive() == 1;
    opts.compJitAlignLoopBoundary       = (unsigned short)JitConfig.JitAlignLoopBoundary();
    opts.compJitAlignLoopMinBlockWeight = (unsigned short)JitConfig.JitAlignLoopMinBlockWeight();

    opts.compJitAlignLoopForJcc             = JitConfig.JitAlignLoopForJcc() == 1;
    opts.compJitAlignLoopMaxCodeSize        = (unsigned short)JitConfig.JitAlignLoopMaxCodeSize();
    opts.compJitHideAlignBehindJmp          = JitConfig.JitHideAlignBehindJmp() == 1;
    opts.compJitOptimizeStructHiddenBuffer  = JitConfig.JitOptimizeStructHiddenBuffer() == 1;
    opts.compJitUnrollLoopMaxIterationCount = (unsigned short)JitConfig.JitUnrollLoopMaxIterationCount();
#else
    opts.compJitAlignLoopAdaptive           = true;
    opts.compJitAlignLoopBoundary           = DEFAULT_ALIGN_LOOP_BOUNDARY;
    opts.compJitAlignLoopMinBlockWeight     = DEFAULT_ALIGN_LOOP_MIN_BLOCK_WEIGHT;
    opts.compJitAlignLoopMaxCodeSize        = DEFAULT_MAX_LOOPSIZE_FOR_ALIGN;
    opts.compJitHideAlignBehindJmp          = true;
    opts.compJitOptimizeStructHiddenBuffer  = true;
    opts.compJitUnrollLoopMaxIterationCount = DEFAULT_UNROLL_LOOP_MAX_ITERATION_COUNT;
#endif

#ifdef TARGET_XARCH
    if (opts.compJitAlignLoopAdaptive)
    {
        // For adaptive alignment, padding limit is equal to the max instruction encoding
        // size which is 15 bytes. Hence (32 >> 1) - 1 = 15 bytes.
        opts.compJitAlignPaddingLimit = (opts.compJitAlignLoopBoundary >> 1) - 1;
    }
    else
    {
        // For non-adaptive alignment, padding limit is 1 less than the alignment boundary
        // specified.
        opts.compJitAlignPaddingLimit = opts.compJitAlignLoopBoundary - 1;
    }
#elif TARGET_ARM64
    if (opts.compJitAlignLoopAdaptive)
    {
        // For adaptive alignment, padding limit is same as specified by the alignment
        // boundary because all instructions are 4 bytes long. Hence (32 >> 1) = 16 bytes.
        opts.compJitAlignPaddingLimit = (opts.compJitAlignLoopBoundary >> 1);
    }
    else
    {
        // For non-adaptive, padding limit is same as specified by the alignment.
        opts.compJitAlignPaddingLimit = opts.compJitAlignLoopBoundary;
    }
#endif

    assert(isPow2(opts.compJitAlignLoopBoundary));
#ifdef TARGET_ARM64
    // The minimum encoding size for Arm64 is 4 bytes.
    assert(opts.compJitAlignLoopBoundary >= 4);
#endif

#if REGEN_SHORTCUTS || REGEN_CALLPAT
    // We never want to have debugging enabled when regenerating GC encoding patterns
    opts.compDbgCode = false;
    opts.compDbgInfo = false;
    opts.compDbgEnC  = false;
#endif

    compSetProcessor();

#ifdef DEBUG
    opts.dspOrder = false;

    // Optionally suppress inliner compiler instance dumping.
    //
    if (compIsForInlining())
    {
        if (JitConfig.JitDumpInlinePhases() > 0)
        {
            verbose = impInlineInfo->InlinerCompiler->verbose;
        }
        else
        {
            verbose = false;
        }
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

    opts.altJit = false;

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

    if (jitFlags->IsSet(JitFlags::JIT_FLAG_ALT_JIT))
    {
        if (pfAltJit->contains(info.compMethodHnd, info.compClassHnd, &info.compMethodInfo->args))
        {
            opts.altJit = true;
        }

        unsigned altJitLimit = ReinterpretHexAsDecimal(JitConfig.AltJitLimit());
        if (altJitLimit > 0 && Compiler::jitTotalMethodCompiled >= altJitLimit)
        {
            opts.altJit = false;
        }
    }

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

    if (jitFlags->IsSet(JitFlags::JIT_FLAG_ALT_JIT))
    {
        // In release mode, you either get all methods or no methods. You must use "*" as the parameter, or we ignore
        // it. You don't get to give a regular expression of methods to match.
        // (Partially, this is because we haven't computed and stored the method and class name except in debug, and it
        // might be expensive to do so.)
        if ((altJitVal != nullptr) && (strcmp(altJitVal, "*") == 0))
        {
            opts.altJit = true;
        }
    }

#endif // !DEBUG

    // Take care of DOTNET_AltJitExcludeAssemblies.
    if (opts.altJit)
    {
        // First, initialize the AltJitExcludeAssemblies list, but only do it once.
        if (!s_pAltJitExcludeAssembliesListInitialized)
        {
            const WCHAR* wszAltJitExcludeAssemblyList = JitConfig.AltJitExcludeAssemblies();
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

#ifdef DEBUG

    // Setup assembly name list for disassembly and dump, if not already set up.
    if (!s_pJitDisasmIncludeAssembliesListInitialized)
    {
        const WCHAR* assemblyNameList = JitConfig.JitDisasmAssemblies();
        if (assemblyNameList != nullptr)
        {
            s_pJitDisasmIncludeAssembliesList = new (HostAllocator::getHostAllocator())
                AssemblyNamesList2(assemblyNameList, HostAllocator::getHostAllocator());
        }
        s_pJitDisasmIncludeAssembliesListInitialized = true;
    }

    // Check for a specific set of assemblies to dump.
    // If we have an assembly name list for disassembly, also check this method's assembly.
    bool assemblyInIncludeList = true; // assume we'll dump, if there's not an include list (or it's empty).
    if (s_pJitDisasmIncludeAssembliesList != nullptr && !s_pJitDisasmIncludeAssembliesList->IsEmpty())
    {
        const char* assemblyName = info.compCompHnd->getAssemblyName(
            info.compCompHnd->getModuleAssembly(info.compCompHnd->getClassModule(info.compClassHnd)));
        if (!s_pJitDisasmIncludeAssembliesList->IsInList(assemblyName))
        {
            // We have a list, and the current assembly is not in it, so we won't dump.
            assemblyInIncludeList = false;
        }
    }

    bool altJitConfig = !pfAltJit->isEmpty();

    bool verboseDump = false;

    if (!altJitConfig || opts.altJit)
    {
        // We should only enable 'verboseDump' when we are actually compiling a matching method
        // and not enable it when we are just considering inlining a matching method.
        //
        if (!compIsForInlining())
        {
            if (JitConfig.JitDump().contains(info.compMethodHnd, info.compClassHnd, &info.compMethodInfo->args))
            {
                verboseDump = true;
            }
            unsigned jitHashDumpVal = (unsigned)JitConfig.JitHashDump();
            if ((jitHashDumpVal != (DWORD)-1) && (jitHashDumpVal == info.compMethodHash()))
            {
                verboseDump = true;
            }
        }
    }

    // Optionally suppress dumping if not in specified list of included assemblies.
    //
    if (verboseDump && !assemblyInIncludeList)
    {
        verboseDump = false;
    }

    // Optionally suppress dumping Tier0 jit requests.
    //
    if (verboseDump && jitFlags->IsSet(JitFlags::JIT_FLAG_TIER0))
    {
        verboseDump = (JitConfig.JitDumpTier0() > 0);
    }

    // Optionally suppress dumping OSR jit requests.
    //
    if (verboseDump && jitFlags->IsSet(JitFlags::JIT_FLAG_OSR))
    {
        verboseDump = (JitConfig.JitDumpOSR() > 0);
    }

    // Optionally suppress dumping except for a specific OSR jit request.
    //
    const int dumpAtOSROffset = JitConfig.JitDumpAtOSROffset();

    if (verboseDump && (dumpAtOSROffset != -1))
    {
        if (jitFlags->IsSet(JitFlags::JIT_FLAG_OSR))
        {
            verboseDump = (((IL_OFFSET)dumpAtOSROffset) == info.compILEntry);
        }
        else
        {
            verboseDump = false;
        }
    }

    if (verboseDump)
    {
        verbose = true;
    }
#endif // DEBUG

#ifdef FEATURE_SIMD
    setUsesSIMDTypes(false);
#endif // FEATURE_SIMD

    lvaEnregEHVars       = (compEnregLocals() && JitConfig.EnableEHWriteThru());
    lvaEnregMultiRegVars = (compEnregLocals() && JitConfig.EnableMultiRegLocals());

#if FEATURE_TAILCALL_OPT
    // By default opportunistic tail call optimization is enabled.
    // Recognition is done in the importer so this must be set for
    // inlinees as well.
    opts.compTailCallOpt = true;
#endif // FEATURE_TAILCALL_OPT

#if FEATURE_FASTTAILCALL
    // By default fast tail calls are enabled.
    opts.compFastTailCalls = true;
#endif // FEATURE_FASTTAILCALL

    // Profile data
    //
    fgPgoSchema      = nullptr;
    fgPgoData        = nullptr;
    fgPgoSchemaCount = 0;
    fgPgoQueryResult = E_FAIL;
    fgPgoFailReason  = nullptr;
    fgPgoSource      = ICorJitInfo::PgoSource::Unknown;
    fgPgoHaveWeights = false;

    if (jitFlags->IsSet(JitFlags::JIT_FLAG_BBOPT))
    {
        fgPgoQueryResult = info.compCompHnd->getPgoInstrumentationResults(info.compMethodHnd, &fgPgoSchema,
                                                                          &fgPgoSchemaCount, &fgPgoData, &fgPgoSource);

        // a failed result that also has a non-NULL fgPgoSchema
        // indicates that the ILSize for the method no longer matches
        // the ILSize for the method when profile data was collected.
        //
        // We will discard the IBC data in this case
        //
        if (FAILED(fgPgoQueryResult))
        {
            fgPgoFailReason = (fgPgoSchema != nullptr) ? "No matching PGO data" : "No PGO data";
            fgPgoData       = nullptr;
            fgPgoSchema     = nullptr;
        }
        // Optionally, disable use of profile data.
        //
        else if (JitConfig.JitDisablePGO() > 0)
        {
            fgPgoFailReason  = "PGO data available, but JitDisablePGO > 0";
            fgPgoQueryResult = E_FAIL;
            fgPgoData        = nullptr;
            fgPgoSchema      = nullptr;
            fgPgoDisabled    = true;
        }
#ifdef DEBUG
        // Optionally, enable use of profile data for only some methods.
        //
        else
        {
            static ConfigMethodRange JitEnablePGORange;
            JitEnablePGORange.EnsureInit(JitConfig.JitEnablePGORange());

            // Base this decision on the root method hash, so a method either sees all available
            // profile data (including that for inlinees), or none of it.
            //
            const unsigned hash = impInlineRoot()->info.compMethodHash();
            if (!JitEnablePGORange.Contains(hash))
            {
                fgPgoFailReason  = "PGO data available, but method hash NOT within JitEnablePGORange";
                fgPgoQueryResult = E_FAIL;
                fgPgoData        = nullptr;
                fgPgoSchema      = nullptr;
                fgPgoDisabled    = true;
            }
        }

        // A successful result implies a non-NULL fgPgoSchema
        //
        if (SUCCEEDED(fgPgoQueryResult))
        {
            assert(fgPgoSchema != nullptr);

            for (UINT32 i = 0; i < fgPgoSchemaCount; i++)
            {
                ICorJitInfo::PgoInstrumentationKind kind = fgPgoSchema[i].InstrumentationKind;
                if (kind == ICorJitInfo::PgoInstrumentationKind::BasicBlockIntCount ||
                    kind == ICorJitInfo::PgoInstrumentationKind::BasicBlockLongCount ||
                    kind == ICorJitInfo::PgoInstrumentationKind::EdgeIntCount ||
                    kind == ICorJitInfo::PgoInstrumentationKind::EdgeLongCount)
                {
                    fgPgoHaveWeights = true;
                    break;
                }
            }
        }

        // A failed result implies a NULL fgPgoSchema
        //   see implementation of Compiler::fgHaveProfileData()
        //
        if (FAILED(fgPgoQueryResult))
        {
            assert(fgPgoSchema == nullptr);
        }
#endif // DEBUG
    }

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

#if defined(TARGET_ARM64)
    // 0 is default: use the appropriate frame type based on the function.
    opts.compJitSaveFpLrWithCalleeSavedRegisters = 0;
#endif // defined(TARGET_ARM64)

    opts.disAsm       = false;
    opts.disDiffable  = false;
    opts.dspDiffable  = false;
    opts.disAlignment = false;
    opts.disCodeBytes = false;

#ifdef DEBUG
    opts.dspInstrs       = false;
    opts.dspLines        = false;
    opts.varNames        = false;
    opts.disAsmSpilled   = false;
    opts.disAddr         = false;
    opts.dspCode         = false;
    opts.dspEHTable      = false;
    opts.dspDebugInfo    = false;
    opts.dspGCtbls       = false;
    opts.dspMetrics      = false;
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
        bool disEnabled = true;

        // Optionally suppress dumping if not in specified list of included assemblies.
        //
        if (!assemblyInIncludeList)
        {
            disEnabled = false;
        }

        if (disEnabled)
        {
            if ((JitConfig.JitOrder() & 1) == 1)
            {
                opts.dspOrder = true;
            }

            if (JitConfig.JitGCDump().contains(info.compMethodHnd, info.compClassHnd, &info.compMethodInfo->args))
            {
                opts.dspGCtbls = true;
            }

            if (JitConfig.JitDisasm().contains(info.compMethodHnd, info.compClassHnd, &info.compMethodInfo->args))
            {
                opts.disAsm = true;
            }

            if (JitConfig.JitDisasmSpilled())
            {
                opts.disAsmSpilled = true;
            }

            if (JitConfig.JitUnwindDump().contains(info.compMethodHnd, info.compClassHnd, &info.compMethodInfo->args))
            {
                opts.dspUnwind = true;
            }

            if (JitConfig.JitEHDump().contains(info.compMethodHnd, info.compClassHnd, &info.compMethodInfo->args))
            {
                opts.dspEHTable = true;
            }

            if (JitConfig.JitDebugDump().contains(info.compMethodHnd, info.compClassHnd, &info.compMethodInfo->args))
            {
                opts.dspDebugInfo = true;
            }
        }

        if (opts.disAsm && JitConfig.JitDisasmWithGC())
        {
            opts.disasmWithGC = true;
        }

#ifdef LATE_DISASM
        if (JitConfig.JitLateDisasm().contains(info.compMethodHnd, info.compClassHnd, &info.compMethodInfo->args))
        {
            opts.doLateDisasm = true;
        }
#endif // LATE_DISASM

        if (JitConfig.JitDasmWithAddress() != 0)
        {
            opts.disAddr = true;
        }

        if (JitConfig.JitLongAddress() != 0)
        {
            opts.compLongAddress = true;
        }

        if (JitConfig.JitOptRepeat().contains(info.compMethodHnd, info.compClassHnd, &info.compMethodInfo->args))
        {
            opts.optRepeat = true;
        }

        opts.dspMetrics = (JitConfig.JitMetrics() != 0);
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
        printf("Generating code for %s %s\n", Target::g_tgtPlatformName(), Target::g_tgtCPUName);
        printf(""); // in our logic this causes a flush
    }

    if (JitConfig.JitBreak().contains(info.compMethodHnd, info.compClassHnd, &info.compMethodInfo->args))
    {
        assert(!"JitBreak reached");
    }

    unsigned jitHashBreakVal = (unsigned)JitConfig.JitHashBreak();
    if ((jitHashBreakVal != (DWORD)-1) && (jitHashBreakVal == info.compMethodHash()))
    {
        assert(!"JitHashBreak reached");
    }

    if (verbose ||
        JitConfig.JitDebugBreak().contains(info.compMethodHnd, info.compClassHnd, &info.compMethodInfo->args) ||
        JitConfig.JitBreak().contains(info.compMethodHnd, info.compClassHnd, &info.compMethodInfo->args))
    {
        compDebugBreak = true;
    }

    memset(compActiveStressModes, 0, sizeof(compActiveStressModes));

    // Read function list, if not already read, and there exists such a list.
    if (!s_pJitFunctionFileInitialized)
    {
        const WCHAR* functionFileName = JitConfig.JitFunctionFile();
        if (functionFileName != nullptr)
        {
            s_pJitMethodSet =
                new (HostAllocator::getHostAllocator()) MethodSet(functionFileName, HostAllocator::getHostAllocator());
        }
        s_pJitFunctionFileInitialized = true;
    }
#else  // DEBUG
    if (JitConfig.JitDisasm().contains(info.compMethodHnd, info.compClassHnd, &info.compMethodInfo->args))
    {
        opts.disAsm = true;
    }
#endif // !DEBUG

#ifndef DEBUG
    if (opts.disAsm)
#endif
    {
        if (JitConfig.JitDisasmTesting())
        {
            opts.disTesting = true;
        }
        if (JitConfig.JitDisasmWithAlignmentBoundaries())
        {
            opts.disAlignment = true;
        }
        if (JitConfig.JitDisasmWithCodeBytes())
        {
            opts.disCodeBytes = true;
        }
        if (JitConfig.JitDisasmDiffable())
        {
            opts.disDiffable = true;
            opts.dspDiffable = true;
        }
    }

//-------------------------------------------------------------------------

#ifdef DEBUG
    assert(!codeGen->isGCTypeFixed());
    opts.compGcChecks = (JitConfig.JitGCChecks() != 0) || compStressCompile(STRESS_GENERIC_VARN, 5);
#endif

#if defined(DEBUG) && defined(TARGET_XARCH)
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
#if defined(TARGET_X86)
    opts.compStackCheckOnCall = (dwJitStackChecks & DWORD(STACK_CHECK_ON_CALL)) != 0;
#endif // defined(TARGET_X86)
#endif // defined(DEBUG) && defined(TARGET_XARCH)

#if MEASURE_MEM_ALLOC
    s_dspMemStats = (JitConfig.DisplayMemStats() != 0);
#endif

#ifdef PROFILING_SUPPORTED
    opts.compNoPInvokeInlineCB = jitFlags->IsSet(JitFlags::JIT_FLAG_PROF_NO_PINVOKE_INLINE);

    // Cache the profiler handle
    if (jitFlags->IsSet(JitFlags::JIT_FLAG_PROF_ENTERLEAVE))
    {
        bool hookNeeded;
        bool indirected;
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

    // Honour DOTNET_JitELTHookEnabled or STRESS_PROFILER_CALLBACKS stress mode
    // only if VM has not asked us to generate profiler hooks in the first place.
    // That is, override VM only if it hasn't asked for a profiler callback for this method.
    // Don't run this stress mode when pre-JITing, as we would need to emit a relocation
    // for the call to the fake ELT hook, which wouldn't make sense, as we can't store that
    // in the pre-JIT image.
    if (!compProfilerHookNeeded)
    {
        if ((JitConfig.JitELTHookEnabled() != 0) ||
            (!jitFlags->IsSet(JitFlags::JIT_FLAG_PREJIT) && compStressCompile(STRESS_PROFILER_CALLBACKS, 5)))
        {
            opts.compJitELTHookEnabled = true;
        }
    }

    // TBD: Exclude PInvoke stubs
    if (opts.compJitELTHookEnabled)
    {
#if defined(DEBUG) // We currently only know if we're running under SuperPMI in DEBUG
        // We don't want to get spurious SuperPMI asm diffs because profile stress kicks in and we use
        // the address of `DummyProfilerELTStub` in the JIT binary, without relocation. So just use
        // a fixed address in this case. It's SuperPMI replay, so the generated code won't be run.
        if (RunningSuperPmiReplay())
        {
#ifdef HOST_64BIT
            static_assert_no_msg(sizeof(void*) == 8);
            compProfilerMethHnd = (void*)0x0BADF00DBEADCAFE;
#else
            static_assert_no_msg(sizeof(void*) == 4);
            compProfilerMethHnd = (void*)0x0BADF00D;
#endif
        }
        else
#endif // DEBUG
        {
            compProfilerMethHnd = (void*)DummyProfilerELTStub;
        }
        compProfilerMethHndIndirected = false;
    }

#endif // PROFILING_SUPPORTED

#if FEATURE_TAILCALL_OPT
    const WCHAR* strTailCallOpt = JitConfig.TailCallOpt();
    if (strTailCallOpt != nullptr)
    {
        opts.compTailCallOpt = (UINT)_wtoi(strTailCallOpt) != 0;
    }

    if (JitConfig.TailCallLoopOpt() == 0)
    {
        opts.compTailCallLoopOpt = false;
    }
#endif

#if FEATURE_FASTTAILCALL
    if (JitConfig.FastTailCalls() == 0)
    {
        opts.compFastTailCalls = false;
    }
#endif // FEATURE_FASTTAILCALL

#ifdef CONFIGURABLE_ARM_ABI
    opts.compUseSoftFP        = jitFlags->IsSet(JitFlags::JIT_FLAG_SOFTFP_ABI);
    unsigned int softFPConfig = opts.compUseSoftFP ? 2 : 1;
    unsigned int oldSoftFPConfig =
        InterlockedCompareExchange(&GlobalJitOptions::compUseSoftFPConfigured, softFPConfig, 0);
    if (oldSoftFPConfig != softFPConfig && oldSoftFPConfig != 0)
    {
        // There are no current scenarios where the abi can change during the lifetime of a process
        // that uses the JIT. If such a change occurs, either compFeatureHfa will need to change to a TLS static
        // or we will need to have some means to reset the flag safely.
        NO_WAY("SoftFP ABI setting changed during lifetime of process");
    }

    GlobalJitOptions::compFeatureHfa = !opts.compUseSoftFP;
#elif defined(ARM_SOFTFP) && defined(TARGET_ARM)
    // Armel is unconditionally enabled in the JIT. Verify that the VM side agrees.
    assert(jitFlags->IsSet(JitFlags::JIT_FLAG_SOFTFP_ABI));
#elif defined(TARGET_ARM)
    assert(!jitFlags->IsSet(JitFlags::JIT_FLAG_SOFTFP_ABI));
#endif // CONFIGURABLE_ARM_ABI

    opts.compScopeInfo = opts.compDbgInfo;

#ifdef LATE_DISASM
    codeGen->getDisAssembler().disOpenForLateDisAsm(info.compMethodName, info.compClassName,
                                                    info.compMethodInfo->args.pSig);
#endif

    //-------------------------------------------------------------------------

    opts.compReloc = jitFlags->IsSet(JitFlags::JIT_FLAG_RELOC);

    bool enableFakeSplitting = false;

#ifdef DEBUG
    enableFakeSplitting = JitConfig.JitFakeProcedureSplitting();

#if defined(TARGET_XARCH)
    // Whether encoding of absolute addr as PC-rel offset is enabled
    opts.compEnablePCRelAddr = (JitConfig.EnablePCRelAddr() != 0);
#endif
#endif // DEBUG

    opts.compProcedureSplitting = jitFlags->IsSet(JitFlags::JIT_FLAG_PROCSPLIT) || enableFakeSplitting;

#ifdef FEATURE_CFI_SUPPORT
    // Hot/cold splitting is not being tested on NativeAOT.
    if (generateCFIUnwindCodes())
    {
        opts.compProcedureSplitting = false;
    }
#endif // FEATURE_CFI_SUPPORT

#if defined(TARGET_LOONGARCH64) || defined(TARGET_RISCV64)
    opts.compProcedureSplitting = false;
#endif // TARGET_LOONGARCH64 || TARGET_RISCV64

#ifdef DEBUG
    opts.compProcedureSplittingEH = opts.compProcedureSplitting;
#endif // DEBUG

    if (opts.compProcedureSplitting)
    {
        // Note that opts.compdbgCode is true under ngen for checked assemblies!
        opts.compProcedureSplitting = !opts.compDbgCode || enableFakeSplitting;

#ifdef DEBUG
        // JitForceProcedureSplitting is used to force procedure splitting on checked assemblies.
        // This is useful for debugging on a checked build.  Note that we still only do procedure
        // splitting in the zapper.
        if (JitConfig.JitForceProcedureSplitting().contains(info.compMethodHnd, info.compClassHnd,
                                                            &info.compMethodInfo->args))
        {
            opts.compProcedureSplitting = true;
        }

        // JitNoProcedureSplitting will always disable procedure splitting.
        if (JitConfig.JitNoProcedureSplitting().contains(info.compMethodHnd, info.compClassHnd,
                                                         &info.compMethodInfo->args))
        {
            opts.compProcedureSplitting = false;
        }
        //
        // JitNoProcedureSplittingEH will disable procedure splitting in functions with EH.
        if (JitConfig.JitNoProcedureSplittingEH().contains(info.compMethodHnd, info.compClassHnd,
                                                           &info.compMethodInfo->args))
        {
            opts.compProcedureSplittingEH = false;
        }
#endif
    }

#ifdef TARGET_64BIT
    opts.compCollect64BitCounts = JitConfig.JitCollect64BitCounts() != 0;

#ifdef DEBUG
    if (JitConfig.JitRandomlyCollect64BitCounts() != 0)
    {
        CLRRandom rng;
        rng.Init(info.compMethodHash() ^ JitConfig.JitRandomlyCollect64BitCounts() ^ 0x3485e20e);
        opts.compCollect64BitCounts = rng.Next(2) == 0;
    }
#endif
#else
    opts.compCollect64BitCounts = false;
#endif

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
            printf("OPTIONS: Tier-0 compilation (set DOTNET_TieredCompilation=0 to disable)\n");
        }
        if (jitFlags->IsSet(JitFlags::JIT_FLAG_TIER1))
        {
            printf("OPTIONS: Tier-1 compilation\n");
        }
        if (compSwitchedToOptimized)
        {
            printf("OPTIONS: Tier-0 compilation, switched to FullOpts\n");
        }
        if (compSwitchedToMinOpts)
        {
            printf("OPTIONS: Tier-1/FullOpts compilation, switched to MinOpts\n");
        }

        if (jitFlags->IsSet(JitFlags::JIT_FLAG_OSR))
        {
            printf("OPTIONS: OSR variant with entry point 0x%x\n", info.compILEntry);
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

        // This is rare; don't clutter up the dump with it normally.
        if (compProfilerHookNeeded)
        {
            printf("OPTIONS: compProfilerHookNeeded   = %s\n", dspBool(compProfilerHookNeeded));
        }

        if (jitFlags->IsSet(JitFlags::JIT_FLAG_BBOPT))
        {
            printf("OPTIONS: optimizer should use profile data\n");
        }

        if (jitFlags->IsSet(JitFlags::JIT_FLAG_PREJIT))
        {
            printf("OPTIONS: Jit invoked for ngen\n");
        }
    }
#endif

#ifdef PROFILING_SUPPORTED
#ifdef UNIX_AMD64_ABI
    if (compIsProfilerHookNeeded())
    {
        opts.compNeedToAlignFrame = true;
    }
#endif // UNIX_AMD64_ABI
#endif

#if defined(DEBUG) && defined(TARGET_ARM64)
    if ((s_pJitMethodSet == nullptr) || s_pJitMethodSet->IsActiveMethod(info.compFullName, info.compMethodHash()))
    {
        opts.compJitSaveFpLrWithCalleeSavedRegisters = JitConfig.JitSaveFpLrWithCalleeSavedRegisters();
    }
#endif // defined(DEBUG) && defined(TARGET_ARM64)

#if defined(TARGET_AMD64)
    rbmAllFloat         = RBM_ALLFLOAT_INIT;
    rbmFltCalleeTrash   = RBM_FLT_CALLEE_TRASH_INIT;
    cntCalleeTrashFloat = CNT_CALLEE_TRASH_FLOAT_INIT;

    if (canUseEvexEncoding())
    {
        rbmAllFloat |= RBM_HIGHFLOAT;
        rbmFltCalleeTrash |= RBM_HIGHFLOAT;
        cntCalleeTrashFloat += CNT_CALLEE_TRASH_HIGHFLOAT;
    }
#endif // TARGET_AMD64

#if defined(TARGET_XARCH)
    rbmAllMask         = RBM_ALLMASK_INIT;
    rbmMskCalleeTrash  = RBM_MSK_CALLEE_TRASH_INIT;
    cntCalleeTrashMask = CNT_CALLEE_TRASH_MASK_INIT;

    if (canUseEvexEncoding())
    {
        rbmAllMask |= RBM_ALLMASK_EVEX;
        rbmMskCalleeTrash |= RBM_MSK_CALLEE_TRASH_EVEX;
        cntCalleeTrashMask += CNT_CALLEE_TRASH_MASK_EVEX;
    }

    // Make sure we copy the register info and initialize the
    // trash regs after the underlying fields are initialized

    const regMaskTP vtCalleeTrashRegs[TYP_COUNT]{
#define DEF_TP(tn, nm, jitType, sz, sze, asze, st, al, regTyp, regFld, csr, ctr, tf) ctr,
#include "typelist.h"
#undef DEF_TP
    };
    memcpy(varTypeCalleeTrashRegs, vtCalleeTrashRegs, sizeof(regMaskTP) * TYP_COUNT);

    codeGen->CopyRegisterInfo();
#endif // TARGET_XARCH
}

#ifdef DEBUG

bool Compiler::compJitHaltMethod()
{
    /* This method returns true when we use an INS_BREAKPOINT to allow us to step into the generated native code */
    /* Note that this these two "Jit" environment variables also work for ngen images */

    if (JitConfig.JitHalt().contains(info.compMethodHnd, info.compClassHnd, &info.compMethodInfo->args))
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

const LPCWSTR Compiler::s_compStressModeNamesW[STRESS_COUNT + 1] = {
#define STRESS_MODE(mode) W("STRESS_") W(#mode),

    STRESS_MODES
#undef STRESS_MODE
};

const char* Compiler::s_compStressModeNames[STRESS_COUNT + 1] = {
#define STRESS_MODE(mode) "STRESS_" #mode,

    STRESS_MODES
#undef STRESS_MODE
};

//------------------------------------------------------------------------
// compStressCompile: determine if a stress mode should be enabled
//
// Arguments:
//   stressArea - stress mode to possibly enable
//   weight - percent of time this mode should be turned on
//     (range 0 to 100); weight 0 effectively disables
//
// Returns:
//   true if this stress mode is enabled
//
// Notes:
//   Methods may be excluded from stress via name or hash.
//
//   Particular stress modes may be disabled or forcibly enabled.
//
//   With JitStress=2, some stress modes are enabled regardless of weight;
//   these modes are the ones after COUNT_VARN in the enumeration.
//
//   For other modes or for nonzero JitStress values, stress will be
//   enabled selectively for roughly weight% of methods.
//
bool Compiler::compStressCompile(compStressArea stressArea, unsigned weight)
{
    // This can be called early, before info is fully set up.
    if ((info.compMethodName == nullptr) || (info.compFullName == nullptr))
    {
        return false;
    }

    // Inlinees defer to the root method for stress, so that we can
    // more easily isolate methods that cause stress failures.
    if (compIsForInlining())
    {
        return impInlineRoot()->compStressCompile(stressArea, weight);
    }

    const bool doStress = compStressCompileHelper(stressArea, weight);

    if (doStress && !compActiveStressModes[stressArea])
    {
        if (verbose)
        {
            printf("\n\n*** JitStress: %s ***\n\n", s_compStressModeNames[stressArea]);
        }
        compActiveStressModes[stressArea] = 1;
    }

    return doStress;
}

//------------------------------------------------------------------------
// compStressAreaHash: Get (or compute) a hash code for a stress area.
//
// Arguments:
//   stressArea - stress mode
//
// Returns:
//   A hash code for the specific stress area.
//
unsigned Compiler::compStressAreaHash(compStressArea area)
{
    static LONG s_hashCodes[STRESS_COUNT];
    assert(static_cast<unsigned>(area) < ArrLen(s_hashCodes));

    unsigned result = (unsigned)s_hashCodes[area];
    if (result == 0)
    {
        result = HashStringA(s_compStressModeNames[area]);
        if (result == 0)
        {
            result = 1;
        }

        InterlockedExchange(&s_hashCodes[area], (LONG)result);
    }

    return result;
}

//------------------------------------------------------------------------
// compStressCompileHelper: helper to determine if a stress mode should be enabled
//
// Arguments:
//   stressArea - stress mode to possibly enable
//   weight - percent of time this mode should be turned on
//     (range 0 to 100); weight 0 effectively disables
//
// Returns:
//   true if this stress mode is enabled
//
// Notes:
//   See compStressCompile
//
bool Compiler::compStressCompileHelper(compStressArea stressArea, unsigned weight)
{
    if (!compAllowStress)
    {
        return false;
    }

    // Does user explicitly prevent using this STRESS_MODE through the command line?
    const WCHAR* strStressModeNamesNot = JitConfig.JitStressModeNamesNot();
    if ((strStressModeNamesNot != nullptr) &&
        (u16_strstr(strStressModeNamesNot, s_compStressModeNamesW[stressArea]) != nullptr))
    {
        return false;
    }

    // Does user explicitly set this STRESS_MODE through the command line?
    const WCHAR* strStressModeNames = JitConfig.JitStressModeNames();
    if (strStressModeNames != nullptr)
    {
        if (u16_strstr(strStressModeNames, s_compStressModeNamesW[stressArea]) != nullptr)
        {
            return true;
        }

        // This stress mode name did not match anything in the stress
        // mode allowlist. If user has requested only enable mode,
        // don't allow this stress mode to turn on.
        const bool onlyEnableMode = JitConfig.JitStressModeNamesOnly() != 0;

        if (onlyEnableMode)
        {
            return false;
        }
    }

    // 0:   No stress (Except when explicitly set in DOTNET_JitStressModeNames)
    // !=2: Vary stress. Performance will be slightly/moderately degraded
    // 2:   Check-all stress. Performance will be REALLY horrible
    const int stressLevel = getJitStressLevel();

    assert(weight <= MAX_STRESS_WEIGHT);

    // Check for boundary conditions
    if (stressLevel == 0 || weight == 0)
    {
        return false;
    }

    // Should we allow unlimited stress ?
    if ((stressArea > STRESS_COUNT_VARN) && (stressLevel == 2))
    {
        return true;
    }

    if (weight == MAX_STRESS_WEIGHT)
    {
        return true;
    }

    // Get a hash which can be compared with 'weight'
    assert(stressArea != 0);
    const unsigned hash = (info.compMethodHash() ^ compStressAreaHash(stressArea) ^ stressLevel) % MAX_STRESS_WEIGHT;

    assert(hash < MAX_STRESS_WEIGHT && weight <= MAX_STRESS_WEIGHT);
    return (hash < weight);
}

//------------------------------------------------------------------------
// compPromoteFewerStructs: helper to determine if the local
//   should not be promoted under a stress mode.
//
// Arguments:
//   lclNum - local number to test
//
// Returns:
//   true if this local should not be promoted.
//
// Notes:
//   Reject ~50% of the potential promotions if STRESS_PROMOTE_FEWER_STRUCTS is active.
//
bool Compiler::compPromoteFewerStructs(unsigned lclNum)
{
    bool       rejectThisPromo = false;
    const bool promoteLess     = compStressCompile(STRESS_PROMOTE_FEWER_STRUCTS, 50);
    if (promoteLess)
    {

        rejectThisPromo = (((info.compMethodHash() ^ lclNum) & 1) == 0);
    }
    return rejectThisPromo;
}

//------------------------------------------------------------------------
// dumpRegMask: display a register mask. For well-known sets of registers, display a well-known token instead of
// a potentially large number of registers.
//
// Arguments:
//   regs - The set of registers to display
//
void Compiler::dumpRegMask(regMaskTP regs) const
{
    if (regs == RBM_ALLINT)
    {
        printf("[allInt]");
    }
    else if (regs == (RBM_ALLINT & ~RBM_FPBASE))
    {
        printf("[allIntButFP]");
    }
    else if (regs == RBM_ALLFLOAT)
    {
        printf("[allFloat]");
    }
    else if (regs == RBM_ALLDOUBLE)
    {
        printf("[allDouble]");
    }
#ifdef TARGET_XARCH
    else if (regs == RBM_ALLMASK)
    {
        printf("[allMask]");
    }
#endif // TARGET_XARCH
    else
    {
        dspRegMask(regs);
    }
}

#endif // DEBUG

void Compiler::compInitDebuggingInfo()
{
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

        fgNewStmtAtEnd(fgFirstBB, gtNewNothingNode());

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
        if (JitConfig.JitMinOptsName().contains(info.compMethodHnd, info.compClassHnd, &info.compMethodInfo->args))
        {
            theMinOptsValue = true;
        }
    }

#ifdef DEBUG
    static ConfigMethodRange s_onlyOptimizeRange;
    s_onlyOptimizeRange.EnsureInit(JitConfig.JitOnlyOptimizeRange());

    if (!theMinOptsValue && !s_onlyOptimizeRange.IsEmpty())
    {
        unsigned methHash = info.compMethodHash();
        theMinOptsValue   = !s_onlyOptimizeRange.Contains(methHash);
    }
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

    // Notify the VM if MinOpts is being used when not requested
    if (theMinOptsValue && !compIsForInlining() && !opts.jitFlags->IsSet(JitFlags::JIT_FLAG_TIER0) &&
        !opts.jitFlags->IsSet(JitFlags::JIT_FLAG_MIN_OPT) && !opts.compDbgCode)
    {
        info.compCompHnd->setMethodAttribs(info.compMethodHnd, CORINFO_FLG_SWITCHED_TO_MIN_OPT);
        opts.jitFlags->Clear(JitFlags::JIT_FLAG_TIER1);
        compSwitchedToMinOpts = true;
    }

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

        lvaEnregEHVars &= compEnregLocals();
        lvaEnregMultiRegVars &= compEnregLocals();
    }

    if (!compIsForInlining())
    {
        codeGen->setFramePointerRequired(false);
        codeGen->setFrameRequired(false);

        if (opts.OptimizationDisabled())
        {
            codeGen->setFrameRequired(true);
        }

#if !defined(TARGET_AMD64)
        // The VM sets JitFlags::JIT_FLAG_FRAMED for two reasons: (1) the DOTNET_JitFramed variable is set, or
        // (2) the function is marked "noinline". The reason for #2 is that people mark functions
        // noinline to ensure the show up on in a stack walk. But for AMD64, we don't need a frame
        // pointer for the frame to show up in stack walk.
        if (opts.jitFlags->IsSet(JitFlags::JIT_FLAG_FRAMED))
            codeGen->setFrameRequired(true);
#endif

        if (opts.OptimizationDisabled() ||
            (opts.jitFlags->IsSet(JitFlags::JIT_FLAG_PREJIT) && !IsTargetAbi(CORINFO_NATIVEAOT_ABI)))
        {
            // The JIT doesn't currently support loop alignment for prejitted images outside NativeAOT.
            // (The JIT doesn't know the final address of the code, hence
            // it can't align code based on unknown addresses.)

            codeGen->SetAlignLoops(false); // loop alignment not supported for prejitted code
        }
        else
        {
            codeGen->SetAlignLoops(JitConfig.JitAlignLoops() == 1);
        }
    }

    fgCanRelocateEHRegions = true;
}

#if defined(TARGET_ARMARCH) || defined(TARGET_RISCV64)
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

#if defined(TARGET_ARM64)

    // TODO-ARM64-CQ: update this!
    JITDUMP(" Returning true (ARM64)\n\n");
    return true; // just always assume we'll need it, for now

#elif defined(TARGET_RISCV64)
    JITDUMP(" Returning true (RISCV64)\n\n");
    return true; // just always assume we'll need it, for now

#else  // TARGET_ARM

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
#endif // TARGET_ARM
}
#endif // TARGET_ARMARCH || TARGET_RISCV64

//------------------------------------------------------------------------
// compGetTieringName: get a string describing tiered compilation settings
//   for this method
//
// Arguments:
//   wantShortName - true if a short name is ok (say for using in file names)
//
// Returns:
//   String describing tiering decisions for this method, including cases
//   where the jit codegen will differ from what the runtime requested.
//
const char* Compiler::compGetTieringName(bool wantShortName) const
{
    const bool tier0         = opts.jitFlags->IsSet(JitFlags::JIT_FLAG_TIER0);
    const bool tier1         = opts.jitFlags->IsSet(JitFlags::JIT_FLAG_TIER1);
    const bool instrumenting = opts.jitFlags->IsSet(JitFlags::JIT_FLAG_BBINSTR);

    if (!opts.compMinOptsIsSet)
    {
        // If 'compMinOptsIsSet' is not set, just return here. Otherwise, if this method is called
        // by the assertAbort(), we would recursively call assert while trying to get MinOpts()
        // and eventually stackoverflow.
        return "Optimization-Level-Not-Yet-Set";
    }

    assert(!tier0 || !tier1); // We don't expect multiple TIER flags to be set at one time.

    if (tier0)
    {
        return instrumenting ? "Instrumented Tier0" : "Tier0";
    }
    else if (tier1)
    {
        if (opts.IsOSR())
        {
            return instrumenting ? "Instrumented Tier1-OSR" : "Tier1-OSR";
        }
        else
        {
            return instrumenting ? "Instrumented Tier1" : "Tier1";
        }
    }
    else if (opts.OptimizationEnabled())
    {
        if (compSwitchedToOptimized)
        {
            return wantShortName ? "Tier0-FullOpts" : "Tier-0 switched to FullOpts";
        }
        else
        {
            return "FullOpts";
        }
    }
    else if (opts.MinOpts())
    {
        if (compSwitchedToMinOpts)
        {
            if (compSwitchedToOptimized)
            {
                return wantShortName ? "Tier0-FullOpts-MinOpts" : "Tier-0 switched to FullOpts, then to MinOpts";
            }
            else
            {
                return wantShortName ? "Tier0-MinOpts" : "Tier-0 switched MinOpts";
            }
        }
        else
        {
            return "MinOpts";
        }
    }
    else if (opts.compDbgCode)
    {
        return "Debug";
    }
    else
    {
        return wantShortName ? "Unknown" : "Unknown optimization level";
    }
}

//------------------------------------------------------------------------
// compGetPgoSourceName: get a string describing PGO source
//
// Returns:
//   String describing describing PGO source (e.g. Dynamic, Static, etc)
//
const char* Compiler::compGetPgoSourceName() const
{
    switch (fgPgoSource)
    {
        case ICorJitInfo::PgoSource::Static:
            return "Static PGO";
        case ICorJitInfo::PgoSource::Dynamic:
            return "Dynamic PGO";
        case ICorJitInfo::PgoSource::Blend:
            return "Blended PGO";
        case ICorJitInfo::PgoSource::Text:
            return "Textual PGO";
        case ICorJitInfo::PgoSource::Sampling:
            return "Sample-based PGO";
        case ICorJitInfo::PgoSource::IBC:
            return "Classic IBC";
        case ICorJitInfo::PgoSource::Synthesis:
            return "Synthesized PGO";
        default:
            return "Unknown PGO";
    }
}

//------------------------------------------------------------------------
// compGetStressMessage: get a string describing jitstress capability
//   for this method
//
// Returns:
//   An empty string if stress is not enabled, else a string describing
//   if this method is subject to stress or is excluded by name or hash.
//
const char* Compiler::compGetStressMessage() const
{
    // Add note about stress where appropriate
    const char* stressMessage = "";

#ifdef DEBUG
    // Is stress enabled via mode name or level?
    if ((JitConfig.JitStressModeNames() != nullptr) || (getJitStressLevel() > 0))
    {
        // Is the method being jitted excluded from stress via range?
        if (compAllowStress)
        {
            // Not excluded -- stress can happen
            stressMessage = " JitStress";
        }
        else
        {
            stressMessage = " NoJitStress";
        }
    }
#endif // DEBUG

    return stressMessage;
}

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
        printf("{ Start Jitting Method %4d %s (MethodHash=%08x) %s\n", Compiler::jitTotalMethodCompiled,
               info.compFullName, info.compMethodHash(),
               compGetTieringName()); /* } editor brace matching workaround for this printf */
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

        // Note: that is incorrect if we are compiling several methods at the same time.
        unsigned methodNumber = Compiler::jitTotalMethodCompiled - 1;

        /* { editor brace-matching workaround for following printf */
        printf("} Jitted Method %4d at" FMT_ADDR "method %s size %08x%s%s\n", methodNumber, DBG_ADDR(methodCodePtr),
               info.compFullName, methodCodeSize, isNYI ? " NYI" : "", opts.altJit ? " altjit" : "");
    }
#endif // DEBUG
}

//------------------------------------------------------------------------
// BeginPhase: begin execution of a phase
//
// Arguments:
//    phase - the phase that is about to begin
//
void Compiler::BeginPhase(Phases phase)
{
    mostRecentlyActivePhase = phase;
}

//------------------------------------------------------------------------
// EndPhase: finish execution of a phase
//
// Arguments:
//    phase - the phase that has just finished
//
void Compiler::EndPhase(Phases phase)
{
#if defined(FEATURE_JIT_METHOD_PERF)
    if (pCompJitTimer != nullptr)
    {
        pCompJitTimer->EndPhase(this, phase);
    }
#endif

    mostRecentlyActivePhase = phase;
}

//------------------------------------------------------------------------
// compCompile: run phases needed for compilation
//
// Arguments:
//   methodCodePtr [OUT] - address of generated code
//   methodCodeSize [OUT] - size of the generated code (hot + cold sections)
//   compileFlags [IN] - flags controlling jit behavior
//
// Notes:
//  This is the most interesting 'toplevel' function in the JIT.  It goes through the operations of
//  importing, morphing, optimizations and code generation.  This is called from the EE through the
//  code:CILJit::compileMethod function.
//
//  For an overview of the structure of the JIT, see:
//   https://github.com/dotnet/runtime/blob/main/docs/design/coreclr/jit/ryujit-overview.md
//
//  Also called for inlinees, though they will only be run through the first few phases.
//
void Compiler::compCompile(void** methodCodePtr, uint32_t* methodCodeSize, JitFlags* compileFlags)
{
    compFunctionTraceStart();

    // Enable flow graph checks
    activePhaseChecks |= PhaseChecks::CHECK_FG;

    // Prepare for importation
    //
    auto preImportPhase = [this]() {
        if (compIsForInlining())
        {
            // Notify root instance that an inline attempt is about to import IL
            impInlineRoot()->m_inlineStrategy->NoteImport();
        }

        hashBv::Init(this);

        VarSetOps::AssignAllowUninitRhs(this, compCurLife, VarSetOps::UninitVal());

        // The temp holding the secret stub argument is used by fgImport() when importing the intrinsic.
        if (info.compPublishStubParam)
        {
            assert(lvaStubArgumentVar == BAD_VAR_NUM);
            lvaStubArgumentVar                     = lvaGrabTempWithImplicitUse(false DEBUGARG("stub argument"));
            lvaGetDesc(lvaStubArgumentVar)->lvType = TYP_I_IMPL;
            // TODO-CQ: there is no need to mark it as doNotEnreg. There are no stores for this local
            // before codegen so liveness and LSRA mark it as "liveIn" and always allocate a stack slot for it.
            // However, it would be better to process it like other argument locals and keep it in
            // a reg for the whole method without spilling to the stack when possible.
            lvaSetVarDoNotEnregister(lvaStubArgumentVar DEBUGARG(DoNotEnregisterReason::VMNeedsStackAddr));
        }
    };
    DoPhase(this, PHASE_PRE_IMPORT, preImportPhase);

    // If we're going to instrument code, we may need to prepare before
    // we import. Also do this before we read in any profile data.
    //
    if (compileFlags->IsSet(JitFlags::JIT_FLAG_BBINSTR))
    {
        DoPhase(this, PHASE_IBCPREP, &Compiler::fgPrepareToInstrumentMethod);
    }

    // Incorporate profile data.
    //
    // Note: the importer is sensitive to block weights, so this has
    // to happen before importation.
    //
    activePhaseChecks |= PhaseChecks::CHECK_PROFILE;
    DoPhase(this, PHASE_INCPROFILE, &Compiler::fgIncorporateProfileData);

    // If we are doing OSR, update flow to initially reach the appropriate IL offset.
    //
    if (opts.IsOSR())
    {
        fgFixEntryFlowForOSR();
    }

    // Enable the post-phase checks that use internal logic to decide when checking makes sense.
    //
    activePhaseChecks |=
        PhaseChecks::CHECK_EH | PhaseChecks::CHECK_LOOPS | PhaseChecks::CHECK_UNIQUE | PhaseChecks::CHECK_LINKED_LOCALS;

    // Import: convert the instrs in each basic block to a tree based intermediate representation
    //
    DoPhase(this, PHASE_IMPORTATION, &Compiler::fgImport);

    // If this is a failed inline attempt, we're done.
    //
    if (compIsForInlining() && compInlineResult->IsFailure())
    {
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

    // If instrumenting, add block and class probes.
    //
    if (compileFlags->IsSet(JitFlags::JIT_FLAG_BBINSTR))
    {
        DoPhase(this, PHASE_IBCINSTR, &Compiler::fgInstrumentMethod);
    }

    // Expand any patchpoints
    //
    DoPhase(this, PHASE_PATCHPOINTS, &Compiler::fgTransformPatchpoints);

    // Transform indirect calls that require control flow expansion.
    //
    DoPhase(this, PHASE_INDXCALL, &Compiler::fgTransformIndirectCalls);

    // Cleanup un-imported BBs, cleanup un-imported or
    // partially imported try regions, add OSR step blocks.
    //
    DoPhase(this, PHASE_POST_IMPORT, &Compiler::fgPostImportationCleanup);

    // If we're importing for inlining, we're done.
    if (compIsForInlining())
    {

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

    // At this point in the phase list, all the inlinee phases have
    // been run, and inlinee compiles have exited, so we should only
    // get this far if we are jitting the root method.
    noway_assert(!compIsForInlining());

    // Prepare for the morph phases
    //
    DoPhase(this, PHASE_MORPH_INIT, &Compiler::fgMorphInit);

    // Inline callee methods into this root method
    //
    DoPhase(this, PHASE_MORPH_INLINE, &Compiler::fgInline);

    // Record "start" values for post-inlining cycles and elapsed time.
    RecordStateAtEndOfInlining();

    // Transform each GT_ALLOCOBJ node into either an allocation helper call or
    // local variable allocation on the stack.
    ObjectAllocator objectAllocator(this); // PHASE_ALLOCATE_OBJECTS

    if (compObjectStackAllocation() && opts.OptimizationEnabled())
    {
        objectAllocator.EnableObjectStackAllocation();
    }

    objectAllocator.Run();

    // Add any internal blocks/trees we may need
    //
    DoPhase(this, PHASE_MORPH_ADD_INTERNAL, &Compiler::fgAddInternal);

    // Disable profile checks now.
    // Over time we will move this further and further back in the phase list, as we fix issues.
    //
    activePhaseChecks &= ~PhaseChecks::CHECK_PROFILE;

    // Remove empty try regions
    //
    DoPhase(this, PHASE_EMPTY_TRY, &Compiler::fgRemoveEmptyTry);

    // Remove empty finally regions
    //
    DoPhase(this, PHASE_EMPTY_FINALLY, &Compiler::fgRemoveEmptyFinally);

    // Streamline chains of finally invocations
    //
    DoPhase(this, PHASE_MERGE_FINALLY_CHAINS, &Compiler::fgMergeFinallyChains);

    // Clone code in finallys to reduce overhead for non-exceptional paths
    //
    DoPhase(this, PHASE_CLONE_FINALLY, &Compiler::fgCloneFinally);

#if DEBUG
    if (lvaEnregEHVars)
    {
        unsigned methHash   = info.compMethodHash();
        char*    lostr      = getenv("JitEHWTHashLo");
        unsigned methHashLo = 0;
        bool     dump       = false;
        if (lostr != nullptr)
        {
            sscanf_s(lostr, "%x", &methHashLo);
            dump = true;
        }
        char*    histr      = getenv("JitEHWTHashHi");
        unsigned methHashHi = UINT32_MAX;
        if (histr != nullptr)
        {
            sscanf_s(histr, "%x", &methHashHi);
            dump = true;
        }
        if (methHash < methHashLo || methHash > methHashHi)
        {
            lvaEnregEHVars = false;
        }
        else if (dump)
        {
            printf("Enregistering EH Vars for method %s, hash = 0x%x.\n", info.compFullName, info.compMethodHash());
            printf(""); // flush
        }
    }
    if (lvaEnregMultiRegVars)
    {
        unsigned methHash   = info.compMethodHash();
        char*    lostr      = getenv("JitMultiRegHashLo");
        unsigned methHashLo = 0;
        bool     dump       = false;
        if (lostr != nullptr)
        {
            sscanf_s(lostr, "%x", &methHashLo);
            dump = true;
        }
        char*    histr      = getenv("JitMultiRegHashHi");
        unsigned methHashHi = UINT32_MAX;
        if (histr != nullptr)
        {
            sscanf_s(histr, "%x", &methHashHi);
            dump = true;
        }
        if (methHash < methHashLo || methHash > methHashHi)
        {
            lvaEnregMultiRegVars = false;
        }
        else if (dump)
        {
            printf("Enregistering MultiReg Vars for method %s, hash = 0x%x.\n", info.compFullName,
                   info.compMethodHash());
            printf(""); // flush
        }
    }
#endif

    // Do some flow-related optimizations
    //
    if (opts.OptimizationEnabled())
    {
        // Tail merge
        //
        DoPhase(this, PHASE_HEAD_TAIL_MERGE, [this]() { return fgHeadTailMerge(true); });

        // Merge common throw blocks
        //
        DoPhase(this, PHASE_MERGE_THROWS, &Compiler::fgTailMergeThrows);

        // Run an early flow graph simplification pass
        //
        DoPhase(this, PHASE_EARLY_UPDATE_FLOW_GRAPH, &Compiler::fgUpdateFlowGraphPhase);
    }

    // Promote struct locals
    //
    DoPhase(this, PHASE_PROMOTE_STRUCTS, &Compiler::fgPromoteStructs);

    // Enable early ref counting of locals
    //
    lvaRefCountState = RCS_EARLY;

    if (opts.OptimizationEnabled())
    {
        fgNodeThreading = NodeThreading::AllLocals;
    }

    // Figure out what locals are address-taken.
    //
    DoPhase(this, PHASE_STR_ADRLCL, &Compiler::fgMarkAddressExposedLocals);

    // Do an early pass of liveness for forward sub and morph. This data is
    // valid until after morph.
    //
    DoPhase(this, PHASE_EARLY_LIVENESS, &Compiler::fgEarlyLiveness);

    // Run a simple forward substitution pass.
    //
    DoPhase(this, PHASE_FWD_SUB, &Compiler::fgForwardSub);

    // Promote struct locals based on primitive access patterns
    //
    DoPhase(this, PHASE_PHYSICAL_PROMOTION, &Compiler::PhysicalPromotion);

    // Expose candidates for implicit byref last-use copy elision.
    DoPhase(this, PHASE_IMPBYREF_COPY_OMISSION, &Compiler::fgMarkImplicitByRefCopyOmissionCandidates);

    // Locals tree list is no longer kept valid.
    fgNodeThreading = NodeThreading::None;

    // Apply the type update to implicit byref parameters; also choose (based on address-exposed
    // analysis) which implicit byref promotions to keep (requires copy to initialize) or discard.
    //
    DoPhase(this, PHASE_MORPH_IMPBYREF, &Compiler::fgRetypeImplicitByRefArgs);

#ifdef DEBUG
    // Now that locals have address-taken and implicit byref marked, we can safely apply stress.
    lvaStressLclFld();
    fgStress64RsltMul();
#endif // DEBUG

    if (opts.OptimizationEnabled())
    {
        // Build post-order that morph will use, and remove dead blocks
        //
        DoPhase(this, PHASE_DFS_BLOCKS, &Compiler::fgDfsBlocksAndRemove);
    }

    // Morph the trees in all the blocks of the method
    //
    unsigned const preMorphBBCount = fgBBcount;
    DoPhase(this, PHASE_MORPH_GLOBAL, &Compiler::fgMorphBlocks);

    auto postMorphPhase = [this]() {

        // Fix any LclVar annotations on discarded struct promotion temps for implicit by-ref args
        fgMarkDemotedImplicitByRefArgs();
        lvaRefCountState       = RCS_INVALID;
        fgLocalVarLivenessDone = false;

        // Decide the kind of code we want to generate
        fgSetOptions();

        fgExpandQmarkNodes();

#ifdef DEBUG
        compCurBB = nullptr;
#endif // DEBUG

        // Enable IR checks
        activePhaseChecks |= PhaseChecks::CHECK_IR;
    };
    DoPhase(this, PHASE_POST_MORPH, postMorphPhase);

    // If we needed to create any new BasicBlocks then renumber the blocks
    //
    if (fgBBcount > preMorphBBCount)
    {
        fgRenumberBlocks();
    }

    // GS security checks for unsafe buffers
    //
    DoPhase(this, PHASE_GS_COOKIE, &Compiler::gsPhase);

    // Compute the block and edge weights
    //
    DoPhase(this, PHASE_COMPUTE_EDGE_WEIGHTS, &Compiler::fgComputeBlockAndEdgeWeights);

#if defined(FEATURE_EH_FUNCLETS)

    // Create funclets from the EH handlers.
    //
    DoPhase(this, PHASE_CREATE_FUNCLETS, &Compiler::fgCreateFunclets);

#endif // FEATURE_EH_FUNCLETS

    if (opts.OptimizationEnabled())
    {
        // Invert loops
        //
        DoPhase(this, PHASE_INVERT_LOOPS, &Compiler::optInvertLoops);

        // Run some flow graph optimizations (but don't reorder)
        //
        DoPhase(this, PHASE_OPTIMIZE_FLOW, &Compiler::optOptimizeFlow);

        // Second pass of tail merge
        //
        DoPhase(this, PHASE_HEAD_TAIL_MERGE2, [this]() { return fgHeadTailMerge(false); });

        // Canonicalize entry to give a unique dominator tree root
        //
        DoPhase(this, PHASE_CANONICALIZE_ENTRY, &Compiler::fgCanonicalizeFirstBB);

        // Compute reachability sets and dominators.
        //
        DoPhase(this, PHASE_COMPUTE_REACHABILITY, &Compiler::fgComputeReachability);

        // Scale block weights and mark run rarely blocks.
        //
        DoPhase(this, PHASE_SET_BLOCK_WEIGHTS, &Compiler::optSetBlockWeights);

        // Discover and classify natural loops (e.g. mark iterative loops as such). Also marks loop blocks
        // and sets bbWeight to the loop nesting levels.
        //
        DoPhase(this, PHASE_FIND_LOOPS, &Compiler::optFindLoopsPhase);

        // Clone loops with optimization opportunities, and choose one based on dynamic condition evaluation.
        //
        DoPhase(this, PHASE_CLONE_LOOPS, &Compiler::optCloneLoops);

        // Unroll loops
        //
        DoPhase(this, PHASE_UNROLL_LOOPS, &Compiler::optUnrollLoops);

        // Compute dominators and exceptional entry blocks
        //
        DoPhase(this, PHASE_COMPUTE_DOMINATORS, &Compiler::fgComputeDominators);
    }

#ifdef DEBUG
    fgDebugCheckLinks();
#endif

    // Morph multi-dimensional array operations.
    // (Consider deferring all array operation morphing, including single-dimensional array ops,
    // from global morph to here, so cloning doesn't have to deal with morphed forms.)
    //
    DoPhase(this, PHASE_MORPH_MDARR, &Compiler::fgMorphArrayOps);

    // Create the variable table (and compute variable ref counts)
    //
    DoPhase(this, PHASE_MARK_LOCAL_VARS, &Compiler::lvaMarkLocalVars);

    // IMPORTANT, after this point, locals are ref counted.
    // However, ref counts are not kept incrementally up to date.
    assert(lvaLocalVarRefCounted());

    // Figure out the order in which operators are to be evaluated
    //
    DoPhase(this, PHASE_FIND_OPER_ORDER, &Compiler::fgFindOperOrder);

    // Weave the tree lists. Anyone who modifies the tree shapes after
    // this point is responsible for calling fgSetStmtSeq() to keep the
    // nodes properly linked.
    //
    DoPhase(this, PHASE_SET_BLOCK_ORDER, &Compiler::fgSetBlockOrder);

    fgNodeThreading = NodeThreading::AllTrees;

    // At this point we know if we are fully interruptible or not
    if (opts.OptimizationEnabled())
    {
        bool doSsa                     = true;
        bool doEarlyProp               = true;
        bool doValueNum                = true;
        bool doLoopHoisting            = true;
        bool doCopyProp                = true;
        bool doBranchOpt               = true;
        bool doCse                     = true;
        bool doAssertionProp           = true;
        bool doVNBasedIntrinExpansion  = true;
        bool doRangeAnalysis           = true;
        bool doVNBasedDeadStoreRemoval = true;
        int  iterations                = 1;

#if defined(OPT_CONFIG)
        doSsa                     = (JitConfig.JitDoSsa() != 0);
        doEarlyProp               = doSsa && (JitConfig.JitDoEarlyProp() != 0);
        doValueNum                = doSsa && (JitConfig.JitDoValueNumber() != 0);
        doLoopHoisting            = doValueNum && (JitConfig.JitDoLoopHoisting() != 0);
        doCopyProp                = doValueNum && (JitConfig.JitDoCopyProp() != 0);
        doBranchOpt               = doValueNum && (JitConfig.JitDoRedundantBranchOpts() != 0);
        doCse                     = doValueNum;
        doAssertionProp           = doValueNum && (JitConfig.JitDoAssertionProp() != 0);
        doVNBasedIntrinExpansion  = doValueNum;
        doRangeAnalysis           = doAssertionProp && (JitConfig.JitDoRangeAnalysis() != 0);
        doVNBasedDeadStoreRemoval = doValueNum && (JitConfig.JitDoVNBasedDeadStoreRemoval() != 0);

        if (opts.optRepeat)
        {
            iterations = JitConfig.JitOptRepeatCount();
        }
#endif // defined(OPT_CONFIG)

        while (iterations > 0)
        {
            fgModified = false;

            if (doSsa)
            {
                // Build up SSA form for the IR
                //
                DoPhase(this, PHASE_BUILD_SSA, &Compiler::fgSsaBuild);
            }
            else
            {
                // At least do local var liveness; lowering depends on this.
                fgLocalVarLiveness();
            }

            if (doEarlyProp)
            {
                // Propagate array length and rewrite getType() method call
                //
                DoPhase(this, PHASE_EARLY_PROP, &Compiler::optEarlyProp);
            }

            if (doValueNum)
            {
                // Value number the trees
                //
                DoPhase(this, PHASE_VALUE_NUMBER, &Compiler::fgValueNumber);
            }

            if (doLoopHoisting)
            {
                // Hoist invariant code out of loops
                //
                DoPhase(this, PHASE_HOIST_LOOP_CODE, &Compiler::optHoistLoopCode);
            }

            if (doCopyProp)
            {
                // Perform VN based copy propagation
                //
                DoPhase(this, PHASE_VN_COPY_PROP, &Compiler::optVnCopyProp);
            }

            if (doBranchOpt)
            {
                // Optimize redundant branches
                //
                DoPhase(this, PHASE_OPTIMIZE_BRANCHES, &Compiler::optRedundantBranches);
            }
            else
            {
                // DFS tree is always invalid after this point.
                //
                fgInvalidateDfsTree();
            }

            if (doCse)
            {
                // Remove common sub-expressions
                //
                DoPhase(this, PHASE_OPTIMIZE_VALNUM_CSES, &Compiler::optOptimizeCSEs);
            }

            if (doAssertionProp)
            {
                // Assertion propagation
                //
                DoPhase(this, PHASE_ASSERTION_PROP_MAIN, &Compiler::optAssertionPropMain);
            }

            if (doVNBasedIntrinExpansion)
            {
                // Expand some intrinsics based on VN data
                //
                DoPhase(this, PHASE_VN_BASED_INTRINSIC_EXPAND, &Compiler::fgVNBasedIntrinsicExpansion);
            }

            if (doRangeAnalysis)
            {
                // Bounds check elimination via range analysis
                //
                DoPhase(this, PHASE_OPTIMIZE_INDEX_CHECKS, &Compiler::rangeCheckPhase);
            }

            if (doVNBasedDeadStoreRemoval)
            {
                // Note: this invalidates SSA and value numbers on tree nodes.
                //
                DoPhase(this, PHASE_VN_BASED_DEAD_STORE_REMOVAL, &Compiler::optVNBasedDeadStoreRemoval);
            }

            if (fgModified)
            {
                // update the flowgraph if we modified it during the optimization phase
                //
                DoPhase(this, PHASE_OPT_UPDATE_FLOW_GRAPH, &Compiler::fgUpdateFlowGraphPhase);

                // Recompute the edge weight if we have modified the flow graph
                //
                DoPhase(this, PHASE_COMPUTE_EDGE_WEIGHTS2, &Compiler::fgComputeEdgeWeights);
            }

            // Iterate if requested, resetting annotations first.
            if (--iterations == 0)
            {
                break;
            }

            // We may have optimized away the canonical entry BB that SSA
            // depends on above, so if we are going for another iteration then
            // make sure we still have a canonical entry.
            //
            DoPhase(this, PHASE_CANONICALIZE_ENTRY, &Compiler::fgCanonicalizeFirstBB);

            ResetOptAnnotations();
            RecomputeFlowGraphAnnotations();
        }
    }

    optLoopsCanonical = false;

#ifdef DEBUG
    DoPhase(this, PHASE_STRESS_SPLIT_TREE, &Compiler::StressSplitTree);
#endif

    // Expand runtime lookups (an optimization but we'd better run it in tier0 too)
    DoPhase(this, PHASE_EXPAND_RTLOOKUPS, &Compiler::fgExpandRuntimeLookups);

    // Partially inline static initializations
    DoPhase(this, PHASE_EXPAND_STATIC_INIT, &Compiler::fgExpandStaticInit);

    // Expand thread local access
    DoPhase(this, PHASE_EXPAND_TLS, &Compiler::fgExpandThreadLocalAccess);

    // Expand casts
    DoPhase(this, PHASE_EXPAND_CASTS, &Compiler::fgLateCastExpansion);

    // Insert GC Polls
    DoPhase(this, PHASE_INSERT_GC_POLLS, &Compiler::fgInsertGCPolls);

    // Create any throw helper blocks that might be needed
    //
    DoPhase(this, PHASE_CREATE_THROW_HELPERS, &Compiler::fgCreateThrowHelperBlocks);

    if (opts.OptimizationEnabled())
    {
        // Optimize boolean conditions
        //
        DoPhase(this, PHASE_OPTIMIZE_BOOLS, &Compiler::optOptimizeBools);

        // If conversion
        //
        DoPhase(this, PHASE_IF_CONVERSION, &Compiler::optIfConversion);

        // Optimize block order
        //
        DoPhase(this, PHASE_OPTIMIZE_LAYOUT, &Compiler::optOptimizeLayout);

        // Conditional to Switch conversion
        //
        DoPhase(this, PHASE_SWITCH_RECOGNITION, &Compiler::optSwitchRecognition);
    }

    // Determine start of cold region if we are hot/cold splitting
    //
    DoPhase(this, PHASE_DETERMINE_FIRST_COLD_BLOCK, &Compiler::fgDetermineFirstColdBlock);

#ifdef DEBUG
    // Stash the current estimate of the function's size if necessary.
    if (verbose)
    {
        compSizeEstimate  = 0;
        compCycleEstimate = 0;
        for (BasicBlock* const block : Blocks())
        {
            for (Statement* const stmt : block->Statements())
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

    fgNodeThreading = NodeThreading::LIR;

    // Enable this to gather statistical data such as
    // call and register argument info, flowgraph and loop info, etc.
    compJitStats();

#ifdef TARGET_ARM
    if (compLocallocUsed)
    {
        // We reserve REG_SAVED_LOCALLOC_SP to store SP on entry for stack unwinding
        codeGen->regSet.rsMaskResvd |= RBM_SAVED_LOCALLOC_SP;
    }
#endif // TARGET_ARM

    // Assign registers to variables, etc.

    // Create LinearScan before Lowering, so that Lowering can call LinearScan methods
    // for determining whether locals are register candidates and (for xarch) whether
    // a node is a containable memory op.
    m_pLinearScan = getLinearScanAllocator(this);

    // Lower
    //
    m_pLowering = new (this, CMK_LSRA) Lowering(this, m_pLinearScan); // PHASE_LOWERING
    m_pLowering->Run();

    // Set stack levels and analyze throw helper usage.
    StackLevelSetter stackLevelSetter(this);
    stackLevelSetter.Run();

    // We can not add any new tracked variables after this point.
    lvaTrackedFixed = true;

    // Now that lowering is completed we can proceed to perform register allocation
    //
    auto linearScanPhase = [this]() { m_pLinearScan->doLinearScan(); };
    DoPhase(this, PHASE_LINEAR_SCAN, linearScanPhase);

    // Copied from rpPredictRegUse()
    SetFullPtrRegMapRequired(codeGen->GetInterruptible() || !codeGen->isFramePointerUsed());

#if FEATURE_LOOP_ALIGN
    // Place loop alignment instructions
    DoPhase(this, PHASE_ALIGN_LOOPS, &Compiler::placeLoopAlignInstructions);
#endif

    // The common phase checks and dumps are no longer relevant past this point.
    //
    activePhaseChecks = PhaseChecks::CHECK_NONE;
    activePhaseDumps  = PhaseDumps::DUMP_NONE;

    // Generate code
    codeGen->genGenerateCode(methodCodePtr, methodCodeSize);

#if TRACK_LSRA_STATS
    if (JitConfig.DisplayLsraStats() == 2)
    {
        m_pLinearScan->dumpLsraStatsCsv(jitstdout());
    }
#endif // TRACK_LSRA_STATS

    // We're done -- set the active phase to the last phase
    // (which isn't really a phase)
    mostRecentlyActivePhase = PHASE_POST_EMIT;

#ifdef FEATURE_JIT_METHOD_PERF
    if (pCompJitTimer)
    {
#if MEASURE_CLRAPI_CALLS
        EndPhase(PHASE_CLR_API);
#else
        EndPhase(PHASE_POST_EMIT);
#endif
        pCompJitTimer->Terminate(this, CompTimeSummaryInfo::s_compTimeSummary, true);
    }
#endif

    // Generate PatchpointInfo
    generatePatchpointInfo();

    RecordStateAtEndOfCompilation();

    unsigned methodsCompiled = (unsigned)InterlockedIncrement((LONG*)&Compiler::jitTotalMethodCompiled);

    if (JitConfig.JitDisasmSummary() && !compIsForInlining())
    {
        char osrBuffer[20] = {0};
        if (opts.IsOSR())
        {
            // Tiering name already includes "OSR", we just want the IL offset
            sprintf_s(osrBuffer, 20, " @0x%x", info.compILEntry);
        }

#ifdef DEBUG
        const char* fullName = info.compFullName;
#else
        const char* fullName =
            eeGetMethodFullName(info.compMethodHnd, /* includeReturnType */ false, /* includeThisSpecifier */ false);
#endif

        char debugPart[128] = {0};
        INDEBUG(sprintf_s(debugPart, 128, ", hash=0x%08x%s", info.compMethodHash(), compGetStressMessage()));

        char metricPart[128] = {0};
#ifdef DEBUG
        if (JitConfig.JitMetrics() > 0)
        {
            sprintf_s(metricPart, 128, ", perfScore=%.2f, numCse=%u", info.compPerfScore, optCSEcount);
        }
#endif

        const bool hasProf = fgHaveProfileData();
        printf("%4d: JIT compiled %s [%s%s%s%s, IL size=%u, code size=%u%s%s]\n", methodsCompiled, fullName,
               compGetTieringName(), osrBuffer, hasProf ? " with " : "", hasProf ? compGetPgoSourceName() : "",
               info.compILCodeSize, *methodCodeSize, debugPart, metricPart);
    }

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

#if FEATURE_LOOP_ALIGN

//------------------------------------------------------------------------
// shouldAlignLoop: Check if it is legal and profitable to align a loop.
//
// Parameters:
//   loop - The loop
//   top  - First block of the loop that appears in lexical order
//
// Returns:
//    True if it is legal and profitable to align this loop.
//
// Remarks:
//   All innermost loops whose block weight meets a threshold are candidates for alignment.
//   The top block of the loop is marked with the BBF_LOOP_ALIGN flag to indicate this.
//
//   Depends on block weights being set.
//
bool Compiler::shouldAlignLoop(FlowGraphNaturalLoop* loop, BasicBlock* top)
{
    if (loop->GetChild() != nullptr)
    {
        JITDUMP("Skipping alignment for " FMT_LP "; not an innermost loop\n", loop->GetIndex());
        return false;
    }

    if (top == fgFirstBB)
    {
        // Adding align instruction in prolog is not supported.
        // TODO: Insert an empty block before the loop, if want to align it, so we have a place to put
        // the align instruction.
        JITDUMP("Skipping alignment for " FMT_LP "; loop starts in first block\n", loop->GetIndex());
        return false;
    }

    if (top->HasFlag(BBF_COLD))
    {
        JITDUMP("Skipping alignment for cold loop " FMT_LP "\n", loop->GetIndex());
        return false;
    }

    bool hasCall = loop->VisitLoopBlocks([](BasicBlock* block) {
        for (GenTree* tree : LIR::AsRange(block))
        {
            if (tree->IsCall())
            {
                return BasicBlockVisit::Abort;
            }
        }

        return BasicBlockVisit::Continue;
    }) == BasicBlockVisit::Abort;

    if (hasCall)
    {
        // Heuristic: it is not valuable to align loops with calls.
        JITDUMP("Skipping alignment for " FMT_LP "; loop contains call\n", loop->GetIndex());
        return false;
    }

    assert(!top->IsFirst());

#if FEATURE_EH_CALLFINALLY_THUNKS
    if (top->Prev()->KindIs(BBJ_CALLFINALLY))
    {
        // It must be a retless BBJ_CALLFINALLY if we get here.
        assert(!top->Prev()->isBBCallFinallyPair());

        // If the block before the loop start is a retless BBJ_CALLFINALLY
        // with FEATURE_EH_CALLFINALLY_THUNKS, we can't add alignment
        // because it will affect reported EH region range. For x86 (where
        // !FEATURE_EH_CALLFINALLY_THUNKS), we can allow this.

        JITDUMP("Skipping alignment for " FMT_LP "; its top block follows a CALLFINALLY block\n", loop->GetIndex());
        return false;
    }
#endif // FEATURE_EH_CALLFINALLY_THUNKS

    if (top->Prev()->isBBCallFinallyPairTail())
    {
        // If the previous block is the BBJ_CALLFINALLYRET of a
        // BBJ_CALLFINALLY/BBJ_CALLFINALLYRET pair, then we can't add alignment
        // because we can't add instructions in that block. In the
        // FEATURE_EH_CALLFINALLY_THUNKS case, it would affect the
        // reported EH, as above.
        JITDUMP("Skipping alignment for " FMT_LP "; its top block follows a CALLFINALLY/ALWAYS pair\n",
                loop->GetIndex());
        return false;
    }

    // Now we have an innerloop candidate that might need alignment

    weight_t topWeight     = top->getBBWeight(this);
    weight_t compareWeight = opts.compJitAlignLoopMinBlockWeight * BB_UNITY_WEIGHT;
    if (topWeight < compareWeight)
    {
        JITDUMP("Skipping alignment for " FMT_LP " that starts at " FMT_BB ", weight=" FMT_WT " < " FMT_WT ".\n",
                loop->GetIndex(), top->bbNum, topWeight, compareWeight);
        return false;
    }

    JITDUMP("Aligning " FMT_LP " that starts at " FMT_BB ", weight=" FMT_WT " >= " FMT_WT ".\n", loop->GetIndex(),
            top->bbNum, topWeight, compareWeight);
    return true;
}

//------------------------------------------------------------------------
// placeLoopAlignInstructions: determine where to place alignment padding
//
// Returns:
//    Suitable phase status
//
// Notes:
//    Iterate over all the blocks and determine
//    the best position to place the 'align' instruction. Inserting 'align'
//    instructions after an unconditional branch is preferred over inserting
//    in the block before the loop. In case there are multiple blocks
//    having 'jmp', the one that has lower weight is preferred.
//    If the block having 'jmp' is hotter than the block before the loop,
//    the align will still be placed after 'jmp' because the processor should
//    be smart enough to not fetch extra instruction beyond jmp.
//
PhaseStatus Compiler::placeLoopAlignInstructions()
{
    JITDUMP("*************** In placeLoopAlignInstructions()\n");

    if (!codeGen->ShouldAlignLoops())
    {
        JITDUMP("Not aligning loops; ShouldAlignLoops is false\n");
        return PhaseStatus::MODIFIED_NOTHING;
    }

    if (!fgMightHaveNaturalLoops)
    {
#ifdef DEBUG
        // If false, then we expect there to be no natural loops. Phases
        // between loop finding and loop alignment that may introduce loops
        // should conservatively set fgMightHaveNaturalLoops.
        FlowGraphDfsTree*      dfsTree = fgComputeDfs();
        FlowGraphNaturalLoops* loops   = FlowGraphNaturalLoops::Find(dfsTree);
        assert(loops->NumLoops() == 0);
#endif

        JITDUMP("Not checking for any loops as fgMightHaveNaturalLoops is false\n");
        return PhaseStatus::MODIFIED_NOTHING;
    }

    FlowGraphDfsTree*      dfsTree = fgComputeDfs();
    FlowGraphNaturalLoops* loops   = FlowGraphNaturalLoops::Find(dfsTree);

    // fgMightHaveNaturalLoops being true does not necessarily mean there's still any more loops left.
    if (loops->NumLoops() == 0)
    {
        JITDUMP("No natural loops found\n");
        return PhaseStatus::MODIFIED_NOTHING;
    }

    BlockToNaturalLoopMap* blockToLoop = BlockToNaturalLoopMap::Build(loops);

    BitVecTraits loopTraits((unsigned)loops->NumLoops(), this);
    BitVec       seenLoops(BitVecOps::MakeEmpty(&loopTraits));
    BitVec       alignedLoops(BitVecOps::MakeEmpty(&loopTraits));

    bool        madeChanges   = false;
    weight_t    minBlockSoFar = BB_MAX_WEIGHT;
    BasicBlock* bbHavingAlign = nullptr;

    for (BasicBlock* const block : Blocks())
    {
        FlowGraphNaturalLoop* loop = blockToLoop->GetLoop(block);

        // If this is the first block of a loop then see if we should align it
        if ((loop != nullptr) && BitVecOps::TryAddElemD(&loopTraits, seenLoops, loop->GetIndex()) &&
            shouldAlignLoop(loop, block))
        {
            block->SetFlags(BBF_LOOP_ALIGN);
            BitVecOps::AddElemD(&loopTraits, alignedLoops, loop->GetIndex());
            INDEBUG(loopAlignCandidates++);

            BasicBlock* prev = block->Prev();
            // shouldAlignLoop should have guaranteed these properties.
            assert((prev != nullptr) && !prev->HasFlag(BBF_COLD));

            if (bbHavingAlign == nullptr)
            {
                // If jmp was not found, then block before the loop start is where align instruction will be added.

                bbHavingAlign = prev;
                JITDUMP("Marking " FMT_BB " before the loop with BBF_HAS_ALIGN for loop at " FMT_BB "\n", prev->bbNum,
                        block->bbNum);
            }
            else
            {
                JITDUMP("Marking " FMT_BB " that ends with unconditional jump with BBF_HAS_ALIGN for loop at " FMT_BB
                        "\n",
                        bbHavingAlign->bbNum, block->bbNum);
            }

            madeChanges = true;
            bbHavingAlign->SetFlags(BBF_HAS_ALIGN);

            minBlockSoFar = BB_MAX_WEIGHT;
            bbHavingAlign = nullptr;

            continue;
        }

        // Otherwise check if this is a candidate block for placing alignment for a future loop.
        // If there is an unconditional jump that won't be removed
        if (opts.compJitHideAlignBehindJmp && block->KindIs(BBJ_ALWAYS) && !block->CanRemoveJumpToNext(this))
        {
            // Track the lower weight blocks
            if (block->bbWeight < minBlockSoFar)
            {
                if ((loop == nullptr) || !BitVecOps::IsMember(&loopTraits, alignedLoops, loop->GetIndex()))
                {
                    // Ok to insert align instruction in this block because it is not part of any aligned loop.
                    minBlockSoFar = block->bbWeight;
                    bbHavingAlign = block;
                    JITDUMP(FMT_BB ", bbWeight=" FMT_WT " ends with unconditional 'jmp' \n", block->bbNum,
                            block->bbWeight);
                }
            }
        }
    }

    JITDUMP("Found %u candidates for loop alignment\n", loopAlignCandidates);

    return madeChanges ? PhaseStatus::MODIFIED_EVERYTHING : PhaseStatus::MODIFIED_NOTHING;
}

#endif

//------------------------------------------------------------------------
// StressSplitTree: A phase that stresses the gtSplitTree function.
//
// Returns:
//    Suitable phase status
//
// Notes:
//   Stress is applied on a function-by-function basis
//
PhaseStatus Compiler::StressSplitTree()
{
    if (compStressCompile(STRESS_SPLIT_TREES_RANDOMLY, 10))
    {
        SplitTreesRandomly();
        return PhaseStatus::MODIFIED_EVERYTHING;
    }

    if (compStressCompile(STRESS_SPLIT_TREES_REMOVE_COMMAS, 10))
    {
        SplitTreesRemoveCommas();
        return PhaseStatus::MODIFIED_EVERYTHING;
    }

    return PhaseStatus::MODIFIED_NOTHING;
}

//------------------------------------------------------------------------
// SplitTreesRandomly: Split all statements at a random location.
//
void Compiler::SplitTreesRandomly()
{
#ifdef DEBUG
    CLRRandom rng;
    rng.Init(info.compMethodHash() ^ 0x077cc4d4);

    // Splitting creates a lot of new locals. Set a limit on how many we end up creating here.
    unsigned maxLvaCount = max(lvaCount * 2, 50000);

    for (BasicBlock* block : Blocks())
    {
        for (Statement* stmt : block->NonPhiStatements())
        {
            int numTrees = 0;
            for (GenTree* tree : stmt->TreeList())
            {
                if (tree->OperIs(GT_JTRUE)) // Due to relop invariant
                {
                    continue;
                }

                numTrees++;
            }

            int splitTree = rng.Next(numTrees);
            for (GenTree* tree : stmt->TreeList())
            {
                if (tree->OperIs(GT_JTRUE))
                    continue;

                if (splitTree == 0)
                {
                    JITDUMP("Splitting " FMT_STMT " at [%06u]\n", stmt->GetID(), dspTreeID(tree));
                    Statement* newStmt;
                    GenTree**  use;
                    if (gtSplitTree(block, stmt, tree, &newStmt, &use))
                    {
                        while ((newStmt != nullptr) && (newStmt != stmt))
                        {
                            fgMorphStmtBlockOps(block, newStmt);
                            newStmt = newStmt->GetNextStmt();
                        }

                        fgMorphStmtBlockOps(block, stmt);
                        gtUpdateStmtSideEffects(stmt);
                    }

                    break;
                }

                splitTree--;
            }

            if (lvaCount > maxLvaCount)
            {
                JITDUMP("Created too many locals (at %u) -- stopping\n", lvaCount);
                return;
            }
        }
    }
#endif
}

//------------------------------------------------------------------------
// SplitTreesRemoveCommas: Split trees to remove all commas.
//
void Compiler::SplitTreesRemoveCommas()
{
    // Splitting creates a lot of new locals. Set a limit on how many we end up creating here.
    unsigned maxLvaCount = max(lvaCount * 2, 50000);

    for (BasicBlock* block : Blocks())
    {
        Statement* stmt = block->FirstNonPhiDef();
        while (stmt != nullptr)
        {
            Statement* nextStmt = stmt->GetNextStmt();
            for (GenTree* tree : stmt->TreeList())
            {
                if (!tree->OperIs(GT_COMMA))
                {
                    continue;
                }

                // Supporting this weird construct would require additional
                // handling, we need to sort of move the comma into to the
                // next node in execution order. We don't see this so just
                // skip it.
                assert(!tree->IsReverseOp());

                JITDUMP("Removing COMMA [%06u]\n", dspTreeID(tree));
                Statement* newStmt;
                GenTree**  use;
                gtSplitTree(block, stmt, tree, &newStmt, &use);
                GenTree* op1SideEffects = nullptr;
                gtExtractSideEffList(tree->gtGetOp1(), &op1SideEffects);

                if (op1SideEffects != nullptr)
                {
                    Statement* op1Stmt = fgNewStmtFromTree(op1SideEffects);
                    fgInsertStmtBefore(block, stmt, op1Stmt);
                    if (newStmt == nullptr)
                    {
                        newStmt = op1Stmt;
                    }
                }

                *use = tree->gtGetOp2();

                for (Statement* cur = newStmt; (cur != nullptr) && (cur != stmt); cur = cur->GetNextStmt())
                {
                    fgMorphStmtBlockOps(block, cur);
                }

                fgMorphStmtBlockOps(block, stmt);
                gtUpdateStmtSideEffects(stmt);

                if (lvaCount > maxLvaCount)
                {
                    JITDUMP("Created too many locals (at %u) -- stopping\n", lvaCount);
                    return;
                }

                // Morphing block ops can introduce commas (and the original
                // statement can also have more commas left). Proceed from the
                // earliest newly introduced statement.
                nextStmt = newStmt != nullptr ? newStmt : stmt;
                break;
            }

            stmt = nextStmt;
        }
    }

    for (BasicBlock* block : Blocks())
    {
        for (Statement* stmt : block->NonPhiStatements())
        {
            for (GenTree* tree : stmt->TreeList())
            {
                assert(!tree->OperIs(GT_COMMA));
            }
        }
    }
}

//------------------------------------------------------------------------
// generatePatchpointInfo: allocate and fill in patchpoint info data,
//    and report it to the VM
//
void Compiler::generatePatchpointInfo()
{
    if (!doesMethodHavePatchpoints() && !doesMethodHavePartialCompilationPatchpoints())
    {
        // Nothing to report
        return;
    }

    // Patchpoints are only found in Tier0 code, which is unoptimized, and so
    // should always have frame pointer.
    assert(codeGen->isFramePointerUsed());

    // Allocate patchpoint info storage from runtime, and fill in initial bits of data.
    const unsigned        patchpointInfoSize = PatchpointInfo::ComputeSize(info.compLocalsCount);
    PatchpointInfo* const patchpointInfo     = (PatchpointInfo*)info.compCompHnd->allocateArray(patchpointInfoSize);

    // Patchpoint offsets always refer to "virtual frame offsets".
    //
    // For x64 this falls out because Tier0 frames are always FP frames, and so the FP-relative
    // offset is what we want.
    //
    // For arm64, if the frame pointer is not at the top of the frame, we need to adjust the
    // offset.
    CLANG_FORMAT_COMMENT_ANCHOR;

#if defined(TARGET_AMD64)
    // We add +TARGET_POINTER_SIZE here is to account for the slot that Jit_Patchpoint
    // creates when it simulates calling the OSR method (the "pseudo return address" slot).
    // This is effectively a new slot at the bottom of the Tier0 frame.
    //
    const int totalFrameSize = codeGen->genTotalFrameSize() + TARGET_POINTER_SIZE;
    const int offsetAdjust   = 0;
#elif defined(TARGET_ARM64) || defined(TARGET_LOONGARCH64) || defined(TARGET_RISCV64)
    // SP is not manipulated by calls so no frame size adjustment needed.
    // Local Offsets may need adjusting, if FP is at bottom of frame.
    //
    const int totalFrameSize = codeGen->genTotalFrameSize();
    const int offsetAdjust   = codeGen->genSPtoFPdelta() - totalFrameSize;
#else
    NYI("patchpoint info generation");
    const int offsetAdjust   = 0;
    const int totalFrameSize = 0;
#endif

    patchpointInfo->Initialize(info.compLocalsCount, totalFrameSize);

    JITDUMP("--OSR--- Total Frame Size %d, local offset adjust is %d\n", patchpointInfo->TotalFrameSize(),
            offsetAdjust);

    // We record offsets for all the "locals" here. Could restrict
    // this to just the IL locals with some extra logic, and save a bit of space,
    // but would need to adjust all consumers, too.
    for (unsigned lclNum = 0; lclNum < info.compLocalsCount; lclNum++)
    {
        // If there are shadowed params, the patchpoint info should refer to the shadow copy.
        //
        unsigned varNum = lclNum;

        if (gsShadowVarInfo != nullptr)
        {
            unsigned const shadowNum = gsShadowVarInfo[lclNum].shadowCopy;
            if (shadowNum != BAD_VAR_NUM)
            {
                assert(shadowNum < lvaCount);
                assert(shadowNum >= info.compLocalsCount);

                varNum = shadowNum;
            }
        }

        LclVarDsc* const varDsc = lvaGetDesc(varNum);

        // We expect all these to have stack homes, and be FP relative
        assert(varDsc->lvOnFrame);
        assert(varDsc->lvFramePointerBased);

        // Record FramePtr relative offset (no localloc yet)
        // Note if IL stream contained an address-of that potentially leads to exposure.
        // That bit of IL might be skipped by OSR partial importation.
        const bool isExposed = varDsc->lvHasLdAddrOp;
        patchpointInfo->SetOffsetAndExposure(lclNum, varDsc->GetStackOffset() + offsetAdjust, isExposed);

        JITDUMP("--OSR-- V%02u is at virtual offset %d%s%s\n", lclNum, patchpointInfo->Offset(lclNum),
                patchpointInfo->IsExposed(lclNum) ? " (exposed)" : "", (varNum != lclNum) ? " (shadowed)" : "");
    }

    // Special offsets
    //
    if (lvaReportParamTypeArg())
    {
        const int offset = lvaCachedGenericContextArgOffset();
        patchpointInfo->SetGenericContextArgOffset(offset + offsetAdjust);
        JITDUMP("--OSR-- cached generic context virtual offset is %d\n", patchpointInfo->GenericContextArgOffset());
    }

    if (lvaKeepAliveAndReportThis())
    {
        const int offset = lvaCachedGenericContextArgOffset();
        patchpointInfo->SetKeptAliveThisOffset(offset + offsetAdjust);
        JITDUMP("--OSR-- kept-alive this virtual offset is %d\n", patchpointInfo->KeptAliveThisOffset());
    }

    if (compGSReorderStackLayout)
    {
        assert(lvaGSSecurityCookie != BAD_VAR_NUM);
        LclVarDsc* const varDsc = lvaGetDesc(lvaGSSecurityCookie);
        patchpointInfo->SetSecurityCookieOffset(varDsc->GetStackOffset() + offsetAdjust);
        JITDUMP("--OSR-- security cookie V%02u virtual offset is %d\n", lvaGSSecurityCookie,
                patchpointInfo->SecurityCookieOffset());
    }

    if (lvaMonAcquired != BAD_VAR_NUM)
    {
        LclVarDsc* const varDsc = lvaGetDesc(lvaMonAcquired);
        patchpointInfo->SetMonitorAcquiredOffset(varDsc->GetStackOffset() + offsetAdjust);
        JITDUMP("--OSR-- monitor acquired V%02u virtual offset is %d\n", lvaMonAcquired,
                patchpointInfo->MonitorAcquiredOffset());
    }

#if defined(TARGET_AMD64)
    // Record callee save registers.
    // Currently only needed for x64.
    //
    regMaskTP rsPushRegs = codeGen->regSet.rsGetModifiedRegsMask() & RBM_CALLEE_SAVED;
    rsPushRegs |= RBM_FPBASE;
    patchpointInfo->SetCalleeSaveRegisters((uint64_t)rsPushRegs);
    JITDUMP("--OSR-- Tier0 callee saves: ");
    JITDUMPEXEC(dspRegMask((regMaskTP)patchpointInfo->CalleeSaveRegisters()));
    JITDUMP("\n");
#endif

    // Register this with the runtime.
    info.compCompHnd->setPatchpointInfo(patchpointInfo);
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
    vnStore              = nullptr;
    m_blockToEHPreds     = nullptr;
    m_dominancePreds     = nullptr;
    fgSsaPassesCompleted = 0;
    fgVNPassesCompleted  = 0;
    fgSsaValid           = false;

    for (BasicBlock* const block : Blocks())
    {
        for (Statement* const stmt : block->Statements())
        {
            for (GenTree* const tree : stmt->TreeList())
            {
                tree->ClearVN();
                tree->ClearAssertion();
                tree->gtCSEnum = NO_CSE;
            }
        }
    }
}

//------------------------------------------------------------------------
// RecomputeLoopInfo: Recompute flow graph annotations between opt-repeat iterations.
//
// Notes:
//    The intent of this method is to update all annotations computed on the flow graph
//    these annotations may have become stale during optimization, and need to be
//    up-to-date before running another iteration of optimizations.
//
void Compiler::RecomputeFlowGraphAnnotations()
{
    assert(opts.optRepeat);
    assert(JitConfig.JitOptRepeatCount() > 0);
    // Recompute reachability sets, dominators, and loops.
    optResetLoopInfo();

    fgComputeReachability();
    optSetBlockWeights();

    fgInvalidateDfsTree();
    m_dfsTree = fgComputeDfs();
    optFindLoops();

    if (fgHasLoops)
    {
        optFindAndScaleGeneralLoopBlocks();
    }

    m_domTree = FlowGraphDominatorTree::Build(m_dfsTree);
}

/*****************************************************************************/
void Compiler::ProcessShutdownWork(ICorStaticInfo* statInfo)
{
}

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

    if (!fJitRange.Contains(info.compMethodHash()))
    {
        return true;
    }

    if (JitConfig.JitExclude().contains(info.compMethodHnd, info.compClassHnd, &info.compMethodInfo->args))
    {
        return true;
    }

    if (!JitConfig.JitInclude().isEmpty() &&
        !JitConfig.JitInclude().contains(info.compMethodHnd, info.compClassHnd, &info.compMethodInfo->args))
    {
        return true;
    }

    return false;
}

#endif

/*****************************************************************************/

int Compiler::compCompile(CORINFO_MODULE_HANDLE classPtr,
                          void**                methodCodePtr,
                          uint32_t*             methodCodeSize,
                          JitFlags*             compileFlags)
{
    // compInit should have set these already.
    noway_assert(info.compMethodInfo != nullptr);
    noway_assert(info.compCompHnd != nullptr);
    noway_assert(info.compMethodHnd != nullptr);

#ifdef FEATURE_JIT_METHOD_PERF
    static bool checkedForJitTimeLog = false;

    pCompJitTimer = nullptr;

    if (!checkedForJitTimeLog)
    {
        // Call into VM to get the config strings. FEATURE_JIT_METHOD_PERF is enabled for
        // retail builds. Do not call the regular Config helper here as it would pull
        // in a copy of the config parser into the clrjit.dll.
        InterlockedCompareExchangeT(&Compiler::compJitTimeLogFilename,
                                    (LPCWSTR)info.compCompHnd->getJitTimeLogFilename(), NULL);

        // At a process or module boundary clear the file and start afresh.
        JitTimer::PrintCsvHeader();

        checkedForJitTimeLog = true;
    }
    if ((Compiler::compJitTimeLogFilename != nullptr) || (JitTimeLogCsv() != nullptr))
    {
        pCompJitTimer = JitTimer::Create(this, info.compMethodInfo->ILCodeSize);
    }
#endif // FEATURE_JIT_METHOD_PERF

#ifdef DEBUG
    Compiler* me  = this;
    forceFrameJIT = (void*)&me; // let us see the this pointer in fastchecked build
#endif

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
#if defined(DEBUG) && !defined(HOST_UNIX) // no 'perror' in the PAL
                perror("Failed to open JitFuncInfoLogFile");
#endif // defined(DEBUG) && !defined(HOST_UNIX)
            }
        }
    }
#endif // FUNC_INFO_LOGGING

    // if (s_compMethodsCount==0) setvbuf(jitstdout(), NULL, _IONBF, 0);

    if (compIsForInlining())
    {
        compileFlags->Clear(JitFlags::JIT_FLAG_OSR);
        info.compILEntry        = 0;
        info.compPatchpointInfo = nullptr;
    }
    else if (compileFlags->IsSet(JitFlags::JIT_FLAG_OSR))
    {
        // Fetch OSR info from the runtime
        info.compPatchpointInfo = info.compCompHnd->getOSRInfo(&info.compILEntry);
        assert(info.compPatchpointInfo != nullptr);
    }

#if defined(TARGET_ARM64)
    compFrameInfo = {0};
#endif

    virtualStubParamInfo = new (this, CMK_Unknown) VirtualStubParamInfo(IsTargetAbi(CORINFO_NATIVEAOT_ABI));

    // compMatchedVM is set to true if both CPU/ABI and OS are matching the execution engine requirements
    //
    // Do we have a matched VM? Or are we "abusing" the VM to help us do JIT work (such as using an x86 native VM
    // with an ARM-targeting "altjit").
    // Match CPU/ABI for compMatchedVM
    info.compMatchedVM = IMAGE_FILE_MACHINE_TARGET == info.compCompHnd->getExpectedTargetArchitecture();

    // Match OS for compMatchedVM
    CORINFO_EE_INFO* eeInfo = eeGetEEInfo();

#ifdef TARGET_OS_RUNTIMEDETERMINED
    noway_assert(TargetOS::OSSettingConfigured);
#endif

    if (TargetOS::IsApplePlatform)
    {
        info.compMatchedVM = info.compMatchedVM && (eeInfo->osType == CORINFO_APPLE);
    }
    else if (TargetOS::IsUnix)
    {
        if (TargetArchitecture::IsX64)
        {
            // Apple x64 uses the Unix jit variant in crossgen2, not a special jit
            info.compMatchedVM =
                info.compMatchedVM && ((eeInfo->osType == CORINFO_UNIX) || (eeInfo->osType == CORINFO_APPLE));
        }
        else
        {
            info.compMatchedVM = info.compMatchedVM && (eeInfo->osType == CORINFO_UNIX);
        }
    }
    else if (TargetOS::IsWindows)
    {
        info.compMatchedVM = info.compMatchedVM && (eeInfo->osType == CORINFO_WINNT);
    }

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
        CORINFO_InstructionSetFlags instructionSetFlags;

        // We need to assume, by default, that all flags coming from the VM are invalid.
        instructionSetFlags.Reset();

// We then add each available instruction set for the target architecture provided
// that the corresponding JitConfig switch hasn't explicitly asked for it to be
// disabled. This allows us to default to "everything" supported for altjit scenarios
// while also still allowing instruction set opt-out providing users with the ability
// to, for example, see and debug ARM64 codegen for any desired CPU configuration without
// needing to have the hardware in question.

#if defined(TARGET_ARM64)
        if (JitConfig.EnableHWIntrinsic() != 0)
        {
            instructionSetFlags.AddInstructionSet(InstructionSet_ArmBase);
        }

        if (JitConfig.EnableArm64AdvSimd() != 0)
        {
            instructionSetFlags.AddInstructionSet(InstructionSet_AdvSimd);
        }

        if (JitConfig.EnableArm64Aes() != 0)
        {
            instructionSetFlags.AddInstructionSet(InstructionSet_Aes);
        }

        if (JitConfig.EnableArm64Crc32() != 0)
        {
            instructionSetFlags.AddInstructionSet(InstructionSet_Crc32);
        }

        if (JitConfig.EnableArm64Dp() != 0)
        {
            instructionSetFlags.AddInstructionSet(InstructionSet_Dp);
        }

        if (JitConfig.EnableArm64Rdm() != 0)
        {
            instructionSetFlags.AddInstructionSet(InstructionSet_Rdm);
        }

        if (JitConfig.EnableArm64Sha1() != 0)
        {
            instructionSetFlags.AddInstructionSet(InstructionSet_Sha1);
        }

        if (JitConfig.EnableArm64Sha256() != 0)
        {
            instructionSetFlags.AddInstructionSet(InstructionSet_Sha256);
        }

        if (JitConfig.EnableArm64Atomics() != 0)
        {
            instructionSetFlags.AddInstructionSet(InstructionSet_Atomics);
        }

        if (JitConfig.EnableArm64Dczva() != 0)
        {
            instructionSetFlags.AddInstructionSet(InstructionSet_Dczva);
        }

        if (JitConfig.EnableArm64Sve() != 0)
        {
            instructionSetFlags.AddInstructionSet(InstructionSet_Sve);
        }
#elif defined(TARGET_XARCH)
        if (JitConfig.EnableHWIntrinsic() != 0)
        {
            instructionSetFlags.AddInstructionSet(InstructionSet_X86Base);
        }

        if (JitConfig.EnableSSE() != 0)
        {
            instructionSetFlags.AddInstructionSet(InstructionSet_SSE);
        }

        if (JitConfig.EnableSSE2() != 0)
        {
            instructionSetFlags.AddInstructionSet(InstructionSet_SSE2);
        }

        if ((JitConfig.EnableSSE3() != 0) && (JitConfig.EnableSSE3_4() != 0))
        {
            instructionSetFlags.AddInstructionSet(InstructionSet_SSE3);
        }

        if (JitConfig.EnableSSSE3() != 0)
        {
            instructionSetFlags.AddInstructionSet(InstructionSet_SSSE3);
        }

        if (JitConfig.EnableSSE41() != 0)
        {
            instructionSetFlags.AddInstructionSet(InstructionSet_SSE41);
        }

        if (JitConfig.EnableSSE42() != 0)
        {
            instructionSetFlags.AddInstructionSet(InstructionSet_SSE42);
        }

        if (JitConfig.EnableAVX() != 0)
        {
            instructionSetFlags.AddInstructionSet(InstructionSet_AVX);
        }

        if (JitConfig.EnableAVX2() != 0)
        {
            instructionSetFlags.AddInstructionSet(InstructionSet_AVX2);
        }

        if (JitConfig.EnableAES() != 0)
        {
            instructionSetFlags.AddInstructionSet(InstructionSet_AES);
        }

        if (JitConfig.EnableBMI1() != 0)
        {
            instructionSetFlags.AddInstructionSet(InstructionSet_BMI1);
        }

        if (JitConfig.EnableBMI2() != 0)
        {
            instructionSetFlags.AddInstructionSet(InstructionSet_BMI2);
        }

        if (JitConfig.EnableFMA() != 0)
        {
            instructionSetFlags.AddInstructionSet(InstructionSet_FMA);
        }

        if (JitConfig.EnableLZCNT() != 0)
        {
            instructionSetFlags.AddInstructionSet(InstructionSet_LZCNT);
        }

        if (JitConfig.EnablePCLMULQDQ() != 0)
        {
            instructionSetFlags.AddInstructionSet(InstructionSet_PCLMULQDQ);
        }

        if (JitConfig.EnablePOPCNT() != 0)
        {
            instructionSetFlags.AddInstructionSet(InstructionSet_POPCNT);
        }

        if (JitConfig.EnableAVXVNNI() != 0)
        {
            instructionSetFlags.AddInstructionSet(InstructionSet_AVXVNNI);
        }

        if (JitConfig.EnableAVX512F() != 0)
        {
            instructionSetFlags.AddInstructionSet(InstructionSet_AVX512F);
        }

        if (JitConfig.EnableAVX512F_VL() != 0)
        {
            instructionSetFlags.AddInstructionSet(InstructionSet_AVX512F_VL);
        }

        if (JitConfig.EnableAVX512BW() != 0)
        {
            instructionSetFlags.AddInstructionSet(InstructionSet_AVX512BW);
        }

        if (JitConfig.EnableAVX512BW_VL() != 0)
        {
            instructionSetFlags.AddInstructionSet(InstructionSet_AVX512BW_VL);
        }

        if (JitConfig.EnableAVX512CD() != 0)
        {
            instructionSetFlags.AddInstructionSet(InstructionSet_AVX512CD);
        }

        if (JitConfig.EnableAVX512CD_VL() != 0)
        {
            instructionSetFlags.AddInstructionSet(InstructionSet_AVX512CD_VL);
        }

        if (JitConfig.EnableAVX512DQ() != 0)
        {
            instructionSetFlags.AddInstructionSet(InstructionSet_AVX512DQ);
        }

        if (JitConfig.EnableAVX512DQ_VL() != 0)
        {
            instructionSetFlags.AddInstructionSet(InstructionSet_AVX512DQ_VL);
        }

        if (JitConfig.EnableAVX512VBMI() != 0)
        {
            instructionSetFlags.AddInstructionSet(InstructionSet_AVX512VBMI);
        }

        if (JitConfig.EnableAVX512VBMI_VL() != 0)
        {
            instructionSetFlags.AddInstructionSet(InstructionSet_AVX512VBMI_VL);
        }
#endif

        // These calls are important and explicitly ordered to ensure that the flags are correct in
        // the face of missing or removed instruction sets. Without them, we might end up with incorrect
        // downstream checks.

        instructionSetFlags.Set64BitInstructionSetVariants();
        instructionSetFlags = EnsureInstructionSetFlagsAreValid(instructionSetFlags);

        compileFlags->SetInstructionSetFlags(instructionSetFlags);
    }

    compMaxUncheckedOffsetForNullObject = eeGetEEInfo()->maxUncheckedOffsetForNullObject;

    // Set the context for token lookup.
    if (compIsForInlining())
    {
        impTokenLookupContextHandle = impInlineInfo->tokenLookupContextHandle;

        assert(impInlineInfo->inlineCandidateInfo->clsHandle == info.compClassHnd);
        assert(impInlineInfo->inlineCandidateInfo->clsAttr == info.compCompHnd->getClassAttribs(info.compClassHnd));
        // printf("%x != %x\n", impInlineInfo->inlineCandidateInfo->clsAttr,
        // info.compCompHnd->getClassAttribs(info.compClassHnd));
        info.compClassAttr = impInlineInfo->inlineCandidateInfo->clsAttr;
    }
    else
    {
        impTokenLookupContextHandle = METHOD_BEING_COMPILED_CONTEXT();

        info.compClassAttr = info.compCompHnd->getClassAttribs(info.compClassHnd);
    }

#ifdef DEBUG
    if (JitConfig.EnableExtraSuperPmiQueries())
    {
        // This call to getClassModule/getModuleAssembly/getAssemblyName fails in crossgen2 due to these
        // APIs being unimplemented. So disable this extra info for pre-jit mode. See
        // https://github.com/dotnet/runtime/issues/48888.
        //
        // Ditto for some of the class name queries for generic params.
        //
        if (!compileFlags->IsSet(JitFlags::JIT_FLAG_PREJIT))
        {
            // Get the assembly name, to aid finding any particular SuperPMI method context function
            (void)info.compCompHnd->getAssemblyName(
                info.compCompHnd->getModuleAssembly(info.compCompHnd->getClassModule(info.compClassHnd)));

            // Fetch class names for the method's generic parameters.
            //
            CORINFO_SIG_INFO sig;
            info.compCompHnd->getMethodSig(info.compMethodHnd, &sig, nullptr);

            const unsigned classInst = sig.sigInst.classInstCount;
            if (classInst > 0)
            {
                for (unsigned i = 0; i < classInst; i++)
                {
                    eeGetClassName(sig.sigInst.classInst[i]);
                }
            }

            const unsigned methodInst = sig.sigInst.methInstCount;
            if (methodInst > 0)
            {
                for (unsigned i = 0; i < methodInst; i++)
                {
                    eeGetClassName(sig.sigInst.methInst[i]);
                }
            }
        }
    }
#endif // DEBUG

    info.compProfilerCallback = false; // Assume false until we are told to hook this method.

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

#endif // DEBUG

    /* Setup an error trap */

    struct Param
    {
        Compiler* pThis;

        CORINFO_MODULE_HANDLE classPtr;
        COMP_HANDLE           compHnd;
        CORINFO_METHOD_INFO*  methodInfo;
        void**                methodCodePtr;
        uint32_t*             methodCodeSize;
        JitFlags*             compileFlags;

        int result;
    } param;
    param.pThis          = this;
    param.classPtr       = classPtr;
    param.compHnd        = info.compCompHnd;
    param.methodInfo     = info.compMethodInfo;
    param.methodCodePtr  = methodCodePtr;
    param.methodCodeSize = methodCodeSize;
    param.compileFlags   = compileFlags;
    param.result         = CORJIT_INTERNALERROR;

    setErrorTrap(info.compCompHnd, Param*, pParam, &param) // ERROR TRAP: Start normal block
    {
        pParam->result =
            pParam->pThis->compCompileHelper(pParam->classPtr, pParam->compHnd, pParam->methodInfo,
                                             pParam->methodCodePtr, pParam->methodCodeSize, pParam->compileFlags);
    }
    finallyErrorTrap() // ERROR TRAP: The following block handles errors
    {
        /* Cleanup  */

        if (compIsForInlining())
        {
            goto DoneCleanUp;
        }

        /* Tell the emitter that we're done with this function */

        GetEmitter()->emitEndCG();

    DoneCleanUp:
        compDone();
    }
    endErrorTrap() // ERROR TRAP: End

        return param.result;
}

#if defined(DEBUG)
//------------------------------------------------------------------------
// compMethodHash: get hash code for currently jitted method
//
// Returns:
//    Hash based on method's full name
//
unsigned Compiler::Info::compMethodHash() const
{
    if (compMethodHashPrivate == 0)
    {
        // compMethodHashPrivate = compCompHnd->getMethodHash(compMethodHnd);
        assert(compFullName != nullptr);
        assert(*compFullName != 0);
        COUNT_T hash = HashStringA(compFullName); // Use compFullName to generate the hash, as it contains the signature
                                                  // and return type
        compMethodHashPrivate = hash;
    }
    return compMethodHashPrivate;
}

//------------------------------------------------------------------------
// compMethodHash: get hash code for specified method
//
// Arguments:
//    methodHnd - method of interest
//
// Returns:
//    Hash based on method's full name
//
unsigned Compiler::compMethodHash(CORINFO_METHOD_HANDLE methodHnd)
{
    // If this is the root method, delegate to the caching version
    //
    if (methodHnd == info.compMethodHnd)
    {
        return info.compMethodHash();
    }

    // Else compute from scratch. Might consider caching this too.
    //
    unsigned    methodHash = 0;
    const char* calleeName = eeGetMethodFullName(methodHnd);

    if (calleeName != nullptr)
    {
        methodHash = HashStringA(calleeName);
    }
    else
    {
        methodHash = info.compCompHnd->getMethodHash(methodHnd);
    }

    return methodHash;
}

#endif // defined(DEBUG)

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
        compArenaAllocator->dumpMemStats(jitstdout());
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
        (impInlinedCodeSize <= 128) &&     // Is the inlining reasonably bounded?
                                           // Small methods cannot meaningfully have a big number of locals
                                           // or arguments. We always track arguments at the start of
                                           // the prolog which requires memory
        (info.compLocalsCount <= 32) && !opts.MinOpts() && // We may have too many local variables, etc
        (getJitStressLevel() == 0) &&                      // We need extra memory for stress
        !opts.optRepeat &&                                 // We need extra memory to repeat opts
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

#if defined(DEBUG)

    m_inlineStrategy->DumpData();

    if (JitConfig.JitInlineDumpXmlFile() != nullptr)
    {
        FILE* file = _wfopen(JitConfig.JitInlineDumpXmlFile(), W("a"));
        if (file != nullptr)
        {
            m_inlineStrategy->DumpXml(file);
            fclose(file);
        }
        else
        {
            m_inlineStrategy->DumpXml();
        }
    }
    else
    {
        m_inlineStrategy->DumpXml();
    }

#endif

#ifdef DEBUG
    if (opts.dspOrder)
    {
        // mdMethodDef __stdcall CEEInfo::getMethodDefFromMethod(CORINFO_METHOD_HANDLE hMethod)
        mdMethodDef currentMethodToken = info.compCompHnd->getMethodDefFromMethod(info.compMethodHnd);

        static bool headerPrinted = false;
        if (!headerPrinted)
        {
            // clang-format off
            headerPrinted = true;
            printf("         |  Profiled   | Method   |   Method has    |   calls   | Num |LclV |AProp| CSE |   Perf  |bytes | %3s codesize| \n", Target::g_tgtCPUName);
            printf(" mdToken |  CNT |  RGN |    Hash  | EH | FRM | LOOP | NRM | IND | BBs | Cnt | Cnt | Cnt |  Score  |  IL  |   HOT | CLD | method name \n");
            printf("---------+------+------+----------+----+-----+------+-----+-----+-----+-----+-----+-----+---------+------+-------+-----+\n");
            //      06001234 | 1234 |  HOT | 0f1e2d3c | EH | ebp | LOOP |  15 |   6 |  12 |  17 |  12 |   8 | 1234.56 |  145 |  1234 | 123 | System.Example(int)
            // clang-format on
        }

        printf("%08X | ", currentMethodToken);

        if (fgHaveProfileWeights())
        {
            if (fgCalledCount < 1000)
            {
                printf("%4.0f | ", fgCalledCount);
            }
            else if (fgCalledCount < 1000000)
            {
                printf("%3.0fK | ", fgCalledCount / 1000);
            }
            else
            {
                printf("%3.0fM | ", fgCalledCount / 1000000);
            }
        }
        else
        {
            printf("     | ");
        }

        CorInfoRegionKind regionKind = info.compMethodInfo->regionKind;

        if (opts.altJit)
        {
            printf("ALT | ");
        }
        else if (regionKind == CORINFO_REGION_NONE)
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

        printf("%08x | ", info.compMethodHash());

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
            printf(" %3d |", optCSEcount);
        }

        if (info.compPerfScore < 9999.995)
        {
            printf(" %7.2f |", info.compPerfScore);
        }
        else
        {
            printf(" %7.0f |", info.compPerfScore);
        }

        printf(" %4d |", info.compMethodInfo->ILCodeSize);
        printf(" %5d |", info.compTotalHotCodeSize);
        printf(" %3d |", info.compTotalColdCodeSize);

        printf(" %s\n", eeGetMethodFullName(info.compMethodHnd));
        printf(""); // in our logic this causes a flush
    }

    if (verbose)
    {
        printf("****** DONE compiling %s\n", info.compFullName);
        printf(""); // in our logic this causes a flush
    }

#if TRACK_ENREG_STATS
    for (unsigned i = 0; i < lvaCount; ++i)
    {
        const LclVarDsc* varDsc = lvaGetDesc(i);

        if (varDsc->lvRefCnt() != 0)
        {
            s_enregisterStats.RecordLocal(varDsc);
        }
    }
#endif // TRACK_ENREG_STATS

    // Only call _DbgBreakCheck when we are jitting, not when we are ngen-ing
    // For ngen the int3 or breakpoint instruction will be right at the
    // start of the ngen method and we will stop when we execute it.
    //
    if (!opts.jitFlags->IsSet(JitFlags::JIT_FLAG_PREJIT))
    {
        if (compJitHaltMethod())
        {
#if !defined(HOST_UNIX)
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

unsigned getMethodBodyChecksum(_In_z_ char* code, int size)
{
#ifdef PSEUDORANDOM_NOP_INSERTION
    return adler32(0, code, size);
#else
    return 0;
#endif
}

int Compiler::compCompileHelper(CORINFO_MODULE_HANDLE classPtr,
                                COMP_HANDLE           compHnd,
                                CORINFO_METHOD_INFO*  methodInfo,
                                void**                methodCodePtr,
                                uint32_t*             methodCodeSize,
                                JitFlags*             compileFlags)
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

        info.compFlags    = impInlineInfo->inlineCandidateInfo->methAttr;
        compInlineContext = impInlineInfo->inlineContext;
    }
    else
    {
        info.compFlags = info.compCompHnd->getMethodAttribs(info.compMethodHnd);
#ifdef PSEUDORANDOM_NOP_INSERTION
        info.compChecksum = getMethodBodyChecksum((char*)methodInfo->ILCode, methodInfo->ILCodeSize);
#endif
        compInlineContext = m_inlineStrategy->GetRootContext();
    }

    compSwitchedToOptimized = false;
    compSwitchedToMinOpts   = false;

    // compInitOptions will set the correct verbose flag.

    compInitOptions(compileFlags);

    if (!compIsForInlining() && !opts.altJit && opts.jitFlags->IsSet(JitFlags::JIT_FLAG_ALT_JIT))
    {
        // We're an altjit, but the DOTNET_AltJit configuration did not say to compile this method,
        // so skip it.
        return CORJIT_SKIPPED;
    }

#ifdef DEBUG

    if (verbose)
    {
        printf("IL to import:\n");
        dumpILRange(info.compCode, info.compILCodeSize);
    }

#endif

    // Check for DOTNET_AggressiveInlining
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

#endif // DEBUG

    impCanReimport = compStressCompile(STRESS_CHK_REIMPORT, 15);

    /* Initialize set a bunch of global values */

    info.compScopeHnd      = classPtr;
    info.compXcptnsCount   = methodInfo->EHcount;
    info.compMaxStack      = methodInfo->maxStack;
    compHndBBtab           = nullptr;
    compHndBBtabCount      = 0;
    compHndBBtabAllocCount = 0;

    info.compNativeCodeSize            = 0;
    info.compTotalHotCodeSize          = 0;
    info.compTotalColdCodeSize         = 0;
    info.compHandleHistogramProbeCount = 0;

    compHasBackwardJump          = false;
    compHasBackwardJumpInHandler = false;

#ifdef DEBUG
    compCurBB = nullptr;
    lvaTable  = nullptr;

    // Reset node and block ID counter
    compGenTreeID    = 0;
    compStatementID  = 0;
    compBasicBlockID = 0;
#endif

#ifdef TARGET_ARM64
    info.compNeedsConsecutiveRegisters = false;
#endif

    /* Initialize emitter */

    if (!compIsForInlining())
    {
        codeGen->GetEmitter()->emitBegCG(this, compHnd);
    }

    info.compIsStatic = (info.compFlags & CORINFO_FLG_STATIC) != 0;

    info.compPublishStubParam = opts.jitFlags->IsSet(JitFlags::JIT_FLAG_PUBLISH_SECRET_PARAM);

    info.compHasNextCallRetAddr = false;

    if (opts.IsReversePInvoke())
    {
        bool unused;
        info.compCallConv = info.compCompHnd->getUnmanagedCallConv(methodInfo->ftn, nullptr, &unused);
        info.compArgOrder = Target::g_tgtUnmanagedArgOrder;
    }
    else
    {
        info.compCallConv = CorInfoCallConvExtension::Managed;
        info.compArgOrder = Target::g_tgtArgOrder;
    }

    info.compIsVarArgs = false;

    switch (methodInfo->args.getCallConv())
    {
        case CORINFO_CALLCONV_NATIVEVARARG:
        case CORINFO_CALLCONV_VARARG:
            info.compIsVarArgs = true;
            break;
        default:
            break;
    }

    info.compRetType = JITtype2varType(methodInfo->args.retType);
    if (info.compRetType == TYP_STRUCT)
    {
        info.compRetType = impNormStructType(methodInfo->args.retTypeClass);
    }

    info.compUnmanagedCallCountWithGCTransition = 0;
    info.compLvFrameListRoot                    = BAD_VAR_NUM;

    info.compInitMem = ((methodInfo->options & CORINFO_OPT_INIT_LOCALS) != 0);

    /* Allocate the local variable table */

    lvaInitTypeRef();

    compInitDebuggingInfo();

    // If are an altjit and have patchpoint info, we might need to tweak the frame size
    // so it's plausible for the altjit architecture.
    //
    if (!info.compMatchedVM && compileFlags->IsSet(JitFlags::JIT_FLAG_OSR))
    {
        assert(info.compLocalsCount == info.compPatchpointInfo->NumberOfLocals());
        const int totalFrameSize = info.compPatchpointInfo->TotalFrameSize();

        int frameSizeUpdate = 0;

#if defined(TARGET_AMD64)
        if ((totalFrameSize % 16) != 8)
        {
            frameSizeUpdate = 8;
        }
#elif defined(TARGET_ARM64) || defined(TARGET_LOONGARCH64) || defined(TARGET_RISCV64)
        if ((totalFrameSize & 0xf) != 0)
        {
            frameSizeUpdate = 8;
        }
#endif
        if (frameSizeUpdate != 0)
        {
            JITDUMP("Mismatched altjit + OSR -- updating tier0 frame size from %d to %d\n", totalFrameSize,
                    totalFrameSize + frameSizeUpdate);

            // Allocate a local copy with altered frame size.
            //
            const unsigned        patchpointInfoSize = PatchpointInfo::ComputeSize(info.compLocalsCount);
            PatchpointInfo* const newInfo =
                (PatchpointInfo*)getAllocator(CMK_Unknown).allocate<char>(patchpointInfoSize);

            newInfo->Initialize(info.compLocalsCount, totalFrameSize + frameSizeUpdate);
            newInfo->Copy(info.compPatchpointInfo);

            // Swap it in place.
            //
            info.compPatchpointInfo = newInfo;
        }
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

        // Profile data allows us to avoid early "too many IL bytes" outs.
        prejitResult.NoteBool(InlineObservation::CALLSITE_HAS_PROFILE_WEIGHTS, fgHaveSufficientProfileWeights());

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
            prejitResult.SetSuccessResult(INLINE_PREJIT_SUCCESS);
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

    // We may decide to optimize this method,
    // to avoid spending a long time stuck in Tier0 code.
    //
    if (fgCanSwitchToOptimized())
    {
        // We only expect to be able to do this at Tier0.
        //
        assert(opts.jitFlags->IsSet(JitFlags::JIT_FLAG_TIER0));

        // Normal tiering should bail us out of Tier0 tail call induced loops.
        // So keep these methods in Tier0 if we're gathering PGO data.
        // If we're not gathering PGO, then switch these to optimized to
        // minimize the number of tail call helper stubs we might need.
        // Reconsider this if/when we're able to share those stubs.
        //
        // Honor the config setting that tells the jit to
        // always optimize methods with loops.
        //
        // If neither of those apply, and OSR is enabled, the jit may still
        // decide to optimize, if there's something in the method that
        // OSR currently cannot handle, or we're optionally suppressing
        // OSR by method hash.
        //
        const char* reason = nullptr;

        if (compTailPrefixSeen && !opts.jitFlags->IsSet(JitFlags::JIT_FLAG_BBINSTR))
        {
            reason = "tail.call and not BBINSTR";
        }
        else if (compHasBackwardJump && ((info.compFlags & CORINFO_FLG_DISABLE_TIER0_FOR_LOOPS) != 0))
        {
            reason = "loop";
        }

        if (compHasBackwardJump && (reason == nullptr) && (JitConfig.TC_OnStackReplacement() > 0))
        {
            bool canEscapeViaOSR = compCanHavePatchpoints(&reason);

#ifdef DEBUG
            if (canEscapeViaOSR)
            {
                // Optionally disable OSR by method hash. This will force any
                // method that might otherwise get trapped in Tier0 to be optimized.
                //
                static ConfigMethodRange JitEnableOsrRange;
                JitEnableOsrRange.EnsureInit(JitConfig.JitEnableOsrRange());
                const unsigned hash = impInlineRoot()->info.compMethodHash();
                if (!JitEnableOsrRange.Contains(hash))
                {
                    canEscapeViaOSR = false;
                    reason          = "OSR disabled by JitEnableOsrRange";
                }
            }
#endif

            if (canEscapeViaOSR)
            {
                JITDUMP("\nOSR enabled for this method\n");
                if (compHasBackwardJump && !compTailPrefixSeen &&
                    opts.jitFlags->IsSet(JitFlags::JIT_FLAG_BBINSTR_IF_LOOPS) && opts.IsTier0())
                {
                    assert((info.compFlags & CORINFO_FLG_DISABLE_TIER0_FOR_LOOPS) == 0);
                    opts.jitFlags->Set(JitFlags::JIT_FLAG_BBINSTR);
                    JITDUMP("\nEnabling instrumentation for this method so OSR'd version will have a profile.\n");
                }
            }
            else
            {
                JITDUMP("\nOSR disabled for this method: %s\n", reason);
                assert(reason != nullptr);
            }
        }

        if (reason != nullptr)
        {
            fgSwitchToOptimized(reason);
        }
    }

    compSetOptimizationLevel();

    if ((JitConfig.JitDisasmOnlyOptimized() != 0) && (!opts.OptimizationEnabled()))
    {
        // Disable JitDisasm for non-optimized code.
        opts.disAsm = false;
    }

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

    compMethodID = 0;
#ifdef DEBUG
    /* Give the function a unique number */

    if (opts.disAsm || verbose)
    {
        compMethodID = ~info.compMethodHash() & 0xffff;
    }
    else
    {
        compMethodID = InterlockedIncrement(&s_compMethodsCount);
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
    if (compIsForInlining())
    {
        compGenTreeID   = impInlineInfo->InlinerCompiler->compGenTreeID;
        compStatementID = impInlineInfo->InlinerCompiler->compStatementID;
    }
#endif

    compCompile(methodCodePtr, methodCodeSize, compileFlags);

#ifdef DEBUG
    if (compIsForInlining())
    {
        impInlineInfo->InlinerCompiler->compGenTreeID    = compGenTreeID;
        impInlineInfo->InlinerCompiler->compStatementID  = compStatementID;
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

#ifdef DEBUG
        if (opts.jitFlags->IsSet(JitFlags::JIT_FLAG_ALT_JIT) && JitConfig.RunAltJitCode() == 0)
        {
            return CORJIT_SKIPPED;
        }
#endif // DEBUG
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

struct genCmpLocalVarLifeBeg
{
    bool operator()(const VarScopeDsc* elem1, const VarScopeDsc* elem2)
    {
        return elem1->vsdLifeBeg < elem2->vsdLifeBeg;
    }
};

struct genCmpLocalVarLifeEnd
{
    bool operator()(const VarScopeDsc* elem1, const VarScopeDsc* elem2)
    {
        return elem1->vsdLifeEnd < elem2->vsdLifeEnd;
    }
};

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

    jitstd::sort(compEnterScopeList, compEnterScopeList + info.compVarScopesCount, genCmpLocalVarLifeBeg());
    jitstd::sort(compExitScopeList, compExitScopeList + info.compVarScopesCount, genCmpLocalVarLifeEnd());
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
                // in ICorJitInfo_wrapper_generated.hpp are no longer in sync with
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
#include "ICorJitInfo_wrapper_generated.hpp"
};

#endif // MEASURE_CLRAPI_CALLS

/*****************************************************************************/

// Compile a single method

int jitNativeCode(CORINFO_METHOD_HANDLE methodHnd,
                  CORINFO_MODULE_HANDLE classPtr,
                  COMP_HANDLE           compHnd,
                  CORINFO_METHOD_INFO*  methodInfo,
                  void**                methodCodePtr,
                  uint32_t*             methodCodeSize,
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
        uint32_t*             methodCodeSize;
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

            pParam->pComp->compInit(pParam->pAlloc, pParam->methodHnd, pParam->compHnd, pParam->methodInfo,
                                    pParam->inlineInfo);

#ifdef DEBUG
            pParam->pComp->jitFallbackCompile = pParam->jitFallbackCompile;
#endif

            // Now generate the code
            pParam->result = pParam->pComp->compCompile(pParam->classPtr, pParam->methodCodePtr, pParam->methodCodeSize,
                                                        pParam->compileFlags);
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

    if (!inlineInfo &&
        (result == CORJIT_INTERNALERROR || result == CORJIT_RECOVERABLEERROR || result == CORJIT_IMPLLIMITATION) &&
        !jitFallbackCompile)
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

    for (BasicBlock* const block : Blocks())
    {
        for (Statement* const stmt : block->NonPhiStatements())
        {
            for (GenTree* const tree : stmt->TreeList())
            {
                TestLabelAndNum tlAndN;

                // For call nodes, translate late args to what they stand for.
                if (tree->OperGet() == GT_CALL)
                {
                    GenTreeCall* call = tree->AsCall();
                    unsigned     i    = 0;
                    for (CallArg& arg : call->gtArgs.Args())
                    {
                        GenTree* argNode = arg.GetNode();
                        if (GetNodeTestData()->Lookup(argNode, &tlAndN))
                        {
                            reachable->Set(argNode, 0);
                        }
                        i++;
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

    for (BasicBlock* const block : Blocks())
    {
        for (Statement* const stmt : block->Statements())
        {
            for (GenTree* const call : stmt->TreeList())
            {
                if (call->gtOper != GT_CALL)
                    continue;

                argNum = regArgNum = regArgDeferred = regArgTemp = regArgConst = regArgLclVar = argDWordNum =
                    argLngNum = argFltNum = argDblNum = 0;

                argTotalCalls++;

                if (call->AsCall()->gtCallThisArg == nullptr)
                {
                    if (call->AsCall()->gtCallType == CT_HELPER)
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

                    if (call->AsCall()->IsVirtual())
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
double JitTimer::s_cyclesPerSec = CachedCyclesPerSecond();
#endif
#endif // FEATURE_JIT_METHOD_PERF

#if defined(FEATURE_JIT_METHOD_PERF) || DUMP_FLOWGRAPHS
const char* PhaseNames[] = {
#define CompPhaseNameMacro(enum_nm, string_nm, hasChildren, parent, measureIR) string_nm,
#include "compphases.h"
};

const char* PhaseEnums[] = {
#define CompPhaseNameMacro(enum_nm, string_nm, hasChildren, parent, measureIR) #enum_nm,
#include "compphases.h"
};

#endif // defined(FEATURE_JIT_METHOD_PERF) || DUMP_FLOWGRAPHS

#ifdef FEATURE_JIT_METHOD_PERF
bool PhaseHasChildren[] = {
#define CompPhaseNameMacro(enum_nm, string_nm, hasChildren, parent, measureIR) hasChildren,
#include "compphases.h"
};

int PhaseParent[] = {
#define CompPhaseNameMacro(enum_nm, string_nm, hasChildren, parent, measureIR) parent,
#include "compphases.h"
};

bool PhaseReportsIRSize[] = {
#define CompPhaseNameMacro(enum_nm, string_nm, hasChildren, parent, measureIR) measureIR,
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
    assert(ArrLen(m_perClrAPIcalls) == API_ICorJitInfo_Names::API_COUNT);
    assert(ArrLen(m_perClrAPIcycles) == API_ICorJitInfo_Names::API_COUNT);
    assert(ArrLen(m_maxClrAPIcycles) == API_ICorJitInfo_Names::API_COUNT);
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
    double countsPerSec = CachedCyclesPerSecond();
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
        assert(ArrLen(PhaseNames) == PHASE_NUMBER_OF);
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
        assert(ArrLen(PhaseNames) == PHASE_NUMBER_OF);
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
#include "ICorJitInfo_names_generated.h"
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

// It's expensive to constantly open and close the file, so open it once and close it
// when the process exits. This should be accessed under the s_csvLock.
FILE* JitTimer::s_csvFile = nullptr;

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

    if (s_csvFile == nullptr)
    {
        s_csvFile = _wfopen(jitTimeLogCsv, W("a"));
    }
    if (s_csvFile != nullptr)
    {
        // Seek to the end of the file s.t. `ftell` doesn't lie to us on Windows
        fseek(s_csvFile, 0, SEEK_END);

        // Write the header if the file is empty
        if (ftell(s_csvFile) == 0)
        {
            fprintf(s_csvFile, "\"Method Name\",");
            fprintf(s_csvFile, "\"Assembly or SPMI Index\",");
            fprintf(s_csvFile, "\"IL Bytes\",");
            fprintf(s_csvFile, "\"Basic Blocks\",");
            fprintf(s_csvFile, "\"Min Opts\",");
            fprintf(s_csvFile, "\"Loops\",");
            fprintf(s_csvFile, "\"Loops Cloned\",");
#if FEATURE_LOOP_ALIGN
#ifdef DEBUG
            fprintf(s_csvFile, "\"Alignment Candidates\",");
            fprintf(s_csvFile, "\"Loops Aligned\",");
#endif // DEBUG
#endif // FEATURE_LOOP_ALIGN
            for (int i = 0; i < PHASE_NUMBER_OF; i++)
            {
                fprintf(s_csvFile, "\"%s\",", PhaseNames[i]);
                if ((JitConfig.JitMeasureIR() != 0) && PhaseReportsIRSize[i])
                {
                    fprintf(s_csvFile, "\"Node Count After %s\",", PhaseNames[i]);
                }
            }

            InlineStrategy::DumpCsvHeader(s_csvFile);

            fprintf(s_csvFile, "\"Executable Code Bytes\",");
            fprintf(s_csvFile, "\"GC Info Bytes\",");
            fprintf(s_csvFile, "\"Total Bytes Allocated\",");
            fprintf(s_csvFile, "\"Total Cycles\",");
            fprintf(s_csvFile, "\"CPS\"\n");

            fflush(s_csvFile);
        }
    }
}

void JitTimer::PrintCsvMethodStats(Compiler* comp)
{
    LPCWSTR jitTimeLogCsv = Compiler::JitTimeLogCsv();
    if (jitTimeLogCsv == nullptr)
    {
        return;
    }

// eeGetMethodFullName uses locks, so don't enter crit sec before this call.
#if defined(DEBUG) || defined(LATE_DISASM)
    // If we already have computed the name because for some reason we're generating the CSV
    // for a DEBUG build (presumably not for the time info), just re-use it.
    const char* methName = comp->info.compFullName;
#else
    const char*          methName  = comp->eeGetMethodFullName(comp->info.compMethodHnd);
#endif

    // Try and access the SPMI index to report in the data set.
    //
    // If the jit is not hosted under SPMI this will return the
    // default value of zero.
    //
    // Query the jit host directly here instead of going via the
    // config cache, since value will change for each method.
    int index = g_jitHost->getIntConfigValue(W("SuperPMIMethodContextNumber"), -1);

    CritSecHolder csvLock(s_csvLock);

    if (s_csvFile == nullptr)
    {
        return;
    }

    fprintf(s_csvFile, "\"%s\",", methName);
    if (index != 0)
    {
        fprintf(s_csvFile, "%d,", index);
    }
    else
    {
        const char* methodAssemblyName = comp->info.compCompHnd->getAssemblyName(
            comp->info.compCompHnd->getModuleAssembly(comp->info.compCompHnd->getClassModule(comp->info.compClassHnd)));
        fprintf(s_csvFile, "\"%s\",", methodAssemblyName);
    }
    fprintf(s_csvFile, "%u,", comp->info.compILCodeSize);
    fprintf(s_csvFile, "%u,", comp->fgBBcount);
    fprintf(s_csvFile, "%u,", comp->opts.MinOpts());
    fprintf(s_csvFile, "%u,", comp->optNumNaturalLoopsFound);
    fprintf(s_csvFile, "%u,", comp->optLoopsCloned);
#if FEATURE_LOOP_ALIGN
#ifdef DEBUG
    fprintf(s_csvFile, "%u,", comp->loopAlignCandidates);
    fprintf(s_csvFile, "%u,", comp->loopsAligned);
#endif // DEBUG
#endif // FEATURE_LOOP_ALIGN
    unsigned __int64 totCycles = 0;
    for (int i = 0; i < PHASE_NUMBER_OF; i++)
    {
        if (!PhaseHasChildren[i])
        {
            totCycles += m_info.m_cyclesByPhase[i];
        }
        fprintf(s_csvFile, "%llu,", (unsigned long long)m_info.m_cyclesByPhase[i]);

        if ((JitConfig.JitMeasureIR() != 0) && PhaseReportsIRSize[i])
        {
            fprintf(s_csvFile, "%u,", m_info.m_nodeCountAfterPhase[i]);
        }
    }

    comp->m_inlineStrategy->DumpCsvData(s_csvFile);

    fprintf(s_csvFile, "%u,", comp->info.compNativeCodeSize);
    fprintf(s_csvFile, "%zu,", comp->compInfoBlkSize);
    fprintf(s_csvFile, "%zu,", comp->compGetArenaAllocator()->getTotalBytesAllocated());
    fprintf(s_csvFile, "%llu,", (unsigned long long)m_info.m_totalCycles);
    fprintf(s_csvFile, "%f\n", CachedCyclesPerSecond());

    fflush(s_csvFile);
}

// Perform process shutdown actions.
//
// static
void JitTimer::Shutdown()
{
    CritSecHolder csvLock(s_csvLock);
    if (s_csvFile != nullptr)
    {
        fclose(s_csvFile);
    }
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
#if defined(DEBUG)

    m_compCyclesAtEndOfInlining    = 0;
    m_compTickCountAtEndOfInlining = 0;
    bool b                         = CycleTimer::GetThreadCyclesS(&m_compCyclesAtEndOfInlining);
    if (!b)
    {
        return; // We don't have a thread cycle counter.
    }
    m_compTickCountAtEndOfInlining = GetTickCount();

#endif // defined(DEBUG)
}

//------------------------------------------------------------------------
// RecordStateAtEndOfCompilation: capture timing data (if enabled) after
// compilation is completed.

void Compiler::RecordStateAtEndOfCompilation()
{
#if defined(DEBUG)

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

#endif // defined(DEBUG)
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
        unsigned varNum    = comp->lvaTrackedIndexToLclNum(varIndex);
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
 *      cStmt,       dStmt          : Display a Statement (call gtDispStmt()).
 *      cTree,       dTree          : Display a tree (call gtDispTree()).
 *      cTreeLIR,    dTreeLIR       : Display a tree in LIR form (call gtDispLIRNode()).
 *      cTreeRange,  dTreeRange     : Display a range of trees in LIR form (call gtDispLIRNode()).
 *      cTrees,      dTrees         : Display all the trees in a function (call fgDumpTrees()).
 *      cEH,         dEH            : Display the EH handler table (call fgDispHandlerTab()).
 *      cVar,        dVar           : Display a local variable given its number (call lvaDumpEntry()).
 *      cVarDsc,     dVarDsc        : Display a local variable given a LclVarDsc* (call lvaDumpEntry()).
 *      cVars,       dVars          : Display the local variable table (call lvaTableDump()).
 *      cVarsFinal,  dVarsFinal     : Display the local variable table (call lvaTableDump(FINAL_FRAME_LAYOUT)).
 *      cBlockPreds, dBlockPreds    : Display a block's predecessors (call block->dspPreds()).
 *      cBlockSuccs, dBlockSuccs    : Display a block's successors (call block->dspSuccs(compiler)).
 *      cReach,      dReach         : Display all block reachability (call BlockReachabilitySets::Dump).
 *      cDoms,       dDoms          : Display all block dominators (call FlowGraphDominatorTree::Dump).
 *      cLiveness,   dLiveness      : Display per-block variable liveness (call fgDispBBLiveness()).
 *      cCVarSet,    dCVarSet       : Display a "converted" VARSET_TP: the varset is assumed to be tracked variable
 *                                    indices. These are converted to variable numbers and sorted. (Calls
 *                                    dumpConvertedVarSet()).
 *      cLoops,      dLoops         : Display the loops (call FlowGraphNaturalLoops::Dump()) with
 *                                    Compiler::m_loops.
 *      cLoopsA,     dLoopsA        : Display the loops (call FlowGraphNaturalLoops::Dump()) with a given
 *                                    loops arg.
 *      cLoop,       dLoop          : Display a single loop (call FlowGraphNaturalLoop::Dump()) with given
 *                                    loop arg.
 *      cTreeFlags,  dTreeFlags     : Display tree flags for a specified tree.
 *      cVN,         dVN            : Display a ValueNum (call vnPrint()).
 *
 * The following don't require a Compiler* to work:
 *      dRegMask                    : Display a regMaskTP (call dspRegMask(mask)).
 *      dBlockList                  : Display a BasicBlockList*.
 *
 * The following find an object in the IR and return it, as well as setting a global variable with the value that can
 * be used in the debugger (e.g., in the watch window, or as a way to get an address for data breakpoints).
 *      dFindTreeInTree             : Find a tree in a given tree, specifying a tree id. Sets `dbTree`.
 *      dFindTree                   : Find a tree in the IR, specifying a tree id. Sets `dbTree` and `dbTreeBlock`.
 *      dFindStmt                   : Find a Statement in the IR, specifying a statement id. Sets `dbStmt`.
 *      dFindBlock                  : Find a block in the IR, specifying a block number. Sets `dbBlock`.
 *      dFindLoop                   : Find a loop specifying a loop index. Sets `dbLoop`.
 */

// Make the debug helpers available (under #ifdef DEBUG) even though they are unreferenced. When the Microsoft
// linker is using /OPT:REF, it throws away unreferenced COMDATs (typically functions). Using "/INCLUDE:symbol"
// forces the linker to keep these anyway.
//
// Mark them `export "C"` so the names are not C++ name mangled. This makes it easier to refer to them in the
// `/INCLUDE` switch, and potentially makes them easier to find by a debugger.
//
// On x64/arm64, "C" names are not decorated (mangled). However, on x86, the names are decorated for `__stdcall`
// with a leading `_` and trailing `@N` for `N` bytes of arguments. To simplify, use __cdecl for x86. (Specifying
// it for other platforms is a no-op.)

#define JITDBGAPI extern "C"

#if defined(_MSC_VER)

#if defined(HOST_AMD64) || defined(HOST_ARM64)

// Functions which take a Compiler*
#pragma comment(linker, "/include:cBlock")
#pragma comment(linker, "/include:cBlocks")
#pragma comment(linker, "/include:cBlocksV")
#pragma comment(linker, "/include:cStmt")
#pragma comment(linker, "/include:cTree")
#pragma comment(linker, "/include:cTreeLIR")
#pragma comment(linker, "/include:cTreeRange")
#pragma comment(linker, "/include:cTrees")
#pragma comment(linker, "/include:cEH")
#pragma comment(linker, "/include:cVar")
#pragma comment(linker, "/include:cVarDsc")
#pragma comment(linker, "/include:cVars")
#pragma comment(linker, "/include:cVarsFinal")
#pragma comment(linker, "/include:cBlockPreds")
#pragma comment(linker, "/include:cBlockSuccs")
#pragma comment(linker, "/include:cReach")
#pragma comment(linker, "/include:cDoms")
#pragma comment(linker, "/include:cLiveness")
#pragma comment(linker, "/include:cCVarSet")
#pragma comment(linker, "/include:cLoops")
#pragma comment(linker, "/include:cLoopsA")
#pragma comment(linker, "/include:cLoop")
#pragma comment(linker, "/include:cTreeFlags")
#pragma comment(linker, "/include:cVN")

// Functions which call the c* functions getting Compiler* using `JitTls::GetCompiler()`
#pragma comment(linker, "/include:dBlock")
#pragma comment(linker, "/include:dBlocks")
#pragma comment(linker, "/include:dBlocksV")
#pragma comment(linker, "/include:dStmt")
#pragma comment(linker, "/include:dTree")
#pragma comment(linker, "/include:dTreeLIR")
#pragma comment(linker, "/include:dTreeRange")
#pragma comment(linker, "/include:dTrees")
#pragma comment(linker, "/include:dEH")
#pragma comment(linker, "/include:dVar")
#pragma comment(linker, "/include:dVarDsc")
#pragma comment(linker, "/include:dVars")
#pragma comment(linker, "/include:dVarsFinal")
#pragma comment(linker, "/include:dBlockPreds")
#pragma comment(linker, "/include:dBlockSuccs")
#pragma comment(linker, "/include:dReach")
#pragma comment(linker, "/include:dDoms")
#pragma comment(linker, "/include:dLiveness")
#pragma comment(linker, "/include:dCVarSet")
#pragma comment(linker, "/include:dLoop")
#pragma comment(linker, "/include:dLoops")
#pragma comment(linker, "/include:dTreeFlags")
#pragma comment(linker, "/include:dVN")

// Functions which don't require a Compiler*
#pragma comment(linker, "/include:dRegMask")
#pragma comment(linker, "/include:dBlockList")

// Functions which search for objects in the IR
#pragma comment(linker, "/include:dFindTreeInTree")
#pragma comment(linker, "/include:dFindTree")
#pragma comment(linker, "/include:dFindStmt")
#pragma comment(linker, "/include:dFindBlock")
#pragma comment(linker, "/include:dFindLoop")

#elif defined(HOST_X86)

// Functions which take a Compiler*
#pragma comment(linker, "/include:_cBlock")
#pragma comment(linker, "/include:_cBlocks")
#pragma comment(linker, "/include:_cBlocksV")
#pragma comment(linker, "/include:_cStmt")
#pragma comment(linker, "/include:_cTree")
#pragma comment(linker, "/include:_cTreeLIR")
#pragma comment(linker, "/include:_cTreeRange")
#pragma comment(linker, "/include:_cTrees")
#pragma comment(linker, "/include:_cEH")
#pragma comment(linker, "/include:_cVar")
#pragma comment(linker, "/include:_cVarDsc")
#pragma comment(linker, "/include:_cVars")
#pragma comment(linker, "/include:_cVarsFinal")
#pragma comment(linker, "/include:_cBlockPreds")
#pragma comment(linker, "/include:_cBlockSuccs")
#pragma comment(linker, "/include:_cReach")
#pragma comment(linker, "/include:_cDoms")
#pragma comment(linker, "/include:_cLiveness")
#pragma comment(linker, "/include:_cCVarSet")
#pragma comment(linker, "/include:_cLoop")
#pragma comment(linker, "/include:_cLoops")
#pragma comment(linker, "/include:_cLoopsA")
#pragma comment(linker, "/include:_cTreeFlags")
#pragma comment(linker, "/include:_cVN")

// Functions which call the c* functions getting Compiler* using `JitTls:_:GetCompiler()`
#pragma comment(linker, "/include:_dBlock")
#pragma comment(linker, "/include:_dBlocks")
#pragma comment(linker, "/include:_dBlocksV")
#pragma comment(linker, "/include:_dStmt")
#pragma comment(linker, "/include:_dTree")
#pragma comment(linker, "/include:_dTreeLIR")
#pragma comment(linker, "/include:_dTreeRange")
#pragma comment(linker, "/include:_dTrees")
#pragma comment(linker, "/include:_dEH")
#pragma comment(linker, "/include:_dVar")
#pragma comment(linker, "/include:_dVarDsc")
#pragma comment(linker, "/include:_dVars")
#pragma comment(linker, "/include:_dVarsFinal")
#pragma comment(linker, "/include:_dBlockPreds")
#pragma comment(linker, "/include:_dBlockSuccs")
#pragma comment(linker, "/include:_dReach")
#pragma comment(linker, "/include:_dDoms")
#pragma comment(linker, "/include:_dLiveness")
#pragma comment(linker, "/include:_dCVarSet")
#pragma comment(linker, "/include:_dLoops")
#pragma comment(linker, "/include:_dLoopsA")
#pragma comment(linker, "/include:_dLoop")
#pragma comment(linker, "/include:_dTreeFlags")
#pragma comment(linker, "/include:_dVN")

// Functions which don't require a Compiler*
#pragma comment(linker, "/include:_dRegMask")
#pragma comment(linker, "/include:_dBlockList")

// Functions which search for objects in the IR
#pragma comment(linker, "/include:_dFindTreeInTree")
#pragma comment(linker, "/include:_dFindTree")
#pragma comment(linker, "/include:_dFindStmt")
#pragma comment(linker, "/include:_dFindBlock")
#pragma comment(linker, "/include:_dFindLoop")

#endif // HOST_*

#endif // _MSC_VER

JITDBGAPI void __cdecl cBlock(Compiler* comp, BasicBlock* block)
{
    static unsigned sequenceNumber = 0; // separate calls with a number to indicate this function has been called
    printf("===================================================================== *Block %u\n", sequenceNumber++);
    comp->fgTableDispBasicBlock(block);
}

JITDBGAPI void __cdecl cBlocks(Compiler* comp)
{
    static unsigned sequenceNumber = 0; // separate calls with a number to indicate this function has been called
    printf("===================================================================== *Blocks %u\n", sequenceNumber++);
    comp->fgDispBasicBlocks();
}

JITDBGAPI void __cdecl cBlocksV(Compiler* comp)
{
    static unsigned sequenceNumber = 0; // separate calls with a number to indicate this function has been called
    printf("===================================================================== *BlocksV %u\n", sequenceNumber++);
    comp->fgDispBasicBlocks(true);
}

JITDBGAPI void __cdecl cStmt(Compiler* comp, Statement* statement)
{
    static unsigned sequenceNumber = 0; // separate calls with a number to indicate this function has been called
    printf("===================================================================== *Stmt %u\n", sequenceNumber++);
    comp->gtDispStmt(statement, ">>>");
}

JITDBGAPI void __cdecl cTree(Compiler* comp, GenTree* tree)
{
    static unsigned sequenceNumber = 0; // separate calls with a number to indicate this function has been called
    printf("===================================================================== *Tree %u\n", sequenceNumber++);
    comp->gtDispTree(tree, nullptr, ">>>");
}

JITDBGAPI void __cdecl cTreeLIR(Compiler* comp, GenTree* tree)
{
    static unsigned sequenceNumber = 0; // separate calls with a number to indicate this function has been called
    printf("===================================================================== *TreeLIR %u\n", sequenceNumber++);
    comp->gtDispLIRNode(tree);
}

JITDBGAPI void __cdecl cTreeRange(Compiler* comp, GenTree* first, GenTree* last)
{
    static unsigned sequenceNumber = 0; // separate calls with a number to indicate this function has been called
    printf("===================================================================== *TreeRange %u\n", sequenceNumber++);
    GenTree* cur = first;
    while (true)
    {
        comp->gtDispLIRNode(cur);
        if (cur == last)
            break;

        cur = cur->gtNext;
    }
}

JITDBGAPI void __cdecl cTrees(Compiler* comp)
{
    static unsigned sequenceNumber = 0; // separate calls with a number to indicate this function has been called
    printf("===================================================================== *Trees %u\n", sequenceNumber++);
    comp->fgDumpTrees(comp->fgFirstBB, nullptr);
}

JITDBGAPI void __cdecl cEH(Compiler* comp)
{
    static unsigned sequenceNumber = 0; // separate calls with a number to indicate this function has been called
    printf("===================================================================== *EH %u\n", sequenceNumber++);
    comp->fgDispHandlerTab();
}

JITDBGAPI void __cdecl cVar(Compiler* comp, unsigned lclNum)
{
    static unsigned sequenceNumber = 0; // separate calls with a number to indicate this function has been called
    printf("===================================================================== *Var %u\n", sequenceNumber++);
    comp->lvaDumpEntry(lclNum, Compiler::FINAL_FRAME_LAYOUT);
}

JITDBGAPI void __cdecl cVarDsc(Compiler* comp, LclVarDsc* varDsc)
{
    static unsigned sequenceNumber = 0; // separate calls with a number to indicate this function has been called
    printf("===================================================================== *VarDsc %u\n", sequenceNumber++);
    unsigned lclNum = comp->lvaGetLclNum(varDsc);
    comp->lvaDumpEntry(lclNum, Compiler::FINAL_FRAME_LAYOUT);
}

JITDBGAPI void __cdecl cVars(Compiler* comp)
{
    static unsigned sequenceNumber = 0; // separate calls with a number to indicate this function has been called
    printf("===================================================================== *Vars %u\n", sequenceNumber++);
    comp->lvaTableDump();
}

JITDBGAPI void __cdecl cVarsFinal(Compiler* comp)
{
    static unsigned sequenceNumber = 0; // separate calls with a number to indicate this function has been called
    printf("===================================================================== *Vars %u\n", sequenceNumber++);
    comp->lvaTableDump(Compiler::FINAL_FRAME_LAYOUT);
}

JITDBGAPI void __cdecl cBlockPreds(Compiler* comp, BasicBlock* block)
{
    static unsigned sequenceNumber = 0; // separate calls with a number to indicate this function has been called
    printf("===================================================================== *BlockPreds %u\n", sequenceNumber++);
    block->dspPreds();
}

JITDBGAPI void __cdecl cBlockSuccs(Compiler* comp, BasicBlock* block)
{
    static unsigned sequenceNumber = 0; // separate calls with a number to indicate this function has been called
    printf("===================================================================== *BlockSuccs %u\n", sequenceNumber++);
    block->dspSuccs(comp);
}

JITDBGAPI void __cdecl cReach(Compiler* comp)
{
    static unsigned sequenceNumber = 0; // separate calls with a number to indicate this function has been called
    printf("===================================================================== *Reach %u\n", sequenceNumber++);
    if (comp->m_reachabilitySets != nullptr)
    {
        comp->m_reachabilitySets->Dump();
    }
    else
    {
        printf("  Not computed\n");
    }
}

JITDBGAPI void __cdecl cDoms(Compiler* comp)
{
    static unsigned sequenceNumber = 0; // separate calls with a number to indicate this function has been called
    printf("===================================================================== *Doms %u\n", sequenceNumber++);
    if (comp->m_domTree != nullptr)
    {
        comp->m_domTree->Dump();
    }
    else
    {
        printf("  Not computed\n");
    }
}

JITDBGAPI void __cdecl cLiveness(Compiler* comp)
{
    static unsigned sequenceNumber = 0; // separate calls with a number to indicate this function has been called
    printf("===================================================================== *Liveness %u\n", sequenceNumber++);
    comp->fgDispBBLiveness();
}

JITDBGAPI void __cdecl cCVarSet(Compiler* comp, VARSET_VALARG_TP vars)
{
    static unsigned sequenceNumber = 0; // separate calls with a number to indicate this function has been called
    printf("===================================================================== *CVarSet %u\n", sequenceNumber++);
    dumpConvertedVarSet(comp, vars);
    printf("\n"); // dumpConvertedVarSet() doesn't emit a trailing newline
}

JITDBGAPI void __cdecl cLoops(Compiler* comp)
{
    static unsigned sequenceNumber = 0; // separate calls with a number to indicate this function has been called
    printf("===================================================================== *NewLoops %u\n", sequenceNumber++);
    FlowGraphNaturalLoops::Dump(comp->m_loops);
}

JITDBGAPI void __cdecl cLoopsA(Compiler* comp, FlowGraphNaturalLoops* loops)
{
    static unsigned sequenceNumber = 0; // separate calls with a number to indicate this function has been called
    printf("===================================================================== *NewLoopsA %u\n", sequenceNumber++);
    FlowGraphNaturalLoops::Dump(loops);
}

JITDBGAPI void __cdecl cLoop(Compiler* comp, FlowGraphNaturalLoop* loop)
{
    static unsigned sequenceNumber = 0; // separate calls with a number to indicate this function has been called
    printf("===================================================================== *NewLoop %u\n", sequenceNumber++);
    FlowGraphNaturalLoop::Dump(loop);
}

JITDBGAPI void __cdecl cTreeFlags(Compiler* comp, GenTree* tree)
{
    static unsigned sequenceNumber = 0; // separate calls with a number to indicate this function has been called
    printf("===================================================================== *TreeFlags %u\n", sequenceNumber++);

    int chars = 0;

    if (tree->gtFlags != 0)
    {
        chars += printf("flags=");

        // Node flags
        CLANG_FORMAT_COMMENT_ANCHOR;

#if defined(DEBUG)
        if (tree->gtDebugFlags & GTF_DEBUG_NODE_LARGE)
        {
            chars += printf("[NODE_LARGE]");
        }
        if (tree->gtDebugFlags & GTF_DEBUG_NODE_SMALL)
        {
            chars += printf("[NODE_SMALL]");
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
            case GT_LCL_FLD:
            case GT_LCL_ADDR:
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
                if (tree->gtFlags & GTF_VAR_MOREUSES)
                {
                    chars += printf("[VAR_MOREUSES]");
                }
                if (!comp->lvaGetDesc(tree->AsLclVarCommon())->lvPromoted)
                {
                    if (tree->gtFlags & GTF_VAR_DEATH)
                    {
                        chars += printf("[VAR_DEATH]");
                    }
                }
                else
                {
                    if (tree->gtFlags & GTF_VAR_FIELD_DEATH0)
                    {
                        chars += printf("[VAR_FIELD_DEATH0]");
                    }
                }
                if (tree->gtFlags & GTF_VAR_FIELD_DEATH1)
                {
                    chars += printf("[VAR_FIELD_DEATH1]");
                }
                if (tree->gtFlags & GTF_VAR_FIELD_DEATH2)
                {
                    chars += printf("[VAR_FIELD_DEATH2]");
                }
                if (tree->gtFlags & GTF_VAR_FIELD_DEATH3)
                {
                    chars += printf("[VAR_FIELD_DEATH3]");
                }
                if (tree->gtFlags & GTF_VAR_EXPLICIT_INIT)
                {
                    chars += printf("[VAR_EXPLICIT_INIT]");
                }
#if defined(DEBUG)
                if (tree->gtDebugFlags & GTF_DEBUG_VAR_CSE_REF)
                {
                    chars += printf("[VAR_CSE_REF]");
                }
#endif
                break;

            case GT_NO_OP:
                break;

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
                if (tree->gtFlags & GTF_IND_TGT_NOT_HEAP)
                {
                    chars += printf("[IND_TGT_NOT_HEAP]");
                }
                if (tree->gtFlags & GTF_IND_TGT_HEAP)
                {
                    chars += printf("[IND_TGT_HEAP]");
                }
                if (tree->gtFlags & GTF_IND_REQ_ADDR_IN_REG)
                {
                    chars += printf("[IND_REQ_ADDR_IN_REG]");
                }
                if (tree->gtFlags & GTF_IND_UNALIGNED)
                {
                    chars += printf("[IND_UNALIGNED]");
                }
                if (tree->gtFlags & GTF_IND_INVARIANT)
                {
                    chars += printf("[IND_INVARIANT]");
                }
                if (tree->gtFlags & GTF_IND_NONNULL)
                {
                    chars += printf("[IND_NONNULL]");
                }
                if (tree->gtFlags & GTF_IND_INITCLASS)
                {
                    chars += printf("[IND_INITCLASS]");
                }
                break;

            case GT_MUL:
#if !defined(TARGET_64BIT)
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

                if (tree->gtFlags & GTF_BOX_CLONED)
                {
                    chars += printf("[BOX_CLONED]");
                }
                break;

            case GT_ARR_ADDR:
                if (tree->gtFlags & GTF_ARR_ADDR_NONNULL)
                {
                    chars += printf("[ARR_ADDR_NONNULL]");
                }
                break;

            case GT_CNS_INT:
            {
                GenTreeFlags handleKind = (tree->gtFlags & GTF_ICON_HDL_MASK);
                if (handleKind != 0)
                {
                    chars += printf("[%s]", GenTree::gtGetHandleKindString(handleKind));
                }
            }
            break;

            case GT_BLK:
            case GT_STORE_BLK:
            case GT_STORE_DYN_BLK:

                if (tree->gtFlags & GTF_IND_VOLATILE)
                {
                    chars += printf("[IND_VOLATILE]");
                }
                if (tree->gtFlags & GTF_IND_UNALIGNED)
                {
                    chars += printf("[IND_UNALIGNED]");
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

                    if (call->IsVirtualStub())
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
                    if (call->gtCallMoreFlags & GTF_CALL_M_TAILCALL_VIA_JIT_HELPER)
                    {
                        chars += printf("[CALL_M_TAILCALL_VIA_JIT_HELPER]");
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

                    if (call->IsFatPointerCandidate())
                    {
                        chars += printf("[CALL_FAT_POINTER_CANDIDATE]");
                    }

                    if (call->IsGuarded())
                    {
                        chars += printf("[CALL_GUARDED]");
                    }

                    if (call->gtCallDebugFlags & GTF_CALL_MD_STRESS_TAILCALL)
                    {
                        chars += printf("[CALL_MD_STRESS_TAILCALL]");
                    }

                    if (call->gtCallDebugFlags & GTF_CALL_MD_DEVIRTUALIZED)
                    {
                        chars += printf("[CALL_MD_DEVIRTUALIZED]");
                    }

                    if (call->gtCallDebugFlags & GTF_CALL_MD_GUARDED)
                    {
                        chars += printf("[CALL_MD_GUARDED]");
                    }

                    if (call->gtCallDebugFlags & GTF_CALL_MD_UNBOXED)
                    {
                        chars += printf("[CALL_MD_UNBOXED]");
                    }
                }
                break;
            default:

            {
                GenTreeFlags flags = (tree->gtFlags & (~(GTF_COMMON_MASK | GTF_OVERFLOW)));
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
            if (tree->OperIsIndirOrArrMetaData())
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
}

JITDBGAPI void __cdecl cVN(Compiler* comp, ValueNum vn)
{
    static unsigned sequenceNumber = 0; // separate calls with a number to indicate this function has been called
    printf("===================================================================== *VN %u\n", sequenceNumber++);
    comp->vnPrint(vn, 1);
    printf("\n");
}

JITDBGAPI void __cdecl dBlock(BasicBlock* block)
{
    cBlock(JitTls::GetCompiler(), block);
}

JITDBGAPI void __cdecl dBlocks()
{
    cBlocks(JitTls::GetCompiler());
}

JITDBGAPI void __cdecl dBlocksV()
{
    cBlocksV(JitTls::GetCompiler());
}

JITDBGAPI void __cdecl dStmt(Statement* statement)
{
    cStmt(JitTls::GetCompiler(), statement);
}

JITDBGAPI void __cdecl dTree(GenTree* tree)
{
    cTree(JitTls::GetCompiler(), tree);
}

JITDBGAPI void __cdecl dTreeLIR(GenTree* tree)
{
    cTreeLIR(JitTls::GetCompiler(), tree);
}

JITDBGAPI void __cdecl dTreeRange(GenTree* first, GenTree* last)
{
    cTreeRange(JitTls::GetCompiler(), first, last);
}

JITDBGAPI void __cdecl dTrees()
{
    cTrees(JitTls::GetCompiler());
}

JITDBGAPI void __cdecl dEH()
{
    cEH(JitTls::GetCompiler());
}

JITDBGAPI void __cdecl dVar(unsigned lclNum)
{
    cVar(JitTls::GetCompiler(), lclNum);
}

JITDBGAPI void __cdecl dVarDsc(LclVarDsc* varDsc)
{
    cVarDsc(JitTls::GetCompiler(), varDsc);
}

JITDBGAPI void __cdecl dVars()
{
    cVars(JitTls::GetCompiler());
}

JITDBGAPI void __cdecl dVarsFinal()
{
    cVarsFinal(JitTls::GetCompiler());
}

JITDBGAPI void __cdecl dBlockPreds(BasicBlock* block)
{
    cBlockPreds(JitTls::GetCompiler(), block);
}

JITDBGAPI void __cdecl dBlockSuccs(BasicBlock* block)
{
    cBlockSuccs(JitTls::GetCompiler(), block);
}

JITDBGAPI void __cdecl dReach()
{
    cReach(JitTls::GetCompiler());
}

JITDBGAPI void __cdecl dDoms()
{
    cDoms(JitTls::GetCompiler());
}

JITDBGAPI void __cdecl dLiveness()
{
    cLiveness(JitTls::GetCompiler());
}

JITDBGAPI void __cdecl dCVarSet(VARSET_VALARG_TP vars)
{
    cCVarSet(JitTls::GetCompiler(), vars);
}

JITDBGAPI void __cdecl dLoops()
{
    cLoops(JitTls::GetCompiler());
}

JITDBGAPI void __cdecl dLoopsA(FlowGraphNaturalLoops* loops)
{
    cLoopsA(JitTls::GetCompiler(), loops);
}

JITDBGAPI void __cdecl dLoop(FlowGraphNaturalLoop* loop)
{
    cLoop(JitTls::GetCompiler(), loop);
}

JITDBGAPI void __cdecl dTreeFlags(GenTree* tree)
{
    cTreeFlags(JitTls::GetCompiler(), tree);
}

JITDBGAPI void __cdecl dVN(ValueNum vn)
{
    cVN(JitTls::GetCompiler(), vn);
}

JITDBGAPI void __cdecl dRegMask(regMaskTP mask)
{
    static unsigned sequenceNumber = 0; // separate calls with a number to indicate this function has been called
    printf("===================================================================== dRegMask %u\n", sequenceNumber++);
    dspRegMask(mask);
    printf("\n"); // dspRegMask() doesn't emit a trailing newline
}

JITDBGAPI void __cdecl dBlockList(BasicBlockList* list)
{
    static unsigned sequenceNumber = 0; // separate calls with a number to indicate this function has been called
    printf("===================================================================== dBlockList %u\n", sequenceNumber++);
    while (list != nullptr)
    {
        printf(FMT_BB " ", list->block->bbNum);
        list = list->next;
    }
    printf("\n");
}

// Global variables available in debug mode.  That are set by debug APIs for finding
// Trees, Stmts, and/or Blocks using id or bbNum.
// That can be used in watch window or as a way to get address of fields for data breakpoints.

GenTree*              dbTree;
Statement*            dbStmt;
BasicBlock*           dbTreeBlock;
BasicBlock*           dbBlock;
FlowGraphNaturalLoop* dbLoop;

// Debug APIs for finding Trees, Stmts, and/or Blocks.
// As a side effect, they set the debug variables above.

JITDBGAPI GenTree* __cdecl dFindTreeInTree(GenTree* tree, unsigned id)
{
    if (tree == nullptr)
    {
        return nullptr;
    }

    if (tree->gtTreeID == id)
    {
        dbTree = tree;
        return tree;
    }

    GenTree* child = nullptr;
    tree->VisitOperands([&child, id](GenTree* operand) -> GenTree::VisitResult {
        child = dFindTreeInTree(child, id);
        return (child != nullptr) ? GenTree::VisitResult::Abort : GenTree::VisitResult::Continue;
    });

    return child;
}

JITDBGAPI GenTree* __cdecl dFindTree(unsigned id)
{
    Compiler* comp = JitTls::GetCompiler();

    dbTreeBlock = nullptr;
    dbTree      = nullptr;

    for (BasicBlock* const block : comp->Blocks())
    {
        for (Statement* const stmt : block->Statements())
        {
            GenTree* const tree = dFindTreeInTree(stmt->GetRootNode(), id);
            if (tree != nullptr)
            {
                dbTreeBlock = block;
                dbTree      = tree;
                return tree;
            }
        }
    }

    return nullptr;
}

JITDBGAPI Statement* __cdecl dFindStmt(unsigned id)
{
    Compiler* comp = JitTls::GetCompiler();

    dbStmt = nullptr;

    unsigned stmtId = 0;
    for (BasicBlock* const block : comp->Blocks())
    {
        for (Statement* const stmt : block->Statements())
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

JITDBGAPI BasicBlock* __cdecl dFindBlock(unsigned bbNum)
{
    Compiler* comp = JitTls::GetCompiler();

    dbBlock = nullptr;

    for (BasicBlock* const block : comp->Blocks())
    {
        if (block->bbNum == bbNum)
        {
            dbBlock = block;
            return block;
        }
    }

    return nullptr;
}

JITDBGAPI FlowGraphNaturalLoop* __cdecl dFindLoop(unsigned index)
{
    Compiler* comp = JitTls::GetCompiler();
    dbLoop         = nullptr;

    if ((comp->m_loops == nullptr) || (index >= comp->m_loops->NumLoops()))
    {
        printf("Index %u out of range\n", index);
        return nullptr;
    }

    dbLoop = comp->m_loops->GetLoopByIndex(index);
    return dbLoop;
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

//------------------------------------------------------------------------
// lvaIsOSRLocal: check if this local var is one that requires special
//     treatment for OSR compilations.
//
// Arguments:
//    varNum     - variable of interest
//
// Return Value:
//    true       - this is an OSR compile and this local requires special treatment
//    false      - not an OSR compile, or not an interesting local for OSR

bool Compiler::lvaIsOSRLocal(unsigned varNum)
{
    LclVarDsc* const varDsc = lvaGetDesc(varNum);

#ifdef DEBUG
    if (opts.IsOSR())
    {
        if (varDsc->lvIsOSRLocal)
        {
            // Sanity check for promoted fields of OSR locals.
            //
            if (varNum >= info.compLocalsCount)
            {
                assert(varDsc->lvIsStructField);
                assert(varDsc->lvParentLcl < info.compLocalsCount);
            }
        }
    }
    else
    {
        assert(!varDsc->lvIsOSRLocal);
    }
#endif

    return varDsc->lvIsOSRLocal;
}

//------------------------------------------------------------------------------
// gtTypeForNullCheck: helper to get the most optimal and correct type for nullcheck
//
// Arguments:
//    tree - the node for nullcheck;
//
var_types Compiler::gtTypeForNullCheck(GenTree* tree)
{
    static const var_types s_typesBySize[] = {TYP_UNDEF, TYP_BYTE,  TYP_SHORT, TYP_UNDEF, TYP_INT,
                                              TYP_UNDEF, TYP_UNDEF, TYP_UNDEF, TYP_LONG};

    if (!varTypeIsStruct(tree))
    {
#if defined(TARGET_XARCH)
        // Just an optimization for XARCH - smaller mov
        if (genTypeSize(tree) == 8)
        {
            return TYP_INT;
        }
#endif

        assert((genTypeSize(tree) < ARRAY_SIZE(s_typesBySize)) && (s_typesBySize[genTypeSize(tree)] != TYP_UNDEF));
        return s_typesBySize[genTypeSize(tree)];
    }
    // for the rest: probe a single byte to avoid potential AVEs
    return TYP_BYTE;
}

//------------------------------------------------------------------------------
// gtChangeOperToNullCheck: helper to change tree oper to a NULLCHECK.
//
// Arguments:
//    tree  - the node to change;
//    block - basic block of the node.
//
// Notes:
//    the function should not be called after lowering for platforms that do not support
//    emitting NULLCHECK nodes, like arm32. Use `Lowering::TransformUnusedIndirection`
//    that handles it and calls this function when appropriate.
//
void Compiler::gtChangeOperToNullCheck(GenTree* tree, BasicBlock* block)
{
    assert(tree->OperIs(GT_IND, GT_BLK));
    tree->ChangeOper(GT_NULLCHECK);
    tree->ChangeType(gtTypeForNullCheck(tree));
    tree->SetIndirExceptionFlags(this);
    block->SetFlags(BBF_HAS_NULLCHECK);
    optMethodFlags |= OMF_HAS_NULLCHECK;
}

#if defined(DEBUG)
//------------------------------------------------------------------------------
// devirtualizationDetailToString: describe the detailed devirtualization reason
//
// Arguments:
//    detail - detail to describe
//
// Returns:
//    descriptive string
//
const char* Compiler::devirtualizationDetailToString(CORINFO_DEVIRTUALIZATION_DETAIL detail)
{
    switch (detail)
    {
        case CORINFO_DEVIRTUALIZATION_UNKNOWN:
            return "unknown";
        case CORINFO_DEVIRTUALIZATION_SUCCESS:
            return "success";
        case CORINFO_DEVIRTUALIZATION_FAILED_CANON:
            return "object class was canonical";
        case CORINFO_DEVIRTUALIZATION_FAILED_COM:
            return "object class was com";
        case CORINFO_DEVIRTUALIZATION_FAILED_CAST:
            return "object class could not be cast to interface class";
        case CORINFO_DEVIRTUALIZATION_FAILED_LOOKUP:
            return "interface method could not be found";
        case CORINFO_DEVIRTUALIZATION_FAILED_DIM:
            return "interface method was default interface method";
        case CORINFO_DEVIRTUALIZATION_FAILED_SUBCLASS:
            return "object not subclass of base class";
        case CORINFO_DEVIRTUALIZATION_FAILED_SLOT:
            return "virtual method installed via explicit override";
        case CORINFO_DEVIRTUALIZATION_FAILED_BUBBLE:
            return "devirtualization crossed version bubble";
        case CORINFO_DEVIRTUALIZATION_MULTIPLE_IMPL:
            return "object class has multiple implementations of interface";
        case CORINFO_DEVIRTUALIZATION_FAILED_BUBBLE_CLASS_DECL:
            return "decl method is defined on class and decl method not in version bubble, and decl method not in "
                   "type closest to version bubble";
        case CORINFO_DEVIRTUALIZATION_FAILED_BUBBLE_INTERFACE_DECL:
            return "decl method is defined on interface and not in version bubble, and implementation type not "
                   "entirely defined in bubble";
        case CORINFO_DEVIRTUALIZATION_FAILED_BUBBLE_IMPL:
            return "object class not defined within version bubble";
        case CORINFO_DEVIRTUALIZATION_FAILED_BUBBLE_IMPL_NOT_REFERENCEABLE:
            return "object class cannot be referenced from R2R code due to missing tokens";
        case CORINFO_DEVIRTUALIZATION_FAILED_DUPLICATE_INTERFACE:
            return "crossgen2 virtual method algorithm and runtime algorithm differ in the presence of duplicate "
                   "interface implementations";
        case CORINFO_DEVIRTUALIZATION_FAILED_DECL_NOT_REPRESENTABLE:
            return "Decl method cannot be represented in R2R image";
        case CORINFO_DEVIRTUALIZATION_FAILED_TYPE_EQUIVALENCE:
            return "Support for type equivalence in devirtualization is not yet implemented in crossgen2";
        default:
            return "undefined";
    }
}

//------------------------------------------------------------------------------
// printfAlloc: printf a string and allocate the result in CMK_DebugOnly
// memory.
//
// Arguments:
//    format - Format string
//
// Returns:
//    Allocated string.
//
const char* Compiler::printfAlloc(const char* format, ...)
{
    char    str[512];
    va_list args;
    va_start(args, format);
    int result = vsprintf_s(str, ArrLen(str), format, args);
    va_end(args);
    assert((result >= 0) && ((unsigned)result < ArrLen(str)));

    char* resultStr = new (this, CMK_DebugOnly) char[result + 1];
    memcpy(resultStr, str, (unsigned)result + 1);
    return resultStr;
}

#endif // defined(DEBUG)

#if TRACK_ENREG_STATS
Compiler::EnregisterStats Compiler::s_enregisterStats;

void Compiler::EnregisterStats::RecordLocal(const LclVarDsc* varDsc)
{
    m_totalNumberOfVars++;
    if (varDsc->TypeGet() == TYP_STRUCT)
    {
        m_totalNumberOfStructVars++;
    }
    if (!varDsc->lvDoNotEnregister)
    {
        m_totalNumberOfEnregVars++;
        if (varDsc->TypeGet() == TYP_STRUCT)
        {
            m_totalNumberOfStructEnregVars++;
        }
    }
    else
    {
        switch (varDsc->GetDoNotEnregReason())
        {
            case DoNotEnregisterReason::AddrExposed:
                m_addrExposed++;
                break;
            case DoNotEnregisterReason::HiddenBufferStructArg:
                m_hiddenStructArg++;
                break;
            case DoNotEnregisterReason::DontEnregStructs:
                m_dontEnregStructs++;
                break;
            case DoNotEnregisterReason::NotRegSizeStruct:
                m_notRegSizeStruct++;
                break;
            case DoNotEnregisterReason::LocalField:
                m_localField++;
                break;
            case DoNotEnregisterReason::VMNeedsStackAddr:
                m_VMNeedsStackAddr++;
                break;
            case DoNotEnregisterReason::LiveInOutOfHandler:
                m_liveInOutHndlr++;
                break;
            case DoNotEnregisterReason::BlockOp:
                m_blockOp++;
                break;
            case DoNotEnregisterReason::IsStructArg:
                m_structArg++;
                break;
            case DoNotEnregisterReason::DepField:
                m_depField++;
                break;
            case DoNotEnregisterReason::NoRegVars:
                m_noRegVars++;
                break;
            case DoNotEnregisterReason::MinOptsGC:
                m_minOptsGC++;
                break;
#if !defined(TARGET_64BIT)
            case DoNotEnregisterReason::LongParamField:
                m_longParamField++;
                break;
#endif
#ifdef JIT32_GCENCODER
            case DoNotEnregisterReason::PinningRef:
                m_PinningRef++;
                break;
#endif
            case DoNotEnregisterReason::LclAddrNode:
                m_lclAddrNode++;
                break;

            case DoNotEnregisterReason::CastTakesAddr:
                m_castTakesAddr++;
                break;

            case DoNotEnregisterReason::StoreBlkSrc:
                m_storeBlkSrc++;
                break;

            case DoNotEnregisterReason::SwizzleArg:
                m_swizzleArg++;
                break;

            case DoNotEnregisterReason::BlockOpRet:
                m_blockOpRet++;
                break;

            case DoNotEnregisterReason::ReturnSpCheck:
                m_returnSpCheck++;
                break;

            case DoNotEnregisterReason::CallSpCheck:
                m_callSpCheck++;
                break;

            case DoNotEnregisterReason::SimdUserForcesDep:
                m_simdUserForcesDep++;
                break;

            default:
                unreached();
                break;
        }

        if (varDsc->GetDoNotEnregReason() == DoNotEnregisterReason::AddrExposed)
        {
            // We can't `assert(IsAddressExposed())` because `fgAdjustForAddressExposedOrWrittenThis`
            // does not clear `m_doNotEnregReason` on `this`.
            switch (varDsc->GetAddrExposedReason())
            {
                case AddressExposedReason::PARENT_EXPOSED:
                    m_parentExposed++;
                    break;

                case AddressExposedReason::TOO_CONSERVATIVE:
                    m_tooConservative++;
                    break;

                case AddressExposedReason::ESCAPE_ADDRESS:
                    m_escapeAddress++;
                    break;

                case AddressExposedReason::WIDE_INDIR:
                    m_wideIndir++;
                    break;

                case AddressExposedReason::OSR_EXPOSED:
                    m_osrExposed++;
                    break;

                case AddressExposedReason::STRESS_LCL_FLD:
                    m_stressLclFld++;
                    break;

                case AddressExposedReason::DISPATCH_RET_BUF:
                    m_dispatchRetBuf++;
                    break;

                case AddressExposedReason::STRESS_POISON_IMPLICIT_BYREFS:
                    m_stressPoisonImplicitByrefs++;
                    break;

                case AddressExposedReason::EXTERNALLY_VISIBLE_IMPLICITLY:
                    m_externallyVisibleImplicitly++;
                    break;

                default:
                    unreached();
                    break;
            }
        }
    }
}

void Compiler::EnregisterStats::Dump(FILE* fout) const
{
    const unsigned totalNumberOfNotStructVars =
        s_enregisterStats.m_totalNumberOfVars - s_enregisterStats.m_totalNumberOfStructVars;
    const unsigned totalNumberOfNotStructEnregVars =
        s_enregisterStats.m_totalNumberOfEnregVars - s_enregisterStats.m_totalNumberOfStructEnregVars;
    const unsigned notEnreg = s_enregisterStats.m_totalNumberOfVars - s_enregisterStats.m_totalNumberOfEnregVars;

    fprintf(fout, "\nLocals enregistration statistics:\n");
    if (m_totalNumberOfVars == 0)
    {
        fprintf(fout, "No locals to report.\n");
        return;
    }
    fprintf(fout, "total number of locals: %d, number of enregistered: %d, notEnreg: %d, ratio: %.2f\n",
            m_totalNumberOfVars, m_totalNumberOfEnregVars, m_totalNumberOfVars - m_totalNumberOfEnregVars,
            (float)m_totalNumberOfEnregVars / m_totalNumberOfVars);

    if (m_totalNumberOfStructVars != 0)
    {
        fprintf(fout, "total number of struct locals: %d, number of enregistered: %d, notEnreg: %d, ratio: %.2f\n",
                m_totalNumberOfStructVars, m_totalNumberOfStructEnregVars,
                m_totalNumberOfStructVars - m_totalNumberOfStructEnregVars,
                (float)m_totalNumberOfStructEnregVars / m_totalNumberOfStructVars);
    }

    const unsigned numberOfPrimitiveLocals = totalNumberOfNotStructVars - totalNumberOfNotStructEnregVars;
    if (numberOfPrimitiveLocals != 0)
    {
        fprintf(fout, "total number of primitive locals: %d, number of enregistered: %d, notEnreg: %d, ratio: %.2f\n",
                totalNumberOfNotStructVars, totalNumberOfNotStructEnregVars, numberOfPrimitiveLocals,
                (float)totalNumberOfNotStructEnregVars / totalNumberOfNotStructVars);
    }

    if (notEnreg == 0)
    {
        fprintf(fout, "All locals are enregistered.\n");
        return;
    }

#define PRINT_STATS(stat, total)                                                                                       \
    if (stat != 0)                                                                                                     \
    {                                                                                                                  \
        fprintf(fout, #stat " %d, ratio: %.2f\n", stat, (float)stat / total);                                          \
    }

    PRINT_STATS(m_addrExposed, notEnreg);
    PRINT_STATS(m_hiddenStructArg, notEnreg);
    PRINT_STATS(m_dontEnregStructs, notEnreg);
    PRINT_STATS(m_notRegSizeStruct, notEnreg);
    PRINT_STATS(m_localField, notEnreg);
    PRINT_STATS(m_VMNeedsStackAddr, notEnreg);
    PRINT_STATS(m_liveInOutHndlr, notEnreg);
    PRINT_STATS(m_blockOp, notEnreg);
    PRINT_STATS(m_structArg, notEnreg);
    PRINT_STATS(m_depField, notEnreg);
    PRINT_STATS(m_noRegVars, notEnreg);
    PRINT_STATS(m_minOptsGC, notEnreg);
#if !defined(TARGET_64BIT)
    PRINT_STATS(m_longParamField, notEnreg);
#endif // !TARGET_64BIT
#ifdef JIT32_GCENCODER
    PRINT_STATS(m_PinningRef, notEnreg);
#endif // JIT32_GCENCODER
    PRINT_STATS(m_lclAddrNode, notEnreg);
    PRINT_STATS(m_castTakesAddr, notEnreg);
    PRINT_STATS(m_storeBlkSrc, notEnreg);
    PRINT_STATS(m_swizzleArg, notEnreg);
    PRINT_STATS(m_blockOpRet, notEnreg);
    PRINT_STATS(m_returnSpCheck, notEnreg);
    PRINT_STATS(m_callSpCheck, notEnreg);
    PRINT_STATS(m_simdUserForcesDep, notEnreg);

    fprintf(fout, "\nAddr exposed details:\n");
    if (m_addrExposed == 0)
    {
        fprintf(fout, "\nNo address exposed locals to report.\n");
        return;
    }

    PRINT_STATS(m_parentExposed, m_addrExposed);
    PRINT_STATS(m_tooConservative, m_addrExposed);
    PRINT_STATS(m_escapeAddress, m_addrExposed);
    PRINT_STATS(m_wideIndir, m_addrExposed);
    PRINT_STATS(m_osrExposed, m_addrExposed);
    PRINT_STATS(m_stressLclFld, m_addrExposed);
    PRINT_STATS(m_dispatchRetBuf, m_addrExposed);
    PRINT_STATS(m_stressPoisonImplicitByrefs, m_addrExposed);
    PRINT_STATS(m_externallyVisibleImplicitly, m_addrExposed);
}
#endif // TRACK_ENREG_STATS
