// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "jitpch.h"
#ifdef _MSC_VER
#pragma hdrstop
#endif

//------------------------------------------------------------------------
// importer_vectorization.cpp
//
// This file is responsible for various (partial) vectorizations during import phase,
// e.g. the following APIs are currently supported:
//
//   1) String.Equals(string, string)
//   2) String.Equals(string, string, StringComparison.Ordinal)
//   3) str.Equals(string)
//   4) str.Equals(String, StringComparison.Ordinal)
//   5) str.StartsWith(string, StringComparison.Ordinal)
//   6) MemoryExtensions.SequenceEqual<char>(ROS<char>, ROS<char>)
//   7) MemoryExtensions.Equals(ROS<char>, ROS<char>, StringComparison.Ordinal)
//   8) MemoryExtensions.StartsWith<char>(ROS<char>, ROS<char>)
//   9) MemoryExtensions.StartsWith(ROS<char>, ROS<char>, StringComparison.Ordinal)
//
// When one of the arguments is a constant string of a [0..32] size so we can inline
// a vectorized comparison against it using SWAR or SIMD techniques (e.g. via two V256 vectors)
//
// We might add these in future:
//   1) OrdinalIgnoreCase for everything above
//   2) Span.CopyTo
//   3) Spans/Arrays of bytes (e.g. UTF8) against a constant RVA data
//

