// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization;
using SourceGenerators;

namespace System.Text.Json.SourceGeneration
{
    /// <summary>
    /// Models compile-time configuration of <see cref="JsonSourceGenerationOptionsAttribute"/>.
    /// Properties are made nullable to model the presence or absence of a given configuration.
    /// </summary>
    public sealed record SourceGenerationOptionsSpec
    {
        public required JsonSourceGenerationMode? GenerationMode { get; init; }

        public required JsonSerializerDefaults? Defaults { get; init; }

        public required bool? AllowOutOfOrderMetadataProperties { get; init; }

        public required bool? AllowTrailingCommas { get; init; }

        public required ImmutableEquatableArray<TypeRef>? Converters { get; init; }

        public required int? DefaultBufferSize { get; init; }

        public required JsonIgnoreCondition? DefaultIgnoreCondition { get; init; }

        public required JsonKnownNamingPolicy? DictionaryKeyPolicy { get; init; }

        public required bool? IgnoreReadOnlyFields { get; init; }

        public required bool? IgnoreReadOnlyProperties { get; init; }

        public required bool? IncludeFields { get; init; }

        public required int? MaxDepth { get; init; }

        public required JsonNumberHandling? NumberHandling { get; init; }

        public required JsonObjectCreationHandling? PreferredObjectCreationHandling { get; init; }

        public required bool? PropertyNameCaseInsensitive { get; init; }

        public required JsonKnownNamingPolicy? PropertyNamingPolicy { get; init; }

        public required JsonCommentHandling? ReadCommentHandling { get; init; }

        public required JsonUnknownTypeHandling? UnknownTypeHandling { get; init; }

        public required JsonUnmappedMemberHandling? UnmappedMemberHandling { get; init; }

        public required bool? UseStringEnumConverter { get; init; }

        public required bool? WriteIndented { get; init; }

        public required char? IndentCharacter { get; init; }

        public required int? IndentSize { get; init; }

        public JsonKnownNamingPolicy? GetEffectivePropertyNamingPolicy()
            => PropertyNamingPolicy ?? (Defaults is JsonSerializerDefaults.Web ? JsonKnownNamingPolicy.CamelCase : null);
    }
}
