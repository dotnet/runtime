// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;
using System.Runtime.InteropServices;

namespace System.Runtime.CompilerServices
{
    public static partial class AsyncHelpers
    {
        public static void HandleAsyncEntryPoint(Task task)
        {
            task.ContinueWith(t =>
            {
                if (t.IsCanceled)
                {
                    CancelMainPromise();
                }
                else if (t.IsFaulted)
                {
                    RejectMainPromise(t.Exception);
                }
                else
                {
                    SystemJS_ResolveMainPromise(0);
                }
            }, TaskScheduler.Default);
        }

        public static int HandleAsyncEntryPoint(Task<int> task)
        {
            task.ContinueWith(t =>
            {
                if (t.IsCanceled)
                {
                    CancelMainPromise();
                }
                else if (t.IsFaulted)
                {
                    RejectMainPromise(t.Exception);
                }
                else
                {
                    SystemJS_ResolveMainPromise(t.Result);
                }
            }, TaskScheduler.Default);
            // dummy exit code, real exit code will be passed via SystemJS_ResolveMainPromise
            return 0x0BADF00D;
        }

        private static void CancelMainPromise()
        {
            string message = "Task was canceled";
            SystemJS_RejectMainPromise(message, message.Length, string.Empty, 0);
        }

        private static void RejectMainPromise(Exception ex)
        {
            Exception inner = ex.InnerException ?? ex;
            string message = inner.GetType().Name + ": " + (inner.Message ?? "");
            string stackTrace = inner.StackTrace ?? "";
            SystemJS_RejectMainPromise(message, message.Length, stackTrace, stackTrace.Length);
        }

        [LibraryImport(RuntimeHelpers.QCall)]
        private static unsafe partial void SystemJS_RejectMainPromise(
            [MarshalAs(UnmanagedType.LPWStr)] string pMessage, int messageLength,
            [MarshalAs(UnmanagedType.LPWStr)] string pStackTrace, int stackTraceLength);

        [LibraryImport(RuntimeHelpers.QCall)]
        private static partial void SystemJS_ResolveMainPromise(int exitCode);
    }
}
