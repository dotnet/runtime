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
    ///       Represents a parameter declaration for method, constructor, or property arguments.
    ///    </para>
    /// </devdoc>
    [
        ComVisible(true)
    ]
    internal class CodeParameterDeclarationExpression : CodeExpression
    {
        private CodeTypeReference _type;
        private string _name;
        private CodeAttributeDeclarationCollection _customAttributes = null;


        /// <devdoc>
        ///    <para>
        ///       Initializes a new instance of <see cref='System.CodeDom.CodeParameterDeclarationExpression'/>.
        ///    </para>
        /// </devdoc>
        public CodeParameterDeclarationExpression()
        {
        }

        /// <devdoc>
        ///    <para>
        ///       Initializes a new instance of <see cref='System.CodeDom.CodeParameterDeclarationExpression'/> using the specified type and name.
        ///    </para>
        /// </devdoc>
        public CodeParameterDeclarationExpression(CodeTypeReference type, string name)
        {
            Type = type;
            Name = name;
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public CodeParameterDeclarationExpression(string type, string name)
        {
            Type = new CodeTypeReference(type);
            Name = name;
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public CodeParameterDeclarationExpression(Type type, string name)
        {
            Type = new CodeTypeReference(type);
            Name = name;
        }

        /// <devdoc>
        ///    <para>
        ///       Gets or sets the custom attributes for the parameter declaration.
        ///    </para>
        /// </devdoc>
        public CodeAttributeDeclarationCollection CustomAttributes
        {
            get
            {
                if (_customAttributes == null)
                {
                    _customAttributes = new CodeAttributeDeclarationCollection();
                }
                return _customAttributes;
            }
            set
            {
                _customAttributes = value;
            }
        }

        /// <devdoc>
        ///    <para>
        ///       Gets or sets
        ///       the type of the parameter.
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

        /// <devdoc>
        ///    <para>
        ///       Gets or sets
        ///       the name of the parameter.
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
    }
}
