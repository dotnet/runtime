// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.CodeDom
{
    public class CodeBinaryOperatorExpression : CodeExpression
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
