// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "jitpch.h"

#define Verify(cond, msg)                                                                                              \
    do                                                                                                                 \
    {                                                                                                                  \
        if (!(cond))                                                                                                   \
        {                                                                                                              \
            verRaiseVerifyExceptionIfNeeded(INDEBUG(msg) DEBUGARG(__FILE__) DEBUGARG(__LINE__));                       \
        }                                                                                                              \
    } while (0)

//------------------------------------------------------------------------
// impImportCall: import a call-inspiring opcode
//
// Arguments:
//    opcode                    - opcode that inspires the call
//    pResolvedToken            - resolved token for the call target
//    pConstrainedResolvedToken - resolved constraint token (or nullptr)
//    newObjThis                - tree for this pointer or uninitialized newobj temp (or nullptr)
//    prefixFlags               - IL prefix flags for the call
//    callInfo                  - EE supplied info for the call
//    rawILOffset               - IL offset of the opcode, used for guarded devirtualization.
//
// Returns:
//    Type of the call's return value.
//    If we're importing an inlinee and have realized the inline must fail, the call return type should be TYP_UNDEF.
//    However we can't assert for this here yet because there are cases we miss. See issue #13272.
//
//
// Notes:
//    opcode can be CEE_CALL, CEE_CALLI, CEE_CALLVIRT, or CEE_NEWOBJ.
//
//    For CEE_NEWOBJ, newobjThis should be the temp grabbed for the allocated
//    uninitialized object.

#ifdef _PREFAST_
#pragma warning(push)
#pragma warning(disable : 21000) // Suppress PREFast warning about overly large function
#endif

