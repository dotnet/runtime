// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XX                                                                           XX
XX                    Inline functions                                       XX
XX                                                                           XX
XX                                                                           XX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
*/

#ifndef _COMPILER_HPP_
#define _COMPILER_HPP_

#include "emit.h" // for emitter::emitAddLabel

#include "bitvec.h"

#include "compilerbitsettraits.hpp"

/*
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XX                                                                           XX
XX  Miscellaneous utility functions. Some of these are defined in Utils.cpp  XX
XX                                                                           XX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
*/

/*****************************************************************************/
/*****************************************************************************/

inline bool getInlinePInvokeEnabled()
{
#ifdef DEBUG
    return JitConfig.JitPInvokeEnabled() && !JitConfig.StressCOMCall();
#else
    return true;
#endif
}

inline bool getInlinePInvokeCheckEnabled()
{
#ifdef DEBUG
    return JitConfig.JitPInvokeCheckEnabled() != 0;
#else
    return false;
#endif
}

// Enforce float narrowing for buggy compilers (notably preWhidbey VC)
inline float forceCastToFloat(double d)
{
    Volatile<float> f = (float)d;
    return f;
}

// Enforce UInt32 narrowing for buggy compilers (notably Whidbey Beta 2 LKG)
inline UINT32 forceCastToUInt32(double d)
{
    Volatile<UINT32> u = (UINT32)d;
    return u;
}

enum RoundLevel
{
    ROUND_NEVER     = 0, // Never round
    ROUND_CMP_CONST = 1, // Round values compared against constants
    ROUND_CMP       = 2, // Round comparands and return values
    ROUND_ALWAYS    = 3, // Round always

    COUNT_ROUND_LEVEL,
    DEFAULT_ROUND_LEVEL = ROUND_NEVER
};

inline RoundLevel getRoundFloatLevel()
{
#ifdef DEBUG
    return (RoundLevel)JitConfig.JitRoundFloat();
#else
    return DEFAULT_ROUND_LEVEL;
#endif
}

/*****************************************************************************/
/*****************************************************************************
 *
 *  Return the lowest bit that is set
 */

template <typename T>
inline T genFindLowestBit(T value)
{
    return (value & (0 - value));
}

/*****************************************************************************/
/*****************************************************************************
 *
 *  Return the highest bit that is set (that is, a mask that includes just the highest bit).
 *  TODO-ARM64-Throughput: we should convert these to use the _BitScanReverse() / _BitScanReverse64()
 *  compiler intrinsics, but our CRT header file intrin.h doesn't define these for ARM64 yet.
 */

inline unsigned int genFindHighestBit(unsigned int mask)
{
    assert(mask != 0);
    unsigned int bit = 1U << ((sizeof(unsigned int) * 8) - 1); // start looking at the top
    while ((bit & mask) == 0)
    {
        bit >>= 1;
    }
    return bit;
}

inline unsigned __int64 genFindHighestBit(unsigned __int64 mask)
{
    assert(mask != 0);
    unsigned __int64 bit = 1ULL << ((sizeof(unsigned __int64) * 8) - 1); // start looking at the top
    while ((bit & mask) == 0)
    {
        bit >>= 1;
    }
    return bit;
}

#if 0
// TODO-ARM64-Cleanup: These should probably be the implementation, when intrin.h is updated for ARM64
inline
unsigned int genFindHighestBit(unsigned int mask)
{
    assert(mask != 0);
    unsigned int index;
    _BitScanReverse(&index, mask);
    return 1L << index;
}

inline
unsigned __int64 genFindHighestBit(unsigned __int64 mask)
{
    assert(mask != 0);
    unsigned int index;
    _BitScanReverse64(&index, mask);
    return 1LL << index;
}
#endif // 0

/*****************************************************************************
*
*  Return true if the given 64-bit value has exactly zero or one bits set.
*/

template <typename T>
inline BOOL genMaxOneBit(T value)
{
    return (value & (value - 1)) == 0;
}

/*****************************************************************************
*
*  Return true if the given 32-bit value has exactly zero or one bits set.
*/

inline BOOL genMaxOneBit(unsigned value)
{
    return (value & (value - 1)) == 0;
}

/*****************************************************************************
*
*  Return true if the given 64-bit value has exactly one bit set.
*/

template <typename T>
inline bool genExactlyOneBit(T value)
{
    return ((value != 0) && genMaxOneBit(value));
}

/*****************************************************************************
*
*  Return true if the given 32-bit value has exactly zero or one bits set.
*/

inline bool genExactlyOneBit(unsigned value)
{
    return ((value != 0) && genMaxOneBit(value));
}

/*****************************************************************************
 *
 *  Given a value that has exactly one bit set, return the position of that
 *  bit, in other words return the logarithm in base 2 of the given value.
 */
inline unsigned genLog2(unsigned value)
{
    return BitPosition(value);
}

// Given an unsigned 64-bit value, returns the lower 32-bits in unsigned format
//
inline unsigned ulo32(unsigned __int64 value)
{
    return static_cast<unsigned>(value);
}

// Given an unsigned 64-bit value, returns the upper 32-bits in unsigned format
//
inline unsigned uhi32(unsigned __int64 value)
{
    return static_cast<unsigned>(value >> 32);
}

/*****************************************************************************
 *
 *  Given a value that has exactly one bit set, return the position of that
 *  bit, in other words return the logarithm in base 2 of the given value.
 */

inline unsigned genLog2(unsigned __int64 value)
{
    unsigned lo32 = ulo32(value);
    unsigned hi32 = uhi32(value);

    if (lo32 != 0)
    {
        assert(hi32 == 0);
        return genLog2(lo32);
    }
    else
    {
        return genLog2(hi32) + 32;
    }
}

/*****************************************************************************
 *
 *  Return the lowest bit that is set in the given register mask.
 */

inline regMaskTP genFindLowestReg(regMaskTP value)
{
    return (regMaskTP)genFindLowestBit(value);
}

/*****************************************************************************
 *
 *  A rather simple routine that counts the number of bits in a given number.
 */

template <typename T>
inline unsigned genCountBits(T bits)
{
    unsigned cnt = 0;

    while (bits)
    {
        cnt++;
        bits -= genFindLowestBit(bits);
    }

    return cnt;
}

/*****************************************************************************
 *
 *  Given 3 masks value, end, start, returns the bits of value between start
 *  and end (exclusive).
 *
 *  value[bitNum(end) - 1, bitNum(start) + 1]
 */

inline unsigned __int64 BitsBetween(unsigned __int64 value, unsigned __int64 end, unsigned __int64 start)
{
    assert(start != 0);
    assert(start < end);
    assert((start & (start - 1)) == 0);
    assert((end & (end - 1)) == 0);

    return value & ~((start - 1) | start) & // Ones to the left of set bit in the start mask.
           (end - 1);                       // Ones to the right of set bit in the end mask.
}

/*****************************************************************************/

inline bool jitIsScaleIndexMul(size_t val)
{
    switch (val)
    {
        case 1:
        case 2:
        case 4:
        case 8:
            return true;

        default:
            return false;
    }
}

// Returns "tree" iff "val" is a valid addressing mode scale shift amount on
// the target architecture.
inline bool jitIsScaleIndexShift(ssize_t val)
{
    // It happens that this is the right test for all our current targets: x86, x64 and ARM.
    // This test would become target-dependent if we added a new target with a different constraint.
    return 0 < val && val < 4;
}

/*****************************************************************************
 * Returns true if value is between [start..end).
 * The comparison is inclusive of start, exclusive of end.
 */

/* static */
inline bool Compiler::jitIsBetween(unsigned value, unsigned start, unsigned end)
{
    return start <= value && value < end;
}

/*****************************************************************************
 * Returns true if value is between [start..end].
 * The comparison is inclusive of both start and end.
 */

/* static */
inline bool Compiler::jitIsBetweenInclusive(unsigned value, unsigned start, unsigned end)
{
    return start <= value && value <= end;
}

/******************************************************************************************
 * Return the EH descriptor for the given region index.
 */
inline EHblkDsc* Compiler::ehGetDsc(unsigned regionIndex)
{
    assert(regionIndex < compHndBBtabCount);
    return &compHndBBtab[regionIndex];
}

/******************************************************************************************
 * Return the EH descriptor index of the enclosing try, for the given region index.
 */
inline unsigned Compiler::ehGetEnclosingTryIndex(unsigned regionIndex)
{
    return ehGetDsc(regionIndex)->ebdEnclosingTryIndex;
}

/******************************************************************************************
 * Return the EH descriptor index of the enclosing handler, for the given region index.
 */
inline unsigned Compiler::ehGetEnclosingHndIndex(unsigned regionIndex)
{
    return ehGetDsc(regionIndex)->ebdEnclosingHndIndex;
}

/******************************************************************************************
 * Return the EH index given a region descriptor.
 */
inline unsigned Compiler::ehGetIndex(EHblkDsc* ehDsc)
{
    assert(compHndBBtab <= ehDsc && ehDsc < compHndBBtab + compHndBBtabCount);
    return (unsigned)(ehDsc - compHndBBtab);
}

/******************************************************************************************
 * Return the EH descriptor for the most nested 'try' region this BasicBlock is a member of
 * (or nullptr if this block is not in a 'try' region).
 */
inline EHblkDsc* Compiler::ehGetBlockTryDsc(BasicBlock* block)
{
    if (!block->hasTryIndex())
    {
        return nullptr;
    }

    return ehGetDsc(block->getTryIndex());
}

/******************************************************************************************
 * Return the EH descriptor for the most nested filter or handler region this BasicBlock is a member of
 * (or nullptr if this block is not in a filter or handler region).
 */
inline EHblkDsc* Compiler::ehGetBlockHndDsc(BasicBlock* block)
{
    if (!block->hasHndIndex())
    {
        return nullptr;
    }

    return ehGetDsc(block->getHndIndex());
}

#if defined(FEATURE_EH_FUNCLETS)

/*****************************************************************************
 *  Get the FuncInfoDsc for the funclet we are currently generating code for.
 *  This is only valid during codegen.
 *
 */
inline FuncInfoDsc* Compiler::funCurrentFunc()
{
    return funGetFunc(compCurrFuncIdx);
}

/*****************************************************************************
 *  Change which funclet we are currently generating code for.
 *  This is only valid after funclets are created.
 *
 */
inline void Compiler::funSetCurrentFunc(unsigned funcIdx)
{
    assert(fgFuncletsCreated);
    assert(FitsIn<unsigned short>(funcIdx));
    noway_assert(funcIdx < compFuncInfoCount);
    compCurrFuncIdx = (unsigned short)funcIdx;
}

/*****************************************************************************
 *  Get the FuncInfoDsc for the given funclet.
 *  This is only valid after funclets are created.
 *
 */
inline FuncInfoDsc* Compiler::funGetFunc(unsigned funcIdx)
{
    assert(fgFuncletsCreated);
    assert(funcIdx < compFuncInfoCount);
    return &compFuncInfos[funcIdx];
}

/*****************************************************************************
 *  Get the funcIdx for the EH funclet that begins with block.
 *  This is only valid after funclets are created.
 *  It is only valid for blocks marked with BBF_FUNCLET_BEG because
 *  otherwise we would have to do a more expensive check to determine
 *  if this should return the filter funclet or the filter handler funclet.
 *
 */
inline unsigned Compiler::funGetFuncIdx(BasicBlock* block)
{
    assert(fgFuncletsCreated);
    assert(block->bbFlags & BBF_FUNCLET_BEG);

    EHblkDsc*    eh      = ehGetDsc(block->getHndIndex());
    unsigned int funcIdx = eh->ebdFuncIndex;
    if (eh->ebdHndBeg != block)
    {
        // If this is a filter EH clause, but we want the funclet
        // for the filter (not the filter handler), it is the previous one
        noway_assert(eh->HasFilter());
        noway_assert(eh->ebdFilter == block);
        assert(funGetFunc(funcIdx)->funKind == FUNC_HANDLER);
        assert(funGetFunc(funcIdx)->funEHIndex == funGetFunc(funcIdx - 1)->funEHIndex);
        assert(funGetFunc(funcIdx - 1)->funKind == FUNC_FILTER);
        funcIdx--;
    }

    return funcIdx;
}

#else // !FEATURE_EH_FUNCLETS

/*****************************************************************************
 *  Get the FuncInfoDsc for the funclet we are currently generating code for.
 *  This is only valid during codegen.  For non-funclet platforms, this is
 *  always the root function.
 *
 */
inline FuncInfoDsc* Compiler::funCurrentFunc()
{
    return &compFuncInfoRoot;
}

/*****************************************************************************
 *  Change which funclet we are currently generating code for.
 *  This is only valid after funclets are created.
 *
 */
inline void Compiler::funSetCurrentFunc(unsigned funcIdx)
{
    assert(funcIdx == 0);
}

/*****************************************************************************
 *  Get the FuncInfoDsc for the givven funclet.
 *  This is only valid after funclets are created.
 *
 */
inline FuncInfoDsc* Compiler::funGetFunc(unsigned funcIdx)
{
    assert(funcIdx == 0);
    return &compFuncInfoRoot;
}

/*****************************************************************************
 *  No funclets, so always 0.
 *
 */
inline unsigned Compiler::funGetFuncIdx(BasicBlock* block)
{
    return 0;
}

#endif // !FEATURE_EH_FUNCLETS

//------------------------------------------------------------------------------
// genRegNumFromMask : Maps a single register mask to a register number.
//
// Arguments:
//    mask - the register mask
//
// Return Value:
//    The number of the register contained in the mask.
//
// Assumptions:
//    The mask contains one and only one register.

inline regNumber genRegNumFromMask(regMaskTP mask)
{
    assert(mask != 0); // Must have one bit set, so can't have a mask of zero

    /* Convert the mask to a register number */

    regNumber regNum = (regNumber)genLog2(mask);

    /* Make sure we got it right */

    assert(genRegMask(regNum) == mask);

    return regNum;
}

//------------------------------------------------------------------------------
// genSmallTypeCanRepresentValue: Checks if a value can be represented by a given small type.
//
// Arguments:
//    value - the value to check
//    type  - the type
//
// Return Value:
//    True if the value is representable, false otherwise.

inline bool genSmallTypeCanRepresentValue(var_types type, ssize_t value)
{
    switch (type)
    {
        case TYP_UBYTE:
        case TYP_BOOL:
            return FitsIn<UINT8>(value);
        case TYP_BYTE:
            return FitsIn<INT8>(value);
        case TYP_USHORT:
            return FitsIn<UINT16>(value);
        case TYP_SHORT:
            return FitsIn<INT16>(value);
        default:
            unreached();
    }
}

/*****************************************************************************
 *
 *  Return the size in bytes of the given type.
 */

extern const BYTE genTypeSizes[TYP_COUNT];

template <class T>
inline unsigned genTypeSize(T type)
{
    assert((unsigned)TypeGet(type) < _countof(genTypeSizes));

    return genTypeSizes[TypeGet(type)];
}

/*****************************************************************************
 *
 *  Return the "stack slot count" of the given type.
 *      returns 1 for 32-bit types and 2 for 64-bit types.
 */

extern const BYTE genTypeStSzs[TYP_COUNT];

inline unsigned genTypeStSz(var_types type)
{
    assert((unsigned)type < _countof(genTypeStSzs));

    return genTypeStSzs[type];
}

/*****************************************************************************
 *
 *  Return the number of registers required to hold a value of the given type.
 */

/*****************************************************************************
 *
 *  The following function maps a 'precise' type to an actual type as seen
 *  by the VM (for example, 'byte' maps to 'int').
 */

extern const BYTE genActualTypes[TYP_COUNT];

inline var_types genActualType(var_types type)
{
    /* Spot check to make certain the table is in synch with the enum */

    assert(genActualTypes[TYP_DOUBLE] == TYP_DOUBLE);
    assert(genActualTypes[TYP_REF] == TYP_REF);

    assert((unsigned)type < sizeof(genActualTypes));
    return (var_types)genActualTypes[type];
}

/*****************************************************************************/

inline var_types genUnsignedType(var_types type)
{
    /* Force signed types into corresponding unsigned type */

    switch (type)
    {
        case TYP_BYTE:
            type = TYP_UBYTE;
            break;
        case TYP_SHORT:
            type = TYP_USHORT;
            break;
        case TYP_INT:
            type = TYP_UINT;
            break;
        case TYP_LONG:
            type = TYP_ULONG;
            break;
        default:
            break;
    }

    return type;
}

/*****************************************************************************/

inline var_types genSignedType(var_types type)
{
    /* Force non-small unsigned type into corresponding signed type */
    /* Note that we leave the small types alone */

    switch (type)
    {
        case TYP_UINT:
            type = TYP_INT;
            break;
        case TYP_ULONG:
            type = TYP_LONG;
            break;
        default:
            break;
    }

    return type;
}

/*****************************************************************************
 *  Can this type be passed as a parameter in a register?
 */

inline bool isRegParamType(var_types type)
{
#if defined(TARGET_X86)
    return (type <= TYP_INT || type == TYP_REF || type == TYP_BYREF);
#else  // !TARGET_X86
    return true;
#endif // !TARGET_X86
}

#if defined(TARGET_AMD64) || defined(TARGET_ARM64)
/*****************************************************************************/
// Returns true if 'type' is a struct that can be enregistered for call args
//                         or can be returned by value in multiple registers.
//              if 'type' is not a struct the return value will be false.
//
// Arguments:
//    type      - the basic jit var_type for the item being queried
//    typeClass - the handle for the struct when 'type' is TYP_STRUCT
//    typeSize  - Out param (if non-null) is updated with the size of 'type'.
//    forReturn - this is true when we asking about a GT_RETURN context;
//                this is false when we are asking about an argument context
//    isVarArg  - whether or not this is a vararg fixed arg or variable argument
//              - if so on arm64 windows getArgTypeForStruct will ignore HFA
//              - types
//
inline bool Compiler::VarTypeIsMultiByteAndCanEnreg(
    var_types type, CORINFO_CLASS_HANDLE typeClass, unsigned* typeSize, bool forReturn, bool isVarArg)
{
    bool     result = false;
    unsigned size   = 0;

    if (varTypeIsStruct(type))
    {
        assert(typeClass != nullptr);
        size = info.compCompHnd->getClassSize(typeClass);
        if (forReturn)
        {
            structPassingKind howToReturnStruct;
            type = getReturnTypeForStruct(typeClass, &howToReturnStruct, size);
        }
        else
        {
            structPassingKind howToPassStruct;
            type = getArgTypeForStruct(typeClass, &howToPassStruct, isVarArg, size);
        }
        if (type != TYP_UNKNOWN)
        {
            result = true;
        }
    }
    else
    {
        size = genTypeSize(type);
    }

    if (typeSize != nullptr)
    {
        *typeSize = size;
    }

    return result;
}
#endif // TARGET_AMD64 || TARGET_ARM64

/*****************************************************************************/

#ifdef DEBUG

inline const char* varTypeGCstring(var_types type)
{
    switch (type)
    {
        case TYP_REF:
            return "gcr";
        case TYP_BYREF:
            return "byr";
        default:
            return "non";
    }
}

#endif

