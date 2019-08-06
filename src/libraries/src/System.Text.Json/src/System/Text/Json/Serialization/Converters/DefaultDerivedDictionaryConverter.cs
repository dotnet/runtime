// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;

namespace System.Text.Json.Serialization.Converters
{
    internal sealed class DefaultDerivedDictionaryConverter : JsonDictionaryConverter
    {
        public override object CreateFromDictionary(ref ReadStack state, IDictionary sourceDictionary, JsonSerializerOptions options)
        {
            JsonPropertyInfo collectionPropertyInfo = state.Current.JsonPropertyInfo;
            JsonPropertyInfo elementPropertyInfo = options.GetJsonPropertyInfoFromClassInfo(collectionPropertyInfo.ElementType, options);
            return elementPropertyInfo.CreateDerivedDictionaryInstance(collectionPropertyInfo, sourceDictionary, state.JsonPath, options);
        }
    }
}
