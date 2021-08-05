// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text.Json.Serialization.Converters;

namespace System.Text.Json.Serialization.Metadata
{
    public static partial class JsonMetadataServices
    {
        /// <summary>
        /// Creates metadata for an array.
        /// </summary>
        /// <typeparam name="TElement">The generic definition of the element type.</typeparam>
        /// <param name="options">The <see cref="JsonSerializerOptions"/> to use.</param>
        /// <param name="elementInfo">A <see cref="JsonTypeInfo"/> instance representing the element type.</param>
        /// <param name="numberHandling">The <see cref="JsonNumberHandling"/> option to apply to number collection elements.</param>
        /// <param name="serializeFunc">An optimized serialization implementation assuming pre-determined <see cref="JsonSourceGenerationOptionsAttribute"/> defaults.</param>
        /// <returns></returns>
        public static JsonTypeInfo<TElement[]> CreateArrayInfo<TElement>(
            JsonSerializerOptions options,
            JsonTypeInfo elementInfo,
            JsonNumberHandling numberHandling,
            Action<Utf8JsonWriter, TElement[]>? serializeFunc)
            => new JsonTypeInfoInternal<TElement[]>(
                options,
                createObjectFunc: null,
                () => new ArrayConverter<TElement[], TElement>(),
                elementInfo,
                numberHandling,
                serializeFunc,
                typeof(TElement));

        /// <summary>
        /// Creates metadata for types assignable to <see cref="List{T}"/>.
        /// </summary>
        /// <typeparam name="TCollection">The generic definition of the type.</typeparam>
        /// <typeparam name="TElement">The generic definition of the element type.</typeparam>
        /// <param name="options"></param>
        /// <param name="createObjectFunc">A <see cref="Func{TResult}"/> to create an instance of the list when deserializing.</param>
        /// <param name="elementInfo">A <see cref="JsonTypeInfo"/> instance representing the element type.</param>
        /// <param name="numberHandling">The <see cref="JsonNumberHandling"/> option to apply to number collection elements.</param>
        /// <param name="serializeFunc">An optimized serialization implementation assuming pre-determined <see cref="JsonSourceGenerationOptionsAttribute"/> defaults.</param>
        /// <returns></returns>
        public static JsonTypeInfo<TCollection> CreateListInfo<TCollection, TElement>(
            JsonSerializerOptions options,
            Func<TCollection>? createObjectFunc,
            JsonTypeInfo elementInfo,
            JsonNumberHandling numberHandling,
            Action<Utf8JsonWriter, TCollection>? serializeFunc)
            where TCollection : List<TElement>
            => new JsonTypeInfoInternal<TCollection>(
                options,
                createObjectFunc,
                () => new ListOfTConverter<TCollection, TElement>(),
                elementInfo,
                numberHandling,
                serializeFunc,
                typeof(TElement));

