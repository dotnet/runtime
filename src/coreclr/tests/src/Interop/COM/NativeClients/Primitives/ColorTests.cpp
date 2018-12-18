// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "ClientTests.h"

#define RED RGB(0xFF, 0x00, 0x00)
#define GREEN RGB(0x00, 0xFF, 0x00)

namespace
{
    void VerifyColorMarshalling(IColorTesting* color)
    {
        HRESULT hr;
        BOOL match;

        THROW_IF_FAILED(color->AreColorsEqual(GREEN, GREEN, &match));

        THROW_FAIL_IF_FALSE(match);
    }

    void VerifyGetRed(IColorTesting* color)
    {
        HRESULT hr;
        OLE_COLOR red;
        
        THROW_IF_FAILED(color->GetRed(&red));
        
        THROW_FAIL_IF_FALSE(red == RED);
    }
}

void Run_ColorTests()
{
    HRESULT hr;

    CoreShimComActivation csact{ W("NETServer.dll"), W("ColorTesting") };

    ComSmartPtr<IColorTesting> color;
    THROW_IF_FAILED(::CoCreateInstance(CLSID_ColorTesting, nullptr, CLSCTX_INPROC, IID_IColorTesting, (void**)&color));

    VerifyColorMarshalling(color);
    VerifyGetRed(color);
}
