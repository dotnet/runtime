// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Reflection.Internal;
using System.Diagnostics.CodeAnalysis;

namespace System.Reflection.Metadata
{
    [DebuggerDisplay("{GetDebuggerDisplay(),nq}")]
    public readonly struct SequencePoint : IEquatable<SequencePoint>
    {
        public const int HiddenLine = 0xfeefee;

        public DocumentHandle Document { get; }
        public int Offset { get; }
        public int StartLine { get; }
        public int EndLine { get; }
        public int StartColumn { get; }
        public int EndColumn { get; }

        internal SequencePoint(DocumentHandle document, int offset)
        {
            Document = document;
            Offset = offset;
            StartLine = HiddenLine;
            StartColumn = 0;
            EndLine = HiddenLine;
            EndColumn = 0;
        }

        internal SequencePoint(DocumentHandle document, int offset, int startLine, ushort startColumn, int endLine, ushort endColumn)
        {
            Document = document;
            Offset = offset;
            StartLine = startLine;
            StartColumn = startColumn;
            EndLine = endLine;
            EndColumn = endColumn;
        }

        public override int GetHashCode()
        {
            return Hash.Combine(Document.RowId,
                   Hash.Combine(Offset,
                   Hash.Combine(StartLine,
                   Hash.Combine(StartColumn,
                   Hash.Combine(EndLine, EndColumn)))));
        }

        public override bool Equals([NotNullWhen(true)] object? obj)
        {
            return obj is SequencePoint sequencePoint && Equals(sequencePoint);
        }

        public bool Equals(SequencePoint other)
        {
            return Document == other.Document
                && Offset == other.Offset
                && StartLine == other.StartLine
                && StartColumn == other.StartColumn
                && EndLine == other.EndLine
                && EndColumn == other.EndColumn;
        }

        public bool IsHidden => StartLine == 0xfeefee;

        private string GetDebuggerDisplay()
        {
            return IsHidden ? "<hidden>" : $"{Offset}: ({StartLine}, {StartColumn}) - ({EndLine}, {EndColumn})";
        }
    }
}
