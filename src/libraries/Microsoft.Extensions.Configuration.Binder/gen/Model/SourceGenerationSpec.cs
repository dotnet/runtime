// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace Microsoft.Extensions.Configuration.Binder.SourceGeneration
{
    internal sealed record SourceGenerationSpec
    {
        public Dictionary<MethodsToGen_CoreBindingHelper, HashSet<TypeSpec>> TypesForGen_CoreBindingHelper_Methods { get; } = new();
        public Dictionary<MethodsToGen_ConfigurationBinder, HashSet<TypeSpec>> TypesForGen_ConfigurationBinder_BindMethods { get; } = new();

        public HashSet<ParsableFromStringSpec> PrimitivesForHelperGen { get; } = new();
        public HashSet<string> TypeNamespaces { get; } = new() { "Microsoft.Extensions.Configuration", "System.Globalization" };

        public MethodsToGen_CoreBindingHelper MethodsToGen_CoreBindingHelper { get; set; }
        public MethodsToGen_ConfigurationBinder MethodsToGen_ConfigurationBinder { get; set; }
        public MethodsToGen_Extensions_OptionsBuilder MethodsToGen_OptionsBuilderExt { get; set; }
        public MethodsToGen_Extensions_ServiceCollection MethodsToGen_ServiceCollectionExt { get; set; }

        public bool ShouldEmitHasChildren { get; set; }
    }
}
