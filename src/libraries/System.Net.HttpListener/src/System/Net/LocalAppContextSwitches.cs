// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;

namespace System
{
    internal static partial class LocalAppContextSwitches
    {
        private static int s_enableKernelResponseBuffering;
        internal static bool EnableKernelResponseBuffering
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => GetCachedSwitchValue("System.Net.HttpListener.EnableKernelResponseBuffering", ref s_enableKernelResponseBuffering);
        }
    }
}
