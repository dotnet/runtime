// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#include <cstddef>
#include <vector>
#include <cstdarg>
#include <cstdint>
#include <numeric>
#include <array>
#include <functional>
#include <iostream>
#using <System.Runtime.dll>
#using <System.Collections.dll>
using namespace System::Collections::Generic;

public enum class TestCases
{
    SumInts,
    SumFloats,
    SumSumInt64s,
    SumWidenedShorts,
    SumDoubles,
    SumWidenedFloats,
    SumHFAs,
    DoublesInIntegerRegisters
};

struct HFA
{
    float f1;
    float f2;
    float f3;
    float f4;
};

#pragma unmanaged

int SumInts(std::size_t numElements, ...)
{
    va_list args;
    va_start(args, numElements);
    int sum = 0;
    for (std::size_t i = 0; i < numElements; ++i)
    {
        sum += va_arg(args, int);
    }
    va_end(args);
    return sum;
}

std::int64_t SumSumInt64s(std::size_t numElements, ...)
{
    va_list args;
    va_start(args, numElements);
    std::int64_t sum = 0;
    for (std::size_t i = 0; i < numElements; ++i)
    {
        sum += va_arg(args, std::int64_t);
    }
    va_end(args);
    return sum;
}

double SumDoubles(std::size_t numElements, ...)
{
    va_list args;
    va_start(args, numElements);
    double sum = 0;
    for (std::size_t i = 0; i < numElements; ++i)
    {
        sum += va_arg(args, double);
    }
    va_end(args);
    return sum;
}

float SumHFAs(std::size_t numElements, ...)
{
    va_list args;
    va_start(args, numElements);
    float sum = 0;
    for (std::size_t i = 0; i < numElements; ++i)
    {
        HFA hfa = va_arg(args, HFA);
        sum += hfa.f1 + hfa.f2 + hfa.f3 + hfa.f4;
    }
    va_end(args);
    return sum;
}

constexpr std::size_t NumArgsPerCall = 41; // Match number of arguments used in JIT/Directed/arglist/vararg test.

#pragma managed

