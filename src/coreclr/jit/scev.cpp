// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// This file contains code to analyze how the value of induction variables
// evolve (scalar evolution analysis), and to turn them into the SCEV IR
// defined in scev.h. The analysis is inspired by "Michael Wolfe. 1992. Beyond
// induction variables." and also by LLVM's scalar evolution analysis.
//
// The main idea of scalar evolution nalysis is to give a closed form
// describing the value of tree nodes inside loops even when taking into
// account that they are changing on each loop iteration. This is useful for
// optimizations that want to reason about values of IR nodes inside loops,
// such as IV widening or strength reduction.
//
// To represent the possibility of evolution the SCEV IR includes the concept
// of an add recurrence <loop, start, step>, which describes a value that
// starts at "start" and changes by adding "step" at each iteration. The IR
// nodes that change in this way (or depend on something that changes in this
// way) are generally called induction variables.
//
// An add recurrence arises only when a local exists in the loop that is
// mutated in each iteration. Such a local will naturally end up with a phi
// node in the loop header. These locals are called primary (or basic)
// induction variables. The non-primary IVs (which always must depend on the
// primary IVs) are sometimes called secondary IVs.
//
// The job of the analysis is to go from a tree node to a SCEV node that
// describes its value (possibly taking its evolution into account). Note that
// SCEV nodes are immutable and the values they represent are _not_
// flow-dependent; that is, they don't exist at a specific location inside the
// loop, even though some particular tree node gave rise to that SCEV node. The
// analysis itself _is_ flow-dependent and guarantees that the Scev* returned
// describes the value that corresponds to what the tree node computes at its
// specific location. However, it would be perfectly legal for two trees at
// different locations in the loop to analyze to the same SCEV node (even
// potentially returning the same pointer). For example, in theory "i" and "j"
// in the following loop would both be represented by the same add recurrence
// <L, 0, 1>, and the analysis could even return the same Scev* for both of
// them, even if it does not today:
//
//   int i = 0;
//   while (true)
//   {
//     i++;
//     ...
//     int j = i - 1;
//   }
//
// Actually materializing the value of a SCEV node back into tree IR is not
// implemented yet, but generally would depend on the availability of tree
// nodes that compute the dependent values at the point where the IR is to be
// materialized.
//
// Besides the add recurrences the analysis itself is generally a
// straightforward translation from JIT IR into the SCEV IR. Creating the add
// recurrences requires paying attention to the structure of PHIs, and
// disambiguating the values coming from outside the loop and the values coming
// from the backedges.
//

#include "jitpch.h"

//------------------------------------------------------------------------
// GetConstantValue: If this SSA use refers to a constant, then fetch that
// constant.
//
// Parameters:
//   comp - Compiler instance
//   cns  - [out] Constant value; only valid if this function returns true.
//
// Returns:
//   True if this SSA use refers to a constant; otherwise false,
//
bool ScevLocal::GetConstantValue(Compiler* comp, int64_t* cns)
{
    LclVarDsc*           dsc     = comp->lvaGetDesc(LclNum);
    LclSsaVarDsc*        ssaDsc  = dsc->GetPerSsaData(SsaNum);
    GenTreeLclVarCommon* defNode = ssaDsc->GetDefNode();
    if ((defNode != nullptr) && defNode->Data()->OperIs(GT_CNS_INT, GT_CNS_LNG))
    {
        *cns = defNode->Data()->AsIntConCommon()->IntegralValue();
        return true;
    }

    return false;
}

//------------------------------------------------------------------------
// Scev::GetConstantValue: If this SCEV is always a constant (i.e. either an
// inline constant or an SSA use referring to a constant) then obtain that
// constant.
//
// Parameters:
//   comp - Compiler instance
//   cns  - [out] Constant value; only valid if this function returns true.
//
// Returns:
//   True if a constant could be extracted.
//
bool Scev::GetConstantValue(Compiler* comp, int64_t* cns)
{
    if (OperIs(ScevOper::Constant))
    {
        *cns = ((ScevConstant*)this)->Value;
        return true;
    }

    if (OperIs(ScevOper::Local))
    {
        return ((ScevLocal*)this)->GetConstantValue(comp, cns);
    }

    return false;
}

