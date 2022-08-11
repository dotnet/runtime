// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Text.Json.Reflection;
using System.Threading;

namespace System.Text.Json.Serialization.Metadata
{
    /// <summary>
    /// Provides JSON serialization-related metadata about a type.
    /// </summary>
    [DebuggerDisplay("{DebuggerDisplay,nq}")]
    public abstract partial class JsonTypeInfo
    {
        internal const string MetadataFactoryRequiresUnreferencedCode = "JSON serialization and deserialization might require types that cannot be statically analyzed and might need runtime code generation. Use System.Text.Json source generation for native AOT applications.";

        internal const string JsonObjectTypeName = "System.Text.Json.Nodes.JsonObject";

        internal delegate T ParameterizedConstructorDelegate<T, TArg0, TArg1, TArg2, TArg3>(TArg0 arg0, TArg1 arg1, TArg2 arg2, TArg3 arg3);

        private JsonPropertyInfoList? _properties;

        /// <summary>
        /// Indices of required properties.
        /// </summary>
        internal int NumberOfRequiredProperties { get; private set; }

        private Action<object>? _onSerializing;
        private Action<object>? _onSerialized;
        private Action<object>? _onDeserializing;
        private Action<object>? _onDeserialized;

        /// <summary>
        /// Gets or sets a parameterless factory to be used on deserialization.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// The <see cref="JsonTypeInfo"/> instance has been locked for further modification.
        ///
        /// -or-
        ///
        /// A parameterless factory is not supported for the current metadata <see cref="Kind"/>.
        /// </exception>
        /// <remarks>
        /// If set to <see langword="null" />, any attempt to deserialize instances of the given type will result in an exception.
        ///
        /// For contracts originating from <see cref="DefaultJsonTypeInfoResolver"/> or <see cref="JsonSerializerContext"/>,
        /// types with a single default constructor or default constructors annotated with <see cref="JsonConstructorAttribute"/>
        /// will be mapped to this delegate.
        /// </remarks>
        public Func<object>? CreateObject
        {
            get => _createObject;
            set
            {
                SetCreateObject(value);
            }
        }

        private protected abstract void SetCreateObject(Delegate? createObject);
        private protected Func<object>? _createObject;

        internal Func<object>? CreateObjectForExtensionDataProperty { get; set; }

        /// <summary>
        /// Gets or sets a callback to be invoked before serialization occurs.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// The <see cref="JsonTypeInfo"/> instance has been locked for further modification.
        ///
        /// -or-
        ///
        /// Serialization callbacks are only supported for <see cref="JsonTypeInfoKind.Object"/> metadata.
        /// </exception>
        /// <remarks>
        /// For contracts originating from <see cref="DefaultJsonTypeInfoResolver"/> or <see cref="JsonSerializerContext"/>,
        /// the value of this callback will be mapped from any <see cref="IJsonOnSerializing"/> implementation on the type.
        /// </remarks>
        public Action<object>? OnSerializing
        {
            get => _onSerializing;
            set
            {
                VerifyMutable();

                if (Kind != JsonTypeInfoKind.Object)
                {
                    ThrowHelper.ThrowInvalidOperationException_JsonTypeInfoOperationNotPossibleForKind(Kind);
                }

                _onSerializing = value;
            }
        }

        /// <summary>
        /// Gets or sets a callback to be invoked after serialization occurs.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// The <see cref="JsonTypeInfo"/> instance has been locked for further modification.
        ///
        /// -or-
        ///
        /// Serialization callbacks are only supported for <see cref="JsonTypeInfoKind.Object"/> metadata.
        /// </exception>
        /// <remarks>
        /// For contracts originating from <see cref="DefaultJsonTypeInfoResolver"/> or <see cref="JsonSerializerContext"/>,
        /// the value of this callback will be mapped from any <see cref="IJsonOnSerialized"/> implementation on the type.
        /// </remarks>
        public Action<object>? OnSerialized
        {
            get => _onSerialized;
            set
            {
                VerifyMutable();

                if (Kind != JsonTypeInfoKind.Object)
                {
                    ThrowHelper.ThrowInvalidOperationException_JsonTypeInfoOperationNotPossibleForKind(Kind);
                }

                _onSerialized = value;
            }
        }

        /// <summary>
        /// Gets or sets a callback to be invoked before deserialization occurs.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// The <see cref="JsonTypeInfo"/> instance has been locked for further modification.
        ///
        /// -or-
        ///
        /// Serialization callbacks are only supported for <see cref="JsonTypeInfoKind.Object"/> metadata.
        /// </exception>
        /// <remarks>
        /// For contracts originating from <see cref="DefaultJsonTypeInfoResolver"/> or <see cref="JsonSerializerContext"/>,
        /// the value of this callback will be mapped from any <see cref="IJsonOnDeserializing"/> implementation on the type.
        /// </remarks>
        public Action<object>? OnDeserializing
        {
            get => _onDeserializing;
            set
            {
                VerifyMutable();

                if (Kind != JsonTypeInfoKind.Object)
                {
                    ThrowHelper.ThrowInvalidOperationException_JsonTypeInfoOperationNotPossibleForKind(Kind);
                }

                _onDeserializing = value;
            }
        }

