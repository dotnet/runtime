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

        // Support Immutable dictionary types.
        public IDictionary TempDictionaryValues;

        // For performance, we order the properties by the first deserialize and PropertyIndex helps find the right slot quicker.
        public int PropertyIndex;
        public List<PropertyRef> PropertyRefCache;

        // The current JSON data for a property does not match a given POCO, so ignore the property (recursively).
        public bool Drain;

        public bool IsImmutableDictionary => JsonClassInfo.ClassType == ClassType.ImmutableDictionary;
        public bool IsDictionary => JsonClassInfo.ClassType == ClassType.Dictionary;

        public bool IsDictionaryProperty => JsonPropertyInfo != null &&
            !JsonPropertyInfo.IsPropertyPolicy &&
            JsonPropertyInfo.ClassType == ClassType.Dictionary;
        public bool IsImmutableDictionaryProperty => JsonPropertyInfo != null &&
            !JsonPropertyInfo.IsPropertyPolicy && (JsonPropertyInfo.ClassType == ClassType.ImmutableDictionary);

        public bool IsEnumerable => JsonClassInfo.ClassType == ClassType.Enumerable;

        public bool IsEnumerableProperty =>
            JsonPropertyInfo != null &&
            !JsonPropertyInfo.IsPropertyPolicy &&
            JsonPropertyInfo.ClassType == ClassType.Enumerable;

        public bool IsProcessingEnumerableOrDictionary => IsProcessingEnumerable || IsProcessingDictionary || IsProcessingImmutableDictionary;
        public bool IsProcessingDictionary => IsDictionary || IsDictionaryProperty;
        public bool IsProcessingImmutableDictionary => IsImmutableDictionary || IsImmutableDictionaryProperty;
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

                // We've got a property info. If we're a Value or polymorphic Value
                // (ClassType.Unknown), return true.
                ClassType type = JsonPropertyInfo.ClassType;
                return type == ClassType.Value || type == ClassType.Unknown ||
                    KeyName != null  && (
                    (IsDictionary && JsonClassInfo.ElementClassInfo.ClassType == ClassType.Unknown) ||
                    (IsDictionaryProperty && JsonPropertyInfo.ElementClassInfo.ClassType == ClassType.Unknown) ||
                    (IsImmutableDictionary && JsonClassInfo.ElementClassInfo.ClassType == ClassType.Unknown) ||
                    (IsImmutableDictionaryProperty && JsonPropertyInfo.ElementClassInfo.ClassType == ClassType.Unknown)
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
                JsonClassInfo.ClassType == ClassType.ImmutableDictionary)
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

            Type propType = state.Current.JsonPropertyInfo.RuntimePropertyType;
            if (typeof(IList).IsAssignableFrom(propType))
            {
                // If IList, add the members as we create them.
                JsonClassInfo collectionClassInfo = state.Current.JsonPropertyInfo.RuntimeClassInfo;
                IList collection = (IList)collectionClassInfo.CreateObject();
                return collection;
            }
            else
            {
                ThrowHelper.ThrowJsonException_DeserializeUnableToConvertValue(propType, reader, state.JsonPath);
                return null;
            }
        }

        public Type GetElementType()
        {
            if (IsEnumerableProperty || IsDictionaryProperty || IsImmutableDictionaryProperty)
            {
                return JsonPropertyInfo.ElementClassInfo.Type;
            }

            if (IsEnumerable || IsDictionary || IsImmutableDictionary)
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