#ifdef DEBUG
//------------------------------------------------------------------------
// Dump: Print this scev node to stdout.
//
// Parameters:
//   comp - Compiler instance
//
void Scev::Dump(Compiler* comp)
{
    switch (Oper)
    {
        case ScevOper::Constant:
        {
            ScevConstant* cns = (ScevConstant*)this;
            printf("%zd", (ssize_t)cns->Value);
            break;
        }
        case ScevOper::Local:
        {
            ScevLocal* invariantLocal = (ScevLocal*)this;
            printf("V%02u.%u", invariantLocal->LclNum, invariantLocal->SsaNum);

            int64_t cns;
            if (invariantLocal->GetConstantValue(comp, &cns))
            {
                printf(" (%lld)", (long long)cns);
            }
            break;
        }
        case ScevOper::ZeroExtend:
        case ScevOper::SignExtend:
        {
            ScevUnop* unop = (ScevUnop*)this;
            printf("%cext<%d>(", unop->Oper == ScevOper::ZeroExtend ? 'z' : 's', genTypeSize(unop->Type) * 8);
            unop->Op1->Dump(comp);
            printf(")");
            break;
        }
        case ScevOper::Add:
        case ScevOper::Mul:
        case ScevOper::Lsh:
        {
            ScevBinop* binop = (ScevBinop*)this;
            printf("(");
            binop->Op1->Dump(comp);
            const char* op;
            switch (binop->Oper)
            {
                case ScevOper::Add:
                    op = "+";
                    break;
                case ScevOper::Mul:
                    op = "*";
                    break;
                case ScevOper::Lsh:
                    op = "<<";
                    break;
                default:
                    unreached();
            }
            printf(" %s ", op);
            binop->Op2->Dump(comp);
            printf(")");
            break;
        }
        case ScevOper::AddRec:
        {
            ScevAddRec* addRec = (ScevAddRec*)this;
            printf("<" FMT_LP, addRec->Loop->GetIndex());
            printf(", ");
            addRec->Start->Dump(comp);
            printf(", ");
            addRec->Step->Dump(comp);
            printf(">");
            break;
        }
        default:
            unreached();
    }
}
#endif

//------------------------------------------------------------------------
// ScalarEvolutionContext: Construct an instance of a context to do scalar evolution in.
//
// Parameters:
//   comp - Compiler instance
//
// Remarks:
//   After construction the context should be reset for a new loop by calling
//   ResetForLoop.
//
ScalarEvolutionContext::ScalarEvolutionContext(Compiler* comp)
    : m_comp(comp), m_cache(comp->getAllocator(CMK_LoopIVOpts)), m_ephemeralCache(comp->getAllocator(CMK_LoopIVOpts))
{
}

//------------------------------------------------------------------------
// ResetForLoop: Reset the internal cache in preparation of scalar
// evolution analysis inside a new loop.
//
// Parameters:
//   loop - The loop.
//
void ScalarEvolutionContext::ResetForLoop(FlowGraphNaturalLoop* loop)
{
    m_loop = loop;
    m_cache.RemoveAll();
}

//------------------------------------------------------------------------
// NewConstant: Create a SCEV node that represents a constant.
//
// Returns:
//   The new node.
//
ScevConstant* ScalarEvolutionContext::NewConstant(var_types type, int64_t value)
{
    ScevConstant* constant = new (m_comp, CMK_LoopIVOpts) ScevConstant(type, value);
    return constant;
}

