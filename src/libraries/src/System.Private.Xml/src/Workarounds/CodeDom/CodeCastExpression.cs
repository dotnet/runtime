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
    ///       Represents a
    ///       type cast expression.
    ///    </para>
    /// </devdoc>
    [
        ComVisible(true)
    ]
    internal class CodeCastExpression : CodeExpression
    {
        private CodeTypeReference _targetType;
        private CodeExpression _expression;

        /// <devdoc>
        ///    <para>
        ///       Initializes a new instance of <see cref='System.CodeDom.CodeCastExpression'/>.
        ///    </para>
        /// </devdoc>
        public CodeCastExpression()
        {
        }

        /// <devdoc>
        ///    <para>
        ///       Initializes a new instance of <see cref='System.CodeDom.CodeCastExpression'/> using the specified
        ///       parameters.
        ///    </para>
        /// </devdoc>
        public CodeCastExpression(CodeTypeReference targetType, CodeExpression expression)
        {
            TargetType = targetType;
            Expression = expression;
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public CodeCastExpression(string targetType, CodeExpression expression)
        {
            TargetType = new CodeTypeReference(targetType);
            Expression = expression;
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public CodeCastExpression(Type targetType, CodeExpression expression)
        {
            TargetType = new CodeTypeReference(targetType);
            Expression = expression;
        }

        /// <devdoc>
        ///    <para>
        ///       The target type of the cast.
        ///    </para>
        /// </devdoc>
        public CodeTypeReference TargetType
        {
            get
            {
                if (_targetType == null)
                {
                    _targetType = new CodeTypeReference("");
                }
                return _targetType;
            }
            set
            {
                _targetType = value;
            }
        }

        /// <devdoc>
        ///    <para>
        ///       The expression to cast.
        ///    </para>
        /// </devdoc>
        public CodeExpression Expression
        {
            get
            {
                return _expression;
            }
            set
            {
                _expression = value;
            }
        }
    }
}
