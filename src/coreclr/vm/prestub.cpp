// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// ===========================================================================
// File: Prestub.cpp
//

// ===========================================================================
// This file contains the implementation for creating and using prestubs
// ===========================================================================
//


#include "common.h"
#include "vars.hpp"
#include "eeconfig.h"
#include "dllimport.h"
#include "comdelegate.h"
#include "dbginterface.h"
#include "stubgen.h"
#include "eventtrace.h"
#include "array.h"
#include "ecall.h"
#include "virtualcallstub.h"

#ifdef FEATURE_INTERPRETER
#include "interpreter.h"
#endif

#ifdef FEATURE_COMINTEROP
#include "clrtocomcall.h"
#endif

#ifdef FEATURE_PERFMAP
#include "perfmap.h"
#endif

#include "methoddescbackpatchinfo.h"

#if defined(FEATURE_GDBJIT)
#include "gdbjit.h"
#endif // FEATURE_GDBJIT

#ifndef DACCESS_COMPILE

#include "customattribute.h"

#if defined(FEATURE_JIT_PITCHING)
EXTERN_C void CheckStacksAndPitch();
EXTERN_C void SavePitchingCandidate(MethodDesc* pMD, ULONG sizeOfCode);
EXTERN_C void DeleteFromPitchingCandidate(MethodDesc* pMD);
EXTERN_C void MarkMethodNotPitchingCandidate(MethodDesc* pMD);
#endif

EXTERN_C void STDCALL ThePreStubPatch();

#if defined(HAVE_GCCOVER)
CrstStatic MethodDesc::m_GCCoverCrst;

void MethodDesc::Init()
{
    m_GCCoverCrst.Init(CrstGCCover);
}

#endif

#define LOG_USING_R2R_CODE(method)  LOG((LF_ZAP, LL_INFO10000,                                                            \
                                        "ZAP: Using R2R precompiled code" FMT_ADDR " for %s.%s sig=\"%s\" (token %x).\n", \
                                        DBG_ADDR(pCode),                                                                  \
                                        m_pszDebugClassName,                                                              \
                                        m_pszDebugMethodName,                                                             \
                                        m_pszDebugMethodSignature,                                                        \
                                        GetMemberDef()));

//==========================================================================

PCODE MethodDesc::DoBackpatch(MethodTable * pMT, MethodTable *pDispatchingMT, BOOL fFullBackPatch)
{
    CONTRACTL
    {
        STANDARD_VM_CHECK;
        PRECONDITION(!ContainsGenericVariables());
        PRECONDITION(pMT == GetMethodTable());
    }
    CONTRACTL_END;

    bool isVersionableWithVtableSlotBackpatch = IsVersionableWithVtableSlotBackpatch();
    LoaderAllocator *mdLoaderAllocator = isVersionableWithVtableSlotBackpatch ? GetLoaderAllocator() : nullptr;

    // Only take the lock if the method is versionable with vtable slot backpatch, for recording slots and synchronizing with
    // backpatching slots
    MethodDescBackpatchInfoTracker::ConditionalLockHolder slotBackpatchLockHolder(isVersionableWithVtableSlotBackpatch);

    // Get the method entry point inside the lock above to synchronize with backpatching in
    // MethodDesc::BackpatchEntryPointSlots()
    PCODE pTarget = GetMethodEntryPoint();

    PCODE pExpected;
    if (isVersionableWithVtableSlotBackpatch)
    {
        _ASSERTE(pTarget == GetEntryPointToBackpatch_Locked());

        pExpected = GetTemporaryEntryPoint();
        if (pExpected == pTarget)
            return pTarget;

        // True interface methods are never backpatched and are not versionable with vtable slot backpatch
        _ASSERTE(!(pMT->IsInterface() && !IsStatic()));

        // Backpatching the funcptr stub:
        //     For methods versionable with vtable slot backpatch, a funcptr stub is guaranteed to point to the at-the-time
        //     current entry point shortly after creation, and backpatching it further is taken care of by
        //     MethodDesc::BackpatchEntryPointSlots()

        // Backpatching the temporary entry point:
        //     The temporary entry point is never backpatched for methods versionable with vtable slot backpatch. New vtable
        //     slots inheriting the method will initially point to the temporary entry point and it must point to the prestub
        //     and come here for backpatching such that the new vtable slot can be discovered and recorded for future
        //     backpatching.

        _ASSERTE(!HasNonVtableSlot());
    }
    else
    {
        _ASSERTE(pTarget == GetStableEntryPoint());

        pExpected = GetTemporaryEntryPoint();
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
    }

    auto RecordAndBackpatchSlot = [&](MethodTable *patchedMT, DWORD slotIndex)
    {
        WRAPPER_NO_CONTRACT;
        _ASSERTE(isVersionableWithVtableSlotBackpatch);

        RecordAndBackpatchEntryPointSlot_Locked(
            mdLoaderAllocator,
            patchedMT->GetLoaderAllocator(),
            dac_cast<TADDR>(patchedMT->GetSlotPtr(slotIndex)),
            EntryPointSlots::SlotType_Vtable,
            pTarget);
    };

    BOOL fBackpatched = FALSE;

#define BACKPATCH(pPatchedMT)                                   \
    do                                                          \
    {                                                           \
        if (pPatchedMT->GetSlot(dwSlot) == pExpected)           \
        {                                                       \
            if (isVersionableWithVtableSlotBackpatch)           \
            {                                                   \
                RecordAndBackpatchSlot(pPatchedMT, dwSlot);     \
            }                                                   \
            else                                                \
            {                                                   \
                pPatchedMT->SetSlot(dwSlot, pTarget);           \
            }                                                   \
            fBackpatched = TRUE;                                \
        }                                                       \
    }                                                           \
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
            if (pRestoredSlotMT != pDispatchingMT)
            {
                BACKPATCH(pRestoredSlotMT);
            }
        }
    }

    if (IsMethodImpl())
    {
        MethodImpl::Iterator it(this);
        while (it.IsValid())
        {
            DWORD dwSlot = it.GetSlot();

            BACKPATCH(pMT);

            if (pDispatchingMT != NULL && pDispatchingMT != pMT)
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

            if (pDispatchingMT != NULL && pDispatchingMT != pMT)
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
// optimizer can assume that it will always be its default value and has
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

void DACNotifyCompilationFinished(MethodDesc *methodDesc, PCODE pCode)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
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

        // Are we listed?
        USHORT jnt = jn.Requested((TADDR) modulePtr, t);
        if (jnt & CLRDATA_METHNOTIFY_GENERATED)
        {
            // If so, throw an exception!
            DACNotify::DoJITNotification(methodDesc, (TADDR)pCode);
        }
    }
}

#ifdef _MSC_VER
#pragma optimize("", on)
#endif
// </TODO>

PCODE MethodDesc::PrepareInitialCode(CallerGCMode callerGCMode)
{
    STANDARD_VM_CONTRACT;
    PrepareCodeConfig config(NativeCodeVersion(this), TRUE, TRUE);
    config.SetCallerGCMode(callerGCMode);
    return PrepareCode(&config);
}

PCODE MethodDesc::PrepareCode(PrepareCodeConfig* pConfig)
{
    STANDARD_VM_CONTRACT;

    // If other kinds of code need multi-versioning we could add more cases here,
    // but for now generation of all other code/stubs occurs in other code paths
    _ASSERTE(IsIL() || IsNoMetadata());
    PCODE pCode = PrepareILBasedCode(pConfig);

#if defined(FEATURE_GDBJIT) && defined(TARGET_UNIX)
    NotifyGdb::MethodPrepared(this);
#endif

    return pCode;
}

bool MayUsePrecompiledILStub()
{
    if (g_pConfig->InteropValidatePinnedObjects())
        return false;

    if (CORProfilerTrackTransitions())
        return false;

    if (g_pConfig->InteropLogArguments())
        return false;

    return true;
}

PCODE MethodDesc::PrepareILBasedCode(PrepareCodeConfig* pConfig)
{
    STANDARD_VM_CONTRACT;
    PCODE pCode = NULL;

    bool shouldTier = false;
#if defined(FEATURE_TIERED_COMPILATION)
    shouldTier = pConfig->GetMethodDesc()->IsEligibleForTieredCompilation();
    // If the method is eligible for tiering but is being
    // called from a Preemptive GC Mode thread or the method
    // has the UnmanagedCallersOnlyAttribute then the Tiered Compilation
    // should be disabled.
    if (shouldTier
        && (pConfig->GetCallerGCMode() == CallerGCMode::Preemptive
            || (pConfig->GetCallerGCMode() == CallerGCMode::Unknown
                && HasUnmanagedCallersOnlyAttribute())))
    {
        NativeCodeVersion codeVersion = pConfig->GetCodeVersion();
        if (codeVersion.IsDefaultVersion())
        {
            pConfig->GetMethodDesc()->GetLoaderAllocator()->GetCallCountingManager()->DisableCallCounting(codeVersion);
            _ASSERTE(codeVersion.IsFinalTier());
        }
        else if (!codeVersion.IsFinalTier())
        {
            codeVersion.SetOptimizationTier(NativeCodeVersion::OptimizationTierOptimized);
        }
        pConfig->SetWasTieringDisabledBeforeJitting();
        shouldTier = false;
    }
#endif // FEATURE_TIERED_COMPILATION
    NativeCodeVersion nativeCodeVersion = pConfig->GetCodeVersion();
    if (shouldTier && !nativeCodeVersion.IsDefaultVersion())
    {
        CodeVersionManager::LockHolder codeVersioningLockHolder;
        if (pConfig->GetCodeVersion().GetILCodeVersion().IsDeoptimized())
        {
            shouldTier = false;
        }
    }

    if (pConfig->MayUsePrecompiledCode())
    {
#ifdef FEATURE_READYTORUN
        if (IsDynamicMethod() && GetLoaderModule()->IsSystem() && MayUsePrecompiledILStub())
        {
            // Images produced using crossgen2 have non-shareable pinvoke stubs which can't be used with the IL
            // stubs that the runtime generates (they take no secret parameter, and each pinvoke has a separate code)
            if (GetModule()->IsReadyToRun() && !GetModule()->GetReadyToRunInfo()->HasNonShareablePInvokeStubs())
            {
                DynamicMethodDesc* stubMethodDesc = this->AsDynamicMethodDesc();
                if (stubMethodDesc->IsILStub() && stubMethodDesc->IsPInvokeStub())
                {
                    MethodDesc* pTargetMD = stubMethodDesc->GetILStubResolver()->GetStubTargetMethodDesc();
                    if (pTargetMD != NULL)
                    {
                        pCode = pTargetMD->GetPrecompiledR2RCode(pConfig);
                        if (pCode != NULL)
                        {
                            LOG_USING_R2R_CODE(this);
                            pConfig->SetNativeCode(pCode, &pCode);
                        }
                    }
                }
            }
        }
#endif // FEATURE_READYTORUN

        if (pCode == NULL)
        {
            pCode = GetPrecompiledCode(pConfig, shouldTier);
        }

#ifdef FEATURE_PERFMAP
        if (pCode != NULL)
            PerfMap::LogPreCompiledMethod(this, pCode);
#endif
    }

    if (pConfig->IsForMulticoreJit() && pCode == NULL && pConfig->ReadyToRunRejectedPrecompiledCode())
    {
        // Was unable to load code from r2r image in mcj thread, don't try to jit it, this method will be loaded later
        return NULL;
    }

    if (pCode == NULL)
    {
        LOG((LF_CLASSLOADER, LL_INFO1000000,
            "    In PrepareILBasedCode, calling JitCompileCode\n"));
        pCode = JitCompileCode(pConfig);
    }
    else
    {
        DACNotifyCompilationFinished(this, pCode);
    }

    return pCode;
}

PCODE MethodDesc::GetPrecompiledCode(PrepareCodeConfig* pConfig, bool shouldTier)
{
    STANDARD_VM_CONTRACT;
    PCODE pCode = NULL;

#ifdef FEATURE_READYTORUN
    pCode = GetPrecompiledR2RCode(pConfig);
    if (pCode != NULL)
    {
        LOG_USING_R2R_CODE(this);

#ifdef FEATURE_TIERED_COMPILATION
        // Finalize the optimization tier before SetNativeCode() is called
        bool shouldCountCalls = shouldTier && pConfig->FinalizeOptimizationTierForTier0Load();
#endif

        if (pConfig->SetNativeCode(pCode, &pCode))
        {
#ifdef FEATURE_CODE_VERSIONING
            pConfig->SetGeneratedOrLoadedNewCode();
#endif
#ifdef FEATURE_TIERED_COMPILATION
            if (shouldCountCalls)
            {
                _ASSERTE(!pConfig->GetCodeVersion().IsFinalTier());
                pConfig->SetShouldCountCalls();
            }
#endif

#ifdef FEATURE_MULTICOREJIT
            // Multi-core JIT is only applicable to the default code version. A method is recorded in the profile only when
            // SetNativeCode() above succeeds to avoid recording duplicates in the multi-core JIT profile. Successful loads
            // of R2R code are also recorded.
            if (pConfig->NeedsMulticoreJitNotification())
            {
                _ASSERTE(pConfig->GetCodeVersion().IsDefaultVersion());
                _ASSERTE(!pConfig->IsForMulticoreJit());

                MulticoreJitManager & mcJitManager = GetAppDomain()->GetMulticoreJitManager();
                if (mcJitManager.IsRecorderActive())
                {
                    if (MulticoreJitManager::IsMethodSupported(this))
                    {
                        mcJitManager.RecordMethodJitOrLoad(this);
                    }
                }
            }
#endif
        }
    }
#endif // FEATURE_READYTORUN

    return pCode;
}