//------------------------------------------------------------------------
// NewLocal: Create a SCEV node that represents an invariant local (i.e. a
// use of an SSA def from outside the loop).
//
// Parameters:
//   lclNum - The local
//   ssaNum - The SSA number of the def outside the loop that is being used.
//
// Returns:
//   The new node.
//
ScevLocal* ScalarEvolutionContext::NewLocal(unsigned lclNum, unsigned ssaNum)
{
    var_types  type           = genActualType(m_comp->lvaGetDesc(lclNum));
    ScevLocal* invariantLocal = new (m_comp, CMK_LoopIVOpts) ScevLocal(type, lclNum, ssaNum);
    return invariantLocal;
}

//------------------------------------------------------------------------
// NewExtension: Create a SCEV node that represents a zero or sign extension.
//
// Parameters:
//   oper       - The operation (ScevOper::ZeroExtend or ScevOper::SignExtend)
//   targetType - The target type of the extension
//   op         - The operand being extended.
//
// Returns:
//   The new node.
//
ScevUnop* ScalarEvolutionContext::NewExtension(ScevOper oper, var_types targetType, Scev* op)
{
    assert(op != nullptr);
    ScevUnop* ext = new (m_comp, CMK_LoopIVOpts) ScevUnop(oper, targetType, op);
    return ext;
}

//------------------------------------------------------------------------
// NewBinop: Create a SCEV node that represents a binary operation.
//
// Parameters:
//   oper - The operation
//   op1  - First operand
//   op2  - Second operand
//
// Returns:
//   The new node.
//
ScevBinop* ScalarEvolutionContext::NewBinop(ScevOper oper, Scev* op1, Scev* op2)
{
    assert((op1 != nullptr) && (op2 != nullptr));
    ScevBinop* binop = new (m_comp, CMK_LoopIVOpts) ScevBinop(oper, op1->Type, op1, op2);
    return binop;
}

//------------------------------------------------------------------------
// NewAddRec: Create a SCEV node that represents a new add recurrence.
//
// Parameters:
//   loop  - The loop where this add recurrence is evolving
//   start - Value of the recurrence at the first iteration
//   step  - Step value of the recurrence
//
// Returns:
//   The new node.
//
ScevAddRec* ScalarEvolutionContext::NewAddRec(Scev* start, Scev* step)
{
    assert((start != nullptr) && (step != nullptr));
    ScevAddRec* addRec = new (m_comp, CMK_LoopIVOpts) ScevAddRec(start->Type, start, step DEBUGARG(m_loop));
    return addRec;
}

//------------------------------------------------------------------------
// CreateSimpleInvariantScev: Create a "simple invariant" SCEV node for a tree:
// either an invariant local use or a constant.
//
// Parameters:
//   tree - The tree
//
// Returns:
//   SCEV node or nullptr if the tree is not a simple invariant.
//
Scev* ScalarEvolutionContext::CreateSimpleInvariantScev(GenTree* tree)
{
    if (tree->OperIs(GT_CNS_INT, GT_CNS_LNG))
    {
        return CreateScevForConstant(tree->AsIntConCommon());
    }

    if (tree->OperIs(GT_LCL_VAR) && tree->AsLclVarCommon()->HasSsaName())
    {
        LclVarDsc*    dsc    = m_comp->lvaGetDesc(tree->AsLclVarCommon());
        LclSsaVarDsc* ssaDsc = dsc->GetPerSsaData(tree->AsLclVarCommon()->GetSsaNum());

        if ((ssaDsc->GetBlock() == nullptr) || !m_loop->ContainsBlock(ssaDsc->GetBlock()))
        {
            return NewLocal(tree->AsLclVarCommon()->GetLclNum(), tree->AsLclVarCommon()->GetSsaNum());
        }
    }

    return nullptr;
}

//------------------------------------------------------------------------
// CreateScevForConstant: Given an integer constant, create a SCEV node for it.
//
// Parameters:
//   tree - The integer constant
//
// Returns:
//   SCEV node or nullptr if the integer constant is not representable (e.g. a handle).
//
Scev* ScalarEvolutionContext::CreateScevForConstant(GenTreeIntConCommon* tree)
{
    if (tree->IsIconHandle() || !tree->TypeIs(TYP_INT, TYP_LONG))
    {
        return nullptr;
    }

    return NewConstant(tree->TypeGet(), tree->AsIntConCommon()->IntegralValue());
}

