// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.Intrinsics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

// Test passing and returning HVAs (homogeneous vector aggregates) to/from managed code.
// Test various sizes (including ones that exceed the limit for being treated as HVAs),
// as well as passing various numbers of them (so that we exceed the available registers),
// and mixing the HVA parameters with non-HVA parameters.

// This Test case covers all cases for
//   Methods that take one HVA argument with between 1 and 5 Vector64 or Vector128 elements
//   - Called normally or by using reflection
//   Methods that return an HVA with between 1 and 5 Vector64 or Vector128 elements
//   - Called normally or by using reflection

// Remaining Test cases to do:
// - Add tests that have one than one HVA argument
// - Add types that are *not* HVA types (e.g. too many vectors, or using Vector256).
// - Add tests that use a mix of HVA and non-HVA arguments

public static class VectorMgdMgd
{
    private const int PASS = 100;
    private const int FAIL = 0;

    public const int DefaultSeed = 20010415;
    public static int Seed = Environment.GetEnvironmentVariable("CORECLR_SEED") switch
    {
        string seedStr when seedStr.Equals("random", StringComparison.OrdinalIgnoreCase) => new Random().Next(),
        string seedStr when int.TryParse(seedStr, out int envSeed) => envSeed,
        _ => DefaultSeed
    };

    static Random random = new Random(Seed);

    public static T GetValueFromInt<T>(int value)
    {
        if (typeof(T) == typeof(float))
        {
            float floatValue = (float)value;
            return (T)(object)floatValue;
        }
        if (typeof(T) == typeof(double))
        {
            double doubleValue = (double)value;
            return (T)(object)doubleValue;
        }
        if (typeof(T) == typeof(int))
        {
            return (T)(object)value;
        }
        if (typeof(T) == typeof(uint))
        {
            uint uintValue = (uint)value;
            return (T)(object)uintValue;
        }
        if (typeof(T) == typeof(long))
        {
            long longValue = (long)value;
            return (T)(object)longValue;
        }
        if (typeof(T) == typeof(ulong))
        {
            ulong longValue = (ulong)value;
            return (T)(object)longValue;
        }
        if (typeof(T) == typeof(ushort))
        {
            return (T)(object)(ushort)value;
        }
        if (typeof(T) == typeof(byte))
        {
            return (T)(object)(byte)value;
        }
        if (typeof(T) == typeof(short))
        {
            return (T)(object)(short)value;
        }
        if (typeof(T) == typeof(sbyte))
        {
            return (T)(object)(sbyte)value;
        }
        else
        {
            throw new ArgumentException();
        }
    }
    public static bool CheckValue<T>(T value, T expectedValue)
    {
        bool returnVal;
        if (typeof(T) == typeof(float))
        {
            returnVal = Math.Abs(((float)(object)value) - ((float)(object)expectedValue)) <= Single.Epsilon;
        }
        if (typeof(T) == typeof(double))
        {
            returnVal = Math.Abs(((double)(object)value) - ((double)(object)expectedValue)) <= Double.Epsilon;
        }
        else
        {
            returnVal = value.Equals(expectedValue);
        }
        return returnVal;
    }

    public unsafe class HVATests<T> where T : struct
    {
        // An HVA can contain up to 4 vectors, so we'll test structs with up to 5 of them
        // (to ensure that even those that are too large are handled consistently).
        // So we need 5 * the element count in the largest (128-bit) vector with the smallest
        // element type (byte).
        static T[] values;
        static T[] check;
        private int ElementCount = (Unsafe.SizeOf<Vector128<T>>() / sizeof(byte)) * 5;
        public bool isPassing = true;
        public bool isReflection = false;

        Type[] reflectionParameterTypes;
        System.Reflection.MethodInfo reflectionMethodInfo;
        object[] reflectionInvokeArgs;

        ////////////////////////////////////////

        public struct HVA64_01
        {
            public Vector64<T> v0;
        }

        public struct HVA64_02
        {
            public Vector64<T> v0;
            public Vector64<T> v1;
        }

        public struct HVA64_03
        {
            public Vector64<T> v0;
            public Vector64<T> v1;
            public Vector64<T> v2;
        }

        public struct HVA64_04
        {
            public Vector64<T> v0;
            public Vector64<T> v1;
            public Vector64<T> v2;
            public Vector64<T> v3;
        }

        public struct HVA64_05
        {
            public Vector64<T> v0;
            public Vector64<T> v1;
            public Vector64<T> v2;
            public Vector64<T> v3;
            public Vector64<T> v4;
        }

        private HVA64_01  hva64_01;
        private HVA64_02  hva64_02;
        private HVA64_03  hva64_03;
        private HVA64_04  hva64_04;
        private HVA64_05  hva64_05;

        ////////////////////////////////////////

        public struct HVA128_01
        {
            public Vector128<T> v0;
        }

        public struct HVA128_02
        {
            public Vector128<T> v0;
            public Vector128<T> v1;
        }

        public struct HVA128_03
        {
            public Vector128<T> v0;
            public Vector128<T> v1;
            public Vector128<T> v2;
        }

        public struct HVA128_04
        {
            public Vector128<T> v0;
            public Vector128<T> v1;
            public Vector128<T> v2;
            public Vector128<T> v3;
        }

