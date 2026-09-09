// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "jitpch.h"
#ifdef _MSC_VER
#pragma hdrstop
#endif

//------------------------------------------------------------------------
// fgMorphArgs: Walk and transform (morph) the arguments of a call
//
// Arguments:
//    call - the call for which we are doing the argument morphing
//
// Return Value:
//    Like most morph methods, this method returns the morphed node,
//    though in this case there are currently no scenarios where the
//    node itself is re-created.
//
// Notes:
//    This calls CallArgs::AddFinalArgsAndDetermineABIInfo to determine ABI
//    information for the call. If it has already been determined, that method
//    will simply return.
//
//    This method changes the state of the call node. It may be called even
//    after it has already done the first round of morphing.
//
//    The first time it is called (i.e. during global morphing), this method
//    computes the "late arguments". This is when it determines which arguments
//    need to be evaluated to temps prior to the main argument setup, and which
//    can be directly evaluated into the argument location. It also creates a
//    second argument list (the late args) that does the final placement of the
//    arguments, e.g. into registers or onto the stack.
//
//    The "non-late arguments", are doing the in-order evaluation of the
//    arguments that might have side-effects, such as embedded stores, calls
//    or possible throws. In these cases, it and earlier arguments must be
//    evaluated to temps.
//
//    On targets with a fixed outgoing argument area (FEATURE_FIXED_OUT_ARGS),
//    if we have any nested calls, we need to defer the copying of the argument
//    into the fixed argument area until after the call. If the argument did
//    not otherwise need to be computed into a temp, it is moved to late
//    argument and replaced in the "early" arg list with a placeholder node.
//    Also see `CallArgs::EvalArgsToTemps`.
//
GenTreeCall* Compiler::fgMorphArgs(GenTreeCall* call)
{
    GenTreeFlags flagsSummary = GTF_EMPTY;

    bool reMorphing = call->gtArgs.AreArgsComplete();

    call->gtArgs.AddFinalArgsAndDetermineABIInfo(this, call);
    JITDUMP("%sMorphing args for %d.%s:\n", (reMorphing) ? "Re" : "", call->gtTreeID, GenTree::OpName(call->gtOper));

    // If we are remorphing, process the late arguments (which were determined by a previous caller).
    if (reMorphing)
    {
        for (CallArg& arg : call->gtArgs.LateArgs())
        {
            arg.SetLateNode(fgMorphTree(arg.GetLateNode()));
            flagsSummary |= arg.GetLateNode()->gtFlags;
        }
    }

    // First we morph the argument subtrees ('this' pointer, arguments, etc.).
    // During the first call to fgMorphArgs we also record the
    // information about late arguments in CallArgs.
    // This information is used later to construct the late args

    for (CallArg& arg : call->gtArgs.Args())
    {
        GenTree** parentArgx = &arg.EarlyNodeRef();

        // Morph the arg node and update the node pointer.
        GenTree* argx = *parentArgx;
        if (argx == nullptr)
        {
            // Skip node that was moved to late args during remorphing, no work to be done.
            assert(reMorphing);
            continue;
        }

        argx        = fgMorphTree(argx);
        *parentArgx = argx;

        if (arg.GetWellKnownArg() == WellKnownArg::ThisPointer)
        {
            // We may want to force 'this' into a temp because we want to use
            // it to expand the call target in morph so that CSE can pick it
            // up.
            if (!reMorphing && call->IsExpandedEarly() && call->IsVirtualVtable() && !argx->OperIsLocal())
            {
                call->gtArgs.SetNeedsTemp(&arg);
            }
        }

        // For pointers to locals we can skip reporting GC info and also skip zero initialization.
        // NOTE: We deferred this from the importer because of the inliner.
        if (argx->OperIs(GT_LCL_ADDR))
        {
            argx->gtType = TYP_I_IMPL;
        }

        if (varTypeIsStruct(arg.GetSignatureType()) && !reMorphing)
        {
            bool makeOutArgCopy = false;
            if (arg.AbiInfo.IsPassedByReference())
            {
                makeOutArgCopy = true;
            }
            else if (fgTryMorphStructArg(&arg))
            {
                argx = *parentArgx;
            }
            else
            {
                makeOutArgCopy = true;
            }

            if (makeOutArgCopy)
            {
                fgMakeOutgoingStructArgCopy(call, &arg);

                if (arg.GetEarlyNode() != nullptr)
                {
                    flagsSummary |= arg.GetEarlyNode()->gtFlags;
                }
            }
        }

        flagsSummary |= arg.GetEarlyNode()->gtFlags;

    } // end foreach argument loop

    if (!reMorphing)
    {
        call->gtArgs.ArgsComplete(this, call);
    }

#if FEATURE_FIXED_OUT_ARGS && defined(UNIX_AMD64_ABI)
    if (!call->IsFastTailCall())
    {
        // This is currently required for the UNIX ABI to work correctly.
        opts.compNeedToAlignFrame = true;
    }
#endif // FEATURE_FIXED_OUT_ARGS && UNIX_AMD64_ABI

    // Clear the ASG and EXCEPT (if possible) flags on the call node
    call->gtFlags &= ~GTF_ASG;
    if (!call->OperMayThrow(this))
    {
        call->gtFlags &= ~GTF_EXCEPT;
    }

    // Calls themselves don't originate an ordering side effect.
    // Instead, derive it from the call args via flagsSummary
    call->gtFlags &= ~GTF_ORDER_SIDEEFF;

    // Union in the side effect flags from the call's operands
    call->gtFlags |= flagsSummary & GTF_ALL_EFFECT;

    // If we are remorphing or don't have any register arguments or other arguments that need
    // temps, then we don't need to call SortArgs() and EvalArgsToTemps().
    //
    if (!reMorphing && (call->gtArgs.HasRegArgs() || call->gtArgs.NeedsTemps()))
    {
        // Do the 'defer or eval to temp' analysis.
        call->gtArgs.EvalArgsToTemps(this, call);
    }

#ifdef DEBUG
    if (verbose)
    {
        JITDUMP("Args for [%06u].%s after fgMorphArgs:\n", dspTreeID(call), GenTree::OpName(call->gtOper));
        for (CallArg& arg : call->gtArgs.Args())
        {
            arg.Dump(this);
        }
        printf("OutgoingArgsStackSize is %u\n\n", call->gtArgs.OutgoingArgsStackSize());
    }
#endif
    return call;
}

