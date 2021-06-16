// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Threading;

namespace System
{
    public partial class Environment
    {
        public static int CurrentManagedThreadId => Thread.CurrentThread.ManagedThreadId;

        public static extern int ExitCode
        {
            [MethodImplAttribute(MethodImplOptions.InternalCall)]
            get;
            [MethodImplAttribute(MethodImplOptions.InternalCall)]
            set;
        }

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern int GetProcessorCount();

        public static extern int TickCount
        {
            [MethodImplAttribute(MethodImplOptions.InternalCall)]
            get;
        }

        public static extern long TickCount64
        {
            [MethodImplAttribute(MethodImplOptions.InternalCall)]
            get;
        }

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        [DoesNotReturn]
        public static extern void Exit(int exitCode);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        public static extern string[] GetCommandLineArgs();

        [DoesNotReturn]
        public static void FailFast(string? message)
        {
            FailFast(message, null, null);
        }

        [DoesNotReturn]
        public static void FailFast(string? message, Exception? exception)
        {
            FailFast(message, exception, null);
        }

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        [DoesNotReturn]
        public static extern void FailFast(string? message, Exception? exception, string? errorSource);
    }
}
