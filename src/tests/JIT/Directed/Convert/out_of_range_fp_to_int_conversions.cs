// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Numerics;

namespace FPBehaviorApp
{
    public enum FPtoIntegerConversionType
    {
        CONVERT_BACKWARD_COMPATIBLE,
        CONVERT_SENTINEL,
        CONVERT_SATURATING,
        CONVERT_NATIVECOMPILERBEHAVIOR,
        CONVERT_MANAGED_BACKWARD_COMPATIBLE_X86_X64,
    }

    public enum ConversionType
    {
        ToInt,
        ToLong,
        ToUInt,
        ToULong
    }

    public static class Native
    {
        [DllImport("out_of_range_fp_to_int_conversionsnative")]
        [SuppressGCTransition]
        public static extern int ConvertDoubleToLong(double x, FPtoIntegerConversionType t);

        [DllImport("out_of_range_fp_to_int_conversionsnative")]
        [SuppressGCTransition]
        public static extern uint ConvertDoubleToUnsignedLong(double x, FPtoIntegerConversionType t);

        [DllImport("out_of_range_fp_to_int_conversionsnative")]
        [SuppressGCTransition]
        public static extern long ConvertDoubleToLongLong(double x, FPtoIntegerConversionType t);

        [DllImport("out_of_range_fp_to_int_conversionsnative")]
        [SuppressGCTransition]
        public static extern ulong ConvertDoubleToUnsignedLongLong(double x, FPtoIntegerConversionType t);
    }

    [StructLayout(LayoutKind.Explicit)]
    struct DoubleULongBitcaster
    {
        [FieldOffset(0)]
        public ulong UintVal;
        [FieldOffset(0)]
        public double DoubleVal;
    }

    public static class Managed
    {
        public static bool IsNaN(double d)
        {
            return !(d == d);
        }

        public static void DumpBits(double d)
        {
            DoubleULongBitcaster bitCaster = default(DoubleULongBitcaster);
            bitCaster.DoubleVal = d;
            ulong uintVal = bitCaster.UintVal;
            bool signBit = (int)((uintVal >> 63) & 0x1) != 0;
            ulong mantissa = uintVal & 0x000F_FFFF_FFFF_FFFF;
            int exponent = (int)((uintVal >> 52) & 0x7FF);
            Console.WriteLine($"{signBit} {exponent:x} {mantissa:x}");
        }

        // Equivalent to Math.Truncate, but written in C# to avoid Debug/Check penalties running on CoreCLR test builds
        public static double Truncate(double d)
        {
            DoubleULongBitcaster bitCaster = default(DoubleULongBitcaster);
            bitCaster.DoubleVal = d;
            ulong uintVal = bitCaster.UintVal;
            int exponent = (int)((uintVal >> 52) & 0x7FF);
            if (exponent < 1023)
            {
                uintVal &= 0x8000_0000_0000_0000ul;
            }
            else if (exponent < 1075)
            {
                uintVal &= (ulong)(~(0xF_FFFF_FFFF_FFFF >> (exponent - 1023)));
            }
            bitCaster.UintVal = uintVal;

            return bitCaster.DoubleVal;
        }

        public static int ConvertDoubleToLong(double x, FPtoIntegerConversionType t)
        {
            if (t == FPtoIntegerConversionType.CONVERT_NATIVECOMPILERBEHAVIOR)
                return (int)x;

            x = Truncate(x); // truncate (round toward zero)

            switch (t)
            {
                case FPtoIntegerConversionType.CONVERT_MANAGED_BACKWARD_COMPATIBLE_X86_X64:
                case FPtoIntegerConversionType.CONVERT_BACKWARD_COMPATIBLE:
                case FPtoIntegerConversionType.CONVERT_SENTINEL:
                    return (IsNaN(x) || (x<int.MinValue) || (x > int.MaxValue)) ? int.MinValue: (int) x;

                case FPtoIntegerConversionType.CONVERT_SATURATING:
                    return IsNaN(x) ? 0 : (x< int.MinValue) ? int.MinValue : (x > int.MaxValue) ? int.MaxValue : (int) x;
            }
            return 0;
        }

