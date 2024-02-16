// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Reflection;
using Xunit;

namespace System.Text.Json.Serialization.Tests
{
    public static class InvalidJsonTests
    {
        [Theory]
        [InlineData(typeof(ImmutableDictionary<string, string>), "\"headers\"")]
        [InlineData(typeof(Dictionary<string, string>), "\"headers\"")]
        [InlineData(typeof(PocoDictionary), "\"headers\"")]
        public static void InvalidJsonForValueShouldFail(Type type, string json)
        {
            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize(json, type));
        }

        public static IEnumerable<string> InvalidJsonForIntValue()
        {
            yield return @"""1""";
            yield return "[";
            yield return "}";
            yield return @"[""1""]";
            yield return "[true]";
        }

        public static IEnumerable<string> InvalidJsonForPoco()
        {
            foreach (string value in InvalidJsonForIntValue())
            {
                yield return value;
                yield return "[" + value + "]";
                yield return "{" + value + "}";
                yield return @"{""Id"":" + value + "}";
            }
        }

        public class PocoWithParameterizedCtor
        {
            public int Obj { get; set; }
        }

        public class ClassWithInt
        {
            public int Obj { get; set; }
        }

        public class ClassWithIntList
        {
            public List<int> Obj { get; set; }
        }

        public class ClassWithIntArray
        {
            public int[] Obj { get; set; }
        }

        public class ClassWithPoco
        {
            public Poco Obj { get; set; }
        }

        public class ClassWithParameterizedCtor_WithPoco
        {
            public PocoWithParameterizedCtor Obj { get; set; }

            public ClassWithParameterizedCtor_WithPoco(PocoWithParameterizedCtor obj) => Obj = obj;
        }

        public class ClassWithPocoArray
        {
            public Poco[] Obj { get; set; }
        }

        public class ClassWithParameterizedCtor_WithPocoArray
        {
            public PocoWithParameterizedCtor[] Obj { get; set; }

            public ClassWithParameterizedCtor_WithPocoArray(PocoWithParameterizedCtor[] obj) => Obj = obj;
        }

        public class ClassWithDictionaryOfIntArray
        {
            public Dictionary<string, int[]> Obj { get; set; }
        }

        public class ClassWithDictionaryOfPoco
        {
            public Dictionary<string, Poco> Obj { get; set; }
        }

        public class ClassWithDictionaryOfPocoList
        {
            public Dictionary<string, List<Poco>> Obj { get; set; }
        }

        public class ClassWithParameterizedCtor_WithDictionaryOfPocoList
        {
            public Dictionary<string, List<PocoWithParameterizedCtor>> Obj { get; set; }

            public ClassWithParameterizedCtor_WithDictionaryOfPocoList(Dictionary<string, List<PocoWithParameterizedCtor>> obj) => Obj = obj;
        }

        public static IEnumerable<Type> TypesForInvalidJsonForCollectionTests()
        {
            static Type MakeClosedCollectionType(Type openCollectionType, Type elementType)
            {
                if (openCollectionType == typeof(Dictionary<,>))
                {
                    return typeof(Dictionary<,>).MakeGenericType(typeof(string), elementType);
                }
                else
                {
                    return openCollectionType.MakeGenericType(elementType);
                }
            }

            Type[] elementTypes = new Type[]
            {
                typeof(int),
                typeof(Poco),
                typeof(ClassWithInt),
                typeof(ClassWithIntList),
                typeof(ClassWithPoco),
                typeof(ClassWithPocoArray),
                typeof(ClassWithDictionaryOfIntArray),
                typeof(ClassWithDictionaryOfPoco),
                typeof(ClassWithDictionaryOfPocoList),
                typeof(PocoWithParameterizedCtor),
                typeof(ClassWithParameterizedCtor_WithPoco),
                typeof(ClassWithParameterizedCtor_WithPocoArray),
                typeof(ClassWithParameterizedCtor_WithDictionaryOfPocoList),
            };

            Type[] collectionTypes = new Type[]
            {
                typeof(List<>),
                typeof(Dictionary<,>),
            };

            foreach (Type type in elementTypes)
            {
                yield return type;
            }

            List<Type> innerTypes = new List<Type>(elementTypes);

            // Create permutations of collections with 1 and 2 levels of nesting.
            for (int i = 0; i < 2; i++)
            {
                foreach (Type collectionType in collectionTypes)
                {
                    List<Type> newInnerTypes = new List<Type>();

                    foreach (Type elementType in innerTypes)
                    {
                        Type newCollectionType = MakeClosedCollectionType(collectionType, elementType);
                        newInnerTypes.Add(newCollectionType);
                        yield return newCollectionType;
                    }

                    innerTypes = newInnerTypes;
                }
            }
        }

