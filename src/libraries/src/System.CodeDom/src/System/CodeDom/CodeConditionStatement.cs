// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.CodeDom
{
    public class CodeConditionStatement : CodeStatement
    {
        public CodeConditionStatement() { }

        public CodeConditionStatement(CodeExpression condition, params CodeStatement[] trueStatements)
        {
            Condition = condition;
            TrueStatements.AddRange(trueStatements);
        }

        public CodeConditionStatement(CodeExpression condition, CodeStatement[] trueStatements, CodeStatement[] falseStatements)
        {
            Condition = condition;
            TrueStatements.AddRange(trueStatements);
            FalseStatements.AddRange(falseStatements);
        }

        public CodeExpression Condition { get; set; }

        public CodeStatementCollection TrueStatements { get; } = new CodeStatementCollection();

        public CodeStatementCollection FalseStatements { get; } = new CodeStatementCollection();
    }
}
