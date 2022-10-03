// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*============================================================
**
** Implementation: WeakReferenceNative.cpp
**
**
===========================================================*/

#include "common.h"

#include "gchandleutilities.h"
#include "weakreferencenative.h"
#include "typestring.h"
#include "typeparse.h"
#include "threadsuspend.h"
#include "interoplibinterface.h"

//************************************************************************

// We use several special values of the handle to track extra state without increasing the instance size.
const LPVOID specialWeakReferenceHandles[3] = { 0, 0, 0 };

// SPECIAL_HANDLE_SPINLOCK is used to implement spinlock that protects against races between setting the target and finalization
#define SPECIAL_HANDLE_SPINLOCK ((OBJECTHANDLE)(&specialWeakReferenceHandles[0]))

// SPECIAL_HANDLE_FINALIZED is used to track the original type of the handle so that IsTrackResurrection keeps working on finalized
// objects for backward compatibility.
#define SPECIAL_HANDLE_FINALIZED_SHORT  ((OBJECTHANDLE)(&specialWeakReferenceHandles[1]))
#define SPECIAL_HANDLE_FINALIZED_LONG   ((OBJECTHANDLE)(&specialWeakReferenceHandles[2]))

#define IS_SPECIAL_HANDLE(h) ((size_t)(h) - (size_t)(&specialWeakReferenceHandles) < sizeof(specialWeakReferenceHandles))

//
// A WeakReference instance can hold one of three types of handles - short or long weak handles,
// or a native COM weak reference handle.  The native COM weak reference handle has the extra capability
// of recreating an RCW for a COM object which is still alive even though the previous RCW had
// been collected.   In order to differentiate this type of handle from the standard weak handles,
// the bottom bit is stolen.
//
// Note that the bit is stolen only in the local copy of the object handle, held in the m_handle
// field of the weak reference object.  The handle in the handle table itself does not have its
// bottom bit stolen, and requires using HandleFetchType to determine what type it is.  The bit
// is strictly a performance optimization for the weak reference implementation, and it is
// responsible for setting up the bit as it needs and ensuring that it is cleared whenever an
// object handle leaves the weak reference code, for instance to interact with the handle table
// or diagnostics tools.
//
// The following functions are to set, test, and unset that bit before the handle is used.
//

// Determine if an object handle is a native COM weak reference handle
bool IsNativeComWeakReferenceHandle(OBJECTHANDLE handle)
{
    STATIC_CONTRACT_LEAF;
    return (reinterpret_cast<UINT_PTR>(handle) & 0x1) != 0x0;
}

// Mark an object handle as being a native COM weak reference handle
OBJECTHANDLE SetNativeComWeakReferenceHandle(OBJECTHANDLE handle)
{
    STATIC_CONTRACT_LEAF;

    _ASSERTE(!IsNativeComWeakReferenceHandle(handle));
    return reinterpret_cast<OBJECTHANDLE>(reinterpret_cast<UINT_PTR>(handle) | 0x1);
}

// Get the object handle value even if the object is a native COM weak reference
OBJECTHANDLE GetHandleValue(OBJECTHANDLE handle)
{
    STATIC_CONTRACT_LEAF;
    UINT_PTR mask = ~(static_cast<UINT_PTR>(0x1));
    return reinterpret_cast<OBJECTHANDLE>(reinterpret_cast<UINT_PTR>(handle) & mask);
}

FORCEINLINE OBJECTHANDLE AcquireWeakHandleSpinLock(WEAKREFERENCEREF pThis);
FORCEINLINE void ReleaseWeakHandleSpinLock(WEAKREFERENCEREF pThis, OBJECTHANDLE newHandle);

struct WeakHandleSpinLockHolder
{
    OBJECTHANDLE RawHandle;
    OBJECTHANDLE Handle;
    WEAKREFERENCEREF* pWeakReference;

    WeakHandleSpinLockHolder(OBJECTHANDLE rawHandle, WEAKREFERENCEREF* weakReference)
        : RawHandle(rawHandle), Handle(GetHandleValue(rawHandle)), pWeakReference(weakReference)
    {
        STATIC_CONTRACT_LEAF;
    }

    ~WeakHandleSpinLockHolder()
    {
        WRAPPER_NO_CONTRACT;
        ReleaseWeakHandleSpinLock(*pWeakReference, RawHandle);
    }

private:
    WeakHandleSpinLockHolder(const WeakHandleSpinLockHolder& other);
    WeakHandleSpinLockHolder& operator=(const WeakHandleSpinLockHolder& other);
};

#if defined(FEATURE_COMINTEROP) || defined(FEATURE_COMWRAPPERS)

