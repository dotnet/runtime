// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using Xunit;

namespace System.Reflection.Metadata.Tests
{
    public class TypeNameParserSamples
    {
        internal sealed class SampleSerializationBinder : SerializationBinder
        {
            private static TypeNameParserOptions _options;

            // we could use Frozen collections here ;)
            private readonly static Dictionary<string, Type> _alwaysAllowed = new()
            {
                { typeof(string).FullName, typeof(string) },
                { typeof(int).FullName, typeof(int) },
                { typeof(uint).FullName, typeof(uint) },
                { typeof(long).FullName, typeof(long) },
                { typeof(ulong).FullName, typeof(ulong) },
                { typeof(double).FullName, typeof(double) },
                { typeof(float).FullName, typeof(float) },
                { typeof(bool).FullName, typeof(bool) },
                { typeof(short).FullName, typeof(short) },
                { typeof(ushort).FullName, typeof(ushort) },
                { typeof(byte).FullName, typeof(byte) },
                { typeof(char).FullName, typeof(char) },
                { typeof(DateTime).FullName, typeof(DateTime) },
                { typeof(TimeSpan).FullName, typeof(TimeSpan) },
                { typeof(Guid).FullName, typeof(Guid) },
                { typeof(Uri).FullName, typeof(Uri) },
                { typeof(DateTimeOffset).FullName, typeof(DateTimeOffset) },
                { typeof(Version).FullName, typeof(Version) },
                { typeof(Nullable).FullName, typeof(Nullable) }, // Nullable is generic!
            };

            private readonly Dictionary<string, Type>? _userDefined;

            public SampleSerializationBinder(Type[]? allowedTypes = null)
                => _userDefined = allowedTypes?.ToDictionary(type => type.FullName);

            public override Type? BindToType(string assemblyName, string typeName)
            {
                // Fast path for common primitive type names and user-defined type names
                // that use the same syntax and casing as System.Type.FullName API.
                if (TryGetTypeFromFullName(typeName, out Type type))
                {
                    return type;
                }

                _options ??= new() // there is no need for lazy initialization, I just wanted to have everything important in one method
                {
                    // We parse only type names, because the attackers may create such a payload,
                    // where "typeName" passed to BindToType contains the assembly name
                    // and "assemblyName" passed to this method contains something else
                    // (some garbage or a different assembly name). Example:
                    // typeName: System.Int32, MyHackyDll.dll
                    // assemblyName: mscorlib.dll
                    AllowFullyQualifiedName = false,
                    // To prevent from unbounded recursion, we set the max depth for parser options.
                    // By ensuring that the max depth limit is enforced, we can safely use recursion in
                    // GetTypeFromParsedTypeName to get arrays of arrays and generics of generics.
                    MaxRecursiveDepth = 10
                };

                if (!TypeName.TryParse(typeName.AsSpan(), out TypeName parsed, _options))
                {
                    // we can throw any exception, log the information etc
                    throw new InvalidOperationException($"Invalid type name: '{typeName}'");
                }

                return GetTypeFromParsedTypeName(parsed);
            }

            private Type? GetTypeFromParsedTypeName(TypeName parsed)
            {
                if (TryGetTypeFromFullName(parsed.FullName, out Type type))
                {
                    return type;
                }
                else if (parsed.IsArray)
                {
                    TypeName arrayElementTypeName = parsed.UnderlyingType; // equivalent of type.GetElementType()
                    Type arrayElementType = GetTypeFromParsedTypeName(arrayElementTypeName); // recursive call allows for creating arrays of arrays etc

                    return parsed.IsSzArrayType
                            ? arrayElementType.MakeArrayType()
                            : arrayElementType.MakeArrayType(parsed.GetArrayRank());
                }
                else if (parsed.IsConstructedGenericType)
                {
                    TypeName genericTypeDefinitionName = parsed.UnderlyingType; // equivalent of type.GetGenericTypeDefinition()
                    Type genericTypeDefinition = GetTypeFromParsedTypeName(genericTypeDefinitionName);
                    Debug.Assert(genericTypeDefinition.IsGenericTypeDefinition);

                    TypeName[] genericArgs = parsed.GetGenericArguments();
                    Type[] typeArguments = new Type[genericArgs.Length];
                    for (int i = 0; i < genericArgs.Length; i++)
                    {
                        typeArguments[i] = GetTypeFromParsedTypeName(genericArgs[i]); // recursive call allows for generics of generics like "List<int?>"
                    }
                    return genericTypeDefinition.MakeGenericType(typeArguments);
                }

                throw new ArgumentException($"{parsed.FullName} is not on the allow list.");
            }

            private bool TryGetTypeFromFullName(string fullName, out Type? type)
                => _alwaysAllowed.TryGetValue(fullName, out type)
                || (_userDefined is not null && _userDefined.TryGetValue(fullName, out type));
        }

