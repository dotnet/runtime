// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

namespace Microsoft.Extensions.Configuration.Binder.SourceGeneration
{
    internal sealed partial record CoreBindingHelperMethodSpec : MethodSpec
    {
        private Dictionary<MethodSpecifier, HashSet<TypeSpec>> TypesForCoreBindingMethodGen { get; } = new();

        private MethodSpecifier _methodsToGen;

        public CoreBindingHelperMethodSpec(SourceGenSpec spec) : base(spec) { }

        [Flags]
        public enum MethodSpecifier
        {
            None = 0x0,
            BindCore = 0x1,
            BindCoreUntyped = 0x2,
            GetCore = 0x4,
            GetValueCore = 0x8,
            Initialize = 0x10,
        }

        public bool ShouldEmitHasChildren { get; set; }

        public HashSet<ParsableFromStringSpec> PrimitivesForHelperGen { get; } = new();

        public HashSet<string> TypeNamespaces { get; } = new() { "Microsoft.Extensions.Configuration", "System.Globalization" };

        public void RegisterTypeForBindCoreUntypedGen(TypeSpec type)
        {
            RegisterTypeForMethodGen(MethodSpecifier.BindCore, type);
            RegisterTypeForMethodGen(MethodSpecifier.BindCoreUntyped, type);
        }

        public void RegisterTypeForMethodGen(MethodSpecifier method, TypeSpec type)
        {
            if (!TypesForCoreBindingMethodGen.TryGetValue(method, out HashSet<TypeSpec>? types))
            {
                TypesForCoreBindingMethodGen[method] = types = new HashSet<TypeSpec>();
            }

            types.Add(type);
            _methodsToGen |= method;
        }

        public override void RegisterInvocation(Parser parser, BinderInvocation invocation)
            => throw new InvalidOperationException();
    }
}
