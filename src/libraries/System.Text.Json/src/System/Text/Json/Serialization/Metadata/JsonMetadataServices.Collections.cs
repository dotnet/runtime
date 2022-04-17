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
        /// Creates serialization metadata for an array.
        /// </summary>
        /// <typeparam name="TElement">The generic definition of the element type.</typeparam>
        /// <param name="options">The <see cref="JsonSerializerOptions"/> to use.</param>
        /// <param name="collectionInfo">Provides serialization metadata about the collection type.</param>
        /// <returns>Serialization metadata for the given type.</returns>
        /// <remarks>This API is for use by the output of the System.Text.Json source generator and should not be called directly.</remarks>
        public static JsonTypeInfo<TElement[]> CreateArrayInfo<TElement>(JsonSerializerOptions options, JsonCollectionInfoValues<TElement[]> collectionInfo)
            => new SourceGenJsonTypeInfo<TElement[]>(
                options,
                collectionInfo,
                () => new ArrayConverter<TElement[], TElement>());

        /// <summary>
        /// Creates serialization metadata for types assignable to <see cref="List{T}"/>.
        /// </summary>
        /// <typeparam name="TCollection">The generic definition of the type.</typeparam>
        /// <typeparam name="TElement">The generic definition of the element type.</typeparam>
        /// <param name="options">The <see cref="JsonSerializerOptions"/> to use.</param>
        /// <param name="collectionInfo">Provides serialization metadata about the collection type.</param>
        /// <returns>Serialization metadata for the given type.</returns>
        /// <remarks>This API is for use by the output of the System.Text.Json source generator and should not be called directly.</remarks>
        public static JsonTypeInfo<TCollection> CreateListInfo<TCollection, TElement>(
            JsonSerializerOptions options,
            JsonCollectionInfoValues<TCollection> collectionInfo)
            where TCollection : List<TElement>
            => new SourceGenJsonTypeInfo<TCollection>(
                options,
                collectionInfo,
                () => new ListOfTConverter<TCollection, TElement>());

        /// <summary>
        /// Creates serialization metadata for types assignable to <see cref="Dictionary{TKey, TValue}"/>.
        /// </summary>
        /// <typeparam name="TCollection">The generic definition of the type.</typeparam>
        /// <typeparam name="TKey">The generic definition of the key type.</typeparam>
        /// <typeparam name="TValue">The generic definition of the value type.</typeparam>
        /// <param name="options"></param>
        /// <param name="collectionInfo">Provides serialization metadata about the collection type.</param>
        /// <returns>Serialization metadata for the given type.</returns>
        /// <remarks>This API is for use by the output of the System.Text.Json source generator and should not be called directly.</remarks>
        public static JsonTypeInfo<TCollection> CreateDictionaryInfo<TCollection, TKey, TValue>(
            JsonSerializerOptions options,
            JsonCollectionInfoValues<TCollection> collectionInfo)
            where TCollection : Dictionary<TKey, TValue>
            where TKey : notnull
            => new SourceGenJsonTypeInfo<TCollection>(
                options,
                collectionInfo,
                () => new DictionaryOfTKeyTValueConverter<TCollection, TKey, TValue>());


#pragma warning disable CS1574 // XML comment has cref attribute that could not be resolved
        /// <summary>
        /// Creates serialization metadata for <see cref="System.Collections.Immutable.ImmutableDictionary{TKey, TValue}"/> and
        /// types assignable to <see cref="System.Collections.Immutable.IImmutableDictionary{TKey, TValue}"/>.
        /// </summary>
        /// <typeparam name="TCollection">The generic definition of the type.</typeparam>
        /// <typeparam name="TKey">The generic definition of the key type.</typeparam>
        /// <typeparam name="TValue">The generic definition of the value type.</typeparam>
        /// <param name="options"></param>
        /// <param name="collectionInfo">Provides serialization metadata about the collection type.</param>
        /// <param name="createRangeFunc">A method to create an immutable dictionary instance.</param>
        /// <returns>Serialization metadata for the given type.</returns>
        /// <remarks>This API is for use by the output of the System.Text.Json source generator and should not be called directly.</remarks>
