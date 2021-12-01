// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;

namespace System
{
    internal static partial class LocalAppContextSwitches
    {
        private static int s_enableUnixSupport;
        public static bool EnableUnixSupport
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return GetCachedSwitchValue(@"System.Drawing.EnableUnixSupport", ref s_enableUnixSupport);
            }
        }
    }
}
