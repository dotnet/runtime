// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.Extensions.Internal;

namespace Microsoft.Extensions.DependencyInjection.ServiceLookup
{
    internal class CallSiteFactory
    {
        private readonly List<ServiceDescriptor> _descriptors;
        private readonly Dictionary<Type, IServiceCallSite> _callSiteCache = new Dictionary<Type, IServiceCallSite>();
        private readonly Dictionary<Type, ServiceDescriptorCacheItem> _descriptorLookup = new Dictionary<Type, ServiceDescriptorCacheItem>();

        public CallSiteFactory(IEnumerable<ServiceDescriptor> descriptors)
        {
            _descriptors = descriptors.ToList();
            Populate(descriptors);
        }

        private void Populate(IEnumerable<ServiceDescriptor> descriptors)
        {
            foreach (var descriptor in descriptors)
            {
                var serviceTypeInfo = descriptor.ServiceType.GetTypeInfo();
                if (serviceTypeInfo.IsGenericTypeDefinition)
                {
                    var implementationTypeInfo = descriptor.ImplementationType?.GetTypeInfo();

                    if (implementationTypeInfo == null || !implementationTypeInfo.IsGenericTypeDefinition)
                    {
                        throw new ArgumentException(
                            Resources.FormatOpenGenericServiceRequiresOpenGenericImplementation(descriptor.ServiceType),
                            nameof(descriptors));
                    }

                    if (implementationTypeInfo.IsAbstract || implementationTypeInfo.IsInterface)
                    {
                        throw new ArgumentException(
                            Resources.FormatTypeCannotBeActivated(descriptor.ImplementationType, descriptor.ServiceType));
                    }
                }
                else if (descriptor.ImplementationInstance == null && descriptor.ImplementationFactory == null)
                {
                    Debug.Assert(descriptor.ImplementationType != null);
                    var implementationTypeInfo = descriptor.ImplementationType.GetTypeInfo();

                    if (implementationTypeInfo.IsGenericTypeDefinition ||
                        implementationTypeInfo.IsAbstract ||
                        implementationTypeInfo.IsInterface)
                    {
                        throw new ArgumentException(
                            Resources.FormatTypeCannotBeActivated(descriptor.ImplementationType, descriptor.ServiceType));
                    }
                }

                var cacheKey = descriptor.ServiceType;
                _descriptorLookup.TryGetValue(cacheKey, out var cacheItem);
                _descriptorLookup[cacheKey] = cacheItem.Add(descriptor);
            }
        }

        internal IServiceCallSite CreateCallSite(Type serviceType, CallSiteChain callSiteChain)
        {
            lock (_callSiteCache)
            {
                if (_callSiteCache.TryGetValue(serviceType, out var cachedCallSite))
                {
                    return cachedCallSite;
                }

                IServiceCallSite callSite;
                try
                {
                    callSiteChain.CheckCircularDependency(serviceType);

                    callSite = TryCreateExact(serviceType, callSiteChain) ??
                               TryCreateOpenGeneric(serviceType, callSiteChain) ??
                               TryCreateEnumerable(serviceType, callSiteChain);
                }
                finally
                {
                    callSiteChain.Remove(serviceType);
                }

                _callSiteCache[serviceType] = callSite;

                return callSite;
            }
        }

        private IServiceCallSite TryCreateExact(Type serviceType, CallSiteChain callSiteChain)
        {
            if (_descriptorLookup.TryGetValue(serviceType, out var descriptor))
            {
                return TryCreateExact(descriptor.Last, serviceType, callSiteChain);
            }

            return null;
        }

        private IServiceCallSite TryCreateOpenGeneric(Type serviceType, CallSiteChain callSiteChain)
        {
            if (serviceType.IsConstructedGenericType
                && _descriptorLookup.TryGetValue(serviceType.GetGenericTypeDefinition(), out var descriptor))
            {
                return TryCreateOpenGeneric(descriptor.Last, serviceType, callSiteChain);
            }

            return null;
        }