// Get the native COM information for the object underlying an RCW if applicable. If the incoming object cannot
// use a native COM weak reference, nullptr is returned. Otherwise, a new NativeComWeakHandleInfo containing an
// AddRef-ed IWeakReference* for the COM object underlying the RCW is returned.
//
// In order to qualify to be used with a HNDTYPE_WEAK_NATIVE_COM, the incoming object must:
//  * be an RCW
//  * not be an aggregated RCW
//  * respond to a QI for IWeakReferenceSource
//  * succeed when asked for an IWeakReference*
//
// Note that *pObject should be GC protected on the way into this method
NativeComWeakHandleInfo* GetComWeakReferenceInfo(OBJECTREF* pObject)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        PRECONDITION(CheckPointer(pObject));
    }
    CONTRACTL_END;

    if (*pObject == NULL)
    {
        return nullptr;
    }

    ASSERT_PROTECTED(pObject);

    MethodTable* pMT = (*pObject)->GetMethodTable();

    SafeComHolder<IWeakReferenceSource> pWeakReferenceSource(nullptr);
    INT64 wrapperId = ComWrappersNative::InvalidWrapperId;

    // If the object is not an RCW, then we do not want to use a native COM weak reference to it
    // If the object is a managed type deriving from a COM type, then we also do not want to use a native COM
    // weak reference to it.  (Otherwise, we'll wind up resolving IWeakReference-s back into the CLR
    // when we don't want to have reentrancy).
#ifdef FEATURE_COMINTEROP
    if (pMT->IsComObjectType()
     && (pMT == g_pBaseCOMObject || !pMT->IsExtensibleRCW()))
    {
        pWeakReferenceSource = reinterpret_cast<IWeakReferenceSource*>(GetComIPFromObjectRef(pObject, IID_IWeakReferenceSource, false /* throwIfNoComIP */));
    }
    else
#endif
    {
#ifdef FEATURE_COMWRAPPERS
        bool isAggregated = false;
        pWeakReferenceSource = reinterpret_cast<IWeakReferenceSource*>(ComWrappersNative::GetIdentityForObject(pObject, IID_IWeakReferenceSource, &wrapperId, &isAggregated));
        if (isAggregated)
        {
            // If the RCW is an aggregated RCW, then the managed object cannot be recreated from the IUnknown as the outer IUnknown wraps the managed object.
            // In this case, don't create a weak reference backed by a COM weak reference.
            pWeakReferenceSource = nullptr;
        }
#endif
    }

    if (pWeakReferenceSource == nullptr)
    {
        return nullptr;
    }

    GCX_PREEMP();
    SafeComHolderPreemp<IWeakReference> pWeakReference;
    if (FAILED(pWeakReferenceSource->GetWeakReference(&pWeakReference)))
    {
        return nullptr;
    }

    NewHolder<NativeComWeakHandleInfo> info = new NativeComWeakHandleInfo { pWeakReference.GetValue(), wrapperId };
    pWeakReference.SuppressRelease();
    return info.Extract();
}

