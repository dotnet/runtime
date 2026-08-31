// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Tracing;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Loader;

namespace System
{
    internal static partial class StartupHookProvider
    {
        [UnmanagedCallersOnly]
        private static unsafe void ManagedStartup(char* pDiagnosticStartupHooks, Exception* pException)
        {
            try
            {
                if (IsSupported)
                    ProcessStartupHooks(new string(pDiagnosticStartupHooks));
            }
            catch (Exception ex)
            {
                *pException = ex;
            }
        }
    }
}
