// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Text.Encodings.Web;
using System.Text.Json.Tests;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Xunit;

namespace System.Text.Json.Serialization.Tests
{
    public static partial class ReferenceHandlerTests
    {
        [Fact]
        public static void ThrowByDefaultOnLoop()
        {
            Employee a = new Employee();
            a.Manager = a;

            JsonException ex = Assert.Throws<JsonException>(() => JsonSerializer.Serialize(a));
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
                ReferenceHandler = ReferenceHandler.Preserve
            };
            json = JsonSerializer.Serialize(obj, optionsWithEncoder);
            Assert.StartsWith("{\"$id\":\"1\",", json);
            Assert.Contains("\"A\u0467\":1", json);

            // Round-trip
            objCopy = JsonSerializer.Deserialize<ClassWithUnicodeProperty>(json, optionsWithEncoder);
            Assert.Equal(1, objCopy.A\u0467);

            // We want to go over StackallocByteThreshold=256 to force a pooled allocation, so this property is 400 chars and 401 bytes.
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
                ReferenceHandler = ReferenceHandler.Preserve
            };
            json = JsonSerializer.Serialize(obj, optionsWithEncoder);
            Assert.Equal("{\"$id\":\"1\",\"A\u0467\":1}", json);

            // Round-trip
            objCopy = JsonSerializer.Deserialize<Dictionary<string, int>>(json, optionsWithEncoder);
            Assert.Equal(1, objCopy["A\u0467"]);

            // We want to go over StackallocByteThreshold=256 to force a pooled allocation, so this property is 200 chars and 400 bytes.
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

        #region ReferenceResolver
        [Fact]
        public static void CustomReferenceResolver()
        {
            string json = @"[
  {
    ""$id"": ""0b64ffdf-d155-44ad-9689-58d9adb137f3"",
    ""Name"": ""John Smith"",
    ""Spouse"": {
      ""$id"": ""ae3c399c-058d-431d-91b0-a36c266441b9"",
      ""Name"": ""Jane Smith"",
      ""Spouse"": {
        ""$ref"": ""0b64ffdf-d155-44ad-9689-58d9adb137f3""
      }
    }
  },
  {
    ""$ref"": ""ae3c399c-058d-431d-91b0-a36c266441b9""
  }
]";
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                ReferenceHandler = new ReferenceHandler<GuidReferenceResolver>()
            };
            ImmutableArray<PersonReference> people = JsonSerializer.Deserialize<ImmutableArray<PersonReference>>(json, options);

            Assert.Equal(2, people.Length);

            PersonReference john = people[0];
            PersonReference jane = people[1];

            Assert.Same(john, jane.Spouse);
            Assert.Same(jane, john.Spouse);

            Assert.Equal(json, JsonSerializer.Serialize(people, options), ignoreLineEndingDifferences: true);
        }

        [Fact]
        public static void CustomReferenceResolverPersistent()
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                ReferenceHandler = new PresistentGuidReferenceHandler
                {
                    // Re-use the same resolver instance across all (de)serialiations based on this options instance.
                    PersistentResolver = new GuidReferenceResolver()
                }
            };

            string json =
@"[
  {
    ""$id"": ""0b64ffdf-d155-44ad-9689-58d9adb137f3"",
    ""Name"": ""John Smith"",
    ""Spouse"": {
      ""$id"": ""ae3c399c-058d-431d-91b0-a36c266441b9"",
      ""Name"": ""Jane Smith"",
      ""Spouse"": {
        ""$ref"": ""0b64ffdf-d155-44ad-9689-58d9adb137f3""
      }
    }
  },
  {
    ""$ref"": ""ae3c399c-058d-431d-91b0-a36c266441b9""
  }
]";
            ImmutableArray<PersonReference> firstListOfPeople = JsonSerializer.Deserialize<ImmutableArray<PersonReference>>(json, options);

            json =