// Given an object handle that stores a native COM weak reference, attempt to create an RCW
// and store it back in the handle, returning the RCW.  If the underlying native COM object
// is not alive, then the result is NULL.
//
// In order to create a new RCW, we must:
//   * Have an m_handle of HNDTYPE_WEAK_NATIVE_COM (ie the bottom bit of m_handle is set)
//   * Have stored an IWeakReference* in the handle extra info when setting up the handle
//     (see GetComWeakReference)
//   * The IWeakReference* must respond to a Resolve request for IID_IInspectable
//   *
NOINLINE Object* LoadComWeakReferenceTarget(WEAKREFERENCEREF weakReference, TypeHandle targetType, LPVOID __me)
{
    CONTRACTL
    {
        FCALL_CHECK;
        PRECONDITION(weakReference != NULL);
    }
    CONTRACTL_END;

    struct
    {
        WEAKREFERENCEREF weakReference;
        OBJECTREF rcw;
        OBJECTREF target;
    } gc;
    ZeroMemory(&gc, sizeof(gc));
    gc.weakReference = weakReference;

    FC_INNER_PROLOG_NO_ME_SETUP();
    HELPER_METHOD_FRAME_BEGIN_RET_ATTRIB_PROTECT(Frame::FRAME_ATTR_EXACT_DEPTH|Frame::FRAME_ATTR_CAPTURE_DEPTH_2, gc);

    // Acquire the spin lock to get the IWeakReference* associated with the weak reference.  We will then need to
    // release the lock while resolving the IWeakReference* since we need to enter preemptive mode while calling out
    // to COM to resolve the object and we don't want to do that while holding the lock.  If we wind up being able
    // to geenrate a new RCW, we'll reacquire the lock to save the RCW in the handle.
    //
    // Since we're acquiring and releasing the lock multiple times, we need to check the handle state each time we
    // reacquire the lock to make sure that another thread hasn't reassigned the target of the handle or finalized it
    SafeComHolder<IWeakReference> pComWeakReference = nullptr;
    INT64 wrapperId = ComWrappersNative::InvalidWrapperId;
    {
        WeakHandleSpinLockHolder handle(AcquireWeakHandleSpinLock(gc.weakReference), &gc.weakReference);
        GCX_NOTRIGGER();

        // Make sure that while we were not holding the spin lock, another thread did not change the target of
        // this weak reference.  Only fetch the IWeakReference* if we still have a valid handle holding a NULL object
        // and the handle is still a HNDTYPE_WEAK_NATIVE_COM type handle.
        if ((handle.Handle != NULL) && !IS_SPECIAL_HANDLE(handle.Handle))
        {
            if (*(Object **)(handle.Handle) != NULL)
            {
                // While we released the spin lock, another thread already set a new target for the weak reference.
                // We don't want to replace it with an RCW that we fetch, so save it to return as the object the
                // weak reference is targeting.
                gc.target = ObjectToOBJECTREF(*(Object **)(handle.Handle));
            }
            else if(IsNativeComWeakReferenceHandle(handle.RawHandle))
            {
                _ASSERTE(GCHandleUtilities::GetGCHandleManager()->HandleFetchType(handle.Handle) == HNDTYPE_WEAK_NATIVE_COM);

                // Retrieve the associated IWeakReference* for this weak reference.  Add a reference to it while we release
                // the spin lock so that another thread doesn't release it out from underneath us.
                //
                // Setting pComWeakReference will claim that it triggers a GC, however that's not true in this case because
                // it's always set to NULL here and there's nothing for it to release.
                _ASSERTE(pComWeakReference.IsNull());
                CONTRACT_VIOLATION(GCViolation);
                IGCHandleManager *mgr = GCHandleUtilities::GetGCHandleManager();
                NativeComWeakHandleInfo* comWeakHandleInfo = reinterpret_cast<NativeComWeakHandleInfo*>(mgr->GetExtraInfoFromHandle(handle.Handle));
                if (comWeakHandleInfo != nullptr)
                {
                    wrapperId = comWeakHandleInfo->WrapperId;
                    pComWeakReference = comWeakHandleInfo->WeakReference;
                    pComWeakReference->AddRef();
                }
            }
        }
    }

    // If the weak reference was in a state that it had an IWeakReference* for us to use, then we need to find the IUnknown
    // identity of the underlying COM object (assuming that object is still alive).  This work is done without holding the
    // spin lock since it will call out to arbitrary code and as such we need to switch to preemptive mode.
    SafeComHolder<IUnknown> pTargetIdentity = nullptr;
    if (pComWeakReference != nullptr)
    {
        _ASSERTE(gc.target == NULL);

        GCX_PREEMP();

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
    }

    // If we were able to get an IUnknown identity for the object, then we can find or create an associated RCW for it.
    if (!pTargetIdentity.IsNull())
    {
        if (wrapperId != ComWrappersNative::InvalidWrapperId)
        {
            // Try the global COM wrappers
            if (GlobalComWrappersForTrackerSupport::IsRegisteredInstance(wrapperId))
            {
                (void)GlobalComWrappersForTrackerSupport::TryGetOrCreateObjectForComInstance(pTargetIdentity, &gc.rcw);
            }
            else if (GlobalComWrappersForMarshalling::IsRegisteredInstance(wrapperId))
            {
                (void)GlobalComWrappersForMarshalling::TryGetOrCreateObjectForComInstance(pTargetIdentity, ObjFromComIP::NONE, &gc.rcw);
            }
        }
#ifdef FEATURE_COMINTEROP
        else
        {
            // If the original RCW was not created through ComWrappers, fall back to the built-in system.
            GetObjectRefFromComIP(&gc.rcw, pTargetIdentity);
        }
#endif // FEATURE_COMINTEROP
    }

    // If we were able to get an RCW, then we need to reacquire the spin lock and store the RCW in the handle.  Note that
    // it's possible that another thread has acquired the spin lock and set the target of the weak reference while we were
    // building the RCW.  In that case, we will defer to the hadle that the other thread set, and let the RCW die.
    if (gc.rcw != NULL)
    {
        // Make sure the type we got back from the native COM object is compatible with the type the managed
        // weak reference expects.  (For instance, in the WeakReference<T> case, the returned type
        // had better be compatible with T).
        TypeHandle rcwType(gc.rcw->GetMethodTable());
        if (!rcwType.CanCastTo(targetType))
        {
            SString weakReferenceTypeName;
            TypeString::AppendType(weakReferenceTypeName, targetType, TypeString::FormatNamespace | TypeString::FormatFullInst | TypeString::FormatAssembly);

            SString resolvedTypeName;
            TypeString::AppendType(resolvedTypeName, rcwType, TypeString::FormatNamespace | TypeString::FormatFullInst | TypeString::FormatAssembly);

            COMPlusThrow(kInvalidCastException, IDS_EE_NATIVE_COM_WEAKREF_BAD_TYPE, weakReferenceTypeName.GetUnicode(), resolvedTypeName.GetUnicode());
        }

        WeakHandleSpinLockHolder handle(AcquireWeakHandleSpinLock(gc.weakReference), &gc.weakReference);
        GCX_NOTRIGGER();


        // Now that we've reacquired the lock, see if the handle is still empty.  If so, then save the RCW as the new target of the handle.
        if ((handle.Handle != NULL) && !IS_SPECIAL_HANDLE(handle.Handle))
        {
            _ASSERTE(gc.target == NULL);
            gc.target = ObjectToOBJECTREF(*(Object **)(handle.Handle));

            if (gc.target == NULL)
            {
                StoreObjectInHandle(handle.Handle, gc.rcw);
                gc.target = gc.rcw;
            }
        }
    }

    HELPER_METHOD_FRAME_END();
    FC_INNER_EPILOG();

    return OBJECTREFToObject(gc.target);
}

#endif // FEATURE_COMINTEROP || FEATURE_COMWRAPPERS

//************************************************************************

//
// Spinlock implemented by overloading the WeakReference::m_Handle field that protects against races between setting
// the target and finalization
//

