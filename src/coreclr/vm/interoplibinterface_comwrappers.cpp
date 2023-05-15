// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifdef FEATURE_COMWRAPPERS

// Runtime headers
#include "common.h"
#include "rcwrefcache.h"
#ifdef FEATURE_COMINTEROP_APARTMENT_SUPPORT
#include "olecontexthelpers.h"
#endif
#include "finalizerthread.h"

// Interop library header
#include <interoplibimports.h>

#include "interoplibinterface.h"

using CreateObjectFlags = InteropLib::Com::CreateObjectFlags;
using CreateComInterfaceFlags = InteropLib::Com::CreateComInterfaceFlags;

namespace
{
    void* GetCurrentCtxCookieWrapper()
    {
        STATIC_CONTRACT_WRAPPER;

    #ifdef FEATURE_COMINTEROP_APARTMENT_SUPPORT
        return GetCurrentCtxCookie();
    #else
        return NULL;
    #endif // FEATURE_COMINTEROP_APARTMENT_SUPPORT
    }

    // This class is used to track the external object within the runtime.
    struct ExternalObjectContext : public InteropLibInterface::ExternalObjectContextBase
    {
        static const DWORD InvalidSyncBlockIndex;

        void* ThreadContext;
        INT64 WrapperId;

        enum
        {
            Flags_None = 0,

            // The EOC has been collected and is no longer visible from managed code.
            Flags_Collected = 1,

            Flags_ReferenceTracker = 2,
            Flags_InCache = 4,

            // The EOC is "detached" and no longer used to map between identity and a managed object.
            // This will only be set if the EOC was inserted into the cache.
            Flags_Detached = 8,

            // This EOC is an aggregated instance
            Flags_Aggregated = 16
        };
        DWORD Flags;

        static void Construct(
            _Out_ ExternalObjectContext* cxt,
            _In_ IUnknown* identity,
            _In_opt_ void* threadContext,
            _In_ DWORD syncBlockIndex,
            _In_ INT64 wrapperId,
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
            cxt->WrapperId = wrapperId;
            cxt->Flags = flags;
        }

        bool IsSet(_In_ DWORD f) const
        {
            return ((Flags & f) == f);
        }

        bool IsActive() const
        {
            return !IsSet(Flags_Collected)
                && (SyncBlockIndex != InvalidSyncBlockIndex);
        }

        void MarkCollected()
        {
            _ASSERTE(GCHeapUtilities::IsGCInProgress());
            SyncBlockIndex = InvalidSyncBlockIndex;
            Flags |= Flags_Collected;
        }

        void MarkDetached()
        {
            _ASSERTE(GCHeapUtilities::IsGCInProgress());
            Flags |= Flags_Detached;
        }

        void MarkNotInCache()
        {
            ::InterlockedAnd((LONG*)&Flags, (~Flags_InCache));
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

        struct Key
        {
        public:
            Key(void* identity, INT64 wrapperId)
                : _identity { identity }
                , _wrapperId { wrapperId }
            {
                _ASSERTE(identity != NULL);
                _ASSERTE(wrapperId != ComWrappersNative::InvalidWrapperId);
            }

            DWORD Hash() const
            {
                DWORD hash = (_wrapperId >> 32) ^ (_wrapperId & 0xFFFFFFFF);
#if POINTER_BITS == 32
                return hash ^ (DWORD)_identity;
#else
                INT64 identityInt64 = (INT64)_identity;
                return hash ^ (identityInt64 >> 32) ^ (identityInt64 & 0xFFFFFFFF);
#endif
            }

            bool operator==(const Key & rhs) const { return _identity == rhs._identity && _wrapperId == rhs._wrapperId; }

        private:
            void* _identity;
            INT64 _wrapperId;
        };

        Key GetKey() const
        {
            return Key(Identity, WrapperId);
        }
    };

    const DWORD ExternalObjectContext::InvalidSyncBlockIndex = 0; // See syncblk.h

    static_assert((sizeof(ExternalObjectContext) % sizeof(void*)) == 0, "Keep context pointer size aligned");

    // Holder for a External Wrapper Result
    struct ExternalWrapperResultHolder
    {
        InteropLib::Com::ExternalWrapperResult Result;
        ExternalWrapperResultHolder()
            : Result{}
        { }
        ~ExternalWrapperResultHolder()
        {
            if (Result.Context != NULL)
            {
                GCX_PREEMP();
                // We also request collection notification since this holder is presently
                // only used for new activation of wrappers therefore the notification won't occur
                // at the typical time of before finalization.
                InteropLib::Com::DestroyWrapperForExternal(Result.Context, /* notifyIsBeingCollected */ true);
            }
        }
        InteropLib::Com::ExternalWrapperResult* operator&()
        {
            return &Result;
        }
        ExternalObjectContext* GetContext()
        {
            return static_cast<ExternalObjectContext*>(Result.Context);
        }
        ExternalObjectContext* DetachContext()
        {
            ExternalObjectContext* t = GetContext();
            Result.Context = NULL;
            return t;
        }
    };

    using ExtObjCxtRefCache = RCWRefCache;

    class ExtObjCxtCache
    {
        static Volatile<ExtObjCxtCache*> g_Instance;

    public: // static
        static ExtObjCxtCache* GetInstanceNoThrow() noexcept
        {
            CONTRACT(ExtObjCxtCache*)
            {
                NOTHROW;
                GC_NOTRIGGER;
                POSTCONDITION(CheckPointer(RETVAL, NULL_OK));
            }
            CONTRACT_END;

            RETURN g_Instance;
        }

        static ExtObjCxtCache* GetInstance()
        {
            CONTRACT(ExtObjCxtCache*)
            {
                THROWS;
                GC_NOTRIGGER;
                POSTCONDITION(RETVAL != NULL);
            }
            CONTRACT_END;

            if (g_Instance.Load() == NULL)
            {
                ExtObjCxtCache* instMaybe = new ExtObjCxtCache();

                // Attempt to set the global instance.
                if (NULL != InterlockedCompareExchangeT(&g_Instance, instMaybe, NULL))
                    delete instMaybe;
            }

            RETURN g_Instance;
        }

    public: // Inner class definitions
        class Traits : public DefaultSHashTraits<ExternalObjectContext *>
        {
        public:
            using key_t = ExternalObjectContext::Key;
            static const key_t GetKey(_In_ element_t e) { LIMITED_METHOD_CONTRACT; return e->GetKey(); }
            static count_t Hash(_In_ key_t key) { LIMITED_METHOD_CONTRACT; return (count_t)key.Hash(); }
            static bool Equals(_In_ key_t lhs, _In_ key_t rhs) { LIMITED_METHOD_CONTRACT; return lhs == rhs; }
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
        ExtObjCxtRefCache* _refCache;

        ExtObjCxtCache()
            : _lock(CrstExternalObjectContextCache, CRST_UNSAFE_COOPGC)
            , _refCache(GetAppDomain()->GetRCWRefCache())
        { }
        ~ExtObjCxtCache() = default;

    public:
#if _DEBUG
        bool IsLockHeld()
        {
            WRAPPER_NO_CONTRACT;
            return (_lock.OwnedByCurrentThread() != FALSE);
        }
#endif // _DEBUG

