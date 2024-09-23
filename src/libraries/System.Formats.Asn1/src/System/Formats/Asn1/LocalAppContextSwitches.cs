// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;

namespace System
{
    internal static partial class LocalAppContextSwitches
    {
        private static int s_allowAnySizeOid;
        public static bool AllowAnySizeOid
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => GetCachedSwitchValue("System.Formats.Asn1.AllowAnySizeOid", ref s_allowAnySizeOid);
        }
    }
}
