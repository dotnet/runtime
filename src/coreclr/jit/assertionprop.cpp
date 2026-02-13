// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XX                                                                           XX
XX                          AssertionProp                                    XX
XX                                                                           XX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
*/

#include "jitpch.h"
#include "rangecheck.h"
#ifdef _MSC_VER
#pragma hdrstop
#endif

//------------------------------------------------------------------------------
// GetAssertionDep: Retrieve the assertions on this local variable
//
// Arguments:
//    lclNum    - The local var id.
//    mustExist - If true, assert that the dependent assertions must exist.
//
// Return Value:
//    The dependent assertions (assertions using the value of the local var)
//    of the local var.
//

ASSERT_TP& Compiler::GetAssertionDep(unsigned lclNum, bool mustExist)
{
    JitExpandArray<ASSERT_TP>& dep = *optAssertionDep;
    if (dep[lclNum] == nullptr)
    {
        if (mustExist)
        {
            assert(!"No dependent assertions for local var");
        }
        dep[lclNum] = BitVecOps::MakeEmpty(apTraits);
    }
    return dep[lclNum];
}

/*****************************************************************************
 *
 *  Initialize the assertion prop bitset traits and the default bitsets.
 */

void Compiler::optAssertionTraitsInit(AssertionIndex assertionCount)
{
    apTraits = new (this, CMK_AssertionProp) BitVecTraits(assertionCount, this);
    apFull   = BitVecOps::MakeFull(apTraits);
}

/*****************************************************************************
 *
 *  Initialize the assertion prop tracking logic.
 */

void Compiler::optAssertionInit(bool isLocalProp)
{
    assert(NO_ASSERTION_INDEX == 0);
    const unsigned maxTrackedLocals = (unsigned)JitConfig.JitMaxLocalsToTrack();

    // We initialize differently for local prop / global prop
    //
    if (isLocalProp)
    {
        optLocalAssertionProp           = true;
        optCrossBlockLocalAssertionProp = true;

        // Disable via config
        //
        if (JitConfig.JitEnableCrossBlockLocalAssertionProp() == 0)
        {
            JITDUMP("Disabling cross-block assertion prop by config setting\n");
            optCrossBlockLocalAssertionProp = false;
        }

#ifdef DEBUG
        // Disable per method via range
        //
        static ConfigMethodRange s_range;
        s_range.EnsureInit(JitConfig.JitEnableCrossBlockLocalAssertionPropRange());
        if (!s_range.Contains(info.compMethodHash()))
        {
            JITDUMP("Disabling cross-block assertion prop by config range\n");
            optCrossBlockLocalAssertionProp = false;
        }
#endif

        // Disable if too many locals
        //
        // The typical number of local assertions is roughly proportional
        // to the number of locals. So when we have huge numbers of locals,
        // just do within-block local assertion prop.
        //
        if (lvaCount > maxTrackedLocals)
        {
            JITDUMP("Disabling cross-block assertion prop: too many locals\n");
            optCrossBlockLocalAssertionProp = false;
        }

        if (optCrossBlockLocalAssertionProp)
        {
            // We may need a fairly large table. Keep size a multiple of 64.
            // Empirical studies show about 1.16 asserions/ tracked local.
            //
            if (lvaTrackedCount < 24)
            {
                optMaxAssertionCount = 64;
            }
            else if (lvaTrackedCount < 64)
            {
                optMaxAssertionCount = 128;
            }
            else
            {
                optMaxAssertionCount = (AssertionIndex)min(maxTrackedLocals, ((3 * lvaTrackedCount / 128) + 1) * 64);
            }

            JITDUMP("Cross-block table size %u (for %u tracked locals)\n", optMaxAssertionCount, lvaTrackedCount);
        }
        else
        {
            // The assertion table will be reset for each block, so it can be smaller.
            //
            optMaxAssertionCount = 64;
        }

        // Local assertion prop keeps mappings from each local var to the assertions about that var.
        //
        optAssertionDep =
            new (this, CMK_AssertionProp) JitExpandArray<ASSERT_TP>(getAllocator(CMK_AssertionProp), max(1u, lvaCount));

        if (optCrossBlockLocalAssertionProp)
        {
            optComplementaryAssertionMap = new (this, CMK_AssertionProp)
                AssertionIndex[optMaxAssertionCount + 1](); // zero-inited (NO_ASSERTION_INDEX)
        }
    }
    else
    {
        // General assertion prop.
        //
        optLocalAssertionProp           = false;
        optCrossBlockLocalAssertionProp = false;

        // Heuristic for sizing the assertion table.
        //
        // The weighting of basicBlocks vs locals reflects their relative contribution observed empirically.
        // Validated against 1,115,046 compiled methods:
        //   - 94.6% of methods stay at the floor of 64 (only 1.9% actually need more).
        //   - Underpredicts for 481 methods (0.043%), with a worst-case deficit of 127.
        //   - Only 0.4% of methods hit the 256 cap.
        optMaxAssertionCount = (AssertionIndex)max(64, min(256, (int)(lvaTrackedCount + 3 * fgBBcount + 48) >> 2));

        optComplementaryAssertionMap = new (this, CMK_AssertionProp)
            AssertionIndex[optMaxAssertionCount + 1](); // zero-inited (NO_ASSERTION_INDEX)
    }

    optAssertionTabPrivate = new (this, CMK_AssertionProp) AssertionDsc[optMaxAssertionCount];
    optAssertionTraitsInit(optMaxAssertionCount);

    optAssertionCount      = 0;
    optAssertionOverflow   = 0;
    optAssertionPropagated = false;
    bbJtrueAssertionOut    = nullptr;
    optCanPropLclVar       = false;
    optCanPropEqual        = false;
    optCanPropNonNull      = false;
    optCanPropBndsChk      = false;
    optCanPropSubRange     = false;
}

#ifdef DEBUG
void Compiler::optPrintAssertion(const AssertionDsc& curAssertion, AssertionIndex assertionIndex /* = 0 */)
{
    // Print assertion index if provided
    if (assertionIndex > 0)
    {
        optPrintAssertionIndex(assertionIndex);
        printf(" ");
    }

    switch (curAssertion.GetOp1().GetKind())
    {
        case O1K_LCLVAR:
            if (optLocalAssertionProp)
            {
                printf("lclvar V%02u", curAssertion.GetOp1().GetLclNum());
            }
            else
            {
                printf("lclvar " FMT_VN "", curAssertion.GetOp1().GetVN());
            }
            break;

        case O1K_VN:
            printf("VN " FMT_VN "", curAssertion.GetOp1().GetVN());
            break;

        case O1K_EXACT_TYPE:
            printf("ExactType " FMT_VN "", curAssertion.GetOp1().GetVN());
            break;

        case O1K_SUBTYPE:
            printf("SubType " FMT_VN "", curAssertion.GetOp1().GetVN());
            break;

        default:
            unreached();
            break;
    }

    switch (curAssertion.GetKind())
    {
        case OAK_EQUAL:
            printf(" == ");
            break;

        case OAK_NOT_EQUAL:
            printf(" != ");
            break;

        case OAK_LT:
            printf(" < ");
            break;

        case OAK_LT_UN:
            printf(" u< ");
            break;

        case OAK_LE:
            printf(" <= ");
            break;

        case OAK_LE_UN:
            printf(" u<= ");
            break;

        case OAK_GT:
            printf(" > ");
            break;

        case OAK_GT_UN:
            printf(" u> ");
            break;

        case OAK_GE:
            printf(" >= ");
            break;

        case OAK_GE_UN:
            printf(" u>= ");
            break;

        case OAK_SUBRANGE:
            printf(" in ");
            break;

        default:
            unreached();
            break;
    }

    switch (curAssertion.GetOp2().GetKind())
    {
        case O2K_LCLVAR_COPY:
            printf("lclvar V%02u", curAssertion.GetOp2().GetLclNum());
            break;

        case O2K_CONST_INT:
            if (curAssertion.GetOp1().KindIs(O1K_EXACT_TYPE, O1K_SUBTYPE))
            {
                ssize_t iconVal = curAssertion.GetOp2().GetIntConstant();
                if (IsAot())
                {
                    printf("MT(%p)", dspPtr(iconVal));
                }
                else
                {
                    printf("MT(%s)", eeGetClassName(reinterpret_cast<CORINFO_CLASS_HANDLE>(iconVal)));
                }
            }
            else if (curAssertion.GetOp2().IsNullConstant())
            {
                printf("null");
            }
            else if (curAssertion.GetOp2().HasIconFlag())
            {
                printf("[%p]", dspPtr(curAssertion.GetOp2().GetIntConstant()));
            }
            else
            {
                printf("%lld", (int64_t)curAssertion.GetOp2().GetIntConstant());
            }
            break;

        case O2K_CONST_DOUBLE:
            if (FloatingPointUtils::isNegativeZero(curAssertion.GetOp2().GetDoubleConstant()))
            {
                printf("-0.0");
            }
            else
            {
                printf("%#lg", curAssertion.GetOp2().GetDoubleConstant());
            }
            break;

        case O2K_ZEROOBJ:
            printf("ZeroObj");
            break;

        case O2K_SUBRANGE:
            IntegralRange::Print(curAssertion.GetOp2().GetIntegralRange());
            break;

        case O2K_CHECKED_BOUND_ADD_CNS:
            printf("(Checked_Bnd_BinOp " FMT_VN " + %d)", curAssertion.GetOp2().GetCheckedBound(),
                   curAssertion.GetOp2().GetCheckedBoundConstant());
            break;

        default:
            unreached();
            break;
    }

    printf("\n");
}

