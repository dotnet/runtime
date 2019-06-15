// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization.Converters;

namespace System.Text.Json
{
    [DebuggerDisplay("ClassType.{JsonClassInfo.ClassType}, {JsonClassInfo.Type.Name}")]
    internal struct ReadStackFrame
    {
        // The object (POCO or IEnumerable) that is being populated
        public object ReturnValue;
        public JsonClassInfo JsonClassInfo;

        // Support Dictionary keys.
        public string KeyName;

        // Support JSON Path on exceptions.
        public byte[] JsonPropertyName;

        // Current property values.
        public JsonPropertyInfo JsonPropertyInfo;

        // Support System.Array and other types that don't implement IList.
        public IList TempEnumerableValues;

        // Has an array or dictionary property been initialized.
        public bool PropertyInitialized;

        // Support IDictionary constructible types, i.e. types that we
        // support by passing and IDictionary to their constructors:
        // immutable dictionaries, Hashtable, SortedList
        public IDictionary TempDictionaryValues;

        // For performance, we order the properties by the first deserialize and PropertyIndex helps find the right slot quicker.
        public int PropertyIndex;
        public List<PropertyRef> PropertyRefCache;

        // The current JSON data for a property does not match a given POCO, so ignore the property (recursively).
        public bool Drain;

        public bool IsIDictionaryConstructible => JsonClassInfo.ClassType == ClassType.IDictionaryConstructible;
        public bool IsDictionary => JsonClassInfo.ClassType == ClassType.Dictionary;
        public bool IsKeyValuePair => JsonClassInfo.ClassType == ClassType.KeyValuePair;

        public bool IsDictionaryProperty => JsonPropertyInfo != null &&
            !JsonPropertyInfo.IsPropertyPolicy &&
            JsonPropertyInfo.ClassType == ClassType.Dictionary;
        public bool IsIDictionaryConstructibleProperty => JsonPropertyInfo != null &&
            !JsonPropertyInfo.IsPropertyPolicy && (JsonPropertyInfo.ClassType == ClassType.IDictionaryConstructible);
        public bool IsKeyValuePairProperty => JsonPropertyInfo != null &&
            !JsonPropertyInfo.IsPropertyPolicy && (JsonPropertyInfo.ClassType == ClassType.KeyValuePair);

        public bool IsEnumerable => JsonClassInfo.ClassType == ClassType.Enumerable;

        public bool IsEnumerableProperty =>
            JsonPropertyInfo != null &&
            !JsonPropertyInfo.IsPropertyPolicy &&
            JsonPropertyInfo.ClassType == ClassType.Enumerable;

        public bool IsProcessingEnumerableOrDictionary => IsProcessingEnumerable || IsProcessingDictionary || IsProcessingIDictionaryConstructibleOrKeyValuePair;
        public bool IsProcessingIDictionaryConstructibleOrKeyValuePair => IsProcessingIDictionaryConstructible || IsProcessingKeyValuePair;

        public bool IsProcessingDictionary => IsDictionary || IsDictionaryProperty;
        public bool IsProcessingIDictionaryConstructible => IsIDictionaryConstructible || IsIDictionaryConstructibleProperty;
        public bool IsProcessingKeyValuePair => IsKeyValuePair || IsKeyValuePairProperty;
        public bool IsProcessingEnumerable => IsEnumerable || IsEnumerableProperty;

        public bool IsProcessingValue
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                if (JsonPropertyInfo == null || SkipProperty)
                {
                    return false;
                }

                if (PropertyInitialized)
                {
                    if ((IsEnumerable || IsDictionary || IsIDictionaryConstructible) &&
                        JsonClassInfo.ElementClassInfo.ClassType == ClassType.Unknown)
                    {
                        return true;
                    }
                    else if ((IsEnumerableProperty || IsDictionaryProperty || IsIDictionaryConstructibleProperty) &&
                        JsonPropertyInfo.ElementClassInfo.ClassType == ClassType.Unknown)
                    {
                        return true;
                    }
                }

                // We've got a property info. If we're a Value or polymorphic Value
                // (ClassType.Unknown), return true.
                ClassType type = JsonPropertyInfo.ClassType;
                return type == ClassType.Value || type == ClassType.Unknown ||
                    KeyName != null  && (
                    (IsDictionary && JsonClassInfo.ElementClassInfo.ClassType == ClassType.Unknown) ||
                    (IsDictionaryProperty && JsonPropertyInfo.ElementClassInfo.ClassType == ClassType.Unknown) ||
                    (IsIDictionaryConstructible && JsonClassInfo.ElementClassInfo.ClassType == ClassType.Unknown) ||
                    (IsIDictionaryConstructibleProperty && JsonPropertyInfo.ElementClassInfo.ClassType == ClassType.Unknown)
                    );
            }
        }

        public void Initialize(Type type, JsonSerializerOptions options)
        {
            JsonClassInfo = options.GetOrAddClass(type);
            InitializeJsonPropertyInfo();
        }

        public void InitializeJsonPropertyInfo()
        {
            if (JsonClassInfo.ClassType == ClassType.Value ||
                JsonClassInfo.ClassType == ClassType.Enumerable ||
                JsonClassInfo.ClassType == ClassType.Dictionary ||
                JsonClassInfo.ClassType == ClassType.IDictionaryConstructible)
            {
                JsonPropertyInfo = JsonClassInfo.GetPolicyProperty();
            }
            else if (JsonClassInfo.ClassType == ClassType.KeyValuePair)
            {
                JsonPropertyInfo = JsonClassInfo.GetPolicyPropertyOfKeyValuePair();
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
            PropertyInitialized = false;
            JsonPropertyInfo = null;
            TempEnumerableValues = null;
            TempDictionaryValues = null;
            JsonPropertyName = null;
            KeyName = null;
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
                    converterList = new List<object>();
                }

                state.Current.TempEnumerableValues = converterList;

                return null;
            }

            Type propertyType = state.Current.JsonPropertyInfo.RuntimePropertyType;
            if (typeof(IList).IsAssignableFrom(propertyType))
            {
                // If IList, add the members as we create them.
                JsonClassInfo collectionClassInfo = state.Current.JsonPropertyInfo.RuntimeClassInfo;
                IList collection = (IList)collectionClassInfo.CreateObject();
                return collection;
            }
            else
            {
                ThrowHelper.ThrowJsonException_DeserializeUnableToConvertValue(propertyType, reader, state.JsonPath);
                return null;
            }
        }

        public Type GetElementType()
        {
            if (IsEnumerableProperty || IsDictionaryProperty || IsIDictionaryConstructibleProperty)
            {
                return JsonPropertyInfo.ElementClassInfo.Type;
            }

            if (IsEnumerable || IsDictionary || IsIDictionaryConstructible)
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

        public bool SkipProperty => Drain ||
            ReferenceEquals(JsonPropertyInfo, JsonPropertyInfo.s_missingProperty) ||
            (JsonPropertyInfo?.IsPropertyPolicy == false && JsonPropertyInfo?.ShouldDeserialize == false);
    }
}
