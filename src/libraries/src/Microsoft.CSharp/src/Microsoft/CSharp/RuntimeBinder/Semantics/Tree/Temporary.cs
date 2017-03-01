// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CSharp.RuntimeBinder.Semantics
{
    internal sealed class EXPRWRAP : EXPR
    {
        private EXPR OptionalExpression;
        public EXPR GetOptionalExpression() { return OptionalExpression; }
        public void SetOptionalExpression(EXPR value) { OptionalExpression = value; }
    }
}