PCODE MethodDesc::GetPrecompiledR2RCode(PrepareCodeConfig* pConfig)
{
    STANDARD_VM_CONTRACT;

    PCODE pCode = NULL;
#ifdef FEATURE_READYTORUN
    ReadyToRunInfo* pAlreadyExaminedInfos[2] = {NULL, NULL};
    Module * pModule = GetModule();
    if (pModule->IsReadyToRun())
    {
        pAlreadyExaminedInfos[0] = pModule->GetReadyToRunInfo();
        pCode = pAlreadyExaminedInfos[0]->GetEntryPoint(this, pConfig, TRUE /* fFixups */);
    }

    //  Generics may be located in several places
    if (pCode == NULL && HasClassOrMethodInstantiation())
    {
        // Generics have an alternative location that is looked up which is based on the first generic
        // argument that the crossgen2 compiler will consider as requiring the cross module compilation logic to kick in.
        pAlreadyExaminedInfos[1] = ReadyToRunInfo::ComputeAlternateGenericLocationForR2RCode(this);

        if (pAlreadyExaminedInfos[1] != NULL &&  pAlreadyExaminedInfos[1] != pAlreadyExaminedInfos[0])
        {
            pCode = pAlreadyExaminedInfos[1]->GetEntryPoint(this, pConfig, TRUE /* fFixups */);
        }

        if (pCode == NULL)
        {
            // R2R also supports a concept of R2R code that has code for "Unrelated" generics embedded within it
            // A linked list of these are formed as those modules are loaded, and this restricted set of modules
            // is examined for all generic method lookups
            ReadyToRunInfo* pUnrelatedInfo = ReadyToRunInfo::GetUnrelatedR2RModules();
            for (;pUnrelatedInfo != NULL && pCode == NULL; pUnrelatedInfo = pUnrelatedInfo->GetNextUnrelatedR2RModule())
            {
                if (pUnrelatedInfo == pAlreadyExaminedInfos[0]) continue;
                if (pUnrelatedInfo == pAlreadyExaminedInfos[1]) continue;

                pCode = pUnrelatedInfo->GetEntryPoint(this, pConfig, TRUE /* fFixups */);
            }
        }
    }
#endif

    return pCode;
}

PCODE MethodDesc::GetMulticoreJitCode(PrepareCodeConfig* pConfig, bool* pWasTier0)
{
    STANDARD_VM_CONTRACT;
    _ASSERTE(pConfig != NULL);
    _ASSERTE(pConfig->GetMethodDesc() == this);
    _ASSERTE(pWasTier0 != NULL);
    _ASSERTE(!*pWasTier0);

    MulticoreJitCodeInfo codeInfo;
#ifdef FEATURE_MULTICOREJIT
    // Quick check before calling expensive out of line function on this method's domain has code JITted by background thread
    MulticoreJitManager & mcJitManager = GetAppDomain()->GetMulticoreJitManager();
    if (mcJitManager.GetMulticoreJitCodeStorage().GetRemainingMethodCount() > 0)
    {
        if (MulticoreJitManager::IsMethodSupported(this))
        {
            codeInfo = mcJitManager.RequestMethodCode(this); // Query multi-core JIT manager for compiled code
        #ifdef FEATURE_TIERED_COMPILATION
            if (!codeInfo.IsNull())
            {
                if (codeInfo.WasTier0())
                {
                    *pWasTier0 = true;
                }
                if (codeInfo.JitSwitchedToOptimized())
                {
                    pConfig->SetJitSwitchedToOptimized();
                }
            }
        #endif
        }
    }
#endif // FEATURE_MULTICOREJIT

    return codeInfo.GetEntryPoint();
}

// ********************************************************************
//                  README!!
// ********************************************************************

// JitCompileCode is the thread safe way to invoke the JIT compiler
// If multiple threads get in here for the same config, ALL of them
// MUST return the SAME value for pcode.
//
// This function creates a DeadlockAware list of methods being jitted
// which prevents us from trying to JIT the same method more that once.

PCODE MethodDesc::JitCompileCode(PrepareCodeConfig* pConfig)
{
    STANDARD_VM_CONTRACT;

    LOG((LF_JIT, LL_INFO1000000,
        "JitCompileCode(%p, ILStub: %s) for %s::%s\n",
        this,
        IsILStub() ? "true" : "false",
        GetMethodTable()->GetDebugClassName(),
        m_pszDebugMethodName));

#if defined(FEATURE_JIT_PITCHING)
    CheckStacksAndPitch();
#endif

    PCODE pCode = NULL;
    {
        // Enter the global lock which protects the list of all functions being JITd
        JitListLock::LockHolder pJitLock(AppDomain::GetCurrentDomain()->GetJitLock());

        // It is possible that another thread stepped in before we entered the global lock for the first time.
        if ((pCode = pConfig->IsJitCancellationRequested()))
        {
            return pCode;
        }

        NativeCodeVersion version = pConfig->GetCodeVersion();

        const char *description = "jit lock";
        INDEBUG(description = m_pszDebugMethodName;)
        ReleaseHolder<JitListLockEntry> pEntry(JitListLockEntry::Find(
            pJitLock, version, description));

        // We have an entry now, we can release the global lock
        pJitLock.Release();

        // Take the entry lock
        {
            JitListLockEntry::LockHolder pEntryLock(pEntry, FALSE);

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
            if ((pCode = pConfig->IsJitCancellationRequested()))
            {
                return pCode;
            }

            // Multi-core-jitted code is generated for the default code version at the initial optimization tier, and so is only
            // applicable to the default code version
            NativeCodeVersion codeVersion = pConfig->GetCodeVersion();
            if (codeVersion.IsDefaultVersion())
            {
                bool wasTier0 = false;
                pCode = GetMulticoreJitCode(pConfig, &wasTier0);
                if (pCode != NULL)
                {
                #ifdef FEATURE_TIERED_COMPILATION
                    // Finalize the optimization tier before SetNativeCode() is called
                    bool shouldCountCalls = wasTier0 && pConfig->FinalizeOptimizationTierForTier0LoadOrJit();
                #endif

                    if (pConfig->SetNativeCode(pCode, &pCode))
                    {
                    #ifdef FEATURE_CODE_VERSIONING
                        pConfig->SetGeneratedOrLoadedNewCode();
                    #endif
                    #ifdef FEATURE_TIERED_COMPILATION
                        if (shouldCountCalls)
                        {
                            pConfig->SetShouldCountCalls();
                        }
                    #endif
                    }
                    pEntry->m_hrResultCode = S_OK;
                    return pCode;
                }
            }

            return JitCompileCodeLockedEventWrapper(pConfig, pEntryLock);
        }
    }
}

namespace
{
    COR_ILMETHOD_DECODER* GetAndVerifyMetadataILHeader(MethodDesc* pMD, PrepareCodeConfig* pConfig, COR_ILMETHOD_DECODER* pDecoderMemory)
    {
        STANDARD_VM_CONTRACT;
        _ASSERTE(pMD != NULL);
        _ASSERTE(!pMD->IsNoMetadata());
        _ASSERTE(pConfig != NULL);
        _ASSERTE(pDecoderMemory != NULL);

        COR_ILMETHOD_DECODER* pHeader = NULL;
        COR_ILMETHOD* ilHeader = pConfig->GetILHeader();
        if (ilHeader == NULL)
            return NULL;

        COR_ILMETHOD_DECODER::DecoderStatus status = COR_ILMETHOD_DECODER::FORMAT_ERROR;
        {
            // Decoder ctor can AV on a malformed method header
            AVInRuntimeImplOkayHolder AVOkay;
            pHeader = new (pDecoderMemory) COR_ILMETHOD_DECODER(ilHeader, pMD->GetMDImport(), &status);
        }

        if (status == COR_ILMETHOD_DECODER::FORMAT_ERROR)
            COMPlusThrowHR(COR_E_BADIMAGEFORMAT, BFA_BAD_IL);

        return pHeader;
    }

    COR_ILMETHOD_DECODER* GetAndVerifyILHeader(MethodDesc* pMD, PrepareCodeConfig* pConfig, COR_ILMETHOD_DECODER* pIlDecoderMemory)
    {
        STANDARD_VM_CONTRACT;
        _ASSERTE(pMD != NULL);
        if (pMD->IsIL())
        {
            return GetAndVerifyMetadataILHeader(pMD, pConfig, pIlDecoderMemory);
        }
        else if (pMD->IsILStub())
        {
            ILStubResolver* pResolver = pMD->AsDynamicMethodDesc()->GetILStubResolver();
            return pResolver->GetILHeader();
        }

        _ASSERTE(pMD->IsNoMetadata());
        return NULL;
    }
}

PCODE MethodDesc::JitCompileCodeLockedEventWrapper(PrepareCodeConfig* pConfig, JitListLockEntry* pEntry)
{
    STANDARD_VM_CONTRACT;

    PCODE pCode = NULL;
    ULONG sizeOfCode = 0;

#ifdef PROFILING_SUPPORTED
    {
        BEGIN_PROFILER_CALLBACK(CORProfilerTrackJITInfo());
        // For methods with non-zero rejit id we send ReJITCompilationStarted, otherwise
        // JITCompilationStarted. It isn't clear if this is the ideal policy for these
        // notifications yet.
        NativeCodeVersion nativeCodeVersion = pConfig->GetCodeVersion();
        ReJITID rejitId = nativeCodeVersion.GetILCodeVersionId();
        if (rejitId != 0)
        {
            _ASSERTE(!nativeCodeVersion.IsDefaultVersion());
            (&g_profControlBlock)->ReJITCompilationStarted((FunctionID)this,
                rejitId,
                TRUE);
        }
        else
            // If profiling, need to give a chance for a tool to examine and modify
            // the IL before it gets to the JIT.  This allows one to add probe calls for
            // things like code coverage, performance, or whatever.
        {
            if (!IsNoMetadata())
            {
                (&g_profControlBlock)->JITCompilationStarted((FunctionID)this, TRUE);

            }
            else
            {
                unsigned int ilSize, unused;
                CorInfoOptions corOptions;
                LPCBYTE ilHeaderPointer = this->AsDynamicMethodDesc()->GetResolver()->GetCodeInfo(&ilSize, &unused, &corOptions, &unused);

                (&g_profControlBlock)->DynamicMethodJITCompilationStarted((FunctionID)this, TRUE, ilHeaderPointer, ilSize);
            }

            if (nativeCodeVersion.IsDefaultVersion())
            {
                pConfig->SetProfilerMayHaveActivatedNonDefaultCodeVersion();
            }
        }
        END_PROFILER_CALLBACK();
    }
#endif // PROFILING_SUPPORTED

    // The profiler may have changed the code on the callback.  Need to
    // pick up the new code.
    //
    // (don't want this for OSR, need to see how it works)
    COR_ILMETHOD_DECODER ilDecoderTemp;
    COR_ILMETHOD_DECODER* pilHeader = GetAndVerifyILHeader(this, pConfig, &ilDecoderTemp);

    if (!ETW_TRACING_CATEGORY_ENABLED(MICROSOFT_WINDOWS_DOTNETRUNTIME_PROVIDER_DOTNET_Context,
        TRACE_LEVEL_VERBOSE,
        CLR_JIT_KEYWORD))
    {
        pCode = JitCompileCodeLocked(pConfig, pilHeader, pEntry, &sizeOfCode);
    }
    else
    {
        SString namespaceOrClassName, methodName, methodSignature;

        // Methods that may be interpreted defer this notification until it is certain
        // we are jitting and not interpreting in CompileMethodWithEtwWrapper.
        // Some further refactoring could consolidate the notification to always
        // occur at the point the interpreter does it, but it might even better
        // to fix the issues that cause us to avoid generating jit notifications
        // for interpreted methods in the first place. The interpreter does generate
        // a small stub of native code but no native-IL mapping.
#ifndef FEATURE_INTERPRETER
        ETW::MethodLog::MethodJitting(this,
            pilHeader,
            &namespaceOrClassName,
            &methodName,
            &methodSignature);
#endif

        pCode = JitCompileCodeLocked(pConfig, pilHeader, pEntry, &sizeOfCode);

        // Interpretted methods skip this notification
#ifdef FEATURE_INTERPRETER
        if (Interpreter::InterpretationStubToMethodInfo(pCode) == NULL)
#endif
        {
            // Fire an ETW event to mark the end of JIT'ing
            ETW::MethodLog::MethodJitted(this,
                &namespaceOrClassName,
                &methodName,
                &methodSignature,
                pCode,
                pConfig);
        }

    }

#ifdef PROFILING_SUPPORTED
    {
        BEGIN_PROFILER_CALLBACK(CORProfilerTrackJITInfo());
        // For methods with non-zero rejit id we send ReJITCompilationFinished, otherwise
        // JITCompilationFinished. It isn't clear if this is the ideal policy for these
        // notifications yet.
        NativeCodeVersion nativeCodeVersion = pConfig->GetCodeVersion();
        ReJITID rejitId = nativeCodeVersion.GetILCodeVersionId();
        if (rejitId != 0)
        {
            _ASSERTE(!nativeCodeVersion.IsDefaultVersion());
            (&g_profControlBlock)->ReJITCompilationFinished((FunctionID)this,
                rejitId,
                S_OK,
                TRUE);
        }
        else
            // Notify the profiler that JIT completed.
            // Must do this after the address has been set.
            // @ToDo: Why must we set the address before notifying the profiler ??
        {
            if (!IsNoMetadata())
            {
                (&g_profControlBlock)->
                    JITCompilationFinished((FunctionID)this,
                        pEntry->m_hrResultCode,
                        TRUE);
            }
            else
            {
                (&g_profControlBlock)->DynamicMethodJITCompilationFinished((FunctionID)this, pEntry->m_hrResultCode, TRUE);
            }

            if (nativeCodeVersion.IsDefaultVersion())
            {
                pConfig->SetProfilerMayHaveActivatedNonDefaultCodeVersion();
            }
        }
        END_PROFILER_CALLBACK();
    }
#endif // PROFILING_SUPPORTED

#ifdef FEATURE_INTERPRETER
    bool isJittedMethod = (Interpreter::InterpretationStubToMethodInfo(pCode) == NULL);
#endif

    // Interpretted methods skip this notification
#ifdef FEATURE_INTERPRETER
    if (isJittedMethod)
#endif
    {
#ifdef FEATURE_PERFMAP
        // Save the JIT'd method information so that perf can resolve JIT'd call frames.
        PerfMap::LogJITCompiledMethod(this, pCode, sizeOfCode, pConfig);
#endif
    }

#ifdef FEATURE_INTERPRETER
    if (isJittedMethod)
#endif
    {
        // The notification will only occur if someone has registered for this method.
        DACNotifyCompilationFinished(this, pCode);
    }

    return pCode;
}

