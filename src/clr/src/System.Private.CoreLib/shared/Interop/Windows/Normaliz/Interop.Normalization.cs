// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable
using System;
using System.Runtime.InteropServices;

internal partial class Interop
{
    internal partial class Normaliz
    {
        [DllImport("Normaliz.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        internal static extern bool IsNormalizedString(int normForm, string source, int length);

        [DllImport("Normaliz.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        internal static extern int NormalizeString(
                                        int normForm,
                                        string source,
                                        int sourceLength,
                                        [System.Runtime.InteropServices.OutAttribute()]
                                        char[]? destination,
                                        int destinationLength);
    }
}