        /// <summary>
        /// Creates metadata for types assignable to <see cref="Dictionary{TKey, TValue}"/>.
        /// </summary>
        /// <typeparam name="TCollection">The generic definition of the type.</typeparam>
        /// <typeparam name="TKey">The generic definition of the key type.</typeparam>
        /// <typeparam name="TValue">The generic definition of the value type.</typeparam>
        /// <param name="options"></param>
        /// <param name="createObjectFunc">A <see cref="Func{TResult}"/> to create an instance of the list when deserializing.</param>
        /// <param name="keyInfo">A <see cref="JsonTypeInfo"/> instance representing the key type.</param>
        /// <param name="valueInfo">A <see cref="JsonTypeInfo"/> instance representing the value type.</param>
        /// <param name="numberHandling">The <see cref="JsonNumberHandling"/> option to apply to number collection elements.</param>
        /// <param name="serializeFunc">An optimized serialization implementation assuming pre-determined <see cref="JsonSourceGenerationOptionsAttribute"/> defaults.</param>
        /// <returns></returns>
        public static JsonTypeInfo<TCollection> CreateDictionaryInfo<TCollection, TKey, TValue>(
            JsonSerializerOptions options,
            Func<TCollection> createObjectFunc,
            JsonTypeInfo keyInfo,
            JsonTypeInfo valueInfo,
            JsonNumberHandling numberHandling,
            Action<Utf8JsonWriter, TCollection>? serializeFunc)
            where TCollection : Dictionary<TKey, TValue>
            where TKey : notnull
            => new JsonTypeInfoInternal<TCollection>(
                options,
                createObjectFunc,
                () => new DictionaryOfTKeyTValueConverter<TCollection, TKey, TValue>(),
                keyInfo,
                valueInfo,
                numberHandling,
                serializeFunc,
                typeof(TKey),
                typeof(TValue));


#pragma warning disable CS1574 // XML comment has cref attribute that could not be resolved
        /// <summary>
        /// Creates metadata for <see cref="System.Collections.Immutable.ImmutableDictionary{TKey, TValue}"/> and
        /// types assignable to <see cref="System.Collections.Immutable.IImmutableDictionary{TKey, TValue}"/>.
        /// </summary>
        /// <typeparam name="TCollection">The generic definition of the type.</typeparam>
        /// <typeparam name="TKey">The generic definition of the key type.</typeparam>
        /// <typeparam name="TValue">The generic definition of the value type.</typeparam>
        /// <param name="options"></param>
        /// <param name="createObjectFunc">A <see cref="Func{TResult}"/> to create an instance of the list when deserializing.</param>
        /// <param name="keyInfo">A <see cref="JsonTypeInfo"/> instance representing the key type.</param>
        /// <param name="valueInfo">A <see cref="JsonTypeInfo"/> instance representing the value type.</param>
        /// <param name="numberHandling">The <see cref="JsonNumberHandling"/> option to apply to number collection elements.</param>
        /// <param name="serializeFunc">An optimized serialization implementation assuming pre-determined <see cref="JsonSourceGenerationOptionsAttribute"/> defaults.</param>
        /// <param name="createRangeFunc">A method to create an immutable dictionary instance.</param>
        /// <returns></returns>
#pragma warning restore CS1574 // XML comment has cref attribute that could not be resolved
        public static JsonTypeInfo<TCollection> CreateImmutableDictionaryInfo<TCollection, TKey, TValue>(
            JsonSerializerOptions options,
            Func<TCollection> createObjectFunc,
            JsonTypeInfo keyInfo,
            JsonTypeInfo valueInfo,
            JsonNumberHandling numberHandling,
            Action<Utf8JsonWriter, TCollection>? serializeFunc,
            Func<IEnumerable<KeyValuePair<TKey, TValue>>, TCollection> createRangeFunc)
            where TCollection : IReadOnlyDictionary<TKey, TValue>
            where TKey : notnull
            => new JsonTypeInfoInternal<TCollection>(
                options,
                createObjectFunc,
                () => new ImmutableDictionaryOfTKeyTValueConverter<TCollection, TKey, TValue>(),
                keyInfo,
                valueInfo,
                numberHandling,
                serializeFunc,
                typeof(TKey),
                typeof(TValue),
                createRangeFunc ?? throw new ArgumentNullException(nameof(createRangeFunc)));

        /// <summary>
        /// Creates metadata for types assignable to <see cref="IDictionary{TKey, TValue}"/>.
        /// </summary>
        /// <typeparam name="TCollection">The generic definition of the type.</typeparam>
        /// <typeparam name="TKey">The generic definition of the key type.</typeparam>
        /// <typeparam name="TValue">The generic definition of the value type.</typeparam>
        /// <param name="options"></param>
        /// <param name="createObjectFunc">A <see cref="Func{TResult}"/> to create an instance of the list when deserializing.</param>
        /// <param name="keyInfo">A <see cref="JsonTypeInfo"/> instance representing the key type.</param>
        /// <param name="valueInfo">A <see cref="JsonTypeInfo"/> instance representing the value type.</param>
        /// <param name="numberHandling">The <see cref="JsonNumberHandling"/> option to apply to number collection elements.</param>
        /// <param name="serializeFunc">An optimized serialization implementation assuming pre-determined <see cref="JsonSourceGenerationOptionsAttribute"/> defaults.</param>
        /// <returns></returns>
        public static JsonTypeInfo<TCollection> CreateIDictionaryInfo<TCollection, TKey, TValue>(
            JsonSerializerOptions options,
            Func<TCollection> createObjectFunc,
            JsonTypeInfo keyInfo,
            JsonTypeInfo valueInfo,
            JsonNumberHandling numberHandling,
            Action<Utf8JsonWriter, TCollection>? serializeFunc)
            where TCollection : IDictionary<TKey, TValue>
            where TKey : notnull
            => new JsonTypeInfoInternal<TCollection>(
                options,
                createObjectFunc,
                () => new IDictionaryOfTKeyTValueConverter<TCollection, TKey, TValue>(),
                keyInfo,
                valueInfo,
                numberHandling,
                serializeFunc,
                typeof(TKey),
                typeof(TValue));

