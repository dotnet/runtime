// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "jitpch.h"

//------------------------------------------------------------------------
// optRedundantBranches: try and optimize redundant branches in the method
//
// Returns:
//   PhaseStatus indicating if anything changed.
//
PhaseStatus Compiler::optRedundantBranches()
{

#if DEBUG
    if (verbose)
    {
        fgDispBasicBlocks(verboseTrees);
    }
#endif // DEBUG

    class OptRedundantBranchesDomTreeVisitor : public DomTreeVisitor<OptRedundantBranchesDomTreeVisitor>
    {
    public:
        bool madeChanges;

        OptRedundantBranchesDomTreeVisitor(Compiler* compiler)
            : DomTreeVisitor(compiler)
            , madeChanges(false)
        {
        }

        void PreOrderVisit(BasicBlock* block)
        {
        }

        void PostOrderVisit(BasicBlock* block)
        {
            // Skip over any removed blocks.
            //
            if (block->HasFlag(BBF_REMOVED))
            {
                return;
            }

            // We currently can optimize some BBJ_CONDs.
            //
            if (block->KindIs(BBJ_COND))
            {
                bool madeChangesThisBlock = m_compiler->optRedundantRelop(block);

                BasicBlock* const bbFalse = block->GetFalseTarget();
                BasicBlock* const bbTrue  = block->GetTrueTarget();

                madeChangesThisBlock |= m_compiler->optRedundantBranch(block);

                if (block->KindIs(BBJ_COND))
                {
                    madeChangesThisBlock |= m_compiler->optRedundantDominatingBranch(block);
                }

                // If we modified some flow out of block but it's still referenced and
                // a BBJ_COND, retry; perhaps one of the later optimizations
                // we can do has enabled one of the earlier optimizations.
                //
                if (madeChangesThisBlock && block->KindIs(BBJ_COND) && (block->countOfInEdges() > 0))
                {
                    JITDUMP("Will retry RBO in " FMT_BB " after partial optimization\n", block->bbNum);
                    madeChangesThisBlock |= m_compiler->optRedundantBranch(block);
                }

                // It's possible that the changed flow into bbFalse or bbTrue may unblock
                // further optimizations there.
                //
                // Note this misses cascading retries, consider reworking the overall
                // strategy here to iterate until closure.
                //
                if (madeChangesThisBlock && (bbFalse->countOfInEdges() == 0))
                {
                    for (BasicBlock* succ : bbFalse->Succs())
                    {
                        JITDUMP("Will retry RBO in " FMT_BB "; pred " FMT_BB " now unreachable\n", succ->bbNum,
                                bbFalse->bbNum);
                        m_compiler->optRedundantBranch(succ);
                    }
                }

                if (madeChangesThisBlock && (bbTrue->countOfInEdges() == 0))
                {
                    for (BasicBlock* succ : bbTrue->Succs())
                    {
                        JITDUMP("Will retry RBO in " FMT_BB "; pred " FMT_BB " now unreachable\n", succ->bbNum,
                                bbTrue->bbNum);
                        m_compiler->optRedundantBranch(succ);
                    }
                }

                madeChanges |= madeChangesThisBlock;
            }
        }
    };

    optReachableBitVecTraits = nullptr;
    OptRedundantBranchesDomTreeVisitor visitor(this);
    visitor.WalkTree(m_domTree);

#if DEBUG
    if (verbose && visitor.madeChanges)
    {
        fgDispBasicBlocks(verboseTrees);
    }
#endif // DEBUG

    // DFS tree is always considered invalid after RBO.
    fgInvalidateDfsTree();

    return visitor.madeChanges ? PhaseStatus::MODIFIED_EVERYTHING : PhaseStatus::MODIFIED_NOTHING;
}

static const ValueNumStore::VN_RELATION_KIND s_vnRelations[] = {ValueNumStore::VN_RELATION_KIND::VRK_Same,
                                                                ValueNumStore::VN_RELATION_KIND::VRK_Reverse,
                                                                ValueNumStore::VN_RELATION_KIND::VRK_Swap,
                                                                ValueNumStore::VN_RELATION_KIND::VRK_SwapReverse};

//------------------------------------------------------------------------
// RelopImplicationRule
//
// A rule allowing inference between two otherwise unrelated relops.
// Related relops are handled via s_vnRelations above.
//
struct RelopImplicationRule
{
    VNFunc domRelop;
    bool   canInferFromTrue;
    bool   canInferFromFalse;
    VNFunc treeRelop;
    bool   reverse;
};

enum RelopResult
{
    Unknown,
    AlwaysFalse,
    AlwaysTrue
};

//------------------------------------------------------------------------
// IsCmp2ImpliedByCmp1: given two constant range checks:
//
//   if (X oper1 bound1)
//   {
//        if (X oper2 bound2)
//        {
//
// determine if the second range check is implied by the dominating first one.
//
// Arguments:
//   oper1  - the first comparison operator
//   bound1 - the first constant bound
//   oper2  - the second comparison operator
//   bound2 - the second constant bound
//
// Returns:
//   Unknown     - the second check is not implied by the first one
//   AlwaysFalse - the second check is implied by the first one and is always false
//   AlwaysTrue  - the second check is implied by the first one and is always true
//
RelopResult IsCmp2ImpliedByCmp1(genTreeOps oper1, target_ssize_t bound1, genTreeOps oper2, target_ssize_t bound2)
{
    struct IntegralRange
    {
        target_ssize_t startIncl; // inclusive
        target_ssize_t endIncl;   // inclusive

        bool Intersects(const IntegralRange other) const
        {
            return (startIncl <= other.endIncl) && (other.startIncl <= endIncl);
        }

        bool Contains(const IntegralRange other) const
        {
            return (startIncl <= other.startIncl) && (other.endIncl <= endIncl);
        }
    };

    constexpr target_ssize_t minValue = TARGET_POINTER_SIZE == 4 ? INT32_MIN : INT64_MIN;
    constexpr target_ssize_t maxValue = TARGET_POINTER_SIZE == 4 ? INT32_MAX : INT64_MAX;

    // Start with the widest possible ranges
    IntegralRange range1 = {minValue, maxValue};
    IntegralRange range2 = {minValue, maxValue};

    // Update ranges based on inputs
    auto setRange = [](genTreeOps oper, target_ssize_t bound, IntegralRange* range) -> bool {
        switch (oper)
        {
            case GT_LT:
                // x < cns -> [minValue, cns - 1]
                if (bound == minValue)
                {
                    // overflows
                    return false;
                }
                range->endIncl = bound - 1;
                return true;

            case GT_LE:
                // x <= cns -> [minValue, cns]
                range->endIncl = bound;
                return true;

            case GT_GT:
                // x > cns -> [cns + 1, maxValue]
                if (bound == maxValue)
                {
                    // overflows
                    return false;
                }
                range->startIncl = bound + 1;
                return true;

            case GT_GE:
                // x >= cns -> [cns, maxValue]
                range->startIncl = bound;
                return true;

            case GT_EQ:
            case GT_NE:
                // x == cns -> [cns, cns]
                // NE is special-cased below
                range->startIncl = bound;
                range->endIncl   = bound;
                return true;

            default:
                // unsupported operator
                return false;
        }
    };

    if (setRange(oper1, bound1, &range1) && setRange(oper2, bound2, &range2))
    {
        // Special handling of GT_NE:
        if ((oper1 == GT_NE) || (oper2 == GT_NE))
        {
            // if (x != 100)
            //    if (x != 100) // always true
            if (oper1 == oper2)
            {
                return bound1 == bound2 ? RelopResult::AlwaysTrue : RelopResult::Unknown;
            }

            // if (x == 100)
            //    if (x != 100) // always false
            //
            // if (x == 100)
            //    if (x != 101) // always true
            if (oper1 == GT_EQ)
            {
                return bound1 == bound2 ? RelopResult::AlwaysFalse : RelopResult::AlwaysTrue;
            }

            // if (x > 100)
            //    if (x != 10) // always true
            if ((oper2 == GT_NE) && !range1.Intersects(range2))
            {
                return AlwaysTrue;
            }

            return RelopResult::Unknown;
        }

        // If ranges never intersect, then the 2nd range is never "true"
        if (!range1.Intersects(range2))
        {
            // E.g.:
            //
            // range1: [100 .. SSIZE_T_MAX]
            // range2: [SSIZE_T_MIN ..  10]
            //
            // or in other words:
            //
            // if (x >= 100)
            //    if (x <= 10) // always false
            //
            return RelopResult::AlwaysFalse;
        }

        // If range1 is a subset of range2, then the 2nd range is always "true"
        if (range2.Contains(range1))
        {
            // E.g.:
            //
            // range1: [100 .. SSIZE_T_MAX]
            // range2: [10  .. SSIZE_T_MAX]
            //
            // or in other words:
            //
            // if (x >= 100)
            //    if (x >= 10) // always true
            //
            return RelopResult::AlwaysTrue;
        }
    }
    return RelopResult::Unknown;
}

//------------------------------------------------------------------------
// s_implicationRules: rule table for unrelated relops
//
// clang-format off
//
#define V(x) (VNFunc)GT_ ## x
#define U(x) VNF_ ## x ## _UN

static const RelopImplicationRule s_implicationRules[] =
{
    // EQ
    {V(EQ),  true, false, V(GE), false},
    {V(EQ),  true, false, V(LE), false},
    {V(EQ),  true, false, V(GT),  true},
    {V(EQ),  true, false, U(GT),  true},
    {V(EQ),  true, false, V(LT),  true},
    {V(EQ),  true, false, U(LT),  true},

    // NE
    {V(NE), false,  true, V(GE),  true},
    {V(NE), false,  true, V(LE),  true},
    {V(NE), false,  true, V(GT), false},
    {V(NE), false,  true, U(GT), false},
    {V(NE), false,  true, V(LT), false},
    {V(NE), false,  true, U(LT), false},

    // LE
    {V(LE), false,  true, V(EQ), false},
    {V(LE), false,  true, V(NE),  true},
    {V(LE), false,  true, V(GE),  true},
    {V(LE), false,  true, V(LT), false},

    // LE_UN
    {U(LE), false,  true, V(EQ), false},
    {U(LE), false,  true, V(NE),  true},
    {U(LE), false,  true, U(GE),  true},
    {U(LE), false,  true, U(LT), false},

    // GT
    {V(GT),  true, false, V(EQ),  true},
    {V(GT),  true, false, V(NE), false},
    {V(GT),  true, false, V(GE), false},
    {V(GT),  true, false, V(LT),  true},

    // GT_UN
    {U(GT),  true, false, V(EQ),  true},
    {U(GT),  true, false, V(NE), false},
    {U(GT),  true, false, U(GE), false},
    {U(GT),  true, false, U(LT),  true},

    // GE
    {V(GE), false,  true, V(EQ), false},
    {V(GE), false,  true, V(NE),  true},
    {V(GE), false,  true, V(LE),  true},
    {V(GE), false,  true, V(GT), false},

    // GE_UN
    {U(GE), false,  true, V(EQ), false},
    {U(GE), false,  true, V(NE),  true},
    {U(GE), false,  true, U(LE),  true},
    {U(GE), false,  true, U(GT), false},

    // LT
    {V(LT),  true, false, V(EQ),  true},
    {V(LT),  true, false, V(NE), false},
    {V(LT),  true, false, V(LE), false},
    {V(LT),  true, false, V(GT),  true},

    // LT_UN
    {U(LT),  true, false, V(EQ),  true},
    {U(LT),  true, false, V(NE), false},
    {U(LT),  true, false, U(LE), false},
    {U(LT),  true, false, U(GT),  true},
};
// clang-format on

