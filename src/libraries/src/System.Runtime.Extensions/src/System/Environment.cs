// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Internal.Runtime.Augments;
using System;
using System.Collections;
using System.IO;
using System.Reflection;
using System.Text;

namespace System
{
    public static partial class Environment
    {
        public static string CommandLine
        {
            get
            {
                StringBuilder sb = StringBuilderCache.Acquire();

                foreach (string arg in GetCommandLineArgs())
                {
                    bool containsQuotes = false, containsWhitespace = false;
                    foreach (char c in arg)
                    {
                        if (char.IsWhiteSpace(c))
                        {
                            containsWhitespace = true;
                        }
                        else if (c == '"')
                        {
                            containsQuotes = true;
                        }
                    }

                    string quote = containsWhitespace ? "\"" : "";
                    string formattedArg = containsQuotes && containsWhitespace ? arg.Replace("\"", "\\\"") : arg;

                    sb.Append(quote).Append(formattedArg).Append(quote).Append(' ');
                }

                if (sb.Length > 0)
                {
                    sb.Length--;
                }

                return StringBuilderCache.GetStringAndRelease(sb);
            }
        }

        public static string CurrentDirectory
        {
            get { return CurrentDirectoryCore; }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }

                if (value.Length == 0)
                {
                    throw new ArgumentException(SR.Argument_PathEmpty, nameof(value));
                }

                CurrentDirectoryCore = value;
            }
        }

        public static int CurrentManagedThreadId => EnvironmentAugments.CurrentManagedThreadId;

        public static void Exit(int exitCode) => EnvironmentAugments.Exit(exitCode);
        
        public static void FailFast(string message) => FailFast(message, exception: null);

        public static void FailFast(string message, Exception exception) => EnvironmentAugments.FailFast(message, exception);

        public static string ExpandEnvironmentVariables(string name)
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            if (name.Length == 0)
            {
                return name;
            }

            return ExpandEnvironmentVariablesCore(name);
        }

        public static string[] GetCommandLineArgs() => EnvironmentAugments.GetCommandLineArgs();

        public static string GetEnvironmentVariable(string variable) 
        {
            if (variable == null)
            {
                throw new ArgumentNullException(nameof(variable));
            }

            // separated from the EnvironmentVariableTarget overload to help with tree shaking in common case
            return GetEnvironmentVariableCore(variable);
        }

        public static string GetEnvironmentVariable(string variable, EnvironmentVariableTarget target)
        {
            if (variable == null)
            {
                throw new ArgumentNullException(nameof(variable));
            }

            ValidateTarget(target);

            return GetEnvironmentVariableCore(variable, target);
        }

        public static IDictionary GetEnvironmentVariables()
        {
            // separated from the EnvironmentVariableTarget overload to help with tree shaking in common case
            return GetEnvironmentVariablesCore();
        }

        public static IDictionary GetEnvironmentVariables(EnvironmentVariableTarget target)
        {
            ValidateTarget(target);

            return GetEnvironmentVariablesCore(target);
        }

        public static string GetFolderPath(SpecialFolder folder) => GetFolderPath(folder, SpecialFolderOption.None);

        public static string GetFolderPath(SpecialFolder folder, SpecialFolderOption option)
        {
            if (!Enum.IsDefined(typeof(SpecialFolder), folder))
            {
                throw new ArgumentOutOfRangeException(nameof(folder), folder, SR.Format(SR.Arg_EnumIllegalVal, folder));
            }

            if (option != SpecialFolderOption.None && !Enum.IsDefined(typeof(SpecialFolderOption), option))
            {
                throw new ArgumentOutOfRangeException(nameof(option), option, SR.Format(SR.Arg_EnumIllegalVal, option));
            }

            return GetFolderPathCore(folder, option);
        }

        public static bool HasShutdownStarted => EnvironmentAugments.HasShutdownStarted;

        public static bool Is64BitProcess => IntPtr.Size == 8;

        public static bool Is64BitOperatingSystem => Is64BitProcess || Is64BitOperatingSystemWhen32BitProcess;
        
        public static void SetEnvironmentVariable(string variable, string value)
        {
            ValidateVariableAndValue(variable, ref value);

            // separated from the EnvironmentVariableTarget overload to help with tree shaking in common case
            SetEnvironmentVariableCore(variable, value);
        }

        public static void SetEnvironmentVariable(string variable, string value, EnvironmentVariableTarget target)
        {
            ValidateVariableAndValue(variable, ref value);
            ValidateTarget(target);

            SetEnvironmentVariableCore(variable, value, target);
        }

        private static void ValidateVariableAndValue(string variable, ref string value)
        {
            const int MaxEnvVariableValueLength = 32767;

            if (variable == null)
            {
                throw new ArgumentNullException(nameof(variable));
            }
            if (variable.Length == 0)
            {
                throw new ArgumentException(SR.Argument_StringZeroLength, nameof(variable));
            }
            if (variable[0] == '\0')
            {
                throw new ArgumentException(SR.Argument_StringFirstCharIsZero, nameof(variable));
            }
            if (variable.Length >= MaxEnvVariableValueLength)
            {
                throw new ArgumentException(SR.Argument_LongEnvVarValue, nameof(variable));
            }
            if (variable.IndexOf('=') != -1)
            {
                throw new ArgumentException(SR.Argument_IllegalEnvVarName, nameof(variable));
            }

            if (string.IsNullOrEmpty(value) || value[0] == '\0')
            {
                // Explicitly null out value if it's empty
                value = null;
            }
            else if (value.Length >= MaxEnvVariableValueLength)
            {
                throw new ArgumentException(SR.Argument_LongEnvVarValue, nameof(value));
            }
        }

        public static OperatingSystem OSVersion => s_osVersion.Value;

        public static string StackTrace => EnvironmentAugments.StackTrace;

        public static int TickCount => EnvironmentAugments.TickCount;

        public static bool UserInteractive => true;

        public static Version Version
        {
            // Previously this represented the File version of mscorlib.dll.  Many other libraries in the framework and outside took dependencies on the first three parts of this version 
            // remaining constant throughout 4.x.  From 4.0 to 4.5.2 this was fine since the file version only incremented the last part. Starting with 4.6 we switched to a file versioning
            // scheme that matched the product version.  In order to preserve compatibility with existing libraries, this needs to be hard-coded.
            get { return new Version(4, 0, 30319, 42000); }
        }

        public static long WorkingSet
        {
            get
            {
                // Use reflection to access the implementation in System.Diagnostics.Process.dll.  While far from ideal,
                // we do this to avoid duplicating the Windows, Linux, macOS, and potentially other platform-specific implementations
                // present in Process.  If it proves important, we could look at separating that functionality out of Process into
                // Common files which could also be included here.
                Type processType = Type.GetType("System.Diagnostics.Process, System.Diagnostics.Process, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a", throwOnError: false);
                IDisposable currentProcess = processType?.GetTypeInfo().GetDeclaredMethod("GetCurrentProcess")?.Invoke(null, null) as IDisposable;
                if (currentProcess != null)
                {
                    try
                    {
                        object result = processType.GetTypeInfo().GetDeclaredProperty("WorkingSet64")?.GetMethod?.Invoke(currentProcess, null);
                        if (result is long) return (long)result;
                    }
                    finally { currentProcess.Dispose(); }
                }

                // Could not get the current working set.
                return 0;
            }
        }

        private static void ValidateTarget(EnvironmentVariableTarget target)
        {
            if (target != EnvironmentVariableTarget.Process &&
                target != EnvironmentVariableTarget.Machine &&
                target != EnvironmentVariableTarget.User)
            {
                throw new ArgumentOutOfRangeException(nameof(target), target, SR.Format(SR.Arg_EnumIllegalVal, target));
            }
        }
    }
}