NOINLINE OBJECTHANDLE AcquireWeakHandleSpinLockSpin(WEAKREFERENCEREF pThis)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    DWORD dwSwitchCount = 0;
    YieldProcessorNormalizationInfo normalizationInfo;

    //
    // Boilerplate spinning logic stolen from other locks
    //
    for (;;)
    {
        if (g_SystemInfo.dwNumberOfProcessors > 1)
        {
            DWORD spincount = g_SpinConstants.dwInitialDuration;

            for (;;)
            {
                YieldProcessorNormalizedForPreSkylakeCount(normalizationInfo, spincount);

                OBJECTHANDLE handle = InterlockedExchangeT(&pThis->m_Handle, SPECIAL_HANDLE_SPINLOCK);
                if (handle != SPECIAL_HANDLE_SPINLOCK)
                    return handle;

                spincount *= g_SpinConstants.dwBackoffFactor;
                if (spincount > g_SpinConstants.dwMaximumDuration)
                {
                    break;
                }
            }
        }

        __SwitchToThread(0, ++dwSwitchCount);

        OBJECTHANDLE handle = InterlockedExchangeT(&pThis->m_Handle, SPECIAL_HANDLE_SPINLOCK);
        if (handle != SPECIAL_HANDLE_SPINLOCK)
            return handle;
    }
}

FORCEINLINE OBJECTHANDLE AcquireWeakHandleSpinLock(WEAKREFERENCEREF pThis)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    OBJECTHANDLE handle = InterlockedExchangeT(&pThis->m_Handle, SPECIAL_HANDLE_SPINLOCK);
    if (handle != SPECIAL_HANDLE_SPINLOCK)
        return handle;
    return AcquireWeakHandleSpinLockSpin(pThis);
}

FORCEINLINE void ReleaseWeakHandleSpinLock(WEAKREFERENCEREF pThis, OBJECTHANDLE newHandle)
{
    LIMITED_METHOD_CONTRACT;

    _ASSERTE(newHandle != SPECIAL_HANDLE_SPINLOCK);
    pThis->m_Handle = newHandle;
}

//************************************************************************

MethodTable *pWeakReferenceMT = NULL;
MethodTable *pWeakReferenceOfTCanonMT = NULL;

//************************************************************************

FCIMPL3(void, WeakReferenceNative::Create, WeakReferenceObject * pThisUNSAFE, Object * pTargetUNSAFE, CLR_BOOL trackResurrection)
{
    FCALL_CONTRACT;

    struct
    {
        WEAKREFERENCEREF pThis;
        OBJECTREF pTarget;
    } gc;

    gc.pThis = WEAKREFERENCEREF(pThisUNSAFE);
    gc.pTarget = OBJECTREF(pTargetUNSAFE);

    HELPER_METHOD_FRAME_BEGIN_PROTECT(gc);

    if (gc.pThis == NULL)
        COMPlusThrow(kNullReferenceException);

    if (pWeakReferenceMT == NULL)
        pWeakReferenceMT = CoreLibBinder::GetClass(CLASS__WEAKREFERENCE);

    _ASSERTE(gc.pThis->GetMethodTable()->CanCastToClass(pWeakReferenceMT));

    // Create the handle.
#if defined(FEATURE_COMINTEROP) || defined(FEATURE_COMWRAPPERS)
    NativeComWeakHandleInfo *comWeakHandleInfo = nullptr;
    if (gc.pTarget != NULL)
    {
        SyncBlock *pSyncBlock = gc.pTarget->PassiveGetSyncBlock();
        if (pSyncBlock != nullptr && pSyncBlock->GetInteropInfoNoCreate() != nullptr)
        {
            comWeakHandleInfo = GetComWeakReferenceInfo(&gc.pTarget);
        }
    }

    if (comWeakHandleInfo != nullptr)
    {
        NewHolder<NativeComWeakHandleInfo> infoHolder(comWeakHandleInfo);
        gc.pThis->m_Handle = SetNativeComWeakReferenceHandle(GetAppDomain()->CreateNativeComWeakHandle(gc.pTarget, infoHolder));
        infoHolder.SuppressRelease();
    }
    else
#endif // FEATURE_COMINTEROP || FEATURE_COMWRAPPERS
    {
        gc.pThis->m_Handle = GetAppDomain()->CreateTypedHandle(gc.pTarget,
            trackResurrection ? HNDTYPE_WEAK_LONG : HNDTYPE_WEAK_SHORT);
    }

    HELPER_METHOD_FRAME_END();
}
FCIMPLEND