PCODE MethodDesc::JitCompileCodeLocked(PrepareCodeConfig* pConfig, COR_ILMETHOD_DECODER* pilHeader, JitListLockEntry* pEntry, ULONG* pSizeOfCode)
{
    STANDARD_VM_CONTRACT;

    PCODE pCode = NULL;
    CORJIT_FLAGS jitFlags;
    PCODE pOtherCode = NULL;

    EX_TRY
    {
        Thread::CurrentPrepareCodeConfigHolder threadPrepareCodeConfigHolder(GetThread(), pConfig);

        pCode = UnsafeJitFunction(pConfig, pilHeader, &jitFlags, pSizeOfCode);
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

        if (!(pOtherCode = pConfig->IsJitCancellationRequested()))
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
        return pOtherCode;
    }

    _ASSERTE(pCode != NULL);

#ifdef HAVE_GCCOVER
    // Instrument for coverage before trying to publish this version
    // of the code as the native code, to avoid other threads seeing
    // partially instrumented methods.
    if (GCStress<cfg_instr_jit>::IsEnabled())
    {
        // Do the instrumentation and publish atomically, so that the
        // instrumentation data always matches the published code.
        CrstHolder gcCoverLock(&m_GCCoverCrst);

        // Make sure no other thread has stepped in before us.
        if ((pOtherCode = pConfig->IsJitCancellationRequested()))
        {
            return pOtherCode;
        }

        SetupGcCoverage(pConfig->GetCodeVersion(), (BYTE*)pCode);
    }
#endif // HAVE_GCCOVER

#ifdef FEATURE_TIERED_COMPILATION
    // Finalize the optimization tier before SetNativeCode() is called
    bool shouldCountCalls = jitFlags.IsSet(CORJIT_FLAGS::CORJIT_FLAG_TIER0) && pConfig->FinalizeOptimizationTierForTier0LoadOrJit();
#endif

    // Aside from rejit, performing a SetNativeCodeInterlocked at this point
    // generally ensures that there is only one winning version of the native
    // code. This also avoid races with profiler overriding ngened code (see
    // matching SetNativeCodeInterlocked done after
    // JITCachedFunctionSearchStarted)
    if (!pConfig->SetNativeCode(pCode, &pOtherCode))
    {
#ifdef HAVE_GCCOVER
        // When GCStress is enabled, this thread should always win the publishing race
        // since we're under a lock.
        _ASSERTE(!GCStress<cfg_instr_jit>::IsEnabled() || !"GC Cover native code publish failed");
#endif

        // Another thread beat us to publishing its copy of the JITted code.
        return pOtherCode;
    }

#ifdef FEATURE_CODE_VERSIONING
    pConfig->SetGeneratedOrLoadedNewCode();
#endif
#ifdef FEATURE_TIERED_COMPILATION
    if (shouldCountCalls)
    {
        pConfig->SetShouldCountCalls();
    }
#endif

#if defined(FEATURE_JIT_PITCHING)
    SavePitchingCandidate(this, *pSizeOfCode);
#endif

#ifdef FEATURE_MULTICOREJIT
    // Multi-core JIT is only applicable to the default code version. A method is recorded in the profile only when
    // SetNativeCode() above succeeds to avoid recording duplicates in the multi-core JIT profile.
    if (pConfig->NeedsMulticoreJitNotification())
    {
        _ASSERTE(pConfig->GetCodeVersion().IsDefaultVersion());
        _ASSERTE(!pConfig->IsForMulticoreJit());

        MulticoreJitManager & mcJitManager = GetAppDomain()->GetMulticoreJitManager();
        if (mcJitManager.IsRecorderActive())
        {
            if (MulticoreJitManager::IsMethodSupported(this))
            {
                mcJitManager.RecordMethodJitOrLoad(this); // Tell multi-core JIT manager to record method on successful JITting
            }
        }
    }
#endif

    // We succeeded in jitting the code, and our jitted code is the one that's going to run now.
    pEntry->m_hrResultCode = S_OK;

    return pCode;
}

namespace
{
    enum class UnsafeAccessorKind
    {
        Constructor, // call instance constructor (`newobj` in IL)
        Method, // call instance method (`callvirt` in IL)
        StaticMethod, // call static method (`call` in IL)
        Field, // address of instance field (`ldflda` in IL)
        StaticField // address of static field (`ldsflda` in IL)
    };

    bool TryParseUnsafeAccessorAttribute(
        MethodDesc* pMD,
        CustomAttributeParser& ca,
        UnsafeAccessorKind& kind,
        SString& name)
    {
        STANDARD_VM_CONTRACT;
        _ASSERTE(pMD != NULL);

        // Get the kind of accessor
        CaArg args[1];
        args[0].InitEnum(SERIALIZATION_TYPE_I4, 0);
        if (FAILED(::ParseKnownCaArgs(ca, args, ARRAY_SIZE(args))))
            return false;

        kind = (UnsafeAccessorKind)args[0].val.i4;

        // Check the name of the target to access. This is the name we
        // use to look up the intended token in metadata.
        CaNamedArg namedArgs[1];
        CaType namedArgTypes[1];
        namedArgTypes[0].Init(SERIALIZATION_TYPE_STRING);
        namedArgs[0].Init("Name", SERIALIZATION_TYPE_PROPERTY, namedArgTypes[0], NULL);
        if (FAILED(::ParseKnownCaNamedArgs(ca, namedArgs, ARRAY_SIZE(namedArgs))))
            return false;

        // If the Name isn't defined, then use the name of the method.
        if (namedArgs[0].val.type.tag == SERIALIZATION_TYPE_UNDEFINED)
        {
            // The Constructor case has an implied value provided by
            // the runtime. We are going to enforce this during consumption
            // so we avoid the setting of the value. We validate the name
            // as empty at the use site.
            if (kind != UnsafeAccessorKind::Constructor)
                name.SetUTF8(pMD->GetName());
        }
        else
        {
            const CaValue& val = namedArgs[0].val;
            name.SetUTF8(val.str.pStr, val.str.cbStr);
        }

        return true;
    }

    struct GenerationContext final
    {
        GenerationContext(UnsafeAccessorKind kind, MethodDesc* pMD)
            : Kind{ kind }
            , Declaration{ pMD }
            , DeclarationSig{ pMD }
            , TargetTypeSig{}
            , TargetType{}
            , IsTargetStatic{ false }
            , TargetMethod{}
            , TargetField{}
        { }

        UnsafeAccessorKind Kind;
        MethodDesc* Declaration;
        MetaSig DeclarationSig;
        SigPointer TargetTypeSig;
        TypeHandle TargetType;
        bool IsTargetStatic;
        MethodDesc* TargetMethod;
        FieldDesc* TargetField;
    };

    TypeHandle ValidateTargetType(TypeHandle targetTypeMaybe)
    {
        TypeHandle targetType = targetTypeMaybe.IsByRef()
            ? targetTypeMaybe.GetTypeParam()
            : targetTypeMaybe;

        // Due to how some types degrade, we block on parameterized
        // types that are represented as TypeDesc. For example ref or pointer.
        if (targetType.IsTypeDesc())
            ThrowHR(COR_E_BADIMAGEFORMAT, BFA_INVALID_UNSAFEACCESSOR);

        return targetType;
    }

    bool DoesMethodMatchUnsafeAccessorDeclaration(
        GenerationContext& cxt,
        MethodDesc* method,
        const Substitution* pMethodSubst,
        MetaSig::CompareState& state)
    {
        STANDARD_VM_CONTRACT;
        _ASSERTE(method != NULL);

        PCCOR_SIGNATURE pSig1;
        DWORD cSig1;
        cxt.Declaration->GetSig(&pSig1, &cSig1);
        PCCOR_SIGNATURE pEndSig1 = pSig1 + cSig1;
        ModuleBase* pModule1 = cxt.Declaration->GetModule();
        const Substitution* pSubst1 = NULL;

        PCCOR_SIGNATURE pSig2;
        DWORD cSig2;
        method->GetSig(&pSig2, &cSig2);
        PCCOR_SIGNATURE pEndSig2 = pSig2 + cSig2;
        ModuleBase* pModule2 = method->GetModule();
        const Substitution* pSubst2 = pMethodSubst;

        //
        // Parsing the signature follows details defined in ECMA-335 - II.23.2.1
        //

        // Validate calling convention
        if ((*pSig1 & IMAGE_CEE_CS_CALLCONV_MASK) != (*pSig2 & IMAGE_CEE_CS_CALLCONV_MASK))
        {
            return false;
        }

        BYTE callConvDecl = *pSig1;
        BYTE callConvMethod = *pSig2;
        pSig1++;
        pSig2++;

        // Handle generic param count
        DWORD declGenericCount = 0;
        DWORD methodGenericCount = 0;
        if (callConvDecl & IMAGE_CEE_CS_CALLCONV_GENERIC)
            IfFailThrow(CorSigUncompressData_EndPtr(pSig1, pEndSig1, &declGenericCount));
        if (callConvMethod & IMAGE_CEE_CS_CALLCONV_GENERIC)
            IfFailThrow(CorSigUncompressData_EndPtr(pSig2, pEndSig2, &methodGenericCount));

        DWORD declArgCount;
        DWORD methodArgCount;
        IfFailThrow(CorSigUncompressData_EndPtr(pSig1, pEndSig1, &declArgCount));
        IfFailThrow(CorSigUncompressData_EndPtr(pSig2, pEndSig2, &methodArgCount));

        // Validate argument count
        if (cxt.Kind == UnsafeAccessorKind::Constructor)
        {
            // Declarations for constructor scenarios have
            // matching argument counts with the target.
            if (declArgCount != methodArgCount)
                return false;
        }
        else
        {
            // Declarations of non-constructor scenarios have
            // an additional argument to indicate target type
            // and to pass an instance for non-static methods.
            if (declArgCount != (methodArgCount + 1))
                return false;
        }

        // Validate return and argument types
        for (DWORD i = 0; i <= methodArgCount; ++i)
        {
            if (i == 0 && cxt.Kind == UnsafeAccessorKind::Constructor)
            {
                // Skip return value (index 0) validation on constructor
                // accessor declarations.
                SigPointer ptr1(pSig1, (DWORD)(pEndSig1 - pSig1));
                IfFailThrow(ptr1.SkipExactlyOne());
                pSig1 = ptr1.GetPtr();

                CorElementType typ;
                SigPointer ptr2(pSig2, (DWORD)(pEndSig2 - pSig2));
                IfFailThrow(ptr2.GetElemType(&typ));
                pSig2 = ptr2.GetPtr();

                // Validate the return value for target constructor
                // candidate is void.
                if (typ != ELEMENT_TYPE_VOID)
                    return false;

                continue;
            }
            else if (i == 1 && cxt.Kind != UnsafeAccessorKind::Constructor)
            {
                // Skip over first argument (index 1) on non-constructor accessors.
                // See argument count validation above.
                SigPointer ptr1(pSig1, (DWORD)(pEndSig1 - pSig1));
                IfFailThrow(ptr1.SkipExactlyOne());
                pSig1 = ptr1.GetPtr();
            }

            // Compare the types
            if (FALSE == MetaSig::CompareElementType(
                pSig1,
                pSig2,
                pEndSig1,
                pEndSig2,
                pModule1,
                pModule2,
                pSubst1,
                pSubst2,
                &state))
            {
                return false;
            }
        }

        return true;
    }