void Compiler::optPrintAssertionIndex(AssertionIndex index)
{
    if (index == NO_ASSERTION_INDEX)
    {
        printf("#NA");
        return;
    }

    printf("#%02u", index);
}

void Compiler::optPrintAssertionIndices(ASSERT_TP assertions)
{
    if (BitVecOps::IsEmpty(apTraits, assertions))
    {
        optPrintAssertionIndex(NO_ASSERTION_INDEX);
        return;
    }

    BitVecOps::Iter iter(apTraits, assertions);
    unsigned        bitIndex = 0;
    if (iter.NextElem(&bitIndex))
    {
        optPrintAssertionIndex(static_cast<AssertionIndex>(bitIndex + 1));
        while (iter.NextElem(&bitIndex))
        {
            printf(" ");
            optPrintAssertionIndex(static_cast<AssertionIndex>(bitIndex + 1));
        }
    }
}
#endif // DEBUG

/* static */
void Compiler::optDumpAssertionIndices(const char* header, ASSERT_TP assertions, const char* footer /* = nullptr */)
{
#ifdef DEBUG
    Compiler* compiler = JitTls::GetCompiler();
    if (compiler->verbose)
    {
        printf(header);
        compiler->optPrintAssertionIndices(assertions);
        if (footer != nullptr)
        {
            printf(footer);
        }
    }
#endif // DEBUG
}

/* static */
void Compiler::optDumpAssertionIndices(ASSERT_TP assertions, const char* footer /* = nullptr */)
{
    optDumpAssertionIndices("", assertions, footer);
}

/******************************************************************************
 *
 * Helper to retrieve the "assertIndex" assertion. Note that assertIndex 0
 * is NO_ASSERTION_INDEX and "optAssertionCount" is the last valid index.
 *
 */
const Compiler::AssertionDsc& Compiler::optGetAssertion(AssertionIndex assertIndex) const
{
    assert(NO_ASSERTION_INDEX == 0);
    assert(assertIndex != NO_ASSERTION_INDEX);
    assert(assertIndex <= optAssertionCount);
    const AssertionDsc& assertion = optAssertionTabPrivate[assertIndex - 1];
#ifdef DEBUG
    optDebugCheckAssertion(assertion);
#endif

    return assertion;
}

ValueNum Compiler::optConservativeNormalVN(GenTree* tree)
{
    if (optLocalAssertionProp)
    {
        return ValueNumStore::NoVN;
    }

    assert(vnStore != nullptr);
    return vnStore->VNConservativeNormalValue(tree->gtVNPair);
}

//------------------------------------------------------------------------
// optCastConstantSmall: Cast a constant to a small type.
//
// Parameters:
//   iconVal - the integer constant
//   smallType - the small type to cast to
//
// Returns:
//   The cast constant after sign/zero extension.
//
ssize_t Compiler::optCastConstantSmall(ssize_t iconVal, var_types smallType)
{
    switch (smallType)
    {
        case TYP_BYTE:
            return int8_t(iconVal);

        case TYP_SHORT:
            return int16_t(iconVal);

        case TYP_USHORT:
            return uint16_t(iconVal);

        case TYP_UBYTE:
            return uint8_t(iconVal);

        default:
            assert(!"Unexpected type to truncate to");
            return iconVal;
    }
}

//------------------------------------------------------------------------
// optCreateAssertion: Create an (op1 assertionKind op2) assertion.
//
// Arguments:
//    op1    - the first assertion operand
//    op2    - the second assertion operand
//    equals - the assertion kind (equals / not equals)

