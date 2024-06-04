// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Diagnostics;

namespace System.Text.Json.Serialization.Metadata
{
    /// <summary>
    /// Provides JSON serialization-related metadata about a type.
    /// </summary>
    /// <typeparam name="T">The generic definition of the type.</typeparam>
    public sealed partial class JsonTypeInfo<T> : JsonTypeInfo
    {
        private Action<Utf8JsonWriter, T>? _serialize;

        private Func<T>? _typedCreateObject;

        internal JsonTypeInfo(JsonConverter converter, JsonSerializerOptions options)
            : base(typeof(T), converter, options)
        {
            EffectiveConverter = converter.CreateCastingConverter<T>();
        }

        /// <summary>
        /// A Converter whose declared type always matches that of the current JsonTypeInfo.
        /// It might be the same instance as JsonTypeInfo.Converter or it could be wrapped
        /// in a CastingConverter in cases where a polymorphic converter is being used.
        /// </summary>
        internal JsonConverter<T> EffectiveConverter { get; }

        /// <summary>
        /// Gets or sets a parameterless factory to be used on deserialization.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// The <see cref="JsonTypeInfo"/> instance has been locked for further modification.
        ///
        /// -or-
        ///
        /// A parameterless factory is not supported for the current metadata <see cref="JsonTypeInfo.Kind"/>.
        /// </exception>
        /// <remarks>
        /// If set to <see langword="null" />, any attempt to deserialize instances of the given type will fail at runtime.
        ///
        /// For contracts originating from <see cref="DefaultJsonTypeInfoResolver"/> or <see cref="JsonSerializerContext"/>,
        /// types with a single default constructor or default constructors annotated with <see cref="JsonConstructorAttribute"/>
        /// will be mapped to this delegate.
        /// </remarks>
        public new Func<T>? CreateObject
        {
            get => _typedCreateObject;
            set
            {
                SetCreateObject(value);
            }
        }

        private protected override void SetCreateObject(Delegate? createObject)
        {
            Debug.Assert(createObject is null or Func<object> or Func<T>);

            VerifyMutable();

            if (Kind == JsonTypeInfoKind.None)
            {
                Debug.Assert(_createObject == null);
                Debug.Assert(_typedCreateObject == null);
                ThrowHelper.ThrowInvalidOperationException_JsonTypeInfoOperationNotPossibleForKind(Kind);
            }

            if (!Converter.SupportsCreateObjectDelegate)
            {
                Debug.Assert(_createObject is null);
                Debug.Assert(_typedCreateObject == null);
                ThrowHelper.ThrowInvalidOperationException_CreateObjectConverterNotCompatible(Type);
            }

            Func<object>? untypedCreateObject;
            Func<T>? typedCreateObject;

            if (createObject is null)
            {
                untypedCreateObject = null;
                typedCreateObject = null;
            }
            else if (createObject is Func<T> typedDelegate)
            {
                typedCreateObject = typedDelegate;
                untypedCreateObject = createObject is Func<object> untypedDelegate ? untypedDelegate : () => typedDelegate()!;
            }
            else
            {
                Debug.Assert(createObject is Func<object>);
                untypedCreateObject = (Func<object>)createObject;
                typedCreateObject = () => (T)untypedCreateObject();
            }

            _createObject = untypedCreateObject;
            _typedCreateObject = typedCreateObject;

            // Clear any data related to the previously specified ctor
            ConstructorAttributeProviderFactory = null;
            ConstructorAttributeProvider = null;

            if (CreateObjectWithArgs is not null)
            {
                _parameterInfoValuesIndex = null;
                CreateObjectWithArgs = null;
                ParameterCount = 0;

                foreach (JsonPropertyInfo propertyInfo in PropertyList)
                {
                    propertyInfo.AssociatedParameter = null;
                }
            }
        }

        /// <summary>
        /// Serializes an instance of <typeparamref name="T"/> using
        /// <see cref="JsonSourceGenerationOptionsAttribute"/> values specified at design time.
        /// </summary>
        /// <remarks>The writer is not flushed after writing.</remarks>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public Action<Utf8JsonWriter, T>? SerializeHandler
        {
            get
            {
                return _serialize;
            }
            internal set
            {
                Debug.Assert(!IsReadOnly, "We should not mutate read-only JsonTypeInfo");
                _serialize = value;
                HasSerializeHandler = value != null;
            }
        }

        private protected override JsonPropertyInfo CreatePropertyInfoForTypeInfo()
        {
            return new JsonPropertyInfo<T>(
                declaringType: typeof(T),
                declaringTypeInfo: this,
                Options)
            {
                JsonTypeInfo = this,
                IsForTypeInfo = true,
            };
        }

        private protected override JsonPropertyInfo CreateJsonPropertyInfo(JsonTypeInfo declaringTypeInfo, Type? declaringType, JsonSerializerOptions options)
        {
            return new JsonPropertyInfo<T>(declaringType ?? declaringTypeInfo.Type, declaringTypeInfo, options)
            {
                JsonTypeInfo = this
            };
        }
    }
}
