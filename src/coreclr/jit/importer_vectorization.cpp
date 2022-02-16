// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "jitpch.h"
#ifdef _MSC_VER
#pragma hdrstop
#endif

//------------------------------------------------------------------------
// impExpandHalfConstEqualsSIMD: Attempts to unroll and vectorize
//    Equals against a constant WCHAR data for Length in [8..32] range
//    using SIMD instructions. It uses the following expression:
//
//      bool equasl = ((v1 ^ cns1) | (v2 ^ cns2)) == Vector128.Zero
//
//    or if a single vector is enough (len == 8 or len == 16 with AVX):
//
//      bool equasl = (v1 ^ cns1) == Vector128.Zero
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
GenTree* Compiler::impExpandHalfConstEqualsSIMD(GenTree* data, WCHAR* cns, int len, int dataOffset)
{
    assert(len >= 8 && len <= 32);

#if defined(FEATURE_HW_INTRINSICS) && defined(TARGET_64BIT)
    if (!compOpportunisticallyDependsOn(InstructionSet_Vector128))
    {
        // We need SSE2 or ADVSIMD at least
        return nullptr;
    }

    CorInfoType type = CORINFO_TYPE_ULONG;

    int       simdSize;
    var_types simdType;

    NamedIntrinsic niZero;
    NamedIntrinsic niEquals;
    NamedIntrinsic loadIntrinsic;

    GenTree* cnsVec1;
    GenTree* cnsVec2;

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

        // Special case: use a single vector for len=16
        useSingleVector = len == 16;

        assert(sizeof(ssize_t) == 8); // this code is guarded with TARGET_64BIT
        GenTree* long1 = gtNewIconNode(*(ssize_t*)(cns + 0), TYP_LONG);
        GenTree* long2 = gtNewIconNode(*(ssize_t*)(cns + 4), TYP_LONG);
        GenTree* long3 = gtNewIconNode(*(ssize_t*)(cns + 8), TYP_LONG);
        GenTree* long4 = gtNewIconNode(*(ssize_t*)(cns + 12), TYP_LONG);
        cnsVec1 = gtNewSimdHWIntrinsicNode(simdType, long1, long2, long3, long4, NI_Vector256_Create, type, simdSize);

        // cnsVec2 most likely overlaps with cnsVec1:
        GenTree* long5 = gtNewIconNode(*(ssize_t*)(cns + len - 16), TYP_LONG);
        GenTree* long6 = gtNewIconNode(*(ssize_t*)(cns + len - 12), TYP_LONG);
        GenTree* long7 = gtNewIconNode(*(ssize_t*)(cns + len - 8), TYP_LONG);
        GenTree* long8 = gtNewIconNode(*(ssize_t*)(cns + len - 4), TYP_LONG);
        cnsVec2 = gtNewSimdHWIntrinsicNode(simdType, long5, long6, long7, long8, NI_Vector256_Create, type, simdSize);

        loadIntrinsic = NI_AVX_LoadVector256;
    }
    else