// Return Value:
//    The new assertion index or NO_ASSERTION_INDEX if a new assertion
//    was not created.
//
// Notes:
//    Assertion creation may fail either because the provided assertion
//    operands aren't supported or because the assertion table is full.
//
AssertionIndex Compiler::optCreateAssertion(GenTree* op1, GenTree* op2, bool equals)
{
    assert(op1 != nullptr);

    if (op2 == nullptr)
    {
        // Must be an OAK_NOT_EQUAL assertion
        assert(!equals);

        // Set op1 to the instance pointer of the indirection
        op1 = op1->gtEffectiveVal();

        // TODO-Cleanup: Replace with gtPeelOffset with proper fgBigOffset check
        // It will produce a few regressions.
        ssize_t offset = 0;
        while (op1->OperIs(GT_ADD) && op1->TypeIs(TYP_BYREF))
        {
            if (op1->gtGetOp2()->IsCnsIntOrI())
            {
                offset += op1->gtGetOp2()->AsIntCon()->gtIconVal;
                op1 = op1->gtGetOp1()->gtEffectiveVal();
            }
            else if (op1->gtGetOp1()->IsCnsIntOrI())
            {
                offset += op1->gtGetOp1()->AsIntCon()->gtIconVal;
                op1 = op1->gtGetOp2()->gtEffectiveVal();
            }
            else
            {
                break;
            }
        }

        if (!fgIsBigOffset(offset) && op1->OperIs(GT_LCL_VAR) && !lvaVarAddrExposed(op1->AsLclVar()->GetLclNum()))
        {
            if (optLocalAssertionProp)
            {
                AssertionDsc assertion = AssertionDsc::CreateLclNonNullAssertion(this, op1->AsLclVar()->GetLclNum());
                return optAddAssertion(assertion);
            }

            ValueNum op1VN = optConservativeNormalVN(op1);
            if (op1VN == ValueNumStore::NoVN)
            {
                return NO_ASSERTION_INDEX;
            }
            AssertionDsc assertion = AssertionDsc::CreateVNNonNullAssertion(this, op1VN);
            return optAddAssertion(assertion);
        }
    }
    //
    // Are we making an assertion about a local variable?
    //
    else if (op1->OperIsScalarLocal())
    {
        unsigned const   lclNum = op1->AsLclVarCommon()->GetLclNum();
        LclVarDsc* const lclVar = lvaGetDesc(lclNum);

        // If the local variable has its address exposed then bail
        //
        if (lclVar->IsAddressExposed())
        {
            return NO_ASSERTION_INDEX;
        }

        /* Skip over a GT_COMMA node(s), if necessary */
        while (op2->OperIs(GT_COMMA))
        {
            op2 = op2->AsOp()->gtOp2;
        }

        switch (op2->OperGet())
        {
            //
            //  Constant Assertions
            //
            case GT_CNS_DBL:
            {
                double dblCns = op2->AsDblCon()->DconValue();
                if (FloatingPointUtils::isNaN(dblCns))
                {
                    return NO_ASSERTION_INDEX;
                }

                ValueNum op1VN = optConservativeNormalVN(op1);
                ValueNum op2VN = optConservativeNormalVN(op2);
                if (!optLocalAssertionProp && (op1VN == ValueNumStore::NoVN || op2VN == ValueNumStore::NoVN))
                {
                    // GlobalAP requires valid VNs.
                    return NO_ASSERTION_INDEX;
                }

                AssertionDsc dsc = AssertionDsc::CreateConstLclVarAssertion(this, lclNum, op1VN, dblCns, op2VN, equals);
                return optAddAssertion(dsc);
            }

            case GT_CNS_INT:
            {
                ValueNum op1VN = optConservativeNormalVN(op1);
                ValueNum op2VN = optConservativeNormalVN(op2);
                if (!optLocalAssertionProp && (op1VN == ValueNumStore::NoVN || op2VN == ValueNumStore::NoVN))
                {
                    return NO_ASSERTION_INDEX;
                }

                ssize_t iconVal = op2->AsIntCon()->IconValue();
                if (op1->TypeIs(TYP_STRUCT))
                {
                    assert(iconVal == 0);
                    AssertionDsc dsc =
                        AssertionDsc::CreateConstLclVarAssertion(this, lclNum, op1VN, O2K_ZEROOBJ, op2VN, equals);
                    return optAddAssertion(dsc);
                }

                if (varTypeIsSmall(lclVar))
                {
                    ssize_t truncatedIconVal = optCastConstantSmall(iconVal, lclVar->TypeGet());
                    if (!op1->OperIs(GT_STORE_LCL_VAR) && (truncatedIconVal != iconVal))
                    {
                        // This assertion would be saying that a small local is equal to a value
                        // outside its range. It means this block is unreachable. Avoid creating
                        // such impossible assertions which can hit assertions in other places.
                        return NO_ASSERTION_INDEX;
                    }

                    iconVal = truncatedIconVal;
                    if (!optLocalAssertionProp)
                    {
                        op2VN = vnStore->VNForIntCon(static_cast<int>(iconVal));
                    }
                }

                AssertionDsc dsc =
                    AssertionDsc::CreateConstLclVarAssertion(this, lclNum, op1VN, iconVal, op2VN, equals,
                                                             op2->GetIconHandleFlag(), op2->AsIntCon()->gtFieldSeq);
                return optAddAssertion(dsc);
            }

            case GT_LCL_VAR:
            {
                if (!optLocalAssertionProp)
                {
                    // O2K_LCLVAR_COPY is local assertion prop only
                    return NO_ASSERTION_INDEX;
                }

                unsigned   lclNum2 = op2->AsLclVarCommon()->GetLclNum();
                LclVarDsc* lclVar2 = lvaGetDesc(lclNum2);

                // If the two locals are the same then bail
                if (lclNum == lclNum2)
                {
                    return NO_ASSERTION_INDEX;
                }

                // If the types are different then bail */
                if (lclVar->lvType != lclVar2->lvType)
                {
                    return NO_ASSERTION_INDEX;
                }

                // If we're making a copy of a "normalize on load" lclvar then the destination
                // has to be "normalize on load" as well, otherwise we risk skipping normalization.
                if (lclVar2->lvNormalizeOnLoad() && !lclVar->lvNormalizeOnLoad())
                {
                    return NO_ASSERTION_INDEX;
                }

                //  If the local variable has its address exposed then bail
                if (lclVar2->IsAddressExposed())
                {
                    return NO_ASSERTION_INDEX;
                }

                // We process locals when we see the LCL_VAR node instead
                // of at its actual use point (its parent). That opens us
                // up to problems in a case like the following, assuming we
                // allowed creating an assertion like V10 = V35:
                //
                // └──▌  ADD       int
                //    ├──▌  LCL_VAR   int    V10 tmp6        -> copy propagated to [V35 tmp31]
                //    └──▌  COMMA     int
                //       ├──▌  STORE_LCL_VAR int    V35 tmp31
                //       │  └──▌  LCL_FLD   int    V03 loc1         [+4]
                if (lclVar2->lvRedefinedInEmbeddedStatement)
                {
                    return NO_ASSERTION_INDEX;
                }

                // Ok everything has been set and the assertion looks good
                AssertionDsc assertion = AssertionDsc::CreateLclvarCopy(this, lclNum, lclNum2, equals);
                return optAddAssertion(assertion);
            }

            case GT_CALL:
            {
                if (optLocalAssertionProp)
                {
                    GenTreeCall* const call = op2->AsCall();
                    if (call->IsHelperCall() && s_helperCallProperties.NonNullReturn(call->GetHelperNum()))
                    {
                        AssertionDsc assertion = AssertionDsc::CreateLclNonNullAssertion(this, lclNum);
                        return optAddAssertion(assertion);
                    }
                }
                break;
            }

            default:
                break;
        }

        // Try and see if we can make a subrange assertion.
        if (optLocalAssertionProp && equals && varTypeIsIntegral(op2))
        {
            IntegralRange nodeRange = IntegralRange::ForNode(op2, this);
            IntegralRange typeRange = IntegralRange::ForType(genActualType(op2));
            assert(typeRange.Contains(nodeRange));

            if (!typeRange.Equals(nodeRange))
            {
                AssertionDsc assertion = AssertionDsc::CreateSubrange(this, lclNum, nodeRange);
                return optAddAssertion(assertion);
            }
        }
    }
    else
    {
        // Currently, O1K_VN serves as a backup for O1K_LCLVAR (where it's not a local),
        // but long term we should keep O1K_LCLVAR for local assertions only.
        if (!optLocalAssertionProp)
        {
            ValueNum op1VN = optConservativeNormalVN(op1);
            ValueNum op2VN = optConservativeNormalVN(op2);

            // For TP reasons, limited to 32-bit constants on the op2 side.
            if (op1VN != ValueNumStore::NoVN && op2VN != ValueNumStore::NoVN && vnStore->IsVNInt32Constant(op2VN) &&
                !vnStore->IsVNHandle(op2VN))
            {
                AssertionDsc assertion = AssertionDsc::CreateInt32ConstantVNAssertion(this, op1VN, op2VN, equals);
                return optAddAssertion(assertion);
            }
        }
    }
    return NO_ASSERTION_INDEX;
}

/*****************************************************************************
 *
 * If tree is a constant node holding an integral value, retrieve the value in
 * pConstant. If the method returns true, pConstant holds the appropriate
 * constant. Set "vnBased" to true to indicate local or global assertion prop.
 * "pFlags" indicates if the constant is a handle marked by GTF_ICON_HDL_MASK.
 */
bool Compiler::optIsTreeKnownIntValue(bool vnBased, GenTree* tree, ssize_t* pConstant, GenTreeFlags* pFlags)
{
    // Is Local assertion prop?
    if (!vnBased)
    {
        if (tree->OperIs(GT_CNS_INT))
        {
            *pConstant = tree->AsIntCon()->IconValue();
            *pFlags    = tree->GetIconHandleFlag();
            return true;
        }
        return false;
    }

    // Global assertion prop
    ValueNum vn = vnStore->VNConservativeNormalValue(tree->gtVNPair);
    if (!vnStore->IsVNConstant(vn))
    {
        return false;
    }

    // ValueNumber 'vn' indicates that this node evaluates to a constant

    var_types vnType = vnStore->TypeOfVN(vn);
    if (vnType == TYP_INT)
    {
        *pConstant = vnStore->ConstantValue<int>(vn);
        *pFlags    = vnStore->IsVNHandle(vn) ? vnStore->GetHandleFlags(vn) : GTF_EMPTY;
        return true;
    }
#ifdef TARGET_64BIT
    else if (vnType == TYP_LONG)
    {
        *pConstant = vnStore->ConstantValue<INT64>(vn);
        *pFlags    = vnStore->IsVNHandle(vn) ? vnStore->GetHandleFlags(vn) : GTF_EMPTY;
        return true;
    }
#endif

    return false;
}

/*****************************************************************************
 *
 *  Given an assertion add it to the assertion table
 *
 *  If it is already in the assertion table return the assertionIndex that
 *  we use to refer to this element.
 *  Otherwise add it to the assertion table and return the assertionIndex that
 *  we use to refer to this element.
 *  If we need to add to the table and the table is full return the value zero
 */
