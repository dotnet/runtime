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

        public readonly DocumentHandle Document;
        public readonly int Offset;
        public readonly int StartLine;
        public readonly int EndLine;
        public readonly ushort StartColumn;
        public readonly ushort EndColumn;

        internal SequencePoint(DocumentHandle document, int offset)
        {
            this.Document = document;
            this.Offset = offset;
            this.StartLine = HiddenLine;
            this.StartColumn = 0;
            this.EndLine = HiddenLine;
            this.EndColumn = 0;
        }

        internal SequencePoint(DocumentHandle document, int offset, int startLine, ushort startColumn, int endLine, ushort endColumn)
        {
            this.Document = document;
            this.Offset = offset;
            this.StartLine = startLine;
            this.StartColumn = startColumn;
            this.EndLine = endLine;
            this.EndColumn = endColumn;
        }

        public override int GetHashCode()
        {
            return Hash.Combine(Document.RowId,
                   Hash.Combine(Offset,
                   Hash.Combine(StartLine,
                   Hash.Combine(StartColumn,
                   Hash.Combine(EndLine, EndColumn)))));
        }

        public override bool Equals(object obj)
        {
            return obj is SequencePoint && Equals((SequencePoint)obj);
        }

        public bool Equals(SequencePoint other)
        {
            return this.Document == other.Document
                && this.Offset == other.Offset
                && this.StartLine == other.StartLine
                && this.StartColumn == other.StartColumn
                && this.EndLine == other.EndLine
                && this.EndColumn == other.EndColumn;
        }

        public bool IsHidden
        {
            get
            {
                return StartLine == 0xfeefee;
            }
        }

        private string GetDebuggerDisplay()
        {
            return IsHidden ? "<hidden>" : string.Format("{0}: ({1}, {2}) - ({3}, {4})", Offset, StartLine, StartColumn, EndLine, EndColumn);
        }
    }
}