        [Serializable]
        public class CustomUserDefinedType
        {
            public int Integer { get; set; }
            public string Text { get; set; }
            public List<DateTime> ListOfDates { get; set; }
            public CustomUserDefinedType[] ArrayOfCustomUserDefinedTypes { get; set; }
        }

        [Fact]
        public void CanDeserializeCustomUserDefinedType()
        {
            CustomUserDefinedType parent = new()
            {
                Integer = 1,
                Text = "parent",
                ListOfDates = new List<DateTime>()
                {
                    DateTime.Parse("02/06/2024")
                },
                ArrayOfCustomUserDefinedTypes = new []
                {
                    new CustomUserDefinedType()
                    {
                        Integer = 2,
                        Text = "child"
                    }
                }
            };
            SampleSerializationBinder binder = new(
                allowedTypes:
                [
                    typeof(CustomUserDefinedType),
                    typeof(List<>) // TODO adsitnik: make it work for List<DateTime> too (currently does not work due to type forwarding)
                ]);

            CustomUserDefinedType deserialized = SerializeDeserialize(parent, binder);

            Assert.Equal(parent.Integer, deserialized.Integer);
            Assert.Equal(parent.Text, deserialized.Text);
            Assert.Equal(parent.ListOfDates.Count, deserialized.ListOfDates.Count);
            for (int i = 0; i < deserialized.ListOfDates.Count; i++)
            {
                Assert.Equal(parent.ListOfDates[i], deserialized.ListOfDates[i]);
            }
            Assert.Equal(parent.ArrayOfCustomUserDefinedTypes.Length, deserialized.ArrayOfCustomUserDefinedTypes.Length);
            for (int i = 0; i < deserialized.ArrayOfCustomUserDefinedTypes.Length; i++)
            {
                Assert.Equal(parent.ArrayOfCustomUserDefinedTypes[i].Integer, deserialized.ArrayOfCustomUserDefinedTypes[i].Integer);
                Assert.Equal(parent.ArrayOfCustomUserDefinedTypes[i].Text, deserialized.ArrayOfCustomUserDefinedTypes[i].Text);
            }
        }

        [Fact]
        public void CanDeserializeDictionaryUsingNonPublicComparerType()
        {
            Dictionary<string, int> dictionary = new(StringComparer.CurrentCulture)
            {
                { "test", 1 }
            };
            SampleSerializationBinder binder = new(
                allowedTypes:
                [
                    typeof(Dictionary<,>), // this could be Dictionary<string, int> to be more strict
                    StringComparer.CurrentCulture.GetType(), // this type is not public, this is all this test is about
                    typeof(Globalization.CompareOptions),
                    typeof(Globalization.CompareInfo),
                    typeof(KeyValuePair<,>), // this could be KeyValuePair<string, int> to be more strict
                ]);

            Dictionary<string, int> deserialized = SerializeDeserialize(dictionary, binder);

            Assert.Equal(dictionary, deserialized);
        }

        [Fact]
        public void CanDeserializeArraysOfArrays()
        {
            int[][] arrayOfArrays = new int[10][];
            for (int i = 0; i < arrayOfArrays.Length; i++)
            {
                arrayOfArrays[i] = Enumerable.Repeat(i, 10).ToArray();
            }

            SampleSerializationBinder binder = new();
            int[][] deserialized = SerializeDeserialize(arrayOfArrays, binder);

            Assert.Equal(arrayOfArrays.Length, deserialized.Length);
            for (int i = 0; i < arrayOfArrays.Length; i++)
            {
                Assert.Equal(arrayOfArrays[i], deserialized[i]);
            }
        }

        [Fact]
        public void CanDeserializeListOfListOfInt()
        {
            List<List<int>> listOfListOfInts = new(10);
            for (int i = 0; i < listOfListOfInts.Count; i++)
            {
                listOfListOfInts[i] = Enumerable.Repeat(i, 10).ToList();
            }

            SampleSerializationBinder binder = new(allowedTypes:
                [
                    typeof(List<>)
                ]);
            List<List<int>> deserialized = SerializeDeserialize(listOfListOfInts, binder);

            Assert.Equal(listOfListOfInts.Count, deserialized.Count);
            for (int i = 0; i < listOfListOfInts.Count; i++)
            {
                Assert.Equal(listOfListOfInts[i], deserialized[i]);
            }
        }

        static T SerializeDeserialize<T>(T instance, SerializationBinder binder)
        {
            using MemoryStream bfStream = new();
            BinaryFormatter bf = new()
            {
                Binder = binder
            };

            bf.Serialize(bfStream, instance);
            bfStream.Position = 0;

            return (T)bf.Deserialize(bfStream);
        }
    }
}
