// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.CSharp.RuntimeBinder.Semantics
{
    // ----------------------------------------------------------------------------
    //
    // ParameterModifierType
    //
    // ParameterModifierType - a symbol representing parameter modifier -- either
    // out or ref.
    //
    // ----------------------------------------------------------------------------

    internal class ParameterModifierType : CType
    {
        public bool isOut;            // True for out parameter, false for ref parameter.

        public CType GetParameterType() { return _pParameterType; }
        public void SetParameterType(CType pType) { _pParameterType = pType; }

        private CType _pParameterType;
    }
}