        /// <summary>
        /// Gets or sets a callback to be invoked after deserialization occurs.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// The <see cref="JsonTypeInfo"/> instance has been locked for further modification.
        ///
        /// -or-
        ///
        /// Serialization callbacks are only supported for <see cref="JsonTypeInfoKind.Object"/> metadata.
        /// </exception>
        /// <remarks>
        /// For contracts originating from <see cref="DefaultJsonTypeInfoResolver"/> or <see cref="JsonSerializerContext"/>,
        /// the value of this callback will be mapped from any <see cref="IJsonOnDeserialized"/> implementation on the type.
        /// </remarks>
        public Action<object>? OnDeserialized
        {
            get => _onDeserialized;
            set
            {
                VerifyMutable();

                if (Kind != JsonTypeInfoKind.Object)
                {
                    ThrowHelper.ThrowInvalidOperationException_JsonTypeInfoOperationNotPossibleForKind(Kind);
                }

                _onDeserialized = value;
            }
        }

        /// <summary>
        /// Gets the list of <see cref="JsonPropertyInfo"/> metadata corresponding to the current type.
        /// </summary>
        /// <remarks>
        /// Property is only applicable to metadata of kind <see cref="JsonTypeInfoKind.Object"/>.
        /// For other kinds an empty, read-only list will be returned.
        ///
        /// The order of <see cref="JsonPropertyInfo"/> entries in the list determines the serialization order,
        /// unless either of the entries specifies a non-zero <see cref="JsonPropertyInfo.Order"/> value,
        /// in which case the properties will be stable sorted by <see cref="JsonPropertyInfo.Order"/>.
        ///
        /// It is required that added <see cref="JsonPropertyInfo"/> entries are unique up to <see cref="JsonPropertyInfo.Name"/>,
        /// however this will only be validated on serialization, once the metadata instance gets locked for further modification.
        /// </remarks>
        public IList<JsonPropertyInfo> Properties
        {
            get
            {
                if (_properties == null)
                {
                    PopulatePropertyList();
                }

                return _properties;
            }
        }

        /// <summary>
        /// Gets or sets a configuration object specifying polymorphism metadata.
        /// </summary>
        /// <exception cref="ArgumentException">
        /// <paramref name="value" /> has been associated with a different <see cref="JsonTypeInfo"/> instance.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// The <see cref="JsonTypeInfo"/> instance has been locked for further modification.
        ///
        /// -or-
        ///
        /// Polymorphic serialization is not supported for the current metadata <see cref="Kind"/>.
        /// </exception>
        /// <remarks>
        /// For contracts originating from <see cref="DefaultJsonTypeInfoResolver"/> or <see cref="JsonSerializerContext"/>,
        /// the configuration of this setting will be mapped from any <see cref="JsonDerivedTypeAttribute"/> or <see cref="JsonPolymorphicAttribute"/> annotations.
        /// </remarks>
        public JsonPolymorphismOptions? PolymorphismOptions
        {
            get => _polymorphismOptions;
            set
            {
                VerifyMutable();

                if (value != null)
                {
                    if (Kind == JsonTypeInfoKind.None)
                    {
                        ThrowHelper.ThrowInvalidOperationException_JsonTypeInfoOperationNotPossibleForKind(Kind);
                    }

                    if (value.DeclaringTypeInfo != null && value.DeclaringTypeInfo != this)
                    {
                        ThrowHelper.ThrowArgumentException_JsonPolymorphismOptionsAssociatedWithDifferentJsonTypeInfo(nameof(value));
                    }

                    value.DeclaringTypeInfo = this;
                }

                _polymorphismOptions = value;
            }
        }

        private protected JsonPolymorphismOptions? _polymorphismOptions;

        internal object? CreateObjectWithArgs { get; set; }

        // Add method delegate for non-generic Stack and Queue; and types that derive from them.
        internal object? AddMethodDelegate { get; set; }

        internal JsonPropertyInfo? ExtensionDataProperty { get; private set; }

        internal PolymorphicTypeResolver? PolymorphicTypeResolver { get; private set; }

        // If enumerable or dictionary, the JsonTypeInfo for the element type.
        private JsonTypeInfo? _elementTypeInfo;

        // Flag indicating that JsonTypeInfo<T>.SerializeHandler is populated and is compatible with the associated Options instance.
        internal bool CanUseSerializeHandler { get; private protected set; }

        // Configure would normally have thrown why initializing properties for source gen but type had SerializeHandler
        // so it is allowed to be used for fast-path serialization but it will throw if used for metadata-based serialization
        internal bool MetadataSerializationNotSupported { get; private protected set; }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void ValidateCanBeUsedForMetadataSerialization()
        {
            if (MetadataSerializationNotSupported)
            {
                ThrowHelper.ThrowInvalidOperationException_NoMetadataForTypeProperties(Options.TypeInfoResolver, Type);
            }
        }

