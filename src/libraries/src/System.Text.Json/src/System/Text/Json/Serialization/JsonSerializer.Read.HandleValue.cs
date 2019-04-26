// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Text.Json.Serialization
{
    public static partial class JsonSerializer
    {
        private static bool HandleValue(JsonTokenType tokenType, JsonSerializerOptions options, ref Utf8JsonReader reader, ref ReadStack state)
        {
            if (state.Current.Skip())
            {
                return false;
            }

            JsonPropertyInfo jsonPropertyInfo = state.Current.JsonPropertyInfo;
            if (jsonPropertyInfo == null || state.Current.JsonClassInfo.ClassType == ClassType.Unknown)
            {
                jsonPropertyInfo = state.Current.JsonClassInfo.CreatePolymorphicProperty(jsonPropertyInfo, typeof(object), options);
            }

            bool lastCall = (!state.Current.IsProcessingEnumerableOrDictionary && state.Current.ReturnValue == null);

            jsonPropertyInfo.Read(tokenType, options, ref state, ref reader);
            return lastCall;
        }
    }
}