FCIMPL3(void, WeakReferenceOfTNative::Create, WeakReferenceObject * pThisUNSAFE, Object * pTargetUNSAFE, CLR_BOOL trackResurrection)
{
    FCALL_CONTRACT;

    struct
    {
        WEAKREFERENCEREF pThis;
        OBJECTREF pTarget;
    } gc;

    gc.pThis = WEAKREFERENCEREF(pThisUNSAFE);
    gc.pTarget = OBJECTREF(pTargetUNSAFE);

    HELPER_METHOD_FRAME_BEGIN_PROTECT(gc);

    if (gc.pThis == NULL)
        COMPlusThrow(kNullReferenceException);

    if (pWeakReferenceOfTCanonMT == NULL)
        pWeakReferenceOfTCanonMT = gc.pThis->GetMethodTable()->GetCanonicalMethodTable();

    _ASSERTE(gc.pThis->GetMethodTable()->GetCanonicalMethodTable() == pWeakReferenceOfTCanonMT);

#if defined(FEATURE_COMINTEROP) || defined(FEATURE_COMWRAPPERS)
    NativeComWeakHandleInfo *comWeakHandleInfo = nullptr;
    if (gc.pTarget != NULL)
    {
        SyncBlock *pSyncBlock = gc.pTarget->PassiveGetSyncBlock();
        if (pSyncBlock != nullptr && pSyncBlock->GetInteropInfoNoCreate() != nullptr)
        {
            comWeakHandleInfo = GetComWeakReferenceInfo(&gc.pTarget);
        }
    }

    if (comWeakHandleInfo != nullptr)
    {
        NewHolder<NativeComWeakHandleInfo> infoHolder(comWeakHandleInfo);
        gc.pThis->m_Handle = SetNativeComWeakReferenceHandle(GetAppDomain()->CreateNativeComWeakHandle(gc.pTarget, infoHolder));
        infoHolder.SuppressRelease();
    }
    else
#endif // FEATURE_COMINTEROP || FEATURE_COMWRAPPERS
    {
        gc.pThis->m_Handle = GetAppDomain()->CreateTypedHandle(gc.pTarget,
            trackResurrection ? HNDTYPE_WEAK_LONG : HNDTYPE_WEAK_SHORT);
    }

    HELPER_METHOD_FRAME_END();
}
FCIMPLEND

//************************************************************************

// This entrypoint is also used for direct finalization by the GC. Note that we cannot depend on the runtime being suspended
// when this is called because of background GC. Background GC is going to call this method while user managed code is running.
void FinalizeWeakReference(Object * obj)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    WEAKREFERENCEREF pThis((WeakReferenceObject *)(obj));

    // The suspension state of the runtime must be prevented from changing while in this function in order for this to be safe.
    OBJECTHANDLE handle = ThreadSuspend::SysIsSuspended() ? pThis->m_Handle.LoadWithoutBarrier() : AcquireWeakHandleSpinLock(pThis);
    OBJECTHANDLE handleToDestroy = NULL;
    bool isWeakNativeComHandle = false;

    // Check for not yet constructed or already finalized handle
    if ((handle != NULL) && !IS_SPECIAL_HANDLE(handle))
    {
        handleToDestroy = GetHandleValue(handle);

        // Cache the old handle value
        HandleType handleType = GCHandleUtilities::GetGCHandleManager()->HandleFetchType(handleToDestroy);
#if defined(FEATURE_COMINTEROP) || defined(FEATURE_COMWRAPPERS)
        _ASSERTE(handleType == HNDTYPE_WEAK_LONG || handleType == HNDTYPE_WEAK_SHORT || handleType == HNDTYPE_WEAK_NATIVE_COM);
        isWeakNativeComHandle = handleType == HNDTYPE_WEAK_NATIVE_COM;
#else // !FEATURE_COMINTEROP && !FEATURE_COMWRAPPERS
        _ASSERTE(handleType == HNDTYPE_WEAK_LONG || handleType == HNDTYPE_WEAK_SHORT);
#endif // FEATURE_COMINTEROP || FEATURE_COMWRAPPERS

        handle = (handleType == HNDTYPE_WEAK_LONG) ?
            SPECIAL_HANDLE_FINALIZED_LONG : SPECIAL_HANDLE_FINALIZED_SHORT;
    }

    // Release the spin lock
    // This is necessary even when the spin lock is not acquired
    // (i.e. When ThreadSuspend::SysIsSuspended() == true)
    // so that the new handle value is set.
    ReleaseWeakHandleSpinLock(pThis, handle);

    if (handleToDestroy != NULL)
    {
#if defined(FEATURE_COMINTEROP) || defined(FEATURE_COMWRAPPERS)
        if (isWeakNativeComHandle)
        {
            DestroyNativeComWeakHandle(handleToDestroy);
        }
        else
#endif // FEATURE_COMINTEROP || FEATURE_COMWRAPPERS
        {
            DestroyTypedHandle(handleToDestroy);
        }
    }
}

FCIMPL1(void, WeakReferenceNative::Finalize, WeakReferenceObject * pThis)
{
    FCALL_CONTRACT;

    HELPER_METHOD_FRAME_BEGIN_NOPOLL();

    if (pThis == NULL)
    {
        FCUnique(0x1);
        COMPlusThrow(kNullReferenceException);
    }

    FinalizeWeakReference(pThis);

    HELPER_METHOD_FRAME_END_POLL();
}
FCIMPLEND

FCIMPL1(void, WeakReferenceOfTNative::Finalize, WeakReferenceObject * pThis)
{
    FCALL_CONTRACT;

    HELPER_METHOD_FRAME_BEGIN_NOPOLL();

    if (pThis == NULL)
        COMPlusThrow(kNullReferenceException);

    FinalizeWeakReference(pThis);

    HELPER_METHOD_FRAME_END_POLL();
}
FCIMPLEND

//************************************************************************

#include <optsmallperfcritical.h>