        /// <summary>
        /// Return the JsonTypeInfo for the element type, or null if the type is not an enumerable or dictionary.
        /// </summary>
        /// <remarks>
        /// This should not be called during warm-up (initial creation of JsonTypeInfos) to avoid recursive behavior
        /// which could result in a StackOverflowException.
        /// </remarks>
        internal JsonTypeInfo? ElementTypeInfo
        {
            get
            {
                if (_elementTypeInfo == null)
                {
                    if (ElementType != null)
                    {
                        // GetOrAddJsonTypeInfo already ensures JsonTypeInfo is configured
                        // also see comment on JsonPropertyInfo.JsonTypeInfo
                        _elementTypeInfo = Options.GetTypeInfoInternal(ElementType);
                    }
                }
                else
                {
                    _elementTypeInfo.EnsureConfigured();
                }

                return _elementTypeInfo;
            }
            set
            {
                // Set by JsonMetadataServices.
                Debug.Assert(_elementTypeInfo == null);
                _elementTypeInfo = value;
            }
        }

        internal Type? ElementType { get; }

        // If dictionary, the JsonTypeInfo for the key type.
        private JsonTypeInfo? _keyTypeInfo;

        /// <summary>
        /// Return the JsonTypeInfo for the key type, or null if the type is not a dictionary.
        /// </summary>
        /// <remarks>
        /// This should not be called during warm-up (initial creation of JsonTypeInfos) to avoid recursive behavior
        /// which could result in a StackOverflowException.
        /// </remarks>
        internal JsonTypeInfo? KeyTypeInfo
        {
            get
            {
                if (_keyTypeInfo == null)
                {
                    if (KeyType != null)
                    {
                        Debug.Assert(PropertyInfoForTypeInfo.ConverterStrategy == ConverterStrategy.Dictionary);

                        // GetOrAddJsonTypeInfo already ensures JsonTypeInfo is configured
                        // also see comment on JsonPropertyInfo.JsonTypeInfo
                        _keyTypeInfo = Options.GetTypeInfoInternal(KeyType);
                    }
                }
                else
                {
                    _keyTypeInfo.EnsureConfigured();
                }

                return _keyTypeInfo;
            }
            set
            {
                // Set by JsonMetadataServices.
                Debug.Assert(_keyTypeInfo == null);
                _keyTypeInfo = value;
            }
        }

        internal Type? KeyType { get; }

        /// <summary>
        /// Gets the <see cref="JsonSerializerOptions"/> value associated with the current <see cref="JsonTypeInfo" /> instance.
        /// </summary>
        public JsonSerializerOptions Options { get; }

        /// <summary>
        /// Gets the <see cref="Type"/> for which the JSON serialization contract is being defined.
        /// </summary>
        public Type Type { get; }

        /// <summary>
        /// Gets the <see cref="JsonConverter"/> associated with the current type.
        /// </summary>
        /// <remarks>
        /// The <see cref="JsonConverter"/> associated with the type determines the value of <see cref="Kind"/>,
        /// and by extension the types of metadata that are configurable in the current JSON contract.
        /// As such, the value of the converter cannot be changed once a <see cref="JsonTypeInfo"/> instance has been created.
        /// </remarks>
        public JsonConverter Converter { get; }

        /// <summary>
        /// Determines the kind of contract metadata that the current instance is specifying.
        /// </summary>
        /// <remarks>
        /// The value of <see cref="Kind"/> determines what aspects of the JSON contract are configurable.
        /// For example, it is only possible to configure the <see cref="Properties"/> list for metadata
        /// of kind <see cref="JsonTypeInfoKind.Object"/>.
        ///
        /// The value of <see cref="Kind"/> is determined exclusively by the <see cref="JsonConverter"/>
        /// resolved for the current type, and cannot be changed once resolution has happened.
        /// User-defined custom converters (specified either via <see cref="JsonConverterAttribute"/> or <see cref="JsonSerializerOptions.Converters"/>)
        /// are metadata-agnostic and thus always resolve to <see cref="JsonTypeInfoKind.None"/>.
        /// </remarks>
        public JsonTypeInfoKind Kind { get; private set; }

        /// <summary>
        /// The JsonPropertyInfo for this JsonTypeInfo. It is used to obtain the converter for the TypeInfo.
        /// </summary>
        /// <remarks>
        /// The returned JsonPropertyInfo does not represent a real property; instead it represents either:
        /// a collection element type,
        /// a generic type parameter,
        /// a property type (if pushed to a new stack frame),
        /// or the root type passed into the root serialization APIs.
        /// For example, for a property returning <see cref="Collections.Generic.List{T}"/> where T is a string,
        /// a JsonTypeInfo will be created with .Type=typeof(string) and .PropertyInfoForTypeInfo=JsonPropertyInfo{string}.
        /// Without this property, a "Converter" property would need to be added to JsonTypeInfo and there would be several more
        /// `if` statements to obtain the converter from either the actual JsonPropertyInfo (for a real property) or from the
        /// TypeInfo (for the cases mentioned above). In addition, methods that have a JsonPropertyInfo argument would also likely
        /// need to add an argument for JsonTypeInfo.
        /// </remarks>
        internal JsonPropertyInfo PropertyInfoForTypeInfo { get; }

        private protected abstract JsonPropertyInfo CreatePropertyInfoForTypeInfo();

