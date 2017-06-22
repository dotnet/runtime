// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// ===========================================================================
// File: Prestub.cpp
//

// ===========================================================================
// This file contains the implementation for creating and using prestubs
// ===========================================================================
//

 
#include "common.h"
#include "vars.hpp"
#include "security.h"
#include "eeconfig.h"
#include "dllimport.h"
#include "comdelegate.h"
#include "dbginterface.h"
#include "listlock.inl"
#include "stubgen.h"
#include "eventtrace.h"
#include "array.h"
#include "compile.h"
#include "ecall.h"
#include "virtualcallstub.h"

#ifdef FEATURE_PREJIT
#include "compile.h"
#endif

#ifdef FEATURE_INTERPRETER
#include "interpreter.h"
#endif

#ifdef FEATURE_COMINTEROP 
#include "clrtocomcall.h"
#endif

#include "mdaassistants.h"

#ifdef FEATURE_STACK_SAMPLING
#include "stacksampler.h"
#endif

#ifdef FEATURE_PERFMAP
#include "perfmap.h"
#endif

#ifdef FEATURE_TIERED_COMPILATION
#include "callcounter.h"
#endif

#ifndef DACCESS_COMPILE 

EXTERN_C void STDCALL ThePreStub();

#if defined(HAS_COMPACT_ENTRYPOINTS) && defined (_TARGET_ARM_)

EXTERN_C void STDCALL ThePreStubCompactARM();

#endif // defined(HAS_COMPACT_ENTRYPOINTS) && defined (_TARGET_ARM_)

EXTERN_C void STDCALL ThePreStubPatch();

//==========================================================================

PCODE MethodDesc::DoBackpatch(MethodTable * pMT, MethodTable *pDispatchingMT, BOOL fFullBackPatch)
{
    CONTRACTL
    {
        STANDARD_VM_CHECK;
        PRECONDITION(!ContainsGenericVariables());
#ifndef FEATURE_INTERPRETER
        PRECONDITION(HasStableEntryPoint());
#endif // FEATURE_INTERPRETER
        PRECONDITION(pMT == GetMethodTable());
    }
    CONTRACTL_END;
#ifdef FEATURE_INTERPRETER
    PCODE pTarget = GetMethodEntryPoint();
#else
    PCODE pTarget = GetStableEntryPoint();
#endif

    if (!HasTemporaryEntryPoint())
        return pTarget;

    PCODE pExpected = GetTemporaryEntryPoint();

    if (pExpected == pTarget)
        return pTarget;

    // True interface methods are never backpatched
    if (pMT->IsInterface() && !IsStatic())
        return pTarget;

    if (fFullBackPatch)
    {
        FuncPtrStubs * pFuncPtrStubs = GetLoaderAllocator()->GetFuncPtrStubsNoCreate();
        if (pFuncPtrStubs != NULL)
        {
            Precode* pFuncPtrPrecode = pFuncPtrStubs->Lookup(this);
            if (pFuncPtrPrecode != NULL)
            {
                // If there is a funcptr precode to patch, we are done for this round.
                if (pFuncPtrPrecode->SetTargetInterlocked(pTarget))
                    return pTarget;
            }
        }

#ifndef HAS_COMPACT_ENTRYPOINTS
        // Patch the fake entrypoint if necessary
        Precode::GetPrecodeFromEntryPoint(pExpected)->SetTargetInterlocked(pTarget);
#endif // HAS_COMPACT_ENTRYPOINTS
    }

    if (HasNonVtableSlot())
        return pTarget;

    BOOL fBackpatched = FALSE;

#define BACKPATCH(pPatchedMT)                           \
    do                                                  \
    {                                                   \
        if (pPatchedMT->GetSlot(dwSlot) == pExpected)   \
        {                                               \
            pPatchedMT->SetSlot(dwSlot, pTarget);       \
            fBackpatched = TRUE;                        \
        }                                               \
    }                                                   \
    while(0)

    // The owning slot has been updated already, so there is no need to backpatch it
    _ASSERTE(pMT->GetSlot(GetSlot()) == pTarget);

    if (pDispatchingMT != NULL && pDispatchingMT != pMT)
    {
        DWORD dwSlot = GetSlot();

        BACKPATCH(pDispatchingMT);

        if (fFullBackPatch)
        {
            //
            // Backpatch the MethodTable that code:MethodTable::GetRestoredSlot() reads the value from. 
            // VSD reads the slot value using code:MethodTable::GetRestoredSlot(), and so we need to make sure 
            // that it returns the stable entrypoint eventually to avoid going through the slow path all the time.
            //
            MethodTable * pRestoredSlotMT = pDispatchingMT->GetRestoredSlotMT(dwSlot);

            BACKPATCH(pRestoredSlotMT);
        }
    }

    if (IsMethodImpl())
    {
        MethodImpl::Iterator it(this);
        while (it.IsValid())
        {
            DWORD dwSlot = it.GetSlot();

            BACKPATCH(pMT);

            if (pDispatchingMT != NULL)
            {
                BACKPATCH(pDispatchingMT);
            }

            it.Next();
        }
    }

    if (fFullBackPatch && !fBackpatched && IsDuplicate())
    {
        // If this is a duplicate, let's scan the rest of the VTable hunting for other hits.
        unsigned numSlots = pMT->GetNumVirtuals();
        for (DWORD dwSlot=0; dwSlot<numSlots; dwSlot++)
        {
            BACKPATCH(pMT);

            if (pDispatchingMT != NULL)
            {
                BACKPATCH(pDispatchingMT);
            }
        }
    }

#undef BACKPATCH

    return pTarget;
}

// <TODO> FIX IN BETA 2
//
// g_pNotificationTable is only modified by the DAC and therefore the
// optmizer can assume that it will always be its default value and has
// been seen to (on IA64 free builds) eliminate the code in DACNotifyCompilationFinished
// such that DAC notifications are no longer sent.
//
// TODO: fix this in Beta 2
// the RIGHT fix is to make g_pNotificationTable volatile, but currently
// we don't have DAC macros to do that. Additionally, there are a number
// of other places we should look at DAC definitions to determine if they
// should be also declared volatile.
//
// for now we just turn off optimization for these guys
#ifdef _MSC_VER 
#pragma optimize("", off)
#endif

void DACNotifyCompilationFinished(MethodDesc *methodDesc)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        SO_INTOLERANT;
        MODE_PREEMPTIVE;
    }
    CONTRACTL_END;

    // Is the list active?
    JITNotifications jn(g_pNotificationTable);
    if (jn.IsActive())
    {
        // Get Module and mdToken
        mdToken t = methodDesc->GetMemberDef();
        Module *modulePtr = methodDesc->GetModule();

        _ASSERTE(modulePtr);

#ifndef FEATURE_GDBJIT
        // Are we listed?
        USHORT jnt = jn.Requested((TADDR) modulePtr, t);
        if (jnt & CLRDATA_METHNOTIFY_GENERATED)
        {
            // If so, throw an exception!
#endif
            DACNotify::DoJITNotification(methodDesc);
#ifndef FEATURE_GDBJIT
        }
#endif
    }
}

#ifdef _MSC_VER 
#pragma optimize("", on)
#endif
// </TODO>


// ********************************************************************
//                  README!!
// ********************************************************************

// MakeJitWorker is the thread safe way to invoke the JIT compiler
// If multiple threads get in here for the same pMD, ALL of them
// MUST return the SAME value for pstub.
//
// This function creates a DeadlockAware list of methods being jitted
// which prevents us from trying to JIT the same method more that once.


