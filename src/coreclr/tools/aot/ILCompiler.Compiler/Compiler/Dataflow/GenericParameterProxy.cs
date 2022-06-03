// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