        private IServiceCallSite TryCreateEnumerable(Type serviceType, CallSiteChain callSiteChain)
        {
            if (serviceType.IsConstructedGenericType &&
                serviceType.GetGenericTypeDefinition() == typeof(IEnumerable<>))
            {
                var itemType = serviceType.GenericTypeArguments.Single();
                callSiteChain.Add(serviceType);

                var callSites = new List<IServiceCallSite>();

                // If item type is not generic we can safely use descriptor cache
                if (!itemType.IsConstructedGenericType &&
                    _descriptorLookup.TryGetValue(itemType, out var descriptors))
                {
                    for (int i = 0; i < descriptors.Count; i++)
                    {
                        var descriptor = descriptors[i];

                        // There may not be any open generics here
                        var callSite = TryCreateExact(descriptor, itemType, callSiteChain);
                        Debug.Assert(callSite != null);

                        callSites.Add(callSite);
                    }
                }
                else
                {
                    foreach (var descriptor in _descriptors)
                    {
                        var callSite = TryCreateExact(descriptor, itemType, callSiteChain) ??
                                       TryCreateOpenGeneric(descriptor, itemType, callSiteChain);

                        if (callSite != null)
                        {
                            callSites.Add(callSite);
                        }
                    }

                }

                return new IEnumerableCallSite(itemType, callSites.ToArray());
            }

            return null;
        }

        private IServiceCallSite TryCreateExact(ServiceDescriptor descriptor, Type serviceType, CallSiteChain callSiteChain)
        {
            if (serviceType == descriptor.ServiceType)
            {
                IServiceCallSite callSite;
                if (descriptor.ImplementationInstance != null)
                {
                    callSite = new ConstantCallSite(descriptor.ServiceType, descriptor.ImplementationInstance);
                }
                else if (descriptor.ImplementationFactory != null)
                {
                    callSite = new FactoryCallSite(descriptor.ServiceType, descriptor.ImplementationFactory);
                }
                else if (descriptor.ImplementationType != null)
                {
                    callSite = CreateConstructorCallSite(descriptor.ServiceType, descriptor.ImplementationType, callSiteChain);
                }
                else
                {
                    throw new InvalidOperationException("Invalid service descriptor");
                }

                return ApplyLifetime(callSite, descriptor, descriptor.Lifetime);
            }

            return null;
        }

        private IServiceCallSite TryCreateOpenGeneric(ServiceDescriptor descriptor, Type serviceType, CallSiteChain callSiteChain)
        {
            if (serviceType.IsConstructedGenericType &&
                serviceType.GetGenericTypeDefinition() == descriptor.ServiceType)
            {
                Debug.Assert(descriptor.ImplementationType != null, "descriptor.ImplementationType != null");

                var closedType = descriptor.ImplementationType.MakeGenericType(serviceType.GenericTypeArguments);
                var constructorCallSite = CreateConstructorCallSite(serviceType, closedType, callSiteChain);

                return ApplyLifetime(constructorCallSite, Tuple.Create(descriptor, serviceType), descriptor.Lifetime);
            }

            return null;
        }

        private IServiceCallSite ApplyLifetime(IServiceCallSite serviceCallSite, object cacheKey, ServiceLifetime descriptorLifetime)
        {
            if (serviceCallSite is ConstantCallSite)
            {
                return serviceCallSite;
            }

            switch (descriptorLifetime)
            {
                case ServiceLifetime.Transient:
                    return new TransientCallSite(serviceCallSite);
                case ServiceLifetime.Scoped:
                    return new ScopedCallSite(serviceCallSite, cacheKey);
                case ServiceLifetime.Singleton:
                    return new SingletonCallSite(serviceCallSite, cacheKey);
                default:
                    throw new ArgumentOutOfRangeException(nameof(descriptorLifetime));
            }
        }

