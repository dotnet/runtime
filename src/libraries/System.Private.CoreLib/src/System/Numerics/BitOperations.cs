// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.Arm;
using System.Runtime.Intrinsics.X86;

using Internal.Runtime.CompilerServices;

// Some routines inspired by the Stanford Bit Twiddling Hacks by Sean Eron Anderson:
// http://graphics.stanford.edu/~seander/bithacks.html

namespace System.Numerics
{
    /// <summary>
    /// Utility methods for intrinsic bit-twiddling operations.
    /// The methods use hardware intrinsics when available on the underlying platform,
    /// otherwise they use optimized software fallbacks.
    /// </summary>
    public static class BitOperations
    {
        // C# no-alloc optimization that directly wraps the data section of the dll (similar to string constants)
        // https://github.com/dotnet/roslyn/pull/24621

        private static ReadOnlySpan<byte> TrailingZeroCountDeBruijn => new byte[32]
        {
            00, 01, 28, 02, 29, 14, 24, 03,
            30, 22, 20, 15, 25, 17, 04, 08,
            31, 27, 13, 23, 21, 19, 16, 07,
            26, 12, 18, 06, 11, 05, 10, 09
        };

        private static ReadOnlySpan<byte> Log2DeBruijn => new byte[32]
        {
            00, 09, 01, 10, 13, 21, 02, 29,
            11, 14, 16, 18, 22, 25, 03, 30,
            08, 12, 20, 28, 15, 17, 24, 07,
            19, 27, 23, 06, 26, 05, 04, 31
        };

        private static ReadOnlySpan<uint> CrcTable => new uint[]
        {
            0, 1996959894, 3993919788, 2567524794, 124634137, 1886057615, 3915621685, 2657392035,
            249268274, 2044508324, 3772115230, 2547177864, 162941995, 2125561021, 3887607047, 2428444049,
            498536548, 1789927666, 4089016648, 2227061214, 450548861, 1843258603, 4107580753, 2211677639,
            325883990, 1684777152, 4251122042, 2321926636, 335633487, 1661365465, 4195302755, 2366115317,
            997073096, 1281953886, 3579855332, 2724688242, 1006888145, 1258607687, 3524101629, 2768942443,
            901097722, 1119000684, 3686517206, 2898065728, 853044451, 1172266101, 3705015759, 2882616665,
            651767980, 1373503546, 3369554304, 3218104598, 565507253, 1454621731, 3485111705, 3099436303,
            671266974, 1594198024, 3322730930, 2970347812, 795835527, 1483230225, 3244367275, 3060149565,
            1994146192, 31158534, 2563907772, 4023717930, 1907459465, 112637215, 2680153253, 3904427059,
            2013776290, 251722036, 2517215374, 3775830040, 2137656763, 141376813, 2439277719, 3865271297,
            1802195444, 476864866, 2238001368, 4066508878, 1812370925, 453092731, 2181625025, 4111451223,
            1706088902, 314042704, 2344532202, 4240017532, 1658658271, 366619977, 2362670323, 4224994405,
            1303535960, 984961486, 2747007092, 3569037538, 1256170817, 1037604311, 2765210733, 3554079995,
            1131014506, 879679996, 2909243462, 3663771856, 1141124467, 855842277, 2852801631, 3708648649,
            1342533948, 654459306, 3188396048, 3373015174, 1466479909, 544179635, 3110523913, 3462522015,
            1591671054, 702138776, 2966460450, 3352799412, 1504918807, 783551873, 3082640443, 3233442989,
            3988292384, 2596254646, 62317068, 1957810842, 3939845945, 2647816111, 81470997, 1943803523,
            3814918930, 2489596804, 225274430, 2053790376, 3826175755, 2466906013, 167816743, 2097651377,
            4027552580, 2265490386, 503444072, 1762050814, 4150417245, 2154129355, 426522225, 1852507879,
            4275313526, 2312317920, 282753626, 1742555852, 4189708143, 2394877945, 397917763, 1622183637,
            3604390888, 2714866558, 953729732, 1340076626, 3518719985, 2797360999, 1068828381, 1219638859,
            3624741850, 2936675148, 906185462, 1090812512, 3747672003, 2825379669, 829329135, 1181335161,
            3412177804, 3160834842, 628085408, 1382605366, 3423369109, 3138078467, 570562233, 1426400815,
            3317316542, 2998733608, 733239954, 1555261956, 3268935591, 3050360625, 752459403, 1541320221,
            2607071920, 3965973030, 1969922972, 40735498, 2617837225, 3943577151, 1913087877, 83908371,
            2512341634, 3803740692, 2075208622, 213261112, 2463272603, 3855990285, 2094854071, 198958881,
            2262029012, 4057260610, 1759359992, 534414190, 2176718541, 4139329115, 1873836001, 414664567,
            2282248934, 4279200368, 1711684554, 285281116, 2405801727, 4167216745, 1634467795, 376229701,
            2685067896, 3608007406, 1308918612, 956543938, 2808555105, 3495958263, 1231636301, 1047427035,
            2932959818, 3654703836, 1088359270, 936918000, 2847714899, 3736837829, 1202900863, 817233897,
            3183342108, 3401237130, 1404277552, 615818150, 3134207493, 3453421203, 1423857449, 601450431,
            3009837614, 3294710456, 1567103746, 711928724, 3020668471, 3272380065, 1510334235, 755167117,
        };