PCODE MethodDesc::MakeJitWorker(COR_ILMETHOD_DECODER* ILHeader, CORJIT_FLAGS flags)
{
    STANDARD_VM_CONTRACT;

    BOOL fIsILStub = IsILStub();        // @TODO: understand the need for this special case

    LOG((LF_JIT, LL_INFO1000000,
         "MakeJitWorker(" FMT_ADDR ", %s) for %s:%s\n",
         DBG_ADDR(this),
         fIsILStub               ? " TRUE" : "FALSE",
         GetMethodTable()->GetDebugClassName(),
         m_pszDebugMethodName));

    PCODE pCode = NULL;
    ULONG sizeOfCode = 0;
#if defined(FEATURE_INTERPRETER) || defined(FEATURE_TIERED_COMPILATION)
    BOOL fStable = TRUE;  // True iff the new code address (to be stored in pCode), is a stable entry point.
#endif
#ifdef FEATURE_INTERPRETER
    PCODE pPreviousInterpStub = NULL;
    BOOL fInterpreted = FALSE;
#endif

#ifdef FEATURE_MULTICOREJIT
    MulticoreJitManager & mcJitManager = GetAppDomain()->GetMulticoreJitManager();

    bool fBackgroundThread = flags.IsSet(CORJIT_FLAGS::CORJIT_FLAG_MCJIT_BACKGROUND);
#endif

    // If this is the first stage of a tiered compilation progression, use tier0, otherwise
    // use default compilation options
#ifdef FEATURE_TIERED_COMPILATION
    if (!IsEligibleForTieredCompilation())
    {
        fStable = TRUE;
    }
    else
    {
        fStable = FALSE;
        flags.Add(CORJIT_FLAGS(CORJIT_FLAGS::CORJIT_FLAG_TIER0));
    }
#endif

    {
        // Enter the global lock which protects the list of all functions being JITd
        ListLockHolder pJitLock (GetDomain()->GetJitLock());

        // It is possible that another thread stepped in before we entered the global lock for the first time.
        pCode = GetNativeCode();
        if (pCode != NULL)
        {
#ifdef FEATURE_INTERPRETER
            if (Interpreter::InterpretationStubToMethodInfo(pCode) == this)
            {
                pPreviousInterpStub = pCode;
            }
            else
#endif // FEATURE_INTERPRETER
            goto Done;
        }

        const char *description = "jit lock";
        INDEBUG(description = m_pszDebugMethodName;)
        ListLockEntryHolder pEntry(ListLockEntry::Find(pJitLock, this, description));

        // We have an entry now, we can release the global lock
        pJitLock.Release();

        // Take the entry lock
        {
            ListLockEntryLockHolder pEntryLock(pEntry, FALSE);

            if (pEntryLock.DeadlockAwareAcquire())
            {
                if (pEntry->m_hrResultCode == S_FALSE)
                {
                    // Nobody has jitted the method yet
                }
                else
                {
                    // We came in to jit but someone beat us so return the
                    // jitted method!

                    // We can just fall through because we will notice below that
                    // the method has code.

                    // @todo: Note that we may have a failed HRESULT here -
                    // we might want to return an early error rather than
                    // repeatedly failing the jit.
                }
            }
            else
            {
                // Taking this lock would cause a deadlock (presumably because we
                // are involved in a class constructor circular dependency.)  For
                // instance, another thread may be waiting to run the class constructor
                // that we are jitting, but is currently jitting this function.
                //
                // To remedy this, we want to go ahead and do the jitting anyway.
                // The other threads contending for the lock will then notice that
                // the jit finished while they were running class constructors, and abort their
                // current jit effort.
                //
                // We don't have to do anything special right here since we
                // can check HasNativeCode() to detect this case later.
                //
                // Note that at this point we don't have the lock, but that's OK because the
                // thread which does have the lock is blocked waiting for us.
            }

            // It is possible that another thread stepped in before we entered the lock.
            pCode = GetNativeCode();
#ifdef FEATURE_INTERPRETER
            if (pCode != NULL && (pCode != pPreviousInterpStub))
#else
            if (pCode != NULL)
#endif // FEATURE_INTERPRETER
            {
                goto Done;
            }

            SString namespaceOrClassName, methodName, methodSignature;

            PCODE pOtherCode = NULL; // Need to move here due to 'goto GotNewCode'
            
#ifdef FEATURE_MULTICOREJIT

            bool fCompiledInBackground = false;

            // If not called from multi-core JIT thread, 
            if (! fBackgroundThread)
            {
                // Quick check before calling expensive out of line function on this method's domain has code JITted by background thread
                if (mcJitManager.GetMulticoreJitCodeStorage().GetRemainingMethodCount() > 0)
                {
                    if (MulticoreJitManager::IsMethodSupported(this))
                    {
                        pCode = mcJitManager.RequestMethodCode(this); // Query multi-core JIT manager for compiled code

                        // Multicore JIT manager starts background thread to pre-compile methods, but it does not back-patch it/notify profiler/notify DAC,
                        // Jumtp to GotNewCode to do so
                        if (pCode != NULL)
                        {
                            fCompiledInBackground = true;
                    
#ifdef DEBUGGING_SUPPORTED
                            // Notify the debugger of the jitted function
                            if (g_pDebugInterface != NULL)
                            {
                                g_pDebugInterface->JITComplete(this, pCode);
                            }
#endif

                            goto GotNewCode;
                        }
                    }
                }
            }
#endif

            if (fIsILStub)
            {
                // we race with other threads to JIT the code for an IL stub and the
                // IL header is released once one of the threads completes.  As a result
                // we must be inside the lock to reliably get the IL header for the
                // stub.

                ILStubResolver* pResolver = AsDynamicMethodDesc()->GetILStubResolver();
                ILHeader = pResolver->GetILHeader();
            }

#ifdef MDA_SUPPORTED 
            MdaJitCompilationStart* pProbe = MDA_GET_ASSISTANT(JitCompilationStart);
            if (pProbe)
                pProbe->NowCompiling(this);
#endif // MDA_SUPPORTED

#ifdef PROFILING_SUPPORTED 
            // If profiling, need to give a chance for a tool to examine and modify
            // the IL before it gets to the JIT.  This allows one to add probe calls for
            // things like code coverage, performance, or whatever.
            {
                BEGIN_PIN_PROFILER(CORProfilerTrackJITInfo());

#ifdef FEATURE_MULTICOREJIT
                // Multicore JIT should be disabled when CORProfilerTrackJITInfo is on
                // But there could be corner case in which profiler is attached when multicore background thread is calling MakeJitWorker
                // Disable this block when calling from multicore JIT background thread
                if (!fBackgroundThread)
#endif
                {
                    if (!IsNoMetadata())
                    {
                        g_profControlBlock.pProfInterface->JITCompilationStarted((FunctionID) this, TRUE);
                        // The profiler may have changed the code on the callback.  Need to
                        // pick up the new code.  Note that you have to be fully trusted in
                        // this mode and the code will not be verified.
                        COR_ILMETHOD *pilHeader = GetILHeader(TRUE);
                        new (ILHeader) COR_ILMETHOD_DECODER(pilHeader, GetMDImport(), NULL);
                    }
                    else
                    {
                        unsigned int ilSize, unused;
                        CorInfoOptions corOptions;
                        LPCBYTE ilHeaderPointer = this->AsDynamicMethodDesc()->GetResolver()->GetCodeInfo(&ilSize, &unused, &corOptions, &unused);

                        g_profControlBlock.pProfInterface->DynamicMethodJITCompilationStarted((FunctionID) this, TRUE, ilHeaderPointer, ilSize);
                    }
                }
                END_PIN_PROFILER();
            }
#endif // PROFILING_SUPPORTED
#ifdef FEATURE_INTERPRETER
            // We move the ETW event for start of JITting inward, after we make the decision
            // to JIT rather than interpret.
#else  // FEATURE_INTERPRETER
            // Fire an ETW event to mark the beginning of JIT'ing
            ETW::MethodLog::MethodJitting(this, &namespaceOrClassName, &methodName, &methodSignature);
#endif  // FEATURE_INTERPRETER

#ifdef FEATURE_STACK_SAMPLING
#ifdef FEATURE_MULTICOREJIT
            if (!fBackgroundThread)
#endif // FEATURE_MULTICOREJIT
            {
                StackSampler::RecordJittingInfo(this, flags);
            }
#endif // FEATURE_STACK_SAMPLING

            EX_TRY
            {
                pCode = UnsafeJitFunction(this, ILHeader, flags, &sizeOfCode);
            }
            EX_CATCH
            {
                // If the current thread threw an exception, but a competing thread
                // somehow succeeded at JITting the same function (e.g., out of memory
                // encountered on current thread but not competing thread), then go ahead
                // and swallow this current thread's exception, since we somehow managed
                // to successfully JIT the code on the other thread.
                // 
                // Note that if a deadlock cycle is broken, that does not result in an
                // exception--the thread would just pass through the lock and JIT the
                // function in competition with the other thread (with the winner of the
                // race decided later on when we do SetNativeCodeInterlocked). This
                // try/catch is purely to deal with the (unusual) case where a competing
                // thread succeeded where we aborted.
                
                pOtherCode = GetNativeCode();
                
                if (pOtherCode == NULL)
                {
                    pEntry->m_hrResultCode = E_FAIL;
                    EX_RETHROW;
                }
            }
            EX_END_CATCH(RethrowTerminalExceptions)

            if (pOtherCode != NULL)
            {
                // Somebody finished jitting recursively while we were jitting the method.
                // Just use their method & leak the one we finished. (Normally we hope
                // not to finish our JIT in this case, as we will abort early if we notice
                // a reentrant jit has occurred.  But we may not catch every place so we
                // do a definitive final check here.
                pCode = pOtherCode;
                goto Done;
            }

            _ASSERTE(pCode != NULL);

#ifdef HAVE_GCCOVER
            if (GCStress<cfg_instr_jit>::IsEnabled())
            {
                SetupGcCoverage(this, (BYTE*) pCode);
            }
#endif // HAVE_GCCOVER

#ifdef FEATURE_INTERPRETER
            // Determine whether the new code address is "stable"...= is not an interpreter stub.
            fInterpreted = (Interpreter::InterpretationStubToMethodInfo(pCode) == this);
            fStable = !fInterpreted;
#endif // FEATURE_INTERPRETER

#ifdef FEATURE_MULTICOREJIT
            
            // If called from multi-core JIT background thread, store code under lock, delay patching until code is queried from application threads
            if (fBackgroundThread)
            {
                // Fire an ETW event to mark the end of JIT'ing
                ETW::MethodLog::MethodJitted(this, &namespaceOrClassName, &methodName, &methodSignature, pCode, 0 /* ReJITID */);

#ifdef FEATURE_PERFMAP
                // Save the JIT'd method information so that perf can resolve JIT'd call frames.
                PerfMap::LogJITCompiledMethod(this, pCode, sizeOfCode);
#endif
                
                mcJitManager.GetMulticoreJitCodeStorage().StoreMethodCode(this, pCode);
                
                goto Done;
            }

GotNewCode:
#endif
            // If this function had already been requested for rejit (before its original
            // code was jitted), then give the rejit manager a chance to jump-stamp the
            // code we just compiled so the first thread entering the function will jump
            // to the prestub and trigger the rejit. Note that the PublishMethodHolder takes
            // a lock to avoid a particular kind of rejit race. See
            // code:ReJitManager::PublishMethodHolder::PublishMethodHolder#PublishCode for
            // details on the rejit race.
            // 
            // Aside from rejit, performing a SetNativeCodeInterlocked at this point
            // generally ensures that there is only one winning version of the native
            // code. This also avoid races with profiler overriding ngened code (see
            // matching SetNativeCodeInterlocked done after
            // JITCachedFunctionSearchStarted)
#ifdef FEATURE_INTERPRETER
            PCODE pExpected = pPreviousInterpStub;
            if (pExpected == NULL) pExpected = GetTemporaryEntryPoint();
#endif
            {
                ReJitPublishMethodHolder publishWorker(this, pCode);
                if (!SetNativeCodeInterlocked(pCode
#ifdef FEATURE_INTERPRETER
                    , pExpected, fStable
#endif
                    ))
                {
                    // Another thread beat us to publishing its copy of the JITted code.
                    pCode = GetNativeCode();
                    goto Done;
                }
            }

#ifdef FEATURE_INTERPRETER
            // State for dynamic methods cannot be freed if the method was ever interpreted,
            // since there is no way to ensure that it is not in use at the moment.
            if (IsDynamicMethod() && !fInterpreted && (pPreviousInterpStub == NULL))
            {
                AsDynamicMethodDesc()->GetResolver()->FreeCompileTimeState();
            }
#endif // FEATURE_INTERPRETER

            // We succeeded in jitting the code, and our jitted code is the one that's going to run now.
            pEntry->m_hrResultCode = S_OK;

 #ifdef PROFILING_SUPPORTED 
            // Notify the profiler that JIT completed.
            // Must do this after the address has been set.
            // @ToDo: Why must we set the address before notifying the profiler ??
            //        Note that if IsInterceptedForDeclSecurity is set no one should access the jitted code address anyway.
            {
                BEGIN_PIN_PROFILER(CORProfilerTrackJITInfo());
                if (!IsNoMetadata())
                {
                    g_profControlBlock.pProfInterface->
                        JITCompilationFinished((FunctionID) this,
                                                pEntry->m_hrResultCode, 
                                                TRUE);
                }
                else
                {
                    g_profControlBlock.pProfInterface->DynamicMethodJITCompilationFinished((FunctionID) this, pEntry->m_hrResultCode, TRUE);
                }
                END_PIN_PROFILER();
            }
#endif // PROFILING_SUPPORTED

#ifdef FEATURE_MULTICOREJIT
            if (! fCompiledInBackground)
#endif
#ifdef FEATURE_INTERPRETER
            // If we didn't JIT, but rather, created an interpreter stub (i.e., fStable is false), don't tell ETW that we did.
            if (fStable)
#endif // FEATURE_INTERPRETER
            {
                // Fire an ETW event to mark the end of JIT'ing
                ETW::MethodLog::MethodJitted(this, &namespaceOrClassName, &methodName, &methodSignature, pCode, 0 /* ReJITID */);

#ifdef FEATURE_PERFMAP
                // Save the JIT'd method information so that perf can resolve JIT'd call frames.
                PerfMap::LogJITCompiledMethod(this, pCode, sizeOfCode);
#endif
            }
 

#ifdef FEATURE_MULTICOREJIT

            // If not called from multi-core JIT thread, not got code from storage, quick check before calling out of line function
            if (! fBackgroundThread && ! fCompiledInBackground && mcJitManager.IsRecorderActive())
            {
                if (MulticoreJitManager::IsMethodSupported(this))
                {
                    mcJitManager.RecordMethodJit(this); // Tell multi-core JIT manager to record method on successful JITting
                }
            }
#endif

            if (!fIsILStub)
            {
                // The notification will only occur if someone has registered for this method.
                DACNotifyCompilationFinished(this);
            }
        }
    }

Done:

    // We must have a code by now.
    _ASSERTE(pCode != NULL);

    LOG((LF_CORDB, LL_EVERYTHING, "MethodDesc::MakeJitWorker finished. Stub is" FMT_ADDR "\n",
         DBG_ADDR(pCode)));

    return pCode;
}

#ifdef FEATURE_STUBS_AS_IL

// CreateInstantiatingILStubTargetSig:
// This method is used to create the signature of the target of the ILStub
// for instantiating and unboxing stubs, when/where we need to introduce a generic context.
// And since the generic context is a hidden parameter, we're creating a signature that
// looks like non-generic but has one additional parameter right after the thisptr
void CreateInstantiatingILStubTargetSig(MethodDesc *pBaseMD, 
                                        SigTypeContext &typeContext,
                                        SigBuilder *stubSigBuilder)
{
    STANDARD_VM_CONTRACT;

    MetaSig msig(pBaseMD);
    BYTE callingConvention = IMAGE_CEE_CS_CALLCONV_DEFAULT;
    if (msig.HasThis())
        callingConvention |= IMAGE_CEE_CS_CALLCONV_HASTHIS;
    // CallingConvention
    stubSigBuilder->AppendByte(callingConvention); 

    // ParamCount
    stubSigBuilder->AppendData(msig.NumFixedArgs() + 1); // +1 is for context param

    // Return type
    SigPointer pReturn = msig.GetReturnProps();
    pReturn.ConvertToInternalExactlyOne(msig.GetModule(), &typeContext, stubSigBuilder, FALSE);

#ifndef _TARGET_X86_
    // The hidden context parameter
    stubSigBuilder->AppendElementType(ELEMENT_TYPE_I);            
#endif // !_TARGET_X86_

    // Copy rest of the arguments
    msig.NextArg();
    SigPointer pArgs = msig.GetArgProps();
    for (unsigned i = 0; i < msig.NumFixedArgs(); i++)
    {
        pArgs.ConvertToInternalExactlyOne(msig.GetModule(), &typeContext, stubSigBuilder);
    }

#ifdef _TARGET_X86_
    // The hidden context parameter
    stubSigBuilder->AppendElementType(ELEMENT_TYPE_I);
#endif // _TARGET_X86_
}

Stub * CreateUnboxingILStubForSharedGenericValueTypeMethods(MethodDesc* pTargetMD)
{

    CONTRACT(Stub*)
    {
        THROWS;
        GC_TRIGGERS;
        POSTCONDITION(CheckPointer(RETVAL));
    }
    CONTRACT_END;

    SigTypeContext typeContext(pTargetMD);

    MetaSig msig(pTargetMD);

    _ASSERTE(msig.HasThis());

    ILStubLinker sl(pTargetMD->GetModule(), 
                    pTargetMD->GetSignature(), 
                    &typeContext, 
                    pTargetMD, 
                    TRUE,           // fTargetHasThis
                    TRUE,           // fStubHasThis
                    FALSE           // fIsNDirectStub
                    );

    ILCodeStream *pCode = sl.NewCodeStream(ILStubLinker::kDispatch);

    // 1. Build the new signature
    SigBuilder stubSigBuilder;
    CreateInstantiatingILStubTargetSig(pTargetMD, typeContext, &stubSigBuilder);

    // 2. Emit the method body
    mdToken tokPinningHelper = pCode->GetToken(MscorlibBinder::GetField(FIELD__PINNING_HELPER__M_DATA));

    // 2.1 Push the thisptr
    // We need to skip over the MethodTable*
    // The trick below will do that.
    pCode->EmitLoadThis();
    pCode->EmitLDFLDA(tokPinningHelper);

#if defined(_TARGET_X86_)
    // 2.2 Push the rest of the arguments for x86
    for (unsigned i = 0; i < msig.NumFixedArgs();i++)
    {
        pCode->EmitLDARG(i);
    }
#endif

    // 2.3 Push the hidden context param
    // The context is going to be captured from the thisptr
    pCode->EmitLoadThis();
    pCode->EmitLDFLDA(tokPinningHelper);
    pCode->EmitLDC(Object::GetOffsetOfFirstField());
    pCode->EmitSUB();
    pCode->EmitLDIND_I();

#if !defined(_TARGET_X86_)
    // 2.4 Push the rest of the arguments for not x86
    for (unsigned i = 0; i < msig.NumFixedArgs();i++)
    {
        pCode->EmitLDARG(i);
    }
#endif

    // 2.5 Push the target address
    pCode->EmitLDC((TADDR)pTargetMD->GetMultiCallableAddrOfCode(CORINFO_ACCESS_ANY));

    // 2.6 Do the calli
    pCode->EmitCALLI(TOKEN_ILSTUB_TARGET_SIG, msig.NumFixedArgs() + 1, msig.IsReturnTypeVoid() ? 0 : 1);
    pCode->EmitRET();

    PCCOR_SIGNATURE pSig;
    DWORD cbSig;
    pTargetMD->GetSig(&pSig,&cbSig);
    PTR_Module pLoaderModule = pTargetMD->GetLoaderModule();
    MethodDesc * pStubMD = ILStubCache::CreateAndLinkNewILStubMethodDesc(pTargetMD->GetLoaderAllocator(),
                                                            pLoaderModule->GetILStubCache()->GetOrCreateStubMethodTable(pLoaderModule),
                                                            ILSTUB_UNBOXINGILSTUB, 
                                                            pTargetMD->GetModule(),
                                                            pSig, cbSig,
                                                            &typeContext,
                                                            &sl);

    ILStubResolver *pResolver = pStubMD->AsDynamicMethodDesc()->GetILStubResolver();
    
    DWORD cbTargetSig = 0;
    PCCOR_SIGNATURE pTargetSig = (PCCOR_SIGNATURE) stubSigBuilder.GetSignature(&cbTargetSig);
    pResolver->SetStubTargetMethodSig(pTargetSig, cbTargetSig);
    pResolver->SetStubTargetMethodDesc(pTargetMD);

    RETURN Stub::NewStub(JitILStub(pStubMD));

}

