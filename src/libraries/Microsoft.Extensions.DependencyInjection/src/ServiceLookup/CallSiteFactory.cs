// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Microsoft.Extensions.Internal;

namespace Microsoft.Extensions.DependencyInjection.ServiceLookup
{
    internal sealed class CallSiteFactory : IServiceProviderIsService, IServiceProviderIsKeyedService
    {
        private const int DefaultSlot = 0;
        private readonly ServiceDescriptor[] _descriptors;
        private readonly ServiceDecoration[] _decorations;
        private readonly ConcurrentDictionary<ServiceCacheKey, ServiceCallSite> _callSiteCache = new ConcurrentDictionary<ServiceCacheKey, ServiceCallSite>();
        private readonly Dictionary<ServiceIdentifier, ServiceDescriptorCacheItem> _descriptorLookup = new Dictionary<ServiceIdentifier, ServiceDescriptorCacheItem>();
        private readonly ConcurrentDictionary<ServiceIdentifier, object> _callSiteLocks = new ConcurrentDictionary<ServiceIdentifier, object>();

        private readonly StackGuard _stackGuard;

        public CallSiteFactory(ICollection<ServiceDescriptor> descriptors)
            : this(descriptors, null)
        {
        }

        public CallSiteFactory(ICollection<ServiceDescriptor> descriptors, IReadOnlyList<ServiceDecoration>? decorations)
        {
            _stackGuard = new StackGuard();
            _descriptors = new ServiceDescriptor[descriptors.Count];
            descriptors.CopyTo(_descriptors, 0);

            if (decorations is { Count: > 0 })
            {
                _decorations = new ServiceDecoration[decorations.Count];
                for (int i = 0; i < decorations.Count; i++)
                {
                    _decorations[i] = decorations[i];
                }
            }
            else
            {
                _decorations = Array.Empty<ServiceDecoration>();
            }

            Populate();
        }

        internal ServiceDescriptor[] Descriptors => _descriptors;

        private void Populate()
        {
            foreach (ServiceDescriptor descriptor in _descriptors)
            {
                Type serviceType = descriptor.ServiceType;
                if (serviceType.IsGenericTypeDefinition)
                {
                    Type? implementationType = descriptor.GetImplementationType();

                    if (implementationType == null || !implementationType.IsGenericTypeDefinition)
                    {
                        throw new ArgumentException(
                            SR.Format(SR.OpenGenericServiceRequiresOpenGenericImplementation, serviceType),
                            "descriptors");
                    }

                    if (implementationType.IsAbstract || implementationType.IsInterface)
                    {
                        throw new ArgumentException(
                            SR.Format(SR.TypeCannotBeActivated, implementationType, serviceType));
                    }

                    Type[] serviceTypeGenericArguments = serviceType.GetGenericArguments();
                    Type[] implementationTypeGenericArguments = implementationType.GetGenericArguments();
                    if (serviceTypeGenericArguments.Length != implementationTypeGenericArguments.Length)
                    {
                        throw new ArgumentException(
                            SR.Format(SR.ArityOfOpenGenericServiceNotEqualArityOfOpenGenericImplementation, serviceType, implementationType), "descriptors");
                    }

                    if (ServiceProvider.VerifyOpenGenericServiceTrimmability)
                    {
                        ValidateTrimmingAnnotations(serviceType, serviceTypeGenericArguments, implementationType, implementationTypeGenericArguments);
                    }
                }
                else if (descriptor.TryGetImplementationType(out Type? implementationType))
                {
                    Debug.Assert(implementationType != null);

                    if (implementationType.IsGenericTypeDefinition ||
                        implementationType.IsAbstract ||
                        implementationType.IsInterface)
                    {
                        throw new ArgumentException(
                            SR.Format(SR.TypeCannotBeActivated, implementationType, serviceType));
                    }
                }

                var cacheKey = ServiceIdentifier.FromDescriptor(descriptor);
                _descriptorLookup.TryGetValue(cacheKey, out ServiceDescriptorCacheItem cacheItem);
                _descriptorLookup[cacheKey] = cacheItem.Add(descriptor);
            }

            // Validate open generic decorations
            foreach (ServiceDecoration decoration in _decorations)
            {
                if (decoration.DecoratorType is not { IsGenericTypeDefinition: true } decoratorType)
                {
                    continue;
                }

                Type serviceType = decoration.ServiceType;
                if (!serviceType.IsGenericTypeDefinition)
                {
                    throw new ArgumentException(
                        SR.Format(SR.OpenGenericServiceRequiresOpenGenericImplementation, serviceType),
                        "decorations");
                }

                if (decoratorType.IsAbstract || decoratorType.IsInterface)
                {
                    throw new ArgumentException(
                        SR.Format(SR.TypeCannotBeActivated, decoratorType, serviceType));
                }

                Type[] serviceTypeGenericArguments = serviceType.GetGenericArguments();
                Type[] decoratorTypeGenericArguments = decoratorType.GetGenericArguments();
                if (serviceTypeGenericArguments.Length != decoratorTypeGenericArguments.Length)
                {
                    throw new ArgumentException(
                        SR.Format(SR.ArityOfOpenGenericServiceNotEqualArityOfOpenGenericImplementation, serviceType, decoratorType), "decorations");
                }
            }
        }

