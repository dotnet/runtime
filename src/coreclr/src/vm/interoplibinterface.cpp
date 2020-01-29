// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "common.h"

// Interop library header
#include <interoplibimports.h>

#include "interoplibinterface.h"

// void ____()
// {
//     CONTRACTL
//     {
//         THROWS;
//         //NOTHROW;
//         MODE_COOPERATIVE;
//         // MODE_PREEMPTIVE;
//         // MODE_ANY;
//         // PRECONDITION(pSrc != NULL);
//     }
//     CONTRACTL_END;
// }

namespace
{
    const HandleType InstanceHandleType{ HNDTYPE_STRONG };
    const HandleType ComWrappersImplHandleType{ HNDTYPE_STRONG };

    void* CallComputeVTables(
        _In_ OBJECTREF impl,
        _In_ OBJECTREF instance,
        _In_ INT32 flags,
        _Out_ DWORD* vtableCount)
    {
        CONTRACTL
        {
            THROWS;
            GC_TRIGGERS;
            MODE_COOPERATIVE;
            PRECONDITION(impl != NULL);
            PRECONDITION(instance != NULL);
            PRECONDITION(vtableCount != NULL);
        }
        CONTRACTL_END;

        void* vtables = NULL;

        struct
        {
            OBJECTREF implRef;
            OBJECTREF instRef;
        } gc;
        ::ZeroMemory(&gc, sizeof(gc));
        GCPROTECT_BEGIN(gc);

        gc.implRef = impl;
        gc.instRef = instance;

        PREPARE_NONVIRTUAL_CALLSITE(METHOD__COMWRAPPERS__COMPUTE_VTABLES);
        DECLARE_ARGHOLDER_ARRAY(args, 4);
        args[ARGNUM_0]  = OBJECTREF_TO_ARGHOLDER(gc.implRef);
        args[ARGNUM_1]  = OBJECTREF_TO_ARGHOLDER(gc.instRef);
        args[ARGNUM_2]  = DWORD_TO_ARGHOLDER(flags);
        args[ARGNUM_3]  = PTR_TO_ARGHOLDER(vtableCount);
        CALL_MANAGED_METHOD(vtables, void*, args);

        GCPROTECT_END();

        return vtables;
    }

    OBJECTREF CallGetObject(
        _In_ OBJECTREF impl,
        _In_ IUnknown* externalComObject,
        _In_ INT32 flags)
    {
        CONTRACTL
        {
            THROWS;
            GC_TRIGGERS;
            MODE_COOPERATIVE;
            PRECONDITION(impl != NULL);
            PRECONDITION(externalComObject != NULL);
        }
        CONTRACTL_END;

        OBJECTREF retObjRef;

        struct
        {
            OBJECTREF implRef;
        } gc;
        ::ZeroMemory(&gc, sizeof(gc));
        GCPROTECT_BEGIN(gc);

        gc.implRef = impl;

        PREPARE_NONVIRTUAL_CALLSITE(METHOD__COMWRAPPERS__CREATE_OBJECT);
        DECLARE_ARGHOLDER_ARRAY(args, 3);
        args[ARGNUM_0]  = OBJECTREF_TO_ARGHOLDER(gc.implRef);
        args[ARGNUM_1]  = PTR_TO_ARGHOLDER(externalComObject);
        args[ARGNUM_2]  = DWORD_TO_ARGHOLDER(flags);
        CALL_MANAGED_METHOD(retObjRef, OBJECTREF, args);

        GCPROTECT_END();

        return retObjRef;
    }

    // This class is used to track the external object within the runtime.
    struct ExternalObjectContext
    {
        void* Identity;
        DWORD SyncBlockIndex;
    };

    class ExtObjCxtCache
    {
        static ExtObjCxtCache* g_Instance;

    public: // static
        static ExtObjCxtCache* GetInstance()
        {
            // [TODO] Properly allocate the cache
            if (g_Instance == nullptr)
                g_Instance = new ExtObjCxtCache();

            return g_Instance;
        }

    public: // Inner class definitions
        class Traits : public DefaultSHashTraits<ExternalObjectContext *>
        {
        public:
            using key_t = void*;
            static const key_t GetKey(_In_ element_t e) { LIMITED_METHOD_CONTRACT; return (key_t)e->Identity; }
            static count_t Hash(_In_ key_t key) { LIMITED_METHOD_CONTRACT; return (count_t)key; }
            static bool Equals(_In_ key_t lhs, _In_ key_t rhs) { LIMITED_METHOD_CONTRACT; return (lhs == rhs); }
        };

