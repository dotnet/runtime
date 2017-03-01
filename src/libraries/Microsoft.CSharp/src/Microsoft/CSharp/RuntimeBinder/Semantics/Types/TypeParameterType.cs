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


        public bool DependsOn(TypeParameterType pType)
        {
            Debug.Assert(pType != null);

            // * If a type parameter T is used as a constraint for type parameter S
            //   then S depends on T.
            // * If a type parameter S depends on a type parameter T and T depends on
            //   U then S depends on U.

            TypeArray pConstraints = GetBounds();
            for (int iConstraint = 0; iConstraint < pConstraints.size; ++iConstraint)
            {
                CType pConstraint = pConstraints.Item(iConstraint);
                if (pConstraint == pType)
                {
                    return true;
                }
                if (pConstraint.IsTypeParameterType() &&
                    pConstraint.AsTypeParameterType().DependsOn(pType))
                {
                    return true;
                }
            }
            return false;
        }

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
        public TypeArray GetInterfaceBounds() { return _pTypeParameterSymbol.GetInterfaceBounds(); }
        public AggregateType GetEffectiveBaseClass() { return _pTypeParameterSymbol.GetEffectiveBaseClass(); }

        private TypeParameterSymbol _pTypeParameterSymbol;
    }
}
