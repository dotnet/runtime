// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Xunit;

public class SharedVectorCopyCost
{
    // Five fields prevent regular promotion of this vector-sized struct.
    [StructLayout(LayoutKind.Explicit, Size = 16)]
    private struct S
    {
        [FieldOffset(0)] public int A;
        [FieldOffset(4)] public int B;
        [FieldOffset(8)] public int C;
        [FieldOffset(12)] public short D;
        [FieldOffset(14)] public short E;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static S Create(int value) =>
        new S { A = value, B = value + 1, C = value + 2, D = (short)(value + 3), E = (short)(value + 4) };

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Fields(int a, int b, int c, int d, int e) { }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Consume(S value) { }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int Copy(int input)
    {
        // After the last copy, reuse the promoted fields instead of reloading the shorts.
        // X64-WINDOWS: call {{.*}}SharedVectorCopyCost:Consume
        // X64-WINDOWS: call {{.*}}SharedVectorCopyCost:Consume
        // X64-WINDOWS: call {{.*}}SharedVectorCopyCost:Consume
        // X64-WINDOWS: call {{.*}}SharedVectorCopyCost:Consume
        // X64-WINDOWS-NOT: movsx
        // X64-WINDOWS: ret
        S src = Create(input);
        Fields(src.A, src.B, src.C, src.D, src.E);
        Fields(src.A, src.B, src.C, src.D, src.E);
        Fields(src.A, src.B, src.C, src.D, src.E);

        // The source is profitable to promote independently. Its replacements
        // already fragment these copies, so promoting matching destination
        // fields must not pay the fragmentation cost again.
        S dst = src;
        dst.A++;
        Consume(dst);
        dst = src;
        dst.A++;
        Consume(dst);
        dst = src;
        dst.A++;
        Consume(dst);
        dst = src;
        dst.A++;
        Consume(dst);

        Fields(dst.A, dst.B, dst.C, dst.D, dst.E);
        return dst.A + dst.B + dst.C + dst.D + dst.E;
    }

    [Fact]
    public static void TestEntryPoint()
    {
        Assert.Equal(61, Copy(10));
        Assert.Equal(-39, Copy(-10));
        Assert.Equal(32769, Copy(32766));
    }
}
