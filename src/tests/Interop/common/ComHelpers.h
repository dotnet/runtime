// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma once
#ifdef WINDOWS
#include <Windows.h>
#include <comdef.h>
#include <exception>
#include <type_traits>
#endif // WINDOWS
#include <atomic>
#include <cassert>

// Common macro for working in COM
#define RETURN_IF_FAILED(exp) { hr = exp; if (FAILED(hr)) { return hr; } }

#ifdef WINDOWS
namespace Internal
{
    template<typename I>
    HRESULT __QueryInterfaceImpl(
        /* [in] */ REFIID riid,
        /* [iid_is][out] */ _COM_Outptr_ void __RPC_FAR *__RPC_FAR *ppvObject,
        /* [in] */ I obj)
    {
        if (riid == __uuidof(I))
        {
            *ppvObject = static_cast<I>(obj);
        }
        else
        {
            *ppvObject = nullptr;
            return E_NOINTERFACE;
        }

        return S_OK;
    }

    template<typename I1, typename ...IR>
    HRESULT __QueryInterfaceImpl(
        /* [in] */ REFIID riid,
        /* [iid_is][out] */ _COM_Outptr_ void __RPC_FAR *__RPC_FAR *ppvObject,
        /* [in] */ I1 i1,
        /* [in] */ IR... remain)
    {
        if (riid == __uuidof(I1))
        {
            *ppvObject = static_cast<I1>(i1);
            return S_OK;
        }

        return __QueryInterfaceImpl(riid, ppvObject, remain...);
    }
}
#endif // WINDOWS

// Implementation of IUnknown operations
class UnknownImpl
{
public:
    UnknownImpl() : _refCount{ 1 } {};
    virtual ~UnknownImpl() = default;

    UnknownImpl(const UnknownImpl&) = delete;
    UnknownImpl& operator=(const UnknownImpl&) = delete;

    UnknownImpl(UnknownImpl&&) = delete;
    UnknownImpl& operator=(UnknownImpl&&) = delete;

    template<typename I1, typename ...IR>
    HRESULT DoQueryInterface(
        /* [in] */ REFIID riid,
        /* [iid_is][out] */ void **ppvObject,
        /* [in] */ I1 i1,
        /* [in] */ IR... remain)
    {
        if (ppvObject == nullptr)
            return E_POINTER;

        if (riid == __uuidof(IUnknown))
        {
            *ppvObject = static_cast<IUnknown *>(i1);
        }
        else
        {
#ifdef WINDOWS
            // Internal::__QueryInterfaceImpl available only for Windows due to __uuidof(T) availability
            HRESULT hr = Internal::__QueryInterfaceImpl(riid, ppvObject, i1, remain...);
            if (hr != S_OK)
                return hr;
#else
            *ppvObject = nullptr;
            return E_NOTIMPL;
#endif // WINDOWS
        }

        DoAddRef();
        return S_OK;
    }

    uint32_t DoAddRef()
    {
        assert(_refCount > 0);
        return (++_refCount);
    }

    uint32_t DoRelease()
    {
        assert(_refCount > 0);
        uint32_t c = (--_refCount);
        if (c == 0)
            delete this;
        return c;
    }

protected:
    uint32_t GetRefCount()
    {
        return _refCount;
    }

private:
    std::atomic<uint32_t> _refCount;
};

// Macro to use for defining ref counting impls
#define DEFINE_REF_COUNTING() \
    STDMETHOD_(ULONG, AddRef)(void) { return UnknownImpl::DoAddRef(); } \
    STDMETHOD_(ULONG, Release)(void) { return UnknownImpl::DoRelease(); }

#ifdef WINDOWS
// Templated class factory
template<typename T>
class ClassFactoryBasic : public UnknownImpl, public IClassFactory
{
public: // static
    static HRESULT Create(_In_ REFIID riid, _Outptr_ LPVOID FAR* ppv)
    {
        try
        {
            auto cf = new ClassFactoryBasic();
            HRESULT hr = cf->QueryInterface(riid, ppv);
            cf->Release();
            return hr;
        }
        catch (const std::bad_alloc&)
        {
            return E_OUTOFMEMORY;
        }
    }

public: // IClassFactory
    STDMETHOD(CreateInstance)(
        _In_opt_  IUnknown *pUnkOuter,
        _In_  REFIID riid,
        _COM_Outptr_  void **ppvObject)
    {
        if (pUnkOuter != nullptr)
            return CLASS_E_NOAGGREGATION;

        try
        {
            auto ti = new T();
            HRESULT hr = ti->QueryInterface(riid, ppvObject);
            ti->Release();
            return hr;
        }
        catch (const std::bad_alloc&)
        {
            return E_OUTOFMEMORY;
        }
    }

    STDMETHOD(LockServer)(/* [in] */ BOOL fLock)
    {
        assert(false && "Not impl");
        return E_NOTIMPL;
    }

public: // IUnknown
    STDMETHOD(QueryInterface)(
        /* [in] */ REFIID riid,
        /* [iid_is][out] */ _COM_Outptr_ void __RPC_FAR *__RPC_FAR *ppvObject)
    {
        return DoQueryInterface(riid, ppvObject, static_cast<IClassFactory *>(this));
    }

    DEFINE_REF_COUNTING();
};