        /// <summary>
        /// Creates metadata for types assignable to <see cref="IReadOnlyDictionary{TKey, TValue}"/>.
        /// </summary>
        /// <typeparam name="TCollection">The generic definition of the type.</typeparam>
        /// <typeparam name="TKey">The generic definition of the key type.</typeparam>
        /// <typeparam name="TValue">The generic definition of the value type.</typeparam>
        /// <param name="options"></param>
        /// <param name="createObjectFunc">A <see cref="Func{TResult}"/> to create an instance of the list when deserializing.</param>
        /// <param name="keyInfo">A <see cref="JsonTypeInfo"/> instance representing the key type.</param>
        /// <param name="valueInfo">A <see cref="JsonTypeInfo"/> instance representing the value type.</param>
        /// <param name="numberHandling">The <see cref="JsonNumberHandling"/> option to apply to number collection elements.</param>
        /// <param name="serializeFunc">An optimized serialization implementation assuming pre-determined <see cref="JsonSourceGenerationOptionsAttribute"/> defaults.</param>
        /// <returns></returns>
        public static JsonTypeInfo<TCollection> CreateIReadOnlyDictionaryInfo<TCollection, TKey, TValue>(
            JsonSerializerOptions options,
            Func<TCollection> createObjectFunc,
            JsonTypeInfo keyInfo,
            JsonTypeInfo valueInfo,
            JsonNumberHandling numberHandling,
            Action<Utf8JsonWriter, TCollection>? serializeFunc)
            where TCollection : IReadOnlyDictionary<TKey, TValue>
            where TKey : notnull
            => new JsonTypeInfoInternal<TCollection>(
                options,
                createObjectFunc,
                () => new IReadOnlyDictionaryOfTKeyTValueConverter<TCollection, TKey, TValue>(),
                keyInfo,
                valueInfo,
                numberHandling,
                serializeFunc,
                typeof(TKey),
                typeof(TValue));

#pragma warning disable CS1574 // XML comment has cref attribute that could not be resolved
        /// <summary>
        /// Creates metadata for non-dictionary immutable collection types.
        /// </summary>
        /// <typeparam name="TCollection">The generic definition of the type.</typeparam>
        /// <typeparam name="TElement">The generic definition of the element type.</typeparam>
        /// <param name="options"></param>
        /// <param name="createObjectFunc">A <see cref="Func{TResult}"/> to create an instance of the list when deserializing.</param>
        /// <param name="elementInfo">A <see cref="JsonTypeInfo"/> instance representing the element type.</param>
        /// <param name="numberHandling">The <see cref="JsonNumberHandling"/> option to apply to number collection elements.</param>
        /// <param name="serializeFunc">An optimized serialization implementation assuming pre-determined <see cref="JsonSourceGenerationOptionsAttribute"/> defaults.</param>
        /// <param name="createRangeFunc">A method to create an immutable dictionary instance.</param>
        /// <returns></returns>
#pragma warning restore CS1574 // XML comment has cref attribute that could not be resolved
        public static JsonTypeInfo<TCollection> CreateImmutableEnumerableInfo<TCollection, TElement>(
            JsonSerializerOptions options,
            Func<TCollection>? createObjectFunc,
            JsonTypeInfo elementInfo,
            JsonNumberHandling numberHandling,
            Action<Utf8JsonWriter, TCollection>? serializeFunc,
            Func<IEnumerable<TElement>, TCollection> createRangeFunc)
            where TCollection : IEnumerable<TElement>
            => new JsonTypeInfoInternal<TCollection>(
                options,
                createObjectFunc,
                () => new ImmutableEnumerableOfTConverter<TCollection, TElement>(),
                elementInfo,
                numberHandling,
                serializeFunc,
                typeof(TElement),
                createRangeFunc ?? throw new ArgumentNullException(nameof(createRangeFunc)));

