// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <cstdio>

#include "common.h"
#include "gcenv.h"
#include "gcenv.ee.h"
#include "gcheaputilities.h"
#include "gchandleutilities.h"

#include "thread.h"
#include "threadstore.h"
#include "threadstore.inl"
#include "thread.inl"

#include "wasm.h"

FCIMPL2(void*, RhpGcStressOnce, void* obj, uint8_t* pFlag)
{
    if (*pFlag)
    {
        // This helper will only stress each safe point once.
        return obj;
    }

    // The GarbageCollect operation below may trash the last win32 error. We save the error here so that it can be
    // restored after the GC operation;
    int32_t lastErrorOnEntry = PalGetLastError();

    Thread* pThread = ThreadStore::GetCurrentThread();
    if (!pThread->IsSuppressGcStressSet() && !pThread->IsDoNotTriggerGcSet())
    {
        // GC-protect our exposed object.
        GCFrameRegistration gc;
        if (obj != nullptr)
        {
            gc.m_pThread = pThread;
            gc.m_pObjRefs = &obj;
            gc.m_numObjRefs = 1;
            gc.m_MaybeInterior = 1;
            pThread->PushGCFrameRegistration(&gc);
        }

        bool isCooperative = pThread->IsCurrentThreadInCooperativeMode();
        if (isCooperative)
        {
            pThread->SetDeferredTransitionFrame((PInvokeTransitionFrame*)pShadowStack);
        }
        else // We can be called in preemptive mode - on an exit from a PInvoke.
        {
            ASSERT(obj == nullptr);
            pThread->DeferTransitionFrame();
            pThread->DisablePreemptiveMode();
        }
        GCHeapUtilities::GetGCHeap()->GarbageCollect();
        if (!isCooperative)
        {
            pThread->EnablePreemptiveMode();
        }

        if (obj != nullptr)
        {
            pThread->PopGCFrameRegistration(&gc);
        }
        *pFlag = true;
    }

    // Restore the saved error
    PalSetLastError(lastErrorOnEntry);
    return obj;
}
FCIMPLEND

FCIMPL_NO_SS(Object*, RhpCheckObj, Object* obj)
{
    if (obj != nullptr)
    {
        MethodTable* pMT = obj->GetMethodTable();
        if (!pMT->Validate())
        {
            printf("Corrupt object/pMT: [%p]/[%p]\n", obj, pMT);
            RhFailFast();
        }
    }

    return obj;
}
FCIMPLEND
