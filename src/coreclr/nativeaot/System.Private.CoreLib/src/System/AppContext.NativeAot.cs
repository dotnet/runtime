// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Internal.Runtime.Augments;
using System.Collections.Generic;
using System.IO;
using System.Runtime;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Text;

namespace System
{
    public static partial class AppContext
    {
        private static unsafe Dictionary<string, object?> InitializeDataStore()
        {
            uint count = RuntimeImports.RhGetKnobValues(out byte** keys, out byte** values);

            var dataStore = new Dictionary<string, object?>((int)count);
            for (int i = 0; i < count; i++)
            {
                dataStore.Add(
                    Encoding.UTF8.GetString(keys[i], string.strlen(keys[i])),
                    Encoding.UTF8.GetString(values[i], string.strlen(values[i])));
            }

            return dataStore;
        }

        [RuntimeExport("OnFirstChanceException")]
        internal static void OnFirstChanceException(object e)
        {
            FirstChanceException?.Invoke(/* AppDomain */ null, new FirstChanceExceptionEventArgs((Exception)e));
        }

        [RuntimeExport("OnUnhandledException")]
        internal static void OnUnhandledException(object e)
        {
            UnhandledException?.Invoke(/* AppDomain */ null, new UnhandledExceptionEventArgs(e, true));
        }

        private static unsafe string GetRuntimeModulePath()
        {
            return RuntimeModulePathHolder.RuntimeModulePath;
        }

        internal static class RuntimeModulePathHolder
        {
            private static string? s_workingDirectoryAtStartup;
            private static string WorkingDirectoryAtStartup
            {
                // In case a user's module initializer tries to get AppContext.BaseDirectory
                // before our module initializer has run, we need to set up the getter to initialize
                // the value.
                get => s_workingDirectoryAtStartup ??= Environment.CurrentDirectory;
            }

            #pragma warning disable CA2255 // The 'ModuleInitializer' attribute is only intended to be used in application code or advanced source generator scenarios
                                           // We need to capture this value at startup in case the user changes Environment.CurrentDirectory
                                           // before the first execution of AppContext.BaseDirectory.
            [ModuleInitializer]
            #pragma warning restore CA2255
            public static void Initialize()
            {
                s_workingDirectoryAtStartup = Environment.CurrentDirectory;
            }

            public static string RuntimeModulePath { get; } = GetRuntimeModulePath();

            private static unsafe string GetRuntimeModulePath()
            {
                // We aren't going to call this method, we just need an address that we know is in this module.
                // As this code is NativeAOT only, we know that this method will be AOT compiled into the executable,
                // so the entry point address will be in the module.
                void* ip = (void*)(delegate*<string>)&GetRuntimeModulePath;
                if (RuntimeAugments.TryGetFullPathToApplicationModule((nint)ip, out _) is string modulePath)
                {
                    if (!Path.IsPathRooted(modulePath))
                    {
                        // If the path is not rooted, it is relative to the working directory.
                        // We need to make it absolute.
                        modulePath = Path.Combine(WorkingDirectoryAtStartup, modulePath);
                        if (!Path.Exists(modulePath))
                        {
                            // If this path doesn't exist, that means that this module was loaded
                            // from a different relative path using something like LD_LIBRARY_PATH.
                            // In this case, we'll say we can't get the module path and return the process path.
                            return Environment.ProcessPath;
                        }
                    }
                    return modulePath;
                }

                // If this method isn't in a dynamically loaded module,
                // then it's in the executable. In that case, we can use the process path.
                return Environment.ProcessPath;
            }
        }
    }
}
