// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
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
    private const float F32 = 3.5f;
    private const double F64 = 6.25;

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

    public struct S1
    {
        public byte A;
    }

    public struct S2
    {
        public short A;
    }

    public struct S48
    {
        public long A, B, C, D, E, F;
    }

    public struct S52
    {
        public int A, B, C, D, E, F, G, H, I, J, K, L, M;
    }

    public struct S304
    {
        public long L00, L01, L02, L03, L04, L05, L06, L07, L08, L09,
                    L10, L11, L12, L13, L14, L15, L16, L17, L18, L19,
                    L20, L21, L22, L23, L24, L25, L26, L27, L28, L29,
                    L30, L31, L32, L33, L34, L35, L36, L37;
    }

    private delegate S2 ReturnsS2Delegate(int a);

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

        // R2R -> interpreted, floating-point returns. A float/double return travels back through
        // the thunk's return buffer and is reloaded with an f32/f64 load; a wrong width or an
        // integer reload silently corrupts it. The runtime's hand-written table had no such shape.
        Assert.Equal(F32, InterpretedStaticReturnsF32(A));
        Assert.Equal(F32, self.InterpretedInstanceReturnsF32());
        Assert.Equal(F64, InterpretedStaticReturnsF64(A));
        Assert.Equal(F64, self.InterpretedInstanceReturnsF64(F32));

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

        // interpreted -> R2R, floating-point returns over the same boundary.
        Assert.Equal(F32, self.InterpretedCallsR2RReturningF32());
        Assert.Equal(F64, self.InterpretedCallsR2RReturningF64());

        // interpreted -> R2R, this+scalar and this/static+struct-arg shapes.
        Assert.Equal(C, self.InterpretedCallsR2RIntNoArg());                                                       // MiTp
        s_sideEffect = 0; self.InterpretedCallsR2RTakesInt(A); Assert.Equal(A + C, s_sideEffect);                  // MvTip
        s_sideEffect = 0; self.InterpretedCallsR2RTakesS16AndInt(new S16 { A = 10, B = 20 }, A); Assert.Equal(30 + A, s_sideEffect); // MvTS16ip
        s_sideEffect = 0; InterpretedCallsR2RStaticTakesS16AndTwoInt(new S16 { A = 10, B = 20 }, A, B); Assert.Equal(30 + A + B, s_sideEffect); // MvS16iip
        Assert.Equal(30 + A + B + C, self.InterpretedCallsR2RTakesS16AndTwoInt(new S16 { A = 10, B = 20 }, A, B)); // MiTS16iip

        // 1- and 2-byte struct shapes (S1 / S2). These small structs travel by value in a single
        // slot and are the 'S1'/'S2' encodings the runtime spells for the return buffer and by-ref
        // argument; the hand-written table only ever had 8-byte forms.
        Assert.Equal(unchecked((byte)A), InterpretedStaticReturnsS1(A).A);          // I S1 i p
        s_sideEffect = 0;
        self.InterpretedInstanceTakesIntAndS2(A, new S2 { A = unchecked((short)B) }); // I v T i S2 p
        Assert.Equal(A + unchecked((short)B), s_sideEffect);

        // R2R reaches an interpreted method through a delegate: its entrypoint is materialized as a
        // native function pointer via GetMultiCallableAddrOfCode, which is the path that needs the
        // R2R-to-interpreter thunk independent of any direct call. The target returns S2 from an
        // instance method (I S2 T i p).
        Assert.Equal(unchecked((short)(A + C)), self.R2RInvokesInterpretedViaDelegate().A);

        // Signature shapes reported missing by CI (PR #132419): crossgen2 must emit the R2R->interp
        // thunk for each, and pure interpreter must reach the callee without one. Keys in comments.
        s_sideEffect = 0; self.InterpretedVoid3IntDouble(A, B, C, F64); Assert.Equal(A + B + C + 1, s_sideEffect);   // IvTiiidp
        s_sideEffect = 0; self.InterpretedVoid3IntFloat(A, B, C, F32); Assert.Equal(A + B + C + 1, s_sideEffect);    // IvTiiifp
        s_sideEffect = 0; self.InterpretedVoid3IntLong(A, B, C, Wide); Assert.Equal(A + B + C + 1, s_sideEffect);    // IvTiiilp
        s_sideEffect = 0; self.InterpretedVoid3IntS48(A, B, C, new S48 { A = 10, F = 20 }); Assert.Equal(A + B + C + 30, s_sideEffect);   // IvTiiiS48p
        s_sideEffect = 0; self.InterpretedVoid3IntS304(A, B, C, new S304 { L00 = 10, L37 = 20 }); Assert.Equal(A + B + C + 30, s_sideEffect); // IvTiiiS304p
        s_sideEffect = 0; self.InterpretedVoidVector128(Vector128.Create(A, B, C, 7)); Assert.Equal(A + 7, s_sideEffect);   // IvTVp
        Assert.Equal(Wide, self.InterpretedLongFromFloat(F32));                                                    // IlTfp
        Assert.Equal((long)A + B + C + A + B, self.InterpretedLongFrom5Int(A, B, C, A, B));                        // IlTiiiiip
        Assert.Equal(190L, self.InterpretedLongFrom19Int(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19)); // IlTiiiiiiiiiiiiiiiiiiip
        Assert.Equal(153, self.InterpretedIntFrom17Int(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17));           // IiTiiiiiiiiiiiiiiiip
        S52 s52 = self.InterpretedInstanceReturnsS52(); Assert.Equal(A, s52.A); Assert.Equal(B, s52.M);           // IS52Tp
        Assert.Equal(unchecked((short)C), InterpretedStaticReturnsS2NoArgs().A);                                             // IS2p
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
    private static float InterpretedStaticReturnsF32(int a) => a == A ? F32 : 0f;

    [BypassReadyToRun]
    [MethodImpl(MethodImplOptions.NoInlining)]
    private float InterpretedInstanceReturnsF32() => _state == C ? F32 : 0f;

    [BypassReadyToRun]
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static double InterpretedStaticReturnsF64(int a) => a == A ? F64 : 0.0;

    [BypassReadyToRun]
    [MethodImpl(MethodImplOptions.NoInlining)]
    private double InterpretedInstanceReturnsF64(float f) => f == F32 ? F64 : 0.0;

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

    [BypassReadyToRun]
    [MethodImpl(MethodImplOptions.NoInlining)]
    private float InterpretedCallsR2RReturningF32() => R2RInstanceReturnsF32();

    [BypassReadyToRun]
    [MethodImpl(MethodImplOptions.NoInlining)]
    private double InterpretedCallsR2RReturningF64() => R2RInstanceReturnsF64();

    [BypassReadyToRun]
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static S1 InterpretedStaticReturnsS1(int a) => new S1 { A = (byte)a };

    [BypassReadyToRun]
    [MethodImpl(MethodImplOptions.NoInlining)]
    private void InterpretedInstanceTakesIntAndS2(int a, S2 s) => s_sideEffect = a + s.A;

    [BypassReadyToRun]
    [MethodImpl(MethodImplOptions.NoInlining)]
    private S2 InterpretedInstanceReturnsS2(int a) => new S2 { A = (short)(a + _state) };

    // R2R code: takes a delegate to the interpreted method above and invokes it. Creating the
    // delegate takes the interpreted method's address, so its entrypoint must be callable from R2R.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private S2 R2RInvokesInterpretedViaDelegate()
    {
        ReturnsS2Delegate d = InterpretedInstanceReturnsS2;
        return d(A);
    }

    [BypassReadyToRun]
    [MethodImpl(MethodImplOptions.NoInlining)]
    private void InterpretedVoid3IntDouble(int a, int b, int c, double d) => s_sideEffect = a + b + c + (d == F64 ? 1 : 0);

    [BypassReadyToRun]
    [MethodImpl(MethodImplOptions.NoInlining)]
    private void InterpretedVoid3IntFloat(int a, int b, int c, float f) => s_sideEffect = a + b + c + (f == F32 ? 1 : 0);

    [BypassReadyToRun]
    [MethodImpl(MethodImplOptions.NoInlining)]
    private void InterpretedVoid3IntLong(int a, int b, int c, long l) => s_sideEffect = a + b + c + (l == Wide ? 1 : 0);

    [BypassReadyToRun]
    [MethodImpl(MethodImplOptions.NoInlining)]
    private void InterpretedVoid3IntS48(int a, int b, int c, S48 s) => s_sideEffect = a + b + c + (int)(s.A + s.F);

    [BypassReadyToRun]
    [MethodImpl(MethodImplOptions.NoInlining)]
    private void InterpretedVoid3IntS304(int a, int b, int c, S304 s) => s_sideEffect = a + b + c + (int)(s.L00 + s.L37);

    [BypassReadyToRun]
    [MethodImpl(MethodImplOptions.NoInlining)]
    private void InterpretedVoidVector128(Vector128<int> v) => s_sideEffect = v.GetElement(0) + v.GetElement(3);

    [BypassReadyToRun]
    [MethodImpl(MethodImplOptions.NoInlining)]
    private long InterpretedLongFromFloat(float f) => f == F32 ? Wide : 0;

    [BypassReadyToRun]
    [MethodImpl(MethodImplOptions.NoInlining)]
    private long InterpretedLongFrom5Int(int a, int b, int c, int d, int e) => (long)a + b + c + d + e;

    [BypassReadyToRun]
    [MethodImpl(MethodImplOptions.NoInlining)]
    private long InterpretedLongFrom19Int(int a1, int a2, int a3, int a4, int a5, int a6, int a7, int a8, int a9, int a10,
                                          int a11, int a12, int a13, int a14, int a15, int a16, int a17, int a18, int a19)
        => (long)a1 + a2 + a3 + a4 + a5 + a6 + a7 + a8 + a9 + a10 + a11 + a12 + a13 + a14 + a15 + a16 + a17 + a18 + a19;

    [BypassReadyToRun]
    [MethodImpl(MethodImplOptions.NoInlining)]
    private int InterpretedIntFrom17Int(int a1, int a2, int a3, int a4, int a5, int a6, int a7, int a8, int a9,
                                        int a10, int a11, int a12, int a13, int a14, int a15, int a16, int a17)
        => a1 + a2 + a3 + a4 + a5 + a6 + a7 + a8 + a9 + a10 + a11 + a12 + a13 + a14 + a15 + a16 + a17;

    [BypassReadyToRun]
    [MethodImpl(MethodImplOptions.NoInlining)]
    private S52 InterpretedInstanceReturnsS52() => new S52 { A = A, M = B };

    [BypassReadyToRun]
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static S2 InterpretedStaticReturnsS2NoArgs() => new S2 { A = unchecked((short)C) };

    [MethodImpl(MethodImplOptions.NoInlining)]
    private int R2RInstanceReturnsI32(int a) => a + _state;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private S16 R2RInstanceReturnsS16() => new S16 { A = Wide, B = _state };

    [MethodImpl(MethodImplOptions.NoInlining)]
    private float R2RInstanceReturnsF32() => _state == C ? F32 : 0f;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private double R2RInstanceReturnsF64() => _state == C ? F64 : 0.0;

    // interpreted -> R2R callers (bypassed, so interpreted) that call the compiled R2R methods below.
    [BypassReadyToRun]
    [MethodImpl(MethodImplOptions.NoInlining)]
    private int InterpretedCallsR2RIntNoArg() => R2RInstanceReturnsI32NoArg();

    [BypassReadyToRun]
    [MethodImpl(MethodImplOptions.NoInlining)]
    private void InterpretedCallsR2RTakesInt(int a) => R2RInstanceTakesInt(a);

    [BypassReadyToRun]
    [MethodImpl(MethodImplOptions.NoInlining)]
    private void InterpretedCallsR2RTakesS16AndInt(S16 s, int a) => R2RInstanceTakesS16AndInt(s, a);

    [BypassReadyToRun]
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void InterpretedCallsR2RStaticTakesS16AndTwoInt(S16 s, int a, int b) => R2RStaticTakesS16AndTwoInt(s, a, b);

    [BypassReadyToRun]
    [MethodImpl(MethodImplOptions.NoInlining)]
    private int InterpretedCallsR2RTakesS16AndTwoInt(S16 s, int a, int b) => R2RInstanceTakesS16AndTwoInt(s, a, b);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private int R2RInstanceReturnsI32NoArg() => _state;                                        // MiTp

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void R2RInstanceTakesInt(int a) => s_sideEffect = a + _state;                      // MvTip

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void R2RInstanceTakesS16AndInt(S16 s, int a) => s_sideEffect = (int)(s.A + s.B) + a; // MvTS16ip

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void R2RStaticTakesS16AndTwoInt(S16 s, int a, int b) => s_sideEffect = (int)(s.A + s.B) + a + b; // MvS16iip

    [MethodImpl(MethodImplOptions.NoInlining)]
    private int R2RInstanceTakesS16AndTwoInt(S16 s, int a, int b) => (int)(s.A + s.B) + a + b + _state; // MiTS16iip
}