        // Alias some useful types
        using Element = SHash<Traits>::element_t;
        using Iterator = SHash<Traits>::Iterator;

        class LockHolder : public CrstHolder
        {
        public:
            LockHolder(_In_ ExtObjCxtCache *cache)
                : CrstHolder(&cache->_lock)
            {
                // This cache must be locked in Cooperative mode
                // since releases of wrappers can occur during a GC.
                CONTRACTL
                {
                    NOTHROW;
                    GC_NOTRIGGER;
                    MODE_COOPERATIVE;
                }
                CONTRACTL_END;
            }
        };

    private:
        friend class InteropLibImports::ExtObjCxtIterator;
        SHash<Traits> _hashMap;
        Crst _lock;

        ExtObjCxtCache()
            : _lock(CrstExternalObjectContextCache, CRST_UNSAFE_COOPGC)
        { }
        ~ExtObjCxtCache() = default;

    public:
        bool IsLockHeld()
        {
            WRAPPER_NO_CONTRACT;
            return (_lock.OwnedByCurrentThread() != FALSE);
        }

        ExternalObjectContext* Find(_In_ IUnknown* instance)
        {
            CONTRACT(ExternalObjectContext*)
            {
                NOTHROW;
                GC_NOTRIGGER;
                MODE_COOPERATIVE;
                PRECONDITION(IsLockHeld());
                PRECONDITION(instance != NULL);
                POSTCONDITION(CheckPointer(RETVAL, NULL_OK));
            }
            CONTRACT_END;

            // Forbid the GC from messing with the hash table.
            GCX_FORBID();

            RETURN _hashMap.Lookup(instance);
        }

        ExternalObjectContext* Add(_In_ ExternalObjectContext* cxt)
        {
            CONTRACT(ExternalObjectContext*)
            {
                THROWS;
                GC_NOTRIGGER;
                MODE_COOPERATIVE;
                PRECONDITION(IsLockHeld());
                PRECONDITION(!Traits::IsNull(cxt));
                PRECONDITION(!Traits::IsDeleted(cxt));
                PRECONDITION(cxt->Identity != NULL);
                PRECONDITION(Find(static_cast<IUnknown*>(cxt->Identity)) == NULL);
                POSTCONDITION(RETVAL == cxt);
            }
            CONTRACT_END;

            _hashMap.Add(cxt);
            RETURN cxt;
        }

        ExternalObjectContext* FindOrAdd(_In_ IUnknown* key, _In_ ExternalObjectContext* newCxt)
        {
            CONTRACT(ExternalObjectContext*)
            {
                THROWS;
                GC_NOTRIGGER;
                MODE_COOPERATIVE;
                PRECONDITION(IsLockHeld());
                PRECONDITION(key != NULL);
                PRECONDITION(!Traits::IsNull(newCxt));
                PRECONDITION(!Traits::IsDeleted(newCxt));
                PRECONDITION(key == newCxt->Identity);
                POSTCONDITION(CheckPointer(RETVAL));
            }
            CONTRACT_END;

            // Forbid the GC from messing with the hash table.
            GCX_FORBID();

            ExternalObjectContext* cxt = Find(key);
            if (Traits::IsNull(cxt))
                cxt = Add(newCxt);

            RETURN cxt;
        }

        void Remove(_In_ ExternalObjectContext* cxt)
        {
            CONTRACTL
            {
                NOTHROW;
                GC_NOTRIGGER;
                MODE_ANY;
                PRECONDITION(!Traits::IsNull(cxt));
                PRECONDITION(!Traits::IsDeleted(cxt));
                PRECONDITION(cxt->Identity != NULL);

                // The GC thread doesn't have to take the lock
                // since all other threads access in cooperative mode
                PRECONDITION(
                    (IsLockHeld() && GetThread()->PreemptiveGCDisabled())
                    || Debug_IsLockedViaThreadSuspension());
            }
            CONTRACTL_END;

            _hashMap.Remove(cxt->Identity);
        }
    };

    // Global instance
    ExtObjCxtCache* ExtObjCxtCache::g_Instance;

