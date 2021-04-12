// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Diagnostics;

namespace System.Text.Json.Serialization.Metadata
{
    /// <summary>
    /// Provides metadata about a set of types that is relevant to JSON serialization.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public abstract partial class JsonSerializerContext
    {
        internal JsonSerializerOptions? _options;

        /// <summary>
        /// Gets the run-time specified options of the context.
        /// </summary>
        /// <remarks>
        /// The instance cannot be mutated once it is bounded with the context instance.
        /// </remarks>
        public JsonSerializerOptions Options
        {
            get
            {
                Debug.Assert(_options != null);
                return _options;
            }
        }

        /// <summary>
        /// Creates an instance of <see cref="JsonSerializerContext"/> and binds it with the indicated <see cref="JsonSerializerOptions"/>.
        /// If no options are provided, then a new instance with default options is created and bound.
        /// </summary>
        /// <param name="options">The run-time provided options for the context instance.</param>
        protected JsonSerializerContext(JsonSerializerOptions? options)
        {
            if (options == null)
            {
                options = new JsonSerializerOptions();
            }
            else if (options._context != null)
            {
                ThrowHelper.ThrowInvalidOperationException_JsonSerializerOptionsAlreadyBoundToContext();
            }

            options._context = this;
            _options = options;
        }

        /// <summary>
        /// Returns a <see cref="JsonTypeInfo"/> instance representing the given type.
        /// </summary>
        /// <param name="type">The type to fetch metadata about.</param>
        /// <returns>Should return null if the context has no metadata for the type.</returns>
        public abstract JsonTypeInfo? GetTypeInfo(Type type);
    }
}
