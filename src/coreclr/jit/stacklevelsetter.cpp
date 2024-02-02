// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "jitpch.h"
#ifdef _MSC_VER
#pragma hdrstop
#endif

#include "stacklevelsetter.h"

StackLevelSetter::StackLevelSetter(Compiler* compiler)
    : Phase(compiler, PHASE_STACK_LEVEL_SETTER)
    , currentStackLevel(0)
    , maxStackLevel(0)
    , memAllocator(compiler->getAllocator(CMK_CallArgs))
    , putArgNumSlots(memAllocator)
    , throwHelperBlocksUsed(comp->fgUseThrowHelperBlocks() && comp->compUsesThrowHelper)
#if !FEATURE_FIXED_OUT_ARGS
    , framePointerRequired(compiler->codeGen->isFramePointerRequired())
#endif // !FEATURE_FIXED_OUT_ARGS
{
    // The constructor reads this value to skip iterations that could set it if it is already set.
    compiler->codeGen->resetWritePhaseForFramePointerRequired();
}

//------------------------------------------------------------------------
// DoPhase: Calculate stack slots numbers for outgoing args.
//
// Returns:
//   PhaseStatus indicating what, if anything, was changed.
//
// Notes:
//   For non-x86 platforms it calculates the max number of slots
//   that calls inside this method can push on the stack.
//   This value is used for sanity checks in the emitter.
//
//   Stack slots are pointer-sized: 4 bytes for 32-bit platforms, 8 bytes for 64-bit platforms.
//
//   For x86 it also sets throw-helper blocks incoming stack depth and set
//   framePointerRequired when it is necessary. These values are used to pop
//   pushed args when an exception occurs.
//
PhaseStatus StackLevelSetter::DoPhase()
{
    for (BasicBlock* const block : comp->Blocks())
    {
        ProcessBlock(block);
    }

#if !FEATURE_FIXED_OUT_ARGS
    if (framePointerRequired)
    {
        comp->codeGen->setFramePointerRequired(true);
    }
#endif // !FEATURE_FIXED_OUT_ARGS

    CheckAdditionalArgs();

    comp->fgSetPtrArgCntMax(maxStackLevel);
    CheckArgCnt();

    // When optimizing, check if there are any unused throw helper blocks,
    // and if so, remove them.
    //
    bool madeChanges = false;

    if (comp->opts.OptimizationEnabled())
    {
        for (Compiler::AddCodeDsc* add = comp->fgGetAdditionalCodeDescriptors(); add != nullptr; add = add->acdNext)
        {
            if (add->acdUsed)
            {
                continue;
            }

            BasicBlock* const block = add->acdDstBlk;
            JITDUMP("Throw help block " FMT_BB " is unused\n", block->bbNum);
            block->RemoveFlags(BBF_DONT_REMOVE);
            comp->fgRemoveBlock(block, /* unreachable */ true);
            madeChanges = true;
        }
    }
    else
    {
        // Mark all the throw helpers as used to avoid asserts later.
        for (Compiler::AddCodeDsc* add = comp->fgGetAdditionalCodeDescriptors(); add != nullptr; add = add->acdNext)
        {
            add->acdUsed = true;
        }
    }

    return madeChanges ? PhaseStatus::MODIFIED_EVERYTHING : PhaseStatus::MODIFIED_NOTHING;
}

//------------------------------------------------------------------------
// ProcessBlock: Do stack level calculations for one block.
//
// Notes:
//   Block starts and ends with an empty outgoing stack.
//   Nodes in blocks are iterated in the reverse order to memorize GT_PUTARG_STK
//   and GT_PUTARG_SPLIT stack sizes.
//
//   Also note which (if any) throw helper blocks might end up being used by
//   codegen.
//
// Arguments:
//   block - the block to process.
//
void StackLevelSetter::ProcessBlock(BasicBlock* block)
{
    assert(currentStackLevel == 0);
    LIR::ReadOnlyRange& range = LIR::AsRange(block);
    for (auto i = range.rbegin(); i != range.rend(); ++i)
    {
        GenTree* node = *i;
        if (node->OperIsPutArgStkOrSplit())
        {
            GenTreePutArgStk* putArg   = node->AsPutArgStk();
            unsigned          numSlots = putArgNumSlots[putArg];
            putArgNumSlots.Remove(putArg);
            SubStackLevel(numSlots);
        }

        if (node->IsCall())
        {
            GenTreeCall* call                = node->AsCall();
            unsigned     usedStackSlotsCount = PopArgumentsFromCall(call);
#if defined(UNIX_X86_ABI)
            call->gtArgs.SetStkSizeBytes(usedStackSlotsCount * TARGET_POINTER_SIZE);
#endif // UNIX_X86_ABI
        }

        if (!throwHelperBlocksUsed)
        {
            continue;
        }

        // When optimizing we want to know what throw helpers might be used
        // so we can remove the ones that aren't needed.
        //
        // If we're not optimizing then the helper requests made in
        // morph are likely still accurate, so we don't bother checking
        // if helpers are indeed used.
        //
        bool checkForHelpers = comp->opts.OptimizationEnabled();

#if !FEATURE_FIXED_OUT_ARGS
        // Even if not optimizing, if we have a moving SP frame, a shared helper may
        // be reached with mixed stack depths and so force this method to use
        // a frame pointer. Once we see that the method will need a frame
        // pointer we no longer need to check for this case.
        //
        checkForHelpers |= !framePointerRequired;
#endif

        if (checkForHelpers)
        {
            if (((node->gtFlags & GTF_EXCEPT) != 0) && node->OperMayThrow(comp))
            {
                SetThrowHelperBlocks(node, block);
            }
        }
    }
    assert(currentStackLevel == 0);
}

