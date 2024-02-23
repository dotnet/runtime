// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Globalization
    {
        [LibraryImport(Libraries.GlobalizationNative, EntryPoint = "GlobalizationNative_GetDefaultLocaleNameNative", StringMarshalling = StringMarshalling.Utf8)]
        internal static partial string GetDefaultLocaleNameNative();

        [LibraryImport(Libraries.GlobalizationNative, EntryPoint = "GlobalizationNative_GetLocaleInfoStringNative", StringMarshalling = StringMarshalling.Utf8)]
        internal static partial string GetLocaleInfoStringNative(string localeName, uint localeStringData, string? uiLocaleName = null);

        [LibraryImport(Libraries.GlobalizationNative, EntryPoint = "GlobalizationNative_GetLocaleInfoIntNative", StringMarshalling = StringMarshalling.Utf8)]
        internal static partial int GetLocaleInfoIntNative(string localeName, uint localeNumberData);

        [LibraryImport(Libraries.GlobalizationNative, EntryPoint = "GlobalizationNative_GetLocaleInfoPrimaryGroupingSizeNative", StringMarshalling = StringMarshalling.Utf8)]
        internal static partial int GetLocaleInfoPrimaryGroupingSizeNative(string localeName, uint localeGroupingData);

        [LibraryImport(Libraries.GlobalizationNative, EntryPoint = "GlobalizationNative_GetLocaleInfoSecondaryGroupingSizeNative", StringMarshalling = StringMarshalling.Utf8)]
        internal static partial int GetLocaleInfoSecondaryGroupingSizeNative(string localeName, uint localeGroupingData);

        [LibraryImport(Libraries.GlobalizationNative, EntryPoint = "GlobalizationNative_GetLocaleNameNative", StringMarshalling = StringMarshalling.Utf8)]
        internal static partial string GetLocaleNameNative(string localeName);

        [LibraryImport(Libraries.GlobalizationNative, EntryPoint = "GlobalizationNative_GetLocalesNative", StringMarshalling = StringMarshalling.Utf16)]
        internal static partial int GetLocalesNative([Out] char[]? value, int valueLength);

        [LibraryImport(Libraries.GlobalizationNative, EntryPoint = "GlobalizationNative_GetLocaleTimeFormatNative", StringMarshalling = StringMarshalling.Utf8)]
        internal static partial string GetLocaleTimeFormatNative(string localeName, [MarshalAs(UnmanagedType.Bool)] bool shortFormat);

        [LibraryImport(Libraries.GlobalizationNative, EntryPoint = "GlobalizationNative_IsPredefinedLocaleNative", StringMarshalling = StringMarshalling.Utf8)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool IsPredefinedLocaleNative(string localeName);
    }
}