var_types Compiler::impImportCall(OPCODE                  opcode,
                                  CORINFO_RESOLVED_TOKEN* pResolvedToken,
                                  CORINFO_RESOLVED_TOKEN* pConstrainedResolvedToken,
                                  GenTree*                newobjThis,
                                  int                     prefixFlags,
                                  CORINFO_CALL_INFO*      callInfo,
                                  IL_OFFSET               rawILOffset)
{
    assert(opcode == CEE_CALL || opcode == CEE_CALLVIRT || opcode == CEE_NEWOBJ || opcode == CEE_CALLI);

    // The current statement DI may not refer to the exact call, but for calls
    // we wish to be able to attach the exact IL instruction to get "return
    // value" support in the debugger, so create one with the exact IL offset.
    DebugInfo di = impCreateDIWithCurrentStackInfo(rawILOffset, true);

    var_types              callRetTyp                     = TYP_COUNT;
    CORINFO_SIG_INFO*      sig                            = nullptr;
    CORINFO_METHOD_HANDLE  methHnd                        = nullptr;
    CORINFO_CLASS_HANDLE   clsHnd                         = nullptr;
    unsigned               clsFlags                       = 0;
    unsigned               mflags                         = 0;
    GenTree*               call                           = nullptr;
    CORINFO_THIS_TRANSFORM constraintCallThisTransform    = CORINFO_NO_THIS_TRANSFORM;
    CORINFO_CONTEXT_HANDLE exactContextHnd                = nullptr;
    bool                   exactContextNeedsRuntimeLookup = false;
    bool                   canTailCall                    = true;
    const char*            szCanTailCallFailReason        = nullptr;
    const int              tailCallFlags                  = (prefixFlags & PREFIX_TAILCALL);
    const bool             isReadonlyCall                 = (prefixFlags & PREFIX_READONLY) != 0;

    methodPointerInfo* ldftnInfo = nullptr;

    // Synchronized methods need to call CORINFO_HELP_MON_EXIT at the end. We could
    // do that before tailcalls, but that is probably not the intended
    // semantic. So just disallow tailcalls from synchronized methods.
    // Also, popping arguments in a varargs function is more work and NYI
    // If we have a security object, we have to keep our frame around for callers
    // to see any imperative security.
    // Reverse P/Invokes need a call to CORINFO_HELP_JIT_REVERSE_PINVOKE_EXIT
    // at the end, so tailcalls should be disabled.
    if (info.compFlags & CORINFO_FLG_SYNCH)
    {
        canTailCall             = false;
        szCanTailCallFailReason = "Caller is synchronized";
    }
    else if (opts.IsReversePInvoke())
    {
        canTailCall             = false;
        szCanTailCallFailReason = "Caller is Reverse P/Invoke";
    }
#if !FEATURE_FIXED_OUT_ARGS
    else if (info.compIsVarArgs)
    {
        canTailCall             = false;
        szCanTailCallFailReason = "Caller is varargs";
    }
#endif // FEATURE_FIXED_OUT_ARGS

    // We only need to cast the return value of pinvoke inlined calls that return small types

    bool checkForSmallType  = false;
    bool bIntrinsicImported = false;

    CORINFO_SIG_INFO calliSig;
    NewCallArg       extraArg;

    /*-------------------------------------------------------------------------
     * First create the call node
     */

    if (opcode == CEE_CALLI)
    {
        if (IsTargetAbi(CORINFO_NATIVEAOT_ABI))
        {
            // See comment in impCheckForPInvokeCall
            BasicBlock* block = compIsForInlining() ? impInlineInfo->iciBlock : compCurBB;
            if (info.compCompHnd->convertPInvokeCalliToCall(pResolvedToken, !impCanPInvokeInlineCallSite(block)))
            {
                eeGetCallInfo(pResolvedToken, nullptr, CORINFO_CALLINFO_ALLOWINSTPARAM, callInfo);
                return impImportCall(CEE_CALL, pResolvedToken, nullptr, nullptr, prefixFlags, callInfo, rawILOffset);
            }
        }

        /* Get the call site sig */
        eeGetSig(pResolvedToken->token, pResolvedToken->tokenScope, pResolvedToken->tokenContext, &calliSig);

        callRetTyp = JITtype2varType(calliSig.retType);

        call = impImportIndirectCall(&calliSig, di);

        // We don't know the target method, so we have to infer the flags, or
        // assume the worst-case.
        mflags = (calliSig.callConv & CORINFO_CALLCONV_HASTHIS) ? 0 : CORINFO_FLG_STATIC;

#ifdef DEBUG
        if (verbose)
        {
            unsigned structSize = (callRetTyp == TYP_STRUCT) ? eeTryGetClassSize(calliSig.retTypeSigClass) : 0;
            printf("\nIn Compiler::impImportCall: opcode is %s, kind=%d, callRetType is %s, structSize is %u\n",
                   opcodeNames[opcode], callInfo->kind, varTypeName(callRetTyp), structSize);
        }
#endif
        sig = &calliSig;
    }
    else // (opcode != CEE_CALLI)
    {
        NamedIntrinsic ni = NI_Illegal;

        // Passing CORINFO_CALLINFO_ALLOWINSTPARAM indicates that this JIT is prepared to
        // supply the instantiation parameters necessary to make direct calls to underlying
        // shared generic code, rather than calling through instantiating stubs.  If the
        // returned signature has CORINFO_CALLCONV_PARAMTYPE then this indicates that the JIT
        // must indeed pass an instantiation parameter.

        methHnd = callInfo->hMethod;

        sig        = &(callInfo->sig);
        callRetTyp = JITtype2varType(sig->retType);

        mflags = callInfo->methodFlags;

#ifdef DEBUG
        if (verbose)
        {
            unsigned structSize = (callRetTyp == TYP_STRUCT) ? eeTryGetClassSize(sig->retTypeSigClass) : 0;
            printf("\nIn Compiler::impImportCall: opcode is %s, kind=%d, callRetType is %s, structSize is %u\n",
                   opcodeNames[opcode], callInfo->kind, varTypeName(callRetTyp), structSize);
        }
#endif
        if (compIsForInlining())
        {
            /* Does the inlinee use StackCrawlMark */

            if (mflags & CORINFO_FLG_DONT_INLINE_CALLER)
            {
                compInlineResult->NoteFatal(InlineObservation::CALLEE_STACK_CRAWL_MARK);
                return TYP_UNDEF;
            }

            /* For now ignore varargs */
            if ((sig->callConv & CORINFO_CALLCONV_MASK) == CORINFO_CALLCONV_NATIVEVARARG)
            {
                compInlineResult->NoteFatal(InlineObservation::CALLEE_HAS_NATIVE_VARARGS);
                return TYP_UNDEF;
            }

            if ((sig->callConv & CORINFO_CALLCONV_MASK) == CORINFO_CALLCONV_VARARG)
            {
                compInlineResult->NoteFatal(InlineObservation::CALLEE_HAS_MANAGED_VARARGS);
                return TYP_UNDEF;
            }

            if ((mflags & CORINFO_FLG_VIRTUAL) && (sig->sigInst.methInstCount != 0) && (opcode == CEE_CALLVIRT))
            {
                compInlineResult->NoteFatal(InlineObservation::CALLEE_IS_GENERIC_VIRTUAL);
                return TYP_UNDEF;
            }
        }

        clsHnd = pResolvedToken->hClass;

        clsFlags = callInfo->classFlags;

#ifdef DEBUG
        // If this is a call to JitTestLabel.Mark, do "early inlining", and record the test attribute.

        // This recognition should really be done by knowing the methHnd of the relevant Mark method(s).
        // These should be in corelib.h, and available through a JIT/EE interface call.
        const char* modName;
        const char* className;
        const char* methodName;
        if ((className = eeGetClassName(clsHnd)) != nullptr &&
            strcmp(className, "System.Runtime.CompilerServices.JitTestLabel") == 0 &&
            (methodName = eeGetMethodName(methHnd, &modName)) != nullptr && strcmp(methodName, "Mark") == 0)
        {
            return impImportJitTestLabelMark(sig->numArgs);
        }
#endif // DEBUG

        const bool isIntrinsic = (mflags & CORINFO_FLG_INTRINSIC) != 0;

        // <NICE> Factor this into getCallInfo </NICE>
        bool isSpecialIntrinsic = false;

        if (isIntrinsic || !info.compMatchedVM)
        {
            // For mismatched VM (AltJit) we want to check all methods as intrinsic to ensure
            // we get more accurate codegen. This particularly applies to HWIntrinsic usage

            const bool isTailCall = canTailCall && (tailCallFlags != 0);

            call =
                impIntrinsic(newobjThis, clsHnd, methHnd, sig, mflags, pResolvedToken->token, isReadonlyCall,
                             isTailCall, pConstrainedResolvedToken, callInfo->thisTransform, &ni, &isSpecialIntrinsic);

            if (compDonotInline())
            {
                return TYP_UNDEF;
            }

            if (call != nullptr)
            {
#ifdef FEATURE_READYTORUN
                if (call->OperGet() == GT_INTRINSIC)
                {
                    if (opts.IsReadyToRun())
                    {
                        noway_assert(callInfo->kind == CORINFO_CALL);
                        call->AsIntrinsic()->gtEntryPoint = callInfo->codePointerLookup.constLookup;
                    }
                    else
                    {
                        call->AsIntrinsic()->gtEntryPoint.addr       = nullptr;
                        call->AsIntrinsic()->gtEntryPoint.accessType = IAT_VALUE;
                    }
                }
#endif

                bIntrinsicImported = true;
                goto DONE_CALL;
            }
        }

#ifdef FEATURE_SIMD
        call = impSIMDIntrinsic(opcode, newobjThis, clsHnd, methHnd, sig, mflags, pResolvedToken->token);
        if (call != nullptr)
        {
            bIntrinsicImported = true;
            goto DONE_CALL;
        }
#endif // FEATURE_SIMD

        if ((mflags & CORINFO_FLG_VIRTUAL) && (mflags & CORINFO_FLG_EnC) && (opcode == CEE_CALLVIRT))
        {
            NO_WAY("Virtual call to a function added via EnC is not supported");
        }

        if ((sig->callConv & CORINFO_CALLCONV_MASK) != CORINFO_CALLCONV_DEFAULT &&
            (sig->callConv & CORINFO_CALLCONV_MASK) != CORINFO_CALLCONV_VARARG &&
            (sig->callConv & CORINFO_CALLCONV_MASK) != CORINFO_CALLCONV_NATIVEVARARG)
        {
            BADCODE("Bad calling convention");
        }

        //-------------------------------------------------------------------------
        //  Construct the call node
        //
        // Work out what sort of call we're making.
        // Dispense with virtual calls implemented via LDVIRTFTN immediately.

        constraintCallThisTransform    = callInfo->thisTransform;
        exactContextHnd                = callInfo->contextHandle;
        exactContextNeedsRuntimeLookup = callInfo->exactContextNeedsRuntimeLookup;

        switch (callInfo->kind)
        {
            case CORINFO_VIRTUALCALL_STUB:
            {
                assert(!(mflags & CORINFO_FLG_STATIC)); // can't call a static method
                assert(!(clsFlags & CORINFO_FLG_VALUECLASS));
                if (callInfo->stubLookup.lookupKind.needsRuntimeLookup)
                {
                    if (callInfo->stubLookup.lookupKind.runtimeLookupKind == CORINFO_LOOKUP_NOT_SUPPORTED)
                    {
                        // Runtime does not support inlining of all shapes of runtime lookups
                        // Inlining has to be aborted in such a case
                        compInlineResult->NoteFatal(InlineObservation::CALLSITE_HAS_COMPLEX_HANDLE);
                        return TYP_UNDEF;
                    }

                    GenTree* stubAddr = impRuntimeLookupToTree(pResolvedToken, &callInfo->stubLookup, methHnd);

                    // stubAddr tree may require a new temp.
                    // If we're inlining, this may trigger the too many locals inline failure.
                    //
                    // If so, we need to bail out.
                    //
                    if (compDonotInline())
                    {
                        return TYP_UNDEF;
                    }

                    // This is the rough code to set up an indirect stub call
                    assert(stubAddr != nullptr);

                    // The stubAddr may be a
                    // complex expression. As it is evaluated after the args,
                    // it may cause registered args to be spilled. Simply spill it.
                    //
                    unsigned const lclNum = lvaGrabTemp(true DEBUGARG("VirtualCall with runtime lookup"));
                    if (compDonotInline())
                    {
                        return TYP_UNDEF;
                    }

                    impAssignTempGen(lclNum, stubAddr, CHECK_SPILL_NONE);
                    stubAddr = gtNewLclvNode(lclNum, TYP_I_IMPL);

                    // Create the actual call node

                    assert((sig->callConv & CORINFO_CALLCONV_MASK) != CORINFO_CALLCONV_VARARG &&
                           (sig->callConv & CORINFO_CALLCONV_MASK) != CORINFO_CALLCONV_NATIVEVARARG);

                    call = gtNewIndCallNode(stubAddr, callRetTyp);

                    call->gtFlags |= GTF_EXCEPT | (stubAddr->gtFlags & GTF_GLOB_EFFECT);
                    call->gtFlags |= GTF_CALL_VIRT_STUB;

#ifdef TARGET_X86
                    // No tailcalls allowed for these yet...
                    canTailCall             = false;
                    szCanTailCallFailReason = "VirtualCall with runtime lookup";
#endif
                }
                else
                {
                    // The stub address is known at compile time
                    call                               = gtNewCallNode(CT_USER_FUNC, callInfo->hMethod, callRetTyp, di);
                    call->AsCall()->gtStubCallStubAddr = callInfo->stubLookup.constLookup.addr;
                    call->gtFlags |= GTF_CALL_VIRT_STUB;
                    assert(callInfo->stubLookup.constLookup.accessType != IAT_PPVALUE &&
                           callInfo->stubLookup.constLookup.accessType != IAT_RELPVALUE);
                    if (callInfo->stubLookup.constLookup.accessType == IAT_PVALUE)
                    {
                        call->AsCall()->gtCallMoreFlags |= GTF_CALL_M_VIRTSTUB_REL_INDIRECT;
                    }
                }

#ifdef FEATURE_READYTORUN
                if (opts.IsReadyToRun())
                {
                    // Null check is sometimes needed for ready to run to handle
                    // non-virtual <-> virtual changes between versions
                    if (callInfo->nullInstanceCheck)
                    {
                        call->gtFlags |= GTF_CALL_NULLCHECK;
                    }
                }
#endif

                break;
            }

            case CORINFO_VIRTUALCALL_VTABLE:
            {
                assert(!(mflags & CORINFO_FLG_STATIC)); // can't call a static method
                assert(!(clsFlags & CORINFO_FLG_VALUECLASS));
                call = gtNewCallNode(CT_USER_FUNC, callInfo->hMethod, callRetTyp, di);
                call->gtFlags |= GTF_CALL_VIRT_VTABLE;

                // Mark this method to expand the virtual call target early in fgMorphCall
                call->AsCall()->SetExpandedEarly();
                break;
            }

            case CORINFO_VIRTUALCALL_LDVIRTFTN:
            {
                if (compIsForInlining())
                {
                    compInlineResult->NoteFatal(InlineObservation::CALLSITE_HAS_CALL_VIA_LDVIRTFTN);
                    return TYP_UNDEF;
                }

                assert(!(mflags & CORINFO_FLG_STATIC)); // can't call a static method
                assert(!(clsFlags & CORINFO_FLG_VALUECLASS));
                // OK, We've been told to call via LDVIRTFTN, so just
                // take the call now....
                call = gtNewIndCallNode(nullptr, callRetTyp, di);

                impPopCallArgs(sig, call->AsCall());

                GenTree* thisPtr = impPopStack().val;
                thisPtr          = impTransformThis(thisPtr, pConstrainedResolvedToken, callInfo->thisTransform);
                assert(thisPtr != nullptr);

                // Clone the (possibly transformed) "this" pointer
                GenTree* thisPtrCopy;
                thisPtr = impCloneExpr(thisPtr, &thisPtrCopy, NO_CLASS_HANDLE, CHECK_SPILL_ALL,
                                       nullptr DEBUGARG("LDVIRTFTN this pointer"));

                GenTree* fptr = impImportLdvirtftn(thisPtr, pResolvedToken, callInfo);
                assert(fptr != nullptr);

                call->AsCall()
                    ->gtArgs.PushFront(this, NewCallArg::Primitive(thisPtrCopy).WellKnown(WellKnownArg::ThisPointer));

                // Now make an indirect call through the function pointer

                unsigned lclNum = lvaGrabTemp(true DEBUGARG("VirtualCall through function pointer"));
                impAssignTempGen(lclNum, fptr, CHECK_SPILL_ALL);
                fptr = gtNewLclvNode(lclNum, TYP_I_IMPL);

                call->AsCall()->gtCallAddr = fptr;
                call->gtFlags |= GTF_EXCEPT | (fptr->gtFlags & GTF_GLOB_EFFECT);

                if ((sig->sigInst.methInstCount != 0) && IsTargetAbi(CORINFO_NATIVEAOT_ABI))
                {
                    // NativeAOT generic virtual method: need to handle potential fat function pointers
                    addFatPointerCandidate(call->AsCall());
                }
#ifdef FEATURE_READYTORUN
                if (opts.IsReadyToRun())
                {
                    // Null check is needed for ready to run to handle
                    // non-virtual <-> virtual changes between versions
                    call->gtFlags |= GTF_CALL_NULLCHECK;
                }
#endif

                // Sine we are jumping over some code, check that its OK to skip that code
                assert((sig->callConv & CORINFO_CALLCONV_MASK) != CORINFO_CALLCONV_VARARG &&
                       (sig->callConv & CORINFO_CALLCONV_MASK) != CORINFO_CALLCONV_NATIVEVARARG);
                goto DONE;
            }

            case CORINFO_CALL:
            {
                // This is for a non-virtual, non-interface etc. call
                call = gtNewCallNode(CT_USER_FUNC, callInfo->hMethod, callRetTyp, di);

                // We remove the nullcheck for the GetType call intrinsic.
                // TODO-CQ: JIT64 does not introduce the null check for many more helper calls
                // and intrinsics.
                if (callInfo->nullInstanceCheck &&
                    !((mflags & CORINFO_FLG_INTRINSIC) != 0 && (ni == NI_System_Object_GetType)))
                {
                    call->gtFlags |= GTF_CALL_NULLCHECK;
                }

#ifdef FEATURE_READYTORUN
                if (opts.IsReadyToRun())
                {
                    call->AsCall()->setEntryPoint(callInfo->codePointerLookup.constLookup);
                }
#endif
                break;
            }

            case CORINFO_CALL_CODE_POINTER:
            {
                // The EE has asked us to call by computing a code pointer and then doing an
                // indirect call.  This is because a runtime lookup is required to get the code entry point.

                // These calls always follow a uniform calling convention, i.e. no extra hidden params
                assert((sig->callConv & CORINFO_CALLCONV_PARAMTYPE) == 0);

                assert((sig->callConv & CORINFO_CALLCONV_MASK) != CORINFO_CALLCONV_VARARG);
                assert((sig->callConv & CORINFO_CALLCONV_MASK) != CORINFO_CALLCONV_NATIVEVARARG);

                GenTree* fptr =
                    impLookupToTree(pResolvedToken, &callInfo->codePointerLookup, GTF_ICON_FTN_ADDR, callInfo->hMethod);

                if (compDonotInline())
                {
                    return TYP_UNDEF;
                }

                // Now make an indirect call through the function pointer

                unsigned lclNum = lvaGrabTemp(true DEBUGARG("Indirect call through function pointer"));
                impAssignTempGen(lclNum, fptr, CHECK_SPILL_ALL);
                fptr = gtNewLclvNode(lclNum, TYP_I_IMPL);

                call = gtNewIndCallNode(fptr, callRetTyp, di);
                call->gtFlags |= GTF_EXCEPT | (fptr->gtFlags & GTF_GLOB_EFFECT);
                if (callInfo->nullInstanceCheck)
                {
                    call->gtFlags |= GTF_CALL_NULLCHECK;
                }

                break;
            }

            default:
                assert(!"unknown call kind");
                break;
        }

        //-------------------------------------------------------------------------
        // Set more flags

        PREFIX_ASSUME(call != nullptr);

        if (mflags & CORINFO_FLG_NOGCCHECK)
        {
            call->AsCall()->gtCallMoreFlags |= GTF_CALL_M_NOGCCHECK;
        }

        // Mark call if it's one of the ones we will maybe treat as an intrinsic
        if (isSpecialIntrinsic)
        {
            call->AsCall()->gtCallMoreFlags |= GTF_CALL_M_SPECIAL_INTRINSIC;
        }
    }
    assert(sig);
    assert(clsHnd || (opcode == CEE_CALLI)); // We're never verifying for CALLI, so this is not set.

    /* Some sanity checks */

    // CALL_VIRT and NEWOBJ must have a THIS pointer
    assert((opcode != CEE_CALLVIRT && opcode != CEE_NEWOBJ) || (sig->callConv & CORINFO_CALLCONV_HASTHIS));
    // static bit and hasThis are negations of one another
    assert(((mflags & CORINFO_FLG_STATIC) != 0) == ((sig->callConv & CORINFO_CALLCONV_HASTHIS) == 0));
    assert(call != nullptr);

    /*-------------------------------------------------------------------------
     * Check special-cases etc
     */

    /* Special case - Check if it is a call to Delegate.Invoke(). */

    if (mflags & CORINFO_FLG_DELEGATE_INVOKE)
    {
        assert(!(mflags & CORINFO_FLG_STATIC)); // can't call a static method
        assert(mflags & CORINFO_FLG_FINAL);

        /* Set the delegate flag */
        call->AsCall()->gtCallMoreFlags |= GTF_CALL_M_DELEGATE_INV;

        if (callInfo->wrapperDelegateInvoke)
        {
            call->AsCall()->gtCallMoreFlags |= GTF_CALL_M_WRAPPER_DELEGATE_INV;
        }

        if (opcode == CEE_CALLVIRT)
        {
            assert(mflags & CORINFO_FLG_FINAL);

            /* It should have the GTF_CALL_NULLCHECK flag set. Reset it */
            assert(call->gtFlags & GTF_CALL_NULLCHECK);
            call->gtFlags &= ~GTF_CALL_NULLCHECK;
        }
    }

    CORINFO_CLASS_HANDLE actualMethodRetTypeSigClass;
    actualMethodRetTypeSigClass = sig->retTypeSigClass;

    /* Check for varargs */
    if (!compFeatureVarArg() && ((sig->callConv & CORINFO_CALLCONV_MASK) == CORINFO_CALLCONV_VARARG ||
                                 (sig->callConv & CORINFO_CALLCONV_MASK) == CORINFO_CALLCONV_NATIVEVARARG))
    {
        BADCODE("Varargs not supported.");
    }

    if ((sig->callConv & CORINFO_CALLCONV_MASK) == CORINFO_CALLCONV_VARARG ||
        (sig->callConv & CORINFO_CALLCONV_MASK) == CORINFO_CALLCONV_NATIVEVARARG)
    {
        assert(!compIsForInlining());

        /* Set the right flags */

        call->gtFlags |= GTF_CALL_POP_ARGS;
        call->AsCall()->gtArgs.SetIsVarArgs();

        /* Can't allow tailcall for varargs as it is caller-pop. The caller
           will be expecting to pop a certain number of arguments, but if we
           tailcall to a function with a different number of arguments, we
           are hosed. There are ways around this (caller remembers esp value,
           varargs is not caller-pop, etc), but not worth it. */
        CLANG_FORMAT_COMMENT_ANCHOR;

#ifdef TARGET_X86
        if (canTailCall)
        {
            canTailCall             = false;
            szCanTailCallFailReason = "Callee is varargs";
        }
#endif

        /* Get the total number of arguments - this is already correct
         * for CALLI - for methods we have to get it from the call site */

        if (opcode != CEE_CALLI)
        {
#ifdef DEBUG
            unsigned numArgsDef = sig->numArgs;
#endif
            eeGetCallSiteSig(pResolvedToken->token, pResolvedToken->tokenScope, pResolvedToken->tokenContext, sig);

            // For vararg calls we must be sure to load the return type of the
            // method actually being called, as well as the return types of the
            // specified in the vararg signature. With type equivalency, these types
            // may not be the same.
            if (sig->retTypeSigClass != actualMethodRetTypeSigClass)
            {
                if (actualMethodRetTypeSigClass != nullptr && sig->retType != CORINFO_TYPE_CLASS &&
                    sig->retType != CORINFO_TYPE_BYREF && sig->retType != CORINFO_TYPE_PTR &&
                    sig->retType != CORINFO_TYPE_VAR)
                {
                    // Make sure that all valuetypes (including enums) that we push are loaded.
                    // This is to guarantee that if a GC is triggered from the prestub of this methods,
                    // all valuetypes in the method signature are already loaded.
                    // We need to be able to find the size of the valuetypes, but we cannot
                    // do a class-load from within GC.
                    info.compCompHnd->classMustBeLoadedBeforeCodeIsRun(actualMethodRetTypeSigClass);
                }
            }

            assert(numArgsDef <= sig->numArgs);
        }

        /* We will have "cookie" as the last argument but we cannot push
         * it on the operand stack because we may overflow, so we append it
         * to the arg list next after we pop them */
    }

    //--------------------------- Inline NDirect ------------------------------

    // For inline cases we technically should look at both the current
    // block and the call site block (or just the latter if we've
    // fused the EH trees). However the block-related checks pertain to
    // EH and we currently won't inline a method with EH. So for
    // inlinees, just checking the call site block is sufficient.
    {
        // New lexical block here to avoid compilation errors because of GOTOs.
        BasicBlock* block = compIsForInlining() ? impInlineInfo->iciBlock : compCurBB;
        impCheckForPInvokeCall(call->AsCall(), methHnd, sig, mflags, block);
    }

#ifdef UNIX_X86_ABI
    // On Unix x86 we use caller-cleaned convention.
    if ((call->gtFlags & GTF_CALL_UNMANAGED) == 0)
        call->gtFlags |= GTF_CALL_POP_ARGS;
#endif // UNIX_X86_ABI

    if (call->gtFlags & GTF_CALL_UNMANAGED)
    {
        // We set up the unmanaged call by linking the frame, disabling GC, etc
        // This needs to be cleaned up on return.
        // In addition, native calls have different normalization rules than managed code
        // (managed calling convention always widens return values in the callee)
        if (canTailCall)
        {
            canTailCall             = false;
            szCanTailCallFailReason = "Callee is native";
        }

        checkForSmallType = true;

        impPopArgsForUnmanagedCall(call->AsCall(), sig);

        goto DONE;
    }
    else if ((opcode == CEE_CALLI) && ((sig->callConv & CORINFO_CALLCONV_MASK) != CORINFO_CALLCONV_DEFAULT) &&
             ((sig->callConv & CORINFO_CALLCONV_MASK) != CORINFO_CALLCONV_VARARG))
    {
        if (!info.compCompHnd->canGetCookieForPInvokeCalliSig(sig))
        {
            // Normally this only happens with inlining.
            // However, a generic method (or type) being NGENd into another module
            // can run into this issue as well.  There's not an easy fall-back for NGEN
            // so instead we fallback to JIT.
            if (compIsForInlining())
            {
                compInlineResult->NoteFatal(InlineObservation::CALLSITE_CANT_EMBED_PINVOKE_COOKIE);
            }
            else
            {
                IMPL_LIMITATION("Can't get PInvoke cookie (cross module generics)");
            }

            return TYP_UNDEF;
        }

        GenTree* cookie = eeGetPInvokeCookie(sig);

        // This cookie is required to be either a simple GT_CNS_INT or
        // an indirection of a GT_CNS_INT
        //
        GenTree* cookieConst = cookie;
        if (cookie->gtOper == GT_IND)
        {
            cookieConst = cookie->AsOp()->gtOp1;
        }
        assert(cookieConst->gtOper == GT_CNS_INT);

        // Setting GTF_DONT_CSE on the GT_CNS_INT as well as on the GT_IND (if it exists) will ensure that
        // we won't allow this tree to participate in any CSE logic
        //
        cookie->gtFlags |= GTF_DONT_CSE;
        cookieConst->gtFlags |= GTF_DONT_CSE;

        call->AsCall()->gtCallCookie = cookie;

        if (canTailCall)
        {
            canTailCall             = false;
            szCanTailCallFailReason = "PInvoke calli";
        }
    }

    /*-------------------------------------------------------------------------
     * Create the argument list
     */

    //-------------------------------------------------------------------------
    // Special case - for varargs we have an implicit last argument

    if ((sig->callConv & CORINFO_CALLCONV_MASK) == CORINFO_CALLCONV_VARARG)
    {
        assert(!compIsForInlining());

        void *varCookie, *pVarCookie;
        if (!info.compCompHnd->canGetVarArgsHandle(sig))
        {
            compInlineResult->NoteFatal(InlineObservation::CALLSITE_CANT_EMBED_VARARGS_COOKIE);
            return TYP_UNDEF;
        }

        varCookie = info.compCompHnd->getVarArgsHandle(sig, &pVarCookie);
        assert((!varCookie) != (!pVarCookie));
        GenTree* cookieNode = gtNewIconEmbHndNode(varCookie, pVarCookie, GTF_ICON_VARG_HDL, sig);
        assert(extraArg.Node == nullptr);
        extraArg = NewCallArg::Primitive(cookieNode).WellKnown(WellKnownArg::VarArgsCookie);
    }

    //-------------------------------------------------------------------------
    // Extra arg for shared generic code and array methods
    //
    // Extra argument containing instantiation information is passed in the
    // following circumstances:
    // (a) To the "Address" method on array classes; the extra parameter is
    //     the array's type handle (a TypeDesc)
    // (b) To shared-code instance methods in generic structs; the extra parameter
    //     is the struct's type handle (a vtable ptr)
    // (c) To shared-code per-instantiation non-generic static methods in generic
    //     classes and structs; the extra parameter is the type handle
    // (d) To shared-code generic methods; the extra parameter is an
    //     exact-instantiation MethodDesc
    //
    // We also set the exact type context associated with the call so we can
    // inline the call correctly later on.

    if (sig->callConv & CORINFO_CALLCONV_PARAMTYPE)
    {
        assert(call->AsCall()->gtCallType == CT_USER_FUNC);
        if (clsHnd == nullptr)
        {
            NO_WAY("CALLI on parameterized type");
        }

        assert(opcode != CEE_CALLI);

        GenTree* instParam;
        bool     runtimeLookup;

        // Instantiated generic method
        if (((SIZE_T)exactContextHnd & CORINFO_CONTEXTFLAGS_MASK) == CORINFO_CONTEXTFLAGS_METHOD)
        {
            assert(exactContextHnd != METHOD_BEING_COMPILED_CONTEXT());

            CORINFO_METHOD_HANDLE exactMethodHandle =
                (CORINFO_METHOD_HANDLE)((SIZE_T)exactContextHnd & ~CORINFO_CONTEXTFLAGS_MASK);

            if (!exactContextNeedsRuntimeLookup)
            {
#ifdef FEATURE_READYTORUN
                if (opts.IsReadyToRun())
                {
                    instParam =
                        impReadyToRunLookupToTree(&callInfo->instParamLookup, GTF_ICON_METHOD_HDL, exactMethodHandle);
                    if (instParam == nullptr)
                    {
                        assert(compDonotInline());
                        return TYP_UNDEF;
                    }
                }
                else
#endif
                {
                    instParam = gtNewIconEmbMethHndNode(exactMethodHandle);
                    info.compCompHnd->methodMustBeLoadedBeforeCodeIsRun(exactMethodHandle);
                }
            }
            else
            {
                instParam = impTokenToHandle(pResolvedToken, &runtimeLookup, true /*mustRestoreHandle*/);
                if (instParam == nullptr)
                {
                    assert(compDonotInline());
                    return TYP_UNDEF;
                }
            }
        }

        // otherwise must be an instance method in a generic struct,
        // a static method in a generic type, or a runtime-generated array method
        else
        {
            assert(((SIZE_T)exactContextHnd & CORINFO_CONTEXTFLAGS_MASK) == CORINFO_CONTEXTFLAGS_CLASS);
            CORINFO_CLASS_HANDLE exactClassHandle = eeGetClassFromContext(exactContextHnd);

            if (compIsForInlining() && (clsFlags & CORINFO_FLG_ARRAY) != 0)
            {
                compInlineResult->NoteFatal(InlineObservation::CALLEE_IS_ARRAY_METHOD);
                return TYP_UNDEF;
            }

            if ((clsFlags & CORINFO_FLG_ARRAY) && isReadonlyCall)
            {
                // We indicate "readonly" to the Address operation by using a null
                // instParam.
                instParam = gtNewIconNode(0, TYP_REF);
            }
            else if (!exactContextNeedsRuntimeLookup)
            {
#ifdef FEATURE_READYTORUN
                if (opts.IsReadyToRun())
                {
                    instParam =
                        impReadyToRunLookupToTree(&callInfo->instParamLookup, GTF_ICON_CLASS_HDL, exactClassHandle);
                    if (instParam == nullptr)
                    {
                        assert(compDonotInline());
                        return TYP_UNDEF;
                    }
                }
                else
#endif
                {
                    instParam = gtNewIconEmbClsHndNode(exactClassHandle);
                    info.compCompHnd->classMustBeLoadedBeforeCodeIsRun(exactClassHandle);
                }
            }
            else
            {
                instParam = impParentClassTokenToHandle(pResolvedToken, &runtimeLookup, true /*mustRestoreHandle*/);
                if (instParam == nullptr)
                {
                    assert(compDonotInline());
                    return TYP_UNDEF;
                }
            }
        }

        assert(extraArg.Node == nullptr);
        extraArg = NewCallArg::Primitive(instParam).WellKnown(WellKnownArg::InstParam);
    }

    if ((opcode == CEE_NEWOBJ) && ((clsFlags & CORINFO_FLG_DELEGATE) != 0))
    {
        // Only verifiable cases are supported.
        // dup; ldvirtftn; newobj; or ldftn; newobj.
        // IL test could contain unverifiable sequence, in this case optimization should not be done.
        if (impStackHeight() > 0)
        {
            typeInfo delegateTypeInfo = impStackTop().seTypeInfo;
            if (delegateTypeInfo.IsMethod())
            {
                ldftnInfo = delegateTypeInfo.GetMethodPointerInfo();
            }
        }
    }

    //-------------------------------------------------------------------------
    // The main group of arguments

    impPopCallArgs(sig, call->AsCall());
    if (extraArg.Node != nullptr)
    {
        if (Target::g_tgtArgOrder == Target::ARG_ORDER_R2L)
        {
            call->AsCall()->gtArgs.PushFront(this, extraArg);
        }
        else
        {
            call->AsCall()->gtArgs.PushBack(this, extraArg);
        }

        call->gtFlags |= extraArg.Node->gtFlags & GTF_GLOB_EFFECT;
    }

    //-------------------------------------------------------------------------
    // The "this" pointer

    if (((mflags & CORINFO_FLG_STATIC) == 0) && ((sig->callConv & CORINFO_CALLCONV_EXPLICITTHIS) == 0) &&
        !((opcode == CEE_NEWOBJ) && (newobjThis == nullptr)))
    {
        GenTree* obj;

        if (opcode == CEE_NEWOBJ)
        {
            obj = newobjThis;
        }
        else
        {
            obj = impPopStack().val;
            obj = impTransformThis(obj, pConstrainedResolvedToken, constraintCallThisTransform);
            if (compDonotInline())
            {
                return TYP_UNDEF;
            }
        }

        // Store the "this" value in the call
        call->gtFlags |= obj->gtFlags & GTF_GLOB_EFFECT;
        call->AsCall()->gtArgs.PushFront(this, NewCallArg::Primitive(obj).WellKnown(WellKnownArg::ThisPointer));

        if (impIsThis(obj))
        {
            call->AsCall()->gtCallMoreFlags |= GTF_CALL_M_NONVIRT_SAME_THIS;
        }
    }

    bool probing;
    probing = impConsiderCallProbe(call->AsCall(), rawILOffset);

    // See if we can devirt if we aren't probing.
    if (!probing && opts.OptimizationEnabled())
    {
        if (call->AsCall()->IsVirtual())
        {
            // only true object pointers can be virtual
            assert(call->AsCall()->gtArgs.HasThisPointer() &&
                   call->AsCall()->gtArgs.GetThisArg()->GetNode()->TypeIs(TYP_REF));

            // See if we can devirtualize.

            const bool isExplicitTailCall     = (tailCallFlags & PREFIX_TAILCALL_EXPLICIT) != 0;
            const bool isLateDevirtualization = false;
            impDevirtualizeCall(call->AsCall(), pResolvedToken, &callInfo->hMethod, &callInfo->methodFlags,
                                &callInfo->contextHandle, &exactContextHnd, isLateDevirtualization, isExplicitTailCall,
                                // Take care to pass raw IL offset here as the 'debug info' might be different for
                                // inlinees.
                                rawILOffset);

            // Devirtualization may change which method gets invoked. Update our local cache.
            //
            methHnd = callInfo->hMethod;
        }
        else if (call->AsCall()->IsDelegateInvoke())
        {
            considerGuardedDevirtualization(call->AsCall(), rawILOffset, false, NO_METHOD_HANDLE, NO_CLASS_HANDLE,
                                            nullptr);
        }
    }

    //-------------------------------------------------------------------------
    // The "this" pointer for "newobj"

    if (opcode == CEE_NEWOBJ)
    {
        if (clsFlags & CORINFO_FLG_VAROBJSIZE)
        {
            assert(!(clsFlags & CORINFO_FLG_ARRAY)); // arrays handled separately
            // This is a 'new' of a variable sized object, wher
            // the constructor is to return the object.  In this case
            // the constructor claims to return VOID but we know it
            // actually returns the new object
            assert(callRetTyp == TYP_VOID);
            callRetTyp   = TYP_REF;
            call->gtType = TYP_REF;
            impSpillSpecialSideEff();

            impPushOnStack(call, typeInfo(TI_REF, clsHnd));
        }
        else
        {
            if (clsFlags & CORINFO_FLG_DELEGATE)
            {
                // New inliner morph it in impImportCall.
                // This will allow us to inline the call to the delegate constructor.
                call = fgOptimizeDelegateConstructor(call->AsCall(), &exactContextHnd, ldftnInfo);
            }

            if (!bIntrinsicImported)
            {

#if defined(DEBUG) || defined(INLINE_DATA)

                // Keep track of the raw IL offset of the call
                call->AsCall()->gtRawILOffset = rawILOffset;

#endif // defined(DEBUG) || defined(INLINE_DATA)

                // Is it an inline candidate?
                impMarkInlineCandidate(call, exactContextHnd, exactContextNeedsRuntimeLookup, callInfo, rawILOffset);
            }

            // append the call node.
            impAppendTree(call, CHECK_SPILL_ALL, impCurStmtDI);

            // Now push the value of the 'new onto the stack

            // This is a 'new' of a non-variable sized object.
            // Append the new node (op1) to the statement list,
            // and then push the local holding the value of this
            // new instruction on the stack.

            if (clsFlags & CORINFO_FLG_VALUECLASS)
            {
                assert(newobjThis->gtOper == GT_ADDR && newobjThis->AsOp()->gtOp1->gtOper == GT_LCL_VAR);

                unsigned tmp = newobjThis->AsOp()->gtOp1->AsLclVarCommon()->GetLclNum();
                impPushOnStack(gtNewLclvNode(tmp, lvaGetRealType(tmp)), verMakeTypeInfo(clsHnd).NormaliseForStack());
            }
            else
            {
                if (newobjThis->gtOper == GT_COMMA)
                {
                    // We must have inserted the callout. Get the real newobj.
                    newobjThis = newobjThis->AsOp()->gtOp2;
                }

                assert(newobjThis->gtOper == GT_LCL_VAR);
                impPushOnStack(gtNewLclvNode(newobjThis->AsLclVarCommon()->GetLclNum(), TYP_REF),
                               typeInfo(TI_REF, clsHnd));
            }
        }
        return callRetTyp;
    }

DONE:

#ifdef DEBUG
    // In debug we want to be able to register callsites with the EE.
    assert(call->AsCall()->callSig == nullptr);
    call->AsCall()->callSig  = new (this, CMK_Generic) CORINFO_SIG_INFO;
    *call->AsCall()->callSig = *sig;
#endif

    // Final importer checks for calls flagged as tail calls.
    //
    if (tailCallFlags != 0)
    {
        const bool isExplicitTailCall = (tailCallFlags & PREFIX_TAILCALL_EXPLICIT) != 0;
        const bool isImplicitTailCall = (tailCallFlags & PREFIX_TAILCALL_IMPLICIT) != 0;
        const bool isStressTailCall   = (tailCallFlags & PREFIX_TAILCALL_STRESS) != 0;

        // Exactly one of these should be true.
        assert(isExplicitTailCall != isImplicitTailCall);

        // This check cannot be performed for implicit tail calls for the reason
        // that impIsImplicitTailCallCandidate() is not checking whether return
        // types are compatible before marking a call node with PREFIX_TAILCALL_IMPLICIT.
        // As a result it is possible that in the following case, we find that
        // the type stack is non-empty if Callee() is considered for implicit
        // tail calling.
        //      int Caller(..) { .... void Callee(); ret val; ... }
        //
        // Note that we cannot check return type compatibility before ImpImportCall()
        // as we don't have required info or need to duplicate some of the logic of
        // ImpImportCall().
        //
        // For implicit tail calls, we perform this check after return types are
        // known to be compatible.
        if (isExplicitTailCall && (verCurrentState.esStackDepth != 0))
        {
            BADCODE("Stack should be empty after tailcall");
        }

        // For opportunistic tailcalls we allow implicit widening, i.e. tailcalls from int32 -> int16, since the
        // managed calling convention dictates that the callee widens the value. For explicit tailcalls we don't
        // want to require this detail of the calling convention to bubble up to the tailcall helpers
        bool allowWidening = isImplicitTailCall;
        if (canTailCall &&
            !impTailCallRetTypeCompatible(allowWidening, info.compRetType, info.compMethodInfo->args.retTypeClass,
                                          info.compCallConv, callRetTyp, sig->retTypeClass,
                                          call->AsCall()->GetUnmanagedCallConv()))
        {
            canTailCall             = false;
            szCanTailCallFailReason = "Return types are not tail call compatible";
        }

        // Stack empty check for implicit tail calls.
        if (canTailCall && isImplicitTailCall && (verCurrentState.esStackDepth != 0))
        {
#ifdef TARGET_AMD64
            // JIT64 Compatibility:  Opportunistic tail call stack mismatch throws a VerificationException
            // in JIT64, not an InvalidProgramException.
            Verify(false, "Stack should be empty after tailcall");
#else  // TARGET_64BIT
            BADCODE("Stack should be empty after tailcall");
#endif //! TARGET_64BIT
        }

        // assert(compCurBB is not a catch, finally or filter block);
        // assert(compCurBB is not a try block protected by a finally block);
        assert(!isExplicitTailCall || compCurBB->bbJumpKind == BBJ_RETURN);

        // Ask VM for permission to tailcall
        if (canTailCall)
        {
            // True virtual or indirect calls, shouldn't pass in a callee handle.
            CORINFO_METHOD_HANDLE exactCalleeHnd =
                ((call->AsCall()->gtCallType != CT_USER_FUNC) || call->AsCall()->IsVirtual()) ? nullptr : methHnd;

            if (info.compCompHnd->canTailCall(info.compMethodHnd, methHnd, exactCalleeHnd, isExplicitTailCall))
            {
                if (isExplicitTailCall)
                {
                    // In case of explicit tail calls, mark it so that it is not considered
                    // for in-lining.
                    call->AsCall()->gtCallMoreFlags |= GTF_CALL_M_EXPLICIT_TAILCALL;
                    JITDUMP("\nGTF_CALL_M_EXPLICIT_TAILCALL set for call [%06u]\n", dspTreeID(call));

                    if (isStressTailCall)
                    {
                        call->AsCall()->gtCallMoreFlags |= GTF_CALL_M_STRESS_TAILCALL;
                        JITDUMP("\nGTF_CALL_M_STRESS_TAILCALL set for call [%06u]\n", dspTreeID(call));
                    }
                }
                else
                {
#if FEATURE_TAILCALL_OPT
                    // Must be an implicit tail call.
                    assert(isImplicitTailCall);

                    // It is possible that a call node is both an inline candidate and marked
                    // for opportunistic tail calling.  In-lining happens before morhphing of
                    // trees.  If in-lining of an in-line candidate gets aborted for whatever
                    // reason, it will survive to the morphing stage at which point it will be
                    // transformed into a tail call after performing additional checks.

                    call->AsCall()->gtCallMoreFlags |= GTF_CALL_M_IMPLICIT_TAILCALL;
                    JITDUMP("\nGTF_CALL_M_IMPLICIT_TAILCALL set for call [%06u]\n", dspTreeID(call));

#else //! FEATURE_TAILCALL_OPT
                    NYI("Implicit tail call prefix on a target which doesn't support opportunistic tail calls");

#endif // FEATURE_TAILCALL_OPT
                }

                // This might or might not turn into a tailcall. We do more
                // checks in morph. For explicit tailcalls we need more
                // information in morph in case it turns out to be a
                // helper-based tailcall.
                if (isExplicitTailCall)
                {
                    assert(call->AsCall()->tailCallInfo == nullptr);
                    call->AsCall()->tailCallInfo = new (this, CMK_CorTailCallInfo) TailCallSiteInfo;
                    switch (opcode)
                    {
                        case CEE_CALLI:
                            call->AsCall()->tailCallInfo->SetCalli(sig);
                            break;
                        case CEE_CALLVIRT:
                            call->AsCall()->tailCallInfo->SetCallvirt(sig, pResolvedToken);
                            break;
                        default:
                            call->AsCall()->tailCallInfo->SetCall(sig, pResolvedToken);
                            break;
                    }
                }
            }
            else
            {
                // canTailCall reported its reasons already
                canTailCall = false;
                JITDUMP("\ninfo.compCompHnd->canTailCall returned false for call [%06u]\n", dspTreeID(call));
            }
        }
        else
        {
            // If this assert fires it means that canTailCall was set to false without setting a reason!
            assert(szCanTailCallFailReason != nullptr);
            JITDUMP("\nRejecting %splicit tail call for [%06u], reason: '%s'\n", isExplicitTailCall ? "ex" : "im",
                    dspTreeID(call), szCanTailCallFailReason);
            info.compCompHnd->reportTailCallDecision(info.compMethodHnd, methHnd, isExplicitTailCall, TAILCALL_FAIL,
                                                     szCanTailCallFailReason);
        }
    }

    // Note: we assume that small return types are already normalized by the managed callee
    // or by the pinvoke stub for calls to unmanaged code.

    if (!bIntrinsicImported)
    {
        //
        // Things needed to be checked when bIntrinsicImported is false.
        //

        assert(call->gtOper == GT_CALL);
        assert(callInfo != nullptr);

        if (compIsForInlining() && opcode == CEE_CALLVIRT)
        {
            assert(call->AsCall()->gtArgs.HasThisPointer());
            GenTree* callObj = call->AsCall()->gtArgs.GetThisArg()->GetEarlyNode();

            if ((call->AsCall()->IsVirtual() || (call->gtFlags & GTF_CALL_NULLCHECK)) &&
                impInlineIsGuaranteedThisDerefBeforeAnySideEffects(nullptr, &call->AsCall()->gtArgs, callObj,
                                                                   impInlineInfo->inlArgInfo))
            {
                impInlineInfo->thisDereferencedFirst = true;
            }
        }

#if defined(DEBUG) || defined(INLINE_DATA)

        // Keep track of the raw IL offset of the call
        call->AsCall()->gtRawILOffset = rawILOffset;

#endif // defined(DEBUG) || defined(INLINE_DATA)

        // Is it an inline candidate?
        impMarkInlineCandidate(call, exactContextHnd, exactContextNeedsRuntimeLookup, callInfo, rawILOffset);
    }

    // Extra checks for tail calls and tail recursion.
    //
    // A tail recursive call is a potential loop from the current block to the start of the root method.
    // If we see a tail recursive call, mark the blocks from the call site back to the entry as potentially
    // being in a loop.
    //
    // Note: if we're importing an inlinee we don't mark the right set of blocks, but by then it's too
    // late. Currently this doesn't lead to problems. See GitHub issue 33529.
    //
    // OSR also needs to handle tail calls specially:
    // * block profiling in OSR methods needs to ensure probes happen before tail calls, not after.
    // * the root method entry must be imported if there's a recursive tail call or a potentially
    //   inlineable tail call.
    //
    if ((tailCallFlags != 0) && canTailCall)
    {
        if (gtIsRecursiveCall(methHnd))
        {
            assert(verCurrentState.esStackDepth == 0);
            BasicBlock* loopHead = nullptr;
            if (!compIsForInlining() && opts.IsOSR())
            {
                // For root method OSR we may branch back to the actual method entry,
                // which is not fgFirstBB, and which we will need to import.
                assert(fgEntryBB != nullptr);
                loopHead = fgEntryBB;
            }
            else
            {
                // For normal jitting we may branch back to the firstBB; this
                // should already be imported.
                loopHead = fgFirstBB;
            }

            JITDUMP("\nTail recursive call [%06u] in the method. Mark " FMT_BB " to " FMT_BB
                    " as having a backward branch.\n",
                    dspTreeID(call), loopHead->bbNum, compCurBB->bbNum);
            fgMarkBackwardJump(loopHead, compCurBB);

            compMayConvertTailCallToLoop = true;
        }

        // We only do these OSR checks in the root method because:
        // * If we fail to import the root method entry when importing the root method, we can't go back
        //    and import it during inlining. So instead of checking just for recursive tail calls we also
        //    have to check for anything that might introduce a recursive tail call.
        // * We only instrument root method blocks in OSR methods,
        //
        if (opts.IsOSR() && !compIsForInlining())
        {
            // If a root method tail call candidate block is not a BBJ_RETURN, it should have a unique
            // BBJ_RETURN successor. Mark that successor so we can handle it specially during profile
            // instrumentation.
            //
            if (compCurBB->bbJumpKind != BBJ_RETURN)
            {
                BasicBlock* const successor = compCurBB->GetUniqueSucc();
                assert(successor->bbJumpKind == BBJ_RETURN);
                successor->bbFlags |= BBF_TAILCALL_SUCCESSOR;
                optMethodFlags |= OMF_HAS_TAILCALL_SUCCESSOR;
            }

            // If this call might eventually turn into a loop back to method entry, make sure we
            // import the method entry.
            //
            assert(call->IsCall());
            GenTreeCall* const actualCall           = call->AsCall();
            const bool         mustImportEntryBlock = gtIsRecursiveCall(methHnd) || actualCall->IsInlineCandidate() ||
                                              actualCall->IsGuardedDevirtualizationCandidate();

            // Only schedule importation if we're not currently importing.
            //
            if (mustImportEntryBlock && (compCurBB != fgEntryBB))
            {
                JITDUMP("\nOSR: inlineable or recursive tail call [%06u] in the method, so scheduling " FMT_BB
                        " for importation\n",
                        dspTreeID(call), fgEntryBB->bbNum);
                impImportBlockPending(fgEntryBB);
            }
        }
    }

    if ((sig->flags & CORINFO_SIGFLAG_FAT_CALL) != 0)
    {
        assert(opcode == CEE_CALLI || callInfo->kind == CORINFO_CALL_CODE_POINTER);
        addFatPointerCandidate(call->AsCall());
    }

DONE_CALL:
    // Push or append the result of the call
    if (callRetTyp == TYP_VOID)
    {
        if (opcode == CEE_NEWOBJ)
        {
            // we actually did push something, so don't spill the thing we just pushed.
            assert(verCurrentState.esStackDepth > 0);
            impAppendTree(call, verCurrentState.esStackDepth - 1, impCurStmtDI);
        }
        else
        {
            impAppendTree(call, CHECK_SPILL_ALL, impCurStmtDI);
        }
    }
    else
    {
        impSpillSpecialSideEff();

        if (clsFlags & CORINFO_FLG_ARRAY)
        {
            eeGetCallSiteSig(pResolvedToken->token, pResolvedToken->tokenScope, pResolvedToken->tokenContext, sig);
        }

        typeInfo tiRetVal = verMakeTypeInfo(sig->retType, sig->retTypeClass);
        tiRetVal.NormaliseForStack();

        if (call->IsCall())
        {
            // Sometimes "call" is not a GT_CALL (if we imported an intrinsic that didn't turn into a call)

            GenTreeCall* origCall = call->AsCall();

            const bool isFatPointerCandidate              = origCall->IsFatPointerCandidate();
            const bool isInlineCandidate                  = origCall->IsInlineCandidate();
            const bool isGuardedDevirtualizationCandidate = origCall->IsGuardedDevirtualizationCandidate();

            if (varTypeIsStruct(callRetTyp))
            {
                // Need to treat all "split tree" cases here, not just inline candidates
                call = impFixupCallStructReturn(call->AsCall(), sig->retTypeClass);
            }

            // TODO: consider handling fatcalli cases this way too...?
            if (isInlineCandidate || isGuardedDevirtualizationCandidate)
            {
                // We should not have made any adjustments in impFixupCallStructReturn
                // as we defer those until we know the fate of the call.
                assert(call == origCall);

                assert(opts.OptEnabled(CLFLG_INLINING));
                assert(!isFatPointerCandidate); // We should not try to inline calli.

                // Make the call its own tree (spill the stack if needed).
                // Do not consume the debug info here. This is particularly
                // important if we give up on the inline, in which case the
                // call will typically end up in the statement that contains
                // the GT_RET_EXPR that we leave on the stack.
                impAppendTree(call, CHECK_SPILL_ALL, impCurStmtDI, false);

                // TODO: Still using the widened type.
                GenTree* retExpr = gtNewInlineCandidateReturnExpr(call, genActualType(callRetTyp), compCurBB->bbFlags);

                // Link the retExpr to the call so if necessary we can manipulate it later.
                origCall->gtInlineCandidateInfo->retExpr = retExpr;

                // Propagate retExpr as the placeholder for the call.
                call = retExpr;
            }
            else
            {
                // If the call is virtual, and has a generics context, and is not going to have a class probe,
                // record the context for possible use during late devirt.
                //
                // If we ever want to devirt at Tier0, and/or see issues where OSR methods under PGO lose
                // important devirtualizations, we'll want to allow both a class probe and a captured context.
                //
                if (origCall->IsVirtual() && (origCall->gtCallType != CT_INDIRECT) && (exactContextHnd != nullptr) &&
                    (origCall->gtHandleHistogramProfileCandidateInfo == nullptr))
                {
                    JITDUMP("\nSaving context %p for call [%06u]\n", exactContextHnd, dspTreeID(origCall));
                    origCall->gtCallMoreFlags |= GTF_CALL_M_HAS_LATE_DEVIRT_INFO;
                    LateDevirtualizationInfo* const info = new (this, CMK_Inlining) LateDevirtualizationInfo;
                    info->exactContextHnd                = exactContextHnd;
                    origCall->gtLateDevirtualizationInfo = info;
                }

                if (isFatPointerCandidate)
                {
                    // fatPointer candidates should be in statements of the form call() or var = call().
                    // Such form allows to find statements with fat calls without walking through whole trees
                    // and removes problems with cutting trees.
                    assert(!bIntrinsicImported);
                    assert(IsTargetAbi(CORINFO_NATIVEAOT_ABI));
                    if (call->OperGet() != GT_LCL_VAR) // can be already converted by impFixupCallStructReturn.
                    {
                        unsigned   calliSlot = lvaGrabTemp(true DEBUGARG("calli"));
                        LclVarDsc* varDsc    = lvaGetDesc(calliSlot);

                        impAssignTempGen(calliSlot, call, tiRetVal.GetClassHandle(), CHECK_SPILL_NONE);
                        // impAssignTempGen can change src arg list and return type for call that returns struct.
                        var_types type = genActualType(lvaTable[calliSlot].TypeGet());
                        call           = gtNewLclvNode(calliSlot, type);
                    }
                }

                // For non-candidates we must also spill, since we
                // might have locals live on the eval stack that this
                // call can modify.
                //
                // Suppress this for certain well-known call targets
                // that we know won't modify locals, eg calls that are
                // recognized in gtCanOptimizeTypeEquality. Otherwise
                // we may break key fragile pattern matches later on.
                bool spillStack = true;
                if (call->IsCall())
                {
                    GenTreeCall* callNode = call->AsCall();
                    if ((callNode->gtCallType == CT_HELPER) && (gtIsTypeHandleToRuntimeTypeHelper(callNode) ||
                                                                gtIsTypeHandleToRuntimeTypeHandleHelper(callNode)))
                    {
                        spillStack = false;
                    }
                    else if ((callNode->gtCallMoreFlags & GTF_CALL_M_SPECIAL_INTRINSIC) != 0)
                    {
                        spillStack = false;
                    }
                }

                if (spillStack)
                {
                    impSpillSideEffects(true, CHECK_SPILL_ALL DEBUGARG("non-inline candidate call"));
                }
            }
        }

        if (!bIntrinsicImported)
        {
            //-------------------------------------------------------------------------
            //
            /* If the call is of a small type and the callee is managed, the callee will normalize the result
                before returning.
                However, we need to normalize small type values returned by unmanaged
                functions (pinvoke). The pinvoke stub does the normalization, but we need to do it here
                if we use the shorter inlined pinvoke stub. */

            if (checkForSmallType && varTypeIsIntegral(callRetTyp) && genTypeSize(callRetTyp) < genTypeSize(TYP_INT))
            {
                call = gtNewCastNode(genActualType(callRetTyp), call, false, callRetTyp);
            }
        }

        impPushOnStack(call, tiRetVal);
    }

    // VSD functions get a new call target each time we getCallInfo, so clear the cache.
    // Also, the call info cache for CALLI instructions is largely incomplete, so clear it out.
    // if ( (opcode == CEE_CALLI) || (callInfoCache.fetchCallInfo().kind == CORINFO_VIRTUALCALL_STUB))
    //  callInfoCache.uncacheCallInfo();

    return callRetTyp;
}
#ifdef _PREFAST_
#pragma warning(pop)
#endif

