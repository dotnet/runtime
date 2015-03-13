// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.CSharp.RuntimeBinder.Semantics
{
    internal class EXPRWRAP : EXPR
    {
        public EXPR OptionalExpression;
        public EXPR GetOptionalExpression() { return OptionalExpression; }
        public void SetOptionalExpression(EXPR value) { OptionalExpression = value; }
    }
}
