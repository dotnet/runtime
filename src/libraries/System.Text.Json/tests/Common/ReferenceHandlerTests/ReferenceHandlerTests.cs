// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Xunit;

namespace System.Text.Json.Serialization.Tests
{
    public abstract partial class ReferenceHandlerTests : SerializerTests
    {
        public ReferenceHandlerTests(JsonSerializerWrapper stringSerializer) : base(stringSerializer)
        {
        }

        [Fact]
        public virtual async Task ThrowByDefaultOnLoop()
        {
            Employee a = new Employee();
            a.Manager = a;

            await Assert.ThrowsAsync<JsonException>(() => Serializer.SerializeWrapper(a));
        }

        #region Root Object
        [Fact]
        public async Task ObjectLoop()
        {
            Employee angela = new Employee();
            angela.Manager = angela;

            // Compare parity with Newtonsoft.Json
            string expected = JsonConvert.SerializeObject(angela, s_newtonsoftSerializerSettingsPreserve);
            string actual = await Serializer.SerializeWrapper(angela, s_serializerOptionsPreserve);

            Assert.Equal(expected, actual);

            // Ensure round-trip
            Employee angelaCopy = await Serializer.DeserializeWrapper<Employee>(actual, s_serializerOptionsPreserve);
            Assert.Same(angelaCopy.Manager, angelaCopy);
        }

        [Fact]
        public async Task ObjectArrayLoop()
        {
            Employee angela = new Employee();
            angela.Subordinates = new List<Employee> { angela };

            string expected = JsonConvert.SerializeObject(angela, s_newtonsoftSerializerSettingsPreserve);
            string actual = await Serializer.SerializeWrapper(angela, s_serializerOptionsPreserve);

            Assert.Equal(expected, actual);

            Employee angelaCopy = await Serializer.DeserializeWrapper<Employee>(actual, s_serializerOptionsPreserve);
            Assert.Same(angelaCopy.Subordinates[0], angelaCopy);
        }

        [Fact]
        public async Task ObjectDictionaryLoop()
        {
            Employee angela = new Employee();
            angela.Contacts = new Dictionary<string, Employee> { { "555-5555", angela } };

            string expected = JsonConvert.SerializeObject(angela, s_newtonsoftSerializerSettingsPreserve);
            string actual = await Serializer.SerializeWrapper(angela, s_serializerOptionsPreserve);

            Assert.Equal(expected, actual);

            Employee angelaCopy = await Serializer.DeserializeWrapper<Employee>(actual, s_serializerOptionsPreserve);
            Assert.Same(angelaCopy.Contacts["555-5555"], angelaCopy);
        }

        [Fact]
        public async Task ObjectPreserveDuplicateObjects()
        {
            Employee angela = new Employee
            {
                Manager = new Employee { Name = "Bob" }
            };
            angela.Manager2 = angela.Manager;

            string expected = JsonConvert.SerializeObject(angela, s_newtonsoftSerializerSettingsPreserve);
            string actual = await Serializer.SerializeWrapper(angela, s_serializerOptionsPreserve);

            Assert.Equal(expected, actual);

            Employee angelaCopy = await Serializer.DeserializeWrapper<Employee>(actual, s_serializerOptionsPreserve);
            Assert.Same(angelaCopy.Manager, angelaCopy.Manager2);
        }

        [Fact]
        public async Task ObjectPreserveDuplicateDictionaries()
        {
            Employee angela = new Employee
            {
                Contacts = new Dictionary<string, Employee> { { "444-4444", new Employee { Name = "Bob" } } }
            };
            angela.Contacts2 = angela.Contacts;

            string expected = JsonConvert.SerializeObject(angela, s_newtonsoftSerializerSettingsPreserve);
            string actual = await Serializer.SerializeWrapper(angela, s_serializerOptionsPreserve);

            Assert.Equal(expected, actual);

            Employee angelaCopy = await Serializer.DeserializeWrapper<Employee>(actual, s_serializerOptionsPreserve);
            Assert.Same(angelaCopy.Contacts, angelaCopy.Contacts2);
        }

