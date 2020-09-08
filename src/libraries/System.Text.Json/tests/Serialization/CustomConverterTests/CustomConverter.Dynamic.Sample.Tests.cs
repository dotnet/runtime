// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Dynamic;
using System.Text.Json.Serialization.Samples;
using Xunit;
using static System.Text.Json.Serialization.Samples.JsonSerializerExtensions;

namespace System.Text.Json.Serialization.Tests
{
    public static class CustomConverterTests_Dynamic
    {
        private enum MyEnum
        {
            Default = 0,
            FortyTwo = 42,
            Hello = 77
        }

        [Fact]
        public static void VerifyPrimitives()
        {
            var options = new JsonSerializerOptions();
            options.EnableDynamicTypes();
            options.Converters.Add(new JsonStringEnumConverter());

            dynamic obj = JsonSerializer.Deserialize<dynamic>(DynamicTests.Json, options);
            Assert.IsType<JsonDynamicObject>(obj);

            // JsonDynamicString has an implicit cast to string.
            Assert.IsType<JsonDynamicString>(obj.MyString);
            Assert.Equal("Hello", obj.MyString);

            // Verify other string-based types.
            Assert.Equal(MyEnum.Hello, (MyEnum)obj.MyString);
            Assert.Equal(DynamicTests.MyDateTime, (DateTime)obj.MyDateTime);
            Assert.Equal(DynamicTests.MyGuid, (Guid)obj.MyGuid);

            // JsonDynamicBoolean has an implicit cast to bool.
            Assert.IsType<JsonDynamicBoolean>(obj.MyBoolean);
            Assert.True(obj.MyBoolean);

            // Numbers must specify the type through a cast or assignment.
            Assert.IsType<JsonDynamicNumber>(obj.MyInt);
            Assert.ThrowsAny<Exception>(() => obj.MyInt == 42L);
            Assert.Equal(42L, (long)obj.MyInt);
            Assert.Equal((byte)42, (byte)obj.MyInt);
            Assert.Equal(42f, (float)obj.MyInt);
            Assert.Equal(42m, (decimal)obj.MyInt);

            Assert.IsType<JsonDynamicNumber>(obj.MyDouble);
            Assert.Equal(4.2, (double)obj.MyDouble);

            double d = obj.MyDouble;
            Assert.Equal(4.2, d);

            // Verify int-based Enum support through "unknown number type" fallback.
            Assert.Equal(MyEnum.FortyTwo, (MyEnum)obj.MyInt);
        }

        [Fact]
        public static void VerifyArray()
        {
            var options = new JsonSerializerOptions();
            options.EnableDynamicTypes();
            options.Converters.Add(new JsonStringEnumConverter());

            dynamic obj = JsonSerializer.Deserialize<dynamic>(DynamicTests.Json, options);
            Assert.IsType<JsonDynamicObject>(obj);

            Assert.IsType<JsonDynamicObject>(obj);
            Assert.IsType<JsonDynamicArray>(obj.MyArray);

            Assert.Equal(2, obj.MyArray.Count);
            Assert.Equal(1, (int)obj.MyArray[0]);
            Assert.Equal(2, (int)obj.MyArray[1]);

            // Ensure we can enumerate
            int count = 0;
            foreach (long value in obj.MyArray)
            {
                count++;
            }
            Assert.Equal(2, count);

            // Ensure we can mutate through indexers
            obj.MyArray[0] = 10;
            Assert.Equal(10, (int)obj.MyArray[0]);
        }

