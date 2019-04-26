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

        public ClassType ClassType;

        // The name of the property with any casing policy or the name specified from JsonPropertyNameAttribute.
        private byte[] _name { get; set; }
        public ReadOnlySpan<byte> Name => _name;
        public string NameAsString { get; private set; }

        // Used to support case-insensitive comparison
        private byte[] _nameUsedToCompare { get; set; }
        public ReadOnlySpan<byte> NameUsedToCompare => _nameUsedToCompare;
        public string NameUsedToCompareAsString { get; private set; }

        // The escaped name passed to the writer.
        public byte[] _escapedName { get; private set; }

        public bool HasGetter { get; set; }
        public bool HasSetter { get; set; }
        public bool ShouldSerialize { get; private set; }
        public bool ShouldDeserialize { get; private set; }

        public bool IgnoreNullValues { get; private set; }


        // todo: to minimize hashtable lookups, cache JsonClassInfo:
        //public JsonClassInfo ClassInfo;

        // Constructor used for internal identifiers
        public JsonPropertyInfo() { }

        public JsonPropertyInfo(
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

        public bool CanBeNull { get; private set; }
        public JsonClassInfo ElementClassInfo { get; private set; }
        public JsonEnumerableConverter EnumerableConverter { get; private set; }

        public bool IsNullableType { get; private set; }

        public PropertyInfo PropertyInfo { get; private set; }

        public Type ParentClassType { get; private set; }

        public Type DeclaredPropertyType { get; private set; }

        public Type RuntimePropertyType { get; private set; }

        public virtual void GetPolicies(JsonSerializerOptions options)
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

                    // null is not valid; JsonClassInfo throws an InvalidOperationException after this return.
                    if (NameAsString == null)
                    {
                        return;
                    }
                }
                else if (options.PropertyNamingPolicy != null)
                {
                    NameAsString = options.PropertyNamingPolicy.ConvertName(PropertyInfo.Name);

                    // null is not valid; JsonClassInfo throws an InvalidOperationException after this return.
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
                    NameUsedToCompareAsString = NameAsString.ToUpperInvariant();
                    _nameUsedToCompare = Encoding.UTF8.GetBytes(NameUsedToCompareAsString);
                }
                else
                {
                    NameUsedToCompareAsString = NameAsString;
                    _nameUsedToCompare = _name;
                }

                // Cache the escaped name.
#if true
                // temporary behavior until the writer can accept escaped string.
                _escapedName = _name;
#else
                
                int valueIdx = JsonWriterHelper.NeedsEscaping(_name);
                if (valueIdx == -1)
                {
                    _escapedName = _name;
                }
                else
                {
                    byte[] pooledName = null;
                    int length = JsonWriterHelper.GetMaxEscapedLength(_name.Length, valueIdx);

                    Span<byte> escapedName = length <= JsonConstants.StackallocThreshold ?
                        stackalloc byte[length] :
                        (pooledName = ArrayPool<byte>.Shared.Rent(length));

                    JsonWriterHelper.EscapeString(_name, escapedName, 0, out int written);

                    _escapedName = escapedName.Slice(0, written).ToArray();

                    if (pooledName != null)
                    {
                        // We clear the array because it is "user data" (although a property name).
                        new Span<byte>(pooledName, 0, written).Clear();
                        ArrayPool<byte>.Shared.Return(pooledName);
                    }
                }
#endif
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
        public void ClearUnusedValuesAfterAdd()
        {
            NameAsString = null;
            NameUsedToCompareAsString = null;
        }

        // Copy any settings defined at run-time to the new property.
        public void CopyRuntimeSettingsTo(JsonPropertyInfo other)
        {
            other._name = _name;
            other._nameUsedToCompare = _nameUsedToCompare;
            other._escapedName = _escapedName;
        }

        public abstract object GetValueAsObject(object obj);

        public TAttribute GetAttribute<TAttribute>() where TAttribute : Attribute
        {
            return (TAttribute)PropertyInfo?.GetCustomAttribute(typeof(TAttribute), inherit: false);
        }

        public abstract void ApplyNullValue(JsonSerializerOptions options, ref ReadStack state);

        public abstract IList CreateConverterList();

        public abstract Type GetConcreteType(Type interfaceType);

        public abstract void Read(JsonTokenType tokenType, JsonSerializerOptions options, ref ReadStack state, ref Utf8JsonReader reader);
        public abstract void ReadEnumerable(JsonTokenType tokenType, JsonSerializerOptions options, ref ReadStack state, ref Utf8JsonReader reader);
        public abstract void SetValueAsObject(object obj, object value);

        public abstract void Write(JsonSerializerOptions options, ref WriteStackFrame current, Utf8JsonWriter writer);

        public abstract void WriteDictionary(JsonSerializerOptions options, ref WriteStackFrame current, Utf8JsonWriter writer);
        public abstract void WriteEnumerable(JsonSerializerOptions options, ref WriteStackFrame current, Utf8JsonWriter writer);
    }
}
