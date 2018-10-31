// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "ClientTests.h"
#include <cstdint>
#include <limits>

namespace
{
    void MarshalByte(_In_ INumericTesting *numericTesting, _In_ byte a, _In_ byte b)
    {
        HRESULT hr;

        byte expected = a + b;
        ::printf("Byte test invariant: %d + %d = %d\n", a, b, expected);

        byte c;
        THROW_IF_FAILED(numericTesting->Add_Byte(a, b, &c));
        THROW_FAIL_IF_FALSE(expected == c);

        c = std::numeric_limits<decltype(c)>::max();
        THROW_IF_FAILED(numericTesting->Add_Byte_Ref(a, b, &c));
        THROW_FAIL_IF_FALSE(expected == c);

        c = 0;
        THROW_IF_FAILED(numericTesting->Add_Byte_Out(a, b, &c));
        THROW_FAIL_IF_FALSE(expected == c);
    }

    void MarshalShort(_In_ INumericTesting *numericTesting, _In_ int16_t a, _In_ int16_t b)
    {
        HRESULT hr;

        int16_t expected = a + b;
        ::printf("Short test invariant: %d + %d = %d\n", a, b, expected);

        int16_t c;
        THROW_IF_FAILED(numericTesting->Add_Short(a, b, &c));
        THROW_FAIL_IF_FALSE(expected == c);

        c = std::numeric_limits<decltype(c)>::max();
        THROW_IF_FAILED(numericTesting->Add_Short_Ref(a, b, &c));
        THROW_FAIL_IF_FALSE(expected == c);

        c = 0;
        THROW_IF_FAILED(numericTesting->Add_Short_Out(a, b, &c));
        THROW_FAIL_IF_FALSE(expected == c);
    }

    void MarshalUShort(_In_ INumericTesting *numericTesting, _In_ uint16_t a, _In_ uint16_t b)
    {
        HRESULT hr;

        uint16_t expected = a + b;
        ::printf("UShort test invariant: %u + %u = %u\n", a, b, expected);

        uint16_t c;
        THROW_IF_FAILED(numericTesting->Add_UShort(a, b, &c));
        THROW_FAIL_IF_FALSE(expected == c);

        c = std::numeric_limits<decltype(c)>::max();
        THROW_IF_FAILED(numericTesting->Add_UShort_Ref(a, b, &c));
        THROW_FAIL_IF_FALSE(expected == c);

        c = 0;
        THROW_IF_FAILED(numericTesting->Add_UShort_Out(a, b, &c));
        THROW_FAIL_IF_FALSE(expected == c);
    }

    void MarshalInt(_In_ INumericTesting *numericTesting, _In_ int32_t a, _In_ int32_t b)
    {
        HRESULT hr;

        int32_t expected = a + b;
        ::printf("Int test invariant: %d + %d = %d\n", a, b, expected);

        int32_t c;
        THROW_IF_FAILED(numericTesting->Add_Int(a, b, &c));
        THROW_FAIL_IF_FALSE(expected == c);

        c = std::numeric_limits<decltype(c)>::max();
        THROW_IF_FAILED(numericTesting->Add_Int_Ref(a, b, &c));
        THROW_FAIL_IF_FALSE(expected == c);

        c = 0;
        THROW_IF_FAILED(numericTesting->Add_Int_Out(a, b, &c));
        THROW_FAIL_IF_FALSE(expected == c);
    }

    void MarshalUInt(_In_ INumericTesting *numericTesting, _In_ uint32_t a, _In_ uint32_t b)
    {
        HRESULT hr;

        uint32_t expected = a + b;
        ::printf("UInt test invariant: %u + %u = %u\n", a, b, expected);

        uint32_t c;
        THROW_IF_FAILED(numericTesting->Add_UInt(a, b, &c));
        THROW_FAIL_IF_FALSE(expected == c);

        c = std::numeric_limits<decltype(c)>::max();
        THROW_IF_FAILED(numericTesting->Add_UInt_Ref(a, b, &c));
        THROW_FAIL_IF_FALSE(expected == c);

        c = 0;
        THROW_IF_FAILED(numericTesting->Add_UInt_Out(a, b, &c));
        THROW_FAIL_IF_FALSE(expected == c);
    }

    void MarshalLong(_In_ INumericTesting *numericTesting, _In_ int64_t a, _In_ int64_t b)
    {
        HRESULT hr;

        int64_t expected = a + b;
        ::printf("Long test invariant: %lld + %lld = %lld\n", a, b, expected);

        int64_t c;
        THROW_IF_FAILED(numericTesting->Add_Long(a, b, &c));
        THROW_FAIL_IF_FALSE(expected == c);

        c = std::numeric_limits<decltype(c)>::max();
        THROW_IF_FAILED(numericTesting->Add_Long_Ref(a, b, &c));
        THROW_FAIL_IF_FALSE(expected == c);

        c = 0;
        THROW_IF_FAILED(numericTesting->Add_Long_Out(a, b, &c));
        THROW_FAIL_IF_FALSE(expected == c);
    }

