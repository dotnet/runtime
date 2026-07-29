// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "jitpch.h"
#ifdef _MSC_VER
#pragma hdrstop
#endif

// The IndirectCallTransformer transforms indirect calls that involve fat function
// pointers, guarded devirtualization candidates, or runtime lookup with dynamic dictionary expansion.
// These transformations introduce control flow and so can't easily be done in the importer.
//
// A fat function pointer is a pointer with the second least significant bit
// (aka FAT_POINTER_MASK) set. If the bit is set, the pointer (after clearing the bit)
// actually points to a tuple <method pointer, instantiation argument> where
// instantiationArgument is a hidden first argument required by method pointer.
//
// Fat pointers are used in NativeAOT as a replacement for instantiating stubs,
// because NativeAOT can't generate stubs in runtime.
//
// The JIT is responsible for emitting code to check the bit at runtime, branching
// to one of two call sites.
//
// When the bit is not set, the code should execute the original indirect call.
//
// When the bit is set, the code should mask off the bit, use the resulting pointer
// to load the real target address and the extra argument, and then call indirect
// via the target, passing the extra argument.
//
// before:
//   current block
//   {
//     previous statements
//     transforming statement
//     {
//       call with GTF_CALL_M_FAT_POINTER_CHECK flag set in function ptr
//     }
//     subsequent statements
//   }
//
// after:
//   current block
//   {
//     previous statements
//   } BBJ_ALWAYS check block
//   check block
//   {
//     jump to else if function ptr has the FAT_POINTER_MASK bit set.
//   } BBJ_COND then block, else block
//   then block
//   {
//     original statement
//   } BBJ_ALWAYS remainder block
//   else block
//   {
//     clear FAT_POINTER_MASK bit
//     load actual function pointer
//     load instantiation argument
//     create newArgList = (instantiation argument, original argList)
//     call (actual function pointer, newArgList)
//   } BBJ_ALWAYS remainder block
//   remainder block
//   {
//     subsequent statements
//   }
//
class IndirectCallTransformer
{
public:
    IndirectCallTransformer(Compiler* compiler)
        : m_compiler(compiler)
    {
    }

    //------------------------------------------------------------------------
    // Run: run transformation for each block.
    //
    // Returns:
    //   Count of calls transformed.
    int Run()
    {
        int count = 0;

        for (BasicBlock* const block : m_compiler->Blocks())
        {
            count += TransformBlock(block);
        }

        return count;
    }

private:
    //------------------------------------------------------------------------
    // TransformBlock: look through statements and transform statements with
    //   particular indirect calls
    //
    // Returns:
    //   Count of calls transformed.
    //
    int TransformBlock(BasicBlock* block)
    {
        int count = 0;

        for (Statement* const stmt : block->Statements())
        {
            if (m_compiler->doesMethodHaveFatPointer() && ContainsFatCalli(stmt))
            {
                FatPointerCallTransformer transformer(m_compiler, block, stmt);
                transformer.Run();
                count++;
            }
            else if (m_compiler->doesMethodHaveGuardedDevirtualization() &&
                     ContainsGuardedDevirtualizationCandidate(stmt))
            {
                GuardedDevirtualizationTransformer transformer(m_compiler, block, stmt);
                transformer.Run();
                count++;
            }
        }

        return count;
    }

    //------------------------------------------------------------------------
    // ContainsFatCalli: check does this statement contain fat pointer call.
    //
    // Checks fatPointerCandidate in form of call() or lclVar = call().
    //
    // Return Value:
    //    true if contains, false otherwise.
    //
    bool ContainsFatCalli(Statement* stmt)
    {
        GenTree* fatPointerCandidate = stmt->GetRootNode();
        if (fatPointerCandidate->OperIs(GT_STORE_LCL_VAR))
        {
            fatPointerCandidate = fatPointerCandidate->AsLclVar()->Data();
        }
        return fatPointerCandidate->IsCall() && fatPointerCandidate->AsCall()->IsFatPointerCandidate();
    }

    //------------------------------------------------------------------------
    // ContainsGuardedDevirtualizationCandidate: check does this statement contain a virtual
    // call that we'd like to guardedly devirtualize?
    //
    // Return Value:
    //    true if contains, false otherwise.
    //
    // Notes:
    //    calls are hoisted to top level ... (we hope)
    bool ContainsGuardedDevirtualizationCandidate(Statement* stmt)
    {
        GenTree* candidate = stmt->GetRootNode();
        return candidate->IsCall() && candidate->AsCall()->IsGuardedDevirtualizationCandidate();
    }

    class Transformer
    {
    public:
        Transformer(Compiler* compiler, BasicBlock* block, Statement* stmt)
            : m_compiler(compiler)
            , m_currBlock(block)
            , m_stmt(stmt)
        {
        }

        //------------------------------------------------------------------------
        // Run: transform the statement as described above.
        //
        virtual void Run()
        {
            Transform();
        }

        void Transform()
        {
            JITDUMP("*** %s: transforming " FMT_STMT "\n", Name(), m_stmt->GetID());
            FixupRetExpr();
            ClearFlag();
            CreateRemainder();
            assert(GetChecksCount() > 0);
            for (uint8_t i = 0; i < GetChecksCount(); i++)
            {
                CreateCheck(i);
                CreateThen(i);
            }
            CreateElse();
            RemoveOldStatement();
            SetWeights();
            ChainFlow();
        }

    protected:
        virtual const char*  Name()                       = 0;
        virtual void         ClearFlag()                  = 0;
        virtual GenTreeCall* GetCall(Statement* callStmt) = 0;
        virtual void         FixupRetExpr()               = 0;

        //------------------------------------------------------------------------
        // SplitCall: spill all side effect uses of the call and the useToSpill into temps.
        //
        // Parameters
        //   block - the block to insert the spill statements into.
        //   useToSpill - the use of the call to spill into a temp.
        //
        void SplitCall(BasicBlock* block, GenTree** useToSpill)
        {
            // Find last arg with a side effect. All args with any effect
            // before that will need to be spilled.
            GenTree** lastSideEffectUse = nullptr;
            for (GenTree** use : m_origCall->UseEdges())
            {
                if (((*use)->gtFlags & GTF_SIDE_EFFECT) != 0)
                {
                    lastSideEffectUse = use;
                }
            }

            if (lastSideEffectUse != nullptr)
            {
                for (GenTree** use : m_origCall->UseEdges())
                {
                    GenTree* node = *use;
                    if (((node->gtFlags & GTF_ALL_EFFECT) != 0) || m_compiler->gtHasLocalsWithAddrOp(node))
                    {
                        SpillUseToTemp(block, use);
                    }

                    if (use == lastSideEffectUse)
                    {
                        break;
                    }
                }
            }

            // We spill the use if it is complex, regardless of side effects.
            if (!(*useToSpill)->IsLocal())
            {
                SpillUseToTemp(block, useToSpill);
            }
        }

        //------------------------------------------------------------------------
        // SpillUseToTemp: spill an argument into a temp.
        //
        // Parameters
        //   arg - The arg to create a temp and local store for.
        //
        void SpillUseToTemp(BasicBlock* block, GenTree** use)
        {
            unsigned       tmpNum = m_compiler->lvaGrabTemp(true DEBUGARG("indirect call transform spill temp"));
            GenTree* const node   = *use;
            GenTree*       store  = m_compiler->gtNewTempStore(tmpNum, node);

            if (node->TypeIs(TYP_REF))
            {
                bool                 isExact   = false;
                bool                 isNonNull = false;
                CORINFO_CLASS_HANDLE cls       = m_compiler->gtGetClassHandle(node, &isExact, &isNonNull);
                if (cls != NO_CLASS_HANDLE)
                {
                    m_compiler->lvaSetClass(tmpNum, cls, isExact);
                }
            }

            Statement* storeStmt = m_compiler->fgNewStmtFromTree(store, m_stmt->GetDebugInfo());
            m_compiler->fgInsertStmtAtEnd(block, storeStmt);

            *use = m_compiler->gtNewLclVarNode(tmpNum);
        }

