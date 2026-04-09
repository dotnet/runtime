// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.CodeDom
{
    public class CodeThrowExceptionStatement : CodeStatement
    {
        public CodeThrowExceptionStatement() { }

        public CodeThrowExceptionStatement(CodeExpression toThrow)
        {
            ToThrow = toThrow;
        }

        public CodeExpression ToThrow { get; set; }
    }
}
