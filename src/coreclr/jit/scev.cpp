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
            if (genTypeSize(cns->Type) == 4)
            {
                printf("%d", (int32_t)cns->Value);
            }
            else
            {
                printf("%lld", (int64_t)cns->Value);
            }
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
// Scev::IsInvariant: Check if the SCEV node is invariant inside the loop.
//
// Returns:
//   True if so.
//
// Remarks:
//   A SCEV is variant if it contains any add recurrence.
//
bool Scev::IsInvariant()
{
    ScevVisit result = Visit([](Scev* scev) {
        return scev->OperIs(ScevOper::AddRec) ? ScevVisit::Abort : ScevVisit::Continue;
    });

    return result != ScevVisit::Abort;
}

//------------------------------------------------------------------------
// Scev::PeelAdditions: Peel the aditions from a SCEV and return the base SCEV
// and the sum of the offsets peeled.
//
// Parameters:
//   offset - [out] The sum of offsets peeled
//
// Returns:
//   The base SCEV.
//
// Remarks:
//   If the SCEV is 32-bits, the user is expected to apply the proper
//   truncation (or extension into 64-bit).
//
Scev* Scev::PeelAdditions(int64_t* offset)
{
    *offset = 0;

    Scev* scev = this;
    while (scev->OperIs(ScevOper::Add))
    {
        Scev* op1 = ((ScevBinop*)scev)->Op1;
        Scev* op2 = ((ScevBinop*)scev)->Op2;
        if (op1->OperIs(ScevOper::Constant))
        {
            *offset += ((ScevConstant*)op1)->Value;
            scev = op2;
        }
        else if (op2->OperIs(ScevOper::Constant))
        {
            *offset += ((ScevConstant*)op2)->Value;
            scev = op1;
        }
        else
        {
            break;
        }
    }

    return scev;
}

//------------------------------------------------------------------------
// Scev::Equals: Check if two SCEV trees are equal.
//
// Parameters:
//   left  - First scev
//   right - Second scev
//
// Returns:
//   True if they represent the same value; otherwise false.
//
bool Scev::Equals(Scev* left, Scev* right)
{
    if (left == right)
    {
        return true;
    }

    if ((left->Oper != right->Oper) || (left->Type != right->Type))
    {
        return false;
    }

    switch (left->Oper)
    {
        case ScevOper::Constant:
            return static_cast<ScevConstant*>(left)->Value == static_cast<ScevConstant*>(right)->Value;
        case ScevOper::Local:
        {
            ScevLocal* leftLocal  = static_cast<ScevLocal*>(left);
            ScevLocal* rightLocal = static_cast<ScevLocal*>(right);
            return (leftLocal->LclNum == rightLocal->LclNum) && (leftLocal->SsaNum == rightLocal->SsaNum);
        }
        case ScevOper::ZeroExtend:
        case ScevOper::SignExtend:
            return Scev::Equals(static_cast<ScevUnop*>(left)->Op1, static_cast<ScevUnop*>(right)->Op1);
        case ScevOper::Add:
        case ScevOper::Mul:
        case ScevOper::Lsh:
        {
            ScevBinop* leftBinop  = static_cast<ScevBinop*>(left);
            ScevBinop* rightBinop = static_cast<ScevBinop*>(right);
            return Scev::Equals(leftBinop->Op1, rightBinop->Op1) && Scev::Equals(leftBinop->Op2, rightBinop->Op2);
        }
        case ScevOper::AddRec:
        {
            ScevAddRec* leftAddRec  = static_cast<ScevAddRec*>(left);
            ScevAddRec* rightAddRec = static_cast<ScevAddRec*>(right);
            return Scev::Equals(leftAddRec->Start, rightAddRec->Start) &&
                   Scev::Equals(leftAddRec->Step, rightAddRec->Step);
        }
        default:
            unreached();
    }
}

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
    : m_comp(comp)
    , m_cache(comp->getAllocator(CMK_LoopIVOpts))
    , m_ephemeralCache(comp->getAllocator(CMK_LoopIVOpts))
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
    var_types resultType = op1->Type;
    if (oper == ScevOper::Add)
    {
        if (varTypeIsGC(op1->Type))
        {
            assert(op2->Type == TYP_I_IMPL);
            resultType = TYP_BYREF;
        }
        else if (varTypeIsGC(op2->Type))
        {
            assert(op1->Type == TYP_I_IMPL);
            resultType = TYP_BYREF;
        }
        else
        {
            assert(op1->Type == op2->Type);
        }
    }

    ScevBinop* binop = new (m_comp, CMK_LoopIVOpts) ScevBinop(oper, resultType, op1, op2);
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

            if ((tree->TypeGet() != dsc->TypeGet()) || varTypeIsSmall(tree))
            {
                // TODO: Truncations (for TYP_INT uses of TYP_LONG locals) and NOL handling?
                return nullptr;
            }

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

            // Try simple but most common case first, where we have a direct
            // add recurrence like i = i + 1.
            Scev* simpleAddRec = CreateSimpleAddRec(store, enterScev, ssaDsc->GetBlock(), ssaDsc->GetDefNode()->Data());
            if (simpleAddRec != nullptr)
            {
                return simpleAddRec;
            }

            // Otherwise try a more powerful approach; we create a symbolic
            // node representing the recurrence and then invoke the analysis
            // recursively. This handles for example cases like
            //
            //   int i = start;
            //   while (i < n)
            //   {
            //     int j = i + 1;
            //     ...
            //     i = j;
            //   }
            // => <L, start, 1>
            //
            // where we need to follow SSA defs. In this case the analysis will result in
            // <symbolic node> + 1. The symbolic node represents a recurrence,
            // so this corresponds to the infinite sequence [start, start + 1,
            // start + 1 + 1, ...] which can be represented by <L, start, 1>.
            //
            // This approach also generalizes to handle chains of recurrences.
            // For example:
            //
            //   int i = 0;
            //   int j = 0;
            //   while (i < n)
            //   {
            //     j++;
            //     i += j;
            //   }
            // => <L, 0, <L, 1, 1>>
            //
            // Here `i` will analyze to <symbolic node> + <L, [initial value of j], 1>.
            // Like before this corresponds to an infinite sequence
            // [start, start + <L, [initial value of j], 1>, start + 2 * <L, [initial value of j], 1>, ...]
            // which again can be represented as <L, start, <L, [initial value of j], 1>>.
            //
            // More generally, as long as we have only additions and only a
            // single operand is the recurrence, we can represent it as an add
            // recurrence. See MakeAddRecFromRecursiveScev for the details.
            //
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
        case GT_SUB:
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
                case GT_SUB:
                    if (varTypeIsGC(op2->Type))
                    {
                        // We represent x - y as x + (-1)*y, which does not
                        // work if y is a GC type. If we wanted to support this
                        // we would need to add an explicit ScevOper::Sub
                        // operator.
                        return nullptr;
                    }

                    oper = ScevOper::Add;
                    op2  = NewBinop(ScevOper::Mul, op2, NewConstant(op2->Type, -1));
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
//   A non-recursive addrec, or nullptr if the recursive SCEV is not
//   representable as an add recurrence.
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

