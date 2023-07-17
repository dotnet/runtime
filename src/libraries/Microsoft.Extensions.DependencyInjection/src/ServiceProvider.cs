// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection.ServiceLookup;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// The default IServiceProvider.
    /// </summary>
    [DebuggerDisplay("{DebuggerToString(),nq}")]
    [DebuggerTypeProxy(typeof(ServiceProviderDebugView))]
    public sealed class ServiceProvider : IServiceProvider, IKeyedServiceProvider, IDisposable, IAsyncDisposable
    {
        private readonly CallSiteValidator? _callSiteValidator;

        private readonly Func<ServiceIdentifier, Func<ServiceProviderEngineScope, object?>> _createServiceAccessor;

        // Internal for testing
        internal ServiceProviderEngine _engine;

        private bool _disposed;

        private readonly ConcurrentDictionary<ServiceIdentifier, Func<ServiceProviderEngineScope, object?>> _realizedServices;

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
            _realizedServices = new ConcurrentDictionary<ServiceIdentifier, Func<ServiceProviderEngineScope, object?>>();

            CallSiteFactory = new CallSiteFactory(serviceDescriptors);
            // The list of built in services that aren't part of the list of service descriptors
            // keep this in sync with CallSiteFactory.IsService
            CallSiteFactory.Add(ServiceIdentifier.FromServiceType(typeof(IServiceProvider)), new ServiceProviderCallSite());
            CallSiteFactory.Add(ServiceIdentifier.FromServiceType(typeof(IServiceScopeFactory)), new ConstantCallSite(typeof(IServiceScopeFactory), Root));
            CallSiteFactory.Add(ServiceIdentifier.FromServiceType(typeof(IServiceProviderIsService)), new ConstantCallSite(typeof(IServiceProviderIsService), CallSiteFactory));
            CallSiteFactory.Add(ServiceIdentifier.FromServiceType(typeof(IServiceProviderIsKeyedService)), new ConstantCallSite(typeof(IServiceProviderIsKeyedService), CallSiteFactory));

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
        public object? GetService(Type serviceType) => GetService(ServiceIdentifier.FromServiceType(serviceType), Root);

        public object? GetKeyedService(Type serviceType, object? serviceKey)
            => GetService(new ServiceIdentifier(serviceKey, serviceType), Root);

        public object GetRequiredKeyedService(Type serviceType, object? serviceKey)
        {
            object? service = GetKeyedService(serviceType, serviceKey);
            if (service == null)
            {
                throw new InvalidOperationException(SR.Format(SR.NoServiceRegistered, serviceType));
            }
            return service;
        }

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

        private void OnResolve(ServiceCallSite callSite, IServiceScope scope)
        {
            _callSiteValidator?.ValidateResolution(callSite, scope, Root);
        }

        internal object? GetService(ServiceIdentifier serviceIdentifier, ServiceProviderEngineScope serviceProviderEngineScope)
        {
            if (_disposed)
            {
                ThrowHelper.ThrowObjectDisposedException();
            }

            Func<ServiceProviderEngineScope, object?> realizedService = _realizedServices.GetOrAdd(serviceIdentifier, _createServiceAccessor);
            var result = realizedService.Invoke(serviceProviderEngineScope);
            System.Diagnostics.Debug.Assert(result is null || CallSiteFactory.IsService(serviceIdentifier));
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

        private Func<ServiceProviderEngineScope, object?> CreateServiceAccessor(ServiceIdentifier serviceIdentifier)
        {
            ServiceCallSite? callSite = CallSiteFactory.GetCallSite(serviceIdentifier, new CallSiteChain());
            if (callSite != null)
            {
                DependencyInjectionEventSource.Log.CallSiteBuilt(this, serviceIdentifier.ServiceType, callSite);
                OnCreate(callSite);

                // Optimize singleton case
                if (callSite.Cache.Location == CallSiteResultCacheLocation.Root)
                {
                    object? value = CallSiteRuntimeResolver.Instance.Resolve(callSite, Root);
                    return scope =>
                    {
                        DependencyInjectionEventSource.Log.ServiceResolved(this, serviceIdentifier.ServiceType);
                        return value;
                    };
                }

                Func<ServiceProviderEngineScope, object?> realizedService = _engine.RealizeService(callSite);
                return scope =>
                {
                    OnResolve(callSite, scope);
                    DependencyInjectionEventSource.Log.ServiceResolved(this, serviceIdentifier.ServiceType);
                    return realizedService(scope);
                };
            }

            return _ => null;
        }

        internal void ReplaceServiceAccessor(ServiceCallSite callSite, Func<ServiceProviderEngineScope, object?> accessor)
        {
            _realizedServices[new ServiceIdentifier(callSite.Key, callSite.ServiceType)] = accessor;
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

        private string DebuggerToString() => Root.DebuggerToString();

        internal sealed class ServiceProviderDebugView
        {
            private readonly ServiceProvider _serviceProvider;

            public ServiceProviderDebugView(ServiceProvider serviceProvider)
            {
                _serviceProvider = serviceProvider;
            }

            public List<ServiceDescriptor> ServiceDescriptors => new List<ServiceDescriptor>(_serviceProvider.Root.RootProvider.CallSiteFactory.Descriptors);
            public List<object> Disposables => new List<object>(_serviceProvider.Root.Disposables);
            public bool Disposed => _serviceProvider.Root.Disposed;
            public bool IsScope => !_serviceProvider.Root.IsRootScope;
        }
    }
}
