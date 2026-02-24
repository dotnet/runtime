// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "common.h"

#ifdef FEATURE_TIERED_COMPILATION

#include "callcounting.h"
#include "threadsuspend.h"
#include <minipal/memorybarrierprocesswide.h>

#ifndef DACCESS_COMPILE
extern "C" void STDCALL OnCallCountThresholdReachedStub();
#endif

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// CallCountingStub

#ifndef DACCESS_COMPILE
const PCODE CallCountingStub::TargetForThresholdReached = (PCODE)GetEEFuncEntryPoint(OnCallCountThresholdReachedStub);
#endif

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// CallCountingManager::CallCountingInfo

#ifndef DACCESS_COMPILE

CallCountingManager::CallCountingInfo::CallCountingInfo(NativeCodeVersion codeVersion)
    : m_codeVersion(codeVersion),
    m_callCountingStub(nullptr),
    m_remainingCallCount(0),
    m_stage(Stage::Disabled)
{
    WRAPPER_NO_CONTRACT;
    _ASSERTE(!codeVersion.IsNull());
}

CallCountingManager::CallCountingInfo *
CallCountingManager::CallCountingInfo::CreateWithCallCountingDisabled(NativeCodeVersion codeVersion)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    return new CallCountingInfo(codeVersion);
}

CallCountingManager::CallCountingInfo::CallCountingInfo(NativeCodeVersion codeVersion, CallCount callCountThreshold)
    : m_codeVersion(codeVersion),
    m_callCountingStub(nullptr),
    m_remainingCallCount(callCountThreshold),
    m_stage(Stage::StubIsNotActive)
{
    WRAPPER_NO_CONTRACT;
    _ASSERTE(!codeVersion.IsNull());
    _ASSERTE(callCountThreshold != 0);
}

CallCountingManager::CallCountingInfo::~CallCountingInfo()
{
    WRAPPER_NO_CONTRACT;
    _ASSERTE(m_stage == Stage::Complete);
    _ASSERTE(m_callCountingStub == nullptr);
}

#endif // !DACCESS_COMPILE

CallCountingManager::PTR_CallCountingInfo CallCountingManager::CallCountingInfo::From(PTR_CallCount remainingCallCountCell)
{
    WRAPPER_NO_CONTRACT;
    _ASSERTE(remainingCallCountCell != nullptr);

    return PTR_CallCountingInfo(dac_cast<TADDR>(remainingCallCountCell) - offsetof(CallCountingInfo, m_remainingCallCount));
}

NativeCodeVersion CallCountingManager::CallCountingInfo::GetCodeVersion() const
{
    WRAPPER_NO_CONTRACT;
    return m_codeVersion;
}

#ifndef DACCESS_COMPILE

const CallCountingStub *CallCountingManager::CallCountingInfo::GetCallCountingStub() const
{
    WRAPPER_NO_CONTRACT;
    _ASSERTE(m_stage != Stage::Disabled);

    return m_callCountingStub;
}

void CallCountingManager::CallCountingInfo::SetCallCountingStub(const CallCountingStub *callCountingStub)
{
    WRAPPER_NO_CONTRACT;
    _ASSERTE(g_pConfig->TieredCompilation_UseCallCountingStubs());
    _ASSERTE(m_stage == Stage::StubIsNotActive);
    _ASSERTE(m_callCountingStub == nullptr);
    _ASSERTE(callCountingStub != nullptr);

    ++s_callCountingStubCount;
    m_callCountingStub = callCountingStub;
}

void CallCountingManager::CallCountingInfo::ClearCallCountingStub()
{
    WRAPPER_NO_CONTRACT;
    _ASSERTE(m_stage == Stage::StubIsNotActive || m_stage == Stage::Complete);
    _ASSERTE(m_callCountingStub != nullptr);

    m_callCountingStub = nullptr;
    // The total and completed stub counts are updated along with deleting stubs
}

PTR_CallCount CallCountingManager::CallCountingInfo::GetRemainingCallCountCell()
{
    WRAPPER_NO_CONTRACT;
    _ASSERTE(m_stage != Stage::Disabled);
    //_ASSERTE(m_callCountingStub != nullptr);

    return &m_remainingCallCount;
}

#endif // !DACCESS_COMPILE

CallCountingManager::CallCountingInfo::Stage CallCountingManager::CallCountingInfo::GetStage() const
{
    WRAPPER_NO_CONTRACT;
    return m_stage;
}

#ifndef DACCESS_COMPILE
FORCEINLINE void CallCountingManager::CallCountingInfo::SetStage(Stage stage)
{
    WRAPPER_NO_CONTRACT;
    _ASSERTE(m_stage != Stage::Disabled);
    _ASSERTE(stage <= Stage::Complete);

    switch (stage)
    {
        case Stage::StubIsNotActive:
            _ASSERTE(m_stage == Stage::StubMayBeActive);
            _ASSERTE(m_callCountingStub != nullptr);
            _ASSERTE(s_activeCallCountingStubCount != 0);
            --s_activeCallCountingStubCount;
            break;

        case Stage::StubMayBeActive:
            _ASSERTE(m_callCountingStub != nullptr);
            FALLTHROUGH;

        case Stage::PendingCompletion:
            _ASSERTE(m_stage == Stage::StubIsNotActive || m_stage == Stage::StubMayBeActive);
            if (m_stage == Stage::StubIsNotActive && m_callCountingStub != nullptr)
            {
                ++s_activeCallCountingStubCount;
            }
            break;

        case Stage::Complete:
            _ASSERTE(m_stage != Stage::Complete);
            if (m_callCountingStub != nullptr)
            {
                if (m_stage != Stage::StubIsNotActive)
                {
                    _ASSERTE(s_activeCallCountingStubCount != 0);
                    --s_activeCallCountingStubCount;
                }
                ++s_completedCallCountingStubCount;
            }
            break;

        default:
            UNREACHABLE();
    }

    m_stage = stage;
}
#endif

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// CallCountingManager::CallCountingInfo::CodeVersionHashTraits

CallCountingManager::CallCountingInfo::CodeVersionHashTraits::key_t
CallCountingManager::CallCountingInfo::CodeVersionHashTraits::GetKey(const element_t &e)
{
    WRAPPER_NO_CONTRACT;
    return e->GetCodeVersion();
}

