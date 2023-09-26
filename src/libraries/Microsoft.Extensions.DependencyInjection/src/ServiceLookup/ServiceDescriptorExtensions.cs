// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Extensions.DependencyInjection.ServiceLookup
{
    internal static class ServiceDescriptorExtensions
    {
        public static bool HasImplementationInstance(this ServiceDescriptor serviceDescriptor) => GetImplementationInstance(serviceDescriptor) != null;

        public static bool HasImplementationFactory(this ServiceDescriptor serviceDescriptor) => GetImplementationFactory(serviceDescriptor) != null;

        public static bool HasImplementationType(this ServiceDescriptor serviceDescriptor) => GetImplementationType(serviceDescriptor) != null;

        public static object? GetImplementationInstance(this ServiceDescriptor serviceDescriptor)
        {
            return serviceDescriptor.IsKeyedService
                ? serviceDescriptor.KeyedImplementationInstance
                : serviceDescriptor.ImplementationInstance;
        }

        public static object? GetImplementationFactory(this ServiceDescriptor serviceDescriptor)
        {
            return serviceDescriptor.IsKeyedService
                ? serviceDescriptor.KeyedImplementationFactory
                : serviceDescriptor.ImplementationFactory;
        }

        [return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
        public static Type? GetImplementationType(this ServiceDescriptor serviceDescriptor)
        {
            return serviceDescriptor.IsKeyedService
                ? serviceDescriptor.KeyedImplementationType
                : serviceDescriptor.ImplementationType;
        }

        public static bool TryGetImplementationType(this ServiceDescriptor serviceDescriptor, out Type? type)
        {
            type = GetImplementationType(serviceDescriptor);
            return type != null;
        }
    }
}
