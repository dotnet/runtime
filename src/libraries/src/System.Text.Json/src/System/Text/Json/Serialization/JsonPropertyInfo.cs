// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers;
using System.Collections;
using System.Diagnostics;
using System.Reflection;
using System.Text.Json.Serialization.Converters;
using System.Text.Json.Serialization.Policies;

namespace System.Text.Json.Serialization
{
    internal abstract class JsonPropertyInfo
    {
        // Cache the converters so they don't get created for every enumerable property.
        private static readonly JsonEnumerableConverter s_jsonArrayConverter = new DefaultArrayConverter();
        private static readonly JsonEnumerableConverter s_jsonEnumerableConverter = new DefaultEnumerableConverter();

        internal ClassType ClassType;

        // The name of the property with any casing policy or the name specified from JsonPropertyNameAttribute.
        private byte[] _name { get; set; }
        internal ReadOnlySpan<byte> Name => _name;
        internal string NameAsString { get; private set; }

        // Used to support case-insensitive comparison
        private byte[] _compareName { get; set; }
        internal ReadOnlySpan<byte> CompareName => _compareName;
        internal string CompareNameAsString { get; private set; }

        // The escaped name passed to the writer.
        internal byte[] _escapedName { get; private set; }
        internal ReadOnlySpan<byte> EscapedName => _escapedName;

        internal bool HasGetter { get; set; }
        internal bool HasSetter { get; set; }
        internal bool ShouldSerialize { get; private set; }
        internal bool ShouldDeserialize { get; private set; }

        internal bool IgnoreNullValues { get; private set; }


        // todo: to minimize hashtable lookups, cache JsonClassInfo:
        //public JsonClassInfo ClassInfo;

        // Constructor used for internal identifiers
        internal JsonPropertyInfo() { }

        internal JsonPropertyInfo(
            Type parentClassType,
            Type declaredPropertyType,
            Type runtimePropertyType,
            PropertyInfo propertyInfo,
            Type elementType,
            JsonSerializerOptions options)
        {
            ParentClassType = parentClassType;
            DeclaredPropertyType = declaredPropertyType;
            RuntimePropertyType = runtimePropertyType;
            PropertyInfo = propertyInfo;
            ClassType = JsonClassInfo.GetClassType(runtimePropertyType);
            if (elementType != null)
            {
                Debug.Assert(ClassType == ClassType.Enumerable || ClassType == ClassType.Dictionary);
                ElementClassInfo = options.GetOrAddClass(elementType);
            }

            IsNullableType = runtimePropertyType.IsGenericType && runtimePropertyType.GetGenericTypeDefinition() == typeof(Nullable<>);
            CanBeNull = IsNullableType || !runtimePropertyType.IsValueType;
        }

        internal bool CanBeNull { get; private set; }
        internal JsonClassInfo ElementClassInfo { get; private set; }
        internal JsonEnumerableConverter EnumerableConverter { get; private set; }

        internal bool IsNullableType { get; private set; }

        internal PropertyInfo PropertyInfo { get; private set; }

        internal Type ParentClassType { get; private set; }

        internal Type DeclaredPropertyType { get; private set; }

        internal Type RuntimePropertyType { get; private set; }

        internal virtual void GetPolicies(JsonSerializerOptions options)
        {
            DetermineSerializationCapabilities(options);
            DeterminePropertyName(options);
            IgnoreNullValues = options.IgnoreNullValues;
        }

        private void DeterminePropertyName(JsonSerializerOptions options)
        {
            if (PropertyInfo != null)
            {
                JsonPropertyNameAttribute nameAttribute = GetAttribute<JsonPropertyNameAttribute>();
                if (nameAttribute != null)
                {
                    NameAsString = nameAttribute.Name;
                    
                    // This is detected and thrown by caller.
                    if (NameAsString == null)
                    {
                        return;
                    }
                }
                else if (options.PropertyNamingPolicy != null)
                {
                    NameAsString = options.PropertyNamingPolicy.ConvertName(PropertyInfo.Name);

                    // This is detected and thrown by caller.
                    if (NameAsString == null)
                    {
                        return;
                    }
                }
                else
                {
                    NameAsString = PropertyInfo.Name;
                }

                // At this point propertyName is valid UTF16, so just call the simple UTF16->UTF8 encoder.
                _name = Encoding.UTF8.GetBytes(NameAsString);

                // Set the compare name.
                if (options.PropertyNameCaseInsensitive)
                {
                    CompareNameAsString = NameAsString.ToUpperInvariant();
                    _compareName = Encoding.UTF8.GetBytes(CompareNameAsString);
                }
                else
                {
                    CompareNameAsString = NameAsString;
                    _compareName = _name;
                }

                // Cache the escaped name.
                int valueIdx = JsonWriterHelper.NeedsEscaping(_name);
                if (valueIdx == -1)
                {
                    _escapedName = _name;
                }
                else
                {
                    int length = JsonWriterHelper.GetMaxEscapedLength(_name.Length, valueIdx);

                    byte[] tempArray = ArrayPool<byte>.Shared.Rent(length);

                    JsonWriterHelper.EscapeString(_name, tempArray, valueIdx, out int written);
                    _escapedName = new byte[written];
                    tempArray.CopyTo(_escapedName, 0);

                    // We clear the array because it is "user data" (although a property name).
                    new Span<byte>(tempArray, 0, written).Clear();
                    ArrayPool<byte>.Shared.Return(tempArray);
                }
            }
        }

