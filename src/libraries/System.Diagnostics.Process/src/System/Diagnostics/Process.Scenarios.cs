// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Runtime.Versioning;
using Microsoft.Win32.SafeHandles;

namespace System.Diagnostics
{
    public partial class Process
    {
        /// <summary>
        /// Starts the process described by <paramref name="startInfo"/>, releases all associated resources,
        /// and returns the process ID.
        /// </summary>
        /// <param name="startInfo">The <see cref="ProcessStartInfo"/> that contains the information used to start the process.</param>
        /// <returns>The process ID of the started process.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="startInfo"/> is <see langword="null"/>.</exception>
        /// <exception cref="InvalidOperationException">
        /// <para>One or more of <see cref="ProcessStartInfo.RedirectStandardInput"/>,
        /// <see cref="ProcessStartInfo.RedirectStandardOutput"/>, or
        /// <see cref="ProcessStartInfo.RedirectStandardError"/> is set to <see langword="true"/>.
        /// Stream redirection is not supported in fire-and-forget scenarios because redirected streams
        /// must be drained to avoid deadlocks.</para>
        /// <para>-or-</para>
        /// <para><see cref="ProcessStartInfo.UseShellExecute"/> is set to <see langword="true"/>.
        /// Shell execution is not supported in fire-and-forget scenarios because on Windows it may not
        /// create a new process, making it impossible to return a valid process ID.</para>
        /// </exception>
        /// <remarks>
        /// <para>
        /// When a standard handle (<see cref="ProcessStartInfo.StandardInputHandle"/>,
        /// <see cref="ProcessStartInfo.StandardOutputHandle"/>, or <see cref="ProcessStartInfo.StandardErrorHandle"/>)
        /// is not provided, it is redirected to the null file by default.
        /// </para>
        /// <para>
        /// This method is designed for fire-and-forget scenarios where the caller wants to launch a process
        /// and does not need to interact with it further. It starts the process, releases all associated
        /// resources, and returns the process ID. The started process continues to run independently.
        /// </para>
        /// </remarks>
        [UnsupportedOSPlatform("ios")]
        [UnsupportedOSPlatform("tvos")]
        [SupportedOSPlatform("maccatalyst")]
        public static int StartAndForget(ProcessStartInfo startInfo)
        {
            ArgumentNullException.ThrowIfNull(startInfo);

            if (startInfo.UseShellExecute)
            {
                throw new InvalidOperationException(SR.StartAndForget_UseShellExecuteNotSupported);
            }

            using SafeProcessHandle processHandle = SafeProcessHandle.Start(startInfo, fallbackToNull: true);
            return processHandle.ProcessId;
        }

        /// <summary>
        /// Starts a process with the specified file name and optional arguments, releases all associated resources,
        /// and returns the process ID.
        /// </summary>
        /// <param name="fileName">The name of the application or document to start.</param>
        /// <param name="arguments">
        /// The command-line arguments to pass to the process. Pass <see langword="null"/> or an empty list
        /// to start the process without additional arguments.
        /// </param>
        /// <returns>The process ID of the started process.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="fileName"/> is <see langword="null"/>.</exception>
        /// <remarks>
        /// <para>
        /// This method is designed for fire-and-forget scenarios where the caller wants to launch a process
        /// and does not need to interact with it further. It starts the process, captures its process ID,
        /// releases all associated resources, and returns the process ID. The started process continues to
        /// run independently.
        /// </para>
        /// <para>
        /// Standard handles are redirected to the null file by default.
        /// </para>
        /// </remarks>
        [UnsupportedOSPlatform("ios")]
        [UnsupportedOSPlatform("tvos")]
        [SupportedOSPlatform("maccatalyst")]
        public static int StartAndForget(string fileName, IList<string>? arguments = null)
        {
            ArgumentNullException.ThrowIfNull(fileName);

            ProcessStartInfo startInfo = new(fileName);
            if (arguments is not null)
            {
                foreach (string argument in arguments)
                {
                    startInfo.ArgumentList.Add(argument);
                }
            }

            return StartAndForget(startInfo);
        }
    }
}
