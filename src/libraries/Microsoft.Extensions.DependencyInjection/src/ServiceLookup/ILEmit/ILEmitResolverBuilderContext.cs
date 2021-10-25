// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace Microsoft.Extensions.DependencyInjection.ServiceLookup
{
    internal sealed class ILEmitResolverBuilderContext
    {
        public TypeBuilder TypeBuilder { get; set; }
        public ILGenerator Generator { get; set; }
        public List<KeyValuePair<string, object>> Fields { get; set; }
        public HashSet<Assembly> Assemblies { get; set; }
        public Stack<Type> Types { get; set; } = new();
    }
}
