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
    internal class IEnumerableConverterFactory : JsonConverterFactory
    {
        private static readonly IDictionaryConverter<IDictionary> s_converterForIDictionary = new IDictionaryConverter<IDictionary>();
        private static readonly IEnumerableConverter<IEnumerable> s_converterForIEnumerable = new IEnumerableConverter<IEnumerable>();
        private static readonly IListConverter<IList> s_converterForIList = new IListConverter<IList>();

        public override bool CanConvert(Type typeToConvert)
        {
            return typeof(IEnumerable).IsAssignableFrom(typeToConvert);
        }

        [PreserveDependency(".ctor", "System.Text.Json.Serialization.Converters.ArrayConverter`2")]
        [PreserveDependency(".ctor", "System.Text.Json.Serialization.Converters.ConcurrentQueueOfTConverter`2")]
        [PreserveDependency(".ctor", "System.Text.Json.Serialization.Converters.ConcurrentStackOfTConverter`2")]
        [PreserveDependency(".ctor", "System.Text.Json.Serialization.Converters.DictionaryOfStringTValueConverter`2")]
        [PreserveDependency(".ctor", "System.Text.Json.Serialization.Converters.ICollectionOfTConverter`2")]
        [PreserveDependency(".ctor", "System.Text.Json.Serialization.Converters.IDictionaryOfStringTValueConverter`2")]
        [PreserveDependency(".ctor", "System.Text.Json.Serialization.Converters.IEnumerableOfTConverter`2")]
        [PreserveDependency(".ctor", "System.Text.Json.Serialization.Converters.IEnumerableWithAddMethodConverter`1")]
        [PreserveDependency(".ctor", "System.Text.Json.Serialization.Converters.IListConverter`1")]
        [PreserveDependency(".ctor", "System.Text.Json.Serialization.Converters.IListOfTConverter`2")]
        [PreserveDependency(".ctor", "System.Text.Json.Serialization.Converters.ImmutableDictionaryOfStringTValueConverter`2")]
        [PreserveDependency(".ctor", "System.Text.Json.Serialization.Converters.ImmutableEnumerableOfTConverter`2")]
        [PreserveDependency(".ctor", "System.Text.Json.Serialization.Converters.IReadOnlyDictionaryOfStringTValueConverter`2")]
        [PreserveDependency(".ctor", "System.Text.Json.Serialization.Converters.ISetOfTConverter`2")]
        [PreserveDependency(".ctor", "System.Text.Json.Serialization.Converters.ListOfTConverter`2")]
        [PreserveDependency(".ctor", "System.Text.Json.Serialization.Converters.QueueOfTConverter`2")]
        [PreserveDependency(".ctor", "System.Text.Json.Serialization.Converters.StackOfTConverter`2")]
        public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
        {
            Type converterType = null!;
            Type[] genericArgs;
            Type? elementType = null;
            Type? actualTypeToConvert;

            // Array
            if (typeToConvert.IsArray)
            {
                // Verify that we don't have a multidimensional array.
                if (typeToConvert.GetArrayRank() > 1)
                {
                    ThrowHelper.ThrowNotSupportedException_SerializationNotSupported(typeToConvert);
                }

                converterType = typeof(ArrayConverter<,>);
                elementType = typeToConvert.GetElementType();
            }
            // List<> or deriving from List<>
            else if ((actualTypeToConvert = typeToConvert.GetCompatibleGenericBaseClass(typeof(List<>))) != null)
            {
                converterType = typeof(ListOfTConverter<,>);
                elementType = actualTypeToConvert.GetGenericArguments()[0];
            }
            // Dictionary<string,> or deriving from Dictionary<string,>
            else if ((actualTypeToConvert = typeToConvert.GetCompatibleGenericBaseClass(typeof(Dictionary<,>))) != null)
            {
                genericArgs = actualTypeToConvert.GetGenericArguments();
                if (genericArgs[0] == typeof(string))
                {
                    converterType = typeof(DictionaryOfStringTValueConverter<,>);
                    elementType = genericArgs[1];
                }
                else
                {
                    ThrowHelper.ThrowNotSupportedException_SerializationNotSupported(typeToConvert);
                }
            }
            // Immutable dictionaries from System.Collections.Immutable, e.g. ImmutableDictionary<string, TValue>
            else if (typeToConvert.IsImmutableDictionaryType())
            {
                genericArgs = typeToConvert.GetGenericArguments();
                if (genericArgs[0] == typeof(string))
                {
                    converterType = typeof(ImmutableDictionaryOfStringTValueConverter<,>);
                    elementType = genericArgs[1];
                }
                else
                {
                    ThrowHelper.ThrowNotSupportedException_SerializationNotSupported(typeToConvert);
                }
            }
            // IDictionary<string,> or deriving from IDictionary<string,>
            else if ((actualTypeToConvert = typeToConvert.GetCompatibleGenericInterface(typeof(IDictionary<,>))) != null)
            {
                genericArgs = actualTypeToConvert.GetGenericArguments();
                if (genericArgs[0] == typeof(string))
                {
                    converterType = typeof(IDictionaryOfStringTValueConverter<,>);
                    elementType = genericArgs[1];
                }
                else
                {
                    ThrowHelper.ThrowNotSupportedException_SerializationNotSupported(typeToConvert);
                }
            }
            // IReadOnlyDictionary<string,> or deriving from IReadOnlyDictionary<string,>
            else if ((actualTypeToConvert = typeToConvert.GetCompatibleGenericInterface(typeof(IReadOnlyDictionary<,>))) != null)
            {
                genericArgs = actualTypeToConvert.GetGenericArguments();
                if (genericArgs[0] == typeof(string))
                {
                    converterType = typeof(IReadOnlyDictionaryOfStringTValueConverter<,>);
                    elementType = genericArgs[1];
                }
                else
                {
                    ThrowHelper.ThrowNotSupportedException_SerializationNotSupported(typeToConvert);
                }
            }
            // Immutable non-dictionaries from System.Collections.Immutable, e.g. ImmutableStack<T>
            else if (typeToConvert.IsImmutableEnumerableType())
            {
                converterType = typeof(ImmutableEnumerableOfTConverter<,>);
                elementType = typeToConvert.GetGenericArguments()[0];
            }
            // IList<>
            else if ((actualTypeToConvert = typeToConvert.GetCompatibleGenericInterface(typeof(IList<>))) != null)
            {
                converterType = typeof(IListOfTConverter<,>);
                elementType = actualTypeToConvert.GetGenericArguments()[0];
            }
            // ISet<>
            else if ((actualTypeToConvert = typeToConvert.GetCompatibleGenericInterface(typeof(ISet<>))) != null)
            {
                converterType = typeof(ISetOfTConverter<,>);
                elementType = actualTypeToConvert.GetGenericArguments()[0];
            }
            // ICollection<>
            else if ((actualTypeToConvert = typeToConvert.GetCompatibleGenericInterface(typeof(ICollection<>))) != null)
            {
                converterType = typeof(ICollectionOfTConverter<,>);
                elementType = actualTypeToConvert.GetGenericArguments()[0];
            }
            // Stack<> or deriving from Stack<>
            else if ((actualTypeToConvert = typeToConvert.GetCompatibleGenericBaseClass(typeof(Stack<>))) != null)
            {
                converterType = typeof(StackOfTConverter<,>);
                elementType = actualTypeToConvert.GetGenericArguments()[0];
            }
            // Queue<> or deriving from Queue<>
            else if ((actualTypeToConvert = typeToConvert.GetCompatibleGenericBaseClass(typeof(Queue<>))) != null)
            {
                converterType = typeof(QueueOfTConverter<,>);
                elementType = actualTypeToConvert.GetGenericArguments()[0];
            }
            // ConcurrentStack<> or deriving from ConcurrentStack<>
            else if ((actualTypeToConvert = typeToConvert.GetCompatibleGenericBaseClass(typeof(ConcurrentStack<>))) != null)
            {
                converterType = typeof(ConcurrentStackOfTConverter<,>);
                elementType = actualTypeToConvert.GetGenericArguments()[0];
            }
            // ConcurrentQueue<> or deriving from ConcurrentQueue<>
            else if ((actualTypeToConvert = typeToConvert.GetCompatibleGenericBaseClass(typeof(ConcurrentQueue<>))) != null)
            {
                converterType = typeof(ConcurrentQueueOfTConverter<,>);
                elementType = actualTypeToConvert.GetGenericArguments()[0];
            }
            // IEnumerable<>, types assignable from List<>
            else if ((actualTypeToConvert = typeToConvert.GetCompatibleGenericInterface(typeof(IEnumerable<>))) != null)
            {
                converterType = typeof(IEnumerableOfTConverter<,>);
                elementType = actualTypeToConvert.GetGenericArguments()[0];
            }
            // Check for non-generics after checking for generics.
            else if (typeof(IDictionary).IsAssignableFrom(typeToConvert))
            {
                if (typeToConvert == typeof(IDictionary))
                {
                    return s_converterForIDictionary;
                }

                converterType = typeof(IDictionaryConverter<>);
            }
            else if (typeof(IList).IsAssignableFrom(typeToConvert))
            {
                if (typeToConvert == typeof(IList))
                {
                    return s_converterForIList;
                }

                converterType = typeof(IListConverter<>);
            }
            else if (typeToConvert.IsNonGenericStackOrQueue())
            {
                converterType = typeof(IEnumerableWithAddMethodConverter<>);
            }
            else
            {
                Debug.Assert(typeof(IEnumerable).IsAssignableFrom(typeToConvert));
                if (typeToConvert == typeof(IEnumerable))
                {
                    return s_converterForIEnumerable;
                }

                converterType = typeof(IEnumerableConverter<>);
            }

            Debug.Assert(converterType != null);

            Type genericType;
            if (converterType.GetGenericArguments().Length == 1)
            {
                genericType = converterType.MakeGenericType(typeToConvert);
            }
            else
            {
                genericType = converterType.MakeGenericType(typeToConvert, elementType!);
            }

            JsonConverter converter = (JsonConverter)Activator.CreateInstance(
                genericType,
                BindingFlags.Instance | BindingFlags.Public,
                binder: null,
                args: null,
                culture: null)!;

            return converter;
        }
    }
}
