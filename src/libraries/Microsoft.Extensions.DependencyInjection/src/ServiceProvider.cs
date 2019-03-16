// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection.ServiceLookup;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// The default IServiceProvider.
    /// </summary>
    public sealed class ServiceProvider : IServiceProvider, IDisposable, IServiceProviderEngineCallback
#if DISPOSE_ASYNC
        , IAsyncDisposable
#endif
    {
        private readonly IServiceProviderEngine _engine;

        private readonly CallSiteValidator _callSiteValidator;

        internal ServiceProvider(IEnumerable<ServiceDescriptor> serviceDescriptors, ServiceProviderOptions options)
        {
            IServiceProviderEngineCallback callback = null;
            if (options.ValidateScopes)
            {
                callback = this;
                _callSiteValidator = new CallSiteValidator();
            }

            switch (options.Mode)
            {
                case ServiceProviderMode.Default:
#if !NETCOREAPP
                    _engine = new DynamicServiceProviderEngine(serviceDescriptors, callback);
#else
                    if (RuntimeFeature.IsSupported("IsDynamicCodeCompiled"))
                    {
                        _engine = new DynamicServiceProviderEngine(serviceDescriptors, callback);
                    }
                    else
                    {
                        // Don't try to compile Expressions/IL if they are going to get interpreted
                        _engine = new RuntimeServiceProviderEngine(serviceDescriptors, callback);
                    }
#endif
                    break;
                case ServiceProviderMode.Dynamic:
                    _engine = new DynamicServiceProviderEngine(serviceDescriptors, callback);
                    break;
                case ServiceProviderMode.Runtime:
                    _engine = new RuntimeServiceProviderEngine(serviceDescriptors, callback);
                    break;
#if IL_EMIT
                case ServiceProviderMode.ILEmit:
                    _engine = new ILEmitServiceProviderEngine(serviceDescriptors, callback);
                    break;
#endif
                case ServiceProviderMode.Expressions:
                    _engine = new ExpressionsServiceProviderEngine(serviceDescriptors, callback);
                    break;
                default:
                    throw new NotSupportedException(nameof(options.Mode));
            }

            if (options.ValidateOnBuild)
            {
                List<Exception> exceptions = null;
                foreach (var serviceDescriptor in serviceDescriptors)
                {
                    try
                    {
                        _engine.ValidateService(serviceDescriptor);
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
        }

        /// <summary>
        /// Gets the service object of the specified type.
        /// </summary>
        /// <param name="serviceType"></param>
        /// <returns></returns>
        public object GetService(Type serviceType) => _engine.GetService(serviceType);

        /// <inheritdoc />
        public void Dispose()
        {
            _engine.Dispose();
        }

        void IServiceProviderEngineCallback.OnCreate(ServiceCallSite callSite)
        {
            _callSiteValidator.ValidateCallSite(callSite);
        }

        void IServiceProviderEngineCallback.OnResolve(Type serviceType, IServiceScope scope)
        {
            _callSiteValidator.ValidateResolution(serviceType, scope, _engine.RootScope);
        }

#if DISPOSE_ASYNC
        /// <inheritdoc/>
        public ValueTask DisposeAsync()
        {
            return _engine.DisposeAsync();
        }
#endif
    }
}
