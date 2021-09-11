// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "common.h"

#ifdef FEATURE_TIERED_COMPILATION

#include "callcounting.h"
#include "threadsuspend.h"

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
    LoaderHeap *heap = m_heap;
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

    LoaderHeap *heap = m_heap;
    if (heap == nullptr)
    {
        heap = AllocateHeap();
    }

    SIZE_T sizeInBytes;
    const CallCountingStub *stub;
    do
    {
        bool forceLongStub = false;
    #if defined(_DEBUG) && defined(TARGET_AMD64)
        if (s_callCountingStubCount % 2 == 0)
        {
            forceLongStub = true;
        }
    #endif

        if (!forceLongStub)
        {
            sizeInBytes = sizeof(CallCountingStubShort);
            AllocMemHolder<void> allocationAddressHolder(heap->AllocAlignedMem(sizeInBytes, CallCountingStub::Alignment));
        #ifdef TARGET_AMD64
            if (CallCountingStubShort::CanUseFor(allocationAddressHolder, targetForMethod))
        #endif
            {
                ExecutableWriterHolder<void> writerHolder(allocationAddressHolder, sizeInBytes);
                new(writerHolder.GetRW()) CallCountingStubShort((CallCountingStubShort*)(void*)allocationAddressHolder, remainingCallCountCell, targetForMethod);
                stub = (CallCountingStub*)(void*)allocationAddressHolder;
                allocationAddressHolder.SuppressRelease();
                break;
            }
        }

    #ifdef TARGET_AMD64
        sizeInBytes = sizeof(CallCountingStubLong);
        void *allocationAddress = (void *)heap->AllocAlignedMem(sizeInBytes, CallCountingStub::Alignment);
        ExecutableWriterHolder<void> writerHolder(allocationAddress, sizeInBytes);
        new(writerHolder.GetRW()) CallCountingStubLong(remainingCallCountCell, targetForMethod);
        stub = (CallCountingStub*)allocationAddress;
    #else
        UNREACHABLE();
    #endif
    } while (false);

    ClrFlushInstructionCache(stub, sizeInBytes);
    return stub;
}

NOINLINE LoaderHeap *CallCountingManager::CallCountingStubAllocator::AllocateHeap()
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    _ASSERTE(m_heap == nullptr);

    LoaderHeap *heap = new LoaderHeap(0, 0, &m_heapRangeList, true /* fMakeExecutable */, true /* fUnlocked */);
    m_heap = heap;
    return heap;
}

#endif // !DACCESS_COMPILE