        //------------------------------------------------------------------------
        // CreateRemainder: split current block at the call stmt and
        // insert statements after the call into m_remainderBlock.
        //
        void CreateRemainder()
        {
            m_remainderBlock = m_compiler->fgSplitBlockAfterStatement(m_currBlock, m_stmt);
            m_remainderBlock->SetFlags(BBF_INTERNAL);
            m_remainderBlock->RemoveFlags(BBF_DONT_REMOVE);

            // We will be adding more blocks after m_currBlock, so remove edge to m_remainderBlock.
            //
            m_compiler->fgRemoveRefPred(m_currBlock->GetTargetEdge());
        }

        virtual void CreateCheck(uint8_t checkIdx) = 0;

        //------------------------------------------------------------------------
        // CreateAndInsertBasicBlock: ask compiler to create new basic block.
        // and insert in into the basic block list.
        //
        // Arguments:
        //    jumpKind    - jump kind for the new basic block
        //    insertAfter - basic block, after which compiler has to insert the new one.
        //    flagsSource - basic block to copy BBF_SPLIT_GAINED flags from
        //
        // Return Value:
        //    new basic block.
        BasicBlock* CreateAndInsertBasicBlock(BBKinds jumpKind, BasicBlock* insertAfter, BasicBlock* flagsSource)
        {
            BasicBlock* block = m_compiler->fgNewBBafter(jumpKind, insertAfter, true);
            block->SetFlags(BBF_IMPORTED);
            if (flagsSource != nullptr)
            {
                block->CopyFlags(flagsSource, BBF_SPLIT_GAINED);
            }
            block->RemoveFlags(BBF_DONT_REMOVE);
            return block;
        }

        virtual void CreateThen(uint8_t checkIdx) = 0;
        virtual void CreateElse()                 = 0;

        //------------------------------------------------------------------------
        // GetChecksCount: Get number of Check-Then pairs
        //
        virtual UINT8 GetChecksCount()
        {
            return 1;
        }

        //------------------------------------------------------------------------
        // RemoveOldStatement: remove original stmt from current block.
        //
        void RemoveOldStatement()
        {
            m_compiler->fgRemoveStmt(m_currBlock, m_stmt);
        }

        //------------------------------------------------------------------------
        // SetWeights: set weights for new blocks.
        //
        virtual void SetWeights()
        {
            m_remainderBlock->inheritWeight(m_currBlock);
            m_checkBlock->inheritWeight(m_currBlock);
            m_thenBlock->inheritWeightPercentage(m_currBlock, m_likelihood);
            m_elseBlock->inheritWeightPercentage(m_currBlock, 100 - m_likelihood);
        }

        //------------------------------------------------------------------------
        // ChainFlow: link new blocks into correct cfg.
        //
        virtual void ChainFlow()
        {
            assert(m_compiler->fgPredsComputed);

            // m_currBlock
            if (m_checkBlock != m_currBlock)
            {
                assert(m_currBlock->KindIs(BBJ_ALWAYS));
                FlowEdge* const newEdge = m_compiler->fgAddRefPred(m_checkBlock, m_currBlock);
                m_currBlock->SetTargetEdge(newEdge);
            }

            // m_checkBlock
            // Todo: get likelihoods right
            //
            assert(m_checkBlock->KindIs(BBJ_ALWAYS));
            FlowEdge* const thenEdge = m_compiler->fgAddRefPred(m_thenBlock, m_checkBlock);
            thenEdge->setLikelihood(0.5);
            FlowEdge* const elseEdge = m_compiler->fgAddRefPred(m_elseBlock, m_checkBlock);
            elseEdge->setLikelihood(0.5);
            m_checkBlock->SetCond(elseEdge, thenEdge);

            // m_thenBlock
            {
                assert(m_thenBlock->KindIs(BBJ_ALWAYS));
                FlowEdge* const newEdge = m_compiler->fgAddRefPred(m_remainderBlock, m_thenBlock);
                m_thenBlock->SetTargetEdge(newEdge);
            }

            // m_elseBlock
            {
                assert(m_elseBlock->KindIs(BBJ_ALWAYS));
                FlowEdge* const newEdge = m_compiler->fgAddRefPred(m_remainderBlock, m_elseBlock);
                m_elseBlock->SetTargetEdge(newEdge);
            }
        }

        Compiler*    m_compiler;
        BasicBlock*  m_currBlock;
        Statement*   m_stmt;
        BasicBlock*  m_remainderBlock = nullptr;
        BasicBlock*  m_checkBlock     = nullptr;
        BasicBlock*  m_thenBlock      = nullptr;
        BasicBlock*  m_elseBlock      = nullptr;
        GenTreeCall* m_origCall       = nullptr;
        unsigned     m_likelihood     = 80; // High likelihood that check succeeds
    };

    class FatPointerCallTransformer final : public Transformer
    {
    public:
        FatPointerCallTransformer(Compiler* compiler, BasicBlock* block, Statement* stmt)
            : Transformer(compiler, block, stmt)
        {
            m_doesReturnValue = stmt->GetRootNode()->OperIs(GT_STORE_LCL_VAR);
            m_origCall        = GetCall(stmt);
            m_fptrAddress     = m_origCall->gtControlExpr;
            m_pointerType     = m_fptrAddress->TypeGet();
        }

    protected:
        virtual const char* Name()
        {
            return "FatPointerCall";
        }

        //------------------------------------------------------------------------
        // GetCall: find a call in a statement.
        //
        // Arguments:
        //    callStmt - the statement with the call inside.
        //
        // Return Value:
        //    call tree node pointer.
        virtual GenTreeCall* GetCall(Statement* callStmt)
        {
            GenTree*     tree = callStmt->GetRootNode();
            GenTreeCall* call = nullptr;
            if (m_doesReturnValue)
            {
                assert(tree->OperIs(GT_STORE_LCL_VAR));
                call = tree->AsLclVar()->Data()->AsCall();
            }
            else
            {
                call = tree->AsCall(); // call with void return type.
            }
            return call;
        }

        //------------------------------------------------------------------------
        // ClearFlag: clear fat pointer candidate flag from the original call.
        //
        virtual void ClearFlag()
        {
            m_origCall->ClearFatPointerCandidate();
        }

        // FixupRetExpr: no action needed as we handle this in the importer.
        virtual void FixupRetExpr()
        {
        }

        //------------------------------------------------------------------------
        // CreateCheck: create check block, that checks fat pointer bit set.
        //
        virtual void CreateCheck(uint8_t checkIdx)
        {
            assert(checkIdx == 0);

            if (m_origCall->IsGenericVirtual(m_compiler))
            {
                SplitCall(m_currBlock, &m_origCall->gtControlExpr);
                m_fptrAddress = m_origCall->gtControlExpr;
            }

            m_checkBlock               = CreateAndInsertBasicBlock(BBJ_ALWAYS, m_currBlock, m_currBlock);
            GenTree*   fatPointerMask  = new (m_compiler, GT_CNS_INT) GenTreeIntCon(TYP_I_IMPL, FAT_POINTER_MASK);
            GenTree*   fptrAddressCopy = m_compiler->gtCloneExpr(m_fptrAddress);
            GenTree*   fatPointerAnd   = m_compiler->gtNewOperNode(GT_AND, TYP_I_IMPL, fptrAddressCopy, fatPointerMask);
            GenTree*   zero            = new (m_compiler, GT_CNS_INT) GenTreeIntCon(TYP_I_IMPL, 0);
            GenTree*   fatPointerCmp   = m_compiler->gtNewOperNode(GT_NE, TYP_INT, fatPointerAnd, zero);
            GenTree*   jmpTree         = m_compiler->gtNewOperNode(GT_JTRUE, TYP_VOID, fatPointerCmp);
            Statement* jmpStmt         = m_compiler->fgNewStmtFromTree(jmpTree, m_stmt->GetDebugInfo());
            m_compiler->fgInsertStmtAtEnd(m_checkBlock, jmpStmt);
        }

