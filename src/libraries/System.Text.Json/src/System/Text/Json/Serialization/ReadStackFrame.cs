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
        public object ReturnValue;
        public JsonClassInfo JsonClassInfo;

        // Support Dictionary keys.
        public string KeyName;

        // Current property values.
        public JsonPropertyInfo JsonPropertyInfo;

        // Pop the stack when the current array or dictionary is done.
        public bool PopStackOnEnd;

        // Support System.Array and other types that don't implement IList.
        public IList TempEnumerableValues;
        public bool EnumerableCreated;

        // For performance, we order the properties by the first deserialize and PropertyIndex helps find the right slot quicker.
        public int PropertyIndex;
        public List<PropertyRef> PropertyRefCache;

        // The current JSON data for a property does not match a given POCO, so ignore the property (recursively).
        public bool Drain;

        public bool IsDictionary => JsonClassInfo.ClassType == ClassType.Dictionary;
        public bool IsEnumerable => JsonClassInfo.ClassType == ClassType.Enumerable;
        public bool IsProcessingEnumerableOrDictionary => IsProcessingEnumerable || IsDictionary;
        public bool IsProcessingEnumerable => IsEnumerable || IsPropertyEnumerable;
        public bool IsPropertyEnumerable => JsonPropertyInfo != null ? JsonPropertyInfo.ClassType == ClassType.Enumerable : false;

        public void Initialize(Type type, JsonSerializerOptions options)
        {
            JsonClassInfo = options.GetOrAddClass(type);
            InitializeJsonPropertyInfo();
        }

        public void InitializeJsonPropertyInfo()
        {
            if (JsonClassInfo.ClassType == ClassType.Value || JsonClassInfo.ClassType == ClassType.Enumerable || JsonClassInfo.ClassType == ClassType.Dictionary)
            {
                JsonPropertyInfo = JsonClassInfo.GetPolicyProperty();
            }
        }

        public void Reset()
        {
            Drain = false;
            JsonClassInfo = null;
            KeyName = null;
            PropertyRefCache = null;
            ReturnValue = null;
            EndObject();
        }

        public void ResetProperty()
        {
            EnumerableCreated = false;
            JsonPropertyInfo = null;
            PopStackOnEnd = false;
            TempEnumerableValues = null;
        }

        public void EndObject()
        {
            PropertyIndex = 0;
            ResetProperty();
        }

        public static object CreateEnumerableValue(ref Utf8JsonReader reader, ref ReadStack state, JsonSerializerOptions options)
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

        public Type GetElementType()
        {
            if (IsPropertyEnumerable)
            {
                return JsonPropertyInfo.ElementClassInfo.Type;
            }

            if (IsEnumerable)
            {
                return JsonClassInfo.ElementClassInfo.Type;
            }

            if (IsDictionary)
            {
                return JsonClassInfo.ElementClassInfo.Type;
            }

            return JsonPropertyInfo.RuntimePropertyType;
        }

        public static IEnumerable GetEnumerableValue(in ReadStackFrame current)
        {
            if (current.IsEnumerable)
            {
                if (current.ReturnValue != null)
                {
                    return (IEnumerable)current.ReturnValue;
                }
            }

            // IEnumerable properties are finished (values added inline) unless they are using tempEnumerableValues.
            return current.TempEnumerableValues;
        }

        public void SetReturnValue(object value)
        {
            Debug.Assert(ReturnValue == null);
            ReturnValue = value;
        }

        public bool Skip()
        {
            return Drain || ReferenceEquals(JsonPropertyInfo, JsonSerializer.s_missingProperty);
        }
    }
}
