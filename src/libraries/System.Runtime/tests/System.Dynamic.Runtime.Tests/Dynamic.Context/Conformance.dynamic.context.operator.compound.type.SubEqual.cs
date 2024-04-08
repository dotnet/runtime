// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CSharp.RuntimeBinder;
using Xunit;

using static Dynamic.Operator.Tests.TypeCommon;

namespace Dynamic.Operator.Tests
{
    public class MinusEqualsTypeTests
    {
        [Fact]
        public static void Bool()
        {
            dynamic d = true;

            Assert.Throws<RuntimeBinderException>(() => d -= s_bool);
            Assert.Throws<RuntimeBinderException>(() => d -= s_byte);
            Assert.Throws<RuntimeBinderException>(() => d -= s_char);
            Assert.Throws<RuntimeBinderException>(() => d -= s_decimal);
            Assert.Throws<RuntimeBinderException>(() => d -= s_double);
            Assert.Throws<RuntimeBinderException>(() => d -= s_float);
            Assert.Throws<RuntimeBinderException>(() => d -= s_int);
            Assert.Throws<RuntimeBinderException>(() => d -= s_long);
            Assert.Throws<RuntimeBinderException>(() => d -= s_object);
            Assert.Throws<RuntimeBinderException>(() => d -= s_sbyte);
            Assert.Throws<RuntimeBinderException>(() => d -= s_short);

            d = true;
            Assert.Throws<RuntimeBinderException>(() => d -= s_string);

            d = true;
            Assert.Throws<RuntimeBinderException>(() => d -= s_uint);
            Assert.Throws<RuntimeBinderException>(() => d -= s_ulong);
            Assert.Throws<RuntimeBinderException>(() => d -= s_ushort);
        }

        [Fact]
        public static void Byte()
        {
            dynamic d = (byte)1;
            Assert.Throws<RuntimeBinderException>(() => d -= s_bool);

            d = (byte)1;
            d -= s_byte;

            d = (byte)1;
            d -= s_char;

            d = (byte)1;
            d -= s_decimal;

            d = (byte)1;
            d -= s_double;

            d = (byte)1;
            d -= s_float;

            d = (byte)1;
            d -= s_int;

            d = (byte)1;
            d -= s_long;

            d = (byte)1;
            Assert.Throws<RuntimeBinderException>(() => d -= s_object);

            d = (byte)1;
            d -= s_sbyte;

            d = (byte)1;
            d -= s_short;

            d = (byte)1;
            Assert.Throws<RuntimeBinderException>(() => d -= s_string);

            d = (byte)1;
            unchecked { d -= s_uint; }

            d = (byte)1;
            unchecked { d -= s_ulong; }

            d = (byte)1;
            d -= s_ushort;
        }

        [Fact]
        public static void Char()
        {
            dynamic d = 'a';
            Assert.Throws<RuntimeBinderException>(() => d -= s_bool);

            d = 'a';
            d -= s_byte;

            d = 'a';
            d -= s_char;

            d = 'a';
            d -= s_decimal;

            d = 'a';
            d -= s_double;

            d = 'a';
            d -= s_float;

            d = 'a';
            d -= s_int;

            d = 'a';
            d -= s_long;

            d = 'a';
            Assert.Throws<RuntimeBinderException>(() => d -= s_object);

            d = 'a';
            d -= s_sbyte;

            d = 'a';
            d -= s_short;

            d = 'a';
            Assert.Throws<RuntimeBinderException>(() => d -= s_string);

            d = 'a';
            d -= s_uint;

            d = 'a';
            d -= s_ulong;

            d = 'a';
            d -= s_ushort;
        }

