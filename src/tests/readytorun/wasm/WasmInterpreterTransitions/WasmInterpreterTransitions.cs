// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime;
using System.Runtime.CompilerServices;
using Xunit;

namespace System.Runtime
{
    [AttributeUsage(AttributeTargets.Method)]
    internal sealed class BypassReadyToRunAttribute : Attribute
    {
    }
}

/// <summary>
/// Exercises the thunks that carry a call between R2R code and the interpreter on wasm. Methods
/// marked <see cref="BypassReadyToRunAttribute"/> are skipped by crossgen2 and so run interpreted,
/// while the rest of the assembly is compiled, which puts a thunk on every call between the two.
///
/// Each case returns values chosen so that a wrong answer is visible. That matters more than usual
/// here: the stack pointer, the hidden return buffer, 'this' and every by-reference argument are
/// all i32, so a thunk whose parameters are in the wrong order still passes wasm's call_indirect
/// type check. It does not trap — it writes through the wrong pointer, and the only symptom is
/// data that quietly comes back wrong.
/// </summary>
public class WasmInterpreterTransitions
{
    private const int A = 0x11223344;
    private const int B = 0x55667788;
    private const int C = 0x1234567;
    private const long Wide = 0x1122334455667788;

    public struct S8
    {
        public int A;
        public int B;
    }

    public struct S12
    {
        public int A;
        public int B;
        public int C;
    }

    public struct S16
    {
        public long A;
        public long B;
    }

    private readonly int _state = C;

    [Fact]
    public static void TestEntryPoint()
    {
        WasmInterpreterTransitions self = new();

        // R2R -> interpreted, struct returns. The return buffer follows 'this' for an instance
        // method and the stack pointer for a static one, which is where the two forms differ.
        S8 s8 = self.InterpretedInstanceReturnsS8(A);
        Assert.Equal(A, s8.A);
        Assert.Equal(C, s8.B);

        S8 staticS8 = InterpretedStaticReturnsS8(A);
        Assert.Equal(A, staticS8.A);
        Assert.Equal(B, staticS8.B);

        S16 s16 = self.InterpretedInstanceReturnsS16();
        Assert.Equal(Wide, s16.A);
        Assert.Equal(C, s16.B);

        S16 staticS16 = InterpretedStaticReturnsS16(Wide, A);
        Assert.Equal(Wide, staticS16.A);
        Assert.Equal(A, staticS16.B);

        // A struct that is not a whole number of 8-byte slots, passed and returned.
        S12 s12 = self.InterpretedInstanceRoundTripsS12(new S12 { A = A, B = B, C = C }, A);
        Assert.Equal(B, s12.A);
        Assert.Equal(C, s12.B);
        Assert.Equal(A, s12.C);

        // R2R -> interpreted, scalar and void shapes.
        Assert.Equal(A + C, self.InterpretedInstanceReturnsI32(A));
        Assert.Equal(Wide, InterpretedStaticReturnsI64(1.5));
        Assert.Equal(A + B + C, InterpretedStaticSumsFour(A, B, C, 0));
        Assert.Equal(A, self.InterpretedInstanceMixedScalars(1.5f, 2.5, Wide));

        // A struct argument arrives as the address of its interpreter stack slot.
        Assert.Equal(A + B, self.InterpretedInstanceTakesS8(new S8 { A = A, B = B }));

        s_sideEffect = 0;
        self.InterpretedInstanceTakesS8ReturnsVoid(new S8 { A = A, B = B });
        Assert.Equal(A + B, s_sideEffect);

        // interpreted -> R2R, the opposite direction over the same shapes.
        Assert.Equal(A + C, self.InterpretedCallsBackIntoR2R());

        S16 fromInterpreter = self.InterpretedCallsR2RReturningS16();
        Assert.Equal(Wide, fromInterpreter.A);
        Assert.Equal(C, fromInterpreter.B);
    }

    private static int s_sideEffect;

    [BypassReadyToRun]
    [MethodImpl(MethodImplOptions.NoInlining)]
    private S8 InterpretedInstanceReturnsS8(int a) => new S8 { A = a, B = _state };

    [BypassReadyToRun]
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static S8 InterpretedStaticReturnsS8(int a) => new S8 { A = a, B = B };

    [BypassReadyToRun]
    [MethodImpl(MethodImplOptions.NoInlining)]
    private S16 InterpretedInstanceReturnsS16() => new S16 { A = Wide, B = _state };

    [BypassReadyToRun]
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static S16 InterpretedStaticReturnsS16(long wide, int a) => new S16 { A = wide, B = a };

    [BypassReadyToRun]
    [MethodImpl(MethodImplOptions.NoInlining)]
    private S12 InterpretedInstanceRoundTripsS12(S12 value, int a) => new S12 { A = value.B, B = value.C, C = a };

    [BypassReadyToRun]
    [MethodImpl(MethodImplOptions.NoInlining)]
    private int InterpretedInstanceReturnsI32(int a) => a + _state;

    [BypassReadyToRun]
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static long InterpretedStaticReturnsI64(double unused) => Wide;

    [BypassReadyToRun]
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int InterpretedStaticSumsFour(int a, int b, int c, int d) => a + b + c + d;

    [BypassReadyToRun]
    [MethodImpl(MethodImplOptions.NoInlining)]
    private int InterpretedInstanceMixedScalars(float f, double d, long l) => l == Wide && f == 1.5f && d == 2.5 ? A : 0;

    [BypassReadyToRun]
    [MethodImpl(MethodImplOptions.NoInlining)]
    private int InterpretedInstanceTakesS8(S8 value) => value.A + value.B;

    [BypassReadyToRun]
    [MethodImpl(MethodImplOptions.NoInlining)]
    private void InterpretedInstanceTakesS8ReturnsVoid(S8 value) => s_sideEffect = value.A + value.B;

    [BypassReadyToRun]
    [MethodImpl(MethodImplOptions.NoInlining)]
    private int InterpretedCallsBackIntoR2R() => R2RInstanceReturnsI32(A);

    [BypassReadyToRun]
    [MethodImpl(MethodImplOptions.NoInlining)]
    private S16 InterpretedCallsR2RReturningS16() => R2RInstanceReturnsS16();

    [MethodImpl(MethodImplOptions.NoInlining)]
    private int R2RInstanceReturnsI32(int a) => a + _state;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private S16 R2RInstanceReturnsS16() => new S16 { A = Wide, B = _state };
}
