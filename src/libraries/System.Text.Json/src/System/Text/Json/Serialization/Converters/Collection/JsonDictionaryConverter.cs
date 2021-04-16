// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Text.Json.Serialization
{
    /// <summary>
    /// Base class for dictionary converters such as IDictionary, Hashtable, Dictionary{,} IDictionary{,} and SortedList.
    /// </summary>
    internal abstract class JsonDictionaryConverter<T> : JsonResumableConverter<T>
    {
        internal sealed override ConverterStrategy ConverterStrategy => ConverterStrategy.Dictionary;

        protected internal abstract bool OnWriteResume(Utf8JsonWriter writer, T dictionary, JsonSerializerOptions options, ref WriteStack state);
    }
}
