// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace System.Text.Json.Serialization.Tests
{
    public static partial class StreamTests
    {
        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/35927", typeof(PlatformDetection), nameof(PlatformDetection.IsMonoInterpreter))]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/35927", TestPlatforms.Browser)]
        public static async Task HandleCollectionsAsync()
        {
            await RunTestAsync<string>();
            await RunTestAsync<ClassWithKVP>();
            await RunTestAsync<ImmutableStructWithStrings>();
        }

        private static async Task RunTestAsync<TElement>()
        {
            foreach ((Type, int) pair in CollectionTestData<TElement>())
            {
                Type type = pair.Item1;
                int bufferSize = pair.Item2;

                // bufferSize * 0.9 is the threshold size from codebase, subtract 2 for [ or { characters, then create a
                // string containing (threshold - 2) amount of char 'a' which when written into output buffer produces buffer
                // which size equal to or very close to threshold size, then adding the string to the list, then adding a big
                // object to the list which changes depth of written json and should cause buffer flush.
                int thresholdSize = (int)(bufferSize * 0.9 - 2);

                var options = new JsonSerializerOptions
                {
                    DefaultBufferSize = bufferSize,
                    WriteIndented = true
                };

                var optionsWithPreservedReferenceHandling = new JsonSerializerOptions(options)
                {
                    ReferenceHandler = ReferenceHandler.Preserve
                };

                object obj = GetPopulatedCollection<TElement>(type, thresholdSize);
                await PerformSerialization<TElement>(obj, type, options);
                await PerformSerialization<TElement>(obj, type, optionsWithPreservedReferenceHandling);
            }
        }

        private static async Task PerformSerialization<TElement>(object obj, Type type, JsonSerializerOptions options)
        {
            string expectedjson = JsonSerializer.Serialize(obj, options);

            using var memoryStream = new MemoryStream();
            await JsonSerializer.SerializeAsync(memoryStream, obj, options);
            string serialized = Encoding.UTF8.GetString(memoryStream.ToArray());
            JsonTestHelper.AssertJsonEqual(expectedjson, serialized);

            memoryStream.Position = 0;

            if (options.ReferenceHandler == null || !GetTypesNonRoundtrippableWithReferenceHandler().Contains(type))
            {
                await TestDeserialization<TElement>(memoryStream, expectedjson, type, options);

                // Deserialize with extra whitespace
                string jsonWithWhiteSpace = GetPayloadWithWhiteSpace(expectedjson);
                using var memoryStreamWithWhiteSpace = new MemoryStream(Encoding.UTF8.GetBytes(jsonWithWhiteSpace));
                await TestDeserialization<TElement>(memoryStreamWithWhiteSpace, expectedjson, type, options);
            }
        }

        private static async Task TestDeserialization<TElement>(
            Stream memoryStream,
            string expectedJson,
            Type type,
            JsonSerializerOptions options)
        {
            try
            {
                object deserialized = await JsonSerializer.DeserializeAsync(memoryStream, type, options);
                string serialized = JsonSerializer.Serialize(deserialized, options);

                // Stack elements reversed during serialization.
                if (StackTypes<TElement>().Contains(type))
                {
                    deserialized = JsonSerializer.Deserialize(serialized, type, options);
                    serialized = JsonSerializer.Serialize(deserialized, options);
                }

                // TODO: https://github.com/dotnet/runtime/issues/35611.
                // Can't control order of dictionary elements when serializing, so reference metadata might not match up.
                if(!(CollectionTestTypes.DictionaryTypes<TElement>().Contains(type) && options.ReferenceHandler == ReferenceHandler.Preserve))
                {
                    JsonTestHelper.AssertJsonEqual(expectedJson, serialized);
                }
            }
            catch (NotSupportedException ex)
            {
                Assert.True(GetTypesNotSupportedForDeserialization<TElement>().Contains(type));
                Assert.Contains(type.ToString(), ex.ToString());
            }
        }

        private static object GetPopulatedCollection<TElement>(Type type, int stringLength)
        {
            if (type == typeof(TElement[]))
            {
                return GetArr_TypedElements<TElement>(stringLength);
            }
            else if (type == typeof(ImmutableList<TElement>))
            {
                return ImmutableList.CreateRange(GetArr_TypedElements<TElement>(stringLength));
            }
            else if (type == typeof(ImmutableStack<TElement>))
            {
                return ImmutableStack.CreateRange(GetArr_TypedElements<TElement>(stringLength));
            }
            else if (type == typeof(ImmutableDictionary<string, TElement>))
            {
                return ImmutableDictionary.CreateRange(GetDict_TypedElements<TElement>(stringLength));
            }
            else if (type == typeof(KeyValuePair<TElement, TElement>))
            {
                TElement item = GetCollectionElement<TElement>(stringLength);
                return new KeyValuePair<TElement, TElement>(item, item);
            }
            else if (
                typeof(IDictionary<string, TElement>).IsAssignableFrom(type) ||
                typeof(IReadOnlyDictionary<string, TElement>).IsAssignableFrom(type) ||
                typeof(IDictionary).IsAssignableFrom(type))
            {
                return Activator.CreateInstance(type, new object[] { GetDict_TypedElements<TElement>(stringLength) });
            }
            else if (typeof(IEnumerable<TElement>).IsAssignableFrom(type))
            {
                return Activator.CreateInstance(type, new object[] { GetArr_TypedElements<TElement>(stringLength) });
            }
            else
            {
                return Activator.CreateInstance(type, new object[] { GetArr_BoxedElements<TElement>(stringLength) });
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

        private static TElement[] GetArr_TypedElements<TElement>(int stringLength)
        {
            Debug.Assert(NumElements > 2);
            var arr = new TElement[NumElements];

            TElement item = GetCollectionElement<TElement>(stringLength);
            arr[0] = item;

            for (int i = 1; i < NumElements - 1; i++)
            {
                arr[i] = GetCollectionElement<TElement>(stringLength);
            }

            arr[NumElements - 1] = item;

            return arr;
        }

        private static object[] GetArr_BoxedElements<TElement>(int stringLength)
        {
            Debug.Assert(NumElements > 2);
            var arr = new object[NumElements];

            TElement item = GetCollectionElement<TElement>(stringLength);
            arr[0] = item;

            for (int i = 1; i < NumElements - 1; i++)
            {
                arr[i] = GetCollectionElement<TElement>(stringLength);
            }

            arr[NumElements - 1] = item;

            return arr;
        }

        private static Dictionary<string, TElement> GetDict_TypedElements<TElement>(int stringLength)
        {
            Debug.Assert(NumElements > 2);

            TElement item = GetCollectionElement<TElement>(stringLength);

            var dict = new Dictionary<string, TElement>();

            dict[$"{item}0"] = item;

            for (int i = 1; i < NumElements - 1; i++)
            {
                TElement newItem = GetCollectionElement<TElement>(stringLength);
                dict[$"{newItem}{i}"] = newItem;
            }

            dict[$"{item}{NumElements - 1}"] = item;

            return dict;
        }

        private static TElement GetCollectionElement<TElement>(int stringLength)
        {
            Type type = typeof(TElement);

            Random rand = new Random();
            char randomChar = (char)rand.Next('a', 'z');

            string value = new string(randomChar, stringLength);
            var kvp = new KeyValuePair<string, SimpleStruct>(value, new SimpleStruct {
                One = 1,
                Two = 2
            });

            if (type == typeof(string))
            {
                return (TElement)(object)value;
            }
            else if (type == typeof(ClassWithKVP))
            {
                return (TElement)(object)new ClassWithKVP { MyKvp = kvp };
            }
            else
            {
                return (TElement)(object)new ImmutableStructWithStrings(value, value);
            }

            throw new NotImplementedException();
        }

        private static IEnumerable<(Type, int)> CollectionTestData<TElement>()
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
            foreach (Type type in StackTypes<TElement>())
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

        private static HashSet<Type> StackTypes<TElement>() => new HashSet<Type>
        {
            typeof(ConcurrentStack<TElement>), // ConcurrentStackOfTConverter
            typeof(Stack), // IEnumerableWithAddMethodConverter
            typeof(Stack<TElement>), // StackOfTConverter
            typeof(ImmutableStack<TElement>) // ImmutableEnumerableOfTConverter
        };

        private static HashSet<Type> GetTypesNotSupportedForDeserialization<TElement>() => new HashSet<Type>
        {
            typeof(WrapperForIEnumerable),
            typeof(WrapperForIReadOnlyCollectionOfT<TElement>),
            typeof(GenericIReadOnlyDictionaryWrapper<string, TElement>)
        };

        // Non-generic types cannot roundtrip when they contain a $ref written on serialization and they are the root type.
        private static HashSet<Type> GetTypesNonRoundtrippableWithReferenceHandler() => new HashSet<Type>
        {
            typeof(Hashtable),
            typeof(Queue),
            typeof(Stack),
            typeof(WrapperForIList),
            typeof(WrapperForIEnumerable)
        };

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
        public static void DeserializeDictionaryStartsWithInvalidJson(string json)
        {
            foreach (Type type in CollectionTestTypes.DictionaryTypes<string>())
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

        [Fact]
        public static void SerializeEmptyCollection()
        {
            foreach (Type type in CollectionTestTypes.EnumerableTypes<int>())
            {
                Assert.Equal("[]", JsonSerializer.Serialize(GetEmptyCollection<int>(type)));
            }

            foreach (Type type in StackTypes<int>())
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
