// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Reflection;

namespace Microsoft.Extensions.DependencyInjection.ServiceLookup
{
    internal sealed class DecoratorCallSite : ServiceCallSite
    {
        internal ServiceCallSite InnerCallSite { get; }
        internal ConstructorInfo? DecoratorConstructor { get; }
        internal ServiceCallSite[]? ParameterCallSites { get; }
        internal int InnerServiceParameterIndex { get; }
        internal Func<IServiceProvider, object, object>? DecoratorFactory { get; }

        /// <summary>
        /// Creates a type-based decorator call site.
        /// </summary>
        public DecoratorCallSite(
            ResultCache cache,
            Type serviceType,
            ServiceCallSite innerCallSite,
            ConstructorInfo decoratorConstructor,
            ServiceCallSite[] parameterCallSites,
            int innerServiceParameterIndex,
            object? serviceKey)
            : base(cache, serviceKey)
        {
            ServiceType = serviceType;
            InnerCallSite = innerCallSite;
            DecoratorConstructor = decoratorConstructor;
            ParameterCallSites = parameterCallSites;
            InnerServiceParameterIndex = innerServiceParameterIndex;
        }

        /// <summary>
        /// Creates a factory-based decorator call site.
        /// </summary>
        public DecoratorCallSite(
            ResultCache cache,
            Type serviceType,
            ServiceCallSite innerCallSite,
            Func<IServiceProvider, object, object> decoratorFactory,
            object? serviceKey)
            : base(cache, serviceKey)
        {
            ServiceType = serviceType;
            InnerCallSite = innerCallSite;
            DecoratorFactory = decoratorFactory;
        }

        public override Type ServiceType { get; }
        public override Type? ImplementationType => DecoratorConstructor?.DeclaringType;
        public override CallSiteKind Kind { get; } = CallSiteKind.Decorator;
    }
}