        // Get the associated reference cache with this external object cache.
        ExtObjCxtRefCache* GetRefCache()
        {
            WRAPPER_NO_CONTRACT;
            return _refCache;
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
                PRECONDITION(!(withFlags & ExternalObjectContext::Flags_Collected));
                PRECONDITION(!(withFlags & ExternalObjectContext::Flags_Detached));
                POSTCONDITION(RETVAL != NULL);
            }
            CONTRACT_END;

            struct
            {
                PTRARRAYREF arrRefTmp;
                PTRARRAYREF arrRef;
            } gc;
            gc.arrRefTmp = NULL;
            gc.arrRef = NULL;
            GCPROTECT_BEGIN(gc);

            // Only add objects that are in the correct thread
            // context, haven't been detached from the cache, and have
            // the appropriate flags set.
            // Define a macro predicate since it used in multiple places.
            // If an instance is in the hashmap, it is active. This invariant
            // holds because the GC is what marks and removes from the cache.
#define SELECT_OBJECT(XX) XX->ThreadContext == threadContext \
                            && !XX->IsSet(ExternalObjectContext::Flags_Detached) \
                            && (withFlags == ExternalObjectContext::Flags_None || XX->IsSet(withFlags))

            // Determine the count of objects to return.
            SIZE_T objCountMax = 0;
            {
                LockHolder lock(this);
                Iterator end = _hashMap.End();
                for (Iterator curr = _hashMap.Begin(); curr != end; ++curr)
                {
                    ExternalObjectContext* inst = *curr;
                    if (SELECT_OBJECT(inst))
                    {
                        objCountMax++;
                    }
                }
            }

            // Allocate enumerable type to return.
            gc.arrRef = (PTRARRAYREF)AllocateObjectArray((DWORD)objCountMax, g_pObjectClass);

            CQuickArrayList<ExternalObjectContext*> localList;

            // Iterate over the hashmap again while populating the above array
            // using the same predicate as before and holding onto context instances.
            SIZE_T objCount = 0;
            if (0 < objCountMax)
            {
                LockHolder lock(this);
                Iterator end = _hashMap.End();
                for (Iterator curr = _hashMap.Begin(); curr != end; ++curr)
                {
                    ExternalObjectContext* inst = *curr;
                    if (SELECT_OBJECT(inst))
                    {
                        localList.Push(inst);

                        gc.arrRef->SetAt(objCount, inst->GetObjectRef());
                        objCount++;

                        STRESS_LOG1(LF_INTEROP, LL_INFO1000, "Add EOC to Enumerable: 0x%p\n", inst);
                    }

                    // There is a chance more objects were added to the hash while the
                    // lock was released during array allocation. Once we hit the computed max
                    // we stop to avoid looking longer than needed.
                    if (objCount == objCountMax)
                        break;
                }
            }

#undef SELECT_OBJECT

            // During the allocation of the array to return, a GC could have
            // occurred and objects detached from this cache. In order to avoid
            // having null array elements we will allocate a new array.
            // This subsequent allocation is okay because the array we are
            // replacing extends all object lifetimes.
            if (objCount < objCountMax)
            {
                gc.arrRefTmp = (PTRARRAYREF)AllocateObjectArray((DWORD)objCount, g_pObjectClass);

                void* dest = gc.arrRefTmp->GetDataPtr();
                void* src = gc.arrRef->GetDataPtr();
                SIZE_T elementSize = gc.arrRef->GetComponentSize();

                memmoveGCRefs(dest, src, objCount * elementSize);
                gc.arrRef = gc.arrRefTmp;
            }

            // All objects are now referenced from the array so won't be collected
            // at this point. This means we can safely iterate over the ExternalObjectContext
            // instances.
            {
                // Separate the wrapper from the tracker runtime prior to
                // passing them onto the caller. This call is okay to make
                // even if the instance isn't from the tracker runtime.
                // We switch to Preemptive mode since separating a wrapper
                // requires us to call out to non-runtime code which could
                // call back into the runtime and/or trigger a GC.
                GCX_PREEMP();
                for (SIZE_T i = 0; i < localList.Size(); i++)
                {
                    ExternalObjectContext* inst = localList[i];
                    InteropLib::Com::SeparateWrapperFromTrackerRuntime(inst);
                }
            }

            GCPROTECT_END();

            RETURN gc.arrRef;
        }

        ExternalObjectContext* Find(_In_ const ExternalObjectContext::Key& key)
        {
            CONTRACT(ExternalObjectContext*)
            {
                NOTHROW;
                GC_NOTRIGGER;
                MODE_COOPERATIVE;
                PRECONDITION(IsLockHeld());
                POSTCONDITION(CheckPointer(RETVAL, NULL_OK));
            }
            CONTRACT_END;

            // Forbid the GC from messing with the hash table.
            GCX_FORBID();

            RETURN _hashMap.Lookup(key);
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
                PRECONDITION(Find(cxt->GetKey()) == NULL);
                POSTCONDITION(RETVAL == cxt);
            }
            CONTRACT_END;

            _hashMap.Add(cxt);
            RETURN cxt;
        }

        ExternalObjectContext* FindOrAdd(_In_ const ExternalObjectContext::Key& key, _In_ ExternalObjectContext* newCxt)
        {
            CONTRACT(ExternalObjectContext*)
            {
                THROWS;
                GC_NOTRIGGER;
                MODE_COOPERATIVE;
                PRECONDITION(IsLockHeld());
                PRECONDITION(!Traits::IsNull(newCxt) && !Traits::IsDeleted(newCxt));
                PRECONDITION(key == newCxt->GetKey());
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

            _hashMap.Remove(cxt->GetKey());
        }

        void DetachNotPromotedEOCs()
        {
            CONTRACTL
            {
                NOTHROW;
                GC_NOTRIGGER;
                MODE_ANY;
                PRECONDITION(GCHeapUtilities::IsGCInProgress()); // GC is in progress and the runtime is suspended
            }
            CONTRACTL_END;

            Iterator curr = _hashMap.Begin();
            Iterator end = _hashMap.End();

            ExternalObjectContext* cxt;
            for (; curr != end; ++curr)
            {
                cxt = *curr;
                if (!cxt->IsSet(ExternalObjectContext::Flags_Detached)
                    && !GCHeapUtilities::GetGCHeap()->IsPromoted(OBJECTREFToObject(cxt->GetObjectRef())))
                {
                    // Indicate the wrapper entry has been detached.
                    cxt->MarkDetached();

                    // Notify the wrapper it was not promoted and is being collected.
                    InteropLib::Com::NotifyWrapperForExternalIsBeingCollected(cxt);
                }
            }
        }
    };

    // Global instance of the external object cache
    Volatile<ExtObjCxtCache*> ExtObjCxtCache::g_Instance;

    // Indicator for if a ComWrappers implementation is globally registered
    INT64 g_marshallingGlobalInstanceId = ComWrappersNative::InvalidWrapperId;
    INT64 g_trackerSupportGlobalInstanceId = ComWrappersNative::InvalidWrapperId;

    // Defined handle types for the specific object uses.
    const HandleType InstanceHandleType{ HNDTYPE_REFCOUNTED };

    // Scenarios for ComWrappers usage.
    // These values should match the managed definition in ComWrappers.
    enum class ComWrappersScenario
    {
        Instance = 0,
        TrackerSupportGlobalInstance = 1,
        MarshallingGlobalInstance = 2,
    };

