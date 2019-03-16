// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;

namespace Microsoft.Extensions.DependencyInjection.ServiceLookup
{
    internal class CallSiteValidator: CallSiteVisitor<CallSiteValidator.CallSiteValidatorState, Type>
    {
        // Keys are services being resolved via GetService, values - first scoped service in their call site tree
        private readonly ConcurrentDictionary<Type, Type> _scopedServices = new ConcurrentDictionary<Type, Type>();

        public void ValidateCallSite(ServiceCallSite callSite)
        {
            var scoped = VisitCallSite(callSite, default);
            if (scoped != null)
            {
                _scopedServices[callSite.ServiceType] = scoped;
            }
        }

        public void ValidateResolution(Type serviceType, IServiceScope scope, IServiceScope rootScope)
        {
            if (ReferenceEquals(scope, rootScope)
                && _scopedServices.TryGetValue(serviceType, out var scopedService))
            {
                if (serviceType == scopedService)
                {
                    throw new InvalidOperationException(
                        Resources.FormatDirectScopedResolvedFromRootException(serviceType,
                            nameof(ServiceLifetime.Scoped).ToLowerInvariant()));
                }

                throw new InvalidOperationException(
                    Resources.FormatScopedResolvedFromRootException(
                        serviceType,
                        scopedService,
                        nameof(ServiceLifetime.Scoped).ToLowerInvariant()));
            }
        }

        protected override Type VisitConstructor(ConstructorCallSite constructorCallSite, CallSiteValidatorState state)
        {
            Type result = null;
            foreach (var parameterCallSite in constructorCallSite.ParameterCallSites)
            {
                var scoped =  VisitCallSite(parameterCallSite, state);
                if (result == null)
                {
                    result = scoped;
                }
            }
            return result;
        }

        protected override Type VisitIEnumerable(IEnumerableCallSite enumerableCallSite,
            CallSiteValidatorState state)
        {
            Type result = null;
            foreach (var serviceCallSite in enumerableCallSite.ServiceCallSites)
            {
                var scoped = VisitCallSite(serviceCallSite, state);
                if (result == null)
                {
                    result = scoped;
                }
            }
            return result;
        }

        protected override Type VisitRootCache(ServiceCallSite singletonCallSite, CallSiteValidatorState state)
        {
            state.Singleton = singletonCallSite;
            return VisitCallSiteMain(singletonCallSite, state);
        }

        protected override Type VisitScopeCache(ServiceCallSite scopedCallSite, CallSiteValidatorState state)
        {
            // We are fine with having ServiceScopeService requested by singletons
            if (scopedCallSite is ServiceScopeFactoryCallSite)
            {
                return null;
            }
            if (state.Singleton != null)
            {
                throw new InvalidOperationException(Resources.FormatScopedInSingletonException(
                    scopedCallSite.ServiceType,
                    state.Singleton.ServiceType,
                    nameof(ServiceLifetime.Scoped).ToLowerInvariant(),
                    nameof(ServiceLifetime.Singleton).ToLowerInvariant()
                    ));
            }

            VisitCallSiteMain(scopedCallSite, state);
            return scopedCallSite.ServiceType;
        }

        protected override Type VisitConstant(ConstantCallSite constantCallSite, CallSiteValidatorState state) => null;

        protected override Type VisitServiceProvider(ServiceProviderCallSite serviceProviderCallSite, CallSiteValidatorState state) => null;

        protected override Type VisitServiceScopeFactory(ServiceScopeFactoryCallSite serviceScopeFactoryCallSite, CallSiteValidatorState state) => null;

        protected override Type VisitFactory(FactoryCallSite factoryCallSite, CallSiteValidatorState state) => null;

        internal struct CallSiteValidatorState
        {
            public ServiceCallSite Singleton { get; set; }
        }
    }
}