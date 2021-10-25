// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection.ServiceLookup;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// The default IServiceProvider.
    /// </summary>
    public sealed class ServiceProvider : IServiceProvider, IDisposable, IAsyncDisposable
    {
        private readonly CallSiteValidator _callSiteValidator;

        private readonly Func<Type, ServiceFactory> _createServiceFactory;

        // Internal for testing
        internal ServiceProviderEngine _engine;

        private bool _disposed;

        private ConcurrentDictionary<Type, ServiceFactory> _serviceFactories;

        internal CallSiteFactory CallSiteFactory { get; }

        internal ServiceProviderEngineScope Root { get; }

        internal static bool VerifyOpenGenericServiceTrimmability { get; } =
            AppContext.TryGetSwitch("Microsoft.Extensions.DependencyInjection.VerifyOpenGenericServiceTrimmability", out bool verifyOpenGenerics) ? verifyOpenGenerics : false;

        internal ServiceProvider(ICollection<ServiceDescriptor> serviceDescriptors, ServiceProviderOptions options)
        {
            // note that Root needs to be set before calling GetEngine(), because the engine may need to access Root
            Root = new ServiceProviderEngineScope(this, isRootScope: true);
            _engine = GetEngine();
            _createServiceFactory = CreateServiceFactory;
            _serviceFactories = new ConcurrentDictionary<Type, ServiceFactory>();

            CallSiteFactory = new CallSiteFactory(serviceDescriptors);
            // The list of built in services that aren't part of the list of service descriptors
            // keep this in sync with CallSiteFactory.IsService
            CallSiteFactory.Add(typeof(IServiceProvider), new ServiceProviderCallSite());
            CallSiteFactory.Add(typeof(IServiceScopeFactory), new ConstantCallSite(typeof(IServiceScopeFactory), Root));
            CallSiteFactory.Add(typeof(IServiceProviderIsService), new ConstantCallSite(typeof(IServiceProviderIsService), CallSiteFactory));

            if (options.ValidateScopes)
            {
                _callSiteValidator = new CallSiteValidator();
            }

            if (options.ValidateOnBuild)
            {
                List<Exception> exceptions = null;
                foreach (ServiceDescriptor serviceDescriptor in serviceDescriptors)
                {
                    try
                    {
                        ValidateService(serviceDescriptor);
                    }
                    catch (Exception e)
                    {
                        exceptions = exceptions ?? new List<Exception>();
                        exceptions.Add(e);
                    }
                }

                if (exceptions != null)
                {
                    throw new AggregateException("Some services are not able to be constructed", exceptions.ToArray());
                }
            }

            DependencyInjectionEventSource.Log.ServiceProviderBuilt(this);
        }

        /// <summary>
        /// Gets the service object of the specified type.
        /// </summary>
        /// <param name="serviceType">The type of the service to get.</param>
        /// <returns>The service that was produced.</returns>
        public object GetService(Type serviceType) => GetService(serviceType, Root);

        /// <inheritdoc />
        public void Dispose()
        {
            DisposeCore();
            Root.Dispose();
        }

        /// <inheritdoc/>
        public ValueTask DisposeAsync()
        {
            DisposeCore();
            return Root.DisposeAsync();
        }

        private void DisposeCore()
        {
            _disposed = true;
            DependencyInjectionEventSource.Log.ServiceProviderDisposed(this);
        }

        private void OnCreate(ServiceCallSite callSite)
        {
            _callSiteValidator?.ValidateCallSite(callSite);
        }

        private void OnResolve(Type serviceType, IServiceScope scope)
        {
            _callSiteValidator?.ValidateResolution(serviceType, scope, Root);
        }

        internal object GetService(Type serviceType, ServiceProviderEngineScope serviceProviderEngineScope)
        {
            if (_disposed)
            {
                ThrowHelper.ThrowObjectDisposedException();
            }

            ServiceFactory factory = _serviceFactories.GetOrAdd(serviceType, _createServiceFactory);
            OnResolve(serviceType, serviceProviderEngineScope);
            DependencyInjectionEventSource.Log.ServiceResolved(this, serviceType);
            var result = factory.Create(serviceProviderEngineScope);
            System.Diagnostics.Debug.Assert(result is null || CallSiteFactory.IsService(serviceType));
            return result;
        }

        private void ValidateService(ServiceDescriptor descriptor)
        {
            if (descriptor.ServiceType.IsGenericType && !descriptor.ServiceType.IsConstructedGenericType)
            {
                return;
            }

            try
            {
                ServiceCallSite callSite = CallSiteFactory.GetCallSite(descriptor, new CallSiteChain());
                if (callSite != null)
                {
                    OnCreate(callSite);
                }
            }
            catch (Exception e)
            {
                throw new InvalidOperationException($"Error while validating the service descriptor '{descriptor}': {e.Message}", e);
            }
        }

        private ServiceFactory CreateServiceFactory(Type serviceType)
        {
            ServiceCallSite callSite = CallSiteFactory.GetCallSite(serviceType, new CallSiteChain());
            if (callSite != null)
            {
                DependencyInjectionEventSource.Log.CallSiteBuilt(this, serviceType, callSite);
                OnCreate(callSite);

                // Optimize singleton case
                if (callSite.Cache.Location == CallSiteResultCacheLocation.Root)
                {
                    object value = CallSiteRuntimeResolver.Instance.Resolve(callSite, Root);
                    return ServiceFactory.FromValue(value);
                }

                return _engine.RealizeService(callSite);
            }

            return ServiceFactory.Null;
        }

        internal void ReplaceServiceAccessor(ServiceCallSite callSite, ServiceFactory factory)
        {
            _serviceFactories[callSite.ServiceType] = factory;
        }

        internal IServiceScope CreateScope()
        {
            if (_disposed)
            {
                ThrowHelper.ThrowObjectDisposedException();
            }

            return new ServiceProviderEngineScope(this, isRootScope: false);
        }

        private ServiceProviderEngine GetEngine()
        {
            ServiceProviderEngine engine;

#if NETFRAMEWORK || NETSTANDARD2_0
            engine = new DynamicServiceProviderEngine(this);
#else
            if (RuntimeFeature.IsDynamicCodeCompiled)
            {
                engine = new DynamicServiceProviderEngine(this);
            }
            else
            {
                // Don't try to compile Expressions/IL if they are going to get interpreted
                engine = RuntimeServiceProviderEngine.Instance;
            }
#endif
            return engine;
        }
    }
}
