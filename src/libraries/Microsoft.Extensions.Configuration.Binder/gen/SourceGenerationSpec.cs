// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

namespace Microsoft.Extensions.Configuration.Binder.SourceGeneration
{
    internal sealed record SourceGenerationSpec(
        Dictionary<MethodSpecifier, HashSet<TypeSpec>> RootConfigTypes,
        MethodSpecifier MethodsToGen,
        HashSet<ParsableFromStringTypeSpec> PrimitivesForHelperGen,
        HashSet<string> Namespaces)
    {
        public bool ShouldEmitMethods(MethodSpecifier method)
                => (MethodsToGen & method) != 0;

        public HashSet<TypeSpec> GetTypesForRootMethodGen(MethodSpecifier methods)
        {
            HashSet<TypeSpec> result = new();

            foreach (MethodSpecifier method in GetFlags(methods))
            {
                if (RootConfigTypes.TryGetValue(method, out HashSet<TypeSpec>? types))
                {
                    result.UnionWith(types);
                }
            }

            return result;
        }

        private static IEnumerable<MethodSpecifier> GetFlags(MethodSpecifier input)
        {
            foreach (MethodSpecifier value in Enum.GetValues(typeof(MethodSpecifier)))
                if (input.HasFlag(value))
                    yield return value;
        }
    }
}
