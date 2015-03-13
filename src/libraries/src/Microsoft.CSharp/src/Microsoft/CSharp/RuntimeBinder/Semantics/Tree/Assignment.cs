// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.CSharp.RuntimeBinder.Semantics
{
    internal class EXPRASSIGNMENT : EXPR
    {
        private EXPR _LHS;
        public EXPR GetLHS() { return _LHS; }
        public void SetLHS(EXPR value) { _LHS = value; }

        private EXPR _RHS;
        public EXPR GetRHS() { return _RHS; }
        public void SetRHS(EXPR value) { _RHS = value; }
    }
}
