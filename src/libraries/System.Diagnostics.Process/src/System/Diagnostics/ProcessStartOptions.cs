// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

namespace System.Diagnostics
{
    /// <summary>
    /// Specifies options for starting a new process.
    /// </summary>
    public sealed class ProcessStartOptions
    {
        private readonly string _fileName;
        private IList<string>? _arguments;
        private Dictionary<string, string?>? _environment;
        private IList<SafeHandle>? _inheritedHandles;

        /// <summary>
        /// Gets the application to start.
        /// </summary>
        public string FileName => _fileName;

        /// <summary>
        /// Gets or sets the command-line arguments to pass to the application.
        /// </summary>
        public IList<string> Arguments
        {
            get => _arguments ??= new List<string>();
            set
            {
                ArgumentNullException.ThrowIfNull(value);
                _arguments = value;
            }
        }

        /// <summary>
        /// Gets the environment variables that apply to this process and its child processes.
        /// </summary>
        /// <remarks>
        /// By default, the environment is a copy of the current process environment.
        /// </remarks>
        public IDictionary<string, string?> Environment
        {
            get
            {
                if (_environment == null)
                {
                    IDictionary envVars = System.Environment.GetEnvironmentVariables();

                    _environment = new Dictionary<string, string?>(
                        envVars.Count,
                        OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal);

                    // Manual use of IDictionaryEnumerator instead of foreach to avoid DictionaryEntry box allocations.
                    IDictionaryEnumerator e = envVars.GetEnumerator();
                    Debug.Assert(!(e is IDisposable), "Environment.GetEnvironmentVariables should not be IDisposable.");
                    while (e.MoveNext())
                    {
                        DictionaryEntry entry = e.Entry;
                        _environment.Add((string)entry.Key, (string?)entry.Value);
                    }
                }
                return _environment;
            }
        }

        /// <summary>
        /// Gets or sets the working directory for the process to be started.
        /// </summary>
        public string? WorkingDirectory { get; set; }

