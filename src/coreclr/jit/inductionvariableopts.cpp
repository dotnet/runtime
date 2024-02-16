// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// This file contains code to analyze how the value of induction variables
// evolve (scalar evolution analysis) and to do optimizations based on it.
// Currently the only optimization done is IV widening.
// The scalar evolution analysis is inspired by "Michael Wolfe. 1992. Beyond
// induction variables." and also by LLVM's scalar evolution.

#include "jitpch.h"

// Evolving values are described using a small IR based around the following
// possible operations. At the core is ScevOper::AddRec, which represents a
// value that evolves by an add recurrence. In dumps it is described by <loop,
// start, step> where "loop" is the loop the value is evolving in, "start" is
// the initial value and "step" is the step by which the value evolves in every
// iteration.
//
enum class ScevOper
{
    Constant,
    Local,
    ZeroExtend,
    SignExtend,
    Add,
    Mul,
    Lsh,
    AddRec,
};

static bool ScevOperIs(ScevOper oper, ScevOper otherOper)
{
    return oper == otherOper;
}

template <typename... Args>
static bool ScevOperIs(ScevOper oper, ScevOper operFirst, Args... operTail)
{
    return oper == operFirst || ScevOperIs(oper, operTail...);
}

struct Scev
{
    const ScevOper  Oper;
    const var_types Type;

    Scev(ScevOper oper, var_types type) : Oper(oper), Type(type)
    {
    }

    template <typename... Args>
    bool OperIs(Args... opers)
    {
        return ScevOperIs(Oper, opers...);
    }

    bool TypeIs(var_types type)
    {
        return Type == type;
    }

    bool GetConstantValue(Compiler* comp, int64_t* cns);
};

struct ScevConstant : Scev
{
    ScevConstant(var_types type, int64_t value) : Scev(ScevOper::Constant, type), Value(value)
    {
    }

    int64_t Value;
};

struct ScevLocal : Scev
{
    ScevLocal(var_types type, unsigned lclNum, unsigned ssaNum)
        : Scev(ScevOper::Local, type), LclNum(lclNum), SsaNum(ssaNum)
    {
    }

    const unsigned LclNum;
    const unsigned SsaNum;

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
    bool GetConstantValue(Compiler* comp, int64_t* cns)
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
};

struct ScevUnop : Scev
{
    ScevUnop(ScevOper oper, var_types type, Scev* op1) : Scev(oper, type), Op1(op1)
    {
    }

    Scev* const Op1;
};

struct ScevBinop : ScevUnop
{
    ScevBinop(ScevOper oper, var_types type, Scev* op1, Scev* op2) : ScevUnop(oper, type, op1), Op2(op2)
    {
    }

    Scev* const Op2;
};

// Represents a value that evolves by an add recurrence.
// The value at iteration N is Start + N * Step.
// "Start" and "Step" are guaranteed to be invariant in "Loop".
struct ScevAddRec : Scev
{
    ScevAddRec(var_types type, Scev* start, Scev* step) : Scev(ScevOper::AddRec, type), Start(start), Step(step)
    {
    }

    Scev* const Start;
    Scev* const Step;
};

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

typedef JitHashTable<GenTree*, JitPtrKeyFuncs<GenTree>, Scev*> ScalarEvolutionMap;

// Scalar evolution is analyzed in the context of a single loop, and are
// computed on-demand by the use of the "Analyze" method on this class, which
// also maintains a cache.
class ScalarEvolutionContext
{
    Compiler*             m_comp;
    FlowGraphNaturalLoop* m_loop = nullptr;
    ScalarEvolutionMap    m_cache;

    Scev* Analyze(BasicBlock* block, GenTree* tree, int depth);
    Scev* AnalyzeNew(BasicBlock* block, GenTree* tree, int depth);
    Scev* CreateSimpleAddRec(GenTreeLclVarCommon* headerStore,
                             ScevLocal*           start,
                             BasicBlock*          stepDefBlock,
                             GenTree*             stepDefData);
    Scev* CreateSimpleInvariantScev(GenTree* tree);
    Scev* CreateScevForConstant(GenTreeIntConCommon* tree);

public:
    ScalarEvolutionContext(Compiler* comp) : m_comp(comp), m_cache(comp->getAllocator(CMK_LoopScalarEvolution))
    {
    }