const SimplificationAssumptions ScalarEvolutionContext::NoAssumptions;

//------------------------------------------------------------------------
// Simplify: Try to simplify a SCEV node by folding and canonicalization.
//
// Parameters:
//   scev        - The node
//   assumptions - Assumptions that the simplification procedure can use.
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
Scev* ScalarEvolutionContext::Simplify(Scev* scev, const SimplificationAssumptions& assumptions)
{
    switch (scev->Oper)
    {
        case ScevOper::Constant:
        {
            return scev;
        }
        case ScevOper::Local:
        {
            ScevLocal* local = (ScevLocal*)scev;
            int64_t    cns;
            if (local->GetConstantValue(m_comp, &cns))
            {
                return NewConstant(local->Type, cns);
            }

            return local;
        }
        case ScevOper::ZeroExtend:
        case ScevOper::SignExtend:
        {
            ScevUnop* unop = (ScevUnop*)scev;
            assert(genTypeSize(unop->Type) >= genTypeSize(unop->Op1->Type));

            Scev* op1 = Simplify(unop->Op1, assumptions);

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
                ScevAddRec* addRec = (ScevAddRec*)op1;

                // We need to guarantee that
                // ext(<L, start, step>) = <L, ext(start), ext(step)> to distribute the extension.
                //
                // Equivalently this is the case iff
                // forall i < backedgeTakenCount, ext(start + step * i) == ext(start) + ext(step) * i.
                //
                // For zext: we must guarantee that 0 <= start + step * i < 2^32.
                // For sext: we must guarantee that -2^31 <= start + step * i < 2^31.
                //
                if (!AddRecMayOverflow(addRec, unop->OperIs(ScevOper::SignExtend), assumptions))
                {
                    Scev* newStart = Simplify(NewExtension(unop->Oper, TYP_LONG, addRec->Start), assumptions);
                    Scev* newStep  = Simplify(NewExtension(unop->Oper, TYP_LONG, addRec->Step), assumptions);
                    return NewAddRec(newStart, newStep);
                }
            }

            return (op1 == unop->Op1) ? unop : NewExtension(unop->Oper, unop->Type, op1);
        }
        case ScevOper::Add:
        case ScevOper::Mul:
        case ScevOper::Lsh:
        {
            ScevBinop* binop = (ScevBinop*)scev;
            Scev*      op1   = Simplify(binop->Op1, assumptions);
            Scev*      op2   = Simplify(binop->Op2, assumptions);

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
                Scev*       newStart = Simplify(NewBinop(binop->Oper, addRec->Start, op2), assumptions);
                Scev*       newStep  = scev->OperIs(ScevOper::Mul, ScevOper::Lsh)
                                           ? Simplify(NewBinop(binop->Oper, addRec->Step, op2), assumptions)
                                           : addRec->Step;
                return NewAddRec(newStart, newStep);
            }

            if (op1->OperIs(ScevOper::Constant) && op2->OperIs(ScevOper::Constant))
            {
                ScevConstant* cns1 = (ScevConstant*)op1;
                ScevConstant* cns2 = (ScevConstant*)op2;
                int64_t       newValue;
                if (genTypeSize(binop->Type) == 4)
                {
                    newValue = FoldBinop<uint32_t>(binop->Oper, static_cast<uint32_t>(cns1->Value),
                                                   static_cast<uint32_t>(cns2->Value));
                }
                else
                {
                    assert(genTypeSize(binop->Type) == 8);
                    newValue = FoldBinop<uint64_t>(binop->Oper, static_cast<uint64_t>(cns1->Value),
                                                   static_cast<uint64_t>(cns2->Value));
                }

                return NewConstant(binop->Type, newValue);
            }
            else if (op2->OperIs(ScevOper::Constant))
            {
                ScevConstant* cns2 = (ScevConstant*)op2;
                // a +/<< 0 => a
                if (binop->OperIs(ScevOper::Add, ScevOper::Lsh) && (cns2->Value == 0))
                {
                    return op1;
                }

                if (binop->OperIs(ScevOper::Add))
                {
                    // (a + c1) + c2 => a + (c1 + c2)
                    if (op1->OperIs(ScevOper::Add) && (((ScevBinop*)op1)->Op2->OperIs(ScevOper::Constant)))
                    {
                        ScevBinop* newOp2 = NewBinop(ScevOper::Add, ((ScevBinop*)op1)->Op2, cns2);
                        ScevBinop* newAdd = NewBinop(ScevOper::Add, ((ScevBinop*)op1)->Op1, newOp2);
                        return Simplify(newAdd, assumptions);
                    }
                }

                if (binop->OperIs(ScevOper::Mul))
                {
                    // a * 0 => 0
                    if (cns2->Value == 0)
                    {
                        return cns2;
                    }

                    // a * 1 => a
                    if (cns2->Value == 1)
                    {
                        return op1;
                    }

                    // (a * c1) * c2 => a * (c1 * c2)
                    if (op1->OperIs(ScevOper::Mul) && (((ScevBinop*)op1)->Op2->OperIs(ScevOper::Constant)))
                    {
                        ScevBinop* newOp2 = NewBinop(ScevOper::Mul, ((ScevBinop*)op1)->Op2, cns2);
                        ScevBinop* newMul = NewBinop(ScevOper::Mul, ((ScevBinop*)op1)->Op1, newOp2);
                        return Simplify(newMul, assumptions);
                    }
                }
            }
            else if (op1->OperIs(ScevOper::Constant))
            {
                ScevConstant* cns1 = (ScevConstant*)op1;
                if (binop->OperIs(ScevOper::Lsh) && (cns1->Value == 0))
                {
                    return cns1;
                }
            }

            if (binop->OperIs(ScevOper::Add))
            {
                // (a + c1) + (b + c2) => (a + b) + (c1 + c2)
                if (op1->OperIs(ScevOper::Add) && ((ScevBinop*)op1)->Op2->OperIs(ScevOper::Constant) &&
                    op2->OperIs(ScevOper::Add) && ((ScevBinop*)op2)->Op2->OperIs(ScevOper::Constant))
                {
                    ScevBinop* newOp1 = NewBinop(ScevOper::Add, ((ScevBinop*)op1)->Op1, ((ScevBinop*)op2)->Op1);
                    ScevBinop* newOp2 = NewBinop(ScevOper::Add, ((ScevBinop*)op1)->Op2, ((ScevBinop*)op2)->Op2);
                    ScevBinop* newAdd = NewBinop(ScevOper::Add, newOp1, newOp2);
                    return Simplify(newAdd, assumptions);
                }
            }

            return (op1 == binop->Op1) && (op2 == binop->Op2) ? binop : NewBinop(binop->Oper, op1, op2);
        }
        case ScevOper::AddRec:
        {
            ScevAddRec* addRec = (ScevAddRec*)scev;
            Scev*       start  = Simplify(addRec->Start, assumptions);
            Scev*       step   = Simplify(addRec->Step, assumptions);
            return (start == addRec->Start) && (step == addRec->Step) ? addRec : NewAddRec(start, step);
        }
        default:
            unreached();
    }
}