#pragma warning restore CS1574 // XML comment has cref attribute that could not be resolved
        public static JsonTypeInfo<TCollection> CreateImmutableDictionaryInfo<TCollection, TKey, TValue>(
            JsonSerializerOptions options,
            JsonCollectionInfoValues<TCollection> collectionInfo,
            Func<IEnumerable<KeyValuePair<TKey, TValue>>, TCollection> createRangeFunc!!)
            where TCollection : IReadOnlyDictionary<TKey, TValue>
            where TKey : notnull
            => new SourceGenJsonTypeInfo<TCollection>(
                options,
                collectionInfo,
                () => new ImmutableDictionaryOfTKeyTValueConverter<TCollection, TKey, TValue>(),
                createObjectWithArgs: createRangeFunc);

        /// <summary>
        /// Creates serialization metadata for types assignable to <see cref="IDictionary{TKey, TValue}"/>.
        /// </summary>
        /// <typeparam name="TCollection">The generic definition of the type.</typeparam>
        /// <typeparam name="TKey">The generic definition of the key type.</typeparam>
        /// <typeparam name="TValue">The generic definition of the value type.</typeparam>
        /// <param name="options"></param>
        /// <param name="collectionInfo">Provides serialization metadata about the collection type.</param>
        /// <returns>Serialization metadata for the given type.</returns>
        /// <remarks>This API is for use by the output of the System.Text.Json source generator and should not be called directly.</remarks>
        public static JsonTypeInfo<TCollection> CreateIDictionaryInfo<TCollection, TKey, TValue>(
            JsonSerializerOptions options,
            JsonCollectionInfoValues<TCollection> collectionInfo)
            where TCollection : IDictionary<TKey, TValue>
            where TKey : notnull
            => new SourceGenJsonTypeInfo<TCollection>(
                options,
                collectionInfo,
                () => new IDictionaryOfTKeyTValueConverter<TCollection, TKey, TValue>());

        /// <summary>
        /// Creates serialization metadata for types assignable to <see cref="IReadOnlyDictionary{TKey, TValue}"/>.
        /// </summary>
        /// <typeparam name="TCollection">The generic definition of the type.</typeparam>
        /// <typeparam name="TKey">The generic definition of the key type.</typeparam>
        /// <typeparam name="TValue">The generic definition of the value type.</typeparam>
        /// <param name="options"></param>
        /// <param name="collectionInfo">Provides serialization metadata about the collection type.</param>
        /// <returns>Serialization metadata for the given type.</returns>
        /// <remarks>This API is for use by the output of the System.Text.Json source generator and should not be called directly.</remarks>
        public static JsonTypeInfo<TCollection> CreateIReadOnlyDictionaryInfo<TCollection, TKey, TValue>(
            JsonSerializerOptions options,
            JsonCollectionInfoValues<TCollection> collectionInfo)
            where TCollection : IReadOnlyDictionary<TKey, TValue>
            where TKey : notnull
            => new SourceGenJsonTypeInfo<TCollection>(
                options,
                collectionInfo,
                () => new IReadOnlyDictionaryOfTKeyTValueConverter<TCollection, TKey, TValue>());

#pragma warning disable CS1574 // XML comment has cref attribute that could not be resolved
        /// <summary>
        /// Creates serialization metadata for non-dictionary immutable collection types.
        /// </summary>
        /// <typeparam name="TCollection">The generic definition of the type.</typeparam>
        /// <typeparam name="TElement">The generic definition of the element type.</typeparam>
        /// <param name="options"></param>
        /// <param name="collectionInfo">Provides serialization metadata about the collection type.</param>
        /// <param name="createRangeFunc">A method to create an immutable dictionary instance.</param>
        /// <returns>Serialization metadata for the given type.</returns>
        /// <remarks>This API is for use by the output of the System.Text.Json source generator and should not be called directly.</remarks>
