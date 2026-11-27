// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace System;

/// <summary>Provides downlevel polyfills for instance members on <see cref="Type"/>.</summary>
internal static partial class TypePolyfills
{
    extension(Type type)
    {
        public bool IsByRefLike
        {
            get
            {
                if (!type.IsValueType)
                {
                    return false;
                }

                object[] attributes = type.GetCustomAttributes(inherit: false);

                for (int i = 0; i < attributes.Length; i++)
                {
                    if (attributes[i].GetType().FullName == "System.Runtime.CompilerServices.IsByRefLikeAttribute")
                    {
                        return true;
                    }
                }

                return false;
            }
        }

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

            // Any instantiation of a generic interface matches the same interfaces as its generic type definition.
            if (interfaceType.IsConstructedGenericType)
            {
                interfaceType = interfaceType.GetGenericTypeDefinition();
            }

            Type[] interfaces = type.GetInterfaces();

            int count = 0;
            foreach (Type implementedInterface in interfaces)
            {
                if ((implementedInterface.IsGenericType ? implementedInterface.GetGenericTypeDefinition() : implementedInterface) == interfaceType)
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
