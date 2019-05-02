// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Text.Json.Serialization.Tests
{
    public class SimpleTestClassWithObjectArrays : ITestClass
    {
        public object[] MyInt16 { get; set; }
        public object[] MyInt32 { get; set; }
        public object[] MyInt64 { get; set; }
        public object[] MyUInt16 { get; set; }
        public object[] MyUInt32 { get; set; }
        public object[] MyUInt64 { get; set; }
        public object[] MyByte { get; set; }
        public object[] MySByte { get; set; }
        public object[] MyChar { get; set; }
        public object[] MyString { get; set; }
        public object[] MyDecimal { get; set; }
        public object[] MyBooleanTrue { get; set; }
        public object[] MyBooleanFalse { get; set; }
        public object[] MySingle { get; set; }
        public object[] MyDouble { get; set; }
        public object[] MyDateTime { get; set; }
        public object[] MyEnum { get; set; }

        public static readonly string s_json =
                @"{" +
                @"""MyInt16"" : [1]," +
                @"""MyInt32"" : [2]," +
                @"""MyInt64"" : [3]," +
                @"""MyUInt16"" : [4]," +
                @"""MyUInt32"" : [5]," +
                @"""MyUInt64"" : [6]," +
                @"""MyByte"" : [7]," +
                @"""MySByte"" : [8]," +
                @"""MyChar"" : [""a""]," +
                @"""MyString"" : [""Hello""]," +
                @"""MyBooleanTrue"" : [true]," +
                @"""MyBooleanFalse"" : [false]," +
                @"""MySingle"" : [1.1]," +
                @"""MyDouble"" : [2.2]," +
                @"""MyDecimal"" : [3.3]," +
                @"""MyDateTime"" : [""2019-01-30T12:01:02.0000000Z""]," +
                @"""MyEnum"" : [2]" + // int by default
            @"}";

        public static readonly byte[] s_data = Encoding.UTF8.GetBytes(s_json);

        private bool _initialized;

        public void Initialize()
        {
            _initialized = true;

            MyInt16 = new object[] { (short)1 };
            MyInt32 = new object[] { (int)2 };
            MyInt64 = new object[] { (long)3 };
            MyUInt16 = new object[] { (ushort)4 };
            MyUInt32 = new object[] { (uint)5 };
            MyUInt64 = new object[] { (ulong)6 };
            MyByte = new object[] { (byte)7 };
            MySByte = new object[] { (sbyte)8 };
            MyChar = new object[] { 'a' };
            MyString = new object[] { "Hello" };
            MyBooleanTrue = new object[] { true };
            MyBooleanFalse = new object[] { false };
            MySingle = new object[] { 1.1f };
            MyDouble = new object[] { 2.2d };
            MyDecimal = new object[] { 3.3m };
            MyDateTime = new object[] { new DateTime(2019, 1, 30, 12, 1, 2, DateTimeKind.Utc) };
            MyEnum = new object[] { SampleEnum.Two };
        }

        public void Verify()
        {
            // Shared test logic verifies state after calling Initialize. In the object
            // case we don't care if the object is initialized with non JsonElement values,
            // they'll still be serialized back in as JsonElement.

            if (_initialized)
                return;

            Assert.IsType<JsonElement>(MyInt16[0]);
            Assert.Equal(JsonValueType.Number, ((JsonElement)MyInt16[0]).Type);
            Assert.Equal(1, ((JsonElement)MyInt16[0]).GetInt32());
            Assert.IsType<JsonElement>(MyInt32[0]);
            Assert.Equal(JsonValueType.Number, ((JsonElement)MyInt32[0]).Type);
            Assert.Equal(2, ((JsonElement)MyInt32[0]).GetInt32());
            Assert.IsType<JsonElement>(MyInt64[0]);
            Assert.Equal(JsonValueType.Number, ((JsonElement)MyInt64[0]).Type);
            Assert.Equal(3L, ((JsonElement)MyInt64[0]).GetInt64());
            Assert.IsType<JsonElement>(MyUInt16[0]);
            Assert.Equal(JsonValueType.Number, ((JsonElement)MyUInt16[0]).Type);
            Assert.Equal(4u, ((JsonElement)MyUInt16[0]).GetUInt32());
            Assert.IsType<JsonElement>(MyUInt32[0]);
            Assert.Equal(JsonValueType.Number, ((JsonElement)MyUInt32[0]).Type);
            Assert.Equal(5u, ((JsonElement)MyUInt32[0]).GetUInt32());
            Assert.IsType<JsonElement>(MyUInt64[0]);
            Assert.Equal(JsonValueType.Number, ((JsonElement)MyUInt64[0]).Type);
            Assert.Equal(6UL, ((JsonElement)MyUInt64[0]).GetUInt32());
            Assert.IsType<JsonElement>(MyByte[0]);
            Assert.Equal(JsonValueType.Number, ((JsonElement)MyByte[0]).Type);
            Assert.Equal(7, ((JsonElement)MyByte[0]).GetInt32());
            Assert.IsType<JsonElement>(MySByte[0]);
            Assert.Equal(JsonValueType.Number, ((JsonElement)MySByte[0]).Type);
            Assert.Equal((byte)8, ((JsonElement)MySByte[0]).GetUInt32());
            Assert.IsType<JsonElement>(MyChar[0]);
            Assert.Equal(JsonValueType.String, ((JsonElement)MyChar[0]).Type);
            Assert.Equal("a", ((JsonElement)MyChar[0]).GetString());
            Assert.IsType<JsonElement>(MyString[0]);
            Assert.Equal(JsonValueType.String, ((JsonElement)MyString[0]).Type);
            Assert.Equal("Hello", ((JsonElement)MyString[0]).GetString());
            Assert.IsType<JsonElement>(MyDecimal[0]);
            Assert.Equal(JsonValueType.Number, ((JsonElement)MyDecimal[0]).Type);
            Assert.Equal(3.3m, ((JsonElement)MyDecimal[0]).GetDecimal());
            Assert.IsType<JsonElement>(MyBooleanFalse[0]);
            Assert.Equal(JsonValueType.False, ((JsonElement)MyBooleanFalse[0]).Type);
            Assert.Equal(false, ((JsonElement)MyBooleanFalse[0]).GetBoolean());
            Assert.IsType<JsonElement>(MyBooleanTrue[0]);
            Assert.Equal(JsonValueType.True, ((JsonElement)MyBooleanTrue[0]).Type);
            Assert.Equal(true, ((JsonElement)MyBooleanTrue[0]).GetBoolean());
            Assert.IsType<JsonElement>(MySingle[0]);
            Assert.Equal(JsonValueType.Number, ((JsonElement)MySingle[0]).Type);
            Assert.Equal(1.1f, ((JsonElement)MySingle[0]).GetSingle());
            Assert.IsType<JsonElement>(MyDouble[0]);
            Assert.Equal(JsonValueType.Number, ((JsonElement)MyDouble[0]).Type);
            Assert.Equal(2.2d, ((JsonElement)MyDouble[0]).GetDouble());
            Assert.IsType<JsonElement>(MyDateTime[0]);
            Assert.Equal(JsonValueType.String, ((JsonElement)MyDateTime[0]).Type);
            Assert.Equal(new DateTime(2019, 1, 30, 12, 1, 2, DateTimeKind.Utc), ((JsonElement)MyDateTime[0]).GetDateTime());
            Assert.IsType<JsonElement>(MyEnum[0]);
            Assert.Equal(JsonValueType.Number, ((JsonElement)MyEnum[0]).Type);
            Assert.Equal(SampleEnum.Two, (SampleEnum)((JsonElement)MyEnum[0]).GetUInt32());
        }
    }
}
