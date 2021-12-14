// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <stdlib.h>
#include <stdio.h>
#include <string.h>
#ifdef TARGET_WINDOWS
#include <windows.h>
#define DLL_EXPORT extern "C" __declspec(dllexport)
#else
#include<errno.h>
#define DLL_EXPORT extern "C" __attribute((visibility("default")))
#endif

#if !defined(__stdcall)
#define __stdcall
#endif

DLL_EXPORT bool __stdcall IsNULL(void *a)
{
    return a == NULL;
}

#ifdef TARGET_WINDOWS
class IComInterface: public IUnknown
{
public:
    virtual HRESULT STDMETHODCALLTYPE DoWork(int param) = 0;
};
GUID IID_IComInterface = { 0x111e91ef, 0x1887, 0x4afd, { 0x81, 0xe3, 0x70, 0xcf, 0x08, 0xe7, 0x15, 0xd8 } };

class NativeComInterface: public IComInterface
{
    int _counter = 1;
    int _value = 45;
public:
    HRESULT STDMETHODCALLTYPE DoWork(int param) override
    {
        _value += param;
        return S_OK;
    }

    ULONG STDMETHODCALLTYPE AddRef() override
    {
        _counter++;
        return S_OK;
    }

    ULONG STDMETHODCALLTYPE Release() override
    {
        _counter--;
        return S_OK;
    }

    HRESULT STDMETHODCALLTYPE QueryInterface(
        REFIID InterfaceId,
        PVOID* Interface) override
    {
        if (InterfaceId == IID_IUnknown ||
            InterfaceId == IID_IComInterface)
        {
            *Interface = (IComInterface*)this;
            AddRef();
            return S_OK;
        }
        else
        {
            *Interface = nullptr;
            return E_NOINTERFACE;
        }
    }
};

IComInterface* capturedComObject;
DLL_EXPORT int __stdcall CaptureComPointer(IComInterface* pUnk)
{
    capturedComObject = pUnk;
    return capturedComObject->DoWork(11);
}

DLL_EXPORT int __stdcall RetreiveCapturedComPointer(IComInterface** ppUnk)
{
    *ppUnk = capturedComObject;
    return S_OK;
}

DLL_EXPORT int __stdcall BuildComPointer(IComInterface** ppUnk)
{
    *ppUnk = new NativeComInterface();
    return S_OK;
}

DLL_EXPORT void ReleaseComPointer()
{
    capturedComObject->Release();
}
#endif
