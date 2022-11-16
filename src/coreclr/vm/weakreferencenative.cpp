// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*============================================================
**
** Implementation: WeakReferenceNative.cpp
**
**
===========================================================*/

#include "common.h"

#include "weakreferencenative.h"
#include "interoplibinterface.h"

// This entrypoint is used for eager finalization by the GC.
void FinalizeWeakReference(Object* obj)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    // Eager finalization happens while scanning for unmarked finalizable objects
    // after marking strongly reachable and prior to marking dependent and long weak handles.
    // Managed code should not be running.
    _ASSERTE(GCHeapUtilities::IsGCInProgress());

    // the lowermost 2 bits are reserved for storing additional info about the handle
    // we can use these bits because handle is at least 4 byte aligned
    const uintptr_t HandleTagBits = 3;

    WeakReferenceObject* weakRefObj = (WeakReferenceObject*)obj;
    OBJECTHANDLE handle = (OBJECTHANDLE)(weakRefObj->m_taggedHandle & ~HandleTagBits);
    HandleType handleType = (weakRefObj->m_taggedHandle & 2) ?
        HandleType::HNDTYPE_STRONG :
        (weakRefObj->m_taggedHandle & 1) ?
        HandleType::HNDTYPE_WEAK_LONG :
        HandleType::HNDTYPE_WEAK_SHORT;

    // keep the bit that indicates whether this reference was tracking resurrection, clear the rest.
    weakRefObj->m_taggedHandle &= (uintptr_t)1;
    GCHandleUtilities::GetGCHandleManager()->DestroyHandleOfType(handle, handleType);
}

#if defined(FEATURE_COMINTEROP) || defined(FEATURE_COMWRAPPERS)

// static
extern "C" void QCALLTYPE ComWeakRefToObject(IWeakReference* pComWeakReference, INT64 wrapperId, QCall::ObjectHandleOnStack retRcw)
{
    QCALL_CONTRACT;
    BEGIN_QCALL;

    _ASSERTE(pComWeakReference != nullptr);

    // If the weak reference was in a state that it had an IWeakReference* for us to use, then we need to find the IUnknown
    // identity of the underlying COM object (assuming that object is still alive).
    SafeComHolder<IUnknown> pTargetIdentity = nullptr;

    // Using the IWeakReference*, get ahold of the target native COM object's IInspectable*.  If this resolve fails, then we
    // assume that the underlying native COM object is no longer alive, and thus we cannot create a new RCW for it.
    SafeComHolderPreemp<IInspectable> pTarget = nullptr;
    if (SUCCEEDED(pComWeakReference->Resolve(IID_IInspectable, &pTarget)))
    {
        if (!pTarget.IsNull())
        {
            // Get the IUnknown identity for the underlying object
            SafeQueryInterfacePreemp(pTarget, IID_IUnknown, &pTargetIdentity);
        }
    }

    // If we were able to get an IUnknown identity for the object, then we can find or create an associated RCW for it.
    if (!pTargetIdentity.IsNull())
    {
        GCX_COOP();
        OBJECTREF rcwRef = NULL;
        GCPROTECT_BEGIN(rcwRef);

        if (wrapperId != ComWrappersNative::InvalidWrapperId)
        {
            // Try the global COM wrappers
            if (GlobalComWrappersForTrackerSupport::IsRegisteredInstance(wrapperId))
            {
                (void)GlobalComWrappersForTrackerSupport::TryGetOrCreateObjectForComInstance(pTargetIdentity, &rcwRef);
            }
            else if (GlobalComWrappersForMarshalling::IsRegisteredInstance(wrapperId))
            {
                (void)GlobalComWrappersForMarshalling::TryGetOrCreateObjectForComInstance(pTargetIdentity, ObjFromComIP::NONE, &rcwRef);
            }
        }
#ifdef FEATURE_COMINTEROP
        else
        {
            // If the original RCW was not created through ComWrappers, fall back to the built-in system.
            GetObjectRefFromComIP(&rcwRef, pTargetIdentity);
        }
#endif // FEATURE_COMINTEROP
        GCPROTECT_END();
        retRcw.Set(rcwRef);
    }

    END_QCALL;
    return;
}

// static
extern "C" IWeakReference * QCALLTYPE ObjectToComWeakRef(QCall::ObjectHandleOnStack obj, INT64* pWrapperId)
{
    QCALL_CONTRACT;

    IWeakReference* pWeakReference = nullptr;
    BEGIN_QCALL;

    *pWrapperId = ComWrappersNative::InvalidWrapperId;
    SafeComHolder<IWeakReferenceSource> pWeakReferenceSource(nullptr);
    _ASSERTE(obj.m_ppObject != nullptr);

    {
        // COM helpers assume COOP mode and the arguments are protected refs.
        GCX_COOP();
        OBJECTREF objRef = obj.Get();
        GCPROTECT_BEGIN(objRef);

        // If the object is not an RCW, then we do not want to use a native COM weak reference to it
        // If the object is a managed type deriving from a COM type, then we also do not want to use a native COM
        // weak reference to it.  (Otherwise, we'll wind up resolving IWeakReference-s back into the CLR
        // when we don't want to have reentrancy).
#ifdef FEATURE_COMINTEROP
        MethodTable* pMT = objRef->GetMethodTable();
        if (pMT->IsComObjectType()
            && (pMT == g_pBaseCOMObject || !pMT->IsExtensibleRCW()))
        {
            pWeakReferenceSource = reinterpret_cast<IWeakReferenceSource*>(GetComIPFromObjectRef(&objRef, IID_IWeakReferenceSource, false /* throwIfNoComIP */));
        }
        else
#endif
        {
#ifdef FEATURE_COMWRAPPERS
            bool isAggregated = false;
            pWeakReferenceSource = reinterpret_cast<IWeakReferenceSource*>(ComWrappersNative::GetIdentityForObject(&objRef, IID_IWeakReferenceSource, pWrapperId, &isAggregated));
            if (isAggregated)
            {
                // If the RCW is an aggregated RCW, then the managed object cannot be recreated from the IUnknown as the outer IUnknown wraps the managed object.
                // In this case, don't create a weak reference backed by a COM weak reference.
                pWeakReferenceSource = nullptr;
            }
#endif
        }

        GCPROTECT_END();
    }

    if (pWeakReferenceSource != nullptr)
    {
        SafeComHolderPreemp<IWeakReference> weakReferenceHolder;
        if (!FAILED(pWeakReferenceSource->GetWeakReference(&weakReferenceHolder)))
        {
            weakReferenceHolder.SuppressRelease();
            pWeakReference = weakReferenceHolder.GetValue();
        }
    }

    END_QCALL;
    return pWeakReference;
}

FCIMPL1(FC_BOOL_RET, ComAwareWeakReferenceNative::HasInteropInfo, Object* pObject)
{
    FCALL_CONTRACT;
    _ASSERTE(pObject != nullptr);

    SyncBlock* pSyncBlock = pObject->PassiveGetSyncBlock();
    _ASSERTE(pSyncBlock != nullptr);
    return pSyncBlock->GetInteropInfoNoCreate() != nullptr;
}
FCIMPLEND

#endif // FEATURE_COMINTEROP || FEATURE_COMWRAPPERS