#ifdef DEBUG
//
var_types Compiler::impImportJitTestLabelMark(int numArgs)
{
    TestLabelAndNum tlAndN;
    if (numArgs == 2)
    {
        tlAndN.m_num  = 0;
        StackEntry se = impPopStack();
        assert(se.seTypeInfo.GetType() == TI_INT);
        GenTree* val = se.val;
        assert(val->IsCnsIntOrI());
        tlAndN.m_tl = (TestLabel)val->AsIntConCommon()->IconValue();
    }
    else if (numArgs == 3)
    {
        StackEntry se = impPopStack();
        assert(se.seTypeInfo.GetType() == TI_INT);
        GenTree* val = se.val;
        assert(val->IsCnsIntOrI());
        tlAndN.m_num = val->AsIntConCommon()->IconValue();
        se           = impPopStack();
        assert(se.seTypeInfo.GetType() == TI_INT);
        val = se.val;
        assert(val->IsCnsIntOrI());
        tlAndN.m_tl = (TestLabel)val->AsIntConCommon()->IconValue();
    }
    else
    {
        assert(false);
    }

    StackEntry expSe = impPopStack();
    GenTree*   node  = expSe.val;

    // There are a small number of special cases, where we actually put the annotation on a subnode.
    if (tlAndN.m_tl == TL_LoopHoist && tlAndN.m_num >= 100)
    {
        // A loop hoist annotation with value >= 100 means that the expression should be a static field access,
        // a GT_IND of a static field address, which should be the sum of a (hoistable) helper call and possibly some
        // offset within the static field block whose address is returned by the helper call.
        // The annotation is saying that this address calculation, but not the entire access, should be hoisted.
        assert(node->OperGet() == GT_IND);
        tlAndN.m_num -= 100;
        GetNodeTestData()->Set(node->AsOp()->gtOp1, tlAndN);
        GetNodeTestData()->Remove(node);
    }
    else
    {
        GetNodeTestData()->Set(node, tlAndN);
    }

    impPushOnStack(node, expSe.seTypeInfo);
    return node->TypeGet();
}
#endif // DEBUG