    void DumpScev(Scev* scev);

    //------------------------------------------------------------------------
    // ResetForLoop: Reset the internal cache in preparation of scalar
    // evolution analysis inside a new loop.
    //
    // Parameters:
    //    loop - The loop.
    //
    void ResetForLoop(FlowGraphNaturalLoop* loop)
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
    ScevConstant* NewConstant(var_types type, int64_t value)
    {
        ScevConstant* constant = new (m_comp, CMK_LoopScalarEvolution) ScevConstant(type, value);
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
    ScevLocal* NewLocal(unsigned lclNum, unsigned ssaNum)
    {
        var_types  type           = genActualType(m_comp->lvaGetDesc(lclNum));
        ScevLocal* invariantLocal = new (m_comp, CMK_LoopScalarEvolution) ScevLocal(type, lclNum, ssaNum);
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
    ScevUnop* NewExtension(ScevOper oper, var_types targetType, Scev* op)
    {
        assert(op != nullptr);
        ScevUnop* ext = new (m_comp, CMK_LoopScalarEvolution) ScevUnop(oper, targetType, op);
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
    ScevBinop* NewBinop(ScevOper oper, Scev* op1, Scev* op2)
    {
        assert((op1 != nullptr) && (op2 != nullptr));
        ScevBinop* binop = new (m_comp, CMK_LoopScalarEvolution) ScevBinop(oper, op1->Type, op1, op2);
        return binop;
    }

    //------------------------------------------------------------------------
    // NewAddRec: Create a SCEV node that represents a new add recurrence.
    //
    // Parameters:
    //   start - Value of the recurrence at the first iteration
    //   step  - Step value of the recurrence
    //
    // Returns:
    //   The new node.
    //
    ScevAddRec* NewAddRec(Scev* start, Scev* step)
    {
        assert((start != nullptr) && (step != nullptr));
        ScevAddRec* addRec = new (m_comp, CMK_LoopScalarEvolution) ScevAddRec(start->Type, start, step);
        return addRec;
    }

    Scev* Analyze(BasicBlock* block, GenTree* tree);
    Scev* Simplify(Scev* scev);
};

#ifdef DEBUG
//------------------------------------------------------------------------
// DumpScev: Print a scev node to stdout.
//
// Parameters:
//   scev - The scev node.
//
void ScalarEvolutionContext::DumpScev(Scev* scev)
{
    switch (scev->Oper)
    {
        case ScevOper::Constant:
        {
            ScevConstant* cns = (ScevConstant*)scev;
            printf("%zd", (ssize_t)cns->Value);
            break;
        }
        case ScevOper::Local:
        {
            ScevLocal* invariantLocal = (ScevLocal*)scev;
            printf("V%02u.%u", invariantLocal->LclNum, invariantLocal->SsaNum);

            int64_t cns;
            if (invariantLocal->GetConstantValue(m_comp, &cns))
            {
                printf(" (%lld)", (long long)cns);
            }
            break;
        }
        case ScevOper::ZeroExtend:
        case ScevOper::SignExtend:
        {
            ScevUnop* unop = (ScevUnop*)scev;
            printf("%cext<%d>(", unop->Oper == ScevOper::ZeroExtend ? 'z' : 's', genTypeSize(unop->Type) * 8);
            DumpScev(unop->Op1);
            printf(")");
            break;
        }
        case ScevOper::Add:
        case ScevOper::Mul:
        case ScevOper::Lsh:
        {
            ScevBinop* binop = (ScevBinop*)scev;
            printf("(");
            DumpScev(binop->Op1);
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
            DumpScev(binop->Op2);
            printf(")");
            break;
        }
        case ScevOper::AddRec:
        {
            ScevAddRec* addRec = (ScevAddRec*)scev;
            printf("<" FMT_LP, m_loop->GetIndex());
            printf(", ");
            DumpScev(addRec->Start);
            printf(", ");
            DumpScev(addRec->Step);
            printf(">");
            break;
        }
        default:
            unreached();
    }
}
#endif

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

            // We currently do not handle complicated addrecs. We can do this
            // by inserting a symbolic node in the cache and analyzing while it
            // is part of the cache. It would allow us to model
            //
            //   int i = 0;
            //   while (i < n)
            //   {
            //     int j = i + 1;
            //     ...
            //     i = j;
            //   }
            // => <L, 0, 1>
            //
            // and chains of recurrences, such as
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
            // The main issue is that it requires cache invalidation afterwards
            // and turning the recursive result into an addrec.
            //
            return CreateSimpleAddRec(store, enterScev, ssaDsc->GetBlock(), ssaDsc->GetDefNode()->Data());
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
//   depth - Current analysis depth.
//
// Returns:
//   SCEV node if the tree was analyzable; otherwise nullptr if the value is
//   cannot be described.
//
Scev* ScalarEvolutionContext::Analyze(BasicBlock* block, GenTree* tree, int depth)
{
    Scev* result;
    if (!m_cache.Lookup(tree, &result))
    {
        if (depth >= SCALAR_EVOLUTION_ANALYSIS_MAX_DEPTH)
        {
            return nullptr;
        }

        result = AnalyzeNew(block, tree, depth);
        m_cache.Set(tree, result);
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

//------------------------------------------------------------------------
// optCanSinkWidenedIV: Check to see if we are able to sink a store to the old
// local into the exits of a loop if we decide to widen.
//
// Parameters:
//   lclNum - The primary induction variable
//   loop   - The loop
//
// Returns:
//   True if we can sink a store to the old local after widening.
//
// Remarks:
//   This handles the situation where the primary induction variable is used
//   after the loop. In those cases we need to store the widened local back
//   into the old one in the exits where the IV variable is live.
//
//   We are able to sink when none of the exits are critical blocks, in the
//   sense that all their predecessors must come from inside the loop. Loop
//   exit canonicalization guarantees this for regular exit blocks. It is not
//   guaranteed for exceptional exits, but we do not expect to widen IVs that
//   are live into exceptional exits since those are marked DNER which makes it
//   unprofitable anyway.
//
//   Note that there may be natural loops that have not had their regular exits
//   canonicalized at the time when IV opts run, in particular if RBO/assertion
//   prop makes a previously unnatural loop natural. This function accounts for
//   and rejects these cases.
//
bool Compiler::optCanSinkWidenedIV(unsigned lclNum, FlowGraphNaturalLoop* loop)
{
    LclVarDsc* dsc = lvaGetDesc(lclNum);

    BasicBlockVisit result = loop->VisitRegularExitBlocks([=](BasicBlock* exit) {

        if (!VarSetOps::IsMember(this, exit->bbLiveIn, dsc->lvVarIndex))
        {
            JITDUMP("  Exit " FMT_BB " does not need a sink; V%02u is not live-in\n", exit->bbNum, lclNum);
            return BasicBlockVisit::Continue;
        }

        return BasicBlockVisit::Abort;

        //for (BasicBlock* pred : exit->PredBlocks())
        //{
        //    if (!loop->ContainsBlock(pred))
        //    {
        //        JITDUMP("  Cannot safely sink widened version of V%02u into exit " FMT_BB " of " FMT_LP
        //                "; it has a non-loop pred " FMT_BB "\n",
        //                lclNum, exit->bbNum, loop->GetIndex(), pred->bbNum);
        //        return BasicBlockVisit::Abort;
        //    }
        //}

        //return BasicBlockVisit::Continue;
    });

#ifdef DEBUG
    // We currently do not expect to ever widen IVs that are live into
    // exceptional exits. Such IVs are expected to have been marked DNER
    // previously (EH write-thru is only for single def locals) which makes it
    // unprofitable. If this ever changes we need some more expansive handling
    // here.
    loop->VisitLoopBlocks([=](BasicBlock* block) {

        block->VisitAllSuccs(this, [=](BasicBlock* succ) {
            if (!loop->ContainsBlock(succ) && bbIsHandlerBeg(succ))
            {
                assert(!VarSetOps::IsMember(this, succ->bbLiveIn, dsc->lvVarIndex) &&
                       "Candidate IV for widening is live into exceptional exit");
            }

            return BasicBlockVisit::Continue;
        });

        return BasicBlockVisit::Continue;
    });
#endif

    return result != BasicBlockVisit::Abort;
}

//------------------------------------------------------------------------
// optIsIVWideningProfitable: Check to see if IV widening is profitable.
//
// Parameters:
//   lclNum              - The primary induction variable
//   needsInitialization - Whether or not the widened IV will need explicit initialization
//   loop                - The loop
//
// Returns:
//   True if IV widening is profitable.
//
// Remarks:
//   IV widening is generally profitable when it allows us to remove casts
//   inside the loop. However, it may also introduce other reg-reg moves:
//     1. We may need to store the narrow IV into the wide one in the
//     preheader. This is necessary when the start value is not constant. If
//     the start value _is_ constant then we assume that the constant store to
//     the narrow local will be a DCE'd.
//     2. We need to store the wide IV back into the narrow one in each of
//     the exits where the narrow IV is live-in.
//
bool Compiler::optIsIVWideningProfitable(unsigned lclNum, bool reusedIV, BasicBlock* initBlock, bool initedToConstant, FlowGraphNaturalLoop* loop)
{
    struct CountZeroExtensionsVisitor : GenTreeVisitor<CountZeroExtensionsVisitor>
    {
    private:
        unsigned m_lclNum;

    public:
        enum
        {
            DoPreOrder = true,
        };

        unsigned NumExtensions = 0;

        CountZeroExtensionsVisitor(Compiler* comp, unsigned lclNum) : GenTreeVisitor(comp), m_lclNum(lclNum)
        {
        }

        fgWalkResult PreOrderVisit(GenTree** use, GenTree* parent)
        {
            GenTree* node = *use;

            if (!node->OperIs(GT_CAST))
            {
                return WALK_CONTINUE;
            }

            GenTreeCast* cast = node->AsCast();
            if ((cast->gtCastType != TYP_LONG) || !cast->IsUnsigned())
            {
                return WALK_CONTINUE;
            }

            GenTree* op = cast->CastOp();
            if (!op->OperIs(GT_LCL_VAR) || (op->AsLclVarCommon()->GetLclNum() != m_lclNum))
            {
                return WALK_CONTINUE;
            }

            // If this is already the source of a store then it is going to be
            // free in our backends regardless.
            if ((parent != nullptr) && parent->OperIs(GT_STORE_LCL_VAR))
            {
                return WALK_CONTINUE;
            }

            NumExtensions++;
            return WALK_SKIP_SUBTREES;
        }
    };

    const weight_t ExtensionCost = 2;
    const int      ExtensionSize = 3;

    CountZeroExtensionsVisitor visitor(this, lclNum);
    weight_t                   savedCost = 0;
    int                        savedSize = 0;

    loop->VisitLoopBlocks([&](BasicBlock* block) {
        for (Statement* stmt : block->NonPhiStatements())
        {
            visitor.WalkTree(stmt->GetRootNodePointer(), nullptr);

            if (visitor.NumExtensions > 0)
            {
                JITDUMP("  Found %u zero extensions in " FMT_STMT "\n", visitor.NumExtensions, stmt->GetID());

                savedSize += (int)visitor.NumExtensions * ExtensionSize;
                savedCost += visitor.NumExtensions * block->getBBWeight(this) * ExtensionCost;
                visitor.NumExtensions = 0;
            }
        }

        return BasicBlockVisit::Continue;
    });

    if (reusedIV)
    {
        // If we are reusing the IV then widening is going to remove a use of
        // the original narrow local, and we assume this leads to more DCE.
        savedSize += ExtensionSize;
        savedCost += initBlock->getBBWeight(this) * ExtensionCost;
    }
    else if (!initedToConstant)
    {
        // We will need to store the narrow IV into the wide one in the init
        // block. We only cost this when init value is not a constant since
        // otherwise we assume that constant initialization of the narrow local
        // will be DCE'd.
        savedSize -= ExtensionSize;
        savedCost -= initBlock->getBBWeight(this) * ExtensionCost;
    }

    // Now account for the cost of sinks.
    LclVarDsc* dsc = lvaGetDesc(lclNum);
    loop->VisitRegularExitBlocks([&](BasicBlock* exit) {
        if (VarSetOps::IsMember(this, exit->bbLiveIn, dsc->lvVarIndex))
        {
            savedSize -= ExtensionSize;
            savedCost -= exit->getBBWeight(this) * ExtensionCost;
        }
        return BasicBlockVisit::Continue;
    });

    const weight_t ALLOWED_SIZE_REGRESSION_PER_CYCLE_IMPROVEMENT = 2;
    weight_t       cycleImprovementPerInvoc                      = savedCost / fgFirstBB->getBBWeight(this);

    JITDUMP("  Estimated cycle improvement: " FMT_WT " cycles per invocation\n", cycleImprovementPerInvoc);
    JITDUMP("  Estimated size improvement: %d bytes\n", savedSize);

    if ((cycleImprovementPerInvoc > 0) &&
        ((cycleImprovementPerInvoc * ALLOWED_SIZE_REGRESSION_PER_CYCLE_IMPROVEMENT) >= -savedSize))
    {
        JITDUMP("    Widening is profitable (cycle improvement)\n");
        return true;
    }

    const weight_t ALLOWED_CYCLE_REGRESSION_PER_SIZE_IMPROVEMENT = 0.01;

    if ((savedSize > 0) && ((savedSize * ALLOWED_CYCLE_REGRESSION_PER_SIZE_IMPROVEMENT) >= -cycleImprovementPerInvoc))
    {
        JITDUMP("  Widening is profitable (size improvement)\n");
        return true;
    }

    JITDUMP("  Widening is not profitable\n");
    return false;
}

//------------------------------------------------------------------------
// optSinkWidenedIV: Create stores back to the narrow IV in the exits where
// that is necessary.
//
// Parameters:
//   lclNum    - Narrow version of primary induction variable
//   newLclNum - Wide version of primary induction variable
//   loop      - The loop
//
// Returns:
//   True if any store was created in any exit block.
//
bool Compiler::optSinkWidenedIV(unsigned lclNum, unsigned newLclNum, FlowGraphNaturalLoop* loop)
{
    bool       anySunk = false;
    LclVarDsc* dsc     = lvaGetDesc(lclNum);
    loop->VisitRegularExitBlocks([=, &anySunk](BasicBlock* exit) {
        if (!VarSetOps::IsMember(this, exit->bbLiveIn, dsc->lvVarIndex))
        {
            return BasicBlockVisit::Continue;
        }

        GenTree*   narrowing = gtNewCastNode(TYP_INT, gtNewLclvNode(newLclNum, TYP_LONG), false, TYP_INT);
        GenTree*   store     = gtNewStoreLclVarNode(lclNum, narrowing);
        Statement* newStmt   = fgNewStmtFromTree(store);
        JITDUMP("Narrow IV local V%02u live into exit block " FMT_BB "; sinking a narrowing\n", lclNum, exit->bbNum);
        DISPSTMT(newStmt);
        fgInsertStmtAtBeg(exit, newStmt);
        anySunk = true;

        return BasicBlockVisit::Continue;
    });

    return anySunk;
}

//------------------------------------------------------------------------
// optReplaceWidenedIV: Replace uses of the narrow IV with the wide IV in the
// specified statement.
//
// Parameters:
//   lclNum    - Narrow version of primary induction variable
//   newLclNum - Wide version of primary induction variable
//   stmt      - The statement to replace uses in.
//
void Compiler::optReplaceWidenedIV(unsigned lclNum, unsigned ssaNum, unsigned newLclNum, Statement* stmt)
{
    struct ReplaceVisitor : GenTreeVisitor<ReplaceVisitor>
    {
    private:
        unsigned m_lclNum;
        unsigned m_ssaNum;
        unsigned m_newLclNum;

        bool IsLocal(GenTreeLclVarCommon* tree)
        {
            return (tree->GetLclNum() == m_lclNum) && ((m_ssaNum == SsaConfig::RESERVED_SSA_NUM) || (tree->GetSsaNum() == m_ssaNum));
        }
    public:
        bool MadeChanges = false;

        enum
        {
            DoPreOrder = true,
        };

        ReplaceVisitor(Compiler* comp, unsigned lclNum, unsigned ssaNum, unsigned newLclNum)
            : GenTreeVisitor(comp), m_lclNum(lclNum), m_ssaNum(ssaNum), m_newLclNum(newLclNum)
        {
        }

        fgWalkResult PreOrderVisit(GenTree** use, GenTree* user)
        {
            GenTree* node = *use;
            if (node->OperIs(GT_CAST))
            {
                GenTreeCast* cast = node->AsCast();
                // TODO: Overflows
                if ((cast->gtCastType == TYP_LONG) && cast->IsUnsigned())
                {
                    GenTree* op = cast->CastOp();
                    if (op->OperIs(GT_LCL_VAR) && IsLocal(op->AsLclVarCommon()))
                    {
                        *use        = m_compiler->gtNewLclvNode(m_newLclNum, TYP_LONG);
                        MadeChanges = true;
                        return fgWalkResult::WALK_SKIP_SUBTREES;
                    }
                }
            }
            else if (node->OperIs(GT_LCL_VAR, GT_STORE_LCL_VAR, GT_LCL_FLD, GT_STORE_LCL_FLD) &&
                     IsLocal(node->AsLclVarCommon()))
            {
                switch (node->OperGet())
                {
                    case GT_LCL_VAR:
                        node->AsLclVarCommon()->SetLclNum(m_newLclNum);
                        // No cast needed -- the backend allows TYP_INT uses of TYP_LONG locals.
                        break;
                    case GT_STORE_LCL_VAR:
                    {
                        node->AsLclVarCommon()->SetLclNum(m_newLclNum);
                        node->gtType = TYP_LONG;
                        node->AsLclVarCommon()->Data() =
                            m_compiler->gtNewCastNode(TYP_LONG, node->AsLclVarCommon()->Data(), true, TYP_LONG);
                        break;
                    }
                    case GT_LCL_FLD:
                    case GT_STORE_LCL_FLD:
                        assert(!"Unexpected field use for local not marked as DNER");
                        break;
                    default:
                        break;
                }

                MadeChanges = true;
            }

            return fgWalkResult::WALK_CONTINUE;
        }
    };

    ReplaceVisitor visitor(this, lclNum, ssaNum, newLclNum);
    visitor.WalkTree(stmt->GetRootNodePointer(), nullptr);
    if (visitor.MadeChanges)
    {
        gtSetStmtInfo(stmt);
        fgSetStmtSeq(stmt);
        JITDUMP("New tree:\n", dspTreeID(stmt->GetRootNode()));
        DISPTREE(stmt->GetRootNode());
        JITDUMP("\n");
    }
    else
    {
        JITDUMP("No replacements made\n");
    }
}

//------------------------------------------------------------------------
// optBestEffortReplaceNarrowIVUsesWith: Try to find and replace uses of the specified
// SSA def with a new local.
//
// Parameters:
//   lclNum    - Previous local
//   ssaNum    - Previous local SSA num
//   newLclNum - New local to replace with
//   block     - Block to replace in
//   firstStmt - First statement in "block" to start replacing in
//
void Compiler::optBestEffortReplaceNarrowIVUsesWith(unsigned lclNum, unsigned ssaNum, unsigned newLclNum, BasicBlock* block, Statement* firstStmt)
{
    JITDUMP("  Replacing V%02u -> V%02u in " FMT_BB " starting at " FMT_STMT "\n", lclNum, newLclNum, block->bbNum, firstStmt == nullptr ? 0 : firstStmt->GetID());

    for (Statement* stmt = firstStmt; stmt != nullptr; stmt = stmt->GetNextStmt())
    {
        JITDUMP("  Replacing V%02u -> V%02u in [%06u]\n", lclNum, newLclNum,
                dspTreeID(stmt->GetRootNode()));
        DISPSTMT(stmt);
        JITDUMP("\n");

        optReplaceWidenedIV(lclNum, ssaNum, newLclNum, stmt);
    }

    block->VisitRegularSuccs(this, [=](BasicBlock* succ) {
        if (succ->GetUniquePred(this) == block)
        {
            optBestEffortReplaceNarrowIVUsesWith(lclNum, ssaNum, newLclNum, succ, succ->firstStmt());
        }

        return BasicBlockVisit::Continue;
        });
}

//------------------------------------------------------------------------
// optInductionVariables: Try and optimize induction variables in the method.
//
// Returns:
//   PhaseStatus indicating if anything changed.
//
PhaseStatus Compiler::optInductionVariables()
{
    JITDUMP("*************** In optInductionVariables()\n");

#ifdef DEBUG
    static ConfigMethodRange s_range;
    s_range.EnsureInit(JitConfig.JitEnableInductionVariableOptsRange());

    if (!s_range.Contains(info.compMethodHash()))
    {
        return PhaseStatus::MODIFIED_NOTHING;
    }
#endif

    if (!fgMightHaveNaturalLoops)
    {
        JITDUMP("  Skipping since this method has no natural loops\n");
        return PhaseStatus::MODIFIED_NOTHING;
    }

    bool changed = false;

    // Currently we only do IV widening which generally is only profitable for
    // x64 because arm64 addressing modes can include the zero/sign-extension
    // of the index for free.
    CLANG_FORMAT_COMMENT_ANCHOR;
#if defined(TARGET_XARCH) && defined(TARGET_64BIT)
    m_dfsTree = fgComputeDfs();
    m_loops   = FlowGraphNaturalLoops::Find(m_dfsTree);
    //m_domTree = FlowGraphDominatorTree::Build(m_dfsTree);

    fgDumpFlowGraph(PHASE_OPTIMIZE_INDUCTION_VARIABLES, PhasePosition::PostPhase);

    ScalarEvolutionContext scevContext(this);
    struct WidenedIV
    {
        unsigned LclNum;
        unsigned InitSsaNum;
        unsigned NewLclNum;

        WidenedIV(unsigned lclNum, unsigned initSsaNum, unsigned newLclNum)
            : LclNum(lclNum), InitSsaNum(initSsaNum), NewLclNum(newLclNum)
        {
        }
    };
    ArrayStack<WidenedIV> widenedIVs(getAllocator(CMK_LoopScalarEvolution));

    JITDUMP("Widening primary induction variables:\n");
    for (FlowGraphNaturalLoop* loop : m_loops->InReversePostOrder())
    {
        JITDUMP("Processing ");
        DBEXEC(verbose, FlowGraphNaturalLoop::Dump(loop));
        scevContext.ResetForLoop(loop);

        for (Statement* stmt : loop->GetHeader()->Statements())
        {
            if (!stmt->IsPhiDefnStmt())
            {
                break;
            }

            JITDUMP("\n");

            DISPSTMT(stmt);

            GenTreeLclVarCommon* lcl    = stmt->GetRootNode()->AsLclVarCommon();
            LclVarDsc*           lclDsc = lvaGetDesc(lcl);
            if (lclDsc->TypeGet() != TYP_INT)
            {
                JITDUMP("  Type is %s, no widening to be done\n", varTypeName(lclDsc->TypeGet()));
                continue;
            }

            // If the IV is not enregisterable then uses/defs are going to go
            // to stack regardless. This check also filters out IVs that may be
            // live into exceptional exits since those are always marked DNER.
            if (lclDsc->lvDoNotEnregister)
            {
                JITDUMP("  V%02u is marked DNER\n", lcl->GetLclNum());
                continue;
            }

            Scev* scev = scevContext.Analyze(loop->GetHeader(), stmt->GetRootNode());
            if (scev == nullptr)
            {
                JITDUMP("  Could not analyze header PHI\n");
                continue;
            }

            scev = scevContext.Simplify(scev);
            JITDUMP("  => ");
            DBEXEC(verbose, scevContext.DumpScev(scev));
            JITDUMP("\n");
            if (!scev->OperIs(ScevOper::AddRec))
            {
                JITDUMP("  Not an addrec\n");
                continue;
            }

            ScevAddRec* addRec = (ScevAddRec*)scev;

            JITDUMP("  V%02u is a primary induction variable in " FMT_LP "\n", lcl->GetLclNum(), loop->GetIndex());

            if (!optCanSinkWidenedIV(lcl->GetLclNum(), loop))
            {
                continue;
            }

            // Start value should always be an SSA use from outside the loop
            // since we only widen primary IVs.
            assert(addRec->Start->OperIs(ScevOper::Local));
            ScevLocal* startLocal = (ScevLocal*)addRec->Start;
            unsigned   newLclNum  = BAD_VAR_NUM;
            for (int i = 0; i < widenedIVs.Height(); i++)
            {
                WidenedIV& wiv = widenedIVs.BottomRef(i);
                if ((wiv.LclNum == startLocal->LclNum) && (wiv.InitSsaNum == startLocal->SsaNum))
                {
                    newLclNum = wiv.NewLclNum;
                    JITDUMP("  Reusing previously widened version with initial value V%02u.%u, new local V%02u\n",
                        wiv.LclNum, wiv.InitSsaNum, wiv.NewLclNum);
                    break;
                }
            }

            int64_t startConstant  = 0;
            bool    initToConstant = startLocal->GetConstantValue(this, &startConstant);
            LclSsaVarDsc* startSsaDsc = lclDsc->GetPerSsaData(startLocal->SsaNum);

            BasicBlock* preheader = loop->EntryEdge(0)->getSourceBlock();
            BasicBlock* initBlock = preheader;
            if (newLclNum != BAD_VAR_NUM)
            {
                assert(startSsaDsc->GetBlock() != nullptr);
                initBlock = startSsaDsc->GetBlock();
            }
            else
            {
                if (startSsaDsc->GetBlock() != nullptr)
                {
                    initBlock = startSsaDsc->GetBlock();
                }
            }

            bool    reusedIV       = newLclNum != BAD_VAR_NUM;
            if (!optIsIVWideningProfitable(lcl->GetLclNum(), reusedIV, initBlock, initToConstant, loop))
            {
                continue;
            }

            changed = true;

            Statement* narrowInitStmt = nullptr;
            if ((initBlock != preheader) && (startSsaDsc->GetDefNode() != nullptr))
            {
                GenTree* narrowInitRoot = startSsaDsc->GetDefNode();
                while (true)
                {
                    GenTree* parent = narrowInitRoot->gtGetParent(nullptr);
                    if (parent == nullptr)
                        break;

                    narrowInitRoot = parent;
                }

                for (Statement* stmt : initBlock->Statements())
                {
                    if (stmt->GetRootNode() == narrowInitRoot)
                    {
                        narrowInitStmt = stmt;
                        break;
                    }
                }

                assert(narrowInitStmt != nullptr);
            }

            Statement* initStmt = nullptr;
            if (newLclNum == BAD_VAR_NUM)
            {
                newLclNum = lvaGrabTemp(false DEBUGARG(printfAlloc("Widened IV V%02u", lcl->GetLclNum())));
                INDEBUG(lclDsc = nullptr);
                assert(startLocal->LclNum == lcl->GetLclNum());

                if (initBlock != preheader)
                {
                    JITDUMP(
                        "Adding initialization of new widened local to same block as reaching def outside loop, " FMT_BB
                        "\n",
                        initBlock->bbNum);

                    // Any other use of this IV can reuse the same wide local/initialization.
                    widenedIVs.Emplace(startLocal->LclNum, startLocal->SsaNum, newLclNum);
                }
                else
                {
                    JITDUMP("Adding initialization of new widened local to preheader " FMT_BB "\n", initBlock->bbNum);
                }

                GenTree* initVal;
                if (initToConstant)
                {
                    initVal = gtNewIconNode((int64_t)(uint32_t)startConstant, TYP_LONG);
                }
                else
                {
                    initVal = gtNewCastNode(TYP_LONG, gtNewLclvNode(lcl->GetLclNum(), TYP_INT), true, TYP_LONG);
                }

                GenTree*   widenStore = gtNewTempStore(newLclNum, initVal);
                initStmt   = fgNewStmtFromTree(widenStore);
                if (narrowInitStmt != nullptr)
                {
                    fgInsertStmtAfter(initBlock, narrowInitStmt, initStmt);
                }
                else
                {
                    fgInsertStmtNearEnd(initBlock, initStmt);
                }

                DISPSTMT(initStmt);
                JITDUMP("\n");
            }

            JITDUMP("  Replacing uses of V%02u with widened version V%02u\n", lcl->GetLclNum(), newLclNum);

            if (initStmt != nullptr)
            {
                optBestEffortReplaceNarrowIVUsesWith(lcl->GetLclNum(), startLocal->SsaNum, newLclNum, initBlock, initStmt->GetNextStmt());
            }

            loop->VisitLoopBlocks([=](BasicBlock* block) {

                for (Statement* stmt : block->NonPhiStatements())
                {
                    JITDUMP("Replacing V%02u -> V%02u in [%06u]\n", lcl->GetLclNum(), newLclNum,
                            dspTreeID(stmt->GetRootNode()));
                    DISPSTMT(stmt);
                    JITDUMP("\n");
                    optReplaceWidenedIV(lcl->GetLclNum(), SsaConfig::RESERVED_SSA_NUM, newLclNum, stmt);
                }

                return BasicBlockVisit::Continue;
            });

            changed |= optSinkWidenedIV(lcl->GetLclNum(), newLclNum, loop);
        }
    }

    fgInvalidateDfsTree();
#endif

    return changed ? PhaseStatus::MODIFIED_EVERYTHING : PhaseStatus::MODIFIED_NOTHING;
}