#endif
        if (len <= 16)
    {
        // Handle [8..16] inputs via two Vector256
        assert(len >= 8 && len <= 16);

        simdSize = 16;
        simdType = TYP_SIMD16;

        niZero   = NI_Vector128_get_Zero;
        niEquals = NI_Vector128_op_Equality;

        // Special case: use a single vector for len=8
        useSingleVector = len == 8;

        assert(sizeof(ssize_t) == 8); // this code is guarded with TARGET_64BIT
        GenTree* long1 = gtNewIconNode(*(ssize_t*)(cns + 0), TYP_LONG);
        GenTree* long2 = gtNewIconNode(*(ssize_t*)(cns + 4), TYP_LONG);
        cnsVec1        = gtNewSimdHWIntrinsicNode(simdType, long1, long2, NI_Vector128_Create, type, simdSize);

        // cnsVec2 most likely overlaps with cnsVec1:
        GenTree* long3 = gtNewIconNode(*(ssize_t*)(cns + len - 8), TYP_LONG);
        GenTree* long4 = gtNewIconNode(*(ssize_t*)(cns + len - 4), TYP_LONG);
        cnsVec2        = gtNewSimdHWIntrinsicNode(simdType, long3, long4, NI_Vector128_Create, type, simdSize);

#if defined(TARGET_XARCH)
        loadIntrinsic = NI_SSE2_LoadVector128;
#else
        loadIntrinsic = NI_AdvSimd_LoadVector128;
#endif
    }
    else
    {
        JITDUMP("impExpandHalfConstEqualsSIMD: No V256 and data is too big for V128\n");
        // NOTE: We might consider using four V128 for ARM64
        return nullptr;
    }

    GenTree* zero = gtNewSimdHWIntrinsicNode(simdType, niZero, type, simdSize);

    GenTree* offset1  = gtNewIconNode(dataOffset, TYP_I_IMPL);
    GenTree* offset2  = gtNewIconNode(dataOffset + len * 2 - simdSize, TYP_I_IMPL);
    GenTree* dataPtr1 = gtNewOperNode(GT_ADD, TYP_BYREF, data, offset1);
    GenTree* dataPtr2 = gtNewOperNode(GT_ADD, TYP_BYREF, gtClone(data), offset2);

    GenTree* vec1 = gtNewSimdHWIntrinsicNode(simdType, dataPtr1, loadIntrinsic, type, simdSize);
    GenTree* vec2 = gtNewSimdHWIntrinsicNode(simdType, dataPtr2, loadIntrinsic, type, simdSize);

    // TODO-CQ: Spill vec1 and vec2 for better pipelining
    // However, Forward-Sub most likely will glue it back

    // ((v1 ^ cns1) | (v2 ^ cns2)) == zero
    GenTree* xor1 = gtNewSimdBinOpNode(GT_XOR, simdType, vec1, cnsVec1, type, simdSize, false);
    GenTree* xor2 = gtNewSimdBinOpNode(GT_XOR, simdType, vec2, cnsVec2, type, simdSize, false);
    GenTree* orr  = gtNewSimdBinOpNode(GT_OR, simdType, xor1, xor2, type, simdSize, false);
    return gtNewSimdHWIntrinsicNode(TYP_BOOL, useSingleVector ? xor1 : orr, zero, niEquals, type, simdSize);
#else
    return nullptr;