AssertionIndex Compiler::optAddAssertion(const AssertionDsc& newAssertion)
{
    bool canAddNewAssertions = optAssertionCount < optMaxAssertionCount;

    // See if we already have this assertion in the table.
    //
    // For local assertion prop we can speed things up by checking the dep vector.
    // Note we only need check the op1 vector; copies get indexed on both op1
    // and op2, so searching the first will find any existing match.
    //
    if (optLocalAssertionProp)
    {
        assert(newAssertion.GetOp1().KindIs(O1K_LCLVAR));

        unsigned        lclNum = newAssertion.GetOp1().GetLclNum();
        BitVecOps::Iter iter(apTraits, GetAssertionDep(lclNum));
        unsigned        bvIndex = 0;
        while (iter.NextElem(&bvIndex))
        {
            AssertionIndex const index        = GetAssertionIndex(bvIndex);
            const AssertionDsc&  curAssertion = optGetAssertion(index);

            if (curAssertion.Equals(newAssertion, /* vnBased */ false))
            {
                return index;
            }
        }
    }
    else
    {
        bool mayHaveDuplicates =
            optAssertionHasAssertionsForVN(newAssertion.GetOp1().GetVN(), /* addIfNotFound */ canAddNewAssertions);
        // We need to register op2.vn too, even if we know for sure there are no duplicates
        if (newAssertion.GetOp2().KindIs(O2K_CHECKED_BOUND_ADD_CNS))
        {
            mayHaveDuplicates |= optAssertionHasAssertionsForVN(newAssertion.GetOp2().GetCheckedBound(),
                                                                /* addIfNotFound */ canAddNewAssertions);

            // Additionally, check for the pattern of "VN + const == checkedBndVN" and register "VN" as well.
            ValueNum addOpVN;
            if (vnStore->IsVNBinFuncWithConst<int>(newAssertion.GetOp1().GetVN(), VNF_ADD, &addOpVN, nullptr))
            {
                mayHaveDuplicates |= optAssertionHasAssertionsForVN(addOpVN, /* addIfNotFound */ canAddNewAssertions);
            }
        }

        if (mayHaveDuplicates)
        {
            // For global prop we search the entire table.
            //
            // Check if exists already, so we can skip adding new one. Search backwards.
            for (AssertionIndex index = optAssertionCount; index >= 1; index--)
            {
                const AssertionDsc& curAssertion = optGetAssertion(index);
                if (curAssertion.Equals(newAssertion, /* vnBased */ true))
                {
                    return index;
                }
            }
        }
    }

    // Check if we are within max count.
    if (!canAddNewAssertions)
    {
        optAssertionOverflow++;
        return NO_ASSERTION_INDEX;
    }

    optAssertionTabPrivate[optAssertionCount] = newAssertion;
    optAssertionCount++;

#ifdef DEBUG
    if (verbose)
    {
        printf("GenTreeNode creates assertion:\n");
        gtDispTree(optAssertionPropCurrentTree, nullptr, nullptr, true);
        printf(optLocalAssertionProp ? "In " FMT_BB " New Local " : "In " FMT_BB " New Global ", compCurBB->bbNum);
        optPrintAssertion(newAssertion, optAssertionCount);
    }
#endif // DEBUG

    // Track the short-circuit criteria
    optCanPropLclVar |= newAssertion.CanPropLclVar();
    optCanPropEqual |= newAssertion.CanPropEqualOrNotEqual();
    optCanPropNonNull |= newAssertion.CanPropNonNull();
    optCanPropSubRange |= newAssertion.CanPropSubRange();
    optCanPropBndsChk |= newAssertion.CanPropBndsCheck();

    // Assertion mask bits are [index + 1].
    if (optLocalAssertionProp)
    {
        assert(newAssertion.GetOp1().KindIs(O1K_LCLVAR));

        // Mark the variables this index depends on
        unsigned lclNum = newAssertion.GetOp1().GetLclNum();
        BitVecOps::AddElemD(apTraits, GetAssertionDep(lclNum), optAssertionCount - 1);
        if (newAssertion.GetOp2().KindIs(O2K_LCLVAR_COPY))
        {
            lclNum = newAssertion.GetOp2().GetLclNum();
            BitVecOps::AddElemD(apTraits, GetAssertionDep(lclNum), optAssertionCount - 1);
        }
    }

#ifdef DEBUG
    optDebugCheckAssertions(optAssertionCount);
#endif
    return optAssertionCount;
}

//------------------------------------------------------------------------
// optAssertionHasAssertionsForVN: Check if we already have assertions for the given VN.
//    If "addIfNotFound" is true, add the VN to the map if it's not already there.
//
// Arguments:
//    vn            - the VN to check for
//    addIfNotFound - whether to add the VN to the map if it's not found
//
// Return Value:
//    true if we already have assertions for the given VN, false otherwise.
//
bool Compiler::optAssertionHasAssertionsForVN(ValueNum vn, bool addIfNotFound)
{
    assert(!optLocalAssertionProp);
    if (vn == ValueNumStore::NoVN)
    {
        assert(!addIfNotFound);
        return false;
    }

    if (addIfNotFound)
    {
        // Lazy initialize the map when we first need to add to it
        if (optAssertionVNsMap == nullptr)
        {
            optAssertionVNsMap = new (this, CMK_AssertionProp) VNSet(getAllocator(CMK_AssertionProp));
        }

        // Avoid double lookup by using the return value of LookupPointerOrAdd to
        // determine whether the VN was already in the map.
        bool* pValue = optAssertionVNsMap->LookupPointerOrAdd(vn, false);
        if (!*pValue)
        {
            *pValue = true;
            return false;
        }
        return true;
    }

    // Otherwise just do a normal lookup
    return (optAssertionVNsMap != nullptr) && optAssertionVNsMap->Lookup(vn);
}

#ifdef DEBUG
void Compiler::optDebugCheckAssertion(const AssertionDsc& assertion) const
{
    switch (assertion.GetOp1().GetKind())
    {
        case O1K_EXACT_TYPE:
        case O1K_SUBTYPE:
        case O1K_VN:
            assert(!optLocalAssertionProp);
            break;
        default:
            break;
    }

    switch (assertion.GetOp2().GetKind())
    {
        case O2K_SUBRANGE:
        case O2K_LCLVAR_COPY:
            assert(optLocalAssertionProp);
            break;

        case O2K_ZEROOBJ:
            // We only make these assertion for stores (not control flow).
            assert(assertion.KindIs(OAK_EQUAL));
            // We use "optLocalAssertionIsEqualOrNotEqual" to find these.
            break;

        case O2K_CONST_DOUBLE:
            assert(!FloatingPointUtils::isNaN(assertion.GetOp2().GetDoubleConstant()));
            break;

        default:
            // for all other 'assertion.GetOp2().GetKind()' values we don't check anything
            break;
    }
}

/*****************************************************************************
 *
 *  Verify that assertion prop related assumptions are valid. If "index"
 *  is 0 (i.e., NO_ASSERTION_INDEX) then verify all assertions in the table.
 *  If "index" is between 1 and optAssertionCount, then verify the assertion
 *  desc corresponding to "index."
 */
void Compiler::optDebugCheckAssertions(AssertionIndex index)
{
    AssertionIndex start = (index == NO_ASSERTION_INDEX) ? 1 : index;
    AssertionIndex end   = (index == NO_ASSERTION_INDEX) ? optAssertionCount : index;
    for (AssertionIndex ind = start; ind <= end; ++ind)
    {
        const AssertionDsc& assertion = optGetAssertion(ind);
        optDebugCheckAssertion(assertion);
    }
}
#endif

