// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
        /// <returns></returns>
        public static JsonTypeInfo<TElement[]> CreateArrayInfo<TElement>(
            JsonSerializerOptions options,
            JsonTypeInfo elementInfo,
            JsonNumberHandling numberHandling)
            => new JsonTypeInfoInternal<TElement[]>(
                options,
                createObjectFunc: null,
                new ArrayConverter<TElement[], TElement>(),
                elementInfo,
                numberHandling);

        /// <summary>
        /// Creates metadata for types assignable to <see cref="List{T}"/>.
        /// </summary>
        /// <typeparam name="TCollection">The generic definition of the type.</typeparam>
        /// <typeparam name="TElement">The generic definition of the element type.</typeparam>
        /// <param name="options"></param>
        /// <param name="createObjectFunc">A <see cref="Func{TResult}"/> to create an instance of the list when deserializing.</param>
        /// <param name="elementInfo">A <see cref="JsonTypeInfo"/> instance representing the element type.</param>
        /// <param name="numberHandling">The <see cref="JsonNumberHandling"/> option to apply to number collection elements.</param>
        /// <returns></returns>
        public static JsonTypeInfo<TCollection> CreateListInfo<TCollection, TElement>(
            JsonSerializerOptions options,
            Func<TCollection>? createObjectFunc,
            JsonTypeInfo elementInfo,
            JsonNumberHandling numberHandling)
            where TCollection : List<TElement>
            => new JsonTypeInfoInternal<TCollection>(
                options,
                createObjectFunc,
                new ListOfTConverter<TCollection, TElement>(),
                elementInfo,
                numberHandling);

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
        /// <returns></returns>
        public static JsonTypeInfo<TCollection> CreateDictionaryInfo<TCollection, TKey, TValue>(
            JsonSerializerOptions options,
            Func<TCollection> createObjectFunc,
            JsonTypeInfo keyInfo,
            JsonTypeInfo valueInfo,
            JsonNumberHandling numberHandling)
            where TCollection : Dictionary<TKey, TValue>
            where TKey : notnull
            => new JsonTypeInfoInternal<TCollection>(
                options,
                createObjectFunc,
                new DictionaryOfTKeyTValueConverter<TCollection, TKey, TValue>(),
                keyInfo,
                valueInfo,
                numberHandling);
    }
}
