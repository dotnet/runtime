// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using Xunit;

namespace MathFusedMultiplyAddTest
{
    public class Program
    {
        private static int _returnCode = 100;

        [Fact]
        public static int TestEntryPoint()
        {
            TestFloats();
            TestDoubles();
            return _returnCode;
        }

#region MathF.FusedMultiplyAdd
        static void TestFloats()
        {
            float[] testValues =
                {
                    MathF.PI, MathF.E, 0.0f, -0.0f, float.MinValue, float.MaxValue, 42, -42, 1000, -1000,
                    int.MaxValue, int.MinValue, float.NaN, float.PositiveInfinity, float.NegativeInfinity
                };

            foreach (float a in testValues)
            {
                foreach (float b in testValues)
                {
                    foreach (float c in testValues)
                    {
                        Check1(a, b, c);
                        Check2(a, b, c);
                        Check3(a, b, c);
                        Check4(a, b, c);
                        Check5(a, b, c);
                        Check6(a, b, c);
                        Check7(a, b, c);
                        Check8(a, b, c);

                        if (Fma.IsSupported)
                        {
                            Vector128<float> vecA = Vector128.Create(42f);
                            TestExplicitFmaUsage1(ref vecA, 9f);
                            TestExplicitFmaUsage2(ref vecA, 9f);
                            TestExplicitFmaUsage3(ref vecA, 9f);
                            TestExplicitFmaUsage4(ref vecA, 9f);
                            TestExplicitFmaUsage5(ref vecA, 9f);
                        }
                    }
                }
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static void Check1(float a, float b, float c) =>
            CompareFloats(ReferenceMultiplyAdd( a,  b,  c), 
                        MathF.FusedMultiplyAdd( a,  b,  c));

        [MethodImpl(MethodImplOptions.NoInlining)]
        static void Check2(float a, float b, float c) =>
            CompareFloats(ReferenceMultiplyAdd(-a,  b,  c),
                        MathF.FusedMultiplyAdd(-a,  b,  c));

        [MethodImpl(MethodImplOptions.NoInlining)]
        static void Check3(float a, float b, float c) =>
            CompareFloats(ReferenceMultiplyAdd(-a, -b,  c),
                        MathF.FusedMultiplyAdd(-a, -b,  c));

        [MethodImpl(MethodImplOptions.NoInlining)]
        static void Check4(float a, float b, float c) =>
            CompareFloats(ReferenceMultiplyAdd(-a, -b, -c),
                        MathF.FusedMultiplyAdd(-a, -b, -c));

        [MethodImpl(MethodImplOptions.NoInlining)]
        static void Check5(float a, float b, float c) =>
            CompareFloats(ReferenceMultiplyAdd( a, -b,  c), 
                        MathF.FusedMultiplyAdd( a, -b,  c));

        [MethodImpl(MethodImplOptions.NoInlining)]
        static void Check6(float a, float b, float c) =>
            CompareFloats(ReferenceMultiplyAdd( a, -b, -c), 
                        MathF.FusedMultiplyAdd( a, -b, -c));

        [MethodImpl(MethodImplOptions.NoInlining)]
        static void Check7(float a, float b, float c) =>
            CompareFloats(ReferenceMultiplyAdd(-a,  b, -c), 
                        MathF.FusedMultiplyAdd(-a,  b, -c));

        [MethodImpl(MethodImplOptions.NoInlining)]
        static void Check8(float a, float b, float c) =>
            CompareFloats(ReferenceMultiplyAdd( a,  b, -c), 
                        MathF.FusedMultiplyAdd( a,  b, -c));

        [MethodImpl(MethodImplOptions.NoInlining)]
        static float ReferenceMultiplyAdd(float a, float b, float c) => a * b + c;

        [MethodImpl(MethodImplOptions.NoInlining)]
        static void CompareFloats(float a, float b)
        {
            if (Math.Abs(a - b) > 0.001f)
            {
                Console.WriteLine($"{a} != {b}");
                _returnCode--;
            }
        }

        // FMA intrinsics can be used explicitly, make sure nothing asserts
        // with various types of arguments (fields, local variables, constants and refs)

        static Vector128<float> _c32 = Vector128.CreateScalarUnsafe(MathF.PI);

        [MethodImpl(MethodImplOptions.NoInlining)]
        static void TestExplicitFmaUsage1(ref Vector128<float> a, float b)
        {
            CompareFloats(ReferenceMultiplyAdd(a.ToScalar(), b, _c32.ToScalar()),
                Fma.MultiplyAdd(a, Vector128.CreateScalarUnsafe(b), _c32).ToScalar());
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static void TestExplicitFmaUsage2(ref Vector128<float> a, float b)
        {
            CompareFloats(ReferenceMultiplyAdd(a.ToScalar(), a.ToScalar(), a.ToScalar()),
                Fma.MultiplyAdd(a, a, a).ToScalar());
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static void TestExplicitFmaUsage3(ref Vector128<float> a, float b)
        {
            CompareFloats(ReferenceMultiplyAdd(_c32.ToScalar(), _c32.ToScalar(), _c32.ToScalar()),
                Fma.MultiplyAdd(_c32, _c32, _c32).ToScalar());
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static void TestExplicitFmaUsage4(ref Vector128<float> a, float b)
        {
            CompareFloats(ReferenceMultiplyAdd(b, b, 333f), 
                Fma.MultiplyAdd(
                    Vector128.CreateScalarUnsafe(b),
                    Vector128.CreateScalarUnsafe(b), 
                    Vector128.CreateScalarUnsafe(333f)).ToScalar());
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static void TestExplicitFmaUsage5(ref Vector128<float> a, float b)
        {
            CompareDoubles(ReferenceMultiplyAdd(-b, -b, -333f),
                Fma.MultiplyAdd(
                    Vector128.CreateScalarUnsafe(-b),
                    Vector128.CreateScalarUnsafe(-b),
                    Vector128.CreateScalarUnsafe(-333f)).ToScalar());
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static void TestExplicitFmaUsage6(ref Vector128<float> a, float b)
        {
            CompareFloats(ReferenceMultiplyAdd(b, b, b),
                Fma.MultiplyAdd(
                    Vector128.CreateScalarUnsafe(b),
                    Vector128.CreateScalar(b),
                    Vector128.Create(b)).ToScalar());
        }
        #endregion

        #region Math.FusedMultiplyAdd
        static void TestDoubles()
        {
            double[] testValues =
                {
                    Math.PI, Math.E, 0.0, -0.0, double.MinValue, double.MaxValue, 42, -42, 100000, -100000,
                    long.MaxValue, long.MinValue, double.NaN, double.PositiveInfinity, double.NegativeInfinity
                };

            foreach (double a in testValues)
            {
                foreach (double b in testValues)
                {
                    foreach (double c in testValues)
                    {
                        Check1(a, b, c);
                        Check2(a, b, c);
                        Check3(a, b, c);
                        Check4(a, b, c);
                        Check5(a, b, c);
                        Check6(a, b, c);
                        Check7(a, b, c);
                        Check8(a, b, c);

                        if (Fma.IsSupported)
                        {
                            Vector128<double> vecA = Vector128.Create(42.0);
                            TestExplicitFmaUsage1(ref vecA, 9f);
                            TestExplicitFmaUsage2(ref vecA, 9f);
                            TestExplicitFmaUsage3(ref vecA, 9f);
                            TestExplicitFmaUsage4(ref vecA, 9f);
                            TestExplicitFmaUsage5(ref vecA, 9f);
                        }
                    }
                }
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static void Check1(double a, double b, double c) =>
            CompareDoubles(ReferenceMultiplyAdd( a,  b,  c), 
                          Math.FusedMultiplyAdd( a,  b,  c));

        [MethodImpl(MethodImplOptions.NoInlining)]
        static void Check2(double a, double b, double c) =>
            CompareDoubles(ReferenceMultiplyAdd(-a,  b,  c),
                          Math.FusedMultiplyAdd(-a,  b,  c));

        [MethodImpl(MethodImplOptions.NoInlining)]
        static void Check3(double a, double b, double c) =>
            CompareDoubles(ReferenceMultiplyAdd(-a, -b,  c),
                          Math.FusedMultiplyAdd(-a, -b,  c));

        [MethodImpl(MethodImplOptions.NoInlining)]
        static void Check4(double a, double b, double c) =>
            CompareDoubles(ReferenceMultiplyAdd(-a, -b, -c),
                          Math.FusedMultiplyAdd(-a, -b, -c));

        [MethodImpl(MethodImplOptions.NoInlining)]
        static void Check5(double a, double b, double c) =>
            CompareDoubles(ReferenceMultiplyAdd( a, -b,  c), 
                          Math.FusedMultiplyAdd( a, -b,  c));

        [MethodImpl(MethodImplOptions.NoInlining)]
        static void Check6(double a, double b, double c) =>
            CompareDoubles(ReferenceMultiplyAdd( a, -b, -c), 
                          Math.FusedMultiplyAdd( a, -b, -c));

        [MethodImpl(MethodImplOptions.NoInlining)]
        static void Check7(double a, double b, double c) =>
            CompareDoubles(ReferenceMultiplyAdd(-a,  b, -c), 
                          Math.FusedMultiplyAdd(-a,  b, -c));

        [MethodImpl(MethodImplOptions.NoInlining)]
        static void Check8(double a, double b, double c) =>
            CompareDoubles(ReferenceMultiplyAdd( a,  b, -c), 
                          Math.FusedMultiplyAdd( a,  b, -c));

        [MethodImpl(MethodImplOptions.NoInlining)]
        static double ReferenceMultiplyAdd(double a, double b, double c) => a * b + c;

        [MethodImpl(MethodImplOptions.NoInlining)]
        static void CompareDoubles(double a, double b)
        {
            if (Math.Abs(a - b) > 0.00001)
            {
                Console.WriteLine($"{a} != {b}");
                _returnCode--;
            }
        }

        // FMA intrinsics can be used explicitly, make sure nothing asserts
        // with various types of arguments (fields, local variables, constants and refs)

        static Vector128<double> _c64 = Vector128.CreateScalarUnsafe(Math.PI);

        [MethodImpl(MethodImplOptions.NoInlining)]
        static void TestExplicitFmaUsage1(ref Vector128<double> a, double b)
        {
            CompareDoubles(ReferenceMultiplyAdd(a.ToScalar(), b, _c64.ToScalar()),
                Fma.MultiplyAdd(a, Vector128.CreateScalarUnsafe(b), _c64).ToScalar());
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static void TestExplicitFmaUsage2(ref Vector128<double> a, double b)
        {
            CompareDoubles(ReferenceMultiplyAdd(a.ToScalar(), a.ToScalar(), a.ToScalar()),
                Fma.MultiplyAdd(a, a, a).ToScalar());
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static void TestExplicitFmaUsage3(ref Vector128<double> a, double b)
        {
            CompareDoubles(ReferenceMultiplyAdd(_c64.ToScalar(), _c64.ToScalar(), _c64.ToScalar()),
                Fma.MultiplyAdd(_c64, _c64, _c64).ToScalar());
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static void TestExplicitFmaUsage4(ref Vector128<double> a, double b)
        {
            CompareDoubles(ReferenceMultiplyAdd(b, b, b), 
                Fma.MultiplyAdd(
                    Vector128.CreateScalarUnsafe(b),
                    Vector128.CreateScalarUnsafe(b), 
                    Vector128.CreateScalarUnsafe(b)).ToScalar());
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static void TestExplicitFmaUsage5(ref Vector128<double> a, double b)
        {
            CompareDoubles(ReferenceMultiplyAdd(-b, -b, -333.0),
                Fma.MultiplyAdd(
                    Vector128.CreateScalarUnsafe(-b),
                    Vector128.CreateScalarUnsafe(-b),
                    Vector128.CreateScalarUnsafe(-333.0)).ToScalar());
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static void TestExplicitFmaUsage6(ref Vector128<double> a, double b)
        {
            CompareDoubles(ReferenceMultiplyAdd(b, b, b),
                Fma.MultiplyAdd(
                    Vector128.CreateScalarUnsafe(b),
                    Vector128.CreateScalar(b),
                    Vector128.Create(b)).ToScalar());
        }
#endregion
    }
}