//------------------------------------------------------------------------
// optCreateComplementaryAssertion: Create an assertion that is the complementary
//     of the specified assertion.
//
// Arguments:
//    assertionIndex - the index of the assertion
//
// Notes:
//    The created complementary assertion is associated with the original
//    assertion such that it can be found by optFindComplementary.
//
void Compiler::optCreateComplementaryAssertion(AssertionIndex assertionIndex)
{
    if (assertionIndex == NO_ASSERTION_INDEX)
    {
        return;
    }

    const AssertionDsc& candidateAssertion = optGetAssertion(assertionIndex);
    if (candidateAssertion.KindIs(OAK_EQUAL))
    {
        // Don't create useless OAK_NOT_EQUAL assertions

        if (candidateAssertion.GetOp1().KindIs(O1K_LCLVAR, O1K_VN))
        {
            // "LCLVAR != CNS" is not a useful assertion (unless CNS is 0/1)
            if (candidateAssertion.GetOp2().KindIs(O2K_CONST_INT) &&
                (candidateAssertion.GetOp2().GetIntConstant() != 0) &&
                (candidateAssertion.GetOp2().GetIntConstant() != 1))
            {
                return;
            }

            // "LCLVAR != LCLVAR_COPY"
            if (candidateAssertion.GetOp2().KindIs(O2K_LCLVAR_COPY))
            {
                return;
            }
        }

        // "Object is not Class" is also not a useful assertion (at least for now)
        if (candidateAssertion.GetOp1().KindIs(O1K_EXACT_TYPE, O1K_SUBTYPE))
        {
            return;
        }
        AssertionDsc reversed = candidateAssertion.Reverse();
        optMapComplementary(optAddAssertion(reversed), assertionIndex);
    }
    else if (candidateAssertion.KindIs(OAK_LT_UN, OAK_LE_UN) &&
             candidateAssertion.GetOp2().KindIs(O2K_CHECKED_BOUND_ADD_CNS))
    {
        // Assertions such as "X > checkedBndVN" aren't very useful.
        return;
    }
    else if (AssertionDsc::IsReversible(candidateAssertion.GetKind()))
    {
        AssertionDsc reversed = candidateAssertion.Reverse();
        optMapComplementary(optAddAssertion(reversed), assertionIndex);
    }
}

//------------------------------------------------------------------------
// optCreateJtrueAssertions: Create assertions about a JTRUE's relop operands.
//
// Arguments:
//    op1    - the first assertion operand
//    op2    - the second assertion operand
//    equals - the assertion kind (equals / not equals)
//
// Return Value:
//    The new assertion index or NO_ASSERTION_INDEX if a new assertion
//    was not created.
//
// Notes:
//    Assertion creation may fail either because the provided assertion
//    operands aren't supported or because the assertion table is full.
//    If an assertion is created successfully then an attempt is made to also
//    create a second, complementary assertion. This may too fail, for the
//    same reasons as the first one.
//
AssertionIndex Compiler::optCreateJtrueAssertions(GenTree* op1, GenTree* op2, bool equals)
{
    AssertionIndex assertionIndex = optCreateAssertion(op1, op2, equals);
    // Don't bother if we don't have an assertion on the JTrue False path. Current implementation
    // allows for a complementary only if there is an assertion on the False path (tree->HasAssertion()).
    if (assertionIndex != NO_ASSERTION_INDEX)
    {
        optCreateComplementaryAssertion(assertionIndex);
    }
    return assertionIndex;
}

AssertionInfo Compiler::optCreateJTrueBoundsAssertion(GenTree* tree)
{
    // These assertions are VN based, so not relevant for local prop
    //
    if (optLocalAssertionProp)
    {
        return NO_ASSERTION_INDEX;
    }

    GenTree* relop = tree->gtGetOp1();
    if (!relop->OperIsCompare())
    {
        return NO_ASSERTION_INDEX;
    }

    ValueNum  relopVN = optConservativeNormalVN(relop);
    VNFuncApp relopFuncApp;
    if (!vnStore->GetVNFunc(relopVN, &relopFuncApp))
    {
        // We're expecting a relop here
        return NO_ASSERTION_INDEX;
    }

    bool isUnsignedRelop;
    if (relopFuncApp.FuncIs(VNF_LE, VNF_LT, VNF_GE, VNF_GT))
    {
        isUnsignedRelop = false;
    }
    else if (relopFuncApp.FuncIs(VNF_LE_UN, VNF_LT_UN, VNF_GE_UN, VNF_GT_UN))
    {
        isUnsignedRelop = true;
    }
    else
    {
        // Not a relop we're interested in.
        // Assertions for NE/EQ are handled elsewhere.
        return NO_ASSERTION_INDEX;
    }

    VNFunc   relopFunc = relopFuncApp.m_func;
    ValueNum op1VN     = relopFuncApp.m_args[0];
    ValueNum op2VN     = relopFuncApp.m_args[1];

    if ((genActualType(vnStore->TypeOfVN(op1VN)) != TYP_INT) || (genActualType(vnStore->TypeOfVN(op2VN)) != TYP_INT))
    {
        // For now, we don't have consumers for assertions derived from non-int32 comparisons
        return NO_ASSERTION_INDEX;
    }

    // "CheckedBnd <relop> X"
    if (!isUnsignedRelop && vnStore->IsVNCheckedBound(op1VN))
    {
        // Move the checked bound to the right side for simplicity
        relopFunc          = ValueNumStore::SwapRelop(relopFunc);
        AssertionDsc   dsc = AssertionDsc::CreateCompareCheckedBound(relopFunc, op2VN, op1VN, 0);
        AssertionIndex idx = optAddAssertion(dsc);
        optCreateComplementaryAssertion(idx);
        return idx;
    }

    // "X <relop> CheckedBnd"
    if (!isUnsignedRelop && vnStore->IsVNCheckedBound(op2VN))
    {
        AssertionDsc   dsc = AssertionDsc::CreateCompareCheckedBound(relopFunc, op1VN, op2VN, 0);
        AssertionIndex idx = optAddAssertion(dsc);
        optCreateComplementaryAssertion(idx);
        return idx;
    }

    // "(CheckedBnd + CNS) <relop> X"
    ValueNum checkedBnd;
    int      checkedBndCns;
    if (!isUnsignedRelop && vnStore->IsVNCheckedBoundAddConst(op1VN, &checkedBnd, &checkedBndCns))
    {
        // Move the (CheckedBnd + CNS) part to the right side for simplicity
        relopFunc          = ValueNumStore::SwapRelop(relopFunc);
        AssertionDsc   dsc = AssertionDsc::CreateCompareCheckedBound(relopFunc, op2VN, checkedBnd, checkedBndCns);
        AssertionIndex idx = optAddAssertion(dsc);
        optCreateComplementaryAssertion(idx);
        return idx;
    }

    // "X <relop> (CheckedBnd + CNS)"
    if (!isUnsignedRelop && vnStore->IsVNCheckedBoundAddConst(op2VN, &checkedBnd, &checkedBndCns))
    {
        AssertionDsc   dsc = AssertionDsc::CreateCompareCheckedBound(relopFunc, op1VN, checkedBnd, checkedBndCns);
        AssertionIndex idx = optAddAssertion(dsc);
        optCreateComplementaryAssertion(idx);
        return idx;
    }

    // Loop condition like "(uint)i < (uint)bnd" or equivalent
    // Assertion: "no throw" since this condition guarantees that i is both >= 0 and < bnd (on the appropriate edge)
    ValueNumStore::UnsignedCompareCheckedBoundInfo unsignedCompareBnd;
    if (vnStore->IsVNUnsignedCompareCheckedBound(relopVN, &unsignedCompareBnd))
    {
        ValueNum idxVN = vnStore->VNNormalValue(unsignedCompareBnd.vnIdx);
        ValueNum lenVN = vnStore->VNNormalValue(unsignedCompareBnd.vnBound);

        AssertionDsc   dsc   = AssertionDsc::CreateNoThrowArrBnd(idxVN, lenVN);
        AssertionIndex index = optAddAssertion(dsc);
        if (unsignedCompareBnd.cmpOper == VNF_GE_UN)
        {
            // By default JTRUE generated assertions hold on the "jump" edge. We have i >= bnd but we're really
            // after i < bnd so we need to change the assertion edge to "next".
            return AssertionInfo::ForNextEdge(index);
        }
        return index;
    }

    // Create "X relop CNS" assertion (both signed and unsigned relops)
    // Ignore non-positive constants for unsigned relops as they don't add any useful information.
    ssize_t cns;
    if (vnStore->IsVNIntegralConstant(op1VN, &cns) && (!isUnsignedRelop || (cns > 0)))
    {
        relopFunc          = ValueNumStore::SwapRelop(relopFunc);
        AssertionDsc   dsc = AssertionDsc::CreateConstantBound(this, relopFunc, op2VN, op1VN);
        AssertionIndex idx = optAddAssertion(dsc);
        optCreateComplementaryAssertion(idx);
        return idx;
    }

    if (vnStore->IsVNIntegralConstant(op2VN, &cns) && (!isUnsignedRelop || (cns > 0)))
    {
        AssertionDsc   dsc = AssertionDsc::CreateConstantBound(this, relopFunc, op1VN, op2VN);
        AssertionIndex idx = optAddAssertion(dsc);
        optCreateComplementaryAssertion(idx);
        return idx;
    }

    return NO_ASSERTION_INDEX;
}