//------------------------------------------------------------------------
// impExpandHalfConstEqualsSIMD: Attempts to unroll and vectorize
//    Equals against a constant WCHAR data for Length in [8..32] range
//    using SIMD instructions. C# equivalent of what this function emits:
//
//    bool IsTestString(ReadOnlySpan<char> span)
//    {
//        // Length and Null checks are not handled here
//        ref char s = ref MemoryMarshal.GetReference(span);
//        var v1 = Vector128.LoadUnsafe(ref s);
//        var v1 = Vector128.LoadUnsafe(ref s, span.Length - Vector128<ushort>.Count);
//        var cns1 = Vector128.Create('T', 'e', 's', 't', 'S', 't', 'r', 'i');
//        var cns2 = Vector128.Create('s', 't', 'S', 't', 'r', 'i', 'n', 'g');
//        return ((v1 ^ cns1) | (v2 ^ cns2)) == Vector<ushort>.Zero;
//
//        // for:
//        // return span.SequenceEqual("TestString");
//    }
//
// Arguments:
//    data -       Pointer to a data to vectorize
//    cns -        Constant data (array of 2-byte chars)
//    len -        Number of chars in the cns
//    dataOffset - Offset for data
//
// Return Value:
//    A pointer to the newly created SIMD node or nullptr if unrolling is not
//    possible or not profitable
//
// Notes:
//    This function doesn't check obj for null or its Length, it's just an internal helper
//    for impExpandHalfConstEquals
//
GenTree* Compiler::impExpandHalfConstEqualsSIMD(GenTreeLclVar* data, WCHAR* cns, int len, int dataOffset)
{
    assert(len >= 8 && len <= 32);

#if defined(FEATURE_HW_INTRINSICS) && defined(TARGET_64BIT)
    if (!compOpportunisticallyDependsOn(InstructionSet_Vector128))
    {
        // We need SSE2 or ADVSIMD at least
        return nullptr;
    }

    CorInfoType baseType = CORINFO_TYPE_ULONG;

    int       simdSize;
    var_types simdType;

    NamedIntrinsic niZero;
    NamedIntrinsic niEquals;
    NamedIntrinsic niCreate;

    GenTree* cnsVec1;
    GenTree* cnsVec2;

    // Optimization: don't use two vectors for Length == 8 or 16
    bool useSingleVector = false;

#if defined(TARGET_XARCH)
    if (compOpportunisticallyDependsOn(InstructionSet_Vector256) && len >= 16)
    {
        // Handle [16..32] inputs via two Vector256
        assert(len >= 16 && len <= 32);

        simdSize = 32;
        simdType = TYP_SIMD32;

        niZero   = NI_Vector256_get_Zero;
        niEquals = NI_Vector256_op_Equality;
        niCreate = NI_Vector256_Create;

        // Special case: use a single vector for Length == 16
        useSingleVector = len == 16;

        assert(sizeof(ssize_t) == 8); // this code is guarded with TARGET_64BIT
        GenTree* long1 = gtNewIconNode(*(ssize_t*)(cns + 0), TYP_LONG);
        GenTree* long2 = gtNewIconNode(*(ssize_t*)(cns + 4), TYP_LONG);
        GenTree* long3 = gtNewIconNode(*(ssize_t*)(cns + 8), TYP_LONG);
        GenTree* long4 = gtNewIconNode(*(ssize_t*)(cns + 12), TYP_LONG);
        cnsVec1        = gtNewSimdHWIntrinsicNode(simdType, long1, long2, long3, long4, niCreate, baseType, simdSize);

        // cnsVec2 most likely overlaps with cnsVec1:
        GenTree* long5 = gtNewIconNode(*(ssize_t*)(cns + len - 16), TYP_LONG);
        GenTree* long6 = gtNewIconNode(*(ssize_t*)(cns + len - 12), TYP_LONG);
        GenTree* long7 = gtNewIconNode(*(ssize_t*)(cns + len - 8), TYP_LONG);
        GenTree* long8 = gtNewIconNode(*(ssize_t*)(cns + len - 4), TYP_LONG);
        cnsVec2        = gtNewSimdHWIntrinsicNode(simdType, long5, long6, long7, long8, niCreate, baseType, simdSize);
    }
    else
#endif
        if (len <= 16)
    {
        // Handle [8..16] inputs via two Vector128
        assert(len >= 8 && len <= 16);

        simdSize = 16;
        simdType = TYP_SIMD16;

        niZero   = NI_Vector128_get_Zero;
        niEquals = NI_Vector128_op_Equality;
        niCreate = NI_Vector128_Create;

        // Special case: use a single vector for Length == 8
        useSingleVector = len == 8;

        assert(sizeof(ssize_t) == 8); // this code is guarded with TARGET_64BIT
        GenTree* long1 = gtNewIconNode(*(ssize_t*)(cns + 0), TYP_LONG);
        GenTree* long2 = gtNewIconNode(*(ssize_t*)(cns + 4), TYP_LONG);
        cnsVec1        = gtNewSimdHWIntrinsicNode(simdType, long1, long2, niCreate, baseType, simdSize);

        // cnsVec2 most likely overlaps with cnsVec1:
        GenTree* long3 = gtNewIconNode(*(ssize_t*)(cns + len - 8), TYP_LONG);
        GenTree* long4 = gtNewIconNode(*(ssize_t*)(cns + len - 4), TYP_LONG);
        cnsVec2        = gtNewSimdHWIntrinsicNode(simdType, long3, long4, niCreate, baseType, simdSize);
    }
    else
    {
        JITDUMP("impExpandHalfConstEqualsSIMD: No V256 support and data is too big for V128\n");
        // NOTE: We might consider using four V128 for ARM64
        return nullptr;
    }

    GenTree* zero = gtNewSimdHWIntrinsicNode(simdType, niZero, baseType, simdSize);

    GenTree* offset1  = gtNewIconNode(dataOffset, TYP_I_IMPL);
    GenTree* offset2  = gtNewIconNode(dataOffset + len * sizeof(USHORT) - simdSize, TYP_I_IMPL);
    GenTree* dataPtr1 = gtNewOperNode(GT_ADD, TYP_BYREF, data, offset1);
    GenTree* dataPtr2 = gtNewOperNode(GT_ADD, TYP_BYREF, gtClone(data), offset2);

    GenTree* vec1 = gtNewIndir(simdType, dataPtr1);
    GenTree* vec2 = gtNewIndir(simdType, dataPtr2);

    // TODO-Unroll-CQ: Spill vec1 and vec2 for better pipelining, currently we end up emitting:
    //
    //   vmovdqu  xmm0, xmmword ptr [rcx+12]
    //   vpxor    xmm0, xmm0, xmmword ptr[reloc @RWD00]
    //   vmovdqu  xmm1, xmmword ptr [rcx+20]
    //   vpxor    xmm1, xmm1, xmmword ptr[reloc @RWD16]
    //
    // While we should re-order them to be:
    //
    //   vmovdqu  xmm0, xmmword ptr [rcx+12]
    //   vmovdqu  xmm1, xmmword ptr [rcx+20]
    //   vpxor    xmm0, xmm0, xmmword ptr[reloc @RWD00]
    //   vpxor    xmm1, xmm1, xmmword ptr[reloc @RWD16]
    //

    // ((v1 ^ cns1) | (v2 ^ cns2)) == zero
    GenTree* xor1 = gtNewSimdBinOpNode(GT_XOR, simdType, vec1, cnsVec1, baseType, simdSize, false);
    GenTree* xor2 = gtNewSimdBinOpNode(GT_XOR, simdType, vec2, cnsVec2, baseType, simdSize, false);
    GenTree* orr  = gtNewSimdBinOpNode(GT_OR, simdType, xor1, xor2, baseType, simdSize, false);
    return gtNewSimdHWIntrinsicNode(TYP_BOOL, useSingleVector ? xor1 : orr, zero, niEquals, baseType, simdSize);
#else
    return nullptr;
#endif
}