//------------------------------------------------------------------------
// optRelopImpliesRelop: determine if a dominating relop implies the value
//   of another relop.
//
// Arguments:
//   rii - struct with relop implication information
//
// Returns:
//   No return value.
//   Sets rii->canInfer and other fields, if inference is possible.
//
// Notes:
//
// First looks for exact or similar relations.
//
// If that fails, then looks for cases where the user or optOptimizeBools
// has combined two distinct predicates with a boolean AND, OR, or has wrapped
// a predicate in NOT.
//
// This will be expressed as  {NE/EQ}({AND/OR/NOT}(...), 0).
// If the operator is EQ then a true {AND/OR} result implies
// a false taken branch, so we need to invert the sense of our
// inferences.
//
// We can also partially infer the tree relop's value from other
// dominating relops, for example, (x >= 0) dominating (x > 0).
//
// We don't get all the cases here we could. Still to do:
// * two unsigned compares, same operands
// * mixture of signed/unsigned compares, same operands
//
void Compiler::optRelopImpliesRelop(RelopImplicationInfo* rii)
{
    assert(!rii->canInfer);

    // Look for related VNs
    //
    for (auto vnRelation : s_vnRelations)
    {
        const ValueNum relatedVN = vnStore->GetRelatedRelop(rii->domCmpNormVN, vnRelation);
        if ((relatedVN != ValueNumStore::NoVN) && (relatedVN == rii->treeNormVN))
        {
            rii->canInfer     = true;
            rii->vnRelation   = vnRelation;
            rii->reverseSense = (rii->vnRelation == ValueNumStore::VN_RELATION_KIND::VRK_Reverse) ||
                                (rii->vnRelation == ValueNumStore::VN_RELATION_KIND::VRK_SwapReverse);
            return;
        }
    }

    // VNs are not directly related. See if dominating
    // compare encompasses a related VN.
    //
    VNFuncApp domApp;
    if (!vnStore->GetVNFunc(rii->domCmpNormVN, &domApp))
    {
        return;
    }

    // Exclude floating point relops.
    //
    if (varTypeIsFloating(vnStore->TypeOfVN(domApp.m_args[0])))
    {
        return;
    }

#ifdef DEBUG
    static ConfigMethodRange JitEnableRboRange;
    JitEnableRboRange.EnsureInit(JitConfig.JitEnableRboRange());
    const unsigned hash    = impInlineRoot()->info.compMethodHash();
    const bool     inRange = JitEnableRboRange.Contains(hash);
#else
    const bool inRange = true;
#endif

    // If the dominating compare has the form R(x,y), see if tree compare has the
    // form R*(x,y) or R*(y,x) where we can infer R* from R.
    //
    VNFunc const domFunc = domApp.m_func;
    VNFuncApp    treeApp;
    if (inRange && ValueNumStore::VNFuncIsComparison(domFunc) && vnStore->GetVNFunc(rii->treeNormVN, &treeApp))
    {
        if (((treeApp.m_args[0] == domApp.m_args[0]) && (treeApp.m_args[1] == domApp.m_args[1])) ||
            ((treeApp.m_args[0] == domApp.m_args[1]) && (treeApp.m_args[1] == domApp.m_args[0])))
        {
            const bool swapped = (treeApp.m_args[0] == domApp.m_args[1]);

            VNFunc const treeFunc = treeApp.m_func;
            VNFunc       domFunc1 = domFunc;

            if (swapped)
            {
                domFunc1 = ValueNumStore::SwapRelop(domFunc);
            }

            for (const RelopImplicationRule& rule : s_implicationRules)
            {
                if ((rule.domRelop == domFunc1) && (rule.treeRelop == treeFunc))
                {
                    rii->canInfer          = true;
                    rii->vnRelation        = ValueNumStore::VN_RELATION_KIND::VRK_Inferred;
                    rii->canInferFromTrue  = rule.canInferFromTrue;
                    rii->canInferFromFalse = rule.canInferFromFalse;
                    rii->reverseSense      = rule.reverse;

                    JITDUMP("Can infer %s from [%s] dominating %s\n", ValueNumStore::VNFuncName(treeFunc),
                            rii->canInferFromTrue ? "true" : "false", ValueNumStore::VNFuncName(domFunc));
                    return;
                }
            }
        }

        if (((treeApp.m_args[0] == domApp.m_args[0]) || (treeApp.m_args[0] == domApp.m_args[1]) ||
             (treeApp.m_args[1] == domApp.m_args[0]) || (treeApp.m_args[1] == domApp.m_args[1])) &&
            optRelopTryInferWithOneEqualOperand(domApp, treeApp, rii))
        {
            return;
        }
    }

    // See if dominating compare is a compound comparison that might
    // tell us the value of the tree compare.
    //
    // Look for {EQ,NE}({AND,OR,NOT}, 0)
    //
    genTreeOps const oper = genTreeOps(domFunc);
    if (!GenTree::StaticOperIs(oper, GT_EQ, GT_NE))
    {
        return;
    }

    if (domApp.m_args[1] != vnStore->VNZeroForType(TYP_INT))
    {
        return;
    }

    const ValueNum predVN = domApp.m_args[0];
    VNFuncApp      predFuncApp;
    if (!vnStore->GetVNFunc(predVN, &predFuncApp))
    {
        return;
    }

    genTreeOps const predOper = genTreeOps(predFuncApp.m_func);

    if (!GenTree::StaticOperIs(predOper, GT_AND, GT_OR, GT_NOT))
    {
        return;
    }

    // Dominating compare is {EQ,NE}({AND,OR,NOT}, 0).
    //
    // See if one of {AND,OR,NOT} operands is related.
    //
    for (unsigned int i = 0; (i < predFuncApp.m_arity) && !rii->canInfer; i++)
    {
        ValueNum pVN = predFuncApp.m_args[i];

        for (auto vnRelation : s_vnRelations)
        {
            const ValueNum relatedVN = vnStore->GetRelatedRelop(pVN, vnRelation);

            if ((relatedVN != ValueNumStore::NoVN) && (relatedVN == rii->treeNormVN))
            {
                rii->vnRelation = vnRelation;
                rii->canInfer   = true;

                // If dom predicate is wrapped in EQ(*,0) then a true dom
                // predicate implies a false branch outcome, and vice versa.
                //
                rii->reverseSense = (rii->vnRelation == ValueNumStore::VN_RELATION_KIND::VRK_Reverse) ||
                                    (rii->vnRelation == ValueNumStore::VN_RELATION_KIND::VRK_SwapReverse);

                // We only get partial knowledge in the AND/OR cases.
                //
                //   AND(p1,p2) = true  ==> both p1 and p2 must be true
                //   AND(p1,p2) = false ==> don't know p1 or p2
                //    OR(p1,p2) = true  ==> don't know p1 or p2
                //    OR(p1,p2) = false ==> both p1 and p2 must be false
                //
                if (predOper == GT_AND)
                {
                    // EQ(AND, 0) false ==> AND true ==> AND operands true
                    rii->canInferFromFalse = (oper == GT_EQ);
                    // NE(AND, 0) true ==> AND true ==> AND operands true
                    rii->canInferFromTrue = (oper == GT_NE);
                    rii->reverseSense ^= (oper == GT_EQ);
                }
                else if (predOper == GT_OR)
                {
                    // NE(OR, 0) false ==> OR false ==> OR operands false
                    rii->canInferFromFalse = (oper == GT_NE);
                    // EQ(OR, 0) true ==> OR false ==> OR operands false
                    rii->canInferFromTrue = (oper == GT_EQ);
                    rii->reverseSense ^= (oper == GT_EQ);
                }
                else
                {
                    assert(predOper == GT_NOT);
                    // NE(NOT(x), 0) ==> NOT(X)
                    // EQ(NOT(x), 0) ==> X
                    rii->canInferFromTrue  = true;
                    rii->canInferFromFalse = true;
                    rii->reverseSense ^= (oper == GT_NE);
                }

                JITDUMP("Inferring predicate value from %s\n", GenTree::OpName(predOper));
                return;
            }
        }
    }
}

//------------------------------------------------------------------------
// optRelopTryInferWithOneEqualOperand: Given a dominating relop R(x, y) and
// another relop R*(a, b) that share an operand, try to see if we can infer
// something about R*(a, b).
//
// Arguments:
//   domApp  - The dominating relop R(x, y)
//   treeApp - The dominated relop R*(a, b)
//   rii     - [out] struct with relop implication information
//
// Returns:
//   True if something was inferred; otherwise false.
//
bool Compiler::optRelopTryInferWithOneEqualOperand(const VNFuncApp&      domApp,
                                                   const VNFuncApp&      treeApp,
                                                   RelopImplicationInfo* rii)
{
    // Canonicalize constants to be on the right.
    VNFunc   domFunc = domApp.m_func;
    ValueNum domOp1  = domApp.m_args[0];
    ValueNum domOp2  = domApp.m_args[1];

    VNFunc   treeFunc = treeApp.m_func;
    ValueNum treeOp1  = treeApp.m_args[0];
    ValueNum treeOp2  = treeApp.m_args[1];

    if (vnStore->IsVNConstant(domOp1))
    {
        std::swap(domOp1, domOp2);
        domFunc = ValueNumStore::SwapRelop(domFunc);
    }

    if (vnStore->IsVNConstant(treeOp1))
    {
        std::swap(treeOp1, treeOp2);
        treeFunc = ValueNumStore::SwapRelop(treeFunc);
    }

    // Given R(x, cns1) and R*(x, cns2) see if we can infer R* from R.
    if ((treeOp1 != domOp1) || !vnStore->IsVNConstant(treeOp2) || !vnStore->IsVNConstant(domOp2))
    {
        return false;
    }

    var_types treeOp1Type = vnStore->TypeOfVN(treeOp1);
    var_types treeOp2Type = vnStore->TypeOfVN(treeOp2);
    var_types domOp1Type  = vnStore->TypeOfVN(domOp1);
    var_types domOp2Type  = vnStore->TypeOfVN(domOp2);
    if (!varTypeIsIntOrI(treeOp1Type) || (domOp1Type != treeOp2Type) || (domOp2Type != treeOp2Type))
    {
        return false;
    }
    // We currently don't handle VNF_relop_UN funcs here
    if (!ValueNumStore::VNFuncIsSignedComparison(domFunc) || !ValueNumStore::VNFuncIsSignedComparison(treeFunc))
    {
        return false;
    }

    // Dominating "X relop CNS"
    const genTreeOps     domOper = static_cast<genTreeOps>(domFunc);
    const target_ssize_t domCns  = vnStore->CoercedConstantValue<target_ssize_t>(domOp2);

    // Dominated "X relop CNS"
    const genTreeOps     treeOper = static_cast<genTreeOps>(treeFunc);
    const target_ssize_t treeCns  = vnStore->CoercedConstantValue<target_ssize_t>(treeOp2);

    // Example:
    //
    // void Test(int x)
    // {
    //     if (x > 100)
    //         if (x > 10)
    //             Console.WriteLine("Taken!");
    // }
    //

    // Corresponding BB layout:
    //
    // BB1:
    //   if (x <= 100)
    //       goto BB4
    //
    // BB2:
    //   // x is known to be > 100 here
    //   if (x <= 10) // never true
    //       goto BB4
    //
    // BB3:
    //   Console.WriteLine("Taken!");
    //
    // BB4:
    //   return;

    // Check whether the dominating compare being "true" or false" implies the dominated compare is known
    // to be either "true" or "false".
    RelopResult ifTrueStatus  = IsCmp2ImpliedByCmp1(domOper, domCns, treeOper, treeCns);
    RelopResult ifFalseStatus = IsCmp2ImpliedByCmp1(GenTree::ReverseRelop(domOper), domCns, treeOper, treeCns);

    if ((ifTrueStatus == RelopResult::Unknown) && (ifFalseStatus == RelopResult::Unknown))
    {
        JITDUMP("Can't infer from both true and false branches - bail out.\n")
        return false;
    }

    if ((ifTrueStatus == RelopResult::AlwaysTrue) && (ifFalseStatus == RelopResult::AlwaysTrue))
    {
        // If it doesn't depend on the dominating relop - bail out, someone else will fold
        // this always-true condition.
        JITDUMP("Always true from both branches - bail out.\n")
        return false;
    }

    rii->canInfer          = true;
    rii->vnRelation        = ValueNumStore::VN_RELATION_KIND::VRK_Inferred;
    rii->canInferFromTrue  = (ifTrueStatus != RelopResult::Unknown);
    rii->canInferFromFalse = (ifFalseStatus != RelopResult::Unknown);
    rii->reverseSense      = (ifFalseStatus == RelopResult::AlwaysTrue) || (ifTrueStatus == RelopResult::AlwaysFalse);
    return true;
}

