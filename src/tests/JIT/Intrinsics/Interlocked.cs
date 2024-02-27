// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Xunit;

namespace InterlockedTest
{
    public unsafe class Program
    {
        [StructLayout(LayoutKind.Explicit)]
        private sealed class Box
        {
            [FieldOffset(0)]
            private long memory;
            [FieldOffset(8)]
            private long val;
            [FieldOffset(16)]
            public nuint offset;

            public long Memory => memory;

            [MethodImpl(MethodImplOptions.NoInlining)]
            public ref T GetRef<T>() where T : unmanaged
            {
                return ref Unsafe.Add(ref Unsafe.As<long, T>(ref memory), offset);
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            public long GetValue<T>(T value, [CallerLineNumber] int line = 0) where T : unmanaged
            {
                long l = val;
                if (l is not (0L or -1L))
                {
                    Console.WriteLine($"Line {line}: found write out of bounds at offset {offset}");
                    _errors++;
                }
                Unsafe.Add(ref Unsafe.As<long, T>(ref l), offset) = value;
                return l;
            }

            public void Set(long value, [CallerLineNumber] int line = 0)
            {
                if (value != ~val)
                {
                    Console.WriteLine($"Line {line}: found corrupt check value at offset {offset}");
                    _errors++;
                }
                memory = val = value;
            }
        }

        private static int _errors;
        private static Box _box;

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

