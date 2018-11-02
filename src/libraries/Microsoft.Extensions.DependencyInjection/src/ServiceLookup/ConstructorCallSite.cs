// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Reflection;

namespace Microsoft.Extensions.DependencyInjection.ServiceLookup
{
    internal class ConstructorCallSite : IServiceCallSite
    {
        internal ConstructorInfo ConstructorInfo { get; }
        internal IServiceCallSite[] ParameterCallSites { get; }

        public ConstructorCallSite(Type serviceType, ConstructorInfo constructorInfo, IServiceCallSite[] parameterCallSites)
        {
            ServiceType = serviceType;
            ConstructorInfo = constructorInfo;
            ParameterCallSites = parameterCallSites;
        }

        public Type ServiceType { get; }

        public Type ImplementationType => ConstructorInfo.DeclaringType;
        public CallSiteKind Kind { get; } = CallSiteKind.Constructor;
    }
}
