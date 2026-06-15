// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Diagnostics
{
    /// <summary>
    /// Represents a pseudo-terminal (PTY) device that can be used to spawn child processes
    /// with a controlling terminal.
    /// </summary>
    public sealed partial class PseudoTerminal : IDisposable
    {
        /// <summary>
        /// Creates a new pseudo-terminal with the specified options.
        /// </summary>
        /// <param name="options">Optional configuration for the pseudo-terminal, such as window size.</param>
        /// <returns>A new <see cref="PseudoTerminal"/> instance.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="options"/> is <see langword="null"/>.</exception>
        /// <exception cref="System.ComponentModel.Win32Exception">The pseudo-terminal could not be created.</exception>
        public static PseudoTerminal Create(PseudoTerminalOptions options)
        {
            ArgumentNullException.ThrowIfNull(options);
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(options.Columns);
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(options.Rows);

            return CreateCore(options);
        }

        /// <summary>
        /// Resizes the pseudo-terminal window to the specified dimensions.
        /// </summary>
        /// <param name="columns">The new number of columns.</param>
        /// <param name="rows">The new number of rows.</param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="columns"/> or <paramref name="rows"/> is less than or equal to zero.</exception>
        public void Resize(int columns, int rows)
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(columns);
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(rows);

            ResizeCore(columns, rows);
        }

        /// <summary>
        /// Releases all resources used by the <see cref="PseudoTerminal"/>.
        /// </summary>
        /// <remarks>
        /// On Windows, closing the pseudo-terminal sends a CTRL_CLOSE_EVENT to each client application
        /// that is still connected to the pseudo console.
        /// On Unix, closing the primary file descriptor causes the secondary side to receive an EOF,
        /// which typically results in a SIGHUP being sent to the foreground process group.
        /// </remarks>
        public void Dispose() => DisposeCore();
    }
}
