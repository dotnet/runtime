// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using Xunit;

namespace MemoryMarshalGetArrayDataReferenceTest
{
    public class Program
    {
        private static int _errors = 0;

        [Fact]
        public unsafe static int TestEntryPoint()
        {
            // use no inline methods to avoid indirect call inlining in the future
            [MethodImpl(MethodImplOptions.NoInlining)]
            static delegate*<byte[], ref byte> GetBytePtr() => &MemoryMarshal.GetArrayDataReference<byte>;
            delegate*<byte[], ref byte> ptrByte = GetBytePtr();
            [MethodImpl(MethodImplOptions.NoInlining)]
            static delegate*<string[], ref string> GetStringPtr() => &MemoryMarshal.GetArrayDataReference<string>;
            delegate*<string[], ref string> ptrString = GetStringPtr();

            [MethodImpl(MethodImplOptions.NoInlining)]
            static T NoInline<T>(T t) => t;

            byte[] testByteArray = new byte[1];
            IsTrue(Unsafe.AreSame(ref MemoryMarshal.GetArrayDataReference(testByteArray), ref testByteArray[0]));
            IsTrue(Unsafe.AreSame(ref ptrByte(testByteArray), ref testByteArray[0]));

            IsTrue(Unsafe.AreSame(ref MemoryMarshal.GetArrayDataReference(NoInline(testByteArray)), ref testByteArray[0]));
            IsTrue(Unsafe.AreSame(ref ptrByte(NoInline(testByteArray)), ref testByteArray[0]));

            string[] testStringArray = new string[1];
            IsTrue(Unsafe.AreSame(ref MemoryMarshal.GetArrayDataReference(testStringArray), ref testStringArray[0]));
            IsTrue(Unsafe.AreSame(ref ptrString(testStringArray), ref testStringArray[0]));

            IsTrue(Unsafe.AreSame(ref MemoryMarshal.GetArrayDataReference(NoInline(testStringArray)), ref testStringArray[0]));
            IsTrue(Unsafe.AreSame(ref ptrString(NoInline(testStringArray)), ref testStringArray[0]));

            IsFalse(Unsafe.IsNullRef(ref MemoryMarshal.GetArrayDataReference(new byte[0])));
            IsFalse(Unsafe.IsNullRef(ref MemoryMarshal.GetArrayDataReference(new string[0])));
            IsFalse(Unsafe.IsNullRef(ref MemoryMarshal.GetArrayDataReference(new Half[0])));
            IsFalse(Unsafe.IsNullRef(ref MemoryMarshal.GetArrayDataReference(new Vector128<byte>[0])));
            IsFalse(Unsafe.IsNullRef(ref MemoryMarshal.GetArrayDataReference(new StructWithByte[0])));
            IsFalse(Unsafe.IsNullRef(ref MemoryMarshal.GetArrayDataReference(new SimpleEnum[0])));
            IsFalse(Unsafe.IsNullRef(ref MemoryMarshal.GetArrayDataReference(new GenericStruct<byte>[0])));
            IsFalse(Unsafe.IsNullRef(ref MemoryMarshal.GetArrayDataReference(new GenericStruct<string>[0])));

            IsFalse(Unsafe.IsNullRef(ref ptrByte(new byte[0])));
            IsFalse(Unsafe.IsNullRef(ref ptrString(new string[0])));

            IsFalse(Unsafe.IsNullRef(ref MemoryMarshal.GetArrayDataReference(NoInline(new byte[0]))));
            IsFalse(Unsafe.IsNullRef(ref MemoryMarshal.GetArrayDataReference(NoInline(new string[0]))));
            IsFalse(Unsafe.IsNullRef(ref MemoryMarshal.GetArrayDataReference(NoInline(new Half[0]))));
            IsFalse(Unsafe.IsNullRef(ref MemoryMarshal.GetArrayDataReference(NoInline(new Vector128<byte>[0]))));
            IsFalse(Unsafe.IsNullRef(ref MemoryMarshal.GetArrayDataReference(NoInline(new StructWithByte[0]))));
            IsFalse(Unsafe.IsNullRef(ref MemoryMarshal.GetArrayDataReference(NoInline(new SimpleEnum[0]))));
            IsFalse(Unsafe.IsNullRef(ref MemoryMarshal.GetArrayDataReference(NoInline(new GenericStruct<byte>[0]))));
            IsFalse(Unsafe.IsNullRef(ref MemoryMarshal.GetArrayDataReference(NoInline(new GenericStruct<string>[0]))));

            IsFalse(Unsafe.IsNullRef(ref ptrByte(NoInline(new byte[0]))));
            IsFalse(Unsafe.IsNullRef(ref ptrString(NoInline(new string[0]))));

            ThrowsNRE(() => { _ = ref MemoryMarshal.GetArrayDataReference<byte>(null); });
            ThrowsNRE(() => { _ = ref MemoryMarshal.GetArrayDataReference<string>(null); });
            ThrowsNRE(() => { _ = ref MemoryMarshal.GetArrayDataReference<Half>(null); });
            ThrowsNRE(() => { _ = ref MemoryMarshal.GetArrayDataReference<Vector128<byte>>(null); });
            ThrowsNRE(() => { _ = ref MemoryMarshal.GetArrayDataReference<StructWithByte>(null); });
            ThrowsNRE(() => { _ = ref MemoryMarshal.GetArrayDataReference<SimpleEnum>(null); });
            ThrowsNRE(() => { _ = ref MemoryMarshal.GetArrayDataReference<GenericStruct<byte>>(null); });
            ThrowsNRE(() => { _ = ref MemoryMarshal.GetArrayDataReference<GenericStruct<string>>(null); });

            ThrowsNRE(() => { _ = ref ptrByte(null); });
            ThrowsNRE(() => { _ = ref ptrString(null); });

            ThrowsNRE(() => { _ = ref MemoryMarshal.GetArrayDataReference(NoInline<byte[]>(null)); });
            ThrowsNRE(() => { _ = ref MemoryMarshal.GetArrayDataReference(NoInline<string[]>(null)); });
            ThrowsNRE(() => { _ = ref MemoryMarshal.GetArrayDataReference(NoInline<Half[]>(null)); });
            ThrowsNRE(() => { _ = ref MemoryMarshal.GetArrayDataReference(NoInline<Vector128<byte>[]>(null)); });
            ThrowsNRE(() => { _ = ref MemoryMarshal.GetArrayDataReference(NoInline<StructWithByte[]>(null)); });
            ThrowsNRE(() => { _ = ref MemoryMarshal.GetArrayDataReference(NoInline<SimpleEnum[]>(null)); });
            ThrowsNRE(() => { _ = ref MemoryMarshal.GetArrayDataReference(NoInline<GenericStruct<byte>[]>(null)); });
            ThrowsNRE(() => { _ = ref MemoryMarshal.GetArrayDataReference(NoInline<GenericStruct<string>[]>(null)); });

            ThrowsNRE(() => { _ = ref ptrByte(NoInline<byte[]>(null)); });
            ThrowsNRE(() => { _ = ref ptrString(NoInline<string[]>(null)); });

            ThrowsNRE(() => ref MemoryMarshal.GetArrayDataReference<byte>(null));
            ThrowsNRE(() => ref MemoryMarshal.GetArrayDataReference<string>(null));
            ThrowsNRE(() => ref MemoryMarshal.GetArrayDataReference<Half>(null));
            ThrowsNRE(() => ref MemoryMarshal.GetArrayDataReference<Vector128<byte>>(null));
            ThrowsNRE(() => ref MemoryMarshal.GetArrayDataReference<StructWithByte>(null));
            ThrowsNRE(() => ref MemoryMarshal.GetArrayDataReference<SimpleEnum>(null));
            ThrowsNRE(() => ref MemoryMarshal.GetArrayDataReference<GenericStruct<byte>>(null));
            ThrowsNRE(() => ref MemoryMarshal.GetArrayDataReference<GenericStruct<string>>(null));

            ThrowsNRE(() => ref ptrByte(null));
            ThrowsNRE(() => ref ptrString(null));

            ThrowsNRE(() => ref MemoryMarshal.GetArrayDataReference(NoInline<byte[]>(null)));
            ThrowsNRE(() => ref MemoryMarshal.GetArrayDataReference(NoInline<string[]>(null)));
            ThrowsNRE(() => ref MemoryMarshal.GetArrayDataReference(NoInline<Half[]>(null)));
            ThrowsNRE(() => ref MemoryMarshal.GetArrayDataReference(NoInline<Vector128<byte>[]>(null)));
            ThrowsNRE(() => ref MemoryMarshal.GetArrayDataReference(NoInline<StructWithByte[]>(null)));
            ThrowsNRE(() => ref MemoryMarshal.GetArrayDataReference(NoInline<SimpleEnum[]>(null)));
            ThrowsNRE(() => ref MemoryMarshal.GetArrayDataReference(NoInline<GenericStruct<byte>[]>(null)));
            ThrowsNRE(() => ref MemoryMarshal.GetArrayDataReference(NoInline<GenericStruct<string>[]>(null)));

            ThrowsNRE(() => ref ptrByte(NoInline<byte[]>(null)));
            ThrowsNRE(() => ref ptrString(NoInline<string[]>(null)));

            // use no inline methods to avoid indirect call inlining in the future
            [MethodImpl(MethodImplOptions.NoInlining)]
            static delegate*<Array, ref byte> GetMdPtr() => &MemoryMarshal.GetArrayDataReference;
            delegate*<Array, ref byte> ptrMd = GetMdPtr();

            IsTrue(Unsafe.AreSame(ref MemoryMarshal.GetArrayDataReference((Array)testByteArray), ref testByteArray[0]));
            IsTrue(Unsafe.AreSame(ref ptrMd(testByteArray), ref testByteArray[0]));

            IsTrue(Unsafe.AreSame(ref MemoryMarshal.GetArrayDataReference((Array)NoInline(testByteArray)), ref testByteArray[0]));
            IsTrue(Unsafe.AreSame(ref MemoryMarshal.GetArrayDataReference(NoInline<Array>(testByteArray)), ref testByteArray[0]));
            IsTrue(Unsafe.AreSame(ref ptrMd(NoInline(testByteArray)), ref testByteArray[0]));

            IsTrue(Unsafe.AreSame(ref Unsafe.As<byte, string>(ref MemoryMarshal.GetArrayDataReference((Array)testStringArray)), ref testStringArray[0]));
            IsTrue(Unsafe.AreSame(ref Unsafe.As<byte, string>(ref ptrMd(testStringArray)), ref testStringArray[0]));

            IsTrue(Unsafe.AreSame(ref Unsafe.As<byte, string>(ref MemoryMarshal.GetArrayDataReference((Array)NoInline(testStringArray))), ref testStringArray[0]));
            IsTrue(Unsafe.AreSame(ref Unsafe.As<byte, string>(ref MemoryMarshal.GetArrayDataReference(NoInline<Array>(testStringArray))), ref testStringArray[0]));
            IsTrue(Unsafe.AreSame(ref Unsafe.As<byte, string>(ref ptrMd(NoInline(testStringArray))), ref testStringArray[0]));

            byte[,] testByteMdArray = new byte[1, 1];
            IsTrue(Unsafe.AreSame(ref MemoryMarshal.GetArrayDataReference(testByteMdArray), ref testByteMdArray[0, 0]));
            IsTrue(Unsafe.AreSame(ref ptrMd(testByteMdArray), ref testByteMdArray[0, 0]));

            IsTrue(Unsafe.AreSame(ref MemoryMarshal.GetArrayDataReference(NoInline(testByteMdArray)), ref testByteMdArray[0, 0]));
            IsTrue(Unsafe.AreSame(ref ptrMd(NoInline(testByteMdArray)), ref testByteMdArray[0, 0]));

            string[,] testStringMdArray = new string[1, 1];
            IsTrue(Unsafe.AreSame(ref Unsafe.As<byte, string>(ref MemoryMarshal.GetArrayDataReference(testStringMdArray)), ref testStringMdArray[0, 0]));
            IsTrue(Unsafe.AreSame(ref Unsafe.As<byte, string>(ref ptrMd(testStringMdArray)), ref testStringMdArray[0, 0]));

            IsTrue(Unsafe.AreSame(ref Unsafe.As<byte, string>(ref MemoryMarshal.GetArrayDataReference(NoInline(testStringMdArray))), ref testStringMdArray[0, 0]));
            IsTrue(Unsafe.AreSame(ref Unsafe.As<byte, string>(ref ptrMd(NoInline(testStringMdArray))), ref testStringMdArray[0, 0]));

            if (TestLibrary.PlatformDetection.IsNonZeroLowerBoundArraySupported)
            {
                Array nonZeroArray = Array.CreateInstance(typeof(string), new [] { 1 }, new [] { -1 });
                string test = "test";
                nonZeroArray.SetValue(test, -1);
                IsTrue(ReferenceEquals(Unsafe.As<byte, string>(ref MemoryMarshal.GetArrayDataReference(nonZeroArray)), test));
                IsTrue(ReferenceEquals(Unsafe.As<byte, string>(ref ptrMd(nonZeroArray)), test));

                IsTrue(ReferenceEquals(Unsafe.As<byte, string>(ref MemoryMarshal.GetArrayDataReference(NoInline(nonZeroArray))), test));
                IsTrue(ReferenceEquals(Unsafe.As<byte, string>(ref ptrMd(NoInline(nonZeroArray))), test));
            }

            IsFalse(Unsafe.IsNullRef(ref MemoryMarshal.GetArrayDataReference((Array)new byte[0])));
            IsFalse(Unsafe.IsNullRef(ref MemoryMarshal.GetArrayDataReference((Array)new string[0])));
            IsFalse(Unsafe.IsNullRef(ref MemoryMarshal.GetArrayDataReference(new byte[0, 0])));
            IsFalse(Unsafe.IsNullRef(ref MemoryMarshal.GetArrayDataReference(new string[0, 0])));

            IsFalse(Unsafe.IsNullRef(ref ptrMd(new byte[0])));
            IsFalse(Unsafe.IsNullRef(ref ptrMd(new string[0])));
            IsFalse(Unsafe.IsNullRef(ref ptrMd(new byte[0, 0])));
            IsFalse(Unsafe.IsNullRef(ref ptrMd(new string[0, 0])));

            IsFalse(Unsafe.IsNullRef(ref MemoryMarshal.GetArrayDataReference((Array)NoInline(new byte[0]))));
            IsFalse(Unsafe.IsNullRef(ref MemoryMarshal.GetArrayDataReference((Array)NoInline(new string[0]))));
            IsFalse(Unsafe.IsNullRef(ref MemoryMarshal.GetArrayDataReference(NoInline(new byte[0, 0]))));
            IsFalse(Unsafe.IsNullRef(ref MemoryMarshal.GetArrayDataReference(NoInline(new string[0, 0]))));
            IsFalse(Unsafe.IsNullRef(ref MemoryMarshal.GetArrayDataReference(NoInline<Array>(new byte[0]))));
            IsFalse(Unsafe.IsNullRef(ref MemoryMarshal.GetArrayDataReference(NoInline<Array>(new string[0]))));
            IsFalse(Unsafe.IsNullRef(ref MemoryMarshal.GetArrayDataReference(NoInline<Array>(new byte[0, 0]))));
            IsFalse(Unsafe.IsNullRef(ref MemoryMarshal.GetArrayDataReference(NoInline<Array>(new string[0, 0]))));

            IsFalse(Unsafe.IsNullRef(ref ptrMd(NoInline(new byte[0]))));
            IsFalse(Unsafe.IsNullRef(ref ptrMd(NoInline(new string[0]))));
            IsFalse(Unsafe.IsNullRef(ref ptrMd(NoInline(new byte[0, 0]))));
            IsFalse(Unsafe.IsNullRef(ref ptrMd(NoInline(new string[0, 0]))));
            IsFalse(Unsafe.IsNullRef(ref ptrMd(NoInline<Array>(new byte[0]))));
            IsFalse(Unsafe.IsNullRef(ref ptrMd(NoInline<Array>(new string[0]))));
            IsFalse(Unsafe.IsNullRef(ref ptrMd(NoInline<Array>(new byte[0, 0]))));
            IsFalse(Unsafe.IsNullRef(ref ptrMd(NoInline<Array>(new string[0, 0]))));

            ThrowsNRE(() => { _ = ref MemoryMarshal.GetArrayDataReference((Array)null); });
            ThrowsNRE(() => { _ = ref ptrMd(null); });

            ThrowsNRE(() => { _ = ref MemoryMarshal.GetArrayDataReference((Array)NoInline<byte[]>(null)); });
            ThrowsNRE(() => { _ = ref MemoryMarshal.GetArrayDataReference((Array)NoInline<string[]>(null)); });
            ThrowsNRE(() => { _ = ref MemoryMarshal.GetArrayDataReference(NoInline<Array>(null)); });

            ThrowsNRE(() => { _ = ref ptrMd(NoInline<byte[]>(null)); });
            ThrowsNRE(() => { _ = ref ptrMd(NoInline<string[]>(null)); });
            ThrowsNRE(() => { _ = ref ptrMd(NoInline<Array>(null)); });

            ThrowsNRE(() => ref MemoryMarshal.GetArrayDataReference((Array)null));
            ThrowsNRE(() => ref ptrMd(null));

            ThrowsNRE(() => ref MemoryMarshal.GetArrayDataReference((Array)NoInline<byte[]>(null)));
            ThrowsNRE(() => ref MemoryMarshal.GetArrayDataReference((Array)NoInline<string[]>(null)));
            ThrowsNRE(() => ref MemoryMarshal.GetArrayDataReference(NoInline<Array>(null)));

            ThrowsNRE(() => ref ptrMd(NoInline<byte[]>(null)));
            ThrowsNRE(() => ref ptrMd(NoInline<string[]>(null)));
            ThrowsNRE(() => ref ptrMd(NoInline<Array>(null)));

            // from https://github.com/dotnet/runtime/issues/58312#issuecomment-993491291
            [MethodImpl(MethodImplOptions.NoInlining)]
            static int Problem1(StructWithByte[] a)
            {
                MemoryMarshal.GetArrayDataReference(a).Byte = 1;

                a[0].Byte = 2;

                return MemoryMarshal.GetArrayDataReference(a).Byte;
            }

            Equals(Problem1(new StructWithByte[] { new StructWithByte { Byte = 1 } }), 2);

            [MethodImpl(MethodImplOptions.NoInlining)]
            static int Problem2(StructWithByte[] a)
            {
                Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(a), 1).Byte = 1;

                a[1].Byte = 2;

                return Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(a), 1).Byte;
            }