        /// <summary>
        /// Creates metadata for types assignable to <see cref="IList"/>.
        /// </summary>
        /// <typeparam name="TCollection">The generic definition of the type.</typeparam>
        /// <param name="options"></param>
        /// <param name="createObjectFunc">A <see cref="Func{TResult}"/> to create an instance of the list when deserializing.</param>
        /// <param name="objectInfo">A <see cref="JsonTypeInfo"/> instance representing the element type.</param>
        /// <param name="numberHandling">The <see cref="JsonNumberHandling"/> option to apply to number collection elements.</param>
        /// <param name="serializeFunc">An optimized serialization implementation assuming pre-determined <see cref="JsonSourceGenerationOptionsAttribute"/> defaults.</param>
        /// <returns></returns>
        public static JsonTypeInfo<TCollection> CreateIListInfo<TCollection>(
            JsonSerializerOptions options,
            Func<TCollection>? createObjectFunc,
            JsonTypeInfo objectInfo,
            JsonNumberHandling numberHandling,
            Action<Utf8JsonWriter, TCollection>? serializeFunc)
            where TCollection : IList
            => new JsonTypeInfoInternal<TCollection>(
                options,
                createObjectFunc,
                () => new IListConverter<TCollection>(),
                objectInfo,
                numberHandling,
                serializeFunc,
                typeof(object));

        /// <summary>
        /// Creates metadata for types assignable to <see cref="IList{T}"/>.
        /// </summary>
        /// <typeparam name="TCollection">The generic definition of the type.</typeparam>
        /// <typeparam name="TElement">The generic definition of the element type.</typeparam>
        /// <param name="options"></param>
        /// <param name="createObjectFunc">A <see cref="Func{TResult}"/> to create an instance of the list when deserializing.</param>
        /// <param name="elementInfo">A <see cref="JsonTypeInfo"/> instance representing the element type.</param>
        /// <param name="numberHandling">The <see cref="JsonNumberHandling"/> option to apply to number collection elements.</param>
        /// <param name="serializeFunc">An optimized serialization implementation assuming pre-determined <see cref="JsonSourceGenerationOptionsAttribute"/> defaults.</param>
        /// <returns></returns>
        public static JsonTypeInfo<TCollection> CreateIListInfo<TCollection, TElement>(
            JsonSerializerOptions options,
            Func<TCollection>? createObjectFunc,
            JsonTypeInfo elementInfo,
            JsonNumberHandling numberHandling,
            Action<Utf8JsonWriter, TCollection>? serializeFunc)
            where TCollection : IList<TElement>
            => new JsonTypeInfoInternal<TCollection>(
                options,
                createObjectFunc,
                () => new IListOfTConverter<TCollection, TElement>(),
                elementInfo,
                numberHandling,
                serializeFunc,
                typeof(TElement));

        /// <summary>
        /// Creates metadata for types assignable to <see cref="ISet{T}"/>.
        /// </summary>
        /// <typeparam name="TCollection">The generic definition of the type.</typeparam>
        /// <typeparam name="TElement">The generic definition of the element type.</typeparam>
        /// <param name="options"></param>
        /// <param name="createObjectFunc">A <see cref="Func{TResult}"/> to create an instance of the list when deserializing.</param>
        /// <param name="elementInfo">A <see cref="JsonTypeInfo"/> instance representing the element type.</param>
        /// <param name="numberHandling">The <see cref="JsonNumberHandling"/> option to apply to number collection elements.</param>
        /// <param name="serializeFunc">An optimized serialization implementation assuming pre-determined <see cref="JsonSourceGenerationOptionsAttribute"/> defaults.</param>
        /// <returns></returns>
        public static JsonTypeInfo<TCollection> CreateISetInfo<TCollection, TElement>(
            JsonSerializerOptions options,
            Func<TCollection>? createObjectFunc,
            JsonTypeInfo elementInfo,
            JsonNumberHandling numberHandling,
            Action<Utf8JsonWriter, TCollection>? serializeFunc)
            where TCollection : ISet<TElement>
            => new JsonTypeInfoInternal<TCollection>(
                options,
                createObjectFunc,
                () => new ISetOfTConverter<TCollection, TElement>(),
                elementInfo,
                numberHandling,
                serializeFunc,
                typeof(TElement));