//-----------------------------------------------------------------------------------
//  impFixupCallStructReturn: For a call node that returns a struct do one of the following:
//  - set the flag to indicate struct return via retbuf arg;
//  - adjust the return type to a SIMD type if it is returned in 1 reg;
//  - spill call result into a temp if it is returned into 2 registers or more and not tail call or inline candidate.
//
//  Arguments:
//    call       -  GT_CALL GenTree node
//    retClsHnd  -  Class handle of return type of the call
//
//  Return Value:
//    Returns new GenTree node after fixing struct return of call node
//
GenTree* Compiler::impFixupCallStructReturn(GenTreeCall* call, CORINFO_CLASS_HANDLE retClsHnd)
{
    if (!varTypeIsStruct(call))
    {
        return call;
    }

    call->gtRetClsHnd = retClsHnd;

#if FEATURE_MULTIREG_RET
    call->InitializeStructReturnType(this, retClsHnd, call->GetUnmanagedCallConv());
    const ReturnTypeDesc* retTypeDesc = call->GetReturnTypeDesc();
    const unsigned        retRegCount = retTypeDesc->GetReturnRegCount();
#else  // !FEATURE_MULTIREG_RET
    const unsigned retRegCount = 1;
#endif // !FEATURE_MULTIREG_RET

    structPassingKind howToReturnStruct;
    var_types         returnType = getReturnTypeForStruct(retClsHnd, call->GetUnmanagedCallConv(), &howToReturnStruct);

    if (howToReturnStruct == SPK_ByReference)
    {
        assert(returnType == TYP_UNKNOWN);
        call->gtCallMoreFlags |= GTF_CALL_M_RETBUFFARG;

        if (call->IsUnmanaged())
        {
            // Native ABIs do not allow retbufs to alias anything.
            // This is allowed by the managed ABI and impAssignStructPtr will
            // never introduce copies due to this.
            unsigned tmpNum = lvaGrabTemp(true DEBUGARG("Retbuf for unmanaged call"));
            impAssignTempGen(tmpNum, call, retClsHnd, CHECK_SPILL_ALL);
            return gtNewLclvNode(tmpNum, lvaGetDesc(tmpNum)->TypeGet());
        }

        return call;
    }

    // Recognize SIMD types as we do for LCL_VARs,
    // note it could be not the ABI specific type, for example, on x64 we can set 'TYP_SIMD8`
    // for `System.Numerics.Vector2` here but lower will change it to long as ABI dictates.
    var_types simdReturnType = impNormStructType(call->gtRetClsHnd);
    if (simdReturnType != call->TypeGet())
    {
        assert(varTypeIsSIMD(simdReturnType));
        JITDUMP("changing the type of a call [%06u] from %s to %s\n", dspTreeID(call), varTypeName(call->TypeGet()),
                varTypeName(simdReturnType));
        call->ChangeType(simdReturnType);
    }

    if (retRegCount == 1)
    {
        return call;
    }

#if FEATURE_MULTIREG_RET
    assert(varTypeIsStruct(call)); // It could be a SIMD returned in several regs.
    assert(returnType == TYP_STRUCT);
    assert((howToReturnStruct == SPK_ByValueAsHfa) || (howToReturnStruct == SPK_ByValue));

#ifdef UNIX_AMD64_ABI
    // must be a struct returned in two registers
    assert(retRegCount == 2);
#else  // not UNIX_AMD64_ABI
    assert(retRegCount >= 2);
#endif // not UNIX_AMD64_ABI

    if (!call->CanTailCall() && !call->IsInlineCandidate())
    {
        // Force a call returning multi-reg struct to be always of the IR form
        //   tmp = call
        //
        // No need to assign a multi-reg struct to a local var if:
        //  - It is a tail call or
        //  - The call is marked for in-lining later
        return impAssignMultiRegTypeToVar(call, retClsHnd DEBUGARG(call->GetUnmanagedCallConv()));
    }
    return call;
#endif // FEATURE_MULTIREG_RET
}

GenTreeCall* Compiler::impImportIndirectCall(CORINFO_SIG_INFO* sig, const DebugInfo& di)
{
    var_types callRetTyp = JITtype2varType(sig->retType);

    /* The function pointer is on top of the stack - It may be a
     * complex expression. As it is evaluated after the args,
     * it may cause registered args to be spilled. Simply spill it.
     */

    // Ignore this trivial case.
    if (impStackTop().val->gtOper != GT_LCL_VAR)
    {
        impSpillStackEntry(verCurrentState.esStackDepth - 1,
                           BAD_VAR_NUM DEBUGARG(false) DEBUGARG("impImportIndirectCall"));
    }

    /* Get the function pointer */

    GenTree* fptr = impPopStack().val;

    // The function pointer is typically a sized to match the target pointer size
    // However, stubgen IL optimization can change LDC.I8 to LDC.I4
    // See ILCodeStream::LowerOpcode
    assert(genActualType(fptr->gtType) == TYP_I_IMPL || genActualType(fptr->gtType) == TYP_INT);

#ifdef DEBUG
    // This temporary must never be converted to a double in stress mode,
    // because that can introduce a call to the cast helper after the
    // arguments have already been evaluated.

    if (fptr->OperGet() == GT_LCL_VAR)
    {
        lvaTable[fptr->AsLclVarCommon()->GetLclNum()].lvKeepType = 1;
    }
#endif

    /* Create the call node */

    GenTreeCall* call = gtNewIndCallNode(fptr, callRetTyp, di);

    call->gtFlags |= GTF_EXCEPT | (fptr->gtFlags & GTF_GLOB_EFFECT);
#ifdef UNIX_X86_ABI
    call->gtFlags &= ~GTF_CALL_POP_ARGS;
#endif

    return call;
}

/*****************************************************************************/

void Compiler::impPopArgsForUnmanagedCall(GenTreeCall* call, CORINFO_SIG_INFO* sig)
{
    assert(call->gtFlags & GTF_CALL_UNMANAGED);

    /* Since we push the arguments in reverse order (i.e. right -> left)
     * spill any side effects from the stack
     *
     * OBS: If there is only one side effect we do not need to spill it
     *      thus we have to spill all side-effects except last one
     */

    unsigned lastLevelWithSideEffects = UINT_MAX;

    unsigned argsToReverse = sig->numArgs;

    // For "thiscall", the first argument goes in a register. Since its
    // order does not need to be changed, we do not need to spill it

    if (call->unmgdCallConv == CorInfoCallConvExtension::Thiscall)
    {
        assert(argsToReverse);
        argsToReverse--;
    }

#ifndef TARGET_X86
    // Don't reverse args on ARM or x64 - first four args always placed in regs in order
    argsToReverse = 0;
#endif

    for (unsigned level = verCurrentState.esStackDepth - argsToReverse; level < verCurrentState.esStackDepth; level++)
    {
        if (verCurrentState.esStack[level].val->gtFlags & GTF_ORDER_SIDEEFF)
        {
            assert(lastLevelWithSideEffects == UINT_MAX);

            impSpillStackEntry(level,
                               BAD_VAR_NUM DEBUGARG(false) DEBUGARG("impPopArgsForUnmanagedCall - other side effect"));
        }
        else if (verCurrentState.esStack[level].val->gtFlags & GTF_SIDE_EFFECT)
        {
            if (lastLevelWithSideEffects != UINT_MAX)
            {
                /* We had a previous side effect - must spill it */
                impSpillStackEntry(lastLevelWithSideEffects,
                                   BAD_VAR_NUM DEBUGARG(false) DEBUGARG("impPopArgsForUnmanagedCall - side effect"));

                /* Record the level for the current side effect in case we will spill it */
                lastLevelWithSideEffects = level;
            }
            else
            {
                /* This is the first side effect encountered - record its level */

                lastLevelWithSideEffects = level;
            }
        }
    }

    /* The argument list is now "clean" - no out-of-order side effects
     * Pop the argument list in reverse order */

    impPopReverseCallArgs(sig, call, sig->numArgs - argsToReverse);

    if (call->unmgdCallConv == CorInfoCallConvExtension::Thiscall)
    {
        GenTree* thisPtr = call->gtArgs.GetArgByIndex(0)->GetNode();
        impBashVarAddrsToI(thisPtr);
        assert(thisPtr->TypeGet() == TYP_I_IMPL || thisPtr->TypeGet() == TYP_BYREF);
    }

    for (CallArg& arg : call->gtArgs.Args())
    {
        GenTree* argNode = arg.GetEarlyNode();

        // We should not be passing gc typed args to an unmanaged call.
        if (varTypeIsGC(argNode->TypeGet()))
        {
            // Tolerate byrefs by retyping to native int.
            //
            // This is needed or we'll generate inconsistent GC info
            // for this arg at the call site (gc info says byref,
            // pinvoke sig says native int).
            //
            if (argNode->TypeGet() == TYP_BYREF)
            {
                argNode->ChangeType(TYP_I_IMPL);
            }
            else
            {
                assert(!"*** invalid IL: gc ref passed to unmanaged call");
            }
        }
    }
}

//------------------------------------------------------------------------
// impInitializeArrayIntrinsic: Attempts to replace a call to InitializeArray
//    with a GT_COPYBLK node.
//
// Arguments:
//    sig - The InitializeArray signature.
//
// Return Value:
//    A pointer to the newly created GT_COPYBLK node if the replacement succeeds or
//    nullptr otherwise.
//
// Notes:
//    The function recognizes the following IL pattern:
//      ldc <length> or a list of ldc <lower bound>/<length>
//      newarr or newobj
//      dup
//      ldtoken <field handle>
//      call InitializeArray
//    The lower bounds need not be constant except when the array rank is 1.
//    The function recognizes all kinds of arrays thus enabling a small runtime
//    such as NativeAOT to skip providing an implementation for InitializeArray.

