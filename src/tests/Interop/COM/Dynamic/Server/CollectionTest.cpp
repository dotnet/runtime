// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "CollectionTest.h"

namespace
{
    class Enumerator : public UnknownImpl, public IEnumVARIANT
    {
    public:
        Enumerator(const std::vector<BSTR> &collection)
            : _collection{ collection }
            , _index{ 0 }
        { }

        HRESULT STDMETHODCALLTYPE Next(
            ULONG celt,
            VARIANT *rgVar,
            ULONG *pCeltFetched)
        {
            for (*pCeltFetched = 0; *pCeltFetched < celt && _index < _collection.size(); ++*pCeltFetched, ++_index)
            {
                VariantClear(&(rgVar[*pCeltFetched]));
                V_VT(&rgVar[*pCeltFetched]) = VT_BSTR;
                V_BSTR(&(rgVar[*pCeltFetched])) = ::SysAllocString(_collection[_index]);
            }

            return celt == *pCeltFetched ? S_OK : S_FALSE;
        }

        HRESULT STDMETHODCALLTYPE Skip(ULONG celt)
        {
            ULONG indexMaybe = _index + celt;
            if (indexMaybe < _collection.size())
            {
                _index = indexMaybe;
                return S_OK;
            }

            _index = static_cast<ULONG>(_collection.size()) - 1;
            return S_FALSE;
        }

        HRESULT STDMETHODCALLTYPE Reset()
        {
            _index = 0;
            return S_OK;
        }

        HRESULT STDMETHODCALLTYPE Clone(IEnumVARIANT **ppEnum) override
        {
            Enumerator *clone = new Enumerator(_collection);
            clone->_index = _index;
            *ppEnum = clone;

            return S_OK;
        }

    public: // IUnknown
        HRESULT STDMETHODCALLTYPE QueryInterface(
            /* [in] */ REFIID riid,
            /* [iid_is][out] */ _COM_Outptr_ void __RPC_FAR *__RPC_FAR *ppvObject)
        {
            return DoQueryInterface(riid, ppvObject, static_cast<IEnumVARIANT *>(this));
        }

        DEFINE_REF_COUNTING();

    private:
        const std::vector<BSTR> &_collection;
        ULONG _index;
    };

    class DispatchCollection : public DispatchImpl, public IDispatchCollection
    {
    public:
        DispatchCollection()
            : DispatchImpl(IID_IDispatchCollection, static_cast<IDispatchCollection *>(this))
        { }

        ~DispatchCollection()
        {
            Clear();
        }

    public:
        HRESULT STDMETHODCALLTYPE get_Count(
            /* [retval][out] */ LONG *ret)
        {
            *ret = static_cast<LONG>(_items.size());
            return S_OK;
        }

        HRESULT STDMETHODCALLTYPE get_Item(
            /* [in] */ ULONG index,
            /* [retval][out] */ IDispatch **ret)
        {
            if (_items.empty() || index >= _items.size() || index < 0)
                return E_INVALIDARG;

            _items[index]->AddRef();
            *ret = _items[index];
            return S_OK;
        }

        HRESULT STDMETHODCALLTYPE put_Item(
            /* [in] */ ULONG index,
            /* [in] */ IDispatch *val)
        {
            if (val == nullptr)
                return E_POINTER;

            if (_items.empty() || index >= _items.size() || index < 0)
                return E_INVALIDARG;

            _items[index]->Release();
            val->AddRef();
            _items[index] = val;
            return S_OK;
        }

        HRESULT STDMETHODCALLTYPE Add(
            /* [in] */ IDispatch *val)
        {
            if (val == nullptr)
                return E_POINTER;

            val->AddRef();
            _items.push_back(val);
            return S_OK;
        }

        HRESULT STDMETHODCALLTYPE Remove(
            /* [in] */ ULONG index)
        {
            if (_items.empty() || index >= _items.size() || index < 0)
                return E_INVALIDARG;

            _items[index]->Release();
            _items.erase(_items.cbegin() + index);
            return S_OK;
        }

        HRESULT STDMETHODCALLTYPE Clear()
        {
            for (IDispatch *d : _items)
                d->Release();

            _items.clear();
            return S_OK;
        }

    public: // IDispatch
        DEFINE_DISPATCH();

    public: // IUnknown
        HRESULT STDMETHODCALLTYPE QueryInterface(
            /* [in] */ REFIID riid,
            /* [iid_is][out] */ _COM_Outptr_ void __RPC_FAR *__RPC_FAR *ppvObject)
        {
            return DoQueryInterface(riid, ppvObject,
                static_cast<IDispatch *>(this),
                static_cast<IDispatchCollection *>(this));
        }

