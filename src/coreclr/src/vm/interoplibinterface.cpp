// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Runtime headers
#include "common.h"
#include "rcwrefcache.h"
#include "rcwwalker.h"
#include "olecontexthelpers.h"
#include "finalizerthread.h"

// Interop library header
#include <interoplibimports.h>

#include "interoplibinterface.h"

namespace
{
    // This class is used to track the external object within the runtime.
    struct ExternalObjectContext
    {
        static const DWORD InvalidSyncBlockIndex;

        void* Identity;
        void* ThreadContext;
        DWORD SyncBlockIndex;

        enum
        {
            Flags_None = 0,
            Flags_Collected = 1,
            Flags_ReferenceTracker = 2,
        };
        DWORD Flags;

        static void Construct(
            _In_ ExternalObjectContext* cxt,
            _In_ IUnknown* identity,
            _In_opt_ void* threadContext,
            _In_ DWORD syncBlockIndex,
            _In_ DWORD flags)
        {
            CONTRACTL
            {
                NOTHROW;
                GC_NOTRIGGER;
                MODE_ANY;
                PRECONDITION(cxt != NULL);
                PRECONDITION(threadContext != NULL);
                PRECONDITION(syncBlockIndex != InvalidSyncBlockIndex);
            }
            CONTRACTL_END;

            cxt->Identity = (void*)identity;
            cxt->ThreadContext = threadContext;
            cxt->SyncBlockIndex = syncBlockIndex;
            cxt->Flags = flags;
        }

        bool IsSet(_In_ DWORD f) const
        {
            return ((Flags & f) == f);
        }

        bool IsActive() const
        {
            return IsSet(Flags_Collected)
                && (SyncBlockIndex != InvalidSyncBlockIndex);
        }

        void MarkCollected()
        {
            _ASSERTE(GCHeapUtilities::IsGCInProgress());
            SyncBlockIndex = InvalidSyncBlockIndex;
            Flags |= Flags_Collected;
        }

        OBJECTREF GetObjectRef()
        {
            CONTRACTL
            {
                NOTHROW;
                GC_NOTRIGGER;
                MODE_COOPERATIVE;
            }
            CONTRACTL_END;

            _ASSERTE(IsActive());
            return ObjectToOBJECTREF(g_pSyncTable[SyncBlockIndex].m_Object);
        }
    };

    const DWORD ExternalObjectContext::InvalidSyncBlockIndex = 0; // See syncblk.h

    static_assert((sizeof(ExternalObjectContext) % sizeof(void*)) == 0, "Keep context pointer size aligned");

