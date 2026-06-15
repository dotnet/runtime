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
        /// Gets or sets the number of columns for the pseudo-terminal window size.
        /// </summary>
        /// <value>The number of columns, or <see langword="null"/> to use the system default.</value>
        public int? Columns { get; set; }

        /// <summary>
        /// Gets or sets the number of rows for the pseudo-terminal window size.
        /// </summary>
        /// <value>The number of rows, or <see langword="null"/> to use the system default.</value>
        public int? Rows { get; set; }
    }
}
