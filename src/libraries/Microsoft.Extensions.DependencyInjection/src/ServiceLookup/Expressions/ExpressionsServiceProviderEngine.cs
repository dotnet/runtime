// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Extensions.DependencyInjection.ServiceLookup
{
    internal sealed class ExpressionsServiceProviderEngine : ServiceProviderEngine
    {
        private readonly ExpressionResolverBuilder _expressionResolverBuilder;

        public ExpressionsServiceProviderEngine(ServiceProvider serviceProvider)
        {
            _expressionResolverBuilder = new ExpressionResolverBuilder(serviceProvider);
        }

        public override Func<ServiceProviderEngineScope, object> RealizeService(ServiceCallSite callSite)
        {
            return _expressionResolverBuilder.Build(callSite);
        }
    }
}
