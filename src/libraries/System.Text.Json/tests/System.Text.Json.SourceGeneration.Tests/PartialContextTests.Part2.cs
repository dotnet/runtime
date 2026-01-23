// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization;

namespace System.Text.Json.SourceGeneration.Tests
{
    // Second type for testing partial contexts with
    // [JsonSerializable] attributes on multiple declarations.

    public class TypeFromPartial2
    {
        public double Value { get; set; }
        public bool IsActive { get; set; }
    }

    // Second partial declaration - declares TypeFromPartial2
    [JsonSerializable(typeof(TypeFromPartial2))]
    internal partial class MultiplePartialDeclarationsContext { }
}
