// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "common.h"
#include "CommonTypes.h"
#include "CommonMacros.h"
#include "daccess.h"
#include "PalRedhawkCommon.h"
#include "PalRedhawk.h"
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
#include "varint.h"
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
    // TODO: Support registering multiple begin/end callbacks.
    // NativeAOT support the concept of multiple managed "worlds",
    // each with their own object heirarchy. Each of these worlds
    // could potentially have its own set of callbacks.
    // However this Objective-C Marshal API was not designed with such a
    // possibility in mind.
    ObjCMarshalNative::BeginEndCallback s_pBeginEndCallback = nullptr;

    struct DisableTriggerGcWhileCallingManagedCode
    {
        Thread * m_pThread;
        bool m_fGcStressWasSuppressed;

        DisableTriggerGcWhileCallingManagedCode()
        {
            // It is illegal for any of the callouts to trigger a GC.
            m_pThread = ThreadStore::GetCurrentThread();
            m_pThread->SetDoNotTriggerGc();

            // Due to the above we have better suppress GC stress.
            m_fGcStressWasSuppressed = m_pThread->IsSuppressGcStressSet();
            if (!m_fGcStressWasSuppressed)
                m_pThread->SetSuppressGcStress();
        }

        ~DisableTriggerGcWhileCallingManagedCode()
        {
            // Revert GC stress mode if we changed it.
            if (!m_fGcStressWasSuppressed)
                m_pThread->ClearSuppressGcStress();

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

bool ObjCMarshalNative::RegisterBeginEndCallback(void * callback)
{
    ASSERT(callback != nullptr);

    // We must be in Cooperative mode since we are setting callbacks that
    // will be used during a GC and we want to ensure a GC isn't occurring
    // while they are being set.
    ASSERT(ThreadStore::GetCurrentThread()->IsCurrentThreadInCooperativeMode());

    return PalInterlockedCompareExchangePointer(&s_pBeginEndCallback, callback, nullptr) == nullptr;
}

bool ObjCMarshalNative::IsTrackedReference(_In_ Object * object, _Out_ bool* isReferenced)
{
    *isReferenced = false;

    if (!object->GetGCSafeMethodTable()->IsTrackedReferenceWithFinalizer())
        return false;

    auto pIsReferencedCallbackCallback = (ObjCMarshalNative::IsReferencedCallback)GetCallbackViaClasslibCallback(
        object, ClasslibFunctionId::ObjectiveCMarshalGetIsTrackedReferenceCallback);

    void* taggedMemory;
    if (!TryGetTaggedMemory(object, &taggedMemory))
    {
        // It should not be possible to create a ref-counted handle
        // without setting up the tagged memory first.
        ASSERT(false);
        return false;
    }

    int result = pIsReferencedCallbackCallback(taggedMemory);

    *isReferenced = (result != 0);
    return true;
}

void ObjCMarshalNative::BeforeRefCountedHandleCallbacks()
{
    if (s_pBeginEndCallback != nullptr)
        s_pBeginEndCallback();
}

void ObjCMarshalNative::AfterRefCountedHandleCallbacks()
{
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
