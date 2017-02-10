// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CSharp.RuntimeBinder.Semantics
{
    internal sealed class EXPRMULTIGET : EXPR
    {
        private EXPRMULTI OptionalMulti;
        public EXPRMULTI GetOptionalMulti() { return OptionalMulti; }
        public void SetOptionalMulti(EXPRMULTI value) { OptionalMulti = value; }
    }

    internal sealed class EXPRMULTI : EXPR
    {
        public EXPR Left;
        public EXPR GetLeft() { return Left; }
        public void SetLeft(EXPR value) { Left = value; }
        public EXPR Operator;
        public EXPR GetOperator() { return Operator; }
        public void SetOperator(EXPR value) { Operator = value; }
    }
}