Stub * CreateInstantiatingILStub(MethodDesc* pTargetMD, void* pHiddenArg)
{

    CONTRACT(Stub*)
    {
        THROWS;
        GC_TRIGGERS;
        PRECONDITION(CheckPointer(pHiddenArg));
        POSTCONDITION(CheckPointer(RETVAL));
    }
    CONTRACT_END;

    SigTypeContext typeContext;
    MethodTable* pStubMT;
    if (pTargetMD->HasMethodInstantiation())
    {
        // The pHiddenArg shall be a MethodDesc*
        MethodDesc* pMD = static_cast<MethodDesc *>(pHiddenArg);
        SigTypeContext::InitTypeContext(pMD, &typeContext);
        pStubMT = pMD->GetMethodTable();
    }
    else
    {
        // The pHiddenArg shall be a MethodTable*
        SigTypeContext::InitTypeContext(TypeHandle::FromPtr(pHiddenArg), &typeContext);
        pStubMT = static_cast<MethodTable *>(pHiddenArg);
    }

    MetaSig msig(pTargetMD);

    ILStubLinker sl(pTargetMD->GetModule(), 
                    pTargetMD->GetSignature(), 
                    &typeContext, 
                    pTargetMD, 
                    msig.HasThis(), // fTargetHasThis
                    msig.HasThis(), // fStubHasThis
                    FALSE           // fIsNDirectStub
                    );

    ILCodeStream *pCode = sl.NewCodeStream(ILStubLinker::kDispatch);

    // 1. Build the new signature
    SigBuilder stubSigBuilder;
    CreateInstantiatingILStubTargetSig(pTargetMD, typeContext, &stubSigBuilder);
    
    // 2. Emit the method body
    if (msig.HasThis())
    {
        // 2.1 Push the thisptr
        pCode->EmitLoadThis();
    }

#if defined(_TARGET_X86_)
    // 2.2 Push the rest of the arguments for x86
    for (unsigned i = 0; i < msig.NumFixedArgs();i++)
    {
        pCode->EmitLDARG(i);
    }
#endif // _TARGET_X86_

    // 2.3 Push the hidden context param
    // InstantiatingStub
    pCode->EmitLDC((TADDR)pHiddenArg);

#if !defined(_TARGET_X86_)
    // 2.4 Push the rest of the arguments for not x86
    for (unsigned i = 0; i < msig.NumFixedArgs();i++)
    {
        pCode->EmitLDARG(i);
    }
#endif // !_TARGET_X86_

    // 2.5 Push the target address
    pCode->EmitLDC((TADDR)pTargetMD->GetMultiCallableAddrOfCode(CORINFO_ACCESS_ANY));

    // 2.6 Do the calli
    pCode->EmitCALLI(TOKEN_ILSTUB_TARGET_SIG, msig.NumFixedArgs() + 1, msig.IsReturnTypeVoid() ? 0 : 1);
    pCode->EmitRET();

    PCCOR_SIGNATURE pSig;
    DWORD cbSig;
    pTargetMD->GetSig(&pSig,&cbSig);
    PTR_Module pLoaderModule = pTargetMD->GetLoaderModule();
    MethodDesc * pStubMD = ILStubCache::CreateAndLinkNewILStubMethodDesc(pTargetMD->GetLoaderAllocator(),
                                                            pStubMT,
                                                            ILSTUB_INSTANTIATINGSTUB, 
                                                            pTargetMD->GetModule(),
                                                            pSig, cbSig,
                                                            &typeContext,
                                                            &sl);

    ILStubResolver *pResolver = pStubMD->AsDynamicMethodDesc()->GetILStubResolver();

    DWORD cbTargetSig = 0;
    PCCOR_SIGNATURE pTargetSig = (PCCOR_SIGNATURE) stubSigBuilder.GetSignature(&cbTargetSig);
    pResolver->SetStubTargetMethodSig(pTargetSig, cbTargetSig);
    pResolver->SetStubTargetMethodDesc(pTargetMD);

    RETURN Stub::NewStub(JitILStub(pStubMD));
}
#endif

/* Make a stub that for a value class method that expects a BOXed this pointer */
Stub * MakeUnboxingStubWorker(MethodDesc *pMD)
{
    CONTRACT(Stub*)
    {
        THROWS;
        GC_TRIGGERS;
        POSTCONDITION(CheckPointer(RETVAL));
    }
    CONTRACT_END;

    Stub *pstub = NULL;

    _ASSERTE (pMD->GetMethodTable()->IsValueType());
    _ASSERTE(!pMD->ContainsGenericVariables());
    MethodDesc *pUnboxedMD = pMD->GetWrappedMethodDesc();

    _ASSERTE(pUnboxedMD != NULL && pUnboxedMD != pMD);

#ifdef FEATURE_STUBS_AS_IL
    if (pUnboxedMD->RequiresInstMethodTableArg())
    {
        pstub = CreateUnboxingILStubForSharedGenericValueTypeMethods(pUnboxedMD);
    }
    else
#endif
    {
        CPUSTUBLINKER sl;
        sl.EmitUnboxMethodStub(pUnboxedMD);
        pstub = sl.Link(pMD->GetLoaderAllocator()->GetStubHeap());
    }
    RETURN pstub;
}

#if defined(FEATURE_SHARE_GENERIC_CODE) 
Stub * MakeInstantiatingStubWorker(MethodDesc *pMD)
{
    CONTRACT(Stub*)
    {
        THROWS;
        GC_TRIGGERS;
        PRECONDITION(pMD->IsInstantiatingStub());
        PRECONDITION(!pMD->RequiresInstArg());
        PRECONDITION(!pMD->IsSharedByGenericMethodInstantiations());
        POSTCONDITION(CheckPointer(RETVAL));
    }
    CONTRACT_END;

    // Note: this should be kept idempotent ... in the sense that
    // if multiple threads get in here for the same pMD
    // it should not matter whose stuff finally gets used.

    MethodDesc *pSharedMD = NULL;
    void* extraArg = NULL;

    // It's an instantiated generic method
    // Fetch the shared code associated with this instantiation
    pSharedMD = pMD->GetWrappedMethodDesc();
    _ASSERTE(pSharedMD != NULL && pSharedMD != pMD);

    if (pMD->HasMethodInstantiation())
    {
        extraArg = pMD;
    }
    else
    {
        // It's a per-instantiation static method
        extraArg = pMD->GetMethodTable();
    }
    Stub *pstub = NULL;

#ifdef FEATURE_STUBS_AS_IL
    pstub = CreateInstantiatingILStub(pSharedMD, extraArg);
#else
    CPUSTUBLINKER sl;
    _ASSERTE(pSharedMD != NULL && pSharedMD != pMD);
    sl.EmitInstantiatingMethodStub(pSharedMD, extraArg);

    pstub = sl.Link(pMD->GetLoaderAllocator()->GetStubHeap());
#endif

    RETURN pstub;
}
#endif // defined(FEATURE_SHARE_GENERIC_CODE)

#if defined (HAS_COMPACT_ENTRYPOINTS) && defined (_TARGET_ARM_)

extern "C" MethodDesc * STDCALL PreStubGetMethodDescForCompactEntryPoint (PCODE pCode)
{
    _ASSERTE (pCode >= PC_REG_RELATIVE_OFFSET);

    pCode = (PCODE) (pCode - PC_REG_RELATIVE_OFFSET + THUMB_CODE);

    _ASSERTE (MethodDescChunk::IsCompactEntryPointAtAddress (pCode));

    return MethodDescChunk::GetMethodDescFromCompactEntryPoint(pCode, FALSE);
}

#endif // defined (HAS_COMPACT_ENTRYPOINTS) && defined (_TARGET_ARM_)

//=============================================================================
// This function generates the real code for a method and installs it into
// the methoddesc. Usually ***BUT NOT ALWAYS***, this function runs only once
// per methoddesc. In addition to installing the new code, this function
// returns a pointer to the new code for the prestub's convenience.
//=============================================================================
extern "C" PCODE STDCALL PreStubWorker(TransitionBlock * pTransitionBlock, MethodDesc * pMD)
{
    PCODE pbRetVal = NULL;

    BEGIN_PRESERVE_LAST_ERROR;

    STATIC_CONTRACT_THROWS;
    STATIC_CONTRACT_GC_TRIGGERS;
    STATIC_CONTRACT_MODE_COOPERATIVE;
    STATIC_CONTRACT_ENTRY_POINT;

    MAKE_CURRENT_THREAD_AVAILABLE();

#ifdef _DEBUG
    Thread::ObjectRefFlush(CURRENT_THREAD);
#endif

    FrameWithCookie<PrestubMethodFrame> frame(pTransitionBlock, pMD);
    PrestubMethodFrame * pPFrame = &frame;

    pPFrame->Push(CURRENT_THREAD);

    INSTALL_MANAGED_EXCEPTION_DISPATCHER;
    INSTALL_UNWIND_AND_CONTINUE_HANDLER;

    ETWOnStartup (PrestubWorker_V1,PrestubWorkerEnd_V1);

    _ASSERTE(!NingenEnabled() && "You cannot invoke managed code inside the ngen compilation process.");

    // Running the PreStubWorker on a method causes us to access its MethodTable
    g_IBCLogger.LogMethodDescAccess(pMD);

    // Make sure the method table is restored, and method instantiation if present
    pMD->CheckRestore();

    CONSISTENCY_CHECK(GetAppDomain()->CheckCanExecuteManagedCode(pMD));

    // Note this is redundant with the above check but we do it anyway for safety
    //
    // This has been disabled so we have a better chance of catching these.  Note that this check is
    // NOT sufficient for domain neutral and ngen cases.
    //
    // pMD->EnsureActive();

    MethodTable *pDispatchingMT = NULL;

    if (pMD->IsVtableMethod())
    {
        OBJECTREF curobj = pPFrame->GetThis();

        if (curobj != NULL) // Check for virtual function called non-virtually on a NULL object
        {
            pDispatchingMT = curobj->GetTrueMethodTable();

#ifdef FEATURE_ICASTABLE
            if (pDispatchingMT->IsICastable())
            {
                MethodTable *pMDMT = pMD->GetMethodTable();
                TypeHandle objectType(pDispatchingMT);
                TypeHandle methodType(pMDMT);

                GCStress<cfg_any>::MaybeTrigger();
                INDEBUG(curobj = NULL); // curobj is unprotected and CanCastTo() can trigger GC
                if (!objectType.CanCastTo(methodType)) 
                {
                    // Apperantly ICastable magic was involved when we chose this method to be called
                    // that's why we better stick to the MethodTable it belongs to, otherwise 
                    // DoPrestub() will fail not being able to find implementation for pMD in pDispatchingMT.

                    pDispatchingMT = pMDMT;
                }
            }
#endif // FEATURE_ICASTABLE

            // For value types, the only virtual methods are interface implementations.
            // Thus pDispatching == pMT because there
            // is no inheritance in value types.  Note the BoxedEntryPointStubs are shared
            // between all sharable generic instantiations, so the == test is on
            // canonical method tables.
#ifdef _DEBUG 
            MethodTable *pMDMT = pMD->GetMethodTable(); // put this here to see what the MT is in debug mode
            _ASSERTE(!pMD->GetMethodTable()->IsValueType() ||
                     (pMD->IsUnboxingStub() && (pDispatchingMT->GetCanonicalMethodTable() == pMDMT->GetCanonicalMethodTable())));
#endif // _DEBUG
        }
    }

    GCX_PREEMP_THREAD_EXISTS(CURRENT_THREAD);
    pbRetVal = pMD->DoPrestub(pDispatchingMT);

    UNINSTALL_UNWIND_AND_CONTINUE_HANDLER;
    UNINSTALL_MANAGED_EXCEPTION_DISPATCHER;

    {
        HardwareExceptionHolder

        // Give debugger opportunity to stop here
        ThePreStubPatch();
    }

    pPFrame->Pop(CURRENT_THREAD);

    POSTCONDITION(pbRetVal != NULL);

    END_PRESERVE_LAST_ERROR;

    return pbRetVal;
}

#ifdef _DEBUG 
//
// These are two functions for testing purposes only, in debug builds only. They can be used by setting
// InjectFatalError to 3. They ensure that we really can restore the guard page for SEH try/catch clauses.
//
// @todo: Do we use this for anything anymore?
//
static void TestSEHGuardPageRestoreOverflow()
{
}

static void TestSEHGuardPageRestore()
{
        PAL_TRY(void *, unused, NULL)
        {
            TestSEHGuardPageRestoreOverflow();
        }
        PAL_EXCEPT(EXCEPTION_EXECUTE_HANDLER)
        {
            _ASSERTE(!"Got first overflow.");
        }
        PAL_ENDTRY;

        PAL_TRY(void *, unused, NULL)
        {
            TestSEHGuardPageRestoreOverflow();
        }
        PAL_EXCEPT(EXCEPTION_EXECUTE_HANDLER)
        {
            // If you get two asserts, then it works!
            _ASSERTE(!"Got second overflow.");
        }
        PAL_ENDTRY;
}
#endif // _DEBUG