//------------------------------------------------------------------------
// AnalyzeNew: Analyze the specified tree in the specified block, without going
// through the cache.
//
// Parameters:
//   block - Block containing the tree
//   tree  - Tree node
//   depth - Current analysis depth
//
// Returns:
//   SCEV node if the tree was analyzable; otherwise nullptr if the value is
//   cannot be described.
//
Scev* ScalarEvolutionContext::AnalyzeNew(BasicBlock* block, GenTree* tree, int depth)
{
    switch (tree->OperGet())
    {
        case GT_CNS_INT:
        case GT_CNS_LNG:
        {
            return CreateScevForConstant(tree->AsIntConCommon());
        }
        case GT_LCL_VAR:
        case GT_PHI_ARG:
        {
            if (!tree->AsLclVarCommon()->HasSsaName())
            {
                return nullptr;
            }

            assert(m_comp->lvaInSsa(tree->AsLclVarCommon()->GetLclNum()));
            LclVarDsc*    dsc    = m_comp->lvaGetDesc(tree->AsLclVarCommon());
            LclSsaVarDsc* ssaDsc = dsc->GetPerSsaData(tree->AsLclVarCommon()->GetSsaNum());

            if ((ssaDsc->GetBlock() == nullptr) || !m_loop->ContainsBlock(ssaDsc->GetBlock()))
            {
                return NewLocal(tree->AsLclVarCommon()->GetLclNum(), tree->AsLclVarCommon()->GetSsaNum());
            }

            if (ssaDsc->GetDefNode() == nullptr)
            {
                // GT_CALL retbuf def?
                return nullptr;
            }

            if (ssaDsc->GetDefNode()->GetLclNum() != tree->AsLclVarCommon()->GetLclNum())
            {
                // Should be a def of the parent
                assert(dsc->lvIsStructField && (ssaDsc->GetDefNode()->GetLclNum() == dsc->lvParentLcl));
                return nullptr;
            }

            return Analyze(ssaDsc->GetBlock(), ssaDsc->GetDefNode(), depth + 1);
        }
        case GT_STORE_LCL_VAR:
        {
            GenTreeLclVarCommon* store = tree->AsLclVarCommon();
            GenTree*             data  = store->Data();
            if (!data->OperIs(GT_PHI))
            {
                return Analyze(block, data, depth + 1);
            }

            if (block != m_loop->GetHeader())
            {
                return nullptr;
            }

            // We have a phi def for the current loop. Look for a primary
            // induction variable.
            GenTreePhi*    phi         = data->AsPhi();
            GenTreePhiArg* enterSsa    = nullptr;
            GenTreePhiArg* backedgeSsa = nullptr;

            for (GenTreePhi::Use& use : phi->Uses())
            {
                GenTreePhiArg*  phiArg = use.GetNode()->AsPhiArg();
                GenTreePhiArg*& ssaArg = m_loop->ContainsBlock(phiArg->gtPredBB) ? backedgeSsa : enterSsa;
                if ((ssaArg == nullptr) || (ssaArg->GetSsaNum() == phiArg->GetSsaNum()))
                {
                    ssaArg = phiArg;
                }
                else
                {
                    return nullptr;
                }
            }

            if ((enterSsa == nullptr) || (backedgeSsa == nullptr))
            {
                return nullptr;
            }

            ScevLocal* enterScev = NewLocal(enterSsa->GetLclNum(), enterSsa->GetSsaNum());

            LclVarDsc*    dsc    = m_comp->lvaGetDesc(store);
            LclSsaVarDsc* ssaDsc = dsc->GetPerSsaData(backedgeSsa->GetSsaNum());

            if (ssaDsc->GetDefNode() == nullptr)
            {
                // GT_CALL retbuf def
                return nullptr;
            }

            if (ssaDsc->GetDefNode()->GetLclNum() != store->GetLclNum())
            {
                assert(dsc->lvIsStructField && ssaDsc->GetDefNode()->GetLclNum() == dsc->lvParentLcl);
                return nullptr;
            }

            assert(ssaDsc->GetBlock() != nullptr);

            Scev* simpleAddRec = CreateSimpleAddRec(store, enterScev, ssaDsc->GetBlock(), ssaDsc->GetDefNode()->Data());
            if (simpleAddRec != nullptr)
            {
                return simpleAddRec;
            }

            ScevConstant* symbolicAddRec = NewConstant(data->TypeGet(), 0xdeadbeef);
            m_ephemeralCache.Emplace(store, symbolicAddRec);

            Scev* result;
            if (m_usingEphemeralCache)
            {
                result = Analyze(ssaDsc->GetBlock(), ssaDsc->GetDefNode()->Data(), depth + 1);
            }
            else
            {
                m_usingEphemeralCache = true;
                result                = Analyze(ssaDsc->GetBlock(), ssaDsc->GetDefNode()->Data(), depth + 1);
                m_usingEphemeralCache = false;
                m_ephemeralCache.RemoveAll();
            }

            if (result == nullptr)
            {
                return nullptr;
            }

            return MakeAddRecFromRecursiveScev(enterScev, result, symbolicAddRec);
        }
        case GT_CAST:
        {
            GenTreeCast* cast = tree->AsCast();
            if (cast->gtCastType != TYP_LONG)
            {
                return nullptr;
            }

            Scev* op = Analyze(block, cast->CastOp(), depth + 1);
            if (op == nullptr)
            {
                return nullptr;
            }

            return NewExtension(cast->IsUnsigned() ? ScevOper::ZeroExtend : ScevOper::SignExtend, TYP_LONG, op);
        }
        case GT_ADD:
        case GT_MUL:
        case GT_LSH:
        {
            Scev* op1 = Analyze(block, tree->gtGetOp1(), depth + 1);
            if (op1 == nullptr)
                return nullptr;

            Scev* op2 = Analyze(block, tree->gtGetOp2(), depth + 1);
            if (op2 == nullptr)
                return nullptr;

            ScevOper oper;
            switch (tree->OperGet())
            {
                case GT_ADD:
                    oper = ScevOper::Add;
                    break;
                case GT_MUL:
                    oper = ScevOper::Mul;
                    break;
                case GT_LSH:
                    oper = ScevOper::Lsh;
                    break;
                default:
                    unreached();
            }

            return NewBinop(oper, op1, op2);
        }
        case GT_COMMA:
        {
            return Analyze(block, tree->gtGetOp2(), depth + 1);
        }
        case GT_ARR_ADDR:
        {
            return Analyze(block, tree->AsArrAddr()->Addr(), depth + 1);
        }
        default:
            return nullptr;
    }
}

