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
    IndirectCallTransformer(Compiler* compiler) : compiler(compiler)
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

        for (BasicBlock* const block : compiler->Blocks())
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
            if (compiler->doesMethodHaveFatPointer() && ContainsFatCalli(stmt))
            {
                FatPointerCallTransformer transformer(compiler, block, stmt);
                transformer.Run();
                count++;
            }
            else if (compiler->doesMethodHaveGuardedDevirtualization() &&
                     ContainsGuardedDevirtualizationCandidate(stmt))
            {
                GuardedDevirtualizationTransformer transformer(compiler, block, stmt);
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
            : compiler(compiler), currBlock(block), stmt(stmt)
        {
            remainderBlock = nullptr;
            checkBlock     = nullptr;
            thenBlock      = nullptr;
            elseBlock      = nullptr;
            origCall       = nullptr;
            likelihood     = HIGH_PROBABILITY;
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
            JITDUMP("*** %s: transforming " FMT_STMT "\n", Name(), stmt->GetID());
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
        virtual void FixupRetExpr()                       = 0;

        //------------------------------------------------------------------------
        // CreateRemainder: split current block at the call stmt and
        // insert statements after the call into remainderBlock.
        //
        void CreateRemainder()
        {
            remainderBlock = compiler->fgSplitBlockAfterStatement(currBlock, stmt);
            remainderBlock->SetFlags(BBF_INTERNAL);
        }

        virtual void CreateCheck(uint8_t checkIdx) = 0;

        //------------------------------------------------------------------------
        // CreateAndInsertBasicBlock: ask compiler to create new basic block.
        // and insert in into the basic block list.
        //
        // Arguments:
        //    jumpKind - jump kind for the new basic block
        //    insertAfter - basic block, after which compiler has to insert the new one.
        //    jumpDest - jump target for the new basic block. Defaults to nullptr.
        //
        // Return Value:
        //    new basic block.
        BasicBlock* CreateAndInsertBasicBlock(BBKinds jumpKind, BasicBlock* insertAfter, BasicBlock* jumpDest = nullptr)
        {
            BasicBlock* block = compiler->fgNewBBafter(jumpKind, insertAfter, true, jumpDest);
            block->SetFlags(BBF_IMPORTED);
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
            compiler->fgRemoveStmt(currBlock, stmt);
        }

        //------------------------------------------------------------------------
        // SetWeights: set weights for new blocks.
        //
        virtual void SetWeights()
        {
            remainderBlock->inheritWeight(currBlock);
            checkBlock->inheritWeight(currBlock);
            thenBlock->inheritWeightPercentage(currBlock, likelihood);
            elseBlock->inheritWeightPercentage(currBlock, 100 - likelihood);
        }

        //------------------------------------------------------------------------
        // ChainFlow: link new blocks into correct cfg.
        //
        virtual void ChainFlow()
        {
            assert(compiler->fgPredsComputed);

            // currBlock
            compiler->fgRemoveRefPred(remainderBlock, currBlock);

            if (checkBlock != currBlock)
            {
                assert(currBlock->KindIs(BBJ_ALWAYS));
                FlowEdge* const newEdge = compiler->fgAddRefPred(checkBlock, currBlock);
                newEdge->setLikelihood(1.0);
                currBlock->SetTargetEdge(newEdge);
            }

            // checkBlock
            // Todo: get likelihoods right
            //
            assert(checkBlock->KindIs(BBJ_ALWAYS));
            checkBlock->SetCond(elseBlock, thenBlock);
            FlowEdge* const thenEdge = compiler->fgAddRefPred(thenBlock, checkBlock);
            thenEdge->setLikelihood(0.5);
            FlowEdge* const elseEdge = compiler->fgAddRefPred(elseBlock, checkBlock);
            elseEdge->setLikelihood(0.5);

            // thenBlock
            assert(thenBlock->TargetIs(remainderBlock));
            {
                FlowEdge* const newEdge = compiler->fgAddRefPred(remainderBlock, thenBlock);
                newEdge->setLikelihood(1.0);
            }

            // elseBlock
            {
                FlowEdge* const newEdge = compiler->fgAddRefPred(remainderBlock, elseBlock);
                newEdge->setLikelihood(1.0);
            }
        }

        Compiler*    compiler;
        BasicBlock*  currBlock;
        BasicBlock*  remainderBlock;
        BasicBlock*  checkBlock;
        BasicBlock*  thenBlock;
        BasicBlock*  elseBlock;
        Statement*   stmt;
        GenTreeCall* origCall;
        unsigned     likelihood;

        const int HIGH_PROBABILITY = 80;
    };

    class FatPointerCallTransformer final : public Transformer
    {
    public:
        FatPointerCallTransformer(Compiler* compiler, BasicBlock* block, Statement* stmt)
            : Transformer(compiler, block, stmt)
        {
            doesReturnValue = stmt->GetRootNode()->OperIs(GT_STORE_LCL_VAR);
            origCall        = GetCall(stmt);
            fptrAddress     = origCall->gtCallAddr;
            pointerType     = fptrAddress->TypeGet();
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
            if (doesReturnValue)
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
            origCall->ClearFatPointerCandidate();
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

            checkBlock = CreateAndInsertBasicBlock(BBJ_ALWAYS, currBlock, currBlock->Next());
            checkBlock->SetFlags(BBF_NONE_QUIRK);
            GenTree*   fatPointerMask  = new (compiler, GT_CNS_INT) GenTreeIntCon(TYP_I_IMPL, FAT_POINTER_MASK);
            GenTree*   fptrAddressCopy = compiler->gtCloneExpr(fptrAddress);
            GenTree*   fatPointerAnd   = compiler->gtNewOperNode(GT_AND, TYP_I_IMPL, fptrAddressCopy, fatPointerMask);
            GenTree*   zero            = new (compiler, GT_CNS_INT) GenTreeIntCon(TYP_I_IMPL, 0);
            GenTree*   fatPointerCmp   = compiler->gtNewOperNode(GT_NE, TYP_INT, fatPointerAnd, zero);
            GenTree*   jmpTree         = compiler->gtNewOperNode(GT_JTRUE, TYP_VOID, fatPointerCmp);
            Statement* jmpStmt         = compiler->fgNewStmtFromTree(jmpTree, stmt->GetDebugInfo());
            compiler->fgInsertStmtAtEnd(checkBlock, jmpStmt);
        }

        //------------------------------------------------------------------------
        // CreateThen: create then block, that is executed if the check succeeds.
        // This simply executes the original call.
        //
        virtual void CreateThen(uint8_t checkIdx)
        {
            assert(remainderBlock != nullptr);
            thenBlock                     = CreateAndInsertBasicBlock(BBJ_ALWAYS, checkBlock, remainderBlock);
            Statement* copyOfOriginalStmt = compiler->gtCloneStmt(stmt);
            compiler->fgInsertStmtAtEnd(thenBlock, copyOfOriginalStmt);
        }

        //------------------------------------------------------------------------
        // CreateElse: create else block, that is executed if call address is fat pointer.
        //
        virtual void CreateElse()
        {
            elseBlock = CreateAndInsertBasicBlock(BBJ_ALWAYS, thenBlock, thenBlock->Next());
            elseBlock->SetFlags(BBF_NONE_QUIRK);

            GenTree* fixedFptrAddress  = GetFixedFptrAddress();
            GenTree* actualCallAddress = compiler->gtNewIndir(pointerType, fixedFptrAddress);
            GenTree* hiddenArgument    = GetHiddenArgument(fixedFptrAddress);

            Statement* fatStmt = CreateFatCallStmt(actualCallAddress, hiddenArgument);
            compiler->fgInsertStmtAtEnd(elseBlock, fatStmt);
        }

        //------------------------------------------------------------------------
        // GetFixedFptrAddress: clear fat pointer bit from fat pointer address.
        //
        // Return Value:
        //    address without fat pointer bit set.
        GenTree* GetFixedFptrAddress()
        {
            GenTree* fptrAddressCopy = compiler->gtCloneExpr(fptrAddress);
            GenTree* fatPointerMask  = new (compiler, GT_CNS_INT) GenTreeIntCon(TYP_I_IMPL, FAT_POINTER_MASK);
            return compiler->gtNewOperNode(GT_SUB, pointerType, fptrAddressCopy, fatPointerMask);
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
            GenTree* fixedFptrAddressCopy = compiler->gtCloneExpr(fixedFptrAddress);
            GenTree* wordSize          = new (compiler, GT_CNS_INT) GenTreeIntCon(TYP_I_IMPL, genTypeSize(TYP_I_IMPL));
            GenTree* hiddenArgumentPtr = compiler->gtNewOperNode(GT_ADD, pointerType, fixedFptrAddressCopy, wordSize);
            return compiler->gtNewIndir(fixedFptrAddressCopy->TypeGet(), hiddenArgumentPtr);
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
            Statement*   fatStmt = compiler->gtCloneStmt(stmt);
            GenTreeCall* fatCall = GetCall(fatStmt);
            fatCall->gtCallAddr  = actualCallAddress;
            fatCall->gtArgs.InsertInstParam(compiler, hiddenArgument);
            return fatStmt;
        }

    private:
        const int FAT_POINTER_MASK = 0x2;

        GenTree*  fptrAddress;
        var_types pointerType;
        bool      doesReturnValue;
    };

    class GuardedDevirtualizationTransformer final : public Transformer
    {
    public:
        GuardedDevirtualizationTransformer(Compiler* compiler, BasicBlock* block, Statement* stmt)
            : Transformer(compiler, block, stmt), returnTemp(BAD_VAR_NUM)
        {
        }

        //------------------------------------------------------------------------
        // Run: transform the statement as described above.
        //
        virtual void Run()
        {
            origCall = GetCall(stmt);

            JITDUMP("\n----------------\n\n*** %s contemplating [%06u] in " FMT_BB " \n", Name(),
                    compiler->dspTreeID(origCall), currBlock->bbNum);

            // We currently need inline candidate info to guarded devirt.
            //
            if (!origCall->IsInlineCandidate())
            {
                JITDUMP("*** %s Bailing on [%06u] -- not an inline candidate\n", Name(), compiler->dspTreeID(origCall));
                ClearFlag();
                return;
            }

            likelihood = origCall->GetGDVCandidateInfo(0)->likelihood;
            assert((likelihood >= 0) && (likelihood <= 100));
            JITDUMP("Likelihood of correct guess is %u\n", likelihood);

            // TODO: implement chaining for multiple GDV candidates
            const bool canChainGdv =
                (GetChecksCount() == 1) && ((origCall->gtCallMoreFlags & GTF_CALL_M_GUARDED_DEVIRT_EXACT) == 0);
            if (canChainGdv)
            {
                const bool isChainedGdv = (origCall->gtCallMoreFlags & GTF_CALL_M_GUARDED_DEVIRT_CHAIN) != 0;

                if (isChainedGdv)
                {
                    JITDUMP("Expansion will chain to the previous GDV\n");
                }

                Transform();

                if (isChainedGdv)
                {
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
            return origCall->GetInlineCandidatesCount();
        }

        virtual void ChainFlow()
        {
            assert(compiler->fgPredsComputed);

            // currBlock
            compiler->fgRemoveRefPred(remainderBlock, currBlock);

            // The rest of chaining is done in-place.
        }

        virtual void SetWeights()
        {
            // remainderBlock has the same weight as the original block.
            remainderBlock->inheritWeight(currBlock);

            // The rest of the weights are assigned in-place.
        }

        //------------------------------------------------------------------------
        // CreateCheck: create check block and check method table
        //
        virtual void CreateCheck(uint8_t checkIdx)
        {
            if (checkIdx == 0)
            {
                // There's no need for a new block here. We can just append to currBlock.
                //
                checkBlock        = currBlock;
                checkFallsThrough = false;
            }
            else
            {
                // In case of multiple checks, append to the previous thenBlock block
                // (Set jump target of new checkBlock in CreateThen())
                BasicBlock* prevCheckBlock = checkBlock;
                checkBlock                 = CreateAndInsertBasicBlock(BBJ_ALWAYS, thenBlock);
                checkFallsThrough          = false;

                // Calculate the total likelihood for this check as a sum of likelihoods
                // of all previous candidates (thenBlocks)
                unsigned checkLikelihood = 100;
                for (uint8_t previousCandidate = 0; previousCandidate < checkIdx; previousCandidate++)
                {
                    checkLikelihood -= origCall->GetGDVCandidateInfo(previousCandidate)->likelihood;
                }

                // Make sure we didn't overflow
                assert(checkLikelihood <= 100);
                weight_t checkLikelihoodWt = ((weight_t)checkLikelihood) / 100.0;

                // prevCheckBlock is expected to jump to this new check (if its type check doesn't succeed)
                prevCheckBlock->SetCond(checkBlock, prevCheckBlock->Next());
                FlowEdge* const checkEdge = compiler->fgAddRefPred(checkBlock, prevCheckBlock);
                checkEdge->setLikelihood(checkLikelihoodWt);
                checkBlock->inheritWeightPercentage(currBlock, checkLikelihood);
            }

            // Find last arg with a side effect. All args with any effect
            // before that will need to be spilled.
            CallArg* lastSideEffArg = nullptr;
            for (CallArg& arg : origCall->gtArgs.Args())
            {
                if ((arg.GetNode()->gtFlags & GTF_SIDE_EFFECT) != 0)
                {
                    lastSideEffArg = &arg;
                }
            }

            if (lastSideEffArg != nullptr)
            {
                for (CallArg& arg : origCall->gtArgs.Args())
                {
                    GenTree* argNode = arg.GetNode();
                    if (((argNode->gtFlags & GTF_ALL_EFFECT) != 0) || compiler->gtHasLocalsWithAddrOp(argNode))
                    {
                        SpillArgToTempBeforeGuard(&arg);
                    }

                    if (&arg == lastSideEffArg)
                    {
                        break;
                    }
                }
            }

            CallArg* thisArg = origCall->gtArgs.GetThisArg();
            // We spill 'this' if it is complex, regardless of side effects. It
            // is going to be used multiple times due to the guard.
            if (!thisArg->GetNode()->IsLocal())
            {
                SpillArgToTempBeforeGuard(thisArg);
            }

            GenTree* thisTree = compiler->gtCloneExpr(thisArg->GetNode());

            // Remember the current last statement. If we're doing a chained GDV, we'll clone/copy
            // all the code in the check block up to and including this statement.
            //
            // Note it's important that we clone/copy the temp assign above, if we created one,
            // because flow along the "cold path" is going to bypass the check block.
            //
            lastStmt = checkBlock->lastStmt();

            // In case if GDV candidates are "exact" (e.g. we have the full list of classes implementing
            // the given interface in the app - NativeAOT only at this moment) we assume the last
            // check will always be true, so we just simplify the block to BBJ_ALWAYS
            const bool isLastCheck = (checkIdx == origCall->GetInlineCandidatesCount() - 1);
            if (isLastCheck && ((origCall->gtCallMoreFlags & GTF_CALL_M_GUARDED_DEVIRT_EXACT) != 0))
            {
                assert(checkBlock->KindIs(BBJ_ALWAYS));
                checkFallsThrough = true;
                return;
            }

            InlineCandidateInfo* guardedInfo = origCall->GetGDVCandidateInfo(checkIdx);

            // Create comparison. On success we will jump to do the indirect call.
            GenTree* compare;
            if (guardedInfo->guardedClassHandle != NO_CLASS_HANDLE)
            {
                // Find target method table
                //
                GenTree*             methodTable       = compiler->gtNewMethodTableLookup(thisTree);
                CORINFO_CLASS_HANDLE clsHnd            = guardedInfo->guardedClassHandle;
                GenTree*             targetMethodTable = compiler->gtNewIconEmbClsHndNode(clsHnd);

                compare = compiler->gtNewOperNode(GT_NE, TYP_INT, targetMethodTable, methodTable);
            }
            else
            {
                assert(origCall->IsVirtualVtable() || origCall->IsDelegateInvoke());
                // We reuse the target except if this is a chained GDV, in
                // which case the check will be moved into the success case of
                // a previous GDV and thus may not execute when we hit the cold
                // path.
                if (origCall->IsVirtualVtable())
                {
                    GenTree* tarTree = compiler->fgExpandVirtualVtableCallTarget(origCall);

                    CORINFO_METHOD_HANDLE methHnd = guardedInfo->guardedMethodHandle;
                    CORINFO_CONST_LOOKUP  lookup;
                    compiler->info.compCompHnd->getFunctionEntryPoint(methHnd, &lookup);

                    GenTree* compareTarTree = CreateTreeForLookup(methHnd, lookup);
                    compare                 = compiler->gtNewOperNode(GT_NE, TYP_INT, compareTarTree, tarTree);
                }
                else
                {
                    GenTree* offset =
                        compiler->gtNewIconNode((ssize_t)compiler->eeGetEEInfo()->offsetOfDelegateFirstTarget,
                                                TYP_I_IMPL);
                    GenTree* tarTree = compiler->gtNewOperNode(GT_ADD, TYP_BYREF, thisTree, offset);
                    tarTree          = compiler->gtNewIndir(TYP_I_IMPL, tarTree, GTF_IND_INVARIANT);

                    CORINFO_METHOD_HANDLE methHnd = guardedInfo->guardedMethodHandle;
                    CORINFO_CONST_LOOKUP  lookup;
                    compiler->info.compCompHnd->getFunctionFixedEntryPoint(methHnd, false, &lookup);

                    GenTree* compareTarTree = CreateTreeForLookup(methHnd, lookup);
                    compare                 = compiler->gtNewOperNode(GT_NE, TYP_INT, compareTarTree, tarTree);
                }
            }

            GenTree*   jmpTree = compiler->gtNewOperNode(GT_JTRUE, TYP_VOID, compare);
            Statement* jmpStmt = compiler->fgNewStmtFromTree(jmpTree, stmt->GetDebugInfo());
            compiler->fgInsertStmtAtEnd(checkBlock, jmpStmt);
        }

        //------------------------------------------------------------------------
        // SpillArgToTempBeforeGuard: spill an argument into a temp in the guard/check block.
        //
        // Parameters
        //   arg - The arg to create a temp and assignment for.
        //
        void SpillArgToTempBeforeGuard(CallArg* arg)
        {
            unsigned   tmpNum    = compiler->lvaGrabTemp(true DEBUGARG("guarded devirt arg temp"));
            GenTree*   store     = compiler->gtNewTempStore(tmpNum, arg->GetNode());
            Statement* storeStmt = compiler->fgNewStmtFromTree(store, stmt->GetDebugInfo());
            compiler->fgInsertStmtAtEnd(checkBlock, storeStmt);

            arg->SetEarlyNode(compiler->gtNewLclVarNode(tmpNum));
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
            InlineCandidateInfo* const inlineInfo = origCall->GetGDVCandidateInfo(0);

            if (!origCall->TypeIs(TYP_VOID))
            {
                // If there's a spill temp already associated with this inline candidate,
                // use that instead of allocating a new temp.
                //
                returnTemp = inlineInfo->preexistingSpillTemp;

                if (returnTemp != BAD_VAR_NUM)
                {
                    JITDUMP("Reworking call(s) to return value via a existing return temp V%02u\n", returnTemp);

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
                    LclVarDsc* const returnTempLcl = compiler->impInlineRoot()->lvaGetDesc(returnTemp);

                    if (returnTempLcl->lvSingleDef == 1)
                    {
                        // In this case it's ok if we already updated the type assuming single def,
                        // we just don't want any further updates.
                        //
                        JITDUMP("Return temp V%02u is no longer a single def temp\n", returnTemp);
                        returnTempLcl->lvSingleDef = 0;
                    }
                }
                else
                {
                    returnTemp = compiler->lvaGrabTemp(false DEBUGARG("guarded devirt return temp"));
                    JITDUMP("Reworking call(s) to return value via a new temp V%02u\n", returnTemp);
                }

                if (varTypeIsStruct(origCall))
                {
                    compiler->lvaSetStruct(returnTemp, origCall->gtRetClsHnd, false);
                }

                GenTree* tempTree = compiler->gtNewLclvNode(returnTemp, origCall->TypeGet());

                JITDUMP("Linking GT_RET_EXPR [%06u] to refer to temp V%02u\n", compiler->dspTreeID(inlineInfo->retExpr),
                        returnTemp);

                inlineInfo->retExpr->gtSubstExpr = tempTree;
            }
            else if (inlineInfo->retExpr != nullptr)
            {
                // We still oddly produce GT_RET_EXPRs for some void
                // returning calls. Just bash the ret expr to a NOP.
                //
                // Todo: consider bagging creation of these RET_EXPRs. The only possible
                // benefit they provide is stitching back larger trees for failed inlines
                // of void-returning methods. But then the calls likely sit in commas and
                // the benefit of a larger tree is unclear.
                JITDUMP("Linking GT_RET_EXPR [%06u] for VOID return to NOP\n",
                        compiler->dspTreeID(inlineInfo->retExpr));
                inlineInfo->retExpr->gtSubstExpr = compiler->gtNewNothingNode();
            }
            else
            {
                // We do not produce GT_RET_EXPRs for CTOR calls, so there is nothing to patch.
            }
        }

        //------------------------------------------------------------------------
        // Devirtualize origCall using the given inline candidate
        //
        void DevirtualizeCall(BasicBlock* block, uint8_t candidateId)
        {
            InlineCandidateInfo* inlineInfo = origCall->GetGDVCandidateInfo(candidateId);
            CORINFO_CLASS_HANDLE clsHnd     = inlineInfo->guardedClassHandle;

            //
            // Copy the 'this' for the devirtualized call to a new temp. For
            // class-based GDV this will allow us to set the exact type on that
            // temp. For delegate GDV, this will be the actual 'this' object
            // stored in the delegate.
            //
            const unsigned thisTemp  = compiler->lvaGrabTemp(false DEBUGARG("guarded devirt this exact temp"));
            GenTree*       clonedObj = compiler->gtCloneExpr(origCall->gtArgs.GetThisArg()->GetNode());
            GenTree*       newThisObj;
            if (origCall->IsDelegateInvoke())
            {
                GenTree* offset =
                    compiler->gtNewIconNode((ssize_t)compiler->eeGetEEInfo()->offsetOfDelegateInstance, TYP_I_IMPL);
                newThisObj = compiler->gtNewOperNode(GT_ADD, TYP_BYREF, clonedObj, offset);
                newThisObj = compiler->gtNewIndir(TYP_REF, newThisObj);
            }
            else
            {
                newThisObj = clonedObj;
            }
            GenTree* store = compiler->gtNewTempStore(thisTemp, newThisObj);

            if (clsHnd != NO_CLASS_HANDLE)
            {
                compiler->lvaSetClass(thisTemp, clsHnd, true);
            }
            else
            {
                compiler->lvaSetClass(thisTemp,
                                      compiler->info.compCompHnd->getMethodClass(inlineInfo->guardedMethodHandle));
            }

            compiler->fgNewStmtAtEnd(block, store);

            // Clone call for the devirtualized case. Note we must use the
            // special candidate helper and we need to use the new 'this'.
            GenTreeCall* call = compiler->gtCloneCandidateCall(origCall);
            call->gtArgs.GetThisArg()->SetEarlyNode(compiler->gtNewLclvNode(thisTemp, TYP_REF));

            INDEBUG(call->SetIsGuarded());

            JITDUMP("Direct call [%06u] in block " FMT_BB "\n", compiler->dspTreeID(call), block->bbNum);

            CORINFO_METHOD_HANDLE  methodHnd = inlineInfo->guardedMethodHandle;
            CORINFO_CONTEXT_HANDLE context   = inlineInfo->exactContextHnd;
            if (clsHnd != NO_CLASS_HANDLE)
            {
                // Then invoke impDevirtualizeCall to actually transform the call for us,
                // given the original (base) method and the exact guarded class. It should succeed.
                //
                unsigned   methodFlags            = compiler->info.compCompHnd->getMethodAttribs(methodHnd);
                const bool isLateDevirtualization = true;
                const bool explicitTailCall = (call->AsCall()->gtCallMoreFlags & GTF_CALL_M_EXPLICIT_TAILCALL) != 0;
                CORINFO_CONTEXT_HANDLE contextInput = context;
                compiler->impDevirtualizeCall(call, nullptr, &methodHnd, &methodFlags, &contextInput, &context,
                                              isLateDevirtualization, explicitTailCall);
            }
            else
            {
                // Otherwise we know the exact method already, so just change
                // the call as necessary here.
                call->gtFlags &= ~GTF_CALL_VIRT_KIND_MASK;
                call->gtCallMethHnd = methodHnd = inlineInfo->guardedMethodHandle;
                call->gtCallType                = CT_USER_FUNC;
                INDEBUG(call->gtCallDebugFlags |= GTF_CALL_MD_DEVIRTUALIZED);
                call->gtCallMoreFlags &= ~GTF_CALL_M_DELEGATE_INV;
                // TODO-GDV: To support R2R we need to get the entry point
                // here. We should unify with the tail of impDevirtualizeCall.

                if (origCall->IsVirtual())
                {
                    // Virtual calls include an implicit null check, which we may
                    // now need to make explicit.
                    bool isExact;
                    bool objIsNonNull;
                    compiler->gtGetClassHandle(newThisObj, &isExact, &objIsNonNull);

                    if (!objIsNonNull)
                    {
                        call->gtFlags |= GTF_CALL_NULLCHECK;
                    }
                }

                context = MAKE_METHODCONTEXT(methodHnd);
            }

            // We know this call can devirtualize or we would not have set up GDV here.
            // So above code should succeed in devirtualizing.
            //
            assert(!call->IsVirtual() && !call->IsDelegateInvoke());

            // If this call is in tail position, see if we've created a recursive tail call
            // candidate...
            //
            if (call->CanTailCall() && compiler->gtIsRecursiveCall(methodHnd))
            {
                compiler->setMethodHasRecursiveTailcall();
                block->SetFlags(BBF_RECURSIVE_TAILCALL);
                JITDUMP("[%06u] is a recursive call in tail position\n", compiler->dspTreeID(call));
            }
            else
            {
                JITDUMP("[%06u] is%s in tail position and is%s recursive\n", compiler->dspTreeID(call),
                        call->CanTailCall() ? "" : " not", compiler->gtIsRecursiveCall(methodHnd) ? "" : " not");
            }

            // If the devirtualizer was unable to transform the call to invoke the unboxed entry, the inline info
            // we set up may be invalid. We won't be able to inline anyways. So demote the call as an inline candidate.
            //
            CORINFO_METHOD_HANDLE unboxedMethodHnd = inlineInfo->guardedMethodUnboxedEntryHandle;
            if ((unboxedMethodHnd != nullptr) && (methodHnd != unboxedMethodHnd))
            {
                // Demote this call to a non-inline candidate
                //
                JITDUMP("Devirtualization was unable to use the unboxed entry; so marking call (to boxed entry) as not "
                        "inlineable\n");

                call->gtFlags &= ~GTF_CALL_INLINE_CANDIDATE;
                call->ClearInlineInfo();

                if (returnTemp != BAD_VAR_NUM)
                {
                    GenTree* const store = compiler->gtNewTempStore(returnTemp, call);
                    compiler->fgNewStmtAtEnd(block, store);
                }
                else
                {
                    compiler->fgNewStmtAtEnd(block, call, stmt->GetDebugInfo());
                }
            }
            else
            {
                // Add the call.
                //
                compiler->fgNewStmtAtEnd(block, call, stmt->GetDebugInfo());

                // Re-establish this call as an inline candidate.
                //
                GenTreeRetExpr* oldRetExpr       = inlineInfo->retExpr;
                inlineInfo->clsHandle            = compiler->info.compCompHnd->getMethodClass(methodHnd);
                inlineInfo->exactContextHnd      = context;
                inlineInfo->preexistingSpillTemp = returnTemp;
                call->SetSingleInlineCandidateInfo(inlineInfo);

                // If there was a ret expr for this call, we need to create a new one
                // and append it just after the call.
                //
                // Note the original GT_RET_EXPR has been linked to a temp.
                // we set all this up in FixupRetExpr().
                if (oldRetExpr != nullptr)
                {
                    inlineInfo->retExpr = compiler->gtNewInlineCandidateReturnExpr(call, call->TypeGet());

                    GenTree* newRetExpr = inlineInfo->retExpr;

                    if (returnTemp != BAD_VAR_NUM)
                    {
                        newRetExpr = compiler->gtNewTempStore(returnTemp, newRetExpr);
                    }
                    else
                    {
                        // We should always have a return temp if we return results by value
                        assert(origCall->TypeGet() == TYP_VOID);
                    }
                    compiler->fgNewStmtAtEnd(block, newRetExpr);
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
            unsigned const thenLikelihood   = origCall->GetGDVCandidateInfo(checkIdx)->likelihood;
            weight_t       thenLikelihoodWt = min(((weight_t)thenLikelihood) / 100.0, 100.0);
            weight_t       elseLikelihoodWt = max(1.0 - thenLikelihoodWt, 0.0);

            // thenBlock always jumps to remainderBlock
            thenBlock = CreateAndInsertBasicBlock(BBJ_ALWAYS, checkBlock, remainderBlock);
            thenBlock->CopyFlags(currBlock, BBF_SPLIT_GAINED);
            thenBlock->inheritWeightPercentage(currBlock, thenLikelihood);

            // Also, thenBlock has a single pred - last checkBlock
            assert(checkBlock->KindIs(BBJ_ALWAYS));
            FlowEdge* const thenEdge = compiler->fgAddRefPred(thenBlock, checkBlock);
            thenEdge->setLikelihood(thenLikelihoodWt);
            FlowEdge* const elseEdge = compiler->fgAddRefPred(remainderBlock, thenBlock);
            elseEdge->setLikelihood(elseLikelihoodWt);
            checkBlock->SetTargetEdge(thenEdge);
            checkBlock->SetFlags(BBF_NONE_QUIRK);
            assert(checkBlock->JumpsToNext());

            DevirtualizeCall(thenBlock, checkIdx);
        }

        //------------------------------------------------------------------------
        // CreateElse: create else block. This executes the original indirect call.
        //
        virtual void CreateElse()
        {
            // Calculate the likelihood of the else block as a remainder of the sum
            // of all the other likelihoods.
            unsigned elseLikelihood = 100;
            for (uint8_t i = 0; i < origCall->GetInlineCandidatesCount(); i++)
            {
                elseLikelihood -= origCall->GetGDVCandidateInfo(i)->likelihood;
            }
            // Make sure it didn't overflow
            assert(elseLikelihood <= 100);
            weight_t elseLikelihoodDbl = ((weight_t)elseLikelihood) / 100.0;

            elseBlock = CreateAndInsertBasicBlock(BBJ_ALWAYS, thenBlock, thenBlock->Next());
            elseBlock->CopyFlags(currBlock, BBF_SPLIT_GAINED);
            elseBlock->SetFlags(BBF_NONE_QUIRK);

            // CheckBlock flows into elseBlock unless we deal with the case
            // where we know the last check is always true (in case of "exact" GDV)
            if (!checkFallsThrough)
            {
                checkBlock->SetCond(elseBlock, checkBlock->Next());
                FlowEdge* const checkEdge = compiler->fgAddRefPred(elseBlock, checkBlock);
                checkEdge->setLikelihood(elseLikelihoodDbl);
            }
            else
            {
                // In theory, we could simplify the IR here, but since it's a rare case
                // and is NativeAOT-only, we just assume the unreached block will be removed
                // by other phases.
                assert(origCall->gtCallMoreFlags & GTF_CALL_M_GUARDED_DEVIRT_EXACT);
            }

            // elseBlock always flows into remainderBlock
            FlowEdge* const elseEdge = compiler->fgAddRefPred(remainderBlock, elseBlock);
            elseEdge->setLikelihood(1.0);

            // Remove everything related to inlining from the original call
            origCall->ClearInlineInfo();

            elseBlock->inheritWeightPercentage(currBlock, elseLikelihood);

            GenTreeCall* call    = origCall;
            Statement*   newStmt = compiler->gtNewStmt(call, stmt->GetDebugInfo());

            call->gtFlags &= ~GTF_CALL_INLINE_CANDIDATE;

            INDEBUG(call->SetIsGuarded());

            JITDUMP("Residual call [%06u] moved to block " FMT_BB "\n", compiler->dspTreeID(call), elseBlock->bbNum);

            if (returnTemp != BAD_VAR_NUM)
            {
                GenTree* store = compiler->gtNewTempStore(returnTemp, call);
                newStmt->SetRootNode(store);
            }

            compiler->fgInsertStmtAtEnd(elseBlock, newStmt);

            // Set the original statement to a nop.
            //
            stmt->SetRootNode(compiler->gtNewNothingNode());
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
            BasicBlock* const coldBlock = checkBlock->Prev();

            if (!coldBlock->KindIs(BBJ_ALWAYS) || !coldBlock->JumpsToNext())
            {
                JITDUMP("Unexpected flow from cold path " FMT_BB "\n", coldBlock->bbNum);
                return;
            }

            BasicBlock* const hotBlock = coldBlock->Prev();

            if (!hotBlock->KindIs(BBJ_ALWAYS) || !hotBlock->TargetIs(checkBlock))
            {
                JITDUMP("Unexpected flow from hot path " FMT_BB "\n", hotBlock->bbNum);
                return;
            }

            JITDUMP("Hot pred block is " FMT_BB " and cold pred block is " FMT_BB "\n", hotBlock->bbNum,
                    coldBlock->bbNum);

            // Clone and and copy the statements in the check block up to
            // and including lastStmt over to the hot block.
            //
            // This will be the "hot" copy of the code.
            //
            Statement* const afterLastStmt = lastStmt->GetNextStmt();

            for (Statement* checkStmt = checkBlock->firstStmt(); checkStmt != afterLastStmt;)
            {
                Statement* const nextStmt = checkStmt->GetNextStmt();

                // We should have ensured during scouting that all the statements
                // here can safely be cloned.
                //
                // Consider: allow inline candidates here, and keep them viable
                // in the hot copy, and demote them in the cold copy.
                //
                Statement* const clonedStmt = compiler->gtCloneStmt(checkStmt);
                compiler->fgInsertStmtAtEnd(hotBlock, clonedStmt);
                checkStmt = nextStmt;
            }

            // Now move the same span of statements to the cold block.
            //
            for (Statement* checkStmt = checkBlock->firstStmt(); checkStmt != afterLastStmt;)
            {
                Statement* const nextStmt = checkStmt->GetNextStmt();
                compiler->fgUnlinkStmt(checkBlock, checkStmt);
                compiler->fgInsertStmtAtEnd(coldBlock, checkStmt);
                checkStmt = nextStmt;
            }

            // Finally, rewire the cold block to jump to the else block,
            // not fall through to the check block.
            //
            FlowEdge* const oldEdge = compiler->fgRemoveRefPred(checkBlock, coldBlock);
            coldBlock->SetKindAndTarget(BBJ_ALWAYS, elseBlock);
            compiler->fgAddRefPred(elseBlock, coldBlock, oldEdge);
        }

        // When the current candidate hads sufficiently high likelihood, scan
        // the remainer block looking for another GDV candidate.
        //
        // (also consider: if currBlock has sufficiently high execution frequency)
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

            if (likelihood < gdvChainLikelihood)
            {
                return;
            }

            JITDUMP("Scouting for possible GDV chain as likelihood %u >= %u\n", likelihood, gdvChainLikelihood);

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
                    : GenTreeVisitor(compiler), m_unclonableNode(nullptr), m_nodeCount(0)
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

            for (Statement* const nextStmt : remainderBlock->Statements())
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
                                compiler->dspTreeID(call), call->GetGDVCandidateInfo(0)->likelihood, gdvChainLikelihood,
                                chainStatementDup, chainNodeDup);

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
                ClonabilityVisitor clonabilityVisitor(compiler);
                clonabilityVisitor.WalkTree(nextStmt->GetRootNodePointer(), nullptr);

                if (clonabilityVisitor.m_unclonableNode != nullptr)
                {
                    JITDUMP("  node [%06u] can't be cloned\n",
                            compiler->dspTreeID(clonabilityVisitor.m_unclonableNode));
                    break;
                }

                // Looks like we can clone this, so keep scouting.
                //
                chainStatementDup++;
                chainNodeDup += clonabilityVisitor.m_nodeCount;
            }
        }

    private:
        unsigned   returnTemp;
        Statement* lastStmt;
        bool       checkFallsThrough;

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
                    tree          = compiler->gtNewIndir(TYP_I_IMPL, tree, GTF_IND_NONFAULTING | GTF_IND_INVARIANT);
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
                    tree          = compiler->gtNewIndir(TYP_I_IMPL, tree, GTF_IND_NONFAULTING | GTF_IND_INVARIANT);
                    tree          = compiler->gtNewOperNode(GT_ADD, TYP_I_IMPL, tree, addr);
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
            GenTree* con = compiler->gtNewIconHandleNode((size_t)lookup.addr, GTF_ICON_FTN_ADDR);
            INDEBUG(con->AsIntCon()->gtTargetHandle = (size_t)methHnd);
            return con;
        }
    };

    Compiler* compiler;
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
