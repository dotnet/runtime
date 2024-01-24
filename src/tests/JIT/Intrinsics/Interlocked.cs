// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System;
using System.Threading;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Xunit;

namespace InterlockedTest
{
    public unsafe class Program
    {
        private static int _errors = 0;

        [Fact]
        public static int TestEntryPoint()
        {
            // use no inline methods to avoid indirect call inlining in the future
            [MethodImpl(MethodImplOptions.NoInlining)]
            static delegate*<ref byte, byte, byte> ExchangeByte() => &Interlocked.Exchange;
            [MethodImpl(MethodImplOptions.NoInlining)]
            static delegate*<ref short, short, short> ExchangeShort() => &Interlocked.Exchange;
            [MethodImpl(MethodImplOptions.NoInlining)]
            static delegate*<ref sbyte, sbyte, sbyte> ExchangeSByte() => &Interlocked.Exchange;
            [MethodImpl(MethodImplOptions.NoInlining)]
            static delegate*<ref ushort, ushort, ushort> ExchangeUShort() => &Interlocked.Exchange;
            [MethodImpl(MethodImplOptions.NoInlining)]
            static delegate*<ref byte, byte, byte, byte> CompareExchangeByte() => &Interlocked.CompareExchange;
            [MethodImpl(MethodImplOptions.NoInlining)]
            static delegate*<ref short, short, short, short> CompareExchangeShort() => &Interlocked.CompareExchange;
            [MethodImpl(MethodImplOptions.NoInlining)]
            static delegate*<ref sbyte, sbyte, sbyte, sbyte> CompareExchangeSByte() => &Interlocked.CompareExchange;
            [MethodImpl(MethodImplOptions.NoInlining)]
            static delegate*<ref ushort, ushort, ushort, ushort> CompareExchangeUShort() => &Interlocked.CompareExchange;

            long mem = -1;
            [MethodImpl(MethodImplOptions.NoInlining)]
            long GetValue<T>(T val) where T : unmanaged
            {
                Unsafe.As<long, T>(ref mem) = val;
                return mem;
            }

            long l = -1;
            Equals(255, Interlocked.Exchange(ref Unsafe.As<long, byte>(ref l), 254));
            Equals(GetValue<byte>(254), l);
            Equals(254, ExchangeByte()(ref Unsafe.As<long, byte>(ref l), 253));
            Equals(GetValue<byte>(253), l);

            mem = 0;
            l = 0;
            Equals(0, Interlocked.Exchange(ref Unsafe.As<long, sbyte>(ref l), -4));
            Equals(GetValue<sbyte>(-4), l);
            Equals(-4, ExchangeSByte()(ref Unsafe.As<long, sbyte>(ref l), -5));
            Equals(GetValue<sbyte>(-5), l);

            mem = -1;
            l = -1;
            Equals(255, Interlocked.CompareExchange(ref Unsafe.As<long, byte>(ref l), 254, 255));
            Equals(GetValue<byte>(254), l);
            Equals(254, CompareExchangeByte()(ref Unsafe.As<long, byte>(ref l), 253, 254));
            Equals(GetValue<byte>(253), l);

            mem = 0;
            l = 0;
            Equals(0, Interlocked.CompareExchange(ref Unsafe.As<long, sbyte>(ref l), -4, 0));
            Equals(GetValue<sbyte>(-4), l);
            Equals(-4, CompareExchangeSByte()(ref Unsafe.As<long, sbyte>(ref l), -5, -4));
            Equals(GetValue<sbyte>(-5), l);

            Equals(251, Interlocked.CompareExchange(ref Unsafe.As<long, byte>(ref l), 2, 10));
            Equals(GetValue<byte>(251), l);
            Equals(251, CompareExchangeByte()(ref Unsafe.As<long, byte>(ref l), 2, 10));
            Equals(GetValue<byte>(251), l);
            Equals(-5, Interlocked.CompareExchange(ref Unsafe.As<long, sbyte>(ref l), 2, 10));
            Equals(GetValue<sbyte>(-5), l);
            Equals(-5, CompareExchangeSByte()(ref Unsafe.As<long, sbyte>(ref l), 2, 10));
            Equals(GetValue<sbyte>(-5), l);

            mem = 0;
            l = 0;
            Equals(0, Interlocked.Exchange(ref Unsafe.As<long, short>(ref l), -2));
            Equals(GetValue<short>(-2), l);
            Equals(-2, ExchangeShort()(ref Unsafe.As<long, short>(ref l), -3));
            Equals(GetValue<short>(-3), l);

            mem = -1;
            l = -1;
            Equals(65535, Interlocked.Exchange(ref Unsafe.As<long, ushort>(ref l), 65532));
            Equals(GetValue<ushort>(65532), l);
            Equals(65532, ExchangeUShort()(ref Unsafe.As<long, ushort>(ref l), 65531));
            Equals(GetValue<ushort>(65531), l);

            mem = 0;
            l = 0;
            Equals(0, Interlocked.CompareExchange(ref Unsafe.As<long, short>(ref l), -2, 0));
            Equals(GetValue<short>(-2), l);
            Equals(-2, CompareExchangeShort()(ref Unsafe.As<long, short>(ref l), -3, -2));
            Equals(GetValue<short>(-3), l);

            mem = -1;
            l = -1;
            Equals(65535, Interlocked.CompareExchange(ref Unsafe.As<long, ushort>(ref l), 65532, 65535));
            Equals(GetValue<ushort>(65532), l);
            Equals(65532, CompareExchangeUShort()(ref Unsafe.As<long, ushort>(ref l), 65531, 65532));
            Equals(GetValue<ushort>(65531), l);

            Equals(-5, Interlocked.CompareExchange(ref Unsafe.As<long, short>(ref l), 1444, 1555));
            Equals(GetValue<short>(-5), l);
            Equals(-5, CompareExchangeShort()(ref Unsafe.As<long, short>(ref l), 1444, 1555));
            Equals(GetValue<short>(-5), l);
            Equals(65531, Interlocked.CompareExchange(ref Unsafe.As<long, ushort>(ref l), 1444, 1555));
            Equals(GetValue<ushort>(65531), l);
            Equals(65531, CompareExchangeUShort()(ref Unsafe.As<long, ushort>(ref l), 1444, 1555));
            Equals(GetValue<ushort>(65531), l);

            mem = -1;
            l = -1;
            Interlocked.Exchange(ref Unsafe.As<long, byte>(ref l), 123);
            Equals(GetValue<byte>(123), l);
            ExchangeByte()(ref Unsafe.As<long, byte>(ref l), 124);
            Equals(GetValue<byte>(124), l);
            Interlocked.Exchange(ref Unsafe.As<long, sbyte>(ref l), 125);
            Equals(GetValue<sbyte>(125), l);
            ExchangeSByte()(ref Unsafe.As<long, sbyte>(ref l), 126);
            Equals(GetValue<sbyte>(126), l);

            Interlocked.CompareExchange(ref Unsafe.As<long, byte>(ref l), 55, 126);
            Equals(GetValue<byte>(55), l);
            CompareExchangeByte()(ref Unsafe.As<long, byte>(ref l), 56, 55);
            Equals(GetValue<byte>(56), l);
            Interlocked.CompareExchange(ref Unsafe.As<long, sbyte>(ref l), 57, 56);
            Equals(GetValue<sbyte>(57), l);
            CompareExchangeSByte()(ref Unsafe.As<long, sbyte>(ref l), 58, 57);
            Equals(GetValue<sbyte>(58), l);

            Interlocked.CompareExchange(ref Unsafe.As<long, byte>(ref l), 10, 2);
            Equals(GetValue<byte>(58), l);
            CompareExchangeByte()(ref Unsafe.As<long, byte>(ref l), 10, 2);
            Equals(GetValue<byte>(58), l);
            Interlocked.CompareExchange(ref Unsafe.As<long, sbyte>(ref l), 10, 2);
            Equals(GetValue<sbyte>(58), l);
            CompareExchangeSByte()(ref Unsafe.As<long, sbyte>(ref l), 10, 2);
            Equals(GetValue<sbyte>(58), l);

            mem = -1;
            l = -1;
            Interlocked.Exchange(ref Unsafe.As<long, short>(ref l), 12345);
            Equals(GetValue<short>(12345), l);
            ExchangeShort()(ref Unsafe.As<long, short>(ref l), 12346);
            Equals(GetValue<short>(12346), l);
            Interlocked.Exchange(ref Unsafe.As<long, ushort>(ref l), 12347);
            Equals(GetValue<ushort>(12347), l);
            ExchangeUShort()(ref Unsafe.As<long, ushort>(ref l), 12348);
            Equals(GetValue<ushort>(12348), l);

            Interlocked.CompareExchange(ref Unsafe.As<long, short>(ref l), 1234, 12348);
            Equals(GetValue<short>(1234), l);
            CompareExchangeShort()(ref Unsafe.As<long, short>(ref l), 1235, 1234);
            Equals(GetValue<short>(1235), l);
            Interlocked.CompareExchange(ref Unsafe.As<long, ushort>(ref l), 1236, 1235);
            Equals(GetValue<ushort>(1236), l);
            CompareExchangeUShort()(ref Unsafe.As<long, ushort>(ref l), 1237, 1236);
            Equals(GetValue<ushort>(1237), l);

            Interlocked.CompareExchange(ref Unsafe.As<long, short>(ref l), 1555, 1444);
            Equals(GetValue<short>(1237), l);
            CompareExchangeShort()(ref Unsafe.As<long, short>(ref l), 1555, 1444);
            Equals(GetValue<short>(1237), l);
            Interlocked.CompareExchange(ref Unsafe.As<long, ushort>(ref l), 1555, 1444);
            Equals(GetValue<ushort>(1237), l);
            CompareExchangeUShort()(ref Unsafe.As<long, ushort>(ref l), 1555, 1444);
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
    }
}