    void* CallComputeVTables(
        _In_ ComWrappersScenario scenario,
        _In_ OBJECTREF* implPROTECTED,
        _In_ OBJECTREF* instancePROTECTED,
        _In_ INT32 flags,
        _Out_ DWORD* vtableCount)
    {
        CONTRACTL
        {
            THROWS;
            MODE_COOPERATIVE;
            PRECONDITION(implPROTECTED != NULL);
            PRECONDITION(instancePROTECTED != NULL);
            PRECONDITION(vtableCount != NULL);
        }
        CONTRACTL_END;

        void* vtables = NULL;

        PREPARE_NONVIRTUAL_CALLSITE(METHOD__COMWRAPPERS__COMPUTE_VTABLES);
        DECLARE_ARGHOLDER_ARRAY(args, 5);
        args[ARGNUM_0]  = DWORD_TO_ARGHOLDER(scenario);
        args[ARGNUM_1]  = OBJECTREF_TO_ARGHOLDER(*implPROTECTED);
        args[ARGNUM_2]  = OBJECTREF_TO_ARGHOLDER(*instancePROTECTED);
        args[ARGNUM_3]  = DWORD_TO_ARGHOLDER(flags);
        args[ARGNUM_4]  = PTR_TO_ARGHOLDER(vtableCount);
        CALL_MANAGED_METHOD(vtables, void*, args);

        return vtables;
    }

    OBJECTREF CallCreateObject(
        _In_ ComWrappersScenario scenario,
        _In_ OBJECTREF* implPROTECTED,
        _In_ IUnknown* externalComObject,
        _In_ INT32 flags)
    {
        CONTRACTL
        {
            THROWS;
            MODE_COOPERATIVE;
            PRECONDITION(implPROTECTED != NULL);
            PRECONDITION(externalComObject != NULL);
        }
        CONTRACTL_END;

        OBJECTREF retObjRef;

        PREPARE_NONVIRTUAL_CALLSITE(METHOD__COMWRAPPERS__CREATE_OBJECT);
        DECLARE_ARGHOLDER_ARRAY(args, 4);
        args[ARGNUM_0]  = DWORD_TO_ARGHOLDER(scenario);
        args[ARGNUM_1]  = OBJECTREF_TO_ARGHOLDER(*implPROTECTED);
        args[ARGNUM_2]  = PTR_TO_ARGHOLDER(externalComObject);
        args[ARGNUM_3]  = DWORD_TO_ARGHOLDER(flags);
        CALL_MANAGED_METHOD_RETREF(retObjRef, OBJECTREF, args);

        return retObjRef;
    }

