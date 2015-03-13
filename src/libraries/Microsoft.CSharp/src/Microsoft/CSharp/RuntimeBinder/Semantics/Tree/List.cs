// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.CSharp.RuntimeBinder.Semantics
{
    internal class EXPRLIST : EXPR
    {
        public EXPR OptionalElement;
        public EXPR GetOptionalElement() { return OptionalElement; }
        public void SetOptionalElement(EXPR value) { OptionalElement = value; }
        public EXPR OptionalNextListNode;
        public EXPR GetOptionalNextListNode() { return OptionalNextListNode; }
        public void SetOptionalNextListNode(EXPR value) { OptionalNextListNode = value; }
    }
}
