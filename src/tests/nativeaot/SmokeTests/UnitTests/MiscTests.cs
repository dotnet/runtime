// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

class MiscTests
{
    internal static int Run()
    {
        TestSurrogateStringLiterals.Run();
        TestLargeStructArrayGC.Run();
        return 100;
    }

    class TestSurrogateStringLiterals
    {
        public static void Run()
        {
            CheckSurrogateLiteral(GetFirstSurrogateLiteral(), '\uD800');
            CheckSurrogateLiteral(GetSecondSurrogateLiteral(), '\uD801');
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static string GetFirstSurrogateLiteral() => "\uD800";

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static string GetSecondSurrogateLiteral() => "\uD801";

        private static void CheckSurrogateLiteral(string value, char expected)
        {
            if (value.Length != 1)
                throw new Exception(value.Length.ToString());

            if (value[0] != expected)
                throw new Exception(((int)value[0]).ToString("X4"));
        }
    }

    // Regression test for GC descriptor encoding of arrays whose element is a large
    // value type containing two GC references separated by a large (32KB-64KB)
    // non-pointer gap. That layout forces the "val_serie" array GCDesc encoding whose
    // per-series run/skip counts are stored in 16-bit-limited fields. A skip in
    // [32768, 65535] must not be produced by a signed (short) cast that then gets
    // sign-extended into the 32-bit field on 64-bit targets: doing so makes the GC
    // walk the array with a bogus stride and drop or corrupt element references.
    //
    // Two encoders are exercised:
    //   * TestSzArray - GCDesc produced by the AOT compiler (GCDescEncoder).
    //   * TestMdArray - GCDesc produced by the runtime type loader
    //                   (EETypeCreator.CreateMdArrayGCDesc) for an array type that
    //                   is only materialized at run time via Array.CreateInstance.
    class TestLargeStructArrayGC
    {
        // Two object references: one at offset 0 and one at offset SecondOffset. The
        // gap between them is 40000 bytes, inside the problematic [32768, 65535]
        // range for the 16-bit skip field.
        const int SecondOffset = 40008;

        [StructLayout(LayoutKind.Explicit, Size = SecondOffset + 8)]
        struct LargeStruct
        {
            [FieldOffset(0)] public object First;
            [FieldOffset(SecondOffset)] public object Second;
        }

        const int ElementCount = 4;

        public static void Run()
        {
            TestSzArray();
            TestMdArray();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static void TestSzArray()
        {
            var array = new LargeStruct[ElementCount];
            var references = new object[2 * ElementCount];
            for (int i = 0; i < array.Length; i++)
            {
                array[i].First = references[2 * i] = MakeReference(i, 'a');
                array[i].Second = references[2 * i + 1] = MakeReference(i, 'A');
            }

            Compact();

            for (int i = 0; i < array.Length; i++)
            {
                Check("SzArray.First", i, references[2 * i], array[i].First);
                Check("SzArray.Second", i, references[2 * i + 1], array[i].Second);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static void TestMdArray()
        {
            // Rank-2 array type materialized at run time by the type loader.
            Array array = Array.CreateInstance(typeof(LargeStruct), ElementCount, 1);
            var references = new object[2 * ElementCount];
            for (int i = 0; i < ElementCount; i++)
            {
                object boxed = new LargeStruct
                {
                    First = references[2 * i] = MakeReference(i, 'a'),
                    Second = references[2 * i + 1] = MakeReference(i, 'A')
                };
                array.SetValue(boxed, i, 0);
            }

            Compact();

            for (int i = 0; i < ElementCount; i++)
            {
                var element = (LargeStruct)array.GetValue(i, 0);
                Check("MdArray.First", i, references[2 * i], element.First);
                Check("MdArray.Second", i, references[2 * i + 1], element.Second);
            }
        }

        // Produce a distinct, verifiable heap object per index/tag (not interned).
        [MethodImpl(MethodImplOptions.NoInlining)]
        static object MakeReference(int i, char tag) => new string(tag, 8 + i);

        // Keep the original objects rooted separately so identity checks detect
        // array references that were not updated when the objects moved.
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void Compact()
        {
            for (int i = 0; i < 1000; i++)
                GC.KeepAlive(new byte[1000]);

            GC.Collect(2, GCCollectionMode.Forced, blocking: true, compacting: true);
            GC.WaitForPendingFinalizers();
            GC.Collect(2, GCCollectionMode.Forced, blocking: true, compacting: true);
        }

        static void Check(string which, int index, object expected, object actual)
        {
            if (!ReferenceEquals(expected, actual))
                throw new Exception($"{which}[{index}]: reference mismatch");
        }
    }
}