        public struct HVA128_05
        {
            public Vector128<T> v0;
            public Vector128<T> v1;
            public Vector128<T> v2;
            public Vector128<T> v3;
            public Vector128<T> v4;
       }

        private HVA128_01 hva128_01;
        private HVA128_02 hva128_02;
        private HVA128_03 hva128_03;
        private HVA128_04 hva128_04;
        private HVA128_05 hva128_05;

        ////////////////////////////////////////

        public void Init_HVAs()
        {
            int i;

            i = 0;
            hva64_01.v0 = Unsafe.As<T, Vector64<T>>(ref values[i]);

            i = 0;
            hva64_02.v0 = Unsafe.As<T, Vector64<T>>(ref values[i]);
            i += Vector64<T>.Count;
            hva64_02.v1 = Unsafe.As<T, Vector64<T>>(ref values[i]);

            i = 0;
            hva64_03.v0 = Unsafe.As<T, Vector64<T>>(ref values[i]);
            i += Vector64<T>.Count;
            hva64_03.v1 = Unsafe.As<T, Vector64<T>>(ref values[i]);
            i += Vector64<T>.Count;
            hva64_03.v2 = Unsafe.As<T, Vector64<T>>(ref values[i]);

            i = 0;
            hva64_04.v0 = Unsafe.As<T, Vector64<T>>(ref values[i]);
            i += Vector64<T>.Count;
            hva64_04.v1 = Unsafe.As<T, Vector64<T>>(ref values[i]);
            i += Vector64<T>.Count;
            hva64_04.v2 = Unsafe.As<T, Vector64<T>>(ref values[i]);
            i += Vector64<T>.Count;
            hva64_04.v3 = Unsafe.As<T, Vector64<T>>(ref values[i]);

            i = 0;
            hva64_05.v0 = Unsafe.As<T, Vector64<T>>(ref values[i]);
            i += Vector64<T>.Count;
            hva64_05.v1 = Unsafe.As<T, Vector64<T>>(ref values[i]);
            i += Vector64<T>.Count;
            hva64_05.v2 = Unsafe.As<T, Vector64<T>>(ref values[i]);
            i += Vector64<T>.Count;
            hva64_05.v3 = Unsafe.As<T, Vector64<T>>(ref values[i]);
            i += Vector64<T>.Count;
            hva64_05.v4 = Unsafe.As<T, Vector64<T>>(ref values[i]);

            ////////////////////////////////////////

            i = 0;
            hva128_01.v0 = Unsafe.As<T, Vector128<T>>(ref values[i]);

            i = 0;
            hva128_02.v0 = Unsafe.As<T, Vector128<T>>(ref values[i]);
            i += Vector128<T>.Count;
            hva128_02.v1 = Unsafe.As<T, Vector128<T>>(ref values[i]);

            i = 0;
            hva128_03.v0 = Unsafe.As<T, Vector128<T>>(ref values[i]);
            i += Vector128<T>.Count;
            hva128_03.v1 = Unsafe.As<T, Vector128<T>>(ref values[i]);
            i += Vector128<T>.Count;
            hva128_03.v2 = Unsafe.As<T, Vector128<T>>(ref values[i]);

            i = 0;
            hva128_04.v0 = Unsafe.As<T, Vector128<T>>(ref values[i]);
            i += Vector128<T>.Count;
            hva128_04.v1 = Unsafe.As<T, Vector128<T>>(ref values[i]);
            i += Vector128<T>.Count;
            hva128_04.v2 = Unsafe.As<T, Vector128<T>>(ref values[i]);
            i += Vector128<T>.Count;
            hva128_04.v3 = Unsafe.As<T, Vector128<T>>(ref values[i]);

            i = 0;
            hva128_05.v0 = Unsafe.As<T, Vector128<T>>(ref values[i]);
            i += Vector128<T>.Count;
            hva128_05.v1 = Unsafe.As<T, Vector128<T>>(ref values[i]);
            i += Vector128<T>.Count;
            hva128_05.v2 = Unsafe.As<T, Vector128<T>>(ref values[i]);
            i += Vector128<T>.Count;
            hva128_05.v3 = Unsafe.As<T, Vector128<T>>(ref values[i]);
            i += Vector128<T>.Count;
            hva128_05.v4 = Unsafe.As<T, Vector128<T>>(ref values[i]);
       }

        public HVATests()
        {
            values = new T[ElementCount];
            for (int i = 0; i < values.Length; i++)
            {
                int data = random.Next(100);
                values[i] = GetValueFromInt<T>(data);
            }

            Init_HVAs();
        }

        // Checks that the values in v correspond to those in the values array starting
        // with values[index]
        private void checkValues(string msg, Vector64<T> v, int index)
        {
            bool printedMsg = false;  // Print at most one message

            for (int i = 0; i < Vector64<T>.Count; i++)
            {
                if (!CheckValue<T>(v.GetElement(i), values[index]))
                {
                    if (!printedMsg)
                    {
                        Console.WriteLine("{0}: FAILED - Vector64<T> checkValues(index = {1}, i = {2}) {3}",
                                          msg, index, i, isReflection ? "(via reflection)" : "" );
                        printedMsg = true;
                    }

                    // Record failure status in global isPassing
                    isPassing = false;
                }
                index++;
            }
        }

