// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Converters;
using System.Threading.Tasks;

namespace System.Text.Json.Serialization.Metadata
{
    /// <summary>
    /// todo
    /// </summary>
    /// <typeparam name="TKey"></typeparam>
    /// <typeparam name="TValue"></typeparam>
    public static class KnownDictionaryTypeInfos<TKey, TValue> where TKey : notnull
    {
        private static JsonTypeInfo<Dictionary<TKey, TValue>>? s_dictionaryOfTKeyTValue;
        /// <summary>
        /// todo
        /// </summary>
        public static JsonTypeInfo<Dictionary<TKey, TValue>> GetDictionary(JsonClassInfo elementInfo, JsonSerializerContext context)
        {
            if (s_dictionaryOfTKeyTValue == null)
            {
                s_dictionaryOfTKeyTValue = new JsonCollectionTypeInfo<Dictionary<TKey, TValue>>(CreateDictionary, new DictionaryOfTKeyTValueConverter<Dictionary<TKey, TValue>, TKey, TValue>(), elementInfo, context._options);
            }

            return s_dictionaryOfTKeyTValue;
        }

        private static Dictionary<TKey, TValue> CreateDictionary()
        {
            return new Dictionary<TKey, TValue>();
        }
    }
}
