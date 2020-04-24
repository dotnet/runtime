// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Logcat
    {
        [DllImport(Libraries.Liblog)]
        private static extern void __android_log_print (int level, string? tag, string format, string args, IntPtr ptr);

        private const int InfoLevel = 4;

        internal static void LogInfo(string? tag, string message) => __android_log_print(InfoLevel, tag, "%s", message, IntPtr.Zero);
    }
}