        // Checks that the values in v correspond to those in the values array starting
        // with values[index]
        private void checkValues(string msg, Vector128<T> v, int index)
        {
            bool printedMsg = false;  // Print at most one message

            for (int i = 0; i < Vector128<T>.Count; i++)
            {
                if (!CheckValue<T>(v.GetElement(i), values[index]))
                {
                    if (!printedMsg)
                    {
                        Console.WriteLine("{0}: FAILED - Vector64<T> checkValues(index = {1}, i = {2}) {3}",
                                          msg, index, i, isReflection ? "(via reflection)" : "" );
                        printedMsg = true;
                    }

                    // Record failure status in global isPassing
                    isPassing = false;
                }
                index++;
            }
        }

        public void Done_Reflection()
        {
            isReflection = false;
        }

        //==========    Vector64<T> tests

        //====================   Tests for passing 1 argument of HVA64_01

        // Test the case where we've passed in a single argument HVA of 1 vector.
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void test1Argument_HVA64_01(HVA64_01 arg1)
        {
            checkValues("test1Argument_HVA64_01(arg1.vo)", arg1.v0, 0);
        }

        public void Init_Reflection_Args_HVA64_01()
        {
            isReflection = true;
            reflectionParameterTypes = new Type[] { typeof(HVA64_01) };
            reflectionMethodInfo = typeof(HVATests<T>).GetMethod(nameof(HVATests<T>.test1Argument_HVA64_01), reflectionParameterTypes);
            reflectionInvokeArgs = new object[] { hva64_01 };
        }

        //====================   Tests for passing 1 argument of HVA64_02

        // Test the case where we've passed in a single argument HVA of 2 vectors.
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void test1Argument_HVA64_02(HVA64_02 arg1)
        {
            checkValues("test1Argument_HVA64_02(arg1.v0)", arg1.v0, 0);
            checkValues("test1Argument_HVA64_02(arg1.v1)", arg1.v1, Vector64<T>.Count);
        }

        public void Init_Reflection_Args_HVA64_02()
        {
            isReflection = true;
            reflectionParameterTypes = new Type[] { typeof(HVA64_02) };
            reflectionMethodInfo = typeof(HVATests<T>).GetMethod(nameof(HVATests<T>.test1Argument_HVA64_02), reflectionParameterTypes);
            reflectionInvokeArgs = new object[] { hva64_02 };
        }

        //====================   Tests for passing 1 argument of HVA64_03

        // Test the case where we've passed in a single argument HVA of 3 vectors.
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void test1Argument_HVA64_03(HVA64_03 arg1)
        {
            checkValues("test1Argument_HVA64_03(arg1.v0)", arg1.v0, 0);
            checkValues("test1Argument_HVA64_03(arg1.v1)", arg1.v1, 1 * Vector64<T>.Count);
            checkValues("test1Argument_HVA64_03(arg1.v2)", arg1.v2, 2 * Vector64<T>.Count);
        }

        public void Init_Reflection_Args_HVA64_03()
        {
            isReflection = true;
            reflectionParameterTypes = new Type[] { typeof(HVA64_03) };
            reflectionMethodInfo = typeof(HVATests<T>).GetMethod(nameof(HVATests<T>.test1Argument_HVA64_03), reflectionParameterTypes);
            reflectionInvokeArgs = new object[] { hva64_03 };
        }

        //====================   Tests for passing 1 argument of HVA64_04

        // Test the case where we've passed in a single argument HVA of 4 vectors.
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void test1Argument_HVA64_04(HVA64_04 arg1)
        {
            checkValues("test1Argument_HVA64_04(arg1.v0)", arg1.v0, 0);
            checkValues("test1Argument_HVA64_04(arg1.v1)", arg1.v1, 1 * Vector64<T>.Count);
            checkValues("test1Argument_HVA64_04(arg1.v2)", arg1.v2, 2 * Vector64<T>.Count);
            checkValues("test1Argument_HVA64_04(arg1.v3)", arg1.v3, 3 * Vector64<T>.Count);
        }

        public void Init_Reflection_Args_HVA64_04()
        {
            isReflection = true;
            reflectionParameterTypes = new Type[] { typeof(HVA64_04) };
            reflectionMethodInfo = typeof(HVATests<T>).GetMethod(nameof(HVATests<T>.test1Argument_HVA64_04), reflectionParameterTypes);
            reflectionInvokeArgs = new object[] { hva64_04 };
        }

        //====================   Tests for passing 1 argument of HVA64_05

        // Test the case where we've passed in a single argument HVA of 5 vectors.
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void test1Argument_HVA64_05(HVA64_05 arg1)
        {
            checkValues("test1Argument_HVA64_05(arg1.v0)", arg1.v0, 0);
            checkValues("test1Argument_HVA64_05(arg1.v1)", arg1.v1, 1 * Vector64<T>.Count);
            checkValues("test1Argument_HVA64_05(arg1.v2)", arg1.v2, 2 * Vector64<T>.Count);
            checkValues("test1Argument_HVA64_05(arg1.v3)", arg1.v3, 3 * Vector64<T>.Count);
            checkValues("test1Argument_HVA64_05(arg1.v4)", arg1.v4, 4 * Vector64<T>.Count);
        }

