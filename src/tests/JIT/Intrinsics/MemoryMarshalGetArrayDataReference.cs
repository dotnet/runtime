// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;

namespace MemoryMarshalGetArrayDataReferenceTest
{
    class Program
    {
        private static int _errors = 0;

        unsafe static int Main(string[] args)
        {
            delegate*<byte[], ref byte> ptrByte = &MemoryMarshal.GetArrayDataReference<byte>;
            delegate*<string[], ref string> ptrString = &MemoryMarshal.GetArrayDataReference<string>;

            byte[] testByteArray = new byte[1];
            IsTrue(Unsafe.AreSame(ref MemoryMarshal.GetArrayDataReference(testByteArray), ref testByteArray[0]));
            IsTrue(Unsafe.AreSame(ref ptrByte(testByteArray), ref testByteArray[0]));

            string[] testStringArray = new string[1];
            IsTrue(Unsafe.AreSame(ref MemoryMarshal.GetArrayDataReference(testStringArray), ref testStringArray[0]));
            IsTrue(Unsafe.AreSame(ref ptrString(testStringArray), ref testStringArray[0]));

            IsFalse(Unsafe.IsNullRef(ref MemoryMarshal.GetArrayDataReference(Array.Empty<byte>())));
            IsFalse(Unsafe.IsNullRef(ref MemoryMarshal.GetArrayDataReference(Array.Empty<string>())));
            IsFalse(Unsafe.IsNullRef(ref MemoryMarshal.GetArrayDataReference(Array.Empty<Half>())));
            IsFalse(Unsafe.IsNullRef(ref MemoryMarshal.GetArrayDataReference(Array.Empty<Vector128<byte>>())));
            IsFalse(Unsafe.IsNullRef(ref MemoryMarshal.GetArrayDataReference(Array.Empty<StructWithByte>())));
            IsFalse(Unsafe.IsNullRef(ref MemoryMarshal.GetArrayDataReference(Array.Empty<SimpleEnum>())));
            IsFalse(Unsafe.IsNullRef(ref MemoryMarshal.GetArrayDataReference(Array.Empty<GenericStruct<byte>>())));
            IsFalse(Unsafe.IsNullRef(ref MemoryMarshal.GetArrayDataReference(Array.Empty<GenericStruct<string>>())));

            IsFalse(Unsafe.IsNullRef(ref ptrByte(Array.Empty<byte>())));
            IsFalse(Unsafe.IsNullRef(ref ptrString(Array.Empty<string>())));

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
            static int Problem2(byte[] a)
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

            Equals(Problem2(new byte[] { 1 }), 0);

            return 100 + _errors;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static void Equals<T>(T left, T right, [CallerLineNumber] int line = 0, [CallerFilePath] string file = "")
        {
            if (EqualityComparer<T>.Default.Equals(left, right))
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
    }
}