// Separated out the body of PreStubWorker for the case where we don't have a frame.
//
// Note that pDispatchingMT may not actually be the MT that is indirected through.
// If a virtual method is called non-virtually, pMT will be used to indirect through
// 
// This returns a pointer to the stable entrypoint for the jitted method. Typically, this
// is the same as the pointer to the top of the JITted code of the method. However, in
// the case of methods that require stubs to be executed first (e.g., remoted methods
// that require remoting stubs to be executed first), this stable entrypoint would be a
// pointer to the stub, and not a pointer directly to the JITted code.
PCODE MethodDesc::DoPrestub(MethodTable *pDispatchingMT)
{
    CONTRACT(PCODE)
    {
        STANDARD_VM_CHECK;
        POSTCONDITION(RETVAL != NULL);
    }
    CONTRACT_END;

    Stub *pStub = NULL;
    PCODE pCode = NULL;

    Thread *pThread = GetThread();

    MethodTable *pMT = GetMethodTable();

    // Running a prestub on a method causes us to access its MethodTable
    g_IBCLogger.LogMethodDescAccess(this);

    // A secondary layer of defense against executing code in inspection-only assembly.
    // This should already have been taken care of by not allowing inspection assemblies
    // to be activated. However, this is a very inexpensive piece of insurance in the name
    // of security.
    if (IsIntrospectionOnly())
    {
        _ASSERTE(!"A ReflectionOnly assembly reached the prestub. This should not have happened.");
        COMPlusThrow(kInvalidOperationException, IDS_EE_CODEEXECUTION_IN_INTROSPECTIVE_ASSEMBLY);
    }

    if (ContainsGenericVariables())
    {
        COMPlusThrow(kInvalidOperationException, IDS_EE_CODEEXECUTION_CONTAINSGENERICVAR);
    }

    /**************************   DEBUG CHECKS  *************************/
    /*-----------------------------------------------------------------
    // Halt if needed, GC stress, check the sharing count etc.
    */

#ifdef _DEBUG 
    static unsigned ctr = 0;
    ctr++;

    if (g_pConfig->ShouldPrestubHalt(this))
    {
        _ASSERTE(!"PreStubHalt");
    }

    LOG((LF_CLASSLOADER, LL_INFO10000, "In PreStubWorker for %s::%s\n",
                m_pszDebugClassName, m_pszDebugMethodName));

    // This is a nice place to test out having some fatal EE errors. We do this only in a checked build, and only
    // under the InjectFatalError key.
    if (g_pConfig->InjectFatalError() == 1)
    {
        EEPOLICY_HANDLE_FATAL_ERROR(COR_E_EXECUTIONENGINE);
    }
    else if (g_pConfig->InjectFatalError() == 2)
    {
        EEPOLICY_HANDLE_FATAL_ERROR(COR_E_STACKOVERFLOW);
    }
    else if (g_pConfig->InjectFatalError() == 3)
    {
        TestSEHGuardPageRestore();
    }

    // Useful to test GC with the prestub on the call stack
    if (g_pConfig->ShouldPrestubGC(this))
    {
        GCX_COOP();
        GCHeapUtilities::GetGCHeap()->GarbageCollect(-1);
    }
#endif // _DEBUG

    STRESS_LOG1(LF_CLASSLOADER, LL_INFO10000, "Prestubworker: method %pM\n", this);


    GCStress<cfg_any, EeconfigFastGcSPolicy, CoopGcModePolicy>::MaybeTrigger();

    // Are we in the prestub because of a rejit request?  If so, let the ReJitManager
    // take it from here.
    pCode = ReJitManager::DoReJitIfNecessary(this);
    if (pCode != NULL)
    {
        // A ReJIT was performed, so nothing left for DoPrestub() to do. Return now.
        // 
        // The stable entrypoint will either be a pointer to the original JITted code
        // (with a jmp at the top to jump to the newly-rejitted code) OR a pointer to any
        // stub code that must be executed first (e.g., a remoting stub), which in turn
        // will call the original JITted code (which then jmps to the newly-rejitted
        // code).
        RETURN GetStableEntryPoint();
    }


#ifdef FEATURE_COMINTEROP 
    /**************************   INTEROP   *************************/
    /*-----------------------------------------------------------------
    // Some method descriptors are COMPLUS-to-COM call descriptors
    // they are not your every day method descriptors, for example
    // they don't have an IL or code.
    */
    if (IsComPlusCall() || IsGenericComPlusCall())
    {
        pCode = GetStubForInteropMethod(this);
        
        GetPrecode()->SetTargetInterlocked(pCode);

        RETURN GetStableEntryPoint();
    }
#endif // FEATURE_COMINTEROP

    // workaround: This is to handle a punted work item dealing with a skipped module constructor
    //       due to appdomain unload. Basically shared code was JITted in domain A, and then
    //       this caused a link to another shared module with a module CCTOR, which was skipped
    //       or aborted in another appdomain we were trying to propagate the activation to.
    //
    //       Note that this is not a fix, but that it just minimizes the window in which the
    //       issue can occur.
    if (pThread->IsAbortRequested())
    {
        pThread->HandleThreadAbort();
    }

    /**************************   CLASS CONSTRUCTOR   ********************/
    // Make sure .cctor has been run

    if (IsClassConstructorTriggeredViaPrestub())
    {
        pMT->CheckRunClassInitThrowing();
    }

    /**************************   BACKPATCHING   *************************/
    // See if the addr of code has changed from the pre-stub
#ifdef FEATURE_INTERPRETER
    if (!IsReallyPointingToPrestub())
#else
    if (!IsPointingToPrestub())
#endif
    {
        // If we are counting calls for tiered compilation, leave the prestub
        // in place so that we can continue intercepting method invocations.
        // When the TieredCompilationManager has received enough call notifications
        // for this method only then do we back-patch it.
#ifdef FEATURE_TIERED_COMPILATION
        PCODE pNativeCode = GetNativeCode();
        if (pNativeCode && IsEligibleForTieredCompilation())
        {
            CallCounter * pCallCounter = GetAppDomain()->GetCallCounter();
            BOOL doBackPatch = pCallCounter->OnMethodCalled(this);
            if (!doBackPatch)
            {
                return pNativeCode;
            }
        }
#endif
        LOG((LF_CLASSLOADER, LL_INFO10000,
                "    In PreStubWorker, method already jitted, backpatching call point\n"));

        RETURN DoBackpatch(pMT, pDispatchingMT, TRUE);
    }

    // record if remoting needs to intercept this call
    BOOL  fRemotingIntercepted = IsRemotingInterceptedViaPrestub();

    BOOL  fReportCompilationFinished = FALSE;
    
    /**************************   CODE CREATION  *************************/
    if (IsUnboxingStub())
    {
        pStub = MakeUnboxingStubWorker(this);
    }
#if defined(FEATURE_SHARE_GENERIC_CODE) 
    else if (IsInstantiatingStub())
    {
        pStub = MakeInstantiatingStubWorker(this);
    }
#endif // defined(FEATURE_SHARE_GENERIC_CODE)
    else if (IsIL() || IsNoMetadata())
    {
        // remember if we need to backpatch the MethodTable slot
        BOOL  fBackpatch = !fRemotingIntercepted
                            && IsNativeCodeStableAfterInit();

#ifdef FEATURE_PREJIT 
        //
        // See if we have any prejitted code to use.
        //

        pCode = GetPreImplementedCode();

#ifdef PROFILING_SUPPORTED
        if (pCode != NULL)
        {
            BOOL fShouldSearchCache = TRUE;

            {
                BEGIN_PIN_PROFILER(CORProfilerTrackCacheSearches());
                g_profControlBlock.pProfInterface->
                    JITCachedFunctionSearchStarted((FunctionID) this,
                                                   &fShouldSearchCache);
                END_PIN_PROFILER();
            }

            if (!fShouldSearchCache)
            {
#ifdef FEATURE_INTERPRETER
                SetNativeCodeInterlocked(NULL, pCode, FALSE);
#else
                SetNativeCodeInterlocked(NULL, pCode);
#endif
                _ASSERTE(!IsPreImplemented());
                pCode = NULL;
            }
        }
#endif // PROFILING_SUPPORTED

        if (pCode != NULL)
        {
            LOG((LF_ZAP, LL_INFO10000,
                "ZAP: Using code" FMT_ADDR "for %s.%s sig=\"%s\" (token %x).\n",
                    DBG_ADDR(pCode),
                    m_pszDebugClassName,
                    m_pszDebugMethodName,
                    m_pszDebugMethodSignature,
                    GetMemberDef()));

            TADDR pFixupList = GetFixupList();
            if (pFixupList != NULL)
            {
                Module *pZapModule = GetZapModule();
                _ASSERTE(pZapModule != NULL);
                if (!pZapModule->FixupDelayList(pFixupList))
                {
                    _ASSERTE(!"FixupDelayList failed");
                    ThrowHR(COR_E_BADIMAGEFORMAT);
                }
            }

#ifdef HAVE_GCCOVER
            if (GCStress<cfg_instr_ngen>::IsEnabled())
                SetupGcCoverage(this, (BYTE*) pCode);
#endif // HAVE_GCCOVER

#ifdef PROFILING_SUPPORTED 
            /*
                * This notifies the profiler that a search to find a
                * cached jitted function has been made.
                */
            {
                BEGIN_PIN_PROFILER(CORProfilerTrackCacheSearches());
                g_profControlBlock.pProfInterface->
                    JITCachedFunctionSearchFinished((FunctionID) this, COR_PRF_CACHED_FUNCTION_FOUND);
                END_PIN_PROFILER();
            }
#endif // PROFILING_SUPPORTED
        }

        //
        // If not, try to jit it
        //

#endif // FEATURE_PREJIT

#ifdef FEATURE_READYTORUN
        if (pCode == NULL)
        {
            Module * pModule = GetModule();
            if (pModule->IsReadyToRun())
            {
                pCode = pModule->GetReadyToRunInfo()->GetEntryPoint(this);
                if (pCode != NULL)
                    fReportCompilationFinished = TRUE;
            }
        }
#endif // FEATURE_READYTORUN

        if (pCode == NULL)
        {
            NewHolder<COR_ILMETHOD_DECODER> pHeader(NULL);
            // Get the information on the method
            if (!IsNoMetadata())
            {
                COR_ILMETHOD* ilHeader = GetILHeader(TRUE);
                if(ilHeader == NULL)
                {
#ifdef FEATURE_COMINTEROP
                    // Abstract methods can be called through WinRT derivation if the deriving type
                    // is not implemented in managed code, and calls through the CCW to the abstract
                    // method. Throw a sensible exception in that case.
                    if (pMT->IsExportedToWinRT() && IsAbstract())
                    {
                        COMPlusThrowHR(E_NOTIMPL);
                    }
#endif // FEATURE_COMINTEROP

                    COMPlusThrowHR(COR_E_BADIMAGEFORMAT, BFA_BAD_IL);
                }

                COR_ILMETHOD_DECODER::DecoderStatus status = COR_ILMETHOD_DECODER::FORMAT_ERROR;

                {
                    // Decoder ctor can AV on a malformed method header
                    AVInRuntimeImplOkayHolder AVOkay;
                    pHeader = new COR_ILMETHOD_DECODER(ilHeader, GetMDImport(), &status);
                    if(pHeader == NULL)
                        status = COR_ILMETHOD_DECODER::FORMAT_ERROR;
                }

                if (status == COR_ILMETHOD_DECODER::VERIFICATION_ERROR &&
                    Security::CanSkipVerification(GetModule()->GetDomainAssembly()))
                {
                    status = COR_ILMETHOD_DECODER::SUCCESS;
                }

                if (status != COR_ILMETHOD_DECODER::SUCCESS)
                {
                    if (status == COR_ILMETHOD_DECODER::VERIFICATION_ERROR)
                    {
                        // Throw a verification HR
                        COMPlusThrowHR(COR_E_VERIFICATION);
                    }
                    else
                    {
                        COMPlusThrowHR(COR_E_BADIMAGEFORMAT, BFA_BAD_IL);
                    }
                }

#ifdef _VER_EE_VERIFICATION_ENABLED 
                static ConfigDWORD peVerify;

                if (peVerify.val(CLRConfig::EXTERNAL_PEVerify))
                    Verify(pHeader, TRUE, FALSE);   // Throws a VerifierException if verification fails
#endif // _VER_EE_VERIFICATION_ENABLED
            } // end if (!IsNoMetadata())

            // JIT it
            LOG((LF_CLASSLOADER, LL_INFO1000000,
                    "    In PreStubWorker, calling MakeJitWorker\n"));

            // Create the precode eagerly if it is going to be needed later.
            if (!fBackpatch)
            {
                GetOrCreatePrecode();
            }

            // Mark the code as hot in case the method ends up in the native image
            g_IBCLogger.LogMethodCodeAccess(this);

            pCode = MakeJitWorker(pHeader, CORJIT_FLAGS());

#ifdef FEATURE_INTERPRETER
            if ((pCode != NULL) && !HasStableEntryPoint())
            {
                // We don't yet have a stable entry point, so don't do backpatching yet.
                // But we do have to handle some extra cases that occur in backpatching.
                // (Perhaps I *should* get to the backpatching code, but in a mode where we know
                // we're not dealing with the stable entry point...)
                if (HasNativeCodeSlot())
                {
                    // We called "SetNativeCodeInterlocked" in MakeJitWorker, which updated the native
                    // code slot, but I think we also want to update the regular slot...
                    PCODE tmpEntry = GetTemporaryEntryPoint();
                    PCODE pFound = FastInterlockCompareExchangePointer(GetAddrOfSlot(), pCode, tmpEntry);
                    // Doesn't matter if we failed -- if we did, it's because somebody else made progress.
                    if (pFound != tmpEntry) pCode = pFound;
                }

                // Now we handle the case of a FuncPtrPrecode.  
                FuncPtrStubs * pFuncPtrStubs = GetLoaderAllocator()->GetFuncPtrStubsNoCreate();
                if (pFuncPtrStubs != NULL)
                {
                    Precode* pFuncPtrPrecode = pFuncPtrStubs->Lookup(this);
                    if (pFuncPtrPrecode != NULL)
                    {
                        // If there is a funcptr precode to patch, attempt to patch it.  If we lose, that's OK,
                        // somebody else made progress.
                        pFuncPtrPrecode->SetTargetInterlocked(pCode);
                    }
                }
            }
#endif // FEATURE_INTERPRETER
        } // end if (pCode == NULL)
    } // end else if (IsIL() || IsNoMetadata())
    else if (IsNDirect())
    {
        pCode = GetStubForInteropMethod(this);
        GetOrCreatePrecode();
    }
    else if (IsFCall())
    {
        // Get the fcall implementation
        BOOL fSharedOrDynamicFCallImpl;
        pCode = ECall::GetFCallImpl(this, &fSharedOrDynamicFCallImpl);

        if (fSharedOrDynamicFCallImpl)
        {
            // Fake ctors share one implementation that has to be wrapped by prestub
            GetOrCreatePrecode();
        }
    }
    else if (IsArray())
    {
        pStub = GenerateArrayOpStub((ArrayMethodDesc*)this);
    }
    else if (IsEEImpl())
    {
        _ASSERTE(GetMethodTable()->IsDelegate());
        pCode = COMDelegate::GetInvokeMethodStub((EEImplMethodDesc*)this);
        GetOrCreatePrecode();
    }
    else
    {
        // This is a method type we don't handle yet
        _ASSERTE(!"Unknown Method Type");
    }

    /**************************   POSTJIT *************************/
#ifndef FEATURE_INTERPRETER
    _ASSERTE(pCode == NULL || GetNativeCode() == NULL || pCode == GetNativeCode());
#else // FEATURE_INTERPRETER
    // Interpreter adds a new possiblity == someone else beat us to installing an intepreter stub.
    _ASSERTE(pCode == NULL || GetNativeCode() == NULL || pCode == GetNativeCode()
             || Interpreter::InterpretationStubToMethodInfo(pCode) == this);
#endif // FEATURE_INTERPRETER

    // At this point we must have either a pointer to managed code or to a stub. All of the above code
    // should have thrown an exception if it couldn't make a stub.
    _ASSERTE((pStub != NULL) ^ (pCode != NULL));

    /**************************   SECURITY   *************************/

    // Lets check to see if we need declarative security on this stub, If we have
    // security checks on this method or class then we need to add an intermediate
    // stub that performs declarative checks prior to calling the real stub.
    // record if security needs to intercept this call (also depends on whether we plan to use stubs for declarative security)


    _ASSERTE((pStub != NULL) ^ (pCode != NULL));

#if defined(_TARGET_X86_) || defined(_TARGET_AMD64_)
    //
    // We are seeing memory reordering race around fixups (see DDB 193514 and related bugs). We get into
    // situation where the patched precode is visible by other threads, but the resolved fixups 
    // are not. IT SHOULD NEVER HAPPEN according to our current understanding of x86/x64 memory model.
    // (see email thread attached to the bug for details).
    //
    // We suspect that there may be bug in the hardware or that hardware may have shortcuts that may be 
    // causing grief. We will try to avoid the race by executing an extra memory barrier.
    //
    MemoryBarrier();
#endif

    // If we are counting calls for tiered compilation, leave the prestub
    // in place so that we can continue intercepting method invocations.
    // When the TieredCompilationManager has received enough call notifications
    // for this method only then do we back-patch it.
#ifdef FEATURE_TIERED_COMPILATION
    if (pCode && IsEligibleForTieredCompilation())
    {
        CallCounter * pCallCounter = GetAppDomain()->GetCallCounter();
        BOOL doBackPatch = pCallCounter->OnMethodCalled(this);
        if (!doBackPatch)
        {
            return pCode;
        }
    }
#endif

    if (pCode != NULL)
    {
        if (HasPrecode())
            GetPrecode()->SetTargetInterlocked(pCode);
        else
        if (!HasStableEntryPoint())
        {
            // Is the result an interpreter stub?
#ifdef FEATURE_INTERPRETER
            if (Interpreter::InterpretationStubToMethodInfo(pCode) == this)
            {
                SetEntryPointInterlocked(pCode);
            }
            else
#endif // FEATURE_INTERPRETER
            {
                ReJitPublishMethodHolder publishWorker(this, pCode);
                SetStableEntryPointInterlocked(pCode);
            }
        }
    }
    else
    {
        if (!GetOrCreatePrecode()->SetTargetInterlocked(pStub->GetEntryPoint()))
        {
            pStub->DecRef();
        }
        else
        if (pStub->HasExternalEntryPoint())
        {
            // If the Stub wraps code that is outside of the Stub allocation, then we
            // need to free the Stub allocation now.
            pStub->DecRef();
        }
    }

#ifdef FEATURE_INTERPRETER
    _ASSERTE(!IsReallyPointingToPrestub());
#else // FEATURE_INTERPRETER
    _ASSERTE(!IsPointingToPrestub());
    _ASSERTE(HasStableEntryPoint());
#endif // FEATURE_INTERPRETER

    if (fReportCompilationFinished)
        DACNotifyCompilationFinished(this);

    RETURN DoBackpatch(pMT, pDispatchingMT, FALSE);
}