        public void Init_Reflection_Args_HVA64_05()
        {
            isReflection = true;
            reflectionParameterTypes = new Type[] { typeof(HVA64_05) };
            reflectionMethodInfo = typeof(HVATests<T>).GetMethod(nameof(HVATests<T>.test1Argument_HVA64_05), reflectionParameterTypes);
            reflectionInvokeArgs = new object[] { hva64_05 };
        }

        //===============   Tests for return values if HVA64

        //====================   Tests for return values of HVA64_01

        // Return an HVA of 1 vectors, with values from the 'values' array.
        [MethodImpl(MethodImplOptions.NoInlining)]
        public HVA64_01 returnTest_HVA64_01()
        {
            return hva64_01;
        }

        public void testReturn_HVA64_01()
        {
            HVA64_01 result = returnTest_HVA64_01();
            checkValues("testReturn_HVA64_01(result.v0)",result.v0, 0);
        }

        public void Init_Reflection_Return_HVA64_01()
        {
            isReflection = true;
            reflectionParameterTypes = new Type[] { };
            reflectionMethodInfo = typeof(HVATests<T>).GetMethod(nameof(HVATests<T>.returnTest_HVA64_01), reflectionParameterTypes);
            reflectionInvokeArgs = new object[] { };
        }

        public void testReflectionReturn_HVA64_01()
        {
            Init_Reflection_Return_HVA64_01();

            object objResult = reflectionMethodInfo.Invoke(this, reflectionInvokeArgs);

            HVA64_01 result = (HVA64_01)objResult;

            checkValues("testReflectionReturn_HVA64_01(result.v0)",result.v0, 0);

            Done_Reflection();
        }

        //====================   Tests for return values of HVA64_02

        // Return an HVA of 2 vectors, with values from the 'values' array.
        [MethodImpl(MethodImplOptions.NoInlining)]
        public HVA64_02 returnTest_HVA64_02()
        {
            return hva64_02;
        }

        public void testReturn_HVA64_02()
        {
            HVA64_02 result = returnTest_HVA64_02();
            checkValues("testReturn_HVA64_02(result.v0)",result.v0, 0);
            checkValues("testReturn_HVA64_02(result.v1)",result.v1, Vector64<T>.Count);
        }

        public void Init_Reflection_Return_HVA64_02()
        {
            isReflection = true;
            reflectionParameterTypes = new Type[] { };
            reflectionMethodInfo = typeof(HVATests<T>).GetMethod(nameof(HVATests<T>.returnTest_HVA64_02), reflectionParameterTypes);
            reflectionInvokeArgs = new object[] { };
        }

        public void testReflectionReturn_HVA64_02()
        {
            Init_Reflection_Return_HVA64_02();

            object objResult = reflectionMethodInfo.Invoke(this, reflectionInvokeArgs);

            HVA64_02 result = (HVA64_02)objResult;

            checkValues("testReflectionReturn_HVA64_02(result.v0)",result.v0, 0);
            checkValues("testReflectionReturn_HVA64_02(result.v1)",result.v1, Vector64<T>.Count);

            Done_Reflection();
        }

        //====================   Tests for return values of HVA64_03

        // Return an HVA of 3 vectors, with values from the 'values' array.
        [MethodImpl(MethodImplOptions.NoInlining)]
        public HVA64_03 returnTest_HVA64_03()
        {
            return hva64_03;
        }

        public void testReturn_HVA64_03()
        {
            HVA64_03 result = returnTest_HVA64_03();
            checkValues("testReturn_HVA64_03(result.v0)",result.v0, 0);
            checkValues("testReturn_HVA64_03(result.v1)",result.v1, 1 * Vector64<T>.Count);
            checkValues("testReturn_HVA64_03(result.v2)",result.v2, 2 * Vector64<T>.Count);
        }

        public void Init_Reflection_Return_HVA64_03()
        {
            isReflection = true;
            reflectionParameterTypes = new Type[] { };
            reflectionMethodInfo = typeof(HVATests<T>).GetMethod(nameof(HVATests<T>.returnTest_HVA64_03), reflectionParameterTypes);
            reflectionInvokeArgs = new object[] { };
        }

        public void testReflectionReturn_HVA64_03()
        {
            Init_Reflection_Return_HVA64_03();

            object objResult = reflectionMethodInfo.Invoke(this, reflectionInvokeArgs);

            HVA64_03 result = (HVA64_03)objResult;

            checkValues("testReflectionReturn_HVA64_03(result.v0)",result.v0, 0);
            checkValues("testReflectionReturn_HVA64_03(result.v1)",result.v1, 1 * Vector64<T>.Count);
            checkValues("testReflectionReturn_HVA64_03(result.v2)",result.v2, 2 * Vector64<T>.Count);

            Done_Reflection();
        }

        //====================   Tests for return values of HVA64_04

        // Return an HVA of 4 vectors, with values from the 'values' array.
        [MethodImpl(MethodImplOptions.NoInlining)]
        public HVA64_04 returnTest_HVA64_04()
        {
            return hva64_04;
        }

