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
    ///       namespace declaration.
    ///    </para>
    /// </devdoc>
    [
        ComVisible(true)
    ]
    internal class CodeNamespace : CodeObject
    {
        private string _name;
        private CodeNamespaceImportCollection _imports = new CodeNamespaceImportCollection();
        private CodeCommentStatementCollection _comments = new CodeCommentStatementCollection();
        private CodeTypeDeclarationCollection _classes = new CodeTypeDeclarationCollection();
        private CodeNamespaceCollection _namespaces = new CodeNamespaceCollection();

        private int _populated = 0x0;
        private const int ImportsCollection = 0x1;
        private const int CommentsCollection = 0x2;
        private const int TypesCollection = 0x4;

#if CODEDOM_NESTED_NAMESPACES
        private const int NamespacesCollection = 0x8;
#endif



        /// <devdoc>
        ///    <para>
        ///       An event that will be fired the first time the Comments Collection is accessed.  
        ///    </para>
        /// </devdoc>
        public event EventHandler PopulateComments;

        /// <devdoc>
        ///    <para>
        ///       An event that will be fired the first time the Imports Collection is accessed.  
        ///    </para>
        /// </devdoc>
        public event EventHandler PopulateImports;

#if CODEDOM_NESTED_NAMESPACES
         /// <devdoc>
        ///    <para>
        ///       An event that will be fired the first time the Namespaces Collection is accessed.  
        ///    </para>
        /// </devdoc>
        public event EventHandler PopulateNamespaces;
#endif


        /// <devdoc>
        ///    <para>
        ///       An event that will be fired the first time the Types Collection is accessed.  
        ///    </para>
        /// </devdoc>
        public event EventHandler PopulateTypes;

        /// <devdoc>
        ///    <para>
        ///       Initializes a new instance of <see cref='System.CodeDom.CodeNamespace'/>.
        ///    </para>
        /// </devdoc>
        public CodeNamespace()
        {
        }

        /// <devdoc>
        ///    <para>
        ///       Initializes a new instance of <see cref='System.CodeDom.CodeNamespace'/> using the specified name.
        ///    </para>
        /// </devdoc>
        public CodeNamespace(string name)
        {
            Name = name;
        }

        /// <devdoc>
        ///    <para>
        ///       Gets or sets the collection of classes.
        ///    </para>
        /// </devdoc>
        public CodeTypeDeclarationCollection Types
        {
            get
            {
                if (0 == (_populated & TypesCollection))
                {
                    _populated |= TypesCollection;
                    if (PopulateTypes != null) PopulateTypes(this, EventArgs.Empty);
                }
                return _classes;
            }
        }

        /// <devdoc>
        ///    <para>
        ///       Gets or sets the collection of namespace imports used by the represented
        ///       namespace.
        ///    </para>
        /// </devdoc>
        public CodeNamespaceImportCollection Imports
        {
            get
            {
                if (0 == (_populated & ImportsCollection))
                {
                    _populated |= ImportsCollection;
                    if (PopulateImports != null) PopulateImports(this, EventArgs.Empty);
                }
                return _imports;
            }
        }

        /// <devdoc>
        ///    <para>
        ///       Gets or sets the name of the namespace.
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


#if CODEDOM_NESTED_NAMESPACES
        
        /// <devdoc>
        ///    <para>
        ///       Gets or sets the collection of Namespaces.
        ///    </para>
        /// </devdoc>
        public CodeNamespaceCollection Namespaces {
            get {
                if (0 == (populated & NamespacesCollection)) {
                    populated |= NamespacesCollection;
                    if (PopulateNamespaces != null) PopulateNamespaces(this, EventArgs.Empty);
                }
                return namespaces;
            }
        }

#endif

        /// <devdoc>
        ///    <para>
        ///       Gets or sets the member comment collection members.
        ///    </para>
        /// </devdoc>
        public CodeCommentStatementCollection Comments
        {
            get
            {
                if (0 == (_populated & CommentsCollection))
                {
                    _populated |= CommentsCollection;
                    if (PopulateComments != null) PopulateComments(this, EventArgs.Empty);
                }
                return _comments;
            }
        }
    }
}
