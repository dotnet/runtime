// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace System.Globalization
{
    internal sealed partial class CultureData
    {
        private const int LOCALE_INFO_BUFFER_LEN = 80;

        private void JSInitLocaleInfo()
        {
            string? localeName = _sName;
            if (string.IsNullOrEmpty(localeName))
            {
                _sEnglishLanguage = "Invariant Language";
                _sNativeLanguage = _sEnglishLanguage;
                _sEnglishCountry = "Invariant Country";
                _sNativeCountry = _sEnglishCountry;
                _sEnglishDisplayName = $"{_sEnglishLanguage} ({_sEnglishCountry})";
                _sNativeDisplayName = _sEnglishDisplayName;
            }
            else
            {
                // English locale info
                (_sEnglishLanguage, _sEnglishCountry) = JSGetLocaleInfo(localeName, "en-US");
                _sEnglishDisplayName = string.IsNullOrEmpty(_sEnglishCountry) ?
                    _sEnglishLanguage :
                    $"{_sEnglishLanguage} ({_sEnglishCountry})";
                // Native locale info
                (_sNativeLanguage, _sNativeCountry) = JSGetLocaleInfo(localeName, localeName);
                _sNativeDisplayName = string.IsNullOrEmpty(_sNativeCountry) ?
                    _sNativeLanguage :
                    $"{_sNativeLanguage} ({_sNativeCountry})";
            }
        }

        // Returns the language and region names of localeName, localized into the language of cultureName.
        private unsafe (string, string) JSGetLocaleInfo(string localeName, string cultureName)
        {
            ReadOnlySpan<char> localeNameSpan = localeName.AsSpan();
            ReadOnlySpan<char> cultureNameSpan = cultureName.AsSpan();
            fixed (char* pLocaleName = &MemoryMarshal.GetReference(localeNameSpan))
            fixed (char* pCultureName = &MemoryMarshal.GetReference(cultureNameSpan))
            {
                char* buffer = stackalloc char[LOCALE_INFO_BUFFER_LEN];
                nint exceptionPtr = Interop.JsGlobalization.GetLocaleInfo(pLocaleName, localeNameSpan.Length, pCultureName, cultureNameSpan.Length, buffer, LOCALE_INFO_BUFFER_LEN, out int resultLength);
                Helper.MarshalAndThrowIfException(exceptionPtr);
                string result = new string(buffer, 0, resultLength);
                string[] subresults = result.Split("##");
                if (subresults.Length == 0)
                    throw new Exception("LocaleInfo received from the Browser is in incorrect format.");
                if (subresults.Length == 1)
                    return (subresults[0], ""); // Neutral culture
                return (subresults[0], subresults[1]);
            }
        }

        private string JSGetNativeDisplayName(string localeName, string cultureName)
        {
            (string languageName, string countryName) = JSGetLocaleInfo(localeName, cultureName);
            return string.IsNullOrEmpty(countryName) ?
                    languageName :
                    $"{languageName} ({countryName})";
        }
    }

    internal static class Helper
    {
        internal static int MarshalAndThrowIfException(nint exceptionPtr, bool failOnlyDebug = false, string failureMessage = "")
        {
            if (exceptionPtr != IntPtr.Zero)
            {
                string message = Marshal.PtrToStringUni(exceptionPtr)!;
                Marshal.FreeHGlobal(exceptionPtr);
                if (failOnlyDebug)
                {
                    Debug.Fail($"{failureMessage} {message}");
                    return -1;
                }
                throw new Exception(message);
            }
            return 0;
        }

    }
}
