// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#pragma once

#include "Servers.h"

#define RED RGB(0xFF, 0x00, 0x00)

class ColorTesting : public UnknownImpl, public IColorTesting
{
public: // IColorTesting
    DEF_FUNC(AreColorsEqual)(
        _In_ OLE_COLOR managed,
        _In_ OLE_COLOR native,
        _Out_ BOOL* areEqual
    )
    {
        *areEqual = (managed == native ? TRUE : FALSE);
        return S_OK;
    }

    DEF_FUNC(GetRed)(
        _Out_ OLE_COLOR* color
    )
    {
        *color = RED;
        return S_OK;
    }

public: // IUnknown
    STDMETHOD(QueryInterface)(
        /* [in] */ REFIID riid,
        /* [iid_is][out] */ _COM_Outptr_ void __RPC_FAR *__RPC_FAR *ppvObject)
    {
        return DoQueryInterface(riid, ppvObject, static_cast<IColorTesting *>(this));
    }

    DEFINE_REF_COUNTING();
};