/*****************************************************************************/

const char* varTypeName(var_types);

/*****************************************************************************
 *
 *  Helpers to pull big-endian values out of a byte stream.
 */

inline unsigned genGetU1(const BYTE* addr)
{
    return addr[0];
}

inline signed genGetI1(const BYTE* addr)
{
    return (signed char)addr[0];
}

inline unsigned genGetU2(const BYTE* addr)
{
    return (addr[0] << 8) | addr[1];
}

inline signed genGetI2(const BYTE* addr)
{
    return (signed short)((addr[0] << 8) | addr[1]);
}

inline unsigned genGetU4(const BYTE* addr)
{
    return (addr[0] << 24) | (addr[1] << 16) | (addr[2] << 8) | addr[3];
}

/*****************************************************************************/
//  Helpers to pull little-endian values out of a byte stream.

inline unsigned __int8 getU1LittleEndian(const BYTE* ptr)
{
    return *(UNALIGNED unsigned __int8*)ptr;
}

inline unsigned __int16 getU2LittleEndian(const BYTE* ptr)
{
    return GET_UNALIGNED_VAL16(ptr);
}

inline unsigned __int32 getU4LittleEndian(const BYTE* ptr)
{
    return GET_UNALIGNED_VAL32(ptr);
}

inline signed __int8 getI1LittleEndian(const BYTE* ptr)
{
    return *(UNALIGNED signed __int8*)ptr;
}

inline signed __int16 getI2LittleEndian(const BYTE* ptr)
{
    return GET_UNALIGNED_VAL16(ptr);
}

inline signed __int32 getI4LittleEndian(const BYTE* ptr)
{
    return GET_UNALIGNED_VAL32(ptr);
}

inline signed __int64 getI8LittleEndian(const BYTE* ptr)
{
    return GET_UNALIGNED_VAL64(ptr);
}

inline float getR4LittleEndian(const BYTE* ptr)
{
    __int32 val = getI4LittleEndian(ptr);
    return *(float*)&val;
}

inline double getR8LittleEndian(const BYTE* ptr)
{
    __int64 val = getI8LittleEndian(ptr);
    return *(double*)&val;
}

/*****************************************************************************
 *
 *  Return the normalized index to use in the EXPSET_TP for the CSE with
 *  the given CSE index.
 *  Each GenTree has the following field:
 *    signed char       gtCSEnum;        // 0 or the CSE index (negated if def)
 *  So zero is reserved to mean this node is not a CSE
 *  and postive values indicate CSE uses and negative values indicate CSE defs.
 *  The caller of this method must pass a non-zero postive value.
 *  This precondition is checked by the assert on the first line of this method.
 */

inline unsigned int genCSEnum2bit(unsigned index)
{
    assert((index > 0) && (index <= EXPSET_SZ));

    return (index - 1);
}

#ifdef DEBUG
const char* genES2str(BitVecTraits* traits, EXPSET_TP set);
const char* refCntWtd2str(unsigned refCntWtd);
#endif

/*
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XX                          GenTree                                          XX
XX                      Inline functions                                     XX
XX                                                                           XX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
*/

void* GenTree::operator new(size_t sz, Compiler* comp, genTreeOps oper)
{
    size_t size = GenTree::s_gtNodeSizes[oper];

#if MEASURE_NODE_SIZE
    genNodeSizeStats.genTreeNodeCnt += 1;
    genNodeSizeStats.genTreeNodeSize += size;
    genNodeSizeStats.genTreeNodeActualSize += sz;

    genNodeSizeStatsPerFunc.genTreeNodeCnt += 1;
    genNodeSizeStatsPerFunc.genTreeNodeSize += size;
    genNodeSizeStatsPerFunc.genTreeNodeActualSize += sz;
#endif // MEASURE_NODE_SIZE

    assert(size >= sz);
    return comp->getAllocator(CMK_ASTNode).allocate<char>(size);
}

// GenTree constructor
inline GenTree::GenTree(genTreeOps oper, var_types type DEBUGARG(bool largeNode))
{
    gtOper     = oper;
    gtType     = type;
    gtFlags    = 0;
    gtLIRFlags = 0;
#ifdef DEBUG
    gtDebugFlags = 0;
#endif // DEBUG
#if FEATURE_ANYCSE
    gtCSEnum = NO_CSE;
#endif // FEATURE_ANYCSE
#if ASSERTION_PROP
    ClearAssertion();
#endif

    gtNext = nullptr;
    gtPrev = nullptr;
    SetRegNum(REG_NA);
    INDEBUG(gtRegTag = GT_REGTAG_NONE;)

    INDEBUG(gtCostsInitialized = false;)

#ifdef DEBUG
    size_t size = GenTree::s_gtNodeSizes[oper];
    if (size == TREE_NODE_SZ_SMALL && !largeNode)
    {
        gtDebugFlags |= GTF_DEBUG_NODE_SMALL;
    }
    else if (size == TREE_NODE_SZ_LARGE || largeNode)
    {
        gtDebugFlags |= GTF_DEBUG_NODE_LARGE;
    }
    else
    {
        assert(!"bogus node size");
    }
#endif

#if COUNT_AST_OPERS
    InterlockedIncrement(&s_gtNodeCounts[oper]);
#endif

#ifdef DEBUG
    gtSeqNum = 0;
    gtTreeID = JitTls::GetCompiler()->compGenTreeID++;
    gtVNPair.SetBoth(ValueNumStore::NoVN);
    gtRegTag   = GT_REGTAG_NONE;
    gtOperSave = GT_NONE;
#endif
}

/*****************************************************************************/

inline Statement* Compiler::gtNewStmt(GenTree* expr, IL_OFFSETX offset)
{
    Statement* stmt = new (this->getAllocator(CMK_ASTNode)) Statement(expr, offset DEBUGARG(compStatementID++));
    return stmt;
}

/*****************************************************************************/

inline GenTree* Compiler::gtNewOperNode(genTreeOps oper, var_types type, GenTree* op1, bool doSimplifications)
{
    assert((GenTree::OperKind(oper) & (GTK_UNOP | GTK_BINOP)) != 0);
    assert((GenTree::OperKind(oper) & GTK_EXOP) ==
           0); // Can't use this to construct any types that extend unary/binary operator.
    assert(op1 != nullptr || oper == GT_RETFILT || oper == GT_NOP || (oper == GT_RETURN && type == TYP_VOID));

    if (doSimplifications)
    {
        // We do some simplifications here.
        // If this gets to be too many, try a switch...
        // TODO-Cleanup: With the factoring out of array bounds checks, it should not be the
        // case that we need to check for the array index case here, but without this check
        // we get failures (see for example jit\Directed\Languages\Python\test_methods_d.exe)
        if (oper == GT_IND)
        {
            // IND(ADDR(IND(x)) == IND(x)
            if (op1->gtOper == GT_ADDR)
            {
                GenTreeUnOp* addr  = op1->AsUnOp();
                GenTree*     indir = addr->gtGetOp1();
                if (indir->OperIs(GT_IND) && ((indir->gtFlags & GTF_IND_ARR_INDEX) == 0))
                {
                    op1 = indir->AsIndir()->Addr();
                }
            }
        }
        else if (oper == GT_ADDR)
        {
            // if "x" is not an array index, ADDR(IND(x)) == x
            if (op1->gtOper == GT_IND && (op1->gtFlags & GTF_IND_ARR_INDEX) == 0)
            {
                return op1->AsOp()->gtOp1;
            }
            else
            {
                // Addr source can't be CSE-ed.
                op1->SetDoNotCSE();
            }
        }
    }

    GenTree* node = new (this, oper) GenTreeOp(oper, type, op1, nullptr);

    return node;
}

// Returns an opcode that is of the largest node size in use.
inline genTreeOps LargeOpOpcode()
{
    assert(GenTree::s_gtNodeSizes[GT_CALL] == TREE_NODE_SZ_LARGE);
    return GT_CALL;
}

/******************************************************************************
 *
 * Use to create nodes which may later be morphed to another (big) operator
 */

inline GenTree* Compiler::gtNewLargeOperNode(genTreeOps oper, var_types type, GenTree* op1, GenTree* op2)
{
    assert((GenTree::OperKind(oper) & (GTK_UNOP | GTK_BINOP)) != 0);
    // Can't use this to construct any types that extend unary/binary operator.
    assert((GenTree::OperKind(oper) & GTK_EXOP) == 0);
    assert(GenTree::s_gtNodeSizes[oper] == TREE_NODE_SZ_SMALL);
    // Allocate a large node
    GenTree* node = new (this, LargeOpOpcode()) GenTreeOp(oper, type, op1, op2 DEBUGARG(/*largeNode*/ true));
    return node;
}

/*****************************************************************************
 *
 *  allocates a integer constant entry that represents a handle (something
 *  that may need to be fixed up).
 */

inline GenTree* Compiler::gtNewIconHandleNode(size_t value, unsigned flags, FieldSeqNode* fields)
{
    GenTree* node;
    assert((flags & (GTF_ICON_HDL_MASK | GTF_ICON_FIELD_OFF)) != 0);

    // Interpret "fields == NULL" as "not a field."
    if (fields == nullptr)
    {
        fields = FieldSeqStore::NotAField();
    }

#if defined(LATE_DISASM)
    node = new (this, LargeOpOpcode()) GenTreeIntCon(TYP_I_IMPL, value, fields DEBUGARG(/*largeNode*/ true));
#else
    node = new (this, GT_CNS_INT) GenTreeIntCon(TYP_I_IMPL, value, fields);
#endif
    node->gtFlags |= flags;
    return node;
}

/*****************************************************************************
 *
 *  It may not be allowed to embed HANDLEs directly into the JITed code (for eg,
 *  as arguments to JIT helpers). Get a corresponding value that can be embedded.
 *  These are versions for each specific type of HANDLE
 */

inline GenTree* Compiler::gtNewIconEmbScpHndNode(CORINFO_MODULE_HANDLE scpHnd)
{
    void *embedScpHnd, *pEmbedScpHnd;

    embedScpHnd = (void*)info.compCompHnd->embedModuleHandle(scpHnd, &pEmbedScpHnd);

    assert((!embedScpHnd) != (!pEmbedScpHnd));

    return gtNewIconEmbHndNode(embedScpHnd, pEmbedScpHnd, GTF_ICON_SCOPE_HDL, scpHnd);
}

//-----------------------------------------------------------------------------

inline GenTree* Compiler::gtNewIconEmbClsHndNode(CORINFO_CLASS_HANDLE clsHnd)
{
    void *embedClsHnd, *pEmbedClsHnd;

    embedClsHnd = (void*)info.compCompHnd->embedClassHandle(clsHnd, &pEmbedClsHnd);

    assert((!embedClsHnd) != (!pEmbedClsHnd));

    return gtNewIconEmbHndNode(embedClsHnd, pEmbedClsHnd, GTF_ICON_CLASS_HDL, clsHnd);
}

//-----------------------------------------------------------------------------

inline GenTree* Compiler::gtNewIconEmbMethHndNode(CORINFO_METHOD_HANDLE methHnd)
{
    void *embedMethHnd, *pEmbedMethHnd;

    embedMethHnd = (void*)info.compCompHnd->embedMethodHandle(methHnd, &pEmbedMethHnd);

    assert((!embedMethHnd) != (!pEmbedMethHnd));

    return gtNewIconEmbHndNode(embedMethHnd, pEmbedMethHnd, GTF_ICON_METHOD_HDL, methHnd);
}

//-----------------------------------------------------------------------------

inline GenTree* Compiler::gtNewIconEmbFldHndNode(CORINFO_FIELD_HANDLE fldHnd)
{
    void *embedFldHnd, *pEmbedFldHnd;

    embedFldHnd = (void*)info.compCompHnd->embedFieldHandle(fldHnd, &pEmbedFldHnd);

    assert((!embedFldHnd) != (!pEmbedFldHnd));

    return gtNewIconEmbHndNode(embedFldHnd, pEmbedFldHnd, GTF_ICON_FIELD_HDL, fldHnd);
}

/*****************************************************************************/

//------------------------------------------------------------------------------
// gtNewHelperCallNode : Helper to create a call helper node.
//
//
// Arguments:
//    helper    - Call helper
//    type      - Type of the node
//    args      - Call args
//
// Return Value:
//    New CT_HELPER node

inline GenTreeCall* Compiler::gtNewHelperCallNode(unsigned helper, var_types type, GenTreeCall::Use* args)
{
    unsigned     flags  = s_helperCallProperties.NoThrow((CorInfoHelpFunc)helper) ? 0 : GTF_EXCEPT;
    GenTreeCall* result = gtNewCallNode(CT_HELPER, eeFindHelper(helper), type, args);
    result->gtFlags |= flags;

#if DEBUG
    // Helper calls are never candidates.

    result->gtInlineObservation = InlineObservation::CALLSITE_IS_CALL_TO_HELPER;
#endif

    return result;
}

//------------------------------------------------------------------------------
// gtNewRuntimeLookupHelperCallNode : Helper to create a runtime lookup call helper node.
//
//
// Arguments:
//    helper    - Call helper
//    type      - Type of the node
//    args      - Call args
//
// Return Value:
//    New CT_HELPER node

inline GenTreeCall* Compiler::gtNewRuntimeLookupHelperCallNode(CORINFO_RUNTIME_LOOKUP* pRuntimeLookup,
                                                               GenTree*                ctxTree,
                                                               void*                   compileTimeHandle)
{
    GenTree* argNode = gtNewIconEmbHndNode(pRuntimeLookup->signature, nullptr, GTF_ICON_TOKEN_HDL, compileTimeHandle);
    GenTreeCall::Use* helperArgs = gtNewCallArgs(ctxTree, argNode);

    return gtNewHelperCallNode(pRuntimeLookup->helper, TYP_I_IMPL, helperArgs);
}

//------------------------------------------------------------------------
// gtNewAllocObjNode: A little helper to create an object allocation node.
//
// Arguments:
//    helper               - Value returned by ICorJitInfo::getNewHelper
//    helperHasSideEffects - True iff allocation helper has side effects
//    clsHnd               - Corresponding class handle
//    type                 - Tree return type (e.g. TYP_REF)
//    op1                  - Node containing an address of VtablePtr
//
// Return Value:
//    Returns GT_ALLOCOBJ node that will be later morphed into an
//    allocation helper call or local variable allocation on the stack.

inline GenTreeAllocObj* Compiler::gtNewAllocObjNode(
    unsigned int helper, bool helperHasSideEffects, CORINFO_CLASS_HANDLE clsHnd, var_types type, GenTree* op1)
{
    GenTreeAllocObj* node = new (this, GT_ALLOCOBJ) GenTreeAllocObj(type, helper, helperHasSideEffects, clsHnd, op1);
    return node;
}

//------------------------------------------------------------------------
// gtNewRuntimeLookup: Helper to create a runtime lookup node
//
// Arguments:
//    hnd - generic handle being looked up
//    hndTyp - type of the generic handle
//    tree - tree for the lookup
//
// Return Value:
//    New GenTreeRuntimeLookup node.

inline GenTree* Compiler::gtNewRuntimeLookup(CORINFO_GENERIC_HANDLE hnd, CorInfoGenericHandleType hndTyp, GenTree* tree)
{
    assert(tree != nullptr);
    GenTree* node = new (this, GT_RUNTIMELOOKUP) GenTreeRuntimeLookup(hnd, hndTyp, tree);
    return node;
}

/*****************************************************************************
 *
 *  A little helper to create a data member reference node.
 */

inline GenTree* Compiler::gtNewFieldRef(var_types typ, CORINFO_FIELD_HANDLE fldHnd, GenTree* obj, DWORD offset)
{
    /* 'GT_FIELD' nodes may later get transformed into 'GT_IND' */
    assert(GenTree::s_gtNodeSizes[GT_IND] <= GenTree::s_gtNodeSizes[GT_FIELD]);

    if (typ == TYP_STRUCT)
    {
        CORINFO_CLASS_HANDLE fieldClass;
        (void)info.compCompHnd->getFieldType(fldHnd, &fieldClass);
        typ = impNormStructType(fieldClass);
    }
    GenTree* tree = new (this, GT_FIELD) GenTreeField(typ, obj, fldHnd, offset);

    // If "obj" is the address of a local, note that a field of that struct local has been accessed.
    if (obj != nullptr && obj->OperGet() == GT_ADDR && varTypeIsStruct(obj->AsOp()->gtOp1) &&
        obj->AsOp()->gtOp1->OperGet() == GT_LCL_VAR)
    {
        unsigned lclNum                  = obj->AsOp()->gtOp1->AsLclVarCommon()->GetLclNum();
        lvaTable[lclNum].lvFieldAccessed = 1;
#if defined(TARGET_AMD64) || defined(TARGET_ARM64)
        // These structs are passed by reference; we should probably be able to treat these
        // as non-global refs, but downstream logic expects these to be marked this way.
        if (lvaTable[lclNum].lvIsParam)
        {
            tree->gtFlags |= GTF_GLOB_REF;
        }
#endif // defined(TARGET_AMD64) || defined(TARGET_ARM64)
    }
    else
    {
        tree->gtFlags |= GTF_GLOB_REF;
    }

    return tree;
}

/*****************************************************************************
 *
 *  A little helper to create an array index node.
 */

inline GenTree* Compiler::gtNewIndexRef(var_types typ, GenTree* arrayOp, GenTree* indexOp)
{
    GenTreeIndex* gtIndx = new (this, GT_INDEX) GenTreeIndex(typ, arrayOp, indexOp, genTypeSize(typ));

    return gtIndx;
}

//------------------------------------------------------------------------------
// gtNewArrLen : Helper to create an array length node.
//
//
// Arguments:
//    typ      -  Type of the node
//    arrayOp  -  Array node
//    lenOffset - Offset of the length field
//    block     - Basic block that will contain the result
//
// Return Value:
//    New GT_ARR_LENGTH node

inline GenTreeArrLen* Compiler::gtNewArrLen(var_types typ, GenTree* arrayOp, int lenOffset, BasicBlock* block)
{
    GenTreeArrLen* arrLen = new (this, GT_ARR_LENGTH) GenTreeArrLen(typ, arrayOp, lenOffset);
    static_assert_no_msg(GTF_ARRLEN_NONFAULTING == GTF_IND_NONFAULTING);
    arrLen->SetIndirExceptionFlags(this);
    if (block != nullptr)
    {
        block->bbFlags |= BBF_HAS_IDX_LEN;
    }
    optMethodFlags |= OMF_HAS_ARRAYREF;
    return arrLen;
}

//------------------------------------------------------------------------------
// gtNewIndir : Helper to create an indirection node.
//
// Arguments:
//    typ   -  Type of the node
//    addr  -  Address of the indirection
//
// Return Value:
//    New GT_IND node

inline GenTree* Compiler::gtNewIndir(var_types typ, GenTree* addr)
{
    GenTree* indir = gtNewOperNode(GT_IND, typ, addr);
    indir->SetIndirExceptionFlags(this);
    return indir;
}

//------------------------------------------------------------------------------
// gtNewNullCheck : Helper to create a null check node.
//
// Arguments:
//    addr        -  Address to null check
//    basicBlock  -  Basic block of the node
//
// Return Value:
//    New GT_NULLCHECK node

