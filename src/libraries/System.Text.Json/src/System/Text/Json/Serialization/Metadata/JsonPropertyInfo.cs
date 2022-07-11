// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text.Json.Reflection;

namespace System.Text.Json.Serialization.Metadata
{
    /// <summary>
    /// Provides JSON serialization-related metadata about a property or field.
    /// </summary>
    [DebuggerDisplay("{DebuggerDisplay,nq}")]
    public abstract class JsonPropertyInfo
    {
        internal static readonly JsonPropertyInfo s_missingProperty = GetPropertyPlaceholder();

        internal JsonTypeInfo? ParentTypeInfo { get; private set; }
        private JsonTypeInfo? _jsonTypeInfo;

        internal ConverterStrategy ConverterStrategy { get; private protected set; }

        /// <summary>
        /// Converter resolved from PropertyType and not taking in consideration any custom attributes or custom settings.
        /// - for reflection we store the original value since we need it in order to construct typed JsonPropertyInfo
        /// - for source gen it remains null, we will initialize it only if someone used resolver to remove CustomConverter
        /// </summary>
        internal JsonConverter? DefaultConverterForType
        {
            get => _defaultConverterForType;
            set
            {
                _defaultConverterForType = value;
                ConverterStrategy = value?.ConverterStrategy ?? default;
            }
        }

        private JsonConverter? _defaultConverterForType;

        /// <summary>
        /// Converter after applying CustomConverter (i.e. JsonConverterAttribute)
        /// </summary>
        internal abstract JsonConverter EffectiveConverter { get; }

        /// <summary>
        /// Custom converter override at the property level, equivalent to <see cref="JsonConverterAttribute" /> annotation.
        /// </summary>
        public JsonConverter? CustomConverter
        {
            get => _customConverter;
            set
            {
                VerifyMutable();
                _customConverter = value;
            }
        }

        private JsonConverter? _customConverter;

        /// <summary>
        /// Gets or sets a getter delegate for the property.
        /// </summary>
        /// <remarks>
        /// Setting to <see langword="null"/> will result in the property being skipped on serialization.
        /// </remarks>
        public Func<object, object?>? Get
        {
            get => _untypedGet;
            set => SetGetter(value);
        }

        /// <summary>
        /// Gets or sets a setter delegate for the property.
        /// </summary>
        /// <remarks>
        /// Setting to <see langword="null"/> will result in the property being skipped on deserialization.
        /// </remarks>
        public Action<object, object?>? Set
        {
            get => _untypedSet;
            set => SetSetter(value);
        }

        private protected Func<object, object?>? _untypedGet;
        private protected Action<object, object?>? _untypedSet;

        private protected abstract void SetGetter(Delegate? getter);
        private protected abstract void SetSetter(Delegate? setter);

        /// <summary>
        /// Gets or sets a predicate deciding whether the current property value should be serialized.
        /// </summary>
        /// <remarks>
        /// The first parameter denotes the parent object, the second parameter denotes the property value.
        ///
        /// Setting the predicate to <see langword="null"/> is equivalent to always serializing the property value.
        ///
        /// When serializing using <see cref="DefaultJsonTypeInfoResolver"/>, the value of
        /// <see cref="JsonIgnoreAttribute.Condition"/> will map to this predicate.
        /// </remarks>
        public Func<object, object?, bool>? ShouldSerialize
        {
            get => _shouldSerialize;
            set
            {
                VerifyMutable();
                _shouldSerialize = value;
                // By default we will go through faster path (not using delegate) and use IgnoreCondition
                // If user sets it explicitly we always go through delegate
                _ignoreCondition = null;
                _shouldSerializeIsExplicitlySet = true;
            }
        }

        internal JsonIgnoreCondition? IgnoreCondition
        {
            get => _ignoreCondition;
            set
            {
                Debug.Assert(!_isConfigured);

                _ignoreCondition = value;
                _shouldSerialize = value != null ? GetShouldSerializeForIgnoreCondition(value.Value) : null;
                _shouldSerializeIsExplicitlySet = false;
            }
        }

