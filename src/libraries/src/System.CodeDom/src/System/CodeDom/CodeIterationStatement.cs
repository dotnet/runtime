// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.CodeDom
{
    public class CodeIterationStatement : CodeStatement
    {
        public CodeIterationStatement() { }

        public CodeIterationStatement(CodeStatement initStatement, CodeExpression testExpression, CodeStatement incrementStatement, params CodeStatement[] statements)
        {
            InitStatement = initStatement;
            TestExpression = testExpression;
            IncrementStatement = incrementStatement;
            Statements.AddRange(statements);
        }

        public CodeStatement InitStatement { get; set; }

        public CodeExpression TestExpression { get; set; }

        public CodeStatement IncrementStatement { get; set; }

        public CodeStatementCollection Statements { get; } = new CodeStatementCollection();
    }
}
