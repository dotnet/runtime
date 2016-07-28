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
    ///       Represents an object create expression.
    ///    </para>
    /// </devdoc>
    [
        ComVisible(true)
    ]
    internal class CodeObjectCreateExpression : CodeExpression
    {
        private CodeTypeReference _createType;
        private CodeExpressionCollection _parameters = new CodeExpressionCollection();

        /// <devdoc>
        ///    <para>
        ///       Initializes a new <see cref='System.CodeDom.CodeObjectCreateExpression'/>.
        ///    </para>
        /// </devdoc>
        public CodeObjectCreateExpression()
        {
        }

        /// <devdoc>
        ///    <para>
        ///       Initializes a new <see cref='System.CodeDom.CodeObjectCreateExpression'/> using the specified type and
        ///       parameters.
        ///    </para>
        /// </devdoc>
        public CodeObjectCreateExpression(CodeTypeReference createType, params CodeExpression[] parameters)
        {
            CreateType = createType;
            Parameters.AddRange(parameters);
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public CodeObjectCreateExpression(string createType, params CodeExpression[] parameters)
        {
            CreateType = new CodeTypeReference(createType);
            Parameters.AddRange(parameters);
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public CodeObjectCreateExpression(Type createType, params CodeExpression[] parameters)
        {
            CreateType = new CodeTypeReference(createType);
            Parameters.AddRange(parameters);
        }

        /// <devdoc>
        ///    <para>
        ///       The type of the object to create.
        ///    </para>
        /// </devdoc>
        public CodeTypeReference CreateType
        {
            get
            {
                if (_createType == null)
                {
                    _createType = new CodeTypeReference("");
                }
                return _createType;
            }
            set
            {
                _createType = value;
            }
        }

        /// <devdoc>
        ///    <para>
        ///       Gets or sets the parameters to use in creating the
        ///       object.
        ///    </para>
        /// </devdoc>
        public CodeExpressionCollection Parameters
        {
            get
            {
                return _parameters;
            }
        }
    }
}
