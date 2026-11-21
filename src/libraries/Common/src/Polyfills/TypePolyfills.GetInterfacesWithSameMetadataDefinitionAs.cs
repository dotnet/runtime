// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace System;

/// <summary>Provides downlevel polyfills for <see cref="Type"/>.GetInterfacesWithSameMetadataDefinitionAs.</summary>
internal static partial class TypePolyfills
{
    extension(Type type)
    {
        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2070:UnrecognizedReflectionPattern",
            Justification = "GetInterfaces technically requires all interfaces to be preserved, but this method only returns the interfaces " +
                            "with the same metadata definition as the specified interfaceType. If interfaceType exists, trimming kept " +
                            "the matching interface implementations on any type.")]
        public Type[] GetInterfacesWithSameMetadataDefinitionAs(Type interfaceType)
        {
            if (interfaceType is null)
            {
                throw new ArgumentNullException(nameof(interfaceType));
            }

            if (!interfaceType.IsInterface || interfaceType.IsConstructedGenericType)
            {
                throw new ArgumentException(null, nameof(interfaceType));
            }

            Type[] interfaces = type.GetInterfaces();

            int count = 0;
            foreach (Type implementedInterface in interfaces)
            {
                if (implementedInterface == interfaceType ||
                    (implementedInterface.IsGenericType && implementedInterface.GetGenericTypeDefinition() == interfaceType))
                {
                    interfaces[count++] = implementedInterface;
                }
            }

            if (count == 0)
            {
                return Type.EmptyTypes;
            }

            Array.Resize(ref interfaces, count);
            return interfaces;
        }
    }
}