        private Func<object, object?, bool>? _shouldSerialize;
        private bool _shouldSerializeIsExplicitlySet;
        private JsonIgnoreCondition? _ignoreCondition;

        private protected abstract Func<object, object?, bool> GetShouldSerializeForIgnoreCondition(JsonIgnoreCondition condition);

        /// <summary>
        /// Gets or sets a custom attribute provider for the current property.
        /// </summary>
        /// <remarks>
        /// When resolving metadata via <see cref="DefaultJsonTypeInfoResolver"/> this
        /// will be populated with the underlying <see cref="MemberInfo" /> of the serialized property or field.
        ///
        /// Setting a custom attribute provider will have no impact on the contract model,
        /// but serves as metadata for downstream contract modifiers.
        /// </remarks>
        public ICustomAttributeProvider? AttributeProvider
        {
            get => _attributeProvider;
            set
            {
                VerifyMutable();

                _attributeProvider = value;
            }
        }

        private ICustomAttributeProvider? _attributeProvider;
        internal string? MemberName { get; private protected set; }
        internal MemberTypes MemberType { get; private protected set; }
        internal bool IsVirtual { get; private set; }

        /// <summary>
        /// Specifies whether the current property is a special extension data property.
        /// </summary>
        /// <remarks>
        /// Properties annotated with <see cref="JsonExtensionDataAttribute"/>
        /// will appear here when using <see cref="DefaultJsonTypeInfoResolver"/> or <see cref="JsonSerializerContext"/>.
        /// </remarks>
        public bool IsExtensionData
        {
            get => _isExtensionDataProperty;
            set
            {
                VerifyMutable();

                if (value && !JsonTypeInfo.IsValidExtensionDataProperty(this))
                {
                    ThrowHelper.ThrowInvalidOperationException_SerializationDataExtensionPropertyInvalid(this);
                }

                _isExtensionDataProperty = value;
            }
        }

        private bool _isExtensionDataProperty;

        internal JsonPropertyInfo(Type declaringType, Type propertyType, JsonTypeInfo? declaringTypeInfo, JsonSerializerOptions options)
        {
            Debug.Assert(declaringTypeInfo is null || declaringTypeInfo.Type == declaringType);

            DeclaringType = declaringType;
            PropertyType = propertyType;
            ParentTypeInfo = declaringTypeInfo; // null parentTypeInfo means it's not tied yet
            Options = options;
        }

        internal static JsonPropertyInfo GetPropertyPlaceholder()
        {
            JsonPropertyInfo info = new JsonPropertyInfo<object>(typeof(object), declaringTypeInfo: null, options: null!);

            Debug.Assert(!info.IsForTypeInfo);
            Debug.Assert(!info.CanDeserialize);
            Debug.Assert(!info.CanSerialize);

            info.Name = string.Empty;

            return info;
        }

        /// <summary>
        /// Gets the type of the current property metadata.
        /// </summary>
        public Type PropertyType { get; }

        private protected void VerifyMutable()
        {
            if (_isConfigured)
            {
                ThrowHelper.ThrowInvalidOperationException_PropertyInfoImmutable();
            }
        }

        private volatile bool _isConfigured;

        internal void EnsureConfigured()
        {
            if (_isConfigured)
            {
                return;
            }

            Configure();

            _isConfigured = true;
        }

        internal void Configure()
        {
            Debug.Assert(ParentTypeInfo != null, "We should have ensured parent is assigned in JsonTypeInfo");
            Debug.Assert(!ParentTypeInfo.IsConfigured);

            DeclaringTypeNumberHandling = ParentTypeInfo.NumberHandling;

            if (!IsForTypeInfo)
            {
                CacheNameAsUtf8BytesAndEscapedNameSection();
            }

            if (IsIgnored)
            {
                return;
            }

            DetermineEffectiveConverter();

            if (IsForTypeInfo)
            {
                DetermineNumberHandlingForTypeInfo();
            }
            else
            {
                DetermineNumberHandlingForProperty();

                if (!IsIgnored)
                {
                    DetermineIgnoreCondition(IgnoreCondition);
                }

                DetermineSerializationCapabilities(IgnoreCondition);
            }
        }

