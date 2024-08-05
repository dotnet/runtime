// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using Xunit;

public class Test34094
{
    static bool TestSseCompareGreaterThan()
    {
        if (Sse.IsSupported)
        {
            const int expectedResult = 0b0100;

            Vector128<float> value1 = Vector128.Create(float.NaN, 1.0f, 2.0f, 3.0f);
            Vector128<float> value2 = Vector128.Create(0.0f, 2.0f, 1.0f, 3.0f);
            Vector128<float> result = Sse.CompareGreaterThan(value1, value2);

            int actualResult = Sse.MoveMask(result);

            if (actualResult != expectedResult)
            {
                Console.WriteLine($"{nameof(Sse)}.{nameof(Sse.CompareGreaterThan)}({value1}, {value2}) returned {Convert.ToString(actualResult, 2)}; expected {Convert.ToString(expectedResult, 2)}");
                return false;
            }
        }
        return true;
    }

    static bool TestSseCompareGreaterThanOrEqual()
    {
        if (Sse.IsSupported)
        {
            const int expectedResult = 0b1100;

            Vector128<float> value1 = Vector128.Create(float.NaN, 1.0f, 2.0f, 3.0f);
            Vector128<float> value2 = Vector128.Create(0.0f, 2.0f, 1.0f, 3.0f);
            Vector128<float> result = Sse.CompareGreaterThanOrEqual(value1, value2);

            int actualResult = Sse.MoveMask(result);

            if (actualResult != expectedResult)
            {
                Console.WriteLine($"{nameof(Sse)}.{nameof(Sse.CompareGreaterThanOrEqual)}({value1}, {value2}) returned {Convert.ToString(actualResult, 2)}; expected {Convert.ToString(expectedResult, 2)}");
                return false;
            }
        }
        return true;
    }

    static bool TestSseCompareNotGreaterThan()
    {
        if (Sse.IsSupported)
        {
            const int expectedResult = 0b1011;

            Vector128<float> value1 = Vector128.Create(float.NaN, 1.0f, 2.0f, 3.0f);
            Vector128<float> value2 = Vector128.Create(0.0f, 2.0f, 1.0f, 3.0f);
            Vector128<float> result = Sse.CompareNotGreaterThan(value1, value2);

            int actualResult = Sse.MoveMask(result);

            if (actualResult != expectedResult)
            {
                Console.WriteLine($"{nameof(Sse)}.{nameof(Sse.CompareNotGreaterThan)}({value1}, {value2}) returned {Convert.ToString(actualResult, 2)}; expected {Convert.ToString(expectedResult, 2)}");
                return false;
            }
        }
        return true;
    }

    static bool TestSseCompareNotGreaterThanOrEqual()
    {
        if (Sse.IsSupported)
        {
            const int expectedResult = 0b0011;

            Vector128<float> value1 = Vector128.Create(float.NaN, 1.0f, 2.0f, 3.0f);
            Vector128<float> value2 = Vector128.Create(0.0f, 2.0f, 1.0f, 3.0f);
            Vector128<float> result = Sse.CompareNotGreaterThanOrEqual(value1, value2);

            int actualResult = Sse.MoveMask(result);

            if (actualResult != expectedResult)
            {
                Console.WriteLine($"{nameof(Sse)}.{nameof(Sse.CompareNotGreaterThanOrEqual)}({value1}, {value2}) returned {Convert.ToString(actualResult, 2)}; expected {Convert.ToString(expectedResult, 2)}");
                return false;
            }
        }
        return true;
    }

    static bool TestSseCompareScalarGreaterThan()
    {
        if (Sse.IsSupported)
        {
            const int expectedResult = 0b0000;

            Vector128<float> value1 = Vector128.Create(float.NaN, 1.0f, 2.0f, 3.0f);
            Vector128<float> value2 = Vector128.Create(0.0f, 2.0f, 1.0f, 3.0f);
            Vector128<float> result = Sse.CompareScalarGreaterThan(value1, value2);

            int actualResult = Sse.MoveMask(result);

            if (actualResult != expectedResult)
            {
                Console.WriteLine($"{nameof(Sse)}.{nameof(Sse.CompareScalarGreaterThan)}({value1}, {value2}) returned {Convert.ToString(actualResult, 2)}; expected {Convert.ToString(expectedResult, 2)}");
                return false;
            }
        }
        return true;
    }