    // Holder for a External Object Context
    struct ExtObjCxtHolder
    {
        ExternalObjectContext* _cxt;
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
            return _cxt;
        }
        ExternalObjectContext** operator&()
        {
            return &_cxt;
        }
        operator ExternalObjectContext*()
        {
            return _cxt;
        }
        ExternalObjectContext* Detach()
        {
            ExternalObjectContext* t = _cxt;
            _cxt = nullptr;
            return t;
        }
    };

    class ExtObjCxtCache
    {
        static Volatile<ExtObjCxtCache*> g_Instance;

    public: // static
        static ExtObjCxtCache* GetInstance()
        {
            if (g_Instance.Load() == NULL)
            {
                ExtObjCxtCache* instMaybe = new ExtObjCxtCache();

                // Attempt to set the global instance.
                if (NULL != FastInterlockCompareExchangePointer(&g_Instance, instMaybe, NULL))
                    delete instMaybe;
            }

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
        friend struct InteropLibImports::RuntimeCallContext;
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

        // Create a managed IEnumerable instance for this collection.
        // The collection should respect the supplied arguments.
        //        withFlags - If Flag_None, then ignore. Otherwise objects must have these flags.
        //    threadContext - The object must be associated with the supplied thread context.
        OBJECTREF CreateManagedEnumerable(_In_ DWORD withFlags, _In_opt_ void* threadContext)
        {
            CONTRACT(OBJECTREF)
            {
                THROWS;
                GC_TRIGGERS;
                MODE_COOPERATIVE;
                PRECONDITION(!IsLockHeld());
                POSTCONDITION(RETVAL != NULL);
            }
            CONTRACT_END;

            DWORD objCount;
            DWORD objCountMax;

            struct
            {
                PTRARRAYREF arrRef;
                PTRARRAYREF arrRefTmp;
            } gc;
            ::ZeroMemory(&gc, sizeof(gc));
            GCPROTECT_BEGIN(gc);

            {
                LockHolder lock(this);
                objCountMax = _hashMap.GetCount();
            }

            // Allocate the max number of objects needed.
            gc.arrRef = (PTRARRAYREF)AllocateObjectArray(objCountMax, g_pObjectClass);

            // Populate the array
            {
                LockHolder lock(this);
                Iterator curr = _hashMap.Begin();
                Iterator end = _hashMap.End();

                ExternalObjectContext* inst;
                for (objCount = 0; curr != end && objCount < objCountMax; objCount++, curr++)
                {
                    inst = *curr;

                    // Only add objects that are in the correct thread
                    // context and have the appropriate flags set.
                    if (inst->ThreadContext == threadContext
                        && (withFlags == ExternalObjectContext::Flags_None || inst->IsSet(withFlags)))
                    {
                        gc.arrRef->SetAt(objCount, inst->GetObjectRef());
                    }
                }
            }

            // Make the array the correct size
            if (objCount < objCountMax)
            {
                gc.arrRefTmp = (PTRARRAYREF)AllocateObjectArray(objCount, g_pObjectClass);

                SIZE_T elementSize = gc.arrRef->GetComponentSize();

                void *src = gc.arrRef->GetDataPtr();
                void *dest = gc.arrRefTmp->GetDataPtr();
                memcpyNoGCRefs(dest, src, objCount * elementSize);
                gc.arrRef = gc.arrRefTmp;
            }

            GCPROTECT_END();

            RETURN gc.arrRef;
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
                PRECONDITION(!Traits::IsNull(cxt) && !Traits::IsDeleted(cxt));
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
                PRECONDITION(!Traits::IsNull(newCxt) && !Traits::IsDeleted(newCxt));
                PRECONDITION(key == newCxt->Identity);
                POSTCONDITION(CheckPointer(RETVAL));
            }
            CONTRACT_END;

            // Forbid the GC from messing with the hash table.
            GCX_FORBID();

            ExternalObjectContext* cxt = Find(key);
            if (cxt == NULL)
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
                PRECONDITION(!Traits::IsNull(cxt) && !Traits::IsDeleted(cxt));
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
    Volatile<ExtObjCxtCache*> ExtObjCxtCache::g_Instance;

    using ExtObjCxtRefCache = RCWRefCache;

    // Defined handle types for the specific object uses.
    const HandleType InstanceHandleType{ HNDTYPE_STRONG };
    const HandleType ComWrappersImplHandleType{ HNDTYPE_STRONG };

    void* CallComputeVTables(
        _In_ OBJECTREF* implPROTECTED,
        _In_ OBJECTREF* instancePROTECTED,
        _In_ INT32 flags,
        _Out_ DWORD* vtableCount)
    {
        CONTRACTL
        {
            THROWS;
            GC_TRIGGERS;
            MODE_COOPERATIVE;
            PRECONDITION(implPROTECTED != NULL);
            PRECONDITION(instancePROTECTED != NULL);
            PRECONDITION(vtableCount != NULL);
        }
        CONTRACTL_END;

        void* vtables = NULL;

        PREPARE_NONVIRTUAL_CALLSITE(METHOD__COMWRAPPERS__COMPUTE_VTABLES);
        DECLARE_ARGHOLDER_ARRAY(args, 4);
        args[ARGNUM_0]  = OBJECTREF_TO_ARGHOLDER(*implPROTECTED);
        args[ARGNUM_1]  = OBJECTREF_TO_ARGHOLDER(*instancePROTECTED);
        args[ARGNUM_2]  = DWORD_TO_ARGHOLDER(flags);
        args[ARGNUM_3]  = PTR_TO_ARGHOLDER(vtableCount);
        CALL_MANAGED_METHOD(vtables, void*, args);

        return vtables;
    }

    OBJECTREF CallGetObject(
        _In_ OBJECTREF* implPROTECTED,
        _In_ IUnknown* externalComObject,
        _In_ IUnknown* agileObjectRef,
        _In_ INT32 flags)
    {
        CONTRACTL
        {
            THROWS;
            GC_TRIGGERS;
            MODE_COOPERATIVE;
            PRECONDITION(implPROTECTED != NULL);
            PRECONDITION(externalComObject != NULL);
        }
        CONTRACTL_END;

        OBJECTREF retObjRef;

        PREPARE_NONVIRTUAL_CALLSITE(METHOD__COMWRAPPERS__CREATE_OBJECT);
        DECLARE_ARGHOLDER_ARRAY(args, 4);
        args[ARGNUM_0]  = OBJECTREF_TO_ARGHOLDER(*implPROTECTED);
        args[ARGNUM_1]  = PTR_TO_ARGHOLDER(externalComObject);
        args[ARGNUM_2]  = PTR_TO_ARGHOLDER(agileObjectRef);
        args[ARGNUM_3]  = DWORD_TO_ARGHOLDER(flags);
        CALL_MANAGED_METHOD(retObjRef, OBJECTREF, args);

        return retObjRef;
    }

    void CallReleaseObjects(
        _In_ OBJECTREF* implPROTECTED,
        _In_ OBJECTREF* objsEnumPROTECTED)
    {
        CONTRACTL
        {
            THROWS;
            GC_TRIGGERS;
            MODE_COOPERATIVE;
            PRECONDITION(implPROTECTED != NULL);
            PRECONDITION(objsEnumPROTECTED != NULL);
        }
        CONTRACTL_END;

        PREPARE_NONVIRTUAL_CALLSITE(METHOD__COMWRAPPERS__RELEASE_OBJECTS);
        DECLARE_ARGHOLDER_ARRAY(args, 2);
        args[ARGNUM_0]  = OBJECTREF_TO_ARGHOLDER(*implPROTECTED);
        args[ARGNUM_1]  = OBJECTREF_TO_ARGHOLDER(*objsEnumPROTECTED);
        CALL_MANAGED_METHOD_NORET(args);
    }

    void* GetOrCreateComInterfaceForObjectInternal(
        _In_ OBJECTREF impl,
        _In_ OBJECTREF instance,
        _In_ INT32 flags)
    {
        CONTRACT(void*)
        {
            THROWS;
            GC_TRIGGERS;
            MODE_COOPERATIVE;
            PRECONDITION(impl != NULL);
            PRECONDITION(instance != NULL);
            POSTCONDITION(CheckPointer(RETVAL));
        }
        CONTRACT_END;

        HRESULT hr;

        SafeComHolder<IUnknown> newWrapper;
        void* wrapper = NULL;

        struct
        {
            OBJECTREF implRef;
            OBJECTREF instRef;
        } gc;
        ::ZeroMemory(&gc, sizeof(gc));
        GCPROTECT_BEGIN(gc);

        gc.implRef = impl;
        gc.instRef = instance;

        // Check the object's SyncBlock for a managed object wrapper.
        SyncBlock* syncBlock = gc.instRef->GetSyncBlock();
        InteropSyncBlockInfo* interopInfo = syncBlock->GetInteropInfo();
        _ASSERTE(syncBlock->IsPrecious());

        // Query the associated InteropSyncBlockInfo for an existing managed object wrapper.
        if (!interopInfo->TryGetManagedObjectComWrapper(&wrapper))
        {
            // Compute VTables for the new existing COM object using the supplied COM Wrappers implementation.
            //
            // N.B. Calling to compute the associated VTables is perhaps early since no lock
            // is taken. However, a key assumption here is that the returned memory will be
            // idempotent for the same object.
            DWORD vtableCount;
            void* vtables = CallComputeVTables(&gc.implRef, &gc.instRef, flags, &vtableCount);

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

        RETURN wrapper;
    }

    OBJECTREF GetOrCreateObjectForComInstanceInternal(
        _In_ OBJECTREF impl,
        _In_ IUnknown* identity,
        _In_ INT32 flags)
    {
        CONTRACT(OBJECTREF)
        {
            THROWS;
            GC_TRIGGERS;
            MODE_COOPERATIVE;
            PRECONDITION(impl != NULL);
            PRECONDITION(identity != NULL);
            POSTCONDITION(RETVAL != NULL);
        }
        CONTRACT_END;

        HRESULT hr;
        ExternalObjectContext* extObjCxt;

        struct
        {
            OBJECTREF implRef;
            OBJECTREF objRef;
        } gc;
        ::ZeroMemory(&gc, sizeof(gc));
        GCPROTECT_BEGIN(gc);

        gc.implRef = impl;

        ExtObjCxtCache* cache = ExtObjCxtCache::GetInstance();

        {
            // Query the external object cache
            ExtObjCxtCache::LockHolder lock(cache);
            extObjCxt = cache->Find(identity);
        }

        if (extObjCxt != NULL)
        {
            gc.objRef = extObjCxt->GetObjectRef();
        }
        else
        {
            // Create context and IAgileReference instance for the possibly new external COM object.
            ExtObjCxtHolder newContext;
            SafeComHolder<IUnknown> agileRef;
            hr = InteropLib::Com::CreateWrapperForExternal(identity, flags, sizeof(ExternalObjectContext), (void**)&newContext, &agileRef);
            if (FAILED(hr))
                COMPlusThrow(hr);

            // Call the implementation to create an external object wrapper.
            gc.objRef = CallGetObject(&gc.implRef, identity, agileRef, flags);
            if (gc.objRef == NULL)
                COMPlusThrow(kArgumentNullException);

            // Construct the new context with the object details.
            DWORD flags = InteropLib::Com::IsReferenceTrackerInstance((void*)newContext)
                            ? ExternalObjectContext::Flags_ReferenceTracker
                            : ExternalObjectContext::Flags_None;
            ExternalObjectContext::Construct(
                newContext,
                identity,
                GetCurrentCtxCookie(),
                gc.objRef->GetSyncBlockIndex(),
                flags);

            // Attempt to insert the new context into the cache.
            {
                ExtObjCxtCache::LockHolder lock(cache);
                extObjCxt = cache->FindOrAdd(identity, newContext);
            }

            // If the returned context matches the new context it means the
            // new context was inserted.
            if (extObjCxt == newContext)
            {
                // Update the object's SyncBlock with a handle to the context for runtime cleanup.
                SyncBlock* syncBlock = gc.objRef->GetSyncBlock();
                InteropSyncBlockInfo* interopInfo = syncBlock->GetInteropInfo();
                _ASSERTE(syncBlock->IsPrecious());
                (void)interopInfo->TrySetExternalComObjectContext((void**)extObjCxt);

                // Detach from the holder to avoid cleanup.
                (void)newContext.Detach();
            }

            _ASSERTE(extObjCxt->IsActive());
        }

        GCPROTECT_END();

        RETURN gc.objRef;
    }
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
            PRECONDITION(mem != NULL);
        }
        CONTRACTL_END;

        ::free(mem);
    }

    HRESULT AddMemoryPressureForExternal(_In_ UINT64 memoryInBytes) noexcept
    {
        CONTRACTL
        {
            NOTHROW;
            MODE_PREEMPTIVE;
        }
        CONTRACTL_END;

        HRESULT hr = S_OK;
        BEGIN_EXTERNAL_ENTRYPOINT(&hr)
        {
            GCInterface::NewAddMemoryPressure(memoryInBytes);
        }
        END_EXTERNAL_ENTRYPOINT;

        return hr;
    }

    HRESULT RemoveMemoryPressureForExternal(_In_ UINT64 memoryInBytes) noexcept
    {
        CONTRACTL
        {
            NOTHROW;
            MODE_PREEMPTIVE;
        }
        CONTRACTL_END;

        HRESULT hr = S_OK;
        BEGIN_EXTERNAL_ENTRYPOINT(&hr)
        {
            GCInterface::NewRemoveMemoryPressure(memoryInBytes);
        }
        END_EXTERNAL_ENTRYPOINT;

        return hr;
    }

    HRESULT RequestGarbageCollectionForExternal(_In_ GcRequest req) noexcept
    {
        CONTRACTL
        {
            NOTHROW;
            MODE_PREEMPTIVE;
        }
        CONTRACTL_END;

        HRESULT hr = S_OK;
        BEGIN_EXTERNAL_ENTRYPOINT(&hr)
        {
            GCX_COOP_THREAD_EXISTS(GET_THREAD());
            if (req == GcRequest::Blocking)
            {
                GCHeapUtilities::GetGCHeap()->GarbageCollect(2, true, collection_blocking | collection_optimized);
            }
            else
            {
                _ASSERTE(req == GcRequest::Default);
                GCHeapUtilities::GetGCHeap()->GarbageCollect();
            }
        }
        END_EXTERNAL_ENTRYPOINT;

        return hr;
    }

    HRESULT WaitForRuntimeFinalizerForExternal() noexcept
    {
        CONTRACTL
        {
            NOTHROW;
            MODE_PREEMPTIVE;
        }
        CONTRACTL_END;

        HRESULT hr = S_OK;
        BEGIN_EXTERNAL_ENTRYPOINT(&hr)
        {
            FinalizerThread::FinalizerThreadWait();
        }
        END_EXTERNAL_ENTRYPOINT;

        return hr;
    }

    HRESULT ReleaseExternalObjectsFromCurrentThread(_In_ InteropLib::OBJECTHANDLE handle) noexcept
    {
        CONTRACTL
        {
            NOTHROW;
            MODE_PREEMPTIVE;
        }
        CONTRACTL_END;

        ::OBJECTHANDLE implHandle = static_cast<::OBJECTHANDLE>(handle);

        HRESULT hr = S_OK;
        BEGIN_EXTERNAL_ENTRYPOINT(&hr)
        {
            // Switch to cooperative mode so the cache can be queried.
            GCX_COOP();

            struct
            {
                OBJECTREF implRef;
                OBJECTREF objsEnumRef;
            } gc;
            ::ZeroMemory(&gc, sizeof(gc));
            GCPROTECT_BEGIN(gc);

            gc.implRef = ObjectFromHandle(implHandle);

            // Pass the objects along to get released.
            ExtObjCxtCache* cache = ExtObjCxtCache::GetInstance();
            gc.objsEnumRef = cache->CreateManagedEnumerable(
                ExternalObjectContext::Flags_ReferenceTracker,
                GetCurrentCtxCookie());

            CallReleaseObjects(&gc.implRef, &gc.objsEnumRef);

            //
            // [TODO] Clear out objects' syncblocks.
            //

            GCPROTECT_END();
        }
        END_EXTERNAL_ENTRYPOINT;

        return hr;
    }

    void DeleteObjectInstanceHandle(_In_ InteropLib::OBJECTHANDLE handle) noexcept
    {
        CONTRACTL
        {
            NOTHROW;
            MODE_ANY;
            PRECONDITION(handle != NULL);
        }
        CONTRACTL_END;

        ::OBJECTHANDLE objectHandle = static_cast<::OBJECTHANDLE>(handle);
        DestroyHandleCommon(objectHandle, InstanceHandleType);
    }

    bool GetGlobalPeggingState() noexcept
    {
        CONTRACTL
        {
            NOTHROW;
            MODE_ANY;
        }
        CONTRACTL_END;

        return (RCWWalker::s_bIsGlobalPeggingOn != FALSE);
    }

    void SetGlobalPeggingState(_In_ bool state) noexcept
    {
        CONTRACTL
        {
            NOTHROW;
            MODE_ANY;
        }
        CONTRACTL_END;

        BOOL newState = state ? TRUE : FALSE;
        VolatileStore(&RCWWalker::s_bIsGlobalPeggingOn, newState);
    }

    HRESULT GetOrCreateTrackerTargetForExternal(
        _In_ InteropLib::OBJECTHANDLE impl,
        _In_ IUnknown* externalComObject,
        _In_ INT32 externalObjectFlags,
        _In_ INT32 trackerTargetFlags,
        _Outptr_ void** trackerTarget) noexcept
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

        HRESULT hr = S_OK;
        ::OBJECTHANDLE implHandle = static_cast<::OBJECTHANDLE>(impl);

        *trackerTarget = NULL;

        // Switch to Cooperative mode since object references
        // are being manipulated.
        BEGIN_EXTERNAL_ENTRYPOINT(&hr)
        {
            GCX_COOP();

            struct
            {
                OBJECTREF implRef;
                OBJECTREF objRef;
            } gc;
            ::ZeroMemory(&gc, sizeof(gc));
            GCPROTECT_BEGIN(gc);

            gc.implRef = ObjectFromHandle(implHandle);

            // Get wrapper for external object
            gc.objRef = GetOrCreateObjectForComInstanceInternal(
                gc.implRef,
                externalComObject,
                externalObjectFlags);

            // Get wrapper for managed object
            *trackerTarget = GetOrCreateComInterfaceForObjectInternal(
                gc.implRef,
                gc.objRef,
                trackerTargetFlags);

            GCPROTECT_END();
        }
        END_EXTERNAL_ENTRYPOINT;

        return hr;
    }

    struct RuntimeCallContext
    {
        RuntimeCallContext(_In_ ExtObjCxtCache* cache, _In_ ExtObjCxtRefCache* refCache)
            : Curr{ cache->_hashMap.Begin() }
            , End{ cache->_hashMap.End() }
            , RefCache{ refCache }
        { }

        // Iterators for all known external objects.
        ExtObjCxtCache::Iterator Curr;
        ExtObjCxtCache::Iterator End;

        // Pointer to cache used to create object references.
        ExtObjCxtRefCache* RefCache;
    };

    HRESULT IteratorNext(
        _In_ RuntimeCallContext* runtimeContext,
        _Outptr_result_maybenull_ void** extObjContext) noexcept
    {
        CONTRACTL
        {
            NOTHROW;
            GC_NOTRIGGER;
            MODE_COOPERATIVE;
            PRECONDITION(runtimeContext != NULL);
            PRECONDITION(extObjContext != NULL);

            // Should only be called during a GC suspension
            PRECONDITION(Debug_IsLockedViaThreadSuspension());
        }
        CONTRACTL_END;

        if (runtimeContext->Curr == runtimeContext->End)
        {
            *extObjContext = NULL;
            return S_FALSE;
        }

        ExtObjCxtCache::Element e = *runtimeContext->Curr++;
        *extObjContext = e;
        return S_OK;
    }

    HRESULT FoundReferencePath(
        _In_ RuntimeCallContext* runtimeContext,
        _In_ void* extObjContextRaw,
        _In_ InteropLib::OBJECTHANDLE handle) noexcept
    {
        CONTRACTL
        {
            NOTHROW;
            GC_NOTRIGGER;
            MODE_COOPERATIVE;
            PRECONDITION(runtimeContext != NULL);
            PRECONDITION(extObjContextRaw != NULL);
            PRECONDITION(handle != NULL);

            // Should only be called during a GC suspension
            PRECONDITION(Debug_IsLockedViaThreadSuspension());
        }
        CONTRACTL_END;

        // Get the external object's managed wrapper
        ExternalObjectContext* extObjContext = static_cast<ExternalObjectContext*>(extObjContextRaw);
        OBJECTREF source = extObjContext->GetObjectRef();

        // Get the target of the external object's reference.
        ::OBJECTHANDLE objectHandle = static_cast<::OBJECTHANDLE>(handle);
        OBJECTREF target = ObjectFromHandle(objectHandle);

        // If these point at the same object don't create a reference.
        if (source->GetSyncBlock() == target->GetSyncBlock())
            return S_FALSE;

        return runtimeContext->RefCache->AddReferenceFromObjectToObject(source, target);
    }
}

