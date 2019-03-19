// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

namespace System.Text.Json.Serialization
{
    internal struct ReadStackFrame
    {
        // The object (POCO or IEnumerable) that is being populated
        internal object ReturnValue;
        internal JsonClassInfo JsonClassInfo;

        // Current property values
        internal JsonPropertyInfo JsonPropertyInfo;
        internal bool PopStackOnEndArray;
        internal bool EnumerableCreated;

        // Support System.Array and other types that don't implement IList
        internal List<object> TempEnumerableValues;

        // For performance, we order the properties by the first usage and this index helps find the right slot quicker.
        internal int PropertyIndex;
        internal bool Drain;

        internal void Reset()
        {
            ReturnValue = null;
            JsonClassInfo = null;
            PropertyIndex = 0;
            Drain = false;
            ResetProperty();
        }

        internal void ResetProperty()
        {
            JsonPropertyInfo = null;
            PopStackOnEndArray = false;
            EnumerableCreated = false;
            TempEnumerableValues = null;
        }

        internal bool IsEnumerable()
        {
            return JsonClassInfo.ClassType == ClassType.Enumerable;
        }

        internal bool Skip()
        {
            return Drain || ReferenceEquals(JsonPropertyInfo, JsonSerializer.s_missingProperty);
        }

        internal bool IsPropertyEnumerable()
        {
            if (JsonPropertyInfo != null)
            {
                return JsonPropertyInfo.ClassType == ClassType.Enumerable;
            }

            return false;
        }

        public Type GetElementType()
        {
            if (IsPropertyEnumerable())
            {
                return JsonPropertyInfo.ElementClassInfo.Type;
            }

            if (IsEnumerable())
            {
                return JsonClassInfo.ElementClassInfo.Type;
            }

            return JsonPropertyInfo.PropertyType;
        }

        internal static object CreateEnumerableValue(ref Utf8JsonReader reader, ref ReadStack state, JsonSerializerOptions options)
        {
            // If the property has an EnumerableConverter, then we use tempEnumerableValues.
            if (state.Current.JsonPropertyInfo.EnumerableConverter != null)
            {
                state.Current.TempEnumerableValues = new List<object>();
                return null;
            }

            Type propType = state.Current.JsonPropertyInfo.PropertyType;
            if (typeof(IList).IsAssignableFrom(propType))
            {
                // If IList, add the members as we create them.
                JsonClassInfo collectionClassInfo = options.GetOrAddClass(propType);
                IList collection = (IList)collectionClassInfo.CreateObject();
                return collection;
            }
            else
            {
                ThrowHelper.ThrowJsonReaderException_DeserializeUnableToConvertValue(propType, reader, state);
                return null;
            }
        }

        internal static IEnumerable GetEnumerableValue(in ReadStackFrame current)
        {
            if (current.IsEnumerable())
            {
                if (current.ReturnValue != null)
                {
                    return (IEnumerable)current.ReturnValue;
                }
            }

            // IEnumerable properties are finished (values added inline) unless they are using tempEnumerableValues.
            return current.TempEnumerableValues;
        }

        internal void SetReturnValue(object value, JsonSerializerOptions options)
        {
            Debug.Assert(ReturnValue == null);
            ReturnValue = value;
        }

        internal static void SetReturnValue(object value, JsonSerializerOptions options, ref ReadStackFrame current, bool setPropertyDirectly = false)
        {
            if (current.IsEnumerable())
            {
                if (current.TempEnumerableValues != null)
                {
                    current.TempEnumerableValues.Add(value);
                }
                else
                {
                    ((IList)current.ReturnValue).Add(value);
                }
            }
            else if (!setPropertyDirectly && current.IsPropertyEnumerable())
            {
                Debug.Assert(current.JsonPropertyInfo != null);
                Debug.Assert(current.ReturnValue != null);
                if (current.TempEnumerableValues != null)
                {
                    current.TempEnumerableValues.Add(value);
                }
                else
                {
                    ((IList)current.JsonPropertyInfo.GetValueAsObject(current.ReturnValue, options)).Add(value);
                }
            }
            else
            {
                Debug.Assert(current.JsonPropertyInfo != null);
                current.JsonPropertyInfo.SetValueAsObject(current.ReturnValue, value, options);
            }
        }
    }
}
