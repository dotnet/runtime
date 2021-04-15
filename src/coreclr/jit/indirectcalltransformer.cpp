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
// actually points to a tuple <method pointer, instantiation argument pointer> where
// instantiationArgument is a hidden first argument required by method pointer.
//
// Fat pointers are used in CoreRT as a replacement for instantiating stubs,
// because CoreRT can't generate stubs in runtime.
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
//   } BBJ_NONE check block
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
//   } BBJ_NONE remainder block
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

        for (BasicBlock* block = compiler->fgFirstBB; block != nullptr; block = block->bbNext)
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

        for (Statement* stmt : block->Statements())
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
            else if (compiler->doesMethodHaveExpRuntimeLookup() && ContainsExpRuntimeLookup(stmt))
            {
                ExpRuntimeLookupTransformer transformer(compiler, block, stmt);
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
        if (fatPointerCandidate->OperIs(GT_ASG))
        {
            fatPointerCandidate = fatPointerCandidate->gtGetOp2();
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

    //------------------------------------------------------------------------
    // ContainsExpRuntimeLookup: check if this statement contains a dictionary
    // with dynamic dictionary expansion that we want to transform in CFG.
    //
    // Return Value:
    //    true if contains, false otherwise.
    //
    bool ContainsExpRuntimeLookup(Statement* stmt)
    {
        GenTree* candidate = stmt->GetRootNode();
        if (candidate->OperIs(GT_ASG))
        {
            candidate = candidate->gtGetOp2();
        }
        if (candidate->OperIs(GT_CALL))
        {
            GenTreeCall* call = candidate->AsCall();
            return call->IsExpRuntimeLookup();
        }
        return false;
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
            CreateCheck();
            CreateThen();
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
            remainderBlock->bbFlags |= BBF_INTERNAL;
        }

        virtual void CreateCheck() = 0;

        //------------------------------------------------------------------------
        // CreateAndInsertBasicBlock: ask compiler to create new basic block.
        // and insert in into the basic block list.
        //
        // Arguments:
        //    jumpKind - jump kind for the new basic block
        //    insertAfter - basic block, after which compiler has to insert the new one.
        //
        // Return Value:
        //    new basic block.
        BasicBlock* CreateAndInsertBasicBlock(BBjumpKinds jumpKind, BasicBlock* insertAfter)
        {
            BasicBlock* block = compiler->fgNewBBafter(jumpKind, insertAfter, true);
            block->bbFlags |= BBF_IMPORTED;
            return block;
        }

        virtual void CreateThen() = 0;
        virtual void CreateElse() = 0;

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
            assert(!compiler->fgComputePredsDone);
            checkBlock->bbJumpDest = elseBlock;
            thenBlock->bbJumpDest  = remainderBlock;
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
            doesReturnValue = stmt->GetRootNode()->OperIs(GT_ASG);
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
                assert(tree->OperIs(GT_ASG));
                call = tree->gtGetOp2()->AsCall();
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
        virtual void CreateCheck()
        {
            checkBlock                 = CreateAndInsertBasicBlock(BBJ_COND, currBlock);
            GenTree*   fatPointerMask  = new (compiler, GT_CNS_INT) GenTreeIntCon(TYP_I_IMPL, FAT_POINTER_MASK);
            GenTree*   fptrAddressCopy = compiler->gtCloneExpr(fptrAddress);
            GenTree*   fatPointerAnd   = compiler->gtNewOperNode(GT_AND, TYP_I_IMPL, fptrAddressCopy, fatPointerMask);
            GenTree*   zero            = new (compiler, GT_CNS_INT) GenTreeIntCon(TYP_I_IMPL, 0);
            GenTree*   fatPointerCmp   = compiler->gtNewOperNode(GT_NE, TYP_INT, fatPointerAnd, zero);
            GenTree*   jmpTree         = compiler->gtNewOperNode(GT_JTRUE, TYP_VOID, fatPointerCmp);
            Statement* jmpStmt         = compiler->fgNewStmtFromTree(jmpTree, stmt->GetILOffsetX());
            compiler->fgInsertStmtAtEnd(checkBlock, jmpStmt);
        }

        //------------------------------------------------------------------------
        // CreateThen: create then block, that is executed if the check succeeds.
        // This simply executes the original call.
        //
        virtual void CreateThen()
        {
            thenBlock                     = CreateAndInsertBasicBlock(BBJ_ALWAYS, checkBlock);
            Statement* copyOfOriginalStmt = compiler->gtCloneStmt(stmt);
            compiler->fgInsertStmtAtEnd(thenBlock, copyOfOriginalStmt);
        }

        //------------------------------------------------------------------------
        // CreateElse: create else block, that is executed if call address is fat pointer.
        //
        virtual void CreateElse()
        {
            elseBlock = CreateAndInsertBasicBlock(BBJ_NONE, thenBlock);

            GenTree* fixedFptrAddress  = GetFixedFptrAddress();
            GenTree* actualCallAddress = compiler->gtNewOperNode(GT_IND, pointerType, fixedFptrAddress);
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
        //    fixedFptrAddress - pointer to the tuple <methodPointer, instantiationArgumentPointer>
        //
        // Return Value:
        //    generic context hidden argument.
        GenTree* GetHiddenArgument(GenTree* fixedFptrAddress)
        {
            GenTree* fixedFptrAddressCopy = compiler->gtCloneExpr(fixedFptrAddress);
            GenTree* wordSize = new (compiler, GT_CNS_INT) GenTreeIntCon(TYP_I_IMPL, genTypeSize(TYP_I_IMPL));
            GenTree* hiddenArgumentPtrPtr =
                compiler->gtNewOperNode(GT_ADD, pointerType, fixedFptrAddressCopy, wordSize);
            GenTree* hiddenArgumentPtr = compiler->gtNewOperNode(GT_IND, pointerType, hiddenArgumentPtrPtr);
            return compiler->gtNewOperNode(GT_IND, fixedFptrAddressCopy->TypeGet(), hiddenArgumentPtr);
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
            AddHiddenArgument(fatCall, hiddenArgument);
            return fatStmt;
        }

        //------------------------------------------------------------------------
        // AddHiddenArgument: add hidden argument to the call argument list.
        //
        // Arguments:
        //    fatCall - fat call node
        //    hiddenArgument - generic context hidden argument
        //
        void AddHiddenArgument(GenTreeCall* fatCall, GenTree* hiddenArgument)
        {
#if USER_ARGS_COME_LAST
            if (fatCall->HasRetBufArg())
            {
                GenTreeCall::Use* retBufArg = fatCall->gtCallArgs;
                compiler->gtInsertNewCallArgAfter(hiddenArgument, retBufArg);
            }
            else
            {
                fatCall->gtCallArgs = compiler->gtPrependNewCallArg(hiddenArgument, fatCall->gtCallArgs);
            }
#else
            if (fatCall->gtCallArgs == nullptr)
            {
                fatCall->gtCallArgs = compiler->gtNewCallArgs(hiddenArgument);
            }
            else
            {
                AddArgumentToTail(fatCall->gtCallArgs, hiddenArgument);
            }
#endif
        }

        //------------------------------------------------------------------------
        // AddArgumentToTail: add hidden argument to the tail of the call argument list.
        //
        // Arguments:
        //    argList - fat call node
        //    hiddenArgument - generic context hidden argument
        //
        void AddArgumentToTail(GenTreeCall::Use* argList, GenTree* hiddenArgument)
        {
            GenTreeCall::Use* iterator = argList;
            while (iterator->GetNext() != nullptr)
            {
                iterator = iterator->GetNext();
            }
            iterator->SetNext(compiler->gtNewCallArgs(hiddenArgument));
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

            JITDUMP("*** %s contemplating [%06u]\n", Name(), compiler->dspTreeID(origCall));

            // We currently need inline candidate info to guarded devirt.
            if (!origCall->IsInlineCandidate())
            {
                JITDUMP("*** %s Bailing on [%06u] -- not an inline candidate\n", Name(), compiler->dspTreeID(origCall));
                ClearFlag();
                return;
            }

            likelihood = origCall->gtGuardedDevirtualizationCandidateInfo->likelihood;
            assert((likelihood >= 0) && (likelihood <= 100));
            JITDUMP("Likelihood of correct guess is %u\n", likelihood);

            Transform();
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

        //------------------------------------------------------------------------
        // ClearFlag: clear guarded devirtualization candidate flag from the original call.
        //
        virtual void ClearFlag()
        {
            origCall->ClearGuardedDevirtualizationCandidate();
        }

        //------------------------------------------------------------------------
        // CreateCheck: create check block and check method table
        //
        virtual void CreateCheck()
        {
            checkBlock = CreateAndInsertBasicBlock(BBJ_COND, currBlock);

            // Fetch method table from object arg to call.
            GenTree* thisTree = compiler->gtCloneExpr(origCall->gtCallThisArg->GetNode());

            // Create temp for this if the tree is costly.
            if (!thisTree->IsLocal())
            {
                const unsigned thisTempNum = compiler->lvaGrabTemp(true DEBUGARG("guarded devirt this temp"));
                // lvaSetClass(thisTempNum, ...);
                GenTree*   asgTree = compiler->gtNewTempAssign(thisTempNum, thisTree);
                Statement* asgStmt = compiler->fgNewStmtFromTree(asgTree, stmt->GetILOffsetX());
                compiler->fgInsertStmtAtEnd(checkBlock, asgStmt);

                thisTree = compiler->gtNewLclvNode(thisTempNum, TYP_REF);

                // Propagate the new this to the call. Must be a new expr as the call
                // will live on in the else block and thisTree is used below.
                origCall->gtCallThisArg = compiler->gtNewCallArgs(compiler->gtNewLclvNode(thisTempNum, TYP_REF));
            }

            GenTree* methodTable = compiler->gtNewMethodTableLookup(thisTree);

            // Find target method table
            GuardedDevirtualizationCandidateInfo* guardedInfo       = origCall->gtGuardedDevirtualizationCandidateInfo;
            CORINFO_CLASS_HANDLE                  clsHnd            = guardedInfo->guardedClassHandle;
            GenTree*                              targetMethodTable = compiler->gtNewIconEmbClsHndNode(clsHnd);

            // Compare and jump to else (which does the indirect call) if NOT equal
            GenTree*   methodTableCompare = compiler->gtNewOperNode(GT_NE, TYP_INT, targetMethodTable, methodTable);
            GenTree*   jmpTree            = compiler->gtNewOperNode(GT_JTRUE, TYP_VOID, methodTableCompare);
            Statement* jmpStmt            = compiler->fgNewStmtFromTree(jmpTree, stmt->GetILOffsetX());
            compiler->fgInsertStmtAtEnd(checkBlock, jmpStmt);
        }

        //------------------------------------------------------------------------
        // FixupRetExpr: set up to repair return value placeholder from call
        //
        virtual void FixupRetExpr()
        {
            // If call returns a value, we need to copy it to a temp, and
            // update the associated GT_RET_EXPR to refer to the temp instead
            // of the call.
            //
            // Note implicit by-ref returns should have already been converted
            // so any struct copy we induce here should be cheap.
            //
            // Todo: make sure we understand how this interacts with return type
            // munging for small structs.
            InlineCandidateInfo* inlineInfo = origCall->gtInlineCandidateInfo;
            GenTree*             retExpr    = inlineInfo->retExpr;

            // Sanity check the ret expr if non-null: it should refer to the original call.
            if (retExpr != nullptr)
            {
                assert(retExpr->AsRetExpr()->gtInlineCandidate == origCall);
            }

            if (origCall->TypeGet() != TYP_VOID)
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

                JITDUMP("Updating GT_RET_EXPR [%06u] to refer to temp V%02u\n", compiler->dspTreeID(retExpr),
                        returnTemp);
                retExpr->AsRetExpr()->gtInlineCandidate = tempTree;
            }
            else if (retExpr != nullptr)
            {
                // We still oddly produce GT_RET_EXPRs for some void
                // returning calls. Just patch the ret expr to a NOP.
                //
                // Todo: consider bagging creation of these RET_EXPRs. The only possible
                // benefit they provide is stitching back larger trees for failed inlines
                // of void-returning methods. But then the calls likely sit in commas and
                // the benefit of a larger tree is unclear.
                JITDUMP("Updating GT_RET_EXPR [%06u] for VOID return to refer to a NOP\n",
                        compiler->dspTreeID(retExpr));
                GenTree* nopTree                        = compiler->gtNewNothingNode();
                retExpr->AsRetExpr()->gtInlineCandidate = nopTree;
            }
            else
            {
                // We do not produce GT_RET_EXPRs for CTOR calls, so there is nothing to patch.
            }
        }

        //------------------------------------------------------------------------
        // CreateThen: create then block with direct call to method
        //
        virtual void CreateThen()
        {
            thenBlock = CreateAndInsertBasicBlock(BBJ_ALWAYS, checkBlock);
            thenBlock->bbFlags |= currBlock->bbFlags & BBF_SPLIT_GAINED;

            InlineCandidateInfo* inlineInfo = origCall->gtInlineCandidateInfo;
            CORINFO_CLASS_HANDLE clsHnd     = inlineInfo->guardedClassHandle;

            // copy 'this' to temp with exact type.
            const unsigned thisTemp  = compiler->lvaGrabTemp(false DEBUGARG("guarded devirt this exact temp"));
            GenTree*       clonedObj = compiler->gtCloneExpr(origCall->gtCallThisArg->GetNode());
            GenTree*       assign    = compiler->gtNewTempAssign(thisTemp, clonedObj);
            compiler->lvaSetClass(thisTemp, clsHnd, true);
            compiler->fgNewStmtAtEnd(thenBlock, assign);

            // Clone call. Note we must use the special candidate helper.
            GenTreeCall* call   = compiler->gtCloneCandidateCall(origCall);
            call->gtCallThisArg = compiler->gtNewCallArgs(compiler->gtNewLclvNode(thisTemp, TYP_REF));
            call->SetIsGuarded();

            JITDUMP("Direct call [%06u] in block " FMT_BB "\n", compiler->dspTreeID(call), thenBlock->bbNum);

            // Then invoke impDevirtualizeCall to actually
            // transform the call for us. It should succeed.... as we have
            // now provided an exact typed this.
            CORINFO_METHOD_HANDLE  methodHnd              = inlineInfo->methInfo.ftn;
            unsigned               methodFlags            = inlineInfo->methAttr;
            CORINFO_CONTEXT_HANDLE context                = inlineInfo->exactContextHnd;
            const bool             isLateDevirtualization = true;
            bool explicitTailCall = (call->AsCall()->gtCallMoreFlags & GTF_CALL_M_EXPLICIT_TAILCALL) != 0;
            compiler->impDevirtualizeCall(call, &methodHnd, &methodFlags, &context, nullptr, isLateDevirtualization,
                                          explicitTailCall);

            // Presumably devirt might fail? If so we should try and avoid
            // making this a guarded devirt candidate instead of ending
            // up here.
            assert(!call->IsVirtual());

            // Re-establish this call as an inline candidate.
            //
            GenTree* oldRetExpr              = inlineInfo->retExpr;
            inlineInfo->clsHandle            = compiler->info.compCompHnd->getMethodClass(methodHnd);
            inlineInfo->exactContextHnd      = context;
            inlineInfo->preexistingSpillTemp = returnTemp;
            call->gtInlineCandidateInfo      = inlineInfo;

            // Add the call.
            compiler->fgNewStmtAtEnd(thenBlock, call);

            // If there was a ret expr for this call, we need to create a new one
            // and append it just after the call.
            //
            // Note the original GT_RET_EXPR is sitting at the join point of the
            // guarded expansion and for non-void calls, and now refers to a temp local;
            // we set all this up in FixupRetExpr().
            if (oldRetExpr != nullptr)
            {
                GenTree* retExpr = compiler->gtNewInlineCandidateReturnExpr(call, call->TypeGet(), thenBlock->bbFlags);
                inlineInfo->retExpr = retExpr;

                if (returnTemp != BAD_VAR_NUM)
                {
                    retExpr = compiler->gtNewTempAssign(returnTemp, retExpr);
                }
                else
                {
                    // We should always have a return temp if we return results by value
                    assert(origCall->TypeGet() == TYP_VOID);
                }
                compiler->fgNewStmtAtEnd(thenBlock, retExpr);
            }
        }

        //------------------------------------------------------------------------
        // CreateElse: create else block. This executes the unaltered indirect call.
        //
        virtual void CreateElse()
        {
            elseBlock = CreateAndInsertBasicBlock(BBJ_NONE, thenBlock);
            elseBlock->bbFlags |= currBlock->bbFlags & BBF_SPLIT_GAINED;
            GenTreeCall* call    = origCall;
            Statement*   newStmt = compiler->gtNewStmt(call);

            call->gtFlags &= ~GTF_CALL_INLINE_CANDIDATE;
            call->SetIsGuarded();

            JITDUMP("Residual call [%06u] moved to block " FMT_BB "\n", compiler->dspTreeID(call), elseBlock->bbNum);

            if (returnTemp != BAD_VAR_NUM)
            {
                GenTree* assign = compiler->gtNewTempAssign(returnTemp, call);
                newStmt->SetRootNode(assign);
            }

            // For stub calls, restore the stub address. For everything else,
            // null out the candidate info field.
            if (call->IsVirtualStub())
            {
                JITDUMP("Restoring stub addr %p from candidate info\n", call->gtInlineCandidateInfo->stubAddr);
                call->gtStubCallStubAddr = call->gtInlineCandidateInfo->stubAddr;
            }
            else
            {
                call->gtInlineCandidateInfo = nullptr;
            }

            compiler->fgInsertStmtAtEnd(elseBlock, newStmt);

            // Set the original statement to a nop.
            stmt->SetRootNode(compiler->gtNewNothingNode());
        }

    private:
        unsigned returnTemp;
    };

    // Runtime lookup with dynamic dictionary expansion transformer,
    // it expects helper runtime lookup call with additional arguments that are:
    // result handle, nullCheck tree, sizeCheck tree.
    // before:
    //   current block
    //   {
    //     previous statements
    //     transforming statement
    //     {
    //       ASG lclVar, call with GTF_CALL_M_EXP_RUNTIME_LOOKUP flag set and additional arguments.
    //     }
    //     subsequent statements
    //   }
    //
    // after:
    //   current block
    //   {
    //     previous statements
    //   } BBJ_NONE check block
    //   check block
    //   {
    //     jump to else if the handle fails size check
    //   } BBJ_COND check block2, else block
    //   check block2
    //   {
    //     jump to else if the handle fails null check
    //   } BBJ_COND then block, else block
    //   then block
    //   {
    //     return handle
    //   } BBJ_ALWAYS remainder block
    //   else block
    //   {
    //     do a helper call
    //   } BBJ_NONE remainder block
    //   remainder block
    //   {
    //     subsequent statements
    //   }
    //
    class ExpRuntimeLookupTransformer final : public Transformer
    {
    public:
        ExpRuntimeLookupTransformer(Compiler* compiler, BasicBlock* block, Statement* stmt)
            : Transformer(compiler, block, stmt)
        {
            GenTreeOp* asg = stmt->GetRootNode()->AsOp();
            resultLclNum   = asg->gtOp1->AsLclVar()->GetLclNum();
            origCall       = GetCall(stmt);
            checkBlock2    = nullptr;
        }

    protected:
        virtual const char* Name() override
        {
            return "ExpRuntimeLookup";
        }

        //------------------------------------------------------------------------
        // GetCall: find a call in a statement.
        //
        // Arguments:
        //    callStmt - the statement with the call inside.
        //
        // Return Value:
        //    call tree node pointer.
        virtual GenTreeCall* GetCall(Statement* callStmt) override
        {
            GenTree* tree = callStmt->GetRootNode();
            assert(tree->OperIs(GT_ASG));
            GenTreeCall* call = tree->gtGetOp2()->AsCall();
            return call;
        }

        //------------------------------------------------------------------------
        // ClearFlag: clear runtime exp lookup flag from the original call.
        //
        virtual void ClearFlag() override
        {
            origCall->ClearExpRuntimeLookup();
        }

        // FixupRetExpr: no action needed.
        virtual void FixupRetExpr() override
        {
        }

        //------------------------------------------------------------------------
        // CreateCheck: create check blocks, that checks dictionary size and does null test.
        //
        virtual void CreateCheck() override
        {
            GenTreeCall::UseIterator argsIter  = origCall->Args().begin();
            GenTree*                 nullCheck = argsIter.GetUse()->GetNode();
            ++argsIter;
            GenTree* sizeCheck = argsIter.GetUse()->GetNode();
            ++argsIter;
            origCall->gtCallArgs = argsIter.GetUse();
            // The first argument is the handle now.
            checkBlock = CreateAndInsertBasicBlock(BBJ_COND, currBlock);

            assert(sizeCheck->OperIs(GT_LE));
            GenTree*   sizeJmpTree = compiler->gtNewOperNode(GT_JTRUE, TYP_VOID, sizeCheck);
            Statement* sizeJmpStmt = compiler->fgNewStmtFromTree(sizeJmpTree, stmt->GetILOffsetX());
            compiler->fgInsertStmtAtEnd(checkBlock, sizeJmpStmt);

            checkBlock2 = CreateAndInsertBasicBlock(BBJ_COND, checkBlock);
            assert(nullCheck->OperIs(GT_EQ));
            GenTree*   nullJmpTree = compiler->gtNewOperNode(GT_JTRUE, TYP_VOID, nullCheck);
            Statement* nullJmpStmt = compiler->fgNewStmtFromTree(nullJmpTree, stmt->GetILOffsetX());
            compiler->fgInsertStmtAtEnd(checkBlock2, nullJmpStmt);
        }

        //------------------------------------------------------------------------
        // CreateThen: create then block, that is executed if the checks succeed.
        // This simply returns the handle.
        //
        virtual void CreateThen() override
        {
            thenBlock = CreateAndInsertBasicBlock(BBJ_ALWAYS, checkBlock2);

            GenTreeCall::UseIterator argsIter     = origCall->Args().begin();
            GenTree*                 resultHandle = argsIter.GetUse()->GetNode();
            ++argsIter;
            // The first argument is the real first argument for the call now.
            origCall->gtCallArgs = argsIter.GetUse();

            GenTree*   asg     = compiler->gtNewTempAssign(resultLclNum, resultHandle);
            Statement* asgStmt = compiler->gtNewStmt(asg, stmt->GetILOffsetX());
            compiler->fgInsertStmtAtEnd(thenBlock, asgStmt);
        }

        //------------------------------------------------------------------------
        // CreateElse: create else block, that is executed if the checks fail.
        //
        virtual void CreateElse() override
        {
            elseBlock          = CreateAndInsertBasicBlock(BBJ_NONE, thenBlock);
            GenTree*   asg     = compiler->gtNewTempAssign(resultLclNum, origCall);
            Statement* asgStmt = compiler->gtNewStmt(asg, stmt->GetILOffsetX());
            compiler->fgInsertStmtAtEnd(elseBlock, asgStmt);
        }

        //------------------------------------------------------------------------
        // SetWeights: set weights for new blocks.
        //
        virtual void SetWeights() override
        {
            remainderBlock->inheritWeight(currBlock);
            checkBlock->inheritWeight(currBlock);
            checkBlock2->inheritWeightPercentage(checkBlock, HIGH_PROBABILITY);
            thenBlock->inheritWeightPercentage(currBlock, HIGH_PROBABILITY);
            elseBlock->inheritWeightPercentage(currBlock, 100 - HIGH_PROBABILITY);
        }

        //------------------------------------------------------------------------
        // ChainFlow: link new blocks into correct cfg.
        //
        virtual void ChainFlow() override
        {
            assert(!compiler->fgComputePredsDone);
            checkBlock->bbJumpDest  = elseBlock;
            checkBlock2->bbJumpDest = elseBlock;
            thenBlock->bbJumpDest   = remainderBlock;
        }

    private:
        BasicBlock* checkBlock2;
        unsigned    resultLclNum;
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
        assert(!call->IsExpRuntimeLookup());
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
    assert(!doesMethodHaveGuardedDevirtualization());
    assert(!doesMethodHaveExpRuntimeLookup());

    for (BasicBlock* block = fgFirstBB; block != nullptr; block = block->bbNext)
    {
        for (Statement* stmt : block->Statements())
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
    if (doesMethodHaveFatPointer() || doesMethodHaveGuardedDevirtualization() || doesMethodHaveExpRuntimeLookup())
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
        clearMethodHasGuardedDevirtualization();
        clearMethodHasExpRuntimeLookup();
    }
    else
    {
        JITDUMP("\n -- no candidates to transform\n");
    }

    INDEBUG(CheckNoTransformableIndirectCallsRemain(););

    return (count == 0) ? PhaseStatus::MODIFIED_NOTHING : PhaseStatus::MODIFIED_EVERYTHING;
}