        /// <summary>
        /// Creates metadata for types assignable to <see cref="ICollection{T}"/>.
        /// </summary>
        /// <typeparam name="TCollection">The generic definition of the type.</typeparam>
        /// <typeparam name="TElement">The generic definition of the element type.</typeparam>
        /// <param name="options"></param>
        /// <param name="createObjectFunc">A <see cref="Func{TResult}"/> to create an instance of the list when deserializing.</param>
        /// <param name="elementInfo">A <see cref="JsonTypeInfo"/> instance representing the element type.</param>
        /// <param name="numberHandling">The <see cref="JsonNumberHandling"/> option to apply to number collection elements.</param>
        /// <param name="serializeFunc">An optimized serialization implementation assuming pre-determined <see cref="JsonSourceGenerationOptionsAttribute"/> defaults.</param>
        /// <returns></returns>
        public static JsonTypeInfo<TCollection> CreateICollectionInfo<TCollection, TElement>(
            JsonSerializerOptions options,
            Func<TCollection>? createObjectFunc,
            JsonTypeInfo elementInfo,
            JsonNumberHandling numberHandling,
            Action<Utf8JsonWriter, TCollection>? serializeFunc)
            where TCollection : ICollection<TElement>
            => new JsonTypeInfoInternal<TCollection>(
                options,
                createObjectFunc,
                () => new ICollectionOfTConverter<TCollection, TElement>(),
                elementInfo,
                numberHandling,
                serializeFunc,
                typeof(TElement));

        /// <summary>
        /// Creates metadata for types assignable to <see cref="Stack{T}"/>.
        /// </summary>
        /// <typeparam name="TCollection">The generic definition of the type.</typeparam>
        /// <typeparam name="TElement">The generic definition of the element type.</typeparam>
        /// <param name="options"></param>
        /// <param name="createObjectFunc">A <see cref="Func{TResult}"/> to create an instance of the list when deserializing.</param>
        /// <param name="elementInfo">A <see cref="JsonTypeInfo"/> instance representing the element type.</param>
        /// <param name="numberHandling">The <see cref="JsonNumberHandling"/> option to apply to number collection elements.</param>
        /// <param name="serializeFunc">An optimized serialization implementation assuming pre-determined <see cref="JsonSourceGenerationOptionsAttribute"/> defaults.</param>
        /// <returns></returns>
        public static JsonTypeInfo<TCollection> CreateStackInfo<TCollection, TElement>(
            JsonSerializerOptions options,
            Func<TCollection>? createObjectFunc,
            JsonTypeInfo elementInfo,
            JsonNumberHandling numberHandling,
            Action<Utf8JsonWriter, TCollection>? serializeFunc)
            where TCollection : Stack<TElement>
            => new JsonTypeInfoInternal<TCollection>(
                options,
                createObjectFunc,
                () => new StackOfTConverter<TCollection, TElement>(),
                elementInfo,
                numberHandling,
                serializeFunc,
                typeof(TElement));

        /// <summary>
        /// Creates metadata for types assignable to <see cref="Queue{T}"/>.
        /// </summary>
        /// <typeparam name="TCollection">The generic definition of the type.</typeparam>
        /// <typeparam name="TElement">The generic definition of the element type.</typeparam>
        /// <param name="options"></param>
        /// <param name="createObjectFunc">A <see cref="Func{TResult}"/> to create an instance of the list when deserializing.</param>
        /// <param name="elementInfo">A <see cref="JsonTypeInfo"/> instance representing the element type.</param>
        /// <param name="numberHandling">The <see cref="JsonNumberHandling"/> option to apply to number collection elements.</param>
        /// <param name="serializeFunc">An optimized serialization implementation assuming pre-determined <see cref="JsonSourceGenerationOptionsAttribute"/> defaults.</param>
        /// <returns></returns>
        public static JsonTypeInfo<TCollection> CreateQueueInfo<TCollection, TElement>(
            JsonSerializerOptions options,
            Func<TCollection>? createObjectFunc,
            JsonTypeInfo elementInfo,
            JsonNumberHandling numberHandling,
            Action<Utf8JsonWriter, TCollection>? serializeFunc)
            where TCollection : Queue<TElement>
            => new JsonTypeInfoInternal<TCollection>(
                options,
                createObjectFunc,
                () => new QueueOfTConverter<TCollection, TElement>(),
                elementInfo,
                numberHandling,
                serializeFunc,
                typeof(TElement));