        [Fact]
        public async Task ObjectPreserveDuplicateArrays()
        {
            Employee angela = new Employee
            {
                Subordinates = new List<Employee> { new Employee { Name = "Bob" } }
            };
            angela.Subordinates2 = angela.Subordinates;

            string expected = JsonConvert.SerializeObject(angela, s_newtonsoftSerializerSettingsPreserve);
            string actual = await Serializer.SerializeWrapper(angela, s_serializerOptionsPreserve);

            Assert.Equal(expected, actual);

            Employee angelaCopy = await Serializer.DeserializeWrapper<Employee>(actual, s_serializerOptionsPreserve);
            Assert.Same(angelaCopy.Subordinates, angelaCopy.Subordinates2);
        }

        [Fact]
        public async Task KeyValuePairTest()
        {
            var kvp = new KeyValuePair<string, string>("key", "value");
            string json = await Serializer.SerializeWrapper(kvp, s_deserializerOptionsPreserve);
            KeyValuePair<string, string> kvp2 = await Serializer.DeserializeWrapper<KeyValuePair<string, string>>(json, s_deserializerOptionsPreserve);

            Assert.Equal(kvp.Key, kvp2.Key);
            Assert.Equal(kvp.Value, kvp2.Value);
        }

        public class ClassWithZeroLengthProperty<TValue>
        {
            [JsonPropertyName("")]
            public TValue ZeroLengthProperty { get; set; }
        }

        [Fact]
        public async Task ObjectZeroLengthProperty()
        {
            // Default

            ClassWithZeroLengthProperty<int> rootValue = await RoundTripZeroLengthProperty(new ClassWithZeroLengthProperty<int>(), 10);
            Assert.Equal(10, rootValue.ZeroLengthProperty);

            ClassWithZeroLengthProperty<Employee> rootObject = await RoundTripZeroLengthProperty(new ClassWithZeroLengthProperty<Employee>(), new Employee { Name = "Test" });
            Assert.Equal("Test", rootObject.ZeroLengthProperty.Name);

            ClassWithZeroLengthProperty<List<int>> rootArray = await RoundTripZeroLengthProperty(new ClassWithZeroLengthProperty<List<int>>(), new List<int>());
            Assert.Equal(0, rootArray.ZeroLengthProperty.Count);

            // Preserve

            ClassWithZeroLengthProperty<int> rootValue2 = await RoundTripZeroLengthProperty(new ClassWithZeroLengthProperty<int>(), 10, s_deserializerOptionsPreserve);
            Assert.Equal(10, rootValue2.ZeroLengthProperty);

            ClassWithZeroLengthProperty<Employee> rootObject2 = await RoundTripZeroLengthProperty(new ClassWithZeroLengthProperty<Employee>(), new Employee { Name = "Test" }, s_deserializerOptionsPreserve);
            Assert.Equal("Test", rootObject2.ZeroLengthProperty.Name);

            ClassWithZeroLengthProperty<List<int>> rootArray2 = await RoundTripZeroLengthProperty(new ClassWithZeroLengthProperty<List<int>>(), new List<int>(), s_deserializerOptionsPreserve);
            Assert.Equal(0, rootArray2.ZeroLengthProperty.Count);
        }

        private async Task<ClassWithZeroLengthProperty<TValue>> RoundTripZeroLengthProperty<TValue>(ClassWithZeroLengthProperty<TValue> obj, TValue value, JsonSerializerOptions opts = null)
        {
            obj.ZeroLengthProperty = value;
            string json = await Serializer.SerializeWrapper(obj, opts);
            Assert.Contains("\"\":", json);

            return await Serializer.DeserializeWrapper<ClassWithZeroLengthProperty<TValue>>(json, opts);
        }