/*****************************************************************************
 *
 *  Compute assertions for the JTrue node.
 */
AssertionInfo Compiler::optAssertionGenJtrue(GenTree* tree)
{
    GenTree* const relop = tree->AsOp()->gtOp1;
    if (!relop->OperIsCompare())
    {
        return NO_ASSERTION_INDEX;
    }

    AssertionInfo info = optCreateJTrueBoundsAssertion(tree);
    if (info.HasAssertion())
    {
        return info;
    }

    if (optLocalAssertionProp && !optCrossBlockLocalAssertionProp)
    {
        return NO_ASSERTION_INDEX;
    }

    // Find assertion kind.
    bool equals;
    switch (relop->gtOper)
    {
        case GT_EQ:
            equals = true;
            break;
        case GT_NE:
            equals = false;
            break;
        default:
            // TODO-CQ: add other relop operands. Disabled for now to measure perf
            // and not occupy assertion table slots. We'll add them when used.
            return NO_ASSERTION_INDEX;
    }

    // Look through any CSEs so we see the actual trees providing values, if possible.
    // This is important for exact type assertions, which need to see the GT_IND.
    //
    GenTree* op1 = relop->AsOp()->gtOp1->gtCommaStoreVal();
    GenTree* op2 = relop->AsOp()->gtOp2->gtCommaStoreVal();

    // Avoid creating local assertions for float types.
    //
    if (optLocalAssertionProp && varTypeIsFloating(op1))
    {
        return NO_ASSERTION_INDEX;
    }

    // See if we have IND(obj) ==/!= TypeHandle
    //
    if (!optLocalAssertionProp && op1->OperIs(GT_IND) && op1->gtGetOp1()->TypeIs(TYP_REF))
    {
        ValueNum objVN     = optConservativeNormalVN(op1->gtGetOp1());
        ValueNum typeHndVN = optConservativeNormalVN(op2);

        if ((objVN != ValueNumStore::NoVN) && vnStore->IsVNTypeHandle(typeHndVN))
        {
            AssertionDsc   dsc   = AssertionDsc::CreateSubtype(this, objVN, typeHndVN, /*exact*/ true);
            AssertionIndex index = optAddAssertion(dsc);

            // We don't need to create a complementary assertion here. We're only interested
            // in the assertion that the object is of a certain type. The opposite assertion
            // (that the object is not of a certain type) is not useful (at least not yet).
            //
            // So if we have "if (obj->pMT != CNS) then create the assertion for the "else" edge.
            if (relop->OperIs(GT_NE))
            {
                return AssertionInfo::ForNextEdge(index);
            }
            return index;
        }
    }

    // Check for op1 or op2 to be lcl var and if so, keep it in op1.
    if (!op1->OperIs(GT_LCL_VAR) && op2->OperIs(GT_LCL_VAR))
    {
        std::swap(op1, op2);
    }

    // If op1 is lcl and op2 is const or lcl, create assertion.
    if (op1->OperIs(GT_LCL_VAR) && (op2->OperIsConst() || op2->OperIs(GT_LCL_VAR))) // Fix for Dev10 851483
    {
        // Watch out for cases where long local(s) are implicitly truncated.
        //
        LclVarDsc* const lcl1Dsc = lvaGetDesc(op1->AsLclVarCommon());
        if (lcl1Dsc->TypeIs(TYP_LONG) && !op1->TypeIs(TYP_LONG))
        {
            return NO_ASSERTION_INDEX;
        }
        if (op2->OperIs(GT_LCL_VAR))
        {
            LclVarDsc* const lcl2Dsc = lvaGetDesc(op2->AsLclVarCommon());
            if (lcl2Dsc->TypeIs(TYP_LONG) && !op2->TypeIs(TYP_LONG))
            {
                return NO_ASSERTION_INDEX;
            }
        }

        return optCreateJtrueAssertions(op1, op2, equals);
    }
    else if (!optLocalAssertionProp)
    {
        ValueNum op1VN = vnStore->VNConservativeNormalValue(op1->gtVNPair);
        ValueNum op2VN = vnStore->VNConservativeNormalValue(op2->gtVNPair);

        if (vnStore->IsVNCheckedBound(op1VN) && vnStore->IsVNInt32Constant(op2VN))
        {
            assert(relop->OperIs(GT_EQ, GT_NE));
            return optCreateJtrueAssertions(op1, op2, equals);
        }
    }

    // Check op1 and op2 for an indirection of a GT_LCL_VAR and keep it in op1.
    if ((!op1->OperIs(GT_IND) || !op1->AsOp()->gtOp1->OperIs(GT_LCL_VAR)) &&
        (op2->OperIs(GT_IND) && op2->AsOp()->gtOp1->OperIs(GT_LCL_VAR)))
    {
        std::swap(op1, op2);
    }
    // If op1 is ind, then extract op1's oper.
    if (op1->OperIs(GT_IND) && op1->AsOp()->gtOp1->OperIs(GT_LCL_VAR))
    {
        return optCreateJtrueAssertions(op1, op2, equals);
    }

    // Look for a call to an IsInstanceOf helper compared to a nullptr
    if (!op2->OperIs(GT_CNS_INT) && op1->OperIs(GT_CNS_INT))
    {
        std::swap(op1, op2);
    }
    // Validate op1 and op2
    if (!op1->OperIs(GT_CALL) || !op1->AsCall()->IsHelperCall() || !op1->TypeIs(TYP_REF) || // op1
        !op2->OperIs(GT_CNS_INT) || (op2->AsIntCon()->gtIconVal != 0))                      // op2
    {
        return NO_ASSERTION_INDEX;
    }

    if (optLocalAssertionProp)
    {
        // O1K_SUBTYPE is Global Assertion Prop only
        return NO_ASSERTION_INDEX;
    }

    GenTreeCall* const call = op1->AsCall();

    // Note CORINFO_HELP_READYTORUN_ISINSTANCEOF does not have the same argument pattern.
    // In particular, it is not possible to deduce what class is being tested from its args.
    //
    // Also note The CASTCLASS helpers won't appear in predicates as they throw on failure.
    // So the helper list here is smaller than the one in optAssertionProp_Call.
    //
    CorInfoHelpFunc helper = eeGetHelperNum(call->gtCallMethHnd);
    if ((helper == CORINFO_HELP_ISINSTANCEOFINTERFACE) || (helper == CORINFO_HELP_ISINSTANCEOFARRAY) ||
        (helper == CORINFO_HELP_ISINSTANCEOFCLASS) || (helper == CORINFO_HELP_ISINSTANCEOFANY))
    {
        GenTree* objectNode      = call->gtArgs.GetUserArgByIndex(1)->GetNode();
        GenTree* methodTableNode = call->gtArgs.GetUserArgByIndex(0)->GetNode();

        // objectNode can be TYP_I_IMPL in case if it's a constant handle
        // (e.g. a string literal from frozen segments)
        //
        assert(objectNode->TypeIs(TYP_REF, TYP_I_IMPL));
        assert(methodTableNode->TypeIs(TYP_I_IMPL));

        ValueNum objVN     = optConservativeNormalVN(objectNode);
        ValueNum typeHndVN = optConservativeNormalVN(methodTableNode);

        if ((objVN != ValueNumStore::NoVN) && vnStore->IsVNTypeHandle(typeHndVN))
        {
            AssertionDsc   dsc   = AssertionDsc::CreateSubtype(this, objVN, typeHndVN, /*exact*/ false);
            AssertionIndex index = optAddAssertion(dsc);

            // We don't need to create a complementary assertion here. We're only interested
            // in the assertion that the object is of a certain type. The opposite assertion
            // (that the object is not of a certain type) is not useful (at least not yet).
            //
            // So if we have "if (ISINST(obj, pMT) == null) then create the assertion for the "else" edge.
            //
            if (relop->OperIs(GT_EQ))
            {
                return AssertionInfo::ForNextEdge(index);
            }
            return index;
        }
    }

    return NO_ASSERTION_INDEX;
}

/*****************************************************************************
 *
 *  If this node creates an assertion then assign an index to the assertion
 *  by adding it to the lookup table, if necessary.
 */
