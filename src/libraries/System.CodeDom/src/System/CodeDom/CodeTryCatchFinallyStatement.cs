// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.CodeDom
{
    public class CodeTryCatchFinallyStatement : CodeStatement
    {
        public CodeTryCatchFinallyStatement() { }

        public CodeTryCatchFinallyStatement(CodeStatement[] tryStatements, CodeCatchClause[] catchClauses)
        {
            TryStatements.AddRange(tryStatements);
            CatchClauses.AddRange(catchClauses);
        }

        public CodeTryCatchFinallyStatement(CodeStatement[] tryStatements, CodeCatchClause[] catchClauses, CodeStatement[] finallyStatements)
        {
            TryStatements.AddRange(tryStatements);
            CatchClauses.AddRange(catchClauses);
            FinallyStatements.AddRange(finallyStatements);
        }

        public CodeStatementCollection TryStatements { get; } = new CodeStatementCollection();

        public CodeCatchClauseCollection CatchClauses { get; } = new CodeCatchClauseCollection();

        public CodeStatementCollection FinallyStatements { get; } = new CodeStatementCollection();
    }
}
