// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text.Encodings.Web;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Converters;
using System.Text.Json.Serialization.Metadata;
using System.Threading;

namespace System.Text.Json
{
    /// <summary>
    /// Provides options to be used with <see cref="JsonSerializer"/>.
    /// </summary>
    [DebuggerDisplay("{DebuggerDisplay,nq}")]
    public sealed partial class JsonSerializerOptions
    {
        internal const int BufferSizeDefault = 16 * 1024;

        // For backward compatibility the default max depth for JsonSerializer is 64,
        // the minimum of JsonReaderOptions.DefaultMaxDepth and JsonWriterOptions.DefaultMaxDepth.
        internal const int DefaultMaxDepth = JsonReaderOptions.DefaultMaxDepth;

        /// <summary>
        /// Gets a read-only, singleton instance of <see cref="JsonSerializerOptions" /> that uses the default configuration.
        /// </summary>
        /// <remarks>
        /// Each <see cref="JsonSerializerOptions" /> instance encapsulates its own serialization metadata caches,
        /// so using fresh default instances every time one is needed can result in redundant recomputation of converters.
        /// This property provides a shared instance that can be consumed by any number of components without necessitating any converter recomputation.
        /// </remarks>
        public static JsonSerializerOptions Default
        {
            [RequiresUnreferencedCode(JsonSerializer.SerializationUnreferencedCodeMessage)]
            [RequiresDynamicCode(JsonSerializer.SerializationRequiresDynamicCodeMessage)]
            get
            {
                return s_defaultOptions ?? GetOrCreateSingleton(ref s_defaultOptions, JsonSerializerDefaults.General);
            }
        }

        private static JsonSerializerOptions? s_defaultOptions;

        /// <summary>
        /// Gets a read-only, singleton instance of <see cref="JsonSerializerOptions" /> that uses the web configuration.
        /// </summary>
        /// <remarks>
        /// Each <see cref="JsonSerializerOptions" /> instance encapsulates its own serialization metadata caches,
        /// so using fresh default instances every time one is needed can result in redundant recomputation of converters.
        /// This property provides a shared instance that can be consumed by any number of components without necessitating any converter recomputation.
        /// </remarks>
        public static JsonSerializerOptions Web
        {
            [RequiresUnreferencedCode(JsonSerializer.SerializationUnreferencedCodeMessage)]
            [RequiresDynamicCode(JsonSerializer.SerializationRequiresDynamicCodeMessage)]
            get
            {
                return s_webOptions ?? GetOrCreateSingleton(ref s_webOptions, JsonSerializerDefaults.Web);
            }
        }

        private static JsonSerializerOptions? s_webOptions;

        // For any new option added, adding it to the options copied in the copy constructor below must be considered.
        private IJsonTypeInfoResolver? _typeInfoResolver;
        private JsonNamingPolicy? _dictionaryKeyPolicy;
        private JsonNamingPolicy? _jsonPropertyNamingPolicy;
        private JsonCommentHandling _readCommentHandling;
        private ReferenceHandler? _referenceHandler;
        private JavaScriptEncoder? _encoder;
        private ConverterList? _converters;
        private JsonIgnoreCondition _defaultIgnoreCondition;
        private JsonNumberHandling _numberHandling;
        private JsonObjectCreationHandling _preferredObjectCreationHandling;
        private JsonUnknownTypeHandling _unknownTypeHandling;
        private JsonUnmappedMemberHandling _unmappedMemberHandling;

        private int _defaultBufferSize = BufferSizeDefault;
        private int _maxDepth;
        private bool _allowTrailingCommas;
        private bool _ignoreNullValues;
        private bool _ignoreReadOnlyProperties;
        private bool _ignoreReadonlyFields;
        private bool _includeFields;
        private bool _propertyNameCaseInsensitive;
        private bool _writeIndented;
        private char _indentCharacter = JsonConstants.DefaultIndentCharacter;
        private int _indentSize = JsonConstants.DefaultIndentSize;

        /// <summary>
        /// Constructs a new <see cref="JsonSerializerOptions"/> instance.
        /// </summary>
        public JsonSerializerOptions()
        {
            TrackOptionsInstance(this);
        }