    static bool TestSseCompareScalarGreaterThanOrEqual()
    {
        if (Sse.IsSupported)
        {
            const int expectedResult = 0b0000;

            Vector128<float> value1 = Vector128.Create(float.NaN, 1.0f, 2.0f, 3.0f);
            Vector128<float> value2 = Vector128.Create(0.0f, 2.0f, 1.0f, 3.0f);
            Vector128<float> result = Sse.CompareScalarGreaterThanOrEqual(value1, value2);

            int actualResult = Sse.MoveMask(result);

            if (actualResult != expectedResult)
            {
                Console.WriteLine($"{nameof(Sse)}.{nameof(Sse.CompareScalarGreaterThanOrEqual)}({value1}, {value2}) returned {Convert.ToString(actualResult, 2)}; expected {Convert.ToString(expectedResult, 2)}");
                return false;
            }
        }
        return true;
    }

    static bool TestSseCompareScalarNotGreaterThan()
    {
        if (Sse.IsSupported)
        {
            const int expectedResult = 0b0001;

            Vector128<float> value1 = Vector128.Create(float.NaN, 1.0f, 2.0f, 3.0f);
            Vector128<float> value2 = Vector128.Create(0.0f, 2.0f, 1.0f, 3.0f);
            Vector128<float> result = Sse.CompareScalarNotGreaterThan(value1, value2);

            int actualResult = Sse.MoveMask(result);

            if (actualResult != expectedResult)
            {
                Console.WriteLine($"{nameof(Sse)}.{nameof(Sse.CompareScalarNotGreaterThan)}({value1}, {value2}) returned {Convert.ToString(actualResult, 2)}; expected {Convert.ToString(expectedResult, 2)}");
                return false;
            }
        }
        return true;
    }

    static bool TestSseCompareScalarNotGreaterThanOrEqual()
    {
        if (Sse.IsSupported)
        {
            const int expectedResult = 0b0001;

            Vector128<float> value1 = Vector128.Create(float.NaN, 1.0f, 2.0f, 3.0f);
            Vector128<float> value2 = Vector128.Create(0.0f, 2.0f, 1.0f, 3.0f);
            Vector128<float> result = Sse.CompareScalarNotGreaterThanOrEqual(value1, value2);

            int actualResult = Sse.MoveMask(result);

            if (actualResult != expectedResult)
            {
                Console.WriteLine($"{nameof(Sse)}.{nameof(Sse.CompareScalarNotGreaterThanOrEqual)}({value1}, {value2}) returned {Convert.ToString(actualResult, 2)}; expected {Convert.ToString(expectedResult, 2)}");
                return false;
            }
        }
        return true;
    }

    static bool TestSse2CompareGreaterThan()
    {
        if (Sse2.IsSupported)
        {
            const int expectedResult = 0b00;

            Vector128<double> value1 = Vector128.Create(double.NaN, 1.0);
            Vector128<double> value2 = Vector128.Create(0.0, 2.0);
            Vector128<double> result = Sse2.CompareGreaterThan(value1, value2);

            int actualResult = Sse2.MoveMask(result);

            if (actualResult != expectedResult)
            {
                Console.WriteLine($"{nameof(Sse2)}.{nameof(Sse2.CompareGreaterThan)}({value1}, {value2}) returned {Convert.ToString(actualResult, 2)}; expected {Convert.ToString(expectedResult, 2)}");
                return false;
            }
        }
        return true;
    }

    static bool TestSse2CompareGreaterThanOrEqual()
    {
        if (Sse2.IsSupported)
        {
            const int expectedResult = 0b00;

            Vector128<double> value1 = Vector128.Create(double.NaN, 1.0);
            Vector128<double> value2 = Vector128.Create(0.0, 2.0);
            Vector128<double> result = Sse2.CompareGreaterThanOrEqual(value1, value2);

            int actualResult = Sse2.MoveMask(result);

            if (actualResult != expectedResult)
            {
                Console.WriteLine($"{nameof(Sse2)}.{nameof(Sse2.CompareGreaterThanOrEqual)}({value1}, {value2}) returned {Convert.ToString(actualResult, 2)}; expected {Convert.ToString(expectedResult, 2)}");
                return false;
            }
        }
        return true;
    }

    static bool TestSse2CompareNotGreaterThan()
    {
        if (Sse2.IsSupported)
        {
            const int expectedResult = 0b11;

            Vector128<double> value1 = Vector128.Create(double.NaN, 1.0);
            Vector128<double> value2 = Vector128.Create(0.0, 2.0);
            Vector128<double> result = Sse2.CompareNotGreaterThan(value1, value2);

            int actualResult = Sse2.MoveMask(result);

            if (actualResult != expectedResult)
            {
                Console.WriteLine($"{nameof(Sse2)}.{nameof(Sse2.CompareNotGreaterThan)}({value1}, {value2}) returned {Convert.ToString(actualResult, 2)}; expected {Convert.ToString(expectedResult, 2)}");
                return false;
            }
        }
        return true;
    }

