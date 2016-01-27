// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