inline GenTree* Compiler::gtNewNullCheck(GenTree* addr, BasicBlock* basicBlock)
{
    GenTree* nullCheck = gtNewOperNode(GT_NULLCHECK, TYP_BYTE, addr);
    nullCheck->gtFlags |= GTF_EXCEPT;
    basicBlock->bbFlags |= BBF_HAS_NULLCHECK;
    optMethodFlags |= OMF_HAS_NULLCHECK;
    return nullCheck;
}

/*****************************************************************************
 *
 *  Create (and check for) a "nothing" node, i.e. a node that doesn't produce
 *  any code. We currently use a "nop" node of type void for this purpose.
 */

inline GenTree* Compiler::gtNewNothingNode()
{
    return new (this, GT_NOP) GenTreeOp(GT_NOP, TYP_VOID);
}
/*****************************************************************************/

inline bool GenTree::IsNothingNode() const
{
    return (gtOper == GT_NOP && gtType == TYP_VOID);
}

/*****************************************************************************
 *
 *  Change the given node to a NOP - May be later changed to a GT_COMMA
 *
 *****************************************************************************/

inline void GenTree::gtBashToNOP()
{
    ChangeOper(GT_NOP);

    gtType        = TYP_VOID;
    AsOp()->gtOp1 = AsOp()->gtOp2 = nullptr;

    gtFlags &= ~(GTF_ALL_EFFECT | GTF_REVERSE_OPS);
}

// return new arg placeholder node.  Does not do anything but has a type associated
// with it so we can keep track of register arguments in lists associated w/ call nodes

inline GenTree* Compiler::gtNewArgPlaceHolderNode(var_types type, CORINFO_CLASS_HANDLE clsHnd)
{
    GenTree* node = new (this, GT_ARGPLACE) GenTreeArgPlace(type, clsHnd);
    return node;
}

/*****************************************************************************/

inline GenTree* Compiler::gtUnusedValNode(GenTree* expr)
{
    return gtNewOperNode(GT_COMMA, TYP_VOID, expr, gtNewNothingNode());
}

/*****************************************************************************
 *
 * A wrapper for gtSetEvalOrder and gtComputeFPlvls
 * Necessary because the FP levels may need to be re-computed if we reverse
 * operands
 */

inline void Compiler::gtSetStmtInfo(Statement* stmt)
{
    GenTree* expr = stmt->GetRootNode();

    /* Recursively process the expression */

    gtSetEvalOrder(expr);
}

/*****************************************************************************/

inline void GenTree::SetOper(genTreeOps oper, ValueNumberUpdate vnUpdate)
{
    assert(((gtDebugFlags & GTF_DEBUG_NODE_SMALL) != 0) != ((gtDebugFlags & GTF_DEBUG_NODE_LARGE) != 0));

    /* Make sure the node isn't too small for the new operator */

    assert(GenTree::s_gtNodeSizes[gtOper] == TREE_NODE_SZ_SMALL ||
           GenTree::s_gtNodeSizes[gtOper] == TREE_NODE_SZ_LARGE);

    assert(GenTree::s_gtNodeSizes[oper] == TREE_NODE_SZ_SMALL || GenTree::s_gtNodeSizes[oper] == TREE_NODE_SZ_LARGE);
    assert(GenTree::s_gtNodeSizes[oper] == TREE_NODE_SZ_SMALL || (gtDebugFlags & GTF_DEBUG_NODE_LARGE));

#if defined(HOST_64BIT) && !defined(TARGET_64BIT)
    if (gtOper == GT_CNS_LNG && oper == GT_CNS_INT)
    {
        // When casting from LONG to INT, we need to force cast of the value,
        // if the host architecture represents INT and LONG with the same data size.
        AsLngCon()->gtLconVal = (INT64)(INT32)AsLngCon()->gtLconVal;
    }
#endif // defined(HOST_64BIT) && !defined(TARGET_64BIT)

    SetOperRaw(oper);

#ifdef DEBUG
    // Maintain the invariant that unary operators always have NULL gtOp2.
    // If we ever start explicitly allocating GenTreeUnOp nodes, we wouldn't be
    // able to do that (but if we did, we'd have to have a check in GetOp() -- perhaps
    // a gtUnOp...)
    if (OperKind(oper) == GTK_UNOP)
    {
        AsOp()->gtOp2 = nullptr;
    }
#endif // DEBUG

#if DEBUGGABLE_GENTREE
    // Until we eliminate SetOper/ChangeOper, we also change the vtable of the node, so that
    // it shows up correctly in the debugger.
    SetVtableForOper(oper);
#endif // DEBUGGABLE_GENTREE

    if (oper == GT_CNS_INT)
    {
        AsIntCon()->gtFieldSeq = nullptr;
    }

#if defined(TARGET_ARM)
    if (oper == GT_MUL_LONG)
    {
        // We sometimes bash GT_MUL to GT_MUL_LONG, which converts it from GenTreeOp to GenTreeMultiRegOp.
        AsMultiRegOp()->gtOtherReg = REG_NA;
        AsMultiRegOp()->ClearOtherRegFlags();
    }
#endif

    if (vnUpdate == CLEAR_VN)
    {
        // Clear the ValueNum field as well.
        gtVNPair.SetBoth(ValueNumStore::NoVN);
    }
}

inline GenTreeCast* Compiler::gtNewCastNode(var_types typ, GenTree* op1, bool fromUnsigned, var_types castType)
{
    GenTreeCast* res = new (this, GT_CAST) GenTreeCast(typ, op1, fromUnsigned, castType);
    return res;
}

inline GenTreeCast* Compiler::gtNewCastNodeL(var_types typ, GenTree* op1, bool fromUnsigned, var_types castType)
{
    /* Some casts get transformed into 'GT_CALL' or 'GT_IND' nodes */

    assert(GenTree::s_gtNodeSizes[GT_CALL] >= GenTree::s_gtNodeSizes[GT_CAST]);
    assert(GenTree::s_gtNodeSizes[GT_CALL] >= GenTree::s_gtNodeSizes[GT_IND]);

    /* Make a big node first and then change it to be GT_CAST */

    GenTreeCast* res =
        new (this, LargeOpOpcode()) GenTreeCast(typ, op1, fromUnsigned, castType DEBUGARG(/*largeNode*/ true));
    return res;
}

/*****************************************************************************/

/*****************************************************************************/

inline void GenTree::SetOperRaw(genTreeOps oper)
{
    // Please do not do anything here other than assign to gtOper (debug-only
    // code is OK, but should be kept to a minimum).
    RecordOperBashing(OperGet(), oper); // nop unless NODEBASH_STATS is enabled
    gtOper = oper;
}

inline void GenTree::SetOperResetFlags(genTreeOps oper)
{
    SetOper(oper);
    gtFlags &= GTF_NODE_MASK;
}

inline void GenTree::ChangeOperConst(genTreeOps oper)
{
#ifdef TARGET_64BIT
    assert(oper != GT_CNS_LNG); // We should never see a GT_CNS_LNG for a 64-bit target!
#endif
    assert(OperIsConst(oper)); // use ChangeOper() instead
    SetOperResetFlags(oper);
    // Some constant subtypes have additional fields that must be initialized.
    if (oper == GT_CNS_INT)
    {
        AsIntCon()->gtFieldSeq = FieldSeqStore::NotAField();
    }
}

inline void GenTree::ChangeOper(genTreeOps oper, ValueNumberUpdate vnUpdate)
{
    assert(!OperIsConst(oper)); // use ChangeOperConst() instead

    unsigned mask = GTF_COMMON_MASK;
    if (this->OperIsIndirOrArrLength() && OperIsIndirOrArrLength(oper))
    {
        mask |= GTF_IND_NONFAULTING;
    }
    SetOper(oper, vnUpdate);
    gtFlags &= mask;

    // Do "oper"-specific initializations...
    switch (oper)
    {
        case GT_LCL_FLD:
        {
            // The original GT_LCL_VAR might be annotated with a zeroOffset field.
            FieldSeqNode* zeroFieldSeq = nullptr;
            Compiler*     compiler     = JitTls::GetCompiler();
            bool          isZeroOffset = compiler->GetZeroOffsetFieldMap()->Lookup(this, &zeroFieldSeq);

            AsLclFld()->SetLclOffs(0);
            AsLclFld()->SetFieldSeq(FieldSeqStore::NotAField());

            if (zeroFieldSeq != nullptr)
            {
                // Set the zeroFieldSeq in the GT_LCL_FLD node
                AsLclFld()->SetFieldSeq(zeroFieldSeq);
                // and remove the annotation from the ZeroOffsetFieldMap
                compiler->GetZeroOffsetFieldMap()->Remove(this);
            }
            break;
        }
        default:
            break;
    }
}

inline void GenTree::ChangeOperUnchecked(genTreeOps oper)
{
    unsigned mask = GTF_COMMON_MASK;
    if (this->OperIsIndirOrArrLength() && OperIsIndirOrArrLength(oper))
    {
        mask |= GTF_IND_NONFAULTING;
    }
    SetOperRaw(oper); // Trust the caller and don't use SetOper()
    gtFlags &= mask;
}

/*****************************************************************************
 *
 * Returns true if the node is of the "ovf" variety, for example, add.ovf.i1.
 * + gtOverflow() can only be called for valid operators (that is, we know it is one
 *   of the operators which may have GTF_OVERFLOW set).
 * + gtOverflowEx() is more expensive, and should be called only if gtOper may be
 *   an operator for which GTF_OVERFLOW is invalid.
 */

inline bool GenTree::gtOverflow() const
{
    assert(OperMayOverflow());

    if ((gtFlags & GTF_OVERFLOW) != 0)
    {
        assert(varTypeIsIntegral(TypeGet()));

        return true;
    }
    else
    {
        return false;
    }
}

inline bool GenTree::gtOverflowEx() const
{
    return OperMayOverflow() && gtOverflow();
}

/*
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XX                          LclVarsInfo                                      XX
XX                      Inline functions                                     XX
XX                                                                           XX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
*/

inline bool Compiler::lvaHaveManyLocals() const
{
    return (lvaCount >= lclMAX_TRACKED);
}

/*****************************************************************************
 *
 *  Allocate a temporary variable or a set of temp variables.
 */

inline unsigned Compiler::lvaGrabTemp(bool shortLifetime DEBUGARG(const char* reason))
{
    if (compIsForInlining())
    {
        // Grab the temp using Inliner's Compiler instance.
        Compiler* pComp = impInlineInfo->InlinerCompiler; // The Compiler instance for the caller (i.e. the inliner)

        if (pComp->lvaHaveManyLocals())
        {
            // Don't create more LclVar with inlining
            compInlineResult->NoteFatal(InlineObservation::CALLSITE_TOO_MANY_LOCALS);
        }

        unsigned tmpNum = pComp->lvaGrabTemp(shortLifetime DEBUGARG(reason));
        lvaTable        = pComp->lvaTable;
        lvaCount        = pComp->lvaCount;
        lvaTableCnt     = pComp->lvaTableCnt;
        return tmpNum;
    }

    // You cannot allocate more space after frame layout!
    noway_assert(lvaDoneFrameLayout < Compiler::TENTATIVE_FRAME_LAYOUT);

    /* Check if the lvaTable has to be grown */
    if (lvaCount + 1 > lvaTableCnt)
    {
        unsigned newLvaTableCnt = lvaCount + (lvaCount / 2) + 1;

        // Check for overflow
        if (newLvaTableCnt <= lvaCount)
        {
            IMPL_LIMITATION("too many locals");
        }

        LclVarDsc* newLvaTable = getAllocator(CMK_LvaTable).allocate<LclVarDsc>(newLvaTableCnt);

        memcpy(newLvaTable, lvaTable, lvaCount * sizeof(*lvaTable));
        memset(newLvaTable + lvaCount, 0, (newLvaTableCnt - lvaCount) * sizeof(*lvaTable));

        for (unsigned i = lvaCount; i < newLvaTableCnt; i++)
        {
            new (&newLvaTable[i], jitstd::placement_t()) LclVarDsc(); // call the constructor.
        }

#ifdef DEBUG
        // Fill the old table with junks. So to detect the un-intended use.
        memset(lvaTable, JitConfig.JitDefaultFill(), lvaCount * sizeof(*lvaTable));
#endif

        lvaTableCnt = newLvaTableCnt;
        lvaTable    = newLvaTable;
    }

    const unsigned tempNum = lvaCount;
    lvaCount++;

    // Initialize lvType, lvIsTemp and lvOnFrame
    lvaTable[tempNum].lvType    = TYP_UNDEF;
    lvaTable[tempNum].lvIsTemp  = shortLifetime;
    lvaTable[tempNum].lvOnFrame = true;

    // If we've started normal ref counting, bump the ref count of this
    // local, as we no longer do any incremental counting, and we presume
    // this new local will be referenced.
    if (lvaLocalVarRefCounted())
    {
        if (opts.OptimizationDisabled())
        {
            lvaTable[tempNum].lvImplicitlyReferenced = 1;
        }
        else
        {
            lvaTable[tempNum].setLvRefCnt(1);
            lvaTable[tempNum].setLvRefCntWtd(BB_UNITY_WEIGHT);
        }
    }

#ifdef DEBUG
    lvaTable[tempNum].lvReason = reason;

    if (verbose)
    {
        printf("\nlvaGrabTemp returning %d (", tempNum);
        gtDispLclVar(tempNum, false);
        printf(")%s called for %s.\n", shortLifetime ? "" : " (a long lifetime temp)", reason);
    }
#endif // DEBUG

    return tempNum;
}

inline unsigned Compiler::lvaGrabTemps(unsigned cnt DEBUGARG(const char* reason))
{
    if (compIsForInlining())
    {
        // Grab the temps using Inliner's Compiler instance.
        unsigned tmpNum = impInlineInfo->InlinerCompiler->lvaGrabTemps(cnt DEBUGARG(reason));

        lvaTable    = impInlineInfo->InlinerCompiler->lvaTable;
        lvaCount    = impInlineInfo->InlinerCompiler->lvaCount;
        lvaTableCnt = impInlineInfo->InlinerCompiler->lvaTableCnt;
        return tmpNum;
    }

#ifdef DEBUG
    if (verbose)
    {
        printf("\nlvaGrabTemps(%d) returning %d..%d (long lifetime temps) called for %s", cnt, lvaCount,
               lvaCount + cnt - 1, reason);
    }
#endif

    // Could handle this...
    assert(!lvaLocalVarRefCounted());

    // You cannot allocate more space after frame layout!
    noway_assert(lvaDoneFrameLayout < Compiler::TENTATIVE_FRAME_LAYOUT);

    /* Check if the lvaTable has to be grown */
    if (lvaCount + cnt > lvaTableCnt)
    {
        unsigned newLvaTableCnt = lvaCount + max(lvaCount / 2 + 1, cnt);

        // Check for overflow
        if (newLvaTableCnt <= lvaCount)
        {
            IMPL_LIMITATION("too many locals");
        }

        LclVarDsc* newLvaTable = getAllocator(CMK_LvaTable).allocate<LclVarDsc>(newLvaTableCnt);

        memcpy(newLvaTable, lvaTable, lvaCount * sizeof(*lvaTable));
        memset(newLvaTable + lvaCount, 0, (newLvaTableCnt - lvaCount) * sizeof(*lvaTable));
        for (unsigned i = lvaCount; i < newLvaTableCnt; i++)
        {
            new (&newLvaTable[i], jitstd::placement_t()) LclVarDsc(); // call the constructor.
        }

#ifdef DEBUG
        // Fill the old table with junks. So to detect the un-intended use.
        memset(lvaTable, JitConfig.JitDefaultFill(), lvaCount * sizeof(*lvaTable));
#endif

        lvaTableCnt = newLvaTableCnt;
        lvaTable    = newLvaTable;
    }

    unsigned tempNum = lvaCount;

    while (cnt--)
    {
        lvaTable[lvaCount].lvType    = TYP_UNDEF; // Initialize lvType, lvIsTemp and lvOnFrame
        lvaTable[lvaCount].lvIsTemp  = false;
        lvaTable[lvaCount].lvOnFrame = true;
        lvaCount++;
    }

    return tempNum;
}

/*****************************************************************************
 *
 *  Allocate a temporary variable which is implicitly used by code-gen
 *  There will be no explicit references to the temp, and so it needs to
 *  be forced to be kept alive, and not be optimized away.
 */

inline unsigned Compiler::lvaGrabTempWithImplicitUse(bool shortLifetime DEBUGARG(const char* reason))
{
    if (compIsForInlining())
    {
        // Grab the temp using Inliner's Compiler instance.
        unsigned tmpNum = impInlineInfo->InlinerCompiler->lvaGrabTempWithImplicitUse(shortLifetime DEBUGARG(reason));

        lvaTable    = impInlineInfo->InlinerCompiler->lvaTable;
        lvaCount    = impInlineInfo->InlinerCompiler->lvaCount;
        lvaTableCnt = impInlineInfo->InlinerCompiler->lvaTableCnt;
        return tmpNum;
    }

    unsigned lclNum = lvaGrabTemp(shortLifetime DEBUGARG(reason));

    LclVarDsc* varDsc = &lvaTable[lclNum];

    // This will prevent it from being optimized away
    // TODO-CQ: We shouldn't have to go as far as to declare these
    // address-exposed -- DoNotEnregister should suffice?
    lvaSetVarAddrExposed(lclNum);

    // Note the implicit use
    varDsc->lvImplicitlyReferenced = 1;

    return lclNum;
}

/*****************************************************************************
 *
 *  Increment the ref counts for a local variable
 */