        /// <summary>
        /// Evaluate whether a given integral value is a power of 2.
        /// </summary>
        /// <param name="value">The value.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsPow2(int value) => (value & (value - 1)) == 0 && value > 0;

        /// <summary>
        /// Evaluate whether a given integral value is a power of 2.
        /// </summary>
        /// <param name="value">The value.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [CLSCompliant(false)]
        public static bool IsPow2(uint value) => (value & (value - 1)) == 0 && value != 0;

        /// <summary>
        /// Evaluate whether a given integral value is a power of 2.
        /// </summary>
        /// <param name="value">The value.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsPow2(long value) => (value & (value - 1)) == 0 && value > 0;

        /// <summary>
        /// Evaluate whether a given integral value is a power of 2.
        /// </summary>
        /// <param name="value">The value.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [CLSCompliant(false)]
        public static bool IsPow2(ulong value) => (value & (value - 1)) == 0 && value != 0;

        /// <summary>
        /// Evaluate whether a given integral value is a power of 2.
        /// </summary>
        /// <param name="value">The value.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsPow2(nint value) => (value & (value - 1)) == 0 && value > 0;

        /// <summary>
        /// Evaluate whether a given integral value is a power of 2.
        /// </summary>
        /// <param name="value">The value.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [CLSCompliant(false)]
        public static bool IsPow2(nuint value) => (value & (value - 1)) == 0 && value != 0;

        /// <summary>Round the given integral value up to a power of 2.</summary>
        /// <param name="value">The value.</param>
        /// <returns>
        /// The smallest power of 2 which is greater than or equal to <paramref name="value"/>.
        /// If <paramref name="value"/> is 0 or the result overflows, returns 0.
        /// </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [CLSCompliant(false)]
        public static uint RoundUpToPowerOf2(uint value)
        {
            if (Lzcnt.IsSupported || ArmBase.IsSupported || X86Base.IsSupported)
            {
#if TARGET_64BIT
                return (uint)(0x1_0000_0000ul >> LeadingZeroCount(value - 1));
#else
                int shift = 32 - LeadingZeroCount(value - 1);
                return (1u ^ (uint)(shift >> 5)) << shift;
#endif
            }

            // Based on https://graphics.stanford.edu/~seander/bithacks.html#RoundUpPowerOf2
            --value;
            value |= value >> 1;
            value |= value >> 2;
            value |= value >> 4;
            value |= value >> 8;
            value |= value >> 16;
            return value + 1;
        }

