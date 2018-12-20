// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;

namespace System.Collections.Generic
{
    /// <summary>
    /// Represents a position within a <see cref="T:System.Collections.Generic.LargeArrayBuilder`1"/>.
    /// </summary>
    [DebuggerDisplay("{DebuggerDisplay,nq}")]
    internal readonly struct CopyPosition
    {
        /// <summary>
        /// Constructs a new <see cref="CopyPosition"/>.
        /// </summary>
        /// <param name="row">The index of the buffer to select.</param>
        /// <param name="column">The index within the buffer to select.</param>
        internal CopyPosition(int row, int column)
        {
            Debug.Assert(row >= 0);
            Debug.Assert(column >= 0);

            Row = row;
            Column = column;
        }

        /// <summary>
        /// Represents a position at the start of a <see cref="T:System.Collections.Generic.LargeArrayBuilder`1"/>.
        /// </summary>
        public static CopyPosition Start => default(CopyPosition);

        /// <summary>
        /// The index of the buffer to select.
        /// </summary>
        internal int Row { get; }

        /// <summary>
        /// The index within the buffer to select.
        /// </summary>
        internal int Column { get; }

        /// <summary>
        /// If this position is at the end of the current buffer, returns the position
        /// at the start of the next buffer. Otherwise, returns this position.
        /// </summary>
        /// <param name="endColumn">The length of the current buffer.</param>
        public CopyPosition Normalize(int endColumn)
        {
            Debug.Assert(Column <= endColumn);

            return Column == endColumn ?
                new CopyPosition(Row + 1, 0) :
                this;
        }

        /// <summary>
        /// Gets a string suitable for display in the debugger.
        /// </summary>
        private string DebuggerDisplay => $"[{Row}, {Column}]";
    }
}