//------------------------------------------------------------------------
// CreateSimpleAddRec: Create a "simple" add-recurrence. This handles the most
// common patterns for primary induction variables where we see a store like
// "i = i + 1".
//
// Parameters:
//   headerStore  - Phi definition of the candidate primary induction variable
//   enterScev    - SCEV describing start value of the primary induction variable
//   stepDefBlock - Block containing the def of the step value
//   stepDefData  - Value of the def of the step value
//
// Returns:
//   SCEV node if this is a simple addrec shape. Otherwise nullptr.
//
Scev* ScalarEvolutionContext::CreateSimpleAddRec(GenTreeLclVarCommon* headerStore,
                                                 ScevLocal*           enterScev,
                                                 BasicBlock*          stepDefBlock,
                                                 GenTree*             stepDefData)
{
    if (!stepDefData->OperIs(GT_ADD))
    {
        return nullptr;
    }

    GenTree* stepTree;
    GenTree* op1 = stepDefData->gtGetOp1();
    GenTree* op2 = stepDefData->gtGetOp2();
    if (op1->OperIs(GT_LCL_VAR) && (op1->AsLclVar()->GetLclNum() == headerStore->GetLclNum()) &&
        (op1->AsLclVar()->GetSsaNum() == headerStore->GetSsaNum()))
    {
        stepTree = op2;
    }
    else if (op2->OperIs(GT_LCL_VAR) && (op2->AsLclVar()->GetLclNum() == headerStore->GetLclNum()) &&
             (op2->AsLclVar()->GetSsaNum() == headerStore->GetSsaNum()))
    {
        stepTree = op1;
    }
    else
    {
        // Not a simple IV shape (i.e. more complex than "i = i + k")
        return nullptr;
    }

    Scev* stepScev = CreateSimpleInvariantScev(stepTree);
    if (stepScev == nullptr)
    {
        return nullptr;
    }

    return NewAddRec(enterScev, stepScev);
}

