 // Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma once

#include <Contract.h>
#include "DispatchImpl.h"
#include <vector>

class CollectionTest : public DispatchImpl, public ICollectionTest
{
public:
    CollectionTest()
        : DispatchImpl(IID_ICollectionTest, static_cast<ICollectionTest *>(this))
        , _dispatchCollection { nullptr }
    { }

    ~CollectionTest()
    {
        Clear();
        if (_dispatchCollection != nullptr)
            _dispatchCollection->Release();
    }

public: // ICollectionTest
    HRESULT STDMETHODCALLTYPE get_Count(
        /* [retval][out] */ LONG *ret);

    HRESULT STDMETHODCALLTYPE get_Item(
        /* [in] */ ULONG index,
        /* [retval][out] */ BSTR *ret);

    HRESULT STDMETHODCALLTYPE put_Item(
        /* [in] */ ULONG index,
        /* [in] */ BSTR val);

    HRESULT STDMETHODCALLTYPE get__NewEnum(
        /* [retval][out] */ IUnknown **retval);

    HRESULT STDMETHODCALLTYPE Add(
        /* [in] */ BSTR val);

    HRESULT STDMETHODCALLTYPE Remove(
        /* [in] */ ULONG index);

    HRESULT STDMETHODCALLTYPE Clear();

    HRESULT STDMETHODCALLTYPE Array_PlusOne_InOut(
        /* [out][in] */ SAFEARRAY **ret);

    HRESULT STDMETHODCALLTYPE Array_PlusOne_Ret(
        /* [in] */ SAFEARRAY *val,
        /* [retval][out] */ SAFEARRAY **ret);

    HRESULT STDMETHODCALLTYPE ArrayVariant_PlusOne_InOut(
        /* [out][in] */ VARIANT *ret);

    HRESULT STDMETHODCALLTYPE ArrayVariant_PlusOne_Ret(
        /* [in] */ VARIANT val,
        /* [retval][out] */ VARIANT *ret);

    HRESULT STDMETHODCALLTYPE GetDispatchCollection(
        /* [retval][out] */ IDispatchCollection **ret);

public: // IDispatch
    DEFINE_DISPATCH();

public: // IUnknown
    HRESULT STDMETHODCALLTYPE QueryInterface(
        /* [in] */ REFIID riid,
        /* [iid_is][out] */ _COM_Outptr_ void __RPC_FAR *__RPC_FAR *ppvObject)
    {
        return DoQueryInterface(riid, ppvObject,
            static_cast<IDispatch *>(this),
            static_cast<ICollectionTest *>(this));
    }

    DEFINE_REF_COUNTING();

private:
    std::vector<BSTR> _strings;
    IDispatchCollection *_dispatchCollection;
};
