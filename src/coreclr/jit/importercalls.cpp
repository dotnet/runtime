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
