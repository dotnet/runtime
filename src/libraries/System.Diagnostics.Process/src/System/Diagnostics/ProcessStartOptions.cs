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
        /// Gets the absolute path of the application to start.
        /// </summary>
        /// <value>
        /// The absolute path to the executable file. This path is resolved from the <c>fileName</c> parameter
        /// passed to the constructor by searching through various directories if needed.
        /// </value>
        /// <remarks>
        /// <para>
        /// The path is "resolved" meaning it has been converted to an absolute path and verified to exist.
        /// </para>
        /// <para>
        /// See <see cref="ProcessStartOptions(string)"/> for complete details on the resolution process.
        /// </para>
        /// </remarks>
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

        internal bool HasEnvironmentBeenAccessed => _environment is not null;

        internal bool HasInheritedHandlesBeenAccessed => _inheritedHandles is not null;

        internal bool HasArgumentsBeenAccessed => _arguments is not null;

        /// <summary>
        /// Initializes a new instance of the <see cref="ProcessStartOptions"/> class.
        /// </summary>
        /// <param name="fileName">The application to start.</param>
        /// <exception cref="ArgumentNullException"><paramref name="fileName"/> is <see langword="null" />.</exception>
        /// <exception cref="ArgumentException"><paramref name="fileName"/> is empty.</exception>
        /// <exception cref="FileNotFoundException"><paramref name="fileName"/> cannot be resolved to an existing file.</exception>
        /// <remarks>
        /// <para>
        /// The <paramref name="fileName"/> is resolved to an absolute path.
        /// </para>
        /// <para>
        /// When the <paramref name="fileName"/> is a fully qualified path, it is used as-is without any resolution.
        /// </para>
        /// <para>
        /// When the <paramref name="fileName"/> is a rooted but not fully qualified path (for example, <c>C:foo.exe</c> or <c>\foo\bar.exe</c> on Windows),
        /// it is resolved to an absolute path using the current directory context.
        /// </para>
        /// <para>
        /// When the <paramref name="fileName"/> is an explicit relative path containing directory separators (for example, <c>.\foo.exe</c> or <c>../bar</c>),
        /// it is resolved relative to the current directory.
        /// </para>
        /// <para>
        /// When the <paramref name="fileName"/> is a bare filename without directory separators, the system searches for the executable in the following locations:
        /// </para>
        /// <para>
        /// On Windows:
        /// </para>
        /// <list type="number">
        /// <item><description>The System directory (for example, <c>C:\Windows\System32</c>).</description></item>
        /// <item><description>The directories listed in the PATH environment variable.</description></item>
        /// </list>
        /// <para>
        /// On Unix:
        /// </para>
        /// <list type="number">
        /// <item><description>The directories listed in the PATH environment variable.</description></item>
        /// </list>
        /// <para>
        /// On Windows, if the <paramref name="fileName"/> does not have an extension and does not contain directory separators, <c>.exe</c> is appended before searching.
        /// </para>
        /// </remarks>
        public ProcessStartOptions(string fileName)
        {
            ArgumentException.ThrowIfNullOrEmpty(fileName);

            // The file could be deleted or replaced after this check and before the process is started (TOCTOU).
            // In such case, the process creation will fail.
            // We resolve the path here to provide unified error handling and to avoid
            // starting a process that will fail immediately after creation.
            string? resolved = ResolvePath(fileName, out bool requiresExistenceCheck);
            if (resolved is null || (requiresExistenceCheck && !File.Exists(resolved)))
            {
                throw new FileNotFoundException(SR.FileNotFoundResolvePath, fileName);
            }
            _fileName = resolved;
        }

        // There are two ways to create a process on Windows using CreateProcess sys-call:
        // 1. With NULL lpApplicationName and non-NULL lpCommandLine, where the first token of the
        // command line is the executable name. In this case, the system will resolve the executable
        // name to an actual file on disk using an algorithm that is not fully documented.
        // 2. With non-NULL lpApplicationName, where the system will use the provided application
        // name as-is without any resolution, and the command line is passed as-is to the process.
        //
        // The recommended way is to use the second approach and provide the resolved executable path.
        //
        // Changing the resolution logic for existing Process APIs would introduce breaking changes.
        // Since we are introducing a new API, we take it as an opportunity to clean up the legacy baggage
        // to have simpler, easier to understand and more secure filename resolution algorithm
        // that is more consistent across OSes and aligned with other modern platforms.
        private static string? ResolvePath(string filename, out bool requiresExistenceCheck)
        {
            Debug.Assert(!string.IsNullOrEmpty(filename), "Caller should have validated the filename.");
            requiresExistenceCheck = true;

            if (Path.IsPathFullyQualified(filename))
            {
                return filename;
            }

            // Check for filenames that are not bare filenames. It includes:
            // - Relative paths with directory separators (e.g., .\foo.exe, ..\foo.exe, subdir\foo.exe)
            // - Rooted but not fully qualified paths (e.g., C:foo.exe, \foo.exe on Windows)
            if (Path.GetFileName(filename.AsSpan()).Length != filename.Length)
            {
                return Path.GetFullPath(filename); // Resolve to absolute path
            }

            // We want to keep the resolution logic in one place for better maintainability and consistency.
            // That is why we don't provide platform-specific implementations files.
            if (OperatingSystem.IsWindows())
            {
                // From: https://learn.microsoft.com/en-us/windows/win32/api/processthreadsapi/nf-processthreadsapi-createprocessw
                // "If the file name does not contain an extension, .exe is appended.
                // Therefore, if the file name extension is .com, this parameter must include the .com extension.
                // If the file name ends in a period (.) with no extension, or if the file name contains a path, .exe is not appended."

                // HasExtension returns false for trailing dot, so we need to check that separately
                if (filename[filename.Length - 1] != '.' && !Path.HasExtension(filename))
                {
                    filename += ".exe";
                }

                // Windows-specific search location: the system directory (e.g., C:\Windows\System32)
                string path = Path.Combine(System.Environment.SystemDirectory, filename);
                if (File.Exists(path))
                {
                    requiresExistenceCheck = false;
                    return path;
                }
            }

            string? fromPath = ProcessUtils.FindProgramInPath(filename);
            requiresExistenceCheck = fromPath is null;
            return fromPath;
        }
    }
}
