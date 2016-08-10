// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.CodeDom
{
    using System.Diagnostics;
    using System;
    using Microsoft.Win32;
    using System.Collections;
    using System.Reflection;
    using System.Runtime.InteropServices;

    /// <devdoc>
    ///    <para>
    ///       Represents a class member.
    ///    </para>
    /// </devdoc>
    [
        ComVisible(true)
    ]
    internal class CodeTypeMember : CodeObject
    {
        private MemberAttributes _attributes = MemberAttributes.Private | MemberAttributes.Final;
        private string _name;
        private CodeCommentStatementCollection _comments = new CodeCommentStatementCollection();
        private CodeAttributeDeclarationCollection _customAttributes = null;
        private CodeLinePragma _linePragma;

        // Optionally Serializable
        private CodeDirectiveCollection _startDirectives = null;
        private CodeDirectiveCollection _endDirectives = null;


        /// <devdoc>
        ///    <para>
        ///       Gets or sets
        ///       the name of the member.
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
        ///       Gets or sets a <see cref='System.CodeDom.MemberAttributes'/> indicating
        ///       the attributes of the member.
        ///    </para>
        /// </devdoc>
        public MemberAttributes Attributes
        {
            get
            {
                return _attributes;
            }
            set
            {
                _attributes = value;
            }
        }

        /// <devdoc>
        ///    <para>
        ///       Gets or sets a <see cref='System.CodeDom.CodeAttributeDeclarationCollection'/> indicating
        ///       the custom attributes of the
        ///       member.
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
        ///       The line the statement occurs on.
        ///    </para>
        /// </devdoc>
        public CodeLinePragma LinePragma
        {
            get
            {
                return _linePragma;
            }
            set
            {
                _linePragma = value;
            }
        }

        /// <devdoc>
        ///    <para>
        ///       Gets or sets the member comment collection members.
        ///    </para>
        /// </devdoc>
        public CodeCommentStatementCollection Comments
        {
            get
            {
                return _comments;
            }
        }

        public CodeDirectiveCollection StartDirectives
        {
            get
            {
                if (_startDirectives == null)
                {
                    _startDirectives = new CodeDirectiveCollection();
                }
                return _startDirectives;
            }
        }

        public CodeDirectiveCollection EndDirectives
        {
            get
            {
                if (_endDirectives == null)
                {
                    _endDirectives = new CodeDirectiveCollection();
                }
                return _endDirectives;
            }
        }
    }
}

