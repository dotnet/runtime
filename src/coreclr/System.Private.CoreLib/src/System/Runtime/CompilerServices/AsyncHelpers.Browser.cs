// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace System.Runtime.CompilerServices
{
    public static partial class AsyncHelpers
    {
        [DynamicDependency(nameof(MainWrapper))]
        [DynamicDependency(nameof(MainWrapperVoid))]
        static AsyncHelpers()
        {
        }

        internal static void MainWrapper(object exitCodeTask)
        {
            var task = (Task<int>)exitCodeTask;
            task.ContinueWith(t =>
            {
                Internal.Console.WriteLine("MainWrapper continuation A");
                if (t.IsFaulted)
                {
                    Internal.Console.WriteLine("MainWrapper continuation B");
                    SystemJS_RejectMainPromise(t.Exception.Message);
                }
                else
                {
                    Internal.Console.WriteLine("MainWrapper continuation C");
                    SystemJS_ResolveMainPromise(t.Result);
                }
            }, TaskScheduler.Default);
            Internal.Console.WriteLine("MainWrapper completed");
        }

        internal static void MainWrapperVoid(object exitCodeTask)
        {
            var task = (Task)exitCodeTask;
            task.ContinueWith(t =>
            {
                if (t.IsFaulted)
                {
                    SystemJS_RejectMainPromise(t.Exception.Message);
                }
                else
                {
                    SystemJS_ResolveMainPromise(0);
                }
            }, TaskScheduler.Default);
            Internal.Console.WriteLine("MainWrapper completed");
        }

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "SystemJS_RejectMainPromise")]
        private static unsafe partial void SystemJS_RejectMainPromise([MarshalAs(UnmanagedType.LPWStr)] string pMessage);

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "SystemJS_ResolveMainPromise")]
        private static partial void SystemJS_ResolveMainPromise(int exitCode);
    }
}
