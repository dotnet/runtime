// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class IpHlpApi
    {
#if !uap
        [DllImport(Interop.Libraries.IpHlpApi, SetLastError = true)]
        internal extern static uint if_nametoindex(string name);
#else
        internal static uint if_nametoindex(string name) => 0;
#endif
    }
}
