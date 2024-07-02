// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace System.Threading.Tasks
{
    internal static class LoggingExtensions
    {
        public static string GetMethodName(this Delegate @delegate)
        {
            DiagnosticMethodInfo? dmi = DiagnosticMethodInfo.Create(@delegate);
            return dmi?.Name ?? "<unknown>";
        }
    }
}