//-----------------------------------------------------------------------------
// fgTryMorphStructArg:
//   Given a varTypeIsStruct argument, try to morph it into a shape that the
//   backend supports.
//
// Arguments:
//   arg - The argument
//
// Returns:
//   False if the argument cannot be put into a shape supported by the backend.
//
// Remarks:
//   The backend requires register-passed arguments to be of FIELD_LIST shape.
//   For split arguments it is additionally required that registers and stack
//   slots have clean mappings to fields.
//   For stack-passed arguments the backend supports struct-typed arguments
//   directly.
//
bool Compiler::fgTryMorphStructArg(CallArg* arg)
{
    GenTree** use     = GenTree::EffectiveUse(&arg->NodeRef());
    GenTree*  argNode = *use;
    assert(varTypeIsStruct(argNode));

    if (arg->AbiInfo.NumSegments == 0)
    {
        // Pseudo arg. One case is WellKnownArg::AsyncAwaiter. We just handle
        // these as arbitrary struct operands that can be expanded into
        // FIELD_LIST. The async transformation will later store the value into
        // the continuation, so FIELD_LIST allows using decomposed stores.
        if (fgTryReplaceStructLocalWithFields(&arg->NodeRef()))
        {
            arg->GetNode()->SetMorphed(this, true);
        }
        return true;
    }

    bool isSplit = arg->AbiInfo.IsSplitAcrossRegistersAndStack();
#ifdef TARGET_ARM
    if ((isSplit && (arg->AbiInfo.CountRegsAndStackSlots() > 4)) || (!isSplit && arg->AbiInfo.HasAnyStackSegment()))
#else
    if (!arg->AbiInfo.HasAnyRegisterSegment())
#endif
    {
        if (argNode->OperIs(GT_LCL_VAR) &&
            (lvaGetPromotionType(argNode->AsLclVar()->GetLclNum()) == PROMOTION_TYPE_INDEPENDENT))
        {
            // TODO-Arm-CQ: support decomposing "large" promoted structs into field lists.
            if (!isSplit)
            {
                GenTreeFieldList* fieldList = fgMorphLclToFieldList(argNode->AsLclVar());
                // TODO-Cleanup: The containment/reg optionality for x86 is
                // conservative in the "no field list" case.
#ifdef TARGET_X86
                *use = fieldList;
#else
                *use = fieldList->SoleFieldOrThis();
#endif
                *use = fgMorphTree(*use);
            }
            else
            {
                // Set DNER to block independent promotion.
                lvaSetVarDoNotEnregister(argNode->AsLclVar()->GetLclNum() DEBUGARG(DoNotEnregisterReason::IsStructArg));
            }
        }
        else if (argNode->OperIs(GT_LCL_FLD))
        {
            lvaSetVarDoNotEnregister(argNode->AsLclFld()->GetLclNum() DEBUGARG(DoNotEnregisterReason::LocalField));
        }
        else if (argNode->OperIs(GT_BLK))
        {
            ClassLayout* layout = argNode->AsBlk()->GetLayout();

            var_types primitiveType = layout->GetRegisterType();
            if (primitiveType != TYP_UNDEF)
            {
                JITDUMP("Converting argument [%06u] to primitive indirection\n", dspTreeID(argNode));

                argNode->SetOper(GT_IND);
                argNode->gtType = primitiveType;
            }
        }

        // Potentially update commas
        arg->GetNode()->ChangeType((*use)->TypeGet());
        return true;
    }

    GenTree* newArg = nullptr;

    if (argNode->OperIs(GT_LCL_VAR))
    {
        GenTreeLclVar* lclNode = argNode->AsLclVar();
        unsigned       lclNum  = lclNode->GetLclNum();
        LclVarDsc*     varDsc  = lvaGetDesc(lclNum);

        if (!arg->AbiInfo.HasExactlyOneRegisterSegment())
        {
            varDsc->lvIsMultiRegArg = true;
        }

        JITDUMP("Struct argument V%02u: ", lclNum);
        JITDUMPEXEC(arg->Dump(this));

        // Try to see if we can and should use promoted fields to pass this
        // argument.
        //
        if (varDsc->lvPromoted && !varDsc->lvDoNotEnregister && (!isSplit || FieldsMatchAbi(varDsc, arg->AbiInfo)))
        {
            newArg = fgMorphLclToFieldList(lclNode)->SoleFieldOrThis();
            newArg = fgMorphTree(newArg);
        }
    }
    else if (argNode->OperIsFieldList())
    {
        // We can already see a field list here if physical promotion created it.
        // Physical promotion will also create single-field field lists which
        // not everything treats the same as a single node, so fix that here.
        newArg = argNode->AsFieldList()->SoleFieldOrThis();
        if (newArg == argNode)
        {
            return true;
        }
    }

    // If we were not able to use the promoted fields...
    //
    if (newArg == nullptr)
    {
        if (!argNode->TypeIs(TYP_STRUCT) && arg->AbiInfo.HasExactlyOneRegisterSegment())
        {
            // This can be treated primitively. Leave it alone.
            return true;
        }

        if (!argNode->OperIsLocalRead() && !argNode->OperIsLoad())
        {
            // A node we do not know how to turn into multiple registers.
            // Usually HWINTRINSIC. Bail.
            return false;
        }

        ClassLayout* layout     = argNode->TypeIs(TYP_STRUCT) ? argNode->GetLayout(this) : nullptr;
        unsigned     structSize = argNode->TypeIs(TYP_STRUCT) ? layout->GetSize() : genTypeSize(argNode);

        if (layout != nullptr)
        {
            assert(ClassLayout::AreCompatible(typGetObjLayout(arg->GetSignatureClassHandle()), layout));
        }
        else
        {
            assert(varTypeIsSIMD(argNode) && varTypeIsSIMD(arg->GetSignatureType()));
        }

        if (argNode->OperIsLoad())
        {
            unsigned lastLoadSize = structSize % TARGET_POINTER_SIZE;
            if ((lastLoadSize != 0) && !isPow2(lastLoadSize))
            {
                // Cannot read this size from a non-local. Bail.
                return false;
            }

            GenTree* indirAddr = argNode->AsIndir()->Addr();
            if (((indirAddr->gtFlags & GTF_PERSISTENT_SIDE_EFFECTS) != 0) &&
                (arg->AbiInfo.CountRegsAndStackSlots() > 1))
            {
                // Cannot create multiple uses of the address. Bail.
                return false;
            }
        }

        auto createSlotAccess = [=](unsigned offset, var_types type) -> GenTree* {
            assert(offset < structSize);

            if (type == TYP_UNDEF)
            {
                unsigned sizeLeft = structSize - offset;
                if (sizeLeft < TARGET_POINTER_SIZE)
                {
                    switch (sizeLeft)
                    {
                        case 1:
                            type = TYP_UBYTE;
                            break;
                        case 2:
                            type = TYP_USHORT;
                            break;
                        case 3:
                        case 4:
                            type = TYP_INT;
                            break;
                        case 5:
                        case 6:
                        case 7:
                        case 8:
                            type = TYP_LONG;
                            break;
                        default:
                            unreached();
                    }

#ifdef TARGET_ARM64
                    if ((offset > 0) && argNode->OperIsLocalRead())
                    {
                        // For arm64 it's beneficial to consider all tails to
                        // be TYP_I_IMPL to allow more ldp's.
                        type = TYP_I_IMPL;
                    }
#endif
                }
                else if ((layout != nullptr) && ((offset % TARGET_POINTER_SIZE) == 0))
                {
                    type = layout->GetGCPtrType(offset / TARGET_POINTER_SIZE);
                }
                else
                {
                    type = TYP_I_IMPL;
                }
            }

            if (argNode->OperIsLocalRead())
            {
                GenTreeLclVarCommon* lclVar = argNode->AsLclVarCommon();
                LclVarDsc*           dsc    = lvaGetDesc(lclVar);
                GenTree*             result;
                // We sometimes end up with struct reinterpretations where the
                // retyping into a primitive allows us to replace by a scalar
                // local here, so make sure we do that if possible.
                if ((lclVar->GetLclOffs() == 0) && (offset == 0) && (genTypeSize(type) == genTypeSize(dsc)))
                {
                    result = gtNewLclVarNode(lclVar->GetLclNum());
                }
                else
                {
                    result = gtNewLclFldNode(lclVar->GetLclNum(), type, lclVar->GetLclOffs() + offset);

                    if (!dsc->lvDoNotEnregister)
                    {
                        lvaSetVarDoNotEnregister(lclVar->GetLclNum() DEBUGARG(DoNotEnregisterReason::LocalField));
                    }
                }
                result = fgMorphTree(result);
                return result;
            }
            else
            {
                assert(argNode->OperIsLoad());
                GenTree* indirAddr = argNode->AsIndir()->Addr();
                GenTree* addr;

                if (offset == 0)
                {
                    addr = indirAddr;
                }
                else
                {
                    GenTree* indirAddrDup = gtCloneExpr(indirAddr);
                    GenTree* offsetNode   = gtNewIconNode(offset, TYP_I_IMPL);
                    addr                  = gtNewOperNode(GT_ADD, indirAddr->TypeGet(), indirAddrDup, offsetNode);
                }

                GenTree* indir = gtNewIndir(type, addr);
                indir->SetMorphed(this, /* doChildren */ true);
                return indir;
            }
        };

        newArg = new (this, GT_FIELD_LIST) GenTreeFieldList();
        newArg->SetMorphed(this);

        for (const ABIPassingSegment& seg : arg->AbiInfo.Segments())
        {
            if (seg.IsPassedInRegister())
            {
                var_types regType = seg.GetRegisterType(layout);
                GenTree*  access  = createSlotAccess(seg.Offset, regType);

                newArg->AsFieldList()->AddField(this, access, seg.Offset, access->TypeGet());
            }
            else
            {
                for (unsigned slotOffset = 0; slotOffset < seg.Size; slotOffset += TARGET_POINTER_SIZE)
                {
                    unsigned layoutOffset = seg.Offset + slotOffset;
                    GenTree* access       = createSlotAccess(layoutOffset, TYP_UNDEF);

                    newArg->AsFieldList()->AddField(this, access, layoutOffset, access->TypeGet());
                }
            }
        }

        newArg = newArg->AsFieldList()->SoleFieldOrThis();
    }

    JITDUMP("fgTryMorphStructArg created tree:\n");
    DISPTREE(newArg);

    *use = newArg;
    // Potentially update commas
    arg->GetNode()->ChangeType((*use)->TypeGet());
    return true;
}

