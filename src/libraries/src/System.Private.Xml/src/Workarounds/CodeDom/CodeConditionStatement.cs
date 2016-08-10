// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.CodeDom
{
    using System.Diagnostics;
    using System;
    using Microsoft.Win32;
    using System.Collections;
    using System.Runtime.InteropServices;

    /// <devdoc>
    ///    <para>
    ///       Represents a basic if statement.
    ///    </para>
    /// </devdoc>
    [
        ComVisible(true)
    ]
    internal class CodeConditionStatement : CodeStatement
    {
        private CodeExpression _condition;
        private CodeStatementCollection _trueStatments = new CodeStatementCollection();
        private CodeStatementCollection _falseStatments = new CodeStatementCollection();

        /// <devdoc>
        ///    <para>
        ///       Initializes a new instance of <see cref='System.CodeDom.CodeConditionStatement'/>.
        ///    </para>
        /// </devdoc>
        public CodeConditionStatement()
        {
        }

        /// <devdoc>
        ///    <para>
        ///       Initializes a new instance of <see cref='System.CodeDom.CodeConditionStatement'/>.
        ///    </para>
        /// </devdoc>
        public CodeConditionStatement(CodeExpression condition, params CodeStatement[] trueStatements)
        {
            Condition = condition;
            TrueStatements.AddRange(trueStatements);
        }

        /// <devdoc>
        ///    <para>
        ///       Initializes a new instance of <see cref='System.CodeDom.CodeConditionStatement'/> that can represent an if..
        ///       else statement.
        ///    </para>
        /// </devdoc>
        public CodeConditionStatement(CodeExpression condition, CodeStatement[] trueStatements, CodeStatement[] falseStatements)
        {
            Condition = condition;
            TrueStatements.AddRange(trueStatements);
            FalseStatements.AddRange(falseStatements);
        }

        /// <devdoc>
        ///    <para>
        ///       Gets or sets
        ///       the condition to test for <see langword='true'/>.
        ///    </para>
        /// </devdoc>
        public CodeExpression Condition
        {
            get
            {
                return _condition;
            }
            set
            {
                _condition = value;
            }
        }

        /// <devdoc>
        ///    <para>
        ///       Gets or sets
        ///       the statements to execute if test condition is <see langword='true'/>.
        ///    </para>
        /// </devdoc>
        public CodeStatementCollection TrueStatements
        {
            get
            {
                return _trueStatments;
            }
        }

        /// <devdoc>
        ///    <para>
        ///       Gets or sets
        ///       the statements to
        ///       execute if test condition is <see langword='false'/> and there is an else
        ///       clause.
        ///    </para>
        /// </devdoc>
        public CodeStatementCollection FalseStatements
        {
            get
            {
                return _falseStatments;
            }
        }
    }
}
