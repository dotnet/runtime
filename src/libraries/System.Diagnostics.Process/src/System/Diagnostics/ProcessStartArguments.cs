// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Win32.SafeHandles;

namespace System.Diagnostics
{
    /// <summary>
    /// Provides the prepared arguments for starting a process via a user-supplied callback.
    /// This class is populated by the <see cref="Process.Start(ProcessStartInfo, Func{ProcessStartArguments, SafeProcessHandle})"/> method
    /// with the resolved file name, command-line arguments, working directory, environment variables, and standard I/O handles.
    /// The user's callback receives this instance and is responsible for invoking the appropriate system call to create the process.
    /// </summary>
    public sealed class ProcessStartArguments
    {
        internal ProcessStartArguments() { }

        /// <summary>
        /// Gets the resolved file name of the process to start.
        /// On Windows, this is <see langword="null"/> (the file name is embedded in <see cref="Arguments"/>).
        /// On Unix, this is the resolved absolute path to the executable.
        /// </summary>
        public string? FileName { get; internal set; }

        /// <summary>
        /// Gets a pointer to the command-line arguments for the process.
        /// On Windows, this is a pointer to a null-terminated <see cref="char"/> string (the full command line including the executable).
        /// On Unix, this is a pointer to a null-terminated array of pointers to null-terminated UTF-8 byte strings (argv).
        /// </summary>
        /// <remarks>
        /// The memory pointed to by this property is only valid for the duration of the callback invocation.
        /// </remarks>
        [CLSCompliant(false)]
        public unsafe void* Arguments { get; internal set; }

        /// <summary>
        /// Gets the working directory for the new process, or <see langword="null"/> if the current directory should be used.
        /// </summary>
        public string? WorkingDirectory { get; internal set; }

        /// <summary>
        /// Gets a pointer to the environment variables block for the new process.
        /// On Windows, this is a pointer to a null-terminated <see cref="char"/> string in the format used by CreateProcess
        /// (each variable is "name=value\0", terminated by an extra '\0').
        /// On Unix, this is a pointer to a null-terminated array of pointers to null-terminated UTF-8 byte strings ("name=value").
        /// When <see langword="null"/>, the new process inherits the current process's environment.
        /// </summary>
        /// <remarks>
        /// The memory pointed to by this property is only valid for the duration of the callback invocation.
        /// </remarks>
        [CLSCompliant(false)]
        public unsafe void* EnvironmentVariables { get; internal set; }

        /// <summary>
        /// Gets the <see cref="SafeFileHandle"/> to use as the standard input for the new process.
        /// </summary>
        public SafeFileHandle StandardInput { get; internal set; } = null!;

        /// <summary>
        /// Gets the <see cref="SafeFileHandle"/> to use as the standard output for the new process.
        /// </summary>
        public SafeFileHandle StandardOutput { get; internal set; } = null!;

        /// <summary>
        /// Gets the <see cref="SafeFileHandle"/> to use as the standard error for the new process.
        /// </summary>
        public SafeFileHandle StandardError { get; internal set; } = null!;

        /// <summary>
        /// Gets the original <see cref="ProcessStartInfo"/> provided by the user,
        /// allowing the callback to inspect any additional configuration.
        /// </summary>
        public ProcessStartInfo ProcessStartInfo { get; internal set; } = null!;
    }
}