        /// <summary>
        /// Returns a helper class used for computing the default value.
        /// </summary>
        internal DefaultValueHolder DefaultValueHolder => _defaultValueHolder ??= DefaultValueHolder.CreateHolder(Type);
        private DefaultValueHolder? _defaultValueHolder;

        /// <summary>
        /// Gets or sets the type-level <see cref="JsonSerializerOptions.NumberHandling"/> override.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// The <see cref="JsonTypeInfo"/> instance has been locked for further modification.
        /// </exception>
        /// <remarks>
        /// For contracts originating from <see cref="DefaultJsonTypeInfoResolver"/> or <see cref="JsonSerializerContext"/>,
        /// the value of this callback will be mapped from any <see cref="JsonNumberHandling"/> annotations.
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

        internal JsonTypeInfo(Type type, JsonConverter converter, JsonSerializerOptions options)
        {
            Type = type;
            Options = options;
            Converter = converter;
            PropertyInfoForTypeInfo = CreatePropertyInfoForTypeInfo();
            ElementType = converter.ElementType;

            switch (PropertyInfoForTypeInfo.ConverterStrategy)
            {
                case ConverterStrategy.Dictionary:
                    {
                        KeyType = converter.KeyType;
                    }
                    break;
                case ConverterStrategy.Object:
                case ConverterStrategy.Enumerable:
                case ConverterStrategy.Value:
                    break;
                case ConverterStrategy.None:
                    {
                        ThrowHelper.ThrowNotSupportedException_SerializationNotSupported(type);
                    }
                    break;
                default:
                    Debug.Fail($"Unexpected class type: {PropertyInfoForTypeInfo.ConverterStrategy}");
                    throw new InvalidOperationException();
            }

            Kind = GetTypeInfoKind(type, PropertyInfoForTypeInfo.ConverterStrategy);
        }

        internal void VerifyMutable()
        {
            if (_isConfigured)
            {
                ThrowHelper.ThrowInvalidOperationException_TypeInfoImmutable();
            }
        }

        private volatile bool _isConfigured;
        private readonly object _configureLock = new object();
        private ExceptionDispatchInfo? _cachedConfigureError;

        internal bool IsConfigured => _isConfigured;

        internal void EnsureConfigured()
        {
            Debug.Assert(!Monitor.IsEntered(_configureLock), "recursive locking detected.");

            if (!_isConfigured)
                ConfigureLocked();

            void ConfigureLocked()
            {
                _cachedConfigureError?.Throw();

                lock (_configureLock)
                {
                    if (_isConfigured)
                        return;

                    _cachedConfigureError?.Throw();

                    try
                    {
                        Configure();

                        _isConfigured = true;
                    }
                    catch (Exception e)
                    {
                        _cachedConfigureError = ExceptionDispatchInfo.Capture(e);
                        throw;
                    }
                }
            }
        }

        internal void Configure()
        {
            Debug.Assert(Monitor.IsEntered(_configureLock), "Configure called directly, use EnsureConfigured which locks this method");

            if (!Options.IsImmutable)
            {
                Options.InitializeForMetadataGeneration();
            }

            PropertyInfoForTypeInfo.EnsureChildOf(this);
            PropertyInfoForTypeInfo.EnsureConfigured();

            CanUseSerializeHandler &= Options.SerializerContext?.CanUseSerializationLogic == true;

            JsonConverter converter = Converter;
            Debug.Assert(PropertyInfoForTypeInfo.ConverterStrategy == Converter.ConverterStrategy,
                $"ConverterStrategy from PropertyInfoForTypeInfo.ConverterStrategy ({PropertyInfoForTypeInfo.ConverterStrategy}) does not match converter's ({Converter.ConverterStrategy})");

            if (Kind == JsonTypeInfoKind.Object)
            {
                InitializePropertyCache();

                if (converter.ConstructorIsParameterized)
                {
                    InitializeConstructorParameters(GetParameterInfoValues(), sourceGenMode: Options.SerializerContext != null);
                }
            }

            if (PolymorphismOptions != null)
            {
                PolymorphicTypeResolver = new PolymorphicTypeResolver(this);
            }
        }

#if DEBUG
        internal string GetPropertyDebugInfo(ReadOnlySpan<byte> unescapedPropertyName)
        {
            string propertyName = JsonHelpers.Utf8GetString(unescapedPropertyName);
            return $"propertyName = {propertyName}; DebugInfo={GetDebugInfo()}";
        }

        internal string GetDebugInfo()
        {
            ConverterStrategy strat = PropertyInfoForTypeInfo.ConverterStrategy;
            string jtiTypeName = GetType().Name;
            string typeName = Type.FullName!;
            bool propCacheInitialized = PropertyCache != null;

            StringBuilder sb = new();
            sb.AppendLine("{");
            sb.AppendLine($"  GetType: {jtiTypeName},");
            sb.AppendLine($"  Type: {typeName},");
            sb.AppendLine($"  ConverterStrategy: {strat},");
            sb.AppendLine($"  IsConfigured: {IsConfigured},");
            sb.AppendLine($"  HasPropertyCache: {propCacheInitialized},");

            if (propCacheInitialized)
            {
                sb.AppendLine("  Properties: {");
                foreach (var property in PropertyCache!.List)
                {
                    JsonPropertyInfo pi = property.Value;
                    sb.AppendLine($"    {property.Key}:");
                    sb.AppendLine($"{pi.GetDebugInfo(indent: 6)},");
                }

                sb.AppendLine("  },");
            }

            sb.AppendLine("}");
            return sb.ToString();
        }
#endif

