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
    /// Provides JSON serialization-related metadata about a property or field defined in an object.
    /// </summary>
    [DebuggerDisplay("{DebuggerDisplay,nq}")]
    public abstract class JsonPropertyInfo
    {
        internal static readonly JsonPropertyInfo s_missingProperty = GetPropertyPlaceholder();

        internal JsonTypeInfo? ParentTypeInfo { get; private set; }
        private JsonTypeInfo? _jsonTypeInfo;

        internal ConverterStrategy ConverterStrategy { get; private protected set; }

        /// <summary>
        /// Converter after applying CustomConverter (i.e. JsonConverterAttribute)
        /// </summary>
        internal JsonConverter EffectiveConverter
        {
            get
            {
                Debug.Assert(_effectiveConverter != null);
                return _effectiveConverter;
            }
        }

        private protected JsonConverter? _effectiveConverter;

        /// <summary>
        /// Gets or sets a custom converter override for the current property.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// The <see cref="JsonPropertyInfo"/> instance has been locked for further modification.
        /// </exception>
        /// <remarks>
        /// It is possible to use <see cref="JsonConverterFactory"/> instances with this property.
        ///
        /// For contracts originating from <see cref="DefaultJsonTypeInfoResolver"/>, the value of
        /// <see cref="CustomConverter"/> will be mapped from <see cref="JsonConverterAttribute" /> annotations.
        /// </remarks>
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
        /// <exception cref="InvalidOperationException">
        /// The <see cref="JsonPropertyInfo"/> instance has been locked for further modification.
        /// </exception>
        /// <remarks>
        /// Setting to <see langword="null"/> will result in the property being skipped on serialization.
        /// </remarks>
        public Func<object, object?>? Get
        {
            get => _untypedGet;
            set
            {
                VerifyMutable();
                SetGetter(value);
            }
        }

        /// <summary>
        /// Gets or sets a setter delegate for the property.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// The <see cref="JsonPropertyInfo"/> instance has been locked for further modification.
        /// </exception>
        /// <remarks>
        /// Setting to <see langword="null"/> will result in the property being skipped on deserialization.
        /// </remarks>
        public Action<object, object?>? Set
        {
            get => _untypedSet;
            set
            {
                VerifyMutable();
                SetSetter(value);
                _isUserSpecifiedSetter = true;
            }
        }

        private protected Func<object, object?>? _untypedGet;
        private protected Action<object, object?>? _untypedSet;
        private bool _isUserSpecifiedSetter;

        private protected abstract void SetGetter(Delegate? getter);
        private protected abstract void SetSetter(Delegate? setter);

        /// <summary>
        /// Gets or sets a predicate deciding whether the current property value should be serialized.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// The <see cref="JsonPropertyInfo"/> instance has been locked for further modification.
        /// </exception>
        /// <remarks>
        /// The first parameter denotes the parent object, the second parameter denotes the property value.
        ///
        /// Setting the predicate to <see langword="null"/> is equivalent to always serializing the property value.
        ///
        /// For contracts originating from <see cref="DefaultJsonTypeInfoResolver"/>,
        /// the value of <see cref="JsonIgnoreAttribute.Condition"/> will map to this predicate.
        /// </remarks>
        public Func<object, object?, bool>? ShouldSerialize
        {
            get => _shouldSerialize;
            set
            {
                VerifyMutable();
                SetShouldSerialize(value);
                // Invalidate any JsonIgnore configuration if delegate set manually by user
                _isUserSpecifiedShouldSerialize = true;
                IgnoreDefaultValuesOnWrite = false;
            }
        }

        private protected Func<object, object?, bool>? _shouldSerialize;
        private bool _isUserSpecifiedShouldSerialize;
        private protected abstract void SetShouldSerialize(Delegate? predicate);

        internal JsonIgnoreCondition? IgnoreCondition
        {
            get => _ignoreCondition;
            set
            {
                Debug.Assert(!_isConfigured);
                ConfigureIgnoreCondition(value);
                _ignoreCondition = value;
            }
        }

        private JsonIgnoreCondition? _ignoreCondition;
        private protected abstract void ConfigureIgnoreCondition(JsonIgnoreCondition? ignoreCondition);

        /// <summary>
        /// Gets or sets a custom attribute provider for the current property.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// The <see cref="JsonPropertyInfo"/> instance has been locked for further modification.
        /// </exception>
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
        /// <exception cref="InvalidOperationException">
        /// The <see cref="JsonPropertyInfo"/> instance has been locked for further modification.
        ///
        /// -or-
        ///
        /// The current <see cref="PropertyType"/> is not valid for use with extension data.
        /// </exception>
        /// <remarks>
        /// For contracts originating from <see cref="DefaultJsonTypeInfoResolver"/> or <see cref="JsonSerializerContext"/>,
        /// the value of this property will be mapped from <see cref="JsonExtensionDataAttribute"/> annotations.
        /// </remarks>
        public bool IsExtensionData
        {
            get => _isExtensionDataProperty;
            set
            {
                VerifyMutable();

                if (value && !JsonTypeInfo.IsValidExtensionDataProperty(PropertyType))
                {
                    ThrowHelper.ThrowInvalidOperationException_SerializationDataExtensionPropertyInvalid(this);
                }

                _isExtensionDataProperty = value;
            }
        }

        private bool _isExtensionDataProperty;

        /// <summary>
        /// Specifies whether the current property is required for deserialization to be successful.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// The <see cref="JsonPropertyInfo"/> instance has been locked for further modification.
        /// </exception>
        /// <remarks>
        /// For contracts originating from <see cref="DefaultJsonTypeInfoResolver"/> or <see cref="JsonSerializerContext"/>,
        /// the value of this property will be mapped from <see cref="JsonRequiredAttribute"/> annotations.
        ///
        /// For contracts using <see cref="DefaultJsonTypeInfoResolver"/>, properties using the <see langword="required"/> keyword
        /// will also map to this setting, unless deserialization uses a SetsRequiredMembersAttribute on a constructor that populates all required properties.
        /// <see langword="required"/> keyword is currently not supported in <see cref="JsonSerializerContext"/> contracts.
        /// </remarks>
        public bool IsRequired
        {
            get => _isRequired;
            set
            {
                VerifyMutable();
                _isRequired = value;
            }
        }

        private bool _isRequired;

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
            Debug.Assert(!info.CanSerialize);
            Debug.Assert(!info.CanDeserialize);

            info.Name = string.Empty;

            return info;
        }

        /// <summary>
        /// Gets the type of the current property.
        /// </summary>
        public Type PropertyType { get; }

        private protected void VerifyMutable()
        {
            if (_isConfigured)
            {
                ThrowHelper.ThrowInvalidOperationException_PropertyInfoImmutable();
            }
        }

        internal bool IsConfigured => _isConfigured;
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

            _jsonTypeInfo ??= Options.GetTypeInfoInternal(PropertyType, ensureConfigured: false);
            DetermineEffectiveConverter(_jsonTypeInfo);

            if (IsForTypeInfo)
            {
                DetermineNumberHandlingForTypeInfo();
            }
            else
            {
                DetermineNumberHandlingForProperty();
                DetermineIgnoreCondition();
                DetermineSerializationCapabilities();
            }

            if (IsRequired)
            {
                if (!CanDeserialize)
                {
                    ThrowHelper.ThrowInvalidOperationException_JsonPropertyRequiredAndNotDeserializable(this);
                }

                if (IsExtensionData)
                {
                    ThrowHelper.ThrowInvalidOperationException_JsonPropertyRequiredAndExtensionData(this);
                }

                Debug.Assert(!IgnoreNullTokensOnRead);
            }
        }

        private protected abstract void DetermineEffectiveConverter(JsonTypeInfo jsonTypeInfo);
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

        private void CacheNameAsUtf8BytesAndEscapedNameSection()
        {
            Debug.Assert(Name != null);

            NameAsUtf8Bytes = Encoding.UTF8.GetBytes(Name);
            EscapedNameSection = JsonHelpers.GetEscapedPropertyNameSection(NameAsUtf8Bytes, Options.Encoder);
        }

        private void DetermineIgnoreCondition()
        {
            if (_ignoreCondition != null)
            {
                // Do not apply global policy if already configured on the property level.
                return;
            }

#pragma warning disable SYSLIB0020 // JsonSerializerOptions.IgnoreNullValues is obsolete
            if (Options.IgnoreNullValues)
#pragma warning restore SYSLIB0020
            {
                Debug.Assert(Options.DefaultIgnoreCondition == JsonIgnoreCondition.Never);
                if (PropertyTypeCanBeNull)
                {
                    IgnoreNullTokensOnRead = !_isUserSpecifiedSetter && !IsRequired;
                    IgnoreDefaultValuesOnWrite = ShouldSerialize is null;
                }
            }
            else if (Options.DefaultIgnoreCondition == JsonIgnoreCondition.WhenWritingNull)
            {
                if (PropertyTypeCanBeNull)
                {
                    IgnoreDefaultValuesOnWrite = ShouldSerialize is null;
                }
            }
            else if (Options.DefaultIgnoreCondition == JsonIgnoreCondition.WhenWritingDefault)
            {
                IgnoreDefaultValuesOnWrite = ShouldSerialize is null;
            }
        }

        private void DetermineSerializationCapabilities()
        {
            Debug.Assert(EffectiveConverter != null, "Must have calculated the effective converter.");

            CanSerialize = HasGetter;
            CanDeserialize = HasSetter;

            Debug.Assert(MemberType is 0 or MemberTypes.Field or MemberTypes.Property);
            if (MemberType == 0 || _ignoreCondition != null)
            {
                // No policy to be applied if either:
                // 1. JsonPropertyInfo is a custom instance (not generated via reflection or sourcegen).
                // 2. A JsonIgnoreCondition has been specified on the property level.
                return;
            }

            if ((ConverterStrategy & (ConverterStrategy.Enumerable | ConverterStrategy.Dictionary)) != 0)
            {
                // Properties of collections types that only have setters are not supported.
                if (Get == null && Set != null && !_isUserSpecifiedSetter)
                {
                    CanDeserialize = false;
                }
            }
            else
            {
                // For read-only properties of non-collection types, apply IgnoreReadOnlyProperties/Fields policy,
                // unless a `ShouldSerialize` predicate has been explicitly applied by the user (null or non-null).
                if (Get != null && Set == null && IgnoreReadOnlyMember && !_isUserSpecifiedShouldSerialize)
                {
                    CanSerialize = false;
                }
            }
        }

        private void DetermineNumberHandlingForTypeInfo()
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

        private void DetermineNumberHandlingForProperty()
        {
            Debug.Assert(!IsConfigured, "Should not be called post-configuration.");
            Debug.Assert(_jsonTypeInfo != null, "Must have already been determined on configuration.");

            bool numberHandlingIsApplicable = NumberHandingIsApplicable();

            if (numberHandlingIsApplicable)
            {
                // Priority 1: Get handling from attribute on property/field, its parent class type or property type.
                JsonNumberHandling? handling = NumberHandling ?? DeclaringTypeNumberHandling ?? _jsonTypeInfo.NumberHandling;

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

        private void DetermineIsRequired(MemberInfo memberInfo, bool shouldCheckForRequiredKeyword)
        {
            IsRequired = memberInfo.GetCustomAttribute<JsonRequiredAttribute>(inherit: false) != null
                || (shouldCheckForRequiredKeyword && memberInfo.HasRequiredMemberAttribute());
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

        internal void InitializeUsingMemberReflection(MemberInfo memberInfo, JsonConverter? customConverter, JsonIgnoreCondition? ignoreCondition, bool shouldCheckForRequiredKeyword)
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

            CustomConverter = customConverter;
            DeterminePoliciesFromMember(memberInfo);
            DeterminePropertyNameFromMember(memberInfo);
            DetermineIsRequired(memberInfo, shouldCheckForRequiredKeyword);

            if (ignoreCondition != JsonIgnoreCondition.Always)
            {
                DetermineMemberAccessors(memberInfo);
            }

            IgnoreCondition = ignoreCondition;
            IsExtensionData = memberInfo.GetCustomAttribute<JsonExtensionDataAttribute>(inherit: false) != null;
        }

        internal bool IgnoreNullTokensOnRead { get; private protected set; }
        internal bool IgnoreDefaultValuesOnWrite { get; private protected set; }

        internal bool IgnoreReadOnlyMember
        {
            get
            {
                Debug.Assert(MemberType == MemberTypes.Property || MemberType == MemberTypes.Field || MemberType == default);
                return MemberType switch
                {
                    MemberTypes.Property => Options.IgnoreReadOnlyProperties,
                    MemberTypes.Field => Options.IgnoreReadOnlyFields,
                    _ => false,
                };
            }
        }

        /// <summary>
        /// True if the corresponding cref="JsonTypeInfo.PropertyInfoForTypeInfo"/> is this instance.
        /// </summary>
        internal bool IsForTypeInfo { get; set; }

        // There are 3 copies of the property name:
        // 1) Name. The unescaped property name.
        // 2) NameAsUtf8Bytes. The Utf8 version of Name. Used during deserialization for property lookup.
        // 3) EscapedNameSection. The escaped version of NameAsUtf8Bytes plus the wrapping quotes and a trailing colon. Used during serialization.

        /// <summary>
        /// Gets or sets the JSON property name used when serializing the property.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="value"/> is null.</exception>
        /// <exception cref="InvalidOperationException">
        /// The <see cref="JsonPropertyInfo"/> instance has been locked for further modification.
        /// </exception>
        /// <remarks>
        /// The value of <see cref="Name"/> cannot conflict with that of other <see cref="JsonPropertyInfo"/> defined in the declaring <see cref="JsonTypeInfo"/>.
        ///
        /// For contracts originating from <see cref="DefaultJsonTypeInfoResolver"/> or <see cref="JsonSerializerContext"/>,
        /// the value typically reflects the underlying .NET member name, the name derived from <see cref="JsonSerializerOptions.PropertyNamingPolicy" />,
        /// or the value specified in <see cref="JsonPropertyNameAttribute" />.
        /// </remarks>
        public string Name
        {
            get
            {
                Debug.Assert(_name != null);
                return _name;
            }
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

        private string? _name;

        /// <summary>
        /// Utf8 version of Name.
        /// </summary>
        internal byte[] NameAsUtf8Bytes { get; set; } = null!;

        /// <summary>
        /// The escaped name passed to the writer.
        /// </summary>
        internal byte[] EscapedNameSection { get; set; } = null!;

        /// <summary>
        /// Gets the <see cref="JsonSerializerOptions"/> value associated with the current contract instance.
        /// </summary>
        public JsonSerializerOptions Options { get; }

        /// <summary>
        /// Gets or sets the serialization order for the current property.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// The <see cref="JsonPropertyInfo"/> instance has been locked for further modification.
        /// </exception>
        /// <remarks>
        /// For contracts originating from <see cref="DefaultJsonTypeInfoResolver"/> or <see cref="JsonSerializerContext"/>,
        /// the value of this property will be mapped from <see cref="JsonPropertyOrderAttribute"/> annotations.
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
                    JsonConverter<object> converter = GetDictionaryValueConverter<object>();
                    object value = converter.Read(ref reader, JsonTypeInfo.ObjectType, Options)!;
                    dictionaryObjectValue[state.Current.JsonPropertyNameAsString!] = value;
                }
            }
            else if (propValue is IDictionary<string, JsonElement> dictionaryElementValue)
            {
                JsonConverter<JsonElement> converter = GetDictionaryValueConverter<JsonElement>();
                JsonElement value = converter.Read(ref reader, typeof(JsonElement), Options);
                dictionaryElementValue[state.Current.JsonPropertyNameAsString!] = value;
            }
            else
            {
                // Avoid a type reference to JsonObject and its converter to support trimming.
                Debug.Assert(propValue is Nodes.JsonObject);
                EffectiveConverter.ReadElementAndSetProperty(propValue, state.Current.JsonPropertyNameAsString!, ref reader, Options, ref state);
            }

            return true;

            JsonConverter<TValue> GetDictionaryValueConverter<TValue>()
            {
                JsonTypeInfo dictionaryValueInfo =
                    JsonTypeInfo.ElementTypeInfo
                    // Slower path for non-generic types that implement IDictionary<,>.
                    // It is possible to cache this converter on JsonTypeInfo if we assume the property value
                    // will always be the same type for all instances.
                    ?? Options.GetTypeInfoInternal(typeof(TValue));

                Debug.Assert(dictionaryValueInfo is JsonTypeInfo<TValue>);
                return ((JsonTypeInfo<TValue>)dictionaryValueInfo).EffectiveConverter;
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

            JsonConverter<JsonElement> converter = (JsonConverter<JsonElement>)Options.GetConverterInternal(typeof(JsonElement));
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
                Debug.Assert(IsConfigured);
                Debug.Assert(_jsonTypeInfo != null);
                _jsonTypeInfo.EnsureConfigured();
                return _jsonTypeInfo;
            }
            set
            {
                // This could potentially be double initialized
                Debug.Assert(_jsonTypeInfo == null || _jsonTypeInfo == value);
                // Ensure the right strategy is surfaced in PropertyInfoForTypeInfo early
                ConverterStrategy = value?.Converter.ConverterStrategy ?? default;
                _jsonTypeInfo = value;
            }
        }

        internal bool IsIgnored => _ignoreCondition == JsonIgnoreCondition.Always;

        /// <summary>
        /// Reflects the value of <see cref="HasGetter"/> combined with any additional global ignore policies.
        /// </summary>
        internal bool CanSerialize { get; private set; }
        /// <summary>
        /// Reflects the value of <see cref="HasSetter"/> combined with any additional global ignore policies.
        /// </summary>
        internal bool CanDeserialize { get; private set; }

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
        /// Gets or sets the <see cref="JsonNumberHandling"/> applied to the current property.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// The <see cref="JsonPropertyInfo"/> instance has been locked for further modification.
        /// </exception>
        /// <remarks>
        /// For contracts originating from <see cref="DefaultJsonTypeInfoResolver"/> or <see cref="JsonSerializerContext"/>,
        /// the value of this property will be mapped from <see cref="JsonNumberHandlingAttribute"/> annotations.
        /// </remarks>
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

        /// <summary>
        /// Required property index on the list of JsonTypeInfo properties.
        /// It is used as a unique identifier for required properties.
        /// It is set just before property is configured and does not change afterward.
        /// It is not equivalent to index on the properties list
        /// </summary>
        internal int RequiredPropertyIndex
        {
            get
            {
                Debug.Assert(_isConfigured);
                Debug.Assert(IsRequired);
                return _index;
            }
            set
            {
                Debug.Assert(!_isConfigured);
                _index = value;
            }
        }

        private int _index;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private string DebuggerDisplay => $"PropertyType = {PropertyType}, Name = {Name}, DeclaringType = {DeclaringType}";
    }
}
