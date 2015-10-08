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
        [return: MarshalAs(UnmanagedType.Bool)]
        internal unsafe static extern bool GetLocaleName(string localeName, [Out] StringBuilder value, int valueLength);

        [DllImport(Libraries.GlobalizationInterop, CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal unsafe static extern bool GetLocaleInfoString(string localeName, uint localeStringData, [Out] StringBuilder value, int valueLength);

        [DllImport(Libraries.GlobalizationInterop, CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal unsafe static extern bool GetDefaultLocaleName([Out] StringBuilder value, int valueLength);

        [DllImport(Libraries.GlobalizationInterop, CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal unsafe static extern bool GetLocaleTimeFormat(string localeName, bool shortFormat, [Out] StringBuilder value, int valueLength);

        [DllImport(Libraries.GlobalizationInterop, CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal unsafe static extern bool GetLocaleInfoInt(string localeName, uint localeNumberData, ref int value);

        [DllImport(Libraries.GlobalizationInterop, CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal unsafe static extern bool GetLocaleInfoGroupingSizes(string localeName, uint localeGroupingData, ref int primaryGroupSize, ref int secondaryGroupSize);
    }
}
