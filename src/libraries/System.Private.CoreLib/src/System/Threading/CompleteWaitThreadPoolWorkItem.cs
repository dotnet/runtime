// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace System.Threading
{
    /// <summary>
    /// The info for a completed wait on a specific <see cref="RegisteredWaitHandle"/>.
    /// </summary>
    internal sealed class CompleteWaitThreadPoolWorkItem(RegisteredWaitHandle registeredWaitHandle, bool timedOut) : IThreadPoolWorkItem
    {
        void IThreadPoolWorkItem.Execute() => PortableThreadPool.CompleteWait(registeredWaitHandle, timedOut);
    }
}