        private protected abstract void DetermineEffectiveConverter();
        private protected abstract void DetermineMemberAccessors(MemberInfo memberInfo);

        private void DeterminePoliciesFromMember(MemberInfo memberInfo)
        {
            JsonPropertyOrderAttribute? orderAttr = memberInfo.GetCustomAttribute<JsonPropertyOrderAttribute>(inherit: false);
            Order = orderAttr?.Order ?? 0;

            JsonNumberHandlingAttribute? numberHandlingAttr = memberInfo.GetCustomAttribute<JsonNumberHandlingAttribute>(inherit: false);
            NumberHandling = numberHandlingAttr?.Handling;
        }

        private void DeterminePropertyNameFromMember(MemberInfo memberInfo)
        {
            JsonPropertyNameAttribute? nameAttribute = memberInfo.GetCustomAttribute<JsonPropertyNameAttribute>(inherit: false);
            string? name;
            if (nameAttribute != null)
            {
                name = nameAttribute.Name;
            }
            else if (Options.PropertyNamingPolicy != null)
            {
                name = Options.PropertyNamingPolicy.ConvertName(memberInfo.Name);
            }
            else
            {
                name = memberInfo.Name;
            }

            if (name == null)
            {
                ThrowHelper.ThrowInvalidOperationException_SerializerPropertyNameNull(this);
            }

            Name = name;
        }

        internal void CacheNameAsUtf8BytesAndEscapedNameSection()
        {
            Debug.Assert(Name != null);

            NameAsUtf8Bytes = Encoding.UTF8.GetBytes(Name);
            EscapedNameSection = JsonHelpers.GetEscapedPropertyNameSection(NameAsUtf8Bytes, Options.Encoder);
        }

        internal void DetermineSerializationCapabilities(JsonIgnoreCondition? ignoreCondition)
        {
            if (IsIgnored)
            {
                CanSerialize = false;
                CanDeserialize = false;
                return;
            }

            Debug.Assert(MemberType == MemberTypes.Property || MemberType == MemberTypes.Field || MemberType == default);

            if ((ConverterStrategy & (ConverterStrategy.Enumerable | ConverterStrategy.Dictionary)) == 0)
            {
                Debug.Assert(ignoreCondition != JsonIgnoreCondition.Always);

                // Three possible values for ignoreCondition:
                // null = JsonIgnore was not placed on this property, global IgnoreReadOnlyProperties/Fields wins
                // WhenNull = only ignore when null, global IgnoreReadOnlyProperties/Fields loses
                // Never = never ignore (always include), global IgnoreReadOnlyProperties/Fields loses
                bool serializeReadOnlyProperty = ignoreCondition != null || (MemberType == MemberTypes.Property
                    ? !Options.IgnoreReadOnlyProperties
                    : !Options.IgnoreReadOnlyFields);

                // We serialize if there is a getter + not ignoring readonly properties.
                CanSerialize = HasGetter && (HasSetter || serializeReadOnlyProperty || _shouldSerializeIsExplicitlySet);

                // We deserialize if there is a setter.
                CanDeserialize = HasSetter;
            }
            else
            {
                if (HasGetter)
                {
                    Debug.Assert(EffectiveConverter != null);

                    CanSerialize = true;

                    if (HasSetter)
                    {
                        CanDeserialize = true;
                    }
                }
            }
        }

