// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Logcat
    {
        [DllImport(Libraries.Liblog)]
        private static extern void __android_log_print(LogLevel level, string? tag, string format, string args, IntPtr ptr);

        internal static void AndroidLogPrint(LogLevel level, string? tag, string message) =>
            __android_log_print(level, tag, "%s", message, IntPtr.Zero);

        internal enum LogLevel
        {
            Unknown = 0x00,
            Default = 0x01,
            Verbose = 0x02,
            Debug   = 0x03,
            Info    = 0x04,
            Warn    = 0x05,
            Error   = 0x06,
            Fatal   = 0x07,
            Silent  = 0x08
        }
    }
}
