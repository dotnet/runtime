// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace System.Diagnostics
{
    /// <summary>
    /// Represents the exit status of a process.
    /// </summary>
    public readonly struct ProcessExitStatus : IEquatable<ProcessExitStatus>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ProcessExitStatus"/> structure.
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

        /// <summary>
        /// Indicates whether the current object is equal to another object of the same type.
        /// </summary>
        /// <param name="other">An object to compare with this object.</param>
        /// <returns>true if the current object is equal to the <paramref name="other"/> parameter; otherwise, false.</returns>
        public bool Equals(ProcessExitStatus other)
        {
            return ExitCode == other.ExitCode && Signal == other.Signal && Canceled == other.Canceled;
        }

        /// <summary>
        /// Indicates whether this instance and a specified object are equal.
        /// </summary>
        /// <param name="obj">The object to compare with the current instance.</param>
        /// <returns>true if <paramref name="obj"/> and this instance are the same type and represent the same value; otherwise, false.</returns>
        public override bool Equals([NotNullWhen(true)] object? obj)
        {
            return obj is ProcessExitStatus other && Equals(other);
        }

        /// <summary>
        /// Returns the hash code for this instance.
        /// </summary>
        /// <returns>A 32-bit signed integer that is the hash code for this instance.</returns>
        public override int GetHashCode()
        {
            return HashCode.Combine(ExitCode, Signal, Canceled);
        }

        /// <summary>
        /// Determines whether two specified instances of <see cref="ProcessExitStatus"/> are equal.
        /// </summary>
        /// <param name="left">The first instance to compare.</param>
        /// <param name="right">The second instance to compare.</param>
        /// <returns>true if <paramref name="left"/> and <paramref name="right"/> represent the same value; otherwise, false.</returns>
        public static bool operator ==(ProcessExitStatus left, ProcessExitStatus right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Determines whether two specified instances of <see cref="ProcessExitStatus"/> are not equal.
        /// </summary>
        /// <param name="left">The first instance to compare.</param>
        /// <param name="right">The second instance to compare.</param>
        /// <returns>true if <paramref name="left"/> and <paramref name="right"/> do not represent the same value; otherwise, false.</returns>
        public static bool operator !=(ProcessExitStatus left, ProcessExitStatus right)
        {
            return !left.Equals(right);
        }
    }
}