#endif // !DACCESS_COMPILE

//==========================================================================
// The following code manages the PreStub. All method stubs initially
// use the prestub.
//==========================================================================

#if defined(_TARGET_X86_) && !defined(FEATURE_STUBS_AS_IL)
static PCODE g_UMThunkPreStub;
#endif // _TARGET_X86_ && !FEATURE_STUBS_AS_IL

#ifndef DACCESS_COMPILE 

void ThePreStubManager::Init(void)
{
    STANDARD_VM_CONTRACT;

    //
    // Add the prestub manager
    //

    StubManager::AddStubManager(new ThePreStubManager());
}

//-----------------------------------------------------------
// Initialize the prestub.
//-----------------------------------------------------------
void InitPreStubManager(void)
{
    STANDARD_VM_CONTRACT;

    if (NingenEnabled())
    {
        return;
    }

#if defined(_TARGET_X86_) && !defined(FEATURE_STUBS_AS_IL)
    g_UMThunkPreStub = GenerateUMThunkPrestub()->GetEntryPoint();
#endif // _TARGET_X86_ && !FEATURE_STUBS_AS_IL

    ThePreStubManager::Init();
}

PCODE TheUMThunkPreStub()
{
    LIMITED_METHOD_CONTRACT;

#if defined(_TARGET_X86_) && !defined(FEATURE_STUBS_AS_IL)
    return g_UMThunkPreStub;
#else  // _TARGET_X86_ && !FEATURE_STUBS_AS_IL
    return GetEEFuncEntryPoint(TheUMEntryPrestub);
#endif // _TARGET_X86_ && !FEATURE_STUBS_AS_IL
}

PCODE TheVarargNDirectStub(BOOL hasRetBuffArg)
{
    LIMITED_METHOD_CONTRACT;

#if !defined(_TARGET_X86_)
    if (hasRetBuffArg)
    {
        return GetEEFuncEntryPoint(VarargPInvokeStub_RetBuffArg);
    }
    else
#endif
    {
        return GetEEFuncEntryPoint(VarargPInvokeStub);
    }
}

static PCODE PatchNonVirtualExternalMethod(MethodDesc * pMD, PCODE pCode, PTR_CORCOMPILE_IMPORT_SECTION pImportSection, TADDR pIndirection)
{
    STANDARD_VM_CONTRACT;

    //
    // Skip fixup precode jump for better perf. Since we have MethodDesc available, we can use cheaper method 
    // than code:Precode::TryToSkipFixupPrecode.
    //
#ifdef HAS_FIXUP_PRECODE
    if (pMD->HasPrecode() && pMD->GetPrecode()->GetType() == PRECODE_FIXUP
        && pMD->IsNativeCodeStableAfterInit()
#ifndef HAS_REMOTING_PRECODE
        && !pMD->IsRemotingInterceptedViaPrestub()
#endif
        )
    {
        PCODE pDirectTarget = pMD->IsFCall() ? ECall::GetFCallImpl(pMD) : pMD->GetNativeCode();
        if (pDirectTarget != NULL)
            pCode = pDirectTarget;
    }
#endif //HAS_FIXUP_PRECODE

    if (pImportSection->Flags & CORCOMPILE_IMPORT_FLAGS_CODE)
    {
        CORCOMPILE_EXTERNAL_METHOD_THUNK * pThunk = (CORCOMPILE_EXTERNAL_METHOD_THUNK *)pIndirection;

#if defined(_TARGET_X86_) || defined(_TARGET_AMD64_)
        INT64 oldValue = *(INT64*)pThunk;
        BYTE* pOldValue = (BYTE*)&oldValue;

        if (pOldValue[0] == X86_INSTR_CALL_REL32)
        {
            INT64 newValue = oldValue;
            BYTE* pNewValue = (BYTE*)&newValue;
            pNewValue[0] = X86_INSTR_JMP_REL32;

            *(INT32 *)(pNewValue+1) = rel32UsingJumpStub((INT32*)(&pThunk->callJmp[1]), pCode, pMD, NULL);

            _ASSERTE(IS_ALIGNED((size_t)pThunk, sizeof(INT64)));
            EnsureWritableExecutablePages(pThunk, sizeof(INT64));
            FastInterlockCompareExchangeLong((INT64*)pThunk, newValue, oldValue);

            FlushInstructionCache(GetCurrentProcess(), pThunk, 8);
        }
#elif  defined(_TARGET_ARM_) || defined(_TARGET_ARM64_)
        // Patchup the thunk to point to the actual implementation of the cross module external method
        EnsureWritableExecutablePages(&pThunk->m_pTarget);
        pThunk->m_pTarget = pCode;

        #if defined(_TARGET_ARM_)
        // ThumbBit must be set on the target address
        _ASSERTE(pCode & THUMB_CODE);
        #endif
#else
        PORTABILITY_ASSERT("ExternalMethodFixupWorker");
#endif
    }
    else
    {
        *EnsureWritableExecutablePages((TADDR *)pIndirection) = pCode;
    }

    return pCode;
}

