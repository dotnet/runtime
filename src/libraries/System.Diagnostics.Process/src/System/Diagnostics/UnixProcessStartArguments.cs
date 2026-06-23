// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Win32.SafeHandles;
using System.Diagnostics;
using System.Runtime.Versioning;

namespace System.Runtime.InteropServices
{
    /// <summary>
    /// Provides the prepared data required to start a process via a user-supplied callback.
    /// This ref struct is populated by the <see cref="UnixProcessStartArguments.Start(ProcessStartInfo, Func{UnixProcessStartArguments, int})"/> method.
    /// </summary>
    [UnsupportedOSPlatform("windows")]
    public ref struct UnixProcessStartArguments
    {
        public UnixProcessStartArguments() { }

        /// <summary>
        /// Gets a pointer to the resolved executable path encoded as null-terminated UTF-8.
        /// </summary>
        /// <value>
        /// A pointer to a null-terminated UTF-8 encoded string representing the resolved executable path.
        /// </value>
        /// <remarks>
        /// The memory pointed to by this property is only valid for the duration of the callback invocation.
        /// Do not cache or use this pointer after the callback returns.
        /// </remarks>
        [CLSCompliant(false)]
        public unsafe byte* ResolvedPath { get; internal set; }

        /// <summary>
        /// Gets a pointer to the command-line arguments for the process.
        /// On Unix, this is a pointer to a null-terminated array of pointers to null-terminated UTF-8 byte strings (argv).
        /// </summary>
        /// <remarks>
        /// The memory pointed to by this property is only valid for the duration of the callback invocation.
        /// </remarks>
        [CLSCompliant(false)]
        public unsafe byte** Arguments { get; internal set; }

        /// <summary>
        /// Gets a pointer to the environment variables block for the new process.
        /// This is a pointer to a null-terminated array of pointers to null-terminated UTF-8 byte strings ("name=value").
        /// </summary>
        /// <remarks>
        /// The memory pointed to by this property is only valid for the duration of the callback invocation.
        /// </remarks>
        [CLSCompliant(false)]
        public unsafe byte** EnvironmentVariables { get; internal set; }

        /// <summary>
        /// Gets the raw handle to use as the standard input for the new process.
        /// </summary>
        public nint StandardInput { get; internal set; }

        /// <summary>
        /// Gets the raw handle to use as the standard output for the new process.
        /// </summary>
        public nint StandardOutput { get; internal set; }

        /// <summary>
        /// Gets the raw handle to use as the standard error for the new process.
        /// </summary>
        public nint StandardError { get; internal set; }

        /// <summary>
        /// Gets the original <see cref="ProcessStartInfo"/> provided by the user,
        /// allowing the callback to inspect any additional configuration.
        /// </summary>
        public ProcessStartInfo ProcessStartInfo { get; internal set; } = null!;


        /// <inheritdoc cref="Start{TState}(ProcessStartInfo, Func{UnixProcessStartArguments, TState, int}, TState)"/>
        [UnsupportedOSPlatform("ios")]
        [UnsupportedOSPlatform("tvos")]
        [SupportedOSPlatform("maccatalyst")]
        public static Process Start(ProcessStartInfo startInfo, Func<UnixProcessStartArguments, int> callback)
            => Start(startInfo, static (args, state) => state(args), state: callback);

        /// <summary>
        /// Starts a new process by preparing all necessary arguments (standard handles, command line, environment)
        /// and then invoking the user-supplied <paramref name="callback"/> to perform the actual process creation system call.
        /// The callback receives a <see cref="UnixProcessStartArguments"/> instance with the prepared data and must return a
        /// <see cref="SafeProcessHandle"/> representing the created process.
        /// </summary>
        /// <param name="startInfo">The <see cref="ProcessStartInfo"/> that contains the information used to start the process.</param>
        /// <param name="state">The user-defined state object that is passed to the callback.</param>
        /// <param name="callback">
        /// A function that receives the prepared <see cref="UnixProcessStartArguments"/> and creates the process using any system call of the user's choice.
        /// The callback must return a valid <see cref="SafeProcessHandle"/> for the newly created process.
        /// The memory referenced by pointer properties in <see cref="UnixProcessStartArguments"/> is only valid for the duration of the callback.
        /// The callback is invoked while an internal process-start lock is held; calling System.Diagnostics.Process APIs that start processes from within the callback may deadlock or throw.
        /// </param>
        /// <returns>A new <see cref="Process"/> instance associated with the started process.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="startInfo"/> or <paramref name="callback"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">The <see cref="SafeProcessHandle"/> returned by the callback is invalid.</exception>
        /// <exception cref="InvalidOperationException"><see cref="ProcessStartInfo.UseShellExecute"/> is set to <see langword="true"/>.</exception>
        [UnsupportedOSPlatform("ios")]
        [UnsupportedOSPlatform("tvos")]
        [SupportedOSPlatform("maccatalyst")]
        public static Process Start<TState>(ProcessStartInfo startInfo, Func<UnixProcessStartArguments, TState, int> callback, TState state)
        {
#if TARGET_WINDOWS
            throw new PlatformNotSupportedException();
#else
            ArgumentNullException.ThrowIfNull(startInfo);
            ArgumentNullException.ThrowIfNull(callback);

            if (startInfo.UseShellExecute)
            {
                throw new InvalidOperationException(SR.Format(SR.UseShellExecuteNotSupportedForScenario, nameof(Start)));
            }

            if (OperatingSystem.IsIOS() || OperatingSystem.IsTvOS())
            {
                throw new PlatformNotSupportedException();
            }

            Process process = new();

            try
            {
                process.StartCore(startInfo, callback, state);
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
