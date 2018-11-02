// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Runtime.ExceptionServices;

namespace Microsoft.Extensions.DependencyInjection.ServiceLookup
{
    internal class CreateInstanceCallSite : IServiceCallSite
    {
        public Type ServiceType { get; }

        public Type ImplementationType { get; }
        public CallSiteKind Kind { get; } = CallSiteKind.CreateInstance;

        public CreateInstanceCallSite(Type serviceType, Type implementationType)
        {
            ServiceType = serviceType;
            ImplementationType = implementationType;
        }
    }
}
