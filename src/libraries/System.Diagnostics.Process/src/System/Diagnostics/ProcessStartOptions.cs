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
        /// If a relative path was provided to the constructor, it was located by searching in the executable's
        /// directory, current directory, system directories (on Windows), and directories in the PATH environment variable.
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

        /// <summary>
        /// Initializes a new instance of the <see cref="ProcessStartOptions"/> class.
        /// </summary>
        /// <param name="fileName">The application to start.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="fileName"/> is null.</exception>
        /// <exception cref="ArgumentException">Thrown when <paramref name="fileName"/> is empty.</exception>
        /// <exception cref="FileNotFoundException">Thrown when <paramref name="fileName"/> cannot be resolved to an existing file.</exception>
        /// <remarks>
        /// <para>
        /// The <paramref name="fileName"/> is resolved to an absolute path before starting the process.
        /// </para>
        /// <para>
        /// When the <paramref name="fileName"/> is a rooted path, it is used as-is without any resolution.
        /// </para>
        /// <para>
        /// On Windows, when <paramref name="fileName"/> is not a rooted path, the system searches
        /// for the executable in the following order:
        /// </para>
        /// <list type="number">
        /// <item><description>The System directory.</description></item>
        /// <item><description>The directories listed in the PATH environment variable.</description></item>
        /// </list>
        /// <para>
        /// On Unix, when <paramref name="fileName"/> is not a rooted path, the system searches
        /// for the executable in the directories listed in the PATH environment variable.
        /// </para>
        /// <para>
        /// On Windows, if the <paramref name="fileName"/> does not have an extension, ".exe" is appended to it before searching.
        /// </para>
        /// </remarks>
        public ProcessStartOptions(string fileName)
        {
            ArgumentException.ThrowIfNullOrEmpty(fileName);

            // The file could be deleted or replaced after this check and before the process is started (TOCTOU).
            // In such case, the process creation will fail.
            // We resolve the path here to provide unified error handling and to avoid
            // starting a process that will fail immediately after creation.
            string? resolved = ResolvePath(fileName);
            if (resolved == null || !File.Exists(resolved))
            {
                throw new FileNotFoundException(SR.FileNotFoundResolvePath, fileName);
            }
            _fileName = resolved;
        }

        // There are two ways to create a process on Windows using CreateProcess sys-call:
        // 1. With NULL lpApplicationName and non-NULL lpCommandLine, where the first token of the
        // command line is the executable name. In this case, the system will resolve the executable
        // name to an actual file on disk using documented search order.
        // 2. With non-NULL lpApplicationName, where the system will use the provided application
        // name as-is without any resolution, and the command line is passed as-is to the process.
        //
        // The official documentation mentions:
        // "(..) do not pass NULL for lpApplicationName. If you do pass NULL for lpApplicationName,
        // use quotation marks around the executable path in lpCommandLine."
        //
        // We have asked the CreateProcess owners (Windows Team) for feedback. This is what they wrote:
        // "Applications should not under any circumstances pass user-controlled input directly to CreateProcess;
        // or, if they intend to do so (for passing user-originated parameters), they should be filling out the lpApplicationName parameter."
        //
        // We could either document that the FileName should never be user-controlled input or resolve it ourselves,
        // pass it as lpApplicationName and arguments as lpCommandLine.
        //
        // On Unix, we were already resolving the executable path before starting the process.
        // We were doing it for two reasons:
        // 1. To mimic Windows resolution path order (consistency across platforms).
        // 2. To avoid forking a process and then failing in exec because the file cannot be found.
        // By resolving the path ourselves before forking we can avoid creating a child process that will fail immediately after creation.
        //
        // Changing the resolution logic could introduce breaking changes. For example:
        // "If the executable module is a 16-bit application, lpApplicationName should be NULL, and the
        // string pointed to by lpCommandLine should specify the executable module as well as its arguments."
        //
        // That is why we don't change the resolution logic for existing APIs, but since we are introducing a new API,
        // we have the opportunity to do it in a more secure way by always resolving the executable name.
        // Moreover, it gives the users the opportunity to:
        // - check the resolved executable path before starting the process.
        // - cache the resolved path and reuse it for subsequent process creations (perf).
        //
        // In upcoming .NET 11 previews, we may consider changing the order.
        internal static string? ResolvePath(string filename)
        {
            // If the filename is a complete path, use it, regardless of whether it exists.
            if (Path.IsPathFullyQualified(filename))
            {
                return filename;
            }

            // Handle rooted but not fully qualified paths (e.g., C:foo.exe, \foo.exe on Windows)
            // On Unix, this never executes since rooted paths are always fully qualified
            if (Path.IsPathRooted(filename))
            {
                return Path.GetFullPath(filename); // Resolve to absolute path
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

#if WINDOWS
            // Windows-specific search location: the system directory (e.g., C:\Windows\System32)
            string path = System.Environment.SystemDirectory;
            if (path != null)
            {
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
    }
}
