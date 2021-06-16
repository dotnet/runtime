// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using Microsoft.CSharp.RuntimeBinder;
using Xunit;

namespace System.Text.Json.Nodes.Tests
{
    public static class DynamicTests
    {
        [Fact]
        public static void ImplicitOperators()
        {
            dynamic jObj = new JsonObject();

            // Dynamic objects do not support object initializers.

            // Primitives
            jObj.MyString = "Hello!";
            Assert.IsAssignableFrom<JsonValue>(jObj.MyString);

            jObj.MyNull = null;
            jObj.MyBoolean = false;

            // Nested array
            jObj.MyArray = new JsonArray(2, 3, 42);

            // Additional primitives
            jObj.MyInt = 43;
            jObj.MyDateTime = new DateTime(2020, 7, 8);
            jObj.MyGuid = new Guid("ed957609-cdfe-412f-88c1-02daca1b4f51");

            // Nested objects
            jObj.MyObject = new JsonObject();
            jObj.MyObject.MyString = "Hello!!";

            jObj.Child = new JsonObject();
            jObj.Child.ChildProp = 1;

            var options = new JsonSerializerOptions();
            options.UnknownTypeHandling = JsonUnknownTypeHandling.JsonNode;

            string json = jObj.ToJsonString(options);
            JsonTestHelper.AssertJsonEqual(JsonNodeTests.ExpectedDomJson, json);
        }

        private enum MyCustomEnum
        {
            Default = 0,
            FortyTwo = 42,
            Hello = 77
        }

        [Fact]
        public static void Primitives_UnknownTypeHandling()
        {
            var options = new JsonSerializerOptions();
            options.UnknownTypeHandling = JsonUnknownTypeHandling.JsonNode;
            options.Converters.Add(new JsonStringEnumConverter());

            dynamic obj = JsonSerializer.Deserialize<object>(Serialization.Tests.DynamicTests.Json, options);
            Assert.IsAssignableFrom<JsonObject>(obj);

            // JsonValue created from a JSON string.
            Assert.IsAssignableFrom<JsonValue>(obj.MyString);
            Assert.Equal("Hello", (string)obj.MyString);

            // Verify other string-based types.
            // Even though a custom converter was used, an explicit deserialize needs to be done.
            Assert.Equal(42, (int)obj.MyInt);
            Assert.ThrowsAny<RuntimeBinderException>(() => (MyCustomEnum)obj.MyInt);
            // Perform the explicit deserialize on the enum.
            Assert.Equal(MyCustomEnum.FortyTwo, JsonSerializer.Deserialize<MyCustomEnum>(obj.MyInt.ToJsonString()));

            Assert.Equal(Serialization.Tests.DynamicTests.MyDateTime, (DateTime)obj.MyDateTime);
            Assert.Equal(Serialization.Tests.DynamicTests.MyGuid, (Guid)obj.MyGuid);

            // JsonValue created from a JSON bool.
            Assert.IsAssignableFrom<JsonValue>(obj.MyBoolean);
            bool b = (bool)obj.MyBoolean;
            Assert.True(b);

            // Numbers must specify the type through a cast or assignment.
            Assert.IsAssignableFrom<JsonValue>(obj.MyInt);
            Assert.ThrowsAny<RuntimeBinderException>(() => obj.MyInt == 42L);
            Assert.Equal(42L, (long)obj.MyInt);
            Assert.Equal((byte)42, (byte)obj.MyInt);

            // Verify floating point.
            obj = JsonSerializer.Deserialize<object>("4.2", options);
            Assert.IsAssignableFrom<JsonValue>(obj);

            double dbl = (double)obj;
            Assert.Equal(4.2, dbl);
        }

        [Fact]
        public static void Array_UnknownTypeHandling()
        {
            var options = new JsonSerializerOptions();
            options.UnknownTypeHandling = JsonUnknownTypeHandling.JsonNode;

            dynamic obj = JsonSerializer.Deserialize<object>(Serialization.Tests.DynamicTests.Json, options);
            Assert.IsAssignableFrom<JsonObject>(obj);
            Assert.IsAssignableFrom<JsonArray>(obj.MyArray);

            Assert.Equal(2, obj.MyArray.Count);
            Assert.Equal(1, (int)obj.MyArray[0]);
            Assert.Equal(2, (int)obj.MyArray[1]);

            int count = 0;
            foreach (object value in obj.MyArray)
            {
                count++;
            }
            Assert.Equal(2, count);
            Assert.Equal(2, obj.MyArray.Count);

            obj.MyArray[0] = 10;
            Assert.IsAssignableFrom<JsonValue>(obj.MyArray[0]);

            Assert.Equal(10, (int)obj.MyArray[0]);
        }

        [Fact]
        public static void CreateDom_UnknownTypeHandling()
        {
            var options = new JsonSerializerOptions();
            options.UnknownTypeHandling = JsonUnknownTypeHandling.JsonNode;

            string GuidJson = $"{Serialization.Tests.DynamicTests.MyGuid.ToString("D")}";

            // We can't convert an unquoted string to a Guid
            dynamic dynamicString = JsonValue.Create(GuidJson);
            InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() => (Guid)dynamicString);
            // "A value of type 'System.String' cannot be converted to a 'System.Guid'."
            Assert.Contains(typeof(string).ToString(), ex.Message);
            Assert.Contains(typeof(Guid).ToString(), ex.Message);

            string json;

