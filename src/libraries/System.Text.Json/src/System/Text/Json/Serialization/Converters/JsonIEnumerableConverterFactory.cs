// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace System.Text.Json.Serialization.Converters
{
    /// <summary>
    /// Converter factory for all IEnumerable types.
    /// </summary>
    internal class JsonIEnumerableConverterFactory : JsonConverterFactory
    {
        private static readonly JsonIDictionaryConverter<IDictionary> s_converterForIDictionary = new JsonIDictionaryConverter<IDictionary>();
        private static readonly JsonIEnumerableConverter<IEnumerable> s_converterForIEnumerable = new JsonIEnumerableConverter<IEnumerable>();
        private static readonly JsonIListConverter<IList> s_converterForIList = new JsonIListConverter<IList>();

        public override bool CanConvert(Type typeToConvert)
        {
            return typeof(IEnumerable).IsAssignableFrom(typeToConvert);
        }

        [PreserveDependency(".ctor", "System.Text.Json.Serialization.Converters.JsonArrayConverter`2")]
        [PreserveDependency(".ctor", "System.Text.Json.Serialization.Converters.JsonConcurrentQueueOfTConverter`2")]
        [PreserveDependency(".ctor", "System.Text.Json.Serialization.Converters.JsonConcurrentStackOfTConverter`2")]
        [PreserveDependency(".ctor", "System.Text.Json.Serialization.Converters.JsonDefaultArrayConverter`2")]
        [PreserveDependency(".ctor", "System.Text.Json.Serialization.Converters.JsonDictionaryOfStringTValueConverter`2")]
        [PreserveDependency(".ctor", "System.Text.Json.Serialization.Converters.JsonICollectionOfTConverter`2")]
        [PreserveDependency(".ctor", "System.Text.Json.Serialization.Converters.JsonIDictionaryOfStringTValueConverter`2")]
        [PreserveDependency(".ctor", "System.Text.Json.Serialization.Converters.JsonIEnumerableOfTConverter`2")]
        [PreserveDependency(".ctor", "System.Text.Json.Serialization.Converters.JsonIEnumerableWithAddMethodConverter`1")]
        [PreserveDependency(".ctor", "System.Text.Json.Serialization.Converters.JsonIListConverter`1")]
        [PreserveDependency(".ctor", "System.Text.Json.Serialization.Converters.JsonIListOfTConverter`2")]
        [PreserveDependency(".ctor", "System.Text.Json.Serialization.Converters.JsonImmutableDictionaryOfStringTValueConverter`2")]
        [PreserveDependency(".ctor", "System.Text.Json.Serialization.Converters.JsonImmutableEnumerableOfTConverter`2")]
        [PreserveDependency(".ctor", "System.Text.Json.Serialization.Converters.JsonIReadOnlyDictionaryOfStringTValueConverter`2")]
        [PreserveDependency(".ctor", "System.Text.Json.Serialization.Converters.JsonISetOfTConverter`2")]
        [PreserveDependency(".ctor", "System.Text.Json.Serialization.Converters.JsonListOfTConverter`2")]
        [PreserveDependency(".ctor", "System.Text.Json.Serialization.Converters.JsonQueueOfTConverter`2")]
        [PreserveDependency(".ctor", "System.Text.Json.Serialization.Converters.JsonStackOfTConverter`2")]
        public override JsonConverter? CreateConverter(Type typeToConvert, JsonSerializerOptions options)
        {
            JsonConverter? converter = null;
            Type converterType;
            Type[] genericArgs;
            Type? elementType = null;
            Type? actualTypeToConvert;

            // Array
            if (typeToConvert.IsArray)
            {
                // Verify that we don't have a multidimensional array.
                if (typeToConvert.GetArrayRank() > 1)
                {
                    return null;
                }

                converterType = typeof(JsonArrayConverter<,>);
                elementType = typeToConvert.GetElementType();
            }
            // List<> or deriving from List<>
            else if ((actualTypeToConvert = typeToConvert.GetCompatibleGenericBaseClass(typeof(List<>))) != null)
            {
                converterType = typeof(JsonListOfTConverter<,>);
                elementType = actualTypeToConvert.GetGenericArguments()[0];
            }
            // Dictionary<string,> or deriving from Dictionary<string,>
            else if ((actualTypeToConvert = typeToConvert.GetCompatibleGenericBaseClass(typeof(Dictionary<,>))) != null)
            {
                genericArgs = actualTypeToConvert.GetGenericArguments();
                if (genericArgs[0] == typeof(string))
                {
                    converterType = typeof(JsonDictionaryOfStringTValueConverter<,>);
                    elementType = genericArgs[1];
                }
                else
                {
                    return null;
                }
            }
            // Immutable dictionaries from System.Collections.Immutable, e.g. ImmutableDictionary<string, TValue>
            else if (typeToConvert.IsImmutableDictionaryType())
            {
                genericArgs = typeToConvert.GetGenericArguments();
                if (genericArgs[0] == typeof(string))
                {
                    converterType = typeof(JsonImmutableDictionaryOfStringTValueConverter<,>);
                    elementType = genericArgs[1];
                }
                else
                {
                    return null;
                }
            }
            // IDictionary<string,> or deriving from IDictionary<string,>
            else if ((actualTypeToConvert = typeToConvert.GetCompatibleGenericInterface(typeof(IDictionary<,>))) != null)
            {
                genericArgs = actualTypeToConvert.GetGenericArguments();
                if (genericArgs[0] == typeof(string))
                {
                    converterType = typeof(JsonIDictionaryOfStringTValueConverter<,>);
                    elementType = genericArgs[1];
                }
                else
                {
                    return null;
                }
            }
            // IReadOnlyDictionary<string,> or deriving from IReadOnlyDictionary<string,>
            else if ((actualTypeToConvert = typeToConvert.GetCompatibleGenericInterface(typeof(IReadOnlyDictionary<,>))) != null)
            {
                genericArgs = actualTypeToConvert.GetGenericArguments();
                if (genericArgs[0] == typeof(string))
                {
                    converterType = typeof(JsonIReadOnlyDictionaryOfStringTValueConverter<,>);
                    elementType = genericArgs[1];
                }
                else
                {
                    return null;
                }
            }
            // Immutable non-dictionaries from System.Collections.Immutable, e.g. ImmutableStack<T>
            else if (typeToConvert.IsImmutableEnumerableType())
            {
                converterType = typeof(JsonImmutableEnumerableOfTConverter<,>);
                elementType = typeToConvert.GetGenericArguments()[0];
            }
            // IList<>
            else if ((actualTypeToConvert = typeToConvert.GetCompatibleGenericInterface(typeof(IList<>))) != null)
            {
                converterType = typeof(JsonIListOfTConverter<,>);
                elementType = actualTypeToConvert.GetGenericArguments()[0];
            }
            // ISet<>
            else if ((actualTypeToConvert = typeToConvert.GetCompatibleGenericInterface(typeof(ISet<>))) != null)
            {
                converterType = typeof(JsonISetOfTConverter<,>);
                elementType = actualTypeToConvert.GetGenericArguments()[0];
            }
            // ICollection<>
            else if ((actualTypeToConvert = typeToConvert.GetCompatibleGenericInterface(typeof(ICollection<>))) != null)
            {
                converterType = typeof(JsonICollectionOfTConverter<,>);
                elementType = actualTypeToConvert.GetGenericArguments()[0];
            }
            // Stack<> or deriving from Stack<>
            else if ((actualTypeToConvert = typeToConvert.GetCompatibleGenericBaseClass(typeof(Stack<>))) != null)
            {
                converterType = typeof(JsonStackOfTConverter<,>);
                elementType = actualTypeToConvert.GetGenericArguments()[0];
            }
            // Queue<> or deriving from Queue<>
            else if ((actualTypeToConvert = typeToConvert.GetCompatibleGenericBaseClass(typeof(Queue<>))) != null)
            {
                converterType = typeof(JsonQueueOfTConverter<,>);
                elementType = actualTypeToConvert.GetGenericArguments()[0];
            }
            // ConcurrentStack<> or deriving from ConcurrentStack<>
            else if ((actualTypeToConvert = typeToConvert.GetCompatibleGenericBaseClass(typeof(ConcurrentStack<>))) != null)
            {
                converterType = typeof(JsonConcurrentStackOfTConverter<,>);
                elementType = actualTypeToConvert.GetGenericArguments()[0];
            }
            // ConcurrentQueue<> or deriving from ConcurrentQueue<>
            else if ((actualTypeToConvert = typeToConvert.GetCompatibleGenericBaseClass(typeof(ConcurrentQueue<>))) != null)
            {
                converterType = typeof(JsonConcurrentQueueOfTConverter<,>);
                elementType = actualTypeToConvert.GetGenericArguments()[0];
            }
            // IEnumerable<>, types assignable from List<>
            else if ((actualTypeToConvert = typeToConvert.GetCompatibleGenericInterface(typeof(IEnumerable<>))) != null)
            {
                converterType = typeof(JsonIEnumerableOfTConverter<,>);
                elementType = actualTypeToConvert.GetGenericArguments()[0];
            }
            // Check for non-generics after checking for generics.
            else if (typeof(IDictionary).IsAssignableFrom(typeToConvert))
            {
                if (typeToConvert == typeof(IDictionary))
                {
                    return s_converterForIDictionary;
                }

                converterType = typeof(JsonIDictionaryConverter<>);
            }
            else if (typeof(IList).IsAssignableFrom(typeToConvert))
            {
                if (typeToConvert == typeof(IList))
                {
                    return s_converterForIList;
                }

                converterType = typeof(JsonIListConverter<>);
            }
            else if (typeToConvert.IsNonGenericStackOrQueue())
            {
                converterType = typeof(JsonIEnumerableWithAddMethodConverter<>);
            }
            else
            {
                Debug.Assert(typeof(IEnumerable).IsAssignableFrom(typeToConvert));
                if (typeToConvert == typeof(IEnumerable))
                {
                    return s_converterForIEnumerable;
                }

                converterType = typeof(JsonIEnumerableConverter<>);
            }

            if (converterType != null)
            {
                Type genericType;
                if (converterType.GetGenericArguments().Length == 1)
                {
                    genericType = converterType.MakeGenericType(typeToConvert);
                }
                else
                {
                    genericType = converterType.MakeGenericType(typeToConvert, elementType!);
                }

                converter = (JsonConverter)Activator.CreateInstance(
                    genericType,
                    BindingFlags.Instance | BindingFlags.Public,
                    binder: null,
                    args: null,
                    culture: null)!;
            }

            return converter;
        }
    }
}
