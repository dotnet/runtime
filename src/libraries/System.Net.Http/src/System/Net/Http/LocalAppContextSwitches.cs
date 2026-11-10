// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;

namespace System
{
    internal static partial class LocalAppContextSwitches
    {
        private static int s_usePortInSpn;
        internal static bool UsePortInSpn
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => GetCachedSwitchValue("System.Net.Http.UsePortInSpn", "DOTNET_SYSTEM_NET_HTTP_USEPORTINSPN", ref s_usePortInSpn);
        }
    }
}
