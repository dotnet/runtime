// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*============================================================
**
** Class:  SafeProcessHandle
**
** A wrapper for a process handle
**
**
===========================================================*/

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Versioning;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Win32.SafeHandles
{
    public sealed partial class SafeProcessHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        internal static readonly SafeProcessHandle InvalidHandle = new SafeProcessHandle();

        // Allows for StartWithShellExecute (and its dependencies) to be trimmed when UseShellExecute is not being used.
        // s_startWithShellExecute is defined in platform-specific partial files with OS-appropriate delegate signatures.
        internal static void EnsureShellExecuteFunc() =>
            s_startWithShellExecute ??= StartWithShellExecute;

        /// <summary>
        /// Creates a <see cref="T:Microsoft.Win32.SafeHandles.SafeHandle" />.
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
        /// Starts a process using the specified <see cref="ProcessStartInfo"/>.
        /// </summary>
        /// <param name="startInfo">The process start information.</param>
        /// <returns>A <see cref="SafeProcessHandle"/> representing the started process.</returns>
        /// <remarks>
        /// On Windows, when <see cref="ProcessStartInfo.UseShellExecute"/> is <see langword="true"/>,
        /// the process is started using ShellExecuteEx. In some cases, such as when execution
        /// is satisfied through a DDE conversation, the returned handle will be invalid.
        /// </remarks>
        [UnsupportedOSPlatform("ios")]
        [UnsupportedOSPlatform("tvos")]
        [SupportedOSPlatform("maccatalyst")]
        public static SafeProcessHandle Start(ProcessStartInfo startInfo)
        {
            ArgumentNullException.ThrowIfNull(startInfo);

            return Start(startInfo, fallbackToNull: startInfo.StartDetached);
        }

        [UnsupportedOSPlatform("ios")]
        [UnsupportedOSPlatform("tvos")]
        [SupportedOSPlatform("maccatalyst")]
        internal static SafeProcessHandle Start(ProcessStartInfo startInfo, bool fallbackToNull)
        {
            startInfo.ThrowIfInvalid(out bool anyRedirection, out SafeHandle[]? inheritedHandles);

            if (anyRedirection)
            {
                // Process has .StandardInput, .StandardOutput, or .StandardError APIs that can express
                // redirection of streams, but SafeProcessHandle doesn't.
                // The caller can provide handles via the StandardInputHandle, StandardOutputHandle,
                // and StandardErrorHandle properties.
                throw new InvalidOperationException(SR.CantSetRedirectForSafeProcessHandleStart);
            }

            if (!ProcessUtils.PlatformSupportsProcessStartAndKill)
            {
                throw new PlatformNotSupportedException();
            }

            SerializationGuard.ThrowIfDeserializationInProgress("AllowProcessCreation", ref ProcessUtils.s_cachedSerializationSwitch);

            SafeFileHandle? childInputHandle = startInfo.StandardInputHandle;
            SafeFileHandle? childOutputHandle = startInfo.StandardOutputHandle;
            SafeFileHandle? childErrorHandle = startInfo.StandardErrorHandle;

            using SafeFileHandle? nullDeviceHandle = fallbackToNull
                && (childInputHandle is null || childOutputHandle is null || childErrorHandle is null)
                ? File.OpenNullHandle()
                : null;

            if (!startInfo.UseShellExecute)
            {
                childInputHandle ??= nullDeviceHandle ?? (ProcessUtils.PlatformSupportsConsole ? Console.OpenStandardInputHandle() : null);
                childOutputHandle ??= nullDeviceHandle ?? (ProcessUtils.PlatformSupportsConsole ? Console.OpenStandardOutputHandle() : null);
                childErrorHandle ??= nullDeviceHandle ?? (ProcessUtils.PlatformSupportsConsole ? Console.OpenStandardErrorHandle() : null);

                ProcessStartInfo.ValidateInheritedHandles(childInputHandle, childOutputHandle, childErrorHandle, inheritedHandles);
            }

            return StartCore(startInfo, childInputHandle, childOutputHandle, childErrorHandle, inheritedHandles);
        }

        /// <summary>
        /// Sends a request to the OS to terminate the process.
        /// </summary>
        /// <remarks>
        /// This method does not throw if the process has already exited.
        /// On Windows, the handle must have <c>PROCESS_TERMINATE</c> access.
        /// </remarks>
        /// <exception cref="InvalidOperationException">The handle is invalid.</exception>
        /// <exception cref="Win32Exception">The process could not be terminated.</exception>
        [UnsupportedOSPlatform("ios")]
        [UnsupportedOSPlatform("tvos")]
        [SupportedOSPlatform("maccatalyst")]
        public void Kill()
        {
            Validate();
            SignalCore(PosixSignal.SIGKILL);
        }

        /// <summary>
        /// Sends a signal to the process.
        /// </summary>
        /// <param name="signal">The signal to send.</param>
        /// <returns>
        /// <see langword="true"/> if the signal was sent successfully;
        /// <see langword="false"/> if the process has already exited (or never existed) and the signal was not delivered.
        /// </returns>
        /// <remarks>
        /// On Windows, only <see cref="PosixSignal.SIGKILL"/> is supported and is mapped to <see cref="Kill"/>.
        /// On Windows, the handle must have <c>PROCESS_TERMINATE</c> access.
        /// </remarks>
        /// <exception cref="InvalidOperationException">The handle is invalid.</exception>
        /// <exception cref="PlatformNotSupportedException">The specified signal is not supported on this platform.</exception>
        /// <exception cref="Win32Exception">The signal could not be sent.</exception>
        [UnsupportedOSPlatform("ios")]
        [UnsupportedOSPlatform("tvos")]
        [SupportedOSPlatform("maccatalyst")]
        public bool Signal(PosixSignal signal)
        {
            Validate();
            return SignalCore(signal);
        }

        /// <summary>
        /// Waits indefinitely for the process to exit.
        /// </summary>
        /// <returns>The exit status of the process.</returns>
        /// <remarks>
        /// On Unix, it's impossible to obtain the exit status of a non-child process.
        /// </remarks>
        /// <exception cref="InvalidOperationException">The handle is invalid.</exception>
        /// <exception cref="PlatformNotSupportedException">On Unix, the process is not a child process.</exception>
        public ProcessExitStatus WaitForExit()
        {
            Validate();

            return WaitForExitCore();
        }

        /// <summary>
        /// Waits for the process to exit within the specified timeout.
        /// </summary>
        /// <param name="timeout">The maximum time to wait for the process to exit.</param>
        /// <param name="exitStatus">When this method returns <see langword="true"/>, contains the exit status of the process.</param>
        /// <returns><see langword="true"/> if the process exited before the timeout; otherwise, <see langword="false"/>.</returns>
        /// <remarks>
        /// On Unix, it's impossible to obtain the exit status of a non-child process.
        /// </remarks>
        /// <exception cref="InvalidOperationException">The handle is invalid.</exception>
        /// <exception cref="PlatformNotSupportedException">On Unix, the process is not a child process.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="timeout"/> is negative and not equal to <see cref="Timeout.InfiniteTimeSpan"/>,
        /// or is greater than <see cref="int.MaxValue"/> milliseconds.</exception>
        public bool TryWaitForExit(TimeSpan timeout, [NotNullWhen(true)] out ProcessExitStatus? exitStatus)
        {
            Validate();

            return TryWaitForExitCore(ProcessUtils.ToTimeoutMilliseconds(timeout), out exitStatus);
        }

        /// <summary>
        /// Waits for the process to exit within the specified timeout.
        /// If the process does not exit before the timeout, it is killed and then waited for exit.
        /// </summary>
        /// <param name="timeout">The maximum time to wait for the process to exit before killing it.</param>
        /// <returns>The exit status of the process. If the process was killed due to timeout,
        /// <see cref="ProcessExitStatus.Canceled"/> will be <see langword="true"/>.</returns>
        /// <remarks>
        /// On Unix, it's impossible to obtain the exit status of a non-child process.
        /// </remarks>
        /// <exception cref="InvalidOperationException">The handle is invalid.</exception>
        /// <exception cref="PlatformNotSupportedException">On Unix, the process is not a child process.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="timeout"/> is negative and not equal to <see cref="Timeout.InfiniteTimeSpan"/>,
        /// or is greater than <see cref="int.MaxValue"/> milliseconds.</exception>
        [UnsupportedOSPlatform("ios")]
        [UnsupportedOSPlatform("tvos")]
        [SupportedOSPlatform("maccatalyst")]
        public ProcessExitStatus WaitForExitOrKillOnTimeout(TimeSpan timeout)
        {
            Validate();

            if (!ProcessUtils.PlatformSupportsProcessStartAndKill)
            {
                throw new PlatformNotSupportedException();
            }

            return WaitForExitOrKillOnTimeoutCore(ProcessUtils.ToTimeoutMilliseconds(timeout));
        }

        /// <summary>
        /// Waits asynchronously for the process to exit.
        /// </summary>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel the wait operation.</param>
        /// <returns>A task that represents the asynchronous wait operation. The task result contains the exit status of the process.</returns>
        /// <exception cref="InvalidOperationException">The handle is invalid.</exception>
        /// <exception cref="OperationCanceledException">The cancellation token was canceled.</exception>
        /// <remarks>
        /// <para>
        /// When the cancellation token is canceled, this method stops waiting and throws <see cref="OperationCanceledException"/>.
        /// The process is NOT killed and continues running. If you want to kill the process on cancellation,
        /// use <see cref="WaitForExitOrKillOnCancellationAsync"/> instead.
        /// </para>
        /// <para>On Unix, it's impossible to obtain the exit status of a non-child process.</para>
        /// </remarks>
        /// <exception cref="PlatformNotSupportedException">On Unix, the process is not a child process.</exception>
        public async Task<ProcessExitStatus> WaitForExitAsync(CancellationToken cancellationToken = default)
        {
            Validate();

            TaskCompletionSource<bool> tcs = new(TaskCreationOptions.RunContinuationsAsynchronously);
            RegisteredWaitHandle? registeredWaitHandle = null;
            CancellationTokenRegistration ctr = default;

            var exitedEvent = GetWaitHandle();

            try
            {
                registeredWaitHandle = ThreadPool.RegisterWaitForSingleObject(
                    exitedEvent,
                    static (state, timedOut) => ((TaskCompletionSource<bool>)state!).TrySetResult(true),
                    tcs,
                    Timeout.Infinite,
                    executeOnlyOnce: true);

                if (cancellationToken.CanBeCanceled)
                {
                    ctr = cancellationToken.UnsafeRegister(
                        static state =>
                        {
                            var (taskSource, token) = ((TaskCompletionSource<bool> taskSource, CancellationToken token))state!;
                            taskSource.TrySetCanceled(token);
                        },
                        (tcs, cancellationToken));
                }

                await tcs.Task.ConfigureAwait(false);
            }
            finally
            {
                ctr.Dispose();
                registeredWaitHandle?.Unregister(null);

                // On Unix, we don't own the ManualResetEvent.
                if (OperatingSystem.IsWindows())
                {
                    exitedEvent.Dispose();
                }
            }

            return GetExitStatus();
        }

        /// <summary>
        /// Waits asynchronously for the process to exit.
        /// When cancelled, kills the process and then waits for it to exit.
        /// </summary>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel the wait operation and kill the process.</param>
        /// <returns>A task that represents the asynchronous wait operation. The task result contains the exit status of the process.
        /// If the process was killed due to cancellation, <see cref="ProcessExitStatus.Canceled"/> will be <see langword="true"/>.</returns>
        /// <exception cref="InvalidOperationException">The handle is invalid.</exception>
        /// <remarks>
        /// <para>
        /// When the cancellation token is canceled, this method kills the process and waits for it to exit.
        /// The returned exit status will have the <see cref="ProcessExitStatus.Canceled"/> property set to <see langword="true"/> if the process was killed.
        /// If the cancellation token cannot be canceled (e.g., <see cref="CancellationToken.None"/>), this method behaves identically
        /// to <see cref="WaitForExitAsync"/> and will wait indefinitely for the process to exit.
        /// </para>
        /// <para>On Unix, it's impossible to obtain the exit status of a non-child process.</para>
        /// </remarks>
        /// <exception cref="PlatformNotSupportedException">On Unix, the process is not a child process.</exception>
        [UnsupportedOSPlatform("ios")]
        [UnsupportedOSPlatform("tvos")]
        [SupportedOSPlatform("maccatalyst")]
        public async Task<ProcessExitStatus> WaitForExitOrKillOnCancellationAsync(CancellationToken cancellationToken)
        {
            Validate();

            if (!ProcessUtils.PlatformSupportsProcessStartAndKill)
            {
                throw new PlatformNotSupportedException();
            }

            TaskCompletionSource<bool> tcs = new(TaskCreationOptions.RunContinuationsAsynchronously);
            RegisteredWaitHandle? registeredWaitHandle = null;
            CancellationTokenRegistration ctr = default;

            var exitedEvent = GetWaitHandle();

            try
            {
                registeredWaitHandle = ThreadPool.RegisterWaitForSingleObject(
                    exitedEvent,
                    static (state, timedOut) => ((TaskCompletionSource<bool>)state!).TrySetResult(true),
                    tcs,
                    Timeout.Infinite,
                    executeOnlyOnce: true);

                if (cancellationToken.CanBeCanceled)
                {
                    ctr = cancellationToken.UnsafeRegister(
                        static state =>
                        {
                            var (handle, tcs) = ((SafeProcessHandle, TaskCompletionSource<bool>))state!;
                            try
                            {
                                handle.Canceled = true;
                                handle.SignalCore(PosixSignal.SIGKILL);
                            }
                            catch (Exception ex)
                            {
                                tcs.TrySetException(ex);
                            }
                        },
                        (this, tcs));
                }

                await tcs.Task.ConfigureAwait(false);
            }
            finally
            {
                ctr.Dispose();
                registeredWaitHandle?.Unregister(null);

                // On Unix, we don't own the ManualResetEvent.
                if (OperatingSystem.IsWindows())
                {
                    exitedEvent.Dispose();
                }
            }

            return GetExitStatus();
        }

        private void Validate()
        {
            if (IsInvalid)
            {
                throw new InvalidOperationException(SR.InvalidProcessHandle);
            }
        }
    }
}
