// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// ===========================================================================
// File: TieredCompilation.h
//
// ===========================================================================

// Exceptions (OOM)
// - On foreground threads, exceptions are propagated unless they can be handled without any compromise
// - On background threads, exceptions are caught and logged. The scope of an exception is limited to one per method or code
//   version such that if there is a loop over many, an exception would not abort the entire loop, rather just one iteration of
//   the loop.
// - Exceptions may lead to one or more methods to not be promoted anymore, and perhaps, though rarely, with no further chance
//   of promotion

#ifndef TIERED_COMPILATION_H
#define TIERED_COMPILATION_H

// TieredCompilationManager determines which methods should be recompiled and
// how they should be recompiled to best optimize the running code. It then
// handles logistics of getting new code created and installed.
class TieredCompilationManager
{
#ifdef FEATURE_TIERED_COMPILATION

public:
#if defined(DACCESS_COMPILE)
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
    void AsyncPromoteToTier1(NativeCodeVersion tier0NativeCodeVersion, bool *createTieringBackgroundWorkerRef);
    static CORJIT_FLAGS GetJitFlags(PrepareCodeConfig *config);

#if !defined(DACCESS_COMPILE) && defined(_DEBUG)
public:
    static Thread *GetBackgroundWorkerThread()
    {
        LIMITED_METHOD_CONTRACT;
        return s_backgroundWorkerThread;
    }
#endif

public:
    static bool TryScheduleBackgroundWorkerWithoutGCTrigger_Locked();
    static void CreateBackgroundWorker();
private:
    static DWORD WINAPI BackgroundWorkerBootstrapper0(LPVOID args);
    static void BackgroundWorkerBootstrapper1(LPVOID args);
    void BackgroundWorkerStart();

private:
    bool IsTieringDelayActive();
    bool TryDeactivateTieringDelay();

public:
    void AsyncCompleteCallCounting();

private:
    static DWORD StaticBackgroundWorkCallback(void* args);
    bool DoBackgroundWork(UINT64 *workDurationTicksRef, UINT64 minWorkDurationTicks, UINT64 maxWorkDurationTicks);

private:
    void OptimizeMethod(NativeCodeVersion nativeCodeVersion);
    NativeCodeVersion GetNextMethodToOptimize();
    BOOL CompileCodeVersion(NativeCodeVersion nativeCodeVersion);
    void ActivateCodeVersion(NativeCodeVersion nativeCodeVersion);

#ifndef DACCESS_COMPILE
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
#endif // !DACCESS_COMPILE

#ifndef DACCESS_COMPILE
private:
    static CrstStatic s_lock;
#ifdef _DEBUG
    static Thread *s_backgroundWorkerThread;
#endif
    static CLREvent s_backgroundWorkAvailableEvent;
    static bool s_isBackgroundWorkerRunning;
    static bool s_isBackgroundWorkerProcessingWork;
#endif // !DACCESS_COMPILE

private:
    SList<SListElem<NativeCodeVersion>> m_methodsToOptimize;
    UINT32 m_countOfMethodsToOptimize;
    UINT32 m_countOfNewMethodsCalledDuringDelay;
    SArray<MethodDesc*>* m_methodsPendingCountingForTier1;
    bool m_tier1CallCountingCandidateMethodRecentlyRecorded;
    bool m_isPendingCallCountingCompletion;
    bool m_recentlyRequestedCallCountingCompletion;

#endif // FEATURE_TIERED_COMPILATION
};

#endif // TIERED_COMPILATION_H
