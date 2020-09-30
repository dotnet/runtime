// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Text.Json.Serialization.Tests
{
    public class SimpleTestClassWithSimpleObject : ITestClass
    {
        public object MyInt16 { get; set; }
        public object MyInt32 { get; set; }
        public object MyInt64 { get; set; }
        public object MyUInt16 { get; set; }
        public object MyUInt32 { get; set; }
        public object MyUInt64 { get; set; }
        public object MyByte { get; set; }
        public object MySByte { get; set; }
        public object MyChar { get; set; }
        public object MyString { get; set; }
        public object MyDecimal { get; set; }
        public object MyBooleanTrue { get; set; }
        public object MyBooleanFalse { get; set; }
        public object MySingle { get; set; }
        public object MyDouble { get; set; }
        public object MyDateTime { get; set; }
        public object MyGuid { get; set; }
        public object MyEnum { get; set; }
        public object MyStruct { get; set; }

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
                @"""MyString"" : ""Hello""," +
                @"""MyBooleanTrue"" : true," +
                @"""MyBooleanFalse"" : false," +
                @"""MySingle"" : 1.1," +
                @"""MyDouble"" : 2.2," +
                @"""MyDecimal"" : 3.3," +
                @"""MyDateTime"" : ""2019-01-30T12:01:02.0000000Z""," +
                @"""MyGuid"" : ""5BB9D872-DA8A-471E-AA70-08E19102683D""," +
                @"""MyEnum"" : 2," + // int by default
                @"""MyStruct"" : { ""One"" : 1, ""Two"" : 3.14 }" +
            @"}";

        public static readonly byte[] s_data = Encoding.UTF8.GetBytes(s_json);

        private bool _initialized;

        public virtual void Initialize()
        {
            _initialized = true;
            MyInt16 = (short)1;
            MyInt32 = (int)2;
            MyInt64 = (long)3;
            MyUInt16 = (ushort)4;
            MyUInt32 = (uint)5;
            MyUInt64 = (ulong)6;
            MyByte = (byte)7;
            MySByte = (sbyte)8;
            MyChar = 'a';
            MyString = "Hello";
            MyBooleanTrue = true;
            MyBooleanFalse = false;
            MySingle = 1.1f;
            MyDouble = 2.2d;
            MyDecimal = 3.3m;
            MyDateTime = new DateTime(2019, 1, 30, 12, 1, 2, DateTimeKind.Utc);
            MyGuid = new Guid("5BB9D872-DA8A-471E-AA70-08E19102683D");
            MyEnum = SampleEnum.Two;
            MyStruct = new SimpleStruct { One = 1, Two = 3.14 };
        }

        public virtual void Verify()
        {
            // Shared test logic verifies state after calling Initialize. In the object
            // case we don't care if the object is initialized with non JsonElement values,
            // they'll still be serialized back in as JsonElement.

            if (_initialized)
                return;

            Assert.IsType<JsonElement>(MyInt16);
            Assert.Equal(JsonValueKind.Number, ((JsonElement)MyInt16).ValueKind);
            Assert.Equal(1, ((JsonElement)MyInt16).GetInt16());
            Assert.IsType<JsonElement>(MyInt32);
            Assert.Equal(JsonValueKind.Number, ((JsonElement)MyInt32).ValueKind);
            Assert.Equal(2, ((JsonElement)MyInt32).GetInt32());
            Assert.IsType<JsonElement>(MyInt64);
            Assert.Equal(JsonValueKind.Number, ((JsonElement)MyInt64).ValueKind);
            Assert.Equal(3L, ((JsonElement)MyInt64).GetInt64());
            Assert.IsType<JsonElement>(MyUInt16);
            Assert.Equal(JsonValueKind.Number, ((JsonElement)MyUInt16).ValueKind);
            Assert.Equal(4u, ((JsonElement)MyUInt16).GetUInt16());
            Assert.IsType<JsonElement>(MyUInt32);
            Assert.Equal(JsonValueKind.Number, ((JsonElement)MyUInt32).ValueKind);
            Assert.Equal(5u, ((JsonElement)MyUInt32).GetUInt32());
            Assert.IsType<JsonElement>(MyUInt64);
            Assert.Equal(JsonValueKind.Number, ((JsonElement)MyUInt64).ValueKind);
            Assert.Equal(6UL, ((JsonElement)MyUInt64).GetUInt64());
            Assert.IsType<JsonElement>(MyByte);
            Assert.Equal(JsonValueKind.Number, ((JsonElement)MyByte).ValueKind);
            Assert.Equal(7, ((JsonElement)MyByte).GetByte());
            Assert.IsType<JsonElement>(MySByte);
            Assert.Equal(JsonValueKind.Number, ((JsonElement)MySByte).ValueKind);
            Assert.Equal(8, ((JsonElement)MySByte).GetSByte());
            Assert.IsType<JsonElement>(MyChar);
            Assert.Equal(JsonValueKind.String, ((JsonElement)MyChar).ValueKind);
            Assert.Equal("a", ((JsonElement)MyChar).GetString());
            Assert.IsType<JsonElement>(MyString);
            Assert.Equal(JsonValueKind.String, ((JsonElement)MyString).ValueKind);
            Assert.Equal("Hello", ((JsonElement)MyString).GetString());
            Assert.IsType<JsonElement>(MyDecimal);
            Assert.Equal(JsonValueKind.Number, ((JsonElement)MyDecimal).ValueKind);
            Assert.Equal(3.3m, ((JsonElement)MyDecimal).GetDecimal());
            Assert.IsType<JsonElement>(MyBooleanFalse);
            Assert.Equal(JsonValueKind.False, ((JsonElement)MyBooleanFalse).ValueKind);
            Assert.False(((JsonElement)MyBooleanFalse).GetBoolean());
            Assert.IsType<JsonElement>(MyBooleanTrue);
            Assert.Equal(JsonValueKind.True, ((JsonElement)MyBooleanTrue).ValueKind);
            Assert.True(((JsonElement)MyBooleanTrue).GetBoolean());
            Assert.IsType<JsonElement>(MySingle);
            Assert.Equal(JsonValueKind.Number, ((JsonElement)MySingle).ValueKind);
            Assert.Equal(1.1f, ((JsonElement)MySingle).GetSingle());
            Assert.IsType<JsonElement>(MyDouble);
            Assert.Equal(JsonValueKind.Number, ((JsonElement)MyDouble).ValueKind);
            Assert.Equal(2.2d, ((JsonElement)MyDouble).GetDouble());
            Assert.IsType<JsonElement>(MyDateTime);
            Assert.Equal(JsonValueKind.String, ((JsonElement)MyDateTime).ValueKind);
            Assert.Equal(new DateTime(2019, 1, 30, 12, 1, 2, DateTimeKind.Utc), ((JsonElement)MyDateTime).GetDateTime());
            Assert.IsType<JsonElement>(MyGuid);
            Assert.Equal(JsonValueKind.String, ((JsonElement)MyGuid).ValueKind);
            Assert.Equal(new Guid("5BB9D872-DA8A-471E-AA70-08E19102683D"), ((JsonElement)MyGuid).GetGuid());
            Assert.IsType<JsonElement>(MyEnum);
            Assert.Equal(JsonValueKind.Number, ((JsonElement)MyEnum).ValueKind);
            Assert.Equal(SampleEnum.Two, (SampleEnum)((JsonElement)MyEnum).GetUInt32());
            Assert.Equal(1, ((JsonElement)MyStruct).GetProperty("One").GetInt32());
            Assert.Equal(3.14, ((JsonElement)MyStruct).GetProperty("Two").GetDouble());
        }
    }
}
