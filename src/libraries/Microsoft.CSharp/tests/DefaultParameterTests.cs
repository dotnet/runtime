// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Xunit;

namespace Microsoft.CSharp.RuntimeBinder.Tests
{
    [ActiveIssue("https://github.com/dotnet/runtime/issues/49568", typeof(PlatformDetection), nameof(PlatformDetection.IsMacOsAppleSilicon))]
    public class DefaultParameterTests
    {

#pragma warning disable 618
        public class MarshalAsMethods
        {
            // Try every defined type that will compile, even if it's nonsense.
            public object AnsiBStr([Optional, MarshalAs(UnmanagedType.AnsiBStr)] object val) => val;

            public object AsAny([Optional, MarshalAs(UnmanagedType.AsAny)] object val) => val;

            public object Bool([Optional, MarshalAs(UnmanagedType.Bool)] object val) => val;

            public object BStr([Optional, MarshalAs(UnmanagedType.BStr)] object val) => val;

            public object Currency([Optional, MarshalAs(UnmanagedType.Currency)] object val) => val;

            public object Error([Optional, MarshalAs(UnmanagedType.Error)] object val) => val;

            public object FunctionPtr([Optional, MarshalAs(UnmanagedType.FunctionPtr)] object val) => val;

            public object HString([Optional, MarshalAs(UnmanagedType.HString)] object val) => val;

            public object I1([Optional, MarshalAs(UnmanagedType.I1)] object val) => val;

            public object I2([Optional, MarshalAs(UnmanagedType.I2)] object val) => val;

            public object I4([Optional, MarshalAs(UnmanagedType.I4)] object val) => val;

            public object I8([Optional, MarshalAs(UnmanagedType.I8)] object val) => val;

            public object IDispatch([Optional, MarshalAs(UnmanagedType.IDispatch)] object val) => val;

            public object IInspectable([Optional, MarshalAs(UnmanagedType.IInspectable)] object val) => val;

            public object Interface([Optional, MarshalAs(UnmanagedType.Interface)] object val) => val;

            public object IUnknown([Optional, MarshalAs(UnmanagedType.IUnknown)] object val) => val;

            public object LPArray([Optional, MarshalAs(UnmanagedType.LPArray)] object val) => val;

            public object LPStr([Optional, MarshalAs(UnmanagedType.LPStr)] object val) => val;

            public object LPStruct([Optional, MarshalAs(UnmanagedType.LPStruct)] object val) => val;

            public object LPTStr([Optional, MarshalAs(UnmanagedType.LPTStr)] object val) => val;

            public object LPWStr([Optional, MarshalAs(UnmanagedType.LPWStr)] object val) => val;

            public object R4([Optional, MarshalAs(UnmanagedType.R4)] object val) => val;

            public object R8([Optional, MarshalAs(UnmanagedType.R8)] object val) => val;

            public object SafeArray([Optional, MarshalAs(UnmanagedType.SafeArray)] object val) => val;

            public object Struct([Optional, MarshalAs(UnmanagedType.Struct)] object val) => val;

            public object SysInt([Optional, MarshalAs(UnmanagedType.SysInt)] object val) => val;

            public object SysUInt([Optional, MarshalAs(UnmanagedType.SysUInt)] object val) => val;

            public object TBStr([Optional, MarshalAs(UnmanagedType.TBStr)] object val) => val;

            public object U1([Optional, MarshalAs(UnmanagedType.U1)] object val) => val;

            public object U2([Optional, MarshalAs(UnmanagedType.U2)] object val) => val;

            public object U4([Optional, MarshalAs(UnmanagedType.U4)] object val) => val;

            public object U8([Optional, MarshalAs(UnmanagedType.U8)] object val) => val;

            public object VariantBool([Optional, MarshalAs(UnmanagedType.VariantBool)] object val) => val;

            public object VBByRefStr([Optional, MarshalAs(UnmanagedType.VBByRefStr)] object val) => val;

            public object UndefinedType([Optional, MarshalAs((UnmanagedType)2000)] object val) => val;
        }
#pragma warning restore 618

