// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;

namespace Microsoft.CSharp.RuntimeBinder.Semantics
{
    internal sealed class TypeParameterSymbol : Symbol
    {
        private bool _bIsMethodTypeParameter;
        private SpecCons _constraints;

        private TypeParameterType _pTypeParameterType;

        private int _nIndexInOwnParameters;
        private int _nIndexInTotalParameters;

        private TypeArray _pBounds;

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

        public void SetBounds(TypeArray pBounds)
        {
            _pBounds = pBounds;
        }

        public TypeArray GetBounds()
        {
            return _pBounds;
        }

        public void SetConstraints(SpecCons constraints)
        {
            _constraints = constraints;
        }

        public bool IsValueType()
        {
            return (_constraints & SpecCons.Val) > 0;
        }

        public bool IsReferenceType()
        {
            return (_constraints & SpecCons.Ref) > 0;
        }

        public bool IsNonNullableValueType()
        {
            return (_constraints & SpecCons.Val) > 0;
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