        /// <summary>
        /// Creates metadata for types assignable to <see cref="ConcurrentStack{T}"/>.
        /// </summary>
        /// <typeparam name="TCollection">The generic definition of the type.</typeparam>
        /// <typeparam name="TElement">The generic definition of the element type.</typeparam>
        /// <param name="options"></param>
        /// <param name="createObjectFunc">A <see cref="Func{TResult}"/> to create an instance of the list when deserializing.</param>
        /// <param name="elementInfo">A <see cref="JsonTypeInfo"/> instance representing the element type.</param>
        /// <param name="numberHandling">The <see cref="JsonNumberHandling"/> option to apply to number collection elements.</param>
        /// <param name="serializeFunc">An optimized serialization implementation assuming pre-determined <see cref="JsonSourceGenerationOptionsAttribute"/> defaults.</param>
        /// <returns></returns>
        public static JsonTypeInfo<TCollection> CreateConcurrentStackInfo<TCollection, TElement>(
            JsonSerializerOptions options,
            Func<TCollection>? createObjectFunc,
            JsonTypeInfo elementInfo,
            JsonNumberHandling numberHandling,
            Action<Utf8JsonWriter, TCollection>? serializeFunc)
            where TCollection : ConcurrentStack<TElement>
            => new JsonTypeInfoInternal<TCollection>(
                options,
                createObjectFunc,
                () => new ConcurrentStackOfTConverter<TCollection, TElement>(),
                elementInfo,
                numberHandling,
                serializeFunc,
                typeof(TElement));

        /// <summary>
        /// Creates metadata for types assignable to <see cref="Queue{T}"/>.
        /// </summary>
        /// <typeparam name="TCollection">The generic definition of the type.</typeparam>
        /// <typeparam name="TElement">The generic definition of the element type.</typeparam>
        /// <param name="options"></param>
        /// <param name="createObjectFunc">A <see cref="Func{TResult}"/> to create an instance of the list when deserializing.</param>
        /// <param name="elementInfo">A <see cref="JsonTypeInfo"/> instance representing the element type.</param>
        /// <param name="numberHandling">The <see cref="JsonNumberHandling"/> option to apply to number collection elements.</param>
        /// <param name="serializeFunc">An optimized serialization implementation assuming pre-determined <see cref="JsonSourceGenerationOptionsAttribute"/> defaults.</param>
        /// <returns></returns>
        public static JsonTypeInfo<TCollection> CreateConcurrentQueueInfo<TCollection, TElement>(
            JsonSerializerOptions options,
            Func<TCollection>? createObjectFunc,
            JsonTypeInfo elementInfo,
            JsonNumberHandling numberHandling,
            Action<Utf8JsonWriter, TCollection>? serializeFunc)
            where TCollection : ConcurrentQueue<TElement>
            => new JsonTypeInfoInternal<TCollection>(
                options,
                createObjectFunc,
                () => new ConcurrentQueueOfTConverter<TCollection, TElement>(),
                elementInfo,
                numberHandling,
                serializeFunc,
                typeof(TElement));

        /// <summary>
        /// Creates metadata for types assignable to <see cref="IEnumerable{T}"/>.
        /// </summary>
        /// <typeparam name="TCollection">The generic definition of the type.</typeparam>
        /// <typeparam name="TElement">The generic definition of the element type.</typeparam>
        /// <param name="options"></param>
        /// <param name="createObjectFunc">A <see cref="Func{TResult}"/> to create an instance of the list when deserializing.</param>
        /// <param name="elementInfo">A <see cref="JsonTypeInfo"/> instance representing the element type.</param>
        /// <param name="numberHandling">The <see cref="JsonNumberHandling"/> option to apply to number collection elements.</param>
        /// <param name="serializeFunc">An optimized serialization implementation assuming pre-determined <see cref="JsonSourceGenerationOptionsAttribute"/> defaults.</param>
        /// <returns></returns>
        public static JsonTypeInfo<TCollection> CreateIEnumerableInfo<TCollection, TElement>(
            JsonSerializerOptions options,
            Func<TCollection>? createObjectFunc,
            JsonTypeInfo elementInfo,
            JsonNumberHandling numberHandling,
            Action<Utf8JsonWriter, TCollection>? serializeFunc)
            where TCollection : IEnumerable<TElement>
            => new JsonTypeInfoInternal<TCollection>(
                options,
                createObjectFunc,
                () => new IEnumerableOfTConverter<TCollection, TElement>(),
                elementInfo,
                numberHandling,
                serializeFunc,
                typeof(TElement));

