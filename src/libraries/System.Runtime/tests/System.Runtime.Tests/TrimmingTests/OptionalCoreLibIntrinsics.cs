// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;

internal static unsafe class Program
{
    private delegate ref int AddInt(ref int value, int offset);
    private delegate ref int AddNative(ref int value, nint offset);
    private delegate ref int AddUnsigned(ref int value, nuint offset);
    private delegate int ReadPointer(void* source);
    private delegate void WritePointer(void* target, int value);

    private static int Main()
    {
        int* values = stackalloc int[4] { 17, 23, 31, 47 };
        // Delegates require callable intrinsic bodies even when the JIT can inline them.
        AddInt addInt = Unsafe.Add<int>;
        AddNative addNative = Unsafe.Add<int>;
        AddUnsigned addUnsigned = Unsafe.Add<int>;
        Check(addInt(ref values[0], 1) == 23 &&
            addNative(ref values[0], 2) == 31 &&
            addUnsigned(ref values[0], 3) == 47, "Unsafe.Add overloads");

        ReadPointer read = Unsafe.Read<int>;
        WritePointer write = Unsafe.Write<int>;
        write(values, 12345);
        Check(read(values) == 12345, "Unsafe.Read and Unsafe.Write");

        Func<DefaultValue> createDefault = Activator.CreateInstance<DefaultValue>;
        Func<ConstructedValue> createConstructed = Activator.CreateInstance<ConstructedValue>;
        Func<ReferenceValue> createReference = Activator.CreateInstance<ReferenceValue>;
        Check(createDefault().Value == 0 &&
            createConstructed().Value == 67 &&
            createReference().Value == 71, "Activator.CreateInstance");

        Check(EqualityComparer<SmallEnum>.Default.Equals(SmallEnum.A, SmallEnum.A) &&
            Comparer<LargeEnum>.Default.Compare(LargeEnum.A, LargeEnum.B) < 0, "enum helpers");
        Check(new[] { new Rune('a') }.AsSpan().SequenceEqual(new[] { new Rune('a') }), "Rune classification");
        Check(new[] { new PlainValue { Value = 37 } }.AsSpan().SequenceEqual(
            new[] { new PlainValue { Value = 37 } }), "struct classification");

        object item = null;
        object replacement = new object();
        Check(Interlocked.CompareExchange(ref item, replacement, null) is null &&
            ReferenceEquals(item, replacement), "Interlocked.CompareExchange");

        return 100;
    }

    private static void Check(bool success, string message)
    {
        if (!success)
            throw new InvalidOperationException(message);
    }

    private struct DefaultValue
    {
        public int Value { get; set; }
    }

    private struct ConstructedValue
    {
        public int Value;
        public ConstructedValue() => Value = 67;
    }

    private struct PlainValue
    {
        public long Value;
    }

    private sealed class ReferenceValue
    {
        public int Value = 71;

        public ReferenceValue() { }
    }

    private enum SmallEnum : byte { A, B }
    private enum LargeEnum : long { A, B }
}
