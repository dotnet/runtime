// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "ClientTests.h"
#include <stdio.h>
#include <cmath>
#include <numeric>
#include <vector>

namespace
{
    bool EqualByBound(double expected, double actual)
    {
        double low = expected - 0.00001;
        double high = expected + 0.00001;
        double eps = std::abs(expected - actual);
        bool isEqual = eps < std::numeric_limits<double>::epsilon() || (low < actual && actual < high);
        return isEqual;
    }

    template<typename T>
    std::vector<T> Convert(_In_ const std::vector<int> &in)
    {
        std::vector<T> out;
        for (auto i : in)
            out.push_back((T)i);
        return out;
    }

    template<typename T>
    VARTYPE ToSafeArrayType();
    template<>
    VARTYPE ToSafeArrayType<byte>() { return VT_UI1; }
    template<>
    VARTYPE ToSafeArrayType<int16_t>() { return VT_I2; }
    template<>
    VARTYPE ToSafeArrayType<uint16_t>() { return VT_UI2; }
    template<>
    VARTYPE ToSafeArrayType<int32_t>() { return VT_I4; }
    template<>
    VARTYPE ToSafeArrayType<uint32_t>() { return VT_UI4; }
    template<>
    VARTYPE ToSafeArrayType<int64_t>() { return VT_I8; }
    template<>
    VARTYPE ToSafeArrayType<uint64_t>() { return VT_UI8; }
    template<>
    VARTYPE ToSafeArrayType<float>() { return VT_R4; }
    template<>
    VARTYPE ToSafeArrayType<double>() { return VT_R8; }

    template<typename T>
    class SafeArraySmartPtr
    {
    public:
        SafeArraySmartPtr(_In_ const std::vector<T> &in)
            : _safeArray{}
            , _elementCount{ static_cast<int>(in.size()) }
        {
            SAFEARRAYBOUND saBound;
            saBound.lLbound = 0;
            saBound.cElements = static_cast<ULONG>(in.size());

            _safeArray = ::SafeArrayCreate(ToSafeArrayType<T>(), 1, &saBound);
            assert(_safeArray != nullptr);

            std::memcpy(static_cast<T*>(_safeArray->pvData), in.data(), sizeof(T) * in.size());
        }

        ~SafeArraySmartPtr()
        {
            ::SafeArrayDestroy(_safeArray);
        }

        int Length() const
        {
            return _elementCount;
        }

        operator SAFEARRAY *()
        {
            return _safeArray;
        }

    private:
        int _elementCount;
        SAFEARRAY *_safeArray;
    };

    void ByteArray(_In_ IArrayTesting *arrayTesting, _In_ const std::vector<int> &baseData, _In_ double expectedMean)
    {
        HRESULT hr;
        auto data = Convert<byte>(baseData);

        ::printf("Byte[] marshalling\n");

        double actual;
        THROW_IF_FAILED(arrayTesting->Mean_Byte_LP_PreLen(static_cast<int>(baseData.size()), data.data(), &actual));
        THROW_FAIL_IF_FALSE(EqualByBound(expectedMean, actual));

        actual = 0.0;
        THROW_IF_FAILED(arrayTesting->Mean_Byte_LP_PostLen(data.data(), static_cast<int>(baseData.size()), &actual));
        THROW_FAIL_IF_FALSE(EqualByBound(expectedMean, actual));

        actual = 0.0;
        int len;
        SafeArraySmartPtr<byte> saData{ data };
        THROW_IF_FAILED(arrayTesting->Mean_Byte_SafeArray_OutLen(saData, &len, &actual));
        THROW_FAIL_IF_FALSE(EqualByBound(expectedMean, actual));
        THROW_FAIL_IF_FALSE(len == saData.Length());
    }

    void ShortArray(_In_ IArrayTesting *arrayTesting, _In_ const std::vector<int> &baseData, _In_ double expectedMean)
    {
        HRESULT hr;
        auto data = Convert<int16_t>(baseData);

        ::printf("Short[] marshalling\n");

        double actual;
        THROW_IF_FAILED(arrayTesting->Mean_Short_LP_PreLen(static_cast<int>(baseData.size()), data.data(), &actual));
        THROW_FAIL_IF_FALSE(EqualByBound(expectedMean, actual));

        actual = 0.0;
        THROW_IF_FAILED(arrayTesting->Mean_Short_LP_PostLen(data.data(), static_cast<int>(baseData.size()), &actual));
        THROW_FAIL_IF_FALSE(EqualByBound(expectedMean, actual));

        actual = 0.0;
        int len;
        SafeArraySmartPtr<int16_t> saData{ data };
        THROW_IF_FAILED(arrayTesting->Mean_Short_SafeArray_OutLen(saData, &len, &actual));
        THROW_FAIL_IF_FALSE(EqualByBound(expectedMean, actual));
        THROW_FAIL_IF_FALSE(len == saData.Length());
    }