        //------------------------------------------------------------------------
        // CreateThen: create then block, that is executed if the check succeeds.
        // This simply executes the original call.
        //
        virtual void CreateThen(uint8_t checkIdx)
        {
            assert(m_remainderBlock != nullptr);
            m_thenBlock                   = CreateAndInsertBasicBlock(BBJ_ALWAYS, m_checkBlock, m_currBlock);
            Statement* copyOfOriginalStmt = m_compiler->gtCloneStmt(m_stmt);
            m_compiler->fgInsertStmtAtEnd(m_thenBlock, copyOfOriginalStmt);
        }

        //------------------------------------------------------------------------
        // CreateElse: create else block, that is executed if call address is fat pointer.
        //
        virtual void CreateElse()
        {
            m_elseBlock = CreateAndInsertBasicBlock(BBJ_ALWAYS, m_thenBlock, m_currBlock);

            GenTree* fixedFptrAddress = GetFixedFptrAddress();
            GenTree* actualCallAddress =
                m_compiler->gtNewIndir(m_pointerType, fixedFptrAddress, GTF_IND_NONFAULTING | GTF_IND_INVARIANT);
            GenTree* hiddenArgument = GetHiddenArgument(fixedFptrAddress);

            Statement* fatStmt = CreateFatCallStmt(actualCallAddress, hiddenArgument);
            m_compiler->fgInsertStmtAtEnd(m_elseBlock, fatStmt);
        }

        //------------------------------------------------------------------------
        // GetFixedFptrAddress: clear fat pointer bit from fat pointer address.
        //
        // Return Value:
        //    address without fat pointer bit set.
        GenTree* GetFixedFptrAddress()
        {
            GenTree* fptrAddressCopy = m_compiler->gtCloneExpr(m_fptrAddress);
            GenTree* fatPointerMask  = new (m_compiler, GT_CNS_INT) GenTreeIntCon(TYP_I_IMPL, FAT_POINTER_MASK);
            return m_compiler->gtNewOperNode(GT_SUB, m_pointerType, fptrAddressCopy, fatPointerMask);
        }

        //------------------------------------------------------------------------
        // GetHiddenArgument: load hidden argument.
        //
        // Arguments:
        //    fixedFptrAddress - pointer to the tuple <methodPointer, instantiationArgument>
        //
        // Return Value:
        //    generic context hidden argument.
        GenTree* GetHiddenArgument(GenTree* fixedFptrAddress)
        {
            GenTree* fixedFptrAddressCopy = m_compiler->gtCloneExpr(fixedFptrAddress);
            GenTree* wordSize = new (m_compiler, GT_CNS_INT) GenTreeIntCon(TYP_I_IMPL, genTypeSize(TYP_I_IMPL));
            GenTree* hiddenArgumentPtr =
                m_compiler->gtNewOperNode(GT_ADD, m_pointerType, fixedFptrAddressCopy, wordSize);
            return m_compiler->gtNewIndir(fixedFptrAddressCopy->TypeGet(), hiddenArgumentPtr,
                                          GTF_IND_NONFAULTING | GTF_IND_INVARIANT);
        }

        //------------------------------------------------------------------------
        // CreateFatCallStmt: create call with fixed call address and hidden argument in the args list.
        //
        // Arguments:
        //    actualCallAddress - fixed call address
        //    hiddenArgument - generic context hidden argument
        //
        // Return Value:
        //    created call node.
        Statement* CreateFatCallStmt(GenTree* actualCallAddress, GenTree* hiddenArgument)
        {
            Statement*   fatStmt   = m_compiler->gtCloneStmt(m_stmt);
            GenTreeCall* fatCall   = GetCall(fatStmt);
            fatCall->gtControlExpr = actualCallAddress;
            fatCall->gtArgs.InsertInstParam(m_compiler, hiddenArgument);
            return fatStmt;
        }

    private:
        const int FAT_POINTER_MASK = 0x2;

        GenTree*  m_fptrAddress;
        var_types m_pointerType;
        bool      m_doesReturnValue;
    };

    class GuardedDevirtualizationTransformer final : public Transformer
    {
    public:
        GuardedDevirtualizationTransformer(Compiler* compiler, BasicBlock* block, Statement* stmt)
            : Transformer(compiler, block, stmt)
        {
        }

        //------------------------------------------------------------------------
        // Run: transform the statement as described above.
        //
        virtual void Run()
        {
            m_origCall = GetCall(m_stmt);

            JITDUMP("\n----------------\n\n*** %s contemplating [%06u] in " FMT_BB " \n", Name(),
                    m_compiler->dspTreeID(m_origCall), m_currBlock->bbNum);

            // We currently need inline candidate info to guarded devirt.
            //
            if (!m_origCall->IsInlineCandidate())
            {
                JITDUMP("*** %s Bailing on [%06u] -- not an inline candidate\n", Name(),
                        m_compiler->dspTreeID(m_origCall));
                ClearFlag();
                return;
            }

            m_likelihood = m_origCall->GetGDVCandidateInfo(0)->likelihood;
            assert((m_likelihood >= 0) && (m_likelihood <= 100));
            JITDUMP("Likelihood of correct guess is %u\n", m_likelihood);

            // TODO: implement chaining for multiple GDV candidates
            const bool canChainGdv =
                (GetChecksCount() == 1) && ((m_origCall->gtCallMoreFlags & GTF_CALL_M_GUARDED_DEVIRT_EXACT) == 0);
            if (canChainGdv)
            {
                m_compiler->Metrics.GDV++;
                if (GetChecksCount() > 1)
                {
                    m_compiler->Metrics.MultiGuessGDV++;
                }

                const bool isChainedGdv = (m_origCall->gtCallMoreFlags & GTF_CALL_M_GUARDED_DEVIRT_CHAIN) != 0;

                if (isChainedGdv)
                {
                    JITDUMP("Expansion will chain to the previous GDV\n");
                }

                Transform();

                if (isChainedGdv)
                {
                    m_compiler->Metrics.ChainedGDV++;
                    TransformForChainedGdv();
                }

                // Look ahead and see if there's another Gdv we might chain to this one.
                //
                ScoutForChainedGdv();
            }
            else
            {
                JITDUMP("Expansion will not chain to the previous GDV due to multiple type checks\n");
                Transform();
            }
        }

    protected:
        virtual const char* Name()
        {
            return "GuardedDevirtualization";
        }

        //------------------------------------------------------------------------
        // GetCall: find a call in a statement.
        //
        // Arguments:
        //    callStmt - the statement with the call inside.
        //
        // Return Value:
        //    call tree node pointer.
        virtual GenTreeCall* GetCall(Statement* callStmt)
        {
            GenTree* tree = callStmt->GetRootNode();
            assert(tree->IsCall());
            GenTreeCall* call = tree->AsCall();
            return call;
        }

        virtual void ClearFlag()
        {
            // We remove the GDV flag from the call in the CreateElse
        }

        virtual UINT8 GetChecksCount()
        {
            return m_origCall->GetInlineCandidatesCount();
        }

        virtual void ChainFlow()
        {
            assert(m_compiler->fgPredsComputed);

            // Chaining is done in-place.
        }

        virtual void SetWeights()
        {
            // m_remainderBlock has the same weight as the original block.
            m_remainderBlock->inheritWeight(m_currBlock);

            // The rest of the weights are assigned in-place.
        }