        /// <summary>
        /// Round the given integral value up to a power of 2.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>
        /// The smallest power of 2 which is greater than or equal to <paramref name="value"/>.
        /// If <paramref name="value"/> is 0 or the result overflows, returns 0.
        /// </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [CLSCompliant(false)]
        public static ulong RoundUpToPowerOf2(ulong value)
        {
            if (Lzcnt.X64.IsSupported || ArmBase.Arm64.IsSupported)
            {
                int shift = 64 - LeadingZeroCount(value - 1);
                return (1ul ^ (ulong)(shift >> 6)) << shift;
            }

            // Based on https://graphics.stanford.edu/~seander/bithacks.html#RoundUpPowerOf2
            --value;
            value |= value >> 1;
            value |= value >> 2;
            value |= value >> 4;
            value |= value >> 8;
            value |= value >> 16;
            value |= value >> 32;
            return value + 1;
        }

        /// <summary>
        /// Round the given integral value up to a power of 2.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>
        /// The smallest power of 2 which is greater than or equal to <paramref name="value"/>.
        /// If <paramref name="value"/> is 0 or the result overflows, returns 0.
        /// </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [CLSCompliant(false)]
        public static nuint RoundUpToPowerOf2(nuint value)
        {
#if TARGET_64BIT
            return (nuint)RoundUpToPowerOf2((ulong)value);
#else
            return (nuint)RoundUpToPowerOf2((uint)value);
#endif
        }

        /// <summary>
        /// Count the number of leading zero bits in a mask.
        /// Similar in behavior to the x86 instruction LZCNT.
        /// </summary>
        /// <param name="value">The value.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [CLSCompliant(false)]
        public static int LeadingZeroCount(uint value)
        {
            if (Lzcnt.IsSupported)
            {
                // LZCNT contract is 0->32
                return (int)Lzcnt.LeadingZeroCount(value);
            }

            if (ArmBase.IsSupported)
            {
                return ArmBase.LeadingZeroCount(value);
            }

            // Unguarded fallback contract is 0->31, BSR contract is 0->undefined
            if (value == 0)
            {
                return 32;
            }

            if (X86Base.IsSupported)
            {
                // LZCNT returns index starting from MSB, whereas BSR gives the index from LSB.
                // 31 ^ BSR here is equivalent to 31 - BSR since the BSR result is always between 0 and 31.
                // This saves an instruction, as subtraction from constant requires either MOV/SUB or NEG/ADD.
                return 31 ^ (int)X86Base.BitScanReverse(value);
            }

            return 31 ^ Log2SoftwareFallback(value);
        }

        /// <summary>
        /// Count the number of leading zero bits in a mask.
        /// Similar in behavior to the x86 instruction LZCNT.
        /// </summary>
        /// <param name="value">The value.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [CLSCompliant(false)]
        public static int LeadingZeroCount(ulong value)
        {
            if (Lzcnt.X64.IsSupported)
            {
                // LZCNT contract is 0->64
                return (int)Lzcnt.X64.LeadingZeroCount(value);
            }

            if (ArmBase.Arm64.IsSupported)
            {
                return ArmBase.Arm64.LeadingZeroCount(value);
            }

            if (X86Base.X64.IsSupported)
            {
                // BSR contract is 0->undefined
                return value == 0 ? 64 : 63 ^ (int)X86Base.X64.BitScanReverse(value);
            }

            uint hi = (uint)(value >> 32);

            if (hi == 0)
            {
                return 32 + LeadingZeroCount((uint)value);
            }

            return LeadingZeroCount(hi);
        }

        /// <summary>
        /// Count the number of leading zero bits in a mask.
        /// Similar in behavior to the x86 instruction LZCNT.
        /// </summary>
        /// <param name="value">The value.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [CLSCompliant(false)]
        public static int LeadingZeroCount(nuint value)
        {
#if TARGET_64BIT
            return LeadingZeroCount((ulong)value);
#else
            return LeadingZeroCount((uint)value);
#endif
        }

