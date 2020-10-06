// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <stdio.h>
#include <xplatform.h>

typedef BOOL(__stdcall *HandleCallback)(void*);

extern "C" DLL_EXPORT size_t __stdcall In(void* handle, HandleCallback handleCallback)
{
    if (handleCallback != nullptr && !handleCallback(handle))
    {
        return (size_t)(-1);
    }

    return reinterpret_cast<size_t>(handle);
}

extern "C" DLL_EXPORT void* __stdcall Ret(void* handleValue)
{
    return handleValue;
}

extern "C" DLL_EXPORT void __stdcall Out(void* handleValue, void** pHandle)
{
    if (pHandle == nullptr)
    {
        return;
    }

    *pHandle = handleValue;
}

extern "C" DLL_EXPORT size_t __stdcall Ref(void** pHandle, HandleCallback handleCallback)
{
    if (handleCallback != nullptr && !handleCallback(*pHandle))
    {
        return (size_t)(-1);
    }

    return reinterpret_cast<size_t>(*pHandle);
}

extern "C" DLL_EXPORT size_t __stdcall RefModify(void* handleValue, void** pHandle, HandleCallback handleCallback)
{
    if (handleCallback != nullptr && !handleCallback(*pHandle))
    {
        return (size_t)(-1);
    }

    void* originalHandle = *pHandle;

    *pHandle = handleValue;

    return reinterpret_cast<size_t>(originalHandle);
}

typedef void(__stdcall *InCallback)(void*);

extern "C" DLL_EXPORT void __stdcall InvokeInCallback(InCallback callback, void* handle)
{
    callback(handle);
}

typedef void(__stdcall *RefCallback)(void**);

extern "C" DLL_EXPORT void __stdcall InvokeRefCallback(RefCallback callback, void** pHandle)
{
    callback(pHandle);
}

typedef void*(__stdcall *RetCallback)();

extern "C" DLL_EXPORT void* __stdcall InvokeRetCallback(RetCallback callback)
{
    return callback();
}
