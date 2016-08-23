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

#ifndef LEGACY_BACKEND
#include "lower.h"
#endif // !LEGACY_BACKEND

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
#ifdef DEBUGGING_SUPPORT

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

#endif
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
Histogram genTreeNcntHist(HostAllocator::getHostAllocator(), genTreeNcntHistBuckets);

unsigned  genTreeNsizHistBuckets[] = {1000, 5000, 10000, 50000, 100000, 500000, 1000000, 0};
Histogram genTreeNsizHist(HostAllocator::getHostAllocator(), genTreeNsizHistBuckets);
#endif // MEASURE_NODE_SIZE

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
Histogram argCntTable(HostAllocator::getHostAllocator(), argCntBuckets);

unsigned  argDWordCntBuckets[] = {0, 1, 2, 3, 4, 5, 6, 10, 0};
Histogram argDWordCntTable(HostAllocator::getHostAllocator(), argDWordCntBuckets);

unsigned  argDWordLngCntBuckets[] = {0, 1, 2, 3, 4, 5, 6, 10, 0};
Histogram argDWordLngCntTable(HostAllocator::getHostAllocator(), argDWordLngCntBuckets);

unsigned  argTempsCntBuckets[] = {0, 1, 2, 3, 4, 5, 6, 10, 0};
Histogram argTempsCntTable(HostAllocator::getHostAllocator(), argTempsCntBuckets);

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
Histogram bbCntTable(HostAllocator::getHostAllocator(), bbCntBuckets);

/* Histogram for the IL opcode size of methods with a single basic block */

unsigned  bbSizeBuckets[] = {1, 4, 8, 16, 32, 64, 128, 256, 512, 1024, 2048, 0};
Histogram bbOneBBSizeTable(HostAllocator::getHostAllocator(), bbSizeBuckets);

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
Histogram loopCountTable(HostAllocator::getHostAllocator(), loopCountBuckets);

/* Histogram for number of loop exits */

unsigned  loopExitCountBuckets[] = {0, 1, 2, 3, 4, 5, 6, 0};
Histogram loopExitCountTable(HostAllocator::getHostAllocator(), loopExitCountBuckets);

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
// Note that for ARM64 there will alwys be exactly two pointer sized fields