        public static uint ConvertDoubleToUnsignedLong(double x, FPtoIntegerConversionType t)
        {
            if (t == FPtoIntegerConversionType.CONVERT_NATIVECOMPILERBEHAVIOR)
                return (uint)x;

            x = Truncate(x); // truncate (round toward zero)

            switch (t)
            {
                case FPtoIntegerConversionType.CONVERT_MANAGED_BACKWARD_COMPATIBLE_X86_X64:
                case FPtoIntegerConversionType.CONVERT_BACKWARD_COMPATIBLE:
                    return (IsNaN(x) || (x < long.MinValue) || (x > long.MaxValue)) ? 0 : (uint)(long)x;

                case FPtoIntegerConversionType.CONVERT_SENTINEL:
                    return (IsNaN(x) || (x < 0) || (x > uint.MaxValue)) ? uint.MaxValue : (uint)x;

                case FPtoIntegerConversionType.CONVERT_SATURATING:
                    return (IsNaN(x) || (x < 0)) ? 0 : (x > uint.MaxValue) ? uint.MaxValue : (uint)x;
            }

            return 0;
        }

        public static long ConvertDoubleToLongLong(double x, FPtoIntegerConversionType t)
        {
            if (t == FPtoIntegerConversionType.CONVERT_NATIVECOMPILERBEHAVIOR)
                return (long)x;

            x = Truncate(x); // truncate (round toward zero)

            // (double)LLONG_MAX cannot be represented exactly as double
            const double llong_max_plus_1 = (double)((ulong)long.MaxValue + 1);

            switch (t)
            {
                case FPtoIntegerConversionType.CONVERT_MANAGED_BACKWARD_COMPATIBLE_X86_X64:
                case FPtoIntegerConversionType.CONVERT_BACKWARD_COMPATIBLE:
                case FPtoIntegerConversionType.CONVERT_SENTINEL:
                    return (IsNaN(x) || (x < long.MinValue) || (x >= llong_max_plus_1)) ? long.MinValue : (long)x;

                case FPtoIntegerConversionType.CONVERT_SATURATING:
                    return IsNaN(x) ? 0 : (x < long.MinValue) ? long.MinValue : (x >= llong_max_plus_1) ? long.MaxValue : (long)x;
            }

            return 0;
        }

        public static ulong ConvertDoubleToUnsignedLongLong(double x, FPtoIntegerConversionType t)
        {
            if (t == FPtoIntegerConversionType.CONVERT_NATIVECOMPILERBEHAVIOR)
                return (ulong)x;

            x = Truncate(x); // truncate (round toward zero)

            // (double)ULLONG_MAX cannot be represented exactly as double
            const double ullong_max_plus_1 = -2.0 * (double)long.MinValue;

            switch (t)
            {
                case FPtoIntegerConversionType.CONVERT_BACKWARD_COMPATIBLE:
                    return (IsNaN(x) || (x < long.MinValue) || (x >= ullong_max_plus_1)) ? unchecked((ulong)long.MinValue): (x < 0) ? (ulong)(long)x: (ulong)x;

                case FPtoIntegerConversionType.CONVERT_SENTINEL:
                    return (IsNaN(x) || (x < 0) || (x >= ullong_max_plus_1)) ? ulong.MaxValue : (ulong)x;

                case FPtoIntegerConversionType.CONVERT_SATURATING:
                    return (IsNaN(x) || (x < 0)) ? 0 : (x >= ullong_max_plus_1) ? ulong.MaxValue : (ulong)x;

                case FPtoIntegerConversionType.CONVERT_MANAGED_BACKWARD_COMPATIBLE_X86_X64:
                    const double two63 = 2147483648.0 * 4294967296.0;

                    if (x < two63)
                    {
                        return (x < long.MinValue) ? unchecked((ulong)long.MinValue) : (ulong)(long)x;
                    }
                    else
                    {
                        // (double)LLONG_MAX cannot be represented exactly as double
                        const double llong_max_plus_1 = (double)((ulong)long.MaxValue + 1);
                        x -= two63;
                        x = Math.Truncate(x);
                        return (ulong)((IsNaN(x) || (x >= llong_max_plus_1)) ? long.MinValue : (long)x) + (0x8000000000000000);
                    }
            }

            return 0;
        }