    static bool TestSse2CompareNotGreaterThanOrEqual()
    {
        if (Sse2.IsSupported)
        {
            const int expectedResult = 0b11;

            Vector128<double> value1 = Vector128.Create(double.NaN, 1.0);
            Vector128<double> value2 = Vector128.Create(0.0, 2.0);
            Vector128<double> result = Sse2.CompareNotGreaterThanOrEqual(value1, value2);

            int actualResult = Sse2.MoveMask(result);

            if (actualResult != expectedResult)
            {
                Console.WriteLine($"{nameof(Sse2)}.{nameof(Sse2.CompareNotGreaterThanOrEqual)}({value1}, {value2}) returned {Convert.ToString(actualResult, 2)}; expected {Convert.ToString(expectedResult, 2)}");
                return false;
            }
        }
        return true;
    }

    static bool TestSse2CompareScalarGreaterThan()
    {
        if (Sse2.IsSupported)
        {
            const int expectedResult = 0b00;

            Vector128<double> value1 = Vector128.Create(double.NaN, 1.0);
            Vector128<double> value2 = Vector128.Create(0.0, 2.0);
            Vector128<double> result = Sse2.CompareScalarGreaterThan(value1, value2);

            int actualResult = Sse2.MoveMask(result);

            if (actualResult != expectedResult)
            {
                Console.WriteLine($"{nameof(Sse2)}.{nameof(Sse2.CompareScalarGreaterThan)}({value1}, {value2}) returned {Convert.ToString(actualResult, 2)}; expected {Convert.ToString(expectedResult, 2)}");
                return false;
            }
        }
        return true;
    }

    static bool TestSse2CompareScalarGreaterThanOrEqual()
    {
        if (Sse2.IsSupported)
        {
            const int expectedResult = 0b00;

            Vector128<double> value1 = Vector128.Create(double.NaN, 1.0);
            Vector128<double> value2 = Vector128.Create(0.0, 2.0);
            Vector128<double> result = Sse2.CompareScalarGreaterThanOrEqual(value1, value2);

            int actualResult = Sse2.MoveMask(result);

            if (actualResult != expectedResult)
            {
                Console.WriteLine($"{nameof(Sse2)}.{nameof(Sse2.CompareScalarGreaterThanOrEqual)}({value1}, {value2}) returned {Convert.ToString(actualResult, 2)}; expected {Convert.ToString(expectedResult, 2)}");
                return false;
            }
        }
        return true;
    }

    static bool TestSse2CompareScalarNotGreaterThan()
    {
        if (Sse2.IsSupported)
        {
            const int expectedResult = 0b01;

            Vector128<double> value1 = Vector128.Create(double.NaN, 1.0);
            Vector128<double> value2 = Vector128.Create(0.0, 2.0);
            Vector128<double> result = Sse2.CompareScalarNotGreaterThan(value1, value2);

            int actualResult = Sse2.MoveMask(result);

            if (actualResult != expectedResult)
            {
                Console.WriteLine($"{nameof(Sse2)}.{nameof(Sse2.CompareScalarNotGreaterThan)}({value1}, {value2}) returned {Convert.ToString(actualResult, 2)}; expected {Convert.ToString(expectedResult, 2)}");
                return false;
            }
        }
        return true;
    }

    static bool TestSse2CompareScalarNotGreaterThanOrEqual()
    {
        if (Sse2.IsSupported)
        {
            const int expectedResult = 0b01;

            Vector128<double> value1 = Vector128.Create(double.NaN, 1.0);
            Vector128<double> value2 = Vector128.Create(0.0, 2.0);
            Vector128<double> result = Sse2.CompareScalarNotGreaterThanOrEqual(value1, value2);

            int actualResult = Sse2.MoveMask(result);

            if (actualResult != expectedResult)
            {
                Console.WriteLine($"{nameof(Sse2)}.{nameof(Sse2.CompareScalarNotGreaterThanOrEqual)}({value1}, {value2}) returned {Convert.ToString(actualResult, 2)}; expected {Convert.ToString(expectedResult, 2)}");
                return false;
            }
        }
        return true;
    }

