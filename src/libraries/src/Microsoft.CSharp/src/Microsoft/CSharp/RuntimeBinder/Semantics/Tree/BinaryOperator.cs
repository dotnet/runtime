// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CSharp.RuntimeBinder.Semantics
{
    internal sealed class EXPRBINOP : EXPR
    {
        private EXPR _OptionalLeftChild;
        public EXPR GetOptionalLeftChild() { return _OptionalLeftChild; }
        public void SetOptionalLeftChild(EXPR value) { _OptionalLeftChild = value; }

        private EXPR _OptionalRightChild;
        public EXPR GetOptionalRightChild() { return _OptionalRightChild; }
        public void SetOptionalRightChild(EXPR value) { _OptionalRightChild = value; }

        private EXPR _OptionalUserDefinedCall;
        public EXPR GetOptionalUserDefinedCall() { return _OptionalUserDefinedCall; }
        public void SetOptionalUserDefinedCall(EXPR value) { _OptionalUserDefinedCall = value; }

        public MethWithInst predefinedMethodToCall;
        public bool isLifted;

        private MethPropWithInst _UserDefinedCallMethod;
        public MethPropWithInst GetUserDefinedCallMethod() { return _UserDefinedCallMethod; }
        public void SetUserDefinedCallMethod(MethPropWithInst value) { _UserDefinedCallMethod = value; }
    }
}