        public static Vector<int> ConvertToVectorInt(Vector<float> vFloat, FPtoIntegerConversionType t)
        {
            int[] values = new int[Vector<float>.Count];
            for (int i = 0; i < values.Length; i++)
            {
                values[i] = ConvertDoubleToLong(vFloat[i], t);
            }
            return new Vector<int>(values);
        }

        public static Vector<uint> ConvertToVectorUInt(Vector<float> vFloat, FPtoIntegerConversionType t)
        {
            uint[] values = new uint[Vector<float>.Count];
            for (int i = 0; i < values.Length; i++)
            {
                values[i] = ConvertDoubleToUnsignedLong(vFloat[i], t);
            }
            return new Vector<uint>(values);
        }

        public static Vector<long> ConvertToVectorLong(Vector<double> vFloat, FPtoIntegerConversionType t)
        {
            long[] values = new long[Vector<double>.Count];
            for (int i = 0; i < values.Length; i++)
            {
                values[i] = ConvertDoubleToLongLong(vFloat[i], t);
            }
            return new Vector<long>(values);
        }

        public static Vector<ulong> ConvertToVectorULong(Vector<double> vFloat, FPtoIntegerConversionType t)
        {
            ulong[] values = new ulong[Vector<double>.Count];
            for (int i = 0; i < values.Length; i++)
            {
                values[i] = ConvertDoubleToUnsignedLongLong(vFloat[i], t);
            }
            return new Vector<ulong>(values);
        }
    }

    [StructLayout(LayoutKind.Explicit)]
    struct FloatUIntBitcaster
    {
        [FieldOffset(0)]
        public uint UintVal;
        [FieldOffset(0)]
        public float FloatVal;
    }

    class Program
    {
        static int failures = 0;
        static FPtoIntegerConversionType ManagedConversionRule = FPtoIntegerConversionType.CONVERT_MANAGED_BACKWARD_COMPATIBLE_X86_X64;