//==========================================================================================
// In NGen images calls to external methods start out pointing to jump thunks.
// These jump thunks initially point to the assembly code _ExternalMethodFixupStub
// It transfers control to ExternalMethodFixupWorker which will patch the jump 
// thunk to point to the actual cross module address for the method body
// Some methods also have one-time prestubs we defer the patching until
// we have the final stable method entry point.
//
EXTERN_C PCODE STDCALL ExternalMethodFixupWorker(TransitionBlock * pTransitionBlock, TADDR pIndirection, DWORD sectionIndex, Module * pModule)
{
    STATIC_CONTRACT_THROWS;
    STATIC_CONTRACT_GC_TRIGGERS;
    STATIC_CONTRACT_MODE_COOPERATIVE;
    STATIC_CONTRACT_ENTRY_POINT;

    // We must save (and restore) the Last Error code before we call anything 
    // that could overwrite it.  Any callsite that leads to TlsGetValue will 
    // potentially overwrite the Last Error code.

    //
    // In Dev10 bug 837293 we were overwriting the Last Error code on the first 
    // call to a PInvoke method.  This occurred when we were running a 
    // (precompiled) PInvoke IL stub implemented in the ngen image.
    //
    // In this IL stub implementation we call the native method kernel32!GetFileAttributes,
    // and then we immediately try to save the Last Error code by calling the 
    // mscorlib method System.StubHelpers.StubHelpers.SetLastError().
    //
    // However when we are coming from a precompiled IL Stub in an ngen image
    // we must use an ExternalMethodFixup to find the target address of 
    // System.StubHelpers.StubHelpers.SetLastError() and this was overwriting
    // the value of the Last Error before it could be retrieved and saved.
    //

    PCODE         pCode   = NULL;

    BEGIN_PRESERVE_LAST_ERROR;

    MAKE_CURRENT_THREAD_AVAILABLE();

#ifdef _DEBUG
    Thread::ObjectRefFlush(CURRENT_THREAD);
#endif

    FrameWithCookie<ExternalMethodFrame> frame(pTransitionBlock);
    ExternalMethodFrame * pEMFrame = &frame;

#if defined(_TARGET_X86_) || defined(_TARGET_AMD64_)
    // Decode indirection cell from callsite if it is not present
    if (pIndirection == NULL)
    {
        // Asssume that the callsite is call [xxxxxxxx]
        PCODE retAddr = pEMFrame->GetReturnAddress();
#ifdef _TARGET_X86_
        pIndirection = *(((TADDR *)retAddr) - 1);
#else
        pIndirection = *(((INT32 *)retAddr) - 1) + retAddr;
#endif
    }
#endif

    // FUTURE: Consider always passing in module and section index to avoid the lookups
    if (pModule == NULL)
    {
        pModule = ExecutionManager::FindZapModule(pIndirection);
        sectionIndex = (DWORD)-1;
    }
    _ASSERTE(pModule != NULL);

    pEMFrame->SetCallSite(pModule, pIndirection);

    pEMFrame->Push(CURRENT_THREAD);         // Push the new ExternalMethodFrame onto the frame stack

    INSTALL_MANAGED_EXCEPTION_DISPATCHER;
    INSTALL_UNWIND_AND_CONTINUE_HANDLER;

    bool fVirtual = false;
    MethodDesc * pMD = NULL;
    MethodTable * pMT = NULL;
    DWORD slot = 0;

    {
        GCX_PREEMP_THREAD_EXISTS(CURRENT_THREAD);

        PEImageLayout *pNativeImage = pModule->GetNativeOrReadyToRunImage();

        RVA rva = pNativeImage->GetDataRva(pIndirection);

        PTR_CORCOMPILE_IMPORT_SECTION pImportSection;
        if (sectionIndex != (DWORD)-1)
        {
            pImportSection = pModule->GetImportSectionFromIndex(sectionIndex);
            _ASSERTE(pImportSection == pModule->GetImportSectionForRVA(rva));
        }
        else
        {
            pImportSection = pModule->GetImportSectionForRVA(rva);
        }
        _ASSERTE(pImportSection != NULL);

        COUNT_T index;
        if (pImportSection->Flags & CORCOMPILE_IMPORT_FLAGS_CODE)
        {
            _ASSERTE(pImportSection->EntrySize == sizeof(CORCOMPILE_EXTERNAL_METHOD_THUNK));
            index = (rva - pImportSection->Section.VirtualAddress) / sizeof(CORCOMPILE_EXTERNAL_METHOD_THUNK);
        }
        else
        {
            _ASSERTE(pImportSection->EntrySize == sizeof(TADDR));
            index = (rva - pImportSection->Section.VirtualAddress) / sizeof(TADDR);
        }

        PTR_DWORD pSignatures = dac_cast<PTR_DWORD>(pNativeImage->GetRvaData(pImportSection->Signatures));

        PCCOR_SIGNATURE pBlob = (BYTE *)pNativeImage->GetRvaData(pSignatures[index]);

        BYTE kind = *pBlob++;

        Module * pInfoModule = pModule;
        if (kind & ENCODE_MODULE_OVERRIDE)
        {
            DWORD moduleIndex = CorSigUncompressData(pBlob);
            pInfoModule = pModule->GetModuleFromIndex(moduleIndex);
            kind &= ~ENCODE_MODULE_OVERRIDE;
        }

        TypeHandle th;
        switch (kind)
        {
        case ENCODE_METHOD_ENTRY:
            {
                pMD =  ZapSig::DecodeMethod(pModule,
                                            pInfoModule,
                                            pBlob);

                if (pModule->IsReadyToRun())
                {
                    // We do not emit activation fixups for version resilient references. Activate the target explicitly.
                    pMD->EnsureActive();
                }

                break;
            }

        case ENCODE_METHOD_ENTRY_DEF_TOKEN:
            {
                mdToken MethodDef = TokenFromRid(CorSigUncompressData(pBlob), mdtMethodDef);
                pMD = MemberLoader::GetMethodDescFromMethodDef(pInfoModule, MethodDef, FALSE);

                pMD->PrepareForUseAsADependencyOfANativeImage();

                if (pModule->IsReadyToRun())
                {
                    // We do not emit activation fixups for version resilient references. Activate the target explicitly.
                    pMD->EnsureActive();
                }

                break;
            }

        case ENCODE_METHOD_ENTRY_REF_TOKEN:
            {
                SigTypeContext typeContext;
                mdToken MemberRef = TokenFromRid(CorSigUncompressData(pBlob), mdtMemberRef);
                FieldDesc * pFD = NULL;

                MemberLoader::GetDescFromMemberRef(pInfoModule, MemberRef, &pMD, &pFD, &typeContext, FALSE /* strict metadata checks */, &th);
                _ASSERTE(pMD != NULL);

                pMD->PrepareForUseAsADependencyOfANativeImage();

                if (pModule->IsReadyToRun())
                {
                    // We do not emit activation fixups for version resilient references. Activate the target explicitly.
                    pMD->EnsureActive();
                }
                else
                {
#ifdef FEATURE_WINMD_RESILIENT
                    // We do not emit activation fixups for version resilient references. Activate the target explicitly.
                    pMD->EnsureActive();
#endif
                }

                break;
            }

        case ENCODE_VIRTUAL_ENTRY:
            {
                pMD = ZapSig::DecodeMethod(pModule, pInfoModule, pBlob, &th);

        VirtualEntry:
                pMD->PrepareForUseAsADependencyOfANativeImage();

                if (pMD->IsVtableMethod())
                {
                    slot = pMD->GetSlot();
                    pMT = th.IsNull() ? pMD->GetMethodTable() : th.GetMethodTable();

                    fVirtual = true;
                }
                else
                if (pModule->IsReadyToRun())
                {
                    // We do not emit activation fixups for version resilient references. Activate the target explicitly.
                    pMD->EnsureActive();
                }
                break;
            }

        case ENCODE_VIRTUAL_ENTRY_DEF_TOKEN:
            {
                mdToken MethodDef = TokenFromRid(CorSigUncompressData(pBlob), mdtMethodDef);
                pMD = MemberLoader::GetMethodDescFromMethodDef(pInfoModule, MethodDef, FALSE);

                goto VirtualEntry;
            }

        case ENCODE_VIRTUAL_ENTRY_REF_TOKEN:
            {
                mdToken MemberRef = TokenFromRid(CorSigUncompressData(pBlob), mdtMemberRef);

                FieldDesc * pFD = NULL;

                SigTypeContext typeContext;
                MemberLoader::GetDescFromMemberRef(pInfoModule, MemberRef, &pMD, &pFD, &typeContext, FALSE /* strict metadata checks */, &th, TRUE /* actual type required */);
                _ASSERTE(pMD != NULL);

                goto VirtualEntry;
            }

        case ENCODE_VIRTUAL_ENTRY_SLOT:
            {
                slot = CorSigUncompressData(pBlob);
                pMT =  ZapSig::DecodeType(pModule, pInfoModule, pBlob).GetMethodTable();

                fVirtual = true;
                break;
            }

        default:
            _ASSERTE(!"Unexpected CORCOMPILE_FIXUP_BLOB_KIND");
            ThrowHR(COR_E_BADIMAGEFORMAT);
        }

        if (fVirtual)
        {
            GCX_COOP_THREAD_EXISTS(CURRENT_THREAD);

            // Get the stub manager for this module
            VirtualCallStubManager *pMgr = pModule->GetLoaderAllocator()->GetVirtualCallStubManager();

            DispatchToken token;
            if (pMT->IsInterface())
                token = pMT->GetLoaderAllocator()->GetDispatchToken(pMT->GetTypeID(), slot);
            else
                token = DispatchToken::CreateDispatchToken(slot);

            OBJECTREF *protectedObj = pEMFrame->GetThisPtr();
            _ASSERTE(protectedObj != NULL);
            if (*protectedObj == NULL) {
                COMPlusThrow(kNullReferenceException);
            }
            
            StubCallSite callSite(pIndirection, pEMFrame->GetReturnAddress());
            pCode = pMgr->ResolveWorker(&callSite, protectedObj, token, VirtualCallStubManager::SK_LOOKUP);
            _ASSERTE(pCode != NULL);
        }
        else
        {
            _ASSERTE(pMD != NULL);

            {
                // Switch to cooperative mode to avoid racing with GC stackwalk
                GCX_COOP_THREAD_EXISTS(CURRENT_THREAD);
                pEMFrame->SetFunction(pMD);
            }

            pCode = pMD->GetMethodEntryPoint();

            //
            // Note that we do not want to call code:MethodDesc::IsPointingToPrestub() here. It does not take remoting interception 
            // into account and so it would cause otherwise intercepted methods to be JITed. It is a compat issue if the JITing fails.
            //
            if (!DoesSlotCallPrestub(pCode))
            {
                pCode = PatchNonVirtualExternalMethod(pMD, pCode, pImportSection, pIndirection);
            }
        }
    }

    // Force a GC on every jit if the stress level is high enough
    GCStress<cfg_any>::MaybeTrigger();

    // Ready to return

    UNINSTALL_UNWIND_AND_CONTINUE_HANDLER;
    UNINSTALL_MANAGED_EXCEPTION_DISPATCHER;

    pEMFrame->Pop(CURRENT_THREAD);          // Pop the ExternalMethodFrame from the frame stack

    END_PRESERVE_LAST_ERROR;

    return pCode;
}


#if !defined(_TARGET_X86_) && !defined(_TARGET_AMD64_)

//==========================================================================================
// In NGen image, virtual slots inherited from cross-module dependencies point to jump thunks.
// These jump thunk initially point to VirtualMethodFixupStub which transfers control here.
// This method 'VirtualMethodFixupWorker' will patch the jump thunk to point to the actual
// inherited method body after we have execute the precode and a stable entry point.
//
EXTERN_C PCODE VirtualMethodFixupWorker(Object * pThisPtr,  CORCOMPILE_VIRTUAL_IMPORT_THUNK *pThunk)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER; 
        MODE_COOPERATIVE;
        ENTRY_POINT;
    }
    CONTRACTL_END;

    _ASSERTE(pThisPtr != NULL);
    VALIDATEOBJECT(pThisPtr);

    MethodTable * pMT = pThisPtr->GetTrueMethodTable();

    WORD slotNumber = pThunk->slotNum;
    _ASSERTE(slotNumber != (WORD)-1);

    PCODE pCode = pMT->GetRestoredSlot(slotNumber);

    if (!DoesSlotCallPrestub(pCode))
    {
        // Skip fixup precode jump for better perf
        PCODE pDirectTarget = Precode::TryToSkipFixupPrecode(pCode);
        if (pDirectTarget != NULL)
            pCode = pDirectTarget;

        // Patch the thunk to the actual method body
        if (EnsureWritableExecutablePagesNoThrow(&pThunk->m_pTarget, sizeof(pThunk->m_pTarget)))
            pThunk->m_pTarget = pCode;
    }
#if defined(_TARGET_ARM_)
    // The target address should have the thumb bit set
    _ASSERTE(pCode & THUMB_CODE);
#endif
    return pCode;
}
#endif // !defined(_TARGET_X86_) && !defined(_TARGET_AMD64_)

#ifdef FEATURE_READYTORUN

static PCODE getHelperForInitializedStatic(Module * pModule, CORCOMPILE_FIXUP_BLOB_KIND kind, MethodTable * pMT, FieldDesc * pFD)
{
    STANDARD_VM_CONTRACT;

    PCODE pHelper = NULL; 

    switch (kind)
    {
    case ENCODE_STATIC_BASE_NONGC_HELPER:
        {
            PVOID baseNonGC;
            {
                GCX_COOP();
                baseNonGC = pMT->GetNonGCStaticsBasePointer();
            }
            pHelper = DynamicHelpers::CreateReturnConst(pModule->GetLoaderAllocator(), (TADDR)baseNonGC);
        }
        break;
    case ENCODE_STATIC_BASE_GC_HELPER:
        {
            PVOID baseGC;
            {
                GCX_COOP();
                baseGC = pMT->GetGCStaticsBasePointer();
            }
            pHelper = DynamicHelpers::CreateReturnConst(pModule->GetLoaderAllocator(), (TADDR)baseGC);
        }
        break;
    case ENCODE_CCTOR_TRIGGER:
        pHelper = DynamicHelpers::CreateReturn(pModule->GetLoaderAllocator());
        break;
    case ENCODE_FIELD_ADDRESS:
        {
            _ASSERTE(pFD->IsStatic());

            PTR_VOID pAddress;

            {
                GCX_COOP();

                PTR_BYTE base = 0;
                if (!pFD->IsRVA()) // for RVA the base is ignored
                    base = pFD->GetBase();
                pAddress = pFD->GetStaticAddressHandle((void *)dac_cast<TADDR>(base));
            }

            // The following code assumes that the statics are pinned that is not the case for collectible types
            _ASSERTE(!pFD->GetEnclosingMethodTable()->Collectible());

            // Unbox valuetype fields
            if (pFD->GetFieldType() == ELEMENT_TYPE_VALUETYPE && !pFD->IsRVA())
                pHelper = DynamicHelpers::CreateReturnIndirConst(pModule->GetLoaderAllocator(), (TADDR)pAddress, (INT8)Object::GetOffsetOfFirstField());
            else
                pHelper = DynamicHelpers::CreateReturnConst(pModule->GetLoaderAllocator(), (TADDR)pAddress);
        }
        break;
    default:
        _ASSERTE(!"Unexpected statics CORCOMPILE_FIXUP_BLOB_KIND");
        ThrowHR(COR_E_BADIMAGEFORMAT);
    }

    return pHelper;
}

static PCODE getHelperForSharedStatic(Module * pModule, CORCOMPILE_FIXUP_BLOB_KIND kind, MethodTable * pMT, FieldDesc * pFD)
{
    STANDARD_VM_CONTRACT;

    _ASSERTE(kind == ENCODE_FIELD_ADDRESS);

    CorInfoHelpFunc helpFunc = CEEInfo::getSharedStaticsHelper(pFD, pMT);

    TADDR moduleID = pMT->GetModuleForStatics()->GetModuleID();

    TADDR classID = 0;
    if (helpFunc != CORINFO_HELP_GETSHARED_NONGCSTATIC_BASE_NOCTOR && helpFunc != CORINFO_HELP_GETSHARED_GCSTATIC_BASE_NOCTOR)
    {
        if (pMT->IsDynamicStatics())
        {
            classID = pMT->GetModuleDynamicEntryID();
        }
        else
        {
            classID = pMT->GetClassIndex();
        }
    }

    bool fUnbox = (pFD->GetFieldType() == ELEMENT_TYPE_VALUETYPE);

    AllocMemTracker amTracker;

    StaticFieldAddressArgs * pArgs = (StaticFieldAddressArgs *)amTracker.Track(
        pModule->GetLoaderAllocator()->GetHighFrequencyHeap()->
            AllocMem(S_SIZE_T(sizeof(StaticFieldAddressArgs))));

    pArgs->staticBaseHelper = (FnStaticBaseHelper)CEEJitInfo::getHelperFtnStatic((CorInfoHelpFunc)helpFunc);
    pArgs->arg0 = moduleID;
    pArgs->arg1 = classID;
    pArgs->offset = pFD->GetOffset(); 

    PCODE pHelper = DynamicHelpers::CreateHelper(pModule->GetLoaderAllocator(), (TADDR)pArgs, 
        fUnbox ? GetEEFuncEntryPoint(JIT_StaticFieldAddressUnbox_Dynamic) : GetEEFuncEntryPoint(JIT_StaticFieldAddress_Dynamic));

    amTracker.SuppressRelease();

    return pHelper;
}

