// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Converts <see cref="ServiceDecoration"/> entries into standard factory-based
    /// <see cref="ServiceDescriptor"/> entries using the keyed-services pattern.
    /// </summary>
    /// <remarks>
    /// This is used as a fallback for DI containers that do not natively support decoration.
    /// Open generic decorations cannot be materialized and will cause an <see cref="InvalidOperationException"/>.
    /// </remarks>
    public static class DecorationMaterializer
    {
        /// <summary>
        /// Converts decorations into standard factory-based <see cref="ServiceDescriptor"/> entries
        /// and replaces matching descriptors in the service collection.
        /// </summary>
        /// <param name="services">The service collection to modify.</param>
        /// <param name="decorations">The decorations to materialize.</param>
        public static void Materialize(IServiceCollection services, IList<ServiceDecoration> decorations)
        {
            if (decorations.Count == 0)
            {
                return;
            }

            for (int d = 0; d < decorations.Count; d++)
            {
                ServiceDecoration decoration = decorations[d];

                if (decoration.ServiceType.IsGenericTypeDefinition)
                {
                    throw new InvalidOperationException(
                        $"Open generic decoration for '{decoration.ServiceType}' cannot be materialized. " +
                        $"Use the built-in container or a container that implements ISupportServiceDecoration<TContainerBuilder>.");
                }

                bool found = false;

                for (int i = services.Count - 1; i >= 0; i--)
                {
                    ServiceDescriptor descriptor = services[i];

                    if (descriptor.ServiceType != decoration.ServiceType)
                    {
                        continue;
                    }

                    if (!object.Equals(descriptor.ServiceKey, decoration.ServiceKey))
                    {
                        continue;
                    }

                    found = true;

                    // Generate a unique key for the original service
                    string decoratedKey = $"{decoration.ServiceType.Name}+{Guid.NewGuid():N}+Decorated";

                    // Move the original descriptor to a keyed registration
                    services.Add(descriptor.WithServiceKey(decoratedKey));

                    // Replace the original with a factory that resolves the keyed original and wraps it
                    Func<IServiceProvider, object?, object> decoratorFactory;

                    if (decoration.DecoratorFactory is { } factory)
                    {
                        decoratorFactory = (sp, _) =>
                        {
                            object inner = GetKeyedService(sp, decoration.ServiceType, decoratedKey);
                            return factory(sp, inner);
                        };
                    }
                    else
                    {
                        Type decoratorType = decoration.DecoratorType!;
                        decoratorFactory = (sp, _) =>
                        {
                            object inner = GetKeyedService(sp, decoration.ServiceType, decoratedKey);
                            return ActivatorUtilities.CreateInstance(sp, decoratorType, inner);
                        };
                    }

                    services[i] = new ServiceDescriptor(
                        descriptor.ServiceType,
                        descriptor.ServiceKey,
                        decoratorFactory,
                        descriptor.Lifetime);
                }

                if (!found)
                {
                    // Decorations added via TryDecorate may have no matching descriptors — that's fine.
                    // Decorations added via Decorate were already validated at registration time.
                }
            }
        }

        private static object GetKeyedService(IServiceProvider sp, Type serviceType, string key)
        {
            if (sp is IKeyedServiceProvider keyedProvider)
            {
                return keyedProvider.GetRequiredKeyedService(serviceType, key);
            }

            throw new InvalidOperationException(
                $"The service provider does not support keyed services, which are required for materialized decorations.");
        }

        private static ServiceDescriptor WithServiceKey(this ServiceDescriptor descriptor, object serviceKey)
        {
            if (descriptor.IsKeyedService)
            {
                if (descriptor.KeyedImplementationType is { } type)
                {
                    return new ServiceDescriptor(descriptor.ServiceType, serviceKey, type, descriptor.Lifetime);
                }

                if (descriptor.KeyedImplementationFactory is { } factory)
                {
                    return new ServiceDescriptor(descriptor.ServiceType, serviceKey, factory, descriptor.Lifetime);
                }

                if (descriptor.KeyedImplementationInstance is { } instance)
                {
                    return new ServiceDescriptor(descriptor.ServiceType, serviceKey, instance);
                }
            }
            else
            {
                if (descriptor.ImplementationType is { } type)
                {
                    return new ServiceDescriptor(descriptor.ServiceType, serviceKey, type, descriptor.Lifetime);
                }

                if (descriptor.ImplementationFactory is { } factory)
                {
                    // Wrap the non-keyed factory to discard the service key parameter
                    return new ServiceDescriptor(descriptor.ServiceType, serviceKey, (sp, _) => factory(sp), descriptor.Lifetime);
                }

                if (descriptor.ImplementationInstance is { } instance)
                {
                    return new ServiceDescriptor(descriptor.ServiceType, serviceKey, instance);
                }
            }

            throw new InvalidOperationException("ServiceDescriptor has no implementation.");
        }
    }
}
