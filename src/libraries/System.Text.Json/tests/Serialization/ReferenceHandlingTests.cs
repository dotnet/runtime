// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Text.Encodings.Web;
using Newtonsoft.Json;
using Xunit;

namespace System.Text.Json.Serialization.Tests
{
    public static partial class ReferenceHandlingTests
    {

        [Fact]
        public static void ThrowByDefaultOnLoop()
        {
            Employee a = new Employee();
            a.Manager = a;

            JsonException ex = Assert.Throws<JsonException>(() => JsonSerializer.Serialize(a));
        }

        [Fact]
        public static void ThrowWhenPassingNullToReferenceHandling()
        {
            Assert.Throws<ArgumentNullException>(() => new JsonSerializerOptions { ReferenceHandling = null });
        }

        #region Root Object
        [Fact]
        public static void ObjectLoop()
        {
            Employee angela = new Employee();
            angela.Manager = angela;

            // Compare parity with Newtonsoft.Json
            string expected = JsonConvert.SerializeObject(angela, s_newtonsoftSerializerSettingsPreserve);
            string actual = JsonSerializer.Serialize(angela, s_serializerOptionsPreserve);

            Assert.Equal(expected, actual);

            // Ensure round-trip
            Employee angelaCopy = JsonSerializer.Deserialize<Employee>(actual, s_serializerOptionsPreserve);
            Assert.Same(angelaCopy.Manager, angelaCopy);
        }

        [Fact]
        public static void ObjectArrayLoop()
        {
            Employee angela = new Employee();
            angela.Subordinates = new List<Employee> { angela };

            string expected = JsonConvert.SerializeObject(angela, s_newtonsoftSerializerSettingsPreserve);
            string actual = JsonSerializer.Serialize(angela, s_serializerOptionsPreserve);

            Assert.Equal(expected, actual);

            Employee angelaCopy = JsonSerializer.Deserialize<Employee>(actual, s_serializerOptionsPreserve);
            Assert.Same(angelaCopy.Subordinates[0], angelaCopy);
        }

        [Fact]
        public static void ObjectDictionaryLoop()
        {
            Employee angela = new Employee();
            angela.Contacts = new Dictionary<string, Employee> { { "555-5555", angela } };

            string expected = JsonConvert.SerializeObject(angela, s_newtonsoftSerializerSettingsPreserve);
            string actual = JsonSerializer.Serialize(angela, s_serializerOptionsPreserve);

            Assert.Equal(expected, actual);

            Employee angelaCopy = JsonSerializer.Deserialize<Employee>(actual, s_serializerOptionsPreserve);
            Assert.Same(angelaCopy.Contacts["555-5555"], angelaCopy);
        }

        [Fact]
        public static void ObjectPreserveDuplicateObjects()
        {
            Employee angela = new Employee
            {
                Manager = new Employee { Name = "Bob" }
            };
            angela.Manager2 = angela.Manager;

            string expected = JsonConvert.SerializeObject(angela, s_newtonsoftSerializerSettingsPreserve);
            string actual = JsonSerializer.Serialize(angela, s_serializerOptionsPreserve);

            Assert.Equal(expected, actual);

            Employee angelaCopy = JsonSerializer.Deserialize<Employee>(actual, s_serializerOptionsPreserve);
            Assert.Same(angelaCopy.Manager, angelaCopy.Manager2);
        }

        [Fact]
        public static void ObjectPreserveDuplicateDictionaries()
        {
            Employee angela = new Employee
            {
                Contacts = new Dictionary<string, Employee> { { "444-4444", new Employee { Name = "Bob" } } }
            };
            angela.Contacts2 = angela.Contacts;

            string expected = JsonConvert.SerializeObject(angela, s_newtonsoftSerializerSettingsPreserve);
            string actual = JsonSerializer.Serialize(angela, s_serializerOptionsPreserve);

            Assert.Equal(expected, actual);

            Employee angelaCopy = JsonSerializer.Deserialize<Employee>(actual, s_serializerOptionsPreserve);
            Assert.Same(angelaCopy.Contacts, angelaCopy.Contacts2);
        }