//------------------------------------------------------------------------
// SetThrowHelperBlocks: Set throw helper blocks incoming stack levels targeted
//                       from the node.
//
// Notes:
//   one node can target several helper blocks, but not all operands that throw do this.
//   So the function can set 0-2 throw blocks depends on oper and overflow flag.
//
// Arguments:
//   node - the node to process;
//   block - the source block for the node.
//
void StackLevelSetter::SetThrowHelperBlocks(GenTree* node, BasicBlock* block)
{
    assert(node->OperMayThrow(comp));

    // Check that it uses throw block, find its kind, find the block, set level.
    switch (node->OperGet())
    {
        case GT_BOUNDS_CHECK:
        {
            GenTreeBoundsChk* bndsChk = node->AsBoundsChk();
            SetThrowHelperBlock(bndsChk->gtThrowKind, block);
        }
        break;

        case GT_INDEX_ADDR:
        case GT_ARR_ELEM:
            SetThrowHelperBlock(SCK_RNGCHK_FAIL, block);
            break;

        case GT_CKFINITE:
            SetThrowHelperBlock(SCK_ARITH_EXCPN, block);
            break;

#if defined(TARGET_ARM64) || defined(TARGET_ARM) || defined(TARGET_LOONGARCH64) || defined(TARGET_RISCV64)
        case GT_DIV:
        case GT_UDIV:
#if defined(TARGET_LOONGARCH64) || defined(TARGET_RISCV64)
        case GT_MOD:
        case GT_UMOD:
#endif
        {
            ExceptionSetFlags exSetFlags = node->OperExceptions(comp);

            if ((exSetFlags & ExceptionSetFlags::DivideByZeroException) != ExceptionSetFlags::None)
            {
                SetThrowHelperBlock(SCK_DIV_BY_ZERO, block);
            }
            else
            {
                // Even if we thought it might divide by zero during morph, now we know it never will.
                node->gtFlags |= GTF_DIV_MOD_NO_BY_ZERO;
            }

            if ((exSetFlags & ExceptionSetFlags::ArithmeticException) != ExceptionSetFlags::None)
            {
                SetThrowHelperBlock(SCK_ARITH_EXCPN, block);
            }
            else
            {
                // Even if we thought it might overflow during morph, now we know it never will.
                node->gtFlags |= GTF_DIV_MOD_NO_OVERFLOW;
            }
        }
        break;
#endif

        default: // Other opers can target throw only due to overflow.
            break;
    }
    if (node->gtOverflowEx())
    {
        SetThrowHelperBlock(SCK_OVERFLOW, block);
    }
}

//------------------------------------------------------------------------
// SetThrowHelperBlock: Set throw helper block incoming stack levels targeted
//                      from the block with this kind.
//
// Notes:
//   Set framePointerRequired if finds that the block has several incoming edges
//   with different stack levels.
//
// Arguments:
//   kind - the special throw-helper kind;
//   block - the source block that targets helper.
//
void StackLevelSetter::SetThrowHelperBlock(SpecialCodeKind kind, BasicBlock* block)
{
    Compiler::AddCodeDsc* add = comp->fgFindExcptnTarget(kind, comp->bbThrowIndex(block));
    assert(add != nullptr);

    // We expect we'll actually need this helper.
    add->acdUsed = true;

#if !FEATURE_FIXED_OUT_ARGS

    if (add->acdStkLvlInit)
    {
        // If different range checks happen at different stack levels,
        // they can't all jump to the same "call @rngChkFailed" AND have
        // frameless methods, as the rngChkFailed may need to unwind the
        // stack, and we have to be able to report the stack level.
        //
        // The following check forces most methods that reference an
        // array element in a parameter list to have an EBP frame,
        // this restriction could be removed with more careful code
        // generation for BBJ_THROW (i.e. range check failed).
        //
        // For Linux/x86, we possibly need to insert stack alignment adjustment
        // before the first stack argument pushed for every call. But we
        // don't know what the stack alignment adjustment will be when
        // we morph a tree that calls fgAddCodeRef(), so the stack depth
        // number will be incorrect. For now, simply force all functions with
        // these helpers to have EBP frames. It might be possible to make
        // this less conservative. E.g., for top-level (not nested) calls
        // without stack args, the stack pointer hasn't changed and stack
        // depth will be known to be zero. Or, figure out a way to update
        // or generate all required helpers after all stack alignment
        // has been added, and the stack level at each call to fgAddCodeRef()
        // is known, or can be recalculated.
        CLANG_FORMAT_COMMENT_ANCHOR;
#if defined(UNIX_X86_ABI)
        framePointerRequired = true;
#else  // !defined(UNIX_X86_ABI)
        if (add->acdStkLvl != currentStackLevel)
        {
            framePointerRequired = true;
        }
#endif // !defined(UNIX_X86_ABI)
    }
    else
    {
        add->acdStkLvlInit = true;
        if (add->acdStkLvl != currentStackLevel)
        {
            JITDUMP("Wrong stack level was set for " FMT_BB "\n", add->acdDstBlk->bbNum);
        }
#ifdef DEBUG
        add->acdDstBlk->bbTgtStkDepth = currentStackLevel;
#endif // Debug
        add->acdStkLvl = currentStackLevel;
    }
#endif // !FEATURE_FIXED_OUT_ARGS
}

