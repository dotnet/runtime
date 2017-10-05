// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;

namespace Microsoft.CSharp.RuntimeBinder.Semantics
{
    /////////////////////////////////////////////////////////////////////////////////

    internal sealed class TypeParameterType : CType
    {
        public TypeParameterSymbol GetTypeParameterSymbol() { return _pTypeParameterSymbol; }
        public void SetTypeParameterSymbol(TypeParameterSymbol pTypePArameterSymbol) { _pTypeParameterSymbol = pTypePArameterSymbol; }

        public ParentSymbol GetOwningSymbol() { return _pTypeParameterSymbol.parent; }

        // Forward calls into the symbol.
        public bool Covariant { get { return _pTypeParameterSymbol.Covariant; } }
        public bool Invariant { get { return _pTypeParameterSymbol.Invariant; } }
        public bool Contravariant { get { return _pTypeParameterSymbol.Contravariant; } }
        public bool IsValueType() { return _pTypeParameterSymbol.IsValueType(); }
        public bool IsReferenceType() { return _pTypeParameterSymbol.IsReferenceType(); }
        public bool IsNonNullableValueType() { return _pTypeParameterSymbol.IsNonNullableValueType(); }
        public bool HasNewConstraint() { return _pTypeParameterSymbol.HasNewConstraint(); }
        public bool HasRefConstraint() { return _pTypeParameterSymbol.HasRefConstraint(); }
        public bool HasValConstraint() { return _pTypeParameterSymbol.HasValConstraint(); }
        public bool IsMethodTypeParameter() { return _pTypeParameterSymbol.IsMethodTypeParameter(); }
        public int GetIndexInOwnParameters() { return _pTypeParameterSymbol.GetIndexInOwnParameters(); }
        public int GetIndexInTotalParameters() { return _pTypeParameterSymbol.GetIndexInTotalParameters(); }
        public TypeArray GetBounds() { return _pTypeParameterSymbol.GetBounds(); }

        private TypeParameterSymbol _pTypeParameterSymbol;
    }
}
