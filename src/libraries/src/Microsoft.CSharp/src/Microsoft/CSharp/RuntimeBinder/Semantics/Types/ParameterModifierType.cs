// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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

    internal sealed class ParameterModifierType : CType
    {
        public bool isOut;            // True for out parameter, false for ref parameter.

        public CType GetParameterType() { return _pParameterType; }
        public void SetParameterType(CType pType) { _pParameterType = pType; }

        private CType _pParameterType;
    }
}