#pragma warning restore CS1574 // XML comment has cref attribute that could not be resolved
        public static JsonTypeInfo<TCollection> CreateImmutableEnumerableInfo<TCollection, TElement>(
            JsonSerializerOptions options,
            JsonCollectionInfoValues<TCollection> collectionInfo,
            Func<IEnumerable<TElement>, TCollection> createRangeFunc!!)
            where TCollection : IEnumerable<TElement>
            => new SourceGenJsonTypeInfo<TCollection>(
                options,
                collectionInfo,
                () => new ImmutableEnumerableOfTConverter<TCollection, TElement>(),
                createObjectWithArgs: createRangeFunc);

        /// <summary>
        /// Creates serialization metadata for types assignable to <see cref="IList"/>.
        /// </summary>
        /// <typeparam name="TCollection">The generic definition of the type.</typeparam>
        /// <param name="options"></param>
        /// <param name="collectionInfo">Provides serialization metadata about the collection type.</param>
        /// <returns>Serialization metadata for the given type.</returns>
        /// <remarks>This API is for use by the output of the System.Text.Json source generator and should not be called directly.</remarks>
        public static JsonTypeInfo<TCollection> CreateIListInfo<TCollection>(
            JsonSerializerOptions options,
            JsonCollectionInfoValues<TCollection> collectionInfo)
            where TCollection : IList
            => new SourceGenJsonTypeInfo<TCollection>(
                options,
                collectionInfo,
                () => new IListConverter<TCollection>());

        /// <summary>
        /// Creates serialization metadata for types assignable to <see cref="IList{T}"/>.
        /// </summary>
        /// <typeparam name="TCollection">The generic definition of the type.</typeparam>
        /// <typeparam name="TElement">The generic definition of the element type.</typeparam>
        /// <param name="options"></param>
        /// <param name="collectionInfo">Provides serialization metadata about the collection type.</param>
        /// <returns>Serialization metadata for the given type.</returns>
        /// <remarks>This API is for use by the output of the System.Text.Json source generator and should not be called directly.</remarks>
        public static JsonTypeInfo<TCollection> CreateIListInfo<TCollection, TElement>(
            JsonSerializerOptions options,
            JsonCollectionInfoValues<TCollection> collectionInfo)
            where TCollection : IList<TElement>
            => new SourceGenJsonTypeInfo<TCollection>(
                options,
                collectionInfo,
                () => new IListOfTConverter<TCollection, TElement>());

        /// <summary>
        /// Creates serialization metadata for types assignable to <see cref="ISet{T}"/>.
        /// </summary>
        /// <typeparam name="TCollection">The generic definition of the type.</typeparam>
        /// <typeparam name="TElement">The generic definition of the element type.</typeparam>
        /// <param name="options"></param>
        /// <param name="collectionInfo">Provides serialization metadata about the collection type.</param>
        /// <returns>Serialization metadata for the given type.</returns>
        /// <remarks>This API is for use by the output of the System.Text.Json source generator and should not be called directly.</remarks>
        public static JsonTypeInfo<TCollection> CreateISetInfo<TCollection, TElement>(
            JsonSerializerOptions options,
            JsonCollectionInfoValues<TCollection> collectionInfo)
            where TCollection : ISet<TElement>
            => new SourceGenJsonTypeInfo<TCollection>(
                options,
                collectionInfo,
                () => new ISetOfTConverter<TCollection, TElement>());

        /// <summary>
        /// Creates serialization metadata for types assignable to <see cref="ICollection{T}"/>.
        /// </summary>
        /// <typeparam name="TCollection">The generic definition of the type.</typeparam>
        /// <typeparam name="TElement">The generic definition of the element type.</typeparam>
        /// <param name="options"></param>
        /// <param name="collectionInfo">Provides serialization metadata about the collection type.</param>
        /// <returns>Serialization metadata for the given type.</returns>
        /// <remarks>This API is for use by the output of the System.Text.Json source generator and should not be called directly.</remarks>
        public static JsonTypeInfo<TCollection> CreateICollectionInfo<TCollection, TElement>(
            JsonSerializerOptions options,
            JsonCollectionInfoValues<TCollection> collectionInfo)
            where TCollection : ICollection<TElement>
            => new SourceGenJsonTypeInfo<TCollection>(
                options,
                collectionInfo,
                () => new ICollectionOfTConverter<TCollection, TElement>());

        /// <summary>
        /// Creates serialization metadata for types assignable to <see cref="Stack{T}"/>.
        /// </summary>
        /// <typeparam name="TCollection">The generic definition of the type.</typeparam>
        /// <typeparam name="TElement">The generic definition of the element type.</typeparam>
        /// <param name="options"></param>
        /// <param name="collectionInfo">Provides serialization metadata about the collection type.</param>
        /// <returns>Serialization metadata for the given type.</returns>
        /// <remarks>This API is for use by the output of the System.Text.Json source generator and should not be called directly.</remarks>
        public static JsonTypeInfo<TCollection> CreateStackInfo<TCollection, TElement>(
            JsonSerializerOptions options,
            JsonCollectionInfoValues<TCollection> collectionInfo)
            where TCollection : Stack<TElement>
            => new SourceGenJsonTypeInfo<TCollection>(
                options,
                collectionInfo,
                () => new StackOfTConverter<TCollection, TElement>());

        /// <summary>
        /// Creates serialization metadata for types assignable to <see cref="Queue{T}"/>.
        /// </summary>
        /// <typeparam name="TCollection">The generic definition of the type.</typeparam>
        /// <typeparam name="TElement">The generic definition of the element type.</typeparam>
        /// <param name="options"></param>
        /// <param name="collectionInfo">Provides serialization metadata about the collection type.</param>
        /// <returns>Serialization metadata for the given type.</returns>
        /// <remarks>This API is for use by the output of the System.Text.Json source generator and should not be called directly.</remarks>
        public static JsonTypeInfo<TCollection> CreateQueueInfo<TCollection, TElement>(
            JsonSerializerOptions options,
            JsonCollectionInfoValues<TCollection> collectionInfo)
            where TCollection : Queue<TElement>
            => new SourceGenJsonTypeInfo<TCollection>(
                options,
                collectionInfo,
                () => new QueueOfTConverter<TCollection, TElement>());

        /// <summary>
        /// Creates serialization metadata for types assignable to <see cref="ConcurrentStack{T}"/>.
        /// </summary>
        /// <typeparam name="TCollection">The generic definition of the type.</typeparam>
        /// <typeparam name="TElement">The generic definition of the element type.</typeparam>
        /// <param name="options"></param>
        /// <param name="collectionInfo">Provides serialization metadata about the collection type.</param>
        /// <returns>Serialization metadata for the given type.</returns>
        /// <remarks>This API is for use by the output of the System.Text.Json source generator and should not be called directly.</remarks>
        public static JsonTypeInfo<TCollection> CreateConcurrentStackInfo<TCollection, TElement>(
            JsonSerializerOptions options,
            JsonCollectionInfoValues<TCollection> collectionInfo)
            where TCollection : ConcurrentStack<TElement>
            => new SourceGenJsonTypeInfo<TCollection>(
                options,
                collectionInfo,
                () => new ConcurrentStackOfTConverter<TCollection, TElement>());

        /// <summary>
        /// Creates serialization metadata for types assignable to <see cref="Queue{T}"/>.
        /// </summary>
        /// <typeparam name="TCollection">The generic definition of the type.</typeparam>
        /// <typeparam name="TElement">The generic definition of the element type.</typeparam>
        /// <param name="options"></param>
        /// <param name="collectionInfo">Provides serialization metadata about the collection type.</param>
        /// <returns>Serialization metadata for the given type.</returns>
        /// <remarks>This API is for use by the output of the System.Text.Json source generator and should not be called directly.</remarks>
        public static JsonTypeInfo<TCollection> CreateConcurrentQueueInfo<TCollection, TElement>(
            JsonSerializerOptions options,
            JsonCollectionInfoValues<TCollection> collectionInfo)
            where TCollection : ConcurrentQueue<TElement>
            => new SourceGenJsonTypeInfo<TCollection>(
                options,
                collectionInfo,
                () => new ConcurrentQueueOfTConverter<TCollection, TElement>());

        /// <summary>
        /// Creates serialization metadata for types assignable to <see cref="IEnumerable{T}"/>.
        /// </summary>
        /// <typeparam name="TCollection">The generic definition of the type.</typeparam>
        /// <typeparam name="TElement">The generic definition of the element type.</typeparam>
        /// <param name="options"></param>
        /// <param name="collectionInfo">Provides serialization metadata about the collection type.</param>
        /// <returns>Serialization metadata for the given type.</returns>
        /// <remarks>This API is for use by the output of the System.Text.Json source generator and should not be called directly.</remarks>
        public static JsonTypeInfo<TCollection> CreateIEnumerableInfo<TCollection, TElement>(
            JsonSerializerOptions options,
            JsonCollectionInfoValues<TCollection> collectionInfo)
            where TCollection : IEnumerable<TElement>
            => new SourceGenJsonTypeInfo<TCollection>(
                options,
                collectionInfo,
                () => new IEnumerableOfTConverter<TCollection, TElement>());

        /// <summary>
        /// Creates serialization metadata for types assignable to <see cref="IDictionary"/>.
        /// </summary>
        /// <typeparam name="TCollection">The generic definition of the type.</typeparam>
        /// <param name="options"></param>
        /// <param name="collectionInfo">Provides serialization metadata about the collection type.</param>
        /// <returns>Serialization metadata for the given type.</returns>
        /// <remarks>This API is for use by the output of the System.Text.Json source generator and should not be called directly.</remarks>
        public static JsonTypeInfo<TCollection> CreateIDictionaryInfo<TCollection>(
            JsonSerializerOptions options,
            JsonCollectionInfoValues<TCollection> collectionInfo)
            where TCollection : IDictionary
            => new SourceGenJsonTypeInfo<TCollection>(
                options,
                collectionInfo,
                () => new IDictionaryConverter<TCollection>());