        /// <summary>
        /// Validates that two generic type definitions have compatible trimming annotations on their generic arguments.
        /// </summary>
        /// <remarks>
        /// When open generic types are used in DI, there is an error when the concrete implementation type
        /// has [DynamicallyAccessedMembers] attributes on a generic argument type, but the interface/service type
        /// doesn't have matching annotations. The problem is that the trimmer doesn't see the members that need to
        /// be preserved on the type being passed to the generic argument. But when the interface/service type also has
        /// the annotations, the trimmer will see which members need to be preserved on the closed generic argument type.
        /// </remarks>
        private static void ValidateTrimmingAnnotations(
            Type serviceType,
            Type[] serviceTypeGenericArguments,
            Type implementationType,
            Type[] implementationTypeGenericArguments)
        {
            Debug.Assert(serviceTypeGenericArguments.Length == implementationTypeGenericArguments.Length);

            for (int i = 0; i < serviceTypeGenericArguments.Length; i++)
            {
                Type serviceGenericType = serviceTypeGenericArguments[i];
                Type implementationGenericType = implementationTypeGenericArguments[i];

                DynamicallyAccessedMemberTypes serviceDynamicallyAccessedMembers = GetDynamicallyAccessedMemberTypes(serviceGenericType);
                DynamicallyAccessedMemberTypes implementationDynamicallyAccessedMembers = GetDynamicallyAccessedMemberTypes(implementationGenericType);

                if (!AreCompatible(serviceDynamicallyAccessedMembers, implementationDynamicallyAccessedMembers))
                {
                    throw new ArgumentException(SR.Format(SR.TrimmingAnnotationsDoNotMatch, implementationType.FullName, serviceType.FullName));
                }

                bool serviceHasNewConstraint = serviceGenericType.GenericParameterAttributes.HasFlag(GenericParameterAttributes.DefaultConstructorConstraint);
                bool implementationHasNewConstraint = implementationGenericType.GenericParameterAttributes.HasFlag(GenericParameterAttributes.DefaultConstructorConstraint);
                if (implementationHasNewConstraint && !serviceHasNewConstraint)
                {
                    throw new ArgumentException(SR.Format(SR.TrimmingAnnotationsDoNotMatch_NewConstraint, implementationType.FullName, serviceType.FullName));
                }
            }
        }

        private static DynamicallyAccessedMemberTypes GetDynamicallyAccessedMemberTypes(Type serviceGenericType)
        {
            foreach (CustomAttributeData attributeData in serviceGenericType.GetCustomAttributesData())
            {
                if (attributeData.AttributeType.FullName == "System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute" &&
                    attributeData.ConstructorArguments.Count == 1 &&
                    attributeData.ConstructorArguments[0].ArgumentType.FullName == "System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes")
                {
                    return (DynamicallyAccessedMemberTypes)(int)attributeData.ConstructorArguments[0].Value!;
                }
            }

            return DynamicallyAccessedMemberTypes.None;
        }

        private static bool AreCompatible(DynamicallyAccessedMemberTypes serviceDynamicallyAccessedMembers, DynamicallyAccessedMemberTypes implementationDynamicallyAccessedMembers)
        {
            // The DynamicallyAccessedMemberTypes don't need to exactly match.
            // The service type needs to preserve a superset of the members required by the implementation type.
            return serviceDynamicallyAccessedMembers.HasFlag(implementationDynamicallyAccessedMembers);
        }

        // For unit testing
        internal int? GetSlot(ServiceDescriptor serviceDescriptor)
        {
            if (_descriptorLookup.TryGetValue(ServiceIdentifier.FromDescriptor(serviceDescriptor), out ServiceDescriptorCacheItem item))
            {
                return item.GetSlot(serviceDescriptor);
            }

            return null;
        }

        internal ServiceCallSite? GetCallSite(ServiceIdentifier serviceIdentifier, CallSiteChain callSiteChain) =>
            _callSiteCache.TryGetValue(new ServiceCacheKey(serviceIdentifier, DefaultSlot), out ServiceCallSite? site) ? site :
            CreateCallSite(serviceIdentifier, callSiteChain);

        internal ServiceCallSite? GetCallSite(ServiceDescriptor serviceDescriptor, CallSiteChain callSiteChain)
        {
            var serviceIdentifier = ServiceIdentifier.FromDescriptor(serviceDescriptor);
            if (_descriptorLookup.TryGetValue(serviceIdentifier, out ServiceDescriptorCacheItem descriptor))
            {
                ServiceCallSite? callSite = TryCreateExact(serviceDescriptor, serviceIdentifier, callSiteChain, descriptor.GetSlot(serviceDescriptor));
                if (callSite != null && _decorations.Length > 0)
                {
                    callSite = ApplyDecorations(callSite, serviceIdentifier, callSiteChain);
                }
                return callSite;
            }

            Debug.Fail("_descriptorLookup didn't contain requested serviceDescriptor");
            return null;
        }

