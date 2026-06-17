// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Diagnostics
{
    /// <summary>
    /// Specifies options for creating a <see cref="PseudoTerminal"/>.
    /// </summary>
    public class PseudoTerminalOptions
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PseudoTerminalOptions"/> class.
        /// </summary>
        /// <param name="columns">The number of columns for the pseudo-terminal window size.</param>
        /// <param name="rows">The number of rows for the pseudo-terminal window size.</param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="columns"/> or <paramref name="rows"/> is less than or equal to zero, or greater than <see cref="short.MaxValue"/>.</exception>
        public PseudoTerminalOptions(int columns, int rows)
        {
            ValidateDimensions(columns, rows);
            Columns = columns;
            Rows = rows;
        }

        /// <summary>
        /// Gets the number of columns for the pseudo-terminal window size.
        /// </summary>
        public int Columns { get; }

        /// <summary>
        /// Gets the number of rows for the pseudo-terminal window size.
        /// </summary>
        public int Rows { get; }

        internal static void ValidateDimensions(int columns, int rows)
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(columns);
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(rows);
            ArgumentOutOfRangeException.ThrowIfGreaterThan(columns, short.MaxValue, nameof(columns));
            ArgumentOutOfRangeException.ThrowIfGreaterThan(rows, short.MaxValue, nameof(rows));
        }
    }
}
