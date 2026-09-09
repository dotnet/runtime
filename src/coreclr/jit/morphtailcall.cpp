// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "jitpch.h"
#ifdef _MSC_VER
#pragma hdrstop
#endif

//------------------------------------------------------------------------
// fgCanFastTailCall: Check to see if this tail call can be optimized as epilog+jmp.
//
// Arguments:
//    callee - The callee to check
//    failReason - If this method returns false, the reason why. Can be nullptr.
//
// Return Value:
//    Returns true or false based on whether the callee can be fastTailCalled
//
// Notes:
//    This function is target specific and each target will make the fastTailCall
//    decision differently. See the notes below.
//
//    This function calls AddFinalArgsAndDetermineABIInfo to initialize the ABI
//    info, which is used to analyze the argument. This function can alter the
//    call arguments by adding argument IR nodes for non-standard arguments.
//
// Windows Amd64:
//    A fast tail call can be made whenever the number of callee arguments
//    is less than or equal to the number of caller arguments, or we have four
//    or fewer callee arguments. This is because, on Windows AMD64, each
//    argument uses exactly one register or one 8-byte stack slot. Thus, we only
//    need to count arguments, and not be concerned with the size of each
//    incoming or outgoing argument.
//
// Can fast tail call examples (amd64 Windows):
//
//    -- Callee will have all register arguments --
//    caller(int, int, int, int)
//    callee(int, int, float, int)
//
//    -- Callee requires stack space that is equal or less than the caller --
//    caller(struct, struct, struct, struct, struct, struct)
//    callee(int, int, int, int, int, int)
//
//    -- Callee requires stack space that is less than the caller --
//    caller(struct, double, struct, float, struct, struct)
//    callee(int, int, int, int, int)
//
//    -- Callee will have all register arguments --
//    caller(int)
//    callee(int, int, int, int)
//
// Cannot fast tail call examples (amd64 Windows):
//
//    -- Callee requires stack space that is larger than the caller --
//    caller(struct, double, struct, float, struct, struct)
//    callee(int, int, int, int, int, double, double, double)
//
//    -- Callee has a byref struct argument --
//    caller(int, int, int)
//    callee(struct(size 3 bytes))
//
// Unix Amd64 && Arm64:
//    A fastTailCall decision can be made whenever the callee's stack space is
//    less than or equal to the caller's stack space. There are many permutations
//    of when the caller and callee have different stack sizes if there are
//    structs being passed to either the caller or callee.
//
// Exceptions:
//    If the callee has a 9 to 16 byte struct argument and the callee has
//    stack arguments, the decision will be to not fast tail call. This is
//    because before fgMorphArgs is done, the struct is unknown whether it
//    will be placed on the stack or enregistered. Therefore, the conservative
//    decision of do not fast tail call is taken. This limitations should be
//    removed if/when fgMorphArgs no longer depends on fgCanFastTailCall.
//
// Can fast tail call examples (amd64 Unix):
//
//    -- Callee will have all register arguments --
//    caller(int, int, int, int)
//    callee(int, int, float, int)
//
//    -- Callee requires stack space that is equal to the caller --
//    caller({ long, long }, { int, int }, { int }, { int }, { int }, { int }) -- 6 int register arguments, 16 byte
//    stack
//    space
//    callee(int, int, int, int, int, int, int, int) -- 6 int register arguments, 16 byte stack space
//
//    -- Callee requires stack space that is less than the caller --
//    caller({ long, long }, int, { long, long }, int, { long, long }, { long, long }) 6 int register arguments, 32 byte
//    stack
//    space
//    callee(int, int, int, int, int, int, { long, long } ) // 6 int register arguments, 16 byte stack space
//
//    -- Callee will have all register arguments --
//    caller(int)
//    callee(int, int, int, int)
//
// Cannot fast tail call examples (amd64 Unix):
//
//    -- Callee requires stack space that is larger than the caller --
//    caller(float, float, float, float, float, float, float, float) -- 8 float register arguments
//    callee(int, int, int, int, int, int, int, int) -- 6 int register arguments, 16 byte stack space
//
//    -- Callee has structs which cannot be enregistered (Implementation Limitation) --
//    caller(float, float, float, float, float, float, float, float, { double, double, double }) -- 8 float register
//    arguments, 24 byte stack space
//    callee({ double, double, double }) -- 24 bytes stack space
//
//    -- Callee requires stack space and has a struct argument >8 bytes and <16 bytes (Implementation Limitation) --
//    caller(int, int, int, int, int, int, { double, double, double }) -- 6 int register arguments, 24 byte stack space
//    callee(int, int, int, int, int, int, { int, int }) -- 6 int registers, 16 byte stack space
//
//    -- Caller requires stack space and nCalleeArgs > nCallerArgs (Bug) --
//    caller({ double, double, double, double, double, double }) // 48 byte stack
//    callee(int, int) -- 2 int registers
//
bool Compiler::fgCanFastTailCall(GenTreeCall* callee, const char** failReason)
{
#if FEATURE_FASTTAILCALL

    // To reach here means that the return types of the caller and callee are tail call compatible.
    // In the case of structs that can be returned in a register, compRetNativeType is set to the actual return type.

#ifdef DEBUG
    if (callee->IsTailPrefixedCall())
    {
        var_types retType = info.compRetType;
        assert(impTailCallRetTypeCompatible(false, retType, info.compMethodInfo->args.retTypeClass, info.compCallConv,
                                            (var_types)callee->gtReturnType, callee->gtRetClsHnd,
                                            callee->GetUnmanagedCallConv()));
    }
#endif

    assert(!callee->gtArgs.AreArgsComplete());

    callee->gtArgs.AddFinalArgsAndDetermineABIInfo(this, callee);

    unsigned calleeArgStackSize = callee->gtArgs.OutgoingArgsStackSize();
    unsigned callerArgStackSize = roundUp(lvaParameterStackSize, TARGET_POINTER_SIZE);

    auto reportFastTailCallDecision = [&](const char* thisFailReason) {
        if (failReason != nullptr)
        {
            *failReason = thisFailReason;
        }

#ifdef DEBUG
        if ((JitConfig.JitReportFastTailCallDecisions()) == 1)
        {
            if (callee->gtCallType != CT_INDIRECT)
            {
                const char* methodName;

                methodName = eeGetMethodFullName(callee->gtCallMethHnd);

                printf("[Fast tailcall decision]: Caller: %s\n[Fast tailcall decision]: Callee: %s -- Decision: ",
                       info.compFullName, methodName);
            }
            else
            {
                printf("[Fast tailcall decision]: Caller: %s\n[Fast tailcall decision]: Callee: IndirectCall -- "
                       "Decision: ",
                       info.compFullName);
            }

            if (thisFailReason == nullptr)
            {
                printf("Will fast tailcall");
            }
            else
            {
                printf("Will not fast tailcall (%s)", thisFailReason);
            }

            printf(" (CallerArgStackSize: %d, CalleeArgStackSize: %d)\n\n", callerArgStackSize, calleeArgStackSize);
        }
        else
        {
            if (thisFailReason == nullptr)
            {
                JITDUMP("[Fast tailcall decision]: Will fast tailcall\n");
            }
            else
            {
                JITDUMP("[Fast tailcall decision]: Will not fast tailcall (%s)\n", thisFailReason);
            }
        }
#endif // DEBUG
    };

#if defined(TARGET_ARM) || defined(TARGET_RISCV64) || defined(TARGET_LOONGARCH64)
    for (CallArg& arg : callee->gtArgs.Args())
    {
        if (arg.AbiInfo.IsSplitAcrossRegistersAndStack())
        {
            reportFastTailCallDecision("Argument splitting in callee is not supported on " TARGET_READABLE_NAME);
            return false;
        }
    }

    for (unsigned lclNum = 0; lclNum < info.compArgsCount; lclNum++)
    {
        const ABIPassingInformation& abiInfo = lvaGetParameterABIInfo(lclNum);
        if (abiInfo.IsSplitAcrossRegistersAndStack())
        {
            reportFastTailCallDecision("Argument splitting in caller is not supported on " TARGET_READABLE_NAME);
            return false;
        }
    }
#endif // TARGET_ARM || TARGET_RISCV64 || defined(TARGET_LOONGARCH64)

#ifdef TARGET_ARM
    if (compIsProfilerHookNeeded())
    {
        reportFastTailCallDecision("Profiler is not supported on ARM32");
        return false;
    }

    // On ARM32 we have only one non-parameter volatile register and we need it
    // for the GS security cookie check. We could technically still tailcall
    // when the callee does not use all argument registers, but we keep the
    // code simple here.
    if (getNeedsGSSecurityCookie())
    {
        reportFastTailCallDecision("Not enough registers available due to the GS security cookie check");
        return false;
    }
#endif

    if (!opts.compFastTailCalls)
    {
        reportFastTailCallDecision("Configuration doesn't allow fast tail calls");
        return false;
    }

    if (callee->IsStressTailCall())
    {
        reportFastTailCallDecision("Fast tail calls are not performed under tail call stress");
        return false;
    }

#ifdef TARGET_ARM
    if (callee->IsR2RRelativeIndir() || callee->HasNonStandardAddedArgs(this))
    {
        reportFastTailCallDecision(
            "Method with non-standard args passed in callee saved register cannot be tail called");
        return false;
    }
#endif

    // Note on vararg methods:
    // If the caller is vararg method, we don't know the number of arguments passed by caller's caller.
    // But we can be sure that in-coming arg area of vararg caller would be sufficient to hold its
    // fixed args. Therefore, we can allow a vararg method to fast tail call other methods as long as
    // out-going area required for callee is bounded by caller's fixed argument space.
    //
    // Note that callee being a vararg method is not a problem since we can account the params being passed.
    //
    // We will currently decide to not fast tail call on Windows armarch if the caller or callee is a vararg
    // method. This is due to the ABI differences for native vararg methods for these platforms. There is
    // work required to shuffle arguments to the correct locations.

    if (TargetOS::IsWindows && TargetArchitecture::IsArmArch && (info.compIsVarArgs || callee->IsVarargs()))
    {
        reportFastTailCallDecision("Fast tail calls with varargs not supported on Windows ARM/ARM64");
        return false;
    }

    if (compLocallocUsed)
    {
        reportFastTailCallDecision("Localloc used");
        return false;
    }

    // If the NextCallReturnAddress intrinsic is used we should do normal calls.
    if (info.compHasNextCallRetAddr)
    {
        reportFastTailCallDecision("Uses NextCallReturnAddress intrinsic");
        return false;
    }

    if (callee->gtArgs.HasRetBuffer())
    {
        // If callee has RetBuf param, caller too must have it.
        // Otherwise go the slow route.
        if (info.compRetBuffArg == BAD_VAR_NUM)
        {
            reportFastTailCallDecision("Callee has RetBuf but caller does not.");
            return false;
        }
    }

    // For a fast tail call the caller will use its incoming arg stack space to place
    // arguments, so if the callee requires more arg stack space than is available here
    // the fast tail call cannot be performed. This is common to all platforms.
    // Note that the GC'ness of on stack args need not match since the arg setup area is marked
    // as non-interruptible for fast tail calls.
    //
    // Wasm passes args via fresh wasm locals, not the caller's stack, so this check doesn't apply.
#ifndef TARGET_WASM
    if (calleeArgStackSize > callerArgStackSize)
    {
        reportFastTailCallDecision("Not enough incoming arg space");
        return false;
    }
#endif // !TARGET_WASM

    // For Windows some struct parameters are copied on the local frame
    // and then passed by reference. We cannot fast tail call in these situation
    // as we need to keep our frame around.
    if (fgCallHasMustCopyByrefParameter(callee))
    {
        reportFastTailCallDecision("Callee has a byref parameter");
        return false;
    }

    reportFastTailCallDecision(nullptr);
    return true;
#else // FEATURE_FASTTAILCALL
    if (failReason)
        *failReason = "Fast tailcalls are not supported on this platform";
    return false;
#endif
}

