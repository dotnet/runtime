// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <platformdefines.h>
#include <xplatform.h>

struct StructWithHandle
{
    HANDLE handle;
};

static_assert(sizeof(HANDLE) == sizeof(intptr_t), "To match types semantically, we want to use HANDLE and intptr_t. However, they should be the same underlying size.");

extern "C" DLL_EXPORT BOOL STDMETHODCALLTYPE SafeHandleByValue(HANDLE handle, intptr_t expectedValue)
{
    return (intptr_t)handle == expectedValue ? TRUE : FALSE;
}

extern "C" DLL_EXPORT BOOL STDMETHODCALLTYPE SafeHandleByRef(HANDLE* handle, intptr_t expectedValue, intptr_t newValue)
{
    bool inputMatches = (intptr_t)*handle == expectedValue;
    *handle = (HANDLE)newValue;
    return inputMatches ? TRUE : FALSE;
}

extern "C" DLL_EXPORT void STDMETHODCALLTYPE SafeHandleOut(HANDLE* handle, intptr_t newValue)
{
    *handle = (HANDLE)newValue;
}

extern "C" DLL_EXPORT HANDLE STDMETHODCALLTYPE SafeHandleReturn(intptr_t newValue)
{
    return (HANDLE)newValue;
}

extern "C" DLL_EXPORT HRESULT STDMETHODCALLTYPE SafeHandleReturn_Swapped(intptr_t newValue, HANDLE* handle)
{
    *handle = (HANDLE)newValue;
    return S_OK;
}

extern "C" DLL_EXPORT BOOL STDMETHODCALLTYPE StructWithSafeHandleByValue(StructWithHandle str, intptr_t expectedValue)
{
    return (intptr_t)str.handle == expectedValue ? TRUE : FALSE;
}

extern "C" DLL_EXPORT BOOL STDMETHODCALLTYPE StructWithSafeHandleByRef(StructWithHandle* str, intptr_t expectedValue, intptr_t newValue)
{
    bool inputMatches = (intptr_t)str->handle == expectedValue;
    str->handle = (HANDLE)newValue;
    return inputMatches ? TRUE : FALSE;
}

extern "C" DLL_EXPORT void STDMETHODCALLTYPE StructWithSafeHandleOut(StructWithHandle* str, intptr_t newValue)
{
    str->handle = (HANDLE)newValue;
}

extern "C" DLL_EXPORT void STDMETHODVCALLTYPE SafeHandle_Invalid(...)
{
}

extern "C" void DLL_EXPORT GetHandleAndCookie(void** pCookie, intptr_t value, HANDLE* handle)
{
    *handle = (HANDLE)value;
    *pCookie = (void*)4567; // the value here does not matter. It just needs to not be nullptr.
}

extern "C" void DLL_EXPORT GetHandleAndArray(/*out*/int16_t* arrSize, int16_t** ppActual, intptr_t value, HANDLE* handle)
{
    *arrSize = -1; // minus one is going to make this throw on unmarshal

    // Still need to provide something allocated using an expected allocator so that the marshaller can unalloc
    *ppActual = (int16_t*)CoreClrAlloc(sizeof(int16_t));

    *handle = (HANDLE)value;
}