        //------------------------------------------------------------------------
        // CreateCheck: create check block and check method table
        //
        virtual void CreateCheck(uint8_t checkIdx)
        {
            if (checkIdx == 0)
            {
                // There's no need for a new block here. We can just append to m_currBlock.
                //
                m_checkBlock        = m_currBlock;
                m_checkFallsThrough = false;
            }
            else
            {
                // In case of multiple checks, append to the previous m_thenBlock block
                // (Set jump target of new m_checkBlock in CreateThen())
                BasicBlock* prevCheckBlock = m_checkBlock;
                m_checkBlock               = CreateAndInsertBasicBlock(BBJ_ALWAYS, m_thenBlock, m_currBlock);
                m_checkFallsThrough        = false;

                // We computed the "then" likelihood in CreateThen, so we
                // just use that to figure out the "else" likelihood.
                //
                assert(prevCheckBlock->KindIs(BBJ_ALWAYS));
                assert(prevCheckBlock->JumpsToNext());
                FlowEdge* const prevCheckThenEdge = prevCheckBlock->GetTargetEdge();
                weight_t        checkLikelihood   = max(0.0, 1.0 - prevCheckThenEdge->getLikelihood());

                JITDUMP("Level %u Check block " FMT_BB " success likelihood " FMT_WT "\n", checkIdx,
                        m_checkBlock->bbNum, checkLikelihood);

                // prevCheckBlock is expected to jump to this new check (if its type check doesn't succeed)
                //
                FlowEdge* const prevCheckCheckEdge = m_compiler->fgAddRefPred(m_checkBlock, prevCheckBlock);
                prevCheckCheckEdge->setLikelihood(checkLikelihood);
                m_checkBlock->inheritWeight(prevCheckBlock);
                m_checkBlock->scaleBBWeight(checkLikelihood);
                prevCheckBlock->SetCond(prevCheckCheckEdge, prevCheckThenEdge);
            }

            CallArg* thisArg = m_origCall->gtArgs.GetThisArg();
            SplitCall(m_checkBlock, &thisArg->EarlyNodeRef());

            GenTree* thisTree = m_compiler->gtCloneExpr(thisArg->GetNode());

            // Remember the current last statement. If we're doing a chained GDV, we'll clone/copy
            // all the code in the check block up to and including this statement.
            //
            // Note it's important that we clone/copy the temp assign above, if we created one,
            // because flow along the "cold path" is going to bypass the check block.
            //
            m_lastStmt = m_checkBlock->lastStmt();

            // In case if GDV candidates are "exact" (e.g. we have the full list of classes implementing
            // the given interface in the app - NativeAOT only at this moment) we assume the last
            // check will always be true, so we just simplify the block to BBJ_ALWAYS
            const bool isLastCheck = (checkIdx == m_origCall->GetInlineCandidatesCount() - 1);
            if (isLastCheck && ((m_origCall->gtCallMoreFlags & GTF_CALL_M_GUARDED_DEVIRT_EXACT) != 0))
            {
                assert(m_checkBlock->KindIs(BBJ_ALWAYS));
                m_checkFallsThrough = true;
                return;
            }

            InlineCandidateInfo* guardedInfo = m_origCall->GetGDVCandidateInfo(checkIdx);

            // Create comparison. On success we will jump to do the indirect call.
            GenTree* compare;
            if (guardedInfo->guardedClassHandle != NO_CLASS_HANDLE)
            {
                // Find target method table
                //
                GenTree*             methodTable       = m_compiler->gtNewMethodTableLookup(thisTree);
                CORINFO_CLASS_HANDLE clsHnd            = guardedInfo->guardedClassHandle;
                GenTree*             targetMethodTable = m_compiler->gtNewIconEmbClsHndNode(clsHnd);

                compare = m_compiler->gtNewOperNode(GT_NE, TYP_INT, targetMethodTable, methodTable);
                m_compiler->Metrics.ClassGDV++;
            }
            else
            {
                assert(m_origCall->IsVirtualVtable() || m_origCall->IsDelegateInvoke());
                // We reuse the target except if this is a chained GDV, in
                // which case the check will be moved into the success case of
                // a previous GDV and thus may not execute when we hit the cold
                // path.
                if (m_origCall->IsVirtualVtable())
                {
                    GenTree* tarTree = m_compiler->fgExpandVirtualVtableCallTarget(m_origCall);

                    CORINFO_METHOD_HANDLE methHnd = guardedInfo->guardedMethodHandle;
                    CORINFO_CONST_LOOKUP  lookup;
                    m_compiler->info.compCompHnd->getFunctionEntryPoint(methHnd, &lookup);

                    GenTree* compareTarTree = CreateTreeForLookup(methHnd, lookup);
                    compare                 = m_compiler->gtNewOperNode(GT_NE, TYP_INT, compareTarTree, tarTree);
                }
                else
                {
                    GenTree* offset =
                        m_compiler->gtNewIconNode((ssize_t)m_compiler->eeGetEEInfo()->offsetOfDelegateFirstTarget,
                                                  TYP_I_IMPL);
                    GenTree* tarTree = m_compiler->gtNewOperNode(GT_ADD, TYP_BYREF, thisTree, offset);
                    tarTree          = m_compiler->gtNewIndir(TYP_I_IMPL, tarTree, GTF_IND_INVARIANT);

                    CORINFO_METHOD_HANDLE methHnd = guardedInfo->guardedMethodHandle;
                    CORINFO_CONST_LOOKUP  lookup;
                    m_compiler->info.compCompHnd->getFunctionFixedEntryPoint(methHnd, false, &lookup);

                    GenTree* compareTarTree = CreateTreeForLookup(methHnd, lookup);
                    compare                 = m_compiler->gtNewOperNode(GT_NE, TYP_INT, compareTarTree, tarTree);
                }
                m_compiler->Metrics.MethodGDV++;
            }

            GenTree*   jmpTree = m_compiler->gtNewOperNode(GT_JTRUE, TYP_VOID, compare);
            Statement* jmpStmt = m_compiler->fgNewStmtFromTree(jmpTree, m_stmt->GetDebugInfo());
            m_compiler->fgInsertStmtAtEnd(m_checkBlock, jmpStmt);
        }