BOOL CallCountingManager::CallCountingInfo::CodeVersionHashTraits::Equals(const key_t &k1, const key_t &k2)
{
    WRAPPER_NO_CONTRACT;
    return k1 == k2;
}

CallCountingManager::CallCountingInfo::CodeVersionHashTraits::count_t
CallCountingManager::CallCountingInfo::CodeVersionHashTraits::Hash(const key_t &k)
{
    WRAPPER_NO_CONTRACT;
    return (count_t)dac_cast<TADDR>(k.GetMethodDesc()) + k.GetVersionId();
}

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// CallCountingManager::CallCountingStubAllocator

CallCountingManager::CallCountingStubAllocator::CallCountingStubAllocator() : m_heap(nullptr)
{
    WRAPPER_NO_CONTRACT;
}

CallCountingManager::CallCountingStubAllocator::~CallCountingStubAllocator()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

#ifndef DACCESS_COMPILE
    InterleavedLoaderHeap *heap = m_heap;
    if (heap != nullptr)
    {
        delete m_heap;
    }
#endif
}

#ifndef DACCESS_COMPILE

void CallCountingManager::CallCountingStubAllocator::Reset()
{
    WRAPPER_NO_CONTRACT;

    this->~CallCountingStubAllocator();
    new(this) CallCountingStubAllocator();
}

const CallCountingStub *CallCountingManager::CallCountingStubAllocator::AllocateStub(
    CallCount *remainingCallCountCell,
    PCODE targetForMethod)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    InterleavedLoaderHeap *heap = m_heap;
    if (heap == nullptr)
    {
        heap = AllocateHeap();
    }

    AllocMemHolder<void> allocationAddressHolder(heap->AllocStub());
    CallCountingStub *stub = (CallCountingStub*)(void*)allocationAddressHolder;
    allocationAddressHolder.SuppressRelease();
    stub->Initialize(targetForMethod, remainingCallCountCell);

    FlushCacheForDynamicMappedStub(stub, CallCountingStub::CodeSize);

    return stub;
}

#if defined(TARGET_ARM64) && defined(TARGET_UNIX)
    #define ENUM_PAGE_SIZE(size) \
        extern "C" void CallCountingStubCode##size(); \
        extern "C" void CallCountingStubCode##size##_End();

    ENUM_PAGE_SIZES
    #undef ENUM_PAGE_SIZE
#else
extern "C" void CallCountingStubCode();
extern "C" void CallCountingStubCode_End();
#endif

#ifdef TARGET_X86
extern "C" size_t CallCountingStubCode_RemainingCallCountCell_Offset;
extern "C" size_t CallCountingStubCode_TargetForMethod_Offset;
extern "C" size_t CallCountingStubCode_TargetForThresholdReached_Offset;

#define SYMBOL_VALUE(name) ((size_t)&name)

#endif

#if defined(TARGET_ARM64) && defined(TARGET_UNIX)
void (*CallCountingStub::CallCountingStubCode)();
#endif

#ifndef DACCESS_COMPILE

static InterleavedLoaderHeapConfig s_callCountingHeapConfig;

#ifdef FEATURE_MAP_THUNKS_FROM_IMAGE
extern "C" void CallCountingStubCodeTemplate();
#else
#define CallCountingStubCodeTemplate NULL
#endif

void CallCountingStub::StaticInitialize()
{
#if defined(TARGET_ARM64) && defined(TARGET_UNIX)
    int pageSize = GetStubCodePageSize();
    #define ENUM_PAGE_SIZE(size) \
        case size: \
            CallCountingStubCode = CallCountingStubCode##size; \
            _ASSERTE((SIZE_T)((BYTE*)CallCountingStubCode##size##_End - (BYTE*)CallCountingStubCode##size) <= CallCountingStub::CodeSize); \
            break;

    switch (pageSize)
    {
        ENUM_PAGE_SIZES
        default:
            EEPOLICY_HANDLE_FATAL_ERROR_WITH_MESSAGE(COR_E_EXECUTIONENGINE, W("Unsupported OS page size"));
    }
    #undef ENUM_PAGE_SIZE

    if (CallCountingStubCodeTemplate != NULL && pageSize != 0x4000)
    {
        // This should fail if the template is used on a platform which doesn't support the supported page size for templates
        ThrowHR(COR_E_EXECUTIONENGINE);
    }
#elif defined(TARGET_WASM)
    // CallCountingStub is not implemented on WASM
#else
    _ASSERTE((SIZE_T)((BYTE*)CallCountingStubCode_End - (BYTE*)CallCountingStubCode) <= CallCountingStub::CodeSize);
#endif

    InitializeLoaderHeapConfig(&s_callCountingHeapConfig, CallCountingStub::CodeSize, (void*)CallCountingStubCodeTemplate, CallCountingStub::GenerateCodePage, NULL);
}

#endif // DACCESS_COMPILE

void CallCountingStub::GenerateCodePage(uint8_t* pageBase, uint8_t* pageBaseRX, size_t pageSize)
{
#ifdef TARGET_X86
    int totalCodeSize = (pageSize / CallCountingStub::CodeSize) * CallCountingStub::CodeSize;

    for (int i = 0; i < totalCodeSize; i += CallCountingStub::CodeSize)
    {
        memcpy(pageBase + i, (const void*)CallCountingStubCode, CallCountingStub::CodeSize);

        // Set absolute addresses of the slots in the stub
        BYTE* pCounterSlot = pageBaseRX + i + pageSize + offsetof(CallCountingStubData, RemainingCallCountCell);
        *(uint8_t**)(pageBase + i + SYMBOL_VALUE(CallCountingStubCode_RemainingCallCountCell_Offset)) = pCounterSlot;

        BYTE* pTargetSlot = pageBaseRX + i + pageSize + offsetof(CallCountingStubData, TargetForMethod);
        *(uint8_t**)(pageBase + i + SYMBOL_VALUE(CallCountingStubCode_TargetForMethod_Offset)) = pTargetSlot;

        BYTE* pCountReachedZeroSlot = pageBaseRX + i + pageSize + offsetof(CallCountingStubData, TargetForThresholdReached);
        *(uint8_t**)(pageBase + i + SYMBOL_VALUE(CallCountingStubCode_TargetForThresholdReached_Offset)) = pCountReachedZeroSlot;
    }
#else // TARGET_X86
    FillStubCodePage(pageBase, (const void*)PCODEToPINSTR((PCODE)CallCountingStubCode), CallCountingStub::CodeSize, pageSize);
#endif
}


NOINLINE InterleavedLoaderHeap *CallCountingManager::CallCountingStubAllocator::AllocateHeap()
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    _ASSERTE(m_heap == nullptr);

    InterleavedLoaderHeap *heap = new InterleavedLoaderHeap(&m_heapRangeList, true /* fUnlocked */, &s_callCountingHeapConfig);
    m_heap = heap;
    return heap;
}

#endif // !DACCESS_COMPILE

bool CallCountingManager::CallCountingStubAllocator::IsStub(TADDR entryPoint)
{
    WRAPPER_NO_CONTRACT;
    _ASSERTE(entryPoint != (TADDR)NULL);

    return !!m_heapRangeList.IsInRange(entryPoint);
}

#ifdef DACCESS_COMPILE

void CallCountingManager::CallCountingStubAllocator::EnumerateHeapRanges(CLRDataEnumMemoryFlags flags)
{
    WRAPPER_NO_CONTRACT;
    m_heapRangeList.EnumMemoryRegions(flags);
}

#endif // DACCESS_COMPILE

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// CallCountingManager::CallCountingManagerHashTraits

CallCountingManager::CallCountingManagerHashTraits::key_t
CallCountingManager::CallCountingManagerHashTraits::GetKey(const element_t &e)
{
    WRAPPER_NO_CONTRACT;
    return e;
}

BOOL CallCountingManager::CallCountingManagerHashTraits::Equals(const key_t &k1, const key_t &k2)
{
    WRAPPER_NO_CONTRACT;
    return k1 == k2;
}

CallCountingManager::CallCountingManagerHashTraits::count_t
CallCountingManager::CallCountingManagerHashTraits::Hash(const key_t &k)
{
    WRAPPER_NO_CONTRACT;
    return (count_t)dac_cast<TADDR>(k);
}

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// CallCountingManager

CallCountingManager::PTR_CallCountingManagerHash CallCountingManager::s_callCountingManagers = PTR_NULL;
COUNT_T CallCountingManager::s_callCountingStubCount = 0;
COUNT_T CallCountingManager::s_activeCallCountingStubCount = 0;
COUNT_T CallCountingManager::s_completedCallCountingStubCount = 0;

CallCountingManager::CallCountingManager()
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

#ifndef DACCESS_COMPILE
    CodeVersionManager::LockHolder codeVersioningLockHolder;
    s_callCountingManagers->Add(this);
#endif
}