            // Number (JsonElement)
            using (JsonDocument doc = JsonDocument.Parse($"{decimal.MaxValue}"))
            {
                dynamic dynamicNumber = JsonValue.Create(doc.RootElement);
                Assert.Equal(decimal.MaxValue, (decimal)dynamicNumber);
                json = dynamicNumber.ToJsonString(options);
                Assert.Equal(decimal.MaxValue.ToString(), json);
            }

            // Boolean
            dynamic dynamicBool = JsonValue.Create(true);
            Assert.True((bool)dynamicBool);
            json = dynamicBool.ToJsonString(options);
            Assert.Equal("true", json);

            // Array
            dynamic arr = new JsonArray();
            arr.Add(1);
            arr.Add(2);
            json = arr.ToJsonString(options);
            Assert.Equal("[1,2]", json);

            // Object
            dynamic dynamicObject = new JsonObject();
            dynamicObject.One = 1;
            dynamicObject.Two = 2;

            json = dynamicObject.ToJsonString(options);
            JsonTestHelper.AssertJsonEqual("{\"One\":1,\"Two\":2}", json);
        }

        /// <summary>
        /// Use a mutable DOM with the 'dynamic' keyword.
        /// </summary>
        [Fact]
        public static void UnknownTypeHandling_Object()
        {
            var options = new JsonSerializerOptions();
            options.UnknownTypeHandling = JsonUnknownTypeHandling.JsonNode;

            dynamic obj = JsonSerializer.Deserialize<object>(Serialization.Tests.DynamicTests.Json, options);
            Assert.IsAssignableFrom<JsonObject>(obj);

            // Change some primitives.
            obj.MyString = "Hello!";
            obj.MyBoolean = false;
            obj.MyInt = 43;

            // Add nested objects.
            // Use JsonObject; ExpandoObject should not be used since it doesn't have the same semantics including
            // null handling and case-sensitivity that respects JsonSerializerOptions.PropertyNameCaseInsensitive.
            dynamic myObject = new JsonObject();
            myObject.MyString = "Hello!!";
            obj.MyObject = myObject;

            dynamic child = new JsonObject();
            child.ChildProp = 1;
            obj.Child = child;

            // Modify number elements.
            dynamic arr = obj.MyArray;
            arr[0] = (int)arr[0] + 1;
            arr[1] = (int)arr[1] + 1;

            // Add an element.
            arr.Add(42);

            string json = obj.ToJsonString(options);
            JsonTestHelper.AssertJsonEqual(JsonNodeTests.ExpectedDomJson, json);
        }

        [Fact]
        public static void ConvertJsonArrayToIListOfJsonNode()
        {
            dynamic obj = JsonSerializer.Deserialize<JsonArray>("[42]");
            Assert.Equal(42, (int)obj[0]);

            IList<JsonNode> ilist = obj;
            Assert.NotNull(ilist);
            Assert.Equal(42, (int)ilist[0]);
        }

        [Fact]
        public static void UnknownTypeHandling_CaseSensitivity()
        {
            var options = new JsonSerializerOptions();
            options.UnknownTypeHandling = JsonUnknownTypeHandling.JsonNode;
            dynamic obj = JsonSerializer.Deserialize<object>("{\"MyProperty\":42}", options);

            Assert.IsType<JsonObject>(obj);
            Assert.IsAssignableFrom<JsonValue>(obj.MyProperty);

            Assert.Equal(42, (int)obj.MyProperty);
            Assert.Null(obj.myProperty);
            Assert.Null(obj.MYPROPERTY);

            options = new JsonSerializerOptions();
            options.UnknownTypeHandling = JsonUnknownTypeHandling.JsonNode;
            options.PropertyNameCaseInsensitive = true;
            obj = JsonSerializer.Deserialize<object>("{\"MyProperty\":42}", options);

            Assert.Equal(42, (int)obj.MyProperty);
            Assert.Equal(42, (int)obj.myproperty);
            Assert.Equal(42, (int)obj.MYPROPERTY);
        }

        [Fact]
        public static void MissingProperty_UnknownTypeHandling()
        {
            var options = new JsonSerializerOptions();
            options.UnknownTypeHandling = JsonUnknownTypeHandling.JsonNode;
            dynamic obj = JsonSerializer.Deserialize<object>("{}", options);
            Assert.Equal(null, obj.NonExistingProperty);
        }

        [Fact]
        public static void Linq_UnknownTypeHandling()
        {
            var options = new JsonSerializerOptions();
            options.UnknownTypeHandling = JsonUnknownTypeHandling.JsonNode;

            IEnumerable<dynamic> allOrders = JsonSerializer.Deserialize<IEnumerable<dynamic>>(JsonNodeTests.Linq_Query_Json, options);
            IEnumerable<dynamic> orders = allOrders.Where(o => ((string)o.Customer.City) == "Fargo");

            Assert.Equal(2, orders.Count());
            Assert.Equal(100, (int)orders.ElementAt(0).OrderId);
            Assert.Equal(300, (int)orders.ElementAt(1).OrderId);
            Assert.Equal("Customer1", (string)orders.ElementAt(0).Customer.Name);
            Assert.Equal("Customer3", (string)orders.ElementAt(1).Customer.Name);

            // Verify methods can be called as well.
            Assert.Equal(100, orders.ElementAt(0).OrderId.GetValue<int>());
            Assert.Equal(300, orders.ElementAt(1).OrderId.GetValue<int>());
            Assert.Equal("Customer1", orders.ElementAt(0).Customer.Name.GetValue<string>());
            Assert.Equal("Customer3", orders.ElementAt(1).Customer.Name.GetValue<string>());
        }
    }
}
