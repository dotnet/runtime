// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;

namespace System.Text.Json.Serialization.Converters
{
    /// <summary>
    /// Converter factory for all IEnumerable types.
    /// </summary>
    internal sealed class IEnumerableConverterFactory : JsonConverterFactory
    {
        private static readonly IDictionaryConverter<IDictionary> s_converterForIDictionary = new IDictionaryConverter<IDictionary>();
        private static readonly IEnumerableConverter<IEnumerable> s_converterForIEnumerable = new IEnumerableConverter<IEnumerable>();
        private static readonly IListConverter<IList> s_converterForIList = new IListConverter<IList>();

        public override bool CanConvert(Type typeToConvert)
        {
            return typeof(IEnumerable).IsAssignableFrom(typeToConvert);
        }

        public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
        {
            Type converterType;
            Type[] genericArgs;
            Type? elementType = null;
            Type? dictionaryKeyType = null;
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
            // Dictionary<TKey, TValue> or deriving from Dictionary<TKey, TValue>
            else if ((actualTypeToConvert = typeToConvert.GetCompatibleGenericBaseClass(typeof(Dictionary<,>))) != null)
            {
                genericArgs = actualTypeToConvert.GetGenericArguments();
                converterType = typeof(DictionaryOfTKeyTValueConverter<,,>);
                dictionaryKeyType = genericArgs[0];
                elementType = genericArgs[1];
            }
            // Immutable dictionaries from System.Collections.Immutable, e.g. ImmutableDictionary<TKey, TValue>
            else if (typeToConvert.IsImmutableDictionaryType())
            {
                genericArgs = typeToConvert.GetGenericArguments();
                converterType = typeof(ImmutableDictionaryOfTKeyTValueConverter<,,>);
                dictionaryKeyType = genericArgs[0];
                elementType = genericArgs[1];
            }
            // IDictionary<TKey, TValue> or deriving from IDictionary<TKey, TValue>
            else if ((actualTypeToConvert = typeToConvert.GetCompatibleGenericInterface(typeof(IDictionary<,>))) != null)
            {
                genericArgs = actualTypeToConvert.GetGenericArguments();
                converterType = typeof(IDictionaryOfTKeyTValueConverter<,,>);
                dictionaryKeyType = genericArgs[0];
                elementType = genericArgs[1];
            }
            // IReadOnlyDictionary<TKey, TValue> or deriving from IReadOnlyDictionary<TKey, TValue>
            else if ((actualTypeToConvert = typeToConvert.GetCompatibleGenericInterface(typeof(IReadOnlyDictionary<,>))) != null)
            {
                genericArgs = actualTypeToConvert.GetGenericArguments();
                converterType = typeof(IReadOnlyDictionaryOfTKeyTValueConverter<,,>);
                dictionaryKeyType = genericArgs[0];
                elementType = genericArgs[1];
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

            Type genericType;
            int numberOfGenericArgs = converterType.GetGenericArguments().Length;
            if (numberOfGenericArgs == 1)
            {
                genericType = converterType.MakeGenericType(typeToConvert);
            }
            else if (numberOfGenericArgs == 2)
            {
                genericType = converterType.MakeGenericType(typeToConvert, elementType!);
            }
            else
            {
                Debug.Assert(numberOfGenericArgs == 3);
                genericType = converterType.MakeGenericType(typeToConvert, dictionaryKeyType!, elementType!);
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