#endif
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
GenTree* Compiler::impExpandHalfConstEqualsSWAR(GenTree* data, WCHAR* cns, int len, int dataOffset)
{
    assert(len >= 1 && len <= 8);

    auto compareValue = [](Compiler* comp, GenTree* obj, var_types type, ssize_t offset, ssize_t value) {
        GenTree* offsetTree    = comp->gtNewIconNode(offset, TYP_I_IMPL);
        GenTree* addOffsetTree = comp->gtNewOperNode(GT_ADD, TYP_BYREF, obj, offsetTree);
        GenTree* indirTree     = comp->gtNewIndir(type, addOffsetTree);
        GenTree* valueTree     = comp->gtNewIconNode(value, type);
        if (varTypeIsSmall(indirTree))
        {
            indirTree = comp->gtNewCastNode(TYP_INT, indirTree, true, TYP_UINT);
            valueTree->ChangeType(TYP_INT);
        }
        return comp->gtNewOperNode(GT_EQ, TYP_INT, indirTree, valueTree);
    };

// Compose Int32 or Int64 values from ushort components
#define MAKEINT32(c1, c2) ((UINT64)c2 << 16) | ((UINT64)c1 << 0)
#define MAKEINT64(c1, c2, c3, c4) ((UINT64)c4 << 48) | ((UINT64)c3 << 32) | ((UINT64)c2 << 16) | ((UINT64)c1 << 0)

    if (len == 1)
    {
        return compareValue(this, data, TYP_SHORT, dataOffset, cns[0]);
    }
    else if (len == 2)
    {
        const UINT32 value = MAKEINT32(cns[0], cns[1]);
        return compareValue(this, data, TYP_INT, dataOffset, value);
    }
#ifdef TARGET_64BIT
    else if (len == 3)
    {
        // handle len = 3 via two Int32 with overlapping
        UINT32   value1      = MAKEINT32(cns[0], cns[1]);
        UINT32   value2      = MAKEINT32(cns[1], cns[2]);
        GenTree* firstIndir  = compareValue(this, data, TYP_INT, dataOffset, value1);
        GenTree* secondIndir = compareValue(this, gtClone(data), TYP_INT, dataOffset + 2, value2);

        // TODO: Consider marging two indirs via XOR instead of QMARK
        // e.g. gtNewOperNode(GT_XOR, TYP_INT, firstIndir, secondIndir);
        // but it currently has CQ issues
        GenTreeColon* doubleIndirColon = gtNewColonNode(TYP_INT, secondIndir, gtFalse());
        return gtNewQmarkNode(TYP_INT, firstIndir, doubleIndirColon);
    }
    else
    {
        assert(len >= 4 && len <= 8);

        UINT64 value1 = MAKEINT64(cns[0], cns[1], cns[2], cns[3]);
        if (len == 4)
        {
            return compareValue(this, data, TYP_LONG, dataOffset, value1);
        }
        else // [5..8] range
        {
            // For 5..7 value2 will overlap with value1
            UINT64   value2     = MAKEINT64(cns[len - 4], cns[len - 3], cns[len - 2], cns[len - 1]);
            GenTree* firstIndir = compareValue(this, data, TYP_LONG, dataOffset, value1);

            ssize_t  offset      = dataOffset + len * 2 - sizeof(UINT64);
            GenTree* secondIndir = compareValue(this, gtClone(data), TYP_LONG, offset, value2);

            // TODO: Consider marging two indirs via XOR instead of QMARK
            GenTreeColon* doubleIndirColon = gtNewColonNode(TYP_INT, secondIndir, gtFalse());
            return gtNewQmarkNode(TYP_INT, firstIndir, doubleIndirColon);
        }
    }
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
//    data -         Pointer to a data to vectorize
//    lengthFld -    Pointer to Length field
//    checkForNull - Check data for null
//    startsWith -   true - StartsWith, false - Equals
//    cns -          Constant data (array of 2-byte chars)
//    len -          Number of 2-byte chars in the cns
//    dataOffset -   Offset for data
//
// Return Value:
//    A pointer to the newly created SIMD node or nullptr if unrolling is not
//    possible or not profitable
//
GenTree* Compiler::impExpandHalfConstEquals(
    GenTree* data, GenTree* lengthFld, bool checkForNull, bool startsWith, WCHAR* cnsData, int len, int dataOffset)
{
    assert(len >= 0);

    if (compCurBB->isRunRarely())
    {
        // Not profitable to expand
        JITDUMP("impExpandHalfConstEquals: block is cold - not profitable to expand.\n");
        return nullptr;
    }

    if (fgBBcount > 20)
    {
        // We don't want to unroll too much and in big methods
        // TODO: come up with some better heuristic/budget
        JITDUMP("impExpandHalfConstEquals: method has too many BBs (>20) - not profitable to expand.\n");
        return nullptr;
    }

    GenTree* elementsCount = gtNewIconNode(len);
    GenTree* lenCheckNode;
    if (len == 0)
    {
        if (startsWith)
        {
            // Any string starts with ""
            return gtTrue();
        }

        // For zero length we don't need to compare content, the following expression is enough:
        //
        //   varData != null && lengthFld == 0
        //
        lenCheckNode = gtNewOperNode(GT_EQ, TYP_INT, lengthFld, elementsCount);
    }
    else
    {
        assert(cnsData != nullptr);

        GenTree* indirCmp = nullptr;
        if (len < 8) // SWAR impl supports len == 8 but we'd better give it to SIMD
        {
            indirCmp = impExpandHalfConstEqualsSWAR(gtClone(data), cnsData, len, dataOffset);
        }
        else if (len <= 32)
        {
            indirCmp = impExpandHalfConstEqualsSIMD(gtClone(data), cnsData, len, dataOffset);
        }

        if (indirCmp == nullptr)
        {
            JITDUMP("unable to compose indirCmp\n");
            return nullptr;
        }

        GenTreeColon* lenCheckColon = gtNewColonNode(TYP_INT, indirCmp, gtFalse());
        lenCheckNode =
            gtNewQmarkNode(TYP_INT, gtNewOperNode(startsWith ? GT_GE : GT_EQ, TYP_INT, lengthFld, elementsCount),
                           lenCheckColon);
    }

    GenTree* rootQmark;
    if (checkForNull)
    {
        // varData == nullptr
        GenTreeColon* nullCheckColon = gtNewColonNode(TYP_INT, lenCheckNode, gtFalse());
        rootQmark = gtNewQmarkNode(TYP_INT, gtNewOperNode(GT_NE, TYP_INT, data, gtNull()), nullCheckColon);
    }
    else
    {
        // no nullcheck, just "obj.Length == len && (SWAR or SIMD)"
        rootQmark = lenCheckNode;
    }

    return rootQmark;
}

//------------------------------------------------------------------------
// impGetStrConFromSpan: Try to obtain string literal out of a span
//    if it was inited with one.
//
// Arguments:
//    span - ReadOnlySpan tree
//
// Returns:
//    GenTreeStrCon node or nullptr
//
GenTreeStrCon* Compiler::impGetStrConFromSpan(GenTree* span)
{
    GenTreeCall* argCall = nullptr;
    if (span->OperIs(GT_RET_EXPR))
    {
        argCall = span->AsRetExpr()->gtInlineCandidate->AsCall();
    }
    else if (span->OperIs(GT_CALL))
    {
        argCall = span->AsCall();
    }

    if (argCall != nullptr)
    {
        NamedIntrinsic ni = lookupNamedIntrinsic(argCall->gtCallMethHnd);
        if ((ni == NI_System_MemoryExtensions_AsSpan) || (ni == NI_System_String_op_Implicit))
        {
            if (argCall->gtCallArgs->GetNode()->OperIs(GT_CNS_STR))
            {
                return argCall->gtCallArgs->GetNode()->AsStrCon();
            }
        }
    }
    return nullptr;
}

GenTree* Compiler::impStringEqualsOrStartsWith(bool startsWith, CORINFO_SIG_INFO* sig, unsigned methodFlags)
{
    // We're going to unroll & vectorize the following cases:
    //
    //   1) String.Equals(obj, "cns")
    //   2) String.Equals(obj, "cns", StringComparison.Ordinal)
    //   3) String.Equals("cns", obj)
    //   4) String.Equals("cns", obj, StringComparison.Ordinal)
    //   5) obj.Equals("cns")
    //   6) obj.Equals("cns", StringComparison.Ordinal)
    //   7) "cns".Equals(obj)
    //   8) "cns".Equals(obj, StringComparison.Ordinal)
    //
    //   9) obj.StartsWith("cns", StringComparison.Ordinal)
    //  10) "cns".StartsWith(obj, StringComparison.Ordinal)
    //
    // For cases 5, 6 and 9 we don't emit "obj != null"
    // NOTE: String.Equals(object) is not supported currently

    bool isStatic  = methodFlags & CORINFO_FLG_STATIC;
    int  argsCount = sig->numArgs + (isStatic ? 0 : 1);

    GenTree* op1;
    GenTree* op2;
    if (argsCount == 3) // overload with StringComparison
    {
        if (!impStackTop(0).val->IsIntegralConst(4)) // StringComparison.Ordinal
        {
            // TODO: Unroll & vectorize OrdinalIgnoreCase
            return nullptr;
        }
        op1 = impStackTop(2).val;
        op2 = impStackTop(1).val;
    }
    else
    {
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
        JITDUMP("Trying to unroll String.Equals(op1, \"\")...\n", str)
    }
    else
    {
        str = info.compCompHnd->getStringLiteral(cnsStr->gtScpHnd, cnsStr->gtSconCPX, &cnsLength);
        if (cnsLength < 0 || str == nullptr)
        {
            // We were unable to get the literal (e.g. dynamic context)
            return nullptr;
        }
        JITDUMP("Trying to unroll String.Equals(op1, \"%ws\")...\n", str)
    }

    // Create a temp safe to gtClone for varStr
    // We're not appending it as a statement untill we figure out unrolling is profitable (and possible)
    unsigned varStrTmp         = lvaGrabTemp(true DEBUGARG("spilling varStr"));
    lvaTable[varStrTmp].lvType = varStr->TypeGet();
    GenTree* varStrLcl         = gtNewLclvNode(varStrTmp, varStr->TypeGet());

    // TODO: Consider using ARR_LENGTH here, but we'll have to modify QMARK to propagate BBF_HAS_IDX_LEN
    int      strLenOffset = OFFSETOF__CORINFO_String__stringLen;
    GenTree* lenOffset    = gtNewIconNode(strLenOffset, TYP_I_IMPL);
    GenTree* lenNode      = gtNewIndir(TYP_INT, gtNewOperNode(GT_ADD, TYP_BYREF, varStrLcl, lenOffset));
    GenTree* unrolled = impExpandHalfConstEquals(gtClone(varStrLcl), lenNode, needsNullcheck, startsWith, (WCHAR*)str,
                                                 cnsLength, strLenOffset + sizeof(int));

    GenTree* retNode = nullptr;
    if (unrolled != nullptr)
    {
        impAssignTempGen(varStrTmp, varStr);
        if (unrolled->OperIs(GT_QMARK))
        {
            // QMARK nodes cannot reside on the evaluation stack
            unsigned rootTmp = lvaGrabTemp(true DEBUGARG("spilling unroll qmark"));
            impAssignTempGen(rootTmp, unrolled);
            retNode = gtNewLclvNode(rootTmp, TYP_INT);
        }
        else
        {
            retNode = unrolled;
        }

        JITDUMP("\n... Successfully unrolled to:\n")
        DISPTREE(unrolled)
        for (int i = 0; i < argsCount; i++)
        {
            // max(2, numArgs) just to handle instance-method Equals that
            // doesn't report "this" as an argument
            impPopStack();
        }
    }
    return retNode;
}

GenTree* Compiler::impSpanEqualsOrStartsWith(bool startsWith, CORINFO_SIG_INFO* sig, unsigned methodFlags)
{
    // We're going to unroll & vectorize the following cases:
    //
    //   1) MemoryExtensions.SequenceEqual<char>(var, "cns")
    //   2) MemoryExtensions.SequenceEqual<char>("cns", var)
    //   3) MemoryExtensions.Equals(var, "cns", StringComparison.Ordinal)
    //   4) MemoryExtensions.Equals("cns", var, StringComparison.Ordinal)
    //   5) MemoryExtensions.StartsWith<char>("cns", var)
    //   6) MemoryExtensions.StartsWith<char>(var, "cns")
    //   7) MemoryExtensions.StartsWith("cns", var, StringComparison.Ordinal)
    //   8) MemoryExtensions.StartsWith(var, "cns", StringComparison.Ordinal)

    bool isStatic  = methodFlags & CORINFO_FLG_STATIC;
    int  argsCount = sig->numArgs + (isStatic ? 0 : 1);

    GenTree* op1;
    GenTree* op2;
    if (argsCount == 3) // overload with StringComparison
    {
        if (!impStackTop(0).val->IsIntegralConst(4)) // StringComparison.Ordinal
        {
            // TODO: Unroll & vectorize OrdinalIgnoreCase
            return nullptr;
        }
        op1 = impStackTop(2).val;
        op2 = impStackTop(1).val;
    }
    else
    {
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

    GenTreeStrCon* op1Str = impGetStrConFromSpan(op1);
    GenTreeStrCon* op2Str = impGetStrConFromSpan(op2);

    if (!((op1Str != nullptr) ^ (op2Str != nullptr)))
    {
        // either op1 or op2 has to be '(ReadOnlySpan)"cns"'
        return nullptr;
    }

    GenTree*       varStr;
    GenTreeStrCon* cnsStr;
    if (op1Str != nullptr)
    {
        cnsStr = op1Str;
        varStr = op2;
    }
    else
    {
        cnsStr = op2Str;
        varStr = op1;
    }

    int             cnsLength = -1;
    const char16_t* str       = nullptr;
    if (cnsStr->IsStringEmptyField())
    {
        // check for fake "" first
        cnsLength = 0;
        JITDUMP("Trying to unroll String.Equals(op1, \"\")...\n", str)
    }
    else
    {
        str = info.compCompHnd->getStringLiteral(cnsStr->gtScpHnd, cnsStr->gtSconCPX, &cnsLength);
        if (cnsLength < 0 || str == nullptr)
        {
            // We were unable to get the literal (e.g. dynamic context)
            return nullptr;
        }
        JITDUMP("Trying to unroll String.Equals(op1, \"%ws\")...\n", str)
    }

    // Rewrite this nonsense (to fields)

    CORINFO_CLASS_HANDLE spanCls      = gtGetStructHandle(varStr);
    CORINFO_FIELD_HANDLE pointerHnd   = info.compCompHnd->getFieldInClass(spanCls, 0);
    CORINFO_FIELD_HANDLE lengthHnd    = info.compCompHnd->getFieldInClass(spanCls, 1);
    const unsigned       lengthOffset = info.compCompHnd->getFieldOffset(lengthHnd);

    GenTree* spanRef = varStr;
    if (varStr->TypeIs(TYP_STRUCT))
    {
        spanRef = gtNewOperNode(GT_ADDR, TYP_BYREF, varStr);
    }
    assert(spanRef->TypeIs(TYP_BYREF));

    unsigned varStrTmp         = lvaGrabTemp(true DEBUGARG("t1"));
    lvaTable[varStrTmp].lvType = TYP_BYREF;

    GenTree* spanRefLcl = gtNewLclvNode(varStrTmp, spanRef->TypeGet());
    GenTree* spanData   = gtNewFieldRef(TYP_BYREF, pointerHnd, spanRefLcl);
    GenTree* spanLength = gtNewFieldRef(TYP_INT, lengthHnd, gtClone(spanRefLcl), lengthOffset);

    unsigned spanDataTmp = lvaGrabTemp(true DEBUGARG("t2"));
    lvaSetStruct(spanDataTmp, spanCls, false);
    lvaTable[spanDataTmp].lvType = spanData->TypeGet();

    GenTree* result   = nullptr;
    GenTree* unrolled = impExpandHalfConstEquals(gtNewLclvNode(spanDataTmp, spanData->TypeGet()), spanLength, false,
                                                 startsWith, (WCHAR*)str, cnsLength, 0);
    if (unrolled != nullptr)
    {
        impAssignTempGen(varStrTmp, spanRef);
        impAssignTempGen(spanDataTmp, spanData);
        if (unrolled->OperIs(GT_QMARK))
        {
            // QMARK can't be a root node, spill it to a temp
            unsigned rootTmp = lvaGrabTemp(true DEBUGARG("spilling unroll qmark"));
            impAssignTempGen(rootTmp, unrolled);
            result = gtNewLclvNode(rootTmp, TYP_INT);
        }
        else
        {
            result = unrolled;
        }

        JITDUMP("... Successfully unrolled to:\n")
        DISPTREE(unrolled)

        for (int i = 0; i < argsCount; i++)
        {
            impPopStack();
        }

        // We have to clean up GT_RET_EXPR for String.op_Implicit
        if (op1->OperIs(GT_RET_EXPR))
        {
            op1->AsRetExpr()->gtInlineCandidate->ReplaceWith(gtNewNothingNode(), this);
            DEBUG_DESTROY_NODE(op1);
        }
        else if (op2->OperIs(GT_RET_EXPR))
        {
            assert(op2Str != nullptr);
            op2->AsRetExpr()->gtInlineCandidate->ReplaceWith(gtNewNothingNode(), this);
            DEBUG_DESTROY_NODE(op2);
        }
    }
    return result;
}
