// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace System.Reflection.Metadata.Experiment
{
    /* The purpose of  this class is to provide wrappers for entities that are referenced in metadata.
        *  The wrappers allows for convenient access to the parent token of an entity.
        *  They override default equality for Assemblies, Types, Methods etc. to make sure identical writes to metadata aren't made for different objects.
        * */
    internal sealed class EntityWrappers
    {
        internal sealed class AssemblyReferenceWrapper
        {
            internal readonly Assembly assembly;

            public AssemblyReferenceWrapper(Assembly assembly)
            {
                this.assembly = assembly;
            }

            public override bool Equals(object? obj)
            {
                return obj is AssemblyReferenceWrapper wrapper &&
                       EqualityComparer<string>.Default.Equals(assembly.GetName().FullName, wrapper.assembly.GetName().FullName);
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(assembly.GetName().FullName);
            }
        }

        internal sealed class TypeReferenceWrapper
        {
            internal readonly Type type;
            internal int parentToken;

            public TypeReferenceWrapper(Type type)
            {
                this.type = type;
            }

            public override bool Equals(object? obj)
            {
                return obj is TypeReferenceWrapper wrapper
                    && EqualityComparer<string>.Default.Equals(type.Name, wrapper.type.Name)
                    && EqualityComparer<string>.Default.Equals(type.Namespace, wrapper.type.Namespace)
                    && parentToken == wrapper.parentToken;
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(type.Name, type.Namespace, parentToken);
            }
        }

        internal sealed class MethodReferenceWrapper
        {
            internal readonly MethodBase method;
            internal int parentToken;

            public MethodReferenceWrapper(MethodBase method)
            {
                this.method = method;
            }

            public override bool Equals(object? obj)
            {
                return obj is MethodReferenceWrapper wrapper
                    && EqualityComparer<string>.Default.Equals(method.Name, wrapper.method.Name)
                    && EqualityComparer<string>.Default.Equals(method.ToString(), wrapper.method.ToString())
                    && parentToken == wrapper.parentToken;
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(method.Name, method.ToString(), parentToken);
            }
        }

        internal sealed class CustomAttributeWrapper
        {
            internal ConstructorInfo constructorInfo;
            internal byte[] binaryAttribute;
            internal int conToken;

            public CustomAttributeWrapper(ConstructorInfo constructorInfo, byte[] binaryAttribute)
            {
                this.constructorInfo = constructorInfo;
                this.binaryAttribute = binaryAttribute;
            }
        }
    }
}
