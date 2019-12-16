// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// ===========================================================================
// File: TieredCompilation.h
//
// ===========================================================================


#ifndef TIERED_COMPILATION_H
#define TIERED_COMPILATION_H

// TieredCompilationManager determines which methods should be recompiled and
// how they should be recompiled to best optimize the running code. It then
// handles logistics of getting new code created and installed.
class TieredCompilationManager
{
#ifdef FEATURE_TIERED_COMPILATION

public:
#if defined(DACCESS_COMPILE) || defined(CROSSGEN_COMPILE)
    TieredCompilationManager() {}
#else
    TieredCompilationManager();
#endif

    void Init();

#endif // FEATURE_TIERED_COMPILATION

public:
    static NativeCodeVersion::OptimizationTier GetInitialOptimizationTier(PTR_MethodDesc pMethodDesc);

#ifdef FEATURE_TIERED_COMPILATION

public:
    void HandleCallCountingForFirstCall(MethodDesc* pMethodDesc);
    bool TrySetCodeEntryPointAndRecordMethodForCallCounting(MethodDesc* pMethodDesc, PCODE codeEntryPoint);
    void AsyncPromoteToTier1(NativeCodeVersion tier0NativeCodeVersion, bool *scheduleTieringBackgroundWorkRef);
    static CORJIT_FLAGS GetJitFlags(NativeCodeVersion nativeCodeVersion);

private:
    bool IsTieringDelayActive();
    static void WINAPI TieringDelayTimerCallback(PVOID parameter, BOOLEAN timerFired);
    void DeactivateTieringDelay();

public:
    void AsyncCompleteCallCounting();

public:
    void ScheduleBackgroundWork();
private:
    void RequestBackgroundWork();
    static DWORD StaticBackgroundWorkCallback(void* args);
    void DoBackgroundWork();

private:
    void OptimizeMethod(NativeCodeVersion nativeCodeVersion);
    NativeCodeVersion GetNextMethodToOptimize();
    BOOL CompileCodeVersion(NativeCodeVersion nativeCodeVersion);
    void ActivateCodeVersion(NativeCodeVersion nativeCodeVersion);

#ifndef DACCESS_COMPILE
private:
    static CrstStatic s_lock;

public:
    static void StaticInitialize()
    {
        WRAPPER_NO_CONTRACT;

        // CodeVersionManager's lock is also CRST_UNSAFE_ANYMODE. To avoid having to unnecessarily take the TieredCompilation
        // lock for larger sections of code before the CodeVersionManager's lock, it is instead taken after the
        // CodeVersionManager's lock for the few cases where both locks need to be held, so it must also be CRST_UNSAFE_ANYMODE.
        s_lock.Init(CrstTieredCompilation, CrstFlags(CRST_UNSAFE_ANYMODE));
    }

#ifdef _DEBUG
public:
    static bool IsLockOwnedByCurrentThread();
#endif

public:
    class LockHolder : private CrstHolder
    {
    public:
        LockHolder() : CrstHolder(&s_lock)
        {
            WRAPPER_NO_CONTRACT;
        }

        LockHolder(const LockHolder &) = delete;
        LockHolder &operator =(const LockHolder &) = delete;
    };

private:
    class AutoResetIsBackgroundWorkScheduled;
#endif // !DACCESS_COMPILE

private:
    SList<SListElem<NativeCodeVersion>> m_methodsToOptimize;
    UINT32 m_countOfMethodsToOptimize;
    UINT32 m_countOfNewMethodsCalledDuringDelay;
    SArray<MethodDesc*>* m_methodsPendingCountingForTier1;
    HANDLE m_tieringDelayTimerHandle;
    bool m_isBackgroundWorkScheduled;
    bool m_tier1CallCountingCandidateMethodRecentlyRecorded;
    bool m_isPendingCallCountingCompletion;
    bool m_recentlyRequestedCallCountingCompletionAgain;

    CLREvent m_asyncWorkDoneEvent;

#endif // FEATURE_TIERED_COMPILATION
};

#endif // TIERED_COMPILATION_H