#ifdef FEATURE_COMINTEROP

void* QCALLTYPE ComWrappersNative::GetOrCreateComInterfaceForObject(
    _In_ QCall::ObjectHandleOnStack comWrappersImpl,
    _In_ QCall::ObjectHandleOnStack instance,
    _In_ INT32 flags)
{
    QCALL_CONTRACT;

    void* wrapper = NULL;

    BEGIN_QCALL;

    // Switch to Cooperative mode since object references
    // are being manipulated.
    {
        GCX_COOP();
        wrapper = GetOrCreateComInterfaceForObjectInternal(
            ObjectToOBJECTREF(*comWrappersImpl.m_ppObject),
            ObjectToOBJECTREF(*instance.m_ppObject),
            flags);
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
    IUnknown* externalComObject = reinterpret_cast<IUnknown*>(ext);

    // Determine the true identity of the object
    SafeComHolder<IUnknown> identity;
    hr = externalComObject->QueryInterface(IID_IUnknown, &identity);
    _ASSERTE(hr == S_OK);

    // Switch to Cooperative mode since object references
    // are being manipulated.
    {
        GCX_COOP();
        OBJECTREF newObj = GetOrCreateObjectForComInstanceInternal(
            ObjectToOBJECTREF(*comWrappersImpl.m_ppObject),
            identity,
            flags);

        // Set the return value
        retValue.Set(newObj);
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

        if (!InteropLib::Com::RegisterReferenceTrackerHostRuntimeImpl(implHandle))
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
        MODE_ANY;
        PRECONDITION(wrapper != NULL);
    }
    CONTRACTL_END;

    InteropLib::Com::DestroyWrapperForObject(wrapper);
}

void ComWrappersNative::DestroyExternalComObjectContext(_In_ void* contextRaw)
{
    CONTRACTL
    {
        NOTHROW;
        MODE_ANY;
        PRECONDITION(contextRaw != NULL);
    }
    CONTRACTL_END;

#ifdef _DEBUG
    ExternalObjectContext* context = static_cast<ExternalObjectContext*>(contextRaw);
    _ASSERTE(!context->IsActive());
#endif

    InteropLib::Com::DestroyWrapperForExternal(context);
}

void ComWrappersNative::MarkExternalComObjectContextCollected(_In_ void* contextRaw)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        PRECONDITION(contextRaw != NULL);
        PRECONDITION(GCHeapUtilities::IsGCInProgress());
    }
    CONTRACTL_END;

    ExternalObjectContext* context = static_cast<ExternalObjectContext*>(contextRaw);
    _ASSERTE(context->IsActive());
    context->MarkCollected();

    ExtObjCxtCache* cache = ExtObjCxtCache::GetInstance();
    _ASSERTE(cache != NULL);
    cache->Remove(context);
}

