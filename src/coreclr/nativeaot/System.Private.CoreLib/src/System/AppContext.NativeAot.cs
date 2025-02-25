// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Internal.Runtime.Augments;
using System.Collections.Generic;
using System.Runtime;
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
            // We aren't going to call this method, we just need an address that we know is in this module.
            // As this code is NativeAOT only, we know that this method will be AOT compiled into the executable,
            // so the entry point address will be in the module.
            void* ip = (void*)(delegate*<string>)&GetRuntimeModulePath;
            if (RuntimeAugments.TryGetFullPathToApplicationModule((nint)ip, out _) is string modulePath)
            {
                return modulePath;
            }

            // If this method isn't in a dynamically loaded module,
            // then it's in the executable. In that case, we can use the process path.
            return Environment.ProcessPath;
        }
    }
}
