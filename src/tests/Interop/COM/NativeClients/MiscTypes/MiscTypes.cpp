// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <xplatform.h>
#include <cassert>
#include <Server.Contracts.h>
#include <windows_version_helpers.h>

// COM headers
#include <objbase.h>
#include <combaseapi.h>

#define COM_CLIENT
#include <Servers.h>

#define THROW_IF_FAILED(exp) { hr = exp; if (FAILED(hr)) { ::printf("FAILURE: 0x%08x = %s\n", hr, #exp); throw hr; } }
#define THROW_FAIL_IF_FALSE(exp) { if (!(exp)) { ::printf("FALSE: %s\n", #exp); throw E_FAIL; } }

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
void ValidationTests();

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
        CoreShimComActivation csact{ W("NETServer"), W("MiscTypesTesting") };
        ValidationTests();
    }
    catch (HRESULT hr)
    {
        ::printf("Test Failure: 0x%08x\n", hr);
        return 101;
    }

    return 100;
}

struct VariantMarshalTest
{
    VARIANT Input;
    VARIANT Result;
    VariantMarshalTest()
    {
        ::VariantInit(&Input);
        ::VariantInit(&Result);
    }
    ~VariantMarshalTest()
    {
        ::VariantClear(&Input);
        ::VariantClear(&Result);
    }
};

void ValidationTests()
{
    ::printf(__FUNCTION__ "() through CoCreateInstance...\n");

    HRESULT hr;

    IMiscTypesTesting *miscTypesTesting;
    THROW_IF_FAILED(::CoCreateInstance(CLSID_MiscTypesTesting, nullptr, CLSCTX_INPROC, IID_IMiscTypesTesting, (void**)&miscTypesTesting));

    ::printf("-- Primitives <=> VARIANT...\n");
    {
        VariantMarshalTest args{};
        V_VT(&args.Input) = VT_EMPTY;
        THROW_IF_FAILED(miscTypesTesting->Marshal_Variant(args.Input, &args.Result));
        THROW_FAIL_IF_FALSE(V_VT(&args.Input) == V_VT(&args.Result));
    }
    {
        VariantMarshalTest args{};
        V_VT(&args.Input) = VT_NULL;
        THROW_IF_FAILED(miscTypesTesting->Marshal_Variant(args.Input, &args.Result));
        THROW_FAIL_IF_FALSE(V_VT(&args.Input) == V_VT(&args.Result));
    }
    {
        VariantMarshalTest args{};
        V_VT(&args.Input) = VT_I1;
        V_I1(&args.Input) = 0x0f;
        THROW_IF_FAILED(miscTypesTesting->Marshal_Variant(args.Input, &args.Result));
        THROW_FAIL_IF_FALSE(V_I1(&args.Input) == V_I1(&args.Result));
    }
    {
        VariantMarshalTest args{};
        V_VT(&args.Input) = VT_I2;
        V_I2(&args.Input) = 0x07ff;
        THROW_IF_FAILED(miscTypesTesting->Marshal_Variant(args.Input, &args.Result));
        THROW_FAIL_IF_FALSE(V_I2(&args.Input) == V_I2(&args.Result));
    }
    {
        VariantMarshalTest args{};
        V_VT(&args.Input) = VT_I4;
        V_I4(&args.Input) = 0x07ffffff;
        THROW_IF_FAILED(miscTypesTesting->Marshal_Variant(args.Input, &args.Result));
        THROW_FAIL_IF_FALSE(V_I4(&args.Input) == V_I4(&args.Result));
    }
    {
        VariantMarshalTest args{};
        V_VT(&args.Input) = VT_I8;
        V_I8(&args.Input) = 0x07ffffffffffffff;
        THROW_IF_FAILED(miscTypesTesting->Marshal_Variant(args.Input, &args.Result));
        THROW_FAIL_IF_FALSE(V_I8(&args.Input) == V_I8(&args.Result));
    }
    {
        VariantMarshalTest args{};
        V_VT(&args.Input) = VT_BOOL;
        V_BOOL(&args.Input) = VARIANT_TRUE;
        THROW_IF_FAILED(miscTypesTesting->Marshal_Variant(args.Input, &args.Result));
        THROW_FAIL_IF_FALSE(V_BOOL(&args.Input) == V_BOOL(&args.Result));
    }
    {
        VariantMarshalTest args{};
        V_VT(&args.Input) = VT_BOOL;
        V_BOOL(&args.Input) = VARIANT_FALSE;
        THROW_IF_FAILED(miscTypesTesting->Marshal_Variant(args.Input, &args.Result));
        THROW_FAIL_IF_FALSE(V_BOOL(&args.Input) == V_BOOL(&args.Result));
    }

    ::printf("-- BSTR <=> VARIANT...\n");
    {
        VariantMarshalTest args{};
        V_VT(&args.Input) = VT_BSTR;
        V_BSTR(&args.Input) = ::SysAllocString(W("The quick Fox jumped over the lazy Dog."));
        THROW_IF_FAILED(miscTypesTesting->Marshal_Variant(args.Input, &args.Result));
        THROW_FAIL_IF_FALSE(CompareStringOrdinal(V_BSTR(&args.Input), -1, V_BSTR(&args.Result), -1, FALSE) == CSTR_EQUAL);
    }

    ::printf("-- System.Guid <=> VARIANT...\n");
    {
        /* 8EFAD956-B33D-46CB-90F4-45F55BA68A96 */
        const GUID expected = { 0x8EFAD956, 0xB33D, 0x46CB, { 0x90, 0xF4, 0x45, 0xF5, 0x5B, 0xA6, 0x8A, 0x96} };

        // Get a System.Guid into native
        VariantMarshalTest guidVar;
        THROW_IF_FAILED(miscTypesTesting->Marshal_Instance_Variant(W("{8EFAD956-B33D-46CB-90F4-45F55BA68A96}"), &guidVar.Result));
        THROW_FAIL_IF_FALSE(V_VT(&guidVar.Result) == VT_RECORD);
        THROW_FAIL_IF_FALSE(memcmp(V_RECORD(&guidVar.Result), &expected, sizeof(expected)) == 0);

        // Use the Guid as input.
        VariantMarshalTest args{};
        THROW_IF_FAILED(::VariantCopy(&args.Input, &guidVar.Result));
        THROW_IF_FAILED(miscTypesTesting->Marshal_Variant(args.Input, &args.Result));
        THROW_FAIL_IF_FALSE(V_VT(&args.Input) == V_VT(&args.Result));
        THROW_FAIL_IF_FALSE(memcmp(V_RECORD(&args.Input), V_RECORD(&args.Result), sizeof(expected)) == 0);
    }
}