        [Fact]
        public async Task UnicodePropertyNames()
        {
            ClassWithUnicodeProperty obj = new ClassWithUnicodeProperty
            {
                A\u0467 = 1
            };

            // Verify the name is escaped after serialize.
            string json = await Serializer.SerializeWrapper(obj, s_serializerOptionsPreserve);
            Assert.StartsWith("{\"$id\":\"1\",", json);
            Assert.Contains(@"""A\u0467"":1", json);

            // Round-trip
            ClassWithUnicodeProperty objCopy = await Serializer.DeserializeWrapper<ClassWithUnicodeProperty>(json, s_serializerOptionsPreserve);
            Assert.Equal(1, objCopy.A\u0467);

            // With custom escaper
            // Specifying encoder on options does not impact deserialize.
            var optionsWithEncoder = new JsonSerializerOptions
            {
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                ReferenceHandler = ReferenceHandler.Preserve
            };
            json = await Serializer.SerializeWrapper(obj, optionsWithEncoder);
            Assert.StartsWith("{\"$id\":\"1\",", json);
            Assert.Contains("\"A\u0467\":1", json);

            // Round-trip
            objCopy = await Serializer.DeserializeWrapper<ClassWithUnicodeProperty>(json, optionsWithEncoder);
            Assert.Equal(1, objCopy.A\u0467);

            // We want to go over StackallocByteThreshold=256 to force a pooled allocation, so this property is 400 chars and 401 bytes.
            obj = new ClassWithUnicodeProperty
            {
                A\u046734567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890 = 1
            };

            // Verify the name is escaped after serialize.
            json = await Serializer.SerializeWrapper(obj, s_serializerOptionsPreserve);
            Assert.StartsWith("{\"$id\":\"1\",", json);
            Assert.Contains(@"""A\u046734567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890"":1", json);

            // Round-trip
            objCopy = await Serializer.DeserializeWrapper<ClassWithUnicodeProperty>(json, s_serializerOptionsPreserve);
            Assert.Equal(1, objCopy.A\u046734567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890);

            // With custom escaper
            json = await Serializer.SerializeWrapper(obj, optionsWithEncoder);
            Assert.StartsWith("{\"$id\":\"1\",", json);
            Assert.Contains("\"A\u046734567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890\":1", json);

            // Round-trip
            objCopy = await Serializer.DeserializeWrapper<ClassWithUnicodeProperty>(json, optionsWithEncoder);
            Assert.Equal(1, objCopy.A\u046734567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890);
        }
        #endregion Root Object

        #region Root Dictionary
        public class DictionaryWithGenericCycle : Dictionary<string, DictionaryWithGenericCycle> { }

        [Fact]
        public async Task DictionaryLoop()
        {
            DictionaryWithGenericCycle root = new DictionaryWithGenericCycle();
            root["Self"] = root;
            root["Other"] = new DictionaryWithGenericCycle();

            string expected = JsonConvert.SerializeObject(root, s_newtonsoftSerializerSettingsPreserve);
            string actual = await Serializer.SerializeWrapper(root, s_serializerOptionsPreserve);

            Assert.Equal(expected, actual);

            DictionaryWithGenericCycle rootCopy = await Serializer.DeserializeWrapper<DictionaryWithGenericCycle>(actual, s_serializerOptionsPreserve);
            Assert.Same(rootCopy, rootCopy["Self"]);
        }

        [Fact]
        public async Task DictionaryPreserveDuplicateDictionaries()
        {
            DictionaryWithGenericCycle root = new DictionaryWithGenericCycle();
            root["Self1"] = root;
            root["Self2"] = root;

            string expected = JsonConvert.SerializeObject(root, s_newtonsoftSerializerSettingsPreserve);
            string actual = await Serializer.SerializeWrapper(root, s_serializerOptionsPreserve);

            Assert.Equal(expected, actual);

            DictionaryWithGenericCycle rootCopy = await Serializer.DeserializeWrapper<DictionaryWithGenericCycle>(actual, s_serializerOptionsPreserve);
            Assert.Same(rootCopy, rootCopy["Self1"]);
            Assert.Same(rootCopy, rootCopy["Self2"]);
        }

        [Fact]
        public async Task DictionaryObjectLoop()
        {
            Dictionary<string, Employee> root = new Dictionary<string, Employee>();
            root["Angela"] = new Employee() { Name = "Angela", Contacts = root };

            string expected = JsonConvert.SerializeObject(root, s_newtonsoftSerializerSettingsPreserve);
            string actual = await Serializer.SerializeWrapper(root, s_serializerOptionsPreserve);

            Assert.Equal(expected, actual);

            Dictionary<string, Employee> rootCopy = await Serializer.DeserializeWrapper<Dictionary<string, Employee>>(actual, s_serializerOptionsPreserve);
            Assert.Same(rootCopy, rootCopy["Angela"].Contacts);
        }

        public class DictionaryWithGenericCycleWithinList : Dictionary<string, List<DictionaryWithGenericCycleWithinList>> { }

        [Fact]
        public async Task DictionaryArrayLoop()
        {
            DictionaryWithGenericCycleWithinList root = new DictionaryWithGenericCycleWithinList();
            root["ArrayWithSelf"] = new List<DictionaryWithGenericCycleWithinList> { root };

            string expected = JsonConvert.SerializeObject(root, s_newtonsoftSerializerSettingsPreserve);
            string actual = await Serializer.SerializeWrapper(root, s_serializerOptionsPreserve);

            Assert.Equal(expected, actual);

            DictionaryWithGenericCycleWithinList rootCopy = await Serializer.DeserializeWrapper<DictionaryWithGenericCycleWithinList>(actual, s_serializerOptionsPreserve);
            Assert.Same(rootCopy, rootCopy["ArrayWithSelf"][0]);
        }

        [Fact]
        public async Task DictionaryPreserveDuplicateArrays()
        {
            DictionaryWithGenericCycleWithinList root = new DictionaryWithGenericCycleWithinList();
            root["Array1"] = new List<DictionaryWithGenericCycleWithinList> { root };
            root["Array2"] = root["Array1"];

            string expected = JsonConvert.SerializeObject(root, s_newtonsoftSerializerSettingsPreserve);
            string actual = await Serializer.SerializeWrapper(root, s_serializerOptionsPreserve);

            Assert.Equal(expected, actual);

            DictionaryWithGenericCycleWithinList rootCopy = await Serializer.DeserializeWrapper<DictionaryWithGenericCycleWithinList>(actual, s_serializerOptionsPreserve);
            Assert.Same(rootCopy, rootCopy["Array1"][0]);
            Assert.Same(rootCopy["Array2"], rootCopy["Array1"]);
        }

        [Fact]
        public async Task DictionaryPreserveDuplicateObjects()
        {
            Dictionary<string, Employee> root = new Dictionary<string, Employee>
            {
                ["Employee1"] = new Employee { Name = "Angela" }
            };
            root["Employee2"] = root["Employee1"];

            string expected = JsonConvert.SerializeObject(root, s_newtonsoftSerializerSettingsPreserve);
            string actual = await Serializer.SerializeWrapper(root, s_serializerOptionsPreserve);

            Assert.Equal(expected, actual);

            Dictionary<string, Employee> rootCopy = await Serializer.DeserializeWrapper<Dictionary<string, Employee>>(actual, s_serializerOptionsPreserve);
            Assert.Same(rootCopy["Employee1"], rootCopy["Employee2"]);
        }

        [Fact]
        public async Task DictionaryZeroLengthKey()
        {
            // Default

            Dictionary<string, int> rootValue = await RoundTripDictionaryZeroLengthKey(new Dictionary<string, int>(), 10);
            Assert.Equal(10, rootValue[string.Empty]);

            Dictionary<string, Employee> rootObject = await RoundTripDictionaryZeroLengthKey(new Dictionary<string, Employee>(), new Employee { Name = "Test" });
            Assert.Equal("Test", rootObject[string.Empty].Name);

            Dictionary<string, List<int>> rootArray = await RoundTripDictionaryZeroLengthKey(new Dictionary<string, List<int>>(), new List<int>());
            Assert.Equal(0, rootArray[string.Empty].Count);

            // Preserve

            Dictionary<string, int> rootValue2 = await RoundTripDictionaryZeroLengthKey(new Dictionary<string, int>(), 10, s_deserializerOptionsPreserve);
            Assert.Equal(10, rootValue2[string.Empty]);

            Dictionary<string, Employee> rootObject2 = await RoundTripDictionaryZeroLengthKey(new Dictionary<string, Employee>(), new Employee { Name = "Test" }, s_deserializerOptionsPreserve);
            Assert.Equal("Test", rootObject2[string.Empty].Name);

            Dictionary<string, List<int>> rootArray2 = await RoundTripDictionaryZeroLengthKey(new Dictionary<string, List<int>>(), new List<int>(), s_deserializerOptionsPreserve);
            Assert.Equal(0, rootArray2[string.Empty].Count);
        }

        private async Task<Dictionary<string, TValue>> RoundTripDictionaryZeroLengthKey<TValue>(Dictionary<string, TValue> dictionary, TValue value, JsonSerializerOptions opts = null)
        {
            dictionary[string.Empty] = value;
            string json = await Serializer.SerializeWrapper(dictionary, opts);
            Assert.Contains("\"\":", json);

            return await Serializer.DeserializeWrapper<Dictionary<string, TValue>>(json, opts);
        }

        [Fact]
        public async Task UnicodeDictionaryKeys()
        {
            Dictionary<string, int> obj = new Dictionary<string, int> { { "A\u0467", 1 } };
            // Verify the name is escaped after serialize.
            string json = await Serializer.SerializeWrapper(obj, s_serializerOptionsPreserve);
            Assert.Equal(@"{""$id"":""1"",""A\u0467"":1}", json);

            // Round-trip
            Dictionary<string, int> objCopy = await Serializer.DeserializeWrapper<Dictionary<string, int>>(json, s_serializerOptionsPreserve);
            Assert.Equal(1, objCopy["A\u0467"]);

            // Verify with encoder.
            var optionsWithEncoder = new JsonSerializerOptions
            {
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                ReferenceHandler = ReferenceHandler.Preserve
            };
            json = await Serializer.SerializeWrapper(obj, optionsWithEncoder);
            Assert.Equal("{\"$id\":\"1\",\"A\u0467\":1}", json);

            // Round-trip
            objCopy = await Serializer.DeserializeWrapper<Dictionary<string, int>>(json, optionsWithEncoder);
            Assert.Equal(1, objCopy["A\u0467"]);

            // We want to go over StackallocByteThreshold=256 to force a pooled allocation, so this property is 200 chars and 400 bytes.
            const int charsInProperty = 200;
            string longPropertyName = new string('\u0467', charsInProperty);
            obj = new Dictionary<string, int> { { $"{longPropertyName}", 1 } };
            Assert.Equal(1, obj[longPropertyName]);

            // Verify the name is escaped after serialize.
            json = await Serializer.SerializeWrapper(obj, s_serializerOptionsPreserve);

            // Duplicate the unicode character 'charsInProperty' times.
            string longPropertyNameEscaped = new StringBuilder().Insert(0, @"\u0467", charsInProperty).ToString();
            string expectedJson = $"{{\"$id\":\"1\",\"{longPropertyNameEscaped}\":1}}";
            Assert.Equal(expectedJson, json);

            // Round-trip
            objCopy = await Serializer.DeserializeWrapper<Dictionary<string, int>>(json, s_serializerOptionsPreserve);
            Assert.Equal(1, objCopy[longPropertyName]);

            // Verify the name is escaped after serialize.
            json = await Serializer.SerializeWrapper(obj, optionsWithEncoder);

            // Duplicate the unicode character 'charsInProperty' times.
            longPropertyNameEscaped = new StringBuilder().Insert(0, "\u0467", charsInProperty).ToString();
            expectedJson = $"{{\"$id\":\"1\",\"{longPropertyNameEscaped}\":1}}";
            Assert.Equal(expectedJson, json);

            // Round-trip
            objCopy = await Serializer.DeserializeWrapper<Dictionary<string, int>>(json, optionsWithEncoder);
            Assert.Equal(1, objCopy[longPropertyName]);
        }
        #endregion

        #region Root Array
        public class ListWithGenericCycle : List<ListWithGenericCycle> { }

        [Fact]
        public async Task ArrayLoop()
        {
            ListWithGenericCycle root = new ListWithGenericCycle();
            root.Add(root);

            string expected = JsonConvert.SerializeObject(root, s_newtonsoftSerializerSettingsPreserve);
            string actual = await Serializer.SerializeWrapper(root, s_serializerOptionsPreserve);

            Assert.Equal(expected, actual);

            ListWithGenericCycle rootCopy = await Serializer.DeserializeWrapper<ListWithGenericCycle>(actual, s_serializerOptionsPreserve);
            Assert.Same(rootCopy, rootCopy[0]);

            // Duplicate reference
            root = new ListWithGenericCycle();
            root.Add(root);
            root.Add(root);
            root.Add(root);

            expected = JsonConvert.SerializeObject(root, s_newtonsoftSerializerSettingsPreserve);
            actual = await Serializer.SerializeWrapper(root, s_serializerOptionsPreserve);

            Assert.Equal(expected, actual);

            rootCopy = await Serializer.DeserializeWrapper<ListWithGenericCycle>(actual, s_serializerOptionsPreserve);
            Assert.Same(rootCopy, rootCopy[0]);
            Assert.Same(rootCopy, rootCopy[1]);
            Assert.Same(rootCopy, rootCopy[2]);
        }

        [Fact]
        public async Task ArrayObjectLoop()
        {
            List<Employee> root = new List<Employee>();
            root.Add(new Employee() { Name = "Angela", Subordinates = root });

            string expected = JsonConvert.SerializeObject(root, s_newtonsoftSerializerSettingsPreserve);
            string actual = await Serializer.SerializeWrapper(root, s_serializerOptionsPreserve);

            Assert.Equal(expected, actual);

            List<Employee> rootCopy = await Serializer.DeserializeWrapper<List<Employee>>(actual, s_serializerOptionsPreserve);
            Assert.Same(rootCopy, rootCopy[0].Subordinates);
        }

        [Fact]
        public async Task ArrayPreserveDuplicateObjects()
        {
            List<Employee> root = new List<Employee>
            {
                new Employee { Name = "Angela" }
            };
            root.Add(root[0]);

            string expected = JsonConvert.SerializeObject(root, s_newtonsoftSerializerSettingsPreserve);
            string actual = await Serializer.SerializeWrapper(root, s_serializerOptionsPreserve);

            Assert.Equal(expected, actual);

            List<Employee> rootCopy = await Serializer.DeserializeWrapper<List<Employee>>(actual, s_serializerOptionsPreserve);
            Assert.Same(rootCopy[0], rootCopy[1]);
        }

        public class ListWithGenericCycleWithinDictionary : List<Dictionary<string, ListWithGenericCycleWithinDictionary>> { }

        [Fact]
        public async Task ArrayDictionaryLoop()
        {
            ListWithGenericCycleWithinDictionary root = new ListWithGenericCycleWithinDictionary();
            root.Add(new Dictionary<string, ListWithGenericCycleWithinDictionary> { { "Root", root } });

            string expected = JsonConvert.SerializeObject(root, s_newtonsoftSerializerSettingsPreserve);
            string actual = await Serializer.SerializeWrapper(root, s_serializerOptionsPreserve);

            Assert.Equal(expected, actual);

            ListWithGenericCycleWithinDictionary rootCopy = await Serializer.DeserializeWrapper<ListWithGenericCycleWithinDictionary>(actual, s_serializerOptionsPreserve);
            Assert.Same(rootCopy, rootCopy[0]["Root"]);
        }

        [Fact]
        public async Task ArrayPreserveDuplicateDictionaries()
        {
            ListWithGenericCycleWithinDictionary root = new ListWithGenericCycleWithinDictionary
            {
                new Dictionary<string, ListWithGenericCycleWithinDictionary>()
            };
            root.Add(root[0]);

            string expected = JsonConvert.SerializeObject(root, s_newtonsoftSerializerSettingsPreserve);
            string actual = await Serializer.SerializeWrapper(root, s_serializerOptionsPreserve);

            Assert.Equal(expected, actual);

            ListWithGenericCycleWithinDictionary rootCopy = await Serializer.DeserializeWrapper<ListWithGenericCycleWithinDictionary>(actual, s_serializerOptionsPreserve);
            Assert.Same(rootCopy[0], rootCopy[1]);
        }
        #endregion

        #region ReferenceResolver
        [Fact]
        public async Task CustomReferenceResolver()
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
            ImmutableArray<PersonReference> people = await Serializer.DeserializeWrapper<ImmutableArray<PersonReference>>(json, options);

            Assert.Equal(2, people.Length);

            PersonReference john = people[0];
            PersonReference jane = people[1];

            Assert.Same(john, jane.Spouse);
            Assert.Same(jane, john.Spouse);

            Assert.Equal(json, await Serializer.SerializeWrapper(people, options), ignoreLineEndingDifferences: true);
        }