static PCODE getHelperForStaticBase(Module * pModule, CORCOMPILE_FIXUP_BLOB_KIND kind, MethodTable * pMT)
{
    STANDARD_VM_CONTRACT;

    int helpFunc = CORINFO_HELP_GETSHARED_NONGCSTATIC_BASE;

    if (kind == ENCODE_STATIC_BASE_GC_HELPER || kind == ENCODE_THREAD_STATIC_BASE_GC_HELPER)
    {
        helpFunc = CORINFO_HELP_GETSHARED_GCSTATIC_BASE;
    }

    if (pMT->IsDynamicStatics())
    {
        const int delta = CORINFO_HELP_GETSHARED_GCSTATIC_BASE_DYNAMICCLASS - CORINFO_HELP_GETSHARED_GCSTATIC_BASE;
        helpFunc += delta;
    }
    else
    if (!pMT->HasClassConstructor() && !pMT->HasBoxedRegularStatics())
    {
        const int delta = CORINFO_HELP_GETSHARED_GCSTATIC_BASE_NOCTOR - CORINFO_HELP_GETSHARED_GCSTATIC_BASE;
        helpFunc += delta;
    }

    if (kind == ENCODE_THREAD_STATIC_BASE_NONGC_HELPER || kind == ENCODE_THREAD_STATIC_BASE_GC_HELPER)
    {
        const int delta = CORINFO_HELP_GETSHARED_GCTHREADSTATIC_BASE - CORINFO_HELP_GETSHARED_GCSTATIC_BASE;
        helpFunc += delta;
    }

    PCODE pHelper; 
    if (helpFunc == CORINFO_HELP_GETSHARED_NONGCSTATIC_BASE_NOCTOR || helpFunc == CORINFO_HELP_GETSHARED_GCSTATIC_BASE_NOCTOR)
    {
        pHelper = DynamicHelpers::CreateHelper(pModule->GetLoaderAllocator(), pMT->GetModule()->GetModuleID(), CEEJitInfo::getHelperFtnStatic((CorInfoHelpFunc)helpFunc));
    }
    else
    {
        TADDR moduleID = pMT->GetModuleForStatics()->GetModuleID();

        TADDR classID;
        if (pMT->IsDynamicStatics())
        {
            classID = pMT->GetModuleDynamicEntryID();
        }
        else
        {
            classID = pMT->GetClassIndex();
        }

        pHelper = DynamicHelpers::CreateHelper(pModule->GetLoaderAllocator(), moduleID, classID, CEEJitInfo::getHelperFtnStatic((CorInfoHelpFunc)helpFunc));
    }

    return pHelper;
}

TADDR GetFirstArgumentRegisterValuePtr(TransitionBlock * pTransitionBlock)
{
    TADDR pArgument = (TADDR)pTransitionBlock + TransitionBlock::GetOffsetOfArgumentRegisters();
#ifdef _TARGET_X86_
    // x86 is special as always
    pArgument += offsetof(ArgumentRegisters, ECX);
#endif

    return pArgument;
}

void ProcessDynamicDictionaryLookup(TransitionBlock *           pTransitionBlock, 
                                    Module *                    pModule, 
                                    Module *                    pInfoModule,                                     
                                    BYTE                        kind, 
                                    PCCOR_SIGNATURE             pBlob, 
                                    PCCOR_SIGNATURE             pBlobStart,                                     
                                    CORINFO_RUNTIME_LOOKUP *    pResult, 
                                    DWORD *                     pDictionaryIndexAndSlot)
{
    STANDARD_VM_CONTRACT;

    TADDR genericContextPtr = *(TADDR*)GetFirstArgumentRegisterValuePtr(pTransitionBlock);

    pResult->testForFixup = pResult->testForNull = false;
    pResult->signature = NULL;

    pResult->indirectFirstOffset = 0;
    pResult->indirectSecondOffset = 0;

    pResult->indirections = CORINFO_USEHELPER;

    DWORD numGenericArgs = 0;
    MethodTable* pContextMT = NULL;
    MethodDesc* pContextMD = NULL;

    if (kind == ENCODE_DICTIONARY_LOOKUP_METHOD)
    {
        pContextMD = (MethodDesc*)genericContextPtr;
        numGenericArgs = pContextMD->GetNumGenericMethodArgs();
        pResult->helper = CORINFO_HELP_RUNTIMEHANDLE_METHOD;
    }
    else
    {
        pContextMT = (MethodTable*)genericContextPtr;

        if (kind == ENCODE_DICTIONARY_LOOKUP_THISOBJ)
        {
            TypeHandle contextTypeHandle = ZapSig::DecodeType(pModule, pInfoModule, pBlob);

            SigPointer p(pBlob);
            p.SkipExactlyOne();
            pBlob = p.GetPtr();

            pContextMT = pContextMT->GetMethodTableMatchingParentClass(contextTypeHandle.AsMethodTable());
        }

        numGenericArgs = pContextMT->GetNumGenericArgs();
        pResult->helper = CORINFO_HELP_RUNTIMEHANDLE_CLASS;
    }

    _ASSERTE(numGenericArgs > 0);

    CORCOMPILE_FIXUP_BLOB_KIND signatureKind = (CORCOMPILE_FIXUP_BLOB_KIND)CorSigUncompressData(pBlob);

    //
    // Optimization cases
    //
    if (signatureKind == ENCODE_TYPE_HANDLE)
    {
        SigPointer sigptr(pBlob, -1);

        CorElementType type;
        IfFailThrow(sigptr.GetElemType(&type));

        if ((type == ELEMENT_TYPE_MVAR) && (kind == ENCODE_DICTIONARY_LOOKUP_METHOD))
        {
            pResult->indirections = 2;
            pResult->offsets[0] = offsetof(InstantiatedMethodDesc, m_pPerInstInfo);

            if (decltype(InstantiatedMethodDesc::m_pPerInstInfo)::isRelative)
            {
                pResult->indirectFirstOffset = 1;
            }

            ULONG data;
            IfFailThrow(sigptr.GetData(&data));
            pResult->offsets[1] = sizeof(TypeHandle) * data;

            return;
        }
        else if ((type == ELEMENT_TYPE_VAR) && (kind != ENCODE_DICTIONARY_LOOKUP_METHOD))
        {
            pResult->indirections = 3;
            pResult->offsets[0] = MethodTable::GetOffsetOfPerInstInfo();
            pResult->offsets[1] = sizeof(TypeHandle*) * (pContextMT->GetNumDicts() - 1);

            ULONG data;
            IfFailThrow(sigptr.GetData(&data));
            pResult->offsets[2] = sizeof(TypeHandle) * data;

            if (MethodTable::IsPerInstInfoRelative())
            {
                pResult->indirectFirstOffset = 1;
                pResult->indirectSecondOffset = 1;
            }

            return;
        }
    }

    if (pContextMT != NULL && pContextMT->GetNumDicts() > 0xFFFF)
        ThrowHR(COR_E_BADIMAGEFORMAT);

    // Dictionary index and slot number are encoded in a 32-bit DWORD. The higher 16 bits
    // are used for the dictionary index, and the lower 16 bits for the slot number.
    *pDictionaryIndexAndSlot = (pContextMT == NULL ? 0 : pContextMT->GetNumDicts() - 1);
    *pDictionaryIndexAndSlot <<= 16;
    
    WORD dictionarySlot;

    if (kind == ENCODE_DICTIONARY_LOOKUP_METHOD)
    {
        if (DictionaryLayout::FindToken(pModule->GetLoaderAllocator(), numGenericArgs, pContextMD->GetDictionaryLayout(), pResult, (BYTE*)pBlobStart, 1, FromReadyToRunImage, &dictionarySlot))
        {
            pResult->testForNull = 1;

            // Indirect through dictionary table pointer in InstantiatedMethodDesc
            pResult->offsets[0] = offsetof(InstantiatedMethodDesc, m_pPerInstInfo);

            if (decltype(InstantiatedMethodDesc::m_pPerInstInfo)::isRelative)
            {
                pResult->indirectFirstOffset = 1;
            }

            *pDictionaryIndexAndSlot |= dictionarySlot;
        }
    }

    // It's a class dictionary lookup (CORINFO_LOOKUP_CLASSPARAM or CORINFO_LOOKUP_THISOBJ)
    else
    {
        if (DictionaryLayout::FindToken(pModule->GetLoaderAllocator(), numGenericArgs, pContextMT->GetClass()->GetDictionaryLayout(), pResult, (BYTE*)pBlobStart, 2, FromReadyToRunImage, &dictionarySlot))
        {
            pResult->testForNull = 1;

            // Indirect through dictionary table pointer in vtable
            pResult->offsets[0] = MethodTable::GetOffsetOfPerInstInfo();

            // Next indirect through the dictionary appropriate to this instantiated type
            pResult->offsets[1] = sizeof(TypeHandle*) * (pContextMT->GetNumDicts() - 1);

            if (MethodTable::IsPerInstInfoRelative())
            {
                pResult->indirectFirstOffset = 1;
                pResult->indirectSecondOffset = 1;
            }

            *pDictionaryIndexAndSlot |= dictionarySlot;
        }
    }
}

