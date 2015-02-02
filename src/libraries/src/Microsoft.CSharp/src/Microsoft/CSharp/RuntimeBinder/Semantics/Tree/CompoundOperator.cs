// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.CSharp.RuntimeBinder.Semantics
{
    internal class EXPRMULTIGET : EXPR
    {
        public EXPRMULTI OptionalMulti;
        public EXPRMULTI GetOptionalMulti() { return OptionalMulti; }
        public void SetOptionalMulti(EXPRMULTI value) { OptionalMulti = value; }
    }

    internal class EXPRMULTI : EXPR
    {
        public EXPR Left;
        public EXPR GetLeft() { return Left; }
        public void SetLeft(EXPR value) { Left = value; }
        public EXPR Operator;
        public EXPR GetOperator() { return Operator; }
        public void SetOperator(EXPR value) { Operator = value; }
    }
}