GenTree* Compiler::impInitializeArrayIntrinsic(CORINFO_SIG_INFO* sig)
{
    assert(sig->numArgs == 2);

    GenTree* fieldTokenNode = impStackTop(0).val;
    GenTree* arrayLocalNode = impStackTop(1).val;

    //
    // Verify that the field token is known and valid.  Note that it's also
    // possible for the token to come from reflection, in which case we cannot do
    // the optimization and must therefore revert to calling the helper.  You can
    // see an example of this in bvt\DynIL\initarray2.exe (in Main).
    //

    // Check to see if the ldtoken helper call is what we see here.
    if (fieldTokenNode->gtOper != GT_CALL || (fieldTokenNode->AsCall()->gtCallType != CT_HELPER) ||
        (fieldTokenNode->AsCall()->gtCallMethHnd != eeFindHelper(CORINFO_HELP_FIELDDESC_TO_STUBRUNTIMEFIELD)))
    {
        return nullptr;
    }

    // Strip helper call away
    fieldTokenNode = fieldTokenNode->AsCall()->gtArgs.GetArgByIndex(0)->GetEarlyNode();

    if (fieldTokenNode->gtOper == GT_IND)
    {
        fieldTokenNode = fieldTokenNode->AsOp()->gtOp1;
    }

    // Check for constant
    if (fieldTokenNode->gtOper != GT_CNS_INT)
    {
        return nullptr;
    }

    CORINFO_FIELD_HANDLE fieldToken = (CORINFO_FIELD_HANDLE)fieldTokenNode->AsIntCon()->gtCompileTimeHandle;
    if (!fieldTokenNode->IsIconHandle(GTF_ICON_FIELD_HDL) || (fieldToken == nullptr))
    {
        return nullptr;
    }

    //
    // We need to get the number of elements in the array and the size of each element.
    // We verify that the newarr statement is exactly what we expect it to be.
    // If it's not then we just return NULL and we don't optimize this call
    //

    // It is possible the we don't have any statements in the block yet.
    if (impLastStmt == nullptr)
    {
        return nullptr;
    }

    //
    // We start by looking at the last statement, making sure it's an assignment, and
    // that the target of the assignment is the array passed to InitializeArray.
    //
    GenTree* arrayAssignment = impLastStmt->GetRootNode();
    if ((arrayAssignment->gtOper != GT_ASG) || (arrayAssignment->AsOp()->gtOp1->gtOper != GT_LCL_VAR) ||
        (arrayLocalNode->gtOper != GT_LCL_VAR) || (arrayAssignment->AsOp()->gtOp1->AsLclVarCommon()->GetLclNum() !=
                                                   arrayLocalNode->AsLclVarCommon()->GetLclNum()))
    {
        return nullptr;
    }

    //
    // Make sure that the object being assigned is a helper call.
    //

    GenTree* newArrayCall = arrayAssignment->AsOp()->gtOp2;
    if ((newArrayCall->gtOper != GT_CALL) || (newArrayCall->AsCall()->gtCallType != CT_HELPER))
    {
        return nullptr;
    }

    //
    // Verify that it is one of the new array helpers.
    //

    bool isMDArray = false;

    if (newArrayCall->AsCall()->gtCallMethHnd != eeFindHelper(CORINFO_HELP_NEWARR_1_DIRECT) &&
        newArrayCall->AsCall()->gtCallMethHnd != eeFindHelper(CORINFO_HELP_NEWARR_1_OBJ) &&
        newArrayCall->AsCall()->gtCallMethHnd != eeFindHelper(CORINFO_HELP_NEWARR_1_VC) &&
        newArrayCall->AsCall()->gtCallMethHnd != eeFindHelper(CORINFO_HELP_NEWARR_1_ALIGN8)
#ifdef FEATURE_READYTORUN
        && newArrayCall->AsCall()->gtCallMethHnd != eeFindHelper(CORINFO_HELP_READYTORUN_NEWARR_1)
#endif
            )
    {
        if (newArrayCall->AsCall()->gtCallMethHnd != eeFindHelper(CORINFO_HELP_NEW_MDARR))
        {
            return nullptr;
        }

        isMDArray = true;
    }

    CORINFO_CLASS_HANDLE arrayClsHnd = (CORINFO_CLASS_HANDLE)newArrayCall->AsCall()->compileTimeHelperArgumentHandle;

    //
    // Make sure we found a compile time handle to the array
    //

    if (!arrayClsHnd)
    {
        return nullptr;
    }

    unsigned rank = 0;
    S_UINT32 numElements;

    if (isMDArray)
    {
        rank = info.compCompHnd->getArrayRank(arrayClsHnd);

        if (rank == 0)
        {
            return nullptr;
        }

        assert(newArrayCall->AsCall()->gtArgs.CountArgs() == 3);
        GenTree* numArgsArg = newArrayCall->AsCall()->gtArgs.GetArgByIndex(1)->GetNode();
        GenTree* argsArg    = newArrayCall->AsCall()->gtArgs.GetArgByIndex(2)->GetNode();

        //
        // The number of arguments should be a constant between 1 and 64. The rank can't be 0
        // so at least one length must be present and the rank can't exceed 32 so there can
        // be at most 64 arguments - 32 lengths and 32 lower bounds.
        //

        if ((!numArgsArg->IsCnsIntOrI()) || (numArgsArg->AsIntCon()->IconValue() < 1) ||
            (numArgsArg->AsIntCon()->IconValue() > 64))
        {
            return nullptr;
        }

        unsigned numArgs = static_cast<unsigned>(numArgsArg->AsIntCon()->IconValue());
        bool     lowerBoundsSpecified;

        if (numArgs == rank * 2)
        {
            lowerBoundsSpecified = true;
        }
        else if (numArgs == rank)
        {
            lowerBoundsSpecified = false;

            //
            // If the rank is 1 and a lower bound isn't specified then the runtime creates
            // a SDArray. Note that even if a lower bound is specified it can be 0 and then
            // we get a SDArray as well, see the for loop below.
            //

            if (rank == 1)
            {
                isMDArray = false;
            }
        }
        else
        {
            return nullptr;
        }

        //
        // The rank is known to be at least 1 so we can start with numElements being 1
        // to avoid the need to special case the first dimension.
        //

        numElements = S_UINT32(1);

        struct Match
        {
            static bool IsArgsFieldInit(GenTree* tree, unsigned index, unsigned lvaNewObjArrayArgs)
            {
                return (tree->OperGet() == GT_ASG) && IsArgsFieldIndir(tree->gtGetOp1(), index, lvaNewObjArrayArgs) &&
                       IsArgsAddr(tree->gtGetOp1()->gtGetOp1()->gtGetOp1(), lvaNewObjArrayArgs);
            }

            static bool IsArgsFieldIndir(GenTree* tree, unsigned index, unsigned lvaNewObjArrayArgs)
            {
                return (tree->OperGet() == GT_IND) && (tree->gtGetOp1()->OperGet() == GT_ADD) &&
                       (tree->gtGetOp1()->gtGetOp2()->IsIntegralConst(sizeof(INT32) * index)) &&
                       IsArgsAddr(tree->gtGetOp1()->gtGetOp1(), lvaNewObjArrayArgs);
            }

            static bool IsArgsAddr(GenTree* tree, unsigned lvaNewObjArrayArgs)
            {
                return (tree->OperGet() == GT_ADDR) && (tree->gtGetOp1()->OperGet() == GT_LCL_VAR) &&
                       (tree->gtGetOp1()->AsLclVar()->GetLclNum() == lvaNewObjArrayArgs);
            }

            static bool IsComma(GenTree* tree)
            {
                return (tree != nullptr) && (tree->OperGet() == GT_COMMA);
            }
        };

        unsigned argIndex = 0;
        GenTree* comma;

        for (comma = argsArg; Match::IsComma(comma); comma = comma->gtGetOp2())
        {
            if (lowerBoundsSpecified)
            {
                //
                // In general lower bounds can be ignored because they're not needed to
                // calculate the total number of elements. But for single dimensional arrays
                // we need to know if the lower bound is 0 because in this case the runtime
                // creates a SDArray and this affects the way the array data offset is calculated.
                //

                if (rank == 1)
                {
                    GenTree* lowerBoundAssign = comma->gtGetOp1();
                    assert(Match::IsArgsFieldInit(lowerBoundAssign, argIndex, lvaNewObjArrayArgs));
                    GenTree* lowerBoundNode = lowerBoundAssign->gtGetOp2();

                    if (lowerBoundNode->IsIntegralConst(0))
                    {
                        isMDArray = false;
                    }
                }

                comma = comma->gtGetOp2();
                argIndex++;
            }

            GenTree* lengthNodeAssign = comma->gtGetOp1();
            assert(Match::IsArgsFieldInit(lengthNodeAssign, argIndex, lvaNewObjArrayArgs));
            GenTree* lengthNode = lengthNodeAssign->gtGetOp2();

            if (!lengthNode->IsCnsIntOrI())
            {
                return nullptr;
            }

            numElements *= S_SIZE_T(lengthNode->AsIntCon()->IconValue());
            argIndex++;
        }

        assert((comma != nullptr) && Match::IsArgsAddr(comma, lvaNewObjArrayArgs));

        if (argIndex != numArgs)
        {
            return nullptr;
        }
    }
    else
    {
        //
        // Make sure there are exactly two arguments:  the array class and
        // the number of elements.
        //

        GenTree* arrayLengthNode;

#ifdef FEATURE_READYTORUN
        if (newArrayCall->AsCall()->gtCallMethHnd == eeFindHelper(CORINFO_HELP_READYTORUN_NEWARR_1))
        {
            // Array length is 1st argument for readytorun helper
            arrayLengthNode = newArrayCall->AsCall()->gtArgs.GetArgByIndex(0)->GetNode();
        }
        else
#endif
        {
            // Array length is 2nd argument for regular helper
            arrayLengthNode = newArrayCall->AsCall()->gtArgs.GetArgByIndex(1)->GetNode();
        }

        //
        // This optimization is only valid for a constant array size.
        //
        if (arrayLengthNode->gtOper != GT_CNS_INT)
        {
            return nullptr;
        }

        numElements = S_SIZE_T(arrayLengthNode->AsIntCon()->gtIconVal);

        if (!info.compCompHnd->isSDArray(arrayClsHnd))
        {
            return nullptr;
        }
    }

    CORINFO_CLASS_HANDLE elemClsHnd;
    var_types            elementType = JITtype2varType(info.compCompHnd->getChildType(arrayClsHnd, &elemClsHnd));

    //
    // Note that genTypeSize will return zero for non primitive types, which is exactly
    // what we want (size will then be 0, and we will catch this in the conditional below).
    // Note that we don't expect this to fail for valid binaries, so we assert in the
    // non-verification case (the verification case should not assert but rather correctly
    // handle bad binaries).  This assert is not guarding any specific invariant, but rather
    // saying that we don't expect this to happen, and if it is hit, we need to investigate
    // why.
    //

    S_UINT32 elemSize(genTypeSize(elementType));
    S_UINT32 size = elemSize * S_UINT32(numElements);

    if (size.IsOverflow())
    {
        return nullptr;
    }

    if ((size.Value() == 0) || (varTypeIsGC(elementType)))
    {
        return nullptr;
    }

    void* initData = info.compCompHnd->getArrayInitializationData(fieldToken, size.Value());
    if (!initData)
    {
        return nullptr;
    }

    //
    // At this point we are ready to commit to implementing the InitializeArray
    // intrinsic using a struct assignment.  Pop the arguments from the stack and
    // return the struct assignment node.
    //

    impPopStack();
    impPopStack();

    const unsigned blkSize = size.Value();
    unsigned       dataOffset;

    if (isMDArray)
    {
        dataOffset = eeGetMDArrayDataOffset(rank);
    }
    else
    {
        dataOffset = eeGetArrayDataOffset();
    }

    GenTree* dstAddr = gtNewOperNode(GT_ADD, TYP_BYREF, arrayLocalNode, gtNewIconNode(dataOffset, TYP_I_IMPL));
    GenTree* dst     = new (this, GT_BLK) GenTreeBlk(GT_BLK, TYP_STRUCT, dstAddr, typGetBlkLayout(blkSize));
    GenTree* src     = gtNewIndOfIconHandleNode(TYP_STRUCT, (size_t)initData, GTF_ICON_CONST_PTR, true);

#ifdef DEBUG
    src->gtGetOp1()->AsIntCon()->gtTargetHandle = THT_InitializeArrayIntrinsics;
#endif

    return gtNewBlkOpNode(dst,   // dst
                          src,   // src
                          false, // volatile
                          true); // copyBlock
}

GenTree* Compiler::impCreateSpanIntrinsic(CORINFO_SIG_INFO* sig)
{
    assert(sig->numArgs == 1);
    assert(sig->sigInst.methInstCount == 1);

    GenTree* fieldTokenNode = impStackTop(0).val;

    //
    // Verify that the field token is known and valid.  Note that it's also
    // possible for the token to come from reflection, in which case we cannot do
    // the optimization and must therefore revert to calling the helper.  You can
    // see an example of this in bvt\DynIL\initarray2.exe (in Main).
    //

    // Check to see if the ldtoken helper call is what we see here.
    if (fieldTokenNode->gtOper != GT_CALL || (fieldTokenNode->AsCall()->gtCallType != CT_HELPER) ||
        (fieldTokenNode->AsCall()->gtCallMethHnd != eeFindHelper(CORINFO_HELP_FIELDDESC_TO_STUBRUNTIMEFIELD)))
    {
        return nullptr;
    }

    // Strip helper call away
    fieldTokenNode = fieldTokenNode->AsCall()->gtArgs.GetArgByIndex(0)->GetNode();
    if (fieldTokenNode->gtOper == GT_IND)
    {
        fieldTokenNode = fieldTokenNode->AsOp()->gtOp1;
    }

    // Check for constant
    if (fieldTokenNode->gtOper != GT_CNS_INT)
    {
        return nullptr;
    }

    CORINFO_FIELD_HANDLE fieldToken = (CORINFO_FIELD_HANDLE)fieldTokenNode->AsIntCon()->gtCompileTimeHandle;
    if (!fieldTokenNode->IsIconHandle(GTF_ICON_FIELD_HDL) || (fieldToken == nullptr))
    {
        return nullptr;
    }

    CORINFO_CLASS_HANDLE fieldOwnerHnd = info.compCompHnd->getFieldClass(fieldToken);

    CORINFO_CLASS_HANDLE fieldClsHnd;
    var_types            fieldElementType =
        JITtype2varType(info.compCompHnd->getFieldType(fieldToken, &fieldClsHnd, fieldOwnerHnd));
    unsigned totalFieldSize;

    // Most static initialization data fields are of some structure, but it is possible for them to be of various
    // primitive types as well
    if (fieldElementType == var_types::TYP_STRUCT)
    {
        totalFieldSize = info.compCompHnd->getClassSize(fieldClsHnd);
    }
    else
    {
        totalFieldSize = genTypeSize(fieldElementType);
    }

    // Limit to primitive or enum type - see ArrayNative::GetSpanDataFrom()
    CORINFO_CLASS_HANDLE targetElemHnd = sig->sigInst.methInst[0];
    if (info.compCompHnd->getTypeForPrimitiveValueClass(targetElemHnd) == CORINFO_TYPE_UNDEF)
    {
        return nullptr;
    }

    const unsigned targetElemSize = info.compCompHnd->getClassSize(targetElemHnd);
    assert(targetElemSize != 0);

    const unsigned count = totalFieldSize / targetElemSize;
    if (count == 0)
    {
        return nullptr;
    }

    void* data = info.compCompHnd->getArrayInitializationData(fieldToken, totalFieldSize);
    if (!data)
    {
        return nullptr;
    }

    //
    // Ready to commit to the work
    //

    impPopStack();

    // Turn count and pointer value into constants.
    GenTree* lengthValue  = gtNewIconNode(count, TYP_INT);
    GenTree* pointerValue = gtNewIconHandleNode((size_t)data, GTF_ICON_CONST_PTR);

    // Construct ReadOnlySpan<T> to return.
    CORINFO_CLASS_HANDLE spanHnd     = sig->retTypeClass;
    unsigned             spanTempNum = lvaGrabTemp(true DEBUGARG("ReadOnlySpan<T> for CreateSpan<T>"));
    lvaSetStruct(spanTempNum, spanHnd, false);

    GenTreeLclFld* pointerField    = gtNewLclFldNode(spanTempNum, TYP_BYREF, 0);
    GenTree*       pointerFieldAsg = gtNewAssignNode(pointerField, pointerValue);

    GenTreeLclFld* lengthField    = gtNewLclFldNode(spanTempNum, TYP_INT, TARGET_POINTER_SIZE);
    GenTree*       lengthFieldAsg = gtNewAssignNode(lengthField, lengthValue);

    // Now append a few statements the initialize the span
    impAppendTree(lengthFieldAsg, CHECK_SPILL_NONE, impCurStmtDI);
    impAppendTree(pointerFieldAsg, CHECK_SPILL_NONE, impCurStmtDI);

    // And finally create a tree that points at the span.
    return impCreateLocalNode(spanTempNum DEBUGARG(0));
}