        [Fact]
        public async Task CustomReferenceResolverPersistent()
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
            ImmutableArray<PersonReference> firstListOfPeople = await Serializer.DeserializeWrapper<ImmutableArray<PersonReference>>(json, options);

            json =
@"[
  {
    ""$ref"": ""0b64ffdf-d155-44ad-9689-58d9adb137f3""
  },
  {
    ""$ref"": ""ae3c399c-058d-431d-91b0-a36c266441b9""
  }
]";
            ImmutableArray<PersonReference> secondListOfPeople = await Serializer.DeserializeWrapper<ImmutableArray<PersonReference>>(json, options);

            Assert.Same(firstListOfPeople[0], secondListOfPeople[0]);
            Assert.Same(firstListOfPeople[1], secondListOfPeople[1]);
            Assert.Same(firstListOfPeople[0].Spouse, secondListOfPeople[0].Spouse);
            Assert.Same(firstListOfPeople[1].Spouse, secondListOfPeople[1].Spouse);

            Assert.Equal(json, await Serializer.SerializeWrapper(secondListOfPeople, options), ignoreLineEndingDifferences: true);
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
        public async Task TestBadReferenceResolver()
        {
            var options = new JsonSerializerOptions { ReferenceHandler = new ReferenceHandler<BadReferenceResolver>() };

            PersonReference angela = new PersonReference { Name = "Angela" };
            PersonReference bob = new PersonReference { Name = "Bob" };

            angela.Spouse = bob;
            bob.Spouse = angela;

            // Nothing is preserved, hence MaxDepth will be reached.
            await Assert.ThrowsAsync<JsonException>(() => Serializer.SerializeWrapper(angela, options));
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
        public async Task PreserveReferenceOfTypeObject()
        {
            var root = new ClassWithObjectProperty();
            root.Child = new ClassWithObjectProperty();
            root.Sibling = root.Child;

            Assert.Same(root.Child, root.Sibling);

            string json = await Serializer.SerializeWrapper(root, s_serializerOptionsPreserve);

            ClassWithObjectProperty rootCopy = await Serializer.DeserializeWrapper<ClassWithObjectProperty>(json, s_serializerOptionsPreserve);
            Assert.Same(rootCopy.Child, rootCopy.Sibling);
        }

        [Fact]
        public async Task PreserveReferenceOfTypeOfObjectOnCollection()
        {
            var root = new ClassWithListOfObjectProperty();
            root.Child = new ClassWithListOfObjectProperty();

            root.ListOfObjects = new List<object>();
            root.ListOfObjects.Add(root.Child);

            Assert.Same(root.Child, root.ListOfObjects[0]);

            string json = await Serializer.SerializeWrapper(root, s_serializerOptionsPreserve);
            ClassWithListOfObjectProperty rootCopy = await Serializer.DeserializeWrapper<ClassWithListOfObjectProperty>(json, s_serializerOptionsPreserve);
            Assert.Same(rootCopy.Child, rootCopy.ListOfObjects[0]);
        }

        [Fact]
        public async Task DoNotPreserveReferenceWhenRefPropertyIsAbsent()
        {
            string json = @"{""Child"":{""$id"":""1""},""Sibling"":{""foo"":""1""}}";
            ClassWithObjectProperty root = await Serializer.DeserializeWrapper<ClassWithObjectProperty>(json);
            Assert.IsType<JsonElement>(root.Sibling);

            // $ref with any escaped character shall not be treated as metadata, hence Sibling must be JsonElement.
            json = @"{""Child"":{""$id"":""1""},""Sibling"":{""\\u0024ref"":""1""}}";
            root = await Serializer.DeserializeWrapper<ClassWithObjectProperty>(json);
            Assert.IsType<JsonElement>(root.Sibling);
        }

        [Fact]
        public async Task VerifyValidationsOnPreservedReferenceOfTypeObject()
        {
            const string baseJson = @"{""Child"":{""$id"":""1""},""Sibling"":";

            // A JSON object that contains a '$ref' metadata property must not contain any other properties.
            string testJson = baseJson + @"{""foo"":""value"",""$ref"":""1""}}";
            JsonException ex = await Assert.ThrowsAsync<JsonException>(async () => await Serializer.DeserializeWrapper<ClassWithObjectProperty>(testJson, s_serializerOptionsPreserve));
            Assert.Equal("$.Sibling", ex.Path);

            testJson = baseJson + @"{""$ref"":""1"",""bar"":""value""}}";
            ex = await Assert.ThrowsAsync<JsonException>(async () => await Serializer.DeserializeWrapper<ClassWithObjectProperty>(testJson, s_serializerOptionsPreserve));
            Assert.Equal("$.Sibling", ex.Path);

            // The '$id' and '$ref' metadata properties must be JSON strings.
            testJson = baseJson + @"{""$ref"":1}}";
            ex = await Assert.ThrowsAsync<JsonException>(async () => await Serializer.DeserializeWrapper<ClassWithObjectProperty>(testJson, s_serializerOptionsPreserve));
            Assert.Equal("$.Sibling", ex.Path);
        }

        [Fact]
        public async Task BoxedStructReferencePreservation_NestedStructObject()
        {
            IBoxedStructWithObjectProperty value = new StructWithObjectProperty();
            value.Property = new object[] { value };

            string json = await Serializer.SerializeWrapper(value, new JsonSerializerOptions { ReferenceHandler = ReferenceHandler.Preserve });
            Assert.Equal(@"{""$id"":""1"",""Property"":[{""$ref"":""1""}]}", json);
        }

        [Fact]
        public async Task BoxedStructReferencePreservation_NestedStructCollection()
        {
            IBoxedStructWithObjectProperty value = new StructCollection();
            value.Property = new object[] { value };

            string json = await Serializer.SerializeWrapper(value, s_serializerOptionsPreserve);
            Assert.Equal(@"{""$id"":""1"",""Property"":[{""$ref"":""1""}]}", json);
        }

        [Fact]
        public async Task BoxedStructReferencePreservation_SiblingStructObjects()
        {
            object box = new StructWithObjectProperty { Property = 42 };
            var array = new object[] { box, box };

            string json = await Serializer.SerializeWrapper(array, s_serializerOptionsPreserve);
            Assert.Equal(@"[{""$id"":""1"",""Property"":42},{""$ref"":""1""}]", json);
        }

        [Fact]
        public async Task BoxedStructReferencePreservation_SiblingStructCollections()
        {
            object box = new StructCollection { Property = 42 };
            var array = new object[] { box, box };

            string json = await Serializer.SerializeWrapper(array, s_serializerOptionsPreserve);
            Assert.Equal(@"[{""$id"":""1"",""$values"":[42]},{""$ref"":""1""}]", json);
        }

        [Fact]
        public async Task BoxedStructReferencePreservation_SiblingPrimitiveValues()
        {
            object box = 42;
            var array = new object[] { box, box };

            string json = await Serializer.SerializeWrapper(array, s_serializerOptionsPreserve);
            Assert.Equal(@"[42,42]", json);
        }

        public class ClassWithObjectProperty
        {
            public ClassWithObjectProperty Child { get; set; }
            public object Sibling { get; set; }
        }

        public class ClassWithListOfObjectProperty
        {
            public ClassWithListOfObjectProperty Child { get; set; }
            public List<object> ListOfObjects { get; set; }
        }

        public interface IBoxedStructWithObjectProperty
        {
            object? Property { get; set; }
        }

        public struct StructWithObjectProperty : IBoxedStructWithObjectProperty
        {
            public object? Property { get; set; }
        }

        public struct StructCollection : IBoxedStructWithObjectProperty, IEnumerable<object>
        {
            public object Property { get; set; }

            public IEnumerator<object> GetEnumerator()
            {
                yield return Property;
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }
    }
}