    void CallReleaseObjects(
        _In_ OBJECTREF* implPROTECTED,
        _In_ OBJECTREF* objsEnumPROTECTED)
    {
        CONTRACTL
        {
            THROWS;
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

    int CallICustomQueryInterface(
        _In_ OBJECTREF* implPROTECTED,
        _In_ REFGUID iid,
        _Outptr_result_maybenull_ void** ppObject)
    {
        CONTRACTL
        {
            THROWS;
            MODE_COOPERATIVE;
            PRECONDITION(implPROTECTED != NULL);
            PRECONDITION(ppObject != NULL);
        }
        CONTRACTL_END;

        int result;

        PREPARE_NONVIRTUAL_CALLSITE(METHOD__COMWRAPPERS__CALL_ICUSTOMQUERYINTERFACE);
        DECLARE_ARGHOLDER_ARRAY(args, 3);
        args[ARGNUM_0]  = OBJECTREF_TO_ARGHOLDER(*implPROTECTED);
        args[ARGNUM_1]  = PTR_TO_ARGHOLDER(&iid);
        args[ARGNUM_2]  = PTR_TO_ARGHOLDER(ppObject);
        CALL_MANAGED_METHOD(result, int, args);

        return result;
    }

    bool TryGetOrCreateComInterfaceForObjectInternal(
        _In_opt_ OBJECTREF impl,
        _In_ INT64 wrapperId,
        _In_ OBJECTREF instance,
        _In_ CreateComInterfaceFlags flags,
        _In_ ComWrappersScenario scenario,
        _Outptr_ void** wrapperRaw)
    {
        CONTRACT(bool)
        {
            THROWS;
            MODE_COOPERATIVE;
            PRECONDITION(instance != NULL);
            PRECONDITION(wrapperRaw != NULL);
            PRECONDITION((impl != NULL && scenario == ComWrappersScenario::Instance) || (impl == NULL && scenario != ComWrappersScenario::Instance));
            PRECONDITION(wrapperId != ComWrappersNative::InvalidWrapperId);
        }
        CONTRACT_END;

        HRESULT hr;

        SafeComHolder<IUnknown> newWrapper;
        void* wrapperRawMaybe = NULL;

        struct
        {
            OBJECTREF implRef;
            OBJECTREF instRef;
        } gc;
        gc.implRef = impl;
        gc.instRef = instance;
        GCPROTECT_BEGIN(gc);

        // Check the object's SyncBlock for a managed object wrapper.
        SyncBlock* syncBlock = gc.instRef->GetSyncBlock();
        InteropSyncBlockInfo* interopInfo = syncBlock->GetInteropInfo();
        _ASSERTE(syncBlock->IsPrecious());

        // Query the associated InteropSyncBlockInfo for an existing managed object wrapper.
        if (!interopInfo->TryGetManagedObjectComWrapper(wrapperId, &wrapperRawMaybe))
        {
            // Compute VTables for the new existing COM object using the supplied COM Wrappers implementation.
            //
            // N.B. Calling to compute the associated VTables is perhaps early since no lock
            // is taken. However, a key assumption here is that the returned memory will be
            // idempotent for the same object.
            DWORD vtableCount;
            void* vtables = CallComputeVTables(scenario, &gc.implRef, &gc.instRef, flags, &vtableCount);

            // Re-query the associated InteropSyncBlockInfo for an existing managed object wrapper.
            if (!interopInfo->TryGetManagedObjectComWrapper(wrapperId, &wrapperRawMaybe)
                && ((vtables != nullptr && vtableCount > 0) || (vtableCount == 0)))
            {
                OBJECTHANDLE instHandle = GetAppDomain()->CreateTypedHandle(gc.instRef, InstanceHandleType);

                // Call the InteropLib and create the associated managed object wrapper.
                {
                    GCX_PREEMP();
                    hr = InteropLib::Com::CreateWrapperForObject(
                        instHandle,
                        vtableCount,
                        vtables,
                        flags,
                        &newWrapper);
                }
                if (FAILED(hr))
                {
                    DestroyHandleCommon(instHandle, InstanceHandleType);
                    COMPlusThrowHR(hr);
                }
                _ASSERTE(!newWrapper.IsNull());

                // Try setting the newly created managed object wrapper on the InteropSyncBlockInfo.
                if (!interopInfo->TrySetManagedObjectComWrapper(wrapperId, newWrapper))
                {
                    // The new wrapper couldn't be set which means a wrapper already exists.
                    newWrapper.Release();

                    // If the managed object wrapper couldn't be set, then
                    // it should be possible to get the current one.
                    if (!interopInfo->TryGetManagedObjectComWrapper(wrapperId, &wrapperRawMaybe))
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
            wrapperRawMaybe = newWrapper.Extract();
            STRESS_LOG1(LF_INTEROP, LL_INFO100, "Created MOW: 0x%p\n", wrapperRawMaybe);
        }
        else if (wrapperRawMaybe != NULL)
        {
            // AddRef() the existing wrapper.
            IUnknown* wrapper = static_cast<IUnknown*>(wrapperRawMaybe);
            (void)wrapper->AddRef();
        }

        GCPROTECT_END();

        *wrapperRaw = wrapperRawMaybe;
        RETURN (wrapperRawMaybe != NULL);
    }

    bool TryGetOrCreateObjectForComInstanceInternal(
        _In_opt_ OBJECTREF impl,
        _In_ INT64 wrapperId,
        _In_ IUnknown* identity,
        _In_opt_ IUnknown* inner,
        _In_ CreateObjectFlags flags,
        _In_ ComWrappersScenario scenario,
        _In_opt_ OBJECTREF wrapperMaybe,
        _Out_ OBJECTREF* objRef)
    {
        CONTRACT(bool)
        {
            THROWS;
            MODE_COOPERATIVE;
            PRECONDITION(identity != NULL);
            PRECONDITION(objRef != NULL);
            PRECONDITION((impl != NULL && scenario == ComWrappersScenario::Instance) || (impl == NULL && scenario != ComWrappersScenario::Instance));
            PRECONDITION(wrapperId != ComWrappersNative::InvalidWrapperId);
        }
        CONTRACT_END;

        HRESULT hr;
        ExternalObjectContext* extObjCxt = NULL;

        struct
        {
            OBJECTREF implRef;
            OBJECTREF wrapperMaybeRef;
            OBJECTREF objRefMaybe;
        } gc;
        gc.implRef = impl;
        gc.wrapperMaybeRef = wrapperMaybe;
        gc.objRefMaybe = NULL;
        GCPROTECT_BEGIN(gc);

        STRESS_LOG4(LF_INTEROP, LL_INFO1000, "Get or Create EOC: (Identity: 0x%p) (Flags: %x) (Maybe: 0x%p) (ID: %lld)\n", identity, flags, OBJECTREFToObject(wrapperMaybe), wrapperId);

        ExtObjCxtCache* cache = ExtObjCxtCache::GetInstance();
        InteropLib::OBJECTHANDLE handle = NULL;

        ExternalObjectContext::Key cacheKey(identity, wrapperId);

        // Check if the user requested a unique instance.
        bool uniqueInstance = !!(flags & CreateObjectFlags::CreateObjectFlags_UniqueInstance);
        if (!uniqueInstance)
        {
            bool objectFound = false;
            {
                // Query the external object cache
                ExtObjCxtCache::LockHolder lock(cache);
                extObjCxt = cache->Find(cacheKey);

                objectFound = extObjCxt != NULL;
                if (objectFound && extObjCxt->IsSet(ExternalObjectContext::Flags_Detached))
                {
                    // If an EOC has been found but is marked detached, then we will remove it from the
                    // cache here instead of letting the GC do it later and pretend like it wasn't found.
                    STRESS_LOG1(LF_INTEROP, LL_INFO10, "Detached EOC requested: 0x%p\n", extObjCxt);
                    cache->Remove(extObjCxt);
                    extObjCxt->MarkNotInCache();
                    extObjCxt = NULL;
                }
            }

            // If is no object found in the cache, check if the object COM instance is actually the CCW
            // representing a managed object. If the user passed the Unwrap flag, COM instances that are
            // actually CCWs should be unwrapped to the original managed object to allow for round
            // tripping object -> COM instance -> object.
            if (!objectFound && (flags & CreateObjectFlags::CreateObjectFlags_Unwrap))
            {
                GCX_PREEMP();

                // If the COM instance is a CCW that is not COM-activated, use the object of that wrapper object.
                InteropLib::OBJECTHANDLE handleLocal;
                if (InteropLib::Com::GetObjectForWrapper(identity, &handleLocal) ==  S_OK
                    && InteropLib::Com::IsComActivated(identity) == S_FALSE)
                {
                    handle = handleLocal;
                }
            }
        }

        STRESS_LOG2(LF_INTEROP, LL_INFO1000, "EOC: 0x%p or Handle: 0x%p\n", extObjCxt, handle);

        if (extObjCxt != NULL)
        {
            gc.objRefMaybe = extObjCxt->GetObjectRef();
        }
        else if (handle != NULL)
        {
            // We have an object handle from the COM instance which is a CCW.
            ::OBJECTHANDLE objectHandle = static_cast<::OBJECTHANDLE>(handle);

            // Now we need to check if this object is a CCW from the same ComWrappers instance
            // as the one creating the EOC. If it is not, we need to create a new EOC for it.
            // Otherwise, use it. This allows for the round-trip from object -> COM instance -> object.
            OBJECTREF objRef = NULL;
            GCPROTECT_BEGIN(objRef);
            objRef = ObjectFromHandle(objectHandle);

            SyncBlock* syncBlock = objRef->GetSyncBlock();
            InteropSyncBlockInfo* interopInfo = syncBlock->GetInteropInfo();

            // If we found a managed object wrapper in this ComWrappers instance
            // and it's the same identity pointer as the one we're creating an EOC for,
            // unwrap it. We don't AddRef the wrapper as we don't take a reference to it.
            //
            // A managed object can have multiple managed object wrappers, with a max of one per context.
            // Let's say we have a managed object A and ComWrappers instances C1 and C2. Let B1 and B2 be the
            // managed object wrappers for A created with C1 and C2 respectively.
            // If we are asked to create an EOC for B1 with the unwrap flag on the C2 ComWrappers instance,
            // we will create a new wrapper. In this scenario, we'll only unwrap B2.
            void* wrapperRawMaybe = NULL;
            if (interopInfo->TryGetManagedObjectComWrapper(wrapperId, &wrapperRawMaybe)
                && wrapperRawMaybe == identity)
            {
                gc.objRefMaybe = objRef;
            }
            else
            {
                STRESS_LOG2(LF_INTEROP, LL_INFO1000, "Not unwrapping handle (0x%p) because the object's MOW in this ComWrappers instance (if any) (0x%p) is not the provided identity\n", handle, wrapperRawMaybe);
            }
            GCPROTECT_END();
        }

        if (gc.objRefMaybe == NULL)
        {
            // Create context instance for the possibly new external object.
            ExternalWrapperResultHolder resultHolder;

            {
                GCX_PREEMP();
                hr = InteropLib::Com::CreateWrapperForExternal(
                    identity,
                    inner,
                    flags,
                    sizeof(ExternalObjectContext),
                    &resultHolder);
            }
            if (FAILED(hr))
                COMPlusThrowHR(hr);

            // The user could have supplied a wrapper so assign that now.
            gc.objRefMaybe = gc.wrapperMaybeRef;

            // If the wrapper hasn't been set yet, call the implementation to create one.
            if (gc.objRefMaybe == NULL)
            {
                gc.objRefMaybe = CallCreateObject(scenario, &gc.implRef, identity, flags);
            }

            // The object may be null if the specified ComWrapper implementation returns null
            // or there is no registered global instance. It is the caller's responsibility
            // to handle this case and error if necessary.
            if (gc.objRefMaybe != NULL)
            {
                // Construct the new context with the object details.
                DWORD eocFlags = (resultHolder.Result.FromTrackerRuntime
                                ? ExternalObjectContext::Flags_ReferenceTracker
                                : ExternalObjectContext::Flags_None) |
                            (uniqueInstance
                                ? ExternalObjectContext::Flags_None
                                : ExternalObjectContext::Flags_InCache) |
                            ((flags & CreateObjectFlags::CreateObjectFlags_Aggregated) != 0
                                ? ExternalObjectContext::Flags_Aggregated
                                : ExternalObjectContext::Flags_None);

                ExternalObjectContext::Construct(
                    resultHolder.GetContext(),
                    identity,
                    GetCurrentCtxCookieWrapper(),
                    gc.objRefMaybe->GetSyncBlockIndex(),
                    wrapperId,
                    eocFlags);

                if (uniqueInstance)
                {
                    extObjCxt = resultHolder.GetContext();
                }
                else
                {
                    // Attempt to insert the new context into the cache.
                    ExtObjCxtCache::LockHolder lock(cache);
                    extObjCxt = cache->FindOrAdd(cacheKey, resultHolder.GetContext());
                }

                STRESS_LOG2(LF_INTEROP, LL_INFO100, "EOC cache insert: 0x%p == 0x%p\n", extObjCxt, resultHolder.GetContext());

                // If the returned context matches the new context it means the
                // new context was inserted or a unique instance was requested.
                if (extObjCxt == resultHolder.GetContext())
                {
                    // Update the object's SyncBlock with a handle to the context for runtime cleanup.
                    SyncBlock* syncBlock = gc.objRefMaybe->GetSyncBlock();
                    InteropSyncBlockInfo* interopInfo = syncBlock->GetInteropInfo();
                    _ASSERTE(syncBlock->IsPrecious());

                    // Since the caller has the option of providing a wrapper, it is
                    // possible the supplied wrapper already has an associated external
                    // object and an object can only be associated with one external object.
                    if (!interopInfo->TrySetExternalComObjectContext((void**)extObjCxt))
                    {
                        // Failed to set the context; one must already exist.
                        // Remove from the cache above as well.
                        ExtObjCxtCache::LockHolder lock(cache);
                        cache->Remove(resultHolder.GetContext());

                        COMPlusThrow(kNotSupportedException);
                    }

                    // Detach from the holder to avoid cleanup.
                    (void)resultHolder.DetachContext();
                    STRESS_LOG2(LF_INTEROP, LL_INFO100, "Created EOC (Unique Instance: %d): 0x%p\n", (int)uniqueInstance, extObjCxt);

                    // If this is an aggregation scenario and the identity object
                    // is a managed object wrapper, we need to call Release() to
                    // indicate this external object isn't rooted. In the event the
                    // object is passed out to native code an AddRef() must be called
                    // based on COM convention and will "fix" the count.
                    if (flags & CreateObjectFlags::CreateObjectFlags_Aggregated
                        && resultHolder.Result.ManagedObjectWrapper)
                    {
                        (void)identity->Release();
                        STRESS_LOG1(LF_INTEROP, LL_INFO100, "EOC aggregated with MOW: 0x%p\n", identity);
                    }
                }

                _ASSERTE(extObjCxt->IsActive());
            }
        }

        STRESS_LOG3(LF_INTEROP, LL_INFO1000, "EOC: 0x%p, 0x%p => 0x%p\n", extObjCxt, identity, OBJECTREFToObject(gc.objRefMaybe));

        GCPROTECT_END();

        *objRef = gc.objRefMaybe;
        RETURN (gc.objRefMaybe != NULL);
    }
}

namespace
{
    BOOL g_isGlobalPeggingOn = TRUE;
}

namespace InteropLibImports
{
    void* MemAlloc(_In_ size_t sizeInBytes, _In_ AllocScenario scenario) noexcept
    {
        CONTRACTL
        {
            NOTHROW;
            GC_NOTRIGGER;
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
            GC_NOTRIGGER;
            MODE_ANY;
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
            GCInterface::AddMemoryPressure(memoryInBytes);
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
            GCInterface::RemoveMemoryPressure(memoryInBytes);
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
            if (req == GcRequest::FullBlocking)
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

    HRESULT ReleaseExternalObjectsFromCurrentThread() noexcept
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
            // Switch to cooperative mode so the cache can be queried.
            GCX_COOP();

            struct
            {
                OBJECTREF implRef;
                OBJECTREF objsEnumRef;
            } gc;
            gc.implRef = NULL; // Use the globally registered implementation.
            gc.objsEnumRef = NULL;
            GCPROTECT_BEGIN(gc);

            // Pass the objects along to get released.
            ExtObjCxtCache* cache = ExtObjCxtCache::GetInstanceNoThrow();
            gc.objsEnumRef = cache->CreateManagedEnumerable(
                ExternalObjectContext::Flags_ReferenceTracker,
                GetCurrentCtxCookieWrapper());

            CallReleaseObjects(&gc.implRef, &gc.objsEnumRef);

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
            GC_NOTRIGGER;
            MODE_ANY;
            PRECONDITION(handle != NULL);
        }
        CONTRACTL_END;

        DestroyHandleCommon(static_cast<::OBJECTHANDLE>(handle), InstanceHandleType);
    }

    bool HasValidTarget(_In_ InteropLib::OBJECTHANDLE handle) noexcept
    {
        CONTRACTL
        {
            NOTHROW;
            GC_NOTRIGGER;
            MODE_ANY;
            PRECONDITION(handle != NULL);
        }
        CONTRACTL_END;

        ::OBJECTHANDLE objectHandle = static_cast<::OBJECTHANDLE>(handle);

        // A valid target is one that is not null.
        bool isNotNull = ObjectHandleIsNull(objectHandle) == FALSE;
        return isNotNull;
    }

    bool GetGlobalPeggingState() noexcept
    {
        CONTRACTL
        {
            NOTHROW;
            GC_NOTRIGGER;
            MODE_ANY;
        }
        CONTRACTL_END;

        return (VolatileLoad(&g_isGlobalPeggingOn) != FALSE);
    }

    void SetGlobalPeggingState(_In_ bool state) noexcept
    {
        CONTRACTL
        {
            NOTHROW;
            GC_NOTRIGGER;
            MODE_ANY;
        }
        CONTRACTL_END;

        BOOL newState = state ? TRUE : FALSE;
        VolatileStore(&g_isGlobalPeggingOn, newState);
    }

    HRESULT GetOrCreateTrackerTargetForExternal(
        _In_ IUnknown* externalComObject,
        _In_ CreateObjectFlags externalObjectFlags,
        _In_ CreateComInterfaceFlags trackerTargetFlags,
        _Outptr_ void** trackerTarget) noexcept
    {
        CONTRACTL
        {
            NOTHROW;
            MODE_PREEMPTIVE;
            PRECONDITION(externalComObject != NULL);
            PRECONDITION(trackerTarget != NULL);
        }
        CONTRACTL_END;

        HRESULT hr = S_OK;
        BEGIN_EXTERNAL_ENTRYPOINT(&hr)
        {
            // Switch to Cooperative mode since object references
            // are being manipulated.
            GCX_COOP();

            struct
            {
                OBJECTREF implRef;
                OBJECTREF wrapperMaybeRef;
                OBJECTREF objRef;
            } gc;
            gc.implRef = NULL; // Use the globally registered implementation.
            gc.wrapperMaybeRef = NULL; // No supplied wrapper here.
            gc.objRef = NULL;
            GCPROTECT_BEGIN(gc);

            // Get wrapper for external object
            bool success = TryGetOrCreateObjectForComInstanceInternal(
                gc.implRef,
                g_trackerSupportGlobalInstanceId,
                externalComObject,
                NULL,
                externalObjectFlags,
                ComWrappersScenario::TrackerSupportGlobalInstance,
                gc.wrapperMaybeRef,
                &gc.objRef);

            if (!success)
                COMPlusThrow(kArgumentNullException);

            // Get wrapper for managed object
            success = TryGetOrCreateComInterfaceForObjectInternal(
                gc.implRef,
                g_trackerSupportGlobalInstanceId,
                gc.objRef,
                trackerTargetFlags,
                ComWrappersScenario::TrackerSupportGlobalInstance,
                trackerTarget);

            if (!success)
                COMPlusThrow(kArgumentException);

            STRESS_LOG2(LF_INTEROP, LL_INFO100, "Created Target for External: 0x%p => 0x%p\n", OBJECTREFToObject(gc.objRef), *trackerTarget);
            GCPROTECT_END();
        }
        END_EXTERNAL_ENTRYPOINT;

        return hr;
    }

    TryInvokeICustomQueryInterfaceResult TryInvokeICustomQueryInterface(
        _In_ InteropLib::OBJECTHANDLE handle,
        _In_ REFGUID iid,
        _Outptr_result_maybenull_ void** obj) noexcept
    {
        CONTRACTL
        {
            NOTHROW;
            MODE_ANY;
            PRECONDITION(handle != NULL);
            PRECONDITION(obj != NULL);
        }
        CONTRACTL_END;

        *obj = NULL;

        // If this is a GC thread, then someone is trying to query for something
        // at a time when we can't run managed code.
        if (IsGCThread())
            return TryInvokeICustomQueryInterfaceResult::OnGCThread;

        // Ideally the BEGIN_EXTERNAL_ENTRYPOINT/END_EXTERNAL_ENTRYPOINT pairs
        // would be used here. However, this code path can be entered from within
        // and from outside the runtime.
        MAKE_CURRENT_THREAD_AVAILABLE_EX(GetThreadNULLOk());
        if (CURRENT_THREAD == NULL)
        {
            CURRENT_THREAD = SetupThreadNoThrow();

            // If we failed to set up a new thread, we are going to indicate
            // there was a general failure to invoke instead of failing fast.
            if (CURRENT_THREAD == NULL)
                return TryInvokeICustomQueryInterfaceResult::FailedToInvoke;
        }

        HRESULT hr;
        auto result = TryInvokeICustomQueryInterfaceResult::FailedToInvoke;
        EX_TRY_THREAD(CURRENT_THREAD)
        {
            // Switch to Cooperative mode since object references
            // are being manipulated.
            GCX_COOP();

            struct
            {
                OBJECTREF objRef;
            } gc;
            gc.objRef = NULL;
            GCPROTECT_BEGIN(gc);

            // Get the target of the external object's reference.
            ::OBJECTHANDLE objectHandle = static_cast<::OBJECTHANDLE>(handle);
            gc.objRef = ObjectFromHandle(objectHandle);

            result = (TryInvokeICustomQueryInterfaceResult)CallICustomQueryInterface(&gc.objRef, iid, obj);

            GCPROTECT_END();
        }
        EX_CATCH_HRESULT(hr);

        // Assert valid value.
        _ASSERTE(TryInvokeICustomQueryInterfaceResult::Min <= result
            && result <= TryInvokeICustomQueryInterfaceResult::Max);

        return result;
    }

    struct RuntimeCallContext
    {
        // Iterators for all known external objects.
        ExtObjCxtCache::Iterator Curr;
        ExtObjCxtCache::Iterator End;

        // Pointer to cache used to create object references.
        ExtObjCxtRefCache* RefCache;

        RuntimeCallContext(_In_ ExtObjCxtCache* cache)
            : Curr{ cache->_hashMap.Begin() }
            , End{ cache->_hashMap.End() }
            , RefCache{ cache->GetRefCache() }
        { }
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

        // Return if the target has been collected or these are the same object.
        if (target == NULL
            || source->PassiveGetSyncBlock() == target->PassiveGetSyncBlock())
        {
            return S_FALSE;
        }

        STRESS_LOG2(LF_INTEROP, LL_INFO1000, "Found reference path: 0x%p => 0x%p\n",
            OBJECTREFToObject(source),
            OBJECTREFToObject(target));
        return runtimeContext->RefCache->AddReferenceFromObjectToObject(source, target);
    }
}

extern "C" BOOL QCALLTYPE ComWrappers_TryGetOrCreateComInterfaceForObject(
    _In_ QCall::ObjectHandleOnStack comWrappersImpl,
    _In_ INT64 wrapperId,
    _In_ QCall::ObjectHandleOnStack instance,
    _In_ INT32 flags,
    _Outptr_ void** wrapper)
{
    QCALL_CONTRACT;

    bool success = false;

    BEGIN_QCALL;

    // Switch to Cooperative mode since object references
    // are being manipulated.
    {
        GCX_COOP();
        success = TryGetOrCreateComInterfaceForObjectInternal(
            ObjectToOBJECTREF(*comWrappersImpl.m_ppObject),
            wrapperId,
            ObjectToOBJECTREF(*instance.m_ppObject),
            (CreateComInterfaceFlags)flags,
            ComWrappersScenario::Instance,
            wrapper);
    }

    END_QCALL;

    return (success ? TRUE : FALSE);
}

extern "C" BOOL QCALLTYPE ComWrappers_TryGetOrCreateObjectForComInstance(
    _In_ QCall::ObjectHandleOnStack comWrappersImpl,
    _In_ INT64 wrapperId,
    _In_ void* ext,
    _In_opt_ void* innerMaybe,
    _In_ INT32 flags,
    _In_ QCall::ObjectHandleOnStack wrapperMaybe,
    _Inout_ QCall::ObjectHandleOnStack retValue)
{
    QCALL_CONTRACT;

    _ASSERTE(ext != NULL);

    bool success = false;

    BEGIN_QCALL;

    HRESULT hr;
    IUnknown* externalComObject = reinterpret_cast<IUnknown*>(ext);
    IUnknown* inner = reinterpret_cast<IUnknown*>(innerMaybe);

    // Determine the true identity and inner of the object
    SafeComHolder<IUnknown> identity;
    hr = InteropLib::Com::DetermineIdentityAndInnerForExternal(
        externalComObject,
        (CreateObjectFlags)flags,
        &identity,
        &inner);
    _ASSERTE(hr == S_OK);

    // Switch to Cooperative mode since object references
    // are being manipulated.
    {
        GCX_COOP();

        OBJECTREF newObj;
        success = TryGetOrCreateObjectForComInstanceInternal(
            ObjectToOBJECTREF(*comWrappersImpl.m_ppObject),
            wrapperId,
            identity,
            inner,
            (CreateObjectFlags)flags,
            ComWrappersScenario::Instance,
            ObjectToOBJECTREF(*wrapperMaybe.m_ppObject),
            &newObj);

        // Set the return value
        if (success)
            retValue.Set(newObj);
    }

    END_QCALL;

    return (success ? TRUE : FALSE);
}

extern "C" void QCALLTYPE ComWrappers_GetIUnknownImpl(
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

extern "C" BOOL QCALLTYPE ComWrappers_TryGetComInstance(
    _In_ QCall::ObjectHandleOnStack wrapperMaybe,
    _Out_ void** externalComObject)
{
    QCALL_CONTRACT;

    _ASSERTE(externalComObject != NULL);

    bool success = false;

    BEGIN_QCALL;

    // Switch to Cooperative mode since object references
    // are being manipulated.
    {
        GCX_COOP();

        SyncBlock* syncBlock = ObjectToOBJECTREF(*wrapperMaybe.m_ppObject)->PassiveGetSyncBlock();
        if (syncBlock != nullptr)
        {
            InteropSyncBlockInfo* interopInfo = syncBlock->GetInteropInfoNoCreate();
            if (interopInfo != nullptr)
            {
                void* contextMaybe;
                if (interopInfo->TryGetExternalComObjectContext(&contextMaybe))
                {
                    ExternalObjectContext* context = reinterpret_cast<ExternalObjectContext*>(contextMaybe);
                    IUnknown* identity = reinterpret_cast<IUnknown*>(context->Identity);
                    GCX_PREEMP();
                    success = SUCCEEDED(identity->QueryInterface(IID_IUnknown, externalComObject));
                }
            }
        }
    }

    END_QCALL;

    return (success ? TRUE : FALSE);
}

extern "C" BOOL QCALLTYPE ComWrappers_TryGetObject(
    _In_ void* wrapperMaybe,
    _Inout_ QCall::ObjectHandleOnStack instance)
{
    QCALL_CONTRACT;

    _ASSERTE(wrapperMaybe != NULL);

    bool success = false;

    BEGIN_QCALL;

    // Determine the true identity of the object
    SafeComHolder<IUnknown> identity;
    HRESULT hr = ((IUnknown*)wrapperMaybe)->QueryInterface(IID_IUnknown, &identity);
    _ASSERTE(hr == S_OK);

    InteropLib::OBJECTHANDLE handle;
    if (InteropLib::Com::GetObjectForWrapper(identity, &handle) == S_OK)
    {
        // Switch to Cooperative mode since object references
        // are being manipulated.
        GCX_COOP();

        // We have an object handle from the COM instance which is a CCW.
        ::OBJECTHANDLE objectHandle = static_cast<::OBJECTHANDLE>(handle);
        instance.Set(ObjectFromHandle(objectHandle));
        success = true;
    }

    END_QCALL;

    return (success ? TRUE : FALSE);
}

void ComWrappersNative::DestroyManagedObjectComWrapper(_In_ void* wrapper)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(wrapper != NULL);
    }
    CONTRACTL_END;

    STRESS_LOG1(LF_INTEROP, LL_INFO100, "Destroying MOW: 0x%p\n", wrapper);

    {
        GCX_PREEMP();
        InteropLib::Com::DestroyWrapperForObject(wrapper);
    }
}

void ComWrappersNative::DestroyExternalComObjectContext(_In_ void* contextRaw)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_ANY;
        PRECONDITION(contextRaw != NULL);
    }
    CONTRACTL_END;

#ifdef _DEBUG
    ExternalObjectContext* context = static_cast<ExternalObjectContext*>(contextRaw);
    _ASSERTE(!context->IsActive());
#endif

    STRESS_LOG1(LF_INTEROP, LL_INFO100, "Destroying EOC: 0x%p\n", contextRaw);

    {
        GCX_PREEMP();
        InteropLib::Com::DestroyWrapperForExternal(contextRaw);
    }
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

    bool inCache = context->IsSet(ExternalObjectContext::Flags_InCache);
    STRESS_LOG2(LF_INTEROP, LL_INFO100, "Mark Collected EOC (In Cache: %d): 0x%p\n", (int)inCache, contextRaw);

    // Verify the caller didn't ignore the cache during creation.
    if (inCache)
    {
        ExtObjCxtCache* cache = ExtObjCxtCache::GetInstanceNoThrow();
        cache->Remove(context);
    }
}

void ComWrappersNative::MarkWrapperAsComActivated(_In_ IUnknown* wrapperMaybe)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        PRECONDITION(wrapperMaybe != NULL);
    }
    CONTRACTL_END;

