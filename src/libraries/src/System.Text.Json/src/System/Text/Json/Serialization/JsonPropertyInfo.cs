// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Diagnostics;
using System.Reflection;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Converters;

namespace System.Text.Json
{
    [DebuggerDisplay("PropertyInfo={PropertyInfo}, Element={ElementClassInfo}")]
    internal abstract class JsonPropertyInfo
    {
        // Cache the converters so they don't get created for every enumerable property.
        private static readonly JsonEnumerableConverter s_jsonDerivedEnumerableConverter = new DefaultDerivedEnumerableConverter();
        private static readonly JsonEnumerableConverter s_jsonArrayConverter = new DefaultArrayConverter();
        private static readonly JsonEnumerableConverter s_jsonICollectionConverter = new DefaultICollectionConverter();
        private static readonly JsonEnumerableConverter s_jsonImmutableEnumerableConverter = new DefaultImmutableEnumerableConverter();
        private static readonly JsonDictionaryConverter s_jsonDerivedDictionaryConverter = new DefaultDerivedDictionaryConverter();
        private static readonly JsonDictionaryConverter s_jsonIDictionaryConverter = new DefaultIDictionaryConverter();
        private static readonly JsonDictionaryConverter s_jsonImmutableDictionaryConverter = new DefaultImmutableDictionaryConverter();

        public static readonly JsonPropertyInfo s_missingProperty = GetMissingProperty();

        private JsonClassInfo _elementClassInfo;
        private JsonClassInfo _runtimeClassInfo;
        private JsonClassInfo _declaredTypeClassInfo;

        public bool CanBeNull { get; private set; }

        public ClassType ClassType;

        public abstract JsonConverter ConverterBase { get; set; }

        private static JsonPropertyInfo GetMissingProperty()
        {
            JsonPropertyInfo info = new JsonPropertyInfoNotNullable<object, object, object, object>();
            info.IsPropertyPolicy = false;
            info.ShouldDeserialize = false;
            info.ShouldSerialize = false;
            return info;
        }

        // Copy any settings defined at run-time to the new property.
        public void CopyRuntimeSettingsTo(JsonPropertyInfo other)
        {
            other.EscapedName = EscapedName;
            other.Name = Name;
            other.NameAsString = NameAsString;
            other.PropertyNameKey = PropertyNameKey;
        }

        public abstract IList CreateConverterList();

        public abstract IEnumerable CreateDerivedEnumerableInstance(ref ReadStack state, JsonPropertyInfo collectionPropertyInfo, IList sourceList);

        public abstract object CreateDerivedDictionaryInstance(ref ReadStack state, JsonPropertyInfo collectionPropertyInfo, IDictionary sourceDictionary);

        public abstract IEnumerable CreateIEnumerableInstance(ref ReadStack state, Type parentType, IList sourceList);

        public abstract IDictionary CreateIDictionaryInstance(ref ReadStack state, Type parentType, IDictionary sourceDictionary);

        public abstract IEnumerable CreateImmutableCollectionInstance(ref ReadStack state, Type collectionType, string delegateKey, IList sourceList, JsonSerializerOptions options);

        public abstract IDictionary CreateImmutableDictionaryInstance(ref ReadStack state, Type collectionType, string delegateKey, IDictionary sourceDictionary, JsonSerializerOptions options);

        // Create a property that is ignored at run-time. It uses the same type (typeof(sbyte)) to help
        // prevent issues with unsupported types and helps ensure we don't accidently (de)serialize it.
        public static JsonPropertyInfo CreateIgnoredPropertyPlaceholder(PropertyInfo propertyInfo, JsonSerializerOptions options)
        {
            JsonPropertyInfo jsonPropertyInfo = new JsonPropertyInfoNotNullable<sbyte, sbyte, sbyte, sbyte>();
            jsonPropertyInfo.Options = options;
            jsonPropertyInfo.PropertyInfo = propertyInfo;
            jsonPropertyInfo.DeterminePropertyName();

            Debug.Assert(!jsonPropertyInfo.ShouldDeserialize);
            Debug.Assert(!jsonPropertyInfo.ShouldSerialize);

            return jsonPropertyInfo;
        }

