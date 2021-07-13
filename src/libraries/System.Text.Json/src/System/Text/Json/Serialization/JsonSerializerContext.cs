// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Text.Json.Serialization.Metadata;

namespace System.Text.Json.Serialization
{
    /// <summary>
    /// Provides metadata about a set of types that is relevant to JSON serialization.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public abstract partial class JsonSerializerContext
    {
        private bool? _canUseSerializationLogic;
        private JsonSerializerOptions? _defaultOptions;

        internal JsonSerializerOptions? _options;

        /// <summary>
        /// Indicates whether pre-generated serialization logic for types in the context
        /// is compatible with the run-time specified <see cref="JsonSerializerOptions"/>.
        /// </summary>
        internal bool CanUseSerializationLogic
        {
            get
            {
                if (!_canUseSerializationLogic.HasValue)
                {
                    if (_defaultOptions == null)
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
                            Options.DefaultIgnoreCondition == _defaultOptions.DefaultIgnoreCondition &&
                            Options.IgnoreReadOnlyFields == _defaultOptions.IgnoreReadOnlyFields &&
                            Options.IgnoreReadOnlyProperties == _defaultOptions.IgnoreReadOnlyProperties &&
                            Options.IncludeFields == _defaultOptions.IncludeFields &&
                            Options.PropertyNamingPolicy == _defaultOptions.PropertyNamingPolicy &&
                            Options.WriteIndented == _defaultOptions.WriteIndented;
                    }
                }

                return _canUseSerializationLogic.Value;
            }
        }

        /// <summary>
        /// Gets the run-time specified options of the context. If no options were passed
        /// when instanciating the context, then a new instance is bound and returned.
        /// </summary>
        /// <remarks>
        /// The instance cannot be mutated once it is bound with the context instance.
        /// </remarks>
        public JsonSerializerOptions Options
        {
            get
            {
                if (_options == null)
                {
                    _options = new JsonSerializerOptions();
                    _options._context = this;
                }

                return _options;
            }
        }

        /// <summary>
        /// Creates an instance of <see cref="JsonSerializerContext"/> and binds it with the indicated <see cref="JsonSerializerOptions"/>.
        /// </summary>
        /// <param name="instanceOptions">The run-time provided options for the context instance.</param>
        /// <param name="defaultOptions">The default run-time options for the context. It's values are defined at design-time via <see cref="JsonSourceGenerationOptionsAttribute"/>.</param>
        /// <remarks>
        /// If no instance options are passed, then no options are set until the context is bound using <see cref="JsonSerializerOptions.AddContext{TContext}"/>,
        /// or until <see cref="Options"/> is called, where a new options instance is created and bound.
        /// </remarks>
        protected JsonSerializerContext(JsonSerializerOptions? instanceOptions, JsonSerializerOptions? defaultOptions)
        {
            if (instanceOptions != null)
            {
                if (instanceOptions._context != null)
                {
                    ThrowHelper.ThrowInvalidOperationException_JsonSerializerOptionsAlreadyBoundToContext();
                }

                _options = instanceOptions;
                instanceOptions._context = this;
            }

            _defaultOptions = defaultOptions;
        }

        /// <summary>
        /// Returns a <see cref="JsonTypeInfo"/> instance representing the given type.
        /// </summary>
        /// <param name="type">The type to fetch metadata about.</param>
        /// <returns>Should return null if the context has no metadata for the type.</returns>
        public abstract JsonTypeInfo? GetTypeInfo(Type type);
    }
}