        public void testReturn_HVA64_04()
        {
            HVA64_04 result = returnTest_HVA64_04();
            checkValues("testReturn_HVA64_04(result.v0)",result.v0, 0);
            checkValues("testReturn_HVA64_04(result.v1)",result.v1, 1 * Vector64<T>.Count);
            checkValues("testReturn_HVA64_04(result.v2)",result.v2, 2 * Vector64<T>.Count);
            checkValues("testReturn_HVA64_04(result.v3)",result.v3, 3 * Vector64<T>.Count);
        }

        public void Init_Reflection_Return_HVA64_04()
        {
            isReflection = true;
            reflectionParameterTypes = new Type[] { };
            reflectionMethodInfo = typeof(HVATests<T>).GetMethod(nameof(HVATests<T>.returnTest_HVA64_04), reflectionParameterTypes);
            reflectionInvokeArgs = new object[] { };
        }

        public void testReflectionReturn_HVA64_04()
        {
            Init_Reflection_Return_HVA64_04();

            object objResult = reflectionMethodInfo.Invoke(this, reflectionInvokeArgs);

            HVA64_04 result = (HVA64_04)objResult;

            checkValues("testReflectionReturn_HVA64_04(result.v0)",result.v0, 0);
            checkValues("testReflectionReturn_HVA64_04(result.v1)",result.v1, 1 * Vector64<T>.Count);
            checkValues("testReflectionReturn_HVA64_04(result.v2)",result.v2, 2 * Vector64<T>.Count);
            checkValues("testReflectionReturn_HVA64_04(result.v3)",result.v3, 3 * Vector64<T>.Count);

            Done_Reflection();
        }

        //====================   Tests for return values of HVA64_05

        // Return an HVA of 5 vectors, with values from the 'values' array.
        [MethodImpl(MethodImplOptions.NoInlining)]
        public HVA64_05 returnTest_HVA64_05()
        {
            return hva64_05;
        }

        public void testReturn_HVA64_05()
        {
            HVA64_05 result = returnTest_HVA64_05();
            checkValues("testReturn_HVA64_05(result.v0)",result.v0, 0);
            checkValues("testReturn_HVA64_05(result.v1)",result.v1, 1 * Vector64<T>.Count);
            checkValues("testReturn_HVA64_05(result.v2)",result.v2, 2 * Vector64<T>.Count);
            checkValues("testReturn_HVA64_05(result.v3)",result.v3, 3 * Vector64<T>.Count);
            checkValues("testReturn_HVA64_05(result.v4)",result.v4, 4 * Vector64<T>.Count);
        }

        public void Init_Reflection_Return_HVA64_05()
        {
            isReflection = true;
            reflectionParameterTypes = new Type[] { };
            reflectionMethodInfo = typeof(HVATests<T>).GetMethod(nameof(HVATests<T>.returnTest_HVA64_05), reflectionParameterTypes);
            reflectionInvokeArgs = new object[] { };
        }

        public void testReflectionReturn_HVA64_05()
        {
            Init_Reflection_Return_HVA64_05();

            object objResult = reflectionMethodInfo.Invoke(this, reflectionInvokeArgs);

            HVA64_05 result = (HVA64_05)objResult;

            checkValues("testReflectionReturn_HVA64_05(result.v0)",result.v0, 0);
            checkValues("testReflectionReturn_HVA64_05(result.v1)",result.v1, 1 * Vector64<T>.Count);
            checkValues("testReflectionReturn_HVA64_05(result.v2)",result.v2, 2 * Vector64<T>.Count);
            checkValues("testReflectionReturn_HVA64_05(result.v3)",result.v3, 3 * Vector64<T>.Count);
            checkValues("testReflectionReturn_HVA64_05(result.v4)",result.v4, 4 * Vector64<T>.Count);

            Done_Reflection();
        }

        //==========    Vector128<T> tests

        //====================   Tests for passing 1 argument of HVA128_01

        // Test the case where we've passed in a single argument HVA of 1 vectors.
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void test1Argument_HVA128_01(HVA128_01 arg1)
        {
            checkValues("test1Argument_HVA128_01(arg1.v0)", arg1.v0, 0);
        }

        public void Init_Reflection_Args_HVA128_01()
        {
            isReflection = true;
            reflectionParameterTypes = new Type[] { typeof(HVA128_01) };
            reflectionMethodInfo = typeof(HVATests<T>).GetMethod(nameof(HVATests<T>.test1Argument_HVA128_01), reflectionParameterTypes);
            reflectionInvokeArgs = new object[] { hva128_01 };
        }

        //====================   Tests for passing 1 argument of HVA128_02

        // Test the case where we've passed in a single argument HVA of 2 vectors.
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void test1Argument_HVA128_02(HVA128_02 arg1)
        {
            checkValues("test1Argument_HVA128_02(arg1.v0)", arg1.v0, 0);
            checkValues("test1Argument_HVA128_02(arg1.v1)", arg1.v1, Vector128<T>.Count);
        }

