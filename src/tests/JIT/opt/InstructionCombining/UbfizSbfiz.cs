// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

public class Program
{
    public const int ShiftBy = 5;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static T ToVar<T>(T t) => t;

    internal static void AssertTrue(bool cond, [CallerLineNumber] int line = 0)
    {
        if (!cond) 
            throw new InvalidOperationException($"Test failed at line {line}.");
    }

    // Tests for https://github.com/dotnet/runtime/pull/61045 optimization
    [Fact]
    public static int TestEntryPoint()
    {
        unchecked
        {
            long[] testData =
            {
                -1, -2, -3, -8, -128, -129, -254, -255, -256,
                0, 1, 2, 3, 8, 128, 129, 254, 255, 256,
                short.MinValue + 1, short.MinValue, short.MinValue + 1,
                short.MaxValue + 1, short.MaxValue, short.MaxValue + 1,
                int.MinValue + 1, int.MinValue, int.MinValue + 1,
                int.MaxValue + 1, int.MaxValue, int.MaxValue + 1,
                long.MinValue + 1, long.MinValue, long.MinValue + 1,
                long.MaxValue + 1, long.MaxValue, long.MaxValue + 1,
                ushort.MaxValue, uint.MaxValue, (long)ulong.MaxValue
            };

            foreach (long t in testData)
            {
                AssertTrue(Tests_byte.Test_byte_to_byte((byte)t));
                AssertTrue(Tests_byte.Test_byte_to_sbyte((byte)t));
                AssertTrue(Tests_byte.Test_byte_to_ushort((byte)t));
                AssertTrue(Tests_byte.Test_byte_to_short((byte)t));
                AssertTrue(Tests_byte.Test_byte_to_uint((byte)t));
                AssertTrue(Tests_byte.Test_byte_to_int((byte)t));
                AssertTrue(Tests_byte.Test_byte_to_ulong((byte)t));
                AssertTrue(Tests_byte.Test_byte_to_long((byte)t));

                AssertTrue(Tests_sbyte.Test_sbyte_to_byte((sbyte)t));
                AssertTrue(Tests_sbyte.Test_sbyte_to_sbyte((sbyte)t));
                AssertTrue(Tests_sbyte.Test_sbyte_to_ushort((sbyte)t));
                AssertTrue(Tests_sbyte.Test_sbyte_to_short((sbyte)t));
                AssertTrue(Tests_sbyte.Test_sbyte_to_uint((sbyte)t));
                AssertTrue(Tests_sbyte.Test_sbyte_to_int((sbyte)t));
                AssertTrue(Tests_sbyte.Test_sbyte_to_ulong((sbyte)t));
                AssertTrue(Tests_sbyte.Test_sbyte_to_long((sbyte)t));

                AssertTrue(Tests_ushort.Test_ushort_to_byte((ushort)t));
                AssertTrue(Tests_ushort.Test_ushort_to_sbyte((ushort)t));
                AssertTrue(Tests_ushort.Test_ushort_to_ushort((ushort)t));
                AssertTrue(Tests_ushort.Test_ushort_to_short((ushort)t));
                AssertTrue(Tests_ushort.Test_ushort_to_uint((ushort)t));
                AssertTrue(Tests_ushort.Test_ushort_to_int((ushort)t));
                AssertTrue(Tests_ushort.Test_ushort_to_ulong((ushort)t));
                AssertTrue(Tests_ushort.Test_ushort_to_long((ushort)t));

                AssertTrue(Tests_short.Test_short_to_byte((short)t));
                AssertTrue(Tests_short.Test_short_to_sbyte((short)t));
                AssertTrue(Tests_short.Test_short_to_ushort((short)t));
                AssertTrue(Tests_short.Test_short_to_short((short)t));
                AssertTrue(Tests_short.Test_short_to_uint((short)t));
                AssertTrue(Tests_short.Test_short_to_int((short)t));
                AssertTrue(Tests_short.Test_short_to_ulong((short)t));
                AssertTrue(Tests_short.Test_short_to_long((short)t));

                AssertTrue(Tests_uint.Test_uint_to_byte((uint)t));
                AssertTrue(Tests_uint.Test_uint_to_sbyte((uint)t));
                AssertTrue(Tests_uint.Test_uint_to_ushort((uint)t));
                AssertTrue(Tests_uint.Test_uint_to_short((uint)t));
                AssertTrue(Tests_uint.Test_uint_to_uint((uint)t));
                AssertTrue(Tests_uint.Test_uint_to_int((uint)t));
                AssertTrue(Tests_uint.Test_uint_to_ulong((uint)t));
                AssertTrue(Tests_uint.Test_uint_to_long((uint)t));

                AssertTrue(Tests_int.Test_int_to_byte((int)t));
                AssertTrue(Tests_int.Test_int_to_sbyte((int)t));
                AssertTrue(Tests_int.Test_int_to_ushort((int)t));
                AssertTrue(Tests_int.Test_int_to_short((int)t));
                AssertTrue(Tests_int.Test_int_to_uint((int)t));
                AssertTrue(Tests_int.Test_int_to_int((int)t));
                AssertTrue(Tests_int.Test_int_to_ulong((int)t));
                AssertTrue(Tests_int.Test_int_to_long((int)t));

                AssertTrue(Tests_ulong.Test_ulong_to_byte((ulong)t));
                AssertTrue(Tests_ulong.Test_ulong_to_sbyte((ulong)t));
                AssertTrue(Tests_ulong.Test_ulong_to_ushort((ulong)t));
                AssertTrue(Tests_ulong.Test_ulong_to_short((ulong)t));
                AssertTrue(Tests_ulong.Test_ulong_to_uint((ulong)t));
                AssertTrue(Tests_ulong.Test_ulong_to_int((ulong)t));
                AssertTrue(Tests_ulong.Test_ulong_to_ulong((ulong)t));
                AssertTrue(Tests_ulong.Test_ulong_to_long((ulong)t));

                AssertTrue(Tests_long.Test_long_to_byte(t));
                AssertTrue(Tests_long.Test_long_to_sbyte(t));
                AssertTrue(Tests_long.Test_long_to_ushort(t));
                AssertTrue(Tests_long.Test_long_to_short(t));
                AssertTrue(Tests_long.Test_long_to_uint(t));
                AssertTrue(Tests_long.Test_long_to_int(t));
                AssertTrue(Tests_long.Test_long_to_ulong(t));
                AssertTrue(Tests_long.Test_long_to_long(t));
            }
        }
        return 100;
    }
}

