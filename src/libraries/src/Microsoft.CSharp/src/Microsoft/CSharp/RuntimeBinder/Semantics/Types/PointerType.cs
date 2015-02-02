// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.CSharp.RuntimeBinder.Semantics
{
    internal class PointerType : CType
    {
        public CType GetReferentType() { return _pReferentType; }
        public void SetReferentType(CType pType) { _pReferentType = pType; }
        private CType _pReferentType;
    }
}