CallCountingManager::~CallCountingManager()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

#ifndef DACCESS_COMPILE
    CodeVersionManager::LockHolder codeVersioningLockHolder;

    for (auto itEnd = m_callCountingInfoByCodeVersionHash.End(), it = m_callCountingInfoByCodeVersionHash.Begin();
        it != itEnd;
        ++it)
    {
        CallCountingInfo *callCountingInfo = *it;
        delete callCountingInfo;
    }

    s_callCountingManagers->Remove(this);
#endif
}

#ifndef DACCESS_COMPILE

void CallCountingManager::StaticInitialize()
{
    WRAPPER_NO_CONTRACT;
    s_callCountingManagers = PTR_CallCountingManagerHash(new CallCountingManagerHash());
    CallCountingStub::StaticInitialize();
}
#endif

bool CallCountingManager::IsCallCountingEnabled(NativeCodeVersion codeVersion)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    _ASSERTE(!codeVersion.IsNull());
    _ASSERTE(codeVersion.IsDefaultVersion());
    _ASSERTE(codeVersion.GetMethodDesc()->IsEligibleForTieredCompilation());

    CodeVersionManager::LockHolder codeVersioningLockHolder;

    PTR_CallCountingInfo callCountingInfo = m_callCountingInfoByCodeVersionHash.Lookup(codeVersion);
    return callCountingInfo == NULL || callCountingInfo->GetStage() != CallCountingInfo::Stage::Disabled;
}

#ifndef DACCESS_COMPILE

void CallCountingManager::DisableCallCounting(NativeCodeVersion codeVersion)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    _ASSERTE(!codeVersion.IsNull());
    _ASSERTE(codeVersion.IsDefaultVersion());
    _ASSERTE(codeVersion.GetMethodDesc()->IsEligibleForTieredCompilation());

    CodeVersionManager::LockHolder codeVersioningLockHolder;

    CallCountingInfo *callCountingInfo = m_callCountingInfoByCodeVersionHash.Lookup(codeVersion);
    if (callCountingInfo != nullptr)
    {
        // Call counting may already have been disabled due to the possibility of concurrent or reentering JIT of the same
        // native code version of a method. The call counting info is created with call counting enabled or disabled and it
        // cannot be changed thereafter for consistency in dependents of the info.
        _ASSERTE(callCountingInfo->GetStage() == CallCountingInfo::Stage::Disabled);
        return;
    }

    NewHolder<CallCountingInfo> callCountingInfoHolder = CallCountingInfo::CreateWithCallCountingDisabled(codeVersion);
    m_callCountingInfoByCodeVersionHash.Add(callCountingInfoHolder);
    callCountingInfoHolder.SuppressRelease();
}

