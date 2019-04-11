// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable
using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static unsafe partial class Kernel32
    {
        internal const int LOCALE_NAME_MAX_LENGTH = 85;

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        internal static extern int ResolveLocaleName(string lpNameToResolve, char* lpLocaleName, int cchLocaleName);
    }
}