//------------------------------------------------------------------------
// optRedundantDominatingBranch: see if we can optimize a branch in a
//    dominating block.
//
// Arguments:
//   block - conditional block whose dominators will be probed for redundancy.
//
// Notes:
//   This handles optimizing cases like
//
//   if (x > 0)    // block A, predicate pA
//     if (x > 1)   // block B, predicate pB
//       S;
//
//  into
//
//   if (x > 1)
//     S;
//
//  by proving that pB ==> pA and that B is side effect free.
//
//  We trigger this starting from block B with successors S and X,
//  looking up at the immediate dominator A. If A branches to B and
//  the other successor of A is either S or X, then we have the right
//  control flow pattern for this optimization.
//
//  Suppose X is the shared successor of A and B.
//
//  We then see if the predicate for B->S implies the predicate for A->B.
//  If so, and B is side effect free, we can change A to unconditionally
//  branch to B.
//
//  If this succeeds and A is side effect free, then we can look at the
//  immediate dominator of A and repeat the process, potentially optimizing
//  multiple dominating branches.
//
//  Note that these dominating compares do not all have to share the
//  same successor of B, that is if B's successors are S and X, then
//  some A's can target S and others can target X.
//
//  We may also want to make this be heuristic driven. If pA is
//  likely false and B is expensive, this may not improve performance.
//
bool Compiler::optRedundantDominatingBranch(BasicBlock* const block)
{
    if (!block->KindIs(BBJ_COND))
    {
        return false;
    }

    if (block->hasSideEffects())
    {
        return false;
    }

    Statement* const stmt = block->lastStmt();

    if (stmt == nullptr)
    {
        return false;
    }

    GenTree* const jumpTree = stmt->GetRootNode();

    if (!jumpTree->OperIs(GT_JTRUE))
    {
        return false;
    }

    GenTree* const tree = jumpTree->AsOp()->gtOp1;

    if (!tree->OperIsCompare())
    {
        return false;
    }

    const ValueNum treeNormVN = vnStore->VNNormalValue(tree->GetVN(VNK_Liberal));

    if (vnStore->IsVNConstant(treeNormVN))
    {
        return false;
    }

    // Exclude floating point compares.
    //
    VNFuncApp treeApp;
    if (!vnStore->GetVNFunc(treeNormVN, &treeApp) || !ValueNumStore::VNFuncIsComparison(treeApp.m_func))
    {
        return false;
    }

    if (varTypeIsFloating(vnStore->TypeOfVN(treeApp.m_args[0])))
    {
        return false;
    }

    // Skip through chains of empty or side effect free blocks.
    // Watch for cycles.
    //
    auto skipSideEffectFreeBlocks = [=](BasicBlock* b) {
        BitVecTraits traits(fgBBNumMax + 1, this);
        BitVec       visitedBlocks = BitVecOps::MakeEmpty(&traits);
        while (!b->hasSideEffects() && b->KindIs(BBJ_ALWAYS))
        {
            b = b->GetUniqueSucc();

            if (!BitVecOps::TryAddElemD(&traits, visitedBlocks, b->bbNum))
            {
                // Block is already visited, we have a cycle. Bail out.
                break;
            }
        }

        return b;
    };

    BasicBlock* const blockTrueSucc  = skipSideEffectFreeBlocks(block->GetTrueTarget());
    BasicBlock* const blockFalseSucc = skipSideEffectFreeBlocks(block->GetFalseTarget());
    BasicBlock*       currentBlock   = block;
    BasicBlock*       domBlockProbe  = fgGetDomSpeculatively(block);
    ValueNum          blockPathVN    = ValueNumStore::NoVN;
    bool              madeChanges    = false;
    unsigned          searchCount    = 0;
    const unsigned    searchLimit    = 8;

    JITDUMP("Checking " FMT_BB " for redundant dominating branches\n", block->bbNum);

    if (domBlockProbe == nullptr)
    {
        JITDUMP("failed -- no dominator\n")
    }

    // Walk up the dominator tree.
    // We may be able to optimize multiple dominating branches.
    //
    while (domBlockProbe != nullptr)
    {
        // Avoid walking too far up long skinny dominator trees.
        //
        searchCount++;

        if (searchCount > searchLimit)
        {
            JITDUMP("stopping, hit search limit\n");
            break;
        }

        // Skip past unconditional dominators, if any, as long as they
        // do not have side effects (since they may now become unconditionally
        // executed along the path to block).
        //
        while ((domBlockProbe != nullptr) && domBlockProbe->KindIs(BBJ_ALWAYS))
        {
            if (domBlockProbe->GetTarget() != currentBlock)
            {
                domBlockProbe = nullptr;
                break;
            }

            if (domBlockProbe->hasSideEffects())
            {
                domBlockProbe = nullptr;
                break;
            }

            currentBlock  = domBlockProbe;
            domBlockProbe = fgGetDomSpeculatively(domBlockProbe);
        }

        if (domBlockProbe == nullptr)
        {
            JITDUMP("failed -- no dominator\n");
            break;
        }

        if (!domBlockProbe->KindIs(BBJ_COND))
        {
            JITDUMP("failed -- dominator " FMT_BB " is not BBJ_COND\n", domBlockProbe->bbNum);
            break;
        }

        currentBlock = skipSideEffectFreeBlocks(currentBlock);

        // Make sure this conditional dominator branches to the same
        // shared block as the original block.
        //
        BasicBlock* const domTrueSucc  = skipSideEffectFreeBlocks(domBlockProbe->GetTrueTarget());
        BasicBlock* const domFalseSucc = skipSideEffectFreeBlocks(domBlockProbe->GetFalseTarget());

        const bool currentIsDomTrueSucc  = (domTrueSucc == currentBlock);
        const bool currentIsDomFalseSucc = (domFalseSucc == currentBlock);

        if (currentIsDomTrueSucc == currentIsDomFalseSucc)
        {
            JITDUMP("failed -- " FMT_BB " is degnerate\n", domBlockProbe->bbNum);
            // degenerate BBJ_COND
            break;
        }

        BasicBlock* const sharedSuccessor = currentIsDomTrueSucc ? domFalseSucc : domTrueSucc;

        // Find the VN for the path from block to the non-shared successor.
        //
        if (sharedSuccessor == blockFalseSucc)
        {
            // Shared successor is block's false successor, so unshared successor is block's true successor.
            // Thus the path from block to the unshared successor corresponds to the relop being true.
            //
            blockPathVN = treeNormVN;
        }
        else if (sharedSuccessor == blockTrueSucc)
        {
            // Shared successor is block's true successor, so unshared successor is block's false successor.
            // Thus the path from block to the unshared successor corresponds to the relop being false.
            //
            blockPathVN = vnStore->GetRelatedRelop(treeNormVN, ValueNumStore::VN_RELATION_KIND::VRK_Reverse);
        }
        else
        {
            JITDUMP("failed -- " FMT_BB " does not share a successor with " FMT_BB "\n", domBlockProbe->bbNum,
                    block->bbNum);
            break;
        }

        if (blockPathVN == ValueNumStore::NoVN)
        {
            JITDUMP("failed -- " FMT_BB " does not have a usable VN\n", block->bbNum);
            break;
        }

        JITDUMP(FMT_BB " and " FMT_BB " have shared successor " FMT_BB "\n", domBlockProbe->bbNum, block->bbNum,
                sharedSuccessor->bbNum);

        // Find the VN for the path from domBlockProbe to block.
        //
        Statement* const domStmt = domBlockProbe->lastStmt();
        assert(domStmt != nullptr);

        GenTree* const domJumpTree = domStmt->GetRootNode();
        assert(domJumpTree->OperIs(GT_JTRUE));

        GenTree* const domTree = domJumpTree->AsOp()->gtGetOp1();

        if (!domTree->OperIsCompare())
        {
            break;
        }

        const ValueNum domNormVN = vnStore->VNNormalValue(domTree->GetVN(VNK_Liberal));

        if (vnStore->IsVNConstant(domNormVN))
        {
            break;
        }

        ValueNum domPathVN = domNormVN;

        if (currentIsDomFalseSucc)
        {
            domPathVN = vnStore->GetRelatedRelop(domPathVN, ValueNumStore::VN_RELATION_KIND::VRK_Reverse);
        }

        if (domPathVN == ValueNumStore::NoVN)
        {
            break;
        }

        // We found a dominating compare with the right pattern of control flow.
        // See if the block's path relop implies the dom's path relop.
        //
        RelopImplicationInfo rii;
        rii.treeNormVN   = domPathVN;
        rii.domCmpNormVN = blockPathVN;

        optRelopImpliesRelop(&rii);
        bool canOptimize = rii.canInfer && rii.canInferFromTrue && !rii.reverseSense;

        genTreeOps newRelop   = GT_NONE;
        bool       isUnsigned = false;

        if (!canOptimize)
        {
            if (rii.canInfer)
            {
                JITDUMP("Can't infer along the path we care about; trying simplification instead\n");
            }
            else
            {
                JITDUMP("Can't infer, trying simplification instead\n");
            }

            // See if we can simplify the VN for blockPathVN AND domPathVN
            //
            ValueNum  andVN = vnStore->VNForFunc(TYP_INT, VNF_AND, blockPathVN, domPathVN);
            VNFuncApp andApp;
            VNFuncApp pathApp;
            VNFunc    newRelopFunc = VNF_NONE;
            if (vnStore->IsVNRelop(andVN, &andApp) && vnStore->GetVNFunc(blockPathVN, &pathApp))
            {
                if (andApp.m_args[0] == pathApp.m_args[0] && andApp.m_args[1] == pathApp.m_args[1])
                {
                    newRelopFunc = andApp.m_func;
                }
                else if (andApp.m_args[0] == pathApp.m_args[1] && andApp.m_args[1] == pathApp.m_args[0])
                {
                    andVN = vnStore->GetRelatedRelop(andVN, ValueNumStore::VN_RELATION_KIND::VRK_Swap);
                    vnStore->GetVNFunc(andVN, &andApp);
                    newRelopFunc = andApp.m_func;
                }

                JITDUMPEXEC(vnStore->vnDump(this, blockPathVN));
                JITDUMP(" AND");
                JITDUMPEXEC(vnStore->vnDump(this, domPathVN));
                JITDUMP(" ==>");
                JITDUMPEXEC(vnStore->vnDump(this, andVN));
            }

            // TODO-CQ: if the AND simplifies to a constant, we can optimize both the dominating branch
            // and the current branch. This is likely rare.

            if (newRelopFunc != VNF_NONE)
            {
                newRelop = vnStore->VNRelopToGenTreeOp(newRelopFunc, &isUnsigned);

                if (newRelop != GT_NONE)
                {
                    JITDUMP("; simplified to %s%s\n", GenTree::OpName(newRelop), isUnsigned ? " (unsigned)" : "");
                    canOptimize = true;
                }
            }
            else
            {
                JITDUMP("; not a relop, cannot simplify\n");
            }
        }

        if (!canOptimize)
        {
            JITDUMP("failed -- Dominated VN " FMT_VN " does not imply dominating VN " FMT_VN "\n", blockPathVN,
                    domPathVN);
            break;
        }

        JITDUMP("Optimizing branch in dominating " FMT_BB " with relop [%06u] based on " FMT_BB "'s relop [%06u]\n",
                domBlockProbe->bbNum, dspTreeID(domTree), block->bbNum, dspTreeID(tree));

        const int domRelopValue = currentIsDomTrueSucc ? 1 : 0;

        bool domMayHaveSideEffects = false;

        // Always preserve side effects in the dominating relop.
        //
        if ((domTree->gtFlags & GTF_SIDE_EFFECT) != 0)
        {
            JITDUMP("Dominating relop has side effects, keeping it, unused\n");
            GenTree* const relopComma    = gtNewOperNode(GT_COMMA, TYP_INT, domTree, gtNewIconNode(domRelopValue));
            domJumpTree->AsUnOp()->gtOp1 = relopComma;
            domMayHaveSideEffects        = true;
        }
        else
        {
            domTree->BashToConst(domRelopValue);
        }

        JITDUMP("\nRedundant dominating branch opt in " FMT_BB ":\n", domBlockProbe->bbNum);

        fgMorphBlockStmt(domBlockProbe, domStmt DEBUGARG(__FUNCTION__), /* allowFGChange */ true,
                         /* invalidateDFSTreeOnFGChange */ false);
        Metrics.RedundantBranchesEliminated++;

        if (newRelop != GT_NONE)
        {
            if (sharedSuccessor == blockTrueSucc)
            {
                newRelop = GenTree::ReverseRelop(newRelop);
            }

            tree->SetOper(newRelop);

            // Update GTF_UNSIGNED before re-value-numbering.
            //
            if (isUnsigned)
            {
                tree->SetUnsigned();
            }
            else
            {
                tree->ClearUnsigned();
            }

            fgValueNumberTree(tree);
        }
        madeChanges = true;

        // We can keep looking if we haven't seen any side effects yet along the path to block.
        //
        if (!domMayHaveSideEffects)
        {
            domMayHaveSideEffects = domBlockProbe->hasSideEffects();
        }

        if (domMayHaveSideEffects)
        {
            JITDUMP("stopping -- side effects seen along path to block\n");
            break;
        }

        currentBlock  = domBlockProbe;
        domBlockProbe = fgGetDomSpeculatively(domBlockProbe);

        JITDUMP("continuing to the next immediate dominator\n");
    }

    return madeChanges;
}