// Templated class factory for aggregation
template<typename T>
class ClassFactoryAggregate : public UnknownImpl, public IClassFactory
{
public: // static
    static HRESULT Create(_In_ REFIID riid, _Outptr_ LPVOID FAR* ppv)
    {
        try
        {
            auto cf = new ClassFactoryAggregate();
            HRESULT hr = cf->QueryInterface(riid, ppv);
            cf->Release();
            return hr;
        }
        catch (const std::bad_alloc&)
        {
            return E_OUTOFMEMORY;
        }
    }

public: // IClassFactory
    STDMETHOD(CreateInstance)(
        _In_opt_  IUnknown *pUnkOuter,
        _In_  REFIID riid,
        _COM_Outptr_  void **ppvObject)
    {
        if (pUnkOuter != nullptr && riid != IID_IUnknown)
            return CLASS_E_NOAGGREGATION;

        try
        {
            auto ti = new T(pUnkOuter);
            HRESULT hr = ti->QueryInterface(riid, ppvObject);
            ti->Release();
            return hr;
        }
        catch (const std::bad_alloc&)
        {
            return E_OUTOFMEMORY;
        }
    }

    STDMETHOD(LockServer)(/* [in] */ BOOL fLock)
    {
        assert(false && "Not impl");
        return E_NOTIMPL;
    }

public: // IUnknown
    STDMETHOD(QueryInterface)(
        /* [in] */ REFIID riid,
        /* [iid_is][out] */ _COM_Outptr_ void __RPC_FAR *__RPC_FAR *ppvObject)
    {
        return DoQueryInterface(riid, ppvObject, static_cast<IClassFactory *>(this));
    }

    DEFINE_REF_COUNTING();
};

// Templated class factory
// Supplied type must have the following properties to use this template:
//  1) Have a static method with the following signature:
//    - HRESULT RequestLicKey(BSTR *key);
//  2) Have a constructor that takes an optional BSTR value as the key
template<typename T>
class ClassFactoryLicense : public UnknownImpl, public IClassFactory2
{
public: // static
    static HRESULT Create(_In_ REFIID riid, _Outptr_ LPVOID FAR* ppv)
    {
        try
        {
            auto cf = new ClassFactoryLicense();
            HRESULT hr = cf->QueryInterface(riid, ppv);
            cf->Release();
            return hr;
        }
        catch (const std::bad_alloc&)
        {
            return E_OUTOFMEMORY;
        }
    }

public: // IClassFactory
    STDMETHOD(CreateInstance)(
        _In_opt_  IUnknown *pUnkOuter,
        _In_  REFIID riid,
        _COM_Outptr_  void **ppvObject)
    {
        return CreateInstanceLic(pUnkOuter, nullptr, riid, nullptr, ppvObject);
    }

    STDMETHOD(LockServer)(/* [in] */ BOOL fLock)
    {
        assert(false && "Not impl");
        return E_NOTIMPL;
    }

public: // IClassFactory2
    STDMETHOD(GetLicInfo)(
        /* [out][in] */ __RPC__inout LICINFO *pLicInfo)
    {
        // The CLR does not call this function and as such,
        // returns an error. Note that this is explicitly illegal
        // in a proper implementation of IClassFactory2.
        return E_UNEXPECTED;
    }

    STDMETHOD(RequestLicKey)(
        /* [in] */ DWORD dwReserved,
        /* [out] */ __RPC__deref_out_opt BSTR *pBstrKey)
    {
        if (dwReserved != 0)
            return E_UNEXPECTED;

        return T::RequestLicKey(pBstrKey);
    }

    STDMETHOD(CreateInstanceLic)(
        /* [annotation][in] */ _In_opt_  IUnknown *pUnkOuter,
        /* [annotation][in] */ _Reserved_  IUnknown *pUnkReserved,
        /* [annotation][in] */ __RPC__in  REFIID riid,
        /* [annotation][in] */ __RPC__in  BSTR bstrKey,
        /* [annotation][iid_is][out] */ __RPC__deref_out_opt  PVOID *ppvObj)
    {
        if (pUnkOuter != nullptr)
            return CLASS_E_NOAGGREGATION;

        if (pUnkReserved != nullptr)
            return E_UNEXPECTED;

        try
        {
            auto ti = new T(bstrKey);
            HRESULT hr = ti->QueryInterface(riid, ppvObj);
            ti->Release();
            return hr;
        }
        catch (HRESULT hr)
        {
            return hr;
        }
        catch (const std::bad_alloc&)
        {
            return E_OUTOFMEMORY;
        }
    }

public: // IUnknown
    STDMETHOD(QueryInterface)(
        /* [in] */ REFIID riid,
        /* [iid_is][out] */ _COM_Outptr_ void __RPC_FAR *__RPC_FAR *ppvObject)
    {
        return DoQueryInterface(riid, ppvObject, static_cast<IClassFactory *>(this), static_cast<IClassFactory2 *>(this));
    }

    DEFINE_REF_COUNTING();
};
#endif // WINDOWS

template<typename T>
struct ComSmartPtr
{
    T* p;

    ComSmartPtr()
        : p{ nullptr }
    { }

    ComSmartPtr(_In_ T* t)
        : p{ t }
    {
        if (p != nullptr)
            (void)p->AddRef();
    }

    ComSmartPtr(_In_ const ComSmartPtr&) = delete;

    ComSmartPtr(_Inout_ ComSmartPtr&& other)
        : p{ other.Detach() }
    { }

    ~ComSmartPtr()
    {
        Release();
    }

    ComSmartPtr& operator=(_In_ const ComSmartPtr&) = delete;

    ComSmartPtr& operator=(_Inout_ ComSmartPtr&& other)
    {
        Attach(other.Detach());
        return (*this);
    }

    operator T*()
    {
        return p;
    }

    T** operator&()
    {
        return &p;
    }

    T* operator->()
    {
        return p;
    }

    void Attach(_In_opt_ T* t) noexcept
    {
        Release();
        p = t;
    }

    T* Detach() noexcept
    {
        T* tmp = p;
        p = nullptr;
        return tmp;
    }

    void Release() noexcept
    {
        if (p != nullptr)
        {
            (void)p->Release();
            p = nullptr;
        }
    }
};
