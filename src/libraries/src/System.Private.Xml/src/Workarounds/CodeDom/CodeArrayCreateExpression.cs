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
    ///    <para> Represents
    ///       an expression that creates an array.</para>
    /// </devdoc>
    [
        ComVisible(true)
    ]
    internal class CodeArrayCreateExpression : CodeExpression
    {
        private CodeTypeReference _createType;
        private CodeExpressionCollection _initializers = new CodeExpressionCollection();
        private CodeExpression _sizeExpression;
        private int _size = 0;

        /// <devdoc>
        ///    <para>
        ///       Initializes a new instance of <see cref='System.CodeDom.CodeArrayCreateExpression'/>.
        ///    </para>
        /// </devdoc>
        public CodeArrayCreateExpression()
        {
        }

        /// <devdoc>
        ///    <para>
        ///       Initializes a new instance of <see cref='System.CodeDom.CodeArrayCreateExpression'/> with the specified
        ///       array type and initializers.
        ///    </para>
        /// </devdoc>
        public CodeArrayCreateExpression(CodeTypeReference createType, params CodeExpression[] initializers)
        {
            _createType = createType;
            _initializers.AddRange(initializers);
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public CodeArrayCreateExpression(string createType, params CodeExpression[] initializers)
        {
            _createType = new CodeTypeReference(createType);
            _initializers.AddRange(initializers);
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public CodeArrayCreateExpression(Type createType, params CodeExpression[] initializers)
        {
            _createType = new CodeTypeReference(createType);
            _initializers.AddRange(initializers);
        }

        /// <devdoc>
        ///    <para>
        ///       Initializes a new instance of <see cref='System.CodeDom.CodeArrayCreateExpression'/>. with the specified array
        ///       type and size.
        ///    </para>
        /// </devdoc>
        public CodeArrayCreateExpression(CodeTypeReference createType, int size)
        {
            _createType = createType;
            _size = size;
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public CodeArrayCreateExpression(string createType, int size)
        {
            _createType = new CodeTypeReference(createType);
            _size = size;
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public CodeArrayCreateExpression(Type createType, int size)
        {
            _createType = new CodeTypeReference(createType);
            _size = size;
        }

        /// <devdoc>
        ///    <para>
        ///       Initializes a new instance of <see cref='System.CodeDom.CodeArrayCreateExpression'/>. with the specified array
        ///       type and size.
        ///    </para>
        /// </devdoc>
        public CodeArrayCreateExpression(CodeTypeReference createType, CodeExpression size)
        {
            _createType = createType;
            _sizeExpression = size;
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public CodeArrayCreateExpression(string createType, CodeExpression size)
        {
            _createType = new CodeTypeReference(createType);
            _sizeExpression = size;
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public CodeArrayCreateExpression(Type createType, CodeExpression size)
        {
            _createType = new CodeTypeReference(createType);
            _sizeExpression = size;
        }

        /// <devdoc>
        ///    <para>
        ///       Gets or sets
        ///       the type of the array to create.
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
        ///       Gets or sets
        ///       the initializers to initialize the array with.
        ///    </para>
        /// </devdoc>
        public CodeExpressionCollection Initializers
        {
            get
            {
                return _initializers;
            }
        }

        /// <devdoc>
        ///    <para>
        ///       Gets or sets
        ///       the size of the array.
        ///    </para>
        /// </devdoc>
        public int Size
        {
            get
            {
                return _size;
            }
            set
            {
                _size = value;
            }
        }

        /// <devdoc>
        ///    <para>Gets or sets the size of the array.</para>
        /// </devdoc>
        public CodeExpression SizeExpression
        {
            get
            {
                return _sizeExpression;
            }
            set
            {
                _sizeExpression = value;
            }
        }
    }
}
