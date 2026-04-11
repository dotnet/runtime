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
        /// Gets the process ID.
        /// </summary>
        public int ProcessId
        {
            get
            {
                Validate();

                if (field == -1)
                {
                    field = GetProcessIdCore();
                }

                return field;
            }
            private set;
        } = -1;

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

            using SafeFileHandle? nullDeviceHandle = startInfo.StartDetached
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
        /// Opens an existing process by its process ID.
        /// </summary>
        /// <param name="processId">The process ID of the process to open.</param>
        /// <returns>A <see cref="SafeProcessHandle"/> representing the opened process.</returns>
        /// <exception cref="ArgumentException">The process with the specified ID was not found.</exception>
        [UnsupportedOSPlatform("ios")]
        [UnsupportedOSPlatform("tvos")]
        [SupportedOSPlatform("maccatalyst")]
        public static SafeProcessHandle Open(int processId)
        {
            return OpenCore(processId);
        }

        /// <summary>
        /// Waits indefinitely for the process to exit and returns its exit status.
        /// </summary>
        /// <returns>The <see cref="ProcessExitStatus"/> of the process.</returns>
        /// <exception cref="InvalidOperationException">The handle is invalid.</exception>
        [UnsupportedOSPlatform("ios")]
        [UnsupportedOSPlatform("tvos")]
        [SupportedOSPlatform("maccatalyst")]
        public ProcessExitStatus WaitForExit()
        {
            Validate();
            return WaitForExitCore(Timeout.InfiniteTimeSpan)!;
        }

        /// <summary>
        /// Waits for the process to exit within the specified timeout.
        /// </summary>
        /// <param name="timeout">The maximum amount of time to wait. Use <see cref="Timeout.InfiniteTimeSpan"/> to wait indefinitely.</param>
        /// <param name="exitStatus">When this method returns <see langword="true"/>, contains the exit status of the process.</param>
        /// <returns><see langword="true"/> if the process exited within the timeout; <see langword="false"/> otherwise.</returns>
        /// <exception cref="InvalidOperationException">The handle is invalid.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="timeout"/> is negative and not <see cref="Timeout.InfiniteTimeSpan"/>.</exception>
        [UnsupportedOSPlatform("ios")]
        [UnsupportedOSPlatform("tvos")]
        [SupportedOSPlatform("maccatalyst")]
        public bool TryWaitForExit(TimeSpan timeout, [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out ProcessExitStatus? exitStatus)
        {
            Validate();

            long totalMilliseconds = (long)timeout.TotalMilliseconds;
            if (totalMilliseconds < -1 || totalMilliseconds > int.MaxValue)
            {
                throw new ArgumentOutOfRangeException(nameof(timeout));
            }

            exitStatus = WaitForExitCore(timeout);
            return exitStatus is not null;
        }

        /// <summary>
        /// Waits for the process to exit within the specified timeout. If the process does not exit within the timeout, it is killed.
        /// </summary>
        /// <param name="timeout">The maximum amount of time to wait before killing the process.</param>
        /// <returns>The <see cref="ProcessExitStatus"/> of the process.</returns>
        /// <exception cref="InvalidOperationException">The handle is invalid.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="timeout"/> is negative and not <see cref="Timeout.InfiniteTimeSpan"/>.</exception>
        [UnsupportedOSPlatform("ios")]
        [UnsupportedOSPlatform("tvos")]
        [SupportedOSPlatform("maccatalyst")]
        public ProcessExitStatus WaitForExitOrKillOnTimeout(TimeSpan timeout)
        {
            Validate();

            long totalMilliseconds = (long)timeout.TotalMilliseconds;
            if (totalMilliseconds < -1 || totalMilliseconds > int.MaxValue)
            {
                throw new ArgumentOutOfRangeException(nameof(timeout));
            }

            ProcessExitStatus? exitStatus = WaitForExitCore(timeout);
            if (exitStatus is not null)
            {
                return exitStatus;
            }

            Kill();

            return WaitForExitCore(Timeout.InfiniteTimeSpan)!;
        }

        /// <summary>
        /// Asynchronously waits for the process to exit.
        /// </summary>
        /// <param name="cancellationToken">A token to cancel the wait.</param>
        /// <returns>A task that completes with the <see cref="ProcessExitStatus"/> of the process.</returns>
        /// <exception cref="InvalidOperationException">The handle is invalid.</exception>
        /// <exception cref="OperationCanceledException">The <paramref name="cancellationToken"/> was canceled.</exception>
        [UnsupportedOSPlatform("ios")]
        [UnsupportedOSPlatform("tvos")]
        [SupportedOSPlatform("maccatalyst")]
        public Task<ProcessExitStatus> WaitForExitAsync(CancellationToken cancellationToken = default)
        {
            Validate();
            return WaitForExitAsyncCore(cancellationToken);
        }

        /// <summary>
        /// Asynchronously waits for the process to exit. If the <paramref name="cancellationToken"/> is canceled, the process is killed.
        /// </summary>
        /// <param name="cancellationToken">A token that, when canceled, causes the process to be killed.</param>
        /// <returns>A task that completes with the <see cref="ProcessExitStatus"/> of the process.</returns>
        /// <exception cref="InvalidOperationException">The handle is invalid.</exception>
        [UnsupportedOSPlatform("ios")]
        [UnsupportedOSPlatform("tvos")]
        [SupportedOSPlatform("maccatalyst")]
        public async Task<ProcessExitStatus> WaitForExitOrKillOnCancellationAsync(CancellationToken cancellationToken)
        {
            Validate();

            try
            {
                return await WaitForExitAsyncCore(cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                Kill();
                return await WaitForExitAsyncCore(CancellationToken.None).ConfigureAwait(false);
            }
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