        static void TestBitValue(uint value, double? dblValNullable = null, FPtoIntegerConversionType? tValue = null)
        {
            double dblVal;

            if (dblValNullable.HasValue)
            {
                dblVal = dblValNullable.Value;
            }
            else
            {
                FloatUIntBitcaster bitCaster = new FloatUIntBitcaster();
                bitCaster.UintVal = value;
                dblVal = bitCaster.FloatVal;
            }

            if (!tValue.HasValue)
            {
                TestBitValue(value, dblVal, FPtoIntegerConversionType.CONVERT_MANAGED_BACKWARD_COMPATIBLE_X86_X64);
                TestBitValue(value, dblVal, FPtoIntegerConversionType.CONVERT_BACKWARD_COMPATIBLE);
                TestBitValue(value, dblVal, FPtoIntegerConversionType.CONVERT_SATURATING);
                TestBitValue(value, dblVal, FPtoIntegerConversionType.CONVERT_SENTINEL);
                return;
            }

            FPtoIntegerConversionType t = tValue.Value;

            if (Managed.ConvertDoubleToLong(dblVal, t) != Native.ConvertDoubleToLong(dblVal, t))
            {
                failures++;
                Console.WriteLine($"Managed.ConvertDoubleToLong(dblVal, t) != Native.ConvertDoubleToLong(dblVal, t) {t} {value} {dblVal} {Managed.ConvertDoubleToLong(dblVal, t)} != {Native.ConvertDoubleToLong(dblVal, t)}");
            }

            if (Managed.ConvertDoubleToUnsignedLong(dblVal, t) != Native.ConvertDoubleToUnsignedLong(dblVal, t))
            {
                failures++;
                Console.WriteLine($"Managed.ConvertDoubleToUnsignedLong(dblVal, t) != Native.ConvertDoubleToUnsignedLong(dblVal, t) {t} {value} {dblVal} {Managed.ConvertDoubleToUnsignedLong(dblVal, t)} != {Native.ConvertDoubleToUnsignedLong(dblVal, t)}");
            }

            if (Managed.ConvertDoubleToLongLong(dblVal, t) != Native.ConvertDoubleToLongLong(dblVal, t))
            {
                failures++;
                Console.WriteLine($"Managed.ConvertDoubleToLongLong(dblVal, t) != Native.ConvertDoubleToLongLong(dblVal, t) {t} {value} {dblVal} {Managed.ConvertDoubleToLongLong(dblVal, t)} != {Native.ConvertDoubleToLongLong(dblVal, t)}");
            }

            if (Managed.ConvertDoubleToUnsignedLongLong(dblVal, t) != Native.ConvertDoubleToUnsignedLongLong(dblVal, t))
            {
                failures++;
                Console.WriteLine($"Managed.ConvertDoubleToUnsignedLongLong(dblVal, t) != Native.ConvertDoubleToUnsignedLongLong(dblVal, t) {t} {value} {dblVal} {Managed.ConvertDoubleToUnsignedLongLong(dblVal, t)} != {Native.ConvertDoubleToUnsignedLongLong(dblVal, t)}");
            }
            
            if (t == ManagedConversionRule)
            {
                if (Managed.ConvertDoubleToLong(dblVal, FPtoIntegerConversionType.CONVERT_NATIVECOMPILERBEHAVIOR) != Managed.ConvertDoubleToLong(dblVal, t))
                {
                    failures++;
                    Console.WriteLine($"ConvertDoubleToLong NativeCompilerBehavior(managed) {t} {value} {dblVal} {Managed.ConvertDoubleToLong(dblVal, FPtoIntegerConversionType.CONVERT_NATIVECOMPILERBEHAVIOR)} != {Managed.ConvertDoubleToLong(dblVal, t)}");
                }

                if (Managed.ConvertDoubleToUnsignedLong(dblVal, FPtoIntegerConversionType.CONVERT_NATIVECOMPILERBEHAVIOR) != Managed.ConvertDoubleToUnsignedLong(dblVal, t))
                {
                    failures++;
                    Console.WriteLine($"ConvertDoubleToUnsignedLong NativeCompilerBehavior(managed) {t} {value} {dblVal} {Managed.ConvertDoubleToUnsignedLong(dblVal, FPtoIntegerConversionType.CONVERT_NATIVECOMPILERBEHAVIOR)} != {Managed.ConvertDoubleToUnsignedLong(dblVal, t)}");
                }

                if (Managed.ConvertDoubleToLongLong(dblVal, FPtoIntegerConversionType.CONVERT_NATIVECOMPILERBEHAVIOR) != Managed.ConvertDoubleToLongLong(dblVal, t))
                {
                    failures++;
                    Console.WriteLine($"ConvertDoubleToLongLong NativeCompilerBehavior(managed) {t} {value} {dblVal} {Managed.ConvertDoubleToLongLong(dblVal, FPtoIntegerConversionType.CONVERT_NATIVECOMPILERBEHAVIOR)} != {Managed.ConvertDoubleToLongLong(dblVal, t)}");
                }
                
                if (Managed.ConvertDoubleToUnsignedLongLong(dblVal, FPtoIntegerConversionType.CONVERT_NATIVECOMPILERBEHAVIOR) != Managed.ConvertDoubleToUnsignedLongLong(dblVal, t))
                {
                    failures++;
                    Console.WriteLine($"ConvertDoubleToUnsignedLongLong NativeCompilerBehavior(managed) {t} {value} {dblVal} {Managed.ConvertDoubleToUnsignedLongLong(dblVal, FPtoIntegerConversionType.CONVERT_NATIVECOMPILERBEHAVIOR)} != {Managed.ConvertDoubleToUnsignedLongLong(dblVal, t)}");
                }

                Vector<float> vFloat = new Vector<float>((float)dblVal);
                Vector<double> vDouble = new Vector<double>(dblVal);

                if (Managed.ConvertToVectorInt(vFloat, t) != Vector.ConvertToInt32(vFloat))
                {
                    failures++;
                    Console.WriteLine($"Managed.ConvertToVectorInt(vFloat, t) != Vector.ConvertToInt32(vFloat) {t} {value} {dblVal} {Managed.ConvertToVectorInt(vFloat, t)} != {Vector.ConvertToInt32(vFloat)}");
                }
                if (Managed.ConvertToVectorUInt(vFloat, t) != Vector.ConvertToUInt32(vFloat))
                {
                    failures++;
                    Console.WriteLine($"Managed.ConvertToVectorUInt(vFloat, t) != Vector.ConvertToUInt32(vFloat) {t} {value} {dblVal} {Managed.ConvertToVectorUInt(vFloat, t)} != {Vector.ConvertToUInt32(vFloat)}");
                }
                if (Managed.ConvertToVectorLong(vDouble, t) != Vector.ConvertToInt64(vDouble))
                {
                    failures++;
                    Console.WriteLine($"Managed.ConvertToVectorLong(vDouble, t) != Vector.ConvertToInt64(vDouble) {t} {value} {dblVal} {Managed.ConvertToVectorLong(vDouble, t)} != {Vector.ConvertToInt64(vDouble)}");
                }
                if (Managed.ConvertToVectorULong(vDouble, t) != Vector.ConvertToUInt64(vDouble))
                {
                    failures++;
                    Console.WriteLine($"Managed.ConvertToVectorULong(vDouble, t) != Vector.ConvertToUInt64(vDouble) {t} {value} {dblVal} {Managed.ConvertToVectorULong(vDouble, t)} != {Vector.ConvertToUInt64(vDouble)}");
                }
            }

            if (failures > 100)
            {
                Console.WriteLine("Encountered 100 failures");
                throw new Exception();
            }
        }

