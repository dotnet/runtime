// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "jitpch.h"
#include "autovectorizer.h"

AutoVectorizer::AutoVectorizer(Compiler* compiler)
    : m_compiler(compiler)
{
}

PhaseStatus AutoVectorizer::RunAnalyze()
{
    if (!IsEnabled())
    {
        return PhaseStatus::MODIFIED_NOTHING;
    }

    if (!IsSupportedCompilation())
    {
        return PhaseStatus::MODIFIED_NOTHING;
    }

    if (m_compiler->m_loops == nullptr)
    {
        Dump("AutoVec: no loop table\n");
        return PhaseStatus::MODIFIED_NOTHING;
    }

    unsigned candidateCount = 0;

    for (FlowGraphNaturalLoop* const loop : m_compiler->m_loops->InPostOrder())
    {
        LoopVectorizationPlan plan;
        if (TryCreateLoopPlan(loop, &plan))
        {
            candidateCount++;
            Dump("AutoVec: accepted loop " FMT_LP ", IV V%02u, VF=%u, vectorSize=%u\n", loop->GetIndex(),
                 plan.InductionVar, plan.VectorizationFactor, plan.VectorSizeBytes);
        }
    }

    Dump("AutoVec: analysis found %u candidate loop%s\n", candidateCount, (candidateCount == 1) ? "" : "s");
    return PhaseStatus::MODIFIED_NOTHING;
}

PhaseStatus AutoVectorizer::RunRewrite()
{
    Dump("AutoVec: rewrite phase enabled\n");
    return PhaseStatus::MODIFIED_NOTHING;
}

bool AutoVectorizer::IsEnabled() const
{
    return JitConfig.JitAutoVectorization() != 0;
}

bool AutoVectorizer::IsSupportedCompilation() const
{
    if (m_compiler->opts.MinOpts() || m_compiler->opts.compDbgCode)
    {
        Dump("AutoVec: disabled for MinOpts/debuggable code\n");
        return false;
    }

#if defined(FEATURE_SIMD) && (defined(TARGET_XARCH) || defined(TARGET_ARM64))
    const unsigned vectorSizeBytes = m_compiler->getMaxVectorByteLength();
    if (vectorSizeBytes < 16)
    {
        Dump("AutoVec: target vector size %u is too small\n", vectorSizeBytes);
        return false;
    }

    return true;
#else
    Dump("AutoVec: unsupported target\n");
    return false;
#endif
}