        [Fact]
        public static void ObjectPreserveDuplicateArrays()
        {
            Employee angela = new Employee
            {
                Subordinates = new List<Employee> { new Employee { Name = "Bob" } }
            };
            angela.Subordinates2 = angela.Subordinates;

            string expected = JsonConvert.SerializeObject(angela, s_newtonsoftSerializerSettingsPreserve);
            string actual = JsonSerializer.Serialize(angela, s_serializerOptionsPreserve);

            Assert.Equal(expected, actual);

            Employee angelaCopy = JsonSerializer.Deserialize<Employee>(actual, s_serializerOptionsPreserve);
            Assert.Same(angelaCopy.Subordinates, angelaCopy.Subordinates2);
        }

        [Fact]
        public static void KeyValuePairTest()
        {
            var kvp = new KeyValuePair<string, string>("key", "value");
            string json = JsonSerializer.Serialize(kvp, s_deserializerOptionsPreserve);
            KeyValuePair<string, string> kvp2 = JsonSerializer.Deserialize<KeyValuePair<string, string>>(json, s_deserializerOptionsPreserve);

            Assert.Equal(kvp.Key, kvp2.Key);
            Assert.Equal(kvp.Value, kvp2.Value);
        }

        private class ClassWithZeroLengthProperty<TValue>
        {
            [JsonPropertyName("")]
            public TValue ZeroLengthProperty { get; set; }
        }

        [Fact]
        public static void OjectZeroLengthProperty()
        {
            // Default

            ClassWithZeroLengthProperty<int> rootValue = RoundTripZeroLengthProperty(new ClassWithZeroLengthProperty<int>(), 10);
            Assert.Equal(10, rootValue.ZeroLengthProperty);

            ClassWithZeroLengthProperty<Employee> rootObject = RoundTripZeroLengthProperty(new ClassWithZeroLengthProperty<Employee>(), new Employee { Name = "Test" });
            Assert.Equal("Test", rootObject.ZeroLengthProperty.Name);

            ClassWithZeroLengthProperty<List<int>> rootArray = RoundTripZeroLengthProperty(new ClassWithZeroLengthProperty<List<int>>(), new List<int>());
            Assert.Equal(0, rootArray.ZeroLengthProperty.Count);

            // Preserve

            ClassWithZeroLengthProperty<int> rootValue2 = RoundTripZeroLengthProperty(new ClassWithZeroLengthProperty<int>(), 10, s_deserializerOptionsPreserve);
            Assert.Equal(10, rootValue2.ZeroLengthProperty);

            ClassWithZeroLengthProperty<Employee> rootObject2 = RoundTripZeroLengthProperty(new ClassWithZeroLengthProperty<Employee>(), new Employee { Name = "Test" }, s_deserializerOptionsPreserve);
            Assert.Equal("Test", rootObject2.ZeroLengthProperty.Name);

            ClassWithZeroLengthProperty<List<int>> rootArray2 = RoundTripZeroLengthProperty(new ClassWithZeroLengthProperty<List<int>>(), new List<int>(), s_deserializerOptionsPreserve);
            Assert.Equal(0, rootArray2.ZeroLengthProperty.Count);
        }

        private static ClassWithZeroLengthProperty<TValue> RoundTripZeroLengthProperty<TValue>(ClassWithZeroLengthProperty<TValue> obj, TValue value, JsonSerializerOptions opts = null)
        {
            obj.ZeroLengthProperty = value;
            string json = JsonSerializer.Serialize(obj, opts);
            Assert.Contains("\"\":", json);

            return JsonSerializer.Deserialize<ClassWithZeroLengthProperty<TValue>>(json, opts);
        }