namespace Internal.Runtime.Augments
{
    // TODO: Temporary mechanism for getting at System.Private.Corelib's runtime-based Environment functionality.
    // This should be moved to a different "internal" class in System.Private.Corelib, exposed to corefx but not in a contract,
    // so that it may be accessed from System.Runtime.Extensions without needing to use reflection. (Our build environment
    // doesn't currently appear to support extern aliases, or else we could simply call the relevant functionality on the Environment
    // in Corelib directly.) In the meantime, we create delegates to the various pieces of functionality.
    internal static class EnvironmentAugments
    {
        private static readonly Type s_environment = typeof(object).GetTypeInfo().Assembly.GetType("System.Environment", throwOnError: true, ignoreCase: false);

        private static readonly Lazy<Func<int>> s_currentManagedThreadId = CreateGetter<Func<int>>("CurrentManagedThreadId");
        private static readonly Lazy<Func<int>> s_exitCodeGet = CreateGetter<Func<int>>("ExitCode");
        private static readonly Lazy<Action<int>> s_exitCodeSet = CreateSetter<Action<int>>("ExitCode");
        private static readonly Lazy<Func<bool>> s_hasShutdownStarted = CreateGetter<Func<bool>>("HasShutdownStarted");
        private static readonly Lazy<Func<string>> s_stackTrace = CreateGetter<Func<string>>("StackTrace");
        private static readonly Lazy<Func<int>> s_tickCount = CreateGetter<Func<int>>("TickCount");

        private static readonly Lazy<Action<int>> s_exit = CreateMethod<Action<int>>("Exit", 1);
        private static readonly Lazy<Action<string, Exception>> s_failFast = CreateMethod<Action<string, Exception>>("FailFast", 2);
        private static readonly Lazy<Func<string[]>> s_getCommandLineArgs = CreateMethod<Func<string[]>>("GetCommandLineArgs", 0);

        private static Lazy<TDelegate> CreateMethod<TDelegate>(string name, int argCount) =>
            new Lazy<TDelegate>(() =>
            {
                foreach (var method in s_environment.GetTypeInfo().GetDeclaredMethods(name))
                {
                    ParameterInfo[] parameters = method.GetParameters();
                    if (parameters.Length == argCount)
                    {
                        return (TDelegate)(object)method.CreateDelegate(typeof(TDelegate));
                    }
                }
                return default(TDelegate);
            });

        private static Lazy<TDelegate> CreateGetter<TDelegate>(string name) =>
            new Lazy<TDelegate>(() => (TDelegate)(object)s_environment.GetTypeInfo().GetDeclaredProperty(name).GetMethod.CreateDelegate(typeof(TDelegate)));

        private static Lazy<TDelegate> CreateSetter<TDelegate>(string name) =>
            new Lazy<TDelegate>(() => (TDelegate)(object)s_environment.GetTypeInfo().GetDeclaredProperty(name).SetMethod.CreateDelegate(typeof(TDelegate)));

        public static int CurrentManagedThreadId => s_currentManagedThreadId.Value();
        public static int ExitCode { get { return s_exitCodeGet.Value(); } set { s_exitCodeSet.Value(value); } }
        public static bool HasShutdownStarted => s_hasShutdownStarted.Value();
        public static string StackTrace => s_stackTrace.Value();
        public static int TickCount => s_tickCount.Value();

        public static void Exit(int exitCode) => s_exit.Value(ExitCode);
        public static void FailFast(string message, Exception error) => s_failFast.Value(message, error);
        public static string[] GetCommandLineArgs() => s_getCommandLineArgs.Value();
    }
}