static FORCEINLINE OBJECTREF GetWeakReferenceTarget(WEAKREFERENCEREF pThis)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_COOPERATIVE;
    }
    CONTRACTL_END;

    OBJECTHANDLE rawHandle = pThis->m_Handle.LoadWithoutBarrier();
    OBJECTHANDLE handle = GetHandleValue(rawHandle);

    if (handle == NULL)
        return NULL;

    // Try a speculative lock-free read first
    if (rawHandle != SPECIAL_HANDLE_SPINLOCK)
    {
        //
        // There is a theoretic chance that the speculative lock-free read may AV while reading the value
        // of freed handle if the handle table decides to release the memory that the handle lives in.
        // It is not exploitable security issue because of we will fail fast on the AV. It is denial of service only.
        // Non-malicious user code will never hit.
        //
        // We had this theoretical bug in there since forever. Fixing it by always taking the lock would
        // degrade the performance critical weak handle getter several times. The right fix may be
        // to ensure that handle table memory is released only if the runtime is suspended.
        //
        Object * pSpeculativeTarget = VolatileLoad((Object **)(handle));

        //
        // We want to ensure that the handle was still alive when we fetched the target,
        // so we double check m_handle here. Note that the reading of the handle
        // value has to take memory barrier for this to work, but reading of m_handle does not.
        //
        if (rawHandle == pThis->m_Handle.LoadWithoutBarrier())
        {
            return OBJECTREF(pSpeculativeTarget);
        }
    }


    rawHandle = AcquireWeakHandleSpinLock(pThis);
    GCX_NOTRIGGER();

    handle = GetHandleValue(rawHandle);
    OBJECTREF pTarget = OBJECTREF(*(Object **)(handle));

    ReleaseWeakHandleSpinLock(pThis, rawHandle);

    return pTarget;
}

FCIMPL1(Object *, WeakReferenceNative::GetTarget, WeakReferenceObject * pThisUNSAFE)
{
    FCALL_CONTRACT;

    WEAKREFERENCEREF pThis(pThisUNSAFE);
    if (pThis == NULL)
    {
        FCUnique(0x1);
        FCThrow(kNullReferenceException);
    }

    OBJECTREF pTarget = GetWeakReferenceTarget(pThis);

#if defined(FEATURE_COMINTEROP) || defined(FEATURE_COMWRAPPERS)
    // If we found an object, or we're not a native COM weak reference, then we're done.  Othewrise
    // we can try to create a new RCW to the underlying native COM object if it's still alive.
    if (pTarget != NULL || !IsNativeComWeakReferenceHandle(pThis->m_Handle))
    {
        FC_GC_POLL_AND_RETURN_OBJREF(pTarget);
    }

    FC_INNER_RETURN(Object*, LoadComWeakReferenceTarget(pThis, g_pObjectClass, GetEEFuncEntryPointMacro(WeakReferenceNative::GetTarget)));
#else // !FEATURE_COMINTEROP && !FEATURE_COMWRAPPERS
    FC_GC_POLL_AND_RETURN_OBJREF(pTarget);
#endif // FEATURE_COMINTEROP || FEATURE_COMWRAPPERS
}
FCIMPLEND

FCIMPL1(Object *, WeakReferenceOfTNative::GetTarget, WeakReferenceObject * pThisUNSAFE)
{
    FCALL_CONTRACT;

    WEAKREFERENCEREF pThis(pThisUNSAFE);
    if (pThis == NULL)
    {
        FCThrow(kNullReferenceException);
    }

    OBJECTREF pTarget = GetWeakReferenceTarget(pThis);


#if defined(FEATURE_COMINTEROP) || defined(FEATURE_COMWRAPPERS)
    // If we found an object, or we're not a native COM weak reference, then we're done.  Othewrise
    // we can try to create a new RCW to the underlying native COM object if it's still alive.
    if (pTarget != NULL || !IsNativeComWeakReferenceHandle(pThis->m_Handle))
    {
        FC_GC_POLL_AND_RETURN_OBJREF(pTarget);
    }

    FC_INNER_RETURN(Object*, LoadComWeakReferenceTarget(pThis, pThis->GetMethodTable()->GetInstantiation()[0], GetEEFuncEntryPointMacro(WeakReferenceOfTNative::GetTarget)));
#else // !FEATURE_COMINTEROP && !FEATURE_COMWRAPPERS
    FC_GC_POLL_AND_RETURN_OBJREF(pTarget);
#endif // FEATURE_COMINTEROP || FEATURE_COMWRAPPERS
}
FCIMPLEND

FCIMPL1(FC_BOOL_RET, WeakReferenceNative::IsAlive, WeakReferenceObject * pThisUNSAFE)
{
    FCALL_CONTRACT;

    WEAKREFERENCEREF pThis(pThisUNSAFE);
    if (pThis == NULL)
    {
        FCThrow(kNullReferenceException);
    }

    BOOL fRet = GetWeakReferenceTarget(pThis) != NULL;

    FC_GC_POLL_RET();

    FC_RETURN_BOOL(fRet);
}
FCIMPLEND

#include <optdefault.h>

//************************************************************************

#include <optsmallperfcritical.h>