//------------------------------------------------------------------------
// PopArgumentsFromCall: Calculate the number of stack arguments that are used by the call.
//
// Notes:
//   memorize number of slots that each stack argument use.
//
// Arguments:
//   call - the call to process.
//
// Return value:
//   the number of stack slots in stack arguments for the call.
unsigned StackLevelSetter::PopArgumentsFromCall(GenTreeCall* call)
{
    unsigned usedStackSlotsCount = 0;
    if (call->gtArgs.HasStackArgs())
    {
        for (CallArg& arg : call->gtArgs.Args())
        {
            const unsigned slotCount = arg.AbiInfo.GetStackSlotsNumber();
            if (slotCount != 0)
            {
                GenTree* node = arg.GetNode();
                assert(node->OperIsPutArgStkOrSplit());

                GenTreePutArgStk* putArg = node->AsPutArgStk();

#if !FEATURE_FIXED_OUT_ARGS
                assert((slotCount * TARGET_POINTER_SIZE) == putArg->GetStackByteSize());
#endif // !FEATURE_FIXED_OUT_ARGS

                putArgNumSlots.Set(putArg, slotCount);

                usedStackSlotsCount += slotCount;
                AddStackLevel(slotCount);
            }
        }
    }
    return usedStackSlotsCount;
}

//------------------------------------------------------------------------
// SubStackLevel: Reflect pushing to the stack.
//
// Arguments:
//   value - a positive value to add.
//
void StackLevelSetter::AddStackLevel(unsigned value)
{
    currentStackLevel += value;

    if (currentStackLevel > maxStackLevel)
    {
        maxStackLevel = currentStackLevel;
    }
}

//------------------------------------------------------------------------
// SubStackLevel: Reflect popping from the stack.
//
// Arguments:
//   value - a positive value to subtract.
//
void StackLevelSetter::SubStackLevel(unsigned value)
{
    assert(currentStackLevel >= value);
    currentStackLevel -= value;
}

//------------------------------------------------------------------------
// CheckArgCnt: Check whether the maximum arg size will change codegen requirements.
//
// Notes:
//    CheckArgCnt records the maximum number of pushed arguments.
//    Depending upon this value of the maximum number of pushed arguments
//    we may need to use an EBP frame or be partially interruptible.
//    This functionality has to be called after maxStackLevel is set.
//
// Assumptions:
//    This must be called when isFramePointerRequired() is in a write phase, because it is a
//    phased variable (can only be written before it has been read).
//
void StackLevelSetter::CheckArgCnt()
{
    if (!comp->compCanEncodePtrArgCntMax())
    {
#ifdef DEBUG
        if (comp->verbose)
        {
            printf("Too many pushed arguments for fully interruptible encoding, marking method as partially "
                   "interruptible\n");
        }
#endif
        comp->SetInterruptible(false);
    }
    if (maxStackLevel >= sizeof(unsigned))
    {
#ifdef DEBUG
        if (comp->verbose)
        {
            printf("Too many pushed arguments for an ESP based encoding, forcing an EBP frame\n");
        }
#endif
        comp->codeGen->setFramePointerRequired(true);
    }
}

//------------------------------------------------------------------------
// CheckAdditionalArgs: Check if there are additional args that need stack slots.
//
// Notes:
//    Currently only x86 profiler hook needs it.
//
void StackLevelSetter::CheckAdditionalArgs()
{
#if defined(TARGET_X86)
    if (comp->compIsProfilerHookNeeded())
    {
        if (maxStackLevel == 0)
        {
            JITDUMP("Upping fgPtrArgCntMax from %d to 1\n", maxStackLevel);
            maxStackLevel = 1;
        }
    }
#endif // TARGET_X86
}