inline void LclVarDsc::incRefCnts(BasicBlock::weight_t weight, Compiler* comp, RefCountState state, bool propagate)
{
    // In minopts and debug codegen, we don't maintain normal ref counts.
    if ((state == RCS_NORMAL) && comp->opts.OptimizationDisabled())
    {
        // Note, at least, that there is at least one reference.
        lvImplicitlyReferenced = 1;
        return;
    }

    Compiler::lvaPromotionType promotionType = DUMMY_INIT(Compiler::PROMOTION_TYPE_NONE);
    if (varTypeIsStruct(lvType))
    {
        promotionType = comp->lvaGetPromotionType(this);
    }

    //
    // Increment counts on the local itself.
    //
    if ((lvType != TYP_STRUCT) || (promotionType != Compiler::PROMOTION_TYPE_INDEPENDENT))
    {
        // We increment ref counts of this local for primitive types, including structs that have been retyped as their
        // only field, as well as for structs whose fields are not independently promoted.

        //
        // Increment lvRefCnt
        //
        int newRefCnt = lvRefCnt(state) + 1;
        if (newRefCnt == (unsigned short)newRefCnt) // lvRefCnt is an "unsigned short". Don't overflow it.
        {
            setLvRefCnt((unsigned short)newRefCnt, state);
        }

        //
        // Increment lvRefCntWtd
        //
        if (weight != 0)
        {
            // We double the weight of internal temps

            bool doubleWeight = lvIsTemp;

#if defined(TARGET_AMD64) || defined(TARGET_ARM64)
            // and, for the time being, implict byref params
            doubleWeight |= lvIsImplicitByRef;
#endif // defined(TARGET_AMD64) || defined(TARGET_ARM64)

            if (doubleWeight && (weight * 2 > weight))
            {
                weight *= 2;
            }

            unsigned newWeight = lvRefCntWtd(state) + weight;
            if (newWeight >= lvRefCntWtd(state))
            { // lvRefCntWtd is an "unsigned".  Don't overflow it
                setLvRefCntWtd(newWeight, state);
            }
            else
            { // On overflow we assign UINT32_MAX
                setLvRefCntWtd(UINT32_MAX, state);
            }
        }
    }

    if (varTypeIsStruct(lvType) && propagate)
    {
        // For promoted struct locals, increment lvRefCnt on its field locals as well.
        if (promotionType == Compiler::PROMOTION_TYPE_INDEPENDENT ||
            promotionType == Compiler::PROMOTION_TYPE_DEPENDENT)
        {
            for (unsigned i = lvFieldLclStart; i < lvFieldLclStart + lvFieldCnt; ++i)
            {
                comp->lvaTable[i].incRefCnts(weight, comp, state, false); // Don't propagate
            }
        }
    }

    if (lvIsStructField && propagate)
    {
        // Depending on the promotion type, increment the ref count for the parent struct as well.
        promotionType           = comp->lvaGetParentPromotionType(this);
        LclVarDsc* parentvarDsc = &comp->lvaTable[lvParentLcl];
        assert(!parentvarDsc->lvRegStruct);
        if (promotionType == Compiler::PROMOTION_TYPE_DEPENDENT)
        {
            parentvarDsc->incRefCnts(weight, comp, state, false); // Don't propagate
        }
    }

#ifdef DEBUG
    if (comp->verbose)
    {
        unsigned varNum = (unsigned)(this - comp->lvaTable);
        assert(&comp->lvaTable[varNum] == this);
        printf("New refCnts for V%02u: refCnt = %2u, refCntWtd = %s\n", varNum, lvRefCnt(state),
               refCntWtd2str(lvRefCntWtd(state)));
    }
#endif
}

/*****************************************************************************
 *
 *  The following returns the mask of all tracked locals
 *  referenced in a statement.
 */

inline VARSET_VALRET_TP Compiler::lvaStmtLclMask(Statement* stmt)
{
    unsigned   varNum;
    LclVarDsc* varDsc;
    VARSET_TP  lclMask(VarSetOps::MakeEmpty(this));

    assert(fgStmtListThreaded);

    for (GenTree* tree = stmt->GetTreeList(); tree != nullptr; tree = tree->gtNext)
    {
        if (tree->gtOper != GT_LCL_VAR)
        {
            continue;
        }

        varNum = tree->AsLclVarCommon()->GetLclNum();
        assert(varNum < lvaCount);
        varDsc = lvaTable + varNum;

        if (!varDsc->lvTracked)
        {
            continue;
        }

        VarSetOps::UnionD(this, lclMask, VarSetOps::MakeSingleton(this, varDsc->lvVarIndex));
    }

    return lclMask;
}

/*****************************************************************************
 Is this a synchronized instance method? If so, we will need to report "this"
 in the GC information, so that the EE can release the object lock
 in case of an exception

 We also need to report "this" and keep it alive for all shared generic
 code that gets the actual generic context from the "this" pointer and
 has exception handlers.

 For example, if List<T>::m() is shared between T = object and T = string,
 then inside m() an exception handler "catch E<T>" needs to be able to fetch
 the 'this' pointer to find out what 'T' is in order to tell if we
 should catch the exception or not.
 */

inline bool Compiler::lvaKeepAliveAndReportThis()
{
    if (info.compIsStatic || lvaTable[0].TypeGet() != TYP_REF)
    {
        return false;
    }

    const bool genericsContextIsThis = (info.compMethodInfo->options & CORINFO_GENERICS_CTXT_FROM_THIS) != 0;

#ifdef JIT32_GCENCODER

    if (info.compFlags & CORINFO_FLG_SYNCH)
        return true;

    if (genericsContextIsThis)
    {
        // TODO: Check if any of the exception clauses are
        // typed using a generic type. Else, we do not need to report this.
        if (info.compXcptnsCount > 0)
            return true;

        if (opts.compDbgCode)
            return true;

        if (lvaGenericsContextInUse)
        {
            JITDUMP("Reporting this as generic context\n");
            return true;
        }
    }
#else // !JIT32_GCENCODER
    // If the generics context is the this pointer we need to report it if either
    // the VM requires us to keep the generics context alive or it is used in a look-up.
    // We keep it alive in the lookup scenario, even when the VM didn't ask us to,
    // because collectible types need the generics context when gc-ing.
    if (genericsContextIsThis)
    {
        const bool mustKeep = (info.compMethodInfo->options & CORINFO_GENERICS_CTXT_KEEP_ALIVE) != 0;

        if (lvaGenericsContextInUse || mustKeep)
        {
            JITDUMP("Reporting this as generic context: %s\n", mustKeep ? "must keep" : "referenced");
            return true;
        }
    }
#endif

    return false;
}

/*****************************************************************************
  Similar to lvaKeepAliveAndReportThis
 */

inline bool Compiler::lvaReportParamTypeArg()
{
    if (info.compMethodInfo->options & (CORINFO_GENERICS_CTXT_FROM_METHODDESC | CORINFO_GENERICS_CTXT_FROM_METHODTABLE))
    {
        assert(info.compTypeCtxtArg != -1);

        // If the VM requires us to keep the generics context alive and report it (for example, if any catch
        // clause catches a type that uses a generic parameter of this method) this flag will be set.
        if (info.compMethodInfo->options & CORINFO_GENERICS_CTXT_KEEP_ALIVE)
        {
            return true;
        }

        // Otherwise, if an exact type parameter is needed in the body, report the generics context.
        // We do this because collectible types needs the generics context when gc-ing.
        if (lvaGenericsContextInUse)
        {
            return true;
        }
    }

    // Otherwise, we don't need to report it -- the generics context parameter is unused.
    return false;
}

//*****************************************************************************

inline int Compiler::lvaCachedGenericContextArgOffset()
{
    assert(lvaDoneFrameLayout == FINAL_FRAME_LAYOUT);

    return lvaCachedGenericContextArgOffs;
}

//------------------------------------------------------------------------
// lvaFrameAddress: Determine the stack frame offset of the given variable,
// and how to generate an address to that stack frame.
//
// Arguments:
//    varNum         - The variable to inquire about. Positive for user variables
//                     or arguments, negative for spill-temporaries.
//    mustBeFPBased  - [TARGET_ARM only] True if the base register must be FP.
//                     After FINAL_FRAME_LAYOUT, if false, it also requires SP base register.
//    pBaseReg       - [TARGET_ARM only] Out arg. *pBaseReg is set to the base
//                     register to use.
//    addrModeOffset - [TARGET_ARM only] The mode offset within the variable that we need to address.
//                     For example, for a large struct local, and a struct field reference, this will be the offset
//                     of the field. Thus, for V02 + 0x28, if V02 itself is at offset SP + 0x10
//                     then addrModeOffset is what gets added beyond that, here 0x28.
//    isFloatUsage   - [TARGET_ARM only] True if the instruction being generated is a floating
//                     point instruction. This requires using floating-point offset restrictions.
//                     Note that a variable can be non-float, e.g., struct, but accessed as a
//                     float local field.
//    pFPbased       - [non-TARGET_ARM] Out arg. Set *FPbased to true if the
//                     variable is addressed off of FP, false if it's addressed
//                     off of SP.
//
// Return Value:
//    Returns the variable offset from the given base register.
//
inline
#ifdef TARGET_ARM
    int
    Compiler::lvaFrameAddress(
        int varNum, bool mustBeFPBased, regNumber* pBaseReg, int addrModeOffset, bool isFloatUsage)
#else
    int
    Compiler::lvaFrameAddress(int varNum, bool* pFPbased)
#endif
{
    assert(lvaDoneFrameLayout != NO_FRAME_LAYOUT);

    int  varOffset;
    bool FPbased;
    bool fConservative = false;
    if (varNum >= 0)
    {
        LclVarDsc* varDsc;

        assert((unsigned)varNum < lvaCount);
        varDsc               = lvaTable + varNum;
        bool isPrespilledArg = false;
#if defined(TARGET_ARM) && defined(PROFILING_SUPPORTED)
        isPrespilledArg = varDsc->lvIsParam && compIsProfilerHookNeeded() &&
                          lvaIsPreSpilled(varNum, codeGen->regSet.rsMaskPreSpillRegs(false));
#endif

        // If we have finished with register allocation, and this isn't a stack-based local,
        // check that this has a valid stack location.
        if (lvaDoneFrameLayout > REGALLOC_FRAME_LAYOUT && !varDsc->lvOnFrame)
        {
#ifdef TARGET_AMD64
#ifndef UNIX_AMD64_ABI
            // On amd64, every param has a stack location, except on Unix-like systems.
            assert(varDsc->lvIsParam);
#endif // UNIX_AMD64_ABI
#else  // !TARGET_AMD64
            // For other targets, a stack parameter that is enregistered or prespilled
            // for profiling on ARM will have a stack location.
            assert((varDsc->lvIsParam && !varDsc->lvIsRegArg) || isPrespilledArg);
#endif // !TARGET_AMD64
        }

        FPbased = varDsc->lvFramePointerBased;

#ifdef DEBUG
#if FEATURE_FIXED_OUT_ARGS
        if ((unsigned)varNum == lvaOutgoingArgSpaceVar)
        {
            assert(FPbased == false);
        }
        else
#endif
        {
#if DOUBLE_ALIGN
            assert(FPbased == (isFramePointerUsed() || (genDoubleAlign() && varDsc->lvIsParam && !varDsc->lvIsRegArg)));
#else
#ifdef TARGET_X86
            assert(FPbased == isFramePointerUsed());
#endif
#endif
        }
#endif // DEBUG

        varOffset = varDsc->lvStkOffs;
    }
    else // Its a spill-temp
    {
        FPbased = isFramePointerUsed();
        if (lvaDoneFrameLayout == Compiler::FINAL_FRAME_LAYOUT)
        {
            TempDsc* tmpDsc = codeGen->regSet.tmpFindNum(varNum);
            // The temp might be in use, since this might be during code generation.
            if (tmpDsc == nullptr)
            {
                tmpDsc = codeGen->regSet.tmpFindNum(varNum, RegSet::TEMP_USAGE_USED);
            }
            assert(tmpDsc != nullptr);
            varOffset = tmpDsc->tdTempOffs();
        }
        else
        {
            // This value is an estimate until we calculate the
            // offset after the final frame layout
            // ---------------------------------------------------
            //   :                         :
            //   +-------------------------+ base --+
            //   | LR, ++N for ARM         |        |   frameBaseOffset (= N)
            //   +-------------------------+        |
            //   | R11, ++N for ARM        | <---FP |
            //   +-------------------------+      --+
            //   | compCalleeRegsPushed - N|        |   lclFrameOffset
            //   +-------------------------+      --+
            //   | lclVars                 |        |
            //   +-------------------------+        |
            //   | tmp[MAX_SPILL_TEMP]     |        |
            //   | tmp[1]                  |        |
            //   | tmp[0]                  |        |   compLclFrameSize
            //   +-------------------------+        |
            //   | outgoingArgSpaceSize    |        |
            //   +-------------------------+      --+
            //   |                         | <---SP
            //   :                         :
            // ---------------------------------------------------

            fConservative = true;
            if (!FPbased)
            {
                // Worst case stack based offset.
                CLANG_FORMAT_COMMENT_ANCHOR;
#if FEATURE_FIXED_OUT_ARGS
                int outGoingArgSpaceSize = lvaOutgoingArgSpaceSize;
#else
                int outGoingArgSpaceSize = 0;
#endif
                varOffset = outGoingArgSpaceSize + max(-varNum * TARGET_POINTER_SIZE, (int)lvaGetMaxSpillTempSize());
            }
            else
            {
                // Worst case FP based offset.
                CLANG_FORMAT_COMMENT_ANCHOR;

#ifdef TARGET_ARM
                varOffset = codeGen->genCallerSPtoInitialSPdelta() - codeGen->genCallerSPtoFPdelta();
#else
                varOffset                = -(codeGen->genTotalFrameSize());
#endif
            }
        }
    }

#ifdef TARGET_ARM
    if (FPbased)
    {
        if (mustBeFPBased)
        {
            *pBaseReg = REG_FPBASE;
        }
        // Change the Frame Pointer (R11)-based addressing to the SP-based addressing when possible because
        // it generates smaller code on ARM. See frame picture above for the math.
        else
        {
            // If it is the final frame layout phase, we don't have a choice, we should stick
            // to either FP based or SP based that we decided in the earlier phase. Because
            // we have already selected the instruction. MinOpts will always reserve R10, so
            // for MinOpts always use SP-based offsets, using R10 as necessary, for simplicity.

            int spVarOffset        = fConservative ? compLclFrameSize : varOffset + codeGen->genSPtoFPdelta();
            int actualSPOffset     = spVarOffset + addrModeOffset;
            int actualFPOffset     = varOffset + addrModeOffset;
            int encodingLimitUpper = isFloatUsage ? 0x3FC : 0xFFF;
            int encodingLimitLower = isFloatUsage ? -0x3FC : -0xFF;

            // Use SP-based encoding. During encoding, we'll pick the best encoding for the actual offset we have.
            if (opts.MinOpts() || (actualSPOffset <= encodingLimitUpper))
            {
                varOffset = spVarOffset;
                *pBaseReg = compLocallocUsed ? REG_SAVED_LOCALLOC_SP : REG_SPBASE;
            }
            // Use Frame Pointer (R11)-based encoding.
            else if ((encodingLimitLower <= actualFPOffset) && (actualFPOffset <= encodingLimitUpper))
            {
                *pBaseReg = REG_FPBASE;
            }
            // Otherwise, use SP-based encoding. This is either (1) a small positive offset using a single movw,
            // (2) a large offset using movw/movt. In either case, we must have already reserved
            // the "reserved register", which will get used during encoding.
            else
            {
                varOffset = spVarOffset;
                *pBaseReg = compLocallocUsed ? REG_SAVED_LOCALLOC_SP : REG_SPBASE;
            }
        }
    }
    else
    {
        *pBaseReg = REG_SPBASE;
    }
#else
    *pFPbased                            = FPbased;
#endif

    return varOffset;
}

inline bool Compiler::lvaIsParameter(unsigned varNum)
{
    LclVarDsc* varDsc;

    assert(varNum < lvaCount);
    varDsc = lvaTable + varNum;

    return varDsc->lvIsParam;
}

inline bool Compiler::lvaIsRegArgument(unsigned varNum)
{
    LclVarDsc* varDsc;

    assert(varNum < lvaCount);
    varDsc = lvaTable + varNum;

    return varDsc->lvIsRegArg;
}

inline BOOL Compiler::lvaIsOriginalThisArg(unsigned varNum)
{
    assert(varNum < lvaCount);

    BOOL isOriginalThisArg = (varNum == info.compThisArg) && (info.compIsStatic == false);

#ifdef DEBUG
    if (isOriginalThisArg)
    {
        LclVarDsc* varDsc = lvaTable + varNum;
        // Should never write to or take the address of the original 'this' arg
        CLANG_FORMAT_COMMENT_ANCHOR;

#ifndef JIT32_GCENCODER
        // With the general encoder/decoder, when the original 'this' arg is needed as a generics context param, we
        // copy to a new local, and mark the original as DoNotEnregister, to
        // ensure that it is stack-allocated.  It should not be the case that the original one can be modified -- it
        // should not be written to, or address-exposed.
        assert(!varDsc->lvHasILStoreOp &&
               (!varDsc->lvAddrExposed || ((info.compMethodInfo->options & CORINFO_GENERICS_CTXT_FROM_THIS) != 0)));
#else
        assert(!varDsc->lvHasILStoreOp && !varDsc->lvAddrExposed);
#endif
    }
#endif

    return isOriginalThisArg;
}

inline BOOL Compiler::lvaIsOriginalThisReadOnly()
{
    return lvaArg0Var == info.compThisArg;
}

/*****************************************************************************
 *
 *  The following is used to detect the cases where the same local variable#
 *  is used both as a long/double value and a 32-bit value and/or both as an
 *  integer/address and a float value.
 */

/* static */ inline unsigned Compiler::lvaTypeRefMask(var_types type)
{
    const static BYTE lvaTypeRefMasks[] = {
#define DEF_TP(tn, nm, jitType, verType, sz, sze, asze, st, al, tf, howUsed) howUsed,
#include "typelist.h"
#undef DEF_TP
    };

    assert((unsigned)type < sizeof(lvaTypeRefMasks));
    assert(lvaTypeRefMasks[type] != 0);

    return lvaTypeRefMasks[type];
}

/*****************************************************************************
 *
 *  The following is used to detect the cases where the same local variable#
 *  is used both as a long/double value and a 32-bit value and/or both as an
 *  integer/address and a float value.
 */

inline var_types Compiler::lvaGetActualType(unsigned lclNum)
{
    return genActualType(lvaGetRealType(lclNum));
}

inline var_types Compiler::lvaGetRealType(unsigned lclNum)
{
    return lvaTable[lclNum].TypeGet();
}

/*
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XX                          Importer                                         XX
XX                      Inline functions                                     XX
XX                                                                           XX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
*/

inline unsigned Compiler::compMapILargNum(unsigned ILargNum)
{
    assert(ILargNum < info.compILargsCount || tiVerificationNeeded);

    // Note that this works because if compRetBuffArg/compTypeCtxtArg/lvVarargsHandleArg are not present
    // they will be BAD_VAR_NUM (MAX_UINT), which is larger than any variable number.
    if (ILargNum >= info.compRetBuffArg)
    {
        ILargNum++;
        assert(ILargNum < info.compLocalsCount || tiVerificationNeeded); // compLocals count already adjusted.
    }

    if (ILargNum >= (unsigned)info.compTypeCtxtArg)
    {
        ILargNum++;
        assert(ILargNum < info.compLocalsCount || tiVerificationNeeded); // compLocals count already adjusted.
    }

    if (ILargNum >= (unsigned)lvaVarargsHandleArg)
    {
        ILargNum++;
        assert(ILargNum < info.compLocalsCount || tiVerificationNeeded); // compLocals count already adjusted.
    }

    assert(ILargNum < info.compArgsCount || tiVerificationNeeded);
    return (ILargNum);
}

//------------------------------------------------------------------------
// Compiler::mangleVarArgsType: Retype float types to their corresponding
//                            : int/long types.
//
// Notes:
//
// The mangling of types will only occur for incoming vararg fixed arguments
// on windows arm|64 or on armel (softFP).
//
// NO-OP for all other cases.
//
inline var_types Compiler::mangleVarArgsType(var_types type)
{
#if defined(TARGET_ARMARCH)
    if (opts.compUseSoftFP
#if defined(TARGET_WINDOWS)
        || info.compIsVarArgs
#endif // defined(TARGET_WINDOWS)
        )
    {
        switch (type)
        {
            case TYP_FLOAT:
                return TYP_INT;
            case TYP_DOUBLE:
                return TYP_LONG;
            default:
                break;
        }
    }
#endif // defined(TARGET_ARMARCH)
    return type;
}