        /// <summary>
        /// Copies the options from a <see cref="JsonSerializerOptions"/> instance to a new instance.
        /// </summary>
        /// <param name="options">The <see cref="JsonSerializerOptions"/> instance to copy options from.</param>
        /// <exception cref="System.ArgumentNullException">
        /// <paramref name="options"/> is <see langword="null"/>.
        /// </exception>
        public JsonSerializerOptions(JsonSerializerOptions options)
        {
            if (options is null)
            {
                ThrowHelper.ThrowArgumentNullException(nameof(options));
            }

            // The following fields are not copied intentionally:
            // 1. _cachingContext can only be set in immutable options instances.
            // 2. _typeInfoResolverChain can be created lazily as it relies on
            //    _typeInfoResolver as its source of truth.

            _dictionaryKeyPolicy = options._dictionaryKeyPolicy;
            _jsonPropertyNamingPolicy = options._jsonPropertyNamingPolicy;
            _readCommentHandling = options._readCommentHandling;
            _referenceHandler = options._referenceHandler;
            _converters = options._converters is { } converters ? new(this, converters) : null;
            _encoder = options._encoder;
            _defaultIgnoreCondition = options._defaultIgnoreCondition;
            _numberHandling = options._numberHandling;
            _preferredObjectCreationHandling = options._preferredObjectCreationHandling;
            _unknownTypeHandling = options._unknownTypeHandling;
            _unmappedMemberHandling = options._unmappedMemberHandling;

            _defaultBufferSize = options._defaultBufferSize;
            _maxDepth = options._maxDepth;
            _allowTrailingCommas = options._allowTrailingCommas;
            _ignoreNullValues = options._ignoreNullValues;
            _ignoreReadOnlyProperties = options._ignoreReadOnlyProperties;
            _ignoreReadonlyFields = options._ignoreReadonlyFields;
            _includeFields = options._includeFields;
            _propertyNameCaseInsensitive = options._propertyNameCaseInsensitive;
            _writeIndented = options._writeIndented;
            _indentCharacter = options._indentCharacter;
            _indentSize = options._indentSize;
            _typeInfoResolver = options._typeInfoResolver;
            EffectiveMaxDepth = options.EffectiveMaxDepth;
            ReferenceHandlingStrategy = options.ReferenceHandlingStrategy;

            TrackOptionsInstance(this);
        }

        /// <summary>
        /// Constructs a new <see cref="JsonSerializerOptions"/> instance with a predefined set of options determined by the specified <see cref="JsonSerializerDefaults"/>.
        /// </summary>
        /// <param name="defaults"> The <see cref="JsonSerializerDefaults"/> to reason about.</param>
        public JsonSerializerOptions(JsonSerializerDefaults defaults) : this()
        {
            // Should be kept in sync with equivalent overload in JsonSourceGenerationOptionsAttribute

            if (defaults == JsonSerializerDefaults.Web)
            {
                _propertyNameCaseInsensitive = true;
                _jsonPropertyNamingPolicy = JsonNamingPolicy.CamelCase;
                _numberHandling = JsonNumberHandling.AllowReadingFromString;
            }
            else if (defaults != JsonSerializerDefaults.General)
            {
                throw new ArgumentOutOfRangeException(nameof(defaults));
            }
        }

        /// <summary>Tracks the options instance to enable all instances to be enumerated.</summary>
        private static void TrackOptionsInstance(JsonSerializerOptions options) => TrackedOptionsInstances.All.Add(options, null);

        internal static class TrackedOptionsInstances
        {
            /// <summary>Tracks all live JsonSerializerOptions instances.</summary>
            /// <remarks>Instances are added to the table in their constructor.</remarks>
            public static ConditionalWeakTable<JsonSerializerOptions, object?> All { get; } =
                // TODO https://github.com/dotnet/runtime/issues/51159:
                // Look into linking this away / disabling it when hot reload isn't in use.
                new ConditionalWeakTable<JsonSerializerOptions, object?>();
        }

