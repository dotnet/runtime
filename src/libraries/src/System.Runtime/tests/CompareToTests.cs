// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Xunit;

namespace System.Tests
{
    public static partial class CompareToTests
    {
        [Fact]
        public static void FromBoolean()
        {
            Assert.Equal(0, false.CompareTo(false));
            Assert.Equal(0, true.CompareTo(true));
            Assert.True(0 > false.CompareTo(true));
            Assert.True(0 < true.CompareTo(false));

            Assert.Equal(0, false.CompareTo((object)false));
            Assert.Equal(0, true.CompareTo((object)true));
            Assert.True(0 > false.CompareTo((object)true));
            Assert.True(0 < true.CompareTo((object)false));

            AssertExtensions.Throws<ArgumentException>(null, () => false.CompareTo("hello"));
        }

        [Fact]
        public static void FromByte()
        {
            Assert.Equal(0, ((byte)0).CompareTo((byte)0));
            Assert.True(0 > byte.MinValue.CompareTo((byte)1));
            Assert.True(0 < byte.MaxValue.CompareTo((byte)0));
            Assert.True(0 > ((byte)0).CompareTo((byte)1));

            Assert.Equal(0, ((byte)0).CompareTo((object)(byte)0));
            Assert.True(0 > byte.MinValue.CompareTo((object)(byte)1));
            Assert.True(0 < byte.MaxValue.CompareTo((object)(byte)0));
            Assert.True(0 > ((byte)0).CompareTo((object)(byte)1));

            AssertExtensions.Throws<ArgumentException>(null, () => ((byte)0.CompareTo("hello")));
        }

        [Fact]
        public static void FromChar()
        {
            Assert.Equal(0, 'c'.CompareTo('c'));
            Assert.True(0 > 'a'.CompareTo('c'));
            Assert.True(0 < 'd'.CompareTo('c'));
            AssertExtensions.Throws<ArgumentException>(null, () => 'x'.CompareTo("hello"));
        }

        [Fact]
        public static void FromDecimal()
        {
            Assert.Equal(0, ((decimal)0).CompareTo((decimal)0));
            Assert.True(0 > decimal.MinValue.CompareTo((decimal)0));
            Assert.True(0 < decimal.MaxValue.CompareTo((decimal)0));
            Assert.True(0 > ((decimal)0).CompareTo((decimal)1));
            Assert.True(0 < ((decimal)0).CompareTo((decimal)-1));

            Assert.Equal(0, ((decimal)0).CompareTo((object)(decimal)0));
            Assert.True(0 > decimal.MinValue.CompareTo((object)(decimal)0));
            Assert.True(0 < decimal.MaxValue.CompareTo((object)(decimal)0));
            Assert.True(0 > ((decimal)0).CompareTo((object)(decimal)1));
            Assert.True(0 < ((decimal)0).CompareTo((object)(decimal)-1));

            AssertExtensions.Throws<ArgumentException>(null, () => ((decimal)0.CompareTo("hello")));
        }

        [Fact]
        public static void FromDouble()
        {
            Assert.Equal(0, ((double)0).CompareTo((double)0));
            Assert.True(0 > double.MinValue.CompareTo((double)0));
            Assert.True(0 < double.MaxValue.CompareTo((double)0));
            Assert.True(0 > ((double)0).CompareTo((double)1));
            Assert.True(0 < ((double)0).CompareTo((double)-1));

            Assert.Equal(0, ((double)0).CompareTo((object)(double)0));
            Assert.True(0 > double.MinValue.CompareTo((object)(double)0));
            Assert.True(0 < double.MaxValue.CompareTo((object)(double)0));
            Assert.True(0 > ((double)0).CompareTo((object)(double)1));
            Assert.True(0 < ((double)0).CompareTo((object)(double)-1));

            AssertExtensions.Throws<ArgumentException>(null, () => ((double)0.CompareTo("hello")));
        }

        [Fact]
        public static void FromInt16()
        {
            Assert.Equal(0, ((short)0).CompareTo((short)0));
            Assert.True(0 > short.MinValue.CompareTo((short)0));
            Assert.True(0 < short.MaxValue.CompareTo((short)0));
            Assert.True(0 > ((short)0).CompareTo((short)1));
            Assert.True(0 < ((short)0).CompareTo((short)-1));

            Assert.Equal(0, ((short)0).CompareTo((object)(short)0));
            Assert.True(0 > short.MinValue.CompareTo((object)(short)0));
            Assert.True(0 < short.MaxValue.CompareTo((object)(short)0));
            Assert.True(0 > ((short)0).CompareTo((object)(short)1));
            Assert.True(0 < ((short)0).CompareTo((object)(short)-1));

            AssertExtensions.Throws<ArgumentException>(null, () => ((short)0.CompareTo("hello")));
        }

        [Fact]
        public static void FromInt32()
        {
            Assert.Equal(0, ((int)0).CompareTo((int)0));
            Assert.True(0 > int.MinValue.CompareTo((int)0));
            Assert.True(0 < int.MaxValue.CompareTo((int)0));
            Assert.True(0 > ((int)0).CompareTo((int)1));
            Assert.True(0 < ((int)0).CompareTo((int)-1));

            Assert.Equal(0, ((int)0).CompareTo((object)(int)0));
            Assert.True(0 > int.MinValue.CompareTo((object)(int)0));
            Assert.True(0 < int.MaxValue.CompareTo((object)(int)0));
            Assert.True(0 > ((int)0).CompareTo((object)(int)1));
            Assert.True(0 < ((int)0).CompareTo((object)(int)-1));

            AssertExtensions.Throws<ArgumentException>(null, () => ((int)0.CompareTo("hello")));
        }