//------------------------------------------------------------------------
// impCreateCompareInd: creates the following tree:
//
//  *  EQ        int
//  +--*  IND       <type>
//  |  \--*  ADD       byref
//  |     +--*  <obj>
//  |     \--*  CNS_INT   <offset>
//  \--*  CNS_INT   <value>
//
// Arguments:
//    comp -       Compiler object
//    obj -        GenTree representing data pointer
//    type -       type for the IND node
//    offset -     offset for the data pointer
//    value -      constant value to compare against
//
// Return Value:
//    A tree with indirect load and comparison
//
static GenTree* impCreateCompareInd(Compiler* comp, GenTreeLclVar* obj, var_types type, ssize_t offset, ssize_t value)
{
    GenTree* offsetTree    = comp->gtNewIconNode(offset, TYP_I_IMPL);
    GenTree* addOffsetTree = comp->gtNewOperNode(GT_ADD, TYP_BYREF, obj, offsetTree);
    GenTree* indirTree     = comp->gtNewIndir(type, addOffsetTree);
    GenTree* valueTree     = comp->gtNewIconNode(value, genActualType(type));
    return comp->gtNewOperNode(GT_EQ, TYP_INT, indirTree, valueTree);
}

//------------------------------------------------------------------------
// impExpandHalfConstEqualsSWAR: Attempts to unroll and vectorize
//    Equals against a constant WCHAR data for Length in [1..8] range
//    using SWAR (a sort of SIMD but for GPR registers and instructions)
//
// Arguments:
//    data -       Pointer to a data to vectorize
//    cns -        Constant data (array of 2-byte chars)
//    len -        Number of chars in the cns
//    dataOffset - Offset for data
//
// Return Value:
//    A pointer to the newly created SWAR node or nullptr if unrolling is not
//    possible or not profitable
//
// Notes:
//    This function doesn't check obj for null or its Length, it's just an internal helper
//    for impExpandHalfConstEquals
//
GenTree* Compiler::impExpandHalfConstEqualsSWAR(GenTreeLclVar* data, WCHAR* cns, int len, int dataOffset)
{
    assert(len >= 1 && len <= 8);

// Compose Int32 or Int64 values from ushort components
#define MAKEINT32(c1, c2) ((UINT64)c2 << 16) | ((UINT64)c1 << 0)
#define MAKEINT64(c1, c2, c3, c4) ((UINT64)c4 << 48) | ((UINT64)c3 << 32) | ((UINT64)c2 << 16) | ((UINT64)c1 << 0)

    if (len == 1)
    {
        //   [ ch1 ]
        //   [value]
        //
        return impCreateCompareInd(this, data, TYP_SHORT, dataOffset, cns[0]);
    }
    if (len == 2)
    {
        //   [ ch1 ][ ch2 ]
        //   [   value    ]
        //
        const UINT32 value = MAKEINT32(cns[0], cns[1]);
        return impCreateCompareInd(this, data, TYP_INT, dataOffset, value);
    }
#ifdef TARGET_64BIT
    if (len == 3)
    {
        // handle len = 3 via two Int32 with overlapping:
        //
        //   [ ch1 ][ ch2 ][ ch3 ]
        //   [   value1   ]
        //          [   value2   ]
        //
        // where offset for value2 is 2 bytes (1 char)
        //
        UINT32   value1     = MAKEINT32(cns[0], cns[1]);
        UINT32   value2     = MAKEINT32(cns[1], cns[2]);
        GenTree* firstIndir = impCreateCompareInd(this, data, TYP_INT, dataOffset, value1);
        GenTree* secondIndir =
            impCreateCompareInd(this, gtClone(data)->AsLclVar(), TYP_INT, dataOffset + sizeof(USHORT), value2);

        // TODO-Unroll-CQ: Consider merging two indirs via XOR instead of QMARK
        // e.g. gtNewOperNode(GT_XOR, TYP_INT, firstIndir, secondIndir);
        // but it currently has CQ issues (redundant movs)
        GenTreeColon* doubleIndirColon = gtNewColonNode(TYP_INT, secondIndir, gtNewFalse());
        return gtNewQmarkNode(TYP_INT, firstIndir, doubleIndirColon);
    }

    assert(len >= 4 && len <= 8);

    UINT64 value1 = MAKEINT64(cns[0], cns[1], cns[2], cns[3]);
    if (len == 4)
    {
        //   [ ch1 ][ ch2 ][ ch3 ][ ch4 ]
        //   [          value           ]
        //
        return impCreateCompareInd(this, data, TYP_LONG, dataOffset, value1);
    }

    // For 5..7 value2 will overlap with value1, e.g. for Length == 6:
    //
    //   [ ch1 ][ ch2 ][ ch3 ][ ch4 ][ ch5 ][ ch6 ]
    //   [          value1          ]
    //                 [          value2          ]
    //
    UINT64   value2     = MAKEINT64(cns[len - 4], cns[len - 3], cns[len - 2], cns[len - 1]);
    GenTree* firstIndir = impCreateCompareInd(this, data, TYP_LONG, dataOffset, value1);

    ssize_t  offset      = dataOffset + len * sizeof(WCHAR) - sizeof(UINT64);
    GenTree* secondIndir = impCreateCompareInd(this, gtClone(data)->AsLclVar(), TYP_LONG, offset, value2);

    // TODO-Unroll-CQ: Consider merging two indirs via XOR instead of QMARK
    GenTreeColon* doubleIndirColon = gtNewColonNode(TYP_INT, secondIndir, gtNewFalse());
    return gtNewQmarkNode(TYP_INT, firstIndir, doubleIndirColon);
#else // TARGET_64BIT
    return nullptr;
#endif
}

