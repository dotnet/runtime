// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using Xunit;

namespace System.Reflection.Metadata.Tests
{
    public class TypeNameParserSamples
    {
        [Fact]
        public void CanImplementSerializationBinder()
        {
            SerializableClass parent = new()
            {
                Integer = 1,
                Text = "parent",
                Dates = new List<DateTime>()
                {
                    DateTime.Parse("02/06/2024")
                }
            };
            SerializableClass child = new()
            {
                Integer = 2,
                Text = "child"
            };
            parent.Array = new SerializableClass[] { child };

            SampleSerializationBinder binder = new(
                allowedCustomElementalTypes: new HashSet<Type>(new Type[]
                {
                    typeof(SerializableClass),
                }));

            BinaryFormatter bf = new()
            {
                Binder = binder
            };
            using MemoryStream bfStream = new();
            bf.Serialize(bfStream, parent);
            bfStream.Position = 0;

            SerializableClass deserialized = (SerializableClass)bf.Deserialize(bfStream);

            Assert.Equal(parent.Integer, deserialized.Integer);
            Assert.Equal(parent.Text, deserialized.Text);
            Assert.Equal(parent.Dates.Count, deserialized.Dates.Count);
            for (int i = 0; i < deserialized.Dates.Count; i++)
            {
                Assert.Equal(parent.Dates[i], deserialized.Dates[i]);
            }
            Assert.Equal(parent.Array.Length, deserialized.Array.Length);
            for (int i = 0; i < deserialized.Array.Length; i++)
            {
                Assert.Equal(parent.Array[i].Integer, deserialized.Array[i].Integer);
                Assert.Equal(parent.Array[i].Text, deserialized.Array[i].Text);
            }
        }

        internal sealed class SampleSerializationBinder : SerializationBinder
        {
            private static readonly TypeNameParserOptions _options = new()
            {
                AllowFullyQualifiedName = false // we parse only type names
            };

            // we could use Frozen collections here!
            private readonly static Dictionary<string, Type> _alwaysAllowedElementalTypes = new()
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
            };

            private static readonly Dictionary<string, Type> _alwaysAllowedOpenGenericTypes = new()
            {
                { typeof(Nullable).FullName, typeof(Nullable) },
                { typeof(List<>).FullName, typeof(List<>) },
            };

            private readonly Dictionary<string, Type>? _allowedCustomElementalTypes, _allowedCustomOpenGenericTypes;

            public SampleSerializationBinder(
                HashSet<Type>? allowedCustomElementalTypes,
                HashSet<Type>? allowedCustomOpenGenericTypes = null)
            {
                if (allowedCustomElementalTypes is not null)
                {
                    foreach (var type in allowedCustomElementalTypes)
                    {
                        if (type.IsGenericType || type.IsByRef || type.IsPointer || type.IsArray)
                        {
                            throw new ArgumentException($"{type.FullName} is not an elemental type");
                        }
                    }
                }

                if (allowedCustomOpenGenericTypes is not null)
                {
                    foreach (var type in allowedCustomOpenGenericTypes)
                    {
                        if (!type.IsGenericTypeDefinition || type.IsByRef || type.IsPointer || type.IsArray)
                        {
                            throw new ArgumentException($"{type.FullName} is not an open generic type");
                        }
                    }
                }

                _allowedCustomElementalTypes = allowedCustomElementalTypes?.ToDictionary(type => type.FullName);
                _allowedCustomOpenGenericTypes = allowedCustomOpenGenericTypes?.ToDictionary(type => type.FullName);
            }

            public override Type? BindToType(string assemblyName, string typeName)
            {
                // fast path for common primitive types like int, bool and string
                if (_alwaysAllowedElementalTypes.TryGetValue(typeName, out Type type))
                {
                    return type;
                }

                if (!TypeName.TryParse(typeName.AsSpan(), out TypeName parsed, _options))
                {
                    // we can throw any exception, log the information etc
                    throw new InvalidOperationException($"Invalid type name: '{typeName}'");
                }

                if (parsed.IsElementalType) // not a pointer, generic, array or managed reference
                {
                    if (TryGetElementalTypeFromFullName(parsed.FullName, out type))
                    {
                        return type;
                    }

                    throw new ArgumentException($"{parsed.FullName} is not on the allow list.");
                }
                else if (parsed.IsArray)
                {
                    TypeName arrayElementType = parsed.UnderlyingType;
                    if (TryGetElementalTypeFromFullName(arrayElementType.FullName, out type))
                    {
                        return arrayElementType.IsSzArrayType
                            ? type.MakeArrayType()
                            : type.MakeArrayType(parsed.GetArrayRank());
                    }

                    throw new ArgumentException($"{parsed.FullName} (array) is not on the allow list.");
                }
                else if (parsed.IsConstructedGenericType)
                {
                    TypeName genericTypeDefinition = parsed.UnderlyingType;
                    if (TryGetOpenGenericTypeFromFullName(genericTypeDefinition.FullName, out type))
                    {
                        TypeName[] genericArguments = parsed.GetGenericArguments();
                        Type[] types = new Type[genericArguments.Length];
                        for (int i = 0; i < genericArguments.Length; i++ )
                        {
                            if (!TryGetElementalTypeFromFullName(genericArguments[i].FullName, out types[i]))
                            {
                                throw new ArgumentException($"{genericArguments[i].FullName} (generic argument) is not on the allow list.");
                            }
                        }
                        return type.MakeGenericType(types);
                    }
                    throw new ArgumentException($"{parsed.FullName} (generic) is not on the allow list.");
                }

                throw new ArgumentException($"{parsed.FullName} is not on the allow list.");
            }

            private bool TryGetElementalTypeFromFullName(string fullName, out Type? type)
            {
                type = null;

                return (_alwaysAllowedElementalTypes is not null && _alwaysAllowedElementalTypes.TryGetValue(fullName, out type))
                    || (_allowedCustomElementalTypes is not null && _allowedCustomElementalTypes.TryGetValue(fullName, out type));
            }

            private bool TryGetOpenGenericTypeFromFullName(string fullName, out Type? type)
            {
                type = null;

                return (_alwaysAllowedOpenGenericTypes is not null && _alwaysAllowedOpenGenericTypes.TryGetValue(fullName, out type))
                    || (_allowedCustomOpenGenericTypes is not null && _allowedCustomOpenGenericTypes.TryGetValue(fullName, out type));
            }
        }

        [Serializable]
        public class SerializableClass
        {
            public int Integer { get; set; }
            public string Text { get; set; }
            public List<DateTime> Dates { get; set; }
            public SerializableClass[] Array { get; set; }
        }
    }
}
