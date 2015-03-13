// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.CSharp.RuntimeBinder.Semantics
{
    internal class EXPRQUESTIONMARK : EXPR
    {
        public EXPR TestExpression;
        public EXPR GetTestExpression() { return TestExpression; }
        public void SetTestExpression(EXPR value) { TestExpression = value; }
        public EXPRBINOP Consequence;
        public EXPRBINOP GetConsequence() { return Consequence; }
        public void SetConsequence(EXPRBINOP value) { Consequence = value; }
    }
}