//------------------------------------------------------------------------
// impExpandHalfConstEquals: Attempts to unroll and vectorize
//    Equals against a constant WCHAR data for Length in [8..32] range
//    using either SWAR or SIMD. In a general case it will look like this:
//
//    bool equals = obj != null && obj.Length == len && (SWAR or SIMD)
//
// Arguments:
//    data         - Pointer (LCL_VAR) to a data to vectorize
//    lengthFld    - Pointer (LCL_VAR or GT_FIELD) to Length field
//    checkForNull - Check data for null
//    startsWith   - Is it StartsWith or Equals?
//    cns          - Constant data (array of 2-byte chars)
//    len          - Number of 2-byte chars in the cns
//    dataOffset   - Offset for data
//
// Return Value:
//    A pointer to the newly created SIMD node or nullptr if unrolling is not
//    possible or not profitable
//
GenTree* Compiler::impExpandHalfConstEquals(GenTreeLclVar* data,
                                            GenTree*       lengthFld,
                                            bool           checkForNull,
                                            bool           startsWith,
                                            WCHAR*         cnsData,
                                            int            len,
                                            int            dataOffset)
{
    assert(len >= 0);

    if (compCurBB->isRunRarely())
    {
        // Not profitable to expand
        JITDUMP("impExpandHalfConstEquals: block is cold - not profitable to expand.\n");
        return nullptr;
    }

    if ((compIsForInlining() ? (fgBBcount + impInlineRoot()->fgBBcount) : (fgBBcount)) > 20)
    {
        // We don't want to unroll too much and in big methods
        // TODO-Unroll-CQ: come up with some better heuristic/budget
        JITDUMP("impExpandHalfConstEquals: method has too many BBs (>20) - not profitable to expand.\n");
        return nullptr;
    }

    const genTreeOps cmpOp         = startsWith ? GT_GE : GT_EQ;
    GenTree*         elementsCount = gtNewIconNode(len);
    GenTree*         lenCheckNode;
    if (len == 0)
    {
        // For zero length we don't need to compare content, the following expression is enough:
        //
        //   varData != null && lengthFld == 0
        //
        lenCheckNode = gtNewOperNode(cmpOp, TYP_INT, lengthFld, elementsCount);
    }
    else
    {
        assert(cnsData != nullptr);

        GenTree* indirCmp = nullptr;
        if (len < 8) // SWAR impl supports len == 8 but we'd better give it to SIMD
        {
            indirCmp = impExpandHalfConstEqualsSWAR(gtClone(data)->AsLclVar(), cnsData, len, dataOffset);
        }
        else if (len <= 32)
        {
            indirCmp = impExpandHalfConstEqualsSIMD(gtClone(data)->AsLclVar(), cnsData, len, dataOffset);
        }

        if (indirCmp == nullptr)
        {
            JITDUMP("unable to compose indirCmp\n");
            return nullptr;
        }

        GenTreeColon* lenCheckColon = gtNewColonNode(TYP_INT, indirCmp, gtNewFalse());

        // For StartsWith we use GT_GE, e.g.: `x.Length >= 10`
        lenCheckNode = gtNewQmarkNode(TYP_INT, gtNewOperNode(cmpOp, TYP_INT, lengthFld, elementsCount), lenCheckColon);
    }

    GenTree* rootQmark;
    if (checkForNull)
    {
        // varData == nullptr
        GenTreeColon* nullCheckColon = gtNewColonNode(TYP_INT, lenCheckNode, gtNewFalse());
        rootQmark = gtNewQmarkNode(TYP_INT, gtNewOperNode(GT_NE, TYP_INT, data, gtNewNull()), nullCheckColon);
    }
    else
    {
        // no nullcheck, just "obj.Length == len && (SWAR or SIMD)"
        rootQmark = lenCheckNode;
    }

    return rootQmark;
}

