// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.CSharp.RuntimeBinder.Semantics
{
    // ----------------------------------------------------------------------------
    // ArrayType - a symbol representing an array.
    // ----------------------------------------------------------------------------

    internal class ArrayType : CType
    {
        // rank of the array. zero means unknown rank int [?].
        public int rank;

        public CType GetElementType() { return _pElementType; }
        public void SetElementType(CType pType) { _pElementType = pType; }

        // Returns the first non-array type in the parent chain.
        public CType GetBaseElementType()
        {
            CType type;
            for (type = GetElementType(); type.IsArrayType(); type = type.AsArrayType().GetElementType()) ;
            return type;
        }

        private CType _pElementType;
    }
}

