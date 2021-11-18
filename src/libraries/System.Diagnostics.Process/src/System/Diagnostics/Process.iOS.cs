// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.Versioning;

namespace System.Diagnostics
{
    public partial class Process : IDisposable
    {
        [UnsupportedOSPlatform("ios")]
        [UnsupportedOSPlatform("tvos")]
        public void Kill(bool entireProcessTree)
        {
            throw new PlatformNotSupportedException();
        }

        /// <summary>
        /// Returns all immediate child processes.
        /// </summary>
        private IReadOnlyList<Process> GetChildProcesses(Process[]? processes = null)
        {
            throw new PlatformNotSupportedException();
        }

        private static bool IsProcessInvalidException(Exception e) =>
            // InvalidOperationException signifies conditions such as the process already being dead.
            // Win32Exception signifies issues such as insufficient permissions to get details on the process.
            // In either case, the predicate couldn't be applied so return the fallback result.
            e is InvalidOperationException || e is Win32Exception;
    }
}
