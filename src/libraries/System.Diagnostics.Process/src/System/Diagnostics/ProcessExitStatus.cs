// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace System.Diagnostics
{
    /// <summary>
    /// Represents the exit status of a process, including exit code, signal information, and cancellation state.
    /// </summary>
    public readonly struct ProcessExitStatus : IEquatable<ProcessExitStatus>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ProcessExitStatus"/> struct.
        /// </summary>
        /// <param name="exitCode">The exit code of the process.</param>
        /// <param name="canceled">Whether the process was terminated due to timeout or cancellation.</param>
        /// <param name="signal">The signal that caused the process to exit, if any.</param>
        internal ProcessExitStatus(int exitCode, bool canceled, PosixSignal? signal = null)
        {
            ExitCode = exitCode;
            Canceled = canceled;
            Signal = signal;
        }

        /// <summary>
        /// Gets the exit code of the process.
        /// </summary>
        public int ExitCode { get; }

        /// <summary>
        /// Gets the signal that caused the process to exit, if any.
        /// </summary>
        public PosixSignal? Signal { get; }

        /// <summary>
        /// Gets a value indicating whether the process has been terminated due to timeout or cancellation.
        /// </summary>
        public bool Canceled { get; }

        /// <inheritdoc />
        public bool Equals(ProcessExitStatus other) =>
            ExitCode == other.ExitCode && Signal == other.Signal && Canceled == other.Canceled;

        /// <inheritdoc />
        public override bool Equals([NotNullWhen(true)] object? obj) =>
            obj is ProcessExitStatus other && Equals(other);

        /// <summary>
        /// Determines whether two <see cref="ProcessExitStatus"/> instances are equal.
        /// </summary>
        public static bool operator ==(ProcessExitStatus left, ProcessExitStatus right) => left.Equals(right);

        /// <summary>
        /// Determines whether two <see cref="ProcessExitStatus"/> instances are not equal.
        /// </summary>
        public static bool operator !=(ProcessExitStatus left, ProcessExitStatus right) => !left.Equals(right);

        /// <inheritdoc />
        public override int GetHashCode() => HashCode.Combine(ExitCode, Signal, Canceled);
    }
}
