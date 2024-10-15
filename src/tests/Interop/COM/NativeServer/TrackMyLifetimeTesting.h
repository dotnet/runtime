// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma once

#include "Servers.h"

class TrackMyLifetimeTesting : public UnknownImpl, public ITrackMyLifetimeTesting
{
    static std::atomic<uint32_t> _instanceCount;

    static uint32_t GetAllocatedTypes()
    {
        return _instanceCount;
    }

public:
    TrackMyLifetimeTesting()
    {
        _instanceCount++;
    }
    ~TrackMyLifetimeTesting()
    {
        _instanceCount--;
    }

public: // ITrackMyLifetimeTesting
    DEF_FUNC(GetAllocationCountCallback)(_Outptr_ void** fptr)
    {
        if (fptr == nullptr)
            return E_POINTER;

        *fptr = (void*)&GetAllocatedTypes;
        return S_OK;
    }

public: // IUnknown
    STDMETHOD(QueryInterface)(
        /* [in] */ REFIID riid,
        /* [iid_is][out] */ _COM_Outptr_ void __RPC_FAR *__RPC_FAR *ppvObject)
    {
        return DoQueryInterface(riid, ppvObject, static_cast<ITrackMyLifetimeTesting *>(this));
    }

    DEFINE_REF_COUNTING();
};

std::atomic<uint32_t> TrackMyLifetimeTesting::_instanceCount = 0;
