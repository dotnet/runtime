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
    ///       Represents a single custom attribute.
    ///    </para>
    /// </devdoc>
    [
        ComVisible(true)
    ]
    internal class CodeAttributeDeclaration
    {
        private string _name;
        private CodeAttributeArgumentCollection _arguments = new CodeAttributeArgumentCollection();
        private CodeTypeReference _attributeType;

        /// <devdoc>
        ///    <para>
        ///       Initializes a new instance of <see cref='System.CodeDom.CodeAttributeDeclaration'/>.
        ///    </para>
        /// </devdoc>
        public CodeAttributeDeclaration()
        {
        }

        /// <devdoc>
        ///    <para>
        ///       Initializes a new instance of <see cref='System.CodeDom.CodeAttributeDeclaration'/> using the specified name.
        ///    </para>
        /// </devdoc>
        public CodeAttributeDeclaration(string name)
        {
            Name = name;
        }

        /// <devdoc>
        ///    <para>
        ///       Initializes a new instance of <see cref='System.CodeDom.CodeAttributeDeclaration'/> using the specified
        ///       arguments.
        ///    </para>
        /// </devdoc>
        public CodeAttributeDeclaration(string name, params CodeAttributeArgument[] arguments)
        {
            Name = name;
            Arguments.AddRange(arguments);
        }

        public CodeAttributeDeclaration(CodeTypeReference attributeType) : this(attributeType, null)
        {
        }

        public CodeAttributeDeclaration(CodeTypeReference attributeType, params CodeAttributeArgument[] arguments)
        {
            _attributeType = attributeType;
            if (attributeType != null)
            {
                _name = attributeType.BaseType;
            }

            if (arguments != null)
            {
                Arguments.AddRange(arguments);
            }
        }

        /// <devdoc>
        ///    <para>
        ///       The name of the attribute being declared.
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
                _attributeType = new CodeTypeReference(_name);
            }
        }

        /// <devdoc>
        ///    <para>
        ///       The arguments for the attribute.
        ///    </para>
        /// </devdoc>
        public CodeAttributeArgumentCollection Arguments
        {
            get
            {
                return _arguments;
            }
        }

        public CodeTypeReference AttributeType
        {
            get
            {
                return _attributeType;
            }
        }
    }
}
