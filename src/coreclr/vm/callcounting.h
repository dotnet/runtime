// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma once

#include "codeversion.h"

#ifdef FEATURE_TIERED_COMPILATION

/*******************************************************************************************************************************
** Summary

Outline of phases
-----------------

When starting call counting for a method (see CallCountingManager::SetCodeEntryPoint):
- A CallCountingInfo is created (associated with the NativeCodeVersion to be counted), which initializes a remaining call count
  with a threshold
- A CallCountingStub is created. It contains a small amount of code that decrements the remaining call count and checks for
  zero. When nonzero, it jumps to the code version's native code entry point. When zero, it forwards to a helper function that
  handles tier promotion.
- For tiered methods that don't have a precode (virtual and interface methods when slot backpatching is enabled), a forwarder
  stub (a precode) is created and it forwards to the call counting stub. This is so that the call counting stub can be safely
  and easily deleted. The forwarder stubs are only used when counting calls, there is one per method (not per code version), and
  they are not deleted.
- The method's code entry point is set to the forwarder stub or the call counting stub to count calls to the code version

When the call count threshold is reached (see CallCountingManager::OnCallCountThresholdReached):
- The helper call enqueues completion of call counting for background processing
- When completing call counting in the background, the code version is enqueued for promotion, and the call counting stub is
  removed from the call chain

After all work queued for promotion is completed and methods transitioned to optimized tier, some cleanup follows
(see CallCountingManager::StopAndDeleteAllCallCountingStubs):
- Some heuristics are checked and if cleanup will be done, the runtime is suspended
- All call counting stubs are deleted. For code versions that have not completed counting, the method's code entry point is
  reset such that call counting would be reestablished on the next call.
- Completed call counting infos are deleted
- For methods that no longer have any code versions that need to be counted, the forwarder stubs are no longer tracked. If a
  new IL code version is added thereafter (perhaps by a profiler), a new forwarder stub may be created.

Miscellaneous
-------------

- The CallCountingManager is the main class with most of the logic. Its private subclasses are just simple data structures.
- The code versioning lock is used for data structures used for call counting. Installing a call counting stub requires that we
  know what the currently active code version is, it made sense to use the same lock.
- Call counting stubs have hardcoded code. x64 has short and long stubs, short stubs are used when possible (often) and use
  IP-relative branches to the method's code and helper stub. Other archs have only one type of stub (a short stub).
  - Call counting stubs pass a stub-identifying token to the threshold-reached helper function. The stub's address can be
    determined from it. On x64, it also indicates whether the stub is a short or long stub.
  - From a call counting stub, the call counting info can be determined using the remaining call count cell, and from the call
    counting info the code version and method can be determined
- Call counting is not stopped when the tiering delay is reactivated (often happens in larger and more realistic scenarios). The
  overhead necessary to stop and restart call counting (among other things, many methods will have to go through the prestub
  again) is greater than the overhead of completing call counting + calling the threshold-reached helper function, even for very
  high call count thresholds. While it may at times be desirable to not count method calls during startup phases, there would be
  a fair bit of additional overhead to stop counting. On the other hand, it may at times be beneficial to rejit some methods
  during startup. So for now, only newly called methods during the current tiering delay would not be counted, any that already
  started counting will continue (their delay already expired).

*******************************************************************************************************************************/

#define DISABLE_COPY(T) \
    T(const T &) = delete; \
    T &operator =(const T &) = delete

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Call counting

typedef UINT16 CallCount;
typedef DPTR(CallCount) PTR_CallCount;

////////////////////////////////////////////////////////////////
// CallCountingStub

class CallCountingStub;
typedef DPTR(const CallCountingStub) PTR_CallCountingStub;

struct CallCountingStubData
{
    PTR_CallCount RemainingCallCountCell;
    PCODE TargetForMethod;
    PCODE TargetForThresholdReached;
};

typedef DPTR(CallCountingStubData) PTR_CallCountingStubData;

class CallCountingStub
{
public:
#if defined(TARGET_AMD64)
    static const int CodeSize = 24;
#elif defined(TARGET_X86)
    static const int CodeSize = 24;
#elif defined(TARGET_ARM64)
    static const int CodeSize = 40;
#elif defined(TARGET_ARM)
    static const int CodeSize = 32;
#endif

private:
    UINT8 m_code[CodeSize];

#if defined(TARGET_ARM64) && defined(TARGET_UNIX)
    static void (*CallCountingStubCode)();
#endif

public:
    static const SIZE_T Alignment = sizeof(void *);

protected:
    PTR_CallCountingStubData GetData() const
    {
        return dac_cast<PTR_CallCountingStubData>(dac_cast<TADDR>(this) + GetOsPageSize());
    }

#ifndef DACCESS_COMPILE
    static const PCODE TargetForThresholdReached;

    CallCountingStub() = default;

public:
    static const CallCountingStub *From(TADDR stubIdentifyingToken);

