// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;

namespace System.Text.Json.Serialization.Policies
{
    internal abstract class JsonDictionaryConverter
    {
        public abstract IDictionary CreateFromDictionary(ref ReadStack state, IDictionary sourceDictionary, JsonSerializerOptions options);
    }
}