    bool TrySetTargetMethod(
        GenerationContext& cxt,
        LPCUTF8 methodName,
        bool ignoreCustomModifiers = true)
    {
        STANDARD_VM_CONTRACT;
        _ASSERTE(methodName != NULL);
        _ASSERTE(cxt.Kind == UnsafeAccessorKind::Constructor
                || cxt.Kind == UnsafeAccessorKind::Method
                || cxt.Kind == UnsafeAccessorKind::StaticMethod);

        TypeHandle targetType = cxt.TargetType;
        _ASSERTE(!targetType.IsTypeDesc());

        // We do not support the Canon type as a valid target.
        if (targetType.AsMethodTable() == g_pCanonMethodTableClass)
            ThrowHR(COR_E_BADIMAGEFORMAT, BFA_INVALID_UNSAFEACCESSOR);

        MethodDesc* targetMaybe = NULL;
        Substitution* pLookupSubst = NULL;

        // Build up a Substitution to use when looking up methods involving generics.
        Substitution substitution;
        SigBuilder sigBuilder;
        DWORD targetGenericParamCount = targetType.AsMethodTable()->GetNumGenericArgs();
        if (targetGenericParamCount > 0)
        {
            // Create a temporary signature that translate VARs to MVARs.
            for (DWORD i = 0; i < targetGenericParamCount; ++i)
            {
                sigBuilder.AppendElementType(ELEMENT_TYPE_MVAR);
                sigBuilder.AppendData(i); // Represents the generic parameter index - II.23.2.12
            }

            DWORD tmpSigLen;
            PVOID tmpSigRaw = sigBuilder.GetSignature(&tmpSigLen);

            SigPointer tmpSig{ (PCCOR_SIGNATURE)tmpSigRaw, tmpSigLen };
            substitution = Substitution{ cxt.Declaration->GetModule(), tmpSig, NULL };
            pLookupSubst = &substitution;
        }

        // Following a similar iteration pattern found in MemberLoader::FindMethod().
        // However, we are only operating on the current type not walking the type hierarchy.
        MethodTable::IntroducedMethodIterator iter(targetType.AsMethodTable());
        for (; iter.IsValid(); iter.Next())
        {
            MethodDesc* curr = iter.GetMethodDesc();

            // Check the target and current method match static/instance state.
            if (cxt.IsTargetStatic != (!!curr->IsStatic()))
                continue;

            // Check for matching name
            if (strcmp(methodName, curr->GetNameThrowing()) != 0)
                continue;

            // Check signature
            TokenPairList list { nullptr };
            MetaSig::CompareState state{ &list };
            state.IgnoreCustomModifiers = ignoreCustomModifiers;
            if (!DoesMethodMatchUnsafeAccessorDeclaration(cxt, curr, pLookupSubst, state))
                continue;

            // Check if there is some ambiguity.
            if (targetMaybe != NULL)
            {
                if (ignoreCustomModifiers)
                {
                    // We have detected ambiguity when ignoring custom modifiers.
                    // Start over, but look for a match requiring custom modifiers
                    // to match precisely.
                    if (TrySetTargetMethod(cxt, methodName, false /* ignoreCustomModifiers */))
                        return true;
                }
                COMPlusThrow(kAmbiguousMatchException, W("Arg_AmbiguousMatchException_UnsafeAccessor"));
            }
            targetMaybe = curr;
        }

        cxt.TargetMethod = targetMaybe;
        return cxt.TargetMethod != NULL;
    }

    bool TrySetTargetField(
        GenerationContext& cxt,
        LPCUTF8 fieldName,
        TypeHandle fieldType)
    {
        STANDARD_VM_CONTRACT;
        _ASSERTE(fieldName != NULL);
        _ASSERTE(cxt.Kind == UnsafeAccessorKind::Field
                || cxt.Kind == UnsafeAccessorKind::StaticField);

        TypeHandle targetType = cxt.TargetType;
        _ASSERTE(!targetType.IsTypeDesc());

        MethodTable* pMT = targetType.AsMethodTable();

        // We do not support the Canon type as a valid target.
        if (pMT == g_pCanonMethodTableClass)
            ThrowHR(COR_E_BADIMAGEFORMAT, BFA_INVALID_UNSAFEACCESSOR);

        CorElementType elemType = fieldType.GetSignatureCorElementType();
        ApproxFieldDescIterator fdIterator(
            pMT,
            (cxt.IsTargetStatic ? ApproxFieldDescIterator::STATIC_FIELDS : ApproxFieldDescIterator::INSTANCE_FIELDS));
        PTR_FieldDesc pField;
        while ((pField = fdIterator.Next()) != NULL)
        {
            // Validate the name and target type match.
            if (strcmp(fieldName, pField->GetName()) != 0)
                continue;

            // We check if the possible field is class or valuetype
            // since generic fields need resolution.
            CorElementType fieldTypeMaybe = pField->GetFieldType();
            if (fieldTypeMaybe == ELEMENT_TYPE_CLASS
                || fieldTypeMaybe == ELEMENT_TYPE_VALUETYPE)
            {
                if (fieldType != pField->LookupFieldTypeHandle())
                    continue;
            }
            else
            {
                if (elemType != fieldTypeMaybe)
                    continue;
            }

            if (cxt.Kind == UnsafeAccessorKind::StaticField && pMT->HasGenericsStaticsInfo())
            {
                // Statics require the exact typed field as opposed to the canonically
                // typed field. In order to do that we lookup the current index of the
                // approx field and then use that index to get the precise field from
                // the approx field.
                MethodTable* pFieldMT = pField->GetApproxEnclosingMethodTable();
                DWORD index = pFieldMT->GetIndexForFieldDesc(pField);
                pField = pMT->GetFieldDescByIndex(index);
            }

            cxt.TargetField = pField;
            return true;
        }
        return false;
    }

    void GenerateAccessor(
        GenerationContext& cxt,
        DynamicResolver** resolver,
        COR_ILMETHOD_DECODER** methodILDecoder)
    {
        STANDARD_VM_CONTRACT;

        NewHolder<ILStubResolver> ilResolver = new ILStubResolver();

        // Initialize the resolver target details.
        ilResolver->SetStubMethodDesc(cxt.Declaration);
        ilResolver->SetStubTargetMethodDesc(cxt.TargetMethod);

        SigTypeContext genericContext;
        if (cxt.Declaration->GetClassification() == mcInstantiated)
            SigTypeContext::InitTypeContext(cxt.Declaration, &genericContext);

        ILStubLinker sl(
            cxt.Declaration->GetModule(),
            cxt.Declaration->GetSignature(),
            &genericContext,
            cxt.TargetMethod,
            (ILStubLinkerFlags)ILSTUB_LINKER_FLAG_NONE);

        ILCodeStream* pCode = sl.NewCodeStream(ILStubLinker::kDispatch);

        // Load stub arguments.
        // When the target is static, the first argument is only
        // used to look up the target member to access and ignored
        // during dispatch.
        UINT beginIndex = cxt.IsTargetStatic ? 1 : 0;
        UINT stubArgCount = cxt.DeclarationSig.NumFixedArgs();
        for (UINT i = beginIndex; i < stubArgCount; ++i)
            pCode->EmitLDARG(i);

        // Provide access to the target member
        UINT targetArgCount = stubArgCount - beginIndex;
        UINT targetRetCount = cxt.DeclarationSig.IsReturnTypeVoid() ? 0 : 1;
        switch (cxt.Kind)
        {
        case UnsafeAccessorKind::Constructor:
        {
            _ASSERTE(cxt.TargetMethod != NULL);
            mdToken target;
            if (!cxt.TargetType.HasInstantiation())
            {
                target = pCode->GetToken(cxt.TargetMethod);
            }
            else
            {
                PCCOR_SIGNATURE sig;
                uint32_t sigLen;
                cxt.TargetTypeSig.GetSignature(&sig, &sigLen);
                mdToken targetTypeSigToken = pCode->GetSigToken(sig, sigLen);
                target = pCode->GetToken(cxt.TargetMethod, targetTypeSigToken);
            }
            pCode->EmitNEWOBJ(target, targetArgCount);
            break;
        }
        case UnsafeAccessorKind::Method:
        case UnsafeAccessorKind::StaticMethod:
        {
            _ASSERTE(cxt.TargetMethod != NULL);
            mdToken target;
            if (!cxt.TargetMethod->HasClassOrMethodInstantiation())
            {
                target = pCode->GetToken(cxt.TargetMethod);
            }
            else
            {
                DWORD targetGenericCount = cxt.TargetMethod->GetNumGenericMethodArgs();

                mdToken methodSpecSigToken = mdTokenNil;
                SigBuilder sigBuilder;
                uint32_t sigLen;
                PCCOR_SIGNATURE sig;
                if (targetGenericCount != 0)
                {
                    // Create signature for the MethodSpec. See ECMA-335 - II.23.2.15
                    sigBuilder.AppendByte(IMAGE_CEE_CS_CALLCONV_GENERICINST);
                    sigBuilder.AppendData(targetGenericCount);
                    for (DWORD i = 0; i < targetGenericCount; ++i)
                    {
                        sigBuilder.AppendElementType(ELEMENT_TYPE_MVAR);
                        sigBuilder.AppendData(i);
                    }
                    sigLen;
                    sig = (PCCOR_SIGNATURE)sigBuilder.GetSignature((DWORD*)&sigLen);
                    methodSpecSigToken = pCode->GetSigToken(sig, sigLen);
                }

                cxt.TargetTypeSig.GetSignature(&sig, &sigLen);
                mdToken targetTypeSigToken = pCode->GetSigToken(sig, sigLen);

                if (methodSpecSigToken == mdTokenNil)
                {
                    // Create a MemberRef
                    target = pCode->GetToken(cxt.TargetMethod, targetTypeSigToken);
                    _ASSERTE(TypeFromToken(target) == mdtMemberRef);
                }
                else
                {
                    // Use the method declaration Instantiation to find the instantiated MethodDesc target.
                    Instantiation methodInst = cxt.Declaration->GetMethodInstantiation();
                    MethodDesc* instantiatedTarget = MethodDesc::FindOrCreateAssociatedMethodDesc(cxt.TargetMethod, cxt.TargetType.GetMethodTable(), FALSE, methodInst, TRUE);

                    // Create a MethodSpec
                    target = pCode->GetToken(instantiatedTarget, targetTypeSigToken, methodSpecSigToken);
                    _ASSERTE(TypeFromToken(target) == mdtMethodSpec);
                }
            }

            if (cxt.Kind == UnsafeAccessorKind::StaticMethod)
            {
                pCode->EmitCALL(target, targetArgCount, targetRetCount);
            }
            else
            {
                pCode->EmitCALLVIRT(target, targetArgCount, targetRetCount);
            }
            break;
        }
        case UnsafeAccessorKind::Field:
        {
            _ASSERTE(cxt.TargetField != NULL);
            mdToken target;
            if (!cxt.TargetType.HasInstantiation())
            {
                target = pCode->GetToken(cxt.TargetField);
            }
            else
            {
                // See the static field case for why this can be mdTokenNil.
                mdToken targetTypeSigToken = mdTokenNil;
                target = pCode->GetToken(cxt.TargetField, targetTypeSigToken);
            }
            pCode->EmitLDFLDA(target);
            break;
        }
        case UnsafeAccessorKind::StaticField:
            _ASSERTE(cxt.TargetField != NULL);
            mdToken target;
            if (!cxt.TargetType.HasInstantiation())
            {
                target = pCode->GetToken(cxt.TargetField);
            }
            else
            {
                // For accessing a generic instance field, every instantiation will
                // be at the same offset, and be the same size, with the same GC layout,
                // as long as the generic is canonically equivalent. However, for static fields,
                // while the offset, size and GC layout remain the same, the address of the
                // field is different, and needs to be found by a lookup of some form. The
                // current form of lookup means the exact type isn't with a type signature.
                PCCOR_SIGNATURE sig;
                uint32_t sigLen;
                cxt.TargetTypeSig.GetSignature(&sig, &sigLen);
                mdToken targetTypeSigToken = pCode->GetSigToken(sig, sigLen);
                target = pCode->GetToken(cxt.TargetField, targetTypeSigToken);
            }
            pCode->EmitLDSFLDA(target);
            break;
        default:
            _ASSERTE(!"Unknown UnsafeAccessorKind");
        }

        // Return from the generated stub
        pCode->EmitRET();

        // Generate all IL associated data for JIT
        {
            UINT maxStack;
            size_t cbCode = sl.Link(&maxStack);
            DWORD cbSig = sl.GetLocalSigSize();

            COR_ILMETHOD_DECODER* pILHeader = ilResolver->AllocGeneratedIL(cbCode, cbSig, maxStack);
            BYTE* pbBuffer = (BYTE*)pILHeader->Code;
            BYTE* pbLocalSig = (BYTE*)pILHeader->LocalVarSig;
            _ASSERTE(cbSig == pILHeader->cbLocalVarSig);
            sl.GenerateCode(pbBuffer, cbCode);
            sl.GetLocalSig(pbLocalSig, cbSig);

            // Store the token lookup map
            ilResolver->SetTokenLookupMap(sl.GetTokenLookupMap());
            ilResolver->SetJitFlags(CORJIT_FLAGS(CORJIT_FLAGS::CORJIT_FLAG_IL_STUB));

            *resolver = (DynamicResolver*)ilResolver;
            *methodILDecoder = pILHeader;
        }

        ilResolver.SuppressRelease();
    }
}

