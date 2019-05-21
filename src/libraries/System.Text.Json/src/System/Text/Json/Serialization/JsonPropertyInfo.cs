// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Text.Json.Serialization.Converters;
using System.Text.Json.Serialization.Policies;

namespace System.Text.Json.Serialization
{
    [DebuggerDisplay("PropertyInfo={PropertyInfo}, Element={ElementClassInfo}")]
    internal abstract class JsonPropertyInfo
    {
        // Cache the converters so they don't get created for every enumerable property.
        private static readonly JsonEnumerableConverter s_jsonArrayConverter = new DefaultArrayConverter();
        private static readonly JsonEnumerableConverter s_jsonEnumerableConverter = new DefaultEnumerableConverter();
        private static readonly JsonEnumerableConverter s_jsonIEnumerableConstuctibleConverter = new DefaultIEnumerableConstructibleConverter();
        private static readonly JsonEnumerableConverter s_jsonImmutableConverter = new DefaultImmutableConverter();

        private JsonClassInfo _runtimeClassInfo;

        private Type _elementType;
        private JsonClassInfo _elementClassInfo;

        public static readonly JsonPropertyInfo s_missingProperty = new JsonPropertyInfoNotNullable<object, object, object>();

        public ClassType ClassType;

        // The name of the property with any casing policy or the name specified from JsonPropertyNameAttribute.
        public byte[] Name { get; private set; }
        public string NameAsString { get; private set; }

        // Used to support case-insensitive comparison
        public byte[] NameUsedToCompare { get; private set; }
        public string NameUsedToCompareAsString { get; private set; }

        // The escaped name passed to the writer.
        public byte[] EscapedName { get; private set; }

        public bool HasGetter { get; set; }
        public bool HasSetter { get; set; }
        public bool ShouldSerialize { get; private set; }
        public bool ShouldDeserialize { get; private set; }

        public bool IsPropertyPolicy {get; protected set;}
        public bool IgnoreNullValues { get; private set; }

        // Options can be referenced here since all JsonPropertyInfos originate from a JsonClassInfo that is cached on JsonSerializerOptions.
        protected JsonSerializerOptions Options { get; set; }

        public JsonClassInfo RuntimeClassInfo
        {
            get
            {
                if (_runtimeClassInfo == null)
                {
                    _runtimeClassInfo = Options.GetOrAddClass(RuntimePropertyType);
                }

                return _runtimeClassInfo;
            }
        }

        /// <summary>
        /// Return the JsonClassInfo for the element type, or null if the the property is not an enumerable or dictionary.
        /// </summary>
        public JsonClassInfo ElementClassInfo
        {
            get
            {
                if (_elementClassInfo == null && _elementType != null)
                {
                    Debug.Assert(ClassType == ClassType.Enumerable || ClassType == ClassType.Dictionary);
                    _elementClassInfo = Options.GetOrAddClass(_elementType);
                }

                return _elementClassInfo;
            }
        }

        public virtual void Initialize(
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
            _elementType = elementType;
            Options = options;
            IsNullableType = runtimePropertyType.IsGenericType && runtimePropertyType.GetGenericTypeDefinition() == typeof(Nullable<>);
            CanBeNull = IsNullableType || !runtimePropertyType.IsValueType;
        }

        public bool CanBeNull { get; private set; }
        public JsonEnumerableConverter EnumerableConverter { get; private set; }

        public bool IsNullableType { get; private set; }

        public PropertyInfo PropertyInfo { get; private set; }

        public Type ParentClassType { get; private set; }

        public Type DeclaredPropertyType { get; private set; }

        public Type RuntimePropertyType { get; private set; }

        public virtual void GetPolicies()
        {
            DetermineSerializationCapabilities();
            DeterminePropertyName();
            IgnoreNullValues = Options.IgnoreNullValues;
        }