#if FEATURE_FASTTAILCALL
//------------------------------------------------------------------------
// fgCallHasMustCopyByrefParameter: Check to see if this call has a byref parameter that
//                                  requires a struct copy in the caller.
//
// Arguments:
//    call - The call to check
//
// Return Value:
//    Returns true or false based on whether this call has a byref parameter that
//    requires a struct copy in the caller.
//
bool Compiler::fgCallHasMustCopyByrefParameter(GenTreeCall* call)
{
#if FEATURE_IMPLICIT_BYREFS
    for (CallArg& arg : call->gtArgs.Args())
    {
        if (fgCallArgWillPointIntoLocalFrame(call, arg))
        {
            return true;
        }
    }
#endif

    return false;
}

//------------------------------------------------------------------------
// fgCallArgWillPointIntoLocalFrame:
//    Check to see if a call arg will end up pointing into the local frame after morph.
//
// Arguments:
//    call - The call to check
//
// Return Value:
//    True if the arg will be passed as an implicit byref pointing to a local
//    on this function's frame; otherwise false.
//
// Remarks:
//    The logic here runs before relevant nodes have been morphed.
//
bool Compiler::fgCallArgWillPointIntoLocalFrame(GenTreeCall* call, CallArg& arg)
{
    if (!arg.AbiInfo.IsPassedByReference())
    {
        return false;
    }

    // If we're optimizing, we may be able to pass our caller's byref to our callee,
    // and so still be able to avoid a struct copy.
    if (opts.OptimizationDisabled())
    {
        return true;
    }

    // First, see if this arg is an implicit byref param.
    GenTreeLclVarCommon* const lcl = arg.GetNode()->IsImplicitByrefParameterValuePreMorph(this);

    if (lcl == nullptr)
    {
        return true;
    }

    // Yes, the arg is an implicit byref param.
    const unsigned   lclNum = lcl->GetLclNum();
    LclVarDsc* const varDsc = lvaGetDesc(lcl);

    // The param must not be promoted; if we've promoted, then the arg will be
    // a local struct assembled from the promoted fields.
    if (varDsc->lvPromoted)
    {
        JITDUMP("Arg [%06u] is promoted implicit byref V%02u, so no tail call\n", dspTreeID(arg.GetNode()), lclNum);

        return true;
    }

    assert(!varDsc->lvIsStructField);

    JITDUMP("Arg [%06u] is unpromoted implicit byref V%02u, seeing if we can still tail call\n",
            dspTreeID(arg.GetNode()), lclNum);

    GenTreeFlags deathFlags;
    if (varDsc->lvFieldLclStart != 0)
    {
        // Undone promotion case.
        deathFlags = lvaGetDesc(varDsc->lvFieldLclStart)->AllFieldDeathFlags();
    }
    else
    {
        deathFlags = GTF_VAR_DEATH;
    }

    if ((lcl->gtFlags & deathFlags) == deathFlags)
    {
        JITDUMP("... yes, arg is a last use\n");
        return false;
    }

    JITDUMP("... no, arg is not a last use\n");
    return true;
}

#endif

