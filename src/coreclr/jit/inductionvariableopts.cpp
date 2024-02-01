// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "jitpch.h"

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
    ScevOper  Oper;
    var_types Type;

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

    unsigned LclNum;
    unsigned SsaNum;
};

struct ScevUnop : Scev
{
    ScevUnop(ScevOper oper, var_types type, Scev* op1) : Scev(oper, type), Op1(op1)
    {
    }

    Scev* Op1;
};

struct ScevBinop : ScevUnop
{
    ScevBinop(ScevOper oper, var_types type, Scev* op1, Scev* op2) : ScevUnop(oper, type, op1), Op2(op2)
    {
    }

    Scev* Op2;
};

// Represents a value that evolves by an add recurrence.
// The value at iteration N is Start + N * Step.
// "Step" is guaranteed to be invariant in "Loop".
struct ScevAddRec : Scev
{
    ScevAddRec(var_types type, FlowGraphNaturalLoop* loop, Scev* start, Scev* step)
        : Scev(ScevOper::AddRec, type), Loop(loop), Start(start), Step(step)
    {
    }

    FlowGraphNaturalLoop* Loop;
    Scev*                 Start;
    Scev*                 Step;
};

typedef JitHashTable<GenTree*, JitPtrKeyFuncs<GenTree>, Scev*> ScalarEvolutionMap;

