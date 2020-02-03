// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace System.Text.Json
{
    public static partial class JsonSerializer
    {
        // AggressiveInlining used although a large method it is only called from two locations and is on a hot path.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void HandleValue(JsonTokenType tokenType, JsonSerializerOptions options, ref Utf8JsonReader reader, ref ReadStack state)
        {
            if (state.Current.SkipProperty)
            {
                return;
            }

            if (state.Current.LastSeenMetadataProperty == MetadataPropertyName.Id || state.Current.LastSeenMetadataProperty == MetadataPropertyName.Ref)
            {
                Debug.Assert(options.ReferenceHandling.ShouldReadPreservedReferences());

                HandleMetadataPropertyValue(ref reader, ref state);
                return;
            }

            JsonPropertyInfo? jsonPropertyInfo = state.Current.JsonPropertyInfo;
            Debug.Assert(state.Current.JsonClassInfo != null);
            if (jsonPropertyInfo == null)
            {
                jsonPropertyInfo = state.Current.JsonClassInfo.CreateRootProperty(options);
            }
            else if (state.Current.JsonClassInfo.ClassType == ClassType.Unknown)
            {
                jsonPropertyInfo = state.Current.JsonClassInfo.GetOrAddPolymorphicProperty(jsonPropertyInfo, typeof(object), options);
            }

            jsonPropertyInfo.Read(tokenType, ref state, ref reader);
        }
    }
}
