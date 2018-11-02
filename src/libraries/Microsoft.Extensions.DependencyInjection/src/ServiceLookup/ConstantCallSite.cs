// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.Extensions.DependencyInjection.ServiceLookup
{
    internal class ConstantCallSite : IServiceCallSite
    {
        internal object DefaultValue { get; }

        public ConstantCallSite(Type serviceType, object defaultValue)
        {
            DefaultValue = defaultValue;
        }

        public Type ServiceType => DefaultValue.GetType();
        public Type ImplementationType => DefaultValue.GetType();
        public CallSiteKind Kind { get; } = CallSiteKind.Constant;
    }
}