bool AutoVectorizer::TryCreateLoopPlan(FlowGraphNaturalLoop* loop, LoopVectorizationPlan* plan)
{
    Dump("AutoVec: considering loop " FMT_LP "\n", loop->GetIndex());

    if (loop->EntryEdges().size() != 1)
    {
        Reject(loop, "loop does not have exactly one entry");
        return false;
    }

    if (loop->BackEdges().size() != 1)
    {
        Reject(loop, "loop does not have exactly one backedge");
        return false;
    }

    if (loop->ExitEdges().size() != 1)
    {
        Reject(loop, "loop does not have exactly one normal exit");
        return false;
    }

    if (loop->GetChild() != nullptr)
    {
        Reject(loop, "nested loop");
        return false;
    }

    BasicBlock* const preheader = loop->EntryEdge(0)->getSourceBlock();
    BasicBlock* const latch     = loop->BackEdge(0)->getSourceBlock();
    BasicBlock* const exit      = loop->ExitEdge(0)->getDestinationBlock();

    if ((preheader == nullptr) || !preheader->KindIs(BBJ_ALWAYS) || !preheader->TargetIs(loop->GetHeader()))
    {
        Reject(loop, "missing canonical preheader");
        return false;
    }

    if (preheader->hasTryIndex() || preheader->hasHndIndex() || exit->hasTryIndex() || exit->hasHndIndex())
    {
        Reject(loop, "EH region near loop");
        return false;
    }

    bool hasEH = false;
    loop->VisitLoopBlocks([&](BasicBlock* block) {
        if (block->hasTryIndex() || block->hasHndIndex())
        {
            hasEH = true;
            return BasicBlockVisit::Abort;
        }

        return BasicBlockVisit::Continue;
    });

    if (hasEH)
    {
        Reject(loop, "EH region in loop");
        return false;
    }

    NaturalLoopIterInfo iterInfo;
    if (!loop->AnalyzeIteration(&iterInfo))
    {
        Reject(loop, "no canonical counted iteration");
        return false;
    }

    if (!iterInfo.HasConstInit || (iterInfo.ConstInitValue != 0))
    {
        Reject(loop, "IV init is not zero");
        return false;
    }

    if (!iterInfo.IsIncreasingLoop() || (iterInfo.IterOper() != GT_ADD) || (iterInfo.IterConst() != 1))
    {
        Reject(loop, "IV is not incremented by one");
        return false;
    }

    const genTreeOps testOper = iterInfo.TestOper();
    if (!GenTree::StaticOperIs(testOper, GT_LT, GT_LE))
    {
        Reject(loop, "unsupported loop test");
        return false;
    }

    if (!iterInfo.HasArrayLengthLimit && !iterInfo.HasInvariantLocalLimit)
    {
        Reject(loop, "limit is not array length or invariant local");
        return false;
    }

    plan->Loop         = loop;
    plan->Preheader    = preheader;
    plan->Header       = loop->GetHeader();
    plan->Latch        = latch;
    plan->Exit         = exit;
    plan->InductionVar = iterInfo.IterVar;
    plan->End          = iterInfo.Limit();
    plan->IterTree     = iterInfo.IterTree;
    plan->TestTree     = iterInfo.TestTree;
    plan->TestBlock    = iterInfo.TestBlock;
    plan->TestOper     = testOper;
    plan->Step         = 1;
#if defined(FEATURE_SIMD) && (defined(TARGET_XARCH) || defined(TARGET_ARM64))
    plan->VectorSizeBytes     = m_compiler->getMaxVectorByteLength();
    plan->VectorizationFactor = plan->VectorSizeBytes / genTypeSize(TYP_INT);
#endif

    if (plan->VectorizationFactor < 2)
    {
        Reject(loop, "vectorization factor is too small");
        return false;
    }

    Dump("AutoVec: loop " FMT_LP " canonical IV V%02u, init=0, step=1, test=%s\n", loop->GetIndex(),
         iterInfo.IterVar, GenTree::OpName(testOper));
    return TryAnalyzeMemory(plan);
}

void AutoVectorizer::Reject(FlowGraphNaturalLoop* loop, const char* reason) const
{
    Dump("AutoVec: rejected loop " FMT_LP ": %s\n", loop->GetIndex(), reason);
}

