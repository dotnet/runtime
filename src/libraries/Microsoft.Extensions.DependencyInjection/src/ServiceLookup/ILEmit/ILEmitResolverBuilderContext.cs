// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace Microsoft.Extensions.DependencyInjection.ServiceLookup
{
    internal class ILEmitResolverBuilderContext
    {
        public ILGenerator Generator { get; set; }
        public List<object> Constants { get; set; }
        public List<Func<IServiceProvider, object>> Factories { get; set; }
    }
}