bool CallCountingManager::CallCountingStubAllocator::IsStub(TADDR entryPoint)
{
    WRAPPER_NO_CONTRACT;
    _ASSERTE(entryPoint != NULL);

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
// CallCountingManager::MethodDescForwarderStubHashTraits

CallCountingManager::MethodDescForwarderStubHashTraits::key_t
CallCountingManager::MethodDescForwarderStubHashTraits::GetKey(const element_t &e)
{
    WRAPPER_NO_CONTRACT;
    return e->GetMethodDesc();
}

BOOL CallCountingManager::MethodDescForwarderStubHashTraits::Equals(const key_t &k1, const key_t &k2)
{
    WRAPPER_NO_CONTRACT;
    return k1 == k2;
}

CallCountingManager::MethodDescForwarderStubHashTraits::count_t
CallCountingManager::MethodDescForwarderStubHashTraits::Hash(const key_t &k)
{
    WRAPPER_NO_CONTRACT;
    return (count_t)(size_t)k;
}

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

        // Backpatching entry point slots requires cooperative GC mode, see MethodDescBackpatchInfoTracker::Backpatch_Locked().
        // The code version manager's table lock is an unsafe lock that may be taken in any GC mode. The lock is taken in
        // cooperative GC mode on other paths, so the caller must use the same ordering to prevent deadlock (switch to
        // cooperative GC mode before taking the lock).
        PRECONDITION(!activeCodeVersion.IsNull());
        if (activeCodeVersion.GetMethodDesc()->MayHaveEntryPointSlotsToBackpatch())
        {
            MODE_COOPERATIVE;
        }
        else
        {
            MODE_ANY;
        }
    }
    CONTRACTL_END;

    MethodDesc *methodDesc = activeCodeVersion.GetMethodDesc();
    _ASSERTE(!methodDesc->MayHaveEntryPointSlotsToBackpatch() || MethodDescBackpatchInfoTracker::IsLockOwnedByCurrentThread());
    _ASSERTE(CodeVersionManager::IsLockOwnedByCurrentThread());
    _ASSERTE(
        activeCodeVersion ==
        methodDesc->GetCodeVersionManager()->GetActiveILCodeVersion(methodDesc).GetActiveNativeCodeVersion(methodDesc));
    _ASSERTE(codeEntryPoint != NULL);
    _ASSERTE(codeEntryPoint == activeCodeVersion.GetNativeCode());
    _ASSERTE(!wasMethodCalled || createTieringBackgroundWorkerRef != nullptr);
    _ASSERTE(createTieringBackgroundWorkerRef == nullptr || !*createTieringBackgroundWorkerRef);

    if (!methodDesc->IsEligibleForTieredCompilation() ||
        (
            // For a default code version that is not tier 0, call counting will have been disabled by this time (checked
            // below). Avoid the redundant and not-insignificant expense of GetOptimizationTier() on a default code version.
            !activeCodeVersion.IsDefaultVersion() &&
            activeCodeVersion.GetOptimizationTier() != NativeCodeVersion::OptimizationTier0
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
                methodDesc->SetCodeEntryPoint(codeEntryPoint);
                return true;
            }

            _ASSERTE(activeCodeVersion.GetOptimizationTier() == NativeCodeVersion::OptimizationTier0);

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
                methodDesc->SetCodeEntryPoint(codeEntryPoint);
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
            _ASSERTE(activeCodeVersion.GetOptimizationTier() == NativeCodeVersion::OptimizationTier0);

            // If the tiering delay is active, postpone further work
            if (GetAppDomain()
                    ->GetTieredCompilationManager()
                    ->TrySetCodeEntryPointAndRecordMethodForCallCounting(methodDesc, codeEntryPoint))
            {
                return true;
            }

            CallCount callCountThreshold = (CallCount)g_pConfig->TieredCompilation_CallCountThreshold();
            _ASSERTE(callCountThreshold != 0);

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
        // The call counting stub should not be the entry point that is called first in the process of a call
        // - Stubs should be deletable. Many methods will have call counting stubs associated with them, and although the memory
        //   involved is typically insignificant compared to the average memory overhead per method, by steady-state it would
        //   otherwise be unnecessary memory overhead serving no purpose.
        // - In order to be able to delete a stub, the jitted code of a method cannot be allowed to load the stub as the entry
        //   point of a callee into a register in a GC-safe point that allows for the stub to be deleted before the register is
        //   reused to call the stub. On some processor architectures, perhaps the JIT can guarantee that it would not load the
        //   entry point into a register before the call, but this is not possible on arm32 or arm64. Rather, perhaps the
        //   region containing the load and call would not be considered GC-safe. Calls are considered GC-safe points, and this
        //   may cause many methods that are currently fully interruptible to have to be partially interruptible and record
        //   extra GC info instead. This would be nontrivial and there would be tradeoffs.
        // - For any method that may have an entry point slot that would be backpatched with the call counting stub's entry
        //   point, a small forwarder stub (precode) is created. The forwarder stub has loader allocator lifetime and fowards to
        //   the larger call counting stub. This is a simple solution for now and seems to have negligible impact.
        // - Reusing FuncPtrStubs was considered. FuncPtrStubs are currently not used as a code entry point for a virtual or
        //   interface method and may be bypassed. For example, a call may call through the vtable slot, or a devirtualized call
        //   may call through a FuncPtrStub. The target of a FuncPtrStub is a code entry point and is backpatched when a
        //   method's active code entry point changes. Mixing the current use of FuncPtrStubs with the use as a forwarder for
        //   call counting does not seem trivial and would likely complicate its use. There may not be much gain in reusing
        //   FuncPtrStubs, as typically, they are created for only a small percentage of virtual/interface methods.

        MethodDescForwarderStubHash &methodDescForwarderStubHash = callCountingManager->m_methodDescForwarderStubHash;
        Precode *forwarderStub = methodDescForwarderStubHash.Lookup(methodDesc);
        if (forwarderStub == nullptr)
        {
            AllocMemTracker forwarderStubAllocationTracker;
            forwarderStub =
                Precode::Allocate(
                    methodDesc->GetPrecodeType(),
                    methodDesc,
                    methodDesc->GetLoaderAllocator(),
                    &forwarderStubAllocationTracker);
            methodDescForwarderStubHash.Add(forwarderStub);
            forwarderStubAllocationTracker.SuppressRelease();
        }

        forwarderStub->SetTargetInterlocked(callCountingCodeEntryPoint, false);
        callCountingCodeEntryPoint = forwarderStub->GetEntryPoint();
    }
    else
    {
        _ASSERTE(methodDesc->IsVersionableWithPrecode());
    }

    methodDesc->SetCodeEntryPoint(callCountingCodeEntryPoint);
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
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        PRECONDITION(CheckPointer(transitionBlock));
    }
    CONTRACTL_END;

    MAKE_CURRENT_THREAD_AVAILABLE();

#ifdef _DEBUG
    Thread::ObjectRefFlush(CURRENT_THREAD);
