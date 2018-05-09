// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;

namespace System.Reflection
{
    internal sealed class SignatureArrayType : SignatureHasElementType
    {
        internal SignatureArrayType(SignatureType elementType, int rank, bool isMultiDim)
            : base(elementType)
        {
            Debug.Assert(rank > 0);
            Debug.Assert(rank == 1 || isMultiDim);
    
            _rank = rank;
            _isMultiDim = isMultiDim;
        }
    
        protected sealed override bool IsArrayImpl() => true;
        protected sealed override bool IsByRefImpl() => false;
        protected sealed override bool IsPointerImpl() => false;
    
        public sealed override bool IsSZArray => !_isMultiDim;
        public sealed override bool IsVariableBoundArray => _isMultiDim;
    
        public sealed override int GetArrayRank() => _rank;
    
        protected sealed override string Suffix
        {
            get
            {
                if (!_isMultiDim)
                    return "[]";
                else if (_rank == 1)
                    return "[*]";
                else
                    return "[" + new string(',', _rank - 1) + "]";
            }
        }
    
        private readonly int _rank;
        private readonly bool _isMultiDim;
    }
}