        [Fact]
        public void MarshalAsOptionalsCorrectDefault()
        {
            dynamic d = new MarshalAsMethods();
            Assert.Same(Type.Missing, d.AnsiBStr());
            Assert.Same(Type.Missing, d.AsAny());
            Assert.Same(Type.Missing, d.Bool());
            Assert.Same(Type.Missing, d.BStr());
            Assert.Same(Type.Missing, d.Currency());
            Assert.Same(Type.Missing, d.Error());
            Assert.Same(Type.Missing, d.FunctionPtr());
            Assert.Same(Type.Missing, d.HString());
            Assert.Same(Type.Missing, d.I1());
            Assert.Same(Type.Missing, d.I2());
            Assert.Same(Type.Missing, d.I4());
            Assert.Same(Type.Missing, d.I8());
            Assert.Null(d.IDispatch());
            Assert.Same(Type.Missing, d.IInspectable());
            Assert.Null(d.Interface());
            Assert.Null(d.IUnknown());
            Assert.Same(Type.Missing, d.LPArray());
            Assert.Same(Type.Missing, d.LPStr());
            Assert.Same(Type.Missing, d.LPStruct());
            Assert.Same(Type.Missing, d.LPTStr());
            Assert.Same(Type.Missing, d.LPWStr());
            Assert.Same(Type.Missing, d.R4());
            Assert.Same(Type.Missing, d.R8());
            Assert.Same(Type.Missing, d.SafeArray());
            Assert.Same(Type.Missing, d.Struct());
            Assert.Same(Type.Missing, d.SysInt());
            Assert.Same(Type.Missing, d.SysUInt());
            Assert.Same(Type.Missing, d.TBStr());
            Assert.Same(Type.Missing, d.U1());
            Assert.Same(Type.Missing, d.U2());
            Assert.Same(Type.Missing, d.U4());
            Assert.Same(Type.Missing, d.U8());
            Assert.Same(Type.Missing, d.VariantBool());
            Assert.Same(Type.Missing, d.VBByRefStr());
        }

        public class TypeWithDefaults
        {
            public DateTime GetDate([Optional, DateTimeConstant(630823790456780000)] DateTime value) => value;

            public decimal GetDecimal(decimal value = 12.3m) => value;

            public byte GetByte(byte value = 123) => value;

            public short GetInt16(short value = 123) => value;

            public int GetInt32(int value = 123) => value;

            public long GetInt64(long value = 123) => value;

            public float GetSingle(float value = 123) => value;

            public double GetDouble(double value = 123) => value;

            public char GetChar(char value = 'X') => value;

            public bool GetBoolean(bool value = true) => value;

            public sbyte GetSByte(sbyte value = 123) => value;

            public ushort GetUInt16(ushort value = 123) => value;

            public uint GetUInt32(uint value = 123) => value;

            public ulong GetUInt64(ulong value = 123) => value;

            public string GetString(string value = "123") => value;

            public Uri GetURI(Uri value = null) => value;

            public StringComparison GetEnum(StringComparison value = StringComparison.InvariantCulture) => value;

            public KeyValuePair<int, string> GetStruct(KeyValuePair<int, string> value = default) => value;

        }

        public class TypeWithOptionalsWithoutDefaults
        {
            public DateTime GetDate([Optional] DateTime value) => value;

            public decimal GetDecimal([Optional] decimal value) => value;

            public byte GetByte([Optional] byte value) => value;

            public short GetInt16([Optional] short value) => value;

            public int GetInt32([Optional] int value) => value;

            public long GetInt64([Optional] long value) => value;

            public float GetSingle([Optional] float value) => value;

            public double GetDouble([Optional] double value) => value;

            public char GetChar([Optional] char value) => value;

            public bool GetBoolean([Optional] bool value) => value;

            public sbyte GetSByte([Optional] sbyte value) => value;

            public ushort GetUInt16([Optional] ushort value) => value;

            public uint GetUInt32([Optional] uint value) => value;

            public ulong GetUInt64([Optional] ulong value) => value;

            public string GetString([Optional] string value) => value;

            public Uri GetURI([Optional] Uri value) => value;

            public StringComparison GetEnum([Optional] StringComparison value) => value;

            public KeyValuePair<int, string> GetStruct([Optional] KeyValuePair<int, string> value) => value;
        }

        [Fact]
        public void DefaultDateTime()
        {
            dynamic d = new TypeWithDefaults();
            Assert.Equal(new DateTime(2000, 1, 2, 3, 4, 5, 678), d.GetDate());
            Assert.Equal(new DateTime(9876, 5, 4, 3, 2, 1), d.GetDate(new DateTime(9876, 5, 4, 3, 2, 1)));
        }