//------------------------------------------------------------------------
// impGetStrConFromSpan: Try to obtain string literal out of a span:
//  var span = "str".AsSpan();
//  var span = (ReadOnlySpan<char>)"str"
//
// Arguments:
//    span - String_op_Implicit or MemoryExtensions_AsSpan call
//           with a string literal
//
// Returns:
//    GenTreeStrCon node or nullptr
//
GenTreeStrCon* Compiler::impGetStrConFromSpan(GenTree* span)
{
    GenTreeCall* argCall = nullptr;
    if (span->OperIs(GT_RET_EXPR))
    {
        // NOTE: we don't support chains of RET_EXPR here
        GenTree* inlineCandidate = span->AsRetExpr()->gtInlineCandidate;
        if (inlineCandidate->OperIs(GT_CALL))
        {
            argCall = inlineCandidate->AsCall();
        }
    }
    else if (span->OperIs(GT_CALL))
    {
        argCall = span->AsCall();
    }

    if ((argCall != nullptr) && ((argCall->gtCallMoreFlags & GTF_CALL_M_SPECIAL_INTRINSIC) != 0))
    {
        const NamedIntrinsic ni = lookupNamedIntrinsic(argCall->gtCallMethHnd);
        if ((ni == NI_System_MemoryExtensions_AsSpan) || (ni == NI_System_String_op_Implicit))
        {
            assert(argCall->gtCallArgs->GetNext() == nullptr);
            if (argCall->gtCallArgs->GetNode()->OperIs(GT_CNS_STR))
            {
                return argCall->gtCallArgs->GetNode()->AsStrCon();
            }
        }
    }
    return nullptr;
}