        internal void DetermineIgnoreCondition(JsonIgnoreCondition? ignoreCondition)
        {
            if (_shouldSerializeIsExplicitlySet)
            {
                Debug.Assert(ignoreCondition == null);
#pragma warning disable SYSLIB0020 // JsonSerializerOptions.IgnoreNullValues is obsolete
                if (Options.IgnoreNullValues)
#pragma warning restore SYSLIB0020
                {
                    Debug.Assert(Options.DefaultIgnoreCondition == JsonIgnoreCondition.Never);
                    if (PropertyTypeCanBeNull)
                    {
                        IgnoreDefaultValuesOnRead = true;
                    }
                }

                return;
            }

            if (ignoreCondition != null)
            {
                // This is not true for CodeGen scenarios since we do not cache this as of yet.
                // Debug.Assert(MemberInfo != null);
                Debug.Assert(ignoreCondition != JsonIgnoreCondition.Always);

                if (ignoreCondition == JsonIgnoreCondition.WhenWritingDefault)
                {
                    IgnoreDefaultValuesOnWrite = true;
                }
                else if (ignoreCondition == JsonIgnoreCondition.WhenWritingNull)
                {
                    if (PropertyTypeCanBeNull)
                    {
                        IgnoreDefaultValuesOnWrite = true;
                    }
                    else
                    {
                        ThrowHelper.ThrowInvalidOperationException_IgnoreConditionOnValueTypeInvalid(MemberName!, DeclaringType);
                    }
                }
            }
#pragma warning disable SYSLIB0020 // JsonSerializerOptions.IgnoreNullValues is obsolete
            else if (Options.IgnoreNullValues)
            {
                Debug.Assert(Options.DefaultIgnoreCondition == JsonIgnoreCondition.Never);
                if (PropertyTypeCanBeNull)
                {
                    IgnoreDefaultValuesOnRead = true;
                    IgnoreDefaultValuesOnWrite = true;
                }
            }
            else if (Options.DefaultIgnoreCondition == JsonIgnoreCondition.WhenWritingNull)
            {
                Debug.Assert(!Options.IgnoreNullValues);
                if (PropertyTypeCanBeNull)
                {
                    IgnoreDefaultValuesOnWrite = true;
                }
            }
            else if (Options.DefaultIgnoreCondition == JsonIgnoreCondition.WhenWritingDefault)
            {
                Debug.Assert(!Options.IgnoreNullValues);
                IgnoreDefaultValuesOnWrite = true;
            }
#pragma warning restore SYSLIB0020
        }

        internal void DetermineNumberHandlingForTypeInfo()
        {
            if (DeclaringTypeNumberHandling != null && DeclaringTypeNumberHandling != JsonNumberHandling.Strict && !EffectiveConverter.IsInternalConverter)
            {
                ThrowHelper.ThrowInvalidOperationException_NumberHandlingOnPropertyInvalid(this);
            }

            if (NumberHandingIsApplicable())
            {
                // This logic is to honor JsonNumberHandlingAttribute placed on
                // custom collections e.g. public class MyNumberList : List<int>.

                // Priority 1: Get handling from the type (parent type in this case is the type itself).
                EffectiveNumberHandling = DeclaringTypeNumberHandling;

                // Priority 2: Get handling from JsonSerializerOptions instance.
                if (!EffectiveNumberHandling.HasValue && Options.NumberHandling != JsonNumberHandling.Strict)
                {
                    EffectiveNumberHandling = Options.NumberHandling;
                }
            }
        }

        internal void DetermineNumberHandlingForProperty()
        {
            bool numberHandlingIsApplicable = NumberHandingIsApplicable();

            if (numberHandlingIsApplicable)
            {
                // Priority 1: Get handling from attribute on property/field, its parent class type or property type.
                JsonNumberHandling? handling = NumberHandling ?? DeclaringTypeNumberHandling ?? JsonTypeInfo.NumberHandling;

                // Priority 2: Get handling from JsonSerializerOptions instance.
                if (!handling.HasValue && Options.NumberHandling != JsonNumberHandling.Strict)
                {
                    handling = Options.NumberHandling;
                }

                EffectiveNumberHandling = handling;
            }
            else if (NumberHandling.HasValue && NumberHandling != JsonNumberHandling.Strict)
            {
                ThrowHelper.ThrowInvalidOperationException_NumberHandlingOnPropertyInvalid(this);
            }
        }