// Returns true if the code entry point was updated to reflect the active code version, false otherwise. In normal paths, the
// code entry point is not updated only when the use of call counting stubs is disabled, as in that case returning to the
// prestub is necessary for further call counting. On exception, the code entry point may or may not have been updated and it's
// up to the caller to decide how to proceed.
bool CallCountingManager::SetCodeEntryPoint(
    NativeCodeVersion activeCodeVersion,
    PCODE codeEntryPoint,
    bool wasMethodCalled,
    bool *createTieringBackgroundWorkerRef)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        PRECONDITION(!activeCodeVersion.IsNull());
        MODE_ANY;
    }
    CONTRACTL_END;

    MethodDesc *methodDesc = activeCodeVersion.GetMethodDesc();
    _ASSERTE(!methodDesc->MayHaveEntryPointSlotsToBackpatch() || MethodDescBackpatchInfoTracker::IsLockOwnedByCurrentThread());
    _ASSERTE(CodeVersionManager::IsLockOwnedByCurrentThread());
    _ASSERTE(
        activeCodeVersion ==
        methodDesc->GetCodeVersionManager()->GetActiveILCodeVersion(methodDesc).GetActiveNativeCodeVersion(methodDesc));
    _ASSERTE(codeEntryPoint != (PCODE)NULL);
    _ASSERTE(codeEntryPoint == activeCodeVersion.GetNativeCode());
    _ASSERTE(!wasMethodCalled || createTieringBackgroundWorkerRef != nullptr);
    _ASSERTE(createTieringBackgroundWorkerRef == nullptr || !*createTieringBackgroundWorkerRef);

    if (!methodDesc->IsEligibleForTieredCompilation() ||
        (
            // For a default code version that is not tier 0, call counting will have been disabled by this time (checked
            // below). Avoid the redundant and not-insignificant expense of GetOptimizationTier() on a default code version.
            !activeCodeVersion.IsDefaultVersion() &&
            activeCodeVersion.IsFinalTier()
        ) ||
        !g_pConfig->TieredCompilation_CallCounting())
    {
        methodDesc->SetCodeEntryPoint(codeEntryPoint);
        return true;
    }

    const CallCountingStub *callCountingStub;
    CallCountingManager *callCountingManager = methodDesc->GetLoaderAllocator()->GetCallCountingManager();
    CallCountingInfoByCodeVersionHash &callCountingInfoByCodeVersionHash =
        callCountingManager->m_callCountingInfoByCodeVersionHash;
    CallCountingInfo *callCountingInfo = callCountingInfoByCodeVersionHash.Lookup(activeCodeVersion);
    do
    {
        if (callCountingInfo != nullptr)
        {
            _ASSERTE(callCountingInfo->GetCodeVersion() == activeCodeVersion);

            CallCountingInfo::Stage callCountingStage = callCountingInfo->GetStage();
            if (callCountingStage >= CallCountingInfo::Stage::PendingCompletion)
            {
                // Call counting is disabled, complete, or pending completion. The pending completion stage here would be
                // relatively rare, let it be handled elsewhere.
                if (methodDesc->MayHaveEntryPointSlotsToBackpatch())
                {
                    Precode *precode = Precode::GetPrecodeFromEntryPoint(methodDesc->GetTemporaryEntryPoint());
                    precode->SetTargetInterlocked(codeEntryPoint, FALSE);
                }
                else
                {
                    methodDesc->SetCodeEntryPoint(codeEntryPoint);
                }
                return true;
            }

            _ASSERTE(!activeCodeVersion.IsFinalTier());

            // If the tiering delay is active, postpone further work
            if (GetAppDomain()
                    ->GetTieredCompilationManager()
                    ->TrySetCodeEntryPointAndRecordMethodForCallCounting(methodDesc, codeEntryPoint))
            {
                if (callCountingStage == CallCountingInfo::Stage::StubMayBeActive)
                {
                    callCountingInfo->SetStage(CallCountingInfo::Stage::StubIsNotActive);
                }
                return true;
            }

            do
            {
                if (!wasMethodCalled)
                {
                    break;
                }

                CallCount remainingCallCount = --*callCountingInfo->GetRemainingCallCountCell();
                if (remainingCallCount != 0)
                {
                    break;
                }

                callCountingInfo->SetStage(CallCountingInfo::Stage::PendingCompletion);
                if (!activeCodeVersion.GetILCodeVersion().HasAnyOptimizedNativeCodeVersion(activeCodeVersion))
                {
                    GetAppDomain()
                        ->GetTieredCompilationManager()
                        ->AsyncPromoteToTier1(activeCodeVersion, createTieringBackgroundWorkerRef);
                }
                if (methodDesc->MayHaveEntryPointSlotsToBackpatch())
                {
                    Precode *precode = Precode::GetPrecodeFromEntryPoint(methodDesc->GetTemporaryEntryPoint());
                    precode->SetTargetInterlocked(codeEntryPoint, FALSE);
                }
                else
                {
                    methodDesc->SetCodeEntryPoint(codeEntryPoint);
                }
                callCountingInfo->SetStage(CallCountingInfo::Stage::Complete);
                return true;
            } while (false);

            callCountingStub = callCountingInfo->GetCallCountingStub();
            if (callCountingStub != nullptr)
            {
                break;
            }
        }
        else
        {
            _ASSERTE(!activeCodeVersion.IsFinalTier());

            // If the tiering delay is active, postpone further work
            if (GetAppDomain()
                    ->GetTieredCompilationManager()
                    ->TrySetCodeEntryPointAndRecordMethodForCallCounting(methodDesc, codeEntryPoint))
            {
                return true;
            }

            CallCount callCountThreshold = g_pConfig->TieredCompilation_CallCountThreshold();
            _ASSERTE(callCountThreshold != 0);

            // Let's tier up all cast helpers faster than other methods. This is because we want to import them as
            // direct calls in codegen and they need to be promoted earlier than their callers.
            if (methodDesc->GetMethodTable() == g_pCastHelpers)
            {
                callCountThreshold = max<CallCount>(1, (CallCount)(callCountThreshold / 2));
            }

            NewHolder<CallCountingInfo> callCountingInfoHolder = new CallCountingInfo(activeCodeVersion, callCountThreshold);
            callCountingInfoByCodeVersionHash.Add(callCountingInfoHolder);
            callCountingInfo = callCountingInfoHolder.Extract();
        }

        if (!g_pConfig->TieredCompilation_UseCallCountingStubs())
        {
            // Call counting is not yet complete, so reset or don't set the code entry point to continue counting calls

            if (wasMethodCalled)
            {
                return false;
            }

            // This path is reached after activating a code version when publishing its code entry point. The method may
            // currently be pointing to the code entry point of a different code version, so an explicit reset is necessary.
            methodDesc->ResetCodeEntryPoint();
            return true;
        }

        callCountingStub =
            callCountingManager->m_callCountingStubAllocator.AllocateStub(
                callCountingInfo->GetRemainingCallCountCell(),
                codeEntryPoint);
        callCountingInfo->SetCallCountingStub(callCountingStub);
    } while (false);

    PCODE callCountingCodeEntryPoint = callCountingStub->GetEntryPoint();
    if (methodDesc->MayHaveEntryPointSlotsToBackpatch())
    {
        // For methods that may have entry point slots to backpatch, redirect the method's temporary entry point
        // (precode) to the call counting stub. This reuses the method's own precode as the stable indirection,
        // avoiding the need to allocate separate forwarder stubs.
        //
        // The call counting stub should not be the entry point stored directly in vtable slots:
        // - Stubs should be deletable without leaving dangling pointers in vtable slots
        // - On some architectures (e.g. arm64), jitted code may load the entry point into a register at a GC-safe
        //   point, and the stub could be deleted before the register is used for the call
        //
        // Ensure vtable slots point to the temporary entry point (precode) so calls flow through
        // precode → call counting stub → native code. Vtable slots may have been backpatched to native code
        // during the initial publish or tiering delay. BackpatchToResetEntryPointSlots() also sets
        // GetMethodEntryPoint() to the temporary entry point, which we override below.
        //
        // There is a benign race window between resetting vtable slots and setting the precode target: a thread
        // may briefly see vtable slots pointing to the precode while the precode still points to its previous
        // target (prestub or native code). This results in at most one uncounted call, which is acceptable since
        // call counting is a heuristic.
        methodDesc->BackpatchToResetEntryPointSlots();

        // Keep GetMethodEntryPoint() set to the native code entry point rather than the temporary entry point.
        // DoBackpatch() (prestub.cpp) skips slot recording when GetMethodEntryPoint() == GetTemporaryEntryPoint(),
        // interpreting it as "method not yet published". By keeping GetMethodEntryPoint() at native code, we
        // ensure that after the precode reverts to prestub (when call counting stubs are deleted), new vtable
        // slots discovered by DoBackpatch() will be properly recorded for future backpatching.
        methodDesc->SetMethodEntryPoint(codeEntryPoint);
        Precode *precode = Precode::GetPrecodeFromEntryPoint(methodDesc->GetTemporaryEntryPoint());
        precode->SetTargetInterlocked(callCountingCodeEntryPoint, FALSE);
    }
    else
    {
        _ASSERTE(methodDesc->IsVersionableWithPrecode());
        methodDesc->SetCodeEntryPoint(callCountingCodeEntryPoint);
    }

    callCountingInfo->SetStage(CallCountingInfo::Stage::StubMayBeActive);
    return true;
}

