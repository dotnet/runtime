// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Reflection.Internal;

namespace System.Reflection.Metadata
{
    [DebuggerDisplay("{GetDebuggerDisplay(),nq}")]
    public struct SequencePoint : IEquatable<SequencePoint>
    {
        public const int HiddenLine = 0xfeefee;

        private DocumentHandle _document;
        private int _offset;
        private int _startLine;
        private int _endLine;
        private ushort _startColumn;
        private ushort _endColumn;

        public DocumentHandle Document { get { return _document; } }
        public int Offset { get { return _offset; } }
        public int StartLine { get { return _startLine; } }
        public int EndLine { get { return _endLine; } }
        public int StartColumn { get { return _startColumn; } }
        public int EndColumn { get { return _endColumn; } }

        internal SequencePoint(DocumentHandle document, int offset)
        {
            _document = document;
            _offset = offset;
            _startLine = HiddenLine;
            _startColumn = 0;
            _endLine = HiddenLine;
            _endColumn = 0;
        }

        internal SequencePoint(DocumentHandle document, int offset, int startLine, ushort startColumn, int endLine, ushort endColumn)
        {
            _document = document;
            _offset = offset;
            _startLine = startLine;
            _startColumn = startColumn;
            _endLine = endLine;
            _endColumn = endColumn;
        }

        public override int GetHashCode()
        {
            return Hash.Combine(_document.RowId,
                   Hash.Combine(_offset,
                   Hash.Combine(_startLine,
                   Hash.Combine(_startColumn,
                   Hash.Combine(_endLine, _endColumn)))));
        }

        public override bool Equals(object obj)
        {
            return obj is SequencePoint && Equals((SequencePoint)obj);
        }

        public bool Equals(SequencePoint other)
        {
            return _document == other._document
                && _offset == other._offset
                && _startLine == other._startLine
                && _startColumn == other._startColumn
                && _endLine == other._endLine
                && _endColumn == other._endColumn;
        }

        public bool IsHidden
        {
            get
            {
                return _startLine == 0xfeefee;
            }
        }

        private string GetDebuggerDisplay()
        {
            return IsHidden ? "<hidden>" : string.Format("{0}: ({1}, {2}) - ({3}, {4})", _offset, _startLine, _startColumn, _endLine, _endColumn);
        }
    }
}
