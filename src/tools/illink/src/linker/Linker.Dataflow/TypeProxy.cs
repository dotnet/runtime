// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Immutable;
using Mono.Cecil;
using Mono.Linker;

namespace ILLink.Shared.TypeSystemProxy
{
    internal readonly partial struct TypeProxy : IEquatable<TypeProxy>
    {
        public TypeProxy(TypeReference type, ITryResolveMetadata resolver)
        {
            Type = type;
            this.resolver = resolver;
        }

        internal partial ImmutableArray<GenericParameterProxy> GetGenericParameters()
        {
            if (!Type.HasGenericParameters)
                return ImmutableArray<GenericParameterProxy>.Empty;

            var builder = ImmutableArray.CreateBuilder<GenericParameterProxy>(Type.GenericParameters.Count);
            foreach (var genericParameter in Type.GenericParameters)
            {
                builder.Add(new GenericParameterProxy(genericParameter));
            }

            return builder.ToImmutableArray();
        }

        private readonly ITryResolveMetadata resolver;

        public TypeReference Type { get; }

        public string Name { get => Type.Name; }

        public string? Namespace { get => Type.Namespace; }

        public bool IsTypeOf(string @namespace, string name) => Type.IsTypeOf(@namespace, name);

        public bool IsTypeOf(WellKnownType wellKnownType) => Type.IsTypeOf(wellKnownType);

        public string GetDisplayName() => Type.GetDisplayName();

        public override string ToString() => Type.ToString();

        public bool Equals(TypeProxy other) => TypeReferenceEqualityComparer.AreEqual(Type, other.Type, resolver);

        public override bool Equals(object? o) => o is TypeProxy other && Equals(other);

        public override int GetHashCode() => TypeReferenceEqualityComparer.GetHashCodeFor(Type);
    }
}