        private bool NumberHandingIsApplicable()
        {
            if (EffectiveConverter.IsInternalConverterForNumberType)
            {
                return true;
            }

            Type potentialNumberType;
            if (!EffectiveConverter.IsInternalConverter ||
                ((ConverterStrategy.Enumerable | ConverterStrategy.Dictionary) & ConverterStrategy) == 0)
            {
                potentialNumberType = PropertyType;
            }
            else
            {
                Debug.Assert(EffectiveConverter.ElementType != null);
                potentialNumberType = EffectiveConverter.ElementType;
            }

            potentialNumberType = Nullable.GetUnderlyingType(potentialNumberType) ?? potentialNumberType;

            return potentialNumberType == typeof(byte) ||
                potentialNumberType == typeof(decimal) ||
                potentialNumberType == typeof(double) ||
                potentialNumberType == typeof(short) ||
                potentialNumberType == typeof(int) ||
                potentialNumberType == typeof(long) ||
                potentialNumberType == typeof(sbyte) ||
                potentialNumberType == typeof(float) ||
                potentialNumberType == typeof(ushort) ||
                potentialNumberType == typeof(uint) ||
                potentialNumberType == typeof(ulong) ||
                potentialNumberType == JsonTypeInfo.ObjectType;
        }

        internal abstract bool GetMemberAndWriteJson(object obj, ref WriteStack state, Utf8JsonWriter writer);
        internal abstract bool GetMemberAndWriteJsonExtensionData(object obj, ref WriteStack state, Utf8JsonWriter writer);

        internal abstract object? GetValueAsObject(object obj);

#if DEBUG
        internal string GetDebugInfo(int indent = 0)
        {
            string ind = new string(' ', indent);
            StringBuilder sb = new();

            sb.AppendLine($"{ind}{{");
            sb.AppendLine($"{ind}  Name: {Name},");
            sb.AppendLine($"{ind}  NameAsUtf8.Length: {(NameAsUtf8Bytes?.Length ?? -1)},");
            sb.AppendLine($"{ind}  IsConfigured: {_isConfigured},");
            sb.AppendLine($"{ind}  IsIgnored: {IsIgnored},");
            sb.AppendLine($"{ind}  CanSerialize: {CanSerialize},");
            sb.AppendLine($"{ind}  CanDeserialize: {CanDeserialize},");
            sb.AppendLine($"{ind}}}");

            return sb.ToString();
        }
#endif

        internal bool HasGetter => _untypedGet is not null;
        internal bool HasSetter => _untypedSet is not null;

        internal void InitializeUsingMemberReflection(MemberInfo memberInfo)
        {
            Debug.Assert(AttributeProvider == null);

            switch (AttributeProvider = memberInfo)
            {
                case PropertyInfo propertyInfo:
                    {
                        MemberName = propertyInfo.Name;
                        IsVirtual = propertyInfo.IsVirtual();
                        MemberType = MemberTypes.Property;
                        break;
                    }
                case FieldInfo fieldInfo:
                    {
                        MemberName = fieldInfo.Name;
                        MemberType = MemberTypes.Field;
                        break;
                    }
                default:
                    Debug.Fail("Only FieldInfo and PropertyInfo members are supported.");
                    break;
            }

            DeterminePoliciesFromMember(memberInfo);
            DeterminePropertyNameFromMember(memberInfo);

            if (!IsIgnored)
            {
                DetermineMemberAccessors(memberInfo);
            }

            IsExtensionData = memberInfo.GetCustomAttribute<JsonExtensionDataAttribute>(inherit: false) != null;
        }

        internal bool IgnoreDefaultValuesOnRead { get; private set; }
        internal bool IgnoreDefaultValuesOnWrite { get; private set; }

        /// <summary>
        /// True if the corresponding cref="JsonTypeInfo.PropertyInfoForTypeInfo"/> is this instance.
        /// </summary>
        internal bool IsForTypeInfo { get; set; }

