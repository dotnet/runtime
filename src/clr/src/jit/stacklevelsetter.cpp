// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "jitpch.h"
#ifdef _MSC_VER
#pragma hdrstop
#endif

#include "stacklevelsetter.h"

StackLevelSetter::StackLevelSetter(Compiler* compiler)
    : Phase(compiler, "StackLevelSetter", PHASE_STACK_LEVEL_SETTER)
    , currentStackLevel(0)
    , maxStackLevel(0)
    , memAllocator(compiler->getAllocator(CMK_fgArgInfoPtrArr))
    , putArgNumSlots(memAllocator)
#if !FEATURE_FIXED_OUT_ARGS
    , framePointerRequired(compiler->codeGen->isFramePointerRequired())
    , throwHelperBlocksUsed(comp->fgUseThrowHelperBlocks() && comp->compUsesThrowHelper)
#endif // !FEATURE_FIXED_OUT_ARGS
{
}

//------------------------------------------------------------------------
// DoPhase: Calculate stack slots numbers for outgoing args.
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
void StackLevelSetter::DoPhase()
{
    for (BasicBlock* block = comp->fgFirstBB; block != nullptr; block = block->bbNext)
    {
        ProcessBlock(block);
    }
#if !FEATURE_FIXED_OUT_ARGS

    if (framePointerRequired && !comp->codeGen->isFramePointerRequired())
    {
        JITDUMP("framePointerRequired is not set when it is required\n");
        comp->codeGen->resetWritePhaseForFramePointerRequired();
        comp->codeGen->setFramePointerRequired(true);
    }
#endif // !FEATURE_FIXED_OUT_ARGS
    if (maxStackLevel != comp->fgGetPtrArgCntMax())
    {
        JITDUMP("fgPtrArgCntMax was calculated wrong during the morph, the old value: %u, the right value: %u.\n",
                comp->fgGetPtrArgCntMax(), maxStackLevel);
        comp->fgSetPtrArgCntMax(maxStackLevel);
    }
}

//------------------------------------------------------------------------
// ProcessBlock: Do stack level calculations for one block.
//
// Notes:
//   Block starts and ends with an empty outgoing stack.
//   Nodes in blocks are iterated in the reverse order to memorize GT_PUTARG_STK
//   and GT_PUTARG_SPLIT stack sizes.
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

#if !FEATURE_FIXED_OUT_ARGS
        // Set throw blocks incoming stack depth for x86.
        if (throwHelperBlocksUsed && !framePointerRequired)
        {
            if (node->OperMayThrow(comp))
            {
                SetThrowHelperBlocks(node, block);
            }
        }
#endif // !FEATURE_FIXED_OUT_ARGS

        if (node->IsCall())
        {
            GenTreeCall* call = node->AsCall();

            unsigned usedStackSlotsCount = PopArgumentsFromCall(call);
#if defined(UNIX_X86_ABI)
            assert(call->fgArgInfo->GetStkSizeBytes() == usedStackSlotsCount * TARGET_POINTER_SIZE);
            call->fgArgInfo->SetStkSizeBytes(usedStackSlotsCount * TARGET_POINTER_SIZE);
#endif // UNIX_X86_ABI
        }
    }
    assert(currentStackLevel == 0);
}

#if !FEATURE_FIXED_OUT_ARGS
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
void StackLevelSetter::SetThrowHelperBlocks(GenTree* node, BasicBlock* block)
{
    assert(node->OperMayThrow(comp));

    // Check that it uses throw block, find its kind, find the block, set level.
    switch (node->OperGet())
    {
        case GT_ARR_BOUNDS_CHECK:
#ifdef FEATURE_SIMD
        case GT_SIMD_CHK:
#endif // FEATURE_SIMD
#ifdef FEATURE_HW_INTRINSICS
        case GT_HW_INTRINSIC_CHK:
#endif // FEATURE_HW_INTRINSICS
        {
            GenTreeBoundsChk* bndsChk = node->AsBoundsChk();
            SetThrowHelperBlock(bndsChk->gtThrowKind, block);
        }
        break;
        case GT_INDEX_ADDR:
        case GT_ARR_ELEM:
        case GT_ARR_INDEX:
        {
            SetThrowHelperBlock(SCK_RNGCHK_FAIL, block);
        }
        break;

        case GT_CKFINITE:
        {
            SetThrowHelperBlock(SCK_ARITH_EXCPN, block);
        }
        break;
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
void StackLevelSetter::SetThrowHelperBlock(SpecialCodeKind kind, BasicBlock* block)
{
    Compiler::AddCodeDsc* add = comp->fgFindExcptnTarget(kind, comp->bbThrowIndex(block));
    assert(add != nullptr);
    if (add->acdStkLvlInit)
    {
        if (add->acdStkLvl != currentStackLevel)
        {
            framePointerRequired = true;
        }
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
}

#endif // !FEATURE_FIXED_OUT_ARGS

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
    unsigned   usedStackSlotsCount = 0;
    fgArgInfo* argInfo             = call->fgArgInfo;
    if (argInfo->HasStackArgs())
    {
        for (unsigned i = 0; i < argInfo->ArgCount(); ++i)
        {
            fgArgTabEntry* argTab = argInfo->ArgTable()[i];
            if (argTab->numSlots != 0)
            {
                GenTree* node = argTab->node;
                assert(node->OperIsPutArgStkOrSplit());

                GenTreePutArgStk* putArg = node->AsPutArgStk();

#if !FEATURE_FIXED_OUT_ARGS
                assert(argTab->numSlots == putArg->gtNumSlots);
#endif // !FEATURE_FIXED_OUT_ARGS

                putArgNumSlots.Set(putArg, argTab->numSlots);

                usedStackSlotsCount += argTab->numSlots;
                AddStackLevel(argTab->numSlots);
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
