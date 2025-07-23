// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//
// Interface between the VM and Interop library.
//

#ifndef _INTEROPLIBINTERFACE_COMWRAPPERS_H_
#define _INTEROPLIBINTERFACE_COMWRAPPERS_H_

#include <interoplibabi.h>

// Native calls for the managed ComWrappers API
class ComWrappersNative
{
public: // COM activation
    static void MarkWrapperAsComActivated(_In_ IUnknown* wrapperMaybe);

public: // Unwrapping support
    static bool IsManagedObjectComWrapper(_In_ OBJECTREF managedObjectWrapperHolderRef, _Out_ bool* pIsRooted);

public: // GC interaction
    static void OnFullGCStarted();
    static void OnFullGCFinished();
    static void OnAfterGCScanRoots();
};

// Native QCalls for the abstract ComWrappers managed type.
extern "C" void QCALLTYPE ComWrappers_GetIUnknownImpl(
    _Out_ void** fpQueryInterface,
    _Out_ void** fpAddRef,
    _Out_ void** fpRelease);

extern "C" void* QCALLTYPE ComWrappers_GetUntrackedAddRefRelease();

extern "C" void* QCALLTYPE ComWrappers_AllocateRefCountedHandle(_In_ QCall::ObjectHandleOnStack obj);

extern "C" void const* QCALLTYPE ComWrappers_GetIReferenceTrackerTargetVftbl();

extern "C" void const* QCALLTYPE ComWrappers_GetTaggedImpl();

extern "C" void QCALLTYPE ComWrappers_RegisterIsRootedCallback();

extern "C" CLR_BOOL QCALLTYPE TrackerObjectManager_HasReferenceTrackerManager();

extern "C" CLR_BOOL QCALLTYPE TrackerObjectManager_TryRegisterReferenceTrackerManager(_In_ void* manager);

extern "C" void QCALLTYPE TrackerObjectManager_RegisterNativeObjectWrapperCache(_In_ QCall::ObjectHandleOnStack cache);

extern "C" CLR_BOOL QCALLTYPE TrackerObjectManager_IsGlobalPeggingEnabled();

class GlobalComWrappersForMarshalling
{
public:
    static bool TryGetOrCreateComInterfaceForObject(
        _In_ OBJECTREF instance,
        _Outptr_ void** wrapperRaw);

    static bool TryGetOrCreateObjectForComInstance(
        _In_ IUnknown* externalComObject,
        _In_ INT32 objFromComIPFlags,
        _Out_ OBJECTREF* objRef);
};

// Define "manually managed" definitions of the ComWrappers types
// that are used in diagnostics and during GC.
class GCHandleSetObject;
class GCHandleSetEntryObject;

class ManagedObjectWrapperHolderObject : public Object
{
    friend class CoreLibBinder;
    friend class ClrDataAccess;
private:
    OBJECTREF _releaser;
public:
    OBJECTREF _wrappedObject;
    DPTR(InteropLib::ABI::ManagedObjectWrapperLayout) _wrapper;
};

class NativeObjectWrapperObject : public Object
{
    friend class CoreLibBinder;
    OBJECTREF _comWrappers;
    TADDR _externalComObject;
    TADDR _inner;
    CLR_BOOL _aggregatedManagedObjectWrapper;
    CLR_BOOL _uniqueInstance;
    OBJECTHANDLE _proxyHandle;
    OBJECTHANDLE _proxyHandleTrackingResurrection;
public:
    OBJECTHANDLE GetProxyHandle() const
    {
        return _proxyHandle;
    }

    TADDR GetExternalComObject() const
    {
        return _externalComObject;
    }
};

class ReferenceTrackerNativeObjectWrapperObject final : public NativeObjectWrapperObject
{
    friend class CoreLibBinder;
    TADDR _trackerObject;
    TADDR _contextToken;
    int _trackerObjectDisconnected;
    CLR_BOOL _releaseTrackerObject;
    OBJECTHANDLE _nativeObjectWrapperWeakHandle;
public:
    TADDR GetTrackerObject() const
    {
        return (_trackerObject == (TADDR)nullptr || _trackerObjectDisconnected == 1) ? (TADDR)nullptr : _trackerObject;
    }
};

#ifdef USE_CHECKED_OBJECTREFS
using MOWHOLDERREF = REF<ManagedObjectWrapperHolderObject>;
using NATIVEOBJECTWRAPPERREF = REF<NativeObjectWrapperObject>;
using REFTRACKEROBJECTWRAPPERREF = REF<ReferenceTrackerNativeObjectWrapperObject>;
using HANDLESETENTRYREF = REF<GCHandleSetEntryObject>;
using HANDLESETREF = REF<GCHandleSetObject>;
#else
using MOWHOLDERREF = DPTR(ManagedObjectWrapperHolderObject);
using NATIVEOBJECTWRAPPERREF = DPTR(NativeObjectWrapperObject);
using REFTRACKEROBJECTWRAPPERREF = DPTR(ReferenceTrackerNativeObjectWrapperObject);
using HANDLESETENTRYREF = DPTR(GCHandleSetEntryObject);
using HANDLESETREF = DPTR(GCHandleSetObject);
#endif

class GCHandleSetEntryObject final : public Object
{
    friend class CoreLibBinder;
    public:
    HANDLESETENTRYREF _next;
    OBJECTHANDLE _value;
};

class GCHandleSetObject final : public Object
{
    friend class CoreLibBinder;
private:
    PTRARRAYREF _buckets;

public:
    class Iterator final
    {
        PTRARRAYREF _buckets;
        HANDLESETENTRYREF _currentEntry;
        int32_t _currentIndex;
    public:
        Iterator(HANDLESETREF obj)
            : _buckets(obj->_buckets)
            , _currentEntry(nullptr)
            , _currentIndex(-1)
        {
            LIMITED_METHOD_CONTRACT;
        }

        OBJECTHANDLE Current() const;

        bool MoveNext();
    };
};

#endif // _INTEROPLIBINTERFACE_COMWRAPPERS_H_
