// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Runtime.InteropServices;

namespace System.Globalization
{
    internal sealed partial class CultureData
    {
        // Native constants, check if we need this for native
        private const int Native_ULOC_KEYWORD_AND_VALUES_CAPACITY = 100; // max size of keyword or value
        private const int Native_ULOC_FULLNAME_CAPACITY = 157;           // max size of locale name

                /// <summary>
        /// This method uses the sRealName field (which is initialized by the constructor before this is called) to
        /// initialize the rest of the state of CultureData based on the underlying OS globalization library.
        /// </summary>
        private bool InitNativeCultureDataCore()
        {
            Debug.Assert(_sRealName != null);
            Debug.Assert(!GlobalizationMode.Invariant);
            string realNameBuffer = _sRealName;

            // Get the locale name
            NativeGetLocaleName(realNameBuffer, out _sWindowsName);
            return true;
        }

        internal static unsafe bool NativeGetLocaleName(string localeName, out string? windowsName)
        {
            System.Diagnostics.Debug.Write("Globalization NativeGetLocaleName is called localeName: " + localeName);
            windowsName = Interop.Globalization.NativeGetLocaleName(localeName, Native_ULOC_FULLNAME_CAPACITY);
            return true;
        }

        private string NativeGetLocaleInfo(LocaleStringData type, string? uiCultureName = null)
        {
            Debug.Assert(!GlobalizationMode.Invariant);
            Debug.Assert(!GlobalizationMode.UseNls);
            Debug.Assert(_sWindowsName != null, "[CultureData.NativeGetLocaleInfo] Expected _sWindowsName to be populated already");
            return NativeGetLocaleInfo(_sWindowsName, type, uiCultureName);
        }

        // For LOCALE_SPARENT we need the option of using the "real" name (forcing neutral names) instead of the
        // "windows" name, which can be specific for downlevel (< windows 7) os's.
        private static unsafe string NativeGetLocaleInfo(string localeName, LocaleStringData type, string? uiCultureName = null)
        {
            Debug.Assert(!GlobalizationMode.UseNls);
            Debug.Assert(localeName != null, "[CultureData.NativeGetLocaleInfo] Expected localeName to be not be null");

            System.Diagnostics.Debug.Write("Globalization NativeGetLocaleInfo is called localeName: " + localeName);
            string result = Interop.Globalization.NativeGetLocaleInfoString(localeName, (uint)type, Native_ULOC_KEYWORD_AND_VALUES_CAPACITY, uiCultureName);
            System.Diagnostics.Debug.Write("Globalization NativeGetLocaleInfo is called result: " + result);
            return result;
        }
    }
}
