// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
