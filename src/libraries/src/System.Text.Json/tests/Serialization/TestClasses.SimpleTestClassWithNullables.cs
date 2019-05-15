// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.Text.Json.Serialization.Tests
{
    public abstract class SimpleBaseClassWithNullables
    {
        public short? MyInt16 { get; set; }
        public int? MyInt32 { get; set; }
        public long? MyInt64 { get; set; }
        public ushort? MyUInt16 { get; set; }
        public uint? MyUInt32 { get; set; }
        public ulong? MyUInt64 { get; set; }
        public byte? MyByte { get; set; }
        public sbyte? MySByte { get; set; }
        public char? MyChar { get; set; }
        public decimal? MyDecimal { get; set; }
        public bool? MyBooleanTrue { get; set; }
        public bool? MyBooleanFalse { get; set; }
        public float? MySingle { get; set; }
        public double? MyDouble { get; set; }
        public DateTime? MyDateTime { get; set; }
        public DateTimeOffset? MyDateTimeOffset { get; set; }
        public Guid? MyGuid { get; set; }
        public SampleEnum? MyEnum { get; set; }
        public short?[] MyInt16Array { get; set; }
        public int?[] MyInt32Array { get; set; }
        public long?[] MyInt64Array { get; set; }
        public ushort?[] MyUInt16Array { get; set; }
        public uint?[] MyUInt32Array { get; set; }
        public ulong?[] MyUInt64Array { get; set; }
        public byte?[] MyByteArray { get; set; }
        public sbyte?[] MySByteArray { get; set; }
        public char?[] MyCharArray { get; set; }
        public decimal?[] MyDecimalArray { get; set; }
        public bool?[] MyBooleanTrueArray { get; set; }
        public bool?[] MyBooleanFalseArray { get; set; }
        public float?[] MySingleArray { get; set; }
        public double?[] MyDoubleArray { get; set; }
        public DateTime?[] MyDateTimeArray { get; set; }
        public DateTimeOffset?[] MyDateTimeOffsetArray { get; set; }
        public Guid?[] MyGuidArray { get; set; }
        public SampleEnum?[] MyEnumArray { get; set; }
        public Dictionary<string, string> MyStringToStringDict { get; set; }
        public List<int?> MyListOfNullInt { get; set; }
    }

    public class SimpleTestClassWithNulls : SimpleBaseClassWithNullables, ITestClass
    {
        public void Initialize()
        {
        }

        public void Verify()
        {
            Assert.Null(MyInt16);
            Assert.Null(MyInt32);
            Assert.Null(MyInt64);
            Assert.Null(MyUInt16);
            Assert.Null(MyUInt32);
            Assert.Null(MyUInt64);
            Assert.Null(MyByte);
            Assert.Null(MySByte);
            Assert.Null(MyChar);
            Assert.Null(MyDecimal);
            Assert.Null(MyBooleanFalse);
            Assert.Null(MyBooleanTrue);
            Assert.Null(MySingle);
            Assert.Null(MyDouble);
            Assert.Null(MyDateTime);
            Assert.Null(MyDateTimeOffset);
            Assert.Null(MyGuid);
            Assert.Null(MyEnum);

            Assert.Null(MyInt16Array);
            Assert.Null(MyInt32Array);
            Assert.Null(MyInt64Array);
            Assert.Null(MyUInt16Array);
            Assert.Null(MyUInt32Array);
            Assert.Null(MyUInt64Array);
            Assert.Null(MyByteArray);
            Assert.Null(MySByteArray);
            Assert.Null(MyCharArray);
            Assert.Null(MyDecimalArray);
            Assert.Null(MyBooleanFalseArray);
            Assert.Null(MyBooleanTrueArray);
            Assert.Null(MySingleArray);
            Assert.Null(MyDoubleArray);
            Assert.Null(MyDateTimeArray);
            Assert.Null(MyDateTimeOffsetArray);
            Assert.Null(MyGuidArray);
            Assert.Null(MyEnumArray);
            Assert.Null(MyStringToStringDict);
            Assert.Null(MyListOfNullInt);
        }
        public static readonly string s_json =
                @"{" +
                @"""MyInt16"" : null," +
                @"""MyInt32"" : null," +
                @"""MyInt64"" : null," +
                @"""MyUInt16"" : null," +
                @"""MyUInt32"" : null," +
                @"""MyUInt64"" : null," +
                @"""MyByte"" : null," +
                @"""MySByte"" : null," +
                @"""MyChar"" : null," +
                @"""MyBooleanTrue"" : null," +
                @"""MyBooleanFalse"" : null," +
                @"""MySingle"" : null," +
                @"""MyDouble"" : null," +
                @"""MyDecimal"" : null," +
                @"""MyDateTime"" : null," +
                @"""MyDateTimeOffset"" : null," +
                @"""MyGuid"" : null," +
                @"""MyEnum"" : null," +
                @"""MyInt16Array"" : null," +
                @"""MyInt32Array"" : null," +
                @"""MyInt64Array"" : null," +
                @"""MyUInt16Array"" : null," +
                @"""MyUInt32Array"" : null," +
                @"""MyUInt64Array"" : null," +
                @"""MyByteArray"" : null," +
                @"""MySByteArray"" : null," +
                @"""MyCharArray"" : null," +
                @"""MyBooleanTrueArray"" : null," +
                @"""MyBooleanFalseArray"" : null," +
                @"""MySingleArray"" : null," +
                @"""MyDoubleArray"" : null," +
                @"""MyDecimalArray"" : null," +
                @"""MyDateTimeArray"" : null," +
                @"""MyDateTimeOffsetArray"" : null," +
                @"""MyEnumArray"" : null," +
                @"""MyStringToStringDict"" : null," +
                @"""MyListOfNullInt"" : null" +
                @"}";

        public static readonly byte[] s_data = Encoding.UTF8.GetBytes(s_json);
    }

    public class SimpleTestClassWithNullables : SimpleBaseClassWithNullables, ITestClass
    {
        public static readonly string s_json =
                @"{" +
                @"""MyInt16"" : 1," +
                @"""MyInt32"" : 2," +
                @"""MyInt64"" : 3," +
                @"""MyUInt16"" : 4," +
                @"""MyUInt32"" : 5," +
                @"""MyUInt64"" : 6," +
                @"""MyByte"" : 7," +
                @"""MySByte"" : 8," +
                @"""MyChar"" : ""a""," +
                @"""MyBooleanTrue"" : true," +
                @"""MyBooleanFalse"" : false," +
                @"""MySingle"" : 1.1," +
                @"""MyDouble"" : 2.2," +
                @"""MyDecimal"" : 3.3," +
                @"""MyDateTime"" : ""2019-01-30T12:01:02.0000000Z""," +
                @"""MyDateTimeOffset"" : ""2019-01-30T12:01:02.0000000+01:00""," +
                @"""MyGuid"" : ""1B33498A-7B7D-4DDA-9C13-F6AA4AB449A6""," +
                @"""MyEnum"" : 2," +
                @"""MyInt16Array"" : [1]," +
                @"""MyInt32Array"" : [2]," +
                @"""MyInt64Array"" : [3]," +
                @"""MyUInt16Array"" : [4]," +
                @"""MyUInt32Array"" : [5]," +
                @"""MyUInt64Array"" : [6]," +
                @"""MyByteArray"" : [7]," +
                @"""MySByteArray"" : [8]," +
                @"""MyCharArray"" : [""a""]," +
                @"""MyBooleanTrueArray"" : [true]," +
                @"""MyBooleanFalseArray"" : [false]," +
                @"""MySingleArray"" : [1.1]," +
                @"""MyDoubleArray"" : [2.2]," +
                @"""MyDecimalArray"" : [3.3]," +
                @"""MyDateTimeArray"" : [""2019-01-30T12:01:02.0000000Z""]," +
                @"""MyDateTimeOffsetArray"" : [""2019-01-30T12:01:02.0000000+01:00""]," +
                @"""MyGuidArray"" : [""1B33498A-7B7D-4DDA-9C13-F6AA4AB449A6""]," +
                @"""MyEnumArray"" : [2]," +
                @"""MyStringToStringDict"" : {""key"" : ""value""}," +
                @"""MyListOfNullInt"" : [null]" +
                @"}";

        public static readonly byte[] s_data = Encoding.UTF8.GetBytes(s_json);

        public void Initialize()
        {
            MyInt16 = 1;
            MyInt32 = 2;
            MyInt64 = 3;
            MyUInt16 = 4;
            MyUInt32 = 5;
            MyUInt64 = 6;
            MyByte = 7;
            MySByte = 8;
            MyChar = 'a';
            MyBooleanTrue = true;
            MyBooleanFalse = false;
            MySingle = 1.1f;
            MyDouble = 2.2d;
            MyDecimal = 3.3m;
            MyDateTime = new DateTime(2019, 1, 30, 12, 1, 2, DateTimeKind.Utc);
            MyDateTimeOffset = new DateTimeOffset(2019, 1, 30, 12, 1, 2, new TimeSpan(1, 0, 0));
            MyGuid = new Guid("1B33498A-7B7D-4DDA-9C13-F6AA4AB449A6");
            MyEnum = SampleEnum.Two;

            MyInt16Array = new short?[] { 1 };
            MyInt32Array = new int?[] { 2 };
            MyInt64Array = new long?[] { 3 };
            MyUInt16Array = new ushort?[] { 4 };
            MyUInt32Array = new uint?[] { 5 };
            MyUInt64Array = new ulong?[] { 6 };
            MyByteArray = new byte?[] { 7 };
            MySByteArray = new sbyte?[] { 8 };
            MyCharArray = new char?[] { 'a' };
            MyBooleanTrueArray = new bool?[] { true };
            MyBooleanFalseArray = new bool?[] { false };
            MySingleArray = new float?[] { 1.1f };
            MyDoubleArray = new double?[] { 2.2d };
            MyDecimalArray = new decimal?[] { 3.3m };
            MyDateTimeArray = new DateTime?[] { new DateTime(2019, 1, 30, 12, 1, 2, DateTimeKind.Utc) };
            MyDateTimeOffsetArray = new DateTimeOffset?[] { new DateTimeOffset(2019, 1, 30, 12, 1, 2, new TimeSpan(1, 0, 0)) };
            MyGuidArray = new Guid?[] { new Guid("1B33498A-7B7D-4DDA-9C13-F6AA4AB449A6") };
            MyEnumArray = new SampleEnum?[] { SampleEnum.Two };
            MyStringToStringDict = new Dictionary<string, string> { { "key", "value" } };
            MyListOfNullInt = new List<int?> { null };
        }

        public void Verify()
        {
            Assert.Equal(MyInt16, (short)1);
            Assert.Equal(MyInt32, (int)2);
            Assert.Equal(MyInt64, (long)3);
            Assert.Equal(MyUInt16, (ushort)4);
            Assert.Equal(MyUInt32, (uint)5);
            Assert.Equal(MyUInt64, (ulong)6);
            Assert.Equal(MyByte, (byte)7);
            Assert.Equal(MySByte, (sbyte)8);
            Assert.Equal(MyChar, 'a');
            Assert.Equal(MyDecimal, 3.3m);
            Assert.Equal(MyBooleanFalse, false);
            Assert.Equal(MyBooleanTrue, true);
            Assert.Equal(MySingle, 1.1f);
            Assert.Equal(MyDouble, 2.2d);
            Assert.Equal(MyDateTime, new DateTime(2019, 1, 30, 12, 1, 2, DateTimeKind.Utc));
            Assert.Equal(MyDateTimeOffset, new DateTimeOffset(2019, 1, 30, 12, 1, 2, new TimeSpan(1, 0, 0)));
            Assert.Equal(MyGuid, new Guid("1B33498A-7B7D-4DDA-9C13-F6AA4AB449A6"));
            Assert.Equal(MyEnum, SampleEnum.Two);

            Assert.Equal((short)1, MyInt16Array[0]);
            Assert.Equal((int)2, MyInt32Array[0]);
            Assert.Equal((long)3, MyInt64Array[0]);
            Assert.Equal((ushort)4, MyUInt16Array[0]);
            Assert.Equal((uint)5, MyUInt32Array[0]);
            Assert.Equal((ulong)6, MyUInt64Array[0]);
            Assert.Equal((byte)7, MyByteArray[0]);
            Assert.Equal((sbyte)8, MySByteArray[0]);
            Assert.Equal('a', MyCharArray[0]);
            Assert.Equal(3.3m, MyDecimalArray[0]);
            Assert.Equal(false, MyBooleanFalseArray[0]);
            Assert.Equal(true, MyBooleanTrueArray[0]);
            Assert.Equal(1.1f, MySingleArray[0]);
            Assert.Equal(2.2d, MyDoubleArray[0]);
            Assert.Equal(new DateTime(2019, 1, 30, 12, 1, 2, DateTimeKind.Utc), MyDateTimeArray[0]);
            Assert.Equal(new DateTimeOffset(2019, 1, 30, 12, 1, 2, new TimeSpan(1, 0, 0)), MyDateTimeOffsetArray[0]);
            Assert.Equal(new Guid("1B33498A-7B7D-4DDA-9C13-F6AA4AB449A6"), MyGuidArray[0]);
            Assert.Equal(SampleEnum.Two, MyEnumArray[0]);
            Assert.Equal("value", MyStringToStringDict["key"]);
            Assert.Null(MyListOfNullInt[0]);
        }
    }
}
