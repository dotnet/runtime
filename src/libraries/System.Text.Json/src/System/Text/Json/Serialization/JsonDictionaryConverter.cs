// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Text.Json.Serialization
{
    /// <summary>
    /// Base class for dictionary converters such as IDictionary, Hashtable, Dictionary{,} IDictionary{,} and SortedList.
    /// </summary>
    internal abstract class JsonDictionaryConverter<T> : JsonResumableConverter<T>
    {
        internal override ClassType ClassType => ClassType.Dictionary;
        protected internal abstract bool OnWriteResume(Utf8JsonWriter writer, T dictionary, JsonSerializerOptions options, ref WriteStack state);
    }
}