//------------------------------------------------------------------------
// fgMorphPotentialTailCall: Attempt to morph a call that the importer has
// identified as a potential tailcall to an actual tailcall and return the
// placeholder node to use in this case.
//
// Arguments:
//    call - The call to morph.
//
// Return Value:
//    Returns a node to use if the call was morphed into a tailcall. If this
//    function returns a node the call is done being morphed and the new node
//    should be used. Otherwise the call will have been demoted to a regular call
//    and should go through normal morph.
//
// Notes:
//    This is called only for calls that the importer has already identified as
//    potential tailcalls. It will do profitability and legality checks and
//    classify which kind of tailcall we are able to (or should) do, along with
//    modifying the trees to perform that kind of tailcall.
//
GenTree* Compiler::fgMorphPotentialTailCall(GenTreeCall* call)
{
    // It should either be an explicit (i.e. tail prefixed) or an implicit tail call
    assert(call->IsTailPrefixedCall() ^ call->IsImplicitTailCall());

    // It cannot be an inline candidate
    assert(!call->IsInlineCandidate());

    auto failTailCall = [&](const char* reason, unsigned lclNum = BAD_VAR_NUM) {
#ifdef DEBUG
        if (verbose)
        {
            printf("\nRejecting tail call in morph for call ");
            printTreeID(call);
            printf(": %s", reason);
            if (lclNum != BAD_VAR_NUM)
            {
                printf(" V%02u", lclNum);
            }
            printf("\n");
        }
#endif

        // for non user funcs, we have no handles to report
        info.compCompHnd->reportTailCallDecision(nullptr,
                                                 (call->gtCallType == CT_USER_FUNC) ? call->gtCallMethHnd : nullptr,
                                                 call->IsTailPrefixedCall(), TAILCALL_FAIL, reason);

        // We have checked the candidate so demote.
        call->gtCallMoreFlags &= ~GTF_CALL_M_EXPLICIT_TAILCALL;
#if FEATURE_TAILCALL_OPT
        call->gtCallMoreFlags &= ~GTF_CALL_M_IMPLICIT_TAILCALL;
#endif
    };

    if (call->IsSpecialIntrinsic())
    {
        failTailCall("Might turn into an intrinsic");
        return nullptr;
    }

    if (call->IsNoReturn() && !call->IsTailPrefixedCall())
    {
        // Such tail calls always throw an exception and we won't be able to see current
        // Caller() in the stacktrace.
        failTailCall("Never returns");
        return nullptr;
    }

#ifdef DEBUG
    if (opts.compGcChecks && (info.compRetType == TYP_REF))
    {
        failTailCall("DOTNET_JitGCChecks or stress might have interposed a call to CORINFO_HELP_CHECK_OBJ, "
                     "invalidating tailcall opportunity");
        return nullptr;
    }
#endif

    if (compIsAsync() != call->IsAsync())
    {
        failTailCall("Caller and callee do not agree on async-ness");
        return nullptr;
    }

    // We have to ensure to pass the incoming retValBuf as the
    // outgoing one. Using a temp will not do as this function will
    // not regain control to do the copy. This can happen when inlining
    // a tailcall which also has a potential tailcall in it: the IL looks
    // like we can do a tailcall, but the trees generated use a temp for the inlinee's
    // result. TODO-CQ: Fix this.
    if (info.compRetBuffArg != BAD_VAR_NUM)
    {
        noway_assert(call->TypeIs(TYP_VOID));
        noway_assert(call->gtArgs.HasRetBuffer());
        GenTree* retValBuf = call->gtArgs.GetRetBufferArg()->GetNode();
        if (!retValBuf->OperIs(GT_LCL_VAR) || retValBuf->AsLclVarCommon()->GetLclNum() != info.compRetBuffArg)
        {
            failTailCall("Need to copy return buffer");
            return nullptr;
        }
    }

    // We are still not sure whether it can be a tail call. Because, when converting
    // a call to an implicit tail call, we must check that there are no locals with
    // their address taken.  If this is the case, we have to assume that the address
    // has been leaked and the current stack frame must live until after the final
    // call.

    // Verify that none of vars has lvHasLdAddrOp or IsAddressExposed() bit set. Note
    // that lvHasLdAddrOp is much more conservative.  We cannot just base it on
    // IsAddressExposed() alone since it is not guaranteed to be set on all VarDscs
    // during morph stage. The reason for also checking IsAddressExposed() is that in case
    // of vararg methods user args are marked as addr exposed but not lvHasLdAddrOp.
    // The combination of lvHasLdAddrOp and IsAddressExposed() though conservative allows us
    // never to be incorrect.
    //
    // TODO-Throughput: have a compiler level flag to indicate whether method has vars whose
    // address is taken. Such a flag could be set whenever lvHasLdAddrOp or IsAddressExposed()
    // is set. This avoids the need for iterating through all lcl vars of the current
    // method.  Right now throughout the code base we are not consistently using 'set'
    // method to set lvHasLdAddrOp and IsAddressExposed() flags.

    bool isImplicitOrStressTailCall = call->IsImplicitTailCall() || call->IsStressTailCall();
    if (isImplicitOrStressTailCall && compLocallocUsed)
    {
        failTailCall("Localloc used");
        return nullptr;
    }

#ifdef DEBUG
    // For explicit tailcalls the importer will avoid inserting stress
    // poisoning after them. However, implicit tailcalls are marked earlier and
    // we must filter those out here if we ended up adding any poisoning IR
    // after them.
    if (isImplicitOrStressTailCall && compPoisoningAnyImplicitByrefs)
    {
        failTailCall("STRESS_POISON_IMPLICIT_BYREFS has introduced IR after tailcall opportunity, invalidating");
        return nullptr;
    }
#endif

    bool hasStructParam = false;
    for (unsigned varNum = 0; varNum < lvaCount; varNum++)
    {
        LclVarDsc* varDsc = lvaGetDesc(varNum);

        // If the method is marked as an explicit tail call we will skip the
        // following three hazard checks.
        // We still must check for any struct parameters and set 'hasStructParam'
        // so that we won't transform the recursive tail call into a loop.
        //
        if (isImplicitOrStressTailCall)
        {
            if (varDsc->IsAddressExposed())
            {
                if (lvaIsImplicitByRefLocal(varNum))
                {
                    // The address of the implicit-byref is a non-address use of the pointer parameter.
                }
                else if (varDsc->lvIsStructField && lvaIsImplicitByRefLocal(varDsc->lvParentLcl))
                {
                    // The address of the implicit-byref's field is likewise a non-address use of the pointer
                    // parameter.
                }
                else if (varDsc->lvPromoted && (lvaTable[varDsc->lvFieldLclStart].lvParentLcl != varNum))
                {
                    // This temp was used for struct promotion bookkeeping.  It will not be used, and will have
                    // its ref count and address-taken flag reset in fgMarkDemotedImplicitByRefArgs.
                    assert(lvaIsImplicitByRefLocal(lvaTable[varDsc->lvFieldLclStart].lvParentLcl));
                    assert(fgGlobalMorph);
                }
                else if (varDsc->IsStackAllocatedObject())
                {
                    // Stack allocated objects currently cannot be passed to callees
                    // so won't be live at tail call sites.
                }
#if FEATURE_FIXED_OUT_ARGS
                else if (varNum == lvaOutgoingArgSpaceVar)
                {
                    // The outgoing arg space is exposed only at callees, which is ok for our purposes.
                }
#endif
                else
                {
                    failTailCall("Local address taken", varNum);
                    return nullptr;
                }
            }
            if (varDsc->lvPinned)
            {
                // A tail call removes the method from the stack, which means the pinning
                // goes away for the callee.  We can't allow that.
                failTailCall("Has Pinned Vars", varNum);
                return nullptr;
            }
        }

        if (varTypeIsStruct(varDsc->TypeGet()) && varDsc->lvIsParam)
        {
            hasStructParam = true;
            // This prevents transforming a recursive tail call into a loop
            // but doesn't prevent tail call optimization so we need to
            // look at the rest of parameters.
        }
    }

    const char* failReason      = nullptr;
    bool        canFastTailCall = fgCanFastTailCall(call, &failReason);

    CORINFO_TAILCALL_HELPERS tailCallHelpers;
    bool                     tailCallViaJitHelper = false;
    if (!canFastTailCall)
    {
        if (call->IsImplicitTailCall())
        {
            // Implicit or opportunistic tail calls are always dispatched via fast tail call
            // mechanism and never via tail call helper for perf.
            failTailCall(failReason);
            return nullptr;
        }

        assert(call->IsTailPrefixedCall());
        assert(call->tailCallInfo != nullptr);

        // We do not currently handle non-standard args except for VSD stubs.
        if (!call->IsVirtualStub() && (call->HasNonStandardAddedArgs(this) ||
                                       (call->gtArgs.FindWellKnownArg(WellKnownArg::SecretStubParam) != nullptr)))
        {
            failTailCall(
                "Method with non-standard args passed in callee trash register cannot be tail called via helper");
            return nullptr;
        }

        // On x86 we have a faster mechanism than the general one which we use
        // in almost all cases. See fgCanTailCallViaJitHelper for more information.
        if (fgCanTailCallViaJitHelper(call))
        {
            tailCallViaJitHelper = true;
        }
        else
        {
            // Make sure we can get the helpers. We do this last as the runtime
            // will likely be required to generate these.
            CORINFO_RESOLVED_TOKEN* token = nullptr;
            CORINFO_SIG_INFO*       sig   = call->tailCallInfo->GetSig();
            unsigned                flags = 0;
            if (!call->tailCallInfo->IsCalli())
            {
                token = call->tailCallInfo->GetToken();
                if (call->tailCallInfo->IsCallvirt())
                {
                    flags |= CORINFO_TAILCALL_IS_CALLVIRT;
                }
            }

            if (call->gtArgs.HasThisPointer())
            {
                var_types thisArgType = call->gtArgs.GetThisArg()->GetNode()->TypeGet();
                if (thisArgType != TYP_REF)
                {
                    flags |= CORINFO_TAILCALL_THIS_ARG_IS_BYREF;
                }
            }

            if (!info.compCompHnd->getTailCallHelpers(token, sig, (CORINFO_GET_TAILCALL_HELPERS_FLAGS)flags,
                                                      &tailCallHelpers))
            {
                failTailCall("Tail call help not available");
                return nullptr;
            }
        }
    }

    // Check if we can make the tailcall a loop.
    bool fastTailCallToLoop = false;
#if FEATURE_TAILCALL_OPT
    // TODO-CQ: enable the transformation when the method has a struct parameter that can be passed in a register
    // or return type is a struct that can be passed in a register.
    //
    // TODO-CQ: if the method being compiled requires generic context reported in gc-info (either through
    // hidden generic context param or through keep alive thisptr), then while transforming a recursive
    // call to such a method requires that the generic context stored on stack slot be updated.  Right now,
    // fgMorphRecursiveFastTailCallIntoLoop() is not handling update of generic context while transforming
    // a recursive call into a loop.  Another option is to modify gtIsRecursiveCall() to check that the
    // generic type parameters of both caller and callee generic method are the same.
    //
    // For OSR, we prefer to tailcall for call counting + potential transition
    // into the actual tier1 version.
    //
    if (opts.compTailCallLoopOpt && canFastTailCall && !opts.IsOSR() && gtIsRecursiveCall(call) &&
        !lvaReportParamTypeArg() && !lvaKeepAliveAndReportThis() && !call->IsVirtual() && !hasStructParam &&
        !varTypeIsStruct(call->TypeGet()))
    {
        fastTailCallToLoop = true;
    }
#endif

    // Ok -- now we are committed to performing a tailcall. Report the decision.
    CorInfoTailCall tailCallResult;
    if (fastTailCallToLoop)
    {
        tailCallResult = TAILCALL_RECURSIVE;
    }
    else if (canFastTailCall)
    {
        tailCallResult = TAILCALL_OPTIMIZED;
    }
    else
    {
        tailCallResult = TAILCALL_HELPER;
    }

    info.compCompHnd->reportTailCallDecision(nullptr,
                                             (call->gtCallType == CT_USER_FUNC) ? call->gtCallMethHnd : nullptr,
                                             call->IsTailPrefixedCall(), tailCallResult, nullptr);

    // Do some profitability checks for whether we should expand a vtable call
    // target early. Note that we may already have expanded it due to GDV at
    // this point, so make sure we do not undo that work.
    //
    if (call->IsExpandedEarly() && call->IsVirtualVtable() && (call->gtControlExpr == nullptr))
    {
        assert(call->gtArgs.HasThisPointer());
        // It isn't always profitable to expand a virtual call early
        //
        // We always expand the TAILCALL_HELPER type late.
        // And we exapnd late when we have an optimized tail call
        // and the this pointer needs to be evaluated into a temp.
        //
        if (tailCallResult == TAILCALL_HELPER)
        {
            // We will always expand this late in lower instead.
            // (see LowerTailCallViaJitHelper as it needs some work
            // for us to be able to expand this earlier in morph)
            //
            call->ClearExpandedEarly();
        }
        else if ((tailCallResult == TAILCALL_OPTIMIZED) &&
                 ((call->gtArgs.GetThisArg()->GetNode()->gtFlags & GTF_SIDE_EFFECT) != 0))
        {
            // We generate better code when we expand this late in lower instead.
            //
            call->ClearExpandedEarly();
        }
    }

    // Now actually morph the call.
    compTailCallUsed = true;
    // This will prevent inlining this call.
    call->gtCallMoreFlags |= GTF_CALL_M_TAILCALL;
    if (tailCallViaJitHelper)
    {
        call->gtCallMoreFlags |= GTF_CALL_M_TAILCALL_VIA_JIT_HELPER;
    }

#if FEATURE_TAILCALL_OPT
    if (fastTailCallToLoop)
    {
        call->gtCallMoreFlags |= GTF_CALL_M_TAILCALL_TO_LOOP;
    }
#endif

    // Mark that this is no longer a pending tailcall. We need to do this before
    // we call fgMorphCall again (which happens in the fast tailcall case) to
    // avoid recursing back into this method.
    call->gtCallMoreFlags &= ~GTF_CALL_M_EXPLICIT_TAILCALL;
#if FEATURE_TAILCALL_OPT
    call->gtCallMoreFlags &= ~GTF_CALL_M_IMPLICIT_TAILCALL;
#endif

#ifdef DEBUG
    if (verbose)
    {
        printf("\nGTF_CALL_M_TAILCALL bit set for call ");
        printTreeID(call);
        printf("\n");
        if (fastTailCallToLoop)
        {
            printf("\nGTF_CALL_M_TAILCALL_TO_LOOP bit set for call ");
            printTreeID(call);
            printf("\n");
        }
    }
#endif

    // For R2R we might need a different entry point for this call if we are doing a tailcall.
    // The reason is that the normal delay load helper uses the return address to find the indirection
    // cell in xarch, but now the JIT is expected to leave the indirection cell in REG_R2R_INDIRECT_PARAM:
    // We optimize delegate invocations manually in the JIT so skip this for those.
    if (call->IsR2RRelativeIndir() && canFastTailCall && !fastTailCallToLoop && !call->IsDelegateInvoke())
    {
        info.compCompHnd->updateEntryPointForTailCall(&call->gtEntryPoint);

#ifdef TARGET_XARCH
        // We have already computed arg info to make the fast tailcall decision, but on X64 we now
        // have to pass the indirection cell, so redo arg info.
        call->gtArgs.ResetFinalArgsAndABIInfo();
#endif
    }

    fgValidateIRForTailCall(call);

    // If this block has a flow successor, make suitable updates.
    //
    if (compCurBB->KindIs(BBJ_ALWAYS))
    {
        BasicBlock* const curBlock    = compCurBB;
        BasicBlock* const targetBlock = curBlock->GetTarget();

        // Flow no longer reaches the target from here.
        //
        fgRemoveRefPred(curBlock->GetTargetEdge());

        // Adjust profile weights of the successor block.
        //
        // Note if this is a tail call to loop, further updates
        // are needed once we install the loop edge.
        //
        if (curBlock->hasProfileWeight() && targetBlock->hasProfileWeight())
        {
            targetBlock->decreaseBBProfileWeight(curBlock->bbWeight);

            if (targetBlock->NumSucc() > 0)
            {
                JITDUMP("Flow removal out of " FMT_BB " needs to be propagated. Data %s inconsistent.\n",
                        curBlock->bbNum, fgPgoConsistent ? "is now" : "was already");
                fgPgoConsistent = false;
            }
        }
    }
    else
    {
        // No unique successor. compCurBB should be a return.
        //
        assert(compCurBB->KindIs(BBJ_RETURN));
    }

#if !FEATURE_TAILCALL_OPT_SHARED_RETURN
    // We enable shared-ret tail call optimization for recursive calls even if
    // FEATURE_TAILCALL_OPT_SHARED_RETURN is not defined.
    if (gtIsRecursiveCall(call))
#endif
    {
        // Many tailcalls will have call and ret in the same block, and thus be
        // BBJ_RETURN, but if the call falls through to a ret, and we are doing a
        // tailcall, change it here.
        compCurBB->SetKindAndTargetEdge(BBJ_RETURN);
    }

    GenTree* stmtExpr = fgMorphStmt->GetRootNode();

#ifdef DEBUG
    // Tail call needs to be in one of the following IR forms
    //    Either a call stmt or
    //    GT_RETURN(GT_CALL(..)) or GT_RETURN(GT_CAST(GT_CALL(..)))
    //    var = GT_CALL(..) or var = (GT_CAST(GT_CALL(..)))
    //    GT_COMMA(GT_CALL(..), GT_NOP) or GT_COMMA(GT_CAST(GT_CALL(..)), GT_NOP)
    // In the above,
    //    GT_CASTS may be nested.
    genTreeOps stmtOper = stmtExpr->gtOper;
    if (stmtOper == GT_CALL)
    {
        assert(stmtExpr == call);
    }
    else
    {
        assert(stmtOper == GT_RETURN || stmtOper == GT_STORE_LCL_VAR || stmtOper == GT_COMMA);
        GenTree* treeWithCall;
        if (stmtOper == GT_RETURN)
        {
            treeWithCall = stmtExpr->gtGetOp1();
        }
        else if (stmtOper == GT_COMMA)
        {
            // Second operation must be nop.
            assert(stmtExpr->gtGetOp2()->IsNothingNode());
            treeWithCall = stmtExpr->gtGetOp1();
        }
        else
        {
            treeWithCall = stmtExpr->AsLclVar()->Data();
        }

        // Peel off casts
        while (treeWithCall->OperIs(GT_CAST))
        {
            assert(!treeWithCall->gtOverflow());
            treeWithCall = treeWithCall->gtGetOp1();
        }

        assert(treeWithCall == call);
    }
#endif
    // Store the call type for later to introduce the correct placeholder.
    var_types origCallType = call->TypeGet();

    GenTree* result;
    if (!canFastTailCall && !tailCallViaJitHelper)
    {
        // For tailcall via CORINFO_TAILCALL_HELPERS we transform into regular
        // calls with (to the JIT) regular control flow so we do not need to do
        // much special handling.
        result = fgMorphTailCallViaHelpers(call, tailCallHelpers);
    }
    else
    {
        // Otherwise we will transform into something that does not return. For
        // fast tailcalls a "jump" and for tailcall via JIT helper a call to a
        // JIT helper that does not return. So peel off everything after the
        // call.
        Statement* nextMorphStmt = fgMorphStmt->GetNextStmt();
        JITDUMP("Remove all stmts after the call.\n");
        while (nextMorphStmt != nullptr)
        {
            Statement* stmtToRemove = nextMorphStmt;
            nextMorphStmt           = stmtToRemove->GetNextStmt();
            fgRemoveStmt(compCurBB, stmtToRemove);
        }

        bool     isRootReplaced = false;
        GenTree* root           = fgMorphStmt->GetRootNode();

        if (root != call)
        {
            JITDUMP("Replace root node [%06d] with [%06d] tail call node.\n", dspTreeID(root), dspTreeID(call));
            isRootReplaced = true;
            fgMorphStmt->SetRootNode(call);
        }

        // Avoid potential extra work for the return (for example, vzeroupper)
        call->gtType = TYP_VOID;

        // The runtime requires that we perform a null check on the `this` argument before
        // tail calling to a virtual dispatch stub. This requirement is a consequence of limitations
        // in the runtime's ability to map an AV to a NullReferenceException if
        // the AV occurs in a dispatch stub that has unmanaged caller.
        if (call->IsVirtualStub())
        {
            call->gtFlags |= GTF_CALL_NULLCHECK;
        }

        // Do some target-specific transformations (before we process the args,
        // etc.) for the JIT helper case.
        if (tailCallViaJitHelper)
        {
            fgMorphTailCallViaJitHelper(call);

            // Force re-evaluating the argInfo. fgMorphTailCallViaJitHelper will modify the
            // argument list, invalidating the argInfo.
            call->gtArgs.ResetFinalArgsAndABIInfo();
        }

        // Tail call via JIT helper: The VM can't use return address hijacking
        // if we're not going to return and the helper doesn't have enough info
        // to safely poll, so we poll before the tail call, if the block isn't
        // already safe. Since tail call via helper is a slow mechanism it
        // doesn't matter whether we emit GC poll. This is done to be in parity
        // with Jit64. Also this avoids GC info size increase if all most all
        // methods are expected to be tail calls (e.g. F#).
        //
        // Note that we can avoid emitting GC-poll if we know that the current
        // BB is dominated by a Gc-SafePoint block. But we don't have dominator
        // info at this point. One option is to just add a place holder node for
        // GC-poll (e.g. GT_GCPOLL) here and remove it in lowering if the block
        // is dominated by a GC-SafePoint. For now it not clear whether
        // optimizing slow tail calls is worth the effort. As a low cost check,
        // we check whether the first and current basic blocks are
        // GC-SafePoints.
        //
        // Fast Tail call as epilog+jmp - No need to insert GC-poll. Instead,
        // fgSetBlockOrder() is going to mark the method as fully interruptible
        // if the block containing this tail call is reachable without executing
        // any call.
        if (canFastTailCall || fgFirstBB->HasFlag(BBF_GC_SAFE_POINT) || compCurBB->HasFlag(BBF_GC_SAFE_POINT))
        {
            // No gc poll needed
        }
        else
        {
            JITDUMP("Marking " FMT_BB " as needs gc poll\n", compCurBB->bbNum);
            compCurBB->SetFlags(BBF_NEEDS_GCPOLL);
            optMethodFlags |= OMF_NEEDS_GCPOLLS;
        }

        fgMorphCall(call);

        // Fast tail call: in case of fast tail calls, we need a jmp epilog and
        // hence mark it as BBJ_RETURN with BBF_JMP flag set.
        noway_assert(compCurBB->KindIs(BBJ_RETURN));
        if (canFastTailCall)
        {
            compCurBB->SetFlags(BBF_HAS_JMP);
        }
        else
        {
            // We call CORINFO_HELP_TAILCALL which does not return, so we will
            // not need epilogue.
            compCurBB->SetKindAndTargetEdge(BBJ_THROW);
        }

        if (isRootReplaced)
        {
            call->SetMorphed(this);

            // We have replaced the root node of this stmt and deleted the rest,
            // but we still have the deleted, dead nodes on the `fgMorph*` stack
            // if the root node was a store, `RET` or `CAST`.
            // Return a zero con node to exit morphing of the old trees without asserts
            // and forbid POST_ORDER morphing doing something wrong with our call.
            var_types zeroType = (origCallType == TYP_STRUCT) ? TYP_INT : genActualType(origCallType);
            result             = fgMorphTree(gtNewZeroConNode(zeroType));
        }
        else
        {
            result = call;
        }
    }

    return result;
}

