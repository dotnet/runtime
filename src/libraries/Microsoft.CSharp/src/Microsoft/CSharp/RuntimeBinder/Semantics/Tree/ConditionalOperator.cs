// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CSharp.RuntimeBinder.Semantics
{
    internal sealed class EXPRQUESTIONMARK : EXPR
    {
        private EXPR TestExpression;
        public EXPR GetTestExpression() { return TestExpression; }
        public void SetTestExpression(EXPR value) { TestExpression = value; }
        private EXPRBINOP Consequence;
        public EXPRBINOP GetConsequence() { return Consequence; }
        public void SetConsequence(EXPRBINOP value) { Consequence = value; }
    }
}