        //------------------------------------------------------------------------
        // FixupRetExpr: set up to repair return value placeholder from call
        //
        virtual void FixupRetExpr()
        {
            // If call returns a value, we need to copy it to a temp, and
            // bash the associated GT_RET_EXPR to refer to the temp instead
            // of the call.
            //
            // Note implicit by-ref returns should have already been converted
            // so any struct copy we induce here should be cheap.
            InlineCandidateInfo* const inlineInfo  = m_origCall->GetGDVCandidateInfo(0);
            GenTree* const             retExprNode = inlineInfo->retExpr;

            if (retExprNode == nullptr)
            {
                // We do not produce GT_RET_EXPRs for CTOR calls, so there is nothing to patch.
                return;
            }

            GenTreeRetExpr* const retExpr       = retExprNode->AsRetExpr();
            bool const            noReturnValue = m_origCall->TypeIs(TYP_VOID);

            // If there is a return value, search the next statement to see if we can find
            // retExprNode's parent. If we find it, see if retExprNode's value is unused.
            //
            // If we fail to find it, we will assume the return value is used.
            //
            if (!noReturnValue)
            {
                Statement* const nextStmt = m_stmt->GetNextStmt();
                if (nextStmt != nullptr)
                {
                    Compiler::FindLinkData fld    = m_compiler->gtFindLink(nextStmt, retExprNode);
                    GenTree* const         parent = fld.parent;

                    if ((parent != nullptr) && parent->OperIs(GT_COMMA) && (parent->AsOp()->gtGetOp1() == retExprNode))
                    {
                        m_returnValueUnused = true;
                        JITDUMP("GT_RET_EXPR [%06u] value is unused\n", m_compiler->dspTreeID(retExprNode));
                    }
                }
            }

            if (noReturnValue)
            {
                JITDUMP("Linking GT_RET_EXPR [%06u] for VOID return to NOP\n",
                        m_compiler->dspTreeID(inlineInfo->retExpr));
                inlineInfo->retExpr->gtSubstExpr = m_compiler->gtNewNothingNode();
            }
            else if (m_returnValueUnused)
            {
                JITDUMP("Linking GT_RET_EXPR [%06u] for UNUSED return to NOP\n",
                        m_compiler->dspTreeID(inlineInfo->retExpr));
                inlineInfo->retExpr->gtSubstExpr = m_compiler->gtNewNothingNode();
            }
            else
            {
                // If there's a spill temp already associated with this inline candidate,
                // use that instead of allocating a new temp.
                //
                m_returnTemp = inlineInfo->preexistingSpillTemp;

                if (m_returnTemp != BAD_VAR_NUM)
                {
                    JITDUMP("Reworking call(s) to return value via a existing return temp V%02u\n", m_returnTemp);

                    // We will be introducing multiple defs for this temp, so make sure
                    // it is no longer marked as single def.
                    //
                    // Otherwise, we could make an incorrect type deduction. Say the
                    // original call site returns a B, but after we devirtualize along the
                    // GDV happy path we see that method returns a D. We can't then assume that
                    // the return temp is type D, because we don't know what type the fallback
                    // path returns. So we have to stick with the current type for B as the
                    // return type.
                    //
                    // Note local vars always live in the root method's symbol table. So we
                    // need to use the root compiler for lookup here.
                    //
                    LclVarDsc* const returnTempLcl = m_compiler->impInlineRoot()->lvaGetDesc(m_returnTemp);

                    if (returnTempLcl->lvSingleDef == 1)
                    {
                        // In this case it's ok if we already updated the type assuming single def,
                        // we just don't want any further updates.
                        //
                        JITDUMP("Return temp V%02u is no longer a single def temp\n", m_returnTemp);
                        returnTempLcl->lvSingleDef = 0;
                    }
                }
                else
                {
                    m_returnTemp = m_compiler->lvaGrabTemp(false DEBUGARG("guarded devirt return temp"));
                    JITDUMP("Reworking call(s) to return value via a new temp V%02u\n", m_returnTemp);

                    // Keep the information about small typedness to avoid
                    // inserting unnecessary casts for normalization, which can
                    // make tailcall invariants unhappy. This is the same logic
                    // that impImportCall uses when it introduces call temps.
                    if (varTypeIsSmall(m_origCall->gtReturnType))
                    {
                        assert(m_origCall->NormalizesSmallTypesOnReturn());
                        m_compiler->lvaGetDesc(m_returnTemp)->lvType = m_origCall->gtReturnType;
                    }
                }

                if (varTypeIsStruct(m_origCall))
                {
                    m_compiler->lvaSetStruct(m_returnTemp, m_origCall->gtRetClsHnd, false);
                }

                GenTree* tempTree = m_compiler->gtNewLclvNode(m_returnTemp, m_origCall->TypeGet());

                JITDUMP("Linking GT_RET_EXPR [%06u] to refer to temp V%02u\n",
                        m_compiler->dspTreeID(inlineInfo->retExpr), m_returnTemp);

                inlineInfo->retExpr->gtSubstExpr = tempTree;
            }
        }

        //------------------------------------------------------------------------
        // Devirtualize m_origCall using the given inline candidate
        //
        void DevirtualizeCall(BasicBlock* block, uint8_t candidateId)
        {
            InlineCandidateInfo* inlineInfo = m_origCall->GetGDVCandidateInfo(candidateId);
            CORINFO_CLASS_HANDLE clsHnd     = inlineInfo->guardedClassHandle;

            //
            // Copy the 'this' for the devirtualized call to a new temp. For
            // class-based GDV this will allow us to set the exact type on that
            // temp. For delegate GDV, this will be the actual 'this' object
            // stored in the delegate.
            //
            const unsigned thisTemp  = m_compiler->lvaGrabTemp(false DEBUGARG("guarded devirt this exact temp"));
            GenTree*       clonedObj = m_compiler->gtCloneExpr(m_origCall->gtArgs.GetThisArg()->GetNode());
            GenTree*       newThisObj;
            if (m_origCall->IsDelegateInvoke())
            {
                GenTree* offset =
                    m_compiler->gtNewIconNode((ssize_t)m_compiler->eeGetEEInfo()->offsetOfDelegateInstance, TYP_I_IMPL);
                newThisObj = m_compiler->gtNewOperNode(GT_ADD, TYP_BYREF, clonedObj, offset);
                newThisObj = m_compiler->gtNewIndir(TYP_REF, newThisObj);
            }
            else
            {
                newThisObj = clonedObj;
            }
            GenTree* store = m_compiler->gtNewTempStore(thisTemp, newThisObj);

            if (clsHnd != NO_CLASS_HANDLE)
            {
                m_compiler->lvaSetClass(thisTemp, clsHnd, true);
            }
            else
            {
                m_compiler->lvaSetClass(thisTemp,
                                        m_compiler->info.compCompHnd->getMethodClass(inlineInfo->guardedMethodHandle));
            }

            m_compiler->fgNewStmtAtEnd(block, store);

            // Clone call for the devirtualized case. Note we must use the
            // special candidate helper and we need to use the new 'this'.
            GenTreeCall* call = m_compiler->gtCloneCandidateCall(m_origCall);
            call->gtArgs.GetThisArg()->SetEarlyNode(m_compiler->gtNewLclvNode(thisTemp, TYP_REF));

            // If the original call was flagged as one that might inspire enumerator de-abstraction
            // cloning, move the flag to the devirtualized call.
            //
            if (m_compiler->hasImpEnumeratorGdvLocalMap())
            {
                Compiler::NodeToUnsignedMap* const map           = m_compiler->getImpEnumeratorGdvLocalMap();
                unsigned                           enumeratorLcl = BAD_VAR_NUM;
                if (map->Lookup(m_origCall, &enumeratorLcl))
                {
                    JITDUMP("Flagging [%06u] for enumerator cloning via V%02u\n", m_compiler->dspTreeID(call),
                            enumeratorLcl);
                    map->Remove(m_origCall);
                    map->Set(call, enumeratorLcl);
                }
            }

            INDEBUG(call->SetIsGuarded());

            JITDUMP("Direct call [%06u] in block " FMT_BB "\n", m_compiler->dspTreeID(call), block->bbNum);

            CORINFO_METHOD_HANDLE methodHnd = inlineInfo->guardedMethodHandle;

            bool objClassIsExact;
            bool objIsNonNull;
            // class-GDV uses the exact temp on the cloned call, method/delegate GDV uses newThisObj
            GenTree* thisObj = (clsHnd != NO_CLASS_HANDLE) ? call->gtArgs.GetThisArg()->GetEarlyNode() : newThisObj;
            m_compiler->gtGetClassHandle(thisObj, &objClassIsExact, &objIsNonNull);

            unsigned derivedMethodAttribs = m_compiler->info.compCompHnd->getMethodAttribs(methodHnd);

            // Transform the already-resolved GDV target into a direct call.
            //
            CORINFO_CONTEXT_HANDLE context      = nullptr;
            CORINFO_CONTEXT_HANDLE exactContext = inlineInfo->exactContextHandle;

            Compiler::DevirtualizedCallInfo dcInfo;
            dcInfo.tokenLookupContext = exactContext;

            CORINFO_SIG_INFO derivedSig;
            m_compiler->info.compCompHnd->getMethodSig(methodHnd, &derivedSig);
            dcInfo.pMethSig = &derivedSig;

            // only for class-based GDV in R2R
            dcInfo.pResolvedToken = (clsHnd != NO_CLASS_HANDLE) ? &inlineInfo->guardedMethodResolvedToken : nullptr;
            dcInfo.pUnboxedResolvedToken =
                (clsHnd != NO_CLASS_HANDLE) ? &inlineInfo->guardedMethodUnboxedResolvedToken : nullptr;

            dcInfo.objIsNonNull         = objIsNonNull;
            dcInfo.hadImplicitNullCheck = m_origCall->IsVirtual();
            dcInfo.isDelegateCall       = m_origCall->IsDelegateInvoke();
            dcInfo.isExplicitTailCall   = (call->gtCallMoreFlags & GTF_CALL_M_EXPLICIT_TAILCALL) != 0;
            dcInfo.objClassIsExact      = (clsHnd != NO_CLASS_HANDLE) && objClassIsExact;
            dcInfo.objClassIsFinal      = false;
            dcInfo.ilOffset             = inlineInfo->ilOffset;
            dcInfo.pInstParamLookup     = &inlineInfo->guardedMethodInstParamLookup;

            m_compiler->impTransformDevirtualizedCall(call, &methodHnd, &derivedMethodAttribs, &dcInfo, block,
                                                      &context COMMA_INDEBUG(inlineInfo->originalMethodHandle));

            // We know this call can devirtualize or we would not have set up GDV here.
            // So above code should succeed in devirtualizing.
            //
            assert(!call->IsVirtual() && !call->IsDelegateInvoke());

            // If the devirtualizer was unable to transform the call to invoke the unboxed entry, the inline info
            // we set up may be invalid. We won't be able to inline anyways. So demote the call as an inline candidate.
            //
            CORINFO_METHOD_HANDLE unboxedMethodHnd = inlineInfo->guardedMethodUnboxedResolvedToken.hMethod;
            if ((unboxedMethodHnd != nullptr) && (methodHnd != unboxedMethodHnd))
            {
                // Demote this call to a non-inline candidate
                //
                JITDUMP("Devirtualization was unable to use the unboxed entry; so marking call (to boxed entry) as not "
                        "inlineable\n");

                call->gtFlags &= ~GTF_CALL_INLINE_CANDIDATE;
                call->ClearInlineInfo();

                if (m_returnTemp != BAD_VAR_NUM)
                {
                    GenTree* const store = m_compiler->gtNewTempStore(m_returnTemp, call);
                    m_compiler->fgNewStmtAtEnd(block, store);
                }
                else
                {
                    m_compiler->fgNewStmtAtEnd(block, call, m_stmt->GetDebugInfo());
                }
            }
            else
            {
                // Add the call.
                //
                m_compiler->fgNewStmtAtEnd(block, call, m_stmt->GetDebugInfo());

                // Re-establish this call as an inline candidate.
                //
                GenTreeRetExpr* oldRetExpr       = inlineInfo->retExpr;
                inlineInfo->clsHandle            = m_compiler->info.compCompHnd->getMethodClass(methodHnd);
                inlineInfo->exactContextHandle   = exactContext;
                inlineInfo->preexistingSpillTemp = m_returnTemp;
                call->SetSingleInlineCandidateInfo(inlineInfo);

                // If there was a ret expr for this call, we need to create a new one
                // and append it just after the call.
                //
                // Note the original GT_RET_EXPR has been linked to a temp.
                // we set all this up in FixupRetExpr().
                if (oldRetExpr != nullptr)
                {
                    inlineInfo->retExpr = m_compiler->gtNewInlineCandidateReturnExpr(call, call->TypeGet());
                    GenTree* newRetExpr = inlineInfo->retExpr;

                    if (m_returnTemp != BAD_VAR_NUM)
                    {
                        newRetExpr = m_compiler->gtNewTempStore(m_returnTemp, newRetExpr);
                    }
                    else
                    {
                        // We should always have a return temp if we return results by value
                        // and that value is used.
                        assert(m_origCall->TypeIs(TYP_VOID) || m_returnValueUnused);
                        newRetExpr = m_compiler->gtUnusedValNode(newRetExpr);
                    }
                    m_compiler->fgNewStmtAtEnd(block, newRetExpr);
                }
            }
        }