        [Fact]
        public static void DirectUseOfDynamicTypes()
        {
            var options = new JsonSerializerOptions();
            options.EnableDynamicTypes();

            // Guid (string)
            string GuidJson = $"{DynamicTests.MyGuid.ToString("D")}";
            string GuidJsonWithQuotes = $"\"{GuidJson}\"";

            dynamic dynamicString = new JsonDynamicString(GuidJson, options);
            Assert.Equal(DynamicTests.MyGuid, (Guid)dynamicString);
            string json = JsonSerializer.Serialize(dynamicString, options);
            Assert.Equal(GuidJsonWithQuotes, json);

            // char (string)
            dynamicString = new JsonDynamicString("a", options);
            Assert.Equal('a', (char)dynamicString);
            json = JsonSerializer.Serialize(dynamicString, options);
            Assert.Equal("\"a\"", json);

            // Number (JsonElement)
            using (JsonDocument doc = JsonDocument.Parse($"{Decimal.MaxValue}"))
            {
                dynamic dynamicNumber = new JsonDynamicNumber(doc.RootElement, options);
                Assert.Equal<decimal>(Decimal.MaxValue, (decimal)dynamicNumber);
                json = JsonSerializer.Serialize(dynamicNumber, options);
                Assert.Equal(Decimal.MaxValue.ToString(), json);
            }

            // Boolean
            dynamic dynamicBool = new JsonDynamicBoolean(true, options);
            Assert.True(dynamicBool);
            json = JsonSerializer.Serialize(dynamicBool, options);
            Assert.Equal("true", json);

            // Array
            List<object> list = new List<object>();
            list.Add(1);
            list.Add(2);

            dynamic dynamicArray = new JsonDynamicArray(list, options);
            json = JsonSerializer.Serialize(dynamicArray, options);
            Assert.Equal("[1,2]", json);

            // Object
            Dictionary<string, object> dictionary = new Dictionary<string, object>();
            dictionary["One"] = 1;
            dictionary["Two"] = 2;

            dynamic dynamicObject = new JsonDynamicObject(dictionary, options);
            json = JsonSerializer.Serialize(dynamicObject, options);
            Assert.Equal("{\"One\":1,\"Two\":2}", json);
        }


        [Fact]
        public static void DirectUseOfDynamicTypes_Deserialize_Fails()
        {
            var options = new JsonSerializerOptions();
            options.EnableDynamicTypes();

            Assert.Throws<InvalidOperationException>(() => JsonSerializer.Deserialize<JsonDynamicType>("{}", options));
            Assert.Throws<InvalidOperationException>(() => JsonSerializer.Deserialize<JsonDynamicArray>("[]", options));
            Assert.Throws<InvalidOperationException>(() => JsonSerializer.Deserialize<JsonDynamicBoolean>("true", options));
            Assert.Throws<InvalidOperationException>(() => JsonSerializer.Deserialize<JsonDynamicNumber>("0", options));
            Assert.Throws<InvalidOperationException>(() => JsonSerializer.Deserialize<JsonDynamicObject>("{}", options));
            Assert.Throws<InvalidOperationException>(() => JsonSerializer.Deserialize<JsonDynamicString>("\"str\"", options));
        }

        [Fact]
        public static void DirectUseOfDynamicTypes_Deserialize()
        {
            var options = new JsonSerializerOptions();
            options.EnableDynamicTypes();

            Assert.IsType<JsonDynamicArray>(JsonSerializer.Deserialize<object>("[]", options));
            Assert.IsType<JsonDynamicBoolean>(JsonSerializer.Deserialize<object>("true", options));
            Assert.IsType<JsonDynamicNumber>(JsonSerializer.Deserialize<object>("0", options));
            Assert.IsType<JsonDynamicObject>(JsonSerializer.Deserialize<object>("{}", options));
            Assert.IsType<JsonDynamicString>(JsonSerializer.Deserialize<object>("\"str\"", options));
        }

        [Fact]
        public static void VerifyMutableDom()
        {
            const string ExpectedJson ="{\"MyString\":\"Hello!\",\"MyNull\":null,\"MyBoolean\":false,\"MyArray\":[\"2\"]," +
                "\"MyInt\":43,\"MyDouble\":4.3,\"MyDateTime\":\"2020-07-08T00:00:00\",\"MyGuid\":\"ed957609-cdfe-412f-88c1-02daca1b4f51\"," +
                "\"MyObject\":{\"MyString\":\"Hello!!\"},\"Child\":{\"ChildProp\":1}}";

            var options = new JsonSerializerOptions();
            options.EnableDynamicTypes();

            dynamic obj = JsonSerializer.Deserialize<dynamic>(DynamicTests.Json, options);
            Assert.IsType<JsonDynamicObject>(obj);

            // Change some values
            obj.MyString = "Hello!";
            obj.MyBoolean = false;
            obj.MyInt = 43;
            obj.MyDouble = 4.3;
            obj.MyObject = new ExpandoObject();
            obj.MyObject.MyString = "Hello!!";
            obj.MyArray.Clear();
            obj.MyArray.Add("2");

            // Use ExandoObject for new property
            dynamic child = new ExpandoObject();
            child.ChildProp = 1;
            obj.Child = child;

            string json = JsonSerializer.Serialize(obj, options);
            Assert.Equal(ExpectedJson, json);
        }