        private IServiceCallSite CreateConstructorCallSite(Type serviceType, Type implementationType, CallSiteChain callSiteChain)
        {
            callSiteChain.Add(serviceType, implementationType);

            var constructors = implementationType.GetTypeInfo()
                .DeclaredConstructors
                .Where(constructor => constructor.IsPublic)
                .ToArray();

            IServiceCallSite[] parameterCallSites = null;

            if (constructors.Length == 0)
            {
                throw new InvalidOperationException(Resources.FormatNoConstructorMatch(implementationType));
            }
            else if (constructors.Length == 1)
            {
                var constructor = constructors[0];
                var parameters = constructor.GetParameters();
                if (parameters.Length == 0)
                {
                    return new CreateInstanceCallSite(serviceType, implementationType);
                }

                parameterCallSites = CreateArgumentCallSites(
                    serviceType,
                    implementationType,
                    callSiteChain,
                    parameters,
                    throwIfCallSiteNotFound: true);

                return new ConstructorCallSite(serviceType, constructor, parameterCallSites);
            }

            Array.Sort(constructors,
                (a, b) => b.GetParameters().Length.CompareTo(a.GetParameters().Length));

            ConstructorInfo bestConstructor = null;
            HashSet<Type> bestConstructorParameterTypes = null;
            for (var i = 0; i < constructors.Length; i++)
            {
                var parameters = constructors[i].GetParameters();

                var currentParameterCallSites = CreateArgumentCallSites(
                    serviceType,
                    implementationType,
                    callSiteChain,
                    parameters,
                    throwIfCallSiteNotFound: false);

                if (currentParameterCallSites != null)
                {
                    if (bestConstructor == null)
                    {
                        bestConstructor = constructors[i];
                        parameterCallSites = currentParameterCallSites;
                    }
                    else
                    {
                        // Since we're visiting constructors in decreasing order of number of parameters,
                        // we'll only see ambiguities or supersets once we've seen a 'bestConstructor'.

                        if (bestConstructorParameterTypes == null)
                        {
                            bestConstructorParameterTypes = new HashSet<Type>(
                                bestConstructor.GetParameters().Select(p => p.ParameterType));
                        }

                        if (!bestConstructorParameterTypes.IsSupersetOf(parameters.Select(p => p.ParameterType)))
                        {
                            // Ambiguous match exception
                            var message = string.Join(
                                Environment.NewLine,
                                Resources.FormatAmbiguousConstructorException(implementationType),
                                bestConstructor,
                                constructors[i]);
                            throw new InvalidOperationException(message);
                        }
                    }
                }
            }

            if (bestConstructor == null)
            {
                throw new InvalidOperationException(
                    Resources.FormatUnableToActivateTypeException(implementationType));
            }
            else
            {
                Debug.Assert(parameterCallSites != null);
                return parameterCallSites.Length == 0 ?
                    (IServiceCallSite)new CreateInstanceCallSite(serviceType, implementationType) :
                    new ConstructorCallSite(serviceType, bestConstructor, parameterCallSites);
            }
        }

        private IServiceCallSite[] CreateArgumentCallSites(
            Type serviceType,
            Type implementationType,
            CallSiteChain callSiteChain,
            ParameterInfo[] parameters,
            bool throwIfCallSiteNotFound)
        {
            var parameterCallSites = new IServiceCallSite[parameters.Length];
            for (var index = 0; index < parameters.Length; index++)
            {
                var callSite = CreateCallSite(parameters[index].ParameterType, callSiteChain);

                if (callSite == null && ParameterDefaultValue.TryGetDefaultValue(parameters[index], out var defaultValue))
                {
                    callSite = new ConstantCallSite(serviceType, defaultValue);
                }

                if (callSite == null)
                {
                    if (throwIfCallSiteNotFound)
                    {
                        throw new InvalidOperationException(Resources.FormatCannotResolveService(
                            parameters[index].ParameterType,
                            implementationType));
                    }

                    return null;
                }

                parameterCallSites[index] = callSite;
            }

            return parameterCallSites;
        }


        public void Add(Type type, IServiceCallSite serviceCallSite)
        {
            _callSiteCache[type] = serviceCallSite;
        }

        private struct ServiceDescriptorCacheItem
        {
            private ServiceDescriptor _item;
            private List<ServiceDescriptor> _items;

            public ServiceDescriptor Last
            {
                get
                {
                    if (_items != null && _items.Count > 0)
                    {
                        return _items[_items.Count - 1];
                    }

                    Debug.Assert(_item != null);
                    return _item;
                }
            }

            public int Count
            {
                get
                {
                    if (_item == null)
                    {
                        Debug.Assert(_items == null);
                        return 0;
                    }

                    return 1 + (_items?.Count ?? 0);
                }
            }

            public ServiceDescriptor this[int index]
            {
                get
                {
                    if (index >= Count)
                    {
                        throw new ArgumentOutOfRangeException(nameof(index));
                    }

                    if (index == 0)
                    {
                        return _item;
                    }

                    return _items[index - 1];
                }
            }

            public ServiceDescriptorCacheItem Add(ServiceDescriptor descriptor)
            {
                var newCacheItem = new ServiceDescriptorCacheItem();
                if (_item == null)
                {
                    Debug.Assert(_items == null);
                    newCacheItem._item = descriptor;
                }
                else
                {
                    newCacheItem._item = _item;
                    newCacheItem._items = _items ?? new List<ServiceDescriptor>();
                    newCacheItem._items.Add(descriptor);
                }
                return newCacheItem;
            }
        }
    }
}
