// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
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
            foreach (ServiceDescriptor descriptor in _descriptors)
            {
                TypeInfo serviceTypeInfo = descriptor.ServiceType.GetTypeInfo();
                if (serviceTypeInfo.IsGenericTypeDefinition)
                {
                    TypeInfo implementationTypeInfo = descriptor.ImplementationType?.GetTypeInfo();

                    if (implementationTypeInfo == null || !implementationTypeInfo.IsGenericTypeDefinition)
                    {
                        throw new ArgumentException(
                            SR.Format(SR.OpenGenericServiceRequiresOpenGenericImplementation, descriptor.ServiceType),
                            "descriptors");
                    }

                    if (implementationTypeInfo.IsAbstract || implementationTypeInfo.IsInterface)
                    {
                        throw new ArgumentException(
                            SR.Format(SR.TypeCannotBeActivated, descriptor.ImplementationType, descriptor.ServiceType));
                    }
                }
                else if (descriptor.ImplementationInstance == null && descriptor.ImplementationFactory == null)
                {
                    Debug.Assert(descriptor.ImplementationType != null);
                    TypeInfo implementationTypeInfo = descriptor.ImplementationType.GetTypeInfo();

                    if (implementationTypeInfo.IsGenericTypeDefinition ||
                        implementationTypeInfo.IsAbstract ||
                        implementationTypeInfo.IsInterface)
                    {
                        throw new ArgumentException(
                            SR.Format(SR.TypeCannotBeActivated, descriptor.ImplementationType, descriptor.ServiceType));
                    }
                }

                Type cacheKey = descriptor.ServiceType;
                _descriptorLookup.TryGetValue(cacheKey, out ServiceDescriptorCacheItem cacheItem);
                _descriptorLookup[cacheKey] = cacheItem.Add(descriptor);
            }
        }

        internal ServiceCallSite GetCallSite(Type serviceType, CallSiteChain callSiteChain)
        {
            return _callSiteCache.GetOrAdd(serviceType, type => CreateCallSite(type, callSiteChain));
        }

        internal ServiceCallSite GetCallSite(ServiceDescriptor serviceDescriptor, CallSiteChain callSiteChain)
        {
            if (_descriptorLookup.TryGetValue(serviceDescriptor.ServiceType, out ServiceDescriptorCacheItem descriptor))
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

            ServiceCallSite callSite = TryCreateExact(serviceType, callSiteChain) ??
                                       TryCreateOpenGeneric(serviceType, callSiteChain) ??
                                       TryCreateEnumerable(serviceType, callSiteChain);

            _callSiteCache[serviceType] = callSite;

            return callSite;
        }

        private ServiceCallSite TryCreateExact(Type serviceType, CallSiteChain callSiteChain)
        {
            if (_descriptorLookup.TryGetValue(serviceType, out ServiceDescriptorCacheItem descriptor))
            {
                return TryCreateExact(descriptor.Last, serviceType, callSiteChain, DefaultSlot);
            }

            return null;
        }

        private ServiceCallSite TryCreateOpenGeneric(Type serviceType, CallSiteChain callSiteChain)
        {
            if (serviceType.IsConstructedGenericType
                && _descriptorLookup.TryGetValue(serviceType.GetGenericTypeDefinition(), out ServiceDescriptorCacheItem descriptor))
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
                    Type itemType = serviceType.GenericTypeArguments[0];
                    CallSiteResultCacheLocation cacheLocation = CallSiteResultCacheLocation.Root;

                    var callSites = new List<ServiceCallSite>();

                    // If item type is not generic we can safely use descriptor cache
                    if (!itemType.IsConstructedGenericType &&
                        _descriptorLookup.TryGetValue(itemType, out ServiceDescriptorCacheItem descriptors))
                    {
                        for (int i = 0; i < descriptors.Count; i++)
                        {
                            ServiceDescriptor descriptor = descriptors[i];

                            // Last service should get slot 0
                            int slot = descriptors.Count - i - 1;
                            // There may not be any open generics here
                            ServiceCallSite callSite = TryCreateExact(descriptor, itemType, callSiteChain, slot);
                            Debug.Assert(callSite != null);

                            cacheLocation = GetCommonCacheLocation(cacheLocation, callSite.Cache.Location);
                            callSites.Add(callSite);
                        }
                    }
                    else
                    {
                        int slot = 0;
                        // We are going in reverse so the last service in descriptor list gets slot 0
                        for (int i = _descriptors.Count - 1; i >= 0; i--)
                        {
                            ServiceDescriptor descriptor = _descriptors[i];
                            ServiceCallSite callSite = TryCreateExact(descriptor, itemType, callSiteChain, slot) ??
                                           TryCreateOpenGeneric(descriptor, itemType, callSiteChain, slot);

                            if (callSite != null)
                            {
                                slot++;

                                cacheLocation = GetCommonCacheLocation(cacheLocation, callSite.Cache.Location);
                                callSites.Add(callSite);
                            }
                        }

                        callSites.Reverse();
                    }


                    ResultCache resultCache = ResultCache.None;
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
                Type closedType = descriptor.ImplementationType.MakeGenericType(serviceType.GenericTypeArguments);
                return CreateConstructorCallSite(lifetime, serviceType, closedType, callSiteChain);
            }

            return null;
        }

        private ServiceCallSite CreateConstructorCallSite(
            ResultCache lifetime,
            Type serviceType,
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type implementationType,
            CallSiteChain callSiteChain)
        {
            try
            {
                callSiteChain.Add(serviceType, implementationType);
                ConstructorInfo[] constructors = implementationType.GetConstructors();

                ServiceCallSite[] parameterCallSites = null;

                if (constructors.Length == 0)
                {
                    throw new InvalidOperationException(SR.Format(SR.NoConstructorMatch, implementationType));
                }
                else if (constructors.Length == 1)
                {
                    ConstructorInfo constructor = constructors[0];
                    ParameterInfo[] parameters = constructor.GetParameters();
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
                for (int i = 0; i < constructors.Length; i++)
                {
                    ParameterInfo[] parameters = constructors[i].GetParameters();

                    ServiceCallSite[] currentParameterCallSites = CreateArgumentCallSites(
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
                                string message = string.Join(
                                    Environment.NewLine,
                                    SR.Format(SR.AmbiguousConstructorException, implementationType),
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
                        SR.Format(SR.UnableToActivateTypeException, implementationType));
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
            for (int index = 0; index < parameters.Length; index++)
            {
                Type parameterType = parameters[index].ParameterType;
                ServiceCallSite callSite = GetCallSite(parameterType, callSiteChain);

                if (callSite == null && ParameterDefaultValue.TryGetDefaultValue(parameters[index], out object defaultValue))
                {
                    callSite = new ConstantCallSite(parameterType, defaultValue);
                }

                if (callSite == null)
                {
                    if (throwIfCallSiteNotFound)
                    {
                        throw new InvalidOperationException(SR.Format(SR.CannotResolveService,
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
                    int index = _items.IndexOf(descriptor);
                    if (index != -1)
                    {
                        return index + 1;
                    }
                }

                throw new InvalidOperationException("Requested service descriptor doesn't exist.");
            }

            public ServiceDescriptorCacheItem Add(ServiceDescriptor descriptor)
            {
                var newCacheItem = default(ServiceDescriptorCacheItem);
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
