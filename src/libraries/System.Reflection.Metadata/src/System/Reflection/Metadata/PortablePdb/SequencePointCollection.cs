// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection.Internal;
using System.Reflection.Metadata.Ecma335;

namespace System.Reflection.Metadata
{
    public readonly struct SequencePointCollection : IEnumerable<SequencePoint>
    {
        private readonly MemoryBlock _block;
        private readonly DocumentHandle _document;

        internal SequencePointCollection(MemoryBlock block, DocumentHandle document)
        {
            _block = block;
            _document = document;
        }

        public Enumerator GetEnumerator()
        {
            return new Enumerator(_block, _document);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        IEnumerator<SequencePoint> IEnumerable<SequencePoint>.GetEnumerator()
        {
            return GetEnumerator();
        }

        public struct Enumerator : IEnumerator<SequencePoint>
        {
            private BlobReader _reader;
            private SequencePoint _current;
            private int _previousNonHiddenStartLine;
            private ushort _previousNonHiddenStartColumn;

            internal Enumerator(MemoryBlock block, DocumentHandle document)
            {
                _reader = new BlobReader(block);
                _current = new SequencePoint(document, -1);
                _previousNonHiddenStartLine = -1;
                _previousNonHiddenStartColumn = 0;
            }

            public bool MoveNext()
            {
                if (_reader.RemainingBytes == 0)
                {
                    return false;
                }

                DocumentHandle document = _current.Document;
                int offset, deltaLines, deltaColumns, startLine;
                ushort startColumn;

                if (_reader.Offset == 0)
                {
                    // header (skip local signature rid):
                    _reader.ReadCompressedInteger();

                    if (document.IsNil)
                    {
                        document = ReadDocumentHandle();
                    }
                    // IL offset:
                    offset = _reader.ReadCompressedInteger();
                }
                else
                {
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
                    Throw.SequencePointValueOutOfRange();
                }

                return (ushort)column;
            }

            private static int AddOffsets(int value, int delta)
            {
                int result = unchecked(value + delta);
                if (result < 0)
                {
                    Throw.SequencePointValueOutOfRange();
                }

                return result;
            }

            private static int AddLines(int value, int delta)
            {
                int result = unchecked(value + delta);
                if (result < 0 || result >= SequencePoint.HiddenLine)
                {
                    Throw.SequencePointValueOutOfRange();
                }

                return result;
            }

            private static ushort AddColumns(ushort value, int delta)
            {
                int result = unchecked(value + delta);
                if (result < 0 || result >= ushort.MaxValue)
                {
                    Throw.SequencePointValueOutOfRange();
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
                _reader.Reset();
                _current = default(SequencePoint);
            }

            void IDisposable.Dispose()
            {
            }
        }
    }
}
