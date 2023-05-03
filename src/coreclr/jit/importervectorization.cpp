// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "jitpch.h"
#ifdef _MSC_VER
#pragma hdrstop
#endif

// For now the max possible size is Vector512<ushort>.Count * 2
#define MaxPossibleUnrollSize 64

//------------------------------------------------------------------------
// importer_vectorization.cpp
//
// This file is responsible for various (partial) vectorizations during import phase,
// e.g. the following APIs are currently supported:
//
//   1) String.Equals(string, string)
//   2) String.Equals(string, string, Ordinal or OrdinalIgnoreCase)
//   3) str.Equals(string)
//   4) str.Equals(String, Ordinal or OrdinalIgnoreCase)
//   5) str.StartsWith(string, Ordinal or OrdinalIgnoreCase)
//   6) MemoryExtensions.SequenceEqual<char>(ROS<char>, ROS<char>)
//   7) MemoryExtensions.Equals(ROS<char>, ROS<char>, Ordinal or OrdinalIgnoreCase)
//   8) MemoryExtensions.StartsWith<char>(ROS<char>, ROS<char>)
//   9) MemoryExtensions.StartsWith(ROS<char>, ROS<char>, Ordinal or OrdinalIgnoreCase)
//
// When one of the arguments is a constant string of a [0..32] size so we can inline
// a vectorized comparison against it using SWAR or SIMD techniques (e.g. via two V256 vectors)
//

//------------------------------------------------------------------------
// ConvertToLowerCase: Converts input ASCII data to lower case
//
// Arguments:
//    input  - Constant data to change casing to lower
//    mask   - Mask to apply to non-constant data, e.g.:
//       input: [  h ][  i ][  4 ][  - ][  A ]
//       mask:  [0x20][0x20][ 0x0][ 0x0][0x20]
//    length - Length of input
//
// Return Value:
//    false if input contains non-ASCII chars
//
static bool ConvertToLowerCase(WCHAR* input, WCHAR* mask, int length)
{
    for (int i = 0; i < length; i++)
    {
        auto ch = (USHORT)input[i];
        if (ch > 127)
        {
            JITDUMP("Constant data contains non-ASCII char(s), give up.\n");
            return false;
        }

        // Inside [0..127] range only [a-z] and [A-Z] sub-ranges are
        // eligible for case changing, we can't apply 0x20 bit for e.g. '-'
        if (((ch >= 'A') && (ch <= 'Z')) || ((ch >= 'a') && (ch <= 'z')))
        {
            input[i] |= 0x20;
            mask[i] = 0x20;
        }
        else
        {
            mask[i] = 0;
        }
    }
    return true;
}