//------------------------------------------------------------------------
// optRedundantBranch: try and optimize a possibly redundant branch
//
// Arguments:
//   block - block with branch to optimize
//
// Returns:
//   True if the branch was optimized.
//
bool Compiler::optRedundantBranch(BasicBlock* const block)
{
    JITDUMP("\n--- Trying RBO in " FMT_BB " ---\n", block->bbNum);

    Statement* const stmt = block->lastStmt();

    if (stmt == nullptr)
    {
        return false;
    }

    GenTree* const jumpTree = stmt->GetRootNode();

    if (!jumpTree->OperIs(GT_JTRUE))
    {
        return false;
    }

    GenTree* const tree = jumpTree->AsOp()->gtOp1;

    if (!tree->OperIsCompare())
    {
        return false;
    }

    // Walk up the dom tree and see if any dominating block has branched on
    // exactly this tree's VN...
    //
    BasicBlock*    prevBlock   = block;
    BasicBlock*    domBlock    = block->bbIDom;
    int            relopValue  = -1;
    ValueNum       treeExcVN   = ValueNumStore::NoVN;
    ValueNum       domCmpExcVN = ValueNumStore::NoVN;
    unsigned       matchCount  = 0;
    const unsigned matchLimit  = 4;

    // Unpack the tree's VN
    //
    ValueNum treeNormVN;
    vnStore->VNUnpackExc(tree->GetVN(VNK_Liberal), &treeNormVN, &treeExcVN);

    // If the treeVN is a constant, we optimize directly.
    //
    // Note the inferencing we do below is not valid for constant VNs,
    // so handling/avoiding this case up front is a correctness requirement.
    //
    if (vnStore->IsVNConstant(treeNormVN))
    {
        relopValue = (treeNormVN == vnStore->VNZeroForType(TYP_INT)) ? 0 : 1;
        JITDUMP("Relop [%06u] " FMT_BB " has known value %s\n ", dspTreeID(tree), block->bbNum,
                relopValue == 0 ? "false" : "true");
    }
    else
    {
        if (domBlock == nullptr)
        {
            return false;
        }

        JITDUMP("Relop [%06u] " FMT_BB " value unknown, trying inference\n", dspTreeID(tree), block->bbNum);
    }

    bool trySpeculativeDom = false;
    while ((relopValue == -1) && !trySpeculativeDom)
    {
        if (domBlock == nullptr)
        {
            // It's possible that bbIDom is not up to date at this point due to recent BB modifications
            // so let's try to quickly calculate new one
            domBlock = fgGetDomSpeculatively(block);
            if (domBlock == block->bbIDom)
            {
                // We already checked this one
                break;
            }
            trySpeculativeDom = true;
        }

        if (domBlock == nullptr)
        {
            break;
        }

        // Check the current dominator
        //
        if (domBlock->KindIs(BBJ_COND))
        {
            Statement* const domJumpStmt = domBlock->lastStmt();
            GenTree* const   domJumpTree = domJumpStmt->GetRootNode();
            assert(domJumpTree->OperIs(GT_JTRUE));
            GenTree* const domCmpTree = domJumpTree->AsOp()->gtGetOp1();

            if (domCmpTree->OperIsCompare())
            {
                // We can use liberal VNs here, as bounds checks are not yet
                // manifest explicitly as relops.
                //
                RelopImplicationInfo rii;
                rii.treeNormVN = treeNormVN;
                vnStore->VNUnpackExc(domCmpTree->GetVN(VNK_Liberal), &rii.domCmpNormVN, &domCmpExcVN);

                // See if knowing the value of domCmpNormVN implies knowing the value of treeNormVN.
                //
                optRelopImpliesRelop(&rii);

                if (rii.canInfer)
                {
                    // If we have a long skinny dominator tree we may scale poorly,
                    // and in particular reachability (below) is costly. Give up if
                    // we've matched a few times and failed to optimize.
                    //
                    if (++matchCount > matchLimit)
                    {
                        JITDUMP("Bailing out; %d matches found w/o optimizing\n", matchCount);
                        break;
                    }

                    // Was this an inference from an unrelated relop (GE => GT, say)?
                    //
                    const bool domIsInferredRelop = (rii.vnRelation == ValueNumStore::VN_RELATION_KIND::VRK_Inferred);

                    // The compare in "tree" is redundant.
                    // Is there a unique path from the dominating compare?
                    //
                    if (domIsInferredRelop)
                    {
                        // We used to assert rii.canInferFromTrue ^ rii.canInferFromFalse here.
                        //
                        // But now we can find fully redundant compares with different relops,
                        // eg LT x, 47 dominating LE x, 46. The second relop's value is equal to the first.
                        //
                        JITDUMP("\nDominator " FMT_BB " of " FMT_BB " can infer value of dominated relop\n",
                                domBlock->bbNum, block->bbNum);
                    }
                    else
                    {
                        JITDUMP("\nDominator " FMT_BB " of " FMT_BB " has relop with %s liberal VN\n", domBlock->bbNum,
                                block->bbNum, ValueNumStore::VNRelationString(rii.vnRelation));
                    }
                    DISPTREE(domCmpTree);
                    JITDUMP(" Redundant compare; current relop:\n");
                    DISPTREE(tree);

                    BasicBlock* const trueSuccessor  = domBlock->GetTrueTarget();
                    BasicBlock* const falseSuccessor = domBlock->GetFalseTarget();

                    // If we can trace the flow from the dominating relop, we can infer its value.
                    //
                    const bool trueReaches  = optReachable(trueSuccessor, block, domBlock);
                    const bool falseReaches = optReachable(falseSuccessor, block, domBlock);

                    if (trueReaches && falseReaches && rii.canInferFromTrue && rii.canInferFromFalse)
                    {
                        // JIT-TP: it didn't produce diffs so let's skip it
                        if (trySpeculativeDom)
                        {
                            break;
                        }

                        // Both dominating compare outcomes reach the current block so we can't infer the
                        // value of the relop.
                        //
                        // However we may be able to update the flow from block's predecessors so they
                        // bypass block and instead transfer control to jump's successors (aka jump threading).
                        //
                        const bool wasThreaded = optJumpThreadDom(block, domBlock, !rii.reverseSense);

                        if (wasThreaded)
                        {
                            return true;
                        }
                    }
                    else if (trueReaches && !falseReaches && rii.canInferFromTrue)
                    {
                        // True path in dominator reaches, false path doesn't; relop must be true/false.
                        //
                        const bool relopIsTrue = !rii.reverseSense;
                        JITDUMP("True successor " FMT_BB " of " FMT_BB " reaches, relop [%06u] must be %s\n",
                                domBlock->GetTrueTarget()->bbNum, domBlock->bbNum, dspTreeID(tree),
                                relopIsTrue ? "true" : "false");
                        relopValue = relopIsTrue ? 1 : 0;
                        break;
                    }
                    else if (falseReaches && !trueReaches && rii.canInferFromFalse)
                    {
                        // False path from dominator reaches, true path doesn't; relop must be false/true.
                        //
                        const bool relopIsFalse = !rii.reverseSense;
                        JITDUMP("False successor " FMT_BB " of " FMT_BB " reaches, relop [%06u] must be %s\n",
                                domBlock->GetFalseTarget()->bbNum, domBlock->bbNum, dspTreeID(tree),
                                relopIsFalse ? "false" : "true");
                        relopValue = relopIsFalse ? 0 : 1;
                        break;
                    }
                    else if (!falseReaches && !trueReaches)
                    {
                        // No apparent path from the dominating BB.
                        //
                        // We should rarely see this given that optReachable is returning
                        // up to date results, but as we optimize we create unreachable blocks,
                        // and that can lead to cases where we can't find paths. That means we may be
                        // optimizing code that is now unreachable, but attempts to fix or avoid
                        // doing that lead to more complications, and it isn't that common.
                        // So we just tolerate it.
                        //
                        // No point in looking further up the tree.
                        //
                        JITDUMP("inference failed -- no apparent path, will stop looking\n");
                        break;
                    }
                    else
                    {
                        // Keep looking up the dom tree
                        //
                        JITDUMP("inference failed -- will keep looking higher\n");
                    }
                }
            }
        }

        // Keep looking higher up in the tree
        //
        prevBlock = domBlock;
        domBlock  = domBlock->bbIDom;
    }

    // Did we determine the relop value via dominance checks? If so, optimize.
    //
    if (relopValue == -1)
    {
        // We were unable to determine the relop value via dominance checks.
        // See if we can jump thread via phi disambiguation.
        //
        return optJumpThreadPhi(block, tree, treeNormVN);
    }

    // Be conservative if there is an exception effect and we're in an EH region
    // as we might not model the full extent of EH flow.
    //
    if (((tree->gtFlags & GTF_EXCEPT) != 0) && block->hasTryIndex())
    {
        JITDUMP("Current relop has exception side effect and is in a try, so we won't optimize\n");
        return false;
    }

    // Handle the side effects: for exceptions we can know whether we can drop them using the exception sets.
    // Other side effects we always leave around (the unused tree will be appropriately transformed by morph).
    //
    bool keepTreeForSideEffects = false;
    if ((tree->gtFlags & GTF_SIDE_EFFECT) != 0)
    {
        keepTreeForSideEffects = true;

        if (((tree->gtFlags & GTF_SIDE_EFFECT) == GTF_EXCEPT) && vnStore->VNExcIsSubset(domCmpExcVN, treeExcVN))
        {
            keepTreeForSideEffects = false;
        }
    }

    if (keepTreeForSideEffects)
    {
        JITDUMP("Current relop has side effects, keeping it, unused\n");
        GenTree* relopComma       = gtNewOperNode(GT_COMMA, TYP_INT, tree, gtNewIconNode(relopValue));
        jumpTree->AsUnOp()->gtOp1 = relopComma;
    }
    else
    {
        tree->BashToConst(relopValue);
    }

    JITDUMP("\nRedundant branch opt in " FMT_BB ":\n", block->bbNum);

    fgMorphBlockStmt(block, stmt DEBUGARG(__FUNCTION__), /* allowFGChange */ true,
                     /* invalidateDFSTreeOnFGChange */ false);
    Metrics.RedundantBranchesEliminated++;
    return true;
}

//------------------------------------------------------------------------
// JumpThreadInfo
//
// Describes the relationship between a block-ending predicate value and the
// block's predecessors.
//
struct JumpThreadInfo
{
    JumpThreadInfo(Compiler* comp, BasicBlock* block)
        : m_block(block)
        , m_trueTarget(block->GetTrueTarget())
        , m_falseTarget(block->GetFalseTarget())
        , m_ambiguousVNBlock(nullptr)
        , traits(comp->m_dfsTree->PostOrderTraits())
        , m_truePreds(BitVecOps::MakeEmpty(&traits))
        , m_ambiguousPreds(BitVecOps::MakeEmpty(&traits))
        , m_phiUses(comp->getAllocator(CMK_RedundantBranch))
        , m_phiDefsToRemove(comp->getAllocator(CMK_RedundantBranch))
        , m_numPreds(0)
        , m_numAmbiguousPreds(0)
        , m_numTruePreds(0)
        , m_numFalsePreds(0)
        , m_ambiguousVN(ValueNumStore::NoVN)
        , m_isPhiBased(false)
    {
    }

    // Block we're trying to optimize
    BasicBlock* const m_block;
    // Block successor if predicate is true
    BasicBlock* const m_trueTarget;
    // Block successor if predicate is false
    BasicBlock* const m_falseTarget;
    // Block that brings in the ambiguous VN
    BasicBlock* m_ambiguousVNBlock;
    // Traits for the below BitVecs
    BitVecTraits traits;
    // Pred blocks for which the predicate will be true
    BitVec m_truePreds;
    // Pred blocks that can't be threaded or for which the predicate
    // value can't be determined
    BitVec m_ambiguousPreds;
    // Uses of block-local phi defs that must be updated if threading bypasses the block.
    struct PhiUse
    {
        PhiUse(BasicBlock* block, GenTreeLclVarCommon* use)
            : m_block(block)
            , m_use(use)
        {
        }

        BasicBlock*          m_block;
        GenTreeLclVarCommon* m_use;
        unsigned             m_replacementSsaNum = SsaConfig::RESERVED_SSA_NUM;
    };
    ArrayStack<PhiUse> m_phiUses;
    // Phi-def statements in the threaded block that become redundant once
    // all remaining incoming paths agree on the same reaching SSA def.
    ArrayStack<Statement*> m_phiDefsToRemove;
    // Total number of predecessors
    int m_numPreds;
    // Number of predecessors that can't be threaded or for which the predicate
    // value can't be determined
    int m_numAmbiguousPreds;
    // Number of predecessors for which predicate is true
    int m_numTruePreds;
    // Number of predecessors for which predicate is false
    int m_numFalsePreds;
    // Refined VN for ambiguous cases
    ValueNum m_ambiguousVN;
    // True if this was a phi-based jump thread
    bool m_isPhiBased;
};

//------------------------------------------------------------------------
// JumpThreadPhiUseVisitor: tree visitor that records uses of a specific SSA
//   def in a block while preserving the containing block for each use.
//
class JumpThreadPhiUseVisitor final : public GenTreeVisitor<JumpThreadPhiUseVisitor>
{
public:
    enum
    {
        DoPreOrder    = true,
        DoLclVarsOnly = true
    };

    JumpThreadPhiUseVisitor(Compiler*                           compiler,
                            unsigned                            lclNum,
                            unsigned                            ssaNum,
                            BasicBlock*                         block,
                            ArrayStack<JumpThreadInfo::PhiUse>* uses)
        : GenTreeVisitor(compiler)
        , m_lclNum(lclNum)
        , m_ssaNum(ssaNum)
        , m_block(block)
        , m_uses(uses)
    {
    }

    Compiler::fgWalkResult PreOrderVisit(GenTree** use, GenTree* user)
    {
        GenTree* const node = *use;

        if (node->OperIsLocalRead())
        {
            GenTreeLclVarCommon* const lclUse = node->AsLclVarCommon();
            if ((lclUse->GetLclNum() == m_lclNum) && (lclUse->GetSsaNum() == m_ssaNum))
            {
                m_uses->Emplace(m_block, lclUse);
            }
        }

        return Compiler::WALK_CONTINUE;
    }

private:
    unsigned                            m_lclNum;
    unsigned                            m_ssaNum;
    BasicBlock* const                   m_block;
    ArrayStack<JumpThreadInfo::PhiUse>* m_uses;
};

//------------------------------------------------------------------------
// optGetThreadedSsaNumForBlock: determine the replacement SSA number for
//   uses in the threaded block once only ambiguous preds still reach it.
//
// Arguments:
//   jti               - threading classification for the block
//   phiDef            - phi definition in the threaded block
//   replacementSsaNum - [OUT] the common reaching SSA number for ambiguous preds
//
// Returns:
//   True if all ambiguous preds reaching block agree on one SSA number.
//
static bool optGetThreadedSsaNumForBlock(JumpThreadInfo& jti, GenTreeLclVar* phiDef, unsigned* replacementSsaNum)
{
    assert(jti.m_numAmbiguousPreds != 0);

    bool              foundReplacement = false;
    unsigned          replacementSsa   = SsaConfig::RESERVED_SSA_NUM;
    GenTreePhi* const phi              = phiDef->Data()->AsPhi();

    for (GenTreePhi::Use& use : phi->Uses())
    {
        GenTreePhiArg* const phiArgNode = use.GetNode()->AsPhiArg();
        BasicBlock* const    predBlock  = phiArgNode->gtPredBB;

        if (!BitVecOps::IsMember(&jti.traits, jti.m_ambiguousPreds, predBlock->bbPostorderNum))
        {
            continue;
        }

        if (!foundReplacement)
        {
            replacementSsa   = phiArgNode->GetSsaNum();
            foundReplacement = true;
        }
        else if (replacementSsa != phiArgNode->GetSsaNum())
        {
            return false;
        }
    }

    if (!foundReplacement)
    {
        return false;
    }

    *replacementSsaNum = replacementSsa;
    return true;
}

//------------------------------------------------------------------------
// optGetThreadedSsaNumForSuccessor: determine the replacement SSA number
//   for uses in a successor once all non-ambiguous preds are threaded.
//
// Arguments:
//   jti               - threading classification for the block
//   phiDef            - phi definition in the threaded block
//   successor         - successor being considered for rewriting
//   hasThreadedPreds  - [OUT] true if one or more preds will be redirected here
//   replacementSsaNum - [OUT] the common reaching SSA number for those preds
//
// Returns:
//   True if all post-threading paths reaching successor agree on one SSA number.
//
static bool optGetThreadedSsaNumForSuccessor(JumpThreadInfo& jti,
                                             GenTreeLclVar*  phiDef,
                                             BasicBlock*     successor,
                                             bool*           hasThreadedPreds,
                                             unsigned*       replacementSsaNum)
{
    assert(successor != nullptr);

    *hasThreadedPreds  = false;
    *replacementSsaNum = SsaConfig::RESERVED_SSA_NUM;

    bool              foundReplacement = false;
    unsigned          replacementSsa   = SsaConfig::RESERVED_SSA_NUM;
    GenTreePhi* const phi              = phiDef->Data()->AsPhi();

    for (GenTreePhi::Use& use : phi->Uses())
    {
        GenTreePhiArg* const phiArgNode = use.GetNode()->AsPhiArg();
        BasicBlock* const    predBlock  = phiArgNode->gtPredBB;
        bool const           isTruePred = BitVecOps::IsMember(&jti.traits, jti.m_truePreds, predBlock->bbPostorderNum);
        bool const isAmbiguousPred = BitVecOps::IsMember(&jti.traits, jti.m_ambiguousPreds, predBlock->bbPostorderNum);

        if (!isAmbiguousPred)
        {
            BasicBlock* const predTarget = isTruePred ? jti.m_trueTarget : jti.m_falseTarget;
            if (predTarget != successor)
            {
                continue;
            }

            *hasThreadedPreds = true;
        }

        if (!foundReplacement)
        {
            replacementSsa   = phiArgNode->GetSsaNum();
            foundReplacement = true;
        }
        else if (replacementSsa != phiArgNode->GetSsaNum())
        {
            return false;
        }
    }

    *replacementSsaNum = replacementSsa;
    return foundReplacement;
}

//------------------------------------------------------------------------
// optFindPhiUsesInBlockAndSuccessors: look for all uses of a PHI SSA def in
//   the candidate block and its immediate successors.
//
// Arguments:
//   block  - jump-threading block that contains the PHI
//   phiDef - PHI definition to inspect
//   jti    - threading information for the block
//
// Returns:
//   True if every recorded SSA use for the PHI def is found.
//
bool Compiler::optFindPhiUsesInBlockAndSuccessors(BasicBlock* block, GenTreeLclVar* phiDef, JumpThreadInfo& jti)
{
    unsigned const lclNum = phiDef->GetLclNum();
    unsigned const ssaNum = phiDef->GetSsaNum();

    LclSsaVarDsc* const ssaVarDsc    = lvaGetDesc(lclNum)->GetPerSsaData(ssaNum);
    int const           expectedUses = ssaVarDsc->GetNumUses();
    if (expectedUses == USHRT_MAX)
    {
        return false;
    }

    int const initialUseCount = jti.m_phiUses.Height();

    JumpThreadPhiUseVisitor blockVisitor(this, lclNum, ssaNum, block, &jti.m_phiUses);
    for (Statement* const blockStmt : block->Statements())
    {
        blockVisitor.WalkTree(blockStmt->GetRootNodePointer(), nullptr);
    }

    for (BasicBlock* const successor : block->Succs())
    {
        if (successor == block)
        {
            continue;
        }

        JumpThreadPhiUseVisitor succVisitor(this, lclNum, ssaNum, successor, &jti.m_phiUses);
        for (Statement* const succStmt : successor->Statements())
        {
            succVisitor.WalkTree(succStmt->GetRootNodePointer(), nullptr);
        }
    }

    int const foundUses = jti.m_phiUses.Height() - initialUseCount;

    return foundUses == expectedUses;
}

