// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.CSharp.RuntimeBinder.Semantics
{
    internal abstract class EXPRSTMT : EXPR
    {
        private EXPRSTMT _NextStatement;
        public EXPRSTMT GetOptionalNextStatement()
        {
            return _NextStatement;
        }
        public void SetOptionalNextStatement(EXPRSTMT nextStatement)
        {
            _NextStatement = nextStatement;
        }
    }
}
