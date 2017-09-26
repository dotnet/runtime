// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CSharp.RuntimeBinder.Semantics
{
    // ----------------------------------------------------------------------------
    // ArrayType - a symbol representing an array.
    // ----------------------------------------------------------------------------

    internal sealed class ArrayType : CType
    {
        // rank of the array. zero means unknown rank int [?].
        public int rank;

        public bool IsSZArray { get; set; }

        public CType GetElementType() { return _pElementType; }
        public void SetElementType(CType pType) { _pElementType = pType; }

        // Returns the first non-array type in the parent chain.
        public CType GetBaseElementType()
        {
            CType type = GetElementType();
            while (type is ArrayType arr)
            {
                type = arr.GetElementType();
            }

            return type;
        }

        private CType _pElementType;
    }
}