//------------------------------------------------------------------------
// impStringEqualsOrStartsWith: The main entry-point for String methods
//   We're going to unroll & vectorize the following cases:
//    1) String.Equals(obj, "cns")
//    2) String.Equals(obj, "cns", StringComparison.Ordinal)
//    3) String.Equals("cns", obj)
//    4) String.Equals("cns", obj, StringComparison.Ordinal)
//    5) obj.Equals("cns")
//    5) obj.Equals("cns")
//    6) obj.Equals("cns", StringComparison.Ordinal)
//    7) "cns".Equals(obj)
//    8) "cns".Equals(obj, StringComparison.Ordinal)
//    9) obj.StartsWith("cns", StringComparison.Ordinal)
//   10) "cns".StartsWith(obj, StringComparison.Ordinal)
//
//   For cases 5, 6 and 9 we don't emit "obj != null"
//   NOTE: String.Equals(object) is not supported currently
//
// Arguments:
//    startsWith  - Is it StartsWith or Equals?
//    sig         - signature of StartsWith or Equals method
//    methodFlags - its flags
//
// Returns:
//    GenTree representing vectorized comparison or nullptr
//
GenTree* Compiler::impStringEqualsOrStartsWith(bool startsWith, CORINFO_SIG_INFO* sig, unsigned methodFlags)
{
    const bool isStatic  = methodFlags & CORINFO_FLG_STATIC;
    const int  argsCount = sig->numArgs + (isStatic ? 0 : 1);

    GenTree* op1;
    GenTree* op2;
    if (argsCount == 3) // overload with StringComparison
    {
        if (!impStackTop(0).val->IsIntegralConst(4)) // StringComparison.Ordinal
        {
            // TODO-Unroll-CQ: Unroll & vectorize OrdinalIgnoreCase
            return nullptr;
        }
        op1 = impStackTop(2).val;
        op2 = impStackTop(1).val;
    }
    else
    {
        assert(argsCount == 2);
        op1 = impStackTop(1).val;
        op2 = impStackTop(0).val;
    }

    if (!(op1->OperIs(GT_CNS_STR) ^ op2->OperIs(GT_CNS_STR)))
    {
        // either op1 or op2 has to be CNS_STR, but not both - that case is optimized
        // just fine as is.
        return nullptr;
    }

    GenTree*       varStr;
    GenTreeStrCon* cnsStr;
    if (op1->OperIs(GT_CNS_STR))
    {
        cnsStr = op1->AsStrCon();
        varStr = op2;
    }
    else
    {
        cnsStr = op2->AsStrCon();
        varStr = op1;
    }

    bool needsNullcheck = true;
    if ((op1 != cnsStr) && !isStatic)
    {
        // for the following cases we should not check varStr for null:
        //
        //  obj.Equals("cns")
        //  obj.Equals("cns", StringComparison.Ordinal)
        //  obj.StartsWith("cns", StringComparison.Ordinal)
        //
        // instead, it should throw NRE if it's null
        needsNullcheck = false;
    }

    int             cnsLength = -1;
    const char16_t* str       = nullptr;
    if (cnsStr->IsStringEmptyField())
    {
        // check for fake "" first
        cnsLength = 0;
        JITDUMP("Trying to unroll String.Equals|StartsWith(op1, \"\")...\n", str)
    }
    else
    {
        str = info.compCompHnd->getStringLiteral(cnsStr->gtScpHnd, cnsStr->gtSconCPX, &cnsLength);
        if ((cnsLength < 0) || (str == nullptr))
        {
            // We were unable to get the literal (e.g. dynamic context)
            return nullptr;
        }
        JITDUMP("Trying to unroll String.Equals|StartsWith(op1, \"%ws\")...\n", str)
    }

    // Create a temp which is safe to gtClone for varStr
    // We're not appending it as a statement until we figure out unrolling is profitable (and possible)
    unsigned varStrTmp         = lvaGrabTemp(true DEBUGARG("spilling varStr"));
    lvaTable[varStrTmp].lvType = varStr->TypeGet();
    GenTreeLclVar* varStrLcl   = gtNewLclvNode(varStrTmp, varStr->TypeGet());

    // Create a tree representing string's Length:
    // TODO-Unroll-CQ: Consider using ARR_LENGTH here, but we'll have to modify QMARK to propagate BBF_HAS_IDX_LEN
    int      strLenOffset = OFFSETOF__CORINFO_String__stringLen;
    GenTree* lenOffset    = gtNewIconNode(strLenOffset, TYP_I_IMPL);
    GenTree* lenNode      = gtNewIndir(TYP_INT, gtNewOperNode(GT_ADD, TYP_BYREF, varStrLcl, lenOffset));
    varStrLcl             = gtClone(varStrLcl)->AsLclVar();

    GenTree* unrolled = impExpandHalfConstEquals(varStrLcl, lenNode, needsNullcheck, startsWith, (WCHAR*)str, cnsLength,
                                                 strLenOffset + sizeof(int));
    if (unrolled != nullptr)
    {
        impAssignTempGen(varStrTmp, varStr);
        if (unrolled->OperIs(GT_QMARK))
        {
            // QMARK nodes cannot reside on the evaluation stack
            unsigned rootTmp = lvaGrabTemp(true DEBUGARG("spilling unroll qmark"));
            impAssignTempGen(rootTmp, unrolled);
            unrolled = gtNewLclvNode(rootTmp, TYP_INT);
        }

        JITDUMP("\n... Successfully unrolled to:\n")
        DISPTREE(unrolled)
        for (int i = 0; i < argsCount; i++)
        {
            impPopStack();
        }
    }
    return unrolled;
}