        [Fact]
        public static void VerifyMutableDom_WithIndexers()
        {
            const string ExpectedJson = "{\"MyString\":\"Hello!\",\"MyNull\":null,\"MyBoolean\":false,\"MyArray\":[\"2\"]," +
                "\"MyInt\":43,\"MyDouble\":4.3,\"MyDateTime\":\"2020-07-08T00:00:00\",\"MyGuid\":\"ed957609-cdfe-412f-88c1-02daca1b4f51\"," +
                "\"MyObject\":{\"MyString\":\"Hello!!\"},\"Child\":{\"ChildProp\":1}}";

            var options = new JsonSerializerOptions();
            options.EnableDynamicTypes();

            JsonDynamicObject obj = (JsonDynamicObject)JsonSerializer.Deserialize<object>(DynamicTests.Json, options);

            // Change some primitives
            obj["MyString"] = "Hello!";
            obj["MyBoolean"] = false;
            obj["MyInt"] = 43;
            obj["MyDouble"] = 4.3;

            ((IDictionary<string, object>)obj["MyObject"])["MyString"] = "Hello!!";

            // Use Dictionary for new child property
            var child = new Dictionary<string, object>();
            child["ChildProp"] = 1;
            obj["Child"] = child;

            ((IList<object>)obj["MyArray"]).Clear();
            ((IList<object>)obj["MyArray"]).Add("1");
            ((IList<object>)obj["MyArray"])[0]="2"; // replace the "1"

            string json = JsonSerializer.Serialize(obj, options);
            Assert.Equal(ExpectedJson, json);
        }

        [Fact]
        public static void DynamicObject_MissingProperty()
        {
            var options = new JsonSerializerOptions();
            options.EnableDynamicTypes();
            dynamic obj = JsonSerializer.Deserialize<dynamic>("{}", options);

            // We return null here; ExpandoObject throws for missing properties.
            Assert.Equal(null, obj.NonExistingProperty);
        }

        [Fact]
        public static void DynamicObject_CaseSensitivity()
        {
            var options = new JsonSerializerOptions();
            options.EnableDynamicTypes();
            dynamic obj = JsonSerializer.Deserialize<dynamic>("{\"MyProperty\":42}", options);

            Assert.Equal(42, (int)obj.MyProperty);
            Assert.Null(obj.myproperty);
            Assert.Null(obj.MYPROPERTY);

            options = new JsonSerializerOptions();
            options.EnableDynamicTypes();
            options.PropertyNameCaseInsensitive = true;
            obj = JsonSerializer.Deserialize<dynamic>("{\"MyProperty\":42}", options);

            Assert.Equal(42, (int)obj.MyProperty);
            Assert.Equal(42, (int)obj.myproperty);
            Assert.Equal(42, (int)obj.MYPROPERTY);
        }

        [Fact]
        public static void NamingPoliciesAreNotUsed()
        {
            const string Json = "{\"myProperty\":42}";

            var options = new JsonSerializerOptions();
            options.EnableDynamicTypes();
            options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;

            dynamic obj = JsonSerializer.Deserialize<dynamic>(Json, options);

            string json = JsonSerializer.Serialize(obj, options);
            Assert.Equal(Json, json);
        }

        [Fact]
        public static void NullHandling()
        {
            var options = new JsonSerializerOptions();
            options.EnableDynamicTypes();

            dynamic obj = JsonSerializer.Deserialize<dynamic>("null", options);
            Assert.Null(obj);
        }

        [Fact]
        public static void QuotedNumbers()
        {
            var options = new JsonSerializerOptions();
            options.EnableDynamicTypes();
            options.NumberHandling = JsonNumberHandling.AllowReadingFromString |
                JsonNumberHandling.AllowNamedFloatingPointLiterals;

            dynamic obj = JsonSerializer.Deserialize<dynamic>("\"42\"", options);
            Assert.IsType<JsonDynamicString>(obj);
            Assert.Equal(42, (int)obj);

            obj = JsonSerializer.Deserialize<dynamic>("\"NaN\"", options);
            Assert.IsType<JsonDynamicString>(obj);
            Assert.Equal(double.NaN, (double)obj);
            Assert.Equal(float.NaN, (float)obj);
        }
    }
}