        /// <summary>
        /// Binds current <see cref="JsonSerializerOptions"/> instance with a new instance of the specified <see cref="Serialization.JsonSerializerContext"/> type.
        /// </summary>
        /// <typeparam name="TContext">The generic definition of the specified context type.</typeparam>
        /// <remarks>
        /// When serializing and deserializing types using the options
        /// instance, metadata for the types will be fetched from the context instance.
        /// </remarks>
        [Obsolete(Obsoletions.JsonSerializerOptionsAddContextMessage, DiagnosticId = Obsoletions.JsonSerializerOptionsAddContextDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public void AddContext<TContext>() where TContext : JsonSerializerContext, new()
        {
            VerifyMutable();
            TContext context = new();
            context.AssociateWithOptions(this);
        }

        /// <summary>
        /// Gets or sets the <see cref="JsonTypeInfo"/> contract resolver used by this instance.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Thrown if this property is set after serialization or deserialization has occurred.
        /// </exception>
        /// <remarks>
        /// A <see langword="null"/> setting is equivalent to using the reflection-based <see cref="DefaultJsonTypeInfoResolver" />.
        /// The property will be populated automatically once used with one of the <see cref="JsonSerializer"/> methods.
        ///
        /// This property is kept in sync with the <see cref="TypeInfoResolverChain"/> property.
        /// Any change made to this property will be reflected by <see cref="TypeInfoResolverChain"/> and vice versa.
        /// </remarks>
        public IJsonTypeInfoResolver? TypeInfoResolver
        {
            get
            {
                return _typeInfoResolver;
            }
            set
            {
                VerifyMutable();

                if (_typeInfoResolverChain is { } resolverChain && !ReferenceEquals(resolverChain, value))
                {
                    // User is setting a new resolver; invalidate the resolver chain if already created.
                    resolverChain.Clear();
                    resolverChain.AddFlattened(value);
                }

                _typeInfoResolver = value;
            }
        }

        /// <summary>
        /// Gets the list of chained <see cref="JsonTypeInfo"/> contract resolvers used by this instance.
        /// </summary>
        /// <remarks>
        /// The ordering of the chain is significant: <see cref="JsonSerializerOptions "/> will query each
        /// of the resolvers in their specified order, returning the first result that is non-null.
        /// If all resolvers in the chain return null, then <see cref="JsonSerializerOptions"/> will also return null.
        ///
        /// This property is auxiliary to and is kept in sync with the <see cref="TypeInfoResolver"/> property.
        /// Any change made to this property will be reflected by <see cref="TypeInfoResolver"/> and vice versa.
        /// </remarks>
        public IList<IJsonTypeInfoResolver> TypeInfoResolverChain => _typeInfoResolverChain ??= new(this);
        private OptionsBoundJsonTypeInfoResolverChain? _typeInfoResolverChain;

        /// <summary>
        /// Defines whether an extra comma at the end of a list of JSON values in an object or array
        /// is allowed (and ignored) within the JSON payload being deserialized.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Thrown if this property is set after serialization or deserialization has occurred.
        /// </exception>
        /// <remarks>
        /// By default, it's set to false, and <exception cref="JsonException"/> is thrown if a trailing comma is encountered.
        /// </remarks>
        public bool AllowTrailingCommas
        {
            get
            {
                return _allowTrailingCommas;
            }
            set
            {
                VerifyMutable();
                _allowTrailingCommas = value;
            }
        }

        /// <summary>
        /// The default buffer size in bytes used when creating temporary buffers.
        /// </summary>
        /// <remarks>The default size is 16K.</remarks>
        /// <exception cref="System.ArgumentException">Thrown when the buffer size is less than 1.</exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown if this property is set after serialization or deserialization has occurred.
        /// </exception>
        public int DefaultBufferSize
        {
            get
            {
                return _defaultBufferSize;
            }
            set
            {
                VerifyMutable();

                if (value < 1)
                {
                    throw new ArgumentException(SR.SerializationInvalidBufferSize);
                }

                _defaultBufferSize = value;
            }
        }

        /// <summary>
        /// The encoder to use when escaping strings, or <see langword="null" /> to use the default encoder.
        /// </summary>
        public JavaScriptEncoder? Encoder
        {
            get
            {
                return _encoder;
            }
            set
            {
                VerifyMutable();

                _encoder = value;
            }
        }

        /// <summary>
        /// Specifies the policy used to convert a <see cref="System.Collections.IDictionary"/> key's name to another format, such as camel-casing.
        /// </summary>
        /// <remarks>
        /// This property can be set to <see cref="JsonNamingPolicy.CamelCase"/> to specify a camel-casing policy.
        /// It is not used when deserializing.
        /// </remarks>
        public JsonNamingPolicy? DictionaryKeyPolicy
        {
            get
            {
                return _dictionaryKeyPolicy;
            }
            set
            {
                VerifyMutable();
                _dictionaryKeyPolicy = value;
            }
        }

        /// <summary>
        /// Determines whether null values are ignored during serialization and deserialization.
        /// The default value is false.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Thrown if this property is set after serialization or deserialization has occurred.
        /// or <see cref="DefaultIgnoreCondition"/> has been set to a non-default value. These properties cannot be used together.
        /// </exception>
        [Obsolete(Obsoletions.JsonSerializerOptionsIgnoreNullValuesMessage, DiagnosticId = Obsoletions.JsonSerializerOptionsIgnoreNullValuesDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public bool IgnoreNullValues
        {
            get
            {
                return _ignoreNullValues;
            }
            set
            {
                VerifyMutable();

                if (value && _defaultIgnoreCondition != JsonIgnoreCondition.Never)
                {
                    throw new InvalidOperationException(SR.DefaultIgnoreConditionAlreadySpecified);
                }

                _ignoreNullValues = value;
            }
        }

        /// <summary>
        /// Specifies a condition to determine when properties with default values are ignored during serialization or deserialization.
        /// The default value is <see cref="JsonIgnoreCondition.Never" />.
        /// </summary>
        /// <exception cref="ArgumentException">
        /// Thrown if this property is set to <see cref="JsonIgnoreCondition.Always"/>.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown if this property is set after serialization or deserialization has occurred,
        /// or <see cref="IgnoreNullValues"/> has been set to <see langword="true"/>. These properties cannot be used together.
        /// </exception>
        public JsonIgnoreCondition DefaultIgnoreCondition
        {
            get
            {
                return _defaultIgnoreCondition;
            }
            set
            {
                VerifyMutable();

                if (value == JsonIgnoreCondition.Always)
                {
                    throw new ArgumentException(SR.DefaultIgnoreConditionInvalid);
                }

                if (value != JsonIgnoreCondition.Never && _ignoreNullValues)
                {
                    throw new InvalidOperationException(SR.DefaultIgnoreConditionAlreadySpecified);
                }

                _defaultIgnoreCondition = value;
            }
        }

        /// <summary>
        /// Specifies how number types should be handled when serializing or deserializing.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Thrown if this property is set after serialization or deserialization has occurred.
        /// </exception>
        public JsonNumberHandling NumberHandling
        {
            get => _numberHandling;
            set
            {
                VerifyMutable();

                if (!JsonSerializer.IsValidNumberHandlingValue(value))
                {
                    throw new ArgumentOutOfRangeException(nameof(value));
                }
                _numberHandling = value;
            }
        }

        /// <summary>
        /// Specifies preferred object creation handling for properties when deserializing JSON.
        /// When set to <see cref="JsonObjectCreationHandling.Populate"/> all properties which
        /// are capable of reusing the existing instance will be populated.
        /// </summary>
        /// <remarks>
        /// Only property type is taken into consideration. For example if property is of type
        /// <see cref="IEnumerable{T}"/> but it is assigned <see cref="List{T}"/> it will not be populated
        /// because <see cref="IEnumerable{T}"/> is not capable of populating.
        /// Additionally value types require a setter to be populated.
        /// </remarks>
        public JsonObjectCreationHandling PreferredObjectCreationHandling
        {
            get => _preferredObjectCreationHandling;
            set
            {
                VerifyMutable();

                if (!JsonSerializer.IsValidCreationHandlingValue(value))
                {
                    throw new ArgumentOutOfRangeException(nameof(value));
                }

                _preferredObjectCreationHandling = value;
            }
        }

        /// <summary>
        /// Determines whether read-only properties are ignored during serialization.
        /// A property is read-only if it contains a public getter but not a public setter.
        /// The default value is false.
        /// </summary>
        /// <remarks>
        /// Read-only properties are not deserialized regardless of this setting.
        /// </remarks>
        /// <exception cref="InvalidOperationException">
        /// Thrown if this property is set after serialization or deserialization has occurred.
        /// </exception>
        public bool IgnoreReadOnlyProperties
        {
            get
            {
                return _ignoreReadOnlyProperties;
            }
            set
            {
                VerifyMutable();
                _ignoreReadOnlyProperties = value;
            }
        }

        /// <summary>
        /// Determines whether read-only fields are ignored during serialization.
        /// A field is read-only if it is marked with the <c>readonly</c> keyword.
        /// The default value is false.
        /// </summary>
        /// <remarks>
        /// Read-only fields are not deserialized regardless of this setting.
        /// </remarks>
        /// <exception cref="InvalidOperationException">
        /// Thrown if this property is set after serialization or deserialization has occurred.
        /// </exception>
        public bool IgnoreReadOnlyFields
        {
            get
            {
                return _ignoreReadonlyFields;
            }
            set
            {
                VerifyMutable();
                _ignoreReadonlyFields = value;
            }
        }

        /// <summary>
        /// Determines whether fields are handled on serialization and deserialization.
        /// The default value is false.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Thrown if this property is set after serialization or deserialization has occurred.
        /// </exception>
        public bool IncludeFields
        {
            get
            {
                return _includeFields;
            }
            set
            {
                VerifyMutable();
                _includeFields = value;
            }
        }

        /// <summary>
        /// Gets or sets the maximum depth allowed when serializing or deserializing JSON, with the default (i.e. 0) indicating a max depth of 64.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Thrown if this property is set after serialization or deserialization has occurred.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when the max depth is set to a negative value.
        /// </exception>
        /// <remarks>
        /// Going past this depth will throw a <exception cref="JsonException"/>.
        /// </remarks>
        public int MaxDepth
        {
            get => _maxDepth;
            set
            {
                VerifyMutable();

                if (value < 0)
                {
                    ThrowHelper.ThrowArgumentOutOfRangeException_MaxDepthMustBePositive(nameof(value));
                }

                _maxDepth = value;
                EffectiveMaxDepth = (value == 0 ? DefaultMaxDepth : value);
            }
        }

        internal int EffectiveMaxDepth { get; private set; } = DefaultMaxDepth;

        /// <summary>
        /// Specifies the policy used to convert a property's name on an object to another format, such as camel-casing.
        /// The resulting property name is expected to match the JSON payload during deserialization, and
        /// will be used when writing the property name during serialization.
        /// </summary>
        /// <remarks>
        /// The policy is not used for properties that have a <see cref="JsonPropertyNameAttribute"/> applied.
        /// This property can be set to <see cref="JsonNamingPolicy.CamelCase"/> to specify a camel-casing policy.
        /// </remarks>
        public JsonNamingPolicy? PropertyNamingPolicy
        {
            get
            {
                return _jsonPropertyNamingPolicy;
            }
            set
            {
                VerifyMutable();
                _jsonPropertyNamingPolicy = value;
            }
        }

        /// <summary>
        /// Determines whether a property's name uses a case-insensitive comparison during deserialization.
        /// The default value is false.
        /// </summary>
        /// <remarks>There is a performance cost associated when the value is true.</remarks>
        public bool PropertyNameCaseInsensitive
        {
            get
            {
                return _propertyNameCaseInsensitive;
            }
            set
            {
                VerifyMutable();
                _propertyNameCaseInsensitive = value;
            }
        }

        /// <summary>
        /// Defines how the comments are handled during deserialization.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Thrown if this property is set after serialization or deserialization has occurred.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when the comment handling enum is set to a value that is not supported (or not within the <see cref="JsonCommentHandling"/> enum range).
        /// </exception>
        /// <remarks>
        /// By default <exception cref="JsonException"/> is thrown if a comment is encountered.
        /// </remarks>
        public JsonCommentHandling ReadCommentHandling
        {
            get
            {
                return _readCommentHandling;
            }
            set
            {
                VerifyMutable();

                Debug.Assert(value >= 0);
                if (value > JsonCommentHandling.Skip)
                    throw new ArgumentOutOfRangeException(nameof(value), SR.JsonSerializerDoesNotSupportComments);

                _readCommentHandling = value;
            }
        }

        /// <summary>
        /// Defines how deserializing a type declared as an <see cref="object"/> is handled during deserialization.
        /// </summary>
        public JsonUnknownTypeHandling UnknownTypeHandling
        {
            get => _unknownTypeHandling;
            set
            {
                VerifyMutable();
                _unknownTypeHandling = value;
            }
        }

        /// <summary>
        /// Determines how <see cref="JsonSerializer"/> handles JSON properties that
        /// cannot be mapped to a specific .NET member when deserializing object types.
        /// </summary>
        public JsonUnmappedMemberHandling UnmappedMemberHandling
        {
            get => _unmappedMemberHandling;
            set
            {
                VerifyMutable();
                _unmappedMemberHandling = value;
            }
        }

        /// <summary>
        /// Defines whether JSON should pretty print which includes:
        /// indenting nested JSON tokens, adding new lines, and adding white space between property names and values.
        /// By default, the JSON is serialized without any extra white space.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Thrown if this property is set after serialization or deserialization has occurred.
        /// </exception>
        public bool WriteIndented
        {
            get
            {
                return _writeIndented;
            }
            set
            {
                VerifyMutable();
                _writeIndented = value;
            }
        }

        /// <summary>
        /// Defines the indentation character being used when <see cref="WriteIndented" /> is enabled. Defaults to the space character.
        /// </summary>
        /// <remarks>Allowed characters are space and horizontal tab.</remarks>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="value"/> contains an invalid character.</exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown if this property is set after serialization or deserialization has occurred.
        /// </exception>
        public char IndentCharacter
        {
            get
            {
                return _indentCharacter;
            }
            set
            {
                JsonWriterHelper.ValidateIndentCharacter(value);
                VerifyMutable();
                _indentCharacter = value;
            }
        }

        /// <summary>
        /// Defines the indentation size being used when <see cref="WriteIndented" /> is enabled. Defaults to two.
        /// </summary>
        /// <remarks>Allowed values are all integers between 0 and 127, included.</remarks>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="value"/> is out of the allowed range.</exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown if this property is set after serialization or deserialization has occurred.
        /// </exception>
        public int IndentSize
        {
            get
            {
                return _indentSize;
            }
            set
            {
                JsonWriterHelper.ValidateIndentSize(value);
                VerifyMutable();
                _indentSize = value;
            }
        }

        /// <summary>
        /// Configures how object references are handled when reading and writing JSON.
        /// </summary>
        public ReferenceHandler? ReferenceHandler
        {
            get => _referenceHandler;
            set
            {
                VerifyMutable();
                _referenceHandler = value;
                ReferenceHandlingStrategy = value?.HandlingStrategy ?? ReferenceHandlingStrategy.None;
            }
        }

        /// <summary>
        /// Returns true if options uses compatible built-in resolvers or a combination of compatible built-in resolvers.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        internal bool CanUseFastPathSerializationLogic
        {
            get
            {
                Debug.Assert(IsReadOnly);
                Debug.Assert(TypeInfoResolver != null);
                return _canUseFastPathSerializationLogic ??= TypeInfoResolver.IsCompatibleWithOptions(this);
            }
        }

        private bool? _canUseFastPathSerializationLogic;

        // The cached value used to determine if ReferenceHandler should use Preserve or IgnoreCycles semantics or None of them.
        internal ReferenceHandlingStrategy ReferenceHandlingStrategy = ReferenceHandlingStrategy.None;

        /// <summary>
        /// Specifies whether the current instance has been locked for user modification.
        /// </summary>
        /// <remarks>
        /// A <see cref="JsonSerializerOptions"/> instance can be locked either if
        /// it has been passed to one of the <see cref="JsonSerializer"/> methods,
        /// has been associated with a <see cref="JsonSerializerContext"/> instance,
        /// or a user explicitly called the <see cref="MakeReadOnly()"/> methods on the instance.
        ///
        /// Read-only instances use caching when querying <see cref="JsonConverter"/> and <see cref="JsonTypeInfo"/> metadata.
        /// </remarks>
        public bool IsReadOnly => _isReadOnly;
        private volatile bool _isReadOnly;

        /// <summary>
        /// Marks the current instance as read-only preventing any further user modification.
        /// </summary>
        /// <exception cref="InvalidOperationException">The instance does not specify a <see cref="TypeInfoResolver"/> setting.</exception>
        /// <remarks>This method is idempotent.</remarks>
        public void MakeReadOnly()
        {
            if (_typeInfoResolver is null)
            {
                ThrowHelper.ThrowInvalidOperationException_JsonSerializerOptionsNoTypeInfoResolverSpecified();
            }

            _isReadOnly = true;
        }

        /// <summary>
        /// Marks the current instance as read-only preventing any further user modification.
        /// </summary>
        /// <param name="populateMissingResolver">Populates unconfigured <see cref="TypeInfoResolver"/> properties with the reflection-based default.</param>
        /// <exception cref="InvalidOperationException">
        /// The instance does not specify a <see cref="TypeInfoResolver"/> setting. Thrown if <paramref name="populateMissingResolver"/> is <see langword="false"/>.
        /// -OR-
        /// The <see cref="JsonSerializer.IsReflectionEnabledByDefault"/> feature switch has been turned off.
        /// </exception>
        /// <remarks>
        /// When <paramref name="populateMissingResolver"/> is set to <see langword="true" />, configures the instance following
        /// the semantics of the <see cref="JsonSerializer"/> methods accepting <see cref="JsonSerializerOptions"/> parameters.
        ///
        /// This method is idempotent.
        /// </remarks>
        [RequiresUnreferencedCode("Populating unconfigured TypeInfoResolver properties with the reflection resolver requires unreferenced code.")]
        [RequiresDynamicCode("Populating unconfigured TypeInfoResolver properties with the reflection resolver requires runtime code generation.")]
        public void MakeReadOnly(bool populateMissingResolver)
        {
            if (populateMissingResolver)
            {
                if (!_isConfiguredForJsonSerializer)
                {
                    ConfigureForJsonSerializer();
                }
            }
            else
            {
                MakeReadOnly();
            }

            Debug.Assert(IsReadOnly);
        }

        /// <summary>
        /// Configures the instance for use by the JsonSerializer APIs, applying reflection-based fallback where applicable.
        /// </summary>
        [RequiresUnreferencedCode(JsonSerializer.SerializationUnreferencedCodeMessage)]
        [RequiresDynamicCode(JsonSerializer.SerializationRequiresDynamicCodeMessage)]
        private void ConfigureForJsonSerializer()
        {
            if (JsonSerializer.IsReflectionEnabledByDefault)
            {
                // Even if a resolver has already been specified, we need to root
                // the default resolver to gain access to the default converters.
                DefaultJsonTypeInfoResolver defaultResolver = DefaultJsonTypeInfoResolver.DefaultInstance;

                switch (_typeInfoResolver)
                {
                    case null:
                        // Use the default reflection-based resolver if no resolver has been specified.
                        _typeInfoResolver = defaultResolver;
                        break;

                    case JsonSerializerContext ctx when AppContextSwitchHelper.IsSourceGenReflectionFallbackEnabled:
                        // .NET 6 compatibility mode: enable fallback to reflection metadata for JsonSerializerContext
                        _effectiveJsonTypeInfoResolver = JsonTypeInfoResolver.Combine(ctx, defaultResolver);

                        if (_cachingContext is { } cachingContext)
                        {
                            // A cache has already been created by the source generator.
                            // Repeat the same configuration routine for that options instance, if different.
                            // Invalidate any cache entries that have already been stored.
                            if (cachingContext.Options != this && !cachingContext.Options._isConfiguredForJsonSerializer)
                            {
                                cachingContext.Options.ConfigureForJsonSerializer();
                            }
                            else
                            {
                                cachingContext.Clear();
                            }
                        }
                        break;
                }
            }
            else if (_typeInfoResolver is null or EmptyJsonTypeInfoResolver)
            {
                ThrowHelper.ThrowInvalidOperationException_JsonSerializerIsReflectionDisabled();
            }

            Debug.Assert(_typeInfoResolver != null);
            // NB preserve write order.
            _isReadOnly = true;
            _isConfiguredForJsonSerializer = true;
        }

        /// <summary>
        /// This flag is supplementary to <see cref="_isReadOnly"/> and is only used to keep track
        /// of source-gen reflection fallback (assuming the IsSourceGenReflectionFallbackEnabled feature switch is on).
        /// This mode necessitates running the <see cref="ConfigureForJsonSerializer"/> method even
        /// for options instances that have been marked as read-only.
        /// </summary>
        private volatile bool _isConfiguredForJsonSerializer;

        // Only populated in .NET 6 compatibility mode encoding reflection fallback in source gen
        private IJsonTypeInfoResolver? _effectiveJsonTypeInfoResolver;

        private JsonTypeInfo? GetTypeInfoNoCaching(Type type)
        {
            IJsonTypeInfoResolver? resolver = _effectiveJsonTypeInfoResolver ?? _typeInfoResolver;
            if (resolver is null)
            {
                return null;
            }

            JsonTypeInfo? info = resolver.GetTypeInfo(type, this);

            if (info != null)
            {
                if (info.Type != type)
                {
                    ThrowHelper.ThrowInvalidOperationException_ResolverTypeNotCompatible(type, info.Type);
                }

                if (info.Options != this)
                {
                    ThrowHelper.ThrowInvalidOperationException_ResolverTypeInfoOptionsNotCompatible();
                }
            }
            else
            {
                Debug.Assert(_effectiveJsonTypeInfoResolver is null, "an effective resolver always returns metadata");

                if (type == JsonTypeInfo.ObjectType)
                {
                    // If the resolver does not provide a JsonTypeInfo<object> instance, fill
                    // with the serialization-only converter to enable polymorphic serialization.
                    var converter = new SlimObjectConverter(resolver);
                    info = new JsonTypeInfo<object>(converter, this);
                }
            }

            return info;
        }

        internal JsonDocumentOptions GetDocumentOptions()
        {
            return new JsonDocumentOptions
            {
                AllowTrailingCommas = AllowTrailingCommas,
                CommentHandling = ReadCommentHandling,
                MaxDepth = MaxDepth
            };
        }

        internal JsonNodeOptions GetNodeOptions()
        {
            return new JsonNodeOptions
            {
                PropertyNameCaseInsensitive = PropertyNameCaseInsensitive
            };
        }

        internal JsonReaderOptions GetReaderOptions()
        {
            return new JsonReaderOptions
            {
                AllowTrailingCommas = AllowTrailingCommas,
                CommentHandling = ReadCommentHandling,
                MaxDepth = EffectiveMaxDepth
            };
        }

        internal JsonWriterOptions GetWriterOptions()
        {
            return new JsonWriterOptions
            {
                Encoder = Encoder,
                Indented = WriteIndented,
                IndentCharacter = IndentCharacter,
                IndentSize = IndentSize,
                MaxDepth = EffectiveMaxDepth,
#if !DEBUG
                SkipValidation = true
#endif
            };
        }

        internal void VerifyMutable()
        {
            if (_isReadOnly)
            {
                ThrowHelper.ThrowInvalidOperationException_SerializerOptionsReadOnly(_typeInfoResolver as JsonSerializerContext);
            }
        }

        private sealed class ConverterList : ConfigurationList<JsonConverter>
        {
            private readonly JsonSerializerOptions _options;

            public ConverterList(JsonSerializerOptions options, IList<JsonConverter>? source = null)
                : base(source)
            {
                _options = options;
            }

            public override bool IsReadOnly => _options.IsReadOnly;
            protected override void OnCollectionModifying() => _options.VerifyMutable();
        }

        private sealed class OptionsBoundJsonTypeInfoResolverChain : JsonTypeInfoResolverChain
        {
            private readonly JsonSerializerOptions _options;

            public OptionsBoundJsonTypeInfoResolverChain(JsonSerializerOptions options)
            {
                _options = options;
                AddFlattened(options._typeInfoResolver);
            }

            public override bool IsReadOnly => _options.IsReadOnly;

            protected override void ValidateAddedValue(IJsonTypeInfoResolver item)
            {
                if (ReferenceEquals(item, this) || ReferenceEquals(item, _options._typeInfoResolver))
                {
                    // Cannot add the instances in TypeInfoResolver or TypeInfoResolverChain to the chain itself.
                    ThrowHelper.ThrowInvalidOperationException_InvalidChainedResolver();
                }
            }

            protected override void OnCollectionModifying()
            {
                _options.VerifyMutable();

                // Collection modified by the user: replace the main
                // resolver with the resolver chain as our source of truth.
                _options._typeInfoResolver = this;
            }
        }

        [RequiresUnreferencedCode(JsonSerializer.SerializationUnreferencedCodeMessage)]
        [RequiresDynamicCode(JsonSerializer.SerializationRequiresDynamicCodeMessage)]
        private static JsonSerializerOptions GetOrCreateSingleton(
            ref JsonSerializerOptions? location,
            JsonSerializerDefaults defaults)
        {
            var options = new JsonSerializerOptions(defaults)
            {
                // Because we're marking the default instance as read-only,
                // we need to specify a resolver instance for the case where
                // reflection is disabled by default: use one that returns null for all types.

                TypeInfoResolver = JsonSerializer.IsReflectionEnabledByDefault
                    ? DefaultJsonTypeInfoResolver.DefaultInstance
                    : JsonTypeInfoResolver.Empty,

                _isReadOnly = true,
            };

            return Interlocked.CompareExchange(ref location, options, null) ?? options;
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private string DebuggerDisplay => $"TypeInfoResolver = {(TypeInfoResolver?.ToString() ?? "<null>")}, IsReadOnly = {IsReadOnly}";
    }
}
