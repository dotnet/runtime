// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection.Internal;
using System.Reflection.Metadata.Ecma335;

namespace System.Reflection.Metadata
{
    [DebuggerDisplay("{GetDebuggerDisplay(),nq}")]
    public struct SequencePointBlobReader : IEnumerator<SequencePoint>
    {
        private BlobReader _reader;
        private SequencePoint _current;
        private int _previousNonHiddenStartLine;
        private ushort _previousNonHiddenStartColumn;

        public unsafe SequencePointBlobReader(byte* buffer, int length)
            : this(MemoryBlock.CreateChecked(buffer, length))
        {
        }

        internal SequencePointBlobReader(MemoryBlock block)
        {
            _reader = new BlobReader(block);
            _current = default(SequencePoint);
            _previousNonHiddenStartLine = -1;
            _previousNonHiddenStartColumn = 0;
        }

        public bool MoveNext()
        {
            if (_reader.RemainingBytes == 0)
            {
                return false;
            }

            DocumentHandle document;
            int offset, deltaLines, deltaColumns, startLine;
            ushort startColumn;

            if (_current.Document.IsNil)
            {
                // header (skip local signature rid):
                _reader.ReadCompressedInteger();
                document = ReadDocumentHandle();

                // IL offset:
                offset = _reader.ReadCompressedInteger();
            }
            else
            {
                document = _current.Document;

                // skip all document records and update the current document accordingly:
                int deltaOffset;
                while ((deltaOffset = _reader.ReadCompressedInteger()) == 0)
                {
                    document = ReadDocumentHandle();
                }

                // IL offset:
                offset = AddOffsets(_current.Offset, deltaOffset);
            }

            ReadDeltaLinesAndColumns(out deltaLines, out deltaColumns);

            // hidden
            if (deltaLines == 0 && deltaColumns == 0)
            {
                _current = new SequencePoint(document, offset);
                return true;
            }

            // delta Start Line & Column:
            if (_previousNonHiddenStartLine < 0)
            {
                Debug.Assert(_previousNonHiddenStartColumn == 0);

                startLine = ReadLine();
                startColumn = ReadColumn();
            }
            else
            {
                startLine = AddLines(_previousNonHiddenStartLine, _reader.ReadCompressedSignedInteger());
                startColumn = AddColumns(_previousNonHiddenStartColumn, _reader.ReadCompressedSignedInteger());
            }

            _previousNonHiddenStartLine = startLine;
            _previousNonHiddenStartColumn = startColumn;

            _current = new SequencePoint(
                document,
                offset,
                startLine,
                startColumn,
                AddLines(startLine, deltaLines),
                AddColumns(startColumn, deltaColumns));

            return true;
        }

        private void ReadDeltaLinesAndColumns(out int deltaLines, out int deltaColumns)
        {
            deltaLines = _reader.ReadCompressedInteger();
            deltaColumns = (deltaLines == 0) ? _reader.ReadCompressedInteger() : _reader.ReadCompressedSignedInteger();
        }

        private int ReadLine()
        {
            return _reader.ReadCompressedInteger();
        }

        private ushort ReadColumn()
        {
            int column = _reader.ReadCompressedInteger();
            if (column > ushort.MaxValue)
            {
                // TODO:
                throw new BadImageFormatException();
            }

            return (ushort)column;
        }

        private int AddOffsets(int value, int delta)
        {
            // TODO:
            int result = unchecked(value + delta);
            if (result < 0 || result > int.MaxValue)
            {
                throw new BadImageFormatException();
            }

            return result;
        }

        private int AddLines(int value, int delta)
        {
            // TODO:
            int result = unchecked(value + delta);
            if (result < 0 || result >= SequencePoint.HiddenLine)
            {
                throw new BadImageFormatException();
            }

            return result;
        }

        private ushort AddColumns(ushort value, int delta)
        {
            // TODO:
            int result = unchecked(value + delta);
            if (result < 0 || result >= ushort.MaxValue)
            {
                throw new BadImageFormatException();
            }

            return (ushort)result;
        }

        private DocumentHandle ReadDocumentHandle()
        {
            int rowId = _reader.ReadCompressedInteger();
            if (rowId == 0 || !TokenTypeIds.IsValidRowId(rowId))
            {
                Throw.InvalidHandle();
            }

            return DocumentHandle.FromRowId(rowId);
        }

        public SequencePoint Current
        {
            get { return _current; }
        }

        object IEnumerator.Current
        {
            get { return _current; }
        }

        public void Reset()
        {
            _reader.SeekOffset(0);
            _current = default(SequencePoint);
        }

        public void Dispose()
        {
        }

        private string GetDebuggerDisplay()
        {
            return _reader.GetDebuggerDisplay();
        }
    }
}
