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
    Compiler*             m_comp;
    FlowGraphNaturalLoop* m_loop = nullptr;
    ScalarEvolutionMap    m_cache;

    Scev* AnalyzeNew(BasicBlock* block, GenTree* tree);
    Scev* CreateSimpleAddRec(GenTreeLclVarCommon* headerStore,
                             Scev*                start,
                             BasicBlock*          stepDefBlock,
                             GenTree*             stepDefData);
    Scev* CreateSimpleInvariantScev(GenTree* tree);
    Scev* CreateScevForConstant(GenTreeIntConCommon* tree);
    bool TrackedLocalVariesInLoop(unsigned lclNum);

public:
    ScalarEvolutionContext(Compiler* comp) : m_comp(comp), m_cache(comp->getAllocator(CMK_LoopScalarEvolution))
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
        var_types  type           = genActualType(m_comp->lvaGetDesc(lclNum));
        ScevLocal* invariantLocal = new (m_comp, CMK_LoopScalarEvolution) ScevLocal(type, lclNum, ssaNum);
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

Scev* ScalarEvolutionContext::CreateScevForConstant(GenTreeIntConCommon* tree)
{
    if (tree->IsIconHandle() || !tree->TypeIs(TYP_INT, TYP_LONG))
    {
        return nullptr;
    }

    return NewConstant(tree->TypeGet(), tree->AsIntConCommon()->IntegralValue());
}

Scev* ScalarEvolutionContext::AnalyzeNew(BasicBlock* block, GenTree* tree)
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
                // Invariant local
                GenTreeLclVarCommon* def = ssaDsc->GetDefNode();
                if ((def != nullptr) && def->Data()->OperIs(GT_CNS_INT, GT_CNS_LNG))
                {
                    // For constant definitions from outside the loop we prefer to inline the constant.
                    // TODO: Maybe we shouldn't but should just do it when we dump the scev?

                    return CreateScevForConstant(def->Data()->AsIntConCommon());
                }

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

            Scev* enterScev = Analyze(block, enterSsa);

            if (enterScev == nullptr)
            {
                return nullptr;
            }

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

Scev* ScalarEvolutionContext::CreateSimpleAddRec(GenTreeLclVarCommon* headerStore,
                                                 Scev*                enterScev,
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

    return NewAddRec(m_loop, enterScev, stepScev);
}

Scev* ScalarEvolutionContext::Analyze(BasicBlock* block, GenTree* tree)
{
    Scev* result;
    if (!m_cache.Lookup(tree, &result))
    {
        result = AnalyzeNew(block, tree);
        m_cache.Set(tree, result);
    }

    return result;
}

template <typename T>
static T FoldArith(ScevOper oper, T op1, T op2)
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
                return NewConstant(unop->Type, unop->OperIs(ScevOper::ZeroExtend) ? (uint64_t)(int32_t)cns->Value
                                                                                  : (int64_t)(int32_t)cns->Value);
            }

            // Folding these requires some proof that it is ok.
            // if (op1->OperIs(ScevOper::AddRec))
            //{
            //    return op1;
            //}

            return (op1 == unop->Op1) ? unop : NewExtension(unop->Oper, unop->Type, op1);
        }
        case ScevOper::Add:
        case ScevOper::Mul:
        case ScevOper::Lsh:
        {
            ScevBinop* binop = (ScevBinop*)scev;
            Scev*      op1   = Fold(binop->Op1);
            Scev*      op2   = Fold(binop->Op2);

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
                ScevAddRec* addRec   = (ScevAddRec*)op1;
                Scev*       newStart = Fold(NewBinop(binop->Oper, addRec->Start, op2));
                Scev*       newStep  = scev->OperIs(ScevOper::Mul, ScevOper::Lsh)
                                    ? Fold(NewBinop(binop->Oper, addRec->Step, op2))
                                    : addRec->Step;
                return NewAddRec(addRec->Loop, newStart, newStep);
            }

            if (op1->OperIs(ScevOper::Constant) && op2->OperIs(ScevOper::Constant))
            {
                ScevConstant* cns1 = (ScevConstant*)op1;
                ScevConstant* cns2 = (ScevConstant*)op2;
                int64_t       newValue;
                if (binop->TypeIs(TYP_INT))
                {
                    newValue = FoldArith<int32_t>(binop->Oper, static_cast<int32_t>(cns1->Value),
                                                  static_cast<int32_t>(cns2->Value));
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
            Scev*       start  = Fold(addRec->Start);
            Scev*       step   = Fold(addRec->Step);
            return (start == addRec->Start) && (step == addRec->Step) ? addRec : NewAddRec(addRec->Loop, start, step);
        }
        default:
            unreached();
    }
}