    // Wrapper for External Object Contexts
    struct ExtObjCxtHolder
    {
        void* _cxt;
        ExtObjCxtHolder()
            : _cxt(nullptr)
        { }
        ~ExtObjCxtHolder()
        {
            if (_cxt != nullptr)
                InteropLib::Com::DestroyWrapperForExternal(_cxt);
        }
        ExternalObjectContext* operator->()
        {
            return (ExternalObjectContext*)_cxt;
        }
        void** operator&()
        {
            return &_cxt;
        }
        operator ExternalObjectContext*()
        {
            return (ExternalObjectContext*)_cxt;
        }
        void* Detach()
        {
            void* t = _cxt;
            _cxt = nullptr;
            return t;
        }
    };
}

namespace InteropLibImports
{
    void* MemAlloc(_In_ size_t sizeInBytes, _In_ AllocScenario scenario) noexcept
    {
        CONTRACTL
        {
            NOTHROW;
            MODE_ANY;
            PRECONDITION(sizeInBytes != 0);
        }
        CONTRACTL_END;

        return ::malloc(sizeInBytes);
    }

    void MemFree(_In_ void* mem, _In_ AllocScenario scenario) noexcept
    {
        CONTRACTL
        {
            NOTHROW;
            MODE_ANY;
            PRECONDITION(mem != NULL);
        }
        CONTRACTL_END;

        ::free(mem);
    }

    void DeleteObjectInstanceHandle(_In_ InteropLib::OBJECTHANDLE handle) noexcept
    {
        CONTRACTL
        {
            NOTHROW;
            MODE_PREEMPTIVE;
            PRECONDITION(handle != NULL);
        }
        CONTRACTL_END;

        ::OBJECTHANDLE objectHandle = static_cast<::OBJECTHANDLE>(handle);
        DestroyHandleCommon(objectHandle, InstanceHandleType);
    }

    HRESULT GetOrCreateTrackerTargetForExternal(
        _In_ InteropLib::OBJECTHANDLE impl,
        _In_ IUnknown* externalComObject,
        _In_ INT32 externalObjectFlags,
        _In_ INT32 trackerTargetFlags,
        _Outptr_ IUnknown** trackerTarget) noexcept
    {
        CONTRACTL
        {
            NOTHROW;
            MODE_PREEMPTIVE;
            PRECONDITION(impl != NULL);
            PRECONDITION(externalComObject != NULL);
            PRECONDITION(trackerTarget != NULL);
        }
        CONTRACTL_END;

        ::OBJECTHANDLE implHandle = static_cast<::OBJECTHANDLE>(impl);

        {
            GCX_COOP();

            struct
            {
                OBJECTREF implRef;
                OBJECTREF newObjRef;
            } gc;
            ::ZeroMemory(&gc, sizeof(gc));
            GCPROTECT_BEGIN(gc);

            gc.implRef = ObjectFromHandle(implHandle);
            _ASSERTE(gc.implRef != NULL);

            //
            // Get wrapper for external object
            //

            //
            // Get wrapper for managed object
            //

            GCPROTECT_END();
        }

        return S_OK;
    }

    class ExtObjCxtIterator
    {
    public:
        ExtObjCxtIterator(_In_ ExtObjCxtCache* cache)
            : Curr{ cache->_hashMap.Begin() }
            , End{ cache->_hashMap.End() }
        { }

        ExtObjCxtCache::Iterator Curr;
        ExtObjCxtCache::Iterator End;
    };

    HRESULT IteratorNext(_In_ ExtObjCxtIterator* iter, _Outptr_result_maybenull_ void** context) noexcept
    {
        CONTRACTL
        {
            NOTHROW;
            GC_NOTRIGGER;
            MODE_COOPERATIVE;
            PRECONDITION(iter != NULL);
            PRECONDITION(context != NULL);

            // Should only be called during a GC suspension
            PRECONDITION(Debug_IsLockedViaThreadSuspension());
        }
        CONTRACTL_END;

        if (iter->Curr == iter->End)
        {
            *context = NULL;
            return S_FALSE;
        }

        ExtObjCxtCache::Element e = *iter->Curr++;
        *context = e;
        return S_OK;
    }
}

#ifdef FEATURE_COMINTEROP

