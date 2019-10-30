// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.CodeDom
{
    public class CodeExpressionStatement : CodeStatement
    {
        public CodeExpressionStatement() { }

        public CodeExpressionStatement(CodeExpression expression)
        {
            Expression = expression;
        }

        public CodeExpression Expression { get; set; }
    }
}