bool AutoVectorizer::TryAnalyzeMemory(LoopVectorizationPlan* plan)
{
    bool foundStore = false;
    bool failed     = false;

    plan->Loop->VisitLoopBlocks([&](BasicBlock* block) {
        for (Statement* const stmt : block->Statements())
        {
            GenTree* const root = stmt->GetRootNode();

            if (root == plan->IterTree)
            {
                continue;
            }

            if (root->OperIs(GT_JTRUE) && (root->gtGetOp1() == plan->TestTree))
            {
                continue;
            }

            if (!root->OperIs(GT_STOREIND))
            {
                failed = true;
                Reject(plan->Loop, "unsupported statement in loop body");
                return BasicBlockVisit::Abort;
            }

            if (foundStore)
            {
                failed = true;
                Reject(plan->Loop, "multiple stores");
                return BasicBlockVisit::Abort;
            }

            if ((root->gtFlags & (GTF_CALL | GTF_ORDER_SIDEEFF)) != 0)
            {
                failed = true;
                Reject(plan->Loop, "store statement has unsupported side effects");
                return BasicBlockVisit::Abort;
            }

            if (ContainsOper(root, GT_BOUNDS_CHECK))
            {
                failed = true;
                Reject(plan->Loop, "remaining bounds check");
                return BasicBlockVisit::Abort;
            }

            GenTreeStoreInd* const store = root->AsStoreInd();
            if (!TryAnalyzeArrayAccess(stmt, store, true, plan->InductionVar, &plan->StoreAccess))
            {
                failed = true;
                Reject(plan->Loop, "unsupported store access");
                return BasicBlockVisit::Abort;
            }

            if (!store->Data()->OperIs(GT_ADD))
            {
                failed = true;
                Reject(plan->Loop, "store data is not an add");
                return BasicBlockVisit::Abort;
            }

            GenTreeOp* const add  = store->Data()->AsOp();
            GenTree*         load = nullptr;
            GenTree*         scalar = nullptr;
            bool             scalarIsRhs = true;

            if (add->gtOp1->OperIs(GT_IND))
            {
                load        = add->gtOp1;
                scalar      = add->gtOp2;
                scalarIsRhs = true;
            }
            else if (add->gtOp2->OperIs(GT_IND))
            {
                load        = add->gtOp2;
                scalar      = add->gtOp1;
                scalarIsRhs = false;
            }
            else
            {
                failed = true;
                Reject(plan->Loop, "add does not contain an array load");
                return BasicBlockVisit::Abort;
            }

            if (!TryAnalyzeArrayAccess(stmt, load, false, plan->InductionVar, &plan->LoadAccess))
            {
                failed = true;
                Reject(plan->Loop, "unsupported load access");
                return BasicBlockVisit::Abort;
            }

            if (!TryGetInvariantInt(plan->Loop, plan->InductionVar, scalar))
            {
                failed = true;
                Reject(plan->Loop, "add operand is not loop invariant int");
                return BasicBlockVisit::Abort;
            }

            if ((plan->StoreAccess.ElementType != TYP_INT) || (plan->LoadAccess.ElementType != TYP_INT))
            {
                failed = true;
                Reject(plan->Loop, "element type is not int");
                return BasicBlockVisit::Abort;
            }

            if (plan->StoreAccess.BaseLocalIfKnown != plan->LoadAccess.BaseLocalIfKnown)
            {
                failed = true;
                Reject(plan->Loop, "load/store base mismatch");
                return BasicBlockVisit::Abort;
            }

            if (plan->StoreAccess.IndexOffset != plan->LoadAccess.IndexOffset)
            {
                failed = true;
                Reject(plan->Loop, "possible loop-carried dependence");
                return BasicBlockVisit::Abort;
            }

            if (plan->StoreAccess.IndexOffset != 0)
            {
                failed = true;
                Reject(plan->Loop, "non-zero index offset");
                return BasicBlockVisit::Abort;
            }

            unsigned lengthLcl = BAD_VAR_NUM;
            if (!TryGetArrayLengthLocal(plan->End, &lengthLcl) || (lengthLcl != plan->StoreAccess.BaseLocalIfKnown))
            {
                failed = true;
                Reject(plan->Loop, "loop limit is not the accessed array length");
                return BasicBlockVisit::Abort;
            }

            plan->StoreStmt          = stmt;
            plan->ScalarOperand      = scalar;
            plan->ScalarOper         = GT_ADD;
            plan->ScalarOperandIsRhs = scalarIsRhs;
            foundStore               = true;
        }

        return BasicBlockVisit::Continue;
    });

    if (failed)
    {
        return false;
    }

    if (!foundStore)
    {
        Reject(plan->Loop, "no vectorizable store");
        return false;
    }

    Dump("AutoVec: found int[] store/load base V%02u, offset %d\n", plan->StoreAccess.BaseLocalIfKnown,
         plan->StoreAccess.IndexOffset);
    return true;
}

bool AutoVectorizer::TryAnalyzeArrayAccess(
    Statement* stmt, GenTree* indir, bool isStore, unsigned ivLcl, LoopVectorizationPlan::ScalarAccess* access)
{
    assert(indir->OperIs(GT_IND, GT_STOREIND));

    GenTreeIndir* const indirNode = indir->AsIndir();
    if (indirNode->IsVolatile() || indirNode->Addr()->OperIs(GT_COMMA) || !indirNode->Addr()->OperIs(GT_ARR_ADDR))
    {
        return false;
    }

    if (!TryAnalyzeArrayAddress(indirNode->Addr()->AsArrAddr(), ivLcl, access))
    {
        return false;
    }

    access->StatementRoot = stmt;
    access->Address       = indirNode->Addr();
    access->ElementSize   = genTypeSize(access->ElementType);
    access->IsLoad        = !isStore;
    access->IsStore       = isStore;
    access->IsVolatile    = indirNode->IsVolatile();

    return true;
}