//------------------------------------------------------------------------
// impSpanEqualsOrStartsWith: The main entry-point for [ReadOnly]Span<char> methods
//    We're going to unroll & vectorize the following cases:
//    1) MemoryExtensions.SequenceEqual<char>(var, "cns")
//    2) MemoryExtensions.SequenceEqual<char>("cns", var)
//    3) MemoryExtensions.Equals(var, "cns", StringComparison.Ordinal)
//    4) MemoryExtensions.Equals("cns", var, StringComparison.Ordinal)
//    5) MemoryExtensions.StartsWith<char>("cns", var)
//    6) MemoryExtensions.StartsWith<char>(var, "cns")
//    7) MemoryExtensions.StartsWith("cns", var, StringComparison.Ordinal)
//    8) MemoryExtensions.StartsWith(var, "cns", StringComparison.Ordinal)
//
// Arguments:
//    startsWith  - Is it StartsWith or Equals?
//    sig         - signature of StartsWith or Equals method
//    methodFlags - its flags
//
// Returns:
//    GenTree representing vectorized comparison or nullptr
//
GenTree* Compiler::impSpanEqualsOrStartsWith(bool startsWith, CORINFO_SIG_INFO* sig, unsigned methodFlags)
{
    const bool isStatic  = methodFlags & CORINFO_FLG_STATIC;
    const int  argsCount = sig->numArgs + (isStatic ? 0 : 1);

    GenTree* op1;
    GenTree* op2;
    if (argsCount == 3) // overload with StringComparison
    {
        if (!impStackTop(0).val->IsIntegralConst(4)) // StringComparison.Ordinal
        {
            // TODO-Unroll-CQ: Unroll & vectorize OrdinalIgnoreCase
            return nullptr;
        }
        op1 = impStackTop(2).val;
        op2 = impStackTop(1).val;
    }
    else
    {
        assert(argsCount == 2);
        op1 = impStackTop(1).val;
        op2 = impStackTop(0).val;
    }

    // For generic StartsWith and Equals we need to make sure T is char
    if (sig->sigInst.methInstCount != 0)
    {
        assert(sig->sigInst.methInstCount == 1);
        CORINFO_CLASS_HANDLE targetElemHnd = sig->sigInst.methInst[0];
        CorInfoType          typ           = info.compCompHnd->getTypeForPrimitiveValueClass(targetElemHnd);
        if ((typ != CORINFO_TYPE_SHORT) && (typ != CORINFO_TYPE_USHORT) && (typ != CORINFO_TYPE_CHAR))
        {
            return nullptr;
        }
    }

    // Try to obtain original string literals out of span arguments
    GenTreeStrCon* op1Str = impGetStrConFromSpan(op1);
    GenTreeStrCon* op2Str = impGetStrConFromSpan(op2);

    if (!((op1Str != nullptr) ^ (op2Str != nullptr)))
    {
        // either op1 or op2 has to be '(ReadOnlySpan)"cns"'
        return nullptr;
    }

    GenTree*       spanObj;
    GenTreeStrCon* cnsStr;
    if (op1Str != nullptr)
    {
        cnsStr  = op1Str;
        spanObj = op2;
    }
    else
    {
        cnsStr  = op2Str;
        spanObj = op1;
    }

    int             cnsLength = -1;
    const char16_t* str       = nullptr;
    if (cnsStr->IsStringEmptyField())
    {
        // check for fake "" first
        cnsLength = 0;
        JITDUMP("Trying to unroll MemoryExtensions.Equals|SequenceEqual|StartsWith(op1, \"\")...\n", str)
    }
    else
    {
        str = info.compCompHnd->getStringLiteral(cnsStr->gtScpHnd, cnsStr->gtSconCPX, &cnsLength);
        if (cnsLength < 0 || str == nullptr)
        {
            // We were unable to get the literal (e.g. dynamic context)
            return nullptr;
        }
        JITDUMP("Trying to unroll MemoryExtensions.Equals|SequenceEqual|StartsWith(op1, \"%ws\")...\n", str)
    }

    CORINFO_CLASS_HANDLE spanCls      = gtGetStructHandle(spanObj);
    CORINFO_FIELD_HANDLE pointerHnd   = info.compCompHnd->getFieldInClass(spanCls, 0);
    CORINFO_FIELD_HANDLE lengthHnd    = info.compCompHnd->getFieldInClass(spanCls, 1);
    const unsigned       lengthOffset = info.compCompHnd->getFieldOffset(lengthHnd);

    // Create a placeholder for Span object - we're not going to Append it to statements
    // in advance to avoid redundant spills in case if we fail to vectorize
    unsigned spanObjRef          = lvaGrabTemp(true DEBUGARG("spanObj tmp"));
    unsigned spanDataTmp         = lvaGrabTemp(true DEBUGARG("spanData tmp"));
    lvaTable[spanObjRef].lvType  = TYP_BYREF;
    lvaTable[spanDataTmp].lvType = TYP_BYREF;

    GenTreeLclVar* spanObjRefLcl  = gtNewLclvNode(spanObjRef, TYP_BYREF);
    GenTreeLclVar* spanDataTmpLcl = gtNewLclvNode(spanDataTmp, TYP_BYREF);

    GenTreeField* spanLength = gtNewFieldRef(TYP_INT, lengthHnd, gtClone(spanObjRefLcl), lengthOffset);
    GenTreeField* spanData   = gtNewFieldRef(TYP_BYREF, pointerHnd, spanObjRefLcl);

    GenTree* unrolled =
        impExpandHalfConstEquals(spanDataTmpLcl, spanLength, false, startsWith, (WCHAR*)str, cnsLength, 0);
    if (unrolled != nullptr)
    {
        // We succeeded, fill the placeholders:
        impAssignTempGen(spanObjRef, impGetStructAddr(spanObj, spanCls, (unsigned)CHECK_SPILL_NONE, true));
        impAssignTempGen(spanDataTmp, spanData);
        if (unrolled->OperIs(GT_QMARK))
        {
            // QMARK can't be a root node, spill it to a temp
            unsigned rootTmp = lvaGrabTemp(true DEBUGARG("spilling unroll qmark"));
            impAssignTempGen(rootTmp, unrolled);
            unrolled = gtNewLclvNode(rootTmp, TYP_INT);
        }

        JITDUMP("... Successfully unrolled to:\n")
        DISPTREE(unrolled)

        for (int i = 0; i < argsCount; i++)
        {
            impPopStack();
        }

        // We have to clean up GT_RET_EXPR for String.op_Implicit or MemoryExtensions.AsSpans
        if ((spanObj != op1) && op1->OperIs(GT_RET_EXPR))
        {
            GenTree* inlineCandidate = op1->AsRetExpr()->gtInlineCandidate;
            assert(inlineCandidate->IsCall());
            inlineCandidate->gtBashToNOP();
        }
        else if ((spanObj != op2) && op2->OperIs(GT_RET_EXPR))
        {
            GenTree* inlineCandidate = op2->AsRetExpr()->gtInlineCandidate;
            assert(inlineCandidate->IsCall());
            inlineCandidate->gtBashToNOP();
        }
    }
    return unrolled;
}
