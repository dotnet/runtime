// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Runtime.Versioning;
using System.Threading;
using System.Threading.Tasks;
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

            ThrowIfUseShellExecute(startInfo, nameof(StartAndForget));

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
            => StartAndForget(CreateStartInfo(fileName, arguments));

        /// <summary>
        /// Starts the process described by <paramref name="startInfo"/>, waits for it to exit, and returns its exit status.
        /// </summary>
        /// <param name="startInfo">The <see cref="ProcessStartInfo"/> that contains the information used to start the process.</param>
        /// <param name="timeout">
        /// The maximum amount of time to wait for the process to exit.
        /// When <see langword="null"/>, waits indefinitely.
        /// If the process does not exit within the specified timeout, it is killed.
        /// </param>
        /// <returns>The exit status of the process.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="startInfo"/> is <see langword="null"/>.</exception>
        /// <exception cref="InvalidOperationException">
        /// <see cref="ProcessStartInfo.UseShellExecute"/> is set to <see langword="true"/>.
        /// </exception>
        [UnsupportedOSPlatform("ios")]
        [UnsupportedOSPlatform("tvos")]
        [SupportedOSPlatform("maccatalyst")]
        public static ProcessExitStatus Run(ProcessStartInfo startInfo, TimeSpan? timeout = default)
        {
            ArgumentNullException.ThrowIfNull(startInfo);

            ThrowIfUseShellExecute(startInfo, nameof(Run));

            using SafeProcessHandle processHandle = SafeProcessHandle.Start(startInfo);

            return timeout.HasValue
                ? processHandle.WaitForExitOrKillOnTimeout(timeout.Value)
                : processHandle.WaitForExit();
        }

        /// <summary>
        /// Starts a process with the specified file name and optional arguments, waits for it to exit, and returns its exit status.
        /// </summary>
        /// <param name="fileName">The name of the application or document to start.</param>
        /// <param name="arguments">
        /// The command-line arguments to pass to the process. Pass <see langword="null"/> or an empty list
        /// to start the process without additional arguments.
        /// </param>
        /// <param name="timeout">
        /// The maximum amount of time to wait for the process to exit.
        /// When <see langword="null"/>, waits indefinitely.
        /// If the process does not exit within the specified timeout, it is killed.
        /// </param>
        /// <returns>The exit status of the process.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="fileName"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="fileName"/> is empty.</exception>
        [UnsupportedOSPlatform("ios")]
        [UnsupportedOSPlatform("tvos")]
        [SupportedOSPlatform("maccatalyst")]
        public static ProcessExitStatus Run(string fileName, IList<string>? arguments = null, TimeSpan? timeout = default)
            => Run(CreateStartInfo(fileName, arguments), timeout);

        /// <summary>
        /// Asynchronously starts the process described by <paramref name="startInfo"/>, waits for it to exit, and returns its exit status.
        /// </summary>
        /// <param name="startInfo">The <see cref="ProcessStartInfo"/> that contains the information used to start the process.</param>
        /// <param name="cancellationToken">
        /// A token to cancel the asynchronous operation.
        /// If the token is canceled, the process is killed.
        /// </param>
        /// <returns>A task that represents the asynchronous operation. The value of the task contains the exit status of the process.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="startInfo"/> is <see langword="null"/>.</exception>
        /// <exception cref="InvalidOperationException">
        /// <see cref="ProcessStartInfo.UseShellExecute"/> is set to <see langword="true"/>.
        /// </exception>
        [UnsupportedOSPlatform("ios")]
        [UnsupportedOSPlatform("tvos")]
        [SupportedOSPlatform("maccatalyst")]
        public static async Task<ProcessExitStatus> RunAsync(ProcessStartInfo startInfo, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(startInfo);

            ThrowIfUseShellExecute(startInfo, nameof(RunAsync));

            using SafeProcessHandle processHandle = SafeProcessHandle.Start(startInfo);

            return await processHandle.WaitForExitOrKillOnCancellationAsync(cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Asynchronously starts a process with the specified file name and optional arguments, waits for it to exit, and returns its exit status.
        /// </summary>
        /// <param name="fileName">The name of the application or document to start.</param>
        /// <param name="arguments">
        /// The command-line arguments to pass to the process. Pass <see langword="null"/> or an empty list
        /// to start the process without additional arguments.
        /// </param>
        /// <param name="cancellationToken">
        /// A token to cancel the asynchronous operation.
        /// If the token is canceled, the process is killed.
        /// </param>
        /// <returns>A task that represents the asynchronous operation. The value of the task contains the exit status of the process.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="fileName"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="fileName"/> is empty.</exception>
        [UnsupportedOSPlatform("ios")]
        [UnsupportedOSPlatform("tvos")]
        [SupportedOSPlatform("maccatalyst")]
        public static Task<ProcessExitStatus> RunAsync(string fileName, IList<string>? arguments = null, CancellationToken cancellationToken = default)
            => RunAsync(CreateStartInfo(fileName, arguments), cancellationToken);

        /// <summary>
        /// Starts the process described by <paramref name="startInfo"/>, captures its standard output and error,
        /// waits for it to exit, and returns the captured text and exit status.
        /// </summary>
        /// <param name="startInfo">The <see cref="ProcessStartInfo"/> that contains the information used to start the process.</param>
        /// <param name="timeout">
        /// The maximum amount of time to wait for the process to exit.
        /// When <see langword="null"/>, waits indefinitely.
        /// If the process does not exit within the specified timeout, it is killed.
        /// </param>
        /// <returns>The captured text output and exit status of the process.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="startInfo"/> is <see langword="null"/>.</exception>
        /// <exception cref="InvalidOperationException">
        /// <para><see cref="ProcessStartInfo.UseShellExecute"/> is set to <see langword="true"/>.</para>
        /// <para>-or-</para>
        /// <para><see cref="ProcessStartInfo.RedirectStandardOutput"/> or <see cref="ProcessStartInfo.RedirectStandardError"/> is not set to <see langword="true"/>.</para>
        /// </exception>
        [UnsupportedOSPlatform("ios")]
        [UnsupportedOSPlatform("tvos")]
        [SupportedOSPlatform("maccatalyst")]
        public static ProcessTextOutput RunAndCaptureText(ProcessStartInfo startInfo, TimeSpan? timeout = default)
        {
            ArgumentNullException.ThrowIfNull(startInfo);

            ThrowIfUseShellExecute(startInfo, nameof(RunAndCaptureText));
            ThrowIfNotRedirected(startInfo, nameof(RunAndCaptureText));

            long startTimestamp = Stopwatch.GetTimestamp();

            using Process process = Start(startInfo)!;

            string standardOutput, standardError;
            try
            {
                (standardOutput, standardError) = process.ReadAllText(timeout);
            }
            catch
            {
                try { process.Kill(); } catch { }
                throw;
            }

            ProcessExitStatus exitStatus;
            if (timeout.HasValue)
            {
                TimeSpan elapsed = Stopwatch.GetElapsedTime(startTimestamp);
                TimeSpan remaining = timeout.Value - elapsed;
                remaining = remaining >= TimeSpan.Zero ? remaining : TimeSpan.Zero;
                exitStatus = process.SafeHandle.WaitForExitOrKillOnTimeout(remaining);
            }
            else
            {
                exitStatus = process.SafeHandle.WaitForExit();
            }

            return new ProcessTextOutput(exitStatus, standardOutput, standardError, process.Id);
        }

        /// <summary>
        /// Starts a process with the specified file name and optional arguments, captures its standard output and error,
        /// waits for it to exit, and returns the captured text and exit status.
        /// </summary>
        /// <param name="fileName">The name of the application or document to start.</param>
        /// <param name="arguments">
        /// The command-line arguments to pass to the process. Pass <see langword="null"/> or an empty list
        /// to start the process without additional arguments.
        /// </param>
        /// <param name="timeout">
        /// The maximum amount of time to wait for the process to exit.
        /// When <see langword="null"/>, waits indefinitely.
        /// If the process does not exit within the specified timeout, it is killed.
        /// </param>
        /// <returns>The captured text output and exit status of the process.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="fileName"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="fileName"/> is empty.</exception>
        [UnsupportedOSPlatform("ios")]
        [UnsupportedOSPlatform("tvos")]
        [SupportedOSPlatform("maccatalyst")]
        public static ProcessTextOutput RunAndCaptureText(string fileName, IList<string>? arguments = null, TimeSpan? timeout = default)
            => RunAndCaptureText(CreateStartInfoForCapture(fileName, arguments), timeout);

        /// <summary>
        /// Asynchronously starts the process described by <paramref name="startInfo"/>, captures its standard output and error,
        /// waits for it to exit, and returns the captured text and exit status.
        /// </summary>
        /// <param name="startInfo">The <see cref="ProcessStartInfo"/> that contains the information used to start the process.</param>
        /// <param name="cancellationToken">
        /// A token to cancel the asynchronous operation.
        /// If the token is canceled, the process is killed.
        /// </param>
        /// <returns>A task that represents the asynchronous operation. The value of the task contains the captured text output and exit status of the process.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="startInfo"/> is <see langword="null"/>.</exception>
        /// <exception cref="InvalidOperationException">
        /// <para><see cref="ProcessStartInfo.UseShellExecute"/> is set to <see langword="true"/>.</para>
        /// <para>-or-</para>
        /// <para><see cref="ProcessStartInfo.RedirectStandardOutput"/> or <see cref="ProcessStartInfo.RedirectStandardError"/> is not set to <see langword="true"/>.</para>
        /// </exception>
        [UnsupportedOSPlatform("ios")]
        [UnsupportedOSPlatform("tvos")]
        [SupportedOSPlatform("maccatalyst")]
        public static async Task<ProcessTextOutput> RunAndCaptureTextAsync(ProcessStartInfo startInfo, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(startInfo);

            ThrowIfUseShellExecute(startInfo, nameof(RunAndCaptureTextAsync));
            ThrowIfNotRedirected(startInfo, nameof(RunAndCaptureTextAsync));

            using Process process = Start(startInfo)!;

            string standardOutput, standardError;
            try
            {
                (standardOutput, standardError) = await process.ReadAllTextAsync(cancellationToken).ConfigureAwait(false);
            }
            catch
            {
                try { process.Kill(); } catch { }
                throw;
            }

            ProcessExitStatus exitStatus = await process.SafeHandle.WaitForExitOrKillOnCancellationAsync(cancellationToken).ConfigureAwait(false);

            return new ProcessTextOutput(exitStatus, standardOutput, standardError, process.Id);
        }

        /// <summary>
        /// Asynchronously starts a process with the specified file name and optional arguments, captures its standard output and error,
        /// waits for it to exit, and returns the captured text and exit status.
        /// </summary>
        /// <param name="fileName">The name of the application or document to start.</param>
        /// <param name="arguments">
        /// The command-line arguments to pass to the process. Pass <see langword="null"/> or an empty list
        /// to start the process without additional arguments.
        /// </param>
        /// <param name="cancellationToken">
        /// A token to cancel the asynchronous operation.
        /// If the token is canceled, the process is killed.
        /// </param>
        /// <returns>A task that represents the asynchronous operation. The value of the task contains the captured text output and exit status of the process.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="fileName"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="fileName"/> is empty.</exception>
        [UnsupportedOSPlatform("ios")]
        [UnsupportedOSPlatform("tvos")]
        [SupportedOSPlatform("maccatalyst")]
        public static Task<ProcessTextOutput> RunAndCaptureTextAsync(string fileName, IList<string>? arguments = null, CancellationToken cancellationToken = default)
            => RunAndCaptureTextAsync(CreateStartInfoForCapture(fileName, arguments), cancellationToken);

        private static ProcessStartInfo CreateStartInfo(string fileName, IList<string>? arguments)
        {
            ArgumentException.ThrowIfNullOrEmpty(fileName);

            ProcessStartInfo startInfo = new(fileName);
            if (arguments is not null)
            {
                foreach (string argument in arguments)
                {
                    startInfo.ArgumentList.Add(argument);
                }
            }

            return startInfo;
        }

        private static ProcessStartInfo CreateStartInfoForCapture(string fileName, IList<string>? arguments)
        {
            ProcessStartInfo startInfo = CreateStartInfo(fileName, arguments);
            startInfo.RedirectStandardOutput = true;
            startInfo.RedirectStandardError = true;

            return startInfo;
        }

        private static void ThrowIfUseShellExecute(ProcessStartInfo startInfo, string methodName)
        {
            if (startInfo.UseShellExecute)
            {
                throw new InvalidOperationException(SR.Format(SR.UseShellExecuteNotSupportedForScenario, methodName));
            }
        }

        private static void ThrowIfNotRedirected(ProcessStartInfo startInfo, string methodName)
        {
            if (!startInfo.RedirectStandardOutput || !startInfo.RedirectStandardError)
            {
                throw new InvalidOperationException(SR.Format(SR.RedirectStandardOutputAndErrorRequired, methodName));
            }
        }
    }
}
