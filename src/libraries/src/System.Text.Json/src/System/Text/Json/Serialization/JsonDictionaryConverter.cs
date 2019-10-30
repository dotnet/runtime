// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;

namespace System.Text.Json.Serialization.Converters
{
    // Helper to deserialize data into collections that store key-value pairs (not including KeyValuePair<,>)
    // e.g. IDictionary, Hashtable, Dictionary<,> IDictionary<,>, SortedList etc.
    // We'll call these collections "dictionaries".
    // Note: the KeyValuePair<,> type has a value converter, so its deserialization flow will not reach here.
    // Also, KeyValuePair<,> is sealed, so deserialization will flow here to support custom types that
    // implement KeyValuePair<,>.
    internal abstract class JsonDictionaryConverter
    {
        // Return type is object, not IDictionary as not all "dictionaries" implement IDictionary e.g. IDictionary<TKey, TValue>.
        public abstract object CreateFromDictionary(ref ReadStack state, IDictionary sourceDictionary, JsonSerializerOptions options);
    }
}