        // There are 3 copies of the property name:
        // 1) Name. The unescaped property name.
        // 2) NameAsUtf8Bytes. The Utf8 version of Name. Used during during deserialization for property lookup.
        // 3) EscapedNameSection. The escaped version of NameAsUtf8Bytes plus the wrapping quotes and a trailing colon. Used during serialization.

        /// <summary>
        /// Gets or sets the JSON property name used when serializing the property.
        /// </summary>
        /// <remarks>
        /// This typically reflects the underlying .NET member name,
        /// the name derived from <see cref="JsonSerializerOptions.PropertyNamingPolicy" />,
        /// or the value specified in <see cref="JsonPropertyNameAttribute" />,
        /// </remarks>
        public string Name
        {
            get => _name;
            set
            {
                VerifyMutable();

                if (value == null)
                {
                    ThrowHelper.ThrowArgumentNullException(nameof(value));
                }

                _name = value;
            }
        }

        private string _name = null!;

        /// <summary>
        /// Utf8 version of Name.
        /// </summary>
        internal byte[] NameAsUtf8Bytes { get; set; } = null!;

        /// <summary>
        /// The escaped name passed to the writer.
        /// </summary>
        internal byte[] EscapedNameSection { get; set; } = null!;

        /// <summary>
        /// Options associated with JsonPropertyInfo
        /// </summary>
        public JsonSerializerOptions Options { get; }

        /// <summary>
        /// Gets or sets the serialization order for the current property.
        /// </summary>
        /// <remarks>
        /// When using <see cref="DefaultJsonTypeInfoResolver"/>, properties annotated
        /// with the <see cref="JsonPropertyOrderAttribute"/> will map to this value.
        /// </remarks>
        public int Order
        {
            get => _order;
            set
            {
                VerifyMutable();
                _order = value;
            }
        }

        private int _order;

        internal bool ReadJsonAndAddExtensionProperty(
            object obj,
            ref ReadStack state,
            ref Utf8JsonReader reader)
        {
            object propValue = GetValueAsObject(obj)!;

            if (propValue is IDictionary<string, object?> dictionaryObjectValue)
            {
                if (reader.TokenType == JsonTokenType.Null)
                {
                    // A null JSON value is treated as a null object reference.
                    dictionaryObjectValue[state.Current.JsonPropertyNameAsString!] = null;
                }
                else
                {
                    JsonConverter<object> converter = (JsonConverter<object>)GetDictionaryValueConverter(JsonTypeInfo.ObjectType);
                    object value = converter.Read(ref reader, JsonTypeInfo.ObjectType, Options)!;
                    dictionaryObjectValue[state.Current.JsonPropertyNameAsString!] = value;
                }
            }
            else if (propValue is IDictionary<string, JsonElement> dictionaryElementValue)
            {
                Type elementType = typeof(JsonElement);
                JsonConverter<JsonElement> converter = (JsonConverter<JsonElement>)GetDictionaryValueConverter(elementType);
                JsonElement value = converter.Read(ref reader, elementType, Options);
                dictionaryElementValue[state.Current.JsonPropertyNameAsString!] = value;
            }
            else
            {
                // Avoid a type reference to JsonObject and its converter to support trimming.
                Debug.Assert(propValue is Nodes.JsonObject);
                EffectiveConverter.ReadElementAndSetProperty(propValue, state.Current.JsonPropertyNameAsString!, ref reader, Options, ref state);
            }

            return true;

            JsonConverter GetDictionaryValueConverter(Type dictionaryValueType)
            {
                JsonConverter converter;
                JsonTypeInfo? dictionaryValueInfo = JsonTypeInfo.ElementTypeInfo;
                if (dictionaryValueInfo != null)
                {
                    // Fast path when there is a generic type such as Dictionary<,>.
                    converter = dictionaryValueInfo.Converter;
                }
                else
                {
                    // Slower path for non-generic types that implement IDictionary<,>.
                    // It is possible to cache this converter on JsonTypeInfo if we assume the property value
                    // will always be the same type for all instances.
                    converter = Options.GetConverterFromTypeInfo(dictionaryValueType);
                }

                Debug.Assert(converter != null);
                return converter;
            }
        }

