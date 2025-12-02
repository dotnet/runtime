// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace System.Threading.Tests
{
    public unsafe class InterlockedTests
    {
        [Fact]
        public void InterlockedAdd_Int32()
        {
            int value = 42;
            Assert.Equal(12387, Interlocked.Add(ref value, 12345));
            Assert.Equal(12387, value);
            Assert.Equal(12387, Interlocked.Add(ref value, 0));
            Assert.Equal(12387, value);
            Assert.Equal(12386, Interlocked.Add(ref value, -1));
            Assert.Equal(12386, value);

            value = int.MaxValue;
            Assert.Equal(int.MinValue, Interlocked.Add(ref value, 1));
            Assert.Equal(int.MinValue, value);
        }

        [Fact]
        public void InterlockedAdd_UInt32()
        {
            uint value = 42;
            Assert.Equal(12387u, Interlocked.Add(ref value, 12345u));
            Assert.Equal(12387u, value);
            Assert.Equal(12387u, Interlocked.Add(ref value, 0u));
            Assert.Equal(12387u, value);
            Assert.Equal(9386u, Interlocked.Add(ref value, 4294964295u));
            Assert.Equal(9386u, value);

            value = uint.MaxValue;
            Assert.Equal(0u, Interlocked.Add(ref value, 1));
            Assert.Equal(0u, value);
        }

        [Fact]
        public void InterlockedAdd_Int64()
        {
            long value = 42;
            Assert.Equal(12387, Interlocked.Add(ref value, 12345));
            Assert.Equal(12387, value);
            Assert.Equal(12387, Interlocked.Add(ref value, 0));
            Assert.Equal(12387, value);
            Assert.Equal(12386, Interlocked.Add(ref value, -1));
            Assert.Equal(12386, value);

            value = long.MaxValue;
            Assert.Equal(long.MinValue, Interlocked.Add(ref value, 1));
            Assert.Equal(long.MinValue, value);
        }

        [Fact]
        public void InterlockedAdd_UInt64()
        {
            ulong value = 42;
            Assert.Equal(12387u, Interlocked.Add(ref value, 12345));
            Assert.Equal(12387u, value);
            Assert.Equal(12387u, Interlocked.Add(ref value, 0));
            Assert.Equal(12387u, value);
            Assert.Equal(10771u, Interlocked.Add(ref value, 18446744073709550000));
            Assert.Equal(10771u, value);

            value = ulong.MaxValue;
            Assert.Equal(0u, Interlocked.Add(ref value, 1));
            Assert.Equal(0u, value);
        }

        [Fact]
        public void InterlockedIncrement_Int32()
        {
            int value = 42;
            Assert.Equal(43, Interlocked.Increment(ref value));
            Assert.Equal(43, value);
        }

        [Fact]
        public void InterlockedIncrement_UInt32()
        {
            uint value = 42u;
            Assert.Equal(43u, Interlocked.Increment(ref value));
            Assert.Equal(43u, value);
        }

        [Fact]
        public void InterlockedIncrement_Int64()
        {
            long value = 42;
            Assert.Equal(43, Interlocked.Increment(ref value));
            Assert.Equal(43, value);
        }

        [Fact]
        public void InterlockedIncrement_UInt64()
        {
            ulong value = 42u;
            Assert.Equal(43u, Interlocked.Increment(ref value));
            Assert.Equal(43u, value);
        }

        [Fact]
        public void InterlockedDecrement_Int32()
        {
            int value = 42;
            Assert.Equal(41, Interlocked.Decrement(ref value));
            Assert.Equal(41, value);
        }

        [Fact]
        public void InterlockedDecrement_UInt32()
        {
            uint value = 42u;
            Assert.Equal(41u, Interlocked.Decrement(ref value));
            Assert.Equal(41u, value);
        }

        [Fact]
        public void InterlockedDecrement_Int64()
        {
            long value = 42;
            Assert.Equal(41, Interlocked.Decrement(ref value));
            Assert.Equal(41, value);
        }

        [Fact]
        public void InterlockedDecrement_UInt64()
        {
            ulong value = 42u;
            Assert.Equal(41u, Interlocked.Decrement(ref value));
            Assert.Equal(41u, value);
        }

        [Fact]
        public void InterlockedExchange_Int8()
        {
            using BoundedMemory<sbyte> memory = BoundedMemory.Allocate<sbyte>(1);
            ref sbyte value = ref memory.Span[0];

            value = 42;
            Assert.Equal(42, Interlocked.Exchange(ref value, 123));
            Assert.Equal(123, value);

            value = 42;
            Assert.Equal(42, Interlocked.Exchange<sbyte>(ref value, 123));
            Assert.Equal(123, value);

            Assert.Throws<NullReferenceException>(() => Interlocked.Exchange(ref Unsafe.NullRef<byte>(), 123));
        }

        [Fact]
        public void InterlockedExchange_UInt8()
        {
            using BoundedMemory<byte> memory = BoundedMemory.Allocate<byte>(1);
            ref byte value = ref memory.Span[0];

            value = 42;
            Assert.Equal(42u, Interlocked.Exchange(ref value, 123));
            Assert.Equal(123u, value);

            value = 42;
            Assert.Equal(42u, Interlocked.Exchange<byte>(ref value, 123));
            Assert.Equal(123u, value);

            Assert.Throws<NullReferenceException>(() => Interlocked.Exchange(ref Unsafe.NullRef<sbyte>(), 123));
        }

        [Fact]
        public void InterlockedExchange_Int16()
        {
            using BoundedMemory<short> memory = BoundedMemory.Allocate<short>(1);
            ref short value = ref memory.Span[0];

            value = 42;
            Assert.Equal(42, Interlocked.Exchange(ref value, 12345));
            Assert.Equal(12345, value);

            value = 42;
            Assert.Equal(42, Interlocked.Exchange<short>(ref value, 12345));
            Assert.Equal(12345, value);

            Assert.Throws<NullReferenceException>(() => Interlocked.Exchange(ref Unsafe.NullRef<short>(), 12345));
        }

        [Fact]
        public void InterlockedExchange_UInt16()
        {
            using BoundedMemory<ushort> memory = BoundedMemory.Allocate<ushort>(1);
            ref ushort value = ref memory.Span[0];

            value = 42;
            Assert.Equal(42u, Interlocked.Exchange(ref value, 12345));
            Assert.Equal(12345u, value);

            value = 42;
            Assert.Equal(42u, Interlocked.Exchange<ushort>(ref value, 12345));
            Assert.Equal(12345u, value);

            Assert.Throws<NullReferenceException>(() => Interlocked.Exchange(ref Unsafe.NullRef<ushort>(), 12345));
        }

        [Fact]
        public void InterlockedExchange_Int32()
        {
            using BoundedMemory<int> memory = BoundedMemory.Allocate<int>(1);
            ref int value = ref memory.Span[0];

            value = 42;
            Assert.Equal(42, Interlocked.Exchange(ref value, 12345));
            Assert.Equal(12345, value);

            value = 42;
            Assert.Equal(42, Interlocked.Exchange<int>(ref value, 12345));
            Assert.Equal(12345, value);

            Assert.Throws<NullReferenceException>(() => Interlocked.Exchange(ref Unsafe.NullRef<int>(), 12345));
        }

        [Fact]
        public void InterlockedExchange_UInt32()
        {
            using BoundedMemory<uint> memory = BoundedMemory.Allocate<uint>(1);
            ref uint value = ref memory.Span[0];

            value = 42;
            Assert.Equal(42u, Interlocked.Exchange(ref value, 12345u));
            Assert.Equal(12345u, value);

            value = 42;
            Assert.Equal(42u, Interlocked.Exchange<uint>(ref value, 12345u));
            Assert.Equal(12345u, value);

            Assert.Throws<NullReferenceException>(() => Interlocked.Exchange(ref Unsafe.NullRef<uint>(), 12345));
        }

        [Fact]
        public void InterlockedExchange_Int64()
        {
            using BoundedMemory<long> memory = BoundedMemory.Allocate<long>(1);
            ref long value = ref memory.Span[0];

            value = 42;
            Assert.Equal(42, Interlocked.Exchange(ref value, 12345));
            Assert.Equal(12345, value);

            value = 42;
            Assert.Equal(42, Interlocked.Exchange<long>(ref value, 12345));
            Assert.Equal(12345, value);

            Assert.Throws<NullReferenceException>(() => Interlocked.Exchange(ref Unsafe.NullRef<long>(), 12345));
        }

        [Fact]
        public void InterlockedExchange_IntPtr()
        {
            using BoundedMemory<nint> memory = BoundedMemory.Allocate<nint>(1);
            ref nint value = ref memory.Span[0];

            value = 42;
            Assert.Equal(42, (nint)Interlocked.Exchange(ref value, (nint)12345));
            Assert.Equal(12345, value);

            value = 42;
            Assert.Equal(42, (nint)Interlocked.Exchange<nint>(ref value, (nint)12345));
            Assert.Equal(12345, value);

            if (Environment.Is64BitProcess)
            {
                Assert.Equal(12345, (nint)Interlocked.Exchange(ref value, unchecked((nint)1 + int.MaxValue)));
                Assert.Equal(unchecked((nint)1 + int.MaxValue), value);

                Assert.Equal(unchecked((nint)1 + int.MaxValue), (nint)Interlocked.Exchange(ref value, unchecked((nint)2 + int.MaxValue)));
                Assert.Equal(unchecked((nint)2 + int.MaxValue), value);
            }
            Assert.Throws<NullReferenceException>(() => Interlocked.Exchange(ref Unsafe.NullRef<nint>(), 12345));
        }

        [Fact]
        public void InterlockedExchange_UInt64()
        {
            using BoundedMemory<ulong> memory = BoundedMemory.Allocate<ulong>(1);
            ref ulong value = ref memory.Span[0];

            value = 42;
            Assert.Equal(42u, Interlocked.Exchange(ref value, 12345u));
            Assert.Equal(12345u, value);

            value = 42;
            Assert.Equal(42u, Interlocked.Exchange<ulong>(ref value, 12345u));
            Assert.Equal(12345u, value);

            Assert.Throws<NullReferenceException>(() => Interlocked.Exchange(ref Unsafe.NullRef<ulong>(), 12345));
        }

        [Fact]
        public void InterlockedExchange_UIntPtr()
        {
            using BoundedMemory<nuint> memory = BoundedMemory.Allocate<nuint>(1);
            ref nuint value = ref memory.Span[0];

            value = 42;
            Assert.Equal(42u, (nuint)Interlocked.Exchange(ref value, (nuint)12345u));
            Assert.Equal(12345u, value);

            value = 42;
            Assert.Equal(42u, (nuint)Interlocked.Exchange<nuint>(ref value, (nuint)12345u));
            Assert.Equal(12345u, value);

            if (Environment.Is64BitProcess)
            {
                Assert.Equal(12345u, (nuint)Interlocked.Exchange(ref value, unchecked((nuint)1 + uint.MaxValue)));
                Assert.Equal(unchecked((nuint)1 + uint.MaxValue), value);

                Assert.Equal(unchecked((nuint)1 + uint.MaxValue), (nuint)Interlocked.Exchange(ref value, unchecked((nuint)2 + uint.MaxValue)));
                Assert.Equal(unchecked((nuint)2 + uint.MaxValue), value);
            }
            Assert.Throws<NullReferenceException>(() => Interlocked.Exchange(ref Unsafe.NullRef<nuint>(), 12345));
        }

        [Fact]
        public void InterlockedExchange_Float()
        {
            using BoundedMemory<float> memory = BoundedMemory.Allocate<float>(1);
            ref float value = ref memory.Span[0];

            value = 42.1f;
            Assert.Equal(42.1f, Interlocked.Exchange(ref value, 12345.1f));
            Assert.Equal(12345.1f, value);

            value = 42.1f;
            Assert.Equal(42.1f, Interlocked.Exchange<float>(ref value, 12345.1f));
            Assert.Equal(12345.1f, value);

            Assert.Throws<NullReferenceException>(() => Interlocked.Exchange(ref Unsafe.NullRef<float>(), 12345.1f));
        }

        [Fact]
        public void InterlockedExchange_Double()
        {
            using BoundedMemory<double> memory = BoundedMemory.Allocate<double>(1);
            ref double value = ref memory.Span[0];

            value = 42.1;
            Assert.Equal(42.1, Interlocked.Exchange(ref value, 12345.1));
            Assert.Equal(12345.1, value);

            value = 42.1;
            Assert.Equal(42.1, Interlocked.Exchange<double>(ref value, 12345.1));
            Assert.Equal(12345.1, value);

            Assert.Throws<NullReferenceException>(() => Interlocked.Exchange(ref Unsafe.NullRef<double>(), 12345.1));
        }

        [Fact]
        public void InterlockedExchange_Object()
        {
            var oldValue = new object();
            var newValue = new object();

            object value = oldValue;
            Assert.Same(oldValue, Interlocked.Exchange(ref value, newValue));
            Assert.Same(newValue, value);

            value = oldValue;
            Assert.Same(oldValue, Interlocked.Exchange<object>(ref value, newValue));
            Assert.Same(newValue, value);

            Assert.Throws<NullReferenceException>(() => Interlocked.Exchange(ref Unsafe.NullRef<object>(), null));
            Assert.Throws<NullReferenceException>(() => Interlocked.Exchange(ref Unsafe.NullRef<object>(), newValue));
        }

        [Fact]
        public void InterlockedExchange_BoxedObject()
        {
            var oldValue = (object)42;
            var newValue = (object)12345;
            object value = oldValue;

            object valueBeforeUpdate = Interlocked.Exchange(ref value, newValue);
            Assert.Same(oldValue, valueBeforeUpdate);
            Assert.Equal(42, (int)valueBeforeUpdate);
            Assert.Same(newValue, value);
            Assert.Equal(12345, (int)value);
        }

        [Fact]
        public void InterlockedExchange_Unsupported()
        {
            DateTime value1 = default;
            TimeSpan value2 = default;
            Rune value3 = default;
            ValueTask value4 = default;

            Assert.Throws<NotSupportedException>(() => Interlocked.Exchange(ref value1, default));
            Assert.Throws<NotSupportedException>(() => Interlocked.Exchange(ref value2, default));
            Assert.Throws<NotSupportedException>(() => Interlocked.Exchange(ref value3, default));
            Assert.Throws<NotSupportedException>(() => Interlocked.Exchange(ref value4, default));
        }

        [Fact]
        public void InterlockedCompareExchange_Int8()
        {
            using BoundedMemory<sbyte> memory = BoundedMemory.Allocate<sbyte>(1);
            ref sbyte value = ref memory.Span[0];
            value = 42;

            Assert.Equal(42, Interlocked.CompareExchange(ref value, 123, 41));
            Assert.Equal(42, value);

            Assert.Equal(42, Interlocked.CompareExchange(ref value, 123, 42));
            Assert.Equal(123, value);

            value = 42;

            Assert.Equal(42, Interlocked.CompareExchange<sbyte>(ref value, 123, 41));
            Assert.Equal(42, value);

            Assert.Equal(42, Interlocked.CompareExchange<sbyte>(ref value, 123, 42));
            Assert.Equal(123, value);

            Assert.Throws<NullReferenceException>(() => Interlocked.CompareExchange(ref Unsafe.NullRef<sbyte>(), 123, 41));
            Assert.Throws<NullReferenceException>(() => Interlocked.CompareExchange<sbyte>(ref Unsafe.NullRef<sbyte>(), 123, 41));
        }

        [Fact]
        public void InterlockedCompareExchange_UInt8()
        {
            using BoundedMemory<byte> memory = BoundedMemory.Allocate<byte>(1);
            ref byte value = ref memory.Span[0];
            value = 42;

            Assert.Equal(42u, Interlocked.CompareExchange(ref value, 123, 41));
            Assert.Equal(42u, value);

            Assert.Equal(42u, Interlocked.CompareExchange(ref value, 123, 42));
            Assert.Equal(123u, value);

            value = 42;

            Assert.Equal(42u, Interlocked.CompareExchange<byte>(ref value, 123, 41));
            Assert.Equal(42u, value);

            Assert.Equal(42u, Interlocked.CompareExchange<byte>(ref value, 123, 42));
            Assert.Equal(123u, value);

            Assert.Throws<NullReferenceException>(() => Interlocked.CompareExchange(ref Unsafe.NullRef<byte>(), 123, 41));
            Assert.Throws<NullReferenceException>(() => Interlocked.CompareExchange<byte>(ref Unsafe.NullRef<byte>(), 123, 41));
        }

        [Fact]
        public void InterlockedCompareExchange_Bool()
        {
            using BoundedMemory<bool> memory = BoundedMemory.Allocate<bool>(1);
            ref bool value = ref memory.Span[0];
            value = false;

            Assert.False(Interlocked.CompareExchange<bool>(ref value, true, false));
            Assert.True(value);

            Assert.True(Interlocked.CompareExchange<bool>(ref value, false, false));
            Assert.True(value);

            Assert.Throws<NullReferenceException>(() => Interlocked.CompareExchange<bool>(ref Unsafe.NullRef<bool>(), false, false));
        }

        [Fact]
        public void InterlockedCompareExchange_Int16()
        {
            using BoundedMemory<short> memory = BoundedMemory.Allocate<short>(1);
            ref short value = ref memory.Span[0];
            value = 42;

            Assert.Equal(42, Interlocked.CompareExchange(ref value, 12345, 41));
            Assert.Equal(42, value);

            Assert.Equal(42, Interlocked.CompareExchange(ref value, 12345, 42));
            Assert.Equal(12345, value);

            value = 42;

            Assert.Equal(42, Interlocked.CompareExchange<short>(ref value, 12345, 41));
            Assert.Equal(42, value);

            Assert.Equal(42, Interlocked.CompareExchange<short>(ref value, 12345, 42));
            Assert.Equal(12345, value);

            Assert.Throws<NullReferenceException>(() => Interlocked.CompareExchange(ref Unsafe.NullRef<short>(), 12345, 41));
            Assert.Throws<NullReferenceException>(() => Interlocked.CompareExchange<short>(ref Unsafe.NullRef<short>(), 12345, 41));
        }

        [Fact]
        public void InterlockedCompareExchange_UInt16()
        {
            using BoundedMemory<ushort> memory = BoundedMemory.Allocate<ushort>(1);
            ref ushort value = ref memory.Span[0];
            value = 42;

            Assert.Equal(42u, Interlocked.CompareExchange(ref value, 12345, 41));
            Assert.Equal(42u, value);

            Assert.Equal(42u, Interlocked.CompareExchange(ref value, 12345, 42));
            Assert.Equal(12345u, value);

            value = 42;

            Assert.Equal(42u, Interlocked.CompareExchange<ushort>(ref value, 12345, 41));
            Assert.Equal(42u, value);

            Assert.Equal(42u, Interlocked.CompareExchange<ushort>(ref value, 12345, 42));
            Assert.Equal(12345u, value);

            Assert.Throws<NullReferenceException>(() => Interlocked.CompareExchange(ref Unsafe.NullRef<ushort>(), 12345, 41));
            Assert.Throws<NullReferenceException>(() => Interlocked.CompareExchange<ushort>(ref Unsafe.NullRef<ushort>(), 12345, 41));
        }

        [Fact]
        public void InterlockedCompareExchange_Char()
        {
            using BoundedMemory<char> memory = BoundedMemory.Allocate<char>(1);
            ref char value = ref memory.Span[0];
            value = (char)42;

            Assert.Equal(42u, Interlocked.CompareExchange<char>(ref value, (char)12345, (char)41));
            Assert.Equal(42u, value);

            Assert.Equal(42u, Interlocked.CompareExchange<char>(ref value, (char)12345, (char)42));
            Assert.Equal(12345u, value);

            Assert.Throws<NullReferenceException>(() => Interlocked.CompareExchange<char>(ref Unsafe.NullRef<char>(), (char)12345, (char)41));
        }

        [Fact]
        public void InterlockedCompareExchange_Int32()
        {
            using BoundedMemory<int> memory = BoundedMemory.Allocate<int>(1);
            ref int value = ref memory.Span[0];
            value = 42;

            Assert.Equal(42, Interlocked.CompareExchange(ref value, 12345, 41));
            Assert.Equal(42, value);

            Assert.Equal(42, Interlocked.CompareExchange(ref value, 12345, 42));
            Assert.Equal(12345, value);

            value = 42;

            Assert.Equal(42, Interlocked.CompareExchange<int>(ref value, 12345, 41));
            Assert.Equal(42, value);

            Assert.Equal(42, Interlocked.CompareExchange<int>(ref value, 12345, 42));
            Assert.Equal(12345, value);

            Assert.Throws<NullReferenceException>(() => Interlocked.CompareExchange(ref Unsafe.NullRef<int>(), 12345, 41));
            Assert.Throws<NullReferenceException>(() => Interlocked.CompareExchange<int>(ref Unsafe.NullRef<int>(), 12345, 41));
        }

        [Fact]
        public void InterlockedCompareExchange_UInt32()
        {
            using BoundedMemory<uint> memory = BoundedMemory.Allocate<uint>(1);
            ref uint value = ref memory.Span[0];
            value = 42;

            Assert.Equal(42u, Interlocked.CompareExchange(ref value, 12345u, 41u));
            Assert.Equal(42u, value);

            Assert.Equal(42u, Interlocked.CompareExchange(ref value, 12345u, 42u));
            Assert.Equal(12345u, value);

            value = 42;

            Assert.Equal(42u, Interlocked.CompareExchange<uint>(ref value, 12345u, 41u));
            Assert.Equal(42u, value);

            Assert.Equal(42u, Interlocked.CompareExchange<uint>(ref value, 12345u, 42u));
            Assert.Equal(12345u, value);

            Assert.Throws<NullReferenceException>(() => Interlocked.CompareExchange(ref Unsafe.NullRef<uint>(), 12345, 41));
            Assert.Throws<NullReferenceException>(() => Interlocked.CompareExchange<uint>(ref Unsafe.NullRef<uint>(), 12345, 41));
        }

        [Fact]
        public void InterlockedCompareExchange_Enum()
        {
            using BoundedMemory<DayOfWeek> memory = BoundedMemory.Allocate<DayOfWeek>(1);
            ref DayOfWeek value = ref memory.Span[0];
            value = DayOfWeek.Monday;

            Assert.Equal(DayOfWeek.Monday, Interlocked.CompareExchange<DayOfWeek>(ref value, DayOfWeek.Tuesday, DayOfWeek.Monday));
            Assert.Equal(DayOfWeek.Tuesday, value);

            Assert.Equal(DayOfWeek.Tuesday, Interlocked.CompareExchange<DayOfWeek>(ref value, DayOfWeek.Wednesday, DayOfWeek.Monday));
            Assert.Equal(DayOfWeek.Tuesday, value);

            Assert.Throws<NullReferenceException>(() => Interlocked.CompareExchange<DayOfWeek>(ref Unsafe.NullRef<DayOfWeek>(), DayOfWeek.Monday, DayOfWeek.Tuesday));
        }

        [Fact]
        public void InterlockedCompareExchange_Int64()
        {
            using BoundedMemory<long> memory = BoundedMemory.Allocate<long>(1);
            ref long value = ref memory.Span[0];
            value = 42;

            Assert.Equal(42, Interlocked.CompareExchange(ref value, 12345, 41));
            Assert.Equal(42, value);

            Assert.Equal(42, Interlocked.CompareExchange(ref value, 12345, 42));
            Assert.Equal(12345, value);

            value = 42;

            Assert.Equal(42, Interlocked.CompareExchange<long>(ref value, 12345, 41));
            Assert.Equal(42, value);

            Assert.Equal(42, Interlocked.CompareExchange<long>(ref value, 12345, 42));
            Assert.Equal(12345, value);

            Assert.Throws<NullReferenceException>(() => Interlocked.CompareExchange(ref Unsafe.NullRef<long>(), 12345, 41));
            Assert.Throws<NullReferenceException>(() => Interlocked.CompareExchange<long>(ref Unsafe.NullRef<long>(), 12345, 41));
        }

        [Fact]
        public void InterlockedCompareExchange_IntPtr()
        {
            using BoundedMemory<nint> memory = BoundedMemory.Allocate<nint>(1);
            ref nint value = ref memory.Span[0];
            value = 42;

            Assert.Equal(42, Interlocked.CompareExchange(ref value, (nint)12345, (nint)41));
            Assert.Equal(42, value);

            Assert.Equal(42, Interlocked.CompareExchange(ref value, (nint)12345, (nint)42));
            Assert.Equal(12345, value);

            value = 42;

            Assert.Equal(42, Interlocked.CompareExchange<nint>(ref value, (nint)12345, (nint)41));
            Assert.Equal(42, value);

            Assert.Equal(42, Interlocked.CompareExchange<nint>(ref value, (nint)12345, (nint)42));
            Assert.Equal(12345, value);

            if (Environment.Is64BitProcess)
            {
                Assert.Equal(12345, Interlocked.CompareExchange(ref value, unchecked((nint)1 + int.MaxValue), (nint)12345));
                Assert.Equal(unchecked((nint)1 + int.MaxValue), value);

                Assert.Equal(unchecked((nint)1 + int.MaxValue), Interlocked.CompareExchange<nint>(ref value, unchecked((nint)2 + int.MaxValue), unchecked((nint)1 + int.MaxValue)));
                Assert.Equal(unchecked((nint)2 + int.MaxValue), value);
            }

            Assert.Throws<NullReferenceException>(() => Interlocked.CompareExchange(ref Unsafe.NullRef<nint>(), 12345, 41));
            Assert.Throws<NullReferenceException>(() => Interlocked.CompareExchange<nint>(ref Unsafe.NullRef<nint>(), 12345, 41));
        }

        [Fact]
        public void InterlockedCompareExchange_UInt64()
        {
            using BoundedMemory<ulong> memory = BoundedMemory.Allocate<ulong>(1);
            ref ulong value = ref memory.Span[0];
            value = 42;

            Assert.Equal(42u, Interlocked.CompareExchange(ref value, 12345u, 41u));
            Assert.Equal(42u, value);

            Assert.Equal(42u, Interlocked.CompareExchange(ref value, 12345u, 42u));
            Assert.Equal(12345u, value);

            value = 42;

            Assert.Equal(42u, Interlocked.CompareExchange<ulong>(ref value, 12345u, 41u));
            Assert.Equal(42u, value);

            Assert.Equal(42u, Interlocked.CompareExchange<ulong>(ref value, 12345u, 42u));
            Assert.Equal(12345u, value);

            Assert.Throws<NullReferenceException>(() => Interlocked.CompareExchange(ref Unsafe.NullRef<ulong>(), 12345, 41));
            Assert.Throws<NullReferenceException>(() => Interlocked.CompareExchange<ulong>(ref Unsafe.NullRef<ulong>(), 12345, 41));
        }

        [Fact]
        public void InterlockedCompareExchange_UIntPtr()
        {
            using BoundedMemory<nuint> memory = BoundedMemory.Allocate<nuint>(1);
            ref nuint value = ref memory.Span[0];
            value = 42;

            Assert.Equal(42u, Interlocked.CompareExchange(ref value, (nuint)12345u, (nuint)41u));
            Assert.Equal(42u, value);

            Assert.Equal(42u, Interlocked.CompareExchange(ref value, (nuint)12345u, (nuint)42u));
            Assert.Equal(12345u, value);

            value = 42;

            Assert.Equal(42u, Interlocked.CompareExchange<nuint>(ref value, (nuint)12345u, (nuint)41u));
            Assert.Equal(42u, value);

            Assert.Equal(42u, Interlocked.CompareExchange<nuint>(ref value, (nuint)12345u, (nuint)42u));
            Assert.Equal(12345u, value);

            if (Environment.Is64BitProcess)
            {
                Assert.Equal(12345u, (nuint)Interlocked.CompareExchange(ref value, unchecked((nuint)1 + uint.MaxValue), (nuint)12345u));
                Assert.Equal(unchecked((nuint)1 + uint.MaxValue), value);

                Assert.Equal(unchecked((nuint)1 + uint.MaxValue), Interlocked.CompareExchange<nuint>(ref value, unchecked((nuint)2 + uint.MaxValue), unchecked((nuint)1 + uint.MaxValue)));
                Assert.Equal(unchecked((nuint)2 + uint.MaxValue), value);
            }

            Assert.Throws<NullReferenceException>(() => Interlocked.CompareExchange(ref Unsafe.NullRef<nuint>(), 12345, 41));
            Assert.Throws<NullReferenceException>(() => Interlocked.CompareExchange<nuint>(ref Unsafe.NullRef<nuint>(), 12345, 41));
        }

        [Fact]
        public void InterlockedCompareExchange_Float()
        {
            using BoundedMemory<float> memory = BoundedMemory.Allocate<float>(1);
            ref float value = ref memory.Span[0];
            value = 42.1f;

            Assert.Equal(42.1f, Interlocked.CompareExchange(ref value, 12345.1f, 41.1f));
            Assert.Equal(42.1f, value);

            Assert.Equal(42.1f, Interlocked.CompareExchange(ref value, 12345.1f, 42.1f));
            Assert.Equal(12345.1f, value);

            value = 42.1f;

            Assert.Equal(42.1f, Interlocked.CompareExchange<float>(ref value, 12345.1f, 41.1f));
            Assert.Equal(42.1f, value);

            Assert.Equal(42.1f, Interlocked.CompareExchange<float>(ref value, 12345.1f, 42.1f));
            Assert.Equal(12345.1f, value);

            Assert.Throws<NullReferenceException>(() => Interlocked.CompareExchange(ref Unsafe.NullRef<float>(), 12345.1f, 41.1f));
            Assert.Throws<NullReferenceException>(() => Interlocked.CompareExchange<float>(ref Unsafe.NullRef<float>(), 12345.1f, 41.1f));
        }

        [Fact]
        public void InterlockedCompareExchange_Double()
        {
            using BoundedMemory<double> memory = BoundedMemory.Allocate<double>(1);
            ref double value = ref memory.Span[0];
            value = 42.1;

            Assert.Equal(42.1, Interlocked.CompareExchange(ref value, 12345.1, 41.1));
            Assert.Equal(42.1, value);

            Assert.Equal(42.1, Interlocked.CompareExchange(ref value, 12345.1, 42.1));
            Assert.Equal(12345.1, value);

            value = 42.1;

            Assert.Equal(42.1, Interlocked.CompareExchange<double>(ref value, 12345.1, 41.1));
            Assert.Equal(42.1, value);

            Assert.Equal(42.1, Interlocked.CompareExchange<double>(ref value, 12345.1, 42.1));
            Assert.Equal(12345.1, value);

            Assert.Throws<NullReferenceException>(() => Interlocked.CompareExchange(ref Unsafe.NullRef<double>(), 12345.1, 41.1));
            Assert.Throws<NullReferenceException>(() => Interlocked.CompareExchange<double>(ref Unsafe.NullRef<double>(), 12345.1, 41.1));
        }

        [Fact]
        public void InterlockedCompareExchange_Object()
        {
            var oldValue = new object();
            var newValue = new object();
            object value = oldValue;

            Assert.Same(oldValue, Interlocked.CompareExchange(ref value, newValue, new object()));
            Assert.Same(oldValue, value);

            Assert.Same(oldValue, Interlocked.CompareExchange(ref value, newValue, oldValue));
            Assert.Same(newValue, value);

            value = oldValue;

            Assert.Same(oldValue, Interlocked.CompareExchange<object>(ref value, newValue, new object()));
            Assert.Same(oldValue, value);

            Assert.Same(oldValue, Interlocked.CompareExchange<object>(ref value, newValue, oldValue));
            Assert.Same(newValue, value);

            Assert.Throws<NullReferenceException>(() => Interlocked.CompareExchange(ref Unsafe.NullRef<object>(), null, null));
            Assert.Throws<NullReferenceException>(() => Interlocked.CompareExchange(ref Unsafe.NullRef<object>(), newValue, oldValue));
            Assert.Throws<NullReferenceException>(() => Interlocked.CompareExchange<object>(ref Unsafe.NullRef<object>(), null, null));
            Assert.Throws<NullReferenceException>(() => Interlocked.CompareExchange<object>(ref Unsafe.NullRef<object>(), newValue, oldValue));
        }

        [Fact]
        public void InterlockedCompareExchange_BoxedObject()
        {
            var oldValue = (object)42;
            var newValue = (object)12345;
            object value = oldValue;

            object valueBeforeUpdate = Interlocked.CompareExchange(ref value, newValue, (object)42);
            Assert.Same(oldValue, valueBeforeUpdate);
            Assert.Equal(42, (int)valueBeforeUpdate);
            Assert.Same(oldValue, value);
            Assert.Equal(42, (int)value);

            valueBeforeUpdate = Interlocked.CompareExchange(ref value, newValue, oldValue);
            Assert.Same(oldValue, valueBeforeUpdate);
            Assert.Equal(42, (int)valueBeforeUpdate);
            Assert.Same(newValue, value);
            Assert.Equal(12345, (int)value);
        }

        [Fact]
        public void InterlockedCompareExchange_Unsupported()
        {
            DateTime value1 = default;
            TimeSpan value2 = default;
            Rune value3 = default;
            ValueTask value4 = default;

            Assert.Throws<NotSupportedException>(() => Interlocked.CompareExchange(ref value1, default, default));
            Assert.Throws<NotSupportedException>(() => Interlocked.CompareExchange(ref value2, default, default));
            Assert.Throws<NotSupportedException>(() => Interlocked.CompareExchange(ref value3, default, default));
            Assert.Throws<NotSupportedException>(() => Interlocked.CompareExchange(ref value4, default, default));
        }

        [Fact]
        public void InterlockedRead_Int64()
        {
            long value = long.MaxValue - 42;
            Assert.Equal(long.MaxValue - 42, Interlocked.Read(ref value));
        }

        [Fact]
        public void InterlockedRead_UInt64()
        {
            ulong value = ulong.MaxValue - 42;
            Assert.Equal(ulong.MaxValue - 42, Interlocked.Read(ref value));
        }

        [Fact]
        public void InterlockedAnd_Int32()
        {
            int value = 0x12345670;
            Assert.Equal(0x12345670, Interlocked.And(ref value, 0x7654321));
            Assert.Equal(0x02244220, value);
        }

        [Fact]
        public void InterlockedAnd_UInt32()
        {
            uint value = 0x12345670u;
            Assert.Equal(0x12345670u, Interlocked.And(ref value, 0x7654321));
            Assert.Equal(0x02244220u, value);
        }

        [Fact]
        public void InterlockedAnd_Int64()
        {
            long value = 0x12345670;
            Assert.Equal(0x12345670, Interlocked.And(ref value, 0x7654321));
            Assert.Equal(0x02244220, value);
        }

        [Fact]
        public void InterlockedAnd_UInt64()
        {
            ulong value = 0x12345670u;
            Assert.Equal(0x12345670u, Interlocked.And(ref value, 0x7654321));
            Assert.Equal(0x02244220u, value);
        }

        [Fact]
        public void InterlockedOr_Int32()
        {
            int value = 0x12345670;
            Assert.Equal(0x12345670, Interlocked.Or(ref value, 0x7654321));
            Assert.Equal(0x17755771, value);
        }

        [Fact]
        public void InterlockedOr_UInt32()
        {
            uint value = 0x12345670u;
            Assert.Equal(0x12345670u, Interlocked.Or(ref value, 0x7654321));
            Assert.Equal(0x17755771u, value);
        }

        [Fact]
        public void InterlockedOr_Int64()
        {
            long value = 0x12345670;
            Assert.Equal(0x12345670, Interlocked.Or(ref value, 0x7654321));
            Assert.Equal(0x17755771, value);
        }

        [Fact]
        public void InterlockedOr_UInt64()
        {
            ulong value = 0x12345670u;
            Assert.Equal(0x12345670u, Interlocked.Or(ref value, 0x7654321));
            Assert.Equal(0x17755771u, value);
        }

        [Theory]
        [InlineData((byte)0xF0, (byte)0x3C, (byte)0x30)]
        [InlineData((byte)0xFF, (byte)0x00, (byte)0x00)]
        [InlineData((byte)0xAA, (byte)0x55, (byte)0x00)]
        [InlineData((byte)0xFF, (byte)0xFF, (byte)0xFF)]
        [InlineData((byte)0x00, (byte)0xFF, (byte)0x00)]
        public void InterlockedAnd_Generic_Byte(byte initial, byte operand, byte expected)
        {
            byte value = initial;
            Assert.Equal(initial, Interlocked.And<byte>(ref value, operand));
            Assert.Equal(expected, value);
        }

        [Fact]
        public void InterlockedAnd_Generic_Byte_NullRef()
        {
            Assert.Throws<NullReferenceException>(() => Interlocked.And<byte>(ref Unsafe.NullRef<byte>(), (byte)0x3C));
        }

        [Theory]
        [InlineData((sbyte)0x70, (sbyte)0x21, (sbyte)0x20)]
        [InlineData((sbyte)-1, (sbyte)0x7F, (sbyte)0x7F)]
        [InlineData((sbyte)-128, (sbyte)127, (sbyte)0)]
        [InlineData((sbyte)-1, (sbyte)-1, (sbyte)-1)]
        [InlineData((sbyte)0, (sbyte)-1, (sbyte)0)]
        public void InterlockedAnd_Generic_SByte(sbyte initial, sbyte operand, sbyte expected)
        {
            sbyte value = initial;
            Assert.Equal(initial, Interlocked.And<sbyte>(ref value, operand));
            Assert.Equal(expected, value);
        }

        [Fact]
        public void InterlockedAnd_Generic_SByte_NullRef()
        {
            Assert.Throws<NullReferenceException>(() => Interlocked.And<sbyte>(ref Unsafe.NullRef<sbyte>(), (sbyte)0x21));
        }

        [Theory]
        [InlineData((ushort)0x1234, (ushort)0x5678, (ushort)0x1230)]
        [InlineData((ushort)0xFFFF, (ushort)0x0000, (ushort)0x0000)]
        [InlineData((ushort)0xAAAA, (ushort)0x5555, (ushort)0x0000)]
        [InlineData((ushort)0xFFFF, (ushort)0xFFFF, (ushort)0xFFFF)]
        [InlineData((ushort)0x0000, (ushort)0xFFFF, (ushort)0x0000)]
        public void InterlockedAnd_Generic_UInt16(ushort initial, ushort operand, ushort expected)
        {
            ushort value = initial;
            Assert.Equal(initial, Interlocked.And<ushort>(ref value, operand));
            Assert.Equal(expected, value);
        }

        [Fact]
        public void InterlockedAnd_Generic_UInt16_NullRef()
        {
            Assert.Throws<NullReferenceException>(() => Interlocked.And<ushort>(ref Unsafe.NullRef<ushort>(), (ushort)0x5678));
        }

        [Theory]
        [InlineData((short)0x1234, (short)0x5678, (short)0x1230)]
        [InlineData((short)-1, (short)0x7FFF, (short)0x7FFF)]
        [InlineData((short)-32768, (short)32767, (short)0)]
        [InlineData((short)-1, (short)-1, (short)-1)]
        [InlineData((short)0, (short)-1, (short)0)]
        public void InterlockedAnd_Generic_Int16(short initial, short operand, short expected)
        {
            short value = initial;
            Assert.Equal(initial, Interlocked.And<short>(ref value, operand));
            Assert.Equal(expected, value);
        }

        [Fact]
        public void InterlockedAnd_Generic_Int16_NullRef()
        {
            Assert.Throws<NullReferenceException>(() => Interlocked.And<short>(ref Unsafe.NullRef<short>(), (short)0x5678));
        }

        [Theory]
        [InlineData(0x12345670, 0x07654321, 0x02244220)]
        [InlineData(-1, 0x7FFFFFFF, 0x7FFFFFFF)]
        [InlineData(int.MinValue, int.MaxValue, 0)]
        [InlineData(-1, -1, -1)]
        [InlineData(0, -1, 0)]
        public void InterlockedAnd_Generic_Int32(int initial, int operand, int expected)
        {
            int value = initial;
            Assert.Equal(initial, Interlocked.And<int>(ref value, operand));
            Assert.Equal(expected, value);
        }

        [Fact]
        public void InterlockedAnd_Generic_Int32_NullRef()
        {
            Assert.Throws<NullReferenceException>(() => Interlocked.And<int>(ref Unsafe.NullRef<int>(), 0x7654321));
        }

        [Theory]
        [InlineData(0x12345670u, 0x07654321u, 0x02244220u)]
        [InlineData(0xFFFFFFFFu, 0x00000000u, 0x00000000u)]
        [InlineData(0xAAAAAAAAu, 0x55555555u, 0x00000000u)]
        [InlineData(0xFFFFFFFFu, 0xFFFFFFFFu, 0xFFFFFFFFu)]
        [InlineData(0x00000000u, 0xFFFFFFFFu, 0x00000000u)]
        public void InterlockedAnd_Generic_UInt32(uint initial, uint operand, uint expected)
        {
            uint value = initial;
            Assert.Equal(initial, Interlocked.And<uint>(ref value, operand));
            Assert.Equal(expected, value);
        }

        [Fact]
        public void InterlockedAnd_Generic_UInt32_NullRef()
        {
            Assert.Throws<NullReferenceException>(() => Interlocked.And<uint>(ref Unsafe.NullRef<uint>(), 0x7654321u));
        }

        [Theory]
        [InlineData(0x12345670L, 0x07654321L, 0x02244220L)]
        [InlineData(-1L, 0x7FFFFFFFFFFFFFFFL, 0x7FFFFFFFFFFFFFFFL)]
        [InlineData(long.MinValue, long.MaxValue, 0L)]
        [InlineData(-1L, -1L, -1L)]
        [InlineData(0L, -1L, 0L)]
        public void InterlockedAnd_Generic_Int64(long initial, long operand, long expected)
        {
            long value = initial;
            Assert.Equal(initial, Interlocked.And<long>(ref value, operand));
            Assert.Equal(expected, value);
        }

        [Fact]
        public void InterlockedAnd_Generic_Int64_NullRef()
        {
            Assert.Throws<NullReferenceException>(() => Interlocked.And<long>(ref Unsafe.NullRef<long>(), 0x7654321L));
        }

        [Theory]
        [InlineData(0x12345670UL, 0x07654321UL, 0x02244220UL)]
        [InlineData(0xFFFFFFFFFFFFFFFFUL, 0x0000000000000000UL, 0x0000000000000000UL)]
        [InlineData(0xAAAAAAAAAAAAAAAAUL, 0x5555555555555555UL, 0x0000000000000000UL)]
        [InlineData(0xFFFFFFFFFFFFFFFFUL, 0xFFFFFFFFFFFFFFFFUL, 0xFFFFFFFFFFFFFFFFUL)]
        [InlineData(0x0000000000000000UL, 0xFFFFFFFFFFFFFFFFUL, 0x0000000000000000UL)]
        public void InterlockedAnd_Generic_UInt64(ulong initial, ulong operand, ulong expected)
        {
            ulong value = initial;
            Assert.Equal(initial, Interlocked.And<ulong>(ref value, operand));
            Assert.Equal(expected, value);
        }

        [Fact]
        public void InterlockedAnd_Generic_UInt64_NullRef()
        {
            Assert.Throws<NullReferenceException>(() => Interlocked.And<ulong>(ref Unsafe.NullRef<ulong>(), 0x7654321UL));
        }

        [Theory]
        [InlineData(DayOfWeek.Sunday | DayOfWeek.Monday | DayOfWeek.Tuesday, DayOfWeek.Sunday | DayOfWeek.Monday, DayOfWeek.Sunday | DayOfWeek.Monday)]
        [InlineData((DayOfWeek)0x7, (DayOfWeek)0x3, (DayOfWeek)0x3)]
        [InlineData((DayOfWeek)0xFF, (DayOfWeek)0x00, (DayOfWeek)0x00)]
        [InlineData((DayOfWeek)0xFF, (DayOfWeek)0xFF, (DayOfWeek)0xFF)]
        public void InterlockedAnd_Generic_Enum(DayOfWeek initial, DayOfWeek operand, DayOfWeek expected)
        {
            DayOfWeek value = initial;
            Assert.Equal(initial, Interlocked.And<DayOfWeek>(ref value, operand));
            Assert.Equal(expected, value);
        }

        [Fact]
        public void InterlockedAnd_Generic_Enum_NullRef()
        {
            Assert.Throws<NullReferenceException>(() => Interlocked.And<DayOfWeek>(ref Unsafe.NullRef<DayOfWeek>(), DayOfWeek.Monday));
        }

        [Fact]
        public void InterlockedAnd_Generic_Float_ThrowsNotSupported()
        {
            float value = 1.0f;
            Assert.Throws<NotSupportedException>(() => Interlocked.And<float>(ref value, 1.0f));
        }

        [Fact]
        public void InterlockedAnd_Generic_Double_ThrowsNotSupported()
        {
            double value = 1.0;
            Assert.Throws<NotSupportedException>(() => Interlocked.And<double>(ref value, 1.0));
        }

        [Fact]
        public void InterlockedAnd_Generic_Half_ThrowsNotSupported()
        {
            Half value = (Half)1.0;
            Assert.Throws<NotSupportedException>(() => Interlocked.And<Half>(ref value, (Half)1.0));
        }

        [Fact]
        public void InterlockedAnd_Generic_Unsupported()
        {
            DateTime value = default;
            Assert.Throws<NotSupportedException>(() => Interlocked.And<DateTime>(ref value, default));
        }

        [Theory]
        [InlineData((byte)0xF0, (byte)0x0C, (byte)0xFC)]
        [InlineData((byte)0x00, (byte)0xFF, (byte)0xFF)]
        [InlineData((byte)0xAA, (byte)0x55, (byte)0xFF)]
        [InlineData((byte)0xFF, (byte)0x00, (byte)0xFF)]
        [InlineData((byte)0x00, (byte)0x00, (byte)0x00)]
        public void InterlockedOr_Generic_Byte(byte initial, byte operand, byte expected)
        {
            byte value = initial;
            Assert.Equal(initial, Interlocked.Or<byte>(ref value, operand));
            Assert.Equal(expected, value);
        }

        [Fact]
        public void InterlockedOr_Generic_Byte_NullRef()
        {
            Assert.Throws<NullReferenceException>(() => Interlocked.Or<byte>(ref Unsafe.NullRef<byte>(), (byte)0x0C));
        }

        [Theory]
        [InlineData((sbyte)0x50, (sbyte)0x21, (sbyte)0x71)]
        [InlineData((sbyte)-128, (sbyte)127, (sbyte)-1)]
        [InlineData((sbyte)0, (sbyte)-1, (sbyte)-1)]
        [InlineData((sbyte)-1, (sbyte)0, (sbyte)-1)]
        [InlineData((sbyte)0, (sbyte)0, (sbyte)0)]
        public void InterlockedOr_Generic_SByte(sbyte initial, sbyte operand, sbyte expected)
        {
            sbyte value = initial;
            Assert.Equal(initial, Interlocked.Or<sbyte>(ref value, operand));
            Assert.Equal(expected, value);
        }

        [Fact]
        public void InterlockedOr_Generic_SByte_NullRef()
        {
            Assert.Throws<NullReferenceException>(() => Interlocked.Or<sbyte>(ref Unsafe.NullRef<sbyte>(), (sbyte)0x21));
        }

        [Theory]
        [InlineData((ushort)0x1234, (ushort)0x5008, (ushort)0x523C)]
        [InlineData((ushort)0x0000, (ushort)0xFFFF, (ushort)0xFFFF)]
        [InlineData((ushort)0xAAAA, (ushort)0x5555, (ushort)0xFFFF)]
        [InlineData((ushort)0xFFFF, (ushort)0x0000, (ushort)0xFFFF)]
        [InlineData((ushort)0x0000, (ushort)0x0000, (ushort)0x0000)]
        public void InterlockedOr_Generic_UInt16(ushort initial, ushort operand, ushort expected)
        {
            ushort value = initial;
            Assert.Equal(initial, Interlocked.Or<ushort>(ref value, operand));
            Assert.Equal(expected, value);
        }

        [Fact]
        public void InterlockedOr_Generic_UInt16_NullRef()
        {
            Assert.Throws<NullReferenceException>(() => Interlocked.Or<ushort>(ref Unsafe.NullRef<ushort>(), (ushort)0x5008));
        }

        [Theory]
        [InlineData((short)0x1234, (short)0x5008, (short)0x523C)]
        [InlineData((short)-32768, (short)32767, (short)-1)]
        [InlineData((short)0, (short)-1, (short)-1)]
        [InlineData((short)-1, (short)0, (short)-1)]
        [InlineData((short)0, (short)0, (short)0)]
        public void InterlockedOr_Generic_Int16(short initial, short operand, short expected)
        {
            short value = initial;
            Assert.Equal(initial, Interlocked.Or<short>(ref value, operand));
            Assert.Equal(expected, value);
        }

        [Fact]
        public void InterlockedOr_Generic_Int16_NullRef()
        {
            Assert.Throws<NullReferenceException>(() => Interlocked.Or<short>(ref Unsafe.NullRef<short>(), (short)0x5008));
        }

        [Theory]
        [InlineData(0x12345670, 0x07654321, 0x17755771)]
        [InlineData(int.MinValue, int.MaxValue, -1)]
        [InlineData(0, -1, -1)]
        [InlineData(-1, 0, -1)]
        [InlineData(0, 0, 0)]
        public void InterlockedOr_Generic_Int32(int initial, int operand, int expected)
        {
            int value = initial;
            Assert.Equal(initial, Interlocked.Or<int>(ref value, operand));
            Assert.Equal(expected, value);
        }

        [Fact]
        public void InterlockedOr_Generic_Int32_NullRef()
        {
            Assert.Throws<NullReferenceException>(() => Interlocked.Or<int>(ref Unsafe.NullRef<int>(), 0x7654321));
        }

        [Theory]
        [InlineData(0x12345670u, 0x07654321u, 0x17755771u)]
        [InlineData(0x00000000u, 0xFFFFFFFFu, 0xFFFFFFFFu)]
        [InlineData(0xAAAAAAAAu, 0x55555555u, 0xFFFFFFFFu)]
        [InlineData(0xFFFFFFFFu, 0x00000000u, 0xFFFFFFFFu)]
        [InlineData(0x00000000u, 0x00000000u, 0x00000000u)]
        public void InterlockedOr_Generic_UInt32(uint initial, uint operand, uint expected)
        {
            uint value = initial;
            Assert.Equal(initial, Interlocked.Or<uint>(ref value, operand));
            Assert.Equal(expected, value);
        }

        [Fact]
        public void InterlockedOr_Generic_UInt32_NullRef()
        {
            Assert.Throws<NullReferenceException>(() => Interlocked.Or<uint>(ref Unsafe.NullRef<uint>(), 0x7654321u));
        }

        [Theory]
        [InlineData(0x12345670L, 0x07654321L, 0x17755771L)]
        [InlineData(long.MinValue, long.MaxValue, -1L)]
        [InlineData(0L, -1L, -1L)]
        [InlineData(-1L, 0L, -1L)]
        [InlineData(0L, 0L, 0L)]
        public void InterlockedOr_Generic_Int64(long initial, long operand, long expected)
        {
            long value = initial;
            Assert.Equal(initial, Interlocked.Or<long>(ref value, operand));
            Assert.Equal(expected, value);
        }

        [Fact]
        public void InterlockedOr_Generic_Int64_NullRef()
        {
            Assert.Throws<NullReferenceException>(() => Interlocked.Or<long>(ref Unsafe.NullRef<long>(), 0x7654321L));
        }

        [Theory]
        [InlineData(0x12345670UL, 0x07654321UL, 0x17755771UL)]
        [InlineData(0x0000000000000000UL, 0xFFFFFFFFFFFFFFFFUL, 0xFFFFFFFFFFFFFFFFUL)]
        [InlineData(0xAAAAAAAAAAAAAAAAUL, 0x5555555555555555UL, 0xFFFFFFFFFFFFFFFFUL)]
        [InlineData(0xFFFFFFFFFFFFFFFFUL, 0x0000000000000000UL, 0xFFFFFFFFFFFFFFFFUL)]
        [InlineData(0x0000000000000000UL, 0x0000000000000000UL, 0x0000000000000000UL)]
        public void InterlockedOr_Generic_UInt64(ulong initial, ulong operand, ulong expected)
        {
            ulong value = initial;
            Assert.Equal(initial, Interlocked.Or<ulong>(ref value, operand));
            Assert.Equal(expected, value);
        }

        [Fact]
        public void InterlockedOr_Generic_UInt64_NullRef()
        {
            Assert.Throws<NullReferenceException>(() => Interlocked.Or<ulong>(ref Unsafe.NullRef<ulong>(), 0x7654321UL));
        }

        [Theory]
        [InlineData(DayOfWeek.Sunday, DayOfWeek.Monday, DayOfWeek.Sunday | DayOfWeek.Monday)]
        [InlineData((DayOfWeek)0x1, (DayOfWeek)0x2, (DayOfWeek)0x3)]
        [InlineData((DayOfWeek)0x00, (DayOfWeek)0xFF, (DayOfWeek)0xFF)]
        [InlineData((DayOfWeek)0xFF, (DayOfWeek)0x00, (DayOfWeek)0xFF)]
        public void InterlockedOr_Generic_Enum(DayOfWeek initial, DayOfWeek operand, DayOfWeek expected)
        {
            DayOfWeek value = initial;
            Assert.Equal(initial, Interlocked.Or<DayOfWeek>(ref value, operand));
            Assert.Equal(expected, value);
        }

        [Fact]
        public void InterlockedOr_Generic_Enum_NullRef()
        {
            Assert.Throws<NullReferenceException>(() => Interlocked.Or<DayOfWeek>(ref Unsafe.NullRef<DayOfWeek>(), DayOfWeek.Monday));
        }

        [Fact]
        public void InterlockedOr_Generic_Float_ThrowsNotSupported()
        {
            float value = 1.0f;
            Assert.Throws<NotSupportedException>(() => Interlocked.Or<float>(ref value, 1.0f));
        }

        [Fact]
        public void InterlockedOr_Generic_Double_ThrowsNotSupported()
        {
            double value = 1.0;
            Assert.Throws<NotSupportedException>(() => Interlocked.Or<double>(ref value, 1.0));
        }

        [Fact]
        public void InterlockedOr_Generic_Half_ThrowsNotSupported()
        {
            Half value = (Half)1.0;
            Assert.Throws<NotSupportedException>(() => Interlocked.Or<Half>(ref value, (Half)1.0));
        }

        [Fact]
        public void InterlockedOr_Generic_Unsupported()
        {
            DateTime value = default;
            Assert.Throws<NotSupportedException>(() => Interlocked.Or<DateTime>(ref value, default));
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public void InterlockedIncrement_Multithreaded_Int32()
        {
            const int ThreadCount = 10;
            const int IterationCount = 100;

            int value = 0;
            var threadStarted = new AutoResetEvent(false);
            var startTest = new ManualResetEvent(false);
            Action threadStart = () =>
            {
                threadStarted.Set();
                startTest.CheckedWait();
                for (int i = 0; i < IterationCount; ++i)
                {
                    Interlocked.Increment(ref value);
                }
            };

            var waitsForThread = new Action[ThreadCount];
            for (int i = 0; i < ThreadCount; ++i)
            {
                Thread t = ThreadTestHelpers.CreateGuardedThread(out waitsForThread[i], threadStart);
                t.IsBackground = true;
                t.Start();
                threadStarted.CheckedWait();
            }

            startTest.Set();
            foreach (var waitForThread in waitsForThread)
            {
                waitForThread();
            }

            Assert.Equal(ThreadCount * IterationCount, Interlocked.CompareExchange(ref value, 0, 0));
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public void InterlockedCompareExchange_Multithreaded_Double()
        {
            const int ThreadCount = 10;
            const int IterationCount = 100;
            const double Increment = ((long)1 << 32) + 1;

            double value = 0;
            var threadStarted = new AutoResetEvent(false);
            var startTest = new ManualResetEvent(false);
            Action threadStart = () =>
            {
                threadStarted.Set();
                startTest.CheckedWait();
                for (int i = 0; i < IterationCount; ++i)
                {
                    double oldValue = value;
                    while (true)
                    {
                        double valueBeforeUpdate = Interlocked.CompareExchange(ref value, oldValue + Increment, oldValue);
                        if (valueBeforeUpdate == oldValue)
                        {
                            break;
                        }

                        oldValue = valueBeforeUpdate;
                    }
                }
            };

            var waitsForThread = new Action[ThreadCount];
            for (int i = 0; i < ThreadCount; ++i)
            {
                Thread t = ThreadTestHelpers.CreateGuardedThread(out waitsForThread[i], threadStart);
                t.IsBackground = true;
                t.Start();
                threadStarted.CheckedWait();
            }

            startTest.Set();
            foreach (var waitForThread in waitsForThread)
            {
                waitForThread();
            }

            Assert.Equal(ThreadCount * IterationCount * Increment, Interlocked.CompareExchange(ref value, 0, 0));
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public void InterlockedAddAndRead_Multithreaded_Int64()
        {
            const int ThreadCount = 10;
            const int IterationCount = 100;
            const long Increment = ((long)1 << 32) + 1;

            long value = 0;
            var threadStarted = new AutoResetEvent(false);
            var startTest = new ManualResetEvent(false);
            int completedThreadCount = 0;
            Action threadStart = () =>
            {
                threadStarted.Set();
                startTest.CheckedWait();
                for (int i = 0; i < IterationCount; ++i)
                {
                    Interlocked.Add(ref value, Increment);
                }

                Interlocked.Increment(ref completedThreadCount);
            };

            var checksForThreadErrors = new Action[ThreadCount];
            var waitsForThread = new Action[ThreadCount];
            for (int i = 0; i < ThreadCount; ++i)
            {
                Thread t =
                    ThreadTestHelpers.CreateGuardedThread(out checksForThreadErrors[i], out waitsForThread[i], threadStart);
                t.IsBackground = true;
                t.Start();
                threadStarted.CheckedWait();
            }

            startTest.Set();
            ThreadTestHelpers.WaitForConditionWithCustomDelay(
                () => completedThreadCount >= ThreadCount,
                () =>
                {
                    long valueSnapshot = Interlocked.Read(ref value);
                    Assert.Equal((int)valueSnapshot, (int)(valueSnapshot >> 32));

                    foreach (var checkForThreadErrors in checksForThreadErrors)
                    {
                        checkForThreadErrors();
                    }

                    Thread.Sleep(1);
                });
            foreach (var waitForThread in waitsForThread)
            {
                waitForThread();
            }

            Assert.Equal(ThreadCount, completedThreadCount);
            Assert.Equal(ThreadCount * IterationCount * Increment, Interlocked.Read(ref value));
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static Action MemoryBarrierDelegate() => Interlocked.MemoryBarrier;

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static delegate* <void> MemoryBarrierPointer() => &Interlocked.MemoryBarrier;

        [Fact()]
        public void MemoryBarrierIntrinsic()
        {
            // Interlocked.MemoryBarrier is a self-referring intrinsic
            // we should be able to call it through a delegate.
            MemoryBarrierDelegate()();

            // through a method pointer
            MemoryBarrierPointer()();
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public void MemoryBarrierProcessWide()
        {
            // Stress MemoryBarrierProcessWide correctness using a simple AsymmetricLock

            AsymmetricLock asymmetricLock = new AsymmetricLock();
            List<Task> threads = new List<Task>();
            int count = 0;
            for (int i = 0; i < 1000; i++)
            {
                threads.Add(Task.Run(() =>
                {
                    for (int j = 0; j < 1000; j++)
                    {
                        var cookie = asymmetricLock.Enter();
                        count++;
                        cookie.Exit();
                    }
                }));
            }
            Task.WaitAll(threads);
            Assert.Equal(1000*1000, count);
        }

        // Taking this lock on the same thread repeatedly is very fast because it has no interlocked operations.
        // Switching the thread where the lock is taken is expensive because of allocation and FlushProcessWriteBuffers.
        private class AsymmetricLock
        {
            public class LockCookie
            {
                internal LockCookie(int threadId)
                {
                    ThreadId = threadId;
                    Taken = false;
                }

                public void Exit()
                {
                    Debug.Assert(ThreadId == Environment.CurrentManagedThreadId);
                    Taken = false;
                }

                internal readonly int ThreadId;
                internal bool Taken;
            }

            private LockCookie _current = new LockCookie(-1);

            [MethodImpl(MethodImplOptions.NoInlining)]
            private static T VolatileReadWithoutBarrier<T>(ref T location)
            {
                return location;
            }

            // Returning LockCookie to call Exit on is the fastest implementation because of it works naturally with the RCU pattern.
            // The traditional Enter/Exit lock interface would require thread local storage or some other scheme to reclaim the cookie.
            // Returning LockCookie to call Exit on is the fastest implementation because of it works naturally with the RCU pattern.
            // The traditional Enter/Exit lock interface would require thread local storage or some other scheme to reclaim the cookie
            public LockCookie Enter()
            {
                int currentThreadId = Environment.CurrentManagedThreadId;

                LockCookie entry = _current;

                if (entry.ThreadId == currentThreadId)
                {
                    entry.Taken = true;

                    //
                    // If other thread started stealing the ownership, we need to take slow path.
                    //
                    // Make sure that the compiler won't reorder the read with the above write by wrapping the read in no-inline method.
                    // RyuJIT won't reorder them today, but more advanced optimizers might. Regular Volatile.Read would be too big of
                    // a hammer because of it will result into memory barrier on ARM that we do not need here.
                    //
                    //
                    if (VolatileReadWithoutBarrier(ref _current) == entry)
                    {
                        return entry;
                    }

                    entry.Taken = false;
                }

                return EnterSlow();
            }

            private LockCookie EnterSlow()
            {
                // Attempt to steal the ownership. Take a regular lock to ensure that only one thread is trying to steal it at a time.
                lock (this)
                {
                    // We are the new fast thread now!
                    var oldEntry = _current;
                    _current = new LockCookie(Environment.CurrentManagedThreadId);

                    // After MemoryBarrierProcessWide, we can be sure that the Volatile.Read done by the fast thread will see that it is not a fast
                    // thread anymore, and thus it will not attempt to enter the lock.
                    Interlocked.MemoryBarrierProcessWide();

                    // Keep looping as long as the lock is taken by other thread
                    SpinWait sw = new SpinWait();
                    while (oldEntry.Taken)
                        sw.SpinOnce();

                    // We have seen that the other thread released the lock by setting Taken to false.
                    // However, on platforms with weak memory ordering (ex: ARM32, ARM64) observing that does not guarantee that the writes executed by that
                    // thread prior to releasing the lock are all committed to the shared memory.
                    // We could fix that by doing the release via Volatile.Write, but we do not want to add expense to every release on the fast path.
                    // Instead we will do another MemoryBarrierProcessWide here.

                    // NOTE: not needed on x86/x64
                    Interlocked.MemoryBarrierProcessWide();

                    _current.Taken = true;
                    return _current;
                }
            }
        }
    }
}