        private void DetermineSerializationCapabilities(JsonSerializerOptions options)
        {
            bool hasIgnoreAttribute = (GetAttribute<JsonIgnoreAttribute>() != null);

            if (hasIgnoreAttribute)
            {
                // We don't serialize or deserialize.
                return;
            }

            if (ClassType != ClassType.Enumerable)
            {
                // We serialize if there is a getter + no [Ignore] attribute + not ignoring readonly properties.
                ShouldSerialize = HasGetter && (HasSetter || !options.IgnoreReadOnlyProperties);

                // We deserialize if there is a setter + no [Ignore] attribute. 
                ShouldDeserialize = HasSetter;
            }
            else
            {
                if (HasGetter)
                {
                    if (HasSetter)
                    {
                        ShouldDeserialize = true;
                    }
                    else if (RuntimePropertyType.IsAssignableFrom(typeof(IList)))
                    {
                        ShouldDeserialize = true;
                    }
                    //else
                    //{
                    //    // todo: future feature that allows non-List types (e.g. from System.Collections.Immutable) to have converters.
                    //}
                }
                //else if (HasSetter)
                //{
                //    // todo: Special case where there is no getter but a setter (and an EnumerableConverter)
                //}

                if (ShouldDeserialize)
                {
                    ShouldSerialize = HasGetter;

                    if (RuntimePropertyType.IsArray)
                    {
                        EnumerableConverter = s_jsonArrayConverter;
                    }
                    else if (typeof(IEnumerable).IsAssignableFrom(RuntimePropertyType))
                    {
                        Type elementType = JsonClassInfo.GetElementType(RuntimePropertyType);

                        // If the property type only has interface(s) exposed by JsonEnumerableT<T> then use JsonEnumerableT as the converter.
                        if (RuntimePropertyType.IsAssignableFrom(typeof(JsonEnumerableT<>).MakeGenericType(elementType)))
                        {
                            EnumerableConverter = s_jsonEnumerableConverter;
                        }
                    }
                }
                else
                {
                    ShouldSerialize = HasGetter && !options.IgnoreReadOnlyProperties;
                }
            }
        }

        // After the property is added, clear any state not used later.
        internal void ClearUnusedValuesAfterAdd()
        {
            NameAsString = null;
            CompareNameAsString = null;
        }

        // Copy any settings defined at run-time to the new property.
        internal void CopyRuntimeSettingsTo(JsonPropertyInfo other)
        {
            other._name = _name;
            other._compareName = _compareName;
            other._escapedName = _escapedName;
        }

        internal abstract object GetValueAsObject(object obj, JsonSerializerOptions options);

        internal TAttribute GetAttribute<TAttribute>() where TAttribute : Attribute
        {
            return (TAttribute)PropertyInfo?.GetCustomAttribute(typeof(TAttribute), inherit: false);
        }

        internal abstract void ApplyNullValue(JsonSerializerOptions options, ref ReadStack state);
            
        internal abstract IList CreateConverterList();

        internal abstract Type GetConcreteType(Type interfaceType);

        internal abstract void Read(JsonTokenType tokenType, JsonSerializerOptions options, ref ReadStack state, ref Utf8JsonReader reader);
        internal abstract void ReadEnumerable(JsonTokenType tokenType, JsonSerializerOptions options, ref ReadStack state, ref Utf8JsonReader reader);
        internal abstract void SetValueAsObject(object obj, object value, JsonSerializerOptions options);

        internal abstract void Write(JsonSerializerOptions options, ref WriteStackFrame current, Utf8JsonWriter writer);

        internal abstract void WriteDictionary(JsonSerializerOptions options, ref WriteStackFrame current, Utf8JsonWriter writer);
        internal abstract void WriteEnumerable(JsonSerializerOptions options, ref WriteStackFrame current, Utf8JsonWriter writer);
    }
}
