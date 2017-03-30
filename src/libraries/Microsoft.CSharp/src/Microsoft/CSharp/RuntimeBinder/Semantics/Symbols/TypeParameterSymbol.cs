// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;

namespace Microsoft.CSharp.RuntimeBinder.Semantics
{
    internal sealed class TypeParameterSymbol : Symbol
    {
        private bool _bIsMethodTypeParameter;
        private bool _bHasRefBound;
        private bool _bHasValBound;
        private SpecCons _constraints;

        private TypeParameterType _pTypeParameterType;

        private int _nIndexInOwnParameters;
        private int _nIndexInTotalParameters;

        private TypeArray _pBounds;
        private TypeArray _pInterfaceBounds;

        private AggregateType _pEffectiveBaseClass;
        private CType _pDeducedBaseClass; // This may be a NullableType or an ArrayType etc, for error reporting.

        public bool Covariant;
        public bool Invariant { get { return !Covariant && !Contravariant; } }
        public bool Contravariant;

        public void SetTypeParameterType(TypeParameterType pType)
        {
            _pTypeParameterType = pType;
        }

        public TypeParameterType GetTypeParameterType()
        {
            return _pTypeParameterType;
        }

        public bool IsMethodTypeParameter()
        {
            return _bIsMethodTypeParameter;
        }

        public void SetIsMethodTypeParameter(bool b)
        {
            _bIsMethodTypeParameter = b;
        }

        public int GetIndexInOwnParameters()
        {
            return _nIndexInOwnParameters;
        }

        public void SetIndexInOwnParameters(int index)
        {
            _nIndexInOwnParameters = index;
        }

        public int GetIndexInTotalParameters()
        {
            return _nIndexInTotalParameters;
        }

        public void SetIndexInTotalParameters(int index)
        {
            Debug.Assert(index >= _nIndexInOwnParameters);
            _nIndexInTotalParameters = index;
        }

        public TypeArray GetInterfaceBounds()
        {
            return _pInterfaceBounds;
        }

        public void SetBounds(TypeArray pBounds)
        {
            _pBounds = pBounds;
            _pInterfaceBounds = null;
            _pEffectiveBaseClass = null;
            _pDeducedBaseClass = null;
            _bHasRefBound = false;
            _bHasValBound = false;
        }

        public TypeArray GetBounds()
        {
            return _pBounds;
        }

        public void SetConstraints(SpecCons constraints)
        {
            _constraints = constraints;
        }

        public AggregateType GetEffectiveBaseClass()
        {
            return _pEffectiveBaseClass;
        }

        public bool IsValueType()
        {
            return (_constraints & SpecCons.Val) > 0 || _bHasValBound;
        }

        public bool IsReferenceType()
        {
            return (_constraints & SpecCons.Ref) > 0 || _bHasRefBound;
        }

        public bool IsNonNullableValueType()
        {
            return (_constraints & SpecCons.Val) > 0 || _bHasValBound && !_pDeducedBaseClass.IsNullableType();
        }

        public bool HasNewConstraint()
        {
            return (_constraints & SpecCons.New) > 0;
        }

        public bool HasRefConstraint()
        {
            return (_constraints & SpecCons.Ref) > 0;
        }

        public bool HasValConstraint()
        {
            return (_constraints & SpecCons.Val) > 0;
        }
    }
}
