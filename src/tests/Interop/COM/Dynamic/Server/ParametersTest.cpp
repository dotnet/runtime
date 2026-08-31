// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "ParametersTest.h"

namespace
{
    HRESULT GetValue(VARIANT val, int *ret)
    {
        // VT_ERROR and DISP_E_PARAMNOTFOUND indicate an optional parameter that was not specified
        if (val.vt == VARENUM::VT_ERROR && val.scode == DISP_E_PARAMNOTFOUND)
        {
            *ret = -1;
            return S_OK;;
        }

        if (val.vt == VARENUM::VT_I4)
        {
            *ret = val.lVal;
            return S_OK;
        }

        return E_INVALIDARG;
    }

    HRESULT GetArray(VARIANT first, VARIANT second, VARIANT *third, SAFEARRAY **ret)
    {
        HRESULT hr;

        SAFEARRAYBOUND saBound { /*cElements*/ 3, /*lLbound*/ 0 };
        *ret = ::SafeArrayCreate(VT_I4, 1, &saBound);
        int *retArray = static_cast<int *>((*ret)->pvData);
        RETURN_IF_FAILED(GetValue(first, &retArray[0]));
        RETURN_IF_FAILED(GetValue(second, &retArray[1]));
        RETURN_IF_FAILED(GetValue(*third, &retArray[2]));

        return S_OK;
    }
}

HRESULT STDMETHODCALLTYPE ParametersTest::Optional(
    /* [optional][in] */ VARIANT first,
    /* [optional][in] */ VARIANT second,
    /* [optional][out][in] */ VARIANT *third,
    /* [retval][out] */ SAFEARRAY **ret)
{
    return GetArray(first, second, third, ret);
}

HRESULT STDMETHODCALLTYPE ParametersTest::DefaultValue(
    /* [defaultvalue][in] */ VARIANT first,
    /* [defaultvalue][in] */ VARIANT second,
    /* [defaultvalue][out][in] */ VARIANT *third,
    /* [retval][out] */ SAFEARRAY **ret)
{
    return GetArray(first, second, third, ret);
}

HRESULT STDMETHODCALLTYPE ParametersTest::Mixed(
    /* [in] */ VARIANT first,
    /* [defaultvalue][in] */ VARIANT second,
    /* [optional][out][in] */ VARIANT *third,
    /* [retval][out] */ SAFEARRAY **ret)
{
    return GetArray(first, second, third, ret);
}

HRESULT STDMETHODCALLTYPE ParametersTest::Required(
    /* [in] */ int first,
    /* [in] */ int second,
    /* [out][in] */ int *third,
    /* [retval][out] */ SAFEARRAY **ret)
{
    SAFEARRAYBOUND saBound{ /*cElements*/ 3, /*lLbound*/ 0 };
    *ret = ::SafeArrayCreate(VT_I4, 1, &saBound);
    int *retArray = static_cast<int *>((*ret)->pvData);
    retArray[0] = first;
    retArray[1] = second;
    retArray[2] = *third;
    return S_OK;
}

HRESULT STDMETHODCALLTYPE ParametersTest::VarArgs(
    /* [in] */ SAFEARRAY *args,
    /* [retval][out] */ SAFEARRAY **ret)
{
    HRESULT hr;

    LONG upperIndex;
    RETURN_IF_FAILED(::SafeArrayGetUBound(args, 1, &upperIndex));

    SAFEARRAYBOUND saBound { /*cElements*/ upperIndex + 1, /*lLbound*/ 0 };
    *ret = ::SafeArrayCreate(VT_I4, 1, &saBound);

    if (upperIndex < 0)
        return S_OK;

    VARTYPE type;
    RETURN_IF_FAILED(::SafeArrayGetVartype(args, &type));
    if (type != VARENUM::VT_VARIANT)
        return E_INVALIDARG;

    int *retArray = static_cast<int *>((*ret)->pvData);
    VARIANT *varArray = static_cast<VARIANT *>(args->pvData);
    for (int i = 0; i <= upperIndex; ++i)
        retArray[i] = varArray[i].vt;

    return S_OK;
}