extern "C" PCODE STDCALL OnCallCountThresholdReached(TransitionBlock *transitionBlock, TADDR stubIdentifyingToken)
{
    WRAPPER_NO_CONTRACT;
    return CallCountingManager::OnCallCountThresholdReached(transitionBlock, stubIdentifyingToken);
}

PCODE CallCountingManager::OnCallCountThresholdReached(TransitionBlock *transitionBlock, TADDR stubIdentifyingToken)
{
    STATIC_CONTRACT_THROWS;
    STATIC_CONTRACT_GC_TRIGGERS;
    STATIC_CONTRACT_MODE_COOPERATIVE;

    PCODE codeEntryPoint = 0;

    PreserveLastErrorHolder preserveLastError;

    MAKE_CURRENT_THREAD_AVAILABLE();

#ifdef _DEBUG
    Thread::ObjectRefFlush(CURRENT_THREAD);
#endif

    // Get the code version from the call counting stub/info in cooperative GC mode to synchronize with deletion. The stub/info
    // may be deleted only when the runtime is suspended, so when we are in cooperative GC mode it is safe to read from them.
    NativeCodeVersion codeVersion =
        CallCountingInfo::From(CallCountingStub::From(stubIdentifyingToken)->GetRemainingCallCountCell())->GetCodeVersion();

    MethodDesc *methodDesc = codeVersion.GetMethodDesc();
    CallCountingHelperFrame callCountingFrame(transitionBlock, methodDesc);
    CallCountingHelperFrame *frame = &callCountingFrame;
    frame->Push(CURRENT_THREAD);

    INSTALL_MANAGED_EXCEPTION_DISPATCHER;
    INSTALL_UNWIND_AND_CONTINUE_HANDLER;

    // The switch to preemptive GC mode no longer guarantees that the stub/info will be valid. Only the code version will be
    // used going forward under appropriate locking to synchronize further with deletion.
    GCX_PREEMP_THREAD_EXISTS(CURRENT_THREAD);

    _ASSERTE(!codeVersion.IsFinalTier());

    codeEntryPoint = codeVersion.GetNativeCode();
    do
    {
        {
            CallCountingManager *callCountingManager = methodDesc->GetLoaderAllocator()->GetCallCountingManager();

            CodeVersionManager::LockHolder codeVersioningLockHolder;

            CallCountingInfo *callCountingInfo = callCountingManager->m_callCountingInfoByCodeVersionHash.Lookup(codeVersion);
            if (callCountingInfo == nullptr)
            {
                break;
            }

            CallCountingInfo::Stage callCountingStage = callCountingInfo->GetStage();
            if (callCountingStage >= CallCountingInfo::Stage::PendingCompletion)
            {
                break;
            }

            // Fully completing call counting for a method is relative expensive. Call counting with stubs is relatively cheap.
            // Since many methods will typically reach the call count threshold at roughly the same time (a perf spike),
            // delegate as much of the overhead as possible to the background. This significantly decreases the degree of the
            // perf spike.
            callCountingManager->m_callCountingInfosPendingCompletion.Append(callCountingInfo);
            callCountingInfo->SetStage(CallCountingInfo::Stage::PendingCompletion);
        }

        GetAppDomain()->GetTieredCompilationManager()->AsyncCompleteCallCounting();
    } while (false);

    UNINSTALL_UNWIND_AND_CONTINUE_HANDLER;
    UNINSTALL_MANAGED_EXCEPTION_DISPATCHER;

    frame->Pop(CURRENT_THREAD);

    return codeEntryPoint;
}

COUNT_T CallCountingManager::GetCountOfCodeVersionsPendingCompletion()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_PREEMPTIVE;
    }
    CONTRACTL_END;

    COUNT_T count = 0;

    CodeVersionManager::LockHolder codeVersioningLockHolder;

    for (auto itEnd = s_callCountingManagers->End(), it = s_callCountingManagers->Begin(); it != itEnd; ++it)
    {
        CallCountingManager *callCountingManager = *it;
        count += callCountingManager->m_callCountingInfosPendingCompletion.GetCount();
    }

    return count;
}

