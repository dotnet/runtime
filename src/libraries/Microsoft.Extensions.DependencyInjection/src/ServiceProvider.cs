// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
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
        private readonly CallSiteValidator? _callSiteValidator;

        private readonly Func<Type, ServiceAccessor> _createServiceAccessor;

        // Internal for testing
        internal ServiceProviderEngine _engine;

        private bool _disposed;

        private readonly ConcurrentDictionary<Type, ServiceAccessor> _realizedServices;

        internal CallSiteFactory CallSiteFactory { get; }

        internal ServiceProviderEngineScope Root { get; }

        internal static bool VerifyOpenGenericServiceTrimmability { get; } =
            AppContext.TryGetSwitch("Microsoft.Extensions.DependencyInjection.VerifyOpenGenericServiceTrimmability", out bool verifyOpenGenerics) ? verifyOpenGenerics : false;

        internal static bool VerifyAotCompatibility =>
#if NETFRAMEWORK || NETSTANDARD2_0
            false;
#else
            !RuntimeFeature.IsDynamicCodeSupported;
#endif

        internal ServiceProvider(ICollection<ServiceDescriptor> serviceDescriptors, ServiceProviderOptions options)
        {
            // note that Root needs to be set before calling GetEngine(), because the engine may need to access Root
            Root = new ServiceProviderEngineScope(this, isRootScope: true);
            _engine = GetEngine();
            _createServiceAccessor = CreateServiceAccessor;
            _realizedServices = new ConcurrentDictionary<Type, ServiceAccessor>();

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
                List<Exception>? exceptions = null;
                foreach (ServiceDescriptor serviceDescriptor in serviceDescriptors)
                {
                    try
                    {
                        ValidateService(serviceDescriptor);
                    }
                    catch (Exception e)
                    {
                        exceptions ??= new List<Exception>();
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
        public object? GetService(Type serviceType) => GetService(serviceType, Root);

        internal bool IsDisposed() => _disposed;

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

        private void OnResolve(ServiceCallSite? callSite, IServiceScope scope)
        {
            if (callSite != null)
            {
                _callSiteValidator?.ValidateResolution(callSite, scope, Root);
            }
        }

        internal object? GetService(Type serviceType, ServiceProviderEngineScope serviceProviderEngineScope)
        {
            if (_disposed)
            {
                ThrowHelper.ThrowObjectDisposedException();
            }

            ServiceAccessor realizedService = _realizedServices.GetOrAdd(serviceType, _createServiceAccessor);
            OnResolve(realizedService.CallSite, serviceProviderEngineScope);
            DependencyInjectionEventSource.Log.ServiceResolved(this, serviceType);
            var result = realizedService.RealizedService.Invoke(serviceProviderEngineScope);
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
                ServiceCallSite? callSite = CallSiteFactory.GetCallSite(descriptor, new CallSiteChain());
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

        private ServiceAccessor CreateServiceAccessor(Type serviceType)
        {
            ServiceCallSite? callSite = CallSiteFactory.GetCallSite(serviceType, new CallSiteChain());
            if (callSite != null)
            {
                DependencyInjectionEventSource.Log.CallSiteBuilt(this, serviceType, callSite);
                OnCreate(callSite);

                // Optimize singleton case
                if (callSite.Cache.Location == CallSiteResultCacheLocation.Root)
                {
                    object? value = CallSiteRuntimeResolver.Instance.Resolve(callSite, Root);
                    return new ServiceAccessor { CallSite = callSite, RealizedService = scope => value };
                }

                return new ServiceAccessor { CallSite = callSite, RealizedService = _engine.RealizeService(callSite) };
            }
            return new ServiceAccessor { CallSite = callSite, RealizedService = _ => null };
        }

        internal void ReplaceServiceAccessor(ServiceCallSite callSite, Func<ServiceProviderEngineScope, object?> accessor)
        {
            _realizedServices[callSite.ServiceType] = new ServiceAccessor { CallSite = callSite, RealizedService = accessor };
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
            engine = CreateDynamicEngine();
#else
            if (RuntimeFeature.IsDynamicCodeCompiled)
            {
                engine = CreateDynamicEngine();
            }
            else
            {
                // Don't try to compile Expressions/IL if they are going to get interpreted
                engine = RuntimeServiceProviderEngine.Instance;
            }
#endif
            return engine;

            [UnconditionalSuppressMessage("AotAnalysis", "IL3050:RequiresDynamicCode",
                Justification = "CreateDynamicEngine won't be called when using NativeAOT.")] // see also https://github.com/dotnet/linker/issues/2715
            ServiceProviderEngine CreateDynamicEngine() => new DynamicServiceProviderEngine(this);
        }

        private struct ServiceAccessor
        {
            public ServiceCallSite? CallSite { get; set; }
            public Func<ServiceProviderEngineScope, object?> RealizedService { get; set; }
        }
    }
}
