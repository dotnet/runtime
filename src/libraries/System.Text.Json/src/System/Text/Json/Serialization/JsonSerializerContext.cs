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
        internal JsonSerializerOptions? _options;

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
        /// The default run-time options for the context. It's values are defined at design-time via <see cref="JsonSerializerOptionsAttribute"/>.
        /// </summary>
        internal JsonSerializerOptions? DefaultOptions { get; }

        /// <summary>
        /// Creates an instance of <see cref="JsonSerializerContext"/> and binds it with the indicated <see cref="JsonSerializerOptions"/>.
        /// </summary>
        /// <param name="instanceOptions">The run-time provided options for the context instance.</param>
        /// <param name="defaultOptions">The default run-time options for the context. It's values are defined at design-time via <see cref="JsonSerializerOptionsAttribute"/>.</param>
        /// <remarks>
        /// If no instance options are passed, then no options are set until the context is bound using <see cref="JsonSerializerOptions.AddContext{TContext}"/>,
        /// or until <see cref="Options"/> is called, where a new options instance is created and bound.
        /// </remarks>
        protected JsonSerializerContext(JsonSerializerOptions? instanceOptions, JsonSerializerOptions? defaultOptions)
        {
            DefaultOptions = defaultOptions;

            if (instanceOptions != null)
            {
                if (instanceOptions._context != null)
                {
                    ThrowHelper.ThrowInvalidOperationException_JsonSerializerOptionsAlreadyBoundToContext();
                }

                _options = instanceOptions;
                instanceOptions._context = this;
            }
        }

        /// <summary>
        /// Returns a <see cref="JsonTypeInfo"/> instance representing the given type.
        /// </summary>
        /// <param name="type">The type to fetch metadata about.</param>
        /// <returns>Should return null if the context has no metadata for the type.</returns>
        public abstract JsonTypeInfo? GetTypeInfo(Type type);
    }
}