        public void Init_Reflection_Args_HVA128_02()
        {
            isReflection = true;
            reflectionParameterTypes = new Type[] { typeof(HVA128_02) };
            reflectionMethodInfo = typeof(HVATests<T>).GetMethod(nameof(HVATests<T>.test1Argument_HVA128_02), reflectionParameterTypes);
            reflectionInvokeArgs = new object[] { hva128_02 };
        }

        //====================   Tests for passing 1 argument of HVA128_03

        // Test the case where we've passed in a single argument HVA of 2 vectors.
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void test1Argument_HVA128_03(HVA128_03 arg1)
        {
            checkValues("test1Argument_HVA128_03(arg1.v0)", arg1.v0, 0);
            checkValues("test1Argument_HVA128_03(arg1.v1)", arg1.v1, 1 * Vector128<T>.Count);
            checkValues("test1Argument_HVA128_03(arg1.v2)", arg1.v2, 2 * Vector128<T>.Count);
        }

        public void Init_Reflection_Args_HVA128_03()
        {
            isReflection = true;
            reflectionParameterTypes = new Type[] { typeof(HVA128_03) };
            reflectionMethodInfo = typeof(HVATests<T>).GetMethod(nameof(HVATests<T>.test1Argument_HVA128_03), reflectionParameterTypes);
            reflectionInvokeArgs = new object[] { hva128_03 };
        }

        //====================   Tests for passing 1 argument of HVA128_04

        // Test the case where we've passed in a single argument HVA of 2 vectors.
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void test1Argument_HVA128_04(HVA128_04 arg1)
        {
            checkValues("test1Argument_HVA128_04(arg1.v0)", arg1.v0, 0);
            checkValues("test1Argument_HVA128_04(arg1.v1)", arg1.v1, 1 * Vector128<T>.Count);
            checkValues("test1Argument_HVA128_04(arg1.v2)", arg1.v2, 2 * Vector128<T>.Count);
            checkValues("test1Argument_HVA128_04(arg1.v3)", arg1.v3, 3 * Vector128<T>.Count);
        }

        public void Init_Reflection_Args_HVA128_04()
        {
            isReflection = true;
            reflectionParameterTypes = new Type[] { typeof(HVA128_04) };
            reflectionMethodInfo = typeof(HVATests<T>).GetMethod(nameof(HVATests<T>.test1Argument_HVA128_04), reflectionParameterTypes);
            reflectionInvokeArgs = new object[] { hva128_04 };
        }

        //====================   Tests for passing 1 argument of HVA128_05

        // Test the case where we've passed in a single argument HVA of 2 vectors.
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void test1Argument_HVA128_05(HVA128_05 arg1)
        {
            checkValues("test1Argument_HVA128_05(arg1.v0)", arg1.v0, 0);
            checkValues("test1Argument_HVA128_05(arg1.v1)", arg1.v1, 1 * Vector128<T>.Count);
            checkValues("test1Argument_HVA128_05(arg1.v2)", arg1.v2, 2 * Vector128<T>.Count);
            checkValues("test1Argument_HVA128_05(arg1.v3)", arg1.v3, 3 * Vector128<T>.Count);
            checkValues("test1Argument_HVA128_05(arg1.v4)", arg1.v4, 4 * Vector128<T>.Count);
        }

        public void Init_Reflection_Args_HVA128_05()
        {
            isReflection = true;
            reflectionParameterTypes = new Type[] { typeof(HVA128_05) };
            reflectionMethodInfo = typeof(HVATests<T>).GetMethod(nameof(HVATests<T>.test1Argument_HVA128_05), reflectionParameterTypes);
            reflectionInvokeArgs = new object[] { hva128_05 };
        }

        //====================   Tests for return values of HVA128_01

        // Return an HVA of 1 vector, with values from the 'values' array.
        [MethodImpl(MethodImplOptions.NoInlining)]
        public HVA128_01 returnTest_HVA128_01()
        {
            return hva128_01;
        }

        public void testReturn_HVA128_01()
        {
            HVA128_01 result = returnTest_HVA128_01();
            checkValues("testReturn_HVA128_01(result.v0)",result.v0, 0);
        }

        public void Init_Reflection_Return_HVA128_01()
        {
            reflectionParameterTypes = new Type[] { };
            reflectionMethodInfo = typeof(HVATests<T>).GetMethod(nameof(HVATests<T>.returnTest_HVA128_01), reflectionParameterTypes);
            reflectionInvokeArgs = new object[] { };
        }

        public void testReflectionReturn_HVA128_01()
        {
            Init_Reflection_Return_HVA128_01();

            object objResult = reflectionMethodInfo.Invoke(this, reflectionInvokeArgs);

            HVA128_01 result = (HVA128_01)objResult;

            checkValues("testReflectionReturn_HVA128_01(result.v0)",result.v0, 0);

            Done_Reflection();
        }

        //====================   Tests for return values of HVA128_02

        // Return an HVA of 2 vectors, with values from the 'values' array.
        [MethodImpl(MethodImplOptions.NoInlining)]
        public HVA128_02 returnTest_HVA128_02()
        {
            return hva128_02;
        }

