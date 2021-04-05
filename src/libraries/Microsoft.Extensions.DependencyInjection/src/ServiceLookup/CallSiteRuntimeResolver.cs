// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Diagnostics;

namespace Microsoft.Extensions.DependencyInjection.ServiceLookup
{
    internal sealed class CallSiteRuntimeResolver : CallSiteVisitor<RuntimeResolverContext, object>
    {
        public object Resolve(ServiceCallSite callSite, ServiceProviderEngineScope scope)
        {
            return VisitCallSite(callSite, new RuntimeResolverContext
            {
                Scope = scope
            });
        }

        protected override object VisitDisposeCache(ServiceCallSite transientCallSite, RuntimeResolverContext context)
        {
            return context.Scope.CaptureDisposable(VisitCallSiteMain(transientCallSite, context));
        }

        protected override object VisitConstructor(ConstructorCallSite constructorCallSite, RuntimeResolverContext context)
        {
            object[] parameterValues;
            if (constructorCallSite.ParameterCallSites.Length == 0)
            {
                parameterValues = Array.Empty<object>();
            }
            else
            {
                parameterValues = new object[constructorCallSite.ParameterCallSites.Length];
                for (int index = 0; index < parameterValues.Length; index++)
                {
                    parameterValues[index] = VisitCallSite(constructorCallSite.ParameterCallSites[index], context);
                }
            }

#if NETSTANDARD2_1
            return constructorCallSite.ConstructorInfo.Invoke(BindingFlags.DoNotWrapExceptions, binder: null, parameters: parameterValues, culture: null);
#else
            try
            {
                return constructorCallSite.ConstructorInfo.Invoke(parameterValues);
            }
            catch (Exception ex) when (ex.InnerException != null)
            {
                ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
                // The above line will always throw, but the compiler requires we throw explicitly.
                throw;
            }
#endif
        }

        protected override object VisitRootCache(ServiceCallSite callSite, RuntimeResolverContext context)
        {
            var lockType = RuntimeResolverLock.Root;
            bool lockTaken = false;

            // using more granular locking (per singleton) for the root
            Monitor.Enter(callSite, ref lockTaken);
            try
            {
                return ResolveService(callSite, context, lockType, serviceProviderEngine: context.Scope.Engine.Root);
            }
            finally
            {
                if (lockTaken)
                {
                    Monitor.Exit(callSite);
                }
            }
        }

        protected override object VisitScopeCache(ServiceCallSite callSite, RuntimeResolverContext context)
        {
            // Check if we are in the situation where scoped service was promoted to singleton
            // and we need to lock the root
            return context.Scope == context.Scope.Engine.Root ?
                VisitRootCache(callSite, context) :
                VisitCache(callSite, context, context.Scope, RuntimeResolverLock.Scope);
        }

        private object VisitCache(ServiceCallSite callSite, RuntimeResolverContext context, ServiceProviderEngineScope serviceProviderEngine, RuntimeResolverLock lockType)
        {
            bool lockTaken = false;
            object sync = serviceProviderEngine.Sync;

            // Taking locks only once allows us to fork resolution process
            // on another thread without causing the deadlock because we
            // always know that we are going to wait the other thread to finish before
            // releasing the lock
            if ((context.AcquiredLocks & lockType) == 0)
            {
                Monitor.Enter(sync, ref lockTaken);
            }

            try
            {
                return ResolveService(callSite, context, lockType, serviceProviderEngine);
            }
            finally
            {
                if (lockTaken)
                {
                    Monitor.Exit(sync);
                }
            }
        }

        private object ResolveService(ServiceCallSite callSite, RuntimeResolverContext context, RuntimeResolverLock lockType, ServiceProviderEngineScope serviceProviderEngine)
        {
            IDictionary<ServiceCacheKey, object> resolvedServices = serviceProviderEngine.ResolvedServices;

            // Note: This method has already taken lock by the caller for resolution and access synchronization.
            // For root: uses a concurrent dictionary and takes a per singleton lock for resolution.
            // For scoped: takes a dictionary as both a resolution lock and a dictionary access lock.
            Debug.Assert(
                (lockType == RuntimeResolverLock.Root && resolvedServices is ConcurrentDictionary<ServiceCacheKey, object>) ||
                (lockType == RuntimeResolverLock.Scope && Monitor.IsEntered(serviceProviderEngine.Sync)));

            if (resolvedServices.TryGetValue(callSite.Cache.Key, out object resolved))
            {
                return resolved;
            }

            resolved = VisitCallSiteMain(callSite, new RuntimeResolverContext
            {
                Scope = serviceProviderEngine,
                AcquiredLocks = context.AcquiredLocks | lockType
            });
            serviceProviderEngine.CaptureDisposable(resolved);
            resolvedServices.Add(callSite.Cache.Key, resolved);
            return resolved;
        }

        protected override object VisitConstant(ConstantCallSite constantCallSite, RuntimeResolverContext context)
        {
            return constantCallSite.DefaultValue;
        }

        protected override object VisitServiceProvider(ServiceProviderCallSite serviceProviderCallSite, RuntimeResolverContext context)
        {
            return context.Scope;
        }

        protected override object VisitServiceScopeFactory(ServiceScopeFactoryCallSite serviceScopeFactoryCallSite, RuntimeResolverContext context)
        {
            return context.Scope.Engine;
        }

        protected override object VisitIEnumerable(IEnumerableCallSite enumerableCallSite, RuntimeResolverContext context)
        {
            var array = Array.CreateInstance(
                enumerableCallSite.ItemType,
                enumerableCallSite.ServiceCallSites.Length);

            for (int index = 0; index < enumerableCallSite.ServiceCallSites.Length; index++)
            {
                object value = VisitCallSite(enumerableCallSite.ServiceCallSites[index], context);
                array.SetValue(value, index);
            }
            return array;
        }

        protected override object VisitFactory(FactoryCallSite factoryCallSite, RuntimeResolverContext context)
        {
            return factoryCallSite.Factory(context.Scope);
        }
    }

    internal struct RuntimeResolverContext
    {
        public ServiceProviderEngineScope Scope { get; set; }

        public RuntimeResolverLock AcquiredLocks { get; set; }
    }

    [Flags]
    internal enum RuntimeResolverLock
    {
        Scope = 1,
        Root = 2
    }
}
