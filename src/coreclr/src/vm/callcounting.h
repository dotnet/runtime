// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#pragma once

#include "codeversion.h"

#ifdef FEATURE_TIERED_COMPILATION

#define DISABLE_COPY(T) \
    T(const T &) = delete; \
    T &operator =(const T &) = delete

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// CallCountingManager

class CallCountingManager;
typedef DPTR(CallCountingManager) PTR_CallCountingManager;

class CallCountingManager
{
public:
    typedef UINT16 CallCount;

private:
    class CallCountingInfo;
    typedef DPTR(CallCountingInfo) PTR_CallCountingInfo;

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    // CallCountingManager::CallCountingStub

private:
    class CallCountingStub
    {
    public:
        static const SIZE_T Alignment;

    #ifndef DACCESS_COMPILE
    protected:
        static const PCODE TargetForThresholdReached;

    protected:
        CallCountingStub() = default;

    #if defined(_TARGET_AMD64_) || defined(_TARGET_X86_)
    protected:
        template<class T> static INT_PTR GetRelativeOffset(const T *relRef, PCODE target);
    #endif
    #endif // !DACCESS_COMPILE

    #if defined(_TARGET_AMD64_) || defined(_TARGET_X86_)
    protected:
        template<class T> static PCODE GetTarget(const T *relRef);
    #endif

    #ifndef DACCESS_COMPILE
    public:
        static const CallCountingStub *From(TADDR stubIdentifyingToken);

    public:
        PCODE GetEntryPoint() const;
    #endif // !DACCESS_COMPILE

    public:
        CallCountingInfo *GetCallCountingInfo() const;
        PCODE GetTargetForMethod() const;

        DISABLE_COPY(CallCountingStub);
    };

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    // CallCountingManager::CallCountingStubLong

private:
    class CallCountingStubLong;

#pragma pack(push, 1)
private:
    class CallCountingStubShort : public CallCountingStub
    {
    #if defined(_TARGET_AMD64_)
    private:
        const UINT8 m_part0[2];
        CallCount *const m_remainingCallCountCell;
        const UINT8 m_part1[5];
        const INT32 m_rel32TargetForMethod;
        const UINT8 m_part2[1];
        const INT32 m_rel32TargetForThresholdReached;
        const UINT8 m_alignmentPadding[0];
    #elif defined(_TARGET_X86_)
    private:
        const UINT8 m_part0[1];
        CallCount *const m_remainingCallCountCell;
        const UINT8 m_part1[5];
        const INT32 m_rel32TargetForMethod;
        const UINT8 m_part2[1];
        const INT32 m_rel32TargetForThresholdReached;
        const UINT8 m_alignmentPadding[1];
    #elif defined(_TARGET_ARM64_)
    private:
        const UINT32 m_part0[10];
        CallCount *const m_remainingCallCountCell;
        const PCODE m_targetForMethod;
        const PCODE m_targetForThresholdReached;
    #elif defined(_TARGET_ARM_)
    private:
        const UINT16 m_part0[16];
        CallCount *const m_remainingCallCountCell;
        const PCODE m_targetForMethod;
        const PCODE m_targetForThresholdReached;
    #else
        #error Unknown processor architecture
    #endif // processor architectures

    #ifndef DACCESS_COMPILE
    public:
        CallCountingStubShort(CallCount *remainingCallCountCell, PCODE targetForMethod);

    public:
        static bool Is(TADDR stubIdentifyingToken);
        static const CallCountingStubShort *From(TADDR stubIdentifyingToken);
    #endif

    public:
        static bool Is(const CallCountingStub *callCountingStub);
        static const CallCountingStubShort *From(const CallCountingStub *callCountingStub);

    #ifndef DACCESS_COMPILE
    #ifdef _TARGET_AMD64_
    private:
        static bool CanUseRelative32BitOffset(const INT32 *rel32Ref, PCODE target);
    public:
        static bool CanUseFor(const void *allocationAddress, PCODE targetForMethod);
    #endif

    #if defined(_TARGET_AMD64_) || defined(_TARGET_X86_)
    private:
        static INT32 GetRelative32BitOffset(const INT32 *rel32Ref, PCODE target);
    #endif
    #endif // !DACCESS_COMPILE

    public:
        PCODE GetTargetForMethod() const;

        friend CallCountingStub;
        friend CallCountingStubLong;
        DISABLE_COPY(CallCountingStubShort);
    };
#pragma pack(pop)

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    // CallCountingManager::CallCountingStubLong

#ifdef _TARGET_AMD64_
#pragma pack(push, 1)
private:
    class CallCountingStubLong : public CallCountingStub
    {
    private:
        const UINT8 m_part0[2];
        CallCount *const m_remainingCallCountCell;
        const UINT8 m_part1[7];
        const PCODE m_targetForMethod;
        const UINT8 m_part2[4];
        const PCODE m_targetForThresholdReached;
        const UINT8 m_part3[2];
        const UINT8 m_alignmentPadding[1];

    #ifndef DACCESS_COMPILE
    public:
        CallCountingStubLong(CallCount *remainingCallCountCell, PCODE targetForMethod);

    public:
        static bool Is(TADDR stubIdentifyingToken);
        static const CallCountingStubLong *From(TADDR stubIdentifyingToken);
    #endif // !DACCESS_COMPILE

    public:
        static bool Is(const CallCountingStub *callCountingStub);
        static const CallCountingStubLong *From(const CallCountingStub *callCountingStub);

    public:
        PCODE GetTargetForMethod() const;