        public void testReturn_HVA128_02()
        {
            HVA128_02 result = returnTest_HVA128_02();
            checkValues("testReturn_HVA128_02(result.v0)",result.v0, 0);
            checkValues("testReturn_HVA128_02(result.v1)",result.v1, Vector128<T>.Count);
        }

        public void Init_Reflection_Return_HVA128_02()
        {
            isReflection = true;
            reflectionParameterTypes = new Type[] { };
            reflectionMethodInfo = typeof(HVATests<T>).GetMethod(nameof(HVATests<T>.returnTest_HVA128_02), reflectionParameterTypes);
            reflectionInvokeArgs = new object[] { };
        }

        public void testReflectionReturn_HVA128_02()
        {
            Init_Reflection_Return_HVA128_02();

            object objResult = reflectionMethodInfo.Invoke(this, reflectionInvokeArgs);

            HVA128_02 result = (HVA128_02)objResult;

            checkValues("testReflectionReturn_HVA128_02(result.v0)",result.v0, 0);
            checkValues("testReflectionReturn_HVA128_02(result.v1)",result.v1, Vector128<T>.Count);

            Done_Reflection();
        }

        //====================   Tests for return values of HVA128_03

        // Return an HVA of 3 vectors, with values from the 'values' array.
        [MethodImpl(MethodImplOptions.NoInlining)]
        public HVA128_03 returnTest_HVA128_03()
        {
            return hva128_03;
        }

        public void testReturn_HVA128_03()
        {
            HVA128_03 result = returnTest_HVA128_03();
            checkValues("testReturn_HVA128_03(result.v0)",result.v0, 0);
            checkValues("testReturn_HVA128_03(result.v1)",result.v1, 1 * Vector128<T>.Count);
            checkValues("testReturn_HVA128_03(result.v2)",result.v2, 2 * Vector128<T>.Count);
        }

        public void Init_Reflection_Return_HVA128_03()
        {
            isReflection = true;
            reflectionParameterTypes = new Type[] { };
            reflectionMethodInfo = typeof(HVATests<T>).GetMethod(nameof(HVATests<T>.returnTest_HVA128_03), reflectionParameterTypes);
            reflectionInvokeArgs = new object[] { };
        }

        public void testReflectionReturn_HVA128_03()
        {
            Init_Reflection_Return_HVA128_03();

            object objResult = reflectionMethodInfo.Invoke(this, reflectionInvokeArgs);

            HVA128_03 result = (HVA128_03)objResult;

            checkValues("testReflectionReturn_HVA128_03(result.v0)",result.v0, 0);
            checkValues("testReflectionReturn_HVA128_03(result.v1)",result.v1, 1 * Vector128<T>.Count);
            checkValues("testReflectionReturn_HVA128_03(result.v2)",result.v2, 2 * Vector128<T>.Count);

            Done_Reflection();
        }

        //====================   Tests for return values of HVA128_04

        // Return an HVA of 3 vectors, with values from the 'values' array.
        [MethodImpl(MethodImplOptions.NoInlining)]
        public HVA128_04 returnTest_HVA128_04()
        {
            return hva128_04;
        }

        public void testReturn_HVA128_04()
        {
            HVA128_04 result = returnTest_HVA128_04();
            checkValues("testReturn_HVA128_04(result.v0)",result.v0, 0);
            checkValues("testReturn_HVA128_04(result.v1)",result.v1, 1 * Vector128<T>.Count);
            checkValues("testReturn_HVA128_04(result.v2)",result.v2, 2 * Vector128<T>.Count);
            checkValues("testReturn_HVA128_04(result.v3)",result.v3, 3 * Vector128<T>.Count);
        }

        public void Init_Reflection_Return_HVA128_04()
        {
            isReflection = true;
            reflectionParameterTypes = new Type[] { };
            reflectionMethodInfo = typeof(HVATests<T>).GetMethod(nameof(HVATests<T>.returnTest_HVA128_04), reflectionParameterTypes);
            reflectionInvokeArgs = new object[] { };
        }

        public void testReflectionReturn_HVA128_04()
        {
            Init_Reflection_Return_HVA128_04();

            object objResult = reflectionMethodInfo.Invoke(this, reflectionInvokeArgs);

            HVA128_04 result = (HVA128_04)objResult;

            checkValues("testReflectionReturn_HVA128_04(result.v0)",result.v0, 0);
            checkValues("testReflectionReturn_HVA128_04(result.v1)",result.v1, 1 * Vector128<T>.Count);
            checkValues("testReflectionReturn_HVA128_04(result.v2)",result.v2, 2 * Vector128<T>.Count);
            checkValues("testReflectionReturn_HVA128_04(result.v3)",result.v3, 3 * Vector128<T>.Count);

            Done_Reflection();
        }

        //====================   Tests for return values of HVA128_05

        // Return an HVA of 3 vectors, with values from the 'values' array.
        [MethodImpl(MethodImplOptions.NoInlining)]
        public HVA128_05 returnTest_HVA128_05()
        {
            return hva128_05;
        }

