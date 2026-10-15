// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using InvalidCSharp;

using Xunit;
using TestLibrary;

public class Validate
{
    [StructLayout(LayoutKind.Explicit)]
    private ref struct Explicit
    {
        [FieldOffset(0)] public Span<byte> Bytes;
        [FieldOffset(0)] public Guid Guid;
    }

    private ref struct Spans
    {
        public Span<int> First;
        public Span<int> Second;
        public Span<int> Third;
        public Span<int> Fourth;
    }

    [StructLayout(LayoutKind.Explicit)]
    private ref struct OverlappingSpans
    {
        [FieldOffset(0)] public Spans First;
        [FieldOffset(0)] public Spans Second;
    }

    [ActiveIssue("expected failure: unsupported type with ref field fails at AOT compile time, not runtime", typeof(PlatformDetection), nameof(PlatformDetection.IsMonoFULLAOT))]
    [Fact]
    public static void Validate_Invalid_RefField_Fails()
    {
        Console.WriteLine($"{nameof(Validate_Invalid_RefField_Fails)}...");
        Assert.Throws<TypeLoadException>(() => { var t = typeof(InvalidStructWithRefField); });
        Assert.Throws<TypeLoadException>(() => { var t = typeof(InvalidRefFieldAlignment); });
        Assert.Throws<TypeLoadException>(() => { var t = typeof(InvalidObjectRefRefFieldOverlap); });
        Assert.Throws<TypeLoadException>(() =>
        {
            var t = new IntPtrRefFieldOverlap()
            {
                Field = IntPtr.Zero
            };
            return t.Field.ToString();
        });
        Assert.Throws<TypeLoadException>(() =>
        {
            var t = new IntPtrOverlapWithInnerFieldType()
            {
                Field = IntPtr.Zero
            };
            ref var i =  ref t.Invalid;
            return i.Size;
        });
        Assert.Throws<TypeLoadException>(() =>
        {
            var t = new Explicit()
            {
                Guid = Guid.NewGuid()
            };
            return t.Bytes.Length;
        });
    }

    [ActiveIssue("expected failure: unsupported type with ref field fails at AOT compile time, not runtime", typeof(PlatformDetection), nameof(PlatformDetection.IsMonoFULLAOT))]
    [Fact]
    public static void Validate_RefStructWithRefField_Load()
    {
        Console.WriteLine($"{nameof(Validate_RefStructWithRefField_Load)}...");
        var t = typeof(WithRefField);
    }

    [Fact]
    public static void Validate_Overlapping_RefFields()
    {
        Console.WriteLine($"{nameof(Validate_Overlapping_RefFields)}...");

        OverlappingSpans value = default;
        value.First.First = new int[10];
        value.First.First.Fill(1);
        value.First.Second = new int[10];
        value.First.Second.Fill(2);
        value.First.Third = new int[10];
        value.First.Third.Fill(3);
        value.First.Fourth = new int[10];
        value.First.Fourth.Fill(4);

        Validate_Overlapping_RefFields_Worker(value);
    }

    // Keep the by-value argument live across the callee's JIT transition under GC stress.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Validate_Overlapping_RefFields_Worker(OverlappingSpans value)
    {
        Assert.Equal(1, value.First.First[0]);
        Assert.Equal(2, value.First.Second[0]);
        Assert.Equal(3, value.First.Third[0]);
        Assert.Equal(4, value.First.Fourth[0]);
        Assert.Equal(1, value.Second.First[0]);
        Assert.Equal(2, value.Second.Second[0]);
        Assert.Equal(3, value.Second.Third[0]);
        Assert.Equal(4, value.Second.Fourth[0]);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Create_RefField_Worker(string str, int depth)
    {
        if (depth == 5)
        {
            return;
        }

        WithRefField s = new(ref str);
        string newStr = new(str);

        Create_RefField_Worker(str + $" {depth}", depth + 1);
        Assert.False(s.ConfirmFieldInstance(newStr));
        Assert.True(s.ConfirmFieldInstance(str));
    }

    [ActiveIssue("expected failure: unsupported type with ref field fails at AOT compile time, not runtime", typeof(PlatformDetection), nameof(PlatformDetection.IsMonoFULLAOT))]
    [Fact]
    public static void Validate_Create_RefField()
    {
        var str = nameof(Validate_Create_RefField);
        Console.WriteLine($"{str}...");
        Create_RefField_Worker(str, 1);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Create_RefStructField_Worker(string str, int depth)
    {
        if (depth == 5)
        {
            return;
        }

        WithRefField s = new(ref str);
        WithRefStructField t = new(ref s);

        Create_RefStructField_Worker(str + $" {depth}", depth + 1);
        Assert.True(t.ConfirmFieldInstance(ref s));
    }

    [ActiveIssue("expected failure: unsupported type with ref field fails at AOT compile time, not runtime", typeof(PlatformDetection), nameof(PlatformDetection.IsMonoFULLAOT))]
    [Fact]
    public static void Validate_Create_RefStructField()
    {
        var str = nameof(Validate_Create_RefStructField);
        Console.WriteLine($"{str}...");
        Create_RefStructField_Worker(str, 1);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Create_TypedReferenceRefField_Worker(Validate v, int depth)
    {
        if (depth == 5)
        {
            return;
        }

        WithTypedReferenceField<Validate> s = new(ref v);

        Create_TypedReferenceRefField_Worker(v, depth + 1);
        Assert.Equal(typeof(Validate), s.GetFieldType());
        Assert.True(s.ConfirmFieldInstance(v));
    }

    [ActiveIssue("expected failure: unsupported type with ref field fails at AOT compile time, not runtime", typeof(PlatformDetection), nameof(PlatformDetection.IsMonoFULLAOT))]
    [Fact]
    public static void Validate_Create_TypedReferenceRefField()
    {
        Console.WriteLine($"{nameof(Validate_Create_TypedReferenceRefField)}...");

        Validate v = new();
        Create_TypedReferenceRefField_Worker(v, 1);
    }
}