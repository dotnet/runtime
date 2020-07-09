// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable
namespace System.Diagnostics
{
    internal static partial class TraceListenerHelpers
    {
        private static volatile string? s_processName;

        internal static int GetThreadId()
        {
            return Environment.CurrentManagedThreadId;
        }
    }
}
