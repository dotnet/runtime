// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

internal static partial class Interop
{
    internal static partial class Globalization
    {
#if MONO
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
#else
        [DllImport(Libraries.GlobalizationNative, CharSet = CharSet.Unicode, EntryPoint = "GlobalizationNative_GetLocaleName")]
        [return: MarshalAs(UnmanagedType.Bool)]
#endif
        internal static extern unsafe bool GetLocaleName(string localeName, char* value, int valueLength);
#if MONO
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
#else
        [DllImport(Libraries.GlobalizationNative, CharSet = CharSet.Unicode, EntryPoint = "GlobalizationNative_GetLocaleInfoString")]
        [return: MarshalAs(UnmanagedType.Bool)]
#endif
        internal static extern unsafe bool GetLocaleInfoString(string localeName, uint localeStringData, char* value, int valueLength);

#if MONO
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
#else
        [DllImport(Libraries.GlobalizationNative, CharSet = CharSet.Unicode, EntryPoint = "GlobalizationNative_GetDefaultLocaleName")]
        [return: MarshalAs(UnmanagedType.Bool)]
#endif
        internal static extern unsafe bool GetDefaultLocaleName(char* value, int valueLength);

#if MONO
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
#else
        [DllImport(Libraries.GlobalizationNative, CharSet = CharSet.Unicode, EntryPoint = "GlobalizationNative_IsPredefinedLocale")]
        [return: MarshalAs(UnmanagedType.Bool)]
#endif
        internal static extern unsafe bool IsPredefinedLocale(string localeName);

#if MONO
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
#else
        [DllImport(Libraries.GlobalizationNative, CharSet = CharSet.Unicode, EntryPoint = "GlobalizationNative_GetLocaleTimeFormat")]
        [return: MarshalAs(UnmanagedType.Bool)]
#endif
        internal static extern unsafe bool GetLocaleTimeFormat(string localeName, bool shortFormat, char* value, int valueLength);
#if MONO
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
#else
        [DllImport(Libraries.GlobalizationNative, CharSet = CharSet.Unicode, EntryPoint = "GlobalizationNative_GetLocaleInfoInt")]
        [return: MarshalAs(UnmanagedType.Bool)]
#endif
        internal static extern bool GetLocaleInfoInt(string localeName, uint localeNumberData, ref int value);

#if MONO
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
#else
        [DllImport(Libraries.GlobalizationNative, CharSet = CharSet.Unicode, EntryPoint = "GlobalizationNative_GetLocaleInfoGroupingSizes")]
        [return: MarshalAs(UnmanagedType.Bool)]
#endif
        internal static extern bool GetLocaleInfoGroupingSizes(string localeName, uint localeGroupingData, ref int primaryGroupSize, ref int secondaryGroupSize);
#if MONO
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
#else
        [DllImport(Libraries.GlobalizationNative, CharSet = CharSet.Unicode, EntryPoint = "GlobalizationNative_GetLocales")]
#endif
        internal static extern int GetLocales(char[]? value, int valueLength);
    }
}