    {
        GCX_PREEMP();
        // The IUnknown may or may not represent a wrapper, so E_INVALIDARG is okay here.
        HRESULT hr = InteropLib::Com::MarkComActivated(wrapperMaybe);
        _ASSERTE(SUCCEEDED(hr) || hr == E_INVALIDARG);
    }
}

extern "C" void QCALLTYPE ComWrappers_SetGlobalInstanceRegisteredForMarshalling(INT64 id)
{
    QCALL_CONTRACT_NO_GC_TRANSITION;

    _ASSERTE(g_marshallingGlobalInstanceId == ComWrappersNative::InvalidWrapperId && id != ComWrappersNative::InvalidWrapperId);
    g_marshallingGlobalInstanceId = id;
}

bool GlobalComWrappersForMarshalling::IsRegisteredInstance(INT64 id)
{
    return g_marshallingGlobalInstanceId != ComWrappersNative::InvalidWrapperId
        && g_marshallingGlobalInstanceId == id;
}

bool GlobalComWrappersForMarshalling::TryGetOrCreateComInterfaceForObject(
    _In_ OBJECTREF instance,
    _Outptr_ void** wrapperRaw)
{
    CONTRACTL
    {
        THROWS;
        MODE_COOPERATIVE;
    }
    CONTRACTL_END;

    if (g_marshallingGlobalInstanceId == ComWrappersNative::InvalidWrapperId)
        return false;

    // Switch to Cooperative mode since object references
    // are being manipulated.
    {
        GCX_COOP();

        CreateComInterfaceFlags flags = CreateComInterfaceFlags::CreateComInterfaceFlags_TrackerSupport;

        // Passing NULL as the ComWrappers implementation indicates using the globally registered instance
        return TryGetOrCreateComInterfaceForObjectInternal(
            NULL,
            g_marshallingGlobalInstanceId,
            instance,
            flags,
            ComWrappersScenario::MarshallingGlobalInstance,
            wrapperRaw);
    }
}