bool MethodDesc::TryGenerateUnsafeAccessor(DynamicResolver** resolver, COR_ILMETHOD_DECODER** methodILDecoder)
{
    STANDARD_VM_CONTRACT;
    _ASSERTE(resolver != NULL);
    _ASSERTE(methodILDecoder != NULL);
    _ASSERTE(*resolver == NULL && *methodILDecoder == NULL);
    _ASSERTE(IsIL());
    _ASSERTE(GetRVA() == 0);

    // The UnsafeAccessorAttribute is applied to methods with an
    // RVA of 0 (for example, C#'s extern keyword).
    const void* data;
    ULONG dataLen;
    HRESULT hr = GetCustomAttribute(WellKnownAttribute::UnsafeAccessorAttribute, &data, &dataLen);
    if (hr != S_OK)
        return false;

    // UnsafeAccessor must be on a static method
    if (!IsStatic())
        ThrowHR(COR_E_BADIMAGEFORMAT, BFA_INVALID_UNSAFEACCESSOR);

    UnsafeAccessorKind kind;
    SString name;

    CustomAttributeParser ca(data, dataLen);
    if (!TryParseUnsafeAccessorAttribute(this, ca, kind, name))
        ThrowHR(COR_E_BADIMAGEFORMAT, BFA_INVALID_UNSAFEACCESSOR);

    GenerationContext context{ kind, this };

    // Parse the signature to determine the type to use:
    //  * Constructor access - examine the return type
    //  * Instance member access - examine type of first parameter
    //  * Static member access - examine type of first parameter
    TypeHandle retType;
    TypeHandle firstArgType;
    retType = context.DeclarationSig.GetRetTypeHandleThrowing();
    UINT argCount = context.DeclarationSig.NumFixedArgs();
    if (argCount > 0)
    {
        context.DeclarationSig.NextArg();

        // Get the target type signature and resolve to a type handle.
        context.TargetTypeSig = context.DeclarationSig.GetArgProps();
        firstArgType = context.DeclarationSig.GetLastTypeHandleThrowing();
    }

    // Using the kind type, perform the following:
    //  1) Validate the basic type information from the signature.
    //  2) Resolve the name to the appropriate member.
    switch (context.Kind)
    {
    case UnsafeAccessorKind::Constructor:
        // A return type is required for a constructor, otherwise
        // we don't know the type to construct.
        // Types should not be parameterized (that is, byref).
        // The name is defined by the runtime and should be empty.
        if (context.DeclarationSig.IsReturnTypeVoid() || retType.IsByRef() || !name.IsEmpty())
        {
            ThrowHR(COR_E_BADIMAGEFORMAT, BFA_INVALID_UNSAFEACCESSOR);
        }

        // Get the target type signature from the return type.
        context.TargetTypeSig = context.DeclarationSig.GetReturnProps();
        context.TargetType = ValidateTargetType(retType);
        if (!TrySetTargetMethod(context, ".ctor"))
            MemberLoader::ThrowMissingMethodException(context.TargetType.AsMethodTable(), ".ctor");
        break;

    case UnsafeAccessorKind::Method:
    case UnsafeAccessorKind::StaticMethod:
        // Method access requires a target type.
        if (firstArgType.IsNull())
            ThrowHR(COR_E_BADIMAGEFORMAT, BFA_INVALID_UNSAFEACCESSOR);

        // If the non-static method access is for a
        // value type, the instance must be byref.
        if (kind == UnsafeAccessorKind::Method
            && firstArgType.IsValueType()
            && !firstArgType.IsByRef())
        {
            ThrowHR(COR_E_BADIMAGEFORMAT, BFA_INVALID_UNSAFEACCESSOR);
        }

        context.TargetType = ValidateTargetType(firstArgType);
        context.IsTargetStatic = kind == UnsafeAccessorKind::StaticMethod;
        if (!TrySetTargetMethod(context, name.GetUTF8()))
            MemberLoader::ThrowMissingMethodException(context.TargetType.AsMethodTable(), name.GetUTF8());
        break;

    case UnsafeAccessorKind::Field:
    case UnsafeAccessorKind::StaticField:
        // Field access requires a single argument for target type and a return type.
        if (argCount != 1 || firstArgType.IsNull() || context.DeclarationSig.IsReturnTypeVoid())
        {
            ThrowHR(COR_E_BADIMAGEFORMAT, BFA_INVALID_UNSAFEACCESSOR);
        }

        // The return type must be byref.
        // If the non-static field access is for a
        // value type, the instance must be byref.
        if (!retType.IsByRef()
            || (kind == UnsafeAccessorKind::Field
                && firstArgType.IsValueType()
                && !firstArgType.IsByRef()))
        {
            ThrowHR(COR_E_BADIMAGEFORMAT, BFA_INVALID_UNSAFEACCESSOR);
        }

        context.TargetType = ValidateTargetType(firstArgType);
        context.IsTargetStatic = kind == UnsafeAccessorKind::StaticField;
        if (!TrySetTargetField(context, name.GetUTF8(), retType.GetTypeParam()))
            MemberLoader::ThrowMissingFieldException(context.TargetType.AsMethodTable(), name.GetUTF8());
        break;

    default:
        ThrowHR(COR_E_BADIMAGEFORMAT, BFA_INVALID_UNSAFEACCESSOR);
    }

    // Generate the IL for the accessor.
    GenerateAccessor(context, resolver, methodILDecoder);
    return true;
}

PrepareCodeConfig::PrepareCodeConfig() {}

PrepareCodeConfig::PrepareCodeConfig(NativeCodeVersion codeVersion, BOOL needsMulticoreJitNotification, BOOL mayUsePrecompiledCode) :
    m_pMethodDesc(codeVersion.GetMethodDesc()),
    m_nativeCodeVersion(codeVersion),
    m_needsMulticoreJitNotification(needsMulticoreJitNotification),
    m_mayUsePrecompiledCode(mayUsePrecompiledCode),
    m_ProfilerRejectedPrecompiledCode(FALSE),
    m_ReadyToRunRejectedPrecompiledCode(FALSE),
    m_callerGCMode(CallerGCMode::Unknown),
#ifdef FEATURE_MULTICOREJIT
    m_isForMulticoreJit(false),
#endif
#ifdef FEATURE_CODE_VERSIONING
    m_profilerMayHaveActivatedNonDefaultCodeVersion(false),
    m_generatedOrLoadedNewCode(false),
#endif
#ifdef FEATURE_TIERED_COMPILATION
    m_wasTieringDisabledBeforeJitting(false),
    m_shouldCountCalls(false),
#endif
    m_jitSwitchedToMinOpt(false),
#ifdef FEATURE_TIERED_COMPILATION
    m_jitSwitchedToOptimized(false),
#endif
    m_nextInSameThread(nullptr)
{}

PCODE PrepareCodeConfig::IsJitCancellationRequested()
{
    LIMITED_METHOD_CONTRACT;
    return m_pMethodDesc->GetNativeCode();
}

BOOL PrepareCodeConfig::NeedsMulticoreJitNotification()
{
    LIMITED_METHOD_CONTRACT;
    return m_needsMulticoreJitNotification;
}

BOOL PrepareCodeConfig::ProfilerRejectedPrecompiledCode()
{
    LIMITED_METHOD_CONTRACT;
    return m_ProfilerRejectedPrecompiledCode;
}

void PrepareCodeConfig::SetProfilerRejectedPrecompiledCode()
{
    LIMITED_METHOD_CONTRACT;
    m_ProfilerRejectedPrecompiledCode = TRUE;
}

BOOL PrepareCodeConfig::ReadyToRunRejectedPrecompiledCode()
{
    LIMITED_METHOD_CONTRACT;
    return m_ReadyToRunRejectedPrecompiledCode;
}

void PrepareCodeConfig::SetReadyToRunRejectedPrecompiledCode()
{
    LIMITED_METHOD_CONTRACT;
    m_ReadyToRunRejectedPrecompiledCode = TRUE;
}

CallerGCMode PrepareCodeConfig::GetCallerGCMode()
{
    LIMITED_METHOD_CONTRACT;
    return m_callerGCMode;
}

void PrepareCodeConfig::SetCallerGCMode(CallerGCMode mode)
{
    LIMITED_METHOD_CONTRACT;
    m_callerGCMode = mode;
}

BOOL PrepareCodeConfig::SetNativeCode(PCODE pCode, PCODE * ppAlternateCodeToUse)
{
    LIMITED_METHOD_CONTRACT;

    if (m_nativeCodeVersion.SetNativeCodeInterlocked(pCode, NULL))
    {
        return TRUE;
    }

    *ppAlternateCodeToUse = m_nativeCodeVersion.GetNativeCode();
    return FALSE;
}

COR_ILMETHOD* PrepareCodeConfig::GetILHeader()
{
    STANDARD_VM_CONTRACT;
    return m_pMethodDesc->GetILHeader(TRUE);
}

CORJIT_FLAGS PrepareCodeConfig::GetJitCompilationFlags()
{
    STANDARD_VM_CONTRACT;

    CORJIT_FLAGS flags;
#ifdef FEATURE_TIERED_COMPILATION
    flags.Add(TieredCompilationManager::GetJitFlags(this));
#endif
    return flags;
}

BOOL PrepareCodeConfig::MayUsePrecompiledCode()
{
    LIMITED_METHOD_CONTRACT;
    return m_mayUsePrecompiledCode;
}

PrepareCodeConfig::JitOptimizationTier PrepareCodeConfig::GetJitOptimizationTier(
    PrepareCodeConfig *config,
    MethodDesc *methodDesc)
{
    WRAPPER_NO_CONTRACT;
    _ASSERTE(methodDesc != nullptr);
    _ASSERTE(config == nullptr || methodDesc == config->GetMethodDesc());

    if (config != nullptr)
    {
        if (config->JitSwitchedToMinOpt())
        {
            return JitOptimizationTier::MinOptJitted;
        }
    #ifdef FEATURE_TIERED_COMPILATION
        else if (config->JitSwitchedToOptimized())
        {
            _ASSERTE(methodDesc->IsEligibleForTieredCompilation());
            _ASSERTE(
                config->IsForMulticoreJit() ||
                config->GetCodeVersion().GetOptimizationTier() == NativeCodeVersion::OptimizationTierOptimized);
            return JitOptimizationTier::Optimized;
        }
        else if (methodDesc->IsEligibleForTieredCompilation())
        {
            switch (config->GetCodeVersion().GetOptimizationTier())
            {
                case NativeCodeVersion::OptimizationTier0:
                    return JitOptimizationTier::QuickJitted;

                case NativeCodeVersion::OptimizationTier1:
                    return JitOptimizationTier::OptimizedTier1;

                case NativeCodeVersion::OptimizationTier1OSR:
                    return JitOptimizationTier::OptimizedTier1OSR;

                case NativeCodeVersion::OptimizationTierOptimized:
                    return JitOptimizationTier::Optimized;

                case NativeCodeVersion::OptimizationTier0Instrumented:
                    return JitOptimizationTier::InstrumentedTier;

                case NativeCodeVersion::OptimizationTier1Instrumented:
                    return JitOptimizationTier::InstrumentedTierOptimized;

                default:
                    UNREACHABLE();
            }
        }
    #endif
    }

    return methodDesc->IsJitOptimizationDisabled() ? JitOptimizationTier::MinOptJitted : JitOptimizationTier::Optimized;
}

