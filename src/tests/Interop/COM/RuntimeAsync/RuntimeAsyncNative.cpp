// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <xplatform.h>
#include <cassert>
#include <windows_version_helpers.h>
#include <Server.Contracts.h>
#include <ComHelpers.h>

// COM headers
#include <objbase.h>
#include <combaseapi.h>

extern "C" DLL_EXPORT bool STDMETHODCALLTYPE ValidateSlotLayoutForDefaultInterface(IUnknown* pUnk, int expectedIntValue, float expectedFloatValue)
{
    ComSmartPtr<IUnknown> spUnk(pUnk);

    ComSmartPtr<IClassDefaultInterfaceExposedToCom> spDefaultInterface;
    HRESULT hr = spUnk->QueryInterface(&spDefaultInterface);
    if (FAILED(hr))
    {
        printf("QueryInterface for IClassDefaultInterfaceExposedToCom failed with hr=0x%08X\n", hr);
        return false;
    }

    int intValue = 0;
    float floatValue = 0.0f;
    if (FAILED(spDefaultInterface->MyMethod(&intValue)))
    {
        printf("MyMethod failed\n");
        return false;
    }

    if (intValue != expectedIntValue)
    {
        printf("MyMethod returned intValue=%d, expected %d\n", intValue, expectedIntValue);
        return false;
    }

    if (FAILED(spDefaultInterface->MyFloatMethod(&floatValue)))
    {
        printf("MyFloatMethod failed\n");
        return false;
    }

    if (floatValue != expectedFloatValue)
    {
        printf("MyFloatMethod returned floatValue=%f, expected %f\n", floatValue, expectedFloatValue);
        return false;
    }

    return true;
}

extern "C" DLL_EXPORT bool STDMETHODCALLTYPE ValidateSlotLayoutForInterface(IUnknown* pUnk, float expectedFloatValue)
{
    ComSmartPtr<IUnknown> spUnk(pUnk);

    ComSmartPtr<IInterfaceExposedToCom> spInterface;
    HRESULT hr = spUnk->QueryInterface(&spInterface);
    if (FAILED(hr))
    {
        printf("QueryInterface for IInterfaceExposedToCom failed with hr=0x%08X\n", hr);
        return false;
    }

    float floatValue = 0.0f;
    if (FAILED(spInterface->FloatMethodOnInterface(&floatValue)))
    {
        printf("FloatMethodOnInterface failed\n");
        return false;
    }

    if (floatValue != expectedFloatValue)
    {
        printf("FloatMethodOnInterface returned floatValue=%f, expected %f\n", floatValue, expectedFloatValue);
        return false;
    }

    return true;
}
