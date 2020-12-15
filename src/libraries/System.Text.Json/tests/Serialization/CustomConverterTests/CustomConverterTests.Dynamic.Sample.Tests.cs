// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Dynamic;
using System.Text.Json.Serialization.Samples;
using Xunit;
using static System.Text.Json.Serialization.Samples.JsonSerializerExtensions;

namespace System.Text.Json.Serialization.Tests
{
    public static partial class CustomConverterTests
    {
        private const string ExpectedDomJson = "{\"MyString\":\"Hello!\",\"MyNull\":null,\"MyBoolean\":false,\"MyArray\":[2,3,42]," +
            "\"MyInt\":43,\"MyDateTime\":\"2020-07-08T00:00:00\",\"MyGuid\":\"ed957609-cdfe-412f-88c1-02daca1b4f51\"," +
            "\"MyObject\":{\"MyString\":\"Hello!!\"},\"Child\":{\"ChildProp\":1}}";

        private enum MyCustomEnum
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
            Assert.Equal(MyCustomEnum.Hello, (MyCustomEnum)obj.MyString);
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

            // Verify int-based Enum support through "unknown number type" fallback.
            Assert.Equal(MyCustomEnum.FortyTwo, (MyCustomEnum)obj.MyInt);

            // Verify floating point.
            obj = JsonSerializer.Deserialize<dynamic>("4.2", options);
            Assert.IsType<JsonDynamicNumber>(obj);

            double dbl = (double)obj;
#if !BUILDING_INBOX_LIBRARY
            string temp = dbl.ToString();
            // The reader uses "G17" format which causes temp to be 4.2000000000000002 in this case.
            dbl = double.Parse(temp, System.Globalization.CultureInfo.InvariantCulture);
#endif
            Assert.Equal(4.2, dbl);
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
        public static void JsonDynamicTypes_Serialize()
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
            using (JsonDocument doc = JsonDocument.Parse($"{decimal.MaxValue}"))
            {
                dynamic dynamicNumber = new JsonDynamicNumber(doc.RootElement, options);
                Assert.Equal<decimal>(decimal.MaxValue, (decimal)dynamicNumber);
                json = JsonSerializer.Serialize(dynamicNumber, options);
                Assert.Equal(decimal.MaxValue.ToString(), json);
            }

            // Boolean
            dynamic dynamicBool = new JsonDynamicBoolean(true, options);
            Assert.True(dynamicBool);
            json = JsonSerializer.Serialize(dynamicBool, options);
            Assert.Equal("true", json);

            // Array
            dynamic arr = new JsonDynamicArray(options);
            arr.Add(1);
            arr.Add(2);
            json = JsonSerializer.Serialize(arr, options);
            Assert.Equal("[1,2]", json);

            // Object
            dynamic dynamicObject = new JsonDynamicObject(options);
            dynamicObject["One"] = 1;
            dynamicObject["Two"] = 2;

            json = JsonSerializer.Serialize(dynamicObject, options);
            JsonTestHelper.AssertJsonEqual("{\"One\":1,\"Two\":2}", json);
        }

        [Fact]
        public static void JsonDynamicTypes_Deserialize()
        {
            var options = new JsonSerializerOptions();
            options.EnableDynamicTypes();

            JsonSerializer.Deserialize<JsonDynamicType>("{}", options);
            JsonSerializer.Deserialize<JsonDynamicArray>("[]", options);
            JsonSerializer.Deserialize<JsonDynamicBoolean>("true", options);
            JsonSerializer.Deserialize<JsonDynamicNumber>("0", options);
            JsonSerializer.Deserialize<JsonDynamicNumber>("1.2", options);
            JsonSerializer.Deserialize<JsonDynamicObject>("{}", options);
            JsonSerializer.Deserialize<JsonDynamicString>("\"str\"", options);
        }

        [Fact]
        public static void JsonDynamicTypes_Deserialize_AsObject()
        {
            var options = new JsonSerializerOptions();
            options.EnableDynamicTypes();

            Assert.IsType<JsonDynamicArray>(JsonSerializer.Deserialize<object>("[]", options));
            Assert.IsType<JsonDynamicBoolean>(JsonSerializer.Deserialize<object>("true", options));
            Assert.IsType<JsonDynamicNumber>(JsonSerializer.Deserialize<object>("0", options));
            Assert.IsType<JsonDynamicNumber>(JsonSerializer.Deserialize<object>("1.2", options));
            Assert.IsType<JsonDynamicObject>(JsonSerializer.Deserialize<object>("{}", options));
            Assert.IsType<JsonDynamicString>(JsonSerializer.Deserialize<object>("\"str\"", options));
        }