// For CORECLR there is no vararg on System V systems.
#if FEATURE_VARARG
inline regNumber Compiler::getCallArgIntRegister(regNumber floatReg)
{
#ifdef TARGET_AMD64
    switch (floatReg)
    {
        case REG_XMM0:
            return REG_RCX;
        case REG_XMM1:
            return REG_RDX;
        case REG_XMM2:
            return REG_R8;
        case REG_XMM3:
            return REG_R9;
        default:
            unreached();
    }
#else  // !TARGET_AMD64
    // How will float args be passed for RyuJIT/x86?
    NYI("getCallArgIntRegister for RyuJIT/x86");
    return REG_NA;
#endif // !TARGET_AMD64
}

inline regNumber Compiler::getCallArgFloatRegister(regNumber intReg)
{
#ifdef TARGET_AMD64
    switch (intReg)
    {
        case REG_RCX:
            return REG_XMM0;
        case REG_RDX:
            return REG_XMM1;
        case REG_R8:
            return REG_XMM2;
        case REG_R9:
            return REG_XMM3;
        default:
            unreached();
    }
#else  // !TARGET_AMD64
    // How will float args be passed for RyuJIT/x86?
    NYI("getCallArgFloatRegister for RyuJIT/x86");
    return REG_NA;
#endif // !TARGET_AMD64
}
#endif // FEATURE_VARARG

/*
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XX                     Register Allocator                                    XX
XX                      Inline functions                                     XX
XX                                                                           XX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
*/

/*****************************************************************************/

inline bool rpCanAsgOperWithoutReg(GenTree* op, bool lclvar)
{
    var_types type;

    switch (op->OperGet())
    {
        case GT_CNS_LNG:
        case GT_CNS_INT:
            return true;
        case GT_LCL_VAR:
            type = genActualType(op->TypeGet());
            if (lclvar && ((type == TYP_INT) || (type == TYP_REF) || (type == TYP_BYREF)))
            {
                return true;
            }
            break;
        default:
            break;
    }

    return false;
}

/*
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XX                                                                           XX
XX                       FlowGraph                                           XX
XX                      Inline functions                                     XX
XX                                                                           XX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
*/

inline bool Compiler::compCanEncodePtrArgCntMax()
{
#ifdef JIT32_GCENCODER
    // DDB 204533:
    // The GC encoding for fully interruptible methods does not
    // support more than 1023 pushed arguments, so we have to
    // use a partially interruptible GC info/encoding.
    //
    return (fgPtrArgCntMax < MAX_PTRARG_OFS);
#else // JIT32_GCENCODER
    return true;
#endif
}

/*****************************************************************************
 *
 *  Call the given function pointer for all nodes in the tree. The 'visitor'
 *  fn should return one of the following values:
 *
 *  WALK_ABORT          stop walking and return immediately
 *  WALK_CONTINUE       continue walking
 *  WALK_SKIP_SUBTREES  don't walk any subtrees of the node just visited
 *
 *  computeStack - true if we want to make stack visible to callback function
 */

inline Compiler::fgWalkResult Compiler::fgWalkTreePre(
    GenTree** pTree, fgWalkPreFn* visitor, void* callBackData, bool lclVarsOnly, bool computeStack)

{
    fgWalkData walkData;

    walkData.compiler      = this;
    walkData.wtprVisitorFn = visitor;
    walkData.pCallbackData = callBackData;
    walkData.parent        = nullptr;
    walkData.wtprLclsOnly  = lclVarsOnly;
#ifdef DEBUG
    walkData.printModified = false;
#endif

    fgWalkResult result;
    if (lclVarsOnly && computeStack)
    {
        GenericTreeWalker<true, true, false, true, true> walker(&walkData);
        result = walker.WalkTree(pTree, nullptr);
    }
    else if (lclVarsOnly)
    {
        GenericTreeWalker<false, true, false, true, true> walker(&walkData);
        result = walker.WalkTree(pTree, nullptr);
    }
    else if (computeStack)
    {
        GenericTreeWalker<true, true, false, false, true> walker(&walkData);
        result = walker.WalkTree(pTree, nullptr);
    }
    else
    {
        GenericTreeWalker<false, true, false, false, true> walker(&walkData);
        result = walker.WalkTree(pTree, nullptr);
    }

#ifdef DEBUG
    if (verbose && walkData.printModified)
    {
        gtDispTree(*pTree);
    }
#endif

    return result;
}

/*****************************************************************************
 *
 *  Same as above, except the tree walk is performed in a depth-first fashion,
 *  The 'visitor' fn should return one of the following values:
 *
 *  WALK_ABORT          stop walking and return immediately
 *  WALK_CONTINUE       continue walking
 *
 *  computeStack - true if we want to make stack visible to callback function
 */

inline Compiler::fgWalkResult Compiler::fgWalkTreePost(GenTree**     pTree,
                                                       fgWalkPostFn* visitor,
                                                       void*         callBackData,
                                                       bool          computeStack)
{
    fgWalkData walkData;

    walkData.compiler      = this;
    walkData.wtpoVisitorFn = visitor;
    walkData.pCallbackData = callBackData;
    walkData.parent        = nullptr;

    fgWalkResult result;
    if (computeStack)
    {
        GenericTreeWalker<true, false, true, false, true> walker(&walkData);
        result = walker.WalkTree(pTree, nullptr);
    }
    else
    {
        GenericTreeWalker<false, false, true, false, true> walker(&walkData);
        result = walker.WalkTree(pTree, nullptr);
    }

    assert(result == WALK_CONTINUE || result == WALK_ABORT);

    return result;
}

/*****************************************************************************
 *
 *  Call the given function pointer for all nodes in the tree. The 'visitor'
 *  fn should return one of the following values:
 *
 *  WALK_ABORT          stop walking and return immediately
 *  WALK_CONTINUE       continue walking
 *  WALK_SKIP_SUBTREES  don't walk any subtrees of the node just visited
 */

inline Compiler::fgWalkResult Compiler::fgWalkTree(GenTree**    pTree,
                                                   fgWalkPreFn* preVisitor,
                                                   fgWalkPreFn* postVisitor,
                                                   void*        callBackData)

{
    fgWalkData walkData;

    walkData.compiler      = this;
    walkData.wtprVisitorFn = preVisitor;
    walkData.wtpoVisitorFn = postVisitor;
    walkData.pCallbackData = callBackData;
    walkData.parent        = nullptr;
    walkData.wtprLclsOnly  = false;
#ifdef DEBUG
    walkData.printModified = false;
#endif

    fgWalkResult result;

    assert(preVisitor || postVisitor);

    if (preVisitor && postVisitor)
    {
        GenericTreeWalker<true, true, true, false, true> walker(&walkData);
        result = walker.WalkTree(pTree, nullptr);
    }
    else if (preVisitor)
    {
        GenericTreeWalker<true, true, false, false, true> walker(&walkData);
        result = walker.WalkTree(pTree, nullptr);
    }
    else
    {
        GenericTreeWalker<true, false, true, false, true> walker(&walkData);
        result = walker.WalkTree(pTree, nullptr);
    }

#ifdef DEBUG
    if (verbose && walkData.printModified)
    {
        gtDispTree(*pTree);
    }
#endif

    return result;
}

/*****************************************************************************
 *
 * Has this block been added to throw an inlined exception
 * Returns true if the block was added to throw one of:
 *    range-check exception
 *    argument exception (used by feature SIMD)
 *    argument range-check exception (used by feature SIMD)
 *    divide by zero exception  (Not used on X86/X64)
 *    null reference exception (Not currently used)
 *    overflow exception
 */

inline bool Compiler::fgIsThrowHlpBlk(BasicBlock* block)
{
    if (!fgIsCodeAdded())
    {
        return false;
    }

    if (!(block->bbFlags & BBF_INTERNAL) || block->bbJumpKind != BBJ_THROW)
    {
        return false;
    }

    GenTree* call = block->lastNode();

#ifdef DEBUG
    if (block->IsLIR())
    {
        LIR::Range& blockRange = LIR::AsRange(block);
        for (LIR::Range::ReverseIterator node = blockRange.rbegin(), end = blockRange.rend(); node != end; ++node)
        {
            if (node->OperGet() == GT_CALL)
            {
                assert(*node == call);
                assert(node == blockRange.rbegin());
                break;
            }
        }
    }
#endif

    if (!call || (call->gtOper != GT_CALL))
    {
        return false;
    }

    if (!((call->AsCall()->gtCallMethHnd == eeFindHelper(CORINFO_HELP_RNGCHKFAIL)) ||
          (call->AsCall()->gtCallMethHnd == eeFindHelper(CORINFO_HELP_THROWDIVZERO)) ||
          (call->AsCall()->gtCallMethHnd == eeFindHelper(CORINFO_HELP_THROWNULLREF)) ||
          (call->AsCall()->gtCallMethHnd == eeFindHelper(CORINFO_HELP_OVERFLOW))))
    {
        return false;
    }

    // We can get to this point for blocks that we didn't create as throw helper blocks
    // under stress, with implausible flow graph optimizations. So, walk the fgAddCodeList
    // for the final determination.

    for (AddCodeDsc* add = fgAddCodeList; add; add = add->acdNext)
    {
        if (block == add->acdDstBlk)
        {
            return add->acdKind == SCK_RNGCHK_FAIL || add->acdKind == SCK_DIV_BY_ZERO || add->acdKind == SCK_OVERFLOW ||
                   add->acdKind == SCK_ARG_EXCPN || add->acdKind == SCK_ARG_RNG_EXCPN;
        }
    }

    // We couldn't find it in the fgAddCodeList
    return false;
}

#if !FEATURE_FIXED_OUT_ARGS

/*****************************************************************************
 *
 *  Return the stackLevel of the inserted block that throws exception
 *  (by calling the EE helper).
 */

inline unsigned Compiler::fgThrowHlpBlkStkLevel(BasicBlock* block)
{
    for (AddCodeDsc* add = fgAddCodeList; add; add = add->acdNext)
    {
        if (block == add->acdDstBlk)
        {
            // Compute assert cond separately as assert macro cannot have conditional compilation directives.
            bool cond =
                (add->acdKind == SCK_RNGCHK_FAIL || add->acdKind == SCK_DIV_BY_ZERO || add->acdKind == SCK_OVERFLOW ||
                 add->acdKind == SCK_ARG_EXCPN || add->acdKind == SCK_ARG_RNG_EXCPN);
            assert(cond);

            // TODO: bbTgtStkDepth is DEBUG-only.
            // Should we use it regularly and avoid this search.
            assert(block->bbTgtStkDepth == add->acdStkLvl);
            return add->acdStkLvl;
        }
    }

    noway_assert(!"fgThrowHlpBlkStkLevel should only be called if fgIsThrowHlpBlk() is true, but we can't find the "
                  "block in the fgAddCodeList list");

    /* We couldn't find the basic block: it must not have been a throw helper block */

    return 0;
}

#endif // !FEATURE_FIXED_OUT_ARGS

/*
    Small inline function to change a given block to a throw block.

*/
inline void Compiler::fgConvertBBToThrowBB(BasicBlock* block)
{
    JITDUMP("Converting " FMT_BB " to BBJ_THROW\n", block->bbNum);

    // Ordering of the following operations matters.
    // First, note if we are looking at the first block of a call always pair.
    const bool isCallAlwaysPair = block->isBBCallAlwaysPair();

    // Scrub this block from the pred lists of any successors
    fgRemoveBlockAsPred(block);

    // Update jump kind after the scrub.
    block->bbJumpKind = BBJ_THROW;

    // Any block with a throw is rare
    block->bbSetRunRarely();

    // If we've converted a BBJ_CALLFINALLY block to a BBJ_THROW block,
    // then mark the subsequent BBJ_ALWAYS block as unreferenced.
    //
    // Must do this after we update bbJumpKind of block.
    if (isCallAlwaysPair)
    {
        BasicBlock* leaveBlk = block->bbNext;
        noway_assert(leaveBlk->bbJumpKind == BBJ_ALWAYS);

        // leaveBlk is now unreachable, so scrub the pred lists.
        leaveBlk->bbFlags &= ~BBF_DONT_REMOVE;
        leaveBlk->bbRefs  = 0;
        leaveBlk->bbPreds = nullptr;

#if defined(FEATURE_EH_FUNCLETS) && defined(TARGET_ARM)
        // This function (fgConvertBBToThrowBB) can be called before the predecessor lists are created (e.g., in
        // fgMorph). The fgClearFinallyTargetBit() function to update the BBF_FINALLY_TARGET bit depends on these
        // predecessor lists. If there are no predecessor lists, we immediately clear all BBF_FINALLY_TARGET bits
        // (to allow subsequent dead code elimination to delete such blocks without asserts), and set a flag to
        // recompute them later, before they are required.
        if (fgComputePredsDone)
        {
            fgClearFinallyTargetBit(leaveBlk->bbJumpDest);
        }
        else
        {
            fgClearAllFinallyTargetBits();
            fgNeedToAddFinallyTargetBits = true;
        }
#endif // defined(FEATURE_EH_FUNCLETS) && defined(TARGET_ARM)
    }
}

/*****************************************************************************
 *
 *  Return true if we've added any new basic blocks.
 */

inline bool Compiler::fgIsCodeAdded()
{
    return fgAddCodeModf;
}

/*****************************************************************************
  Is the offset too big?
*/
inline bool Compiler::fgIsBigOffset(size_t offset)
{
    return (offset > compMaxUncheckedOffsetForNullObject);
}

/*
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XX                          TempsInfo                                        XX
XX                      Inline functions                                     XX
XX                                                                           XX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
*/

/*****************************************************************************/

/* static */ inline unsigned RegSet::tmpSlot(unsigned size)
{
    noway_assert(size >= sizeof(int));
    noway_assert(size <= TEMP_MAX_SIZE);
    assert((size % sizeof(int)) == 0);

    assert(size < UINT32_MAX);
    return size / sizeof(int) - 1;
}

/*****************************************************************************
 *
 *  Finish allocating temps - should be called each time after a pass is made
 *  over a function body.
 */

inline void RegSet::tmpEnd()
{
#ifdef DEBUG
    if (m_rsCompiler->verbose && (tmpCount > 0))
    {
        printf("%d tmps used\n", tmpCount);
    }
#endif // DEBUG
}

/*****************************************************************************
 *
 *  Shuts down the temp-tracking code. Should be called once per function
 *  compiled.
 */

inline void RegSet::tmpDone()
{
#ifdef DEBUG
    unsigned count;
    TempDsc* temp;

    assert(tmpAllFree());
    for (temp = tmpListBeg(), count = temp ? 1 : 0; temp; temp = tmpListNxt(temp), count += temp ? 1 : 0)
    {
        assert(temp->tdLegalOffset());
    }

    // Make sure that all the temps were released
    assert(count == tmpCount);
    assert(tmpGetCount == 0);
#endif // DEBUG
}

#ifdef DEBUG
inline bool Compiler::shouldUseVerboseTrees()
{
    return (JitConfig.JitDumpVerboseTrees() == 1);
}

inline bool Compiler::shouldUseVerboseSsa()
{
    return (JitConfig.JitDumpVerboseSsa() == 1);
}

//------------------------------------------------------------------------
// shouldDumpASCIITrees: Should we use only ASCII characters for tree dumps?
//
// Notes:
//    This is set to default to 1 in clrConfigValues.h

inline bool Compiler::shouldDumpASCIITrees()
{
    return (JitConfig.JitDumpASCII() == 1);
}

/*****************************************************************************
 *  Should we enable JitStress mode?
 *   0:   No stress
 *   !=2: Vary stress. Performance will be slightly/moderately degraded
 *   2:   Check-all stress. Performance will be REALLY horrible
 */

inline int getJitStressLevel()
{
    return JitConfig.JitStress();
}

#endif // DEBUG

/*****************************************************************************/
/* Map a register argument number ("RegArgNum") to a register number ("RegNum").
 * A RegArgNum is in this range:
 *      [0, MAX_REG_ARG)        -- for integer registers
 *      [0, MAX_FLOAT_REG_ARG)  -- for floating point registers
 * Note that RegArgNum's are overlapping for integer and floating-point registers,
 * while RegNum's are not (for ARM anyway, though for x86, it might be different).
 * If we have a fixed return buffer register and are given it's index
 * we return the fixed return buffer register
 */

inline regNumber genMapIntRegArgNumToRegNum(unsigned argNum)
{
    if (hasFixedRetBuffReg() && (argNum == theFixedRetBuffArgNum()))
    {
        return theFixedRetBuffReg();
    }

    assert(argNum < ArrLen(intArgRegs));

    return intArgRegs[argNum];
}

inline regNumber genMapFloatRegArgNumToRegNum(unsigned argNum)
{
#ifndef TARGET_X86
    assert(argNum < ArrLen(fltArgRegs));

    return fltArgRegs[argNum];
#else
    assert(!"no x86 float arg regs\n");
    return REG_NA;
#endif
}

__forceinline regNumber genMapRegArgNumToRegNum(unsigned argNum, var_types type)
{
    if (varTypeUsesFloatArgReg(type))
    {
        return genMapFloatRegArgNumToRegNum(argNum);
    }
    else
    {
        return genMapIntRegArgNumToRegNum(argNum);
    }
}

/*****************************************************************************/
/* Map a register argument number ("RegArgNum") to a register mask of the associated register.
 * Note that for floating-pointer registers, only the low register for a register pair
 * (for a double on ARM) is returned.
 */

inline regMaskTP genMapIntRegArgNumToRegMask(unsigned argNum)
{
    assert(argNum < ArrLen(intArgMasks));

    return intArgMasks[argNum];
}

inline regMaskTP genMapFloatRegArgNumToRegMask(unsigned argNum)
{
#ifndef TARGET_X86
    assert(argNum < ArrLen(fltArgMasks));

    return fltArgMasks[argNum];
#else
    assert(!"no x86 float arg regs\n");
    return RBM_NONE;
#endif
}

__forceinline regMaskTP genMapArgNumToRegMask(unsigned argNum, var_types type)
{
    regMaskTP result;
    if (varTypeUsesFloatArgReg(type))
    {
        result = genMapFloatRegArgNumToRegMask(argNum);
#ifdef TARGET_ARM
        if (type == TYP_DOUBLE)
        {
            assert((result & RBM_DBL_REGS) != 0);
            result |= (result << 1);
        }
#endif
    }
    else
    {
        result = genMapIntRegArgNumToRegMask(argNum);
    }
    return result;
}

/*****************************************************************************/
/* Map a register number ("RegNum") to a register argument number ("RegArgNum")
 * If we have a fixed return buffer register we return theFixedRetBuffArgNum
 */