void Compiler::optAssertionGen(GenTree* tree)
{
    tree->ClearAssertion();

    // If there are QMARKs in the IR, we won't generate assertions
    // for conditionally executed code.
    //
    if (optLocalAssertionProp && ((tree->gtFlags & GTF_COLON_COND) != 0))
    {
        return;
    }

#ifdef DEBUG
    optAssertionPropCurrentTree = tree;
#endif

    AssertionInfo assertionInfo;
    switch (tree->OperGet())
    {
        case GT_STORE_LCL_VAR:
            // VN takes care of non local assertions for data flow.
            if (optLocalAssertionProp)
            {
                assertionInfo = optCreateAssertion(tree, tree->AsLclVar()->Data(), /*equals*/ true);
            }
            break;

        case GT_IND:
        case GT_XAND:
        case GT_XORR:
        case GT_XADD:
        case GT_XCHG:
        case GT_CMPXCHG:
        case GT_BLK:
        case GT_STOREIND:
        case GT_STORE_BLK:
        case GT_NULLCHECK:
        case GT_ARR_LENGTH:
        case GT_MDARR_LENGTH:
        case GT_MDARR_LOWER_BOUND:
            // These indirs (esp. GT_IND and GT_STOREIND) are the most popular sources of assertions.
            if (tree->IndirMayFault(this))
            {
                assertionInfo = optCreateAssertion(tree->GetIndirOrArrMetaDataAddr(), nullptr, /*equals*/ false);
            }
            break;

        case GT_INTRINSIC:
            if (tree->AsIntrinsic()->gtIntrinsicName == NI_System_Object_GetType)
            {
                assertionInfo = optCreateAssertion(tree->AsIntrinsic()->gtGetOp1(), nullptr, /*equals*/ false);
            }
            break;

        case GT_BOUNDS_CHECK:
            if (!optLocalAssertionProp)
            {
                ValueNum idxVN = optConservativeNormalVN(tree->AsBoundsChk()->GetIndex());
                ValueNum lenVN = optConservativeNormalVN(tree->AsBoundsChk()->GetArrayLength());
                if ((idxVN == ValueNumStore::NoVN) || (lenVN == ValueNumStore::NoVN))
                {
                    assertionInfo = NO_ASSERTION_INDEX;
                }
                else
                {
                    // GT_BOUNDS_CHECK node provides the following contract:
                    // * idxVN < lenVN
                    // * lenVN is non-negative
                    assertionInfo = optAddAssertion(AssertionDsc::CreateNoThrowArrBnd(idxVN, lenVN));
                }
            }
            break;

        case GT_ARR_ELEM:
            // An array element reference can create a non-null assertion
            assertionInfo = optCreateAssertion(tree->AsArrElem()->gtArrObj, nullptr, /*equals*/ false);
            break;

        case GT_CALL:
        {
            // A virtual call can create a non-null assertion. We transform some virtual calls into non-virtual calls
            // with a GTF_CALL_NULLCHECK flag set.
            // Ignore tail calls because they have 'this` pointer in the regular arg list and an implicit null check.
            GenTreeCall* const call = tree->AsCall();
            if (call->NeedsNullCheck() || (call->IsVirtual() && !call->IsTailCall()))
            {
                //  Retrieve the 'this' arg.
                GenTree* thisArg = call->gtArgs.GetThisArg()->GetNode();
                assert(thisArg != nullptr);
                assertionInfo = optCreateAssertion(thisArg, nullptr, /*equals*/ false);
            }
        }
        break;

        case GT_JTRUE:
            assertionInfo = optAssertionGenJtrue(tree);
            break;

        default:
            // All other gtOper node kinds, leave 'assertionIndex' = NO_ASSERTION_INDEX
            break;
    }

    if (assertionInfo.HasAssertion())
    {
        tree->SetAssertionInfo(assertionInfo);
    }
}

/*****************************************************************************
 *
 * Maps a complementary assertion to its original assertion so it can be
 * retrieved faster.
 */
void Compiler::optMapComplementary(AssertionIndex assertionIndex, AssertionIndex index)
{
    if (assertionIndex == NO_ASSERTION_INDEX || index == NO_ASSERTION_INDEX)
    {
        return;
    }

    assert(assertionIndex <= optMaxAssertionCount);
    assert(index <= optMaxAssertionCount);

    optComplementaryAssertionMap[assertionIndex] = index;
    optComplementaryAssertionMap[index]          = assertionIndex;
}

/*****************************************************************************
 *
 *  Given an assertion index, return the assertion index of the complementary
 *  assertion or 0 if one does not exist.
 */
AssertionIndex Compiler::optFindComplementary(AssertionIndex assertIndex)
{
    if (assertIndex == NO_ASSERTION_INDEX)
    {
        return NO_ASSERTION_INDEX;
    }
    const AssertionDsc& inputAssertion = optGetAssertion(assertIndex);

    // Must be an equal or not equal assertion.
    if (!AssertionDsc::IsReversible(inputAssertion.GetKind()))
    {
        return NO_ASSERTION_INDEX;
    }

    AssertionIndex index = optComplementaryAssertionMap[assertIndex];
    if (index != NO_ASSERTION_INDEX && index <= optAssertionCount)
    {
        return index;
    }

    for (AssertionIndex index = 1; index <= optAssertionCount; ++index)
    {
        // Make sure assertion kinds are complementary and op1, op2 kinds match.
        const AssertionDsc& curAssertion = optGetAssertion(index);
        if (curAssertion.Complementary(inputAssertion, !optLocalAssertionProp))
        {
            optMapComplementary(assertIndex, index);
            return index;
        }
    }
    return NO_ASSERTION_INDEX;
}

//------------------------------------------------------------------------
// optAssertionIsSubrange: Find a subrange assertion for the given range and tree.
//
// This function will return the index of the first assertion in "assertions"
// which claims that the value of "tree" is within the bounds of the provided
// "range" (i. e. "range.Contains(assertedRange)").
//
// Arguments:
//    tree       - the tree for which to find the assertion
//    range      - range the subrange of which to look for
//    assertions - the set of assertions
//
// Return Value:
//    Index of the found assertion, NO_ASSERTION_INDEX otherwise.
//
AssertionIndex Compiler::optAssertionIsSubrange(GenTree* tree, IntegralRange range, ASSERT_VALARG_TP assertions)
{
    assert(optLocalAssertionProp); // Subrange assertions are local only.
    if (!optCanPropSubRange)
    {
        return NO_ASSERTION_INDEX;
    }

    BitVecOps::Iter iter(apTraits, assertions);
    unsigned        bvIndex = 0;
    while (iter.NextElem(&bvIndex))
    {
        AssertionIndex const index        = GetAssertionIndex(bvIndex);
        const AssertionDsc&  curAssertion = optGetAssertion(index);
        if (curAssertion.CanPropSubRange())
        {
            if (curAssertion.GetOp1().GetLclNum() != tree->AsLclVarCommon()->GetLclNum())
            {
                continue;
            }

            if (range.Contains(curAssertion.GetOp2().GetIntegralRange()))
            {
                return index;
            }
        }
    }

    return NO_ASSERTION_INDEX;
}

/**********************************************************************************
 *
 * Given a "tree" that is usually arg1 of a isinst/cast kind of GT_CALL (a class
 * handle), and "methodTableArg" which is a const int (a class handle), then search
 * if there is an assertion in "assertions", that asserts the equality of the two
 * class handles and then returns the index of the assertion. If one such assertion
 * could not be found, then it returns NO_ASSERTION_INDEX.
 *
 */