    void UShortArray(_In_ IArrayTesting *arrayTesting, _In_ const std::vector<int> &baseData, _In_ double expectedMean)
    {
        HRESULT hr;
        auto data = Convert<uint16_t>(baseData);

        ::printf("UShort[] marshalling\n");

        double actual;
        THROW_IF_FAILED(arrayTesting->Mean_UShort_LP_PreLen(static_cast<int>(baseData.size()), data.data(), &actual));
        THROW_FAIL_IF_FALSE(EqualByBound(expectedMean, actual));

        actual = 0.0;
        THROW_IF_FAILED(arrayTesting->Mean_UShort_LP_PostLen(data.data(), static_cast<int>(baseData.size()), &actual));
        THROW_FAIL_IF_FALSE(EqualByBound(expectedMean, actual));

        actual = 0.0;
        int len;
        SafeArraySmartPtr<uint16_t> saData{ data };
        THROW_IF_FAILED(arrayTesting->Mean_UShort_SafeArray_OutLen(saData, &len, &actual));
        THROW_FAIL_IF_FALSE(EqualByBound(expectedMean, actual));
        THROW_FAIL_IF_FALSE(len == saData.Length());
    }

    void IntArray(_In_ IArrayTesting *arrayTesting, _In_ const std::vector<int> &baseData, _In_ double expectedMean)
    {
        HRESULT hr;
        auto data = Convert<int32_t>(baseData);

        ::printf("Int[] marshalling\n");

        double actual;
        THROW_IF_FAILED(arrayTesting->Mean_Int_LP_PreLen(static_cast<int>(baseData.size()), data.data(), &actual));
        THROW_FAIL_IF_FALSE(EqualByBound(expectedMean, actual));

        actual = 0.0;
        THROW_IF_FAILED(arrayTesting->Mean_Int_LP_PostLen(data.data(), static_cast<int>(baseData.size()), &actual));
        THROW_FAIL_IF_FALSE(EqualByBound(expectedMean, actual));

        actual = 0.0;
        int len;
        SafeArraySmartPtr<int32_t> saData{ data };
        THROW_IF_FAILED(arrayTesting->Mean_Int_SafeArray_OutLen(saData, &len, &actual));
        THROW_FAIL_IF_FALSE(EqualByBound(expectedMean, actual));
        THROW_FAIL_IF_FALSE(len == saData.Length());
    }

    void UIntArray(_In_ IArrayTesting *arrayTesting, _In_ const std::vector<int> &baseData, _In_ double expectedMean)
    {
        HRESULT hr;
        auto data = Convert<uint32_t>(baseData);

        ::printf("UInt[] marshalling\n");

        double actual;
        THROW_IF_FAILED(arrayTesting->Mean_UInt_LP_PreLen(static_cast<int>(baseData.size()), data.data(), &actual));
        THROW_FAIL_IF_FALSE(EqualByBound(expectedMean, actual));

        actual = 0.0;
        THROW_IF_FAILED(arrayTesting->Mean_UInt_LP_PostLen(data.data(), static_cast<int>(baseData.size()), &actual));
        THROW_FAIL_IF_FALSE(EqualByBound(expectedMean, actual));

        actual = 0.0;
        int len;
        SafeArraySmartPtr<uint32_t> saData{ data };
        THROW_IF_FAILED(arrayTesting->Mean_UInt_SafeArray_OutLen(saData, &len, &actual));
        THROW_FAIL_IF_FALSE(EqualByBound(expectedMean, actual));
        THROW_FAIL_IF_FALSE(len == saData.Length());
    }

    void LongArray(_In_ IArrayTesting *arrayTesting, _In_ const std::vector<int> &baseData, _In_ double expectedMean)
    {
        HRESULT hr;
        auto data = Convert<int64_t>(baseData);

        ::printf("Long[] marshalling\n");

        double actual;
        THROW_IF_FAILED(arrayTesting->Mean_Long_LP_PreLen(static_cast<int>(baseData.size()), data.data(), &actual));
        THROW_FAIL_IF_FALSE(EqualByBound(expectedMean, actual));

        actual = 0.0;
        THROW_IF_FAILED(arrayTesting->Mean_Long_LP_PostLen(data.data(), static_cast<int>(baseData.size()), &actual));
        THROW_FAIL_IF_FALSE(EqualByBound(expectedMean, actual));

        actual = 0.0;
        int len;
        SafeArraySmartPtr<int64_t> saData{ data };
        THROW_IF_FAILED(arrayTesting->Mean_Long_SafeArray_OutLen(saData, &len, &actual));
        THROW_FAIL_IF_FALSE(EqualByBound(expectedMean, actual));
        THROW_FAIL_IF_FALSE(len == saData.Length());
    }