    void MarshalULong(_In_ INumericTesting *numericTesting, _In_ uint64_t a, _In_ uint64_t b)
    {
        HRESULT hr;

        uint64_t expected = a + b;
        ::printf("ULong test invariant: %llu + %llu = %llu\n", a, b, expected);

        uint64_t c;
        THROW_IF_FAILED(numericTesting->Add_ULong(a, b, &c));
        THROW_FAIL_IF_FALSE(expected == c);

        c = std::numeric_limits<decltype(c)>::max();
        THROW_IF_FAILED(numericTesting->Add_ULong_Ref(a, b, &c));
        THROW_FAIL_IF_FALSE(expected == c);

        c = 0;
        THROW_IF_FAILED(numericTesting->Add_ULong_Out(a, b, &c));
        THROW_FAIL_IF_FALSE(expected == c);
    }

    template<typename T>
    bool EqualByBound(_In_ T expected, _In_ T actual)
    {
        T low = expected - (T)0.0001;
        T high = expected + (T)0.0001;
        T eps = std::abs(expected - actual);
        return (eps < std::numeric_limits<T>::epsilon() || (low < actual && actual < high));
    }

    void MarshalFloat(_In_ INumericTesting *numericTesting, _In_ float a, _In_ float b)
    {
        HRESULT hr;

        float expected = a + b;
        ::printf("Float test invariant: %f + %f = %f\n", a, b, expected);

        float c;
        THROW_IF_FAILED(numericTesting->Add_Float(a, b, &c));
        THROW_FAIL_IF_FALSE(EqualByBound(expected, c));

        c = std::numeric_limits<decltype(c)>::max();
        THROW_IF_FAILED(numericTesting->Add_Float_Ref(a, b, &c));
        THROW_FAIL_IF_FALSE(EqualByBound(expected, c));

        c = 0;
        THROW_IF_FAILED(numericTesting->Add_Float_Out(a, b, &c));
        THROW_FAIL_IF_FALSE(EqualByBound(expected, c));
    }

    void MarshalDouble(_In_ INumericTesting *numericTesting, _In_ double a, _In_ double b)
    {
        HRESULT hr;

        double expected = a + b;
        ::printf("Double test invariant: %f + %f = %f\n", a, b, expected);

        double c;
        THROW_IF_FAILED(numericTesting->Add_Double(a, b, &c));
        THROW_FAIL_IF_FALSE(EqualByBound(expected, c));

        c = std::numeric_limits<decltype(c)>::max();
        THROW_IF_FAILED(numericTesting->Add_Double_Ref(a, b, &c));
        THROW_FAIL_IF_FALSE(EqualByBound(expected, c));

        c = 0;
        THROW_IF_FAILED(numericTesting->Add_Double_Out(a, b, &c));
        THROW_FAIL_IF_FALSE(EqualByBound(expected, c));
    }

    void MarshalManyInts(_In_ INumericTesting *numericTesting)
    {
        HRESULT hr;

        int expected = 1 + 2 + 3 + 4 + 5 + 6 + 7 + 8 + 9 + 10 + 11;
        ::printf("Many ints 11 test invariant: 1 + 2 + 3 + 4... + 11 = %d\n", expected);

        int result = 0;
        THROW_IF_FAILED(numericTesting->Add_ManyInts11(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, &result));
        THROW_FAIL_IF_FALSE(result == expected);

        expected = 1 + 2 + 3 + 4 + 5 + 6 + 7 + 8 + 9 + 10 + 11 + 12;
        ::printf("Many ints 12 test invariant: 1 + 2 + 3 + 4... + 11 + 12= %d\n", expected);

        result = 0;
        THROW_IF_FAILED(numericTesting->Add_ManyInts12(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, &result));
        THROW_FAIL_IF_FALSE(result == expected);
    }
}

void Run_NumericTests()
{
    HRESULT hr;

    CoreShimComActivation csact{ W("NETServer.dll"), W("NumericTesting") };

    ComSmartPtr<INumericTesting> numericTesting;
    THROW_IF_FAILED(::CoCreateInstance(CLSID_NumericTesting, nullptr, CLSCTX_INPROC, IID_INumericTesting, (void**)&numericTesting));

    int seed = 37;
    ::srand(seed);

    ::printf("Numeric RNG seed: %d\n", seed);

    int a = ::rand();
    int b = ::rand();

    MarshalByte(numericTesting, (byte)a, (byte)b);
    MarshalShort(numericTesting, (int16_t)a, (int16_t)b);
    MarshalUShort(numericTesting, (uint16_t)a, (uint16_t)b);
    MarshalInt(numericTesting, a, b);
    MarshalUInt(numericTesting, (uint32_t)a, (uint32_t)b);
    MarshalLong(numericTesting, (int64_t)a, (int64_t)b);
    MarshalULong(numericTesting, (uint64_t)a, (uint64_t)b);
    MarshalFloat(numericTesting, (float)a / 100.f, (float)b / 100.f);
    MarshalDouble(numericTesting, (double)a / 100.0, (double)b / 100.0);
    MarshalManyInts(numericTesting);
}