void CallCountingManager::CompleteCallCounting()
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
    }
    CONTRACTL_END;

    _ASSERTE(GetThreadNULLOk() == TieredCompilationManager::GetBackgroundWorkerThread());

    AppDomain *appDomain = GetAppDomain();
    TieredCompilationManager *tieredCompilationManager = appDomain->GetTieredCompilationManager();
    CodeVersionManager *codeVersionManager = appDomain->GetCodeVersionManager();

    MethodDescBackpatchInfoTracker::ConditionalLockHolder slotBackpatchLockHolder;
    CodeVersionManager::LockHolder codeVersioningLockHolder;

    for (auto itEnd = s_callCountingManagers->End(), it = s_callCountingManagers->Begin(); it != itEnd; ++it)
    {
        CallCountingManager *callCountingManager = *it;
        SArray<CallCountingInfo *> &callCountingInfosPendingCompletion =
            callCountingManager->m_callCountingInfosPendingCompletion;
        COUNT_T callCountingInfoCount = callCountingInfosPendingCompletion.GetCount();
        if (callCountingInfoCount == 0)
        {
            continue;
        }

        CallCountingInfo **callCountingInfos = callCountingInfosPendingCompletion.GetElements();
        for (COUNT_T i = 0; i < callCountingInfoCount; ++i)
        {
            CallCountingInfo *callCountingInfo = callCountingInfos[i];
            CallCountingInfo::Stage callCountingStage = callCountingInfo->GetStage();
            if (callCountingStage != CallCountingInfo::Stage::PendingCompletion)
            {
                continue;
            }

            NativeCodeVersion codeVersion = callCountingInfo->GetCodeVersion();
            MethodDesc *methodDesc = codeVersion.GetMethodDesc();
            _ASSERTE(codeVersionManager == methodDesc->GetCodeVersionManager());
            EX_TRY
            {
                if (!codeVersion.GetILCodeVersion().HasAnyOptimizedNativeCodeVersion(codeVersion))
                {
                    bool createTieringBackgroundWorker = false;
                    tieredCompilationManager->AsyncPromoteToTier1(codeVersion, &createTieringBackgroundWorker);
                    _ASSERTE(!createTieringBackgroundWorker); // the current thread is the background worker thread
                }

                // The active code version may have changed externally after the call counting stub was activated,
                // deactivating the call counting stub without our knowledge. Check the active code version and determine
                // what needs to be done.
                NativeCodeVersion activeCodeVersion =
                    codeVersionManager->GetActiveILCodeVersion(methodDesc).GetActiveNativeCodeVersion(methodDesc);
                do
                {
                    if (activeCodeVersion == codeVersion)
                    {
                        if (methodDesc->MayHaveEntryPointSlotsToBackpatch())
                        {
                            Precode *precode = Precode::GetPrecodeFromEntryPoint(methodDesc->GetTemporaryEntryPoint());
                            precode->SetTargetInterlocked(activeCodeVersion.GetNativeCode(), FALSE);
                        }
                        else
                        {
                            methodDesc->SetCodeEntryPoint(activeCodeVersion.GetNativeCode());
                        }
                        break;
                    }

                    // There is at least one case where the IL code version is changed inside the code versioning lock, the
                    // lock is released and reacquired, then the method's code entry point is reset. So if this path is
                    // reached between those locks, the method would still be pointing to the call counting stub. Once the
                    // stub is marked as complete, it may be deleted, so in all cases update the method's code entry point
                    // to ensure that the method is no longer pointing to the call counting stub.

                    if (!activeCodeVersion.IsNull())
                    {
                        PCODE activeNativeCode = activeCodeVersion.GetNativeCode();
                        if (activeNativeCode != 0)
                        {
                            if (methodDesc->MayHaveEntryPointSlotsToBackpatch())
                            {
                                Precode *precode = Precode::GetPrecodeFromEntryPoint(methodDesc->GetTemporaryEntryPoint());
                                precode->SetTargetInterlocked(activeNativeCode, FALSE);
                            }
                            else
                            {
                                methodDesc->SetCodeEntryPoint(activeNativeCode);
                            }
                            break;
                        }
                    }

                    if (methodDesc->MayHaveEntryPointSlotsToBackpatch())
                    {
                        Precode::GetPrecodeFromEntryPoint(methodDesc->GetTemporaryEntryPoint())->ResetTargetInterlocked();
                    }
                    else
                    {
                        methodDesc->ResetCodeEntryPoint();
                    }
                } while (false);

                callCountingInfo->SetStage(CallCountingInfo::Stage::Complete);
            }
            EX_CATCH
            {
                // Avoid abandoning call counting completion for all recorded call counting infos on exception. Since this
                // is happening on a background thread, following the general policy so far, the exception will be caught,
                // logged, and ignored anyway, so make an attempt to complete call counting for each item. Individual items
                // that fail will result in those code versions not getting promoted (similar to elsewhere).
                STRESS_LOG1(LF_TIEREDCOMPILATION, LL_WARNING, "CallCountingManager::CompleteCallCounting: "
                    "Exception, hr=0x%x\n",
                    GET_EXCEPTION()->GetHR());
                RethrowTerminalExceptions();
            }
            EX_END_CATCH
        }

        callCountingInfosPendingCompletion.Clear();
        if (callCountingInfosPendingCompletion.GetAllocation() > 64)
        {
            callCountingInfosPendingCompletion.Trim();
            EX_TRY
            {
                callCountingInfosPendingCompletion.Preallocate(64);
            }
            EX_SWALLOW_NONTERMINAL;
        }
    }
}