#if defined(FEATURE_HW_INTRINSICS)
//------------------------------------------------------------------------
// impExpandHalfConstEqualsSIMD: Attempts to unroll and vectorize
//    Equals against a constant WCHAR data for Length in [8..64] range
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
//    data       - Pointer to a data to vectorize
//    cns        - Constant data (array of 2-byte chars)
//    len        - Number of chars in the cns
//    dataOffset - Offset for data
//    cmpMode    - Ordinal or OrdinalIgnoreCase mode (works only for ASCII cns)
//
// Return Value:
//    A pointer to the newly created SIMD node or nullptr if unrolling is not
//    possible, not profitable or constant data contains non-ASCII char(s) in 'ignoreCase' mode
//
// Notes:
//    This function doesn't check obj for null or its Length, it's just an internal helper
//    for impExpandHalfConstEquals
//
GenTree* Compiler::impExpandHalfConstEqualsSIMD(
    GenTreeLclVar* data, WCHAR* cns, int len, int dataOffset, StringComparison cmpMode)
{
    assert(len >= 8 && len <= MaxPossibleUnrollSize);

    const int byteLen  = len * sizeof(WCHAR);
    int       simdSize = (int)roundDownSIMDSize(byteLen);
#ifdef TARGET_XARCH
    if ((simdSize == YMM_REGSIZE_BYTES) && !compOpportunisticallyDependsOn(InstructionSet_AVX2))
    {
        // We need AVX2 for NI_Vector256_op_Equality, fallback to Vector128 if only AVX is available
        simdSize = XMM_REGSIZE_BYTES;
    }
#endif
    if (byteLen > (simdSize * 2))
    {
        // Data is too big to be processed via two SIMD loads
        // or baseline has no SIMD support
        return nullptr;
    }
    assert((byteLen >= simdSize) && (simdSize >= 16));

    WCHAR cnsValue[MaxPossibleUnrollSize]    = {};
    WCHAR toLowerMask[MaxPossibleUnrollSize] = {};

    memcpy(cnsValue, cns, byteLen);

    if ((cmpMode == OrdinalIgnoreCase) && !ConvertToLowerCase(cnsValue, toLowerMask, len))
    {
        // value contains non-ASCII chars, we can't proceed further
        return nullptr;
    }

    const var_types   simdType = getSIMDTypeForSize(simdSize);
    const CorInfoType baseType = CORINFO_TYPE_NATIVEUINT;

    GenTreeVecCon* cnsVec1 = gtNewVconNode(simdType, cnsValue);
    GenTreeVecCon* cnsVec2 = gtNewVconNode(simdType, (BYTE*)cnsValue + byteLen - simdSize);

    GenTree* offset1 = gtNewIconNode(dataOffset, TYP_I_IMPL);
    GenTree* offset2 = gtNewIconNode(dataOffset + byteLen - simdSize, TYP_I_IMPL);
    GenTree* vec1    = gtNewIndir(simdType, gtNewOperNode(GT_ADD, TYP_BYREF, data, offset1));
    GenTree* vec2    = gtNewIndir(simdType, gtNewOperNode(GT_ADD, TYP_BYREF, gtClone(data), offset2));

    if (cmpMode == OrdinalIgnoreCase)
    {
        // Apply ASCII-only ToLowerCase mask (bitwise OR 0x20 for all a-Z chars)
        GenTreeVecCon* toLowerVec1 = gtNewVconNode(simdType, toLowerMask);
        GenTreeVecCon* toLowerVec2 = gtNewVconNode(simdType, (BYTE*)toLowerMask + byteLen - simdSize);

        vec1 = gtNewSimdBinOpNode(GT_OR, simdType, vec1, toLowerVec1, baseType, simdSize);
        vec2 = gtNewSimdBinOpNode(GT_OR, simdType, vec2, toLowerVec2, baseType, simdSize);
    }

    // ((v1 ^ cns1) | (v2 ^ cns2)) == zero
    GenTree* xor1 = gtNewSimdBinOpNode(GT_XOR, simdType, vec1, cnsVec1, baseType, simdSize);
    GenTree* xor2 = gtNewSimdBinOpNode(GT_XOR, simdType, vec2, cnsVec2, baseType, simdSize);
    GenTree* orr  = gtNewSimdBinOpNode(GT_OR, simdType, xor1, xor2, baseType, simdSize);

    // Optimization: use a single load when byteLen equals simdSize.
    // For code simplicity we always create nodes for two vectors case.
    const bool useSingleVector = simdSize == byteLen;
    return gtNewSimdCmpOpAllNode(GT_EQ, TYP_BOOL, useSingleVector ? xor1 : orr, gtNewZeroConNode(simdType), baseType,
                                 simdSize);

    // Codegen example for byteLen=40 and OrdinalIgnoreCase mode with AVX:
    //
    //  vmovups  ymm0, ymmword ptr [rcx+0CH]
    //  vpor     ymm0, ymm0, ymmword ptr [reloc @RWD00]
    //  vpxor    ymm0, ymm0, ymmword ptr [reloc @RWD32]
    //  vmovups  ymm1, ymmword ptr [rcx+28H]
    //  vpor     ymm1, ymm1, ymmword ptr [reloc @RWD64]
    //  vpxor    ymm1, ymm1, ymmword ptr [reloc @RWD96]
    //  vpor     ymm0, ymm0, ymm1
    //  vptest   ymm0, ymm0
    //  sete     al
    //  movzx    rax, al
}
#endif // defined(FEATURE_HW_INTRINSICS)

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
//  or in case of 'ignoreCase':
//
//  *  EQ        int
//  +--*  OR        int
//  |  +--*  IND       <type>
//  |  |  \--*  ADD       byref
//  |  |     +--*  <obj>
//  |  |     \--*  CNS_INT   <offset>
//  |  \--*  CNS_INT   <lowercase mask>
//  \--*  CNS_INT   <lowercased value>
//
// Arguments:
//    comp       - Compiler object
//    obj        - GenTree representing data pointer
//    type       - Type for the IND node
//    offset     - Offset for the data pointer
//    value      - Constant value to compare against
//    cmpMode    - Ordinal or OrdinalIgnoreCase mode (works only for ASCII cns)
//    joint      - Type of joint, can be Eq ((d1 == cns1) && (s2 == cns2))
//                 or Xor (d1 ^ cns1) | (s2 ^ cns2).
//
// Return Value:
//    A tree with indirect load and comparison
//    nullptr in case of 'ignoreCase' mode and non-ASCII value
//
GenTree* Compiler::impCreateCompareInd(GenTreeLclVar*        obj,
                                       var_types             type,
                                       ssize_t               offset,
                                       ssize_t               value,
                                       StringComparison      cmpMode,
                                       StringComparisonJoint joint)
{
    var_types actualType    = genActualType(type);
    GenTree*  offsetTree    = gtNewIconNode(offset, TYP_I_IMPL);
    GenTree*  addOffsetTree = gtNewOperNode(GT_ADD, TYP_BYREF, obj, offsetTree);
    GenTree*  indirTree     = gtNewIndir(type, addOffsetTree);

    if (cmpMode == OrdinalIgnoreCase)
    {
        ssize_t mask;
        if (!ConvertToLowerCase((WCHAR*)&value, (WCHAR*)&mask, sizeof(ssize_t) / sizeof(WCHAR)))
        {
            // value contains non-ASCII chars, we can't proceed further
            return nullptr;
        }
        GenTree* toLowerMask = gtNewIconNode(mask, actualType);
        indirTree            = gtNewOperNode(GT_OR, actualType, indirTree, toLowerMask);
    }

    GenTree* valueTree = gtNewIconNode(value, actualType);
    if (joint == Xor)
    {
        // XOR is better than CMP if we want to join multiple comparisons
        return gtNewOperNode(GT_XOR, actualType, indirTree, valueTree);
    }
    assert(joint == Eq);
    return gtNewOperNode(GT_EQ, TYP_INT, indirTree, valueTree);
}