        [Fact]
        public void DefaultDecimal()
        {
            dynamic d = new TypeWithDefaults();
            Assert.Equal(12.3m, d.GetDecimal());
            Assert.Equal(49m, d.GetDecimal(49m));
        }

        [Fact]
        public void DefaultByte()
        {
            dynamic d = new TypeWithDefaults();
            Assert.Equal(123, d.GetByte());
            Assert.Equal(49, d.GetByte(49));
        }

        [Fact]
        public void DefaultInt16()
        {
            dynamic d = new TypeWithDefaults();
            Assert.Equal(123, d.GetInt16());
            Assert.Equal(49, d.GetInt16(49));
        }

        [Fact]
        public void DefaultInt32()
        {
            dynamic d = new TypeWithDefaults();
            Assert.Equal(123, d.GetInt32());
            Assert.Equal(49, d.GetInt32(49));
        }

        [Fact]
        public void DefaultInt64()
        {
            dynamic d = new TypeWithDefaults();
            Assert.Equal(123, d.GetInt64());
            Assert.Equal(49, d.GetInt64(49));
        }

        [Fact]
        public void DefaultSingle()
        {
            dynamic d = new TypeWithDefaults();
            Assert.Equal(123, d.GetSingle());
            Assert.Equal(49, d.GetSingle(49));
        }

        [Fact]
        public void DefaultDouble()
        {
            dynamic d = new TypeWithDefaults();
            Assert.Equal(123, d.GetDouble());
            Assert.Equal(49, d.GetDouble(49));
        }

        [Fact]
        public void DefaultChar()
        {
            dynamic d = new TypeWithDefaults();
            Assert.Equal('X', d.GetChar());
            Assert.Equal('!', d.GetChar('!'));
        }

        [Fact]
        public void DefaultBoolean()
        {
            dynamic d = new TypeWithDefaults();
            Assert.True(d.GetBoolean());
            Assert.True(d.GetBoolean(true));
            Assert.False(d.GetBoolean(false));
        }

        [Fact]
        public void DefaultSByte()
        {
            dynamic d = new TypeWithDefaults();
            Assert.Equal(123, d.GetSByte());
            Assert.Equal(49, d.GetSByte(49));
        }

        [Fact]
        public void DefaultUInt16()
        {
            dynamic d = new TypeWithDefaults();
            Assert.Equal(123, d.GetUInt16());
            Assert.Equal(49, d.GetUInt16(49));
        }

        [Fact]
        public void DefaultUInt32()
        {
            dynamic d = new TypeWithDefaults();
            Assert.Equal(123U, d.GetUInt32());
            Assert.Equal(49U, d.GetUInt32(49));
        }

        [Fact]
        public void DefaultUInt64()
        {
            dynamic d = new TypeWithDefaults();
            Assert.Equal(123UL, d.GetUInt64());
            Assert.Equal(49UL, d.GetUInt64(49));
        }

        [Fact]
        public void DefaultString()
        {
            dynamic d = new TypeWithDefaults();
            Assert.Equal("123", d.GetString());
            Assert.Equal("something else", d.GetString("something else"));
        }

        [Fact]
        public void DefaultUri()
        {
            dynamic d = new TypeWithDefaults();
            Assert.Null(d.GetURI());
            Assert.Equal(new Uri("http://example.net/"), d.GetURI(new Uri("http://example.net/")));
        }

        [Fact]
        public void DefaultEnum()
        {
            dynamic d = new TypeWithDefaults();
            Assert.Equal(StringComparison.InvariantCulture, d.GetEnum());
            Assert.Equal(StringComparison.OrdinalIgnoreCase, d.GetEnum(StringComparison.OrdinalIgnoreCase));
        }


        [Fact]
        public void DefaultStruct()
        {
            dynamic d = new TypeWithDefaults();
            KeyValuePair<int, string> kvp = d.GetStruct();
            Assert.Equal(0, kvp.Key);
            Assert.Null(kvp.Value);
            kvp = d.GetStruct(new KeyValuePair<int, string>(23, "value"));
            Assert.Equal(23, kvp.Key);
            Assert.Equal("value", kvp.Value);
        }

        [Fact]
        public void OptionalDateTime()
        {
            dynamic d = new TypeWithOptionalsWithoutDefaults();
            Assert.Equal(default(DateTime), d.GetDate());
            Assert.Equal(new DateTime(9876, 5, 4, 3, 2, 1), d.GetDate(new DateTime(9876, 5, 4, 3, 2, 1)));
        }