const char *PrepareCodeConfig::GetJitOptimizationTierStr(PrepareCodeConfig *config, MethodDesc *methodDesc)
{
    WRAPPER_NO_CONTRACT;

    switch (GetJitOptimizationTier(config, methodDesc))
    {
        case JitOptimizationTier::Unknown: return "Unknown";
        case JitOptimizationTier::MinOptJitted: return "MinOptJitted";
        case JitOptimizationTier::Optimized: return "Optimized";
        case JitOptimizationTier::QuickJitted: return "QuickJitted";
        case JitOptimizationTier::OptimizedTier1: return "OptimizedTier1";
        case JitOptimizationTier::OptimizedTier1OSR: return "OptimizedTier1OSR";
        case JitOptimizationTier::InstrumentedTier: return "InstrumentedTier";
        case JitOptimizationTier::InstrumentedTierOptimized: return "InstrumentedTierOptimized";

        default:
            UNREACHABLE();
    }
}

#ifdef FEATURE_TIERED_COMPILATION
// This function should be called before SetNativeCode() for consistency with usage of FinalizeOptimizationTierForTier0Jit
bool PrepareCodeConfig::FinalizeOptimizationTierForTier0Load()
{
    _ASSERTE(GetMethodDesc()->IsEligibleForTieredCompilation());
    _ASSERTE(!JitSwitchedToOptimized());

    if (!IsForMulticoreJit())
    {
        return true; // should count calls if SetNativeCode() succeeds
    }

    // When using multi-core JIT, the loaded code would not be used until the method is called. Record some information that may
    // be used later when the method is called.
    ((MulticoreJitPrepareCodeConfig *)this)->SetWasTier0();
    return false; // don't count calls
}

// This function should be called before SetNativeCode() to update the optimization tier if necessary before SetNativeCode() is
// called. As soon as SetNativeCode() is called, another thread may get the native code and the optimization tier for that code
// version, and it should have already been finalized.
bool PrepareCodeConfig::FinalizeOptimizationTierForTier0LoadOrJit()
{
    _ASSERTE(GetMethodDesc()->IsEligibleForTieredCompilation());

    if (IsForMulticoreJit())
    {
        // When using multi-core JIT, the jitted code would not be used until the method is called. Don't make changes to the
        // optimization tier yet, just record some information that may be used later when the method is called.
        ((MulticoreJitPrepareCodeConfig *)this)->SetWasTier0();
        return false; // don't count calls
    }

    if (JitSwitchedToOptimized())
    {
    #ifdef _DEBUG
        // Call counting may already have been disabled due to the possibility of concurrent or reentering JIT of the same
        // native code version of a method. The current optimization tier should be consistent with the change being made
        // (Tier 0 to Optimized), such that the tier is not changed in an unexpected way or at an unexpected time. Since changes
        // to the optimization tier are unlocked, this assertion is just a speculative check on possible values.
        NativeCodeVersion::OptimizationTier previousOptimizationTier = GetCodeVersion().GetOptimizationTier();
        _ASSERTE(
            previousOptimizationTier == NativeCodeVersion::OptimizationTier0 ||
            previousOptimizationTier == NativeCodeVersion::OptimizationTier0Instrumented ||
            previousOptimizationTier == NativeCodeVersion::OptimizationTierOptimized);
    #endif // _DEBUG

        // Update the tier in the code version. The JIT may have decided to switch from tier 0 to optimized, in which case
        // call counting would have to be disabled for the method.
        NativeCodeVersion codeVersion = GetCodeVersion();
        if (codeVersion.IsDefaultVersion())
        {
            GetMethodDesc()->GetLoaderAllocator()->GetCallCountingManager()->DisableCallCounting(codeVersion);
        }
        codeVersion.SetOptimizationTier(NativeCodeVersion::OptimizationTierOptimized);
        return false; // don't count calls
    }

    return true; // should count calls if SetNativeCode() succeeds
}
#endif // FEATURE_TIERED_COMPILATION

#ifdef FEATURE_CODE_VERSIONING
VersionedPrepareCodeConfig::VersionedPrepareCodeConfig() {}

VersionedPrepareCodeConfig::VersionedPrepareCodeConfig(NativeCodeVersion codeVersion) :
    // Multi-core JIT is only applicable to the default code version, so don't request a notification
    PrepareCodeConfig(codeVersion, FALSE, FALSE)
{
    LIMITED_METHOD_CONTRACT;

    _ASSERTE(!m_nativeCodeVersion.IsDefaultVersion());
    _ASSERTE(CodeVersionManager::IsLockOwnedByCurrentThread());
    m_ilCodeVersion = m_nativeCodeVersion.GetILCodeVersion();
}

HRESULT VersionedPrepareCodeConfig::FinishConfiguration()
{
    STANDARD_VM_CONTRACT;

    _ASSERTE(!CodeVersionManager::IsLockOwnedByCurrentThread());

    // Any code build stages that do just in time configuration should
    // be configured now
#ifdef FEATURE_REJIT
    if (m_ilCodeVersion.GetRejitState() != ILCodeVersion::kStateActive)
    {
        ReJitManager::ConfigureILCodeVersion(m_ilCodeVersion);
    }
    _ASSERTE(m_ilCodeVersion.GetRejitState() == ILCodeVersion::kStateActive);
#endif

    return S_OK;
}

PCODE VersionedPrepareCodeConfig::IsJitCancellationRequested()
{
    LIMITED_METHOD_CONTRACT;
    return m_nativeCodeVersion.GetNativeCode();
}

COR_ILMETHOD* VersionedPrepareCodeConfig::GetILHeader()
{
    STANDARD_VM_CONTRACT;
    return m_ilCodeVersion.GetIL();
}

CORJIT_FLAGS VersionedPrepareCodeConfig::GetJitCompilationFlags()
{
    STANDARD_VM_CONTRACT;
    CORJIT_FLAGS flags;

#ifdef FEATURE_REJIT
    DWORD profilerFlags = m_ilCodeVersion.GetJitFlags();
    flags.Add(ReJitManager::JitFlagsFromProfCodegenFlags(profilerFlags));
#endif

#ifdef FEATURE_TIERED_COMPILATION
    flags.Add(TieredCompilationManager::GetJitFlags(this));
#endif

    return flags;
}

PrepareCodeConfigBuffer::PrepareCodeConfigBuffer(NativeCodeVersion codeVersion)
{
    WRAPPER_NO_CONTRACT;

    if (codeVersion.IsDefaultVersion())
    {
        // fast path
        new(m_buffer) PrepareCodeConfig(codeVersion, TRUE, TRUE);
        return;
    }

    // a bit slower path (+1 usec?)
    VersionedPrepareCodeConfig *config;
    {
        CodeVersionManager::LockHolder codeVersioningLockHolder;
        config = new(m_buffer) VersionedPrepareCodeConfig(codeVersion);
    }
    config->FinishConfiguration();
}

#endif //FEATURE_CODE_VERSIONING

#ifdef FEATURE_INSTANTIATINGSTUB_AS_IL

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
    pReturn.ConvertToInternalExactlyOne(msig.GetModule(), &typeContext, stubSigBuilder);

#ifndef TARGET_X86
    // The hidden context parameter
    stubSigBuilder->AppendElementType(ELEMENT_TYPE_I);
#endif // !TARGET_X86

    // Copy rest of the arguments
    msig.NextArg();
    SigPointer pArgs = msig.GetArgProps();
    for (unsigned i = 0; i < msig.NumFixedArgs(); i++)
    {
        pArgs.ConvertToInternalExactlyOne(msig.GetModule(), &typeContext, stubSigBuilder);
    }

#ifdef TARGET_X86
    // The hidden context parameter
    stubSigBuilder->AppendElementType(ELEMENT_TYPE_I);
#endif // TARGET_X86
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
                    (ILStubLinkerFlags)(ILSTUB_LINKER_FLAG_STUB_HAS_THIS | ILSTUB_LINKER_FLAG_TARGET_HAS_THIS));

    ILCodeStream *pCode = sl.NewCodeStream(ILStubLinker::kDispatch);

    // 1. Build the new signature
    SigBuilder stubSigBuilder;
    CreateInstantiatingILStubTargetSig(pTargetMD, typeContext, &stubSigBuilder);

    // 2. Emit the method body
    mdToken tokRawData = pCode->GetToken(CoreLibBinder::GetField(FIELD__RAW_DATA__DATA));

    // 2.1 Push the thisptr
    // We need to skip over the MethodTable*
    // The trick below will do that.
    pCode->EmitLoadThis();
    pCode->EmitLDFLDA(tokRawData);

#if defined(TARGET_X86)
    // 2.2 Push the rest of the arguments for x86
    for (unsigned i = 0; i < msig.NumFixedArgs();i++)
    {
        pCode->EmitLDARG(i);
    }
#endif

    // 2.3 Push the hidden context param
    // The context is going to be captured from the thisptr
    pCode->EmitLoadThis();
    pCode->EmitLDFLDA(tokRawData);
    pCode->EmitLDC(Object::GetOffsetOfFirstField());
    pCode->EmitSUB();
    pCode->EmitLDIND_I();

#if !defined(TARGET_X86)
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
                    msig.HasThis()
                        ? (ILStubLinkerFlags)(ILSTUB_LINKER_FLAG_STUB_HAS_THIS | ILSTUB_LINKER_FLAG_TARGET_HAS_THIS)
                        : (ILStubLinkerFlags)ILSTUB_LINKER_FLAG_NONE
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

#if defined(TARGET_X86)
    // 2.2 Push the rest of the arguments for x86
    for (unsigned i = 0; i < msig.NumFixedArgs();i++)
    {
        pCode->EmitLDARG(i);
    }
#endif // TARGET_X86

    // 2.3 Push the hidden context param
    // InstantiatingStub
    pCode->EmitLDC((TADDR)pHiddenArg);

#if !defined(TARGET_X86)
    // 2.4 Push the rest of the arguments for not x86
    for (unsigned i = 0; i < msig.NumFixedArgs();i++)
    {
        pCode->EmitLDARG(i);
    }
#endif // !TARGET_X86

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

#ifdef FEATURE_PORTABLE_SHUFFLE_THUNKS
    StackSArray<ShuffleEntry> portableShuffle;
    BOOL usePortableShuffle = FALSE;
    if (!pUnboxedMD->RequiresInstMethodTableArg())
    {
        ShuffleEntry entry;
        entry.srcofs = ShuffleEntry::SENTINEL;
        entry.dstofs = 0;
        portableShuffle.Append(entry);
        usePortableShuffle = TRUE;
    }
    else
    {
        usePortableShuffle = GenerateShuffleArrayPortable(pMD, pUnboxedMD, &portableShuffle, ShuffleComputationType::InstantiatingStub);
    }

    if (usePortableShuffle)
    {
        CPUSTUBLINKER sl;
        _ASSERTE(pUnboxedMD != NULL && pUnboxedMD != pMD);

        // The shuffle for an unboxing stub of a method that doesn't capture the
        // type of the this pointer must be a no-op
        _ASSERTE(pUnboxedMD->RequiresInstMethodTableArg() || (portableShuffle.GetCount() == 1));

        sl.EmitComputedInstantiatingMethodStub(pUnboxedMD, &portableShuffle[0], NULL);

        pstub = sl.Link(pMD->GetLoaderAllocator()->GetStubHeap(), NEWSTUB_FL_INSTANTIATING_METHOD);
    }
    else
