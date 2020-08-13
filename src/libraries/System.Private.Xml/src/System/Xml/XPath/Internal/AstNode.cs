// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable
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
