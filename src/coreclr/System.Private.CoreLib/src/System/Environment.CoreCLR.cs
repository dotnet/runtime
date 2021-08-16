// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;

namespace System
{
    public static partial class Environment
    {
        public static extern int CurrentManagedThreadId
        {
            [MethodImpl(MethodImplOptions.InternalCall)]
            get;
        }

        // Terminates this process with the given exit code.
        [DllImport(RuntimeHelpers.QCall, CharSet = CharSet.Unicode)]
        [DoesNotReturn]
        private static extern void _Exit(int exitCode);

        [DoesNotReturn]
        public static void Exit(int exitCode) => _Exit(exitCode);

        public static extern int ExitCode
        {
            [MethodImpl(MethodImplOptions.InternalCall)]
            get;
            [MethodImpl(MethodImplOptions.InternalCall)]
            set;
        }

        // Note: The CLR's Watson bucketization code looks at the caller of the FCALL method
        // to assign blame for crashes.  Don't mess with this, such as by making it call
        // another managed helper method, unless you consult with some CLR Watson experts.
        [DoesNotReturn]
        [MethodImpl(MethodImplOptions.InternalCall)]
        public static extern void FailFast(string? message);

        // This overload of FailFast will allow you to specify the exception object
        // whose bucket details *could* be used when undergoing the failfast process.
        // To be specific:
        //
        // 1) When invoked from within a managed EH clause (fault/finally/catch),
        //    if the exception object is preallocated, the runtime will try to find its buckets
        //    and use them. If the exception object is not preallocated, it will use the bucket
        //    details contained in the object (if any).
        //
        // 2) When invoked from outside the managed EH clauses (fault/finally/catch),
        //    if the exception object is preallocated, the runtime will use the callsite's
        //    IP for bucketing. If the exception object is not preallocated, it will use the bucket
        //    details contained in the object (if any).
        [DoesNotReturn]
        [MethodImpl(MethodImplOptions.InternalCall)]
        public static extern void FailFast(string? message, Exception? exception);

        [DoesNotReturn]
        [MethodImpl(MethodImplOptions.InternalCall)]
        public static extern void FailFast(string? message, Exception? exception, string? errorMessage);

        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern string[] GetCommandLineArgsNative();

        public static string[] GetCommandLineArgs()
        {
            // There are multiple entry points to a hosted app. The host could
            // use ::ExecuteAssembly() or ::CreateDelegate option:
            //
            // ::ExecuteAssembly() -> In this particular case, the runtime invokes the main
            // method based on the arguments set by the host, and we return those arguments
            //
            // ::CreateDelegate() -> In this particular case, the host is asked to create a
            // delegate based on the appDomain, assembly and methodDesc passed to it.
            // which the caller uses to invoke the method. In this particular case we do not have
            // any information on what arguments would be passed to the delegate.
            // So our best bet is to simply use the commandLine that was used to invoke the process.
            // in case it is present.

            return s_commandLineArgs != null ?
                (string[])s_commandLineArgs.Clone() :
                GetCommandLineArgsNative();
        }

        [DllImport(RuntimeHelpers.QCall, CharSet = CharSet.Unicode)]
        private static extern int GetProcessorCount();

        // Used by VM
        internal static string? GetResourceStringLocal(string key) => SR.GetResourceString(key);

        /// <summary>Gets the number of milliseconds elapsed since the system started.</summary>
        /// <value>A 32-bit signed integer containing the amount of time in milliseconds that has passed since the last time the computer was started.</value>
        public static extern int TickCount
        {
            [MethodImpl(MethodImplOptions.InternalCall)]
            get;
        }

        /// <summary>Gets the number of milliseconds elapsed since the system started.</summary>
        /// <value>A 64-bit signed integer containing the amount of time in milliseconds that has passed since the last time the computer was started.</value>
        public static extern long TickCount64
        {
            [MethodImpl(MethodImplOptions.InternalCall)]
            get;
        }
    }
}
