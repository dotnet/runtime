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
        GcReturnBuffer();
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
        // Keep .locals init, but emit no explicit initialization before the call.
        Unsafe.SkipInit(out S value);
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
        // Keep .locals init, but emit no explicit initialization before the call.
        Unsafe.SkipInit(out S value);
        CheckFields(value, 0, 0, 0);
        value = Create(123);
        CheckFields(value, 123, 124, 125);
    }

    private struct GcStruct
    {
        public object Reference;
        public long First, Second, Third;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static GcStruct CreateGcStruct(object reference)
    {
        // The caller's return buffer must contain valid GC references at this point.
        System.GC.Collect(2, System.GCCollectionMode.Forced, blocking: true, compacting: true);
        return new GcStruct { Reference = reference, First = 1, Second = 2, Third = 3 };
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void GcReturnBuffer()
    {
        GcStruct value = CreateGcStruct(typeof(ReturnBufferInitialization));
        CheckGcStruct(value);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void CheckGcStruct(GcStruct value)
    {
        Assert.Same(typeof(ReturnBufferInitialization), value.Reference);
        Assert.Equal(1, value.First);
        Assert.Equal(2, value.Second);
        Assert.Equal(3, value.Third);
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
        // The call defines only Value; .locals init must still zero Other.
        Unsafe.SkipInit(out Outer value);
        value.Value = Create(123);
        CheckOuter(value);
    }
}