//------------------------------------------------------------------------
// optCanRewritePhiUses: determine if PHIs with global uses can be handled
//   safely for jump threading by rewriting successor uses.
//
// Arguments:
//   jti  - threading information for the block
//
// Returns:
//   True if all PHI uses are accounted for and any successor uses can be
//   rewritten to a unique reaching SSA def.
//
// Notes:
//   We bail out in cases where a successor has a PHI or would need to have
//   a PHI for any of the locals that have PHIs in block.
//
bool Compiler::optCanRewritePhiUses(JumpThreadInfo& jti)
{
    BasicBlock* const block = jti.m_block;

    // Now check if we can find all of the uses of the PHIs in block,
    // either in block or in its successors.
    //
    for (Statement* const stmt : block->Statements())
    {
        if (!stmt->IsPhiDefnStmt())
        {
            break;
        }

        GenTreeLclVar* const phiDef = stmt->GetRootNode()->AsLclVar();
        unsigned const       lclNum = phiDef->GetLclNum();
        unsigned const       ssaNum = phiDef->GetSsaNum();

        LclSsaVarDsc* const ssaVarDsc = lvaGetDesc(lclNum)->GetPerSsaData(ssaNum);
        if (!ssaVarDsc->HasGlobalUse())
        {
            continue;
        }

        if (!optFindPhiUsesInBlockAndSuccessors(block, phiDef, jti))
        {
            JITDUMP("Could not find all uses for V%02u.%u in " FMT_BB " or its successors\n", lclNum, ssaNum,
                    block->bbNum);
            return false;
        }

        bool hasBlockUse = false;
        for (JumpThreadInfo::PhiUse& phiUse : jti.m_phiUses.BottomUpOrder())
        {
            if ((phiUse.m_use->GetLclNum() != lclNum) || (phiUse.m_use->GetSsaNum() != ssaNum))
            {
                continue;
            }

            if (phiUse.m_block == block)
            {
                hasBlockUse = true;
                break;
            }
        }

        bool     removePhiDef        = false;
        unsigned blockReplacementSsa = SsaConfig::RESERVED_SSA_NUM;
        if (hasBlockUse && (jti.m_numAmbiguousPreds != 0))
        {
            if (!optGetThreadedSsaNumForBlock(jti, phiDef, &blockReplacementSsa))
            {
                JITDUMP("Ambiguous preds do not agree on a replacement SSA for V%02u.%u in " FMT_BB "\n", lclNum,
                        ssaNum, block->bbNum);
                return false;
            }

            for (JumpThreadInfo::PhiUse& phiUse : jti.m_phiUses.BottomUpOrder())
            {
                if ((phiUse.m_use->GetLclNum() != lclNum) || (phiUse.m_use->GetSsaNum() != ssaNum))
                {
                    continue;
                }

                if (phiUse.m_block == block)
                {
                    phiUse.m_replacementSsaNum = blockReplacementSsa;
                }
            }

            jti.m_phiDefsToRemove.Push(stmt);
            removePhiDef = true;
        }

        for (BasicBlock* const successor : block->Succs())
        {
            bool hasSuccUse = false;
            for (JumpThreadInfo::PhiUse& phiUse : jti.m_phiUses.BottomUpOrder())
            {
                if ((phiUse.m_use->GetLclNum() != lclNum) || (phiUse.m_use->GetSsaNum() != ssaNum))
                {
                    continue;
                }

                if (phiUse.m_block == successor)
                {
                    hasSuccUse = true;
                    break;
                }
            }

            if (!hasSuccUse)
            {
                continue;
            }

            // If successor is a join, we might need to introduce or modify a PHI. Bail instead.
            //
            if (successor->GetUniquePred(this) != block)
            {
                JITDUMP(FMT_BB " successor " FMT_BB " is a join with phi uses; cannot rewrite phi uses\n", block->bbNum,
                        successor->bbNum);
                return false;
            }

            // Verify that all post-threading incoming defs that can reach successor agree on the same
            // SSA number so no phi is needed in successor, just a rewrite of the uses to the common SSA number.
            //
            bool     hasThreadedPreds = false;
            unsigned replacementSsa   = SsaConfig::RESERVED_SSA_NUM;
            if (!optGetThreadedSsaNumForSuccessor(jti, phiDef, successor, &hasThreadedPreds, &replacementSsa))
            {
                JITDUMP(FMT_BB " successor " FMT_BB " incoming defs for V%02u.%u do not agree\n", block->bbNum,
                        successor->bbNum, lclNum, ssaNum);
                return false;
            }

            if (!hasThreadedPreds && !removePhiDef)
            {
                continue;
            }

            assert(!removePhiDef || (jti.m_numAmbiguousPreds != 0));
            assert(!removePhiDef || (replacementSsa != SsaConfig::RESERVED_SSA_NUM));

            for (JumpThreadInfo::PhiUse& phiUse : jti.m_phiUses.BottomUpOrder())
            {
                if ((phiUse.m_use->GetLclNum() != lclNum) || (phiUse.m_use->GetSsaNum() != ssaNum))
                {
                    continue;
                }

                if (phiUse.m_block == successor)
                {
                    phiUse.m_replacementSsaNum = replacementSsa;
                }
            }
        }
    }

    return true;
}

//------------------------------------------------------------------------
// optJumpThreadCheck: see if block is suitable for jump threading.
//
// Arguments:
//   block - block in question
//   domBlock - dom block used in inferencing (if any)
//
// Returns:
//   Viability of jump threading: either CannotThread, CanThread, or NeedsPhiUseResolution.
//
Compiler::JumpThreadCheckResult Compiler::optJumpThreadCheck(BasicBlock* const block, BasicBlock* const domBlock)
{
    // If the block is the first block of try-region, then skip jump threading
    if (bbIsTryBeg(block))
    {
        JITDUMP(FMT_BB " is first block of try-region; no threading\n", block->bbNum);
        return JumpThreadCheckResult::CannotThread;
    }

    // Verify that dom block dominates all of block's predecessors.
    //
    // This will initially be true but if we jump thread through
    // dom block, it may no longer be true.
    //
    if (domBlock != nullptr)
    {
        for (BasicBlock* const predBlock : block->PredBlocks())
        {
            if (m_dfsTree->Contains(predBlock) && !m_domTree->Dominates(domBlock, predBlock))
            {
                JITDUMP("Dom " FMT_BB " is stale (does not dominate pred " FMT_BB "); no threading\n", domBlock->bbNum,
                        predBlock->bbNum);
                return JumpThreadCheckResult::CannotThread;
            }
        }
    }

    // Since flow is going to bypass block, make sure there
    // is nothing in block that can cause a side effect.
    //
    // For non-PHI RBO, we neglect PHI stores. This can leave SSA in
    // an incorrect state but so far it has not yet caused problems.
    //
    // For PHI-based RBO we need to be more cautious. We can now tolerate
    // some non-local PHI uses, but only when all uses are found in the
    // block or its immediate successors so the needed SSA/VN updates can be
    // made.
    //
    // TODO: handle blocks with side effects. For those predecessors that are
    // favorable (ones that don't reach block via a critical edge), consider
    // duplicating block's IR into the predecessor. This is the jump threading
    // analog of the optimization we encourage via fgOptimizeUncondBranchToSimpleCond.
    //
    Statement* const lastStmt         = block->lastStmt();
    bool const       isPhiRBO         = (domBlock == nullptr);
    bool             hasGlobalPhiUses = false;

    for (Statement* const stmt : block->Statements())
    {
        GenTree* const tree = stmt->GetRootNode();

        // If we are doing PHI-based RBO then each local PHI must either be
        // locally consumed or have only tracked uses that we can safely rewrite.
        //
        if (stmt->IsPhiDefnStmt())
        {
            if (isPhiRBO)
            {
                GenTreeLclVarCommon* const phiDef = tree->AsLclVarCommon();
                unsigned const             lclNum = phiDef->GetLclNum();
                unsigned const             ssaNum = phiDef->GetSsaNum();
                LclVarDsc* const           varDsc = lvaGetDesc(lclNum);

                // We do not put implicit uses of promoted local fields into SSA.
                // So assume the worst here, that there is some implicit use of this ssa
                // def we don't know about.
                //
                if (varDsc->lvIsStructField)
                {
                    JITDUMP(FMT_BB " has phi for promoted field V%02u.%u; no phi-based threading\n", block->bbNum,
                            lclNum, ssaNum);
                    return JumpThreadCheckResult::CannotThread;
                }

                LclSsaVarDsc* const ssaVarDsc = varDsc->GetPerSsaData(ssaNum);

                // Bypassing a global use might require SSA updates.
                // Note a phi use is ok if it's local (self loop)
                //
                if (ssaVarDsc->HasGlobalUse())
                {
                    JITDUMP(FMT_BB " has global phi for V%02u.%u; must look for phi uses\n", block->bbNum, lclNum,
                            ssaNum);
                    hasGlobalPhiUses = true;
                }
            }

            // We are either not doing PHI-based RBO or this PHI won't cause
            // problems. Carry on.
            //
            continue;
        }

        // This is a "real" statement.
        //
        // We can ignore exception side effects in the jump tree.
        //
        // They are covered by the exception effects in the dominating compare.
        // We know this because the VNs match and they encode exception states.
        //
        if ((tree->gtFlags & GTF_SIDE_EFFECT) != 0)
        {
            if (stmt == lastStmt)
            {
                assert(tree->OperIs(GT_JTRUE));
                if ((tree->gtFlags & GTF_SIDE_EFFECT) == GTF_EXCEPT)
                {
                    // However, be conservative if the blocks are not in the
                    // same EH region, as we might not be able to fully
                    // describe control flow between them.
                    //
                    if ((domBlock != nullptr) && BasicBlock::sameEHRegion(block, domBlock))
                    {
                        // We will ignore the side effect on this tree.
                        //
                        continue;
                    }
                }
            }

            JITDUMP(FMT_BB " has side effects; no threading\n", block->bbNum);
            return JumpThreadCheckResult::CannotThread;
        }
    }

    return hasGlobalPhiUses ? JumpThreadCheckResult::NeedsPhiUseResolution : JumpThreadCheckResult::CanThread;
}

