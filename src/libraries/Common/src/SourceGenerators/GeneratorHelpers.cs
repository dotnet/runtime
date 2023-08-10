// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace SourceGenerators
{
    public static class GeneratorHelpers
    {
        public static string MakeNameUnique(ref string name) => name += $"_{new Random().Next():X8}";
    }
}
