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
    ///       Represents a class constructor.
    ///    </para>
    /// </devdoc>
    [
        ComVisible(true)
    ]
    internal class CodeConstructor : CodeMemberMethod
    {
        private CodeExpressionCollection _baseConstructorArgs = new CodeExpressionCollection();
        private CodeExpressionCollection _chainedConstructorArgs = new CodeExpressionCollection();

        /// <devdoc>
        ///    <para>
        ///       Initializes a new instance of <see cref='System.CodeDom.CodeConstructor'/>.
        ///    </para>
        /// </devdoc>
        public CodeConstructor()
        {
            Name = ".ctor";
        }

        /// <devdoc>
        ///    <para>
        ///       Gets or sets the base constructor arguments.
        ///    </para>
        /// </devdoc>
        public CodeExpressionCollection BaseConstructorArgs
        {
            get
            {
                return _baseConstructorArgs;
            }
        }

        /// <devdoc>
        ///    <para>
        ///       Gets or sets the chained constructor arguments.
        ///    </para>
        /// </devdoc>
        public CodeExpressionCollection ChainedConstructorArgs
        {
            get
            {
                return _chainedConstructorArgs;
            }
        }
    }
}
