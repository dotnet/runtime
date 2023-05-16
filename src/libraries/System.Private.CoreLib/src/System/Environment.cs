// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;

namespace System
{
    public static partial class Environment
    {
        public static int ProcessorCount { get; } = GetProcessorCount();

        /// <summary>
        /// Gets whether the current machine has only a single processor.
        /// </summary>
        internal static bool IsSingleProcessor => ProcessorCount == 1;

        private static volatile sbyte s_privilegedProcess;

        /// <summary>
        /// Gets whether the current process is authorized to perform security-relevant functions.
        /// </summary>
        public static bool IsPrivilegedProcess
        {
            get
            {
                sbyte privilegedProcess = s_privilegedProcess;
                if (privilegedProcess == 0)
                {
                    s_privilegedProcess = privilegedProcess = IsPrivilegedProcessCore() ? (sbyte)1 : (sbyte)-1;
                }
                return privilegedProcess > 0;
            }
        }

        // Unconditionally return false since .NET Core does not support object finalization during shutdown.
        public static bool HasShutdownStarted => false;

        public static string? GetEnvironmentVariable(string variable)
        {
            ArgumentNullException.ThrowIfNull(variable);

            return GetEnvironmentVariableCore(variable);
        }

        public static string? GetEnvironmentVariable(string variable, EnvironmentVariableTarget target)
        {
            if (target == EnvironmentVariableTarget.Process)
                return GetEnvironmentVariable(variable);

            ArgumentNullException.ThrowIfNull(variable);

            bool fromMachine = ValidateAndConvertRegistryTarget(target);
            return GetEnvironmentVariableFromRegistry(variable, fromMachine);
        }

        public static IDictionary GetEnvironmentVariables(EnvironmentVariableTarget target)
        {
            if (target == EnvironmentVariableTarget.Process)
                return GetEnvironmentVariables();

            bool fromMachine = ValidateAndConvertRegistryTarget(target);
            return GetEnvironmentVariablesFromRegistry(fromMachine);
        }

        public static void SetEnvironmentVariable(string variable, string? value)
        {
            ValidateVariableAndValue(variable, ref value);
            SetEnvironmentVariableCore(variable, value);
        }

        public static void SetEnvironmentVariable(string variable, string? value, EnvironmentVariableTarget target)
        {
            if (target == EnvironmentVariableTarget.Process)
            {
                SetEnvironmentVariable(variable, value);
                return;
            }

            ValidateVariableAndValue(variable, ref value);

            bool fromMachine = ValidateAndConvertRegistryTarget(target);
            SetEnvironmentVariableFromRegistry(variable, value, fromMachine: fromMachine);
        }

#if !MONO
        internal static string[]? s_commandLineArgs;

        public static string[] GetCommandLineArgs()
        {
            // s_commandLineArgs is expected to be initialize with application command line arguments
            // during startup. GetCommandLineArgsNative fallback is used for hosted libraries.
            return s_commandLineArgs != null ?
                (string[])s_commandLineArgs.Clone() :
                GetCommandLineArgsNative();
        }
#endif

        public static string CommandLine => PasteArguments.Paste(GetCommandLineArgs(), pasteFirstArgumentUsingArgV0Rules: true);

        public static string CurrentDirectory
        {
            get => CurrentDirectoryCore;
            set
            {
                ArgumentException.ThrowIfNullOrEmpty(value);
                CurrentDirectoryCore = value;
            }
        }

        public static string ExpandEnvironmentVariables(string name)
        {
            ArgumentNullException.ThrowIfNull(name);

            if (name.Length == 0)
                return name;

            return ExpandEnvironmentVariablesCore(name);
        }

        public static string GetFolderPath(SpecialFolder folder) => GetFolderPath(folder, SpecialFolderOption.None);

        public static string GetFolderPath(SpecialFolder folder, SpecialFolderOption option)
        {
            if (!Enum.IsDefined(folder))
                throw new ArgumentOutOfRangeException(nameof(folder), folder, SR.Format(SR.Arg_EnumIllegalVal, folder));

            if (option != SpecialFolderOption.None && !Enum.IsDefined(option))
                throw new ArgumentOutOfRangeException(nameof(option), option, SR.Format(SR.Arg_EnumIllegalVal, option));

            return GetFolderPathCore(folder, option);
        }

