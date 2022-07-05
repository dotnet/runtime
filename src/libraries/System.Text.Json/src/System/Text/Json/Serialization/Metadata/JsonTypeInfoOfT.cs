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
    public abstract class JsonTypeInfo<T> : JsonTypeInfo
    {
        private Action<Utf8JsonWriter, T>? _serialize;

        private Func<T>? _typedCreateObject;

        /// <summary>
        /// Function for creating object before properties are set. If set to null type is not deserializable.
        /// </summary>
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
                ThrowHelper.ThrowInvalidOperationException_JsonTypeInfoOperationNotPossibleForKindNone();
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
        }

        internal JsonTypeInfo(JsonConverter converter, JsonSerializerOptions options)
            : base(typeof(T), converter, options)
        { }

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
            private protected set
            {
                Debug.Assert(!_isConfigured, "We should not mutate configured JsonTypeInfo");
                _serialize = value;
                HasSerialize = value != null;
            }
        }

        private protected override JsonPropertyInfo CreatePropertyInfoForTypeInfo()
        {
            return new JsonPropertyInfo<T>(
                declaringType: typeof(T),
                declaringTypeInfo: null,
                Options)
            {
                DefaultConverterForType = Converter,
                JsonTypeInfo = this,
                IsForTypeInfo = true,
            };
        }

        private protected sealed override JsonPropertyInfo CreateJsonPropertyInfo(JsonTypeInfo declaringTypeInfo, JsonSerializerOptions options)
        {
            // Options on this type info might not be the same as the one passed in
            // This is because we're taking JsonTypeInfo from cache which might end up being equivalent but not same reference
            return new JsonPropertyInfo<T>(declaringTypeInfo.Type, declaringTypeInfo, options)
            {
                DefaultConverterForType = Converter,
                JsonTypeInfo = this,
            };
        }

        private protected abstract override void LateAddProperties();

        private protected void MapInterfaceTypesToCallbacks()
        {
            // Callbacks currently only supported in object kinds
            // TODO: extend to collections/dictionaries
            if (Kind == JsonTypeInfoKind.Object)
            {
                if (typeof(IJsonOnSerializing).IsAssignableFrom(typeof(T)))
                {
                    OnSerializing = static obj => ((IJsonOnSerializing)obj).OnSerializing();
                }

                if (typeof(IJsonOnSerialized).IsAssignableFrom(typeof(T)))
                {
                    OnSerialized = static obj => ((IJsonOnSerialized)obj).OnSerialized();
                }

                if (typeof(IJsonOnDeserializing).IsAssignableFrom(typeof(T)))
                {
                    OnDeserializing = static obj => ((IJsonOnDeserializing)obj).OnDeserializing();
                }

                if (typeof(IJsonOnDeserialized).IsAssignableFrom(typeof(T)))
                {
                    OnDeserialized = static obj => ((IJsonOnDeserialized)obj).OnDeserialized();
                }
            }
        }
    }
}
