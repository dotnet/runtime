// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.Internal;

namespace Microsoft.Extensions.DependencyInjection.ServiceLookup
{
    internal class CallSiteFactory
    {
        private const int DefaultSlot = 0;
        private readonly List<ServiceDescriptor> _descriptors;
        private readonly ConcurrentDictionary<Type, ServiceCallSite> _callSiteCache = new ConcurrentDictionary<Type, ServiceCallSite>();
        private readonly Dictionary<Type, ServiceDescriptorCacheItem> _descriptorLookup = new Dictionary<Type, ServiceDescriptorCacheItem>();

        private readonly StackGuard _stackGuard;

        public CallSiteFactory(IEnumerable<ServiceDescriptor> descriptors)
        {
            _stackGuard = new StackGuard();
            _descriptors = descriptors.ToList();
            Populate();
        }

        private void Populate()
        {
            foreach (var descriptor in _descriptors)
            {
                var serviceTypeInfo = descriptor.ServiceType.GetTypeInfo();
                if (serviceTypeInfo.IsGenericTypeDefinition)
                {
                    var implementationTypeInfo = descriptor.ImplementationType?.GetTypeInfo();

                    if (implementationTypeInfo == null || !implementationTypeInfo.IsGenericTypeDefinition)
                    {
                        throw new ArgumentException(
                            Resources.FormatOpenGenericServiceRequiresOpenGenericImplementation(descriptor.ServiceType),
                            "descriptors");
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

        internal ServiceCallSite GetCallSite(Type serviceType, CallSiteChain callSiteChain)
        {
#if NETCOREAPP2_0
            return _callSiteCache.GetOrAdd(serviceType, (type, chain) => CreateCallSite(type, chain), callSiteChain);
#else
            return _callSiteCache.GetOrAdd(serviceType, type => CreateCallSite(type, callSiteChain));
#endif
        }

        internal ServiceCallSite GetCallSite(ServiceDescriptor serviceDescriptor, CallSiteChain callSiteChain)
        {
            if (_descriptorLookup.TryGetValue(serviceDescriptor.ServiceType, out var descriptor))
            {
                return TryCreateExact(serviceDescriptor, serviceDescriptor.ServiceType, callSiteChain, descriptor.GetSlot(serviceDescriptor));
            }

            Debug.Fail("_descriptorLookup didn't contain requested serviceDescriptor");
            return null;
        }

        private ServiceCallSite CreateCallSite(Type serviceType, CallSiteChain callSiteChain)
        {
            if (!_stackGuard.TryEnterOnCurrentStack())
            {
                return _stackGuard.RunOnEmptyStack((type, chain) => CreateCallSite(type, chain), serviceType, callSiteChain);
            }

            callSiteChain.CheckCircularDependency(serviceType);

            var callSite = TryCreateExact(serviceType, callSiteChain) ??
                                       TryCreateOpenGeneric(serviceType, callSiteChain) ??
                                       TryCreateEnumerable(serviceType, callSiteChain);

            _callSiteCache[serviceType] = callSite;

            return callSite;
        }

        private ServiceCallSite TryCreateExact(Type serviceType, CallSiteChain callSiteChain)
        {
            if (_descriptorLookup.TryGetValue(serviceType, out var descriptor))
            {
                return TryCreateExact(descriptor.Last, serviceType, callSiteChain, DefaultSlot);
            }

            return null;
        }

        private ServiceCallSite TryCreateOpenGeneric(Type serviceType, CallSiteChain callSiteChain)
        {
            if (serviceType.IsConstructedGenericType
                && _descriptorLookup.TryGetValue(serviceType.GetGenericTypeDefinition(), out var descriptor))
            {
                return TryCreateOpenGeneric(descriptor.Last, serviceType, callSiteChain, DefaultSlot);
            }

            return null;
        }

        private ServiceCallSite TryCreateEnumerable(Type serviceType, CallSiteChain callSiteChain)
        {
            try
            {
                callSiteChain.Add(serviceType);

                if (serviceType.IsConstructedGenericType &&
                    serviceType.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                {
                    var itemType = serviceType.GenericTypeArguments.Single();
                    var cacheLocation = CallSiteResultCacheLocation.Root;

                    var callSites = new List<ServiceCallSite>();

                    // If item type is not generic we can safely use descriptor cache
                    if (!itemType.IsConstructedGenericType &&
                        _descriptorLookup.TryGetValue(itemType, out var descriptors))
                    {
                        for (int i = 0; i < descriptors.Count; i++)
                        {
                            var descriptor = descriptors[i];

                            // Last service should get slot 0
                            var slot = descriptors.Count - i - 1;
                            // There may not be any open generics here
                            var callSite = TryCreateExact(descriptor, itemType, callSiteChain, slot);
                            Debug.Assert(callSite != null);

                            cacheLocation = GetCommonCacheLocation(cacheLocation, callSite.Cache.Location);
                            callSites.Add(callSite);
                        }
                    }
                    else
                    {
                        var slot = 0;
                        // We are going in reverse so the last service in descriptor list gets slot 0
                        for (var i = _descriptors.Count - 1; i >= 0; i--)
                        {
                            var descriptor = _descriptors[i];
                            var callSite = TryCreateExact(descriptor, itemType, callSiteChain, slot) ??
                                           TryCreateOpenGeneric(descriptor, itemType, callSiteChain, slot);
                            slot++;
                            if (callSite != null)
                            {
                                cacheLocation = GetCommonCacheLocation(cacheLocation, callSite.Cache.Location);
                                callSites.Add(callSite);
                            }
                        }

                        callSites.Reverse();
                    }


                    var resultCache = ResultCache.None;
                    if (cacheLocation == CallSiteResultCacheLocation.Scope || cacheLocation == CallSiteResultCacheLocation.Root)
                    {
                        resultCache = new ResultCache(cacheLocation, new ServiceCacheKey(serviceType, DefaultSlot));
                    }

                    return new IEnumerableCallSite(resultCache, itemType, callSites.ToArray());
                }

                return null;
            }
            finally
            {
                callSiteChain.Remove(serviceType);
            }
        }

        private CallSiteResultCacheLocation GetCommonCacheLocation(CallSiteResultCacheLocation locationA, CallSiteResultCacheLocation locationB)
        {
            return (CallSiteResultCacheLocation)Math.Max((int)locationA, (int)locationB);
        }

        private ServiceCallSite TryCreateExact(ServiceDescriptor descriptor, Type serviceType, CallSiteChain callSiteChain, int slot)
        {
            if (serviceType == descriptor.ServiceType)
            {
                ServiceCallSite callSite;
                var lifetime = new ResultCache(descriptor.Lifetime, serviceType, slot);
                if (descriptor.ImplementationInstance != null)
                {
                    callSite = new ConstantCallSite(descriptor.ServiceType, descriptor.ImplementationInstance);
                }
                else if (descriptor.ImplementationFactory != null)
                {
                    callSite = new FactoryCallSite(lifetime, descriptor.ServiceType, descriptor.ImplementationFactory);
                }
                else if (descriptor.ImplementationType != null)
                {
                    callSite = CreateConstructorCallSite(lifetime, descriptor.ServiceType, descriptor.ImplementationType, callSiteChain);
                }
                else
                {
                    throw new InvalidOperationException("Invalid service descriptor");
                }

                return callSite;
            }

            return null;
        }

        private ServiceCallSite TryCreateOpenGeneric(ServiceDescriptor descriptor, Type serviceType, CallSiteChain callSiteChain, int slot)
        {
            if (serviceType.IsConstructedGenericType &&
                serviceType.GetGenericTypeDefinition() == descriptor.ServiceType)
            {
                Debug.Assert(descriptor.ImplementationType != null, "descriptor.ImplementationType != null");
                var lifetime = new ResultCache(descriptor.Lifetime, serviceType, slot);
                var closedType = descriptor.ImplementationType.MakeGenericType(serviceType.GenericTypeArguments);
                return CreateConstructorCallSite(lifetime, serviceType, closedType, callSiteChain);
            }

            return null;
        }

        private ServiceCallSite CreateConstructorCallSite(ResultCache lifetime, Type serviceType, Type implementationType,
            CallSiteChain callSiteChain)
        {
            try
            {
                callSiteChain.Add(serviceType, implementationType);
                var constructors = implementationType.GetTypeInfo()
                    .DeclaredConstructors
                    .Where(constructor => constructor.IsPublic)
                    .ToArray();

                ServiceCallSite[] parameterCallSites = null;

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
                        return new ConstructorCallSite(lifetime, serviceType, constructor);
                    }

                    parameterCallSites = CreateArgumentCallSites(
                        serviceType,
                        implementationType,
                        callSiteChain,
                        parameters,
                        throwIfCallSiteNotFound: true);

                    return new ConstructorCallSite(lifetime, serviceType, constructor, parameterCallSites);
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
                    return new ConstructorCallSite(lifetime, serviceType, bestConstructor, parameterCallSites);
                }
            }
            finally
            {
                callSiteChain.Remove(serviceType);
            }
        }

        private ServiceCallSite[] CreateArgumentCallSites(
            Type serviceType,
            Type implementationType,
            CallSiteChain callSiteChain,
            ParameterInfo[] parameters,
            bool throwIfCallSiteNotFound)
        {
            var parameterCallSites = new ServiceCallSite[parameters.Length];
            for (var index = 0; index < parameters.Length; index++)
            {
                var parameterType = parameters[index].ParameterType;
                var callSite = GetCallSite(parameterType, callSiteChain);

                if (callSite == null && ParameterDefaultValue.TryGetDefaultValue(parameters[index], out var defaultValue))
                {
                    callSite = new ConstantCallSite(parameterType, defaultValue);
                }

                if (callSite == null)
                {
                    if (throwIfCallSiteNotFound)
                    {
                        throw new InvalidOperationException(Resources.FormatCannotResolveService(
                            parameterType,
                            implementationType));
                    }

                    return null;
                }

                parameterCallSites[index] = callSite;
            }

            return parameterCallSites;
        }


        public void Add(Type type, ServiceCallSite serviceCallSite)
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

            public int GetSlot(ServiceDescriptor descriptor)
            {
                if (descriptor == _item)
                {
                    return 0;
                }

                if (_items != null)
                {
                    var index = _items.IndexOf(descriptor);
                    if (index != -1)
                    {
                        return index + 1;
                    }
                }

                throw new InvalidOperationException("Requested service descriptor doesn't exist.");
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
