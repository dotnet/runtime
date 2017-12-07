// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Internal.Runtime.Augments;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;

namespace System
{
    public static partial class Environment
    {
        public static string GetEnvironmentVariable(string variable)
        {
            return EnvironmentAugments.GetEnvironmentVariable(variable);
        }

        public static string GetEnvironmentVariable(string variable, EnvironmentVariableTarget target)
        {
            return EnvironmentAugments.GetEnvironmentVariable(variable, target);
        }

        public static IDictionary GetEnvironmentVariables()
        {
            // To maintain complete compatibility with prior versions we need to return a Hashtable.
            // We did ship a prior version of Core with LowLevelDictionary, which does iterate the
            // same (e.g. yields DictionaryEntry), but it is not a public type.
            //
            // While we could pass Hashtable back from CoreCLR the type is also defined here. We only
            // want to surface the local Hashtable.
            return EnvironmentAugments.EnumerateEnvironmentVariables().ToHashtable();
        }

        public static IDictionary GetEnvironmentVariables(EnvironmentVariableTarget target)
        {
            // See comments in GetEnvironmentVariables()
            return EnvironmentAugments.EnumerateEnvironmentVariables(target).ToHashtable();
        }

        private static Hashtable ToHashtable(this IEnumerable<KeyValuePair<string, string>> pairs)
        {
            Hashtable hashTable = new Hashtable();
            foreach (KeyValuePair<string, string> pair in pairs)
            {
                hashTable.Add(pair.Key, pair.Value);
            }
            return hashTable;
        }

        public static void SetEnvironmentVariable(string variable, string value)
        {
            EnvironmentAugments.SetEnvironmentVariable(variable, value);
        }

        public static void SetEnvironmentVariable(string variable, string value, EnvironmentVariableTarget target)
        {
            EnvironmentAugments.SetEnvironmentVariable(variable, value, target);
        }

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

        public static OperatingSystem OSVersion => s_osVersion.Value;

        public static int ProcessorCount => EnvironmentAugments.ProcessorCount;

        public static string StackTrace
        {
            [MethodImpl(MethodImplOptions.NoInlining)] // Prevent inlining from affecting where the stacktrace starts
            get
            {
                return EnvironmentAugments.StackTrace;
            }
        }

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
                    catch (TargetInvocationException tie)
                    {
                        if(tie.InnerException != null)
                            throw tie.InnerException;

                        throw tie;
                    }
                    finally { currentProcess.Dispose(); }
                }

                // Could not get the current working set.
                return 0;
            }
        }
    }
}