bool GlobalComWrappersForMarshalling::TryGetOrCreateObjectForComInstance(
    _In_ IUnknown* externalComObject,
    _In_ INT32 objFromComIPFlags,
    _Out_ OBJECTREF* objRef)
{
    CONTRACTL
    {
        THROWS;
        MODE_COOPERATIVE;
    }
    CONTRACTL_END;

    if (g_marshallingGlobalInstanceId == ComWrappersNative::InvalidWrapperId)
        return false;

    // Determine the true identity of the object
    SafeComHolder<IUnknown> identity;
    {
        GCX_PREEMP();

        HRESULT hr = externalComObject->QueryInterface(IID_IUnknown, &identity);
        _ASSERTE(hr == S_OK);
    }

    // Switch to Cooperative mode since object references
    // are being manipulated.
    {
        GCX_COOP();

        // TrackerObject support and unwrapping matches the built-in semantics that the global marshalling scenario mimics.
        int flags = CreateObjectFlags::CreateObjectFlags_TrackerObject | CreateObjectFlags::CreateObjectFlags_Unwrap;
        if ((objFromComIPFlags & ObjFromComIP::UNIQUE_OBJECT) != 0)
            flags |= CreateObjectFlags::CreateObjectFlags_UniqueInstance;

        // Passing NULL as the ComWrappers implementation indicates using the globally registered instance
        return TryGetOrCreateObjectForComInstanceInternal(
            NULL /*comWrappersImpl*/,
            g_marshallingGlobalInstanceId,
            identity,
            NULL,
            (CreateObjectFlags)flags,
            ComWrappersScenario::MarshallingGlobalInstance,
            NULL /*wrapperMaybe*/,
            objRef);
    }
}