#endif
    {
#ifdef FEATURE_INSTANTIATINGSTUB_AS_IL
#ifndef FEATURE_PORTABLE_SHUFFLE_THUNKS
        if (pUnboxedMD->RequiresInstMethodTableArg())
#endif // !FEATURE_PORTABLE_SHUFFLE_THUNKS
        {
            _ASSERTE(pUnboxedMD->RequiresInstMethodTableArg());
            pstub = CreateUnboxingILStubForSharedGenericValueTypeMethods(pUnboxedMD);
        }
#ifndef FEATURE_PORTABLE_SHUFFLE_THUNKS
        else
#endif // !FEATURE_PORTABLE_SHUFFLE_THUNKS
#endif // FEATURE_INSTANTIATINGSTUB_AS_IL
#ifndef FEATURE_PORTABLE_SHUFFLE_THUNKS
        {
            CPUSTUBLINKER sl;
            sl.EmitUnboxMethodStub(pUnboxedMD);
            pstub = sl.Link(pMD->GetLoaderAllocator()->GetStubHeap());
        }
#endif // !FEATURE_PORTABLE_SHUFFLE_THUNKS
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

#ifdef FEATURE_PORTABLE_SHUFFLE_THUNKS
    StackSArray<ShuffleEntry> portableShuffle;
    if (GenerateShuffleArrayPortable(pMD, pSharedMD, &portableShuffle, ShuffleComputationType::InstantiatingStub))
    {
        CPUSTUBLINKER sl;
        _ASSERTE(pSharedMD != NULL && pSharedMD != pMD);
        sl.EmitComputedInstantiatingMethodStub(pSharedMD, &portableShuffle[0], extraArg);

        pstub = sl.Link(pMD->GetLoaderAllocator()->GetStubHeap(), NEWSTUB_FL_INSTANTIATING_METHOD);
    }
    else
#endif
    {
#ifdef FEATURE_INSTANTIATINGSTUB_AS_IL
        pstub = CreateInstantiatingILStub(pSharedMD, extraArg);
#else
        CPUSTUBLINKER sl;
        _ASSERTE(pSharedMD != NULL && pSharedMD != pMD);
        sl.EmitInstantiatingMethodStub(pSharedMD, extraArg);

        pstub = sl.Link(pMD->GetLoaderAllocator()->GetStubHeap());
#endif
    }

    RETURN pstub;
}
#endif // defined(FEATURE_SHARE_GENERIC_CODE)

#if defined (HAS_COMPACT_ENTRYPOINTS) && defined (TARGET_ARM)

extern "C" MethodDesc * STDCALL PreStubGetMethodDescForCompactEntryPoint (PCODE pCode)
{
    _ASSERTE (pCode >= PC_REG_RELATIVE_OFFSET);

    pCode = (PCODE) (pCode - PC_REG_RELATIVE_OFFSET + THUMB_CODE);

    _ASSERTE (MethodDescChunk::IsCompactEntryPointAtAddress (pCode));

    return MethodDescChunk::GetMethodDescFromCompactEntryPoint(pCode, FALSE);
}

#endif // defined (HAS_COMPACT_ENTRYPOINTS) && defined (TARGET_ARM)

//=============================================================================
// This function generates the real code when from Preemptive mode.
// It is specifically designed to work with the UnmanagedCallersOnlyAttribute.
//=============================================================================
static PCODE PreStubWorker_Preemptive(
    _In_ TransitionBlock* pTransitionBlock,
    _In_ MethodDesc* pMD,
    _In_opt_ Thread* currentThread)
{
    _ASSERTE(pMD->HasUnmanagedCallersOnlyAttribute());

    PCODE pbRetVal = NULL;

    STATIC_CONTRACT_THROWS;
    STATIC_CONTRACT_GC_TRIGGERS;
    STATIC_CONTRACT_MODE_PREEMPTIVE;

    // Starting from preemptive mode means the possibility exists
    // that the thread is new to the runtime so we might have to
    // create one.
    if (currentThread == NULL)
    {
        // If our attempt to create a thread fails, there is nothing
        // more we can do except fail fast. The reverse P/Invoke isn't
        // going to work.
        CREATETHREAD_IF_NULL_FAILFAST(currentThread, W("Failed to setup new thread during reverse P/Invoke"));
    }

    MAKE_CURRENT_THREAD_AVAILABLE_EX(currentThread);

    // No GC frame is needed here since there should be no OBJECTREFs involved
    // in this call due to UnmanagedCallersOnlyAttribute semantics.

    INSTALL_MANAGED_EXCEPTION_DISPATCHER;
    INSTALL_UNWIND_AND_CONTINUE_HANDLER;

    // Make sure the method table is restored, and method instantiation if present
    pMD->CheckRestore();
    CONSISTENCY_CHECK(GetAppDomain()->CheckCanExecuteManagedCode(pMD));

    pbRetVal = pMD->DoPrestub(NULL, CallerGCMode::Preemptive);

    UNINSTALL_UNWIND_AND_CONTINUE_HANDLER;
    UNINSTALL_MANAGED_EXCEPTION_DISPATCHER;

    {
        HardwareExceptionHolder;

        // Give debugger opportunity to stop here
        ThePreStubPatch();
    }

    return pbRetVal;
}

//=============================================================================
// This function generates the real code for a method and installs it into
// the methoddesc. Usually ***BUT NOT ALWAYS***, this function runs only once
// per methoddesc. In addition to installing the new code, this function
// returns a pointer to the new code for the prestub's convenience.
//=============================================================================
extern "C" PCODE STDCALL PreStubWorker(TransitionBlock* pTransitionBlock, MethodDesc* pMD)
{
    PCODE pbRetVal = NULL;

    BEGIN_PRESERVE_LAST_ERROR;

    STATIC_CONTRACT_THROWS;
    STATIC_CONTRACT_GC_TRIGGERS;
    STATIC_CONTRACT_MODE_ANY;
    STATIC_CONTRACT_ENTRY_POINT;

    ETWOnStartup(PrestubWorker_V1, PrestubWorkerEnd_V1);

    MAKE_CURRENT_THREAD_AVAILABLE_EX(GetThreadNULLOk());

    // Attempt to check what GC mode we are running under.
    if (CURRENT_THREAD == NULL
        || !CURRENT_THREAD->PreemptiveGCDisabled())
    {
        pbRetVal = PreStubWorker_Preemptive(pTransitionBlock, pMD, CURRENT_THREAD);
    }
    else
    {
        // This is the typical case (i.e. COOP mode).

#ifdef _DEBUG
        Thread::ObjectRefFlush(CURRENT_THREAD);
#endif

        FrameWithCookie<PrestubMethodFrame> frame(pTransitionBlock, pMD);
        PrestubMethodFrame* pPFrame = &frame;

        pPFrame->Push(CURRENT_THREAD);

        INSTALL_MANAGED_EXCEPTION_DISPATCHER;
        INSTALL_UNWIND_AND_CONTINUE_HANDLER;

        // Make sure the method table is restored, and method instantiation if present
        pMD->CheckRestore();
        CONSISTENCY_CHECK(GetAppDomain()->CheckCanExecuteManagedCode(pMD));

        MethodTable* pDispatchingMT = NULL;
        if (pMD->IsVtableMethod())
        {
            OBJECTREF curobj = pPFrame->GetThis();

            if (curobj != NULL) // Check for virtual function called non-virtually on a NULL object
            {
                pDispatchingMT = curobj->GetMethodTable();

                if (pDispatchingMT->IsICastable() || pDispatchingMT->IsIDynamicInterfaceCastable())
                {
                    MethodTable* pMDMT = pMD->GetMethodTable();
                    TypeHandle objectType(pDispatchingMT);
                    TypeHandle methodType(pMDMT);

                    GCStress<cfg_any>::MaybeTrigger();
                    INDEBUG(curobj = NULL); // curobj is unprotected and CanCastTo() can trigger GC
                    if (!objectType.CanCastTo(methodType))
                    {
                        // Apparently ICastable magic was involved when we chose this method to be called
                        // that's why we better stick to the MethodTable it belongs to, otherwise
                        // DoPrestub() will fail not being able to find implementation for pMD in pDispatchingMT.

                        pDispatchingMT = pMDMT;
                    }
                }

                // For value types, the only virtual methods are interface implementations.
                // Thus pDispatching == pMT because there
                // is no inheritance in value types.  Note the BoxedEntryPointStubs are shared
                // between all sharable generic instantiations, so the == test is on
                // canonical method tables.
#ifdef _DEBUG
                MethodTable* pMDMT = pMD->GetMethodTable(); // put this here to see what the MT is in debug mode
                _ASSERTE(!pMD->GetMethodTable()->IsValueType() ||
                (pMD->IsUnboxingStub() && (pDispatchingMT->GetCanonicalMethodTable() == pMDMT->GetCanonicalMethodTable())));
#endif // _DEBUG
            }
        }

        GCX_PREEMP_THREAD_EXISTS(CURRENT_THREAD);
        {
            pbRetVal = pMD->DoPrestub(pDispatchingMT, CallerGCMode::Coop);
        }

        UNINSTALL_UNWIND_AND_CONTINUE_HANDLER;
        UNINSTALL_MANAGED_EXCEPTION_DISPATCHER;

        {
            HardwareExceptionHolder;

            // Give debugger opportunity to stop here
            ThePreStubPatch();
        }

        pPFrame->Pop(CURRENT_THREAD);
    }

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
PCODE MethodDesc::DoPrestub(MethodTable *pDispatchingMT, CallerGCMode callerGCMode)
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

    STRESS_LOG1(LF_CLASSLOADER, LL_INFO10000, "DoPrestub: method %p\n", this);


    GCStress<cfg_any, EeconfigFastGcSPolicy, CoopGcModePolicy>::MaybeTrigger();


#ifdef FEATURE_COMINTEROP
    /**************************   INTEROP   *************************/
    /*-----------------------------------------------------------------
    // Some method descriptors are COMPLUS-to-COM call descriptors
    // they are not your every day method descriptors, for example
    // they don't have an IL or code.
    */
    if (IsComPlusCall())
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

    /**************************   BACKPATCHING   *************************/
#ifdef FEATURE_CODE_VERSIONING
    if (IsVersionable())
    {
        bool doBackpatch = true;
        bool doFullBackpatch = false;
        pCode = GetCodeVersionManager()->PublishVersionableCodeIfNecessary(this, callerGCMode, &doBackpatch, &doFullBackpatch);

        if (doBackpatch)
        {
            RETURN DoBackpatch(pMT, pDispatchingMT, doFullBackpatch);
        }

        _ASSERTE(pCode != NULL);
        _ASSERTE(!doFullBackpatch);
        RETURN pCode;
    }
#endif

    if (!IsPointingToPrestub())
    {
        LOG((LF_CLASSLOADER, LL_INFO10000,
            "    In PreStubWorker, method already jitted, backpatching call point\n"));
        #if defined(FEATURE_JIT_PITCHING)
            MarkMethodNotPitchingCandidate(this);
        #endif

        RETURN DoBackpatch(pMT, pDispatchingMT, TRUE);
    }

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
        if (!IsNativeCodeStableAfterInit())
        {
            GetOrCreatePrecode();
        }
        pCode = PrepareInitialCode(callerGCMode);
    } // end else if (IsIL() || IsNoMetadata())
    else if (IsNDirect())
    {
        if (GetModule()->IsReadyToRun() && GetModule()->GetReadyToRunInfo()->HasNonShareablePInvokeStubs() && MayUsePrecompiledILStub())
        {
            // In crossgen2, we compile non-shareable IL stubs for pinvokes. If we can find code for such
            // a stub, we'll use it directly instead and avoid emitting an IL stub.
            PrepareCodeConfig config(NativeCodeVersion(this), TRUE, TRUE);
            pCode = GetPrecompiledR2RCode(&config);
            if (pCode != NULL)
            {
                LOG_USING_R2R_CODE(this);
            }
        }

        if (pCode == NULL)
        {
            pCode = GetStubForInteropMethod(this);
        }

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
    _ASSERTE(pCode == NULL || GetNativeCode() == NULL || pCode == GetNativeCode());

    // At this point we must have either a pointer to managed code or to a stub. All of the above code
    // should have thrown an exception if it couldn't make a stub.
    _ASSERTE((pStub != NULL) ^ (pCode != NULL));

#if defined(TARGET_X86) || defined(TARGET_AMD64)
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

    if (pCode != NULL)
    {
        _ASSERTE(!MayHaveEntryPointSlotsToBackpatch()); // This path doesn't lock the MethodDescBackpatchTracker as it should only
                                                        // happen for jump-stampable or non-versionable methods
        SetCodeEntryPoint(pCode);
    }
    else
    {
        if (!GetOrCreatePrecode()->SetTargetInterlocked(pStub->GetEntryPoint()))
        {
            if (pStub->HasExternalEntryPoint())
            {
                // Stubs with external entry point are allocated from regular heap and so they are always writeable
                pStub->DecRef();
            }
            else
            {
                ExecutableWriterHolder<Stub> stubWriterHolder(pStub, sizeof(Stub));
                stubWriterHolder.GetRW()->DecRef();
            }
        }
        else if (pStub->HasExternalEntryPoint())
        {
            // If the Stub wraps code that is outside of the Stub allocation, then we
            // need to free the Stub allocation now.
            pStub->DecRef();
        }
    }

    _ASSERTE(!IsPointingToPrestub());
    _ASSERTE(HasStableEntryPoint());

    RETURN DoBackpatch(pMT, pDispatchingMT, FALSE);
}

