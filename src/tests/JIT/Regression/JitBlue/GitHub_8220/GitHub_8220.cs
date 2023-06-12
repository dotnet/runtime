// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Regression test for Vector3 intrinsics using upper non-zero'd bits from
// a byref return.

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Numerics;
using Xunit;

namespace Test
{

    public class Program
    {
        static Random random;

        public const int DefaultSeed = 20010415;
        public static int Seed = Environment.GetEnvironmentVariable("CORECLR_SEED") switch
        {
            string seedStr when seedStr.Equals("random", StringComparison.OrdinalIgnoreCase) => new Random().Next(),
            string seedStr when int.TryParse(seedStr, out int envSeed) => envSeed,
            _ => DefaultSeed
        };

        static Program()
        {
            random = new Random(Seed);
        }

        [MethodImpl( MethodImplOptions.NoInlining )]
        public static double StackScribble()
        {
            double d1 = random.NextDouble();
            double d2 = random.NextDouble();
            double d3 = random.NextDouble();
            double d4 = random.NextDouble();
            double d5 = random.NextDouble();
            double d6 = random.NextDouble();
            double d7 = random.NextDouble();
            double d8 = random.NextDouble();
            double d9 = random.NextDouble();
            double d10 = random.NextDouble();
            double d11 = random.NextDouble();
            double d12 = random.NextDouble();
            double d13 = random.NextDouble();
            double d14 = random.NextDouble();
            double d15 = random.NextDouble();
            double d16 = random.NextDouble();
            double d17 = random.NextDouble();
            double d18 = random.NextDouble();
            double d19 = random.NextDouble();
            double d20 = random.NextDouble();
            double d21 = random.NextDouble();
            double d22 = random.NextDouble();
            double d23 = random.NextDouble();
            double d24 = random.NextDouble();
            double d25 = random.NextDouble();
            double d26 = random.NextDouble();
            double d27 = random.NextDouble();
            double d28 = random.NextDouble();
            double d29 = random.NextDouble();
            double d30 = random.NextDouble();
            double d31 = random.NextDouble();
            double d32 = random.NextDouble();
            double d33 = random.NextDouble();
            double d34 = random.NextDouble();
            double d35 = random.NextDouble();
            double d36 = random.NextDouble();
            double d37 = random.NextDouble();
            double d38 = random.NextDouble();
            double d39 = random.NextDouble();
            double d40 = random.NextDouble();
            return d1 + d2 + d3 + d4 + d5 + d6 + d7 + d8 + d9 + d10 +
                   d11 + d12 + d13 + d14 + d15 + d16 + d17 + d18 + d19 + d20 +
                   d21 + d22 + d23 + d24 + d25 + d26 + d27 + d28 + d29 + d20 +
                   d31 + d32 + d33 + d34 + d35 + d36 + d37 + d38 + d39 + d40;
        }

        [MethodImpl( MethodImplOptions.NoInlining )]
        public static Vector3 getTestValue(float f1, float f2, float f3)
        {
            return new Vector3(f1, f2, f3);
        }

        public static bool Check(float value, float expectedValue)
        {
            // These may differ in the last place.
            float expectedValueLow;
            float expectedValueHigh;

            unsafe
            {
                UInt32 expectedValueUInt = *(UInt32*)&expectedValue;
                UInt32 expectedValueUIntLow = (expectedValueUInt == 0) ? 0 : expectedValueUInt - 1;
                UInt32 expectedValueUIntHigh = (expectedValueUInt == 0xffffffff) ? 0xffffffff : expectedValueUInt + 1;
                expectedValueLow = *(float*)&expectedValueUIntLow;
                expectedValueHigh = *(float*)&expectedValueUIntHigh;
            }
            float errorMargin = Math.Abs(expectedValueHigh - expectedValueLow);
            if (Math.Abs(value - expectedValue) > errorMargin)
            {
                return false;
            }
            return true;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static int testDotProduct(Vector3 v0)
        {
            float f1 = (float)random.NextDouble();
            float f2 = (float)random.NextDouble();
            float f3 = (float)random.NextDouble();

            Vector3 v1 = Vector3.Normalize(getTestValue(f1, f2, f3) - v0);
            Vector3 v2 = new Vector3(f1, f2, f3) - v0;
            v2 = v2 / v2.Length();

            if (!Check(v1.X, v2.X) || !Check(v1.Y, v2.Y) || !Check(v1.Z, v2.Z))
            {
                Console.WriteLine("Vectors do not match " + v1 + v2);
                return -1;
            }

            return 100;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static int testEquals(Vector3 v0)
        {
            float f1 = (float)random.NextDouble();
            float f2 = (float)random.NextDouble();
            float f3 = (float)random.NextDouble();

            Vector3 v1 = new Vector3(f1, f2, f3) - v0;
            bool result = v1.Equals(getTestValue(f1, f2, f3) - v0);

            if ((result == false) || !v1.Equals(getTestValue(f1, f2, f3) - v0))
            {
                Console.WriteLine("Equals returns wrong value " + v1);
                return -1;
            }

            return 100;
        }

        [Fact]
        public static int TestEntryPoint()
        {
            int returnValue = 100;
            Console.WriteLine("Testing Dot Product");
            for (int i = 0; i < 10; i++)
            {
                StackScribble();
                if (testDotProduct(new Vector3(1.0F, 2.0F, 3.0F)) != 100)
                {
                    Console.WriteLine("Failed on iteration " + i);
                    returnValue = -1;
                    break;
                }
            }
            Console.WriteLine("Testing Equals");
            for (int i = 0; i < 10; i++)
            {
                StackScribble();
                if (testEquals(new Vector3(1.0F, 2.0F, 3.0F)) != 100)
                {
                    Console.WriteLine("Failed on iteration " + i);
                    returnValue = -1;
                    break;
                }
            }
            return returnValue;
        }
    }
}

