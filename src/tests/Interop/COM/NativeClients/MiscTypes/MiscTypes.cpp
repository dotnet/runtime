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
void ValidationByRefTests();
void ValidationClassInterfaceTests();

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
        ValidationByRefTests();
    }
    catch (HRESULT hr)
    {
        ::printf("Test Failure: 0x%08x\n", hr);
        return 101;
    }

    try
    {
        ValidationClassInterfaceTests();
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

class InterfaceImpl :
    public UnknownImpl,
    public IInterface2
{
public: // IInterface1
public: // IInterface2
public: // IUnknown
    STDMETHOD(QueryInterface)(
        /* [in] */ REFIID riid,
        /* [iid_is][out] */ _COM_Outptr_ void __RPC_FAR *__RPC_FAR *ppvObject)
    {
        return DoQueryInterface(riid, ppvObject, static_cast<IInterface1 *>(this), static_cast<IInterface2 *>(this));
    }

    DEFINE_REF_COUNTING();
};

void ValidationTests()
{
    ::printf(__FUNCTION__ "() through CoCreateInstance...\n");

    HRESULT hr;

    ComSmartPtr<IMiscTypesTesting> miscTypesTesting;
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
        V_VT(&args.Input) = VT_VOID;
        THROW_IF_FAILED(miscTypesTesting->Marshal_Variant(args.Input, &args.Result));
        THROW_FAIL_IF_FALSE(VT_EMPTY == V_VT(&args.Result));
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
    {
        VariantMarshalTest args{};
        V_VT(&args.Input) = VT_INT;
        V_INT(&args.Input) = 0x07ffffff;
        THROW_IF_FAILED(miscTypesTesting->Marshal_Variant(args.Input, &args.Result));
        THROW_FAIL_IF_FALSE(VT_I4 == V_VT(&args.Result));
        THROW_FAIL_IF_FALSE(V_I4(&args.Input) == V_I4(&args.Result));
    }
    {
        VariantMarshalTest args{};
        V_VT(&args.Input) = VT_DECIMAL;
        VarDecFromR8(123.456, &V_DECIMAL(&args.Input));
        THROW_IF_FAILED(miscTypesTesting->Marshal_Variant(args.Input, &args.Result));
        THROW_FAIL_IF_FALSE(VarDecCmp(&V_DECIMAL(&args.Input), &V_DECIMAL(&args.Result)) == VARCMP_EQ);
    }
    {
        VariantMarshalTest args{};
        V_VT(&args.Input) = VT_DATE;
        V_R8(&args.Input) = -657434.0;
        THROW_IF_FAILED(miscTypesTesting->Marshal_Variant(args.Input, &args.Result));
        THROW_FAIL_IF_FALSE(V_DATE(&args.Input) == V_DATE(&args.Result));
    }
    {
        VariantMarshalTest args{};
        V_VT(&args.Input) = VT_CY;
        VarCyFromR8(12.34, &V_CY(&args.Input));
        DECIMAL d;
        VarDecFromCy(V_CY(&args.Input), &d);
        THROW_IF_FAILED(miscTypesTesting->Marshal_Variant(args.Input, &args.Result));
        THROW_FAIL_IF_FALSE(VarDecCmp(&d, &V_DECIMAL(&args.Result)) == VARCMP_EQ);
    }
    {
        VariantMarshalTest args{};
        V_VT(&args.Input) = VT_ERROR;
        V_ERROR(&args.Input) = E_FAIL;
        THROW_IF_FAILED(miscTypesTesting->Marshal_Variant(args.Input, &args.Result));
        THROW_FAIL_IF_FALSE(VT_I4 == V_VT(&args.Result));
        THROW_FAIL_IF_FALSE(V_ERROR(&args.Input) == V_ERROR(&args.Result));
    }
    {
        VariantMarshalTest args{};
        V_VT(&args.Input) = VT_ERROR;
        V_ERROR(&args.Input) = DISP_E_PARAMNOTFOUND;
        THROW_IF_FAILED(miscTypesTesting->Marshal_Variant(args.Input, &args.Result));
        THROW_FAIL_IF_FALSE(V_VT(&args.Input) == V_VT(&args.Result));
        THROW_FAIL_IF_FALSE(V_ERROR(&args.Input) == V_ERROR(&args.Result));
    }

    ::printf("-- BYREF <=> VARIANT...\n");
    {
        VariantMarshalTest args{};
        LONG value = 0x07ffffff;
        V_VT(&args.Input) = VT_BYREF|VT_I4;
        V_I4REF(&args.Input) = &value;
        THROW_IF_FAILED(miscTypesTesting->Marshal_Variant(args.Input, &args.Result));
        THROW_FAIL_IF_FALSE(value == V_I4(&args.Result));
    }
    {
        VariantMarshalTest args{};
        V_VT(&args.Input) = VT_BYREF|VT_EMPTY;
        V_I4REF(&args.Input) = NULL;
        THROW_IF_FAILED(miscTypesTesting->Marshal_Variant(args.Input, &args.Result));
#ifdef HOST_64BIT
        THROW_FAIL_IF_FALSE(VT_UI8 == V_VT(&args.Result));
#else
        THROW_FAIL_IF_FALSE(VT_UI4 == V_VT(&args.Result));
#endif
    }
    {
        VariantMarshalTest args{};
        VARIANT nested{};
        V_VT(&nested) = VT_I4;
        V_I4(&nested) = 0x07ffffff;
        V_VT(&args.Input) = VT_BYREF|VT_VARIANT;
        V_VARIANTREF(&args.Input) = &nested;
        THROW_IF_FAILED(miscTypesTesting->Marshal_Variant(args.Input, &args.Result));
        THROW_FAIL_IF_FALSE(V_I4(&nested) == V_I4(&args.Result));
    }
    {
        VariantMarshalTest args{};
        ComSmartPtr<IUnknown> unknown;
        (void)miscTypesTesting->QueryInterface(IID_IUnknown, (void**)&unknown);
        V_VT(&args.Input) = VT_BYREF|VT_UNKNOWN;
        V_UNKNOWNREF(&args.Input) = &unknown;
        THROW_IF_FAILED(miscTypesTesting->Marshal_Variant(args.Input, &args.Result));
        THROW_FAIL_IF_FALSE(unknown == V_UNKNOWN(&args.Result));
    }

    ::printf("-- BSTR <=> VARIANT...\n");
    {
        VariantMarshalTest args{};
        V_VT(&args.Input) = VT_BSTR;
        V_BSTR(&args.Input) = ::SysAllocString(W("The quick Fox jumped over the lazy Dog."));
        THROW_IF_FAILED(miscTypesTesting->Marshal_Variant(args.Input, &args.Result));
        THROW_FAIL_IF_FALSE(CompareStringOrdinal(V_BSTR(&args.Input), -1, V_BSTR(&args.Result), -1, FALSE) == CSTR_EQUAL);
    }

    ::printf("-- Array <=> VARIANT...\n");
    {
        VariantMarshalTest args{};
        V_VT(&args.Input) = VT_ARRAY|VT_I2;
        short data[3] = { 12, 34, 56 };
        SAFEARRAYBOUND saBound;
        saBound.lLbound = 0;
        saBound.cElements = static_cast<ULONG>(sizeof(data) / sizeof(*data));
        V_ARRAY(&args.Input) = ::SafeArrayCreate(VT_I2, 1, &saBound);
        memcpy(static_cast<short*>(V_ARRAY(&args.Input)->pvData), data, sizeof(data));
        THROW_IF_FAILED(miscTypesTesting->Marshal_Variant(args.Input, &args.Result));
        THROW_FAIL_IF_FALSE((VT_ARRAY|VT_I2) == V_VT(&args.Result));
        THROW_FAIL_IF_FALSE(memcmp(V_ARRAY(&args.Result)->pvData, data, sizeof(data)) == 0);
    }

    ::printf("-- IUnknown <=> VARIANT...\n");
    {
        VariantMarshalTest args{};
        V_VT(&args.Input) = VT_UNKNOWN;
        (void)miscTypesTesting->QueryInterface(IID_IUnknown, (void**)&V_UNKNOWN(&args.Input));
        THROW_IF_FAILED(miscTypesTesting->Marshal_Variant(args.Input, &args.Result));
        THROW_FAIL_IF_FALSE(V_UNKNOWN(&args.Input) == V_UNKNOWN(&args.Result));
    }
    {
        VariantMarshalTest args{};
        V_VT(&args.Input) = VT_UNKNOWN;
        V_UNKNOWN(&args.Input) = NULL;
        THROW_IF_FAILED(miscTypesTesting->Marshal_Variant(args.Input, &args.Result));
        THROW_FAIL_IF_FALSE(VT_EMPTY == V_VT(&args.Result));
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

    ::printf("-- Unsupported types <=> VARIANT...\n");
    {
        VariantMarshalTest args{};
        V_VT(&args.Input) = VT_SAFEARRAY;
        HRESULT hr = miscTypesTesting->Marshal_Variant(args.Input, &args.Result);
        THROW_FAIL_IF_FALSE(hr == E_INVALIDARG);
    }
    {
        VariantMarshalTest args{};
        V_VT(&args.Input) = VT_HRESULT;
        HRESULT hr = miscTypesTesting->Marshal_Variant(args.Input, &args.Result);
        THROW_FAIL_IF_FALSE(hr == E_INVALIDARG);
    }
    {
        VariantMarshalTest args{};
        V_VT(&args.Input) = VT_VARIANT;
        HRESULT hr = miscTypesTesting->Marshal_Variant(args.Input, &args.Result);
        THROW_FAIL_IF_FALSE(hr == E_INVALIDARG);
    }
    {
        VariantMarshalTest args{};
        V_VT(&args.Input) = (VARENUM)0x8888;
        HRESULT hr = miscTypesTesting->Marshal_Variant(args.Input, &args.Result);
        THROW_FAIL_IF_FALSE(hr == 0x80131531); // COR_E_INVALIDOLEVARIANTTYPE
    }

    ::printf("-- Interfaces...\n");
    {
        ComSmartPtr<InterfaceImpl> iface;
        iface.Attach(new InterfaceImpl());

        ComSmartPtr<IInterface2> result;
        HRESULT hr = miscTypesTesting->Marshal_Interface(iface, &result);
        THROW_IF_FAILED(hr);
    }
}

void ValidationByRefTests()
{
    ::printf(__FUNCTION__ "() through CoCreateInstance...\n");

    HRESULT hr;

    ComSmartPtr<IMiscTypesTesting> miscTypesTesting;
    THROW_IF_FAILED(::CoCreateInstance(CLSID_MiscTypesTesting, nullptr, CLSCTX_INPROC, IID_IMiscTypesTesting, (void**)&miscTypesTesting));

    ::printf("-- Primitives <=> BYREF VARIANT...\n");
    {
        VariantMarshalTest args{};
        LONG value = 0;
        V_VT(&args.Input) = VT_I4;
        V_I4(&args.Input) = 0x07ffffff;
        V_VT(&args.Result) = VT_BYREF|VT_I4;
        V_I4REF(&args.Result) = &value;
        THROW_IF_FAILED(miscTypesTesting->Marshal_ByRefVariant(&args.Result, args.Input));
        THROW_FAIL_IF_FALSE(V_I4(&args.Input) == value);
    }
    {
        VariantMarshalTest args{};
        LONG value = 0;
        V_VT(&args.Input) = VT_I8;
        V_I8(&args.Input) = 0x07ffffff;
        V_VT(&args.Result) = VT_BYREF|VT_I4;
        V_I4REF(&args.Result) = &value;
        THROW_IF_FAILED(miscTypesTesting->Marshal_ByRefVariant(&args.Result, args.Input));
        THROW_FAIL_IF_FALSE(V_I8(&args.Input) == value);
    }
    ::printf("-- BSTR <=> BYREF VARIANT...\n");
    {
        VariantMarshalTest args{};
        BSTR expected = ::SysAllocString(W("1234"));
        V_VT(&args.Input) = VT_I4;
        V_I4(&args.Input) = 1234;
        BSTR value = ::SysAllocString(W("The quick Fox jumped over the lazy Dog."));
        V_VT(&args.Result) = VT_BYREF|VT_BSTR;
        V_BSTRREF(&args.Result) = &value;
        THROW_IF_FAILED(miscTypesTesting->Marshal_ByRefVariant(&args.Result, args.Input));
        THROW_FAIL_IF_FALSE(CompareStringOrdinal(expected, -1, value, -1, FALSE) == CSTR_EQUAL);
        ::SysFreeString(expected);
    }

    ::printf("-- System.Guid <=> BYREF VARIANT...\n");
    {
        /* 8EFAD956-B33D-46CB-90F4-45F55BA68A96 */
        const GUID expected = { 0x8EFAD956, 0xB33D, 0x46CB, { 0x90, 0xF4, 0x45, 0xF5, 0x5B, 0xA6, 0x8A, 0x96} };

        // Get a System.Guid into native
        VariantMarshalTest guidVar;
        THROW_IF_FAILED(miscTypesTesting->Marshal_Instance_Variant(W("{8EFAD956-B33D-46CB-90F4-45F55BA68A96}"), &guidVar.Input));
        THROW_FAIL_IF_FALSE(V_VT(&guidVar.Input) == VT_RECORD);
        THROW_FAIL_IF_FALSE(memcmp(V_RECORD(&guidVar.Input), &expected, sizeof(expected)) == 0);
        THROW_IF_FAILED(miscTypesTesting->Marshal_Instance_Variant(W("{00000000-0000-0000-0000-000000000000}"), &guidVar.Result));
        THROW_FAIL_IF_FALSE(V_VT(&guidVar.Result) == VT_RECORD);

        // Use the Guid as input.
        VariantMarshalTest args{};
        THROW_IF_FAILED(::VariantCopy(&args.Input, &guidVar.Input));
        THROW_IF_FAILED(::VariantCopy(&args.Result, &guidVar.Result));
        V_VT(&args.Result) = VT_BYREF|VT_RECORD;
        THROW_IF_FAILED(miscTypesTesting->Marshal_ByRefVariant(&args.Result, args.Input));
        THROW_FAIL_IF_FALSE((VT_BYREF|VT_RECORD) == V_VT(&args.Result));
        THROW_FAIL_IF_FALSE(memcmp(V_RECORD(&args.Input), V_RECORD(&args.Result), sizeof(expected)) == 0);
    }

    ::printf("-- Type mismatch <=> BYREF VARIANT...\n");
    {
        VariantMarshalTest args{};
        LONG value = 0;
        V_VT(&args.Input) = VT_NULL;
        V_VT(&args.Result) = VT_BYREF|VT_I4;
        V_I4REF(&args.Result) = &value;
        THROW_FAIL_IF_FALSE(miscTypesTesting->Marshal_ByRefVariant(&args.Result, args.Input) == 0x80004002); // COR_E_INVALIDCAST
    }
}

void ValidationClassInterfaceTests()
{
    ::printf(__FUNCTION__ "() through CoCreateInstance...\n");

    HRESULT hr;

    {
        CoreShimComActivation csact{ W("NETServer"), W("ClassInterfaceNotSetTesting") };
        ::printf("-- ClassInterfaceType not set ...\n");

        ComSmartPtr<IUnknown> pUnk;
        THROW_IF_FAILED(::CoCreateInstance(CLSID_ClassInterfaceNotSetTesting, nullptr, CLSCTX_INPROC, IID_IUnknown, (void**)&pUnk));
        ComSmartPtr<IDispatch> pDisp;
        THROW_IF_FAILED(::CoCreateInstance(CLSID_ClassInterfaceNotSetTesting, nullptr, CLSCTX_INPROC, IID_IDispatch, (void**)&pDisp));
    }
    {
        CoreShimComActivation csact{ W("NETServer"), W("ClassInterfaceNoneTesting") };
        ::printf("-- ClassInterfaceType.None ...\n");

        ComSmartPtr<IUnknown> pUnk;
        THROW_IF_FAILED(::CoCreateInstance(CLSID_ClassInterfaceNoneTesting, nullptr, CLSCTX_INPROC, IID_IUnknown, (void**)&pUnk));
        ComSmartPtr<IDispatch> pDisp;
        THROW_FAIL_IF_FALSE(E_NOINTERFACE == ::CoCreateInstance(CLSID_ClassInterfaceNoneTesting, nullptr, CLSCTX_INPROC, IID_IDispatch, (void**)&pDisp));
    }
    {
        CoreShimComActivation csact{ W("NETServer"), W("ClassInterfaceAutoDispatchTesting") };
        ::printf("-- ClassInterfaceType.AutoDispatch ...\n");

        ComSmartPtr<IUnknown> pUnk;
        THROW_IF_FAILED(::CoCreateInstance(CLSID_ClassInterfaceAutoDispatchTesting, nullptr, CLSCTX_INPROC, IID_IUnknown, (void**)&pUnk));
        ComSmartPtr<IDispatch> pDisp;
        THROW_IF_FAILED(::CoCreateInstance(CLSID_ClassInterfaceAutoDispatchTesting, nullptr, CLSCTX_INPROC, IID_IDispatch, (void**)&pDisp));
    }
    {
        CoreShimComActivation csact{ W("NETServer"), W("ClassInterfaceAutoDualTesting") };
        ::printf("-- ClassInterfaceType.AutoDual ...\n");

        ComSmartPtr<IUnknown> pUnk;
        THROW_IF_FAILED(::CoCreateInstance(CLSID_ClassInterfaceAutoDualTesting, nullptr, CLSCTX_INPROC, IID_IUnknown, (void**)&pUnk));
        ComSmartPtr<IDispatch> pDisp;
        THROW_IF_FAILED(::CoCreateInstance(CLSID_ClassInterfaceAutoDualTesting, nullptr, CLSCTX_INPROC, IID_IDispatch, (void**)&pDisp));
    }
}