void CallCountingManager::StopAndDeleteAllCallCountingStubs()
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
    }
    CONTRACTL_END;

    _ASSERTE(GetThreadNULLOk() == TieredCompilationManager::GetBackgroundWorkerThread());

    // If a number of call counting stubs have completed, we can try to delete them to reclaim some memory. Deleting
    // involves suspending the runtime and will delete all call counting stubs, and after that some call counting stubs may
    // be recreated in the foreground. The threshold is to decrease the impact of both of those overheads.
    COUNT_T deleteCallCountingStubsAfter = g_pConfig->TieredCompilation_DeleteCallCountingStubsAfter();
    if (deleteCallCountingStubsAfter == 0 || s_completedCallCountingStubCount < deleteCallCountingStubsAfter)
    {
        return;
    }

    TieredCompilationManager *tieredCompilationManager = GetAppDomain()->GetTieredCompilationManager();

    ThreadSuspend::SuspendEE(ThreadSuspend::SUSPEND_OTHER);
    struct AutoRestartEE
    {
        ~AutoRestartEE()
        {
            WRAPPER_NO_CONTRACT;
            ThreadSuspend::RestartEE(false, true);
        }
    } autoRestartEE;

    MethodDescBackpatchInfoTracker::ConditionalLockHolder slotBackpatchLockHolder;
    CodeVersionManager::LockHolder codeVersioningLockHolder;

    // After the following, no method's entry point would be pointing to a call counting stub
    StopAllCallCounting(tieredCompilationManager);

    // Call counting has been stopped above and call counting stubs will soon be deleted. Ensure that call counting stubs
    // will not be used after resuming the runtime. The following ensures that other threads will not use an old cached
    // entry point value that will not be valid. Do this here in case of exception later.
    MemoryBarrier(); // flush writes from this thread first to guarantee ordering
    minipal_memory_barrier_process_wide();

    // At this point, allocated call counting stubs won't be used anymore. Call counting stubs and corresponding infos may
    // now be safely deleted. Note that call counting infos may not be deleted prior to this point because call counting
    // stubs refer to the remaining call count in the info, and the call counting info is necessary to get a code version
    // from a call counting stub address.
    DeleteAllCallCountingStubs();
}

void CallCountingManager::StopAllCallCounting(TieredCompilationManager *tieredCompilationManager)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_COOPERATIVE; // for slot backpatching
    }
    CONTRACTL_END;

    _ASSERTE(GetThreadNULLOk() == TieredCompilationManager::GetBackgroundWorkerThread());
    _ASSERTE(MethodDescBackpatchInfoTracker::IsLockOwnedByCurrentThread());
    _ASSERTE(CodeVersionManager::IsLockOwnedByCurrentThread());
    _ASSERTE(tieredCompilationManager != nullptr);

    for (auto itEnd = s_callCountingManagers->End(), it = s_callCountingManagers->Begin(); it != itEnd; ++it)
    {
        CallCountingManager *callCountingManager = *it;

        CallCountingInfoByCodeVersionHash &callCountingInfoByCodeVersionHash =
            callCountingManager->m_callCountingInfoByCodeVersionHash;
        for (auto itEnd = callCountingInfoByCodeVersionHash.End(), it = callCountingInfoByCodeVersionHash.Begin();
            it != itEnd;
            ++it)
        {
            CallCountingInfo *callCountingInfo = *it;
            CallCountingInfo::Stage callCountingStage = callCountingInfo->GetStage();
            if (callCountingStage != CallCountingInfo::Stage::StubMayBeActive &&
                callCountingStage != CallCountingInfo::Stage::PendingCompletion)
            {
                continue;
            }

            NativeCodeVersion codeVersion = callCountingInfo->GetCodeVersion();
            CallCountingInfo::Stage newCallCountingStage;
            if (callCountingStage == CallCountingInfo::Stage::StubMayBeActive)
            {
                newCallCountingStage = CallCountingInfo::Stage::StubIsNotActive;
            }
            else
            {
                _ASSERTE(callCountingStage == CallCountingInfo::Stage::PendingCompletion);
                if (!codeVersion.GetILCodeVersion().HasAnyOptimizedNativeCodeVersion(codeVersion))
                {
                    bool createTieringBackgroundWorker = false;
                    tieredCompilationManager->AsyncPromoteToTier1(codeVersion, &createTieringBackgroundWorker);
                    _ASSERTE(!createTieringBackgroundWorker); // the current thread is the background worker thread
                }

                newCallCountingStage = CallCountingInfo::Stage::Complete;
            }

            // The intention is that all call counting stubs will be deleted shortly, and only methods that are called again
            // will cause stubs to be recreated, so reset the code entry point
            MethodDesc *methodDesc = codeVersion.GetMethodDesc();
            if (methodDesc->MayHaveEntryPointSlotsToBackpatch())
            {
                Precode::GetPrecodeFromEntryPoint(methodDesc->GetTemporaryEntryPoint())->ResetTargetInterlocked();
            }
            else
            {
                methodDesc->ResetCodeEntryPoint();
            }
            callCountingInfo->SetStage(newCallCountingStage);
        }

        // Clear recorded call counting infos pending completion, they have been completed above
        SArray<CallCountingInfo *> &callCountingInfosPendingCompletion =
            callCountingManager->m_callCountingInfosPendingCompletion;
        if (!callCountingInfosPendingCompletion.IsEmpty())
        {
            callCountingInfosPendingCompletion.Clear();
            if (callCountingInfosPendingCompletion.GetAllocation() > 64)
            {
                callCountingInfosPendingCompletion.Trim();
                EX_TRY
                {
                    callCountingInfosPendingCompletion.Preallocate(64);
                }
                EX_SWALLOW_NONTERMINAL;
            }
        }
    }
}

void CallCountingManager::DeleteAllCallCountingStubs()
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    _ASSERTE(CodeVersionManager::IsLockOwnedByCurrentThread());
    _ASSERTE(IsSuspendEEThread());
    _ASSERTE(s_activeCallCountingStubCount == 0);

    // An attempt will be made to delete stubs below, clear the counts first in case of exception later
    s_callCountingStubCount = 0;
    s_completedCallCountingStubCount = 0;

    for (auto itEnd = s_callCountingManagers->End(), it = s_callCountingManagers->Begin(); it != itEnd; ++it)
    {
        CallCountingManager *callCountingManager = *it;
        _ASSERTE(callCountingManager->m_callCountingInfosPendingCompletion.IsEmpty());

        // Clear the call counting stub from call counting infos and delete completed infos
        CallCountingInfoByCodeVersionHash &callCountingInfoByCodeVersionHash =
            callCountingManager->m_callCountingInfoByCodeVersionHash;
        for (auto itEnd = callCountingInfoByCodeVersionHash.End(), it = callCountingInfoByCodeVersionHash.Begin();
            it != itEnd;
            ++it)
        {
            CallCountingInfo *callCountingInfo = *it;
            CallCountingInfo::Stage callCountingStage = callCountingInfo->GetStage();
            if (callCountingStage == CallCountingInfo::Stage::Disabled)
            {
                continue;
            }

            if (callCountingInfo->GetCallCountingStub() != nullptr)
            {
                callCountingInfo->ClearCallCountingStub();
            }

            if (callCountingStage != CallCountingInfo::Stage::Complete)
            {
                _ASSERTE(callCountingStage == CallCountingInfo::Stage::StubIsNotActive);
                continue;
            }

            callCountingInfoByCodeVersionHash.Remove(it);
            delete callCountingInfo;
        }

        callCountingManager->TrimCollections();

        // All call counting stubs are deleted, not just the completed stubs. Typically, there are many methods that are called
        // only a few times and don't reach the call count threshold, so many stubs may not be recreated. On the other hand,
        // some methods may still be getting called, just less frequently, then call counting stubs would be recreated in the
        // foreground, which has some overhead that is currently managed in the conditions for deleting call counting stubs.
        // There are potential solutions to reclaim as much memory as possible and to minimize the foreground overhead, but they
        // seem to involve significantly higher complexity that doesn't seem worthwhile.
        callCountingManager->m_callCountingStubAllocator.Reset();
    }
}

