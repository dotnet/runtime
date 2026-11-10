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
        /// <param name="canceled">A value indicating whether the process has been terminated due to timeout or cancellation.</param>
        /// <param name="signal">On Unix, the POSIX signal that terminated the process, or null if the process exited normally.</param>
        public ProcessExitStatus(int exitCode, bool canceled, PosixSignal? signal = null)
        {
            ExitCode = exitCode;
            Canceled = canceled;
            Signal = signal;
        }

        /// <summary>
        /// Gets the exit code of the process.
        /// </summary>
        /// <remarks>
        /// <para>
        /// If the process was terminated by a signal on Unix, this is 128 + the signal number.
        /// Use <see cref="Signal"/> to get the actual signal.
        /// </para>
        /// </remarks>
        public int ExitCode { get; }

        /// <summary>
        /// Gets the POSIX signal that terminated the process on Unix, or <see langword="null" /> if the process was not terminated by a signal.
        /// </summary>
        /// <remarks>
        /// <para>
        /// On Unix, a process can be terminated by a signal (e.g., SIGKILL, SIGTERM). When this happens,
        /// the kernel reports "terminated by signal X" rather than an exit code. This property captures
        /// that signal. When the process exits normally on Unix, or on Windows where signals do not exist,
        /// this property is <see langword="null" />.
        /// </para>
        /// </remarks>
        public PosixSignal? Signal { get; }

        /// <summary>
        /// Gets a value indicating whether the process has been terminated due to timeout or cancellation.
        /// </summary>
        public bool Canceled { get; }
    }
}