        /// <summary>
        /// Gets a list of handles that will be inherited by the child process.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Handles do not need to have inheritance enabled beforehand.
        /// They are also not duplicated, just added as-is to the child process
        /// so the exact same handle values can be used in the child process.
        /// </para>
        /// <para>
        /// On Windows, the implementation will automatically enable inheritance on any handle added to this list
        /// by modifying the handle's flags using SetHandleInformation.
        /// </para>
        /// <para>
        /// On Unix, the implementation will modify the copy of every handle in the child process
        /// by removing FD_CLOEXEC flag. It happens after the fork and before the exec, so it does not affect parent process.
        /// </para>
        /// </remarks>
        public IList<SafeHandle> InheritedHandles
        {
            get => _inheritedHandles ??= new List<SafeHandle>();
            set
            {
                ArgumentNullException.ThrowIfNull(value);
                _inheritedHandles = value;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the child process should be terminated when the parent process exits.
        /// </summary>
        public bool KillOnParentExit { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to create the process in a new process group.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Creating a new process group enables sending signals to the process (e.g., SIGINT, SIGQUIT)
        /// on Windows and provides process group isolation on all platforms.
        /// </para>
        /// <para>
        /// On Unix systems, child processes in a new process group won't receive signals sent to the parent's
        /// process group, which can be useful for background processes that should continue running independently.
        /// </para>
        /// </remarks>
        public bool CreateNewProcessGroup { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ProcessStartOptions"/> class.
        /// </summary>
        /// <param name="fileName">The application to start.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="fileName"/> is null.</exception>
        /// <exception cref="ArgumentException">Thrown when <paramref name="fileName"/> is empty.</exception>
        /// <exception cref="FileNotFoundException">Thrown when <paramref name="fileName"/> cannot be resolved to an existing file.</exception>
        public ProcessStartOptions(string fileName)
        {
            ArgumentException.ThrowIfNullOrEmpty(fileName);

            string? resolved = ResolvePath(fileName);
            if (resolved == null || !File.Exists(resolved))
            {
                throw new FileNotFoundException(SR.FileNotFoundResolvePath, fileName);
            }
            _fileName = resolved;
        }

        internal static string? ResolvePath(string filename)
        {
            // If the filename is a complete path, use it, regardless of whether it exists.
            if (Path.IsPathRooted(filename))
            {
                // In this case, it doesn't matter whether the file exists or not;
                // it's what the caller asked for, so it's what they'll get
                return filename;
            }

#if WINDOWS
            // From: https://learn.microsoft.com/en-us/windows/win32/api/processthreadsapi/nf-processthreadsapi-createprocessw
            // "If the file name does not contain an extension, .exe is appended.
            // Therefore, if the file name extension is .com, this parameter must include the .com extension.
            // If the file name ends in a period (.) with no extension, or if the file name contains a path, .exe is not appended."

            // HasExtension returns false for trailing dot, so we need to check that separately
            if (filename[filename.Length - 1] != '.'
                && string.IsNullOrEmpty(Path.GetDirectoryName(filename))
                && !Path.HasExtension(filename))
            {
                filename += ".exe";
            }
#endif

            // Then check the executable's directory
            string? path = System.Environment.ProcessPath;
            if (path != null)
            {
                try
                {
                    path = Path.Combine(Path.GetDirectoryName(path)!, filename);
                    if (File.Exists(path))
                    {
                        return path;
                    }
                }
                catch (ArgumentException) { } // ignore any errors in data that may come from the exe path
            }

            // Then check the current directory
            path = Path.Combine(Directory.GetCurrentDirectory(), filename);
            if (File.Exists(path))
            {
                return path;
            }

#if WINDOWS
            // Windows-specific search locations (from CreateProcessW documentation)

            // Check the system directory (e.g., System32)
            path = GetSystemDirectory();
            if (path != null)
            {
                path = Path.Combine(path, filename);
                if (File.Exists(path))
                {
                    return path;
                }
            }

            // Check the Windows directory
            path = GetWindowsDirectory();
            if (path != null)
            {
                // Check the legacy System subdirectory of Windows directory (for compatibility)
                string systemPath = Path.Combine(path, "System", filename);
                if (File.Exists(systemPath))
                {
                    return systemPath;
                }

                // Check the Windows directory itself
                path = Path.Combine(path, filename);
                if (File.Exists(path))
                {
                    return path;
                }
            }
#endif

            return FindProgramInPath(filename);
        }

        internal static string? FindProgramInPath(string program)
        {
            string? pathEnvVar = System.Environment.GetEnvironmentVariable("PATH");
            if (pathEnvVar != null)
            {
                char pathSeparator = OperatingSystem.IsWindows() ? ';' : ':';
                var pathParser = new StringParser(pathEnvVar, pathSeparator, skipEmpty: true);
                while (pathParser.MoveNext())
                {
                    string subPath = pathParser.ExtractCurrent();
                    string path = Path.Combine(subPath, program);
#if WINDOWS
                    if (File.Exists(path))
#else
                    if (Process.IsExecutable(path))
#endif
                    {
                        return path;
                    }
                }
            }

            return null;
        }

#if WINDOWS
        private static string? s_cachedSystemDirectory;
        private static string? s_cachedWindowsDirectory;

        private static string? GetSystemDirectory()
        {
            return s_cachedSystemDirectory ??= System.Environment.SystemDirectory;
        }

        private static string? GetWindowsDirectory()
        {
            if (s_cachedWindowsDirectory == null)
            {
                Span<char> buffer = stackalloc char[260]; // MAX_PATH
                uint length = Interop.Kernel32.GetWindowsDirectoryW(ref MemoryMarshal.GetReference(buffer), (uint)buffer.Length);
                if (length > 0 && length < buffer.Length)
                {
                    s_cachedWindowsDirectory = new string(buffer.Slice(0, (int)length));
                }
            }
            return s_cachedWindowsDirectory;
        }
#endif
    }
}
