// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Runtime;
using System.Text;

namespace System
{
    public static partial class AppContext
    {
        // Reading these slots must not trigger AppContext's static constructor.
        private static unsafe class ExceptionCallbacks
        {
            internal static volatile delegate*<object, void> s_firstChanceException;
            internal static volatile delegate*<object, void> s_unhandledException;
        }

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

        static partial void SetFirstChanceExceptionHandler()
        {
            unsafe
            {
                ExceptionCallbacks.s_firstChanceException = &OnFirstChanceExceptionCore;
            }
        }

        static partial void SetUnhandledExceptionHandler()
        {
            unsafe
            {
                ExceptionCallbacks.s_unhandledException = &OnUnhandledExceptionCore;
            }
        }

        [RuntimeExport("OnFirstChanceException")]
        internal static unsafe void OnFirstChanceException(object e)
        {
            delegate*<object, void> callback = ExceptionCallbacks.s_firstChanceException;
            if (callback != null)
                callback(e);
        }

        private static void OnFirstChanceExceptionCore(object e)
            => OnFirstChanceException((Exception)e);

        [RuntimeExport("OnUnhandledException")]
        internal static unsafe void OnUnhandledException(object e)
        {
            RuntimeExceptionHelpers.SerializeCrashInfo(RhFailFastReason.UnhandledException, (e as Exception)?.Message, e as Exception);

            delegate*<object, void> callback = ExceptionCallbacks.s_unhandledException;
            if (callback != null)
                callback(e);
        }
    }
}
