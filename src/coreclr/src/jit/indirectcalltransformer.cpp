// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "jitpch.h"
#ifdef _MSC_VER
#pragma hdrstop
#endif

// The IndirectCallTransformer transforms indirect calls that involve fat function
// pointers or guarded devirtualization candidates. These transformations introduce
// control flow and so can't easily be done in the importer.
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

        for (GenTreeStmt* stmt = block->firstStmt(); stmt != nullptr; stmt = stmt->gtNextStmt)
        {
            if (ContainsFatCalli(stmt))
            {
                FatPointerCallTransformer transformer(compiler, block, stmt);
                transformer.Run();
                count++;
            }

            if (ContainsGuardedDevirtualizationCandidate(stmt))
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
    bool ContainsFatCalli(GenTreeStmt* stmt)
    {
        GenTree* fatPointerCandidate = stmt->gtStmtExpr;
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
    bool ContainsGuardedDevirtualizationCandidate(GenTreeStmt* stmt)
    {
        GenTree* candidate = stmt->gtStmtExpr;
        return candidate->IsCall() && candidate->AsCall()->IsGuardedDevirtualizationCandidate();
    }

    class Transformer
    {
    public:
        Transformer(Compiler* compiler, BasicBlock* block, GenTreeStmt* stmt)
            : compiler(compiler), currBlock(block), stmt(stmt)
        {
            remainderBlock = nullptr;
            checkBlock     = nullptr;
            thenBlock      = nullptr;
            elseBlock      = nullptr;
            origCall       = nullptr;
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
            JITDUMP("*** %s: transforming [%06u]\n", Name(), compiler->dspTreeID(stmt));
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
        virtual const char*  Name()                         = 0;
        virtual void         ClearFlag()                    = 0;
        virtual GenTreeCall* GetCall(GenTreeStmt* callStmt) = 0;
        virtual void FixupRetExpr()                         = 0;

        //------------------------------------------------------------------------
        // CreateRemainder: split current block at the call stmt and
        // insert statements after the call into remainderBlock.
        //
        void CreateRemainder()
        {
            remainderBlock          = compiler->fgSplitBlockAfterStatement(currBlock, stmt);
            unsigned propagateFlags = currBlock->bbFlags & BBF_GC_SAFE_POINT;
            remainderBlock->bbFlags |= BBF_JMP_TARGET | BBF_HAS_LABEL | propagateFlags;
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
            if ((insertAfter->bbFlags & BBF_INTERNAL) == 0)
            {
                block->bbFlags &= ~BBF_INTERNAL;
                block->bbFlags |= BBF_IMPORTED;
            }
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
        void SetWeights()
        {
            remainderBlock->inheritWeight(currBlock);
            checkBlock->inheritWeight(currBlock);
            thenBlock->inheritWeightPercentage(currBlock, HIGH_PROBABILITY);
            elseBlock->inheritWeightPercentage(currBlock, 100 - HIGH_PROBABILITY);
        }

        //------------------------------------------------------------------------
        // ChainFlow: link new blocks into correct cfg.
        //
        void ChainFlow()
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
        GenTreeStmt* stmt;
        GenTreeCall* origCall;

        const int HIGH_PROBABILITY = 80;
    };

    class FatPointerCallTransformer final : public Transformer
    {
    public:
        FatPointerCallTransformer(Compiler* compiler, BasicBlock* block, GenTreeStmt* stmt)
            : Transformer(compiler, block, stmt)
        {
            doesReturnValue = stmt->gtStmtExpr->OperIs(GT_ASG);
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
        virtual GenTreeCall* GetCall(GenTreeStmt* callStmt)
        {
            GenTree*     tree = callStmt->gtStmtExpr;
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
            checkBlock                   = CreateAndInsertBasicBlock(BBJ_COND, currBlock);
            GenTree*     fatPointerMask  = new (compiler, GT_CNS_INT) GenTreeIntCon(TYP_I_IMPL, FAT_POINTER_MASK);
            GenTree*     fptrAddressCopy = compiler->gtCloneExpr(fptrAddress);
            GenTree*     fatPointerAnd   = compiler->gtNewOperNode(GT_AND, TYP_I_IMPL, fptrAddressCopy, fatPointerMask);
            GenTree*     zero            = new (compiler, GT_CNS_INT) GenTreeIntCon(TYP_I_IMPL, 0);
            GenTree*     fatPointerCmp   = compiler->gtNewOperNode(GT_NE, TYP_INT, fatPointerAnd, zero);
            GenTree*     jmpTree         = compiler->gtNewOperNode(GT_JTRUE, TYP_VOID, fatPointerCmp);
            GenTreeStmt* jmpStmt         = compiler->fgNewStmtFromTree(jmpTree, stmt->gtStmtILoffsx);
            compiler->fgInsertStmtAtEnd(checkBlock, jmpStmt);
        }

        //------------------------------------------------------------------------
        // CreateThen: create then block, that is executed if the check succeeds.
        // This simply executes the original call.
        //
        virtual void CreateThen()
        {
            thenBlock                       = CreateAndInsertBasicBlock(BBJ_ALWAYS, checkBlock);
            GenTreeStmt* copyOfOriginalStmt = compiler->gtCloneExpr(stmt)->AsStmt();
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

            GenTreeStmt* fatStmt = CreateFatCallStmt(actualCallAddress, hiddenArgument);
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
        GenTreeStmt* CreateFatCallStmt(GenTree* actualCallAddress, GenTree* hiddenArgument)
        {
            GenTreeStmt* fatStmt = compiler->gtCloneExpr(stmt)->AsStmt();
            GenTree*     fatTree = fatStmt->gtStmtExpr;
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
            GenTreeArgList* oldArgs = fatCall->gtCallArgs;
            GenTreeArgList* newArgs;
#if USER_ARGS_COME_LAST
            if (fatCall->HasRetBufArg())
            {
                GenTree*        retBuffer = oldArgs->Current();
                GenTreeArgList* rest      = oldArgs->Rest();
                newArgs                   = compiler->gtNewListNode(hiddenArgument, rest);
                newArgs                   = compiler->gtNewListNode(retBuffer, newArgs);
            }
            else
            {
                newArgs = compiler->gtNewListNode(hiddenArgument, oldArgs);
            }
#else
            newArgs = oldArgs;
            AddArgumentToTail(newArgs, hiddenArgument);
#endif
            fatCall->gtCallArgs = newArgs;
        }

        //------------------------------------------------------------------------
        // AddArgumentToTail: add hidden argument to the tail of the call argument list.
        //
        // Arguments:
        //    argList - fat call node
        //    hiddenArgument - generic context hidden argument
        //
        void AddArgumentToTail(GenTreeArgList* argList, GenTree* hiddenArgument)
        {
            GenTreeArgList* iterator = argList;
            while (iterator->Rest() != nullptr)
            {
                iterator = iterator->Rest();
            }
            iterator->Rest() = compiler->gtNewArgList(hiddenArgument);
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
        GuardedDevirtualizationTransformer(Compiler* compiler, BasicBlock* block, GenTreeStmt* stmt)
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

            // For now, bail on transforming calls that still appear
            // to return structs by value as there is deferred work
            // needed to fix up the return type.
            //
            // See for instance fgUpdateInlineReturnExpressionPlaceHolder.
            if (origCall->TypeGet() == TYP_STRUCT)
            {
                JITDUMP("*** %s Bailing on [%06u] -- can't handle by-value struct returns yet\n", Name(),
                        compiler->dspTreeID(origCall));
                ClearFlag();

                // For stub calls restore the stub address
                if (origCall->IsVirtualStub())
                {
                    origCall->gtStubCallStubAddr = origCall->gtInlineCandidateInfo->stubAddr;
                }
                return;
            }

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
        virtual GenTreeCall* GetCall(GenTreeStmt* callStmt)
        {
            GenTree* tree = callStmt->gtStmtExpr;
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
            GenTree* thisTree = compiler->gtCloneExpr(origCall->gtCallObjp);

            // Create temp for this if the tree is costly.
            if (!thisTree->IsLocal())
            {
                const unsigned thisTempNum = compiler->lvaGrabTemp(true DEBUGARG("guarded devirt this temp"));
                // lvaSetClass(thisTempNum, ...);
                GenTree*     asgTree = compiler->gtNewTempAssign(thisTempNum, thisTree);
                GenTreeStmt* asgStmt = compiler->fgNewStmtFromTree(asgTree, stmt->gtStmtILoffsx);
                compiler->fgInsertStmtAtEnd(checkBlock, asgStmt);

                thisTree = compiler->gtNewLclvNode(thisTempNum, TYP_REF);

                // Propagate the new this to the call. Must be a new expr as the call
                // will live on in the else block and thisTree is used below.
                origCall->gtCallObjp = compiler->gtNewLclvNode(thisTempNum, TYP_REF);
            }

            GenTree* methodTable = compiler->gtNewIndir(TYP_I_IMPL, thisTree);
            methodTable->gtFlags |= GTF_IND_INVARIANT;

            // Find target method table
            GuardedDevirtualizationCandidateInfo* guardedInfo       = origCall->gtGuardedDevirtualizationCandidateInfo;
            CORINFO_CLASS_HANDLE                  clsHnd            = guardedInfo->guardedClassHandle;
            GenTree*                              targetMethodTable = compiler->gtNewIconEmbClsHndNode(clsHnd);

            // Compare and jump to else (which does the indirect call) if NOT equal
            GenTree*     methodTableCompare = compiler->gtNewOperNode(GT_NE, TYP_INT, targetMethodTable, methodTable);
            GenTree*     jmpTree            = compiler->gtNewOperNode(GT_JTRUE, TYP_VOID, methodTableCompare);
            GenTreeStmt* jmpStmt            = compiler->fgNewStmtFromTree(jmpTree, stmt->gtStmtILoffsx);
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
                assert(retExpr->gtRetExpr.gtInlineCandidate == origCall);
            }

            if (origCall->TypeGet() != TYP_VOID)
            {
                returnTemp = compiler->lvaGrabTemp(false DEBUGARG("guarded devirt return temp"));
                JITDUMP("Reworking call(s) to return value via a new temp V%02u\n", returnTemp);

                if (varTypeIsStruct(origCall))
                {
                    compiler->lvaSetStruct(returnTemp, origCall->gtRetClsHnd, false);
                }

                GenTree* tempTree = compiler->gtNewLclvNode(returnTemp, origCall->TypeGet());

                JITDUMP("Updating GT_RET_EXPR [%06u] to refer to temp V%02u\n", compiler->dspTreeID(retExpr),
                        returnTemp);
                retExpr->gtRetExpr.gtInlineCandidate = tempTree;
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
                GenTree* nopTree                     = compiler->gtNewNothingNode();
                retExpr->gtRetExpr.gtInlineCandidate = nopTree;
            }
            else
            {
                // We do not produce GT_RET_EXPRs for CTOR calls, so there is nothing to patch.
            }
        }

        //------------------------------------------------------------------------
        // CreateThen: create else block with direct call to method
        //
        virtual void CreateThen()
        {
            thenBlock = CreateAndInsertBasicBlock(BBJ_ALWAYS, checkBlock);

            InlineCandidateInfo* inlineInfo = origCall->gtInlineCandidateInfo;
            CORINFO_CLASS_HANDLE clsHnd     = inlineInfo->clsHandle;

            // copy 'this' to temp with exact type.
            const unsigned thisTemp  = compiler->lvaGrabTemp(false DEBUGARG("guarded devirt this exact temp"));
            GenTree*       clonedObj = compiler->gtCloneExpr(origCall->gtCallObjp);
            GenTree*       assign    = compiler->gtNewTempAssign(thisTemp, clonedObj);
            compiler->lvaSetClass(thisTemp, clsHnd, true);
            GenTreeStmt* assignStmt = compiler->gtNewStmt(assign);
            compiler->fgInsertStmtAtEnd(thenBlock, assignStmt);

            // Clone call. Note we must use the special candidate helper.
            GenTreeCall* call = compiler->gtCloneCandidateCall(origCall);
            call->gtCallObjp  = compiler->gtNewLclvNode(thisTemp, TYP_REF);
            call->SetIsGuarded();

            JITDUMP("Direct call [%06u] in block BB%02u\n", compiler->dspTreeID(call), thenBlock->bbNum);

            // Then invoke impDevirtualizeCall to actually
            // transform the call for us. It should succeed.... as we have
            // now provided an exact typed this.
            CORINFO_METHOD_HANDLE  methodHnd              = inlineInfo->methInfo.ftn;
            unsigned               methodFlags            = inlineInfo->methAttr;
            CORINFO_CONTEXT_HANDLE context                = inlineInfo->exactContextHnd;
            const bool             isLateDevirtualization = true;
            bool explicitTailCall = (call->gtCall.gtCallMoreFlags & GTF_CALL_M_EXPLICIT_TAILCALL) != 0;
            compiler->impDevirtualizeCall(call, &methodHnd, &methodFlags, &context, nullptr, isLateDevirtualization,
                                          explicitTailCall);

            // Presumably devirt might fail? If so we should try and avoid
            // making this a guarded devirt candidate instead of ending
            // up here.
            assert(!call->IsVirtual());

            // Re-establish this call as an inline candidate.
            GenTree* oldRetExpr         = inlineInfo->retExpr;
            inlineInfo->clsHandle       = clsHnd;
            inlineInfo->exactContextHnd = context;
            call->gtInlineCandidateInfo = inlineInfo;

            // Add the call
            GenTreeStmt* callStmt = compiler->gtNewStmt(call);
            compiler->fgInsertStmtAtEnd(thenBlock, callStmt);

            // If there was a ret expr for this call, we need to create a new one
            // and append it just after the call.
            //
            // Note the original GT_RET_EXPR is sitting at the join point of the
            // guarded expansion and for non-void calls, and now refers to a temp local;
            // we set all this up in FixupRetExpr().
            if (oldRetExpr != nullptr)
            {
                GenTree* retExpr    = compiler->gtNewInlineCandidateReturnExpr(call, call->TypeGet());
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

                GenTreeStmt* resultStmt = compiler->gtNewStmt(retExpr);
                compiler->fgInsertStmtAtEnd(thenBlock, resultStmt);
            }
        }

        //------------------------------------------------------------------------
        // CreateElse: create else block. This executes the unaltered indirect call.
        //
        virtual void CreateElse()
        {
            elseBlock            = CreateAndInsertBasicBlock(BBJ_NONE, thenBlock);
            GenTreeCall* call    = origCall;
            GenTreeStmt* newStmt = compiler->gtNewStmt(call);

            call->gtFlags &= ~GTF_CALL_INLINE_CANDIDATE;
            call->SetIsGuarded();

            JITDUMP("Residual call [%06u] moved to block BB%02u\n", compiler->dspTreeID(call), elseBlock->bbNum);

            if (returnTemp != BAD_VAR_NUM)
            {
                GenTree* assign     = compiler->gtNewTempAssign(returnTemp, call);
                newStmt->gtStmtExpr = assign;
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
            stmt->gtStmtExpr = compiler->gtNewNothingNode();
        }

    private:
        unsigned returnTemp;
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
        assert(!tree->AsCall()->IsFatPointerCandidate());
        assert(!tree->AsCall()->IsGuardedDevirtualizationCandidate());
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

    for (BasicBlock* block = fgFirstBB; block != nullptr; block = block->bbNext)
    {
        for (GenTreeStmt* stmt = fgFirstBB->firstStmt(); stmt != nullptr; stmt = stmt->gtNextStmt)
        {
            fgWalkTreePre(&stmt->gtStmtExpr, fgDebugCheckForTransformableIndirectCalls);
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
void Compiler::fgTransformIndirectCalls()
{
    JITDUMP("\n*************** in fgTransformIndirectCalls(%s)\n", compIsForInlining() ? "inlinee" : "root");

    if (doesMethodHaveFatPointer() || doesMethodHaveGuardedDevirtualization())
    {
        IndirectCallTransformer indirectCallTransformer(this);
        int                     count = indirectCallTransformer.Run();

        if (count > 0)
        {
            JITDUMP("\n*************** After fgTransformIndirectCalls() [%d calls transformed]\n", count);
            INDEBUG(if (verbose) { fgDispBasicBlocks(true); });
        }
        else
        {
            JITDUMP(" -- no transforms done (?)\n");
        }

        clearMethodHasFatPointer();
        clearMethodHasGuardedDevirtualization();
    }
    else
    {
        JITDUMP(" -- no candidates to transform\n");
    }

    INDEBUG(CheckNoTransformableIndirectCallsRemain(););
}