        private ServiceCallSite? CreateCallSite(ServiceIdentifier serviceIdentifier, CallSiteChain callSiteChain)
        {
            if (!_stackGuard.TryEnterOnCurrentStack())
            {
                return _stackGuard.RunOnEmptyStack(CreateCallSite, serviceIdentifier, callSiteChain);
            }

            // We need to lock the resolution process for a single service type at a time:
            // Consider the following:
            // C -> D -> A
            // E -> D -> A
            // Resolving C and E in parallel means that they will be modifying the callsite cache concurrently
            // to add the entry for C and E, but the resolution of D and A is synchronized
            // to make sure C and E both reference the same instance of the callsite.

            // This is to make sure we can safely store singleton values on the callsites themselves

            var callsiteLock = _callSiteLocks.GetOrAdd(serviceIdentifier, static _ => new object());

            lock (callsiteLock)
            {
                callSiteChain.CheckCircularDependency(serviceIdentifier);

                ServiceCallSite? callSite = TryCreateExact(serviceIdentifier, callSiteChain) ??
                                           TryCreateOpenGeneric(serviceIdentifier, callSiteChain) ??
                                           TryCreateEnumerable(serviceIdentifier, callSiteChain);

                if (callSite != null && _decorations.Length > 0)
                {
                    callSite = ApplyDecorations(callSite, serviceIdentifier, callSiteChain);
                }

                return callSite;
            }
        }

        private ServiceCallSite? TryCreateExact(ServiceIdentifier serviceIdentifier, CallSiteChain callSiteChain)
        {
            if (_descriptorLookup.TryGetValue(serviceIdentifier, out ServiceDescriptorCacheItem descriptor))
            {
                return TryCreateExact(descriptor.Last, serviceIdentifier, callSiteChain, DefaultSlot);
            }

            if (serviceIdentifier.ServiceKey != null)
            {
                // Check if there is a registration with KeyedService.AnyKey
                var catchAllIdentifier = new ServiceIdentifier(KeyedService.AnyKey, serviceIdentifier.ServiceType);
                if (_descriptorLookup.TryGetValue(catchAllIdentifier, out descriptor))
                {
                    return TryCreateExact(descriptor.Last, serviceIdentifier, callSiteChain, DefaultSlot);
                }
            }

            return null;
        }

        private ServiceCallSite? TryCreateOpenGeneric(ServiceIdentifier serviceIdentifier, CallSiteChain callSiteChain)
        {
            if (serviceIdentifier.ServiceType.IsConstructedGenericType)
            {
                ServiceIdentifier genericIdentifier = serviceIdentifier.GetGenericTypeDefinition();
                if (_descriptorLookup.TryGetValue(genericIdentifier, out ServiceDescriptorCacheItem descriptor))
                {
                    return TryCreateOpenGeneric(descriptor.Last, serviceIdentifier, callSiteChain, DefaultSlot, true);
                }

                if (serviceIdentifier.ServiceKey != null)
                {
                    // Check if there is a registration with KeyedService.AnyKey
                    var catchAllIdentifier = new ServiceIdentifier(KeyedService.AnyKey, genericIdentifier.ServiceType);
                    if (_descriptorLookup.TryGetValue(catchAllIdentifier, out descriptor))
                    {
                        return TryCreateOpenGeneric(descriptor.Last, serviceIdentifier, callSiteChain, DefaultSlot, true);
                    }
                }
            }

            return null;
        }

