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
    ///    <para> Represents a comment.</para>
    /// </devdoc>
    [
        ComVisible(true)
    ]
    internal class CodeCommentStatement : CodeStatement
    {
        private CodeComment _comment;

        /// <devdoc>
        ///    <para>
        ///       Initializes a new instance of <see cref='System.CodeDom.CodeCommentStatement'/>.
        ///    </para>
        /// </devdoc>
        public CodeCommentStatement()
        {
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public CodeCommentStatement(CodeComment comment)
        {
            _comment = comment;
        }

        /// <devdoc>
        ///    <para>
        ///       Initializes a new instance of <see cref='System.CodeDom.CodeCommentStatement'/> with the specified text as
        ///       contents.
        ///    </para>
        /// </devdoc>
        public CodeCommentStatement(string text)
        {
            _comment = new CodeComment(text);
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public CodeCommentStatement(string text, bool docComment)
        {
            _comment = new CodeComment(text, docComment);
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public CodeComment Comment
        {
            get
            {
                return _comment;
            }
            set
            {
                _comment = value;
            }
        }
    }
}
