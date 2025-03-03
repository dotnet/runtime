// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "jitpch.h"
#ifdef _MSC_VER
#pragma hdrstop
#endif

// Overestimated threshold to avoid memory allocations,
#define MaxPossibleUnrollSize 128

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
//   10) str.EndsWith(string, Ordinal or OrdinalIgnoreCase)
//   11) MemoryExtensions.EndsWith<char>(ROS<char>, ROS<char>)
//   12) MemoryExtensions.EndsWith(ROS<char>, ROS<char>, Ordinal or OrdinalIgnoreCase)
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

//------------------------------------------------------------------------
// impExpandHalfConstEquals: Attempts to unroll and vectorize
//    Equals against a constant WCHAR data
//
// Arguments:
//    data       - Pointer to a data to vectorize
//    cns        - Constant data (array of 2-byte chars)
//    charLen    - Number of chars in the cns
//    dataOffset - Offset for data
//    cmpMode    - Ordinal or OrdinalIgnoreCase mode (works only for ASCII cns)
//
// Return Value:
//    A tree representing unrolled comparison or nullptr if unrolling is not possible
//    (possible only if cns contains non-ASCII char(s) in OrdinalIgnoreCase mode)
//
GenTree* Compiler::impExpandHalfConstEquals(
    GenTreeLclVarCommon* data, WCHAR* cns, int charLen, int dataOffset, StringComparison cmpMode)
{
    static_assert_no_msg(sizeof(WCHAR) == 2);
    assert((charLen > 0) && (charLen <= MaxPossibleUnrollSize));

    // A gtNewOperNode which can handle SIMD operands (used for bitwise operations):
    auto bitwiseOp = [&](genTreeOps oper, var_types type, GenTree* op1, GenTree* op2) -> GenTree* {
#ifdef FEATURE_HW_INTRINSICS
        if (varTypeIsSIMD(type))
        {
            return gtNewSimdBinOpNode(oper, type, op1, op2, CORINFO_TYPE_NATIVEUINT, genTypeSize(type));
        }
        if (varTypeIsSIMD(op1))
        {
            // E.g. a comparison of SIMD ops returning TYP_INT;
            assert(varTypeIsSIMD(op2));
            return gtNewSimdCmpOpAllNode(oper, type, op1, op2, CORINFO_TYPE_NATIVEUINT, genTypeSize(op1));
        }
#endif
        return gtNewOperNode(oper, type, op1, op2);
    };

    // Convert charLen to byteLen. It never overflows because charLen is a small value
    unsigned byteLen = (unsigned)charLen * 2;

    // Find the largest possible type to read data
    var_types readType         = roundDownMaxType(byteLen, true);
    GenTree*  result           = nullptr;
    unsigned  byteLenRemaining = byteLen;
    while (byteLenRemaining > 0)
    {
        // We have a remaining data to process and it's smaller than the
        // previously processed data
        if (byteLenRemaining < genTypeSize(readType))
        {
            if (varTypeIsIntegral(readType))
            {
                // Use a smaller GPR load for the remaining data, we're going to zero-extend it
                // since the previous GPR load was larger. Hence, for e.g. 6 bytes we're going to do
                // "(IND<INT> ^ cns1) | (UINT)(IND<USHORT> ^ cns2)"
                readType = roundUpGPRType(byteLenRemaining);
            }
            else
            {
                // TODO-CQ: We should probably do the same for SIMD, e.g. 34 bytes -> SIMD32 and SIMD16
                // while currently we do SIMD32 and SIMD32. This involves a bit more complex upcasting logic.
            }

            // Overlap with the previously processed data
            byteLenRemaining = genTypeSize(readType);
            assert(byteLenRemaining <= byteLen);
        }

        ssize_t byteOffset = ((ssize_t)byteLen - (ssize_t)byteLenRemaining);

        // Total offset includes dataOffset (e.g. 12 for String)
        ssize_t totalOffset = byteOffset + (ssize_t)dataOffset;

        // Clone dst and add offset if necessary.
        GenTree* absOffset  = gtNewIconNode(totalOffset, TYP_I_IMPL);
        GenTree* currData   = gtNewOperNode(GT_ADD, TYP_BYREF, gtCloneExpr(data), absOffset);
        GenTree* loadedData = gtNewIndir(readType, currData, GTF_IND_UNALIGNED | GTF_IND_ALLOW_NON_ATOMIC);

        // For OrdinalIgnoreCase mode we need to convert both data and cns to lower case
        if (cmpMode == OrdinalIgnoreCase)
        {
            WCHAR mask[MaxPossibleUnrollSize] = {};
            int   maskSize                    = (int)genTypeSize(readType) / 2;
            if (!ConvertToLowerCase(cns + (byteOffset / 2), reinterpret_cast<WCHAR*>(&mask), maskSize))
            {
                // value contains non-ASCII chars, we can't proceed further
                return nullptr;
            }

            // 0x20 mask for the current chunk to convert it to lower case
            GenTree* toLowerMask = gtNewGenericCon(readType, (uint8_t*)mask);

            // loadedData is now "loadedData | toLowerMask"
            loadedData = bitwiseOp(GT_OR, genActualType(readType), loadedData, toLowerMask);
        }
        else
        {
            assert(cmpMode == Ordinal);
        }

        GenTree* srcCns = gtNewGenericCon(readType, (uint8_t*)cns + byteOffset);

        // A small optimization: prefer X == Y over X ^ Y == 0 since
        // just one comparison is needed, and we can do it with a single load.
        if ((genTypeSize(readType) == byteLen) && varTypeIsIntegral(readType))
        {
            // TODO-CQ: Figure out why it's a size regression for SIMD
            return bitwiseOp(GT_EQ, TYP_INT, loadedData, srcCns);
        }

        // loadedData ^ srcCns
        GenTree* xorNode = bitwiseOp(GT_XOR, genActualType(readType), loadedData, srcCns);

        // Merge with the previous result with OR
        if (result == nullptr)
        {
            // It's the first check
            result = xorNode;
        }
        else
        {
            if (!result->TypeIs(readType))
            {
                assert(varTypeIsIntegral(result) && varTypeIsIntegral(readType));
                xorNode = gtNewCastNode(result->TypeGet(), xorNode, true, result->TypeGet());
            }

            // Merge with the previous result via OR
            result = bitwiseOp(GT_OR, genActualType(result->TypeGet()), result, xorNode);
        }

        // Move to the next chunk.
        byteLenRemaining -= genTypeSize(readType);
    }

    // Compare the result against zero, e.g. (chunk1 ^ cns1) | (chunk2 ^ cns2) == 0
    return bitwiseOp(GT_EQ, TYP_INT, result, gtNewZeroConNode(result->TypeGet()));
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
//    kind         - Is it StartsWith, Equals or EndsWith?
//    cns          - Constant data (array of 2-byte chars)
//    len          - Number of 2-byte chars in the cns
//    dataOffset   - Offset for data
//    cmpMode      - Ordinal or OrdinalIgnoreCase mode (works only for ASCII cns)
//
// Return Value:
//    A pointer to the newly created SWAR/SIMD node or nullptr if unrolling is not
//    possible, not profitable or constant data contains non-ASCII char(s) in 'ignoreCase' mode
//
GenTree* Compiler::impExpandHalfConstEquals(GenTreeLclVarCommon* data,
                                            GenTree*             lengthFld,
                                            bool                 checkForNull,
                                            StringComparisonKind kind,
                                            WCHAR*               cnsData,
                                            int                  len,
                                            int                  dataOffset,
                                            StringComparison     cmpMode)
{
    assert(len >= 0);

    if (compCurBB->isRunRarely())
    {
        // Not profitable to expand
        JITDUMP("impExpandHalfConstEquals: block is cold - not profitable to expand.\n");
        return nullptr;
    }

    const genTreeOps cmpOp         = kind == StringComparisonKind::Equals ? GT_EQ : GT_GE;
    GenTree*         elementsCount = gtNewIconNode(len);
    GenTree*         lenCheckNode;
    if (len == 0)
    {
        // For zero length we don't need to compare content, the following expression is enough:
        //
        //   varData != null && lengthFld cmpOp 0
        //
        lenCheckNode = gtNewOperNode(cmpOp, TYP_INT, lengthFld, elementsCount);
    }
    else
    {
        assert(cnsData != nullptr);

        GenTreeLclVarCommon* dataAddr = gtClone(data)->AsLclVarCommon();

        if (kind == StringComparisonKind::EndsWith)
        {
            // For EndsWith we need to adjust dataAddr to point to the end of the string minus value's length
            // We spawn a local that we're going to set below
            unsigned dataTmp         = lvaGrabTemp(true DEBUGARG("clonning data ptr"));
            lvaTable[dataTmp].lvType = TYP_BYREF;
            dataAddr                 = gtNewLclvNode(dataTmp, TYP_BYREF);
        }

        GenTree* indirCmp = impExpandHalfConstEquals(dataAddr, cnsData, len, dataOffset, cmpMode);
        if (indirCmp == nullptr)
        {
            JITDUMP("unable to compose indirCmp\n");
            return nullptr;
        }
        assert(indirCmp->TypeIs(TYP_INT, TYP_UBYTE));

        if (kind == StringComparisonKind::EndsWith)
        {
            // len is expected to be small, so no overflow is possible
            assert(!CheckedOps::MulOverflows(len, 2, CheckedOps::Signed));

            // dataAddr = dataAddr + (length * 2 - len * 2)
            GenTree*   castedLen = gtNewCastNode(TYP_I_IMPL, gtCloneExpr(lengthFld), false, TYP_I_IMPL);
            GenTree*   byteLen   = gtNewOperNode(GT_MUL, TYP_I_IMPL, castedLen, gtNewIconNode(2, TYP_I_IMPL));
            GenTreeOp* cmpStart  = gtNewOperNode(GT_ADD, TYP_BYREF, gtClone(data),
                                                 gtNewOperNode(GT_SUB, TYP_I_IMPL, byteLen,
                                                               gtNewIconNode((ssize_t)(len * 2), TYP_I_IMPL)));
            GenTree*   storeTmp  = gtNewTempStore(dataAddr->GetLclNum(), cmpStart);
            indirCmp             = gtNewOperNode(GT_COMMA, indirCmp->TypeGet(), storeTmp, indirCmp);
        }

        GenTreeColon* lenCheckColon = gtNewColonNode(TYP_INT, indirCmp, gtNewFalse());

        // For StartsWith/EndsWith we use GT_GE, e.g.: `x.Length >= 10`
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

    if ((argCall != nullptr) && argCall->IsSpecialIntrinsic())
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
// impUtf16StringComparison: The main entry-point for String methods
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
//   11) obj.EndsWith("cns", Ordinal or OrdinalIgnoreCase)
//   12) "cns".EndsWith(obj, Ordinal or OrdinalIgnoreCase)
//
//   For cases 5, 6 and 9 we don't emit "obj != null"
//   NOTE: String.Equals(object) is not supported currently
//
// Arguments:
//    kind        - Is it StartsWith, EndsWith or Equals?
//    sig         - signature of StartsWith, EndsWith or Equals method
//    methodFlags - its flags
//
// Returns:
//    GenTree representing vectorized comparison or nullptr
//
GenTree* Compiler::impUtf16StringComparison(StringComparisonKind kind, CORINFO_SIG_INFO* sig, unsigned methodFlags)
{
    const bool isStatic  = methodFlags & CORINFO_FLG_STATIC;
    const int  argsCount = sig->numArgs + (isStatic ? 0 : 1);

    // This optimization spawns several temps so make sure we have a room
    if (lvaHaveManyLocals(0.75))
    {
        JITDUMP("impUtf16StringComparison: Method has too many locals - bail out.\n")
        return nullptr;
    }

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

    if (!op1->OperIs(GT_CNS_STR) && !op2->OperIs(GT_CNS_STR))
    {
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
        if (kind != StringComparisonKind::Equals)
        {
            // StartsWith and EndsWith are not commutative
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
        //  obj.EndsWith("cns", Ordinal or OrdinalIgnoreCase)
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
        JITDUMP("Trying to unroll String.Equals|StartsWith|EndsWith(op1, \"\")...\n", str)
    }
    else
    {
        cnsLength = info.compCompHnd->getStringLiteral(cnsStr->gtScpHnd, cnsStr->gtSconCPX, str, MaxPossibleUnrollSize);
        if (cnsLength < 0)
        {
            // We were unable to get the literal (e.g. dynamic context)
            return nullptr;
        }
        if (cnsLength > ((int)getUnrollThreshold(MemcmpU16) / 2))
        {
            JITDUMP("UTF16 data is too long to unroll - bail out.\n");
            return nullptr;
        }
        JITDUMP("Trying to unroll String.Equals|StartsWith|EndsWith(op1, \"cns\")...\n")
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

    GenTree* unrolled = impExpandHalfConstEquals(varStrLcl, lenNode, needsNullcheck, kind, (WCHAR*)str, cnsLength,
                                                 strLenOffset + sizeof(int), cmpMode);
    if (unrolled != nullptr)
    {
        impStoreToTemp(varStrTmp, varStr, CHECK_SPILL_NONE);
        if (unrolled->OperIs(GT_QMARK))
        {
            // QMARK nodes cannot reside on the evaluation stack
            unsigned rootTmp = lvaGrabTemp(true DEBUGARG("spilling unroll qmark"));
            impStoreToTemp(rootTmp, unrolled, CHECK_SPILL_NONE);
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
// impUtf16SpanComparison: The main entry-point for [ReadOnly]Span<char> methods
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
//    9) MemoryExtensions.EndsWith<char>("cns", var)
//    10) MemoryExtensions.EndsWith<char>(var, "cns")
//    11) MemoryExtensions.EndsWith("cns", var, Ordinal or OrdinalIgnoreCase)
//    12) MemoryExtensions.EndsWith(var, "cns", Ordinal or OrdinalIgnoreCase)
//
// Arguments:
//    kind        - Is it StartsWith, EndsWith or Equals?
//    sig         - signature of StartsWith, EndsWith or Equals method
//    methodFlags - its flags
//
// Returns:
//    GenTree representing vectorized comparison or nullptr
//
GenTree* Compiler::impUtf16SpanComparison(StringComparisonKind kind, CORINFO_SIG_INFO* sig, unsigned methodFlags)
{
    const bool isStatic  = methodFlags & CORINFO_FLG_STATIC;
    const int  argsCount = sig->numArgs + (isStatic ? 0 : 1);

    // This optimization spawns several temps so make sure we have a room
    if (lvaHaveManyLocals(0.75))
    {
        JITDUMP("impUtf16SpanComparison: Method has too many locals - bail out.\n")
        return nullptr;
    }

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

    // For generic StartsWith, EndsWith and Equals we need to make sure T is char
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

    if ((op1Str == nullptr) && (op2Str == nullptr))
    {
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
        if (kind != StringComparisonKind::Equals)
        {
            // StartsWith and EndsWith are not commutative
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
        if (cnsLength < 0)
        {
            // We were unable to get the literal (e.g. dynamic context)
            return nullptr;
        }
        if (cnsLength > ((int)getUnrollThreshold(MemcmpU16) / 2))
        {
            JITDUMP("UTF16 data is too long to unroll - bail out.\n");
            return nullptr;
        }

        JITDUMP("Trying to unroll MemoryExtensions.Equals|SequenceEqual|StartsWith(op1, \"%s\")...\n",
                convertUtf16ToUtf8ForPrinting((WCHAR*)str));
    }

    unsigned spanLclNum;
    if (spanObj->OperIs(GT_LCL_VAR))
    {
        // Argument is already a local
        spanLclNum = spanObj->AsLclVarCommon()->GetLclNum();
    }
    else
    {
        // Access a local that will be set if we successfully unroll it
        spanLclNum = lvaGrabTemp(true DEBUGARG("spilling spanObj"));
        CORINFO_CLASS_HANDLE spanCls;
        info.compCompHnd->getArgType(sig, sig->args, &spanCls);
        lvaSetStruct(spanLclNum, spanCls, false);
    }

    GenTreeLclFld* spanReferenceFld = gtNewLclFldNode(spanLclNum, TYP_BYREF, OFFSETOF__CORINFO_Span__reference);
    GenTreeLclFld* spanLengthFld    = gtNewLclFldNode(spanLclNum, TYP_INT, OFFSETOF__CORINFO_Span__length);
    GenTree*       unrolled =
        impExpandHalfConstEquals(spanReferenceFld, spanLengthFld, false, kind, (WCHAR*)str, cnsLength, 0, cmpMode);

    if (unrolled != nullptr)
    {
        if (!spanObj->OperIs(GT_LCL_VAR))
        {
            impStoreToTemp(spanLclNum, spanObj, CHECK_SPILL_NONE);
        }

        if (unrolled->OperIs(GT_QMARK))
        {
            // QMARK can't be a root node, spill it to a temp
            unsigned rootTmp = lvaGrabTemp(true DEBUGARG("spilling unroll qmark"));
            impStoreToTemp(rootTmp, unrolled, CHECK_SPILL_NONE);
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
