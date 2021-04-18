// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace Microsoft.Extensions.DependencyInjection.ServiceLookup
{
    internal sealed class ILEmitResolverBuilderContext
    {
        public ILGenerator Generator { get; set; }
        public List<object> Constants { get; set; }
        public List<Func<IServiceProvider, object>> Factories { get; set; }
    }
}
