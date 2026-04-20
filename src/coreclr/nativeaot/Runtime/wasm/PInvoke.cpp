// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <stdlib.h>

#include "common.h"
#include "daccess.h"
#include "CommonTypes.h"
#include "CommonMacros.h"
#include "CommonMacros.inl"
#include "Pal.h"
#include "thread.h"
#include "threadstore.h"
#include "thread.inl"
#include "threadstore.inl"

#include "wasm.h"

void* Thread::GetShadowStackBottom()
{
    ASSERT(m_pShadowStackBottom != nullptr);
    return m_pShadowStackBottom;
}

void* Thread::GetShadowStackTop(PInvokeTransitionFrame* pTransitionFrame)
{
    void* pShadowStack;
    if (pTransitionFrame == TOP_OF_STACK_MARKER) // TODO-LLVM: remove this check by replacing TOP_OF_STACK_MARKER with m_pShadowStackBottom.
    {
        pShadowStack = GetShadowStackBottom();
    }
    else
    {
        pShadowStack = pTransitionFrame;
    }
    ASSERT(pShadowStack != nullptr);
    return pShadowStack;
}

FORCEINLINE static void* GetAlignedShadowStackTop(Thread* pThread, PInvokeTransitionFrame* pTransitionFrame, size_t alignment)
{
    void* pShadowStack = pThread->GetShadowStackTop(pTransitionFrame);

    // Note how this aligning means that the transition frame on exit (saved back into current Thread) may differ
    // from its value on entry. This is ok since that value will only grow in a bounded manner, such that calling
    // an RPI method in a loop will never lead to runaway shadow stack usage.
    if (alignment != 0)
    {
        ASSERT(alignment == 8);
        ZeroMemory(pShadowStack, 4);
        pShadowStack = ALIGN_UP(pShadowStack, alignment);
    }
    return pShadowStack;
}

FORCEINLINE static ReversePInvokeFrame* GetReversePInvokeFrame(void* pShadowStack)
{
    return (ReversePInvokeFrame*)pShadowStack;
}

FORCEINLINE static SparseVirtualUnwindFrame* GetSparseVirtualUnwindFrame(ReversePInvokeFrame* pFrame)
{
    return (SparseVirtualUnwindFrame*)(pFrame + 1);
}

FORCEINLINE void* Thread::InlineTryFastReversePInvoke_Wasm(size_t alignment)
{
    PInvokeTransitionFrame* pTransitionFrame = m_pTransitionFrame;
    if (pTransitionFrame == nullptr)
        return nullptr; // Uninitialized thread or illegal transition. Use the slow path.

    ASSERT(!IsCurrentThreadInCooperativeMode());
    void* pShadowStack = GetAlignedShadowStackTop(this, pTransitionFrame, alignment);
    ReversePInvokeFrame* pFrame = GetReversePInvokeFrame(pShadowStack);
    if (!InlineTryFastReversePInvoke(pFrame))
        return nullptr; // Need to suspend the thread.

    return pShadowStack;
}

NOINLINE void* Thread::ReversePInvokeAttachOrTrapThread_Wasm(size_t alignment)
{
    // This check is necessary to support GC callouts, see "InlineTryFastReversePInvoke".
    // We move it to the slow path since GC callouts should be very rare on WASM.
    if (IsDoNotTriggerGcSet())
    {
        // We expect this scenario only when EE is stopped.
        ASSERT(ThreadStore::IsTrapThreadsRequested());
        return GetAlignedShadowStackTop(this, GetTransitionFrame(), alignment); // The suspender transition frame.
    }

    // The shadow stack at this point may not have been allocated yet, so we need to use a local RPI frame.
    ReversePInvokeFrame localFrame;
    ReversePInvokeAttachOrTrapThread(&localFrame);

    void* pShadowStack = GetAlignedShadowStackTop(this, localFrame.m_savedPInvokeTransitionFrame, alignment);
    *GetReversePInvokeFrame(pShadowStack) = localFrame;
    return pShadowStack;
}

FCIMPL_NO_SS(void*, RhpReversePInvoke, size_t alignment)
{
    Thread* pThread = ThreadStore::RawGetCurrentThread();
    void* pShadowStack = pThread->InlineTryFastReversePInvoke_Wasm(alignment);
    if (pShadowStack == nullptr)
        pShadowStack = pThread->ReversePInvokeAttachOrTrapThread_Wasm(alignment);

    return pShadowStack;
}
FCIMPLEND

FCIMPL_NO_SS(void*, RhpReversePInvokeAndPushSparseVirtualUnwindFrame, size_t alignment, void* pUnwindTable, size_t unwindIndex)
{
    Thread* pThread = ThreadStore::RawGetCurrentThread();
    void* pShadowStack = pThread->InlineTryFastReversePInvoke_Wasm(alignment);
    if (pShadowStack == nullptr)
        pShadowStack = pThread->ReversePInvokeAttachOrTrapThread_Wasm(alignment);

    SparseVirtualUnwindFrame* pSparseVirtualUnwindFrame = GetSparseVirtualUnwindFrame(GetReversePInvokeFrame(pShadowStack));
    InlinePushSparseVirtualUnwindFrame(pSparseVirtualUnwindFrame, pUnwindTable, unwindIndex);
    return pShadowStack;
}
FCIMPLEND

FCIMPL_NO_SS(void, RhpReversePInvokeReturn, ReversePInvokeFrame* pFrame)
{
    ThreadStore::RawGetCurrentThread()->InlineReversePInvokeReturn(pFrame);
}
FCIMPLEND

FCIMPL_NO_SS(void, RhpReversePInvokeReturnAndPopSparseVirtualUnwindFrame, ReversePInvokeFrame* pFrame)
{
    InlinePopSparseVirtualUnwindFrame(GetSparseVirtualUnwindFrame(pFrame));
    ThreadStore::RawGetCurrentThread()->InlineReversePInvokeReturn(pFrame);
}
FCIMPLEND

FCIMPL0(void, RhpPInvoke)
{
    PInvokeTransitionFrame* pFrame = (PInvokeTransitionFrame*)pShadowStack;
    ThreadStore::RawGetCurrentThread()->InlinePInvoke(pFrame);
}
FCIMPLEND

FCIMPL0(void, RhpPInvokeReturn)
{
    // WASM TLS is cheap:
    // 1. Without threading, it is free.
    // 2. With threading, it costs a single additional load (of the TLS base global).
    // So not caching the current thread in the PI frame doesn't cost us anything.
    PInvokeTransitionFrame* pFrame = (PInvokeTransitionFrame*)pShadowStack;
    ThreadStore::RawGetCurrentThread()->InlinePInvokeReturn(pFrame);
}
FCIMPLEND

FCIMPL0(void*, RhpGetCurrentThreadShadowStackBottom)
{
    return ThreadStore::RawGetCurrentThread()->GetShadowStackBottom();
}
FCIMPLEND
