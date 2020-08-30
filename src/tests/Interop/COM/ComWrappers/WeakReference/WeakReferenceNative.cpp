// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <xplatform.h>
#include <ComHelpers.h>
#include <inspectable.h>
#include <WeakReference.h>

namespace
{
    struct WeakReference : public IWeakReference, public UnknownImpl
    {
        IInspectable* _reference;
        std::atomic<ULONG> _strongRefCount;

        WeakReference(IInspectable* reference, ULONG strongRefCount)
            : _reference(reference),
            _strongRefCount(strongRefCount)
        {}

        ULONG AddStrongRef()
        {
            assert(_strongRefCount > 0);
            return (++_strongRefCount);
        }

        ULONG ReleaseStrongRef()
        {
            assert(_strongRefCount > 0);
            return --_strongRefCount;
        }

        STDMETHOD(Resolve)(REFIID riid, IInspectable** ppvObject)
        {
            if (_strongRefCount > 0)
            {
                void* pObject;
                HRESULT hr = _reference->QueryInterface(riid, &pObject);
                *ppvObject = reinterpret_cast<IInspectable*>(pObject);
                return hr;
            }
            return E_NOINTERFACE;
        }

        STDMETHOD(QueryInterface)(
            /* [in] */ REFIID riid,
            /* [iid_is][out] */ _COM_Outptr_ void __RPC_FAR* __RPC_FAR* ppvObject)
        {
            return DoQueryInterface(riid, ppvObject, static_cast<IWeakReference*>(this));
        }

        DEFINE_REF_COUNTING()
    };

    struct WeakReferencableObject : public IWeakReferenceSource, public IInspectable, public UnknownImpl
    {
        ComSmartPtr<WeakReference> _weakReference;
        STDMETHOD(GetWeakReference)(_COM_Outptr_ IWeakReference** ppWeakReference)
        {
            if (!_weakReference)
            {
                ULONG refCount = UnknownImpl::GetRefCount();
                _weakReference = new WeakReference(this, refCount);
            }
            _weakReference->AddRef();
            *ppWeakReference = _weakReference;
            return S_OK;
        }

        STDMETHOD(GetRuntimeClassName)(HSTRING* pRuntimeClassName)
        {
            return E_NOTIMPL;
        }

        STDMETHOD(GetIids)(
            ULONG *iidCount,
            IID   **iids)
        {
            return E_NOTIMPL;
        }

        STDMETHOD(GetTrustLevel)(TrustLevel *trustLevel)
        {
            *trustLevel = FullTrust;
            return S_OK;
        }

        STDMETHOD(QueryInterface)(
            /* [in] */ REFIID riid,
            /* [iid_is][out] */ _COM_Outptr_ void __RPC_FAR* __RPC_FAR* ppvObject)
        {
            HRESULT hr = DoQueryInterface(riid, ppvObject, static_cast<IWeakReferenceSource*>(this), static_cast<IInspectable*>(this), static_cast<IWeakReferenceSource*>(this));
            if (SUCCEEDED(hr) && _weakReference)
            {
                _weakReference->AddStrongRef();
            }
            return hr;
        }
        STDMETHOD_(ULONG, AddRef)(void)
        {
            if (_weakReference)
            {
                return _weakReference->AddStrongRef();
            }
            return UnknownImpl::DoAddRef();
        }
        STDMETHOD_(ULONG, Release)(void)
        {
            if (_weakReference)
            {
                ULONG c = _weakReference->ReleaseStrongRef();
                if (c == 0)
                    delete this;
                return c;
            }
            return UnknownImpl::DoRelease();
        }
    };
}
extern "C" DLL_EXPORT WeakReferencableObject* STDMETHODCALLTYPE CreateWeakReferencableObject()
{
    return new WeakReferencableObject();
}