        static int Main(string[] args)
        {
            switch (RuntimeInformation.ProcessArchitecture)
            {
                case Architecture.X86:
                case Architecture.X64:
                    Program.ManagedConversionRule = FPtoIntegerConversionType.CONVERT_MANAGED_BACKWARD_COMPATIBLE_X86_X64;
                    break;

                case Architecture.Arm:
                case Architecture.Arm64:
                    Program.ManagedConversionRule = FPtoIntegerConversionType.CONVERT_SENTINEL;
                    break;
            }
            Console.WriteLine($"Expected managed float behavior is {Program.ManagedConversionRule} Execute with parameter to adjust");
            if (args.Length > 0)
            {
                if (!Enum.TryParse(args[0], out ManagedConversionRule))
                {
                    Console.WriteLine($"Unable to parse {args[0]}");
                    return 1;
                }
            }
            Console.WriteLine("Specific test cases");

            TestBitValue(0, 9223372036854777856.0);
            TestBitValue(0, -9223372036854775808.0);
            TestBitValue(0, -2147483649.0);
            TestBitValue(0, -2147483648.999999523162841796875);
            TestBitValue(0, -2147483648.0);
            TestBitValue(0, -1.0);
            TestBitValue(0, -0.99999999999999988897769753748434595763683319091796875);
            TestBitValue(0, 0.0);
            TestBitValue(0, -0.0);
            TestBitValue(0, 0.99999999999999988897769753748434595763683319091796875);
            TestBitValue(0, 1.0);
            TestBitValue(0, 2147483647.9999997615814208984375);
            TestBitValue(0, 2147483648.0);
            TestBitValue(0, 4294967295.999999523162841796875);
            TestBitValue(0, 4294967296.0);
            TestBitValue(0, 9223372036854774784.0);
            TestBitValue(0, 9223372036854775808.0);
            TestBitValue(0, 18446744073709549568.0);
            TestBitValue(0, 18446744073709551616.0);

            const int increment = 997; // Largest prime under 1000, use 1 for exhaustive search
            Console.WriteLine($"Exhaustive scan of first {increment + 1} floats");
            uint bitValue = 0;
            for (bitValue = 0; bitValue <= increment; bitValue++)
            {
                TestBitValue(bitValue);
            }

            Console.WriteLine($"Walk through rest of 32bit float space skipping {increment} floats at a time (swap to 1 float at a time for a more exhaustive scan");
            int i = 0;
            for (bitValue = increment + 1; bitValue > increment; bitValue+=increment)
            {
                if (((++i) % 1_000_000) == 0)
                    Console.Write(".");
                TestBitValue(bitValue);
            }

            if (failures > 0)
            {
                Console.WriteLine($"{failures} failures");
                return 10;
            }

            return 100;
        }
    }
}
