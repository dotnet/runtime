// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;

namespace System.Text.Json.Serialization
{
    public static partial class JsonSerializer
    {
        private static void HandleStartObject(JsonSerializerOptions options, ref Utf8JsonReader reader, ref ReadStack state)
        {
            Debug.Assert(!state.Current.IsProcessingDictionary);

            if (state.Current.IsProcessingEnumerable)
            {
                // A nested object within an enumerable.
                Type objType = state.Current.GetElementType();
                state.Push();
                state.Current.Initialize(objType, options);
            }
            else if (state.Current.JsonPropertyInfo != null)
            {
                // Nested object.
                Type objType = state.Current.JsonPropertyInfo.RuntimePropertyType;
                state.Push();
                state.Current.Initialize(objType, options);
            }

            JsonClassInfo classInfo = state.Current.JsonClassInfo;

            if (classInfo.CreateObject is null && classInfo.ClassType == ClassType.Object)
            {
                if (classInfo.Type.IsInterface)
                {
                    ThrowHelper.ThrowInvalidOperationException_DeserializePolymorphicInterface(classInfo.Type);
                }
                else
                {
                    ThrowHelper.ThrowInvalidOperationException_DeserializeMissingParameterlessConstructor(classInfo.Type);
                }
            }

            state.Current.ReturnValue = classInfo.CreateObject();
        }

        private static void HandleEndObject(JsonSerializerOptions options, ref Utf8JsonReader reader, ref ReadStack state)
        {
            Debug.Assert(!state.Current.IsProcessingDictionary);

            state.Current.JsonClassInfo.UpdateSortedPropertyCache(ref state.Current);

            object value = state.Current.ReturnValue;

            if (state.IsLastFrame)
            {
                state.Current.Reset();
                state.Current.ReturnValue = value;
            }
            else
            {
                state.Pop();
                ApplyObjectToEnumerable(value, ref state, ref reader);
            }
        }
    }
}
