// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Internal.TypeSystem;

#nullable enable

namespace ILLink.Shared.TypeSystemProxy
{
    internal readonly partial struct GenericParameterProxy
    {
        public GenericParameterProxy(GenericParameterDesc genericParameter) => GenericParameter = genericParameter;

        public static implicit operator GenericParameterProxy(GenericParameterDesc genericParameter) => new(genericParameter);

        internal partial bool HasDefaultConstructorConstraint() => GenericParameter.HasDefaultConstructorConstraint;

        public readonly GenericParameterDesc GenericParameter;

        public override string ToString() => GenericParameter.ToString();
    }
}