extern "C" void QCALLTYPE ComWrappers_SetGlobalInstanceRegisteredForTrackerSupport(INT64 id)
{
    QCALL_CONTRACT_NO_GC_TRANSITION;

    _ASSERTE(g_trackerSupportGlobalInstanceId == ComWrappersNative::InvalidWrapperId && id != ComWrappersNative::InvalidWrapperId);
    g_trackerSupportGlobalInstanceId = id;
}

bool GlobalComWrappersForTrackerSupport::IsRegisteredInstance(INT64 id)
{
    return g_trackerSupportGlobalInstanceId != ComWrappersNative::InvalidWrapperId
        && g_trackerSupportGlobalInstanceId == id;
}

bool GlobalComWrappersForTrackerSupport::TryGetOrCreateComInterfaceForObject(
    _In_ OBJECTREF instance,
    _Outptr_ void** wrapperRaw)
{
    CONTRACTL
    {
        THROWS;
        MODE_COOPERATIVE;
    }
    CONTRACTL_END;

    if (g_trackerSupportGlobalInstanceId == ComWrappersNative::InvalidWrapperId)
        return false;

    // Passing NULL as the ComWrappers implementation indicates using the globally registered instance
    return TryGetOrCreateComInterfaceForObjectInternal(
        NULL,
        g_trackerSupportGlobalInstanceId,
        instance,
        CreateComInterfaceFlags::CreateComInterfaceFlags_TrackerSupport,
        ComWrappersScenario::TrackerSupportGlobalInstance,
        wrapperRaw);
}

