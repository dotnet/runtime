// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Text.Json.Serialization.Metadata;

namespace System.Text.Json.Serialization
{
    /// <summary>
    /// Provides metadata about a set of types that is relevant to JSON serialization.
    /// </summary>
    public abstract partial class JsonSerializerContext : IJsonTypeInfoResolver
    {
        private bool? _canUseSerializationLogic;

        private JsonSerializerOptions? _options;

        /// <summary>
        /// Gets the run time specified options of the context. If no options were passed
        /// when instanciating the context, then a new instance is bound and returned.
        /// </summary>
        /// <remarks>
        /// The options instance cannot be mutated once it is bound to the context instance.
        /// </remarks>
        public JsonSerializerOptions Options
        {
            get => _options ??= new JsonSerializerOptions { TypeInfoResolver = this, IsImmutable = true };

            internal set
            {
                Debug.Assert(!value.IsImmutable);
                value.TypeInfoResolver = this;
                value.IsImmutable = true;
                _options = value;
            }
        }

        /// <summary>
        /// Indicates whether pre-generated serialization logic for types in the context
        /// is compatible with the run time specified <see cref="JsonSerializerOptions"/>.
        /// </summary>
        internal bool CanUseSerializationLogic
        {
            get
            {
                if (!_canUseSerializationLogic.HasValue)
                {
                    if (GeneratedSerializerOptions == null)
                    {
                        _canUseSerializationLogic = false;
                    }
                    else
                    {
                        _canUseSerializationLogic =
                            // Guard against unsupported features
                            Options.Converters.Count == 0 &&
                            Options.Encoder == null &&
                            // Disallow custom number handling we'd need to honor when writing.
                            // AllowReadingFromString and Strict are fine since there's no action to take when writing.
                            (Options.NumberHandling & (JsonNumberHandling.WriteAsString | JsonNumberHandling.AllowNamedFloatingPointLiterals)) == 0 &&
                            Options.ReferenceHandlingStrategy == ReferenceHandlingStrategy.None &&
#pragma warning disable SYSLIB0020
                            !Options.IgnoreNullValues && // This property is obsolete.
#pragma warning restore SYSLIB0020

                            // Ensure options values are consistent with expected defaults.
                            Options.DefaultIgnoreCondition == GeneratedSerializerOptions.DefaultIgnoreCondition &&
                            Options.IgnoreReadOnlyFields == GeneratedSerializerOptions.IgnoreReadOnlyFields &&
                            Options.IgnoreReadOnlyProperties == GeneratedSerializerOptions.IgnoreReadOnlyProperties &&
                            Options.IncludeFields == GeneratedSerializerOptions.IncludeFields &&
                            Options.PropertyNamingPolicy == GeneratedSerializerOptions.PropertyNamingPolicy &&
                            Options.DictionaryKeyPolicy == GeneratedSerializerOptions.DictionaryKeyPolicy &&
                            Options.WriteIndented == GeneratedSerializerOptions.WriteIndented;
                    }
                }

                return _canUseSerializationLogic.Value;
            }
        }

        /// <summary>
        /// The default run time options for the context. Its values are defined at design-time via <see cref="JsonSourceGenerationOptionsAttribute"/>.
        /// </summary>
        protected abstract JsonSerializerOptions? GeneratedSerializerOptions { get; }

        /// <summary>
        /// Creates an instance of <see cref="JsonSerializerContext"/> and binds it with the indicated <see cref="JsonSerializerOptions"/>.
        /// </summary>
        /// <param name="options">The run time provided options for the context instance.</param>
        /// <remarks>
        /// If no instance options are passed, then no options are set until the context is bound using <see cref="JsonSerializerOptions.AddContext{TContext}"/>,
        /// or until <see cref="Options"/> is called, where a new options instance is created and bound.
        /// </remarks>
        protected JsonSerializerContext(JsonSerializerOptions? options)
        {
            if (options != null)
            {
                options.VerifyMutable();
                Options = options;
            }
        }

        /// <summary>
        /// Returns a <see cref="JsonTypeInfo"/> instance representing the given type.
        /// </summary>
        /// <param name="type">The type to fetch metadata about.</param>
        /// <returns>The metadata for the specified type, or <see langword="null" /> if the context has no metadata for the type.</returns>
        public abstract JsonTypeInfo? GetTypeInfo(Type type);

        JsonTypeInfo? IJsonTypeInfoResolver.GetTypeInfo(Type type, JsonSerializerOptions options)
        {
            if (options != null && options != _options)
            {
                ThrowHelper.ThrowInvalidOperationException_ResolverTypeInfoOptionsNotCompatible();
            }

            return GetTypeInfo(type);
        }
    }
}