//------------------------------------------------------------------------
// optJumpThreadDom: try and bypass the current block by rerouting
//   flow from predecessors directly to successors.
//
// Arguments:
//   block - block with branch to optimize
//   domBlock - a dominating block that has an equivalent branch
//   domIsSameRelop - if true, dominating block does the same compare;
//                    if false, dominating block does a reverse compare
//
// Returns:
//   True if the branch was optimized.
//
// Notes:
//
// Conceptually this just transforms flow as follows:
//
//     domBlock           domBlock
//    /       |          /       |
//    Ts      Fs         Ts      Fs    True/False successor
//   ....    ....       ....    ....
//    Tp      Fp         Tp      Fp    True/False pred
//     \     /           |       |
//      \   /            |       |
//      block     ==>    |       |
//      /   \            |       |
//     /     \           |       |
//    Tt     Ft          Tt      Ft    True/false target
//
// However we may try to re-purpose block, and so end up producing flow more like this:
//
//     domBlock           domBlock
//    /       |          /       |
//    Ts      Fs         Ts      Fs    True/False successor
//   ....    ....       ....    ....
//    Tp      Fp         Tp      Fp    True/False pred
//     \     /           |       |
//      \   /            |       |
//      block     ==>    |      block   (repurposed)
//      /   \            |       |
//     /     \           |       |
//    Tt     Ft          Tt      Ft    True/false target
//
bool Compiler::optJumpThreadDom(BasicBlock* const block, BasicBlock* const domBlock, bool domIsSameRelop)
{
    assert(block->KindIs(BBJ_COND));
    assert(domBlock->KindIs(BBJ_COND));

    // If the dominating block is not the immediate dominator
    // we might need to duplicate a lot of code to thread
    // the jumps. See if that's the case.
    //
    const bool isIDom = domBlock == block->bbIDom;
    if (!isIDom)
    {
        // Walk up the dom tree until we hit dom block.
        //
        // If none of the doms in the stretch are BBJ_COND,
        // then we must have already optimized them, and
        // so should not have to duplicate code to thread.
        //
        BasicBlock* idomBlock = block->bbIDom;
        while ((idomBlock != nullptr) && (idomBlock != domBlock))
        {
            if (idomBlock->KindIs(BBJ_COND))
            {
                JITDUMP(" -- " FMT_BB " not closest branching dom, so no threading\n", idomBlock->bbNum);
                return false;
            }
            JITDUMP(" -- bypassing %sdom " FMT_BB " as it was already optimized\n",
                    (idomBlock == block->bbIDom) ? "i" : "", idomBlock->bbNum);
            idomBlock = idomBlock->bbIDom;
        }

        // If we didn't bail out above, we should have reached domBlock.
        //
        assert(idomBlock == domBlock);
    }

    JITDUMP("Both successors of %sdom " FMT_BB " reach " FMT_BB " -- attempting jump threading\n", isIDom ? "i" : "",
            domBlock->bbNum, block->bbNum);

    const JumpThreadCheckResult check = optJumpThreadCheck(block, domBlock);
    if (check == JumpThreadCheckResult::CannotThread)
    {
        return false;
    }

    if (check == JumpThreadCheckResult::NeedsPhiUseResolution)
    {
        JITDUMP(FMT_BB " has global phi uses; no jump threading\n", block->bbNum);
        return false;
    }

    // In order to optimize we have to be able to determine which predecessors
    // are correlated exclusively with a true value for block's relop, and which
    // are correlated exclusively with a false value (aka true preds and false preds).
    //
    // To do this we try and follow the flow from domBlock to block. When domIsSameRelop
    // is true, any block pred reachable from domBlock's true edge is a true pred of block,
    // and any block pred reachable from domBlock's false edge is a false pred of block.
    //
    // If domIsSameRelop is false, then the roles of the of the paths from domBlock swap:
    // any block pred reachable from domBlock's true edge is a false pred of block,
    // and any block pred reachable from domBlock's false edge is a true pred of block.
    //
    // However, there are some exceptions:
    //
    // * It's possible for a pred to be reachable from both paths out of domBlock;
    // if so, we can't jump thread that pred.
    //
    // * It's also possible that a pred can't branch directly to a successor as
    // it might violate EH region constraints. Since this causes the same issues
    // as an ambiguous pred we'll just classify these as ambiguous too.
    //
    // * It's also possible to have preds with implied eh flow to the current
    // block, eg a catch return, and so we won't see either path reachable.
    // We'll handle those as ambiguous as well.
    //
    // * It's also possible that the pred is a switch; we will treat switch
    // preds as ambiguous as well.
    //
    // If there are ambiguous preds they will continue to flow into the
    // unaltered block, while true and false preds will flow to the appropriate
    // successors directly.
    //
    BasicBlock* const domTrueSuccessor  = domIsSameRelop ? domBlock->GetTrueTarget() : domBlock->GetFalseTarget();
    BasicBlock* const domFalseSuccessor = domIsSameRelop ? domBlock->GetFalseTarget() : domBlock->GetTrueTarget();
    JumpThreadInfo    jti(this, block);

    for (BasicBlock* const predBlock : block->PredBlocks())
    {
        jti.m_numPreds++;

        // Treat switch preds as ambiguous for now.
        //
        if (predBlock->KindIs(BBJ_SWITCH))
        {
            JITDUMP(FMT_BB " is a switch pred\n", predBlock->bbNum);
            BitVecOps::AddElemD(&jti.traits, jti.m_ambiguousPreds, predBlock->bbPostorderNum);
            jti.m_numAmbiguousPreds++;
            continue;
        }

        const bool isTruePred =
            (predBlock == domBlock) ? (domTrueSuccessor == block) : optReachable(domTrueSuccessor, predBlock, domBlock);
        const bool isFalsePred = (predBlock == domBlock) ? (domFalseSuccessor == block)
                                                         : optReachable(domFalseSuccessor, predBlock, domBlock);

        if (isTruePred == isFalsePred)
        {
            // Either both dom successors reach, or neither reaches.
            //
            // We should rarely see (false,false) given that optReachable is returning
            // up to date results, but as we optimize we create unreachable blocks,
            // and that can lead to cases where we can't find paths. That means we may be
            // optimizing code that is now unreachable, but attempts to fix or avoid doing that
            // lead to more complications, and it isn't that common. So we tolerate it.
            //
            JITDUMP(FMT_BB " is an ambiguous pred\n", predBlock->bbNum);
            BitVecOps::AddElemD(&jti.traits, jti.m_ambiguousPreds, predBlock->bbPostorderNum);
            jti.m_numAmbiguousPreds++;
            continue;
        }

        if (isTruePred)
        {
            if (!BasicBlock::sameEHRegion(predBlock, jti.m_trueTarget))
            {
                JITDUMP(FMT_BB " is an eh constrained pred\n", predBlock->bbNum);
                jti.m_numAmbiguousPreds++;
                BitVecOps::AddElemD(&jti.traits, jti.m_ambiguousPreds, predBlock->bbPostorderNum);
                continue;
            }

            jti.m_numTruePreds++;
            BitVecOps::AddElemD(&jti.traits, jti.m_truePreds, predBlock->bbPostorderNum);
            JITDUMP(FMT_BB " is a true pred\n", predBlock->bbNum);
        }
        else
        {
            assert(isFalsePred);

            if (!BasicBlock::sameEHRegion(predBlock, jti.m_falseTarget))
            {
                JITDUMP(FMT_BB " is an eh constrained pred\n", predBlock->bbNum);
                BitVecOps::AddElemD(&jti.traits, jti.m_ambiguousPreds, predBlock->bbPostorderNum);
                jti.m_numAmbiguousPreds++;
                continue;
            }

            jti.m_numFalsePreds++;
            JITDUMP(FMT_BB " is a false pred\n", predBlock->bbNum);
        }
    }

    // Do the optimization.
    //
    return optJumpThreadCore(jti);
}

//------------------------------------------------------------------------
// optJumpThreadPhi: attempt jump threading by disambiguating through phis.
//
// Arguments:
//   block - block with relop we're trying to optimize
//   tree - relop we're trying to optimize
//   treeNormVN - liberal normal VN from the relop
//
// Returns:
//   True if the branch was optimized.
//
bool Compiler::optJumpThreadPhi(BasicBlock* block, GenTree* tree, ValueNum treeNormVN)
{
    // First see if block is eligible for threading.
    //
    const JumpThreadCheckResult check = optJumpThreadCheck(block, /* domBlock*/ nullptr);
    if (check == JumpThreadCheckResult::CannotThread)
    {
        return false;
    }

    // We expect the controlling predicate to be a relop and so be a func app with two args.
    //
    // We should have screened out constants already. Might want to check if some other kind
    // of leaf can meaningfully make it here.
    //
    VNFuncApp treeNormVNFuncApp;
    if (!vnStore->GetVNFunc(treeNormVN, &treeNormVNFuncApp) || !(treeNormVNFuncApp.m_arity == 2))
    {
        return false;
    }

    // Bypass handler blocks, as they can have unusual PHI args.
    // In particular multiple SSA defs coming from the same block.
    //
    if (bbIsHandlerBeg(block))
    {
        return false;
    }

    // Find occurrences of phi def VNs in the relop VN.
    // We currently just do one level of func destructuring.
    //
    unsigned funcArgToPhiLocalMap[]   = {BAD_VAR_NUM, BAD_VAR_NUM};
    GenTree* funcArgToPhiDefNodeMap[] = {nullptr, nullptr};
    bool     foundPhiDef              = false;

    for (int i = 0; i < 2; i++)
    {
        const ValueNum phiDefVN = treeNormVNFuncApp.m_args[i];
        VNPhiDef       phiDef;
        if (!vnStore->GetPhiDef(phiDefVN, &phiDef))
        {
            // This input is not a phi def. If it's a func app it might depend on
            // transitively on a phi def; consider a general search utility.
            //
            continue;
        }

        // The PhiDef args tell us which local and which SSA def of that local.
        //
        const unsigned lclNum    = phiDef.LclNum;
        const unsigned ssaDefNum = phiDef.SsaDef;
        JITDUMP("... JT-PHI [interestingVN] in " FMT_BB " relop %s operand VN is PhiDef for V%02u\n", block->bbNum,
                i == 0 ? "first" : "second", lclNum, ssaDefNum);
        if (!foundPhiDef)
        {
            DISPTREE(tree);
        }

        // Find the PHI for lclNum local in the current block.
        //
        GenTree* phiNode = nullptr;
        for (Statement* const stmt : block->Statements())
        {
            // If the tree is not an SSA def, break out of the loop: we're done.
            if (!stmt->IsPhiDefnStmt())
            {
                break;
            }

            GenTreeLclVar* const phiDefNode = stmt->GetRootNode()->AsLclVar();
            assert(phiDefNode->IsPhiDefn());

            if (phiDefNode->GetLclNum() == lclNum)
            {
                if (phiDefNode->GetSsaNum() == ssaDefNum)
                {
                    funcArgToPhiLocalMap[i]   = lclNum;
                    funcArgToPhiDefNodeMap[i] = phiDefNode;
                    foundPhiDef               = true;
                    JITDUMP("Found local PHI [%06u] for V%02u\n", dspTreeID(phiDefNode), lclNum);
                }
                else
                {
                    // Relop input is phi def from some other block.
                    //
                    break;
                }
            }
        }
    }

    if (!foundPhiDef)
    {
        // No usable PhiDef VNs in the relop's VN.
        //
        JITDUMP("No usable PhiDef VNs\n");
        return false;
    }

    // At least one relop input depends on a local phi. Walk pred by pred and
    // see if the relop value is correlated with the pred.
    //
    JumpThreadInfo jti(this, block);
    jti.m_isPhiBased = true;

    for (BasicBlock* const predBlock : block->PredBlocks())
    {
        jti.m_numPreds++;

        // Find VNs for the relevant phi inputs from this block.
        //
        ValueNum newRelopArgs[] = {treeNormVNFuncApp.m_args[0], treeNormVNFuncApp.m_args[1]};
        bool     updatedArg     = false;

        for (int i = 0; i < 2; i++)
        {
            if (funcArgToPhiLocalMap[i] == BAD_VAR_NUM)
            {
                // this relop VN arg not phi dependent
                continue;
            }

            GenTree* const    phiDef = funcArgToPhiDefNodeMap[i];
            GenTreePhi* const phi    = phiDef->AsLclVar()->Data()->AsPhi();
            for (GenTreePhi::Use& use : phi->Uses())
            {
                GenTreePhiArg* const phiArgNode = use.GetNode()->AsPhiArg();
                assert(phiArgNode->GetLclNum() == funcArgToPhiLocalMap[i]);

                if (phiArgNode->gtPredBB == predBlock)
                {
                    ValueNum phiArgVN = phiArgNode->GetVN(VNK_Liberal);

                    // We sometimes see cases where phi args do not have VNs.
                    // (VN works in RPO, so PHIs from back edges won't have VNs.
                    //
                    if (phiArgVN != ValueNumStore::NoVN)
                    {
                        newRelopArgs[i] = phiArgVN;
                        updatedArg      = true;
                        break;
                    }
                }
            }
        }

        // We may not find predBlock in the phi args, as we only have one phi
        // arg per ssa num, not one per pred.
        //
        // See SsaBuilder::AddPhiArgsToSuccessors.
        //
        if (!updatedArg)
        {
            JITDUMP("Could not map phi inputs from pred " FMT_BB "\n", predBlock->bbNum);
            JITDUMP(FMT_BB " is an ambiguous pred\n", predBlock->bbNum);
            BitVecOps::AddElemD(&jti.traits, jti.m_ambiguousPreds, predBlock->bbPostorderNum);
            jti.m_numAmbiguousPreds++;
            continue;
        }

        // We have a refined set of args for the relop VN for this
        // pred. See if that simplifies the relop.
        //
        const ValueNum substVN =
            vnStore->VNForFunc(tree->TypeGet(), treeNormVNFuncApp.m_func, newRelopArgs[0], newRelopArgs[1]);

        JITDUMP("... substituting (" FMT_VN "," FMT_VN ") for (" FMT_VN "," FMT_VN ") in " FMT_VN " gives " FMT_VN "\n",
                newRelopArgs[0], newRelopArgs[1], treeNormVNFuncApp.m_args[0], treeNormVNFuncApp.m_args[1], treeNormVN,
                substVN);

        // If this VN is constant, we're all set!
        //
        // Note there are other cases we could possibly handle here, say if the substituted
        // VN not constant but is related to some dominating relop VN.
        //
        if (vnStore->IsVNConstant(substVN))
        {
            const bool relopIsTrue = (substVN != vnStore->VNZeroForType(TYP_INT));
            JITDUMP("... substituted VN implies relop is %d when coming from pred " FMT_BB "\n", relopIsTrue,
                    predBlock->bbNum);

            if (relopIsTrue)
            {
                if (!BasicBlock::sameEHRegion(predBlock, jti.m_trueTarget))
                {
                    JITDUMP(FMT_BB " is an eh constrained pred\n", predBlock->bbNum);
                    jti.m_numAmbiguousPreds++;
                    BitVecOps::AddElemD(&jti.traits, jti.m_ambiguousPreds, predBlock->bbPostorderNum);
                    continue;
                }

                jti.m_numTruePreds++;
                BitVecOps::AddElemD(&jti.traits, jti.m_truePreds, predBlock->bbPostorderNum);
                JITDUMP(FMT_BB " is a true pred\n", predBlock->bbNum);
            }
            else
            {
                if (!BasicBlock::sameEHRegion(predBlock, jti.m_falseTarget))
                {
                    JITDUMP(FMT_BB " is an eh constrained pred\n", predBlock->bbNum);
                    BitVecOps::AddElemD(&jti.traits, jti.m_ambiguousPreds, predBlock->bbPostorderNum);
                    jti.m_numAmbiguousPreds++;
                    continue;
                }

                jti.m_numFalsePreds++;
                JITDUMP(FMT_BB " is a false pred\n", predBlock->bbNum);
            }
        }
        else
        {
            JITDUMP(FMT_BB " is an ambiguous pred\n", predBlock->bbNum);
            BitVecOps::AddElemD(&jti.traits, jti.m_ambiguousPreds, predBlock->bbPostorderNum);
            jti.m_numAmbiguousPreds++;

            // If this was the first ambiguous pred, remember the substVN
            // and the block that provided it, in case we can use later to
            // sharpen the predicate's liberal normal VN.
            //
            if ((jti.m_numAmbiguousPreds == 1) && (substVN != treeNormVN))
            {
                assert(jti.m_ambiguousVN == ValueNumStore::NoVN);
                assert(jti.m_ambiguousVNBlock == nullptr);

                jti.m_ambiguousVN      = substVN;
                jti.m_ambiguousVNBlock = predBlock;
            }

            continue;
        }
    }

    if ((check == JumpThreadCheckResult::NeedsPhiUseResolution) && !optCanRewritePhiUses(jti))
    {
        JITDUMP(FMT_BB " has global phi uses we cannot safely account for; no phi-based threading\n", block->bbNum);
        return false;
    }

    // Do the optimization.
    //
    return optJumpThreadCore(jti);
}

