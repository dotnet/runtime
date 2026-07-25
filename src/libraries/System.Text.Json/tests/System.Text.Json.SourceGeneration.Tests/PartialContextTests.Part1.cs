// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization;

namespace System.Text.Json.SourceGeneration.Tests
{
    // Test for https://github.com/dotnet/runtime/issues/99669
    // Types and first partial declaration for testing partial contexts with
    // [JsonSerializable] attributes on multiple declarations.

    public class TypeFromPartial1
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    // First partial declaration - declares TypeFromPartial1
    [JsonSerializable(typeof(TypeFromPartial1))]
    internal partial class MultiplePartialDeclarationsContext : JsonSerializerContext { }
}
