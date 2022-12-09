// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Text.Json.SourceGeneration
{
    internal sealed class PropertyInitializerGenerationSpec
    {
        public required PropertyGenerationSpec Property { get; init; }

        public required int ParameterIndex { get; init; }

        public required bool MatchesConstructorParameter { get; init; }
    }
}
