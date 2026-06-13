// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Win32.SafeHandles;
using System.Runtime.Versioning;

namespace System.Diagnostics
{
    /// <summary>
    /// Provides the prepared data required to start a process via a user-supplied callback.
    /// This ref struct is populated by the <see cref="Process.Start(ProcessStartInfo, Func{ProcessStartArguments, SafeProcessHandle})"/> method.
    /// </summary>
    public ref struct ProcessStartArguments
    {
        public ProcessStartArguments() { }

        /// <summary>
        /// Gets or sets a pointer to the resolved executable path encoded as null-terminated UTF-8.
        /// </summary>
        /// <value>
        /// A pointer to a null-terminated UTF-8 encoded string representing the resolved executable path.
        /// </value>
        /// <remarks>
        /// The memory pointed to by this property is only valid for the duration of the callback invocation.
        /// This property is writable to allow callback implementations to override the resolved path when needed.
        /// Do not cache or use this pointer after the callback returns.
        /// </remarks>
        [CLSCompliant(false)]
        [UnsupportedOSPlatform("windows")]
        public unsafe byte* ResolvedPath { get; set; }

        /// <summary>
        /// Gets or sets a pointer to the command-line arguments for the process.
        /// On Windows, this is a pointer to a null-terminated <see cref="char"/> string (the full command line including the executable).
        /// On Unix, this is a pointer to a null-terminated array of pointers to null-terminated UTF-8 byte strings (argv).
        /// </summary>
        /// <remarks>
        /// The memory pointed to by this property is only valid for the duration of the callback invocation.
        /// </remarks>
        [CLSCompliant(false)]
        public unsafe void* Arguments { get; set; }

        /// <summary>
        /// Gets or sets a pointer to the environment variables block for the new process.
        /// On Windows, this is a pointer to a null-terminated <see cref="char"/> string in the format used by CreateProcess
        /// (each variable is "name=value\0", terminated by an extra '\0').
        /// On Unix, this is a pointer to a null-terminated array of pointers to null-terminated UTF-8 byte strings ("name=value").
        /// When <see langword="null"/>, the new process inherits the current process's environment.
        /// </summary>
        /// <remarks>
        /// The memory pointed to by this property is only valid for the duration of the callback invocation.
        /// </remarks>
        [CLSCompliant(false)]
        public unsafe void* EnvironmentVariables { get; set; }

        /// <summary>
        /// Gets or sets the raw handle to use as the standard input for the new process.
        /// </summary>
        public nint StandardInput { get; set; }

        /// <summary>
        /// Gets or sets the raw handle to use as the standard output for the new process.
        /// </summary>
        public nint StandardOutput { get; set; }

        /// <summary>
        /// Gets or sets the raw handle to use as the standard error for the new process.
        /// </summary>
        public nint StandardError { get; set; }

        /// <summary>
        /// Gets or sets the original <see cref="ProcessStartInfo"/> provided by the user,
        /// allowing the callback to inspect any additional configuration.
        /// </summary>
        public ProcessStartInfo ProcessStartInfo { get; set; } = null!;
    }
}
