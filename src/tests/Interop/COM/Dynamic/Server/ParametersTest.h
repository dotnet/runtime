// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma once

#include <Contract.h>
#include "DispatchImpl.h"

class ParametersTest : public DispatchImpl, public IParametersTest
{
public:
    ParametersTest()
        : DispatchImpl(IID_IParametersTest, static_cast<IParametersTest *>(this))
    { }

public: // IParametersTest
    virtual HRESULT STDMETHODCALLTYPE Optional(
        /* [optional][in] */ VARIANT first,
        /* [optional][in] */ VARIANT second,
        /* [optional][out][in] */ VARIANT *third,
        /* [retval][out] */ SAFEARRAY **ret);
    
    virtual HRESULT STDMETHODCALLTYPE DefaultValue(
        /* [defaultvalue][in] */ VARIANT first,
        /* [defaultvalue][in] */ VARIANT second,
        /* [defaultvalue][out][in] */ VARIANT *third,
        /* [retval][out] */ SAFEARRAY **ret);
    
    virtual HRESULT STDMETHODCALLTYPE Mixed(
        /* [in] */ VARIANT first,
        /* [defaultvalue][in] */ VARIANT second,
        /* [optional][out][in] */ VARIANT *third,
        /* [retval][out] */ SAFEARRAY **ret);

    virtual HRESULT STDMETHODCALLTYPE Required(
        /* [in] */ int first,
        /* [in] */ int second,
        /* [out][in] */ int *third,
        /* [retval][out] */ SAFEARRAY **ret);
    
    virtual HRESULT STDMETHODCALLTYPE VarArgs(
        /* [in] */ SAFEARRAY *args,
        /* [retval][out] */ SAFEARRAY **ret);

public: // IDispatch
    DEFINE_DISPATCH();

public: // IUnknown
    HRESULT STDMETHODCALLTYPE QueryInterface(
        /* [in] */ REFIID riid,
        /* [iid_is][out] */ _COM_Outptr_ void __RPC_FAR *__RPC_FAR *ppvObject)
    {
        return DoQueryInterface(riid, ppvObject,
            static_cast<IDispatch *>(this),
            static_cast<IParametersTest *>(this));
    }

    DEFINE_REF_COUNTING();
};