bool AutoVectorizer::TryAnalyzeIndexExpr(GenTree* tree, unsigned ivLcl, int* offset)
{
    if (tree->OperIs(GT_CAST))
    {
        return TryAnalyzeIndexExpr(tree->AsCast()->CastOp(), ivLcl, offset);
    }

    if (tree->OperIs(GT_LCL_VAR) && (tree->AsLclVarCommon()->GetLclNum() == ivLcl))
    {
        return true;
    }

    if (tree->OperIs(GT_ADD))
    {
        GenTree* op1 = tree->AsOp()->gtOp1;
        GenTree* op2 = tree->AsOp()->gtOp2;

        if (op1->IsCnsIntOrI())
        {
            *offset += static_cast<int>(op1->AsIntConCommon()->IconValue());
            return TryAnalyzeIndexExpr(op2, ivLcl, offset);
        }

        if (op2->IsCnsIntOrI())
        {
            *offset += static_cast<int>(op2->AsIntConCommon()->IconValue());
            return TryAnalyzeIndexExpr(op1, ivLcl, offset);
        }
    }

    return false;
}

bool AutoVectorizer::TryAnalyzeArrayAddress(
    GenTreeArrAddr* arrAddr, unsigned ivLcl, LoopVectorizationPlan::ScalarAccess* access)
{
    struct AddressParts
    {
        unsigned ArrayLcl   = BAD_VAR_NUM;
        ssize_t  Offset     = 0;
        ssize_t  IndexScale = 0;
    };

    const unsigned elemSize        = genTypeSize(arrAddr->GetElemType());
    const ssize_t  firstElemOffset = arrAddr->GetFirstElemOffset();
    AddressParts   parts;

    class AddressVisitor
    {
    public:
        AddressVisitor(AutoVectorizer* vectorizer, unsigned ivLcl, unsigned elemSize, AddressParts* parts)
            : m_vectorizer(vectorizer)
            , m_ivLcl(ivLcl)
            , m_elemSize(elemSize)
            , m_parts(parts)
        {
        }

        bool Analyze(GenTree* tree)
        {
            if (tree->OperIs(GT_ARR_ADDR))
            {
                return Analyze(tree->AsArrAddr()->Addr());
            }

            if (tree->OperIs(GT_ADD))
            {
                return Analyze(tree->AsOp()->gtOp1) && Analyze(tree->AsOp()->gtOp2);
            }

            if (tree->IsCnsIntOrI())
            {
                m_parts->Offset += tree->AsIntConCommon()->IconValue();
                return true;
            }

            if (tree->OperIs(GT_LCL_VAR) && varTypeIsGC(tree->TypeGet()))
            {
                if (m_parts->ArrayLcl != BAD_VAR_NUM)
                {
                    return false;
                }

                m_parts->ArrayLcl = tree->AsLclVarCommon()->GetLclNum();
                return true;
            }

            if (tree->OperIs(GT_MUL))
            {
                GenTree* index = tree->AsOp()->gtOp1;
                GenTree* scale = tree->AsOp()->gtOp2;

                if (!scale->IsCnsIntOrI())
                {
                    std::swap(index, scale);
                }

                if (!scale->IsCnsIntOrI() || (scale->AsIntConCommon()->IconValue() != m_elemSize))
                {
                    return false;
                }

                int indexOffset = 0;
                if (!m_vectorizer->TryAnalyzeIndexExpr(index, m_ivLcl, &indexOffset))
                {
                    return false;
                }

                m_parts->IndexScale += m_elemSize;
                m_parts->Offset += static_cast<ssize_t>(indexOffset) * static_cast<ssize_t>(m_elemSize);
                return true;
            }

            if (m_elemSize == 1)
            {
                int indexOffset = 0;
                if (m_vectorizer->TryAnalyzeIndexExpr(tree, m_ivLcl, &indexOffset))
                {
                    m_parts->IndexScale += 1;
                    m_parts->Offset += indexOffset;
                    return true;
                }
            }

            return false;
        }

    private:
        AutoVectorizer* m_vectorizer;
        unsigned        m_ivLcl;
        unsigned        m_elemSize;
        AddressParts*   m_parts;
    };

    AddressVisitor visitor(this, ivLcl, elemSize, &parts);
    if (!visitor.Analyze(arrAddr->Addr()))
    {
        return false;
    }

    if ((parts.ArrayLcl == BAD_VAR_NUM) || (parts.IndexScale != elemSize) || (parts.Offset < firstElemOffset))
    {
        return false;
    }

    const ssize_t offsetBytes = parts.Offset - firstElemOffset;
    if ((offsetBytes % static_cast<ssize_t>(elemSize)) != 0)
    {
        return false;
    }

    access->BaseLocalIfKnown = parts.ArrayLcl;
    access->IndexOffset      = static_cast<int>(offsetBytes / static_cast<ssize_t>(elemSize));
    access->ElementSize      = elemSize;
    access->ElementType      = arrAddr->GetElemType();
    access->IsArray          = true;
    return true;
}