        private ServiceCallSite? TryCreateEnumerable(ServiceIdentifier serviceIdentifier, CallSiteChain callSiteChain)
        {
            ServiceCacheKey callSiteKey = new ServiceCacheKey(serviceIdentifier, DefaultSlot);
            if (_callSiteCache.TryGetValue(callSiteKey, out ServiceCallSite? serviceCallSite))
            {
                return serviceCallSite;
            }

            try
            {
                callSiteChain.Add(serviceIdentifier);

                var serviceType = serviceIdentifier.ServiceType;

                if (!serviceType.IsConstructedGenericType ||
                    serviceType.GetGenericTypeDefinition() != typeof(IEnumerable<>))
                {
                    return null;
                }

                Type itemType = serviceType.GenericTypeArguments[0];
                var cacheKey = new ServiceIdentifier(serviceIdentifier.ServiceKey, itemType);
                if (ServiceProvider.VerifyAotCompatibility && itemType.IsValueType)
                {
                    // NativeAOT apps are not able to make Enumerable of ValueType services
                    // since there is no guarantee the ValueType[] code has been generated.
                    throw new InvalidOperationException(SR.Format(SR.AotCannotCreateEnumerableValueType, itemType));
                }

                CallSiteResultCacheLocation cacheLocation = CallSiteResultCacheLocation.Root;
                ServiceCallSite[] callSites;

                bool isAnyKeyLookup = serviceIdentifier.ServiceKey == KeyedService.AnyKey;

                // If item type is not generic we can safely use descriptor cache
                // Special case for KeyedService.AnyKey, we don't want to check the cache because a KeyedService.AnyKey registration
                // will "hide" all the other service registration
                if (!itemType.IsConstructedGenericType &&
                    !isAnyKeyLookup &&
                    _descriptorLookup.TryGetValue(cacheKey, out ServiceDescriptorCacheItem descriptors))
                {
                    // Last service will get slot 0.
                    int slot = descriptors.Count;

                    callSites = new ServiceCallSite[descriptors.Count];
                    for (int i = 0; i < descriptors.Count; i++)
                    {
                        ServiceDescriptor descriptor = descriptors[i];

                        // There are no open generics here, so we only need to call CreateExact().
                        ServiceCallSite callSite = CreateExact(descriptor, cacheKey, callSiteChain, --slot);
                        if (_decorations.Length > 0)
                        {
                            callSite = ApplyDecorations(callSite, cacheKey, callSiteChain);
                        }
                        cacheLocation = GetCommonCacheLocation(cacheLocation, callSite.Cache.Location);
                        callSites[i] = callSite;
                    }
                }
                else
                {
                    // We need to construct a list of matching call sites in declaration order, but to ensure
                    // correct caching we must assign slots in reverse declaration order and with slots being
                    // given out first to any exact matches before any open generic matches. Therefore, we
                    // iterate over the descriptors twice in reverse, catching exact matches on the first pass
                    // and open generic matches on the second pass.

                    List<KeyValuePair<int, ServiceCallSite>> callSitesByIndex = new();
                    Dictionary<ServiceIdentifier, int>? keyedSlotAssignment = null;
                    int slot = 0;

                    // Do the exact matches first.
                    for (int i = _descriptors.Length - 1; i >= 0; i--)
                    {
                        if (KeysMatch(cacheKey.ServiceKey, _descriptors[i].ServiceKey))
                        {
                            if (ShouldCreateExact(_descriptors[i].ServiceType, cacheKey.ServiceType))
                            {
                                // For AnyKey, we want to cache based on descriptor identity, not AnyKey that cacheKey has.
                                ServiceIdentifier registrationKey = isAnyKeyLookup ? ServiceIdentifier.FromDescriptor(_descriptors[i]) : cacheKey;
                                slot = GetSlot(registrationKey);
                                ServiceCallSite callSite = CreateExact(_descriptors[i], registrationKey, callSiteChain, slot);
                                AddCallSite(callSite, i);
                                UpdateSlot(registrationKey);
                            }
                        }
                    }

                    // Do the open generic matches second.
                    for (int i = _descriptors.Length - 1; i >= 0; i--)
                    {
                        if (KeysMatch(cacheKey.ServiceKey, _descriptors[i].ServiceKey))
                        {
                            if (ShouldCreateOpenGeneric(_descriptors[i].ServiceType, cacheKey.ServiceType))
                            {
                                // For AnyKey, we want to cache based on descriptor identity, not AnyKey that cacheKey has.
                                ServiceIdentifier registrationKey = isAnyKeyLookup ? ServiceIdentifier.FromDescriptor(_descriptors[i]) : cacheKey;
                                slot = GetSlot(registrationKey);

                                // We skip open generics with incompatible constraints.
                                if (CreateOpenGeneric(_descriptors[i], registrationKey, callSiteChain, slot, false) is { } callSite)
                                {
                                    AddCallSite(callSite, i);
                                    UpdateSlot(registrationKey);
                                }
                                else if (slot == 0)
                                {
                                    // If the last registration has incompatible constraints, we still need to update the slot.
                                    // This ensures that single service resolution (GetService) will attempt to resolve using the last
                                    // registration and throw an ArgumentException, maintaining "last wins" semantics. During enumerable
                                    // resolution (GetServices), the incompatible registration is simply skipped.
                                    UpdateSlot(registrationKey);
                                }
                            }
                        }
                    }

                    callSitesByIndex.Sort((a, b) => a.Key.CompareTo(b.Key));
                    callSites = new ServiceCallSite[callSitesByIndex.Count];
                    for (var i = 0; i < callSites.Length; ++i)
                    {
                        callSites[i] = callSitesByIndex[i].Value;
                    }

                    void AddCallSite(ServiceCallSite callSite, int index)
                    {
                        if (_decorations.Length > 0)
                        {
                            callSite = ApplyDecorations(callSite, cacheKey, callSiteChain);
                        }
                        cacheLocation = GetCommonCacheLocation(cacheLocation, callSite.Cache.Location);
                        callSitesByIndex.Add(new(index, callSite));
                    }

                    int GetSlot(ServiceIdentifier key)
                    {
                        if (!isAnyKeyLookup)
                        {
                            return slot;
                        }

                        // Each unique key (including its service type) maintains its own slot counter for ordering and identity.

                        if (keyedSlotAssignment is null)
                        {
                            keyedSlotAssignment = new Dictionary<ServiceIdentifier, int>(capacity: _descriptors.Length)
                            {
                                { key, 0 }
                            };

                            return 0;
                        }

                        if (keyedSlotAssignment.TryGetValue(key, out int existingSlot))
                        {
                            return existingSlot;
                        }

                        keyedSlotAssignment.Add(key, 0);
                        return 0;
                    }

                    void UpdateSlot(ServiceIdentifier key)
                    {
                        if (!isAnyKeyLookup)
                        {
                            slot++;
                        }
                        else
                        {
                            Debug.Assert(keyedSlotAssignment is not null);
                            keyedSlotAssignment[key] = slot + 1;
                        }
                    }
                }

                ResultCache resultCache = (cacheLocation == CallSiteResultCacheLocation.Scope || cacheLocation == CallSiteResultCacheLocation.Root)
                    ? new ResultCache(cacheLocation, callSiteKey)
                    : new ResultCache(CallSiteResultCacheLocation.None, callSiteKey);

                return _callSiteCache[callSiteKey] = new IEnumerableCallSite(resultCache, itemType, callSites, serviceIdentifier.ServiceKey);
            }
            finally
            {
                callSiteChain.Remove(serviceIdentifier);
            }

            static bool KeysMatch(object? lookupKey, object? descriptorKey)
            {
                if (lookupKey == null && descriptorKey == null)
                {
                    // Both are non keyed services
                    return true;
                }

                if (lookupKey != null && descriptorKey != null)
                {
                    // Both are keyed services

                    // We don't want to return AnyKey registration, so ignore it
                    if (descriptorKey.Equals(KeyedService.AnyKey))
                        return false;

                    // Check if both keys are equal, or if the lookup key
                    // should matches all keys (except AnyKey)
                    return lookupKey.Equals(descriptorKey)
                        || lookupKey.Equals(KeyedService.AnyKey);
                }

                // One is a keyed service, one is not
                return false;
            }
        }