bool Compiler::optCanSinkWidenedIV(unsigned lclNum, FlowGraphNaturalLoop* loop)
{
    LclVarDsc* dsc = lvaGetDesc(lclNum);

    BasicBlockVisit result = loop->VisitAllExitBlocks([=](BasicBlock* exit) {
        if (!VarSetOps::IsMember(this, exit->bbLiveIn, dsc->lvVarIndex))
        {
            JITDUMP("  Exit " FMT_BB " does not need a sink; V%02u is not live-in\n", exit->bbNum, lclNum);
            return BasicBlockVisit::Continue;
        }

        for (FlowEdge* predEdge = BlockPredsWithEH(exit); predEdge != nullptr; predEdge = predEdge->getNextPredEdge())
        {
            if (!loop->ContainsBlock(predEdge->getSourceBlock()))
            {
                JITDUMP("  Cannot safely sink widened version of V%02u into exit " FMT_BB " of " FMT_LP
                        "; it has a non-loop pred " FMT_BB "\n",
                        lclNum, exit->bbNum, loop->GetIndex(), predEdge->getSourceBlock()->bbNum);
                return BasicBlockVisit::Abort;
            }
        }

        JITDUMP("  V%02u is live into exit " FMT_BB "; will sink the widened value\n", lclNum, exit->bbNum);
        return BasicBlockVisit::Continue;
    });

    return result == BasicBlockVisit::Continue;
}