#endif // FEATURE_COMINTEROP

void Interop::OnGCStarted(_In_ int nCondemnedGeneration)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

#ifdef FEATURE_COMINTEROP
    //
    // Let GC detect managed/native cycles with input from jupiter
    // Jupiter will
    // 1. Report reference from RCW to CCW based on native reference in Jupiter
    // 2. Identify the subset of CCWs that needs to be rooted
    //
    // We'll build the references from RCW to CCW using
    // 1. Preallocated arrays
    // 2. Dependent handles
    //
    RCWWalker::OnGCStarted(nCondemnedGeneration);

    //
    // Note that we could get nested GCStart/GCEnd calls, such as :
    // GCStart for Gen 2 background GC
    //    GCStart for Gen 0/1 foregorund GC
    //    GCEnd   for Gen 0/1 foreground GC
    //    ....
    // GCEnd for Gen 2 background GC
    //
    // The nCondemnedGeneration >= 2 check takes care of this nesting problem
    //
    // See Interop::OnGCFinished()
    if (nCondemnedGeneration >= 2)
    {
        ExtObjCxtCache* cache = ExtObjCxtCache::GetInstance();
        _ASSERTE(cache != NULL);

        RCWRefCache *refCache = GetAppDomain()->GetRCWRefCache();

        // Reset the ref cache
        refCache->ResetDependentHandles();

        // Create a call context for the InteropLib.
        InteropLibImports::RuntimeCallContext cxt(cache, refCache);
        (void)InteropLib::Com::BeginExternalObjectReferenceTracking(&cxt);

        // Shrink cache and clear unused handles.
        refCache->ShrinkDependentHandles();
    }

#endif // FEATURE_COMINTEROP
}

void Interop::OnGCFinished(_In_ int nCondemnedGeneration)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

#ifdef FEATURE_COMINTEROP
    //
    // Tell Jupiter GC has finished
    //
    RCWWalker::OnGCFinished(nCondemnedGeneration);

    //
    // Note that we could get nested GCStart/GCEnd calls, such as :
    // GCStart for Gen 2 background GC
    //    GCStart for Gen 0/1 foregorund GC
    //    GCEnd   for Gen 0/1 foreground GC
    //    ....
    // GCEnd for Gen 2 background GC
    //
    // The nCondemnedGeneration >= 2 check takes care of this nesting problem
    //
    // See Interop::OnGCStarted()
    if (nCondemnedGeneration >= 2)
        (void)InteropLib::Com::EndExternalObjectReferenceTracking();

#endif // FEATURE_COMINTEROP
}

void Interop::OnRuntimeShutdown()
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    } CONTRACTL_END;

#ifdef FEATURE_COMINTEROP
    // Release IJupiterGCMgr*
    RCWWalker::OnEEShutdown();

    InteropLib::Shutdown();

#endif // FEATURE_COMINTEROP
}
