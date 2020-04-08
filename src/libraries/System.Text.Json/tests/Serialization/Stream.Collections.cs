// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace System.Text.Json.Serialization.Tests
{
    public static partial class StreamTests
    {
        [Fact]
        public static void HandleCollectionsAsync()
        {
            static void RunTest<TElement>()
            {
                foreach ((Type, int) pair in EnumerableTestData<TElement>())
                {
                    Type type = pair.Item1;
                    int bufferSize = pair.Item2;

                    // bufferSize * 0.9 is the threshold size from codebase, subtract 2 for [ or { characters, then create a 
                    // string containing (threshold - 2) amount of char 'a' which when written into output buffer produces buffer 
                    // which size equal to or very close to threshold size, then adding the string to the list, then adding a big 
                    // object to the list which changes depth of written json and should cause buffer flush.
                    int thresholdSize = (int)(bufferSize * 0.9 - 2);

                    object obj = GetPopulatedCollection<TElement>(type, thresholdSize);

                    var options = new JsonSerializerOptions
                    {
                        DefaultBufferSize = bufferSize,
                        WriteIndented = true
                    };

                    var optionsWithPreservedReferenceHandling = new JsonSerializerOptions(options)
                    {
                        ReferenceHandling = ReferenceHandling.Preserve
                    };

                    Type elementType = typeof(TElement);

                    Task[] tasks = new Task[2];
                    tasks[0] = Task.Run(async () => await PerformSerialization(obj, type, elementType, options));
                    tasks[1] = Task.Run(async () => await PerformSerialization(obj, type, elementType, optionsWithPreservedReferenceHandling));
                    Task.WaitAll(tasks);
                }
            }

            RunTest<string>();
            RunTest<ClassWithString>();
            RunTest<ImmutableStructWithString>();
        }

        private static async Task PerformSerialization(
            object obj,
            Type type,
            Type elementType,
            JsonSerializerOptions options)
        {
            string json = JsonSerializer.Serialize(obj, options);

            using (var memoryStream = new MemoryStream())
            {
                await JsonSerializer.SerializeAsync(memoryStream, obj, options);
                string jsonSerialized = Encoding.UTF8.GetString(memoryStream.ToArray());
                AssertJsonEqual(json, jsonSerialized);

                try
                {
                    memoryStream.Position = 0;
                    object deserialized = await JsonSerializer.DeserializeAsync(memoryStream, type, options);
                    AssertJsonEqual(json, JsonSerializer.Serialize(deserialized, options));
                }
                catch (NotSupportedException ex)
                {
                    Assert.True(GetTypesNotSupportedForDeserialization(elementType).Contains(type));
                    Assert.Contains(type.ToString(), ex.ToString());
                }
            }

            // Deserialize with extra whitespace
            string jsonWithWhiteSpace = GetPayloadWithWhiteSpace(json);

            using (var memoryStream = new MemoryStream(Encoding.UTF8.GetBytes(jsonWithWhiteSpace)))
            {
                try
                {
                    object deserialized = await JsonSerializer.DeserializeAsync(memoryStream, type, options);
                    AssertJsonEqual(json, JsonSerializer.Serialize(deserialized, options));
                }
                catch (NotSupportedException ex)
                {
                    Assert.True(GetTypesNotSupportedForDeserialization(elementType).Contains(type));
                    Assert.Contains(type.ToString(), ex.ToString());
                }
            }
        }

        private static object GetPopulatedCollection<TElement>(Type type, int stringLength)
        {
            TElement value = GetCollectionElement<TElement>(stringLength);

            if (type == typeof(TElement[]))
            {
                return GetArr_TypedElements(value);
            }
            else if (type == typeof(ImmutableList<TElement>))
            {
                return ImmutableList.CreateRange(GetArr_TypedElements(value));
            }
            else if (type == typeof(ImmutableStack<TElement>))
            {
                return ImmutableStack.CreateRange(GetArr_TypedElements(value));
            }
            else if (type == typeof(ImmutableDictionary<string, TElement>))
            {
                return ImmutableDictionary.CreateRange(GetDict_TypedElements(value));
            }
            else if (
                typeof(IDictionary<string, TElement>).IsAssignableFrom(type) ||
                typeof(IReadOnlyDictionary<string, TElement>).IsAssignableFrom(type) ||
                typeof(IDictionary).IsAssignableFrom(type))
            {
                return Activator.CreateInstance(type, new object[] { GetDict_TypedElements(value) });
            }
            else if (typeof(IEnumerable<TElement>).IsAssignableFrom(type))
            {
                return Activator.CreateInstance(type, new object[] { GetArr_TypedElements(value) });
            }
            else
            {
                return Activator.CreateInstance(type, new object[] { GetArr_BoxedElements(value) });
            }
        }

        private static void AssertJsonEqual(string expected, string actual)
        {
            AssertJsonEqual(JsonDocument.Parse(expected).RootElement, JsonDocument.Parse(actual).RootElement);
        }

        private static void AssertJsonEqual(JsonElement expected, JsonElement actual)
        {
            JsonValueKind valueKind = expected.ValueKind;
            Assert.Equal(valueKind, actual.ValueKind);

            switch (valueKind)
            {
                case JsonValueKind.Object:
                    var propertyNames = new HashSet<string>();

                    foreach (JsonProperty property in expected.EnumerateObject())
                    {
                        propertyNames.Append(property.Name);
                    }

                    foreach (JsonProperty property in actual.EnumerateObject())
                    {
                        propertyNames.Append(property.Name);
                    }

                    foreach (string name in propertyNames)
                    {
                        AssertJsonEqual(expected.GetProperty(name), actual.GetProperty(name));
                    }

                    break;
                case JsonValueKind.Array:
                    JsonElement.ArrayEnumerator expectedEnumerator = actual.EnumerateArray();
                    JsonElement.ArrayEnumerator actualEnumerator = expected.EnumerateArray();

                    while (expectedEnumerator.MoveNext() && actualEnumerator.MoveNext())
                    {
                        AssertJsonEqual(expectedEnumerator.Current, actualEnumerator.Current);
                    }

                    if (expected.GetRawText() != actual.GetRawText())
                    {
                        Console.WriteLine(expected.GetRawText());
                        Console.WriteLine(actual.GetRawText());
                        Console.WriteLine("===");
                    }

                    //actualEnumerator.MoveNext();
                    while (actualEnumerator.MoveNext())
                    {
                        Console.WriteLine(actualEnumerator.Current.GetRawText());
                    }
                    break;
                case JsonValueKind.String:
                    Assert.Equal(expected.GetString(), actual.GetString());
                    break;
                default:
                    throw new NotImplementedException();
            }
        }

        private static string GetPayloadWithWhiteSpace(string json) => json.Replace(" ", new string(' ', 4));

        private const int NumElements = 40;

        private static TElement[] GetArr_TypedElements<TElement>(TElement value) => Enumerable.Repeat(value, NumElements).ToArray();

        private static object[] GetArr_BoxedElements(object value) => Enumerable.Repeat(value, NumElements).ToArray();

        private static Dictionary<string, TElement> GetDict_TypedElements<TElement>(TElement value)
        {
            var dict = new Dictionary<string, TElement>();
            for (int i = 0; i < NumElements; i++)
            {
                dict[$"{value}{i}"] = value;
            }

            return dict;
        }

        private static TElement GetCollectionElement<TElement>(int stringLength)
        {
            Type type = typeof(TElement);

            string value = new string('v', stringLength);

            if (type == typeof(string))
            {
                return (TElement)(object)value;
            }
            else if (type == typeof(ClassWithString))
            {
                return (TElement)(object)new ClassWithString { MyString = value };
            }
            else
            {
                return (TElement)(object)new ImmutableStructWithString(myString: value);
            }

            throw new NotImplementedException();
        }

        private static IEnumerable<(Type, int)> EnumerableTestData<TElement>()
        {
            foreach (Type type in EnumerableTypes<TElement>())
            {
                foreach (int bufferSize in BufferSizes())
                {
                    yield return (type, bufferSize);
                }
            }
        }

        private static IEnumerable<int> BufferSizes()
        {
            yield return 128;
            yield return 1024;
            yield return 4096;
            yield return 8192;
            yield return 16384;
            yield return 65536;
        }

        private static IEnumerable<Type> EnumerableTypes<TElement>()
        {
            yield return typeof(TElement[]); // ArrayConverter
            yield return typeof(ConcurrentQueue<TElement>); // ConcurrentQueueOfTConverter
            yield return typeof(GenericICollectionWrapper<TElement>); // ICollectionOfTConverter
            yield return typeof(WrapperForIEnumerable); // IEnumerableConverter
            yield return typeof(WrapperForIReadOnlyCollectionOfT<TElement>); // IEnumerableOfTConverter
            yield return typeof(Queue); // IEnumerableWithAddMethodConverter
            yield return typeof(WrapperForIList); // IListConverter
            yield return typeof(Collection<TElement>); // IListOfTConverter
            yield return typeof(ImmutableList<TElement>); // ImmutableEnumerableOfTConverter
            yield return typeof(HashSet<TElement>); // ISetOfTConverter
            yield return typeof(List<TElement>); // ListOfTConverter
            yield return typeof(Queue<TElement>); // QueueOfTConverter
            //// Stack types
            yield return typeof(ConcurrentStack<TElement>); // ConcurrentStackOfTConverter
            yield return typeof(Stack); // IEnumerableWithAddMethodConverter
            yield return typeof(ImmutableStack<TElement>); // ImmutableEnumerableOfTConverter
            // Dictionary types
            foreach (Type type in DictionaryTypes<TElement>())
            {
                yield return type;
            }
        }

        private static IEnumerable<Type> DictionaryTypes<TElement>()
        {
            yield return typeof(Dictionary<string, TElement>); // DictionaryOfStringTValueConverter
            yield return typeof(Hashtable); // IDictionaryConverter
            yield return typeof(ConcurrentDictionary<string, TElement>); // IDictionaryOfStringTValueConverter
            yield return typeof(GenericIDictionaryWrapper<string, TElement>); // IDictionaryOfStringTValueConverter
            yield return typeof(ImmutableDictionary<string, TElement>); // ImmutableDictionaryOfStringTValueConverter
            yield return typeof(GenericIReadOnlyDictionaryWrapper<string, TElement>); // IReadOnlyDictionaryOfStringTValueConverter
        }

        private static HashSet<Type> GetTypesNotSupportedForDeserialization(Type elementType) => new HashSet<Type>
        {
            typeof(WrapperForIEnumerable),
            typeof(WrapperForIReadOnlyCollectionOfT<>).MakeGenericType(elementType),
            typeof(GenericIReadOnlyDictionaryWrapper<,>).MakeGenericType(typeof(string), elementType)
        };

        private class ClassWithString
        {
            public string MyString { get; set; }
        }

        private struct ImmutableStructWithString
        {
            public string MyString { get; }

            [JsonConstructor]
            public ImmutableStructWithString(string myString) => MyString = myString;
        }

        [Theory]
        [InlineData("")]
        [InlineData("}")]
        [InlineData("[")]
        [InlineData("]")]
        public static void DeserializeDictionaryStartsWithInvalidJson(string json)
        {
            foreach (Type type in DictionaryTypes<string>())
            {
                Assert.ThrowsAsync<JsonException>(async () =>
                {
                    using (var memoryStream = new MemoryStream(Encoding.UTF8.GetBytes(json)))
                    {
                        await JsonSerializer.DeserializeAsync(memoryStream, type);
                    }
                });
            }
        }
    }
}
