// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Runtime.Versioning;

namespace System.Threading
{
    internal sealed partial class ThreadPoolWorkQueue
    {
        [SupportedOSPlatform("macos")]
        [SupportedOSPlatform("ios")]
        [SupportedOSPlatform("tvos")]
        [SupportedOSPlatform("watchos")]
        private static void DispatchItemWithAutoreleasePool(object workItem, Thread currentThread)
        {
            Debug.Fail("DispatchItemWithAutoreleasePool should only be called on macOS-like platforms.");
        }
    }
}