//------------------------------------------------------------------------
// ExtractAddOperands: Extract all operands of potentially nested add
// operations.
//
// Parameters:
//   binop    - The binop representing an add
//   operands - Array stack to add the operands to
//
void ScalarEvolutionContext::ExtractAddOperands(ScevBinop* binop, ArrayStack<Scev*>& operands)
{
    assert(binop->OperIs(ScevOper::Add));

    if (binop->Op1->OperIs(ScevOper::Add))
    {
        ExtractAddOperands(static_cast<ScevBinop*>(binop->Op1), operands);
    }
    else
    {
        operands.Push(binop->Op1);
    }

    if (binop->Op2->OperIs(ScevOper::Add))
    {
        ExtractAddOperands(static_cast<ScevBinop*>(binop->Op2), operands);
    }
    else
    {
        operands.Push(binop->Op2);
    }
}

//------------------------------------------------------------------------
// MakeAddRecFromRecursiveScev: Given a recursive SCEV and a symbolic SCEV
// whose appearances represent an occurrence of the full SCEV, create a
// non-recursive add-rec from it.
//
// Parameters:
//   startScev     - The start value of the addrec
//   scev          - The scev
//   recursiveScev - A symbolic node whose appearance represents the value of "scev"
//
// Returns:
//   A non-recursive addrec
//
Scev* ScalarEvolutionContext::MakeAddRecFromRecursiveScev(Scev* startScev, Scev* scev, Scev* recursiveScev)
{
    if (!scev->OperIs(ScevOper::Add))
    {
        return nullptr;
    }

    ArrayStack<Scev*> addOperands(m_comp->getAllocator(CMK_LoopIVOpts));
    ExtractAddOperands(static_cast<ScevBinop*>(scev), addOperands);

    assert(addOperands.Height() >= 2);

    int numAppearances = 0;
    for (int i = 0; i < addOperands.Height(); i++)
    {
        Scev* addOperand = addOperands.Bottom(i);
        if (addOperand == recursiveScev)
        {
            numAppearances++;
        }
        else
        {
            ScevVisit result = addOperand->Visit([=](Scev* node) {
                if (node == recursiveScev)
                {
                    return ScevVisit::Abort;
                }

                return ScevVisit::Continue;
            });

            if (result == ScevVisit::Abort)
            {
                // We do not handle nested occurrences. Some of these may be representable, some won't.
                return nullptr;
            }
        }
    }

    if (numAppearances == 0)
    {
        // TODO-CQ: We currently cannot handle cases like
        // i = arr.Length;
        // j = i - 1;
        // i = j;
        // while (true) { ...; j = i - 1; i = j; }
        //
        // These cases can arise from loop structures like "for (int i =
        // arr.Length; --i >= 0;)" when Roslyn emits a "sub; dup; stloc"
        // sequence, and local prop + loop inversion converts the duplicated
        // local into a fully fledged IV.
        // In this case we see that i = <L, [i from outside loop], -1>, but for
        // j we will see <L, [i from outside loop], -1> + (-1) in this function
        // as the value coming around the backedge, and we cannot reconcile
        // this.
        //
        return nullptr;
    }

    if (numAppearances > 1)
    {
        // Multiple occurrences -- cannot be represented as an addrec
        // (corresponds to a geometric progression).
        return nullptr;
    }

    Scev* step = nullptr;
    for (int i = 0; i < addOperands.Height(); i++)
    {
        Scev* addOperand = addOperands.Bottom(i);
        if (addOperand == recursiveScev)
        {
            continue;
        }

        if (step == nullptr)
        {
            step = addOperand;
        }
        else
        {
            step = NewBinop(ScevOper::Add, step, addOperand);
        }
    }

    return NewAddRec(startScev, step);
}

