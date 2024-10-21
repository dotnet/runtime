// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis;

namespace System.Private.CoreLib.Generators.Models
{
    internal sealed class EventMethodArgument
    {
        public string Name { get; set; }

        public int Index { get; set; }

        public string TypeName { get; set; }

        public SpecialType SpecialType { get; set; }
    }
}