        static IEnumerable<string> GetInvalidJsonStringsForType(Type type)
        {
            if (type == typeof(int))
            {
                foreach (string json in InvalidJsonForIntValue())
                {
                    yield return json;
                }
                yield break;
            }

            if (type == typeof(Poco))
            {
                foreach (string json in InvalidJsonForPoco())
                {
                    yield return json;
                }
                yield break;
            }

            Type elementType;

            if (!typeof(IEnumerable).IsAssignableFrom(type))
            {
                // Get type of "Obj" property.
                elementType = type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)[0].PropertyType;
            }
            else if (type.IsArray)
            {
                elementType = type.GetElementType();
            }
            else if (!type.IsGenericType)
            {
                Assert.Fail("Expected generic type");
                yield break;
            }
            else
            {
                Type genericTypeDef = type.GetGenericTypeDefinition();

                if (genericTypeDef == typeof(List<>))
                {
                    elementType = type.GetGenericArguments()[0];
                }
                else if (genericTypeDef == typeof(Dictionary<,>))
                {
                    elementType = type.GetGenericArguments()[1];
                }
                else
                {
                    Assert.Fail("Expected List or Dictionary type");
                    yield break;
                }
            }

            foreach (string invalidJson in GetInvalidJsonStringsForType(elementType))
            {
                yield return "[" + invalidJson + "]";
                yield return "{" + invalidJson + "}";
                yield return @"{""Obj"":" + invalidJson + "}";
            }
        }

        public static IEnumerable<object[]> DataForInvalidJsonForTypeTests()
        {
            foreach (Type type in TypesForInvalidJsonForCollectionTests())
            {
                foreach (string invalidJson in GetInvalidJsonStringsForType(type))
                {
                    yield return new object[] { type, invalidJson };
                }
            }

            yield return new object[] { typeof(int[]), @"""test""" };
            yield return new object[] { typeof(int[]), @"1" };
            yield return new object[] { typeof(int[]), @"false" };
            yield return new object[] { typeof(int[]), @"{}" };
            yield return new object[] { typeof(int[]), @"{""test"": 1}" };
            yield return new object[] { typeof(int[]), @"[""test""" };
            yield return new object[] { typeof(int[]), @"[""test""]" };
            yield return new object[] { typeof(int[]), @"[true]" };
            yield return new object[] { typeof(int[]), @"[{}]" };
            yield return new object[] { typeof(int[]), @"[[]]" };
            yield return new object[] { typeof(int[]), @"[{""test"": 1}]" };
            yield return new object[] { typeof(int[]), @"[[true]]" };
            yield return new object[] { typeof(Dictionary<string, int[]>), @"{""test"": {}}" };
            yield return new object[] { typeof(Dictionary<string, int[]>), @"{""test"": {""test"": 1}}" };
            yield return new object[] { typeof(Dictionary<string, int[]>), @"{""test"": ""test""}" };
            yield return new object[] { typeof(Dictionary<string, int[]>), @"{""test"": 1}" };
            yield return new object[] { typeof(Dictionary<string, int[]>), @"{""test"": true}" };
            yield return new object[] { typeof(Dictionary<string, int[]>), @"{""test"": [""test""}" };
            yield return new object[] { typeof(Dictionary<string, int[]>), @"{""test"": [""test""]}" };
            yield return new object[] { typeof(Dictionary<string, int[]>), @"{""test"": [[]]}" };
            yield return new object[] { typeof(Dictionary<string, int[]>), @"{""test"": [true]}" };
            yield return new object[] { typeof(Dictionary<string, int[]>), @"{""test"": [{}]}" };
            yield return new object[] { typeof(ClassWithIntArray), @"{""Obj"": ""test""}" };
            yield return new object[] { typeof(ClassWithIntArray), @"{""Obj"": 1}" };
            yield return new object[] { typeof(ClassWithIntArray), @"{""Obj"": false}" };
            yield return new object[] { typeof(ClassWithIntArray), @"{""Obj"": {}}" };
            yield return new object[] { typeof(ClassWithIntArray), @"{""Obj"": {""test"": 1}}" };
            yield return new object[] { typeof(ClassWithIntArray), @"{""Obj"": [""test""}" };
            yield return new object[] { typeof(ClassWithIntArray), @"{""Obj"": [""test""]}" };
            yield return new object[] { typeof(ClassWithIntArray), @"{""Obj"": [true]}" };
            yield return new object[] { typeof(ClassWithIntArray), @"{""Obj"": [{}]}" };
            yield return new object[] { typeof(ClassWithIntArray), @"{""Obj"": [[]]}" };
            yield return new object[] { typeof(ClassWithIntArray), @"{""Obj"": [{""test"": 1}]}" };
            yield return new object[] { typeof(ClassWithIntArray), @"{""Obj"": [[true]]}" };
            yield return new object[] { typeof(Dictionary<string, string>), @"""test""" };
            yield return new object[] { typeof(Dictionary<string, string>), @"1" };
            yield return new object[] { typeof(Dictionary<string, string>), @"false" };
            yield return new object[] { typeof(Dictionary<string, string>), @"{"""": 1}" };
            yield return new object[] { typeof(Dictionary<string, string>), @"{"""": {}}" };
            yield return new object[] { typeof(Dictionary<string, string>), @"{"""": {"""":""""}}" };
            yield return new object[] { typeof(Dictionary<string, string>), @"[""test""" };
            yield return new object[] { typeof(Dictionary<string, string>), @"[""test""]" };
            yield return new object[] { typeof(Dictionary<string, string>), @"[true]" };
            yield return new object[] { typeof(Dictionary<string, string>), @"[{}]" };
            yield return new object[] { typeof(Dictionary<string, string>), @"[[]]" };
            yield return new object[] { typeof(Dictionary<string, string>), @"[{""test"": 1}]" };
            yield return new object[] { typeof(Dictionary<string, string>), @"[[true]]" };
            yield return new object[] { typeof(ClassWithDictionaryOfIntArray), @"{""Obj"":""test""}" };
            yield return new object[] { typeof(ClassWithDictionaryOfIntArray), @"{""Obj"":1}" };
            yield return new object[] { typeof(ClassWithDictionaryOfIntArray), @"{""Obj"":false}" };
            yield return new object[] { typeof(ClassWithDictionaryOfIntArray), @"{""Obj"":{"""": 1}}" };
            yield return new object[] { typeof(ClassWithDictionaryOfIntArray), @"{""Obj"":{"""": {}}}" };
            yield return new object[] { typeof(ClassWithDictionaryOfIntArray), @"{""Obj"":{"""": {"""":""""}}}" };
            yield return new object[] { typeof(ClassWithDictionaryOfIntArray), @"{""Obj"":[""test""}" };
            yield return new object[] { typeof(ClassWithDictionaryOfIntArray), @"{""Obj"":[""test""]}" };
            yield return new object[] { typeof(ClassWithDictionaryOfIntArray), @"{""Obj"":[true]}" };
            yield return new object[] { typeof(ClassWithDictionaryOfIntArray), @"{""Obj"":[{}]}" };
            yield return new object[] { typeof(ClassWithDictionaryOfIntArray), @"{""Obj"":[[]]}" };
            yield return new object[] { typeof(ClassWithDictionaryOfIntArray), @"{""Obj"":[{""test"": 1}]}" };
            yield return new object[] { typeof(ClassWithDictionaryOfIntArray), @"{""Obj"":[[true]]}" };
            yield return new object[] { typeof(Dictionary<string, Poco>), @"{""key"":[{""Id"":3}]}" };
            yield return new object[] { typeof(Dictionary<string, Poco>), @"{""key"":[""test""]}" };
            yield return new object[] { typeof(Dictionary<string, Poco>), @"{""key"":[1]}" };
            yield return new object[] { typeof(Dictionary<string, Poco>), @"{""key"":[false]}" };
            yield return new object[] { typeof(Dictionary<string, Poco>), @"{""key"":[]}" };
            yield return new object[] { typeof(Dictionary<string, Poco>), @"{""key"":1}" };
            yield return new object[] { typeof(Dictionary<string, List<Poco>>), @"{""key"":{""Id"":3}}" };
            yield return new object[] { typeof(Dictionary<string, List<Poco>>), @"{""key"":{}}" };
            yield return new object[] { typeof(Dictionary<string, List<Poco>>), @"{""key"":[[]]}" };
            yield return new object[] { typeof(Dictionary<string, Dictionary<string, Poco>>), @"{""key"":[]}" };
            yield return new object[] { typeof(Dictionary<string, Dictionary<string, Poco>>), @"{""key"":1}" };
        }

        [Fact, OuterLoop]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/42677", platforms: TestPlatforms.Windows, runtimes: TestRuntimes.Mono)]
        public static void InvalidJsonForTypeShouldFail()
        {
            foreach (object[] args in DataForInvalidJsonForTypeTests()) // ~140K tests, too many for theory to handle well with our infrastructure
            {
                var type = (Type)args[0];
                var invalidJson = (string)args[1];
                Assert.Throws<JsonException>(() => JsonSerializer.Deserialize(invalidJson, type));
            }
        }

        [Fact]
        public static void InvalidEmptyDictionaryInput()
        {
            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<string>("{}"));
        }
    }
}
