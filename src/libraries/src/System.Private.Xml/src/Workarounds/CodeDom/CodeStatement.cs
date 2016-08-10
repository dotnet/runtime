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
    ///       Represents a statement.
    ///    </para>
    /// </devdoc>
    [
        ComVisible(true)
    ]
    internal class CodeStatement : CodeObject
    {
        private CodeLinePragma _linePragma;

        // Optionally Serializable
        private CodeDirectiveCollection _startDirectives = null;
        private CodeDirectiveCollection _endDirectives = null;


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