        private static volatile int s_processId;

        /// <summary>Gets the unique identifier for the current process.</summary>
        public static int ProcessId
        {
            get
            {
                int processId = s_processId;
                if (processId == 0)
                {
                    s_processId = processId = GetProcessId();
                    // Assume that process Id zero is invalid for user processes. It holds for all mainstream operating systems.
                    Debug.Assert(processId != 0);
                }
                return processId;
            }
        }

        private static volatile string? s_processPath;

        /// <summary>
        /// Returns the path of the executable that started the currently executing process. Returns null when the path is not available.
        /// </summary>
        /// <returns>Path of the executable that started the currently executing process</returns>
        /// <remarks>
        /// If the executable is renamed or deleted before this property is first accessed, the return value is undefined and depends on the operating system.
        /// </remarks>
        public static string? ProcessPath
        {
            get
            {
                string? processPath = s_processPath;
                if (processPath == null)
                {
                    // The value is cached both as a performance optimization and to ensure that the API always returns
                    // the same path in a given process.
                    Interlocked.CompareExchange(ref s_processPath, GetProcessPath() ?? "", null);
                    processPath = s_processPath;
                    Debug.Assert(processPath != null);
                }
                return (processPath.Length != 0) ? processPath : null;
            }
        }

        public static bool Is64BitProcess => IntPtr.Size == 8;

        public static bool Is64BitOperatingSystem => Is64BitProcess || Is64BitOperatingSystemWhen32BitProcess;

        public static string NewLine => NewLineConst;

        private static volatile OperatingSystem? s_osVersion;

        public static OperatingSystem OSVersion
        {
            get
            {
                OperatingSystem? osVersion = s_osVersion;
                if (osVersion == null)
                {
                    Interlocked.CompareExchange(ref s_osVersion, GetOSVersion(), null);
                    osVersion = s_osVersion;
                    Debug.Assert(osVersion != null);
                }
                return osVersion;
            }
        }

        public static Version Version
        {
            get
            {
                string? versionString = typeof(object).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;

                ReadOnlySpan<char> versionSpan = versionString.AsSpan();

                // Strip optional suffixes
                int separatorIndex = versionSpan.IndexOfAny('-', '+', ' ');
                if (separatorIndex >= 0)
                    versionSpan = versionSpan.Slice(0, separatorIndex);

                // Return zeros rather then failing if the version string fails to parse
                return Version.TryParse(versionSpan, out Version? version) ? version : new Version();
            }
        }

        public static string StackTrace
        {
            [MethodImpl(MethodImplOptions.NoInlining)] // Prevent inlining from affecting where the stacktrace starts
            get => new StackTrace(true).ToString(System.Diagnostics.StackTrace.TraceFormat.Normal);
        }

        private static volatile int s_systemPageSize;

        public static int SystemPageSize
        {
            get
            {
                int systemPageSize = s_systemPageSize;
                if (systemPageSize == 0)
                {
                    s_systemPageSize = systemPageSize = GetSystemPageSize();
                }
                return systemPageSize;
            }
        }

        private static bool ValidateAndConvertRegistryTarget(EnvironmentVariableTarget target)
        {
            Debug.Assert(target != EnvironmentVariableTarget.Process);

            if (target == EnvironmentVariableTarget.Machine)
                return true;

            if (target == EnvironmentVariableTarget.User)
                return false;

            throw new ArgumentOutOfRangeException(nameof(target), target, SR.Format(SR.Arg_EnumIllegalVal, target));
        }

        private static void ValidateVariableAndValue(string variable, ref string? value)
        {
            ArgumentException.ThrowIfNullOrEmpty(variable);

            if (variable[0] == '\0')
                throw new ArgumentException(SR.Argument_StringFirstCharIsZero, nameof(variable));

            if (variable.Contains('='))
                throw new ArgumentException(SR.Argument_IllegalEnvVarName, nameof(variable));

            if (string.IsNullOrEmpty(value) || value[0] == '\0')
            {
                // Explicitly null out value if it's empty
                value = null;
            }
        }
    }
}
