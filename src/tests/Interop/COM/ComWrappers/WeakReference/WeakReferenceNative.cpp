// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <xplatform.h>
#include <ComHelpers.h>
#ifdef WINDOWS
#include <inspectable.h>
#include <WeakReference.h>
#endif // WINDOWS

namespace
{
    struct WeakReference : public IWeakReference, public UnknownImpl
    {
        IInspectable* _reference;
        std::atomic<uint32_t> _strongRefCount;

        WeakReference(IInspectable* reference, uint32_t strongRefCount)
            : _reference(reference),
            _strongRefCount(strongRefCount)
        {}

        uint32_t AddStrongRef()
        {
            assert(_strongRefCount > 0);
            return (++_strongRefCount);
        }

        uint32_t ReleaseStrongRef()
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
            /* [iid_is][out] */ void ** ppvObject)
        {
#ifdef WINDOWS
            return DoQueryInterface(riid, ppvObject, static_cast<IWeakReference*>(this));
#else
            if (ppvObject == nullptr)
               return E_POINTER;

            if (riid == __uuidof(IUnknown))
            {
                *ppvObject = static_cast<IUnknown*>(this);
            }
            else
            {
                if (riid == __uuidof(IWeakReference))
                {
                    *ppvObject = static_cast<IWeakReference*>(this);
                }
                else
                {
                    *ppvObject = nullptr;
                    return E_NOINTERFACE;
                }
            }

            DoAddRef();
            return S_OK;
#endif
        }

        DEFINE_REF_COUNTING()
    };

    struct WeakReferencableObject : public IWeakReferenceSource, public IInspectable, public UnknownImpl
    {
        ComSmartPtr<WeakReference> _weakReference;
        STDMETHOD(GetWeakReference)(IWeakReference** ppWeakReference)
        {
            if (!_weakReference)
            {
                uint32_t refCount = UnknownImpl::GetRefCount();
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
            uint32_t *iidCount,
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
            /* [iid_is][out] */ void ** ppvObject)
        {
            HRESULT hr;
#ifdef WINDOWS
            hr = DoQueryInterface(riid, ppvObject, static_cast<IWeakReferenceSource*>(this), static_cast<IInspectable*>(this), static_cast<IWeakReferenceSource*>(this));
#else
            if (ppvObject == nullptr)
               return E_POINTER;

            if (riid == __uuidof(IUnknown) || riid == __uuidof(IWeakReferenceSource))
            {
                *ppvObject = static_cast<IWeakReferenceSource*>(this);
                hr = S_OK;
            }
            else if (riid == __uuidof(IInspectable))
            {
                *ppvObject = static_cast<IInspectable*>(this);
                hr = S_OK;
            }
            else
            {
                *ppvObject = nullptr;
                return E_NOINTERFACE;
            }

            DoAddRef();
#endif
            if (SUCCEEDED(hr) && _weakReference)
            {
                _weakReference->AddStrongRef();
            }
            return hr;
        }
        STDMETHOD_(uint32_t, AddRef)(void)
        {
            if (_weakReference)
            {
                return _weakReference->AddStrongRef();
            }
            return UnknownImpl::DoAddRef();
        }
        STDMETHOD_(uint32_t, Release)(void)
        {
            if (_weakReference)
            {
                uint32_t c = _weakReference->ReleaseStrongRef();
                if (c == 0)
                    delete this;
                return c;
            }
            return UnknownImpl::DoRelease();
        }
    };

    struct WeakReferenceSource : public IWeakReferenceSource, public IInspectable
    {
    private:
        IUnknown* _outerUnknown;
        ComSmartPtr<WeakReference> _weakReference;
    public:
        WeakReferenceSource(IUnknown* outerUnknown)
            :_outerUnknown(outerUnknown),
            _weakReference(new WeakReference(this, 1))
        {
        }

        STDMETHOD(GetWeakReference)(IWeakReference** ppWeakReference)
        {
            _weakReference->AddRef();
            *ppWeakReference = _weakReference;
            return S_OK;
        }

        STDMETHOD(QueryInterface)(
            /* [in] */ REFIID riid,
            /* [iid_is][out] */ void ** ppvObject)
        {
            if (riid == __uuidof(IWeakReferenceSource))
            {
                *ppvObject = static_cast<IWeakReferenceSource*>(this);
                _weakReference->AddStrongRef();
                return S_OK;
            }
            return _outerUnknown->QueryInterface(riid, ppvObject);
        }
        STDMETHOD_(uint32_t, AddRef)(void)
        {
            return _weakReference->AddStrongRef();
        }
        STDMETHOD_(uint32_t, Release)(void)
        {
            return _weakReference->ReleaseStrongRef();
        }

        STDMETHOD(GetRuntimeClassName)(HSTRING* pRuntimeClassName)
        {
            return E_NOTIMPL;
        }

        STDMETHOD(GetIids)(
            uint32_t *iidCount,
            IID   **iids)
        {
            return E_NOTIMPL;
        }

        STDMETHOD(GetTrustLevel)(TrustLevel *trustLevel)
        {
            *trustLevel = FullTrust;
            return S_OK;
        }
    };

    struct AggregatedWeakReferenceSource : IInspectable
    {
    private:
        IUnknown* _outerUnknown;
        ComSmartPtr<WeakReferenceSource> _weakReference;
    public:
        AggregatedWeakReferenceSource(IUnknown* outerUnknown)
            :_outerUnknown(outerUnknown),
            _weakReference(new WeakReferenceSource(outerUnknown))
        {
        }

        STDMETHOD(GetRuntimeClassName)(HSTRING* pRuntimeClassName)
        {
            return E_NOTIMPL;
        }

        STDMETHOD(GetIids)(
            uint32_t *iidCount,
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
            /* [iid_is][out] */ void ** ppvObject)
        {
            if (riid == __uuidof(IWeakReferenceSource))
            {
                return _weakReference->QueryInterface(riid, ppvObject);
            }
            return _outerUnknown->QueryInterface(riid, ppvObject);
        }
        STDMETHOD_(uint32_t, AddRef)(void)
        {
            return _outerUnknown->AddRef();
        }
        STDMETHOD_(uint32_t, Release)(void)
        {
            return _outerUnknown->Release();
        }
    };
}
extern "C" DLL_EXPORT WeakReferencableObject* STDMETHODCALLTYPE CreateWeakReferencableObject()
{
    return new WeakReferencableObject();
}

extern "C" DLL_EXPORT AggregatedWeakReferenceSource* STDMETHODCALLTYPE CreateAggregatedWeakReferenceObject(IUnknown* pOuter)
{
    return new AggregatedWeakReferenceSource(pOuter);
}