//------------------------------------------------------------------------
// impExpandHalfConstEqualsSWAR: Attempts to unroll and vectorize
//    Equals against a constant WCHAR data for Length in [1..8] range
//    using SWAR (a sort of SIMD but for GPR registers and instructions)
//
// Arguments:
//    data       - Pointer to a data to vectorize
//    cns        - Constant data (array of 2-byte chars)
//    len        - Number of chars in the cns
//    dataOffset - Offset for data
//    cmpMode    - Ordinal or OrdinalIgnoreCase mode (works only for ASCII cns)
//
// Return Value:
//    A pointer to the newly created SWAR node or nullptr if unrolling is not
//    possible, not profitable or constant data contains non-ASCII char(s) in 'ignoreCase' mode
//
// Notes:
//    This function doesn't check obj for null or its Length, it's just an internal helper
//    for impExpandHalfConstEquals
//
GenTree* Compiler::impExpandHalfConstEqualsSWAR(
    GenTreeLclVar* data, WCHAR* cns, int len, int dataOffset, StringComparison cmpMode)
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
        return impCreateCompareInd(data, TYP_USHORT, dataOffset, cns[0], cmpMode);
    }
    if (len == 2)
    {
        //   [ ch1 ][ ch2 ]
        //   [   value    ]
        //
        const UINT32 value = MAKEINT32(cns[0], cns[1]);
        return impCreateCompareInd(data, TYP_INT, dataOffset, value, cmpMode);
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
        GenTree* firstIndir = impCreateCompareInd(data, TYP_INT, dataOffset, value1, cmpMode, Xor);
        GenTree* secondIndir =
            impCreateCompareInd(gtClone(data)->AsLclVar(), TYP_INT, dataOffset + sizeof(USHORT), value2, cmpMode, Xor);

        if ((firstIndir == nullptr) || (secondIndir == nullptr))
        {
            return nullptr;
        }

        return gtNewOperNode(GT_EQ, TYP_INT, gtNewOperNode(GT_OR, TYP_INT, firstIndir, secondIndir), gtNewIconNode(0));
    }

    assert(len >= 4 && len <= 8);

    UINT64 value1 = MAKEINT64(cns[0], cns[1], cns[2], cns[3]);
    if (len == 4)
    {
        //   [ ch1 ][ ch2 ][ ch3 ][ ch4 ]
        //   [          value           ]
        //
        return impCreateCompareInd(data, TYP_LONG, dataOffset, value1, cmpMode);
    }

    // For 5..7 value2 will overlap with value1, e.g. for Length == 6:
    //
    //   [ ch1 ][ ch2 ][ ch3 ][ ch4 ][ ch5 ][ ch6 ]
    //   [          value1          ]
    //                 [          value2          ]
    //

    // For 5..6 the overlapping part is 4 bytes
    if (len <= 6)
    {
        UINT32   value2     = MAKEINT32(cns[len - 2], cns[len - 1]);
        GenTree* firstIndir = impCreateCompareInd(data, TYP_LONG, dataOffset, value1, cmpMode, Xor);

        ssize_t  offset      = dataOffset + len * sizeof(WCHAR) - sizeof(UINT32);
        GenTree* secondIndir = impCreateCompareInd(gtClone(data)->AsLclVar(), TYP_INT, offset, value2, cmpMode, Xor);

        if ((firstIndir == nullptr) || (secondIndir == nullptr))
        {
            return nullptr;
        }

        secondIndir = gtNewCastNode(TYP_LONG, secondIndir, true, TYP_LONG);
        return gtNewOperNode(GT_EQ, TYP_INT, gtNewOperNode(GT_OR, TYP_LONG, firstIndir, secondIndir),
                             gtNewIconNode(0, TYP_LONG));
    }

    // For 7..8 the overlapping part is 8 bytes
    assert((len == 7) || (len == 8));

    UINT64   value2     = MAKEINT64(cns[len - 4], cns[len - 3], cns[len - 2], cns[len - 1]);
    GenTree* firstIndir = impCreateCompareInd(data, TYP_LONG, dataOffset, value1, cmpMode, Xor);

    ssize_t  offset      = dataOffset + len * sizeof(WCHAR) - sizeof(UINT64);
    GenTree* secondIndir = impCreateCompareInd(gtClone(data)->AsLclVar(), TYP_LONG, offset, value2, cmpMode, Xor);

    if ((firstIndir == nullptr) || (secondIndir == nullptr))
    {
        return nullptr;
    }

    return gtNewOperNode(GT_EQ, TYP_INT, gtNewOperNode(GT_OR, TYP_LONG, firstIndir, secondIndir),
                         gtNewIconNode(0, TYP_LONG));
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
//    lengthFld    - Pointer (LCL_VAR or GT_IND) to Length field
//    checkForNull - Check data for null
//    startsWith   - Is it StartsWith or Equals?
//    cns          - Constant data (array of 2-byte chars)
//    len          - Number of 2-byte chars in the cns
//    dataOffset   - Offset for data
//    cmpMode      - Ordinal or OrdinalIgnoreCase mode (works only for ASCII cns)
//
// Return Value:
//    A pointer to the newly created SWAR/SIMD node or nullptr if unrolling is not
//    possible, not profitable or constant data contains non-ASCII char(s) in 'ignoreCase' mode
//
GenTree* Compiler::impExpandHalfConstEquals(GenTreeLclVar*   data,
                                            GenTree*         lengthFld,
                                            bool             checkForNull,
                                            bool             startsWith,
                                            WCHAR*           cnsData,
                                            int              len,
                                            int              dataOffset,
                                            StringComparison cmpMode)
{
    assert(len >= 0);

    if (compCurBB->isRunRarely())
    {
        // Not profitable to expand
        JITDUMP("impExpandHalfConstEquals: block is cold - not profitable to expand.\n");
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
            indirCmp = impExpandHalfConstEqualsSWAR(gtClone(data)->AsLclVar(), cnsData, len, dataOffset, cmpMode);
        }
#if defined(FEATURE_HW_INTRINSICS)
        else if (IsBaselineSimdIsaSupported())
        {
            indirCmp = impExpandHalfConstEqualsSIMD(gtClone(data)->AsLclVar(), cnsData, len, dataOffset, cmpMode);
        }
#endif

        if (indirCmp == nullptr)
        {
            JITDUMP("unable to compose indirCmp\n");
            return nullptr;
        }
        assert(indirCmp->TypeIs(TYP_INT, TYP_BOOL));

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
            assert(argCall->gtArgs.CountArgs() == 1);
            GenTree* arg = argCall->gtArgs.GetArgByIndex(0)->GetNode();
            if (arg->OperIs(GT_CNS_STR))
            {
                return arg->AsStrCon();
            }
        }
    }
    return nullptr;
}