    PCODE GetEntryPoint() const
    {
        WRAPPER_NO_CONTRACT;
        return PINSTRToPCODE((TADDR)this);
    }
#endif // !DACCESS_COMPILE

public:

#ifndef DACCESS_COMPILE
    void Initialize(PCODE targetForMethod, CallCount* remainingCallCountCell)
    {
        PTR_CallCountingStubData pStubData = GetData();
        pStubData->RemainingCallCountCell = remainingCallCountCell;
        pStubData->TargetForMethod = targetForMethod;
        pStubData->TargetForThresholdReached = CallCountingStub::TargetForThresholdReached;
    }

    static void StaticInitialize();
#endif // !DACCESS_COMPILE

    static void GenerateCodePage(BYTE* pageBase, BYTE* pageBaseRX);

    PTR_CallCount GetRemainingCallCountCell() const;
    PCODE GetTargetForMethod() const;

protected:

    DISABLE_COPY(CallCountingStub);
};

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// CallCountingManager

class CallCountingManager;
typedef DPTR(CallCountingManager) PTR_CallCountingManager;

class CallCountingManager
{
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    // CallCountingManager::CallCountingInfo

private:
    class CallCountingInfo;
    typedef DPTR(CallCountingInfo) PTR_CallCountingInfo;

    class CallCountingInfo
    {
    public:
        enum class Stage : UINT8
        {
            // Stub is definitely not going to be called, stub may be deleted
            StubIsNotActive,

            // Stub may be called, don't know if it's actually active (changes to code versions, etc.)
            StubMayBeActive,

            // Stub may be active, call counting complete, not yet promoted
            PendingCompletion,

            // Stub is not active and will not become active, call counting complete, promoted, stub may be deleted
            Complete,

            // Call counting is disabled, only used for the default code version to indicate that it is to be optimized
            Disabled
        };

    private:
        const NativeCodeVersion m_codeVersion;
        const CallCountingStub *m_callCountingStub;
        CallCount m_remainingCallCount;
        Stage m_stage;

    #ifndef DACCESS_COMPILE
    private:
        CallCountingInfo(NativeCodeVersion codeVersion);
    public:
        static CallCountingInfo *CreateWithCallCountingDisabled(NativeCodeVersion codeVersion);
        CallCountingInfo(NativeCodeVersion codeVersion, CallCount callCountThreshold);
        ~CallCountingInfo();
    #endif

    public:
        static PTR_CallCountingInfo From(PTR_CallCount remainingCallCountCell);
        NativeCodeVersion GetCodeVersion() const;

    #ifndef DACCESS_COMPILE
    public:
        const CallCountingStub *GetCallCountingStub() const;
        void SetCallCountingStub(const CallCountingStub *callCountingStub);
        void ClearCallCountingStub();
        CallCount *GetRemainingCallCountCell();
    #endif

    public:
        Stage GetStage() const;
    #ifndef DACCESS_COMPILE
    public:
        void SetStage(Stage stage);
    #endif

        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // CallCountingManager::CallCountingInfo::CodeVersionHashTraits

    public:
        class CodeVersionHashTraits : public DefaultSHashTraits<PTR_CallCountingInfo>
        {
        private:
            typedef DefaultSHashTraits<PTR_CallCountingInfo> Base;
        public:
            typedef Base::element_t element_t;
            typedef Base::count_t count_t;
            typedef const NativeCodeVersion key_t;

        public:
            static key_t GetKey(const element_t &e);
            static BOOL Equals(const key_t &k1, const key_t &k2);
            static count_t Hash(const key_t &k);
        };
    };

    typedef SHash<CallCountingInfo::CodeVersionHashTraits> CallCountingInfoByCodeVersionHash;

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    // CallCountingManager::CallCountingStubAllocator

private:
    class CallCountingStubAllocator
    {
    private:
        // LoaderHeap cannot be constructed when DACCESS_COMPILE is defined (at the time, its destructor was private). Working
        // around that by controlling creation/destruction using a pointer.
        LoaderHeap *m_heap;
        RangeList m_heapRangeList;

    public:
        CallCountingStubAllocator();
        ~CallCountingStubAllocator();

    #ifndef DACCESS_COMPILE
    public:
        void Reset();
        const CallCountingStub *AllocateStub(CallCount *remainingCallCountCell, PCODE targetForMethod);
    private:
        LoaderHeap *AllocateHeap();
    #endif // !DACCESS_COMPILE

    public:
        bool IsStub(TADDR entryPoint);

    #ifdef DACCESS_COMPILE
        void EnumerateHeapRanges(CLRDataEnumMemoryFlags flags);
    #endif

        DISABLE_COPY(CallCountingStubAllocator);
    };

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    // CallCountingManager::MethodDescForwarderStub

private:
    class MethodDescForwarderStubHashTraits : public DefaultSHashTraits<Precode *>
    {
    private:
        typedef DefaultSHashTraits<Precode *> Base;
    public:
        typedef Base::element_t element_t;
        typedef Base::count_t count_t;
        typedef MethodDesc *key_t;

    public:
        static key_t GetKey(const element_t &e);
        static BOOL Equals(const key_t &k1, const key_t &k2);
        static count_t Hash(const key_t &k);
    };