//------------------------------------------------------------------------
// fgValidateIRForTailCall:
//     Validate that the IR looks ok to perform a tailcall.
//
// Arguments:
//     call - The call that we are dispatching as a tailcall.
//
// Notes:
//   This function needs to handle somewhat complex IR that appears after
//   tailcall candidates due to inlining.
//   Does not support checking struct returns since physical promotion can
//   create very hard to validate IR patterns.
//
void Compiler::fgValidateIRForTailCall(GenTreeCall* call)
{
#ifdef DEBUG
    if (call->TypeIs(TYP_STRUCT))
    {
        // Due to struct fields it can be very hard to track valid return
        // patterns; just give up on validating those.
        return;
    }

    class TailCallIRValidatorVisitor final : public GenTreeVisitor<TailCallIRValidatorVisitor>
    {
        GenTreeCall* m_tailcall;
        unsigned     m_lclNum;
        bool         m_active;

    public:
        enum
        {
            DoPostOrder       = true,
            UseExecutionOrder = true,
        };

        TailCallIRValidatorVisitor(Compiler* comp, GenTreeCall* tailcall)
            : GenTreeVisitor(comp)
            , m_tailcall(tailcall)
            , m_lclNum(BAD_VAR_NUM)
            , m_active(false)
        {
        }

        fgWalkResult PostOrderVisit(GenTree** use, GenTree* user)
        {
            GenTree* tree = *use;

            // Wait until we get to the actual call...
            if (!m_active)
            {
                if (tree == m_tailcall)
                {
                    m_active = true;
                }

                return WALK_CONTINUE;
            }

            if (tree->OperIs(GT_RETURN))
            {
                assert((tree->TypeIs(TYP_VOID) || ValidateUse(tree->gtGetOp1())) &&
                       "Expected return to be result of tailcall");
                return WALK_ABORT;
            }

            if (tree->OperIs(GT_NOP))
            {
                // GT_NOP might appear due to stores that end up as
                // self-stores, which get morphed to GT_NOP.
            }
            // We might see arbitrary chains of stores that trivially
            // propagate the result. Example:
            //
            //    *  STORE_LCL_VAR   ref    V05 tmp5
            //    \--*  CALL      ref    CultureInfo.InitializeUserDefaultUICulture
            // (in a new statement/BB)
            //    *  STORE_LCL_VAR   ref    V02 tmp2
            //    \--*  LCL_VAR   ref    V05 tmp5
            // (in a new statement/BB)
            //    *  RETURN    ref
            //    \--*  LCL_VAR   ref    V02 tmp2
            //
            else if (tree->OperIs(GT_STORE_LCL_VAR))
            {
                assert(ValidateUse(tree->AsLclVar()->Data()) && "Expected value of store to be result of tailcall");
                m_lclNum = tree->AsLclVar()->GetLclNum();
            }
            else if (tree->OperIs(GT_LCL_VAR))
            {
                assert(ValidateUse(tree) && "Expected use of local to be tailcall value");
            }
            else if (tree->OperIs(GT_CAST))
            {
                // The inliner can insert small-type-normalizing casts before the return
                // (int -> ubyte -> int for bool-return-calls, for example)
                // In the jit the callee is responsible for normalizing, so a tailcall
                // can freely bypass this extra cast
                assert(!m_compiler->fgCastNeeded(m_tailcall, tree->AsCast()->CastToType()) &&
                       ValidateUse(tree->AsCast()->CastOp()) && "Expected normalizing cast of tailcall result");
            }
            else if (IsCommaNop(tree))
            {
                // COMMA(NOP,NOP)
            }
            else
            {
                DISPTREE(tree);
                assert(!"Unexpected tree op after call marked as tailcall");
            }

            return WALK_CONTINUE;
        }

        bool IsCommaNop(GenTree* node)
        {
            if (!node->OperIs(GT_COMMA))
            {
                return false;
            }

            return node->AsOp()->gtGetOp1()->OperIs(GT_NOP) && node->AsOp()->gtGetOp2()->OperIs(GT_NOP);
        }

        bool ValidateUse(GenTree* node)
        {
            if (node->OperIs(GT_CAST))
            {
                node = node->AsCast()->CastOp();
            }

            if (m_lclNum != BAD_VAR_NUM)
            {
                return node->OperIs(GT_LCL_VAR) && (node->AsLclVar()->GetLclNum() == m_lclNum);
            }

            if (node == m_tailcall)
            {
                return true;
            }

            // If we do not use the call value directly we might have passed
            // this function's ret buffer arg, so verify that is being used.
            CallArg* retBufferArg = m_tailcall->gtArgs.GetRetBufferArg();
            if (retBufferArg != nullptr)
            {
                GenTree* retBufferNode = retBufferArg->GetNode();
                return retBufferNode->OperIs(GT_LCL_VAR) &&
                       (retBufferNode->AsLclVar()->GetLclNum() == m_compiler->info.compRetBuffArg) &&
                       node->OperIs(GT_LCL_VAR) && (node->AsLclVar()->GetLclNum() == m_compiler->info.compRetBuffArg);
            }

            return false;
        }
    };

    TailCallIRValidatorVisitor visitor(this, call);
    for (Statement* stmt = compCurStmt; stmt != nullptr; stmt = stmt->GetNextStmt())
    {
        visitor.WalkTree(stmt->GetRootNodePointer(), nullptr);
    }

    BasicBlock* bb = compCurBB;
    while (!bb->KindIs(BBJ_RETURN))
    {
        bb = bb->GetUniqueSucc();
        assert((bb != nullptr) && "Expected straight flow after tailcall");

        for (Statement* stmt : bb->Statements())
        {
            visitor.WalkTree(stmt->GetRootNodePointer(), nullptr);
        }
    }
#endif
}