        /// <summary>
        /// Use a mutable DOM with the 'dynamic' keyword.
        /// </summary>
        [Fact]
        public static void VerifyMutableDom_UsingDynamicKeyword()
        {
            var options = new JsonSerializerOptions();
            options.EnableDynamicTypes();

            dynamic obj = JsonSerializer.Deserialize<dynamic>(DynamicTests.Json, options);
            Assert.IsType<JsonDynamicObject>(obj);

            // Change some primitives.
            obj.MyString = "Hello!";
            obj.MyBoolean = false;
            obj.MyInt = 43;

            // Add nested objects.
            // Use JsonDynamicObject; ExpandoObject should not be used since it doesn't have the same semantics including
            // null handling and case-sensitivity that respects JsonSerializerOptions.PropertyNameCaseInsensitive.
            dynamic myObject = new JsonDynamicObject(options);
            myObject.MyString = "Hello!!";
            obj.MyObject = myObject;

            dynamic child = new JsonDynamicObject(options);
            child.ChildProp = 1;
            obj.Child = child;

            // Modify number elements.
            dynamic arr = obj["MyArray"];
            arr[0] = (int)arr[0] + 1;
            arr[1] = (int)arr[1] + 1;

            // Add an element.
            arr.Add(42);

            string json = JsonSerializer.Serialize(obj, options);
            JsonTestHelper.AssertJsonEqual(ExpectedDomJson, json);
        }

        /// <summary>
        /// Use a mutable DOM without the 'dynamic' keyword.
        /// </summary>
        [Fact]
        public static void VerifyMutableDom_WithoutUsingDynamicKeyword()
        {
            var options = new JsonSerializerOptions();
            options.EnableDynamicTypes();

            JsonDynamicObject obj = (JsonDynamicObject)JsonSerializer.Deserialize<object>(DynamicTests.Json, options);

            // Change some primitives.
            obj["MyString"] = "Hello!";
            obj["MyBoolean"] = false;
            obj["MyInt"] = 43;

            // Add nested objects.
            obj["MyObject"] = new JsonDynamicObject(options)
            {
                ["MyString"] = "Hello!!"
            };

            obj["Child"] = new JsonDynamicObject(options)
            {
                ["ChildProp"] = 1
            };

            // Modify number elements.
            var arr = (JsonDynamicArray)obj["MyArray"];
            var elem = (JsonDynamicNumber)arr[0];
            elem.SetValue(elem.GetValue<int>() + 1);
            elem = (JsonDynamicNumber)arr[1];
            elem.SetValue(elem.GetValue<int>() + 1);

            // Add an element.
            arr.Add(42);

            string json = JsonSerializer.Serialize(obj, options);
            JsonTestHelper.AssertJsonEqual(ExpectedDomJson, json);
        }

        /// <summary>
        /// Use a mutable DOM without the 'dynamic' keyword and use round-trippable values
        /// meaning the 'JsonDynamicType' values are used instead of raw primitives.
        /// </summary>
        [Fact]
        public static void VerifyMutableDom_WithoutUsingDynamicKeyword_JsonDynamicType()
        {
            var options = new JsonSerializerOptions();
            options.EnableDynamicTypes();

            JsonDynamicObject obj = (JsonDynamicObject)JsonSerializer.Deserialize<object>(DynamicTests.Json, options);
            Verify();

            // Verify the values are round-trippable.
            ((JsonDynamicArray)obj["MyArray"]).RemoveAt(2);
            Verify();

            void Verify()
            {
                // Change some primitives.
                ((JsonDynamicType)obj["MyString"]).SetValue("Hello!");
                ((JsonDynamicType)obj["MyBoolean"]).SetValue(false);
                ((JsonDynamicType)obj["MyInt"]).SetValue(43);

                // Add nested objects.
                obj["MyObject"] = new JsonDynamicObject(options)
                {
                    ["MyString"] = new JsonDynamicString("Hello!!", options)
                };
                obj["Child"] = new JsonDynamicObject(options)
                {
                    ["ChildProp"] = new JsonDynamicNumber(1, options)
                };

                // Modify number elements.
                var arr = (JsonDynamicArray)obj["MyArray"];
                ((JsonDynamicType)arr[0]).SetValue(2);
                ((JsonDynamicType)arr[1]).SetValue(3);

                // Add an element.
                arr.Add(new JsonDynamicNumber(42, options));

                string json = JsonSerializer.Serialize(obj, options);
                JsonTestHelper.AssertJsonEqual(ExpectedDomJson, json);
            }
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
            options.PropertyNamingPolicy = new SimpleSnakeCasePolicy();

            dynamic obj = JsonSerializer.Deserialize<dynamic>(Json, options);

            string json = JsonSerializer.Serialize(obj, options);
            JsonTestHelper.AssertJsonEqual(Json, json);
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
        public static void QuotedNumbers_Deserialize()
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

        [Fact]
        public static void QuotedNumbers_Serialize()
        {
            var options = new JsonSerializerOptions();
            options.EnableDynamicTypes();
            options.NumberHandling = JsonNumberHandling.WriteAsString;

            dynamic obj = 42L;
            string json = JsonSerializer.Serialize<dynamic>(obj, options);
            Assert.Equal("\"42\"", json);

            obj = double.NaN;
            json = JsonSerializer.Serialize<dynamic>(obj, options);
            Assert.Equal("\"NaN\"", json);
        }
    }
}