void* QCALLTYPE ComWrappersNative::GetOrCreateComInterfaceForObject(
    _In_ QCall::ObjectHandleOnStack comWrappersImpl,
    _In_ QCall::ObjectHandleOnStack instance,
    _In_ INT32 flags)
{
    QCALL_CONTRACT;

    HRESULT hr;

    SafeComHolder<IUnknown> newWrapper;
    void* wrapper = NULL;

    BEGIN_QCALL;

    // Switch to COOP mode to check if the object already
    // has a wrapper in its syncblock.
    {
        GCX_COOP();

        struct
        {
            OBJECTREF implRef;
            OBJECTREF instRef;
        } gc;
        ::ZeroMemory(&gc, sizeof(gc));
        GCPROTECT_BEGIN(gc);

        gc.instRef = ObjectToOBJECTREF(*instance.m_ppObject);
        _ASSERTE(gc.instRef != NULL);

        // Check the object's SyncBlock for a managed object wrapper.
        SyncBlock* syncBlock = gc.instRef->GetSyncBlock();
        InteropSyncBlockInfo* interopInfo = syncBlock->GetInteropInfo();

        // Query the associated InteropSyncBlockInfo for an existing managed object wrapper.
        if (!interopInfo->TryGetManagedObjectComWrapper(&wrapper))
        {
            // Get the supplied COM Wrappers implementation to request VTable computation.
            gc.implRef = ObjectToOBJECTREF(*comWrappersImpl.m_ppObject);
            _ASSERTE(gc.implRef != NULL);

            // Compute VTables for the new existing COM object
            //
            // N.B. Calling to compute the associated VTables is perhaps early since no lock
            // is taken. However, a key assumption here is that the returned memory will be
            // idempotent for the same object.
            DWORD vtableCount;
            void* vtables = CallComputeVTables(gc.implRef, gc.instRef, flags, &vtableCount);

            // Re-query the associated InteropSyncBlockInfo for an existing managed object wrapper.
            if (!interopInfo->TryGetManagedObjectComWrapper(&wrapper))
            {
                OBJECTHANDLE instHandle = GetAppDomain()->CreateTypedHandle(gc.instRef, InstanceHandleType);

                // Call the InteropLib and create the associated managed object wrapper.
                hr = InteropLib::Com::CreateWrapperForObject(instHandle, vtableCount, vtables, flags, &newWrapper);
                if (FAILED(hr))
                {
                    DestroyHandleCommon(instHandle, InstanceHandleType);
                    COMPlusThrowHR(hr);
                }
                _ASSERTE(!newWrapper.IsNull());

                // Try setting the newly created managed object wrapper on the InteropSyncBlockInfo.
                if (!interopInfo->TrySetManagedObjectComWrapper(newWrapper))
                {
                    // The new wrapper couldn't be set which means a wrapper already exists.
                    newWrapper.Release();

                    // If the managed object wrapper couldn't be set, then
                    // it should be possible to get the current one.
                    if (!interopInfo->TryGetManagedObjectComWrapper(&wrapper))
                    {
                        UNREACHABLE();
                    }
                }
            }
        }

        // Determine what to return.
        if (!newWrapper.IsNull())
        {
            // A new managed object wrapper was created, remove the object from the holder.
            // No AddRef() here since the wrapper should be created with a reference.
            wrapper = newWrapper.Extract();
        }
        else
        {
            _ASSERTE(wrapper != NULL);
            // It is possible the supplied wrapper is no longer valid. If so, reactivate the
            // wrapper with the object instance's new handle. If this reactivation
            // wasn't needed, delete the handle.
            OBJECTHANDLE instHandle = GetAppDomain()->CreateTypedHandle(gc.instRef, InstanceHandleType);
            hr = InteropLib::Com::EnsureActiveWrapperAndAddRef(static_cast<IUnknown *>(wrapper), instHandle);
            if (hr != S_OK)
                DestroyHandleCommon(instHandle, InstanceHandleType);

            if (FAILED(hr))
                COMPlusThrowHR(hr);
        }

        GCPROTECT_END();
    }

    END_QCALL;

    _ASSERTE(wrapper != NULL);
    return wrapper;
}