//------------------------------------------------------------------------
// fgMorphTailCallViaHelpers: Transform the given GT_CALL tree for tailcall code
// generation.
//
// Arguments:
//     call - The call to transform
//     helpers - The tailcall helpers provided by the runtime.
//
// Return Value:
//    Returns the transformed node.
//
// Notes:
//   This transforms
//     GT_CALL
//         {callTarget}
//         {this}
//         {args}
//   into
//     GT_COMMA
//       GT_CALL StoreArgsStub
//         {callTarget}         (depending on flags provided by the runtime)
//         {this}               (as a regular arg)
//         {args}
//       GT_COMMA
//         GT_CALL Dispatcher
//           GT_LCL_ADDR ReturnAddress
//           {CallTargetStub}
//           GT_LCL_ADDR ReturnValue
//         GT_LCL ReturnValue
// whenever the call node returns a value. If the call node does not return a
// value the last comma will not be there.
//
GenTree* Compiler::fgMorphTailCallViaHelpers(GenTreeCall* call, CORINFO_TAILCALL_HELPERS& help)
{
    // R2R requires different handling but we don't support tailcall via
    // helpers in R2R yet, so just leave it for now.
    // TODO: R2R: TailCallViaHelper
    assert(!IsAot());

    JITDUMP("fgMorphTailCallViaHelpers (before):\n");
    DISPTREE(call);

    // Don't support tail calling helper methods
    assert(!call->IsHelperCall());

    // We come this route only for tail prefixed calls that cannot be dispatched as
    // fast tail calls
    assert(!call->IsImplicitTailCall());

    // We want to use the following assert, but it can modify the IR in some cases, so we
    // can't do that in an assert.
    // assert(!fgCanFastTailCall(call, nullptr));

    // We might or might not have called AddFinalArgsAndDetermineABIInfo before
    // this point: in builds with FEATURE_FASTTAILCALL we will have called it
    // when checking if we could do a fast tailcall, so it is possible we have
    // added extra IR for non-standard args that we must get rid of. Get rid of
    // the extra arguments here.
    call->gtArgs.ResetFinalArgsAndABIInfo();

    GenTree* callDispatcherAndGetResult = fgCreateCallDispatcherAndGetResult(call, help.hCallTarget, help.hDispatcher);

    // Change the call to a call to the StoreArgs stub.
    if (call->gtArgs.HasRetBuffer())
    {
        JITDUMP("Removing retbuf");

        call->gtArgs.Remove(call->gtArgs.GetRetBufferArg());
        call->gtCallMoreFlags &= ~GTF_CALL_M_RETBUFFARG;
    }

    const bool stubNeedsTargetFnPtr = (help.flags & CORINFO_TAILCALL_STORE_TARGET) != 0;

    GenTree* doBeforeStoreArgsStub = nullptr;
    GenTree* thisPtrStubArg        = nullptr;

    // Put 'this' in normal param list
    if (call->gtArgs.HasThisPointer())
    {
        JITDUMP("Moving this pointer into arg list\n");
        CallArg* thisArg = call->gtArgs.GetThisArg();
        GenTree* objp    = thisArg->GetNode();
        GenTree* thisPtr = nullptr;

        // JIT will need one or two copies of "this" in the following cases:
        //   1) the call needs null check;
        //   2) StoreArgs stub needs the target function pointer address and if the call is virtual
        //      the stub also needs "this" in order to evaluate the target.

        const bool callNeedsNullCheck = call->NeedsNullCheck();
        const bool stubNeedsThisPtr   = stubNeedsTargetFnPtr && call->IsVirtual();

        if (callNeedsNullCheck || stubNeedsThisPtr)
        {
            // Clone "this" if "this" has no side effects.
            if ((objp->gtFlags & GTF_SIDE_EFFECT) == 0)
            {
                thisPtr = gtClone(objp, true);
            }

            // Create a temp and spill "this" to the temp if "this" has side effects or "this" was too complex to clone.
            if (thisPtr == nullptr)
            {
                const unsigned lclNum = lvaGrabTemp(true DEBUGARG("tail call thisptr"));

                // tmp = "this"
                doBeforeStoreArgsStub = gtNewTempStore(lclNum, objp);

                if (callNeedsNullCheck)
                {
                    // COMMA(tmp = "this", deref(tmp))
                    GenTree* tmp          = gtNewLclvNode(lclNum, objp->TypeGet());
                    GenTree* nullcheck    = gtNewNullCheck(tmp);
                    doBeforeStoreArgsStub = gtNewOperNode(GT_COMMA, TYP_VOID, doBeforeStoreArgsStub, nullcheck);
                }

                thisPtr = gtNewLclvNode(lclNum, objp->TypeGet());

                if (stubNeedsThisPtr)
                {
                    thisPtrStubArg = gtNewLclvNode(lclNum, objp->TypeGet());
                }
            }
            else
            {
                if (callNeedsNullCheck)
                {
                    // deref("this")
                    doBeforeStoreArgsStub = gtNewNullCheck(objp);

                    if (stubNeedsThisPtr)
                    {
                        thisPtrStubArg = gtClone(objp, true);
                    }
                }
                else
                {
                    assert(stubNeedsThisPtr);

                    thisPtrStubArg = objp;
                }
            }

            call->gtFlags &= ~GTF_CALL_NULLCHECK;

            assert((thisPtrStubArg != nullptr) == stubNeedsThisPtr);
        }
        else
        {
            thisPtr = objp;
        }

        // During rationalization tmp="this" and null check will be materialized
        // in the right execution order.
        call->gtArgs.PushFront(this, NewCallArg::Primitive(thisPtr, thisArg->GetSignatureType()));
        call->gtArgs.Remove(thisArg);
    }

    // We may need to pass the target, for instance for calli or generic methods
    // where we pass instantiating stub.
    if (stubNeedsTargetFnPtr)
    {
        JITDUMP("Adding target since VM requested it\n");
        GenTree* target;
        if (!call->IsVirtual())
        {
            if (call->gtCallType == CT_INDIRECT)
            {
                noway_assert(call->gtControlExpr != nullptr);
                target = call->gtControlExpr;
            }
            else
            {
                CORINFO_CONST_LOOKUP addrInfo;
                info.compCompHnd->getFunctionEntryPoint(call->gtCallMethHnd, &addrInfo);
                target = gtNewIconEmbHndNode(&addrInfo, GTF_ICON_FTN_ADDR, call->gtCallMethHnd);
            }
        }
        else
        {
            assert(!call->tailCallInfo->GetSig()->hasTypeArg());

            CORINFO_CALL_INFO callInfo;
            unsigned          flags = CORINFO_CALLINFO_LDFTN;
            if (call->tailCallInfo->IsCallvirt())
            {
                flags |= CORINFO_CALLINFO_CALLVIRT;
            }

            eeGetCallInfo(call->tailCallInfo->GetToken(), nullptr, (CORINFO_CALLINFO_FLAGS)flags, &callInfo);
            target = getVirtMethodPointerTree(thisPtrStubArg, call->tailCallInfo->GetToken(), &callInfo);
        }

        call->gtArgs.PushBack(this, NewCallArg::Primitive(target));
    }

    // This is now a direct call to the store args stub and not a tailcall.
    call->gtCallType    = CT_USER_FUNC;
    call->gtControlExpr = nullptr;
    call->gtCallMethHnd = help.hStoreArgs;
    call->gtFlags &= ~GTF_CALL_VIRT_KIND_MASK;
    call->gtCallMoreFlags &= ~(GTF_CALL_M_TAILCALL | GTF_CALL_M_DELEGATE_INV);

    // The store-args stub returns no value.
    call->gtRetClsHnd  = nullptr;
    call->gtType       = TYP_VOID;
    call->gtReturnType = TYP_VOID;

    GenTree* callStoreArgsStub = call;

    if (doBeforeStoreArgsStub != nullptr)
    {
        callStoreArgsStub = gtNewOperNode(GT_COMMA, TYP_VOID, doBeforeStoreArgsStub, callStoreArgsStub);
    }

    GenTree* finalTree =
        gtNewOperNode(GT_COMMA, callDispatcherAndGetResult->TypeGet(), callStoreArgsStub, callDispatcherAndGetResult);

    finalTree = fgMorphTree(finalTree);

    JITDUMP("fgMorphTailCallViaHelpers (after):\n");
    DISPTREE(finalTree);
    return finalTree;
}

//------------------------------------------------------------------------
// fgCreateCallDispatcherAndGetResult: Given a call
// CALL
//   {callTarget}
//   {retbuf}
//   {this}
//   {args}
// create a similarly typed node that calls the tailcall dispatcher and returns
// the result, as in the following:
// COMMA
//   CALL TailCallDispatcher
//     ADDR ReturnAddress
//     &CallTargetFunc
//     ADDR RetValue
//   RetValue
// If the call has type TYP_VOID, only create the CALL node.
//
// Arguments:
//    origCall - the call
//    callTargetStubHnd - the handle of the CallTarget function (this is a special
//    IL stub created by the runtime)
//    dispatcherHnd - the handle of the tailcall dispatcher function
//
// Return Value:
//    A node that can be used in place of the original call.
//
GenTree* Compiler::fgCreateCallDispatcherAndGetResult(GenTreeCall*          origCall,
                                                      CORINFO_METHOD_HANDLE callTargetStubHnd,
                                                      CORINFO_METHOD_HANDLE dispatcherHnd)
{
    GenTreeCall* callDispatcherNode = gtNewUserCallNode(dispatcherHnd, TYP_VOID, fgMorphStmt->GetDebugInfo());
    // The dispatcher has signature
    // void DispatchTailCalls(void* callersRetAddrSlot, void* callTarget, ref byte retValue)

    // Add return value arg.
    GenTree*     retValArg;
    GenTree*     retVal    = nullptr;
    unsigned int newRetLcl = BAD_VAR_NUM;

    if (origCall->gtArgs.HasRetBuffer())
    {
        JITDUMP("Transferring retbuf\n");
        GenTree* retBufArg = origCall->gtArgs.GetRetBufferArg()->GetNode();

        assert(info.compRetBuffArg != BAD_VAR_NUM);
        assert(retBufArg->OperIsLocal());
        assert(retBufArg->AsLclVarCommon()->GetLclNum() == info.compRetBuffArg);

        retValArg = retBufArg;

        if (!origCall->TypeIs(TYP_VOID))
        {
            retVal = gtClone(retBufArg);
        }
    }
    else if (!origCall->TypeIs(TYP_VOID))
    {
        JITDUMP("Creating a new temp for the return value\n");
        newRetLcl = lvaGrabTemp(false DEBUGARG("Return value for tail call dispatcher"));
        if (varTypeIsStruct(origCall->gtType))
        {
            lvaSetStruct(newRetLcl, origCall->gtRetClsHnd, false);
        }
        else
        {
            // Since we pass a reference to the return value to the dispatcher
            // we need to use the real return type so we can normalize it on
            // load when we return it.
            lvaTable[newRetLcl].lvType = (var_types)origCall->gtReturnType;
        }

        lvaSetVarAddrExposed(newRetLcl DEBUGARG(AddressExposedReason::DISPATCH_RET_BUF));

        if (varTypeIsStruct(origCall) && compMethodReturnsMultiRegRetType())
        {
            lvaGetDesc(newRetLcl)->lvIsMultiRegRet = true;
        }

        retValArg = gtNewLclVarAddrNode(newRetLcl);
        retVal    = gtNewLclvNode(newRetLcl, genActualType(lvaTable[newRetLcl].lvType));
    }
    else
    {
        JITDUMP("No return value so using null pointer as arg\n");
        retValArg = gtNewZeroConNode(TYP_I_IMPL);
    }

    // Args are (void** callersReturnAddressSlot, void* callTarget, ref byte retVal)
    GenTree* callTarget = new (this, GT_FTN_ADDR) GenTreeFptrVal(TYP_I_IMPL, callTargetStubHnd);

    // Add the caller's return address slot.
    if (lvaRetAddrVar == BAD_VAR_NUM)
    {
        lvaRetAddrVar                  = lvaGrabTemp(false DEBUGARG("Return address"));
        lvaTable[lvaRetAddrVar].lvType = TYP_I_IMPL;
        lvaSetVarAddrExposed(lvaRetAddrVar DEBUGARG(AddressExposedReason::DISPATCH_RET_BUF));
    }

    GenTree* retAddrSlot = gtNewLclVarAddrNode(lvaRetAddrVar);

    NewCallArg retAddrSlotArg = NewCallArg::Primitive(retAddrSlot);
    NewCallArg callTargetArg  = NewCallArg::Primitive(callTarget);
    NewCallArg retValCallArg  = NewCallArg::Primitive(retValArg);
    callDispatcherNode->gtArgs.PushFront(this, retAddrSlotArg, callTargetArg, retValCallArg);

    if (origCall->TypeIs(TYP_VOID))
    {
        return callDispatcherNode;
    }

    assert(retVal != nullptr);
    GenTree* comma = gtNewOperNode(GT_COMMA, origCall->TypeGet(), callDispatcherNode, retVal);

    // The JIT seems to want to CSE this comma and messes up multi-reg ret
    // values in the process. Just avoid CSE'ing this tree entirely in that
    // case.
    if (origCall->HasMultiRegRetVal())
    {
        comma->gtFlags |= GTF_DONT_CSE;
    }

    return comma;
}

