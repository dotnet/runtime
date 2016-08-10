// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.CodeDom
{
    using System.Diagnostics;
    using System;
    using Microsoft.Win32;
    using System.Collections;
    using System.Collections.Specialized;
    using System.Runtime.InteropServices;

    /// <devdoc>
    ///    <para>
    ///       Represents a
    ///       compilation unit declaration.
    ///    </para>
    /// </devdoc>
    [
        ComVisible(true)
    ]
    internal class CodeCompileUnit : CodeObject
    {
        private CodeNamespaceCollection _namespaces = new CodeNamespaceCollection();
        private StringCollection _assemblies = null;
        private CodeAttributeDeclarationCollection _attributes = null;

        // Optionally Serializable
        private CodeDirectiveCollection _startDirectives = null;
        private CodeDirectiveCollection _endDirectives = null;

        /// <devdoc>
        ///    <para>
        ///       Initializes a new instance of <see cref='System.CodeDom.CodeCompileUnit'/>.
        ///    </para>
        /// </devdoc>
        public CodeCompileUnit()
        {
        }

        /// <devdoc>
        ///    <para>
        ///       Gets or sets the collection of namespaces.
        ///    </para>
        /// </devdoc>
        public CodeNamespaceCollection Namespaces
        {
            get
            {
                return _namespaces;
            }
        }

        /// <devdoc>
        ///    <para>
        ///       Gets the collection of assemblies. Most code generators will not need this, but the Managed
        ///       extensions for C++ code generator and 
        ///       other very low level code generators will need to do a more complete compilation. If both this
        ///       and the compiler assemblies are specified, the compiler assemblies should win.
        ///    </para>
        /// </devdoc>
        public StringCollection ReferencedAssemblies
        {
            get
            {
                if (_assemblies == null)
                {
                    _assemblies = new StringCollection();
                }
                return _assemblies;
            }
        }

        /// <devdoc>
        ///    <para>
        ///       Gets the collection of assembly level attributes.
        ///    </para>
        /// </devdoc>
        public CodeAttributeDeclarationCollection AssemblyCustomAttributes
        {
            get
            {
                if (_attributes == null)
                {
                    _attributes = new CodeAttributeDeclarationCollection();
                }
                return _attributes;
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
