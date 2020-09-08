// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using Xunit;

namespace System.Text.Json.Serialization.Tests
{
    public static class DynamicTests
    {
        public const string Json =
            "{\"MyString\":\"Hello\",\"MyNull\":null,\"MyBoolean\":true,\"MyArray\":[1,2],\"MyInt\":42,\"MyDouble\":4.2,\"MyDateTime\":\"2020-07-08T00:00:00\",\"MyGuid\":\"ed957609-cdfe-412f-88c1-02daca1b4f51\",\"MyObject\":{\"MyString\":\"World\"}}";
        public static DateTime MyDateTime => new DateTime(2020, 7, 8);
        public static Guid MyGuid => new Guid("ed957609-cdfe-412f-88c1-02daca1b4f51");

        internal static ExpandoObject GetExpandoObject()
        {
            dynamic myDynamicChild = new ExpandoObject();
            myDynamicChild.MyString = "World";

            dynamic myDynamic = new ExpandoObject();
            myDynamic.MyString = "Hello";
            myDynamic.MyNull = null;
            myDynamic.MyBoolean = true;
            myDynamic.MyArray = new List<int>() { 1, 2 };
            myDynamic.MyInt = 42;
            myDynamic.MyDouble = 4.2;
            myDynamic.MyDateTime = MyDateTime;
            myDynamic.MyGuid = MyGuid;
            myDynamic.MyObject = myDynamicChild;

            // Verify basic dynamic support.
            int c = myDynamic.MyInt;
            Assert.Equal(42, c);

            return myDynamic;
        }

        [Fact]
        public static void DynamicKeyword()
        {
            dynamic myDynamic = GetExpandoObject();

            // STJ can serialize with ExpandoObject + 'dynamic' keyword.
            string json = JsonSerializer.Serialize<dynamic>(myDynamic);
            Assert.Equal(Json, json);

            dynamic d = JsonSerializer.Deserialize<dynamic>(json);

            try
            {
                // We will get an exception here if we try to access a dynamic property since 'object' is deserialized
                // as a JsonElement and not an ExpandoObject.
                int c = d.MyInt;
                Assert.True(false, "Should have thrown Exception!");
            }
            catch (Exception) { }

            Assert.IsType<JsonElement>(d);
            JsonElement elem = (JsonElement)d;

            VerifyPrimitives();
            VerifyObject();
            VerifyArray();

            // Re-serialize
            json = JsonSerializer.Serialize<object>(elem);
            Assert.Equal(Json, json);

            json = JsonSerializer.Serialize<dynamic>(elem);
            Assert.Equal(Json, json);

            json = JsonSerializer.Serialize(elem);
            Assert.Equal(Json, json);

            void VerifyPrimitives()
            {
                Assert.Equal("Hello", elem.GetProperty("MyString").GetString());
                Assert.True(elem.GetProperty("MyBoolean").GetBoolean());
                Assert.Equal(42, elem.GetProperty("MyInt").GetInt32());
                Assert.Equal(4.2, elem.GetProperty("MyDouble").GetDouble());
                Assert.Equal(MyDateTime, elem.GetProperty("MyDateTime").GetDateTime());
                Assert.Equal(MyGuid, elem.GetProperty("MyGuid").GetGuid());
            }

            void VerifyObject()
            {
                Assert.Equal("World", elem.GetProperty("MyObject").GetProperty("MyString").GetString());
            }

            void VerifyArray()
            {
                JsonElement.ArrayEnumerator enumerator = elem.GetProperty("MyArray").EnumerateArray();
                Assert.Equal(2, enumerator.Count());
                enumerator.MoveNext();
                Assert.Equal(1, enumerator.Current.GetInt32());
                enumerator.MoveNext();
                Assert.Equal(2, enumerator.Current.GetInt32());
            }
        }

        [Fact]
        public static void ExpandoObject()
        {
            ExpandoObject expando = JsonSerializer.Deserialize<ExpandoObject>(Json);
            Assert.Equal(9, ((IDictionary<string, object>)expando).Keys.Count);

            dynamic obj = expando;

            VerifyPrimitives();
            VerifyObject();
            VerifyArray();

            // Re-serialize
            string json = JsonSerializer.Serialize<ExpandoObject>(obj);
            Assert.Equal(Json, json);

            json = JsonSerializer.Serialize<dynamic>(obj);
            Assert.Equal(Json, json);

            json = JsonSerializer.Serialize(obj);
            Assert.Equal(Json, json);

            void VerifyPrimitives()
            {
                JsonElement jsonElement = obj.MyString;
                Assert.Equal("Hello", jsonElement.GetString());

                jsonElement = obj.MyBoolean;
                Assert.True(jsonElement.GetBoolean());

                jsonElement = obj.MyInt;
                Assert.Equal(42, jsonElement.GetInt32());

                jsonElement = obj.MyDouble;
                Assert.Equal(4.2, jsonElement.GetDouble());

                jsonElement = obj.MyDateTime;
                Assert.Equal(MyDateTime, jsonElement.GetDateTime());

                jsonElement = obj.MyGuid;
                Assert.Equal(MyGuid, jsonElement.GetGuid());
            }

            void VerifyObject()
            {
                JsonElement jsonElement = obj.MyObject;
                // Here we access a property on a nested object and must use JsonElement (not a dynamic property).
                Assert.Equal("World", jsonElement.GetProperty("MyString").GetString());
            }

            void VerifyArray()
            {
                JsonElement jsonElement = obj.MyArray;
                Assert.Equal(2, jsonElement.EnumerateArray().Count());
            }
        }
    }
}