//------------------------------------------------------------------------
// getLookupTree: get a lookup tree
//
// Arguments:
//    pLookup - the lookup to get the tree for
//    handleFlags - flags to set on the result node
//    compileTimeHandle - compile-time handle corresponding to the lookup
//
// Return Value:
//    A node representing the lookup tree
//
GenTree* Compiler::getLookupTree(CORINFO_LOOKUP* pLookup, GenTreeFlags handleFlags, void* compileTimeHandle)
{
    if (!pLookup->lookupKind.needsRuntimeLookup)
    {
        // No runtime lookup is required.
        // Access is direct or memory-indirect (of a fixed address) reference

        CORINFO_GENERIC_HANDLE handle       = nullptr;
        void*                  pIndirection = nullptr;
        assert(pLookup->constLookup.accessType != IAT_PPVALUE && pLookup->constLookup.accessType != IAT_RELPVALUE);

        if (pLookup->constLookup.accessType == IAT_VALUE)
        {
            handle = pLookup->constLookup.handle;
        }
        else if (pLookup->constLookup.accessType == IAT_PVALUE)
        {
            pIndirection = pLookup->constLookup.addr;
        }

        return gtNewIconEmbHndNode(handle, pIndirection, handleFlags, compileTimeHandle);
    }

    return getRuntimeLookupTree(pLookup, compileTimeHandle);
}

//------------------------------------------------------------------------
// getRuntimeLookupTree: get a tree for a runtime lookup
//
// Arguments:
//    pLookup - the lookup to get the tree for
//    compileTimeHandle - compile-time handle corresponding to the lookup
//
// Return Value:
//    A node representing the runtime lookup tree
//
GenTree* Compiler::getRuntimeLookupTree(CORINFO_LOOKUP* pLookup, void* compileTimeHandle)
{
    CORINFO_RUNTIME_LOOKUP* pRuntimeLookup = &pLookup->runtimeLookup;

    // If pRuntimeLookup->indirections is equal to CORINFO_USEHELPER, it specifies that a run-time helper should be
    // used; otherwise, it specifies the number of indirections via pRuntimeLookup->offsets array.
    if ((pRuntimeLookup->indirections == CORINFO_USEHELPER) || (pRuntimeLookup->indirections == CORINFO_USENULL) ||
        pRuntimeLookup->testForNull)
    {
        return gtNewRuntimeLookupHelperCallNode(pRuntimeLookup,
                                                getRuntimeContextTree(pLookup->lookupKind.runtimeLookupKind),
                                                compileTimeHandle);
    }

    GenTree* result = getRuntimeContextTree(pLookup->lookupKind.runtimeLookupKind);

    ArrayStack<GenTree*> stmts(getAllocator(CMK_ArrayStack));

    auto cloneTree = [&](GenTree** tree DEBUGARG(const char* reason)) -> GenTree* {
        if (!((*tree)->gtFlags & GTF_GLOB_EFFECT))
        {
            GenTree* clone = gtClone(*tree, true);

            if (clone)
            {
                return clone;
            }
        }

        unsigned temp = lvaGrabTemp(true DEBUGARG(reason));
        stmts.Push(gtNewTempStore(temp, *tree));
        *tree = gtNewLclvNode(temp, lvaGetActualType(temp));
        return gtNewLclvNode(temp, lvaGetActualType(temp));
    };

    // Apply repeated indirections
    for (WORD i = 0; i < pRuntimeLookup->indirections; i++)
    {
        GenTree* preInd = nullptr;
        if ((i == 1 && pRuntimeLookup->indirectFirstOffset) || (i == 2 && pRuntimeLookup->indirectSecondOffset))
        {
            preInd = cloneTree(&result DEBUGARG("getRuntimeLookupTree indirectOffset"));
        }

        if (i != 0)
        {
            result = gtNewIndir(TYP_I_IMPL, result, GTF_IND_NONFAULTING | GTF_IND_INVARIANT);
        }

        if ((i == 1 && pRuntimeLookup->indirectFirstOffset) || (i == 2 && pRuntimeLookup->indirectSecondOffset))
        {
            result = gtNewOperNode(GT_ADD, TYP_I_IMPL, preInd, result);
        }

        if (pRuntimeLookup->offsets[i] != 0)
        {
            result = gtNewOperNode(GT_ADD, TYP_I_IMPL, result, gtNewIconNode(pRuntimeLookup->offsets[i], TYP_I_IMPL));
        }
    }

    assert(!pRuntimeLookup->testForNull);
    if (pRuntimeLookup->indirections > 0)
    {
        result = gtNewIndir(TYP_I_IMPL, result, GTF_IND_NONFAULTING);
    }

    // Produces GT_COMMA(stmt1, GT_COMMA(stmt2, ... GT_COMMA(stmtN, result)))

    while (!stmts.Empty())
    {
        result = gtNewOperNode(GT_COMMA, TYP_I_IMPL, stmts.Pop(), result);
    }

    DISPTREE(result);
    return result;
}

//------------------------------------------------------------------------
// getVirtMethodPointerTree: get a tree for a virtual method pointer
//
// Arguments:
//    thisPtr - tree representing `this` pointer
//    pResolvedToken - pointer to the resolved token of the method
//    pCallInfo - pointer to call info
//
// Return Value:
//    A node representing the virtual method pointer

GenTree* Compiler::getVirtMethodPointerTree(GenTree*                thisPtr,
                                            CORINFO_RESOLVED_TOKEN* pResolvedToken,
                                            CORINFO_CALL_INFO*      pCallInfo)
{
    GenTree* exactMethodDesc = getTokenHandleTree(pResolvedToken, false);
    GenTree* exactTypeDesc   = getTokenHandleTree(pResolvedToken, true);

    return gtNewVirtualFunctionLookupHelperCallNode(CORINFO_HELP_VIRTUAL_FUNC_PTR, TYP_I_IMPL, thisPtr, exactMethodDesc,
                                                    exactTypeDesc);
}

//------------------------------------------------------------------------
// getTokenHandleTree: get a handle tree for a token. This method should never
//    be called for tokens imported from inlinees.
//
// Arguments:
//    pResolvedToken - token to get a handle for
//    parent - whether parent should be imported
//
// Return Value:
//    A node representing the virtual method pointer

GenTree* Compiler::getTokenHandleTree(CORINFO_RESOLVED_TOKEN* pResolvedToken, bool parent)
{
    CORINFO_GENERICHANDLE_RESULT embedInfo;

    // NOTE: inlining is done at this point, so we don't know which method contained this token.
    // It's fine because currently this is never used for something that belongs to an inlinee.
    // Namely, we currently use it for:
    //   1) Methods with EH are never inlined
    //   2) Methods with explicit tail calls are never inlined
    //
    info.compCompHnd->embedGenericHandle(pResolvedToken, parent, info.compMethodHnd, &embedInfo);

    GenTree* result =
        getLookupTree(&embedInfo.lookup, gtTokenToIconFlags(pResolvedToken->token), embedInfo.compileTimeHandle);

    // If we have a result and it requires runtime lookup, wrap it in a runtime lookup node.
    if ((result != nullptr) && embedInfo.lookup.lookupKind.needsRuntimeLookup)
    {
        result = gtNewRuntimeLookup(embedInfo.compileTimeHandle, embedInfo.handleType, result);
    }

    return result;
}

/*****************************************************************************
 *
 *  Transform the given GT_CALL tree for tail call via JIT helper.
 */