//------------------------------------------------------------------------
// optJumpThreadCore: restructure block flow based on jump thread information
//
// Arguments:
//   jti - information on how to jump thread this block
//
// Returns:
//   True if the branch was optimized.
//
bool Compiler::optJumpThreadCore(JumpThreadInfo& jti)
{
    // All preds should have been classified.
    //
    assert(jti.m_numPreds == jti.m_numTruePreds + jti.m_numFalsePreds + jti.m_numAmbiguousPreds);

    // There should be at least one pred that can bypass block.
    //
    if ((jti.m_numTruePreds == 0) && (jti.m_numFalsePreds == 0))
    {
        // This is possible, but should be rare.
        //
        JITDUMP(FMT_BB " only has ambiguous preds, not jump threading\n", jti.m_block->bbNum);
        return false;
    }

    // We should be good to go
    //
    JITDUMP("Optimizing via jump threading\n");

    for (JumpThreadInfo::PhiUse& phiUse : jti.m_phiUses.BottomUpOrder())
    {
        if (phiUse.m_replacementSsaNum == SsaConfig::RESERVED_SSA_NUM)
        {
            continue;
        }

        GenTreeLclVarCommon* const use       = phiUse.m_use;
        unsigned const             oldSsaNum = use->GetSsaNum();
        unsigned const             lclNum    = use->GetLclNum();

        assert(oldSsaNum != phiUse.m_replacementSsaNum);

        JITDUMP("Updating [%06u] in " FMT_BB " from u:%u to u:%u\n", dspTreeID(use), phiUse.m_block->bbNum, oldSsaNum,
                phiUse.m_replacementSsaNum);

        LclSsaVarDsc* const replacementSsaDef = lvaGetDesc(lclNum)->GetPerSsaData(phiUse.m_replacementSsaNum);

        use->SetSsaNum(phiUse.m_replacementSsaNum);

        // Keep the use's value number in sync with the rewritten SSA def.
        //
        if (use->gtVNPair != replacementSsaDef->m_vnPair)
        {
            ValueNumPair newVNPair = replacementSsaDef->m_vnPair;

            // If this is a field use, get the proper field VN.

            if (use->OperIs(GT_LCL_FLD))
            {
                GenTreeLclFld* const lclFld = use->AsLclFld();

                newVNPair = vnStore->VNPairForLoad(replacementSsaDef->m_vnPair, lvaLclValueSize(lclNum),
                                                   lclFld->TypeGet(), lclFld->GetLclOffs(), lclFld->GetValueSize());
            }
            else
            {
                assert(use->OperIs(GT_LCL_VAR));
            }

            JITDUMP("Updating [%06u] VN from ", dspTreeID(use));
            JITDUMPEXEC(vnpPrint(use->gtVNPair, 1));
            JITDUMP(" to ");
            JITDUMPEXEC(vnpPrint(newVNPair, 1));
            JITDUMP("\n");

            use->SetVNs(newVNPair);

            GenTree* node   = use;
            GenTree* parent = node->gtGetParent(nullptr);

            while ((parent != nullptr) && parent->OperIs(GT_COMMA) && (parent->AsOp()->gtOp2 == node))
            {
                JITDUMP(" Updating COMMA parent VN [%06u]\n", dspTreeID(parent));
                ValueNumPair op1Xvnp = vnStore->VNPExceptionSet(parent->AsOp()->gtOp1->gtVNPair);
                parent->SetVNs(vnStore->VNPWithExc(parent->AsOp()->gtOp2->gtVNPair, op1Xvnp));
                node   = parent;
                parent = node->gtGetParent(nullptr);
            }
        }

        replacementSsaDef->AddUse(phiUse.m_block);
    }

    // If there were ambiguous preds, and all agreed on the incoming SSA def
    // for some local, we can remove the associated PHI.
    //
    for (Statement* const phiDefStmt : jti.m_phiDefsToRemove.BottomUpOrder())
    {
        JITDUMP("Removing redundant phi def from " FMT_BB "\n", jti.m_block->bbNum);
        fgRemoveStmt(jti.m_block, phiDefStmt);
    }

    bool setNoCseIn = false;

    // If this is a phi-based threading, and the block we're bypassing has
    // a memory phi, mark the successor blocks with BBF_NO_CSE_IN so we can
    // block unsound CSE propagation.
    //
    if (jti.m_isPhiBased)
    {
        for (MemoryKind memoryKind : allMemoryKinds())
        {
            if ((memoryKind == ByrefExposed) && byrefStatesMatchGcHeapStates)
            {
                continue;
            }

            if (jti.m_block->bbMemorySsaPhiFunc[memoryKind] != nullptr)
            {
                JITDUMP(FMT_BB " has %s memory phi; will be marking blocks with BBF_NO_CSE_IN\n", jti.m_block->bbNum,
                        memoryKindNames[memoryKind]);
                setNoCseIn = true;
                break;
            }
        }
    }

    // Now reroute the flow from the predecessors.
    // If this pred is in the set that will reuse block, do nothing.
    // Else revise pred to branch directly to the appropriate successor of block.
    //
    bool modifiedProfile = false;
    for (FlowEdge* const predEdge : jti.m_block->PredEdgesEditing())
    {
        BasicBlock* const predBlock = predEdge->getSourceBlock();

        // If this was an ambiguous pred, skip.
        //
        if (BitVecOps::IsMember(&jti.traits, jti.m_ambiguousPreds, predBlock->bbPostorderNum))
        {
            if (setNoCseIn && !jti.m_block->HasFlag(BBF_NO_CSE_IN))
            {
                JITDUMP(FMT_BB " => BBF_NO_CSE_IN\n", jti.m_block->bbNum);
                jti.m_block->SetFlags(BBF_NO_CSE_IN);
            }
            continue;
        }

        const bool isTruePred = BitVecOps::IsMember(&jti.traits, jti.m_truePreds, predBlock->bbPostorderNum);

        // Jump to the appropriate successor.
        //
        BasicBlock* newTarget = nullptr;
        if (isTruePred)
        {
            JITDUMP("Jump flow from pred " FMT_BB " -> " FMT_BB
                    " implies predicate true; we can safely redirect flow to be " FMT_BB " -> " FMT_BB "\n",
                    predBlock->bbNum, jti.m_block->bbNum, predBlock->bbNum, jti.m_trueTarget->bbNum);
            newTarget = jti.m_trueTarget;
        }
        else
        {
            JITDUMP("Jump flow from pred " FMT_BB " -> " FMT_BB
                    " implies predicate false; we can safely redirect flow to be " FMT_BB " -> " FMT_BB "\n",
                    predBlock->bbNum, jti.m_block->bbNum, predBlock->bbNum, jti.m_falseTarget->bbNum);
            newTarget = jti.m_falseTarget;
        }

        fgReplaceJumpTarget(predBlock, jti.m_block, newTarget);

        if (setNoCseIn && !newTarget->HasFlag(BBF_NO_CSE_IN))
        {
            JITDUMP(FMT_BB " => BBF_NO_CSE_IN\n", newTarget->bbNum);
            newTarget->SetFlags(BBF_NO_CSE_IN);
        }

        if (predBlock->hasProfileWeight())
        {
            newTarget->increaseBBProfileWeight(predEdge->getLikelyWeight());
            modifiedProfile = true;
        }
    }

    // jti.m_block is unreachable, but we won't remove it until the next flowgraph simplification pass.
    // Mark the profile as inconsistent to pass the post-phase checks.
    if (modifiedProfile)
    {
        JITDUMP("RBO: " FMT_BB
                " is now unreachable, and flow into its successors needs to be removed. Data %s inconsistent.\n",
                jti.m_block->bbNum, fgPgoConsistent ? "is now" : "was already");
        fgPgoConsistent = false;
    }

    // If block didn't get fully optimized, and now has just one pred, see if
    // we can sharpen the predicate's VN.
    //
    // (Todo, perhaps: revisit all the uses of the old SSA def, update to the
    // surviving ssa input, and update all the value numbers...)
    //
    BasicBlock* const ambBlock = jti.m_ambiguousVNBlock;
    if ((ambBlock != nullptr) && jti.m_block->KindIs(BBJ_COND) && (jti.m_block->GetUniquePred(this) == ambBlock))
    {
        JITDUMP(FMT_BB " has just one remaining predecessor " FMT_BB "\n", jti.m_block->bbNum, ambBlock->bbNum);

        Statement* const stmt = jti.m_block->lastStmt();
        assert(stmt != nullptr);
        GenTree* const jumpTree = stmt->GetRootNode();
        assert(jumpTree->OperIs(GT_JTRUE));
        GenTree* const tree = jumpTree->AsOp()->gtOp1;
        assert(tree->OperIsCompare());

        ValueNum treeOldVN  = tree->GetVN(VNK_Liberal);
        ValueNum treeNormVN = ValueNumStore::NoVN;
        ValueNum treeExcVN  = ValueNumStore::NoVN;
        vnStore->VNUnpackExc(treeOldVN, &treeNormVN, &treeExcVN);
        ValueNum treeNewVN = vnStore->VNWithExc(jti.m_ambiguousVN, treeExcVN);
        tree->SetVN(VNK_Liberal, treeNewVN);

        JITDUMP("Updating [%06u] liberal VN from " FMT_VN " to " FMT_VN "\n", dspTreeID(tree), treeOldVN, treeNewVN);
    }

    // We optimized.
    //
    Metrics.JumpThreadingsPerformed++;
    fgModified = true;
    return true;
}