        public Type DeclaredPropertyType { get; private set; }

        public Type ImplementedPropertyType { get; private set; }

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

            // Cache the escaped property name.
            EscapedName = JsonEncodedText.Encode(Name, Options.Encoder);

            ulong key = JsonClassInfo.GetKey(Name);
            PropertyNameKey = key;
        }

        private void DetermineSerializationCapabilities()
        {
            if (ClassType != ClassType.Enumerable &&
                ClassType != ClassType.Dictionary &&
                ClassType != ClassType.IDictionaryConstructible)
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
                    ShouldSerialize = true;

                    if (HasSetter)
                    {
                        ShouldDeserialize = true;

                        if (RuntimePropertyType.IsArray)
                        {
                            // Verify that we don't have a multidimensional array.
                            if (RuntimePropertyType.GetArrayRank() > 1)
                            {
                                throw ThrowHelper.GetNotSupportedException_SerializationNotSupportedCollection(RuntimePropertyType, ParentClassType, PropertyInfo);
                            }

                            EnumerableConverter = s_jsonArrayConverter;
                        }
                        else if (ClassType == ClassType.IDictionaryConstructible)
                        {
                            // Natively supported type.
                            if (DeclaredPropertyType == ImplementedPropertyType)
                            {
                                if (RuntimePropertyType.FullName.StartsWith(JsonClassInfo.ImmutableNamespaceName))
                                {
                                    DefaultImmutableDictionaryConverter.RegisterImmutableDictionary(
                                        RuntimePropertyType, JsonClassInfo.GetElementType(RuntimePropertyType, ParentClassType, PropertyInfo, Options), Options);

                                    DictionaryConverter = s_jsonImmutableDictionaryConverter;
                                }
                                else if (JsonClassInfo.IsDeserializedByConstructingWithIDictionary(RuntimePropertyType))
                                {
                                    DictionaryConverter = s_jsonIDictionaryConverter;
                                }
                            }
                            // Type that implements a type with ClassType IDictionaryConstructible.
                            else
                            {
                                DictionaryConverter = s_jsonDerivedDictionaryConverter;
                            }
                        }
                        else if (ClassType == ClassType.Enumerable)
                        {
                            // Else if it's an implementing type whose runtime type is not assignable to IList.
                            if (DeclaredPropertyType != ImplementedPropertyType &&
                                (!typeof(IList).IsAssignableFrom(RuntimePropertyType) ||
                                ImplementedPropertyType == typeof(ArrayList) ||
                                ImplementedPropertyType == typeof(IList)))
                            {
                                EnumerableConverter = s_jsonDerivedEnumerableConverter;
                            }
                            else if (JsonClassInfo.IsDeserializedByConstructingWithIList(RuntimePropertyType) ||
                                (!typeof(IList).IsAssignableFrom(RuntimePropertyType) &&
                                JsonClassInfo.HasConstructorThatTakesGenericIEnumerable(RuntimePropertyType, Options)))
                            {
                                EnumerableConverter = s_jsonICollectionConverter;
                            }
                            else if (RuntimePropertyType.IsGenericType &&
                                RuntimePropertyType.FullName.StartsWith(JsonClassInfo.ImmutableNamespaceName) &&
                                RuntimePropertyType.GetGenericArguments().Length == 1)
                            {
                                DefaultImmutableEnumerableConverter.RegisterImmutableCollection(RuntimePropertyType,
                                    JsonClassInfo.GetElementType(RuntimePropertyType, ParentClassType, PropertyInfo, Options), Options);
                                EnumerableConverter = s_jsonImmutableEnumerableConverter;
                            }

                        }
                    }
                }
            }
        }

        /// <summary>
        /// Return the JsonClassInfo for the element type, or null if the property is not an enumerable or dictionary.
        /// </summary>
        /// <remarks>
        /// This should not be called during warm-up (initial creation of JsonClassInfos) to avoid recursive behavior
        /// which could result in a StackOverflowException.
        /// </remarks>
        public JsonClassInfo ElementClassInfo
        {
            get
            {
                if (_elementClassInfo == null && ElementType != null)
                {
                    Debug.Assert(ClassType == ClassType.Enumerable ||
                        ClassType == ClassType.Dictionary ||
                        ClassType == ClassType.IDictionaryConstructible);

                    _elementClassInfo = Options.GetOrAddClass(ElementType);
                }

                return _elementClassInfo;
            }
        }

        public Type ElementType { get; set; }

        public JsonEnumerableConverter EnumerableConverter { get; private set; }
        public JsonDictionaryConverter DictionaryConverter { get; private set; }

        // The escaped name passed to the writer.
        // Use a field here (not a property) to avoid value semantics.
        public JsonEncodedText? EscapedName;

        public static TAttribute GetAttribute<TAttribute>(PropertyInfo propertyInfo) where TAttribute : Attribute
        {
            return (TAttribute)propertyInfo?.GetCustomAttribute(typeof(TAttribute), inherit: false);
        }

        public abstract Type GetDictionaryConcreteType();

        public abstract Type GetConcreteType(Type type);

        public virtual void GetPolicies()
        {
            DetermineSerializationCapabilities();
            DeterminePropertyName();
            IgnoreNullValues = Options.IgnoreNullValues;
        }

        public abstract object GetValueAsObject(object obj);

        public bool HasGetter { get; set; }
        public bool HasSetter { get; set; }

        public bool HasInternalConverter { get; private set; }

        public virtual void Initialize(
            Type parentClassType,
            Type declaredPropertyType,
            Type runtimePropertyType,
            Type implementedPropertyType,
            PropertyInfo propertyInfo,
            Type elementType,
            JsonConverter converter,
            JsonSerializerOptions options)
        {
            ParentClassType = parentClassType;
            DeclaredPropertyType = declaredPropertyType;
            RuntimePropertyType = runtimePropertyType;
            ImplementedPropertyType = implementedPropertyType;
            PropertyInfo = propertyInfo;
            ElementType = elementType;
            Options = options;
            IsNullableType = runtimePropertyType.IsGenericType && runtimePropertyType.GetGenericTypeDefinition() == typeof(Nullable<>);
            CanBeNull = IsNullableType || !runtimePropertyType.IsValueType;

            if (converter != null)
            {
                ConverterBase = converter;

                HasInternalConverter = (converter.GetType().Assembly == GetType().Assembly);

                // Avoid calling GetClassType since it will re-ask if there is a converter which is slow.
                if (runtimePropertyType == typeof(object))
                {
                    ClassType = ClassType.Unknown;
                }
                else
                {
                    ClassType = ClassType.Value;
                }
            }
            // Special case for immutable collections.
            else if (declaredPropertyType != implementedPropertyType && !JsonClassInfo.IsNativelySupportedCollection(declaredPropertyType))
            {
                ClassType = JsonClassInfo.GetClassType(declaredPropertyType, options);
            }
            else
            {
                ClassType = JsonClassInfo.GetClassType(runtimePropertyType, options);
            }
        }

        public bool IgnoreNullValues { get; private set; }

        public bool IsNullableType { get; private set; }

        public bool IsPropertyPolicy { get; protected set; }

        // The name from a Json value. This is cached for performance on first deserialize.
        public byte[] JsonPropertyName { get; set; }

        // The name of the property with any casing policy or the name specified from JsonPropertyNameAttribute.
        public byte[] Name { get; private set; }
        public string NameAsString { get; private set; }

        // Key for fast property name lookup.
        public ulong PropertyNameKey { get; set; }

        // Options can be referenced here since all JsonPropertyInfos originate from a JsonClassInfo that is cached on JsonSerializerOptions.
        protected JsonSerializerOptions Options { get; set; }

        protected abstract void OnRead(ref ReadStack state, ref Utf8JsonReader reader);
        protected abstract void OnReadEnumerable(ref ReadStack state, ref Utf8JsonReader reader);
        protected abstract void OnWrite(ref WriteStackFrame current, Utf8JsonWriter writer);
        protected virtual void OnWriteDictionary(ref WriteStackFrame current, Utf8JsonWriter writer) { }
        protected abstract void OnWriteEnumerable(ref WriteStackFrame current, Utf8JsonWriter writer);

        public Type ParentClassType { get; private set; }

        public PropertyInfo PropertyInfo { get; private set; }

        public void Read(JsonTokenType tokenType, ref ReadStack state, ref Utf8JsonReader reader)
        {
            Debug.Assert(ShouldDeserialize);

            JsonPropertyInfo propertyInfo;
            JsonClassInfo elementClassInfo = ElementClassInfo;
            if (elementClassInfo != null && (propertyInfo = elementClassInfo.PolicyProperty) != null)
            {
                // Forward the setter to the value-based JsonPropertyInfo.
                propertyInfo.ReadEnumerable(tokenType, ref state, ref reader);
            }
            // For performance on release build, don't verify converter correctness for internal converters.
            else if (HasInternalConverter)
            {
#if DEBUG
                JsonTokenType originalTokenType = reader.TokenType;
                int originalDepth = reader.CurrentDepth;
                long originalBytesConsumed = reader.BytesConsumed;
#endif

                OnRead(ref state, ref reader);

#if DEBUG
                VerifyRead(originalTokenType, originalDepth, originalBytesConsumed, ref reader);
#endif
            }
            else
            {
                JsonTokenType originalTokenType = reader.TokenType;
                int originalDepth = reader.CurrentDepth;
                long originalBytesConsumed = reader.BytesConsumed;

                OnRead(ref state, ref reader);

                VerifyRead(originalTokenType, originalDepth, originalBytesConsumed, ref reader);
            }
        }

        public void ReadEnumerable(JsonTokenType tokenType, ref ReadStack state, ref Utf8JsonReader reader)
        {
            Debug.Assert(ShouldDeserialize);

            // For performance on release build, don't verify converter correctness for internal converters.
            if (HasInternalConverter)
            {
#if DEBUG
                JsonTokenType originalTokenType = reader.TokenType;
                int originalDepth = reader.CurrentDepth;
                long originalBytesConsumed = reader.BytesConsumed;
#endif

                OnReadEnumerable(ref state, ref reader);

#if DEBUG
                VerifyRead(originalTokenType, originalDepth, originalBytesConsumed, ref reader);
#endif
            }
            else
            {
                JsonTokenType originalTokenType = reader.TokenType;
                int originalDepth = reader.CurrentDepth;
                long originalBytesConsumed = reader.BytesConsumed;

                OnReadEnumerable(ref state, ref reader);

                VerifyRead(originalTokenType, originalDepth, originalBytesConsumed, ref reader);
            }
        }

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

        public JsonClassInfo DeclaredTypeClassInfo
        {
            get
            {
                if (_declaredTypeClassInfo == null)
                {
                    _declaredTypeClassInfo = Options.GetOrAddClass(DeclaredPropertyType);
                }

                return _declaredTypeClassInfo;
            }
        }

        public Type RuntimePropertyType { get; private set; }

        public abstract void SetValueAsObject(object obj, object value);

        public bool ShouldSerialize { get; private set; }
        public bool ShouldDeserialize { get; private set; }

        private void VerifyRead(JsonTokenType tokenType, int depth, long bytesConsumed, ref Utf8JsonReader reader)
        {
            switch (tokenType)
            {
                case JsonTokenType.StartArray:
                    if (reader.TokenType != JsonTokenType.EndArray)
                    {
                        ThrowHelper.ThrowJsonException_SerializationConverterRead(ConverterBase);
                    }
                    else if (depth != reader.CurrentDepth)
                    {
                        ThrowHelper.ThrowJsonException_SerializationConverterRead(ConverterBase);
                    }

                    // Should not be possible to have not read anything.
                    Debug.Assert(bytesConsumed < reader.BytesConsumed);
                    break;

                case JsonTokenType.StartObject:
                    if (reader.TokenType != JsonTokenType.EndObject)
                    {
                        ThrowHelper.ThrowJsonException_SerializationConverterRead(ConverterBase);
                    }
                    else if (depth != reader.CurrentDepth)
                    {
                        ThrowHelper.ThrowJsonException_SerializationConverterRead(ConverterBase);
                    }

                    // Should not be possible to have not read anything.
                    Debug.Assert(bytesConsumed < reader.BytesConsumed);
                    break;

                default:
                    // Reading a single property value.
                    if (reader.BytesConsumed != bytesConsumed)
                    {
                        ThrowHelper.ThrowJsonException_SerializationConverterRead(ConverterBase);
                    }

                    // Should not be possible to change token type.
                    Debug.Assert(reader.TokenType == tokenType);

                    break;
            }
        }

        public void Write(ref WriteStack state, Utf8JsonWriter writer)
        {
            Debug.Assert(ShouldSerialize);

            if (state.Current.CollectionEnumerator != null)
            {
                // Forward the setter to the value-based JsonPropertyInfo.
                JsonPropertyInfo propertyInfo = ElementClassInfo.PolicyProperty;
                propertyInfo.WriteEnumerable(ref state, writer);
            }
            // For performance on release build, don't verify converter correctness for internal converters.
            else if (HasInternalConverter)
            {
#if DEBUG
                int originalDepth = writer.CurrentDepth;
#endif

                OnWrite(ref state.Current, writer);

#if DEBUG
                VerifyWrite(originalDepth, writer);
#endif
            }
            else
            {
                int originalDepth = writer.CurrentDepth;
                OnWrite(ref state.Current, writer);
                VerifyWrite(originalDepth, writer);
            }
        }

        public void WriteDictionary(ref WriteStack state, Utf8JsonWriter writer)
        {
            Debug.Assert(ShouldSerialize);

            // For performance on release build, don't verify converter correctness for internal converters.
            if (HasInternalConverter)
            {
#if DEBUG
                int originalDepth = writer.CurrentDepth;
#endif

                OnWriteDictionary(ref state.Current, writer);

#if DEBUG
                VerifyWrite(originalDepth, writer);
#endif
            }
            else
            {
                int originalDepth = writer.CurrentDepth;
                OnWriteDictionary(ref state.Current, writer);
                VerifyWrite(originalDepth, writer);
            }
        }

        public void WriteEnumerable(ref WriteStack state, Utf8JsonWriter writer)
        {
            Debug.Assert(ShouldSerialize);

            // For performance on release build, don't verify converter correctness for internal converters.
            if (HasInternalConverter)
            {
#if DEBUG
                int originalDepth = writer.CurrentDepth;
#endif

                OnWriteEnumerable(ref state.Current, writer);

#if DEBUG
                VerifyWrite(originalDepth, writer);
#endif
            }
            else
            {
                int originalDepth = writer.CurrentDepth;
                OnWriteEnumerable(ref state.Current, writer);
                VerifyWrite(originalDepth, writer);
            }
        }

        private void VerifyWrite(int originalDepth, Utf8JsonWriter writer)
        {
            if (originalDepth != writer.CurrentDepth)
            {
                ThrowHelper.ThrowJsonException_SerializationConverterWrite(ConverterBase);
            }
        }
    }
}