        internal virtual void LateAddProperties() { }

        /// <summary>
        /// Creates a blank <see cref="JsonTypeInfo{T}"/> instance.
        /// </summary>
        /// <typeparam name="T">The type for which contract metadata is specified.</typeparam>
        /// <param name="options">The <see cref="JsonSerializerOptions"/> instance the metadata is associated with.</param>
        /// <returns>A blank <see cref="JsonTypeInfo{T}"/> instance.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="options"/> is null.</exception>
        /// <remarks>
        /// The returned <see cref="JsonTypeInfo{T}"/> will be blank, with the exception of the
        /// <see cref="Converter"/> property which will be resolved either from
        /// <see cref="JsonSerializerOptions.Converters"/> or the built-in converters for the type.
        /// Any converters specified via <see cref="JsonConverterAttribute"/> on the type declaration
        /// will not be resolved by this method.
        ///
        /// What converter does get resolved influences the value of <see cref="Kind"/>,
        /// which constrains the type of metadata that can be modified in the <see cref="JsonTypeInfo"/> instance.
        /// </remarks>
        [RequiresUnreferencedCode(MetadataFactoryRequiresUnreferencedCode)]
        [RequiresDynamicCode(MetadataFactoryRequiresUnreferencedCode)]
        public static JsonTypeInfo<T> CreateJsonTypeInfo<T>(JsonSerializerOptions options)
        {
            if (options == null)
            {
                ThrowHelper.ThrowArgumentNullException(nameof(options));
            }

            JsonConverter converter = DefaultJsonTypeInfoResolver.GetConverterForType(typeof(T), options, resolveJsonConverterAttribute: false);
            return new CustomJsonTypeInfo<T>(converter, options);
        }

        /// <summary>
        /// Creates a blank <see cref="JsonTypeInfo"/> instance.
        /// </summary>
        /// <param name="type">The type for which contract metadata is specified.</param>
        /// <param name="options">The <see cref="JsonSerializerOptions"/> instance the metadata is associated with.</param>
        /// <returns>A blank <see cref="JsonTypeInfo"/> instance.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="type"/> or <paramref name="options"/> is null.</exception>
        /// <exception cref="ArgumentException"><paramref name="type"/> cannot be used for serialization.</exception>
        /// <remarks>
        /// The returned <see cref="JsonTypeInfo"/> will be blank, with the exception of the
        /// <see cref="Converter"/> property which will be resolved either from
        /// <see cref="JsonSerializerOptions.Converters"/> or the built-in converters for the type.
        /// Any converters specified via <see cref="JsonConverterAttribute"/> on the type declaration
        /// will not be resolved by this method.
        ///
        /// What converter does get resolved influences the value of <see cref="Kind"/>,
        /// which constrains the type of metadata that can be modified in the <see cref="JsonTypeInfo"/> instance.
        /// </remarks>
        [RequiresUnreferencedCode(MetadataFactoryRequiresUnreferencedCode)]
        [RequiresDynamicCode(MetadataFactoryRequiresUnreferencedCode)]
        public static JsonTypeInfo CreateJsonTypeInfo(Type type, JsonSerializerOptions options)
        {
            if (type == null)
            {
                ThrowHelper.ThrowArgumentNullException(nameof(type));
            }

            if (options == null)
            {
                ThrowHelper.ThrowArgumentNullException(nameof(options));
            }

            if (IsInvalidForSerialization(type))
            {
                ThrowHelper.ThrowArgumentException_CannotSerializeInvalidType(nameof(type), type, null, null);
            }

            JsonTypeInfo jsonTypeInfo;
            JsonConverter converter = DefaultJsonTypeInfoResolver.GetConverterForType(type, options, resolveJsonConverterAttribute: false);

            if (converter.TypeToConvert == type)
            {
                // For performance, avoid doing a reflection-based instantiation
                // if the converter type matches that of the declared type.
                jsonTypeInfo = converter.CreateCustomJsonTypeInfo(options);
            }
            else
            {
                Type jsonTypeInfoType = typeof(CustomJsonTypeInfo<>).MakeGenericType(type);
                jsonTypeInfo = (JsonTypeInfo)jsonTypeInfoType.CreateInstanceNoWrapExceptions(
                    parameterTypes: new Type[] { typeof(JsonConverter), typeof(JsonSerializerOptions) },
                    parameters: new object[] { converter, options })!;
            }

            Debug.Assert(jsonTypeInfo.Type == type);
            return jsonTypeInfo;
        }

