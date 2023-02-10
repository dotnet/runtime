// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

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
           /* const string ICU_COLLATION_KEYWORD = "@collation=";

            // Basic validation
            if (!IsValidCultureName(realNameBuffer, out var index))
            {
                return false;
            }

            // Replace _ (alternate sort) with @collation= for ICU
            ReadOnlySpan<char> alternateSortName = default;
            if (index > 0)
            {
                alternateSortName = realNameBuffer.AsSpan(index + 1);
                realNameBuffer = string.Concat(realNameBuffer.AsSpan(0, index), ICU_COLLATION_KEYWORD, alternateSortName);
            }*/

            // Get the locale name
            if (!NativeGetLocaleName(realNameBuffer, out _sWindowsName))
            {
                return false; // fail
            }

           /* // Replace the ICU collation keyword with an _
            Debug.Assert(_sWindowsName != null);
            index = _sWindowsName.IndexOf(ICU_COLLATION_KEYWORD, StringComparison.Ordinal);
            if (index >= 0)
            {
                // Use original culture name if alternateSortName is not set, which is possible even if the normalized
                // culture name has "@collation=".
                // "zh-TW-u-co-zhuyin" is a good example. The term "u-co-" means the following part will be the sort name
                // and it will be treated in ICU as "zh-TW@collation=zhuyin".
                _sName = alternateSortName.Length == 0 ? realNameBuffer : string.Concat(_sWindowsName.AsSpan(0, index), "_", alternateSortName);
            }
            else
            {
                _sName = _sWindowsName;
            }
            _sRealName = _sName;

            _iLanguage = LCID;
            if (_iLanguage == 0)
            {
                _iLanguage = CultureInfo.LOCALE_CUSTOM_UNSPECIFIED;
            }
            _bNeutral = TwoLetterISOCountryName.Length == 0;
            // Implement for native
            //_sSpecificCulture = _bNeutral ? IcuLocaleData.GetSpecificCultureName(_sRealName) : _sRealName;
            // Remove the sort from sName unless custom culture
            if (index > 0 && !_bNeutral && !IsCustomCultureId(_iLanguage))
            {
                _sName = _sWindowsName.Substring(0, index);
            }*/
            return true;
        }

        internal static unsafe bool NativeGetLocaleName(string localeName, out string? windowsName)
        {
            // Is this needed for native?
            // Get the locale name from ICU
            char* buffer = stackalloc char[Native_ULOC_FULLNAME_CAPACITY];
            System.Diagnostics.Debug.Write("Globalization NativeGetLocaleName is called");
            System.Diagnostics.Debug.Write("Globalization NativeGetLocaleName is called localeName: " + localeName);
            if (!Interop.Globalization.NativeGetLocaleName(localeName, buffer, Native_ULOC_FULLNAME_CAPACITY))
            {
                windowsName = null;
                return false; // fail
            }

            // Success - use the locale name returned which may be different than realNameBuffer (casing)
            windowsName = new string(buffer); // the name passed to subsequent ICU calls
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
        private unsafe string NativeGetLocaleInfo(string localeName, LocaleStringData type, string? uiCultureName = null)
        {
            Debug.Assert(!GlobalizationMode.UseNls);
            Debug.Assert(localeName != null, "[CultureData.NativeGetLocaleInfo] Expected localeName to be not be null");

            /*switch (type)
            {
                case LocaleStringData.NegativeInfinitySymbol:
                    // not an equivalent in ICU; prefix the PositiveInfinitySymbol with NegativeSign
                    return IcuGetLocaleInfo(localeName, LocaleStringData.NegativeSign) +
                        IcuGetLocaleInfo(localeName, LocaleStringData.PositiveInfinitySymbol);
            }*/

            char* buffer = stackalloc char[Native_ULOC_KEYWORD_AND_VALUES_CAPACITY];
            // this buffer is initialized
            Debug.Write("Globalization NativeGetLocaleInfo is called");
            System.Diagnostics.Debug.Write("Globalization NativeGetLocaleInfo is called localeName: " + localeName);
            System.Diagnostics.Debug.Write("Globalization NativeGetLocaleInfo is called type: " + type.ToString());
            bool result = Interop.Globalization.NativeGetLocaleInfoString(localeName, (uint)type, buffer, Native_ULOC_KEYWORD_AND_VALUES_CAPACITY, uiCultureName);
            System.Diagnostics.Debug.Write("Globalization NativeGetLocaleInfo is called result: " + result.ToString());
            System.Diagnostics.Debug.Write("Globalization NativeGetLocaleInfo is called buffer: " + new string(buffer));
            if (!result)
            {
                // Failed, just use empty string
                Debug.Fail("[CultureData.NativeGetLocaleInfo(LocaleStringData)] Failed");
                return string.Empty;
            }
            return new string(buffer);
        }

        /*private int NativeGetLocaleInfo(LocaleNumberData type)
        {
            Debug.Assert(!GlobalizationMode.UseNls);

            Debug.Assert(_sWindowsName != null, "[CultureData.NativeGetLocaleInfoInt(LocaleNumberData)] Expected _sWindowsName to be populated already");

            switch (type)
            {
                case LocaleNumberData.CalendarType:
                    // returning 0 will cause the first supported calendar to be returned, which is the preferred calendar
                    return 0;
            }


            int value = 0;
            bool result = Interop.Globalization.NativeGetLocaleInfoInt(_sWindowsName, (uint)type, ref value);
            if (!result)
            {
                // Failed, just use 0
                Debug.Fail("[CultureData.NativeGetLocaleInfoInt(LocaleNumberData)] failed");
            }

            return value;
        }*/
    }
}