//-----------------------------------------------------------------------------
// FieldsMatchAbi:
//   Check if the fields of a local map cleanly (in terms of offsets) to the
//   specified ABI info.
//
// Arguments:
//   varDsc  - promoted local
//   abiInfo - ABI information
//
// Returns:
//   True if it does. In that case FIELD_LIST usage is allowed for split args
//   by the backend.
//
bool Compiler::FieldsMatchAbi(LclVarDsc* varDsc, const ABIPassingInformation& abiInfo)
{
    if (varDsc->lvFieldCnt != abiInfo.CountRegsAndStackSlots())
    {
        return false;
    }

    for (const ABIPassingSegment& seg : abiInfo.Segments())
    {
        if (seg.IsPassedInRegister())
        {
            unsigned fieldLclNum = lvaGetFieldLocal(varDsc, seg.Offset);
            if (fieldLclNum == BAD_VAR_NUM)
            {
                return false;
            }
        }
        else
        {
            for (unsigned offset = 0; offset < seg.Size; offset += TARGET_POINTER_SIZE)
            {
                if (lvaGetFieldLocal(varDsc, seg.Offset + offset) == BAD_VAR_NUM)
                {
                    return false;
                }
            }
        }
    }

    return true;
}

//------------------------------------------------------------------------
// fgMorphLclToFieldList: Morph a GT_LCL_VAR node to a GT_FIELD_LIST of its promoted fields
//
// Arguments:
//    lcl  - The GT_LCL_VAR node we will transform
//
// Return value:
//    The new GT_FIELD_LIST that we have created.
//
GenTreeFieldList* Compiler::fgMorphLclToFieldList(GenTreeLclVar* lcl)
{
    LclVarDsc* varDsc = lvaGetDesc(lcl);
    assert(varDsc->lvPromoted);
    unsigned fieldCount  = varDsc->lvFieldCnt;
    unsigned fieldLclNum = varDsc->lvFieldLclStart;

    GenTreeFieldList* fieldList = new (this, GT_FIELD_LIST) GenTreeFieldList();

    for (unsigned i = 0; i < fieldCount; i++)
    {
        LclVarDsc* fieldVarDsc = lvaGetDesc(fieldLclNum);
        GenTree*   lclVar      = gtNewLclvNode(fieldLclNum, fieldVarDsc->TypeGet());
        fieldList->AddField(this, lclVar, fieldVarDsc->lvFldOffset, fieldVarDsc->TypeGet());
        fieldLclNum++;
    }

    return fieldList;
}