//------------------------------------------------------------------------
// impStringEqualsOrStartsWith: The main entry-point for String methods
//   We're going to unroll & vectorize the following cases:
//    1) String.Equals(obj, "cns")
//    2) String.Equals(obj, "cns", Ordinal or OrdinalIgnoreCase)
//    3) String.Equals("cns", obj)
//    4) String.Equals("cns", obj, Ordinal or OrdinalIgnoreCase)
//    5) obj.Equals("cns")
//    5) obj.Equals("cns")
//    6) obj.Equals("cns", Ordinal or OrdinalIgnoreCase)
//    7) "cns".Equals(obj)
//    8) "cns".Equals(obj, Ordinal or OrdinalIgnoreCase)
//    9) obj.StartsWith("cns", Ordinal or OrdinalIgnoreCase)
//   10) "cns".StartsWith(obj, Ordinal or OrdinalIgnoreCase)
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

    StringComparison cmpMode = Ordinal;
    GenTree*         op1;
    GenTree*         op2;
    if (argsCount == 3) // overload with StringComparison
    {
        if (impStackTop(0).val->IsIntegralConst(OrdinalIgnoreCase))
        {
            cmpMode = OrdinalIgnoreCase;
        }
        else if (!impStackTop(0).val->IsIntegralConst(Ordinal))
        {
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
    if (op2->OperIs(GT_CNS_STR))
    {
        cnsStr = op2->AsStrCon();
        varStr = op1;
    }
    else
    {
        if (startsWith)
        {
            // StartsWith is not commutative
            return nullptr;
        }
        cnsStr = op1->AsStrCon();
        varStr = op2;
    }

    bool needsNullcheck = true;
    if ((op1 != cnsStr) && !isStatic)
    {
        // for the following cases we should not check varStr for null:
        //
        //  obj.Equals("cns")
        //  obj.Equals("cns", Ordinal or OrdinalIgnoreCase)
        //  obj.StartsWith("cns", Ordinal or OrdinalIgnoreCase)
        //
        // instead, it should throw NRE if it's null
        needsNullcheck = false;
    }

    int      cnsLength;
    char16_t str[MaxPossibleUnrollSize];
    if (cnsStr->IsStringEmptyField())
    {
        // check for fake "" first
        cnsLength = 0;
        JITDUMP("Trying to unroll String.Equals|StartsWith(op1, \"\")...\n", str)
    }
    else
    {
        cnsLength = info.compCompHnd->getStringLiteral(cnsStr->gtScpHnd, cnsStr->gtSconCPX, str, MaxPossibleUnrollSize);
        if ((cnsLength < 0) || (cnsLength > MaxPossibleUnrollSize))
        {
            // We were unable to get the literal (e.g. dynamic context)
            return nullptr;
        }
        JITDUMP("Trying to unroll String.Equals|StartsWith(op1, \"cns\")...\n")
    }

    // Create a temp which is safe to gtClone for varStr
    // We're not appending it as a statement until we figure out unrolling is profitable (and possible)
    unsigned varStrTmp         = lvaGrabTemp(true DEBUGARG("spilling varStr"));
    lvaTable[varStrTmp].lvType = varStr->TypeGet();
    GenTreeLclVar* varStrLcl   = gtNewLclvNode(varStrTmp, varStr->TypeGet());

    // Create a tree representing string's Length:
    int      strLenOffset = OFFSETOF__CORINFO_String__stringLen;
    GenTree* lenNode      = gtNewArrLen(TYP_INT, varStrLcl, strLenOffset, compCurBB);
    varStrLcl             = gtClone(varStrLcl)->AsLclVar();

    GenTree* unrolled = impExpandHalfConstEquals(varStrLcl, lenNode, needsNullcheck, startsWith, (WCHAR*)str, cnsLength,
                                                 strLenOffset + sizeof(int), cmpMode);
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
//    3) MemoryExtensions.Equals(var, "cns", Ordinal or OrdinalIgnoreCase)
//    4) MemoryExtensions.Equals("cns", var, Ordinal or OrdinalIgnoreCase)
//    5) MemoryExtensions.StartsWith<char>("cns", var)
//    6) MemoryExtensions.StartsWith<char>(var, "cns")
//    7) MemoryExtensions.StartsWith("cns", var, Ordinal or OrdinalIgnoreCase)
//    8) MemoryExtensions.StartsWith(var, "cns", Ordinal or OrdinalIgnoreCase)
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

    StringComparison cmpMode = Ordinal;
    GenTree*         op1;
    GenTree*         op2;
    if (argsCount == 3) // overload with StringComparison
    {
        if (impStackTop(0).val->IsIntegralConst(OrdinalIgnoreCase))
        {
            cmpMode = OrdinalIgnoreCase;
        }
        else if (!impStackTop(0).val->IsIntegralConst(Ordinal))
        {
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
    if (op2Str != nullptr)
    {
        cnsStr  = op2Str;
        spanObj = op1;
    }
    else
    {
        if (startsWith)
        {
            // StartsWith is not commutative
            return nullptr;
        }
        cnsStr  = op1Str;
        spanObj = op2;
    }

    int      cnsLength = -1;
    char16_t str[MaxPossibleUnrollSize];
    if (cnsStr->IsStringEmptyField())
    {
        // check for fake "" first
        cnsLength = 0;
        JITDUMP("Trying to unroll MemoryExtensions.Equals|SequenceEqual|StartsWith(op1, \"\")...\n")
    }
    else
    {
        cnsLength = info.compCompHnd->getStringLiteral(cnsStr->gtScpHnd, cnsStr->gtSconCPX, str, MaxPossibleUnrollSize);
        if ((cnsLength < 0) || (cnsLength > MaxPossibleUnrollSize))
        {
            // We were unable to get the literal (e.g. dynamic context)
            return nullptr;
        }
        JITDUMP("Trying to unroll MemoryExtensions.Equals|SequenceEqual|StartsWith(op1, \"%ws\")...\n", str)
    }

    CORINFO_CLASS_HANDLE spanCls;
    info.compCompHnd->getArgType(sig, sig->args, &spanCls);
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

    GenTreeFieldAddr* spanLengthAddr = gtNewFieldAddrNode(TYP_BYREF, lengthHnd, gtClone(spanObjRefLcl), lengthOffset);
    GenTree*          spanLength     = gtNewFieldIndirNode(TYP_INT, nullptr, spanLengthAddr);
    GenTreeFieldAddr* spanDataAddr   = gtNewFieldAddrNode(TYP_BYREF, pointerHnd, spanObjRefLcl);
    GenTree*          spanData       = gtNewFieldIndirNode(TYP_BYREF, nullptr, spanDataAddr);

    GenTree* unrolled =
        impExpandHalfConstEquals(spanDataTmpLcl, spanLength, false, startsWith, (WCHAR*)str, cnsLength, 0, cmpMode);
    if (unrolled != nullptr)
    {
        // We succeeded, fill the placeholders:
        impAssignTempGen(spanObjRef, impGetStructAddr(spanObj, spanCls, CHECK_SPILL_NONE, true));
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