// Slow path helper for setting the target of a weak reference.  This code is used if a native COM weak reference might
// be required.
NOINLINE void SetWeakReferenceTarget(WEAKREFERENCEREF weakReference, OBJECTREF target, LPVOID __me)
{
    FCALL_CONTRACT;

    FC_INNER_PROLOG_NO_ME_SETUP();
    HELPER_METHOD_FRAME_BEGIN_ATTRIB_2(Frame::FRAME_ATTR_EXACT_DEPTH|Frame::FRAME_ATTR_CAPTURE_DEPTH_2, target, weakReference);

#if defined(FEATURE_COMINTEROP) || defined(FEATURE_COMWRAPPERS)
    NewHolder<NativeComWeakHandleInfo> comWeakHandleInfo(GetComWeakReferenceInfo(&target));
#endif // FEATURE_COMINTEROP || FEATURE_COMWRAPPERS

    WeakHandleSpinLockHolder handle(AcquireWeakHandleSpinLock(weakReference), &weakReference);
    GCX_NOTRIGGER();

#if defined(FEATURE_COMINTEROP) || defined(FEATURE_COMWRAPPERS)
    //
    // We have four combinations to handle here
    //
    // Existing target is a GC object, new target is a GC object:
    //  * Just store the new object in the handle
    //
    // Existing target is native COM weak reference, new target is native COM weak reference:
    //   * Release the existing IWeakReference*
    //   * Store the new IWeakReference*
    //   * Store the new object in the handle
    //
    // Existing target is native COM weak reference, new target is GC:
    //   * Release the existing IWeakReference*
    //   * Store null to the IWeakReference* field
    //   * Store the new object in the handle
    //
    // Existing target is GC, new target is native COM weak reference:
    //   * Destroy the existing handle
    //   * Allocate a new native COM weak handle for the new target
    //

    if (IsNativeComWeakReferenceHandle(handle.RawHandle))
    {
        // If the existing reference is a native COM weak reference, we need to delete its native COM info
        // and update it with the new native COM info. If the incoming object is not an RCW that can use
        // IWeakReference, then comWeakHandleInfo will be null. Therefore, no matter what the incoming
        // object type is, we can unconditionally store comWeakHandleInfo to the object handle's extra data.
        IGCHandleManager *mgr = GCHandleUtilities::GetGCHandleManager();
        NativeComWeakHandleInfo* existingInfo = reinterpret_cast<NativeComWeakHandleInfo*>(mgr->GetExtraInfoFromHandle(handle.Handle));
        mgr->SetExtraInfoForHandle(handle.Handle, HNDTYPE_WEAK_NATIVE_COM, reinterpret_cast<void*>(comWeakHandleInfo.GetValue()));
        StoreObjectInHandle(handle.Handle, target);

        if (existingInfo != nullptr)
        {
            _ASSERTE(existingInfo->WeakReference != nullptr);
            existingInfo->WeakReference->Release();
            delete existingInfo;
        }
    }
    else if (comWeakHandleInfo != nullptr)
    {
        // The existing handle is not a native COM weak reference, but we need to store the new object in
        // a native COM weak reference.  Therefore we need to destroy the old handle and create a new native COM
        // handle.  The new handle needs to be allocated first to prevent the weak reference from holding
        // a destroyed handle if we fail to allocate the new one.
        _ASSERTE(!IsNativeComWeakReferenceHandle(handle.RawHandle));
        OBJECTHANDLE previousHandle = handle.RawHandle;

        handle.Handle = GetAppDomain()->CreateNativeComWeakHandle(target, comWeakHandleInfo);
        handle.RawHandle = SetNativeComWeakReferenceHandle(handle.Handle);

        DestroyTypedHandle(previousHandle);
    }
    else
#endif // FEATURE_COMINTEROP || FEATURE_COMWRAPPERS
    {
        StoreObjectInHandle(handle.Handle, target);
    }

#if defined(FEATURE_COMINTEROP) || defined(FEATURE_COMWRAPPERS)
    comWeakHandleInfo.SuppressRelease();
#endif // FEATURE_COMINTEROP || FEATURE_COMWRAPPERS

    HELPER_METHOD_FRAME_END();
    FC_INNER_EPILOG();
}

FCIMPL2(void, WeakReferenceNative::SetTarget, WeakReferenceObject * pThisUNSAFE, Object * pTargetUNSAFE)
{
    FCALL_CONTRACT;

    WEAKREFERENCEREF pThis(pThisUNSAFE);
    OBJECTREF pTarget(pTargetUNSAFE);

    if (pThis == NULL)
    {
        FCUnique(0x1);
        FCThrowVoid(kNullReferenceException);
    }

    bool storedObject = false;

    OBJECTHANDLE handle = AcquireWeakHandleSpinLock(pThis);
    {
        if (handle == NULL || IS_SPECIAL_HANDLE(handle))
        {
            ReleaseWeakHandleSpinLock(pThis, handle);
            FCThrowResVoid(kInvalidOperationException, W("InvalidOperation_HandleIsNotInitialized"));
        }

        // Switch to no-trigger after the handle was validate. FCThrow triggers.
        GCX_NOTRIGGER();

        // If the existing handle is a GC weak handle and the new target is not an RCW, then
        // we can avoid setting up a helper method frame and just reset the handle directly.
        if (!IsNativeComWeakReferenceHandle(handle))
        {
            if (pTarget == NULL || !pTarget->GetMethodTable()->IsComObjectType())
            {
                StoreObjectInHandle(handle, pTarget);
                storedObject = true;
            }
        }

        // SetWeakReferenceTarget will reacquire the spinlock after setting up a helper method frame.  This allows
        // the frame setup to throw without worrying about leaking the spinlock, and allows the epilog to be cleanly
        // walked by the epilog decoder.
        ReleaseWeakHandleSpinLock(pThis, handle);
    }

    // If we reset the handle directly, then early out before setting up a helper method frame
    if (storedObject)
    {
        FC_GC_POLL();
        return;
    }

    FC_INNER_RETURN_VOID(SetWeakReferenceTarget(pThis, pTarget, GetEEFuncEntryPointMacro(WeakReferenceNative::SetTarget)));
}
FCIMPLEND

