// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Reflection;
using System.Reflection.Runtime.General;

namespace Internal.Reflection.Core
{
    public abstract class ReflectionDomainSetup
    {
        protected ReflectionDomainSetup() { }
        public abstract AssemblyBinder AssemblyBinder { get; }
        public abstract Exception CreateMissingMetadataException(TypeInfo pertainant);
        public abstract Exception CreateMissingMetadataException(Type pertainant);
        public abstract Exception CreateMissingMetadataException(TypeInfo pertainant, string nestedTypeName);
        public abstract Exception CreateNonInvokabilityException(MemberInfo pertainant);
        public abstract Exception CreateMissingArrayTypeException(Type elementType, bool isMultiDim, int rank);
        public abstract Exception CreateMissingConstructedGenericTypeException(Type genericTypeDefinition, Type[] genericTypeArguments);
    }
}