        private void DeterminePropertyName()
        {
            if (PropertyInfo == null)
            {
                return;
            }

            JsonPropertyNameAttribute nameAttribute = GetAttribute<JsonPropertyNameAttribute>(PropertyInfo);
            if (nameAttribute != null)
            {
                string name = nameAttribute.Name;
                if (name == null)
                {
                    ThrowHelper.ThrowInvalidOperationException_SerializerPropertyNameNull(ParentClassType, this);
                }

                NameAsString = name;
            }
            else if (Options.PropertyNamingPolicy != null)
            {
                string name = Options.PropertyNamingPolicy.ConvertName(PropertyInfo.Name);
                if (name == null)
                {
                    ThrowHelper.ThrowInvalidOperationException_SerializerPropertyNameNull(ParentClassType, this);
                }

                NameAsString = name;
            }
            else
            {
                NameAsString = PropertyInfo.Name;
            }

            Debug.Assert(NameAsString != null);

            // At this point propertyName is valid UTF16, so just call the simple UTF16->UTF8 encoder.
            Name = Encoding.UTF8.GetBytes(NameAsString);

            // Set the compare name.
            if (Options.PropertyNameCaseInsensitive)
            {
                NameUsedToCompareAsString = NameAsString.ToUpperInvariant();
                NameUsedToCompare = Encoding.UTF8.GetBytes(NameUsedToCompareAsString);
            }
            else
            {
                NameUsedToCompareAsString = NameAsString;
                NameUsedToCompare = Name;
            }

            // Cache the escaped name.
#if true
            // temporary behavior until the writer can accept escaped string.
            EscapedName = Name;
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

        private void DetermineSerializationCapabilities()
        {
            if (ClassType != ClassType.Enumerable && ClassType != ClassType.Dictionary)
            {
                // We serialize if there is a getter + not ignoring readonly properties.
                ShouldSerialize = HasGetter && (HasSetter || !Options.IgnoreReadOnlyProperties);

                // We deserialize if there is a setter. 
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
                    else if (!RuntimePropertyType.IsArray &&
                        (typeof(IList).IsAssignableFrom(RuntimePropertyType) || typeof(IDictionary).IsAssignableFrom(RuntimePropertyType)))
                    {
                        ShouldDeserialize = true;
                    }
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
                        Type elementType = JsonClassInfo.GetElementType(RuntimePropertyType, ParentClassType, PropertyInfo);

                        // If the property type only has interface(s) exposed by JsonEnumerableT<T> then use JsonEnumerableT as the converter.
                        if (RuntimePropertyType.IsAssignableFrom(typeof(JsonEnumerableT<>).MakeGenericType(elementType)))
                        {
                            EnumerableConverter = s_jsonEnumerableConverter;
                        }
                        // Else if IList can't be assigned from the property type (we populate and return an IList directly)
                        // and the type can be constructed with an IEnumerable<T>, then use the
                        // IEnumerableConstructible converter to create the instance.
                        else if (!typeof(IList).IsAssignableFrom(RuntimePropertyType) &&
                            RuntimePropertyType.GetConstructor(new Type[] { typeof(List<>).MakeGenericType(elementType) }) != null)
                        {
                            EnumerableConverter = s_jsonIEnumerableConstuctibleConverter;
                        }
                        // Else if it's a System.Collections.Immutable type with one generic argument.
                        else if (RuntimePropertyType.IsGenericType &&
                            RuntimePropertyType.FullName.StartsWith(DefaultImmutableConverter.ImmutableNamespace) &&
                            RuntimePropertyType.GetGenericArguments().Length == 1)
                        {
                            EnumerableConverter = s_jsonImmutableConverter;
                            ((DefaultImmutableConverter)EnumerableConverter).RegisterImmutableCollectionType(RuntimePropertyType, elementType, Options);
                        }
                    }
                }
                else
                {
                    ShouldSerialize = HasGetter && !Options.IgnoreReadOnlyProperties;
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
            other.Name = Name;
            other.NameUsedToCompare = NameUsedToCompare;
            other.EscapedName = EscapedName;
        }

        // Create a property that is either ignored at run-time. It uses typeof(int) in order to prevent
        // issues with unsupported types and helps ensure we don't accidently (de)serialize it.
        public static JsonPropertyInfo CreateIgnoredPropertyPlaceholder(PropertyInfo propertyInfo, JsonSerializerOptions options)
        {
            JsonPropertyInfo jsonPropertyInfo = new JsonPropertyInfoNotNullable<int, int, int>();
            jsonPropertyInfo.Options = options;
            jsonPropertyInfo.PropertyInfo = propertyInfo;
            jsonPropertyInfo.DeterminePropertyName();

            Debug.Assert(!jsonPropertyInfo.ShouldDeserialize);
            Debug.Assert(!jsonPropertyInfo.ShouldSerialize);

            return jsonPropertyInfo;
        }

        public abstract object GetValueAsObject(object obj);

        public static TAttribute GetAttribute<TAttribute>(PropertyInfo propertyInfo) where TAttribute : Attribute
        {
            return (TAttribute)propertyInfo?.GetCustomAttribute(typeof(TAttribute), inherit: false);
        }

        public abstract IEnumerable CreateImmutableCollectionFromList(string delegateKey, IList sourceList);

        public abstract IEnumerable CreateIEnumerableConstructibleType(Type enumerableType, IList sourceList);

        public abstract IList CreateConverterList();

        public abstract Type GetDictionaryConcreteType();

        public abstract void Read(JsonTokenType tokenType, ref ReadStack state, ref Utf8JsonReader reader);
        public abstract void ReadEnumerable(JsonTokenType tokenType, ref ReadStack state, ref Utf8JsonReader reader);
        public abstract void SetValueAsObject(object obj, object value);

        public abstract void Write(ref WriteStackFrame current, Utf8JsonWriter writer);

        public virtual void WriteDictionary(ref WriteStackFrame current, Utf8JsonWriter writer) { }
        public abstract void WriteEnumerable(ref WriteStackFrame current, Utf8JsonWriter writer);
    }
}
