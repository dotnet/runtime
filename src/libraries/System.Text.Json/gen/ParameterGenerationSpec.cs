// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace System.Text.Json.SourceGeneration
{
    internal class ParameterGenerationSpec
    {
        public TypeGenerationSpec TypeGenerationSpec { get; init; }

        public ParameterInfo ParameterInfo { get; init; }
    }
}
