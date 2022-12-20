// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Extensions.Hosting.Internal
{
    internal sealed class ServiceFactoryAdapter<TContainerBuilder> : IServiceFactoryAdapter where TContainerBuilder : notnull
    {
        private IServiceProviderFactory<TContainerBuilder>? _serviceProviderFactory;
        private readonly Func<HostBuilderContext>? _contextResolver;
        private Func<HostBuilderContext, IServiceProviderFactory<TContainerBuilder>>? _factoryResolver;

        public ServiceFactoryAdapter(IServiceProviderFactory<TContainerBuilder> serviceProviderFactory)
        {
            ThrowHelper.ThrowIfNull(serviceProviderFactory);

            _serviceProviderFactory = serviceProviderFactory;
        }

        public ServiceFactoryAdapter(Func<HostBuilderContext> contextResolver, Func<HostBuilderContext, IServiceProviderFactory<TContainerBuilder>> factoryResolver)
        {
            ThrowHelper.ThrowIfNull(contextResolver);
            ThrowHelper.ThrowIfNull(factoryResolver);

            _contextResolver = contextResolver;
            _factoryResolver = factoryResolver;
        }

        public object CreateBuilder(IServiceCollection services)
        {
            if (_serviceProviderFactory == null)
            {
                Debug.Assert(_factoryResolver != null && _contextResolver != null);
                _serviceProviderFactory = _factoryResolver(_contextResolver());

                if (_serviceProviderFactory == null)
                {
                    throw new InvalidOperationException(SR.ResolverReturnedNull);
                }
            }
            return _serviceProviderFactory.CreateBuilder(services);
        }

        public IServiceProvider CreateServiceProvider(object containerBuilder)
        {
            if (_serviceProviderFactory == null)
            {
                throw new InvalidOperationException(SR.CreateBuilderCallBeforeCreateServiceProvider);
            }

            return _serviceProviderFactory.CreateServiceProvider((TContainerBuilder)containerBuilder);
        }
    }
}