//------------------------------------------------------------------------
// fgMakeOutgoingStructArgCopy: make a copy of a struct variable if necessary,
//   to pass to a callee.
//
// Arguments:
//    call - call being processed
//    arg - arg for the call
//
// The arg is updated if necessary with the copy.
//
void Compiler::fgMakeOutgoingStructArgCopy(GenTreeCall* call, CallArg* arg)
{
    GenTree* argx = arg->GetEarlyNode();

#if FEATURE_IMPLICIT_BYREFS
    // If we're optimizing, see if we can avoid making a copy.
    //
    // We don't need a copy if this is the last use of the local.
    //
    if (opts.OptimizationEnabled() && arg->AbiInfo.IsPassedByReference())
    {
        GenTree*             implicitByRefLclAddr;
        target_ssize_t       implicitByRefLclOffs;
        GenTreeLclVarCommon* implicitByRefLcl =
            argx->IsImplicitByrefParameterValuePostMorph(this, &implicitByRefLclAddr, &implicitByRefLclOffs);

        GenTreeLclVarCommon* lcl = implicitByRefLcl;
        if ((lcl == nullptr) && argx->OperIsLocal())
        {
            lcl                  = argx->AsLclVarCommon();
            implicitByRefLclOffs = lcl->GetLclOffs();
        }

        if (lcl != nullptr)
        {
            const unsigned   varNum = lcl->GetLclNum();
            LclVarDsc* const varDsc = lvaGetDesc(varNum);

            // We generally use liveness to figure out if we can omit creating
            // this copy. However, even without liveness (e.g. due to too many
            // tracked locals), we also handle some other cases:
            //
            // * (must not copy) If the call is a tail call, the use is a last use.
            //   We must skip the copy if we have a fast tail call.
            //
            // * (may not copy) if the call is noreturn, the use is a last use.
            //   We also check for just one reference here as we are not doing
            //   alias analysis of the call's parameters, or checking if the call
            //   site is not within some try region.
            //
            bool omitCopy = call->IsTailCall();

            if (!omitCopy && fgGlobalMorph)
            {
                omitCopy = (varDsc->lvIsLastUseCopyOmissionCandidate || (implicitByRefLcl != nullptr)) &&
                           !varDsc->lvPromoted && !varDsc->lvIsStructField && ((lcl->gtFlags & GTF_VAR_DEATH) != 0);
            }

            // Disallow the argument from potentially aliasing the return
            // buffer.
            if (omitCopy)
            {
                GenTreeLclVarCommon* retBuffer = gtCallGetDefinedRetBufLclAddr(call);
                if ((retBuffer != nullptr) && (retBuffer->GetLclNum() == varNum))
                {
                    unsigned       retBufferSize  = typGetObjLayout(call->gtRetClsHnd)->GetSize();
                    target_ssize_t retBufferStart = retBuffer->GetLclOffs();
                    target_ssize_t retBufferEnd   = retBufferStart + static_cast<target_ssize_t>(retBufferSize);

                    unsigned       argSize        = arg->GetSignatureType() == TYP_STRUCT
                                                        ? typGetObjLayout(arg->GetSignatureClassHandle())->GetSize()
                                                        : genTypeSize(arg->GetSignatureType());
                    target_ssize_t implByrefStart = implicitByRefLclOffs;
                    target_ssize_t implByrefEnd   = implByrefStart + static_cast<target_ssize_t>(argSize);

                    bool disjoint = (retBufferEnd <= implByrefStart) || (implByrefEnd <= retBufferStart);
                    omitCopy      = disjoint;
                }
            }

            if (omitCopy)
            {
                if (implicitByRefLcl != nullptr)
                {
                    arg->SetEarlyNode(implicitByRefLclAddr);
                }
                else
                {
                    uint16_t offs = lcl->GetLclOffs();
                    lcl->ChangeOper(GT_LCL_ADDR);
                    lcl->AsLclFld()->SetLclOffs(offs);
                    lcl->gtType = TYP_I_IMPL;
                    lcl->gtFlags &= ~GTF_ALL_EFFECT;
                    lvaSetVarAddrExposed(varNum DEBUGARG(AddressExposedReason::ESCAPE_ADDRESS));

                    // Copy prop could allow creating another later use of lcl if there are live assertions about it.
                    fgKillDependentAssertions(varNum DEBUGARG(lcl));
                }

                JITDUMP("did not need to make outgoing copy for last use of V%02d\n", varNum);
                return;
            }
        }
    }
#endif

    JITDUMP("making an outgoing copy for struct arg\n");
    assert(!call->IsTailCall() || !arg->AbiInfo.IsPassedByReference());

    CORINFO_CLASS_HANDLE copyBlkClass = arg->GetSignatureClassHandle();
    unsigned             tmp          = 0;
    bool                 found        = false;

    // Attempt to find a local we have already used for an outgoing struct and reuse it.
    // We do not reuse within a statement and we don't reuse if we're in LIR
    if (!opts.MinOpts() && (fgOrder == FGOrderTree))
    {
        found = ForEachHbvBitSet(*fgAvailableOutgoingArgTemps, [&](indexType lclNum) {
            LclVarDsc*   varDsc = lvaGetDesc((unsigned)lclNum);
            ClassLayout* layout = varDsc->GetLayout();
            if (!layout->IsCustomLayout() && (layout->GetClassHandle() == copyBlkClass))
            {
                tmp = (unsigned)lclNum;
                JITDUMP("reusing outgoing struct arg V%02u\n", tmp);
                fgAvailableOutgoingArgTemps->clearBit(lclNum);
                return HbvWalk::Abort;
            }

            return HbvWalk::Continue;
        }) == HbvWalk::Abort;
    }

    // Create the CopyBlk tree and insert it.
    if (!found)
    {
        // Get a new temp
        // Here We don't need unsafe value cls check, since the addr of this temp is used only in copyblk.
        tmp = lvaGrabTemp(true DEBUGARG("by-value struct argument"));
        lvaSetStruct(tmp, copyBlkClass, false);
    }

    if (fgUsedSharedTemps != nullptr)
    {
        fgUsedSharedTemps->Push(tmp);
    }
    else
    {
        assert(!fgGlobalMorph);
    }

    call->gtArgs.SetNeedsTemp(arg);

    // Copy the valuetype to the temp
    GenTree* copyBlk = gtNewStoreLclVarNode(tmp, argx);
    copyBlk          = fgMorphCopyBlock(copyBlk);

    GenTree* argNode;
    if (arg->AbiInfo.IsPassedByReference())
    {
        argNode = gtNewLclVarAddrNode(tmp);
        lvaSetVarAddrExposed(tmp DEBUGARG(AddressExposedReason::ESCAPE_ADDRESS));
    }
    else
    {
        argNode = gtNewLclvNode(tmp, lvaGetDesc(tmp)->TypeGet());
    }
    argNode->SetMorphed(this);

#if FEATURE_FIXED_OUT_ARGS

    // For fixed out args we create the setup node here; EvalArgsToTemps knows
    // to handle the case of "already have a setup node" properly.
    arg->SetEarlyNode(copyBlk);
    arg->SetLateNode(argNode);

#else  // !FEATURE_FIXED_OUT_ARGS

    // Structs are always on the stack, and thus never need temps
    // so we have to put the copy and temp all into one expression.
    // Change the expression to "(tmp=val),tmp"
    argNode = gtNewOperNode(GT_COMMA, argNode->TypeGet(), copyBlk, argNode);
    argNode->SetMorphed(this);

    arg->SetEarlyNode(argNode);
#endif // !FEATURE_FIXED_OUT_ARGS

    if (!arg->AbiInfo.IsPassedByReference())
    {
        bool morphed = fgTryMorphStructArg(arg);
        // Should always succeed for an unpromoted local.
        assert(morphed);
    }
}