#endif

    // Get the code version from the call counting stub/info in cooperative GC mode to synchronize with deletion. The stub/info
    // may be deleted only when the runtime is suspended, so when we are in cooperative GC mode it is safe to read from them.
    NativeCodeVersion codeVersion =
        CallCountingInfo::From(CallCountingStub::From(stubIdentifyingToken)->GetRemainingCallCountCell())->GetCodeVersion();

    MethodDesc *methodDesc = codeVersion.GetMethodDesc();
    FrameWithCookie<CallCountingHelperFrame> frameWithCookie(transitionBlock, methodDesc);
    CallCountingHelperFrame *frame = &frameWithCookie;
    frame->Push(CURRENT_THREAD);

    PCODE codeEntryPoint;

    INSTALL_MANAGED_EXCEPTION_DISPATCHER;
    INSTALL_UNWIND_AND_CONTINUE_HANDLER;

    // The switch to preemptive GC mode no longer guarantees that the stub/info will be valid. Only the code version will be
    // used going forward under appropriate locking to synchronize further with deletion.
    GCX_PREEMP_THREAD_EXISTS(CURRENT_THREAD);

    _ASSERTE(codeVersion.GetOptimizationTier() == NativeCodeVersion::OptimizationTier0);

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

    MethodDescBackpatchInfoTracker::ConditionalLockHolderForGCCoop slotBackpatchLockHolder;

    // Backpatching entry point slots requires cooperative GC mode, see
    // MethodDescBackpatchInfoTracker::Backpatch_Locked(). The code version manager's table lock is an unsafe lock that
    // may be taken in any GC mode. The lock is taken in cooperative GC mode on some other paths, so the same ordering
    // must be used here to prevent deadlock.
    GCX_COOP();
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
                        methodDesc->SetCodeEntryPoint(activeCodeVersion.GetNativeCode());
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
                        if (activeNativeCode != NULL)
                        {
                            methodDesc->SetCodeEntryPoint(activeNativeCode);
                            break;
                        }
                    }

                    methodDesc->ResetCodeEntryPoint();
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
            }
            EX_END_CATCH(RethrowTerminalExceptions);
        }

        callCountingInfosPendingCompletion.Clear();
        if (callCountingInfosPendingCompletion.GetAllocation() > 64)
        {
            callCountingInfosPendingCompletion.Trim();
            EX_TRY
            {
                callCountingInfosPendingCompletion.Preallocate(64);
            }
            EX_CATCH
            {
            }
            EX_END_CATCH(RethrowTerminalExceptions);
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

    MethodDescBackpatchInfoTracker::ConditionalLockHolderForGCCoop slotBackpatchLockHolder;

    ThreadSuspend::SuspendEE(ThreadSuspend::SUSPEND_OTHER);
    struct AutoRestartEE
    {
        ~AutoRestartEE()
        {
            WRAPPER_NO_CONTRACT;
            ThreadSuspend::RestartEE(false, true);
        }
    } autoRestartEE;

    // Backpatching entry point slots requires cooperative GC mode, see
    // MethodDescBackpatchInfoTracker::Backpatch_Locked(). The code version manager's table lock is an unsafe lock that
    // may be taken in any GC mode. The lock is taken in cooperative GC mode on some other paths, so the same ordering
    // must be used here to prevent deadlock.
    GCX_COOP();
    CodeVersionManager::LockHolder codeVersioningLockHolder;

    // After the following, no method's entry point would be pointing to a call counting stub
    StopAllCallCounting(tieredCompilationManager);

    // Call counting has been stopped above and call counting stubs will soon be deleted. Ensure that call counting stubs
    // will not be used after resuming the runtime. The following ensures that other threads will not use an old cached
    // entry point value that will not be valid. Do this here in case of exception later.
    MemoryBarrier(); // flush writes from this thread first to guarantee ordering
    FlushProcessWriteBuffers();

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
            codeVersion.GetMethodDesc()->ResetCodeEntryPoint();
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
                EX_CATCH
                {
                }
                EX_END_CATCH(RethrowTerminalExceptions);
            }
        }

        // Reset forwarder stubs, they are not in use anymore
        MethodDescForwarderStubHash &methodDescForwarderStubHash = callCountingManager->m_methodDescForwarderStubHash;
        for (auto itEnd = methodDescForwarderStubHash.End(), it = methodDescForwarderStubHash.Begin(); it != itEnd; ++it)
        {
            Precode *forwarderStub = *it;
            forwarderStub->ResetTargetInterlocked();
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
        MethodDescForwarderStubHash &methodDescForwarderStubHash = callCountingManager->m_methodDescForwarderStubHash;
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

            // Currently, tier 0 is the last code version that is counted, and the method is typically not counted anymore.
            // Remove the forwarder stub if one exists, a new one will be created if necessary, for example, if a profiler adds
            // an IL code version for the method.
            Precode *const *forwarderStubPtr =
                methodDescForwarderStubHash.LookupPtr(callCountingInfo->GetCodeVersion().GetMethodDesc());
            if (forwarderStubPtr != nullptr)
            {
                methodDescForwarderStubHash.RemovePtr(const_cast<Precode **>(forwarderStubPtr));
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
        EX_CATCH
        {
        }
        EX_END_CATCH(RethrowTerminalExceptions);
    }

    count = m_methodDescForwarderStubHash.GetCount();
    capacity = m_methodDescForwarderStubHash.GetCapacity();
    if (count == 0)
    {
        if (capacity != 0)
        {
            m_methodDescForwarderStubHash.RemoveAll();
        }
    }
    else if (count <= capacity / 4)
    {
        EX_TRY
        {
            m_methodDescForwarderStubHash.Reallocate(count * 2);
        }
        EX_CATCH
        {
        }
        EX_END_CATCH(RethrowTerminalExceptions);
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
    _ASSERTE(entryAddress != NULL);

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
