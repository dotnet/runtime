// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.Versioning;

namespace System.Runtime.InteropServices
{
    /// <summary>
    /// Provides the prepared data required to start a process via a user-supplied callback.
    /// This ref struct is populated by the <see cref="Start(ProcessStartInfo, Func{WindowsProcessStartArguments, nint})"/> method.
    /// </summary>
    public readonly ref struct WindowsProcessStartArguments
    {
        internal unsafe WindowsProcessStartArguments(char* arguments, char* environmentVariables, nint standardInput, nint standardOutput, nint standardError, ProcessStartInfo processStartInfo)
        {
            Arguments = arguments;
            EnvironmentVariables = environmentVariables;
            StandardInput = standardInput;
            StandardOutput = standardOutput;
            StandardError = standardError;
            ProcessStartInfo = processStartInfo;
        }

        /// <summary>
        /// Gets a pointer to the command-line arguments for the process.
        /// This is a pointer to a null-terminated <see cref="char"/> string (the full command line including the executable).
        /// </summary>
        /// <remarks>
        /// The memory pointed to by this property is only valid for the duration of the callback invocation.
        /// </remarks>
        [CLSCompliant(false)]
        public unsafe char* Arguments { get; }

        /// <summary>
        /// Gets a pointer to the environment variables block for the new process.
        /// This is a pointer to a null-terminated <see cref="char"/> string in the format used by CreateProcess
        /// (each variable is "name=value\0", terminated by an extra '\0').
        /// </summary>
        /// <remarks>
        /// The memory pointed to by this property is only valid for the duration of the callback invocation.
        /// This block is UTF-16; if passed to CreateProcess, include CREATE_UNICODE_ENVIRONMENT.
        /// </remarks>
        [CLSCompliant(false)]
        public unsafe char* EnvironmentVariables { get; }

        /// <summary>
        /// Gets the raw handle to use as the standard input for the new process.
        /// </summary>
        public nint StandardInput { get; }

        /// <summary>
        /// Gets the raw handle to use as the standard output for the new process.
        /// </summary>
        public nint StandardOutput { get; }

        /// <summary>
        /// Gets the raw handle to use as the standard error for the new process.
        /// </summary>
        public nint StandardError { get; }

        /// <summary>
        /// Gets the original <see cref="ProcessStartInfo"/> provided by the user,
        /// allowing the callback to inspect any additional configuration.
        /// </summary>
        public ProcessStartInfo ProcessStartInfo { get; }

        /// <summary>
        /// Starts a new process by preparing all necessary arguments (standard handles, command line, environment)
        /// and then invoking the user-supplied <paramref name="callback"/> to perform the actual process creation system call.
        /// The callback receives a <see cref="WindowsProcessStartArguments"/> instance with the prepared data and must return an
        /// <see cref="nint"/> representing the handle of the created process.
        /// </summary>
        /// <param name="startInfo">The <see cref="ProcessStartInfo"/> that contains the information used to start the process.</param>
        /// <param name="callback">
        /// A function that receives the prepared <see cref="WindowsProcessStartArguments"/> and creates the process using any system call of the user's choice.
        /// The callback must return a valid process handle (<see cref="nint"/>) for the newly created process.
        /// The memory referenced by pointer properties in <see cref="WindowsProcessStartArguments"/> is only valid for the duration of the callback.
        /// The callback is invoked while an internal process-start lock is held; calling System.Diagnostics.Process APIs that start processes from within the callback may deadlock or throw.
        /// </param>
        /// <returns>A new <see cref="Process"/> instance associated with the started process.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="startInfo"/> or <paramref name="callback"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">The <see cref="nint"/> returned by the callback is invalid.</exception>
        /// <exception cref="InvalidOperationException"><see cref="ProcessStartInfo.UseShellExecute"/> is set to <see langword="true"/>.</exception>
        [SupportedOSPlatform("windows")]
        public static Process Start(ProcessStartInfo startInfo, Func<WindowsProcessStartArguments, nint> callback)
        {
#if !TARGET_WINDOWS
            throw new PlatformNotSupportedException();
#else
            ArgumentNullException.ThrowIfNull(startInfo);
            ArgumentNullException.ThrowIfNull(callback);

            if (startInfo.UseShellExecute)
            {
                throw new InvalidOperationException(SR.Format(SR.UseShellExecuteNotSupportedForScenario, nameof(Start)));
            }

            Process process = new();

            try
            {
                process.StartCoreWithCallback(startInfo, callback);
            }
            catch
            {
                process.Dispose();

                throw;
            }

            return process;
#endif
        }
    }
}