PCODE DynamicHelperFixup(TransitionBlock * pTransitionBlock, TADDR * pCell, DWORD sectionIndex, Module * pModule, CORCOMPILE_FIXUP_BLOB_KIND * pKind, TypeHandle * pTH, MethodDesc ** ppMD, FieldDesc ** ppFD)
{
    STANDARD_VM_CONTRACT;

    PEImageLayout *pNativeImage = pModule->GetNativeOrReadyToRunImage();

    RVA rva = pNativeImage->GetDataRva((TADDR)pCell);

    PTR_CORCOMPILE_IMPORT_SECTION pImportSection = pModule->GetImportSectionFromIndex(sectionIndex);
    _ASSERTE(pImportSection == pModule->GetImportSectionForRVA(rva));

    _ASSERTE(pImportSection->EntrySize == sizeof(TADDR));

    COUNT_T index = (rva - pImportSection->Section.VirtualAddress) / sizeof(TADDR);

    PTR_DWORD pSignatures = dac_cast<PTR_DWORD>(pNativeImage->GetRvaData(pImportSection->Signatures));

    PCCOR_SIGNATURE pBlob = (BYTE *)pNativeImage->GetRvaData(pSignatures[index]);
    PCCOR_SIGNATURE pBlobStart = pBlob;

    BYTE kind = *pBlob++;

    Module * pInfoModule = pModule;
    if (kind & ENCODE_MODULE_OVERRIDE)
    {
        DWORD moduleIndex = CorSigUncompressData(pBlob);
        pInfoModule = pModule->GetModuleFromIndex(moduleIndex);
        kind &= ~ENCODE_MODULE_OVERRIDE;
    }

    bool fReliable = false;
    TypeHandle th;
    MethodDesc * pMD = NULL;
    FieldDesc * pFD = NULL;
    CORINFO_RUNTIME_LOOKUP genericLookup;
    DWORD dictionaryIndexAndSlot = -1;

    switch (kind)
    {
    case ENCODE_NEW_HELPER:
        th = ZapSig::DecodeType(pModule, pInfoModule, pBlob);
        th.AsMethodTable()->EnsureInstanceActive();
        break;
    case ENCODE_ISINSTANCEOF_HELPER:
    case ENCODE_CHKCAST_HELPER:
        fReliable = true;
    case ENCODE_NEW_ARRAY_HELPER:
        th = ZapSig::DecodeType(pModule, pInfoModule, pBlob);
        break;

    case ENCODE_THREAD_STATIC_BASE_NONGC_HELPER:
    case ENCODE_THREAD_STATIC_BASE_GC_HELPER:
    case ENCODE_STATIC_BASE_NONGC_HELPER:
    case ENCODE_STATIC_BASE_GC_HELPER:
    case ENCODE_CCTOR_TRIGGER:
        th = ZapSig::DecodeType(pModule, pInfoModule, pBlob);
    Statics:
        th.AsMethodTable()->EnsureInstanceActive();
        th.AsMethodTable()->CheckRunClassInitThrowing();
        fReliable = true;
        break;

    case ENCODE_FIELD_ADDRESS:
        pFD = ZapSig::DecodeField(pModule, pInfoModule, pBlob, &th);
        _ASSERTE(pFD->IsStatic());
        goto Statics;

    case ENCODE_VIRTUAL_ENTRY:
    // case ENCODE_VIRTUAL_ENTRY_DEF_TOKEN:
    // case ENCODE_VIRTUAL_ENTRY_REF_TOKEN:
    // case ENCODE_VIRTUAL_ENTRY_SLOT:
        fReliable = true;
    case ENCODE_DELEGATE_CTOR:
        {
            pMD = ZapSig::DecodeMethod(pModule, pInfoModule, pBlob, &th);
            if (pMD->RequiresInstArg())
            {
                pMD = MethodDesc::FindOrCreateAssociatedMethodDesc(pMD,
                    th.AsMethodTable(),
                    FALSE /* forceBoxedEntryPoint */,
                    pMD->GetMethodInstantiation(),
                    FALSE /* allowInstParam */);
            }
            pMD->EnsureActive();
        }
        break;

    case ENCODE_DICTIONARY_LOOKUP_THISOBJ:
    case ENCODE_DICTIONARY_LOOKUP_TYPE:
    case ENCODE_DICTIONARY_LOOKUP_METHOD:
        ProcessDynamicDictionaryLookup(pTransitionBlock, pModule, pInfoModule, kind, pBlob, pBlobStart, &genericLookup, &dictionaryIndexAndSlot);
        break;

    default:
        _ASSERTE(!"Unexpected CORCOMPILE_FIXUP_BLOB_KIND");
        ThrowHR(COR_E_BADIMAGEFORMAT);
    }

    PCODE pHelper = NULL;

    if (fReliable)
    {
        // For reliable helpers, exceptions in creating the optimized helper are non-fatal. Swallow them to make CER work well.
        EX_TRY
        {
            switch (kind)
            {
            case ENCODE_ISINSTANCEOF_HELPER:
            case ENCODE_CHKCAST_HELPER:
                {
                    bool fClassMustBeRestored;
                    CorInfoHelpFunc helpFunc = CEEInfo::getCastingHelperStatic(th, /* throwing */ (kind == ENCODE_CHKCAST_HELPER), &fClassMustBeRestored);
                    pHelper = DynamicHelpers::CreateHelperArgMove(pModule->GetLoaderAllocator(), th.AsTAddr(), CEEJitInfo::getHelperFtnStatic(helpFunc));
                }
                break;
            case ENCODE_THREAD_STATIC_BASE_NONGC_HELPER:
            case ENCODE_THREAD_STATIC_BASE_GC_HELPER:
            case ENCODE_STATIC_BASE_NONGC_HELPER:
            case ENCODE_STATIC_BASE_GC_HELPER:
            case ENCODE_CCTOR_TRIGGER:
            case ENCODE_FIELD_ADDRESS:
                {
                    MethodTable * pMT = th.AsMethodTable();

                    bool fNeedsNonTrivialHelper = false;

                    if (pMT->IsDomainNeutral() && !IsSingleAppDomain())
                    {
                        fNeedsNonTrivialHelper = true;
                    }
                    else
                    if (pMT->Collectible() && (kind != ENCODE_CCTOR_TRIGGER))
                    {
                        // Collectible statics are not pinned - the fast getters expect statics to be pinned
                        fNeedsNonTrivialHelper = true;
                    }
                    else
                    {
                        if (pFD != NULL)
                        {
                            fNeedsNonTrivialHelper = !!pFD->IsSpecialStatic();
                        }
                        else
                        {
                            fNeedsNonTrivialHelper = (kind == ENCODE_THREAD_STATIC_BASE_NONGC_HELPER) || (kind == ENCODE_THREAD_STATIC_BASE_GC_HELPER);
                        }
                    }

                    if (fNeedsNonTrivialHelper)
                    {
                        if (pFD != NULL)
                        {
                            if (pFD->IsRVA() || pFD->IsContextStatic())
                            {
                                _ASSERTE(!"Fast getter for rare kinds of static fields");
                            }
                            else
                            {
                                pHelper = getHelperForSharedStatic(pModule, (CORCOMPILE_FIXUP_BLOB_KIND)kind, pMT, pFD);
                            }
                        }
                        else
                        {
                            pHelper = getHelperForStaticBase(pModule, (CORCOMPILE_FIXUP_BLOB_KIND)kind, pMT);
                        }
                    }
                    else
                    {
                        // Delay the creation of the helper until the type is initialized
                        if (pMT->IsClassInited())
                            pHelper = getHelperForInitializedStatic(pModule, (CORCOMPILE_FIXUP_BLOB_KIND)kind, pMT, pFD);
                    }
                }
                break;

            case ENCODE_VIRTUAL_ENTRY:
            // case ENCODE_VIRTUAL_ENTRY_DEF_TOKEN:
            // case ENCODE_VIRTUAL_ENTRY_REF_TOKEN:
            // case ENCODE_VIRTUAL_ENTRY_SLOT:
                {
                   if (!pMD->IsVtableMethod())
                   {
                        pHelper = DynamicHelpers::CreateReturnConst(pModule->GetLoaderAllocator(), pMD->GetMultiCallableAddrOfCode());
                    }
                    else
                    {
                        AllocMemTracker amTracker;

                        VirtualFunctionPointerArgs * pArgs = (VirtualFunctionPointerArgs *)amTracker.Track(
                            pModule->GetLoaderAllocator()->GetHighFrequencyHeap()->
                                AllocMem(S_SIZE_T(sizeof(VirtualFunctionPointerArgs))));

                        pArgs->classHnd = (CORINFO_CLASS_HANDLE)th.AsPtr();
                        pArgs->methodHnd = (CORINFO_METHOD_HANDLE)pMD;

                        pHelper = DynamicHelpers::CreateHelperWithArg(pModule->GetLoaderAllocator(), (TADDR)pArgs, 
                            GetEEFuncEntryPoint(JIT_VirtualFunctionPointer_Dynamic));

                        amTracker.SuppressRelease();
                    }
                }
                break;

            default:
                UNREACHABLE();
            }

            if (pHelper != NULL)
            {
                *EnsureWritableExecutablePages((TADDR *)pCell) = pHelper;
            }

#ifdef _DEBUG
            // Always execute the reliable fallback in debug builds
            pHelper = NULL;
#endif
        }
        EX_CATCH
        {
        }
        EX_END_CATCH (SwallowAllExceptions);
    }
    else
    {
        switch (kind)
        {
        case ENCODE_NEW_HELPER:
            {
                CorInfoHelpFunc helpFunc = CEEInfo::getNewHelperStatic(th.AsMethodTable());
                pHelper = DynamicHelpers::CreateHelper(pModule->GetLoaderAllocator(), th.AsTAddr(), CEEJitInfo::getHelperFtnStatic(helpFunc));
            }
            break;
        case ENCODE_NEW_ARRAY_HELPER:
            {
                CorInfoHelpFunc helpFunc = CEEInfo::getNewArrHelperStatic(th);
                ArrayTypeDesc *pArrayTypeDesc = th.AsArray();
                MethodTable *pArrayMT = pArrayTypeDesc->GetTemplateMethodTable();
                pHelper = DynamicHelpers::CreateHelperArgMove(pModule->GetLoaderAllocator(), dac_cast<TADDR>(pArrayMT), CEEJitInfo::getHelperFtnStatic(helpFunc));
            }
            break;

        case ENCODE_DELEGATE_CTOR:
            {
                MethodTable * pDelegateType = NULL;

                {
                    GCX_COOP();

                    TADDR pArgument = GetFirstArgumentRegisterValuePtr(pTransitionBlock);

                    if (pArgument != NULL)
                    {
                        pDelegateType = (*(Object **)pArgument)->GetMethodTable();
                        _ASSERTE(pDelegateType->IsDelegate());
                    }
                }

                DelegateCtorArgs ctorData;
                ctorData.pMethod = NULL;
                ctorData.pArg3 = NULL;
                ctorData.pArg4 = NULL;
                ctorData.pArg5 = NULL;

                MethodDesc * pDelegateCtor = NULL;

                if (pDelegateType != NULL)
                {
                    pDelegateCtor = COMDelegate::GetDelegateCtor(TypeHandle(pDelegateType), pMD, &ctorData);

                    if (ctorData.pArg4 != NULL || ctorData.pArg5 != NULL)
                    {
                        // This should never happen - we should never get collectible or secure delegates here
                        _ASSERTE(false);
                        pDelegateCtor = NULL;
                    }
                }

                TADDR target = NULL;

                if (pDelegateCtor != NULL)
                {
                    target = pDelegateCtor->GetMultiCallableAddrOfCode();
                }
                else
                {
                    target = ECall::GetFCallImpl(MscorlibBinder::GetMethod(METHOD__DELEGATE__CONSTRUCT_DELEGATE));
                    ctorData.pArg3 = NULL;
                }

                if (ctorData.pArg3 != NULL)
                {
                    pHelper = DynamicHelpers::CreateHelperWithTwoArgs(pModule->GetLoaderAllocator(), pMD->GetMultiCallableAddrOfCode(), (TADDR)ctorData.pArg3, target);
                }
                else
                {
                    pHelper = DynamicHelpers::CreateHelperWithTwoArgs(pModule->GetLoaderAllocator(), pMD->GetMultiCallableAddrOfCode(), target);
                }
            }
            break;

        case ENCODE_DICTIONARY_LOOKUP_THISOBJ:
        case ENCODE_DICTIONARY_LOOKUP_TYPE:
        case ENCODE_DICTIONARY_LOOKUP_METHOD:
            {
                pHelper = DynamicHelpers::CreateDictionaryLookupHelper(pModule->GetLoaderAllocator(), &genericLookup, dictionaryIndexAndSlot, pModule);
            }
            break;

        default:
            UNREACHABLE();
        }

        if (pHelper != NULL)
        {
            *EnsureWritableExecutablePages((TADDR *)pCell) = pHelper;
        }
    }

    *pKind = (CORCOMPILE_FIXUP_BLOB_KIND)kind;
    *pTH = th;
    *ppMD = pMD;
    *ppFD = pFD;

    return pHelper;
}

extern "C" SIZE_T STDCALL DynamicHelperWorker(TransitionBlock * pTransitionBlock, TADDR * pCell, DWORD sectionIndex, Module * pModule, INT frameFlags)
{
    PCODE pHelper = NULL;
    SIZE_T result = NULL;

    STATIC_CONTRACT_THROWS;
    STATIC_CONTRACT_GC_TRIGGERS;
    STATIC_CONTRACT_MODE_COOPERATIVE;

    MAKE_CURRENT_THREAD_AVAILABLE();

#ifdef _DEBUG
    Thread::ObjectRefFlush(CURRENT_THREAD);
#endif

    FrameWithCookie<DynamicHelperFrame> frame(pTransitionBlock, frameFlags);
    DynamicHelperFrame * pFrame = &frame;

    pFrame->Push(CURRENT_THREAD);

    INSTALL_MANAGED_EXCEPTION_DISPATCHER;
    INSTALL_UNWIND_AND_CONTINUE_HANDLER;

#if defined(_TARGET_X86_) || defined(_TARGET_AMD64_)
    // Decode indirection cell from callsite if it is not present
    if (pCell == NULL)
    {
        // Asssume that the callsite is call [xxxxxxxx]
        PCODE retAddr = pFrame->GetReturnAddress();
#ifdef _TARGET_X86_
        pCell = *(((TADDR **)retAddr) - 1);
#else
        pCell = (TADDR *)(*(((INT32 *)retAddr) - 1) + retAddr);
#endif
    }
#endif
    _ASSERTE(pCell != NULL);

    TypeHandle th;
    MethodDesc * pMD = NULL;
    FieldDesc * pFD = NULL;
    CORCOMPILE_FIXUP_BLOB_KIND kind = ENCODE_NONE;

    {
        GCX_PREEMP_THREAD_EXISTS(CURRENT_THREAD);

        pHelper = DynamicHelperFixup(pTransitionBlock, pCell, sectionIndex, pModule, &kind, &th, &pMD, &pFD);
    }

    if (pHelper == NULL)
    {
        TADDR pArgument = GetFirstArgumentRegisterValuePtr(pTransitionBlock);

        switch (kind)
        {
        case ENCODE_ISINSTANCEOF_HELPER:
        case ENCODE_CHKCAST_HELPER:
            {
                BOOL throwInvalidCast = (kind == ENCODE_CHKCAST_HELPER);
                if (*(Object **)pArgument == NULL || ObjIsInstanceOf(*(Object **)pArgument, th, throwInvalidCast))
                {
                    result = (SIZE_T)(*(Object **)pArgument);
                }
                else
                {
                    _ASSERTE (!throwInvalidCast);
                    result = NULL;
                }
            }
            break;
        case ENCODE_STATIC_BASE_NONGC_HELPER:
            result = (SIZE_T)th.AsMethodTable()->GetNonGCStaticsBasePointer();
            break;
        case ENCODE_STATIC_BASE_GC_HELPER:
            result = (SIZE_T)th.AsMethodTable()->GetGCStaticsBasePointer();
            break;
        case ENCODE_THREAD_STATIC_BASE_NONGC_HELPER:
            ThreadStatics::GetTLM(th.AsMethodTable())->EnsureClassAllocated(th.AsMethodTable());
            result = (SIZE_T)th.AsMethodTable()->GetNonGCThreadStaticsBasePointer();
            break;
        case ENCODE_THREAD_STATIC_BASE_GC_HELPER:
            ThreadStatics::GetTLM(th.AsMethodTable())->EnsureClassAllocated(th.AsMethodTable());
            result = (SIZE_T)th.AsMethodTable()->GetGCThreadStaticsBasePointer();
            break;
        case ENCODE_CCTOR_TRIGGER:
            break;
        case ENCODE_FIELD_ADDRESS:
            result = (SIZE_T)pFD->GetCurrentStaticAddress();
            break;
        case ENCODE_VIRTUAL_ENTRY:
        // case ENCODE_VIRTUAL_ENTRY_DEF_TOKEN:
        // case ENCODE_VIRTUAL_ENTRY_REF_TOKEN:
        // case ENCODE_VIRTUAL_ENTRY_SLOT:
            {
                OBJECTREF objRef = ObjectToOBJECTREF(*(Object **)pArgument);

                GCPROTECT_BEGIN(objRef);

                if (objRef == NULL)
                    COMPlusThrow(kNullReferenceException);

                // Duplicated logic from JIT_VirtualFunctionPointer_Framed
                if (!pMD->IsVtableMethod())
                {
                    result = pMD->GetMultiCallableAddrOfCode();
                }
                else
                {
                    result = pMD->GetMultiCallableAddrOfVirtualizedCode(&objRef, th);
                }

                GCPROTECT_END();
            }
            break;
        default:
            UNREACHABLE();
        }
    }

    UNINSTALL_UNWIND_AND_CONTINUE_HANDLER;
    UNINSTALL_MANAGED_EXCEPTION_DISPATCHER;

    pFrame->Pop(CURRENT_THREAD);

    if (pHelper == NULL)
        *(SIZE_T *)((TADDR)pTransitionBlock + TransitionBlock::GetOffsetOfArgumentRegisters()) = result;
    return pHelper;
}

#endif // FEATURE_READYTORUN

#endif // !DACCESS_COMPILE
