// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;

namespace System.Globalization
{
    internal partial class CultureData
    {
        private const string ICU_COLLATION_KEYWORD = "@collation=";

        /// <summary>
        /// This method uses the sRealName field (which is initialized by the constructor before this is called) to
        /// initialize the rest of the state of CultureData based on the underlying OS globalization library.
        /// </summary>
        private bool InitCultureDataCore()
        {
            Debug.Assert(_sRealName != null);
            Debug.Assert(!GlobalizationMode.Invariant);

            string realNameBuffer = _sRealName;

            // Basic validation
            if (realNameBuffer.Contains('@'))
            {
                return false; // don't allow ICU variants to come in directly
            }

            // Replace _ (alternate sort) with @collation= for ICU
            ReadOnlySpan<char> alternateSortName = default;
            int index = realNameBuffer.IndexOf('_');
            if (index > 0)
            {
                if (index >= (realNameBuffer.Length - 1) // must have characters after _
                    || realNameBuffer.IndexOf('_', index + 1) >= 0) // only one _ allowed
                {
                    return false; // fail
                }
                alternateSortName = realNameBuffer.AsSpan(index + 1);
                realNameBuffer = string.Concat(realNameBuffer.AsSpan(0, index), ICU_COLLATION_KEYWORD, alternateSortName);
            }

            // Get the locale name from ICU
            if (!GetLocaleName(realNameBuffer, out _sWindowsName))
            {
                return false; // fail
            }

            // Replace the ICU collation keyword with an _
            Debug.Assert(_sWindowsName != null);
            index = _sWindowsName.IndexOf(ICU_COLLATION_KEYWORD, StringComparison.Ordinal);
            if (index >= 0)
            {
                _sName = string.Concat(_sWindowsName.AsSpan(0, index), "_", alternateSortName);
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

            _sSpecificCulture = _bNeutral ? IcuLocaleData.GetSpecificCultureName(_sRealName) : _sRealName;

            // Remove the sort from sName unless custom culture
            if (index > 0 && !_bNeutral && !IsCustomCultureId(_iLanguage))
            {
                _sName = _sWindowsName.Substring(0, index);
            }
            return true;
        }

        internal bool IsWin32Installed => false;

        internal static unsafe CultureData GetCurrentRegionData() => CultureInfo.CurrentCulture._cultureData;
    }
}