bool Compiler::optIsIVWideningProfitable(unsigned lclNum, ScevAddRec* addRec, FlowGraphNaturalLoop* loop)
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
            if (node->OperIs(GT_CAST))
            {
                GenTreeCast* cast = node->AsCast();
                if ((cast->gtCastType == TYP_LONG) && cast->IsUnsigned())
                {
                    GenTree* op = cast->CastOp();
                    if (op->OperIs(GT_LCL_VAR) && (op->AsLclVarCommon()->GetLclNum() == m_lclNum))
                    {
                        NumExtensions++;
                        return WALK_SKIP_SUBTREES;
                    }
                }
            }

            return WALK_CONTINUE;
        }
    };

    const weight_t ExtensionCost = 2;
    const int      ExtensionSize = 3;

    CountZeroExtensionsVisitor visitor(this, lclNum);
    weight_t                   savedCost = 0;
    int                        savedSize = 0;

    loop->VisitLoopBlocks([&](BasicBlock* block) {
        visitor.NumExtensions = 0;

        for (Statement* stmt : block->NonPhiStatements())
        {
            visitor.WalkTree(stmt->GetRootNodePointer(), nullptr);
        }

        savedSize += (int)visitor.NumExtensions * ExtensionSize;
        savedCost += visitor.NumExtensions * block->getBBWeight(this) * ExtensionCost;
        return BasicBlockVisit::Continue;
    });

    if (!addRec->Start->OperIs(ScevOper::Constant))
    {
        // Need to insert a move from the narrow local in the preheader.
        savedSize -= ExtensionSize;
        savedCost -= loop->EntryEdge(0)->getSourceBlock()->getBBWeight(this) * ExtensionCost;
    }
    else
    {
        // If this is a constant then we are likely going to save the cost of
        // initializing the narrow local which will balance out initializing
        // the widened local.
    }

    // Now account for the cost of sinks.
    LclVarDsc* dsc = lvaGetDesc(lclNum);
    loop->VisitAllExitBlocks([&](BasicBlock* exit) {
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

bool Compiler::optSinkWidenedIV(unsigned lclNum, unsigned newLclNum, FlowGraphNaturalLoop* loop)
{
    bool       anySunk = false;
    LclVarDsc* dsc     = lvaGetDesc(lclNum);
    loop->VisitAllExitBlocks([=, &anySunk](BasicBlock* exit) {
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

void Compiler::optReplaceWidenedIV(unsigned lclNum, unsigned newLclNum, Statement* stmt)
{
    struct ReplaceVisitor : GenTreeVisitor<ReplaceVisitor>
    {
    private:
        unsigned m_lclNum;
        unsigned m_newLclNum;

    public:
        bool MadeChanges = false;

        enum
        {
            DoPreOrder = true,
        };

        ReplaceVisitor(Compiler* comp, unsigned lclNum, unsigned newLclNum)
            : GenTreeVisitor(comp), m_lclNum(lclNum), m_newLclNum(newLclNum)
        {
        }

        fgWalkResult PreOrderVisit(GenTree** use, GenTree* user)
        {
            GenTree* node = *use;
            if (node->OperIs(GT_CAST))
            {
                GenTreeCast* cast = node->AsCast();
                if ((cast->gtCastType == TYP_LONG) && cast->IsUnsigned())
                {
                    GenTree* op = cast->CastOp();
                    if (op->OperIs(GT_LCL_VAR) && (op->AsLclVarCommon()->GetLclNum() == m_lclNum))
                    {
                        *use        = m_compiler->gtNewLclvNode(m_newLclNum, TYP_LONG);
                        MadeChanges = true;
                        return fgWalkResult::WALK_SKIP_SUBTREES;
                    }
                }
            }
            else if (node->OperIs(GT_LCL_VAR, GT_LCL_FLD, GT_STORE_LCL_VAR, GT_STORE_LCL_FLD) &&
                     (node->AsLclVarCommon()->GetLclNum() == m_lclNum))
            {
                switch (node->OperGet())
                {
                    case GT_LCL_VAR:
                        node->AsLclVarCommon()->SetLclNum(m_newLclNum);
                        // No cast needed -- the backend allows TYP_INT uses of TYP_LONG locals.
                        break;
                    case GT_LCL_FLD:
                    case GT_STORE_LCL_FLD: // TODO: Do we need to skip widening if we have one of these?
                        node->AsLclFld()->SetLclNum(m_newLclNum);
                        m_compiler->lvaSetVarDoNotEnregister(m_newLclNum DEBUGARG(DoNotEnregisterReason::LocalField));
                        break;
                    case GT_STORE_LCL_VAR:
                        node->AsLclVarCommon()->SetLclNum(m_newLclNum);
                        node->AsLclVarCommon()->gtType = TYP_LONG;
                        node->AsLclVarCommon()->Data() =
                            m_compiler->gtNewCastNode(TYP_LONG, node->AsLclVarCommon()->Data(), true, TYP_LONG);
                        break;
                }

                MadeChanges = true;
            }

            return fgWalkResult::WALK_CONTINUE;
        }
    };

    ReplaceVisitor visitor(this, lclNum, newLclNum);
    visitor.WalkTree(stmt->GetRootNodePointer(), nullptr);
    if (visitor.MadeChanges)
    {
        compCurStmt = stmt;
        // stmt->SetRootNode(fgMorphTree(stmt->GetRootNode()));
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
// optInductionVariables: Try and optimize induction variables in the method.
//
// Returns:
//   PhaseStatus indicating if anything changed.
//
PhaseStatus Compiler::optInductionVariables()
{
    JITDUMP("*************** In optInductionVariables()\n");

    if (JitConfig.JitEnableInductionVariableOpts() == 0)
    {
        return PhaseStatus::MODIFIED_NOTHING;
    }

#ifdef DEBUG
    static ConfigMethodRange s_range;
    s_range.EnsureInit(JitConfig.JitEnableInductionVariableOptsRange());

    if (!s_range.Contains(info.compMethodHash()))
    {
        return PhaseStatus::MODIFIED_NOTHING;
    }
#endif

    m_blockToLoop = BlockToNaturalLoopMap::Build(m_loops);
    ScalarEvolutionContext scevContext(this);

    for (FlowGraphNaturalLoop* loop : m_loops->InReversePostOrder())
    {
        JITDUMP("Analyzing scalar evolution in ");
        DBEXEC(verbose, FlowGraphNaturalLoop::Dump(loop));
        scevContext.ResetForLoop(loop);

        loop->VisitLoopBlocksReversePostOrder([=, &scevContext](BasicBlock* block) {
            DBEXEC(verbose, block->dspBlockHeader(this));
            JITDUMP("\n");

            for (Statement* stmt : block->Statements())
            {
                DISPSTMT(stmt);
                JITDUMP("\n");

                for (GenTree* node : stmt->TreeList())
                {
                    Scev* scev = scevContext.Analyze(block, node);
                    if (scev != nullptr)
                    {
                        JITDUMP("[%06u] => ", dspTreeID(node));
                        DBEXEC(verbose, DumpScev(scev));
                        JITDUMP("\n  => ", dspTreeID(node));
                        Scev* folded = scevContext.Fold(scev);
                        DBEXEC(verbose, DumpScev(folded));
                        JITDUMP("\n");
                    }
                }

                JITDUMP("\n");
            }

            return BasicBlockVisit::Continue;
        });
    }

    bool changed = false;

#ifdef TARGET_64BIT
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

            GenTreeLclVarCommon* lcl = stmt->GetRootNode()->AsLclVarCommon();
            if (genActualType(lcl) != TYP_INT)
            {
                JITDUMP("  Type is %s, no widening to be done\n", varTypeName(genActualType(lcl)));
                continue;
            }

            Scev* scev = scevContext.Analyze(loop->GetHeader(), stmt->GetRootNode());
            if (scev == nullptr)
            {
                JITDUMP("  Could not analyze header PHI\n");
                continue;
            }

            scev = scevContext.Fold(scev);
            JITDUMP("  => ");
            DBEXEC(verbose, DumpScev(scev));
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

            if (!optIsIVWideningProfitable(lcl->GetLclNum(), addRec, loop))
            {
                continue;
            }

            changed            = true;
            unsigned newLclNum = lvaGrabTemp(false DEBUGARG("Widened primary induction variable"));
            JITDUMP("  Replacing V%02u with a widened version V%02u\n", lcl->GetLclNum(), newLclNum);

            GenTree* initVal;
            if (addRec->Start->OperIs(ScevOper::Constant))
            {
                ScevConstant* cns = (ScevConstant*)addRec->Start;
                initVal           = gtNewIconNode((int64_t)(uint32_t)(((ScevConstant*)addRec->Start)->Value), TYP_LONG);
            }
            else
            {
                LclVarDsc* lclDsc = lvaGetDesc(lcl);
                initVal =
                    gtNewCastNode(TYP_LONG, gtNewLclvNode(lcl->GetLclNum(),
                                                          lclDsc->lvNormalizeOnLoad() ? lclDsc->TypeGet() : TYP_INT),
                                  true, TYP_LONG);
            }

            JITDUMP("Adding initialization of new widened local to preheader:\n");
            GenTree*    widenStore = gtNewTempStore(newLclNum, initVal);
            BasicBlock* preheader  = loop->EntryEdge(0)->getSourceBlock();
            Statement*  initStmt   = fgNewStmtFromTree(widenStore);
            fgInsertStmtAtEnd(preheader, initStmt);
            DISPSTMT(initStmt);
            JITDUMP("\n");

            loop->VisitLoopBlocks([=](BasicBlock* block) {

                compCurBB = block;
                for (Statement* stmt : block->NonPhiStatements())
                {
                    JITDUMP("Replacing V%02u -> V%02u in [%06u]\n", lcl->GetLclNum(), newLclNum,
                            dspTreeID(stmt->GetRootNode()));
                    DISPSTMT(stmt);
                    JITDUMP("\n");
                    optReplaceWidenedIV(lcl->GetLclNum(), newLclNum, stmt);
                }

                return BasicBlockVisit::Continue;
            });

            changed |= optSinkWidenedIV(lcl->GetLclNum(), newLclNum, loop);
        }
    }
#endif

    if (changed)
    {
        fgSsaBuild();
    }

    return changed ? PhaseStatus::MODIFIED_EVERYTHING : PhaseStatus::MODIFIED_NOTHING;
}
