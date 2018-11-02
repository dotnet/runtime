// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.Extensions.DependencyInjection.ServiceLookup
{
    internal class TransientCallSite : IServiceCallSite
    {
        internal IServiceCallSite ServiceCallSite { get; }

        public TransientCallSite(IServiceCallSite serviceCallSite)
        {
            ServiceCallSite = serviceCallSite;
        }

        public Type ServiceType => ServiceCallSite.ServiceType;
        public Type ImplementationType => ServiceCallSite.ImplementationType;
        public CallSiteKind Kind { get; } = CallSiteKind.Transient;
    }
}