static void DumpScev(Scev* scev)
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
            case ScevOper::Add: op = "+"; break;
            case ScevOper::Mul: op = "*"; break;
            case ScevOper::Lsh: op = "<<"; break;
            default: unreached();
            }
            printf(" %s ", op);
            DumpScev(binop->Op2);
            printf(")");
            break;
        }
        case ScevOper::AddRec:
        {
            ScevAddRec* addRec = (ScevAddRec*)scev;
            printf("<" FMT_LP, addRec->Loop->GetIndex());
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

class ScalarEvolutionContext
{
    Compiler*          m_comp;
    FlowGraphNaturalLoop* m_loop = nullptr;
    ScalarEvolutionMap m_cache;
    ScalarEvolutionMap m_cyclicCache;
    bool m_usingCyclicCache = false;

    Scev* AnalyzeNew(BasicBlock* block, GenTree* tree);
    Scev* CreateSimpleAddRec(GenTreeLclVarCommon* headerStore, Scev* start, BasicBlock* stepDefBlock, GenTree* stepDefData);
    Scev* MakeAddRecFromRecursiveScev(Scev* start, Scev* scev, Scev* recursiveScev);
    GenTreeLclVarCommon* GetSsaDef(GenTreeLclVarCommon* lcl, BasicBlock** defBlock);
    Scev* CreateSimpleInvariantScev(GenTree* tree);
    bool TrackedLocalVariesInLoop(unsigned lclNum);

public:
    ScalarEvolutionContext(Compiler* comp) :
        m_comp(comp),
        m_cache(comp->getAllocator(CMK_LoopScalarEvolution)),
        m_cyclicCache(comp->getAllocator(CMK_LoopScalarEvolution))
    {
    }

    void ResetForLoop(FlowGraphNaturalLoop* loop)
    {
        m_loop = loop;
        m_cache.RemoveAll();
    }

    ScevConstant* NewConstant(var_types type, int64_t value)
    {
        ScevConstant* constant = new (m_comp, CMK_LoopScalarEvolution) ScevConstant(type, value);
        return constant;
    }

    ScevLocal* NewLocal(unsigned lclNum, unsigned ssaNum)
    {
        var_types           type = genActualType(m_comp->lvaGetDesc(lclNum));
        ScevLocal* invariantLocal =
            new (m_comp, CMK_LoopScalarEvolution) ScevLocal(type, lclNum, ssaNum);
        return invariantLocal;
    }

    ScevUnop* NewExtension(ScevOper oper, var_types targetType, Scev* op)
    {
        assert(op != nullptr);
        ScevUnop* ext = new (m_comp, CMK_LoopScalarEvolution) ScevUnop(oper, targetType, op);
        return ext;
    }

    ScevBinop* NewBinop(ScevOper oper, Scev* op1, Scev* op2)
    {
        assert((op1 != nullptr) && (op2 != nullptr));
        ScevBinop* binop = new (m_comp, CMK_LoopScalarEvolution) ScevBinop(oper, op1->Type, op1, op2);
        return binop;
    }

    ScevAddRec* NewAddRec(FlowGraphNaturalLoop* loop, Scev* start, Scev* step)
    {
        assert((start != nullptr) && (step != nullptr));
        ScevAddRec* addRec = new (m_comp, CMK_LoopScalarEvolution) ScevAddRec(start->Type, loop, start, step);
        return addRec;
    }

    Scev* Analyze(BasicBlock* block, GenTree* tree);

    Scev* Fold(Scev* scev);
};

GenTreeLclVarCommon* ScalarEvolutionContext::GetSsaDef(GenTreeLclVarCommon* lcl, BasicBlock** defBlock)
{
    assert(lcl->OperIs(GT_LCL_VAR, GT_PHI_ARG));
    if (!lcl->HasSsaName())
        return nullptr;

    LclVarDsc*           dsc    = m_comp->lvaGetDesc(lcl);
    LclSsaVarDsc*        ssaDsc = dsc->GetPerSsaData(lcl->GetSsaNum());
    GenTreeLclVarCommon* ssaDef = ssaDsc->GetDefNode();
    if (ssaDef == nullptr)
    {
        assert(lcl->GetSsaNum() == SsaConfig::FIRST_SSA_NUM);
        // TODO: We should handle zero-inited locals and parameters in some proper way...
        return nullptr;
    }
    assert(ssaDef->OperIsLocalStore());
    *defBlock = ssaDsc->GetBlock();
    return ssaDef;
}

Scev* ScalarEvolutionContext::CreateSimpleInvariantScev(GenTree* tree)
{
    if (tree->OperIsConst())
    {
        return NewConstant(tree->TypeGet(), tree->AsIntConCommon()->IntegralValue());
    }

    if (tree->OperIs(GT_LCL_VAR) && tree->AsLclVarCommon()->HasSsaName())
    {
        LclVarDsc* dsc = m_comp->lvaGetDesc(tree->AsLclVarCommon());
        LclSsaVarDsc* ssaDsc = dsc->GetPerSsaData(tree->AsLclVarCommon()->GetSsaNum());

        if ((ssaDsc->GetBlock() == nullptr) || !m_loop->ContainsBlock(ssaDsc->GetBlock()))
        {
            return NewLocal(tree->AsLclVarCommon()->GetLclNum(), tree->AsLclVarCommon()->GetSsaNum());
        }
    }

    return nullptr;
}

bool ScalarEvolutionContext::TrackedLocalVariesInLoop(unsigned lclNum)
{
    for (Statement* stmt : m_loop->GetHeader()->Statements())
    {
        if (!stmt->IsPhiDefnStmt())
        {
            break;
        }

        if (stmt->GetRootNode()->AsLclVarCommon()->GetLclNum() == lclNum)
        {
            return true;
        }
    }

    return false;
}

Scev* ScalarEvolutionContext::AnalyzeNew(BasicBlock* block, GenTree* tree)
{
    switch (tree->OperGet())
    {
        case GT_CNS_INT:
        case GT_CNS_LNG:
        {
            return NewConstant(tree->TypeGet(), tree->AsIntConCommon()->IntegralValue());
        }
        case GT_LCL_VAR:
        {
            if (!tree->AsLclVarCommon()->HasSsaName())
            {
                return nullptr;
            }

            assert(m_comp->lvaInSsa(tree->AsLclVarCommon()->GetLclNum()));
            LclVarDsc* dsc = m_comp->lvaGetDesc(tree->AsLclVarCommon());
            LclSsaVarDsc* ssaDsc = dsc->GetPerSsaData(tree->AsLclVarCommon()->GetSsaNum());

            if ((ssaDsc->GetBlock() == nullptr) || !m_loop->ContainsBlock(ssaDsc->GetBlock()))
            {
                // Invariant local
                assert(!TrackedLocalVariesInLoop(tree->AsLclVarCommon()->GetLclNum()));
                return NewLocal(tree->AsLclVarCommon()->GetLclNum(), tree->AsLclVarCommon()->GetSsaNum());
            }

            return Analyze(ssaDsc->GetBlock(), ssaDsc->GetDefNode());
        }
        case GT_STORE_LCL_VAR:
        {
            GenTreeLclVarCommon* store = tree->AsLclVarCommon();
            GenTree*             data  = store->Data();
            if (!data->OperIs(GT_PHI))
            {
                return Analyze(block, data);
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

            LclVarDsc* dsc = m_comp->lvaGetDesc(store);

            assert(enterSsa->GetLclNum() == store->GetLclNum());
            LclSsaVarDsc* enterSsaDsc = dsc->GetPerSsaData(enterSsa->GetSsaNum());

            assert((enterSsaDsc->GetBlock() == nullptr) || !m_loop->ContainsBlock(enterSsaDsc->GetBlock()));

            // TODO: Represent initial value? E.g. "for (; arg < 10; arg++)" => <L, InitVal(arg), 1>?
            // Also zeroed locals.
            if (enterSsaDsc->GetDefNode() == nullptr)
            {
                return nullptr;
            }

            Scev* enterScev = Analyze(enterSsaDsc->GetBlock(), enterSsaDsc->GetDefNode());

            if (enterScev == nullptr)
            {
                return nullptr;
            }

            BasicBlock*          stepDefBlock;
            GenTreeLclVarCommon* stepDef = GetSsaDef(backedgeSsa, &stepDefBlock);
            assert(stepDef != nullptr); // This should always exist since it comes from a backedge.

            GenTree* stepDefData = stepDef->Data();

            Scev* simpleAddRec = CreateSimpleAddRec(store, enterScev, stepDefBlock, stepDefData);

            if (simpleAddRec != nullptr)
            {
                return simpleAddRec;
            }

            ScevConstant* symbolicAddRec = NewConstant(TYP_INT, 0xdeadbeef);
            m_cyclicCache.Emplace(store, symbolicAddRec);

            Scev* result;
            if (m_usingCyclicCache)
            {
                result = Analyze(stepDefBlock, stepDefData);
            }
            else
            {
                m_usingCyclicCache = true;

                result = Analyze(stepDefBlock, stepDefData);
                m_usingCyclicCache = false;
                m_cyclicCache.RemoveAll();
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

            Scev* op = Analyze(block, cast->CastOp());
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
            Scev* op1 = Analyze(block, tree->gtGetOp1());
            if (op1 == nullptr)
                return nullptr;

            Scev* op2 = Analyze(block, tree->gtGetOp2());
            if (op2 == nullptr)
                return nullptr;

            ScevOper oper;
            switch (tree->OperGet())
            {
            case GT_ADD: oper = ScevOper::Add; break;
            case GT_MUL: oper = ScevOper::Mul; break;
            case GT_LSH: oper = ScevOper::Lsh; break;
            default: unreached();
            }

            return NewBinop(oper, op1, op2);
        }
        case GT_COMMA:
        {
            return Analyze(block, tree->gtGetOp2());
        }
        case GT_ARR_ADDR:
        {
            return Analyze(block, tree->AsArrAddr()->Addr());
        }
        default:
            return nullptr;
    }
}

Scev* ScalarEvolutionContext::CreateSimpleAddRec(GenTreeLclVarCommon* headerStore, Scev* enterScev, BasicBlock* stepDefBlock, GenTree* stepDefData)
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

    return NewAddRec(m_loop, enterScev, stepScev);
}

//------------------------------------------------------------------------
// MakeAddRecFromRecursiveScev: Given a SCEV and a recursive SCEV that the first one may contain,
// create a non-recursive add-rec from it.
//
// Parameters:
//   scev - The scev
//   recursiveScev - A symbolic node whose appearence represents an appearence of "scev"
//
// Returns:
//   A non-recursive addrec
//
// Remarks:
//   Currently only handles simple binary addition
//
Scev* ScalarEvolutionContext::MakeAddRecFromRecursiveScev(Scev* startScev, Scev* scev, Scev* recursiveScev)
{
    if (!scev->OperIs(ScevOper::Add))
    {
        return nullptr;
    }

    ScevBinop* add = (ScevBinop*)scev;
    if ((add->Op1 == recursiveScev) && (add->Op2 != recursiveScev) && add->Op2->OperIs(ScevOper::Constant, ScevOper::AddRec))
    {
        return NewAddRec(m_loop, startScev, add->Op2);
    }

    if ((add->Op2 == recursiveScev) && add->Op1->OperIs(ScevOper::Constant, ScevOper::AddRec))
    {
        return NewAddRec(m_loop, startScev, add->Op1);
    }

    return nullptr;
}

Scev* ScalarEvolutionContext::Analyze(BasicBlock* block, GenTree* tree)
{
    Scev* result;
    if (!m_cache.Lookup(tree, &result) && (!m_usingCyclicCache || !m_cyclicCache.Lookup(tree, &result)))
    {
        result = AnalyzeNew(block, tree);

        if (m_usingCyclicCache)
            m_cyclicCache.Set(tree, result, ScalarEvolutionMap::Overwrite);
        else
            m_cache.Set(tree, result);
    }

    return result;
}

template<typename T>
static T FoldArith(ScevOper oper, T op1, T op2)
{
    switch (oper)
    {
        case ScevOper::Add: return op1 + op2;
        case ScevOper::Mul: return op1 * op2;
        case ScevOper::Lsh: return op1 << op2;
        default: unreached();
    }
}

Scev* ScalarEvolutionContext::Fold(Scev* scev)
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

            Scev* op1 = Fold(unop->Op1);

            if (unop->Type == op1->Type)
            {
                return op1;
            }

            assert((unop->Type == TYP_LONG) && (op1->Type == TYP_INT));

            if (op1->OperIs(ScevOper::Constant))
            {
                ScevConstant* cns = (ScevConstant*)op1;
                return NewConstant(unop->Type, unop->OperIs(ScevOper::ZeroExtend) ? (uint64_t)(int32_t)cns->Value : (int64_t)(int32_t)cns->Value);
            }

            //if (op1->OperIs(ScevOper::AddRec))
            //{
            //    // TODO: We need to prove the extension can be removed safely...
            //    return op1;
            //}

            return (op1 == unop->Op1) ? unop : NewExtension(unop->Oper, unop->Type, op1);
        }
        case ScevOper::Add:
        case ScevOper::Mul:
        case ScevOper::Lsh:
        {
            ScevBinop* binop = (ScevBinop*)scev;
            Scev* op1 = Fold(binop->Op1);
            Scev* op2 = Fold(binop->Op2);

            if (binop->OperIs(ScevOper::Add, ScevOper::Mul))
            {
                // Normalize addrecs to the left
                if (op2->OperIs(ScevOper::AddRec))
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
                ScevAddRec* addRec = (ScevAddRec*)op1;
                Scev* newStart = Fold(NewBinop(binop->Oper, addRec->Start, op2));
                Scev* newStep = scev->OperIs(ScevOper::Mul, ScevOper::Lsh) ? Fold(NewBinop(binop->Oper, addRec->Step, op2)) : addRec->Step;
                return NewAddRec(addRec->Loop, newStart, newStep);
            }

            if (op1->OperIs(ScevOper::Constant) && op2->OperIs(ScevOper::Constant))
            {
                ScevConstant* cns1 = (ScevConstant*)op1;
                ScevConstant* cns2 = (ScevConstant*)op2;
                int64_t       newValue;
                if (binop->TypeIs(TYP_INT))
                {
                    newValue = FoldArith<int32_t>(binop->Oper, static_cast<int32_t>(cns1->Value), static_cast<int32_t>(cns2->Value));
                }
                else
                {
                    assert(binop->TypeIs(TYP_LONG));
                    newValue = FoldArith<int64_t>(binop->Oper, cns1->Value, cns2->Value);
                }

                return NewConstant(binop->Type, newValue);
            }

            return (op1 == binop->Op1) && (op2 == binop->Op2) ? binop : NewBinop(binop->Oper, op1, op2);
        }
        case ScevOper::AddRec:
        {
            ScevAddRec* addRec = (ScevAddRec*)scev;
            Scev* start = Fold(addRec->Start);
            Scev* step = Fold(addRec->Step);
            return (start == addRec->Start) && (step == addRec->Step) ? addRec : NewAddRec(addRec->Loop, start, step);
        }
        default:
            unreached();
    }
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

    fgDispBasicBlocks(true);

    m_blockToLoop = BlockToNaturalLoopMap::Build(m_loops);
    ScalarEvolutionContext scevContext(this);

    for (FlowGraphNaturalLoop* loop : m_loops->InReversePostOrder())
    {
        JITDUMP("Analyzing scalar evolution in ");
        FlowGraphNaturalLoop::Dump(loop);
        scevContext.ResetForLoop(loop);

        loop->VisitLoopBlocksReversePostOrder([=, &scevContext](BasicBlock* block) {
            DBEXEC(verbose, block->dspBlockHeader(this));

            for (Statement* stmt : block->Statements())
            {
                JITDUMP("\n");
                DISPSTMT(stmt);

                for (GenTree* node : stmt->TreeList())
                {
                    Scev* scev = scevContext.Analyze(block, node);
                    if (scev != nullptr)
                    {
                        JITDUMP("[%06u] => ", dspTreeID(node));
                        DumpScev(scev);
                        Scev* folded = scevContext.Fold(scev);
                        JITDUMP(" => ");
                        DumpScev(folded);
                        JITDUMP("\n");
                    }
                }
            }

            return BasicBlockVisit::Continue;
        });
    }

    return PhaseStatus::MODIFIED_NOTHING;
}
