// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System;
using System.Threading;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace InterlockedTest
{
    class Program
    {
        private static int _errors = 0;

        unsafe static int Main(string[] args)
        {
            // use no inline methods to avoid indirect call inlining in the future
            [MethodImpl(MethodImplOptions.NoInlining)]
            static delegate*<ref byte, byte, byte> ExchangeByte() => &Interlocked.Exchange<byte>;
            [MethodImpl(MethodImplOptions.NoInlining)]
            static delegate*<ref short, short, short> ExchangeShort() => &Interlocked.Exchange<short>;
            [MethodImpl(MethodImplOptions.NoInlining)]
            static delegate*<ref sbyte, sbyte, sbyte> ExchangeSByte() => &Interlocked.Exchange<sbyte>;
            [MethodImpl(MethodImplOptions.NoInlining)]
            static delegate*<ref ushort, ushort, ushort> ExchangeUShort() => &Interlocked.Exchange<ushort>;
            [MethodImpl(MethodImplOptions.NoInlining)]
            static delegate*<ref byte, byte, byte, byte> CompareExchangeByte() => &Interlocked.CompareExchange<byte>;
            [MethodImpl(MethodImplOptions.NoInlining)]
            static delegate*<ref short, short, short, short> CompareExchangeShort() => &Interlocked.CompareExchange<short>;
            [MethodImpl(MethodImplOptions.NoInlining)]
            static delegate*<ref sbyte, sbyte, sbyte, sbyte> CompareExchangeSByte() => &Interlocked.CompareExchange<sbyte>;
            [MethodImpl(MethodImplOptions.NoInlining)]
            static delegate*<ref ushort, ushort, ushort, ushort> CompareExchangeUShort() => &Interlocked.CompareExchange<ushort>;

            [MethodImpl(MethodImplOptions.NoInlining)]
            static long GetValue<T>(T val) where T : unmanaged
            {
                long v = -1;
                Unsafe.As<long, T>(ref v) = val;
                return v;
            }

            long l = -1;

            Equals(255, Interlocked.Exchange(ref Unsafe.As<long, byte>(ref l), 123));
            Equals(GetValue<byte>(123), l);
            Equals(123, ExchangeByte()(ref Unsafe.As<long, byte>(ref l), 124));
            Equals(GetValue<byte>(124), l);
            Equals(124, Interlocked.Exchange(ref Unsafe.As<long, sbyte>(ref l), 125));
            Equals(GetValue<sbyte>(125), l);
            Equals(125, ExchangeSByte()(ref Unsafe.As<long, sbyte>(ref l), 126));
            Equals(GetValue<sbyte>(126), l);

            Equals(126, Interlocked.CompareExchange(ref Unsafe.As<long, byte>(ref l), 126, 55));
            Equals(GetValue<byte>(55), l);
            Equals(55, CompareExchangeByte()(ref Unsafe.As<long, byte>(ref l), 55, 56));
            Equals(GetValue<byte>(56), l);
            Equals(56, Interlocked.CompareExchange(ref Unsafe.As<long, sbyte>(ref l), 56, 57));
            Equals(GetValue<sbyte>(57), l);
            Equals(57, CompareExchangeSByte()(ref Unsafe.As<long, sbyte>(ref l), 57, 58));
            Equals(GetValue<sbyte>(58), l);

            Equals(58, Interlocked.CompareExchange(ref Unsafe.As<long, byte>(ref l), 2, 10));
            Equals(GetValue<byte>(58), l);
            Equals(58, CompareExchangeByte()(ref Unsafe.As<long, byte>(ref l), 2, 10));
            Equals(GetValue<byte>(58), l);
            Equals(58, Interlocked.CompareExchange(ref Unsafe.As<long, sbyte>(ref l), 2, 10));
            Equals(GetValue<sbyte>(58), l);
            Equals(58, CompareExchangeSByte()(ref Unsafe.As<long, sbyte>(ref l), 2, 10));
            Equals(GetValue<sbyte>(58), l);

            l = -1;

            Equals(-1, Interlocked.Exchange(ref Unsafe.As<long, short>(ref l), 12345));
            Equals(GetValue<short>(12345), l);
            Equals(12345, ExchangeShort()(ref Unsafe.As<long, short>(ref l), 12346));
            Equals(GetValue<short>(12346), l);
            Equals(12346, Interlocked.Exchange(ref Unsafe.As<long, ushort>(ref l), 12347));
            Equals(GetValue<ushort>(12347), l);
            Equals(12347, ExchangeUShort()(ref Unsafe.As<long, ushort>(ref l), 12348));
            Equals(GetValue<ushort>(12348), l);

            Equals(12348, Interlocked.CompareExchange(ref Unsafe.As<long, short>(ref l), 12348, 1234));
            Equals(GetValue<short>(1234), l);
            Equals(55, CompareExchangeShort()(ref Unsafe.As<long, short>(ref l), 1234, 1235));
            Equals(GetValue<short>(1235), l);
            Equals(56, Interlocked.CompareExchange(ref Unsafe.As<long, ushort>(ref l), 1235, 1236));
            Equals(GetValue<ushort>(1236), l);
            Equals(57, CompareExchangeUShort()(ref Unsafe.As<long, ushort>(ref l), 1236, 1237));
            Equals(GetValue<ushort>(1237), l);

            Equals(58, Interlocked.CompareExchange(ref Unsafe.As<long, short>(ref l), 1444, 1555));
            Equals(GetValue<short>(1237), l);
            Equals(58, CompareExchangeShort()(ref Unsafe.As<long, short>(ref l), 1444, 1555));
            Equals(GetValue<short>(1237), l);
            Equals(58, Interlocked.CompareExchange(ref Unsafe.As<long, ushort>(ref l), 1444, 1555));
            Equals(GetValue<ushort>(1237), l);
            Equals(58, CompareExchangeUShort()(ref Unsafe.As<long, ushort>(ref l), 1444, 1555));
            Equals(GetValue<ushort>(1237), l);

            l = -1;

            Interlocked.Exchange(ref Unsafe.As<long, byte>(ref l), 123);
            Equals(GetValue<byte>(123), l);
            ExchangeByte()(ref Unsafe.As<long, byte>(ref l), 124);
            Equals(GetValue<byte>(124), l);
            Interlocked.Exchange(ref Unsafe.As<long, sbyte>(ref l), 125);
            Equals(GetValue<sbyte>(125), l);
            ExchangeSByte()(ref Unsafe.As<long, sbyte>(ref l), 126);
            Equals(GetValue<sbyte>(126), l);

            Interlocked.CompareExchange(ref Unsafe.As<long, byte>(ref l), 126, 55);
            Equals(GetValue<byte>(55), l);
            CompareExchangeByte()(ref Unsafe.As<long, byte>(ref l), 55, 56);
            Equals(GetValue<byte>(56), l);
            Interlocked.CompareExchange(ref Unsafe.As<long, sbyte>(ref l), 56, 57);
            Equals(GetValue<sbyte>(57), l);
            CompareExchangeSByte()(ref Unsafe.As<long, sbyte>(ref l), 57, 58);
            Equals(GetValue<sbyte>(58), l);

            Interlocked.CompareExchange(ref Unsafe.As<long, byte>(ref l), 2, 10);
            Equals(GetValue<byte>(58), l);
            CompareExchangeByte()(ref Unsafe.As<long, byte>(ref l), 2, 10);
            Equals(GetValue<byte>(58), l);
            Interlocked.CompareExchange(ref Unsafe.As<long, sbyte>(ref l), 2, 10);
            Equals(GetValue<sbyte>(58), l);
            CompareExchangeSByte()(ref Unsafe.As<long, sbyte>(ref l), 2, 10);
            Equals(GetValue<sbyte>(58), l);

            l = -1;

            Interlocked.Exchange(ref Unsafe.As<long, short>(ref l), 12345);
            Equals(GetValue<short>(12345), l);
            ExchangeShort()(ref Unsafe.As<long, short>(ref l), 12346);
            Equals(GetValue<short>(12346), l);
            Interlocked.Exchange(ref Unsafe.As<long, ushort>(ref l), 12347);
            Equals(GetValue<ushort>(12347), l);
            ExchangeUShort()(ref Unsafe.As<long, ushort>(ref l), 12348);
            Equals(GetValue<ushort>(12348), l);

            Interlocked.CompareExchange(ref Unsafe.As<long, short>(ref l), 12348, 1234);
            Equals(GetValue<short>(1234), l);
            CompareExchangeShort()(ref Unsafe.As<long, short>(ref l), 1234, 1235);
            Equals(GetValue<short>(1235), l);
            Interlocked.CompareExchange(ref Unsafe.As<long, ushort>(ref l), 1235, 1236);
            Equals(GetValue<ushort>(1236), l);
            CompareExchangeUShort()(ref Unsafe.As<long, ushort>(ref l), 1236, 1237);
            Equals(GetValue<ushort>(1237), l);

            Interlocked.CompareExchange(ref Unsafe.As<long, short>(ref l), 1444, 1555);
            Equals(GetValue<short>(1237), l);
            CompareExchangeShort()(ref Unsafe.As<long, short>(ref l), 1444, 1555);
            Equals(GetValue<short>(1237), l);
            Interlocked.CompareExchange(ref Unsafe.As<long, ushort>(ref l), 1444, 1555);
            Equals(GetValue<ushort>(1237), l);
            CompareExchangeUShort()(ref Unsafe.As<long, ushort>(ref l), 1444, 1555);
            Equals(GetValue<ushort>(1237), l);

            ThrowsNRE(() => { Interlocked.Exchange(ref Unsafe.NullRef<byte>(), 0); });
            ThrowsNRE(() => { Interlocked.Exchange(ref Unsafe.NullRef<sbyte>(), 0); });
            ThrowsNRE(() => { Interlocked.Exchange(ref Unsafe.NullRef<short>(), 0); });
            ThrowsNRE(() => { Interlocked.Exchange(ref Unsafe.NullRef<ushort>(), 0); });
            ThrowsNRE(() => { Interlocked.CompareExchange(ref Unsafe.NullRef<byte>(), 0, 0); });
            ThrowsNRE(() => { Interlocked.CompareExchange(ref Unsafe.NullRef<sbyte>(), 0, 0); });
            ThrowsNRE(() => { Interlocked.CompareExchange(ref Unsafe.NullRef<short>(), 0, 0); });
            ThrowsNRE(() => { Interlocked.CompareExchange(ref Unsafe.NullRef<ushort>(), 0, 0); });
            ThrowsNRE(() => { ExchangeByte()(ref Unsafe.NullRef<byte>(), 0); });
            ThrowsNRE(() => { ExchangeSByte()(ref Unsafe.NullRef<sbyte>(), 0); });
            ThrowsNRE(() => { ExchangeShort()(ref Unsafe.NullRef<short>(), 0); });
            ThrowsNRE(() => { ExchangeUShort()(ref Unsafe.NullRef<ushort>(), 0); });
            ThrowsNRE(() => { CompareExchangeByte()(ref Unsafe.NullRef<byte>(), 0, 0); });
            ThrowsNRE(() => { CompareExchangeSByte()(ref Unsafe.NullRef<sbyte>(), 0, 0); });
            ThrowsNRE(() => { CompareExchangeShort()(ref Unsafe.NullRef<short>(), 0, 0); });
            ThrowsNRE(() => { CompareExchangeUShort()(ref Unsafe.NullRef<ushort>(), 0, 0); });

            return 100 + _errors;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static void Equals(long left, long right, [CallerLineNumber] int line = 0, [CallerFilePath] string file = "")
        {
            if (left != right)
            {
                Console.WriteLine($"{file}:L{line} test failed (expected: equal, actual: {left}-{right}).");
                _errors++;
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ThrowsNRE<T>(Action action, [CallerLineNumber] int line = 0, [CallerFilePath] string file = "")
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
    }
}
