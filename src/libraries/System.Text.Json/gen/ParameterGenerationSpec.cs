// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;

namespace System.Text.Json.SourceGeneration
{
    internal sealed class ParameterGenerationSpec
    {
        public required TypeGenerationSpec TypeGenerationSpec { get; init; }

        public required ParameterInfo ParameterInfo { get; init; }

        public required int ParameterIndex { get; init; }
    }
}
