// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if smolloy_codedom_full_internalish
namespace System.Runtime.Serialization.CodeDom
#nullable disable
#else
namespace System.CodeDom
#endif
{
#if smolloy_codedom_full_internalish
    internal sealed class CodeBinaryOperatorExpression : CodeExpression
#else
    public class CodeBinaryOperatorExpression : CodeExpression
#endif
    {
        public CodeBinaryOperatorExpression() { }

        public CodeBinaryOperatorExpression(CodeExpression left, CodeBinaryOperatorType op, CodeExpression right)
        {
            Right = right;
            Operator = op;
            Left = left;
        }

        public CodeExpression Right { get; set; }

        public CodeExpression Left { get; set; }

        public CodeBinaryOperatorType Operator { get; set; }
    }
}
