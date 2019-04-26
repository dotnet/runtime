// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Collections.Generic;
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
            JsonPropertyInfo jsonPropertyInfo;

            jsonPropertyInfo = state.Current.JsonPropertyInfo;

            bool skip = jsonPropertyInfo != null && !jsonPropertyInfo.ShouldDeserialize;
            if (skip || state.Current.Skip())
            {
                // The array is not being applied to the object.
                state.Push();
                state.Current.Drain = true;
                return;
            }

            if (jsonPropertyInfo == null || state.Current.JsonClassInfo.ClassType == ClassType.Unknown)
            {
                jsonPropertyInfo = state.Current.JsonClassInfo.CreatePolymorphicProperty(jsonPropertyInfo, typeof(object), options);
            }

            Type arrayType = jsonPropertyInfo.RuntimePropertyType;
            if (!typeof(IEnumerable).IsAssignableFrom(arrayType) || (arrayType.IsArray && arrayType.GetArrayRank() > 1))
            {
                ThrowHelper.ThrowJsonReaderException_DeserializeUnableToConvertValue(arrayType, reader, state);
            }

            Debug.Assert(state.Current.IsPropertyEnumerable || state.Current.IsDictionary);

            if (state.Current.EnumerableCreated)
            {
                // A nested json array so push a new stack frame.
                Type elementType = state.Current.JsonClassInfo.ElementClassInfo.GetPolicyProperty().RuntimePropertyType;

                state.Push();

                state.Current.Initialize(elementType, options);
                state.Current.PopStackOnEnd = true;
            }
            else
            {
                state.Current.EnumerableCreated = true;
            }

            jsonPropertyInfo = state.Current.JsonPropertyInfo;

            // If current property is already set (from a constructor, for example) leave as-is.
            if (jsonPropertyInfo.GetValueAsObject(state.Current.ReturnValue) == null)
            {
                // Create the enumerable.
                object value = ReadStackFrame.CreateEnumerableValue(ref reader, ref state, options);
                if (value != null)
                {
                    if (state.Current.ReturnValue != null)
                    {
                        state.Current.JsonPropertyInfo.SetValueAsObject(state.Current.ReturnValue, value);
                    }
                    else
                    {
                        // Primitive arrays being returned without object
                        state.Current.SetReturnValue(value);
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

            bool valueReturning = state.Current.PopStackOnEnd;
            if (state.Current.PopStackOnEnd)
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
                else if (state.Current.IsEnumerable || state.Current.IsDictionary)
                {
                    // Returning a non-converted list.
                    return true;
                }
                // else there must be an outer object, so we'll return false here.
            }

            ApplyObjectToEnumerable(value, options, ref state.Current, setPropertyDirectly: setPropertyDirectly);

            if (!valueReturning)
            {
                state.Current.ResetProperty();
            }

            return false;
        }

        // If this method is changed, also change ApplyValueToEnumerable.
        internal static void ApplyObjectToEnumerable(object value, JsonSerializerOptions options, ref ReadStackFrame frame, bool setPropertyDirectly = false)
        {
            if (frame.IsEnumerable)
            {
                if (frame.TempEnumerableValues != null)
                {
                    frame.TempEnumerableValues.Add(value);
                }
                else
                {
                    ((IList)frame.ReturnValue).Add(value);
                }
            }
            else if (!setPropertyDirectly && frame.IsPropertyEnumerable)
            {
                Debug.Assert(frame.JsonPropertyInfo != null);
                Debug.Assert(frame.ReturnValue != null);
                if (frame.TempEnumerableValues != null)
                {
                    frame.TempEnumerableValues.Add(value);
                }
                else
                {
                    ((IList)frame.JsonPropertyInfo.GetValueAsObject(frame.ReturnValue)).Add(value);
                }
            }
            else if (frame.IsDictionary)
            {
                Debug.Assert(frame.ReturnValue != null);
                ((IDictionary)frame.JsonPropertyInfo.GetValueAsObject(frame.ReturnValue)).Add(frame.KeyName, value);
            }
            else
            {
                Debug.Assert(frame.JsonPropertyInfo != null);
                frame.JsonPropertyInfo.SetValueAsObject(frame.ReturnValue, value);
            }
        }

        // If this method is changed, also change ApplyObjectToEnumerable.
        internal static void ApplyValueToEnumerable<TProperty>(
            ref TProperty value,
            JsonSerializerOptions options,
            ref ReadStackFrame frame)
        {
            if (frame.IsEnumerable)
            {
                if (frame.TempEnumerableValues != null)
                {
                    ((IList<TProperty>)frame.TempEnumerableValues).Add(value);
                }
                else
                {
                    ((IList<TProperty>)frame.ReturnValue).Add(value);
                }
            }
            else if (frame.IsPropertyEnumerable)
            {
                Debug.Assert(frame.JsonPropertyInfo != null);
                Debug.Assert(frame.ReturnValue != null);
                if (frame.TempEnumerableValues != null)
                {
                    ((IList<TProperty>)frame.TempEnumerableValues).Add(value);
                }
                else
                {
                    ((IList<TProperty>)frame.JsonPropertyInfo.GetValueAsObject(frame.ReturnValue)).Add(value);
                }
            }
            else if (frame.IsDictionary)
            {
                Debug.Assert(frame.ReturnValue != null);
                ((IDictionary<string, TProperty>)frame.JsonPropertyInfo.GetValueAsObject(frame.ReturnValue)).Add(frame.KeyName, value);
            }
            else
            {
                Debug.Assert(frame.JsonPropertyInfo != null);
                frame.JsonPropertyInfo.SetValueAsObject(frame.ReturnValue, value);
            }
        }
    }
}
