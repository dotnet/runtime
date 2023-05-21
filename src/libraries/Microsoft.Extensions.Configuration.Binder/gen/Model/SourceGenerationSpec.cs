// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace Microsoft.Extensions.Configuration.Binder.SourceGeneration
{
    internal sealed record SourceGenerationSpec(
        Dictionary<BinderMethodSpecifier, HashSet<TypeSpec>> RootConfigTypes,
        BinderMethodSpecifier MethodsToGen,
        HashSet<ParsableFromStringTypeSpec> PrimitivesForHelperGen,
        HashSet<string> Namespaces)
    {
        public bool HasRootMethods() =>
            ShouldEmitMethods(BinderMethodSpecifier.Get | BinderMethodSpecifier.Bind | BinderMethodSpecifier.Configure | BinderMethodSpecifier.GetValue);

        public bool ShouldEmitMethods(BinderMethodSpecifier methods) => (MethodsToGen & methods) != 0;
    }
}