#endif // !DACCESS_COMPILE

//==========================================================================
// The following code manages the PreStub. All method stubs initially
// use the prestub.
//==========================================================================

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

    ThePreStubManager::Init();
}

PCODE TheUMThunkPreStub()
{
    LIMITED_METHOD_CONTRACT;

    return GetEEFuncEntryPoint(TheUMEntryPrestub);
}

PCODE TheVarargNDirectStub(BOOL hasRetBuffArg)
{
    LIMITED_METHOD_CONTRACT;

#if !defined(TARGET_X86) && !defined(TARGET_ARM64) && !defined(TARGET_LOONGARCH64) && !defined(TARGET_RISCV64)
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

static PCODE PatchNonVirtualExternalMethod(MethodDesc * pMD, PCODE pCode, PTR_READYTORUN_IMPORT_SECTION pImportSection, TADDR pIndirection)
{
    STANDARD_VM_CONTRACT;

    //
    // Skip fixup precode jump for better perf. Since we have MethodDesc available, we can use cheaper method
    // than code:Precode::TryToSkipFixupPrecode.
    //
#ifdef HAS_FIXUP_PRECODE
    if (pMD->HasPrecode() && pMD->GetPrecode()->GetType() == PRECODE_FIXUP
        && pMD->IsNativeCodeStableAfterInit())
    {
        PCODE pDirectTarget = pMD->IsFCall() ? ECall::GetFCallImpl(pMD) : pMD->GetNativeCode();
        if (pDirectTarget != NULL)
            pCode = pDirectTarget;
    }
#endif //HAS_FIXUP_PRECODE

    *(TADDR *)pIndirection = pCode;

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
    // CoreLib method System.StubHelpers.StubHelpers.SetLastError().
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

#if defined(TARGET_X86) || defined(TARGET_AMD64)
    // Decode indirection cell from callsite if it is not present
    if (pIndirection == NULL)
    {
        // Assume that the callsite is call [xxxxxxxx]
        PCODE retAddr = pEMFrame->GetReturnAddress();
#ifdef TARGET_X86
        pIndirection = *(((TADDR *)retAddr) - 1);
#else
        pIndirection = *(((INT32 *)retAddr) - 1) + retAddr;
#endif
    }
#endif

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

        PEImageLayout *pNativeImage = pModule->GetReadyToRunImage();

        RVA rva = pNativeImage->GetDataRva(pIndirection);

        PTR_READYTORUN_IMPORT_SECTION pImportSection;
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

        _ASSERTE(pImportSection->EntrySize == sizeof(TADDR));
        COUNT_T index = (rva - pImportSection->Section.VirtualAddress) / sizeof(TADDR);

        PTR_DWORD pSignatures = dac_cast<PTR_DWORD>(pNativeImage->GetRvaData(pImportSection->Signatures));

        PCCOR_SIGNATURE pBlob = (BYTE *)pNativeImage->GetRvaData(pSignatures[index]);

        BYTE kind = *pBlob++;

        ModuleBase * pInfoModule = pModule;
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

                _ASSERTE(!pMD->GetMethodTable()->IsGenericTypeDefinition() || pMD->GetMethodTable()->GetNumGenericArgs() == 0);

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
                _ASSERTE(pInfoModule->IsFullModule());
                pMD = MemberLoader::GetMethodDescFromMethodDef(static_cast<Module*>(pInfoModule), MethodDef, FALSE);

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
                _ASSERTE(pInfoModule->IsFullModule());
                pMD = MemberLoader::GetMethodDescFromMethodDef(static_cast<Module*>(pInfoModule), MethodDef, FALSE);

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

            OBJECTREF *protectedObj = pEMFrame->GetThisPtr();
            _ASSERTE(protectedObj != NULL);
            if (*protectedObj == NULL) {
                COMPlusThrow(kNullReferenceException);
            }

            DispatchToken token;
            if (pMT->IsInterface())
            {
                if (pMT->IsInterface())
                    token = pMT->GetLoaderAllocator()->GetDispatchToken(pMT->GetTypeID(), slot);
                else
                    token = DispatchToken::CreateDispatchToken(slot);

                StubCallSite callSite(pIndirection, pEMFrame->GetReturnAddress());
                pCode = pMgr->ResolveWorker(&callSite, protectedObj, token, STUB_CODE_BLOCK_VSD_LOOKUP_STUB);
            }
            else
            {
                pCode = pMgr->GetVTableCallStub(slot);
                *(TADDR *)pIndirection = pCode;
            }
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

#if _DEBUG
            if (pEMFrame->GetGCRefMap() != NULL)
            {
                _ASSERTE(CheckGCRefMapEqual(pEMFrame->GetGCRefMap(), pMD, false));
            }
#endif // _DEBUG

            //
            // Note that we do not want to call code:MethodDesc::IsPointingToPrestub() here. It does not take remoting
            // interception into account and so it would cause otherwise intercepted methods to be JITed. It is a compat
            // issue if the JITing fails.
            //
            if (!DoesSlotCallPrestub(pCode))
            {
                if (pMD->IsVersionableWithVtableSlotBackpatch())
                {
                    // The entry point for this method needs to be versionable, so use a FuncPtrStub similarly to what is done
                    // in MethodDesc::GetMultiCallableAddrOfCode()
                    GCX_COOP();
                    pCode = pMD->GetLoaderAllocator()->GetFuncPtrStubs()->GetFuncPtrStub(pMD);
                }

                pCode = PatchNonVirtualExternalMethod(pMD, pCode, pImportSection, pIndirection);
            }
        }

#if defined (FEATURE_JIT_PITCHING)
        DeleteFromPitchingCandidate(pMD);
#endif
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
#ifdef TARGET_X86
    // x86 is special as always
    pArgument += offsetof(ArgumentRegisters, ECX);
#endif

    return pArgument;
}

void ProcessDynamicDictionaryLookup(TransitionBlock *           pTransitionBlock,
                                    Module *                    pModule,
                                    ModuleBase *                pInfoModule,
                                    BYTE                        kind,
                                    PCCOR_SIGNATURE             pBlob,
                                    PCCOR_SIGNATURE             pBlobStart,
                                    CORINFO_RUNTIME_LOOKUP *    pResult,
                                    DWORD *                     pDictionaryIndexAndSlot)
{
    STANDARD_VM_CONTRACT;

    TADDR genericContextPtr = *(TADDR*)GetFirstArgumentRegisterValuePtr(pTransitionBlock);

    pResult->signature = NULL;
    pResult->testForNull = false;
    pResult->indirectFirstOffset = 0;
    pResult->indirectSecondOffset = 0;
    // Dictionary size checks skipped by default, unless we decide otherwise
    pResult->sizeOffset = CORINFO_NO_SIZE_CHECK;
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

            uint32_t data;
            IfFailThrow(sigptr.GetData(&data));
            pResult->offsets[1] = sizeof(TypeHandle) * data;

            return;
        }
        else if ((type == ELEMENT_TYPE_VAR) && (kind != ENCODE_DICTIONARY_LOOKUP_METHOD))
        {
            pResult->indirections = 3;
            pResult->offsets[0] = MethodTable::GetOffsetOfPerInstInfo();
            pResult->offsets[1] = sizeof(TypeHandle*) * (pContextMT->GetNumDicts() - 1);

            uint32_t data;
            IfFailThrow(sigptr.GetData(&data));
            pResult->offsets[2] = sizeof(TypeHandle) * data;

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
        if (DictionaryLayout::FindToken(pContextMD, pModule->GetLoaderAllocator(), 1, NULL, (BYTE*)pBlobStart, FromReadyToRunImage, pResult, &dictionarySlot))
        {
            pResult->testForNull = 1;
            int minDictSize = pContextMD->GetNumGenericMethodArgs() + 1 + pContextMD->GetDictionaryLayout()->GetNumInitialSlots();
            if (dictionarySlot >= minDictSize)
            {
                // Dictionaries are guaranteed to have at least the number of slots allocated initially, so skip size check for smaller indexes
                pResult->sizeOffset = (WORD)pContextMD->GetNumGenericMethodArgs() * sizeof(DictionaryEntry);
            }

            // Indirect through dictionary table pointer in InstantiatedMethodDesc
            pResult->offsets[0] = offsetof(InstantiatedMethodDesc, m_pPerInstInfo);

            *pDictionaryIndexAndSlot |= dictionarySlot;
        }
    }

    // It's a class dictionary lookup (CORINFO_LOOKUP_CLASSPARAM or CORINFO_LOOKUP_THISOBJ)
    else
    {
        if (DictionaryLayout::FindToken(pContextMT, pModule->GetLoaderAllocator(), 2, NULL, (BYTE*)pBlobStart, FromReadyToRunImage, pResult, &dictionarySlot))
        {
            pResult->testForNull = 1;
            int minDictSize = pContextMT->GetNumGenericArgs() + 1 + pContextMT->GetClass()->GetDictionaryLayout()->GetNumInitialSlots();
            if (dictionarySlot >= minDictSize)
            {
                // Dictionaries are guaranteed to have at least the number of slots allocated initially, so skip size check for smaller indexes
                pResult->sizeOffset = (WORD)pContextMT->GetNumGenericArgs() * sizeof(DictionaryEntry);
            }

            // Indirect through dictionary table pointer in vtable
            pResult->offsets[0] = MethodTable::GetOffsetOfPerInstInfo();

            // Next indirect through the dictionary appropriate to this instantiated type
            pResult->offsets[1] = sizeof(TypeHandle*) * (pContextMT->GetNumDicts() - 1);

            *pDictionaryIndexAndSlot |= dictionarySlot;
        }
    }
}

PCODE DynamicHelperFixup(TransitionBlock * pTransitionBlock, TADDR * pCell, DWORD sectionIndex, Module * pModule, CORCOMPILE_FIXUP_BLOB_KIND * pKind, TypeHandle * pTH, MethodDesc ** ppMD, FieldDesc ** ppFD)
{
    STANDARD_VM_CONTRACT;

    PEImageLayout *pNativeImage = pModule->GetReadyToRunImage();

    RVA rva = pNativeImage->GetDataRva((TADDR)pCell);

    PTR_READYTORUN_IMPORT_SECTION pImportSection = pModule->GetImportSectionFromIndex(sectionIndex);
    _ASSERTE(pImportSection == pModule->GetImportSectionForRVA(rva));

    _ASSERTE(pImportSection->EntrySize == sizeof(TADDR));

    COUNT_T index = (rva - pImportSection->Section.VirtualAddress) / sizeof(TADDR);

    PTR_DWORD pSignatures = dac_cast<PTR_DWORD>(pNativeImage->GetRvaData(pImportSection->Signatures));

    PCCOR_SIGNATURE pBlob = (BYTE *)pNativeImage->GetRvaData(pSignatures[index]);
    PCCOR_SIGNATURE pBlobStart = pBlob;

    BYTE kind = *pBlob++;

    ModuleBase * pInfoModule = pModule;
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
        FALLTHROUGH;
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
        FALLTHROUGH;
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
                    CorInfoHelpFunc helpFunc = CEEInfo::getCastingHelperStatic(th, /* throwing */ (kind == ENCODE_CHKCAST_HELPER));
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
                            if (pFD->IsRVA())
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
                *(TADDR *)pCell = pHelper;
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
                bool fHasSideEffectsUnused;
                CorInfoHelpFunc helpFunc = CEEInfo::getNewHelperStatic(th.AsMethodTable(), &fHasSideEffectsUnused);
                pHelper = DynamicHelpers::CreateHelper(pModule->GetLoaderAllocator(), th.AsTAddr(), CEEJitInfo::getHelperFtnStatic(helpFunc));
            }
            break;
        case ENCODE_NEW_ARRAY_HELPER:
            {
                CorInfoHelpFunc helpFunc = CEEInfo::getNewArrHelperStatic(th);
                MethodTable *pArrayMT = th.AsMethodTable();
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
                        // This should never happen - we should never get collectible or wrapper delegates here
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
                    target = ECall::GetFCallImpl(CoreLibBinder::GetMethod(METHOD__DELEGATE__CONSTRUCT_DELEGATE));
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
            *(TADDR *)pCell = pHelper;
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

#if defined(TARGET_X86) || defined(TARGET_AMD64)
    // Decode indirection cell from callsite if it is not present
    if (pCell == NULL)
    {
        // Assume that the callsite is call [xxxxxxxx]
        PCODE retAddr = pFrame->GetReturnAddress();
#ifdef TARGET_X86
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
