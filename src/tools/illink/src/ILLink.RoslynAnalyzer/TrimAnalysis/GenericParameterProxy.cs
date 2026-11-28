// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Microsoft.CodeAnalysis;

namespace ILLink.Shared.TypeSystemProxy
{
    internal readonly partial struct GenericParameterProxy : IEquatable<GenericParameterProxy>
    {
        public GenericParameterProxy(ITypeParameterSymbol typeParameterSymbol) => TypeParameterSymbol = typeParameterSymbol;

        internal partial bool HasDefaultConstructorConstraint() =>
            TypeParameterSymbol.HasConstructorConstraint |
            TypeParameterSymbol.HasValueTypeConstraint |
            TypeParameterSymbol.HasUnmanagedTypeConstraint;

        internal partial bool HasEnumConstraint()
        {
            foreach (ITypeSymbol constraintType in TypeParameterSymbol.ConstraintTypes)
            {
                if (constraintType.SpecialType == SpecialType.System_Enum)
                    return true;
            }

            return false;
        }

        public readonly ITypeParameterSymbol TypeParameterSymbol;

        public bool Equals(GenericParameterProxy other) => SymbolEqualityComparer.Default.Equals(TypeParameterSymbol, other.TypeParameterSymbol);

        public override bool Equals(object? obj) => obj is GenericParameterProxy other && Equals(other);

        public override int GetHashCode() => SymbolEqualityComparer.Default.GetHashCode(TypeParameterSymbol);

        public override string ToString() => TypeParameterSymbol.ToString();
    }
}