        /// <summary>
        /// Returns the integer (floor) log of the specified value, base 2.
        /// Note that by convention, input value 0 returns 0 since log(0) is undefined.
        /// </summary>
        /// <param name="value">The value.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [CLSCompliant(false)]
        public static int Log2(uint value)
        {
            // The 0->0 contract is fulfilled by setting the LSB to 1.
            // Log(1) is 0, and setting the LSB for values > 1 does not change the log2 result.
            value |= 1;

            // value    lzcnt   actual  expected
            // ..0001   31      31-31    0
            // ..0010   30      31-30    1
            // 0010..    2      31-2    29
            // 0100..    1      31-1    30
            // 1000..    0      31-0    31
            if (Lzcnt.IsSupported)
            {
                return 31 ^ (int)Lzcnt.LeadingZeroCount(value);
            }

            if (ArmBase.IsSupported)
            {
                return 31 ^ ArmBase.LeadingZeroCount(value);
            }

            // BSR returns the log2 result directly. However BSR is slower than LZCNT
            // on AMD processors, so we leave it as a fallback only.
            if (X86Base.IsSupported)
            {
                return (int)X86Base.BitScanReverse(value);
            }

            // Fallback contract is 0->0
            return Log2SoftwareFallback(value);
        }

        /// <summary>
        /// Returns the integer (floor) log of the specified value, base 2.
        /// Note that by convention, input value 0 returns 0 since log(0) is undefined.
        /// </summary>
        /// <param name="value">The value.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [CLSCompliant(false)]
        public static int Log2(ulong value)
        {
            value |= 1;

            if (Lzcnt.X64.IsSupported)
            {
                return 63 ^ (int)Lzcnt.X64.LeadingZeroCount(value);
            }

            if (ArmBase.Arm64.IsSupported)
            {
                return 63 ^ ArmBase.Arm64.LeadingZeroCount(value);
            }

            if (X86Base.X64.IsSupported)
            {
                return (int)X86Base.X64.BitScanReverse(value);
            }

            uint hi = (uint)(value >> 32);

            if (hi == 0)
            {
                return Log2((uint)value);
            }

            return 32 + Log2(hi);
        }

        /// <summary>
        /// Returns the integer (floor) log of the specified value, base 2.
        /// Note that by convention, input value 0 returns 0 since log(0) is undefined.
        /// </summary>
        /// <param name="value">The value.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [CLSCompliant(false)]
        public static int Log2(nuint value)
        {
#if TARGET_64BIT
            return Log2((ulong)value);
#else
            return Log2((uint)value);
#endif
        }

        /// <summary>
        /// Returns the integer (floor) log of the specified value, base 2.
        /// Note that by convention, input value 0 returns 0 since Log(0) is undefined.
        /// Does not directly use any hardware intrinsics, nor does it incur branching.
        /// </summary>
        /// <param name="value">The value.</param>
        private static int Log2SoftwareFallback(uint value)
        {
            // No AggressiveInlining due to large method size
            // Has conventional contract 0->0 (Log(0) is undefined)

            // Fill trailing zeros with ones, eg 00010010 becomes 00011111
            value |= value >> 01;
            value |= value >> 02;
            value |= value >> 04;
            value |= value >> 08;
            value |= value >> 16;

            // uint.MaxValue >> 27 is always in range [0 - 31] so we use Unsafe.AddByteOffset to avoid bounds check
            return Unsafe.AddByteOffset(
                // Using deBruijn sequence, k=2, n=5 (2^5=32) : 0b_0000_0111_1100_0100_1010_1100_1101_1101u
                ref MemoryMarshal.GetReference(Log2DeBruijn),
                // uint|long -> IntPtr cast on 32-bit platforms does expensive overflow checks not needed here
                (IntPtr)(int)((value * 0x07C4ACDDu) >> 27));
        }

        /// <summary>Returns the integer (ceiling) log of the specified value, base 2.</summary>
        /// <param name="value">The value.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static int Log2Ceiling(uint value)
        {
            int result = Log2(value);
            if (PopCount(value) != 1)
            {
                result++;
            }
            return result;
        }