#pragma warning disable CS1574 // XML comment has cref attribute that could not be resolved
        /// <summary>
        /// Creates serialization metadata for <see cref="System.Collections.Stack"/> types.
        /// </summary>
        /// <typeparam name="TCollection">The generic definition of the type.</typeparam>
        /// <param name="options"></param>
        /// <param name="collectionInfo">Provides serialization metadata about the collection type.</param>
        /// <param name="addFunc">A method for adding elements to the collection when using the serializer's code-paths.</param>
        /// <returns>Serialization metadata for the given type.</returns>
        /// <remarks>This API is for use by the output of the System.Text.Json source generator and should not be called directly.</remarks>
#pragma warning restore CS1574 // XML comment has cref attribute that could not be resolved
        public static JsonTypeInfo<TCollection> CreateStackInfo<TCollection>(
            JsonSerializerOptions options,
            JsonCollectionInfoValues<TCollection> collectionInfo,
            Action<TCollection, object?> addFunc)
            where TCollection : IEnumerable
            => CreateStackOrQueueInfo(options, collectionInfo, addFunc);

#pragma warning disable CS1574 // XML comment has cref attribute that could not be resolved
        /// <summary>
        /// Creates serialization metadata for <see cref="System.Collections.Queue"/> types.
        /// </summary>
        /// <typeparam name="TCollection">The generic definition of the type.</typeparam>
        /// <param name="options"></param>
        /// <param name="collectionInfo">Provides serialization metadata about the collection type.</param>
        /// <param name="addFunc">A method for adding elements to the collection when using the serializer's code-paths.</param>
        /// <returns>Serialization metadata for the given type.</returns>
        /// <remarks>This API is for use by the output of the System.Text.Json source generator and should not be called directly.</remarks>