//------------------------------------------------------------------------
// Analyze: Analyze the specified tree in the specified block.
//
// Parameters:
//   block - Block containing the tree
//   tree  - Tree node
//
// Returns:
//   SCEV node if the tree was analyzable; otherwise nullptr if the value is
//   cannot be described.
//
Scev* ScalarEvolutionContext::Analyze(BasicBlock* block, GenTree* tree)
{
    return Analyze(block, tree, 0);
}

// Since the analysis follows SSA defs we have no upper bound on the potential
// depth of the analysis performed. We put an artificial limit on this for two
// reasons:
// 1. The analysis is recursive, and we should not stack overflow regardless of
// the input program.
// 2. If we produced arbitrarily deep SCEV trees then all algorithms over their
// structure would similarly be at risk of stack overflows if they were
// recursive. However, these algorithms are generally much more elegant when
// they make use of recursion.
const int SCALAR_EVOLUTION_ANALYSIS_MAX_DEPTH = 64;

//------------------------------------------------------------------------
// Analyze: Analyze the specified tree in the specified block.
//
// Parameters:
//   block - Block containing the tree
//   tree  - Tree node
//   depth - Current analysis depth
//
// Returns:
//   SCEV node if the tree was analyzable; otherwise nullptr if the value is
//   cannot be described.
//
Scev* ScalarEvolutionContext::Analyze(BasicBlock* block, GenTree* tree, int depth)
{
    Scev* result;
    if (!m_cache.Lookup(tree, &result) && (!m_usingEphemeralCache || !m_ephemeralCache.Lookup(tree, &result)))
    {
        if (depth >= SCALAR_EVOLUTION_ANALYSIS_MAX_DEPTH)
        {
            return nullptr;
        }

        result = AnalyzeNew(block, tree, depth);

        if (m_usingEphemeralCache)
        {
            m_ephemeralCache.Set(tree, result, ScalarEvolutionMap::Overwrite);
        }
        else
        {
            m_cache.Set(tree, result);
        }
    }

    return result;
}

//------------------------------------------------------------------------
// FoldBinop: Fold simple binops.
//
// Type parameters:
//   T - Type that the binop is being evaluated in
//
// Parameters:
//   oper - Binary operation
//   op1  - First operand
//   op2  - Second operand
//
// Returns:
//   Folded value.
//
template <typename T>
static T FoldBinop(ScevOper oper, T op1, T op2)
{
    switch (oper)
    {
        case ScevOper::Add:
            return op1 + op2;
        case ScevOper::Mul:
            return op1 * op2;
        case ScevOper::Lsh:
            return op1 << op2;
        default:
            unreached();
    }
}