        [Fact]
        public static void Decimal()
        {
            dynamic d = 10m;
            Assert.Throws<RuntimeBinderException>(() => d -= s_bool);

            d = 10m;
            d -= s_byte;

            d = 10m;
            d -= s_char;

            d = 10m;
            d -= s_decimal;

            d = 10m;
            Assert.Throws<RuntimeBinderException>(() => d -= s_double);
            Assert.Throws<RuntimeBinderException>(() => d -= s_float);

            d = 10m;
            d -= s_int;

            d = 10m;
            d -= s_long;

            d = 10m;
            Assert.Throws<RuntimeBinderException>(() => d -= s_object);

            d = 10m;
            d -= s_sbyte;

            d = 10m;
            d -= s_short;

            d = 10m;
            Assert.Throws<RuntimeBinderException>(() => d -= s_string);

            d = 10m;
            d -= s_uint;

            d = 10m;
            d -= s_ulong;

            d = 10m;
            d -= s_ushort;
        }

        [Fact]
        public static void Double()
        {
            dynamic d = 10d;
            Assert.Throws<RuntimeBinderException>(() => d -= s_bool);

            d = 10d;
            d -= s_byte;

            d = 10d;
            d -= s_char;

            d = 10d;
            Assert.Throws<RuntimeBinderException>(() => d -= s_decimal);

            d = 10d;
            d -= s_double;

            d = 10d;
            d -= s_float;

            d = 10d;
            d -= s_int;

            d = 10d;
            d -= s_long;

            d = 10d;
            Assert.Throws<RuntimeBinderException>(() => d -= s_object);

            d = 10d;
            d -= s_sbyte;

            d = 10d;
            d -= s_short;

            d = 10d;
            Assert.Throws<RuntimeBinderException>(() => d -= s_string);

            d = 10d;
            d -= s_uint;

            d = 10d;
            d -= s_ulong;

            d = 10d;
            d -= s_ushort;
        }

        [Fact]
        public static void Float()
        {
            dynamic d = 10f;
            Assert.Throws<RuntimeBinderException>(() => d -= s_bool);

            d = 10f;
            d -= s_byte;

            d = 10f;
            d -= s_char;

            d = 10f;
            Assert.Throws<RuntimeBinderException>(() => d -= s_decimal);

            d = 10f;
            d -= s_double;

            d = 10f;
            d -= s_float;

            d = 10f;
            d -= s_int;

            d = 10f;
            d -= s_long;

            d = 10f;
            Assert.Throws<RuntimeBinderException>(() => d -= s_object);

            d = 10f;
            d -= s_sbyte;

            d = 10f;
            d -= s_short;

            d = 10f;
            Assert.Throws<RuntimeBinderException>(() => d -= s_string);

            d = 10f;
            d -= s_uint;

            d = 10f;
            d -= s_ulong;

            d = 10f;
            d -= s_ushort;
        }

        [Fact]
        public static void Int()
        {
            dynamic d = 10;
            Assert.Throws<RuntimeBinderException>(() => d -= s_bool);

            d = 10;
            d -= s_byte;

            d = 10;
            d -= s_char;

            d = 10;
            d -= s_decimal;

            d = 10;
            d -= s_double;

            d = 10;
            d -= s_float;

            d = 10;
            d -= s_int;

            d = 10;
            d -= s_long;

            d = 10;
            Assert.Throws<RuntimeBinderException>(() => d -= s_object);

            d = 10;
            d -= s_sbyte;

            d = 10;
            d -= s_short;

            d = 10;
            Assert.Throws<RuntimeBinderException>(() => d -= s_string);

            d = 10;
            d -= s_uint;

            d = 10;
            Assert.Throws<RuntimeBinderException>(() => d -= s_ulong);

            d = 10;
            d -= s_ushort;
        }

        [Fact]
        public static void Long()
        {
            dynamic d = 10L;
            Assert.Throws<RuntimeBinderException>(() => d -= s_bool);

            d = 10L;
            d -= s_byte;

            d = 10L;
            d -= s_char;

            d = 10L;
            d -= s_decimal;

            d = 10L;
            d -= s_double;

            d = 10L;
            d -= s_float;

            d = 10L;
            d -= s_int;

            d = 10L;
            d -= s_long;

            d = 10L;
            Assert.Throws<RuntimeBinderException>(() => d -= s_object);

            d = 10L;
            d -= s_sbyte;

            d = 10L;
            d -= s_short;

            d = 10L;
            Assert.Throws<RuntimeBinderException>(() => d -= s_string);

            d = 10L;
            d -= s_uint;

            d = 10L;
            Assert.Throws<RuntimeBinderException>(() => d -= s_ulong);

            d = 10L;
            d -= s_ushort;
        }

