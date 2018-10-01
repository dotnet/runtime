// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include <xplatform.h>
#include <cassert>
#include <Server.Contracts.h>

#define COM_CLIENT
#include <Servers.h>

#define THROW_IF_FAILED(exp) { hr = exp; if (FAILED(hr)) { ::printf("FAILURE: 0x%08x = %s\n", hr, #exp); throw hr; } }
#define THROW_FAIL_IF_FALSE(exp) { if (!(exp)) { ::printf("FALSE: %s\n", #exp); throw E_FAIL; } }

template<typename T>
struct ComSmartPtr
{
    ComSmartPtr()
        : p{}
    { }

    ComSmartPtr(_In_ const ComSmartPtr &) = delete;
    ComSmartPtr(_Inout_ ComSmartPtr &&) = delete;

    ComSmartPtr& operator=(_In_ const ComSmartPtr &) = delete;
    ComSmartPtr& operator=(_Inout_ ComSmartPtr &&) = delete;

    ~ComSmartPtr()
    {
        if (p != nullptr)
            p->Release();
    }

    operator T*()
    {
        return p;
    }

    T** operator&()
    {
        return &p;
    }

    T* operator->()
    {
        return p;
    }

    void Attach(_In_opt_ T *t)
    {
        if (p != nullptr)
            p->Release();

        p = t;
    }

    T *Detach()
    {
        T *tmp = p;
        p = nullptr;
        return tmp;
    }

    T *p;
};

void Run_NumericTests();
void Run_ArrayTests();
void Run_StringTests();
void Run_ErrorTests();
