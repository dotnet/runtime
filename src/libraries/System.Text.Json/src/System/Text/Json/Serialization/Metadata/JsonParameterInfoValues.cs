// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;

namespace System.Text.Json.Serialization.Metadata
{
    /// <summary>
    /// Provides information about a constructor parameter required for JSON deserialization.
    /// </summary>
    /// <remarks>This API is for use by the output of the System.Text.Json source generator and should not be called directly.</remarks>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public sealed class JsonParameterInfoValues
    {
        /// <summary>
        /// The name of the parameter.
        /// </summary>
        /// <remarks>This API is for use by the output of the System.Text.Json source generator and should not be called directly.</remarks>
        public string Name { get; init; } = null!;

        /// <summary>
        /// The type of the parameter.
        /// </summary>
        /// <remarks>This API is for use by the output of the System.Text.Json source generator and should not be called directly.</remarks>
        public Type ParameterType { get; init; } = null!;

        /// <summary>
        /// The zero-based position of the parameter in the formal parameter list.
        /// </summary>
        /// <remarks>This API is for use by the output of the System.Text.Json source generator and should not be called directly.</remarks>
        public int Position { get; init; }

        /// <summary>
        /// Whether a default value was specified for the parameter.
        /// </summary>
        /// <remarks>This API is for use by the output of the System.Text.Json source generator and should not be called directly.</remarks>
        public bool HasDefaultValue { get; init; }

        /// <summary>
        /// The default value of the parameter.
        /// </summary>
        /// <remarks>This API is for use by the output of the System.Text.Json source generator and should not be called directly.</remarks>
        public object? DefaultValue { get; init; }

        /// <summary>
        /// Whether the parameter allows <see langword="null"/> values.
        /// </summary>
        /// <remarks>This API is for use by the output of the System.Text.Json source generator and should not be called directly.</remarks>
        public bool IsNullable { get; init; }

        /// <summary>
        /// Whether the parameter represents a required or init-only member initializer.
        /// </summary>
        /// <remarks>This API is for use by the output of the System.Text.Json source generator and should not be called directly.</remarks>
        public bool IsMemberInitializer { get; init; }
    }
}