        [Fact]
        public static void UnicodePropertyNames()
        {
            ClassWithUnicodeProperty obj = new ClassWithUnicodeProperty
            {
                A\u0467 = 1
            };

            // Verify the name is escaped after serialize.
            string json = JsonSerializer.Serialize(obj, s_serializerOptionsPreserve);
            Assert.StartsWith("{\"$id\":\"1\",", json);
            Assert.Contains(@"""A\u0467"":1", json);

            // Round-trip
            ClassWithUnicodeProperty objCopy = JsonSerializer.Deserialize<ClassWithUnicodeProperty>(json, s_serializerOptionsPreserve);
            Assert.Equal(1, objCopy.A\u0467);

            // With custom escaper
            // Specifying encoder on options does not impact deserialize.
            var optionsWithEncoder = new JsonSerializerOptions
            {
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                ReferenceHandling = ReferenceHandling.Preserve
            };
            json = JsonSerializer.Serialize(obj, optionsWithEncoder);
            Assert.StartsWith("{\"$id\":\"1\",", json);
            Assert.Contains("\"A\u0467\":1", json);

            // Round-trip
            objCopy = JsonSerializer.Deserialize<ClassWithUnicodeProperty>(json, optionsWithEncoder);
            Assert.Equal(1, objCopy.A\u0467);

            // We want to go over StackallocThreshold=256 to force a pooled allocation, so this property is 400 chars and 401 bytes.
            obj = new ClassWithUnicodeProperty
            {
                A\u046734567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890 = 1
            };

            // Verify the name is escaped after serialize.
            json = JsonSerializer.Serialize(obj, s_serializerOptionsPreserve);
            Assert.StartsWith("{\"$id\":\"1\",", json);
            Assert.Contains(@"""A\u046734567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890"":1", json);

            // Round-trip
            objCopy = JsonSerializer.Deserialize<ClassWithUnicodeProperty>(json, s_serializerOptionsPreserve);
            Assert.Equal(1, objCopy.A\u046734567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890);

            // With custom escaper
            json = JsonSerializer.Serialize(obj, optionsWithEncoder);
            Assert.StartsWith("{\"$id\":\"1\",", json);
            Assert.Contains("\"A\u046734567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890\":1", json);

            // Round-trip
            objCopy = JsonSerializer.Deserialize<ClassWithUnicodeProperty>(json, optionsWithEncoder);
            Assert.Equal(1, objCopy.A\u046734567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890);
        }
        #endregion Root Object

        #region Root Dictionary
        private class DictionaryWithGenericCycle : Dictionary<string, DictionaryWithGenericCycle> { }

        [Fact]
        public static void DictionaryLoop()
        {
            DictionaryWithGenericCycle root = new DictionaryWithGenericCycle();
            root["Self"] = root;
            root["Other"] = new DictionaryWithGenericCycle();

            string expected = JsonConvert.SerializeObject(root, s_newtonsoftSerializerSettingsPreserve);
            string actual = JsonSerializer.Serialize(root, s_serializerOptionsPreserve);

            Assert.Equal(expected, actual);

            DictionaryWithGenericCycle rootCopy = JsonSerializer.Deserialize<DictionaryWithGenericCycle>(actual, s_serializerOptionsPreserve);
            Assert.Same(rootCopy, rootCopy["Self"]);
        }

        [Fact]
        public static void DictionaryPreserveDuplicateDictionaries()
        {
            DictionaryWithGenericCycle root = new DictionaryWithGenericCycle();
            root["Self1"] = root;
            root["Self2"] = root;

            string expected = JsonConvert.SerializeObject(root, s_newtonsoftSerializerSettingsPreserve);
            string actual = JsonSerializer.Serialize(root, s_serializerOptionsPreserve);

            Assert.Equal(expected, actual);

            DictionaryWithGenericCycle rootCopy = JsonSerializer.Deserialize<DictionaryWithGenericCycle>(actual, s_serializerOptionsPreserve);
            Assert.Same(rootCopy, rootCopy["Self1"]);
            Assert.Same(rootCopy, rootCopy["Self2"]);
        }

        [Fact]
        public static void DictionaryObjectLoop()
        {
            Dictionary<string, Employee> root = new Dictionary<string, Employee>();
            root["Angela"] = new Employee() { Name = "Angela", Contacts = root };

            string expected = JsonConvert.SerializeObject(root, s_newtonsoftSerializerSettingsPreserve);
            string actual = JsonSerializer.Serialize(root, s_serializerOptionsPreserve);

            Assert.Equal(expected, actual);

            Dictionary<string, Employee> rootCopy = JsonSerializer.Deserialize<Dictionary<string, Employee>>(actual, s_serializerOptionsPreserve);
            Assert.Same(rootCopy, rootCopy["Angela"].Contacts);
        }

        private class DictionaryWithGenericCycleWithinList : Dictionary<string, List<DictionaryWithGenericCycleWithinList>> { }

        [Fact]
        public static void DictionaryArrayLoop()
        {
            DictionaryWithGenericCycleWithinList root = new DictionaryWithGenericCycleWithinList();
            root["ArrayWithSelf"] = new List<DictionaryWithGenericCycleWithinList> { root };

            string expected = JsonConvert.SerializeObject(root, s_newtonsoftSerializerSettingsPreserve);
            string actual = JsonSerializer.Serialize(root, s_serializerOptionsPreserve);

            Assert.Equal(expected, actual);

            DictionaryWithGenericCycleWithinList rootCopy = JsonSerializer.Deserialize<DictionaryWithGenericCycleWithinList>(actual, s_serializerOptionsPreserve);
            Assert.Same(rootCopy, rootCopy["ArrayWithSelf"][0]);
        }

        [Fact]
        public static void DictionaryPreserveDuplicateArrays()
        {
            DictionaryWithGenericCycleWithinList root = new DictionaryWithGenericCycleWithinList();
            root["Array1"] = new List<DictionaryWithGenericCycleWithinList> { root };
            root["Array2"] = root["Array1"];

            string expected = JsonConvert.SerializeObject(root, s_newtonsoftSerializerSettingsPreserve);
            string actual = JsonSerializer.Serialize(root, s_serializerOptionsPreserve);

            Assert.Equal(expected, actual);

            DictionaryWithGenericCycleWithinList rootCopy = JsonSerializer.Deserialize<DictionaryWithGenericCycleWithinList>(actual, s_serializerOptionsPreserve);
            Assert.Same(rootCopy, rootCopy["Array1"][0]);
            Assert.Same(rootCopy["Array2"], rootCopy["Array1"]);
        }

        [Fact]
        public static void DictionaryPreserveDuplicateObjects()
        {
            Dictionary<string, Employee> root = new Dictionary<string, Employee>
            {
                ["Employee1"] = new Employee { Name = "Angela" }
            };
            root["Employee2"] = root["Employee1"];

            string expected = JsonConvert.SerializeObject(root, s_newtonsoftSerializerSettingsPreserve);
            string actual = JsonSerializer.Serialize(root, s_serializerOptionsPreserve);

            Assert.Equal(expected, actual);

            Dictionary<string, Employee> rootCopy = JsonSerializer.Deserialize<Dictionary<string, Employee>>(actual, s_serializerOptionsPreserve);
            Assert.Same(rootCopy["Employee1"], rootCopy["Employee2"]);
        }

        [Fact]
        public static void DictionaryZeroLengthKey()
        {
            // Default

            Dictionary<string, int> rootValue = RoundTripDictionaryZeroLengthKey(new Dictionary<string, int>(), 10);
            Assert.Equal(10, rootValue[string.Empty]);

            Dictionary<string, Employee> rootObject = RoundTripDictionaryZeroLengthKey(new Dictionary<string, Employee>(), new Employee { Name = "Test" });
            Assert.Equal("Test", rootObject[string.Empty].Name);

            Dictionary<string, List<int>> rootArray = RoundTripDictionaryZeroLengthKey(new Dictionary<string, List<int>>(), new List<int>());
            Assert.Equal(0, rootArray[string.Empty].Count);

            // Preserve

            Dictionary<string, int> rootValue2 = RoundTripDictionaryZeroLengthKey(new Dictionary<string, int>(), 10, s_deserializerOptionsPreserve);
            Assert.Equal(10, rootValue2[string.Empty]);

            Dictionary<string, Employee> rootObject2 = RoundTripDictionaryZeroLengthKey(new Dictionary<string, Employee>(), new Employee { Name = "Test" }, s_deserializerOptionsPreserve);
            Assert.Equal("Test", rootObject2[string.Empty].Name);

            Dictionary<string, List<int>> rootArray2 = RoundTripDictionaryZeroLengthKey(new Dictionary<string, List<int>>(), new List<int>(), s_deserializerOptionsPreserve);
            Assert.Equal(0, rootArray2[string.Empty].Count);
        }

        private static Dictionary<string, TValue> RoundTripDictionaryZeroLengthKey<TValue>(Dictionary<string, TValue> dictionary, TValue value, JsonSerializerOptions opts = null)
        {
            dictionary[string.Empty] = value;
            string json = JsonSerializer.Serialize(dictionary, opts);
            Assert.Contains("\"\":", json);

            return JsonSerializer.Deserialize<Dictionary<string, TValue>>(json, opts);
        }

        [Fact]
        public static void UnicodeDictionaryKeys()
        {
            Dictionary<string, int> obj = new Dictionary<string, int> { { "A\u0467", 1 } };
            // Verify the name is escaped after serialize.
            string json = JsonSerializer.Serialize(obj, s_serializerOptionsPreserve);
            Assert.Equal(@"{""$id"":""1"",""A\u0467"":1}", json);

            // Round-trip
            Dictionary<string, int> objCopy = JsonSerializer.Deserialize<Dictionary<string, int>>(json, s_serializerOptionsPreserve);
            Assert.Equal(1, objCopy["A\u0467"]);

            // Verify with encoder.
            var optionsWithEncoder = new JsonSerializerOptions
            {
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                ReferenceHandling = ReferenceHandling.Preserve
            };
            json = JsonSerializer.Serialize(obj, optionsWithEncoder);
            Assert.Equal("{\"$id\":\"1\",\"A\u0467\":1}", json);

            // Round-trip
            objCopy = JsonSerializer.Deserialize<Dictionary<string, int>>(json, optionsWithEncoder);
            Assert.Equal(1, objCopy["A\u0467"]);

            // We want to go over StackallocThreshold=256 to force a pooled allocation, so this property is 200 chars and 400 bytes.
            const int charsInProperty = 200;
            string longPropertyName = new string('\u0467', charsInProperty);
            obj = new Dictionary<string, int> { { $"{longPropertyName}", 1 } };
            Assert.Equal(1, obj[longPropertyName]);

            // Verify the name is escaped after serialize.
            json = JsonSerializer.Serialize(obj, s_serializerOptionsPreserve);

            // Duplicate the unicode character 'charsInProperty' times.
            string longPropertyNameEscaped = new StringBuilder().Insert(0, @"\u0467", charsInProperty).ToString();
            string expectedJson = $"{{\"$id\":\"1\",\"{longPropertyNameEscaped}\":1}}";
            Assert.Equal(expectedJson, json);

            // Round-trip
            objCopy = JsonSerializer.Deserialize<Dictionary<string, int>>(json, s_serializerOptionsPreserve);
            Assert.Equal(1, objCopy[longPropertyName]);

            // Verify the name is escaped after serialize.
            json = JsonSerializer.Serialize(obj, optionsWithEncoder);

            // Duplicate the unicode character 'charsInProperty' times.
            longPropertyNameEscaped = new StringBuilder().Insert(0, "\u0467", charsInProperty).ToString();
            expectedJson = $"{{\"$id\":\"1\",\"{longPropertyNameEscaped}\":1}}";
            Assert.Equal(expectedJson, json);

            // Round-trip
            objCopy = JsonSerializer.Deserialize<Dictionary<string, int>>(json, optionsWithEncoder);
            Assert.Equal(1, objCopy[longPropertyName]);
        }
        #endregion

        #region Root Array
        private class ListWithGenericCycle : List<ListWithGenericCycle> { }

        [Fact]
        public static void ArrayLoop()
        {
            ListWithGenericCycle root = new ListWithGenericCycle();
            root.Add(root);

            string expected = JsonConvert.SerializeObject(root, s_newtonsoftSerializerSettingsPreserve);
            string actual = JsonSerializer.Serialize(root, s_serializerOptionsPreserve);

            Assert.Equal(expected, actual);

            ListWithGenericCycle rootCopy = JsonSerializer.Deserialize<ListWithGenericCycle>(actual, s_serializerOptionsPreserve);
            Assert.Same(rootCopy, rootCopy[0]);

            // Duplicate reference
            root = new ListWithGenericCycle();
            root.Add(root);
            root.Add(root);
            root.Add(root);

            expected = JsonConvert.SerializeObject(root, s_newtonsoftSerializerSettingsPreserve);
            actual = JsonSerializer.Serialize(root, s_serializerOptionsPreserve);

            Assert.Equal(expected, actual);

            rootCopy = JsonSerializer.Deserialize<ListWithGenericCycle>(actual, s_serializerOptionsPreserve);
            Assert.Same(rootCopy, rootCopy[0]);
            Assert.Same(rootCopy, rootCopy[1]);
            Assert.Same(rootCopy, rootCopy[2]);
        }

        [Fact]
        public static void ArrayObjectLoop()
        {
            List<Employee> root = new List<Employee>();
            root.Add(new Employee() { Name = "Angela", Subordinates = root });

            string expected = JsonConvert.SerializeObject(root, s_newtonsoftSerializerSettingsPreserve);
            string actual = JsonSerializer.Serialize(root, s_serializerOptionsPreserve);

            Assert.Equal(expected, actual);

            List<Employee> rootCopy = JsonSerializer.Deserialize<List<Employee>>(actual, s_serializerOptionsPreserve);
            Assert.Same(rootCopy, rootCopy[0].Subordinates);
        }

        [Fact]
        public static void ArrayPreserveDuplicateObjects()
        {
            List<Employee> root = new List<Employee>
            {
                new Employee { Name = "Angela" }
            };
            root.Add(root[0]);

            string expected = JsonConvert.SerializeObject(root, s_newtonsoftSerializerSettingsPreserve);
            string actual = JsonSerializer.Serialize(root, s_serializerOptionsPreserve);

            Assert.Equal(expected, actual);

            List<Employee> rootCopy = JsonSerializer.Deserialize<List<Employee>>(actual, s_serializerOptionsPreserve);
            Assert.Same(rootCopy[0], rootCopy[1]);
        }

        private class ListWithGenericCycleWithinDictionary : List<Dictionary<string, ListWithGenericCycleWithinDictionary>> { }

        [Fact]
        public static void ArrayDictionaryLoop()
        {
            ListWithGenericCycleWithinDictionary root = new ListWithGenericCycleWithinDictionary();
            root.Add(new Dictionary<string, ListWithGenericCycleWithinDictionary> { { "Root", root } });

            string expected = JsonConvert.SerializeObject(root, s_newtonsoftSerializerSettingsPreserve);
            string actual = JsonSerializer.Serialize(root, s_serializerOptionsPreserve);

            Assert.Equal(expected, actual);

            ListWithGenericCycleWithinDictionary rootCopy = JsonSerializer.Deserialize<ListWithGenericCycleWithinDictionary>(actual, s_serializerOptionsPreserve);
            Assert.Same(rootCopy, rootCopy[0]["Root"]);
        }

        [Fact]
        public static void ArrayPreserveDuplicateDictionaries()
        {
            ListWithGenericCycleWithinDictionary root = new ListWithGenericCycleWithinDictionary
            {
                new Dictionary<string, ListWithGenericCycleWithinDictionary>()
            };
            root.Add(root[0]);

            string expected = JsonConvert.SerializeObject(root, s_newtonsoftSerializerSettingsPreserve);
            string actual = JsonSerializer.Serialize(root, s_serializerOptionsPreserve);

            Assert.Equal(expected, actual);

            ListWithGenericCycleWithinDictionary rootCopy = JsonSerializer.Deserialize<ListWithGenericCycleWithinDictionary>(actual, s_serializerOptionsPreserve);
            Assert.Same(rootCopy[0], rootCopy[1]);
        }
        #endregion
    }
}