void Compiler::getStructGcPtrsFromOp(GenTreePtr op, BYTE* gcPtrsOut)
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
        if (!info.compCompHnd->isValueClass(clsHnd) && info.compCompHnd->getClassNumInstanceFields(clsHnd) != 1)
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
//     If the struct is a one element HFA, we will return the
//     proper floating point type.
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
//
var_types Compiler::getPrimitiveTypeForStruct(unsigned structSize, CORINFO_CLASS_HANDLE clsHnd)
{
    assert(structSize != 0);

    var_types useType;

    switch (structSize)
    {
        case 1:
            useType = TYP_BYTE;
            break;

        case 2:
            useType = TYP_SHORT;
            break;

#ifndef _TARGET_XARCH_
        case 3:
            useType = TYP_INT;
            break;

#endif // _TARGET_XARCH_

#ifdef _TARGET_64BIT_
        case 4:
            if (IsHfa(clsHnd))
            {
                // A structSize of 4 with IsHfa, it must be an HFA of one float
                useType = TYP_FLOAT;
            }
            else
            {
                useType = TYP_INT;
            }
            break;

#ifndef _TARGET_XARCH_
        case 5:
        case 6:
        case 7:
            useType = TYP_I_IMPL;
            break;

#endif // _TARGET_XARCH_
#endif // _TARGET_64BIT_

        case TARGET_POINTER_SIZE:
#ifdef ARM_SOFTFP
            // For ARM_SOFTFP, HFA is unsupported so we need to check in another way
            // This matters only for size-4 struct cause bigger structs would be processed with RetBuf
            if (isSingleFloat32Struct(clsHnd))
#else  // !ARM_SOFTFP
            if (IsHfa(clsHnd))
#endif // ARM_SOFTFP
            {
#ifdef _TARGET_64BIT_
                var_types hfaType = GetHfaType(clsHnd);

                // A structSize of 8 with IsHfa, we have two possiblities:
                // An HFA of one double or an HFA of two floats
                //
                // Check and exclude the case of an HFA of two floats
                if (hfaType == TYP_DOUBLE)
                {
                    // We have an HFA of one double
                    useType = TYP_DOUBLE;
                }
                else
                {
                    assert(hfaType == TYP_FLOAT);

                    // We have an HFA of two floats
                    // This should be passed or returned in two FP registers
                    useType = TYP_UNKNOWN;
                }
#else  // a 32BIT target
                // A structSize of 4 with IsHfa, it must be an HFA of one float
                useType = TYP_FLOAT;
#endif // _TARGET_64BIT_
            }
            else
            {
                BYTE gcPtr = 0;
                // Check if this pointer-sized struct is wrapping a GC object
                info.compCompHnd->getClassGClayout(clsHnd, &gcPtr);
                useType = getJitGCType(gcPtr);
            }
            break;

#ifdef _TARGET_ARM_
        case 8:
            if (IsHfa(clsHnd))
            {
                var_types hfaType = GetHfaType(clsHnd);

                // A structSize of 8 with IsHfa, we have two possiblities:
                // An HFA of one double or an HFA of two floats
                //
                // Check and exclude the case of an HFA of two floats
                if (hfaType == TYP_DOUBLE)
                {
                    // We have an HFA of one double
                    useType = TYP_DOUBLE;
                }
                else
                {
                    assert(hfaType == TYP_FLOAT);

                    // We have an HFA of two floats
                    // This should be passed or returned in two FP registers
                    useType = TYP_UNKNOWN;
                }
            }
            else
            {
                // We don't have an HFA
                useType = TYP_UNKNOWN;
            }
            break;
#endif // _TARGET_ARM_

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
//     passed as the optional third argument, as this allows us to avoid
//     an extra call to getClassSize(clsHnd)
//
// Arguments:
//    clsHnd       - the handle for the struct type
//    wbPassStruct - An "out" argument with information about how
//                   the struct is to be passed
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
                                        unsigned             structSize /* = 0 */)
{
    var_types         useType         = TYP_UNKNOWN;
    structPassingKind howToPassStruct = SPK_Unknown; // We must change this before we return

    if (structSize == 0)
    {
        structSize = info.compCompHnd->getClassSize(clsHnd);
    }
    assert(structSize > 0);

#ifdef FEATURE_UNIX_AMD64_STRUCT_PASSING

    // An 8-byte struct may need to be passed in a floating point register
    // So we always consult the struct "Classifier" routine
    //
    SYSTEMV_AMD64_CORINFO_STRUCT_REG_PASSING_DESCRIPTOR structDesc;
    eeGetSystemVAmd64PassStructInRegisterDescriptor(clsHnd, &structDesc);

    // If we have one eightByteCount then we can set 'useType' based on that
    if (structDesc.eightByteCount == 1)
    {
        // Set 'useType' to the type of the first eightbyte item
        useType = GetEightByteType(structDesc, 0);
    }

#elif defined(_TARGET_X86_)

    // On x86 we never pass structs as primitive types (unless the VM unwraps them for us)
    useType = TYP_UNKNOWN;

#else // all other targets

    // The largest primitive type is 8 bytes (TYP_DOUBLE)
    // so we can skip calling getPrimitiveTypeForStruct when we
    // have a struct that is larger than that.
    //
    if (structSize <= sizeof(double))
    {
        // We set the "primitive" useType based upon the structSize
        // and also examine the clsHnd to see if it is an HFA of count one
        useType = getPrimitiveTypeForStruct(structSize, clsHnd);
    }

#endif // all other targets

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
            // Structs that are HFA's are passed by value in multiple registers
            if (IsHfa(clsHnd))
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

#ifdef FEATURE_UNIX_AMD64_STRUCT_PASSING

                // The case of (structDesc.eightByteCount == 1) should have already been handled
                if (structDesc.eightByteCount > 1)
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

#if defined(_TARGET_X86_) || defined(_TARGET_ARM_)

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
                                           structPassingKind*   wbReturnStruct,
                                           unsigned             structSize /* = 0 */)
{
    var_types         useType           = TYP_UNKNOWN;
    structPassingKind howToReturnStruct = SPK_Unknown; // We must change this before we return

    assert(clsHnd != NO_CLASS_HANDLE);

    if (structSize == 0)
    {
        structSize = info.compCompHnd->getClassSize(clsHnd);
    }
    assert(structSize > 0);

#ifdef FEATURE_UNIX_AMD64_STRUCT_PASSING

    // An 8-byte struct may need to be returned in a floating point register
    // So we always consult the struct "Classifier" routine
    //
    SYSTEMV_AMD64_CORINFO_STRUCT_REG_PASSING_DESCRIPTOR structDesc;
    eeGetSystemVAmd64PassStructInRegisterDescriptor(clsHnd, &structDesc);

    // If we have one eightByteCount then we can set 'useType' based on that
    if (structDesc.eightByteCount == 1)
    {
        // Set 'useType' to the type of the first eightbyte item
        useType = GetEightByteType(structDesc, 0);
        assert(structDesc.passedInRegisters == true);
    }

#else // not UNIX_AMD64

    // The largest primitive type is 8 bytes (TYP_DOUBLE)
    // so we can skip calling getPrimitiveTypeForStruct when we
    // have a struct that is larger than that.
    //
    if (structSize <= sizeof(double))
    {
        // We set the "primitive" useType based upon the structSize
        // and also examine the clsHnd to see if it is an HFA of count one
        useType = getPrimitiveTypeForStruct(structSize, clsHnd);
    }

#endif // FEATURE_UNIX_AMD64_STRUCT_PASSING

#ifdef _TARGET_64BIT_
    // Note this handles an odd case when FEATURE_MULTIREG_RET is disabled and HFAs are enabled
    //
    // getPrimitiveTypeForStruct will return TYP_UNKNOWN for a struct that is an HFA of two floats
    // because when HFA are enabled, normally we would use two FP registers to pass or return it
    //
    // But if we don't have support for multiple register return types, we have to change this.
    // Since we what we have an 8-byte struct (float + float)  we change useType to TYP_I_IMPL
    // so that the struct is returned instead using an 8-byte integer register.
    //
    if ((FEATURE_MULTIREG_RET == 0) && (useType == TYP_UNKNOWN) && (structSize == (2 * sizeof(float))) && IsHfa(clsHnd))
    {
        useType = TYP_I_IMPL;
    }
#endif

    // Did we change this struct type into a simple "primitive" type?
    //
    if (useType != TYP_UNKNOWN)
    {
        // Yes, we should use the "primitive" type in 'useType'
        howToReturnStruct = SPK_PrimitiveType;
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

#ifdef FEATURE_UNIX_AMD64_STRUCT_PASSING

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

/* static */
bool Compiler::s_dspMemStats = false;
#endif

#ifndef DEBUGGING_SUPPORT
/* static */
const bool Compiler::Options::compDbgCode = false;
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

    // Initialize the JIT's allocator.
    ArenaAllocator::startup();

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

    ArenaAllocator::shutdown();

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

    // Where should we write our statistics output?
    FILE* fout = jitstdout;

#ifdef FEATURE_JIT_METHOD_PERF
    if (compJitTimeLogFilename != NULL)
    {
        // I assume that this will return NULL if it fails for some reason, and
        // that...
        FILE* jitTimeLogFile = _wfopen(compJitTimeLogFilename, W("a"));
        // ...Print will return silently with a NULL argument.
        CompTimeSummaryInfo::s_compTimeSummary.Print(jitTimeLogFile);
        fclose(jitTimeLogFile);
    }
#endif // FEATURE_JIT_METHOD_PERF

#if FUNC_INFO_LOGGING
    if (compJitFuncInfoFile != nullptr)
    {
        fclose(compJitFuncInfoFile);
        compJitFuncInfoFile = nullptr;
    }
#endif // FUNC_INFO_LOGGING

#if COUNT_RANGECHECKS
    if (optRangeChkAll > 0)
    {
        fprintf(fout, "Removed %u of %u range checks\n", optRangeChkRmv, optRangeChkAll);
    }
#endif // COUNT_RANGECHECKS

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

    fprintf(fout, "Allocated %6u tree nodes (%7u bytes total, avg %4u bytes per method)\n",
            genNodeSizeStats.genTreeNodeCnt, genNodeSizeStats.genTreeNodeSize,
            genNodeSizeStats.genTreeNodeSize / genMethodCnt);

    fprintf(fout, "Allocated %7u bytes of unused tree node space (%3.2f%%)\n",
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

#ifdef DEBUG
    // Under debug, we only dump memory stats when the COMPlus_* variable is defined.
    // Under non-debug, we don't have the COMPlus_* variable, and we always dump it.
    if (s_dspMemStats)
#endif
    {
        fprintf(fout, "\nAll allocations:\n");
        s_aggMemStats.Print(jitstdout);

        fprintf(fout, "\nLargest method:\n");
        s_maxCompMemStats.Print(jitstdout);
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
    /*
        IMPORTANT:  Use the following code to check the alignment of
                    GenTree members (in a retail build, of course).
     */

    GenTree* gtDummy = nullptr;

    fprintf(fout, "\n");
    fprintf(fout, "Offset / size of gtOper         = %2u / %2u\n", offsetof(GenTree, gtOper), sizeof(gtDummy->gtOper));
    fprintf(fout, "Offset / size of gtType         = %2u / %2u\n", offsetof(GenTree, gtType), sizeof(gtDummy->gtType));
#if FEATURE_ANYCSE
    fprintf(fout, "Offset / size of gtCSEnum       = %2u / %2u\n", offsetof(GenTree, gtCSEnum),
            sizeof(gtDummy->gtCSEnum));
#endif // FEATURE_ANYCSE
#if ASSERTION_PROP
    fprintf(fout, "Offset / size of gtAssertionNum = %2u / %2u\n", offsetof(GenTree, gtAssertionNum),
            sizeof(gtDummy->gtAssertionNum));
#endif // ASSERTION_PROP
#if FEATURE_STACK_FP_X87
    fprintf(fout, "Offset / size of gtFPlvl        = %2u / %2u\n", offsetof(GenTree, gtFPlvl),
            sizeof(gtDummy->gtFPlvl));
#endif // FEATURE_STACK_FP_X87
    // TODO: The section that report GenTree sizes should be made into a public static member function of the GenTree
    // class (see https://github.com/dotnet/coreclr/pull/493)
    // fprintf(fout, "Offset / size of gtCostEx       = %2u / %2u\n", offsetof(GenTree, _gtCostEx     ),
    // sizeof(gtDummy->_gtCostEx     ));
    // fprintf(fout, "Offset / size of gtCostSz       = %2u / %2u\n", offsetof(GenTree, _gtCostSz     ),
    // sizeof(gtDummy->_gtCostSz     ));
    fprintf(fout, "Offset / size of gtFlags        = %2u / %2u\n", offsetof(GenTree, gtFlags),
            sizeof(gtDummy->gtFlags));
    fprintf(fout, "Offset / size of gtVNPair       = %2u / %2u\n", offsetof(GenTree, gtVNPair),
            sizeof(gtDummy->gtVNPair));
    fprintf(fout, "Offset / size of gtRsvdRegs     = %2u / %2u\n", offsetof(GenTree, gtRsvdRegs),
            sizeof(gtDummy->gtRsvdRegs));
#ifdef LEGACY_BACKEND
    fprintf(fout, "Offset / size of gtUsedRegs     = %2u / %2u\n", offsetof(GenTree, gtUsedRegs),
            sizeof(gtDummy->gtUsedRegs));
#endif // LEGACY_BACKEND
#ifndef LEGACY_BACKEND
    fprintf(fout, "Offset / size of gtLsraInfo     = %2u / %2u\n", offsetof(GenTree, gtLsraInfo),
            sizeof(gtDummy->gtLsraInfo));
#endif // !LEGACY_BACKEND
    fprintf(fout, "Offset / size of gtNext         = %2u / %2u\n", offsetof(GenTree, gtNext), sizeof(gtDummy->gtNext));
    fprintf(fout, "Offset / size of gtPrev         = %2u / %2u\n", offsetof(GenTree, gtPrev), sizeof(gtDummy->gtPrev));
    fprintf(fout, "\n");

#if SMALL_TREE_NODES
    fprintf(fout, "Small tree node size        = %3u\n", TREE_NODE_SZ_SMALL);
#endif // SMALL_TREE_NODES
    fprintf(fout, "Large tree node size        = %3u\n", TREE_NODE_SZ_LARGE);
    fprintf(fout, "Size of GenTree             = %3u\n", sizeof(GenTree));
    fprintf(fout, "Size of GenTreeUnOp         = %3u\n", sizeof(GenTreeUnOp));
    fprintf(fout, "Size of GenTreeOp           = %3u\n", sizeof(GenTreeOp));
    fprintf(fout, "Size of GenTreeVal          = %3u\n", sizeof(GenTreeVal));
    fprintf(fout, "Size of GenTreeIntConCommon = %3u\n", sizeof(GenTreeIntConCommon));
    fprintf(fout, "Size of GenTreePhysReg      = %3u\n", sizeof(GenTreePhysReg));
#ifndef LEGACY_BACKEND
    fprintf(fout, "Size of GenTreeJumpTable    = %3u\n", sizeof(GenTreeJumpTable));
#endif // !LEGACY_BACKEND
    fprintf(fout, "Size of GenTreeIntCon       = %3u\n", sizeof(GenTreeIntCon));
    fprintf(fout, "Size of GenTreeLngCon       = %3u\n", sizeof(GenTreeLngCon));
    fprintf(fout, "Size of GenTreeDblCon       = %3u\n", sizeof(GenTreeDblCon));
    fprintf(fout, "Size of GenTreeStrCon       = %3u\n", sizeof(GenTreeStrCon));
    fprintf(fout, "Size of GenTreeLclVarCommon = %3u\n", sizeof(GenTreeLclVarCommon));
    fprintf(fout, "Size of GenTreeLclVar       = %3u\n", sizeof(GenTreeLclVar));
    fprintf(fout, "Size of GenTreeLclFld       = %3u\n", sizeof(GenTreeLclFld));
    fprintf(fout, "Size of GenTreeRegVar       = %3u\n", sizeof(GenTreeRegVar));
    fprintf(fout, "Size of GenTreeCast         = %3u\n", sizeof(GenTreeCast));
    fprintf(fout, "Size of GenTreeBox          = %3u\n", sizeof(GenTreeBox));
    fprintf(fout, "Size of GenTreeField        = %3u\n", sizeof(GenTreeField));
    fprintf(fout, "Size of GenTreeArgList      = %3u\n", sizeof(GenTreeArgList));
    fprintf(fout, "Size of GenTreeColon        = %3u\n", sizeof(GenTreeColon));
    fprintf(fout, "Size of GenTreeCall         = %3u\n", sizeof(GenTreeCall));
    fprintf(fout, "Size of GenTreeCmpXchg      = %3u\n", sizeof(GenTreeCmpXchg));
    fprintf(fout, "Size of GenTreeFptrVal      = %3u\n", sizeof(GenTreeFptrVal));
    fprintf(fout, "Size of GenTreeQmark        = %3u\n", sizeof(GenTreeQmark));
    fprintf(fout, "Size of GenTreeIntrinsic    = %3u\n", sizeof(GenTreeIntrinsic));
    fprintf(fout, "Size of GenTreeIndex        = %3u\n", sizeof(GenTreeIndex));
    fprintf(fout, "Size of GenTreeArrLen       = %3u\n", sizeof(GenTreeArrLen));
    fprintf(fout, "Size of GenTreeBoundsChk    = %3u\n", sizeof(GenTreeBoundsChk));
    fprintf(fout, "Size of GenTreeArrElem      = %3u\n", sizeof(GenTreeArrElem));
    fprintf(fout, "Size of GenTreeAddrMode     = %3u\n", sizeof(GenTreeAddrMode));
    fprintf(fout, "Size of GenTreeIndir        = %3u\n", sizeof(GenTreeIndir));
    fprintf(fout, "Size of GenTreeStoreInd     = %3u\n", sizeof(GenTreeStoreInd));
    fprintf(fout, "Size of GenTreeRetExpr      = %3u\n", sizeof(GenTreeRetExpr));
    fprintf(fout, "Size of GenTreeStmt         = %3u\n", sizeof(GenTreeStmt));
    fprintf(fout, "Size of GenTreeObj          = %3u\n", sizeof(GenTreeObj));
    fprintf(fout, "Size of GenTreeClsVar       = %3u\n", sizeof(GenTreeClsVar));
    fprintf(fout, "Size of GenTreeArgPlace     = %3u\n", sizeof(GenTreeArgPlace));
    fprintf(fout, "Size of GenTreeLabel        = %3u\n", sizeof(GenTreeLabel));
    fprintf(fout, "Size of GenTreePhiArg       = %3u\n", sizeof(GenTreePhiArg));
    fprintf(fout, "Size of GenTreePutArgStk    = %3u\n", sizeof(GenTreePutArgStk));
    fprintf(fout, "\n");
#endif // MEASURE_NODE_SIZE

#if MEASURE_BLOCK_SIZE

    BasicBlock* bbDummy = nullptr;

    fprintf(fout, "\n");
    fprintf(fout, "Offset / size of bbNext                = %3u / %3u\n", offsetof(BasicBlock, bbNext),
            sizeof(bbDummy->bbNext));
    fprintf(fout, "Offset / size of bbNum                 = %3u / %3u\n", offsetof(BasicBlock, bbNum),
            sizeof(bbDummy->bbNum));
    fprintf(fout, "Offset / size of bbPostOrderNum        = %3u / %3u\n", offsetof(BasicBlock, bbPostOrderNum),
            sizeof(bbDummy->bbPostOrderNum));
    fprintf(fout, "Offset / size of bbRefs                = %3u / %3u\n", offsetof(BasicBlock, bbRefs),
            sizeof(bbDummy->bbRefs));
    fprintf(fout, "Offset / size of bbFlags               = %3u / %3u\n", offsetof(BasicBlock, bbFlags),
            sizeof(bbDummy->bbFlags));
    fprintf(fout, "Offset / size of bbWeight              = %3u / %3u\n", offsetof(BasicBlock, bbWeight),
            sizeof(bbDummy->bbWeight));
    fprintf(fout, "Offset / size of bbJumpKind            = %3u / %3u\n", offsetof(BasicBlock, bbJumpKind),
            sizeof(bbDummy->bbJumpKind));
    fprintf(fout, "Offset / size of bbJumpOffs            = %3u / %3u\n", offsetof(BasicBlock, bbJumpOffs),
            sizeof(bbDummy->bbJumpOffs));
    fprintf(fout, "Offset / size of bbJumpDest            = %3u / %3u\n", offsetof(BasicBlock, bbJumpDest),
            sizeof(bbDummy->bbJumpDest));
    fprintf(fout, "Offset / size of bbJumpSwt             = %3u / %3u\n", offsetof(BasicBlock, bbJumpSwt),
            sizeof(bbDummy->bbJumpSwt));
    fprintf(fout, "Offset / size of bbTreeList            = %3u / %3u\n", offsetof(BasicBlock, bbTreeList),
            sizeof(bbDummy->bbTreeList));
    fprintf(fout, "Offset / size of bbEntryState          = %3u / %3u\n", offsetof(BasicBlock, bbEntryState),
            sizeof(bbDummy->bbEntryState));
    fprintf(fout, "Offset / size of bbStkTempsIn          = %3u / %3u\n", offsetof(BasicBlock, bbStkTempsIn),
            sizeof(bbDummy->bbStkTempsIn));
    fprintf(fout, "Offset / size of bbStkTempsOut         = %3u / %3u\n", offsetof(BasicBlock, bbStkTempsOut),
            sizeof(bbDummy->bbStkTempsOut));
    fprintf(fout, "Offset / size of bbTryIndex            = %3u / %3u\n", offsetof(BasicBlock, bbTryIndex),
            sizeof(bbDummy->bbTryIndex));
    fprintf(fout, "Offset / size of bbHndIndex            = %3u / %3u\n", offsetof(BasicBlock, bbHndIndex),
            sizeof(bbDummy->bbHndIndex));
    fprintf(fout, "Offset / size of bbCatchTyp            = %3u / %3u\n", offsetof(BasicBlock, bbCatchTyp),
            sizeof(bbDummy->bbCatchTyp));
    fprintf(fout, "Offset / size of bbStkDepth            = %3u / %3u\n", offsetof(BasicBlock, bbStkDepth),
            sizeof(bbDummy->bbStkDepth));
    fprintf(fout, "Offset / size of bbFPinVars            = %3u / %3u\n", offsetof(BasicBlock, bbFPinVars),
            sizeof(bbDummy->bbFPinVars));
    fprintf(fout, "Offset / size of bbPreds               = %3u / %3u\n", offsetof(BasicBlock, bbPreds),
            sizeof(bbDummy->bbPreds));
    fprintf(fout, "Offset / size of bbReach               = %3u / %3u\n", offsetof(BasicBlock, bbReach),
            sizeof(bbDummy->bbReach));
    fprintf(fout, "Offset / size of bbIDom                = %3u / %3u\n", offsetof(BasicBlock, bbIDom),
            sizeof(bbDummy->bbIDom));
    fprintf(fout, "Offset / size of bbDfsNum              = %3u / %3u\n", offsetof(BasicBlock, bbDfsNum),
            sizeof(bbDummy->bbDfsNum));
    fprintf(fout, "Offset / size of bbCodeOffs            = %3u / %3u\n", offsetof(BasicBlock, bbCodeOffs),
            sizeof(bbDummy->bbCodeOffs));
    fprintf(fout, "Offset / size of bbCodeOffsEnd         = %3u / %3u\n", offsetof(BasicBlock, bbCodeOffsEnd),
            sizeof(bbDummy->bbCodeOffsEnd));
    fprintf(fout, "Offset / size of bbVarUse              = %3u / %3u\n", offsetof(BasicBlock, bbVarUse),
            sizeof(bbDummy->bbVarUse));
    fprintf(fout, "Offset / size of bbVarDef              = %3u / %3u\n", offsetof(BasicBlock, bbVarDef),
            sizeof(bbDummy->bbVarDef));
    fprintf(fout, "Offset / size of bbVarTmp              = %3u / %3u\n", offsetof(BasicBlock, bbVarTmp),
            sizeof(bbDummy->bbVarTmp));
    fprintf(fout, "Offset / size of bbLiveIn              = %3u / %3u\n", offsetof(BasicBlock, bbLiveIn),
            sizeof(bbDummy->bbLiveIn));
    fprintf(fout, "Offset / size of bbLiveOut             = %3u / %3u\n", offsetof(BasicBlock, bbLiveOut),
            sizeof(bbDummy->bbLiveOut));
    fprintf(fout, "Offset / size of bbHeapSsaPhiFunc      = %3u / %3u\n", offsetof(BasicBlock, bbHeapSsaPhiFunc),
            sizeof(bbDummy->bbHeapSsaPhiFunc));
    fprintf(fout, "Offset / size of bbHeapSsaNumIn        = %3u / %3u\n", offsetof(BasicBlock, bbHeapSsaNumIn),
            sizeof(bbDummy->bbHeapSsaNumIn));
    fprintf(fout, "Offset / size of bbHeapSsaNumOut       = %3u / %3u\n", offsetof(BasicBlock, bbHeapSsaNumOut),
            sizeof(bbDummy->bbHeapSsaNumOut));

#ifdef DEBUGGING_SUPPORT
    fprintf(fout, "Offset / size of bbScope               = %3u / %3u\n", offsetof(BasicBlock, bbScope),
            sizeof(bbDummy->bbScope));
#endif // DEBUGGING_SUPPORT

    fprintf(fout, "Offset / size of bbCseGen              = %3u / %3u\n", offsetof(BasicBlock, bbCseGen),
            sizeof(bbDummy->bbCseGen));
    fprintf(fout, "Offset / size of bbCseIn               = %3u / %3u\n", offsetof(BasicBlock, bbCseIn),
            sizeof(bbDummy->bbCseIn));
    fprintf(fout, "Offset / size of bbCseOut              = %3u / %3u\n", offsetof(BasicBlock, bbCseOut),
            sizeof(bbDummy->bbCseOut));

    fprintf(fout, "Offset / size of bbEmitCookie          = %3u / %3u\n", offsetof(BasicBlock, bbEmitCookie),
            sizeof(bbDummy->bbEmitCookie));

#if FEATURE_EH_FUNCLETS && defined(_TARGET_ARM_)
    fprintf(fout, "Offset / size of bbUnwindNopEmitCookie = %3u / %3u\n", offsetof(BasicBlock, bbUnwindNopEmitCookie),
            sizeof(bbDummy->bbUnwindNopEmitCookie));
#endif // FEATURE_EH_FUNCLETS && defined(_TARGET_ARM_)

#ifdef VERIFIER
    fprintf(fout, "Offset / size of bbStackIn             = %3u / %3u\n", offsetof(BasicBlock, bbStackIn),
            sizeof(bbDummy->bbStackIn));
    fprintf(fout, "Offset / size of bbStackOut            = %3u / %3u\n", offsetof(BasicBlock, bbStackOut),
            sizeof(bbDummy->bbStackOut));
    fprintf(fout, "Offset / size of bbTypesIn             = %3u / %3u\n", offsetof(BasicBlock, bbTypesIn),
            sizeof(bbDummy->bbTypesIn));
    fprintf(fout, "Offset / size of bbTypesOut            = %3u / %3u\n", offsetof(BasicBlock, bbTypesOut),
            sizeof(bbDummy->bbTypesOut));
#endif // VERIFIER

#if FEATURE_STACK_FP_X87
    fprintf(fout, "Offset / size of bbFPStateX87          = %3u / %3u\n", offsetof(BasicBlock, bbFPStateX87),
            sizeof(bbDummy->bbFPStateX87));
#endif // FEATURE_STACK_FP_X87

#ifdef DEBUG
    fprintf(fout, "Offset / size of bbLoopNum             = %3u / %3u\n", offsetof(BasicBlock, bbLoopNum),
            sizeof(bbDummy->bbLoopNum));
#endif // DEBUG

    fprintf(fout, "\n");
    fprintf(fout, "Size   of BasicBlock                   = %3u\n", sizeof(BasicBlock));

#endif // MEASURE_BLOCK_SIZE

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
    compAllocator = pAlloc;

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
        compAsIAllocator = nullptr; // We shouldn't be using the compAsIAllocator for other than the root compiler.
#if MEASURE_MEM_ALLOC
        compAsIAllocatorBitset    = nullptr;
        compAsIAllocatorGC        = nullptr;
        compAsIAllocatorLoopHoist = nullptr;
#ifdef DEBUG
        compAsIAllocatorDebugOnly = nullptr;
#endif // DEBUG
#endif // MEASURE_MEM_ALLOC

        compQMarks = nullptr;
    }
    else
    {
        m_inlineStrategy = new (this, CMK_Inlining) InlineStrategy(this);
        compInlineResult = nullptr;
        compAsIAllocator = new (this, CMK_Unknown) CompAllocator(this, CMK_AsIAllocator);
#if MEASURE_MEM_ALLOC
        compAsIAllocatorBitset    = new (this, CMK_Unknown) CompAllocator(this, CMK_bitset);
        compAsIAllocatorGC        = new (this, CMK_Unknown) CompAllocator(this, CMK_GC);
        compAsIAllocatorLoopHoist = new (this, CMK_Unknown) CompAllocator(this, CMK_LoopHoist);
#ifdef DEBUG
        compAsIAllocatorDebugOnly = new (this, CMK_Unknown) CompAllocator(this, CMK_DebugOnly);
#endif // DEBUG
#endif // MEASURE_MEM_ALLOC

        compQMarks = new (this, CMK_Unknown) ExpandArrayStack<GenTreePtr>(getAllocator());
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
#ifdef LEGACY_BACKEND
        raInit();
#endif // LEGACY_BACKEND
        optInit();
#ifndef LEGACY_BACKEND
        hashBv::Init(this);
#endif // !LEGACY_BACKEND

        compVarScopeMap = nullptr;

        // If this method were a real constructor for Compiler, these would
        // become method initializations.
        impPendingBlockMembers    = ExpandArray<BYTE>(getAllocator());
        impSpillCliquePredMembers = ExpandArray<BYTE>(getAllocator());
        impSpillCliqueSuccMembers = ExpandArray<BYTE>(getAllocator());

        memset(&lvHeapPerSsaData, 0, sizeof(PerSsaArray));
        lvHeapPerSsaData.Init(getAllocator());
        lvHeapNumSsaNames = 0;

        //
        // Initialize all the per-method statistics gathering data structures.
        //

        optLoopsCloned = 0;

#if MEASURE_MEM_ALLOC
        genMemStats.Init();
#endif // MEASURE_MEM_ALLOC
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
    compQmarkRationalized = false;
    compQmarkUsed         = false;
    compFloatingPointUsed = false;
    compUnsafeCastUsed    = false;
#if CPU_USES_BLOCK_MOVE
    compBlkOpUsed = false;
#endif
#if FEATURE_STACK_FP_X87
    compMayHaveTransitionBlocks = false;
#endif
    compNeedsGSSecurityCookie = false;
    compGSReorderStackLayout  = false;
#if STACK_PROBES
    compStackProbePrologDone = false;
#endif

    compGeneratingProlog = false;
    compGeneratingEpilog = false;

#ifndef LEGACY_BACKEND
    compLSRADone = false;
#endif // !LEGACY_BACKEND
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

    for (unsigned i = 0; i < MAX_LOOP_NUM; i++)
    {
        AllVarSetOps::AssignNoCopy(this, optLoopTable[i].lpAsgVars, AllVarSetOps::UninitVal());
    }

#ifdef DEBUG
    m_nodeTestData      = nullptr;
    m_loopHoistCSEClass = FIRST_LOOP_HOIST_CSE_CLASS;
#endif
    m_switchDescMap      = nullptr;
    m_blockToEHPreds     = nullptr;
    m_fieldSeqStore      = nullptr;
    m_zeroOffsetFieldMap = nullptr;
    m_arrayInfoMap       = nullptr;
    m_heapSsaMap         = nullptr;
    m_refAnyClass        = nullptr;

#ifdef DEBUG
    if (!compIsForInlining())
    {
        compDoComponentUnitTestsOnce();
    }
#endif // DEBUG

    vnStore               = nullptr;
    m_opAsgnVarDefSsaNums = nullptr;
    m_indirAssignMap      = nullptr;
    fgSsaPassesCompleted  = 0;
    fgVNPassesCompleted   = 0;

    // check that HelperCallProperties are initialized

    assert(s_helperCallProperties.IsPure(CORINFO_HELP_GETSHARED_GCSTATIC_BASE));
    assert(!s_helperCallProperties.IsPure(CORINFO_HELP_GETFIELDOBJ)); // quick sanity check

    // We start with the flow graph in tree-order
    fgOrder = FGOrderTree;

#ifdef FEATURE_SIMD
    // SIMD Types
    SIMDFloatHandle   = nullptr;
    SIMDDoubleHandle  = nullptr;
    SIMDIntHandle     = nullptr;
    SIMDUShortHandle  = nullptr;
    SIMDUByteHandle   = nullptr;
    SIMDShortHandle   = nullptr;
    SIMDByteHandle    = nullptr;
    SIMDLongHandle    = nullptr;
    SIMDUIntHandle    = nullptr;
    SIMDULongHandle   = nullptr;
    SIMDVector2Handle = nullptr;
    SIMDVector3Handle = nullptr;
    SIMDVector4Handle = nullptr;
    SIMDVectorHandle  = nullptr;
#endif

#ifdef DEBUG
    inlRNG = nullptr;
#endif

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
        addr = (void*)0xCA11CA11; // "callcall"
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
        sigSize = 2 * sizeof(void*);
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
#endif // DEBUG

/******************************************************************************
 *
 *  The Emitter uses this callback function to allocate its memory
 */

/* static */
void* Compiler::compGetMemCallback(void* p, size_t size, CompMemKind cmk)
{
    assert(p);

    return ((Compiler*)p)->compGetMem(size, cmk);
}

/*****************************************************************************
 *
 *  The central memory allocation routine used by the compiler. Normally this
 *  is a simple inline method defined in compiler.hpp, but for debugging it's
 *  often convenient to keep it non-inline.
 */

#ifdef DEBUG

void* Compiler::compGetMem(size_t sz, CompMemKind cmk)
{
#if 0
#if SMALL_TREE_NODES
    if  (sz != TREE_NODE_SZ_SMALL &&
         sz != TREE_NODE_SZ_LARGE && sz > 32)
    {
        printf("Alloc %3u bytes\n", sz);
    }
#else
    if  (sz != sizeof(GenTree)    && sz > 32)
    {
        printf("Alloc %3u bytes\n", sz);
    }
#endif
#endif // 0

#if MEASURE_MEM_ALLOC
    genMemStats.AddAlloc(sz, cmk);
#endif

    void* ptr = compAllocator->allocateMemory(sz);

    // Verify that the current block is aligned. Only then will the next
    // block allocated be on an aligned boundary.
    assert((size_t(ptr) & (sizeof(size_t) - 1)) == 0);

    return ptr;
}

#endif

/*****************************************************************************/
#ifdef DEBUG
/*****************************************************************************/

VarName Compiler::compVarName(regNumber reg, bool isFloatReg)
{
    if (isFloatReg)
    {
#if FEATURE_STACK_FP_X87
        assert(reg < FP_STK_SIZE); // would like to have same assert as below but sometimes you get -1?
#else
        assert(genIsValidFloatReg(reg));
#endif
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

#ifdef LEGACY_BACKEND
        // maybe var is marked dead, but still used (last use)
        if (!isFloatReg && codeGen->regSet.rsUsedTree[reg] != NULL)
        {
            GenTreePtr nodePtr;

            if (GenTree::OperIsUnary(codeGen->regSet.rsUsedTree[reg]->OperGet()))
            {
                assert(codeGen->regSet.rsUsedTree[reg]->gtOp.gtOp1 != NULL);
                nodePtr = codeGen->regSet.rsUsedTree[reg]->gtOp.gtOp1;
            }
            else
            {
                nodePtr = codeGen->regSet.rsUsedTree[reg];
            }

            if ((nodePtr->gtOper == GT_REG_VAR) && (nodePtr->gtRegVar.gtRegNum == reg) &&
                (nodePtr->gtRegVar.gtLclNum < info.compVarScopesCount))
            {
                VarScopeDsc* varScope =
                    compFindLocalVar(nodePtr->gtRegVar.gtLclNum, compCurBB->bbCodeOffs, compCurBB->bbCodeOffsEnd);
                if (varScope)
                    return varScope->vsdName;
            }
        }
#endif // LEGACY_BACKEND
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

#define MAX_REG_PAIR_NAME_LENGTH 10

const char* Compiler::compRegPairName(regPairNo regPair)
{
    static char regNameLong[MAX_REG_PAIR_NAME_LENGTH];

    if (regPair == REG_PAIR_NONE)
    {
        return "NA|NA";
    }

    assert(regPair >= REG_PAIR_FIRST && regPair <= REG_PAIR_LAST);

    strcpy_s(regNameLong, sizeof(regNameLong), compRegVarName(genRegPairLo(regPair)));
    strcat_s(regNameLong, sizeof(regNameLong), "|");
    strcat_s(regNameLong, sizeof(regNameLong), compRegVarName(genRegPairHi(regPair)));
    return regNameLong;
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

#if FEATURE_STACK_FP_X87
    /* 'fpReg' is the distance from the bottom of the stack, ie.
     * it is independant of the current FP stack level
     */

    if (displayVar && codeGen->genFPregCnt)
    {
        assert(fpReg < FP_STK_SIZE);
        assert(compCodeGenDone || (fpReg <= codeGen->compCurFPState.m_uStackSize));

        int pos = codeGen->genFPregCnt - (fpReg + 1 - codeGen->genGetFPstkLevel());
        if (pos >= 0)
        {
            VarName varName = compVarName((regNumber)pos, true);

            if (varName)
            {
                sprintf_s(nameVarReg[index], NAME_VAR_REG_BUFFER_LEN, "ST(%d)'%s'", fpReg, VarNameToStr(varName));
                return nameVarReg[index];
            }
        }
    }
#endif // FEATURE_STACK_FP_X87

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
    unsigned compileFlags = opts.eeFlags;

#if defined(_TARGET_ARM_)
    info.genCPU = CPU_ARM;
#elif defined(_TARGET_AMD64_)
    info.genCPU = CPU_X64;
#elif defined(_TARGET_X86_)
    if (compileFlags & CORJIT_FLG_TARGET_P4)
        info.genCPU = CPU_X86_PENTIUM_4;
    else
        info.genCPU = CPU_X86;
#endif

    //
    // Processor specific optimizations
    //
    CLANG_FORMAT_COMMENT_ANCHOR;

#ifdef _TARGET_AMD64_
    opts.compUseFCOMI   = false;
    opts.compUseCMOV    = true;
    opts.compCanUseSSE2 = true;

#ifdef FEATURE_AVX_SUPPORT
    // COMPlus_EnableAVX can be used to disable using AVX if available on a target machine.
    // Note that FEATURE_AVX_SUPPORT is not enabled for ctpjit
    opts.compCanUseAVX = false;
    if (((compileFlags & CORJIT_FLG_PREJIT) == 0) && ((compileFlags & CORJIT_FLG_USE_AVX2) != 0))
    {
        if (JitConfig.EnableAVX() != 0)
        {
            opts.compCanUseAVX = true;
            if (!compIsForInlining())
            {
                codeGen->getEmitter()->SetUseAVX(true);
            }
        }
    }
#endif
#endif //_TARGET_AMD64_

#ifdef _TARGET_X86_
    opts.compUseFCOMI   = ((opts.eeFlags & CORJIT_FLG_USE_FCOMI) != 0);
    opts.compUseCMOV    = ((opts.eeFlags & CORJIT_FLG_USE_CMOV) != 0);
    opts.compCanUseSSE2 = ((opts.eeFlags & CORJIT_FLG_USE_SSE2) != 0);

#ifdef DEBUG
    if (opts.compUseFCOMI)
        opts.compUseFCOMI = !compStressCompile(STRESS_USE_FCOMI, 50);
    if (opts.compUseCMOV)
        opts.compUseCMOV = !compStressCompile(STRESS_USE_CMOV, 50);

    // Should we override the SSE2 setting
    enum
    {
        SSE2_FORCE_DISABLE = 0,
        SSE2_FORCE_USE     = 1,
        SSE2_FORCE_INVALID = -1
    };

    if (JitConfig.JitCanUseSSE2() == SSE2_FORCE_DISABLE)
        opts.compCanUseSSE2 = false;
    else if (JitConfig.JitCanUseSSE2() == SSE2_FORCE_USE)
        opts.compCanUseSSE2 = true;
    else if (opts.compCanUseSSE2)
        opts.compCanUseSSE2 = !compStressCompile(STRESS_GENERIC_VARN, 50);
#endif // DEBUG
#endif // _TARGET_X86_
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

void Compiler::compInitOptions(CORJIT_FLAGS* jitFlags)
{
#ifdef UNIX_AMD64_ABI
    opts.compNeedToAlignFrame = false;
#endif // UNIX_AMD64_ABI
    memset(&opts, 0, sizeof(opts));

    unsigned compileFlags = jitFlags->corJitFlags;

    if (compIsForInlining())
    {
        assert((compileFlags & CORJIT_FLG_LOST_WHEN_INLINING) == 0);
        assert(compileFlags & CORJIT_FLG_SKIP_VERIFICATION);
    }

    opts.jitFlags  = jitFlags;
    opts.eeFlags   = compileFlags;
    opts.compFlags = CLFLG_MAXOPT; // Default value is for full optimization

    if (opts.eeFlags & (CORJIT_FLG_DEBUG_CODE | CORJIT_FLG_MIN_OPT))
    {
        opts.compFlags = CLFLG_MINOPT;
    }
    // Don't optimize .cctors (except prejit) or if we're an inlinee
    else if (!(opts.eeFlags & CORJIT_FLG_PREJIT) && ((info.compFlags & FLG_CCTOR) == FLG_CCTOR) && !compIsForInlining())
    {
        opts.compFlags = CLFLG_MINOPT;
    }

    // Default value is to generate a blend of size and speed optimizations
    //
    opts.compCodeOpt = BLENDED_CODE;

    // If the EE sets SIZE_OPT or if we are compiling a Class constructor
    // we will optimize for code size at the expense of speed
    //
    if ((opts.eeFlags & CORJIT_FLG_SIZE_OPT) || ((info.compFlags & FLG_CCTOR) == FLG_CCTOR))
    {
        opts.compCodeOpt = SMALL_CODE;
    }
    //
    // If the EE sets SPEED_OPT we will optimize for speed at the expense of code size
    //
    else if (opts.eeFlags & CORJIT_FLG_SPEED_OPT)
    {
        opts.compCodeOpt = FAST_CODE;
        assert((opts.eeFlags & CORJIT_FLG_SIZE_OPT) == 0);
    }

//-------------------------------------------------------------------------

#ifdef DEBUGGING_SUPPORT
    opts.compDbgCode = (opts.eeFlags & CORJIT_FLG_DEBUG_CODE) != 0;
    opts.compDbgInfo = (opts.eeFlags & CORJIT_FLG_DEBUG_INFO) != 0;
    opts.compDbgEnC  = (opts.eeFlags & CORJIT_FLG_DEBUG_EnC) != 0;
#if REGEN_SHORTCUTS || REGEN_CALLPAT
    // We never want to have debugging enabled when regenerating GC encoding patterns
    opts.compDbgCode = false;
    opts.compDbgInfo = false;
    opts.compDbgEnC  = false;
#endif
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
    if (opts.eeFlags & CORJIT_FLG_PREJIT)
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
    if (opts.eeFlags & CORJIT_FLG_PREJIT)
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
    LPCWSTR dumpIRFormat       = nullptr;

    if (!altJitConfig || opts.altJit)
    {
        LPCWSTR dumpIRFormat = nullptr;

        // We should only enable 'verboseDump' when we are actually compiling a matching method
        // and not enable it when we are just considering inlining a matching method.
        //
        if (!compIsForInlining())
        {
            if (opts.eeFlags & CORJIT_FLG_PREJIT)
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
#ifdef _TARGET_AMD64_
    // Minimum bar for availing SIMD benefits is SSE2 on AMD64.
    featureSIMD = ((opts.eeFlags & CORJIT_FLG_FEATURE_SIMD) != 0);
#endif // _TARGET_AMD64_
#endif // FEATURE_SIMD

    if (compIsForInlining() || compIsForImportOnly())
    {
        return;
    }
    // The rest of the opts fields that we initialize here
    // should only be used when we generate code for the method
    // They should not be used when importing or inlining

    opts.genFPorder = true;
    opts.genFPopt   = true;

    opts.instrCount = 0;
    opts.lvRefCount = 0;

#if FEATURE_TAILCALL_OPT
    // By default opportunistic tail call optimization is enabled
    opts.compTailCallOpt     = true;
    opts.compTailCallLoopOpt = true;
#endif

#ifdef DEBUG
    opts.dspInstrs             = false;
    opts.dspEmit               = false;
    opts.dspLines              = false;
    opts.varNames              = false;
    opts.dmpHex                = false;
    opts.disAsm                = false;
    opts.disAsmSpilled         = false;
    opts.disDiffable           = false;
    opts.dspCode               = false;
    opts.dspEHTable            = false;
    opts.dspGCtbls             = false;
    opts.disAsm2               = false;
    opts.dspUnwind             = false;
    s_dspMemStats              = false;
    opts.compLongAddress       = false;
    opts.compJitELTHookEnabled = false;

#ifdef LATE_DISASM
    opts.doLateDisasm = false;
#endif // LATE_DISASM

    compDebugBreak = false;

    //  If we have a non-empty AltJit config then we change all of these other
    //  config values to refer only to the AltJit.
    //
    if (!altJitConfig || opts.altJit)
    {
        if (opts.eeFlags & CORJIT_FLG_PREJIT)
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
        }
        else
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

            if (JitConfig.JitUnwindDump().contains(info.compMethodName, info.compClassName, &info.compMethodInfo->args))
            {
                opts.dspUnwind = true;
            }

            if (JitConfig.JitEHDump().contains(info.compMethodName, info.compClassName, &info.compMethodInfo->args))
            {
                opts.dspEHTable = true;
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

        if (JitConfig.DisplayMemStats() != 0)
        {
            s_dspMemStats = true;
        }

        if (JitConfig.JitLongAddress() != 0)
        {
            opts.compLongAddress = true;
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

#endif // DEBUG

//-------------------------------------------------------------------------

#ifdef DEBUGGING_SUPPORT
#ifdef DEBUG
    assert(!codeGen->isGCTypeFixed());
    opts.compGcChecks = (JitConfig.JitGCChecks() != 0) || compStressCompile(STRESS_GENERIC_VARN, 5);

    enum
    {
        STACK_CHECK_ON_RETURN = 0x1,
        STACK_CHECK_ON_CALL   = 0x2,
        STACK_CHECK_ALL       = 0x3,
    };

    DWORD dwJitStackChecks = JitConfig.JitStackChecks();
    if (compStressCompile(STRESS_GENERIC_VARN, 5))
    {
        dwJitStackChecks = STACK_CHECK_ALL;
    }
    opts.compStackCheckOnRet  = (dwJitStackChecks & DWORD(STACK_CHECK_ON_RETURN)) != 0;
    opts.compStackCheckOnCall = (dwJitStackChecks & DWORD(STACK_CHECK_ON_CALL)) != 0;
#endif

#ifdef PROFILING_SUPPORTED
    opts.compNoPInvokeInlineCB = (opts.eeFlags & CORJIT_FLG_PROF_NO_PINVOKE_INLINE) ? true : false;

    // Cache the profiler handle
    if (opts.eeFlags & CORJIT_FLG_PROF_ENTERLEAVE)
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

#if defined(_TARGET_ARM_) || defined(_TARGET_AMD64_)
    // Right now this ELT hook option is enabled only for arm and amd64

    // Honour complus_JitELTHookEnabled only if VM has not asked us to generate profiler
    // hooks in the first place. That is, Override VM only if it hasn't asked for a
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
#endif // _TARGET_ARM_ || _TARGET_AMD64_

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

    opts.compMustInlinePInvokeCalli = (opts.eeFlags & CORJIT_FLG_IL_STUB) ? true : false;

    opts.compScopeInfo = opts.compDbgInfo;
#endif // DEBUGGING_SUPPORT

#ifdef LATE_DISASM
    codeGen->getDisAssembler().disOpenForLateDisAsm(info.compMethodName, info.compClassName,
                                                    info.compMethodInfo->args.pSig);
#endif

//-------------------------------------------------------------------------

#if RELOC_SUPPORT
    opts.compReloc = (opts.eeFlags & CORJIT_FLG_RELOC) ? true : false;
#endif

#ifdef DEBUG
#if defined(_TARGET_XARCH_) && !defined(LEGACY_BACKEND)
    // Whether encoding of absolute addr as PC-rel offset is enabled in RyuJIT
    opts.compEnablePCRelAddr = (JitConfig.EnablePCRelAddr() != 0);
#endif
#endif // DEBUG

    opts.compProcedureSplitting = (opts.eeFlags & CORJIT_FLG_PROCSPLIT) ? true : false;

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

    fgProfileBuffer              = nullptr;
    fgProfileData_ILSizeMismatch = false;
    fgNumProfileRuns             = 0;
    if (opts.eeFlags & CORJIT_FLG_BBOPT)
    {
        assert(!compIsForInlining());
        HRESULT hr;
        hr = info.compCompHnd->getBBProfileData(info.compMethodHnd, &fgProfileBufferCount, &fgProfileBuffer,
                                                &fgNumProfileRuns);

        // a failed result that also has a non-NULL fgProfileBuffer
        // indicates that the ILSize for the method no longer matches
        // the ILSize for the method when profile data was collected.
        //
        // We will discard the IBC data in this case
        //
        if (FAILED(hr) && (fgProfileBuffer != nullptr))
        {
            fgProfileData_ILSizeMismatch = true;
            fgProfileBuffer              = nullptr;
        }
#ifdef DEBUG
        // A successful result implies a non-NULL fgProfileBuffer
        //
        if (SUCCEEDED(hr))
        {
            assert(fgProfileBuffer != nullptr);
        }

        // A failed result implies a NULL fgProfileBuffer
        //   see implementation of Compiler::fgHaveProfileData()
        //
        if (FAILED(hr))
        {
            assert(fgProfileBuffer == nullptr);
        }
#endif
    }

    opts.compNeedStackProbes = false;

#ifdef DEBUG
    if (JitConfig.StackProbesOverride() != 0 || compStressCompile(STRESS_GENERIC_VARN, 5))
    {
        opts.compNeedStackProbes = true;
    }
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

        if ((opts.eeFlags & CORJIT_FLG_BBOPT) && fgHaveProfileData())
        {
            printf("OPTIONS: using real profile data\n");
        }

        if (fgProfileData_ILSizeMismatch)
        {
            printf("OPTIONS: discarded IBC profile data due to mismatch in ILSize\n");
        }

        if (opts.eeFlags & CORJIT_FLG_PREJIT)
        {
            printf("OPTIONS: Jit invoked for ngen\n");
        }
        printf("OPTIONS: Stack probing is %s\n", opts.compNeedStackProbes ? "ENABLED" : "DISABLED");
    }
#endif

    opts.compGCPollType = GCPOLL_NONE;
    if (opts.eeFlags & CORJIT_FLG_GCPOLL_CALLS)
    {
        opts.compGCPollType = GCPOLL_CALL;
    }
    else if (opts.eeFlags & CORJIT_FLG_GCPOLL_INLINE)
    {
        // make sure that the EE didn't set both flags.
        assert(opts.compGCPollType == GCPOLL_NONE);
        opts.compGCPollType = GCPOLL_INLINE;
    }
}

#ifdef DEBUG

void JitDump(const char* pcFormat, ...)
{
    va_list lst;
    va_start(lst, pcFormat);
    vflogf(jitstdout, pcFormat, lst);
    va_end(lst);
}

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
        if (verbose)
        {
            printf("JitStressModeNamesNot contains %ws\n", s_compStressModeNames[stressArea]);
        }
        doStress = false;
        goto _done;
    }

    // Does user explicitly set this STRESS_MODE through the command line?
    strStressModeNames = JitConfig.JitStressModeNames();
    if (strStressModeNames != nullptr)
    {
        if (wcsstr(strStressModeNames, s_compStressModeNames[stressArea]) != nullptr)
        {
            if (verbose)
            {
                printf("JitStressModeNames contains %ws\n", s_compStressModeNames[stressArea]);
            }
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

#ifdef DEBUGGING_SUPPORT
    if (opts.compScopeInfo)
#endif
    {
        eeGetVars();
    }

#ifdef DEBUGGING_SUPPORT
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

        JITDUMP("Debuggable code - Add new BB%02u to perform initialization of variables [%08X]\n", fgFirstBB->bbNum,
                dspPtr(fgFirstBB));
    }
#endif // DEBUGGING_SUPPORT

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

#ifdef DEBUGGING_SUPPORT
    if (opts.compDbgInfo)
#endif
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
    unsigned compileFlags;
    bool     theMinOptsValue;
    unsigned jitMinOpts;

    compileFlags = opts.eeFlags;

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
        unsigned methodCount     = Compiler::jitTotalMethodCompiled;
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

    if (compStressCompile(STRESS_MIN_OPTS, 5))
    {
        theMinOptsValue = true;
    }
    // For PREJIT we never drop down to MinOpts
    // unless unless CLFLG_MINOPT is set
    else if (!(compileFlags & CORJIT_FLG_PREJIT))
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
    if (!theMinOptsValue && !(compileFlags & CORJIT_FLG_PREJIT) &&
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

    if (opts.MinOpts() || opts.compDbgCode)
    {
        opts.compFlags &= ~CLFLG_MAXOPT;
        opts.compFlags |= CLFLG_MINOPT;
    }

    if (!compIsForInlining())
    {
        codeGen->setFramePointerRequired(false);
        codeGen->setFrameRequired(false);

        if (opts.MinOpts() || opts.compDbgCode)
        {
            codeGen->setFrameRequired(true);
        }

#if !defined(_TARGET_AMD64_)
        // The VM sets CORJIT_FLG_FRAMED for two reasons: (1) the COMPlus_JitFramed variable is set, or
        // (2) the function is marked "noinline". The reason for #2 is that people mark functions
        // noinline to ensure the show up on in a stack walk. But for AMD64, we don't need a frame
        // pointer for the frame to show up in stack walk.
        if (compileFlags & CORJIT_FLG_FRAMED)
            codeGen->setFrameRequired(true);
#endif

        if (compileFlags & CORJIT_FLG_RELOC)
        {
            codeGen->genAlignLoops = false; // loop alignment not supported for prejitted code

            // The zapper doesn't set CORJIT_FLG_ALIGN_LOOPS, and there is
            // no reason for it to set it as the JIT doesn't currently support loop alignment
            // for prejitted images. (The JIT doesn't know the final address of the code, hence
            // it can't align code based on unknown addresses.)
            assert((compileFlags & CORJIT_FLG_ALIGN_LOOPS) == 0);
        }
        else
        {
            codeGen->genAlignLoops = (compileFlags & CORJIT_FLG_ALIGN_LOOPS) != 0;
        }
    }

    info.compUnwrapContextful = !opts.MinOpts() && !opts.compDbgCode;

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

    if (opts.MinOpts())
    {
        // Have a recovery path in case we fail to reserve REG_OPT_RSVD and go
        // over the limit of SP and FP offset ranges due to large
        // temps.
        return true;
    }

    unsigned calleeSavedRegMaxSz = CALLEE_SAVED_REG_MAXSZ;
    if (compFloatingPointUsed)
    {
        calleeSavedRegMaxSz += CALLEE_SAVED_FLOAT_MAXSZ;
    }

    noway_assert(frameSize > calleeSavedRegMaxSz);

#if defined(_TARGET_ARM64_)

    // TODO-ARM64-CQ: update this!
    return true; // just always assume we'll need it, for now

#else  // _TARGET_ARM_

    // frame layout:
    //
    //         low addresses
    //                         inArgs               compArgSize
    //  origSP --->
    //  LR     --->
    //  R11    --->
    //                +        callee saved regs    CALLEE_SAVED_REG_MAXSZ   (32 bytes)
    //                     optional saved fp regs   16 * sizeof(float)       (64 bytes)
    //                -        lclSize
    //                             incl. TEMPS      MAX_SPILL_TEMP_SIZE
    //                +            incl. outArgs
    //  SP     --->
    //                -
    //          high addresses

    // With codeGen->isFramePointerRequired we use R11 to access incoming args with positive offsets
    // and use R11 to access LclVars with negative offsets in the non funclet or
    // main region we use SP with positive offsets. The limiting factor in the
    // codeGen->isFramePointerRequired case is that we need the offset to be less than or equal to 0x7C
    // for negative offsets, but positive offsets can be imm12 limited by vldr/vstr
    // using +/-imm8.
    //
    // Subtract 4 bytes for alignment of a local var because number of temps could
    // trigger a misaligned double or long.
    //
    unsigned maxR11ArgLimit = (compFloatingPointUsed ? 0x03FC : 0x0FFC);
    unsigned maxR11LclLimit = 0x0078;

    if (codeGen->isFramePointerRequired())
    {
        unsigned maxR11LclOffs = frameSize;
        unsigned maxR11ArgOffs = compArgSize + (2 * REGSIZE_BYTES);
        if (maxR11LclOffs > maxR11LclLimit || maxR11ArgOffs > maxR11ArgLimit)
        {
            return true;
        }
    }

    // So this case is the SP based frame case, but note that we also will use SP based
    // offsets for R11 based frames in the non-funclet main code area. However if we have
    // passed the above max_R11_offset check these SP checks won't fire.

    // Check local coverage first. If vldr/vstr will be used the limit can be +/-imm8.
    unsigned maxSPLclLimit = (compFloatingPointUsed ? 0x03F8 : 0x0FF8);
    if (frameSize > (codeGen->isFramePointerUsed() ? (maxR11LclLimit + maxSPLclLimit) : maxSPLclLimit))
    {
        return true;
    }

    // Check arguments coverage.
    if ((!codeGen->isFramePointerUsed() || (compArgSize > maxR11ArgLimit)) && (compArgSize + frameSize) > maxSPLclLimit)
    {
        return true;
    }

    // We won't need to reserve REG_OPT_RSVD.
    //
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
        printf("} Jitted Entry %03x at" FMT_ADDR "method %s size %08x%s\n", Compiler::jitTotalMethodCompiled,
               DBG_ADDR(methodCodePtr), info.compFullName, methodCodeSize,
               isNYI ? " NYI" : (compIsForImportOnly() ? " import only" : ""));
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
void Compiler::compCompile(void** methodCodePtr, ULONG* methodCodeSize, CORJIT_FLAGS* compileFlags)
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
        // Remove cheap predecessors before inlining; allowing the cheap predecessor lists to be inserted
        // with inlined blocks causes problems.
        fgRemovePreds();
    }

    if (compIsForInlining())
    {
        /* Quit inlining if fgImport() failed for any reason. */

        if (compDonotInline())
        {
            return;
        }

        /* Filter out unimported BBs */

        fgRemoveEmptyBlocks();

        return;
    }

    assert(!compDonotInline());

    EndPhase(PHASE_IMPORTATION);

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

    if (compileFlags->corJitFlags & CORJIT_FLG_BBINSTR)
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
    EndPhase(PHASE_MORPH);

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

    // Compute the edge weights (if we have profile data)
    fgComputeEdgeWeights();
    EndPhase(PHASE_COMPUTE_EDGE_WEIGHTS);

#if FEATURE_EH_FUNCLETS

    /* Create funclets from the EH handlers. */

    fgCreateFunclets();
    EndPhase(PHASE_CREATE_FUNCLETS);

#endif // FEATURE_EH_FUNCLETS

    if (!opts.MinOpts() && !opts.compDbgCode)
    {
        optOptimizeLayout();
        EndPhase(PHASE_OPTIMIZE_LAYOUT);

        // Compute reachability sets and dominators.
        fgComputeReachability();
    }

    // Transform each GT_ALLOCOBJ node into either an allocation helper call or
    // local variable allocation on the stack.
    ObjectAllocator objectAllocator(this);
    objectAllocator.Run();

    if (!opts.MinOpts() && !opts.compDbgCode)
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
    assert(lvaLocalVarRefCounted == true);

    if (!opts.MinOpts() && !opts.compDbgCode)
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
    if (!opts.MinOpts() && !opts.compDbgCode)
    {
        bool doSsa           = true;
        bool doEarlyProp     = true;
        bool doValueNum      = true;
        bool doLoopHoisting  = true;
        bool doCopyProp      = true;
        bool doAssertionProp = true;
        bool doRangeAnalysis = true;

#ifdef DEBUG
        doSsa           = (JitConfig.JitDoSsa() != 0);
        doEarlyProp     = doSsa && (JitConfig.JitDoEarlyProp() != 0);
        doValueNum      = doSsa && (JitConfig.JitDoValueNumber() != 0);
        doLoopHoisting  = doValueNum && (JitConfig.JitDoLoopHoisting() != 0);
        doCopyProp      = doValueNum && (JitConfig.JitDoCopyProp() != 0);
        doAssertionProp = doValueNum && (JitConfig.JitDoAssertionProp() != 0);
        doRangeAnalysis = doAssertionProp && (JitConfig.JitDoRangeAnalysis() != 0);
#endif

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
        compSizeEstimate = 0;
        compCycleEstimate = 0;
        for (BasicBlock* block = fgFirstBB; block != nullptr; block = block->bbNext)
        {
            for (GenTreeStmt* statement = block->firstStmt(); statement != nullptr; statement = statement->getNextStmt())
            {
                compSizeEstimate += statement->GetCostSz();
                compCycleEstimate += statement->GetCostEx();
            }
        }
    }
#endif

#ifndef LEGACY_BACKEND
    // TODO-CQ: Remove "if-block" when lowering doesn't rely on front end liveness.
    // Remove "if-block" to repro assert('!"We should never hit any assignment operator in lowering"')
    // using self_host_tests_amd64\JIT\Methodical\eh\finallyexec\tryCatchFinallyThrow_nonlocalexit1_d.exe
    if (!fgLocalVarLivenessDone)
    {
        fgLocalVarLiveness();
    }
    // rationalize trees
    Rationalizer rat(this); // PHASE_RATIONALIZE
    rat.Run();
#endif // !LEGACY_BACKEND

    // Here we do "simple lowering".  When the RyuJIT backend works for all
    // platforms, this will be part of the more general lowering phase.  For now, though, we do a separate
    // pass of "final lowering."  We must do this before (final) liveness analysis, because this creates
    // range check throw blocks, in which the liveness must be correct.
    fgSimpleLowering();
    EndPhase(PHASE_SIMPLE_LOWERING);

#ifdef LEGACY_BACKEND
    /* Local variable liveness */
    fgLocalVarLiveness();
    EndPhase(PHASE_LCLVARLIVENESS);
#endif // !LEGACY_BACKEND

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
#ifdef _TARGET_ARMARCH_
    if (compRsvdRegCheck(PRE_REGALLOC_FRAME_LAYOUT))
    {
        // We reserve R10/IP1 in this case to hold the offsets in load/store instructions
        codeGen->regSet.rsMaskResvd |= RBM_OPT_RSVD;
        assert(REG_OPT_RSVD != REG_FP);
    }

#ifdef DEBUG
    //
    // Display the pre-regalloc frame offsets that we have tentatively decided upon
    //
    if (verbose)
        lvaTableDump();
#endif
#endif // _TARGET_ARMARCH_

    /* Assign registers to variables, etc. */
    CLANG_FORMAT_COMMENT_ANCHOR;

#ifndef LEGACY_BACKEND
    ///////////////////////////////////////////////////////////////////////////////
    // Dominator and reachability sets are no longer valid. They haven't been
    // maintained up to here, and shouldn't be used (unless recomputed).
    ///////////////////////////////////////////////////////////////////////////////
    fgDomsComputed = false;

    /* Create LSRA before Lowering, this way Lowering can initialize the TreeNode Map */
    m_pLinearScan = getLinearScanAllocator(this);

    /* Lower */
    Lowering lower(this, m_pLinearScan); // PHASE_LOWERING
    lower.Run();

    assert(lvaSortAgain == false); // We should have re-run fgLocalVarLiveness() in lower.Run()
    lvaTrackedFixed = true;        // We can not add any new tracked variables after this point.

    /* Now that lowering is completed we can proceed to perform register allocation */
    m_pLinearScan->doLinearScan();
    EndPhase(PHASE_LINEAR_SCAN);

    // Copied from rpPredictRegUse()
    genFullPtrRegMap = (codeGen->genInterruptible || !codeGen->isFramePointerUsed());
#else  // LEGACY_BACKEND

    lvaTrackedFixed = true; // We cannot add any new tracked variables after this point.
    // For the classic JIT32 at this point lvaSortAgain can be set and raAssignVars() will call lvaSortOnly()

    // Now do "classic" register allocation.
    raAssignVars();
    EndPhase(PHASE_RA_ASSIGN_VARS);
#endif // LEGACY_BACKEND

#ifdef DEBUG
    fgDebugCheckLinks();
#endif

    /* Generate code */

    codeGen->genGenerateCode(methodCodePtr, methodCodeSize);

#ifdef FEATURE_JIT_METHOD_PERF
    if (pCompJitTimer)
        pCompJitTimer->Terminate(this, CompTimeSummaryInfo::s_compTimeSummary);
#endif

    RecordStateAtEndOfCompilation();

#ifdef FEATURE_TRACELOGGING
    compJitTelemetry.NotifyEndOfCompilation();
#endif

#if defined(DEBUG)
    ++Compiler::jitTotalMethodCompiled;
#endif // defined(DEBUG)

    compFunctionTraceEnd(*methodCodePtr, *methodCodeSize, false);

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
                          CORJIT_FLAGS*         compileFlags)
{
#ifdef FEATURE_JIT_METHOD_PERF
    static bool checkedForJitTimeLog = false;

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
    if ((Compiler::compJitTimeLogFilename != NULL) || (JitTimeLogCsv() != NULL))
    {
        pCompJitTimer = JitTimer::Create(this, methodInfo->ILCodeSize);
    }
    else
    {
        pCompJitTimer = NULL;
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
        }
    }
#endif // FUNC_INFO_LOGGING

    // if (s_compMethodsCount==0) setvbuf(jitstdout, NULL, _IONBF, 0);

    info.compCompHnd    = compHnd;
    info.compMethodHnd  = methodHnd;
    info.compMethodInfo = methodInfo;

    // Do we have a matched VM? Or are we "abusing" the VM to help us do JIT work (such as using an x86 native VM
    // with an ARM-targeting "altjit").
    info.compMatchedVM = IMAGE_FILE_MACHINE_TARGET == info.compCompHnd->getExpectedTargetArchitecture();

#if defined(ALT_JIT) && defined(UNIX_AMD64_ABI)
    // ToDo: This code is to allow us to run UNIX codegen on Windows for now. Remove when appropriate.
    // Make sure that the generated UNIX altjit code is skipped on Windows. The static jit codegen is used to run.
    info.compMatchedVM = false;
#endif // UNIX_AMD64_ABI

#if COR_JIT_EE_VERSION > 460
    compMaxUncheckedOffsetForNullObject = eeGetEEInfo()->maxUncheckedOffsetForNullObject;
#else  // COR_JIT_EE_VERSION <= 460
    compMaxUncheckedOffsetForNullObject = MAX_UNCHECKED_OFFSET_FOR_NULL_OBJECT;
#endif // COR_JIT_EE_VERSION > 460

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
    info.compClassName  = (char*)compGetMem(len, CMK_DebugOnly);
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
    tiVerificationNeeded = (compileFlags->corJitFlags & CORJIT_FLG_SKIP_VERIFICATION) == 0;

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
                    // is detected, it should pass in CORJIT_FLG_SKIP_VERIFICATION.
                    assert(!"The VM should have used CORJIT_FLG_SKIP_VERIFICATION");
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
        CORJIT_FLAGS*         compileFlags;

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
        // Grab the relevant lock.
        CritSecHolder statsLock(s_memStatsLock);

        // Make the updates.
        genMemStats.nraTotalSizeAlloc = compGetAllocator()->getTotalBytesAllocated();
        genMemStats.nraTotalSizeUsed  = compGetAllocator()->getTotalBytesUsed();
        s_aggMemStats.Add(genMemStats);
        if (genMemStats.allocSz > s_maxCompMemStats.allocSz)
        {
            s_maxCompMemStats = genMemStats;
        }
    }

#ifdef DEBUG
    if (s_dspMemStats || verbose)
    {
        printf("\nAllocations for %s (MethodHash=%08x)\n", info.compFullName, info.compMethodHash());
        genMemStats.Print(jitstdout);
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
        !compAllocator->bypassHostAllocator() && // ArenaAllocator::getDefaultPageSize() is artificially low for
                                                 // DirectAlloc
        (compAllocator->getTotalBytesAllocated() > (2 * ArenaAllocator::getDefaultPageSize())) &&
// Factor of 2x is because data-structures are bigger under DEBUG
#ifndef LEGACY_BACKEND
        // RyuJIT backend needs memory tuning! TODO-Cleanup: remove this case when memory tuning is complete.
        (compAllocator->getTotalBytesAllocated() > (10 * ArenaAllocator::getDefaultPageSize())) &&
#endif
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
        if (((opts.eeFlags & CORJIT_FLG_BBOPT) != 0) && fgHaveProfileData())
        {
            assert(fgProfileBuffer[0].ILOffset == 0);
            profCallCount = fgProfileBuffer[0].ExecutionCount;
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

#ifndef LEGACY_BACKEND
        printf(" LSRA    |"); // TODO-Cleanup: dump some interesting LSRA stat into the order file?
#else // LEGACY_BACKEND
        printf("%s%4d p%1d |", (tmpCount > 0) ? "T" : " ", rpStkPredict / BB_UNITY_WEIGHT, rpPasses);
#endif // LEGACY_BACKEND
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
    if ((opts.eeFlags & CORJIT_FLG_PREJIT) == 0)
    {
        if (compJitHaltMethod())
        {
#if !defined(_TARGET_ARM64_) && !defined(PLATFORM_UNIX)
            // TODO-ARM64-NYI: re-enable this when we have an OS that supports a pop-up dialog

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
                                CORJIT_FLAGS*                    compileFlags,
                                CorInfoInstantiationVerification instVerInfo)
{
    CORINFO_METHOD_HANDLE methodHnd = info.compMethodHnd;

    info.compCode       = methodInfo->ILCode;
    info.compILCodeSize = methodInfo->ILCodeSize;

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

    // Check for COMPlus_AgressiveInlining
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

#ifdef DEBUG
    compCurBB = nullptr;
    lvaTable  = nullptr;

    // Reset node ID counter
    compGenTreeID = 0;
#endif

    /* Initialize emitter */

    if (!compIsForInlining())
    {
        codeGen->getEmitter()->emitBegCG(this, compHnd);
    }

    info.compIsStatic = (info.compFlags & CORINFO_FLG_STATIC) != 0;

    info.compIsContextful = (info.compClassAttr & CORINFO_FLG_CONTEXTFUL) != 0;

    info.compPublishStubParam = (opts.eeFlags & CORJIT_FLG_PUBLISH_SECRET_PARAM) != 0;

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

#if FEATURE_FIXED_OUT_ARGS
    lvaOutgoingArgSpaceSize = 0;
#endif

    lvaGenericsContextUsed = false;

    info.compInitMem = ((methodInfo->options & CORINFO_OPT_INIT_LOCALS) != 0);

    /* Allocate the local variable table */

    lvaInitTypeRef();

    if (!compIsForInlining())
    {
        compInitDebuggingInfo();
    }

    const bool forceInline = !!(info.compFlags & CORINFO_FLG_FORCEINLINE);

    if (!compIsForInlining() && (opts.eeFlags & CORJIT_FLG_PREJIT))
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
        impInlineInfo->InlinerCompiler->compGenTreeID = compGenTreeID;
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

/*****************************************************************************/
#ifdef DEBUGGING_SUPPORT
/*****************************************************************************/

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

static int __cdecl genCmpLocalVarLifeBeg(const void* elem1, const void* elem2)
{
    return (*((VarScopeDsc**)elem1))->vsdLifeBeg - (*((VarScopeDsc**)elem2))->vsdLifeBeg;
}

static int __cdecl genCmpLocalVarLifeEnd(const void* elem1, const void* elem2)
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

/*****************************************************************************/
#endif // DEBUGGING_SUPPORT
/*****************************************************************************/

#if defined(DEBUGGING_SUPPORT) && defined(DEBUG)

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

#endif

#if defined(DEBUG)

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

#endif

/*****************************************************************************/

// Compile a single method

int jitNativeCode(CORINFO_METHOD_HANDLE methodHnd,
                  CORINFO_MODULE_HANDLE classPtr,
                  COMP_HANDLE           compHnd,
                  CORINFO_METHOD_INFO*  methodInfo,
                  void**                methodCodePtr,
                  ULONG*                methodCodeSize,
                  CORJIT_FLAGS*         compileFlags,
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

    if (inlineInfo)
    {
        // Use inliner's memory allocator when compiling the inlinee.
        pAlloc = inlineInfo->InlinerCompiler->compGetAllocator();
    }
    else
    {
        IEEMemoryManager* pMemoryManager = compHnd->getMemoryManager();

        // Try to reuse the pre-inited allocator
        pAlloc = ArenaAllocator::getPooledAllocator(pMemoryManager);

        if (pAlloc == nullptr)
        {
            alloc  = ArenaAllocator(pMemoryManager);
            pAlloc = &alloc;
        }
    }

    Compiler* pComp;
    pComp = nullptr;

    struct Param
    {
        Compiler*       pComp;
        ArenaAllocator* pAlloc;
        ArenaAllocator* alloc;
        bool            jitFallbackCompile;

        CORINFO_METHOD_HANDLE methodHnd;
        CORINFO_MODULE_HANDLE classPtr;
        COMP_HANDLE           compHnd;
        CORINFO_METHOD_INFO*  methodInfo;
        void**                methodCodePtr;
        ULONG*                methodCodeSize;
        CORJIT_FLAGS*         compileFlags;
        InlineInfo*           inlineInfo;

        int result;
    } param;
    param.pComp              = nullptr;
    param.pAlloc             = pAlloc;
    param.alloc              = &alloc;
    param.jitFallbackCompile = jitFallbackCompile;
    param.methodHnd          = methodHnd;
    param.classPtr           = classPtr;
    param.compHnd            = compHnd;
    param.methodInfo         = methodInfo;
    param.methodCodePtr      = methodCodePtr;
    param.methodCodeSize     = methodCodeSize;
    param.compileFlags       = compileFlags;
    param.inlineInfo         = inlineInfo;
    param.result             = result;

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
            // Add a dummy touch to pComp so that it is kept alive, and is easy to get to
            // during debugging since all other data can be obtained through it.
            //
            if (pParamOuter->pComp) // If OOM is thrown when allocating memory for pComp, we will end up here.
                                    // In that case, pComp is still NULL.
            {
                pParamOuter->pComp->info.compCode = nullptr;

                // pop the compiler off the TLS stack only if it was linked above
                assert(JitTls::GetCompiler() == pParamOuter->pComp);
                JitTls::SetCompiler(JitTls::GetCompiler()->prevCompiler);
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
        compileFlags->corJitFlags |= CORJIT_FLG_MIN_OPT;
        compileFlags->corJitFlags &= ~(CORJIT_FLG_SIZE_OPT | CORJIT_FLG_SPEED_OPT);

        goto START;
    }

    return result;
}

#if defined(FEATURE_UNIX_AMD64_STRUCT_PASSING)

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
#endif // defined(FEATURE_UNIX_AMD64_STRUCT_PASSING)

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
        for (GenTreePtr stmt = block->FirstNonPhiDef(); stmt != nullptr; stmt = stmt->gtNext)
        {
            for (GenTreePtr tree = stmt->gtStmt.gtStmtList; tree; tree = tree->gtNext)
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
                        GenTreePtr arg = args->Current();
                        if (arg->gtFlags & GTF_LATE_ARG)
                        {
                            // Find the corresponding late arg.
                            GenTreePtr lateArg = nullptr;
                            for (unsigned j = 0; j < call->fgArgInfo->ArgCount(); j++)
                            {
                                if (call->fgArgInfo->ArgTable()[j]->argNum == i)
                                {
                                    lateArg = call->fgArgInfo->ArgTable()[j]->node;
                                    break;
                                }
                            }
                            assert(lateArg != nullptr);
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

void Compiler::TransferTestDataToNode(GenTreePtr from, GenTreePtr to)
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

void Compiler::CopyTestDataToCloneTree(GenTreePtr from, GenTreePtr to)
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

        if (from->gtGetOp2() != nullptr)
        {
            assert(to->gtGetOp2() != nullptr);
            CopyTestDataToCloneTree(from->gtGetOp2(), to->gtGetOp2());
        }
        else
        {
            assert(to->gtGetOp2() == nullptr);
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
            CopyTestDataToCloneTree(from->gtBoundsChk.gtArrLen, to->gtBoundsChk.gtArrLen);
            CopyTestDataToCloneTree(from->gtBoundsChk.gtIndex, to->gtBoundsChk.gtIndex);
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
/*****************************************************************************/

/*****************************************************************************
 *
 *  If any temporary tables are smaller than 'genMinSize2free' we won't bother
 *  freeing them.
 */

const size_t genMinSize2free = 64;

/*****************************************************************************/

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
    GenTreePtr args;
    GenTreePtr argx;

    BasicBlock* block;
    GenTreePtr  stmt;
    GenTreePtr  call;

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

    for (block = fgFirstBB; block; block = block->bbNext)
    {
        for (stmt = block->bbTreeList; stmt; stmt = stmt->gtNext)
        {
            assert(stmt->gtOper == GT_STMT);

            for (call = stmt->gtStmt.gtStmtList; call; call = call->gtNext)
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

                    if (call->gtFlags & (GTF_CALL_VIRT_VTABLE | GTF_CALL_VIRT_STUB))
                    {
                        /* virtual function */
                        argVirtualCalls++;
                    }
                    else
                    {
                        argNonVirtualCalls++;
                    }
                }

#ifdef LEGACY_BACKEND
                // TODO-Cleaenup: We need to add support below for additional node types that RyuJIT backend has in the
                // IR.
                // Gather arguments information.

                for (args = call->gtCall.gtCallArgs; args; args = args->gtOp.gtOp2)
                {
                    argx = args->gtOp.gtOp1;

                    argNum++;

                    switch (genActualType(argx->TypeGet()))
                    {
                        case TYP_INT:
                        case TYP_REF:
                        case TYP_BYREF:
                            argDWordNum++;
                            break;

                        case TYP_LONG:
                            argLngNum++;
                            break;

                        case TYP_FLOAT:
                            argFltNum++;
                            break;

                        case TYP_DOUBLE:
                            argDblNum++;
                            break;

                        case TYP_VOID:
                            /* This is a deferred register argument */
                            assert(argx->gtOper == GT_NOP);
                            assert(argx->gtFlags & GTF_LATE_ARG);
                            argDWordNum++;
                            break;
                    }

                    /* Is this argument a register argument? */

                    if (argx->gtFlags & GTF_LATE_ARG)
                    {
                        regArgNum++;

                        /* We either have a deferred argument or a temp */

                        if (argx->gtOper == GT_NOP)
                        {
                            regArgDeferred++;
                        }
                        else
                        {
                            assert(argx->gtOper == GT_ASG);
                            regArgTemp++;
                        }
                    }
                }

                /* Look at the register arguments and count how many constants, local vars */

                for (args = call->gtCall.gtCallLateArgs; args; args = args->gtOp.gtOp2)
                {
                    argx = args->gtOp.gtOp1;

                    switch (argx->gtOper)
                    {
                        case GT_CNS_INT:
                            regArgConst++;
                            break;

                        case GT_LCL_VAR:
                            regArgLclVar++;
                            break;
                    }
                }

                assert(argNum == argDWordNum + argLngNum + argFltNum + argDblNum);
                assert(regArgNum == regArgDeferred + regArgTemp);

                argTotalArgs += argNum;
                argTotalRegArgs += regArgNum;

                argTotalDWordArgs += argDWordNum;
                argTotalLongArgs += argLngNum;
                argTotalFloatArgs += argFltNum;
                argTotalDoubleArgs += argDblNum;

                argTotalDeferred += regArgDeferred;
                argTotalTemps += regArgTemp;
                argTotalConst += regArgConst;
                argTotalLclVar += regArgLclVar;

                argTempsThisMethod += regArgTemp;

                argCntTable.record(argNum);
                argDWordCntTable.record(argDWordNum);
                argDWordLngCntTable.record(argDWordNum + (2 * argLngNum));
#endif // LEGACY_BACKEND
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
#endif // FEATURE_JIT_METHOD_PERF

#if defined(FEATURE_JIT_METHOD_PERF) || DUMP_FLOWGRAPHS
const char* PhaseNames[] = {
#define CompPhaseNameMacro(enum_nm, string_nm, short_nm, hasChildren, parent) string_nm,
#include "compphases.h"
};

const char* PhaseEnums[] = {
#define CompPhaseNameMacro(enum_nm, string_nm, short_nm, hasChildren, parent) #enum_nm,
#include "compphases.h"
};

const LPCWSTR PhaseShortNames[] = {
#define CompPhaseNameMacro(enum_nm, string_nm, short_nm, hasChildren, parent) W(short_nm),
#include "compphases.h"
};
#endif // defined(FEATURE_JIT_METHOD_PERF) || DUMP_FLOWGRAPHS

#ifdef FEATURE_JIT_METHOD_PERF
bool PhaseHasChildren[] = {
#define CompPhaseNameMacro(enum_nm, string_nm, short_nm, hasChildren, parent) hasChildren,
#include "compphases.h"
};

int PhaseParent[] = {
#define CompPhaseNameMacro(enum_nm, string_nm, short_nm, hasChildren, parent) parent,
#include "compphases.h"
};

CompTimeInfo::CompTimeInfo(unsigned byteCodeBytes)
    : m_byteCodeBytes(byteCodeBytes), m_totalCycles(0), m_parentPhaseEndSlop(0), m_timerFailure(false)
{
    for (int i = 0; i < PHASE_NUMBER_OF; i++)
    {
        m_invokesByPhase[i] = 0;
        m_cyclesByPhase[i]  = 0;
    }
}

bool CompTimeSummaryInfo::IncludedInFilteredData(CompTimeInfo& info)
{
    return false; // info.m_byteCodeBytes < 10;
}

void CompTimeSummaryInfo::AddInfo(CompTimeInfo& info)
{
    if (info.m_timerFailure)
        return; // Don't update if there was a failure.

    CritSecHolder timeLock(s_compTimeSummaryLock);
    m_numMethods++;

    bool includeInFiltered = IncludedInFilteredData(info);

    // Update the totals and maxima.
    m_total.m_byteCodeBytes += info.m_byteCodeBytes;
    m_maximum.m_byteCodeBytes = max(m_maximum.m_byteCodeBytes, info.m_byteCodeBytes);
    m_total.m_totalCycles += info.m_totalCycles;
    m_maximum.m_totalCycles = max(m_maximum.m_totalCycles, info.m_totalCycles);

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
        if (includeInFiltered)
        {
            m_filtered.m_invokesByPhase[i] += info.m_invokesByPhase[i];
            m_filtered.m_cyclesByPhase[i] += info.m_cyclesByPhase[i];
        }
        m_maximum.m_cyclesByPhase[i] = max(m_maximum.m_cyclesByPhase[i], info.m_cyclesByPhase[i]);
    }
    m_total.m_parentPhaseEndSlop += info.m_parentPhaseEndSlop;
    m_maximum.m_parentPhaseEndSlop = max(m_maximum.m_parentPhaseEndSlop, info.m_parentPhaseEndSlop);
}

// Static
LPCWSTR Compiler::compJitTimeLogFilename = NULL;

void CompTimeSummaryInfo::Print(FILE* f)
{
    if (f == NULL)
        return;
    // Otherwise...
    double countsPerSec = CycleTimer::CyclesPerSecond();
    if (countsPerSec == 0.0)
    {
        fprintf(f, "Processor does not have a high-frequency timer.\n");
        return;
    }

    fprintf(f, "JIT Compilation time report:\n");
    fprintf(f, "  Compiled %d methods.\n", m_numMethods);
    if (m_numMethods != 0)
    {
        fprintf(f, "  Compiled %d bytecodes total (%d max, %8.2f avg).\n", m_total.m_byteCodeBytes,
                m_maximum.m_byteCodeBytes, (double)m_total.m_byteCodeBytes / (double)m_numMethods);
        double totTime_ms = ((double)m_total.m_totalCycles / countsPerSec) * 1000.0;
        fprintf(f, "  Time: total: %10.3f Mcycles/%10.3f ms\n", ((double)m_total.m_totalCycles / 1000000.0),
                totTime_ms);
        fprintf(f, "          max: %10.3f Mcycles/%10.3f ms\n", ((double)m_maximum.m_totalCycles) / 1000000.0,
                ((double)m_maximum.m_totalCycles / countsPerSec) * 1000.0);
        fprintf(f, "          avg: %10.3f Mcycles/%10.3f ms\n",
                ((double)m_total.m_totalCycles) / 1000000.0 / (double)m_numMethods, totTime_ms / (double)m_numMethods);

        fprintf(f, "  Total time by phases:\n");
        fprintf(f, "     PHASE                            inv/meth Mcycles    time (ms)  %% of total    max (ms)\n");
        fprintf(f, "     --------------------------------------------------------------------------------------\n");
        // Ensure that at least the names array and the Phases enum have the same number of entries:
        assert(sizeof(PhaseNames) / sizeof(const char*) == PHASE_NUMBER_OF);
        for (int i = 0; i < PHASE_NUMBER_OF; i++)
        {
            double phase_tot_ms = (((double)m_total.m_cyclesByPhase[i]) / countsPerSec) * 1000.0;
            double phase_max_ms = (((double)m_maximum.m_cyclesByPhase[i]) / countsPerSec) * 1000.0;
            // Indent nested phases, according to depth.
            int ancPhase = PhaseParent[i];
            while (ancPhase != -1)
            {
                fprintf(f, "  ");
                ancPhase = PhaseParent[ancPhase];
            }
            fprintf(f, "     %-30s  %5.2f  %10.2f   %9.3f   %8.2f%%    %8.3f\n", PhaseNames[i],
                    ((double)m_total.m_invokesByPhase[i]) / ((double)m_numMethods),
                    ((double)m_total.m_cyclesByPhase[i]) / 1000000.0, phase_tot_ms, (phase_tot_ms * 100.0 / totTime_ms),
                    phase_max_ms);
        }
        fprintf(f, "\n  'End phase slop' should be very small (if not, there's unattributed time): %9.3f Mcycles.\n",
                m_total.m_parentPhaseEndSlop);
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
        assert(sizeof(PhaseNames) / sizeof(const char*) == PHASE_NUMBER_OF);
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
        fprintf(f, "\n  'End phase slop' should be very small (if not, there's unattributed time): %9.3f Mcycles.\n",
                m_filtered.m_parentPhaseEndSlop);
    }
}

JitTimer::JitTimer(unsigned byteCodeSize) : m_info(byteCodeSize)
{
#ifdef DEBUG
    m_lastPhase = (Phases)-1;
#endif

    unsigned __int64 threadCurCycles;
    if (GetThreadCycles(&threadCurCycles))
    {
        m_start         = threadCurCycles;
        m_curPhaseStart = threadCurCycles;
    }
}

void JitTimer::EndPhase(Phases phase)
{
    // Otherwise...
    // We re-run some phases currently, so this following assert doesn't work.
    // assert((int)phase > (int)m_lastPhase);  // We should end phases in increasing order.

    unsigned __int64 threadCurCycles;
    if (GetThreadCycles(&threadCurCycles))
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
            // Credit the phase's ancestors, if any.
            int ancPhase = PhaseParent[phase];
            while (ancPhase != -1)
            {
                m_info.m_cyclesByPhase[ancPhase] += phaseCycles;
                ancPhase = PhaseParent[ancPhase];
            }
            // Did we just end the last phase?
            if (phase + 1 == PHASE_NUMBER_OF)
            {
                m_info.m_totalCycles = (threadCurCycles - m_start);
            }
            else
            {
                m_curPhaseStart = threadCurCycles;
            }
        }
    }
#ifdef DEBUG
    m_lastPhase = phase;
#endif
}

CritSecObject JitTimer::s_csvLock;

LPCWSTR Compiler::JitTimeLogCsv()
{
    LPCWSTR jitTimeLogCsv = JitConfig.JitTimeLogCsv();
    return jitTimeLogCsv;
}

void JitTimer::PrintCsvHeader()
{
    LPCWSTR jitTimeLogCsv = Compiler::JitTimeLogCsv();
    if (jitTimeLogCsv == NULL)
    {
        return;
    }

    CritSecHolder csvLock(s_csvLock);

    if (_waccess(jitTimeLogCsv, 0) == -1)
    {
        // File doesn't exist, so create it and write the header

        // Use write mode, so we rewrite the file, and retain only the last compiled process/dll.
        // Ex: ngen install mscorlib won't print stats for "ngen" but for "mscorsvw"
        FILE* fp = _wfopen(jitTimeLogCsv, W("w"));
        fprintf(fp, "\"Method Name\",");
        fprintf(fp, "\"Method Index\",");
        fprintf(fp, "\"IL Bytes\",");
        fprintf(fp, "\"Basic Blocks\",");
        fprintf(fp, "\"Opt Level\",");
        fprintf(fp, "\"Loops Cloned\",");

        for (int i = 0; i < PHASE_NUMBER_OF; i++)
        {
            fprintf(fp, "\"%s\",", PhaseNames[i]);
        }

        InlineStrategy::DumpCsvHeader(fp);

        fprintf(fp, "\"Total Cycles\",");
        fprintf(fp, "\"CPS\"\n");
        fclose(fp);
    }
}

extern ICorJitHost* g_jitHost;

void JitTimer::PrintCsvMethodStats(Compiler* comp)
{
    LPCWSTR jitTimeLogCsv = Compiler::JitTimeLogCsv();
    if (jitTimeLogCsv == NULL)
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
    fprintf(fp, "%d,", index);
    fprintf(fp, "%u,", comp->info.compILCodeSize);
    fprintf(fp, "%u,", comp->fgBBcount);
    fprintf(fp, "%u,", comp->opts.MinOpts());
    fprintf(fp, "%u,", comp->optLoopsCloned);
    unsigned __int64 totCycles = 0;
    for (int i = 0; i < PHASE_NUMBER_OF; i++)
    {
        if (!PhaseHasChildren[i])
            totCycles += m_info.m_cyclesByPhase[i];
        fprintf(fp, "%I64u,", m_info.m_cyclesByPhase[i]);
    }

    comp->m_inlineStrategy->DumpCsvData(fp);

    fprintf(fp, "%I64u,", m_info.m_totalCycles);
    fprintf(fp, "%f\n", CycleTimer::CyclesPerSecond());
    fclose(fp);
}

// Completes the timing of the current method, and adds it to "sum".
void JitTimer::Terminate(Compiler* comp, CompTimeSummaryInfo& sum)
{
#ifdef DEBUG
    unsigned __int64 totCycles2 = 0;
    for (int i = 0; i < PHASE_NUMBER_OF; i++)
    {
        if (!PhaseHasChildren[i])
            totCycles2 += m_info.m_cyclesByPhase[i];
    }
    // We include m_parentPhaseEndSlop in the next phase's time also (we probably shouldn't)
    // totCycles2 += m_info.m_parentPhaseEndSlop;
    assert(totCycles2 == m_info.m_totalCycles);
#endif

    PrintCsvMethodStats(comp);

    sum.AddInfo(m_info);
}
#endif // FEATURE_JIT_METHOD_PERF

#if MEASURE_MEM_ALLOC
// static vars.
CritSecObject               Compiler::s_memStatsLock;    // Default constructor.
Compiler::AggregateMemStats Compiler::s_aggMemStats;     // Default constructor.
Compiler::MemStats          Compiler::s_maxCompMemStats; // Default constructor.

const char* Compiler::MemStats::s_CompMemKindNames[] = {
#define CompMemKindMacro(kind) #kind,
#include "compmemkind.h"
};

void Compiler::MemStats::Print(FILE* f)
{
    fprintf(f, "count: %10u, size: %10llu, max = %10llu\n", allocCnt, allocSz, allocSzMax);
    fprintf(f, "allocateMemory: %10llu, nraUsed: %10llu\n", nraTotalSizeAlloc, nraTotalSizeUsed);
    PrintByKind(f);
}

void Compiler::MemStats::PrintByKind(FILE* f)
{
    fprintf(f, "\nAlloc'd bytes by kind:\n  %20s | %10s | %7s\n", "kind", "size", "pct");
    fprintf(f, "  %20s-+-%10s-+-%7s\n", "--------------------", "----------", "-------");
    float allocSzF = static_cast<float>(allocSz);
    for (int cmk = 0; cmk < CMK_Count; cmk++)
    {
        float pct = 100.0f * static_cast<float>(allocSzByKind[cmk]) / allocSzF;
        fprintf(f, "  %20s | %10llu | %6.2f%%\n", s_CompMemKindNames[cmk], allocSzByKind[cmk], pct);
    }
    fprintf(f, "\n");
}

void Compiler::AggregateMemStats::Print(FILE* f)
{
    fprintf(f, "For %9u methods:\n", nMethods);
    fprintf(f, "  count:       %12u (avg %7u per method)\n", allocCnt, allocCnt / nMethods);
    fprintf(f, "  alloc size : %12llu (avg %7llu per method)\n", allocSz, allocSz / nMethods);
    fprintf(f, "  max alloc  : %12llu\n", allocSzMax);
    fprintf(f, "\n");
    fprintf(f, "  allocateMemory   : %12llu (avg %7llu per method)\n", nraTotalSizeAlloc, nraTotalSizeAlloc / nMethods);
    fprintf(f, "  nraUsed    : %12llu (avg %7llu per method)\n", nraTotalSizeUsed, nraTotalSizeUsed / nMethods);
    PrintByKind(f);
}
#endif // MEASURE_MEM_ALLOC

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

// dumpConvertedVarSet() is just like dumpVarSet(), except we assume the varset bits are tracked
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

    VARSET_ITER_INIT(comp, iter, vars, varIndex);
    while (iter.NextElem(comp, &varIndex))
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
 *      cBlock,      dBlock         : Display a basic block (call fgDispBasicBlock()).
 *      cBlocks,     dBlocks        : Display all the basic blocks of a function (call fgDispBasicBlocks()).
 *      cBlocksV,    dBlocksV       : Display all the basic blocks of a function (call fgDispBasicBlocks(true)).
 *                                    "V" means "verbose", and will dump all the trees.
 *      cTree,       dTree          : Display a tree (call gtDispTree()).
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
 *      dVarSet                     : Display a VARSET_TP (call dumpVarSet()).
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
        printf("BB%02u ", list->block->bbNum);
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
    printf("HEAD   BB%02u\n", blockHead->bbNum);
    printf("FIRST  BB%02u\n", blockFirst->bbNum);
    printf("TOP    BB%02u\n", blockTop->bbNum);
    printf("ENTRY  BB%02u\n", blockEntry->bbNum);
    if (loop->lpExitCnt == 1)
    {
        printf("EXIT   BB%02u\n", blockExit->bbNum);
    }
    else
    {
        printf("EXITS  %u", loop->lpExitCnt);
    }
    printf("BOTTOM BB%02u\n", blockBottom->bbNum);
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
        printf("BB%02u:\n", block->bbNum);
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
                GenTree* tree;

                foreach_treenode_execution_order(tree, stmt)
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
            chars += printf(" BB%02u", block->bbJumpDest->bbNum);
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
            chars += printf(" BB%02u", block->bbJumpDest->bbNum);
            if (block->bbFlags & BBF_KEEP_BBJ_ALWAYS)
            {
                chars += dTabStopIR(chars, COLUMN_KINDS);
                chars += printf("; [KEEP_BBJ_ALWAYS]");
            }
            break;

        case BBJ_LEAVE:
            chars += printf("BRANCH(LEAVE)");
            chars += dTabStopIR(chars, COLUMN_OPERANDS);
            chars += printf(" BB%02u", block->bbJumpDest->bbNum);
            break;

        case BBJ_CALLFINALLY:
            chars += printf("BRANCH(CALLFINALLY)");
            chars += dTabStopIR(chars, COLUMN_OPERANDS);
            chars += printf(" BB%02u", block->bbJumpDest->bbNum);
            break;

        case BBJ_COND:
            chars += printf("BRANCH(COND)");
            chars += dTabStopIR(chars, COLUMN_OPERANDS);
            chars += printf(" BB%02u", block->bbJumpDest->bbNum);
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
                chars += printf("%c BB%02u", (jumpTab == block->bbJumpSwt->bbsDstTab) ? ' ' : ',', (*jumpTab)->bbNum);
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
    if (kind & GTK_ASGOP)
    {
        chars += printf("[ASGOP]");
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
#if SMALL_TREE_NODES
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
#endif // SMALL_TREE_NODES
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
            case GT_REG_VAR:

                if (tree->gtFlags & GTF_VAR_DEF)
                {
                    chars += printf("[VAR_DEF]");
                }
                if (tree->gtFlags & GTF_VAR_USEASG)
                {
                    chars += printf("[VAR_USEASG]");
                }
                if (tree->gtFlags & GTF_VAR_USEDEF)
                {
                    chars += printf("[VAR_USEDEF]");
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
                if (op == GT_REG_VAR)
                {
                    if (tree->gtFlags & GTF_REG_BIRTH)
                    {
                        chars += printf("[REG_BIRTH]");
                    }
                }
                break;

            case GT_NOP:

                if (tree->gtFlags & GTF_NOP_DEATH)
                {
                    chars += printf("[NOP_DEATH]");
                }
                break;

            case GT_NO_OP:

                if (tree->gtFlags & GTF_NO_OP_NO)
                {
                    chars += printf("[NO_OP_NO]");
                }
                break;

            case GT_FIELD:

                if (tree->gtFlags & GTF_FLD_NULLCHECK)
                {
                    chars += printf("[FLD_NULLCHECK]");
                }
                if (tree->gtFlags & GTF_FLD_VOLATILE)
                {
                    chars += printf("[FLD_VOLATILE]");
                }
                break;

            case GT_INDEX:

                if (tree->gtFlags & GTF_INX_RNGCHK)
                {
                    chars += printf("[INX_RNGCHK]");
                }
                if (tree->gtFlags & GTF_INX_REFARR_LAYOUT)
                {
                    chars += printf("[INX_REFARR_LAYOUT]");
                }
                if (tree->gtFlags & GTF_INX_STRING_LAYOUT)
                {
                    chars += printf("[INX_STRING_LAYOUT]");
                }
                break;

            case GT_IND:
            case GT_STOREIND:

                if (tree->gtFlags & GTF_IND_VOLATILE)
                {
                    chars += printf("[IND_VOLATILE]");
                }
                if (tree->gtFlags & GTF_IND_REFARR_LAYOUT)
                {
                    chars += printf("[IND_REFARR_LAYOUT]");
                }
                if (tree->gtFlags & GTF_IND_TGTANYWHERE)
                {
                    chars += printf("[IND_TGTANYWHERE]");
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
                if (tree->gtFlags & GTF_IND_ARR_LEN)
                {
                    chars += printf("[IND_ARR_INDEX]");
                }
                break;

            case GT_CLS_VAR:

                if (tree->gtFlags & GTF_CLS_VAR_ASG_LHS)
                {
                    chars += printf("[CLS_VAR_ASG_LHS]");
                }
                break;

            case GT_ADDR:

                if (tree->gtFlags & GTF_ADDR_ONSTACK)
                {
                    chars += printf("[ADDR_ONSTACK]");
                }
                break;

            case GT_MUL:

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

                if (tree->gtFlags & GTF_MOD_INT_RESULT)
                {
                    chars += printf("[MOD_INT_RESULT]");
                }
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
                if (tree->gtFlags & GTF_RELOP_SMALL)
                {
                    chars += printf("[RELOP_SMALL]");
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

            case GT_COPYBLK:
            case GT_INITBLK:
            case GT_COPYOBJ:

                if (tree->AsBlkOp()->HasGCPtr())
                {
                    chars += printf("[BLK_HASGCPTR]");
                }
                if (tree->AsBlkOp()->IsVolatile())
                {
                    chars += printf("[BLK_VOLATILE]");
                }
                if (tree->AsBlkOp()->IsUnaligned())
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
                if (tree->gtFlags & GTF_CALL_NONVIRT)
                {
                    chars += printf("[CALL_NONVIRT]");
                }
                if (tree->gtFlags & GTF_CALL_VIRT_VTABLE)
                {
                    chars += printf("[CALL_VIRT_VTABLE]");
                }
                if (tree->gtFlags & GTF_CALL_VIRT_STUB)
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
                if (tree->gtFlags & GTF_CALL_REG_SAVE)
                {
                    chars += printf("[CALL_REG_SAVE]");
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
#ifndef LEGACY_BACKEND
                    if (call->gtCallMoreFlags & GTF_CALL_M_TAILCALL_VIA_HELPER)
                    {
                        chars += printf("[CALL_M_TAILCALL_VIA_HELPER]");
                    }
#endif
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
            case GT_ASG_ADD:
            case GT_ASG_SUB:
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
        if (tree->gtFlags & GTF_REG_VAL)
        {
            chars += printf("[REG_VAL]");
        }
        if (tree->gtFlags & GTF_SPILLED)
        {
            chars += printf("[SPILLED_OPER]");
        }
#if defined(LEGACY_BACKEND)
        if (tree->gtFlags & GTF_SPILLED_OP2)
        {
            chars += printf("[SPILLED_OP2]");
        }
#endif
        if (tree->gtFlags & GTF_ZSF_SET)
        {
            chars += printf("[ZSF_SET]");
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
            if ((op == GT_IND) || (op == GT_STOREIND))
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
        if (tree->gtFlags & GTF_SMALL_OK)
        {
            chars += printf("[SMALL_OK]");
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
        if (tree->gtFlags & GTF_SPILL_HIGH)
        {
            chars += printf("[SPILL_HIGH]");
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
            chars += printf(STR_VN "%x", vn);
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
            chars += printf(STR_VN "%x", vn);
            if (ValueNumStore::isReservedVN(vn))
            {
                chars += printf("R");
            }
            chars += printf(",");
            vn = vnp.GetConservative();
            chars += printf(STR_VN "%x", vn);
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
        case GT_REG_VAR:

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
                            if (isRegPairType(varDsc->TypeGet()))
                            {
                                chars += printf(":%s:%s",
                                                getRegName(varDsc->lvOtherReg), // hi32
                                                getRegName(varDsc->lvRegNum));  // lo32
                            }
                            else
                            {
                                chars += printf(":%s", getRegName(varDsc->lvRegNum));
                            }
                        }
                        else
                        {
                            switch (tree->GetRegTag())
                            {
                                case GenTree::GT_REGTAG_REG:
                                    chars += printf(":%s", comp->compRegVarName(tree->gtRegNum));
                                    break;
#if CPU_LONG_USES_REGPAIR
                                case GenTree::GT_REGTAG_REGPAIR:
                                    chars += printf(":%s", comp->compRegPairName(tree->gtRegPair));
                                    break;
#endif
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
                        chars += printf("(");
                        if (isRegPairType(varDsc->TypeGet()))
                        {
                            chars += printf("%s:%s",
                                            getRegName(varDsc->lvOtherReg), // hi32
                                            getRegName(varDsc->lvRegNum));  // lo32
                        }
                        else
                        {
                            chars += printf("%s", getRegName(varDsc->lvRegNum));
                        }
                        chars += printf(")");
                    }
                    else
                    {
                        switch (tree->GetRegTag())
                        {
                            case GenTree::GT_REGTAG_REG:
                                chars += printf("(%s)", comp->compRegVarName(tree->gtRegNum));
                                break;
#if CPU_LONG_USES_REGPAIR
                            case GenTree::GT_REGTAG_REGPAIR:
                                chars += printf("(%s)", comp->compRegPairName(tree->gtRegPair));
                                break;
#endif
                            default:
                                break;
                        }
                    }
                }
            }

            if (op == GT_REG_VAR)
            {
                if (isFloatRegType(tree->gtType))
                {
                    assert(tree->gtRegVar.gtRegNum == tree->gtRegNum);
                    chars += printf("(FPV%u)", tree->gtRegNum);
                }
                else
                {
                    chars += printf("(%s)", comp->compRegVarName(tree->gtRegVar.gtRegNum));
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
                            if (isRegPairType(varDsc->TypeGet()))
                            {
                                chars += printf(":%s:%s",
                                                getRegName(varDsc->lvOtherReg), // hi32
                                                getRegName(varDsc->lvRegNum));  // lo32
                            }
                            else
                            {
                                chars += printf(":%s", getRegName(varDsc->lvRegNum));
                            }
                        }
                        else
                        {
                            switch (tree->GetRegTag())
                            {
                                case GenTree::GT_REGTAG_REG:
                                    chars += printf(":%s", comp->compRegVarName(tree->gtRegNum));
                                    break;
#if CPU_LONG_USES_REGPAIR
                                case GenTree::GT_REGTAG_REGPAIR:
                                    chars += printf(":%s", comp->compRegPairName(tree->gtRegPair));
                                    break;
#endif
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
                        chars += printf("(");
                        if (isRegPairType(varDsc->TypeGet()))
                        {
                            chars += printf("%s:%s",
                                            getRegName(varDsc->lvOtherReg), // hi32
                                            getRegName(varDsc->lvRegNum));  // lo32
                        }
                        else
                        {
                            chars += printf("%s", getRegName(varDsc->lvRegNum));
                        }
                        chars += printf(")");
                    }
                    else
                    {
                        switch (tree->GetRegTag())
                        {
                            case GenTree::GT_REGTAG_REG:
                                chars += printf("(%s)", comp->compRegVarName(tree->gtRegNum));
                                break;
#if CPU_LONG_USES_REGPAIR
                            case GenTree::GT_REGTAG_REGPAIR:
                                chars += printf("(%s)", comp->compRegPairName(tree->gtRegPair));
                                break;
#endif
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
        case GT_PROF_HOOK:
        case GT_CATCH_ARG:
        case GT_MEMORYBARRIER:
        case GT_ARGPLACE:
        case GT_PINVOKE_PROLOG:
#ifndef LEGACY_BACKEND
        case GT_JMPTABLE:
#endif
            // Do nothing.
            break;

        case GT_RET_EXPR:

            chars += printf("t%d", tree->gtRetExpr.gtInlineCandidate->gtTreeID);
            break;

        case GT_PHYSREG:

            chars += printf("%s", getRegName(tree->gtPhysReg.gtSrcReg, varTypeIsFloating(tree)));
            break;

        case GT_LABEL:

            if (tree->gtLabel.gtLabBB)
            {
                chars += printf("BB%02u", tree->gtLabel.gtLabBB->bbNum);
            }
            else
            {
                chars += printf("BB?");
            }
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
    else if (dumpDataflow && (operand->OperIsAssignment() || (op == GT_STORE_LCL_VAR) || (op == GT_STORE_LCL_FLD)))
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
    if (dataflowView && tree->OperIsAssignment())
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
        if (tree->OperIsAssignment() || (op == GT_STORE_LCL_VAR) || (op == GT_STORE_LCL_FLD) || (op == GT_STOREIND))
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
            case CORINFO_INTRINSIC_Acos:
                chars += printf("Acos");
                break;
            case CORINFO_INTRINSIC_Atan:
                chars += printf("Atan");
                break;
            case CORINFO_INTRINSIC_Atan2:
                chars += printf("Atan2");
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

        case GT_STORE_CLS_VAR:

            chars += printf(" ???");
            break;

        case GT_LEA:

            GenTreeAddrMode* lea    = tree->AsAddrMode();
            GenTree*         base   = lea->Base();
            GenTree*         index  = lea->Index();
            unsigned         scale  = lea->gtScale;
            unsigned         offset = lea->gtOffset;

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
                chars += printf("%u", offset);
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