void CallCountingManager::TrimCollections()
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    _ASSERTE(CodeVersionManager::IsLockOwnedByCurrentThread());

    // Resize the hash tables if it would save some space. The hash tables' item counts typically spikes and then stabilizes at
    // a lower value after most of the repeatedly called methods are promoted and the call counting infos deleted above.

    COUNT_T count = m_callCountingInfoByCodeVersionHash.GetCount();
    COUNT_T capacity = m_callCountingInfoByCodeVersionHash.GetCapacity();
    if (count == 0)
    {
        if (capacity != 0)
        {
            m_callCountingInfoByCodeVersionHash.RemoveAll();
        }
    }
    else if (count <= capacity / 4)
    {
        EX_TRY
        {
            m_callCountingInfoByCodeVersionHash.Reallocate(count * 2);
        }
        EX_SWALLOW_NONTERMINAL
    }
}

#endif // !DACCESS_COMPILE

bool CallCountingManager::IsCallCountingStub(PCODE entryPoint)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        SUPPORTS_DAC;
    }
    CONTRACTL_END;

    TADDR entryAddress = PCODEToPINSTR(entryPoint);
    _ASSERTE(entryAddress != (PCODE)NULL);

    CodeVersionManager::LockHolder codeVersioningLockHolder;

    PTR_CallCountingManagerHash callCountingManagers = s_callCountingManagers;
    if (callCountingManagers == NULL)
    {
        return false;
    }

    for (auto itEnd = callCountingManagers->End(), it = callCountingManagers->Begin(); it != itEnd; ++it)
    {
        PTR_CallCountingManager callCountingManager = *it;
        if (callCountingManager->m_callCountingStubAllocator.IsStub(entryAddress))
        {
            return true;
        }
    }
    return false;
}

PCODE CallCountingManager::GetTargetForMethod(PCODE callCountingStubEntryPoint)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_COOPERATIVE; // the call counting stub cannot be deleted while inspecting it
        SUPPORTS_DAC;
    }
    CONTRACTL_END;

    _ASSERTE(IsCallCountingStub(callCountingStubEntryPoint));

    return PTR_CallCountingStub(PCODEToPINSTR(callCountingStubEntryPoint))->GetTargetForMethod();
}

#ifdef DACCESS_COMPILE

void CallCountingManager::DacEnumerateCallCountingStubHeapRanges(CLRDataEnumMemoryFlags flags)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        SUPPORTS_DAC;
    }
    CONTRACTL_END;

    CodeVersionManager::LockHolder codeVersioningLockHolder;

    PTR_CallCountingManagerHash callCountingManagers = s_callCountingManagers;
    if (callCountingManagers == NULL)
    {
        return;
    }

    for (auto itEnd = callCountingManagers->End(), it = callCountingManagers->Begin(); it != itEnd; ++it)
    {
        PTR_CallCountingManager callCountingManager = *it;
        callCountingManager->m_callCountingStubAllocator.EnumerateHeapRanges(flags);
    }
}

#endif // DACCESS_COMPILE

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// CallCountingManager::CallCountingStubManager

SPTR_IMPL(CallCountingStubManager, CallCountingStubManager, g_pManager);

#ifndef DACCESS_COMPILE

CallCountingStubManager::CallCountingStubManager()
{
    WRAPPER_NO_CONTRACT;
}

void CallCountingStubManager::Init()
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    g_pManager = new CallCountingStubManager();
    StubManager::AddStubManager(g_pManager);
}

#endif // !DACCESS_COMPILE

#ifdef _DEBUG
const char *CallCountingStubManager::DbgGetName()
{
    WRAPPER_NO_CONTRACT;
    return "CallCountingStubManager";
}
#endif

#ifdef DACCESS_COMPILE
LPCWSTR CallCountingStubManager::GetStubManagerName(PCODE addr)
{
    WRAPPER_NO_CONTRACT;
    return W("CallCountingStub");
}
#endif

BOOL CallCountingStubManager::CheckIsStub_Internal(PCODE entryPoint)
{
    WRAPPER_NO_CONTRACT;
    SUPPORTS_DAC;

    return CallCountingManager::IsCallCountingStub(entryPoint);
}

BOOL CallCountingStubManager::DoTraceStub(PCODE callCountingStubEntryPoint, TraceDestination *trace)
{
    WRAPPER_NO_CONTRACT;
    SUPPORTS_DAC;
    _ASSERTE(trace != nullptr);

    trace->InitForStub(CallCountingManager::GetTargetForMethod(callCountingStubEntryPoint));
    return true;
}

#ifdef DACCESS_COMPILE
void CallCountingStubManager::DoEnumMemoryRegions(CLRDataEnumMemoryFlags flags)
{
    WRAPPER_NO_CONTRACT;
    SUPPORTS_DAC;

    DAC_ENUM_VTHIS();
    EMEM_OUT(("MEM: %p CallCountingStubManager\n", dac_cast<TADDR>(this)));
    CallCountingManager::DacEnumerateCallCountingStubHeapRanges(flags);
}
#endif

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

#endif // FEATURE_TIERED_COMPILATION
