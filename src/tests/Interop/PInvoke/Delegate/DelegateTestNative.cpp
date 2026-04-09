// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <xplatform.h>

using TestDelegate = int(STDMETHODCALLTYPE*)();

namespace
{
    const int nativeTestExpectedValue = 123456789;

    int NativeTestFunction()
    {
        return nativeTestExpectedValue;
    }
}

extern "C" DLL_EXPORT BOOL STDMETHODCALLTYPE ValidateDelegateReturnsExpected(int i, TestDelegate delegate)
{
    return i == delegate() ? TRUE : FALSE;
}

extern "C" DLL_EXPORT BOOL STDMETHODCALLTYPE ReplaceDelegate(int expectedValue, TestDelegate* pDelegate, int* pNewExpectedValue)
{
    if ((*pDelegate)() != expectedValue)
    {
        return FALSE;
    }
    *pDelegate = NativeTestFunction;
    *pNewExpectedValue = nativeTestExpectedValue;
    return TRUE;
}

extern "C" DLL_EXPORT void STDMETHODCALLTYPE GetNativeTestFunction(TestDelegate* pDelegate, int* pExpectedValue)
{
    *pDelegate = NativeTestFunction;
    *pExpectedValue = nativeTestExpectedValue;
}

extern "C" DLL_EXPORT TestDelegate STDMETHODCALLTYPE GetNativeTestFunctionReturned(int* pExpectedValue)
{
    *pExpectedValue = nativeTestExpectedValue;
    return NativeTestFunction;
}

struct CallbackWithExpectedValue
{
    int expectedValue;
    TestDelegate callback;
};

extern "C" DLL_EXPORT BOOL STDMETHODCALLTYPE ValidateCallbackWithValue(CallbackWithExpectedValue val)
{
    return val.callback() == val.expectedValue ? TRUE : FALSE;
}

extern "C" DLL_EXPORT BOOL STDMETHODCALLTYPE ValidateAndUpdateCallbackWithValue(CallbackWithExpectedValue* val)
{
    BOOL retVal = val->callback() == val->expectedValue ? TRUE : FALSE;
    *val = CallbackWithExpectedValue { nativeTestExpectedValue, NativeTestFunction };
    return retVal;
}

extern "C" DLL_EXPORT void STDMETHODCALLTYPE GetNativeCallbackAndValue(CallbackWithExpectedValue* val)
{
    *val = CallbackWithExpectedValue { nativeTestExpectedValue, NativeTestFunction };
}
