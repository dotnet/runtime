// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.CodeDom.Compiler
{
    using System;
    using System.CodeDom;
    using System.Globalization;


    /// <devdoc>
    ///    <para>
    ///       Represents a compiler error.
    ///    </para>
    /// </devdoc>
    internal class CompilerError
    {
        private int _line;
        private int _column;
        private string _errorNumber;
        private bool _warning = false;
        private string _errorText;
        private string _fileName;

        /// <devdoc>
        ///    <para>
        ///       Initializes a new instance of <see cref='System.CodeDom.Compiler.CompilerError'/>.
        ///    </para>
        /// </devdoc>
        public CompilerError()
        {
            _line = 0;
            _column = 0;
            _errorNumber = string.Empty;
            _errorText = string.Empty;
            _fileName = string.Empty;
        }
        /// <devdoc>
        ///    <para>
        ///       Initializes a new instance of <see cref='System.CodeDom.Compiler.CompilerError'/> using the specified
        ///       filename, line, column, error number and error text.
        ///    </para>
        /// </devdoc>
        public CompilerError(string fileName, int line, int column, string errorNumber, string errorText)
        {
            _line = line;
            _column = column;
            _errorNumber = errorNumber;
            _errorText = errorText;
            _fileName = fileName;
        }

        /// <devdoc>
        ///    <para>
        ///       Gets or sets the line number where the source of the error occurs.
        ///    </para>
        /// </devdoc>
        public int Line
        {
            get
            {
                return _line;
            }
            set
            {
                _line = value;
            }
        }

        /// <devdoc>
        ///    <para>
        ///       Gets or sets the column number where the source of the error occurs.
        ///    </para>
        /// </devdoc>
        public int Column
        {
            get
            {
                return _column;
            }
            set
            {
                _column = value;
            }
        }

        /// <devdoc>
        ///    <para>
        ///       Gets or sets the error number.
        ///    </para>
        /// </devdoc>
        public string ErrorNumber
        {
            get
            {
                return _errorNumber;
            }
            set
            {
                _errorNumber = value;
            }
        }

        /// <devdoc>
        ///    <para>
        ///       Gets or sets the text of the error message.
        ///    </para>
        /// </devdoc>
        public string ErrorText
        {
            get
            {
                return _errorText;
            }
            set
            {
                _errorText = value;
            }
        }

        /// <devdoc>
        ///    <para>
        ///       Gets or sets
        ///       a value indicating whether the error is a warning.
        ///    </para>
        /// </devdoc>
        public bool IsWarning
        {
            get
            {
                return _warning;
            }
            set
            {
                _warning = value;
            }
        }

        /// <devdoc>
        ///    <para>
        ///       Gets or sets the filename of the source that caused the error.
        ///    </para>
        /// </devdoc>
        public string FileName
        {
            get
            {
                return _fileName;
            }
            set
            {
                _fileName = value;
            }
        }

        /// <devdoc>
        ///    <para>
        ///       Overrides Object's ToString.
        ///    </para>
        /// </devdoc>
        public override string ToString()
        {
            if (FileName.Length > 0)
            {
                return string.Format(CultureInfo.InvariantCulture, "{0}({1},{2}) : {3} {4}: {5}",
                                     new object[] {
                                        FileName,
                                        Line,
                                        Column,
                                        IsWarning ? "warning" : "error",
                                        ErrorNumber,
                                        ErrorText});
            }
            else
                return string.Format(CultureInfo.InvariantCulture, "{0} {1}: {2}",
                                        IsWarning ? "warning" : "error",
                                        ErrorNumber,
                                        ErrorText);
        }
    }
}