//------------------------------------------------------------------------
// impIntrinsic: possibly expand intrinsic call into alternate IR sequence
//
// Arguments:
//    newobjThis - for constructor calls, the tree for the newly allocated object
//    clsHnd - handle for the intrinsic method's class
//    method - handle for the intrinsic method
//    sig    - signature of the intrinsic method
//    methodFlags - CORINFO_FLG_XXX flags of the intrinsic method
//    memberRef - the token for the intrinsic method
//    readonlyCall - true if call has a readonly prefix
//    tailCall - true if call is in tail position
//    pConstrainedResolvedToken -- resolved token for constrained call, or nullptr
//       if call is not constrained
//    constraintCallThisTransform -- this transform to apply for a constrained call
//    pIntrinsicName [OUT] -- intrinsic name (see enumeration in namedintrinsiclist.h)
//       for "traditional" jit intrinsics
//    isSpecialIntrinsic [OUT] -- set true if intrinsic expansion is a call
//       that is amenable to special downstream optimization opportunities
//
// Returns:
//    IR tree to use in place of the call, or nullptr if the jit should treat
//    the intrinsic call like a normal call.
//
//    pIntrinsicName set to non-illegal value if the call is recognized as a
//    traditional jit intrinsic, even if the intrinsic is not expaned.
//
//    isSpecial set true if the expansion is subject to special
//    optimizations later in the jit processing
//
// Notes:
//    On success the IR tree may be a call to a different method or an inline
//    sequence. If it is a call, then the intrinsic processing here is responsible
//    for handling all the special cases, as upon return to impImportCall
//    expanded intrinsics bypass most of the normal call processing.
//
//    Intrinsics are generally not recognized in minopts and debug codegen.
//
//    However, certain traditional intrinsics are identifed as "must expand"
//    if there is no fallback implementation to invoke; these must be handled
//    in all codegen modes.
//
//    New style intrinsics (where the fallback implementation is in IL) are
//    identified as "must expand" if they are invoked from within their
//    own method bodies.
//
GenTree* Compiler::impIntrinsic(GenTree*                newobjThis,
                                CORINFO_CLASS_HANDLE    clsHnd,
                                CORINFO_METHOD_HANDLE   method,
                                CORINFO_SIG_INFO*       sig,
                                unsigned                methodFlags,
                                int                     memberRef,
                                bool                    readonlyCall,
                                bool                    tailCall,
                                CORINFO_RESOLVED_TOKEN* pConstrainedResolvedToken,
                                CORINFO_THIS_TRANSFORM  constraintCallThisTransform,
                                NamedIntrinsic*         pIntrinsicName,
                                bool*                   isSpecialIntrinsic)
{
    bool       mustExpand  = false;
    bool       isSpecial   = false;
    const bool isIntrinsic = (methodFlags & CORINFO_FLG_INTRINSIC) != 0;

    NamedIntrinsic ni = lookupNamedIntrinsic(method);

    if (isIntrinsic)
    {
        // The recursive non-virtual calls to Jit intrinsics are must-expand by convention.
        mustExpand = gtIsRecursiveCall(method) && !(methodFlags & CORINFO_FLG_VIRTUAL);
    }
    else
    {
        // For mismatched VM (AltJit) we want to check all methods as intrinsic to ensure
        // we get more accurate codegen. This particularly applies to HWIntrinsic usage
        assert(!info.compMatchedVM);
    }

    // We specially support the following on all platforms to allow for dead
    // code optimization and to more generally support recursive intrinsics.

    if (isIntrinsic)
    {
        if (ni == NI_IsSupported_True)
        {
            assert(sig->numArgs == 0);
            return gtNewIconNode(true);
        }

        if (ni == NI_IsSupported_False)
        {
            assert(sig->numArgs == 0);
            return gtNewIconNode(false);
        }

        if (ni == NI_Throw_PlatformNotSupportedException)
        {
            return impUnsupportedNamedIntrinsic(CORINFO_HELP_THROW_PLATFORM_NOT_SUPPORTED, method, sig, mustExpand);
        }

        if ((ni > NI_SRCS_UNSAFE_START) && (ni < NI_SRCS_UNSAFE_END))
        {
            assert(!mustExpand);
            return impSRCSUnsafeIntrinsic(ni, clsHnd, method, sig);
        }
    }

#ifdef FEATURE_HW_INTRINSICS
    if ((ni > NI_HW_INTRINSIC_START) && (ni < NI_HW_INTRINSIC_END))
    {
        if (!isIntrinsic)
        {
#if defined(TARGET_XARCH)
            // We can't guarantee that all overloads for the xplat intrinsics can be
            // handled by the AltJit, so limit only the platform specific intrinsics
            assert((NI_Vector256_Xor + 1) == NI_X86Base_BitScanForward);

            if (ni < NI_Vector256_Xor)
#elif defined(TARGET_ARM64)
            // We can't guarantee that all overloads for the xplat intrinsics can be
            // handled by the AltJit, so limit only the platform specific intrinsics
            assert((NI_Vector128_Xor + 1) == NI_AdvSimd_Abs);

            if (ni < NI_Vector128_Xor)
#else
#error Unsupported platform
#endif
            {
                // Several of the NI_Vector64/128/256 APIs do not have
                // all overloads as intrinsic today so they will assert
                return nullptr;
            }
        }

        GenTree* hwintrinsic = impHWIntrinsic(ni, clsHnd, method, sig, mustExpand);

        if (mustExpand && (hwintrinsic == nullptr))
        {
            return impUnsupportedNamedIntrinsic(CORINFO_HELP_THROW_NOT_IMPLEMENTED, method, sig, mustExpand);
        }

        return hwintrinsic;
    }

    if (isIntrinsic && (ni > NI_SIMD_AS_HWINTRINSIC_START) && (ni < NI_SIMD_AS_HWINTRINSIC_END))
    {
        // These intrinsics aren't defined recursively and so they will never be mustExpand
        // Instead, they provide software fallbacks that will be executed instead.

        assert(!mustExpand);
        return impSimdAsHWIntrinsic(ni, clsHnd, method, sig, newobjThis);
    }
#endif // FEATURE_HW_INTRINSICS

    if (!isIntrinsic)
    {
        // Outside the cases above, there are many intrinsics which apply to only a
        // subset of overload and where simply matching by name may cause downstream
        // asserts or other failures. Math.Min is one example, where it only applies
        // to the floating-point overloads.
        return nullptr;
    }

    *pIntrinsicName = ni;

    if (ni == NI_System_StubHelpers_GetStubContext)
    {
        // must be done regardless of DbgCode and MinOpts
        return gtNewLclvNode(lvaStubArgumentVar, TYP_I_IMPL);
    }

    if (ni == NI_System_StubHelpers_NextCallReturnAddress)
    {
        // For now we just avoid inlining anything into these methods since
        // this intrinsic is only rarely used. We could do this better if we
        // wanted to by trying to match which call is the one we need to get
        // the return address of.
        info.compHasNextCallRetAddr = true;
        return new (this, GT_LABEL) GenTree(GT_LABEL, TYP_I_IMPL);
    }

    switch (ni)
    {
        // CreateSpan must be expanded for NativeAOT
        case NI_System_Runtime_CompilerServices_RuntimeHelpers_CreateSpan:
        case NI_System_Runtime_CompilerServices_RuntimeHelpers_InitializeArray:
            mustExpand |= IsTargetAbi(CORINFO_NATIVEAOT_ABI);
            break;

        case NI_Internal_Runtime_MethodTable_Of:
        case NI_System_Activator_AllocatorOf:
        case NI_System_Activator_DefaultConstructorOf:
        case NI_System_EETypePtr_EETypePtrOf:
            mustExpand = true;
            break;

        default:
            break;
    }

    GenTree* retNode = nullptr;

    // Under debug and minopts, only expand what is required.
    // NextCallReturnAddress intrinsic returns the return address of the next call.
    // If that call is an intrinsic and is expanded, codegen for NextCallReturnAddress will fail.
    // To avoid that we conservatively expand only required intrinsics in methods that call
    // the NextCallReturnAddress intrinsic.
    if (!mustExpand && (opts.OptimizationDisabled() || info.compHasNextCallRetAddr))
    {
        *pIntrinsicName = NI_Illegal;
        return retNode;
    }

    CorInfoType callJitType = sig->retType;
    var_types   callType    = JITtype2varType(callJitType);

    /* First do the intrinsics which are always smaller than a call */

    if (ni != NI_Illegal)
    {
        assert(retNode == nullptr);
        switch (ni)
        {
            case NI_Array_Address:
            case NI_Array_Get:
            case NI_Array_Set:
                retNode = impArrayAccessIntrinsic(clsHnd, sig, memberRef, readonlyCall, ni);
                break;

            case NI_System_String_Equals:
            {
                retNode = impStringEqualsOrStartsWith(/*startsWith:*/ false, sig, methodFlags);
                break;
            }

            case NI_System_MemoryExtensions_Equals:
            case NI_System_MemoryExtensions_SequenceEqual:
            {
                retNode = impSpanEqualsOrStartsWith(/*startsWith:*/ false, sig, methodFlags);
                break;
            }

            case NI_System_String_StartsWith:
            {
                retNode = impStringEqualsOrStartsWith(/*startsWith:*/ true, sig, methodFlags);
                break;
            }

            case NI_System_MemoryExtensions_StartsWith:
            {
                retNode = impSpanEqualsOrStartsWith(/*startsWith:*/ true, sig, methodFlags);
                break;
            }

            case NI_System_MemoryExtensions_AsSpan:
            case NI_System_String_op_Implicit:
            {
                assert(sig->numArgs == 1);
                isSpecial = impStackTop().val->OperIs(GT_CNS_STR);
                break;
            }

            case NI_System_String_get_Chars:
            {
                GenTree* op2  = impPopStack().val;
                GenTree* op1  = impPopStack().val;
                GenTree* addr = gtNewIndexAddr(op1, op2, TYP_USHORT, NO_CLASS_HANDLE, OFFSETOF__CORINFO_String__chars,
                                               OFFSETOF__CORINFO_String__stringLen);
                retNode = gtNewIndexIndir(addr->AsIndexAddr());
                break;
            }

            case NI_System_String_get_Length:
            {
                GenTree* op1 = impPopStack().val;
                if (op1->OperIs(GT_CNS_STR))
                {
                    // Optimize `ldstr + String::get_Length()` to CNS_INT
                    // e.g. "Hello".Length => 5
                    GenTreeIntCon* iconNode = gtNewStringLiteralLength(op1->AsStrCon());
                    if (iconNode != nullptr)
                    {
                        retNode = iconNode;
                        break;
                    }
                }
                GenTreeArrLen* arrLen = gtNewArrLen(TYP_INT, op1, OFFSETOF__CORINFO_String__stringLen, compCurBB);
                op1                   = arrLen;

                // Getting the length of a null string should throw
                op1->gtFlags |= GTF_EXCEPT;

                retNode = op1;
                break;
            }

            case NI_System_Runtime_CompilerServices_RuntimeHelpers_CreateSpan:
            {
                retNode = impCreateSpanIntrinsic(sig);
                break;
            }

            case NI_System_Runtime_CompilerServices_RuntimeHelpers_InitializeArray:
            {
                retNode = impInitializeArrayIntrinsic(sig);
                break;
            }

            case NI_System_Runtime_CompilerServices_RuntimeHelpers_IsKnownConstant:
            {
                GenTree* op1 = impPopStack().val;
                if (op1->OperIsConst())
                {
                    // op1 is a known constant, replace with 'true'.
                    retNode = gtNewIconNode(1);
                    JITDUMP("\nExpanding RuntimeHelpers.IsKnownConstant to true early\n");
                    // We can also consider FTN_ADDR and typeof(T) here
                }
                else
                {
                    // op1 is not a known constant, we'll do the expansion in morph
                    retNode = new (this, GT_INTRINSIC) GenTreeIntrinsic(TYP_INT, op1, ni, method);
                    JITDUMP("\nConverting RuntimeHelpers.IsKnownConstant to:\n");
                    DISPTREE(retNode);
                }
                break;
            }

            case NI_Internal_Runtime_MethodTable_Of:
            case NI_System_Activator_AllocatorOf:
            case NI_System_Activator_DefaultConstructorOf:
            case NI_System_EETypePtr_EETypePtrOf:
            {
                assert(IsTargetAbi(CORINFO_NATIVEAOT_ABI)); // Only NativeAOT supports it.
                CORINFO_RESOLVED_TOKEN resolvedToken;
                resolvedToken.tokenContext = impTokenLookupContextHandle;
                resolvedToken.tokenScope   = info.compScopeHnd;
                resolvedToken.token        = memberRef;
                resolvedToken.tokenType    = CORINFO_TOKENKIND_Method;

                CORINFO_GENERICHANDLE_RESULT embedInfo;
                info.compCompHnd->expandRawHandleIntrinsic(&resolvedToken, &embedInfo);

                GenTree* rawHandle = impLookupToTree(&resolvedToken, &embedInfo.lookup, gtTokenToIconFlags(memberRef),
                                                     embedInfo.compileTimeHandle);
                if (rawHandle == nullptr)
                {
                    return nullptr;
                }

                noway_assert(genTypeSize(rawHandle->TypeGet()) == genTypeSize(TYP_I_IMPL));

                unsigned rawHandleSlot = lvaGrabTemp(true DEBUGARG("rawHandle"));
                impAssignTempGen(rawHandleSlot, rawHandle, clsHnd, CHECK_SPILL_NONE);

                GenTree*  lclVar     = gtNewLclvNode(rawHandleSlot, TYP_I_IMPL);
                GenTree*  lclVarAddr = gtNewOperNode(GT_ADDR, TYP_I_IMPL, lclVar);
                var_types resultType = JITtype2varType(sig->retType);
                if (resultType == TYP_STRUCT)
                {
                    retNode = gtNewObjNode(sig->retTypeClass, lclVarAddr);
                }
                else
                {
                    retNode = gtNewIndir(resultType, lclVarAddr);
                }
                break;
            }

            case NI_System_Span_get_Item:
            case NI_System_ReadOnlySpan_get_Item:
            {
                // Have index, stack pointer-to Span<T> s on the stack. Expand to:
                //
                // For Span<T>
                //   Comma
                //     BoundsCheck(index, s->_length)
                //     s->_reference + index * sizeof(T)
                //
                // For ReadOnlySpan<T> -- same expansion, as it now returns a readonly ref
                //
                // Signature should show one class type parameter, which
                // we need to examine.
                assert(sig->sigInst.classInstCount == 1);
                assert(sig->numArgs == 1);
                CORINFO_CLASS_HANDLE spanElemHnd = sig->sigInst.classInst[0];
                const unsigned       elemSize    = info.compCompHnd->getClassSize(spanElemHnd);
                assert(elemSize > 0);

                const bool isReadOnly = (ni == NI_System_ReadOnlySpan_get_Item);

                JITDUMP("\nimpIntrinsic: Expanding %sSpan<T>.get_Item, T=%s, sizeof(T)=%u\n",
                        isReadOnly ? "ReadOnly" : "", eeGetClassName(spanElemHnd), elemSize);

                GenTree* index          = impPopStack().val;
                GenTree* ptrToSpan      = impPopStack().val;
                GenTree* indexClone     = nullptr;
                GenTree* ptrToSpanClone = nullptr;
                assert(genActualType(index) == TYP_INT);
                assert(ptrToSpan->TypeGet() == TYP_BYREF);

#if defined(DEBUG)
                if (verbose)
                {
                    printf("with ptr-to-span\n");
                    gtDispTree(ptrToSpan);
                    printf("and index\n");
                    gtDispTree(index);
                }
#endif // defined(DEBUG)

                // We need to use both index and ptr-to-span twice, so clone or spill.
                index = impCloneExpr(index, &indexClone, NO_CLASS_HANDLE, CHECK_SPILL_ALL,
                                     nullptr DEBUGARG("Span.get_Item index"));
                ptrToSpan = impCloneExpr(ptrToSpan, &ptrToSpanClone, NO_CLASS_HANDLE, CHECK_SPILL_ALL,
                                         nullptr DEBUGARG("Span.get_Item ptrToSpan"));

                // Bounds check
                CORINFO_FIELD_HANDLE lengthHnd    = info.compCompHnd->getFieldInClass(clsHnd, 1);
                const unsigned       lengthOffset = info.compCompHnd->getFieldOffset(lengthHnd);
                GenTree*             length       = gtNewFieldRef(TYP_INT, lengthHnd, ptrToSpan, lengthOffset);
                GenTree* boundsCheck = new (this, GT_BOUNDS_CHECK) GenTreeBoundsChk(index, length, SCK_RNGCHK_FAIL);

                // Element access
                index = indexClone;

#ifdef TARGET_64BIT
                if (index->OperGet() == GT_CNS_INT)
                {
                    index->gtType = TYP_I_IMPL;
                }
                else
                {
                    index = gtNewCastNode(TYP_I_IMPL, index, true, TYP_I_IMPL);
                }
#endif

                if (elemSize != 1)
                {
                    GenTree* sizeofNode = gtNewIconNode(static_cast<ssize_t>(elemSize), TYP_I_IMPL);
                    index               = gtNewOperNode(GT_MUL, TYP_I_IMPL, index, sizeofNode);
                }

                CORINFO_FIELD_HANDLE ptrHnd    = info.compCompHnd->getFieldInClass(clsHnd, 0);
                const unsigned       ptrOffset = info.compCompHnd->getFieldOffset(ptrHnd);
                GenTree*             data      = gtNewFieldRef(TYP_BYREF, ptrHnd, ptrToSpanClone, ptrOffset);
                GenTree*             result    = gtNewOperNode(GT_ADD, TYP_BYREF, data, index);

                // Prepare result
                var_types resultType = JITtype2varType(sig->retType);
                assert(resultType == result->TypeGet());
                retNode = gtNewOperNode(GT_COMMA, resultType, boundsCheck, result);

                break;
            }

            case NI_System_RuntimeTypeHandle_GetValueInternal:
            {
                GenTree* op1 = impStackTop(0).val;
                if (op1->gtOper == GT_CALL && (op1->AsCall()->gtCallType == CT_HELPER) &&
                    gtIsTypeHandleToRuntimeTypeHandleHelper(op1->AsCall()))
                {
                    // Old tree
                    // Helper-RuntimeTypeHandle -> TreeToGetNativeTypeHandle
                    //
                    // New tree
                    // TreeToGetNativeTypeHandle

                    // Remove call to helper and return the native TypeHandle pointer that was the parameter
                    // to that helper.

                    op1 = impPopStack().val;

                    // Get native TypeHandle argument to old helper
                    assert(op1->AsCall()->gtArgs.CountArgs() == 1);
                    op1     = op1->AsCall()->gtArgs.GetArgByIndex(0)->GetNode();
                    retNode = op1;
                }
                // Call the regular function.
                break;
            }

            case NI_System_Type_GetTypeFromHandle:
            {
                GenTree*        op1 = impStackTop(0).val;
                CorInfoHelpFunc typeHandleHelper;
                if (op1->gtOper == GT_CALL && (op1->AsCall()->gtCallType == CT_HELPER) &&
                    gtIsTypeHandleToRuntimeTypeHandleHelper(op1->AsCall(), &typeHandleHelper))
                {
                    op1 = impPopStack().val;
                    // Replace helper with a more specialized helper that returns RuntimeType
                    if (typeHandleHelper == CORINFO_HELP_TYPEHANDLE_TO_RUNTIMETYPEHANDLE)
                    {
                        typeHandleHelper = CORINFO_HELP_TYPEHANDLE_TO_RUNTIMETYPE;
                    }
                    else
                    {
                        assert(typeHandleHelper == CORINFO_HELP_TYPEHANDLE_TO_RUNTIMETYPEHANDLE_MAYBENULL);
                        typeHandleHelper = CORINFO_HELP_TYPEHANDLE_TO_RUNTIMETYPE_MAYBENULL;
                    }
                    assert(op1->AsCall()->gtArgs.CountArgs() == 1);
                    op1 = gtNewHelperCallNode(typeHandleHelper, TYP_REF,
                                              op1->AsCall()->gtArgs.GetArgByIndex(0)->GetEarlyNode());
                    op1->gtType = TYP_REF;
                    retNode     = op1;
                }
                break;
            }

            case NI_System_Type_op_Equality:
            case NI_System_Type_op_Inequality:
            {
                JITDUMP("Importing Type.op_*Equality intrinsic\n");
                GenTree* op1     = impStackTop(1).val;
                GenTree* op2     = impStackTop(0).val;
                GenTree* optTree = gtFoldTypeEqualityCall(ni == NI_System_Type_op_Equality, op1, op2);
                if (optTree != nullptr)
                {
                    // Success, clean up the evaluation stack.
                    impPopStack();
                    impPopStack();

                    // See if we can optimize even further, to a handle compare.
                    optTree = gtFoldTypeCompare(optTree);

                    // See if we can now fold a handle compare to a constant.
                    optTree = gtFoldExpr(optTree);

                    retNode = optTree;
                }
                else
                {
                    // Retry optimizing these later
                    isSpecial = true;
                }
                break;
            }

            case NI_System_Enum_HasFlag:
            {
                GenTree* thisOp  = impStackTop(1).val;
                GenTree* flagOp  = impStackTop(0).val;
                GenTree* optTree = gtOptimizeEnumHasFlag(thisOp, flagOp);

                if (optTree != nullptr)
                {
                    // Optimization successful. Pop the stack for real.
                    impPopStack();
                    impPopStack();
                    retNode = optTree;
                }
                else
                {
                    // Retry optimizing this during morph.
                    isSpecial = true;
                }

                break;
            }

            case NI_System_Type_IsAssignableFrom:
            {
                GenTree* typeTo   = impStackTop(1).val;
                GenTree* typeFrom = impStackTop(0).val;

                retNode = impTypeIsAssignable(typeTo, typeFrom);
                break;
            }

            case NI_System_Type_IsAssignableTo:
            {
                GenTree* typeTo   = impStackTop(0).val;
                GenTree* typeFrom = impStackTop(1).val;

                retNode = impTypeIsAssignable(typeTo, typeFrom);
                break;
            }

            case NI_System_Type_get_IsValueType:
            case NI_System_Type_get_IsByRefLike:
            {
                // Optimize
                //
                //   call Type.GetTypeFromHandle (which is replaced with CORINFO_HELP_TYPEHANDLE_TO_RUNTIMETYPE)
                //   call Type.IsXXX
                //
                // to `true` or `false`
                // e.g., `typeof(int).IsValueType` => `true`
                // e.g., `typeof(Span<int>).IsByRefLike` => `true`
                if (impStackTop().val->IsCall())
                {
                    GenTreeCall* call = impStackTop().val->AsCall();
                    if (call->gtCallMethHnd == eeFindHelper(CORINFO_HELP_TYPEHANDLE_TO_RUNTIMETYPE))
                    {
                        assert(call->gtArgs.CountArgs() == 1);
                        CORINFO_CLASS_HANDLE hClass =
                            gtGetHelperArgClassHandle(call->gtArgs.GetArgByIndex(0)->GetEarlyNode());
                        if (hClass != NO_CLASS_HANDLE)
                        {
                            switch (ni)
                            {
                                case NI_System_Type_get_IsValueType:
                                    retNode = gtNewIconNode(eeIsValueClass(hClass) ? 1 : 0);
                                    break;
                                case NI_System_Type_get_IsByRefLike:
                                    retNode = gtNewIconNode(
                                        (info.compCompHnd->getClassAttribs(hClass) & CORINFO_FLG_BYREF_LIKE) ? 1 : 0);
                                    break;
                                default:
                                    NO_WAY("Intrinsic not supported in this path.");
                            }
                            impPopStack(); // drop CORINFO_HELP_TYPEHANDLE_TO_RUNTIMETYPE call
                        }
                    }
                }
                break;
            }

            case NI_System_Threading_Thread_get_ManagedThreadId:
            {
                if (impStackTop().val->OperIs(GT_RET_EXPR))
                {
                    GenTreeCall* call = impStackTop().val->AsRetExpr()->gtInlineCandidate->AsCall();
                    if (call->gtCallMoreFlags & GTF_CALL_M_SPECIAL_INTRINSIC)
                    {
                        if (lookupNamedIntrinsic(call->gtCallMethHnd) == NI_System_Threading_Thread_get_CurrentThread)
                        {
                            // drop get_CurrentThread() call
                            impPopStack();
                            call->ReplaceWith(gtNewNothingNode(), this);
                            retNode = gtNewHelperCallNode(CORINFO_HELP_GETCURRENTMANAGEDTHREADID, TYP_INT);
                        }
                    }
                }
                break;
            }

#ifdef TARGET_ARM64
            // Intrinsify Interlocked.Or and Interlocked.And only for arm64-v8.1 (and newer)
            // TODO-CQ: Implement for XArch (https://github.com/dotnet/runtime/issues/32239).
            case NI_System_Threading_Interlocked_Or:
            case NI_System_Threading_Interlocked_And:
            {
                if (compOpportunisticallyDependsOn(InstructionSet_Atomics))
                {
                    assert(sig->numArgs == 2);
                    GenTree*   op2 = impPopStack().val;
                    GenTree*   op1 = impPopStack().val;
                    genTreeOps op  = (ni == NI_System_Threading_Interlocked_Or) ? GT_XORR : GT_XAND;
                    retNode        = gtNewOperNode(op, genActualType(callType), op1, op2);
                    retNode->gtFlags |= GTF_GLOB_REF | GTF_ASG;
                }
                break;
            }
#endif // TARGET_ARM64

#if defined(TARGET_XARCH) || defined(TARGET_ARM64)
            // TODO-ARM-CQ: reenable treating InterlockedCmpXchg32 operation as intrinsic
            case NI_System_Threading_Interlocked_CompareExchange:
            {
                var_types retType = JITtype2varType(sig->retType);
                if ((retType == TYP_LONG) && (TARGET_POINTER_SIZE == 4))
                {
                    break;
                }
                if ((retType != TYP_INT) && (retType != TYP_LONG))
                {
                    break;
                }

                assert(callType != TYP_STRUCT);
                assert(sig->numArgs == 3);

                GenTree* op3 = impPopStack().val; // comparand
                GenTree* op2 = impPopStack().val; // value
                GenTree* op1 = impPopStack().val; // location

                GenTree* node = new (this, GT_CMPXCHG) GenTreeCmpXchg(genActualType(callType), op1, op2, op3);

                node->AsCmpXchg()->gtOpLocation->gtFlags |= GTF_DONT_CSE;
                retNode = node;
                break;
            }

            case NI_System_Threading_Interlocked_Exchange:
            case NI_System_Threading_Interlocked_ExchangeAdd:
            {
                assert(callType != TYP_STRUCT);
                assert(sig->numArgs == 2);

                var_types retType = JITtype2varType(sig->retType);
                if ((retType == TYP_LONG) && (TARGET_POINTER_SIZE == 4))
                {
                    break;
                }
                if ((retType != TYP_INT) && (retType != TYP_LONG))
                {
                    break;
                }

                GenTree* op2 = impPopStack().val;
                GenTree* op1 = impPopStack().val;

                // This creates:
                //   val
                // XAdd
                //   addr
                //     field (for example)
                //
                // In the case where the first argument is the address of a local, we might
                // want to make this *not* make the var address-taken -- but atomic instructions
                // on a local are probably pretty useless anyway, so we probably don't care.

                op1 = gtNewOperNode(ni == NI_System_Threading_Interlocked_ExchangeAdd ? GT_XADD : GT_XCHG,
                                    genActualType(callType), op1, op2);
                op1->gtFlags |= GTF_GLOB_REF | GTF_ASG;
                retNode = op1;
                break;
            }
#endif // defined(TARGET_XARCH) || defined(TARGET_ARM64)

            case NI_System_Threading_Interlocked_MemoryBarrier:
            case NI_System_Threading_Interlocked_ReadMemoryBarrier:
            {
                assert(sig->numArgs == 0);

                GenTree* op1 = new (this, GT_MEMORYBARRIER) GenTree(GT_MEMORYBARRIER, TYP_VOID);
                op1->gtFlags |= GTF_GLOB_REF | GTF_ASG;

                // On XARCH `NI_System_Threading_Interlocked_ReadMemoryBarrier` fences need not be emitted.
                // However, we still need to capture the effect on reordering.
                if (ni == NI_System_Threading_Interlocked_ReadMemoryBarrier)
                {
                    op1->gtFlags |= GTF_MEMORYBARRIER_LOAD;
                }

                retNode = op1;
                break;
            }

#ifdef FEATURE_HW_INTRINSICS
            case NI_System_Math_FusedMultiplyAdd:
            {
#ifdef TARGET_XARCH
                if (compExactlyDependsOn(InstructionSet_FMA))
                {
                    assert(varTypeIsFloating(callType));

                    // We are constructing a chain of intrinsics similar to:
                    //    return FMA.MultiplyAddScalar(
                    //        Vector128.CreateScalarUnsafe(x),
                    //        Vector128.CreateScalarUnsafe(y),
                    //        Vector128.CreateScalarUnsafe(z)
                    //    ).ToScalar();

                    GenTree* op3 = gtNewSimdHWIntrinsicNode(TYP_SIMD16, impPopStack().val,
                                                            NI_Vector128_CreateScalarUnsafe, callJitType, 16);
                    GenTree* op2 = gtNewSimdHWIntrinsicNode(TYP_SIMD16, impPopStack().val,
                                                            NI_Vector128_CreateScalarUnsafe, callJitType, 16);
                    GenTree* op1 = gtNewSimdHWIntrinsicNode(TYP_SIMD16, impPopStack().val,
                                                            NI_Vector128_CreateScalarUnsafe, callJitType, 16);
                    GenTree* res =
                        gtNewSimdHWIntrinsicNode(TYP_SIMD16, op1, op2, op3, NI_FMA_MultiplyAddScalar, callJitType, 16);

                    retNode = gtNewSimdHWIntrinsicNode(callType, res, NI_Vector128_ToScalar, callJitType, 16);
                    break;
                }
#elif defined(TARGET_ARM64)
                if (compExactlyDependsOn(InstructionSet_AdvSimd))
                {
                    assert(varTypeIsFloating(callType));

                    // We are constructing a chain of intrinsics similar to:
                    //    return AdvSimd.FusedMultiplyAddScalar(
                    //        Vector64.Create{ScalarUnsafe}(z),
                    //        Vector64.Create{ScalarUnsafe}(y),
                    //        Vector64.Create{ScalarUnsafe}(x)
                    //    ).ToScalar();

                    NamedIntrinsic createVector64 =
                        (callType == TYP_DOUBLE) ? NI_Vector64_Create : NI_Vector64_CreateScalarUnsafe;

                    constexpr unsigned int simdSize = 8;

                    GenTree* op3 =
                        gtNewSimdHWIntrinsicNode(TYP_SIMD8, impPopStack().val, createVector64, callJitType, simdSize);
                    GenTree* op2 =
                        gtNewSimdHWIntrinsicNode(TYP_SIMD8, impPopStack().val, createVector64, callJitType, simdSize);
                    GenTree* op1 =
                        gtNewSimdHWIntrinsicNode(TYP_SIMD8, impPopStack().val, createVector64, callJitType, simdSize);

                    // Note that AdvSimd.FusedMultiplyAddScalar(op1,op2,op3) corresponds to op1 + op2 * op3
                    // while Math{F}.FusedMultiplyAddScalar(op1,op2,op3) corresponds to op1 * op2 + op3
                    retNode = gtNewSimdHWIntrinsicNode(TYP_SIMD8, op3, op2, op1, NI_AdvSimd_FusedMultiplyAddScalar,
                                                       callJitType, simdSize);

                    retNode = gtNewSimdHWIntrinsicNode(callType, retNode, NI_Vector64_ToScalar, callJitType, simdSize);
                    break;
                }
#endif

                // TODO-CQ-XArch: Ideally we would create a GT_INTRINSIC node for fma, however, that currently
                // requires more extensive changes to valuenum to support methods with 3 operands

                // We want to generate a GT_INTRINSIC node in the case the call can't be treated as
                // a target intrinsic so that we can still benefit from CSE and constant folding.

                break;
            }
#endif // FEATURE_HW_INTRINSICS

            case NI_System_Math_Abs:
            case NI_System_Math_Acos:
            case NI_System_Math_Acosh:
            case NI_System_Math_Asin:
            case NI_System_Math_Asinh:
            case NI_System_Math_Atan:
            case NI_System_Math_Atanh:
            case NI_System_Math_Atan2:
            case NI_System_Math_Cbrt:
            case NI_System_Math_Ceiling:
            case NI_System_Math_Cos:
            case NI_System_Math_Cosh:
            case NI_System_Math_Exp:
            case NI_System_Math_Floor:
            case NI_System_Math_FMod:
            case NI_System_Math_ILogB:
            case NI_System_Math_Log:
            case NI_System_Math_Log2:
            case NI_System_Math_Log10:
            {
                retNode = impMathIntrinsic(method, sig, callType, ni, tailCall);
                break;
            }
#if defined(TARGET_ARM64)
            // ARM64 has fmax/fmin which are IEEE754:2019 minimum/maximum compatible
            // TODO-XARCH-CQ: Enable this for XARCH when one of the arguments is a constant
            // so we can then emit maxss/minss and avoid NaN/-0.0 handling
            case NI_System_Math_Max:
            case NI_System_Math_Min:
#endif

#if defined(FEATURE_HW_INTRINSICS) && defined(TARGET_XARCH)
            case NI_System_Math_Max:
            case NI_System_Math_Min:
            {
                assert(varTypeIsFloating(callType));
                assert(sig->numArgs == 2);

                GenTreeDblCon* cnsNode   = nullptr;
                GenTree*       otherNode = nullptr;

                GenTree* op2 = impStackTop().val;
                GenTree* op1 = impStackTop(1).val;

                if (op2->IsCnsFltOrDbl())
                {
                    cnsNode   = op2->AsDblCon();
                    otherNode = op1;
                }
                else if (op1->IsCnsFltOrDbl())
                {
                    cnsNode   = op1->AsDblCon();
                    otherNode = op2;
                }

                if (cnsNode == nullptr)
                {
                    // no constant node, nothing to do
                    break;
                }

                if (otherNode->IsCnsFltOrDbl())
                {
                    // both are constant, we can fold this operation completely. Pop both peeked values

                    if (ni == NI_System_Math_Max)
                    {
                        cnsNode->SetDconValue(
                            FloatingPointUtils::maximum(cnsNode->DconValue(), otherNode->AsDblCon()->DconValue()));
                    }
                    else
                    {
                        assert(ni == NI_System_Math_Min);
                        cnsNode->SetDconValue(
                            FloatingPointUtils::minimum(cnsNode->DconValue(), otherNode->AsDblCon()->DconValue()));
                    }

                    retNode = cnsNode;

                    impPopStack();
                    impPopStack();
                    DEBUG_DESTROY_NODE(otherNode);

                    break;
                }

                // only one is constant, we can fold in specialized scenarios

                if (cnsNode->IsFloatNaN())
                {
                    impSpillSideEffects(false, CHECK_SPILL_ALL DEBUGARG("spill side effects before propagating NaN"));

                    // maxsd, maxss, minsd, and minss all return op2 if either is NaN
                    // we require NaN to be propagated so ensure the known NaN is op2

                    impPopStack();
                    impPopStack();
                    DEBUG_DESTROY_NODE(otherNode);

                    retNode = cnsNode;
                    break;
                }

                if (!compOpportunisticallyDependsOn(InstructionSet_SSE2))
                {
                    break;
                }

                if (ni == NI_System_Math_Max)
                {
                    // maxsd, maxss return op2 if both inputs are 0 of either sign
                    // we require +0 to be greater than -0, so we can't handle if
                    // the known constant is +0. This is because if the unknown value
                    // is -0, we'd need the cns to be op2. But if the unknown value
                    // is NaN, we'd need the cns to be op1 instead.

                    if (cnsNode->IsFloatPositiveZero())
                    {
                        break;
                    }

                    // Given the checks, op1 can safely be the cns and op2 the other node

                    ni = (callType == TYP_DOUBLE) ? NI_SSE2_Max : NI_SSE_Max;

                    // one is constant and we know its something we can handle, so pop both peeked values

                    op1 = cnsNode;
                    op2 = otherNode;
                }
                else
                {
                    assert(ni == NI_System_Math_Min);

                    // minsd, minss return op2 if both inputs are 0 of either sign
                    // we require -0 to be lesser than +0, so we can't handle if
                    // the known constant is -0. This is because if the unknown value
                    // is +0, we'd need the cns to be op2. But if the unknown value
                    // is NaN, we'd need the cns to be op1 instead.

                    if (cnsNode->IsFloatNegativeZero())
                    {
                        break;
                    }

                    // Given the checks, op1 can safely be the cns and op2 the other node

                    ni = (callType == TYP_DOUBLE) ? NI_SSE2_Min : NI_SSE_Min;

                    // one is constant and we know its something we can handle, so pop both peeked values

                    op1 = cnsNode;
                    op2 = otherNode;
                }

                assert(op1->IsCnsFltOrDbl() && !op2->IsCnsFltOrDbl());

                impPopStack();
                impPopStack();

                GenTreeVecCon* vecCon = gtNewVconNode(TYP_SIMD16);

                if (callJitType == CORINFO_TYPE_FLOAT)
                {
                    vecCon->gtSimd16Val.f32[0] = (float)op1->AsDblCon()->DconValue();
                }
                else
                {
                    vecCon->gtSimd16Val.f64[0] = op1->AsDblCon()->DconValue();
                }

                op1 = vecCon;
                op2 = gtNewSimdHWIntrinsicNode(TYP_SIMD16, op2, NI_Vector128_CreateScalarUnsafe, callJitType, 16);

                retNode = gtNewSimdHWIntrinsicNode(TYP_SIMD16, op1, op2, ni, callJitType, 16);
                retNode = gtNewSimdHWIntrinsicNode(callType, retNode, NI_Vector128_ToScalar, callJitType, 16);

                break;
            }
#endif

            case NI_System_Math_Pow:
            case NI_System_Math_Round:
            case NI_System_Math_Sin:
            case NI_System_Math_Sinh:
            case NI_System_Math_Sqrt:
            case NI_System_Math_Tan:
            case NI_System_Math_Tanh:
            case NI_System_Math_Truncate:
            {
                retNode = impMathIntrinsic(method, sig, callType, ni, tailCall);
                break;
            }

            case NI_System_Array_Clone:
            case NI_System_Collections_Generic_Comparer_get_Default:
            case NI_System_Collections_Generic_EqualityComparer_get_Default:
            case NI_System_Object_MemberwiseClone:
            case NI_System_Threading_Thread_get_CurrentThread:
            {
                // Flag for later handling.
                isSpecial = true;
                break;
            }

            case NI_System_Object_GetType:
            {
                JITDUMP("\n impIntrinsic: call to Object.GetType\n");
                GenTree* op1 = impStackTop(0).val;

                // If we're calling GetType on a boxed value, just get the type directly.
                if (op1->IsBoxedValue())
                {
                    JITDUMP("Attempting to optimize box(...).getType() to direct type construction\n");

                    // Try and clean up the box. Obtain the handle we
                    // were going to pass to the newobj.
                    GenTree* boxTypeHandle = gtTryRemoveBoxUpstreamEffects(op1, BR_REMOVE_AND_NARROW_WANT_TYPE_HANDLE);

                    if (boxTypeHandle != nullptr)
                    {
                        // Note we don't need to play the TYP_STRUCT games here like
                        // do for LDTOKEN since the return value of this operator is Type,
                        // not RuntimeTypeHandle.
                        impPopStack();
                        GenTree* runtimeType =
                            gtNewHelperCallNode(CORINFO_HELP_TYPEHANDLE_TO_RUNTIMETYPE, TYP_REF, boxTypeHandle);
                        retNode = runtimeType;
                    }
                }

                // If we have a constrained callvirt with a "box this" transform
                // we know we have a value class and hence an exact type.
                //
                // If so, instead of boxing and then extracting the type, just
                // construct the type directly.
                if ((retNode == nullptr) && (pConstrainedResolvedToken != nullptr) &&
                    (constraintCallThisTransform == CORINFO_BOX_THIS))
                {
                    // Ensure this is one of the simple box cases (in particular, rule out nullables).
                    const CorInfoHelpFunc boxHelper = info.compCompHnd->getBoxHelper(pConstrainedResolvedToken->hClass);
                    const bool            isSafeToOptimize = (boxHelper == CORINFO_HELP_BOX);

                    if (isSafeToOptimize)
                    {
                        JITDUMP("Optimizing constrained box-this obj.getType() to direct type construction\n");
                        impPopStack();
                        GenTree* typeHandleOp =
                            impTokenToHandle(pConstrainedResolvedToken, nullptr, true /* mustRestoreHandle */);
                        if (typeHandleOp == nullptr)
                        {
                            assert(compDonotInline());
                            return nullptr;
                        }
                        GenTree* runtimeType =
                            gtNewHelperCallNode(CORINFO_HELP_TYPEHANDLE_TO_RUNTIMETYPE, TYP_REF, typeHandleOp);
                        retNode = runtimeType;
                    }
                }

#ifdef DEBUG
                if (retNode != nullptr)
                {
                    JITDUMP("Optimized result for call to GetType is\n");
                    if (verbose)
                    {
                        gtDispTree(retNode);
                    }
                }
#endif

                // Else expand as an intrinsic, unless the call is constrained,
                // in which case we defer expansion to allow impImportCall do the
                // special constraint processing.
                if ((retNode == nullptr) && (pConstrainedResolvedToken == nullptr))
                {
                    JITDUMP("Expanding as special intrinsic\n");
                    impPopStack();
                    op1 = new (this, GT_INTRINSIC) GenTreeIntrinsic(genActualType(callType), op1, ni, method);

                    // Set the CALL flag to indicate that the operator is implemented by a call.
                    // Set also the EXCEPTION flag because the native implementation of
                    // NI_System_Object_GetType intrinsic can throw NullReferenceException.
                    op1->gtFlags |= (GTF_CALL | GTF_EXCEPT);
                    retNode = op1;
                    // Might be further optimizable, so arrange to leave a mark behind
                    isSpecial = true;
                }

                if (retNode == nullptr)
                {
                    JITDUMP("Leaving as normal call\n");
                    // Might be further optimizable, so arrange to leave a mark behind
                    isSpecial = true;
                }

                break;
            }

            case NI_System_Array_GetLength:
            case NI_System_Array_GetLowerBound:
            case NI_System_Array_GetUpperBound:
            {
                // System.Array.GetLength(Int32) method:
                //     public int GetLength(int dimension)
                // System.Array.GetLowerBound(Int32) method:
                //     public int GetLowerBound(int dimension)
                // System.Array.GetUpperBound(Int32) method:
                //     public int GetUpperBound(int dimension)
                //
                // Only implement these as intrinsics for multi-dimensional arrays.
                // Only handle constant dimension arguments.

                GenTree* gtDim = impStackTop().val;
                GenTree* gtArr = impStackTop(1).val;

                if (gtDim->IsIntegralConst())
                {
                    bool                 isExact   = false;
                    bool                 isNonNull = false;
                    CORINFO_CLASS_HANDLE arrCls    = gtGetClassHandle(gtArr, &isExact, &isNonNull);
                    if (arrCls != NO_CLASS_HANDLE)
                    {
                        unsigned rank = info.compCompHnd->getArrayRank(arrCls);
                        if ((rank > 1) && !info.compCompHnd->isSDArray(arrCls))
                        {
                            // `rank` is guaranteed to be <=32 (see MAX_RANK in vm\array.h). Any constant argument
                            // is `int` sized.
                            INT64 dimValue = gtDim->AsIntConCommon()->IntegralValue();
                            assert((unsigned int)dimValue == dimValue);
                            unsigned dim = (unsigned int)dimValue;
                            if (dim < rank)
                            {
                                // This is now known to be a multi-dimension array with a constant dimension
                                // that is in range; we can expand it as an intrinsic.

                                impPopStack().val; // Pop the dim and array object; we already have a pointer to them.
                                impPopStack().val;

                                // Make sure there are no global effects in the array (such as it being a function
                                // call), so we can mark the generated indirection with GTF_IND_INVARIANT. In the
                                // GetUpperBound case we need the cloned object, since we refer to the array
                                // object twice. In the other cases, we don't need to clone.
                                GenTree* gtArrClone = nullptr;
                                if (((gtArr->gtFlags & GTF_GLOB_EFFECT) != 0) || (ni == NI_System_Array_GetUpperBound))
                                {
                                    gtArr = impCloneExpr(gtArr, &gtArrClone, NO_CLASS_HANDLE, CHECK_SPILL_ALL,
                                                         nullptr DEBUGARG("MD intrinsics array"));
                                }

                                switch (ni)
                                {
                                    case NI_System_Array_GetLength:
                                    {
                                        // Generate *(array + offset-to-length-array + sizeof(int) * dim)
                                        unsigned offs   = eeGetMDArrayLengthOffset(rank, dim);
                                        GenTree* gtOffs = gtNewIconNode(offs, TYP_I_IMPL);
                                        GenTree* gtAddr = gtNewOperNode(GT_ADD, TYP_BYREF, gtArr, gtOffs);
                                        retNode         = gtNewIndir(TYP_INT, gtAddr);
                                        retNode->gtFlags |= GTF_IND_INVARIANT;
                                        break;
                                    }
                                    case NI_System_Array_GetLowerBound:
                                    {
                                        // Generate *(array + offset-to-bounds-array + sizeof(int) * dim)
                                        unsigned offs   = eeGetMDArrayLowerBoundOffset(rank, dim);
                                        GenTree* gtOffs = gtNewIconNode(offs, TYP_I_IMPL);
                                        GenTree* gtAddr = gtNewOperNode(GT_ADD, TYP_BYREF, gtArr, gtOffs);
                                        retNode         = gtNewIndir(TYP_INT, gtAddr);
                                        retNode->gtFlags |= GTF_IND_INVARIANT;
                                        break;
                                    }
                                    case NI_System_Array_GetUpperBound:
                                    {
                                        assert(gtArrClone != nullptr);

                                        // Generate:
                                        //    *(array + offset-to-length-array + sizeof(int) * dim) +
                                        //    *(array + offset-to-bounds-array + sizeof(int) * dim) - 1
                                        unsigned offs         = eeGetMDArrayLowerBoundOffset(rank, dim);
                                        GenTree* gtOffs       = gtNewIconNode(offs, TYP_I_IMPL);
                                        GenTree* gtAddr       = gtNewOperNode(GT_ADD, TYP_BYREF, gtArr, gtOffs);
                                        GenTree* gtLowerBound = gtNewIndir(TYP_INT, gtAddr);
                                        gtLowerBound->gtFlags |= GTF_IND_INVARIANT;

                                        offs              = eeGetMDArrayLengthOffset(rank, dim);
                                        gtOffs            = gtNewIconNode(offs, TYP_I_IMPL);
                                        gtAddr            = gtNewOperNode(GT_ADD, TYP_BYREF, gtArrClone, gtOffs);
                                        GenTree* gtLength = gtNewIndir(TYP_INT, gtAddr);
                                        gtLength->gtFlags |= GTF_IND_INVARIANT;

                                        GenTree* gtSum = gtNewOperNode(GT_ADD, TYP_INT, gtLowerBound, gtLength);
                                        GenTree* gtOne = gtNewIconNode(1, TYP_INT);
                                        retNode        = gtNewOperNode(GT_SUB, TYP_INT, gtSum, gtOne);
                                        break;
                                    }
                                    default:
                                        unreached();
                                }
                            }
                        }
                    }
                }
                break;
            }

            case NI_System_Buffers_Binary_BinaryPrimitives_ReverseEndianness:
            {
                assert(sig->numArgs == 1);

                // We expect the return type of the ReverseEndianness routine to match the type of the
                // one and only argument to the method. We use a special instruction for 16-bit
                // BSWAPs since on x86 processors this is implemented as ROR <16-bit reg>, 8. Additionally,
                // we only emit 64-bit BSWAP instructions on 64-bit archs; if we're asked to perform a
                // 64-bit byte swap on a 32-bit arch, we'll fall to the default case in the switch block below.

                switch (sig->retType)
                {
                    case CorInfoType::CORINFO_TYPE_SHORT:
                    case CorInfoType::CORINFO_TYPE_USHORT:
                        retNode = gtNewCastNode(TYP_INT, gtNewOperNode(GT_BSWAP16, TYP_INT, impPopStack().val), false,
                                                callType);
                        break;

                    case CorInfoType::CORINFO_TYPE_INT:
                    case CorInfoType::CORINFO_TYPE_UINT:
#ifdef TARGET_64BIT
                    case CorInfoType::CORINFO_TYPE_LONG:
                    case CorInfoType::CORINFO_TYPE_ULONG:
#endif // TARGET_64BIT
                        retNode = gtNewOperNode(GT_BSWAP, callType, impPopStack().val);
                        break;

                    default:
                        // This default case gets hit on 32-bit archs when a call to a 64-bit overload
                        // of ReverseEndianness is encountered. In that case we'll let JIT treat this as a standard
                        // method call, where the implementation decomposes the operation into two 32-bit
                        // bswap routines. If the input to the 64-bit function is a constant, then we rely
                        // on inlining + constant folding of 32-bit bswaps to effectively constant fold
                        // the 64-bit call site.
                        break;
                }

                break;
            }

            // Fold PopCount for constant input
            case NI_System_Numerics_BitOperations_PopCount:
            {
                assert(sig->numArgs == 1);
                if (impStackTop().val->IsIntegralConst())
                {
                    typeInfo argType = verParseArgSigToTypeInfo(sig, sig->args).NormaliseForStack();
                    INT64    cns     = impPopStack().val->AsIntConCommon()->IntegralValue();
                    if (argType.IsType(TI_LONG))
                    {
                        retNode = gtNewIconNode(genCountBits(cns), callType);
                    }
                    else
                    {
                        assert(argType.IsType(TI_INT));
                        retNode = gtNewIconNode(genCountBits(static_cast<unsigned>(cns)), callType);
                    }
                }
                break;
            }

            case NI_System_GC_KeepAlive:
            {
                retNode = impKeepAliveIntrinsic(impPopStack().val);
                break;
            }

            case NI_System_BitConverter_DoubleToInt64Bits:
            {
                GenTree* op1 = impStackTop().val;
                assert(varTypeIsFloating(op1));

                if (op1->IsCnsFltOrDbl())
                {
                    impPopStack();

                    double f64Cns = op1->AsDblCon()->DconValue();
                    retNode       = gtNewLconNode(*reinterpret_cast<int64_t*>(&f64Cns));
                }
#if TARGET_64BIT
                else
                {
                    // TODO-Cleanup: We should support this on 32-bit but it requires decomposition work
                    impPopStack();

                    if (op1->TypeGet() != TYP_DOUBLE)
                    {
                        op1 = gtNewCastNode(TYP_DOUBLE, op1, false, TYP_DOUBLE);
                    }
                    retNode = gtNewBitCastNode(TYP_LONG, op1);
                }
#endif
                break;
            }

            case NI_System_BitConverter_Int32BitsToSingle:
            {
                GenTree* op1 = impPopStack().val;
                assert(varTypeIsInt(op1));

                if (op1->IsIntegralConst())
                {
                    int32_t i32Cns = (int32_t)op1->AsIntConCommon()->IconValue();
                    retNode        = gtNewDconNode(*reinterpret_cast<float*>(&i32Cns), TYP_FLOAT);
                }
                else
                {
                    retNode = gtNewBitCastNode(TYP_FLOAT, op1);
                }
                break;
            }

            case NI_System_BitConverter_Int64BitsToDouble:
            {
                GenTree* op1 = impStackTop().val;
                assert(varTypeIsLong(op1));

                if (op1->IsIntegralConst())
                {
                    impPopStack();

                    int64_t i64Cns = op1->AsIntConCommon()->LngValue();
                    retNode        = gtNewDconNode(*reinterpret_cast<double*>(&i64Cns));
                }
#if TARGET_64BIT
                else
                {
                    // TODO-Cleanup: We should support this on 32-bit but it requires decomposition work
                    impPopStack();

                    retNode = gtNewBitCastNode(TYP_DOUBLE, op1);
                }
#endif
                break;
            }

            case NI_System_BitConverter_SingleToInt32Bits:
            {
                GenTree* op1 = impPopStack().val;
                assert(varTypeIsFloating(op1));

                if (op1->IsCnsFltOrDbl())
                {
                    float f32Cns = (float)op1->AsDblCon()->DconValue();
                    retNode      = gtNewIconNode(*reinterpret_cast<int32_t*>(&f32Cns));
                }
                else
                {
                    if (op1->TypeGet() != TYP_FLOAT)
                    {
                        op1 = gtNewCastNode(TYP_FLOAT, op1, false, TYP_FLOAT);
                    }
                    retNode = gtNewBitCastNode(TYP_INT, op1);
                }
                break;
            }

            default:
                break;
        }
    }

    if (mustExpand && (retNode == nullptr))
    {
        assert(!"Unhandled must expand intrinsic, throwing PlatformNotSupportedException");
        return impUnsupportedNamedIntrinsic(CORINFO_HELP_THROW_PLATFORM_NOT_SUPPORTED, method, sig, mustExpand);
    }

    // Optionally report if this intrinsic is special
    // (that is, potentially re-optimizable during morph).
    if (isSpecialIntrinsic != nullptr)
    {
        *isSpecialIntrinsic = isSpecial;
    }

    return retNode;
}