        /// <summary>
        /// Creates a blank <see cref="JsonPropertyInfo"/> instance for the current <see cref="JsonTypeInfo"/>.
        /// </summary>
        /// <param name="propertyType">The declared type for the property.</param>
        /// <param name="name">The property name used in JSON serialization and deserialization.</param>
        /// <returns>A blank <see cref="JsonPropertyInfo"/> instance.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="propertyType"/> or <paramref name="name"/> is null.</exception>
        /// <exception cref="ArgumentException"><paramref name="propertyType"/> cannot be used for serialization.</exception>
        [RequiresUnreferencedCode(MetadataFactoryRequiresUnreferencedCode)]
        [RequiresDynamicCode(MetadataFactoryRequiresUnreferencedCode)]
        public JsonPropertyInfo CreateJsonPropertyInfo(Type propertyType, string name)
        {
            if (propertyType == null)
            {
                ThrowHelper.ThrowArgumentNullException(nameof(propertyType));
            }

            if (name == null)
            {
                ThrowHelper.ThrowArgumentNullException(nameof(name));
            }

            if (IsInvalidForSerialization(propertyType))
            {
                ThrowHelper.ThrowArgumentException_CannotSerializeInvalidType(nameof(propertyType), propertyType, Type, name);
            }

            JsonPropertyInfo propertyInfo = CreatePropertyUsingReflection(propertyType);
            propertyInfo.Name = name;

            return propertyInfo;
        }

        [MemberNotNull(nameof(_properties))]
        private void PopulatePropertyList()
        {
            Debug.Assert(!Monitor.IsEntered(_configureLock), "should not be invoked from Configure");

            if (!_isConfigured)
            {
                // For mutable instances we need to synchronize access
                // with Configure() calls, otherwise we risk corrupting property state.
                lock (_configureLock)
                {
                    if (!_isConfigured)
                    {
                        // Ensure SourceGen had a chance to add properties
                        LateAddProperties();
                        _properties = new(this);
                        return;
                    }
                }
            }

            _properties = new(this);
        }

        internal abstract JsonParameterInfoValues[] GetParameterInfoValues();

        internal void CacheMember(JsonPropertyInfo jsonPropertyInfo, JsonPropertyDictionary<JsonPropertyInfo> propertyCache, ref Dictionary<string, JsonPropertyInfo>? ignoredMembers)
        {
            Debug.Assert(jsonPropertyInfo.MemberName != null, "MemberName can be null in custom JsonPropertyInfo instances and should never be passed in this method");
            string memberName = jsonPropertyInfo.MemberName;

            if (jsonPropertyInfo.IsExtensionData)
            {
                if (ExtensionDataProperty != null)
                {
                    ThrowHelper.ThrowInvalidOperationException_SerializationDuplicateTypeAttribute(Type, typeof(JsonExtensionDataAttribute));
                }

                ExtensionDataProperty = jsonPropertyInfo;
                return;
            }

            // The JsonPropertyNameAttribute or naming policy resulted in a collision.
            if (!propertyCache.TryAdd(jsonPropertyInfo.Name, jsonPropertyInfo))
            {
                JsonPropertyInfo other = propertyCache[jsonPropertyInfo.Name]!;

                if (other.IsIgnored)
                {
                    // Overwrite previously cached property since it has [JsonIgnore].
                    propertyCache[jsonPropertyInfo.Name] = jsonPropertyInfo;
                }
                else if (
                    // Does the current property have `JsonIgnoreAttribute`?
                    !jsonPropertyInfo.IsIgnored &&
                    // Is the current property hidden by the previously cached property
                    // (with `new` keyword, or by overriding)?
                    other.MemberName != memberName &&
                    // Was a property with the same CLR name was ignored? That property hid the current property,
                    // thus, if it was ignored, the current property should be ignored too.
                    ignoredMembers?.ContainsKey(memberName) != true)
                {
                    // We throw if we have two public properties that have the same JSON property name, and neither have been ignored.
                    ThrowHelper.ThrowInvalidOperationException_SerializerPropertyNameConflict(Type, jsonPropertyInfo.Name);
                }
                // Ignore the current property.
            }

            if (jsonPropertyInfo.IsIgnored)
            {
                (ignoredMembers ??= new Dictionary<string, JsonPropertyInfo>()).Add(memberName, jsonPropertyInfo);
            }
        }

        private sealed class ParameterLookupKey
        {
            public ParameterLookupKey(string name, Type type)
            {
                Name = name;
                Type = type;
            }

            public string Name { get; }
            public Type Type { get; }

            public override int GetHashCode()
            {
                return StringComparer.OrdinalIgnoreCase.GetHashCode(Name);
            }

            public override bool Equals([NotNullWhen(true)] object? obj)
            {
                Debug.Assert(obj is ParameterLookupKey);

                ParameterLookupKey other = (ParameterLookupKey)obj;
                return Type == other.Type && string.Equals(Name, other.Name, StringComparison.OrdinalIgnoreCase);
            }
        }

        private sealed class ParameterLookupValue
        {
            public ParameterLookupValue(JsonPropertyInfo jsonPropertyInfo)
            {
                JsonPropertyInfo = jsonPropertyInfo;
            }

            public string? DuplicateName { get; set; }
            public JsonPropertyInfo JsonPropertyInfo { get; }
        }

