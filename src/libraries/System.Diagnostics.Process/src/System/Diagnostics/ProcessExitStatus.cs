// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

namespace System.Diagnostics
{
    /// <summary>
    /// Represents the exit status of a process.
    /// </summary>
    public sealed class ProcessExitStatus
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ProcessExitStatus"/> class.
        /// </summary>
        /// <param name="exitCode">The exit code of the process.</param>
        /// <param name="cancelled">A value indicating whether the process has been terminated due to timeout or cancellation.</param>
        /// <param name="signal">The POSIX signal that terminated the process, or null if the process exited normally.</param>
        public ProcessExitStatus(int exitCode, bool cancelled, PosixSignal? signal = null)
        {
            ExitCode = exitCode;
            Canceled = cancelled;
            Signal = signal;
        }

        /// <summary>
        /// Gets the exit code of the process.
        /// </summary>
        /// <remarks>
        /// <para>On Windows, this is the value passed to ExitProcess or returned from main().</para>
        /// <para>On Unix, if the process exited normally, this is the value passed to exit() or returned from main().</para>
        /// <para>
        /// If the process was terminated by a signal on Unix, this is 128 + the signal number.
        /// Use <see cref="Signal"/> to get the actual signal.
        /// </para>
        /// </remarks>
        public int ExitCode { get; }

        /// <summary>
        /// Gets the POSIX signal that terminated the process, or null if the process exited normally.
        /// </summary>
        /// <remarks>
        /// This property is always null on Windows.
        /// </remarks>
        public PosixSignal? Signal { get; }

        /// <summary>
        /// Gets a value indicating whether the process has been terminated due to timeout or cancellation.
        /// </summary>
        public bool Canceled { get; }
    }
}