void Compiler::fgMorphTailCallViaJitHelper(GenTreeCall* call)
{
    JITDUMP("fgMorphTailCallViaJitHelper (before):\n");
    DISPTREE(call);

    // For the helper-assisted tail calls, we need to push all the arguments
    // into a single list, and then add a few extra at the beginning or end.
    //
    // For Windows x86, the tailcall helper is defined as:
    //
    //      JIT_TailCall(<function args>, int numberOfOldStackArgsWords, int numberOfNewStackArgsWords, int flags, void*
    //      callTarget)
    //
    // Note that the special arguments are on the stack, whereas the function arguments follow
    // the normal convention: there might be register arguments in ECX and EDX. The stack will
    // look like (highest address at the top):
    //      first normal stack argument
    //      ...
    //      last normal stack argument
    //      numberOfOldStackArgs
    //      numberOfNewStackArgs
    //      flags
    //      callTarget
    //
    // Each special arg is 4 bytes.
    //
    // 'flags' is a bitmask where:
    //      1 == restore callee-save registers (EDI,ESI,EBX). The JIT always saves all
    //          callee-saved registers for tailcall functions. Note that the helper assumes
    //          that the callee-saved registers live immediately below EBP, and must have been
    //          pushed in this order: EDI, ESI, EBX.
    //      2 == call target is a virtual stub dispatch.
    //
    // The x86 tail call helper lives in VM\i386\jithelp.asm. See that function for more details
    // on the custom calling convention.

    // Check for PInvoke call types that we don't handle in codegen yet.
    assert(!call->IsUnmanaged());

    // Don't support tail calling helper methods
    assert(!call->IsHelperCall());

    // We come this route only for tail prefixed calls that cannot be dispatched as
    // fast tail calls
    assert(!call->IsImplicitTailCall());

    // We want to use the following assert, but it can modify the IR in some cases, so we
    // can't do that in an assert.
    // assert(!fgCanFastTailCall(call, nullptr));

    // First move the 'this' pointer (if any) onto the regular arg list. We do this because
    // we are going to prepend special arguments onto the argument list (for non-x86 platforms),
    // and thus shift where the 'this' pointer will be passed to a later argument slot. In
    // addition, for all platforms, we are going to change the call into a helper call. Our code
    // generation code for handling calls to helpers does not handle 'this' pointers. So, when we
    // do this transformation, we must explicitly create a null 'this' pointer check, if required,
    // since special 'this' pointer handling will no longer kick in.
    //
    // Some call types, such as virtual vtable calls, require creating a call address expression
    // that involves the "this" pointer. Lowering will sometimes create an embedded statement
    // to create a temporary that is assigned to the "this" pointer expression, and then use
    // that temp to create the call address expression. This temp creation embedded statement
    // will occur immediately before the "this" pointer argument, and then will be used for both
    // the "this" pointer argument as well as the call address expression. In the normal ordering,
    // the embedded statement establishing the "this" pointer temp will execute before both uses
    // of the temp. However, for tail calls via a helper, we move the "this" pointer onto the
    // normal call argument list, and insert a placeholder which will hold the call address
    // expression. For non-x86, things are ok, because the order of execution of these is not
    // altered. However, for x86, the call address expression is inserted as the *last* argument
    // in the argument list, *after* the "this" pointer. It will be put on the stack, and be
    // evaluated first. To ensure we don't end up with out-of-order temp definition and use,
    // for those cases where call lowering creates an embedded form temp of "this", we will
    // create a temp here, early, that will later get morphed correctly.

    CallArg* thisArg = call->gtArgs.GetThisArg();
    if (thisArg != nullptr)
    {
        GenTree* thisPtr = nullptr;
        GenTree* objp    = thisArg->GetNode();

        if ((call->IsDelegateInvoke() || call->IsVirtualVtable()) && !objp->OperIs(GT_LCL_VAR))
        {
            // tmp = "this"
            unsigned lclNum = lvaGrabTemp(true DEBUGARG("tail call thisptr"));
            GenTree* store  = gtNewTempStore(lclNum, objp);

            // COMMA(tmp = "this", tmp)
            var_types vt  = objp->TypeGet();
            GenTree*  tmp = gtNewLclvNode(lclNum, vt);
            thisPtr       = gtNewOperNode(GT_COMMA, vt, store, tmp);

            objp = thisPtr;
        }

        if (call->NeedsNullCheck())
        {
            // clone "this" if "this" has no side effects.
            if ((thisPtr == nullptr) && !(objp->gtFlags & GTF_SIDE_EFFECT))
            {
                thisPtr = gtClone(objp, true);
            }

            var_types vt = objp->TypeGet();
            if (thisPtr == nullptr)
            {
                // create a temp if either "this" has side effects or "this" is too complex to clone.

                // tmp = "this"
                unsigned lclNum = lvaGrabTemp(true DEBUGARG("tail call thisptr"));
                GenTree* store  = gtNewTempStore(lclNum, objp);

                // COMMA(tmp = "this", deref(tmp))
                GenTree* tmp       = gtNewLclvNode(lclNum, vt);
                GenTree* nullcheck = gtNewNullCheck(tmp);
                store              = gtNewOperNode(GT_COMMA, TYP_VOID, store, nullcheck);

                // COMMA(COMMA(tmp = "this", deref(tmp)), tmp)
                thisPtr = gtNewOperNode(GT_COMMA, vt, store, gtNewLclvNode(lclNum, vt));
            }
            else
            {
                // thisPtr = COMMA(deref("this"), "this")
                GenTree* nullcheck = gtNewNullCheck(thisPtr);
                thisPtr            = gtNewOperNode(GT_COMMA, vt, nullcheck, gtClone(objp, true));
            }

            call->gtFlags &= ~GTF_CALL_NULLCHECK;
        }
        else
        {
            thisPtr = objp;
        }

        // TODO-Cleanup: we leave it as a virtual stub call to
        // use logic in `LowerVirtualStubCall`, clear GTF_CALL_VIRT_KIND_MASK here
        // and change `LowerCall` to recognize it as a direct call.

        // During rationalization tmp="this" and null check will
        // materialize as embedded stmts in right execution order.
        assert(thisPtr != nullptr);
        call->gtArgs.PushFront(this, NewCallArg::Primitive(thisPtr, thisArg->GetSignatureType()));
        call->gtArgs.Remove(thisArg);
    }

    unsigned nOldStkArgsWords = lvaParameterStackSize / REGSIZE_BYTES;
    GenTree* arg3Node         = gtNewIconNode((ssize_t)nOldStkArgsWords, TYP_I_IMPL);
    CallArg* arg3 =
        call->gtArgs.PushBack(this, NewCallArg::Primitive(arg3Node).WellKnown(WellKnownArg::X86TailCallSpecialArg));
    // Inject a placeholder for the count of outgoing stack arguments that the Lowering phase will generate.
    // The constant will be replaced.
    GenTree* arg2Node = gtNewIconNode(9, TYP_I_IMPL);
    CallArg* arg2 =
        call->gtArgs.InsertAfter(this, arg3,
                                 NewCallArg::Primitive(arg2Node).WellKnown(WellKnownArg::X86TailCallSpecialArg));
    // Inject a placeholder for the flags.
    // The constant will be replaced.
    GenTree* arg1Node = gtNewIconNode(8, TYP_I_IMPL);
    CallArg* arg1 =
        call->gtArgs.InsertAfter(this, arg2,
                                 NewCallArg::Primitive(arg1Node).WellKnown(WellKnownArg::X86TailCallSpecialArg));
    // Inject a placeholder for the real call target that the Lowering phase will generate.
    // The constant will be replaced.
    GenTree* arg0Node = gtNewIconNode(7, TYP_I_IMPL);
    CallArg* arg0 =
        call->gtArgs.InsertAfter(this, arg1,
                                 NewCallArg::Primitive(arg0Node).WellKnown(WellKnownArg::X86TailCallSpecialArg));

    // It is now a varargs tail call.
    call->gtArgs.SetIsVarArgs();
    call->gtFlags &= ~GTF_CALL_POP_ARGS;

    // The function is responsible for doing explicit null check when it is necessary.
    assert(!call->NeedsNullCheck());

    JITDUMP("fgMorphTailCallViaJitHelper (after):\n");
    DISPTREE(call);
}

//------------------------------------------------------------------------
// fgGetStubAddrArg: Return the virtual stub address for the given call.
//
// Notes:
//    the JIT must place the address of the stub used to load the call target,
//    the "stub indirection cell", in special call argument with special register.
//
// Arguments:
//    call - a call that needs virtual stub dispatching.
//
// Return Value:
//    addr tree
//
GenTree* Compiler::fgGetStubAddrArg(GenTreeCall* call)
{
    assert(call->IsVirtualStub());
    GenTree* stubAddrArg;
    if (call->gtCallType == CT_INDIRECT)
    {
        stubAddrArg = gtClone(call->gtControlExpr, true);
    }
    else
    {
        assert(call->gtCallMoreFlags & GTF_CALL_M_VIRTSTUB_REL_INDIRECT);
        ssize_t addr = ssize_t(call->gtStubCallStubAddr);
        stubAddrArg  = gtNewIconHandleNode(addr, GTF_ICON_FTN_ADDR);
        INDEBUG(stubAddrArg->AsIntCon()->SetTargetHandle((size_t)call->gtCallMethHnd));
    }
    assert(stubAddrArg != nullptr);
    return stubAddrArg;
}

//------------------------------------------------------------------------------
// fgGetArgTabEntryParameterLclNum : Get the lcl num for the parameter that
// corresponds to the argument to a recursive call.
//
// Notes:
//    Due to non-standard args this is not just the index of the argument in
//    the arg list. For example, in R2R compilations we will have added a
//    non-standard arg for the R2R indirection cell.
//
// Arguments:
//    arg  - the arg
//
unsigned Compiler::fgGetArgParameterLclNum(GenTreeCall* call, CallArg* arg)
{
    unsigned num = 0;

    for (CallArg& otherArg : call->gtArgs.Args())
    {
        if (&otherArg == arg)
        {
            break;
        }

        // Late added args add extra args that do not map to IL parameters and that we should not reassign.
        if (!otherArg.IsArgAddedLate())
        {
            num++;
        }
    }

    return num;
}

//------------------------------------------------------------------------------
// fgMorphRecursiveFastTailCallIntoLoop : Transform a recursive fast tail call into a loop.
//
//
// Arguments:
//    block  - basic block ending with a recursive fast tail call
//    recursiveTailCall - recursive tail call to transform
//
// Notes:
//    The legality of the transformation is ensured by the checks in endsWithTailCallConvertibleToLoop.

