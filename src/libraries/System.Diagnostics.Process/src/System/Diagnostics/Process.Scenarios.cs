// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using System.Runtime.ExceptionServices;
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
            ValidateNonCapturingStartInfo(startInfo, nameof(StartAndForget));

            using SafeProcessHandle processHandle = SafeProcessHandle.Start(startInfo, fallbackToNull: true);
            return processHandle.ProcessId;
        }

        /// <summary>
        /// Starts a process with the specified file name and optional arguments, releases all associated resources,
        /// and returns the process ID.
        /// </summary>
        /// <param name="fileName">The name of the application or document to start.</param>
        /// <param name="arguments">
        /// The command-line arguments to pass to the process. Pass <see langword="null"/> or an empty sequence
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
        public static int StartAndForget(string fileName, IEnumerable<string>? arguments = null)
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
            ValidateNonCapturingStartInfo(startInfo, nameof(Run));

            using SafeProcessHandle processHandle = SafeProcessHandle.Start(startInfo);

            return timeout.HasValue
                ? processHandle.WaitForExitOrKillOnTimeout(timeout.Value)
                : processHandle.WaitForExit();
        }

        /// <inheritdoc cref="Run(ProcessStartInfo, TimeSpan?)"/>
        /// <param name="startInfo"><inheritdoc cref="Run(ProcessStartInfo, TimeSpan?)" path="/param[@name='startInfo']/node()"/></param>
        /// <param name="timeout">
        /// The maximum amount of time to wait for the process to exit, or <see cref="Timeout.InfiniteTimeSpan"/> to wait indefinitely.
        /// If the process does not exit within the specified timeout, it is killed.
        /// </param>
        /// <param name="entireProcessTree">
        /// <see langword="true"/> to kill the process and its descendants on timeout; otherwise,
        /// <see langword="false"/> to kill only the process.
        /// </param>
        /// <remarks>
        /// On Windows, when tree termination may be required, the process is started in a new job.
        /// Descendants that explicitly break away from the job are not terminated.
        /// Job creation or assignment failures are reported as exceptions.
        /// Successful completion does not terminate surviving descendants.
        /// </remarks>
        [UnsupportedOSPlatform("ios")]
        [UnsupportedOSPlatform("tvos")]
        [SupportedOSPlatform("maccatalyst")]
        public static ProcessExitStatus Run(ProcessStartInfo startInfo, TimeSpan timeout, bool entireProcessTree)
        {
            if (!entireProcessTree || timeout == Timeout.InfiniteTimeSpan)
            {
                return Run(startInfo, timeout);
            }

            ValidateNonCapturingStartInfo(startInfo, nameof(Run));

            return RunWithEntireProcessTree(startInfo, timeout);
        }

        /// <summary>
        /// Starts a process with the specified file name and optional arguments, waits for it to exit, and returns its exit status.
        /// </summary>
        /// <param name="fileName">The name of the application or document to start.</param>
        /// <param name="arguments">
        /// The command-line arguments to pass to the process. Pass <see langword="null"/> or an empty sequence
        /// to start the process without additional arguments.
        /// </param>
        /// <param name="silent">
        /// When <see langword="true"/>, redirects standard input, output, and error to the null device,
        /// ensuring the process produces no visible console output.
        /// When <see langword="false"/>, the process inherits standard handles from the parent.
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
        public static ProcessExitStatus Run(string fileName, IEnumerable<string>? arguments = null, bool silent = false, TimeSpan? timeout = default)
        {
            ProcessStartInfo startInfo = CreateStartInfo(fileName, arguments);

            using SafeFileHandle? nullHandle = silent ? File.OpenNullHandle() : null;
            startInfo.StandardInputHandle = nullHandle;
            startInfo.StandardOutputHandle = nullHandle;
            startInfo.StandardErrorHandle = nullHandle;

            return Run(startInfo, timeout);
        }

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
            ValidateNonCapturingStartInfo(startInfo, nameof(RunAsync));

            using SafeProcessHandle processHandle = SafeProcessHandle.Start(startInfo);

            return await processHandle.WaitForExitOrKillOnCancellationAsync(cancellationToken).ConfigureAwait(false);
        }

        /// <inheritdoc cref="RunAsync(ProcessStartInfo, CancellationToken)"/>
        /// <param name="startInfo"><inheritdoc cref="RunAsync(ProcessStartInfo, CancellationToken)" path="/param[@name='startInfo']/node()"/></param>
        /// <param name="entireProcessTree">
        /// <see langword="true"/> to kill the process and its descendants on cancellation; otherwise,
        /// <see langword="false"/> to kill only the process.
        /// </param>
        /// <param name="cancellationToken"><inheritdoc cref="RunAsync(ProcessStartInfo, CancellationToken)" path="/param[@name='cancellationToken']/node()"/></param>
        /// <remarks><inheritdoc cref="Run(ProcessStartInfo, TimeSpan, bool)" path="/remarks/node()"/></remarks>
        [UnsupportedOSPlatform("ios")]
        [UnsupportedOSPlatform("tvos")]
        [SupportedOSPlatform("maccatalyst")]
        public static async Task<ProcessExitStatus> RunAsync(ProcessStartInfo startInfo, bool entireProcessTree, CancellationToken cancellationToken)
        {
            if (!entireProcessTree || !cancellationToken.CanBeCanceled)
            {
                return await RunAsync(startInfo, cancellationToken).ConfigureAwait(false);
            }

            ValidateNonCapturingStartInfo(startInfo, nameof(RunAsync));

            return await RunWithEntireProcessTreeAsync(startInfo, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Asynchronously starts a process with the specified file name and optional arguments, waits for it to exit, and returns its exit status.
        /// </summary>
        /// <param name="fileName">The name of the application or document to start.</param>
        /// <param name="arguments">
        /// The command-line arguments to pass to the process. Pass <see langword="null"/> or an empty sequence
        /// to start the process without additional arguments.
        /// </param>
        /// <param name="silent">
        /// When <see langword="true"/>, redirects standard input, output, and error to the null device,
        /// ensuring the process produces no visible console output.
        /// When <see langword="false"/>, the process inherits standard handles from the parent.
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
        public static async Task<ProcessExitStatus> RunAsync(string fileName, IEnumerable<string>? arguments = null, bool silent = false, CancellationToken cancellationToken = default)
        {
            ProcessStartInfo startInfo = CreateStartInfo(fileName, arguments);

            using SafeFileHandle? nullHandle = silent ? File.OpenNullHandle() : null;
            startInfo.StandardInputHandle = nullHandle;
            startInfo.StandardOutputHandle = nullHandle;
            startInfo.StandardErrorHandle = nullHandle;

            return await RunAsync(startInfo, cancellationToken).ConfigureAwait(false);
        }

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
            ValidateCaptureStartInfo(startInfo, nameof(RunAndCaptureText));

            long startTimestamp = Stopwatch.GetTimestamp();
            using Process process = Start(startInfo)!;

            return RunAndCaptureTextCore(process, timeout, startTimestamp, killOnFailure: null);
        }

        /// <inheritdoc cref="RunAndCaptureText(ProcessStartInfo, TimeSpan?)"/>
        /// <param name="startInfo"><inheritdoc cref="RunAndCaptureText(ProcessStartInfo, TimeSpan?)" path="/param[@name='startInfo']/node()"/></param>
        /// <param name="timeout">
        /// The maximum amount of time to wait for the process to exit, or <see cref="Timeout.InfiniteTimeSpan"/> to wait indefinitely.
        /// If the process does not exit within the specified timeout, it is killed.
        /// </param>
        /// <param name="entireProcessTree">
        /// <see langword="true"/> to kill the process and its descendants on timeout or output-capture failure; otherwise,
        /// <see langword="false"/> to kill only the process.
        /// </param>
        /// <remarks><inheritdoc cref="Run(ProcessStartInfo, TimeSpan, bool)" path="/remarks/node()"/></remarks>
        [UnsupportedOSPlatform("ios")]
        [UnsupportedOSPlatform("tvos")]
        [SupportedOSPlatform("maccatalyst")]
        public static ProcessTextOutput RunAndCaptureText(ProcessStartInfo startInfo, TimeSpan timeout, bool entireProcessTree)
        {
            TimeSpan? captureTimeout = timeout == Timeout.InfiniteTimeSpan ? null : timeout;
            if (!entireProcessTree)
            {
                return RunAndCaptureText(startInfo, captureTimeout);
            }

            ValidateCaptureStartInfo(startInfo, nameof(RunAndCaptureText));

            long startTimestamp = Stopwatch.GetTimestamp();
            return RunAndCaptureTextWithEntireProcessTree(startInfo, captureTimeout, startTimestamp);
        }

        [UnsupportedOSPlatform("ios")]
        [UnsupportedOSPlatform("tvos")]
        [SupportedOSPlatform("maccatalyst")]
        private static ProcessTextOutput RunAndCaptureTextCore(Process process, TimeSpan? timeout, long startTimestamp, Action? killOnFailure)
        {
            string standardOutput, standardError;
            try
            {
                (standardOutput, standardError) = process.ReadAllText(timeout);
            }
            catch
            {
                // Cleanup must not replace the original capture exception.
                try
                {
                    if (killOnFailure is not null)
                    {
                        killOnFailure();
                    }
                    else
                    {
                        process.Kill();
                    }
                }
                catch { }
                throw;
            }

            ProcessExitStatus exitStatus;
            if (timeout.HasValue)
            {
                TimeSpan elapsed = Stopwatch.GetElapsedTime(startTimestamp);
                TimeSpan remaining = timeout.Value - elapsed;
                remaining = remaining >= TimeSpan.Zero ? remaining : TimeSpan.Zero;
                exitStatus = process.SafeHandle.WaitForExitOrKillOnTimeout(remaining, killOnFailure);
            }
            else
            {
                exitStatus = process.WaitForExitStatus();
            }

            return new ProcessTextOutput(exitStatus, standardOutput, standardError, process.Id);
        }

        /// <summary>
        /// Starts a process with the specified file name and optional arguments, captures its standard output and error,
        /// waits for it to exit, and returns the captured text and exit status.
        /// </summary>
        /// <param name="fileName">The name of the application or document to start.</param>
        /// <param name="arguments">
        /// The command-line arguments to pass to the process. Pass <see langword="null"/> or an empty sequence
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
        public static ProcessTextOutput RunAndCaptureText(string fileName, IEnumerable<string>? arguments = null, TimeSpan? timeout = default)
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
            ValidateCaptureStartInfo(startInfo, nameof(RunAndCaptureTextAsync));

            using Process process = Start(startInfo)!;

            return await RunAndCaptureTextAsyncCore(process, cancellationToken, killOnFailure: null).ConfigureAwait(false);
        }

        /// <inheritdoc cref="RunAndCaptureTextAsync(ProcessStartInfo, CancellationToken)"/>
        /// <param name="startInfo"><inheritdoc cref="RunAndCaptureTextAsync(ProcessStartInfo, CancellationToken)" path="/param[@name='startInfo']/node()"/></param>
        /// <param name="entireProcessTree">
        /// <see langword="true"/> to kill the process and its descendants on cancellation or output-capture failure; otherwise,
        /// <see langword="false"/> to kill only the process.
        /// </param>
        /// <param name="cancellationToken"><inheritdoc cref="RunAndCaptureTextAsync(ProcessStartInfo, CancellationToken)" path="/param[@name='cancellationToken']/node()"/></param>
        /// <remarks><inheritdoc cref="Run(ProcessStartInfo, TimeSpan, bool)" path="/remarks/node()"/></remarks>
        [UnsupportedOSPlatform("ios")]
        [UnsupportedOSPlatform("tvos")]
        [SupportedOSPlatform("maccatalyst")]
        public static async Task<ProcessTextOutput> RunAndCaptureTextAsync(ProcessStartInfo startInfo, bool entireProcessTree, CancellationToken cancellationToken)
        {
            if (!entireProcessTree)
            {
                return await RunAndCaptureTextAsync(startInfo, cancellationToken).ConfigureAwait(false);
            }

            ValidateCaptureStartInfo(startInfo, nameof(RunAndCaptureTextAsync));

            return await RunAndCaptureTextWithEntireProcessTreeAsync(startInfo, cancellationToken).ConfigureAwait(false);
        }

        [UnsupportedOSPlatform("ios")]
        [UnsupportedOSPlatform("tvos")]
        [SupportedOSPlatform("maccatalyst")]
        private static async Task<ProcessTextOutput> RunAndCaptureTextAsyncCore(Process process, CancellationToken cancellationToken, Action? killOnFailure)
        {
            string standardOutput, standardError;
            try
            {
                (standardOutput, standardError) = await process.ReadAllTextAsync(cancellationToken).ConfigureAwait(false);
            }
            catch
            {
                // Cleanup must not replace the original capture exception.
                try
                {
                    if (killOnFailure is not null)
                    {
                        killOnFailure();
                    }
                    else
                    {
                        process.Kill();
                    }
                }
                catch { }
                throw;
            }

            ProcessExitStatus exitStatus = await process.SafeHandle.WaitForExitOrKillOnCancellationAsync(cancellationToken, killOnFailure).ConfigureAwait(false);

            return new ProcessTextOutput(exitStatus, standardOutput, standardError, process.Id);
        }

        /// <summary>
        /// Asynchronously starts a process with the specified file name and optional arguments, captures its standard output and error,
        /// waits for it to exit, and returns the captured text and exit status.
        /// </summary>
        /// <param name="fileName">The name of the application or document to start.</param>
        /// <param name="arguments">
        /// The command-line arguments to pass to the process. Pass <see langword="null"/> or an empty sequence
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
        public static Task<ProcessTextOutput> RunAndCaptureTextAsync(string fileName, IEnumerable<string>? arguments = null, CancellationToken cancellationToken = default)
            => RunAndCaptureTextAsync(CreateStartInfoForCapture(fileName, arguments), cancellationToken);

        private static ProcessStartInfo CreateStartInfo(string fileName, IEnumerable<string>? arguments)
        {
            ArgumentException.ThrowIfNullOrEmpty(fileName);

            return arguments is not null ? new ProcessStartInfo(fileName, arguments) : new ProcessStartInfo(fileName);
        }

        private static ProcessStartInfo CreateStartInfoForCapture(string fileName, IEnumerable<string>? arguments)
        {
            ProcessStartInfo startInfo = CreateStartInfo(fileName, arguments);
            startInfo.RedirectStandardOutput = true;
            startInfo.RedirectStandardError = true;

            return startInfo;
        }

        private static void ValidateNonCapturingStartInfo(ProcessStartInfo startInfo, string methodName)
        {
            ThrowIfUseShellExecute(startInfo, methodName);
            startInfo.ThrowIfInvalid(out bool anyRedirection, out _);
            if (anyRedirection)
            {
                throw new InvalidOperationException(SR.CantSetRedirectForSafeProcessHandleStart);
            }
        }

        private static void ValidateCaptureStartInfo(ProcessStartInfo startInfo, string methodName)
        {
            ThrowIfUseShellExecute(startInfo, methodName);
            ThrowIfNotRedirected(startInfo, methodName);
        }

        private static void ThrowIfUseShellExecute(ProcessStartInfo startInfo, string methodName)
        {
            ArgumentNullException.ThrowIfNull(startInfo);

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