FCIMPL2(void, WeakReferenceOfTNative::SetTarget, WeakReferenceObject * pThisUNSAFE, Object * pTargetUNSAFE)
{
    FCALL_CONTRACT;

    WEAKREFERENCEREF pThis(pThisUNSAFE);
    OBJECTREF pTarget(pTargetUNSAFE);

    if (pThis == NULL)
    {
        FCThrowVoid(kNullReferenceException);
    }

    bool storedObject = false;

    OBJECTHANDLE handle = AcquireWeakHandleSpinLock(pThis);
    {
        if (handle == NULL || IS_SPECIAL_HANDLE(handle))
        {
            ReleaseWeakHandleSpinLock(pThis, handle);
            FCThrowResVoid(kInvalidOperationException, W("InvalidOperation_HandleIsNotInitialized"));
        }

        // Switch to no-trigger after the handle was validate. FCThrow triggers.
        GCX_NOTRIGGER();

        // If the existing handle is a GC weak handle and the new target is not an RCW, then
        // we can avoid setting up a helper method frame and just reset the handle directly.
        if (!IsNativeComWeakReferenceHandle(handle))
        {
            if (pTarget == NULL || !pTarget->GetMethodTable()->IsComObjectType())
            {
                StoreObjectInHandle(handle, pTarget);
                storedObject = true;
            }
        }

        // SetWeakReferenceTarget will reacquire the spinlock after setting up a helper method frame.  This allows
        // the frame setup to throw without worrying about leaking the spinlock, and allows the epilog to be cleanly
        // walked by the epilog decoder.
        ReleaseWeakHandleSpinLock(pThis, handle);
    }

    // If we reset the handle directly, then early out before setting up a helper method frame
    if (storedObject)
    {
        FC_GC_POLL();
        return;
    }

    FC_INNER_RETURN_VOID(SetWeakReferenceTarget(pThis, pTarget, GetEEFuncEntryPointMacro(WeakReferenceOfTNative::SetTarget)));
}
FCIMPLEND

#include <optdefault.h>

//************************************************************************

FCIMPL1(FC_BOOL_RET, WeakReferenceNative::IsTrackResurrection, WeakReferenceObject * pThisUNSAFE)
{
    FCALL_CONTRACT;

    WEAKREFERENCEREF pThis(pThisUNSAFE);

    if (pThis == NULL)
    {
        FCUnique(0x1);
        FCThrow(kNullReferenceException);
    }

    BOOL trackResurrection = FALSE;
    OBJECTHANDLE handle = AcquireWeakHandleSpinLock(pThis);
    {
        GCX_NOTRIGGER();

        if (handle == NULL)
        {
            trackResurrection = FALSE;
        }
        else
        if (IS_SPECIAL_HANDLE(handle))
        {
            trackResurrection = (handle == SPECIAL_HANDLE_FINALIZED_LONG);
        }
        else
        {
            trackResurrection = GCHandleUtilities::GetGCHandleManager()->HandleFetchType(GetHandleValue(handle)) == HNDTYPE_WEAK_LONG;
        }

        ReleaseWeakHandleSpinLock(pThis, handle);
    }

    FC_GC_POLL_RET();
    FC_RETURN_BOOL(trackResurrection);
}
FCIMPLEND

FCIMPL1(FC_BOOL_RET, WeakReferenceOfTNative::IsTrackResurrection, WeakReferenceObject * pThisUNSAFE)
{
    FCALL_CONTRACT;

    WEAKREFERENCEREF pThis(pThisUNSAFE);

    if (pThis == NULL)
    {
        FCThrow(kNullReferenceException);
    }

    BOOL trackResurrection = FALSE;
    OBJECTHANDLE handle = AcquireWeakHandleSpinLock(pThis);
    {
        GCX_NOTRIGGER();

        if (handle == NULL)
        {
            trackResurrection = FALSE;
        }
        else
        if (IS_SPECIAL_HANDLE(handle))
        {
            trackResurrection = (handle == SPECIAL_HANDLE_FINALIZED_LONG);
        }
        else
        {
            trackResurrection = GCHandleUtilities::GetGCHandleManager()->HandleFetchType(GetHandleValue(handle)) == HNDTYPE_WEAK_LONG;
        }

        ReleaseWeakHandleSpinLock(pThis, handle);
    }

    FC_GC_POLL_RET();
    FC_RETURN_BOOL(trackResurrection);
}
FCIMPLEND

//************************************************************************
