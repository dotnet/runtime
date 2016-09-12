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
    ///       Represents a binary operator expression.
    ///    </para>
    /// </devdoc>
    [
        ComVisible(true)
    ]
    internal class CodeBinaryOperatorExpression : CodeExpression
    {
        private CodeBinaryOperatorType _op;
        private CodeExpression _left;
        private CodeExpression _right;

        /// <devdoc>
        ///    <para>
        ///       Initializes a new instance of <see cref='System.CodeDom.CodeBinaryOperatorExpression'/>.
        ///    </para>
        /// </devdoc>
        public CodeBinaryOperatorExpression()
        {
        }

        /// <devdoc>
        ///    <para>
        ///       Initializes a new instance of <see cref='System.CodeDom.CodeBinaryOperatorExpression'/>
        ///       using the specified
        ///       parameters.
        ///    </para>
        /// </devdoc>
        public CodeBinaryOperatorExpression(CodeExpression left, CodeBinaryOperatorType op, CodeExpression right)
        {
            Right = right;
            Operator = op;
            Left = left;
        }

        /// <devdoc>
        ///    <para>
        ///       Gets or sets
        ///       the code expression on the right of the operator.
        ///    </para>
        /// </devdoc>
        public CodeExpression Right
        {
            get
            {
                return _right;
            }
            set
            {
                _right = value;
            }
        }

        /// <devdoc>
        ///    <para>
        ///       Gets or sets
        ///       the code expression on the left of the operator.
        ///    </para>
        /// </devdoc>
        public CodeExpression Left
        {
            get
            {
                return _left;
            }
            set
            {
                _left = value;
            }
        }

        /// <devdoc>
        ///    <para>
        ///       Gets or sets
        ///       the operator in the binary operator expression.
        ///    </para>
        /// </devdoc>
        public CodeBinaryOperatorType Operator
        {
            get
            {
                return _op;
            }
            set
            {
                _op = value;
            }
        }
    }
}
