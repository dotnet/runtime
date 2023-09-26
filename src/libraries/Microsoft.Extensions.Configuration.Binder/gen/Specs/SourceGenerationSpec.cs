// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

namespace Microsoft.Extensions.Configuration.Binder.SourceGeneration
{
    internal sealed record SourceGenerationSpec
    {
        public Dictionary<Enum, List<InterceptorLocationInfo>> InterceptionInfo { get; } = new();
        public ConfigurationBinderInterceptorInfo InterceptionInfo_ConfigBinder { get; } = new();

        public Dictionary<MethodsToGen_CoreBindingHelper, HashSet<TypeSpec>> TypesForGen_CoreBindingHelper_Methods { get; } = new();

        public HashSet<ParsableFromStringSpec> PrimitivesForHelperGen { get; } = new();
        public HashSet<string> Namespaces { get; } = new()
        {
            "System",
            "System.CodeDom.Compiler",
            "System.Globalization",
            "System.Runtime.CompilerServices",
            "Microsoft.Extensions.Configuration",
        };

        public MethodsToGen_CoreBindingHelper MethodsToGen_CoreBindingHelper { get; set; }
        public MethodsToGen_ConfigurationBinder MethodsToGen_ConfigurationBinder { get; set; }
        public MethodsToGen_Extensions_OptionsBuilder MethodsToGen_OptionsBuilderExt { get; set; }
        public MethodsToGen_Extensions_ServiceCollection MethodsToGen_ServiceCollectionExt { get; set; }
    }
}
