// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Reflection.Emit
{
    internal sealed class SequencePoint
    {
        public const int HiddenLine = 0xfeefee;

        public int Offset { get; }
        public int StartLine { get; }
        public int EndLine { get; }
        public int StartColumn { get; }
        public int EndColumn { get; }

        public SequencePoint(int offset, int startLine, int startColumn, int endLine, int endColumn)
        {
            Offset = offset;
            StartLine = startLine;
            EndLine = endLine;
            StartColumn = startColumn;
            EndColumn = endColumn;
        }

        public bool IsHidden => StartLine == HiddenLine;
    }
}
