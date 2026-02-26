// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Win32.SafeHandles
{
    /// <summary>
    /// A wrapper for a process handle.
    /// </summary>
    public sealed partial class SafeProcessHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        internal static readonly SafeProcessHandle InvalidHandle = new SafeProcessHandle();

        /// <summary>
        /// Gets or sets the process ID.
        /// </summary>
        internal int ProcessId { get; set; }

        /// <summary>
        /// Creates a <see cref="T:Microsoft.Win32.SafeHandles.SafeProcessHandle" />.
        /// </summary>
        public SafeProcessHandle()
            : this(IntPtr.Zero)
        {
        }

        internal SafeProcessHandle(IntPtr handle)
            : this(handle, true)
        {
        }

        /// <summary>
        /// Creates a <see cref="T:Microsoft.Win32.SafeHandles.SafeHandle" /> around a process handle.
        /// </summary>
        /// <param name="existingHandle">Handle to wrap</param>
        /// <param name="ownsHandle">Whether to control the handle lifetime</param>
        public SafeProcessHandle(IntPtr existingHandle, bool ownsHandle)
            : base(ownsHandle)
        {
            SetHandle(existingHandle);
        }

        /// <summary>
        /// Opens an existing child process by its process ID.
        /// </summary>
        /// <param name="processId">The process ID of the process to open.</param>
        /// <returns>A <see cref="SafeProcessHandle"/> that represents the opened process.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="processId"/> is negative or zero.</exception>
        /// <exception cref="Win32Exception">Thrown when the process could not be opened.</exception>
        /// <remarks>
        /// On Windows, this method uses OpenProcess with PROCESS_QUERY_LIMITED_INFORMATION, SYNCHRONIZE, and PROCESS_TERMINATE permissions.
        /// On Linux with pidfd support, this method uses the pidfd_open syscall.
        /// On other Unix systems, this method uses kill(pid, 0) to verify the process exists and the caller has permission to signal it.
        /// </remarks>
        public static SafeProcessHandle Open(int processId)
        {
            ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(processId, 0);

            return OpenCore(processId);
        }

        /// <summary>
        /// Starts a new process.
        /// </summary>
        /// <param name="options">The process start options.</param>
        /// <param name="input">The handle to use for standard input, or <see langword="null"/> to provide no input.</param>
        /// <param name="output">The handle to use for standard output, or <see langword="null"/> to discard output.</param>
        /// <param name="error">The handle to use for standard error, or <see langword="null"/> to discard error.</param>
        /// <returns>A handle to the started process.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="options"/> is null.</exception>
        public static SafeProcessHandle Start(ProcessStartOptions options, SafeFileHandle? input, SafeFileHandle? output, SafeFileHandle? error)
        {
            return StartInternal(options, input, output, error, createSuspended: false);
        }

        /// <summary>
        /// Starts a new process in a suspended state.
        /// </summary>
        /// <param name="options">Process start options.</param>
        /// <param name="input">Standard input handle.</param>
        /// <param name="output">Standard output handle.</param>
        /// <param name="error">Standard error handle.</param>
        /// <returns>A handle to the suspended process. Call <see cref="Resume"/> to start execution.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="options"/> is null.</exception>
        internal static SafeProcessHandle StartSuspended(ProcessStartOptions options, SafeFileHandle? input, SafeFileHandle? output, SafeFileHandle? error)
        {
            return StartInternal(options, input, output, error, createSuspended: true);
        }

        private static SafeProcessHandle StartInternal(ProcessStartOptions options, SafeFileHandle? input, SafeFileHandle? output, SafeFileHandle? error, bool createSuspended)
        {
            ArgumentNullException.ThrowIfNull(options);

            SafeFileHandle? nullHandle = null;

            if (input is null || output is null || error is null)
            {
                nullHandle = File.OpenNullHandle();

                input ??= nullHandle;
                output ??= nullHandle;
                error ??= nullHandle;
            }

            try
            {
                return StartCore(options, input, output, error, createSuspended);
            }
            finally
            {
                nullHandle?.Dispose();
            }
        }

        /// <summary>
        /// Waits for the process to exit without a timeout.
        /// </summary>
        /// <returns>The exit status of the process.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the handle is invalid.</exception>
        public ProcessExitStatus WaitForExit()
        {
            Validate();

            return WaitForExitCore();
        }

        /// <summary>
        /// Waits for the process to exit within the specified timeout.
        /// </summary>
        /// <param name="timeout">The maximum time to wait for the process to exit.</param>
        /// <param name="exitStatus">When this method returns true, contains the exit status of the process.</param>
        /// <returns>true if the process exited before the timeout; otherwise, false.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the handle is invalid.</exception>
        public bool TryWaitForExit(TimeSpan timeout, [NotNullWhen(true)] out ProcessExitStatus? exitStatus)
        {
            Validate();

            return TryWaitForExitCore(GetTimeoutInMilliseconds(timeout), out exitStatus);
        }

        /// <summary>
        /// Waits for the process to exit within the specified timeout.
        /// If the process does not exit before the timeout, it is killed and then waited for exit.
        /// </summary>
        /// <param name="timeout">The maximum time to wait for the process to exit before killing it.</param>
        /// <returns>The exit status of the process. If the process was killed due to timeout, Canceled will be true.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the handle is invalid.</exception>
        public ProcessExitStatus WaitForExitOrKillOnTimeout(TimeSpan timeout)
        {
            Validate();

            return WaitForExitOrKillOnTimeoutCore(GetTimeoutInMilliseconds(timeout));
        }

        /// <summary>
        /// Waits asynchronously for the process to exit and reports the exit status.
        /// </summary>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel the wait operation.</param>
        /// <returns>A task that represents the asynchronous wait operation. The task result contains the exit status of the process.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the handle is invalid.</exception>
        /// <exception cref="OperationCanceledException">Thrown when the cancellation token is canceled.</exception>
        /// <remarks>
        /// When the cancellation token is canceled, this method stops waiting and throws <see cref="OperationCanceledException"/>.
        /// The process is NOT killed and continues running. If you want to kill the process on cancellation,
        /// use <see cref="WaitForExitOrKillOnCancellationAsync"/> instead.
        /// </remarks>
        public Task<ProcessExitStatus> WaitForExitAsync(CancellationToken cancellationToken = default)
        {
            Validate();

            return WaitForExitAsyncCore(cancellationToken);
        }

        /// <summary>
        /// Waits asynchronously for the process to exit and reports the exit status.
        /// When cancelled, kills the process and then waits for exit without timeout.
        /// </summary>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel the wait operation and kill the process.</param>
        /// <returns>A task that represents the asynchronous wait operation. The task result contains the exit status of the process.
        /// If the process was killed due to cancellation, the Canceled property will be true.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the handle is invalid.</exception>
        /// <remarks>
        /// When the cancellation token is canceled, this method kills the process and waits for it to exit.
        /// The returned exit status will have the <see cref="ProcessExitStatus.Canceled"/> property set to true if the process was killed.
        /// If the cancellation token cannot be canceled (e.g., <see cref="CancellationToken.None"/>), this method behaves identically
        /// to <see cref="WaitForExitAsync"/> and will wait indefinitely for the process to exit.
        /// </remarks>
        public Task<ProcessExitStatus> WaitForExitOrKillOnCancellationAsync(CancellationToken cancellationToken)
        {
            Validate();

            return WaitForExitOrKillOnCancellationAsyncCore(cancellationToken);
        }

        /// <summary>
        /// Terminates the process.
        /// </summary>
        /// <returns>
        /// <c>true</c> if the process was terminated; <c>false</c> if the process had already exited.
        /// </returns>
        /// <exception cref="InvalidOperationException">Thrown when the handle is invalid.</exception>
        /// <exception cref="Win32Exception">Thrown when the kill operation fails for reasons other than the process having already exited.</exception>
        public bool Kill()
        {
            Validate();

            return KillCore(throwOnError: true, entireProcessGroup: false);
        }

        /// <summary>
        /// Terminates the entire process group.
        /// </summary>
        /// <returns>
        /// <c>true</c> if the process group was terminated; <c>false</c> if the process had already exited.
        /// </returns>
        /// <exception cref="InvalidOperationException">Thrown when the handle is invalid.</exception>
        /// <exception cref="Win32Exception">Thrown when the kill operation fails for reasons other than the process having already exited.</exception>
        /// <remarks>
        /// On Unix, sends SIGKILL to all processes in the process group.
        /// On Windows, requires the process to have been started with <see cref="ProcessStartOptions.CreateNewProcessGroup"/>=true.
        /// Terminates all processes in the job object. If the process was not started with <see cref="ProcessStartOptions.CreateNewProcessGroup"/>=true,
        /// throws an <see cref="InvalidOperationException"/>.
        /// </remarks>
        internal bool KillProcessGroup()
        {
            Validate();

            return KillCore(throwOnError: true, entireProcessGroup: true);
        }

        /// <summary>
        /// Resumes a process that was created via <see cref="StartSuspended"/>.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown when the process was not started in a suspended state, or has already been resumed.</exception>
        /// <exception cref="Win32Exception">Thrown when the resume operation fails.</exception>
        /// <remarks>
        /// This method can only be called once. After the process has been resumed, calling this method again will throw an <see cref="InvalidOperationException"/>.
        /// This is not a general purpose process resume (like <c>NtResumeProcess</c>). It can only resume processes created via <see cref="StartSuspended"/>.
        /// </remarks>
        internal void Resume()
        {
            Validate();

            ResumeCore();
        }

        /// <summary>
        /// Sends a signal to the process.
        /// </summary>
        /// <param name="signal">The signal to send.</param>
        /// <exception cref="InvalidOperationException">Thrown when the handle is invalid.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when the signal value is not supported.</exception>
        /// <exception cref="ArgumentException">Thrown when the signal is not supported on the current platform.</exception>
        /// <exception cref="Win32Exception">Thrown when the signal operation fails.</exception>
        /// <remarks>
        /// On Windows, only SIGINT (mapped to CTRL_C_EVENT), SIGQUIT (mapped to CTRL_BREAK_EVENT), and SIGKILL are supported.
        /// The process must have been started with <see cref="ProcessStartOptions.CreateNewProcessGroup"/> set to true for signals to work properly.
        /// On Windows, signals are always sent to the entire process group, not just the single process.
        /// On Unix/Linux, all signals defined in PosixSignal are supported, and the signal is sent only to the specific process.
        /// </remarks>
        public void Signal(PosixSignal signal)
        {
            if (!Enum.IsDefined(signal))
            {
                throw new ArgumentOutOfRangeException(nameof(signal));
            }

            Validate();

            SendSignalCore(signal, entireProcessGroup: false);
        }

        /// <summary>
        /// Sends a signal to the entire process group.
        /// </summary>
        /// <param name="signal">The signal to send.</param>
        /// <exception cref="InvalidOperationException">Thrown when the handle is invalid.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when the signal value is not supported.</exception>
        /// <exception cref="Win32Exception">Thrown when the signal operation fails.</exception>
        /// <remarks>
        /// On Windows, only SIGINT (mapped to CTRL_C_EVENT), SIGQUIT (mapped to CTRL_BREAK_EVENT), and SIGKILL are supported.
        /// The process must have been started with <see cref="ProcessStartOptions.CreateNewProcessGroup"/> set to true for signals to work properly.
        /// On Windows, signals are always sent to the entire process group.
        /// On Unix/Linux, all signals defined in PosixSignal are supported, and the signal is sent to all processes in the process group.
        /// </remarks>
        public void SignalProcessGroup(PosixSignal signal)
        {
            if (!Enum.IsDefined(signal))
            {
                throw new ArgumentOutOfRangeException(nameof(signal));
            }

            Validate();

            SendSignalCore(signal, entireProcessGroup: true);
        }

        private void Validate()
        {
            if (IsInvalid)
            {
                throw new InvalidOperationException(SR.InvalidProcessHandle);
            }
        }

        internal static int GetTimeoutInMilliseconds(TimeSpan timeout)
        {
            long totalMilliseconds = (long)timeout.TotalMilliseconds;

            ArgumentOutOfRangeException.ThrowIfLessThan(totalMilliseconds, -1, nameof(timeout));
            ArgumentOutOfRangeException.ThrowIfGreaterThan(totalMilliseconds, int.MaxValue, nameof(timeout));

            return (int)totalMilliseconds;
        }
    }
}