        /// <summary>
        /// Creates metadata for types assignable to <see cref="IDictionary"/>.
        /// </summary>
        /// <typeparam name="TCollection">The generic definition of the type.</typeparam>
        /// <param name="options"></param>
        /// <param name="createObjectFunc">A <see cref="Func{TResult}"/> to create an instance of the list when deserializing.</param>
        /// <param name="stringInfo">A <see cref="JsonTypeInfo"/> instance representing <see cref="string"/> instances.</param>
        /// <param name="objectInfo">A <see cref="JsonTypeInfo"/> instance representing <see cref="object"/> instances.</param>
        /// <param name="numberHandling">The <see cref="JsonNumberHandling"/> option to apply to number collection elements.</param>
        /// <param name="serializeFunc">An optimized serialization implementation assuming pre-determined <see cref="JsonSourceGenerationOptionsAttribute"/> defaults.</param>
        /// <returns></returns>
        public static JsonTypeInfo<TCollection> CreateIDictionaryInfo<TCollection>(
            JsonSerializerOptions options,
            Func<TCollection>? createObjectFunc,
            JsonTypeInfo stringInfo,
            JsonTypeInfo objectInfo,
            JsonNumberHandling numberHandling,
            Action<Utf8JsonWriter, TCollection>? serializeFunc)
            where TCollection : IDictionary
            => new JsonTypeInfoInternal<TCollection>(
                options,
                createObjectFunc,
                () => new IDictionaryConverter<TCollection>(),
                keyInfo: stringInfo,
                valueInfo: objectInfo,
                numberHandling,
                serializeFunc,
                typeof(string),
                typeof(object));

        /// <summary>
        /// Creates metadata for types assignable to <see cref="IList"/>.
        /// </summary>
        /// <typeparam name="TCollection">The generic definition of the type.</typeparam>
        /// <param name="options"></param>
        /// <param name="createObjectFunc">A <see cref="Func{TResult}"/> to create an instance of the list when deserializing.</param>
        /// <param name="elementInfo">A <see cref="JsonTypeInfo"/> instance representing the element type.</param>
        /// <param name="numberHandling">The <see cref="JsonNumberHandling"/> option to apply to number collection elements.</param>
        /// <param name="serializeFunc">An optimized serialization implementation assuming pre-determined <see cref="JsonSourceGenerationOptionsAttribute"/> defaults.</param>
        /// <param name="addFunc">A method for adding elements to the collection when using the serializer's code-paths.</param>
        /// <returns></returns>
        public static JsonTypeInfo<TCollection> CreateStackOrQueueInfo<TCollection>(
            JsonSerializerOptions options,
            Func<TCollection>? createObjectFunc,
            JsonTypeInfo elementInfo,
            JsonNumberHandling numberHandling,
            Action<Utf8JsonWriter, TCollection>? serializeFunc,
            Action<TCollection, object?> addFunc)
            where TCollection : IEnumerable
            => new JsonTypeInfoInternal<TCollection>(
                options,
                createObjectFunc,
                () => new StackOrQueueConverter<TCollection>(),
                elementInfo,
                numberHandling,
                serializeFunc,
                typeof(object),
                createObjectWithArgs: null,
                addFunc: addFunc ?? throw new ArgumentNullException(nameof(addFunc)));

        /// <summary>
        /// Creates metadata for types assignable to <see cref="IList"/>.
        /// </summary>
        /// <typeparam name="TCollection">The generic definition of the type.</typeparam>
        /// <param name="options"></param>
        /// <param name="createObjectFunc">A <see cref="Func{TResult}"/> to create an instance of the list when deserializing.</param>
        /// <param name="elementInfo">A <see cref="JsonTypeInfo"/> instance representing the element type.</param>
        /// <param name="numberHandling">The <see cref="JsonNumberHandling"/> option to apply to number collection elements.</param>
        /// <param name="serializeFunc">An optimized serialization implementation assuming pre-determined <see cref="JsonSourceGenerationOptionsAttribute"/> defaults.</param>
        /// <returns></returns>
        public static JsonTypeInfo<TCollection> CreateIEnumerableInfo<TCollection>(
            JsonSerializerOptions options,
            Func<TCollection>? createObjectFunc,
            JsonTypeInfo elementInfo,
            JsonNumberHandling numberHandling,
            Action<Utf8JsonWriter, TCollection>? serializeFunc)
            where TCollection : IEnumerable
            => new JsonTypeInfoInternal<TCollection>(
                options,
                createObjectFunc,
                () => new IEnumerableConverter<TCollection>(),
                elementInfo,
                numberHandling,
                serializeFunc,
                typeof(object));
    }
}
