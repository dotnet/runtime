// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#if !FEATURE_SERIALIZATION_UAPAOT
namespace System.Xml.Serialization
{
    using System.IO;

    /// <summary>
    /// This class will write to a stream and manage indentation.
    /// </summary>
    internal class IndentedWriter
    {
        private TextWriter _writer;
        private bool _needIndent;
        private int _indentLevel;
        private bool _compact;

        internal IndentedWriter(TextWriter writer, bool compact)
        {
            _writer = writer;
            _compact = compact;
        }

        internal int Indent
        {
            get
            {
                return _indentLevel;
            }
            set
            {
                _indentLevel = value;
            }
        }

        internal void Write(string s)
        {
            if (_needIndent) WriteIndent();
            _writer.Write(s);
        }

        internal void Write(char c)
        {
            if (_needIndent) WriteIndent();
            _writer.Write(c);
        }

        internal void WriteLine(string s)
        {
            if (_needIndent) WriteIndent();
            _writer.WriteLine(s);
            _needIndent = true;
        }

        internal void WriteLine()
        {
            _writer.WriteLine();
            _needIndent = true;
        }

        internal void WriteIndent()
        {
            _needIndent = false;
            if (!_compact)
            {
                for (int i = 0; i < _indentLevel; i++)
                {
                    _writer.Write("    ");
                }
            }
        }
    }
}
#endif
