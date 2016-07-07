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
    ///       Represents line number information for an external file.
    ///    </para>
    /// </devdoc>
    [
        ComVisible(true)
    ]
    internal class CodeLinePragma
    {
        private string _fileName;
        private int _lineNumber;

        public CodeLinePragma()
        {
        }

        /// <devdoc>
        ///    <para>
        ///       Initializes a new instance of <see cref='System.CodeDom.CodeLinePragma'/>.
        ///    </para>
        /// </devdoc>
        public CodeLinePragma(string fileName, int lineNumber)
        {
            FileName = fileName;
            LineNumber = lineNumber;
        }

        /// <devdoc>
        ///    <para>
        ///       Gets or sets
        ///       the filename of
        ///       the associated file.
        ///    </para>
        /// </devdoc>
        public string FileName
        {
            get
            {
                return (_fileName == null) ? string.Empty : _fileName;
            }
            set
            {
                _fileName = value;
            }
        }

        /// <devdoc>
        ///    <para>
        ///       Gets or sets the line number of the file for
        ///       the current pragma.
        ///    </para>
        /// </devdoc>
        public int LineNumber
        {
            get
            {
                return _lineNumber;
            }
            set
            {
                _lineNumber = value;
            }
        }
    }
}
