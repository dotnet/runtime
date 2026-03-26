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
using System.Diagnostics;
using System.Runtime.Serialization;
using System.Runtime.Versioning;

namespace Microsoft.Win32.SafeHandles
{
    public sealed partial class SafeProcessHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        internal static readonly SafeProcessHandle InvalidHandle = new SafeProcessHandle();

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

        internal int ProcessId { get; private set; }

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
                // redirection of streams, but this API doesn't support it.
                // The caller should use Process.Start(ProcessStartInfo) instead.
                throw new InvalidOperationException("Redirection of streams is not supported by this API.");
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
    }
}
