// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.CodeDom
{
    public class CodeMethodReturnStatement : CodeStatement
    {
        public CodeMethodReturnStatement() { }

        public CodeMethodReturnStatement(CodeExpression expression)
        {
            Expression = expression;
        }

        public CodeExpression Expression { get; set; }
    }
}