bool AutoVectorizer::TryGetArrayLengthLocal(GenTree* tree, unsigned* lclNum)
{
    if (!tree->OperIs(GT_ARR_LENGTH))
    {
        return false;
    }

    GenTree* const arrRef = tree->AsArrLen()->ArrRef();
    if (!arrRef->OperIs(GT_LCL_VAR))
    {
        return false;
    }

    *lclNum = arrRef->AsLclVarCommon()->GetLclNum();
    return true;
}

bool AutoVectorizer::TryGetInvariantInt(FlowGraphNaturalLoop* loop, unsigned ivLcl, GenTree* tree)
{
    if (!tree->TypeIs(TYP_INT))
    {
        return false;
    }

    if (tree->IsCnsIntOrI())
    {
        return true;
    }

    if (!tree->OperIs(GT_LCL_VAR))
    {
        return false;
    }

    const unsigned lclNum = tree->AsLclVarCommon()->GetLclNum();
    return (lclNum != ivLcl) && !loop->HasDef(lclNum);
}

bool AutoVectorizer::ContainsOper(GenTree* tree, genTreeOps oper)
{
    class Visitor final : public GenTreeVisitor<Visitor>
    {
    public:
        enum
        {
            DoPreOrder = true,
        };

        Visitor(Compiler* compiler, genTreeOps oper)
            : GenTreeVisitor<Visitor>(compiler)
            , m_oper(oper)
        {
        }

        fgWalkResult PreOrderVisit(GenTree** use, GenTree* user)
        {
            if ((*use)->OperIs(m_oper))
            {
                m_found = true;
                return fgWalkResult::WALK_ABORT;
            }

            return fgWalkResult::WALK_CONTINUE;
        }

        bool Found() const
        {
            return m_found;
        }

    private:
        genTreeOps m_oper;
        bool       m_found = false;
    };

    Visitor visitor(m_compiler, oper);
    visitor.WalkTree(&tree, nullptr);
    return visitor.Found();
}

bool AutoVectorizer::ShouldDump() const
{
#ifdef DEBUG
    return m_compiler->verbose ||
           JitConfig.JitAutoVectorizationDump().contains(m_compiler->info.compMethodHnd, m_compiler->info.compClassHnd,
                                                         &m_compiler->info.compMethodInfo->args);
#else
    return false;
#endif
}

void AutoVectorizer::Dump(const char* format, ...) const
{
#ifdef DEBUG
    if (!ShouldDump())
    {
        return;
    }

    va_list args;
    va_start(args, format);
    vprintf(format, args);
    va_end(args);
#endif
}

PhaseStatus Compiler::optAutoVectorizeAnalyze()
{
    AutoVectorizer autoVectorizer(this);
    return autoVectorizer.RunAnalyze();
}

PhaseStatus Compiler::optAutoVectorizeRewrite()
{
    AutoVectorizer autoVectorizer(this);
    return autoVectorizer.RunRewrite();
}