    void ULongArray(_In_ IArrayTesting *arrayTesting, _In_ const std::vector<int> &baseData, _In_ double expectedMean)
    {
        HRESULT hr;
        auto data = Convert<uint64_t>(baseData);

        ::printf("ULong[] marshalling\n");

        double actual;
        THROW_IF_FAILED(arrayTesting->Mean_ULong_LP_PreLen(static_cast<int>(baseData.size()), data.data(), &actual));
        THROW_FAIL_IF_FALSE(EqualByBound(expectedMean, actual));

        actual = 0.0;
        THROW_IF_FAILED(arrayTesting->Mean_ULong_LP_PostLen(data.data(), static_cast<int>(baseData.size()), &actual));
        THROW_FAIL_IF_FALSE(EqualByBound(expectedMean, actual));

        actual = 0.0;
        int len;
        SafeArraySmartPtr<uint64_t> saData{ data };
        THROW_IF_FAILED(arrayTesting->Mean_ULong_SafeArray_OutLen(saData, &len, &actual));
        THROW_FAIL_IF_FALSE(EqualByBound(expectedMean, actual));
        THROW_FAIL_IF_FALSE(len == saData.Length());
    }

    void FloatArray(_In_ IArrayTesting *arrayTesting, _In_ const std::vector<int> &baseData, _In_ double expectedMean)
    {
        HRESULT hr;
        auto data = Convert<float>(baseData);

        ::printf("Float[] marshalling\n");

        double actual;
        THROW_IF_FAILED(arrayTesting->Mean_Float_LP_PreLen(static_cast<int>(baseData.size()), data.data(), &actual));
        THROW_FAIL_IF_FALSE(EqualByBound(expectedMean, actual));

        actual = 0.0;
        THROW_IF_FAILED(arrayTesting->Mean_Float_LP_PostLen(data.data(), static_cast<int>(baseData.size()), &actual));
        THROW_FAIL_IF_FALSE(EqualByBound(expectedMean, actual));

        actual = 0.0;
        int len;
        SafeArraySmartPtr<float> saData{ data };
        THROW_IF_FAILED(arrayTesting->Mean_Float_SafeArray_OutLen(saData, &len, &actual));
        THROW_FAIL_IF_FALSE(EqualByBound(expectedMean, actual));
        THROW_FAIL_IF_FALSE(len == saData.Length());
    }

    void DoubleArray(_In_ IArrayTesting *arrayTesting, _In_ const std::vector<int> &baseData, _In_ double expectedMean)
    {
        HRESULT hr;
        auto data = Convert<double>(baseData);

        ::printf("Double[] marshalling\n");

        double actual;
        THROW_IF_FAILED(arrayTesting->Mean_Double_LP_PreLen(static_cast<int>(baseData.size()), data.data(), &actual));
        THROW_FAIL_IF_FALSE(EqualByBound(expectedMean, actual));

        actual = 0.0;
        THROW_IF_FAILED(arrayTesting->Mean_Double_LP_PostLen(data.data(), static_cast<int>(baseData.size()), &actual));
        THROW_FAIL_IF_FALSE(EqualByBound(expectedMean, actual));

        actual = 0.0;
        int len;
        SafeArraySmartPtr<double> saData{ data };
        THROW_IF_FAILED(arrayTesting->Mean_Double_SafeArray_OutLen(saData, &len, &actual));
        THROW_FAIL_IF_FALSE(EqualByBound(expectedMean, actual));
        THROW_FAIL_IF_FALSE(len == saData.Length());
    }
}

void Run_ArrayTests()
{
    HRESULT hr;

    CoreShimComActivation csact{ W("NETServer.dll"), W("ArrayTesting") };

    ComSmartPtr<IArrayTesting> arrayTesting;
    THROW_IF_FAILED(::CoCreateInstance(CLSID_ArrayTesting, nullptr, CLSCTX_INPROC, IID_IArrayTesting, (void**)&arrayTesting));

    std::vector<int> baseData(10);
    std::iota(std::begin(baseData), std::end(baseData), 0);
    double mean = std::accumulate(std::begin(baseData), std::end(baseData), 0.0) / baseData.size();

    ByteArray(arrayTesting, baseData, mean);
    ShortArray(arrayTesting, baseData, mean);
    UShortArray(arrayTesting, baseData, mean);
    IntArray(arrayTesting, baseData, mean);
    UIntArray(arrayTesting, baseData, mean);
    LongArray(arrayTesting, baseData, mean);
    ULongArray(arrayTesting, baseData, mean);
    FloatArray(arrayTesting, baseData, mean);
    DoubleArray(arrayTesting, baseData, mean);
}