    static bool TestAvxCompareGreaterThanSingle()
    {
        if (Avx.IsSupported)
        {
            const int expectedResult = 0b0010_0100;

            Vector256<float> value1 = Vector256.Create(float.NaN, 1.0f, 2.0f, 3.0f, 0.0f, 2.0f, 1.0f, 3.0f);
            Vector256<float> value2 = Vector256.Create(0.0f, 2.0f, 1.0f, 3.0f, float.NaN, 1.0f, 2.0f, 3.0f);
            Vector256<float> result = Avx.CompareGreaterThan(value1, value2);

            int actualResult = Avx.MoveMask(result);

            if (actualResult != expectedResult)
            {
                Console.WriteLine($"{nameof(Avx)}.{nameof(Avx.CompareGreaterThan)}({value1}, {value2}) returned {Convert.ToString(actualResult, 2)}; expected {Convert.ToString(expectedResult, 2)}");
                return false;
            }
        }
        return true;
    }

    static bool TestAvxCompareGreaterThanOrEqualSingle()
    {
        if (Avx.IsSupported)
        {
            const int expectedResult = 0b1010_1100;

            Vector256<float> value1 = Vector256.Create(float.NaN, 1.0f, 2.0f, 3.0f, 0.0f, 2.0f, 1.0f, 3.0f);
            Vector256<float> value2 = Vector256.Create(0.0f, 2.0f, 1.0f, 3.0f, float.NaN, 1.0f, 2.0f, 3.0f);
            Vector256<float> result = Avx.CompareGreaterThanOrEqual(value1, value2);

            int actualResult = Avx.MoveMask(result);

            if (actualResult != expectedResult)
            {
                Console.WriteLine($"{nameof(Avx)}.{nameof(Avx.CompareGreaterThanOrEqual)}({value1}, {value2}) returned {Convert.ToString(actualResult, 2)}; expected {Convert.ToString(expectedResult, 2)}");
                return false;
            }
        }
        return true;
    }

    static bool TestAvxCompareNotGreaterThanSingle()
    {
        if (Avx.IsSupported)
        {
            const int expectedResult = 0b1101_1011;

            Vector256<float> value1 = Vector256.Create(float.NaN, 1.0f, 2.0f, 3.0f, 0.0f, 2.0f, 1.0f, 3.0f);
            Vector256<float> value2 = Vector256.Create(0.0f, 2.0f, 1.0f, 3.0f, float.NaN, 1.0f, 2.0f, 3.0f);
            Vector256<float> result = Avx.CompareNotGreaterThan(value1, value2);

            int actualResult = Avx.MoveMask(result);

            if (actualResult != expectedResult)
            {
                Console.WriteLine($"{nameof(Avx)}.{nameof(Avx.CompareNotGreaterThan)}({value1}, {value2}) returned {Convert.ToString(actualResult, 2)}; expected {Convert.ToString(expectedResult, 2)}");
                return false;
            }
        }
        return true;
    }

    static bool TestAvxCompareNotGreaterThanOrEqualSingle()
    {
        if (Avx.IsSupported)
        {
            const int expectedResult = 0b0101_0011;

            Vector256<float> value1 = Vector256.Create(float.NaN, 1.0f, 2.0f, 3.0f, 0.0f, 2.0f, 1.0f, 3.0f);
            Vector256<float> value2 = Vector256.Create(0.0f, 2.0f, 1.0f, 3.0f, float.NaN, 1.0f, 2.0f, 3.0f);
            Vector256<float> result = Avx.CompareNotGreaterThanOrEqual(value1, value2);

            int actualResult = Avx.MoveMask(result);

            if (actualResult != expectedResult)
            {
                Console.WriteLine($"{nameof(Avx)}.{nameof(Avx.CompareNotGreaterThanOrEqual)}({value1}, {value2}) returned {Convert.ToString(actualResult, 2)}; expected {Convert.ToString(expectedResult, 2)}");
                return false;
            }
        }
        return true;
    }

    static bool TestAvxCompareGreaterThanDouble()
    {
        if (Avx.IsSupported)
        {
            const int expectedResult = 0b1000;

            Vector256<double> value1 = Vector256.Create(double.NaN, 1.0, 0.0, 2.0);
            Vector256<double> value2 = Vector256.Create(0.0, 2.0, double.NaN, 1.0);
            Vector256<double> result = Avx.CompareGreaterThan(value1, value2);

            int actualResult = Avx.MoveMask(result);

            if (actualResult != expectedResult)
            {
                Console.WriteLine($"{nameof(Avx)}.{nameof(Avx.CompareGreaterThan)}({value1}, {value2}) returned {Convert.ToString(actualResult, 2)}; expected {Convert.ToString(expectedResult, 2)}");
                return false;
            }
        }
        return true;
    }