//------------------------------------------------------------------------
// optRedundantRelop: see if the value of tree is redundant given earlier
//   relops in this block.
//
// Arguments:
//    block - block of interest (BBJ_COND)
//
// Returns:
//    true, if changes were made.
//
// Notes:
//
// Here's a walkthrough of how this operates. Given a block like
//
// STMT00388 (IL 0x30D...  ???)
//  *  STORE_LCL_VAR ref    V121 tmp97       d:1
//   \--*  IND       ref    <l:$9d3, c:$9d4>
//       \--*  LCL_VAR   byref  V116 tmp92       u:1 (last use) Zero Fseq[m_task] $18c
//
// STMT00390 (IL 0x30D...  ???)
//  *  STORE_LCL_VAR int    V123 tmp99       d:1
//  \--*  NE        int    <l:$8ff, c:$a02>
//     +--*  LCL_VAR   ref    V121 tmp97       u:1 <l:$2c8, c:$99f>
//     \--*  CNS_INT   ref    null $VN.Null
//
// STMT00391
//  *  STORE_LCL_VAR ref    V124 tmp100      d:1
//  \--*  IND       ref    $133
//     \--*  CNS_INT(h) long   0x31BD3020 [ICON_STR_HDL] $34f
//
// STMT00392
//  *  JTRUE     void
//  \--*  NE        int    <l:$8ff, c:$a02>
//     +--*  LCL_VAR   int    V123 tmp99       u:1 (last use) <l:$8ff, c:$a02>
//     \--*  CNS_INT   int    0 $40
//
// We will first consider STMT00391. It is a local store but the value's VN
// isn't related to $8ff. So we continue searching and add V124 to the array
// of defined locals.
//
// Next we consider STMT00390. It is a local store and the value's VN is the
// same, $8ff. So this compare is a fwd-sub candidate. We check if any local
// in the value tree in the defined locals array. The answer is no. So the
// value tree can be safely forwarded in place of the compare in STMT00392.
// We check if V123 is live out of the block. The answer is no. So this value
// tree becomes the candidate tree. We add V123 to the array of defined locals
// and keep searching.
//
// Next we consider STMT00388, It is a local store but the value's VN isn't
// related to $8ff. So we continue searching and add V121 to the array of
// defined locals.
//
// We reach the end of the block and stop searching.
//
// Since we found a viable candidate, we clone it and substitute into the jump:
//
// STMT00388 (IL 0x30D...  ???)
//  *  STORE_LCL_VAR ref    V121 tmp97       d:1
//   \--*  IND       ref    <l:$9d3, c:$9d4>
//       \--*  LCL_VAR   byref  V116 tmp92       u:1 (last use) Zero Fseq[m_task] $18c
//
// STMT00390 (IL 0x30D...  ???)
//  *  STORE_LCL_VAR int    V123 tmp99       d:1
//  \--*  NE        int    <l:$8ff, c:$a02>
//     +--*  LCL_VAR   ref    V121 tmp97       u:1 <l:$2c8, c:$99f>
//     \--*  CNS_INT   ref    null $VN.Null
//
// STMT00391
//  *  STORE_LCL_VAR ref    V124 tmp100      d:1
//  \--*  IND       ref    $133
//     \--*  CNS_INT(h) long   0x31BD3020 [ICON_STR_HDL] $34f
//
// STMT00392
//  *  JTRUE     void
//  \--*  NE        int    <l:$8ff, c:$a02>
//     +--*  LCL_VAR   ref    V121 tmp97       u:1 <l:$2c8, c:$99f>
//     \--*  CNS_INT   ref    null $VN.Null
//
// We anticipate that STMT00390 will become dead code, and if and so we've
// eliminated one of the two compares in the block.
//
bool Compiler::optRedundantRelop(BasicBlock* const block)
{
    Statement* const stmt = block->lastStmt();

    if (stmt == nullptr)
    {
        return false;
    }

    // If there's just one statement, bail.
    //
    if (stmt == block->firstStmt())
    {
        return false;
    }

    GenTree* const jumpTree = stmt->GetRootNode();

    if (!jumpTree->OperIs(GT_JTRUE))
    {
        return false;
    }

    GenTree* const tree = jumpTree->AsOp()->gtOp1;

    if (!tree->OperIsCompare())
    {
        return false;
    }

    // If tree has side effects other than GTF_EXCEPT, bail.
    //
    if ((tree->gtFlags & GTF_SIDE_EFFECT) != 0)
    {
        if ((tree->gtFlags & GTF_SIDE_EFFECT) != GTF_EXCEPT)
        {
            return false;
        }
    }

    // If relop's value is known, bail.
    //
    const ValueNum treeVN = vnStore->VNNormalValue(tree->GetVN(VNK_Liberal));

    if (vnStore->IsVNConstant(treeVN))
    {
        JITDUMP(" -- no, jump tree cond is constant\n");
        return false;
    }

    // Save off the jump tree's liberal exceptional VN.
    //
    const ValueNum treeExcVN = vnStore->VNExceptionSet(tree->GetVN(VNK_Liberal));

    JITDUMP("\noptRedundantRelop in " FMT_BB "; jump tree is\n", block->bbNum);
    DISPTREE(jumpTree);

    // We're going to search back to find the earliest tree in block that
    //  * makes the current relop redundant;
    //  * can safely and profitably forward substituted to the jump.
    //
    Statement*                      prevStmt            = stmt;
    GenTree*                        candidateTree       = nullptr;
    Statement*                      candidateStmt       = nullptr;
    ValueNumStore::VN_RELATION_KIND candidateVnRelation = ValueNumStore::VN_RELATION_KIND::VRK_Same;
    bool                            sideEffect          = false;

    // We need to keep track of which locals might be killed by
    // the trees between the expression we want to forward substitute
    // and the jump.
    //
    // We don't use a varset here because we are indexing by local ID,
    // not by tracked index.
    //
    // The table size here also implicitly limits how far back we'll search.
    //
    enum
    {
        DEFINED_LOCALS_SIZE = 10
    };
    unsigned definedLocals[DEFINED_LOCALS_SIZE];
    unsigned definedLocalsCount = 0;

    while (true)
    {
        // If we've run a cross a side effecting pred tree, stop looking.
        //
        if (sideEffect)
        {
            break;
        }

        prevStmt = prevStmt->GetPrevStmt();

        // Backwards statement walks wrap around, so if we get
        // back to stmt we've seen everything there is to see.
        //
        if (prevStmt == stmt)
        {
            break;
        }

        // We are looking for STORE_LCL_VAR(...)
        //
        GenTree* const prevTree = prevStmt->GetRootNode();

        JITDUMP(" ... checking previous tree\n");
        DISPTREE(prevTree);

        // Ignore nops.
        //
        if (prevTree->OperIs(GT_NOP))
        {
            continue;
        }

        if (!prevTree->OperIs(GT_STORE_LCL_VAR))
        {
            JITDUMP(" -- prev tree not STORE_LCL_VAR\n");
            break;
        }

        GenTree* const prevTreeValue = prevTree->AsLclVar()->Data();

        // If prevTree has side effects, bail, unless it is in the immediately preceding statement.
        // We'll handle exceptional side effects with VNs below.
        //
        if (((prevTree->gtFlags & (GTF_CALL | GTF_ORDER_SIDEEFF)) != 0) || ((prevTreeValue->gtFlags & GTF_ASG) != 0))
        {
            if (prevStmt->GetNextStmt() != stmt)
            {
                JITDUMP(" -- prev tree has side effects and is not next to jumpTree\n");
                break;
            }

            JITDUMP(" -- prev tree has side effects, allowing as prev tree is immediately before jumpTree\n");
            sideEffect = true;
        }

        // If we are seeing PHIs we have run out of interesting stmts.
        //
        if (prevTreeValue->OperIs(GT_PHI))
        {
            JITDUMP(" -- prev tree is a phi\n");
            break;
        }

        // Figure out what local is defined here.
        //
        const unsigned   prevTreeLclNum = prevTree->AsLclVarCommon()->GetLclNum();
        LclVarDsc* const prevTreeLclDsc = lvaGetDesc(prevTreeLclNum);

        // If local is not tracked, assume we can't safely reason about interference
        // or liveness.
        //
        if (!prevTreeLclDsc->lvTracked)
        {
            JITDUMP(" -- prev tree defs untracked V%02u\n", prevTreeLclNum);
            break;
        }

        // If we've run out of room to keep track of defined locals, bail.
        //
        if (definedLocalsCount >= DEFINED_LOCALS_SIZE)
        {
            JITDUMP(" -- ran out of space for tracking kills\n");
            break;
        }

        definedLocals[definedLocalsCount++] = prevTreeLclNum;

        // If the normal liberal VN of RHS is the normal liberal VN of the current tree, or is "related",
        // consider forward sub.
        //
        const ValueNum                  domCmpVN        = vnStore->VNNormalValue(prevTreeValue->GetVN(VNK_Liberal));
        bool                            matched         = false;
        ValueNumStore::VN_RELATION_KIND vnRelationMatch = ValueNumStore::VN_RELATION_KIND::VRK_Same;

        for (auto vnRelation : s_vnRelations)
        {
            const ValueNum relatedVN = vnStore->GetRelatedRelop(domCmpVN, vnRelation);
            if ((relatedVN != ValueNumStore::NoVN) && (relatedVN == treeVN))
            {
                vnRelationMatch = vnRelation;
                matched         = true;
                break;
            }
        }

        if (!matched)
        {
            JITDUMP(" -- prev tree VN is not related\n");
            continue;
        }

        JITDUMP("  -- prev tree has relop with %s liberal VN\n", ValueNumStore::VNRelationString(vnRelationMatch));

        // If the jump tree VN has exceptions, verify that the value tree has a superset.
        //
        if (treeExcVN != vnStore->VNForEmptyExcSet())
        {
            const ValueNum prevTreeExcVN = vnStore->VNExceptionSet(prevTreeValue->GetVN(VNK_Liberal));

            if (!vnStore->VNExcIsSubset(prevTreeExcVN, treeExcVN))
            {
                JITDUMP(" -- prev tree does not anticipate all jump tree exceptions\n");
                break;
            }
        }

        // See if we can safely move a copy of prevTreeValue later, to replace tree.
        // We can, if none of its lcls are killed.
        //
        bool interferes = false;

        for (unsigned int i = 0; i < definedLocalsCount; i++)
        {
            if (gtTreeHasLocalRead(prevTreeValue, definedLocals[i]))
            {
                JITDUMP(" -- prev tree ref to V%02u interferes\n", definedLocals[i]);
                interferes = true;
                break;
            }
        }

        if (interferes)
        {
            break;
        }

        if (gtMayHaveStoreInterference(prevTreeValue, tree))
        {
            JITDUMP(" -- prev tree has an embedded store that interferes with [%06u]\n", dspTreeID(tree));
            break;
        }

        // Heuristic: only forward sub a relop
        //
        if (!prevTreeValue->OperIsCompare())
        {
            JITDUMP(" -- prev tree is not relop\n");
            continue;
        }

        // If the lcl defined here is live out, forward sub is problematic.
        // We'll either create a redundant tree (as the original won't be dead)
        // or lose the def (if we actually move the RHS tree).
        //
        if (VarSetOps::IsMember(this, block->bbLiveOut, prevTreeLclDsc->lvVarIndex))
        {
            JITDUMP(" -- prev tree lcl V%02u is live-out\n", prevTreeLclNum);
            continue;
        }

        if ((prevTreeValue->gtFlags & GTF_GLOB_REF) != 0)
        {
            bool hasExtraUses = false;

            // We can only allow duplicating a GTF_GLOB_REF tree if we can
            // prove that the local dies as a result -- otherwise we would
            // introduce data races here. We have already checked live-out
            // above, so the remaining check is to verify that all uses of the
            // local are in the terminating statement that we will be
            // replacing.
            for (Statement* cur = prevStmt->GetNextStmt(); cur != stmt; cur = cur->GetNextStmt())
            {
                if (gtTreeHasLocalRead(cur->GetRootNode(), prevTreeLclNum))
                {
                    JITDUMP("-- prev tree has GTF_GLOB_REF and " FMT_STMT " has an interfering use\n", cur->GetID());
                    hasExtraUses = true;
                    break;
                }
            }

            if (hasExtraUses)
            {
                continue;
            }
        }

        JITDUMP(" -- prev tree is viable candidate for relop fwd sub!\n");
        candidateTree       = prevTreeValue;
        candidateStmt       = prevStmt;
        candidateVnRelation = vnRelationMatch;
    }

    if (candidateTree == nullptr)
    {
        return false;
    }

    GenTree* substituteTree = nullptr;
    bool     usedCopy       = false;

    if (candidateStmt->GetNextStmt() == stmt)
    {
        // We are going forward-sub candidateTree
        //
        substituteTree = candidateTree;
    }
    else
    {
        // We going to forward-sub a copy of candidateTree
        //
        assert(!sideEffect);
        substituteTree = gtCloneExpr(candidateTree);
        usedCopy       = true;
    }

    // If we need the reverse compare, make it so.
    // We also need to set a proper VN.
    //
    if ((candidateVnRelation == ValueNumStore::VN_RELATION_KIND::VRK_Reverse) ||
        (candidateVnRelation == ValueNumStore::VN_RELATION_KIND::VRK_SwapReverse))
    {
        // Copy the vn info as it will be trashed when we change the oper.
        //
        ValueNumPair origVNP = substituteTree->gtVNPair;

        // Update the tree. Note we don't actually swap operands...?
        //
        substituteTree->SetOper(GenTree::ReverseRelop(substituteTree->OperGet()));

        // Compute the right set of VNs for this new tree.
        //
        ValueNum origNormConVN = vnStore->VNConservativeNormalValue(origVNP);
        ValueNum origNormLibVN = vnStore->VNLiberalNormalValue(origVNP);
        ValueNum newNormConVN  = vnStore->GetRelatedRelop(origNormConVN, ValueNumStore::VN_RELATION_KIND::VRK_Reverse);
        ValueNum newNormLibVN  = vnStore->GetRelatedRelop(origNormLibVN, ValueNumStore::VN_RELATION_KIND::VRK_Reverse);
        ValueNumPair newNormalVNP(newNormLibVN, newNormConVN);
        ValueNumPair origExcVNP = vnStore->VNPExceptionSet(origVNP);
        ValueNumPair newVNP     = vnStore->VNPWithExc(newNormalVNP, origExcVNP);

        substituteTree->SetVNs(newVNP);
    }

    // This relop is now a subtree of a jump.
    //
    substituteTree->gtFlags |= (GTF_RELOP_JMP_USED | GTF_DONT_CSE);

    // Swap in the new tree.
    //
    GenTree** const treeUse = &(jumpTree->AsOp()->gtOp1);
    jumpTree->ReplaceOperand(treeUse, substituteTree);
    fgSetStmtSeq(stmt);
    gtUpdateStmtSideEffects(stmt);

    DEBUG_DESTROY_NODE(tree);

    // If we didn't forward sub a copy, the candidateStmt must be removed.
    //
    if (!usedCopy)
    {
        fgRemoveStmt(block, candidateStmt);

        // Make sure the removed store node isn't referenced by an SSA definition
        assert(candidateStmt->GetRootNode()->OperIs(GT_STORE_LCL_VAR));
        GenTreeLclVarCommon* const rootNode = candidateStmt->GetRootNode()->AsLclVarCommon();
        LclVarDsc* const           varDsc   = lvaGetDesc(rootNode);
        LclSsaVarDsc* const        defDsc   = varDsc->GetPerSsaData(rootNode->GetSsaNum());
        assert(defDsc->GetDefNode() == rootNode);
        defDsc->SetDefNode(nullptr);

        DEBUG_DESTROY_NODE(rootNode);
    }
    else
    {
        optRecordSsaUses(substituteTree, block);
    }

    JITDUMP(" -- done! new jump tree is\n");
    DISPTREE(jumpTree);

    return true;
}

//------------------------------------------------------------------------
// optReachable: see if there's a path from one block to another,
//   including paths involving EH flow.
//
// Arguments:
//    fromBlock     - starting block
//    toBlock       - ending block
//    excludedBlock - ignore paths that flow through this block
//
// Returns:
//    true if there is a path, false if there is no path
//
// Notes:
//    Like fgReachable, but computed on demand (and so accurate given
//    the current flow graph), and also considers paths involving EH.
//
//    This may overstate "true" reachability in methods where there are
//    finallies with multiple continuations.
//
bool Compiler::optReachable(BasicBlock* const fromBlock, BasicBlock* const toBlock, BasicBlock* const excludedBlock)
{
    ReachabilityResult result = optReachableWithBudget(fromBlock, toBlock, excludedBlock, nullptr);
    assert(result != ReachabilityResult::BudgetExceeded);
    return result == ReachabilityResult::Reachable;
}

//------------------------------------------------------------------------
// optReachableWithBudget: see if there's a path from one block to another,
//   including paths involving EH flow. Same as optReachable, but with a budget check.
//
// Arguments:
//    fromBlock     - starting block
//    toBlock       - ending block
//    excludedBlock - ignore paths that flow through this block
//    pBudget       - number of blocks to examine before returning BudgetExceeded
//
// Returns:
//    ReachabilityResult::Reachable if there is a path from fromBlock to toBlock,
//    ReachabilityResult::Unreachable if there is no such path,
//    ReachabilityResult::BudgetExceeded if we ran out of budget before finding either.
//
Compiler::ReachabilityResult Compiler::optReachableWithBudget(BasicBlock* const fromBlock,
                                                              BasicBlock* const toBlock,
                                                              BasicBlock* const excludedBlock,
                                                              int*              pBudget)
{
    if (fromBlock == toBlock)
    {
        return ReachabilityResult::Reachable;
    }

    if (optReachableBitVecTraits == nullptr)
    {
        optReachableBitVecTraits = new (this, CMK_Reachability) BitVecTraits(fgBBNumMax + 1, this);
        optReachableBitVec       = BitVecOps::MakeEmpty(optReachableBitVecTraits);
    }
    else
    {
        assert(BitVecTraits::GetSize(optReachableBitVecTraits) == fgBBNumMax + 1);
        BitVecOps::ClearD(optReachableBitVecTraits, optReachableBitVec);
    }

    ArrayStack<BasicBlock*> stack(getAllocator(CMK_Reachability));
    stack.Push(fromBlock);

    while (!stack.Empty())
    {
        BasicBlock* const nextBlock = stack.Pop();
        assert(nextBlock != toBlock);

        if (nextBlock == excludedBlock)
        {
            continue;
        }
        bool            budgetExceeded = false;
        BasicBlockVisit result =
            nextBlock->VisitAllSuccs(this, [this, toBlock, &stack, &budgetExceeded, pBudget](BasicBlock* succ) {
            if (succ == toBlock)
            {
                return BasicBlockVisit::Abort;
            }

            if ((pBudget != nullptr) && (--(*pBudget) <= 0))
            {
                budgetExceeded = true;
                return BasicBlockVisit::Abort;
            }

            if (BitVecOps::TryAddElemD(optReachableBitVecTraits, optReachableBitVec, succ->bbNum))
            {
                stack.Push(succ);
            }

            return BasicBlockVisit::Continue;
        });

        if (result == BasicBlockVisit::Abort)
        {
            return budgetExceeded ? ReachabilityResult::BudgetExceeded : ReachabilityResult::Reachable;
        }
    }

    return ReachabilityResult::Unreachable;
}