        friend CallCountingStub;
        DISABLE_COPY(CallCountingStubLong);
    };
#pragma pack(pop)
#endif // _TARGET_AMD64_

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    // CallCountingManager::CallCountingInfo

private:
    class CallCountingInfo
    {
    public:
        enum class Stage : UINT8
        {
            // Stub is definitely not going to be called, stub may be deleted
            StubIsNotActive = 0,

            // Stub may be called, don't know if it's actually active (changes to code versions, etc.)
            StubMayBeActive = 1,

            // Stub may be active, call counting complete, not yet promoted
            PendingCompletion = 2,

            // Stub is not active and will not become active, call counting complete, promoted, stub may be deleted
            Complete = 3,

            BitCount = 2,
            BitMask = (1 << BitCount) - 1
        };

    private:
        static const UINT16 CallCountingDisabledState;

    private:
        const NativeCodeVersion m_codeVersion;
        const CallCountingStub *m_callCountingStub;
        CallCount m_remainingCallCount;
        UINT16 m_state;

    #ifndef DACCESS_COMPILE
    private:
        CallCountingInfo(NativeCodeVersion codeVersion);
    public:
        static CallCountingInfo *CreateWithCallCountingDisabled(NativeCodeVersion codeVersion);
        CallCountingInfo(NativeCodeVersion codeVersion, CallCount callCountThreshold);
        ~CallCountingInfo();
    #endif

    public:
        static CallCountingInfo *From(CallCount *remainingCallCountCell);
        NativeCodeVersion GetCodeVersion() const;
        bool IsCallCountingEnabled() const;

    #ifndef DACCESS_COMPILE
    public:
        const CallCountingStub *GetCallCountingStub() const;
        void SetCallCountingStub(const CallCountingStub *callCountingStub);
        void ClearCallCountingStub();
        CallCount *GetRemainingCallCountCell();
        CallCount GetCallCountThreshold() const;
        Stage GetStage() const;
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
    class MethodDescForwarderStub
    {
    private:
        MethodDesc *m_methodDesc;
        Precode *m_forwarderStub;

    #ifndef DACCESS_COMPILE
    public:
        MethodDescForwarderStub();
        MethodDescForwarderStub(MethodDesc *methodDesc, Precode *forwarderStub);
    #endif // !DACCESS_COMPILE

    public:
        MethodDesc *GetMethodDesc() const;

    #ifndef DACCESS_COMPILE
    public:
        Precode *GetForwarderStub() const;
    #endif // !DACCESS_COMPILE

        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // CallCountingManager::MethodDescForwarderStub::MethodDescHashTraits

    public:
        class MethodDescHashTraits : public NoRemoveSHashTraits<DefaultSHashTraits<MethodDescForwarderStub>>
        {
        private:
            typedef NoRemoveSHashTraits<DefaultSHashTraits<MethodDescForwarderStub>> Base;
        public:
            typedef Base::element_t element_t;
            typedef Base::count_t count_t;
            typedef MethodDesc *key_t;

        public:
            static key_t GetKey(const element_t &e);
            static BOOL Equals(const key_t &k1, const key_t &k2);
            static count_t Hash(const key_t &k);
            static element_t Null();
            static bool IsNull(const element_t &e);
        };
    };

    typedef SHash<MethodDescForwarderStub::MethodDescHashTraits> MethodDescForwarderStubHash;

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

public:
    static const CallCount MaximumCallCountThreshold = UINT16_MAX >> (UINT8)CallCountingInfo::Stage::BitCount;

private:
    static PTR_CallCountingManagerHash s_callCountingManagers;
    static MethodDescForwarderStubHash *s_methodDescForwarderStubHash;
    static SArray<CallCountingInfo *> s_callCountingInfosPendingCompletion;
    static COUNT_T s_callCountingStubCount;
    static COUNT_T s_activeCallCountingStubCount;
    static COUNT_T s_completedCallCountingStubCount;

private:
    CallCountingInfoByCodeVersionHash m_callCountingInfoByCodeVersionHash;
    CallCountingStubAllocator m_callCountingStubAllocator;

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
        bool *scheduleTieringBackgroundWorkRef);
    static PCODE OnCallCountThresholdReached(TransitionBlock *transitionBlock, TADDR stubIdentifyingToken);
    static void CompleteCallCounting();

public:
    static bool MayDeleteCallCountingStubs()
    {
        WRAPPER_NO_CONTRACT;

        // If a number of call counting stubs have completed, we can try to delete them to reclaim some memory. Deleting
        // involves suspending the runtime and will delete all call counting stubs, and after that some call counting stubs may
        // be recreated in the foreground. The threshold is to decrease the impact of both of those overheads.
        COUNT_T deleteCallCountingStubsAfter = g_pConfig->TieredCompilation_DeleteCallCountingStubsAfter();
        return deleteCallCountingStubsAfter != 0 && s_completedCallCountingStubCount >= deleteCallCountingStubsAfter;
    }

public:
    static void StopAndDeleteAllCallCountingStubs();
private:
    static void StopAllCallCounting(TieredCompilationManager *tieredCompilationManager, bool *scheduleTieringBackgroundWorkRef);
    static void DeleteAllCallCountingStubs();
#endif // !DACCESS_COMPILE

public:
    static bool IsCallCountingStub(PCODE entryPoint);
    static PCODE GetTargetForMethod(PCODE callCountingStubEntryPoint);
#ifdef DACCESS_COMPILE
    static void DacEnumerateCallCountingStubHeapRanges(CLRDataEnumMemoryFlags flags);
#endif

    DISABLE_COPY(CallCountingManager);
};

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
