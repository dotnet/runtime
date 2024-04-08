// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using SourceGenerators;

namespace Microsoft.Extensions.Configuration.Binder.SourceGeneration
{
    public sealed record SourceGenerationSpec
    {
        public required InterceptorInfo InterceptorInfo { get; init; }
        public required BindingHelperInfo BindingHelperInfo { get; init; }
        public required ImmutableEquatableArray<TypeSpec> ConfigTypes { get; init; }
        public required bool EmitEnumParseMethod { get; set; }
        public required bool EmitGenericParseEnum { get; set; }
        public required bool EmitThrowIfNullMethod { get; set; }
    }
}
