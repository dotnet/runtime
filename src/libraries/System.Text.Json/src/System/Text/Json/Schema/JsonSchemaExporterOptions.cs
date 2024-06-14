// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Nodes;

namespace System.Text.Json.Schema
{
    /// <summary>
    /// Configures the behavior of the <see cref="JsonSchemaExporter"/> APIs.
    /// </summary>
    public sealed class JsonSchemaExporterOptions
    {
        /// <summary>
        /// Gets the default configuration object used by <see cref="JsonSchemaExporter"/>.
        /// </summary>
        public static JsonSchemaExporterOptions Default { get; } = new();

        /// <summary>
        /// Determines whether non-nullable schemas should be generated for null oblivious reference types.
        /// </summary>
        /// <remarks>
        /// Defaults to <see langword="false"/>. Due to restrictions in the run-time representation of nullable reference types
        /// most occurences are null oblivious and are treated as nullable by the serializer. A notable exception to that rule
        /// are nullability annotations of field, property and constructor parameters which are represented in the contract metadata.
        /// </remarks>
        public bool TreatNullObliviousAsNonNullable { get; init; }

        /// <summary>
        /// Defines a callback that is invoked for every schema that is generated within the type graph.
        /// </summary>
        public Func<JsonSchemaExporterContext, JsonNode, JsonNode>? TransformSchemaNode { get; init; }
    }
}
