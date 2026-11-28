// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "common.h"
#include "CommonTypes.h"
#include "CommonMacros.h"
#include "daccess.h"
#include "PalLimitedContext.h"
#include "Pal.h"
#include "rhassert.h"
#include "slist.h"
#include "holder.h"
#include "shash.h"
#include "rhbinder.h"
#include "Crst.h"
#include "RuntimeInstance.h"
#include "TypeManager.h"
#include "MethodTable.h"
#include "ObjectLayout.h"
#include "event.h"
#include "regdisplay.h"
#include "StackFrameIterator.h"
#include "thread.h"
#include "threadstore.h"
#include "threadstore.inl"
#include "thread.inl"

#include "interoplibinterface.h"

#include "MethodTable.inl"

#ifdef FEATURE_OBJCMARSHAL

namespace
{
    // The Objective-C Marshal API is designed as a single use per process.
    ObjCMarshalNative::BeginEndCallback s_pBeginEndCallback = nullptr;
    ObjCMarshalNative::HaveReferencesChangedCallback s_pHaveReferencesChangedCallback = nullptr;
    bool s_refCountedHandleRescanActive;

    struct DisableTriggerGcWhileCallingManagedCode
    {
        Thread * m_pThread;

        DisableTriggerGcWhileCallingManagedCode()
        {
            // It is illegal for any of the callouts to trigger a GC.
            m_pThread = ThreadStore::GetCurrentThread();
            m_pThread->SetDoNotTriggerGc();
        }

        ~DisableTriggerGcWhileCallingManagedCode()
        {
            m_pThread->ClearDoNotTriggerGc();
        }
    };

    bool TryGetTaggedMemory(_In_ Object * obj, _Out_ void ** tagged)
    {
        void* fn = obj->GetMethodTable()
                       ->GetTypeManagerPtr()
                       ->AsTypeManager()
                       ->GetClasslibFunction(ClasslibFunctionId::ObjectiveCMarshalTryGetTaggedMemory);

        ASSERT(fn != nullptr);

        auto pTryGetTaggedMemoryCallback = reinterpret_cast<ObjCMarshalNative::TryGetTaggedMemoryCallback>(fn);

        DisableTriggerGcWhileCallingManagedCode disabler{};

        bool result = pTryGetTaggedMemoryCallback(obj, tagged);

        return result;
    }

    // Calls a managed classlib function to potentially get an unmanaged callback.
    // Not for use with ObjectiveCMarshalTryGetTaggedMemory.
    void * GetCallbackViaClasslibCallback(_In_ Object * object, _In_ ClasslibFunctionId id)
    {
        void* fn = object->GetMethodTable()
                        ->GetTypeManagerPtr()
                        ->AsTypeManager()
                        ->GetClasslibFunction(id);

        ASSERT(fn != nullptr);

        DisableTriggerGcWhileCallingManagedCode disabler{};

        fn = ((ObjCMarshalNative::TryGetCallback)fn)();

        ASSERT(fn != nullptr);

        return fn;
    }
}

bool ObjCMarshalNative::RegisterReferenceTrackerCallbacks(void * beginEndCallback, void * haveReferencesChangedCallback)
{
    ASSERT(beginEndCallback != nullptr);

    // We must be in Cooperative mode since we are setting callbacks that
    // will be used during a GC and we want to ensure a GC isn't occurring
    // while they are being set.
    ASSERT(ThreadStore::GetCurrentThread()->IsCurrentThreadInCooperativeMode());

    if (PalInterlockedCompareExchangePointer(&s_pBeginEndCallback, beginEndCallback, nullptr) != nullptr)
        return false;

    s_pHaveReferencesChangedCallback = reinterpret_cast<HaveReferencesChangedCallback>(haveReferencesChangedCallback);
    return true;
}

bool ObjCMarshalNative::IsTrackedReference(_In_ Object * object, _Out_ bool* isReferenced)
{
    *isReferenced = false;

    if (!object->GetGCSafeMethodTable()->IsTrackedReferenceWithFinalizer())
        return false;

    void* taggedMemory;
    if (!TryGetTaggedMemory(object, &taggedMemory))
    {
        // It should not be possible to create a ref-counted handle
        // without setting up the tagged memory first.
        ASSERT(false);
        return false;
    }

    if (s_refCountedHandleRescanActive)
    {
        // Conservatively treat tracked objects as referenced during the rescan.
        *isReferenced = true;
        return true;
    }

    auto pIsReferencedCallbackCallback = (ObjCMarshalNative::IsReferencedCallback)GetCallbackViaClasslibCallback(
        object, ClasslibFunctionId::ObjectiveCMarshalGetIsTrackedReferenceCallback);

    int result = pIsReferencedCallbackCallback(taggedMemory);

    *isReferenced = (result != 0);
    return true;
}

void ObjCMarshalNative::BeforeRefCountedHandleCallbacks()
{
    if (s_pBeginEndCallback != nullptr)
        s_pBeginEndCallback();
}

bool ObjCMarshalNative::ShouldRescanRefCountedHandles()
{
    ASSERT(!s_refCountedHandleRescanActive);

    if (s_pHaveReferencesChangedCallback != nullptr)
        s_refCountedHandleRescanActive = (s_pHaveReferencesChangedCallback() != 0);

    return s_refCountedHandleRescanActive;
}

void ObjCMarshalNative::AfterScanForFinalization()
{
    s_refCountedHandleRescanActive = false;

    if (s_pBeginEndCallback != nullptr)
        s_pBeginEndCallback();
}

void ObjCMarshalNative::OnEnteredFinalizerQueue(_In_ Object * object)
{
    auto pOnEnteredFinalizerQueueCallback = (ObjCMarshalNative::EnteredFinalizationCallback)GetCallbackViaClasslibCallback(
        object, ClasslibFunctionId::ObjectiveCMarshalGetOnEnteredFinalizerQueueCallback);

    void* taggedMemory;
    if (!TryGetTaggedMemory(object, &taggedMemory))
    {
        // Its possible to create an object that supports reference tracking
        // without ever creating a ref-counted handle for it. In this case,
        // there will be no tagged memory.
        return;
    }

    pOnEnteredFinalizerQueueCallback(taggedMemory);
}

#endif // FEATURE_OBJCMARSHAL
