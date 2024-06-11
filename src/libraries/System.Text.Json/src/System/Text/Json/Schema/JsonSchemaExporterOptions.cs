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
        /// Defines a callback that is invoked for every schema that is generated within the type graph.
        /// </summary>
        public Action<JsonSchemaExporterContext, JsonObject>? OnSchemaNodeGenerated { get; init; }
    }
}
