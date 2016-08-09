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
    ///       Represents a local variable declaration.
    ///    </para>
    /// </devdoc>
    [
        ComVisible(true)
    ]
    internal class CodeVariableDeclarationStatement : CodeStatement
    {
        private CodeTypeReference _type;
        private string _name;
        private CodeExpression _initExpression;

        /// <devdoc>
        ///    <para>
        ///       Initializes a new instance of <see cref='System.CodeDom.CodeVariableDeclarationStatement'/>.
        ///    </para>
        /// </devdoc>
        public CodeVariableDeclarationStatement()
        {
        }

        /// <devdoc>
        ///    <para>
        ///       Initializes a new instance of <see cref='System.CodeDom.CodeVariableDeclarationStatement'/> using the specified type and name.
        ///    </para>
        /// </devdoc>
        public CodeVariableDeclarationStatement(CodeTypeReference type, string name)
        {
            Type = type;
            Name = name;
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public CodeVariableDeclarationStatement(string type, string name)
        {
            Type = new CodeTypeReference(type);
            Name = name;
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public CodeVariableDeclarationStatement(Type type, string name)
        {
            Type = new CodeTypeReference(type);
            Name = name;
        }

        /// <devdoc>
        ///    <para>
        ///       Initializes a new instance of <see cref='System.CodeDom.CodeVariableDeclarationStatement'/> using the specified type, name and
        ///       initialization expression.
        ///    </para>
        /// </devdoc>
        public CodeVariableDeclarationStatement(CodeTypeReference type, string name, CodeExpression initExpression)
        {
            Type = type;
            Name = name;
            InitExpression = initExpression;
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public CodeVariableDeclarationStatement(string type, string name, CodeExpression initExpression)
        {
            Type = new CodeTypeReference(type);
            Name = name;
            InitExpression = initExpression;
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public CodeVariableDeclarationStatement(Type type, string name, CodeExpression initExpression)
        {
            Type = new CodeTypeReference(type);
            Name = name;
            InitExpression = initExpression;
        }

        /// <devdoc>
        ///    <para>
        ///       Gets or sets the initialization expression for the variable.
        ///    </para>
        /// </devdoc>
        public CodeExpression InitExpression
        {
            get
            {
                return _initExpression;
            }
            set
            {
                _initExpression = value;
            }
        }

        /// <devdoc>
        ///    <para>
        ///       Gets or sets the name of the variable.
        ///    </para>
        /// </devdoc>
        public string Name
        {
            get
            {
                return (_name == null) ? string.Empty : _name;
            }
            set
            {
                _name = value;
            }
        }

        /// <devdoc>
        ///    <para>
        ///       Gets or sets the type of the variable.
        ///    </para>
        /// </devdoc>
        public CodeTypeReference Type
        {
            get
            {
                if (_type == null)
                {
                    _type = new CodeTypeReference("");
                }
                return _type;
            }
            set
            {
                _type = value;
            }
        }
    }
}