public class Tests_byte
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool Test_byte_to_byte(byte x)
    {
        unchecked
        {
            return (byte)(x << Program.ShiftBy) ==
                   (byte)(Program.ToVar(x) << Program.ToVar(Program.ShiftBy));
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool Test_byte_to_sbyte(byte x)
    {
        unchecked
        {
            return (sbyte)((sbyte)x << Program.ShiftBy) ==
                   (sbyte)((sbyte)Program.ToVar(x) << Program.ToVar(Program.ShiftBy));
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool Test_byte_to_ushort(byte x)
    {
        unchecked
        {
            return (ushort)(x << Program.ShiftBy) ==
                   (ushort)(Program.ToVar(x) << Program.ToVar(Program.ShiftBy));
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool Test_byte_to_short(byte x)
    {
        unchecked
        {
            return (short)(x << Program.ShiftBy) ==
                   (short)(Program.ToVar(x) << Program.ToVar(Program.ShiftBy));
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool Test_byte_to_uint(byte x)
    {
        return (uint)x << Program.ShiftBy ==
               (uint)Program.ToVar(x) << Program.ToVar(Program.ShiftBy);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool Test_byte_to_int(byte x)
    {
        return x << Program.ShiftBy ==
               Program.ToVar(x) << Program.ToVar(Program.ShiftBy);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool Test_byte_to_ulong(byte x)
    {
        return (ulong)x << Program.ShiftBy ==
               (ulong)Program.ToVar(x) << Program.ToVar(Program.ShiftBy);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool Test_byte_to_long(byte x)
    {
        return (long)x << Program.ShiftBy ==
               (long)Program.ToVar(x) << Program.ToVar(Program.ShiftBy);
    }
}

public class Tests_sbyte
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool Test_sbyte_to_byte(sbyte x)
    {
        unchecked
        {
            return (byte)((byte)x << Program.ShiftBy) ==
                   (byte)((byte)Program.ToVar(x) << Program.ToVar(Program.ShiftBy));
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool Test_sbyte_to_sbyte(sbyte x)
    {
        unchecked
        {
            return (sbyte)(x << Program.ShiftBy) ==
                   (sbyte)(Program.ToVar(x) << Program.ToVar(Program.ShiftBy));
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool Test_sbyte_to_ushort(sbyte x)
    {
        unchecked
        {
            return (ushort)((ushort)x << Program.ShiftBy) ==
                   (ushort)((ushort)Program.ToVar(x) << Program.ToVar(Program.ShiftBy));
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool Test_sbyte_to_short(sbyte x)
    {
        unchecked
        {
            return (short)(x << Program.ShiftBy) ==
                   (short)(Program.ToVar(x) << Program.ToVar(Program.ShiftBy));
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool Test_sbyte_to_uint(sbyte x)
    {
        unchecked
        {
            return (uint)x << Program.ShiftBy ==
                   (uint)Program.ToVar(x) << Program.ToVar(Program.ShiftBy);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool Test_sbyte_to_int(sbyte x)
    {
        return x << Program.ShiftBy ==
               Program.ToVar(x) << Program.ToVar(Program.ShiftBy);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool Test_sbyte_to_ulong(sbyte x)
    {
        unchecked
        {
            return (ulong)x << Program.ShiftBy ==
                   (ulong)Program.ToVar(x) << Program.ToVar(Program.ShiftBy);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool Test_sbyte_to_long(sbyte x)
    {
        return (long)x << Program.ShiftBy ==
               (long)Program.ToVar(x) << Program.ToVar(Program.ShiftBy);
    }
}

public class Tests_ushort
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool Test_ushort_to_byte(ushort x)
    {
        unchecked
        {
            return (byte)((byte)x << Program.ShiftBy) ==
                   (byte)((byte)Program.ToVar(x) << Program.ToVar(Program.ShiftBy));
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool Test_ushort_to_sbyte(ushort x)
    {
        unchecked
        {
            return (sbyte)((sbyte)x << Program.ShiftBy) ==
                   (sbyte)((sbyte)Program.ToVar(x) << Program.ToVar(Program.ShiftBy));
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool Test_ushort_to_ushort(ushort x)
    {
        unchecked
        {
            return (ushort)(x << Program.ShiftBy) ==
                   (ushort)(Program.ToVar(x) << Program.ToVar(Program.ShiftBy));
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool Test_ushort_to_short(ushort x)
    {
        unchecked
        {
            return (short)((short)x << Program.ShiftBy) ==
                   (short)((short)Program.ToVar(x) << Program.ToVar(Program.ShiftBy));
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool Test_ushort_to_uint(ushort x)
    {
        return (uint)x << Program.ShiftBy ==
               (uint)Program.ToVar(x) << Program.ToVar(Program.ShiftBy);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool Test_ushort_to_int(ushort x)
    {
        return x << Program.ShiftBy ==
               Program.ToVar(x) << Program.ToVar(Program.ShiftBy);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool Test_ushort_to_ulong(ushort x)
    {
        return (ulong)x << Program.ShiftBy ==
               (ulong)Program.ToVar(x) << Program.ToVar(Program.ShiftBy);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool Test_ushort_to_long(ushort x)
    {
        return (long)x << Program.ShiftBy ==
               (long)Program.ToVar(x) << Program.ToVar(Program.ShiftBy);
    }
}

public class Tests_short
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool Test_short_to_byte(short x)
    {
        unchecked
        {
            return (byte)((byte)x << Program.ShiftBy) ==
                   (byte)((byte)Program.ToVar(x) << Program.ToVar(Program.ShiftBy));
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool Test_short_to_sbyte(short x)
    {
        unchecked
        {
            return (sbyte)((sbyte)x << Program.ShiftBy) ==
                   (sbyte)((sbyte)Program.ToVar(x) << Program.ToVar(Program.ShiftBy));
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool Test_short_to_ushort(short x)
    {
        unchecked
        {
            return (ushort)((ushort)x << Program.ShiftBy) ==
                   (ushort)((ushort)Program.ToVar(x) << Program.ToVar(Program.ShiftBy));
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool Test_short_to_short(short x)
    {
        unchecked
        {
            return (short)(x << Program.ShiftBy) ==
                   (short)(Program.ToVar(x) << Program.ToVar(Program.ShiftBy));
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool Test_short_to_uint(short x)
    {
        unchecked
        {
            return (uint)x << Program.ShiftBy ==
                   (uint)Program.ToVar(x) << Program.ToVar(Program.ShiftBy);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool Test_short_to_int(short x)
    {
        return x << Program.ShiftBy ==
               Program.ToVar(x) << Program.ToVar(Program.ShiftBy);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool Test_short_to_ulong(short x)
    {
        unchecked
        {
            return (ulong)x << Program.ShiftBy ==
                   (ulong)Program.ToVar(x) << Program.ToVar(Program.ShiftBy);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool Test_short_to_long(short x)
    {
        return (long)x << Program.ShiftBy ==
               (long)Program.ToVar(x) << Program.ToVar(Program.ShiftBy);
    }
}

public class Tests_uint
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool Test_uint_to_byte(uint x)
    {
        unchecked
        {
            return (byte)((byte)x << Program.ShiftBy) ==
                   (byte)((byte)Program.ToVar(x) << Program.ToVar(Program.ShiftBy));
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool Test_uint_to_sbyte(uint x)
    {
        unchecked
        {
            return (sbyte)((sbyte)x << Program.ShiftBy) ==
                   (sbyte)((sbyte)Program.ToVar(x) << Program.ToVar(Program.ShiftBy));
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool Test_uint_to_ushort(uint x)
    {
        unchecked
        {
            return (ushort)((ushort)x << Program.ShiftBy) ==
                   (ushort)((ushort)Program.ToVar(x) << Program.ToVar(Program.ShiftBy));
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool Test_uint_to_short(uint x)
    {
        unchecked
        {
            return (short)((short)x << Program.ShiftBy) ==
                   (short)((short)Program.ToVar(x) << Program.ToVar(Program.ShiftBy));
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool Test_uint_to_uint(uint x)
    {
        return x << Program.ShiftBy ==
               Program.ToVar(x) << Program.ToVar(Program.ShiftBy);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool Test_uint_to_int(uint x)
    {
        unchecked
        {
            return (int)x << Program.ShiftBy ==
                   (int)Program.ToVar(x) << Program.ToVar(Program.ShiftBy);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool Test_uint_to_ulong(uint x)
    {
        return (ulong)x << Program.ShiftBy ==
               (ulong)Program.ToVar(x) << Program.ToVar(Program.ShiftBy);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool Test_uint_to_long(uint x)
    {
        return (long)x << Program.ShiftBy ==
               (long)Program.ToVar(x) << Program.ToVar(Program.ShiftBy);
    }
}

public class Tests_int
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool Test_int_to_byte(int x)
    {
        unchecked
        {
            return (byte)((byte)x << Program.ShiftBy) ==
                   (byte)((byte)Program.ToVar(x) << Program.ToVar(Program.ShiftBy));
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool Test_int_to_sbyte(int x)
    {
        unchecked
        {
            return (sbyte)((sbyte)x << Program.ShiftBy) ==
                   (sbyte)((sbyte)Program.ToVar(x) << Program.ToVar(Program.ShiftBy));
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool Test_int_to_ushort(int x)
    {
        unchecked
        {
            return (ushort)((ushort)x << Program.ShiftBy) ==
                   (ushort)((ushort)Program.ToVar(x) << Program.ToVar(Program.ShiftBy));
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool Test_int_to_short(int x)
    {
        unchecked
        {
            return (short)((short)x << Program.ShiftBy) ==
                   (short)((short)Program.ToVar(x) << Program.ToVar(Program.ShiftBy));
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool Test_int_to_uint(int x)
    {
        unchecked
        {
            return (uint)x << Program.ShiftBy ==
                   (uint)Program.ToVar(x) << Program.ToVar(Program.ShiftBy);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool Test_int_to_int(int x)
    {
        return x << Program.ShiftBy ==
               Program.ToVar(x) << Program.ToVar(Program.ShiftBy);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool Test_int_to_ulong(int x)
    {
        unchecked
        {
            return (ulong)x << Program.ShiftBy ==
                   (ulong)Program.ToVar(x) << Program.ToVar(Program.ShiftBy);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool Test_int_to_long(int x)
    {
        return (long)x << Program.ShiftBy ==
               (long)Program.ToVar(x) << Program.ToVar(Program.ShiftBy);
    }
}

public class Tests_ulong
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool Test_ulong_to_byte(ulong x)
    {
        unchecked
        {
            return (byte)((byte)x << Program.ShiftBy) ==
                   (byte)((byte)Program.ToVar(x) << Program.ToVar(Program.ShiftBy));
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool Test_ulong_to_sbyte(ulong x)
    {
        unchecked
        {
            return (sbyte)((sbyte)x << Program.ShiftBy) ==
                   (sbyte)((sbyte)Program.ToVar(x) << Program.ToVar(Program.ShiftBy));
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool Test_ulong_to_ushort(ulong x)
    {
        unchecked
        {
            return (ushort)((ushort)x << Program.ShiftBy) ==
                   (ushort)((ushort)Program.ToVar(x) << Program.ToVar(Program.ShiftBy));
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool Test_ulong_to_short(ulong x)
    {
        unchecked
        {
            return (short)((short)x << Program.ShiftBy) ==
                   (short)((short)Program.ToVar(x) << Program.ToVar(Program.ShiftBy));
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool Test_ulong_to_uint(ulong x)
    {
        unchecked
        {
            return (uint)x << Program.ShiftBy ==
                   (uint)Program.ToVar(x) << Program.ToVar(Program.ShiftBy);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool Test_ulong_to_int(ulong x)
    {
        unchecked
        {
            return (int)x << Program.ShiftBy ==
                   (int)Program.ToVar(x) << Program.ToVar(Program.ShiftBy);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool Test_ulong_to_ulong(ulong x)
    {
        return x << Program.ShiftBy ==
               Program.ToVar(x) << Program.ToVar(Program.ShiftBy);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool Test_ulong_to_long(ulong x)
    {
        unchecked
        {
            return (long)x << Program.ShiftBy ==
                   (long)Program.ToVar(x) << Program.ToVar(Program.ShiftBy);
        }
    }
}

public class Tests_long
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool Test_long_to_byte(long x)
    {
        unchecked
        {
            return (byte)((byte)x << Program.ShiftBy) ==
                   (byte)((byte)Program.ToVar(x) << Program.ToVar(Program.ShiftBy));
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool Test_long_to_sbyte(long x)
    {
        unchecked
        {
            return (sbyte)((sbyte)x << Program.ShiftBy) ==
                   (sbyte)((sbyte)Program.ToVar(x) << Program.ToVar(Program.ShiftBy));
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool Test_long_to_ushort(long x)
    {
        unchecked
        {
            return (ushort)((ushort)x << Program.ShiftBy) ==
                   (ushort)((ushort)Program.ToVar(x) << Program.ToVar(Program.ShiftBy));
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool Test_long_to_short(long x)
    {
        unchecked
        {
            return (short)((short)x << Program.ShiftBy) ==
                   (short)((short)Program.ToVar(x) << Program.ToVar(Program.ShiftBy));
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool Test_long_to_uint(long x)
    {
        unchecked
        {
            return (uint)x << Program.ShiftBy ==
                   (uint)Program.ToVar(x) << Program.ToVar(Program.ShiftBy);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool Test_long_to_int(long x)
    {
        unchecked
        {
            return (int)x << Program.ShiftBy ==
                   (int)Program.ToVar(x) << Program.ToVar(Program.ShiftBy);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool Test_long_to_ulong(long x)
    {
        unchecked
        {
            return (ulong)x << Program.ShiftBy ==
                   (ulong)Program.ToVar(x) << Program.ToVar(Program.ShiftBy);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool Test_long_to_long(long x)
    {
        return x << Program.ShiftBy ==
               Program.ToVar(x) << Program.ToVar(Program.ShiftBy);
    }
}