    static bool TestAvxCompareGreaterThanOrEqualDouble()
    {
        if (Avx.IsSupported)
        {
            const int expectedResult = 0b1000;

            Vector256<double> value1 = Vector256.Create(double.NaN, 1.0, 0.0, 2.0);
            Vector256<double> value2 = Vector256.Create(0.0, 2.0, double.NaN, 1.0);
            Vector256<double> result = Avx.CompareGreaterThanOrEqual(value1, value2);

            int actualResult = Avx.MoveMask(result);

            if (actualResult != expectedResult)
            {
                Console.WriteLine($"{nameof(Avx)}.{nameof(Avx.CompareGreaterThanOrEqual)}({value1}, {value2}) returned {Convert.ToString(actualResult, 2)}; expected {Convert.ToString(expectedResult, 2)}");
                return false;
            }
        }
        return true;
    }

    static bool TestAvxCompareNotGreaterThanDouble()
    {
        if (Avx.IsSupported)
        {
            const int expectedResult = 0b0111;

            Vector256<double> value1 = Vector256.Create(double.NaN, 1.0, 0.0, 2.0);
            Vector256<double> value2 = Vector256.Create(0.0, 2.0, double.NaN, 1.0);
            Vector256<double> result = Avx.CompareNotGreaterThan(value1, value2);

            int actualResult = Avx.MoveMask(result);

            if (actualResult != expectedResult)
            {
                Console.WriteLine($"{nameof(Avx)}.{nameof(Avx.CompareNotGreaterThan)}({value1}, {value2}) returned {Convert.ToString(actualResult, 2)}; expected {Convert.ToString(expectedResult, 2)}");
                return false;
            }
        }
        return true;
    }

    static bool TestAvxCompareNotGreaterThanOrEqualDouble()
    {
        if (Avx.IsSupported)
        {
            const int expectedResult = 0b0111;

            Vector256<double> value1 = Vector256.Create(double.NaN, 1.0, 0.0, 2.0);
            Vector256<double> value2 = Vector256.Create(0.0, 2.0, double.NaN, 1.0);
            Vector256<double> result = Avx.CompareNotGreaterThanOrEqual(value1, value2);

            int actualResult = Avx.MoveMask(result);

            if (actualResult != expectedResult)
            {
                Console.WriteLine($"{nameof(Avx)}.{nameof(Avx.CompareNotGreaterThanOrEqual)}({value1}, {value2}) returned {Convert.ToString(actualResult, 2)}; expected {Convert.ToString(expectedResult, 2)}");
                return false;
            }
        }
        return true;
    }

    [Fact]
    public static unsafe int TestEntryPoint()
    {
        if (!Sse.IsSupported)
        {
            Console.WriteLine("SSE is not supported");
        }

        if (!Sse2.IsSupported)
        {
            Console.WriteLine("SSE2 is not supported");
        }

        if (!Avx.IsSupported)
        {
            Console.WriteLine("AVX is not supported");
        }

        return TestSseCompareGreaterThan()
             & TestSseCompareGreaterThanOrEqual()
             & TestSseCompareNotGreaterThan()
             & TestSseCompareNotGreaterThanOrEqual()
             & TestSseCompareScalarGreaterThan()
             & TestSseCompareScalarGreaterThanOrEqual()
             & TestSseCompareScalarNotGreaterThan()
             & TestSseCompareScalarNotGreaterThanOrEqual()
             & TestSse2CompareGreaterThan()
             & TestSse2CompareGreaterThanOrEqual()
             & TestSse2CompareNotGreaterThan()
             & TestSse2CompareNotGreaterThanOrEqual()
             & TestSse2CompareScalarGreaterThan()
             & TestSse2CompareScalarGreaterThanOrEqual()
             & TestSse2CompareScalarNotGreaterThan()
             & TestSse2CompareScalarNotGreaterThanOrEqual()
             & TestAvxCompareGreaterThanSingle()
             & TestAvxCompareGreaterThanOrEqualSingle()
             & TestAvxCompareNotGreaterThanSingle()
             & TestAvxCompareNotGreaterThanOrEqualSingle()
             & TestAvxCompareGreaterThanDouble()
             & TestAvxCompareGreaterThanOrEqualDouble()
             & TestAvxCompareNotGreaterThanDouble()
             & TestAvxCompareNotGreaterThanOrEqualDouble() ? 100 : 0;
    }
}