//------------------------------------------------------------------------
// Materialize: Materialize a SCEV into IR and/or a value number.
//
// Parameters:
//   scev      - The SCEV
//   createIR  - Whether to create IR. If so "result" will be assigned.
//   result    - [out] The IR node result.
//   resultVNP - [out] The VNP result. Cannot be nullptr.
//
// Returns:
//   True on success. Add recurrences cannot be materialized.
//
bool ScalarEvolutionContext::Materialize(Scev* scev, bool createIR, GenTree** result, ValueNumPair* resultVNP)
{
    switch (scev->Oper)
    {
        case ScevOper::Constant:
        {
            ScevConstant* cns = (ScevConstant*)scev;
            if (cns->TypeIs(TYP_REF))
            {
                if (cns->Value != 0)
                {
                    // TODO-CQ: Proper handling for handles
                    return false;
                }

                resultVNP->SetBoth(m_comp->vnStore->VNForNull());
            }
            else if (cns->TypeIs(TYP_BYREF))
            {
                if (cns->Value != 0)
                {
                    // TODO-CQ: Proper handling for handles
                    return false;
                }

                resultVNP->SetBoth(m_comp->vnStore->VNForNull());
            }
            else
            {
                resultVNP->SetBoth(
                    m_comp->vnStore->VNForGenericCon(scev->Type, reinterpret_cast<uint8_t*>(&cns->Value)));
            }

            if (createIR)
            {
                if (scev->TypeIs(TYP_LONG))
                {
                    *result = m_comp->gtNewLconNode(cns->Value);
                }
                else
                {
                    *result = m_comp->gtNewIconNode((ssize_t)cns->Value, scev->Type);
                }
            }

            break;
        }
        case ScevOper::Local:
        {
            ScevLocal*    lcl    = (ScevLocal*)scev;
            LclVarDsc*    dsc    = m_comp->lvaGetDesc(lcl->LclNum);
            LclSsaVarDsc* ssaDsc = dsc->GetPerSsaData(lcl->SsaNum);
            *resultVNP           = m_comp->vnStore->VNPNormalPair(ssaDsc->m_vnPair);

            if (createIR)
            {
                *result = m_comp->gtNewLclvNode(((ScevLocal*)scev)->LclNum, scev->Type);
            }

            break;
        }
        case ScevOper::ZeroExtend:
        case ScevOper::SignExtend:
        {
            ScevUnop*    ext = (ScevUnop*)scev;
            GenTree*     op  = nullptr;
            ValueNumPair opVN;
            if (!Materialize(ext->Op1, createIR, &op, &opVN))
            {
                return false;
            }

            *resultVNP = m_comp->vnStore->VNPairForCast(opVN, TYP_LONG, ext->Type, scev->OperIs(ScevOper::ZeroExtend));

            if (createIR)
            {
                *result = m_comp->gtNewCastNode(ext->Type, op, scev->OperIs(ScevOper::ZeroExtend), TYP_LONG);
            }

            break;
        }
        case ScevOper::Add:
        case ScevOper::Mul:
        case ScevOper::Lsh:
        {
            ScevBinop*   binop = (ScevBinop*)scev;
            GenTree*     op1   = nullptr;
            ValueNumPair op1VN;
            GenTree*     op2 = nullptr;
            ValueNumPair op2VN;
            if (!Materialize(binop->Op1, createIR, &op1, &op1VN) || !Materialize(binop->Op2, createIR, &op2, &op2VN))
            {
                return false;
            }

            genTreeOps oper;
            switch (scev->Oper)
            {
                case ScevOper::Add:
                    oper = GT_ADD;
                    break;
                case ScevOper::Mul:
                    oper = GT_MUL;
                    break;
                case ScevOper::Lsh:
                    oper = GT_LSH;
                    break;
                default:
                    unreached();
            }

            *resultVNP = m_comp->vnStore->VNPairForFunc(binop->Type, VNFunc(oper), op1VN, op2VN);
            if (createIR)
            {
                if (oper == GT_MUL)
                {
                    if (op1->IsIntegralConst(-1))
                    {
                        *result = m_comp->gtNewOperNode(GT_NEG, op2->TypeGet(), op2);
                        break;
                    }
                    if (op2->IsIntegralConst(-1))
                    {
                        *result = m_comp->gtNewOperNode(GT_NEG, op1->TypeGet(), op1);
                        break;
                    }
                }

#ifndef TARGET_64BIT
                if ((oper == GT_MUL) && binop->TypeIs(TYP_LONG))
                {
                    // These require helper calls. Just don't bother.
                    return false;
                }
#endif
                *result = m_comp->gtNewOperNode(oper, binop->Type, op1, op2);
            }

            break;
        }
        case ScevOper::AddRec:
            return false;
        default:
            unreached();
    }

    if (createIR)
    {
        (*result)->SetVNs(*resultVNP);
    }

    return true;
}

