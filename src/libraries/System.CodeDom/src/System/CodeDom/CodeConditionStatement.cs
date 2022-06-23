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
    internal sealed class CodeConditionStatement : CodeStatement
#else
    public class CodeConditionStatement : CodeStatement
#endif
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