        /// <summary>Returns the integer (ceiling) log of the specified value, base 2.</summary>
        /// <param name="value">The value.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static int Log2Ceiling(ulong value)
        {
            int result = Log2(value);
            if (PopCount(value) != 1)
            {
                result++;
            }
            return result;
        }

        /// <summary>
        /// Returns the population count (number of bits set) of a mask.
        /// Similar in behavior to the x86 instruction POPCNT.
        /// </summary>
        /// <param name="value">The value.</param>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [CLSCompliant(false)]
        public static int PopCount(uint value)
        {
            if (Popcnt.IsSupported)
            {
                return (int)Popcnt.PopCount(value);
            }

            if (AdvSimd.Arm64.IsSupported)
            {
                // PopCount works on vector so convert input value to vector first.

                Vector64<uint> input = Vector64.CreateScalar(value);
                Vector64<byte> aggregated = AdvSimd.Arm64.AddAcross(AdvSimd.PopCount(input.AsByte()));
                return aggregated.ToScalar();
            }

            return SoftwareFallback(value);

            static int SoftwareFallback(uint value)
            {
                const uint c1 = 0x_55555555u;
                const uint c2 = 0x_33333333u;
                const uint c3 = 0x_0F0F0F0Fu;
                const uint c4 = 0x_01010101u;

                value -= (value >> 1) & c1;
                value = (value & c2) + ((value >> 2) & c2);
                value = (((value + (value >> 4)) & c3) * c4) >> 24;

                return (int)value;
            }
        }

        /// <summary>
        /// Returns the population count (number of bits set) of a mask.
        /// Similar in behavior to the x86 instruction POPCNT.
        /// </summary>
        /// <param name="value">The value.</param>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [CLSCompliant(false)]
        public static int PopCount(ulong value)
        {
            if (Popcnt.X64.IsSupported)
            {
                return (int)Popcnt.X64.PopCount(value);
            }

            if (AdvSimd.Arm64.IsSupported)
            {
                // PopCount works on vector so convert input value to vector first.
                Vector64<ulong> input = Vector64.Create(value);
                Vector64<byte> aggregated = AdvSimd.Arm64.AddAcross(AdvSimd.PopCount(input.AsByte()));
                return aggregated.ToScalar();
            }

#if TARGET_32BIT
            return PopCount((uint)value) // lo
                + PopCount((uint)(value >> 32)); // hi
#else
            return SoftwareFallback(value);

            static int SoftwareFallback(ulong value)
            {
                const ulong c1 = 0x_55555555_55555555ul;
                const ulong c2 = 0x_33333333_33333333ul;
                const ulong c3 = 0x_0F0F0F0F_0F0F0F0Ful;
                const ulong c4 = 0x_01010101_01010101ul;

                value -= (value >> 1) & c1;
                value = (value & c2) + ((value >> 2) & c2);
                value = (((value + (value >> 4)) & c3) * c4) >> 56;

                return (int)value;
            }
#endif
        }

        /// <summary>
        /// Returns the population count (number of bits set) of a mask.
        /// Similar in behavior to the x86 instruction POPCNT.
        /// </summary>
        /// <param name="value">The value.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [CLSCompliant(false)]
        public static int PopCount(nuint value)
        {
#if TARGET_64BIT
            return PopCount((ulong)value);
#else
            return PopCount((uint)value);
#endif
        }

        /// <summary>
        /// Count the number of trailing zero bits in an integer value.
        /// Similar in behavior to the x86 instruction TZCNT.
        /// </summary>
        /// <param name="value">The value.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int TrailingZeroCount(int value)
            => TrailingZeroCount((uint)value);

