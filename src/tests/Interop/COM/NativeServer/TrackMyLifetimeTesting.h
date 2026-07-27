// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma once

#include "Servers.h"

class TrackMyLifetimeTesting : public UnknownImpl, public ITrackMyLifetimeTesting, public IAgileObject
{
    static std::atomic<uint32_t> _instanceCount;

    static uint32_t GetAllocatedTypes()
    {
        return _instanceCount;
    }

private:
    const bool _isAgileInstance = false;

public:
    TrackMyLifetimeTesting()
        : TrackMyLifetimeTesting(false)
    { }
    TrackMyLifetimeTesting(bool isAgileInstance)
        : _isAgileInstance(isAgileInstance)
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

    DEF_FUNC(CreateAgileInstance)(ITrackMyLifetimeTesting** agileInstance)
    {
        if (agileInstance == nullptr)
            return E_POINTER;

        *agileInstance = new TrackMyLifetimeTesting(/*isAgileInstance*/ true);
        return S_OK;
    }

    DEF_FUNC(Method)()
    {
        return S_OK;
    }

public: // IUnknown
    STDMETHOD(QueryInterface)(
        /* [in] */ REFIID riid,
        /* [iid_is][out] */ _COM_Outptr_ void __RPC_FAR *__RPC_FAR *ppvObject)
    {
        if (_isAgileInstance)
        {
            if (riid == __uuidof(IAgileObject))
            {
                *ppvObject = static_cast<IAgileObject*>(this);
                AddRef();
                return S_OK;
            }
        }
        return DoQueryInterface(riid, ppvObject, static_cast<ITrackMyLifetimeTesting *>(this));
    }

    DEFINE_REF_COUNTING();
};

std::atomic<uint32_t> TrackMyLifetimeTesting::_instanceCount = 0;