            Equals(Problem2(new StructWithByte[] { new StructWithByte { Byte = 1 }, new StructWithByte { Byte = 1 } }), 2);

            [MethodImpl(MethodImplOptions.NoInlining)]
            static int Problem3(byte[] a)
            {
                if (MemoryMarshal.GetArrayDataReference(a) == 1)
                {
                    a[0] = 2;
                    if (MemoryMarshal.GetArrayDataReference(a) == 1)
                    {
                        return -1;
                    }
                }

                return 0;
            }

            Equals(Problem3(new byte[] { 1 }), 0);

            [MethodImpl(MethodImplOptions.NoInlining)]
            static int Problem4(byte[] a)
            {
                if (Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(a), 1) == 1)
                {
                    a[1] = 2;
                    if (Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(a), 1) == 1)
                    {
                        return -1;
                    }
                }

                return 0;
            }

            Equals(Problem4(new byte[] { 1, 1 }), 0);

            [MethodImpl(MethodImplOptions.NoInlining)]
            static void Problem5()
            {
                int[] inputArray = CreateArray(17);

                Create(out Vector<int> v, inputArray, inputArray.Length);

                static void Create(out Vector<int> result, int[] values, int index)
                {
                    // We explicitly don't check for `null` because historically this has thrown `NullReferenceException` for perf reasons

                    if ((index < 0) || ((values.Length - index) < Vector<int>.Count))
                    {
                        ThrowArgumentOutOfRangeException();
                    }

                    result = Unsafe.ReadUnaligned<Vector<int>>(ref Unsafe.As<int, byte>(ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(values), index)));
                }

                static void ThrowArgumentOutOfRangeException()
                {
                    throw new ArgumentOutOfRangeException();
                }

                [MethodImpl(MethodImplOptions.NoInlining)]
                static int[] CreateArray(int size) => new int[size];
            }

            try
            {
                Problem5();
                _errors++;
            }
            catch (ArgumentOutOfRangeException)
            {
                // expected
            }
            catch
            {
                _errors++;
            }

            return 100 + _errors;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static void Equals<T>(T left, T right, [CallerLineNumber] int line = 0, [CallerFilePath] string file = "")
        {
            if (!EqualityComparer<T>.Default.Equals(left, right))
            {
                Console.WriteLine($"{file}:L{line} test failed (expected: equal, actual: {left}-{right}).");
                _errors++;
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static void IsTrue(bool expression, [CallerLineNumber] int line = 0, [CallerFilePath] string file = "")
        {
            if (!expression)
            {
                Console.WriteLine($"{file}:L{line} test failed (expected: true).");
                _errors++;
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static void IsFalse(bool expression, [CallerLineNumber] int line = 0, [CallerFilePath] string file = "")
        {
            if (expression)
            {
                Console.WriteLine($"{file}:L{line} test failed (expected: false).");
                _errors++;
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ThrowsNRE(Action action, [CallerLineNumber] int line = 0, [CallerFilePath] string file = "")
        {
            try
            {
                action();
            }
            catch (NullReferenceException)
            {
                return;
            }
            catch (Exception exc)
            {
                Console.WriteLine($"{file}:L{line} {exc}");
            }
            Console.WriteLine($"Line {line}: test failed (expected: NullReferenceException)");
            _errors++;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ThrowsNRE<T>(RefFunction<T> function, [CallerLineNumber] int line = 0, [CallerFilePath] string file = "")
        {
            try
            {
                [MethodImpl(MethodImplOptions.NoInlining)]
                static void Use(ref T t) { }
                Use(ref function());
            }
            catch (NullReferenceException)
            {
                return;
            }
            catch (Exception exc)
            {
                Console.WriteLine($"{file}:L{line} {exc}");
            }
            Console.WriteLine($"Line {line}: test failed (expected: NullReferenceException)");
            _errors++;
        }

        public struct GenericStruct<T>
        {
            public T field;
        }

        public enum SimpleEnum
        {
            A,B,C
        }

        struct StructWithByte
        {
            public byte Byte;
        }

        delegate ref T RefFunction<T>();
    }
}