        /// <summary>
        /// Count the number of trailing zero bits in an integer value.
        /// Similar in behavior to the x86 instruction TZCNT.
        /// </summary>
        /// <param name="value">The value.</param>
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int TrailingZeroCount(uint value)
        {
            if (Bmi1.IsSupported)
            {
                // TZCNT contract is 0->32
                return (int)Bmi1.TrailingZeroCount(value);
            }

            if (ArmBase.IsSupported)
            {
                return ArmBase.LeadingZeroCount(ArmBase.ReverseElementBits(value));
            }

            // Unguarded fallback contract is 0->0, BSF contract is 0->undefined
            if (value == 0)
            {
                return 32;
            }

            if (X86Base.IsSupported)
            {
                return (int)X86Base.BitScanForward(value);
            }

            // uint.MaxValue >> 27 is always in range [0 - 31] so we use Unsafe.AddByteOffset to avoid bounds check
            return Unsafe.AddByteOffset(
                // Using deBruijn sequence, k=2, n=5 (2^5=32) : 0b_0000_0111_0111_1100_1011_0101_0011_0001u
                ref MemoryMarshal.GetReference(TrailingZeroCountDeBruijn),
                // uint|long -> IntPtr cast on 32-bit platforms does expensive overflow checks not needed here
                (IntPtr)(int)(((value & (uint)-(int)value) * 0x077CB531u) >> 27)); // Multi-cast mitigates redundant conv.u8
        }

        /// <summary>
        /// Count the number of trailing zero bits in a mask.
        /// Similar in behavior to the x86 instruction TZCNT.
        /// </summary>
        /// <param name="value">The value.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int TrailingZeroCount(long value)
            => TrailingZeroCount((ulong)value);

        /// <summary>
        /// Count the number of trailing zero bits in a mask.
        /// Similar in behavior to the x86 instruction TZCNT.
        /// </summary>
        /// <param name="value">The value.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [CLSCompliant(false)]
        public static int TrailingZeroCount(ulong value)
        {
            if (Bmi1.X64.IsSupported)
            {
                // TZCNT contract is 0->64
                return (int)Bmi1.X64.TrailingZeroCount(value);
            }

            if (ArmBase.Arm64.IsSupported)
            {
                return ArmBase.Arm64.LeadingZeroCount(ArmBase.Arm64.ReverseElementBits(value));
            }

            if (X86Base.X64.IsSupported)
            {
                // BSF contract is 0->undefined
                return value == 0 ? 64 : (int)X86Base.X64.BitScanForward(value);
            }

            uint lo = (uint)value;

            if (lo == 0)
            {
                return 32 + TrailingZeroCount((uint)(value >> 32));
            }

            return TrailingZeroCount(lo);
        }

        /// <summary>
        /// Count the number of trailing zero bits in a mask.
        /// Similar in behavior to the x86 instruction TZCNT.
        /// </summary>
        /// <param name="value">The value.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int TrailingZeroCount(nint value)
            => TrailingZeroCount((nuint)value);

        /// <summary>
        /// Count the number of trailing zero bits in a mask.
        /// Similar in behavior to the x86 instruction TZCNT.
        /// </summary>
        /// <param name="value">The value.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [CLSCompliant(false)]
        public static int TrailingZeroCount(nuint value)
        {
#if TARGET_64BIT
            return TrailingZeroCount((ulong)value);
#else
            return TrailingZeroCount((uint)value);
#endif
        }

        /// <summary>
        /// Rotates the specified value left by the specified number of bits.
        /// Similar in behavior to the x86 instruction ROL.
        /// </summary>
        /// <param name="value">The value to rotate.</param>
        /// <param name="offset">The number of bits to rotate by.
        /// Any value outside the range [0..31] is treated as congruent mod 32.</param>
        /// <returns>The rotated value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [CLSCompliant(false)]
        public static uint RotateLeft(uint value, int offset)
            => (value << offset) | (value >> (32 - offset));

        /// <summary>
        /// Rotates the specified value left by the specified number of bits.
        /// Similar in behavior to the x86 instruction ROL.
        /// </summary>
        /// <param name="value">The value to rotate.</param>
        /// <param name="offset">The number of bits to rotate by.
        /// Any value outside the range [0..63] is treated as congruent mod 64.</param>
        /// <returns>The rotated value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [CLSCompliant(false)]
        public static ulong RotateLeft(ulong value, int offset)
            => (value << offset) | (value >> (64 - offset));

