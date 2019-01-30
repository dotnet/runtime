// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.Win32;

namespace System
{
    public static partial class Environment
    {
        public static int CurrentManagedThreadId => Thread.CurrentThread.ManagedThreadId;

        // Terminates this process with the given exit code.
        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        private static extern void _Exit(int exitCode);

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
        [MethodImpl(MethodImplOptions.InternalCall)]
        public static extern void FailFast(string message);

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
        [MethodImpl(MethodImplOptions.InternalCall)]
        public static extern void FailFast(string message, Exception exception);

        [MethodImpl(MethodImplOptions.InternalCall)]
        public static extern void FailFast(string message, Exception exception, string errorMessage);

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

        public static extern bool HasShutdownStarted
        {
            [MethodImpl(MethodImplOptions.InternalCall)]
            get;
        }

        public static int ProcessorCount => GetProcessorCount();

        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        private static extern int GetProcessorCount();

        // If you change this method's signature then you must change the code that calls it
        // in excep.cpp and probably you will have to visit mscorlib.h to add the new signature
        // as well as metasig.h to create the new signature type
        internal static string GetResourceStringLocal(string key) => SR.GetResourceString(key);

        public static string StackTrace
        {
            [MethodImpl(MethodImplOptions.NoInlining)] // Prevent inlining from affecting where the stacktrace starts
            get => new StackTrace(true).ToString(System.Diagnostics.StackTrace.TraceFormat.Normal);
        }

        public static extern int TickCount
        {
            [MethodImpl(MethodImplOptions.InternalCall)]
            get;
        }

#if !FEATURE_PAL
        internal static bool IsWindows8OrAbove => WindowsVersion.IsWindows8OrAbove;

        // Seperate type so a .cctor is not created for Enviroment which then would be triggered during startup
        private static class WindowsVersion
        {
            // Cache the value in readonly static that can be optimized out by the JIT
            internal readonly static bool IsWindows8OrAbove = GetIsWindows8OrAbove();

            private static bool GetIsWindows8OrAbove()
            {
                ulong conditionMask = Win32Native.VerSetConditionMask(0, Win32Native.VER_MAJORVERSION, Win32Native.VER_GREATER_EQUAL);
                conditionMask = Win32Native.VerSetConditionMask(conditionMask, Win32Native.VER_MINORVERSION, Win32Native.VER_GREATER_EQUAL);
                conditionMask = Win32Native.VerSetConditionMask(conditionMask, Win32Native.VER_SERVICEPACKMAJOR, Win32Native.VER_GREATER_EQUAL);
                conditionMask = Win32Native.VerSetConditionMask(conditionMask, Win32Native.VER_SERVICEPACKMINOR, Win32Native.VER_GREATER_EQUAL);

                // Windows 8 version is 6.2
                var version = new Win32Native.OSVERSIONINFOEX();
                unsafe
                {
                    version.dwOSVersionInfoSize = sizeof(Win32Native.OSVERSIONINFOEX);
                }
                version.dwMajorVersion = 6;
                version.dwMinorVersion = 2;
                version.wServicePackMajor = 0;
                version.wServicePackMinor = 0;

                return Win32Native.VerifyVersionInfoW(ref version,
                    Win32Native.VER_MAJORVERSION | Win32Native.VER_MINORVERSION | Win32Native.VER_SERVICEPACKMAJOR | Win32Native.VER_SERVICEPACKMINOR,
                    conditionMask);
            }
        }
#endif

#if FEATURE_COMINTEROP
        // Seperate type so a .cctor is not created for Enviroment which then would be triggered during startup
        private static class WinRT
        {
            // Cache the value in readonly static that can be optimized out by the JIT
            public readonly static bool IsSupported = WinRTSupported();
        }

        // Does the current version of Windows have Windows Runtime suppport?
        internal static bool IsWinRTSupported => WinRT.IsSupported;

        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool WinRTSupported();
#endif // FEATURE_COMINTEROP
    }
}