        //------------------------------------------------------------------------
        // CreateThen: create then block with direct call to method
        //
        virtual void CreateThen(uint8_t checkIdx)
        {
            // Compute likelihoods
            //
            // If this is the first check the likelihood is just the candidate likelihood.
            // If there are multiple checks things are a bit more complicated.
            //
            // Say we had three candidates with likelihoods 0.5, 0.3, and 0.1.
            //
            // The first one's likelihood is 0.5.
            //
            // The second one (given that we've already checked the first and failed)
            // is (0.3) / (1.0 - 0.5) = 0.6.
            //
            // The third one is (0.1) / (1.0 - (0.5 + 0.3)) = (0.1)/(0.2) = 0.5
            //
            // So to figure out the proper divisor, we start with 1.0 and subtract off each
            // preceeding test's likelihood of success.
            //
            unsigned const thenLikelihood = m_origCall->GetGDVCandidateInfo(checkIdx)->likelihood;
            unsigned       baseLikelihood = 0;

            for (uint8_t i = 0; i < checkIdx; i++)
            {
                baseLikelihood += m_origCall->GetGDVCandidateInfo(i)->likelihood;
            }
            assert(baseLikelihood < 100);
            baseLikelihood = 100 - baseLikelihood;

            weight_t adjustedThenLikelihood = min(((weight_t)thenLikelihood) / baseLikelihood, 100.0);
            JITDUMP("For check in " FMT_BB ": orig likelihood " FMT_WT ", base likelihood " FMT_WT
                    ", adjusted likelihood " FMT_WT "\n",
                    m_checkBlock->bbNum, (weight_t)thenLikelihood / 100.0, (weight_t)baseLikelihood / 100.0,
                    adjustedThenLikelihood);

            // m_thenBlock always jumps to m_remainderBlock
            //
            m_thenBlock = CreateAndInsertBasicBlock(BBJ_ALWAYS, m_checkBlock, m_currBlock);
            m_thenBlock->inheritWeight(m_checkBlock);
            m_thenBlock->scaleBBWeight(adjustedThenLikelihood);
            FlowEdge* const thenRemainderEdge = m_compiler->fgAddRefPred(m_remainderBlock, m_thenBlock);
            m_thenBlock->SetTargetEdge(thenRemainderEdge);

            // m_thenBlock has a single pred - last m_checkBlock.
            //
            assert(m_checkBlock->KindIs(BBJ_ALWAYS));
            FlowEdge* const checkThenEdge = m_compiler->fgAddRefPred(m_thenBlock, m_checkBlock);
            m_checkBlock->SetTargetEdge(checkThenEdge);
            assert(m_checkBlock->JumpsToNext());

            // SetTargetEdge() gave checkThenEdge a (correct) likelihood of 1.0.
            // Later on, we might convert this m_checkBlock into a BBJ_COND.
            // Since we have the adjusted likelihood calculated here, set it prematurely.
            // If we leave this block as a BBJ_ALWAYS, we'll assert later that the likelihood is 1.0.
            //
            checkThenEdge->setLikelihood(adjustedThenLikelihood);

            // We will set the "else edge" likelihood in CreateElse later,
            // based on the thenEdge likelihood.
            //
            DevirtualizeCall(m_thenBlock, checkIdx);
        }