        private static CallSiteResultCacheLocation GetCommonCacheLocation(CallSiteResultCacheLocation locationA, CallSiteResultCacheLocation locationB)
        {
            return (CallSiteResultCacheLocation)Math.Max((int)locationA, (int)locationB);
        }

        private ServiceCallSite? TryCreateExact(ServiceDescriptor descriptor, ServiceIdentifier serviceIdentifier, CallSiteChain callSiteChain, int slot)
        {
            if (ShouldCreateExact(descriptor.ServiceType, serviceIdentifier.ServiceType))
            {
                return CreateExact(descriptor, serviceIdentifier, callSiteChain, slot);
            }

            return null;
        }

        private static bool ShouldCreateExact(Type descriptorType, Type serviceType) =>
            descriptorType == serviceType;

        private ServiceCallSite CreateExact(ServiceDescriptor descriptor, ServiceIdentifier serviceIdentifier, CallSiteChain callSiteChain, int slot)
        {
            ServiceCacheKey callSiteKey = new ServiceCacheKey(serviceIdentifier, slot);
            if (_callSiteCache.TryGetValue(callSiteKey, out ServiceCallSite? serviceCallSite))
            {
                return serviceCallSite;
            }

            ServiceCallSite callSite;
            var lifetime = new ResultCache(descriptor.Lifetime, serviceIdentifier, slot);
            if (descriptor.HasImplementationInstance())
            {
                callSite = new ConstantCallSite(descriptor.ServiceType, descriptor.GetImplementationInstance(), descriptor.ServiceKey);
            }
            else if (!descriptor.IsKeyedService && descriptor.ImplementationFactory != null)
            {
                callSite = new FactoryCallSite(lifetime, descriptor.ServiceType, descriptor.ImplementationFactory);
            }
            else if (descriptor.IsKeyedService && descriptor.KeyedImplementationFactory != null)
            {
                callSite = new FactoryCallSite(lifetime, descriptor.ServiceType, serviceIdentifier.ServiceKey!, descriptor.KeyedImplementationFactory);
            }
            else if (descriptor.HasImplementationType())
            {
                callSite = CreateConstructorCallSite(lifetime, serviceIdentifier, descriptor.GetImplementationType()!, callSiteChain);
            }
            else
            {
                throw new InvalidOperationException(SR.InvalidServiceDescriptor);
            }

            Debug.Assert(callSite != null);
            return _callSiteCache[callSiteKey] = callSite;
        }

        private ServiceCallSite? TryCreateOpenGeneric(ServiceDescriptor descriptor, ServiceIdentifier serviceIdentifier, CallSiteChain callSiteChain, int slot, bool throwOnConstraintViolation)
        {
            if (ShouldCreateOpenGeneric(descriptor.ServiceType, serviceIdentifier.ServiceType))
            {
                return CreateOpenGeneric(descriptor, serviceIdentifier, callSiteChain, slot, throwOnConstraintViolation);
            }

            return null;
        }

