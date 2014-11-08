// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Xml.XPath;

namespace MS.Internal.Xml.XPath
{
    internal abstract class AstNode
    {
        public enum AstType
        {
            Axis,
            Operator,
            Filter,
            ConstantOperand,
            Function,
            Group,
            Root,
            Variable,
            Error
        };

        public abstract AstType Type { get; }
        public abstract XPathResultType ReturnType { get; }
    }
}