        DEFINE_REF_COUNTING();

    private:
        std::vector<IDispatch *> _items;
    };
}

HRESULT STDMETHODCALLTYPE CollectionTest::get_Count(
    /* [retval][out] */ LONG* ret)
{
    *ret = static_cast<LONG>(_strings.size());
    return S_OK;
};

HRESULT STDMETHODCALLTYPE CollectionTest::get_Item(
    /* [in] */ ULONG index,
    /* [retval][out] */ BSTR* ret)
{
    if (_strings.empty() || index >= _strings.size() || index < 0)
        return E_INVALIDARG;

    *ret = ::SysAllocString(_strings[index]);
    return S_OK;
}

HRESULT STDMETHODCALLTYPE CollectionTest::put_Item(
    /* [in] */ ULONG index,
    /* [in] */ BSTR val)
{
    if (_strings.empty() || index >= _strings.size() || index < 0)
        return E_INVALIDARG;

    ::SysFreeString(_strings[index]);
    _strings[index] = ::SysAllocString(val);
    return S_OK;
}

HRESULT STDMETHODCALLTYPE CollectionTest::get__NewEnum(
    /* [retval][out] */ IUnknown** retval)
{
    *retval = new Enumerator(_strings);
    return S_OK;
}

HRESULT STDMETHODCALLTYPE CollectionTest::Add(
    /* [in] */ BSTR val)
{
    _strings.push_back(::SysAllocString(val));
    return S_OK;
}

HRESULT STDMETHODCALLTYPE CollectionTest::Remove(
    /* [in] */ ULONG index)
{
    if (_strings.empty() || index >= _strings.size() || index < 0)
        return E_INVALIDARG;

    ::SysFreeString(_strings[index]);
    _strings.erase(_strings.cbegin() + index);
    return S_OK;
}

HRESULT STDMETHODCALLTYPE CollectionTest::Clear()
{
    for (BSTR str : _strings)
        ::SysFreeString(str);

    _strings.clear();
    return S_OK;
}

namespace
{
    HRESULT UpdateArray(SAFEARRAY *val)
    {
        HRESULT hr;
        VARTYPE type;
        RETURN_IF_FAILED(::SafeArrayGetVartype(val, &type));
        if (type != VARENUM::VT_I4)
            return E_INVALIDARG;

        LONG upperIndex;
        RETURN_IF_FAILED(::SafeArrayGetUBound(val, 1, &upperIndex));

        int *valArray = static_cast<int *>(val->pvData);
        for (int i = 0; i <= upperIndex; ++i)
            valArray[i] += 1;

        return S_OK;
    }
}

HRESULT STDMETHODCALLTYPE CollectionTest::Array_PlusOne_InOut(
    /* [out][in] */ SAFEARRAY **ret)
{
    return UpdateArray(*ret);
}

HRESULT STDMETHODCALLTYPE CollectionTest::Array_PlusOne_Ret(
    /* [in] */ SAFEARRAY *val,
    /* [retval][out] */ SAFEARRAY **ret)
{
    HRESULT hr;
    RETURN_IF_FAILED(::SafeArrayCopy(val, ret));
    return UpdateArray(*ret);
}

HRESULT STDMETHODCALLTYPE CollectionTest::ArrayVariant_PlusOne_InOut(
    /* [out][in] */ VARIANT *ret)
{
    if (ret->vt != (VARENUM::VT_ARRAY | VARENUM::VT_BYREF | VARENUM::VT_I4))
        return E_INVALIDARG;

    return UpdateArray(*ret->pparray);
}

HRESULT STDMETHODCALLTYPE CollectionTest::ArrayVariant_PlusOne_Ret(
    /* [in] */ VARIANT val,
    /* [retval][out] */ VARIANT *ret)
{
    HRESULT hr;
    if (val.vt != (VARENUM::VT_ARRAY | VARENUM::VT_I4))
        return E_INVALIDARG;

    RETURN_IF_FAILED(::VariantCopy(ret, &val));
    return UpdateArray(ret->parray);
}

HRESULT STDMETHODCALLTYPE CollectionTest::GetDispatchCollection(
    /* [retval][out] */ IDispatchCollection **ret)
{
    if (_dispatchCollection == nullptr)
        _dispatchCollection = new DispatchCollection();

    _dispatchCollection->AddRef();
    *ret = _dispatchCollection;
    return S_OK;
}