        private static bool ShouldCreateOpenGeneric(Type descriptorType, Type serviceType) =>
            serviceType.IsConstructedGenericType &&
            serviceType.GetGenericTypeDefinition() == descriptorType;

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2055:MakeGenericType",
            Justification = "MakeGenericType here is used to create a closed generic implementation type given the closed service type. " +
            "Trimming annotations on the generic types are verified when 'Microsoft.Extensions.DependencyInjection.VerifyOpenGenericServiceTrimmability' is set, which is set by default when PublishTrimmed=true. " +
            "That check informs developers when these generic types don't have compatible trimming annotations.")]
        [UnconditionalSuppressMessage("AotAnalysis", "IL3050:RequiresDynamicCode",
            Justification = "When ServiceProvider.VerifyAotCompatibility is true, which it is by default when PublishAot=true, " +
            "this method ensures the generic types being created aren't using ValueTypes.")]
        private ServiceCallSite? CreateOpenGeneric(ServiceDescriptor descriptor, ServiceIdentifier serviceIdentifier, CallSiteChain callSiteChain, int slot, bool throwOnConstraintViolation)
        {
            ServiceCacheKey callSiteKey = new ServiceCacheKey(serviceIdentifier, slot);
            if (_callSiteCache.TryGetValue(callSiteKey, out ServiceCallSite? serviceCallSite))
            {
                return serviceCallSite;
            }

            Type? implementationType = descriptor.GetImplementationType();
            Debug.Assert(implementationType != null, "descriptor.ImplementationType != null");
            var lifetime = new ResultCache(descriptor.Lifetime, serviceIdentifier, slot);
            Type closedType;
            try
            {
                Type[] genericTypeArguments = serviceIdentifier.ServiceType.GenericTypeArguments;
                if (ServiceProvider.VerifyAotCompatibility)
                {
                    VerifyOpenGenericAotCompatibility(serviceIdentifier.ServiceType, genericTypeArguments);
                }

                closedType = implementationType.MakeGenericType(genericTypeArguments);
            }
            catch (ArgumentException)
            {
                if (throwOnConstraintViolation)
                {
                    throw;
                }

                return null;
            }

            ConstructorCallSite site = CreateConstructorCallSite(lifetime, serviceIdentifier, closedType, callSiteChain);
            Debug.Assert(site != null);
            return _callSiteCache[callSiteKey] = site;
        }

        private ConstructorCallSite CreateConstructorCallSite(
            ResultCache lifetime,
            ServiceIdentifier serviceIdentifier,
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type implementationType,
            CallSiteChain callSiteChain)
        {
            try
            {
                callSiteChain.Add(serviceIdentifier, implementationType);
                ConstructorInfo[] constructors = implementationType.GetConstructors();

                ServiceCallSite[]? parameterCallSites = null;

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
                        return new ConstructorCallSite(lifetime, serviceIdentifier.ServiceType, constructor, serviceIdentifier.ServiceKey);
                    }

                    parameterCallSites = CreateArgumentCallSites(
                        serviceIdentifier,
                        implementationType,
                        callSiteChain,
                        parameters,
                        throwIfCallSiteNotFound: true)!;

                    return new ConstructorCallSite(lifetime, serviceIdentifier.ServiceType, constructor, parameterCallSites, serviceIdentifier.ServiceKey);
                }

                Array.Sort(constructors,
                    (a, b) => b.GetParameters().Length.CompareTo(a.GetParameters().Length));

                ConstructorInfo? bestConstructor = null;
                HashSet<Type>? bestConstructorParameterTypes = null;
                for (int i = 0; i < constructors.Length; i++)
                {
                    ParameterInfo[] parameters = constructors[i].GetParameters();

                    ServiceCallSite[]? currentParameterCallSites = CreateArgumentCallSites(
                        serviceIdentifier,
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
                                bestConstructorParameterTypes = new HashSet<Type>();
                                foreach (ParameterInfo p in bestConstructor.GetParameters())
                                {
                                    bestConstructorParameterTypes.Add(p.ParameterType);
                                }
                            }

                            foreach (ParameterInfo p in parameters)
                            {
                                if (!bestConstructorParameterTypes.Contains(p.ParameterType))
                                {
                                    // Ambiguous match exception
                                    throw new InvalidOperationException(string.Join(
                                        Environment.NewLine,
                                        SR.Format(SR.AmbiguousConstructorException, implementationType),
                                        bestConstructor,
                                        constructors[i]));
                                }
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
                    return new ConstructorCallSite(lifetime, serviceIdentifier.ServiceType, bestConstructor, parameterCallSites, serviceIdentifier.ServiceKey);
                }
            }
            finally
            {
                callSiteChain.Remove(serviceIdentifier);
            }
        }

        /// <returns>Not <b>null</b> if <b>throwIfCallSiteNotFound</b> is true</returns>
        private ServiceCallSite[]? CreateArgumentCallSites(
            ServiceIdentifier serviceIdentifier,
            Type implementationType,
            CallSiteChain callSiteChain,
            ParameterInfo[] parameters,
            bool throwIfCallSiteNotFound)
        {
            var parameterCallSites = new ServiceCallSite[parameters.Length];

            for (int index = 0; index < parameters.Length; index++)
            {
                ServiceCallSite? callSite = null;
                bool isKeyedParameter = false;
                Type parameterType = parameters[index].ParameterType;
                foreach (var attribute in parameters[index].GetCustomAttributes(true))
                {
                    if (serviceIdentifier.ServiceKey != null && attribute is ServiceKeyAttribute)
                    {
                        // Even though the parameter may be strongly typed, support 'object' if AnyKey is used.

                        if (serviceIdentifier.ServiceKey == KeyedService.AnyKey)
                        {
                            parameterType = typeof(object);
                        }
                        else if (parameterType != serviceIdentifier.ServiceKey.GetType()
                            && parameterType != typeof(object))
                        {
                            throw new InvalidOperationException(SR.InvalidServiceKeyType);
                        }

                        callSite = new ConstantCallSite(parameterType, serviceIdentifier.ServiceKey);
                        break;
                    }

                    if (attribute is FromKeyedServicesAttribute fromKeyedServicesAttribute)
                    {
                        object? serviceKey = fromKeyedServicesAttribute.LookupMode switch
                        {
                            ServiceKeyLookupMode.InheritKey => serviceIdentifier.ServiceKey,
                            ServiceKeyLookupMode.ExplicitKey => fromKeyedServicesAttribute.Key,
                            ServiceKeyLookupMode.NullKey => null,
                            _ => null
                        };

                        if (serviceKey is not null)
                        {
                            callSite = GetCallSite(new ServiceIdentifier(serviceKey, parameterType), callSiteChain);
                            isKeyedParameter = true;
                            break;
                        }
                    }
                }

                if (!isKeyedParameter)
                {
                    callSite ??= GetCallSite(ServiceIdentifier.FromServiceType(parameterType), callSiteChain);
                }

                if (callSite == null && ParameterDefaultValue.TryGetDefaultValue(parameters[index], out object? defaultValue))
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

        /// <summary>
        /// Verifies none of the generic type arguments are ValueTypes.
        /// </summary>
        /// <remarks>
        /// NativeAOT apps are not guaranteed that the native code for the closed generic of ValueType
        /// has been generated. To catch these problems early, this verification is enabled at development-time
        /// to inform the developer early that this scenario will not work once AOT'd.
        /// </remarks>
        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2055:MakeGenericType",
            Justification = "Open generic decorator types are validated at registration time.")]
        [UnconditionalSuppressMessage("AotAnalysis", "IL3050:RequiresDynamicCode",
            Justification = "Open generic decorator types are validated at registration time.")]
        private ServiceCallSite ApplyDecorations(ServiceCallSite callSite, ServiceIdentifier serviceIdentifier, CallSiteChain callSiteChain)
        {
            for (int i = 0; i < _decorations.Length; i++)
            {
                ServiceDecoration decoration = _decorations[i];

                if (!DecorationMatches(decoration, serviceIdentifier))
                {
                    continue;
                }

                // The decorator takes over the inner call site's cache — the decorated
                // result is cached as a unit with the same lifetime as the inner service.
                // The inner's cache is cleared to avoid duplicate cache entries.
                ResultCache decoratorCache = callSite.Cache;
                callSite.Cache = ResultCache.None(callSite.ServiceType, callSite.Key);

                if (decoration.DecoratorFactory is { } factory)
                {
                    // Factory-based decoration
                    callSite = new DecoratorCallSite(
                        decoratorCache,
                        serviceIdentifier.ServiceType,
                        callSite,
                        factory,
                        serviceIdentifier.ServiceKey);
                }
                else
                {
                    // Type-based decoration
                    Type decoratorType = decoration.DecoratorType!;

                    // Close generic decorator type if needed
                    if (decoratorType.IsGenericTypeDefinition)
                    {
                        decoratorType = decoratorType.MakeGenericType(serviceIdentifier.ServiceType.GenericTypeArguments);
                    }

                    // Find the best constructor and build parameter call sites
                    ConstructorInfo[] constructors = decoratorType.GetConstructors();
                    if (constructors.Length == 0)
                    {
                        throw new InvalidOperationException(SR.Format(SR.NoConstructorMatch, decoratorType));
                    }

                    // Pick the constructor with the most parameters
                    Array.Sort(constructors, (a, b) => b.GetParameters().Length.CompareTo(a.GetParameters().Length));

                    ConstructorInfo? bestCtor = null;
                    ServiceCallSite[]? bestParameterCallSites = null;
                    int bestInnerIndex = -1;

                    foreach (ConstructorInfo ctor in constructors)
                    {
                        ParameterInfo[] parameters = ctor.GetParameters();
                        var paramCallSites = new ServiceCallSite[parameters.Length];
                        int innerIndex = -1;
                        bool valid = true;

                        for (int p = 0; p < parameters.Length; p++)
                        {
                            if (innerIndex == -1 && serviceIdentifier.ServiceType.IsAssignableFrom(parameters[p].ParameterType))
                            {
                                // This parameter receives the inner service
                                innerIndex = p;
                                paramCallSites[p] = callSite; // placeholder, will be resolved at runtime
                            }
                            else
                            {
                                ServiceCallSite? paramSite = GetCallSite(ServiceIdentifier.FromServiceType(parameters[p].ParameterType), callSiteChain);
                                if (paramSite == null)
                                {
                                    if (ParameterDefaultValue.TryGetDefaultValue(parameters[p], out object? defaultValue))
                                    {
                                        paramSite = new ConstantCallSite(parameters[p].ParameterType, defaultValue);
                                    }
                                    else
                                    {
                                        valid = false;
                                        break;
                                    }
                                }
                                paramCallSites[p] = paramSite;
                            }
                        }

                        if (valid && innerIndex != -1)
                        {
                            bestCtor = ctor;
                            bestParameterCallSites = paramCallSites;
                            bestInnerIndex = innerIndex;
                            break;
                        }
                    }

                    if (bestCtor == null)
                    {
                        throw new InvalidOperationException(
                            SR.Format(SR.UnableToActivateTypeException, decoratorType));
                    }

                    callSite = new DecoratorCallSite(
                        decoratorCache,
                        serviceIdentifier.ServiceType,
                        callSite,
                        bestCtor,
                        bestParameterCallSites!,
                        bestInnerIndex,
                        serviceIdentifier.ServiceKey);
                }
            }

            return callSite;
        }

        private static bool DecorationMatches(ServiceDecoration decoration, ServiceIdentifier serviceIdentifier)
        {
            if (!object.Equals(decoration.ServiceKey, serviceIdentifier.ServiceKey))
            {
                return false;
            }

            if (decoration.ServiceType.IsGenericTypeDefinition)
            {
                return serviceIdentifier.ServiceType.IsConstructedGenericType
                    && serviceIdentifier.ServiceType.GetGenericTypeDefinition() == decoration.ServiceType;
            }

            return decoration.ServiceType == serviceIdentifier.ServiceType;
        }

        private static void VerifyOpenGenericAotCompatibility(Type serviceType, Type[] genericTypeArguments)
        {
            foreach (Type typeArg in genericTypeArguments)
            {
                if (typeArg.IsValueType)
                {
                    throw new InvalidOperationException(SR.Format(SR.AotCannotCreateGenericValueType, serviceType, typeArg));
                }
            }
        }

        public void Add(ServiceIdentifier serviceIdentifier, ServiceCallSite serviceCallSite)
        {
            _callSiteCache[new ServiceCacheKey(serviceIdentifier, DefaultSlot)] = serviceCallSite;
        }

        public bool IsService(Type serviceType) => IsService(new ServiceIdentifier(null, serviceType));

        public bool IsKeyedService(Type serviceType, object? key) => IsService(new ServiceIdentifier(key, serviceType));

        internal bool IsService(ServiceIdentifier serviceIdentifier)
        {
            var serviceType = serviceIdentifier.ServiceType;

            ArgumentNullException.ThrowIfNull(serviceType);

            // Querying for an open generic should return false (they aren't resolvable)
            if (serviceType.IsGenericTypeDefinition)
            {
                return false;
            }

            if (_descriptorLookup.ContainsKey(serviceIdentifier))
            {
                return true;
            }

            if (serviceIdentifier.ServiceKey != null && _descriptorLookup.ContainsKey(new ServiceIdentifier(KeyedService.AnyKey, serviceType)))
            {
                return true;
            }

            if (serviceType.IsConstructedGenericType && serviceType.GetGenericTypeDefinition() is Type genericDefinition)
            {
                // We special case IEnumerable since it isn't explicitly registered in the container
                // yet we can manifest instances of it when requested.
                return genericDefinition == typeof(IEnumerable<>) || _descriptorLookup.ContainsKey(serviceIdentifier.GetGenericTypeDefinition());
            }

            // These are the built in service types that aren't part of the list of service descriptors
            // If you update these make sure to also update the code in ServiceProvider.ctor
            return serviceType == typeof(IServiceProvider) ||
                   serviceType == typeof(IServiceScopeFactory) ||
                   serviceType == typeof(IServiceProviderIsService) ||
                   serviceType == typeof(IServiceProviderIsKeyedService);
        }

        private struct ServiceDescriptorCacheItem
        {
            [DisallowNull]
            private ServiceDescriptor? _item;

            [DisallowNull]
            private List<ServiceDescriptor>? _items;

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
                        return _item!;
                    }

                    return _items![index - 1];
                }
            }

            public int GetSlot(ServiceDescriptor descriptor)
            {
                if (descriptor == _item)
                {
                    return Count - 1;
                }

                if (_items != null)
                {
                    int index = _items.IndexOf(descriptor);
                    if (index != -1)
                    {
                        return _items.Count - (index + 1);
                    }
                }

                throw new InvalidOperationException(SR.ServiceDescriptorNotExist);
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
