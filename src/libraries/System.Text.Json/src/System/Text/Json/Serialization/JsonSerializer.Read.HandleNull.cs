// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;

namespace System.Text.Json
{
    public static partial class JsonSerializer
    {
        private static bool HandleNull(JsonSerializerOptions options, ref Utf8JsonReader reader, ref ReadStack state)
        {
            if (state.Current.SkipProperty)
            {
                // Clear the current property in case it is a dictionary, since dictionaries must have EndProperty() called when completed.
                // A non-dictionary property can also have EndProperty() called when completed, although it is redundant.
                state.Current.EndProperty();

                return false;
            }

            JsonPropertyInfo jsonPropertyInfo = state.Current.JsonPropertyInfo;

            if (jsonPropertyInfo == null || (reader.CurrentDepth == 0 && jsonPropertyInfo.CanBeNull))
            {
                Debug.Assert(state.IsLastFrame);
                Debug.Assert(state.Current.ReturnValue == null);
                return true;
            }

            Debug.Assert(jsonPropertyInfo != null);

            if (state.Current.IsProcessingCollectionObject())
            {
                AddNullToCollection(jsonPropertyInfo, ref reader, ref state);
                return false;
            }

            if (state.Current.IsProcessingCollectionProperty())
            {
                if (state.Current.CollectionPropertyInitialized)
                {
                    // Add the element.
                    AddNullToCollection(jsonPropertyInfo, ref reader, ref state);
                }
                else
                {
                    // Set the property to null.
                    ApplyObjectToEnumerable(null, ref state, setPropertyDirectly: true);

                    // Reset so that `Is*Property` no longer returns true
                    state.Current.EndProperty();
                }

                return false;
            }

            if (!jsonPropertyInfo.CanBeNull)
            {
                // Allow a value type converter to return a null value representation, such as JsonElement.
                // Most likely this will throw JsonException.
                jsonPropertyInfo.Read(JsonTokenType.Null, ref state, ref reader);
                return false;
            }

            if (state.Current.ReturnValue == null)
            {
                Debug.Assert(state.IsLastFrame);
                return true;
            }

            if (!jsonPropertyInfo.IgnoreNullValues)
            {
                state.Current.JsonPropertyInfo.SetValueAsObject(state.Current.ReturnValue, value: null);
            }

            return false;
        }

        private static void AddNullToCollection(JsonPropertyInfo jsonPropertyInfo, ref Utf8JsonReader reader, ref ReadStack state)
        {
            JsonPropertyInfo elementPropertyInfo = jsonPropertyInfo.ElementClassInfo.PolicyProperty;

            // if elementPropertyInfo == null then this element doesn't need a converter (an object).
            if (elementPropertyInfo?.CanBeNull == false)
            {
                // Allow a value type converter to return a null value representation.
                // Most likely this will throw JsonException unless the converter has special logic (like converter for JsonElement).
                elementPropertyInfo.ReadEnumerable(JsonTokenType.Null, ref state, ref reader);
            }
            else
            {
                // Assume collection types are reference types and can have null assigned.
                ApplyObjectToEnumerable(null, ref state);
            }
        }
    }
}