        //------------------------------------------------------------------------
        // CreateElse: create else block. This executes the original indirect call.
        //
        virtual void CreateElse()
        {
            m_elseBlock = CreateAndInsertBasicBlock(BBJ_ALWAYS, m_thenBlock, m_currBlock);

            // We computed the "then" likelihood in CreateThen, so we
            // just use that to figure out the "else" likelihood.
            //
            assert(m_checkBlock->KindIs(BBJ_ALWAYS));
            FlowEdge* const checkThenEdge  = m_checkBlock->GetTargetEdge();
            weight_t        elseLikelihood = max(0.0, 1.0 - checkThenEdge->getLikelihood());

            // CheckBlock flows into m_elseBlock unless we deal with the case
            // where we know the last check is always true (in case of "exact" GDV)
            //
            if (!m_checkFallsThrough)
            {
                assert(m_checkBlock->JumpsToNext());
                FlowEdge* const checkElseEdge = m_compiler->fgAddRefPred(m_elseBlock, m_checkBlock);
                checkElseEdge->setLikelihood(elseLikelihood);
                m_checkBlock->SetCond(checkElseEdge, checkThenEdge);
            }
            else
            {
                // In theory, we could simplify the IR here, but since it's a rare case
                // and is NativeAOT-only, we just assume the unreached block will be removed
                // by other phases.
                assert(m_origCall->gtCallMoreFlags & GTF_CALL_M_GUARDED_DEVIRT_EXACT);

                // We aren't converting m_checkBlock to a BBJ_COND. Its successor edge likelihood should remain 1.0.
                //
                assert(checkThenEdge->getLikelihood() == 1.0);
            }

            // m_elseBlock always flows into m_remainderBlock
            FlowEdge* const elseRemainderEdge = m_compiler->fgAddRefPred(m_remainderBlock, m_elseBlock);
            m_elseBlock->SetTargetEdge(elseRemainderEdge);

            // Remove everything related to inlining from the original call
            m_origCall->ClearInlineInfo();

            m_elseBlock->inheritWeight(m_checkBlock);
            m_elseBlock->scaleBBWeight(elseLikelihood);

            GenTreeCall* call    = m_origCall;
            Statement*   newStmt = m_compiler->gtNewStmt(call, m_stmt->GetDebugInfo());

            call->gtFlags &= ~GTF_CALL_INLINE_CANDIDATE;

            INDEBUG(call->SetIsGuarded());

            JITDUMP("Residual call [%06u] moved to block " FMT_BB "\n", m_compiler->dspTreeID(call),
                    m_elseBlock->bbNum);

            if (m_returnTemp != BAD_VAR_NUM)
            {
                GenTree* store = m_compiler->gtNewTempStore(m_returnTemp, call);
                newStmt->SetRootNode(store);
            }

            m_compiler->fgInsertStmtAtEnd(m_elseBlock, newStmt);

            // Set the original statement to a nop.
            //
            m_stmt->SetRootNode(m_compiler->gtNewNothingNode());
        }

        // For chained gdv, we modify the expansion as follows:
        //
        // We verify the check block has two BBJ_ALWAYS predecessors, one of
        // which (the "cold path") ends in a normal call, the other in an
        // inline candidate call.
        //
        // All the statements in the check block before the type test are copied to the
        // predecessor blocks (one via cloning, the other via direct copy).
        //
        // The cold path block is then modified to bypass the type test and jump
        // directly to the else block.
        //
        void TransformForChainedGdv()
        {
            // Find the hot/cold predecessors. (Consider: just record these when
            // we did the scouting).
            //
            BasicBlock* const coldBlock = m_checkBlock->Prev();

            if (!coldBlock->KindIs(BBJ_ALWAYS) || !coldBlock->JumpsToNext())
            {
                JITDUMP("Unexpected flow from cold path " FMT_BB "\n", coldBlock->bbNum);
                return;
            }

            BasicBlock* const hotBlock = coldBlock->Prev();

            if (!hotBlock->KindIs(BBJ_ALWAYS) || !hotBlock->TargetIs(m_checkBlock))
            {
                JITDUMP("Unexpected flow from hot path " FMT_BB "\n", hotBlock->bbNum);
                return;
            }

            JITDUMP("Hot pred block is " FMT_BB " and cold pred block is " FMT_BB "\n", hotBlock->bbNum,
                    coldBlock->bbNum);

            // Clone and and copy the statements in the check block up to
            // and including m_lastStmt over to the hot block.
            //
            // This will be the "hot" copy of the code.
            //
            Statement* const afterLastStmt = m_lastStmt->GetNextStmt();

            for (Statement* checkStmt = m_checkBlock->firstStmt(); checkStmt != afterLastStmt;)
            {
                Statement* const nextStmt = checkStmt->GetNextStmt();

                // We should have ensured during scouting that all the statements
                // here can safely be cloned.
                //
                // Consider: allow inline candidates here, and keep them viable
                // in the hot copy, and demote them in the cold copy.
                //
                Statement* const clonedStmt = m_compiler->gtCloneStmt(checkStmt);
                m_compiler->fgInsertStmtAtEnd(hotBlock, clonedStmt);
                checkStmt = nextStmt;
            }

            // Now move the same span of statements to the cold block.
            //
            for (Statement* checkStmt = m_checkBlock->firstStmt(); checkStmt != afterLastStmt;)
            {
                Statement* const nextStmt = checkStmt->GetNextStmt();
                m_compiler->fgUnlinkStmt(m_checkBlock, checkStmt);
                m_compiler->fgInsertStmtAtEnd(coldBlock, checkStmt);
                checkStmt = nextStmt;
            }

            // Rewire the cold block to jump to the else block,
            // not fall through to the check block.
            //
            m_compiler->fgRedirectEdge(coldBlock->TargetEdgeRef(), m_elseBlock);

            // Update the profile data
            //
            if (coldBlock->hasProfileWeight())
            {
                // Check block
                //
                FlowEdge* const coldElseEdge   = m_compiler->fgGetPredForBlock(m_elseBlock, coldBlock);
                weight_t        newCheckWeight = m_checkBlock->bbWeight - coldElseEdge->getLikelyWeight();

                if (newCheckWeight < 0)
                {
                    // If weights were consistent, we expect at worst a slight underflow.
                    //
                    if (m_compiler->fgPgoConsistent)
                    {
                        bool const isReasonableUnderflow = Compiler::fgProfileWeightsEqual(newCheckWeight, 0.0);
                        assert(isReasonableUnderflow);

                        if (!isReasonableUnderflow)
                        {
                            JITDUMP("Profile data could not be locally repaired. Data %s inconsistent.\n",
                                    m_compiler->fgPgoConsistent ? "is now" : "was already");

                            if (m_compiler->fgPgoConsistent)
                            {
                                m_compiler->Metrics.ProfileInconsistentChainedGDV++;
                                m_compiler->fgPgoConsistent = false;
                            }
                        }
                    }

                    // No matter what, the minimum weight is zero
                    //
                    newCheckWeight = 0;
                }
                m_checkBlock->setBBProfileWeight(newCheckWeight);

                // Else block
                //
                FlowEdge* const checkElseEdge = m_compiler->fgGetPredForBlock(m_elseBlock, m_checkBlock);
                weight_t const  newElseWeight = checkElseEdge->getLikelyWeight() + coldElseEdge->getLikelyWeight();
                m_elseBlock->setBBProfileWeight(newElseWeight);

                // Then block
                //
                FlowEdge* const checkThenEdge = m_compiler->fgGetPredForBlock(m_thenBlock, m_checkBlock);
                m_thenBlock->setBBProfileWeight(checkThenEdge->getLikelyWeight());
            }
        }

