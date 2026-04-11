// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Diagnostics
{
    /// <summary>
    /// Represents the captured text output and exit status of a completed process.
    /// </summary>
    public sealed class ProcessTextOutput
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ProcessTextOutput"/> class.
        /// </summary>
        /// <param name="exitStatus">The exit status of the process.</param>
        /// <param name="standardOutput">The captured standard output text.</param>
        /// <param name="standardError">The captured standard error text.</param>
        /// <param name="processId">The process ID of the completed process.</param>
        public ProcessTextOutput(ProcessExitStatus exitStatus, string standardOutput, string standardError, int processId)
        {
            ArgumentNullException.ThrowIfNull(exitStatus);
            ArgumentNullException.ThrowIfNull(standardOutput);
            ArgumentNullException.ThrowIfNull(standardError);

            ExitStatus = exitStatus;
            StandardOutput = standardOutput;
            StandardError = standardError;
            ProcessId = processId;
        }

        /// <summary>
        /// Gets the exit status of the process.
        /// </summary>
        public ProcessExitStatus ExitStatus { get; }

        /// <summary>
        /// Gets the captured standard output text.
        /// </summary>
        public string StandardOutput { get; }

        /// <summary>
        /// Gets the captured standard error text.
        /// </summary>
        public string StandardError { get; }

        /// <summary>
        /// Gets the process ID of the completed process.
        /// </summary>
        public int ProcessId { get; }
    }
}