//------------------------------------------------------------------------
// Materialize: Materialize a SCEV into IR.
//
// Parameters:
//   scev - The SCEV
//
// Returns:
//   The node, or nullptr if the SCEV cannot be materialized to IR.
//
GenTree* ScalarEvolutionContext::Materialize(Scev* scev)
{
    ValueNumPair vnp;
    GenTree*     result;
    return Materialize(scev, true, &result, &vnp) ? result : nullptr;
}

//------------------------------------------------------------------------
// MaterializeVN: Materialize a SCEV into a VN.
//
// Parameters:
//   scev - The SCEV
//
// Returns:
//   The VNP, or (NoVN, NoVN) if the SCEV is not representable as a VN.
//
ValueNumPair ScalarEvolutionContext::MaterializeVN(Scev* scev)
{
    ValueNumPair vnp;
    return Materialize(scev, false, nullptr, &vnp) ? vnp : ValueNumPair();
}

//------------------------------------------------------------------------
// RelopEvaluationResultString: Convert a RelopEvaluationResult to a string.
//
// Parameters:
//   result - The evaluation result
//
// Returns:
//   String representation
//
static const char* RelopEvaluationResultString(RelopEvaluationResult result)
{
    switch (result)
    {
        case RelopEvaluationResult::Unknown:
            return "unknown";
        case RelopEvaluationResult::True:
            return "true";
        case RelopEvaluationResult::False:
            return "false";
        default:
            return "n/a";
    }
}