AssertionIndex Compiler::optAssertionIsSubtype(GenTree* tree, GenTree* methodTableArg, ASSERT_VALARG_TP assertions)
{
    BitVecOps::Iter iter(apTraits, assertions);
    unsigned        bvIndex = 0;
    while (iter.NextElem(&bvIndex))
    {
        AssertionIndex const index        = GetAssertionIndex(bvIndex);
        const AssertionDsc&  curAssertion = optGetAssertion(index);
        if (!curAssertion.KindIs(OAK_EQUAL) || !curAssertion.GetOp1().KindIs(O1K_SUBTYPE, O1K_EXACT_TYPE))
        {
            // TODO-CQ: We might benefit from OAK_NOT_EQUAL assertion as well, e.g.:
            // if (obj is not MyClass) // obj is known to be never of MyClass class
            // {
            //     if (obj is MyClass) // can be folded to false
            //     {
            //
            continue;
        }

        if ((curAssertion.GetOp1().GetVN() != vnStore->VNConservativeNormalValue(tree->gtVNPair) ||
             !curAssertion.GetOp2().KindIs(O2K_CONST_INT)))
        {
            continue;
        }

        ssize_t      methodTableVal = 0;
        GenTreeFlags iconFlags      = GTF_EMPTY;
        if (!optIsTreeKnownIntValue(!optLocalAssertionProp, methodTableArg, &methodTableVal, &iconFlags))
        {
            continue;
        }

        if (curAssertion.GetOp2().GetIntConstant() == methodTableVal)
        {
            // TODO-CQ: if they don't match, we might still be able to prove that the result is foldable via
            // compareTypesForCast.
            return index;
        }
    }
    return NO_ASSERTION_INDEX;
}


//------------------------------------------------------------------------------
// optAssertionPropMain: assertion propagation phase
//
// Returns:
//    Suitable phase status.
//
PhaseStatus Compiler::optAssertionPropMain()
{
    if (fgSsaPassesCompleted == 0)
    {
        return PhaseStatus::MODIFIED_NOTHING;
    }

    optAssertionInit(false);

    noway_assert(optAssertionCount == 0);
    bool madeChanges = false;

    // Assertion prop can speculatively create trees.
    INDEBUG(const unsigned baseTreeID = compGenTreeID);

    // First discover all assertions and record them in the table.
    ArrayStack<BasicBlock*> switchBlocks(getAllocator(CMK_AssertionProp));
    for (BasicBlock* const block : Blocks())
    {
        compCurBB           = block;
        fgRemoveRestOfBlock = false;

        Statement* stmt = block->firstStmt();
        while (stmt != nullptr)
        {
            // We need to remove the rest of the block.
            if (fgRemoveRestOfBlock)
            {
                fgRemoveStmt(block, stmt);
                stmt        = stmt->GetNextStmt();
                madeChanges = true;
                continue;
            }
            else
            {
                // Perform VN based assertion prop before assertion gen.
                Statement* nextStmt = optVNAssertionPropCurStmt(block, stmt);
                madeChanges |= optAssertionPropagatedCurrentStmt;
                INDEBUG(madeChanges |= (baseTreeID != compGenTreeID));

                // Propagation resulted in removal of the remaining stmts, perform it.
                if (fgRemoveRestOfBlock)
                {
                    stmt = stmt->GetNextStmt();
                    continue;
                }

                // Propagation removed the current stmt or next few stmts, so skip them.
                if (stmt != nextStmt)
                {
                    stmt = nextStmt;
                    continue;
                }
            }

            // Perform assertion gen for control flow based assertions.
            for (GenTree* const tree : stmt->TreeList())
            {
                optAssertionGen(tree);
            }

            // Advance the iterator
            stmt = stmt->GetNextStmt();
        }

        if (block->KindIs(BBJ_SWITCH))
        {
            switchBlocks.Push(block);
        }
    }

    for (int i = 0; i < switchBlocks.Height(); i++)
    {
        madeChanges |= optCreateJumpTableImpliedAssertions(switchBlocks.Bottom(i));
    }

    if (optAssertionCount == 0)
    {
        // Zero out the bbAssertionIn values, as these can be referenced in RangeCheck::MergeAssertion
        // and this is sharedstate with the CSE phase: bbCseIn
        //
        for (BasicBlock* const block : Blocks())
        {
            block->bbAssertionIn = BitVecOps::MakeEmpty(apTraits);
        }
        return madeChanges ? PhaseStatus::MODIFIED_EVERYTHING : PhaseStatus::MODIFIED_NOTHING;
    }

#ifdef DEBUG
    fgDebugCheckLinks();
#endif

    // Allocate the bits for the predicate sensitive dataflow analysis
    bbJtrueAssertionOut    = optInitAssertionDataflowFlags();
    ASSERT_TP* jumpDestGen = optComputeAssertionGen();

    // Modified dataflow algorithm for available expressions.
    DataFlow                  flow(this);
    AssertionPropFlowCallback ap(this, bbJtrueAssertionOut, jumpDestGen);
    if (ap.VerboseDataflow())
    {
        JITDUMP("AssertionPropFlowCallback:\n\n")
    }
    flow.ForwardAnalysis(ap);

#ifdef DEBUG
    if (verbose)
    {
        for (BasicBlock* const block : Blocks())
        {
            printf(FMT_BB ":\n", block->bbNum);
            optDumpAssertionIndices(" in   = ", block->bbAssertionIn, "\n");
            optDumpAssertionIndices(" out  = ", block->bbAssertionOut, "\n");
            if (block->KindIs(BBJ_COND))
            {
                printf(" " FMT_BB " = ", block->GetTrueTarget()->bbNum);
                optDumpAssertionIndices(bbJtrueAssertionOut[block->bbNum], "\n");
            }
        }
        printf("\n");
    }
#endif // DEBUG

    ASSERT_TP assertions = BitVecOps::MakeEmpty(apTraits);

    // Perform assertion propagation (and constant folding)
    for (BasicBlock* const block : Blocks())
    {
        BitVecOps::Assign(apTraits, assertions, block->bbAssertionIn);

        // TODO-Review: EH successor/predecessor iteration seems broken.
        // SELF_HOST_TESTS_ARM\jit\Directed\ExcepFilters\fault\fault.exe
        if (block->bbCatchTyp == BBCT_FAULT)
        {
            continue;
        }

        // Make the current basic block address available globally.
        compCurBB           = block;
        fgRemoveRestOfBlock = false;

        // Walk the statement trees in this basic block
        Statement* stmt = block->FirstNonPhiDef();
        while (stmt != nullptr)
        {
            // Propagation tells us to remove the rest of the block. Remove it.
            if (fgRemoveRestOfBlock)
            {
                fgRemoveStmt(block, stmt);
                stmt        = stmt->GetNextStmt();
                madeChanges = true;
                continue;
            }

            // Preserve the prev link before the propagation and morph, to check if propagation
            // removes the current stmt.
            Statement* prevStmt = (stmt == block->firstStmt()) ? nullptr : stmt->GetPrevStmt();

            optAssertionPropagatedCurrentStmt = false; // set to true if a assertion propagation took place
                                                       // and thus we must morph, set order, re-link
            for (GenTree* tree = stmt->GetTreeList(); tree != nullptr; tree = tree->gtNext)
            {
                optDumpAssertionIndices("Propagating ", assertions, " ");
                JITDUMP("for " FMT_BB ", stmt " FMT_STMT ", tree [%06d]", block->bbNum, stmt->GetID(), dspTreeID(tree));
                JITDUMP(", tree -> ");
                JITDUMPEXEC(optPrintAssertionIndex(tree->GetAssertionInfo().GetAssertionIndex()));
                JITDUMP("\n");

                GenTree* newTree = optAssertionProp(assertions, tree, stmt, block);
                if (newTree)
                {
                    assert(optAssertionPropagatedCurrentStmt == true);
                    tree = newTree;
                }

                // If this tree makes an assertion - make it available.
                if (tree->GeneratesAssertion())
                {
                    AssertionInfo info = tree->GetAssertionInfo();
                    BitVecOps::AddElemD(apTraits, assertions, info.GetAssertionIndex() - 1);
                }
            }

            if (optAssertionPropagatedCurrentStmt)
            {
#ifdef DEBUG
                if (verbose)
                {
                    printf("Re-morphing this stmt:\n");
                    gtDispStmt(stmt);
                    printf("\n");
                }
#endif
                // Re-morph the statement.
                fgMorphBlockStmt(block, stmt DEBUGARG("optAssertionPropMain"));
                madeChanges = true;
            }

            // Check if propagation removed statements starting from current stmt.
            // If so, advance to the next good statement.
            Statement* nextStmt = (prevStmt == nullptr) ? block->firstStmt() : prevStmt->GetNextStmt();
            stmt                = (stmt == nextStmt) ? stmt->GetNextStmt() : nextStmt;
        }
        optAssertionPropagatedCurrentStmt = false; // clear it back as we are done with stmts.
    }

    return madeChanges ? PhaseStatus::MODIFIED_EVERYTHING : PhaseStatus::MODIFIED_NOTHING;
}
