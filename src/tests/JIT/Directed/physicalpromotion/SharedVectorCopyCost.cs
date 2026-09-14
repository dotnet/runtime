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

    // Six integer arguments exhaust the Unix x64 argument registers.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ConsumeStackArgs(int a, int b, int c, int d, int e, int f, S value) { }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void Consume(S value, bool stackArgs)
    {
        if (stackArgs)
        {
            ConsumeStackArgs(1, 2, 3, 4, 5, 6, value);
        }
        else
        {
            Consume(value);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int Copy(int input)
    {
        // After the last copy, reuse the promoted fields instead of reloading the shorts.
        // X64: call {{.*}}SharedVectorCopyCost:Consume
        // X64: call {{.*}}SharedVectorCopyCost:Consume
        // X64: call {{.*}}SharedVectorCopyCost:Consume
        // X64: call {{.*}}SharedVectorCopyCost:Consume
        // X64-NOT: movsx
        // X64: ret
        return CopyCore(input, false);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int CopyStackArgs(int input)
    {
        // X64: call {{.*}}SharedVectorCopyCost:ConsumeStackArgs
        // X64: call {{.*}}SharedVectorCopyCost:ConsumeStackArgs
        // X64: call {{.*}}SharedVectorCopyCost:ConsumeStackArgs
        // X64: call {{.*}}SharedVectorCopyCost:ConsumeStackArgs
        // X64-NOT: movsx
        // X64: ret
        return CopyCore(input, true);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int CopyCore(int input, bool stackArgs)
    {
        S src = Create(input);
        Fields(src.A, src.B, src.C, src.D, src.E);
        Fields(src.A, src.B, src.C, src.D, src.E);
        Fields(src.A, src.B, src.C, src.D, src.E);

        // The source is profitable to promote independently. Its replacements
        // already fragment these copies, so promoting matching destination
        // fields must not pay the fragmentation cost again.
        S dst = src;
        dst.A++;
        Consume(dst, stackArgs);
        dst = src;
        dst.A++;
        Consume(dst, stackArgs);
        dst = src;
        dst.A++;
        Consume(dst, stackArgs);
        dst = src;
        dst.A++;
        Consume(dst, stackArgs);

        Fields(dst.A, dst.B, dst.C, dst.D, dst.E);
        return dst.A + dst.B + dst.C + dst.D + dst.E;
    }

    [StructLayout(LayoutKind.Explicit, Size = 16)]
    private struct ArrayValue
    {
        [FieldOffset(0)] public int A;
        [FieldOffset(4)] public int B;
        [FieldOffset(8)] public int C;
        [FieldOffset(12)] public int D;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static ArrayValue CreateArrayValue(int value) =>
        new ArrayValue { A = value, B = value + 1, C = value + 2, D = value + 3 };

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void CheckFields(int a, int b, int c, int d)
    {
        Assert.Equal(a + 1, b);
        Assert.Equal(b + 1, c);
        Assert.Equal(c + 1, d);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Consume(ArrayValue value) { }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int CopyToRegularlyPromotedLocal(int input)
    {
        // The four-field destination is eligible for regular promotion. Whole-local
        // copies must credit that endpoint just as they credit physical promotion.
        // X64: call {{.*}}SharedVectorCopyCost:CheckFields
        // X64: call {{.*}}SharedVectorCopyCost:CheckFields
        // X64: call {{.*}}SharedVectorCopyCost:CheckFields
        // X64: call {{.*}}SharedVectorCopyCost:CheckFields
        // Keep the source fields promoted instead of copying their stack storage.
        // X64-NOT: xmmword ptr
        // X64: ret
        ArrayValue source = CreateArrayValue(input);
        CheckFields(source.A, source.B, source.C, source.D);
        CheckFields(source.A, source.B, source.C, source.D);
        CheckFields(source.A, source.B, source.C, source.D);
        CheckFields(source.A, source.B, source.C, source.D);
        Consume(source);
        Consume(source);
        Consume(source);
        Consume(source);
        ArrayValue destination = source;
        destination.A++;
        Consume(destination);
        destination = source;
        destination.A++;
        Consume(destination);
        destination = source;
        destination.A++;
        Consume(destination);
        destination = source;
        destination.A++;
        Consume(destination);
        return destination.A + destination.B + destination.C + destination.D;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void CopyToArray(int input, ArrayValue[] destination)
    {
        // Preserve vector copies instead of splitting each array store into four field stores.
        // X64-WINDOWS: call {{.*}}SharedVectorCopyCost:CheckFields
        // X64-WINDOWS: call {{.*}}SharedVectorCopyCost:CheckFields
        // X64-WINDOWS: {{v?movups}} xmm{{[0-9]+}}, xmmword ptr
        // X64-WINDOWS: {{v?movups}} xmmword ptr
        // X64-WINDOWS: {{v?movups}} xmm{{[0-9]+}}, xmmword ptr
        // X64-WINDOWS: {{v?movups}} xmmword ptr
        // X64-WINDOWS: {{v?movups}} xmm{{[0-9]+}}, xmmword ptr
        // X64-WINDOWS: {{v?movups}} xmmword ptr
        // X64-WINDOWS: {{v?movups}} xmm{{[0-9]+}}, xmmword ptr
        // X64-WINDOWS: {{v?movups}} xmmword ptr
        ArrayValue source = CreateArrayValue(input);
        CheckFields(source.A, source.B, source.C, source.D);
        CheckFields(source.A, source.B, source.C, source.D);
        destination[0] = source;
        destination[1] = source;
        destination[2] = source;
        destination[3] = source;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void CopyConstantToArray(System.Guid[] destination)
    {
        // Promotion should allow constant fields to propagate into the copies.
        // X64-WINDOWS-NOT: xmmword ptr [rsp
        // X64-WINDOWS: ret
        System.Guid source = new System.Guid(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11);
        destination[0] = source;
        destination[1] = source;
        destination[2] = source;
        destination[3] = source;
    }

    [StructLayout(LayoutKind.Explicit, Size = 24)]
    private struct OuterValue
    {
        [FieldOffset(0)] public long Prefix;
        [FieldOffset(8)] public ArrayValue Value;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static OuterValue CreateOuterValue(int input) =>
        new OuterValue { Prefix = input, Value = new ArrayValue { A = input, B = input + 1, C = input + 2, D = input + 3 } };

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void CopyInducedToArray(int input, ArrayValue[] destination)
    {
        // Copy a subrange of the promoted source into a local with no explicit field reads.
        // Its induced fields must still pay for fragmenting the subsequent array copies.
        // ARM64: SharedVectorCopyCost:CheckFields
        // ARM64: SharedVectorCopyCost:CheckFields
        // ARM64: SharedVectorCopyCost:CheckFields
        // ARM64: ldr q{{[0-9]+}}, [{{fp|sp}}
        // ARM64: {{stp|str}} q{{[0-9]+}},
        // ARM64: ret
        // On x86 these int fields are native-sized and must remain uncharged.
        // X86: call {{.*}}SharedVectorCopyCost:CheckFields
        // X86-NOT: xmmword ptr
        // X86: ret
        // X64: call {{.*}}SharedVectorCopyCost:CheckFields
        // X64: call {{.*}}SharedVectorCopyCost:CheckFields
        // X64: call {{.*}}SharedVectorCopyCost:CheckFields
        // X64: {{v?movups}} xmm{{[0-9]+}}, xmmword ptr
        // X64: {{v?movups}} xmmword ptr
        // X64: {{v?movups}} xmm{{[0-9]+}}, xmmword ptr
        // X64: {{v?movups}} xmmword ptr
        OuterValue source = CreateOuterValue(input);
        CheckFields(source.Value.A, source.Value.B, source.Value.C, source.Value.D);
        CheckFields(source.Value.A, source.Value.B, source.Value.C, source.Value.D);
        CheckFields(source.Value.A, source.Value.B, source.Value.C, source.Value.D);
        ArrayValue value = source.Value;
        destination[0] = value;
        destination[1] = value;
        destination[2] = value;
        destination[3] = value;
        destination[4] = value;
        destination[5] = value;
        destination[6] = value;
        destination[7] = value;
    }

    [StructLayout(LayoutKind.Explicit, Size = 16)]
    private struct MixedValue
    {
        [FieldOffset(0)] public long A;
        [FieldOffset(8)] public short B;
        [FieldOffset(10)] public short C;
        [FieldOffset(12)] public short D;
        [FieldOffset(14)] public short E;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static MixedValue CreateMixedValue(int value) =>
        new MixedValue { A = value, B = (short)value, C = (short)value, D = (short)value, E = (short)value };

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void MixedFields(long a, int b, int c, int d, int e) { }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void CopyMixedToArray(int input, MixedValue[] destination)
    {
        // The native-width read keeps this mixed-width copy outside the fragmentation charge.
        // X64: call {{.*}}SharedVectorCopyCost:MixedFields
        // X64: call {{.*}}SharedVectorCopyCost:MixedFields
        // X64-NOT: xmmword ptr
        // X64-NOT: ptr [rsp
        // X64: ret
        MixedValue source = CreateMixedValue(input);
        MixedFields(source.A, source.B, source.C, source.D, source.E);
        MixedFields(source.A, source.B, source.C, source.D, source.E);
        destination[0] = source;
        destination[1] = source;
        destination[2] = source;
        destination[3] = source;
        destination[4] = source;
        destination[5] = source;
        destination[6] = source;
        destination[7] = source;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int InitializeNonzero(int input)
    {
        // Nonzero initblk is initialization, not a copy to fragment.
        // X64-NOT: xmmword ptr
        // X64: call {{.*}}SharedVectorCopyCost:Fields
        // X64: ret
        // X86-NOT: xmmword ptr
        // X86: call {{.*}}SharedVectorCopyCost:Fields
        // X86: ret
        // ARM64-NOT: {{stp|str}} q{{[0-9]+}}
        // ARM64: SharedVectorCopyCost:Fields
        // ARM64: ret
        S value;
        Unsafe.SkipInit(out value);
        Unsafe.InitBlockUnaligned(ref Unsafe.As<S, byte>(ref value), 0xAB, (uint)Unsafe.SizeOf<S>());
        value.A = input;
        Fields(value.A, value.B, value.C, value.D, value.E);
        return value.A + value.B + value.C + value.D + value.E;
    }

    [Fact]
    public static void TestEntryPoint()
    {
        Assert.Equal(unchecked(10 + 2 * (int)0xABABABAB + 2 * (short)0xABAB), InitializeNonzero(10));
        Assert.Equal(unchecked(-10 + 2 * (int)0xABABABAB + 2 * (short)0xABAB), InitializeNonzero(-10));
        Assert.Equal(61, Copy(10));
        Assert.Equal(-39, Copy(-10));
        Assert.Equal(32769, Copy(32766));
        Assert.Equal(61, CopyStackArgs(10));
        Assert.Equal(-39, CopyStackArgs(-10));
        Assert.Equal(32769, CopyStackArgs(32766));
        Assert.Equal(47, CopyToRegularlyPromotedLocal(10));
        Assert.Equal(-33, CopyToRegularlyPromotedLocal(-10));

        ArrayValue[] values = new ArrayValue[4];
        foreach (int input in new[] { 10, -10 })
        {
            CopyToArray(input, values);
            foreach (ArrayValue value in values)
            {
                Assert.Equal(input, value.A);
                CheckFields(value.A, value.B, value.C, value.D);
            }
        }

        ArrayValue[] inducedValues = new ArrayValue[8];
        foreach (int input in new[] { 10, -10 })
        {
            CopyInducedToArray(input, inducedValues);
            foreach (ArrayValue value in inducedValues)
            {
                Assert.Equal(input, value.A);
                CheckFields(value.A, value.B, value.C, value.D);
            }
        }

        MixedValue[] mixedValues = new MixedValue[8];
        foreach (int input in new[] { 10, -10, 32768 })
        {
            CopyMixedToArray(input, mixedValues);
            foreach (MixedValue value in mixedValues)
            {
                Assert.Equal((long)input, value.A);
                Assert.Equal((short)input, value.B);
                Assert.Equal((short)input, value.C);
                Assert.Equal((short)input, value.D);
                Assert.Equal((short)input, value.E);
            }
        }

        System.Guid[] constants = new System.Guid[4];
        CopyConstantToArray(constants);
        foreach (System.Guid value in constants)
        {
            Assert.Equal(new System.Guid(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11), value);
        }
    }
}
