// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Concurrent;

namespace Microsoft.Extensions.DependencyInjection.ServiceLookup
{
    internal sealed class CallSiteValidator: CallSiteVisitor<CallSiteValidator.CallSiteValidatorState, Type>
    {
        // Keys are services being resolved via GetService, values - first scoped service in their call site tree
        private readonly ConcurrentDictionary<Type, Type> _scopedServices = new ConcurrentDictionary<Type, Type>();

        public void ValidateCallSite(ServiceCallSite callSite)
        {
            Type scoped = VisitCallSite(callSite, default);
            if (scoped != null)
            {
                _scopedServices[callSite.ServiceType] = scoped;
            }
        }

        public void ValidateResolution(Type serviceType, IServiceScope scope, IServiceScope rootScope)
        {
            if (ReferenceEquals(scope, rootScope)
                && _scopedServices.TryGetValue(serviceType, out Type scopedService))
            {
                if (serviceType == scopedService)
                {
                    throw new InvalidOperationException(
                        SR.Format(SR.DirectScopedResolvedFromRootException, serviceType,
                            nameof(ServiceLifetime.Scoped).ToLowerInvariant()));
                }

                throw new InvalidOperationException(
                    SR.Format(SR.ScopedResolvedFromRootException,
                        serviceType,
                        scopedService,
                        nameof(ServiceLifetime.Scoped).ToLowerInvariant()));
            }
        }

        protected override Type VisitConstructor(ConstructorCallSite constructorCallSite, CallSiteValidatorState state)
        {
            Type result = null;
            foreach (ServiceCallSite parameterCallSite in constructorCallSite.ParameterCallSites)
            {
                Type scoped =  VisitCallSite(parameterCallSite, state);
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
            foreach (ServiceCallSite serviceCallSite in enumerableCallSite.ServiceCallSites)
            {
                Type scoped = VisitCallSite(serviceCallSite, state);
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
                throw new InvalidOperationException(SR.Format(SR.ScopedInSingletonException,
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
