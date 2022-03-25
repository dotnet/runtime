// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace System.Text.Json.Serialization.Tests
{
    public partial class StreamTests
    {
        // Empty class functioning as witness type for TElement
        public class Witness<T> { }

        [Theory]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/35927", typeof(PlatformDetection), nameof(PlatformDetection.IsMonoInterpreter))]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/35927", TestPlatforms.Browser)]
        [MemberData(nameof(GetTestedCollectionData))]
        public async Task HandleCollectionsAsync<TCollection, TElement>(TCollection collection, int bufferSize, Witness<TElement> elementType)
        {
            _ = elementType; // only needed by Xunit to inject the right TElement type parameter

            var options = new JsonSerializerOptions { DefaultBufferSize = bufferSize };
            await PerformSerialization<TCollection, TElement>(collection, options);

            options = new JsonSerializerOptions(options) { ReferenceHandler = ReferenceHandler.Preserve };
            await PerformSerialization<TCollection, TElement>(collection, options);
        }

        private async Task PerformSerialization<TCollection, TElement>(
            TCollection collection,
            JsonSerializerOptions options)
        {
            string expectedjson = JsonSerializer.Serialize(collection, options);
            string actualJson = await Serializer.SerializeWrapper(collection, options);
            JsonTestHelper.AssertJsonEqual(expectedjson, actualJson);

            if (options.ReferenceHandler == ReferenceHandler.Preserve &&
                TypeHelper<TElement>.NonRoundtrippableWithReferenceHandler.Contains(typeof(TCollection)))
            {
                return;
            }

            await TestDeserialization<TCollection, TElement>(actualJson, options);

            // Deserialize with extra whitespace
            string jsonWithWhiteSpace = GetPayloadWithWhiteSpace(actualJson);
            await TestDeserialization<TCollection, TElement>(jsonWithWhiteSpace, options);
        }

        private async Task TestDeserialization<TCollection, TElement>(string json, JsonSerializerOptions options)
        {
            if (TypeHelper<TElement>.NotSupportedForDeserialization.Contains(typeof(TCollection)))
            {
                NotSupportedException exception = await Assert.ThrowsAsync<NotSupportedException>(() => Serializer.DeserializeWrapper<TCollection>(json, options));
                Assert.Contains(typeof(TCollection).ToString(), exception.ToString());
                return;
            }

            TCollection deserialized = await Serializer.DeserializeWrapper<TCollection>(json, options);

            // Validate the integrity of the deserialized value by reserializing
            // it using the non-streaming serializer and comparing the roundtripped value.
            string roundtrippedJson = JsonSerializer.Serialize(deserialized, options);

            // Stack elements reversed during serialization.
            if (TypeHelper<TElement>.StackTypes.Contains(typeof(TCollection)))
            {
                deserialized = JsonSerializer.Deserialize<TCollection>(roundtrippedJson, options);
                roundtrippedJson = JsonSerializer.Serialize(deserialized, options);
            }

            // TODO: https://github.com/dotnet/runtime/issues/35611.
            // Can't control order of dictionary elements when serializing, so reference metadata might not match up.
            if (options.ReferenceHandler == ReferenceHandler.Preserve &&
                TypeHelper<TElement>.DictionaryTypes.Contains(typeof(TCollection)))
            {
                return;
            }

            JsonTestHelper.AssertJsonEqual(json, roundtrippedJson);
        }

        public static IEnumerable<object[]> GetTestedCollectionData()
        {
            return new IEnumerable<object[]>[] {
                GetTestedCollectionsForElement<string>(),
                GetTestedCollectionsForElement<ClassWithKVP>(),
                GetTestedCollectionsForElement<ImmutableStructWithStrings>(),
            }.SelectMany(x => x);

            static IEnumerable<object[]> GetTestedCollectionsForElement<TElement>()
            {
                foreach ((Type collectionType, int bufferSize) in CollectionTestData<TElement>())
                {
                    // bufferSize * 0.9 is the threshold size from codebase, subtract 2 for [ or { characters, then create a
                    // string containing (threshold - 2) amount of char 'a' which when written into output buffer produces buffer
                    // which size equal to or very close to threshold size, then adding the string to the list, then adding a big
                    // object to the list which changes depth of written json and should cause buffer flush.
                    int elementSize = (int)(bufferSize * 0.9 - 2);
                    object collection = GetPopulatedCollection<TElement>(collectionType, elementSize);
                    yield return new object[] { collection, bufferSize, new Witness<TElement>() };
                }
            }
        }

        private static object GetPopulatedCollection<TElement>(Type collectionType, int elementSize)
        {
            if (collectionType == typeof(TElement[]))
            {
                return GetArr_TypedElements<TElement>(elementSize);
            }
            else if (collectionType == typeof(ImmutableList<TElement>))
            {
                return ImmutableList.CreateRange(GetArr_TypedElements<TElement>(elementSize));
            }
            else if (collectionType == typeof(ImmutableStack<TElement>))
            {
                return ImmutableStack.CreateRange(GetArr_TypedElements<TElement>(elementSize));
            }
            else if (collectionType == typeof(ImmutableDictionary<string, TElement>))
            {
                return ImmutableDictionary.CreateRange(GetDict_TypedElements<TElement>(elementSize));
            }
            else if (collectionType == typeof(KeyValuePair<TElement, TElement>))
            {
                TElement item = GetCollectionElement<TElement>(elementSize);
                return new KeyValuePair<TElement, TElement>(item, item);
            }
            else if (
                typeof(IDictionary<string, TElement>).IsAssignableFrom(collectionType) ||
                typeof(IReadOnlyDictionary<string, TElement>).IsAssignableFrom(collectionType) ||
                typeof(IDictionary).IsAssignableFrom(collectionType))
            {
                return Activator.CreateInstance(collectionType, new object[] { GetDict_TypedElements<TElement>(elementSize) });
            }
            else if (typeof(IEnumerable<TElement>).IsAssignableFrom(collectionType))
            {
                return Activator.CreateInstance(collectionType, new object[] { GetArr_TypedElements<TElement>(elementSize) });
            }
            else
            {
                return Activator.CreateInstance(collectionType, new object[] { GetArr_BoxedElements<TElement>(elementSize) });
            }
        }

        private static object GetEmptyCollection<TElement>(Type type)
        {
            if (type == typeof(TElement[]))
            {
                return Array.Empty<TElement>();
            }
            else if (type == typeof(ImmutableList<TElement>))
            {
                return ImmutableList.CreateRange(Array.Empty<TElement>());
            }
            else if (type == typeof(ImmutableStack<TElement>))
            {
                return ImmutableStack.CreateRange(Array.Empty<TElement>());
            }
            else if (type == typeof(ImmutableDictionary<string, TElement>))
            {
                return ImmutableDictionary.CreateRange(new Dictionary<string, TElement>());
            }
            else
            {
                return Activator.CreateInstance(type);
            }
        }

        private static string GetPayloadWithWhiteSpace(string json) => json.Replace("  ", new string(' ', 8));

        private const int NumElements = 15;

        private static TElement[] GetArr_TypedElements<TElement>(int elementSize)
        {
            Debug.Assert(NumElements > 2);
            var arr = new TElement[NumElements];

            Random random = new Random(Seed: elementSize);
            TElement item = GetCollectionElement<TElement>(elementSize, random);
            arr[0] = item;

            for (int i = 1; i < NumElements - 1; i++)
            {
                arr[i] = GetCollectionElement<TElement>(elementSize, random);
            }

            arr[NumElements - 1] = item;

            return arr;
        }

        private static object[] GetArr_BoxedElements<TElement>(int elementSize)
        {
            Debug.Assert(NumElements > 2);
            var arr = new object[NumElements];

            Random random = new Random(Seed: elementSize);
            TElement item = GetCollectionElement<TElement>(elementSize, random);
            arr[0] = item;

            for (int i = 1; i < NumElements - 1; i++)
            {
                arr[i] = GetCollectionElement<TElement>(elementSize, random);
            }

            arr[NumElements - 1] = item;

            return arr;
        }

        private static Dictionary<string, TElement> GetDict_TypedElements<TElement>(int elementSize)
        {
            Debug.Assert(NumElements > 2);

            Random random = new Random(Seed: elementSize);
            TElement item = GetCollectionElement<TElement>(elementSize, random);

            var dict = new Dictionary<string, TElement>();

            dict[$"{item}0"] = item;

            for (int i = 1; i < NumElements - 1; i++)
            {
                TElement newItem = GetCollectionElement<TElement>(elementSize, random);
                dict[$"{newItem}{i}"] = newItem;
            }

            dict[$"{item}{NumElements - 1}"] = item;

            return dict;
        }

        private static TElement GetCollectionElement<TElement>(int elementSize, Random? random = null)
        {
            Type type = typeof(TElement);
            char randomChar = (char)(random ??= new(Seed: elementSize)).Next('a', 'z');

            string value = new string(randomChar, elementSize);

            if (type == typeof(string))
            {
                return (TElement)(object)value;
            }
            else if (type == typeof(ClassWithKVP))
            {
                var kvp = new KeyValuePair<string, SimpleStruct>(value, new SimpleStruct {
                    One = 1,
                    Two = 2
                });

                return (TElement)(object)new ClassWithKVP { MyKvp = kvp };
            }
            else
            {
                return (TElement)(object)new ImmutableStructWithStrings(value, value);
            }

            throw new NotImplementedException();
        }

        private static IEnumerable<(Type collectionType, int bufferSize)> CollectionTestData<TElement>()
        {
            foreach (Type type in CollectionTypes<TElement>())
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

        private static IEnumerable<Type> CollectionTypes<TElement>()
        {
            foreach (Type type in CollectionTestTypes.EnumerableTypes<TElement>())
            {
                yield return type;
            }
            foreach (Type type in ObjectNotationTypes<TElement>())
            {
                yield return type;
            }
            // Stack types
            foreach (Type type in TypeHelper<TElement>.StackTypes)
            {
                yield return type;
            }
            // Dictionary types
            foreach (Type type in CollectionTestTypes.DictionaryTypes<TElement>())
            {
                yield return type;
            }
        }

        private static IEnumerable<Type> ObjectNotationTypes<TElement>()
        {
            yield return typeof(KeyValuePair<TElement, TElement>); // KeyValuePairConverter
        }

        private static class TypeHelper<TElement>
        {
            public static HashSet<Type> DictionaryTypes { get; } = new HashSet<Type>(CollectionTestTypes.DictionaryTypes<TElement>());

            public static HashSet<Type> StackTypes { get; } = new HashSet<Type>
            {
                typeof(ConcurrentStack<TElement>), // ConcurrentStackOfTConverter
                typeof(Stack), // IEnumerableWithAddMethodConverter
                typeof(Stack<TElement>), // StackOfTConverter
                typeof(ImmutableStack<TElement>) // ImmutableEnumerableOfTConverter
            };

            public static HashSet<Type> NotSupportedForDeserialization { get; } = new HashSet<Type>
            {
                typeof(WrapperForIEnumerable),
                typeof(WrapperForIReadOnlyCollectionOfT<TElement>),
                typeof(GenericIReadOnlyDictionaryWrapper<string, TElement>)
            };

            // Non-generic types cannot roundtrip when they contain a $ref written on serialization and they are the root type.
            public static HashSet<Type> NonRoundtrippableWithReferenceHandler { get; } = new HashSet<Type>
            {
                typeof(Hashtable),
                typeof(Queue),
                typeof(Stack),
                typeof(WrapperForIList),
                typeof(WrapperForIEnumerable)
            };
        }

        private class ClassWithKVP
        {
            public KeyValuePair<string, SimpleStruct> MyKvp { get; set; }
        }

        private struct ImmutableStructWithStrings
        {
            public string MyFirstString { get; }
            public string MySecondString { get; }

            [JsonConstructor]
            public ImmutableStructWithStrings(
                string myFirstString, string mySecondString)
            {
                MyFirstString = myFirstString;
                MySecondString = mySecondString;
            }
        }

        [Theory]
        [InlineData("")]
        [InlineData("}")]
        [InlineData("[")]
        [InlineData("]")]
        public void DeserializeDictionaryStartsWithInvalidJson(string json)
        {
            foreach (Type type in CollectionTestTypes.DictionaryTypes<string>())
            {
                Assert.ThrowsAsync<JsonException>(async () =>
                {
                    using (var memoryStream = new MemoryStream(Encoding.UTF8.GetBytes(json)))
                    {
                        await Serializer.DeserializeWrapper(memoryStream, type);
                    }
                });
            }
        }

        [Fact]
        public void SerializeEmptyCollection()
        {
            foreach (Type type in CollectionTestTypes.EnumerableTypes<int>())
            {
                Assert.Equal("[]", JsonSerializer.Serialize(GetEmptyCollection<int>(type)));
            }

            foreach (Type type in TypeHelper<int>.StackTypes)
            {
                Assert.Equal("[]", JsonSerializer.Serialize(GetEmptyCollection<int>(type)));
            }

            foreach (Type type in CollectionTestTypes.DictionaryTypes<int>())
            {
                Assert.Equal("{}", JsonSerializer.Serialize(GetEmptyCollection<int>(type)));
            }

            foreach (Type type in ObjectNotationTypes<int>())
            {
                Assert.Equal(@"{""Key"":0,""Value"":0}", JsonSerializer.Serialize(GetEmptyCollection<int>(type)));
            }
        }
    }
}
