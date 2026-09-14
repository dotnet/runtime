// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Xunit;

public class ReturnBufferInitialization
{
    [Fact]
    public static void TestEntryPoint()
    {
        ReturnBufferExceptions();
        ReturnBufferAfterRead();
        PartialReturnBuffer();
        Assert.Equal(124L, FullyDefined(123));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static long FullyDefined(long input)
    {
        // X64-WINDOWS-NOT: xor
        // X64-WINDOWS: call {{.*}}ReturnBufferInitialization:Create
        S value = Create(input);
        value.Wide++;
        Observe(value.Wide);
        Observe(value.Wide);
        Observe(value.Wide);
        CheckFields(value, input + 1, input + 1, input + 2);
        return value.Wide;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Observe(long value) { }

    [StructLayout(LayoutKind.Explicit, Size = 24)]
    private struct S
    {
        [FieldOffset(0)] public long Wide;
        [FieldOffset(8)] public long Other;
        [FieldOffset(16)] public long Last;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static S Create(long value) => new S { Wide = value, Other = value + 1, Last = value + 2 };

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void CheckFields(S value, long wide, long other, long last)
    {
        Assert.Equal(wide, value.Wide);
        Assert.Equal(other, value.Other);
        Assert.Equal(last, value.Last);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static S ThrowBeforeReturning() => throw new System.InvalidOperationException();

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ReturnBufferExceptions()
    {
        S value = default;
        try
        {
            value = ThrowBeforeReturning();
        }
        catch (System.InvalidOperationException)
        {
            CheckFields(value, 0, 0, 0);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ReturnBufferAfterRead()
    {
        S value = default;
        CheckFields(value, 0, 0, 0);
        value = Create(123);
        CheckFields(value, 123, 124, 125);
    }

    private struct Outer
    {
        public S Value;
        public long Other;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void CheckOuter(Outer value)
    {
        CheckFields(value.Value, 123, 124, 125);
        Assert.Equal(0, value.Other);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void PartialReturnBuffer()
    {
        Outer value = default;
        value.Value = Create(123);
        CheckOuter(value);
    }
}
