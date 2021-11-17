// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Globalization
    {
        [GeneratedDllImport(Libraries.GlobalizationNative, EntryPoint = "GlobalizationNative_GetLocaleName", CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static unsafe partial bool GetLocaleName(string localeName, char* value, int valueLength);

        [GeneratedDllImport(Libraries.GlobalizationNative, EntryPoint = "GlobalizationNative_GetLocaleInfoString", CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static unsafe partial bool GetLocaleInfoString(string localeName, uint localeStringData, char* value, int valueLength, string? uiLocaleName = null);

        [GeneratedDllImport(Libraries.GlobalizationNative, EntryPoint = "GlobalizationNative_GetDefaultLocaleName", CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static unsafe partial bool GetDefaultLocaleName(char* value, int valueLength);

        [GeneratedDllImport(Libraries.GlobalizationNative, EntryPoint = "GlobalizationNative_IsPredefinedLocale", CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool IsPredefinedLocale(string localeName);

        [GeneratedDllImport(Libraries.GlobalizationNative, EntryPoint = "GlobalizationNative_GetLocaleTimeFormat", CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static unsafe partial bool GetLocaleTimeFormat(string localeName, bool shortFormat, char* value, int valueLength);

        [GeneratedDllImport(Libraries.GlobalizationNative, EntryPoint = "GlobalizationNative_GetLocaleInfoInt", CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool GetLocaleInfoInt(string localeName, uint localeNumberData, ref int value);

        [GeneratedDllImport(Libraries.GlobalizationNative, EntryPoint = "GlobalizationNative_GetLocaleInfoGroupingSizes", CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool GetLocaleInfoGroupingSizes(string localeName, uint localeGroupingData, ref int primaryGroupSize, ref int secondaryGroupSize);

        [GeneratedDllImport(Libraries.GlobalizationNative, EntryPoint = "GlobalizationNative_GetLocales", CharSet = CharSet.Unicode)]
        internal static partial int GetLocales([Out] char[]? value, int valueLength);
    }
}
