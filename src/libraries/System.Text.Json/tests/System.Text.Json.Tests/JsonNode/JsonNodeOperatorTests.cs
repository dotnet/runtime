// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Text.Json.Node.Tests
{
    public static class OperatorTests
    {
        private const string ExpectedPrimitiveJson =
                @"{" +
                @"""MyInt16"":1," +
                @"""MyInt32"":2," +
                @"""MyInt64"":3," +
                @"""MyUInt16"":4," +
                @"""MyUInt32"":5," +
                @"""MyUInt64"":6," +
                @"""MyByte"":7," +
                @"""MySByte"":8," +
                @"""MyChar"":""a""," +
                @"""MyString"":""Hello""," +
                @"""MyBooleanTrue"":true," +
                @"""MyBooleanFalse"":false," +
                @"""MySingle"":1.1," +
                @"""MyDouble"":2.2," +
                @"""MyDecimal"":3.3," +
                @"""MyDateTime"":""2019-01-30T12:01:02Z""," +
                @"""MyDateTimeOffset"":""2019-01-30T12:01:02+01:00""," +
                @"""MyGuid"":""1b33498a-7b7d-4dda-9c13-f6aa4ab449a6""" + // note lowercase
                @"}";

        [Fact]
        public static void ImplicitOperators_FromProperties()
        {
            var jObject = new JsonObject();
            jObject["MyInt16"] = (short)1;
            jObject["MyInt32"] = 2;
            jObject["MyInt64"] = (long)3;
            jObject["MyUInt16"] = (ushort)4;
            jObject["MyUInt32"] = (uint)5;
            jObject["MyUInt64"] = (ulong)6;
            jObject["MyByte"] = (byte)7;
            jObject["MySByte"] = (sbyte)8;
            jObject["MyChar"] = 'a';
            jObject["MyString"] = "Hello";
            jObject["MyBooleanTrue"] = true;
            jObject["MyBooleanFalse"] = false;
            jObject["MySingle"] = 1.1f;
            jObject["MyDouble"] = 2.2d;
            jObject["MyDecimal"] = 3.3m;
            jObject["MyDateTime"] = new DateTime(2019, 1, 30, 12, 1, 2, DateTimeKind.Utc);
            jObject["MyDateTimeOffset"] = new DateTimeOffset(2019, 1, 30, 12, 1, 2, new TimeSpan(1, 0, 0));
            jObject["MyGuid"] = new Guid("1B33498A-7B7D-4DDA-9C13-F6AA4AB449A6");

            string expected = ExpectedPrimitiveJson;

            string json = jObject.ToJsonString();

            // Adjust for non-Core frameworks which do not have round-trippable floating point strings.
            json = json.Replace("1.10000002", "1.1").Replace("2.2000000000000002", "2.2");

            Assert.Equal(expected, json);
        }

        [Fact]
        public static void ExplicitOperators_FromProperties()
        {
            JsonObject jObject = JsonNode.Parse(ExpectedPrimitiveJson).AsObject();
            Assert.Equal(1, (short)jObject["MyInt16"]);
            Assert.Equal(2, (int)jObject["MyInt32"]);
            Assert.Equal(3, (long)jObject["MyInt64"]);
            Assert.Equal(4, (ushort)jObject["MyUInt16"]);
            Assert.Equal<uint>(5, (uint)jObject["MyUInt32"]);
            Assert.Equal<ulong>(6, (ulong)jObject["MyUInt64"]);
            Assert.Equal(7, (byte)jObject["MyByte"]);
            Assert.Equal(8, (sbyte)jObject["MySByte"]);
            Assert.Equal('a', (char)jObject["MyChar"]);
            Assert.Equal("Hello", (string)jObject["MyString"]);
            Assert.True((bool)jObject["MyBooleanTrue"]);
            Assert.False((bool)jObject["MyBooleanFalse"]);
            Assert.Equal(1.1f, (float)jObject["MySingle"]);
            Assert.Equal(2.2d, (double)jObject["MyDouble"]);
            Assert.Equal(3.3m, (decimal)jObject["MyDecimal"]);
            Assert.Equal(new DateTime(2019, 1, 30, 12, 1, 2, DateTimeKind.Utc), (DateTime)jObject["MyDateTime"]);
            Assert.Equal(new DateTimeOffset(2019, 1, 30, 12, 1, 2, new TimeSpan(1, 0, 0)), (DateTimeOffset)jObject["MyDateTimeOffset"]);
            Assert.Equal(new Guid("1B33498A-7B7D-4DDA-9C13-F6AA4AB449A6"), (Guid)jObject["MyGuid"]);
        }

        [Fact]
        public static void ExplicitOperators_FromValues()
        {
            Assert.Equal(1, (short)(JsonNode)(short)1);
            Assert.Equal(2, (int)(JsonNode)2);
            Assert.Equal(3, (long)(JsonNode)(long)3);
            Assert.Equal(4, (ushort)(JsonNode)(ushort)4);
            Assert.Equal<uint>(5, (uint)(JsonNode)(uint)5);
            Assert.Equal<ulong>(6, (ulong)(JsonNode)(ulong)6);
            Assert.Equal(7, (byte)(JsonNode)(byte)7);
            Assert.Equal(8, (sbyte)(JsonNode)(sbyte)8);
            Assert.Equal('a', (char)(JsonNode)'a');
            Assert.Equal("Hello", (string)(JsonNode)"Hello");
            Assert.True((bool)(JsonNode)true);
            Assert.False((bool)(JsonNode)false);
            Assert.Equal(1.1f, (float)(JsonNode)1.1f);
            Assert.Equal(2.2d, (double)(JsonNode)2.2d);
            Assert.Equal(3.3m, (decimal)(JsonNode)3.3m);
            Assert.Equal(new DateTime(2019, 1, 30, 12, 1, 2, DateTimeKind.Utc),
                (DateTime)(JsonNode)new DateTime(2019, 1, 30, 12, 1, 2, DateTimeKind.Utc));
            Assert.Equal(new DateTimeOffset(2019, 1, 30, 12, 1, 2, new TimeSpan(1, 0, 0)),
                (DateTimeOffset)(JsonNode)new DateTimeOffset(2019, 1, 30, 12, 1, 2, new TimeSpan(1, 0, 0)));
            Assert.Equal(new Guid("1B33498A-7B7D-4DDA-9C13-F6AA4AB449A6"),
                (Guid)(JsonNode)new Guid("1B33498A-7B7D-4DDA-9C13-F6AA4AB449A6"));
        }

        [Fact]
        public static void ExplicitOperators_FromNullValues()
        {
            Assert.Null((byte?)(JsonValue)null);
            Assert.Null((short?)(JsonValue)null);
            Assert.Null((int?)(JsonValue)null);
            Assert.Null((long?)(JsonValue)null);
            Assert.Null((sbyte?)(JsonValue)null);
            Assert.Null((ushort?)(JsonValue)null);
            Assert.Null((uint?)(JsonValue)null);
            Assert.Null((ulong?)(JsonValue)null);
            Assert.Null((char?)(JsonValue)null);
            Assert.Null((string)(JsonValue)null);
            Assert.Null((bool?)(JsonValue)null);
            Assert.Null((float?)(JsonValue)null);
            Assert.Null((double?)(JsonValue)null);
            Assert.Null((decimal?)(JsonValue)null);
            Assert.Null((DateTime?)(JsonValue)null);
            Assert.Null((DateTimeOffset?)(JsonValue)null);
            Assert.Null((Guid?)(JsonValue)null);
        }

        [Fact]
        public static void ExplicitOperators_FromNullableValues()
        {
            Assert.NotNull((byte?)(JsonValue)(byte)42);
            Assert.NotNull((short?)(JsonValue)(short)42);
            Assert.NotNull((int?)(JsonValue)42);
            Assert.NotNull((long?)(JsonValue)(long)42);
            Assert.NotNull((sbyte?)(JsonValue)(sbyte)42);
            Assert.NotNull((ushort?)(JsonValue)(ushort)42);
            Assert.NotNull((uint?)(JsonValue)(uint)42);
            Assert.NotNull((ulong?)(JsonValue)(ulong)42);
            Assert.NotNull((char?)(JsonValue)'a');
            Assert.NotNull((string)(JsonValue)"");
            Assert.NotNull((bool?)(JsonValue)true);
            Assert.NotNull((float?)(JsonValue)(float)42);
            Assert.NotNull((double?)(JsonValue)(double)42);
            Assert.NotNull((decimal?)(JsonValue)(decimal)42);
            Assert.NotNull((DateTime?)(JsonValue)new DateTime(2019, 1, 30, 12, 1, 2, DateTimeKind.Utc));
            Assert.NotNull((DateTimeOffset?)(JsonValue)new DateTimeOffset(2019, 1, 30, 12, 1, 2, new TimeSpan(1, 0, 0)));
            Assert.NotNull((Guid?)(JsonValue)new Guid("1B33498A-7B7D-4DDA-9C13-F6AA4AB449A6"));
        }

        [Fact]
        public static void ImplicitOperators_FromNullValues()
        {
            Assert.Null((JsonValue?)(byte?)null);
            Assert.Null((JsonValue?)(short?)null);
            Assert.Null((JsonValue?)(int?)null);
            Assert.Null((JsonValue?)(long?)null);
            Assert.Null((JsonValue?)(sbyte?)null);
            Assert.Null((JsonValue?)(ushort?)null);
            Assert.Null((JsonValue?)(uint?)null);
            Assert.Null((JsonValue?)(ulong?)null);
            Assert.Null((JsonValue?)(char?)null);
            Assert.Null((JsonValue?)(string)null);
            Assert.Null((JsonValue?)(bool?)null);
            Assert.Null((JsonValue?)(float?)null);
            Assert.Null((JsonValue?)(double?)null);
            Assert.Null((JsonValue?)(decimal?)null);
            Assert.Null((JsonValue?)(DateTime?)null);
            Assert.Null((JsonValue?)(DateTimeOffset?)null);
            Assert.Null((JsonValue?)(Guid?)null);
        }

        [Fact]
        public static void ImplicitOperators_FromNullableValues()
        {
            Assert.NotNull((JsonValue?)(byte?)42);
            Assert.NotNull((JsonValue?)(short?)42);
            Assert.NotNull((JsonValue?)(int?)42);
            Assert.NotNull((JsonValue?)(long?)42);
            Assert.NotNull((JsonValue?)(sbyte?)42);
            Assert.NotNull((JsonValue?)(ushort?)42);
            Assert.NotNull((JsonValue?)(uint?)42);
            Assert.NotNull((JsonValue?)(ulong?)42);
            Assert.NotNull((JsonValue?)(char?)'a');
            Assert.NotNull((JsonValue?)(bool?)true);
            Assert.NotNull((JsonValue?)(float?)42);
            Assert.NotNull((JsonValue?)(double?)42);
            Assert.NotNull((JsonValue?)(decimal?)42);
            Assert.NotNull((JsonValue?)(DateTime?)new DateTime(2019, 1, 30, 12, 1, 2, DateTimeKind.Utc));
            Assert.NotNull((JsonValue?)(DateTimeOffset?)new DateTimeOffset(2019, 1, 30, 12, 1, 2, new TimeSpan(1, 0, 0)));
            Assert.NotNull((JsonValue?)(Guid?)new Guid("1B33498A-7B7D-4DDA-9C13-F6AA4AB449A6"));
        }

        [Fact]
        public static void CastsNotSupported()
        {
            // Since generics and boxing do not support casts, we get InvalidCastExceptions here.
            InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() => (byte)(JsonNode)(long)3); // narrowing
            // "A value of type 'System.Int64' cannot be converted to a 'System.Byte'."
            Assert.Contains(typeof(long).ToString(), ex.Message);
            Assert.Contains(typeof(byte).ToString(), ex.Message);

            ex = Assert.Throws<InvalidOperationException>(() => (long)(JsonNode)(byte)3); // widening
            // "A value of type 'System.Byte' cannot be converted to a 'System.Int64'."
            Assert.Contains(typeof(byte).ToString(), ex.Message);
            Assert.Contains(typeof(long).ToString(), ex.Message);
        }

        [Fact]
        public static void Boxing()
        {
            var node = JsonValue.Create(42);
            Assert.Equal(42, node.GetValue<int>());

            Assert.Equal<object>(42, node.GetValue<object>());
        }
    }
}