        public void testReturn_HVA128_05()
        {
            HVA128_05 result = returnTest_HVA128_05();
            checkValues("testReturn_HVA128_05(result.v0)",result.v0, 0);
            checkValues("testReturn_HVA128_05(result.v1)",result.v1, 1 * Vector128<T>.Count);
            checkValues("testReturn_HVA128_05(result.v2)",result.v2, 2 * Vector128<T>.Count);
            checkValues("testReturn_HVA128_05(result.v3)",result.v3, 3 * Vector128<T>.Count);
            checkValues("testReturn_HVA128_05(result.v4)",result.v4, 4 * Vector128<T>.Count);
        }

        public void Init_Reflection_Return_HVA128_05()
        {
            isReflection = true;
            reflectionParameterTypes = new Type[] { };
            reflectionMethodInfo = typeof(HVATests<T>).GetMethod(nameof(HVATests<T>.returnTest_HVA128_05), reflectionParameterTypes);
            reflectionInvokeArgs = new object[] { };
        }

        public void testReflectionReturn_HVA128_05()
        {
            Init_Reflection_Return_HVA128_05();

            object objResult = reflectionMethodInfo.Invoke(this, reflectionInvokeArgs);

            HVA128_05 result = (HVA128_05)objResult;

            checkValues("testReflectionReturn_HVA128_05(result.v0)",result.v0, 0);
            checkValues("testReflectionReturn_HVA128_05(result.v1)",result.v1, 1 * Vector128<T>.Count);
            checkValues("testReflectionReturn_HVA128_05(result.v2)",result.v2, 2 * Vector128<T>.Count);
            checkValues("testReflectionReturn_HVA128_05(result.v3)",result.v3, 3 * Vector128<T>.Count);
            checkValues("testReflectionReturn_HVA128_05(result.v4)",result.v4, 4 * Vector128<T>.Count);

            Done_Reflection();
        }

        //////////////////////////////////////////////////

        public void doTests()
        {
            //////  Vector64<T> tests

            // Test HVA Vector64<T> Arguments

            test1Argument_HVA64_01(hva64_01);

            Init_Reflection_Args_HVA64_01();
            reflectionMethodInfo.Invoke(this, reflectionInvokeArgs);
            Done_Reflection();


            test1Argument_HVA64_02(hva64_02);

            Init_Reflection_Args_HVA64_02();
            reflectionMethodInfo.Invoke(this, reflectionInvokeArgs);
            Done_Reflection();


            test1Argument_HVA64_03(hva64_03);

            Init_Reflection_Args_HVA64_03();
            reflectionMethodInfo.Invoke(this, reflectionInvokeArgs);
            Done_Reflection();


            test1Argument_HVA64_04(hva64_04);

            Init_Reflection_Args_HVA64_04();
            reflectionMethodInfo.Invoke(this, reflectionInvokeArgs);
            Done_Reflection();


            test1Argument_HVA64_05(hva64_05);

            Init_Reflection_Args_HVA64_05();
            reflectionMethodInfo.Invoke(this, reflectionInvokeArgs);
            Done_Reflection();


            // Test HVA Vector64<T> Return values

            testReturn_HVA64_01();

            testReflectionReturn_HVA64_01();


            testReturn_HVA64_02();

            testReflectionReturn_HVA64_02();


            testReturn_HVA64_03();

            testReflectionReturn_HVA64_03();


            testReturn_HVA64_04();

            testReflectionReturn_HVA64_04();


            testReturn_HVA64_05();

            testReflectionReturn_HVA64_05();


            //////  Vector128<T> tests

            // Test HVA Vector128<T> Arguments

            test1Argument_HVA128_01(hva128_01);

            Init_Reflection_Args_HVA128_01();
            reflectionMethodInfo.Invoke(this, reflectionInvokeArgs);
            Done_Reflection();


            test1Argument_HVA128_02(hva128_02);

            Init_Reflection_Args_HVA128_02();
            reflectionMethodInfo.Invoke(this, reflectionInvokeArgs);
            Done_Reflection();


            test1Argument_HVA128_03(hva128_03);

            Init_Reflection_Args_HVA128_03();
            reflectionMethodInfo.Invoke(this, reflectionInvokeArgs);
            Done_Reflection();


            test1Argument_HVA128_04(hva128_04);

            Init_Reflection_Args_HVA128_04();
            reflectionMethodInfo.Invoke(this, reflectionInvokeArgs);
            Done_Reflection();


            test1Argument_HVA128_05(hva128_05);

            Init_Reflection_Args_HVA128_05();
            reflectionMethodInfo.Invoke(this, reflectionInvokeArgs);
            Done_Reflection();


            // Test HVA Vector128<T> Return values

            testReturn_HVA128_01();
            testReflectionReturn_HVA128_01();

            testReturn_HVA128_02();
            testReflectionReturn_HVA128_02();

            testReturn_HVA128_03();
            testReflectionReturn_HVA128_03();

            testReturn_HVA128_04();
            testReflectionReturn_HVA128_04();

            testReturn_HVA128_05();
            testReflectionReturn_HVA128_05();
        }
    }

    public static int Main(string[] args)
    {

        HVATests<byte> byteTests = new HVATests<byte>();
        byteTests.doTests();

        if (byteTests.isPassing)
        {
            Console.WriteLine("Test Passed");
        }
        else
        {
            Console.WriteLine("Test FAILED");
        }

        return byteTests.isPassing ? PASS : FAIL;
    }
}