GenTree* Compiler::impSRCSUnsafeIntrinsic(NamedIntrinsic        intrinsic,
                                          CORINFO_CLASS_HANDLE  clsHnd,
                                          CORINFO_METHOD_HANDLE method,
                                          CORINFO_SIG_INFO*     sig)
{
    // NextCallRetAddr requires a CALL, so return nullptr.
    if (info.compHasNextCallRetAddr)
    {
        return nullptr;
    }

    assert(sig->sigInst.classInstCount == 0);

    switch (intrinsic)
    {
        case NI_SRCS_UNSAFE_Add:
        {
            assert(sig->sigInst.methInstCount == 1);

            // ldarg.0
            // ldarg.1
            // sizeof !!T
            // conv.i
            // mul
            // add
            // ret

            GenTree* op2 = impPopStack().val;
            GenTree* op1 = impPopStack().val;
            impBashVarAddrsToI(op1, op2);

            op2 = impImplicitIorI4Cast(op2, TYP_I_IMPL);

            unsigned classSize = info.compCompHnd->getClassSize(sig->sigInst.methInst[0]);

            if (classSize != 1)
            {
                GenTree* size = gtNewIconNode(classSize, TYP_I_IMPL);
                op2           = gtNewOperNode(GT_MUL, TYP_I_IMPL, op2, size);
            }

            var_types type = impGetByRefResultType(GT_ADD, /* uns */ false, &op1, &op2);
            return gtNewOperNode(GT_ADD, type, op1, op2);
        }

        case NI_SRCS_UNSAFE_AddByteOffset:
        {
            assert(sig->sigInst.methInstCount == 1);

            // ldarg.0
            // ldarg.1
            // add
            // ret

            GenTree* op2 = impPopStack().val;
            GenTree* op1 = impPopStack().val;
            impBashVarAddrsToI(op1, op2);

            var_types type = impGetByRefResultType(GT_ADD, /* uns */ false, &op1, &op2);
            return gtNewOperNode(GT_ADD, type, op1, op2);
        }

        case NI_SRCS_UNSAFE_AreSame:
        {
            assert(sig->sigInst.methInstCount == 1);

            // ldarg.0
            // ldarg.1
            // ceq
            // ret

            GenTree* op2 = impPopStack().val;
            GenTree* op1 = impPopStack().val;

            GenTree* tmp = gtNewOperNode(GT_EQ, TYP_INT, op1, op2);
            return gtFoldExpr(tmp);
        }

        case NI_SRCS_UNSAFE_As:
        {
            assert((sig->sigInst.methInstCount == 1) || (sig->sigInst.methInstCount == 2));

            // ldarg.0
            // ret

            return impPopStack().val;
        }

        case NI_SRCS_UNSAFE_AsPointer:
        {
            assert(sig->sigInst.methInstCount == 1);

            // ldarg.0
            // conv.u
            // ret

            GenTree* op1 = impPopStack().val;
            impBashVarAddrsToI(op1);

            return gtNewCastNode(TYP_I_IMPL, op1, /* uns */ false, TYP_I_IMPL);
        }

        case NI_SRCS_UNSAFE_AsRef:
        {
            assert(sig->sigInst.methInstCount == 1);

            // ldarg.0
            // ret

            return impPopStack().val;
        }

        case NI_SRCS_UNSAFE_ByteOffset:
        {
            assert(sig->sigInst.methInstCount == 1);

            // ldarg.1
            // ldarg.0
            // sub
            // ret

            impSpillSideEffect(true, verCurrentState.esStackDepth -
                                         2 DEBUGARG("Spilling op1 side effects for Unsafe.ByteOffset"));

            GenTree* op2 = impPopStack().val;
            GenTree* op1 = impPopStack().val;
            impBashVarAddrsToI(op1, op2);

            var_types type = impGetByRefResultType(GT_SUB, /* uns */ false, &op2, &op1);
            return gtNewOperNode(GT_SUB, type, op2, op1);
        }

        case NI_SRCS_UNSAFE_Copy:
        {
            assert(sig->sigInst.methInstCount == 1);

            // ldarg.0
            // ldarg.1
            // ldobj !!T
            // stobj !!T
            // ret

            return nullptr;
        }

        case NI_SRCS_UNSAFE_CopyBlock:
        {
            assert(sig->sigInst.methInstCount == 0);

            // ldarg.0
            // ldarg.1
            // ldarg.2
            // cpblk
            // ret

            return nullptr;
        }

        case NI_SRCS_UNSAFE_CopyBlockUnaligned:
        {
            assert(sig->sigInst.methInstCount == 0);

            // ldarg.0
            // ldarg.1
            // ldarg.2
            // unaligned. 0x1
            // cpblk
            // ret

            return nullptr;
        }

        case NI_SRCS_UNSAFE_InitBlock:
        {
            assert(sig->sigInst.methInstCount == 0);

            // ldarg.0
            // ldarg.1
            // ldarg.2
            // initblk
            // ret

            return nullptr;
        }

        case NI_SRCS_UNSAFE_InitBlockUnaligned:
        {
            assert(sig->sigInst.methInstCount == 0);

            // ldarg.0
            // ldarg.1
            // ldarg.2
            // unaligned. 0x1
            // initblk
            // ret

            return nullptr;
        }

        case NI_SRCS_UNSAFE_IsAddressGreaterThan:
        {
            assert(sig->sigInst.methInstCount == 1);

            // ldarg.0
            // ldarg.1
            // cgt.un
            // ret

            GenTree* op2 = impPopStack().val;
            GenTree* op1 = impPopStack().val;

            GenTree* tmp = gtNewOperNode(GT_GT, TYP_INT, op1, op2);
            tmp->gtFlags |= GTF_UNSIGNED;
            return gtFoldExpr(tmp);
        }

        case NI_SRCS_UNSAFE_IsAddressLessThan:
        {
            assert(sig->sigInst.methInstCount == 1);

            // ldarg.0
            // ldarg.1
            // clt.un
            // ret

            GenTree* op2 = impPopStack().val;
            GenTree* op1 = impPopStack().val;

            GenTree* tmp = gtNewOperNode(GT_LT, TYP_INT, op1, op2);
            tmp->gtFlags |= GTF_UNSIGNED;
            return gtFoldExpr(tmp);
        }

        case NI_SRCS_UNSAFE_IsNullRef:
        {
            assert(sig->sigInst.methInstCount == 1);

            // ldarg.0
            // ldc.i4.0
            // conv.u
            // ceq
            // ret

            GenTree* op1 = impPopStack().val;
            GenTree* cns = gtNewIconNode(0, TYP_BYREF);
            GenTree* tmp = gtNewOperNode(GT_EQ, TYP_INT, op1, cns);
            return gtFoldExpr(tmp);
        }

        case NI_SRCS_UNSAFE_NullRef:
        {
            assert(sig->sigInst.methInstCount == 1);

            // ldc.i4.0
            // conv.u
            // ret

            return gtNewIconNode(0, TYP_BYREF);
        }

        case NI_SRCS_UNSAFE_Read:
        {
            assert(sig->sigInst.methInstCount == 1);

            // ldarg.0
            // ldobj !!T
            // ret

            return nullptr;
        }

        case NI_SRCS_UNSAFE_ReadUnaligned:
        {
            assert(sig->sigInst.methInstCount == 1);

            // ldarg.0
            // unaligned. 0x1
            // ldobj !!T
            // ret

            return nullptr;
        }

        case NI_SRCS_UNSAFE_SizeOf:
        {
            assert(sig->sigInst.methInstCount == 1);

            // sizeof !!T
            // ret

            unsigned classSize = info.compCompHnd->getClassSize(sig->sigInst.methInst[0]);
            return gtNewIconNode(classSize, TYP_INT);
        }

        case NI_SRCS_UNSAFE_SkipInit:
        {
            assert(sig->sigInst.methInstCount == 1);

            // ret

            GenTree* op1 = impPopStack().val;

            if ((op1->gtFlags & GTF_SIDE_EFFECT) != 0)
            {
                return gtUnusedValNode(op1);
            }
            else
            {
                return gtNewNothingNode();
            }
        }

        case NI_SRCS_UNSAFE_Subtract:
        {
            assert(sig->sigInst.methInstCount == 1);

            // ldarg.0
            // ldarg.1
            // sizeof !!T
            // conv.i
            // mul
            // sub
            // ret

            GenTree* op2 = impPopStack().val;
            GenTree* op1 = impPopStack().val;
            impBashVarAddrsToI(op1, op2);

            op2 = impImplicitIorI4Cast(op2, TYP_I_IMPL);

            unsigned classSize = info.compCompHnd->getClassSize(sig->sigInst.methInst[0]);

            if (classSize != 1)
            {
                GenTree* size = gtNewIconNode(classSize, TYP_I_IMPL);
                op2           = gtNewOperNode(GT_MUL, TYP_I_IMPL, op2, size);
            }

            var_types type = impGetByRefResultType(GT_SUB, /* uns */ false, &op1, &op2);
            return gtNewOperNode(GT_SUB, type, op1, op2);
        }

        case NI_SRCS_UNSAFE_SubtractByteOffset:
        {
            assert(sig->sigInst.methInstCount == 1);

            // ldarg.0
            // ldarg.1
            // sub
            // ret

            GenTree* op2 = impPopStack().val;
            GenTree* op1 = impPopStack().val;
            impBashVarAddrsToI(op1, op2);

            var_types type = impGetByRefResultType(GT_SUB, /* uns */ false, &op1, &op2);
            return gtNewOperNode(GT_SUB, type, op1, op2);
        }

        case NI_SRCS_UNSAFE_Unbox:
        {
            assert(sig->sigInst.methInstCount == 1);

            // ldarg.0
            // unbox !!T
            // ret

            return nullptr;
        }

        case NI_SRCS_UNSAFE_Write:
        {
            assert(sig->sigInst.methInstCount == 1);

            // ldarg.0
            // ldarg.1
            // stobj !!T
            // ret

            return nullptr;
        }

        case NI_SRCS_UNSAFE_WriteUnaligned:
        {
            assert(sig->sigInst.methInstCount == 1);

            // ldarg.0
            // ldarg.1
            // unaligned. 0x01
            // stobj !!T
            // ret

            return nullptr;
        }

        default:
        {
            unreached();
        }
    }
}