inline unsigned genMapIntRegNumToRegArgNum(regNumber regNum)
{
    assert(genRegMask(regNum) & fullIntArgRegMask());

    switch (regNum)
    {
        case REG_ARG_0:
            return 0;
#if MAX_REG_ARG >= 2
        case REG_ARG_1:
            return 1;
#if MAX_REG_ARG >= 3
        case REG_ARG_2:
            return 2;
#if MAX_REG_ARG >= 4
        case REG_ARG_3:
            return 3;
#if MAX_REG_ARG >= 5
        case REG_ARG_4:
            return 4;
#if MAX_REG_ARG >= 6
        case REG_ARG_5:
            return 5;
#if MAX_REG_ARG >= 7
        case REG_ARG_6:
            return 6;
#if MAX_REG_ARG >= 8
        case REG_ARG_7:
            return 7;
#endif
#endif
#endif
#endif
#endif
#endif
#endif
        default:
            // Check for the Arm64 fixed return buffer argument register
            if (hasFixedRetBuffReg() && (regNum == theFixedRetBuffReg()))
            {
                return theFixedRetBuffArgNum();
            }
            else
            {
                assert(!"invalid register arg register");
                return BAD_VAR_NUM;
            }
    }
}

inline unsigned genMapFloatRegNumToRegArgNum(regNumber regNum)
{
    assert(genRegMask(regNum) & RBM_FLTARG_REGS);

#ifdef TARGET_ARM
    return regNum - REG_F0;
#elif defined(TARGET_ARM64)
    return regNum - REG_V0;
#elif defined(UNIX_AMD64_ABI)
    return regNum - REG_FLTARG_0;
#else

#if MAX_FLOAT_REG_ARG >= 1
    switch (regNum)
    {
        case REG_FLTARG_0:
            return 0;
#if MAX_REG_ARG >= 2
        case REG_FLTARG_1:
            return 1;
#if MAX_REG_ARG >= 3
        case REG_FLTARG_2:
            return 2;
#if MAX_REG_ARG >= 4
        case REG_FLTARG_3:
            return 3;
#if MAX_REG_ARG >= 5
        case REG_FLTARG_4:
            return 4;
#endif
#endif
#endif
#endif
        default:
            assert(!"invalid register arg register");
            return BAD_VAR_NUM;
    }
#else
    assert(!"flt reg args not allowed");
    return BAD_VAR_NUM;
#endif
#endif // !arm
}

inline unsigned genMapRegNumToRegArgNum(regNumber regNum, var_types type)
{
    if (varTypeUsesFloatArgReg(type))
    {
        return genMapFloatRegNumToRegArgNum(regNum);
    }
    else
    {
        return genMapIntRegNumToRegArgNum(regNum);
    }
}

/*****************************************************************************/
/* Return a register mask with the first 'numRegs' argument registers set.
 */

inline regMaskTP genIntAllRegArgMask(unsigned numRegs)
{
    assert(numRegs <= MAX_REG_ARG);

    regMaskTP result = RBM_NONE;
    for (unsigned i = 0; i < numRegs; i++)
    {
        result |= intArgMasks[i];
    }
    return result;
}

inline regMaskTP genFltAllRegArgMask(unsigned numRegs)
{
#ifndef TARGET_X86
    assert(numRegs <= MAX_FLOAT_REG_ARG);

    regMaskTP result = RBM_NONE;
    for (unsigned i = 0; i < numRegs; i++)
    {
        result |= fltArgMasks[i];
    }
    return result;
#else
    assert(!"no x86 float arg regs\n");
    return RBM_NONE;
#endif
}

/*
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XX                          Liveness                                         XX
XX                      Inline functions                                     XX
XX                                                                           XX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
*/

//------------------------------------------------------------------------
// compUpdateLife: Update the GC's masks, register's masks and reports change on variable's homes given a set of
//    current live variables if changes have happened since "compCurLife".
//
// Arguments:
//    newLife - the set of variables that are alive.
//
// Assumptions:
//    The set of live variables reflects the result of only emitted code, it should not be considering the becoming
//    live/dead of instructions that has not been emitted yet. This is requires by "compChangeLife".
template <bool ForCodeGen>
inline void Compiler::compUpdateLife(VARSET_VALARG_TP newLife)
{
    if (!VarSetOps::Equal(this, compCurLife, newLife))
    {
        compChangeLife<ForCodeGen>(newLife);
    }
#ifdef DEBUG
    else
    {
        if (verbose)
        {
            printf("Liveness not changing: %s ", VarSetOps::ToString(this, compCurLife));
            dumpConvertedVarSet(this, compCurLife);
            printf("\n");
        }
    }
#endif // DEBUG
}

/*****************************************************************************
 *
 *  We stash cookies in basic blocks for the code emitter; this call retrieves
 *  the cookie associated with the given basic block.
 */

inline void* emitCodeGetCookie(BasicBlock* block)
{
    assert(block);
    return block->bbEmitCookie;
}

/*
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XX                          Optimizer                                        XX
XX                      Inline functions                                     XX
XX                                                                           XX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
*/

#if LOCAL_ASSERTION_PROP

/*****************************************************************************
 *
 *  The following resets the value assignment table
 *  used only during local assertion prop
 */

inline void Compiler::optAssertionReset(AssertionIndex limit)
{
    PREFAST_ASSUME(optAssertionCount <= optMaxAssertionCount);

    while (optAssertionCount > limit)
    {
        AssertionIndex index        = optAssertionCount;
        AssertionDsc*  curAssertion = optGetAssertion(index);
        optAssertionCount--;
        unsigned lclNum = curAssertion->op1.lcl.lclNum;
        assert(lclNum < lvaTableCnt);
        BitVecOps::RemoveElemD(apTraits, GetAssertionDep(lclNum), index - 1);

        //
        // Find the Copy assertions
        //
        if ((curAssertion->assertionKind == OAK_EQUAL) && (curAssertion->op1.kind == O1K_LCLVAR) &&
            (curAssertion->op2.kind == O2K_LCLVAR_COPY))
        {
            //
            //  op2.lcl.lclNum no longer depends upon this assertion
            //
            lclNum = curAssertion->op2.lcl.lclNum;
            BitVecOps::RemoveElemD(apTraits, GetAssertionDep(lclNum), index - 1);
        }
    }
    while (optAssertionCount < limit)
    {
        AssertionIndex index        = ++optAssertionCount;
        AssertionDsc*  curAssertion = optGetAssertion(index);
        unsigned       lclNum       = curAssertion->op1.lcl.lclNum;
        BitVecOps::AddElemD(apTraits, GetAssertionDep(lclNum), index - 1);

        //
        // Check for Copy assertions
        //
        if ((curAssertion->assertionKind == OAK_EQUAL) && (curAssertion->op1.kind == O1K_LCLVAR) &&
            (curAssertion->op2.kind == O2K_LCLVAR_COPY))
        {
            //
            //  op2.lcl.lclNum now depends upon this assertion
            //
            lclNum = curAssertion->op2.lcl.lclNum;
            BitVecOps::AddElemD(apTraits, GetAssertionDep(lclNum), index - 1);
        }
    }
}

/*****************************************************************************
 *
 *  The following removes the i-th entry in the value assignment table
 *  used only during local assertion prop
 */

inline void Compiler::optAssertionRemove(AssertionIndex index)
{
    assert(index > 0);
    assert(index <= optAssertionCount);
    PREFAST_ASSUME(optAssertionCount <= optMaxAssertionCount);

    AssertionDsc* curAssertion = optGetAssertion(index);

    //  Two cases to consider if (index == optAssertionCount) then the last
    //  entry in the table is to be removed and that happens automatically when
    //  optAssertionCount is decremented and we can just clear the optAssertionDep bits
    //  The other case is when index < optAssertionCount and here we overwrite the
    //  index-th entry in the table with the data found at the end of the table
    //  Since we are reordering the rable the optAssertionDep bits need to be recreated
    //  using optAssertionReset(0) and optAssertionReset(newAssertionCount) will
    //  correctly update the optAssertionDep bits
    //
    if (index == optAssertionCount)
    {
        unsigned lclNum = curAssertion->op1.lcl.lclNum;
        BitVecOps::RemoveElemD(apTraits, GetAssertionDep(lclNum), index - 1);

        //
        // Check for Copy assertions
        //
        if ((curAssertion->assertionKind == OAK_EQUAL) && (curAssertion->op1.kind == O1K_LCLVAR) &&
            (curAssertion->op2.kind == O2K_LCLVAR_COPY))
        {
            //
            //  op2.lcl.lclNum no longer depends upon this assertion
            //
            lclNum = curAssertion->op2.lcl.lclNum;
            BitVecOps::RemoveElemD(apTraits, GetAssertionDep(lclNum), index - 1);
        }

        optAssertionCount--;
    }
    else
    {
        AssertionDsc*  lastAssertion     = optGetAssertion(optAssertionCount);
        AssertionIndex newAssertionCount = optAssertionCount - 1;

        optAssertionReset(0); // This make optAssertionCount equal 0

        memcpy(curAssertion,  // the entry to be removed
               lastAssertion, // last entry in the table
               sizeof(AssertionDsc));

        optAssertionReset(newAssertionCount);
    }
}
#endif // LOCAL_ASSERTION_PROP

inline void Compiler::LoopDsc::AddModifiedField(Compiler* comp, CORINFO_FIELD_HANDLE fldHnd)
{
    if (lpFieldsModified == nullptr)
    {
        lpFieldsModified =
            new (comp->getAllocatorLoopHoist()) Compiler::LoopDsc::FieldHandleSet(comp->getAllocatorLoopHoist());
    }
    lpFieldsModified->Set(fldHnd, true, FieldHandleSet::Overwrite);
}

inline void Compiler::LoopDsc::AddModifiedElemType(Compiler* comp, CORINFO_CLASS_HANDLE structHnd)
{
    if (lpArrayElemTypesModified == nullptr)
    {
        lpArrayElemTypesModified =
            new (comp->getAllocatorLoopHoist()) Compiler::LoopDsc::ClassHandleSet(comp->getAllocatorLoopHoist());
    }
    lpArrayElemTypesModified->Set(structHnd, true, ClassHandleSet::Overwrite);
}

inline void Compiler::LoopDsc::VERIFY_lpIterTree()
{
#ifdef DEBUG
    assert(lpFlags & LPFLG_ITER);

    // iterTree should be "lcl ASG lcl <op> const"

    assert(lpIterTree->OperIs(GT_ASG));

    GenTree* lhs = lpIterTree->AsOp()->gtOp1;
    GenTree* rhs = lpIterTree->AsOp()->gtOp2;
    assert(lhs->OperGet() == GT_LCL_VAR);

    switch (rhs->gtOper)
    {
        case GT_ADD:
        case GT_SUB:
        case GT_MUL:
        case GT_RSH:
        case GT_LSH:
            break;
        default:
            assert(!"Unknown operator for loop increment");
    }
    assert(rhs->AsOp()->gtOp1->OperGet() == GT_LCL_VAR);
    assert(rhs->AsOp()->gtOp1->AsLclVarCommon()->GetLclNum() == lhs->AsLclVarCommon()->GetLclNum());
    assert(rhs->AsOp()->gtOp2->OperGet() == GT_CNS_INT);
#endif
}

//-----------------------------------------------------------------------------

inline unsigned Compiler::LoopDsc::lpIterVar()
{
    VERIFY_lpIterTree();
    return lpIterTree->AsOp()->gtOp1->AsLclVarCommon()->GetLclNum();
}

//-----------------------------------------------------------------------------

inline int Compiler::LoopDsc::lpIterConst()
{
    VERIFY_lpIterTree();
    GenTree* rhs = lpIterTree->AsOp()->gtOp2;
    return (int)rhs->AsOp()->gtOp2->AsIntCon()->gtIconVal;
}

//-----------------------------------------------------------------------------

inline genTreeOps Compiler::LoopDsc::lpIterOper()
{
    VERIFY_lpIterTree();
    GenTree* rhs = lpIterTree->AsOp()->gtOp2;
    return rhs->OperGet();
}

inline var_types Compiler::LoopDsc::lpIterOperType()
{
    VERIFY_lpIterTree();

    var_types type = lpIterTree->TypeGet();
    assert(genActualType(type) == TYP_INT);

    if ((lpIterTree->gtFlags & GTF_UNSIGNED) && type == TYP_INT)
    {
        type = TYP_UINT;
    }

    return type;
}

inline void Compiler::LoopDsc::VERIFY_lpTestTree()
{
#ifdef DEBUG
    assert(lpFlags & LPFLG_ITER);
    assert(lpTestTree);

    genTreeOps oper = lpTestTree->OperGet();
    assert(GenTree::OperIsCompare(oper));

    GenTree* iterator = nullptr;
    GenTree* limit    = nullptr;
    if ((lpTestTree->AsOp()->gtOp2->gtOper == GT_LCL_VAR) &&
        (lpTestTree->AsOp()->gtOp2->gtFlags & GTF_VAR_ITERATOR) != 0)
    {
        iterator = lpTestTree->AsOp()->gtOp2;
        limit    = lpTestTree->AsOp()->gtOp1;
    }
    else if ((lpTestTree->AsOp()->gtOp1->gtOper == GT_LCL_VAR) &&
             (lpTestTree->AsOp()->gtOp1->gtFlags & GTF_VAR_ITERATOR) != 0)
    {
        iterator = lpTestTree->AsOp()->gtOp1;
        limit    = lpTestTree->AsOp()->gtOp2;
    }
    else
    {
        // one of the nodes has to be the iterator
        assert(false);
    }

    if (lpFlags & LPFLG_CONST_LIMIT)
    {
        assert(limit->OperIsConst());
    }
    if (lpFlags & LPFLG_VAR_LIMIT)
    {
        assert(limit->OperGet() == GT_LCL_VAR);
    }
    if (lpFlags & LPFLG_ARRLEN_LIMIT)
    {
        assert(limit->OperGet() == GT_ARR_LENGTH);
    }
#endif
}

//-----------------------------------------------------------------------------

inline bool Compiler::LoopDsc::lpIsReversed()
{
    VERIFY_lpTestTree();
    return ((lpTestTree->AsOp()->gtOp2->gtOper == GT_LCL_VAR) &&
            (lpTestTree->AsOp()->gtOp2->gtFlags & GTF_VAR_ITERATOR) != 0);
}

//-----------------------------------------------------------------------------

inline genTreeOps Compiler::LoopDsc::lpTestOper()
{
    VERIFY_lpTestTree();
    genTreeOps op = lpTestTree->OperGet();
    return lpIsReversed() ? GenTree::SwapRelop(op) : op;
}

//-----------------------------------------------------------------------------

inline GenTree* Compiler::LoopDsc::lpIterator()
{
    VERIFY_lpTestTree();

    return lpIsReversed() ? lpTestTree->AsOp()->gtOp2 : lpTestTree->AsOp()->gtOp1;
}

//-----------------------------------------------------------------------------

inline GenTree* Compiler::LoopDsc::lpLimit()
{
    VERIFY_lpTestTree();

    return lpIsReversed() ? lpTestTree->AsOp()->gtOp1 : lpTestTree->AsOp()->gtOp2;
}

//-----------------------------------------------------------------------------

inline int Compiler::LoopDsc::lpConstLimit()
{
    VERIFY_lpTestTree();
    assert(lpFlags & LPFLG_CONST_LIMIT);

    GenTree* limit = lpLimit();
    assert(limit->OperIsConst());
    return (int)limit->AsIntCon()->gtIconVal;
}

//-----------------------------------------------------------------------------

inline unsigned Compiler::LoopDsc::lpVarLimit()
{
    VERIFY_lpTestTree();
    assert(lpFlags & LPFLG_VAR_LIMIT);

    GenTree* limit = lpLimit();
    assert(limit->OperGet() == GT_LCL_VAR);
    return limit->AsLclVarCommon()->GetLclNum();
}

//-----------------------------------------------------------------------------

inline bool Compiler::LoopDsc::lpArrLenLimit(Compiler* comp, ArrIndex* index)
{
    VERIFY_lpTestTree();
    assert(lpFlags & LPFLG_ARRLEN_LIMIT);

    GenTree* limit = lpLimit();
    assert(limit->OperGet() == GT_ARR_LENGTH);

    // Check if we have a.length or a[i][j].length
    if (limit->AsArrLen()->ArrRef()->gtOper == GT_LCL_VAR)
    {
        index->arrLcl = limit->AsArrLen()->ArrRef()->AsLclVarCommon()->GetLclNum();
        index->rank   = 0;
        return true;
    }
    // We have a[i].length, extract a[i] pattern.
    else if (limit->AsArrLen()->ArrRef()->gtOper == GT_COMMA)
    {
        return comp->optReconstructArrIndex(limit->AsArrLen()->ArrRef(), index, BAD_VAR_NUM);
    }
    return false;
}

/*****************************************************************************
 *  Is "var" assigned in the loop "lnum" ?
 */

inline bool Compiler::optIsVarAssgLoop(unsigned lnum, unsigned var)
{
    assert(lnum < optLoopCount);
    if (var < lclMAX_ALLSET_TRACKED)
    {
        ALLVARSET_TP vs(AllVarSetOps::MakeSingleton(this, var));
        return optIsSetAssgLoop(lnum, vs) != 0;
    }
    else
    {
        return optIsVarAssigned(optLoopTable[lnum].lpHead->bbNext, optLoopTable[lnum].lpBottom, nullptr, var);
    }
}

/*
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XX                                                                           XX
XX                Optimization activation rules                              XX
XX                                                                           XX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
*/

// are we compiling for fast code, or are we compiling for blended code and
// inside a loop?
// We return true for BLENDED_CODE if the Block executes more than BB_LOOP_WEIGHT/2
inline bool Compiler::optFastCodeOrBlendedLoop(BasicBlock::weight_t bbWeight)
{
    return (compCodeOpt() == FAST_CODE) ||
           ((compCodeOpt() == BLENDED_CODE) && (bbWeight > (BB_LOOP_WEIGHT / 2 * BB_UNITY_WEIGHT)));
}

// are we running on a Intel Pentium 4?
inline bool Compiler::optPentium4(void)
{
    return (info.genCPU == CPU_X86_PENTIUM_4);
}

// should we use add/sub instead of inc/dec? (faster on P4, but increases size)
inline bool Compiler::optAvoidIncDec(BasicBlock::weight_t bbWeight)
{
    return optPentium4() && optFastCodeOrBlendedLoop(bbWeight);
}

// should we try to replace integer multiplication with lea/add/shift sequences?
inline bool Compiler::optAvoidIntMult(void)
{
    return (compCodeOpt() != SMALL_CODE);
}

/*
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XX                          EEInterface                                      XX
XX                      Inline functions                                     XX
XX                                                                           XX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
*/

extern var_types JITtype2varType(CorInfoType type);

#include "ee_il_dll.hpp"

inline CORINFO_METHOD_HANDLE Compiler::eeFindHelper(unsigned helper)
{
    assert(helper < CORINFO_HELP_COUNT);

    /* Helpers are marked by the fact that they are odd numbers
     * force this to be an odd number (will shift it back to extract) */

    return ((CORINFO_METHOD_HANDLE)((((size_t)helper) << 2) + 1));
}

inline CorInfoHelpFunc Compiler::eeGetHelperNum(CORINFO_METHOD_HANDLE method)
{
    // Helpers are marked by the fact that they are odd numbers
    if (!(((size_t)method) & 1))
    {
        return (CORINFO_HELP_UNDEF);
    }
    return ((CorInfoHelpFunc)(((size_t)method) >> 2));
}