bool GlobalComWrappersForTrackerSupport::TryGetOrCreateObjectForComInstance(
    _In_ IUnknown* externalComObject,
    _Out_ OBJECTREF* objRef)
{
    CONTRACTL
    {
        THROWS;
        MODE_COOPERATIVE;
    }
    CONTRACTL_END;

    if (g_trackerSupportGlobalInstanceId == ComWrappersNative::InvalidWrapperId)
        return false;

    // Determine the true identity of the object
    SafeComHolder<IUnknown> identity;
    {
        GCX_PREEMP();

        HRESULT hr = externalComObject->QueryInterface(IID_IUnknown, &identity);
        _ASSERTE(hr == S_OK);
    }

    // Passing NULL as the ComWrappers implementation indicates using the globally registered instance
    return TryGetOrCreateObjectForComInstanceInternal(
        NULL /*comWrappersImpl*/,
        g_trackerSupportGlobalInstanceId,
        identity,
        NULL,
        CreateObjectFlags::CreateObjectFlags_TrackerObject,
        ComWrappersScenario::TrackerSupportGlobalInstance,
        NULL /*wrapperMaybe*/,
        objRef);
}

IUnknown* ComWrappersNative::GetIdentityForObject(_In_ OBJECTREF* objectPROTECTED, _In_ REFIID riid, _Out_ INT64* wrapperId, _Out_ bool* isAggregated)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
        PRECONDITION(CheckPointer(objectPROTECTED));
        PRECONDITION(CheckPointer(wrapperId));
    }
    CONTRACTL_END;

    ASSERT_PROTECTED(objectPROTECTED);

    *wrapperId = ComWrappersNative::InvalidWrapperId;

    SyncBlock* syncBlock = (*objectPROTECTED)->PassiveGetSyncBlock();
    if (syncBlock == nullptr)
    {
        return nullptr;
    }

    InteropSyncBlockInfo* interopInfo = syncBlock->GetInteropInfoNoCreate();
    if (interopInfo == nullptr)
    {
        return nullptr;
    }

    void* contextMaybe;
    if (interopInfo->TryGetExternalComObjectContext(&contextMaybe))
    {
        ExternalObjectContext* context = reinterpret_cast<ExternalObjectContext*>(contextMaybe);
        *wrapperId = context->WrapperId;
        *isAggregated = context->IsSet(ExternalObjectContext::Flags_Aggregated);

        IUnknown* identity = reinterpret_cast<IUnknown*>(context->Identity);
        GCX_PREEMP();
        IUnknown* result;
        if (SUCCEEDED(identity->QueryInterface(riid, (void**)&result)))
        {
            return result;
        }
    }
    return nullptr;
}

namespace
{
    struct CallbackContext
    {
        bool HasWrapper;
        bool IsRooted;
    };
    bool IsWrapperRootedCallback(_In_ void* mocw, _In_ void* cxtRaw)
    {
        CONTRACTL
        {
            NOTHROW;
            GC_NOTRIGGER;
            MODE_ANY;
            PRECONDITION(mocw != NULL);
            PRECONDITION(cxtRaw != NULL);
        }
        CONTRACTL_END;

        auto cxt = static_cast<CallbackContext*>(cxtRaw);
        cxt->HasWrapper = true;

        IUnknown* wrapper = static_cast<IUnknown*>(mocw);
        cxt->IsRooted = (InteropLib::Com::IsWrapperRooted(wrapper) == S_OK);

        // If we find a single rooted wrapper then the managed object
        // is considered rooted and we can stop enumerating.
        if (cxt->IsRooted)
            return false;

        return true;
    }
}

bool ComWrappersNative::HasManagedObjectComWrapper(_In_ OBJECTREF object, _Out_ bool* isRooted)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        PRECONDITION(CheckPointer(isRooted));
    }
    CONTRACTL_END;

    *isRooted = false;
    SyncBlock* syncBlock = object->PassiveGetSyncBlock();
    if (syncBlock == nullptr)
        return false;

    InteropSyncBlockInfo* interopInfo = syncBlock->GetInteropInfoNoCreate();
    if (interopInfo == nullptr)
        return false;

    CallbackContext cxt{};
    interopInfo->EnumManagedObjectComWrappers(&IsWrapperRootedCallback, &cxt);

    *isRooted = cxt.IsRooted;
    return cxt.HasWrapper;
}

void ComWrappersNative::OnFullGCStarted()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    // If no cache exists, then there is nothing to do here.
    ExtObjCxtCache* cache = ExtObjCxtCache::GetInstanceNoThrow();
    if (cache != NULL)
    {
        STRESS_LOG0(LF_INTEROP, LL_INFO10000, "Begin Reference Tracking\n");
        ExtObjCxtRefCache* refCache = cache->GetRefCache();

        // Reset the ref cache
        refCache->ResetDependentHandles();

        // Create a call context for the InteropLib.
        InteropLibImports::RuntimeCallContext cxt(cache);
        (void)InteropLib::Com::BeginExternalObjectReferenceTracking(&cxt);

        // Shrink cache and clear unused handles.
        refCache->ShrinkDependentHandles();
    }
}

void ComWrappersNative::OnFullGCFinished()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    ExtObjCxtCache* cache = ExtObjCxtCache::GetInstanceNoThrow();
    if (cache != NULL)
    {
        (void)InteropLib::Com::EndExternalObjectReferenceTracking();
        STRESS_LOG0(LF_INTEROP, LL_INFO10000, "End Reference Tracking\n");
    }
}

void ComWrappersNative::AfterRefCountedHandleCallbacks()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    ExtObjCxtCache* cache = ExtObjCxtCache::GetInstanceNoThrow();
    if (cache != NULL)
        cache->DetachNotPromotedEOCs();
}

#endif // FEATURE_COMWRAPPERS
