// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.CodeDom
{
    public class CodeAssignStatement : CodeStatement
    {
        public CodeAssignStatement() { }

        public CodeAssignStatement(CodeExpression left, CodeExpression right)
        {
            Left = left;
            Right = right;
        }

        public CodeExpression Left { get; set; }

        public CodeExpression Right { get; set; }
    }
}
