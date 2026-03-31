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
using System.Runtime.Serialization;
using System.Runtime.Versioning;
using System.Runtime.InteropServices;

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
            startInfo.ThrowIfInvalid(out bool anyRedirection);

            if (anyRedirection)
            {
                // Process has .StandardInput, .StandardOutput, or .StandardError APIs that can express
                // redirection of streams, but SafeProcessHandle doesn't.
                // The caller can provide handles via the StandardInputHandle, StandardOutputHandle,
                // and StandardErrorHandle properties.
                throw new InvalidOperationException(SR.CantSetRedirectForSafeProcessHandleStart);
            }

            SerializationGuard.ThrowIfDeserializationInProgress("AllowProcessCreation", ref ProcessUtils.s_cachedSerializationSwitch);

            SafeFileHandle? childInputHandle = startInfo.StandardInputHandle;
            SafeFileHandle? childOutputHandle = startInfo.StandardOutputHandle;
            SafeFileHandle? childErrorHandle = startInfo.StandardErrorHandle;

            if (!startInfo.UseShellExecute)
            {
                if (childInputHandle is null && !OperatingSystem.IsAndroid())
                {
                    childInputHandle = Console.OpenStandardInputHandle();
                }

                if (childOutputHandle is null && !OperatingSystem.IsAndroid())
                {
                    childOutputHandle = Console.OpenStandardOutputHandle();
                }

                if (childErrorHandle is null && !OperatingSystem.IsAndroid())
                {
                    childErrorHandle = Console.OpenStandardErrorHandle();
                }
            }

            return StartCore(startInfo, childInputHandle, childOutputHandle, childErrorHandle);
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

        private void Validate()
        {
            if (IsInvalid)
            {
                throw new InvalidOperationException(SR.InvalidProcessHandle);
            }
        }
    }
}
