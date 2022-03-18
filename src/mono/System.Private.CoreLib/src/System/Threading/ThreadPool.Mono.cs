// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Diagnostics;

namespace System.Threading
{
    public static partial class ThreadPool
    {
        [Conditional("unnecessary")]
        internal static void ReportThreadStatus(bool isWorking)
        {
        }

        private static long PendingUnmanagedWorkItemCount => 0;
    }
}