        /// <summary>
        /// Rotates the specified value left by the specified number of bits.
        /// Similar in behavior to the x86 instruction ROL.
        /// </summary>
        /// <param name="value">The value to rotate.</param>
        /// <param name="offset">The number of bits to rotate by.
        /// Any value outside the range [0..31] is treated as congruent mod 32 on a 32-bit process,
        /// and any value outside the range [0..63] is treated as congruent mod 64 on a 64-bit process.</param>
        /// <returns>The rotated value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [CLSCompliant(false)]
        public static nuint RotateLeft(nuint value, int offset)
        {
#if TARGET_64BIT
            return (nuint)RotateLeft((ulong)value, offset);
#else
            return (nuint)RotateLeft((uint)value, offset);
#endif
        }

        /// <summary>
        /// Rotates the specified value right by the specified number of bits.
        /// Similar in behavior to the x86 instruction ROR.
        /// </summary>
        /// <param name="value">The value to rotate.</param>
        /// <param name="offset">The number of bits to rotate by.
        /// Any value outside the range [0..31] is treated as congruent mod 32.</param>
        /// <returns>The rotated value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [CLSCompliant(false)]
        public static uint RotateRight(uint value, int offset)
            => (value >> offset) | (value << (32 - offset));

        /// <summary>
        /// Rotates the specified value right by the specified number of bits.
        /// Similar in behavior to the x86 instruction ROR.
        /// </summary>
        /// <param name="value">The value to rotate.</param>
        /// <param name="offset">The number of bits to rotate by.
        /// Any value outside the range [0..63] is treated as congruent mod 64.</param>
        /// <returns>The rotated value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [CLSCompliant(false)]
        public static ulong RotateRight(ulong value, int offset)
            => (value >> offset) | (value << (64 - offset));

        /// <summary>
        /// Rotates the specified value right by the specified number of bits.
        /// Similar in behavior to the x86 instruction ROR.
        /// </summary>
        /// <param name="value">The value to rotate.</param>
        /// <param name="offset">The number of bits to rotate by.
        /// Any value outside the range [0..31] is treated as congruent mod 32 on a 32-bit process,
        /// and any value outside the range [0..63] is treated as congruent mod 64 on a 64-bit process.</param>
        /// <returns>The rotated value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [CLSCompliant(false)]
        public static nuint RotateRight(nuint value, int offset)
        {
#if TARGET_64BIT
            return (nuint)RotateRight((ulong)value, offset);
#else
            return (nuint)RotateRight((uint)value, offset);
#endif
        }

        /// <summary>
        /// Calculates the CRC (Cyclic redundancy check) checksum.
        ///
        /// Uses following Hardware Intrinsics, if supported:
        /// * uint32_t __crc32b (uint32_t a, uint8_t b)
        /// * uint32_t __crc32cb (uint32_t a, uint8_t b)
        /// </summary>
        /// <param name="crc">The base value to calculate checksum on</param>
        /// <param name="data">The checksum data</param>
        /// <returns>The CRC-checksum</returns>
        [CLSCompliant(false)]
        public static uint Crc32C(uint crc, byte data)
        {
            if (Sse42.IsSupported)
            {
                // uint32_t __crc32b (uint32_t a, uint8_t b)
                return Sse42.Crc32(crc, data);
            }
            if (Crc32.IsSupported)
            {
                // uint32_t __crc32cb (uint32_t a, uint8_t b)
                return Crc32.ComputeCrc32C(crc, data);
            }


            // Software fallback
            int tableIndex = (int)((crc ^ data) & 0xFF);
            crc = CrcTable[tableIndex] ^ (crc >> 8);

            return crc ^ 0xFFFFFFFF;
        }

