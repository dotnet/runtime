// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;
using System.Text;

internal static partial class Interop
{
    internal static partial class GlobalizationInterop
    {
        [DllImport(Libraries.GlobalizationInterop, CharSet = CharSet.Unicode)]
        internal unsafe static extern int GetLocaleName(string localeName, StringBuilder value, int valueLength);

        [DllImport(Libraries.GlobalizationInterop, CharSet = CharSet.Unicode)]
        internal unsafe static extern int GetLocaleInfoString(string localeName, uint localeStringData, StringBuilder value, int valueLength);

        [DllImport(Libraries.GlobalizationInterop, CharSet = CharSet.Unicode)]
        internal unsafe static extern int GetLocaleInfoInt(string localeName, uint localeNumberData, ref int value);
    }
}