        internal void InitializePropertyCache()
        {
            Debug.Assert(Kind == JsonTypeInfoKind.Object);

            if (_properties != null)
            {
                // Properties have been exported to a metadata resolver,
                // invalidate the property cache and build from scratch

                ExtensionDataProperty = null;
                if (PropertyCache is null)
                {
                    PropertyCache = CreatePropertyCache(capacity: _properties.Count);
                }
                else
                {
                    PropertyCache.Clear();
                }

                bool isOrderSpecified = false;
                foreach (JsonPropertyInfo property in _properties)
                {
                    if (property.IsExtensionData)
                    {
                        if (ExtensionDataProperty != null)
                        {
                            ThrowHelper.ThrowInvalidOperationException_SerializationDuplicateTypeAttribute(Type, typeof(JsonExtensionDataAttribute));
                        }

                        ExtensionDataProperty = property;
                        continue;
                    }

                    if (!PropertyCache.TryAddValue(property.Name, property))
                    {
                        ThrowHelper.ThrowInvalidOperationException_SerializerPropertyNameConflict(Type, property.Name);
                    }

                    isOrderSpecified |= property.Order != 0;
                }

                if (isOrderSpecified)
                {
                    PropertyCache.List.StableSortByKey(static prop => prop.Value.Order);
                }
            }
            else
            {
                // Resolver didn't modify properties

                // Source gen currently when initializes properties
                // also assigns JsonPropertyInfo's JsonTypeInfo which causes SO if there are any
                // cycles in the object graph. For that reason properties cannot be added immediately.
                // This is a no-op for ReflectionJsonTypeInfo
                LateAddProperties();
                PropertyCache ??= CreatePropertyCache(capacity: 0);
            }

            if (ExtensionDataProperty != null)
            {
                ExtensionDataProperty.EnsureChildOf(this);
                ExtensionDataProperty.EnsureConfigured();
            }

            int numberOfRequiredProperties = 0;
            foreach (KeyValuePair<string, JsonPropertyInfo> jsonPropertyInfoKv in PropertyCache.List)
            {
                JsonPropertyInfo jsonPropertyInfo = jsonPropertyInfoKv.Value;

                if (jsonPropertyInfo.IsRequired)
                {
                    jsonPropertyInfo.RequiredPropertyIndex = numberOfRequiredProperties++;
                }

                jsonPropertyInfo.EnsureChildOf(this);
                jsonPropertyInfo.EnsureConfigured();
            }

            NumberOfRequiredProperties = numberOfRequiredProperties;
        }

        internal void InitializeConstructorParameters(JsonParameterInfoValues[] jsonParameters, bool sourceGenMode = false)
        {
            Debug.Assert(ParameterCache is null);
            Debug.Assert(Kind == JsonTypeInfoKind.Object);

            var parameterCache = new JsonPropertyDictionary<JsonParameterInfo>(Options.PropertyNameCaseInsensitive, jsonParameters.Length);

            // Cache the lookup from object property name to JsonPropertyInfo using a case-insensitive comparer.
            // Case-insensitive is used to support both camel-cased parameter names and exact matches when C#
            // record types or anonymous types are used.
            // The property name key does not use [JsonPropertyName] or PropertyNamingPolicy since we only bind
            // the parameter name to the object property name and do not use the JSON version of the name here.
            var nameLookup = new Dictionary<ParameterLookupKey, ParameterLookupValue>(PropertyCache!.Count);

            foreach (KeyValuePair<string, JsonPropertyInfo> kvp in PropertyCache.List)
            {
                JsonPropertyInfo jsonProperty = kvp.Value;
                string propertyName = jsonProperty.MemberName ?? jsonProperty.Name;

                ParameterLookupKey key = new(propertyName, jsonProperty.PropertyType);
                ParameterLookupValue value = new(jsonProperty);

                if (!JsonHelpers.TryAdd(nameLookup, key, value))
                {
                    // More than one property has the same case-insensitive name and Type.
                    // Remember so we can throw a nice exception if this property is used as a parameter name.
                    ParameterLookupValue existing = nameLookup[key];
                    existing.DuplicateName = propertyName;
                }
            }

            foreach (JsonParameterInfoValues parameterInfo in jsonParameters)
            {
                ParameterLookupKey paramToCheck = new(parameterInfo.Name, parameterInfo.ParameterType);

                if (nameLookup.TryGetValue(paramToCheck, out ParameterLookupValue? matchingEntry))
                {
                    if (matchingEntry.DuplicateName != null)
                    {
                        // Multiple object properties cannot bind to the same constructor parameter.
                        ThrowHelper.ThrowInvalidOperationException_MultiplePropertiesBindToConstructorParameters(
                            Type,
                            parameterInfo.Name!,
                            matchingEntry.JsonPropertyInfo.Name,
                            matchingEntry.DuplicateName);
                    }

                    Debug.Assert(matchingEntry.JsonPropertyInfo != null);
                    JsonPropertyInfo jsonPropertyInfo = matchingEntry.JsonPropertyInfo;
                    JsonParameterInfo jsonParameterInfo = CreateConstructorParameter(parameterInfo, jsonPropertyInfo, sourceGenMode, Options);
                    parameterCache.Add(jsonPropertyInfo.Name, jsonParameterInfo);
                }
                // It is invalid for the extension data property to bind with a constructor argument.
                else if (ExtensionDataProperty != null &&
                    StringComparer.OrdinalIgnoreCase.Equals(paramToCheck.Name, ExtensionDataProperty.Name))
                {
                    Debug.Assert(ExtensionDataProperty.MemberName != null, "Custom property info cannot be data extension property");
                    ThrowHelper.ThrowInvalidOperationException_ExtensionDataCannotBindToCtorParam(ExtensionDataProperty.MemberName, ExtensionDataProperty);
                }
            }

            ParameterCount = jsonParameters.Length;
            ParameterCache = parameterCache;
        }

