// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Xunit;

// Regression coverage for the amd64 calling convention of small (<= 16 byte) value types whose
// fields straddle the 8-byte eightbyte boundary, when passed by value to shared generic
// ("gshared") code.
//
// A Nullable<T> built around an 8-byte, 4-aligned struct packs its value field at offset 4, so the
// 8-byte value spans bytes 4..11 and crosses the eightbyte boundary. A partially-shared caller sees
// such a value as an opaque type parameter with a single straddling field, while a concrete callee
// (e.g. the per-type box/unbox helper) sees the flattened layout. Both must agree on the calling
// convention. mini-amd64.c add_valuetype keeps these in two integer registers for managed calls
// rather than forcing them onto the stack, so the two views match in both the JIT and the LLVM
// backend. Previously the value was passed by-value on the stack while the concrete helper read it
// from registers, which recent LLVM exposed as a miscompile under Mono LLVM full-AOT.
public class StraddlingVtypeAbi
{
    // Generic constrained struct: GenQ<int> is { int? Field } == { bool, int } == 8 bytes, 4-aligned.
    // Its Nullable is therefore 12 bytes with the value at offset 4, straddling the eightbyte boundary.
    // Being generic also forces partial generic sharing, which is what triggers the opaque/concrete
    // calling-convention split.
    public struct GenQ<T> where T : struct
    {
        public T? Field;
    }

    // Plain 8-byte, 4-aligned all-integer struct -> straddling Nullable.
    public struct Int8
    {
        public int A;
        public int B;
    }

    // 8-byte, 4-aligned struct with a float field -> straddling Nullable. Managed calls still pass
    // the value in integer registers, so the float field rides along in the integer eightbytes.
    public struct Float8
    {
        public int A;
        public float B;
    }

    // 16-byte, 8-aligned struct that contains an object reference at offset 0 (whole, not split).
    // Used to confirm that register-passing a reference-containing <= 16 byte vtype through shared
    // generic code is GC-safe (the reference stays inside a single eightbyte register).
    public struct RefHolder
    {
        public object O;
        public int A;
        public int B;
    }

    // Box a Nullable<T> and unbox it back, all through shared generic code.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool BoxUnboxGeneric<T>(T? value, T expected, bool hasValue) where T : struct
    {
        object boxed = value;
        if (!hasValue)
            return boxed is null;
        if (boxed is null)
            return false;
        T unboxed = (T)boxed;
        return EqualityComparer<T>.Default.Equals(unboxed, expected);
    }

    // Concrete (non-shared) baseline for the same operation.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool BoxUnboxConcrete(GenQ<int>? value, GenQ<int> expected, bool hasValue)
    {
        object boxed = value;
        if (!hasValue)
            return boxed is null;
        if (boxed is null)
            return false;
        GenQ<int> unboxed = (GenQ<int>)boxed;
        return unboxed.Field.HasValue == expected.Field.HasValue
            && (!unboxed.Field.HasValue || unboxed.Field.Value == expected.Field.Value);
    }

    // Pass a value type by value through shared generic code and return it.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static T IdentityGeneric<T>(T value) => value;

    [Fact]
    public static int TestEntryPoint()
    {
        // 1. The exact bug shape: Nullable<GenQ<int>> box/unbox through shared generic code.
        GenQ<int> g = new GenQ<int> { Field = 0x12345678 };
        if (!BoxUnboxGeneric<GenQ<int>>(g, g, hasValue: true))
            return 101;
        if (!BoxUnboxGeneric<GenQ<int>>(default(GenQ<int>?), default, hasValue: false))
            return 102;

        // A GenQ<int> whose inner nullable has no value -> the inner HasValue must round-trip as
        // false (the original miscompile corrupted this byte to true).
        GenQ<int> gEmptyInner = new GenQ<int> { Field = null };
        if (!BoxUnboxGeneric<GenQ<int>>(gEmptyInner, gEmptyInner, hasValue: true))
            return 103;

        // 2. Concrete baseline of the same operation.
        if (!BoxUnboxConcrete(g, g, hasValue: true))
            return 111;
        if (!BoxUnboxConcrete(gEmptyInner, gEmptyInner, hasValue: true))
            return 112;
        if (!BoxUnboxConcrete(default(GenQ<int>?), default, hasValue: false))
            return 113;

        // 3. All-integer straddling Nullable.
        Int8 i8 = new Int8 { A = unchecked((int)0xAABBCCDD), B = unchecked((int)0xEEFF0011) };
        if (!BoxUnboxGeneric<Int8>(i8, i8, hasValue: true))
            return 121;
        if (!BoxUnboxGeneric<Int8>(default(Int8?), default, hasValue: false))
            return 122;

        // 4. Float-field straddling Nullable.
        Float8 f8 = new Float8 { A = 0x0BADF00D, B = 3.1415927f };
        if (!BoxUnboxGeneric<Float8>(f8, f8, hasValue: true))
            return 131;
        if (!BoxUnboxGeneric<Float8>(default(Float8?), default, hasValue: false))
            return 132;

        // 5. Direct by-value pass-through of straddling Nullables through shared generic code.
        GenQ<int>? ng = g;
        GenQ<int>? backNg = IdentityGeneric<GenQ<int>?>(ng);
        if (backNg.HasValue != ng.HasValue || backNg.Value.Field.Value != ng.Value.Field.Value)
            return 141;
        Int8? ni = i8;
        Int8? backNi = IdentityGeneric<Int8?>(ni);
        if (backNi.HasValue != ni.HasValue || backNi.Value.A != ni.Value.A || backNi.Value.B != ni.Value.B)
            return 142;

        // 6. Register-passing a reference-containing <= 16 byte vtype through shared generic code is
        //    GC-safe: the reference survives a collection that happens while the value is in flight.
        object marker = new object();
        RefHolder rh = new RefHolder { O = marker, A = 7, B = 9 };
        RefHolder backRh = IdentityWithGc<RefHolder>(rh);
        if (!ReferenceEquals(backRh.O, marker) || backRh.A != 7 || backRh.B != 9)
            return 151;

        return 100;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static T IdentityWithGc<T>(T value)
    {
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        return value;
    }
}
