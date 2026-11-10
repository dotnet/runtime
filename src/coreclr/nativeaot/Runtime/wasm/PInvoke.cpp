// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "common.h"
#include "CommonTypes.h"
#include "daccess.h"
#include "thread.h"
#include "threadstore.h"
#include "threadstore.inl"
#include "thread.inl"

// Does the pinvoke frame transition; the naked wrappers below have already set the wasm
// __stack_pointer global to callersStackPointer so it is safe to run native code here.
EXTERN_C void RhpPInvokeImpl(uintptr_t callersStackPointer, PInvokeTransitionFrame* pFrame)
{
    Thread* pCurThread = ThreadStore::RawGetCurrentThread();
    pFrame->m_ShadowSP = callersStackPointer;

    // Link the frame and transition to preemptive GC mode for the native call.
    pCurThread->InlinePInvoke(pFrame);
}

// Managed code keeps its shadow SP in a local and leaves the __stack_pointer global stale, so publish
// the incoming sp to __stack_pointer before any native code runs and leave it there so the
// subsequent native pinvoke target is also safe.
EXTERN_C __attribute__((naked)) void RhpPInvoke(void* sp, PInvokeTransitionFrame* pFrame)
{
    asm("local.get 0\n"                /* sp */
        "global.set __stack_pointer\n" /* __stack_pointer = sp before any native code runs */
        "local.get 0\n"                /* sp */
        "local.get 1\n"                /* pFrame */
        "call %0\n"
        "return" ::"i"(RhpPInvokeImpl));
}

#ifdef _DEBUG
// Debug variant of these apis tests that sp and __stack_pointer are in sync
EXTERN_C void RhpPInvokeReturnImpl(uintptr_t sp, uintptr_t stack_pointer_global_value, PInvokeTransitionFrame* pFrame)
{
    ASSERT(sp == stack_pointer_global_value);

    // Reenter cooperative mode and rendezvous with a running collection if necessary.
    pFrame->m_pThread->InlinePInvokeReturn(pFrame);
}

EXTERN_C __attribute__((naked)) void RhpPInvokeReturn(void* sp, PInvokeTransitionFrame* pFrame)
{
    asm(
        "local.get 0\n"                /* sp */
        "global.get __stack_pointer\n" /* __stack_pointer */
        "local.get 1\n"                /* pFrame */
        "local.get 0\n"                /* sp */
        "global.set __stack_pointer\n" /* __stack_pointer = sp before any native code runs, set this here, so that if the assumption around sp == __stack_pointer is wrong the assert logic will work correctly. */
        "call %0\n"
        "return" ::"i"(RhpPInvokeReturnImpl));
}
#else // _DEBUG
EXTERN_C void RhpPInvokeReturn(void* sp, PInvokeTransitionFrame* pFrame)
{
    // Reenter cooperative mode and rendezvous with a running collection if necessary.
    pFrame->m_pThread->InlinePInvokeReturn(pFrame);
}
#endif // _DEBUG