inline Compiler::fgWalkResult Compiler::CountSharedStaticHelper(GenTree** pTree, fgWalkData* data)
{
    if (Compiler::IsSharedStaticHelper(*pTree))
    {
        int* pCount = (int*)data->pCallbackData;
        (*pCount)++;
    }

    return WALK_CONTINUE;
}

//  TODO-Cleanup: Replace calls to IsSharedStaticHelper with new HelperCallProperties
//

inline bool Compiler::IsSharedStaticHelper(GenTree* tree)
{
    if (tree->gtOper != GT_CALL || tree->AsCall()->gtCallType != CT_HELPER)
    {
        return false;
    }

    CorInfoHelpFunc helper = eeGetHelperNum(tree->AsCall()->gtCallMethHnd);

    bool result1 =
        // More helpers being added to IsSharedStaticHelper (that have similar behaviors but are not true
        // ShareStaticHelperts)
        helper == CORINFO_HELP_STRCNS || helper == CORINFO_HELP_BOX ||

        // helpers being added to IsSharedStaticHelper
        helper == CORINFO_HELP_GETSTATICFIELDADDR_CONTEXT || helper == CORINFO_HELP_GETSTATICFIELDADDR_TLS ||
        helper == CORINFO_HELP_GETGENERICS_GCSTATIC_BASE || helper == CORINFO_HELP_GETGENERICS_NONGCSTATIC_BASE ||
        helper == CORINFO_HELP_GETGENERICS_GCTHREADSTATIC_BASE ||
        helper == CORINFO_HELP_GETGENERICS_NONGCTHREADSTATIC_BASE ||

        helper == CORINFO_HELP_GETSHARED_GCSTATIC_BASE || helper == CORINFO_HELP_GETSHARED_NONGCSTATIC_BASE ||
        helper == CORINFO_HELP_GETSHARED_GCSTATIC_BASE_NOCTOR ||
        helper == CORINFO_HELP_GETSHARED_NONGCSTATIC_BASE_NOCTOR ||
        helper == CORINFO_HELP_GETSHARED_GCSTATIC_BASE_DYNAMICCLASS ||
        helper == CORINFO_HELP_GETSHARED_NONGCSTATIC_BASE_DYNAMICCLASS ||
        helper == CORINFO_HELP_GETSHARED_GCTHREADSTATIC_BASE ||
        helper == CORINFO_HELP_GETSHARED_NONGCTHREADSTATIC_BASE ||
        helper == CORINFO_HELP_GETSHARED_GCTHREADSTATIC_BASE_NOCTOR ||
        helper == CORINFO_HELP_GETSHARED_NONGCTHREADSTATIC_BASE_NOCTOR ||
        helper == CORINFO_HELP_GETSHARED_GCTHREADSTATIC_BASE_DYNAMICCLASS ||
        helper == CORINFO_HELP_GETSHARED_NONGCTHREADSTATIC_BASE_DYNAMICCLASS ||
#ifdef FEATURE_READYTORUN_COMPILER
        helper == CORINFO_HELP_READYTORUN_STATIC_BASE || helper == CORINFO_HELP_READYTORUN_GENERIC_STATIC_BASE ||
#endif
        helper == CORINFO_HELP_CLASSINIT_SHARED_DYNAMICCLASS;
#if 0
    // See above TODO-Cleanup
    bool result2 = s_helperCallProperties.IsPure(helper) && s_helperCallProperties.NonNullReturn(helper);
    assert (result1 == result2);
#endif
    return result1;
}

inline bool Compiler::IsTreeAlwaysHoistable(GenTree* tree)
{
    if (IsSharedStaticHelper(tree))
    {
        return (GTF_CALL_HOISTABLE & tree->gtFlags) ? true : false;
    }
    else
    {
        return false;
    }
}

inline bool Compiler::IsGcSafePoint(GenTree* tree)
{
    if (tree->IsCall())
    {
        GenTreeCall* call = tree->AsCall();
        if (!call->IsFastTailCall())
        {
            if (call->IsUnmanaged() && call->IsSuppressGCTransition())
            {
                // Both an indirect and user calls can be unmanaged
                // and have a request to suppress the GC transition so
                // the check is done prior to the separate handling of
                // indirect and user calls.
                return false;
            }
            else if (call->gtCallType == CT_INDIRECT)
            {
                return true;
            }
            else if (call->gtCallType == CT_USER_FUNC)
            {
                if ((call->gtCallMoreFlags & GTF_CALL_M_NOGCCHECK) == 0)
                {
                    return true;
                }
            }
            // otherwise we have a CT_HELPER
        }
    }

    return false;
}

//
// Note that we want to have two special FIELD_HANDLES that will both
// be considered non-Data Offset handles
//
// The special values that we use are FLD_GLOBAL_DS and FLD_GLOBAL_FS
//

inline bool jitStaticFldIsGlobAddr(CORINFO_FIELD_HANDLE fldHnd)
{
    return (fldHnd == FLD_GLOBAL_DS || fldHnd == FLD_GLOBAL_FS);
}

#if defined(DEBUG) || defined(FEATURE_JIT_METHOD_PERF) || defined(FEATURE_SIMD) || defined(FEATURE_TRACELOGGING)

inline bool Compiler::eeIsNativeMethod(CORINFO_METHOD_HANDLE method)
{
    return ((((size_t)method) & 0x2) == 0x2);
}

inline CORINFO_METHOD_HANDLE Compiler::eeGetMethodHandleForNative(CORINFO_METHOD_HANDLE method)
{
    assert((((size_t)method) & 0x3) == 0x2);
    return (CORINFO_METHOD_HANDLE)(((size_t)method) & ~0x3);
}
#endif

inline CORINFO_METHOD_HANDLE Compiler::eeMarkNativeTarget(CORINFO_METHOD_HANDLE method)
{
    assert((((size_t)method) & 0x3) == 0);
    if (method == nullptr)
    {
        return method;
    }
    else
    {
        return (CORINFO_METHOD_HANDLE)(((size_t)method) | 0x2);
    }
}

/*
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XX                          Compiler                                         XX
XX                      Inline functions                                     XX
XX                                                                           XX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
*/

#ifndef DEBUG
inline bool Compiler::compStressCompile(compStressArea stressArea, unsigned weightPercentage)
{
    return false;
}
#endif

inline ArenaAllocator* Compiler::compGetArenaAllocator()
{
    return compArenaAllocator;
}

inline bool Compiler::compIsProfilerHookNeeded()
{
#ifdef PROFILING_SUPPORTED
    return compProfilerHookNeeded
           // IL stubs are excluded by VM and we need to do the same even running
           // under a complus env hook to generate profiler hooks
           || (opts.compJitELTHookEnabled && !opts.jitFlags->IsSet(JitFlags::JIT_FLAG_IL_STUB));
#else  // !PROFILING_SUPPORTED
    return false;
#endif // !PROFILING_SUPPORTED
}

/*****************************************************************************
 *
 *  Check for the special case where the object is the constant 0.
 *  As we can't even fold the tree (null+fldOffs), we are left with
 *  op1 and op2 both being a constant. This causes lots of problems.
 *  We simply grab a temp and assign 0 to it and use it in place of the NULL.
 */

inline GenTree* Compiler::impCheckForNullPointer(GenTree* obj)
{
    /* If it is not a GC type, we will be able to fold it.
       So don't need to do anything */

    if (!varTypeIsGC(obj->TypeGet()))
    {
        return obj;
    }

    if (obj->gtOper == GT_CNS_INT)
    {
        assert(obj->gtType == TYP_REF || obj->gtType == TYP_BYREF);

        // We can see non-zero byrefs for RVA statics.
        if (obj->AsIntCon()->gtIconVal != 0)
        {
            assert(obj->gtType == TYP_BYREF);
            return obj;
        }

        unsigned tmp = lvaGrabTemp(true DEBUGARG("CheckForNullPointer"));

        // We don't need to spill while appending as we are only assigning
        // NULL to a freshly-grabbed temp.

        impAssignTempGen(tmp, obj, (unsigned)CHECK_SPILL_NONE);

        obj = gtNewLclvNode(tmp, obj->gtType);
    }

    return obj;
}

/*****************************************************************************
 *
 *  Check for the special case where the object is the methods original 'this' pointer.
 *  Note that, the original 'this' pointer is always local var 0 for non-static method,
 *  even if we might have created the copy of 'this' pointer in lvaArg0Var.
 */

inline bool Compiler::impIsThis(GenTree* obj)
{
    if (compIsForInlining())
    {
        return impInlineInfo->InlinerCompiler->impIsThis(obj);
    }
    else
    {
        return ((obj != nullptr) && (obj->gtOper == GT_LCL_VAR) &&
                lvaIsOriginalThisArg(obj->AsLclVarCommon()->GetLclNum()));
    }
}

/*****************************************************************************
 *
 *  Check to see if the delegate is created using "LDFTN <TOK>" or not.
 */

inline bool Compiler::impIsLDFTN_TOKEN(const BYTE* delegateCreateStart, const BYTE* newobjCodeAddr)
{
    assert(newobjCodeAddr[0] == CEE_NEWOBJ);
    return (newobjCodeAddr - delegateCreateStart == 6 && // LDFTN <TOK> takes 6 bytes
            delegateCreateStart[0] == CEE_PREFIX1 && delegateCreateStart[1] == (CEE_LDFTN & 0xFF));
}

/*****************************************************************************
 *
 *  Check to see if the delegate is created using "DUP LDVIRTFTN <TOK>" or not.
 */

inline bool Compiler::impIsDUP_LDVIRTFTN_TOKEN(const BYTE* delegateCreateStart, const BYTE* newobjCodeAddr)
{
    assert(newobjCodeAddr[0] == CEE_NEWOBJ);
    return (newobjCodeAddr - delegateCreateStart == 7 && // DUP LDVIRTFTN <TOK> takes 6 bytes
            delegateCreateStart[0] == CEE_DUP && delegateCreateStart[1] == CEE_PREFIX1 &&
            delegateCreateStart[2] == (CEE_LDVIRTFTN & 0xFF));
}
/*****************************************************************************
 *
 * Returns true if the compiler instance is created for import only (verification).
 */

inline bool Compiler::compIsForImportOnly()
{
    return opts.jitFlags->IsSet(JitFlags::JIT_FLAG_IMPORT_ONLY);
}

/*****************************************************************************
 *
 *  Returns true if the compiler instance is created for inlining.
 */

inline bool Compiler::compIsForInlining() const
{
    return (impInlineInfo != nullptr);
}

/*****************************************************************************
 *
 *  Check the inline result field in the compiler to see if inlining failed or not.
 */

inline bool Compiler::compDonotInline()
{
    if (compIsForInlining())
    {
        assert(compInlineResult != nullptr);
        return compInlineResult->IsFailure();
    }
    else
    {
        return false;
    }
}

inline bool Compiler::impIsPrimitive(CorInfoType jitType)
{
    return ((CORINFO_TYPE_BOOL <= jitType && jitType <= CORINFO_TYPE_DOUBLE) || jitType == CORINFO_TYPE_PTR);
}

/*****************************************************************************
 *
 *  Get the promotion type of a struct local.
 */

inline Compiler::lvaPromotionType Compiler::lvaGetPromotionType(const LclVarDsc* varDsc)
{
    assert(!varDsc->lvPromoted || varTypeIsPromotable(varDsc) || varDsc->lvUnusedStruct);

    if (!varDsc->lvPromoted)
    {
        // no struct promotion for this LclVar
        return PROMOTION_TYPE_NONE;
    }
    if (varDsc->lvDoNotEnregister)
    {
        // The struct is not enregistered
        return PROMOTION_TYPE_DEPENDENT;
    }
    if (!varDsc->lvIsParam)
    {
        // The struct is a register candidate
        return PROMOTION_TYPE_INDEPENDENT;
    }

    // Has struct promotion for arguments been disabled using COMPlus_JitNoStructPromotion=2
    if (fgNoStructParamPromotion)
    {
        // The struct parameter is not enregistered
        return PROMOTION_TYPE_DEPENDENT;
    }

    // We have a parameter that could be enregistered
    CLANG_FORMAT_COMMENT_ANCHOR;

#if defined(TARGET_AMD64) || defined(TARGET_ARM64)

    // The struct parameter is a register candidate
    return PROMOTION_TYPE_INDEPENDENT;
#else
    // The struct parameter is not enregistered
    return PROMOTION_TYPE_DEPENDENT;
#endif
}

/*****************************************************************************
 *
 *  Get the promotion type of a struct local.
 */

inline Compiler::lvaPromotionType Compiler::lvaGetPromotionType(unsigned varNum)
{
    assert(varNum < lvaCount);
    return lvaGetPromotionType(&lvaTable[varNum]);
}

/*****************************************************************************
 *
 *  Given a field local, get the promotion type of its parent struct local.
 */

inline Compiler::lvaPromotionType Compiler::lvaGetParentPromotionType(const LclVarDsc* varDsc)
{
    assert(varDsc->lvIsStructField);
    assert(varDsc->lvParentLcl < lvaCount);

    lvaPromotionType promotionType = lvaGetPromotionType(varDsc->lvParentLcl);
    assert(promotionType != PROMOTION_TYPE_NONE);
    return promotionType;
}

/*****************************************************************************
 *
 *  Given a field local, get the promotion type of its parent struct local.
 */

inline Compiler::lvaPromotionType Compiler::lvaGetParentPromotionType(unsigned varNum)
{
    assert(varNum < lvaCount);
    return lvaGetParentPromotionType(&lvaTable[varNum]);
}

/*****************************************************************************
 *
 *  Return true if the local is a field local of a promoted struct of type PROMOTION_TYPE_DEPENDENT.
 *  Return false otherwise.
 */

inline bool Compiler::lvaIsFieldOfDependentlyPromotedStruct(const LclVarDsc* varDsc)
{
    if (!varDsc->lvIsStructField)
    {
        return false;
    }

    lvaPromotionType promotionType = lvaGetParentPromotionType(varDsc);
    if (promotionType == PROMOTION_TYPE_DEPENDENT)
    {
        return true;
    }

    assert(promotionType == PROMOTION_TYPE_INDEPENDENT);
    return false;
}

//------------------------------------------------------------------------
// lvaIsGCTracked: Determine whether this var should be reported
//    as tracked for GC purposes.
//
// Arguments:
//    varDsc - the LclVarDsc for the var in question.
//
// Return Value:
//    Returns true if the variable should be reported as tracked in the GC info.
//
// Notes:
//    This never returns true for struct variables, even if they are tracked.
//    This is because struct variables are never tracked as a whole for GC purposes.
//    It is up to the caller to ensure that the fields of struct variables are
//    correctly tracked.
//    On Amd64, we never GC-track fields of dependently promoted structs, even
//    though they may be tracked for optimization purposes.
//    It seems that on x86 and arm, we simply don't track these
//    fields, though I have not verified that.  I attempted to make these GC-tracked,
//    but there was too much logic that depends on these being untracked, so changing
//    this would require non-trivial effort.

inline bool Compiler::lvaIsGCTracked(const LclVarDsc* varDsc)
{
    if (varDsc->lvTracked && (varDsc->lvType == TYP_REF || varDsc->lvType == TYP_BYREF))
    {
        // Stack parameters are always untracked w.r.t. GC reportings
        const bool isStackParam = varDsc->lvIsParam && !varDsc->lvIsRegArg;
#ifdef TARGET_AMD64
        return !isStackParam && !lvaIsFieldOfDependentlyPromotedStruct(varDsc);
#else  // !TARGET_AMD64
        return !isStackParam;
#endif // !TARGET_AMD64
    }
    else
    {
        return false;
    }
}

/*****************************************************************************/
#if MEASURE_CLRAPI_CALLS

inline void Compiler::CLRApiCallEnter(unsigned apix)
{
    if (pCompJitTimer != nullptr)
    {
        pCompJitTimer->CLRApiCallEnter(apix);
    }
}
inline void Compiler::CLRApiCallLeave(unsigned apix)
{
    if (pCompJitTimer != nullptr)
    {
        pCompJitTimer->CLRApiCallLeave(apix);
    }
}

inline void Compiler::CLR_API_Enter(API_ICorJitInfo_Names ename)
{
    CLRApiCallEnter(ename);
}

inline void Compiler::CLR_API_Leave(API_ICorJitInfo_Names ename)
{
    CLRApiCallLeave(ename);
}

#endif // MEASURE_CLRAPI_CALLS

//------------------------------------------------------------------------------
// fgVarIsNeverZeroInitializedInProlog : Check whether the variable is never zero initialized in the prolog.
//
// Arguments:
//    varNum     -       local variable number
//
// Returns:
//             true if this is a special variable that is never zero initialized in the prolog;
//             false otherwise
//

bool Compiler::fgVarIsNeverZeroInitializedInProlog(unsigned varNum)
{
    LclVarDsc* varDsc = lvaGetDesc(varNum);
    bool result = varDsc->lvIsParam || lvaIsOSRLocal(varNum) || (opts.IsOSR() && (varNum == lvaGSSecurityCookie)) ||
                  (varNum == lvaInlinedPInvokeFrameVar) || (varNum == lvaStubArgumentVar) || (varNum == lvaRetAddrVar);

#if FEATURE_FIXED_OUT_ARGS
    result = result || (varNum == lvaPInvokeFrameRegSaveVar) || (varNum == lvaOutgoingArgSpaceVar);
#endif

#if defined(FEATURE_EH_FUNCLETS)
    result = result || (varNum == lvaPSPSym);
#endif

    return result;
}

//------------------------------------------------------------------------------
// fgVarNeedsExplicitZeroInit : Check whether the variable needs an explicit zero initialization.
//
// Arguments:
//    varNum     -       local var number
//    bbInALoop  -       true if the basic block may be in a loop
//    bbIsReturn -       true if the basic block always returns
//
// Returns:
//             true if the var needs explicit zero-initialization in this basic block;
//             false otherwise
//
// Notes:
//     If the variable is not being initialized in a loop, we can avoid explicit zero initialization if
//      - the variable is a gc pointer, or
//      - the variable is a struct with gc pointer fields and either all fields are gc pointer fields
//           or the struct is big enough to guarantee block initialization, or
//      - compInitMem is set and the variable has a long lifetime or has gc fields.
//     In these cases we will insert zero-initialization in the prolog if necessary.