//------------------------------------------------------------------------
// Simplify: Try to simplify a SCEV node by folding and canonicalization.
//
// Parameters:
//   scev - The node
//
// Returns:
//   Simplified node.
//
// Remarks:
//   Canonicalization is done for binops; constants are moved to the right and
//   addrecs are moved to the left.
//
//   Simple unops/binops on constants are folded. Operands are distributed into
//   add recs whenever possible.
//
Scev* ScalarEvolutionContext::Simplify(Scev* scev)
{
    switch (scev->Oper)
    {
        case ScevOper::Constant:
        case ScevOper::Local:
        {
            return scev;
        }
        case ScevOper::ZeroExtend:
        case ScevOper::SignExtend:
        {
            ScevUnop* unop = (ScevUnop*)scev;
            assert(genTypeSize(unop->Type) >= genTypeSize(unop->Op1->Type));

            Scev* op1 = Simplify(unop->Op1);

            if (unop->Type == op1->Type)
            {
                return op1;
            }

            assert((unop->Type == TYP_LONG) && (op1->Type == TYP_INT));

            if (op1->OperIs(ScevOper::Constant))
            {
                ScevConstant* cns = (ScevConstant*)op1;
                return NewConstant(unop->Type, unop->OperIs(ScevOper::ZeroExtend) ? (uint64_t)(int32_t)cns->Value
                                                                                  : (int64_t)(int32_t)cns->Value);
            }

            if (op1->OperIs(ScevOper::AddRec))
            {
                // TODO-Cleanup: This requires some proof that it is ok, but
                // currently we do not rely on this.
                return op1;
            }

            return (op1 == unop->Op1) ? unop : NewExtension(unop->Oper, unop->Type, op1);
        }
        case ScevOper::Add:
        case ScevOper::Mul:
        case ScevOper::Lsh:
        {
            ScevBinop* binop = (ScevBinop*)scev;
            Scev*      op1   = Simplify(binop->Op1);
            Scev*      op2   = Simplify(binop->Op2);

            if (binop->OperIs(ScevOper::Add, ScevOper::Mul))
            {
                // Normalize addrecs to the left
                if (op2->OperIs(ScevOper::AddRec) && !op1->OperIs(ScevOper::AddRec))
                {
                    std::swap(op1, op2);
                }
                // Normalize constants to the right
                if (op1->OperIs(ScevOper::Constant) && !op2->OperIs(ScevOper::Constant))
                {
                    std::swap(op1, op2);
                }
            }

            if (op1->OperIs(ScevOper::AddRec))
            {
                // <L, start, step> + x => <L, start + x, step>
                // <L, start, step> * x => <L, start * x, step * x>
                ScevAddRec* addRec   = (ScevAddRec*)op1;
                Scev*       newStart = Simplify(NewBinop(binop->Oper, addRec->Start, op2));
                Scev*       newStep  = scev->OperIs(ScevOper::Mul, ScevOper::Lsh)
                                    ? Simplify(NewBinop(binop->Oper, addRec->Step, op2))
                                    : addRec->Step;
                return NewAddRec(newStart, newStep);
            }

            if (op1->OperIs(ScevOper::Constant) && op2->OperIs(ScevOper::Constant))
            {
                ScevConstant* cns1 = (ScevConstant*)op1;
                ScevConstant* cns2 = (ScevConstant*)op2;
                int64_t       newValue;
                if (binop->TypeIs(TYP_INT))
                {
                    newValue = FoldBinop<int32_t>(binop->Oper, static_cast<int32_t>(cns1->Value),
                                                  static_cast<int32_t>(cns2->Value));
                }
                else
                {
                    assert(binop->TypeIs(TYP_LONG));
                    newValue = FoldBinop<int64_t>(binop->Oper, cns1->Value, cns2->Value);
                }

                return NewConstant(binop->Type, newValue);
            }

            return (op1 == binop->Op1) && (op2 == binop->Op2) ? binop : NewBinop(binop->Oper, op1, op2);
        }
        case ScevOper::AddRec:
        {
            ScevAddRec* addRec = (ScevAddRec*)scev;
            Scev*       start  = Simplify(addRec->Start);
            Scev*       step   = Simplify(addRec->Step);
            return (start == addRec->Start) && (step == addRec->Step) ? addRec : NewAddRec(start, step);
        }
        default:
            unreached();
    }
}
