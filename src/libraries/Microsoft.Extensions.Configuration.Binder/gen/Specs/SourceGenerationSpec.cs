// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using SourceGenerators;

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

        public required ImmutableEquatableArray<string> TypeNamespaces { get; init; }
        public required ImmutableEquatableArray<TypeSpec> TypeList { get; init; }

        public required ImmutableEquatableArray<TypeWithChildrenSpec> TypesForGen_ConfigurationBinder_Bind_instance { get; init; }
        public required ImmutableEquatableArray<TypeWithChildrenSpec> TypesForGen_ConfigurationBinder_Bind_instance_BinderOptions { get; init; }
        public required ImmutableEquatableArray<TypeWithChildrenSpec> TypesForGen_ConfigurationBinder_Bind_key_instance { get; init; }

        public required bool GraphContainsEnum { get; init; }
        public required ImmutableEquatableArray<TypeSpec> TypesForGen_CoreBindingHelper_BindCoreUntyped { get; init; }
        public required ImmutableEquatableArray<TypeSpec> TypesForGen_CoreBindingHelper_GetCore { get; init; }
        public required ImmutableEquatableArray<TypeSpec> TypesForGen_CoreBindingHelper_GetValueCore { get; init; }
        public required ImmutableEquatableArray<TypeWithChildrenSpec> TypesForGen_CoreBindingHelper_BindCore { get; init; }
        public required ImmutableEquatableArray<ObjectSpec> TypesForGen_CoreBindingHelper_Initialize { get; init; }
        public required ImmutableEquatableArray<ParsableFromStringSpec> TypesForGen_CoreBindingHelper_ParsePrimitive { get; init; }

        // TODO: add ImmutableEquatableDictionary to be supplied by the parser.
        // https://github.com/dotnet/runtime/issues/89318
        public Dictionary<TypeRef, TypeSpec> GetTypeIndex() => TypeList.ToDictionary(t => t.TypeRef);
    }
}