            _box = new();
            for (; _box.offset < sizeof(long) / sizeof(ushort); _box.offset++)
            {
                _box.Set(-1);
                Equals(255, Interlocked.Exchange(ref _box.GetRef<byte>(), 254));
                Equals(_box.GetValue<byte>(254), _box.Memory);
                Equals(254, ExchangeByte()(ref _box.GetRef<byte>(), 253));
                Equals(_box.GetValue<byte>(253), _box.Memory);

                _box.Set(0);
                Equals(0, Interlocked.Exchange(ref _box.GetRef<sbyte>(), -4));
                Equals(_box.GetValue<sbyte>(-4), _box.Memory);
                Equals(-4, ExchangeSByte()(ref _box.GetRef<sbyte>(), -5));
                Equals(_box.GetValue<sbyte>(-5), _box.Memory);

                _box.Set(-1);
                Equals(255, Interlocked.CompareExchange(ref _box.GetRef<byte>(), 254, 255));
                Equals(_box.GetValue<byte>(254), _box.Memory);
                Equals(254, CompareExchangeByte()(ref _box.GetRef<byte>(), 253, 254));
                Equals(_box.GetValue<byte>(253), _box.Memory);

                _box.Set(0);
                Equals(0, Interlocked.CompareExchange(ref _box.GetRef<sbyte>(), -4, 0));
                Equals(_box.GetValue<sbyte>(-4), _box.Memory);
                Equals(-4, CompareExchangeSByte()(ref _box.GetRef<sbyte>(), -5, -4));
                Equals(_box.GetValue<sbyte>(-5), _box.Memory);

                Equals(251, Interlocked.CompareExchange(ref _box.GetRef<byte>(), 2, 10));
                Equals(_box.GetValue<byte>(251), _box.Memory);
                Equals(251, CompareExchangeByte()(ref _box.GetRef<byte>(), 2, 10));
                Equals(_box.GetValue<byte>(251), _box.Memory);
                Equals(-5, Interlocked.CompareExchange(ref _box.GetRef<sbyte>(), 2, 10));
                Equals(_box.GetValue<sbyte>(-5), _box.Memory);
                Equals(-5, CompareExchangeSByte()(ref _box.GetRef<sbyte>(), 2, 10));
                Equals(_box.GetValue<sbyte>(-5), _box.Memory);

                _box.Set(-1);
                _box.Set(0);
                Equals(0, Interlocked.Exchange(ref _box.GetRef<short>(), -2));
                Equals(_box.GetValue<short>(-2), _box.Memory);
                Equals(-2, ExchangeShort()(ref _box.GetRef<short>(), -3));
                Equals(_box.GetValue<short>(-3), _box.Memory);

                _box.Set(-1);
                Equals(65535, Interlocked.Exchange(ref _box.GetRef<ushort>(), 65532));
                Equals(_box.GetValue<ushort>(65532), _box.Memory);
                Equals(65532, ExchangeUShort()(ref _box.GetRef<ushort>(), 65531));
                Equals(_box.GetValue<ushort>(65531), _box.Memory);

                _box.Set(0);
                Equals(0, Interlocked.CompareExchange(ref _box.GetRef<short>(), -2, 0));
                Equals(_box.GetValue<short>(-2), _box.Memory);
                Equals(-2, CompareExchangeShort()(ref _box.GetRef<short>(), -3, -2));
                Equals(_box.GetValue<short>(-3), _box.Memory);

                _box.Set(-1);
                Equals(65535, Interlocked.CompareExchange(ref _box.GetRef<ushort>(), 65532, 65535));
                Equals(_box.GetValue<ushort>(65532), _box.Memory);
                Equals(65532, CompareExchangeUShort()(ref _box.GetRef<ushort>(), 65531, 65532));
                Equals(_box.GetValue<ushort>(65531), _box.Memory);

                Equals(-5, Interlocked.CompareExchange(ref _box.GetRef<short>(), 1444, 1555));
                Equals(_box.GetValue<short>(-5), _box.Memory);
                Equals(-5, CompareExchangeShort()(ref _box.GetRef<short>(), 1444, 1555));
                Equals(_box.GetValue<short>(-5), _box.Memory);
                Equals(65531, Interlocked.CompareExchange(ref _box.GetRef<ushort>(), 1444, 1555));
                Equals(_box.GetValue<ushort>(65531), _box.Memory);
                Equals(65531, CompareExchangeUShort()(ref _box.GetRef<ushort>(), 1444, 1555));
                Equals(_box.GetValue<ushort>(65531), _box.Memory);

                _box.Set(0);
                _box.Set(-1);
                Interlocked.Exchange(ref _box.GetRef<byte>(), 123);
                Equals(_box.GetValue<byte>(123), _box.Memory);
                ExchangeByte()(ref _box.GetRef<byte>(), 124);
                Equals(_box.GetValue<byte>(124), _box.Memory);
                Interlocked.Exchange(ref _box.GetRef<sbyte>(), 125);
                Equals(_box.GetValue<sbyte>(125), _box.Memory);
                ExchangeSByte()(ref _box.GetRef<sbyte>(), 126);
                Equals(_box.GetValue<sbyte>(126), _box.Memory);

                Interlocked.CompareExchange(ref _box.GetRef<byte>(), 55, 126);
                Equals(_box.GetValue<byte>(55), _box.Memory);
                CompareExchangeByte()(ref _box.GetRef<byte>(), 56, 55);
                Equals(_box.GetValue<byte>(56), _box.Memory);
                Interlocked.CompareExchange(ref _box.GetRef<sbyte>(), 57, 56);
                Equals(_box.GetValue<sbyte>(57), _box.Memory);
                CompareExchangeSByte()(ref _box.GetRef<sbyte>(), 58, 57);
                Equals(_box.GetValue<sbyte>(58), _box.Memory);

                Interlocked.CompareExchange(ref _box.GetRef<byte>(), 10, 2);
                Equals(_box.GetValue<byte>(58), _box.Memory);
                CompareExchangeByte()(ref _box.GetRef<byte>(), 10, 2);
                Equals(_box.GetValue<byte>(58), _box.Memory);
                Interlocked.CompareExchange(ref _box.GetRef<sbyte>(), 10, 2);
                Equals(_box.GetValue<sbyte>(58), _box.Memory);
                CompareExchangeSByte()(ref _box.GetRef<sbyte>(), 10, 2);
                Equals(_box.GetValue<sbyte>(58), _box.Memory);

                _box.Set(0);
                _box.Set(-1);
                Interlocked.Exchange(ref _box.GetRef<short>(), 12345);
                Equals(_box.GetValue<short>(12345), _box.Memory);
                ExchangeShort()(ref _box.GetRef<short>(), 12346);
                Equals(_box.GetValue<short>(12346), _box.Memory);
                Interlocked.Exchange(ref _box.GetRef<ushort>(), 12347);
                Equals(_box.GetValue<ushort>(12347), _box.Memory);
                ExchangeUShort()(ref _box.GetRef<ushort>(), 12348);
                Equals(_box.GetValue<ushort>(12348), _box.Memory);

                Interlocked.CompareExchange(ref _box.GetRef<short>(), 1234, 12348);
                Equals(_box.GetValue<short>(1234), _box.Memory);
                CompareExchangeShort()(ref _box.GetRef<short>(), 1235, 1234);
                Equals(_box.GetValue<short>(1235), _box.Memory);
                Interlocked.CompareExchange(ref _box.GetRef<ushort>(), 1236, 1235);
                Equals(_box.GetValue<ushort>(1236), _box.Memory);
                CompareExchangeUShort()(ref _box.GetRef<ushort>(), 1237, 1236);
                Equals(_box.GetValue<ushort>(1237), _box.Memory);

                Interlocked.CompareExchange(ref _box.GetRef<short>(), 1555, 1444);
                Equals(_box.GetValue<short>(1237), _box.Memory);
                CompareExchangeShort()(ref _box.GetRef<short>(), 1555, 1444);
                Equals(_box.GetValue<short>(1237), _box.Memory);
                Interlocked.CompareExchange(ref _box.GetRef<ushort>(), 1555, 1444);
                Equals(_box.GetValue<ushort>(1237), _box.Memory);
                CompareExchangeUShort()(ref _box.GetRef<ushort>(), 1555, 1444);
                Equals(_box.GetValue<ushort>(1237), _box.Memory);
                _box.Set(0);
            }

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
        private static void Equals(long left, long right, [CallerLineNumber] int line = 0, [CallerFilePath] string file = "")
        {
            if (left == right)
                return;
            Console.WriteLine($"{file}:L{line} test failed (not equal, expected: {left}, actual: {right}) at offset {_box.offset}.");
            _errors++;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ThrowsNRE(Action action, [CallerLineNumber] int line = 0, [CallerFilePath] string file = "")
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
