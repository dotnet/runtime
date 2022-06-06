// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*=============================================================================
**
**
**
** Purpose: Class for creating and managing a threadpool
**
**
=============================================================================*/

#pragma warning disable 0420

using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace System.Threading
{
    public static partial class ThreadPool
    {
        internal static void ReportThreadStatus(bool isWorking)
        {
        }

        private static long PendingUnmanagedWorkItemCount => 0;
    }
}