        // When the current candidate has sufficiently high likelihood, scan
        // the remainer block looking for another GDV candidate.
        //
        // (also consider: if m_currBlock has sufficiently high execution frequency)
        //
        // We want to see if it makes sense to mark the subsequent GDV site as a "chained"
        // GDV, where we duplicate the code in between to stitch together the high-likehood
        // outcomes without a join.
        //
        void ScoutForChainedGdv()
        {
            // If the current call isn't sufficiently likely, don't try and form a chain.
            //
            const unsigned gdvChainLikelihood = JitConfig.JitGuardedDevirtualizationChainLikelihood();

            if (m_likelihood < gdvChainLikelihood)
            {
                return;
            }

            JITDUMP("Scouting for possible GDV chain as likelihood %u >= %u\n", m_likelihood, gdvChainLikelihood);

            const unsigned maxStatementDup   = JitConfig.JitGuardedDevirtualizationChainStatements();
            unsigned       chainStatementDup = 0;
            unsigned       chainNodeDup      = 0;
            unsigned       chainLikelihood   = 0;
            GenTreeCall*   chainedCall       = nullptr;

            // Helper class to check/fix a statement for clonability and count
            // the total number of nodes
            //
            class ClonabilityVisitor final : public GenTreeVisitor<ClonabilityVisitor>
            {
            public:
                enum
                {
                    DoPreOrder = true
                };

                GenTree* m_unclonableNode;
                unsigned m_nodeCount;

                ClonabilityVisitor(Compiler* compiler)
                    : GenTreeVisitor(compiler)
                    , m_unclonableNode(nullptr)
                    , m_nodeCount(0)
                {
                }

                fgWalkResult PreOrderVisit(GenTree** use, GenTree* user)
                {
                    GenTree* const node = *use;

                    if (node->IsCall())
                    {
                        GenTreeCall* const call = node->AsCall();

                        if (call->IsInlineCandidate() && !call->IsGuardedDevirtualizationCandidate())
                        {
                            m_unclonableNode = node;
                            return fgWalkResult::WALK_ABORT;
                        }
                    }
                    else if (node->OperIs(GT_RET_EXPR))
                    {
                        // If this is a RET_EXPR that we already know how to substitute then it is the
                        // "fixed-up" RET_EXPR from a previous GDV candidate. In that case we can
                        // substitute it right here to make it eligibile for cloning.
                        if (node->AsRetExpr()->gtSubstExpr != nullptr)
                        {
                            assert(node->AsRetExpr()->gtInlineCandidate->IsGuarded());
                            *use = node->AsRetExpr()->gtSubstExpr;
                            return fgWalkResult::WALK_CONTINUE;
                        }

                        m_unclonableNode = node;
                        return fgWalkResult::WALK_ABORT;
                    }

                    m_nodeCount++;
                    return fgWalkResult::WALK_CONTINUE;
                }
            };

            for (Statement* const nextStmt : m_remainderBlock->Statements())
            {
                JITDUMP(" Scouting " FMT_STMT "\n", nextStmt->GetID());

                // See if this is a guarded devirt candidate.
                // These will be top-level trees.
                //
                GenTree* const root = nextStmt->GetRootNode();

                if (root->IsCall())
                {
                    GenTreeCall* const call = root->AsCall();

                    if (call->IsGuardedDevirtualizationCandidate() &&
                        (call->GetGDVCandidateInfo(0)->likelihood >= gdvChainLikelihood))
                    {
                        JITDUMP("GDV call at [%06u] has likelihood %u >= %u; chaining (%u stmts, %u nodes to dup).\n",
                                m_compiler->dspTreeID(call), call->GetGDVCandidateInfo(0)->likelihood,
                                gdvChainLikelihood, chainStatementDup, chainNodeDup);

                        call->gtCallMoreFlags |= GTF_CALL_M_GUARDED_DEVIRT_CHAIN;
                        break;
                    }
                }

                // Stop searching if we've accumulated too much dup cost.
                // Consider: use node count instead.
                //
                if (chainStatementDup >= maxStatementDup)
                {
                    JITDUMP("  reached max statement dup limit of %u, bailing out\n", maxStatementDup);
                    break;
                }

                // See if this statement's tree is one that we can clone.
                //
                ClonabilityVisitor clonabilityVisitor(m_compiler);
                clonabilityVisitor.WalkTree(nextStmt->GetRootNodePointer(), nullptr);

                if (clonabilityVisitor.m_unclonableNode != nullptr)
                {
                    JITDUMP("  node [%06u] can't be cloned\n",
                            m_compiler->dspTreeID(clonabilityVisitor.m_unclonableNode));
                    break;
                }

                // Looks like we can clone this, so keep scouting.
                //
                chainStatementDup++;
                chainNodeDup += clonabilityVisitor.m_nodeCount;
            }
        }

    private:
        unsigned   m_returnTemp        = BAD_VAR_NUM;
        Statement* m_lastStmt          = nullptr;
        bool       m_checkFallsThrough = false;
        bool       m_returnValueUnused = false;

        //------------------------------------------------------------------------
        // CreateTreeForLookup: Create a tree representing a lookup of a method address.
        //
        // Arguments:
        //   methHnd - the handle for the method the lookup is for
        //   lookup  - lookup information for the address
        //
        // Returns:
        //   A node representing the lookup.
        //
        GenTree* CreateTreeForLookup(CORINFO_METHOD_HANDLE methHnd, const CORINFO_CONST_LOOKUP& lookup)
        {
            switch (lookup.accessType)
            {
                case IAT_VALUE:
                {
                    return CreateFunctionTargetAddr(methHnd, lookup);
                }
                case IAT_PVALUE:
                {
                    GenTree* tree = CreateFunctionTargetAddr(methHnd, lookup);
                    tree          = m_compiler->gtNewIndir(TYP_I_IMPL, tree, GTF_IND_NONFAULTING | GTF_IND_INVARIANT);
                    return tree;
                }
                case IAT_PPVALUE:
                {
                    noway_assert(!"Unexpected IAT_PPVALUE");
                    return nullptr;
                }
                case IAT_RELPVALUE:
                {
                    GenTree* addr = CreateFunctionTargetAddr(methHnd, lookup);
                    GenTree* tree = CreateFunctionTargetAddr(methHnd, lookup);
                    tree          = m_compiler->gtNewIndir(TYP_I_IMPL, tree, GTF_IND_NONFAULTING | GTF_IND_INVARIANT);
                    tree          = m_compiler->gtNewOperNode(GT_ADD, TYP_I_IMPL, tree, addr);
                    return tree;
                }
                default:
                {
                    noway_assert(!"Bad accessType");
                    return nullptr;
                }
            }
        }

        GenTree* CreateFunctionTargetAddr(CORINFO_METHOD_HANDLE methHnd, const CORINFO_CONST_LOOKUP& lookup)
        {
            GenTree* con = m_compiler->gtNewIconHandleNode((size_t)lookup.addr, GTF_ICON_FTN_ADDR);
            INDEBUG(con->AsIntCon()->SetTargetHandle((size_t)methHnd));
            return con;
        }
    };

    Compiler* m_compiler;
};

#ifdef DEBUG

//------------------------------------------------------------------------
// fgDebugCheckForTransformableIndirectCalls: callback to make sure there
//  are no more GTF_CALL_M_FAT_POINTER_CHECK or GTF_CALL_M_GUARDED_DEVIRT
//  calls remaining
//
Compiler::fgWalkResult Compiler::fgDebugCheckForTransformableIndirectCalls(GenTree** pTree, fgWalkData* data)
{
    GenTree* tree = *pTree;
    if (tree->IsCall())
    {
        GenTreeCall* call = tree->AsCall();
        assert(!call->IsFatPointerCandidate());
        assert(!call->IsGuardedDevirtualizationCandidate());
    }
    return WALK_CONTINUE;
}

//------------------------------------------------------------------------
// CheckNoTransformableIndirectCallsRemain: walk through blocks and check
//    that there are no indirect call candidates left to transform.
//
void Compiler::CheckNoTransformableIndirectCallsRemain()
{
    assert(!doesMethodHaveFatPointer());

    for (BasicBlock* const block : Blocks())
    {
        for (Statement* const stmt : block->Statements())
        {
            fgWalkTreePre(stmt->GetRootNodePointer(), fgDebugCheckForTransformableIndirectCalls);
        }
    }
}
#endif

//------------------------------------------------------------------------
// fgTransformIndirectCalls: find and transform various indirect calls
//
// These transformations happen post-import because they may introduce
// control flow.
//
// Returns:
//   phase status indicating if changes were made
//
PhaseStatus Compiler::fgTransformIndirectCalls()
{
    int count = 0;
    if (doesMethodHaveFatPointer() || doesMethodHaveGuardedDevirtualization())
    {
        IndirectCallTransformer indirectCallTransformer(this);
        count = indirectCallTransformer.Run();

        if (count > 0)
        {
            JITDUMP("\n -- %d calls transformed\n", count);
        }
        else
        {
            JITDUMP("\n -- no transforms done (?)\n");
        }

        clearMethodHasFatPointer();
    }
    else
    {
        JITDUMP("\n -- no candidates to transform\n");
    }

    INDEBUG(CheckNoTransformableIndirectCallsRemain(););

    return (count == 0) ? PhaseStatus::MODIFIED_NOTHING : PhaseStatus::MODIFIED_EVERYTHING;
}
