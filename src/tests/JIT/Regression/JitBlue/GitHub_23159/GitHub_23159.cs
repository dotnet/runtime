// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Xunit;

public static class GitHub_23159
{
    [Fact]
    public static int TestEntryPoint()
    {
        var str = "application/json,text/html;q=0.9,application/xhtml+xml;q=0.9,application/xml;q=0.8,*/*;q=0.7";
        var span = Encoding.ASCII.GetBytes(str).AsSpan();

        if (BytesOrdinalEqualsStringAndAscii(str, span))
        {
            return 100;
        }
        else
        {
            return -1;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public unsafe static bool BytesOrdinalEqualsStringAndAscii(string previousValue, Span<byte> newValue)
    {
        // We just widen the bytes to char for comparison, if either the string or the bytes are not ascii
        // this will result in non-equality, so we don't need to specifically test for non-ascii.
        Debug.Assert(previousValue.Length == newValue.Length);

        // Use IntPtr values rather than int, to avoid unnessary 32 -> 64 movs on 64-bit.
        // Unfortunately this means we also need to cast to byte* for comparisons as IntPtr doesn't
        // support operator comparisons (e.g. <=, >, etc).
        // Note: Pointer comparison is unsigned, so we use the compare pattern (offset + length <= count)
        // rather than (offset <= count - length) which we'd do with signed comparison to avoid overflow.
        var count = (IntPtr)newValue.Length;
        var offset = (IntPtr)0;

        ref var bytes = ref MemoryMarshal.GetReference(newValue);
        ref var str = ref MemoryMarshal.GetReference(previousValue.AsSpan());

        do
        {
            // If Vector not-accelerated or remaining less than vector size
            if (!Vector.IsHardwareAccelerated || (byte*)(offset + Vector<byte>.Count) > (byte*)count)
            {
                if (IntPtr.Size == 8) // Use Intrinsic switch for branch elimination
                {
                    // 64-bit: Loop longs by default
                    while ((byte*)(offset + sizeof(long)) <= (byte*)count)
                    {
                        if (Unsafe.Add(ref str, offset) != (char)Unsafe.Add(ref bytes, offset) ||
                            Unsafe.Add(ref str, offset + 1) != (char)Unsafe.Add(ref bytes, offset + 1) ||
                            Unsafe.Add(ref str, offset + 2) != (char)Unsafe.Add(ref bytes, offset + 2) ||
                            Unsafe.Add(ref str, offset + 3) != (char)Unsafe.Add(ref bytes, offset + 3) ||
                            Unsafe.Add(ref str, offset + 4) != (char)Unsafe.Add(ref bytes, offset + 4) ||
                            Unsafe.Add(ref str, offset + 5) != (char)Unsafe.Add(ref bytes, offset + 5) ||
                            Unsafe.Add(ref str, offset + 6) != (char)Unsafe.Add(ref bytes, offset + 6) ||
                            Unsafe.Add(ref str, offset + 7) != (char)Unsafe.Add(ref bytes, offset + 7))
                        {
                            goto NotEqual;
                        }

                        offset += sizeof(long);
                    }
                    if ((byte*)(offset + sizeof(int)) <= (byte*)count)
                    {
                        if (Unsafe.Add(ref str, offset) != (char)Unsafe.Add(ref bytes, offset) ||
                            Unsafe.Add(ref str, offset + 1) != (char)Unsafe.Add(ref bytes, offset + 1) ||
                            Unsafe.Add(ref str, offset + 2) != (char)Unsafe.Add(ref bytes, offset + 2) ||
                            Unsafe.Add(ref str, offset + 3) != (char)Unsafe.Add(ref bytes, offset + 3))
                        {
                            goto NotEqual;
                        }

                        offset += sizeof(int);
                    }
                }
                else
                {
                    // 32-bit: Loop ints by default
                    while ((byte*)(offset + sizeof(int)) <= (byte*)count)
                    {
                        if (Unsafe.Add(ref str, offset) != (char)Unsafe.Add(ref bytes, offset) ||
                            Unsafe.Add(ref str, offset + 1) != (char)Unsafe.Add(ref bytes, offset + 1) ||
                            Unsafe.Add(ref str, offset + 2) != (char)Unsafe.Add(ref bytes, offset + 2) ||
                            Unsafe.Add(ref str, offset + 3) != (char)Unsafe.Add(ref bytes, offset + 3))
                        {
                            goto NotEqual;
                        }

                        offset += sizeof(int);
                    }
                }
                if ((byte*)(offset + sizeof(short)) <= (byte*)count)
                {
                    if (Unsafe.Add(ref str, offset) != (char)Unsafe.Add(ref bytes, offset) ||
                        Unsafe.Add(ref str, offset + 1) != (char)Unsafe.Add(ref bytes, offset + 1))
                    {
                        goto NotEqual;
                    }

                    offset += sizeof(short);
                }
                if ((byte*)offset < (byte*)count)
                {
                    if (Unsafe.Add(ref str, offset) != (char)Unsafe.Add(ref bytes, offset))
                    {
                        goto NotEqual;
                    }
                }

                return true;
            }

            // do/while as entry condition already checked
            var AllTrue = new Vector<ushort>(ushort.MaxValue);
            do
            {
                var vector = Unsafe.As<byte, Vector<byte>>(ref Unsafe.Add(ref bytes, offset));
                Vector.Widen(vector, out var vector0, out var vector1);
                var compare0 = Unsafe.As<char, Vector<ushort>>(ref Unsafe.Add(ref str, offset));
                var compare1 = Unsafe.As<char, Vector<ushort>>(ref Unsafe.Add(ref str, offset + Vector<ushort>.Count));

                if (!AllTrue.Equals(
                    Vector.BitwiseAnd(
                        Vector.Equals(compare0, vector0),
                        Vector.Equals(compare1, vector1))))
                {
                    goto NotEqual;
                }

                offset += Vector<byte>.Count;
            } while ((byte*)(offset + Vector<byte>.Count) <= (byte*)count);

            // Vector path done, loop back to do non-Vector
            // If is a exact multiple of vector size, bail now
        } while ((byte*)offset < (byte*)count);

        return true;
    NotEqual:
        return false;
    }
}
