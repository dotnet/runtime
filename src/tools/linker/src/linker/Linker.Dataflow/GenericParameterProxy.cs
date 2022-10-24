// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Mono.Cecil;

namespace ILLink.Shared.TypeSystemProxy
{
    internal readonly partial struct GenericParameterProxy
    {
        public GenericParameterProxy(GenericParameter genericParameter) => GenericParameter = genericParameter;

        public static implicit operator GenericParameterProxy(GenericParameter genericParameter) => new(genericParameter);

        internal partial bool HasDefaultConstructorConstraint() => GenericParameter.HasDefaultConstructorConstraint;

        public readonly GenericParameter GenericParameter;

        public override string ToString() => GenericParameter.ToString();
    }
}