void QCALLTYPE ComWrappersNative::GetOrCreateObjectForComInstance(
    _In_ QCall::ObjectHandleOnStack comWrappersImpl,
    _In_ void* ext,
    _In_ INT32 flags,
    _Inout_ QCall::ObjectHandleOnStack retValue)
{
    QCALL_CONTRACT;

    _ASSERTE(ext != NULL);

    BEGIN_QCALL;

    HRESULT hr;
    ExternalObjectContext* extObjCxt;
    IUnknown* externalComObject = reinterpret_cast<IUnknown*>(ext);

    // Determine the true identity of the object
    SafeComHolder<IUnknown> identity;
    hr = externalComObject->QueryInterface(IID_IUnknown, &identity);
    _ASSERTE(hr == S_OK);

    // Switch to COOP mode in order to check if the external object is already
    // known or a new one should be created.
    {
        GCX_COOP();

        struct
        {
            OBJECTREF implRef;
            OBJECTREF objRef;
        } gc;
        ::ZeroMemory(&gc, sizeof(gc));
        GCPROTECT_BEGIN(gc);

        ExtObjCxtCache* cache = ExtObjCxtCache::GetInstance();

        {
            // Query the external object cache
            ExtObjCxtCache::LockHolder lock(cache);
            extObjCxt = cache->Find(identity);
        }

        if (extObjCxt != NULL)
        {
            if (extObjCxt->SyncBlockIndex == 0)
            {
                // [TODO] We are in a bad spot?
            }

            gc.objRef = ObjectToOBJECTREF(g_pSyncTable[extObjCxt->SyncBlockIndex].m_Object);
        }
        else
        {
            ExtObjCxtHolder newContext;
            hr = InteropLib::Com::CreateWrapperForExternal(identity, flags, sizeof(ExternalObjectContext), &newContext);
            if (FAILED(hr))
                COMPlusThrow(hr);

            gc.implRef = ObjectToOBJECTREF(*comWrappersImpl.m_ppObject);
            _ASSERTE(gc.implRef != NULL);

            // Call the implementation to create an external object wrapper.
            gc.objRef = CallGetObject(gc.implRef, identity, flags);
            if (gc.objRef == NULL)
                COMPlusThrow(kArgumentNullException);

            newContext->Identity = (void*)identity;
            newContext->SyncBlockIndex = gc.objRef->GetSyncBlockIndex();

            {
                ExtObjCxtCache::LockHolder lock(cache);
                extObjCxt = cache->FindOrAdd(identity, newContext);
            }

            // Detach from the holder if the returned context matches the new context
            // since it means the new context was inserted.
            if (extObjCxt == newContext)
                (void)newContext.Detach();
        }

        // Set the return value
        retValue.Set(gc.objRef);

        GCPROTECT_END();
    }

    END_QCALL;
}

void QCALLTYPE ComWrappersNative::RegisterForReferenceTrackerHost(
    _In_ QCall::ObjectHandleOnStack comWrappersImpl)
{
    QCALL_CONTRACT;

    OBJECTHANDLE implHandle;

    BEGIN_QCALL;

    // Enter cooperative mode to create the handle and store it
    // for future use in the reference tracker host scenario.
    {
        GCX_COOP();

        OBJECTREF implRef = NULL;
        GCPROTECT_BEGIN(implRef);

        implRef = ObjectToOBJECTREF(*comWrappersImpl.m_ppObject);
        _ASSERTE(implRef != NULL);

        implHandle = GetAppDomain()->CreateTypedHandle(implRef, ComWrappersImplHandleType);

        if (!InteropLib::Com::RegisterReferenceTrackerHostCallback(implHandle))
        {
            DestroyHandleCommon(implHandle, ComWrappersImplHandleType);
            COMPlusThrow(kInvalidOperationException, IDS_EE_RESET_REFERENCETRACKERHOST_CALLBACKS);
        }

        GCPROTECT_END();
    }

    END_QCALL;
}

void QCALLTYPE ComWrappersNative::GetIUnknownImpl(
        _Out_ void** fpQueryInterface,
        _Out_ void** fpAddRef,
        _Out_ void** fpRelease)
{
    QCALL_CONTRACT;

    _ASSERTE(fpQueryInterface != NULL);
    _ASSERTE(fpAddRef != NULL);
    _ASSERTE(fpRelease != NULL);

    BEGIN_QCALL;

    InteropLib::Com::GetIUnknownImpl(fpQueryInterface, fpAddRef, fpRelease);

    END_QCALL;
}

void ComWrappersNative::DestroyManagedObjectComWrapper(_In_ void* wrapper)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        PRECONDITION(wrapper != NULL);
    }
    CONTRACTL_END;

    InteropLib::Com::DestroyWrapperForObject(wrapper);
}

#endif // FEATURE_COMINTEROP