        /// <summary>
        /// Calculates the CRC (Cyclic redundancy check) checksum.
        ///
        /// Uses following Hardware Intrinsics, if supported:
        /// * unsigned int _mm_crc32_u16 (unsigned int crc, unsigned short v)
        /// * uint32_t __crc32ch (uint32_t a, uint16_t b)
        /// </summary>
        /// <param name="crc">The base value to calculate checksum on</param>
        /// <param name="data">The checksum data</param>
        /// <returns>The CRC-checksum</returns>
        [CLSCompliant(false)]
        public static uint Crc32C(uint crc, ushort data)
        {
            if (Sse42.IsSupported)
            {
                // unsigned int _mm_crc32_u16 (unsigned int crc, unsigned short v)
                return Sse42.Crc32(crc, data);
            }
            if (Crc32.IsSupported)
            {
                // uint32_t __crc32ch (uint32_t a, uint16_t b)
                return Crc32.ComputeCrc32C(crc, data);
            }


            // Software fallback

            Span<byte> bytes = stackalloc byte[sizeof(ushort)];
            Unsafe.As<byte, ushort>(ref bytes[0]) = data;

            foreach (byte b in bytes)
            {
                int tableIndex = (int)((crc ^ b) & 0xFF);
                crc = CrcTable[tableIndex] ^ (crc >> 8);
            }

            return crc ^ 0xFFFFFFFF;
        }

        /// <summary>
        /// Calculates the CRC (Cyclic redundancy check) checksum.
        ///
        /// Uses following Hardware Intrinsics, if supported:
        /// * unsigned int _mm_crc32_u32 (unsigned int crc, unsigned int v)
        /// * uint32_t __crc32w (uint32_t a, uint32_t b)
        /// </summary>
        /// <param name="crc">The base value to calculate checksum on</param>
        /// <param name="data">The checksum data</param>
        /// <returns>The CRC-checksum</returns>
        [CLSCompliant(false)]
        public static uint Crc32C(uint crc, uint data)
        {
            if (Sse42.IsSupported)
            {
                // unsigned int _mm_crc32_u32 (unsigned int crc, unsigned int v)
                return Sse42.Crc32(crc, data);
            }
            if (Crc32.IsSupported)
            {
                // uint32_t __crc32w (uint32_t a, uint32_t b)
                return Crc32.ComputeCrc32(crc, data);
            }


            // Software fallback

            Span<byte> bytes = stackalloc byte[sizeof(uint)];
            Unsafe.As<byte, uint>(ref bytes[0]) = data;

            foreach (byte b in bytes)
            {
                int tableIndex = (int)((crc ^ b) & 0xFF);
                crc = CrcTable[tableIndex] ^ (crc >> 8);
            }

            return crc ^ 0xFFFFFFFF;
        }

        /// <summary>
        /// Calculates the CRC (Cyclic redundancy check) checksum.
        ///
        /// Uses following Hardware Intrinsics, if supported:
        /// * unsigned __int64 _mm_crc32_u64 (unsigned __int64 crc, unsigned __int64 v)
        /// * uint32_t __crc32d (uint32_t a, uint64_t b)
        /// </summary>
        /// <param name="crc">The base value to calculate checksum on</param>
        /// <param name="data">The checksum data</param>
        /// <returns>The CRC-checksum</returns>
        [CLSCompliant(false)]
        public static uint Crc32C(uint crc, ulong data)
        {
            if (Sse42.X64.IsSupported)
            {
                // unsigned __int64 _mm_crc32_u64 (unsigned __int64 crc, unsigned __int64 v)
                return (uint)Sse42.X64.Crc32(crc, data);
            }

            if (Crc32.Arm64.IsSupported)
            {
                // uint32_t __crc32d (uint32_t a, uint64_t b)
                return Crc32.Arm64.ComputeCrc32(crc, data);
            }

            // Software fallback

            Span<byte> bytes = stackalloc byte[sizeof(ulong)];
            Unsafe.As<byte, ulong>(ref bytes[0]) = data;

            foreach (byte b in bytes)
            {
                int tableIndex = (int)((crc ^ b) & 0xFF);
                crc = CrcTable[tableIndex] ^ (crc >> 8);
            }

            return crc ^ 0xFFFFFFFF;
        }
    }
}
