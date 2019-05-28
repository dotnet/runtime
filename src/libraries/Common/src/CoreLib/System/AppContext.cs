// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Runtime.Loader;
using System.Runtime.Versioning;
using System.Threading;

namespace System
{
    public static partial class AppContext
    {
        private static readonly Dictionary<string, object?> s_dataStore = new Dictionary<string, object?>();
        private static Dictionary<string, bool>? s_switches;
        private static string? s_defaultBaseDirectory;

        public static string? BaseDirectory
        {
            get
            {
                // The value of APP_CONTEXT_BASE_DIRECTORY key has to be a string and it is not allowed to be any other type. 
                // Otherwise the caller will get invalid cast exception
                return (string?)GetData("APP_CONTEXT_BASE_DIRECTORY") ??
                    (s_defaultBaseDirectory ?? (s_defaultBaseDirectory = GetBaseDirectoryCore()));
            }
        }

        public static string? TargetFrameworkName
        {
            get
            {
                // The Target framework is not the framework that the process is actually running on.
                // It is the value read from the TargetFrameworkAttribute on the .exe that started the process.
                return Assembly.GetEntryAssembly()?.GetCustomAttribute<TargetFrameworkAttribute>()?.FrameworkName;
            }
        }

        public static object? GetData(string name)
        {
            if (name == null)
                throw new ArgumentNullException(nameof(name));

            object? data;
            lock (s_dataStore)
            {
                s_dataStore.TryGetValue(name, out data);
            }
            return data;
        }

        public static void SetData(string name, object? data)
        {
            if (name == null)
                throw new ArgumentNullException(nameof(name));

            lock (s_dataStore)
            {
                s_dataStore[name] = data;
            }
        }

#pragma warning disable CS0067 // events raised by the VM
        public static event UnhandledExceptionEventHandler UnhandledException;

        public static event System.EventHandler<FirstChanceExceptionEventArgs> FirstChanceException;
#pragma warning restore CS0067

        public static event System.EventHandler ProcessExit;

        internal static void OnProcessExit()
        {
            AssemblyLoadContext.OnProcessExit();

            ProcessExit?.Invoke(null /* AppDomain */, EventArgs.Empty);
        }

        /// <summary>
        /// Try to get the value of the switch.
        /// </summary>
        /// <param name="switchName">The name of the switch</param>
        /// <param name="isEnabled">A variable where to place the value of the switch</param>
        /// <returns>A return value of true represents that the switch was set and <paramref name="isEnabled"/> contains the value of the switch</returns>
        public static bool TryGetSwitch(string switchName, out bool isEnabled)
        {
            if (switchName == null)
                throw new ArgumentNullException(nameof(switchName));
            if (switchName.Length == 0)
                throw new ArgumentException(SR.Argument_EmptyName, nameof(switchName));

            if (s_switches != null)
            {
                lock (s_switches)
                {
                    if (s_switches.TryGetValue(switchName, out isEnabled))
                        return true;
                }
            }

            if (GetData(switchName) is string value && bool.TryParse(value, out isEnabled))
            {
               return true;
            }

            isEnabled = false;
            return false;
        }

        /// <summary>
        /// Assign a switch a value
        /// </summary>
        /// <param name="switchName">The name of the switch</param>
        /// <param name="isEnabled">The value to assign</param>
        public static void SetSwitch(string switchName, bool isEnabled)
        {
            if (switchName == null)
                throw new ArgumentNullException(nameof(switchName));
            if (switchName.Length == 0)
                throw new ArgumentException(SR.Argument_EmptyName, nameof(switchName));

            if (s_switches == null)
            {
                // Compatibility switches are rarely used. Initialize the Dictionary lazily
                Interlocked.CompareExchange(ref s_switches, new Dictionary<string, bool>(), null);
            }

            lock (s_switches!) // TODO-NULLABLE: Remove ! when compiler specially-recognizes CompareExchange for nullability
            {
                s_switches[switchName] = isEnabled;
            }
        }
    }
}