bool Compiler::fgVarNeedsExplicitZeroInit(unsigned varNum, bool bbInALoop, bool bbIsReturn)
{
    LclVarDsc* varDsc = lvaGetDesc(varNum);

    if (lvaIsFieldOfDependentlyPromotedStruct(varDsc))
    {
        // Fields of dependently promoted structs may only be initialized in the prolog when the whole
        // struct is initialized in the prolog.
        return fgVarNeedsExplicitZeroInit(varDsc->lvParentLcl, bbInALoop, bbIsReturn);
    }

    if (bbInALoop && !bbIsReturn)
    {
        return true;
    }

    if (fgVarIsNeverZeroInitializedInProlog(varNum))
    {
        return true;
    }

    if (varTypeIsGC(varDsc->lvType))
    {
        return false;
    }

    if ((varDsc->lvType == TYP_STRUCT) && varDsc->HasGCPtr())
    {
        ClassLayout* layout = varDsc->GetLayout();
        if (layout->GetSlotCount() == layout->GetGCPtrCount())
        {
            return false;
        }

// Below conditions guarantee block initialization, which will initialize
// all struct fields. If the logic for block initialization in CodeGen::genCheckUseBlockInit()
// changes, these conditions need to be updated.
#ifdef TARGET_64BIT
#if defined(TARGET_AMD64)
        // We can clear using aligned SIMD so the threshold is lower,
        // and clears in order which is better for auto-prefetching
        if (roundUp(varDsc->lvSize(), TARGET_POINTER_SIZE) / sizeof(int) > 4)
#else // !defined(TARGET_AMD64)
        if (roundUp(varDsc->lvSize(), TARGET_POINTER_SIZE) / sizeof(int) > 8)
#endif
#else
        if (roundUp(varDsc->lvSize(), TARGET_POINTER_SIZE) / sizeof(int) > 4)
#endif
        {
            return false;
        }
    }

    return !info.compInitMem || (varDsc->lvIsTemp && !varDsc->HasGCPtr());
}

/*****************************************************************************/
ValueNum Compiler::GetUseAsgDefVNOrTreeVN(GenTree* op)
{
    if (op->gtFlags & GTF_VAR_USEASG)
    {
        unsigned lclNum = op->AsLclVarCommon()->GetLclNum();
        unsigned ssaNum = GetSsaNumForLocalVarDef(op);
        return lvaTable[lclNum].GetPerSsaData(ssaNum)->m_vnPair.GetConservative();
    }
    else
    {
        return op->gtVNPair.GetConservative();
    }
}

/*****************************************************************************/
unsigned Compiler::GetSsaNumForLocalVarDef(GenTree* lcl)
{
    // Address-taken variables don't have SSA numbers.
    if (!lvaInSsa(lcl->AsLclVarCommon()->GetLclNum()))
    {
        return SsaConfig::RESERVED_SSA_NUM;
    }

    if (lcl->gtFlags & GTF_VAR_USEASG)
    {
        // It's partial definition of a struct. "lcl" is both used and defined here;
        // we've chosen in this case to annotate "lcl" with the SSA number (and VN) of the use,
        // and to store the SSA number of the def in a side table.
        unsigned ssaNum;
        // In case of a remorph (fgMorph) in CSE/AssertionProp after SSA phase, there
        // wouldn't be an entry for the USEASG portion of the indir addr, return
        // reserved.
        if (!GetOpAsgnVarDefSsaNums()->Lookup(lcl, &ssaNum))
        {
            return SsaConfig::RESERVED_SSA_NUM;
        }
        return ssaNum;
    }
    else
    {
        return lcl->AsLclVarCommon()->GetSsaNum();
    }
}

template <typename TVisitor>
void GenTree::VisitOperands(TVisitor visitor)
{
    switch (OperGet())
    {
        // Leaf nodes
        case GT_LCL_VAR:
        case GT_LCL_FLD:
        case GT_LCL_VAR_ADDR:
        case GT_LCL_FLD_ADDR:
        case GT_CATCH_ARG:
        case GT_LABEL:
        case GT_FTN_ADDR:
        case GT_RET_EXPR:
        case GT_CNS_INT:
        case GT_CNS_LNG:
        case GT_CNS_DBL:
        case GT_CNS_STR:
        case GT_MEMORYBARRIER:
        case GT_JMP:
        case GT_JCC:
        case GT_SETCC:
        case GT_NO_OP:
        case GT_START_NONGC:
        case GT_START_PREEMPTGC:
        case GT_PROF_HOOK:
#if !defined(FEATURE_EH_FUNCLETS)
        case GT_END_LFIN:
#endif // !FEATURE_EH_FUNCLETS
        case GT_PHI_ARG:
        case GT_JMPTABLE:
        case GT_CLS_VAR:
        case GT_CLS_VAR_ADDR:
        case GT_ARGPLACE:
        case GT_PHYSREG:
        case GT_EMITNOP:
        case GT_PINVOKE_PROLOG:
        case GT_PINVOKE_EPILOG:
        case GT_IL_OFFSET:
            return;

        // Unary operators with an optional operand
        case GT_NOP:
        case GT_RETURN:
        case GT_RETFILT:
            if (this->AsUnOp()->gtOp1 == nullptr)
            {
                return;
            }
            __fallthrough;

        // Standard unary operators
        case GT_STORE_LCL_VAR:
        case GT_STORE_LCL_FLD:
        case GT_NOT:
        case GT_NEG:
        case GT_BSWAP:
        case GT_BSWAP16:
        case GT_COPY:
        case GT_RELOAD:
        case GT_ARR_LENGTH:
        case GT_CAST:
        case GT_BITCAST:
        case GT_CKFINITE:
        case GT_LCLHEAP:
        case GT_ADDR:
        case GT_IND:
        case GT_OBJ:
        case GT_BLK:
        case GT_BOX:
        case GT_ALLOCOBJ:
        case GT_INIT_VAL:
        case GT_JTRUE:
        case GT_SWITCH:
        case GT_NULLCHECK:
        case GT_PUTARG_REG:
        case GT_PUTARG_STK:
#if FEATURE_ARG_SPLIT
        case GT_PUTARG_SPLIT:
#endif // FEATURE_ARG_SPLIT
        case GT_RETURNTRAP:
        case GT_KEEPALIVE:
            visitor(this->AsUnOp()->gtOp1);
            return;

// Variadic nodes
#ifdef FEATURE_SIMD
        case GT_SIMD:
            if (this->AsSIMD()->gtSIMDIntrinsicID == SIMDIntrinsicInitN)
            {
                assert(this->AsSIMD()->gtOp1 != nullptr);
                this->AsSIMD()->gtOp1->VisitListOperands(visitor);
            }
            else
            {
                VisitBinOpOperands<TVisitor>(visitor);
            }
            return;
#endif // FEATURE_SIMD

#ifdef FEATURE_HW_INTRINSICS
        case GT_HWINTRINSIC:
            if ((this->AsHWIntrinsic()->gtOp1 != nullptr) && this->AsHWIntrinsic()->gtOp1->OperIsList())
            {
                this->AsHWIntrinsic()->gtOp1->VisitListOperands(visitor);
            }
            else
            {
                VisitBinOpOperands<TVisitor>(visitor);
            }
            return;
#endif // FEATURE_HW_INTRINSICS

        // Special nodes
        case GT_PHI:
            for (GenTreePhi::Use& use : AsPhi()->Uses())
            {
                if (visitor(use.GetNode()) == VisitResult::Abort)
                {
                    break;
                }
            }
            return;

        case GT_FIELD_LIST:
            for (GenTreeFieldList::Use& field : AsFieldList()->Uses())
            {
                if (visitor(field.GetNode()) == VisitResult::Abort)
                {
                    break;
                }
            }
            return;

        case GT_CMPXCHG:
        {
            GenTreeCmpXchg* const cmpXchg = this->AsCmpXchg();
            if (visitor(cmpXchg->gtOpLocation) == VisitResult::Abort)
            {
                return;
            }
            if (visitor(cmpXchg->gtOpValue) == VisitResult::Abort)
            {
                return;
            }
            visitor(cmpXchg->gtOpComparand);
            return;
        }

        case GT_ARR_BOUNDS_CHECK:
#ifdef FEATURE_SIMD
        case GT_SIMD_CHK:
#endif // FEATURE_SIMD
#ifdef FEATURE_HW_INTRINSICS
        case GT_HW_INTRINSIC_CHK:
#endif // FEATURE_HW_INTRINSICS
        {
            GenTreeBoundsChk* const boundsChk = this->AsBoundsChk();
            if (visitor(boundsChk->gtIndex) == VisitResult::Abort)
            {
                return;
            }
            visitor(boundsChk->gtArrLen);
            return;
        }

        case GT_FIELD:
            if (this->AsField()->gtFldObj != nullptr)
            {
                visitor(this->AsField()->gtFldObj);
            }
            return;

        case GT_ARR_ELEM:
        {
            GenTreeArrElem* const arrElem = this->AsArrElem();
            if (visitor(arrElem->gtArrObj) == VisitResult::Abort)
            {
                return;
            }
            for (unsigned i = 0; i < arrElem->gtArrRank; i++)
            {
                if (visitor(arrElem->gtArrInds[i]) == VisitResult::Abort)
                {
                    return;
                }
            }
            return;
        }

        case GT_ARR_OFFSET:
        {
            GenTreeArrOffs* const arrOffs = this->AsArrOffs();
            if (visitor(arrOffs->gtOffset) == VisitResult::Abort)
            {
                return;
            }
            if (visitor(arrOffs->gtIndex) == VisitResult::Abort)
            {
                return;
            }
            visitor(arrOffs->gtArrObj);
            return;
        }

        case GT_DYN_BLK:
        {
            GenTreeDynBlk* const dynBlock = this->AsDynBlk();
            if (visitor(dynBlock->gtOp1) == VisitResult::Abort)
            {
                return;
            }
            visitor(dynBlock->gtDynamicSize);
            return;
        }

        case GT_STORE_DYN_BLK:
        {
            GenTreeDynBlk* const dynBlock = this->AsDynBlk();
            if (visitor(dynBlock->gtOp1) == VisitResult::Abort)
            {
                return;
            }
            if (visitor(dynBlock->gtOp2) == VisitResult::Abort)
            {
                return;
            }
            visitor(dynBlock->gtDynamicSize);
            return;
        }

        case GT_CALL:
        {
            GenTreeCall* const call = this->AsCall();
            if ((call->gtCallThisArg != nullptr) && (visitor(call->gtCallThisArg->GetNode()) == VisitResult::Abort))
            {
                return;
            }

            for (GenTreeCall::Use& use : call->Args())
            {
                if (visitor(use.GetNode()) == VisitResult::Abort)
                {
                    return;
                }
            }

            for (GenTreeCall::Use& use : call->LateArgs())
            {
                if (visitor(use.GetNode()) == VisitResult::Abort)
                {
                    return;
                }
            }

            if (call->gtCallType == CT_INDIRECT)
            {
                if ((call->gtCallCookie != nullptr) && (visitor(call->gtCallCookie) == VisitResult::Abort))
                {
                    return;
                }
                if ((call->gtCallAddr != nullptr) && (visitor(call->gtCallAddr) == VisitResult::Abort))
                {
                    return;
                }
            }
            if ((call->gtControlExpr != nullptr))
            {
                visitor(call->gtControlExpr);
            }
            return;
        }

        // Binary nodes
        default:
            assert(this->OperIsBinary());
            VisitBinOpOperands<TVisitor>(visitor);
            return;
    }
}

template <typename TVisitor>
GenTree::VisitResult GenTree::VisitListOperands(TVisitor visitor)
{
    for (GenTreeArgList* node = this->AsArgList(); node != nullptr; node = node->Rest())
    {
        if (visitor(node->gtOp1) == VisitResult::Abort)
        {
            return VisitResult::Abort;
        }
    }

    return VisitResult::Continue;
}

template <typename TVisitor>
void GenTree::VisitBinOpOperands(TVisitor visitor)
{
    assert(this->OperIsBinary());

    GenTreeOp* const op = this->AsOp();

    GenTree* const op1 = op->gtOp1;
    if ((op1 != nullptr) && (visitor(op1) == VisitResult::Abort))
    {
        return;
    }

    GenTree* const op2 = op->gtOp2;
    if (op2 != nullptr)
    {
        visitor(op2);
    }
}

/*****************************************************************************
 *  operator new
 *
 *  Note that compiler's allocator is an arena allocator that returns memory that is
 *  not zero-initialized and can contain data from a prior allocation lifetime.
 */

inline void* __cdecl operator new(size_t sz, Compiler* compiler, CompMemKind cmk)
{
    return compiler->getAllocator(cmk).allocate<char>(sz);
}

inline void* __cdecl operator new[](size_t sz, Compiler* compiler, CompMemKind cmk)
{
    return compiler->getAllocator(cmk).allocate<char>(sz);
}

inline void* __cdecl operator new(size_t sz, void* p, const jitstd::placement_t& /* syntax_difference */)
{
    return p;
}

/*****************************************************************************/

#ifdef DEBUG

inline void printRegMask(regMaskTP mask)
{
    printf(REG_MASK_ALL_FMT, mask);
}

inline char* regMaskToString(regMaskTP mask, Compiler* context)
{
    const size_t cchRegMask = 24;
    char*        regmask    = new (context, CMK_Unknown) char[cchRegMask];

    sprintf_s(regmask, cchRegMask, REG_MASK_ALL_FMT, mask);

    return regmask;
}

inline void printRegMaskInt(regMaskTP mask)
{
    printf(REG_MASK_INT_FMT, (mask & RBM_ALLINT));
}

inline char* regMaskIntToString(regMaskTP mask, Compiler* context)
{
    const size_t cchRegMask = 24;
    char*        regmask    = new (context, CMK_Unknown) char[cchRegMask];

    sprintf_s(regmask, cchRegMask, REG_MASK_INT_FMT, (mask & RBM_ALLINT));

    return regmask;
}

#endif // DEBUG

inline static bool StructHasOverlappingFields(DWORD attribs)
{
    return ((attribs & CORINFO_FLG_OVERLAPPING_FIELDS) != 0);
}

inline static bool StructHasCustomLayout(DWORD attribs)
{
    return ((attribs & CORINFO_FLG_CUSTOMLAYOUT) != 0);
}

inline static bool StructHasNoPromotionFlagSet(DWORD attribs)
{
    return ((attribs & CORINFO_FLG_DONT_PROMOTE) != 0);
}

/*****************************************************************************
 * This node should not be referenced by anyone now. Set its values to garbage
 * to catch extra references
 */

inline void DEBUG_DESTROY_NODE(GenTree* tree)
{
#ifdef DEBUG
    // printf("DEBUG_DESTROY_NODE for [0x%08x]\n", tree);

    // Save gtOper in case we want to find out what this node was
    tree->gtOperSave = tree->gtOper;

    tree->gtType = TYP_UNDEF;
    tree->gtFlags |= 0xFFFFFFFF & ~GTF_NODE_MASK;
    if (tree->OperIsSimple())
    {
        tree->AsOp()->gtOp1 = tree->AsOp()->gtOp2 = nullptr;
    }
    // Must do this last, because the "AsOp()" check above will fail otherwise.
    // Don't call SetOper, because GT_COUNT is not a valid value
    tree->gtOper = GT_COUNT;
#endif
}

//------------------------------------------------------------------------------
// lvRefCnt: access reference count for this local var
//
// Arguments:
//    state: the requestor's expected ref count state; defaults to RCS_NORMAL
//
// Return Value:
//    Ref count for the local.

inline unsigned short LclVarDsc::lvRefCnt(RefCountState state) const
{

#if defined(DEBUG)
    assert(state != RCS_INVALID);
    Compiler* compiler = JitTls::GetCompiler();
    assert(compiler->lvaRefCountState == state);
#endif

    if (lvImplicitlyReferenced && (m_lvRefCnt == 0))
    {
        return 1;
    }

    return m_lvRefCnt;
}

//------------------------------------------------------------------------------
// incLvRefCnt: increment reference count for this local var
//
// Arguments:
//    delta: the amount of the increment
//    state: the requestor's expected ref count state; defaults to RCS_NORMAL
//
// Notes:
//    It is currently the caller's responsibilty to ensure this increment
//    will not cause overflow.

inline void LclVarDsc::incLvRefCnt(unsigned short delta, RefCountState state)
{

#if defined(DEBUG)
    assert(state != RCS_INVALID);
    Compiler* compiler = JitTls::GetCompiler();
    assert(compiler->lvaRefCountState == state);
#endif

    unsigned short oldRefCnt = m_lvRefCnt;
    m_lvRefCnt += delta;
    assert(m_lvRefCnt >= oldRefCnt);
}

//------------------------------------------------------------------------------
// setLvRefCnt: set the reference count for this local var
//
// Arguments:
//    newValue: the desired new reference count
//    state: the requestor's expected ref count state; defaults to RCS_NORMAL
//
// Notes:
//    Generally after calling v->setLvRefCnt(Y), v->lvRefCnt() == Y.
//    However this may not be true when v->lvImplicitlyReferenced == 1.

inline void LclVarDsc::setLvRefCnt(unsigned short newValue, RefCountState state)
{

#if defined(DEBUG)
    assert(state != RCS_INVALID);
    Compiler* compiler = JitTls::GetCompiler();
    assert(compiler->lvaRefCountState == state);
#endif

    m_lvRefCnt = newValue;
}

//------------------------------------------------------------------------------
// lvRefCntWtd: access wighted reference count for this local var
//
// Arguments:
//    state: the requestor's expected ref count state; defaults to RCS_NORMAL
//
// Return Value:
//    Weighted ref count for the local.

inline BasicBlock::weight_t LclVarDsc::lvRefCntWtd(RefCountState state) const
{

#if defined(DEBUG)
    assert(state != RCS_INVALID);
    Compiler* compiler = JitTls::GetCompiler();
    assert(compiler->lvaRefCountState == state);
#endif

    if (lvImplicitlyReferenced && (m_lvRefCntWtd == 0))
    {
        return BB_UNITY_WEIGHT;
    }

    return m_lvRefCntWtd;
}

//------------------------------------------------------------------------------
// incLvRefCntWtd: increment weighted reference count for this local var
//
// Arguments:
//    delta: the amount of the increment
//    state: the requestor's expected ref count state; defaults to RCS_NORMAL
//
// Notes:
//    It is currently the caller's responsibilty to ensure this increment
//    will not cause overflow.

inline void LclVarDsc::incLvRefCntWtd(BasicBlock::weight_t delta, RefCountState state)
{

#if defined(DEBUG)
    assert(state != RCS_INVALID);
    Compiler* compiler = JitTls::GetCompiler();
    assert(compiler->lvaRefCountState == state);
#endif

    BasicBlock::weight_t oldRefCntWtd = m_lvRefCntWtd;
    m_lvRefCntWtd += delta;
    assert(m_lvRefCntWtd >= oldRefCntWtd);
}

//------------------------------------------------------------------------------
// setLvRefCntWtd: set the weighted reference count for this local var
//
// Arguments:
//    newValue: the desired new weighted reference count
//    state: the requestor's expected ref count state; defaults to RCS_NORMAL
//
// Notes:
//    Generally after calling v->setLvRefCntWtd(Y), v->lvRefCntWtd() == Y.
//    However this may not be true when v->lvImplicitlyReferenced == 1.

inline void LclVarDsc::setLvRefCntWtd(BasicBlock::weight_t newValue, RefCountState state)
{

#if defined(DEBUG)
    assert(state != RCS_INVALID);
    Compiler* compiler = JitTls::GetCompiler();
    assert(compiler->lvaRefCountState == state);
#endif

    m_lvRefCntWtd = newValue;
}

/*****************************************************************************/
#endif //_COMPILER_HPP_
/*****************************************************************************/
