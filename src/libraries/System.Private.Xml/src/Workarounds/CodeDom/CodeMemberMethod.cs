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
    ///       Represents a class method.
    ///    </para>
    /// </devdoc>
    [
        ComVisible(true)
    ]
    internal class CodeMemberMethod : CodeTypeMember
    {
        private CodeParameterDeclarationExpressionCollection _parameters = new CodeParameterDeclarationExpressionCollection();
        private CodeStatementCollection _statements = new CodeStatementCollection();
        private CodeTypeReference _returnType;
        private CodeTypeReference _privateImplements = null;
        private CodeTypeReferenceCollection _implementationTypes = null;
        private CodeAttributeDeclarationCollection _returnAttributes = null;

        private CodeTypeParameterCollection _typeParameters;

        private int _populated = 0x0;
        private const int ParametersCollection = 0x1;
        private const int StatementsCollection = 0x2;
        private const int ImplTypesCollection = 0x4;

        /// <devdoc>
        ///    <para>
        ///       An event that will be fired the first time the Parameters Collection is accessed.  
        ///    </para>
        /// </devdoc>
        public event EventHandler PopulateParameters;

        /// <devdoc>
        ///    <para>
        ///       An event that will be fired the first time the Statements Collection is accessed.  
        ///    </para>
        /// </devdoc>
        public event EventHandler PopulateStatements;

        /// <devdoc>
        ///    <para>
        ///       An event that will be fired the first time the ImplementationTypes Collection is accessed.  
        ///    </para>
        /// </devdoc>
        public event EventHandler PopulateImplementationTypes;

        /// <devdoc>
        ///    <para>
        ///       Gets or sets the return type of the method.
        ///    </para>
        /// </devdoc>
        public CodeTypeReference ReturnType
        {
            get
            {
                if (_returnType == null)
                {
                    _returnType = new CodeTypeReference(typeof(void).FullName);
                }
                return _returnType;
            }
            set
            {
                _returnType = value;
            }
        }

        /// <devdoc>
        ///    <para>
        ///       Gets or sets the statements within the method.
        ///    </para>
        /// </devdoc>
        public CodeStatementCollection Statements
        {
            get
            {
                if (0 == (_populated & StatementsCollection))
                {
                    _populated |= StatementsCollection;
                    if (PopulateStatements != null) PopulateStatements(this, EventArgs.Empty);
                }
                return _statements;
            }
        }

        /// <devdoc>
        ///    <para>
        ///       Gets or sets the parameter declarations for the method.
        ///    </para>
        /// </devdoc>
        public CodeParameterDeclarationExpressionCollection Parameters
        {
            get
            {
                if (0 == (_populated & ParametersCollection))
                {
                    _populated |= ParametersCollection;
                    if (PopulateParameters != null) PopulateParameters(this, EventArgs.Empty);
                }
                return _parameters;
            }
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public CodeTypeReference PrivateImplementationType
        {
            get
            {
                return _privateImplements;
            }
            set
            {
                _privateImplements = value;
            }
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public CodeTypeReferenceCollection ImplementationTypes
        {
            get
            {
                if (_implementationTypes == null)
                {
                    _implementationTypes = new CodeTypeReferenceCollection();
                }

                if (0 == (_populated & ImplTypesCollection))
                {
                    _populated |= ImplTypesCollection;
                    if (PopulateImplementationTypes != null) PopulateImplementationTypes(this, EventArgs.Empty);
                }
                return _implementationTypes;
            }
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public CodeAttributeDeclarationCollection ReturnTypeCustomAttributes
        {
            get
            {
                if (_returnAttributes == null)
                {
                    _returnAttributes = new CodeAttributeDeclarationCollection();
                }
                return _returnAttributes;
            }
        }

        [System.Runtime.InteropServices.ComVisible(false)]
        public CodeTypeParameterCollection TypeParameters
        {
            get
            {
                if (_typeParameters == null)
                {
                    _typeParameters = new CodeTypeParameterCollection();
                }

                return _typeParameters;
            }
        }
    }
}
