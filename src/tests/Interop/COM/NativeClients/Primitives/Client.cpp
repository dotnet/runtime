// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "ClientTests.h"
#include <windows_version_helpers.h>

template<COINIT TM>
struct ComInit
{
    const HRESULT Result;

    ComInit()
        : Result{ ::CoInitializeEx(nullptr, TM) }
    { }

    ~ComInit()
    {
        if (SUCCEEDED(Result))
            ::CoUninitialize();
    }
};

using ComMTA = ComInit<COINIT_MULTITHREADED>;

int __cdecl main()
{
    if (is_windows_nano() == S_OK)
    {
        ::puts("RegFree COM is not supported on Windows Nano. Auto-passing this test.\n");
        return 100;
    }
    ComMTA init;
    if (FAILED(init.Result))
        return -1;

    try
    {
        Run_NumericTests();
        Run_ArrayTests();
        Run_StringTests();
        Run_ErrorTests();
        Run_ColorTests();
    }
    catch (HRESULT hr)
    {
        ::printf("Test Failure: 0x%08x\n", hr);
        return 101;
    }

    return 100;
}