        [Fact]
        public void OptionalDecimal()
        {
            dynamic d = new TypeWithOptionalsWithoutDefaults();
            Assert.Equal(0m, d.GetDecimal());
            Assert.Equal(49m, d.GetDecimal(49m));
        }

        [Fact]
        public void OptionalByte()
        {
            dynamic d = new TypeWithOptionalsWithoutDefaults();
            Assert.Equal(0, d.GetByte());
            Assert.Equal(49, d.GetByte(49));
        }

        [Fact]
        public void OptionalInt16()
        {
            dynamic d = new TypeWithOptionalsWithoutDefaults();
            Assert.Equal(0, d.GetInt16());
            Assert.Equal(49, d.GetInt16(49));
        }

        [Fact]
        public void OptionalInt32()
        {
            dynamic d = new TypeWithOptionalsWithoutDefaults();
            Assert.Equal(0, d.GetInt32());
            Assert.Equal(49, d.GetInt32(49));
        }

        [Fact]
        public void OptionalInt64()
        {
            dynamic d = new TypeWithOptionalsWithoutDefaults();
            Assert.Equal(0, d.GetInt64());
            Assert.Equal(49, d.GetInt64(49));
        }

        [Fact]
        public void OptionalSingle()
        {
            dynamic d = new TypeWithOptionalsWithoutDefaults();
            Assert.Equal(0.0f, d.GetSingle());
            Assert.Equal(49, d.GetSingle(49));
        }

        [Fact]
        public void OptionalDouble()
        {
            dynamic d = new TypeWithOptionalsWithoutDefaults();
            Assert.Equal(0.0, d.GetDouble());
            Assert.Equal(49, d.GetDouble(49));
        }

        [Fact]
        public void OptionalChar()
        {
            dynamic d = new TypeWithOptionalsWithoutDefaults();
            Assert.Equal('\0', d.GetChar());
            Assert.Equal('!', d.GetChar('!'));
        }

        [Fact]
        public void OptionalBoolean()
        {
            dynamic d = new TypeWithOptionalsWithoutDefaults();
            Assert.False(d.GetBoolean());
            Assert.True(d.GetBoolean(true));
            Assert.False(d.GetBoolean(false));
        }

        [Fact]
        public void OptionalSByte()
        {
            dynamic d = new TypeWithOptionalsWithoutDefaults();
            Assert.Equal(0, d.GetSByte());
            Assert.Equal(49, d.GetSByte(49));
        }

        [Fact]
        public void OptionalUInt16()
        {
            dynamic d = new TypeWithOptionalsWithoutDefaults();
            Assert.Equal(0, d.GetUInt16());
            Assert.Equal(49, d.GetUInt16(49));
        }

        [Fact]
        public void OptionalUInt32()
        {
            dynamic d = new TypeWithOptionalsWithoutDefaults();
            Assert.Equal(0U, d.GetUInt32());
            Assert.Equal(49U, d.GetUInt32(49));
        }

        [Fact]
        public void OptionalUInt64()
        {
            dynamic d = new TypeWithOptionalsWithoutDefaults();
            Assert.Equal(0UL, d.GetUInt64());
            Assert.Equal(49UL, d.GetUInt64(49));
        }

        [Fact]
        public void OptionalString()
        {
            dynamic d = new TypeWithOptionalsWithoutDefaults();
            Assert.Null(d.GetString());
            Assert.Equal("something else", d.GetString("something else"));
        }

        [Fact]
        public void OptionalUri()
        {
            dynamic d = new TypeWithOptionalsWithoutDefaults();
            Assert.Null(d.GetURI());
            Assert.Equal(new Uri("http://example.net/"), d.GetURI(new Uri("http://example.net/")));
        }

        [Fact]
        public void OptionalEnum()
        {
            dynamic d = new TypeWithOptionalsWithoutDefaults();
            Assert.Equal(default(StringComparison), d.GetEnum());
            Assert.Equal(StringComparison.OrdinalIgnoreCase, d.GetEnum(StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public void OptionalStruct()
        {
            dynamic d = new TypeWithOptionalsWithoutDefaults();
            KeyValuePair<int, string> kvp = d.GetStruct();
            Assert.Equal(0, kvp.Key);
            Assert.Null(kvp.Value);
            kvp = d.GetStruct(new KeyValuePair<int, string>(23, "value"));
            Assert.Equal(23, kvp.Key);
            Assert.Equal("value", kvp.Value);
        }
    }
}