        [Fact]
        public static void FromInt64()
        {
            Assert.Equal(0, ((long)0).CompareTo((long)0));
            Assert.True(0 > long.MinValue.CompareTo((long)0));
            Assert.True(0 < long.MaxValue.CompareTo((long)0));
            Assert.True(0 > ((long)0).CompareTo((long)1));
            Assert.True(0 < ((long)0).CompareTo((long)-1));

            Assert.Equal(0, ((long)0).CompareTo((object)(long)0));
            Assert.True(0 > long.MinValue.CompareTo((object)(long)0));
            Assert.True(0 < long.MaxValue.CompareTo((object)(long)0));
            Assert.True(0 > ((long)0).CompareTo((object)(long)1));
            Assert.True(0 < ((long)0).CompareTo((object)(long)-1));

            AssertExtensions.Throws<ArgumentException>(null, () => ((long)0.CompareTo("hello")));
        }

        public static void FromSByte()
        {
            Assert.Equal(0, ((sbyte)0).CompareTo((sbyte)0));
            Assert.True(0 > sbyte.MinValue.CompareTo((sbyte)0));
            Assert.True(0 < sbyte.MaxValue.CompareTo((sbyte)0));
            Assert.True(0 > ((sbyte)0).CompareTo((sbyte)1));
            Assert.True(0 < ((sbyte)0).CompareTo((sbyte)-1));

            Assert.Equal(0, ((sbyte)0).CompareTo((object)(sbyte)0));
            Assert.True(0 > sbyte.MinValue.CompareTo((object)(sbyte)0));
            Assert.True(0 < sbyte.MaxValue.CompareTo((object)(sbyte)0));
            Assert.True(0 > ((sbyte)0).CompareTo((object)(sbyte)1));
            Assert.True(0 < ((sbyte)0).CompareTo((object)(sbyte)-1));

            AssertExtensions.Throws<ArgumentException>(null, () => ((sbyte)0.CompareTo("hello")));
        }

        [Fact]
        public static void FromSingle()
        {
            Assert.Equal(0, ((float)0).CompareTo((float)0));
            Assert.True(0 > float.MinValue.CompareTo((float)0));
            Assert.True(0 < float.MaxValue.CompareTo((float)0));
            Assert.True(0 > ((float)0).CompareTo((float)1));
            Assert.True(0 < ((float)0).CompareTo((float)-1));

            Assert.Equal(0, ((float)0).CompareTo((object)(float)0));
            Assert.True(0 > float.MinValue.CompareTo((object)(float)0));
            Assert.True(0 < float.MaxValue.CompareTo((object)(float)0));
            Assert.True(0 > ((float)0).CompareTo((object)(float)1));
            Assert.True(0 < ((float)0).CompareTo((object)(float)-1));

            AssertExtensions.Throws<ArgumentException>(null, () => ((float)0.CompareTo("hello")));
        }

        [Fact]
        public static void FromUInt16()
        {
            Assert.Equal(0, ((ushort)0).CompareTo((ushort)0));
            Assert.True(0 > ushort.MinValue.CompareTo((ushort)1));
            Assert.True(0 < ushort.MaxValue.CompareTo((ushort)0));
            Assert.True(0 > ((ushort)0).CompareTo((ushort)1));

            Assert.Equal(0, ((ushort)0).CompareTo((object)(ushort)0));
            Assert.True(0 > ushort.MinValue.CompareTo((object)(ushort)1));
            Assert.True(0 < ushort.MaxValue.CompareTo((object)(ushort)0));
            Assert.True(0 > ((ushort)0).CompareTo((object)(ushort)1));

            AssertExtensions.Throws<ArgumentException>(null, () => ((ushort)0.CompareTo("hello")));
        }

        [Fact]
        public static void FromUInt32()
        {
            Assert.Equal(0, ((uint)0).CompareTo((uint)0));
            Assert.True(0 > uint.MinValue.CompareTo((uint)1));
            Assert.True(0 < uint.MaxValue.CompareTo((uint)0));
            Assert.True(0 > ((uint)0).CompareTo((uint)1));

            Assert.Equal(0, ((uint)0).CompareTo((object)(uint)0));
            Assert.True(0 > uint.MinValue.CompareTo((object)(uint)1));
            Assert.True(0 < uint.MaxValue.CompareTo((object)(uint)0));
            Assert.True(0 > ((uint)0).CompareTo((object)(uint)1));

            AssertExtensions.Throws<ArgumentException>(null, () => ((uint)0.CompareTo("hello")));
        }

        [Fact]
        public static void FromUInt64()
        {
            Assert.Equal(0, ((ulong)0).CompareTo((ulong)0));
            Assert.True(0 > ulong.MinValue.CompareTo((ulong)1));
            Assert.True(0 < ulong.MaxValue.CompareTo((ulong)0));
            Assert.True(0 > ((ulong)0).CompareTo((ulong)1));

            Assert.Equal(0, ((ulong)0).CompareTo((object)(ulong)0));
            Assert.True(0 > ulong.MinValue.CompareTo((object)(ulong)1));
            Assert.True(0 < ulong.MaxValue.CompareTo((object)(ulong)0));
            Assert.True(0 > ((ulong)0).CompareTo((object)(ulong)1));

            AssertExtensions.Throws<ArgumentException>(null, () => ((ulong)0.CompareTo("hello")));
        }
    }
}