@"[
  {
    ""$ref"": ""0b64ffdf-d155-44ad-9689-58d9adb137f3""
  },
  {
    ""$ref"": ""ae3c399c-058d-431d-91b0-a36c266441b9""
  }
]";
            ImmutableArray<PersonReference> secondListOfPeople = JsonSerializer.Deserialize<ImmutableArray<PersonReference>>(json, options);

            Assert.Same(firstListOfPeople[0], secondListOfPeople[0]);
            Assert.Same(firstListOfPeople[1], secondListOfPeople[1]);
            Assert.Same(firstListOfPeople[0].Spouse, secondListOfPeople[0].Spouse);
            Assert.Same(firstListOfPeople[1].Spouse, secondListOfPeople[1].Spouse);

            Assert.Equal(json, JsonSerializer.Serialize(secondListOfPeople, options), ignoreLineEndingDifferences: true);
        }

        internal class PresistentGuidReferenceHandler : ReferenceHandler
        {
            public ReferenceResolver PersistentResolver { get; set; }
            public override ReferenceResolver CreateResolver() => PersistentResolver;
        }

        public class GuidReferenceResolver : ReferenceResolver
        {
            private readonly IDictionary<Guid, PersonReference> _people = new Dictionary<Guid, PersonReference>();

            public override object ResolveReference(string referenceId)
            {
                Guid id = new Guid(referenceId);

                PersonReference p;
                _people.TryGetValue(id, out p);

                return p;
            }

            public override string GetReference(object value, out bool alreadyExists)
            {
                PersonReference p = (PersonReference)value;

                alreadyExists = _people.ContainsKey(p.Id);
                _people[p.Id] = p;

                return p.Id.ToString();
            }

            public override void AddReference(string reference, object value)
            {
                Guid id = new Guid(reference);
                PersonReference person = (PersonReference)value;
                person.Id = id;
                _people[id] = person;
            }
        }

        [Fact]
        public static void TestBadReferenceResolver()
        {
            var options = new JsonSerializerOptions { ReferenceHandler = new ReferenceHandler<BadReferenceResolver>() };

            PersonReference angela = new PersonReference { Name = "Angela" };
            PersonReference bob = new PersonReference { Name = "Bob" };

            angela.Spouse = bob;
            bob.Spouse = angela;

            // Nothing is preserved, hence MaxDepth will be reached.
            Assert.Throws<JsonException>(() => JsonSerializer.Serialize(angela, options));
        }

        class BadReferenceResolver : ReferenceResolver
        {
            private int _count;
            public override void AddReference(string referenceId, object value)
            {
                throw new NotImplementedException();
            }

            public override string GetReference(object value, out bool alreadyExists)
            {
                alreadyExists = false;
                _count++;

                return _count.ToString();
            }

            public override object ResolveReference(string referenceId)
            {
                throw new NotImplementedException();
            }
        }
        #endregion

        [Fact]
        public static void PreserveReferenceOfTypeObject()
        {
            var root = new ClassWithObjectProperty();
            root.Child = new ClassWithObjectProperty();
            root.Sibling = root.Child;

            Assert.Same(root.Child, root.Sibling);

            string json = JsonSerializer.Serialize(root, s_serializerOptionsPreserve);

            ClassWithObjectProperty rootCopy = JsonSerializer.Deserialize<ClassWithObjectProperty>(json, s_serializerOptionsPreserve);
            Assert.Same(rootCopy.Child, rootCopy.Sibling);
        }

        [Fact]
        public static async Task PreserveReferenceOfTypeObjectAsync()
        {
            var root = new ClassWithObjectProperty();
            root.Child = new ClassWithObjectProperty();
            root.Sibling = root.Child;

            Assert.Same(root.Child, root.Sibling);

            var stream = new MemoryStream();
            await JsonSerializer.SerializeAsync(stream, root, s_serializerOptionsPreserve);
            stream.Position = 0;

            ClassWithObjectProperty rootCopy = await JsonSerializer.DeserializeAsync<ClassWithObjectProperty>(stream, s_serializerOptionsPreserve);
            Assert.Same(rootCopy.Child, rootCopy.Sibling);
        }

        [Fact]
        public static void PreserveReferenceOfTypeOfObjectOnCollection()
        {
            var root = new ClassWithListOfObjectProperty();
            root.Child = new ClassWithListOfObjectProperty();

            root.ListOfObjects = new List<object>();
            root.ListOfObjects.Add(root.Child);

            Assert.Same(root.Child, root.ListOfObjects[0]);

            string json = JsonSerializer.Serialize(root, s_serializerOptionsPreserve);
            ClassWithListOfObjectProperty rootCopy = JsonSerializer.Deserialize<ClassWithListOfObjectProperty>(json, s_serializerOptionsPreserve);
            Assert.Same(rootCopy.Child, rootCopy.ListOfObjects[0]);
        }

        [Fact]
        public static void DoNotPreserveReferenceWhenRefPropertyIsAbsent()
        {
            string json = @"{""Child"":{""$id"":""1""},""Sibling"":{""foo"":""1""}}";
            ClassWithObjectProperty root = JsonSerializer.Deserialize<ClassWithObjectProperty>(json);
            Assert.IsType<JsonElement>(root.Sibling);

            // $ref with any escaped character shall not be treated as metadata, hence Sibling must be JsonElement.
            json = @"{""Child"":{""$id"":""1""},""Sibling"":{""\\u0024ref"":""1""}}";
            root = JsonSerializer.Deserialize<ClassWithObjectProperty>(json);
            Assert.IsType<JsonElement>(root.Sibling);
        }

        [Fact]
        public static void VerifyValidationsOnPreservedReferenceOfTypeObject()
        {
            const string baseJson = @"{""Child"":{""$id"":""1""},""Sibling"":";

            // A JSON object that contains a '$ref' metadata property must not contain any other properties.
            string testJson = baseJson + @"{""foo"":""value"",""$ref"":""1""}}";
            JsonException ex = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<ClassWithObjectProperty>(testJson, s_serializerOptionsPreserve));
            Assert.Equal("$.Sibling", ex.Path);

            testJson = baseJson + @"{""$ref"":""1"",""bar"":""value""}}";
            ex = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<ClassWithObjectProperty>(testJson, s_serializerOptionsPreserve));
            Assert.Equal("$.Sibling", ex.Path);

            // The '$id' and '$ref' metadata properties must be JSON strings.
            testJson = baseJson + @"{""$ref"":1}}";
            ex = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<ClassWithObjectProperty>(testJson, s_serializerOptionsPreserve));
            Assert.Equal("$.Sibling", ex.Path);
        }

        private class ClassWithObjectProperty
        {
            public ClassWithObjectProperty Child { get; set; }
            public object Sibling { get; set; }
        }

        private class ClassWithListOfObjectProperty
        {
            public ClassWithListOfObjectProperty Child { get; set; }
            public List<object> ListOfObjects { get; set; }
        }
    }
}