public ref class TestClass
{
public:
    List<TestCases>^ RunTests(int seed)
    {
        System::Random^ rng = gcnew System::Random(seed);
        List<TestCases>^ failedTests = gcnew List<TestCases>();

        if (!RunIntsTest(rng))
        {
            failedTests->Add(TestCases::SumInts);
        }
        if (!RunDoublesTest(rng))
        {
            failedTests->Add(TestCases::SumDoubles);
        }
        if (!RunSumInt64sTest(rng))
        {
            failedTests->Add(TestCases::SumSumInt64s);
        }
        if (!RunWidenedShortsTest(rng))
        {
            failedTests->Add(TestCases::SumWidenedShorts);
        }
        if (!RunWidenedFloatsTest(rng))
        {
            failedTests->Add(TestCases::SumWidenedFloats);
        }
        if (!RunHFAsTest(rng))
        {
            failedTests->Add(TestCases::SumHFAs);
        }
#if HOST_64BIT
        if (!RunDoublesInIntegerRegistersTest())
        {
            failedTests->Add(TestCases::DoublesInIntegerRegisters);
        }
#endif

        return failedTests;
    }
private:
    bool RunIntsTest(System::Random^ rng)
    {
        std::array<int, NumArgsPerCall> values;
        for(std::size_t i = 0; i < NumArgsPerCall; ++i)
        {
            values[i] = rng->Next(System::Int32::MinValue, System::Int32::MaxValue);
        }

        auto expected = std::accumulate(values.begin(), values.end(), 0, std::plus<>{});
        auto actual = SumInts(NumArgsPerCall,
            values[0],
            values[1],
            values[2],
            values[3],
            values[4],
            values[5],
            values[6],
            values[7],
            values[8],
            values[9],
            values[10],
            values[11],
            values[12],
            values[13],
            values[14],
            values[15],
            values[16],
            values[17],
            values[18],
            values[19],
            values[20],
            values[21],
            values[22],
            values[23],
            values[24],
            values[25],
            values[26],
            values[27],
            values[28],
            values[29],
            values[30],
            values[31],
            values[32],
            values[33],
            values[34],
            values[35],
            values[36],
            values[37],
            values[38],
            values[39],
            values[40]
        );
        bool result = expected == actual;
        if (!result)
        {
            std::cout << "RunIntsTest Failed:" << "Expected:" << expected << '\t' << "Actual:" << actual << std::endl;
        }
        return result;
    }

    bool RunDoublesTest(System::Random^ rng)
    {
        std::array<double, NumArgsPerCall> values;
        for (std::size_t i = 0; i < NumArgsPerCall; ++i)
        {
            values[i] = (double)rng->Next(System::Int32::MinValue, System::Int32::MaxValue);
        }

        auto expected = std::accumulate(values.begin(), values.end(), 0.0, std::plus<>{});
        auto actual = SumDoubles(NumArgsPerCall,
            values[0],
            values[1],
            values[2],
            values[3],
            values[4],
            values[5],
            values[6],
            values[7],
            values[8],
            values[9],
            values[10],
            values[11],
            values[12],
            values[13],
            values[14],
            values[15],
            values[16],
            values[17],
            values[18],
            values[19],
            values[20],
            values[21],
            values[22],
            values[23],
            values[24],
            values[25],
            values[26],
            values[27],
            values[28],
            values[29],
            values[30],
            values[31],
            values[32],
            values[33],
            values[34],
            values[35],
            values[36],
            values[37],
            values[38],
            values[39],
            values[40]
        );
        bool result = expected == actual;
        if (!result)
        {
            std::cout << "RunDoublesTest Failed:" << "Expected:" << expected << '\t' << "Actual:" << actual << std::endl;
        }
        return result;
    }

    bool RunHFAsTest(System::Random^ rng)
    {
        std::array<HFA, NumArgsPerCall> values;
        for (std::size_t i = 0; i < NumArgsPerCall; ++i)
        {
            values[i].f1 = (float)rng->Next(System::Int32::MinValue, System::Int32::MaxValue);
            values[i].f2 = (float)rng->Next(System::Int32::MinValue, System::Int32::MaxValue);
            values[i].f3 = (float)rng->Next(System::Int32::MinValue, System::Int32::MaxValue);
            values[i].f4 = (float)rng->Next(System::Int32::MinValue, System::Int32::MaxValue);
        }

        auto expected = std::accumulate(values.begin(), values.end(), 0.0f, AggregateHFAs);
        auto actual = SumHFAs(NumArgsPerCall,
            values[0],
            values[1],
            values[2],
            values[3],
            values[4],
            values[5],
            values[6],
            values[7],
            values[8],
            values[9],
            values[10],
            values[11],
            values[12],
            values[13],
            values[14],
            values[15],
            values[16],
            values[17],
            values[18],
            values[19],
            values[20],
            values[21],
            values[22],
            values[23],
            values[24],
            values[25],
            values[26],
            values[27],
            values[28],
            values[29],
            values[30],
            values[31],
            values[32],
            values[33],
            values[34],
            values[35],
            values[36],
            values[37],
            values[38],
            values[39],
            values[40]
        );
        bool result = expected == actual;
        if (!result)
        {
            std::cout << "RunHFAsTest Failed:" << "Expected:" << expected << '\t' << "Actual:" << actual << std::endl;
        }
        return result;
    }

    static float AggregateHFAs(float current, HFA hfa)
    {
        return current + hfa.f1 + hfa.f2 + hfa.f3 + hfa.f4;
    }

    bool RunSumInt64sTest(System::Random^ rng)
    {
        std::array<std::int64_t, NumArgsPerCall> values;
        for (std::size_t i = 0; i < NumArgsPerCall; ++i)
        {
            values[i] = rng->Next(System::Int32::MinValue, System::Int32::MaxValue);
        }

        auto expected = std::accumulate(values.begin(), values.end(), 0LL, std::plus<>{});
        auto actual = SumSumInt64s(NumArgsPerCall,
            values[0],
            values[1],
            values[2],
            values[3],
            values[4],
            values[5],
            values[6],
            values[7],
            values[8],
            values[9],
            values[10],
            values[11],
            values[12],
            values[13],
            values[14],
            values[15],
            values[16],
            values[17],
            values[18],
            values[19],
            values[20],
            values[21],
            values[22],
            values[23],
            values[24],
            values[25],
            values[26],
            values[27],
            values[28],
            values[29],
            values[30],
            values[31],
            values[32],
            values[33],
            values[34],
            values[35],
            values[36],
            values[37],
            values[38],
            values[39],
            values[40]
        );
        bool result = expected == actual;
        if (!result)
        {
            std::cout << "RunSumInt64sTest Failed:" << "Expected:" << expected << '\t' << "Actual:" << actual << std::endl;
        }
        return result;
    }

    bool RunWidenedShortsTest(System::Random^ rng)
    {
        std::array<int, NumArgsPerCall / 2> intValues;
        std::array<short, NumArgsPerCall - (NumArgsPerCall / 2)> shortValues;
        for (std::size_t i = 0; i < intValues.size(); ++i)
        {
            intValues[i] = rng->Next(System::Int32::MinValue, System::Int32::MaxValue);
        }
        for (std::size_t i = 0; i < shortValues.size(); ++i)
        {
            shortValues[i] = (short)rng->Next(System::Int16::MinValue, System::Int16::MaxValue);
        }

        auto expected = std::accumulate(intValues.begin(), intValues.end(), 0, std::plus<>{}) + std::accumulate(shortValues.begin(), shortValues.end(), 0LL, std::plus<>{});
        auto actual = SumInts(NumArgsPerCall,
            shortValues[0],
            intValues[0], shortValues[1],
            intValues[1], shortValues[2],
            intValues[2], shortValues[3],
            intValues[3], shortValues[4],
            intValues[4], shortValues[5],
            intValues[5], shortValues[6],
            intValues[6], shortValues[7],
            intValues[7], shortValues[8],
            intValues[8], shortValues[9],
            intValues[9], shortValues[10],
            intValues[10], shortValues[11],
            intValues[11], shortValues[12],
            intValues[12], shortValues[13],
            intValues[13], shortValues[14],
            intValues[14], shortValues[15],
            intValues[15], shortValues[16],
            intValues[16], shortValues[17],
            intValues[17], shortValues[18],
            intValues[18], shortValues[19],
            intValues[19], shortValues[20]
        );
        bool result = expected == actual;
        if (!result)
        {
            std::cout << "RunWidenedShortsTest Failed:" << "Expected:" << expected << '\t' << "Actual:" << actual << std::endl;
        }
        return result;
    }

    bool RunWidenedFloatsTest(System::Random^ rng)
    {
        std::array<float, NumArgsPerCall / 2> floatValues;
        std::array<double, NumArgsPerCall - (NumArgsPerCall / 2)> doubleValues;
        for (std::size_t i = 0; i < floatValues.size(); ++i)
        {
            floatValues[i] = (float)rng->Next(System::Int32::MinValue, System::Int32::MaxValue);
        }
        for (std::size_t i = 0; i < doubleValues.size(); ++i)
        {
            doubleValues[i] = (double)rng->Next(System::Int32::MinValue, System::Int32::MaxValue);
        }

        double expected = 0.0;
        for (std::size_t i = 0; i < floatValues.size(); ++i)
        {
            expected += floatValues[i];
        }
        for (std::size_t i = 0; i < doubleValues.size(); ++i)
        {
            expected += doubleValues[i];
        }

        auto actual = SumDoubles(NumArgsPerCall,
            doubleValues[0],
            floatValues[0], doubleValues[1],
            floatValues[1], doubleValues[2],
            floatValues[2], doubleValues[3],
            floatValues[3], doubleValues[4],
            floatValues[4], doubleValues[5],
            floatValues[5], doubleValues[6],
            floatValues[6], doubleValues[7],
            floatValues[7], doubleValues[8],
            floatValues[8], doubleValues[9],
            floatValues[9], doubleValues[10],
            floatValues[10], doubleValues[11],
            floatValues[11], doubleValues[12],
            floatValues[12], doubleValues[13],
            floatValues[13], doubleValues[14],
            floatValues[14], doubleValues[15],
            floatValues[15], doubleValues[16],
            floatValues[16], doubleValues[17],
            floatValues[17], doubleValues[18],
            floatValues[18], doubleValues[19],
            floatValues[19], doubleValues[20]
        );
        bool result = expected == actual;
        if (!result)
        {
            std::cout << "RunWidenedFloatsTest Failed:" << "Expected:" << expected << '\t' << "Actual:" << actual << std::endl;
        }
        return result;
    }

    bool RunDoublesInIntegerRegistersTest()
    {
        double a = 123.456;
        std::int64_t expected = *reinterpret_cast<std::int64_t*>(&a);
        std::int64_t actual = SumSumInt64s(1, a);
        bool result = expected == actual;
        if (!result)
        {
            std::cout << "RunDoublesInIntegerRegistersTest Failed:" << "Expected:" << expected << '\t' << "Actual:" << actual << std::endl;
        }
        return result;
    }
};