//------------------------------------------------------------------------
// EvaluateRelop:
//   Try to evaluate a relop represented by a VN.
//
// Parameters:
//   vn - The VN representing the relop.
//
// Returns:
//   The result of the evaluation, or RelopEvaluationResult::Unknown if the
//   result is not known.
//
// Remarks:
//   Utilizes RBO's reasoning to try to prove the direction of symbolic VNs.
//   This function will build dominators if necessary.
//
RelopEvaluationResult ScalarEvolutionContext::EvaluateRelop(ValueNum vn)
{
    if (m_comp->vnStore->IsVNConstant(vn))
    {
        assert(m_comp->vnStore->TypeOfVN(vn) == TYP_INT);
        return m_comp->vnStore->ConstantValue<int32_t>(vn) != 0 ? RelopEvaluationResult::True
                                                                : RelopEvaluationResult::False;
    }

    // Evaluate by using dominators and RBO's logic.
    assert(m_comp->m_domTree != nullptr);
    //
    // TODO-CQ: Using assertions could be stronger given its dataflow, but it
    // is not convenient to use (optVNConstantPropOnJTrue does not actually
    // make any use of assertions to evaluate conditionals, so it seems like
    // the logic does not actually exist anywhere.)
    //

    for (BasicBlock* idom = m_loop->GetHeader()->bbIDom; idom != nullptr; idom = idom->bbIDom)
    {
        if (!idom->KindIs(BBJ_COND))
        {
            continue;
        }

        Statement* const domJumpStmt = idom->lastStmt();
        GenTree* const   domJumpTree = domJumpStmt->GetRootNode();
        assert(domJumpTree->OperIs(GT_JTRUE));
        GenTree* const domCmpTree = domJumpTree->AsOp()->gtGetOp1();

        if (!domCmpTree->OperIsCompare())
        {
            continue;
        }

        // We can use liberal VNs here, as bounds checks are not yet
        // manifest explicitly as relops.
        //
        RelopImplicationInfo rii;
        rii.treeNormVN   = vn;
        rii.domCmpNormVN = m_comp->vnStore->VNLiberalNormalValue(domCmpTree->gtVNPair);

        m_comp->optRelopImpliesRelop(&rii);

        if (!rii.canInfer)
        {
            continue;
        }

        bool domIsInferredRelop = (rii.vnRelation == ValueNumStore::VN_RELATION_KIND::VRK_Inferred);
        bool domIsSameRelop     = (rii.vnRelation == ValueNumStore::VN_RELATION_KIND::VRK_Same) ||
                              (rii.vnRelation == ValueNumStore::VN_RELATION_KIND::VRK_Swap);

        bool trueReaches  = m_comp->optReachable(idom->GetTrueTarget(), m_loop->GetHeader(), idom);
        bool falseReaches = m_comp->optReachable(idom->GetFalseTarget(), m_loop->GetHeader(), idom);

        if (trueReaches && !falseReaches && rii.canInferFromTrue)
        {
            bool relopIsTrue = rii.reverseSense ^ (domIsSameRelop | domIsInferredRelop);
            return relopIsTrue ? RelopEvaluationResult::True : RelopEvaluationResult::False;
        }

        if (falseReaches && !trueReaches && rii.canInferFromFalse)
        {
            bool relopIsFalse = rii.reverseSense ^ (domIsSameRelop | domIsInferredRelop);
            return relopIsFalse ? RelopEvaluationResult::False : RelopEvaluationResult::True;
        }
    }

    return RelopEvaluationResult::Unknown;
}

