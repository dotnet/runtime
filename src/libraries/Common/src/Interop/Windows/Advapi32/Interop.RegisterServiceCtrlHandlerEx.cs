// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Advapi32
    {
        public delegate int ServiceControlCallbackEx(int control, int eventType, IntPtr eventData, IntPtr eventContext);

        [GeneratedDllImport(Libraries.Advapi32, EntryPoint = "RegisterServiceCtrlHandlerExW", CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true)]
        public static partial IntPtr RegisterServiceCtrlHandlerEx(string? serviceName, ServiceControlCallbackEx? callback, IntPtr userData);
    }
}