        internal abstract bool ReadJsonAndSetMember(object obj, ref ReadStack state, ref Utf8JsonReader reader);

        internal abstract bool ReadJsonAsObject(ref ReadStack state, ref Utf8JsonReader reader, out object? value);

        internal bool ReadJsonExtensionDataValue(ref ReadStack state, ref Utf8JsonReader reader, out object? value)
        {
            Debug.Assert(this == state.Current.JsonTypeInfo.ExtensionDataProperty);

            if (JsonTypeInfo.ElementType == JsonTypeInfo.ObjectType && reader.TokenType == JsonTokenType.Null)
            {
                value = null;
                return true;
            }

            JsonConverter<JsonElement> converter = (JsonConverter<JsonElement>)Options.GetConverterFromTypeInfo(typeof(JsonElement));
            if (!converter.TryRead(ref reader, typeof(JsonElement), Options, ref state, out JsonElement jsonElement))
            {
                // JsonElement is a struct that must be read in full.
                value = null;
                return false;
            }

            value = jsonElement;
            return true;
        }

        internal void EnsureChildOf(JsonTypeInfo parent)
        {
            if (ParentTypeInfo == null)
            {
                ParentTypeInfo = parent;
            }
            else if (ParentTypeInfo != parent)
            {
                ThrowHelper.ThrowInvalidOperationException_JsonPropertyInfoIsBoundToDifferentJsonTypeInfo(this);
            }
        }

        internal Type DeclaringType { get; }

        [AllowNull]
        internal JsonTypeInfo JsonTypeInfo
        {
            get
            {
                if (_jsonTypeInfo != null)
                {
                    // We should not call it on set as it's usually called during initialization
                    // which is too early to `lock` the JsonTypeInfo
                    // If this property ever becomes public we should move this to callsites
                    _jsonTypeInfo.EnsureConfigured();
                }
                else
                {
                    // GetOrAddJsonTypeInfo already ensures it's configured.
                    _jsonTypeInfo = Options.GetTypeInfoCached(PropertyType);
                }

                return _jsonTypeInfo;
            }
            set
            {
                // Used by JsonMetadataServices.
                // This could potentially be double initialized
                Debug.Assert(_jsonTypeInfo == null || _jsonTypeInfo == value);
                _jsonTypeInfo = value;
            }
        }

        internal abstract void SetExtensionDictionaryAsObject(object obj, object? extensionDict);

        internal bool CanSerialize { get; private set; }

        internal bool CanDeserialize { get; private set; }

        internal bool IsIgnored => _ignoreCondition == JsonIgnoreCondition.Always;

        /// <summary>
        /// Relevant to source generated metadata: did the property have the <see cref="JsonIncludeAttribute"/>?
        /// </summary>
        internal bool SrcGen_HasJsonInclude { get; set; }

        /// <summary>
        /// Relevant to source generated metadata: is the property public?
        /// </summary>
        internal bool SrcGen_IsPublic { get; set; }

        /// <summary>
        /// Number handling for declaring type
        /// </summary>
        internal JsonNumberHandling? DeclaringTypeNumberHandling { get; set; }

        /// <summary>
        /// Number handling specific to this property, i.e. set by attribute
        /// </summary>
        public JsonNumberHandling? NumberHandling
        {
            get => _numberHandling;
            set
            {
                VerifyMutable();
                _numberHandling = value;
            }
        }

        private JsonNumberHandling? _numberHandling;

        /// <summary>
        /// Number handling after considering options and declaring type number handling
        /// </summary>
        internal JsonNumberHandling? EffectiveNumberHandling { get; set; }

        //  Whether the property type can be null.
        internal abstract bool PropertyTypeCanBeNull { get; }

        /// <summary>
        /// Default value used for parameterized ctor invocation.
        /// </summary>
        internal abstract object? DefaultValue { get; }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private string DebuggerDisplay => $"MemberInfo={AttributeProvider as MemberInfo}";
    }
}