//------------------------------------------------------------------------
// MayOverflowBeforeExit:
//   Check if an add recurrence may overflow its computation type before an
//   exit condition returns true.
//
// Parameters:
//   lhs    - The LHS of an expression causing the loop to exit. An add recurrence.
//   rhs    - The RHS of the expression causing the loop to exit.
//   exitOp - The relop such that when [lhs] [exitOp] [rhs] evaluates to true, the loop exits.
//
// Returns:
//   True if it is possible for the LHS to overflow its computation type without the loop exiting.
//   False if we were able to prove that it cannot.
//
// Remarks:
//   May return true conservatively.
//
bool ScalarEvolutionContext::MayOverflowBeforeExit(ScevAddRec* lhs, Scev* rhs, VNFunc exitOp)
{
    int64_t stepCns;
    if (!lhs->Step->GetConstantValue(m_comp, &stepCns))
    {
        // TODO-CQ: With divisibility checks we can likely handle some of
        // these.
        return true;
    }

    // Handle odd cases, where we count in the other direction of the test, and
    // thus only exit after overflow (or immediately).
    //
    // TODO-CQ: One potential pattern we ought to be able to handle for
    // downwards counting loops with unsigned indices is:
    // for (uint i = (uint)arr.Length - 1u; i < (uint)arr.Length; i--)
    // This one overflows, but on overflow it immediately exits.
    //
    switch (exitOp)
    {
        case VNF_GE:
        case VNF_GT:
        case VNF_GE_UN:
        case VNF_GT_UN:
            if (stepCns < 0)
            {
                return true;
            }

            break;
        case VNF_LE:
        case VNF_LT:
        case VNF_LE_UN:
        case VNF_LT_UN:
            if (stepCns > 0)
            {
                return true;
            }

            break;
        default:
            unreached();
    }

    // A step count of 1/-1 will always exit for "or equal" checks.
    if ((stepCns == 1) && ((exitOp == VNFunc(GT_GE)) || (exitOp == VNF_GE_UN)))
    {
        return false;
    }

    if ((stepCns == -1) && ((exitOp == VNFunc(GT_LE)) || (exitOp == VNF_LE_UN)))
    {
        return false;
    }

    // Example: If exitOp is GT_GT then it means that we exit when iv >
    // limitCns. In the worst case the IV ends up at limitCns and then steps
    // once more. If that still causes us to exit then no overflow is possible.
    Scev* step = lhs->Step;
    if ((exitOp == VNF_GE) || (exitOp == VNF_GE_UN))
    {
        // Exit on iv >= limitCns, so in worst case we are at limitCns - 1. Include the -1 in the step.
        Scev* negOne = NewConstant(rhs->Type, -1);
        step         = NewBinop(ScevOper::Add, step, negOne);
    }
    else if ((exitOp == VNF_LE) || (exitOp == VNF_LE_UN))
    {
        // Exit on iv <= limitCns, so in worst case we are at limitCns + 1. Include the +1 in the step.
        Scev* posOne = NewConstant(rhs->Type, 1);
        step         = NewBinop(ScevOper::Add, step, posOne);
    }

    Scev* steppedVal           = NewBinop(ScevOper::Add, rhs, step);
    steppedVal                 = Simplify(steppedVal);
    ValueNumPair steppedValVNP = MaterializeVN(steppedVal);

    ValueNumPair rhsVNP = MaterializeVN(rhs);
    ValueNum     relop  = m_comp->vnStore->VNForFunc(TYP_INT, exitOp, steppedValVNP.GetLiberal(), rhsVNP.GetLiberal());
    RelopEvaluationResult result = EvaluateRelop(relop);
    return result != RelopEvaluationResult::True;
}

//------------------------------------------------------------------------
// AddRecMayOverflow:
//   Check if an add recurrence may overflow inside the containing loop.
//
// Parameters:
//   addRec      - The add recurrence
//   signedBound - Whether to check using signed (true) or unsigned (false) bounds.
//   assumptions - Assumptions about the containing loop.
//
// Returns:
//   True if the add recurrence may overflow and wrap around. False if we were
//   able to prove that it cannot.
//
// Remarks:
//   May return true conservatively.
//
bool ScalarEvolutionContext::AddRecMayOverflow(ScevAddRec*                      addRec,
                                               bool                             signedBound,
                                               const SimplificationAssumptions& assumptions)
{
    if (assumptions.NumBackEdgeTakenBound == 0)
    {
        return true;
    }

    if (!addRec->TypeIs(TYP_INT))
    {
        return true;
    }

    // In general we are interested in proving that the add recurrence does not
    // cross the minimum or maximum bounds during the iteration of the loop:
    //
    // For signed bounds   (sext): sext(a + b) != sext(a) + sext(b) if a + b crosses -2^31 or 2^31 - 1.
    // For unsigned bounds (zext): zext(a + b) != zext(a) + zext(b) if a + b crosses 0 or 2^32 - 1.
    //
    // We need to verify this condition for all i < bound where a = start, b =
    // step + i.
    //
    // For now, we only handle the super duper simple case of unsigned bounds
    // with addRec = <L, 0, 1> and a TYP_INT bound.
    //
    if (signedBound)
    {
        return true;
    }

    int64_t startCns;
    if (addRec->Start->GetConstantValue(m_comp, &startCns) && (startCns != 0))
    {
        return true;
    }

    int64_t stepCns;
    if (!addRec->Step->GetConstantValue(m_comp, &stepCns) || (stepCns != 1))
    {
        return true;
    }

    for (unsigned i = 0; i < assumptions.NumBackEdgeTakenBound; i++)
    {
        Scev* bound = assumptions.BackEdgeTakenBound[i];
        if (bound->TypeIs(TYP_INT))
        {
            return false;
        }
    }

    return true;
}

//------------------------------------------------------------------------
// MapRelopToVNFunc:
//   Given a potentially unsigned IR relop, map it to a VNFunc.
//
// Parameters:
//   oper       - The IR oper.
//   isUnsigned - Whether or not this is an unsigned relop.
//
// Returns:
//   The VNFunc for the (potentially unsigned) relop.
//
VNFunc ScalarEvolutionContext::MapRelopToVNFunc(genTreeOps oper, bool isUnsigned)
{
    if (isUnsigned)
    {
        switch (oper)
        {
            case GT_EQ:
            case GT_NE:
                return VNFunc(oper);
            case GT_LT:
                return VNF_LT_UN;
            case GT_LE:
                return VNF_LE_UN;
            case GT_GT:
                return VNF_GT_UN;
            case GT_GE:
                return VNF_GE_UN;
            default:
                unreached();
        }
    }
    else
    {
        return VNFunc(oper);
    }
}

