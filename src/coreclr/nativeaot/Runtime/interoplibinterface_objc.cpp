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
#include "gcrhinterface.h"
#include "shash.h"
#include "RWLock.h"
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

#include "interoplibinterface.h"

#include "MethodTable.inl"

#ifdef FEATURE_OBJCMARSHAL

namespace
{
    struct GcBeginEndCallbacks
    {
        ObjCMarshalNative::BeginEndCallback m_pCallbackFunction;
        // May be NULL for the end of the linked list
        GcBeginEndCallbacks * m_pNext;
    };

    CrstStatic s_sLock;
    GcBeginEndCallbacks * m_pBeginEndCallbacks = nullptr;

    void CallBeginEndCallbacks()
    {
        GcBeginEndCallbacks * pCallback = m_pBeginEndCallbacks;
        while (pCallback != nullptr)
        {
            ObjCMarshalNative::BeginEndCallback fn = pCallback->m_pCallbackFunction;
            fn();
            pCallback = pCallback->m_pNext;
        }
    }

    bool TryGetTaggedMemory(_In_ Object * obj, _Out_ void ** tagged)
    {
        void* fn = obj->get_EEType()
                      ->GetTypeManagerPtr()
                      ->AsTypeManager()
                      ->GetClasslibFunction(ClasslibFunctionId::ObjectiveCMarshalTryGetTaggedMemory);
        
        if (fn == nullptr)
            return false;
        
        auto pTryGetTaggedMemoryCallback = reinterpret_cast<ObjCMarshalNative::TryGetTaggedMemoryCallback>(fn);

        // It is illegal for any of the callouts to trigger a GC.
        Thread * pThread = ThreadStore::GetCurrentThread();
        pThread->SetDoNotTriggerGc();

        // Due to the above we have better suppress GC stress.
        bool fGcStressWasSuppressed = pThread->IsSuppressGcStressSet();
        if (!fGcStressWasSuppressed)
            pThread->SetSuppressGcStress();

        bool result = pTryGetTaggedMemoryCallback(obj, tagged);

        // Revert GC stress mode if we changed it.
        if (!fGcStressWasSuppressed)
            pThread->ClearSuppressGcStress();

        pThread->ClearDoNotTriggerGc();

        return result;
    }

    template <typename T>
    T TryGetCallbackViaClasslib(_In_ Object * object, _In_ ClasslibFunctionId id)
    {
        void* fn = object->get_EEType()
                        ->GetTypeManagerPtr()
                        ->AsTypeManager()
                        ->GetClasslibFunction(id);

        if (fn != nullptr)
        {
            fn = ((ObjCMarshalNative::TryGetCallback)fn)();
        }

        return (T)fn;
    }
}

// One time startup initialization.
bool ObjCMarshalNative::Initialize()
{
    s_sLock.Init(CrstObjectiveCMarshalCallouts, CRST_DEFAULT);

    return true;
}

bool ObjCMarshalNative::RegisterBeginEndCallback(void * callback)
{
    _ASSERTE(callback != nullptr);

     // We must be in Cooperative mode since we are setting callbacks that
     // will be used during a GC and we want to ensure a GC isn't occurring
     // while they are being set.
     _ASSERTE(ThreadStore::GetCurrentThread()->IsCurrentThreadInCooperativeMode());
    
    GcBeginEndCallbacks * pCallback = new (nothrow) GcBeginEndCallbacks();
    if (pCallback == NULL)
        return false;
    
    pCallback->m_pCallbackFunction = (ObjCMarshalNative::BeginEndCallback)callback;

    CrstHolder lh(&s_sLock);

    pCallback->m_pNext = m_pBeginEndCallbacks;
    m_pBeginEndCallbacks = pCallback;

    return true;
}

bool ObjCMarshalNative::IsTrackedReference(_In_ Object * object, _Out_ bool* isReferenced)
{
    *isReferenced = false;

    if (!object->GetGCSafeMethodTable()->IsTrackedReferenceWithFinalizer())
        return false;
    
    auto pIsReferencedCallbackCallback = TryGetCallbackViaClasslib<ObjCMarshalNative::IsReferencedCallback>(
        object, ClasslibFunctionId::ObjectiveCMarshalGetIsTrackedReferenceCallback);

    if (pIsReferencedCallbackCallback == nullptr)
        return false;

    void* taggedMemory;
    if (!TryGetTaggedMemory(object, &taggedMemory))
        return false;
    
    int result = pIsReferencedCallbackCallback(taggedMemory);

    *isReferenced = (result != 0);
    return true;
}

void ObjCMarshalNative::BeforeRefCountedHandleCallbacks()
{
    CallBeginEndCallbacks();
}

void ObjCMarshalNative::AfterRefCountedHandleCallbacks()
{
    CallBeginEndCallbacks();
}

void ObjCMarshalNative::OnEnteredFinalizerQueue(_In_ Object * object)
{
    auto pOnEnteredFinalizerQueueCallback = TryGetCallbackViaClasslib<ObjCMarshalNative::EnteredFinalizationCallback>(
        object, ClasslibFunctionId::ObjectiveCMarshalGetOnEnteredFinalizerQueueCallback);

    if (pOnEnteredFinalizerQueueCallback == nullptr)
        return;

    void* taggedMemory;
    if (!TryGetTaggedMemory(object, &taggedMemory))
        return;

    pOnEnteredFinalizerQueueCallback(taggedMemory);
}

#endif // FEATURE_OBJCMARSHAL