#pragma warning restore CS1574 // XML comment has cref attribute that could not be resolved
        public static JsonTypeInfo<TCollection> CreateQueueInfo<TCollection>(
            JsonSerializerOptions options,
            JsonCollectionInfoValues<TCollection> collectionInfo,
            Action<TCollection, object?> addFunc)
            where TCollection : IEnumerable
            => CreateStackOrQueueInfo(options, collectionInfo, addFunc);

        private static JsonTypeInfo<TCollection> CreateStackOrQueueInfo<TCollection>(
            JsonSerializerOptions options,
            JsonCollectionInfoValues<TCollection> collectionInfo,
            Action<TCollection, object?> addFunc!!)
            where TCollection : IEnumerable
            => new SourceGenJsonTypeInfo<TCollection>(
                options,
                collectionInfo,
                () => new StackOrQueueConverter<TCollection>(),
                createObjectWithArgs: null,
                addFunc: addFunc);

        /// <summary>
        /// Creates serialization metadata for types assignable to <see cref="IList"/>.
        /// </summary>
        /// <typeparam name="TCollection">The generic definition of the type.</typeparam>
        /// <param name="options"></param>
        /// <param name="collectionInfo">Provides serialization metadata about the collection type.</param>
        /// <returns>Serialization metadata for the given type.</returns>
        /// <remarks>This API is for use by the output of the System.Text.Json source generator and should not be called directly.</remarks>
        public static JsonTypeInfo<TCollection> CreateIEnumerableInfo<TCollection>(
            JsonSerializerOptions options,
            JsonCollectionInfoValues<TCollection> collectionInfo)
            where TCollection : IEnumerable
            => new SourceGenJsonTypeInfo<TCollection>(
                options,
                collectionInfo,
                () => new IEnumerableConverter<TCollection>());
    }
}
