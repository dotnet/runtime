// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.CSharp.RuntimeBinder.Semantics
{
    internal class EXPRBLOCK : EXPRSTMT
    {
        private EXPRSTMT _OptionalStatements;
        public EXPRSTMT GetOptionalStatements() { return _OptionalStatements; }
        public void SetOptionalStatements(EXPRSTMT value) { _OptionalStatements = value; }

        public Scope OptionalScopeSymbol;
    }
}