    typedef SHash<MethodDescForwarderStubHashTraits> MethodDescForwarderStubHash;

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    // CallCountingManager::CallCountingManagerHashTraits

private:
    class CallCountingManagerHashTraits : public DefaultSHashTraits<PTR_CallCountingManager>
    {
    private:
        typedef DefaultSHashTraits<PTR_CallCountingManager> Base;
    public:
        typedef Base::element_t element_t;
        typedef Base::count_t count_t;
        typedef PTR_CallCountingManager key_t;

    public:
        static key_t GetKey(const element_t &e);
        static BOOL Equals(const key_t &k1, const key_t &k2);
        static count_t Hash(const key_t &k);
    };

    typedef SHash<CallCountingManagerHashTraits> CallCountingManagerHash;
    typedef DPTR(CallCountingManagerHash) PTR_CallCountingManagerHash;

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    // CallCountingManager members

private:
    static PTR_CallCountingManagerHash s_callCountingManagers;
    static COUNT_T s_callCountingStubCount;
    static COUNT_T s_activeCallCountingStubCount;
    static COUNT_T s_completedCallCountingStubCount;

private:
    CallCountingInfoByCodeVersionHash m_callCountingInfoByCodeVersionHash;
    CallCountingStubAllocator m_callCountingStubAllocator;
    MethodDescForwarderStubHash m_methodDescForwarderStubHash;
    SArray<CallCountingInfo *> m_callCountingInfosPendingCompletion;

public:
    CallCountingManager();
    ~CallCountingManager();

#ifndef DACCESS_COMPILE
public:
    static void StaticInitialize();
#endif // !DACCESS_COMPILE

public:
    bool IsCallCountingEnabled(NativeCodeVersion codeVersion);

#ifndef DACCESS_COMPILE
public:
    void DisableCallCounting(NativeCodeVersion codeVersion);

public:
    static bool SetCodeEntryPoint(
        NativeCodeVersion activeCodeVersion,
        PCODE codeEntryPoint,
        bool wasMethodCalled,
        bool *createTieringBackgroundWorker);
    static PCODE OnCallCountThresholdReached(TransitionBlock *transitionBlock, TADDR stubIdentifyingToken);
    static COUNT_T GetCountOfCodeVersionsPendingCompletion();
    static void CompleteCallCounting();

public:
    static void StopAndDeleteAllCallCountingStubs();
    static const CallCountingStub* GetCallCountingStub(CallCount *pCallCount)
    {
        return CallCountingInfo::From(pCallCount)->GetCallCountingStub();
    }
private:
    static void StopAllCallCounting(TieredCompilationManager *tieredCompilationManager);
    static void DeleteAllCallCountingStubs();
    void TrimCollections();
#endif // !DACCESS_COMPILE

public:
    static bool IsCallCountingStub(PCODE entryPoint);
    static PCODE GetTargetForMethod(PCODE callCountingStubEntryPoint);
#ifdef DACCESS_COMPILE
    static void DacEnumerateCallCountingStubHeapRanges(CLRDataEnumMemoryFlags flags);
#endif

    DISABLE_COPY(CallCountingManager);
};

////////////////////////////////////////////////////////////////
// CallCountingStub definitions

#ifndef DACCESS_COMPILE
inline const CallCountingStub *CallCountingStub::From(TADDR stubIdentifyingToken)
{
    WRAPPER_NO_CONTRACT;
    _ASSERTE(stubIdentifyingToken != NULL);

    // The stubIdentifyingToken is the pointer to the CallCount
    const CallCountingStub *stub = CallCountingManager::GetCallCountingStub((CallCount*)stubIdentifyingToken);

    _ASSERTE(IS_ALIGNED(stub, Alignment));
    return stub;
}
#endif

inline PTR_CallCount CallCountingStub::GetRemainingCallCountCell() const
{
    WRAPPER_NO_CONTRACT;
    return GetData()->RemainingCallCountCell;
}

inline PCODE CallCountingStub::GetTargetForMethod() const
{
    WRAPPER_NO_CONTRACT;
    return GetData()->TargetForMethod;
}

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// CallCountingManager::CallCountingStubManager

class CallCountingStubManager;
typedef VPTR(CallCountingStubManager) PTR_CallCountingStubManager;

class CallCountingStubManager : public StubManager
{
    VPTR_VTABLE_CLASS(CallCountingStubManager, StubManager);

private:
    SPTR_DECL(CallCountingStubManager, g_pManager);

#ifndef DACCESS_COMPILE
public:
    CallCountingStubManager();

public:
    static void Init();
#endif

#ifdef _DEBUG
public:
    virtual const char *DbgGetName(); // override
#endif

#ifdef DACCESS_COMPILE
public:
    virtual LPCWSTR GetStubManagerName(PCODE addr);
#endif

protected:
    virtual BOOL CheckIsStub_Internal(PCODE entryPoint); // override
    virtual BOOL DoTraceStub(PCODE callCountingStubEntryPoint, TraceDestination *trace); // override

#ifdef DACCESS_COMPILE
protected:
    virtual void DoEnumMemoryRegions(CLRDataEnumMemoryFlags flags); // override
#endif

    DISABLE_COPY(CallCountingStubManager);
};

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

#undef DISABLE_COPY

#endif // FEATURE_TIERED_COMPILATION
