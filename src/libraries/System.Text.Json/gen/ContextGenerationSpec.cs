// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Text.Json.Reflection;
using System.Diagnostics;

namespace System.Text.Json.SourceGeneration
{
    /// <summary>
    /// Represents the set of input types and options needed to provide an
    /// implementation for a user-provided JsonSerializerContext-derived type.
    /// </summary>
    [DebuggerDisplay("ContextTypeRef={ContextTypeRef}")]
    internal sealed class ContextGenerationSpec
    {
        public JsonSourceGenerationOptionsAttribute GenerationOptions { get; init; }

        public Type ContextType { get; init; }

        public List<TypeGenerationSpec> RootSerializableTypes { get; } = new();

        public HashSet<TypeGenerationSpec>? ImplicitlyRegisteredTypes { get; } = new();

        public List<string> ContextClassDeclarationList { get; init; }

        /// <summary>
        /// Types that we have initiated serialization metadata generation for. A type may be discoverable in the object graph,
        /// but not reachable for serialization (e.g. it is [JsonIgnore]'d); thus we maintain a separate cache.
        /// </summary>
        public HashSet<TypeGenerationSpec> TypesWithMetadataGenerated { get; } = new();

        /// <summary>
        /// Cache of runtime property names (statically determined) found accross the object graph of the JsonSerializerContext.
        /// </summary>
        public HashSet<string> RuntimePropertyNames { get; } = new();

        public string ContextTypeRef => ContextType.GetCompilableName();
    }
}
