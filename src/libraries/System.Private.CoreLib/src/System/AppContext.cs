// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Tracing;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Runtime.Loader;
using System.Runtime.Versioning;
using System.Threading;

namespace System
{
    public static partial class AppContext
    {
        private static Dictionary<string, object?>? s_dataStore;
        private static Dictionary<string, bool>? s_switches;
        private static string? s_defaultBaseDirectory;

        public static string BaseDirectory =>
            // The value of APP_CONTEXT_BASE_DIRECTORY key has to be a string and it is not allowed to be any other type.
            // Otherwise the caller will get invalid cast exception
            GetData("APP_CONTEXT_BASE_DIRECTORY") as string ??
            (s_defaultBaseDirectory ??= GetBaseDirectoryCore());

        public static string? TargetFrameworkName =>
            // The Target framework is not the framework that the process is actually running on.
            // It is the value read from the TargetFrameworkAttribute on the .exe that started the process.
            Assembly.GetEntryAssembly()?.GetCustomAttribute<TargetFrameworkAttribute>()?.FrameworkName;

        public static object? GetData(string name)
        {
            ArgumentNullException.ThrowIfNull(name);

            if (s_dataStore == null)
                return null;

            object? data;
            lock (s_dataStore)
            {
                s_dataStore.TryGetValue(name, out data);
            }
            return data;
        }

        /// <summary>
        /// Sets the value of the named data element assigned to the current application domain.
        /// </summary>
        /// <param name="name">The name of the data element</param>
        /// <param name="data">The value of <paramref name="name"/></param>
        /// <exception cref="ArgumentNullException">If <paramref name="name"/> is <see langword="null"/></exception>
        public static void SetData(string name, object? data)
        {
            ArgumentNullException.ThrowIfNull(name);

            if (s_dataStore == null)
            {
                Interlocked.CompareExchange(ref s_dataStore, new Dictionary<string, object?>(), null);
            }

            lock (s_dataStore)
            {
                s_dataStore[name] = data;
            }
        }

#pragma warning disable CS0067 // events raised by the VM
        [field: DynamicDependency(DynamicallyAccessedMemberTypes.PublicConstructors, typeof(UnhandledExceptionEventArgs))]
        public static event UnhandledExceptionEventHandler? UnhandledException;

        [field: DynamicDependency(DynamicallyAccessedMemberTypes.PublicConstructors, typeof(FirstChanceExceptionEventArgs))]
        public static event EventHandler<FirstChanceExceptionEventArgs>? FirstChanceException;
#pragma warning restore CS0067

        public static event EventHandler? ProcessExit;

        internal static void OnProcessExit()
        {
            AssemblyLoadContext.OnProcessExit();
            if (EventSource.IsSupported)
            {
                EventListener.DisposeOnShutdown();
            }

            ProcessExit?.Invoke(AppDomain.CurrentDomain, EventArgs.Empty);
        }

        /// <summary>
        /// Try to get the value of the switch.
        /// </summary>
        /// <param name="switchName">The name of the switch</param>
        /// <param name="isEnabled">A variable where to place the value of the switch</param>
        /// <returns>A return value of true represents that the switch was set and <paramref name="isEnabled"/> contains the value of the switch</returns>
        public static bool TryGetSwitch(string switchName, out bool isEnabled)
        {
            ArgumentException.ThrowIfNullOrEmpty(switchName);

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
            ArgumentException.ThrowIfNullOrEmpty(switchName);

            if (s_switches == null)
            {
                // Compatibility switches are rarely used. Initialize the Dictionary lazily
                Interlocked.CompareExchange(ref s_switches, new Dictionary<string, bool>(), null);
            }

            lock (s_switches)
            {
                s_switches[switchName] = isEnabled;
            }
        }

#if !NATIVEAOT
        internal static unsafe void Setup(char** pNames, char** pValues, int count)
        {
            Debug.Assert(s_dataStore == null, "s_dataStore is not expected to be inited before Setup is called");
            s_dataStore = new Dictionary<string, object?>(count);
            for (int i = 0; i < count; i++)
            {
                s_dataStore.Add(new string(pNames[i]), new string(pValues[i]));
            }
        }
#endif
    }
}