//------------------------------------------------------------------------
// ComputeExitNotTakenCount:
//   Given an exiting basic block, try to compute an exact expression for the
//   number of times the exit is not taken, before it is taken.
//
// Parameters:
//   exiting - The exiting basic block. Must be a BBJ_COND block.
//
// Returns:
//   A SCEV representing the number of times the exit is _not_ taken, assuming
//   it runs on every iteration of the loop. Returns nullptr if an exact count
//   cannot be computed.
//
// Remarks:
//   The SCEV returned here is equal to the backedge count when the exiting
//   block dominates all backedges and when it is the only exit of the loop.
//
//   The backedge count of the loop is defined as the number of times the
//   header block is entered from a backedge. It follows that the number of
//   times the header block is entered is the backedge count + 1. This quantity
//   is typically called the trip count.
//
//   The backedge count gives insight about add recurrences in the loop, since
//   it is the number of times every add recurrence steps. The final value of
//   an add recurrence <L, start, step> is thus (start + step * <backedge
//   count>).
//
//   Loops for which the backedge/trip count can be computed are called counted
//   loops.
//
Scev* ScalarEvolutionContext::ComputeExitNotTakenCount(BasicBlock* exiting)
{
    assert(exiting->KindIs(BBJ_COND));
    assert(m_loop->ContainsBlock(exiting->GetTrueTarget()) != m_loop->ContainsBlock(exiting->GetFalseTarget()));

    Statement* lastStmt = exiting->lastStmt();
    GenTree*   lastExpr = lastStmt->GetRootNode();
    assert(lastExpr->OperIs(GT_JTRUE));
    GenTree* cond = lastExpr->gtGetOp1();

    if (!cond->OperIs(GT_LT, GT_LE, GT_GT, GT_GE))
    {
        // TODO-CQ: We can handle EQ/NE with divisibility checks.
        return nullptr;
    }

    if (!varTypeIsIntegralOrI(cond->gtGetOp1()))
    {
        return nullptr;
    }

    Scev* op1 = Analyze(exiting, cond->gtGetOp1());
    Scev* op2 = Analyze(exiting, cond->gtGetOp2());

    if ((op1 == nullptr) || (op2 == nullptr))
    {
        return nullptr;
    }

    if (varTypeIsGC(op1->Type) || varTypeIsGC(op2->Type))
    {
        // TODO-CQ: Add SUB operator
        return nullptr;
    }

    // Now phrase the test such that the loop is exited when [lhs] >/>= [rhs].
    Scev*      lhs    = Simplify(op1);
    Scev*      rhs    = Simplify(op2);
    genTreeOps exitOp = cond->gtOper;
    if (!m_loop->ContainsBlock(exiting->GetFalseTarget()))
    {
        // We exit in the false case, so we exit when the oper is the reverse.
        exitOp = GenTree::ReverseRelop(exitOp);
    }

    // We require an add recurrence to have been exposed at this point.
    if (!lhs->OperIs(ScevOper::AddRec) && !rhs->OperIs(ScevOper::AddRec))
    {
        // If both are invariant we could still handle some cases here (it will
        // be 0 or infinite). Probably uncommon.
        return nullptr;
    }

    // Now normalize variant SCEV to the left.
    bool lhsInvariant = lhs->IsInvariant();
    bool rhsInvariant = rhs->IsInvariant();
    if (lhsInvariant == rhsInvariant)
    {
        // Both variant. Here we could also prove also try to prove some cases,
        // but again this is expected to be uncommon.
        return nullptr;
    }

    if (lhsInvariant)
    {
        exitOp = GenTree::SwapRelop(exitOp);
        std::swap(lhs, rhs);
    }

    assert(lhs->OperIs(ScevOper::AddRec));

#ifdef DEBUG
    if (m_comp->verbose)
    {
        printf("  " FMT_LP " exits when:\n  ", m_loop->GetIndex());
        lhs->Dump(m_comp);
        const char* exitOpStr = "";
        switch (exitOp)
        {
            case GT_LT:
                exitOpStr = "<";
                break;
            case GT_LE:
                exitOpStr = "<=";
                break;
            case GT_GT:
                exitOpStr = ">";
                break;
            case GT_GE:
                exitOpStr = ">=";
                break;
            default:
                unreached();
        }
        printf(" %s ", exitOpStr);
        rhs->Dump(m_comp);
        printf("\n");
    }
#endif

    VNFunc exitOpVNF = MapRelopToVNFunc(exitOp, cond->IsUnsigned());
    if (MayOverflowBeforeExit((ScevAddRec*)lhs, rhs, exitOpVNF))
    {
        JITDUMP("  May overflow, cannot determine backedge count\n");
        return nullptr;
    }

    JITDUMP("  Does not overflow past the test\n");

    // We have lhs [exitOp] rhs, where lhs is an add rec
    Scev* lowerBound;
    Scev* upperBound;
    Scev* divisor;
    switch (exitOpVNF)
    {
        case VNF_GE:
        case VNF_GE_UN:
        {
            // Exit on <L, start, step> >= rhs.
            // Trip count expression is ceil((rhs - start) / step) = (rhs + (step - 1) - start) / step.
            Scev* stepNegOne  = NewBinop(ScevOper::Add, ((ScevAddRec*)lhs)->Step, NewConstant(rhs->Type, -1));
            Scev* rhsWithStep = NewBinop(ScevOper::Add, rhs, stepNegOne);
            lowerBound        = ((ScevAddRec*)lhs)->Start;
            upperBound        = rhsWithStep;
            divisor           = ((ScevAddRec*)lhs)->Step;
            break;
        }
        case VNF_GT:
        case VNF_GT_UN:
        {
            // Exit on <L, start, step> > rhs.
            // Trip count expression is ceil((rhs + 1 - start) / step) = (rhs + step - start) / step.
            lowerBound = ((ScevAddRec*)lhs)->Start;
            upperBound = NewBinop(ScevOper::Add, rhs, ((ScevAddRec*)lhs)->Step);
            divisor    = ((ScevAddRec*)lhs)->Step;
            break;
        }
        case VNF_LE:
        case VNF_LE_UN:
        {
            // Exit on <L, start, step> <= rhs.
            // Trip count expression is ceil((start - rhs) / -step) = (start + (-step - 1) - rhs) / -step
            // = (start - step - 1 - rhs) / -step = start - (rhs + step + 1) / -step.
            Scev* stepPlusOne = NewBinop(ScevOper::Add, ((ScevAddRec*)lhs)->Step, NewConstant(rhs->Type, 1));
            Scev* rhsWithStep = NewBinop(ScevOper::Add, rhs, stepPlusOne);
            lowerBound        = rhsWithStep;
            upperBound        = ((ScevAddRec*)lhs)->Start;
            divisor           = NewBinop(ScevOper::Mul, ((ScevAddRec*)lhs)->Step, NewConstant(lhs->Type, -1));
            break;
        }
        case VNF_LT:
        case VNF_LT_UN:
        {
            // Exit on <L, start, step> < rhs.
            // Trip count expression is ceil((start - (rhs - 1)) / -step) = (start + (-step - 1) - (rhs - 1)) / -step
            // = (start - (rhs + step)) / -step.
            lowerBound = NewBinop(ScevOper::Add, rhs, ((ScevAddRec*)lhs)->Step);
            upperBound = ((ScevAddRec*)lhs)->Start;
            divisor    = NewBinop(ScevOper::Mul, ((ScevAddRec*)lhs)->Step, NewConstant(lhs->Type, -1));
            break;
        }
        default:
            unreached();
    }

    lowerBound = Simplify(lowerBound);
    upperBound = Simplify(upperBound);

    // Now prove that the lower bound is indeed a lower bound.
    JITDUMP("  Need to prove ");
    DBEXEC(VERBOSE, lowerBound->Dump(m_comp));
    JITDUMP(" <= ");
    DBEXEC(VERBOSE, upperBound->Dump(m_comp));

    VNFunc       relopFunc     = ValueNumStore::VNFuncIsSignedComparison(exitOpVNF) ? VNF_LE : VNF_LE_UN;
    ValueNumPair lowerBoundVNP = MaterializeVN(lowerBound);
    if (lowerBoundVNP.GetLiberal() == ValueNumStore::NoVN)
    {
        return nullptr;
    }

    ValueNumPair upperBoundVNP = MaterializeVN(upperBound);
    if (upperBoundVNP.GetLiberal() == ValueNumStore::NoVN)
    {
        return nullptr;
    }

    ValueNum relop =
        m_comp->vnStore->VNForFunc(TYP_INT, relopFunc, lowerBoundVNP.GetLiberal(), upperBoundVNP.GetLiberal());
    RelopEvaluationResult result = EvaluateRelop(relop);
    JITDUMP(": %s\n", RelopEvaluationResultString(result));

    if (result != RelopEvaluationResult::True)
    {
        return nullptr;
    }

    divisor = Simplify(divisor);
    int64_t divisorVal;
    if (!divisor->GetConstantValue(m_comp, &divisorVal) || ((divisorVal != 1) && (divisorVal != -1)))
    {
        // TODO-CQ: Enable. Likely need to add a division operator to SCEV.
        return nullptr;
    }

    Scev* backedgeCountSubtraction =
        NewBinop(ScevOper::Add, upperBound, NewBinop(ScevOper::Mul, lowerBound, NewConstant(lowerBound->Type, -1)));
    Scev* backedgeCount = backedgeCountSubtraction;
    if (divisorVal == -1)
    {
        backedgeCount = NewBinop(ScevOper::Mul, backedgeCount, NewConstant(backedgeCount->Type, -1));
    }

    backedgeCount = Simplify(backedgeCount);
    JITDUMP("  Backedge count: ");
    DBEXEC(VERBOSE, backedgeCount->Dump(m_comp));
    JITDUMP("\n");

    return backedgeCount;
}