void Compiler::fgMorphRecursiveFastTailCallIntoLoop(BasicBlock* block, GenTreeCall* recursiveTailCall)
{
    assert(recursiveTailCall->IsTailCallConvertibleToLoop());
    Statement* lastStmt = block->lastStmt();
    assert(recursiveTailCall == lastStmt->GetRootNode());

    // Transform recursive tail call into a loop.

    Statement*       earlyArgInsertionPoint = lastStmt;
    const DebugInfo& callDI                 = lastStmt->GetDebugInfo();

    // All arguments whose trees may involve caller parameter local variables need to be assigned to temps first;
    // then the temps need to be assigned to the method parameters. This is done so that the caller
    // parameters are not re-assigned before call arguments depending on them  are evaluated.
    // tmpAssignmentInsertionPoint and paramAssignmentInsertionPoint keep track of
    // where the next temp or parameter assignment should be inserted.

    // In the example below the first call argument (arg1 - 1) needs to be assigned to a temp first
    // while the second call argument (const 1) doesn't.
    // Basic block before tail recursion elimination:
    //  ***** BB04, stmt 1 (top level)
    //  [000037] ------------             *  stmtExpr  void  (top level) (IL 0x00A...0x013)
    //  [000033] --C - G------ - \--*  call      void   RecursiveMethod
    //  [000030] ------------ | / --*  const     int - 1
    //  [000031] ------------arg0 in rcx + --*  +int
    //  [000029] ------------ | \--*  lclVar    int    V00 arg1
    //  [000032] ------------arg1 in rdx    \--*  const     int    1
    //
    //
    //  Basic block after tail recursion elimination :
    //  ***** BB04, stmt 1 (top level)
    //  [000051] ------------             *  stmtExpr  void  (top level) (IL 0x00A... ? ? ? )
    //  [000030] ------------ | / --*  const     int - 1
    //  [000031] ------------ | / --*  +int
    //  [000029] ------------ | | \--*  lclVar    int    V00 arg1
    //  [000050] - A----------             \--* = int
    //  [000049] D------N----                \--*  lclVar    int    V02 tmp0
    //
    //  ***** BB04, stmt 2 (top level)
    //  [000055] ------------             *  stmtExpr  void  (top level) (IL 0x00A... ? ? ? )
    //  [000052] ------------ | / --*  lclVar    int    V02 tmp0
    //  [000054] - A----------             \--* = int
    //  [000053] D------N----                \--*  lclVar    int    V00 arg0

    //  ***** BB04, stmt 3 (top level)
    //  [000058] ------------             *  stmtExpr  void  (top level) (IL 0x00A... ? ? ? )
    //  [000032] ------------ | / --*  const     int    1
    //  [000057] - A----------             \--* = int
    //  [000056] D------N----                \--*  lclVar    int    V01 arg1

    Statement* tmpAssignmentInsertionPoint   = lastStmt;
    Statement* paramAssignmentInsertionPoint = lastStmt;

    // Process early args. They may contain both setup statements for late args and actual args.
    for (CallArg& arg : recursiveTailCall->gtArgs.EarlyArgs())
    {
        GenTree* earlyArg = arg.GetEarlyNode();
        if (arg.GetLateNode() != nullptr)
        {
            // This is a setup node so we need to hoist it.
            Statement* earlyArgStmt = gtNewStmt(earlyArg, callDI);
            fgInsertStmtBefore(block, earlyArgInsertionPoint, earlyArgStmt);
        }
        else
        {
            // This is an actual argument that needs to be assigned to the corresponding caller parameter.
            // Late-added non-standard args are extra args that are not passed as locals, so skip those
            if (!arg.IsArgAddedLate())
            {
                Statement* paramAssignStmt =
                    fgAssignRecursiveCallArgToCallerParam(earlyArg, &arg,
                                                          fgGetArgParameterLclNum(recursiveTailCall, &arg), block,
                                                          callDI, tmpAssignmentInsertionPoint,
                                                          paramAssignmentInsertionPoint);
                if ((tmpAssignmentInsertionPoint == lastStmt) && (paramAssignStmt != nullptr))
                {
                    // All temp assignments will happen before the first param assignment.
                    tmpAssignmentInsertionPoint = paramAssignStmt;
                }
            }
        }
    }

    // Process late args.
    for (CallArg& arg : recursiveTailCall->gtArgs.LateArgs())
    {
        // A late argument is an actual argument that needs to be assigned to the corresponding caller's parameter.
        GenTree* lateArg = arg.GetLateNode();
        // Late-added non-standard args are extra args that are not passed as locals, so skip those
        if (!arg.IsArgAddedLate())
        {
            Statement* paramAssignStmt =
                fgAssignRecursiveCallArgToCallerParam(lateArg, &arg, fgGetArgParameterLclNum(recursiveTailCall, &arg),
                                                      block, callDI, tmpAssignmentInsertionPoint,
                                                      paramAssignmentInsertionPoint);

            if ((tmpAssignmentInsertionPoint == lastStmt) && (paramAssignStmt != nullptr))
            {
                // All temp assignments will happen before the first param assignment.
                tmpAssignmentInsertionPoint = paramAssignStmt;
            }
        }
    }

    // If the method has starg.s 0 or ldarga.s 0 a special local (lvaArg0Var) is created so that
    // compThisArg stays immutable. Normally it's assigned in fgFirstBBScratch block. Since that
    // block won't be in the loop (it's assumed to have no predecessors), we need to update the special local here.
    if (!info.compIsStatic && (lvaArg0Var != info.compThisArg))
    {
        GenTree* const thisArg = gtNewLclVarNode(info.compThisArg);
        thisArg->SetMorphed(this);
        GenTree* const arg0Store = gtNewStoreLclVarNode(lvaArg0Var, thisArg);
        arg0Store->SetMorphed(this);
        Statement* const arg0StoreStmt = gtNewStmt(arg0Store, callDI);
        fgInsertStmtBefore(block, paramAssignmentInsertionPoint, arg0StoreStmt);
    }

    // If compInitMem is set, we may need to zero-initialize some locals. Normally it's done in the prolog
    // but this loop can't include the prolog. Since we don't have liveness information, we insert zero-initialization
    // for all non-parameter IL locals as well as temp structs with GC fields.
    // Liveness phase will remove unnecessary initializations.
    if (info.compInitMem || compSuppressedZeroInit)
    {
        for (unsigned varNum = 0; varNum < lvaCount; varNum++)
        {
#if FEATURE_FIXED_OUT_ARGS
            if (varNum == lvaOutgoingArgSpaceVar)
            {
                continue;
            }
#endif // FEATURE_FIXED_OUT_ARGS

            LclVarDsc* varDsc = lvaGetDesc(varNum);

            if (varDsc->lvIsParam)
            {
                continue;
            }

#if FEATURE_IMPLICIT_BYREFS
            if (varDsc->lvPromoted)
            {
                LclVarDsc* firstField = lvaGetDesc(varDsc->lvFieldLclStart);
                if (firstField->lvParentLcl != varNum)
                {
                    // Local copy for implicit byref promotion that was undone. Do
                    // not introduce new references to it, all uses have been
                    // morphed to access the parameter.

#ifdef DEBUG
                    LclVarDsc* param = lvaGetDesc(firstField->lvParentLcl);
                    assert(param->lvIsImplicitByRef && !param->lvPromoted);
                    assert(param->lvFieldLclStart == varNum);
#endif
                    continue;
                }
            }
#endif

            var_types lclType            = varDsc->TypeGet();
            bool      isUserLocal        = (varNum < info.compLocalsCount);
            bool      structWithGCFields = ((lclType == TYP_STRUCT) && varDsc->GetLayout()->HasGCPtr());
            bool      hadSuppressedInit  = varDsc->lvSuppressedZeroInit;
            if ((info.compInitMem && (isUserLocal || structWithGCFields)) || hadSuppressedInit)
            {
                GenTree* zero = (lclType == TYP_STRUCT) ? gtNewIconNode(0) : gtNewZeroConNode(lclType);
                zero->SetMorphed(this);
                GenTree* init = gtNewStoreLclVarNode(varNum, zero);

                // No need for assertion prop here since the first block is now an (opaque) join
                // and has already been morphed.
                init->SetMorphed(this);
                init->gtType = lclType; // TODO-ASG: delete this zero-diff quirk.
                if (lclType == TYP_STRUCT)
                {
                    init = fgMorphInitBlock(init);
                }

                Statement* initStmt = gtNewStmt(init, callDI);
                fgInsertStmtBefore(block, lastStmt, initStmt);
            }
        }
    }

    // Remove the call
    fgRemoveStmt(block, lastStmt);

    assert(!opts.IsOSR());
    // Set the loop edge.
    // TODO-Cleanup: We should really be expanding tailcalls into loops much
    // earlier than this, at a place where we can just use the init BB here.
    BasicBlock* entryBB = fgGetFirstILBlock();
    assert(doesMethodHaveRecursiveTailcall());

    FlowEdge* const newEdge = fgAddRefPred(entryBB, block);
    block->SetKindAndTargetEdge(BBJ_ALWAYS, newEdge);

    // Update profile
    if (block->hasProfileWeight() && entryBB->hasProfileWeight())
    {
        entryBB->increaseBBProfileWeight(block->bbWeight);
        JITDUMP("Flow into entry BB " FMT_BB " increased. Data %s inconsistent.\n", entryBB->bbNum,
                fgPgoConsistent ? "is now" : "was already");
        fgPgoConsistent = false;
    }

    // Finish hooking things up.
    block->RemoveFlags(BBF_HAS_JMP);
}

//------------------------------------------------------------------------------
// fgAssignRecursiveCallArgToCallerParam : Assign argument to a recursive call to the corresponding caller parameter.
//
// Arguments:
//    arg                           - argument to assign
//    callArg                       - the corresponding call argument
//    lclParamNum                   - the lcl num of the parameter
//    block                         - basic block the call is in
//    callILOffset                  - IL offset of the call
//    tmpAssignmentInsertionPoint   - tree before which temp assignment should be inserted (if necessary)
//    paramAssignmentInsertionPoint - tree before which parameter assignment should be inserted
//
// Return Value:
//    parameter assignment statement if one was inserted; nullptr otherwise.
//
Statement* Compiler::fgAssignRecursiveCallArgToCallerParam(GenTree*         arg,
                                                           CallArg*         callArg,
                                                           unsigned         lclParamNum,
                                                           BasicBlock*      block,
                                                           const DebugInfo& callDI,
                                                           Statement*       tmpAssignmentInsertionPoint,
                                                           Statement*       paramAssignmentInsertionPoint)
{
    // Call arguments should be assigned to temps first and then the temps should be assigned to parameters because
    // some argument trees may reference parameters directly.

    GenTree* argInTemp             = nullptr;
    bool     needToAssignParameter = true;

    // TODO-CQ: enable calls with struct arguments passed in registers.
    noway_assert(!varTypeIsStruct(arg->TypeGet()));

    if (arg->IsCnsIntOrI() || arg->IsCnsFltOrDbl())
    {
        // The argument is already assigned to a temp or is a const.
        argInTemp = arg;
    }
    else if (arg->OperIs(GT_LCL_VAR))
    {
        unsigned   lclNum = arg->AsLclVar()->GetLclNum();
        LclVarDsc* varDsc = lvaGetDesc(lclNum);
        if (!varDsc->lvIsParam)
        {
            // The argument is a non-parameter local so it doesn't need to be assigned to a temp.
            argInTemp = arg;
        }
        else if (lclNum == lclParamNum)
        {
            // The argument is the same parameter local that we were about to assign so
            // we can skip the assignment.
            needToAssignParameter = false;
        }
    }

    // TODO: We don't need temp assignments if we can prove that the argument tree doesn't involve
    // any caller parameters. Some common cases are handled above but we may be able to eliminate
    // more temp assignments.

    Statement* paramAssignStmt = nullptr;
    if (needToAssignParameter)
    {
        if (argInTemp == nullptr)
        {
            // The argument is not assigned to a temp. We need to create a new temp and insert a store.
            unsigned tmpNum         = lvaGrabTemp(true DEBUGARG("arg temp"));
            lvaTable[tmpNum].lvType = arg->gtType;
            GenTree* tempSrc        = arg;
            GenTree* tmpStoreNode   = gtNewStoreLclVarNode(tmpNum, tempSrc);
            tmpStoreNode->SetMorphed(this);
            Statement* tmpStoreStmt = gtNewStmt(tmpStoreNode, callDI);
            fgInsertStmtBefore(block, tmpAssignmentInsertionPoint, tmpStoreStmt);
            argInTemp = gtNewLclvNode(tmpNum, tempSrc->gtType);

            // No need for assertion prop here since the first block is now an opqaque join
            // and has laready been morphed
            argInTemp->SetMorphed(this);
        }

        // Now assign the temp to the parameter.
        assert(lvaGetDesc(lclParamNum)->lvIsParam);
        GenTree* paramStoreNode = gtNewStoreLclVarNode(lclParamNum, argInTemp);

        // No need for assertion prop here since the first block is now an opqaque join
        // and has laready been morphed
        paramStoreNode->SetMorphed(this);

        paramAssignStmt = gtNewStmt(paramStoreNode, callDI);

        fgInsertStmtBefore(block, paramAssignmentInsertionPoint, paramAssignStmt);
    }
    return paramAssignStmt;
}

//------------------------------------------------------------------------
// fgCanTailCallViaJitHelper: check whether we can use the faster tailcall
// JIT helper on x86.
//
// Arguments:
//   call - the tailcall
//
// Return Value:
//    'true' if we can; or 'false' if we should use the generic tailcall mechanism.
//
bool Compiler::fgCanTailCallViaJitHelper(GenTreeCall* call)
{
#if !defined(TARGET_X86) || defined(UNIX_X86_ABI)
    // On anything except windows X86 we have no faster mechanism available.
    return false;
#else
    // For R2R make sure we go through portable mechanism that the 'EE' side
    // will properly turn into a runtime JIT.
    if (IsAot())
    {
        return false;
    }

    // The JIT helper does not properly handle the case where localloc was used.
    if (compLocallocUsed)
    {
        return false;
    }

    // Delegate calls may go through VSD stub in rare cases. Those look at the
    // call site so we cannot use the JIT helper.
    if (call->IsDelegateInvoke())
    {
        return false;
    }

    return true;
#endif
}
