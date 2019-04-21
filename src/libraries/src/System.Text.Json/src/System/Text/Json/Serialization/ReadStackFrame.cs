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

        // Support Dictionary
        internal string KeyName;

        // Current property values
        internal JsonPropertyInfo JsonPropertyInfo;
        internal bool PopStackOnEndArray;
        internal bool EnumerableCreated;

        // Support System.Array and other types that don't implement IList
        internal IList TempEnumerableValues;

        // For performance, we order the properties by the first deserialize and PropertyIndex helps find the right slot quicker.
        internal int PropertyIndex;
        internal List<PropertyRef> PropertyRefCache;

        // The current JSON data for a property does not match a given POCO, so ignore the property (recursively for enumerables or object).
        internal bool Drain;

        internal void Initialize(Type type, JsonSerializerOptions options)
        {
            JsonClassInfo = options.GetOrAddClass(type);
            if (JsonClassInfo.ClassType == ClassType.Value || JsonClassInfo.ClassType == ClassType.Enumerable || JsonClassInfo.ClassType == ClassType.Dictionary)
            {
                JsonPropertyInfo = JsonClassInfo.GetPolicyProperty();
            }
        }

        internal void Reset()
        {
            ReturnValue = null;
            JsonClassInfo = null;
            PropertyRefCache = null;
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
            KeyName = null;
        }

        internal bool IsProcessingEnumerableOrDictionary()
        {
            return IsEnumerable() ||IsPropertyEnumerable() || IsDictionary() || IsPropertyADictionary();
        }

        internal bool IsEnumerable()
        {
            return JsonClassInfo.ClassType == ClassType.Enumerable;
        }

        internal bool IsDictionary()
        {
            return JsonClassInfo.ClassType == ClassType.Dictionary;
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

        internal bool IsPropertyADictionary()
        {
            if (JsonPropertyInfo != null)
            {
                return JsonPropertyInfo.ClassType == ClassType.Dictionary;
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

            if (IsDictionary())
            {
                return JsonClassInfo.ElementClassInfo.Type;
            }

            return JsonPropertyInfo.RuntimePropertyType;
        }

        internal static object CreateEnumerableValue(ref Utf8JsonReader reader, ref ReadStack state, JsonSerializerOptions options)
        {
            JsonPropertyInfo jsonPropertyInfo = state.Current.JsonPropertyInfo;

            // If the property has an EnumerableConverter, then we use tempEnumerableValues.
            if (jsonPropertyInfo.EnumerableConverter != null)
            {
                IList converterList;
                if (jsonPropertyInfo.ElementClassInfo.ClassType == ClassType.Value)
                {
                    converterList = jsonPropertyInfo.ElementClassInfo.GetPolicyProperty().CreateConverterList();
                }
                else
                {
                    converterList =  new List<object>();
                }

                state.Current.TempEnumerableValues = converterList;

                return null;
            }

            Type propType = state.Current.JsonPropertyInfo.RuntimePropertyType;
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
    }
}
