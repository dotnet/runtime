// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Runtime;
using System.Text;
using System.Threading;

namespace System
{
    public static partial class AppContext
    {
        // Reading these slots must not trigger AppContext's static constructor.
        private static class ExceptionCallbacks
        {
            internal static nint s_firstChanceException;
            internal static nint s_unhandledException;
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

        // The event add accessor's Interlocked.CompareExchange provides the publication fence.
        internal static unsafe void SetFirstChanceExceptionHandler() =>
            ExceptionCallbacks.s_firstChanceException = (nint)(delegate*<object, void>)&OnFirstChanceExceptionCore;

        internal static unsafe void SetUnhandledExceptionHandler() =>
            ExceptionCallbacks.s_unhandledException = (nint)(delegate*<object, void>)&OnUnhandledException;

        [RuntimeExport("OnFirstChanceException")]
        internal static unsafe void OnFirstChanceException(object e)
        {
            delegate*<object, void> callback = (delegate*<object, void>)Volatile.Read(ref ExceptionCallbacks.s_firstChanceException);
            if (callback != null)
                callback(e);
        }

        private static void OnFirstChanceExceptionCore(object e)
            => OnFirstChanceException((Exception)e, sender: null);

        [RuntimeExport("OnUnhandledException")]
        private static unsafe void OnUnhandledExceptionNative(object e)
        {
            RuntimeExceptionHelpers.SerializeCrashInfo(RhFailFastReason.UnhandledException, (e as Exception)?.Message, e as Exception);

            delegate*<object, void> callback = (delegate*<object, void>)Volatile.Read(ref ExceptionCallbacks.s_unhandledException);
            if (callback != null)
                callback(e);
        }
    }
}