        [Fact]
        public static void Object()
        {
            dynamic d = new object();

            Assert.Throws<RuntimeBinderException>(() => d -= s_bool);
            Assert.Throws<RuntimeBinderException>(() => d -= s_byte);
            Assert.Throws<RuntimeBinderException>(() => d -= s_char);
            Assert.Throws<RuntimeBinderException>(() => d -= s_decimal);
            Assert.Throws<RuntimeBinderException>(() => d -= s_double);
            Assert.Throws<RuntimeBinderException>(() => d -= s_float);
            Assert.Throws<RuntimeBinderException>(() => d -= s_int);
            Assert.Throws<RuntimeBinderException>(() => d -= s_long);
            Assert.Throws<RuntimeBinderException>(() => d -= s_object);
            Assert.Throws<RuntimeBinderException>(() => d -= s_sbyte);
            Assert.Throws<RuntimeBinderException>(() => d -= s_short);

            d = new object();
            Assert.Throws<RuntimeBinderException>(() => d -= s_string);

            d = new object();
            Assert.Throws<RuntimeBinderException>(() => d -= s_uint);
            Assert.Throws<RuntimeBinderException>(() => d -= s_ulong);
            Assert.Throws<RuntimeBinderException>(() => d -= s_ushort);
        }

        [Fact]
        public static void SByte()
        {
            dynamic d = (sbyte)10;
            Assert.Throws<RuntimeBinderException>(() => d -= s_bool);

            d = (sbyte)10;
            d -= s_byte;

            d = (sbyte)10;
            d -= s_char;

            d = (sbyte)10;
            d -= s_decimal;

            d = (sbyte)10;
            d -= s_double;

            d = (sbyte)10;
            d -= s_float;

            d = (sbyte)10;
            d -= s_int;

            d = (sbyte)10;
            d -= s_long;

            d = (sbyte)10;
            Assert.Throws<RuntimeBinderException>(() => d -= s_object);

            d = (sbyte)10;
            d -= s_sbyte;

            d = (sbyte)10;
            d -= s_short;

            d = (sbyte)10;
            Assert.Throws<RuntimeBinderException>(() => d -= s_string);

            d = (sbyte)10;
            d -= s_uint;

            d = (sbyte)10;
            Assert.Throws<RuntimeBinderException>(() => d -= s_ulong);

            d = (sbyte)10;
            d -= s_ushort;
        }

        [Fact]
        public static void Short()
        {
            dynamic d = (short)10;
            Assert.Throws<RuntimeBinderException>(() => d -= s_bool);

            d = (short)10;
            d -= s_byte;

            d = (short)10;
            d -= s_char;

            d = (short)10;
            d -= s_decimal;

            d = (short)10;
            d -= s_double;

            d = (short)10;
            d -= s_float;

            d = (short)10;
            d -= s_int;

            d = (short)10;
            d -= s_long;

            d = (short)10;
            Assert.Throws<RuntimeBinderException>(() => d -= s_object);

            d = (short)10;
            d -= s_sbyte;

            d = (short)10;
            d -= s_short;

            d = (short)10;
            Assert.Throws<RuntimeBinderException>(() => d -= s_string);

            d = (short)10;
            d -= s_uint;

            d = (short)10;
            Assert.Throws<RuntimeBinderException>(() => d -= s_ulong);

            d = (short)10;
            d -= s_ushort;
        }