        internal static void ValidateType(Type type)
        {
            if (IsInvalidForSerialization(type))
            {
                ThrowHelper.ThrowInvalidOperationException_CannotSerializeInvalidType(type, declaringType: null, memberInfo: null);
            }
        }

        internal static bool IsInvalidForSerialization(Type type)
        {
            return type == typeof(void) || type.IsPointer || type.IsByRef || IsByRefLike(type) || type.ContainsGenericParameters;
        }

        private static bool IsByRefLike(Type type)
        {
#if NETCOREAPP
            return type.IsByRefLike;
#else
            if (!type.IsValueType)
            {
                return false;
            }

            object[] attributes = type.GetCustomAttributes(inherit: false);

            for (int i = 0; i < attributes.Length; i++)
            {
                if (attributes[i].GetType().FullName == "System.Runtime.CompilerServices.IsByRefLikeAttribute")
                {
                    return true;
                }
            }

            return false;
#endif
        }

        internal static bool IsValidExtensionDataProperty(Type propertyType)
        {
            return typeof(IDictionary<string, object>).IsAssignableFrom(propertyType) ||
                typeof(IDictionary<string, JsonElement>).IsAssignableFrom(propertyType) ||
                // Avoid a reference to typeof(JsonNode) to support trimming.
                (propertyType.FullName == JsonObjectTypeName && ReferenceEquals(propertyType.Assembly, typeof(JsonTypeInfo).Assembly));
        }

        internal JsonPropertyDictionary<JsonPropertyInfo> CreatePropertyCache(int capacity)
        {
            return new JsonPropertyDictionary<JsonPropertyInfo>(Options.PropertyNameCaseInsensitive, capacity);
        }

        private static JsonParameterInfo CreateConstructorParameter(
            JsonParameterInfoValues parameterInfo,
            JsonPropertyInfo jsonPropertyInfo,
            bool sourceGenMode,
            JsonSerializerOptions options)
        {
            if (jsonPropertyInfo.IsIgnored)
            {
                return JsonParameterInfo.CreateIgnoredParameterPlaceholder(parameterInfo, jsonPropertyInfo, sourceGenMode);
            }

            JsonConverter converter = jsonPropertyInfo.EffectiveConverter;
            JsonParameterInfo jsonParameterInfo = converter.CreateJsonParameterInfo();

            jsonParameterInfo.Initialize(parameterInfo, jsonPropertyInfo, options);

            return jsonParameterInfo;
        }

        private static JsonTypeInfoKind GetTypeInfoKind(Type type, ConverterStrategy converterStrategy)
        {
            // System.Object is polymorphic and will not respect Properties
            if (type == typeof(object))
            {
                return JsonTypeInfoKind.None;
            }

            return converterStrategy switch
            {
                ConverterStrategy.Object => JsonTypeInfoKind.Object,
                ConverterStrategy.Enumerable => JsonTypeInfoKind.Enumerable,
                ConverterStrategy.Dictionary => JsonTypeInfoKind.Dictionary,
                _ => JsonTypeInfoKind.None
            };
        }

        private sealed class JsonPropertyInfoList : ConfigurationList<JsonPropertyInfo>
        {
            private readonly JsonTypeInfo _jsonTypeInfo;

            public JsonPropertyInfoList(JsonTypeInfo jsonTypeInfo)
                : base(jsonTypeInfo.PropertyCache?.Values)
            {
                if (jsonTypeInfo.ExtensionDataProperty is not null)
                {
                    _list.Add(jsonTypeInfo.ExtensionDataProperty);
                }

                _jsonTypeInfo = jsonTypeInfo;
            }

            protected override bool IsImmutable => _jsonTypeInfo.IsConfigured || _jsonTypeInfo.Kind != JsonTypeInfoKind.Object;
            protected override void VerifyMutable()
            {
                _jsonTypeInfo.VerifyMutable();

                if (_jsonTypeInfo.Kind != JsonTypeInfoKind.Object)
                {
                    ThrowHelper.ThrowInvalidOperationException_JsonTypeInfoOperationNotPossibleForKind(_jsonTypeInfo.Kind);
                }
            }

            protected override void OnAddingElement(JsonPropertyInfo item)
            {
                item.EnsureChildOf(_jsonTypeInfo);
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private string DebuggerDisplay => $"Type = {Type.Name}, Kind = {Kind}";
    }
}
