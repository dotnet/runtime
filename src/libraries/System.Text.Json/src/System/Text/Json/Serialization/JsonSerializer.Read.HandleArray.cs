// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Diagnostics;
using System.Text.Json.Serialization.Policies;

namespace System.Text.Json.Serialization
{
    public static partial class JsonSerializer
    {
        private static void HandleStartArray(
            JsonSerializerOptions options,
            ref Utf8JsonReader reader,
            ref ReadStack state)
        {
            if (state.Current.Skip())
            {
                // The array is not being applied to the object.
                state.Push();
                state.Current.Drain = true;
                return;
            }

            Type arrayType = state.Current.JsonPropertyInfo.PropertyType;
            if (!typeof(IEnumerable).IsAssignableFrom(arrayType) || (arrayType.IsArray && arrayType.GetArrayRank() > 1))
            {
                ThrowHelper.ThrowJsonReaderException_DeserializeUnableToConvertValue(arrayType, reader, state);
            }

            Debug.Assert(state.Current.IsPropertyEnumerable());
            if (state.Current.IsPropertyEnumerable())
            {
                if (state.Current.EnumerableCreated)
                {
                    // A nested json array so push a new stack frame.
                    Type elementType = state.Current.JsonClassInfo.ElementClassInfo.GetPolicyProperty().PropertyType;

                    state.Push();

                    state.Current.JsonClassInfo = options.GetOrAddClass(elementType);
                    state.Current.JsonPropertyInfo = state.Current.JsonClassInfo.GetPolicyProperty();
                    state.Current.PopStackOnEndArray = true;
                }
                else
                {
                    state.Current.EnumerableCreated = true;
                }

                // If current property is already set (from a constructor, for example) leave as-is
                if (state.Current.JsonPropertyInfo.GetValueAsObject(state.Current.ReturnValue, options) == null)
                {
                    // Create the enumerable.
                    object value = ReadStackFrame.CreateEnumerableValue(ref reader, ref state, options);
                    if (value != null)
                    {
                        if (state.Current.ReturnValue != null)
                        {
                            state.Current.JsonPropertyInfo.SetValueAsObject(state.Current.ReturnValue, value, options);
                        }
                        else
                        {
                            // Primitive arrays being returned without object
                            state.Current.SetReturnValue(value, options);
                        }
                    }
                }
            }
        }

        private static bool HandleEndArray(
            JsonSerializerOptions options,
            ref ReadStack state)
        {
            bool lastFrame = state.IsLastFrame;

            if (state.Current.Drain)
            {
                // The array is not being applied to the object.
                state.Pop();
                return lastFrame;
            }

            IEnumerable value = ReadStackFrame.GetEnumerableValue(state.Current);
            if (value == null)
            {
                // We added the items to the list property already.
                state.Current.ResetProperty();
                return false;
            }

            bool setPropertyDirectly;
            if (state.Current.TempEnumerableValues != null)
            {
                JsonEnumerableConverter converter = state.Current.JsonPropertyInfo.EnumerableConverter;
                Debug.Assert(converter != null);

                Type elementType = state.Current.GetElementType();
                value = converter.CreateFromList(elementType, (IList)value);
                setPropertyDirectly = true;
            }
            else
            {
                setPropertyDirectly = false;
            }

            bool valueReturning = state.Current.PopStackOnEndArray;
            if (state.Current.PopStackOnEndArray)
            {
                state.Pop();
            }

            if (lastFrame)
            {
                if (state.Current.ReturnValue == null)
                {
                    // Returning a converted list or object.
                    state.Current.Reset();
                    state.Current.ReturnValue = value;
                    return true;
                }
                else if (state.Current.IsEnumerable())
                {
                    // Returning a non-converted list.
                    return true;
                }
                // else there must be an outer object, so we'll return false here.
            }

            ReadStackFrame.SetReturnValue(value, options, ref state.Current, setPropertyDirectly: setPropertyDirectly);

            if (!valueReturning)
            {
                state.Current.ResetProperty();
            }

            return false;
        }
    }
}