        [Fact]
        public static void String()
        {
            dynamic d = "a";

            Assert.Throws<RuntimeBinderException>(() => d -= s_bool);
            Assert.Throws<RuntimeBinderException>(() => d -= s_byte);
            Assert.Throws<RuntimeBinderException>(() => d -= s_char);
            Assert.Throws<RuntimeBinderException>(() => d -= s_decimal);
            Assert.Throws<RuntimeBinderException>(() => d -= s_double);
            Assert.Throws<RuntimeBinderException>(() => d -= s_float);
            Assert.Throws<RuntimeBinderException>(() => d -= s_int);
            Assert.Throws<RuntimeBinderException>(() => d -= s_long);
            Assert.Throws<RuntimeBinderException>(() => d -= s_object);
            Assert.Throws<RuntimeBinderException>(() => d -= s_sbyte);
            Assert.Throws<RuntimeBinderException>(() => d -= s_short);
            Assert.Throws<RuntimeBinderException>(() => d -= s_string);
            Assert.Throws<RuntimeBinderException>(() => d -= s_uint);
            Assert.Throws<RuntimeBinderException>(() => d -= s_ulong);
            Assert.Throws<RuntimeBinderException>(() => d -= s_ushort);
        }

        [Fact]
        public static void UInt()
        {
            dynamic d = (uint)10;
            Assert.Throws<RuntimeBinderException>(() => d -= s_bool);

            d = (uint)10;
            d -= s_byte;

            d = (uint)10;
            unchecked { d -= s_char; }

            d = (uint)10;
            d -= s_decimal;

            d = (uint)10;
            d -= s_double;

            d = (uint)10;
            d -= s_float;

            d = (uint)10;
            d -= s_int;

            d = (uint)10;
            d -= s_long;

            d = (uint)10;
            Assert.Throws<RuntimeBinderException>(() => d -= s_object);

            d = (uint)10;
            d -= s_sbyte;

            d = (uint)10;
            d -= s_short;

            d = (uint)10;
            Assert.Throws<RuntimeBinderException>(() => d -= s_string);

            d = (uint)10;
            unchecked { d -= s_uint; }

            d = (uint)10;
            d -= s_ulong;

            d = (uint)10;
            d -= s_ushort;
        }

        [Fact]
        public static void ULong()
        {
            dynamic d = (ulong)10;
            Assert.Throws<RuntimeBinderException>(() => d -= s_bool);

            d = (ulong)10;
            d -= s_byte;

            d = (ulong)10;
            unchecked { d -= s_char; }

            d = (ulong)10;
            d -= s_decimal;

            d = (ulong)10;
            d -= s_double;

            d = (ulong)10;
            d -= s_float;

            d = (ulong)10;
            Assert.Throws<RuntimeBinderException>(() => d -= s_int);
            Assert.Throws<RuntimeBinderException>(() => d -= s_long);
            Assert.Throws<RuntimeBinderException>(() => d -= s_object);
            Assert.Throws<RuntimeBinderException>(() => d -= s_sbyte);
            Assert.Throws<RuntimeBinderException>(() => d -= s_short);

            d = (ulong)10;
            Assert.Throws<RuntimeBinderException>(() => d -= s_string);

            d = (ulong)10;
            unchecked { d -= s_uint; }

            d = (ulong)10;
            d -= s_ulong;

            d = (ulong)10;
            d -= s_ushort;
        }

        [Fact]
        public static void UShort()
        {
            dynamic d = (ushort)10;
            Assert.Throws<RuntimeBinderException>(() => d -= s_bool);

            d = (ushort)10;
            d -= s_byte;

            d = (ushort)10;
            d -= s_char;

            d = (ushort)10;
            d -= s_decimal;

            d = (ushort)10;
            d -= s_double;

            d = (ushort)10;
            d -= s_float;

            d = (ushort)10;
            d -= s_int;

            d = (ushort)10;
            d -= s_long;

            d = (ushort)10;
            Assert.Throws<RuntimeBinderException>(() => d -= s_object);

            d = (ushort)10;
            d -= s_sbyte;

            d = (ushort)10;
            d -= s_short;

            d = (ushort)10;
            Assert.Throws<RuntimeBinderException>(() => d -= s_string);

            d = (ushort)10;
            unchecked { d -= s_uint; }

            d = (ushort)10;
            d -= s_ulong;

            d = (ushort)10;
            d -= s_ushort;
        }
    }
}
